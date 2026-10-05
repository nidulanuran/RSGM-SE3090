import { useEffect, useMemo, useState } from "react";
import {
  BriefcaseBusiness,
  Building2,
  FileText,
  Users,
} from "lucide-react";

import { getAdminDashboardStats } from "../../services/adminDashboardService";

const PIE_COLORS = [
  "#171717",
  "#525252",
  "#737373",
  "#a3a3a3",
  "#d4d4d4",
  "#404040",
  "#8a8a8a",
  "#e5e5e5",
];

function HorizontalBarChart({ items, emptyMessage }) {
  const maxCount = Math.max(...items.map((item) => item.count), 0);

  if (items.length === 0) {
    return <p className="text-sm text-neutral-500">{emptyMessage}</p>;
  }

  return (
    <div className="space-y-4">
      {items.map((item) => {
        const width = maxCount > 0 ? (item.count / maxCount) * 100 : 0;

        return (
          <div key={item.label}>
            <div className="mb-1.5 flex items-center justify-between gap-4 text-sm">
              <span className="truncate text-neutral-700">{item.label}</span>
              <span className="font-medium text-neutral-900">{item.count}</span>
            </div>
            <div className="h-2.5 overflow-hidden rounded-full bg-neutral-100">
              <div
                className="h-full rounded-full bg-neutral-800 transition-all duration-500"
                style={{ width: `${Math.max(width, item.count > 0 ? 4 : 0)}%` }}
              />
            </div>
          </div>
        );
      })}
    </div>
  );
}

