import { useEffect, useMemo, useState } from "react";
import {
  AlertCircle,
  CheckCircle2,
  Clock3,
  Loader2,
  ShieldCheck,
  Sparkles,
  UserRoundCheck,
  XCircle,
} from "lucide-react";

import { getRecruiterJobPostings } from "../../services/recruiterJobPostingService";

import {
  getSkillMatchingAgentPanelists,
  startSkillMatchingAgent,
  approveSkillMatchingAgentWorkflow,
  rejectSkillMatchingAgentWorkflow,
} from "../../services/recruiterSkillMatchingAgentService";

const surface =
  "rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30";

export default function SkillMatchingShortlistingAgentPage() {
  const [jobs, setJobs] = useState([]);
  const [panelists, setPanelists] = useState([]);

  const [jobId, setJobId] = useState("");
  const [panelistId, setPanelistId] = useState("");

  const [objective, setObjective] = useState("");

  const [workflow, setWorkflow] = useState(null);

  const [comment, setComment] = useState("");

  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [deciding, setDeciding] = useState(false);

  const [error, setError] = useState("");

  // ------------------------------------------------------------
  // Initial data loading
  // ------------------------------------------------------------

  useEffect(() => {
    let alive = true;

    async function loadInitialData() {
      setLoading(true);
      setError("");

      try {
        const [jobData, panelistData] =
          await Promise.all([
            getRecruiterJobPostings(),
            getSkillMatchingAgentPanelists(),
          ]);

        if (!alive) {
          return;
        }

        const publishedJobs = (
          jobData || []
        ).filter(
          (job) =>
            job.status === "Published" ||
            job.status === 1
        );

        setJobs(publishedJobs);
        setPanelists(panelistData || []);

        if (publishedJobs.length > 0) {
          setJobId(publishedJobs[0].id);
        }

        if ((panelistData || []).length > 0) {
          setPanelistId(panelistData[0].id);
        }
      } catch (requestError) {
        if (alive) {
          setError(
            requestError.message ||
              "Unable to load published jobs and hiring panelists."
          );
        }
      } finally {
        if (alive) {
          setLoading(false);
        }
      }
    }

    loadInitialData();

    return () => {
      alive = false;
    };
  }, []);

  // ------------------------------------------------------------
  // Selected job
  // ------------------------------------------------------------

  const selectedJob = useMemo(
    () =>
      jobs.find(
        (job) => job.id === jobId
      ),
    [jobs, jobId]
  );

  // ------------------------------------------------------------
  // Start Agent
  // ------------------------------------------------------------

  async function runAgent() {
    if (!jobId) {
      setError(
        "Please select a published job."
      );
      return;
    }

    if (!panelistId) {
      setError(
        "Please select a hiring panelist."
      );
      return;
    }

    setRunning(true);
    setError("");
    setWorkflow(null);
    setComment("");

    try {
      const result =
        await startSkillMatchingAgent({
          jobId,
          panelistId,
          objective:
            objective.trim() || null,
        });

      setWorkflow(result);
    } catch (requestError) {
      setError(
        requestError.message ||
          "Unable to start the AI shortlisting workflow."
      );
    } finally {
      setRunning(false);
    }
  }

  // ------------------------------------------------------------
  // Approve
  // ------------------------------------------------------------

  async function approve() {
    if (!workflow?.id) {
      return;
    }

    setDeciding(true);
    setError("");

    try {
      const result =
        await approveSkillMatchingAgentWorkflow(
          workflow.id,
          comment.trim()
        );

      setWorkflow(result);
    } catch (requestError) {
      setError(
        requestError.message ||
          "Unable to approve the AI shortlist."
      );
    } finally {
      setDeciding(false);
    }
  }

  // ------------------------------------------------------------
  // Reject
  // ------------------------------------------------------------

  async function reject() {
    if (!workflow?.id) {
      return;
    }

    setDeciding(true);
    setError("");

    try {
      const result =
        await rejectSkillMatchingAgentWorkflow(
          workflow.id,
          comment.trim() ||
            "Recruiter rejected the AI recommendation."
        );

      setWorkflow(result);
    } catch (requestError) {
      setError(
        requestError.message ||
          "Unable to reject the AI shortlist."
      );
    } finally {
      setDeciding(false);
    }
  }

  // ------------------------------------------------------------
  // Render
  // ------------------------------------------------------------

  return (
    <div>
      {/* ----------------------------------------------------- */}
      {/* Header */}
      {/* ----------------------------------------------------- */}

      <div className="inline-flex items-center gap-2 rounded-full bg-blue-50 px-3 py-1.5 text-[11px] font-semibold text-blue-600">
        <Sparkles size={12} />
        AI SKILL MATCHING
      </div>

      <div className="mt-4 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight text-neutral-900 sm:text-4xl">
            AI Shortlisting Agent
          </h1>

          <p className="mt-2 max-w-3xl text-sm leading-6 text-neutral-500">
            Run the controlled multi-agent skill matching
            workflow, review the evidence, and approve the
            final shortlist before it is sent to the Hiring
            Panelist.
          </p>
        </div>

        <div className="inline-flex items-center gap-2 rounded-xl border border-emerald-100 bg-emerald-50 px-4 py-3 text-xs font-semibold text-emerald-700">
          <ShieldCheck size={15} />
          Human approval required
        </div>
      </div>

      {/* ----------------------------------------------------- */}
      {/* Error */}
      {/* ----------------------------------------------------- */}

      {error && (
        <div className="mt-6 flex gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-600">
          <AlertCircle
            size={17}
            className="mt-0.5 shrink-0"
          />

          <span>{error}</span>
        </div>
      )}

      {/* ----------------------------------------------------- */}
      {/* Agent configuration */}
      {/* ----------------------------------------------------- */}

      <section
        className={`${surface} mt-8 p-5 sm:p-6`}
      >
        <div className="flex items-center gap-2">
          <Sparkles
            size={18}
            className="text-blue-600"
          />

          <div>
            <h2 className="text-lg font-semibold text-neutral-900">
              Start AI Shortlisting
            </h2>

            <p className="mt-1 text-xs text-neutral-500">
              Select the job and panelist for this
              shortlisting workflow.
            </p>
          </div>
        </div>

        <div className="mt-6 grid gap-5 lg:grid-cols-2">
          {/* Job */}
          <label className="text-sm font-medium text-neutral-700">
            Published Job

            <select
              value={jobId}
              onChange={(event) => {
                setJobId(event.target.value);
                setWorkflow(null);
                setError("");
              }}
              disabled={loading || running}
              className="mt-2 h-11 w-full rounded-xl border border-neutral-200 bg-white px-3 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100 disabled:cursor-not-allowed disabled:bg-neutral-50"
            >
              <option value="">
                Select a published job
              </option>

              {jobs.map((job) => (
                <option
                  key={job.id}
                  value={job.id}
                >
                  {job.title}
                </option>
              ))}
            </select>
          </label>

          {/* Panelist */}
          <label className="text-sm font-medium text-neutral-700">
            Hiring Panelist

            <select
              value={panelistId}
              onChange={(event) =>
                setPanelistId(
                  event.target.value
                )
              }
              disabled={loading || running}
              className="mt-2 h-11 w-full rounded-xl border border-neutral-200 bg-white px-3 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100 disabled:cursor-not-allowed disabled:bg-neutral-50"
            >
              <option value="">
                Select a hiring panelist
              </option>

              {panelists.map((panelist) => (
                <option
                  key={panelist.id}
                  value={panelist.id}
                >
                  {panelist.name}
                </option>
              ))}
            </select>
          </label>
        </div>

        {/* Objective */}
        <label className="mt-5 block text-sm font-medium text-neutral-700">
          Workflow Objective

          <span className="ml-1 font-normal text-neutral-400">
            optional
          </span>

          <textarea
            value={objective}
            onChange={(event) =>
              setObjective(
                event.target.value
              )
            }
            maxLength={500}
            rows={3}
            placeholder="Example: identify the strongest candidates for this position using weighted skills and candidate proficiency."
            className="mt-2 w-full rounded-xl border border-neutral-200 bg-white p-3 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100"
          />

          <span className="mt-1 block text-right text-[11px] text-neutral-400">
            {objective.length}/500
          </span>
        </label>

        {/* Selected job information */}
        {selectedJob && (
          <div className="mt-5 rounded-xl border border-blue-100 bg-blue-50 p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-blue-600">
              Selected position
            </p>

            <p className="mt-1 text-sm font-semibold text-blue-900">
              {selectedJob.title}
            </p>

            <p className="mt-1 text-xs text-blue-700">
              The Agent will analyse eligible applications
              for this published position.
            </p>
          </div>
        )}

        {/* Start button */}
        <div className="mt-6 flex justify-end">
          <button
            type="button"
            onClick={runAgent}
            disabled={
              loading ||
              running ||
              !jobId ||
              !panelistId
            }
            className="inline-flex h-11 items-center justify-center gap-2 rounded-xl bg-neutral-900 px-6 text-sm font-semibold text-white transition hover:bg-neutral-800 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {running ? (
              <Loader2
                size={16}
                className="animate-spin"
              />
            ) : (
              <Sparkles size={16} />
            )}

            {running
              ? "Running Agent..."
              : "Run AI Shortlisting"}
          </button>
        </div>
      </section>

      {/* ----------------------------------------------------- */}
      {/* Workflow result */}
      {/* ----------------------------------------------------- */}

      {workflow && (
        <div className="mt-8 space-y-6">
          <WorkflowOverview
            workflow={workflow}
          />

          <CandidateRecommendations
            workflow={workflow}
          />

          {workflow.status ===
            "WaitingForApproval" && (
            <ApprovalPanel
              comment={comment}
              setComment={setComment}
              deciding={deciding}
              approve={approve}
              reject={reject}
            />
          )}

          <WorkflowCompletion
            workflow={workflow}
          />
        </div>
      )}
    </div>
  );
}

