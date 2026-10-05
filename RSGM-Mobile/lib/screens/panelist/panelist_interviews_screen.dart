import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models/panelist/panelist_interview.dart';
import '../../services/api_client.dart';
import '../../services/panelist/panelist_interview_service.dart';
import '../../theme/app_theme.dart';

class PanelistInterviewsScreen extends StatefulWidget {
  const PanelistInterviewsScreen({
    super.key,
    this.service,
  });

  final PanelistInterviewService? service;

  @override
  State<PanelistInterviewsScreen> createState() =>
      _PanelistInterviewsScreenState();
}

class _PanelistInterviewsScreenState
    extends State<PanelistInterviewsScreen> {
  static const Color amber = Color(0xFFD97706);
  static const Color amberSoft = Color(0xFFFFFBEB);
  static const Color amberBorder = Color(0xFFFDE68A);

  late final PanelistInterviewService _service;

  List<PanelistInterview> _interviews = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? PanelistInterviewService();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final interviews = await _service.getInterviews();

      interviews.sort(
        (a, b) => b.scheduledAt.compareTo(a.scheduledAt),
      );

      if (!mounted) return;

      setState(() {
        _interviews = interviews;
        _loading = false;
      });
    } on ApiException catch (e) {
      if (!mounted) return;

      setState(() {
        _error = e.message;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) return;

      setState(() {
        _error = 'Unable to load assigned interviews.';
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppTheme.background,
      child: SafeArea(
        child: RefreshIndicator(
          color: amber,
          onRefresh: _load,
          child: SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _buildRolePill(),
                const SizedBox(height: 14),
                Text(
                  'My interviews',
                  style: Theme.of(context).textTheme.headlineMedium,
                ),
                const SizedBox(height: 8),
                Text(
                  'View your assigned interview schedule and submitted feedback.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 24),
                _buildContent(),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildRolePill() {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: 12,
        vertical: 8,
      ),
      decoration: BoxDecoration(
        color: amberSoft,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: amberBorder),
      ),
      child: const Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.event_note_rounded,
            size: 15,
            color: amber,
          ),
          SizedBox(width: 7),
          Text(
            'ASSIGNED INTERVIEWS',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              color: amber,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildContent() {
    if (_loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 70),
        child: Center(
          child: CircularProgressIndicator(color: amber),
        ),
      );
    }

    if (_error != null) {
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: const Color(0xFFFEF2F2),
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: const Color(0xFFFECACA),
          ),
        ),
        child: Column(
          children: [
            const Icon(
              Icons.error_outline_rounded,
              size: 36,
              color: Color(0xFFDC2626),
            ),
            const SizedBox(height: 12),
            Text(
              _error!,
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: Color(0xFF991B1B),
                fontWeight: FontWeight.w500,
              ),
            ),
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: _load,
              icon: const Icon(Icons.refresh_rounded),
              label: const Text('Try again'),
            ),
          ],
        ),
      );
    }

    if (_interviews.isEmpty) {
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.all(28),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(22),
          border: Border.all(color: AppTheme.border),
        ),
        child: const Column(
          children: [
            Icon(
              Icons.event_busy_rounded,
              size: 42,
              color: AppTheme.muted,
            ),
            SizedBox(height: 12),
            Text(
              'No interviews assigned yet.',
              style: TextStyle(
                fontWeight: FontWeight.w600,
                color: AppTheme.ink,
              ),
            ),
          ],
        ),
      );
    }

    return Column(
      children: _interviews
          .map(
            (interview) => Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: _InterviewCard(interview: interview),
            ),
          )
          .toList(),
    );
  }
}

class _InterviewCard extends StatelessWidget {
  const _InterviewCard({
    required this.interview,
  });

  static const Color amber = Color(0xFFD97706);
  static const Color amberSoft = Color(0xFFFFFBEB);

  final PanelistInterview interview;

