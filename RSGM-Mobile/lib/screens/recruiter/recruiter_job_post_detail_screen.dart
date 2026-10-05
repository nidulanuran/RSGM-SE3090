import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/recruiter/recruiter_job_post.dart';
import '../../services/api_client.dart';
import '../../services/recruiter/recruiter_job_post_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';

/// Read-only detail screen for viewing complete job post information.
class RecruiterJobPostDetailScreen extends StatefulWidget {
  const RecruiterJobPostDetailScreen({
    super.key,
    required this.jobId,
    this.service,
    this.initialJob,
  });

  final String jobId;
  final RecruiterJobPostService? service;
  final RecruiterJobPost? initialJob;

  @override
  State<RecruiterJobPostDetailScreen> createState() =>
      _RecruiterJobPostDetailScreenState();
}

class _RecruiterJobPostDetailScreenState
    extends State<RecruiterJobPostDetailScreen> {
  late final RecruiterJobPostService _service;
  RecruiterJobPost? _job;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? RecruiterJobPostService();
    if (widget.initialJob != null) {
      _job = widget.initialJob;
      _isLoading = false;
      _loadJob(silent: true);
    } else {
      _loadJob();
    }
  }

  Future<void> _loadJob({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    }

    try {
      final job = await _service.getJobPostingById(widget.jobId);
      if (!mounted) return;
      setState(() {
        _job = job;
        _isLoading = false;
        _error = null;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      if (_job == null) {
        setState(() {
          _error = e.message;
          _isLoading = false;
        });
      }
    } catch (_) {
      if (!mounted) return;
      if (_job == null) {
        setState(() {
          _error =
              'Unable to load job post details. Please check your connection and try again.';
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
          'Job Post Details',
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
                const Icon(Icons.error_outline_rounded,
                    size: 40, color: Color(0xFFDC2626)),
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
                  onPressed: _loadJob,
                ),
              ],
            ),
          ),
        ),
      );
    }

    if (_job == null) {
      return const Center(child: Text('Job post not found.'));
    }

    final job = _job!;
    final deadlineStr = job.applicationDeadline != null
        ? DateFormat('d MMMM yyyy').format(job.applicationDeadline!)
        : null;

    final createdAtStr =
        DateFormat('d MMMM yyyy').format(job.createdAt.toLocal());

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header Card
          _buildHeaderCard(job),
          const SizedBox(height: 16),

          // Key Highlights Card (Salary, Experience, Deadline, Applicants)
          _buildHighlightsCard(job, deadlineStr),
          const SizedBox(height: 16),

          // Required Skills
          if (job.requiredSkills.isNotEmpty) ...[
            _buildSkillsCard(job),
            const SizedBox(height: 16),
          ],

          // Description
          if (job.description != null && job.description!.trim().isNotEmpty) ...[
            _buildSectionCard(
              title: 'Job Description',
              icon: Icons.description_outlined,
              content: job.description!.trim(),
            ),
            const SizedBox(height: 16),
          ],

          // Responsibilities
          if (job.responsibilities.trim().isNotEmpty) ...[
            _buildSectionCard(
              title: 'Key Responsibilities',
              icon: Icons.checklist_rounded,
              content: job.responsibilities.trim(),
            ),
            const SizedBox(height: 16),
          ],

          // Requirements
          if (job.requirements.trim().isNotEmpty) ...[
            _buildSectionCard(
              title: 'Requirements & Qualifications',
              icon: Icons.verified_user_outlined,
              content: job.requirements.trim(),
            ),
            const SizedBox(height: 16),
          ],

          // Metadata footer
          Center(
            child: Text(
              'Posted on $createdAtStr • Read-only view',
              style: const TextStyle(fontSize: 12, color: AppTheme.muted),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHeaderCard(RecruiterJobPost job) {
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
              // Company Logo or Letter Avatar
              _buildCompanyLogo(job),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      job.title,
                      style: const TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        color: AppTheme.ink,
                        letterSpacing: -0.5,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      job.company,
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: AppTheme.muted,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        const Icon(Icons.location_on_outlined,
                            size: 14, color: AppTheme.muted),
                        const SizedBox(width: 4),
                        Expanded(
                          child: Text(
                            job.location,
                            style: const TextStyle(
                              fontSize: 13,
                              color: AppTheme.muted,
                            ),
                          ),
                        ),
                      ],
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
                  color: statusBg,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: statusColor.withValues(alpha: 0.3)),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Container(
                      width: 7,
                      height: 7,
                      decoration: BoxDecoration(
                        color: statusColor,
                        shape: BoxShape.circle,
                      ),
                    ),
                    const SizedBox(width: 6),
                    Text(
                      job.status,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        color: statusColor,
                      ),
                    ),
                  ],
                ),
              ),
              _buildPillTag(job.employmentTypeLabel, Icons.badge_outlined),
              _buildPillTag(job.workModeLabel, Icons.computer_rounded),
              _buildPillTag(
                  job.experienceLevelLabel, Icons.trending_up_rounded),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildCompanyLogo(RecruiterJobPost job) {
    if (job.companyLogoUrl != null &&
        job.companyLogoUrl!.trim().startsWith('http')) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: Image.network(
          job.companyLogoUrl!.trim(),
          width: 50,
          height: 50,
          fit: BoxFit.cover,
          errorBuilder: (_, _, _) => _buildFallbackAvatar(job.company),
        ),
      );
    }
    return _buildFallbackAvatar(job.company);
  }

  Widget _buildFallbackAvatar(String company) {
    final letter = company.trim().isNotEmpty ? company.trim()[0].toUpperCase() : 'C';
    return Container(
      width: 50,
      height: 50,
      decoration: BoxDecoration(
        color: const Color(0xFFF5F3FF),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFDDD6FE)),
      ),
      alignment: Alignment.center,
      child: Text(
        letter,
        style: const TextStyle(
          fontSize: 22,
          fontWeight: FontWeight.w800,
          color: AppTheme.violet,
        ),
      ),
    );
  }

  Widget _buildHighlightsCard(RecruiterJobPost job, String? deadlineStr) {
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
            'Overview & Compensation',
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w700,
              color: AppTheme.ink,
            ),
          ),
          const SizedBox(height: 14),
          _buildInfoRow(
            icon: Icons.payments_outlined,
            label: 'Salary Range',
            value: job.salaryRangeFormatted,
          ),
          if (job.minExperienceYears != null) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.work_history_outlined,
              label: 'Min. Experience',
              value: '${job.minExperienceYears} years required',
            ),
          ],
          const SizedBox(height: 12),
          _buildInfoRow(
            icon: Icons.people_outline_rounded,
            label: 'Applicants',
            value: '${job.applicantCount} Candidate(s)',
          ),
          if (deadlineStr != null) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.event_available_outlined,
              label: 'Application Deadline',
              value: deadlineStr,
            ),
          ],
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
                  fontSize: 14,
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

  Widget _buildSkillsCard(RecruiterJobPost job) {
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
              const Icon(Icons.psychology_outlined,
                  size: 20, color: AppTheme.violet),
              const SizedBox(width: 8),
              Text(
                'Required Skills (${job.requiredSkills.length})',
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.ink,
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: job.requiredSkills.map((skill) {
              return Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
                decoration: BoxDecoration(
                  color: const Color(0xFFF5F3FF),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: const Color(0xFFDDD6FE)),
                ),
                child: Text(
                  skill.weight > 1.0
                      ? '${skill.name} (${skill.weight.toStringAsFixed(1)}x weight)'
                      : skill.name,
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: AppTheme.violet,
                  ),
                ),
              );
            }).toList(),
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
              fontSize: 14,
              height: 1.55,
              color: AppTheme.ink,
            ),
          ),
        ],
      ),
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
}