/* ========================================================= */
/* Workflow Overview                                          */
/* ========================================================= */

function WorkflowOverview({
  workflow,
}) {
  return (
    <section
      className={`${surface} p-5 sm:p-6`}
    >
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-wide text-neutral-400">
            Workflow status
          </p>

          <div className="mt-2 flex items-center gap-3">
            <StatusBadge
              status={workflow.status}
            />

            <h2 className="text-xl font-semibold text-neutral-900">
              {workflow.jobTitle}
            </h2>
          </div>
        </div>

        <div className="inline-flex items-center gap-2 rounded-full bg-neutral-100 px-3 py-1.5 text-xs font-semibold text-neutral-600">
          <Clock3 size={13} />

          {workflow.steps?.length || 0} execution records
        </div>
      </div>

      {/* Plan */}
      <div className="mt-6">
        <div className="mb-3 flex items-center gap-2">
          <div className="h-2 w-2 rounded-full bg-blue-600" />

          <h3 className="text-sm font-semibold text-neutral-800">
            Agent execution plan
          </h3>
        </div>

        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {(workflow.plan || []).map(
            (step) => (
              <div
                key={`${step.step}-${step.agent}`}
                className="rounded-xl border border-neutral-200 bg-neutral-50 p-4"
              >
                <div className="text-xs font-semibold text-blue-600">
                  Step {step.step}
                </div>

                <div className="mt-2 text-sm font-semibold text-neutral-800">
                  {step.agent}
                </div>

                <div className="mt-1 text-xs leading-5 text-neutral-500">
                  {step.action}
                </div>
              </div>
            )
          )}
        </div>
      </div>

      {/* Execution records */}
      <div className="mt-6">
        <h3 className="mb-3 text-sm font-semibold text-neutral-800">
          Execution records
        </h3>

        <div className="space-y-2">
          {(workflow.steps || []).map(
            (step) => (
              <div
                key={step.id}
                className="rounded-xl border border-neutral-200 bg-white px-4 py-3"
              >
                <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <p className="text-sm font-semibold text-neutral-800">
                      {step.stepNumber}.{" "}
                      {step.agentName}
                    </p>

                    <p className="mt-1 text-xs leading-5 text-neutral-500">
                      {step.outputSummary ||
                        step.errorMessage ||
                        "Processing..."}
                    </p>
                  </div>

                  <StepStatus
                    status={step.status}
                  />
                </div>
              </div>
            )
          )}
        </div>
      </div>
    </section>
  );
}

