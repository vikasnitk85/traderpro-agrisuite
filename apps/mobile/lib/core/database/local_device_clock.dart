abstract interface class LocalDeviceClock {
  DateTime nowUtc();
}

final class SystemLocalDeviceClock implements LocalDeviceClock {
  const SystemLocalDeviceClock();

  @override
  DateTime nowUtc() => DateTime.now().toUtc();
}
