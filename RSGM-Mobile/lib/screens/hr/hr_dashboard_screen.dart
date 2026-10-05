import 'package:flutter/material.dart';

import '../../models/hr/hr_models.dart';
import '../../services/api_client.dart';
import '../../services/hr/hr_service.dart';
import '../../theme/app_theme.dart';

class HrDashboardScreen extends StatefulWidget {
  const HrDashboardScreen({
    super.key,
    this.service,
  });

  final HrService? service;

  @override
  State<HrDashboardScreen> createState() => _HrDashboardScreenState();
}

class _HrDashboardScreenState extends State<HrDashboardScreen> {
  late final HrService _service = widget.service ?? HrService();

  HrDashboardStats? _stats;
  bool _loading = true;
  String? _error;

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final data = await _service.getDashboardStats();

      if (!mounted) return;

      setState(() {
        _stats = data;
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
        _error = 'Unable to load HR dashboard.';
        _loading = false;
      });
    }
  }

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: _load,
      color: const Color(0xFF059669),
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
        children: [
          const _Pill(),

          const SizedBox(height: 14),

          Text(
            'HR Manager dashboard',
            style: Theme.of(context).textTheme.headlineMedium,
          ),

          const SizedBox(height: 8),

          Text(
            'Monitor the live recruitment pipeline and focus on items needing HR attention.',
            style: Theme.of(context).textTheme.bodyMedium,
          ),

          const SizedBox(height: 24),

          if (_loading)
            const Center(
              child: Padding(
                padding: EdgeInsets.all(60),
                child: CircularProgressIndicator(),
              ),
            )
          else if (_error != null)
            _ErrorCard(
              message: _error!,
              retry: _load,
            )
          else
            _StatsGrid(stats: _stats!),
        ],
      ),
    );
  }
}

class _Pill extends StatelessWidget {
  const _Pill();

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Container(
        padding: const EdgeInsets.symmetric(
          horizontal: 12,
          vertical: 7,
        ),
        decoration: BoxDecoration(
          color: const Color(0xFFECFDF5),
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: const Color(0xFFA7F3D0),
          ),
        ),
        child: const Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.auto_awesome_rounded,
              size: 15,
              color: Color(0xFF059669),
            ),
            SizedBox(width: 7),
            Text(
              'HR MANAGER',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: Color(0xFF047857),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatsGrid extends StatelessWidget {
  const _StatsGrid({
    required this.stats,
  });

  final HrDashboardStats stats;

  @override
  Widget build(BuildContext context) {
    final items = [
      (
        'Pending requisitions',
        stats.pendingRequisitions,
        Icons.fact_check_outlined,
      ),
      (
        'Active job postings',
        stats.activeJobPostings,
        Icons.work_outline_rounded,
      ),
      (
        'Candidates in pipeline',
        stats.candidatesInPipeline,
        Icons.groups_outlined,
      ),
      (
        'Upcoming interviews',
        stats.upcomingInterviews,
        Icons.event_available_outlined,
      ),
      (
        'Offers awaiting approval',
        stats.offersAwaitingApproval,
        Icons.description_outlined,
      ),
      (
        'Hires this month',
        stats.hiresThisMonth,
        Icons.person_add_alt_1_rounded,
      ),
    ];

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: items.length,
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        crossAxisSpacing: 12,
        mainAxisSpacing: 12,

        // Important fix:
        // Gives every card enough height for labels that wrap to 2 lines.
        mainAxisExtent: 148,
      ),
      itemBuilder: (_, i) {
        final item = items[i];

        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(20),
            border: Border.all(
              color: AppTheme.border,
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                item.$3,
                color: const Color(0xFF059669),
                size: 25,
              ),

              const SizedBox(height: 10),

              Text(
                '${item.$2}',
                style: const TextStyle(
                  fontSize: 26,
                  fontWeight: FontWeight.w800,
                  color: AppTheme.ink,
                  height: 1,
                ),
              ),

              const SizedBox(height: 10),

              Expanded(
                child: Align(
                  alignment: Alignment.topLeft,
                  child: Text(
                    item.$1,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 12,
                      height: 1.3,
                      color: AppTheme.muted,
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _ErrorCard extends StatelessWidget {
  const _ErrorCard({
    required this.message,
    required this.retry,
  });

  final String message;
  final VoidCallback retry;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: const Color(0xFFFEF2F2),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        children: [
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Color(0xFF991B1B),
            ),
          ),

          const SizedBox(height: 12),

          OutlinedButton.icon(
            onPressed: retry,
            icon: const Icon(Icons.refresh_rounded),
            label: const Text('Try again'),
          ),
        ],
      ),
    );
  }
}