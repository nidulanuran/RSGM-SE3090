import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../config/api_config.dart';

/// Centralized HTTP API client for authenticated communication with the RSGM backend.
class ApiClient {
  ApiClient({http.Client? httpClient, this.overrideToken})
      : _httpClient = httpClient ?? http.Client();

  final http.Client _httpClient;
  final String? overrideToken;

  /// Retrieves the stored JWT session token from SharedPreferences.
  Future<String?> getToken() async {
    if (overrideToken != null && overrideToken!.isNotEmpty) {
      return overrideToken;
    }

    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('rsgm_session_token');
  }

  /// Builds a normalized URI using [ApiConfig.baseUrl] and the provided [path].
  Uri buildUri(String path, [Map<String, dynamic>? queryParameters]) {
    final base = ApiConfig.baseUrl.replaceAll(RegExp(r'/+$'), '');
    final cleanPath = path.replaceAll(RegExp(r'^/+'), '');
    final fullUrl = cleanPath.isEmpty ? base : '$base/$cleanPath';
    final parsed = Uri.parse(fullUrl);

    if (queryParameters == null || queryParameters.isEmpty) {
      return parsed;
    }

    final sanitizedParams = <String, String>{};

    for (final entry in queryParameters.entries) {
      if (entry.value != null) {
        sanitizedParams[entry.key] = entry.value.toString();
      }
    }

    return parsed.replace(
      queryParameters: {
        ...parsed.queryParameters,
        ...sanitizedParams,
      },
    );
  }

  Future<Map<String, String>> _buildHeaders([
    Map<String, String>? extraHeaders,
  ]) async {
    final token = await getToken();

    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
    };

    if (extraHeaders != null) {
      headers.addAll(extraHeaders);
    }

