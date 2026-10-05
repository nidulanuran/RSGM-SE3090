import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/screens/recruiter/recruiter_availability_screen.dart';
import 'package:rsgm_mobile/services/api_client.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_availability_service.dart';
import 'package:rsgm_mobile/theme/app_theme.dart';

class MockHttpClient extends http.BaseClient {
  MockHttpClient(this._handler);
  final Future<http.Response> Function(http.Request request) _handler;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final httpRequest = request as http.Request;
    final response = await _handler(httpRequest);
    return http.StreamedResponse(
      Stream.value(response.bodyBytes),
      response.statusCode,
      headers: response.headers,
    );
  }
}

void main() {
  Widget buildTestable(Widget child) {
    return MaterialApp(
      theme: AppTheme.light(),
      home: Scaffold(body: child),
    );
  }

  group('RecruiterAvailabilityScreen widget tests', () {
    testWidgets('renders empty state when no busy times exist', (tester) async {
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode([]), 200, headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterAvailabilityService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterAvailabilityScreen(service: service)));
      await tester.pumpAndSettle();

      expect(find.text('Availability'), findsOneWidget);
      expect(find.text("You're currently available"), findsOneWidget);
      expect(find.text('No unavailable periods have been added. You will be considered available for scheduled recruitment activities.'), findsOneWidget);
      expect(find.text('Add unavailable time'), findsOneWidget);
    });

    testWidgets('renders busy times list with title, dates, and description', (tester) async {
      final mock = MockHttpClient((req) async {
        return http.Response(
          jsonEncode([
            {
              'id': 'bt-1',
              'title': 'Dentist Checkup',
              'description': 'Out of office for medical reasons',
              'startsAt': '2026-10-10T09:00:00.000Z',
              'endsAt': '2026-10-10T11:00:00.000Z',
              'cancelledInterviews': 1,
            }
          ]),
          200,
          headers: {'content-type': 'application/json'},
        );
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterAvailabilityService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterAvailabilityScreen(service: service)));
      await tester.pumpAndSettle();

      expect(find.text('Dentist Checkup'), findsOneWidget);
      expect(find.text('Out of office for medical reasons'), findsOneWidget);
      expect(find.text('1 conflicting interview(s) cancelled'), findsOneWidget);
      expect(find.byIcon(Icons.delete_outline_rounded), findsOneWidget);
    });

    testWidgets('opens Add Unavailable Time modal bottom sheet on CTA tap', (tester) async {
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode([]), 200, headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterAvailabilityService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterAvailabilityScreen(service: service)));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Add unavailable time'));
      await tester.pumpAndSettle();

      expect(find.text('Add Unavailable Time'), findsOneWidget);
      expect(find.text('Title *'), findsOneWidget);
      expect(find.text('Start Time *'), findsOneWidget);
      expect(find.text('End Time *'), findsOneWidget);
      expect(find.text('Save unavailable period'), findsOneWidget);
    });

    testWidgets('shows validation error when title is empty upon submit', (tester) async {
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode([]), 200, headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterAvailabilityService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterAvailabilityScreen(service: service)));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Add unavailable time'));
      await tester.pumpAndSettle();

      // Ensure submit button is scrolled into view and tap it
      await tester.ensureVisible(find.text('Save unavailable period'));
      await tester.tap(find.text('Save unavailable period'));
      await tester.pumpAndSettle();

      expect(find.text('Title is required.'), findsOneWidget);
    });

    testWidgets('shows confirmation dialog when delete button is tapped', (tester) async {
      final mock = MockHttpClient((req) async {
        return http.Response(
          jsonEncode([
            {
              'id': 'bt-1',
              'title': 'Board Meeting',
              'startsAt': '2026-10-12T10:00:00.000Z',
              'endsAt': '2026-10-12T12:00:00.000Z',
            }
          ]),
          200,
          headers: {'content-type': 'application/json'},
        );
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterAvailabilityService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterAvailabilityScreen(service: service)));
      await tester.pumpAndSettle();

      // Tap delete
      await tester.tap(find.byIcon(Icons.delete_outline_rounded));
      await tester.pumpAndSettle();

      expect(find.text('Delete unavailable time?'), findsOneWidget);
      expect(find.text('Are you sure you want to remove "Board Meeting" from your unavailable times?'), findsOneWidget);

      // Tap Cancel
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();

      expect(find.text('Delete unavailable time?'), findsNothing);
      expect(find.text('Board Meeting'), findsOneWidget);
    });
  });
}
