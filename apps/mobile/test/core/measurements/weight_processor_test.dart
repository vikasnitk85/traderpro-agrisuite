import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processing_exception.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processor.dart';

void main() {
  test('preserves the exact raw text', () {
    final result = WeightProcessor.process(
      rawWeightKg: '00050.2300',
      decimalPlaces: 2,
      method: 'Ceiling',
    );

    expect(result.rawWeightKg, '00050.2300');
    expect(result.processedWeightKg, '50.230000');
  });

  test('Standard uses half-up at an exact midpoint', () {
    final result = WeightProcessor.process(
      rawWeightKg: '50.25',
      decimalPlaces: 1,
      method: 'Standard',
    );

    expect(result.processedWeightKg, '50.300000');
  });

  test('Ceiling does not increase an exact value', () {
    final result = WeightProcessor.process(
      rawWeightKg: '50.2300',
      decimalPlaces: 2,
      method: 'Ceiling',
    );

    expect(result.processedWeightKg, '50.230000');
  });

  test('Standard can carry into the integer part', () {
    final result = WeightProcessor.process(
      rawWeightKg: '99.999',
      decimalPlaces: 2,
      method: 'Standard',
    );

    expect(result.processedWeightKg, '100.000000');
    expect(result.displayWeightKg, '100.00');
  });

  test('Ceiling processes the smallest supported positive value', () {
    final result = WeightProcessor.process(
      rawWeightKg: '0.000001',
      decimalPlaces: 3,
      method: 'Ceiling',
    );

    expect(result.processedWeightKg, '0.001000');
  });

  test('maximum supported input is processed without overflow', () {
    final result = WeightProcessor.process(
      rawWeightKg: '99999999999999.999999',
      decimalPlaces: 3,
      method: 'Ceiling',
    );

    expect(result.processedWeightKg, '100000000000000.000000');
  });

  test('invalid decimal syntax exposes a stable error code', () {
    expect(
      () => WeightProcessor.process(
        rawWeightKg: '50,2',
        decimalPlaces: 2,
        method: 'Standard',
      ),
      throwsA(
        isA<WeightProcessingException>().having(
          (exception) => exception.errorCode,
          'errorCode',
          WeightProcessingException.rawInvalid,
        ),
      ),
    );
  });
}
