import 'package:uuid/uuid.dart';

abstract interface class LocalIdGenerator {
  String newUuidV7();
}

final class UuidV7LocalIdGenerator implements LocalIdGenerator {
  UuidV7LocalIdGenerator({Uuid? uuid}) : _uuid = uuid ?? const Uuid();

  final Uuid _uuid;

  @override
  String newUuidV7() => _uuid.v7();
}