/* ========================================================= */
/* Candidate Recommendations                                  */
/* ========================================================= */

function CandidateRecommendations({
  workflow,
}) {
  return (
    <section
      className={`${surface} p-5 sm:p-6`}
    >
      <div className="flex items-center gap-2">
        <UserRoundCheck
          size={18}
          className="text-blue-600"
        />

        <h2 className="text-xl font-semibold text-neutral-900">
          AI-recommended shortlist
        </h2>
      </div>

      <p className="mt-2 text-sm leading-6 text-neutral-500">
        Candidate scores are calculated by deterministic
        skill matching. Groq provides structured
        explanations and recommendation text.
      </p>

      {(workflow.candidates || []).length ===
        0 && (
        <div className="mt-5 rounded-xl border border-dashed border-neutral-300 bg-neutral-50 p-6 text-center">
          <p className="text-sm font-medium text-neutral-700">
            No candidate recommendations were returned.
          </p>

          <p className="mt-1 text-xs text-neutral-500">
            Check the workflow execution records and
            backend validation result.
          </p>
        </div>
      )}

      <div className="mt-5 space-y-4">
        {(workflow.candidates || []).map(
          (candidate, index) => (
            <CandidateCard
              key={candidate.applicationId}
              candidate={candidate}
              rank={index + 1}
            />
          )
        )}
      </div>
    </section>
  );
}

