final class WeightProcessingException implements Exception {
  const WeightProcessingException(this.errorCode, this.message);

  static const rawRequired = 'WEIGHT_RAW_REQUIRED';
  static const rawInvalid = 'WEIGHT_RAW_INVALID';
  static const rawNegative = 'WEIGHT_RAW_NEGATIVE';
  static const rawIntegerDigitsExceeded = 'WEIGHT_RAW_INTEGER_DIGITS_EXCEEDED';
  static const rawFractionDigitsExceeded =
      'WEIGHT_RAW_FRACTION_DIGITS_EXCEEDED';
  static const decimalPlacesInvalid = 'WEIGHT_DECIMAL_PLACES_INVALID';
  static const processingMethodInvalid = 'WEIGHT_PROCESSING_METHOD_INVALID';

  final String errorCode;
  final String message;

  @override
  String toString() => 'WeightProcessingException($errorCode): $message';
}
