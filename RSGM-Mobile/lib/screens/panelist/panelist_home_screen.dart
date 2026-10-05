import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models/panelist/panelist_interview.dart';
import '../../services/api_client.dart';
import '../../services/panelist/panelist_interview_service.dart';
import '../../theme/app_theme.dart';

class PanelistHomeScreen extends StatefulWidget {
  const PanelistHomeScreen({
    super.key,
    this.service,
  });

  final PanelistInterviewService? service;

  @override
  State<PanelistHomeScreen> createState() => _PanelistHomeScreenState();
}

class _PanelistHomeScreenState extends State<PanelistHomeScreen> {
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
        (a, b) => a.scheduledAt.compareTo(b.scheduledAt),
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
        _error = 'Unable to load your interview dashboard.';
        _loading = false;
      });
    }
  }

  int get _upcomingCount =>
      _interviews.where((interview) => interview.isUpcoming).length;

  int get _feedbackCount =>
      _interviews.where((interview) => interview.hasFeedback).length;

  PanelistInterview? get _nextInterview {
    final upcoming = _interviews
        .where((interview) => interview.isUpcoming)
        .toList();

    if (upcoming.isEmpty) return null;

    upcoming.sort(
      (a, b) => a.scheduledAt.compareTo(b.scheduledAt),
    );

    return upcoming.first;
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppTheme.background,
      child: SafeArea(
        child: RefreshIndicator(
          onRefresh: _load,
          color: amber,
          child: SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _buildRolePill(),
                const SizedBox(height: 14),
                Text(
                  'Your interview schedule',
                  style: Theme.of(context).textTheme.headlineMedium,
                ),
                const SizedBox(height: 8),
                Text(
                  'Review your assigned interviews, candidates and submitted feedback.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 28),
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
            Icons.auto_awesome_rounded,
            size: 15,
            color: amber,
          ),
          SizedBox(width: 7),
          Text(
            'HIRING PANELIST',
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
        padding: EdgeInsets.symmetric(vertical: 80),
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

    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: _StatCard(
                icon: Icons.calendar_month_rounded,
                label: 'Upcoming interviews',
                value: _upcomingCount,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _StatCard(
                icon: Icons.check_circle_outline_rounded,
                label: 'Feedback submitted',
                value: _feedbackCount,
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),
        _buildNextInterview(),
      ],
    );
  }

  Widget _buildNextInterview() {
    final interview = _nextInterview;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: AppTheme.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0A000000),
            blurRadius: 20,
            offset: Offset(0, 6),
          ),
        ],
      ),
      child: interview == null
          ? const Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Next up',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w700,
                    color: AppTheme.ink,
                  ),
                ),
                SizedBox(height: 10),
                Text(
                  'No upcoming interviews.',
                  style: TextStyle(color: AppTheme.muted),
                ),
              ],
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Next up',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w700,
                    color: AppTheme.ink,
                  ),
                ),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Container(
                      width: 46,
                      height: 46,
                      decoration: BoxDecoration(
                        color: amberSoft,
                        borderRadius: BorderRadius.circular(14),
                      ),
                      child: const Icon(
                        Icons.calendar_month_rounded,
                        color: amber,
                      ),
                    ),
                    const SizedBox(width: 14),
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
                            interview.job,
                            style: const TextStyle(
                              fontSize: 13,
                              color: AppTheme.muted,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                Text(
                  DateFormat('EEE, d MMM yyyy • h:mm a')
                      .format(interview.scheduledAt.toLocal()),
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w600,
                    color: amber,
                  ),
                ),
                if (interview.locationOrLink != null &&
                    interview.locationOrLink!.trim().isNotEmpty) ...[
                  const SizedBox(height: 6),
                  Text(
                    interview.locationOrLink!,
                    style: const TextStyle(
                      fontSize: 13,
                      color: AppTheme.muted,
                    ),
                  ),
                ],
              ],
            ),
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.icon,
    required this.label,
    required this.value,
  });

  static const Color amber = Color(0xFFD97706);
  static const Color amberSoft = Color(0xFFFFFBEB);

  final IconData icon;
  final String label;
  final int value;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: AppTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: amberSoft,
              borderRadius: BorderRadius.circular(14),
            ),
            child: Icon(icon, color: amber),
          ),
          const SizedBox(height: 16),
          Text(
            '$value',
            style: const TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.w800,
              color: AppTheme.ink,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: const TextStyle(
              fontSize: 12,
              color: AppTheme.muted,
            ),
          ),
        ],
      ),
    );
  }
}