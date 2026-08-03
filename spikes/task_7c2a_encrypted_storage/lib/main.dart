import 'package:flutter/material.dart';

void main() {
  runApp(const StorageSpikeApp());
}

/// Deliberately isolated evidence shell. It is not connected to TraderPro
/// production navigation, repositories, authentication, or business data.
class StorageSpikeApp extends StatelessWidget {
  const StorageSpikeApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Task 7C2A storage spike',
      home: Scaffold(
        appBar: AppBar(title: const Text('Task 7C2A storage spike')),
        body: const Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'Non-production harness. Run the automated host and Android '
            'integration tests for encrypted Drift and secure-store evidence.',
          ),
        ),
      ),
    );
  }
}
