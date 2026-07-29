import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:traderpro_agrisuite_mobile/app/app.dart';

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
  });
}
