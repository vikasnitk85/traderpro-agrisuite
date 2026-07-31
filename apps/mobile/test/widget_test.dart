import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/app/app.dart';
import 'package:traderpro_agrisuite_mobile/features/procurement_poc/procurement_poc.dart';

const expectedFoundationMessage =
    'Foundation scaffold \u2014 business modules not yet implemented.';

void main() {
  testWidgets('renders the foundation application shell', (tester) async {
    await tester.pumpWidget(const TraderProAgriSuiteApp());

    final app = tester.widget<MaterialApp>(find.byType(MaterialApp));
    final message = tester.widget<Text>(find.text(expectedFoundationMessage));

    expect(app.title, 'TraderPro AgriSuite');
    expect(find.byType(Scaffold), findsOneWidget);
    expect(foundationMessage, expectedFoundationMessage);
    expect(find.text(expectedFoundationMessage), findsOneWidget);
    expect(message.textAlign, TextAlign.center);
    expect(find.byKey(const Key('open-procurement-poc')), findsNothing);
  });

  testWidgets('POC route is available only when explicitly enabled', (
    tester,
  ) async {
    await tester.pumpWidget(
      TraderProAgriSuiteApp(
        procurementPocFeature: const ProcurementPocFeatureConfiguration(
          compileTimeEnabled: true,
          releaseMode: false,
        ),
        procurementPocBuilder: (_) =>
            const Scaffold(body: Text('Injected POC')),
      ),
    );
    await tester.tap(find.byKey(const Key('open-procurement-poc')));
    await tester.pumpAndSettle();
    expect(find.text('Injected POC'), findsOneWidget);
  });

  testWidgets('disabled configuration fails the POC route closed', (
    tester,
  ) async {
    await tester.pumpWidget(
      TraderProAgriSuiteApp(
        procurementPocFeature: const ProcurementPocFeatureConfiguration(
          compileTimeEnabled: false,
          releaseMode: false,
        ),
        procurementPocBuilder: (_) =>
            const Scaffold(body: Text('Must not render')),
      ),
    );
    expect(find.byKey(const Key('open-procurement-poc')), findsNothing);
    final context = tester.element(find.byType(FoundationScreen));
    unawaited(Navigator.of(context).pushNamed(procurementPocRoute));
    await tester.pumpAndSettle();
    expect(find.text('Must not render'), findsNothing);
    expect(find.text(expectedFoundationMessage), findsOneWidget);
  });

  testWidgets('release configuration fails the POC route closed', (
    tester,
  ) async {
    await tester.pumpWidget(
      TraderProAgriSuiteApp(
        procurementPocFeature: const ProcurementPocFeatureConfiguration(
          compileTimeEnabled: true,
          releaseMode: true,
        ),
        procurementPocBuilder: (_) =>
            const Scaffold(body: Text('Must not render')),
      ),
    );
    expect(find.byKey(const Key('open-procurement-poc')), findsNothing);
    final context = tester.element(find.byType(FoundationScreen));
    unawaited(Navigator.of(context).pushNamed(procurementPocRoute));
    await tester.pumpAndSettle();
    expect(find.text('Must not render'), findsNothing);
    expect(find.text(expectedFoundationMessage), findsOneWidget);
  });

  testWidgets('app composition leaves debug visual flags disabled', (
    tester,
  ) async {
    await tester.pumpWidget(const TraderProAgriSuiteApp());

    final app = tester.widget<MaterialApp>(find.byType(MaterialApp));
    expect(app.showSemanticsDebugger, isFalse);
    expect(debugPaintBaselinesEnabled, isFalse);
    expect(debugPaintSizeEnabled, isFalse);
    expect(debugPaintPointersEnabled, isFalse);
    expect(debugPaintLayerBordersEnabled, isFalse);
  });
}
