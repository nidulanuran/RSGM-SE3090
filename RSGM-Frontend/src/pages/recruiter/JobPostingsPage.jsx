import { useEffect, useState } from "react";
import {
  AlertCircle, Ban, BriefcaseBusiness, CalendarDays, CircleCheck,
  Eye, Loader2, MapPin, Pencil, Plus, Save, Sparkles, Trash2, X,
} from "lucide-react";

import { getSkills } from "../../services/skillService";
import { getMyRequisitions } from "../../services/recruiterRequisitionService";
import {
  createRecruiterJobPosting,
  deleteRecruiterJobPosting,
  getRecruiterJobPostings,
  updateRecruiterJobPosting,
  updateRecruiterJobStatus,
} from "../../services/recruiterJobPostingService";

const EMPLOYMENT_TYPES = [
  ["FullTime", "Full-Time"],
  ["PartTime", "Part-Time"],
  ["Contract", "Contract"],
  ["Internship", "Internship"],
];

const WORK_MODES = [
  ["OnSite", "On-site"],
  ["Remote", "Remote"],
  ["Hybrid", "Hybrid"],
];

const EXPERIENCE_LEVELS = ["Entry", "Junior", "Mid", "Senior"];

const STATUS_STYLES = {
  Published: "bg-emerald-50 text-emerald-600",
  Draft: "bg-neutral-100 text-neutral-500",
  Closed: "bg-red-50 text-red-600",
};

function defaultDeadline() {
  const date = new Date();
  date.setDate(date.getDate() + 30);
  return date.toISOString().slice(0, 10);
}

function emptyForm() {
  return {
    jobRequisitionId: "",
    title: "",
    location: "",
    employmentType: "FullTime",
    workMode: "OnSite",
    description: "",
    responsibilities: "",
    requirements: "",
    experienceLevel: "Entry",
    minExperienceYears: "0",
    minSalary: "",
    maxSalary: "",
    currency: "LKR",
    applicationDeadline: defaultDeadline(),
    skillIds: [],
    skillWeights: {},
  };
}

function toPayload(form) {
  const hasSalary = form.minSalary !== "" || form.maxSalary !== "";

  const skillWeights = Object.fromEntries(
    form.skillIds.map((skillId) => [
      skillId,
      Number(form.skillWeights?.[skillId] ?? 1),
    ])
  );

  return {
    ...form,
    minExperienceYears: form.employmentType === "Internship" ||
      form.minExperienceYears === "" ? null : Number(form.minExperienceYears),
    minSalary: form.minSalary === "" ? null : Number(form.minSalary),
    maxSalary: form.maxSalary === "" ? null : Number(form.maxSalary),
    currency: hasSalary ? form.currency.trim().toUpperCase() : null,
    skillWeights,
  };
}

