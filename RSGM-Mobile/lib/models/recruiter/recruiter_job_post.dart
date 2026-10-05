/// Represents a required skill associated with a job posting.
class RecruiterJobSkill {
  const RecruiterJobSkill({
    required this.id,
    required this.name,
    required this.weight,
  });

  final String id;
  final String name;
  final double weight;

  factory RecruiterJobSkill.fromJson(Map<String, dynamic> json) {
    return RecruiterJobSkill(
      id: json['id']?.toString() ?? '',
      name: json['name']?.toString() ?? '',
      weight: (json['weight'] as num?)?.toDouble() ?? 1.0,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'weight': weight,
      };
}

/// Represents a job posting created or managed by a recruiter.
///
/// Corresponds to backend `RecruiterJobPostingResponse`.
class RecruiterJobPost {
  const RecruiterJobPost({
    required this.id,
    this.jobRequisitionId,
    required this.title,
    required this.company,
    this.companyLogoUrl,
    required this.location,
    this.description,
    required this.employmentType,
    required this.workMode,
    required this.responsibilities,
    required this.requirements,
    required this.experienceLevel,
    this.minExperienceYears,
    this.minSalary,
    this.maxSalary,
    this.currency,
    this.applicationDeadline,
    required this.status,
    required this.applicantCount,
    required this.createdAt,
    this.requiredSkills = const [],
  });

  final String id;
  final String? jobRequisitionId;
  final String title;
  final String company;
  final String? companyLogoUrl;
  final String location;
  final String? description;
  final String employmentType;
  final String workMode;
  final String responsibilities;
  final String requirements;
  final String experienceLevel;
  final int? minExperienceYears;
  final double? minSalary;
  final double? maxSalary;
  final String? currency;
  final DateTime? applicationDeadline;
  final String status;
  final int applicantCount;
  final DateTime createdAt;
  final List<RecruiterJobSkill> requiredSkills;

  /// Whether the posting is publicly active and receiving applications.
  bool get isPublished =>
      status.toLowerCase() == 'published' || status == '1';

  /// Whether the posting has been closed to applications.
  bool get isClosed => status.toLowerCase() == 'closed' || status == '2';

  /// Whether the posting is still in draft state.
  bool get isDraft => status.toLowerCase() == 'draft' || status == '0';

  /// Human-friendly representation of the employment type.
  String get employmentTypeLabel {
    switch (employmentType.toLowerCase()) {
      case 'fulltime':
      case '0':
        return 'Full-Time';
      case 'parttime':
      case '1':
        return 'Part-Time';
      case 'contract':
      case '2':
        return 'Contract';
      case 'internship':
      case '3':
        return 'Internship';
      default:
        return employmentType;
    }
  }

  /// Human-friendly representation of the work mode.
  String get workModeLabel {
    switch (workMode.toLowerCase()) {
      case 'onsite':
      case '0':
        return 'On-Site';
      case 'remote':
      case '1':
        return 'Remote';
      case 'hybrid':
      case '2':
        return 'Hybrid';
      default:
        return workMode;
    }
  }

  /// Human-friendly representation of the experience level.
  String get experienceLevelLabel {
    switch (experienceLevel.toLowerCase()) {
      case 'entry':
      case '0':
        return 'Entry Level';
      case 'junior':
      case '1':
        return 'Junior';
      case 'mid':
      case '2':
        return 'Mid Level';
      case 'senior':
      case '3':
        return 'Senior';
      default:
        return experienceLevel;
    }
  }

  /// Formatted salary string for display.
  String get salaryRangeFormatted {
    final curr = currency?.trim().isNotEmpty == true ? currency! : 'LKR';
    if (minSalary != null && maxSalary != null) {
      return '$curr ${_formatAmount(minSalary!)} – ${_formatAmount(maxSalary!)}';
    } else if (minSalary != null) {
      return 'From $curr ${_formatAmount(minSalary!)}';
    } else if (maxSalary != null) {
      return 'Up to $curr ${_formatAmount(maxSalary!)}';
    }
    return 'Salary not disclosed';
  }

  static String _formatAmount(double amount) {
    if (amount >= 1000) {
      final whole = amount.toInt();
      return whole.toString().replaceAllMapped(
            RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
            (m) => '${m[1]},',
          );
    }
    return amount.toStringAsFixed(0);
  }

  factory RecruiterJobPost.fromJson(Map<String, dynamic> json) {
    DateTime? deadline;
    if (json['applicationDeadline'] != null) {
      final raw = json['applicationDeadline'].toString();
      deadline = DateTime.tryParse(raw);
    }

    final rawSkills = json['requiredSkills'];
    final skills = <RecruiterJobSkill>[];
    if (rawSkills is List) {
      for (final item in rawSkills) {
        if (item is Map<String, dynamic>) {
          skills.add(RecruiterJobSkill.fromJson(item));
        }
      }
    }

    return RecruiterJobPost(
      id: json['id']?.toString() ?? '',
      jobRequisitionId: json['jobRequisitionId']?.toString(),
      title: json['title']?.toString() ?? '',
      company: json['company']?.toString() ?? '',
      companyLogoUrl: json['companyLogoUrl']?.toString(),
      location: json['location']?.toString() ?? '',
      description: json['description']?.toString(),
      employmentType: json['employmentType']?.toString() ?? 'FullTime',
      workMode: json['workMode']?.toString() ?? 'OnSite',
      responsibilities: json['responsibilities']?.toString() ?? '',
      requirements: json['requirements']?.toString() ?? '',
      experienceLevel: json['experienceLevel']?.toString() ?? 'Entry',
      minExperienceYears: (json['minExperienceYears'] as num?)?.toInt(),
      minSalary: (json['minSalary'] as num?)?.toDouble(),
      maxSalary: (json['maxSalary'] as num?)?.toDouble(),
      currency: json['currency']?.toString(),
      applicationDeadline: deadline,
      status: json['status']?.toString() ?? 'Draft',
      applicantCount: (json['applicantCount'] as num?)?.toInt() ?? 0,
      createdAt: json['createdAt'] != null
          ? (DateTime.tryParse(json['createdAt'].toString())?.toUtc() ??
              DateTime.now().toUtc())
          : DateTime.now().toUtc(),
      requiredSkills: skills,
    );
  }
}
