import '../recruiter/recruiter_requisition.dart';

class HrDashboardStats {
  const HrDashboardStats({
    required this.pendingRequisitions,
    required this.activeJobPostings,
    required this.candidatesInPipeline,
    required this.upcomingInterviews,
    required this.offersAwaitingApproval,
    required this.hiresThisMonth,
    this.generatedAt,
  });

  final int pendingRequisitions;
  final int activeJobPostings;
  final int candidatesInPipeline;
  final int upcomingInterviews;
  final int offersAwaitingApproval;
  final int hiresThisMonth;
  final DateTime? generatedAt;

  factory HrDashboardStats.fromJson(Map<String, dynamic> json) => HrDashboardStats(
        pendingRequisitions: (json['pendingRequisitions'] as num?)?.toInt() ?? 0,
        activeJobPostings: (json['activeJobPostings'] as num?)?.toInt() ?? 0,
        candidatesInPipeline: (json['candidatesInPipeline'] as num?)?.toInt() ?? 0,
        upcomingInterviews: (json['upcomingInterviews'] as num?)?.toInt() ?? 0,
        offersAwaitingApproval: (json['offersAwaitingApproval'] as num?)?.toInt() ?? 0,
        hiresThisMonth: (json['hiresThisMonth'] as num?)?.toInt() ?? 0,
        generatedAt: DateTime.tryParse(json['generatedAt']?.toString() ?? ''),
      );
}

class HrWorkflow {
  const HrWorkflow({
    required this.id,
    required this.name,
    required this.stage,
    required this.daysOpen,
    required this.inactiveDays,
    required this.health,
  });

  final String id;
  final String name;
  final String stage;
  final int daysOpen;
  final int inactiveDays;
  final String health;

  factory HrWorkflow.fromJson(Map<String, dynamic> json) => HrWorkflow(
        id: json['id']?.toString() ?? '',
        name: json['name']?.toString() ?? 'Hiring workflow',
        stage: json['stage']?.toString() ?? 'Unknown',
        daysOpen: (json['daysOpen'] as num?)?.toInt() ?? 0,
        inactiveDays: (json['inactiveDays'] as num?)?.toInt() ?? 0,
        health: json['health']?.toString() ?? 'on-track',
      );
}

class HrWorkflowResponse {
  const HrWorkflowResponse({required this.workflows, this.generatedAt});
  final List<HrWorkflow> workflows;
  final DateTime? generatedAt;

  factory HrWorkflowResponse.fromJson(Map<String, dynamic> json) => HrWorkflowResponse(
        workflows: (json['workflows'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(HrWorkflow.fromJson)
            .toList(),
        generatedAt: DateTime.tryParse(json['generatedAt']?.toString() ?? ''),
      );
}

class HrAnalyticsStat {
  const HrAnalyticsStat({
    required this.averageTimeToHireDays,
    required this.offerAcceptanceRate,
    required this.candidatesInPipeline,
    required this.newPipelineThisMonth,
    required this.requisitionApprovalRate,
  });
  final double averageTimeToHireDays;
  final double offerAcceptanceRate;
  final int candidatesInPipeline;
  final int newPipelineThisMonth;
  final double requisitionApprovalRate;

  factory HrAnalyticsStat.fromJson(Map<String, dynamic> json) => HrAnalyticsStat(
        averageTimeToHireDays: (json['averageTimeToHireDays'] as num?)?.toDouble() ?? 0,
        offerAcceptanceRate: (json['offerAcceptanceRate'] as num?)?.toDouble() ?? 0,
        candidatesInPipeline: (json['candidatesInPipeline'] as num?)?.toInt() ?? 0,
        newPipelineThisMonth: (json['newPipelineThisMonth'] as num?)?.toInt() ?? 0,
        requisitionApprovalRate: (json['requisitionApprovalRate'] as num?)?.toDouble() ?? 0,
      );
}

class HrFunnelItem {
  const HrFunnelItem({required this.stage, required this.count});
  final String stage;
  final int count;
  factory HrFunnelItem.fromJson(Map<String, dynamic> json) => HrFunnelItem(
        stage: json['stage']?.toString() ?? '',
        count: (json['count'] as num?)?.toInt() ?? 0,
      );
}

class HrAnalytics {
  const HrAnalytics({required this.stats, required this.funnel, this.generatedAt});
  final HrAnalyticsStat stats;
  final List<HrFunnelItem> funnel;
  final DateTime? generatedAt;

  factory HrAnalytics.fromJson(Map<String, dynamic> json) => HrAnalytics(
        stats: HrAnalyticsStat.fromJson((json['stats'] as Map<String, dynamic>?) ?? const {}),
        funnel: (json['funnel'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(HrFunnelItem.fromJson)
            .toList(),
        generatedAt: DateTime.tryParse(json['generatedAt']?.toString() ?? ''),
      );
}

typedef HrRequisition = RecruiterRequisition;