function JobPostingsPage() {
  const [postings, setPostings] = useState([]);
  const [skills, setSkills] = useState([]);
  const [requisitions, setRequisitions] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [busyId, setBusyId] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let ignore = false;
    Promise.all([getRecruiterJobPostings(), getSkills(), getMyRequisitions()])
      .then(([jobData, skillData, requisitionData]) => {
        if (!ignore) {
          setPostings(jobData);
          setSkills(skillData.filter((skill) => skill.isActive));
          setRequisitions(requisitionData);
        }
      })
      .catch((requestError) => {
        if (!ignore) setError(requestError.message || "Unable to load job postings.");
      })
      .finally(() => {
        if (!ignore) setLoading(false);
      });
    return () => { ignore = true; };
  }, []);

  const availableRequisitions = requisitions.filter((item) =>
    (item.status === 3 || item.status === "Approved") &&
    !postings.some((posting) => posting.jobRequisitionId === item.id)
  );

  const selectRequisition = (id) => {
    const requisition = availableRequisitions.find((item) => item.id === id);
    if (!requisition) {
      setForm(emptyForm());
      return;
    }
    setForm({
      ...emptyForm(),
      jobRequisitionId: id,
      title: requisition.positionTitle,
      location: requisition.location,
      employmentType: EMPLOYMENT_TYPES[requisition.employmentType]?.[0] ?? "FullTime",
      workMode: WORK_MODES[requisition.workMode]?.[0] ?? "OnSite",
      experienceLevel: EXPERIENCE_LEVELS[requisition.experienceLevel] ?? "Entry",
      minExperienceYears: requisition.minExperienceYears?.toString() ?? "",
      minSalary: requisition.minSalary?.toString() ?? "",
      maxSalary: requisition.maxSalary?.toString() ?? "",
      currency: requisition.currency || "LKR",
      description: requisition.description ?? "",
      responsibilities: requisition.responsibilities ?? "",
      requirements: requisition.requirements ?? "",
    });
  };

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm());
    setShowForm(true);
    setError("");
  };

  const openEdit = (posting) => {
    setEditingId(posting.id);
    setForm({
      jobRequisitionId: posting.jobRequisitionId ?? "",
      title: posting.title,
      location: posting.location,
      employmentType: posting.employmentType,
      workMode: posting.workMode,
      description: posting.description ?? "",
      responsibilities: posting.responsibilities ?? "",
      requirements: posting.requirements ?? "",
      experienceLevel: posting.experienceLevel,
      minExperienceYears: posting.minExperienceYears?.toString() ?? "",
      minSalary: posting.minSalary?.toString() ?? "",
      maxSalary: posting.maxSalary?.toString() ?? "",
      currency: posting.currency ?? "LKR",
      applicationDeadline: posting.applicationDeadline ?? "",
      skillIds: posting.requiredSkills.map((skill) => skill.id),
      skillWeights: Object.fromEntries(
        posting.requiredSkills.map((skill) => [
          skill.id,
          Number(skill.weight ?? 1),
        ])
      ),
    });
    setShowForm(true);
    setError("");
  };

  const submit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    try {
      const payload = toPayload(form);
      const saved = editingId
        ? await updateRecruiterJobPosting(editingId, payload)
        : await createRecruiterJobPosting(payload);
      setPostings((current) => current.some((item) => item.id === saved.id)
        ? current.map((item) => item.id === saved.id ? saved : item)
        : [saved, ...current]);
      setShowForm(false);
    } catch (requestError) {
      setError(requestError.message || "Unable to save job posting.");
    } finally {
      setSaving(false);
    }
  };

  const changeStatus = async (posting, status) => {
    setBusyId(posting.id);
    setError("");
    try {
      const updated = await updateRecruiterJobStatus(posting.id, status);
      setPostings((current) => current.map((item) => item.id === updated.id ? updated : item));
    } catch (requestError) {
      setError(requestError.message || "Unable to change job status.");
    } finally {
      setBusyId(null);
    }
  };

  const remove = async (posting) => {
    if (!window.confirm(`Delete ${posting.title}?`)) return;
    setBusyId(posting.id);
    setError("");
    try {
      await deleteRecruiterJobPosting(posting.id);
      setPostings((current) => current.filter((item) => item.id !== posting.id));
    } catch (requestError) {
      setError(requestError.message || "Unable to delete job posting.");
    } finally {
      setBusyId(null);
    }
  };

  const toggleSkill = (skillId) => {
    setForm((current) => {
      const selected = current.skillIds.includes(skillId);

      if (selected) {
        const nextWeights = { ...current.skillWeights };
        delete nextWeights[skillId];

        return {
          ...current,
          skillIds: current.skillIds.filter((id) => id !== skillId),
          skillWeights: nextWeights,
        };
      }

      return {
        ...current,
        skillIds: [...current.skillIds, skillId],
        skillWeights: {
          ...current.skillWeights,
          [skillId]: 1,
        },
      };
    });
  };

  const changeEmploymentType = (employmentType) => {
    setForm((current) => ({
      ...current,
      employmentType,
      minExperienceYears: employmentType === "Internship"
        ? ""
        : current.minExperienceYears || "0",
    }));
  };

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-blue-50 text-blue-600 text-[11px] font-semibold">
            <Sparkles size={12} /> JOB POSTINGS
          </div>
          <h1 className="mt-4 text-3xl sm:text-4xl font-semibold tracking-tight">My Job Postings</h1>
          <p className="mt-2 text-neutral-500">Create a job from an HR-approved requisition, then publish it.</p>
        </div>
        <button type="button" onClick={openCreate} disabled={loading || availableRequisitions.length === 0}
          className="h-12 disabled:opacity-50 disabled:cursor-not-allowed px-5 rounded-xl bg-neutral-900 text-white text-sm font-semibold flex items-center justify-center gap-2">
          <Plus size={16} /> New posting
        </button>
      </div>

      {!loading && availableRequisitions.length === 0 && (
        <p className="mt-4 text-sm text-neutral-500">
          No unused approved requisitions. <a className="text-blue-600 underline" href="/recruiter/requisitions">View requisitions</a>
        </p>
      )}

      {error && (
        <div className="mt-5 flex gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">
          <AlertCircle size={17} className="shrink-0 mt-0.5" /> {error}
        </div>
      )}

      {showForm && (
        <form onSubmit={submit} className="mt-6 rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold">{editingId ? "Edit job posting" : "Create job posting"}</h2>
            <button type="button" onClick={() => setShowForm(false)}><X size={18} /></button>
          </div>

          {!editingId && (
            <Field label="Approved requisition *">
              <select required value={form.jobRequisitionId}
                onChange={(event) => selectRequisition(event.target.value)} className={inputClass}>
                <option value="">Select an approved requisition</option>
                {availableRequisitions.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.positionTitle} · {item.department} · {item.headcount} position(s)
                  </option>
                ))}
              </select>
            </Field>
          )}
          {editingId && form.jobRequisitionId && (
            <p className="mt-3 text-sm text-blue-600">Approved requisition linked. HR-approved job details are locked.</p>
          )}

          <div className="mt-1 grid sm:grid-cols-2 gap-x-4">
            <Field label="Job title *">
              <input required readOnly={Boolean(form.jobRequisitionId)} maxLength={150} value={form.title}
                onChange={(event) => setForm({ ...form, title: event.target.value })}
                placeholder="Software Engineer" className={inputClass} />
            </Field>
            <Field label="Location *">
              <input required readOnly={Boolean(form.jobRequisitionId)} maxLength={150} value={form.location}
                onChange={(event) => setForm({ ...form, location: event.target.value })}
                placeholder="Colombo, Sri Lanka" className={inputClass} />
            </Field>
            <Field label="Employment type *">
              <select disabled={Boolean(form.jobRequisitionId)} value={form.employmentType}
                onChange={(event) => changeEmploymentType(event.target.value)} className={inputClass}>
                {EMPLOYMENT_TYPES.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </Field>
            <Field label="Work mode *">
              <select disabled={Boolean(form.jobRequisitionId)} value={form.workMode}
                onChange={(event) => setForm({ ...form, workMode: event.target.value })} className={inputClass}>
                {WORK_MODES.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </Field>
            <Field label="Experience level *">
              <select disabled={Boolean(form.jobRequisitionId)} value={form.experienceLevel}
                onChange={(event) => setForm({ ...form, experienceLevel: event.target.value })} className={inputClass}>
                {EXPERIENCE_LEVELS.map((level) => <option key={level}>{level}</option>)}
              </select>
            </Field>
            {form.employmentType !== "Internship" && (
              <Field label={`Minimum experience (years)${form.experienceLevel === "Entry" ? "" : " *"}`}>
                <input required={form.experienceLevel !== "Entry"} readOnly={Boolean(form.jobRequisitionId)} type="number" min="0" max="50" value={form.minExperienceYears}
                  onChange={(event) => setForm({ ...form, minExperienceYears: event.target.value })}
                  className={inputClass} />
              </Field>
            )}
            <Field label="Application deadline *">
              <input required type="date" min={new Date().toISOString().slice(0, 10)}
                value={form.applicationDeadline}
                onChange={(event) => setForm({ ...form, applicationDeadline: event.target.value })}
                className={inputClass} />
            </Field>
          </div>

          <Field label="Description *">
            <textarea required maxLength={2000} rows={4} value={form.description}
              onChange={(event) => setForm({ ...form, description: event.target.value })}
              className={`${inputClass} mt-2 h-auto py-3`} />
          </Field>
          <div className="grid sm:grid-cols-2 gap-x-4">
            <Field label="Responsibilities *">
              <textarea required maxLength={3000} rows={5} value={form.responsibilities}
                onChange={(event) => setForm({ ...form, responsibilities: event.target.value })}
                placeholder="Develop APIs, review code..." className={`${inputClass} h-auto py-3`} />
            </Field>
            <Field label="Requirements *">
              <textarea required maxLength={3000} rows={5} value={form.requirements}
                onChange={(event) => setForm({ ...form, requirements: event.target.value })}
                placeholder="C#, .NET, SQL..." className={`${inputClass} h-auto py-3`} />
            </Field>
          </div>

          <div className="mt-5 rounded-xl bg-neutral-50 p-4">
            <p className="text-sm font-semibold text-neutral-700">Salary (optional)</p>
            <div className="grid sm:grid-cols-3 gap-x-4">
              <Field label="Minimum salary">
                <input readOnly={Boolean(form.jobRequisitionId)} type="number" min="0" step="0.01" value={form.minSalary}
                  onChange={(event) => setForm({ ...form, minSalary: event.target.value })}
                  className={inputClass} />
              </Field>
              <Field label="Maximum salary">
                <input readOnly={Boolean(form.jobRequisitionId)} type="number" min="0" step="0.01" value={form.maxSalary}
                  onChange={(event) => setForm({ ...form, maxSalary: event.target.value })}
                  className={inputClass} />
              </Field>
              <Field label="Currency">
                <input readOnly={Boolean(form.jobRequisitionId)} maxLength={3} value={form.currency}
                  onChange={(event) => setForm({ ...form, currency: event.target.value.toUpperCase() })}
                  placeholder="LKR" className={inputClass} />
              </Field>
            </div>
          </div>

          <div className="mt-5">
            <p className="text-sm font-medium text-neutral-600">Required skills *</p>
            <p className="mt-1 text-xs text-neutral-400">
              Select at least one active skill and assign its importance for candidate matching.
            </p>

            <div className="mt-3 flex flex-wrap gap-2">
              {skills.map((skill) => {
                const selected = form.skillIds.includes(skill.id);

                return (
                  <button
                    key={skill.id}
                    type="button"
                    onClick={() => toggleSkill(skill.id)}
                    className={`px-3 py-1.5 rounded-full border text-xs font-medium ${selected
                      ? "border-blue-500 bg-blue-50 text-blue-600"
                      : "border-neutral-200 text-neutral-500"}`}
                  >
                    {skill.name}
                  </button>
                );
              })}
            </div>

            {form.skillIds.length > 0 && (
              <div className="mt-4 space-y-2">
                {form.skillIds.map((skillId) => {
                  const skill = skills.find((item) => item.id === skillId);
                  if (!skill) return null;

                  return (
                    <div
                      key={skill.id}
                      className="flex flex-col gap-2 rounded-xl border border-neutral-200 bg-neutral-50 p-3 sm:flex-row sm:items-center sm:justify-between"
                    >
                      <span className="text-sm font-medium text-neutral-700">
                        {skill.name}
                      </span>

                      <label className="flex items-center gap-2 text-xs text-neutral-400">
                        Weight
                        <input
                          type="number"
                          min="0.01"
                          max="100"
                          step="0.01"
                          required
                          value={form.skillWeights?.[skill.id] ?? 1}
                          onChange={(event) =>
                            setForm((current) => ({
                              ...current,
                              skillWeights: {
                                ...current.skillWeights,
                                [skill.id]: event.target.value,
                              },
                            }))
                          }
                          className="w-24 rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm text-neutral-700 outline-none focus:border-blue-400 focus:ring-4 focus:ring-blue-100"
                        />
                      </label>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          <button disabled={saving}
            className="mt-6 h-11 px-5 rounded-xl bg-blue-600 text-white text-sm font-semibold flex items-center gap-2 disabled:opacity-50">
            {saving ? <Loader2 size={15} className="animate-spin" /> : <Save size={15} />}
            {editingId ? "Save changes" : "Save as draft"}
          </button>
        </form>
      )}

      {loading ? (
        <div className="py-20 text-center text-neutral-400">
          <Loader2 size={20} className="inline animate-spin mr-2" /> Loading job postings...
        </div>
      ) : (
        <div className="mt-8 grid sm:grid-cols-2 gap-5">
          {postings.map((posting) => (
            <article key={posting.id}
              className="rounded-2xl border border-white/70 bg-white/80 shadow-xl shadow-neutral-200/30 p-6">
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-start gap-3 min-w-0">
                  <CompanyLogo posting={posting} />
                  <div className="min-w-0">
                    <p className="font-semibold text-neutral-900">{posting.title}</p>
                    <p className="mt-1 text-sm text-neutral-500">{posting.company}</p>
                  </div>
                </div>
                <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${STATUS_STYLES[posting.status]}`}>
                  {posting.status}
                </span>
              </div>
              <div className="mt-4 flex flex-wrap gap-3 text-xs text-neutral-500">
                <span className="inline-flex items-center gap-1"><MapPin size={13} />{posting.location}</span>
                <span className="inline-flex items-center gap-1"><BriefcaseBusiness size={13} />{labelFor(EMPLOYMENT_TYPES, posting.employmentType)} · {labelFor(WORK_MODES, posting.workMode)}</span>
                <span className="inline-flex items-center gap-1"><CalendarDays size={13} />Deadline {formatDate(posting.applicationDeadline)}</span>
              </div>
              {posting.jobRequisitionId ? (
                <p className="mt-2 text-xs text-blue-600">Linked to approved requisition</p>
              ) : (
                <p className="mt-2 text-xs text-amber-700">Legacy posting · approval link required to publish</p>
              )}
              {posting.description && <p className="mt-4 text-sm text-neutral-500 line-clamp-2">{posting.description}</p>}
              <div className="mt-4 flex flex-wrap gap-1.5">
                {posting.requiredSkills.map((skill) => (
                  <span key={skill.id} className="px-2.5 py-1 rounded-full bg-blue-50 text-blue-600 text-xs">
                    {skill.name} · {Number(skill.weight ?? 1).toFixed(2)}
                  </span>
                ))}
              </div>
              <p className="mt-4 text-xs text-neutral-500">
                {posting.employmentType === "Internship"
                  ? "No experience required"
                  : `${posting.minExperienceYears} year${posting.minExperienceYears === 1 ? "" : "s"} minimum experience`}
                {formatSalary(posting) && ` · ${formatSalary(posting)}`}
              </p>
              <div className="mt-4 flex items-center gap-2 text-sm text-neutral-500">
                <Eye size={14} /> {posting.applicantCount} applicant{posting.applicantCount !== 1 ? "s" : ""}
              </div>
              <div className="mt-5 flex flex-wrap gap-2">
                {posting.status !== "Closed" && <ActionButton onClick={() => openEdit(posting)} icon={Pencil} text="Edit" />}
                {posting.status === "Draft" && posting.jobRequisitionId && (
                  <ActionButton onClick={() => changeStatus(posting, "Published")}
                    icon={CircleCheck} text="Publish" disabled={busyId === posting.id} />
                )}
                {posting.status === "Published" && (
                  <ActionButton onClick={() => changeStatus(posting, "Closed")}
                    icon={Ban} text="Close" disabled={busyId === posting.id} />
                )}
                <ActionButton onClick={() => remove(posting)} icon={Trash2} text="Delete"
                  danger disabled={busyId === posting.id} />
              </div>
            </article>
          ))}
        </div>
      )}

      {!loading && postings.length === 0 && (
        <div className="mt-8 py-16 text-center rounded-2xl border border-dashed border-neutral-300 text-neutral-400">
          You have not created any job postings yet.
        </div>
      )}
    </div>
  );
}

const inputClass = "mt-2 w-full h-11 rounded-xl border border-neutral-200 bg-white px-4 text-sm outline-none focus:border-blue-400 focus:ring-4 focus:ring-blue-100";

function Field({ label, children }) {
  return <label className="mt-4 block text-sm font-medium text-neutral-600"><span>{label}</span>{children}</label>;
}

function CompanyLogo({ posting }) {
  if (posting.companyLogoUrl) {
    return <img src={posting.companyLogoUrl} alt={`${posting.company} logo`}
      className="w-11 h-11 rounded-xl border border-neutral-100 bg-white object-contain p-1 shrink-0" />;
  }
  return (
    <div className="w-11 h-11 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center font-semibold shrink-0">
      {posting.company?.charAt(0)?.toUpperCase() || "C"}
    </div>
  );
}

function labelFor(options, value) {
  return options.find(([option]) => option === value)?.[1] ?? value;
}

function formatDate(value) {
  if (!value) return "Not set";
  return new Date(`${value}T00:00:00`).toLocaleDateString();
}

function formatSalary(posting) {
  if (posting.minSalary == null && posting.maxSalary == null) return "";
  const formatter = new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 });
  const min = posting.minSalary == null ? "" : formatter.format(posting.minSalary);
  const max = posting.maxSalary == null ? "" : formatter.format(posting.maxSalary);
  const range = min && max ? `${min}–${max}` : min || max;
  return `${posting.currency ?? ""} ${range}`.trim();
}

function ActionButton({ onClick, icon: Icon, text, danger = false, disabled = false }) {
  return (
    <button type="button" onClick={onClick} disabled={disabled}
      className={`h-9 px-3 rounded-lg border text-xs font-semibold flex items-center gap-1.5 disabled:opacity-50 ${danger
        ? "border-red-200 text-red-600 hover:bg-red-50"
        : "border-neutral-200 text-neutral-600 hover:bg-neutral-50"}`}>
      <Icon size={13} /> {text}
    </button>
  );
}

export default JobPostingsPage;
