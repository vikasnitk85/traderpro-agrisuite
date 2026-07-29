final class LocalStoreException implements Exception {
  const LocalStoreException(this.errorCode, this.message, [this.cause]);

  static const operationPayloadConflict = 'LOCAL_OPERATION_PAYLOAD_CONFLICT';
  static const sessionNotFound = 'LOCAL_SESSION_NOT_FOUND';
  static const sessionNotEditable = 'LOCAL_SESSION_NOT_EDITABLE';
  static const sequenceConflict = 'LOCAL_SEQUENCE_CONFLICT';
  static const storageFailure = 'LOCAL_STORAGE_FAILURE';
  static const operationNotFound = 'LOCAL_OPERATION_NOT_FOUND';
  static const operationStateConflict = 'LOCAL_OPERATION_STATE_CONFLICT';
  static const invalidInput = 'LOCAL_INPUT_INVALID';
  static const storedTimestampInvalid = 'LOCAL_STORED_TIMESTAMP_INVALID';

  final String errorCode;
  final String message;
  final Object? cause;

  @override
  String toString() => 'LocalStoreException($errorCode): $message';
}
