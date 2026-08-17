import 'dart:async';
import 'dart:io';

import 'package:flutter/widgets.dart';

import '../../app/commercial_identity_controller.dart';
import '../../app/commercial_secure_startup_state.dart';
import '../../core/identity/commercial_identity_failure.dart';
import '../../core/security/commercial_storage_failure.dart';
import '../../core/security/commercial_storage_state_machine.dart';
import '../../core/security/storage_diagnostics.dart';
import '../database/commercial/commercial_database.dart';
import '../database/commercial/commercial_database_initializer.dart';
import '../database/commercial/commercial_database_paths.dart';
import '../database/commercial/commercial_identity_binding_repository.dart';
import '../identity/commercial_identity_service.dart';
import '../identity/commercial_refresh_coordinator.dart';
import '../identity/flutter_secure_commercial_identity_credential_store.dart';
import '../network/commercial_api_environment.dart';
import '../network/commercial_identity_http_client.dart';
import '../security/flutter_secure_commercial_database_key_store.dart';
import '../storage/commercial_provisioning_marker_store.dart';

typedef CommercialStorageOpen = Future<void> Function();
typedef CommercialStorageClose = Future<void> Function();

/// The sole production composition and lifecycle owner for Commercial storage.
///
/// This boundary exposes only a safe startup state. It never exposes the Drift
/// database, secure-store adapter, or raw key material to application widgets.
final class CommercialSecureRuntime with WidgetsBindingObserver {
  CommercialSecureRuntime({
    required CommercialStorageOpen storageOpener,
    required CommercialStorageClose storageCloser,
    this.observeApplicationLifecycle = true,
  }) : _openStorage = storageOpener,
       _closeStorage = storageCloser;

  factory CommercialSecureRuntime.production({
    required Directory applicationSupportDirectory,
    StorageDiagnosticSink diagnosticSink = const NoopStorageDiagnosticSink(),
  }) {
    final paths = CommercialDatabasePaths.fromApplicationSupport(
      applicationSupportDirectory,
    );
    final initializer = CommercialDatabaseInitializer(
      paths: paths,
      keyStore: FlutterSecureCommercialDatabaseKeyStore(),
      markerStore: CommercialProvisioningMarkerStore(paths.provisioningMarker),
      diagnosticSink: diagnosticSink,
    );
    final credentialStore = FlutterSecureCommercialIdentityCredentialStore();
    CommercialIdentityHttpClient? identityTransport;
    CommercialRefreshCoordinator? refreshCoordinator;
    CommercialIdentityController? identityController;
    late final CommercialSecureRuntime runtime;
    runtime = CommercialSecureRuntime(
      storageOpener: () async {
        const allowDebugHttp = bool.fromEnvironment(
          'TRADERPRO_ALLOW_DEBUG_HTTP',
          defaultValue: false,
        );
        final environment = CommercialApiEnvironment.fromBuildDefine(
          allowDebugHttp: allowDebugHttp,
        );
        final database = await initializer.initialize(
          context: CommercialProvisioningContext.explicitFirstInitialization,
        );
        final bindingRepository = CommercialIdentityBindingRepository(database);
        final transport = CommercialIdentityHttpClient(
          origin: environment.origin,
        );
        final identityService = CommercialIdentityService(
          transport: transport,
          credentialStore: credentialStore,
          activationAttemptStore: credentialStore,
          bindingPort: bindingRepository,
          normalizedApiOrigin: environment.origin.normalized,
          installationReferenceReader: () =>
              _readInstallationReference(database),
        );
        final coordinator = CommercialRefreshCoordinator(
          identityService: identityService,
          credentialStore: credentialStore,
        );
        final controller = CommercialIdentityController(
          identityService: identityService,
          refreshCoordinator: coordinator,
          credentialStore: credentialStore,
          activationAttemptStore: credentialStore,
          bindingPort: bindingRepository,
        );
        identityTransport = transport;
        refreshCoordinator = coordinator;
        identityController = controller;
        runtime._identityController = controller;
        await controller.start();
      },
      storageCloser: () async {
        identityController?.dispose();
        await refreshCoordinator?.dispose();
        identityTransport?.dispose();
        await initializer.close();
      },
    );
    return runtime;
  }

  final CommercialStorageOpen _openStorage;
  final CommercialStorageClose _closeStorage;
  final bool observeApplicationLifecycle;

  Future<CommercialSecureStartupState>? _opening;
  Future<void>? _closing;
  CommercialSecureStartupState? _startupState;
  CommercialIdentityController? _identityController;
  bool _observerAttached = false;

  CommercialSecureStartupState? get startupState => _startupState;

  Future<CommercialSecureStartupState> open() {
    final state = _startupState;
    if (state != null) {
      return Future<CommercialSecureStartupState>.value(state);
    }
    final inFlight = _opening;
    if (inFlight != null) {
      return inFlight;
    }
    if (_closing != null) {
      const closed = CommercialSecureStartupState.closed();
      _startupState = closed;
      return Future<CommercialSecureStartupState>.value(closed);
    }

    _attachLifecycleObserver();
    final opening = _openOnce();
    _opening = opening;
    return opening;
  }

  Future<CommercialSecureStartupState> _openOnce() async {
    late final CommercialSecureStartupState result;
    try {
      await _openStorage();
      result = CommercialSecureStartupState.ready(_identityController);
    } on CommercialStorageException catch (error) {
      result = CommercialSecureStartupState.unavailable(error.code.safeCode);
    } on CommercialIdentityFailure catch (error) {
      result = CommercialSecureStartupState.unavailable(error.safeCode);
    } on Object {
      result = CommercialSecureStartupState.unavailable(
        CommercialStorageFailureCode.unexpectedStorageFailure.safeCode,
      );
    }
    _startupState = result;
    return result;
  }

  Future<void> close() {
    final inFlight = _closing;
    if (inFlight != null) {
      return inFlight;
    }
    final closing = _closeOnce();
    _closing = closing;
    return closing;
  }

  Future<void> _closeOnce() async {
    try {
      final opening = _opening;
      if (opening != null) {
        await opening;
      }
      await _closeStorage();
    } finally {
      _detachLifecycleObserver();
      _startupState = const CommercialSecureStartupState.closed();
    }
  }

  void _attachLifecycleObserver() {
    if (!observeApplicationLifecycle || _observerAttached) {
      return;
    }
    WidgetsBinding.instance.addObserver(this);
    _observerAttached = true;
  }

  void _detachLifecycleObserver() {
    if (!_observerAttached) {
      return;
    }
    WidgetsBinding.instance.removeObserver(this);
    _observerAttached = false;
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      final controller = _identityController;
      if (controller != null) {
        unawaited(controller.onResume());
      }
    }
    if (state == AppLifecycleState.detached) {
      unawaited(close());
    }
  }

  static Future<String?> _readInstallationReference(
    CommercialDatabase database,
  ) async {
    final metadata = await database
        .select(database.commercialStorageMetadata)
        .getSingleOrNull();
    return metadata?.installationId;
  }
}
