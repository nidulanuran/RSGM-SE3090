import { useEffect, useMemo, useState } from "react";
import {
  Clock,
  Percent,
  RefreshCw,
  Sparkles,
  TrendingUp,
  Users,
} from "lucide-react";
import { getHrAnalytics } from "../../services/hrAnalyticsService";

const FUNNEL_COLORS = [
  "bg-emerald-500",
  "bg-teal-500",
  "bg-cyan-500",
  "bg-blue-500",
  "bg-violet-500",
  "bg-neutral-800",
];

function formatNumber(value) {
  return Number(value ?? 0).toLocaleString();
}

function formatPercent(value) {
  const number = Number(value ?? 0);
  return `${Number.isInteger(number) ? number : number.toFixed(1)}%`;
}

function formatDays(value) {
  const number = Number(value ?? 0);
  const formatted = Number.isInteger(number) ? number : number.toFixed(1);
  return `${formatted} ${number === 1 ? "day" : "days"}`;
}

function signed(value, suffix = "") {
  const number = Number(value ?? 0);

  if (number === 0) {
    return `No change${suffix ? ` ${suffix}` : ""}`;
  }

  return `${number > 0 ? "+" : ""}${
    Number.isInteger(number) ? number : number.toFixed(1)
  }${suffix}`;
}

function AnalyticsPage() {
  const [analytics, setAnalytics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let cancelled = false;

    async function fetchAnalytics() {
      try {
        const data = await getHrAnalytics();

        if (cancelled) {
          return;
        }

        setAnalytics(data);
        setError("");
      } catch (err) {
        if (cancelled) {
          return;
        }

        setError(err?.message || "Unable to load analytics.");
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    async function refreshAnalytics() {
      try {
        const data = await getHrAnalytics();

        if (cancelled) {
          return;
        }

        setAnalytics(data);
        setError("");
      } catch (err) {
        if (cancelled) {
          return;
        }

        setError(err?.message || "Unable to refresh analytics.");
      }
    }

    void fetchAnalytics();

    const intervalId = window.setInterval(() => {
      void refreshAnalytics();
    }, 30000);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, []);

  async function handleRefresh() {
    setRefreshing(true);

    try {
      const data = await getHrAnalytics();
      setAnalytics(data);
      setError("");
    } catch (err) {
      setError(err?.message || "Unable to refresh analytics.");
    } finally {
      setRefreshing(false);
    }
  }

  const stats = useMemo(() => {
    const data = analytics?.stats ?? {};

    return [
      {
        icon: Clock,
        label: "Avg. time to hire",
        value: formatDays(data.averageTimeToHireDays),
        trend: `${signed(
          data.averageTimeToHireTrendDays,
          " days"
        )} vs last quarter`,
      },
      {
        icon: Percent,
        label: "Offer acceptance rate",
        value: formatPercent(data.offerAcceptanceRate),
        trend: `${signed(
          data.offerAcceptanceTrend,
          "%"
        )} vs last quarter`,
      },
      {
        icon: Users,
        label: "Candidates in pipeline",
        value: formatNumber(data.candidatesInPipeline),
        trend: `+${formatNumber(
          data.newPipelineThisMonth
        )} new this month`,
      },
      {
        icon: TrendingUp,
        label: "Requisition approval rate",
        value: formatPercent(data.requisitionApprovalRate),
        trend: `${signed(
          data.requisitionApprovalTrend,
          "%"
        )} vs last quarter`,
      },
    ];
  }, [analytics]);

  const funnel = useMemo(
    () =>
      (analytics?.funnel ?? []).map((item, index) => ({
        ...item,
        color: FUNNEL_COLORS[index] ?? "bg-neutral-500",
      })),
    [analytics]
  );

  const maxCount = Math.max(
    1,
    ...funnel.map((item) => Number(item.count ?? 0))
  );

  return (
    <div>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-emerald-50 text-emerald-600 text-[11px] font-semibold">
            <Sparkles size={12} />
            RECRUITMENT ANALYTICS
          </div>

          <h1 className="mt-4 text-3xl sm:text-4xl font-semibold tracking-tight">
            Analytics
          </h1>

          <p className="mt-2 text-neutral-500">
            Live metrics for your company&apos;s hiring funnel this quarter.
          </p>
        </div>

        <button
          type="button"
          onClick={handleRefresh}
          disabled={refreshing}
          className="mt-1 inline-flex items-center gap-2 rounded-xl border border-neutral-200 bg-white px-3.5 py-2 text-sm font-medium text-neutral-700 shadow-sm hover:bg-neutral-50 disabled:opacity-60"
        >
          <RefreshCw
            size={15}
            className={refreshing ? "animate-spin" : ""}
          />
          Refresh
        </button>
      </div>

      {error && (
        <div className="mt-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {loading && !analytics ? (
        <div className="mt-8 rounded-2xl border border-white/70 bg-white/75 p-8 text-sm text-neutral-500 shadow-xl shadow-neutral-200/30">
          Loading live analytics...
        </div>
      ) : (
        <>
          <div className="mt-8 grid sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {stats.map((stat) => (
              <div
                key={stat.label}
                className="rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6"
              >
                <div className="w-11 h-11 rounded-2xl bg-emerald-100 flex items-center justify-center">
                  <stat.icon
                    size={20}
                    className="text-emerald-600"
                  />
                </div>

                <p className="mt-4 text-xs text-neutral-400">
                  {stat.label}
                </p>

                <p className="mt-1 text-2xl font-semibold tracking-tight">
                  {stat.value}
                </p>

                <p className="mt-2 text-xs text-emerald-600 font-medium">
                  {stat.trend}
                </p>
              </div>
            ))}
          </div>

          <div className="mt-8 rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h2 className="text-lg font-semibold tracking-tight">
                Hiring funnel
              </h2>

              {analytics?.generatedAt && (
                <span className="text-xs text-neutral-400">
                  Updated{" "}
                  {new Date(
                    analytics.generatedAt
                  ).toLocaleTimeString()}
                </span>
              )}
            </div>

            <div className="mt-6 space-y-4">
              {funnel.map((item) => (
                <div key={item.stage}>
                  <div className="flex items-center justify-between text-sm mb-1.5">
                    <span className="font-medium text-neutral-700">
                      {item.stage}
                    </span>

                    <span className="text-neutral-400">
                      {formatNumber(item.count)}
                    </span>
                  </div>

                  <div className="h-2.5 rounded-full bg-neutral-100 overflow-hidden">
                    <div
                      className={`h-full rounded-full ${item.color}`}
                      style={{
                        width: `${
                          (Number(item.count ?? 0) / maxCount) * 100
                        }%`,
                      }}
                    />
                  </div>
                </div>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default AnalyticsPage;