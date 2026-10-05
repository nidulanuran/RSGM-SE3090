import 'package:flutter/material.dart';
import '../theme/app_theme.dart';
import '../widgets/rsgm_widgets.dart';
import 'login_page.dart';
import 'register_page.dart';

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: GradientBackdrop(
        child: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 34),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _TopBar(
                  onLogin: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const LoginPage())),
                ),
                const SizedBox(height: 54),
                const InfoPill(text: 'Recruitment & Skill-Gap Matching'),
                const SizedBox(height: 22),
                Text('Hire smarter.\nMatch better.', style: Theme.of(context).textTheme.headlineLarge),
                const SizedBox(height: 18),
                Text(
                  'A connected recruitment platform for jobs, applications, interviews, offers, and intelligent skill matching.',
                  style: Theme.of(context).textTheme.bodyLarge,
                ),
                const SizedBox(height: 28),
                PrimaryButton(
                  label: 'Get Started',
                  icon: Icons.north_east_rounded,
                  onPressed: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const RegisterPage())),
                ),
                const SizedBox(height: 12),
                SizedBox(
                  width: double.infinity,
                  height: 54,
                  child: OutlinedButton(
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppTheme.ink,
                      backgroundColor: Colors.white.withValues(alpha: .55),
                      side: const BorderSide(color: AppTheme.border),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(27)),
                    ),
                    onPressed: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const LoginPage())),
                    child: const Text('I already have an account', style: TextStyle(fontWeight: FontWeight.w600)),
                  ),
                ),
                const SizedBox(height: 42),
                const Text('Built for the full hiring journey', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppTheme.muted)),
                const SizedBox(height: 14),
                const _FeatureCard(
                  icon: Icons.psychology_alt_outlined,
                  title: 'Smart Skill Matching',
                  body: 'Compare candidate skills with job requirements and identify relevant gaps.',
                ),
                const SizedBox(height: 12),
                const _FeatureCard(
                  icon: Icons.manage_search_rounded,
                  title: 'AI-Assisted Shortlisting',
                  body: 'Give recruiters useful candidate insights during shortlisting.',
                ),
                const SizedBox(height: 12),
                const _FeatureCard(
                  icon: Icons.work_outline_rounded,
                  title: 'Recruitment Workflow',
                  body: 'Keep postings, applications, interviews, and offers in one workflow.',
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _TopBar extends StatelessWidget {
  const _TopBar({required this.onLogin});
  final VoidCallback onLogin;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(14, 10, 10, 10),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .6),
        borderRadius: BorderRadius.circular(32),
        border: Border.all(color: Colors.white.withValues(alpha: .9)),
        boxShadow: const [BoxShadow(color: Color(0x0C000000), blurRadius: 20, offset: Offset(0, 8))],
      ),
      child: Row(
        children: [
          const RsgmBrand(compact: true),
          const Spacer(),
          TextButton(onPressed: onLogin, child: const Text('Log in', style: TextStyle(color: AppTheme.ink, fontWeight: FontWeight.w600))),
        ],
      ),
    );
  }
}

class _FeatureCard extends StatelessWidget {
  const _FeatureCard({required this.icon, required this.title, required this.body});
  final IconData icon;
  final String title;
  final String body;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .82),
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppTheme.border),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(color: const Color(0xFFF5F3FF), borderRadius: BorderRadius.circular(14)),
            child: Icon(icon, color: AppTheme.violet),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: AppTheme.ink)),
                const SizedBox(height: 6),
                Text(body, style: const TextStyle(fontSize: 13, height: 1.45, color: AppTheme.muted)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
