import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/app/app.dart';
import 'package:traderpro_agrisuite_mobile/app/commercial_secure_startup_state.dart';

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
    expect(find.byKey(const Key('open-development-route')), findsNothing);
  });

  testWidgets('development route is available only when injected', (
    tester,
  ) async {
    await tester.pumpWidget(
      TraderProAgriSuiteApp(
        developmentRouteName: '/development-test',
        developmentActionLabel: 'Development Only',
        developmentRouteBuilder: (_) =>
            const Scaffold(body: Text('Injected development route')),
      ),
    );
    await tester.tap(find.byKey(const Key('open-development-route')));
    await tester.pumpAndSettle();
    expect(find.text('Injected development route'), findsOneWidget);
  });

  testWidgets('renders only safe Commercial startup state', (tester) async {
    await tester.pumpWidget(
      const TraderProAgriSuiteApp(
        startupState: CommercialSecureStartupState.unavailable(
          'COMMERCIAL_SECURE_STORE_UNAVAILABLE',
        ),
      ),
    );

    expect(
      find.textContaining('COMMERCIAL_SECURE_STORE_UNAVAILABLE'),
      findsOneWidget,
    );
    expect(find.textContaining('native'), findsNothing);
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
