class PanelistCandidateSkill {
  const PanelistCandidateSkill({
    required this.name,
    required this.proficiencyLevel,
  });

  final String name;
  final int proficiencyLevel;

  factory PanelistCandidateSkill.fromJson(Map<String, dynamic> json) {
    return PanelistCandidateSkill(
      name: json['name']?.toString() ?? '',
      proficiencyLevel:
          (json['proficiencyLevel'] as num?)?.toInt() ?? 0,
    );
  }
}

class PanelistWorkExperience {
  const PanelistWorkExperience({
    required this.jobTitle,
    required this.companyName,
    this.location,
    this.startDate,
    this.endDate,
    required this.isCurrent,
  });

  final String jobTitle;
  final String companyName;
  final String? location;
  final String? startDate;
  final String? endDate;
  final bool isCurrent;

  factory PanelistWorkExperience.fromJson(
    Map<String, dynamic> json,
  ) {
    return PanelistWorkExperience(
      jobTitle: json['jobTitle']?.toString() ?? '',
      companyName: json['companyName']?.toString() ?? '',
      location: json['location']?.toString(),
      startDate: json['startDate']?.toString(),
      endDate: json['endDate']?.toString(),
      isCurrent: json['isCurrent'] == true,
    );
  }
}

class PanelistEducation {
  const PanelistEducation({
    required this.degree,
    required this.institution,
    this.fieldOfStudy,
    this.startDate,
    this.endDate,
    required this.isCurrent,
  });

  final String degree;
  final String institution;
  final String? fieldOfStudy;
  final String? startDate;
  final String? endDate;
  final bool isCurrent;

  factory PanelistEducation.fromJson(Map<String, dynamic> json) {
    return PanelistEducation(
      degree: json['degree']?.toString() ?? '',
      institution: json['institution']?.toString() ?? '',
      fieldOfStudy: json['fieldOfStudy']?.toString(),
      startDate: json['startDate']?.toString(),
      endDate: json['endDate']?.toString(),
      isCurrent: json['isCurrent'] == true,
    );
  }
}

class PanelistCandidate {
  const PanelistCandidate({
    required this.id,
    required this.fullName,
    required this.email,
    this.jobTitle,
    this.headline,
    this.phoneNumber,
    this.location,
    this.bio,
    required this.skills,
    required this.workExperience,
    required this.education,
    this.linkedInUrl,
    this.gitHubUrl,
    this.portfolioUrl,
    required this.hasCv,
    this.cvFileName,
  });

  final String id;
  final String fullName;
  final String email;

  final String? jobTitle;
  final String? headline;
  final String? phoneNumber;
  final String? location;
  final String? bio;

  final List<PanelistCandidateSkill> skills;
  final List<PanelistWorkExperience> workExperience;
  final List<PanelistEducation> education;

  final String? linkedInUrl;
  final String? gitHubUrl;
  final String? portfolioUrl;

  final bool hasCv;
  final String? cvFileName;

  factory PanelistCandidate.fromJson(Map<String, dynamic> json) {
    final skillData = json['skills'];
    final workData = json['workExperience'];
    final educationData = json['education'];

    return PanelistCandidate(
      id: json['id']?.toString() ?? '',
      fullName:
          json['fullName']?.toString() ?? 'Unknown candidate',
      email: json['email']?.toString() ?? '',
      jobTitle: json['jobTitle']?.toString(),
      headline: json['headline']?.toString(),
      phoneNumber: json['phoneNumber']?.toString(),
      location: json['location']?.toString(),
      bio: json['bio']?.toString(),

      skills: skillData is List
          ? skillData
              .whereType<Map<String, dynamic>>()
              .map(PanelistCandidateSkill.fromJson)
              .toList()
          : const [],

      workExperience: workData is List
          ? workData
              .whereType<Map<String, dynamic>>()
              .map(PanelistWorkExperience.fromJson)
              .toList()
          : const [],

      education: educationData is List
          ? educationData
              .whereType<Map<String, dynamic>>()
              .map(PanelistEducation.fromJson)
              .toList()
          : const [],

      linkedInUrl: json['linkedInUrl']?.toString(),
      gitHubUrl: json['gitHubUrl']?.toString(),
      portfolioUrl: json['portfolioUrl']?.toString(),

      hasCv: json['hasCv'] == true,
      cvFileName: json['cvFileName']?.toString(),
    );
  }
}