import { useEffect, useState } from "react";
import {
  BriefcaseBusiness,
  Building2,
  CircleDollarSign,
  FileText,
  MapPin,
  Send,
  Users,
  Sparkles,
  Bot,
  ShieldCheck,
} from "lucide-react";

import {
  createRequisition,
  getMyRequisitions,
  submitRequisition,
  updateRequisition,
} from "../../services/recruiterRequisitionService";

import {
  analyzeRequisitionWithHrAgent,
  submitRequisitionWithHrApprovalGate,
  getHrRequisitionAgentStatus,
} from "../../services/hrRequisitionAgentService";

import HrRequisitionAgentDossierModal from "../../components/hr/HrRequisitionAgentDossierModal";

const initialForm = {
  positionTitle: "",
  department: "",
  headcount: 1,
  employmentType: 0,
  workMode: 0,
  location: "",
  experienceLevel: 0,
  minExperienceYears: 0,
  minSalary: "",
  maxSalary: "",
  currency: "LKR",
  description: "",
  responsibilities: "",
  requirements: "",
  justification: "",
};

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
    return "bg-emerald-50 text-emerald-700 border-emerald-200";
  }

  if (label === "Rejected") {
    return "bg-red-50 text-red-700 border-red-200";
  }

  if (label === "Submitted") {
    return "bg-amber-50 text-amber-700 border-amber-200";
  }

  return "bg-slate-100 text-slate-700 border-slate-200";
}

