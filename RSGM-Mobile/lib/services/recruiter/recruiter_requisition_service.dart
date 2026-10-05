import '../../models/recruiter/recruiter_requisition.dart';
import '../api_client.dart';

/// Read-only service for viewing recruiter job requisitions and review statuses.
///
/// Communicates with backend endpoints:
/// - GET `/api/recruiter/requisitions`
/// - GET `/api/recruiter/requisitions/{id}`
class RecruiterRequisitionService {
  RecruiterRequisitionService({ApiClient? apiClient})
      : _apiClient = apiClient ?? ApiClient();

  final ApiClient _apiClient;

  /// Fetches all job requisitions submitted by the authenticated recruiter.
  ///
  /// Corresponds to GET `/api/recruiter/requisitions`.
  Future<List<RecruiterRequisition>> getMyRequisitions() async {
    final response = await _apiClient.get('recruiter/requisitions');

    if (response is List) {
      return response
          .whereType<Map<String, dynamic>>()
          .map(RecruiterRequisition.fromJson)
          .toList();
    }

    return const [];
  }

  /// Fetches single job requisition details including HR review feedback.
  ///
  /// Corresponds to GET `/api/recruiter/requisitions/{id}`.
  Future<RecruiterRequisition> getRequisitionById(String id) async {
    final response = await _apiClient.get('recruiter/requisitions/$id');

    if (response is Map<String, dynamic>) {
      return RecruiterRequisition.fromJson(response);
    }

    throw const ApiException(
      message: 'Unexpected server response format for requisition.',
    );
  }
}
