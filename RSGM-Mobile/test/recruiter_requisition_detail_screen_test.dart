import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/models/recruiter/recruiter_requisition.dart';
import 'package:rsgm_mobile/screens/recruiter/recruiter_requisition_detail_screen.dart';
import 'package:rsgm_mobile/services/api_client.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_requisition_service.dart';
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

final _fullApprovedRequisitionJson = {
  'id': 'req-detail-1',
  'companyId': 'comp-10',
  'companyName': 'Apex Global Systems',
  'recruiterId': 'rec-5',
  'recruiterName': 'Kasun Perera',
  'positionTitle': 'Principal Cloud Architect',
  'department': 'Enterprise Architecture',
  'headcount': 2,
  'employmentType': 0, // FullTime
  'workMode': 2, // Hybrid
  'location': 'Colombo, Sri Lanka',
  'experienceLevel': 4, // Lead
  'minExperienceYears': 8,
  'minSalary': 600000.0,
  'maxSalary': 950000.0,
  'currency': 'LKR',
  'description':
      'Lead cloud transformation strategy and multi-cloud architectures across regions.',
  'responsibilities':
      'Architect resilient microservices infrastructure and oversee cloud governance.',
  'requirements':
      '10+ years software engineering, AWS/GCP Solutions Architect Professional, Kubernetes expert.',
  'justification':
      'Mandatory headcount expansion for financial services enterprise platform rollout.',
  'status': 3, // Approved
  'createdAt': '2026-09-15T08:30:00.000Z',
  'submittedAt': '2026-09-16T10:00:00.000Z',
  'reviewedAt': '2026-09-18T14:20:00.000Z',
  'approvedAt': '2026-09-19T09:00:00.000Z',
  'reviewedByName': 'Anoma Senanayake',
  'hrFeedback':
      'Requisition approved by Executive HR committee. Approved for immediate posting.',
};

