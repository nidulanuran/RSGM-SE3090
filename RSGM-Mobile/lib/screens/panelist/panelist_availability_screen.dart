import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/panelist/panelist_busy_time.dart';
import '../../services/api_client.dart';
import '../../services/panelist/panelist_availability_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';

/// Screen for viewing, adding, and deleting hiring panelist availability / busy times.
class PanelistAvailabilityScreen extends StatefulWidget {
  const PanelistAvailabilityScreen({super.key, this.service});

  final PanelistAvailabilityService? service;

  @override
  State<PanelistAvailabilityScreen> createState() =>
      _PanelistAvailabilityScreenState();
}

class _PanelistAvailabilityScreenState
    extends State<PanelistAvailabilityScreen> {
  late final PanelistAvailabilityService _service;

  List<PanelistBusyTime> _busyTimes = [];
  bool _isLoading = true;
  String? _error;
  String? _deletingId;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? PanelistAvailabilityService();
    _loadBusyTimes();
  }

  Future<void> _loadBusyTimes() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final items = await _service.getBusyTimes();
      if (!mounted) return;
      // Sort upcoming first
      items.sort((a, b) => a.startsAt.compareTo(b.startsAt));
      setState(() {
        _busyTimes = items;
        _isLoading = false;
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
            'Unable to load availability. Please check your connection and try again.';
        _isLoading = false;
      });
    }
  }

  Future<void> _confirmDelete(PanelistBusyTime busyTime) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete unavailable time?'),
        content: Text(
          'Are you sure you want to remove "${busyTime.title}" from your unavailable times?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFFDC2626),
            ),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

    setState(() => _deletingId = busyTime.id);

    try {
      await _service.deleteBusyTime(busyTime.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Unavailable time deleted.')),
      );
      await _loadBusyTimes();
    } on ApiException catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e.message)),
      );
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Failed to delete unavailable time. Please try again.'),
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _deletingId = null);
      }
    }
  }

  Future<void> _showAddModal() async {
    await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => _AddBusyTimeSheet(service: _service),
    );

    if (mounted) {
      _loadBusyTimes();
    }
  }

  @override
  Widget build(BuildContext context) {
    return GradientBackdrop(
      child: SafeArea(
        child: RefreshIndicator(
          onRefresh: _loadBusyTimes,
          color: Color(0xFFD97706),
          child: SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const InfoPill(text: 'HIRING PANELIST SCHEDULE'),
                const SizedBox(height: 14),
                Text(
                  'Availability',
                  style: Theme.of(context).textTheme.headlineMedium,
                ),
                const SizedBox(height: 8),
                Text(
                  'Manage the times when you are unavailable for interviews or panel activities.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 20),
                // Add Unavailable Time CTA Button
                SizedBox(
                  width: double.infinity,
                  height: 52,
                  child: FilledButton.icon(
                    style: FilledButton.styleFrom(
                      backgroundColor: AppTheme.ink,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16),
                      ),
                    ),
                    icon: const Icon(Icons.add_rounded, size: 20),
                    label: const Text(
                      'Add unavailable time',
                      style:
                          TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
                    ),
                    onPressed: _showAddModal,
                  ),
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

  Widget _buildContent() {
    if (_isLoading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 60),
        child: Center(
          child: CircularProgressIndicator(color: Color(0xFFD97706)),
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
          border: Border.all(color: const Color(0xFFFECACA)),
        ),
        child: Column(
          children: [
            const Icon(Icons.error_outline_rounded,
                size: 36, color: Color(0xFFDC2626)),
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
              onPressed: _loadBusyTimes,
            ),
          ],
        ),
      );
    }

    if (_busyTimes.isEmpty) {
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.all(28),
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: .9),
          borderRadius: BorderRadius.circular(24),
          border: Border.all(color: AppTheme.border),
          boxShadow: const [
            BoxShadow(
              color: Color(0x0A000000),
              blurRadius: 20,
              offset: Offset(0, 6),
            ),
          ],
        ),
        child: Column(
          children: [
            Container(
              width: 64,
              height: 64,
              decoration: BoxDecoration(
                color: const Color(0xFFFFFBEB),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: const Color(0xFFFDE68A)),
              ),
              child: const Icon(
                Icons.event_available_rounded,
                color: Color(0xFFD97706),
                size: 32,
              ),
            ),
            const SizedBox(height: 18),
            const Text(
              "You're currently available",
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w700,
                color: AppTheme.ink,
              ),
            ),
            const SizedBox(height: 8),
            const Text(
              'No unavailable periods have been added. You will be considered available for scheduled recruitment activities.',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 13,
                color: AppTheme.muted,
                height: 1.5,
              ),
            ),
          ],
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Text(
              'Upcoming Unavailable Slots (${_busyTimes.length})',
              style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w700,
                color: AppTheme.ink,
              ),
            ),
          ],
        ),
        const SizedBox(height: 14),
        ..._busyTimes.map((item) => _BusyTimeCard(
              busyTime: item,
              isDeleting: _deletingId == item.id,
              onDelete: () => _confirmDelete(item),
            )),
      ],
    );
  }
}

