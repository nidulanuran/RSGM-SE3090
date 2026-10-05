import { useEffect, useMemo, useState } from "react";
import { FileClock, Loader2, RefreshCw, Search, ShieldAlert, Sparkles } from "lucide-react";
import { getAdminAuditLogs } from "../../services/adminMonitoringService";

const SEVERITY_STYLES = {
  info: "bg-blue-50 text-blue-600",
  warning: "bg-amber-50 text-amber-600",
  critical: "bg-red-50 text-red-600",
};

function AdminAuditLogsPage() {
  const [logs, setLogs] = useState([]);
  const [query, setQuery] = useState("");
  const [severity, setSeverity] = useState("All");
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");

  async function refreshLogs() {
    try {
      setRefreshing(true);
      setError("");
      const data = await getAdminAuditLogs(200);
      setLogs(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err.message || "Failed to load audit logs.");
    } finally {
      setRefreshing(false);
    }
  }

  useEffect(() => {
    let cancelled = false;

    getAdminAuditLogs(200)
      .then((data) => {
        if (cancelled) return;
        setLogs(Array.isArray(data) ? data : []);
        setError("");
      })
      .catch((err) => {
        if (cancelled) return;
        setError(err.message || "Failed to load audit logs.");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const filteredLogs = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    return logs.filter((log) => {
      const matchesQuery = !normalizedQuery ||
        [log.actor, log.action, log.target, log.result]
          .some((value) => String(value ?? "").toLowerCase().includes(normalizedQuery));
      const matchesSeverity = severity === "All" || log.severity === severity;
      return matchesQuery && matchesSeverity;
    });
  }, [logs, query, severity]);

  return (
    <div>
      <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-violet-50 text-violet-600 text-[11px] font-semibold">
        <Sparkles size={12} />
        AUDIT TRAIL
      </div>

      <div className="mt-4 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl sm:text-4xl font-semibold tracking-tight">Audit Logs</h1>
          <p className="mt-2 text-neutral-500">Real audit events stored in the platform database.</p>
        </div>
        <button
          type="button"
          onClick={refreshLogs}
          disabled={refreshing}
          className="inline-flex h-10 items-center gap-2 rounded-xl border border-neutral-200 bg-white px-4 text-sm font-medium text-neutral-700 hover:bg-neutral-50 disabled:opacity-60"
        >
          <RefreshCw size={15} className={refreshing ? "animate-spin" : ""} />
          Refresh
        </button>
      </div>

      {error && <div className="mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}

      <div className="mt-8 flex flex-col sm:flex-row gap-3">
        <div className="relative max-w-sm w-full">
          <Search size={17} className="absolute left-4 top-1/2 -translate-y-1/2 text-neutral-400" />
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search logs..."
            className="w-full h-11 rounded-xl border border-neutral-200 bg-white pl-11 pr-4 text-sm outline-none focus:border-violet-400 focus:ring-4 focus:ring-violet-100 transition"
          />
        </div>

        <select
          value={severity}
          onChange={(event) => setSeverity(event.target.value)}
          className="h-11 rounded-xl border border-neutral-200 bg-white px-4 text-sm outline-none focus:border-violet-400 focus:ring-4 focus:ring-violet-100 transition"
        >
          <option value="All">All severities</option>
          <option value="info">Info</option>
          <option value="warning">Warning</option>
          <option value="critical">Critical</option>
        </select>
      </div>

      <div className="mt-6 space-y-3">
        {loading && (
          <div className="flex items-center gap-2 py-12 text-sm text-neutral-500">
            <Loader2 size={18} className="animate-spin" /> Loading audit logs...
          </div>
        )}

        {!loading && filteredLogs.map((log) => (
          <div key={log.id} className="flex items-start gap-4 p-4 rounded-2xl border border-white/70 bg-white/75 backdrop-blur-xl shadow-sm">
            <div className="w-9 h-9 rounded-xl bg-neutral-100 flex items-center justify-center shrink-0">
              {log.severity === "critical"
                ? <ShieldAlert size={16} className="text-red-500" />
                : <FileClock size={16} className="text-neutral-500" />}
            </div>

            <div className="flex-1 min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <p className="text-sm font-medium text-neutral-900">{log.action}</p>
                <span className={`px-2 py-0.5 rounded-full text-[11px] font-medium ${SEVERITY_STYLES[log.severity] ?? SEVERITY_STYLES.info}`}>
                  {log.severity}
                </span>
              </div>
              <p className="mt-1 text-sm text-neutral-500 wrap-break-word">{log.target}</p>
              <p className="mt-1 text-xs text-neutral-400">
                {log.actor} · {new Date(log.timestamp).toLocaleString()}
              </p>
            </div>
          </div>
        ))}

        {!loading && filteredLogs.length === 0 && (
          <div className="py-16 text-center text-sm text-neutral-400 rounded-2xl border border-neutral-200 bg-white">
            No audit entries match your filters.
          </div>
        )}
      </div>
    </div>
  );
}

export default AdminAuditLogsPage;
