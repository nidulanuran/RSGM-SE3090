import 'package:flutter/material.dart';

import '../../models/hr/hr_models.dart';
import '../../services/api_client.dart';
import '../../services/hr/hr_service.dart';
import '../../theme/app_theme.dart';

class HrAnalyticsScreen extends StatefulWidget {
  const HrAnalyticsScreen({super.key, this.service});

  final HrService? service;

  @override
  State<HrAnalyticsScreen> createState() => _HrAnalyticsScreenState();
}

class _HrAnalyticsScreenState extends State<HrAnalyticsScreen> {
  late final HrService _service = widget.service ?? HrService();
  HrAnalytics? _data;
  bool _loading = true;
  String? _error;

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final data = await _service.getAnalytics();
      if (!mounted) return;
      setState(() {
        _data = data;
        _loading = false;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  @override
  void initState() {
    super.initState();
    _load();
  }

  String _number(double value) =>
      value % 1 == 0 ? value.toStringAsFixed(0) : value.toStringAsFixed(1);

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: _load,
      color: const Color(0xFF059669),
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
        children: [
          Text(
            'Recruitment analytics',
            style: Theme.of(context).textTheme.headlineMedium,
          ),
          const SizedBox(height: 8),
          Text(
            'Live hiring performance and funnel metrics for your company.',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
          const SizedBox(height: 22),
          if (_loading)
            const Center(
              child: Padding(
                padding: EdgeInsets.all(60),
                child: CircularProgressIndicator(),
              ),
            )
          else if (_error != null)
            Text(_error!, style: const TextStyle(color: Colors.red))
          else
            _buildContent(_data!),
        ],
      ),
    );
  }

  Widget _buildContent(HrAnalytics analytics) {
    final stats = analytics.stats;
    final cards = <({String label, String value, IconData icon})>[
      (
        label: 'Avg. time to hire',
        value: '${_number(stats.averageTimeToHireDays)} days',
        icon: Icons.schedule_rounded,
      ),
      (
        label: 'Offer acceptance',
        value: '${_number(stats.offerAcceptanceRate)}%',
        icon: Icons.percent_rounded,
      ),
      (
        label: 'Candidates in pipeline',
        value: '${stats.candidatesInPipeline}',
        icon: Icons.groups_rounded,
      ),
      (
        label: 'Req. approval rate',
        value: '${_number(stats.requisitionApprovalRate)}%',
        icon: Icons.trending_up_rounded,
      ),
    ];

    final maxCount = analytics.funnel.fold<int>(
      1,
      (current, item) => item.count > current ? item.count : current,
    );

    return Column(
      children: [
        GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: cards.length,
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: 2,
            crossAxisSpacing: 12,
            mainAxisSpacing: 12,
            childAspectRatio: 1.18,
          ),
          itemBuilder: (_, index) {
            final card = cards[index];
            return Container(
              padding: const EdgeInsets.all(15),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: AppTheme.border),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(card.icon, color: const Color(0xFF059669)),
                  const Spacer(),
                  Text(
                    card.value,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    card.label,
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppTheme.muted,
                    ),
                  ),
                ],
              ),
            );
          },
        ),
        const SizedBox(height: 22),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(17),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: AppTheme.border),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Hiring funnel',
                style: TextStyle(fontSize: 17, fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 16),
              ...analytics.funnel.map(
                (item) => Padding(
                  padding: const EdgeInsets.only(bottom: 14),
                  child: Column(
                    children: [
                      Row(
                        children: [
                          Expanded(child: Text(item.stage)),
                          Text(
                            '${item.count}',
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                        ],
                      ),
                      const SizedBox(height: 7),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: LinearProgressIndicator(
                          value: item.count / maxCount,
                          minHeight: 8,
                          backgroundColor: const Color(0xFFF3F4F6),
                          color: const Color(0xFF059669),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
