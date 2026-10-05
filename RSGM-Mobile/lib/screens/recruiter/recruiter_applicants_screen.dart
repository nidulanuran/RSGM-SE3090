import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/recruiter/recruiter_applicant.dart';
import '../../services/api_client.dart';
import '../../services/recruiter/recruiter_application_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';
import 'recruiter_applicant_detail_screen.dart';

/// Screen for viewing recruiter applicants with local search, job filtering, and status filtering.
class RecruiterApplicantsScreen extends StatefulWidget {
  const RecruiterApplicantsScreen({super.key, this.service});

  final RecruiterApplicationService? service;

  @override
  State<RecruiterApplicantsScreen> createState() =>
      _RecruiterApplicantsScreenState();
}

class _RecruiterApplicantsScreenState extends State<RecruiterApplicantsScreen> {
  late final RecruiterApplicationService _service;

  List<RecruiterApplicant> _allApplicants = [];
  bool _isLoading = true;
  String? _error;

  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';
  String _selectedJob = 'All Jobs';
  String _selectedStatus = 'All';

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? RecruiterApplicationService();
    _loadApplicants();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadApplicants({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    }

    try {
      final items = await _service.getApplications();
      if (!mounted) return;
      setState(() {
        _allApplicants = items;
        _isLoading = false;
        _error = null;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _isLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error =
            'Unable to load applicants. Please check your connection and try again.';
        _isLoading = false;
      });
    }
  }

  List<String> get _availableJobs {
    final jobs = <String>{'All Jobs'};
    for (final a in _allApplicants) {
      if (a.jobTitle.trim().isNotEmpty) {
        jobs.add(a.jobTitle.trim());
      }
    }
    return jobs.toList();
  }

  List<String> get _availableStatuses {
    final statuses = <String>{'All'};
    for (final a in _allApplicants) {
      if (a.statusLabel.trim().isNotEmpty) {
        statuses.add(a.statusLabel.trim());
      }
    }
    return statuses.toList();
  }

  List<RecruiterApplicant> get _filteredApplicants {
    final query = _searchQuery.trim().toLowerCase();

    return _allApplicants.where((applicant) {
      // 1. Job Filter
      if (_selectedJob != 'All Jobs' &&
          applicant.jobTitle.trim().toLowerCase() !=
              _selectedJob.trim().toLowerCase()) {
        return false;
      }

      // 2. Status Filter
      if (_selectedStatus != 'All') {
        final matchesStatusLabel = applicant.statusLabel
                .toLowerCase() ==
            _selectedStatus.toLowerCase();
        final matchesRawStatus =
            applicant.status.toLowerCase() == _selectedStatus.toLowerCase();
        if (!matchesStatusLabel && !matchesRawStatus) {
          return false;
        }
      }

      // 3. Search Query Filter
      if (query.isNotEmpty) {
        final nameMatch = applicant.fullName.toLowerCase().contains(query);
        final emailMatch = applicant.email.toLowerCase().contains(query);
        final headlineMatch =
            applicant.headline?.toLowerCase().contains(query) ?? false;
        final locationMatch =
            applicant.location?.toLowerCase().contains(query) ?? false;
        final jobMatch = applicant.jobTitle.toLowerCase().contains(query);
        final skillsMatch =
            applicant.skills.any((s) => s.toLowerCase().contains(query));
        final matchedSkillsMatch = applicant.matchedSkills
            .any((s) => s.toLowerCase().contains(query));
        final missingSkillsMatch = applicant.missingSkills
            .any((s) => s.toLowerCase().contains(query));

        if (!nameMatch &&
            !emailMatch &&
            !headlineMatch &&
            !locationMatch &&
            !jobMatch &&
            !skillsMatch &&
            !matchedSkillsMatch &&
            !missingSkillsMatch) {
          return false;
        }
      }

      return true;
    }).toList();
  }

  void _clearFilters() {
    _searchController.clear();
    setState(() {
      _searchQuery = '';
      _selectedJob = 'All Jobs';
      _selectedStatus = 'All';
    });
  }

