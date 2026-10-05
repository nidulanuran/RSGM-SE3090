class HrBusyTime {
  const HrBusyTime({
    required this.id,
    required this.title,
    required this.startsAt,
    required this.endsAt,
    this.description,
  });

  final String id;
  final String title;
  final String? description;
  final DateTime startsAt;
  final DateTime endsAt;

  factory HrBusyTime.fromJson(Map<String, dynamic> json) {
    return HrBusyTime(
      id: json['id']?.toString() ?? '',
      title: json['title']?.toString() ?? '',
      description: json['description']?.toString(),
      startsAt: DateTime.tryParse(json['startsAt']?.toString() ?? '')?.toUtc() ??
          DateTime.now().toUtc(),
      endsAt: DateTime.tryParse(json['endsAt']?.toString() ?? '')?.toUtc() ??
          DateTime.now().toUtc(),
    );
  }
}
