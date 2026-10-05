import { useState } from "react";
import {
  Sparkles,
  CheckCircle2,
  AlertTriangle,
  Clock,
  ShieldCheck,
  Cpu,
  Layers,
  Award,
  X,
} from "lucide-react";

export default function HrRequisitionAgentDossierModal({
  isOpen,
  onClose,
  workflow,
  onDecision,
  isHrManager = false,
}) {
  const [decisionComment, setDecisionComment] = useState("");
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen || !workflow) {
    return null;
  }

  let outcome = null;

  if (workflow.finalOutcome) {
    try {
      outcome = JSON.parse(workflow.finalOutcome);
    } catch {
      outcome = null;
    }
  }

  const score =
    outcome?.ReadinessScore ??
    outcome?.readinessScore ??
    0;

  const strengths =
    outcome?.Strengths ??
    outcome?.strengths ??
    [];

  const risks =
    outcome?.Risks ??
    outcome?.risks ??
    outcome?.IdentifiedRisks ??
    [];

  const recommendedSkills =
    outcome?.RecommendedSkills ??
    outcome?.recommendedSkills ??
    outcome?.SuggestedSkills ??
    [];

  const executiveSummary =
    outcome?.ExecutiveSummary ??
    outcome?.executiveSummary ??
    workflow.objective;

  const recommendation =
    outcome?.Recommendation ??
    outcome?.recommendation ??
    outcome?.RecommendationForApprover ??
    "";

  const steps = workflow.steps || [];

  const isPendingApproval =
    workflow.status === "WaitingForApproval";

  const handleAction = async (decisionCode) => {
    if (!onDecision) {
      return;
    }

    setSubmitting(true);

    try {
      await onDecision(
        decisionCode,
        decisionComment
      );

      onClose();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm animate-fade-in">
      <div className="relative flex max-h-[90vh] w-full max-w-4xl flex-col overflow-y-auto rounded-3xl border border-slate-200 bg-white shadow-2xl">
        <div className="sticky top-0 z-10 flex items-center justify-between border-b border-slate-100 bg-white/90 px-6 py-4 backdrop-blur-md">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-2xl border border-indigo-100 bg-indigo-50 text-indigo-600 shadow-sm">
              <Cpu size={20} />
            </div>

            <div>
              <div className="flex items-center gap-2">
                <span className="rounded-full bg-indigo-100 px-2.5 py-0.5 text-xs font-semibold text-indigo-700">
                  HR Function Agent
                </span>

                <span className="text-xs font-medium text-slate-500">
                  Job Posting & Requisition Specialist
                </span>
              </div>

              <h2 className="mt-0.5 text-lg font-bold text-slate-900">
                AI Readiness & Approval Dossier
              </h2>
            </div>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="rounded-full p-2 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
          >
            <X size={20} />
          </button>
        </div>

        <div className="flex-1 space-y-6 p-6">
          <div className="flex flex-col items-center justify-between gap-6 rounded-2xl bg-linear-to-br from-slate-900 to-indigo-950 p-6 text-white shadow-lg sm:flex-row">
            <div className="max-w-lg space-y-2">
              <div className="inline-flex items-center gap-1.5 rounded-full bg-white/10 px-3 py-1 text-xs font-medium text-indigo-200">
                <Sparkles size={13} />
                Automated Readiness Assessment
              </div>

              <h3 className="text-xl font-bold leading-tight">
                {outcome?.PositionTitle ||
                  "Requisition Readiness"}
              </h3>

              <p className="text-sm leading-relaxed text-slate-300">
                {executiveSummary}
              </p>
            </div>

            <div className="flex min-w-32.5 flex-col items-center justify-center rounded-2xl border border-white/10 bg-white/10 p-4 backdrop-blur-md">
              <span className="text-3xl font-extrabold tracking-tight text-emerald-400">
                {score}
                <span className="text-base font-normal text-slate-300">
                  /100
                </span>
              </span>

              <span className="mt-1 text-xs font-medium uppercase tracking-wider text-slate-300">
                Readiness Score
              </span>
            </div>
          </div>

          {recommendation && (
            <div className="flex items-start gap-3 rounded-2xl border border-indigo-100 bg-indigo-50/70 p-4 text-indigo-900">
              <ShieldCheck
                size={20}
                className="mt-0.5 shrink-0 text-indigo-600"
              />

              <div>
                <p className="text-xs font-semibold uppercase tracking-wider text-indigo-700">
                  Agent Recommendation
                </p>

                <p className="mt-0.5 text-sm font-medium">
                  {recommendation}
                </p>
              </div>
            </div>
          )}

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div className="space-y-3 rounded-2xl border border-slate-200 bg-slate-50/50 p-5">
              <div className="flex items-center gap-2 text-sm font-semibold text-emerald-700">
                <CheckCircle2 size={16} />
                <span>
                  Verified Strengths ({strengths.length})
                </span>
              </div>

              <ul className="space-y-2">
                {strengths.map((strength, index) => (
                  <li
                    key={index}
                    className="flex items-start gap-2 text-xs text-slate-700"
                  >
                    <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-emerald-500" />
                    <span>{strength}</span>
                  </li>
                ))}

                {strengths.length === 0 && (
                  <li className="text-xs italic text-slate-400">
                    No specific strengths documented.
                  </li>
                )}
              </ul>
            </div>

            <div className="space-y-3 rounded-2xl border border-slate-200 bg-slate-50/50 p-5">
              <div className="flex items-center gap-2 text-sm font-semibold text-amber-700">
                <AlertTriangle size={16} />

                <span>
                  Risks & Advisories ({risks.length})
                </span>
              </div>

              <ul className="space-y-2">
                {risks.map((risk, index) => (
                  <li
                    key={index}
                    className="flex items-start gap-2 text-xs text-slate-700"
                  >
                    <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-amber-500" />
                    <span>{risk}</span>
                  </li>
                ))}

                {risks.length === 0 && (
                  <li className="flex items-center gap-1.5 text-xs text-slate-500">
                    <CheckCircle2
                      size={14}
                      className="text-emerald-500"
                    />
                    No critical risk flags identified.
                  </li>
                )}
              </ul>
            </div>
          </div>

          {recommendedSkills.length > 0 && (
            <div className="space-y-3 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm font-semibold text-slate-900">
                  <Award
                    size={16}
                    className="text-indigo-600"
                  />

                  <span>
                    AI Recommended Competencies for Job Posting
                  </span>
                </div>

                <span className="text-xs text-slate-400">
                  Auto-mapped from system skill catalog
                </span>
              </div>

              <div className="flex flex-wrap gap-2 pt-1">
                {recommendedSkills.map(
                  (skill, index) => (
                    <div
                      key={index}
                      className="flex items-center gap-2 rounded-xl border border-indigo-100 bg-indigo-50/50 px-3 py-1.5 text-xs font-medium text-indigo-900"
                    >
                      <span>
                        {skill.SkillName ||
                          skill.skillName}
                      </span>

                      <span className="rounded-md bg-indigo-200/60 px-1.5 py-0.5 text-[10px] font-bold text-indigo-800">
                        W:{" "}
                        {skill.Weight ??
                          skill.weight ??
                          1.0}
                      </span>
                    </div>
                  )
                )}
              </div>
            </div>
          )}

          <div className="space-y-3 rounded-2xl border border-slate-200 bg-slate-50/40 p-5">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-sm font-semibold text-slate-900">
                <Layers
                  size={16}
                  className="text-slate-600"
                />

                <span>
                  Allow-Listed Tool Execution Trace
                </span>
              </div>

              <span className="text-xs font-medium text-slate-500">
                Status:{" "}
                <span className="font-semibold text-slate-800">
                  {workflow.status}
                </span>
              </span>
            </div>

            <div className="space-y-2">
              {steps.map((step) => (
                <div
                  key={
                    step.id ||
                    step.stepNumber
                  }
                  className="flex flex-col justify-between gap-2 rounded-xl border border-slate-200 bg-white p-3 text-xs shadow-xs sm:flex-row sm:items-center"
                >
                  <div className="flex items-center gap-2.5">
                    <span className="flex h-5 w-5 items-center justify-center rounded-full bg-slate-100 text-[10px] font-bold text-slate-700">
                      {step.stepNumber}
                    </span>

                    <span className="font-mono text-[11px] font-semibold text-slate-800">
                      {step.toolName}
                    </span>

                    <span className="hidden text-slate-400 sm:inline">
                      •
                    </span>

                    <span className="max-w-sm truncate text-slate-600">
                      {step.outputSummary ||
                        step.inputSummary}
                    </span>
                  </div>

                  <div className="flex shrink-0 items-center gap-2 self-end sm:self-auto">
                    <span className="flex items-center gap-1 font-mono text-[11px] text-slate-400">
                      <Clock size={11} />
                      {step.durationMs}ms
                    </span>

                    <span
                      className={`rounded-md px-2 py-0.5 text-[10px] font-semibold ${
                        step.validationStatus ===
                        "Passed"
                          ? "border border-emerald-200 bg-emerald-50 text-emerald-700"
                          : step.validationStatus ===
                              "Warning"
                            ? "border border-amber-200 bg-amber-50 text-amber-700"
                            : "border border-red-200 bg-red-50 text-red-700"
                      }`}
                    >
                      {step.validationStatus}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {isHrManager &&
            isPendingApproval && (
              <div className="space-y-4 rounded-2xl border-2 border-indigo-200 bg-indigo-50/40 p-5">
                <div className="flex items-center gap-2 text-sm font-bold text-indigo-950">
                  <ShieldCheck
                    size={18}
                    className="text-indigo-600"
                  />

                  <span>
                    Human-in-the-Loop Approval Gate
                  </span>
                </div>

                <p className="text-xs text-indigo-900/80">
                  In compliance with platform rules,
                  high-impact actions require your explicit
                  authorization. Select your decision below:
                </p>

                <textarea
                  value={decisionComment}
                  onChange={(e) =>
                    setDecisionComment(
                      e.target.value
                    )
                  }
                  placeholder="Optional feedback or decision rationale..."
                  className="w-full rounded-xl border border-indigo-200 bg-white p-3 text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  rows={2}
                />

                <div className="flex flex-wrap items-center justify-end gap-2 pt-1">
                  <button
                    type="button"
                    disabled={submitting}
                    onClick={() =>
                      handleAction(2)
                    }
                    className="rounded-xl border border-red-200 bg-red-50 px-4 py-2 text-xs font-semibold text-red-700 transition-colors hover:bg-red-100 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Reject Requisition
                  </button>

                  <button
                    type="button"
                    disabled={submitting}
                    onClick={() =>
                      handleAction(3)
                    }
                    className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-2 text-xs font-semibold text-amber-800 transition-colors hover:bg-amber-100 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Request Revision
                  </button>

                  <button
                    type="button"
                    disabled={submitting}
                    onClick={() =>
                      handleAction(1)
                    }
                    className="flex items-center gap-1.5 rounded-xl bg-emerald-600 px-5 py-2 text-xs font-semibold text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    <CheckCircle2 size={14} />
                    Authorize & Approve
                  </button>
                </div>
              </div>
            )}
        </div>

        <div className="flex items-center justify-end border-t border-slate-100 bg-slate-50 px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            className="rounded-xl border border-slate-200 bg-white px-5 py-2 text-xs font-semibold text-slate-700 shadow-xs transition-colors hover:bg-slate-100"
          >
            Close Dossier
          </button>
        </div>
      </div>
    </div>
  );
}
