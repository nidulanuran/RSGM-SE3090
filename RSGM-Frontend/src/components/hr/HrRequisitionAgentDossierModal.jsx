import { useState } from "react";
import {
  Sparkles,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  Clock,
  ShieldCheck,
  Cpu,
  Layers,
  Award,
  ChevronRight,
  X,
  Send,
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

  if (!isOpen || !workflow) return null;

  let outcome = null;
  if (workflow.finalOutcome) {
    try {
      outcome = JSON.parse(workflow.finalOutcome);
    } catch {
      outcome = null;
    }
  }

  const score = outcome?.ReadinessScore ?? outcome?.readinessScore ?? 0;
  const strengths = outcome?.Strengths ?? outcome?.strengths ?? [];
  const risks = outcome?.Risks ?? outcome?.risks ?? outcome?.IdentifiedRisks ?? [];
  const recommendedSkills = outcome?.RecommendedSkills ?? outcome?.recommendedSkills ?? outcome?.SuggestedSkills ?? [];
  const executiveSummary = outcome?.ExecutiveSummary ?? outcome?.executiveSummary ?? workflow.objective;
  const recommendation = outcome?.Recommendation ?? outcome?.recommendation ?? outcome?.RecommendationForApprover ?? "";
  const steps = workflow.steps || [];

  const isPendingApproval = workflow.status === "WaitingForApproval";

  const handleAction = async (decisionCode) => {
    if (!onDecision) return;
    setSubmitting(true);
    try {
      await onDecision(decisionCode, decisionComment);
      onClose();
    } finally {
      setSubmitting(false);
    }
  };

  const getScoreColor = (s) => {
    if (s >= 80) return "text-emerald-600 bg-emerald-50 border-emerald-200";
    if (s >= 50) return "text-amber-600 bg-amber-50 border-amber-200";
    return "text-red-600 bg-red-50 border-red-200";
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-fade-in">
      <div className="relative w-full max-w-4xl max-h-[90vh] overflow-y-auto rounded-3xl bg-white shadow-2xl border border-slate-200 flex flex-col">
        {/* Header */}
        <div className="sticky top-0 z-10 flex items-center justify-between px-6 py-4 bg-white/90 backdrop-blur-md border-b border-slate-100">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-10 h-10 rounded-2xl bg-indigo-50 text-indigo-600 border border-indigo-100 shadow-sm">
              <Cpu size={20} />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-indigo-100 text-indigo-700">
                  HR Function Agent
                </span>
                <span className="text-xs font-medium text-slate-500">
                  Job Posting & Requisition Specialist
                </span>
              </div>
              <h2 className="text-lg font-bold text-slate-900 mt-0.5">
                AI Readiness & Approval Dossier
              </h2>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-full transition-colors"
          >
            <X size={20} />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-6 flex-1">
          {/* Top Score Banner */}
          <div className="flex flex-col sm:flex-row items-center justify-between gap-6 p-6 rounded-2xl bg-gradient-to-br from-slate-900 to-indigo-950 text-white shadow-lg">
            <div className="space-y-2 max-w-lg">
              <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-white/10 text-xs text-indigo-200 font-medium">
                <Sparkles size={13} />
                Automated Readiness Assessment
              </div>
              <h3 className="text-xl font-bold leading-tight">
                {outcome?.PositionTitle || "Requisition Readiness"}
              </h3>
              <p className="text-sm text-slate-300 leading-relaxed">
                {executiveSummary}
              </p>
            </div>

            <div className="flex flex-col items-center justify-center p-4 rounded-2xl bg-white/10 backdrop-blur-md border border-white/10 min-w-[130px]">
              <span className="text-3xl font-extrabold tracking-tight text-emerald-400">
                {score}
                <span className="text-base font-normal text-slate-300">/100</span>
              </span>
              <span className="text-xs font-medium text-slate-300 mt-1 uppercase tracking-wider">
                Readiness Score
              </span>
            </div>
          </div>

          {/* Recommendations & Advisories */}
          {recommendation && (
            <div className="p-4 rounded-2xl border border-indigo-100 bg-indigo-50/70 text-indigo-900 flex items-start gap-3">
              <ShieldCheck size={20} className="text-indigo-600 shrink-0 mt-0.5" />
              <div>
                <p className="text-xs font-semibold uppercase tracking-wider text-indigo-700">
                  Agent Recommendation
                </p>
                <p className="text-sm font-medium mt-0.5">{recommendation}</p>
              </div>
            </div>
          )}

          {/* Strengths & Risks Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="p-5 rounded-2xl border border-slate-200 bg-slate-50/50 space-y-3">
              <div className="flex items-center gap-2 text-sm font-semibold text-emerald-700">
                <CheckCircle2 size={16} />
                <span>Verified Strengths ({strengths.length})</span>
              </div>
              <ul className="space-y-2">
                {strengths.map((str, idx) => (
                  <li key={idx} className="text-xs text-slate-700 flex items-start gap-2">
                    <span className="h-1.5 w-1.5 rounded-full bg-emerald-500 mt-1.5 shrink-0" />
                    <span>{str}</span>
                  </li>
                ))}
                {strengths.length === 0 && (
                  <li className="text-xs text-slate-400 italic">No specific strengths documented.</li>
                )}
              </ul>
            </div>

            <div className="p-5 rounded-2xl border border-slate-200 bg-slate-50/50 space-y-3">
              <div className="flex items-center gap-2 text-sm font-semibold text-amber-700">
                <AlertTriangle size={16} />
                <span>Risks & Advisories ({risks.length})</span>
              </div>
              <ul className="space-y-2">
                {risks.map((risk, idx) => (
                  <li key={idx} className="text-xs text-slate-700 flex items-start gap-2">
                    <span className="h-1.5 w-1.5 rounded-full bg-amber-500 mt-1.5 shrink-0" />
                    <span>{risk}</span>
                  </li>
                ))}
                {risks.length === 0 && (
                  <li className="text-xs text-slate-500 flex items-center gap-1.5">
                    <CheckCircle2 size={14} className="text-emerald-500" />
                    No critical risk flags identified.
                  </li>
                )}
              </ul>
            </div>
          </div>

          {/* Suggested Skills & Competencies */}
          {recommendedSkills.length > 0 && (
            <div className="p-5 rounded-2xl border border-slate-200 bg-white space-y-3 shadow-sm">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm font-semibold text-slate-900">
                  <Award size={16} className="text-indigo-600" />
                  <span>AI Recommended Competencies for Job Posting</span>
                </div>
                <span className="text-xs text-slate-400">
                  Auto-mapped from system skill catalog
                </span>
              </div>
              <div className="flex flex-wrap gap-2 pt-1">
                {recommendedSkills.map((sk, idx) => (
                  <div
                    key={idx}
                    className="flex items-center gap-2 px-3 py-1.5 rounded-xl border border-indigo-100 bg-indigo-50/50 text-indigo-900 text-xs font-medium"
                  >
                    <span>{sk.SkillName || sk.skillName}</span>
                    <span className="px-1.5 py-0.5 rounded-md bg-indigo-200/60 text-[10px] font-bold text-indigo-800">
                      W: {sk.Weight || sk.weight || 1.0}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Execution Steps Trace */}
          <div className="p-5 rounded-2xl border border-slate-200 bg-slate-50/40 space-y-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-sm font-semibold text-slate-900">
                <Layers size={16} className="text-slate-600" />
                <span>Allow-Listed Tool Execution Trace</span>
              </div>
              <span className="text-xs font-medium text-slate-500">
                Status: <span className="font-semibold text-slate-800">{workflow.status}</span>
              </span>
            </div>

            <div className="space-y-2">
              {steps.map((st) => (
                <div
                  key={st.id || st.stepNumber}
                  className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 p-3 rounded-xl border border-slate-200 bg-white text-xs shadow-xs"
                >
                  <div className="flex items-center gap-2.5">
                    <span className="flex items-center justify-center w-5 h-5 rounded-full bg-slate-100 text-slate-700 font-bold text-[10px]">
                      {st.stepNumber}
                    </span>
                    <span className="font-semibold text-slate-800 font-mono text-[11px]">
                      {st.toolName}
                    </span>
                    <span className="text-slate-400 hidden sm:inline">•</span>
                    <span className="text-slate-600 truncate max-w-sm">
                      {st.outputSummary || st.inputSummary}
                    </span>
                  </div>
                  <div className="flex items-center gap-2 shrink-0 self-end sm:self-auto">
                    <span className="text-[11px] text-slate-400 flex items-center gap-1 font-mono">
                      <Clock size={11} /> {st.durationMs}ms
                    </span>
                    <span
                      className={`px-2 py-0.5 rounded-md font-semibold text-[10px] ${
                        st.validationStatus === "Passed"
                          ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                          : st.validationStatus === "Warning"
                          ? "bg-amber-50 text-amber-700 border border-amber-200"
                          : "bg-red-50 text-red-700 border border-red-200"
                      }`}
                    >
                      {st.validationStatus}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Mandatory Human Approval Gate Controls (for HR Manager) */}
          {isHrManager && isPendingApproval && (
            <div className="p-5 rounded-2xl border-2 border-indigo-200 bg-indigo-50/40 space-y-4">
              <div className="flex items-center gap-2 text-sm font-bold text-indigo-950">
                <ShieldCheck size={18} className="text-indigo-600" />
                <span>Human-in-the-Loop Approval Gate</span>
              </div>
              <p className="text-xs text-indigo-900/80">
                In compliance with platform rules, high-impact actions require your explicit authorization.
                Select your decision below:
              </p>

              <textarea
                value={decisionComment}
                onChange={(e) => setDecisionComment(e.target.value)}
                placeholder="Optional feedback or decision rationale..."
                className="w-full text-xs p-3 rounded-xl border border-indigo-200 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 text-slate-800"
                rows={2}
              />

              <div className="flex flex-wrap items-center justify-end gap-2 pt-1">
                <button
                  type="button"
                  disabled={submitting}
                  onClick={() => handleAction(2)} // Rejected
                  className="px-4 py-2 rounded-xl border border-red-200 bg-red-50 text-red-700 text-xs font-semibold hover:bg-red-100 transition-colors"
                >
                  Reject Requisition
                </button>
                <button
                  type="button"
                  disabled={submitting}
                  onClick={() => handleAction(3)} // RevisionRequested
                  className="px-4 py-2 rounded-xl border border-amber-200 bg-amber-50 text-amber-800 text-xs font-semibold hover:bg-amber-100 transition-colors"
                >
                  Request Revision
                </button>
                <button
                  type="button"
                  disabled={submitting}
                  onClick={() => handleAction(1)} // Approved
                  className="px-5 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-semibold shadow-sm transition-colors flex items-center gap-1.5"
                >
                  <CheckCircle2 size={14} />
                  Authorize & Approve
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 bg-slate-50 border-t border-slate-100 flex items-center justify-end">
          <button
            onClick={onClose}
            className="px-5 py-2 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-xl hover:bg-slate-100 transition-colors shadow-xs"
          >
            Close Dossier
          </button>
        </div>
      </div>
    </div>
  );
}
