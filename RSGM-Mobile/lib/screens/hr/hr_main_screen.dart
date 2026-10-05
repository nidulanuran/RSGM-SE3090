import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../services/hr/hr_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';
import '../home_page.dart';
import 'hr_analytics_screen.dart';
import 'hr_dashboard_screen.dart';
import 'hr_requisitions_screen.dart';
import 'hr_schedule_screen.dart';
import 'hr_workflows_screen.dart';

class HrMainScreen extends StatefulWidget {
  const HrMainScreen({
    super.key,
    this.initialIndex = 0,
    this.service,
  });

  final int initialIndex;
  final HrService? service;

  @override
  State<HrMainScreen> createState() => _HrMainScreenState();
}

class _HrMainScreenState extends State<HrMainScreen> {
  late int _index;
  late final HrService _service;

  @override
  void initState() {
    super.initState();
    _index = widget.initialIndex;
    _service = widget.service ?? HrService();
  }

  Future<void> _logout() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sign out'),
        content: const Text(
          'Are you sure you want to sign out of your HR Manager account?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Sign out'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

    const secure = FlutterSecureStorage();
    await secure.delete(key: 'rsgm_token');

    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('rsgm_session_token');
    await prefs.remove('rsgm_roles');
    await prefs.remove('rsgm_user');

    if (!mounted) return;
    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(builder: (_) => const HomePage()),
      (route) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        title: Row(
          children: [
            const RsgmBrand(compact: true),
            const SizedBox(width: 10),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: const Color(0xFFECFDF5),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFA7F3D0)),
              ),
              child: const Text(
                'HR Manager',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                  color: Color(0xFF047857),
                ),
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Sign out',
            onPressed: _logout,
            icon: const Icon(Icons.logout_rounded, color: AppTheme.muted),
          ),
        ],
      ),
      body: IndexedStack(
        index: _index,
        children: [
          HrDashboardScreen(service: _service),
          HrRequisitionsScreen(service: _service),
          HrWorkflowsScreen(service: _service),
          const HrScheduleScreen(),
          HrAnalyticsScreen(service: _service),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _index,
        onTap: (index) => setState(() => _index = index),
        type: BottomNavigationBarType.fixed,
        selectedItemColor: const Color(0xFF059669),
        unselectedItemColor: AppTheme.muted,
        selectedFontSize: 11,
        unselectedFontSize: 11,
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.dashboard_outlined),
            activeIcon: Icon(Icons.dashboard_rounded),
            label: 'Dashboard',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.fact_check_outlined),
            activeIcon: Icon(Icons.fact_check_rounded),
            label: 'Approvals',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.account_tree_outlined),
            activeIcon: Icon(Icons.account_tree_rounded),
            label: 'Workflows',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.calendar_month_outlined),
            activeIcon: Icon(Icons.calendar_month_rounded),
            label: 'Schedule',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.bar_chart_outlined),
            activeIcon: Icon(Icons.bar_chart_rounded),
            label: 'Analytics',
          ),
        ],
      ),
    );
  }
}
