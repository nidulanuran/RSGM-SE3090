import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../theme/app_theme.dart';
import '../widgets/rsgm_widgets.dart';
import 'login_page.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});
  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  final _auth = AuthService();
  bool _obscure = true;
  bool _obscureConfirm = true;
  bool _terms = false;
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _name.dispose(); _email.dispose(); _password.dispose(); _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (!_terms) {
      setState(() => _error = 'Please agree to the Terms & Conditions and Privacy Policy.');
      return;
    }
    setState(() { _loading = true; _error = null; });
    try {
      await _auth.register(
        fullName: _name.text.trim(),
        email: _email.text.trim(),
        password: _password.text,
      );
      if (!mounted) return;
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (_) => const LoginPage()),
      );
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Account created successfully. Please sign in.')),
      );
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
                Row(children: [
                  const RsgmBrand(compact: true),
                  const Spacer(),
                  IconButton.filledTonal(onPressed: () => Navigator.pop(context), icon: const Icon(Icons.arrow_back_rounded)),
                ]),
                const SizedBox(height: 34),
                const InfoPill(text: 'CREATE ACCOUNT'),
                const SizedBox(height: 16),
                Text('Join RSGM', style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 10),
                Text('Create your account and start using the recruitment and skill-gap matching platform.', style: Theme.of(context).textTheme.bodyMedium),
                const SizedBox(height: 24),
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: .86),
                    borderRadius: BorderRadius.circular(28),
                    border: Border.all(color: Colors.white),
                    boxShadow: const [BoxShadow(color: Color(0x0D000000), blurRadius: 30, offset: Offset(0, 12))],
                  ),
                  child: Form(
                    key: _formKey,
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      const _Label('Full name'),
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _name,
                        textCapitalization: TextCapitalization.words,
                        autofillHints: const [AutofillHints.name],
                        decoration: const InputDecoration(prefixIcon: Icon(Icons.person_outline_rounded), hintText: 'John Smith'),
                        validator: (v) => (v == null || v.trim().isEmpty) ? 'Enter your full name.' : null,
                      ),
                      const SizedBox(height: 18),
                      const _Label('Email address'),
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _email,
                        keyboardType: TextInputType.emailAddress,
                        autofillHints: const [AutofillHints.email],
                        decoration: const InputDecoration(prefixIcon: Icon(Icons.mail_outline_rounded), hintText: 'name@example.com'),
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
                        autofillHints: const [AutofillHints.newPassword],
                        decoration: InputDecoration(
                          prefixIcon: const Icon(Icons.lock_outline_rounded),
                          hintText: 'Create a password',
                          suffixIcon: IconButton(onPressed: () => setState(() => _obscure = !_obscure), icon: Icon(_obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined)),
                        ),
                        validator: (v) {
                          if (v == null || v.isEmpty) return 'Enter a password.';
                          if (v.length < 8) return 'Password must be at least 8 characters.';
                          return null;
                        },
                      ),
                      const SizedBox(height: 8),
                      const Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Icon(Icons.check_circle_outline_rounded, size: 16, color: AppTheme.muted),
                        SizedBox(width: 7),
                        Expanded(child: Text('Use at least 8 characters with uppercase, lowercase and a number.', style: TextStyle(fontSize: 11, color: AppTheme.muted, height: 1.4))),
                      ]),
                      const SizedBox(height: 18),
                      const _Label('Confirm password'),
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _confirm,
                        obscureText: _obscureConfirm,
                        decoration: InputDecoration(
                          prefixIcon: const Icon(Icons.lock_outline_rounded),
                          hintText: 'Enter your password again',
                          suffixIcon: IconButton(onPressed: () => setState(() => _obscureConfirm = !_obscureConfirm), icon: Icon(_obscureConfirm ? Icons.visibility_outlined : Icons.visibility_off_outlined)),
                        ),
                        validator: (v) => v != _password.text ? 'Passwords do not match.' : null,
                      ),
                      const SizedBox(height: 10),
                      Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Checkbox(value: _terms, activeColor: AppTheme.violet, onChanged: (v) => setState(() { _terms = v ?? false; _error = null; })),
                        const Expanded(child: Padding(
                          padding: EdgeInsets.only(top: 11),
                          child: Text('I agree to the Terms & Conditions and Privacy Policy.', style: TextStyle(fontSize: 12, color: AppTheme.muted, height: 1.45)),
                        )),
                      ]),
                      if (_error != null) ...[
                        const SizedBox(height: 6),
                        _MessageBox(message: _error!),
                      ],
                      const SizedBox(height: 16),
                      PrimaryButton(label: 'Create account', loading: _loading, onPressed: _submit),
                      const SizedBox(height: 18),
                      Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                        const Text('Already have an account? ', style: TextStyle(color: AppTheme.muted, fontSize: 13)),
                        TextButton(
                          onPressed: () => Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => const LoginPage())),
                          child: const Text('Sign in', style: TextStyle(fontWeight: FontWeight.w700, color: AppTheme.violet)),
                        ),
                      ]),
                    ]),
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
