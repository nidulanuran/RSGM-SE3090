import 'package:flutter_test/flutter_test.dart';

import 'package:rsgm_mobile/main.dart';

void main() {
  testWidgets('RSGM app loads home page', (WidgetTester tester) async {
    await tester.pumpWidget(const RsgmApp());

    await tester.pumpAndSettle();

    expect(find.text('RSGM'), findsWidgets);
  });
}