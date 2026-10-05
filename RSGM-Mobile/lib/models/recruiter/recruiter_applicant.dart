/// Represents the breakdown of a specific skill comparison for an applicant.
class RecruiterSkillMatchBreakdown {
  const RecruiterSkillMatchBreakdown({
    required this.skillId,
    required this.skillName,
    required this.requiredWeight,
    this.candidateProficiency,
    this.proficiencyLabel,
    required this.contributionPercentage,
    required this.matched,
  });

  final String skillId;
  final String skillName;
  final double requiredWeight;
  final int? candidateProficiency;
  final String? proficiencyLabel;
  final double contributionPercentage;
  final bool matched;

  factory RecruiterSkillMatchBreakdown.fromJson(Map<String, dynamic> json) {
    return RecruiterSkillMatchBreakdown(
      skillId: json['skillId']?.toString() ?? '',
      skillName: json['skillName']?.toString() ?? '',
      requiredWeight: (json['requiredWeight'] as num?)?.toDouble() ?? 1.0,
      candidateProficiency: (json['candidateProficiency'] as num?)?.toInt(),
      proficiencyLabel: json['proficiencyLabel']?.toString(),
      contributionPercentage: (json['contributionPercentage'] as num?)?.toDouble() ?? 0.0,
      matched: json['matched'] == true,
    );
  }

  Map<String, dynamic> toJson() => {
        'skillId': skillId,
        'skillName': skillName,
        'requiredWeight': requiredWeight,
        'candidateProficiency': candidateProficiency,
        'proficiencyLabel': proficiencyLabel,
        'contributionPercentage': contributionPercentage,
        'matched': matched,
      };
}

/// Represents an applicant who applied to one of the recruiter's job postings.
///
/// Corresponds to backend `RecruiterApplicantDto`.
class RecruiterApplicant {
  const RecruiterApplicant({
    required this.id,
    required this.jobPostingId,
    required this.jobTitle,
    required this.candidateId,
    required this.fullName,
    required this.email,
    this.phoneNumber,
    this.headline,
    this.location,
    this.bio,
    this.linkedInUrl,
    this.gitHubUrl,
    this.portfolioUrl,
    required this.hasCv,
    this.cvFileName,
    required this.status,
    this.shortlistRank,
    required this.appliedAt,
    required this.matchScore,
    required this.exactMatchScore,
    required this.matchExplanation,
    this.matchBreakdown = const [],
    this.skills = const [],
    this.matchedSkills = const [],
    this.missingSkills = const [],
    this.education = const [],
    this.workExperience = const [],
  });

  final String id;
  final String jobPostingId;
  final String jobTitle;
  final String candidateId;
  final String fullName;
  final String email;
  final String? phoneNumber;
  final String? headline;
  final String? location;
  final String? bio;
  final String? linkedInUrl;
  final String? gitHubUrl;
  final String? portfolioUrl;
  final bool hasCv;
  final String? cvFileName;
  final String status;
  final int? shortlistRank;
  final DateTime appliedAt;
  final int matchScore;
  final double exactMatchScore;
  final String matchExplanation;
  final List<RecruiterSkillMatchBreakdown> matchBreakdown;
  final List<String> skills;
  final List<String> matchedSkills;
  final List<String> missingSkills;
  final List<String> education;
  final List<String> workExperience;

  /// Human-friendly applicant initials for avatar circle.
  String get initials {
    final parts = fullName.trim().split(RegExp(r'\s+'));
    if (parts.isEmpty || parts.first.isEmpty) return '?';
    if (parts.length == 1) return parts.first.substring(0, 1).toUpperCase();
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }

  /// Formatted status label for display.
  String get statusLabel {
    switch (status.toLowerCase()) {
      case 'underreview':
      case 'review':
      case '0':
        return 'Under Review';
      case 'shortlisted':
      case '1':
        return 'Shortlisted';
      case 'interview':
      case '2':
        return 'Interview';
      case 'offer':
      case '3':
        return 'Offer';
      case 'rejected':
      case '4':
        return 'Rejected';
      case 'withdrawn':
      case '5':
        return 'Withdrawn';
      case 'hired':
      case '6':
        return 'Hired';
      case 'offerdeclined':
      case '7':
        return 'Offer Declined';
      default:
        return status;
    }
  }

  bool get isShortlisted =>
      status.toLowerCase() == 'shortlisted' || status == '1';
  bool get isUnderReview =>
      status.toLowerCase() == 'underreview' ||
      status.toLowerCase() == 'review' ||
      status == '0';
  bool get isInterview =>
      status.toLowerCase() == 'interview' || status == '2';
  bool get isRejected =>
      status.toLowerCase() == 'rejected' || status == '4';

  factory RecruiterApplicant.fromJson(Map<String, dynamic> json) {
    final rawBreakdown = json['matchBreakdown'];
    final breakdown = <RecruiterSkillMatchBreakdown>[];
    if (rawBreakdown is List) {
      for (final item in rawBreakdown) {
        if (item is Map<String, dynamic>) {
          breakdown.add(RecruiterSkillMatchBreakdown.fromJson(item));
        }
      }
    }

    List<String> parseStringList(dynamic list) {
      if (list is List) {
        return list
            .map((e) => e?.toString() ?? '')
            .where((s) => s.isNotEmpty)
            .toList();
      }
      return const [];
    }

    return RecruiterApplicant(
      id: json['id']?.toString() ?? '',
      jobPostingId: json['jobPostingId']?.toString() ?? '',
      jobTitle: json['jobTitle']?.toString() ?? '',
      candidateId: json['candidateId']?.toString() ?? '',
      fullName: json['fullName']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      phoneNumber: json['phoneNumber']?.toString(),
      headline: json['headline']?.toString(),
      location: json['location']?.toString(),
      bio: json['bio']?.toString(),
      linkedInUrl: json['linkedInUrl']?.toString(),
      gitHubUrl: json['gitHubUrl']?.toString(),
      portfolioUrl: json['portfolioUrl']?.toString(),
      hasCv: json['hasCv'] == true,
      cvFileName: json['cvFileName']?.toString(),
      status: json['status']?.toString() ?? 'UnderReview',
      shortlistRank: (json['shortlistRank'] as num?)?.toInt(),
      appliedAt: json['appliedAt'] != null
          ? (DateTime.tryParse(json['appliedAt'].toString())?.toUtc() ??
              DateTime.now().toUtc())
          : DateTime.now().toUtc(),
      matchScore: (json['matchScore'] as num?)?.toInt() ?? 0,
      exactMatchScore: (json['exactMatchScore'] as num?)?.toDouble() ?? 0.0,
      matchExplanation: json['matchExplanation']?.toString() ?? '',
      matchBreakdown: breakdown,
      skills: parseStringList(json['skills']),
      matchedSkills: parseStringList(json['matchedSkills']),
      missingSkills: parseStringList(json['missingSkills']),
      education: parseStringList(json['education']),
      workExperience: parseStringList(json['workExperience']),
    );
  }
}
