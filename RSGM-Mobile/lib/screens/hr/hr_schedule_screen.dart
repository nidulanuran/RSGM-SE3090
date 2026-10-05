import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models/hr/hr_busy_time.dart';
import '../../services/api_client.dart';
import '../../services/hr/hr_availability_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';

class HrScheduleScreen extends StatefulWidget {
  const HrScheduleScreen({super.key, this.service});

  final HrAvailabilityService? service;

  @override
  State<HrScheduleScreen> createState() => _HrScheduleScreenState();
}

class _HrScheduleScreenState extends State<HrScheduleScreen> {
  late final HrAvailabilityService _service;
  List<HrBusyTime> _items = [];
  bool _loading = true;
  String? _error;
  String? _deletingId;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? HrAvailabilityService();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final items = await _service.getBusyTimes();
      items.sort((a, b) => a.startsAt.compareTo(b.startsAt));
      if (!mounted) return;
      setState(() {
        _items = items;
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
        _error = 'Unable to load your schedule. Please try again.';
        _loading = false;
      });
    }
  }

  Future<void> _add() async {
    final saved = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _AddHrBusyTimeSheet(service: _service),
    );
    if (saved == true && mounted) await _load();
  }

  Future<void> _delete(HrBusyTime item) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete busy time?'),
        content: Text('Remove "${item.title}" from your schedule?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _deletingId = item.id);
    try {
      await _service.deleteBusyTime(item.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Busy time deleted.')),
      );
      await _load();
    } on ApiException catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(e.message)));
    } finally {
      if (mounted) setState(() => _deletingId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    return GradientBackdrop(
      child: SafeArea(
        child: RefreshIndicator(
          onRefresh: _load,
          color: const Color(0xFF059669),
          child: SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const InfoPill(text: 'INTERVIEW PLANNING'),
                const SizedBox(height: 14),
                Text('My schedule',
                    style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 8),
                Text(
                  'Add meetings and unavailable periods on weekdays. Only future dates and times from 8:00 AM to 5:00 PM can be scheduled.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 20),
                SizedBox(
                  width: double.infinity,
                  height: 52,
                  child: FilledButton.icon(
                    style: FilledButton.styleFrom(
                      backgroundColor: const Color(0xFF059669),
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16),
                      ),
                    ),
                    onPressed: _add,
                    icon: const Icon(Icons.add_rounded),
                    label: const Text(
                      'Add busy time',
                      style: TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                ),
                const SizedBox(height: 24),
                _content(),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _content() {
    if (_loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 60),
        child: Center(
          child: CircularProgressIndicator(color: Color(0xFF059669)),
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
        ),
        child: Column(
          children: [
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: _load,
              icon: const Icon(Icons.refresh_rounded),
              label: const Text('Try again'),
            ),
          ],
        ),
      );
    }
    if (_items.isEmpty) {
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
            Icon(Icons.event_available_rounded,
                size: 42, color: Color(0xFF059669)),
            SizedBox(height: 12),
            Text(
              'No busy times added',
              style: TextStyle(fontSize: 17, fontWeight: FontWeight.w700),
            ),
            SizedBox(height: 6),
            Text(
              'Your remaining interview hours are treated as available automatically.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppTheme.muted),
            ),
          ],
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Upcoming busy times (${_items.length})',
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 12),
        ..._items.map((item) {
          final start = item.startsAt.toLocal();
          final end = item.endsAt.toLocal();
          return Container(
            margin: const EdgeInsets.only(bottom: 12),
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(18),
              border: Border.all(color: AppTheme.border),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.calendar_month_rounded,
                    color: Color(0xFF059669)),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item.title,
                          style: const TextStyle(
                              fontWeight: FontWeight.w700, fontSize: 15)),
                      const SizedBox(height: 6),
                      Text(DateFormat('EEE, d MMM yyyy').format(start),
                          style: const TextStyle(color: AppTheme.muted)),
                      const SizedBox(height: 3),
                      Text(
                        '${DateFormat('hh:mm a').format(start)} - ${DateFormat('hh:mm a').format(end)}',
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      if (item.description?.trim().isNotEmpty == true) ...[
                        const SizedBox(height: 8),
                        Text(item.description!,
                            style: const TextStyle(color: AppTheme.muted)),
                      ],
                    ],
                  ),
                ),
                IconButton(
                  onPressed: _deletingId == item.id ? null : () => _delete(item),
                  icon: _deletingId == item.id
                      ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.delete_outline_rounded,
                          color: Color(0xFFDC2626)),
                ),
              ],
            ),
          );
        }),
      ],
    );
  }
}

class _AddHrBusyTimeSheet extends StatefulWidget {
  const _AddHrBusyTimeSheet({required this.service});
  final HrAvailabilityService service;

  @override
  State<_AddHrBusyTimeSheet> createState() => _AddHrBusyTimeSheetState();
}

