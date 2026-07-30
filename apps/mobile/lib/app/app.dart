import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import '../features/procurement_poc/application/procurement_poc_feature.dart';

const foundationMessage =
    'Foundation scaffold \u2014 business modules not yet implemented.';

class TraderProAgriSuiteApp extends StatelessWidget {
  const TraderProAgriSuiteApp({
    this.procurementPocFeature = const ProcurementPocFeatureConfiguration(
      compileTimeEnabled: procurementPocCompileTimeEnabled,
      releaseMode: kReleaseMode,
    ),
    this.procurementPocBuilder,
    super.key,
  });

  final ProcurementPocFeatureConfiguration procurementPocFeature;
  final WidgetBuilder? procurementPocBuilder;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'TraderPro AgriSuite',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.green),
        useMaterial3: true,
      ),
      home: FoundationScreen(
        procurementPocAvailable:
            procurementPocFeature.isEnabled && procurementPocBuilder != null,
      ),
      onGenerateRoute: (settings) {
        if (settings.name != procurementPocRoute) {
          return null;
        }
        final builder = procurementPocBuilder;
        if (!procurementPocFeature.isEnabled || builder == null) {
          return MaterialPageRoute<void>(
            settings: settings,
            builder: (_) =>
                const FoundationScreen(procurementPocAvailable: false),
          );
        }
        return MaterialPageRoute<void>(settings: settings, builder: builder);
      },
    );
  }
}

class FoundationScreen extends StatelessWidget {
  const FoundationScreen({this.procurementPocAvailable = false, super.key});

  final bool procurementPocAvailable;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text(foundationMessage, textAlign: TextAlign.center),
                if (procurementPocAvailable) ...[
                  const SizedBox(height: 24),
                  FilledButton.tonalIcon(
                    key: const Key('open-procurement-poc'),
                    onPressed: () =>
                        Navigator.of(context).pushNamed(procurementPocRoute),
                    icon: const Icon(Icons.science_outlined),
                    label: const Text('Development Only: Procurement POC'),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
