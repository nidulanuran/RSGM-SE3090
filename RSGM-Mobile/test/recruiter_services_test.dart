import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rsgm_mobile/services/api_client.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_availability_service.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_job_post_service.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_application_service.dart';
import 'package:rsgm_mobile/services/recruiter/recruiter_requisition_service.dart';

/// Lightweight mock HTTP client extending `http.BaseClient` without external packages.
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
  group('RecruiterAvailabilityService tests', () {
    test('getBusyTimes parses array of busy times', () async {
      final mock = MockHttpClient((req) async {
        expect(req.method, 'GET');
        expect(req.url.path, '/api/hiring/busy-times');
        return http.Response(
          jsonEncode([
            {
              'id': 'bt-1',
              'title': 'Team Sync',
              'startsAt': '2026-10-10T09:00:00.000Z',
              'endsAt': '2026-10-10T10:00:00.000Z',
            }
          ]),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterAvailabilityService(apiClient: apiClient);

      final list = await service.getBusyTimes();
      expect(list.length, 1);
      expect(list.first.id, 'bt-1');
      expect(list.first.title, 'Team Sync');
    });

    test('createBusyTime validates same calendar day client-side', () async {
      final apiClient = ApiClient(overrideToken: 'fake-jwt');
      final service = RecruiterAvailabilityService(apiClient: apiClient);

      // Different days
      final start = DateTime.utc(2026, 10, 10, 9, 0);
      final end = DateTime.utc(2026, 10, 11, 10, 0);

      expect(
        () => service.createBusyTime(
          title: 'Multi-day',
          startsAt: start,
          endsAt: end,
        ),
        throwsA(isA<ApiException>().having(
          (e) => e.message,
          'message',
          contains('same calendar day'),
        )),
      );
    });

    test('createBusyTime sends correct payload and parses response', () async {
      final start = DateTime.utc(2026, 10, 10, 4, 0); // 9:30 AM Colombo
      final end = DateTime.utc(2026, 10, 10, 6, 0); // 11:30 AM Colombo

      final mock = MockHttpClient((req) async {
        expect(req.method, 'POST');
        expect(req.url.path, '/api/hiring/busy-times');
        final body = jsonDecode(req.body) as Map<String, dynamic>;
        expect(body['title'], 'Doctor Appointment');
        expect(body['startsAt'], start.toIso8601String());
        expect(body['endsAt'], end.toIso8601String());
        expect(body['description'], 'Medical visit');

        return http.Response(
          jsonEncode({
            'id': 'bt-created-1',
            'title': 'Doctor Appointment',
            'startsAt': start.toIso8601String(),
            'endsAt': end.toIso8601String(),
            'description': 'Medical visit',
            'cancelledInterviews': 1,
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterAvailabilityService(apiClient: apiClient);

      final result = await service.createBusyTime(
        title: 'Doctor Appointment',
        startsAt: start,
        endsAt: end,
        description: 'Medical visit',
      );

      expect(result.id, 'bt-created-1');
      expect(result.cancelledInterviews, 1);
    });

    test('deleteBusyTime sends DELETE request with id in path', () async {
      var called = false;
      final mock = MockHttpClient((req) async {
        expect(req.method, 'DELETE');
        expect(req.url.path, '/api/hiring/busy-times/bt-to-delete');
        called = true;
        return http.Response('', 204);
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterAvailabilityService(apiClient: apiClient);

      await service.deleteBusyTime('bt-to-delete');
      expect(called, isTrue);
    });
  });

  group('RecruiterJobPostService tests', () {
    test('getMyJobPostings fetches list of postings', () async {
      final mock = MockHttpClient((req) async {
        expect(req.method, 'GET');
        expect(req.url.path, '/api/recruiter/postings');
        return http.Response(
          jsonEncode([
            {
              'id': 'job-1',
              'title': 'Senior Flutter Dev',
              'company': 'Tech Corp',
              'location': 'Colombo',
              'employmentType': 'FullTime',
              'workMode': 'Remote',
              'responsibilities': 'Dev',
              'requirements': 'Flutter',
              'experienceLevel': 'Senior',
              'status': 'Published',
              'applicantCount': 5,
              'createdAt': '2026-10-01T00:00:00.000Z',
            }
          ]),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterJobPostService(apiClient: apiClient);

      final list = await service.getMyJobPostings();
      expect(list.length, 1);
      expect(list.first.id, 'job-1');
      expect(list.first.isPublished, isTrue);
    });

    test('getJobPostingById fetches single job posting', () async {
      final mock = MockHttpClient((req) async {
        expect(req.method, 'GET');
        expect(req.url.path, '/api/recruiter/postings/job-42');
        return http.Response(
          jsonEncode({
            'id': 'job-42',
            'title': 'Backend Lead',
            'company': 'Tech Corp',
            'location': 'Remote',
            'employmentType': 'FullTime',
            'workMode': 'Remote',
            'responsibilities': 'Architecture',
            'requirements': '.NET, Postgres',
            'experienceLevel': 'Senior',
            'status': 'Draft',
            'applicantCount': 0,
            'createdAt': '2026-10-01T00:00:00.000Z',
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterJobPostService(apiClient: apiClient);

      final job = await service.getJobPostingById('job-42');
      expect(job.id, 'job-42');
      expect(job.isDraft, isTrue);
    });
  });

  group('RecruiterApplicationService tests', () {
    test('getApplications includes jobId query parameter when provided', () async {
      final mock = MockHttpClient((req) async {
        expect(req.method, 'GET');
        expect(req.url.path, '/api/recruiter/applications');
        expect(req.url.queryParameters['jobId'], 'job-999');
        return http.Response(
          jsonEncode([
            {
              'id': 'app-1',
              'jobPostingId': 'job-999',
              'jobTitle': 'Senior Dev',
              'candidateId': 'c-1',
              'fullName': 'John Doe',
              'email': 'john@example.com',
              'hasCv': true,
              'status': 'Shortlisted',
              'appliedAt': '2026-10-01T00:00:00.000Z',
              'matchScore': 85,
              'exactMatchScore': 80.0,
              'matchExplanation': 'Good match',
            }
          ]),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterApplicationService(apiClient: apiClient);

      final list = await service.getApplications(jobId: 'job-999');
      expect(list.length, 1);
      expect(list.first.fullName, 'John Doe');
      expect(list.first.isShortlisted, isTrue);
    });

    test('getApplicationById fetches detailed applicant profile', () async {
      final mock = MockHttpClient((req) async {
        expect(req.method, 'GET');
        expect(req.url.path, '/api/recruiter/applications/app-77');
        return http.Response(
          jsonEncode({
            'id': 'app-77',
            'jobPostingId': 'job-1',
            'jobTitle': 'Architect',
            'candidateId': 'c-7',
            'fullName': 'Alice Wonderland',
            'email': 'alice@example.com',
            'hasCv': false,
            'status': 'UnderReview',
            'appliedAt': '2026-10-02T00:00:00.000Z',
            'matchScore': 90,
            'exactMatchScore': 90.0,
            'matchExplanation': 'Full match',
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterApplicationService(apiClient: apiClient);

      final app = await service.getApplicationById('app-77');
      expect(app.id, 'app-77');
      expect(app.isUnderReview, isTrue);
    });

    test('downloadCv calls CV endpoint', () async {
      final mock = MockHttpClient((req) async {
        expect(req.method, 'GET');
        expect(req.url.path, '/api/recruiter/applications/app-77/cv');
        return http.Response(
          '%PDF-1.4 dummy pdf bytes',
          200,
          headers: {'content-type': 'application/pdf'},
        );
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterApplicationService(apiClient: apiClient);

      final result = await service.downloadCv('app-77');
      expect(result, isNotNull);
    });
  });

  group('RecruiterRequisitionService tests', () {
    test('getMyRequisitions fetches requisitions with integer status values', () async {
      final mock = MockHttpClient((req) async {
        expect(req.method, 'GET');
        expect(req.url.path, '/api/recruiter/requisitions');
        return http.Response(
          jsonEncode([
            {
              'id': 'req-1',
              'companyId': 'comp-1',
              'companyName': 'Acme Inc',
              'recruiterId': 'rec-1',
              'recruiterName': 'Recruiter Bob',
              'positionTitle': 'DevOps Engineer',
              'department': 'Infra',
              'headcount': 1,
              'employmentType': 0, // FullTime
              'workMode': 1, // Remote
              'location': 'Colombo',
              'experienceLevel': 3, // Senior
              'currency': 'LKR',
              'status': 3, // Approved
              'createdAt': '2026-10-01T00:00:00.000Z',
            }
          ]),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      final list = await service.getMyRequisitions();
      expect(list.length, 1);
      expect(list.first.positionTitle, 'DevOps Engineer');
      expect(list.first.isApproved, isTrue);
    });

    test('getRequisitionById fetches single requisition details', () async {
      final mock = MockHttpClient((req) async {
        expect(req.method, 'GET');
        expect(req.url.path, '/api/recruiter/requisitions/req-55');
        return http.Response(
          jsonEncode({
            'id': 'req-55',
            'companyId': 'comp-1',
            'companyName': 'Acme Inc',
            'recruiterId': 'rec-1',
            'recruiterName': 'Recruiter Bob',
            'positionTitle': 'UI Designer',
            'department': 'Product',
            'headcount': 1,
            'employmentType': 1, // PartTime
            'workMode': 2, // Hybrid
            'location': 'Colombo',
            'experienceLevel': 1, // Junior
            'currency': 'LKR',
            'status': 1, // Submitted
            'createdAt': '2026-10-01T00:00:00.000Z',
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final apiClient = ApiClient(httpClient: mock, overrideToken: 'fake-jwt');
      final service = RecruiterRequisitionService(apiClient: apiClient);

      final req = await service.getRequisitionById('req-55');
      expect(req.id, 'req-55');
      expect(req.isSubmitted, isTrue);
    });
  });
}
