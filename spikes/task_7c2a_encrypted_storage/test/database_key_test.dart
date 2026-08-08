import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';

void main() {
  test('explicit provisioning creates and persists one strong key', () async {
    final store = MemoryKeyStore();
    final controller = DatabaseKeyController(store);

    final first = await controller.provisionNewDatabase(databaseExists: false);
    final second = await controller.provisionNewDatabase(databaseExists: false);

    expect(first, matches(RegExp(r'^[0-9a-f]{64}$')));
    expect(second, first);
    expect(store.writes, 1);
  });

  test(
    'missing or reset key fails closed without silent replacement',
    () async {
      final store = MemoryKeyStore();
      final controller = DatabaseKeyController(store);

      await expectLater(
        controller.loadExisting(),
        throwsA(isA<MissingDatabaseKeyException>()),
      );
      expect(store.writes, 0);

      await controller.provisionNewDatabase(databaseExists: false);
      store.value =
          null; // Simulates a secure-store reset while DB still exists.
      await expectLater(
        controller.loadExisting(),
        throwsA(isA<MissingDatabaseKeyException>()),
      );
      expect(store.writes, 1);
    },
  );

  test(
    'provisioning refuses to replace a missing key when ciphertext exists',
    () async {
      final store = MemoryKeyStore();

      await expectLater(
        DatabaseKeyController(store).provisionNewDatabase(databaseExists: true),
        throwsA(isA<MissingDatabaseKeyException>()),
      );
      expect(store.writes, 0);
    },
  );

  test(
    'secure-store unavailability propagates and does not mutate state',
    () async {
      final store = MemoryKeyStore()..readFailure = StateError('unavailable');

      await expectLater(
        DatabaseKeyController(store).loadExisting(),
        throwsStateError,
      );
      expect(store.writes, 0);
    },
  );

  test('malformed key is rejected', () {
    expect(
      () => validateDatabaseKey('not-a-key'),
      throwsA(isA<InvalidDatabaseKeyException>()),
    );
  });
}

final class MemoryKeyStore implements DatabaseKeyStore {
  String? value;
  Object? readFailure;
  int writes = 0;

  @override
  Future<void> delete() async => value = null;

  @override
  Future<String?> read() async {
    if (readFailure case final failure?) {
      throw failure;
    }
    return value;
  }

  @override
  Future<void> write(String keyHex) async {
    writes++;
    value = keyHex;
  }
}
