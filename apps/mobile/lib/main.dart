import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:traderpro_agrisuite_mobile/app/app.dart';
import 'package:traderpro_agrisuite_mobile/app/procurement_poc_runtime.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  const feature = ProcurementPocFeatureConfiguration(
    compileTimeEnabled: procurementPocCompileTimeEnabled,
    releaseMode: kReleaseMode,
  );
  if (!feature.isEnabled) {
    runApp(const TraderProAgriSuiteApp());
    return;
  }

  final runtime = await ProcurementPocRuntime.open(feature: feature);
  await runtime.start();
  runApp(
    ProcurementPocRuntimeHost(
      runtime: runtime,
      child: TraderProAgriSuiteApp(
        procurementPocFeature: feature,
        procurementPocBuilder: (_) =>
            ProcurementPocHome(controller: runtime.controller),
      ),
    ),
  );
}
