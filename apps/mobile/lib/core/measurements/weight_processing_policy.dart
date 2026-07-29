import 'weight_processing_exception.dart';
import 'weight_processing_method.dart';

final class WeightProcessingPolicy {
  factory WeightProcessingPolicy({
    required int decimalPlaces,
    required WeightProcessingMethod method,
  }) {
    _validateDecimalPlaces(decimalPlaces);
    return WeightProcessingPolicy._(decimalPlaces, method);
  }

  factory WeightProcessingPolicy.parse({
    required int decimalPlaces,
    required String method,
  }) {
    _validateDecimalPlaces(decimalPlaces);
    return WeightProcessingPolicy._(
      decimalPlaces,
      WeightProcessingMethod.parseExact(method),
    );
  }

  const WeightProcessingPolicy._(this.decimalPlaces, this.method);

  final int decimalPlaces;
  final WeightProcessingMethod method;

  static void _validateDecimalPlaces(int decimalPlaces) {
    if (decimalPlaces < 1 || decimalPlaces > 3) {
      throw const WeightProcessingException(
        WeightProcessingException.decimalPlacesInvalid,
        'Weight decimal places must be exactly 1, 2, or 3.',
      );
    }
  }
}