/* ========================================================= */
/* Candidate Card                                             */
/* ========================================================= */

function CandidateCard({
  candidate,
  rank,
}) {
  return (
    <article className="rounded-xl border border-neutral-200 bg-white p-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="flex gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-blue-50 text-sm font-bold text-blue-700">
            #{rank}
          </div>

          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="font-semibold text-neutral-900">
                {candidate.candidateName}
              </h3>

              <span className="rounded-full bg-blue-50 px-2.5 py-1 text-[11px] font-semibold text-blue-700">
                {candidate.matchScore}%
              </span>

              <span className="rounded-full bg-neutral-100 px-2.5 py-1 text-[11px] font-medium text-neutral-600">
                {candidate.recommendation}
              </span>
            </div>

            <p className="mt-2 text-sm leading-6 text-neutral-600">
              {candidate.explanation}
            </p>
          </div>
        </div>

        <div className="rounded-xl bg-neutral-50 px-4 py-3 text-right">
          <p className="text-[10px] font-semibold uppercase tracking-wide text-neutral-400">
            Weighted score
          </p>

          <p className="mt-1 text-2xl font-bold text-blue-700">
            {candidate.matchScore}%
          </p>
        </div>
      </div>

      <div className="mt-5 grid gap-4 lg:grid-cols-2">
        <SkillList
          title="Matched skills"
          items={candidate.matchedSkills}
          positive
        />

        <SkillList
          title="Skill gaps"
          items={candidate.missingSkills}
        />
      </div>

      <div className="mt-4 grid gap-4 lg:grid-cols-2">
        <SkillList
          title="AI strengths"
          items={candidate.strengths}
          positive
        />

        <SkillList
          title="AI gaps"
          items={candidate.gaps}
        />
      </div>
    </article>
  );
}

/* ========================================================= */
/* Skill List                                                  */
/* ========================================================= */

function SkillList({
  title,
  items = [],
  positive = false,
}) {
  return (
    <div className="rounded-xl border border-neutral-200 p-4">
      <p className="text-xs font-semibold uppercase tracking-wide text-neutral-400">
        {title}
      </p>

      <div className="mt-3 flex flex-wrap gap-1.5">
        {items.length === 0 ? (
          <span className="text-xs text-neutral-400">
            None recorded.
          </span>
        ) : (
          items.map((item) => (
            <span
              key={item}
              className={`rounded-full px-2.5 py-1 text-[11px] font-medium ${
                positive
                  ? "bg-emerald-50 text-emerald-700"
                  : "bg-red-50 text-red-600"
              }`}
            >
              {item}
            </span>
          ))
        )}
      </div>
    </div>
  );
}

/* ========================================================= */
/* Approval Panel                                              */
/* ========================================================= */

