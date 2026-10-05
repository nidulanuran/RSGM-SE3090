import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/recruiter/recruiter_job_post.dart';
import '../../services/api_client.dart';
import '../../services/recruiter/recruiter_job_post_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';
import 'recruiter_job_post_detail_screen.dart';

/// Screen for displaying recruiter job postings with local search and filtering.
class RecruiterJobPostsScreen extends StatefulWidget {
  const RecruiterJobPostsScreen({super.key, this.service});

  final RecruiterJobPostService? service;

  @override
  State<RecruiterJobPostsScreen> createState() =>
      _RecruiterJobPostsScreenState();
}

class _RecruiterJobPostsScreenState extends State<RecruiterJobPostsScreen> {
  late final RecruiterJobPostService _service;

  List<RecruiterJobPost> _allPosts = [];
  bool _isLoading = true;
  String? _error;

  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';
  String _selectedStatus = 'All';

  static const List<String> _statusFilters = [
    'All',
    'Published',
    'Draft',
    'Closed',
  ];

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? RecruiterJobPostService();
    _loadJobPosts();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadJobPosts({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    }

    try {
      final posts = await _service.getMyJobPostings();
      if (!mounted) return;
      // Sort most recent first
      posts.sort((a, b) => b.createdAt.compareTo(a.createdAt));
      setState(() {
        _allPosts = posts;
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
            'Unable to load job posts. Please check your connection and try again.';
        _isLoading = false;
      });
    }
  }

  List<RecruiterJobPost> get _filteredPosts {
    final query = _searchQuery.trim().toLowerCase();

    return _allPosts.where((post) {
      // 1. Status Filter
      if (_selectedStatus != 'All') {
        if (_selectedStatus == 'Published' && !post.isPublished) return false;
        if (_selectedStatus == 'Draft' && !post.isDraft) return false;
        if (_selectedStatus == 'Closed' && !post.isClosed) return false;
        if (_selectedStatus != 'Published' &&
            _selectedStatus != 'Draft' &&
            _selectedStatus != 'Closed') {
          if (post.status.toLowerCase() != _selectedStatus.toLowerCase()) {
            return false;
          }
        }
      }

      // 2. Search Query Filter
      if (query.isNotEmpty) {
        final titleMatch = post.title.toLowerCase().contains(query);
        final companyMatch = post.company.toLowerCase().contains(query);
        final locationMatch = post.location.toLowerCase().contains(query);
        final descMatch =
            post.description?.toLowerCase().contains(query) ?? false;
        final empTypeMatch =
            post.employmentTypeLabel.toLowerCase().contains(query);
        final workModeMatch =
            post.workModeLabel.toLowerCase().contains(query);
        final expLevelMatch =
            post.experienceLevelLabel.toLowerCase().contains(query);
        final skillMatch = post.requiredSkills
            .any((s) => s.name.toLowerCase().contains(query));

        if (!titleMatch &&
            !companyMatch &&
            !locationMatch &&
            !descMatch &&
            !empTypeMatch &&
            !workModeMatch &&
            !expLevelMatch &&
            !skillMatch) {
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
      _selectedStatus = 'All';
    });
  }

  @override
  Widget build(BuildContext context) {
    return GradientBackdrop(
      child: SafeArea(
        child: RefreshIndicator(
          color: AppTheme.violet,
          onRefresh: () => _loadJobPosts(silent: true),
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const InfoPill(text: 'JOB POSTS'),
                      const SizedBox(height: 12),
                      Text(
                        'My Job Posts',
                        style: Theme.of(context).textTheme.headlineMedium,
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'View your published and existing job postings.',
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                      const SizedBox(height: 16),

                      // Search input
                      _buildSearchField(),
                      const SizedBox(height: 12),

                      // Status filter chips
                      _buildStatusFilterRow(),
                      const SizedBox(height: 16),
                    ],
                  ),
                ),
              ),

              // Content Area
              _buildContentSliver(),

              // Bottom padding
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
          hintText: 'Search job title, company, location, skills...',
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

  Widget _buildStatusFilterRow() {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: _statusFilters.map((status) {
          final isSelected = _selectedStatus == status;
          final count = _getStatusCount(status);

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

  int _getStatusCount(String status) {
    if (status == 'All') return _allPosts.length;
    if (status == 'Published') {
      return _allPosts.where((p) => p.isPublished).length;
    }
    if (status == 'Draft') {
      return _allPosts.where((p) => p.isDraft).length;
    }
    if (status == 'Closed') {
      return _allPosts.where((p) => p.isClosed).length;
    }
    return _allPosts
        .where((p) => p.status.toLowerCase() == status.toLowerCase())
        .length;
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
                  onPressed: () => _loadJobPosts(),
                ),
              ],
            ),
          ),
        ),
      );
    }

