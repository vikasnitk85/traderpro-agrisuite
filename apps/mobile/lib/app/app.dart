import 'package:flutter/material.dart';

const foundationMessage =
    'Foundation scaffold — business modules not yet implemented.';

class TraderProAgriSuiteApp extends StatelessWidget {
  const TraderProAgriSuiteApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'TraderPro AgriSuite',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.green),
        useMaterial3: true,
      ),
      home: const FoundationScreen(),
    );
  }
}

class FoundationScreen extends StatelessWidget {
  const FoundationScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: EdgeInsets.all(24),
            child: Text(
              foundationMessage,
              textAlign: TextAlign.center,
            ),
          ),
        ),
      ),
    );
  }
}
