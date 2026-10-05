import '../../models/panelist/panelist_interview.dart';
import '../api_client.dart';

/// Read-only interview service for the Hiring Panelist mobile app.
///
/// Uses the existing backend endpoint:
/// GET /api/panelist/interviews
///
/// Interview actions such as rescheduling, cancelling,
/// feedback submission, and recommendations remain web-only.
class PanelistInterviewService {
  PanelistInterviewService({ApiClient? apiClient})
      : _apiClient = apiClient ?? ApiClient();

  final ApiClient _apiClient;

  Future<List<PanelistInterview>> getInterviews() async {
    final response = await _apiClient.get('panelist/interviews');

    if (response is List) {
      return response
          .whereType<Map<String, dynamic>>()
          .map(PanelistInterview.fromJson)
          .toList();
    }

    return const [];
  }
}