  @override
  Widget build(BuildContext context) {
    final feedback = interview.feedback;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: AppTheme.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x08000000),
            blurRadius: 16,
            offset: Offset(0, 5),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: amberSoft,
                  borderRadius: BorderRadius.circular(14),
                ),
                child: const Icon(
                  Icons.calendar_month_rounded,
                  color: amber,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      interview.candidate,
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                        color: AppTheme.ink,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${interview.job} • ${interview.type}',
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppTheme.muted,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              _StatusChip(status: interview.status),
            ],
          ),

          const SizedBox(height: 16),

          _DetailRow(
            icon: Icons.schedule_rounded,
            text: DateFormat('EEE, d MMM yyyy • h:mm a')
                .format(interview.scheduledAt.toLocal()),
          ),

          if (interview.locationOrLink != null &&
              interview.locationOrLink!.trim().isNotEmpty) ...[
            const SizedBox(height: 9),
            _DetailRow(
              icon: interview.type.toLowerCase() == 'online'
                  ? Icons.videocam_outlined
                  : Icons.location_on_outlined,
              text: interview.locationOrLink!,
            ),
          ],

          if (interview.candidateEmail != null &&
              interview.candidateEmail!.trim().isNotEmpty) ...[
            const SizedBox(height: 9),
            _DetailRow(
              icon: Icons.email_outlined,
              text: interview.candidateEmail!,
            ),
          ],

          if (feedback != null) ...[
            const SizedBox(height: 16),
            const Divider(height: 1),
            const SizedBox(height: 12),

            Theme(
              data: Theme.of(context).copyWith(
                dividerColor: Colors.transparent,
              ),
              child: ExpansionTile(
                tilePadding: EdgeInsets.zero,
                childrenPadding: EdgeInsets.zero,
                title: Row(
                  children: [
                    const Icon(
                      Icons.check_circle_outline_rounded,
                      size: 19,
                      color: Color(0xFF059669),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        feedback.recommendation == null ||
                                feedback.recommendation!.isEmpty
                            ? 'Feedback submitted'
                            : 'Feedback: ${feedback.recommendation}',
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
                          color: Color(0xFF047857),
                        ),
                      ),
                    ),
                  ],
                ),
                children: [
                  _FeedbackView(feedback: feedback),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({
    required this.status,
  });

  final String status;

  @override
  Widget build(BuildContext context) {
    Color background;
    Color foreground;

    switch (status.toLowerCase()) {
      case 'cancelled':
        background = const Color(0xFFFEF2F2);
        foreground = const Color(0xFFB91C1C);
        break;

      case 'completed':
        background = const Color(0xFFECFDF5);
        foreground = const Color(0xFF047857);
        break;

      default:
        background = const Color(0xFFFFFBEB);
        foreground = const Color(0xFFD97706);
    }

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: 10,
        vertical: 5,
      ),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        status.isEmpty ? 'Unknown' : status,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w700,
          color: foreground,
        ),
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({
    required this.icon,
    required this.text,
  });

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SizedBox(width: 4),
        Icon(
          icon,
          size: 17,
          color: const Color(0xFFD97706),
        ),
        const SizedBox(width: 9),
        Expanded(
          child: Text(
            text,
            style: const TextStyle(
              fontSize: 13,
              color: AppTheme.muted,
            ),
          ),
        ),
      ],
    );
  }
}

class _FeedbackView extends StatelessWidget {
  const _FeedbackView({
    required this.feedback,
  });

  final PanelistInterviewFeedback feedback;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFFAFAFA),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _rating(
            'Technical skills',
            feedback.technicalSkills,
          ),
          _rating(
            'Problem solving',
            feedback.problemSolving,
          ),
          _rating(
            'Communication',
            feedback.communication,
          ),
          _rating(
            'Culture fit',
            feedback.cultureFit,
          ),

          if (feedback.desiredSalary != null) ...[
            const SizedBox(height: 10),
            Text(
              'Expected salary: '
              '${feedback.desiredSalaryCurrency ?? ''} '
              '${feedback.desiredSalary!.toStringAsFixed(2)}',
              style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: AppTheme.ink,
              ),
            ),
          ],

          if (feedback.comments != null &&
              feedback.comments!.trim().isNotEmpty) ...[
            const SizedBox(height: 10),
            const Text(
              'Comments',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: AppTheme.ink,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              feedback.comments!,
              style: const TextStyle(
                fontSize: 12,
                height: 1.4,
                color: AppTheme.muted,
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _rating(String label, int? value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 7),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 12,
                color: AppTheme.muted,
              ),
            ),
          ),
          Text(
            value == null ? '—' : '$value / 5',
            style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              color: AppTheme.ink,
            ),
          ),
        ],
      ),
    );
  }
}