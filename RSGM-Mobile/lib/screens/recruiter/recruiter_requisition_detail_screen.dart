import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/recruiter/recruiter_requisition.dart';
import '../../services/api_client.dart';
import '../../services/recruiter/recruiter_requisition_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';

/// Read-only detail screen for viewing complete job requisition details and HR review statuses.
class RecruiterRequisitionDetailScreen extends StatefulWidget {
  const RecruiterRequisitionDetailScreen({
    super.key,
    required this.requisitionId,
    this.service,
    this.initialRequisition,
  });

  final String requisitionId;
  final RecruiterRequisitionService? service;
  final RecruiterRequisition? initialRequisition;

  @override
  State<RecruiterRequisitionDetailScreen> createState() =>
      _RecruiterRequisitionDetailScreenState();
}

class _RecruiterRequisitionDetailScreenState
    extends State<RecruiterRequisitionDetailScreen> {
  late final RecruiterRequisitionService _service;
  RecruiterRequisition? _requisition;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? RecruiterRequisitionService();
    if (widget.initialRequisition != null) {
      _requisition = widget.initialRequisition;
      _isLoading = false;
      _loadRequisition(silent: true);
    } else {
      _loadRequisition();
    }
  }

  Future<void> _loadRequisition({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    }

    try {
      final req = await _service.getRequisitionById(widget.requisitionId);
      if (!mounted) return;
      setState(() {
        _requisition = req;
        _isLoading = false;
        _error = null;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      if (_requisition == null) {
        setState(() {
          _error = e.message;
          _isLoading = false;
        });
      }
    } catch (_) {
      if (!mounted) return;
      if (_requisition == null) {
        setState(() {
          _error =
              'Unable to load requisition details. Please check your connection and try again.';
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        scrolledUnderElevation: 1,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded, color: AppTheme.ink),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          'Requisition Details',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.w700,
            color: AppTheme.ink,
          ),
        ),
      ),
      body: GradientBackdrop(
        child: SafeArea(
          child: _buildBody(),
        ),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(
        child: CircularProgressIndicator(color: AppTheme.violet),
      );
    }

    if (_error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Container(
            width: double.infinity,
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: const Color(0xFFFEF2F2),
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: const Color(0xFFFECACA)),
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(
                  Icons.error_outline_rounded,
                  size: 40,
                  color: Color(0xFFDC2626),
                ),
                const SizedBox(height: 12),
                Text(
                  _error!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 14,
                    color: Color(0xFF991B1B),
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 16),
                OutlinedButton.icon(
                  style: OutlinedButton.styleFrom(
                    foregroundColor: const Color(0xFFDC2626),
                    side: const BorderSide(color: Color(0xFFDC2626)),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(20),
                    ),
                  ),
                  icon: const Icon(Icons.refresh_rounded, size: 18),
                  label: const Text('Try again'),
                  onPressed: _loadRequisition,
                ),
              ],
            ),
          ),
        ),
      );
    }

    if (_requisition == null) {
      return const Center(child: Text('Requisition not found.'));
    }

    final req = _requisition!;
    final createdDateStr =
        DateFormat('d MMMM yyyy').format(req.createdAt.toLocal());
    final submittedDateStr = req.submittedAt != null
        ? DateFormat('d MMMM yyyy').format(req.submittedAt!.toLocal())
        : null;
    final reviewedDateStr = req.reviewedAt != null
        ? DateFormat('d MMMM yyyy').format(req.reviewedAt!.toLocal())
        : null;
    final approvedDateStr = req.approvedAt != null
        ? DateFormat('d MMMM yyyy').format(req.approvedAt!.toLocal())
        : null;

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // 1. Header Card
          _buildHeaderCard(req),
          const SizedBox(height: 16),

          // 2. HR Review & Approval Information
          _buildReviewCard(
            req: req,
            createdDate: createdDateStr,
            submittedDate: submittedDateStr,
            reviewedDate: reviewedDateStr,
            approvedDate: approvedDateStr,
          ),
          const SizedBox(height: 16),

          // 3. Employment & Compensation Card
          _buildEmploymentCard(req),
          const SizedBox(height: 16),

          // 4. Description (if available)
          if (req.description != null && req.description!.trim().isNotEmpty) ...[
            _buildSectionCard(
              title: 'Position Description',
              icon: Icons.description_outlined,
              content: req.description!.trim(),
            ),
            const SizedBox(height: 16),
          ],

          // 5. Responsibilities (if available)
          if (req.responsibilities != null &&
              req.responsibilities!.trim().isNotEmpty) ...[
            _buildSectionCard(
              title: 'Key Responsibilities',
              icon: Icons.checklist_rounded,
              content: req.responsibilities!.trim(),
            ),
            const SizedBox(height: 16),
          ],

          // 6. Requirements (if available)
          if (req.requirements != null &&
              req.requirements!.trim().isNotEmpty) ...[
            _buildSectionCard(
              title: 'Requirements & Qualifications',
              icon: Icons.verified_user_outlined,
              content: req.requirements!.trim(),
            ),
            const SizedBox(height: 16),
          ],

          // 7. Business Justification (if available)
          if (req.justification != null &&
              req.justification!.trim().isNotEmpty) ...[
            _buildSectionCard(
              title: 'Business Justification',
              icon: Icons.lightbulb_outline_rounded,
              content: req.justification!.trim(),
            ),
            const SizedBox(height: 16),
          ],

          // 8. Read-only Footer
          Center(
            child: Text(
              'Requisition ID: ${req.id} • Read-only view',
              style: const TextStyle(fontSize: 12, color: AppTheme.muted),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHeaderCard(RecruiterRequisition req) {
    final statusTheme = _getStatusTheme(req.status);

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .94),
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppTheme.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x08000000),
            blurRadius: 20,
            offset: Offset(0, 6),
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
                width: 52,
                height: 52,
                decoration: BoxDecoration(
                  color: const Color(0xFFF5F3FF),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: const Color(0xFFDDD6FE)),
                ),
                alignment: Alignment.center,
                child: const Icon(
                  Icons.assignment_outlined,
                  color: AppTheme.violet,
                  size: 28,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      req.positionTitle,
                      style: const TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        color: AppTheme.ink,
                        letterSpacing: -0.4,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      req.department,
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: AppTheme.violet,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${req.companyName} • ${req.location}',
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
          const Divider(height: 1, color: Color(0xFFF3F4F6)),
          const SizedBox(height: 14),

          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              // Status Badge
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                decoration: BoxDecoration(
                  color: statusTheme.background,
                  borderRadius: BorderRadius.circular(12),
                  border:
                      Border.all(color: statusTheme.color.withValues(alpha: 0.3)),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Container(
                      width: 7,
                      height: 7,
                      decoration: BoxDecoration(
                        color: statusTheme.color,
                        shape: BoxShape.circle,
                      ),
                    ),
                    const SizedBox(width: 6),
                    Text(
                      req.status.label,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        color: statusTheme.color,
                      ),
                    ),
                  ],
                ),
              ),

              // Headcount pill
              _buildPillTag(
                '${req.headcount} ${req.headcount == 1 ? "Opening" : "Openings"}',
                Icons.groups_outlined,
              ),

              // Employment Type pill
              _buildPillTag(
                req.employmentType.label,
                Icons.badge_outlined,
              ),

              // Work mode pill
              _buildPillTag(
                req.workMode.label,
                Icons.computer_rounded,
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildReviewCard({
    required RecruiterRequisition req,
    required String createdDate,
    required String? submittedDate,
    required String? reviewedDate,
    required String? approvedDate,
  }) {
    final statusTheme = _getStatusTheme(req.status);

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .94),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.verified_outlined, size: 18, color: statusTheme.color),
              const SizedBox(width: 8),
              const Text(
                'Approval & Workflow Status',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.ink,
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),

          _buildInfoRow(
            icon: Icons.flag_outlined,
            label: 'Workflow Status',
            value: req.status.label,
          ),
          const SizedBox(height: 12),

          _buildInfoRow(
            icon: Icons.calendar_today_outlined,
            label: 'Created Date',
            value: createdDate,
          ),

          if (submittedDate != null) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.send_outlined,
              label: 'Submitted for Review',
              value: submittedDate,
            ),
          ],

          if (reviewedDate != null) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.rate_review_outlined,
              label: 'Reviewed Date',
              value: reviewedDate,
            ),
          ],

          if (approvedDate != null) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.task_alt_rounded,
              label: 'Approved Date',
              value: approvedDate,
            ),
          ],

          if (req.reviewedByName != null &&
              req.reviewedByName!.trim().isNotEmpty) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.person_outline_rounded,
              label: 'Reviewer',
              value: req.reviewedByName!.trim(),
            ),
          ],

          // HR Feedback Box (if feedback exists from HR review)
          if (req.hrFeedback != null && req.hrFeedback!.trim().isNotEmpty) ...[
            const SizedBox(height: 16),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: req.isRejected
                    ? const Color(0xFFFEF2F2)
                    : const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                  color: req.isRejected
                      ? const Color(0xFFFECACA)
                      : const Color(0xFFE2E8F0),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(
                        req.isRejected
                            ? Icons.feedback_outlined
                            : Icons.comment_outlined,
                        size: 16,
                        color: req.isRejected
                            ? const Color(0xFFDC2626)
                            : AppTheme.violet,
                      ),
                      const SizedBox(width: 6),
                      Text(
                        'HR Review Feedback',
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w700,
                          color: req.isRejected
                              ? const Color(0xFF991B1B)
                              : AppTheme.ink,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text(
                    req.hrFeedback!.trim(),
                    style: TextStyle(
                      fontSize: 13,
                      height: 1.5,
                      color: req.isRejected
                          ? const Color(0xFF7F1D1D)
                          : const Color(0xFF334155),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildEmploymentCard(RecruiterRequisition req) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .94),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Position & Compensation Details',
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w700,
              color: AppTheme.ink,
            ),
          ),
          const SizedBox(height: 14),

          if (req.minSalary != null || req.maxSalary != null) ...[
            _buildInfoRow(
              icon: Icons.payments_outlined,
              label: 'Salary Range',
              value: req.salaryRangeFormatted,
            ),
            const SizedBox(height: 12),
          ],

          _buildInfoRow(
            icon: Icons.badge_outlined,
            label: 'Employment Type',
            value: req.employmentType.label,
          ),
          const SizedBox(height: 12),

          _buildInfoRow(
            icon: Icons.computer_rounded,
            label: 'Work Mode',
            value: req.workMode.label,
          ),
          const SizedBox(height: 12),

          _buildInfoRow(
            icon: Icons.trending_up_rounded,
            label: 'Experience Level',
            value: req.experienceLevel.label,
          ),

          if (req.minExperienceYears != null) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.work_history_outlined,
              label: 'Min. Experience',
              value: '${req.minExperienceYears} years required',
            ),
          ],

          const SizedBox(height: 12),
          _buildInfoRow(
            icon: Icons.person_search_outlined,
            label: 'Requested By',
            value: req.recruiterName.trim().isNotEmpty
                ? req.recruiterName
                : 'Recruiter',
          ),
        ],
      ),
    );
  }

  Widget _buildSectionCard({
    required String title,
    required IconData icon,
    required String content,
  }) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .94),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 18, color: AppTheme.violet),
              const SizedBox(width: 8),
              Text(
                title,
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.ink,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            content,
            style: const TextStyle(
              fontSize: 13,
              height: 1.55,
              color: AppTheme.ink,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildInfoRow({
    required IconData icon,
    required String label,
    required String value,
  }) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 18, color: AppTheme.violet),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: const TextStyle(fontSize: 12, color: AppTheme.muted),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: AppTheme.ink,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildPillTag(String label, IconData icon) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppTheme.border),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 12, color: AppTheme.muted),
          const SizedBox(width: 5),
          Text(
            label,
            style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: AppTheme.ink,
            ),
          ),
        ],
      ),
    );
  }

  _StatusTheme _getStatusTheme(RequisitionStatus status) {
    switch (status) {
      case RequisitionStatus.approved:
        return const _StatusTheme(
          color: Color(0xFF059669),
          background: Color(0xFFECFDF5),
        );
      case RequisitionStatus.rejected:
        return const _StatusTheme(
          color: Color(0xFFDC2626),
          background: Color(0xFFFEF2F2),
        );
      case RequisitionStatus.submitted:
        return const _StatusTheme(
          color: Color(0xFF2563EB),
          background: Color(0xFFEFF6FF),
        );
      case RequisitionStatus.draft:
        return const _StatusTheme(
          color: Color(0xFFD97706),
          background: Color(0xFFFFFBEB),
        );
    }
  }
}

class _StatusTheme {
  const _StatusTheme({required this.color, required this.background});
  final Color color;
  final Color background;
}
