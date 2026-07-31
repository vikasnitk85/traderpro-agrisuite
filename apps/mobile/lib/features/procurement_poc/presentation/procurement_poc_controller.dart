// ignore_for_file: prefer_initializing_formals

import 'package:flutter/foundation.dart';

import '../../../core/measurements/weight_processing_method.dart';
import '../../../core/sync/local_outbox_operation.dart';
import '../../receiving/application/local_receiving_commands.dart';
import '../../receiving/domain/local_receiving_records.dart';
import '../../receiving/domain/weight_source.dart';
import '../../receiving/infrastructure/local_receiving_store.dart';
import '../application/procurement_poc_control_service.dart';
import '../application/procurement_poc_coordinator.dart';
import '../application/procurement_poc_ports.dart';
import '../domain/procurement_poc_models.dart';

final class ProcurementPocController extends ChangeNotifier {
  ProcurementPocController({
    required ProcurementPocProfileStore profileStore,
    required ProcurementPocSyncStore syncStore,
    required ProcurementPocReadStore readStore,
    required LocalReceivingStore receivingStore,
    required ProcurementPocControlService controlService,
    required ProcurementPocCoordinator coordinator,
    required ProcurementPocApiFactory apiFactory,
    required ProcurementPocBootstrapApiFactory bootstrapApiFactory,
    ProcurementPocClock clock = const SystemProcurementPocClock(),
  }) : _profileStore = profileStore,
       _syncStore = syncStore,
       _readStore = readStore,
       _receivingStore = receivingStore,
       _controlService = controlService,
       _coordinator = coordinator,
       _apiFactory = apiFactory,
       _bootstrapApiFactory = bootstrapApiFactory,
       _clock = clock;

  final ProcurementPocProfileStore _profileStore;
  final ProcurementPocSyncStore _syncStore;
  final ProcurementPocReadStore _readStore;
  final LocalReceivingStore _receivingStore;
  final ProcurementPocControlService _controlService;
  final ProcurementPocCoordinator _coordinator;
  final ProcurementPocApiFactory _apiFactory;
  final ProcurementPocBootstrapApiFactory _bootstrapApiFactory;
  final ProcurementPocClock _clock;

  PocDeviceProfile? profile;
  ProcurementPocBootstrapResult? bootstrapResult;
  LocalReceivingSessionRecord? activeLocalSession;
  ReceivingSessionCloudState? activeCloudState;
  List<LocalReceivingEntryRecord> localEntries = const [];
  List<LocalOutboxOperation> localOperations = const [];
  List<RemoteReceivingSessionProjection> remoteSessions = const [];
  RemoteReceivingSessionProjection? selectedRemoteSession;
  List<PocControlCommand> controlCommands = const [];
  PocSyncDiagnostics? diagnostics;
  String? lastMessage;
  String? lastErrorCode;
  bool busy = false;
  bool initialized = false;
  var _disposed = false;
  Future<void>? _initializationFuture;

  bool get canRetrySameFinalization {
    final sessionId = selectedRemoteSession?.sessionId;
    return sessionId != null &&
        controlCommands.any(
          (command) =>
              command.sessionId == sessionId &&
              command.commandType == PocControlCommandType.finalize &&
              command.status == PocControlCommandStatus.completed &&
              command.sourceKey == profile?.sourceKey &&
              command.actingDeviceId == profile?.deviceId,
        );
  }

  Future<void> initialize() {
    if (_disposed || initialized) {
      return Future<void>.value();
    }
    return _initializationFuture ??= _initializeOnce();
  }

  Future<void> _initializeOnce() async {
    try {
      await _run(() async {
        profile = await _profileStore.loadActiveProfile();
        _coordinator.setAutomaticSyncPaused(
          profile?.automaticSyncPaused ?? false,
        );
        await _refreshLocalState();
        if (profile != null) {
          await _refreshRemoteFromLocal();
        }
        initialized = true;
      });
    } finally {
      if (!initialized) {
        _initializationFuture = null;
      }
    }
  }