    if (_allPosts.isEmpty) {
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
                    Icons.work_off_outlined,
                    color: AppTheme.violet,
                    size: 28,
                  ),
                ),
                const SizedBox(height: 16),
                const Text(
                  'No job posts found',
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: AppTheme.ink,
                  ),
                ),
                const SizedBox(height: 6),
                const Text(
                  "You don't currently have any job posts to display.",
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

    final filtered = _filteredPosts;

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
                  'No matching job posts',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppTheme.ink,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  _searchQuery.isNotEmpty
                      ? 'No job posts matched "$_searchQuery" with status "$_selectedStatus".'
                      : 'No job posts found with status "$_selectedStatus".',
                  textAlign: TextAlign.center,
                  style: const TextStyle(
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
            final job = filtered[index];
            return Padding(
              padding: const EdgeInsets.only(bottom: 14),
              child: _JobPostCard(
                job: job,
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => RecruiterJobPostDetailScreen(
                        jobId: job.id,
                        initialJob: job,
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

class _JobPostCard extends StatelessWidget {
  const _JobPostCard({
    required this.job,
    required this.onTap,
  });

  final RecruiterJobPost job;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    Color statusBg;
    Color statusColor;
    if (job.isPublished) {
      statusBg = const Color(0xFFECFDF5);
      statusColor = const Color(0xFF059669);
    } else if (job.isDraft) {
      statusBg = const Color(0xFFFFFBEB);
      statusColor = const Color(0xFFD97706);
    } else {
      statusBg = const Color(0xFFF3F4F6);
      statusColor = const Color(0xFF4B5563);
    }

    final deadlineStr = job.applicationDeadline != null
        ? DateFormat('d MMMM yyyy').format(job.applicationDeadline!)
        : null;

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
                // Top row: Title & Status
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            job.title,
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                              color: AppTheme.ink,
                              letterSpacing: -0.2,
                            ),
                          ),
                          const SizedBox(height: 3),
                          Text(
                            job.company,
                            style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: AppTheme.muted,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
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
                            job.status,
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

                // Location • Work Mode
                Row(
                  children: [
                    const Icon(
                      Icons.location_on_outlined,
                      size: 14,
                      color: AppTheme.muted,
                    ),
                    const SizedBox(width: 4),
                    Text(
                      '${job.location} • ${job.workModeLabel}',
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppTheme.muted,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 6),

                // Employment Type • Experience Level
                Row(
                  children: [
                    const Icon(
                      Icons.work_history_outlined,
                      size: 14,
                      color: AppTheme.muted,
                    ),
                    const SizedBox(width: 4),
                    Text(
                      '${job.employmentTypeLabel} • ${job.experienceLevelLabel}',
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppTheme.muted,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                const Divider(height: 1, color: Color(0xFFF3F4F6)),
                const SizedBox(height: 10),

                // Footer Row: Applicants, Deadline & "View Details →"
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              const Icon(
                                Icons.people_outline_rounded,
                                size: 14,
                                color: AppTheme.violet,
                              ),
                              const SizedBox(width: 5),
                              Text(
                                '${job.applicantCount} Applicants',
                                style: const TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w600,
                                  color: AppTheme.ink,
                                ),
                              ),
                            ],
                          ),
                          if (deadlineStr != null) ...[
                            const SizedBox(height: 3),
                            Row(
                              children: [
                                const Icon(
                                  Icons.event_outlined,
                                  size: 14,
                                  color: AppTheme.muted,
                                ),
                                const SizedBox(width: 5),
                                Text(
                                  'Deadline: $deadlineStr',
                                  style: const TextStyle(
                                    fontSize: 11,
                                    color: AppTheme.muted,
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ],
                      ),
                    ),
                    const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          'View Details',
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