final _rejectedRequisitionJson = {
  'id': 'req-detail-2',
  'companyId': 'comp-10',
  'companyName': 'Apex Global Systems',
  'recruiterId': 'rec-5',
  'recruiterName': 'Kasun Perera',
  'positionTitle': 'Junior DevOps Engineer',
  'department': 'DevOps & SRE',
  'headcount': 1,
  'employmentType': 1, // PartTime
  'workMode': 0, // OnSite
  'location': 'Kandy, Sri Lanka',
  'experienceLevel': 1, // Junior
  'minExperienceYears': 1,
  'minSalary': 120000.0,
  'maxSalary': 180000.0,
  'currency': 'LKR',
  'description': 'Assist in CI/CD pipeline automation and server maintenance.',
  'responsibilities': 'Monitor build pipelines and report issues.',
  'requirements': 'Basic knowledge of Linux, Docker, and GitHub Actions.',
  'justification': 'Need part-time support for night deployments.',
  'status': 2, // Rejected
  'createdAt': '2026-09-20T08:30:00.000Z',
  'submittedAt': '2026-09-21T10:00:00.000Z',
  'reviewedAt': '2026-09-22T14:20:00.000Z',
  'reviewedByName': 'John HR Director',
  'hrFeedback': 'Headcount frozen for junior part-time roles until Q1 2027.',
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

  group('RecruiterRequisitionDetailScreen widget tests', () {
    testWidgets(
        'renders complete requisition information including header, workflow, compensation, and sections',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_fullApprovedRequisitionJson), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterRequisitionDetailScreen(
        requisitionId: 'req-detail-1',
        service: service,
      )));
      await tester.pumpAndSettle();

      // Screen title & Header card
      expect(find.text('Requisition Details'), findsOneWidget);
      expect(find.text('Principal Cloud Architect'), findsOneWidget);
      expect(find.text('Enterprise Architecture'), findsOneWidget);
      expect(find.text('Apex Global Systems • Colombo, Sri Lanka'), findsOneWidget);
      expect(find.text('Approved'), findsWidgets);
      expect(find.text('2 Openings'), findsOneWidget);
      expect(find.text('Full-Time'), findsWidgets);
      expect(find.text('Hybrid'), findsWidgets);

      // Approval & Workflow Status Card
      expect(find.text('Approval & Workflow Status'), findsOneWidget);
      expect(find.text('Created Date'), findsOneWidget);
      expect(find.text('Submitted for Review'), findsOneWidget);
      expect(find.text('Reviewed Date'), findsOneWidget);
      expect(find.text('Approved Date'), findsOneWidget);
      expect(find.text('Reviewer'), findsOneWidget);
      expect(find.text('Anoma Senanayake'), findsOneWidget);

      // HR Review Feedback
      expect(find.text('HR Review Feedback'), findsOneWidget);
      expect(
        find.text(
            'Requisition approved by Executive HR committee. Approved for immediate posting.'),
        findsOneWidget,
      );

      // Position & Compensation Details Card
      expect(find.text('Position & Compensation Details'), findsOneWidget);
      expect(find.text('Salary Range'), findsOneWidget);
      expect(find.text('LKR 600,000 – 950,000'), findsOneWidget);
      expect(find.text('Employment Type'), findsOneWidget);
      expect(find.text('Work Mode'), findsOneWidget);
      expect(find.text('Experience Level'), findsOneWidget);
      expect(find.text('Lead / Principal'), findsOneWidget);
      expect(find.text('Min. Experience'), findsOneWidget);
      expect(find.text('8 years required'), findsOneWidget);
      expect(find.text('Requested By'), findsOneWidget);
      expect(find.text('Kasun Perera'), findsOneWidget);

      // Detailed text sections
      expect(find.text('Position Description'), findsOneWidget);
      expect(
        find.text(
            'Lead cloud transformation strategy and multi-cloud architectures across regions.'),
        findsOneWidget,
      );
      expect(find.text('Key Responsibilities'), findsOneWidget);
      expect(
        find.text(
            'Architect resilient microservices infrastructure and oversee cloud governance.'),
        findsOneWidget,
      );
      expect(find.text('Requirements & Qualifications'), findsOneWidget);
      expect(
        find.text(
            '10+ years software engineering, AWS/GCP Solutions Architect Professional, Kubernetes expert.'),
        findsOneWidget,
      );
      expect(find.text('Business Justification'), findsOneWidget);
      expect(
        find.text(
            'Mandatory headcount expansion for financial services enterprise platform rollout.'),
        findsOneWidget,
      );

      // Read-only identifier footer
      expect(find.textContaining('Read-only view'), findsOneWidget);
    });

    testWidgets(
        'renders rejected status with HR rejection feedback gracefully',
        (tester) async {
      setMobileView(tester);
      final requisition =
          RecruiterRequisition.fromJson(_rejectedRequisitionJson);

      await tester.pumpWidget(buildTestable(RecruiterRequisitionDetailScreen(
        requisitionId: 'req-detail-2',
        initialRequisition: requisition,
      )));
      await tester.pumpAndSettle();

      expect(find.text('Junior DevOps Engineer'), findsOneWidget);
      expect(find.text('Rejected'), findsWidgets);
      expect(find.text('HR Review Feedback'), findsOneWidget);
      expect(
        find.text(
            'Headcount frozen for junior part-time roles until Q1 2027.'),
        findsOneWidget,
      );
      expect(find.text('John HR Director'), findsOneWidget);
    });

    testWidgets(
        'strictly adheres to read-only constraints (no mutation or status change actions)',
        (tester) async {
      setMobileView(tester);
      final requisition =
          RecruiterRequisition.fromJson(_fullApprovedRequisitionJson);

      await tester.pumpWidget(buildTestable(RecruiterRequisitionDetailScreen(
        requisitionId: 'req-detail-1',
        initialRequisition: requisition,
      )));
      await tester.pumpAndSettle();

      // Verify absence of requisition mutation / decision / status buttons
      expect(find.text('Create'), findsNothing);
      expect(find.text('Create Requisition'), findsNothing);
      expect(find.text('Edit'), findsNothing);
      expect(find.text('Edit Requisition'), findsNothing);
      expect(find.text('Delete'), findsNothing);
      expect(find.text('Delete Requisition'), findsNothing);
      expect(find.text('Submit'), findsNothing);
      expect(find.text('Submit Requisition'), findsNothing);
      expect(find.text('Approve'), findsNothing);
      expect(find.text('Reject'), findsNothing);
      expect(find.text('Withdraw'), findsNothing);
      expect(find.text('Resubmit'), findsNothing);
      expect(find.text('Change Status'), findsNothing);
      expect(find.text('Send back'), findsNothing);
      expect(find.text('Edit feedback'), findsNothing);
      expect(find.text('AI Generate'), findsNothing);
      expect(find.text('AI Recommendation'), findsNothing);
      expect(find.byType(DropdownButton), findsNothing);
      expect(find.byType(DropdownButtonFormField), findsNothing);
      expect(find.byIcon(Icons.edit), findsNothing);
      expect(find.byIcon(Icons.delete), findsNothing);
      expect(find.byIcon(Icons.add), findsNothing);
    });

    testWidgets(
        'gracefully omits missing optional fields (no salary, dates, or feedback)',
        (tester) async {
      setMobileView(tester);
      final minimalJson = {
        'id': 'req-min-1',
        'companyId': 'comp-1',
        'companyName': 'Acme Inc',
        'recruiterId': 'rec-1',
        'recruiterName': '',
        'positionTitle': 'Software Engineer Intern',
        'department': 'Engineering',
        'headcount': 1,
        'employmentType': 'Internship',
        'workMode': 'Remote',
        'location': 'Remote',
        'experienceLevel': 'Entry',
        'status': 'Draft',
        'createdAt': '2026-10-01T12:00:00.000Z',
      };

      final requisition = RecruiterRequisition.fromJson(minimalJson);

      await tester.pumpWidget(buildTestable(RecruiterRequisitionDetailScreen(
        requisitionId: 'req-min-1',
        initialRequisition: requisition,
      )));
      await tester.pumpAndSettle();

      // Core fields
      expect(find.text('Software Engineer Intern'), findsOneWidget);
      expect(find.text('Draft'), findsWidgets);
      expect(find.text('Internship'), findsWidgets);
      expect(find.text('Remote'), findsWidgets);
      expect(find.text('Entry Level'), findsOneWidget);

      // Missing optional sections should NOT render
      expect(find.text('Position Description'), findsNothing);
      expect(find.text('Key Responsibilities'), findsNothing);
      expect(find.text('Requirements & Qualifications'), findsNothing);
      expect(find.text('Business Justification'), findsNothing);
      expect(find.text('HR Review Feedback'), findsNothing);
      expect(find.text('Submitted for Review'), findsNothing);
      expect(find.text('Reviewed Date'), findsNothing);
      expect(find.text('Approved Date'), findsNothing);
      expect(find.text('Reviewer'), findsNothing);
      expect(find.text('Min. Experience'), findsNothing);
      expect(find.text('Salary Range'), findsNothing);
    });

    testWidgets(
        'shows error state with retry on fetch failure when no initial requisition is given',
        (tester) async {
      setMobileView(tester);
      int attempts = 0;
      final mock = MockHttpClient((req) async {
        attempts++;
        if (attempts == 1) {
          return http.Response(
            jsonEncode({'message': 'Requisition record not found'}),
            404,
            headers: {'content-type': 'application/json'},
          );
        }
        return http.Response(jsonEncode(_fullApprovedRequisitionJson), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      await tester.pumpWidget(buildTestable(RecruiterRequisitionDetailScreen(
        requisitionId: 'req-detail-1',
        service: service,
      )));
      await tester.pumpAndSettle();

      expect(find.text('Requisition record not found'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);

      // Tap retry
      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();

      expect(find.text('Principal Cloud Architect'), findsOneWidget);
    });
  });
}
