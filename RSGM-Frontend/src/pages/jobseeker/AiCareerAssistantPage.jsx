import { useEffect, useMemo, useState } from "react";
import {
  AlertCircle,
  BrainCircuit,
  CheckCircle2,
  ChevronRight,
  Loader2,
  RefreshCw,
  Send,
  ShieldCheck,
  Sparkles,
  Target,
  XCircle,
} from "lucide-react";

import {
  approveCareerWorkflow,
  getCareerWorkflows,
  rejectCareerWorkflow,
  reviseCareerWorkflow,
  startCareerWorkflow,
} from "../../services/jobSeekerAiCareerService";

const DEFAULT_OBJECTIVE =
  "Find jobs that fit my current profile, explain my skill gaps, and prepare the strongest recommendation for my approval.";

function AiCareerAssistantPage() {
  const [objective, setObjective] = useState(DEFAULT_OBJECTIVE);
  const [workflow, setWorkflow] = useState(null);
  const [history, setHistory] = useState([]);
  const [revision, setRevision] = useState("");
  const [comment, setComment] = useState("");
  const [busyAction, setBusyAction] = useState("");
  const [loadingHistory, setLoadingHistory] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let ignore = false;
    getCareerWorkflows()
      .then((items) => {
        if (!ignore) {
          setHistory(items);
          if (items.length > 0) setWorkflow(items[0]);
        }
      })
      .catch((requestError) => {
        if (!ignore) setError(requestError.message);
      })
      .finally(() => {
        if (!ignore) setLoadingHistory(false);
      });
    return () => {
      ignore = true;
    };
  }, []);

  const selectedMatch = useMemo(() => {
    if (!workflow?.jobMatches?.length) return null;
    return (
      workflow.jobMatches.find((item) => item.jobId === workflow.selectedJobId) ??
      workflow.jobMatches[0]
    );
  }, [workflow]);

  const runAction = async (name, action) => {
    setError("");
    setBusyAction(name);
    try {
      const result = await action();
      setWorkflow(result);
      setHistory((previous) => [
        result,
        ...previous.filter((item) => item.workflowId !== result.workflowId),
      ]);
      setRevision("");
      setComment("");
    } catch (requestError) {
      setError(requestError.message || "Request failed.");
    } finally {
      setBusyAction("");
    }
  };

  const handleStart = async (event) => {
    event.preventDefault();
    const trimmed = objective.trim();
    if (trimmed.length < 5) {
      setError("Please enter a clear career objective.");
      return;
    }
    await runAction("start", () => startCareerWorkflow(trimmed));
  };

  const awaitingApproval =
    workflow?.status === "AwaitingApproval" && workflow?.approvalStatus === "Pending";

  return (
    <div className="pb-10">
      <div className="inline-flex items-center gap-2 rounded-full bg-violet-50 px-3 py-1.5 text-[11px] font-semibold text-violet-600">
        <Sparkles size={12} /> AI CAREER ASSISTANT
      </div>

      <div className="mt-4 flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">
            Your agentic job search
          </h1>
          <p className="mt-2 max-w-2xl text-neutral-500">
            Hireon plans the task, analyses your recorded profile, ranks published jobs,
            validates the result, and stops before applying until you approve.
          </p>
        </div>
        <div className="flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs font-medium text-emerald-700">
          <ShieldCheck size={15} /> Human approval required before application
        </div>
      </div>

      {error && (
        <div className="mt-5 flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          <AlertCircle size={18} className="mt-0.5 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <form onSubmit={handleStart} className="mt-7 rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm">
        <label className="text-sm font-semibold text-neutral-900">Career objective</label>
        <textarea
          value={objective}
          onChange={(event) => setObjective(event.target.value)}
          maxLength={500}
          rows={3}
          className="mt-2 w-full resize-none rounded-xl border border-neutral-200 px-4 py-3 text-sm outline-none transition focus:border-violet-400 focus:ring-4 focus:ring-violet-100"
        />
        <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
          <span className="text-xs text-neutral-400">The AI can recommend, but it cannot submit without you.</span>
          <button
            type="submit"
            disabled={busyAction !== ""}
            className="inline-flex h-10 items-center gap-2 rounded-xl bg-neutral-900 px-4 text-sm font-semibold text-white transition hover:bg-neutral-800 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {busyAction === "start" ? <Loader2 size={16} className="animate-spin" /> : <BrainCircuit size={16} />}
            Start AI workflow
          </button>
        </div>
      </form>

      {loadingHistory ? (
        <div className="mt-8 flex items-center gap-2 text-sm text-neutral-400">
          <Loader2 size={18} className="animate-spin" /> Loading AI workflow history...
        </div>
      ) : workflow ? (
        <div className="mt-7 grid gap-6 xl:grid-cols-[1.45fr_0.75fr]">
          <div className="space-y-6">
            <WorkflowProgress workflow={workflow} />
            {workflow.profileAnalysis && <ProfileAnalysisCard profile={workflow.profileAnalysis} />}
            <JobMatchesCard workflow={workflow} />
            {workflow.careerAdvice && <CareerAdviceCard advice={workflow.careerAdvice} />}
            {awaitingApproval && selectedMatch && (
              <ApprovalCard
                match={selectedMatch}
                comment={comment}
                setComment={setComment}
                revision={revision}
                setRevision={setRevision}
                busyAction={busyAction}
                onApprove={() => runAction("approve", () => approveCareerWorkflow(workflow.workflowId, comment))}
                onReject={() => runAction("reject", () => rejectCareerWorkflow(workflow.workflowId, comment))}
                onRevise={() => {
                  if (revision.trim().length < 3) {
                    setError("Enter what you want the agents to revise.");
                    return;
                  }
                  runAction("revise", () => reviseCareerWorkflow(workflow.workflowId, revision.trim()));
                }}
              />
            )}
            <OutcomeCard workflow={workflow} />
          </div>

          <HistoryCard history={history} currentId={workflow.workflowId} onSelect={setWorkflow} />
        </div>
      ) : (
        <div className="mt-8 rounded-2xl border border-dashed border-neutral-300 bg-white/60 p-10 text-center">
          <BrainCircuit size={32} className="mx-auto text-violet-500" />
          <p className="mt-3 font-medium">No AI career workflow yet</p>
          <p className="mt-1 text-sm text-neutral-500">Start with the objective above.</p>
        </div>
      )}
    </div>
  );
}

function WorkflowProgress({ workflow }) {
  return (
    <section className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-sm font-semibold">Agent workflow</p>
          <p className="mt-1 text-xs text-neutral-500">Status: {workflow.status} · Current step: {workflow.currentStep}</p>
        </div>
        {workflow.validation?.valid ? (
          <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">
            <CheckCircle2 size={14} /> Deterministic validation passed
          </span>
        ) : (
          <span className="inline-flex items-center gap-1.5 rounded-full bg-red-50 px-3 py-1 text-xs font-semibold text-red-700">
            <XCircle size={14} /> Validation not passed
          </span>
        )}
      </div>
      <div className="mt-5 space-y-2">
        {(workflow.plan ?? []).map((step) => (
          <div key={`${step.order}-${step.agent}`} className="flex items-center gap-3 rounded-xl bg-neutral-50 px-3 py-2.5 text-sm">
            <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-violet-100 text-xs font-bold text-violet-700">{step.order}</span>
            <div className="min-w-0">
              <span className="font-medium text-neutral-900">{step.agent}</span>
              <span className="text-neutral-500"> — {step.action}</span>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function ProfileAnalysisCard({ profile }) {
  return (
    <section className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-2"><Target size={18} className="text-violet-600" /><h2 className="font-semibold">Profile Analysis Agent</h2></div>
      <p className="mt-3 text-sm leading-6 text-neutral-600">{profile.summary}</p>
      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <MiniList title="Strong skills" items={profile.strongSkills} />
        <MiniList title="Developing skills" items={profile.developingSkills} />
        <MiniList title="Profile strengths" items={profile.strengths} />
        <MiniList title="Recorded gaps" items={profile.profileGaps} />
      </div>
      <div className="mt-4 rounded-xl bg-violet-50 p-3 text-sm text-violet-800">
        <span className="font-semibold">Career direction:</span> {profile.primaryCareerArea} · {profile.experienceLevel}
      </div>
    </section>
  );
}

function JobMatchesCard({ workflow }) {
  return (
    <section className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm">
      <h2 className="font-semibold">Job Matching Agent</h2>
      <p className="mt-1 text-xs text-neutral-500">Scores are calculated deterministically from recorded skills and experience.</p>
      <div className="mt-4 space-y-3">
        {(workflow.jobMatches ?? []).map((match, index) => (
          <div key={match.jobId} className={`rounded-xl border p-4 ${match.jobId === workflow.selectedJobId ? "border-violet-300 bg-violet-50/50" : "border-neutral-200"}`}>
            <div className="flex items-start justify-between gap-4">
              <div><p className="font-semibold">{index + 1}. {match.title}</p><p className="text-sm text-neutral-500">{match.company}</p></div>
              <span className="rounded-lg bg-neutral-900 px-2.5 py-1 text-sm font-bold text-white">{match.matchScore}%</span>
            </div>
            <p className="mt-3 text-sm leading-6 text-neutral-600">{match.explanation}</p>
            <div className="mt-3 flex flex-wrap gap-2 text-xs">
              {(match.matchedSkills ?? []).map((skill) => <span key={`m-${skill}`} className="rounded-full bg-emerald-50 px-2.5 py-1 text-emerald-700">✓ {skill}</span>)}
              {(match.missingSkills ?? []).map((skill) => <span key={`g-${skill}`} className="rounded-full bg-amber-50 px-2.5 py-1 text-amber-700">Gap: {skill}</span>)}
            </div>
          </div>
        ))}
        {(!workflow.jobMatches || workflow.jobMatches.length === 0) && <p className="text-sm text-neutral-500">No eligible published jobs were available.</p>}
      </div>
    </section>
  );
}

function CareerAdviceCard({ advice }) {
  return (
    <section className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm">
      <h2 className="font-semibold">Career Coach Agent</h2>
      <p className="mt-3 text-sm leading-6 text-neutral-600">{advice.summary}</p>
      {advice.headlineSuggestion && <div className="mt-4 rounded-xl bg-neutral-50 p-3 text-sm"><span className="font-semibold">Suggested headline:</span> {advice.headlineSuggestion}</div>}
      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <MiniList title="Learning priorities" items={advice.learningPriorities} />
        <MiniList title="Application tips" items={advice.applicationTips} />
      </div>
    </section>
  );
}

function ApprovalCard({ match, comment, setComment, revision, setRevision, busyAction, onApprove, onReject, onRevise }) {
  return (
    <section className="rounded-2xl border-2 border-violet-300 bg-violet-50/60 p-5 shadow-sm">
      <div className="flex items-center gap-2 text-violet-800"><ShieldCheck size={20} /><h2 className="font-semibold">Human approval required</h2></div>
      <p className="mt-2 text-sm leading-6 text-neutral-700">
        The agents recommend <strong>{match.title}</strong> at <strong>{match.company}</strong> ({match.matchScore}% match). No application has been submitted yet.
      </p>
      <textarea value={comment} onChange={(e) => setComment(e.target.value)} maxLength={500} rows={2} placeholder="Optional approval/rejection note" className="mt-4 w-full resize-none rounded-xl border border-violet-200 bg-white px-3 py-2.5 text-sm outline-none focus:ring-4 focus:ring-violet-100" />
      <div className="mt-3 flex flex-wrap gap-2">
        <ActionButton label="Reject" icon={XCircle} busy={busyAction === "reject"} disabled={busyAction !== ""} onClick={onReject} secondary />
        <ActionButton label="Approve & Apply" icon={Send} busy={busyAction === "approve"} disabled={busyAction !== ""} onClick={onApprove} />
      </div>
      <div className="mt-5 border-t border-violet-200 pt-4">
        <label className="text-xs font-semibold uppercase tracking-wide text-violet-700">Request revision</label>
        <div className="mt-2 flex flex-col gap-2 sm:flex-row">
          <input value={revision} onChange={(e) => setRevision(e.target.value)} maxLength={500} placeholder="Example: Prefer remote jobs with lower experience requirements" className="h-10 flex-1 rounded-xl border border-violet-200 bg-white px-3 text-sm outline-none focus:ring-4 focus:ring-violet-100" />
          <button type="button" onClick={onRevise} disabled={busyAction !== ""} className="inline-flex h-10 items-center justify-center gap-2 rounded-xl border border-violet-300 bg-white px-4 text-sm font-semibold text-violet-700 hover:bg-violet-100 disabled:opacity-60">
            {busyAction === "revise" ? <Loader2 size={15} className="animate-spin" /> : <RefreshCw size={15} />} Revise
          </button>
        </div>
      </div>
    </section>
  );
}

function OutcomeCard({ workflow }) {
  if (workflow.status === "AwaitingApproval") return null;

  const success = workflow.status === "Completed" && workflow.approvalStatus === "Approved";
  const validationErrors = workflow.validation?.errors ?? [];
  const failedStep = (workflow.steps ?? []).find((step) => step.status === "Failed");

  return (
    <section className={`rounded-2xl border p-5 ${success ? "border-emerald-200 bg-emerald-50" : "border-neutral-200 bg-white"}`}>
      <div className="flex items-center gap-2 font-semibold">
        {success ? <CheckCircle2 size={18} className="text-emerald-600" /> : <AlertCircle size={18} className="text-neutral-500" />}
        Workflow outcome
      </div>

      <p className="mt-2 text-sm text-neutral-600">
        {success
          ? `Approved by you and submitted. Application ID: ${workflow.createdApplicationId}`
          : workflow.errorSummary || `Workflow status: ${workflow.status}. Approval: ${workflow.approvalStatus}.`}
      </p>

      {!success && failedStep && (
        <div className="mt-4 rounded-xl bg-amber-50 p-3 text-sm text-amber-900">
          <span className="font-semibold">Failed step:</span> {failedStep.agent} — {failedStep.action}
        </div>
      )}

      {!success && validationErrors.length > 0 && (
        <div className="mt-4 rounded-xl border border-red-200 bg-red-50 p-4">
          <p className="text-sm font-semibold text-red-800">Safety / validation details</p>
          <ul className="mt-2 space-y-1.5 text-sm text-red-700">
            {validationErrors.map((error, index) => (
              <li key={`${index}-${error}`} className="flex gap-2">
                <span aria-hidden="true">•</span>
                <span>{error}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}
function HistoryCard({ history, currentId, onSelect }) {
  return (
    <aside className="h-fit rounded-2xl border border-neutral-200 bg-white p-4 shadow-sm xl:sticky xl:top-6">
      <h2 className="font-semibold">Recent workflows</h2>
      <div className="mt-3 space-y-2">
        {history.map((item) => (
          <button key={item.workflowId} type="button" onClick={() => onSelect(item)} className={`w-full rounded-xl border p-3 text-left transition ${item.workflowId === currentId ? "border-violet-300 bg-violet-50" : "border-neutral-200 hover:bg-neutral-50"}`}>
            <div className="flex items-start justify-between gap-2">
              <span className="line-clamp-2 text-sm font-medium">{item.objective}</span><ChevronRight size={15} className="mt-0.5 shrink-0 text-neutral-400" />
            </div>
            <p className="mt-2 text-xs text-neutral-500">{item.status} · {new Date(item.startedAt).toLocaleString()}</p>
          </button>
        ))}
      </div>
    </aside>
  );
}

function MiniList({ title, items = [] }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wide text-neutral-500">{title}</p>
      <ul className="mt-2 space-y-1.5 text-sm text-neutral-700">
        {items.length > 0 ? items.map((item) => <li key={item} className="flex gap-2"><span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-violet-400" />{item}</li>) : <li className="text-neutral-400">None identified</li>}
      </ul>
    </div>
  );
}

function ActionButton({ label, icon: Icon, busy, disabled, onClick, secondary = false }) {
  return (
    <button type="button" onClick={onClick} disabled={disabled} className={`inline-flex h-10 items-center justify-center gap-2 rounded-xl px-4 text-sm font-semibold transition disabled:opacity-60 ${secondary ? "border border-neutral-300 bg-white text-neutral-700 hover:bg-neutral-100" : "bg-neutral-900 text-white hover:bg-neutral-800"}`}>
      {busy ? <Loader2 size={15} className="animate-spin" /> : <Icon size={15} />} {label}
    </button>
  );
}

export default AiCareerAssistantPage;
