import '../../models/panelist/panelist_shortlist.dart';
import '../api_client.dart';
import '../../models/panelist/panelist_candidate.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import '../../models/panelist/panelist_cv_file.dart';

/// Read-only shortlist service for the Hiring Panelist mobile app.
///
/// Uses the existing backend endpoint:
/// GET /api/hiring/panelist/shortlists
///
/// Scheduling and Agentic AI actions remain web-only.
class PanelistShortlistService {
  PanelistShortlistService({ApiClient? apiClient})
      : _apiClient = apiClient ?? ApiClient();

  final ApiClient _apiClient;

  Future<List<PanelistShortlist>> getShortlists() async {
    final response =
        await _apiClient.get('hiring/panelist/shortlists');

    if (response is List) {
      return response
          .whereType<Map<String, dynamic>>()
          .map(PanelistShortlist.fromJson)
          .toList();
    }

    return const [];
  }

  /// Fetches the full read-only profile of a shortlisted candidate.
  ///
  /// [applicationId] is the shortlist candidate/application ID.
  Future<PanelistCandidate> getCandidate(String applicationId) async {
    final response = await _apiClient.get(
      'hiring/panelist/shortlists/applications/$applicationId/candidate',
    );

    if (response is Map<String, dynamic>) {
      return PanelistCandidate.fromJson(response);
    }

    throw const ApiException(
      message: 'Unexpected server response when loading candidate details.',
    );
  }
  Future<PanelistCvFile> downloadCandidateCv(String applicationId) async {
    final token = await _apiClient.getToken();

    final uri = _apiClient.buildUri(
      'hiring/panelist/shortlists/applications/$applicationId/cv',
    );

    try {
      final response = await http.get(
        uri,
        headers: {
          'Accept': '*/*',
          if (token != null && token.isNotEmpty)
            'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode >= 200 && response.statusCode < 300) {
        final contentType =
            response.headers['content-type'] ?? 'application/octet-stream';

        final fileName = _extractFileName(
            response.headers['content-disposition'],
        );

        return PanelistCvFile(
          bytes: response.bodyBytes,
          fileName: fileName,
          contentType: contentType,
        );
      }

      String message = 'Unable to download candidate CV.';

      if (response.bodyBytes.isNotEmpty) {
        try {
          final decoded = jsonDecode(
            utf8.decode(
              response.bodyBytes,
              allowMalformed: true,
            ),
          );

          if (decoded is Map<String, dynamic> &&
              decoded['message'] is String) {
            message = decoded['message'] as String;
          }
          } catch (_) {
            // Keep the default message when the response is not JSON.
          }
        }

        throw ApiException(
          message: message,
          statusCode: response.statusCode,
        );
      } on ApiException {
        rethrow;
      } catch (_) {
        throw const ApiException(
          message:
              'Unable to download the CV. Check your connection and ensure the backend is running.',
        );
      }
    }
    String _extractFileName(String? contentDisposition) {
      if (contentDisposition == null || contentDisposition.trim().isEmpty) {
        return 'candidate-cv.pdf';
      }

      final encodedMatch = RegExp(
        r"filename\*=UTF-8''([^;]+)",
        caseSensitive: false,
      ).firstMatch(contentDisposition);

      if (encodedMatch != null) {
        return Uri.decodeComponent(
          encodedMatch.group(1)!.trim(),
        );
      }

      final normalMatch = RegExp(
        r'filename="?([^";]+)"?',
        caseSensitive: false,
      ).firstMatch(contentDisposition);

      if (normalMatch != null) {
        return normalMatch.group(1)!.trim();
      }

      return 'candidate-cv.pdf';
    }
}