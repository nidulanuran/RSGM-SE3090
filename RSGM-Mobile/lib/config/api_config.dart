import 'package:flutter/foundation.dart';

class ApiConfig {
  static String get baseUrl {
    if (kIsWeb) {
      // Flutter Web
      return 'http://localhost:5248/api';
    }

    // Physical Android phone connected using:
    // adb reverse tcp:5248 tcp:5248
    return 'http://localhost:5248/api';
  }
}