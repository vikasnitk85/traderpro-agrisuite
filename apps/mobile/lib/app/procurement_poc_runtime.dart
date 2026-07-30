import 'dart:async';

import 'package:flutter/foundation.dart' show kReleaseMode;
import 'package:flutter/material.dart';

import '../core/database/database.dart';
import '../features/procurement_poc/procurement_poc.dart';
import '../features/receiving/receiving.dart';

// ignore_for_file: prefer_initializing_formals

final class ProcurementPocRuntime with WidgetsBindingObserver {
  ProcurementPocRuntime._({
    required this.database,
    required this.repository,
    required this.syncEngine,
    required this.eventPoller,
    required this.controlService,
    required this.coordinator,
    required this.controller,
    required _ProcurementPocApiScope apiScope,
  }) : _apiScope = apiScope;

  static Future<ProcurementPocRuntime> open({
    ProcurementPocFeatureConfiguration feature =
        const ProcurementPocFeatureConfiguration(
          compileTimeEnabled: procurementPocCompileTimeEnabled,
          releaseMode: kReleaseMode,
        ),
    ProcurementPocScheduler scheduler = const TimerProcurementPocScheduler(),
    ProcurementPocClock clock = const SystemProcurementPocClock(),
    ProcurementPocApiFactory? apiFactory,
    ProcurementPocBootstrapApiFactory? bootstrapApiFactory,
  }) async {
    final database = await LocalDatabaseOpeners.openForMobile();
    return fromDatabase(
      database: database,
      feature: feature,
      scheduler: scheduler,
      clock: clock,
      apiFactory: apiFactory,
      bootstrapApiFactory: bootstrapApiFactory,
    );
  }

  static ProcurementPocRuntime fromDatabase({
    required TraderProLocalDatabase database,
    required ProcurementPocFeatureConfiguration feature,
    ProcurementPocScheduler scheduler = const TimerProcurementPocScheduler(),
    ProcurementPocClock clock = const SystemProcurementPocClock(),
    ProcurementPocApiFactory? apiFactory,
    ProcurementPocBootstrapApiFactory? bootstrapApiFactory,
  }) {
    final receivingStore = LocalReceivingStore(
      database: database,
      installReference: 'POCDEV',
    );
    final repository = ProcurementPocLocalRepository(
      database: database,
      localReceivingStore: receivingStore,
    );
    final apiScope = _ProcurementPocApiScope();
    final guardedApiFactory = apiScope.profileFactory(
      apiFactory ?? ProcurementPocApiClient.forProfile,
    );
    final guardedBootstrapFactory = apiScope.bootstrapFactory(
      bootstrapApiFactory ??
          (baseUrl) => ProcurementPocApiClient(backendBaseUrl: baseUrl),
    );
    final syncEngine = ProcurementPocSyncEngine(
      feature: feature,
      profileStore: repository,
      syncStore: repository,
      apiFactory: guardedApiFactory,
      clock: clock,
    );
    final eventPoller = ProcurementPocEventPoller(
      feature: feature,
      profileStore: repository,
      eventStore: repository,
      apiFactory: guardedApiFactory,
      clock: clock,
    );
    final controlService = ProcurementPocControlService(
      feature: feature,
      profileStore: repository,
      syncStore: repository,
      controlStore: repository,
      apiFactory: guardedApiFactory,
      clock: clock,
    );
    final coordinator = ProcurementPocCoordinator(
      profileStore: repository,
      syncEngine: syncEngine,
      eventPoller: eventPoller,
      controlService: controlService,
      scheduler: scheduler,
    );
    final controller = ProcurementPocController(
      profileStore: repository,
      syncStore: repository,
      readStore: repository,
      receivingStore: receivingStore,
      controlService: controlService,
      coordinator: coordinator,
      apiFactory: guardedApiFactory,
      bootstrapApiFactory: guardedBootstrapFactory,
      clock: clock,
    );
    coordinator.setCycleCompletedCallback(controller.refreshAfterCycle);
    return ProcurementPocRuntime._(
      database: database,
      repository: repository,
      syncEngine: syncEngine,
      eventPoller: eventPoller,
      controlService: controlService,
      coordinator: coordinator,
      controller: controller,
      apiScope: apiScope,
    );
  }

  final TraderProLocalDatabase database;
  final ProcurementPocLocalRepository repository;
  final ProcurementPocSyncEngine syncEngine;
  final ProcurementPocEventPoller eventPoller;
  final ProcurementPocControlService controlService;
  final ProcurementPocCoordinator coordinator;
  final ProcurementPocController controller;
  final _ProcurementPocApiScope _apiScope;
  var _started = false;
  var _disposed = false;
  Future<void>? _startFuture;
  Future<void>? _disposeFuture;

  bool get isDisposed => _disposed;

  Future<void> start() {
    if (_disposed) {
      return Future<void>.value();
    }
    final existing = _startFuture;
    if (_started) {
      return existing ?? Future<void>.value();
    }
    _started = true;
    WidgetsBinding.instance.addObserver(this);
    coordinator.setForeground(
      WidgetsBinding.instance.lifecycleState == AppLifecycleState.resumed,
    );
    coordinator.start();
    return _startFuture = controller.initialize();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    coordinator.setForeground(state == AppLifecycleState.resumed);
  }

