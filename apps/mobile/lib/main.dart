import 'package:flutter/material.dart';
import 'package:path_provider/path_provider.dart';
import 'package:traderpro_agrisuite_mobile/app/app.dart';
import 'package:traderpro_agrisuite_mobile/infrastructure/runtime/commercial_secure_runtime.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final supportDirectory = await getApplicationSupportDirectory();
  final runtime = CommercialSecureRuntime.production(
    applicationSupportDirectory: supportDirectory,
  );
  final startupState = await runtime.open();
  runApp(TraderProAgriSuiteApp(startupState: startupState));
}
