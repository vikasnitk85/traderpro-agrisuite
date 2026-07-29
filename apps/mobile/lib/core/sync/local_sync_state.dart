final class LocalSyncState {
  const LocalSyncState({
    required this.key,
    required this.eventCursor,
    required this.lastSuccessfulSyncAtUtc,
    required this.updatedAtUtc,
  });

  final String key;
  final String? eventCursor;
  final DateTime? lastSuccessfulSyncAtUtc;
  final DateTime updatedAtUtc;
}
