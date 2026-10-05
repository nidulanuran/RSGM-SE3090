import '../../models/hr/hr_busy_time.dart';
import '../api_client.dart';

class HrAvailabilityService {
  HrAvailabilityService({ApiClient? apiClient})
      : _apiClient = apiClient ?? ApiClient();

  final ApiClient _apiClient;

  Future<List<HrBusyTime>> getBusyTimes() async {
    final response = await _apiClient.get('hiring/busy-times');
    if (response is List) {
      return response
          .whereType<Map<String, dynamic>>()
          .map(HrBusyTime.fromJson)
          .toList();
    }
    return const [];
  }

  Future<void> createBusyTime({
    required String title,
    required DateTime startsAt,
    required DateTime endsAt,
    String? description,
  }) async {
    if (!startsAt.isAfter(DateTime.now())) {
      throw const ApiException(
        message: 'Start date and time must be in the future.',
        statusCode: 400,
      );
    }

    if (!startsAt.isBefore(endsAt)) {
      throw const ApiException(
        message: 'End time must be after the start time.',
        statusCode: 400,
      );
    }

    final startMinutes = startsAt.hour * 60 + startsAt.minute;
    final endMinutes = endsAt.hour * 60 + endsAt.minute;
    if (startMinutes < 8 * 60 || endMinutes > 17 * 60) {
      throw const ApiException(
        message: 'Select a time between 8:00 AM and 5:00 PM.',
        statusCode: 400,
      );
    }

    await _apiClient.post('hiring/busy-times', {
      'title': title.trim(),
      'startsAt': startsAt.toUtc().toIso8601String(),
      'endsAt': endsAt.toUtc().toIso8601String(),
      if (description != null && description.trim().isNotEmpty)
        'description': description.trim(),
    });
  }

  Future<void> deleteBusyTime(String id) async {
    await _apiClient.delete('hiring/busy-times/$id');
  }
}
