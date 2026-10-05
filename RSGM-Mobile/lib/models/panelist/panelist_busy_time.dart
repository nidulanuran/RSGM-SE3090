/// Model representing an unavailable period for a hiring panelist.
///
/// Uses the existing backend UserBusyTime / BusyTimeRequest structure.
class PanelistBusyTime {
  const PanelistBusyTime({
    required this.id,
    required this.title,
    required this.startsAt,
    required this.endsAt,
    this.description,
    this.cancelledInterviews,
  });

  final String id;
  final String title;
  final String? description;
  final DateTime startsAt;
  final DateTime endsAt;
  final int? cancelledInterviews;

  Duration get duration => endsAt.difference(startsAt);

  bool get isCurrent {
    final now = DateTime.now().toUtc();
    return now.isAfter(startsAt) && now.isBefore(endsAt);
  }

  bool get isUpcoming {
    final now = DateTime.now().toUtc();
    return startsAt.isAfter(now);
  }

  factory PanelistBusyTime.fromJson(Map<String, dynamic> json) {
    return PanelistBusyTime(
      id: json['id']?.toString() ?? '',
      title: json['title']?.toString() ?? '',
      description: json['description']?.toString(),
      startsAt: json['startsAt'] != null
          ? (DateTime.tryParse(json['startsAt'].toString())?.toUtc() ??
              DateTime.now().toUtc())
          : DateTime.now().toUtc(),
      endsAt: json['endsAt'] != null
          ? (DateTime.tryParse(json['endsAt'].toString())?.toUtc() ??
              DateTime.now().toUtc())
          : DateTime.now().toUtc(),
      cancelledInterviews: (json['cancelledInterviews'] as num?)?.toInt(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'title': title.trim(),
      'startsAt': startsAt.toUtc().toIso8601String(),
      'endsAt': endsAt.toUtc().toIso8601String(),
      if (description != null && description!.trim().isNotEmpty)
        'description': description!.trim(),
    };
  }
}