import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/recruiter/recruiter_applicant.dart';
import '../../services/api_client.dart';
import '../../services/recruiter/recruiter_application_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';

/// Read-only detail screen for viewing complete candidate application information.
class RecruiterApplicantDetailScreen extends StatefulWidget {
  const RecruiterApplicantDetailScreen({
    super.key,
    required this.applicantId,
    this.service,
    this.initialApplicant,
  });

  final String applicantId;
  final RecruiterApplicationService? service;
  final RecruiterApplicant? initialApplicant;

  @override
  State<RecruiterApplicantDetailScreen> createState() =>
      _RecruiterApplicantDetailScreenState();
}

class _RecruiterApplicantDetailScreenState
    extends State<RecruiterApplicantDetailScreen> {
  late final RecruiterApplicationService _service;
  RecruiterApplicant? _applicant;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? RecruiterApplicationService();
    if (widget.initialApplicant != null) {
      _applicant = widget.initialApplicant;
      _isLoading = false;
      _loadApplicant(silent: true);
    } else {
      _loadApplicant();
    }
  }

  Future<void> _loadApplicant({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    }

    try {
      final applicant =
          await _service.getApplicationById(widget.applicantId);
      if (!mounted) return;
      setState(() {
        _applicant = applicant;
        _isLoading = false;
        _error = null;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      if (_applicant == null) {
        setState(() {
          _error = e.message;
          _isLoading = false;
        });
      }
    } catch (_) {
      if (!mounted) return;
      if (_applicant == null) {
        setState(() {
          _error =
              'Unable to load applicant details. Please check your connection and try again.';
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
          'Applicant Details',
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
                  onPressed: _loadApplicant,
                ),
              ],
            ),
          ),
        ),
      );
    }

    if (_applicant == null) {
      return const Center(child: Text('Applicant not found.'));
    }

    final applicant = _applicant!;
    final appliedDateStr =
        DateFormat('d MMMM yyyy').format(applicant.appliedAt.toLocal());

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // 1. Candidate Header Card
          _buildHeaderCard(applicant),
          const SizedBox(height: 16),

          // 2. Application & Contact Overview Card
          _buildOverviewCard(applicant, appliedDateStr),
          const SizedBox(height: 16),

          // 3. Match Information Card (if matchScore > 0 or explanation exists)
          if (applicant.matchScore > 0 ||
              applicant.matchExplanation.trim().isNotEmpty ||
              applicant.matchBreakdown.isNotEmpty) ...[
            _buildMatchCard(applicant),
            const SizedBox(height: 16),
          ],

          // 4. Matched Skills Card
          if (applicant.matchedSkills.isNotEmpty) ...[
            _buildSkillsSectionCard(
              title: 'Matched Skills (${applicant.matchedSkills.length})',
              icon: Icons.check_circle_outline_rounded,
              iconColor: const Color(0xFF059669),
              skills: applicant.matchedSkills,
              chipBg: const Color(0xFFECFDF5),
              chipBorder: const Color(0xFFA7F3D0),
              chipText: const Color(0xFF065F46),
            ),
            const SizedBox(height: 16),
          ],

          // 5. Missing Skills Card
          if (applicant.missingSkills.isNotEmpty) ...[
            _buildSkillsSectionCard(
              title: 'Missing Skills (${applicant.missingSkills.length})',
              icon: Icons.highlight_off_rounded,
              iconColor: const Color(0xFFDC2626),
              skills: applicant.missingSkills,
              chipBg: const Color(0xFFFEF2F2),
              chipBorder: const Color(0xFFFECACA),
              chipText: const Color(0xFF991B1B),
            ),
            const SizedBox(height: 16),
          ],

          // 6. Candidate Skills (All declared skills)
          if (applicant.skills.isNotEmpty) ...[
            _buildSkillsSectionCard(
              title: 'Candidate Skills (${applicant.skills.length})',
              icon: Icons.psychology_outlined,
              iconColor: AppTheme.violet,
              skills: applicant.skills,
              chipBg: const Color(0xFFF5F3FF),
              chipBorder: const Color(0xFFDDD6FE),
              chipText: AppTheme.violet,
            ),
            const SizedBox(height: 16),
          ],

          // 7. Work Experience Card
          if (applicant.workExperience.isNotEmpty) ...[
            _buildListSectionCard(
              title: 'Work Experience',
              icon: Icons.work_history_outlined,
              items: applicant.workExperience,
            ),
            const SizedBox(height: 16),
          ],

          // 8. Education Card
          if (applicant.education.isNotEmpty) ...[
            _buildListSectionCard(
              title: 'Education',
              icon: Icons.school_outlined,
              items: applicant.education,
            ),
            const SizedBox(height: 16),
          ],

          // 9. Bio Card (if present)
          if (applicant.bio != null && applicant.bio!.trim().isNotEmpty) ...[
            _buildTextSectionCard(
              title: 'Candidate Bio',
              icon: Icons.person_outline_rounded,
              text: applicant.bio!.trim(),
            ),
            const SizedBox(height: 16),
          ],

          // 10. Read-only Footer
          Center(
            child: Text(
              'Application ID: ${applicant.id} • Read-only view',
              style: const TextStyle(fontSize: 12, color: AppTheme.muted),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHeaderCard(RecruiterApplicant applicant) {
    final statusTheme = _getStatusColor(applicant);

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
              // Avatar circle with initials
              Container(
                width: 56,
                height: 56,
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    colors: [Color(0xFF7C3AED), Color(0xFF4F46E5)],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                  borderRadius: BorderRadius.circular(18),
                  boxShadow: const [
                    BoxShadow(
                      color: Color(0x207C3AED),
                      blurRadius: 10,
                      offset: Offset(0, 4),
                    ),
                  ],
                ),
                alignment: Alignment.center,
                child: Text(
                  applicant.initials,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      applicant.fullName,
                      style: const TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        color: AppTheme.ink,
                        letterSpacing: -0.4,
                      ),
                    ),
                    if (applicant.headline != null &&
                        applicant.headline!.trim().isNotEmpty) ...[
                      const SizedBox(height: 3),
                      Text(
                        applicant.headline!.trim(),
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w600,
                          color: AppTheme.violet,
                        ),
                      ),
                    ],
                    if (applicant.location != null &&
                        applicant.location!.trim().isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          const Icon(
                            Icons.location_on_outlined,
                            size: 14,
                            color: AppTheme.muted,
                          ),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              applicant.location!.trim(),
                              style: const TextStyle(
                                fontSize: 13,
                                color: AppTheme.muted,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          const Divider(height: 1, color: Color(0xFFF3F4F6)),
          const SizedBox(height: 14),

          // Status & Match pill row
          Wrap(
            spacing: 8,
            runSpacing: 8,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              // Status Badge
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                decoration: BoxDecoration(
                  color: statusTheme.background,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: statusTheme.color.withValues(alpha: 0.3)),
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
                      applicant.statusLabel,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        color: statusTheme.color,
                      ),
                    ),
                  ],
                ),
              ),

              // Match Score Badge
              if (applicant.matchScore > 0)
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                  decoration: BoxDecoration(
                    color: const Color(0xFFEFF6FF),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: const Color(0xFFBFDBFE)),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.auto_graph_rounded,
                          size: 13, color: Color(0xFF2563EB)),
                      const SizedBox(width: 5),
                      Text(
                        'Match: ${applicant.matchScore}%',
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                          color: Color(0xFF1D4ED8),
                        ),
                      ),
                    ],
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildOverviewCard(RecruiterApplicant applicant, String appliedDateStr) {
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
            'Application & Contact Information',
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w700,
              color: AppTheme.ink,
            ),
          ),
          const SizedBox(height: 14),

          _buildInfoRow(
            icon: Icons.work_outline_rounded,
            label: 'Applied Job Position',
            value: applicant.jobTitle,
          ),
          const SizedBox(height: 12),

          _buildInfoRow(
            icon: Icons.event_available_outlined,
            label: 'Date Applied',
            value: appliedDateStr,
          ),
          const SizedBox(height: 12),

          _buildInfoRow(
            icon: Icons.email_outlined,
            label: 'Email Address',
            value: applicant.email,
          ),

          if (applicant.phoneNumber != null &&
              applicant.phoneNumber!.trim().isNotEmpty) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.phone_outlined,
              label: 'Phone Number',
              value: applicant.phoneNumber!.trim(),
            ),
          ],

          // Informational CV metadata (strictly read-only, NO download/view actions)
          const SizedBox(height: 12),
          _buildInfoRow(
            icon: Icons.description_outlined,
            label: 'Curriculum Vitae (CV)',
            value: applicant.hasCv
                ? (applicant.cvFileName != null &&
                        applicant.cvFileName!.trim().isNotEmpty
                    ? 'CV on file (${applicant.cvFileName!.trim()})'
                    : 'CV provided')
                : 'No CV provided',
          ),

          // External Profile URLs (read-only text presentation)
          if (applicant.linkedInUrl != null &&
              applicant.linkedInUrl!.trim().isNotEmpty) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.link_rounded,
              label: 'LinkedIn Profile',
              value: applicant.linkedInUrl!.trim(),
            ),
          ],
          if (applicant.gitHubUrl != null &&
              applicant.gitHubUrl!.trim().isNotEmpty) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.code_rounded,
              label: 'GitHub Profile',
              value: applicant.gitHubUrl!.trim(),
            ),
          ],
          if (applicant.portfolioUrl != null &&
              applicant.portfolioUrl!.trim().isNotEmpty) ...[
            const SizedBox(height: 12),
            _buildInfoRow(
              icon: Icons.language_rounded,
              label: 'Portfolio Website',
              value: applicant.portfolioUrl!.trim(),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildMatchCard(RecruiterApplicant applicant) {
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
              const Icon(Icons.analytics_outlined,
                  size: 20, color: AppTheme.violet),
              const SizedBox(width: 8),
              const Text(
                'Skill Match Information',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.ink,
                ),
              ),
              const Spacer(),
              if (applicant.matchScore > 0)
                Text(
                  '${applicant.matchScore}%',
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                    color: AppTheme.violet,
                  ),
                ),
            ],
          ),
          const SizedBox(height: 12),

          // Exact match score
          if (applicant.exactMatchScore > 0) ...[
            Row(
              children: [
                const Text(
                  'Exact Match Score: ',
                  style: TextStyle(fontSize: 12, color: AppTheme.muted),
                ),
                Text(
                  '${applicant.exactMatchScore.toStringAsFixed(1)}%',
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                    color: AppTheme.ink,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
          ],

          // Match Explanation
          if (applicant.matchExplanation.trim().isNotEmpty) ...[
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: const Color(0xFFE2E8F0)),
              ),
              child: Text(
                applicant.matchExplanation.trim(),
                style: const TextStyle(
                  fontSize: 13,
                  color: Color(0xFF334155),
                  height: 1.5,
                ),
              ),
            ),
            const SizedBox(height: 14),
          ],

          // Skill Match Breakdown list
          if (applicant.matchBreakdown.isNotEmpty) ...[
            const Text(
              'Skill Breakdown Details',
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: AppTheme.ink,
              ),
            ),
            const SizedBox(height: 8),
            ...applicant.matchBreakdown.map((item) {
              return Container(
                margin: const EdgeInsets.only(bottom: 8),
                padding:
                    const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                decoration: BoxDecoration(
                  color: item.matched
                      ? const Color(0xFFF0FDF4)
                      : const Color(0xFFFEF2F2),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(
                    color: item.matched
                        ? const Color(0xFFBBF7D0)
                        : const Color(0xFFFECACA),
                  ),
                ),
                child: Row(
                  children: [
                    Icon(
                      item.matched
                          ? Icons.check_circle_rounded
                          : Icons.cancel_rounded,
                      size: 16,
                      color: item.matched
                          ? const Color(0xFF16A34A)
                          : const Color(0xFFDC2626),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            item.skillName,
                            style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: AppTheme.ink,
                            ),
                          ),
                          if (item.proficiencyLabel != null &&
                              item.proficiencyLabel!.trim().isNotEmpty)
                            Text(
                              item.proficiencyLabel!.trim(),
                              style: const TextStyle(
                                fontSize: 11,
                                color: AppTheme.muted,
                              ),
                            ),
                        ],
                      ),
                    ),
                    if (item.contributionPercentage > 0)
                      Text(
                        '+${item.contributionPercentage.toStringAsFixed(1)}%',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                          color: item.matched
                              ? const Color(0xFF15803D)
                              : const Color(0xFFB91C1C),
                        ),
                      ),
                  ],
                ),
              );
            }),
          ],
        ],
      ),
    );
  }

  Widget _buildSkillsSectionCard({
    required String title,
    required IconData icon,
    required Color iconColor,
    required List<String> skills,
    required Color chipBg,
    required Color chipBorder,
    required Color chipText,
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
              Icon(icon, size: 18, color: iconColor),
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
          const SizedBox(height: 14),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: skills.map((skill) {
              return Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(
                  color: chipBg,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: chipBorder),
                ),
                child: Text(
                  skill,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: chipText,
                  ),
                ),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }

  Widget _buildListSectionCard({
    required String title,
    required IconData icon,
    required List<String> items,
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
          const SizedBox(height: 14),
          ...items.map((item) {
            return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    margin: const EdgeInsets.only(top: 6),
                    width: 6,
                    height: 6,
                    decoration: const BoxDecoration(
                      color: AppTheme.violet,
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      item,
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppTheme.ink,
                        height: 1.45,
                      ),
                    ),
                  ),
                ],
              ),
            );
          }),
        ],
      ),
    );
  }

  Widget _buildTextSectionCard({
    required String title,
    required IconData icon,
    required String text,
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
            text,
            style: const TextStyle(
              fontSize: 13,
              height: 1.5,
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

  _StatusTheme _getStatusColor(RecruiterApplicant applicant) {
    if (applicant.isShortlisted) {
      return const _StatusTheme(
        color: Color(0xFF059669),
        background: Color(0xFFECFDF5),
      );
    } else if (applicant.isInterview) {
      return const _StatusTheme(
        color: Color(0xFF4F46E5),
        background: Color(0xFFEEF2FF),
      );
    } else if (applicant.isUnderReview) {
      return const _StatusTheme(
        color: Color(0xFFD97706),
        background: Color(0xFFFFFBEB),
      );
    } else if (applicant.isRejected) {
      return const _StatusTheme(
        color: Color(0xFFDC2626),
        background: Color(0xFFFEF2F2),
      );
    }
    return const _StatusTheme(
      color: Color(0xFF4B5563),
      background: Color(0xFFF3F4F6),
    );
  }
}

class _StatusTheme {
  const _StatusTheme({required this.color, required this.background});
  final Color color;
  final Color background;
}
