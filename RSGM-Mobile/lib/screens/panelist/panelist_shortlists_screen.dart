import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models/panelist/panelist_shortlist.dart';
import '../../services/api_client.dart';
import '../../services/panelist/panelist_shortlist_service.dart';
import '../../theme/app_theme.dart';
import 'panelist_candidate_detail_screen.dart';

class PanelistShortlistsScreen extends StatefulWidget {
  const PanelistShortlistsScreen({
    super.key,
    this.service,
  });

  final PanelistShortlistService? service;

  @override
  State<PanelistShortlistsScreen> createState() =>
      _PanelistShortlistsScreenState();
}

class _PanelistShortlistsScreenState
    extends State<PanelistShortlistsScreen> {
  static const Color amber = Color(0xFFD97706);
  static const Color amberSoft = Color(0xFFFFFBEB);
  static const Color amberBorder = Color(0xFFFDE68A);

  late final PanelistShortlistService _service;

  List<PanelistShortlist> _shortlists = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? PanelistShortlistService();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final items = await _service.getShortlists();

      if (!mounted) return;

      setState(() {
        _shortlists = items;
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
        _error = 'Unable to load assigned shortlists.';
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
                  'Ranked shortlists',
                  style: Theme.of(context).textTheme.headlineMedium,
                ),
                const SizedBox(height: 8),
                Text(
                  'View candidates assigned to you for interview review.',
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
            Icons.people_alt_outlined,
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

    if (_shortlists.isEmpty) {
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
              Icons.people_outline_rounded,
              size: 42,
              color: AppTheme.muted,
            ),
            SizedBox(height: 12),
            Text(
              'No shortlists assigned yet.',
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
      children: _shortlists
          .map(
            (shortlist) => Padding(
              padding: const EdgeInsets.only(bottom: 18),
              child: _ShortlistCard(shortlist: shortlist),
            ),
          )
          .toList(),
    );
  }
}

class _ShortlistCard extends StatelessWidget {
  const _ShortlistCard({
    required this.shortlist,
  });

  static const Color amber = Color(0xFFD97706);
  static const Color amberSoft = Color(0xFFFFFBEB);

  final PanelistShortlist shortlist;

  @override
  Widget build(BuildContext context) {
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
          Text(
            shortlist.jobTitle,
            style: const TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.w700,
              color: AppTheme.ink,
            ),
          ),
          const SizedBox(height: 5),
          Text(
            'Sent by ${shortlist.recruiter} • '
            '${DateFormat('d MMM yyyy, h:mm a').format(shortlist.submittedAt.toLocal())}',
            style: const TextStyle(
              fontSize: 12,
              color: AppTheme.muted,
            ),
          ),
          const SizedBox(height: 16),

          ...shortlist.candidates.map(
            (candidate) => InkWell(
              borderRadius: BorderRadius.circular(16),
              onTap: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => PanelistCandidateDetailScreen(
                      applicationId: candidate.id,
                    ),
                  ),
                );
              },
              child: Container(
                margin: const EdgeInsets.only(bottom: 10),
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: const Color(0xFFFAFAFA),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: AppTheme.border),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 8,
                      ),
                      decoration: BoxDecoration(
                        color: amberSoft,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        '#${candidate.shortlistRank}',
                        style: const TextStyle(
                          fontWeight: FontWeight.w700,
                          color: amber,
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            candidate.candidate,
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w700,
                              color: AppTheme.ink,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            candidate.email,
                            style: const TextStyle(
                              fontSize: 12,
                              color: AppTheme.muted,
                            ),
                          ),
                          const SizedBox(height: 8),
                          Row(
                            children: [
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 9,
                                  vertical: 4,
                                ),
                                decoration: BoxDecoration(
                                  color: amberSoft,
                                  borderRadius: BorderRadius.circular(20),
                                ),
                                child: Text(
                                  candidate.status,
                                  style: const TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.w600,
                                    color: amber,
                                  ),
                                ),
                              ),
                              const Spacer(),
                              const Icon(
                                Icons.chevron_right_rounded,
                                color: amber,
                                size: 22,
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}