    return headers;
  }

  /// Headers for multipart requests.
  ///
  /// Do not set Content-Type manually because [http.MultipartRequest]
  /// generates the correct multipart boundary.
  Future<Map<String, String>> _buildMultipartHeaders() async {
    final token = await getToken();

    return <String, String>{
      'Accept': 'application/json',
      if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
    };
  }

  Future<dynamic> get(
    String path, {
    Map<String, String>? headers,
    Map<String, dynamic>? queryParameters,
  }) async {
    final uri = buildUri(path, queryParameters);
    final requestHeaders = await _buildHeaders(headers);

    try {
      final response = await _httpClient.get(uri, headers: requestHeaders);
      return _handleResponse(response);
    } on ApiException {
      rethrow;
    } catch (_) {
      throw const ApiException(
        message:
            'Unable to connect to the RSGM server. Check your connection and ensure the backend is running.',
      );
    }
  }

  Future<dynamic> post(
    String path,
    dynamic body, {
    Map<String, String>? headers,
    Map<String, dynamic>? queryParameters,
  }) async {
    final uri = buildUri(path, queryParameters);
    final requestHeaders = await _buildHeaders(headers);

    try {
      final response = await _httpClient.post(
        uri,
        headers: requestHeaders,
        body: body != null ? jsonEncode(body) : null,
      );

      return _handleResponse(response);
    } on ApiException {
      rethrow;
    } catch (_) {
      throw const ApiException(
        message:
            'Unable to connect to the RSGM server. Check your connection and ensure the backend is running.',
      );
    }
  }

  /// Uploads one in-memory file as multipart/form-data.
  ///
  /// This works on Flutter Web, Windows and mobile because it does not depend
  /// on dart:io file paths.
  Future<dynamic> postMultipartBytes(
    String path, {
    required String fieldName,
    required String fileName,
    required Uint8List bytes,
    required String contentType,
    Map<String, String>? fields,
  }) async {
    final uri = buildUri(path);
    final requestHeaders = await _buildMultipartHeaders();

    try {
      final request = http.MultipartRequest('POST', uri)
        ..headers.addAll(requestHeaders);

      if (fields != null) {
        request.fields.addAll(fields);
      }

      request.files.add(
        http.MultipartFile.fromBytes(
          fieldName,
          bytes,
          filename: fileName,
          contentType: MediaType.parse(contentType),
        ),
      );

      final streamedResponse = await _httpClient.send(request);
      final response = await http.Response.fromStream(streamedResponse);

      return _handleResponse(response);
    } on ApiException {
      rethrow;
    } catch (_) {
      throw const ApiException(
        message:
            'Unable to upload the file. Check your connection and ensure the backend is running.',
      );
    }
  }

  Future<dynamic> put(
    String path,
    dynamic body, {
    Map<String, String>? headers,
    Map<String, dynamic>? queryParameters,
  }) async {
    final uri = buildUri(path, queryParameters);
    final requestHeaders = await _buildHeaders(headers);

    try {
      final response = await _httpClient.put(
        uri,
        headers: requestHeaders,
        body: body != null ? jsonEncode(body) : null,
      );

      return _handleResponse(response);
    } on ApiException {
      rethrow;
    } catch (_) {
      throw const ApiException(
        message:
            'Unable to connect to the RSGM server. Check your connection and ensure the backend is running.',
      );
    }
  }

  Future<dynamic> patch(
    String path,
    dynamic body, {
    Map<String, String>? headers,
    Map<String, dynamic>? queryParameters,
  }) async {
    final uri = buildUri(path, queryParameters);
    final requestHeaders = await _buildHeaders(headers);

    try {
      final response = await _httpClient.patch(
        uri,
        headers: requestHeaders,
        body: body != null ? jsonEncode(body) : null,
      );

      return _handleResponse(response);
    } on ApiException {
      rethrow;
    } catch (_) {
      throw const ApiException(
        message:
            'Unable to connect to the RSGM server. Check your connection and ensure the backend is running.',
      );
    }
  }

  Future<dynamic> delete(
    String path, {
    Map<String, String>? headers,
    dynamic body,
    Map<String, dynamic>? queryParameters,
  }) async {
    final uri = buildUri(path, queryParameters);
    final requestHeaders = await _buildHeaders(headers);

    try {
      final response = await _httpClient.delete(
        uri,
        headers: requestHeaders,
        body: body != null ? jsonEncode(body) : null,
      );

      return _handleResponse(response);
    } on ApiException {
      rethrow;
    } catch (_) {
      throw const ApiException(
        message:
            'Unable to connect to the RSGM server. Check your connection and ensure the backend is running.',
      );
    }
  }

  dynamic _handleResponse(http.Response response) {
    dynamic decoded;

    if (response.body.isNotEmpty) {
      try {
        decoded = jsonDecode(response.body);
      } catch (_) {
        decoded = response.body;
      }
    }

    if (response.statusCode >= 200 && response.statusCode < 300) {
      return decoded;
    }

    throw _createException(response.statusCode, decoded);
  }

  ApiException _createException(int statusCode, dynamic decoded) {
    String message = 'Request failed with status $statusCode.';
    List<String> errorList = [];

    if (decoded is Map<String, dynamic>) {
      if (decoded['message'] is String &&
          (decoded['message'] as String).isNotEmpty) {
        message = decoded['message'] as String;
      }

      final errors = decoded['errors'];

      if (errors is List) {
        errorList = errors.map((e) => e.toString()).toList();
      } else if (errors is Map) {
        for (final val in errors.values) {
          if (val is List) {
            errorList.addAll(val.map((e) => e.toString()));
          } else if (val != null) {
            errorList.add(val.toString());
          }
        }
      }

      if (errorList.isNotEmpty && !message.contains(errorList.first)) {
        message = '$message ${errorList.join(' ')}'.trim();
      }
    } else if (decoded is String && decoded.isNotEmpty) {
      message = decoded;
    } else if (statusCode == 401) {
      message = 'Session expired or unauthorized. Please sign in again.';
    } else if (statusCode == 403) {
      message =
          'Access denied. You do not have permission to perform this action.';
    } else if (statusCode == 404) {
      message = 'The requested resource was not found.';
    }

    return ApiException(
      message: message,
      statusCode: statusCode,
      errors: errorList,
    );
  }
}

class ApiException implements Exception {
  const ApiException({
    required this.message,
    this.statusCode,
    this.errors = const [],
  });

  final String message;
  final int? statusCode;
  final List<String> errors;

  bool get isUnauthorized => statusCode == 401;
  bool get isForbidden => statusCode == 403;
  bool get isNotFound => statusCode == 404;

  @override
  String toString() => message;
}
