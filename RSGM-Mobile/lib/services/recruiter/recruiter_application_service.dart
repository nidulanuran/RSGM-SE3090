import '../../models/recruiter/recruiter_applicant.dart';
import '../api_client.dart';

/// Read-only service for viewing recruiter applications and downloading applicant CVs.
///
/// Communicates with backend endpoints:
/// - GET `/api/recruiter/applications` (supports optional `jobId` query parameter)
/// - GET `/api/recruiter/applications/{id}`
/// - GET `/api/recruiter/applications/{id}/cv`
class RecruiterApplicationService {
  RecruiterApplicationService({ApiClient? apiClient})
      : _apiClient = apiClient ?? ApiClient();

  final ApiClient _apiClient;

  /// Fetches applications for the authenticated recruiter, optionally filtered by [jobId].
  ///
  /// Corresponds to GET `/api/recruiter/applications`.
  Future<List<RecruiterApplicant>> getApplications({String? jobId}) async {
    final queryParams = <String, dynamic>{
      if (jobId != null && jobId.trim().isNotEmpty) 'jobId': jobId.trim(),
    };

    final response = await _apiClient.get(
      'recruiter/applications',
      queryParameters: queryParams.isNotEmpty ? queryParams : null,
    );

    if (response is List) {
      return response
          .whereType<Map<String, dynamic>>()
          .map(RecruiterApplicant.fromJson)
          .toList();
    }

    return const [];
  }

  /// Fetches detailed application profile by ID.
  ///
  /// Corresponds to GET `/api/recruiter/applications/{id}`.
  Future<RecruiterApplicant> getApplicationById(String id) async {
    final response = await _apiClient.get('recruiter/applications/$id');

    if (response is Map<String, dynamic>) {
      return RecruiterApplicant.fromJson(response);
    }

    throw const ApiException(
      message: 'Unexpected server response format for application.',
    );
  }

  /// Fetches CV file content for an application.
  ///
  /// Corresponds to GET `/api/recruiter/applications/{id}/cv`.
  Future<dynamic> downloadCv(String id) async {
    return _apiClient.get('recruiter/applications/$id/cv');
  }
}
