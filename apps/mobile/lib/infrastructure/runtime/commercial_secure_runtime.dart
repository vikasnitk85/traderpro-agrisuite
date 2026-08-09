import 'dart:async';
import 'dart:io';

import 'package:flutter/widgets.dart';

import '../../app/commercial_secure_startup_state.dart';
import '../../core/security/commercial_storage_failure.dart';
import '../../core/security/commercial_storage_state_machine.dart';
import '../../core/security/storage_diagnostics.dart';
import '../database/commercial/commercial_database_initializer.dart';
import '../database/commercial/commercial_database_paths.dart';
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

    return CommercialSecureRuntime(
      storageOpener: () async {
        await initializer.initialize(
          context: CommercialProvisioningContext.explicitFirstInitialization,
        );
      },
      storageCloser: initializer.close,
    );
  }

  final CommercialStorageOpen _openStorage;
  final CommercialStorageClose _closeStorage;
  final bool observeApplicationLifecycle;

  Future<CommercialSecureStartupState>? _opening;
  Future<void>? _closing;
  CommercialSecureStartupState? _startupState;
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
      result = const CommercialSecureStartupState.ready();
    } on CommercialStorageException catch (error) {
      result = CommercialSecureStartupState.unavailable(error.code.safeCode);
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
    if (state == AppLifecycleState.detached) {
      unawaited(close());
    }
  }
}
