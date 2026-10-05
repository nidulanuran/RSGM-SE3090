import { useEffect, useState } from "react";
import {
  BriefcaseBusiness,
  Building2,
  CalendarClock,
  FileCheck2,
  MapPin,
  Pencil,
  Save,
  UserCheck,
  Users,
  UsersRound,
} from "lucide-react";
import { getHrCompanyProfile, saveHrCompanyProfile } from "../../services/hrCompanyProfileService";
import { getHrDashboardStats } from "../../services/hrDashboardService";

const empty = {
  currentEmployeeCount: 0,
  workingLocationCount: 1,
  organizationType: "Local",
  mainDepartments: "",
  majorSkillRequirements: "",
};

const defaultHiringStats = {
  pendingRequisitions: 0,
  activeJobPostings: 0,
  candidatesInPipeline: 0,
  upcomingInterviews: 0,
  offersAwaitingApproval: 0,
  hiresThisMonth: 0,
};

export default function HRDashboardPage() {
  const [profile, setProfile] = useState(null);
  const [form, setForm] = useState(empty);
  const [hiringStats, setHiringStats] = useState(defaultHiringStats);
  const [statsUpdatedAt, setStatsUpdatedAt] = useState(null);
  const [editing, setEditing] = useState(false);
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState("");
  const [saved, setSaved] = useState("");

  useEffect(() => {
    let cancelled = false;

    async function loadDashboard() {
      try {
        const [profileData, statsData] = await Promise.all([
          getHrCompanyProfile(),
          getHrDashboardStats(),
        ]);

        if (cancelled) return;

        setProfile(profileData);
        setForm({
          currentEmployeeCount: profileData.currentEmployeeCount,
          workingLocationCount: profileData.workingLocationCount || 1,
          organizationType: profileData.organizationType || "Local",
          mainDepartments: profileData.mainDepartments || "",
          majorSkillRequirements: profileData.majorSkillRequirements || "",
        });
        setEditing(!profileData.isConfigured);
        setHiringStats({ ...defaultHiringStats, ...statsData });
        setStatsUpdatedAt(statsData.generatedAt ?? null);
        setError("");
      } catch (err) {
        if (!cancelled) setError(err?.message || "Unable to load the HR dashboard.");
      } finally {
        if (!cancelled) setBusy(false);
      }
    }

    async function refreshStats() {
      try {
        const statsData = await getHrDashboardStats();
        if (cancelled) return;
        setHiringStats({ ...defaultHiringStats, ...statsData });
        setStatsUpdatedAt(statsData.generatedAt ?? null);
      } catch {
        // Keep the last successful dashboard values during background refreshes.
      }
    }

    void loadDashboard();
    const intervalId = window.setInterval(() => void refreshStats(), 30000);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, []);

  const update = (key) => (event) =>
    setForm((current) => ({ ...current, [key]: event.target.value }));

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    setSaved("");

    try {
      const result = await saveHrCompanyProfile({
        ...form,
        currentEmployeeCount: Number(form.currentEmployeeCount),
        workingLocationCount: Number(form.workingLocationCount),
      });
      setProfile(result);
      setEditing(false);
      setSaved("Company information saved.");
    } catch (err) {
      setError(err?.message || "Unable to save company information.");
    } finally {
      setBusy(false);
    }
  }

  if (busy && !profile) {
    return <p className="text-sm text-neutral-500">Loading company dashboard...</p>;
  }

  const companyStats = profile
    ? [
        { label: "Current employees", value: profile.currentEmployeeCount, icon: Users },
        { label: "Working locations", value: profile.workingLocationCount, icon: MapPin },
        { label: "Organization type", value: profile.organizationType || "Not set", icon: Building2 },
      ]
    : [];

  const recruitmentStats = [
    { label: "Pending requisitions", value: hiringStats.pendingRequisitions, icon: FileCheck2 },
    { label: "Active job postings", value: hiringStats.activeJobPostings, icon: BriefcaseBusiness },
    { label: "Candidates in pipeline", value: hiringStats.candidatesInPipeline, icon: UsersRound },
    { label: "Upcoming interviews", value: hiringStats.upcomingInterviews, icon: CalendarClock },
    { label: "Offers awaiting approval", value: hiringStats.offersAwaitingApproval, icon: FileCheck2 },
    { label: "Hires this month", value: hiringStats.hiresThisMonth, icon: UserCheck },
  ];

  return (
    <div className="space-y-7">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-wider text-emerald-600">HR manager workspace</p>
          <h1 className="mt-3 text-3xl font-semibold">{profile?.name || "Company dashboard"}</h1>
          <p className="mt-2 text-sm text-neutral-500">
            Live company and recruitment information for your HR workspace.
          </p>
        </div>

        {profile?.isConfigured && !editing && (
          <button
            type="button"
            onClick={() => setEditing(true)}
            className="inline-flex items-center gap-2 rounded-xl border border-neutral-200 bg-white px-4 py-2 text-sm font-semibold"
          >
            <Pencil size={15} /> Edit profile
          </button>
        )}
      </div>

      {error && <p role="alert" className="rounded-xl bg-red-50 p-3 text-sm text-red-700">{error}</p>}
      {saved && <p role="status" className="rounded-xl bg-emerald-50 p-3 text-sm text-emerald-700">{saved}</p>}

      {!editing && profile && (
        <>
          <section>
            <div className="flex items-end justify-between gap-4">
              <div>
                <h2 className="text-lg font-semibold">Recruitment overview</h2>
                <p className="mt-1 text-sm text-neutral-500">Live counts from your company&apos;s hiring pipeline.</p>
              </div>
              {statsUpdatedAt && (
                <p className="text-xs text-neutral-400">
                  Updated {new Date(statsUpdatedAt).toLocaleTimeString()}
                </p>
              )}
            </div>

            <div className="mt-4 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {recruitmentStats.map((item) => (
                <article key={item.label} className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm">
                  <item.icon size={20} className="text-emerald-600" />
                  <p className="mt-4 text-xs text-neutral-500">{item.label}</p>
                  <p className="mt-1 text-2xl font-semibold">{Number(item.value ?? 0).toLocaleString()}</p>
                </article>
              ))}
            </div>
          </section>

          <section>
            <h2 className="text-lg font-semibold">Company profile</h2>
            <div className="mt-4 grid gap-4 sm:grid-cols-3">
              {companyStats.map((item) => (
                <article key={item.label} className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm">
                  <item.icon size={20} className="text-emerald-600" />
                  <p className="mt-4 text-xs text-neutral-500">{item.label}</p>
                  <p className="mt-1 text-2xl font-semibold">{item.value}</p>
                </article>
              ))}
            </div>
          </section>

          <div className="grid gap-4 lg:grid-cols-2">
            <article className="rounded-2xl border border-neutral-200 bg-white p-5">
              <h2 className="font-semibold">Main departments</h2>
              <p className="mt-3 whitespace-pre-wrap text-sm text-neutral-600">{profile.mainDepartments}</p>
            </article>
            <article className="rounded-2xl border border-neutral-200 bg-white p-5">
              <h2 className="font-semibold">Major skill requirements</h2>
              <p className="mt-3 whitespace-pre-wrap text-sm text-neutral-600">{profile.majorSkillRequirements}</p>
            </article>
          </div>

          <p className="text-xs text-neutral-400">
            The employee count increases automatically when a jobseeker accepts an approved offer.
          </p>
        </>
      )}

      {editing && (
        <form onSubmit={submit} className="grid gap-4 rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm sm:grid-cols-2">
          <div className="sm:col-span-2">
            <h2 className="text-lg font-semibold">{profile?.isConfigured ? "Edit company information" : "Complete company information"}</h2>
            <p className="mt-1 text-sm text-neutral-500">This information powers your HR dashboard.</p>
          </div>

          <label className="text-sm font-medium">
            Current number of employees
            <input required min="0" max="10000000" type="number" value={form.currentEmployeeCount} onChange={update("currentEmployeeCount")} className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5" />
          </label>

          <label className="text-sm font-medium">
            Number of working locations
            <input required min="1" max="100000" type="number" value={form.workingLocationCount} onChange={update("workingLocationCount")} className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5" />
          </label>

          <label className="text-sm font-medium sm:col-span-2">
            Organization type
            <select value={form.organizationType} onChange={update("organizationType")} className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5">
              <option>Local</option>
              <option>International</option>
            </select>
          </label>

          <label className="text-sm font-medium sm:col-span-2">
            Main departments
            <textarea required maxLength={2000} rows={4} value={form.mainDepartments} onChange={update("mainDepartments")} placeholder="Engineering, Finance, Sales, Human Resources..." className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5" />
          </label>

          <label className="text-sm font-medium sm:col-span-2">
            Major skills required by the company
            <textarea required maxLength={2000} rows={4} value={form.majorSkillRequirements} onChange={update("majorSkillRequirements")} placeholder="Software engineering, project management, financial analysis..." className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5" />
          </label>

          <div className="flex gap-3 sm:col-span-2">
            <button disabled={busy} className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50">
              <Save size={16} /> Save company information
            </button>
            {profile?.isConfigured && (
              <button type="button" onClick={() => setEditing(false)} className="rounded-xl border border-neutral-200 px-5 py-2.5 text-sm font-semibold">
                Cancel
              </button>
            )}
          </div>
        </form>
      )}
    </div>
  );
}