class _BusyTimeCard extends StatelessWidget {
  const _BusyTimeCard({
    required this.busyTime,
    required this.isDeleting,
    required this.onDelete,
  });

  final PanelistBusyTime busyTime;
  final bool isDeleting;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    final localStart = busyTime.startsAt.toLocal();
    final localEnd = busyTime.endsAt.toLocal();

    final dateStr = DateFormat('EEEE, d MMMM yyyy').format(localStart);
    final timeStr =
        '${DateFormat('hh:mm a').format(localStart)} - ${DateFormat('hh:mm a').format(localEnd)}';

    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .94),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppTheme.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x08000000),
            blurRadius: 16,
            offset: Offset(0, 4),
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
                width: 42,
                height: 42,
                decoration: BoxDecoration(
                  color: const Color(0xFFFFFBEB),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFFFDE68A)),
                ),
                child: const Icon(
                  Icons.event_busy_rounded,
                  color: Color(0xFFD97706),
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      busyTime.title,
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                        color: AppTheme.ink,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        const Icon(
                          Icons.calendar_today_outlined,
                          size: 13,
                          color: AppTheme.muted,
                        ),
                        const SizedBox(width: 5),
                        Expanded(
                          child: Text(
                            dateStr,
                            style: const TextStyle(
                              fontSize: 13,
                              color: AppTheme.muted,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        const Icon(
                          Icons.access_time_rounded,
                          size: 13,
                          color: Color(0xFFD97706),
                        ),
                        const SizedBox(width: 5),
                        Text(
                          timeStr,
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: AppTheme.ink,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              IconButton(
                tooltip: 'Delete unavailable time',
                icon: isDeleting
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Color(0xFFDC2626),
                        ),
                      )
                    : const Icon(
                        Icons.delete_outline_rounded,
                        color: Color(0xFFDC2626),
                        size: 20,
                      ),
                onPressed: isDeleting ? null : onDelete,
              ),
            ],
          ),
          if (busyTime.description != null &&
              busyTime.description!.trim().isNotEmpty) ...[
            const SizedBox(height: 12),
            const Divider(color: Color(0xFFF3F4F6), height: 1),
            const SizedBox(height: 10),
            Text(
              busyTime.description!.trim(),
              style: const TextStyle(
                fontSize: 13,
                height: 1.45,
                color: AppTheme.muted,
              ),
            ),
          ],
          if (busyTime.cancelledInterviews != null &&
              busyTime.cancelledInterviews! > 0) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: const Color(0xFFFFFBEB),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFFFDE68A)),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(
                    Icons.warning_amber_rounded,
                    size: 14,
                    color: Color(0xFFD97706),
                  ),
                  const SizedBox(width: 6),
                  Text(
                    '${busyTime.cancelledInterviews} conflicting interview(s) cancelled',
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: Color(0xFFB45309),
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
}

/// Bottom sheet dialog for adding a new unavailable period.
class _AddBusyTimeSheet extends StatefulWidget {
  const _AddBusyTimeSheet({required this.service});

  final PanelistAvailabilityService service;

  @override
  State<_AddBusyTimeSheet> createState() => _AddBusyTimeSheetState();
}