class _AddHrBusyTimeSheetState extends State<_AddHrBusyTimeSheet> {
  final _formKey = GlobalKey<FormState>();
  final _title = TextEditingController();
  final _description = TextEditingController();
  late DateTime _date;
  int _startMinutes = 8 * 60;
  int _endMinutes = 9 * 60;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _date = DateTime(now.year, now.month, now.day);
    while (_date.weekday == DateTime.saturday ||
        _date.weekday == DateTime.sunday) {
      _date = _date.add(const Duration(days: 1));
    }
  }

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    super.dispose();
  }

  bool _isWithinOfficeHours(int minutes) =>
      minutes >= 8 * 60 && minutes <= 17 * 60;

  Future<void> _pickTime({required bool start}) async {
    final currentMinutes = start ? _startMinutes : _endMinutes;
    final picked = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(
        hour: currentMinutes ~/ 60,
        minute: currentMinutes % 60,
      ),
      helpText: start ? 'Select start time' : 'Select end time',
    );

    if (picked == null) return;

    final minutes = picked.hour * 60 + picked.minute;
    if (!_isWithinOfficeHours(minutes) || (start && minutes >= 17 * 60)) {
      setState(() {
        _error = start
            ? 'Start time must be between 8:00 AM and 4:59 PM.'
            : 'End time must be between 8:00 AM and 5:00 PM.';
      });
      return;
    }

    setState(() {
      if (start) {
        _startMinutes = minutes;
        if (_endMinutes <= _startMinutes) {
          _endMinutes = (_startMinutes + 60).clamp(8 * 60, 17 * 60).toInt();
        }
      } else {
        _endMinutes = minutes;
      }
      _error = null;
    });
  }

  String _formatMinutes(int minutes) {
    final hour = minutes ~/ 60;
    final minute = minutes % 60;
    final suffix = hour >= 12 ? 'PM' : 'AM';
    final hour12 = hour % 12 == 0 ? 12 : hour % 12;
    return '$hour12:${minute.toString().padLeft(2, '0')} $suffix';
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final picked = await showDatePicker(
      context: context,
      initialDate: _date.isBefore(today) ? today : _date,
      firstDate: today,
      lastDate: today.add(const Duration(days: 365)),
      selectableDayPredicate: (date) =>
          date.weekday != DateTime.saturday && date.weekday != DateTime.sunday,
    );
    if (picked != null) {
      setState(() {
        _date = picked;
        _error = null;
      });
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (!_isWithinOfficeHours(_startMinutes) ||
        !_isWithinOfficeHours(_endMinutes)) {
      setState(() =>
          _error = 'Times must be between 8:00 AM and 5:00 PM.');
      return;
    }
    if (_endMinutes <= _startMinutes) {
      setState(() => _error = 'End time must be after the start time.');
      return;
    }

    final startsAt = DateTime(
      _date.year,
      _date.month,
      _date.day,
      _startMinutes ~/ 60,
      _startMinutes % 60,
    );
    final endsAt = DateTime(
      _date.year,
      _date.month,
      _date.day,
      _endMinutes ~/ 60,
      _endMinutes % 60,
    );

    if (!startsAt.isAfter(DateTime.now())) {
      setState(() => _error = 'Start date and time must be in the future.');
      return;
    }

    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      await widget.service.createBusyTime(
        title: _title.text,
        startsAt: startsAt,
        endsAt: endsAt,
        description: _description.text,
      );
      if (!mounted) return;
      Navigator.pop(context, true);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Busy time added successfully.')),
      );
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _saving = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Unable to save busy time. Please try again.';
        _saving = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final inset = MediaQuery.of(context).viewInsets.bottom;
    return Container(
      padding: EdgeInsets.fromLTRB(20, 18, 20, 24 + inset),
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
      ),
      child: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              const Center(child: SizedBox(width: 40, child: Divider(thickness: 4))),
              const SizedBox(height: 12),
              const Text(
                'Add busy time',
                style: TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 5),
              const Text(
                'Only future weekdays from 8:00 AM to 5:00 PM are available.',
                style: TextStyle(color: AppTheme.muted, fontSize: 13),
              ),
              const SizedBox(height: 18),
              TextFormField(
                controller: _title,
                maxLength: 150,
                decoration: const InputDecoration(labelText: 'Meeting or event name'),
                validator: (value) => value == null || value.trim().isEmpty
                    ? 'Meeting or event name is required.'
                    : null,
              ),
              const SizedBox(height: 12),
              InkWell(
                onTap: _pickDate,
                borderRadius: BorderRadius.circular(16),
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Date',
                    prefixIcon: Icon(Icons.calendar_today_rounded),
                  ),
                  child: Text(DateFormat('EEE, d MMM yyyy').format(_date)),
                ),
              ),
              const SizedBox(height: 14),
              Row(
                children: [
                  Expanded(
                    child: InkWell(
                      onTap: () => _pickTime(start: true),
                      borderRadius: BorderRadius.circular(16),
                      child: InputDecorator(
                        decoration: const InputDecoration(
                          labelText: 'Start time',
                          prefixIcon: Icon(Icons.access_time_rounded),
                        ),
                        child: Text(_formatMinutes(_startMinutes)),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: InkWell(
                      onTap: () => _pickTime(start: false),
                      borderRadius: BorderRadius.circular(16),
                      child: InputDecorator(
                        decoration: const InputDecoration(
                          labelText: 'End time',
                          prefixIcon: Icon(Icons.access_time_rounded),
                        ),
                        child: Text(_formatMinutes(_endMinutes)),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              TextFormField(
                controller: _description,
                maxLength: 500,
                maxLines: 2,
                decoration: const InputDecoration(labelText: 'Description (optional)'),
              ),
              if (_error != null) ...[
                const SizedBox(height: 8),
                Text(_error!, style: const TextStyle(color: Color(0xFFB91C1C))),
              ],
              const SizedBox(height: 18),
              SizedBox(
                width: double.infinity,
                height: 50,
                child: FilledButton(
                  onPressed: _saving ? null : _save,
                  style: FilledButton.styleFrom(
                    backgroundColor: const Color(0xFF059669),
                  ),
                  child: _saving
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : const Text('Add busy time'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