  Future<void> saveProfile({
    required String backendBaseUrl,
    required String workspaceId,
    required String deviceId,
    required PocDisplayRole displayRole,
    String? displayLabel,
  }) async {
    await _run(() async {
      final now = _clock.nowUtc();
      final candidate = PocDeviceProfile(
        backendBaseUrl: backendBaseUrl,
        workspaceId: workspaceId,
        deviceId: deviceId,
        displayRole: displayRole,
        displayLabel: displayLabel,
        automaticSyncPaused: profile?.automaticSyncPaused ?? false,
        createdAtUtc: profile?.createdAtUtc ?? now,
        updatedAtUtc: now,
      );
      profile = await _profileStore.saveActiveProfile(candidate);
      _coordinator.setAutomaticSyncPaused(profile!.automaticSyncPaused);
      await _refreshRemoteFromLocal();
      lastMessage =
          'Development profile saved. These IDs are context only and are '
          'not authentication.';
    });
  }

  Future<void> bootstrap(String backendBaseUrl) async {
    await _run(() async {
      final api = _bootstrapApiFactory(normalizeBackendBaseUrl(backendBaseUrl));
      try {
        bootstrapResult = await api.bootstrap();
        lastMessage =
            'Development bootstrap completed. Select the correct device ID '
            'for this installation.';
      } finally {
        api.dispose();
      }
    });
  }

  Future<void> testConnection() async {
    await _run(() async {
      final current = _requireProfile();
      final api = _apiFactory(current);
      try {
        await api.listReceivingSessions(limit: 1);
        lastMessage = 'Development backend connection succeeded.';
      } finally {
        api.dispose();
      }
    });
  }

  Future<void> createLocalSession() async {
    await _run(() async {
      final current = _requireProfile();
      if (current.displayRole != PocDisplayRole.operator) {
        throw StateError('Only Operator display mode can create a session.');
      }
      await _receivingStore.createLocalReceivingSession();
      await _refreshLocalState();
      lastMessage = 'Receiving Session saved on this device.';
    });
  }

  Future<void> recordManualWeight({
    required String productReference,
    required String bagTypeReference,
    required int bagCount,
    required String rawWeightKg,
    required int decimalPlaces,
    required WeightProcessingMethod processingMethod,
    required WeightSource weightSource,
    required String capturedAtDeviceUtcText,
  }) async {
    await _run(() async {
      final currentProfile = _requireProfile();
      final session = activeLocalSession;
      if (currentProfile.displayRole != PocDisplayRole.operator ||
          session == null ||
          session.localStatus.storageValue != 'Open') {
        throw StateError('An editable Operator session is required.');
      }
      final capturedAtDeviceUtc = _parseStrictUtc(capturedAtDeviceUtcText);
      await _receivingStore.recordWeightLocally(
        RecordWeightLocallyCommand(
          sessionId: session.id,
          productReference: productReference,
          bagTypeReference: bagTypeReference,
          bagCount: bagCount,
          rawWeightKg: rawWeightKg,
          decimalPlaces: decimalPlaces,
          processingMethod: processingMethod,
          weightSource: weightSource,
          capturedAtDeviceUtc: capturedAtDeviceUtc,
        ),
      );
      await _refreshLocalState();
      lastMessage = 'Immutable weight saved on this device.';
    });
  }

  Future<void> submitLocalSession() async {
    await _run(() async {
      final session = activeLocalSession;
      if (_requireProfile().displayRole != PocDisplayRole.operator ||
          session == null) {
        throw StateError('An Operator session is required.');
      }
      await _receivingStore.submitSessionLocally(sessionId: session.id);
      await _refreshLocalState();
      lastMessage = 'Submission saved locally and queued for cloud sync.';
    });
  }

  Future<void> synchronizeNow() async {
    await _run(() async {
      await _coordinator.runManualCycle();
      lastMessage = 'Manual foreground synchronization cycle completed.';
    });
  }

