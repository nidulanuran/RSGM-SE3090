import { useState } from "react";
import {
  Link,
  useLocation,
  useNavigate,
} from "react-router-dom";

import {
  AlertCircle,
  ArrowRight,
  CircleCheck,
  Mail,
} from "lucide-react";

import AuthShell from "../components/auth/AuthShell";
import FormField from "../components/auth/FormField";
import PasswordField from "../components/auth/PasswordField";

import {
  loginUser,
  saveAuth,
} from "../services/authService";

function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();

  const [form, setForm] = useState({
    email: "",
    password: "",
    rememberMe: false,
  });

  const [error, setError] = useState("");
  const [isLoading, setIsLoading] =
    useState(false);

  const successMessage =
    location.state?.message || "";

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

    setError("");
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    setError("");

    if (
      !form.email.trim() ||
      !form.password
    ) {
      setError(
        "Please enter your email and password."
      );
      return;
    }

    setIsLoading(true);

    try {
      const response = await loginUser({
        email: form.email.trim(),
        password: form.password,
      });

      saveAuth(
        response,
        form.rememberMe
      );

      const roles = response.roles || [];

      if (roles.includes("SystemAdmin")) {
        navigate("/admin", {
          replace: true,
        });
      } else if (
        roles.includes("Recruiter")
      ) {
        navigate("/recruiter", {
          replace: true,
        });
      } else if (
        roles.includes("HRManager")
      ) {
        navigate("/hr", {
          replace: true,
        });
      } else if (
        roles.includes("JobSeeker")
      ) {
        navigate("/jobs", {
          replace: true,
        });
      } else if (
        roles.includes("HiringPanelist")
      ) {
        navigate("/panelist", {
          replace: true,
        });
      } else {
        navigate("/", {
          replace: true,
        });
      }
    } catch (err) {
      setError(
        err.message ||
          "Unable to login."
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthShell
      title="Sign in to Hireon"
      subtitle="Access your account and continue managing your recruitment workflow."
    >
      <form
        onSubmit={handleSubmit}
        className="space-y-5"
      >
        {/* ACCOUNT DELETED SUCCESS MESSAGE */}

        {successMessage && (
          <div className="flex items-start gap-3 p-3 rounded-xl border border-emerald-200 bg-emerald-50 text-sm text-emerald-700">
            <CircleCheck
              size={17}
              className="mt-0.5 shrink-0"
            />

            <span>{successMessage}</span>
          </div>
        )}

        {/* EMAIL */}

        <FormField
          label="Email address"
          name="email"
          type="email"
          value={form.email}
          onChange={handleChange}
          placeholder="you@example.com"
          icon={Mail}
          autoComplete="email"
        />

        {/* PASSWORD */}

        <PasswordField
          label="Password"
          name="password"
          value={form.password}
          onChange={handleChange}
          placeholder="Enter your password"
          autoComplete="current-password"
        />

        {/* ERROR MESSAGE */}

        {error && (
          <div className="flex items-start gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">
            <AlertCircle
              size={17}
              className="mt-0.5 shrink-0"
            />

            <span>{error}</span>
          </div>
        )}

        {/* LOGIN OPTIONS */}

        <div className="flex items-center justify-between gap-4">
          <label className="flex items-center gap-2 cursor-pointer text-sm text-neutral-500">
            <input
              type="checkbox"
              name="rememberMe"
              checked={form.rememberMe}
              onChange={handleChange}
              className="w-4 h-4 rounded border-neutral-300 accent-violet-600"
            />

            Remember me
          </label>

          <button
            type="button"
            className="text-sm font-medium text-violet-600 hover:text-violet-700 transition"
          >
            Forgot password?
          </button>
        </div>

        {/* LOGIN BUTTON */}

        <button
          type="submit"
          disabled={isLoading}
          className="group w-full h-12 rounded-xl bg-neutral-900 text-white text-sm font-semibold flex items-center justify-center gap-3 hover:bg-neutral-800 active:scale-[0.99] transition disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {isLoading
            ? "Signing in..."
            : "Sign in"}

          {!isLoading && (
            <ArrowRight
              size={17}
              className="group-hover:translate-x-1 transition-transform"
            />
          )}
        </button>
      </form>

      {/* DIVIDER */}

      <div className="relative my-7">
        <div className="absolute inset-0 flex items-center">
          <div className="w-full border-t border-neutral-200" />
        </div>

        <div className="relative flex justify-center">
          <span className="bg-white px-4 text-xs text-neutral-400">
            NEW TO HIREON?
          </span>
        </div>
      </div>

      {/* REGISTER LINK */}

      <p className="text-center text-sm text-neutral-500">
        Don&apos;t have an account?{" "}

        <Link
          to="/register"
          className="font-semibold text-violet-600 hover:text-violet-700 transition"
        >
          Create account
        </Link>
      </p>
    </AuthShell>
  );
}

export default LoginPage;