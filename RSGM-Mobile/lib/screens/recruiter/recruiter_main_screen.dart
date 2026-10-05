import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../theme/app_theme.dart';
import '../../services/recruiter/recruiter_application_service.dart';
import '../../services/recruiter/recruiter_availability_service.dart';
import '../../services/recruiter/recruiter_job_post_service.dart';
import '../../services/recruiter/recruiter_requisition_service.dart';
import '../../widgets/rsgm_widgets.dart';
import '../home_page.dart';
import 'recruiter_applicants_screen.dart';
import 'recruiter_availability_screen.dart';
import 'recruiter_job_posts_screen.dart';
import 'recruiter_requisitions_screen.dart';

/// Main navigation shell for the authenticated Recruiter role.
///
/// Provides a bottom navigation bar with four primary destinations:
/// 1. Availability
/// 2. Job Posts
/// 3. Applicants
/// 4. Requisitions
class RecruiterMainScreen extends StatefulWidget {
  const RecruiterMainScreen({
    super.key,
    this.initialIndex = 0,
    this.availabilityService,
    this.jobPostService,
    this.applicationService,
    this.requisitionService,
  });

  final int initialIndex;
  final RecruiterAvailabilityService? availabilityService;
  final RecruiterJobPostService? jobPostService;
  final RecruiterApplicationService? applicationService;
  final RecruiterRequisitionService? requisitionService;

  @override
  State<RecruiterMainScreen> createState() => _RecruiterMainScreenState();
}

class _RecruiterMainScreenState extends State<RecruiterMainScreen> {
  late int _currentIndex;

  @override
  void initState() {
    super.initState();
    _currentIndex = widget.initialIndex;
  }

  Future<void> _handleLogout() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sign out'),
        content: const Text(
          'Are you sure you want to sign out of your Recruiter account?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppTheme.ink),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Sign out'),
          ),
        ],
      ),
    );

    if (confirmed == true && mounted) {
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
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        scrolledUnderElevation: 1,
        title: Row(
          children: [
            const RsgmBrand(compact: true),
            const SizedBox(width: 10),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: const Color(0xFFF5F3FF),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFDDD6FE)),
              ),
              child: const Text(
                'Recruiter',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.violet,
                ),
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Sign out',
            icon: const Icon(Icons.logout_rounded, color: AppTheme.muted),
            onPressed: _handleLogout,
          ),
        ],
      ),
      body: IndexedStack(
        index: _currentIndex,
        children: [
          RecruiterAvailabilityScreen(service: widget.availabilityService),
          RecruiterJobPostsScreen(service: widget.jobPostService),
          RecruiterApplicantsScreen(service: widget.applicationService),
          RecruiterRequisitionsScreen(service: widget.requisitionService),
        ],
      ),
      bottomNavigationBar: Container(
        decoration: const BoxDecoration(
          border: Border(top: BorderSide(color: AppTheme.border, width: 1)),
        ),
        child: BottomNavigationBar(
          currentIndex: _currentIndex,
          onTap: (index) => setState(() => _currentIndex = index),
          type: BottomNavigationBarType.fixed,
          backgroundColor: Colors.white,
          selectedItemColor: AppTheme.violet,
          unselectedItemColor: AppTheme.muted,
          selectedFontSize: 12,
          unselectedFontSize: 12,
          selectedLabelStyle: const TextStyle(fontWeight: FontWeight.w700),
          unselectedLabelStyle: const TextStyle(fontWeight: FontWeight.w500),
          items: const [
            BottomNavigationBarItem(
              icon: Icon(Icons.calendar_month_outlined),
              activeIcon: Icon(Icons.calendar_month_rounded),
              label: 'Availability',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.work_outline_rounded),
              activeIcon: Icon(Icons.work_rounded),
              label: 'Job Posts',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.people_outline_rounded),
              activeIcon: Icon(Icons.people_rounded),
              label: 'Applicants',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.assignment_outlined),
              activeIcon: Icon(Icons.assignment_rounded),
              label: 'Requisitions',
            ),
          ],
        ),
      ),
    );
  }
}