  Future<void> setAutomaticSyncPaused(bool paused) async {
    await _run(() async {
      final previouslyPaused = _requireProfile().automaticSyncPaused;
      _coordinator.setAutomaticSyncPaused(paused);
      try {
        profile = await _profileStore.setAutomaticSyncPaused(
          paused,
          _clock.nowUtc(),
        );
      } on Object {
        _coordinator.setAutomaticSyncPaused(previouslyPaused);
        rethrow;
      }
      lastMessage = paused
          ? 'Automatic synchronization paused. Manual Sync Now remains '
                'available.'
          : 'Automatic foreground synchronization resumed.';
    });
  }

  Future<void> refreshOwnerSessions() async {
    await _run(() async {
      final current = _requireProfile();
      final api = _apiFactory(current);
      try {
        int? cursor;
        final sessions = <RemoteReceivingSessionProjection>[];
        for (var page = 0; page < 100; page++) {
          final result = await api.listReceivingSessions(
            after: cursor,
            limit: 100,
          );
          sessions.addAll(result.sessions);
          if (!result.hasMore) {
            break;
          }
          cursor = result.nextCursor;
        }
        await _readStore.saveSessionList(profile: current, sessions: sessions);
      } finally {
        api.dispose();
      }
      await _refreshRemoteFromLocal();
      lastMessage = 'Server session list refreshed.';
    });
  }

  Future<void> openRemoteSession(String sessionId) async {
    await _run(() async {
      selectedRemoteSession = await _readStore.getRemoteSession(
        _requireProfile(),
        sessionId,
      );
      controlCommands = await _readStore.listControlCommands(
        _requireProfile(),
        sessionId: sessionId,
      );
    });
  }

  Future<void> refreshLiveView(String sessionId) async {
    await _run(() async {
      await _refreshLiveViewInternal(sessionId);
      lastMessage = 'Server live view refreshed.';
    });
  }

  Future<void> approveSelected() async {
    await _run(() async {
      final current = _requireOwnerSelection('SubmittedForReview');
      await _controlService.queueApproval(
        sessionId: current.sessionId,
        expectedVersion: current.cloudVersion,
      );
      await _controlService.executePending();
      await _refreshLiveViewInternal(current.sessionId);
    });
  }

  Future<void> finalizeSelected() async {
    await _run(() async {
      final current = _requireOwnerSelection('Approved');
      await _controlService.queueFinalization(
        sessionId: current.sessionId,
        expectedVersion: current.cloudVersion,
      );
      await _controlService.executePending();
      await _refreshLiveViewInternal(current.sessionId);
    });
  }

  Future<void> retrySameFinalization() async {
    await _run(() async {
      final current = _requireOwnerSelection('Finalized');
      await _controlService.retrySameFinalization(current.sessionId);
      await _controlService.executePending();
      await _refreshLiveViewInternal(current.sessionId);
      lastMessage =
          'The exact persisted finalization idempotency key was replayed.';
    });
  }

  Future<void> _refreshLiveViewInternal(String sessionId) async {
    final current = _requireProfile();
    final api = _apiFactory(current);
    try {
      final live = await api.getReceivingSessionLiveView(sessionId);
      await _readStore.saveLiveView(profile: current, liveView: live);
    } finally {
      api.dispose();
    }
    await _refreshRemoteFromLocal();
    selectedRemoteSession = await _readStore.getRemoteSession(
      current,
      sessionId,
    );
    controlCommands = await _readStore.listControlCommands(
      current,
      sessionId: sessionId,
    );
  }

  Future<void> refreshAfterCycle(
    ProcurementPocCycleNotification notification,
  ) async {
    if (_disposed) {
      return;
    }
    try {
      profile = await _profileStore.loadActiveProfile();
      if (_disposed) {
        return;
      }
      await _refreshLocalState();
      await _refreshRemoteFromLocal();
      if (_disposed) {
        return;
      }
      lastMessage = notification.message;
      lastErrorCode = notification.errorCode;
      _notifyIfActive();
    } on ProcurementPocLocalException catch (error) {
      if (_disposed) {
        return;
      }
      lastErrorCode = error.code;
      lastMessage = error.message;
      _notifyIfActive();
    } on Object {
      if (_disposed) {
        return;
      }
      lastErrorCode = 'POC_STATE_REFRESH_FAILED';
      lastMessage =
          'The foreground cycle completed, but visible state could not be '
          'reloaded safely. The next cycle will retry.';
      _notifyIfActive();
    }
  }

