class PanelistInterviewFeedback {
  const PanelistInterviewFeedback({
    this.technicalSkills,
    this.problemSolving,
    this.communication,
    this.cultureFit,
    this.recommendation,
    this.comments,
    this.desiredSalary,
    this.desiredSalaryCurrency,
  });

  final int? technicalSkills;
  final int? problemSolving;
  final int? communication;
  final int? cultureFit;
  final String? recommendation;
  final String? comments;
  final double? desiredSalary;
  final String? desiredSalaryCurrency;

  factory PanelistInterviewFeedback.fromJson(Map<String, dynamic> json) {
    return PanelistInterviewFeedback(
      technicalSkills: (json['technicalSkills'] as num?)?.toInt(),
      problemSolving: (json['problemSolving'] as num?)?.toInt(),
      communication: (json['communication'] as num?)?.toInt(),
      cultureFit: (json['cultureFit'] as num?)?.toInt(),
      recommendation: json['recommendation']?.toString(),
      comments: json['comments']?.toString(),
      desiredSalary: (json['desiredSalary'] as num?)?.toDouble(),
      desiredSalaryCurrency: json['desiredSalaryCurrency']?.toString(),
    );
  }
}

class PanelistInterview {
  const PanelistInterview({
    required this.id,
    required this.candidate,
    required this.job,
    required this.type,
    required this.scheduledAt,
    required this.status,
    this.candidateEmail,
    this.locationOrLink,
    this.jobPostingId,
    this.hrManagerId,
    this.feedback,
  });

  final String id;
  final String candidate;
  final String? candidateEmail;
  final String job;
  final String type;
  final DateTime scheduledAt;
  final String status;
  final String? locationOrLink;
  final String? jobPostingId;
  final String? hrManagerId;
  final PanelistInterviewFeedback? feedback;

  bool get isUpcoming =>
      scheduledAt.isAfter(DateTime.now()) && status != 'Cancelled';

  bool get hasFeedback => feedback != null;

  factory PanelistInterview.fromJson(Map<String, dynamic> json) {
    final feedbackJson = json['feedback'];

    return PanelistInterview(
      id: json['id']?.toString() ?? '',
      candidate: json['candidate']?.toString() ?? 'Unknown candidate',
      candidateEmail: json['candidateEmail']?.toString(),
      job: json['job']?.toString() ?? 'Unknown job',
      type: json['type']?.toString() ?? '',
      scheduledAt:
          DateTime.tryParse(json['scheduledAt']?.toString() ?? '') ??
              DateTime.now(),
      status: json['status']?.toString() ?? '',
      locationOrLink: json['locationOrLink']?.toString(),
      jobPostingId: json['jobPostingId']?.toString(),
      hrManagerId: json['hrManagerId']?.toString(),
      feedback: feedbackJson is Map<String, dynamic>
          ? PanelistInterviewFeedback.fromJson(feedbackJson)
          : null,
    );
  }
}