function DonutChart({ items, total }) {
  const segments = useMemo(() => {
    if (total <= 0) return [];

    let cursor = 0;
    return items
      .filter((item) => item.count > 0)
      .map((item, index) => {
        const start = cursor;
        const end = cursor + (item.count / total) * 100;
        cursor = end;

        return {
          ...item,
          color: PIE_COLORS[index % PIE_COLORS.length],
          start,
          end,
        };
      });
  }, [items, total]);

  const background = segments.length
    ? `conic-gradient(${segments
        .map((segment) => `${segment.color} ${segment.start}% ${segment.end}%`)
        .join(", ")})`
    : "#f5f5f5";

  return (
    <div className="grid items-center gap-6 lg:grid-cols-[180px_1fr]">
      <div className="relative mx-auto h-44 w-44 rounded-full" style={{ background }}>
        <div className="absolute inset-8 flex flex-col items-center justify-center rounded-full bg-white shadow-sm">
          <span className="text-3xl font-semibold text-neutral-900">{total}</span>
          <span className="text-xs text-neutral-500">Applications</span>
        </div>
      </div>

      <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-1 xl:grid-cols-2">
        {segments.length === 0 ? (
          <p className="text-sm text-neutral-500">No application data yet.</p>
        ) : (
          segments.map((segment) => (
            <div
              key={segment.label}
              className="flex items-center justify-between gap-3 rounded-lg bg-neutral-50 px-3 py-2"
            >
              <div className="flex min-w-0 items-center gap-2">
                <span
                  className="h-2.5 w-2.5 shrink-0 rounded-full"
                  style={{ backgroundColor: segment.color }}
                />
                <span className="truncate text-sm text-neutral-600">{segment.label}</span>
              </div>
              <span className="text-sm font-medium text-neutral-900">
                {segment.count}
              </span>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

function AdminDashboardPage() {
  const [stats, setStats] = useState({
    totalUsers: 0,
    activeUsers: 0,
    totalCompanies: 0,
    totalJobPostings: 0,
    totalApplications: 0,
    totalSkills: 0,
    publishedJobPostings: 0,
    newUsersLast30Days: 0,
    usersByRole: [],
    applicationsByStatus: [],
    jobPostingsByStatus: [],
  });

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let cancelled = false;

    async function loadInitialDashboard() {
      try {
        const data = await getAdminDashboardStats();
        if (!cancelled) {
          setStats({
            totalUsers: data.totalUsers ?? 0,
            activeUsers: data.activeUsers ?? 0,
            totalCompanies: data.totalCompanies ?? 0,
            totalJobPostings: data.totalJobPostings ?? 0,
            totalApplications: data.totalApplications ?? 0,
            totalSkills: data.totalSkills ?? 0,
            publishedJobPostings: data.publishedJobPostings ?? 0,
            newUsersLast30Days: data.newUsersLast30Days ?? 0,
            usersByRole: data.usersByRole ?? [],
            applicationsByStatus: data.applicationsByStatus ?? [],
            jobPostingsByStatus: data.jobPostingsByStatus ?? [],
          });
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message || "Failed to load dashboard statistics.");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadInitialDashboard();
    return () => {
      cancelled = true;
    };
  }, []);

  const cards = [
    {
      label: "Total Users",
      value: stats.totalUsers,
      icon: Users,
    },
    {
      label: "Companies",
      value: stats.totalCompanies,
      icon: Building2,
    },
    {
      label: "Job Postings",
      value: stats.totalJobPostings,
      icon: BriefcaseBusiness,
    },
    {
      label: "Applications",
      value: stats.totalApplications,
      icon: FileText,
    },
  ];

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-semibold">Admin Dashboard</h1>

        <p className="mt-1 text-sm text-neutral-500">
          Overview of users, companies, jobs, and applications.
        </p>
      </div>

      {error && (
        <div className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {loading ? (
        <p className="text-sm text-neutral-500">Loading dashboard...</p>
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {cards.map((card) => {
              const Icon = card.icon;

              return (
                <div
                  key={card.label}
                  className="rounded-2xl border border-neutral-200 bg-white p-5"
                >
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm text-neutral-500">{card.label}</p>
                      <p className="mt-2 text-3xl font-semibold">{card.value}</p>
                    </div>

                    <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-neutral-100">
                      <Icon size={20} className="text-neutral-700" />
                    </div>
                  </div>
                </div>
              );
            })}
          </div>

          <div className="mt-6 grid gap-4 sm:grid-cols-3">
            <div className="rounded-2xl border border-neutral-200 bg-white p-5">
              <p className="text-sm text-neutral-500">Skills in Catalog</p>
              <p className="mt-2 text-2xl font-semibold">{stats.totalSkills}</p>
            </div>
            <div className="rounded-2xl border border-neutral-200 bg-white p-5">
              <p className="text-sm text-neutral-500">Published Jobs</p>
              <p className="mt-2 text-2xl font-semibold">
                {stats.publishedJobPostings}
              </p>
            </div>
            <div className="rounded-2xl border border-neutral-200 bg-white p-5">
              <p className="text-sm text-neutral-500">New Users (30 days)</p>
              <p className="mt-2 text-2xl font-semibold">
                {stats.newUsersLast30Days}
              </p>
            </div>
          </div>

          <div className="mt-6 grid gap-6 xl:grid-cols-2">
            <section className="rounded-2xl border border-neutral-200 bg-white p-5 sm:p-6">
              <div className="mb-5">
                <h2 className="font-semibold">Users by Role</h2>
                <p className="mt-1 text-sm text-neutral-500">
                  Distribution of registered users across system roles.
                </p>
              </div>

              <HorizontalBarChart
                items={stats.usersByRole}
                emptyMessage="No role data available yet."
              />
            </section>

            <section className="rounded-2xl border border-neutral-200 bg-white p-5 sm:p-6">
              <div className="mb-5">
                <h2 className="font-semibold">Application Status</h2>
                <p className="mt-1 text-sm text-neutral-500">
                  Current candidate application distribution.
                </p>
              </div>

              <DonutChart
                items={stats.applicationsByStatus}
                total={stats.totalApplications}
              />
            </section>
          </div>

          <div className="mt-6 grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
            <section className="rounded-2xl border border-neutral-200 bg-white p-5 sm:p-6">
              <div className="mb-5">
                <h2 className="font-semibold">Job Posting Status</h2>
                <p className="mt-1 text-sm text-neutral-500">
                  Compare draft, published, and closed job postings.
                </p>
              </div>

              <HorizontalBarChart
                items={stats.jobPostingsByStatus}
                emptyMessage="No job posting data available yet."
              />
            </section>

            <section className="rounded-2xl border border-neutral-200 bg-white p-5 sm:p-6">
              <h2 className="font-semibold">User Activity</h2>

              <div className="mt-4 grid gap-4 sm:grid-cols-2 xl:grid-cols-1">
                <div className="rounded-xl bg-neutral-50 p-4">
                  <p className="text-sm text-neutral-500">Active Users</p>
                  <p className="mt-1 text-2xl font-semibold">{stats.activeUsers}</p>
                </div>

                <div className="rounded-xl bg-neutral-50 p-4">
                  <p className="text-sm text-neutral-500">Inactive Users</p>
                  <p className="mt-1 text-2xl font-semibold">
                    {Math.max(stats.totalUsers - stats.activeUsers, 0)}
                  </p>
                </div>
              </div>
            </section>
          </div>
        </>
      )}
    </div>
  );
}

export default AdminDashboardPage;
