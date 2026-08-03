import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:traderpro_7c2a_storage_spike/src/database_key.dart';
import 'package:traderpro_7c2a_storage_spike/src/secure_database_key_store.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('Android secure store persists and deletes a synthetic key', (
    tester,
  ) async {
    final store = FlutterSecureDatabaseKeyStore();
    final key = generateDatabaseKey();
    await store.delete();

    await store.write(key);
    expect(await store.read(), key);

    await store.delete();
    expect(await store.read(), isNull);
  });
}