  Future<void> disposeRuntime() {
    return _disposeFuture ??= _disposeOnce();
  }

  Future<void> _disposeOnce() async {
    if (_disposed) {
      return;
    }
    _disposed = true;
    if (_started) {
      WidgetsBinding.instance.removeObserver(this);
    }
    coordinator.dispose();
    _apiScope.dispose();
    controller.dispose();
    await database.close();
  }
}

final class ProcurementPocRuntimeHost extends StatefulWidget {
  const ProcurementPocRuntimeHost({
    required this.runtime,
    required this.child,
    super.key,
  });

  final ProcurementPocRuntime runtime;
  final Widget child;

  @override
  State<ProcurementPocRuntimeHost> createState() =>
      _ProcurementPocRuntimeHostState();
}

final class _ProcurementPocRuntimeHostState
    extends State<ProcurementPocRuntimeHost> {
  @override
  void initState() {
    super.initState();
    unawaited(widget.runtime.start());
  }

  @override
  void dispose() {
    unawaited(widget.runtime.disposeRuntime());
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}

final class _ProcurementPocApiScope {
  final _clients = <_ScopedProcurementPocApi>{};
  var _disposed = false;

  ProcurementPocApiFactory profileFactory(ProcurementPocApiFactory inner) {
    return (profile) => _track(inner(profile));
  }

  ProcurementPocBootstrapApiFactory bootstrapFactory(
    ProcurementPocBootstrapApiFactory inner,
  ) {
    return (baseUrl) => _track(inner(baseUrl));
  }

  ProcurementPocApi _track(ProcurementPocApi inner) {
    _requireActive();
    late final _ScopedProcurementPocApi scoped;
    scoped = _ScopedProcurementPocApi(
      inner: inner,
      requireActive: _requireActive,
      onDispose: () => _clients.remove(scoped),
    );
    _clients.add(scoped);
    return scoped;
  }

  void _requireActive() {
    if (_disposed) {
      throw const ProcurementPocApiException(
        code: 'POC_RUNTIME_DISPOSED',
        message: 'The development POC runtime is no longer active.',
        retryable: true,
        responseAmbiguous: false,
      );
    }
  }

  void dispose() {
    if (_disposed) {
      return;
    }
    _disposed = true;
    for (final client in _clients.toList(growable: false)) {
      client.dispose();
    }
    _clients.clear();
  }
}

final class _ScopedProcurementPocApi implements ProcurementPocApi {
  _ScopedProcurementPocApi({
    required ProcurementPocApi inner,
    required void Function() requireActive,
    required void Function() onDispose,
  }) : _inner = inner,
       _requireActive = requireActive,
       _onDispose = onDispose;

  final ProcurementPocApi _inner;
  final void Function() _requireActive;
  final void Function() _onDispose;
  var _disposed = false;

  void _beforeCall() {
    if (_disposed) {
      throw const ProcurementPocApiException(
        code: 'POC_CLIENT_DISPOSED',
        message: 'The development API client is no longer active.',
        retryable: true,
        responseAmbiguous: false,
      );
    }
    _requireActive();
  }

  @override
  Future<MobileSyncBatchResult> sendOperations(
    List<MobileSyncOperationEnvelope> operations,
  ) {
    _beforeCall();
    return _inner.sendOperations(operations);
  }

  @override
  Future<MobileSyncEventPage> readEvents({
    required int after,
    required int limit,
  }) {
    _beforeCall();
    return _inner.readEvents(after: after, limit: limit);
  }

  @override
  Future<ReceivingSessionListPage> listReceivingSessions({
    String? status,
    int? after,
    int limit = 50,
  }) {
    _beforeCall();
    return _inner.listReceivingSessions(
      status: status,
      after: after,
      limit: limit,
    );
  }

  @override
  Future<ReceivingSessionLiveView> getReceivingSessionLiveView(
    String sessionId,
  ) {
    _beforeCall();
    return _inner.getReceivingSessionLiveView(sessionId);
  }

  @override
  Future<ProcurementPocCommandResult> heartbeat({
    required String sessionId,
    required String leaseId,
    required String idempotencyKey,
  }) {
    _beforeCall();
    return _inner.heartbeat(
      sessionId: sessionId,
      leaseId: leaseId,
      idempotencyKey: idempotencyKey,
    );
  }

  @override
  Future<ProcurementPocCommandResult> approve({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) {
    _beforeCall();
    return _inner.approve(
      sessionId: sessionId,
      expectedVersion: expectedVersion,
      idempotencyKey: idempotencyKey,
    );
  }

  @override
  Future<ProcurementPocCommandResult> finalize({
    required String sessionId,
    required int expectedVersion,
    required String idempotencyKey,
  }) {
    _beforeCall();
    return _inner.finalize(
      sessionId: sessionId,
      expectedVersion: expectedVersion,
      idempotencyKey: idempotencyKey,
    );
  }

  @override
  Future<ProcurementPocBootstrapResult> bootstrap() {
    _beforeCall();
    return _inner.bootstrap();
  }

  @override
  void dispose() {
    if (_disposed) {
      return;
    }
    _disposed = true;
    _inner.dispose();
    _onDispose();
  }
}
