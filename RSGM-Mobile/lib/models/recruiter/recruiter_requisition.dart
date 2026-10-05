/// Requisition approval workflow status.
///
/// Backend numeric representation: 0=Draft, 1=Submitted, 2=Rejected, 3=Approved.
enum RequisitionStatus {
  draft,
  submitted,
  rejected,
  approved;

  String get label {
    switch (this) {
      case RequisitionStatus.draft:
        return 'Draft';
      case RequisitionStatus.submitted:
        return 'Submitted';
      case RequisitionStatus.rejected:
        return 'Rejected';
      case RequisitionStatus.approved:
        return 'Approved';
    }
  }

  static RequisitionStatus fromJson(dynamic value) {
    if (value is int) {
      switch (value) {
        case 0:
          return RequisitionStatus.draft;
        case 1:
          return RequisitionStatus.submitted;
        case 2:
          return RequisitionStatus.rejected;
        case 3:
          return RequisitionStatus.approved;
      }
    } else if (value is String) {
      final lower = value.trim().toLowerCase();
      if (lower.contains('draft') || lower == '0') return RequisitionStatus.draft;
      if (lower.contains('submit') || lower == '1') return RequisitionStatus.submitted;
      if (lower.contains('reject') || lower == '2') return RequisitionStatus.rejected;
      if (lower.contains('approv') || lower == '3') return RequisitionStatus.approved;
    }
    return RequisitionStatus.draft;
  }
}

/// Requisition employment type.
///
/// Backend numeric representation: 0=FullTime, 1=PartTime, 2=Contract, 3=Internship.
enum RequisitionEmploymentType {
  fullTime,
  partTime,
  contract,
  internship;

  String get label {
    switch (this) {
      case RequisitionEmploymentType.fullTime:
        return 'Full-Time';
      case RequisitionEmploymentType.partTime:
        return 'Part-Time';
      case RequisitionEmploymentType.contract:
        return 'Contract';
      case RequisitionEmploymentType.internship:
        return 'Internship';
    }
  }

  static RequisitionEmploymentType fromJson(dynamic value) {
    if (value is int) {
      switch (value) {
        case 0:
          return RequisitionEmploymentType.fullTime;
        case 1:
          return RequisitionEmploymentType.partTime;
        case 2:
          return RequisitionEmploymentType.contract;
        case 3:
          return RequisitionEmploymentType.internship;
      }
    } else if (value is String) {
      final lower = value.trim().toLowerCase();
      if (lower.contains('part') || lower == '1') return RequisitionEmploymentType.partTime;
      if (lower.contains('contract') || lower == '2') return RequisitionEmploymentType.contract;
      if (lower.contains('intern') || lower == '3') return RequisitionEmploymentType.internship;
      return RequisitionEmploymentType.fullTime;
    }
    return RequisitionEmploymentType.fullTime;
  }
}

/// Requisition work mode.
///
/// Backend numeric representation: 0=OnSite, 1=Remote, 2=Hybrid.
enum RequisitionWorkMode {
  onSite,
  remote,
  hybrid;

  String get label {
    switch (this) {
      case RequisitionWorkMode.onSite:
        return 'On-Site';
      case RequisitionWorkMode.remote:
        return 'Remote';
      case RequisitionWorkMode.hybrid:
        return 'Hybrid';
    }
  }

  static RequisitionWorkMode fromJson(dynamic value) {
    if (value is int) {
      switch (value) {
        case 0:
          return RequisitionWorkMode.onSite;
        case 1:
          return RequisitionWorkMode.remote;
        case 2:
          return RequisitionWorkMode.hybrid;
      }
    } else if (value is String) {
      final lower = value.trim().toLowerCase();
      if (lower.contains('remote') || lower == '1') return RequisitionWorkMode.remote;
      if (lower.contains('hybrid') || lower == '2') return RequisitionWorkMode.hybrid;
      return RequisitionWorkMode.onSite;
    }
    return RequisitionWorkMode.onSite;
  }
}

/// Requisition experience level.
///
/// Backend numeric representation: 0=Entry, 1=Junior, 2=Mid, 3=Senior, 4=Lead.
enum RequisitionExperienceLevel {
  entry,
  junior,
  mid,
  senior,
  lead;

  String get label {
    switch (this) {
      case RequisitionExperienceLevel.entry:
        return 'Entry Level';
      case RequisitionExperienceLevel.junior:
        return 'Junior';
      case RequisitionExperienceLevel.mid:
        return 'Mid Level';
      case RequisitionExperienceLevel.senior:
        return 'Senior';
      case RequisitionExperienceLevel.lead:
        return 'Lead / Principal';
    }
  }

