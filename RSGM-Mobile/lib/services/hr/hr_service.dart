import '../../models/hr/hr_models.dart';
import '../../models/recruiter/recruiter_requisition.dart';
import '../api_client.dart';

class HrService {
  HrService({ApiClient? apiClient}) : _api = apiClient ?? ApiClient();
  final ApiClient _api;

  Future<HrDashboardStats> getDashboardStats() async {
    final response = await _api.get('hr/dashboard/stats');
    if (response is Map<String, dynamic>) return HrDashboardStats.fromJson(response);
    throw const ApiException(message: 'Unexpected HR dashboard response.');
  }

  Future<List<RecruiterRequisition>> getRequisitions() async {
    final response = await _api.get('hr/requisitions');
    if (response is List) {
      return response.whereType<Map<String, dynamic>>().map(RecruiterRequisition.fromJson).toList();
    }
    return const [];
  }

  Future<void> approveRequisition(String id) async {
    await _api.post('hr/requisitions/$id/approve', null);
  }

  Future<void> rejectRequisition(String id, String feedback) async {
    await _api.post('hr/requisitions/$id/reject', {'feedback': feedback});
  }

  Future<HrWorkflowResponse> getWorkflows() async {
    final response = await _api.get('hr/dashboard/workflows');
    if (response is Map<String, dynamic>) return HrWorkflowResponse.fromJson(response);
    throw const ApiException(message: 'Unexpected workflow response.');
  }

  Future<HrAnalytics> getAnalytics() async {
    final response = await _api.get('hr/analytics');
    if (response is Map<String, dynamic>) return HrAnalytics.fromJson(response);
    throw const ApiException(message: 'Unexpected analytics response.');
  }
}
