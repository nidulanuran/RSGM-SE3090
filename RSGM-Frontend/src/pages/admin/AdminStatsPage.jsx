import { useEffect, useMemo, useState } from "react";
import { Activity, BriefcaseBusiness, ListChecks, Loader2, RefreshCw, Sparkles, TrendingUp, Users } from "lucide-react";
import { getAdminStatistics } from "../../services/adminMonitoringService";

const ROLE_COLORS = ["bg-violet-500", "bg-blue-500", "bg-cyan-500", "bg-emerald-500", "bg-neutral-800", "bg-amber-500"];

function AdminStatsPage() {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");

  async function refreshStats() {
    try {
      setRefreshing(true);
      setError("");
      setStats(await getAdminStatistics());
    } catch (err) {
      setError(err.message || "Failed to load statistics.");
    } finally {
      setRefreshing(false);
    }
  }

  useEffect(() => {
    let cancelled = false;

    getAdminStatistics()
      .then((data) => {
        if (cancelled) return;
        setStats(data);
        setError("");
      })
      .catch((err) => {
        if (cancelled) return;
        setError(err.message || "Failed to load statistics.");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const roleBreakdown = useMemo(() => stats?.roleBreakdown ?? [], [stats]);
  const totalRoleUsers = roleBreakdown.reduce((sum, role) => sum + role.count, 0);

  const cards = stats ? [
    { icon: Users, label: "Total users", value: stats.totalUsers, trend: `+${stats.newUsersLast7Days} in the last 7 days` },
    { icon: BriefcaseBusiness, label: "Active job postings", value: stats.activeJobPostings, trend: `+${stats.newJobsLast7Days} jobs created in 7 days` },
    { icon: ListChecks, label: "Skills in catalog", value: stats.skillsInCatalog, trend: `+${stats.newSkillsLast7Days} skills in 7 days` },
    { icon: Activity, label: "AI workflows run (24h)", value: stats.aiWorkflowsLast24Hours, trend: `${stats.aiWorkflowSuccessRate}% completed successfully` },
  ] : [];

  return (
    <div>
      <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-violet-50 text-violet-600 text-[11px] font-semibold">
        <Sparkles size={12} />
        SYSTEM OVERVIEW
      </div>

      <div className="mt-4 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl sm:text-4xl font-semibold tracking-tight">Statistics</h1>
          <p className="mt-2 text-neutral-500">Live platform statistics calculated from the database.</p>
        </div>
        <button
          type="button"
          onClick={refreshStats}
          disabled={refreshing}
          className="inline-flex h-10 items-center gap-2 rounded-xl border border-neutral-200 bg-white px-4 text-sm font-medium text-neutral-700 hover:bg-neutral-50 disabled:opacity-60"
        >
          <RefreshCw size={15} className={refreshing ? "animate-spin" : ""} /> Refresh
        </button>
      </div>

      {error && <div className="mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}

      {loading ? (
        <div className="mt-8 flex items-center gap-2 text-sm text-neutral-500"><Loader2 size={18} className="animate-spin" /> Loading statistics...</div>
      ) : (
        <>
          <div className="mt-8 grid sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {cards.map((card) => (
              <div key={card.label} className="relative overflow-hidden rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6">
                <div className="w-11 h-11 rounded-2xl bg-violet-100 flex items-center justify-center">
                  <card.icon size={20} className="text-violet-600" />
                </div>
                <p className="mt-4 text-xs text-neutral-400">{card.label}</p>
                <p className="mt-1 text-2xl font-semibold tracking-tight">{Number(card.value ?? 0).toLocaleString()}</p>
                <p className="mt-2 flex items-center gap-1 text-xs text-emerald-600 font-medium">
                  <TrendingUp size={12} /> {card.trend}
                </p>
              </div>
            ))}
          </div>

          <div className="mt-8 rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6">
            <h2 className="text-lg font-semibold tracking-tight">Users by role</h2>
            <div className="mt-5 space-y-4">
              {roleBreakdown.map((role, index) => (
                <div key={role.role}>
                  <div className="flex items-center justify-between text-sm mb-1.5">
                    <span className="font-medium text-neutral-700">{role.role}</span>
                    <span className="text-neutral-400">{role.count}</span>
                  </div>
                  <div className="h-2 rounded-full bg-neutral-100 overflow-hidden">
                    <div
                      className={`h-full rounded-full ${ROLE_COLORS[index % ROLE_COLORS.length]}`}
                      style={{ width: `${totalRoleUsers === 0 ? 0 : (role.count / totalRoleUsers) * 100}%` }}
                    />
                  </div>
                </div>
              ))}
              {roleBreakdown.length === 0 && <p className="text-sm text-neutral-400">No role data is available yet.</p>}
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default AdminStatsPage;
