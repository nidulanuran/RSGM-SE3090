import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../config/api_config.dart';

class AuthService {
  static const _secure = FlutterSecureStorage();

  Future<Map<String, dynamic>> login({
    required String email,
    required String password,
    required bool rememberMe,
  }) async {
    final data = await _post('/auth/login', {'email': email, 'password': password});
    await _saveAuth(data, rememberMe: rememberMe);
    return data;
  }

  Future<Map<String, dynamic>> register({
    required String fullName,
    required String email,
    required String password,
  }) async {
    return _post('/auth/register', {
      'fullName': fullName,
      'email': email,
      'password': password,
    });
  }

  Future<Map<String, dynamic>> _post(String path, Map<String, dynamic> body) async {
    try {
      final response = await http.post(
        Uri.parse('${ApiConfig.baseUrl}$path'),
        headers: const {'Content-Type': 'application/json'},
        body: jsonEncode(body),
      );

      Map<String, dynamic> data = {};
      if (response.body.isNotEmpty) {
        final decoded = jsonDecode(response.body);
        if (decoded is Map<String, dynamic>) data = decoded;
      }

      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw AuthException(_errorMessage(data));
      }
      return data;
    } on AuthException {
      rethrow;
    } catch (_) {
      throw AuthException(
        'Unable to connect to the Hireon server. Check that the backend is running and the mobile API URL is correct.',
      );
    }
  }

  String _errorMessage(Map<String, dynamic> data) {
    if (data['message'] is String && (data['message'] as String).isNotEmpty) {
      final errors = data['errors'];
      if (errors is List && errors.isNotEmpty) {
        return '${data['message']} ${errors.join(' ')}';
      }
      return data['message'] as String;
    }
    if (data['errors'] is List) return (data['errors'] as List).join(' ');
    return 'Request failed. Please try again.';
  }

  Future<void> _saveAuth(Map<String, dynamic> data, {required bool rememberMe}) async {
    final token = data['token']?.toString();
    if (token == null || token.isEmpty) return;

    final roles = data['roles'] is List
        ? (data['roles'] as List).map((e) => e.toString()).toList()
        : <String>[];

    if (rememberMe) {
      await _secure.write(key: 'rsgm_token', value: token);
    } else {
      await _secure.delete(key: 'rsgm_token');
    }

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('rsgm_session_token', token);
    await prefs.setStringList('rsgm_roles', roles);
    await prefs.setString('rsgm_user', jsonEncode(data));
  }
}

class AuthException implements Exception {
  AuthException(this.message);
  final String message;
  @override
  String toString() => message;
}