  @override
  Widget build(BuildContext context) {
    return GradientBackdrop(
      child: SafeArea(
        child: RefreshIndicator(
          color: AppTheme.violet,
          onRefresh: () => _loadApplicants(silent: true),
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const InfoPill(text: 'APPLICANTS'),
                      const SizedBox(height: 12),
                      Text(
                        'Applicants',
                        style: Theme.of(context).textTheme.headlineMedium,
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'View candidates who have applied to your job posts.',
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                      const SizedBox(height: 16),

                      // Search input
                      _buildSearchField(),
                      const SizedBox(height: 12),

                      // Job dropdown filter (if applicants exist)
                      if (_allApplicants.isNotEmpty) ...[
                        _buildJobFilterDropdown(),
                        const SizedBox(height: 12),
                        _buildStatusFilterRow(),
                        const SizedBox(height: 16),
                      ],
                    ],
                  ),
                ),
              ),

              // Content Sliver
              _buildContentSliver(),

              // Bottom Padding
              const SliverToBoxAdapter(
                child: SizedBox(height: 32),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSearchField() {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppTheme.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x06000000),
            blurRadius: 10,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: TextField(
        controller: _searchController,
        onChanged: (val) => setState(() => _searchQuery = val),
        decoration: InputDecoration(
          hintText: 'Search candidate, job, skills, location...',
          hintStyle: const TextStyle(
            fontSize: 13,
            color: AppTheme.muted,
          ),
          prefixIcon: const Icon(
            Icons.search_rounded,
            color: AppTheme.muted,
            size: 20,
          ),
          suffixIcon: _searchController.text.isNotEmpty
              ? IconButton(
                  icon: const Icon(
                    Icons.clear_rounded,
                    color: AppTheme.muted,
                    size: 18,
                  ),
                  onPressed: () {
                    _searchController.clear();
                    setState(() => _searchQuery = '');
                  },
                )
              : null,
          border: InputBorder.none,
          contentPadding:
              const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        ),
      ),
    );
  }

