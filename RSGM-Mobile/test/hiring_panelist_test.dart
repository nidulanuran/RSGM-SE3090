import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/screens/panelist/panelist_home_screen.dart';
import 'package:rsgm_mobile/services/api_client.dart';
import 'package:rsgm_mobile/services/panelist/panelist_interview_service.dart';
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

Map<String, dynamic> interview({
  required String id,
  required String candidate,
  required String job,
  required DateTime scheduledAt,
  String status = 'Scheduled',
  String? location,
  Map<String, dynamic>? feedback,
}) {
  return {
    'id': id,
    'candidate': candidate,
    'candidateEmail': '${candidate.toLowerCase().replaceAll(' ', '.')}@example.com',
    'job': job,
    'type': 'Technical',
    'scheduledAt': scheduledAt.toIso8601String(),
    'status': status,
    'locationOrLink': location,
    'feedback': feedback,
  };
}

void main() {
  PanelistInterviewService serviceWith(
    Future<http.Response> Function(http.Request request) handler,
  ) {
    return PanelistInterviewService(
      apiClient: ApiClient(
        httpClient: _MockHttpClient(handler),
        overrideToken: 'test-token',
      ),
    );
  }

  Widget buildScreen(PanelistInterviewService service) => MaterialApp(
        theme: AppTheme.light(),
        home: Scaffold(body: PanelistHomeScreen(service: service)),
      );

  group('Hiring Panelist mobile tests', () {
    testWidgets('1. renders role heading, counts, and the nearest interview',
        (tester) async {
      final now = DateTime.now();
      final data = [
        interview(
          id: 'i2',
          candidate: 'Kasun Silva',
          job: 'Backend Engineer',
          scheduledAt: now.add(const Duration(days: 3)),
          location: 'Google Meet',
        ),
        interview(
          id: 'i1',
          candidate: 'Ayesha Fernando',
          job: 'ML Engineer',
          scheduledAt: now.add(const Duration(days: 1)),
          location: 'Meeting Room A',
        ),
      ];

      final service = serviceWith(
        (_) async => http.Response(
          jsonEncode(data),
          200,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      expect(find.text('HIRING PANELIST'), findsOneWidget);
      expect(find.text('Your interview schedule'), findsOneWidget);
      expect(find.text('Upcoming interviews'), findsOneWidget);
      expect(find.text('2'), findsOneWidget);
      expect(find.text('Ayesha Fernando'), findsOneWidget);
      expect(find.text('ML Engineer'), findsOneWidget);
      expect(find.text('Meeting Room A'), findsOneWidget);
    });

    testWidgets('2. cancelled future interviews are excluded from upcoming count',
        (tester) async {
      final now = DateTime.now();
      final data = [
        interview(
          id: 'i1',
          candidate: 'Active Candidate',
          job: 'Developer',
          scheduledAt: now.add(const Duration(days: 1)),
        ),
        interview(
          id: 'i2',
          candidate: 'Cancelled Candidate',
          job: 'Developer',
          scheduledAt: now.add(const Duration(days: 2)),
          status: 'Cancelled',
        ),
      ];

      final service = serviceWith(
        (_) async => http.Response(
          jsonEncode(data),
          200,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      expect(find.text('Upcoming interviews'), findsOneWidget);
      expect(find.text('1'), findsOneWidget);
      expect(find.text('Active Candidate'), findsOneWidget);
      expect(find.text('Cancelled Candidate'), findsNothing);
    });

    testWidgets('3. counts interviews that already contain feedback',
        (tester) async {
      final now = DateTime.now();
      final feedback = {
        'technicalSkills': 4,
        'problemSolving': 4,
        'communication': 5,
        'cultureFit': 4,
        'recommendation': 'Hire',
        'comments': 'Strong candidate',
      };
      final data = [
        interview(
          id: 'i1',
          candidate: 'Candidate One',
          job: 'Engineer',
          scheduledAt: now.subtract(const Duration(days: 2)),
          status: 'Completed',
          feedback: feedback,
        ),
        interview(
          id: 'i2',
          candidate: 'Candidate Two',
          job: 'Engineer',
          scheduledAt: now.subtract(const Duration(days: 1)),
          status: 'Completed',
          feedback: feedback,
        ),
      ];

      final service = serviceWith(
        (_) async => http.Response(
          jsonEncode(data),
          200,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      expect(find.text('Feedback submitted'), findsOneWidget);
      expect(find.text('2'), findsOneWidget);
      expect(find.text('No upcoming interviews.'), findsOneWidget);
    });

    testWidgets('4. shows an empty upcoming state when no interviews exist',
        (tester) async {
      final service = serviceWith(
        (_) async => http.Response(
          jsonEncode(<dynamic>[]),
          200,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      expect(find.text('0'), findsNWidgets(2));
      expect(find.text('Next up'), findsOneWidget);
      expect(find.text('No upcoming interviews.'), findsOneWidget);
    });

    testWidgets('5. shows API error and retry loads the interview dashboard',
        (tester) async {
      var attempts = 0;
      final now = DateTime.now();
      final service = serviceWith((_) async {
        attempts++;
        if (attempts == 1) {
          return http.Response(
            jsonEncode({'message': 'Unable to load panelist interviews'}),
            500,
            headers: {'content-type': 'application/json'},
          );
        }
        return http.Response(
          jsonEncode([
            interview(
              id: 'i1',
              candidate: 'Retry Candidate',
              job: 'Software Engineer',
              scheduledAt: now.add(const Duration(days: 1)),
            ),
          ]),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      expect(find.text('Unable to load panelist interviews'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);

      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();

      expect(attempts, 2);
      expect(find.text('Retry Candidate'), findsOneWidget);
      expect(find.text('Software Engineer'), findsOneWidget);
    });
  });
}