class _AddBusyTimeSheetState extends State<_AddBusyTimeSheet> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _descController = TextEditingController();

  late DateTime _selectedDate;
  TimeOfDay _startTime = const TimeOfDay(hour: 8, minute: 0);
  TimeOfDay _endTime = const TimeOfDay(hour: 11, minute: 0);

  bool _isSubmitting = false;
  String? _formError;

  @override
  void initState() {
    super.initState();
    // Default to today or next weekday if weekend
    final now = DateTime.now();
    _selectedDate = DateTime(now.year, now.month, now.day);
    if (_selectedDate.weekday == DateTime.saturday) {
      _selectedDate = _selectedDate.add(const Duration(days: 2));
    } else if (_selectedDate.weekday == DateTime.sunday) {
      _selectedDate = _selectedDate.add(const Duration(days: 1));
    }
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descController.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 365)),
      helpText: 'SELECT DATE (SINGLE DAY)',
    );

    if (picked != null) {
      setState(() {
        _selectedDate = DateTime(picked.year, picked.month, picked.day);
        _formError = null;
      });
    }
  }

  List<TimeOfDay> _allowedTimes({required bool forStart}) {
    final values = <TimeOfDay>[];
    final first = forStart ? 8 * 60 : 8 * 60 + 30;
    final last = forStart ? 16 * 60 + 30 : 17 * 60;
    for (var minutes = first; minutes <= last; minutes += 30) {
      values.add(TimeOfDay(hour: minutes ~/ 60, minute: minutes % 60));
    }
    return values;
  }

  Future<TimeOfDay?> _showAllowedTimePicker({
    required String title,
    required bool forStart,
    required TimeOfDay selected,
  }) async {
    return showModalBottomSheet<TimeOfDay>(
      context: context,
      showDragHandle: true,
      builder: (ctx) => SafeArea(
        child: SizedBox(
          height: 430,
          child: Column(
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 6, 20, 12),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text(
                    title,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w700,
                      color: AppTheme.ink,
                    ),
                  ),
                ),
              ),
              const Padding(
                padding: EdgeInsets.symmetric(horizontal: 20),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text(
                    'Choose a time between 8:00 AM and 5:00 PM.',
                    style: TextStyle(fontSize: 13, color: AppTheme.muted),
                  ),
                ),
              ),
              const SizedBox(height: 8),
              Expanded(
                child: ListView(
                  children: _allowedTimes(forStart: forStart).map((time) {
                    final isSelected =
                        time.hour == selected.hour && time.minute == selected.minute;
                    return ListTile(
                      leading: Icon(
                        isSelected
                            ? Icons.radio_button_checked_rounded
                            : Icons.radio_button_off_rounded,
                        color: isSelected ? const Color(0xFFD97706) : AppTheme.muted,
                      ),
                      title: Text(
                        time.format(ctx),
                        style: TextStyle(
                          fontWeight:
                              isSelected ? FontWeight.w700 : FontWeight.w500,
                        ),
                      ),
                      onTap: () => Navigator.pop(ctx, time),
                    );
                  }).toList(),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _pickStartTime() async {
    final picked = await _showAllowedTimePicker(
      title: 'Select start time',
      forStart: true,
      selected: _startTime,
    );

    if (picked != null) {
      setState(() {
        _startTime = picked;
        _formError = null;
      });
    }
  }

  Future<void> _pickEndTime() async {
    final picked = await _showAllowedTimePicker(
      title: 'Select end time',
      forStart: false,
      selected: _endTime,
    );

    if (picked != null) {
      setState(() {
        _endTime = picked;
        _formError = null;
      });
    }
  }

  Future<void> _handleSubmit() async {
    if (!_formKey.currentState!.validate()) return;

    final startMinutes = _startTime.hour * 60 + _startTime.minute;
    final endMinutes = _endTime.hour * 60 + _endTime.minute;

    if (endMinutes <= startMinutes) {
      setState(() {
        _formError = 'End time must be after the start time.';
      });
      return;
    }

    final startsAt = DateTime(
      _selectedDate.year,
      _selectedDate.month,
      _selectedDate.day,
      _startTime.hour,
      _startTime.minute,
    );

    final endsAt = DateTime(
      _selectedDate.year,
      _selectedDate.month,
      _selectedDate.day,
      _endTime.hour,
      _endTime.minute,
    );

    if (startsAt.isBefore(DateTime.now())) {
      setState(() {
        _formError = 'Unavailable times must be scheduled in the future.';
      });
      return;
    }

    setState(() {
      _isSubmitting = true;
      _formError = null;
    });

    try {
      await widget.service.createBusyTime(
        title: _titleController.text.trim(),
        startsAt: startsAt,
        endsAt: endsAt,
        description: _descController.text.trim().isNotEmpty
            ? _descController.text.trim()
            : null,
      );

      if (!mounted) return;
      Navigator.pop(context, true);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Unavailable time added successfully.')),
      );
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _formError = e.message;
        _isSubmitting = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _formError =
            'Failed to save unavailable period. Please check the values and try again.';
        _isSubmitting = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).viewInsets.bottom;
    final dateDisplay = DateFormat('EEE, d MMM yyyy').format(_selectedDate);

    return Container(
      padding: EdgeInsets.fromLTRB(20, 20, 20, 24 + bottomInset),
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
      ),
      child: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: AppTheme.border,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              const SizedBox(height: 18),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Add Unavailable Time',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w700,
                      color: AppTheme.ink,
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close_rounded,
                        color: AppTheme.muted, size: 22),
                    onPressed: () => Navigator.pop(context),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              const Text(
                'Define a future weekday period when you are unavailable for interviews.',
                style: TextStyle(fontSize: 13, color: AppTheme.muted),
              ),
              const SizedBox(height: 18),
              const Text(
                'Title *',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.ink,
                ),
              ),
              const SizedBox(height: 6),
              TextFormField(
                controller: _titleController,
                decoration: const InputDecoration(
                  hintText: 'e.g. Doctor Appointment, Sprint Planning',
                  prefixIcon: Icon(Icons.title_rounded, size: 20),
                ),
                validator: (v) {
                  if (v == null || v.trim().isEmpty) {
                    return 'Title is required.';
                  }
                  if (v.trim().length > 150) {
                    return 'Title cannot exceed 150 characters.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              const Text(
                'Date (same day for start & end) *',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.ink,
                ),
              ),
              const SizedBox(height: 6),
              InkWell(
                onTap: _pickDate,
                borderRadius: BorderRadius.circular(16),
                child: Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    border: Border.all(color: AppTheme.border),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.calendar_today_rounded,
                          size: 18, color: Color(0xFFD97706)),
                      const SizedBox(width: 10),
                      Text(
                        dateDisplay,
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w600,
                          color: AppTheme.ink,
                        ),
                      ),
                      const Spacer(),
                      const Icon(Icons.arrow_drop_down_rounded,
                          color: AppTheme.muted),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Start Time *',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                            color: AppTheme.ink,
                          ),
                        ),
                        const SizedBox(height: 6),
                        InkWell(
                          onTap: _pickStartTime,
                          borderRadius: BorderRadius.circular(16),
                          child: Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 14, vertical: 14),
                            decoration: BoxDecoration(
                              color: Colors.white,
                              border: Border.all(color: AppTheme.border),
                              borderRadius: BorderRadius.circular(16),
                            ),
                            child: Row(
                              children: [
                                const Icon(Icons.access_time_rounded,
                                    size: 16, color: Color(0xFFD97706)),
                                const SizedBox(width: 8),
                                Text(
                                  _startTime.format(context),
                                  style: const TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w600,
                                    color: AppTheme.ink,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'End Time *',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                            color: AppTheme.ink,
                          ),
                        ),
                        const SizedBox(height: 6),
                        InkWell(
                          onTap: _pickEndTime,
                          borderRadius: BorderRadius.circular(16),
                          child: Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 14, vertical: 14),
                            decoration: BoxDecoration(
                              color: Colors.white,
                              border: Border.all(color: AppTheme.border),
                              borderRadius: BorderRadius.circular(16),
                            ),
                            child: Row(
                              children: [
                                const Icon(Icons.access_time_rounded,
                                    size: 16, color: Color(0xFFD97706)),
                                const SizedBox(width: 8),
                                Text(
                                  _endTime.format(context),
                                  style: const TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w600,
                                    color: AppTheme.ink,
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
              ),
              const SizedBox(height: 16),
              const Text(
                'Description (optional)',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.ink,
                ),
              ),
              const SizedBox(height: 6),
              TextFormField(
                controller: _descController,
                maxLines: 2,
                decoration: const InputDecoration(
                  hintText: 'e.g. Out of office for dental checkup',
                  prefixIcon: Padding(
                    padding: EdgeInsets.only(bottom: 24),
                    child: Icon(Icons.notes_rounded, size: 20),
                  ),
                ),
                validator: (v) {
                  if (v != null && v.trim().length > 500) {
                    return 'Description cannot exceed 500 characters.';
                  }
                  return null;
                },
              ),
              if (_formError != null) ...[
                const SizedBox(height: 14),
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFEF2F2),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: const Color(0xFFFECACA)),
                  ),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(Icons.error_outline_rounded,
                          size: 18, color: Color(0xFFDC2626)),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          _formError!,
                          style: const TextStyle(
                            fontSize: 12,
                            color: Color(0xFF991B1B),
                            height: 1.4,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
              const SizedBox(height: 22),
              SizedBox(
                width: double.infinity,
                height: 52,
                child: FilledButton(
                  style: FilledButton.styleFrom(
                    backgroundColor: AppTheme.ink,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(16),
                    ),
                  ),
                  onPressed: _isSubmitting ? null : _handleSubmit,
                  child: _isSubmitting
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : const Text(
                          'Save unavailable period',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}



