const procurementPocRoute = '/development/procurement-poc';

const procurementPocCompileTimeEnabled = bool.fromEnvironment(
  'TRADERPRO_PROCUREMENT_POC',
);

final class ProcurementPocFeatureConfiguration {
  const ProcurementPocFeatureConfiguration({
    required this.compileTimeEnabled,
    required this.releaseMode,
  });

  final bool compileTimeEnabled;
  final bool releaseMode;

  bool get isEnabled => compileTimeEnabled && !releaseMode;
}
