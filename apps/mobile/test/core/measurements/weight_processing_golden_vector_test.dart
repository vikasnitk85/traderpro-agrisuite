import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processing_exception.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processor.dart';

void main() {
  final vectors = _loadGoldenVectors();

  test('golden-vector metadata matches the supported contract', () {
    expect(vectors['contract'], 'TraderPro.WeightProcessing');
    expect(vectors['version'], 1);
    expect(vectors['storageDecimalPlaces'], 6);
    expect(
      (vectors['validCases']! as List<Object?>).length,
      greaterThanOrEqualTo(30),
    );
    expect(
      (vectors['invalidCases']! as List<Object?>).length,
      greaterThanOrEqualTo(14),
    );
  });

  group('valid golden vectors', () {
    for (final value in vectors['validCases']! as List<Object?>) {
      final vector = value! as Map<String, Object?>;
      final name = vector['name']! as String;

      test(name, () {
        final rawWeightKg = vector['rawWeightKg']! as String;
        final decimalPlaces = vector['decimalPlaces']! as int;
        final method = vector['method']! as String;
        final result = WeightProcessor.process(
          rawWeightKg: rawWeightKg,
          decimalPlaces: decimalPlaces,
          method: method,
        );

        expect(result.rawWeightKg, rawWeightKg);
        expect(result.processedWeightKg, vector['expectedProcessedWeightKg']);
        expect(result.displayWeightKg, vector['expectedDisplayWeightKg']);
        expect(result.decimalPlaces, decimalPlaces);
        expect(result.method.contractName, method);
      });
    }
  });

  group('invalid golden vectors', () {
    for (final value in vectors['invalidCases']! as List<Object?>) {
      final vector = value! as Map<String, Object?>;
      final name = vector['name']! as String;

      test(name, () {
        expect(
          () => WeightProcessor.process(
            rawWeightKg: vector['rawWeightKg']! as String,
            decimalPlaces: vector['decimalPlaces']! as int,
            method: vector['method']! as String,
          ),
          throwsA(
            isA<WeightProcessingException>().having(
              (exception) => exception.errorCode,
              'errorCode',
              vector['expectedErrorCode'],
            ),
          ),
        );
      });
    }
  });
}

Map<String, Object?> _loadGoldenVectors() {
  final repositoryRoot = _findRepositoryRoot();
  final vectorFile = File(
    '${repositoryRoot.path}${Platform.pathSeparator}'
    'contracts${Platform.pathSeparator}'
    'golden-vectors${Platform.pathSeparator}'
    'weight-processing.v1.json',
  );

  return jsonDecode(vectorFile.readAsStringSync())! as Map<String, Object?>;
}

Directory _findRepositoryRoot() {
  Directory? directory = Directory.current.absolute;

  while (directory != null) {
    final solution = File(
      '${directory.path}${Platform.pathSeparator}TraderPro.sln',
    );
    final vectors = File(
      '${directory.path}${Platform.pathSeparator}'
      'contracts${Platform.pathSeparator}'
      'golden-vectors${Platform.pathSeparator}'
      'weight-processing.v1.json',
    );
    if (solution.existsSync() && vectors.existsSync()) {
      return directory;
    }

    final parent = directory.parent;
    directory = parent.path == directory.path ? null : parent;
  }

  throw FileSystemException(
    'Could not locate the repository root and weight golden vectors.',
    Directory.current.path,
  );
}
