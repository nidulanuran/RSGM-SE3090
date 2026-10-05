import { useEffect, useMemo, useState } from "react";

import {
  AlertCircle,
  BarChart3,
  BriefcaseBusiness,
  CheckCircle2,
  FileCheck2,
  FileText,
  Loader2,
  Sparkles,
  Target,
  TrendingUp,
  UserRound,
  XCircle,
} from "lucide-react";

import { getJobSeekerDashboard } from "../../services/jobSeekerDashboardService";

const emptyDashboard = {
  fullName: "",
  profileCompleteness: 0,
  totalApplications: 0,
  activeApplications: 0,
  shortlistedApplications: 0,
  rejectedApplications: 0,
  interviewApplications: 0,
  offerApplications: 0,
  hiredApplications: 0,
  withdrawnApplications: 0,
  availableJobs: 0,
  cvPassRate: 0,
  rejectionRate: 0,
  interviewRate: 0,
  offerRate: 0,
  averageMatchScore: 0,
  skillsCount: 0,
  cvUploaded: false,
  applicationStatusBreakdown: [],
  applicationTrend: [],
  skillGapStats: [],
};

function clampPercentage(value) {
  return Math.min(Math.max(Number(value) || 0, 0), 100);
}

function formatRate(value) {
  const number = Number(value) || 0;
  return `${Number.isInteger(number) ? number : number.toFixed(1)}%`;
}

function MetricCard({ label, value, helper, icon: Icon }) {
  return (
    <div className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm shadow-neutral-100/60">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-sm text-neutral-500">{label}</p>
          <p className="mt-2 text-3xl font-semibold tracking-tight text-neutral-900">
            {value}
          </p>
          {helper && (
            <p className="mt-1 text-xs leading-5 text-neutral-400">{helper}</p>
          )}
        </div>

        <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-neutral-100">
          <Icon size={20} className="text-neutral-700" />
        </div>
      </div>
    </div>
  );
}