  Future<void> _refreshLocalState() async {
    final sessions = await _readStore.listLocalReceivingSessions();
    activeLocalSession = sessions.isEmpty ? null : sessions.first;
    final session = activeLocalSession;
    if (session == null) {
      activeCloudState = null;
      localEntries = const [];
      localOperations = const [];
    } else {
      final current = profile;
      activeCloudState = current == null
          ? null
          : await _syncStore.getCloudState(current, session.id);
      localEntries = await _readStore.listLocalReceivingEntries(session.id);
      localOperations = await _readStore.listLocalOutboxOperations(
        aggregateId: session.id,
      );
    }
    final current = profile;
    if (current != null) {
      diagnostics = await _readStore.loadDiagnostics(current);
      controlCommands = await _readStore.listControlCommands(current);
    } else {
      diagnostics = null;
      controlCommands = const [];
    }
  }

  Future<void> _refreshRemoteFromLocal() async {
    final current = profile;
    if (current == null) {
      remoteSessions = const [];
      selectedRemoteSession = null;
      return;
    }
    remoteSessions = await _readStore.listRemoteSessions(current);
    final selectedId = selectedRemoteSession?.sessionId;
    if (selectedId != null) {
      selectedRemoteSession = await _readStore.getRemoteSession(
        current,
        selectedId,
      );
    }
    diagnostics = await _readStore.loadDiagnostics(current);
  }

  PocDeviceProfile _requireProfile() {
    return profile ??
        (throw StateError('Configure the development device profile first.'));
  }

  RemoteReceivingSessionProjection _requireOwnerSelection(
    String requiredStatus,
  ) {
    final currentProfile = _requireProfile();
    final session = selectedRemoteSession;
    if (currentProfile.displayRole != PocDisplayRole.owner ||
        session == null ||
        session.status != requiredStatus) {
      throw StateError(
        'Owner display mode and status $requiredStatus are required.',
      );
    }
    return session;
  }

  Future<void> _run(Future<void> Function() action) async {
    if (_disposed || busy) {
      return;
    }
    busy = true;
    lastErrorCode = null;
    _notifyIfActive();
    try {
      await action();
    } on ProcurementPocApiException catch (error) {
      lastErrorCode = error.code;
      lastMessage = error.message;
    } on ProcurementPocLocalException catch (error) {
      lastErrorCode = error.code;
      lastMessage = error.message;
    } on FormatException catch (error) {
      lastErrorCode = 'POC_INPUT_INVALID';
      lastMessage = error.message;
    } on Object {
      lastErrorCode = 'POC_ACTION_FAILED';
      lastMessage =
          'The development action could not be completed. Durable local '
          'facts were retained.';
    } finally {
      busy = false;
      _notifyIfActive();
    }
  }

  static DateTime _parseStrictUtc(String value) {
    final text = value.trim();
    final hasUtcSuffix = RegExp(r'(?:Z|[+-]00:00)$').hasMatch(text);
    final parsed = hasUtcSuffix ? DateTime.tryParse(text) : null;
    if (parsed == null || !parsed.isUtc) {
      throw const ProcurementPocLocalException(
        ProcurementPocLocalException.captureTimestampInvalid,
        'Captured time must be valid ISO-8601 text with an explicit UTC '
        'offset, for example 2026-07-30T09:30:00.000Z.',
      );
    }
    return parsed;
  }

  void _notifyIfActive() {
    if (!_disposed) {
      notifyListeners();
    }
  }

  @override
  void dispose() {
    if (_disposed) {
      return;
    }
    _disposed = true;
    super.dispose();
  }
}
