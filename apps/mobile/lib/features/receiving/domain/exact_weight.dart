abstract final class ExactWeight {
  static const zero = '0.000000';
  static final RegExp _canonicalPattern = RegExp(r'^\d+\.\d{6}$');
  static final BigInt _scale = BigInt.from(1000000);

  static String add(String left, String right) {
    return format(parseCanonical(left) + parseCanonical(right));
  }

  static BigInt parseCanonical(String value) {
    if (!_canonicalPattern.hasMatch(value)) {
      throw FormatException('Weight is not canonical six-decimal text.', value);
    }

    final separator = value.length - 7;
    final integerPart = BigInt.parse(value.substring(0, separator));
    final fractionPart = BigInt.parse(value.substring(separator + 1));
    return integerPart * _scale + fractionPart;
  }

  static String format(BigInt scaledValue) {
    if (scaledValue.isNegative) {
      throw ArgumentError.value(
        scaledValue,
        'scaledValue',
        'Weight cannot be negative.',
      );
    }

    final integerPart = scaledValue ~/ _scale;
    final fractionPart = scaledValue.remainder(_scale);
    return '${integerPart.toString()}.'
        '${fractionPart.toString().padLeft(6, '0')}';
  }
}
