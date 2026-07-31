import 'dart:async';

// ignore_for_file: prefer_initializing_formals

import 'procurement_poc_control_service.dart';
import 'procurement_poc_event_poller.dart';
import 'procurement_poc_ports.dart';
import 'procurement_poc_sync_engine.dart';
import '../domain/procurement_poc_models.dart';

final class TimerProcurementPocScheduler implements ProcurementPocScheduler {
  const TimerProcurementPocScheduler();

  @override
  ProcurementPocScheduledTask schedulePeriodic(
    Duration interval,
    void Function() callback,
  ) {
    return _TimerScheduledTask(Timer.periodic(interval, (_) => callback()));
  }
}

final class _TimerScheduledTask implements ProcurementPocScheduledTask {
  _TimerScheduledTask(this._timer);

  final Timer _timer;
  var _cancelled = false;

  @override
  void cancel() {
    if (_cancelled) {
      return;
    }
    _cancelled = true;
    _timer.cancel();
  }
}

final class ProcurementPocCycleNotification {
  const ProcurementPocCycleNotification({
    required this.automatic,
    required this.message,
    this.errorCode,
  });

  final bool automatic;
  final String message;
  final String? errorCode;
}

typedef ProcurementPocCycleCompleted =
    Future<void> Function(ProcurementPocCycleNotification notification);

final class ProcurementPocCoordinator {
  ProcurementPocCoordinator({
    required ProcurementPocProfileStore profileStore,
    required ProcurementPocSyncEngine syncEngine,
    required ProcurementPocEventPoller eventPoller,
    required ProcurementPocControlService controlService,
    ProcurementPocScheduler scheduler = const TimerProcurementPocScheduler(),
    ProcurementPocCycleCompleted? onCycleCompleted,
    this.pollingInterval = const Duration(seconds: 3),
  }) : _profileStore = profileStore,
       _syncEngine = syncEngine,
       _eventPoller = eventPoller,
       _controlService = controlService,
       _scheduler = scheduler,
       _onCycleCompleted = onCycleCompleted;

  final ProcurementPocProfileStore _profileStore;
  final ProcurementPocSyncEngine _syncEngine;
  final ProcurementPocEventPoller _eventPoller;
  final ProcurementPocControlService _controlService;
  final ProcurementPocScheduler _scheduler;
  final Duration pollingInterval;
  ProcurementPocCycleCompleted? _onCycleCompleted;
  ProcurementPocScheduledTask? _task;
  bool? _automaticSyncPaused;
  var _foreground = false;
  var _cycleRunning = false;
  var _disposed = false;

  bool get isCycleRunning => _cycleRunning;

  void setCycleCompletedCallback(ProcurementPocCycleCompleted callback) {
    if (_disposed) {
      return;
    }
    _onCycleCompleted = callback;
  }

  void start() {
    if (_disposed || _task != null) {
      return;
    }
    _task = _scheduler.schedulePeriodic(pollingInterval, _onTimer);
  }

  void _onTimer() {
    unawaited(
      runAutomaticCycle().catchError((Object _) {
        // _runCycle converts failures to safe notifications. This final
        // boundary prevents a scheduler callback from emitting an unhandled
        // asynchronous error if a future implementation violates that rule.
      }),
    );
  }

  void setForeground(bool foreground) {
    if (_disposed) {
      return;
    }
    _foreground = foreground;
  }

  void setAutomaticSyncPaused(bool paused) {
    if (_disposed) {
      return;
    }
    _automaticSyncPaused = paused;
  }

  Future<void> runAutomaticCycle() => _runCycle(automatic: true);

  Future<void> runManualCycle() => _runCycle(automatic: false);

  Future<void> _runCycle({required bool automatic}) async {
    if (_disposed || _cycleRunning || !_foreground) {
      return;
    }
    _cycleRunning = true;
    ProcurementPocCycleNotification? notification;
    try {
      final profile = await _profileStore.loadActiveProfile();
      if (_disposed || !_foreground) {
        return;
      }
      if (profile == null) {
        notification = ProcurementPocCycleNotification(
          automatic: automatic,
          message: 'Configure a development device profile first.',
          errorCode: 'POC_PROFILE_REQUIRED',
        );
        return;
      }
      if (_automaticWorkIsPaused(automatic, profile)) {
        return;
      }

      final sync = await _syncEngine.synchronize();
      if (_disposed || !_foreground) {
        return;
      }
      if (_automaticWorkIsPaused(automatic, profile)) {
        return;
      }
      await _controlService.queueDueHeartbeats();
      if (_disposed || !_foreground) {
        return;
      }
      if (_automaticWorkIsPaused(automatic, profile)) {
        return;
      }
      final controls = await _controlService.executePending();
      if (_disposed || !_foreground) {
        return;
      }
      if (_automaticWorkIsPaused(automatic, profile)) {
        return;
      }
      final events = await _eventPoller.pollOnce();
      notification = ProcurementPocCycleNotification(
        automatic: automatic,
        message: '${sync.message} ${controls.message} ${events.message}',
      );
    } on ProcurementPocApiException catch (error) {
      notification = ProcurementPocCycleNotification(
        automatic: automatic,
        message: error.message,
        errorCode: error.code,
      );
    } on ProcurementPocLocalException catch (error) {
      notification = ProcurementPocCycleNotification(
        automatic: automatic,
        message: error.message,
        errorCode: error.code,
      );
    } on Object {
      notification = ProcurementPocCycleNotification(
        automatic: automatic,
        message:
            'The foreground POC cycle stopped safely. Durable local facts '
            'remain available for retry.',
        errorCode: 'POC_AUTOMATIC_CYCLE_FAILED',
      );
    } finally {
      _cycleRunning = false;
      final callback = _onCycleCompleted;
      if (!_disposed && notification != null && callback != null) {
        try {
          await callback(notification);
        } on Object {
          // Refresh failures must not escape a periodic timer. A later cycle
          // reloads the same durable state.
        }
      }
    }
  }

  bool _automaticWorkIsPaused(
    bool automatic,
    PocDeviceProfile persistedProfile,
  ) {
    return automatic &&
        (_automaticSyncPaused ?? persistedProfile.automaticSyncPaused);
  }

  void dispose() {
    if (_disposed) {
      return;
    }
    _disposed = true;
    _foreground = false;
    _onCycleCompleted = null;
    _task?.cancel();
    _task = null;
  }
}