function ApprovalPanel({
  comment,
  setComment,
  deciding,
  approve,
  reject,
}) {
  return (
    <section
      className={`${surface} border-blue-100 p-5 sm:p-6`}
    >
      <div className="rounded-xl border border-blue-100 bg-blue-50 p-4">
        <div className="flex gap-3">
          <ShieldCheck
            size={19}
            className="mt-0.5 shrink-0 text-blue-700"
          />

          <div>
            <h2 className="font-semibold text-blue-900">
              Human approval required
            </h2>

            <p className="mt-1 text-sm leading-6 text-blue-800">
              The Agent has prepared the shortlist,
              but no candidate has been dispatched to
              the Hiring Panelist yet.
            </p>
          </div>
        </div>
      </div>

      <textarea
        value={comment}
        onChange={(event) =>
          setComment(event.target.value)
        }
        maxLength={1000}
        rows={3}
        placeholder="Optional recruiter decision comment"
        className="mt-4 w-full rounded-xl border border-neutral-200 bg-white p-3 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100"
      />

      <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={reject}
          disabled={deciding}
          className="inline-flex h-11 items-center justify-center gap-2 rounded-xl border border-red-200 bg-white px-5 text-sm font-semibold text-red-600 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {deciding ? (
            <Loader2
              size={16}
              className="animate-spin"
            />
          ) : (
            <XCircle size={16} />
          )}

          Reject Recommendation
        </button>

        <button
          type="button"
          onClick={approve}
          disabled={deciding}
          className="inline-flex h-11 items-center justify-center gap-2 rounded-xl bg-neutral-900 px-5 text-sm font-semibold text-white transition hover:bg-neutral-800 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {deciding ? (
            <Loader2
              size={16}
              className="animate-spin"
            />
          ) : (
            <CheckCircle2 size={16} />
          )}

          Approve & Send to Panelist
        </button>
      </div>
    </section>
  );
}

/* ========================================================= */
/* Completion                                                  */
/* ========================================================= */

function WorkflowCompletion({
  workflow,
}) {
  const terminalStatuses = [
    "Completed",
    "Rejected",
    "SafelyFailed",
    "FailedValidation",
    "TimedOut",
  ];

  if (
    !terminalStatuses.includes(
      workflow.status
    )
  ) {
    return null;
  }

  const successful =
    workflow.status === "Completed";

  return (
    <section
      className={`${surface} p-5 sm:p-6`}
    >
      <div className="flex items-start gap-3">
        {successful ? (
          <CheckCircle2
            size={22}
            className="mt-0.5 text-emerald-600"
          />
        ) : (
          <XCircle
            size={22}
            className="mt-0.5 text-red-600"
          />
        )}

        <div>
          <h2 className="font-semibold text-neutral-900">
            {successful
              ? "Approved shortlist dispatched"
              : workflow.status}
          </h2>

          <p className="mt-1 text-sm leading-6 text-neutral-500">
            {workflow.decisionComment ||
              workflow.errorMessage ||
              "No further action is required."}
          </p>

          {workflow.dispatchId && (
            <p className="mt-2 text-xs text-neutral-400">
              Dispatch ID:{" "}
              {workflow.dispatchId}
            </p>
          )}
        </div>
      </div>
    </section>
  );
}

/* ========================================================= */
/* Status badges                                               */
/* ========================================================= */

function StatusBadge({
  status,
}) {
  const styles = {
    Created:
      "bg-neutral-100 text-neutral-600",

    Planning:
      "bg-blue-50 text-blue-700",

    Running:
      "bg-blue-50 text-blue-700",

    WaitingForApproval:
      "bg-amber-50 text-amber-700",

    Approved:
      "bg-emerald-50 text-emerald-700",

    Completed:
      "bg-emerald-50 text-emerald-700",

    Rejected:
      "bg-red-50 text-red-700",

    FailedValidation:
      "bg-red-50 text-red-700",

    SafelyFailed:
      "bg-red-50 text-red-700",

    TimedOut:
      "bg-red-50 text-red-700",
  };

  return (
    <span
      className={`rounded-full px-3 py-1 text-xs font-semibold ${
        styles[status] ||
        "bg-neutral-100 text-neutral-600"
      }`}
    >
      {status}
    </span>
  );
}

function StepStatus({
  status,
}) {
  const styles = {
    Running:
      "bg-blue-50 text-blue-700",

    Completed:
      "bg-emerald-50 text-emerald-700",

    Failed:
      "bg-red-50 text-red-700",

    Pending:
      "bg-neutral-100 text-neutral-600",
  };

  return (
    <span
      className={`rounded-full px-2.5 py-1 text-[11px] font-semibold ${
        styles[status] ||
        "bg-neutral-100 text-neutral-600"
      }`}
    >
      {status}
    </span>
  );
}