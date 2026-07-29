import 'weight_processing_method.dart';

final class WeightProcessingResult {
  const WeightProcessingResult({
    required this.rawWeightKg,
    required this.processedWeightKg,
    required this.displayWeightKg,
    required this.decimalPlaces,
    required this.method,
  });

  final String rawWeightKg;
  final String processedWeightKg;
  final String displayWeightKg;
  final int decimalPlaces;
  final WeightProcessingMethod method;
}
