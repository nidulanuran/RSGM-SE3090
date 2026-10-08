import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

class RsgmBrand extends StatelessWidget {
  const RsgmBrand({super.key, this.compact = false});
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: compact ? 36 : 40,
          height: compact ? 36 : 40,
          decoration: const BoxDecoration(
            shape: BoxShape.circle,
            color: AppTheme.ink,
          ),
          alignment: Alignment.center,
          child: Text(
            'H',
            style: TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.w800,
              fontSize: compact ? 15 : 17,
            ),
          ),
        ),
        const SizedBox(width: 10),
        Text(
          'Hireon',
          style: TextStyle(
            color: AppTheme.ink,
            fontSize: compact ? 18 : 20,
            fontWeight: FontWeight.w700,
            letterSpacing: -.3,
          ),
        ),
      ],
    );
  }
}

class GradientBackdrop extends StatelessWidget {
  const GradientBackdrop({super.key, required this.child});
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        const Positioned.fill(child: ColoredBox(color: AppTheme.background)),
        Positioned(
          top: -120,
          left: 60,
          child: _Glow(size: 330, color: Color(0x337C3AED)),
        ),
        Positioned(
          top: 290,
          right: -130,
          child: _Glow(size: 320, color: Color(0x333B82F6)),
        ),
        Positioned(
          bottom: -130,
          left: 50,
          child: _Glow(size: 290, color: Color(0x3322D3EE)),
        ),
        Positioned.fill(child: CustomPaint(painter: _GridPainter())),
        Positioned.fill(child: child),
      ],
    );
  }
}

class _Glow extends StatelessWidget {
  const _Glow({required this.size, required this.color});
  final double size;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        gradient: RadialGradient(
          colors: [color, color.withValues(alpha: 0)],
        ),
      ),
    );
  }
}

class _GridPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = const Color(0x0A000000)
      ..strokeWidth = 1;
    const gap = 54.0;
    for (double x = 0; x < size.width; x += gap) {
      canvas.drawLine(Offset(x, 0), Offset(x, size.height), paint);
    }
    for (double y = 0; y < size.height; y += gap) {
      canvas.drawLine(Offset(0, y), Offset(size.width, y), paint);
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class PrimaryButton extends StatelessWidget {
  const PrimaryButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.loading = false,
    this.icon = Icons.arrow_forward_rounded,
  });

  final String label;
  final VoidCallback? onPressed;
  final bool loading;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: 56,
      child: FilledButton(
        style: FilledButton.styleFrom(
          backgroundColor: AppTheme.ink,
          foregroundColor: Colors.white,
          disabledBackgroundColor: AppTheme.ink.withValues(alpha: .7),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(28)),
        ),
        onPressed: loading ? null : onPressed,
        child: loading
            ? const SizedBox(
                width: 22,
                height: 22,
                child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
              )
            : Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(label, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15)),
                  const SizedBox(width: 8),
                  Icon(icon, size: 18),
                ],
              ),
      ),
    );
  }
}

class InfoPill extends StatelessWidget {
  const InfoPill({super.key, required this.text});
  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .78),
        border: Border.all(color: AppTheme.border),
        borderRadius: BorderRadius.circular(24),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.auto_awesome_rounded, size: 15, color: AppTheme.violet),
          const SizedBox(width: 7),
          Text(text, style: const TextStyle(fontSize: 12, color: AppTheme.muted)),
        ],
      ),
    );
  }
}
