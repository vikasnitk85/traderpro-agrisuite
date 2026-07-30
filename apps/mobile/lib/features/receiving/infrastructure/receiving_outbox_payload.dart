import 'dart:convert';

import 'package:crypto/crypto.dart';

abstract final class ReceivingOutboxPayload {
  static String startSession({
    required String operationId,
    required String localSessionId,
    required String temporaryReference,
    required String createdAtDeviceUtc,
  }) {
    return jsonEncode(<String, Object?>{
      'operationId': operationId,
      'localSessionId': localSessionId,
      'temporaryReference': temporaryReference,
      'createdAtDeviceUtc': createdAtDeviceUtc,
    });
  }

  static String recordEntry({
    required String operationId,
    required String localSessionId,
    required String? cloudSessionId,
    required int localSequence,
    required String productReference,
    required String bagTypeReference,
    required int bagCount,
    required String rawWeightKg,
    required String processedWeightKg,
    required String displayWeightKg,
    required int decimalPlaces,
    required String processingMethod,
    required String weightSource,
    required String capturedAtDeviceUtc,
  }) {
    return jsonEncode(<String, Object?>{
      'operationId': operationId,
      'localSessionId': localSessionId,
      'cloudSessionId': cloudSessionId,
      'localSequence': localSequence,
      'productReference': productReference,
      'bagTypeReference': bagTypeReference,
      'bagCount': bagCount,
      'rawWeightKg': rawWeightKg,
      'processedWeightKg': processedWeightKg,
      'displayWeightKg': displayWeightKg,
      'decimalPlaces': decimalPlaces,
      'processingMethod': processingMethod,
      'weightSource': weightSource,
      'capturedAtDeviceUtc': capturedAtDeviceUtc,
    });
  }

  static String submitSession({
    required String operationId,
    required String localSessionId,
  }) {
    return jsonEncode(<String, Object?>{
      'operationId': operationId,
      'localSessionId': localSessionId,
    });
  }

  static String hash(String payloadJson) {
    return sha256.convert(utf8.encode(payloadJson)).toString();
  }
}
