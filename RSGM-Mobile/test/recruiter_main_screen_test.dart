import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/screens/recruiter/recruiter_main_screen.dart';
import 'package:rsgm_mobile/services/api_client.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_application_service.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_availability_service.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_job_post_service.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_requisition_service.dart';
import 'package:rsgm_mobile/theme/app_theme.dart';

class _MockHttpClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    return http.StreamedResponse(
      Stream.value(utf8.encode('[]')),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

RecruiterAvailabilityService _createMockService() {
  final client = ApiClient(httpClient: _MockHttpClient(), overrideToken: 'fake');
  return RecruiterAvailabilityService(apiClient: client);
}

RecruiterJobPostService _createMockJobPostService() {
  final client = ApiClient(httpClient: _MockHttpClient(), overrideToken: 'fake');
  return RecruiterJobPostService(apiClient: client);
}

RecruiterApplicationService _createMockApplicationService() {
  final client = ApiClient(httpClient: _MockHttpClient(), overrideToken: 'fake');
  return RecruiterApplicationService(apiClient: client);
}

RecruiterRequisitionService _createMockRequisitionService() {
  final client = ApiClient(httpClient: _MockHttpClient(), overrideToken: 'fake');
  return RecruiterRequisitionService(apiClient: client);
}

void main() {
  Widget buildTestableWidget(Widget child) {
    return MaterialApp(
      theme: AppTheme.light(),
      home: child,
    );
  }

  group('RecruiterMainScreen widget tests', () {
    testWidgets('renders shell with AppBar, Recruiter badge, and 4 bottom navigation tabs', (tester) async {
      await tester.pumpWidget(buildTestableWidget(RecruiterMainScreen(
        availabilityService: _createMockService(),
        jobPostService: _createMockJobPostService(),
        applicationService: _createMockApplicationService(),
        requisitionService: _createMockRequisitionService(),
      )));
      await tester.pumpAndSettle();

      // Check AppBar branding and role badge
      expect(find.text('RSGM'), findsOneWidget);
      expect(find.text('Recruiter'), findsOneWidget);
      expect(find.byIcon(Icons.logout_rounded), findsOneWidget);

      // Check navigation tabs in BottomNavigationBar and initial content
      expect(find.byType(BottomNavigationBar), findsOneWidget);
      expect(find.text('Availability'), findsWidgets);
      expect(find.text('Job Posts'), findsOneWidget);
      expect(find.text('Applicants'), findsOneWidget);
      expect(find.text('Requisitions'), findsOneWidget);
    });

    testWidgets('switching bottom navigation tabs switches displayed content', (tester) async {
      await tester.pumpWidget(buildTestableWidget(RecruiterMainScreen(
        availabilityService: _createMockService(),
        jobPostService: _createMockJobPostService(),
        applicationService: _createMockApplicationService(),
        requisitionService: _createMockRequisitionService(),
      )));
      await tester.pumpAndSettle();

      // Tap 'Job Posts' (real screen)
      await tester.tap(find.text('Job Posts'));
      await tester.pumpAndSettle();
      expect(find.text('My Job Posts'), findsOneWidget);
      expect(find.text('No job posts found'), findsOneWidget);

      // Tap 'Applicants' (real screen)
      await tester.tap(find.text('Applicants'));
      await tester.pumpAndSettle();
      expect(find.text('Applicants'), findsWidgets);
      expect(find.text('No applicants yet'), findsOneWidget);

      // Tap 'Requisitions' (real screen)
      await tester.tap(find.text('Requisitions'));
      await tester.pumpAndSettle();
      expect(find.text('Requisitions'), findsWidgets);
      expect(find.text('No requisitions yet'), findsOneWidget);

      // Tap back to 'Availability' (real screen)
      await tester.tap(find.text('Availability'));
      await tester.pumpAndSettle();
      expect(find.text('Availability'), findsWidgets);
    });

    testWidgets('tapping logout button displays confirmation dialog', (tester) async {
      await tester.pumpWidget(buildTestableWidget(RecruiterMainScreen(
        availabilityService: _createMockService(),
        jobPostService: _createMockJobPostService(),
        applicationService: _createMockApplicationService(),
        requisitionService: _createMockRequisitionService(),
      )));
      await tester.pumpAndSettle();

      // Tap logout icon in AppBar
      await tester.tap(find.byIcon(Icons.logout_rounded));
      await tester.pumpAndSettle();

      // Verify confirmation dialog
      expect(find.text('Sign out'), findsWidgets);
      expect(
        find.text('Are you sure you want to sign out of your Recruiter account?'),
        findsOneWidget,
      );
      expect(find.text('Cancel'), findsOneWidget);

      // Tap Cancel closes dialog
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();
      expect(
        find.text('Are you sure you want to sign out of your Recruiter account?'),
        findsNothing,
      );
    });
  });
}
