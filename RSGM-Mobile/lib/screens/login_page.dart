import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../theme/app_theme.dart';
import '../widgets/rsgm_widgets.dart';
import 'recruiter/recruiter_main_screen.dart';
import 'jobseeker/jobseeker_main_screen.dart';
import 'panelist/panelist_main_screen.dart';
import 'hr/hr_main_screen.dart';
import 'register_page.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _auth = AuthService();
  bool _remember = false;
  bool _obscure = true;
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final data = await _auth.login(
        email: _email.text.trim(),
        password: _password.text,
        rememberMe: _remember,
      );
      if (!mounted) return;
      final rolesList = data['roles'] is List
          ? (data['roles'] as List).map((e) => e.toString()).toList()
          : <String>[];
      final rolesStr = rolesList.isNotEmpty ? rolesList.join(', ') : 'User';
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Signed in successfully as $rolesStr.')),
      );
      if (rolesList.contains('Recruiter')) {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (_) => const RecruiterMainScreen(),
          ),
        );
      } else if (rolesList.contains('JobSeeker')) {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (_) => const JobSeekerMainScreen(),
          ),
        );
      } else if (rolesList.contains('HiringPanelist')) {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (_) => const PanelistMainScreen(),
          ),
        );
      } else if (rolesList.contains('HRManager')) {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (_) => const HrMainScreen(),
          ),
        );
      }
    } on AuthException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: GradientBackdrop(
        child: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _AuthTop(onBack: () => Navigator.pop(context)),
                const SizedBox(height: 42),
                const InfoPill(text: 'WELCOME BACK'),
                const SizedBox(height: 16),
                Text('Sign in to Hireon', style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 10),
                Text('Access your account and continue managing your recruitment workflow.', style: Theme.of(context).textTheme.bodyMedium),
                const SizedBox(height: 26),
                _Card(
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const _Label('Email address'),
                        const SizedBox(height: 8),
                        TextFormField(
                          controller: _email,
                          keyboardType: TextInputType.emailAddress,
                          autofillHints: const [AutofillHints.email],
                          decoration: const InputDecoration(prefixIcon: Icon(Icons.mail_outline_rounded), hintText: 'you@example.com'),
                          validator: (v) {
                            final value = v?.trim() ?? '';
                            if (value.isEmpty) return 'Enter your email address.';
                            if (!value.contains('@')) return 'Enter a valid email address.';
                            return null;
                          },
                        ),
                        const SizedBox(height: 18),
                        const _Label('Password'),
                        const SizedBox(height: 8),
                        TextFormField(
                          controller: _password,
                          obscureText: _obscure,
                          autofillHints: const [AutofillHints.password],
                          decoration: InputDecoration(
                            prefixIcon: const Icon(Icons.lock_outline_rounded),
                            hintText: 'Enter your password',
                            suffixIcon: IconButton(
                              onPressed: () => setState(() => _obscure = !_obscure),
                              icon: Icon(_obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                            ),
                          ),
                          validator: (v) => (v == null || v.isEmpty) ? 'Enter your password.' : null,
                        ),
                        const SizedBox(height: 10),
                        Row(
                          children: [
                            Checkbox(
                              value: _remember,
                              activeColor: AppTheme.violet,
                              onChanged: (v) => setState(() => _remember = v ?? false),
                            ),
                            const Text('Remember me', style: TextStyle(fontSize: 13, color: AppTheme.muted)),
                          ],
                        ),
                        if (_error != null) ...[
                          const SizedBox(height: 8),
                          _MessageBox(message: _error!),
                        ],
                        const SizedBox(height: 16),
                        PrimaryButton(label: 'Sign in', loading: _loading, onPressed: _submit),
                        const SizedBox(height: 20),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            const Text('New to Hireon? ', style: TextStyle(color: AppTheme.muted, fontSize: 13)),
                            TextButton(
                              onPressed: () => Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => const RegisterPage())),
                              child: const Text('Create account', style: TextStyle(fontWeight: FontWeight.w700, color: AppTheme.violet)),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _AuthTop extends StatelessWidget {
  const _AuthTop({required this.onBack});
  final VoidCallback onBack;
  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        const RsgmBrand(compact: true),
        const Spacer(),
        IconButton.filledTonal(onPressed: onBack, icon: const Icon(Icons.arrow_back_rounded)),
      ],
    );
  }
}

class _Card extends StatelessWidget {
  const _Card({required this.child});
  final Widget child;
  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .86),
        borderRadius: BorderRadius.circular(28),
        border: Border.all(color: Colors.white),
        boxShadow: const [BoxShadow(color: Color(0x0D000000), blurRadius: 30, offset: Offset(0, 12))],
      ),
      child: child,
    );
  }
}

class _Label extends StatelessWidget {
  const _Label(this.text);
  final String text;
  @override
  Widget build(BuildContext context) => Text(text, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppTheme.ink));
}

class _MessageBox extends StatelessWidget {
  const _MessageBox({required this.message});
  final String message;
  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: const Color(0xFFFEF2F2), border: Border.all(color: const Color(0xFFFECACA)), borderRadius: BorderRadius.circular(14)),
      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        const Icon(Icons.error_outline_rounded, size: 18, color: Color(0xFFDC2626)),
        const SizedBox(width: 9),
        Expanded(child: Text(message, style: const TextStyle(color: Color(0xFFDC2626), fontSize: 13, height: 1.4))),
      ]),
    );
  }
}
