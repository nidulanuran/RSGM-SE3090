import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

import '../../models/jobseeker/jobseeker_models.dart';
import '../../services/api_client.dart';
import '../../services/jobseeker/jobseeker_service.dart';
import '../../theme/app_theme.dart';

class JobSeekerProfileScreen extends StatefulWidget {
  const JobSeekerProfileScreen({
    super.key,
    required this.service,
  });

  final JobSeekerService service;

  @override
  State<JobSeekerProfileScreen> createState() => _JobSeekerProfileScreenState();
}

class _JobSeekerProfileScreenState extends State<JobSeekerProfileScreen> {
  static const int _maxCvSizeBytes = 5 * 1024 * 1024;

  JobSeekerProfile? profile;
  List<JobSeekerSkill> skills = [];
  List<SkillCatalogItem> catalog = [];
  JobSeekerCv? cv;

  bool loading = true;
  bool cvUploading = false;
  bool cvDeleting = false;
  String? error;

  bool get _cvBusy => cvUploading || cvDeleting;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => loading = true);

    try {
      final p = await widget.service.getProfile();
      final s = await widget.service.getSkills();
      final c = await widget.service.getSkillCatalog();
      final v = await widget.service.getCv();

      if (!mounted) {
        return;
      }

      setState(() {
        profile = p;
        skills = s;
        catalog = c;
        cv = v;
        error = null;
      });
    } on ApiException catch (e) {
      if (mounted) {
        setState(() => error = e.message);
      }
    } finally {
      if (mounted) {
        setState(() => loading = false);
      }
    }
  }

  Future<void> _editProfile() async {
    final p = profile!;

    final name = TextEditingController(text: p.fullName);
    final headline = TextEditingController(text: p.headline);
    final location = TextEditingController(text: p.location);
    final bio = TextEditingController(text: p.bio);
    final linkedIn = TextEditingController(text: p.linkedInUrl);
    final gitHub = TextEditingController(text: p.gitHubUrl);
    final portfolio = TextEditingController(text: p.portfolioUrl);

    final save = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => Dialog(
        insetPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 520),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 20, 20, 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Edit profile',
                  style: TextStyle(fontSize: 24, fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 18),
                Flexible(
                  child: SingleChildScrollView(
                    child: Column(
                      children: [
                        TextField(
                          controller: name,
                          decoration: const InputDecoration(labelText: 'Full name'),
                        ),
                        const SizedBox(height: 14),
                        TextField(
                          controller: headline,
                          decoration: const InputDecoration(labelText: 'Headline'),
                        ),
                        const SizedBox(height: 14),
                        TextField(
                          controller: location,
                          decoration: const InputDecoration(labelText: 'Location'),
                        ),
                        const SizedBox(height: 14),
                        TextField(
                          controller: bio,
                          minLines: 3,
                          maxLines: 5,
                          decoration: const InputDecoration(labelText: 'Bio'),
                        ),
                        const SizedBox(height: 14),
                        TextField(
                          controller: linkedIn,
                          keyboardType: TextInputType.url,
                          decoration: const InputDecoration(labelText: 'LinkedIn URL'),
                        ),
                        const SizedBox(height: 14),
                        TextField(
                          controller: gitHub,
                          keyboardType: TextInputType.url,
                          decoration: const InputDecoration(labelText: 'GitHub URL'),
                        ),
                        const SizedBox(height: 14),
                        TextField(
                          controller: portfolio,
                          keyboardType: TextInputType.url,
                          decoration: const InputDecoration(labelText: 'Portfolio URL'),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 18),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(
                      onPressed: () => Navigator.pop(dialogContext, false),
                      child: const Text('Cancel'),
                    ),
                    const SizedBox(width: 8),
                    FilledButton(
                      onPressed: () => Navigator.pop(dialogContext, true),
                      child: const Text('Save'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );

    if (save != true) {
      return;
    }

    try {
      final updated = await widget.service.updateProfile(
        JobSeekerProfile(
          fullName: name.text.trim(),
          email: p.email,
          headline: headline.text.trim(),
          location: location.text.trim(),
          bio: bio.text.trim(),
          linkedInUrl: linkedIn.text.trim(),
          gitHubUrl: gitHub.text.trim(),
          portfolioUrl: portfolio.text.trim(),
        ),
      );

      if (mounted) {
        setState(() => profile = updated);
      }
    } on ApiException catch (e) {
      _showMessage(e.message, isError: true);
    }
  }

  Future<void> _addSkill() async {
    final existing = skills.map((e) => e.skillId).toSet();
    final options = catalog.where((e) => !existing.contains(e.id)).toList();

    if (options.isEmpty) {
      _showMessage('No more skills available to add.');
      return;
    }

    String selected = options.first.id;
    double level = 3;

    final ok = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setLocal) => AlertDialog(
          title: const Text('Add skill'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<String>(
                initialValue: selected,
                items: options
                    .map(
                      (skill) => DropdownMenuItem(
                        value: skill.id,
                        child: Text(skill.name),
                      ),
                    )
                    .toList(),
                onChanged: (value) {
                  if (value != null) {
                    setLocal(() => selected = value);
                  }
                },
                decoration: const InputDecoration(labelText: 'Skill'),
              ),
              const SizedBox(height: 12),
              Text('Proficiency: ${level.round()}/5'),
              Slider(
                value: level,
                min: 1,
                max: 5,
                divisions: 4,
                onChanged: (value) => setLocal(() => level = value),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(dialogContext, true),
              child: const Text('Add'),
            ),
          ],
        ),
      ),
    );

    if (ok != true) {
      return;
    }

    try {
      await widget.service.addSkill(selected, level.round());
      await _load();
    } on ApiException catch (e) {
      _showMessage(e.message, isError: true);
    }
  }

  Future<void> _skillMenu(JobSeekerSkill skill) async {
    final action = await showModalBottomSheet<String>(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Wrap(
          children: [
            ListTile(
              title: Text(
                skill.name,
                style: const TextStyle(fontWeight: FontWeight.w800),
              ),
            ),
            ListTile(
              leading: const Icon(Icons.tune_rounded),
              title: const Text('Change proficiency'),
              onTap: () => Navigator.pop(sheetContext, 'edit'),
            ),
            ListTile(
              leading: const Icon(Icons.delete_outline_rounded),
              title: const Text('Remove skill'),
              onTap: () => Navigator.pop(sheetContext, 'delete'),
            ),
          ],
        ),
      ),
    );

    if (!mounted || action == null) {
      return;
    }

    try {
      if (action == 'delete') {
        await widget.service.deleteSkill(skill.skillId);
        await _load();
        return;
      }

      if (action == 'edit') {
        double level = skill.proficiencyLevel.toDouble();

        final ok = await showDialog<bool>(
          context: context,
          builder: (dialogContext) => StatefulBuilder(
            builder: (context, setLocal) => AlertDialog(
              title: Text(skill.name),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('Proficiency: ${level.round()}/5'),
                  Slider(
                    value: level,
                    min: 1,
                    max: 5,
                    divisions: 4,
                    onChanged: (value) => setLocal(() => level = value),
                  ),
                ],
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(dialogContext, false),
                  child: const Text('Cancel'),
                ),
                FilledButton(
                  onPressed: () => Navigator.pop(dialogContext, true),
                  child: const Text('Save'),
                ),
              ],
            ),
          ),
        );

        if (ok == true) {
          await widget.service.updateSkill(skill.skillId, level.round());
          await _load();
        }
      }
    } on ApiException catch (e) {
      _showMessage(e.message, isError: true);
    }
  }

  Future<void> _pickAndUploadCv() async {
    if (_cvBusy) {
      return;
    }

    if (cv != null) {
      final replace = await showDialog<bool>(
        context: context,
        builder: (dialogContext) => AlertDialog(
          title: const Text('Replace current CV?'),
          content: Text(
            'Your current CV "${cv!.fileName}" will be replaced after the new file uploads successfully.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(dialogContext, true),
              child: const Text('Choose new CV'),
            ),
          ],
        ),
      );

      if (replace != true || !mounted) {
        return;
      }
    }

    PlatformFile? picked;

    try {
      picked = await FilePicker.pickFile(
        type: FileType.custom,
        allowedExtensions: const ['pdf', 'doc', 'docx'],
      );
    } catch (_) {
      _showMessage(
        'Unable to open the file picker. Please try again.',
        isError: true,
      );
      return;
    }

    if (picked == null) {
      return;
    }

    final file = picked;
    final extension = (file.extension ?? _extensionOf(file.name)).toLowerCase();

    if (!const {'pdf', 'doc', 'docx'}.contains(extension)) {
      _showMessage(
        'Only PDF, DOC and DOCX files are allowed.',
        isError: true,
      );
      return;
    }

    int? fileSize = file.lengthSync();

    fileSize ??= await file.length();

    if (fileSize != null && fileSize <= 0) {
      _showMessage('The selected CV is empty.', isError: true);
      return;
    }

    if (fileSize != null && fileSize > _maxCvSizeBytes) {
      _showMessage(
        'CV must be 5 MB or smaller. Selected file is ${_formatBytes(fileSize)}.',
        isError: true,
      );
      return;
    }

    late final Uint8List bytes;

    try {
      bytes = await file.readAsBytes();
    } catch (_) {
      _showMessage(
        'The selected file could not be read. Please choose it again.',
        isError: true,
      );
      return;
    }

    final actualSize = bytes.length;

    if (actualSize <= 0) {
      _showMessage('The selected CV is empty.', isError: true);
      return;
    }

    if (actualSize > _maxCvSizeBytes) {
      _showMessage(
        'CV must be 5 MB or smaller. Selected file is ${_formatBytes(actualSize)}.',
        isError: true,
      );
      return;
    }

    final contentType = _contentTypeFor(extension);

    setState(() => cvUploading = true);

    try {
      final uploaded = await widget.service.uploadCv(
        fileName: file.name,
        bytes: bytes,
        contentType: contentType,
      );

      if (!mounted) {
        return;
      }

      setState(() => cv = uploaded);
      _showMessage(
        cv == null ? 'CV uploaded successfully.' : 'CV saved successfully.',
      );
    } on ApiException catch (e) {
      _showMessage(e.message, isError: true);
    } catch (_) {
      _showMessage(
        'Something went wrong while uploading the CV. Please try again.',
        isError: true,
      );
    } finally {
      if (mounted) {
        setState(() => cvUploading = false);
      }
    }
  }

  Future<void> _deleteCv() async {
    if (cv == null || _cvBusy) {
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Remove CV?'),
        content: Text(
          'Remove "${cv!.fileName}" from your profile? You can upload another CV later.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text('Cancel'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.pop(dialogContext, true),
            child: const Text('Remove'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) {
      return;
    }

    setState(() => cvDeleting = true);

    try {
      await widget.service.deleteCv();

      if (!mounted) {
        return;
      }

      setState(() => cv = null);
      _showMessage('CV removed.');
    } on ApiException catch (e) {
      _showMessage(e.message, isError: true);
    } finally {
      if (mounted) {
        setState(() => cvDeleting = false);
      }
    }
  }

  String _extensionOf(String fileName) {
    final index = fileName.lastIndexOf('.');

    if (index < 0 || index == fileName.length - 1) {
      return '';
    }

    return fileName.substring(index + 1).toLowerCase();
  }

  String _contentTypeFor(String extension) {
    switch (extension) {
      case 'pdf':
        return 'application/pdf';
      case 'doc':
        return 'application/msword';
      case 'docx':
        return 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
      default:
        return 'application/octet-stream';
    }
  }

  String _formatBytes(int bytes) {
    if (bytes >= 1024 * 1024) {
      return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
    }

    return '${(bytes / 1024).toStringAsFixed(0)} KB';
  }

  void _showMessage(String message, {bool isError = false}) {
    if (!mounted) {
      return;
    }

    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: isError ? Colors.red.shade700 : null,
        ),
      );
  }

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.error_outline_rounded, size: 42),
              const SizedBox(height: 12),
              Text(
                error!,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 12),
              FilledButton.icon(
                onPressed: _load,
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    final p = profile!;

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      CircleAvatar(
                        radius: 26,
                        child: Text(
                          p.fullName.isEmpty
                              ? 'J'
                              : p.fullName[0].toUpperCase(),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              p.fullName,
                              style: const TextStyle(
                                fontWeight: FontWeight.w900,
                                fontSize: 18,
                              ),
                            ),
                            Text(
                              p.email,
                              style: const TextStyle(color: AppTheme.muted),
                            ),
                            if (p.headline.isNotEmpty) Text(p.headline),
                          ],
                        ),
                      ),
                      IconButton(
                        onPressed: _editProfile,
                        icon: const Icon(Icons.edit_outlined),
                        tooltip: 'Edit profile',
                      ),
                    ],
                  ),
                  if (p.location.isNotEmpty) ...[
                    const SizedBox(height: 10),
                    Text('📍 ${p.location}'),
                  ],
                  if (p.bio.isNotEmpty) ...[
                    const SizedBox(height: 12),
                    Text(
                      p.bio,
                      style: const TextStyle(height: 1.4),
                    ),
                  ],
                ],
              ),
            ),
          ),
          const SizedBox(height: 12),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      const Expanded(
                        child: Text(
                          'Skills',
                          style: TextStyle(
                            fontWeight: FontWeight.w900,
                            fontSize: 17,
                          ),
                        ),
                      ),
                      IconButton(
                        onPressed: _addSkill,
                        icon: const Icon(Icons.add_circle_outline_rounded),
                        tooltip: 'Add skill',
                      ),
                    ],
                  ),
                  if (skills.isEmpty)
                    const Text(
                      'No skills added yet.',
                      style: TextStyle(color: AppTheme.muted),
                    )
                  else
                    ...skills.map(
                      (skill) => ListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(skill.name),
                        subtitle: Text(
                          'Proficiency ${skill.proficiencyLevel}/5',
                        ),
                        trailing: IconButton(
                          onPressed: () => _skillMenu(skill),
                          icon: const Icon(Icons.more_vert_rounded),
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 12),
          _buildCvCard(),
        ],
      ),
    );
  }

  Widget _buildCvCard() {
    final hasCv = cv != null;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    color: AppTheme.violet.withValues(alpha: 0.10),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Icon(
                    Icons.description_outlined,
                    color: AppTheme.violet,
                  ),
                ),
                const SizedBox(width: 12),
                const Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'CV / Resume',
                        style: TextStyle(
                          fontWeight: FontWeight.w900,
                          fontSize: 17,
                        ),
                      ),
                      SizedBox(height: 2),
                      Text(
                        'PDF, DOC or DOCX • Maximum 5 MB',
                        style: TextStyle(
                          color: AppTheme.muted,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
                Icon(
                  hasCv
                      ? Icons.check_circle_rounded
                      : Icons.warning_amber_rounded,
                  color: hasCv ? Colors.green : Colors.orange,
                ),
              ],
            ),
            const SizedBox(height: 16),
            if (hasCv)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  border: Border.all(color: Colors.black12),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.attach_file_rounded),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            cv!.fileName,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            _formatBytes(cv!.fileSizeBytes),
                            style: const TextStyle(
                              color: AppTheme.muted,
                              fontSize: 12,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              )
            else
              const Text(
                'No CV uploaded yet. Add your latest CV so recruiters and application workflows can use it.',
                style: TextStyle(
                  color: AppTheme.muted,
                  height: 1.4,
                ),
              ),
            const SizedBox(height: 16),
            if (cvUploading)
              const Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  LinearProgressIndicator(),
                  SizedBox(height: 8),
                  Text(
                    'Uploading CV...',
                    style: TextStyle(
                      color: AppTheme.muted,
                      fontSize: 12,
                    ),
                  ),
                ],
              )
            else if (cvDeleting)
              const Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  LinearProgressIndicator(),
                  SizedBox(height: 8),
                  Text(
                    'Removing CV...',
                    style: TextStyle(
                      color: AppTheme.muted,
                      fontSize: 12,
                    ),
                  ),
                ],
              )
            else
              Wrap(
                spacing: 10,
                runSpacing: 10,
                children: [
                  FilledButton.icon(
                    onPressed: _pickAndUploadCv,
                    icon: Icon(
                      hasCv
                          ? Icons.upload_file_rounded
                          : Icons.note_add_outlined,
                    ),
                    label: Text(hasCv ? 'Replace CV' : 'Upload CV'),
                  ),
                  if (hasCv)
                    OutlinedButton.icon(
                      onPressed: _deleteCv,
                      icon: const Icon(Icons.delete_outline_rounded),
                      label: const Text('Remove'),
                    ),
                ],
              ),
          ],
        ),
      ),
    );
  }
}