  static RequisitionExperienceLevel fromJson(dynamic value) {
    if (value is int) {
      switch (value) {
        case 0:
          return RequisitionExperienceLevel.entry;
        case 1:
          return RequisitionExperienceLevel.junior;
        case 2:
          return RequisitionExperienceLevel.mid;
        case 3:
          return RequisitionExperienceLevel.senior;
        case 4:
          return RequisitionExperienceLevel.lead;
      }
    } else if (value is String) {
      final lower = value.trim().toLowerCase();
      if (lower.contains('lead') || lower == '4') return RequisitionExperienceLevel.lead;
      if (lower.contains('senior') || lower == '3') return RequisitionExperienceLevel.senior;
      if (lower.contains('mid') || lower == '2') return RequisitionExperienceLevel.mid;
      if (lower.contains('junior') || lower == '1') return RequisitionExperienceLevel.junior;
      return RequisitionExperienceLevel.entry;
    }
    return RequisitionExperienceLevel.entry;
  }
}

/// Represents a job requisition created by a recruiter and reviewed by HR.
///
/// Corresponds to backend `JobRequisitionResponse`.
class RecruiterRequisition {
  const RecruiterRequisition({
    required this.id,
    required this.companyId,
    required this.companyName,
    required this.recruiterId,
    required this.recruiterName,
    required this.positionTitle,
    required this.department,
    required this.headcount,
    required this.employmentType,
    required this.workMode,
    required this.location,
    required this.experienceLevel,
    this.minExperienceYears,
    this.minSalary,
    this.maxSalary,
    required this.currency,
    this.description,
    this.responsibilities,
    this.requirements,
    this.justification,
    required this.status,
    this.hrFeedback,
    this.reviewedByUserId,
    this.reviewedByName,
    this.reviewedAt,
    required this.createdAt,
    this.updatedAt,
    this.submittedAt,
    this.approvedAt,
  });

  final String id;
  final String companyId;
  final String companyName;
  final String recruiterId;
  final String recruiterName;
  final String positionTitle;
  final String department;
  final int headcount;
  final RequisitionEmploymentType employmentType;
  final RequisitionWorkMode workMode;
  final String location;
  final RequisitionExperienceLevel experienceLevel;
  final int? minExperienceYears;
  final double? minSalary;
  final double? maxSalary;
  final String currency;
  final String? description;
  final String? responsibilities;
  final String? requirements;
  final String? justification;
  final RequisitionStatus status;
  final String? hrFeedback;
  final String? reviewedByUserId;
  final String? reviewedByName;
  final DateTime? reviewedAt;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final DateTime? submittedAt;
  final DateTime? approvedAt;

  bool get isApproved => status == RequisitionStatus.approved;
  bool get isRejected => status == RequisitionStatus.rejected;
  bool get isSubmitted => status == RequisitionStatus.submitted;
  bool get isDraft => status == RequisitionStatus.draft;

  /// Formatted salary string for display.
  String get salaryRangeFormatted {
    final curr = currency.trim().isNotEmpty ? currency : 'LKR';
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

  factory RecruiterRequisition.fromJson(Map<String, dynamic> json) {
    DateTime? parseDate(dynamic value) {
      if (value == null) return null;
      return DateTime.tryParse(value.toString())?.toUtc();
    }

    return RecruiterRequisition(
      id: json['id']?.toString() ?? '',
      companyId: json['companyId']?.toString() ?? '',
      companyName: json['companyName']?.toString() ?? '',
      recruiterId: json['recruiterId']?.toString() ?? '',
      recruiterName: json['recruiterName']?.toString() ?? '',
      positionTitle: json['positionTitle']?.toString() ?? '',
      department: json['department']?.toString() ?? '',
      headcount: (json['headcount'] as num?)?.toInt() ?? 1,
      employmentType: RequisitionEmploymentType.fromJson(json['employmentType']),
      workMode: RequisitionWorkMode.fromJson(json['workMode']),
      location: json['location']?.toString() ?? '',
      experienceLevel: RequisitionExperienceLevel.fromJson(json['experienceLevel']),
      minExperienceYears: (json['minExperienceYears'] as num?)?.toInt(),
      minSalary: (json['minSalary'] as num?)?.toDouble(),
      maxSalary: (json['maxSalary'] as num?)?.toDouble(),
      currency: json['currency']?.toString() ?? 'LKR',
      description: json['description']?.toString(),
      responsibilities: json['responsibilities']?.toString(),
      requirements: json['requirements']?.toString(),
      justification: json['justification']?.toString(),
      status: RequisitionStatus.fromJson(json['status']),
      hrFeedback: json['hrFeedback']?.toString(),
      reviewedByUserId: json['reviewedByUserId']?.toString(),
      reviewedByName: json['reviewedByName']?.toString(),
      reviewedAt: parseDate(json['reviewedAt']),
      createdAt: parseDate(json['createdAt']) ?? DateTime.now().toUtc(),
      updatedAt: parseDate(json['updatedAt']),
      submittedAt: parseDate(json['submittedAt']),
      approvedAt: parseDate(json['approvedAt']),
    );
  }
}
