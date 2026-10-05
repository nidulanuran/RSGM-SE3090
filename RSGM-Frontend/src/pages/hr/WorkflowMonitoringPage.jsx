import { useEffect, useState } from "react";
import { AlertTriangle, CheckCircle2, Clock, RefreshCw, Sparkles } from "lucide-react";
import { getHrWorkflows } from "../../services/hrDashboardService";

const HEALTH_CONFIG = {
  "on-track": { label: "On track", icon: CheckCircle2, style: "bg-emerald-50 text-emerald-600" },
  "at-risk": { label: "At risk", icon: Clock, style: "bg-amber-50 text-amber-600" },
  blocked: { label: "Blocked", icon: AlertTriangle, style: "bg-red-50 text-red-600" },
};

function WorkflowMonitoringPage() {
  const [workflows, setWorkflows] = useState([]);
  const [generatedAt, setGeneratedAt] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let cancelled = false;

    async function fetchWorkflows() {
      try {
        const data = await getHrWorkflows();
        if (cancelled) return;
        setWorkflows(data.workflows ?? []);
        setGeneratedAt(data.generatedAt ?? null);
        setError("");
      } catch (err) {
        if (!cancelled) setError(err?.message || "Unable to load recruitment workflows.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    async function refreshWorkflows() {
      try {
        const data = await getHrWorkflows();
        if (cancelled) return;
        setWorkflows(data.workflows ?? []);
        setGeneratedAt(data.generatedAt ?? null);
        setError("");
      } catch (err) {
        if (!cancelled) setError(err?.message || "Unable to refresh recruitment workflows.");
      }
    }

    void fetchWorkflows();
    const intervalId = window.setInterval(() => void refreshWorkflows(), 30000);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, []);

  async function handleRefresh() {
    setRefreshing(true);
    try {
      const data = await getHrWorkflows();
      setWorkflows(data.workflows ?? []);
      setGeneratedAt(data.generatedAt ?? null);
      setError("");
    } catch (err) {
      setError(err?.message || "Unable to refresh recruitment workflows.");
    } finally {
      setRefreshing(false);
    }
  }

  return (
    <div>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-emerald-50 text-emerald-600 text-[11px] font-semibold">
            <Sparkles size={12} />
            RECRUITMENT WORKFLOWS
          </div>

          <h1 className="mt-4 text-3xl sm:text-4xl font-semibold tracking-tight">Workflow Monitoring</h1>
          <p className="mt-2 text-neutral-500">
            Track live hiring pipelines and identify workflows with no recent activity.
          </p>
        </div>

        <button
          type="button"
          onClick={handleRefresh}
          disabled={refreshing}
          className="inline-flex items-center gap-2 rounded-xl border border-neutral-200 bg-white px-3.5 py-2 text-sm font-medium text-neutral-700 shadow-sm hover:bg-neutral-50 disabled:opacity-60"
        >
          <RefreshCw size={15} className={refreshing ? "animate-spin" : ""} />
          Refresh
        </button>
      </div>

      {generatedAt && (
        <p className="mt-3 text-xs text-neutral-400">
          Updated {new Date(generatedAt).toLocaleTimeString()}
        </p>
      )}

      {error && (
        <p role="alert" className="mt-5 rounded-xl bg-red-50 p-3 text-sm text-red-700">
          {error}
        </p>
      )}

      <div className="mt-8 space-y-3">
        {loading ? (
          <div className="rounded-2xl border border-white/70 bg-white/75 p-5 text-sm text-neutral-500 shadow-xl shadow-neutral-200/30">
            Loading live workflows...
          </div>
        ) : workflows.length === 0 ? (
          <div className="rounded-2xl border border-white/70 bg-white/75 p-5 text-sm text-neutral-500 shadow-xl shadow-neutral-200/30">
            No active hiring workflows were found for your company.
          </div>
        ) : (
          workflows.map((workflow) => {
            const health = HEALTH_CONFIG[workflow.health] ?? HEALTH_CONFIG["on-track"];
            const HealthIcon = health.icon;

            return (
              <div
                key={workflow.id}
                className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-5 rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30"
              >
                <div>
                  <p className="font-medium text-neutral-900">{workflow.name}</p>
                  <p className="mt-1 text-sm text-neutral-500">
                    Current stage: {workflow.stage} · Open for {workflow.daysOpen} days
                  </p>
                  <p className="mt-1 text-xs text-neutral-400">
                    Last activity {workflow.inactiveDays === 0 ? "today" : `${workflow.inactiveDays} day${workflow.inactiveDays === 1 ? "" : "s"} ago`}
                  </p>
                </div>

                <span className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium shrink-0 ${health.style}`}>
                  <HealthIcon size={13} />
                  {health.label}
                </span>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}

export default WorkflowMonitoringPage;
