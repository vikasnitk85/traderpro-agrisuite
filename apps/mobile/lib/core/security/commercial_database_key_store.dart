import 'commercial_database_key_material.dart';

enum CommercialDatabaseKeyStoreStateKind {
  uninitialized,
  valid,
  missing,
  malformed,
  unavailable,
  invalidated,
  unexpected,
}

sealed class CommercialDatabaseKeyStoreState {
  const CommercialDatabaseKeyStoreState(this.kind);

  final CommercialDatabaseKeyStoreStateKind kind;
}

final class CommercialDatabaseKeyStoreUninitialized
    extends CommercialDatabaseKeyStoreState {
  const CommercialDatabaseKeyStoreUninitialized()
    : super(CommercialDatabaseKeyStoreStateKind.uninitialized);
}

final class CommercialDatabaseKeyStoreValid
    extends CommercialDatabaseKeyStoreState {
  const CommercialDatabaseKeyStoreValid(this.keyMaterial)
    : super(CommercialDatabaseKeyStoreStateKind.valid);

  final CommercialDatabaseKeyMaterial keyMaterial;
}

final class CommercialDatabaseKeyStoreMissing
    extends CommercialDatabaseKeyStoreState {
  const CommercialDatabaseKeyStoreMissing()
    : super(CommercialDatabaseKeyStoreStateKind.missing);
}

final class CommercialDatabaseKeyStoreMalformed
    extends CommercialDatabaseKeyStoreState {
  const CommercialDatabaseKeyStoreMalformed()
    : super(CommercialDatabaseKeyStoreStateKind.malformed);
}

final class CommercialDatabaseKeyStoreUnavailable
    extends CommercialDatabaseKeyStoreState {
  const CommercialDatabaseKeyStoreUnavailable()
    : super(CommercialDatabaseKeyStoreStateKind.unavailable);
}

final class CommercialDatabaseKeyStoreInvalidated
    extends CommercialDatabaseKeyStoreState {
  const CommercialDatabaseKeyStoreInvalidated()
    : super(CommercialDatabaseKeyStoreStateKind.invalidated);
}

final class CommercialDatabaseKeyStoreUnexpected
    extends CommercialDatabaseKeyStoreState {
  const CommercialDatabaseKeyStoreUnexpected()
    : super(CommercialDatabaseKeyStoreStateKind.unexpected);
}

abstract interface class CommercialDatabaseKeyStore {
  Future<CommercialDatabaseKeyStoreState> read({required bool keyExpected});

  Future<void> writeInitial(CommercialDatabaseKeyMaterial keyMaterial);
}
