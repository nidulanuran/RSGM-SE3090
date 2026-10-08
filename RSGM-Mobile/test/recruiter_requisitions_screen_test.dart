import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/screens/recruiter/recruiter_requisitions_screen.dart';
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

final _mockRequisitions = [
  {
    'id': 'req-1',
    'companyId': 'comp-1',
    'companyName': 'Hireon Technologies',
    'recruiterId': 'rec-1',
    'recruiterName': 'Recruiter One',
    'positionTitle': 'Senior Data Engineer',
    'department': 'Analytics & Data',
    'headcount': 2,
    'employmentType': 0, // FullTime
    'workMode': 2, // Hybrid
    'location': 'Colombo',
    'experienceLevel': 3, // Senior
    'minExperienceYears': 5,
    'minSalary': 300000.0,
    'maxSalary': 550000.0,
    'currency': 'LKR',
    'description': 'Build distributed ETL data pipelines',
    'responsibilities': 'Maintain data lakehouses',
    'requirements': 'Spark, Kafka, Python',
    'justification': 'Expanding enterprise analytics capability',
    'status': 1, // Submitted
    'createdAt': '2026-10-01T10:00:00.000Z',
    'submittedAt': '2026-10-02T10:00:00.000Z',
  },
  {
    'id': 'req-2',
    'companyId': 'comp-2',
    'companyName': 'Acme Corp',
    'recruiterId': 'rec-1',
    'recruiterName': 'Recruiter One',
    'positionTitle': 'Full Stack Developer',
    'department': 'Product Engineering',
    'headcount': 1,
    'employmentType': 0, // FullTime
    'workMode': 1, // Remote
    'location': 'Kandy',
    'experienceLevel': 2, // Mid
    'minExperienceYears': 3,
    'currency': 'LKR',
    'status': 3, // Approved
    'createdAt': '2026-09-25T10:00:00.000Z',
    'submittedAt': '2026-09-26T10:00:00.000Z',
    'approvedAt': '2026-09-28T10:00:00.000Z',
    'reviewedByName': 'Sarah HR Director',
    'hrFeedback': 'Approved with standard salary band',
  },
  {
    'id': 'req-3',
    'companyId': 'comp-3',
    'companyName': 'Beta Labs',
    'recruiterId': 'rec-1',
    'recruiterName': 'Recruiter One',
    'positionTitle': 'UI/UX Designer',
    'department': 'Design Studio',
    'headcount': 1,
    'employmentType': 1, // PartTime
    'workMode': 0, // OnSite
    'location': 'Galle',
    'experienceLevel': 1, // Junior
    'currency': 'LKR',
    'status': 2, // Rejected
    'createdAt': '2026-09-10T10:00:00.000Z',
    'reviewedByName': 'John HR Lead',
    'hrFeedback': 'Budget constraints for Q4',
  },
];

