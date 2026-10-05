import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  downloadShortlistedCandidateCv,
  getAvailableSlots,
  getHrManagers,
  getPanelistShortlists,
  getShortlistedCandidate,
  proposeInterview,
} from "../../services/panelistWorkflowService";
import {
  startInterviewSchedulingAgent,
   getInterviewSchedulingWorkflows,
  approveInterviewMode,
  decideInterviewSchedule,
} from "../../services/interviewSchedulingAgentService";

export default function ShortlistsPage() {
  const [jobs, setJobs] = useState([]);
  const [selection, setSelection] = useState(null);
  const [managers, setManagers] = useState([]);
  const [hrId, setHrId] = useState("");
  const [slots, setSlots] = useState([]);
  const [start, setStart] = useState("");
  const [type, setType] = useState("Physical");
  const [location, setLocation] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [agentSelection, setAgentSelection] = useState(null);
  const [agentHrId, setAgentHrId] = useState("");
  const [agentObjective, setAgentObjective] = useState("");
  const [agentWorkflow, setAgentWorkflow] = useState(null);
  const [agentBusy, setAgentBusy] = useState(false);
  const [agentError, setAgentError] = useState("");
  const [agentInterviewType, setAgentInterviewType] = useState("Physical");
  const [agentLocation, setAgentLocation] = useState("");
  const [agentApprovalComment, setAgentApprovalComment] = useState("");
  const [agentScheduleComment, setAgentScheduleComment] = useState("");
  const [candidatePreview, setCandidatePreview] = useState(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [previewError, setPreviewError] = useState("");
  const [cvDownloading, setCvDownloading] = useState(false);

  const refresh = useCallback(
    () =>
      getPanelistShortlists()
        .then(setJobs)
        .catch((e) => setError(e.message)),
    []
  );

  useEffect(() => {
    refresh();
  }, [refresh]);

  useEffect(() => {
    let cancelled = false;

    async function restoreActiveAgentWorkflow() {
      try {
        const workflows =
          await getInterviewSchedulingWorkflows();

        if (cancelled) return;

        const terminalStatuses = [
          "Completed",
          "Rejected",
          "FailedValidation",
          "TimedOut",
          "SafelyFailed",
        ];

        const activeWorkflow =
          workflows.find(
            (workflow) =>
              !terminalStatuses.includes(
                workflow.status
              )
          );

        if (activeWorkflow) {
          setAgentWorkflow(activeWorkflow);
        }
      } catch (err) {
        if (!cancelled) {
          setAgentError(err.message);
        }
      }
    }

    restoreActiveAgentWorkflow();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!selection) return;

    getHrManagers(selection.jobId)
      .then((people) => {
        setManagers(people);
        setHrId(people[0]?.id || "");
      })
      .catch((e) => setError(e.message));
  }, [selection]);

  useEffect(() => {
    if (!selection || !hrId) return;

    let cancelled = false;

    getAvailableSlots(selection.jobId, hrId)
      .then((data) => {
        if (!cancelled) setSlots(data);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message);
      });

    return () => {
      cancelled = true;
    };
  }, [selection, hrId]);

  async function openCandidate(candidate) {
    setPreviewLoading(true);
    setPreviewError("");
    setCandidatePreview(null);

    try {
      const details = await getShortlistedCandidate(candidate.id);
      setCandidatePreview(details);
    } catch (err) {
      setPreviewError(err.message);
    } finally {
      setPreviewLoading(false);
    }
  }

  function closeCandidatePreview() {
    setCandidatePreview(null);
    setPreviewError("");
  }

  async function downloadCv() {
    if (!candidatePreview) return;

    setCvDownloading(true);
    setPreviewError("");

    try {
      await downloadShortlistedCandidateCv(
        candidatePreview.id,
        candidatePreview.cvFileName
      );
    } catch (err) {
      setPreviewError(err.message);
    } finally {
      setCvDownloading(false);
    }
  }

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");

    try {
      await proposeInterview({
        applicationId: selection.candidate.id,
        hrManagerId: hrId,
        startsAt: start,
        type,
        locationOrLink: location.trim(),
      });

      setSelection(null);
      setSlots([]);
      setLocation("");

      await refresh();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function startAgent(e) {
    e.preventDefault();

    if (!agentSelection || !agentHrId) return;

    setAgentBusy(true);
    setAgentError("");

    try {
      const workflow =
        await startInterviewSchedulingAgent(
          agentSelection.candidate.id,
          agentHrId,
          agentObjective
        );

      setAgentWorkflow(workflow);
    } catch (err) {
      setAgentError(err.message);
    } finally {
      setAgentBusy(false);
    }
  }

  async function approveAgentMode(e) {
    e.preventDefault();

    if (!agentWorkflow) return;

    setAgentBusy(true);
    setAgentError("");

    try {
      const workflow = await approveInterviewMode(
        agentWorkflow.id,
        agentInterviewType,
        agentLocation.trim(),
        agentApprovalComment
      );

      setAgentWorkflow(workflow);
    } catch (err) {
      setAgentError(err.message);
    } finally {
      setAgentBusy(false);
    }
  }

  async function decideAgentSchedule(approved) {
    if (!agentWorkflow) return;

      setAgentBusy(true);
      setAgentError("");

      try {
        const workflow = await decideInterviewSchedule(
          agentWorkflow.id,
          approved,
          agentScheduleComment
        );

        setAgentWorkflow(workflow);
      } catch (err) {
        setAgentError(err.message);
      } finally {
        setAgentBusy(false);
      }
    }

  return (
    <div>
      <p className="text-xs font-semibold uppercase text-amber-600">
        Hiring panelist
      </p>

      <h1 className="mt-3 text-3xl font-semibold">
        Ranked shortlists
      </h1>

      <p className="mt-2 text-sm text-neutral-500">
        Review shortlisted candidates before arranging interviews.
        Available interview slots consider recruiter, panelist and HR
        availability.
      </p>

      <Link
        className="mt-3 inline-block text-sm font-semibold text-amber-700"
        to="/panelist/schedule"
      >
        Manage your busy schedule →
      </Link>

      {error && (
        <p
          role="alert"
          className="mt-5 rounded-xl bg-red-50 p-3 text-sm text-red-700"
        >
          {error}
        </p>
      )}

      <div className="mt-7 space-y-5">
        {jobs.length === 0 && (
          <p className="rounded-2xl bg-white p-5 text-neutral-500">
            No shortlists assigned yet.
          </p>
        )}

        {jobs.map((job) => (
          <section
            key={job.jobPostingId}
            className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm"
          >
            <h2 className="text-lg font-semibold">
              {job.jobTitle}
            </h2>

            <p className="mb-4 text-xs text-neutral-500">
              Sent by {job.recruiter} ·{" "}
              {new Date(job.submittedAt).toLocaleString()}
            </p>

            {job.candidates.map((candidate) => (
              <div
                key={candidate.id}
                className="flex flex-wrap items-center gap-3 border-t border-neutral-100 py-3"
              >
                <span className="rounded-xl bg-amber-50 px-3 py-2 text-sm font-semibold text-amber-700">
                  #{candidate.shortlistRank}
                </span>

                <div className="flex-1">
                  <p className="font-semibold">
                    {candidate.candidate}
                  </p>

                  <p className="text-xs text-neutral-500">
                    {candidate.email} · {candidate.status}
                  </p>
                </div>

                {candidate.status === "Shortlisted" && (
                  <div className="flex flex-wrap gap-2">
                    <button
                      type="button"
                      onClick={() => openCandidate(candidate)}
                      className="rounded-xl border border-amber-600 px-4 py-2 text-sm font-semibold text-amber-700 hover:bg-amber-50"
                    >
                      View candidate
                    </button>

                    

                    <button
                      type="button"
                      onClick={async () => {
                        setAgentError("");
                        setAgentWorkflow(null);
                        setAgentObjective("");

                        try {
                          const people = await getHrManagers(
                            job.jobPostingId
                          );

                          setManagers(people);

                          const defaultHrId =
                            people[0]?.id || "";

                          setAgentHrId(defaultHrId);

                          setAgentSelection({
                            jobId: job.jobPostingId,
                            jobTitle: job.jobTitle,
                            candidate,
                          });
                        } catch (err) {
                          setAgentError(err.message);
                        }
                      }}
                      className="rounded-xl bg-amber-600 px-4 py-2 text-sm font-semibold text-white hover:bg-amber-700"
                    >
                      Activate Interview Scheduling Agent
                    </button>

                  </div>
                )}
              </div>
            ))}
          </section>
        ))}
      </div>

      {previewLoading && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/50 p-4">
          <div className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-2xl">
            <p className="text-center text-sm text-neutral-500">
              Loading candidate information...
            </p>
          </div>
        </div>
      )}

      {!previewLoading && previewError && !candidatePreview && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/50 p-4">
          <div className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-2xl">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">
                Candidate information
              </h2>

              <button
                type="button"
                onClick={closeCandidatePreview}
              >
                ✕
              </button>
            </div>

            <p className="mt-4 rounded-xl bg-red-50 p-3 text-sm text-red-700">
              {previewError}
            </p>
          </div>
        </div>
      )}

      {candidatePreview && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/50 p-4">
          <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white p-6 shadow-2xl">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase text-amber-600">
                  Candidate profile
                </p>

                <h2 className="mt-1 text-2xl font-semibold">
                  {candidatePreview.fullName}
                </h2>

                <p className="mt-1 text-sm text-neutral-500">
                  {candidatePreview.jobTitle}
                </p>
              </div>

              <button
                type="button"
                onClick={closeCandidatePreview}
                aria-label="Close candidate profile"
              >
                ✕
              </button>
            </div>

            {previewError && (
              <p className="mt-4 rounded-xl bg-red-50 p-3 text-sm text-red-700">
                {previewError}
              </p>
            )}

            <div className="mt-6 space-y-6">
              <section className="rounded-2xl bg-amber-50 p-4">
                <p className="font-semibold">
                  {candidatePreview.headline || "Job applicant"}
                </p>

                <p className="mt-2 text-sm text-neutral-700">
                  {candidatePreview.email}
                </p>

                {candidatePreview.phoneNumber && (
                  <p className="mt-1 text-sm text-neutral-700">
                    {candidatePreview.phoneNumber}
                  </p>
                )}

                {candidatePreview.location && (
                  <p className="mt-1 text-sm text-neutral-700">
                    {candidatePreview.location}
                  </p>
                )}
              </section>

              {candidatePreview.bio && (
                <section>
                  <h3 className="font-semibold">About</h3>

                  <p className="mt-2 whitespace-pre-wrap text-sm text-neutral-600">
                    {candidatePreview.bio}
                  </p>
                </section>
              )}

              <section>
                <h3 className="font-semibold">Skills</h3>

                <div className="mt-2 flex flex-wrap gap-2">
                  {candidatePreview.skills?.length ? (
                    candidatePreview.skills.map((skill, index) => (
                      <span
                        key={`${skill.name}-${index}`}
                        className="rounded-full bg-amber-50 px-3 py-1 text-xs text-amber-800"
                      >
                        {skill.name} · {skill.proficiencyLevel}/5
                      </span>
                    ))
                  ) : (
                    <p className="text-sm text-neutral-400">
                      No skills provided.
                    </p>
                  )}
                </div>
              </section>

              <section>
                <h3 className="font-semibold">
                  Work experience
                </h3>

                <div className="mt-2 space-y-2">
                  {candidatePreview.workExperience?.length ? (
                    candidatePreview.workExperience.map(
                      (experience, index) => (
                        <div
                          key={index}
                          className="rounded-xl bg-neutral-50 p-3 text-sm"
                        >
                          <p className="font-medium">
                            {experience.jobTitle} ·{" "}
                            {experience.companyName}
                          </p>

                          {experience.location && (
                            <p className="mt-1 text-xs text-neutral-500">
                              {experience.location}
                            </p>
                          )}

                          <p className="mt-1 text-xs text-neutral-500">
                            {experience.startDate} –{" "}
                            {experience.isCurrent
                              ? "Present"
                              : experience.endDate || ""}
                          </p>
                        </div>
                      )
                    )
                  ) : (
                    <p className="text-sm text-neutral-400">
                      No work experience provided.
                    </p>
                  )}
                </div>
              </section>

              <section>
                <h3 className="font-semibold">Education</h3>

                <div className="mt-2 space-y-2">
                  {candidatePreview.education?.length ? (
                    candidatePreview.education.map(
                      (education, index) => (
                        <div
                          key={index}
                          className="rounded-xl bg-neutral-50 p-3 text-sm"
                        >
                          <p className="font-medium">
                            {education.degree} ·{" "}
                            {education.institution}
                          </p>

                          {education.fieldOfStudy && (
                            <p className="mt-1 text-xs text-neutral-500">
                              {education.fieldOfStudy}
                            </p>
                          )}
                        </div>
                      )
                    )
                  ) : (
                    <p className="text-sm text-neutral-400">
                      No education records provided.
                    </p>
                  )}
                </div>
              </section>

              <section>
                <h3 className="font-semibold">
                  Professional links
                </h3>

                <div className="mt-2 flex flex-wrap gap-2">
                  {candidatePreview.linkedInUrl && (
                    <a
                      href={candidatePreview.linkedInUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="rounded-xl border border-neutral-200 px-3 py-2 text-sm font-semibold text-amber-700"
                    >
                      LinkedIn ↗
                    </a>
                  )}

                  {candidatePreview.gitHubUrl && (
                    <a
                      href={candidatePreview.gitHubUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="rounded-xl border border-neutral-200 px-3 py-2 text-sm font-semibold text-amber-700"
                    >
                      GitHub ↗
                    </a>
                  )}

                  {candidatePreview.portfolioUrl && (
                    <a
                      href={candidatePreview.portfolioUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="rounded-xl border border-neutral-200 px-3 py-2 text-sm font-semibold text-amber-700"
                    >
                      Portfolio ↗
                    </a>
                  )}
                </div>
              </section>

              <section>
                <h3 className="font-semibold">CV</h3>

                {candidatePreview.hasCv ? (
                  <button
                    type="button"
                    disabled={cvDownloading}
                    onClick={downloadCv}
                    className="mt-2 rounded-xl bg-amber-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
                  >
                    {cvDownloading
                      ? "Downloading..."
                      : `Download ${
                          candidatePreview.cvFileName || "CV"
                        }`}
                  </button>
                ) : (
                  <p className="mt-2 text-sm text-neutral-400">
                    No CV uploaded.
                  </p>
                )}
              </section>
            </div>
          </div>
        </div>
      )}

      {agentWorkflow &&
        [
          "Completed",
          "Rejected",
          "FailedValidation",
          "TimedOut",
          "SafelyFailed",
        ].includes(agentWorkflow.status) && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/50 p-4">
            <div className="w-full max-w-lg space-y-4 rounded-2xl bg-white p-6 shadow-2xl">
              <div>
                <p className="text-xs font-semibold uppercase text-amber-600">
                  Interview Scheduling Agent
                </p>

                <h2 className="mt-1 text-xl font-semibold">
                  Workflow result
                </h2>
              </div>

              {agentWorkflow.status === "Completed" && (
                <div className="rounded-xl bg-green-50 p-4 text-sm text-green-800">
            <p className="font-semibold">
              Interview scheduled successfully ✅
            </p>

            <p className="mt-1">
              The final slot passed validation and the interview
              was created.
            </p>
          </div>
        )}

        {agentWorkflow.status === "Rejected" && (
          <div className="rounded-xl bg-neutral-100 p-4 text-sm text-neutral-700">
            <p className="font-semibold">
              Schedule rejected
            </p>

            <p className="mt-1">
              No interview was created because the proposed
              schedule was rejected by the panelist.
            </p>
          </div>
        )}

        {[
          "FailedValidation",
          "TimedOut",
          "SafelyFailed",
        ].includes(agentWorkflow.status) && (
          <div className="rounded-xl bg-red-50 p-4 text-sm text-red-700">
            <p className="font-semibold">
              Agent stopped safely
            </p>

            <p className="mt-1">
              {agentWorkflow.errorMessage ||
                "The workflow could not be completed safely."}
            </p>
          </div>
        )}

        <div className="rounded-xl bg-neutral-50 p-4 text-sm">
          <p>
            <span className="text-neutral-500">
              Candidate:
            </span>{" "}
            <span className="font-semibold">
              {agentWorkflow.candidateName}
            </span>
          </p>

          <p className="mt-2">
            <span className="text-neutral-500">
              Job:
            </span>{" "}
            <span className="font-semibold">
              {agentWorkflow.jobTitle}
            </span>
          </p>

          <p className="mt-2">
            <span className="text-neutral-500">
              Final status:
            </span>{" "}
            <span className="font-semibold">
              {agentWorkflow.status}
            </span>
          </p>

          {agentWorkflow.interviewId && (
            <p className="mt-2">
              <span className="text-neutral-500">
                Interview ID:
              </span>{" "}
              <span className="break-all font-mono text-xs">
                {agentWorkflow.interviewId}
              </span>
            </p>
          )}
        </div>

        <button
          type="button"
          onClick={async () => {
            setAgentWorkflow(null);
            setAgentSelection(null);
            setAgentHrId("");
            setAgentObjective("");
            setAgentInterviewType("Physical");
            setAgentLocation("");
            setAgentApprovalComment("");
            setAgentScheduleComment("");
            setAgentError("");

            await refresh();
          }}
          className="w-full rounded-xl bg-neutral-900 px-4 py-3 text-sm font-semibold text-white"
        >
          Close
        </button>
      </div>
    </div>
  )}

      {selection && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/50 p-4">
          <form
            onSubmit={submit}
            className="w-full max-w-lg space-y-4 rounded-2xl bg-white p-6 shadow-2xl"
          >
            <div className="flex justify-between">
              <h2 className="text-lg font-semibold">
                Interview · {selection.candidate.candidate}
              </h2>

              <button
                type="button"
                onClick={() => {
                  setSelection(null);
                  setSlots([]);
                  setStart("");
                }}
                aria-label="Close"
              >
                ✕
              </button>
            </div>

            <label className="block text-sm font-medium">
              HR Manager

              <select
                required
                className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
                value={hrId}
                onChange={(e) => {
                  setHrId(e.target.value);
                  setSlots([]);
                  setStart("");
                }}
              >
                {managers.map((manager) => (
                  <option
                    key={manager.id}
                    value={manager.id}
                  >
                    {manager.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="block text-sm font-medium">
              Available one-hour slot

              <select
                required
                className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
                value={start}
                onChange={(e) => setStart(e.target.value)}
              >
                <option value="">
                  Select a slot
                </option>

                {slots.map((iso) => (
                  <option
                    key={iso}
                    value={iso}
                  >
                    {new Date(iso).toLocaleString()}
                  </option>
                ))}
              </select>
            </label>

            {!slots.length && (
              <p className="text-sm text-amber-700">
                No free slots were found in the next 30 days.
              </p>
            )}

            <label className="block text-sm font-medium">
              Mode

              <select
                className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
                value={type}
                onChange={(e) => setType(e.target.value)}
              >
                <option>Physical</option>
                <option>Online</option>
              </select>
            </label>

            <label className="block text-sm font-medium">
              {type === "Online"
                ? "Meeting link"
                : "Office location"}

              <input
                required
                maxLength={500}
                type={type === "Online" ? "url" : "text"}
                className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
              />
            </label>

            <button
              disabled={busy || !start}
              className="w-full rounded-xl bg-amber-600 px-4 py-3 text-sm font-semibold text-white disabled:opacity-50"
            >
              Send interview proposal
            </button>
          </form>
        </div>
      )}

      {agentWorkflow?.status === "WaitingForScheduleApproval" &&
        agentWorkflow.proposal && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/50 p-4">
            <div className="w-full max-w-lg space-y-4 rounded-2xl bg-white p-6 shadow-2xl">
              <div>
                <p className="text-xs font-semibold uppercase text-amber-600">
                  Human Approval #2
                </p>

                <h2 className="mt-1 text-lg font-semibold">
                  Review proposed interview
                </h2>

                <p className="mt-1 text-sm text-neutral-500">
                  The Coordination Agent selected a validated slot.
                </p>
              </div>

              {agentError && (
                <p
                  role="alert"
                  className="rounded-xl bg-red-50 p-3 text-sm text-red-700"
                >
                  {agentError}
                </p>
              )}

              <div className="space-y-3 rounded-2xl bg-neutral-50 p-4 text-sm">
                <div>
                  <p className="text-xs text-neutral-500">
                    Candidate
                  </p>

                  <p className="font-semibold">
                    {agentWorkflow.proposal.candidateName}
                  </p>
                </div>

                <div>
                  <p className="text-xs text-neutral-500">
                    Job
                  </p>

                  <p className="font-semibold">
                    {agentWorkflow.proposal.jobTitle}
                  </p>
                </div>

                <div>
                  <p className="text-xs text-neutral-500">
                    Proposed time
                  </p>

                  <p className="font-semibold">
                    {new Date(
                      agentWorkflow.proposal.startsAt
                    ).toLocaleString()}
                  </p>
                </div>  

                <div>
                  <p className="text-xs text-neutral-500">
                    Proposed time
                  </p>

                  <p className="font-semibold">
                    {new Date(
                      agentWorkflow.proposal.startsAt
                    ).toLocaleString()}
                  </p>
                </div>

                <div>
                  <p className="text-xs text-neutral-500">
                    Interview mode
                  </p>

                  <p className="font-semibold">
                    {agentWorkflow.proposal.type}
                  </p>
                </div>

                <div>
                  <p className="text-xs text-neutral-500">
                    Location / link
                  </p>

                  <p className="break-all font-semibold">
                    {agentWorkflow.proposal.locationOrLink}
                  </p>
                </div>

                <div>
                  <p className="text-xs text-neutral-500">
                    HR Manager
                  </p>

                  <p className="font-semibold">
                    {agentWorkflow.proposal.hrManagerName}
                  </p>
                </div>

                <div>
                  <p className="text-xs text-neutral-500">
                    Agent reasoning
                  </p>

                  <p className="mt-1 text-neutral-700">
                    {agentWorkflow.proposal.reason}
                  </p>
                </div>
              </div>

              <label className="block text-sm font-medium">
                Decision comment
                <span className="ml-1 text-xs font-normal text-neutral-400">
                  Optional
                </span>

                <textarea
                  rows={3}
                  maxLength={1000}
                  className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
                  value={agentScheduleComment}
                  onChange={(e) =>
                    setAgentScheduleComment(e.target.value)
                  }
                  placeholder="Optional note about your decision."
                />
              </label>

              <div className="grid grid-cols-2 gap-3">
                <button
                  type="button"
                  disabled={agentBusy}
                  onClick={() => decideAgentSchedule(false)}
                  className="rounded-xl border border-red-200 px-4 py-3 text-sm font-semibold text-red-700 disabled:opacity-50"
                >
                  Reject
                </button>

                <button
                  type="button"
                  disabled={agentBusy}
                  onClick={() => decideAgentSchedule(true)}
                  className="rounded-xl bg-amber-600 px-4 py-3 text-sm font-semibold text-white disabled:opacity-50"
                >
                  {agentBusy
                    ? "Validating..."
                    : "Approve schedule"}
                </button>
              </div>

              <p className="text-xs text-neutral-500">
                Approval does not bypass validation. The system will
                check the proposed slot again before creating the
                interview.
              </p>
            </div>
          </div>
        )}

      {agentWorkflow?.status === "WaitingForModeApproval" && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/50 p-4">
          <form
            onSubmit={approveAgentMode}
            className="w-full max-w-lg space-y-4 rounded-2xl bg-white p-6 shadow-2xl"
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase text-amber-600">
                  Human Approval #1
                </p>

                <h2 className="mt-1 text-lg font-semibold">
                  Choose interview mode
                </h2>

                <p className="mt-1 text-sm text-neutral-500">
                  {agentWorkflow.candidateName} · {agentWorkflow.jobTitle}
                </p>
              </div>

              <button
                type="button"
                onClick={() => {
                  setAgentWorkflow(null);
                  setAgentSelection(null);
                  setAgentError("");
                }}
                aria-label="Close"
              >
                ✕
              </button>
            </div>

            <div className="rounded-xl bg-neutral-50 p-4 text-sm text-neutral-700">
              <p className="font-semibold">
                Agent progress
              </p>

              <p className="mt-1">
                The context and availability checks are complete.
                The agent is waiting for your approval before choosing
                an interview time.
              </p>

              <p className="mt-2 text-xs text-neutral-500">
                Valid slots found:{" "}
                {agentWorkflow.availableSlots?.length || 0}
              </p>
            </div>

            {agentError && (
              <p
                role="alert"
                className="rounded-xl bg-red-50 p-3 text-sm text-red-700"
              >
                {agentError}
              </p>
            )}

            <label className="block text-sm font-medium">
              Interview mode

              <select
                className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
                value={agentInterviewType}
                onChange={(e) => {
                setAgentInterviewType(e.target.value);
                setAgentLocation("");
                }}
              >
              <option value="Physical">
                Physical
              </option>

              <option value="Online">
                Online
              </option>
              </select>
            </label>

            <label className="block text-sm font-medium">
              {agentInterviewType === "Online"
                ? "Meeting link"
                : "Interview location"}

              <input
                required
                maxLength={500}
                type={
                  agentInterviewType === "Online"
                    ? "url"
                    : "text"
                }
                className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
                placeholder={
                  agentInterviewType === "Online"
                    ? "https://meet.google.com/..."
                    : "Interview Room 2"
                }
                value={agentLocation}
                onChange={(e) =>
                  setAgentLocation(e.target.value)
                }
              />
            </label>

            <label className="block text-sm font-medium">
              Approval comment
              <span className="ml-1 text-xs font-normal text-neutral-400">
                Optional
              </span>

              <textarea
                rows={3}
                maxLength={1000}
                className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
                value={agentApprovalComment}
                onChange={(e) =>
                  setAgentApprovalComment(e.target.value)
                }
                placeholder="Optional note for this approval."
              />
            </label>

            <button
              type="submit"
              disabled={
                agentBusy ||
                !agentLocation.trim()
              }
              className="w-full rounded-xl bg-amber-600 px-4 py-3 text-sm font-semibold text-white disabled:opacity-50"
            >
              {agentBusy
                ? "Agent is coordinating..."
                : "Approve mode and continue"}
            </button>
          </form>
        </div>
      )}

      {agentSelection && !agentWorkflow && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/50 p-4">
          <form
            onSubmit={startAgent}
            className="w-full max-w-lg space-y-4 rounded-2xl bg-white p-6 shadow-2xl"
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase text-amber-600">
                  Interview Scheduling Agent
                </p>

                <h2 className="mt-1 text-lg font-semibold">
                  {agentSelection.candidate.candidate}
                </h2>

                <p className="mt-1 text-sm text-neutral-500">
                  {agentSelection.jobTitle}
                </p>
              </div>

              <button
                type="button"
                  onClick={() => {
                    setAgentSelection(null);
                    setAgentWorkflow(null);
                    setAgentHrId("");
                    setAgentObjective("");
                    setAgentError("");
                  }}
                  aria-label="Close interview scheduling agent"
              >
                ✕
              </button>
            </div>

            <p className="rounded-xl bg-amber-50 p-3 text-sm text-amber-800">
              The agent will check the recruiter, hiring panelist and
              HR Manager schedules and recommend a conflict-free
              interview time. You will approve the interview mode and
              final schedule before anything is created.
            </p>

            {agentError && (
              <p
                role="alert"
                className="rounded-xl bg-red-50 p-3 text-sm text-red-700"
              >
                {agentError}
              </p>
            )}

            <label className="block text-sm font-medium">
              HR Manager

              <select
                required
                className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
                value={agentHrId}
                onChange={(e) => setAgentHrId(e.target.value)}
              >
                <option value="">
                  Select an HR Manager
                </option>

                {managers.map((manager) => (
                  <option
                    key={manager.id}
                    value={manager.id}
                  >
                    {manager.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="block text-sm font-medium">
              Agent objective
            <span className="ml-1 text-xs font-normal text-neutral-400">
              Optional
            </span>

            <textarea
              maxLength={500}
              rows={4}
              className="mt-2 w-full rounded-xl border border-neutral-200 p-3"
              placeholder="Example: Find the earliest suitable conflict-free interview time."
              value={agentObjective}
              onChange={(e) =>
                setAgentObjective(e.target.value)
              }
            />
            </label>

            <button
              type="submit"
              disabled={agentBusy || !agentHrId}
              className="w-full rounded-xl bg-amber-600 px-4 py-3 text-sm font-semibold text-white disabled:opacity-50"
            >
              {agentBusy
                ? "Activating agent..."
                : "Activate Interview Scheduling Agent"}
            </button>
          </form>
        </div>
    )}

    </div>
  );
}