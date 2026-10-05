import '../../models/panelist/panelist_busy_time.dart';
import '../api_client.dart';

/// Service for managing Hiring Panelist busy / unavailable times.
///
/// Uses the existing backend endpoints:
/// GET    /api/hiring/busy-times
/// POST   /api/hiring/busy-times
/// DELETE /api/hiring/busy-times/{id}
class PanelistAvailabilityService {
  PanelistAvailabilityService({ApiClient? apiClient})
      : _apiClient = apiClient ?? ApiClient();

  final ApiClient _apiClient;

  /// Fetches busy times for the authenticated Hiring Panelist.
  Future<List<PanelistBusyTime>> getBusyTimes() async {
    final response = await _apiClient.get('hiring/busy-times');

    if (response is List) {
      return response
          .whereType<Map<String, dynamic>>()
          .map(PanelistBusyTime.fromJson)
          .toList();
    }

    return const [];
  }

  /// Creates a new unavailable time.
  Future<PanelistBusyTime> createBusyTime({
    required String title,
    required DateTime startsAt,
    required DateTime endsAt,
    String? description,
  }) async {
    final trimmedTitle = title.trim();

    if (trimmedTitle.isEmpty) {
      throw const ApiException(
        message: 'A title is required for the busy time period.',
        statusCode: 400,
      );
    }

    if (trimmedTitle.length > 150) {
      throw const ApiException(
        message: 'Title must not exceed 150 characters.',
        statusCode: 400,
      );
    }

    final trimmedDescription = description?.trim();

    if (trimmedDescription != null && trimmedDescription.length > 500) {
      throw const ApiException(
        message: 'Description must not exceed 500 characters.',
        statusCode: 400,
      );
    }

    if (!startsAt.isBefore(endsAt)) {
      throw const ApiException(
        message: 'Start time must be before end time.',
        statusCode: 400,
      );
    }

    const colomboOffset = Duration(hours: 5, minutes: 30);

    final localStart = startsAt.toUtc().add(colomboOffset);
    final localEnd = endsAt.toUtc().add(colomboOffset);

    if (localStart.year != localEnd.year ||
        localStart.month != localEnd.month ||
        localStart.day != localEnd.day) {
      throw const ApiException(
        message: 'Busy time start and end must be on the same calendar day.',
        statusCode: 400,
      );
    }

    final payload = <String, dynamic>{
      'title': trimmedTitle,
      'startsAt': startsAt.toUtc().toIso8601String(),
      'endsAt': endsAt.toUtc().toIso8601String(),
      if (trimmedDescription != null && trimmedDescription.isNotEmpty)
        'description': trimmedDescription,
    };

    final response =
        await _apiClient.post('hiring/busy-times', payload);

    if (response is Map<String, dynamic>) {
      return PanelistBusyTime.fromJson(response);
    }

    throw const ApiException(
      message: 'Unexpected server response when adding unavailable time.',
    );
  }

  /// Deletes an existing unavailable time.
  Future<void> deleteBusyTime(String id) async {
    await _apiClient.delete('hiring/busy-times/$id');
  }
}