abstract final class ProcurementPocErrorPolicy {
  static const handledCodes = <String>{
    'IDEMPOTENCY_PAYLOAD_CONFLICT',
    'IDEMPOTENCY_PREVIOUS_ATTEMPT_FAILED',
    'WORKSPACE_CONTEXT_REQUIRED',
    'DEVICE_CONTEXT_REQUIRED',
    'DEVICE_CONTEXT_INVALID',
    'DEVICE_NOT_FOUND',
    'DEVICE_WORKSPACE_MISMATCH',
    'DEVICE_NOT_ACTIVE',
    'RECEIVING_POC_SESSION_NOT_FOUND',
    'RECEIVING_POC_LEASE_REQUIRED',
    'RECEIVING_POC_LEASE_INVALID',
    'RECEIVING_POC_LEASE_EXPIRED',
    'RECEIVING_POC_EDITOR_DEVICE_MISMATCH',
    'RECEIVING_POC_SEQUENCE_CONFLICT',
    'RECEIVING_POC_SEQUENCE_GAP',
    'RECEIVING_POC_VERSION_CONFLICT',
    'RECEIVING_POC_STATUS_INVALID',
    'RECEIVING_POC_ENTRY_REQUIRED',
    'RECEIVING_POC_WEIGHT_PROCESSING_MISMATCH',
    'RECEIVING_POC_OWNER_DEVICE_REQUIRED',
    'RECEIVING_POC_ALREADY_FINALIZED',
    'RECEIVING_POC_TOTAL_WEIGHT_EXCEEDED',
    'SYNC_OPERATION_PAYLOAD_HASH_INVALID',
    'SYNC_OPERATION_TYPE_UNSUPPORTED',
    'SYNC_OPERATION_BATCH_INVALID',
    'REQUEST_BODY_INVALID',
  };

  static String safeMessage(String code, String fallback) {
    return switch (code) {
      'WORKSPACE_CONTEXT_REQUIRED' ||
      'DEVICE_CONTEXT_REQUIRED' ||
      'DEVICE_CONTEXT_INVALID' ||
      'DEVICE_NOT_FOUND' ||
      'DEVICE_WORKSPACE_MISMATCH' ||
      'DEVICE_NOT_ACTIVE' =>
        'Check the Development Only Workspace and Device IDs. These IDs are '
            'temporary context, not authentication.',
      'RECEIVING_POC_LEASE_REQUIRED' ||
      'RECEIVING_POC_LEASE_INVALID' ||
      'RECEIVING_POC_LEASE_EXPIRED' ||
      'RECEIVING_POC_EDITOR_DEVICE_MISMATCH' =>
        'The editor lease is unavailable, expired, or belongs to another '
            'device. Immutable operations were retained for attention.',
      'RECEIVING_POC_SEQUENCE_CONFLICT' || 'RECEIVING_POC_SEQUENCE_GAP' =>
        'The cloud rejected the local operation order. Inspect the earliest '
            'Needs Attention operation; it was not rebuilt or deleted.',
      'RECEIVING_POC_VERSION_CONFLICT' =>
        'The cloud version is stale. Refresh the live view before creating a '
            'different logical owner command.',
      'RECEIVING_POC_OWNER_DEVICE_REQUIRED' =>
        'Approval and finalization require the configured owner device, '
            'separate from the operator editor.',
      'RECEIVING_POC_ALREADY_FINALIZED' =>
        'The session already has one finalization. Use Retry Same '
            'Finalization only for the persisted command.',
      'RECEIVING_POC_WEIGHT_PROCESSING_MISMATCH' =>
        'The cloud did not accept the captured processing result. The raw '
            'weight and immutable payload were retained.',
      'RECEIVING_POC_TOTAL_WEIGHT_EXCEEDED' =>
        'The exact processed total exceeds the POC cloud limit. The entry '
            'remains an immutable local fact requiring attention.',
      'SYNC_OPERATION_PAYLOAD_HASH_INVALID' || 'IDEMPOTENCY_PAYLOAD_CONFLICT' =>
        'The immutable operation identity does not match its stored payload. '
            'The client will not rewrite or replace it.',
      'IDEMPOTENCY_PREVIOUS_ATTEMPT_FAILED' =>
        'The server cannot replay the previous attempt safely. The persisted '
            'command requires explicit development review.',
      'RECEIVING_POC_SESSION_NOT_FOUND' =>
        'The cloud session was not found in this Workspace.',
      'RECEIVING_POC_STATUS_INVALID' ||
      'RECEIVING_POC_ENTRY_REQUIRED' ||
      'SYNC_OPERATION_TYPE_UNSUPPORTED' ||
      'SYNC_OPERATION_BATCH_INVALID' ||
      'REQUEST_BODY_INVALID' => fallback,
      _ => fallback,
    };
  }
}
