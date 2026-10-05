import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/screens/recruiter/recruiter_applicants_screen.dart';
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

final _mockApplicants = [
  {
    'id': 'app-1',
    'jobPostingId': 'job-1',
    'jobTitle': 'ML Engineer',
    'candidateId': 'c-1',
    'fullName': 'Dinesh Silva',
    'email': 'dinesh@example.com',
    'headline': 'Machine Learning Engineer',
    'location': 'Colombo',
    'status': 'Shortlisted',
    'matchScore': 87,
    'exactMatchScore': 87.2,
    'matchExplanation': 'Strong alignment with ML requirements',
    'appliedAt': '2026-10-12T00:00:00.000Z',
    'hasCv': true,
    'cvFileName': 'dinesh_cv.pdf',
    'matchedSkills': ['Python', 'Machine Learning'],
    'missingSkills': ['Docker'],
    'skills': ['Python', 'Machine Learning', 'PyTorch'],
  },
  {
    'id': 'app-2',
    'jobPostingId': 'job-2',
    'jobTitle': 'Software Engineer',
    'candidateId': 'c-2',
    'fullName': 'Amara Fernando',
    'email': 'amara@example.com',
    'headline': 'Frontend Architect',
    'location': 'Kandy',
    'status': 'UnderReview',
    'matchScore': 65,
    'exactMatchScore': 65.0,
    'matchExplanation': 'Good candidate with solid frontend foundation',
    'appliedAt': '2026-10-10T00:00:00.000Z',
    'hasCv': true,
    'cvFileName': null,
    'matchedSkills': ['Dart', 'Flutter'],
    'missingSkills': ['Kubernetes'],
    'skills': ['Dart', 'Flutter', 'TypeScript'],
  },
  {
    'id': 'app-3',
    'jobPostingId': 'job-2',
    'jobTitle': 'Software Engineer',
    'candidateId': 'c-3',
    'fullName': 'Kamal Perera',
    'email': 'kamal@example.com',
    'headline': 'DevOps Engineer',
    'location': 'Galle',
    'status': 'Rejected',
    'matchScore': 40,
    'exactMatchScore': 40.0,
    'matchExplanation': 'Lacks required mobile experience',
    'appliedAt': '2026-09-28T00:00:00.000Z',
    'hasCv': false,
    'cvFileName': null,
    'matchedSkills': ['Linux'],
    'missingSkills': ['Flutter', 'Dart'],
    'skills': ['Linux', 'Bash'],
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

  group('RecruiterApplicantsScreen widget tests', () {
    testWidgets('renders empty state when no applicants exist',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode([]), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterApplicantsScreen(service: service)));
      await tester.pumpAndSettle();

      expect(find.text('Applicants'), findsWidgets);
      expect(find.text('No applicants yet'), findsOneWidget);
      expect(
        find.text(
            'Applicants will appear here when candidates apply to your job posts.'),
        findsOneWidget,
      );

      // Verify NO create job post button
      expect(find.text('Create Job'), findsNothing);
      expect(find.text('Create Job Post'), findsNothing);
    });

    testWidgets('renders list of applicant cards with correct info',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockApplicants), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterApplicantsScreen(service: service)));
      await tester.pumpAndSettle();

      // Card 1
      expect(find.text('DS'), findsOneWidget);
      expect(find.text('Dinesh Silva'), findsOneWidget);
      expect(find.text('Machine Learning Engineer'), findsOneWidget);
      expect(find.text('ML Engineer'), findsOneWidget);
      expect(find.text('Shortlisted'), findsWidgets);
      expect(find.text('Match: 87%'), findsOneWidget);
      expect(find.text('2 Matched'), findsWidgets);
      expect(find.text('1 Missing'), findsWidgets);
      expect(find.text('Applied: 12 Oct 2026'), findsOneWidget);

      // Card 2
      expect(find.text('AF'), findsOneWidget);
      expect(find.text('Amara Fernando'), findsOneWidget);
      expect(find.text('Frontend Architect'), findsOneWidget);
      expect(find.text('Software Engineer'), findsWidgets);
      expect(find.text('Under Review'), findsWidgets);
      expect(find.text('Match: 65%'), findsOneWidget);

      // View Profile action
      expect(find.text('View Profile'), findsWidgets);
    });

    testWidgets('searches locally across candidate name, skills, and email',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockApplicants), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterApplicantsScreen(service: service)));
      await tester.pumpAndSettle();

      // Search 'dinesh'
      await tester.enterText(find.byType(TextField), 'dinesh');
      await tester.pumpAndSettle();

      expect(find.text('Dinesh Silva'), findsOneWidget);
      expect(find.text('Amara Fernando'), findsNothing);
      expect(find.text('Kamal Perera'), findsNothing);

      // Clear search
      await tester.tap(find.byIcon(Icons.clear_rounded));
      await tester.pumpAndSettle();

      expect(find.text('Dinesh Silva'), findsOneWidget);
      expect(find.text('Amara Fernando'), findsOneWidget);
      expect(find.text('Kamal Perera'), findsOneWidget);
    });

    testWidgets('filters locally by job title using dropdown', (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockApplicants), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterApplicantsScreen(service: service)));
      await tester.pumpAndSettle();

      // Open job dropdown and select 'ML Engineer'
      await tester.tap(find.byType(DropdownButton<String>));
      await tester.pumpAndSettle();

      await tester.tap(find.text('ML Engineer').last);
      await tester.pumpAndSettle();

      expect(find.text('Dinesh Silva'), findsOneWidget);
      expect(find.text('Amara Fernando'), findsNothing);
      expect(find.text('Kamal Perera'), findsNothing);
    });

    testWidgets('filters locally by status chip', (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockApplicants), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterApplicantsScreen(service: service)));
      await tester.pumpAndSettle();

      // Select 'Shortlisted' chip
      await tester.tap(find.widgetWithText(FilterChip, 'Shortlisted (1)'));
      await tester.pumpAndSettle();

      expect(find.text('Dinesh Silva'), findsOneWidget);
      expect(find.text('Amara Fernando'), findsNothing);
      expect(find.text('Kamal Perera'), findsNothing);

      // Select 'Under Review' chip
      await tester.tap(find.widgetWithText(FilterChip, 'Under Review (1)'));
      await tester.pumpAndSettle();

      expect(find.text('Amara Fernando'), findsOneWidget);
      expect(find.text('Dinesh Silva'), findsNothing);

      // Select 'All' chip
      await tester.tap(find.widgetWithText(FilterChip, 'All (3)'));
      await tester.pumpAndSettle();

      expect(find.text('Dinesh Silva'), findsOneWidget);
      expect(find.text('Amara Fernando'), findsOneWidget);
      expect(find.text('Kamal Perera'), findsOneWidget);
    });

    testWidgets('handles combined search + job + status filter and clear filters',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockApplicants), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterApplicantsScreen(service: service)));
      await tester.pumpAndSettle();

      // Search 'Amara' and filter Status to 'Shortlisted' -> 0 matches
      await tester.enterText(find.byType(TextField), 'Amara');
      await tester.tap(find.widgetWithText(FilterChip, 'Shortlisted (1)'));
      await tester.pumpAndSettle();

      expect(find.text('No matching applicants'), findsOneWidget);
      expect(find.text('Clear filters'), findsOneWidget);

      // Tap 'Clear filters'
      await tester.tap(find.text('Clear filters'));
      await tester.pumpAndSettle();

      expect(find.text('Dinesh Silva'), findsOneWidget);
      expect(find.text('Amara Fernando'), findsOneWidget);
      expect(find.text('Kamal Perera'), findsOneWidget);
    });

    testWidgets('shows error state with retry on network failure',
        (tester) async {
      setMobileView(tester);
      int attempts = 0;
      final mock = MockHttpClient((req) async {
        attempts++;
        if (attempts == 1) {
          return http.Response(
            jsonEncode({'message': 'Network unavailable'}),
            503,
            headers: {'content-type': 'application/json'},
          );
        }
        return http.Response(jsonEncode([]), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterApplicantsScreen(service: service)));
      await tester.pumpAndSettle();

      expect(find.text('Network unavailable'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);

      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();

      expect(find.text('No applicants yet'), findsOneWidget);
    });

    testWidgets('tapping applicant card navigates to detail screen',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        if (req.url.path.contains('recruiter/applications/app-1')) {
          return http.Response(jsonEncode(_mockApplicants[0]), 200,
              headers: {'content-type': 'application/json'});
        }
        return http.Response(jsonEncode(_mockApplicants), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterApplicationService(apiClient: apiClient);

      await tester.pumpWidget(
          buildTestable(RecruiterApplicantsScreen(service: service)));
      await tester.pumpAndSettle();

      // Tap Dinesh Silva card
      await tester.tap(find.text('Dinesh Silva'));
      await tester.pumpAndSettle();

      // Verifies navigation to detail screen
      expect(find.text('Applicant Details'), findsOneWidget);
      expect(find.text('Application & Contact Information'), findsOneWidget);
      expect(find.text('Skill Match Information'), findsOneWidget);
    });
  });
}
