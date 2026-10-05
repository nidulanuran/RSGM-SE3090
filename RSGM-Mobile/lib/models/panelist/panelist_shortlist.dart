class PanelistShortlistCandidate {
  const PanelistShortlistCandidate({
    required this.id,
    required this.candidate,
    required this.email,
    required this.status,
    required this.shortlistRank,
  });

  final String id;
  final String candidate;
  final String email;
  final String status;
  final int shortlistRank;

  factory PanelistShortlistCandidate.fromJson(
    Map<String, dynamic> json,
  ) {
    return PanelistShortlistCandidate(
      id: json['id']?.toString() ?? '',
      candidate: json['candidate']?.toString() ?? 'Unknown candidate',
      email: json['email']?.toString() ?? '',
      status: json['status']?.toString() ?? '',
      shortlistRank: (json['shortlistRank'] as num?)?.toInt() ?? 0,
    );
  }
}

class PanelistShortlist {
  const PanelistShortlist({
    required this.jobPostingId,
    required this.jobTitle,
    required this.recruiter,
    required this.submittedAt,
    required this.candidates,
  });

  final String jobPostingId;
  final String jobTitle;
  final String recruiter;
  final DateTime submittedAt;
  final List<PanelistShortlistCandidate> candidates;

  factory PanelistShortlist.fromJson(Map<String, dynamic> json) {
    final candidateData = json['candidates'];

    return PanelistShortlist(
      jobPostingId: json['jobPostingId']?.toString() ?? '',
      jobTitle: json['jobTitle']?.toString() ?? 'Unknown job',
      recruiter: json['recruiter']?.toString() ?? '',
      submittedAt:
          DateTime.tryParse(json['submittedAt']?.toString() ?? '') ??
              DateTime.now(),
      candidates: candidateData is List
          ? candidateData
              .whereType<Map<String, dynamic>>()
              .map(PanelistShortlistCandidate.fromJson)
              .toList()
          : const [],
    );
  }
}