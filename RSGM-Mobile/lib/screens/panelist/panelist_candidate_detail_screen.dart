import 'package:flutter/material.dart';
import 'package:file_picker/file_picker.dart';

import '../../models/panelist/panelist_candidate.dart';
import '../../services/api_client.dart';
import '../../services/panelist/panelist_shortlist_service.dart';
import '../../theme/app_theme.dart';


class PanelistCandidateDetailScreen extends StatefulWidget {
  const PanelistCandidateDetailScreen({
    super.key,
    required this.applicationId,
    this.service,
  });

  final String applicationId;
  final PanelistShortlistService? service;

  @override
  State<PanelistCandidateDetailScreen> createState() =>
      _PanelistCandidateDetailScreenState();
}

class _PanelistCandidateDetailScreenState
    extends State<PanelistCandidateDetailScreen> {
  static const Color amber = Color(0xFFD97706);
  static const Color amberSoft = Color(0xFFFFFBEB);
  static const Color amberBorder = Color(0xFFFDE68A);

  late final PanelistShortlistService _service;

  PanelistCandidate? _candidate;
  bool _loading = true;
  String? _error;
  bool _savingCv = false;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? PanelistShortlistService();
    _loadCandidate();
  }

  Future<void> _loadCandidate() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final candidate =
          await _service.getCandidate(widget.applicationId);

      if (!mounted) return;

      setState(() {
        _candidate = candidate;
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
        _error = 'Unable to load candidate details.';
        _loading = false;
      });
    }
  }

  Future<void> _saveCv() async {
    if (_savingCv) return;

    setState(() {
      _savingCv = true;
    });

    try {
      final cv = await _service.downloadCandidateCv(
       widget.applicationId,
      );

      if (cv.isEmpty) {
        throw const ApiException(
          message: 'The downloaded CV is empty.',
        );
      }

      final savedUri = await FilePicker.saveFile(
        dialogTitle: 'Save candidate CV',
        fileName: cv.fileName,
        bytes: cv.bytes,
        mimeType: cv.contentType,
      );

      if (!mounted) return;

      if (savedUri != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'CV saved successfully: ${cv.fileName}',
          ),
        ),
      );
    }
    } on ApiException catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(e.message),
        ),
      );
    } catch (_) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Unable to save the candidate CV.'),
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _savingCv = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        scrolledUnderElevation: 1,
        leading: IconButton(
          icon: const Icon(
            Icons.arrow_back_rounded,
            color: AppTheme.ink,
          ),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          'Candidate Details',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.w700,
            color: AppTheme.ink,
          ),
        ),
      ),
      body: SafeArea(
        child: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(
        child: CircularProgressIndicator(color: amber),
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
              border: Border.all(
                color: const Color(0xFFFECACA),
              ),
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
                    color: Color(0xFF991B1B),
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 16),
                OutlinedButton.icon(
                  onPressed: _loadCandidate,
                  icon: const Icon(Icons.refresh_rounded),
                  label: const Text('Try again'),
                ),
              ],
            ),
          ),
        ),
      );
    }

    final candidate = _candidate;

    if (candidate == null) {
      return const Center(
        child: Text('Candidate not found.'),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadCandidate,
      color: amber,
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildHeader(candidate),
            const SizedBox(height: 16),

            if (candidate.bio != null &&
                candidate.bio!.trim().isNotEmpty) ...[
              _buildSection(
                title: 'About',
                icon: Icons.person_outline_rounded,
                child: Text(
                  candidate.bio!,
                  style: const TextStyle(
                    fontSize: 14,
                    height: 1.5,
                    color: AppTheme.muted,
                  ),
                ),
              ),
              const SizedBox(height: 16),
            ],

            _buildSkills(candidate),
            const SizedBox(height: 16),

            _buildWorkExperience(candidate),
            const SizedBox(height: 16),

            _buildEducation(candidate),
            const SizedBox(height: 16),

            _buildProfessionalLinks(candidate),
            const SizedBox(height: 16),

            _buildCv(candidate),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(PanelistCandidate candidate) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: amberSoft,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: amberBorder),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'CANDIDATE PROFILE',
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w700,
              color: amber,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            candidate.fullName,
            style: const TextStyle(
              fontSize: 24,
              fontWeight: FontWeight.w800,
              color: AppTheme.ink,
            ),
          ),

          if (candidate.jobTitle != null &&
              candidate.jobTitle!.trim().isNotEmpty) ...[
            const SizedBox(height: 4),
            Text(
              candidate.jobTitle!,
              style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: amber,
              ),
            ),
          ],

          if (candidate.headline != null &&
              candidate.headline!.trim().isNotEmpty) ...[
            const SizedBox(height: 4),
            Text(
              candidate.headline!,
              style: const TextStyle(
                fontSize: 14,
                color: AppTheme.muted,
              ),
            ),
          ],

          const SizedBox(height: 18),
          _detailRow(
            Icons.email_outlined,
            candidate.email,
          ),

          if (candidate.phoneNumber != null &&
              candidate.phoneNumber!.trim().isNotEmpty)
            _detailRow(
              Icons.phone_outlined,
              candidate.phoneNumber!,
            ),

          if (candidate.location != null &&
              candidate.location!.trim().isNotEmpty)
            _detailRow(
              Icons.location_on_outlined,
              candidate.location!,
            ),
        ],
      ),
    );
  }

  Widget _detailRow(IconData icon, String text) {
    return Padding(
      padding: const EdgeInsets.only(top: 7),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            icon,
            size: 17,
            color: amber,
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              text,
              style: const TextStyle(
                fontSize: 13,
                color: AppTheme.ink,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSkills(PanelistCandidate candidate) {
    return _buildSection(
      title: 'Skills',
      icon: Icons.psychology_outlined,
      child: candidate.skills.isEmpty
          ? const Text(
              'No skills provided.',
              style: TextStyle(color: AppTheme.muted),
            )
          : Wrap(
              spacing: 8,
              runSpacing: 8,
              children: candidate.skills
                  .map(
                    (skill) => Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 7,
                      ),
                      decoration: BoxDecoration(
                        color: amberSoft,
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(color: amberBorder),
                      ),
                      child: Text(
                        '${skill.name} • ${skill.proficiencyLevel}/5',
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: amber,
                        ),
                      ),
                    ),
                  )
                  .toList(),
            ),
    );
  }

  Widget _buildWorkExperience(PanelistCandidate candidate) {
    return _buildSection(
      title: 'Work experience',
      icon: Icons.work_outline_rounded,
      child: candidate.workExperience.isEmpty
          ? const Text(
              'No work experience provided.',
              style: TextStyle(color: AppTheme.muted),
            )
          : Column(
              children: candidate.workExperience
                  .map(
                    (item) => _infoItem(
                      title:
                          '${item.jobTitle} • ${item.companyName}',
                      subtitle: [
                        if (item.location != null &&
                            item.location!.trim().isNotEmpty)
                          item.location!,
                        if (item.startDate != null)
                          '${item.startDate} - '
                              '${item.isCurrent ? 'Present' : item.endDate ?? ''}',
                      ].join('\n'),
                    ),
                  )
                  .toList(),
            ),
    );
  }

  Widget _buildEducation(PanelistCandidate candidate) {
    return _buildSection(
      title: 'Education',
      icon: Icons.school_outlined,
      child: candidate.education.isEmpty
          ? const Text(
              'No education records provided.',
              style: TextStyle(color: AppTheme.muted),
            )
          : Column(
              children: candidate.education
                  .map(
                    (item) => _infoItem(
                      title:
                          '${item.degree} • ${item.institution}',
                      subtitle: [
                        if (item.fieldOfStudy != null &&
                            item.fieldOfStudy!.trim().isNotEmpty)
                          item.fieldOfStudy!,
                        if (item.startDate != null)
                          '${item.startDate} - '
                              '${item.isCurrent ? 'Present' : item.endDate ?? ''}',
                      ].join('\n'),
                    ),
                  )
                  .toList(),
            ),
    );
  }

  Widget _buildProfessionalLinks(
    PanelistCandidate candidate,
  ) {
    final links = <String>[
      if (candidate.linkedInUrl != null &&
          candidate.linkedInUrl!.trim().isNotEmpty)
        'LinkedIn: ${candidate.linkedInUrl}',
      if (candidate.gitHubUrl != null &&
          candidate.gitHubUrl!.trim().isNotEmpty)
        'GitHub: ${candidate.gitHubUrl}',
      if (candidate.portfolioUrl != null &&
          candidate.portfolioUrl!.trim().isNotEmpty)
        'Portfolio: ${candidate.portfolioUrl}',
    ];

    return _buildSection(
      title: 'Professional links',
      icon: Icons.link_rounded,
      child: links.isEmpty
          ? const Text(
              'No professional links provided.',
              style: TextStyle(color: AppTheme.muted),
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: links
                  .map(
                    (link) => Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: Text(
                        link,
                        style: const TextStyle(
                          fontSize: 13,
                          color: amber,
                        ),
                      ),
                    ),
                  )
                  .toList(),
            ),
    );
  }

  Widget _buildCv(PanelistCandidate candidate) {
    return _buildSection(
      title: 'CV',
      icon: Icons.description_outlined,
      child: candidate.hasCv
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Icon(
                      Icons.check_circle_rounded,
                      size: 20,
                      color: Color(0xFF059669),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        candidate.cvFileName ??
                            'Candidate CV available',
                        style: const TextStyle(
                        fontWeight: FontWeight.w600,
                        color: AppTheme.ink,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    onPressed: _savingCv ? null : _saveCv,
                    style: FilledButton.styleFrom(
                      backgroundColor: amber,
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(
                        vertical: 13,
                      ),
                    ),
                    icon: _savingCv
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Icon(
                            Icons.download_rounded,
                          ),
                    label: Text(
                      _savingCv
                          ? 'Downloading...'
                          : 'Save CV',
                  ),
                ),
              ),
            ],
          )
          : const Text(
              'No CV uploaded.',
              style: TextStyle(
                color: AppTheme.muted,
              ),
            ),
    );
  }

  Widget _buildSection({
    required String title,
    required IconData icon,
    required Widget child,
  }) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                icon,
                size: 20,
                color: amber,
              ),
              const SizedBox(width: 9),
              Text(
                title,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.ink,
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          child,
        ],
      ),
    );
  }

  Widget _infoItem({
    required String title,
    required String subtitle,
  }) {
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(13),
      decoration: BoxDecoration(
        color: const Color(0xFFFAFAFA),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppTheme.ink,
            ),
          ),
          if (subtitle.trim().isNotEmpty) ...[
            const SizedBox(height: 5),
            Text(
              subtitle,
              style: const TextStyle(
                fontSize: 12,
                height: 1.4,
                color: AppTheme.muted,
              ),
            ),
          ],
        ],
      ),
    );
  }
}