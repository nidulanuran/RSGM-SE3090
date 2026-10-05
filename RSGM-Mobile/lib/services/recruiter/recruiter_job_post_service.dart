import '../../models/recruiter/recruiter_job_post.dart';
import '../api_client.dart';

/// Read-only service for viewing recruiter job postings and details.
///
/// Communicates with backend endpoints:
/// - GET `/api/recruiter/postings`
/// - GET `/api/recruiter/postings/{id}`
class RecruiterJobPostService {
  RecruiterJobPostService({ApiClient? apiClient})
      : _apiClient = apiClient ?? ApiClient();

  final ApiClient _apiClient;

  /// Fetches all job postings owned by the authenticated recruiter.
  ///
  /// Corresponds to GET `/api/recruiter/postings`.
  Future<List<RecruiterJobPost>> getMyJobPostings() async {
    final response = await _apiClient.get('recruiter/postings');

    if (response is List) {
      return response
          .whereType<Map<String, dynamic>>()
          .map(RecruiterJobPost.fromJson)
          .toList();
    }

    return const [];
  }

  /// Fetches details for a single job posting by ID.
  ///
  /// Corresponds to GET `/api/recruiter/postings/{id}`.
  Future<RecruiterJobPost> getJobPostingById(String id) async {
    final response = await _apiClient.get('recruiter/postings/$id');

    if (response is Map<String, dynamic>) {
      return RecruiterJobPost.fromJson(response);
    }

    throw const ApiException(
      message: 'Unexpected server response format for job posting.',
    );
  }
}