  Widget _buildJobFilterDropdown() {
    final jobs = _availableJobs;
    final currentSelection =
        jobs.contains(_selectedJob) ? _selectedJob : 'All Jobs';

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 2),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppTheme.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x04000000),
            blurRadius: 8,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<String>(
          isExpanded: true,
          value: currentSelection,
          icon: const Icon(Icons.keyboard_arrow_down_rounded,
              color: AppTheme.muted),
          style: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            color: AppTheme.ink,
          ),
          items: jobs.map((job) {
            final count = job == 'All Jobs'
                ? _allApplicants.length
                : _allApplicants
                    .where((a) =>
                        a.jobTitle.trim().toLowerCase() ==
                        job.trim().toLowerCase())
                    .length;
            return DropdownMenuItem<String>(
              value: job,
              child: Row(
                children: [
                  const Icon(Icons.work_outline_rounded,
                      size: 16, color: AppTheme.muted),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      job,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Text(
                    '($count)',
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppTheme.muted,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ),
            );
          }).toList(),
          onChanged: (newJob) {
            if (newJob != null) {
              setState(() => _selectedJob = newJob);
            }
          },
        ),
      ),
    );
  }

  Widget _buildStatusFilterRow() {
    final statuses = _availableStatuses;

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: statuses.map((status) {
          final isSelected = _selectedStatus == status;
          final count = status == 'All'
              ? _allApplicants.length
              : _allApplicants
                  .where((a) =>
                      a.statusLabel.toLowerCase() == status.toLowerCase() ||
                      a.status.toLowerCase() == status.toLowerCase())
                  .length;

          return Padding(
            padding: const EdgeInsets.only(right: 8),
            child: FilterChip(
              label: Text(
                count > 0 ? '$status ($count)' : status,
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                  color: isSelected ? Colors.white : AppTheme.ink,
                ),
              ),
              selected: isSelected,
              selectedColor: AppTheme.violet,
              backgroundColor: Colors.white,
              checkmarkColor: Colors.white,
              showCheckmark: false,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(20),
                side: BorderSide(
                  color: isSelected ? AppTheme.violet : AppTheme.border,
                ),
              ),
              onSelected: (_) {
                setState(() => _selectedStatus = status);
              },
            ),
          );
        }).toList(),
      ),
    );
  }

  Widget _buildContentSliver() {
    if (_isLoading) {
      return const SliverFillRemaining(
        hasScrollBody: false,
        child: Center(
          child: CircularProgressIndicator(color: AppTheme.violet),
        ),
      );
    }

    if (_error != null) {
      return SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
          child: Container(
            width: double.infinity,
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: const Color(0xFFFEF2F2),
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: const Color(0xFFFECACA)),
            ),
            child: Column(
              children: [
                const Icon(
                  Icons.error_outline_rounded,
                  size: 36,
                  color: Color(0xFFDC2626),
                ),
                const SizedBox(height: 10),
                Text(
                  _error!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 13,
                    color: Color(0xFF991B1B),
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 14),
                OutlinedButton.icon(
                  style: OutlinedButton.styleFrom(
                    foregroundColor: const Color(0xFFDC2626),
                    side: const BorderSide(color: Color(0xFFDC2626)),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(18),
                    ),
                  ),
                  icon: const Icon(Icons.refresh_rounded, size: 16),
                  label: const Text('Try again'),
                  onPressed: () => _loadApplicants(),
                ),
              ],
            ),
          ),
        ),
      );
    }

    if (_allApplicants.isEmpty) {
      return SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 20),
          child: Container(
            width: double.infinity,
            padding: const EdgeInsets.all(32),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: .92),
              borderRadius: BorderRadius.circular(24),
              border: Border.all(color: AppTheme.border),
              boxShadow: const [
                BoxShadow(
                  color: Color(0x06000000),
                  blurRadius: 16,
                  offset: Offset(0, 6),
                ),
              ],
            ),
            child: Column(
              children: [
                Container(
                  width: 56,
                  height: 56,
                  decoration: BoxDecoration(
                    color: const Color(0xFFF5F3FF),
                    borderRadius: BorderRadius.circular(18),
                    border: Border.all(color: const Color(0xFFDDD6FE)),
                  ),
                  child: const Icon(
                    Icons.people_outline_rounded,
                    color: AppTheme.violet,
                    size: 28,
                  ),
                ),
                const SizedBox(height: 16),
                const Text(
                  'No applicants yet',
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: AppTheme.ink,
                  ),
                ),
                const SizedBox(height: 6),
                const Text(
                  'Applicants will appear here when candidates apply to your job posts.',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 13,
                    color: AppTheme.muted,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
        ),
      );
    }

    final filtered = _filteredApplicants;

    if (filtered.isEmpty) {
      return SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 20),
          child: Container(
            width: double.infinity,
            padding: const EdgeInsets.all(28),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: .92),
              borderRadius: BorderRadius.circular(24),
              border: Border.all(color: AppTheme.border),
            ),
            child: Column(
              children: [
                Container(
                  width: 50,
                  height: 50,
                  decoration: BoxDecoration(
                    color: const Color(0xFFF3F4F6),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: const Icon(
                    Icons.search_off_rounded,
                    color: AppTheme.muted,
                    size: 26,
                  ),
                ),
                const SizedBox(height: 14),
                const Text(
                  'No matching applicants',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppTheme.ink,
                  ),
                ),
                const SizedBox(height: 6),
                const Text(
                  'Try changing your search or filters.',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 13,
                    color: AppTheme.muted,
                  ),
                ),
                const SizedBox(height: 16),
                TextButton.icon(
                  icon: const Icon(Icons.filter_alt_off_rounded, size: 16),
                  label: const Text('Clear filters'),
                  style: TextButton.styleFrom(
                    foregroundColor: AppTheme.violet,
                    textStyle: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                  onPressed: _clearFilters,
                ),
              ],
            ),
          ),
        ),
      );
    }

    return SliverPadding(
      padding: const EdgeInsets.symmetric(horizontal: 20),
      sliver: SliverList(
        delegate: SliverChildBuilderDelegate(
          (context, index) {
            final applicant = filtered[index];
            return Padding(
              padding: const EdgeInsets.only(bottom: 14),
              child: _ApplicantCard(
                applicant: applicant,
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => RecruiterApplicantDetailScreen(
                        applicantId: applicant.id,
                        initialApplicant: applicant,
                        service: _service,
                      ),
                    ),
                  );
                },
              ),
            );
          },
          childCount: filtered.length,
        ),
      ),
    );
  }
}

class _ApplicantCard extends StatelessWidget {
  const _ApplicantCard({
    required this.applicant,
    required this.onTap,
  });

  final RecruiterApplicant applicant;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    Color statusBg;
    Color statusColor;
    if (applicant.isShortlisted) {
      statusBg = const Color(0xFFECFDF5);
      statusColor = const Color(0xFF059669);
    } else if (applicant.isInterview) {
      statusBg = const Color(0xFFEEF2FF);
      statusColor = const Color(0xFF4F46E5);
    } else if (applicant.isUnderReview) {
      statusBg = const Color(0xFFFFFBEB);
      statusColor = const Color(0xFFD97706);
    } else if (applicant.isRejected) {
      statusBg = const Color(0xFFFEF2F2);
      statusColor = const Color(0xFFDC2626);
    } else {
      statusBg = const Color(0xFFF3F4F6);
      statusColor = const Color(0xFF4B5563);
    }

