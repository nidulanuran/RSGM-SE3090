import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/models/recruiter/recruiter_applicant.dart';
import 'package:rsgm_mobile/screens/recruiter/recruiter_applicant_detail_screen.dart';
import 'package:rsgm_mobile/services/api_client.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_application_service.dart';
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

final _fullApplicantJson = {
  'id': 'app-detail-1',
  'jobPostingId': 'job-1',
  'jobTitle': 'Senior Cloud Architect',
  'candidateId': 'c-10',
  'fullName': 'Nimal Weerasinghe',
  'email': 'nimal@example.com',
  'phoneNumber': '+94 77 123 4567',
  'headline': 'Cloud Solutions Architect & Kubernetes Specialist',
  'location': 'Colombo, Western Province',
  'bio': 'Passionate cloud engineer with 10+ years of distributed systems experience.',
  'linkedInUrl': 'https://linkedin.com/in/nimal-w',
  'gitHubUrl': 'https://github.com/nimal-w',
  'portfolioUrl': 'https://nimal.dev',
  'hasCv': true,
  'cvFileName': 'nimal_resume_2026.pdf',
  'status': 'Shortlisted',
  'appliedAt': '2026-10-01T10:00:00.000Z',
  'matchScore': 92,
  'exactMatchScore': 92.4,
  'matchExplanation': 'Exceptional alignment across Docker, Cloud, and Go requirements.',
  'matchBreakdown': [
    {
      'skillId': 'sk-1',
      'skillName': 'Kubernetes',
      'requiredWeight': 1.5,
      'candidateProficiency': 5,
      'proficiencyLabel': 'Expert',
      'contributionPercentage': 35.0,
      'matched': true,
    },
    {
      'skillId': 'sk-2',
      'skillName': 'Go',
      'requiredWeight': 1.0,
      'candidateProficiency': 4,
      'proficiencyLabel': 'Advanced',
      'contributionPercentage': 25.0,
      'matched': true,
    },
    {
      'skillId': 'sk-3',
      'skillName': 'Terraform',
      'requiredWeight': 1.0,
      'candidateProficiency': 0,
      'proficiencyLabel': null,
      'contributionPercentage': 0.0,
      'matched': false,
    },
  ],
  'skills': ['Kubernetes', 'Go', 'Docker', 'AWS', 'gRPC'],
  'matchedSkills': ['Kubernetes', 'Go'],
  'missingSkills': ['Terraform'],
  'education': [
    'BSc (Hons) in Computer Systems Engineering - University of Moratuwa',
    'MSc in Cloud Architecture - National University of Singapore',
  ],
  'workExperience': [
    'Lead Cloud Architect - Global Tech Lanka (2022 - Present)',
    'Senior DevOps Engineer - FinTech Innovations (2018 - 2022)',
  ],
};

