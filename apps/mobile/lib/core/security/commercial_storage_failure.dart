enum CommercialStorageFailureCode {
  secureStoreMissing('COMMERCIAL_SECURE_STORE_MISSING'),
  secureStoreMalformed('COMMERCIAL_SECURE_STORE_MALFORMED'),
  secureStoreUnavailable('COMMERCIAL_SECURE_STORE_UNAVAILABLE'),
  secureStoreInvalidated('COMMERCIAL_SECURE_STORE_INVALIDATED'),
  databaseKeyUnavailable('COMMERCIAL_DATABASE_KEY_UNAVAILABLE'),
  databaseConfigurationMismatch('COMMERCIAL_DATABASE_CONFIGURATION_MISMATCH'),
  databaseWrongKeyOrUnreadable('COMMERCIAL_DATABASE_WRONG_KEY_OR_UNREADABLE'),
  databaseCorrupt('COMMERCIAL_DATABASE_CORRUPT'),
  databaseInitializationIncomplete(
    'COMMERCIAL_DATABASE_INITIALIZATION_INCOMPLETE',
  ),
  databaseMigrationFailed('COMMERCIAL_DATABASE_MIGRATION_FAILED'),
  storageUnavailable('COMMERCIAL_STORAGE_UNAVAILABLE'),
  unexpectedStorageFailure('COMMERCIAL_STORAGE_UNEXPECTED_FAILURE');

  const CommercialStorageFailureCode(this.safeCode);

  final String safeCode;
}

final class CommercialStorageException implements Exception {
  const CommercialStorageException(this.code, {required this.phase});

  final CommercialStorageFailureCode code;
  final String phase;

  @override
  String toString() =>
      'CommercialStorageException(${code.safeCode}, phase: $phase)';
}
