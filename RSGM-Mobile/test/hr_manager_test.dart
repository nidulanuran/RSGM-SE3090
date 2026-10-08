import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/screens/hr/hr_dashboard_screen.dart';
import 'package:rsgm_mobile/services/api_client.dart';
import 'package:rsgm_mobile/services/hr/hr_service.dart';
import 'package:rsgm_mobile/theme/app_theme.dart';

class _MockHttpClient extends http.BaseClient {
  _MockHttpClient(this.handler);

  final Future<http.Response> Function(http.Request request) handler;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final response = await handler(request as http.Request);
    return http.StreamedResponse(
      Stream.value(response.bodyBytes),
      response.statusCode,
      headers: response.headers,
    );
  }
}

const _dashboardJson = {
  'pendingRequisitions': 4,
  'activeJobPostings': 9,
  'candidatesInPipeline': 26,
  'upcomingInterviews': 7,
  'offersAwaitingApproval': 3,
  'hiresThisMonth': 5,
  'generatedAt': '2026-10-05T10:00:00Z',
};

void main() {
  Widget buildScreen(HrService service) => MaterialApp(
        theme: AppTheme.light(),
        home: Scaffold(body: HrDashboardScreen(service: service)),
      );

  HrService serviceWith(
    Future<http.Response> Function(http.Request request) handler,
  ) {
    final client = _MockHttpClient(handler);
    return HrService(
      apiClient: ApiClient(httpClient: client, overrideToken: 'test-token'),
    );
  }

  group('HR Manager mobile tests', () {
    testWidgets('1. renders HR dashboard heading and all statistic labels',
        (tester) async {
      final service = serviceWith(
        (_) async => http.Response(
          jsonEncode(_dashboardJson),
          200,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      expect(find.text('HR MANAGER'), findsOneWidget);
      expect(find.text('HR Manager dashboard'), findsOneWidget);
      expect(find.text('Pending requisitions'), findsOneWidget);
      expect(find.text('Active job postings'), findsOneWidget);
      expect(find.text('Candidates in pipeline'), findsOneWidget);
      expect(find.text('Upcoming interviews'), findsOneWidget);
      expect(find.text('Offers awaiting approval'), findsOneWidget);
      expect(find.text('Hires this month'), findsOneWidget);
    });

    testWidgets('2. displays dashboard values returned by the backend',
        (tester) async {
      final service = serviceWith(
        (_) async => http.Response(
          jsonEncode(_dashboardJson),
          200,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      for (final value in ['4', '9', '26', '7', '3', '5']) {
        expect(find.text(value), findsOneWidget);
      }
    });

    testWidgets('3. safely renders zero values when dashboard fields are missing',
        (tester) async {
      final service = serviceWith(
        (_) async => http.Response(
          jsonEncode(<String, dynamic>{}),
          200,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      expect(find.text('0'), findsNWidgets(6));
      expect(find.text('Pending requisitions'), findsOneWidget);
    });

    testWidgets('4. shows an API error and retry control when loading fails',
        (tester) async {
      final service = serviceWith(
        (_) async => http.Response(
          jsonEncode({'message': 'HR dashboard is temporarily unavailable'}),
          500,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      expect(
        find.text('HR dashboard is temporarily unavailable'),
        findsOneWidget,
      );
      expect(find.text('Try again'), findsOneWidget);
    });

    testWidgets('5. retry reloads the dashboard after a temporary failure',
        (tester) async {
      var attempts = 0;
      final service = serviceWith((_) async {
        attempts++;
        if (attempts == 1) {
          return http.Response(
            jsonEncode({'message': 'Temporary server error'}),
            500,
            headers: {'content-type': 'application/json'},
          );
        }
        return http.Response(
          jsonEncode(_dashboardJson),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();
      expect(find.text('Temporary server error'), findsOneWidget);

      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();

      expect(attempts, 2);
      expect(find.text('26'), findsOneWidget);
      expect(find.text('Candidates in pipeline'), findsOneWidget);
    });
  });
}