    final appliedDateStr =
        DateFormat('d MMM yyyy').format(applicant.appliedAt.toLocal());

    return Container(
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .96),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppTheme.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x06000000),
            blurRadius: 16,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        borderRadius: BorderRadius.circular(20),
        child: InkWell(
          borderRadius: BorderRadius.circular(20),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Top row: Initials Avatar + Full name & Headline + Status
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: BoxDecoration(
                        gradient: const LinearGradient(
                          colors: [Color(0xFF7C3AED), Color(0xFF4F46E5)],
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                        ),
                        borderRadius: BorderRadius.circular(14),
                      ),
                      alignment: Alignment.center,
                      child: Text(
                        applicant.initials,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            applicant.fullName,
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                              color: AppTheme.ink,
                              letterSpacing: -0.2,
                            ),
                          ),
                          if (applicant.headline != null &&
                              applicant.headline!.trim().isNotEmpty) ...[
                            const SizedBox(height: 2),
                            Text(
                              applicant.headline!.trim(),
                              style: const TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w500,
                                color: AppTheme.violet,
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ],
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    // Status Badge
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: statusBg,
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(
                            color: statusColor.withValues(alpha: 0.3)),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Container(
                            width: 6,
                            height: 6,
                            decoration: BoxDecoration(
                              color: statusColor,
                              shape: BoxShape.circle,
                            ),
                          ),
                          const SizedBox(width: 5),
                          Text(
                            applicant.statusLabel,
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                              color: statusColor,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                // Applied for position
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Applied for: ',
                      style: TextStyle(fontSize: 12, color: AppTheme.muted),
                    ),
                    Expanded(
                      child: Text(
                        applicant.jobTitle,
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppTheme.ink,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 6),

                // Match Score & Location
                Row(
                  children: [
                    if (applicant.matchScore > 0) ...[
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 7, vertical: 3),
                        decoration: BoxDecoration(
                          color: const Color(0xFFEFF6FF),
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: const Color(0xFFBFDBFE)),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(Icons.auto_graph_rounded,
                                size: 12, color: Color(0xFF2563EB)),
                            const SizedBox(width: 4),
                            Text(
                              'Match: ${applicant.matchScore}%',
                              style: const TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w700,
                                color: Color(0xFF1D4ED8),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 8),
                    ],
                    if (applicant.location != null &&
                        applicant.location!.trim().isNotEmpty) ...[
                      const Icon(Icons.location_on_outlined,
                          size: 13, color: AppTheme.muted),
                      const SizedBox(width: 3),
                      Expanded(
                        child: Text(
                          applicant.location!.trim(),
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppTheme.muted,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ],
                ),

                // Skills match indicators
                if (applicant.matchedSkills.isNotEmpty ||
                    applicant.missingSkills.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 6,
                    runSpacing: 4,
                    children: [
                      if (applicant.matchedSkills.isNotEmpty)
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 7, vertical: 2),
                          decoration: BoxDecoration(
                            color: const Color(0xFFECFDF5),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            '${applicant.matchedSkills.length} Matched',
                            style: const TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              color: Color(0xFF065F46),
                            ),
                          ),
                        ),
                      if (applicant.missingSkills.isNotEmpty)
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 7, vertical: 2),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFEF2F2),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            '${applicant.missingSkills.length} Missing',
                            style: const TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              color: Color(0xFF991B1B),
                            ),
                          ),
                        ),
                    ],
                  ),
                ],

                const SizedBox(height: 12),
                const Divider(height: 1, color: Color(0xFFF3F4F6)),
                const SizedBox(height: 10),

                // Footer Row: Applied Date & "View Profile →"
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        const Icon(
                          Icons.event_outlined,
                          size: 14,
                          color: AppTheme.muted,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          'Applied: $appliedDateStr',
                          style: const TextStyle(
                            fontSize: 11,
                            color: AppTheme.muted,
                          ),
                        ),
                      ],
                    ),
                    const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          'View Profile',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: AppTheme.violet,
                          ),
                        ),
                        SizedBox(width: 4),
                        Icon(
                          Icons.arrow_forward_rounded,
                          size: 14,
                          color: AppTheme.violet,
                        ),
                      ],
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
