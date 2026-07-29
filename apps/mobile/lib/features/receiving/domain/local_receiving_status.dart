enum LocalReceivingStatus {
  open('Open'),
  closed('Closed');

  const LocalReceivingStatus(this.storageValue);

  final String storageValue;

  static LocalReceivingStatus fromStorage(String value) {
    return switch (value) {
      'Open' => LocalReceivingStatus.open,
      'Closed' => LocalReceivingStatus.closed,
      _ => throw StateError('Unsupported local receiving status: $value'),
    };
  }
}

enum LocalReceivingEntryStatus {
  active('Active'),
  reversed('Reversed');

  const LocalReceivingEntryStatus(this.storageValue);

  final String storageValue;

  static LocalReceivingEntryStatus fromStorage(String value) {
    return switch (value) {
      'Active' => LocalReceivingEntryStatus.active,
      'Reversed' => LocalReceivingEntryStatus.reversed,
      _ => throw StateError('Unsupported local receiving entry status: $value'),
    };
  }
}
