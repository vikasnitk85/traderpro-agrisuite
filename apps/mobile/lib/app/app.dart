import 'package:flutter/material.dart';

import 'commercial_identity_screens.dart';
import 'commercial_secure_startup_state.dart';

const foundationMessage =
    'Foundation scaffold \u2014 business modules not yet implemented.';

class TraderProAgriSuiteApp extends StatelessWidget {
  const TraderProAgriSuiteApp({
    this.startupState,
    this.developmentRouteName,
    this.developmentRouteBuilder,
    this.developmentActionLabel,
    super.key,
  });

  final CommercialSecureStartupState? startupState;
  final String? developmentRouteName;
  final WidgetBuilder? developmentRouteBuilder;
  final String? developmentActionLabel;

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
        startupState: startupState,
        developmentRouteName: developmentRouteName,
        developmentActionLabel: developmentActionLabel,
      ),
      onGenerateRoute: (settings) {
        final routeName = developmentRouteName;
        final builder = developmentRouteBuilder;
        if (routeName == null ||
            builder == null ||
            settings.name != routeName) {
          return null;
        }
        return MaterialPageRoute<void>(settings: settings, builder: builder);
      },
    );
  }
}

class FoundationScreen extends StatelessWidget {
  const FoundationScreen({
    this.startupState,
    this.developmentRouteName,
    this.developmentActionLabel,
    super.key,
  });

  final CommercialSecureStartupState? startupState;
  final String? developmentRouteName;
  final String? developmentActionLabel;

  @override
  Widget build(BuildContext context) {
    final identityController = startupState?.identityController;
    if (identityController != null) {
      return CommercialIdentityScreen(controller: identityController);
    }
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(switch (startupState?.status) {
                  CommercialSecureStartupStatus.ready =>
                    'Secure local foundation ready.',
                  CommercialSecureStartupStatus.unavailable =>
                    'Secure local foundation unavailable. '
                        '${startupState!.safeFailureCode}',
                  CommercialSecureStartupStatus.closed =>
                    'Secure local foundation closed.',
                  null => foundationMessage,
                }, textAlign: TextAlign.center),
                if (developmentRouteName != null &&
                    developmentActionLabel != null) ...[
                  const SizedBox(height: 24),
                  FilledButton.tonalIcon(
                    key: const Key('open-development-route'),
                    onPressed: () =>
                        Navigator.of(context).pushNamed(developmentRouteName!),
                    icon: const Icon(Icons.science_outlined),
                    label: Text(developmentActionLabel!),
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
