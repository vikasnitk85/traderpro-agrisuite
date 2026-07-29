import 'weight_processing_exception.dart';

enum WeightProcessingMethod {
  standard('Standard'),
  floor('Floor'),
  ceiling('Ceiling');

  const WeightProcessingMethod(this.contractName);

  final String contractName;

  static WeightProcessingMethod parseExact(String method) {
    return switch (method) {
      'Standard' => WeightProcessingMethod.standard,
      'Floor' => WeightProcessingMethod.floor,
      'Ceiling' => WeightProcessingMethod.ceiling,
      _ => throw const WeightProcessingException(
        WeightProcessingException.processingMethodInvalid,
        'The weight-processing method must be exactly Standard, Floor, or Ceiling.',
      ),
    };
  }
}
