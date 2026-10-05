import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/recruiter/recruiter_requisition.dart';
import '../../services/api_client.dart';
import '../../services/recruiter/recruiter_requisition_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';
import 'recruiter_requisition_detail_screen.dart';

/// Screen for viewing recruiter job requisitions with local search and status filtering.
class RecruiterRequisitionsScreen extends StatefulWidget {
  const RecruiterRequisitionsScreen({super.key, this.service});

  final RecruiterRequisitionService? service;

  @override
  State<RecruiterRequisitionsScreen> createState() =>
      _RecruiterRequisitionsScreenState();
}

class _RecruiterRequisitionsScreenState
    extends State<RecruiterRequisitionsScreen> {
  late final RecruiterRequisitionService _service;

  List<RecruiterRequisition> _allRequisitions = [];
  bool _isLoading = true;
  String? _error;

  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';
  String _selectedStatus = 'All';

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? RecruiterRequisitionService();
    _loadRequisitions();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadRequisitions({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    }

    try {
      final items = await _service.getMyRequisitions();
      if (!mounted) return;
      // Sort most recent first
      items.sort((a, b) => b.createdAt.compareTo(a.createdAt));
      setState(() {
        _allRequisitions = items;
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
            'Unable to load requisitions. Please check your connection and try again.';
        _isLoading = false;
      });
    }
  }

  List<String> get _availableStatuses {
    final statuses = <String>{'All'};
    for (final r in _allRequisitions) {
      statuses.add(r.status.label);
    }
    return statuses.toList();
  }

  List<RecruiterRequisition> get _filteredRequisitions {
    final query = _searchQuery.trim().toLowerCase();

    return _allRequisitions.where((req) {
      // 1. Status Filter
      if (_selectedStatus != 'All' &&
          req.status.label.toLowerCase() != _selectedStatus.toLowerCase()) {
        return false;
      }

      // 2. Search Query Filter
      if (query.isNotEmpty) {
        final titleMatch = req.positionTitle.toLowerCase().contains(query);
        final deptMatch = req.department.toLowerCase().contains(query);
        final companyMatch = req.companyName.toLowerCase().contains(query);
        final locationMatch = req.location.toLowerCase().contains(query);
        final empTypeMatch =
            req.employmentType.label.toLowerCase().contains(query);
        final workModeMatch =
            req.workMode.label.toLowerCase().contains(query);
        final expLevelMatch =
            req.experienceLevel.label.toLowerCase().contains(query);
        final descMatch =
            req.description?.toLowerCase().contains(query) ?? false;
        final respMatch =
            req.responsibilities?.toLowerCase().contains(query) ?? false;
        final reqMatch =
            req.requirements?.toLowerCase().contains(query) ?? false;
        final justMatch =
            req.justification?.toLowerCase().contains(query) ?? false;
        final recruiterMatch =
            req.recruiterName.toLowerCase().contains(query);
        final feedbackMatch =
            req.hrFeedback?.toLowerCase().contains(query) ?? false;

        if (!titleMatch &&
            !deptMatch &&
            !companyMatch &&
            !locationMatch &&
            !empTypeMatch &&
            !workModeMatch &&
            !expLevelMatch &&
            !descMatch &&
            !respMatch &&
            !reqMatch &&
            !justMatch &&
            !recruiterMatch &&
            !feedbackMatch) {
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
          onRefresh: () => _loadRequisitions(silent: true),
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const InfoPill(text: 'REQUISITIONS'),
                      const SizedBox(height: 12),
                      Text(
                        'Requisitions',
                        style: Theme.of(context).textTheme.headlineMedium,
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'View your recruitment requisitions and their approval status.',
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                      const SizedBox(height: 16),

                      // Search Input
                      _buildSearchField(),
                      const SizedBox(height: 12),

                      // Status filter chips (if requisitions exist)
                      if (_allRequisitions.isNotEmpty) ...[
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
          hintText: 'Search position, department, requirements...',
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
    final statuses = _availableStatuses;

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: statuses.map((status) {
          final isSelected = _selectedStatus == status;
          final count = status == 'All'
              ? _allRequisitions.length
              : _allRequisitions
                  .where((r) =>
                      r.status.label.toLowerCase() == status.toLowerCase())
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
                  onPressed: () => _loadRequisitions(),
                ),
              ],
            ),
          ),
        ),
      );
    }

    if (_allRequisitions.isEmpty) {
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
                    Icons.assignment_outlined,
                    color: AppTheme.violet,
                    size: 28,
                  ),
                ),
                const SizedBox(height: 16),
                const Text(
                  'No requisitions yet',
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: AppTheme.ink,
                  ),
                ),
                const SizedBox(height: 6),
                const Text(
                  'Your recruitment requisitions will appear here.',
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

    final filtered = _filteredRequisitions;

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
                  'No matching requisitions',
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
            final req = filtered[index];
            return Padding(
              padding: const EdgeInsets.only(bottom: 14),
              child: _RequisitionCard(
                requisition: req,
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => RecruiterRequisitionDetailScreen(
                        requisitionId: req.id,
                        initialRequisition: req,
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

class _RequisitionCard extends StatelessWidget {
  const _RequisitionCard({
    required this.requisition,
    required this.onTap,
  });

  final RecruiterRequisition requisition;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    Color statusBg;
    Color statusColor;
    switch (requisition.status) {
      case RequisitionStatus.approved:
        statusBg = const Color(0xFFECFDF5);
        statusColor = const Color(0xFF059669);
        break;
      case RequisitionStatus.rejected:
        statusBg = const Color(0xFFFEF2F2);
        statusColor = const Color(0xFFDC2626);
        break;
      case RequisitionStatus.submitted:
        statusBg = const Color(0xFFEFF6FF);
        statusColor = const Color(0xFF2563EB);
        break;
      case RequisitionStatus.draft:
        statusBg = const Color(0xFFFFFBEB);
        statusColor = const Color(0xFFD97706);
        break;
    }

    final dateStr = requisition.submittedAt != null
        ? 'Submitted: ${DateFormat('d MMM yyyy').format(requisition.submittedAt!.toLocal())}'
        : 'Created: ${DateFormat('d MMM yyyy').format(requisition.createdAt.toLocal())}';

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
                // Top row: Position Title & Status Badge
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            requisition.positionTitle,
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                              color: AppTheme.ink,
                              letterSpacing: -0.2,
                            ),
                          ),
                          const SizedBox(height: 3),
                          Text(
                            '${requisition.department} • ${requisition.companyName}',
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
                            requisition.status.label,
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
                      '${requisition.location} • ${requisition.workMode.label}',
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
                      '${requisition.employmentType.label} • ${requisition.experienceLevel.label}',
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppTheme.muted,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),

                // Headcount & Feedback notice row
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 7, vertical: 3),
                      decoration: BoxDecoration(
                        color: const Color(0xFFF1F5F9),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.groups_outlined,
                              size: 13, color: AppTheme.ink),
                          const SizedBox(width: 4),
                          Text(
                            '${requisition.headcount} ${requisition.headcount == 1 ? "Opening" : "Openings"}',
                            style: const TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              color: AppTheme.ink,
                            ),
                          ),
                        ],
                      ),
                    ),
                    if (requisition.hrFeedback != null &&
                        requisition.hrFeedback!.trim().isNotEmpty) ...[
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 7, vertical: 3),
                        decoration: BoxDecoration(
                          color: requisition.isRejected
                              ? const Color(0xFFFEF2F2)
                              : const Color(0xFFF8FAFC),
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(
                            color: requisition.isRejected
                                ? const Color(0xFFFECACA)
                                : const Color(0xFFE2E8F0),
                          ),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(
                              Icons.feedback_outlined,
                              size: 11,
                              color: requisition.isRejected
                                  ? const Color(0xFFDC2626)
                                  : AppTheme.muted,
                            ),
                            const SizedBox(width: 4),
                            Text(
                              'Feedback on file',
                              style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w600,
                                color: requisition.isRejected
                                    ? const Color(0xFFB91C1C)
                                    : AppTheme.muted,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ],
                ),

                const SizedBox(height: 12),
                const Divider(height: 1, color: Color(0xFFF3F4F6)),
                const SizedBox(height: 10),

                // Footer Row: Date & "View Details →"
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
                          dateStr,
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
