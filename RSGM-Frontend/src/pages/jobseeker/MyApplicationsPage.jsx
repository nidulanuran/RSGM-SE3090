import { useEffect, useState } from "react";
import {
  AlertCircle,
  Briefcase,
  Loader2,
  Sparkles,
  Star,
  TriangleAlert,
  X,
} from "lucide-react";

import {
  getMyApplications,
  withdrawApplication,
} from "../../services/jobSeekerApplicationService";

const STATUS_STEPS = ["UnderReview", "Shortlisted", "Interview", "Offer"];

const STATUS_LABELS = {
  UnderReview: "Under Review",
  Shortlisted: "Shortlisted",
  Interview: "Interview",
  Offer: "Offer",
  Rejected: "Rejected",
  Withdrawn: "Withdrawn",
};

const STATUS_STYLES = {
  UnderReview: "bg-amber-50 text-amber-600",
  Shortlisted: "bg-emerald-50 text-emerald-600",
  Interview: "bg-blue-50 text-blue-600",
  Offer: "bg-violet-50 text-violet-600",
  Rejected: "bg-red-50 text-red-600",
  Withdrawn: "bg-neutral-100 text-neutral-500",
};

function MyApplicationsPage() {
  const [applications, setApplications] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  const [withdrawing, setWithdrawing] = useState(null);
  const [isWithdrawing, setIsWithdrawing] = useState(false);

  useEffect(() => {
    let ignore = false;

    getMyApplications()
      .then((data) => {
        if (!ignore) {
          setApplications(data);
        }
      })
      .catch((err) => {
        if (!ignore) {
          setError(err.message || "Unable to load applications.");
        }
      })
      .finally(() => {
        if (!ignore) {
          setIsLoading(false);
        }
      });

    return () => {
      ignore = true;
    };
  }, []);

  const confirmWithdraw = async () => {
    if (!withdrawing) {
      return;
    }

    setIsWithdrawing(true);
    setError("");

    try {
      await withdrawApplication(withdrawing);

      setApplications((previousApplications) =>
        previousApplications.map((application) =>
          application.id === withdrawing
            ? { ...application, status: "Withdrawn" }
            : application,
        ),
      );

      setWithdrawing(null);
    } catch (err) {
      setError(err.message || "Unable to withdraw application.");
      setWithdrawing(null);
    } finally {
      setIsWithdrawing(false);
    }
  };

  return (
    <div>
      <div className="inline-flex items-center gap-2 rounded-full bg-violet-50 px-3 py-1.5 text-[11px] font-semibold text-violet-600">
        <Sparkles size={12} />
        MY APPLICATIONS
      </div>

      <h1 className="mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">
        Applications
      </h1>

      <p className="mt-2 text-neutral-500">
        Track your progress and see exactly what&apos;s helping — or hurting —
        your match score.
      </p>

      {error && (
        <div
          role="alert"
          className="mt-5 flex max-w-lg items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-600"
        >
          <AlertCircle size={17} className="mt-0.5 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {isLoading ? (
        <div className="mt-8 flex items-center gap-2 text-sm text-neutral-400">
          <Loader2 size={18} className="animate-spin" />
          Loading applications...
        </div>
      ) : (
        <div className="mt-8 space-y-5">
          {applications.map((application) => {
            // IMPORTANT:
            // Candidates may withdraw ONLY while the application
            // is still in the UnderReview stage.
            const isWithdrawable = application.status === "UnderReview";

            const currentIndex = STATUS_STEPS.indexOf(application.status);

            return (
              <div
                key={application.id}
                className="rounded-2xl border border-white/70 bg-white/75 p-6 shadow-xl shadow-neutral-200/30 backdrop-blur-2xl"
              >
                <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                  <div className="flex items-start gap-4">
                    {application.companyLogoUrl ? (
                      <img
                        src={application.companyLogoUrl}
                        alt={`${application.company} logo`}
                        className="h-11 w-11 shrink-0 rounded-2xl border border-neutral-100 bg-white object-contain p-1"
                      />
                    ) : (
                      <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-violet-100">
                        <Briefcase size={20} className="text-violet-600" />
                      </div>
                    )}

                    <div>
                      <p className="font-semibold text-neutral-900">
                        {application.jobTitle}
                      </p>

                      <p className="mt-0.5 text-sm text-neutral-500">
                        {application.company} · Applied{" "}
                        {new Date(application.appliedAt).toLocaleDateString()}
                      </p>
                    </div>
                  </div>

                  <div className="flex shrink-0 items-center gap-3">
                    <div className="flex items-center gap-1.5">
                      <Star
                        size={15}
                        className="fill-amber-400 text-amber-400"
                      />
                      <span className="text-sm font-semibold">
                        {application.matchScore}%
                      </span>
                    </div>

                    <span
                      className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                        STATUS_STYLES[application.status] ??
                        "bg-neutral-100 text-neutral-600"
                      }`}
                    >
                      {STATUS_LABELS[application.status] ?? application.status}
                    </span>
                  </div>
                </div>

                {/* PROGRESS TRACKER */}
                {currentIndex !== -1 && (
                  <div className="mt-5 flex items-center">
                    {STATUS_STEPS.map((step, index) => {
                      const isDone = index <= currentIndex;

                      return (
                        <div
                          key={step}
                          className="flex flex-1 items-center last:flex-none"
                        >
                          <div className="flex flex-col items-center gap-1.5">
                            <div
                              className={`h-3 w-3 rounded-full ${
                                isDone ? "bg-violet-600" : "bg-neutral-200"
                              }`}
                            />

                            <span
                              className={`text-[11px] font-medium ${
                                isDone
                                  ? "text-neutral-700"
                                  : "text-neutral-400"
                              }`}
                            >
                              {STATUS_LABELS[step]}
                            </span>
                          </div>

                          {index < STATUS_STEPS.length - 1 && (
                            <div
                              className={`mx-1.5 mb-4 h-0.5 flex-1 ${
                                index < currentIndex
                                  ? "bg-violet-600"
                                  : "bg-neutral-200"
                              }`}
                            />
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}

                {/* SKILL-GAP FEEDBACK */}
                <div className="mt-5 flex flex-wrap gap-1.5">
                  {(application.matchedSkills ?? []).map((skill) => (
                    <span
                      key={skill}
                      className="rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-600"
                    >
                      {skill}
                    </span>
                  ))}

                  {(application.gapSkills ?? []).map((skill) => (
                    <span
                      key={skill}
                      className="rounded-full bg-neutral-100 px-2 py-0.5 text-[11px] font-medium text-neutral-500"
                    >
                      Gap: {skill}
                    </span>
                  ))}
                </div>

                {/* WITHDRAW IS AVAILABLE ONLY FOR UNDER REVIEW */}
                {isWithdrawable && (
                  <div className="mt-5">
                    <button
                      type="button"
                      onClick={() => setWithdrawing(application.id)}
                      className="h-9 rounded-lg border border-red-200 px-4 text-xs font-semibold text-red-600 transition hover:bg-red-50"
                    >
                      Withdraw application
                    </button>
                  </div>
                )}
              </div>
            );
          })}

          {applications.length === 0 && (
            <div className="rounded-2xl border border-neutral-200 bg-white py-16 text-center text-sm text-neutral-400">
              You haven&apos;t applied to any jobs yet.
            </div>
          )}
        </div>
      )}

      {withdrawing && (
        <WithdrawConfirm
          isLoading={isWithdrawing}
          onConfirm={confirmWithdraw}
          onClose={() => {
            if (!isWithdrawing) {
              setWithdrawing(null);
            }
          }}
        />
      )}
    </div>
  );
}

function WithdrawConfirm({ isLoading, onConfirm, onClose }) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="withdraw-dialog-title"
    >
      <button
        type="button"
        aria-label="Close withdrawal confirmation"
        className="absolute inset-0 bg-black/30 backdrop-blur-sm"
        onClick={onClose}
        disabled={isLoading}
      />

      <div className="relative w-full max-w-sm rounded-3xl border border-white/70 bg-white p-6 shadow-2xl sm:p-7">
        <button
          type="button"
          onClick={onClose}
          disabled={isLoading}
          aria-label="Close"
          className="absolute right-5 top-5 text-neutral-400 transition hover:text-neutral-700 disabled:opacity-50"
        >
          <X size={18} />
        </button>

        <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-red-50">
          <TriangleAlert size={20} className="text-red-500" />
        </div>

        <h2
          id="withdraw-dialog-title"
          className="mt-4 text-lg font-semibold tracking-tight"
        >
          Withdraw this application?
        </h2>

        <p className="mt-1.5 text-sm text-neutral-500">
          You can withdraw only while your application is under review. This
          action cannot be undone, and you would need to reapply if you change
          your mind.
        </p>

        <div className="flex items-center gap-3 pt-6">
          <button
            type="button"
            onClick={onClose}
            disabled={isLoading}
            className="h-11 flex-1 rounded-xl border border-neutral-200 text-sm font-semibold text-neutral-600 transition hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Cancel
          </button>

          <button
            type="button"
            onClick={onConfirm}
            disabled={isLoading}
            className="flex h-11 flex-1 items-center justify-center gap-2 rounded-xl bg-red-600 text-sm font-semibold text-white transition hover:bg-red-700 active:scale-[0.99] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isLoading && <Loader2 size={15} className="animate-spin" />}
            {isLoading ? "Withdrawing..." : "Withdraw"}
          </button>
        </div>
      </div>
    </div>
  );
}

export default MyApplicationsPage;
