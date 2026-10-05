import { useEffect, useMemo, useState } from "react";
import { AlertTriangle, CheckCircle2, Loader2, RefreshCw, Sparkles, Workflow, XCircle } from "lucide-react";
import { getAdminAgentWorkflows } from "../../services/adminMonitoringService";

function statusClass(status, failed) {
  if (failed) return "bg-red-50 text-red-700";
  if (["Completed", "Approved", "Success", "Succeeded"].includes(status)) return "bg-emerald-50 text-emerald-700";
  if (["Running", "Planning"].includes(status)) return "bg-blue-50 text-blue-700";
  return "bg-amber-50 text-amber-700";
}

function AdminWorkflowsPage() {
  const [workflows, setWorkflows] = useState([]);
  const [filter, setFilter] = useState("all");
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");

  async function refreshWorkflows() {
    try {
      setRefreshing(true);
      setError("");
      const data = await getAdminAgentWorkflows(200);
      setWorkflows(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err.message || "Failed to load AI workflows.");
    } finally {
      setRefreshing(false);
    }
  }

  useEffect(() => {
    let cancelled = false;

    getAdminAgentWorkflows(200)
      .then((data) => {
        if (cancelled) return;
        setWorkflows(Array.isArray(data) ? data : []);
        setError("");
      })
      .catch((err) => {
        if (cancelled) return;
        setError(err.message || "Failed to load AI workflows.");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const visibleWorkflows = useMemo(() => {
    if (filter === "failed") return workflows.filter((workflow) => workflow.isFailed);
    if (filter === "active") {
      return workflows.filter((workflow) => ["Created", "Pending", "Planning", "Running", "WaitingForApproval"].includes(workflow.status));
    }
    if (filter === "completed") {
      return workflows.filter((workflow) => ["Completed", "Approved", "Success", "Succeeded"].includes(workflow.status));
    }
    return workflows;
  }, [workflows, filter]);

  const failedCount = workflows.filter((workflow) => workflow.isFailed).length;

  return (
    <div>
      <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-violet-50 text-violet-600 text-[11px] font-semibold">
        <Sparkles size={12} />
        AGENTIC AI MONITORING
      </div>

      <div className="mt-4 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl sm:text-4xl font-semibold tracking-tight">AI Workflows</h1>
          <p className="mt-2 text-neutral-500">Live workflow records from the AI subsystems.</p>
        </div>
        <button
          type="button"
          onClick={refreshWorkflows}
          disabled={refreshing}
          className="inline-flex h-10 items-center gap-2 rounded-xl border border-neutral-200 bg-white px-4 text-sm font-medium text-neutral-700 hover:bg-neutral-50 disabled:opacity-60"
        >
          <RefreshCw size={15} className={refreshing ? "animate-spin" : ""} /> Refresh
        </button>
      </div>

      {error && <div className="mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <div className="flex items-center gap-2 text-sm text-neutral-500">
          <AlertTriangle size={15} className="text-amber-500" />
          {failedCount} failed workflow{failedCount !== 1 ? "s" : ""}
        </div>
        <select
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
          className="h-10 rounded-xl border border-neutral-200 bg-white px-3 text-sm outline-none"
        >
          <option value="all">All workflows</option>
          <option value="active">Active</option>
          <option value="completed">Completed</option>
          <option value="failed">Failed</option>
        </select>
      </div>

      <div className="mt-4 space-y-3">
        {loading && (
          <div className="flex items-center gap-2 py-12 text-sm text-neutral-500">
            <Loader2 size={18} className="animate-spin" /> Loading workflows...
          </div>
        )}

        {!loading && visibleWorkflows.map((workflow) => (
          <div key={`${workflow.name}-${workflow.id}`} className={`flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-5 rounded-2xl border ${workflow.isFailed ? "border-red-100 bg-red-50/40" : "border-neutral-200 bg-white"}`}>
            <div className="flex items-start gap-3 min-w-0">
              <div className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${workflow.isFailed ? "bg-red-100" : "bg-neutral-100"}`}>
                {workflow.isFailed
                  ? <XCircle size={16} className="text-red-500" />
                  : workflow.status === "Completed" || workflow.status === "Approved"
                    ? <CheckCircle2 size={16} className="text-emerald-600" />
                    : <Workflow size={16} className="text-violet-600" />}
              </div>

              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="text-sm font-medium text-neutral-900">{workflow.name}</p>
                  <span className={`rounded-full px-2 py-0.5 text-[11px] font-medium ${statusClass(workflow.status, workflow.isFailed)}`}>{workflow.status}</span>
                </div>
                <p className="mt-1 text-sm text-neutral-500 wrap-break-word">{workflow.subject}</p>
                {workflow.error && workflow.isFailed && <p className="mt-1 text-xs text-red-600 wrap-break-word">{workflow.error}</p>}
                <p className="mt-1 text-xs text-neutral-400">Updated {new Date(workflow.updatedAt).toLocaleString()}</p>
              </div>
            </div>
          </div>
        ))}

        {!loading && visibleWorkflows.length === 0 && (
          <div className="py-16 text-center text-sm text-neutral-400 rounded-2xl border border-neutral-200 bg-white">
            No workflows match this filter.
          </div>
        )}
      </div>
    </div>
  );
}

export default AdminWorkflowsPage;
