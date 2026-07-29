import 'weight_processing_exception.dart';
import 'weight_processing_method.dart';
import 'weight_processing_policy.dart';
import 'weight_processing_result.dart';

abstract final class WeightProcessor {
  static const _storageDecimalPlaces = 6;
  static const _maximumIntegerDigits = 14;
  static const _maximumFractionDigits = 6;
  static final BigInt _storageScale = BigInt.from(
    10,
  ).pow(_storageDecimalPlaces);

  static WeightProcessingResult process({
    required String rawWeightKg,
    required int decimalPlaces,
    required String method,
  }) {
    return processWithPolicy(
      rawWeightKg: rawWeightKg,
      policy: WeightProcessingPolicy.parse(
        decimalPlaces: decimalPlaces,
        method: method,
      ),
    );
  }

  static WeightProcessingResult processWithPolicy({
    required String rawWeightKg,
    required WeightProcessingPolicy policy,
  }) {
    final rawScaledWeight = _parseRawWeight(rawWeightKg);
    final quantum = BigInt.from(
      10,
    ).pow(_storageDecimalPlaces - policy.decimalPlaces);
    var units = rawScaledWeight ~/ quantum;
    final remainder = rawScaledWeight.remainder(quantum);

    switch (policy.method) {
      case WeightProcessingMethod.standard:
        if (remainder * BigInt.two >= quantum) {
          units += BigInt.one;
        }
      case WeightProcessingMethod.floor:
        break;
      case WeightProcessingMethod.ceiling:
        if (remainder != BigInt.zero) {
          units += BigInt.one;
        }
    }

    final processedScaledWeight = units * quantum;

    return WeightProcessingResult(
      rawWeightKg: rawWeightKg,
      processedWeightKg: _formatWeight(
        processedScaledWeight,
        _storageDecimalPlaces,
      ),
      displayWeightKg: _formatWeight(
        processedScaledWeight,
        policy.decimalPlaces,
      ),
      decimalPlaces: policy.decimalPlaces,
      method: policy.method,
    );
  }

  static BigInt _parseRawWeight(String rawWeightKg) {
    if (rawWeightKg.trim().isEmpty) {
      throw const WeightProcessingException(
        WeightProcessingException.rawRequired,
        'Raw weight is required.',
      );
    }

    if (rawWeightKg.startsWith('-')) {
      throw const WeightProcessingException(
        WeightProcessingException.rawNegative,
        'Raw weight cannot be negative.',
      );
    }

    final decimalSeparatorIndex = rawWeightKg.indexOf('.');
    if (decimalSeparatorIndex == 0 ||
        decimalSeparatorIndex == rawWeightKg.length - 1 ||
        decimalSeparatorIndex != rawWeightKg.lastIndexOf('.')) {
      throw _invalidRawWeight();
    }

    for (var index = 0; index < rawWeightKg.length; index++) {
      if (index == decimalSeparatorIndex) {
        continue;
      }

      final codeUnit = rawWeightKg.codeUnitAt(index);
      if (codeUnit < 0x30 || codeUnit > 0x39) {
        throw _invalidRawWeight();
      }
    }

    final integerDigits = decimalSeparatorIndex < 0
        ? rawWeightKg.length
        : decimalSeparatorIndex;
    final fractionDigits = decimalSeparatorIndex < 0
        ? 0
        : rawWeightKg.length - decimalSeparatorIndex - 1;

    if (integerDigits > _maximumIntegerDigits) {
      throw const WeightProcessingException(
        WeightProcessingException.rawIntegerDigitsExceeded,
        'Raw weight cannot have more than 14 integer digits.',
      );
    }

    if (fractionDigits > _maximumFractionDigits) {
      throw const WeightProcessingException(
        WeightProcessingException.rawFractionDigitsExceeded,
        'Raw weight cannot have more than 6 fractional digits.',
      );
    }

    final integerPart = BigInt.parse(rawWeightKg.substring(0, integerDigits));
    final fractionalPart = fractionDigits == 0
        ? BigInt.zero
        : BigInt.parse(rawWeightKg.substring(decimalSeparatorIndex + 1)) *
              BigInt.from(10).pow(_storageDecimalPlaces - fractionDigits);

    return integerPart * _storageScale + fractionalPart;
  }

  static String _formatWeight(BigInt scaledWeight, int decimalPlaces) {
    final integerPart = scaledWeight ~/ _storageScale;
    final sixDigitFraction = scaledWeight.remainder(_storageScale);
    final displayDivisor = BigInt.from(
      10,
    ).pow(_storageDecimalPlaces - decimalPlaces);
    final displayedFraction = sixDigitFraction ~/ displayDivisor;

    return '${integerPart.toString()}.'
        '${displayedFraction.toString().padLeft(decimalPlaces, '0')}';
  }

  static WeightProcessingException _invalidRawWeight() {
    return const WeightProcessingException(
      WeightProcessingException.rawInvalid,
      'Raw weight must use unsigned decimal notation with a dot separator.',
    );
  }
}
