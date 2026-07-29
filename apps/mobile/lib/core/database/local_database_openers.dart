import 'dart:io';

import 'package:drift/native.dart';
import 'package:path/path.dart' as path;
import 'package:path_provider/path_provider.dart';

import 'trader_pro_local_database.dart';

abstract final class LocalDatabaseOpeners {
  static Future<TraderProLocalDatabase> openForMobile({
    String fileName = 'traderpro-local.sqlite',
  }) async {
    final supportDirectory = await getApplicationSupportDirectory();
    await supportDirectory.create(recursive: true);
    final databaseFile = File(path.join(supportDirectory.path, fileName));
    return TraderProLocalDatabase(
      NativeDatabase.createInBackground(databaseFile),
    );
  }

  static TraderProLocalDatabase openInMemoryForTest() {
    return TraderProLocalDatabase(NativeDatabase.memory());
  }

  static TraderProLocalDatabase openFileForTest(File databaseFile) {
    return TraderProLocalDatabase(NativeDatabase(databaseFile));
  }
}
