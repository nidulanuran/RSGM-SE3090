import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/screens/recruiter/recruiter_job_posts_screen.dart';
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

final _mockJobPosts = [
  {
    'id': 'job-1',
    'title': 'Software Engineer',
    'company': 'RSGM Technologies',
    'location': 'Colombo',
    'employmentType': 'FullTime',
    'workMode': 'Hybrid',
    'experienceLevel': 'Mid',
    'minExperienceYears': 3,
    'minSalary': 200000.0,
    'maxSalary': 350000.0,
    'currency': 'LKR',
    'applicationDeadline': '2026-10-20T00:00:00.000Z',
    'status': 'Published',
    'applicantCount': 12,
    'createdAt': '2026-09-01T10:00:00.000Z',
    'description': 'Building next gen platforms',
    'responsibilities': 'Develop scalable backend services',
    'requirements': 'Proficiency in Dart and C#',
    'requiredSkills': [
      {'id': 's1', 'name': 'Python', 'weight': 1.0},
      {'id': 's2', 'name': 'Docker', 'weight': 1.5},
    ],
  },
  {
    'id': 'job-2',
    'title': 'Frontend Developer',
    'company': 'Acme Corp',
    'location': 'Kandy',
    'employmentType': 'PartTime',
    'workMode': 'Remote',
    'experienceLevel': 'Senior',
    'minExperienceYears': 5,
    'minSalary': 300000.0,
    'maxSalary': 500000.0,
    'currency': 'LKR',
    'applicationDeadline': null,
    'status': 'Draft',
    'applicantCount': 0,
    'createdAt': '2026-09-05T10:00:00.000Z',
    'description': 'Mobile and web UI architecture',
    'responsibilities': 'Create Flutter components',
    'requirements': 'Expertise in modern mobile architecture',
    'requiredSkills': [
      {'id': 's3', 'name': 'Flutter', 'weight': 2.0},
    ],
  },
  {
    'id': 'job-3',
    'title': 'QA Lead',
    'company': 'Beta Inc',
    'location': 'Galle',
    'employmentType': 'Contract',
    'workMode': 'OnSite',
    'experienceLevel': 'Senior',
    'minExperienceYears': 4,
    'status': 'Closed',
    'applicantCount': 5,
    'createdAt': '2026-08-10T10:00:00.000Z',
    'description': 'Test automation and quality control',
    'responsibilities': 'Maintain end-to-end test pipelines',
    'requirements': 'Extensive experience in automated testing',
    'requiredSkills': [],
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

  group('RecruiterJobPostsScreen widget tests', () {
    testWidgets('renders empty state when recruiter has no job postings',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode([]), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterJobPostService(apiClient: apiClient);

      await tester
          .pumpWidget(buildTestable(RecruiterJobPostsScreen(service: service)));
      await tester.pumpAndSettle();

      expect(find.text('My Job Posts'), findsOneWidget);
      expect(find.text('View your published and existing job postings.'),
          findsOneWidget);
      expect(find.text('No job posts found'), findsOneWidget);
      expect(find.text("You don't currently have any job posts to display."),
          findsOneWidget);

      // Verify strictly NO create button exists
      expect(find.text('Create Job Post'), findsNothing);
      expect(find.byIcon(Icons.add), findsNothing);
    });

    testWidgets('renders list of job posts with correct details and formatting',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockJobPosts), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterJobPostService(apiClient: apiClient);

      await tester
          .pumpWidget(buildTestable(RecruiterJobPostsScreen(service: service)));
      await tester.pumpAndSettle();

      // Check card 1
      expect(find.text('Software Engineer'), findsOneWidget);
      expect(find.text('RSGM Technologies'), findsOneWidget);
      expect(find.text('Colombo • Hybrid'), findsOneWidget);
      expect(find.text('Full-Time • Mid Level'), findsOneWidget);
      expect(find.text('Published'), findsWidgets);
      expect(find.text('12 Applicants'), findsOneWidget);
      expect(find.text('Deadline: 20 October 2026'), findsOneWidget);

      // Check card 2
      expect(find.text('Frontend Developer'), findsOneWidget);
      expect(find.text('Acme Corp'), findsOneWidget);
      expect(find.text('Kandy • Remote'), findsOneWidget);
      expect(find.text('Part-Time • Senior'), findsOneWidget);
      expect(find.text('Draft'), findsWidgets);
      expect(find.text('0 Applicants'), findsOneWidget);

      // Check "View Details" callout
      expect(find.text('View Details'), findsWidgets);
    });

    testWidgets('filters job posts locally with case-insensitive search',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockJobPosts), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterJobPostService(apiClient: apiClient);

      await tester
          .pumpWidget(buildTestable(RecruiterJobPostsScreen(service: service)));
      await tester.pumpAndSettle();

      // Search for "frontend"
      await tester.enterText(find.byType(TextField), 'frontend');
      await tester.pumpAndSettle();

      expect(find.text('Frontend Developer'), findsOneWidget);
      expect(find.text('Software Engineer'), findsNothing);
      expect(find.text('QA Lead'), findsNothing);

      // Clear search via clear icon
      await tester.tap(find.byIcon(Icons.clear_rounded));
      await tester.pumpAndSettle();

      expect(find.text('Software Engineer'), findsOneWidget);
      expect(find.text('Frontend Developer'), findsOneWidget);
      expect(find.text('QA Lead'), findsOneWidget);
    });

    testWidgets('filters job posts locally by status chip', (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockJobPosts), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterJobPostService(apiClient: apiClient);

      await tester
          .pumpWidget(buildTestable(RecruiterJobPostsScreen(service: service)));
      await tester.pumpAndSettle();

      // Select 'Published' chip
      await tester.tap(find.widgetWithText(FilterChip, 'Published (1)'));
      await tester.pumpAndSettle();

      expect(find.text('Software Engineer'), findsOneWidget);
      expect(find.text('Frontend Developer'), findsNothing);
      expect(find.text('QA Lead'), findsNothing);

      // Select 'Draft' chip
      await tester.tap(find.widgetWithText(FilterChip, 'Draft (1)'));
      await tester.pumpAndSettle();

      expect(find.text('Frontend Developer'), findsOneWidget);
      expect(find.text('Software Engineer'), findsNothing);

      // Select 'All' chip
      await tester.tap(find.widgetWithText(FilterChip, 'All (3)'));
      await tester.pumpAndSettle();

      expect(find.text('Software Engineer'), findsOneWidget);
      expect(find.text('Frontend Developer'), findsOneWidget);
      expect(find.text('QA Lead'), findsOneWidget);
    });

    testWidgets('handles combined search + status filter and clear filters action',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        return http.Response(jsonEncode(_mockJobPosts), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterJobPostService(apiClient: apiClient);

      await tester
          .pumpWidget(buildTestable(RecruiterJobPostsScreen(service: service)));
      await tester.pumpAndSettle();

      // Search 'Engineer' but select 'Draft' filter
      await tester.enterText(find.byType(TextField), 'Engineer');
      await tester.tap(find.widgetWithText(FilterChip, 'Draft (1)'));
      await tester.pumpAndSettle();

      // Should show empty matching results
      expect(find.text('No matching job posts'), findsOneWidget);
      expect(find.text('Clear filters'), findsOneWidget);

      // Tap 'Clear filters'
      await tester.tap(find.text('Clear filters'));
      await tester.pumpAndSettle();

      // All 3 posts restored
      expect(find.text('Software Engineer'), findsOneWidget);
      expect(find.text('Frontend Developer'), findsOneWidget);
      expect(find.text('QA Lead'), findsOneWidget);
    });

    testWidgets('displays error state with retry button on API failure',
        (tester) async {
      setMobileView(tester);
      int attempts = 0;
      final mock = MockHttpClient((req) async {
        attempts++;
        if (attempts == 1) {
          return http.Response(
            jsonEncode({'message': 'Server connection timeout'}),
            500,
            headers: {'content-type': 'application/json'},
          );
        }
        return http.Response(jsonEncode([]), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterJobPostService(apiClient: apiClient);

      await tester
          .pumpWidget(buildTestable(RecruiterJobPostsScreen(service: service)));
      await tester.pumpAndSettle();

      expect(find.text('Server connection timeout'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);

      // Tap retry
      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();

      expect(find.text('No job posts found'), findsOneWidget);
    });

    testWidgets('tapping a job post card opens the detail screen',
        (tester) async {
      setMobileView(tester);
      final mock = MockHttpClient((req) async {
        if (req.url.path.contains('recruiter/postings/job-1')) {
          return http.Response(jsonEncode(_mockJobPosts[0]), 200,
              headers: {'content-type': 'application/json'});
        }
        return http.Response(jsonEncode(_mockJobPosts), 200,
            headers: {'content-type': 'application/json'});
      });
      final apiClient = ApiClient(httpClient: mock, overrideToken: 'dummy');
      final service = RecruiterJobPostService(apiClient: apiClient);

      await tester
          .pumpWidget(buildTestable(RecruiterJobPostsScreen(service: service)));
      await tester.pumpAndSettle();

      // Tap the Software Engineer card
      await tester.tap(find.text('Software Engineer'));
      await tester.pumpAndSettle();

      // Verifies navigation to RecruiterJobPostDetailScreen
      expect(find.text('Job Post Details'), findsOneWidget);
      expect(find.text('Overview & Compensation'), findsOneWidget);
      expect(find.text('12 Candidate(s)'), findsOneWidget);
    });
  });
}