export default function RequisitionsPage() {
  const [items, setItems] = useState([]);
  const [form, setForm] = useState(initialForm);
  const [editingId, setEditingId] = useState(null);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  // HR Agent state
  const [agentWorkflow, setAgentWorkflow] = useState(null);
  const [isDossierOpen, setIsDossierOpen] = useState(false);
  const [agentLoadingId, setAgentLoadingId] = useState(null);

  async function handleRunAgentAnalysis(id) {
    setAgentLoadingId(id);
    setError("");
    try {
      const workflow = await analyzeRequisitionWithHrAgent(id);
      setAgentWorkflow(workflow);
      setIsDossierOpen(true);
    } catch (err) {
      setError(err.message);
    } finally {
      setAgentLoadingId(null);
    }
  }

  async function handleSubmitWithApprovalGate(id) {
    const confirmed = window.confirm(
      "Submit requisition through AI Agent with mandatory HR Approval Gate?"
    );
    if (!confirmed) return;

    setAgentLoadingId(id);
    setError("");
    try {
      const workflow = await submitRequisitionWithHrApprovalGate(id);
      setAgentWorkflow(workflow);
      setIsDossierOpen(true);
      await load();
    } catch (err) {
      setError(err.message);
    } finally {
      setAgentLoadingId(null);
    }
  }

  async function handleViewAgentDossier(id) {
    setAgentLoadingId(id);
    setError("");
    try {
      const workflow = await getHrRequisitionAgentStatus(id);
      setAgentWorkflow(workflow);
      setIsDossierOpen(true);
    } catch (err) {
      // If none found, run analysis
      handleRunAgentAnalysis(id);
    } finally {
      setAgentLoadingId(null);
    }
  }

  async function load() {
    setLoading(true);
    setError("");

    try {
      const data = await getMyRequisitions();
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
        const data = await getMyRequisitions();
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

  function updateField(name, value) {
    setForm((previous) => ({
      ...previous,
      [name]: value,
    }));
  }

  function resetForm() {
    setForm(initialForm);
    setEditingId(null);
  }

  function startEdit(item) {
    setEditingId(item.id);

    setForm({
      positionTitle: item.positionTitle || "",
      department: item.department || "",
      headcount: item.headcount ?? 1,
      employmentType: item.employmentType ?? 0,
      workMode: item.workMode ?? 0,
      location: item.location || "",
      experienceLevel: item.experienceLevel ?? 0,
      minExperienceYears: item.minExperienceYears ?? 0,
      minSalary: item.minSalary ?? "",
      maxSalary: item.maxSalary ?? "",
      currency: item.currency || "LKR",
      description: item.description || "",
      responsibilities: item.responsibilities || "",
      requirements: item.requirements || "",
      justification: item.justification || "",
    });

    window.scrollTo({
      top: 0,
      behavior: "smooth",
    });
  }

  async function handleSave(event) {
    event.preventDefault();

    setSaving(true);
    setError("");

    try {
      const payload = {
        ...form,

        headcount: Number(form.headcount),

        employmentType: Number(
          form.employmentType
        ),

        workMode: Number(form.workMode),

        experienceLevel: Number(
          form.experienceLevel
        ),

        minExperienceYears:
          form.minExperienceYears === ""
            ? null
            : Number(
                form.minExperienceYears
              ),

        minSalary:
          form.minSalary === ""
            ? null
            : Number(form.minSalary),

        maxSalary:
          form.maxSalary === ""
            ? null
            : Number(form.maxSalary),

        currency:
          form.currency.trim() || "LKR",
      };

      if (editingId) {
        await updateRequisition(
          editingId,
          payload
        );
      } else {
        await createRequisition(payload);
      }

      resetForm();
      await load();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  }

  async function handleSubmit(id) {
    const confirmed = window.confirm(
      "Are you sure you want to submit this requisition to HR Manager?"
    );

    if (!confirmed) {
      return;
    }

    setError("");

    try {
      await submitRequisition(id);
      await load();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="min-h-screen bg-slate-50/50 px-6 py-7">
      <div className="mx-auto max-w-6xl space-y-7">
        {/* Header */}
        <div className="rounded-2xl bg-slate-950 px-7 py-6 text-white shadow-sm">
          <div className="flex items-center gap-4">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white/10">
              <BriefcaseBusiness size={24} />
            </div>

            <div>
              <h1 className="text-2xl font-bold">
                Job Requisitions
              </h1>

              <p className="mt-1 text-sm text-slate-300">
                Create internal hiring requests
                and submit them to HR Manager
                for approval.
              </p>
            </div>
          </div>
        </div>

        {error && (
          <div className="rounded-xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-700">
            {error}
          </div>
        )}

        {/* Form */}
        <form
          onSubmit={handleSave}
          className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm"
        >
          <div className="border-b border-slate-200 bg-slate-50 px-7 py-5">
            <h2 className="text-lg font-semibold text-slate-900">
              {editingId
                ? "Edit Job Requisition"
                : "Create Job Requisition"}
            </h2>

            <p className="mt-1 text-sm text-slate-500">
              Enter the requested job
              position details below.
            </p>
          </div>

          <div className="space-y-8 p-7">
            {/* Basic Information */}
            <section>
              <div className="mb-5 flex items-center gap-2">
                <Building2
                  size={18}
                  className="text-slate-700"
                />

                <h3 className="font-semibold text-slate-900">
                  Position Information
                </h3>
              </div>

              <div className="grid gap-5 md:grid-cols-2">
                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Position Title
                    <span className="text-red-500">
                      *
                    </span>
                  </label>

                  <input
                    required
                    maxLength={150}
                    placeholder="e.g. Software Engineer"
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.positionTitle}
                    onChange={(e) =>
                      updateField(
                        "positionTitle",
                        e.target.value
                      )
                    }
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Department
                    <span className="text-red-500">
                      *
                    </span>
                  </label>

                  <input
                    required
                    maxLength={150}
                    placeholder="e.g. Engineering"
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.department}
                    onChange={(e) =>
                      updateField(
                        "department",
                        e.target.value
                      )
                    }
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Headcount
                    <span className="text-red-500">
                      *
                    </span>
                  </label>

                  <input
                    required
                    type="number"
                    min="1"
                    max="1000"
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.headcount}
                    onChange={(e) =>
                      updateField(
                        "headcount",
                        e.target.value
                      )
                    }
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Location
                    <span className="text-red-500">
                      *
                    </span>
                  </label>

                  <div className="relative">
                    <MapPin
                      size={17}
                      className="absolute left-4 top-3.5 text-slate-400"
                    />

                    <input
                      required
                      maxLength={150}
                      placeholder="e.g. Colombo"
                      className="w-full rounded-xl border border-slate-300 py-3 pl-11 pr-4 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                      value={form.location}
                      onChange={(e) =>
                        updateField(
                          "location",
                          e.target.value
                        )
                      }
                    />
                  </div>
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Employment Type
                  </label>

                  <select
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.employmentType}
                    onChange={(e) =>
                      updateField(
                        "employmentType",
                        e.target.value
                      )
                    }
                  >
                    <option value={0}>
                      Full Time
                    </option>

                    <option value={1}>
                      Part Time
                    </option>

                    <option value={2}>
                      Contract
                    </option>

                    <option value={3}>
                      Internship
                    </option>
                  </select>
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Work Mode
                  </label>

                  <select
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.workMode}
                    onChange={(e) =>
                      updateField(
                        "workMode",
                        e.target.value
                      )
                    }
                  >
                    <option value={0}>
                      On Site
                    </option>

                    <option value={1}>
                      Remote
                    </option>

                    <option value={2}>
                      Hybrid
                    </option>
                  </select>
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Experience Level
                  </label>

                  <select
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.experienceLevel}
                    onChange={(e) =>
                      updateField(
                        "experienceLevel",
                        e.target.value
                      )
                    }
                  >
                    <option value={0}>
                      Entry
                    </option>

                    <option value={1}>
                      Junior
                    </option>

                    <option value={2}>
                      Mid
                    </option>

                    <option value={3}>
                      Senior
                    </option>

                    <option value={4}>
                      Lead
                    </option>
                  </select>
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Minimum Experience
                    (Years)
                  </label>

                  <input
                    type="number"
                    min="0"
                    max="50"
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={
                      form.minExperienceYears
                    }
                    onChange={(e) =>
                      updateField(
                        "minExperienceYears",
                        e.target.value
                      )
                    }
                  />
                </div>
              </div>
            </section>

            {/* Salary */}
            <section className="border-t border-slate-200 pt-7">
              <div className="mb-5 flex items-center gap-2">
                <CircleDollarSign
                  size={18}
                  className="text-slate-700"
                />

                <h3 className="font-semibold text-slate-900">
                  Salary Information
                </h3>
              </div>

              <div className="grid gap-5 md:grid-cols-3">
                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Minimum Salary
                  </label>

                  <input
                    type="number"
                    min="0"
                    placeholder="e.g. 120000"
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.minSalary}
                    onChange={(e) =>
                      updateField(
                        "minSalary",
                        e.target.value
                      )
                    }
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Maximum Salary
                  </label>

                  <input
                    type="number"
                    min="0"
                    placeholder="e.g. 180000"
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.maxSalary}
                    onChange={(e) =>
                      updateField(
                        "maxSalary",
                        e.target.value
                      )
                    }
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Currency
                  </label>

                  <select
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.currency}
                    onChange={(e) =>
                      updateField(
                        "currency",
                        e.target.value
                      )
                    }
                  >
                    <option value="LKR">
                      LKR
                    </option>

                    <option value="USD">
                      USD
                    </option>

                    <option value="EUR">
                      EUR
                    </option>

                    <option value="GBP">
                      GBP
                    </option>
                  </select>
                </div>
              </div>
            </section>

            {/* Description */}
            <section className="border-t border-slate-200 pt-7">
              <div className="mb-5 flex items-center gap-2">
                <FileText
                  size={18}
                  className="text-slate-700"
                />

                <h3 className="font-semibold text-slate-900">
                  Job Details
                </h3>
              </div>

              <div className="space-y-5">
                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Job Description
                  </label>

                  <textarea
                    rows={4}
                    maxLength={3000}
                    placeholder="Describe the purpose and main duties of the position..."
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.description}
                    onChange={(e) =>
                      updateField(
                        "description",
                        e.target.value
                      )
                    }
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Responsibilities
                  </label>

                  <textarea
                    rows={4}
                    maxLength={3000}
                    placeholder="List the main responsibilities..."
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={
                      form.responsibilities
                    }
                    onChange={(e) =>
                      updateField(
                        "responsibilities",
                        e.target.value
                      )
                    }
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Requirements
                  </label>

                  <textarea
                    rows={4}
                    maxLength={3000}
                    placeholder="Enter qualifications, skills and other requirements..."
                    className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                    value={form.requirements}
                    onChange={(e) =>
                      updateField(
                        "requirements",
                        e.target.value
                      )
                    }
                  />
                </div>
              </div>
            </section>

            {/* Business justification */}
            <section className="border-t border-slate-200 pt-7">
              <div className="mb-5 flex items-center gap-2">
                <Users
                  size={18}
                  className="text-slate-700"
                />

                <h3 className="font-semibold text-slate-900">
                  Business Justification
                </h3>
              </div>

              <div>
                <label className="mb-2 block text-sm font-medium text-slate-700">
                  Why is this position
                  needed?
                </label>

                <textarea
                  rows={4}
                  maxLength={2000}
                  placeholder="Explain why this role is required, such as new project demand, employee replacement or team expansion..."
                  className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-slate-900 focus:ring-2 focus:ring-slate-900/10"
                  value={form.justification}
                  onChange={(e) =>
                    updateField(
                      "justification",
                      e.target.value
                    )
                  }
                />
              </div>
            </section>

            {/* Actions */}
            <div className="flex flex-wrap gap-3 border-t border-slate-200 pt-7">
              <button
                type="submit"
                disabled={saving}
                className="inline-flex items-center gap-2 rounded-xl bg-slate-950 px-5 py-3 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
              >
                <FileText size={17} />

                {saving
                  ? "Saving..."
                  : editingId
                    ? "Update Requisition"
                    : "Save Draft"}
              </button>

              {editingId && (
                <button
                  type="button"
                  onClick={resetForm}
                  className="rounded-xl border border-slate-300 bg-white px-5 py-3 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                >
                  Cancel Editing
                </button>
              )}
            </div>
          </div>
        </form>

        {/* Existing requisitions */}
        <div>
          <div className="mb-4">
            <h2 className="text-xl font-semibold text-slate-900">
              My Requisitions
            </h2>

            <p className="text-sm text-slate-500">
              Track draft, submitted,
              approved and rejected
              requisitions.
            </p>
          </div>

          {loading ? (
            <div className="rounded-xl border bg-white p-8 text-center text-slate-500">
              Loading requisitions...
            </div>
          ) : items.length === 0 ? (
            <div className="rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center">
              <BriefcaseBusiness
                size={34}
                className="mx-auto text-slate-400"
              />

              <p className="mt-3 font-medium text-slate-700">
                No requisitions yet
              </p>

              <p className="mt-1 text-sm text-slate-500">
                Create your first job
                requisition using the
                form above.
              </p>
            </div>
          ) : (
            <div className="space-y-4">
              {items.map((item) => {
                const status =
                  getStatusLabel(
                    item.status
                  );

                const editable =
                  status === "Draft" ||
                  status === "Rejected";

                return (
                  <div
                    key={item.id}
                    className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-4">
                      <div>
                        <h3 className="text-lg font-semibold text-slate-900">
                          {
                            item.positionTitle
                          }
                        </h3>

                        <p className="mt-1 text-sm text-slate-500">
                          {item.department} •{" "}
                          {item.location}
                        </p>
                      </div>

                      <span
                        className={`rounded-full border px-3 py-1 text-xs font-semibold ${getStatusClasses(
                          item.status
                        )}`}
                      >
                        {status}
                      </span>
                    </div>

                    <div className="mt-5 grid gap-4 rounded-xl bg-slate-50 p-4 text-sm md:grid-cols-2 lg:grid-cols-4">
                      <div>
                        <p className="text-xs uppercase tracking-wide text-slate-400">
                          Headcount
                        </p>

                        <p className="mt-1 font-medium text-slate-700">
                          {item.headcount}
                        </p>
                      </div>

                      <div>
                        <p className="text-xs uppercase tracking-wide text-slate-400">
                          Employment
                        </p>

                        <p className="mt-1 font-medium text-slate-700">
                          {
                            employmentTypeLabels[
                              item
                                .employmentType
                            ]
                          }
                        </p>
                      </div>

                      <div>
                        <p className="text-xs uppercase tracking-wide text-slate-400">
                          Work Mode
                        </p>

                        <p className="mt-1 font-medium text-slate-700">
                          {
                            workModeLabels[
                              item.workMode
                            ]
                          }
                        </p>
                      </div>

                      <div>
                        <p className="text-xs uppercase tracking-wide text-slate-400">
                          Experience
                        </p>

                        <p className="mt-1 font-medium text-slate-700">
                          {
                            experienceLevelLabels[
                              item
                                .experienceLevel
                            ]
                          }
                        </p>
                      </div>
                    </div>

                    <div className="mt-4 text-sm text-slate-600">
                      <span className="font-medium">
                        Salary:
                      </span>{" "}
                      {item.minSalary ?? "-"} -{" "}
                      {item.maxSalary ?? "-"}{" "}
                      {item.currency}
                    </div>

                    {item.hrFeedback && (
                      <div className="mt-5 rounded-xl border border-red-200 bg-red-50 p-4">
                        <p className="font-semibold text-red-800">
                          HR Manager Feedback
                        </p>

                        <p className="mt-1 text-sm text-red-700">
                          {
                            item.hrFeedback
                          }
                        </p>
                      </div>
                    )}

                    <div className="mt-5 flex flex-wrap items-center gap-3">
                      {editable && (
                        <>
                          <button
                            type="button"
                            onClick={() => startEdit(item)}
                            className="rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                          >
                            Edit
                          </button>

                          <button
                            type="button"
                            disabled={agentLoadingId === item.id}
                            onClick={() => handleRunAgentAnalysis(item.id)}
                            className="inline-flex items-center gap-2 rounded-xl border border-indigo-200 bg-indigo-50/70 px-4 py-2.5 text-sm font-medium text-indigo-700 transition hover:bg-indigo-100"
                          >
                            <Sparkles size={16} />
                            {agentLoadingId === item.id ? "Analyzing..." : "AI Readiness Check"}
                          </button>

                          <button
                            type="button"
                            disabled={agentLoadingId === item.id}
                            onClick={() => handleSubmitWithApprovalGate(item.id)}
                            className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-indigo-700 shadow-xs"
                          >
                            <ShieldCheck size={16} />
                            Submit with AI Gate
                          </button>

                          <button
                            type="button"
                            onClick={() => handleSubmit(item.id)}
                            className="inline-flex items-center gap-2 rounded-xl bg-slate-950 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-slate-800"
                          >
                            <Send size={16} />
                            {status === "Rejected" ? "Resubmit to HR" : "Standard Submit"}
                          </button>
                        </>
                      )}

                      {!editable && (
                        <button
                          type="button"
                          disabled={agentLoadingId === item.id}
                          onClick={() => handleViewAgentDossier(item.id)}
                          className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
                        >
                          <Bot size={16} className="text-indigo-600" />
                          {agentLoadingId === item.id ? "Loading..." : "View AI Agent Dossier"}
                        </button>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {/* HR Agent Dossier Modal */}
      <HrRequisitionAgentDossierModal
        isOpen={isDossierOpen}
        onClose={() => setIsDossierOpen(false)}
        workflow={agentWorkflow}
      />
    </div>
  );
}