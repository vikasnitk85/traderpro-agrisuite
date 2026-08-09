import 'dart:convert';
import 'dart:math';
import 'dart:typed_data';

import 'commercial_storage_failure.dart';

abstract interface class DatabaseKeyGenerator {
  Uint8List generate32Bytes();
}

final class SecureRandomDatabaseKeyGenerator implements DatabaseKeyGenerator {
  SecureRandomDatabaseKeyGenerator({Random? random})
    : _random = random ?? Random.secure();

  final Random _random;

  @override
  Uint8List generate32Bytes() => Uint8List.fromList(
    List<int>.generate(
      CommercialDatabaseKeyMaterial.byteLength,
      (_) => _random.nextInt(256),
      growable: false,
    ),
  );
}

final class CommercialDatabaseKeyMaterial {
  CommercialDatabaseKeyMaterial._(this._bytes);

  factory CommercialDatabaseKeyMaterial.copyFrom(List<int> bytes) {
    if (bytes.length != byteLength || bytes.any((value) => value > 255)) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.secureStoreMalformed,
        phase: 'key-validation',
      );
    }
    return CommercialDatabaseKeyMaterial._(Uint8List.fromList(bytes));
  }

  factory CommercialDatabaseKeyMaterial.generate(
    DatabaseKeyGenerator generator,
  ) {
    final generated = generator.generate32Bytes();
    try {
      return CommercialDatabaseKeyMaterial.copyFrom(generated);
    } finally {
      generated.fillRange(0, generated.length, 0);
    }
  }

  static const int byteLength = 32;

  Uint8List? _bytes;

  bool get isDisposed => _bytes == null;

  R useBytes<R>(R Function(Uint8List bytes) operation) {
    final source = _bytes;
    if (source == null) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.databaseKeyUnavailable,
        phase: 'key-disposed',
      );
    }
    final temporary = Uint8List.fromList(source);
    try {
      return operation(temporary);
    } finally {
      temporary.fillRange(0, temporary.length, 0);
    }
  }

  void dispose() {
    final bytes = _bytes;
    if (bytes == null) {
      return;
    }
    bytes.fillRange(0, bytes.length, 0);
    _bytes = null;
  }

  @override
  String toString() => 'CommercialDatabaseKeyMaterial(redacted)';
}

final class CommercialDatabaseKeyCodec {
  const CommercialDatabaseKeyCodec();

  static const String _prefix = 'v1:';
  static final RegExp _payloadPattern = RegExp(r'^[A-Za-z0-9_-]{43}$');

  String encode(CommercialDatabaseKeyMaterial key) => key.useBytes((bytes) {
    final payload = base64Url.encode(bytes).replaceAll('=', '');
    return '$_prefix$payload';
  });

  CommercialDatabaseKeyMaterial decode(String serialized) {
    if (!serialized.startsWith(_prefix)) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.secureStoreMalformed,
        phase: 'key-version',
      );
    }
    final payload = serialized.substring(_prefix.length);
    if (!_payloadPattern.hasMatch(payload)) {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.secureStoreMalformed,
        phase: 'key-encoding',
      );
    }
    try {
      final bytes = base64Url.decode('$payload=');
      return CommercialDatabaseKeyMaterial.copyFrom(bytes);
    } on FormatException {
      throw const CommercialStorageException(
        CommercialStorageFailureCode.secureStoreMalformed,
        phase: 'key-decoding',
      );
    }
  }
}
