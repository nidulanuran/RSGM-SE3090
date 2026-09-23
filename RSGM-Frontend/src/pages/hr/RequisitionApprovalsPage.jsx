import { useEffect, useState } from "react";
import {
  BadgeCheck,
  BriefcaseBusiness,
  Building2,
  CalendarClock,
  CircleDollarSign,
  FileText,
  MapPin,
  UserRound,
  XCircle,
  Sparkles,
  Bot,
  ShieldCheck,
  Cpu,
} from "lucide-react";

import {
  approveRequisition,
  getHrRequisitions,
  rejectRequisition,
} from "../../services/hrRequisitionService";

import {
  analyzeRequisitionWithHrAgent,
  decideHrApprovalGate,
  getHrRequisitionAgentStatus,
} from "../../services/hrRequisitionAgentService";

import HrRequisitionAgentDossierModal from "../../components/hr/HrRequisitionAgentDossierModal";

const employmentTypeLabels = {
  0: "Full Time",
  1: "Part Time",
  2: "Contract",
  3: "Internship",
};

const workModeLabels = {
  0: "On Site",
  1: "Remote",
  2: "Hybrid",
};

const experienceLevelLabels = {
  0: "Entry",
  1: "Junior",
  2: "Mid",
  3: "Senior",
  4: "Lead",
};

function getStatusLabel(status) {
  if (typeof status === "string") {
    return status;
  }

  const labels = {
    0: "Draft",
    1: "Submitted",
    2: "Rejected",
    3: "Approved",
  };

  return labels[status] ?? "Unknown";
}

function getStatusClasses(status) {
  const label = getStatusLabel(status);

  if (label === "Approved") {
    return "border-emerald-200 bg-emerald-50 text-emerald-700";
  }

  if (label === "Rejected") {
    return "border-red-200 bg-red-50 text-red-700";
  }

  if (label === "Submitted") {
    return "border-amber-200 bg-amber-50 text-amber-700";
  }

  return "border-slate-200 bg-slate-100 text-slate-700";
}

function formatDate(date) {
  if (!date) return "-";

  return new Date(date).toLocaleString();
}

