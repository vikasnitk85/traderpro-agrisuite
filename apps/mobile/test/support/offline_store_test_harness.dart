import 'package:traderpro_agrisuite_mobile/core/database/database.dart';
import 'package:traderpro_agrisuite_mobile/core/measurements/weight_processing_method.dart';
import 'package:traderpro_agrisuite_mobile/features/receiving/receiving.dart';

final class FixedLocalDeviceClock implements LocalDeviceClock {
  FixedLocalDeviceClock([DateTime? value])
    : value = value ?? DateTime.utc(2026, 7, 29, 9, 30);

  DateTime value;

  @override
  DateTime nowUtc() => value;
}

final class SequentialLocalIdGenerator implements LocalIdGenerator {
  var _next = 1;

  @override
  String newUuidV7() {
    final suffix = (_next++).toRadixString(16).padLeft(12, '0');
    return '019fad0f-2d6a-7000-8000-$suffix';
  }
}

final class QueuedLocalIdGenerator implements LocalIdGenerator {
  QueuedLocalIdGenerator(List<String> values) : _values = List.of(values);

  final List<String> _values;
  var _next = 0;

  @override
  String newUuidV7() {
    if (_next >= _values.length) {
      throw StateError('No queued local ID remains.');
    }
    return _values[_next++];
  }
}

final class OfflineStoreTestHarness {
  OfflineStoreTestHarness.inMemory()
    : database = LocalDatabaseOpeners.openInMemoryForTest(),
      clock = FixedLocalDeviceClock(),
      idGenerator = SequentialLocalIdGenerator() {
    store = LocalReceivingStore(
      database: database,
      installReference: 'test-01',
      clock: clock,
      idGenerator: idGenerator,
    );
  }

  final TraderProLocalDatabase database;
  final FixedLocalDeviceClock clock;
  final SequentialLocalIdGenerator idGenerator;
  late final LocalReceivingStore store;

  RecordWeightLocallyCommand recordCommand({
    required String sessionId,
    String? operationId,
    String productReference = 'PADDY-01',
    String bagTypeReference = 'JUTE-50',
    int bagCount = 10,
    String rawWeightKg = '50.237',
    int decimalPlaces = 2,
    WeightProcessingMethod processingMethod = WeightProcessingMethod.floor,
    WeightSource weightSource = WeightSource.manualSpike,
    DateTime? capturedAtDeviceUtc,
  }) {
    return RecordWeightLocallyCommand(
      operationId: operationId,
      sessionId: sessionId,
      productReference: productReference,
      bagTypeReference: bagTypeReference,
      bagCount: bagCount,
      rawWeightKg: rawWeightKg,
      decimalPlaces: decimalPlaces,
      processingMethod: processingMethod,
      weightSource: weightSource,
      capturedAtDeviceUtc:
          capturedAtDeviceUtc ?? DateTime.utc(2026, 7, 29, 9, 29),
    );
  }
}
