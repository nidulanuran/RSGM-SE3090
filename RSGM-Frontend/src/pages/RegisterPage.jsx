import { useState } from "react";
import {
  Link,
  useNavigate,
} from "react-router-dom";

import {
  AlertCircle,
  ArrowRight,
  CheckCircle2,
  Mail,
  User,
} from "lucide-react";

import AuthShell from "../components/auth/AuthShell";
import FormField from "../components/auth/FormField";
import PasswordField from "../components/auth/PasswordField";

import {
  registerUser,
} from "../services/authService";

function RegisterPage() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    fullName: "",
    email: "",
    password: "",
    confirmPassword: "",
    agreeToTerms: false,
  });

  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  // =========================================================
  // HANDLE INPUT CHANGES
  // =========================================================

  const handleChange = (event) => {
    const {
      name,
      value,
      type,
      checked,
    } = event.target;

    setForm((previous) => ({
      ...previous,
      [name]:
        type === "checkbox"
          ? checked
          : value,
    }));
  };

  // =========================================================
  // REGISTER
  // =========================================================

  const handleSubmit = async (event) => {
    event.preventDefault();

    setError("");

    // =======================================================
    // FRONTEND VALIDATION
    // =======================================================

    if (!form.fullName.trim()) {
      setError(
        "Please enter your full name."
      );
      return;
    }

    if (!form.email.trim()) {
      setError(
        "Please enter your email address."
      );
      return;
    }

    if (!form.password) {
      setError(
        "Please enter a password."
      );
      return;
    }

    if (form.password.length < 8) {
      setError(
        "Password must contain at least 8 characters."
      );
      return;
    }

    if (
      form.password !==
      form.confirmPassword
    ) {
      setError(
        "Passwords do not match."
      );
      return;
    }

    if (!form.agreeToTerms) {
      setError(
        "Please accept the Terms and Conditions."
      );
      return;
    }

    // =======================================================
    // SEND DATA TO BACKEND
    // =======================================================

    setIsLoading(true);

    try {
      /*
        IMPORTANT

        This object matches your ASP.NET RegisterRequest:

        public string FullName
        public string Email
        public string Password

        ASP.NET Core JSON handling maps:

        fullName -> FullName
        email    -> Email
        password -> Password
      */

      const registerData = {
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        password: form.password,
      };

      console.log(
        "Sending registration data:",
        registerData
      );

      const response =
        await registerUser(
          registerData
        );

      console.log(
        "Registration successful:",
        response
      );

      // =====================================================
      // REGISTRATION SUCCESSFUL
      // =====================================================

      navigate("/login", {
        replace: true,

        state: {
          registered: true,
        },
      });

    } catch (err) {
      console.error(
        "Registration error:",
        err
      );

      setError(
        err.message ||
          "Registration failed. Please try again."
      );

    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthShell
      badge="CREATE ACCOUNT"
      title="Join Hireon"
      subtitle="Create your account and start using the recruitment and skill-gap matching platform."
    >

      <form
        onSubmit={handleSubmit}
        className="space-y-5"
      >

        {/* =====================================================
            FULL NAME
        ===================================================== */}

        <FormField
          label="Full name"
          name="fullName"
          type="text"
          placeholder="John Smith"
          value={form.fullName}
          onChange={handleChange}
          icon={User}
          required
          autoComplete="name"
        />

        {/* =====================================================
            EMAIL
        ===================================================== */}

        <FormField
          label="Email address"
          name="email"
          type="email"
          placeholder="name@example.com"
          value={form.email}
          onChange={handleChange}
          icon={Mail}
          required
          autoComplete="email"
        />

        {/* =====================================================
            PASSWORD
        ===================================================== */}

        <PasswordField
          label="Password"
          name="password"
          placeholder="Create a password"
          value={form.password}
          onChange={handleChange}
          required
          autoComplete="new-password"
        />

        {/* PASSWORD RULE */}

        <div className="flex items-start gap-2 -mt-2">

          <CheckCircle2
            size={14}
            className="mt-0.5 text-neutral-400 shrink-0"
          />

          <p className="text-[11px] text-neutral-400 leading-relaxed">
            Use at least 8 characters with uppercase,
            lowercase and a number.
          </p>

        </div>

        {/* =====================================================
            CONFIRM PASSWORD
        ===================================================== */}

        <PasswordField
          label="Confirm password"
          name="confirmPassword"
          placeholder="Enter your password again"
          value={form.confirmPassword}
          onChange={handleChange}
          required
          autoComplete="new-password"
        />

        {/* =====================================================
            ERROR
        ===================================================== */}

        {error && (
          <div className="flex items-start gap-3 p-3.5 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">

            <AlertCircle
              size={17}
              className="mt-0.5 shrink-0"
            />

            <span>
              {error}
            </span>

          </div>
        )}

        {/* =====================================================
            TERMS
        ===================================================== */}

        <label className="flex items-start gap-3 cursor-pointer">

          <input
            type="checkbox"
            name="agreeToTerms"
            checked={form.agreeToTerms}
            onChange={handleChange}
            className="mt-0.5 w-4 h-4 rounded border-neutral-300 accent-violet-600"
          />

          <span className="text-xs sm:text-sm text-neutral-500 leading-relaxed">

            I agree to the{" "}

            <button
              type="button"
              className="font-medium text-neutral-700 hover:text-violet-600 transition"
            >
              Terms & Conditions
            </button>

            {" "}and{" "}

            <button
              type="button"
              className="font-medium text-neutral-700 hover:text-violet-600 transition"
            >
              Privacy Policy
            </button>

            .

          </span>

        </label>

        {/* =====================================================
            SUBMIT BUTTON
        ===================================================== */}

        <button
          type="submit"
          disabled={isLoading}
          className="
            group
            w-full
            h-12
            rounded-xl
            bg-neutral-900
            text-white
            text-sm
            font-semibold
            flex
            items-center
            justify-center
            gap-3
            hover:bg-neutral-800
            active:scale-[0.99]
            transition
            disabled:opacity-60
            disabled:cursor-not-allowed
            disabled:active:scale-100
          "
        >

          {isLoading
            ? "Creating account..."
            : "Create Account"}

          {!isLoading && (
            <ArrowRight
              size={16}
              className="group-hover:translate-x-1 transition-transform"
            />
          )}

        </button>

      </form>

      {/* =======================================================
          DIVIDER
      ======================================================= */}

      <div className="relative my-7">

        <div className="absolute inset-0 flex items-center">

          <div className="w-full border-t border-neutral-200" />

        </div>

        <div className="relative flex justify-center">

          <span className="bg-white px-4 text-xs text-neutral-400">
            ALREADY REGISTERED?
          </span>

        </div>

      </div>

      {/* =======================================================
          LOGIN
      ======================================================= */}

      <p className="text-center text-sm text-neutral-500">

        Already have an account?{" "}

        <Link
          to="/login"
          className="font-semibold text-violet-600 hover:text-violet-700 transition"
        >
          Sign in
        </Link>

      </p>

    </AuthShell>
  );
}

export default RegisterPage;