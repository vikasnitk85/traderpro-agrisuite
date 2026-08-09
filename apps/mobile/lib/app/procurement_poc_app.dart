import 'package:flutter/foundation.dart' show kReleaseMode;
import 'package:flutter/material.dart';

import '../features/procurement_poc/procurement_poc.dart';
import 'app.dart';
import 'procurement_poc_runtime.dart';

final class ProcurementPocApp extends StatelessWidget {
  const ProcurementPocApp({
    required this.runtime,
    required this.feature,
    super.key,
  });

  final ProcurementPocRuntime runtime;
  final ProcurementPocFeatureConfiguration feature;

  @override
  Widget build(BuildContext context) {
    if (kReleaseMode || !feature.isEnabled) {
      return const TraderProAgriSuiteApp();
    }
    return ProcurementPocRuntimeHost(
      runtime: runtime,
      child: TraderProAgriSuiteApp(
        developmentRouteName: procurementPocRoute,
        developmentActionLabel: 'Development Only: Procurement POC',
        developmentRouteBuilder: (_) =>
            ProcurementPocHome(controller: runtime.controller),
      ),
    );
  }
}
