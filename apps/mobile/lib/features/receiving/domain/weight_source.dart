enum WeightSource {
  manualSpike('ManualSpike'),
  testScale('TestScale');

  const WeightSource(this.storageValue);

  final String storageValue;
}
