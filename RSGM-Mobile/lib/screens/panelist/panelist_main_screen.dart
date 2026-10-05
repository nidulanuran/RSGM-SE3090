import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../services/panelist/panelist_availability_service.dart';
import '../../services/panelist/panelist_interview_service.dart';
import '../../services/panelist/panelist_shortlist_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';
import '../home_page.dart';
import 'panelist_availability_screen.dart';
import 'panelist_home_screen.dart';
import 'panelist_interviews_screen.dart';
import 'panelist_shortlists_screen.dart';

/// Main navigation shell for the authenticated Hiring Panelist role.
///
/// Mobile responsibilities:
/// 1. Home dashboard
/// 2. Availability management
/// 3. Read-only shortlists
/// 4. Read-only interviews
class PanelistMainScreen extends StatefulWidget {
  const PanelistMainScreen({
    super.key,
    this.initialIndex = 0,
    this.availabilityService,
    this.interviewService,
    this.shortlistService,
  });

  final int initialIndex;
  final PanelistAvailabilityService? availabilityService;
  final PanelistInterviewService? interviewService;
  final PanelistShortlistService? shortlistService;

  @override
  State<PanelistMainScreen> createState() =>
      _PanelistMainScreenState();
}

class _PanelistMainScreenState
    extends State<PanelistMainScreen> {
  static const Color amber = Color(0xFFD97706);
  static const Color amberSoft = Color(0xFFFFFBEB);
  static const Color amberBorder = Color(0xFFFDE68A);

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
          'Are you sure you want to sign out of your Hiring Panelist account?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(
              backgroundColor: AppTheme.ink,
            ),
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
        MaterialPageRoute(
          builder: (_) => const HomePage(),
        ),
        (route) => false,
      );
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

        title: Row(
          children: [
            const RsgmBrand(compact: true),
            const SizedBox(width: 10),

            Container(
              padding: const EdgeInsets.symmetric(
                horizontal: 8,
                vertical: 4,
              ),
              decoration: BoxDecoration(
                color: amberSoft,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(
                  color: amberBorder,
                ),
              ),
              child: const Text(
                'Hiring Panelist',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                  color: amber,
                ),
              ),
            ),
          ],
        ),

        actions: [
          IconButton(
            tooltip: 'Sign out',
            icon: const Icon(
              Icons.logout_rounded,
              color: AppTheme.muted,
            ),
            onPressed: _handleLogout,
          ),
        ],
      ),

      body: IndexedStack(
        index: _currentIndex,
        children: [
          PanelistHomeScreen(
            service: widget.interviewService,
          ),

          PanelistAvailabilityScreen(
            service: widget.availabilityService,
          ),

          PanelistShortlistsScreen(
            service: widget.shortlistService,
          ),

          PanelistInterviewsScreen(
            service: widget.interviewService,
          ),
        ],
      ),

      bottomNavigationBar: Container(
        decoration: const BoxDecoration(
          border: Border(
            top: BorderSide(
              color: AppTheme.border,
              width: 1,
            ),
          ),
        ),
        child: BottomNavigationBar(
          currentIndex: _currentIndex,

          onTap: (index) {
            setState(() {
              _currentIndex = index;
            });
          },

          type: BottomNavigationBarType.fixed,
          backgroundColor: Colors.white,

          selectedItemColor: amber,
          unselectedItemColor: AppTheme.muted,

          selectedFontSize: 12,
          unselectedFontSize: 12,

          selectedLabelStyle: const TextStyle(
            fontWeight: FontWeight.w700,
          ),

          unselectedLabelStyle: const TextStyle(
            fontWeight: FontWeight.w500,
          ),

          items: const [
            BottomNavigationBarItem(
              icon: Icon(
                Icons.home_outlined,
              ),
              activeIcon: Icon(
                Icons.home_rounded,
              ),
              label: 'Home',
            ),

            BottomNavigationBarItem(
              icon: Icon(
                Icons.calendar_month_outlined,
              ),
              activeIcon: Icon(
                Icons.calendar_month_rounded,
              ),
              label: 'Availability',
            ),

            BottomNavigationBarItem(
              icon: Icon(
                Icons.people_outline_rounded,
              ),
              activeIcon: Icon(
                Icons.people_rounded,
              ),
              label: 'Shortlists',
            ),

            BottomNavigationBarItem(
              icon: Icon(
                Icons.event_note_outlined,
              ),
              activeIcon: Icon(
                Icons.event_note_rounded,
              ),
              label: 'Interviews',
            ),
          ],
        ),
      ),
    );
  }
}