function HorizontalBarChart({ items }) {
  const maxValue = Math.max(...items.map((item) => item.count), 1);

  if (!items.length || items.every((item) => item.count === 0)) {
    return (
      <div className="flex h-48 items-center justify-center rounded-xl bg-neutral-50 text-sm text-neutral-400">
        No application activity yet.
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {items.map((item) => (
        <div key={item.status}>
          <div className="mb-1.5 flex items-center justify-between gap-3 text-sm">
            <span className="text-neutral-600">{item.status}</span>
            <span className="font-semibold text-neutral-900">{item.count}</span>
          </div>
          <div className="h-2.5 overflow-hidden rounded-full bg-neutral-100">
            <div
              className="h-full rounded-full bg-violet-600 transition-all duration-500"
              style={{ width: `${(item.count / maxValue) * 100}%` }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}

function TrendBarChart({ items }) {
  const maxValue = Math.max(...items.map((item) => item.applications), 1);

  if (!items.length) {
    return (
      <div className="flex h-56 items-center justify-center rounded-xl bg-neutral-50 text-sm text-neutral-400">
        No trend data available.
      </div>
    );
  }

  return (
    <div className="flex h-56 items-end gap-3 pt-6">
      {items.map((item) => {
        const height = item.applications === 0
          ? 8
          : Math.max((item.applications / maxValue) * 170, 18);

        return (
          <div key={item.label} className="flex min-w-0 flex-1 flex-col items-center">
            <span className="mb-2 text-xs font-semibold text-neutral-700">
              {item.applications}
            </span>
            <div className="flex h-42.5 w-full items-end justify-center rounded-xl bg-neutral-50 px-2">
              <div
                className="w-full max-w-12 rounded-t-lg bg-violet-500 transition-all duration-500"
                style={{ height: `${height}px` }}
              />
            </div>
            <span className="mt-2 text-xs text-neutral-500">{item.label}</span>
          </div>
        );
      })}
    </div>
  );
}

function RateRow({ label, value }) {
  const percentage = clampPercentage(value);

  return (
    <div>
      <div className="mb-1.5 flex items-center justify-between gap-3">
        <span className="text-sm text-neutral-600">{label}</span>
        <span className="text-sm font-semibold text-neutral-900">
          {formatRate(value)}
        </span>
      </div>
      <div className="h-2 overflow-hidden rounded-full bg-neutral-100">
        <div
          className="h-full rounded-full bg-violet-600 transition-all duration-500"
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}

function JobSeekerDashboardPage() {
  const [dashboard, setDashboard] = useState(emptyDashboard);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let cancelled = false;

    async function loadInitialDashboard() {
      try {
        const data = await getJobSeekerDashboard();

        if (!cancelled) {
          setDashboard({ ...emptyDashboard, ...data });
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message || "Failed to load dashboard.");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadInitialDashboard();
    return () => { cancelled = true; };
  }, []);

  const cards = useMemo(() => [
    {
      label: "Total Applications",
      value: dashboard.totalApplications,
      helper: `${dashboard.activeApplications} currently active`,
      icon: FileText,
    },
    {
      label: "CV Pass Rate",
      value: formatRate(dashboard.cvPassRate),
      helper: "Reached shortlist stage or beyond",
      icon: FileCheck2,
    },
    {
      label: "Interview Rate",
      value: formatRate(dashboard.interviewRate),
      helper: `${dashboard.interviewApplications} reached interview stage`,
      icon: TrendingUp,
    },
    {
      label: "Rejection Rate",
      value: formatRate(dashboard.rejectionRate),
      helper: `${dashboard.rejectedApplications} applications rejected`,
      icon: XCircle,
    },
    {
      label: "Offers Received",
      value: dashboard.offerApplications,
      helper: `${formatRate(dashboard.offerRate)} of active job search outcomes`,
      icon: CheckCircle2,
    },
    {
      label: "Available Jobs",
      value: dashboard.availableJobs,
      helper: "Published jobs you have not applied for",
      icon: BriefcaseBusiness,
    },
  ], [dashboard]);

  if (loading) {
    return (
      <div className="flex items-center gap-2 text-sm text-neutral-500">
        <Loader2 size={18} className="animate-spin" />
        Loading dashboard...
      </div>
    );
  }

  return (
    <div className="pb-10">
      <div className="inline-flex items-center gap-2 rounded-full bg-violet-50 px-3 py-1.5 text-[11px] font-semibold text-violet-600">
        <Sparkles size={12} />
        JOB SEEKER DASHBOARD
      </div>

      <h1 className="mt-4 text-3xl font-semibold tracking-tight">
        Welcome{dashboard.fullName ? `, ${dashboard.fullName}` : ""}
      </h1>

      <p className="mt-2 text-neutral-500">
        Track your profile, application performance, and skill gaps in one place.
      </p>

      {error && (
        <div className="mt-5 flex max-w-lg items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-600">
          <AlertCircle size={17} className="mt-0.5 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <section className="mt-8 rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm shadow-neutral-100/60">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-violet-100">
              <UserRound size={20} className="text-violet-600" />
            </div>

            <div>
              <h2 className="font-semibold">Profile Completion</h2>
              <p className="text-sm text-neutral-500">
                Complete your profile to improve your job opportunities.
              </p>
            </div>
          </div>

          <div className="flex flex-wrap gap-2 text-xs">
            <span className="rounded-full bg-neutral-100 px-3 py-1.5 font-medium text-neutral-600">
              {dashboard.skillsCount} recorded skills
            </span>
            <span className="rounded-full bg-neutral-100 px-3 py-1.5 font-medium text-neutral-600">
              Avg. match {formatRate(dashboard.averageMatchScore)}
            </span>
            <span className="rounded-full bg-neutral-100 px-3 py-1.5 font-medium text-neutral-600">
              CV {dashboard.cvUploaded ? "uploaded" : "missing"}
            </span>
            <span className="rounded-full bg-violet-50 px-3 py-1.5 font-semibold text-violet-600">
              {dashboard.profileCompleteness}% complete
            </span>
          </div>
        </div>

        <div className="mt-4 h-2 overflow-hidden rounded-full bg-neutral-100">
          <div
            className="h-full rounded-full bg-violet-600 transition-all duration-500"
            style={{ width: `${clampPercentage(dashboard.profileCompleteness)}%` }}
          />
        </div>
      </section>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-6">
        {cards.map((card) => (
          <MetricCard key={card.label} {...card} />
        ))}
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-2">
        <section className="rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm shadow-neutral-100/60">
          <div className="mb-6 flex items-start justify-between gap-4">
            <div>
              <h2 className="flex items-center gap-2 text-lg font-semibold">
                <BarChart3 size={19} className="text-violet-600" />
                Application Status Overview
              </h2>
              <p className="mt-1 text-sm text-neutral-500">
                Current distribution of your application outcomes.
              </p>
            </div>
          </div>

          <HorizontalBarChart items={dashboard.applicationStatusBreakdown} />
        </section>

        <section className="rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm shadow-neutral-100/60">
          <div>
            <h2 className="flex items-center gap-2 text-lg font-semibold">
              <TrendingUp size={19} className="text-violet-600" />
              Application Activity
            </h2>
            <p className="mt-1 text-sm text-neutral-500">
              Applications submitted during the last six months.
            </p>
          </div>

          <TrendBarChart items={dashboard.applicationTrend} />
        </section>
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-2">
        <section className="rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm shadow-neutral-100/60">
          <div className="mb-6">
            <h2 className="flex items-center gap-2 text-lg font-semibold">
              <Target size={19} className="text-violet-600" />
              Job Search Conversion
            </h2>
            <p className="mt-1 text-sm text-neutral-500">
              Rates are calculated from non-withdrawn applications.
            </p>
          </div>

          <div className="space-y-5">
            <RateRow label="CV pass rate" value={dashboard.cvPassRate} />
            <RateRow label="Interview rate" value={dashboard.interviewRate} />
            <RateRow label="Offer rate" value={dashboard.offerRate} />
            <RateRow label="Rejection rate" value={dashboard.rejectionRate} />
          </div>
        </section>

        <section className="rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm shadow-neutral-100/60">
          <div className="mb-6">
            <h2 className="flex items-center gap-2 text-lg font-semibold">
              <Sparkles size={19} className="text-violet-600" />
              Skill Gap Analysis
            </h2>
            <p className="mt-1 text-sm text-neutral-500">
              Most frequent missing skills across your active application history.
            </p>
          </div>

          {dashboard.skillGapStats.length > 0 ? (
            <div className="space-y-4">
              {dashboard.skillGapStats.map((item) => {
                const maxCount = Math.max(
                  ...dashboard.skillGapStats.map((skill) => skill.count),
                  1
                );

                return (
                  <div key={item.skill}>
                    <div className="mb-1.5 flex items-center justify-between gap-3 text-sm">
                      <span className="font-medium text-neutral-700">{item.skill}</span>
                      <span className="text-neutral-500">{item.count} jobs</span>
                    </div>
                    <div className="h-2.5 overflow-hidden rounded-full bg-neutral-100">
                      <div
                        className="h-full rounded-full bg-violet-500 transition-all duration-500"
                        style={{ width: `${(item.count / maxCount) * 100}%` }}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="flex h-48 items-center justify-center rounded-xl bg-neutral-50 px-6 text-center text-sm text-neutral-400">
              No skill-gap data is available yet. Apply to jobs with recorded skill requirements to populate this chart.
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

export default JobSeekerDashboardPage;
