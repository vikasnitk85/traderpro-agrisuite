import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_database_key_material.dart';
import 'package:traderpro_agrisuite_mobile/core/security/commercial_storage_failure.dart';

void main() {
  test('generator produces and owns exactly 32 copied bytes', () {
    final generated = Uint8List.fromList(List<int>.generate(32, (i) => i));
    final key = CommercialDatabaseKeyMaterial.generate(
      _FixedGenerator(generated),
    );

    expect(generated, everyElement(0));
    expect(key.isDisposed, isFalse);
    expect(key.toString(), 'CommercialDatabaseKeyMaterial(redacted)');
    key.useBytes((bytes) {
      expect(bytes, orderedEquals(List<int>.generate(32, (i) => i)));
      bytes[0] = 255;
    });
    key.useBytes((bytes) => expect(bytes[0], 0));
    key.dispose();
    expect(key.isDisposed, isTrue);
  });

  test('rejects every non-32-byte input', () {
    for (final length in <int>[0, 1, 31, 33, 64]) {
      expect(
        () => CommercialDatabaseKeyMaterial.copyFrom(List.filled(length, 1)),
        throwsA(
          isA<CommercialStorageException>().having(
            (error) => error.code,
            'code',
            CommercialStorageFailureCode.secureStoreMalformed,
          ),
        ),
      );
    }
  });

  test('codec uses versioned base64url and round-trips raw bytes', () {
    const codec = CommercialDatabaseKeyCodec();
    final key = CommercialDatabaseKeyMaterial.copyFrom(
      List<int>.generate(32, (i) => 255 - i),
    );
    final serialized = codec.encode(key);

    expect(serialized, matches(RegExp(r'^v1:[A-Za-z0-9_-]{43}$')));
    expect(serialized, isNot(contains('=')));
    final decoded = codec.decode(serialized);
    decoded.useBytes(
      (bytes) =>
          expect(bytes, orderedEquals(List<int>.generate(32, (i) => 255 - i))),
    );
    decoded.dispose();
    key.dispose();
  });

  test(
    'codec rejects version, alphabet, padding, and decoded-length drift',
    () {
      const codec = CommercialDatabaseKeyCodec();
      for (final value in <String>[
        'v2:${'A' * 43}',
        'v1:${'A' * 42}',
        'v1:${'A' * 43}=',
        'v1:${'A' * 42}+',
      ]) {
        expect(
          () => codec.decode(value),
          throwsA(isA<CommercialStorageException>()),
        );
      }
    },
  );

  test('disposed key cannot be reused', () {
    final key = CommercialDatabaseKeyMaterial.copyFrom(List.filled(32, 7));
    key.dispose();
    expect(
      () => key.useBytes((_) {}),
      throwsA(
        isA<CommercialStorageException>().having(
          (error) => error.code,
          'code',
          CommercialStorageFailureCode.databaseKeyUnavailable,
        ),
      ),
    );
  });
}

final class _FixedGenerator implements DatabaseKeyGenerator {
  _FixedGenerator(this.bytes);

  final Uint8List bytes;

  @override
  Uint8List generate32Bytes() => bytes;
}
