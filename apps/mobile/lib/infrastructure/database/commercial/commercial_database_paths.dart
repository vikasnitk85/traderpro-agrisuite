import 'dart:io';

import 'package:path/path.dart' as path;

final class CommercialDatabasePaths {
  CommercialDatabasePaths._({
    required this.directory,
    required this.finalDatabase,
    required this.initializingDatabase,
    required this.provisioningMarker,
  });

  factory CommercialDatabasePaths.fromApplicationSupport(
    Directory applicationSupport,
  ) {
    final directory = Directory(
      path.join(applicationSupport.path, 'commercial'),
    );
    return CommercialDatabasePaths._(
      directory: directory,
      finalDatabase: File(
        path.join(directory.path, 'traderpro-commercial-v1.sqlite3'),
      ),
      initializingDatabase: File(
        path.join(
          directory.path,
          'traderpro-commercial-v1.initializing.sqlite3',
        ),
      ),
      provisioningMarker: File(
        path.join(directory.path, 'traderpro-commercial-v1.provisioning.json'),
      ),
    );
  }

  final Directory directory;
  final File finalDatabase;
  final File initializingDatabase;
  final File provisioningMarker;

  Iterable<File> sidecarsFor(File database) sync* {
    yield File('${database.path}-wal');
    yield File('${database.path}-shm');
    yield File('${database.path}-journal');
  }
}