export default function RequisitionApprovalsPage() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);

  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const [rejectingId, setRejectingId] =
    useState(null);

  const [feedback, setFeedback] =
    useState("");

  const [processingId, setProcessingId] =
    useState(null);

  // HR Agent state
  const [agentWorkflow, setAgentWorkflow] = useState(null);
  const [isDossierOpen, setIsDossierOpen] = useState(false);
  const [selectedRequisitionId, setSelectedRequisitionId] = useState(null);
  const [agentLoadingId, setAgentLoadingId] = useState(null);

  async function handleOpenAgentDossier(id) {
    setSelectedRequisitionId(id);
    setAgentLoadingId(id);
    setError("");
    try {
      let workflow = null;
      try {
        workflow = await getHrRequisitionAgentStatus(id);
      } catch {
        // If not analyzed yet, run analysis
        workflow = await analyzeRequisitionWithHrAgent(id);
      }
      setAgentWorkflow(workflow);
      setIsDossierOpen(true);
    } catch (err) {
      setError(err.message);
    } finally {
      setAgentLoadingId(null);
    }
  }

  async function handleAgentDecision(decisionCode, comment) {
    if (!selectedRequisitionId) return;
    setProcessingId(selectedRequisitionId);
    setError("");
    setSuccess("");
    try {
      const res = await decideHrApprovalGate(selectedRequisitionId, decisionCode, comment);
      setSuccess(res.message || "Decision successfully processed.");
      await load();
    } catch (err) {
      setError(err.message);
    } finally {
      setProcessingId(null);
    }
  }

  async function load() {
    setLoading(true);
    setError("");

    try {
      const data =
        await getHrRequisitions();

      setItems(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let cancelled = false;

    async function loadInitialRequisitions() {
      try {
        const data = await getHrRequisitions();
        if (!cancelled) {
          setItems(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadInitialRequisitions();
    return () => { cancelled = true; };
  }, []);

  async function handleApprove(id) {
    const confirmed = window.confirm(
      "Approve this job requisition?"
    );

    if (!confirmed) {
      return;
    }

    setError("");
    setSuccess("");
    setProcessingId(id);

    try {
      await approveRequisition(id);

      setSuccess(
        "Job requisition approved successfully."
      );

      await load();
    } catch (err) {
      setError(err.message);
    } finally {
      setProcessingId(null);
    }
  }

  function openRejectBox(id) {
    setRejectingId(id);
    setFeedback("");
    setError("");
    setSuccess("");
  }

  function cancelReject() {
    setRejectingId(null);
    setFeedback("");
  }

  async function handleReject(id) {
    const cleanFeedback =
      feedback.trim();

    if (cleanFeedback.length < 5) {
      setError(
        "Please provide a clear reason for rejection."
      );
      return;
    }

    setProcessingId(id);
    setError("");
    setSuccess("");

    try {
      await rejectRequisition(
        id,
        cleanFeedback
      );

      setSuccess(
        "Job requisition rejected and feedback sent to the recruiter."
      );

      setRejectingId(null);
      setFeedback("");

      await load();
    } catch (err) {
      setError(err.message);
    } finally {
      setProcessingId(null);
    }
  }

  return (
    <div className="min-h-screen bg-emerald-50/20 px-6 py-7">
      <div className="mx-auto max-w-6xl space-y-7">

        {/* Header */}
        <div className="overflow-hidden rounded-2xl border border-emerald-100 bg-linear-to-r from-emerald-900 to-emerald-700 px-7 py-6 text-white shadow-sm">
          <div className="flex items-center gap-4">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white/10">
              <BadgeCheck size={25} />
            </div>

            <div>
              <h1 className="text-2xl font-bold">
                Requisition Approvals
              </h1>

              <p className="mt-1 text-sm text-emerald-100">
                Review job requisitions
                submitted by recruiters
                in your company.
              </p>
            </div>
          </div>
        </div>

        {/* Alerts */}
        {error && (
          <div className="rounded-xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-700">
            {error}
          </div>
        )}

        {success && (
          <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-5 py-4 text-sm text-emerald-700">
            {success}
          </div>
        )}

        {/* Summary */}
        <div className="grid gap-4 md:grid-cols-3">
          <div className="rounded-2xl border border-emerald-100 bg-white p-5 shadow-sm">
            <p className="text-sm text-slate-500">
              Total Requisitions
            </p>

            <p className="mt-2 text-3xl font-bold text-slate-900">
              {items.length}
            </p>
          </div>

          <div className="rounded-2xl border border-amber-100 bg-white p-5 shadow-sm">
            <p className="text-sm text-slate-500">
              Pending Approval
            </p>

            <p className="mt-2 text-3xl font-bold text-amber-600">
              {
                items.filter(
                  (item) =>
                    getStatusLabel(
                      item.status
                    ) === "Submitted"
                ).length
              }
            </p>
          </div>

          <div className="rounded-2xl border border-emerald-100 bg-white p-5 shadow-sm">
            <p className="text-sm text-slate-500">
              Approved
            </p>

            <p className="mt-2 text-3xl font-bold text-emerald-600">
              {
                items.filter(
                  (item) =>
                    getStatusLabel(
                      item.status
                    ) === "Approved"
                ).length
              }
            </p>
          </div>
        </div>

        {/* List */}
        <section>
          <div className="mb-4">
            <h2 className="text-xl font-semibold text-slate-900">
              Company Requisitions
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              Review position details,
              budget, justification and
              recruiter information before
              making a decision.
            </p>
          </div>

          {loading ? (
            <div className="rounded-2xl border border-emerald-100 bg-white p-10 text-center text-slate-500">
              Loading requisitions...
            </div>
          ) : items.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-emerald-200 bg-white p-12 text-center">
              <BriefcaseBusiness
                size={38}
                className="mx-auto text-emerald-400"
              />

              <p className="mt-4 font-semibold text-slate-700">
                No requisitions available
              </p>

              <p className="mt-1 text-sm text-slate-500">
                Submitted requisitions
                from recruiters will appear
                here.
              </p>
            </div>
          ) : (
            <div className="space-y-5">
              {items.map((item) => {
                const status =
                  getStatusLabel(
                    item.status
                  );

                const isSubmitted =
                  status ===
                  "Submitted";

                return (
                  <article
                    key={item.id}
                    className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm"
                  >

                    {/* Card Header */}
                    <div className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-100 bg-slate-50/70 px-6 py-5">
                      <div>
                        <h3 className="text-xl font-semibold text-slate-900">
                          {
                            item.positionTitle
                          }
                        </h3>

                        <div className="mt-2 flex flex-wrap items-center gap-4 text-sm text-slate-500">
                          <span className="inline-flex items-center gap-1.5">
                            <UserRound size={15} />

                            Requested by{" "}
                            <span className="font-medium text-slate-700">
                              {
                                item.recruiterName
                              }
                            </span>
                          </span>

                          <span className="inline-flex items-center gap-1.5">
                            <Building2 size={15} />

                            {
                              item.companyName
                            }
                          </span>
                        </div>
                      </div>

                      <span
                        className={`rounded-full border px-3 py-1.5 text-xs font-semibold ${getStatusClasses(
                          item.status
                        )}`}
                      >
                        {status}
                      </span>
                    </div>

                    <div className="space-y-7 p-6">

                      {/* Basic Information */}
                      <section>
                        <h4 className="mb-4 flex items-center gap-2 font-semibold text-slate-900">
                          <BriefcaseBusiness
                            size={18}
                            className="text-emerald-700"
                          />

                          Position Information
                        </h4>

                        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                          <InfoBox
                            label="Department"
                            value={
                              item.department
                            }
                          />

                          <InfoBox
                            label="Headcount"
                            value={
                              item.headcount
                            }
                          />

                          <InfoBox
                            label="Employment Type"
                            value={
                              employmentTypeLabels[
                                item
                                  .employmentType
                              ] ?? "-"
                            }
                          />

                          <InfoBox
                            label="Work Mode"
                            value={
                              workModeLabels[
                                item.workMode
                              ] ?? "-"
                            }
                          />

                          <InfoBox
                            label="Experience Level"
                            value={
                              experienceLevelLabels[
                                item
                                  .experienceLevel
                              ] ?? "-"
                            }
                          />

                          <InfoBox
                            label="Minimum Experience"
                            value={
                              item.minExperienceYears !=
                              null
                                ? `${item.minExperienceYears} years`
                                : "-"
                            }
                          />

                          <InfoBox
                            label="Location"
                            value={
                              item.location
                            }
                            icon={
                              <MapPin
                                size={
                                  14
                                }
                              />
                            }
                          />

                          <InfoBox
                            label="Submitted"
                            value={formatDate(
                              item.submittedAt
                            )}
                            icon={
                              <CalendarClock
                                size={
                                  14
                                }
                              />
                            }
                          />
                        </div>
                      </section>

                      {/* Salary */}
                      <section className="border-t border-slate-100 pt-6">
                        <h4 className="mb-4 flex items-center gap-2 font-semibold text-slate-900">
                          <CircleDollarSign
                            size={18}
                            className="text-emerald-700"
                          />

                          Salary Information
                        </h4>

                        <div className="rounded-xl border border-emerald-100 bg-emerald-50/50 p-5">
                          <div className="grid gap-4 sm:grid-cols-3">
                            <InfoBox
                              label="Minimum Salary"
                              value={
                                item.minSalary !=
                                null
                                  ? `${item.minSalary} ${item.currency}`
                                  : "-"
                              }
                            />

                            <InfoBox
                              label="Maximum Salary"
                              value={
                                item.maxSalary !=
                                null
                                  ? `${item.maxSalary} ${item.currency}`
                                  : "-"
                              }
                            />

                            <InfoBox
                              label="Currency"
                              value={
                                item.currency ||
                                "-"
                              }
                            />
                          </div>
                        </div>
                      </section>

                      {/* Job Details */}
                      <section className="border-t border-slate-100 pt-6">
                        <h4 className="mb-4 flex items-center gap-2 font-semibold text-slate-900">
                          <FileText
                            size={18}
                            className="text-emerald-700"
                          />

                          Job Details
                        </h4>

                        <div className="grid gap-5 lg:grid-cols-2">
                          <DetailBlock
                            title="Description"
                            value={
                              item.description
                            }
                          />

                          <DetailBlock
                            title="Responsibilities"
                            value={
                              item.responsibilities
                            }
                          />

                          <DetailBlock
                            title="Requirements"
                            value={
                              item.requirements
                            }
                          />

                          <DetailBlock
                            title="Business Justification"
                            value={
                              item.justification
                            }
                          />
                        </div>
                      </section>

                      {/* Previous HR Feedback */}
                      {item.hrFeedback && (
                        <section className="rounded-xl border border-red-200 bg-red-50 p-5">
                          <div className="flex items-start gap-3">
                            <XCircle
                              size={20}
                              className="mt-0.5 shrink-0 text-red-600"
                            />

                            <div>
                              <p className="font-semibold text-red-800">
                                HR Feedback
                              </p>

                              <p className="mt-1 text-sm leading-6 text-red-700">
                                {
                                  item.hrFeedback
                                }
                              </p>
                            </div>
                          </div>
                        </section>
                      )}

                      {/* Actions */}
                      {isSubmitted && (
                        <section className="border-t border-slate-100 pt-6">
                          <div className="flex flex-wrap items-center gap-3">
                            <button
                              type="button"
                              disabled={agentLoadingId === item.id}
                              onClick={() => handleOpenAgentDossier(item.id)}
                              className="inline-flex items-center gap-2 rounded-xl border border-indigo-200 bg-indigo-50/80 px-5 py-2.5 text-sm font-semibold text-indigo-700 transition hover:bg-indigo-100 shadow-xs"
                            >
                              <Sparkles size={17} className="text-indigo-600" />
                              {agentLoadingId === item.id ? "Auditing with AI..." : "View AI Readiness Dossier"}
                            </button>

                            <button
                              type="button"
                              disabled={
                                processingId ===
                                item.id
                              }
                              onClick={() =>
                                handleApprove(
                                  item.id
                                )
                              }
                              className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
                            >
                              <BadgeCheck
                                size={17}
                              />

                              {processingId ===
                              item.id
                                ? "Processing..."
                                : "Approve Requisition"}
                            </button>

                            <button
                              type="button"
                              disabled={
                                processingId ===
                                item.id
                              }
                              onClick={() =>
                                openRejectBox(
                                  item.id
                                )
                              }
                              className="inline-flex items-center gap-2 rounded-xl border border-red-200 bg-white px-5 py-2.5 text-sm font-semibold text-red-600 transition hover:bg-red-50"
                            >
                              <XCircle
                                size={17}
                              />

                              Reject Requisition
                            </button>
                          </div>
                        </section>
                      )}

                      {/* Reject form */}
                      {rejectingId ===
                        item.id && (
                        <section className="rounded-2xl border border-red-200 bg-red-50/60 p-5">
                          <div className="mb-4">
                            <h4 className="font-semibold text-red-900">
                              Reject Job
                              Requisition
                            </h4>

                            <p className="mt-1 text-sm text-red-700">
                              Explain why
                              this request
                              cannot be
                              approved. The
                              recruiter will
                              use this
                              feedback to
                              update and
                              resubmit the
                              requisition.
                            </p>
                          </div>

                          <div>
                            <label className="mb-2 block text-sm font-medium text-slate-700">
                              Rejection
                              Reason
                              <span className="text-red-500">
                                *
                              </span>
                            </label>

                            <textarea
                              rows={5}
                              maxLength={
                                2000
                              }
                              value={
                                feedback
                              }
                              onChange={(
                                e
                              ) =>
                                setFeedback(
                                  e
                                    .target
                                    .value
                                )
                              }
                              placeholder="Example: The proposed salary exceeds the approved budget. Please reduce the maximum salary to LKR 170,000."
                              className="w-full rounded-xl border border-red-200 bg-white px-4 py-3 outline-none transition focus:border-red-500 focus:ring-2 focus:ring-red-500/10"
                            />

                            <div className="mt-2 flex justify-between text-xs text-slate-500">
                              <span>
                                Minimum 5
                                characters
                              </span>

                              <span>
                                {
                                  feedback.length
                                }
                                /2000
                              </span>
                            </div>
                          </div>

                          <div className="mt-5 flex flex-wrap gap-3">
                            <button
                              type="button"
                              disabled={
                                processingId ===
                                item.id
                              }
                              onClick={() =>
                                handleReject(
                                  item.id
                                )
                              }
                              className="inline-flex items-center gap-2 rounded-xl bg-red-600 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-60"
                            >
                              <XCircle
                                size={17}
                              />

                              {processingId ===
                              item.id
                                ? "Rejecting..."
                                : "Confirm Rejection"}
                            </button>

                            <button
                              type="button"
                              onClick={
                                cancelReject
                              }
                              className="rounded-xl border border-slate-300 bg-white px-5 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                            >
                              Cancel
                            </button>
                          </div>
                        </section>
                      )}

                      {!isSubmitted && (
                        <div className="border-t border-slate-100 pt-4 flex justify-end">
                          <button
                            type="button"
                            disabled={agentLoadingId === item.id}
                            onClick={() => handleOpenAgentDossier(item.id)}
                            className="inline-flex items-center gap-2 text-xs font-semibold text-indigo-700 hover:text-indigo-800 bg-indigo-50 px-3 py-1.5 rounded-lg border border-indigo-100 transition"
                          >
                            <Bot size={14} />
                            {agentLoadingId === item.id ? "Loading AI..." : "View AI Audit Dossier"}
                          </button>
                        </div>
                      )}
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </section>
      </div>

      {/* HR Agent Dossier Modal */}
      <HrRequisitionAgentDossierModal
        isOpen={isDossierOpen}
        onClose={() => setIsDossierOpen(false)}
        workflow={agentWorkflow}
        onDecision={handleAgentDecision}
        isHrManager={true}
      />
    </div>
  );
}

function InfoBox({
  label,
  value,
  icon,
}) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">
        {label}
      </p>

      <p className="mt-1 flex items-center gap-1.5 text-sm font-medium text-slate-700">
        {icon}
        {value ?? "-"}
      </p>
    </div>
  );
}

function DetailBlock({
  title,
  value,
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-slate-50/60 p-4">
      <p className="text-sm font-semibold text-slate-800">
        {title}
      </p>

      <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-600">
        {value || "Not provided"}
      </p>
    </div>
  );
}