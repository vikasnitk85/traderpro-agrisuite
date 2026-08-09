enum StorageDiagnosticPhase {
  inspect,
  provision,
  secureStore,
  configure,
  verify,
  schema,
  promote,
  reopen,
  close,
}

final class StorageDiagnosticEvent {
  const StorageDiagnosticEvent({
    required this.safeCode,
    required this.phase,
    required this.databaseExists,
    required this.secureStoreState,
    required this.schemaContractVersion,
    required this.storageContractVersion,
    required this.engineContractVersion,
    required this.coarsePlatform,
    required this.timestampUtc,
    required this.ephemeralCorrelationId,
  });

  final String safeCode;
  final StorageDiagnosticPhase phase;
  final bool databaseExists;
  final String secureStoreState;
  final int schemaContractVersion;
  final int storageContractVersion;
  final int engineContractVersion;
  final String coarsePlatform;
  final DateTime timestampUtc;
  final String ephemeralCorrelationId;
}

abstract interface class StorageDiagnosticSink {
  void record(StorageDiagnosticEvent event);
}

final class NoopStorageDiagnosticSink implements StorageDiagnosticSink {
  const NoopStorageDiagnosticSink();

  @override
  void record(StorageDiagnosticEvent event) {}
}
