import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/screens/jobseeker/jobseeker_dashboard_screen.dart';
import 'package:rsgm_mobile/services/api_client.dart';
import 'package:rsgm_mobile/services/jobseeker/jobseeker_service.dart';
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
  'fullName': 'Vimukthi Perera',
  'totalApplications': 8,
  'activeApplications': 4,
  'shortlistedApplications': 2,
  'interviewApplications': 1,
  'offerApplications': 0,
  'availableJobs': 15,
  'averageMatchScore': 78.4,
  'skillsCount': 6,
  'cvUploaded': true,
  'profileCompleteness': 85,
  'topGapSkills': ['Docker', 'Kubernetes'],
};

void main() {
  JobSeekerService serviceWith(
    Future<http.Response> Function(http.Request request) handler,
  ) {
    return JobSeekerService(
      client: ApiClient(
        httpClient: _MockHttpClient(handler),
        overrideToken: 'test-token',
      ),
    );
  }

  Widget buildScreen(JobSeekerService service, {VoidCallback? onBrowseJobs}) {
    return MaterialApp(
      theme: AppTheme.light(),
      home: Scaffold(
        body: JobSeekerDashboardScreen(
          service: service,
          onBrowseJobs: onBrowseJobs ?? () {},
        ),
      ),
    );
  }

  group('Job Seeker mobile tests', () {
    testWidgets('1. uses Job Seeker as the fallback when full name is empty',
        (tester) async {
      final data = Map<String, dynamic>.from(_dashboardJson)
        ..['fullName'] = ''
        ..['topGapSkills'] = <String>[];

      final service = serviceWith(
        (_) async => http.Response(
          jsonEncode(data),
          200,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.pumpWidget(buildScreen(service));
      await tester.pumpAndSettle();

      expect(find.text('Welcome, Job Seeker'), findsOneWidget);
      expect(find.text('Top skill gaps'), findsNothing);
    });

    testWidgets('2. displays an error and successfully retries dashboard loading',
        (tester) async {
      var attempts = 0;
      final service = serviceWith((_) async {
        attempts++;
        if (attempts == 1) {
          return http.Response(
            jsonEncode({'message': 'Unable to load job seeker dashboard'}),
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

      expect(find.text('Unable to load job seeker dashboard'), findsOneWidget);
      expect(find.text('Retry'), findsOneWidget);

      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();

      expect(attempts, 2);
      expect(find.text('Welcome, Vimukthi Perera'), findsOneWidget);
    });
  });
}
