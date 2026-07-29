enum OutboxOperationStatus {
  pending('Pending'),
  sending('Sending'),
  accepted('Accepted'),
  needsAttention('NeedsAttention'),
  rejected('Rejected'),
  superseded('Superseded');

  const OutboxOperationStatus(this.storageValue);

  final String storageValue;

  static OutboxOperationStatus fromStorage(String value) {
    return switch (value) {
      'Pending' => OutboxOperationStatus.pending,
      'Sending' => OutboxOperationStatus.sending,
      'Accepted' => OutboxOperationStatus.accepted,
      'NeedsAttention' => OutboxOperationStatus.needsAttention,
      'Rejected' => OutboxOperationStatus.rejected,
      'Superseded' => OutboxOperationStatus.superseded,
      _ => throw StateError('Unsupported outbox status: $value'),
    };
  }
}
