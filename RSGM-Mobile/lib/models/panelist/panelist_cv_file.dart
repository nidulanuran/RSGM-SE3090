import 'dart:typed_data';

/// Represents a candidate CV downloaded from the backend.
class PanelistCvFile {
  const PanelistCvFile({
    required this.bytes,
    required this.fileName,
    required this.contentType,
  });

  final Uint8List bytes;
  final String fileName;
  final String contentType;

  bool get isPdf =>
      contentType.toLowerCase().contains('pdf') ||
      fileName.toLowerCase().endsWith('.pdf');

  bool get isEmpty => bytes.isEmpty;
}