void main() {
  Widget buildTestable(Widget child) {
    return MaterialApp(
      theme: AppTheme.light(),
      home: Scaffold(body: child),
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

  group('RecruiterRequisitionsScreen widget tests', () {
    testWidgets('renders empty state when no requisitions exist',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode([]), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterRequisitionsScreen(service: service)));
      await tester.pumpAndSettle();

      expect(find.text('Requisitions'), findsWidgets);
      expect(find.text('No requisitions yet'), findsOneWidget);
      expect(
        find.text('Your recruitment requisitions will appear here.'),
        findsOneWidget,
      );

      // Verify strictly NO creation/submission buttons
      expect(find.text('Create Requisition'), findsNothing);
      expect(find.text('Submit Requisition'), findsNothing);
      expect(find.byIcon(Icons.add), findsNothing);
    });

    testWidgets('renders list of requisition cards with correct details',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockRequisitions), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterRequisitionsScreen(service: service)));
      await tester.pumpAndSettle();

      // Requisition 1
      expect(find.text('Senior Data Engineer'), findsOneWidget);
      expect(find.text('Analytics & Data • Hireon Technologies'), findsOneWidget);
      expect(find.text('Colombo • Hybrid'), findsOneWidget);
      expect(find.text('Full-Time • Senior'), findsOneWidget);
      expect(find.text('Submitted'), findsWidgets);
      expect(find.text('2 Openings'), findsOneWidget);
      expect(find.text('Submitted: 2 Oct 2026'), findsOneWidget);

      // Requisition 2
      expect(find.text('Full Stack Developer'), findsOneWidget);
      expect(find.text('Product Engineering • Acme Corp'), findsOneWidget);
      expect(find.text('Approved'), findsWidgets);
      expect(find.text('Feedback on file'), findsWidgets);

      // Requisition 3
      expect(find.text('UI/UX Designer'), findsOneWidget);
      expect(find.text('Rejected'), findsWidgets);

      // View Details callout
      expect(find.text('View Details'), findsWidgets);
    });

    testWidgets('searches locally across title, department, and company',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockRequisitions), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterRequisitionsScreen(service: service)));
      await tester.pumpAndSettle();

      // Search 'data'
      await tester.enterText(find.byType(TextField), 'data');
      await tester.pumpAndSettle();

      expect(find.text('Senior Data Engineer'), findsOneWidget);
      expect(find.text('Full Stack Developer'), findsNothing);
      expect(find.text('UI/UX Designer'), findsNothing);

      // Clear search
      await tester.tap(find.byIcon(Icons.clear_rounded));
      await tester.pumpAndSettle();

      expect(find.text('Senior Data Engineer'), findsOneWidget);
      expect(find.text('Full Stack Developer'), findsOneWidget);
      expect(find.text('UI/UX Designer'), findsOneWidget);
    });

    testWidgets('filters locally by status chip', (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockRequisitions), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterRequisitionsScreen(service: service)));
      await tester.pumpAndSettle();

      // Select 'Submitted' chip
      await tester.tap(find.widgetWithText(FilterChip, 'Submitted (1)'));
      await tester.pumpAndSettle();

      expect(find.text('Senior Data Engineer'), findsOneWidget);
      expect(find.text('Full Stack Developer'), findsNothing);
      expect(find.text('UI/UX Designer'), findsNothing);

      // Select 'Approved' chip
      await tester.tap(find.widgetWithText(FilterChip, 'Approved (1)'));
      await tester.pumpAndSettle();

      expect(find.text('Full Stack Developer'), findsOneWidget);
      expect(find.text('Senior Data Engineer'), findsNothing);

      // Select 'All' chip
      await tester.tap(find.widgetWithText(FilterChip, 'All (3)'));
      await tester.pumpAndSettle();

      expect(find.text('Senior Data Engineer'), findsOneWidget);
      expect(find.text('Full Stack Developer'), findsOneWidget);
      expect(find.text('UI/UX Designer'), findsOneWidget);
    });

    testWidgets('handles combined search + status filter and clear filters action',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockRequisitions), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterRequisitionsScreen(service: service)));
      await tester.pumpAndSettle();

      // Search 'Data' but select 'Approved' filter -> 0 matches
      await tester.enterText(find.byType(TextField), 'Data');
      await tester.tap(find.widgetWithText(FilterChip, 'Approved (1)'));
      await tester.pumpAndSettle();

      expect(find.text('No matching requisitions'), findsOneWidget);
      expect(find.text('Clear filters'), findsOneWidget);

      // Tap 'Clear filters'
      await tester.tap(find.text('Clear filters'));
      await tester.pumpAndSettle();

      expect(find.text('Senior Data Engineer'), findsOneWidget);
      expect(find.text('Full Stack Developer'), findsOneWidget);
      expect(find.text('UI/UX Designer'), findsOneWidget);
    });

    testWidgets('shows error state with retry on network failure',
        (tester) async {
      setMobileView(tester);
      int attempts = 0;
      final mock = MockHttpClient((req) async {
        attempts++;
        if (attempts == 1) {
          return http.Response(
            jsonEncode({'message': 'Failed to connect to requisitions service'}),
            500,
            headers: {'content-type': 'application/json'},
          );
        }
        return http.Response(jsonEncode([]), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterRequisitionsScreen(service: service)));
      await tester.pumpAndSettle();

      expect(
          find.text('Failed to connect to requisitions service'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);

      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();

      expect(find.text('No requisitions yet'), findsOneWidget);
    });

    testWidgets('tapping requisition card navigates to detail screen',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        if (req.url.path.contains('recruiter/requisitions/req-1')) {
          return http.Response(jsonEncode(_mockRequisitions[0]), 200,
              headers: {'content-type': 'application/json'});
        }
        return http.Response(jsonEncode(_mockRequisitions), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterRequisitionsScreen(service: service)));
      await tester.pumpAndSettle();

      // Tap card
      await tester.tap(find.text('Senior Data Engineer'));
      await tester.pumpAndSettle();

      // Detail screen opened
      expect(find.text('Requisition Details'), findsOneWidget);
      expect(find.text('Approval & Workflow Status'), findsOneWidget);
      expect(find.text('Position & Compensation Details'), findsOneWidget);
    });
  });
}
