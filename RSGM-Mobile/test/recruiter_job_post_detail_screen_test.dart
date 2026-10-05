import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/models/recruiter/recruiter_job_post.dart';
import 'package:rsgm_mobile/screens/recruiter/recruiter_job_post_detail_screen.dart';
import 'package:rsgm_mobile/services/api_client.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_job_post_service.dart';
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

final _fullJobJson = {
  'id': 'job-detail-1',
  'title': 'Senior Systems Architect',
  'company': 'RSGM Enterprise',
  'location': 'Colombo',
  'employmentType': 'FullTime',
  'workMode': 'Hybrid',
  'experienceLevel': 'Senior',
  'minExperienceYears': 7,
  'minSalary': 450000.0,
  'maxSalary': 700000.0,
  'currency': 'LKR',
  'applicationDeadline': '2026-10-25T00:00:00.000Z',
  'status': 'Published',
  'applicantCount': 18,
  'createdAt': '2026-09-15T10:00:00.000Z',
  'description': 'Architect core cloud infrastructure and event queues.',
  'responsibilities': 'Design robust backend services\nLead architecture reviews',
  'requirements': 'BSc or MSc in Computer Science\nStrong AWS and Kubernetes background',
  'requiredSkills': [
    {'id': 'sk-1', 'name': 'Dart', 'weight': 1.0},
    {'id': 'sk-2', 'name': 'Docker', 'weight': 1.5},
  ],
};

void main() {
  Widget buildTestable(Widget child) {
    return MaterialApp(
      theme: AppTheme.light(),
      home: child,
    );
  }

  group('RecruiterJobPostDetailScreen widget tests', () {
    testWidgets('renders complete job details from service', (tester) async {
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_fullJobJson), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterJobPostService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterJobPostDetailScreen(
        jobId: 'job-detail-1',
        service: service,
      )));
      await tester.pumpAndSettle();

      // AppBar
      expect(find.text('Job Post Details'), findsOneWidget);

      // Header
      expect(find.text('Senior Systems Architect'), findsOneWidget);
      expect(find.text('RSGM Enterprise'), findsOneWidget);
      expect(find.text('Colombo'), findsOneWidget);
      expect(find.text('Published'), findsOneWidget);
      expect(find.text('Full-Time'), findsOneWidget);
      expect(find.text('Hybrid'), findsOneWidget);
      expect(find.text('Senior'), findsOneWidget);

      // Highlights
      expect(find.text('Overview & Compensation'), findsOneWidget);
      expect(find.text('LKR 450,000 – 700,000'), findsOneWidget);
      expect(find.text('7 years required'), findsOneWidget);
      expect(find.text('18 Candidate(s)'), findsOneWidget);
      expect(find.text('25 October 2026'), findsOneWidget);

      // Skills
      expect(find.text('Required Skills (2)'), findsOneWidget);
      expect(find.text('Dart'), findsOneWidget);
      expect(find.text('Docker (1.5x weight)'), findsOneWidget);

      // Sections
      expect(find.text('Job Description'), findsOneWidget);
      expect(find.text('Architect core cloud infrastructure and event queues.'),
          findsOneWidget);
      expect(find.text('Key Responsibilities'), findsOneWidget);
      expect(find.text('Requirements & Qualifications'), findsOneWidget);

      // Read-only indicator
      expect(find.textContaining('Read-only view'), findsOneWidget);
    });

    testWidgets('strictly adheres to read-only constraint (no edit, delete, publish)',
        (tester) async {
      final job = RecruiterJobPost.fromJson(_fullJobJson);

      await tester.pumpWidget(buildTestable(RecruiterJobPostDetailScreen(
        jobId: 'job-detail-1',
        initialJob: job,
      )));
      await tester.pumpAndSettle();

      // Verify no mutation buttons
      expect(find.text('Edit'), findsNothing);
      expect(find.text('Edit Job Post'), findsNothing);
      expect(find.text('Publish'), findsNothing);
      expect(find.text('Unpublish'), findsNothing);
      expect(find.text('Delete'), findsNothing);
      expect(find.text('Save'), findsNothing);
      expect(find.text('Save changes'), findsNothing);
      expect(find.text('Create'), findsNothing);
      expect(find.byIcon(Icons.edit), findsNothing);
      expect(find.byIcon(Icons.delete), findsNothing);
    });

    testWidgets('gracefully omits missing optional fields', (tester) async {
      final minimalJson = {
        'id': 'job-min-1',
        'title': 'Junior QA Engineer',
        'company': 'Test Org',
        'location': 'Jaffna',
        'employmentType': 'Contract',
        'workMode': 'OnSite',
        'experienceLevel': 'Junior',
        'status': 'Draft',
        'applicantCount': 0,
        'createdAt': '2026-09-01T10:00:00.000Z',
        'description': '',
        'responsibilities': '',
        'requirements': '',
        'requiredSkills': [],
      };

      final job = RecruiterJobPost.fromJson(minimalJson);

      await tester.pumpWidget(buildTestable(RecruiterJobPostDetailScreen(
        jobId: 'job-min-1',
        initialJob: job,
      )));
      await tester.pumpAndSettle();

      // Basic fields
      expect(find.text('Junior QA Engineer'), findsOneWidget);
      expect(find.text('Draft'), findsOneWidget);
      expect(find.text('0 Candidate(s)'), findsOneWidget);
      expect(find.text('Salary not disclosed'), findsOneWidget);

      // Missing sections should NOT appear
      expect(find.text('Job Description'), findsNothing);
      expect(find.text('Key Responsibilities'), findsNothing);
      expect(find.text('Requirements & Qualifications'), findsNothing);
      expect(find.textContaining('Required Skills'), findsNothing);
      expect(find.textContaining('Application Deadline'), findsNothing);
    });

    testWidgets('shows error state with retry on fetch failure when no initial job',
        (tester) async {
      int attempts = 0;
      final mock = MockHttpClient((req) async {
        attempts++;
        if (attempts == 1) {
          return http.Response(
            jsonEncode({'message': 'Job posting not found'}),
            404,
            headers: {'content-type': 'application/json'},
          );
        }
        return http.Response(jsonEncode(_fullJobJson), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterJobPostService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterJobPostDetailScreen(
        jobId: 'job-detail-1',
        service: service,
      )));
      await tester.pumpAndSettle();

      expect(find.text('Job posting not found'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);

      // Tap retry
      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();

      expect(find.text('Senior Systems Architect'), findsOneWidget);
    });
  });
}