void main() {
  Widget buildTestable(Widget child) {
    return MaterialApp(
      theme: AppTheme.light(),
      home: child,
    );
  }

  void setMobileView(WidgetTester tester) {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
  }

  group('RecruiterApplicantDetailScreen widget tests', () {
    testWidgets('renders complete applicant profile, skills, education, and experience',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_fullApplicantJson), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterApplicantDetailScreen(
        applicantId: 'app-detail-1',
        service: service,
      )));
      await tester.pumpAndSettle();

      // Screen title
      expect(find.text('Applicant Details'), findsOneWidget);

      // Header Card
      expect(find.text('NW'), findsOneWidget);
      expect(find.text('Nimal Weerasinghe'), findsOneWidget);
      expect(find.text('Cloud Solutions Architect & Kubernetes Specialist'),
          findsOneWidget);
      expect(find.text('Colombo, Western Province'), findsOneWidget);
      expect(find.text('Shortlisted'), findsOneWidget);
      expect(find.text('Match: 92%'), findsWidgets);

      // Application Overview
      expect(find.text('Application & Contact Information'), findsOneWidget);
      expect(find.text('Senior Cloud Architect'), findsOneWidget);
      expect(find.text('1 October 2026'), findsOneWidget);
      expect(find.text('nimal@example.com'), findsOneWidget);
      expect(find.text('+94 77 123 4567'), findsOneWidget);
      expect(find.text('CV on file (nimal_resume_2026.pdf)'), findsOneWidget);
      expect(find.text('https://linkedin.com/in/nimal-w'), findsOneWidget);
      expect(find.text('https://github.com/nimal-w'), findsOneWidget);
      expect(find.text('https://nimal.dev'), findsOneWidget);

      // Match Information
      expect(find.text('Skill Match Information'), findsOneWidget);
      expect(find.text('92%'), findsWidgets);
      expect(find.text('92.4%'), findsOneWidget);
      expect(
          find.text(
              'Exceptional alignment across Docker, Cloud, and Go requirements.'),
          findsOneWidget);
      expect(find.text('Kubernetes'), findsWidgets);
      expect(find.text('Expert'), findsOneWidget);
      expect(find.text('+35.0%'), findsOneWidget);

      // Matched & Missing Skills
      expect(find.text('Matched Skills (2)'), findsOneWidget);
      expect(find.text('Missing Skills (1)'), findsOneWidget);
      expect(find.text('Terraform'), findsWidgets);

      // Education & Work Experience
      expect(find.text('Education'), findsOneWidget);
      expect(
        find.text(
            'BSc (Hons) in Computer Systems Engineering - University of Moratuwa'),
        findsOneWidget,
      );
      expect(find.text('Work Experience'), findsOneWidget);
      expect(
        find.text('Lead Cloud Architect - Global Tech Lanka (2022 - Present)'),
        findsOneWidget,
      );

      // Bio
      expect(find.text('Candidate Bio'), findsOneWidget);
      expect(
        find.text(
            'Passionate cloud engineer with 10+ years of distributed systems experience.'),
        findsOneWidget,
      );

      // Read-only indicator
      expect(find.textContaining('Read-only view'), findsOneWidget);
    });

    testWidgets('strictly adheres to read-only constraints (no action buttons)',
        (tester) async {
      setMobileView(tester);
      final applicant = RecruiterApplicant.fromJson(_fullApplicantJson);

      await tester.pumpWidget(buildTestable(RecruiterApplicantDetailScreen(
        applicantId: 'app-detail-1',
        initialApplicant: applicant,
      )));
      await tester.pumpAndSettle();

      // Verify absence of recruitment mutation / action buttons
      expect(find.text('Shortlist'), findsNothing);
      expect(find.text('Approve'), findsNothing);
      expect(find.text('Reject'), findsNothing);
      expect(find.text('Interview'), findsNothing);
      expect(find.text('Schedule Interview'), findsNothing);
      expect(find.text('Hire'), findsNothing);
      expect(find.text('Make Offer'), findsNothing);
      expect(find.text('Change Status'), findsNothing);
      expect(find.text('AI Shortlist'), findsNothing);
      expect(find.text('AI Approve'), findsNothing);
      expect(find.text('AI Reject'), findsNothing);
      expect(find.text('Download CV'), findsNothing);
      expect(find.text('View CV'), findsNothing);
      expect(find.text('Delete'), findsNothing);
      expect(find.text('Edit'), findsNothing);
      expect(find.byIcon(Icons.download), findsNothing);
      expect(find.byIcon(Icons.delete), findsNothing);
      expect(find.byIcon(Icons.edit), findsNothing);
    });

    testWidgets('gracefully omits missing optional fields', (tester) async {
      setMobileView(tester);
      final minimalJson = {
        'id': 'app-min-1',
        'jobPostingId': 'job-2',
        'jobTitle': 'Junior QA Analyst',
        'candidateId': 'c-20',
        'fullName': 'Sita Kumari',
        'email': 'sita@example.com',
        'status': 'UnderReview',
        'appliedAt': '2026-10-02T10:00:00.000Z',
        'hasCv': false,
        'matchScore': 0,
        'exactMatchScore': 0.0,
        'matchExplanation': '',
        'matchBreakdown': [],
        'skills': [],
        'matchedSkills': [],
        'missingSkills': [],
        'education': [],
        'workExperience': [],
      };

      final applicant = RecruiterApplicant.fromJson(minimalJson);

      await tester.pumpWidget(buildTestable(RecruiterApplicantDetailScreen(
        applicantId: 'app-min-1',
        initialApplicant: applicant,
      )));
      await tester.pumpAndSettle();

      // Core profile fields
      expect(find.text('Sita Kumari'), findsOneWidget);
      expect(find.text('Under Review'), findsOneWidget);
      expect(find.text('Junior QA Analyst'), findsOneWidget);
      expect(find.text('No CV provided'), findsOneWidget);

      // Missing optional sections should NOT render
      expect(find.text('Skill Match Information'), findsNothing);
      expect(find.textContaining('Matched Skills'), findsNothing);
      expect(find.textContaining('Missing Skills'), findsNothing);
      expect(find.textContaining('Candidate Skills'), findsNothing);
      expect(find.text('Education'), findsNothing);
      expect(find.text('Work Experience'), findsNothing);
      expect(find.text('Candidate Bio'), findsNothing);
    });

    testWidgets('shows error state with retry on fetch failure when no initial applicant',
        (tester) async {
      setMobileView(tester);
      int attempts = 0;
      final mock = MockHttpClient((req) async {
        attempts++;
        if (attempts == 1) {
          return http.Response(
            jsonEncode({'message': 'Applicant record not found'}),
            404,
            headers: {'content-type': 'application/json'},
          );
        }
        return http.Response(jsonEncode(_fullApplicantJson), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterApplicantDetailScreen(
        applicantId: 'app-detail-1',
        service: service,
      )));
      await tester.pumpAndSettle();

      expect(find.text('Applicant record not found'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);

      // Tap retry
      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();

      expect(find.text('Nimal Weerasinghe'), findsOneWidget);
    });
  });
}
