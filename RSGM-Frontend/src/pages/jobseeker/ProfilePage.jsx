import { useEffect, useRef, useState } from "react";
import {
  AlertCircle, BriefcaseBusiness, CircleCheck, FileText, GraduationCap, KeyRound,
  Loader2, Pencil, Plus, Sparkles, Trash2, Upload, X,
} from "lucide-react";
import { useNavigate } from "react-router-dom";

import { logout } from "../../services/authService";
import {
  changePassword, createEducationRecord, createWorkExperience, deleteAccount,
  deleteEducationRecord, deleteWorkExperience, getEducationRecords, getProfile,
  getWorkExperiences, updateEducationRecord, updateProfile, updateWorkExperience,
} from "../../services/jobSeekerProfileService";
import {
  addMySkill,
  getMySkills,
  removeMySkill,
  updateMySkillProficiency,
} from "../../services/jobSeekerSkillService";
import { getSkills } from "../../services/skillService";
import { deleteCv, getCv, uploadCv } from "../../services/jobSeekerCvService";
import {
  firstValidationMessage,
  validateCv,
  validateEducation,
  validatePasswordChange,
  validateProfile,
  validateSkill,
  validateWorkExperience,
} from "../../utils/jobSeekerValidation";

const EMPTY_EDUCATION = {
  institution: "", degree: "", fieldOfStudy: "", startDate: "", endDate: "",
  isCurrent: false, description: "",
};

const EMPTY_EXPERIENCE = {
  jobTitle: "", companyName: "", location: "", startDate: "", endDate: "",
  isCurrent: false, description: "",
};

function ProfilePage() {
  const navigate = useNavigate();
  const [profile, setProfile] = useState({
    fullName: "", headline: "", location: "", bio: "",
    linkedInUrl: "", gitHubUrl: "", portfolioUrl: "",
  });
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [saveError, setSaveError] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [isEditingProfile, setIsEditingProfile] = useState(false);
  const profileBeforeEditRef = useRef(null);

  const [skills, setSkills] = useState([]);
  const [skillsLoading, setSkillsLoading] = useState(true);
  const [skillsError, setSkillsError] = useState("");
  const [catalog, setCatalog] = useState([]);
  const [selectedSkillId, setSelectedSkillId] = useState("");
  const [selectedProficiencyLevel, setSelectedProficiencyLevel] = useState(3);
  const [isAddingSkill, setIsAddingSkill] = useState(false);
  const [updatingSkillId, setUpdatingSkillId] = useState(null);

  const [educationRecords, setEducationRecords] = useState([]);
  const [educationForm, setEducationForm] = useState(null);
  const [educationEditingId, setEducationEditingId] = useState(null);
  const [educationLoading, setEducationLoading] = useState(true);
  const [educationSaving, setEducationSaving] = useState(false);
  const [educationError, setEducationError] = useState("");

  const [workExperiences, setWorkExperiences] = useState([]);
  const [experienceForm, setExperienceForm] = useState(null);
  const [experienceEditingId, setExperienceEditingId] = useState(null);
  const [experienceLoading, setExperienceLoading] = useState(true);
  const [experienceSaving, setExperienceSaving] = useState(false);
  const [experienceError, setExperienceError] = useState("");

  const [cv, setCv] = useState(null);
  const [cvLoading, setCvLoading] = useState(true);
  const [cvError, setCvError] = useState("");
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef(null);

  const [passwordForm, setPasswordForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const [passwordError, setPasswordError] = useState("");
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [passwordChanged, setPasswordChanged] = useState(false);

  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [deletePassword, setDeletePassword] = useState("");
  const [deleteConfirmation, setDeleteConfirmation] = useState("");
  const [deleteError, setDeleteError] = useState("");
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    let ignore = false;

    Promise.all([getMySkills(), getSkills()])
      .then(([mySkills, allSkills]) => {
        if (ignore) return;
        setSkills(mySkills);
        setCatalog(allSkills.filter((s) => s.isActive));
      })
      .catch((err) => {
        if (!ignore) setSkillsError(err.message || "Unable to load skills.");
      })
      .finally(() => {
        if (!ignore) setSkillsLoading(false);
      });

    return () => {
      ignore = true;
    };
  }, []);

  useEffect(() => {
    let ignore = false;

    getProfile()
      .then((data) => {
        if (ignore) return;
        setProfile({
          fullName: data.fullName ?? "",
          headline: data.headline ?? "",
          location: data.location ?? "",
          bio: data.bio ?? "",
          linkedInUrl: data.linkedInUrl ?? "",
          gitHubUrl: data.gitHubUrl ?? "",
          portfolioUrl: data.portfolioUrl ?? "",
        });
      })
      .catch((err) => {
        if (!ignore) setLoadError(err.message || "Unable to load profile.");
      })
      .finally(() => {
        if (!ignore) setIsLoading(false);
      });

    return () => {
      ignore = true;
    };
  }, []);

  useEffect(() => {
    let ignore = false;
    getEducationRecords()
      .then((records) => { if (!ignore) setEducationRecords(records); })
      .catch((err) => { if (!ignore) setEducationError(err.message || "Unable to load education."); })
      .finally(() => { if (!ignore) setEducationLoading(false); });
    return () => { ignore = true; };
  }, []);

  useEffect(() => {
    let ignore = false;
    getWorkExperiences()
      .then((records) => { if (!ignore) setWorkExperiences(records); })
      .catch((err) => { if (!ignore) setExperienceError(err.message || "Unable to load work experience."); })
      .finally(() => { if (!ignore) setExperienceLoading(false); });
    return () => { ignore = true; };
  }, []);

  useEffect(() => {
    let ignore = false;

    getCv()
      .then((data) => {
        if (!ignore) setCv(data);
      })
      .catch((err) => {
        if (!ignore) setCvError(err.message || "Unable to load CV.");
      })
      .finally(() => {
        if (!ignore) setCvLoading(false);
      });

    return () => {
      ignore = true;
    };
  }, []);

  const updateField = (key, value) => {
    setProfile((prev) => ({ ...prev, [key]: value }));
    setSaved(false);
  };

  const beginProfileEdit = () => {
    profileBeforeEditRef.current = { ...profile };
    setSaveError("");
    setSaved(false);
    setIsEditingProfile(true);
  };

  const cancelProfileEdit = () => {
    if (profileBeforeEditRef.current) {
      setProfile(profileBeforeEditRef.current);
    }

    profileBeforeEditRef.current = null;
    setSaveError("");
    setSaved(false);
    setIsEditingProfile(false);
  };

  const handleSaveProfile = async () => {
    setSaveError("");

    const validationErrors = validateProfile(profile);
    if (Object.keys(validationErrors).length > 0) {
      setSaveError(firstValidationMessage(validationErrors));
      return;
    }

    setIsSaving(true);

    try {
      const updated = await updateProfile({
        fullName: profile.fullName,
        headline: profile.headline,
        location: profile.location,
        bio: profile.bio,
        linkedInUrl: profile.linkedInUrl.trim() || null,
        gitHubUrl: profile.gitHubUrl.trim() || null,
        portfolioUrl: profile.portfolioUrl.trim() || null,
      });
      setProfile({
        fullName: updated.fullName ?? "",
        headline: updated.headline ?? "",
        location: updated.location ?? "",
        bio: updated.bio ?? "",
        linkedInUrl: updated.linkedInUrl ?? "",
        gitHubUrl: updated.gitHubUrl ?? "",
        portfolioUrl: updated.portfolioUrl ?? "",
      });
      profileBeforeEditRef.current = null;
      setIsEditingProfile(false);
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
    } catch (err) {
      setSaveError(err.message || "Unable to save profile.");
    } finally {
      setIsSaving(false);
    }
  };

  const addedSkillIds = new Set(skills.map((s) => s.skillId));
  const availableCatalog = catalog.filter((s) => !addedSkillIds.has(s.id));

  const addSkill = async () => {
    setSkillsError("");

    const validationErrors = validateSkill(
      selectedSkillId,
      selectedProficiencyLevel,
      skills
    );

    if (Object.keys(validationErrors).length > 0) {
      setSkillsError(firstValidationMessage(validationErrors));
      return;
    }

    setIsAddingSkill(true);

    try {
      const added = await addMySkill(selectedSkillId, selectedProficiencyLevel);
      setSkills((prev) => [...prev, added].sort((a, b) => a.name.localeCompare(b.name)));
      setSelectedSkillId("");
      setSelectedProficiencyLevel(3);
    } catch (err) {
      setSkillsError(err.message || "Unable to add skill.");
    } finally {
      setIsAddingSkill(false);
    }
  };

  const removeSkill = async (skillId) => {
    setSkillsError("");

    try {
      await removeMySkill(skillId);
      setSkills((prev) => prev.filter((s) => s.skillId !== skillId));
    } catch (err) {
      setSkillsError(err.message || "Unable to remove skill.");
    }
  };

  const updateSkillProficiency = async (skillId, proficiencyLevel) => {
    setSkillsError("");
    setUpdatingSkillId(skillId);

    try {
      const updated = await updateMySkillProficiency(skillId, proficiencyLevel);
      setSkills((prev) =>
        prev.map((skill) => (skill.skillId === skillId ? updated : skill))
      );
    } catch (err) {
      setSkillsError(err.message || "Unable to update skill proficiency.");
    } finally {
      setUpdatingSkillId(null);
    }
  };

  const handleFileSelect = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setCvError("");

    const validationError = validateCv(file);
    if (validationError) {
      setCvError(validationError);
      e.target.value = "";
      return;
    }

    setIsUploading(true);

    try {
      const uploaded = await uploadCv(file);
      setCv(uploaded);
    } catch (err) {
      setCvError(err.message || "Unable to upload CV.");
    } finally {
      setIsUploading(false);
      // reset so selecting the same file again still fires onChange
      e.target.value = "";
    }
  };

  const handleRemoveCv = async () => {
    setCvError("");

    try {
      await deleteCv();
      setCv(null);
    } catch (err) {
      setCvError(err.message || "Unable to remove CV.");
    }
  };

  const updatePasswordField = (key, value) => {
    setPasswordForm((prev) => ({ ...prev, [key]: value }));
    setPasswordChanged(false);
  };

  const handleChangePassword = async () => {
    setPasswordError("");

    const validationErrors = validatePasswordChange(
      passwordForm.currentPassword,
      passwordForm.newPassword,
      passwordForm.confirmPassword
    );

    if (Object.keys(validationErrors).length > 0) {
      setPasswordError(firstValidationMessage(validationErrors));
      return;
    }

    setIsChangingPassword(true);

    try {
      await changePassword(passwordForm.currentPassword, passwordForm.newPassword);
      setPasswordForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
      setPasswordChanged(true);
      setTimeout(() => setPasswordChanged(false), 2500);
    } catch (err) {
      setPasswordError(err.message || "Unable to update password.");
    } finally {
      setIsChangingPassword(false);
    }
  };

  const openEducationCreate = () => {
    setEducationEditingId(null);
    setEducationForm({ ...EMPTY_EDUCATION });
    setEducationError("");
  };

  const openEducationEdit = (record) => {
    setEducationEditingId(record.id);
    setEducationForm({
      institution: record.institution,
      degree: record.degree,
      fieldOfStudy: record.fieldOfStudy ?? "",
      startDate: record.startDate,
      endDate: record.endDate ?? "",
      isCurrent: record.isCurrent,
      description: record.description ?? "",
    });
    setEducationError("");
  };

  const saveEducation = async (event) => {
    event.preventDefault();
    setEducationError("");

    const validationErrors = validateEducation(educationForm);
    if (Object.keys(validationErrors).length > 0) {
      setEducationError(firstValidationMessage(validationErrors));
      return;
    }

    setEducationSaving(true);
    try {
      const payload = {
        ...educationForm,
        endDate: educationForm.isCurrent ? null : educationForm.endDate,
      };
      const savedRecord = educationEditingId
        ? await updateEducationRecord(educationEditingId, payload)
        : await createEducationRecord(payload);
      setEducationRecords((current) => sortCareerRecords(
        current.some((item) => item.id === savedRecord.id)
          ? current.map((item) => item.id === savedRecord.id ? savedRecord : item)
          : [...current, savedRecord]
      ));
      setEducationForm(null);
      setEducationEditingId(null);
    } catch (err) {
      setEducationError(err.message || "Unable to save education.");
    } finally {
      setEducationSaving(false);
    }
  };

  const removeEducation = async (record) => {
    if (!window.confirm(`Delete ${record.degree} at ${record.institution}?`)) return;
    setEducationError("");
    try {
      await deleteEducationRecord(record.id);
      setEducationRecords((current) => current.filter((item) => item.id !== record.id));
    } catch (err) {
      setEducationError(err.message || "Unable to delete education.");
    }
  };

  const openExperienceCreate = () => {
    setExperienceEditingId(null);
    setExperienceForm({ ...EMPTY_EXPERIENCE });
    setExperienceError("");
  };

  const openExperienceEdit = (record) => {
    setExperienceEditingId(record.id);
    setExperienceForm({
      jobTitle: record.jobTitle,
      companyName: record.companyName,
      location: record.location ?? "",
      startDate: record.startDate,
      endDate: record.endDate ?? "",
      isCurrent: record.isCurrent,
      description: record.description ?? "",
    });
    setExperienceError("");
  };

  const saveExperience = async (event) => {
    event.preventDefault();
    setExperienceError("");

    const validationErrors = validateWorkExperience(experienceForm);
    if (Object.keys(validationErrors).length > 0) {
      setExperienceError(firstValidationMessage(validationErrors));
      return;
    }

    setExperienceSaving(true);
    try {
      const payload = {
        ...experienceForm,
        endDate: experienceForm.isCurrent ? null : experienceForm.endDate,
      };
      const savedRecord = experienceEditingId
        ? await updateWorkExperience(experienceEditingId, payload)
        : await createWorkExperience(payload);
      setWorkExperiences((current) => sortCareerRecords(
        current.some((item) => item.id === savedRecord.id)
          ? current.map((item) => item.id === savedRecord.id ? savedRecord : item)
          : [...current, savedRecord]
      ));
      setExperienceForm(null);
      setExperienceEditingId(null);
    } catch (err) {
      setExperienceError(err.message || "Unable to save work experience.");
    } finally {
      setExperienceSaving(false);
    }
  };

  const removeExperience = async (record) => {
    if (!window.confirm(`Delete ${record.jobTitle} at ${record.companyName}?`)) return;
    setExperienceError("");
    try {
      await deleteWorkExperience(record.id);
      setWorkExperiences((current) => current.filter((item) => item.id !== record.id));
    } catch (err) {
      setExperienceError(err.message || "Unable to delete work experience.");
    }
  };

  const closeDeleteDialog = () => {
    if (isDeleting) return;
    setShowDeleteDialog(false);
    setDeletePassword("");
    setDeleteConfirmation("");
    setDeleteError("");
  };

  const handleDeleteAccount = async () => {
    setDeleteError("");

    if (!deletePassword) {
      setDeleteError("Enter your current password.");
      return;
    }

    if (deleteConfirmation !== "DELETE") {
      setDeleteError('Type "DELETE" exactly to confirm.');
      return;
    }

    setIsDeleting(true);

    try {
      await deleteAccount(deletePassword);
      logout();
      navigate("/login", {
        replace: true,
        state: { message: "Your account was deleted successfully." },
      });
    } catch (err) {
      setDeleteError(err.message || "Unable to delete account.");
      setIsDeleting(false);
    }
  };

  return (
    <div>
      <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-violet-50 text-violet-600 text-[11px] font-semibold">
        <Sparkles size={12} />
        YOUR PROFILE
      </div>

      <h1 className="mt-4 text-3xl sm:text-4xl font-semibold tracking-tight">Profile</h1>
      <p className="mt-2 text-neutral-500">
        Keep your details, CV, and skills up to date for the best job matches.
      </p>

      <div className="mt-8 grid gap-6 xl:grid-cols-2 xl:items-start">
        {/* ================= PERSONAL DETAILS ================= */}

        <div className="rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-lg font-semibold tracking-tight">Personal details</h2>
            <p className="mt-1 text-xs text-neutral-400">
              {isEditingProfile
                ? "Edit your details, then save or cancel your changes."
                : "Click Edit to update your personal details."}
            </p>
          </div>

          {!isLoading && !loadError && !isEditingProfile && (
            <button
              type="button"
              onClick={beginProfileEdit}
              className="h-10 px-4 rounded-xl border border-neutral-200 bg-white text-sm font-semibold text-neutral-700 flex items-center gap-2 hover:bg-neutral-50 active:scale-[0.99] transition"
            >
              <Pencil size={15} />
              Edit
            </button>
          )}
        </div>

        {isLoading ? (
          <div className="mt-6 flex items-center gap-2 text-sm text-neutral-400">
            <Loader2 size={16} className="animate-spin" />
            Loading profile...
          </div>
        ) : loadError ? (
          <div className="mt-5 flex items-start gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">
            <AlertCircle size={17} className="mt-0.5 shrink-0" />
            <span>{loadError}</span>
          </div>
        ) : (
          <>
            <div className="mt-5 space-y-4">
              <Field label="Full name">
                <input
                  value={profile.fullName}
                  maxLength={100}
                  onChange={(e) => updateField("fullName", e.target.value)}
                  disabled={!isEditingProfile}
                  className={`w-full h-12 rounded-xl border px-4 text-sm outline-none transition ${isEditingProfile ? "border-neutral-200 bg-neutral-50/70 focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100" : "border-neutral-200 bg-neutral-100/70 text-neutral-600 cursor-default"}`}
                />
              </Field>

              <Field label="Headline">
                <input
                  value={profile.headline}
                  maxLength={150}
                  onChange={(e) => updateField("headline", e.target.value)}
                  disabled={!isEditingProfile}
                  placeholder="e.g. Frontend Engineer"
                  className={`w-full h-12 rounded-xl border px-4 text-sm outline-none transition ${isEditingProfile ? "border-neutral-200 bg-neutral-50/70 focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100" : "border-neutral-200 bg-neutral-100/70 text-neutral-600 cursor-default"}`}
                />
              </Field>

              <Field label="Location">
                <input
                  value={profile.location}
                  maxLength={150}
                  onChange={(e) => updateField("location", e.target.value)}
                  disabled={!isEditingProfile}
                  className={`w-full h-12 rounded-xl border px-4 text-sm outline-none transition ${isEditingProfile ? "border-neutral-200 bg-neutral-50/70 focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100" : "border-neutral-200 bg-neutral-100/70 text-neutral-600 cursor-default"}`}
                />
              </Field>

              <Field label="About you">
                <textarea
                  value={profile.bio}
                  maxLength={1000}
                  onChange={(e) => updateField("bio", e.target.value)}
                  disabled={!isEditingProfile}
                  rows={3}
                  className={`w-full rounded-xl border px-4 py-3 text-sm outline-none transition resize-none ${isEditingProfile ? "border-neutral-200 bg-neutral-50/70 focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100" : "border-neutral-200 bg-neutral-100/70 text-neutral-600 cursor-default"}`}
                />
              </Field>

              <Field label="LinkedIn URL">
                <input
                  type="url"
                  value={profile.linkedInUrl}
                  maxLength={500}
                  onChange={(e) => updateField("linkedInUrl", e.target.value)}
                  disabled={!isEditingProfile}
                  placeholder="https://www.linkedin.com/in/your-name"
                  className={`w-full h-12 rounded-xl border px-4 text-sm outline-none transition ${isEditingProfile ? "border-neutral-200 bg-neutral-50/70 focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100" : "border-neutral-200 bg-neutral-100/70 text-neutral-600 cursor-default"}`}
                />
              </Field>

              <Field label="GitHub URL">
                <input
                  type="url"
                  value={profile.gitHubUrl}
                  maxLength={500}
                  onChange={(e) => updateField("gitHubUrl", e.target.value)}
                  disabled={!isEditingProfile}
                  placeholder="https://github.com/your-username"
                  className={`w-full h-12 rounded-xl border px-4 text-sm outline-none transition ${isEditingProfile ? "border-neutral-200 bg-neutral-50/70 focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100" : "border-neutral-200 bg-neutral-100/70 text-neutral-600 cursor-default"}`}
                />
              </Field>

              <Field label="Portfolio URL">
                <input
                  type="url"
                  value={profile.portfolioUrl}
                  maxLength={500}
                  onChange={(e) => updateField("portfolioUrl", e.target.value)}
                  disabled={!isEditingProfile}
                  placeholder="https://your-portfolio.com"
                  className={`w-full h-12 rounded-xl border px-4 text-sm outline-none transition ${isEditingProfile ? "border-neutral-200 bg-neutral-50/70 focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100" : "border-neutral-200 bg-neutral-100/70 text-neutral-600 cursor-default"}`}
                />
              </Field>
            </div>

            {saveError && (
              <div className="mt-4 flex items-start gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">
                <AlertCircle size={17} className="mt-0.5 shrink-0" />
                <span>{saveError}</span>
              </div>
            )}

            {(isEditingProfile || saved) && (
              <div className="mt-6 flex items-center gap-3">
                {isEditingProfile && (
                  <>
                    <button
                      type="button"
                      onClick={handleSaveProfile}
                      disabled={isSaving}
                      className="h-11 px-5 rounded-xl bg-neutral-900 text-white text-sm font-semibold flex items-center gap-2 hover:bg-neutral-800 active:scale-[0.99] transition disabled:opacity-60"
                    >
                      {isSaving && <Loader2 size={15} className="animate-spin" />}
                      Save changes
                    </button>

                    <button
                      type="button"
                      onClick={cancelProfileEdit}
                      disabled={isSaving}
                      className="h-11 px-5 rounded-xl border border-neutral-200 bg-white text-neutral-700 text-sm font-semibold hover:bg-neutral-50 active:scale-[0.99] transition disabled:opacity-60"
                    >
                      Cancel
                    </button>
                  </>
                )}

                {saved && !isEditingProfile && (
                  <span className="flex items-center gap-1.5 text-sm text-emerald-600 font-medium">
                    <CircleCheck size={15} />
                    Saved
                  </span>
                )}
              </div>
            )}
          </>
        )}
      </div>

        <div className="space-y-6">
          {/* ================= EDUCATION ================= */}

          <div className="rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6">
        <SectionHeader
          icon={GraduationCap}
          title="Education"
          buttonText="Add education"
          onAdd={openEducationCreate}
        />

        {educationError && <InlineError message={educationError} />}

        {educationForm && (
          <EducationForm
            form={educationForm}
            setForm={setEducationForm}
            editing={Boolean(educationEditingId)}
            saving={educationSaving}
            onSubmit={saveEducation}
            onCancel={() => { setEducationForm(null); setEducationEditingId(null); }}
          />
        )}

        {educationLoading ? (
          <LoadingRow text="Loading education..." />
        ) : (
          <div className="mt-5 space-y-3">
            {educationRecords.map((record) => (
              <article key={record.id} className="rounded-xl border border-neutral-200 bg-neutral-50/60 p-4">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <h3 className="font-semibold text-neutral-900">{record.degree}</h3>
                    <p className="mt-1 text-sm text-neutral-600">
                      {record.institution}{record.fieldOfStudy ? ` · ${record.fieldOfStudy}` : ""}
                    </p>
                    <p className="mt-1 text-xs text-neutral-400">{formatCareerPeriod(record)}</p>
                  </div>
                  <RecordActions onEdit={() => openEducationEdit(record)} onDelete={() => removeEducation(record)} />
                </div>
                {record.description && <p className="mt-3 text-sm text-neutral-500 whitespace-pre-line">{record.description}</p>}
              </article>
            ))}
            {educationRecords.length === 0 && !educationForm && (
              <p className="text-sm text-neutral-400">No education records added yet.</p>
            )}
          </div>
        )}
      </div>

          {/* ================= WORK EXPERIENCE ================= */}

          <div className="rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6">
        <SectionHeader
          icon={BriefcaseBusiness}
          title="Work experience"
          buttonText="Add experience"
          onAdd={openExperienceCreate}
        />

        {experienceError && <InlineError message={experienceError} />}

        {experienceForm && (
          <WorkExperienceForm
            form={experienceForm}
            setForm={setExperienceForm}
            editing={Boolean(experienceEditingId)}
            saving={experienceSaving}
            onSubmit={saveExperience}
            onCancel={() => { setExperienceForm(null); setExperienceEditingId(null); }}
          />
        )}

        {experienceLoading ? (
          <LoadingRow text="Loading work experience..." />
        ) : (
          <div className="mt-5 space-y-3">
            {workExperiences.map((record) => (
              <article key={record.id} className="rounded-xl border border-neutral-200 bg-neutral-50/60 p-4">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <h3 className="font-semibold text-neutral-900">{record.jobTitle}</h3>
                    <p className="mt-1 text-sm text-neutral-600">
                      {record.companyName}{record.location ? ` · ${record.location}` : ""}
                    </p>
                    <p className="mt-1 text-xs text-neutral-400">{formatCareerPeriod(record)}</p>
                  </div>
                  <RecordActions onEdit={() => openExperienceEdit(record)} onDelete={() => removeExperience(record)} />
                </div>
                {record.description && <p className="mt-3 text-sm text-neutral-500 whitespace-pre-line">{record.description}</p>}
              </article>
            ))}
            {workExperiences.length === 0 && !experienceForm && (
              <p className="text-sm text-neutral-400">No work experience added yet.</p>
            )}
          </div>
        )}
          </div>
        </div>
      </div>

      {/* ================= CV UPLOAD ================= */}

      <div className="mt-6 rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6 max-w-2xl">
        <h2 className="text-lg font-semibold tracking-tight">CV / Resume</h2>

        {cvLoading ? (
          <div className="mt-4 flex items-center gap-2 text-sm text-neutral-400">
            <Loader2 size={16} className="animate-spin" />
            Loading CV...
          </div>
        ) : cv ? (
          <div className="mt-4 flex items-center justify-between gap-4 p-4 rounded-xl border border-neutral-200 bg-neutral-50/60">
            <div className="flex items-center gap-3 min-w-0">
              <div className="w-10 h-10 rounded-xl bg-violet-100 flex items-center justify-center shrink-0">
                <FileText size={17} className="text-violet-600" />
              </div>
              <div className="min-w-0">
                <p className="text-sm font-medium text-neutral-900 truncate">{cv.fileName}</p>
                <p className="text-xs text-neutral-400">
                  Uploaded {new Date(cv.uploadedAt).toLocaleDateString()}
                </p>
              </div>
            </div>

            <button
              onClick={handleRemoveCv}
              className="text-xs font-semibold text-red-600 hover:text-red-700 transition shrink-0"
            >
              Remove
            </button>
          </div>
        ) : (
          <p className="mt-4 text-sm text-neutral-400">No CV uploaded yet.</p>
        )}

        {cvError && (
          <div className="mt-3 flex items-start gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">
            <AlertCircle size={17} className="mt-0.5 shrink-0" />
            <span>{cvError}</span>
          </div>
        )}

        <input
          ref={fileInputRef}
          type="file"
          accept=".pdf,.doc,.docx"
          onChange={handleFileSelect}
          className="hidden"
        />

        <button
          onClick={() => fileInputRef.current?.click()}
          disabled={isUploading}
          className="mt-4 h-11 px-5 rounded-xl border border-neutral-200 text-sm font-semibold text-neutral-600 hover:bg-neutral-100 transition flex items-center gap-2 disabled:opacity-60"
        >
          {isUploading ? <Loader2 size={15} className="animate-spin" /> : <Upload size={15} />}
          {isUploading ? "Uploading..." : cv ? "Replace CV" : "Upload CV"}
        </button>

        <p className="mt-2 text-xs text-neutral-400">PDF, DOC, or DOCX. Max 5 MB.</p>
      </div>

      {/* ================= SKILLS ================= */}

      <div className="mt-6 rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6 max-w-2xl">
        <h2 className="text-lg font-semibold tracking-tight">Skills</h2>
        <p className="mt-1 text-sm text-neutral-500">
          Add your skills and choose a proficiency level from Beginner to Expert.
        </p>

        {skillsLoading ? (
          <div className="mt-4 flex items-center gap-2 text-sm text-neutral-400">
            <Loader2 size={16} className="animate-spin" />
            Loading skills...
          </div>
        ) : (
          <>
            <div className="mt-4 space-y-3">
              {skills.map((skill) => (
                <div
                  key={skill.skillId}
                  className="flex flex-col gap-3 rounded-xl border border-neutral-200 bg-neutral-50/70 p-3 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div>
                    <p className="text-sm font-semibold text-neutral-800">{skill.name}</p>
                    <p className="mt-0.5 text-xs text-neutral-400">
                      {getProficiencyLabel(skill.proficiencyLevel)}
                    </p>
                  </div>

                  <div className="flex items-center gap-2">
                    <select
                      value={skill.proficiencyLevel ?? 3}
                      disabled={updatingSkillId === skill.skillId}
                      onChange={(event) =>
                        updateSkillProficiency(skill.skillId, Number(event.target.value))
                      }
                      className="h-10 rounded-xl border border-neutral-200 bg-white px-3 text-sm outline-none focus:border-violet-400 focus:ring-4 focus:ring-violet-100 disabled:opacity-60"
                    >
                      <option value={1}>Beginner</option>
                      <option value={2}>Basic</option>
                      <option value={3}>Intermediate</option>
                      <option value={4}>Advanced</option>
                      <option value={5}>Expert</option>
                    </select>

                    {updatingSkillId === skill.skillId && (
                      <Loader2 size={15} className="animate-spin text-neutral-400" />
                    )}

                    <button
                      type="button"
                      onClick={() => removeSkill(skill.skillId)}
                      className="flex h-10 w-10 items-center justify-center rounded-xl border border-neutral-200 text-neutral-400 transition hover:bg-red-50 hover:text-red-500"
                      aria-label={`Remove ${skill.name}`}
                    >
                      <X size={14} />
                    </button>
                  </div>
                </div>
              ))}

              {skills.length === 0 && (
                <p className="text-sm text-neutral-400">No skills added yet.</p>
              )}
            </div>

            {skillsError && (
              <div className="mt-3 flex items-start gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">
                <AlertCircle size={17} className="mt-0.5 shrink-0" />
                <span>{skillsError}</span>
              </div>
            )}

            <div className="mt-4 grid gap-2 sm:grid-cols-[1fr_180px_auto]">
              <select
                value={selectedSkillId}
                onChange={(event) => setSelectedSkillId(event.target.value)}
                className="h-11 rounded-xl border border-neutral-200 bg-neutral-50/70 px-4 text-sm outline-none focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100 transition"
              >
                <option value="">Select a skill...</option>
                {availableCatalog.map((skill) => (
                  <option key={skill.id} value={skill.id}>
                    {skill.name}
                  </option>
                ))}
              </select>

              <select
                value={selectedProficiencyLevel}
                onChange={(event) => setSelectedProficiencyLevel(Number(event.target.value))}
                className="h-11 rounded-xl border border-neutral-200 bg-neutral-50/70 px-4 text-sm outline-none focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100 transition"
              >
                <option value={1}>Beginner</option>
                <option value={2}>Basic</option>
                <option value={3}>Intermediate</option>
                <option value={4}>Advanced</option>
                <option value={5}>Expert</option>
              </select>

              <button
                type="button"
                onClick={addSkill}
                disabled={isAddingSkill || !selectedSkillId}
                className="h-11 px-4 rounded-xl bg-neutral-900 text-white text-sm font-semibold flex items-center justify-center gap-1.5 hover:bg-neutral-800 active:scale-[0.99] transition disabled:opacity-60"
              >
                {isAddingSkill ? (
                  <Loader2 size={14} className="animate-spin" />
                ) : (
                  <Plus size={14} />
                )}
                Add
              </button>
            </div>

            <div className="mt-3 grid grid-cols-2 gap-2 text-xs text-neutral-400 sm:grid-cols-5">
              <span>1 · Beginner</span>
              <span>2 · Basic</span>
              <span>3 · Intermediate</span>
              <span>4 · Advanced</span>
              <span>5 · Expert</span>
            </div>

            {availableCatalog.length === 0 && !skillsLoading && (
              <p className="mt-2 text-xs text-neutral-400">
                You&apos;ve added every skill currently in the catalog.
              </p>
            )}
          </>
        )}
      </div>

      {/* ================= PASSWORD ================= */}

      <div className="mt-6 rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30 p-6 max-w-2xl">
        <h2 className="text-lg font-semibold tracking-tight">Password</h2>
        <p className="mt-1 text-sm text-neutral-500">
          Update the password you use to sign in.
        </p>

        <div className="mt-5 space-y-4">
          <Field label="Current password">
            <input
              type="password"
              value={passwordForm.currentPassword}
              onChange={(e) => updatePasswordField("currentPassword", e.target.value)}
              className="w-full h-12 rounded-xl border border-neutral-200 bg-neutral-50/70 px-4 text-sm outline-none focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100 transition"
            />
          </Field>

          <Field label="New password">
            <input
              type="password"
              value={passwordForm.newPassword}
              onChange={(e) => updatePasswordField("newPassword", e.target.value)}
              className="w-full h-12 rounded-xl border border-neutral-200 bg-neutral-50/70 px-4 text-sm outline-none focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100 transition"
            />
          </Field>

          <Field label="Confirm new password">
            <input
              type="password"
              value={passwordForm.confirmPassword}
              onChange={(e) => updatePasswordField("confirmPassword", e.target.value)}
              className="w-full h-12 rounded-xl border border-neutral-200 bg-neutral-50/70 px-4 text-sm outline-none focus:bg-white focus:border-violet-400 focus:ring-4 focus:ring-violet-100 transition"
            />
          </Field>
        </div>

        {passwordError && (
          <div className="mt-4 flex items-start gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">
            <AlertCircle size={17} className="mt-0.5 shrink-0" />
            <span>{passwordError}</span>
          </div>
        )}

        <div className="mt-6 flex items-center gap-3">
          <button
            onClick={handleChangePassword}
            disabled={isChangingPassword}
            className="h-11 px-5 rounded-xl bg-neutral-900 text-white text-sm font-semibold flex items-center gap-2 hover:bg-neutral-800 active:scale-[0.99] transition disabled:opacity-60"
          >
            {isChangingPassword ? <Loader2 size={15} className="animate-spin" /> : <KeyRound size={15} />}
            Update password
          </button>

          {passwordChanged && (
            <span className="flex items-center gap-1.5 text-sm text-emerald-600 font-medium">
              <CircleCheck size={15} />
              Updated
            </span>
          )}
        </div>
      </div>

      {/* ================= DANGER ZONE ================= */}

      <div className="mt-6 rounded-2xl border border-red-200 bg-red-50/70 p-6 max-w-2xl">
        <h2 className="text-lg font-semibold tracking-tight text-red-700">Danger zone</h2>
        <p className="mt-1 text-sm text-red-600/80">
          Permanently delete your account, profile, skills, CV, and job applications.
          This action cannot be undone.
        </p>
        <button
          onClick={() => setShowDeleteDialog(true)}
          className="mt-5 h-11 px-5 rounded-xl bg-red-600 text-white text-sm font-semibold flex items-center gap-2 hover:bg-red-700 active:scale-[0.99] transition"
        >
          <Trash2 size={15} />
          Delete account
        </button>
      </div>

      {showDeleteDialog && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-950/50 backdrop-blur-sm p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="delete-account-title"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) closeDeleteDialog();
          }}
        >
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
            <div className="flex items-start justify-between gap-4">
              <div>
                <h2 id="delete-account-title" className="text-xl font-semibold text-neutral-900">
                  Delete your account?
                </h2>
                <p className="mt-2 text-sm text-neutral-500">
                  All of your Job Seeker data will be permanently removed.
                </p>
              </div>
              <button
                onClick={closeDeleteDialog}
                disabled={isDeleting}
                aria-label="Close delete account dialog"
                className="rounded-lg p-1.5 text-neutral-400 hover:bg-neutral-100 hover:text-neutral-700 transition disabled:opacity-50"
              >
                <X size={18} />
              </button>
            </div>

            <div className="mt-5 space-y-4">
              <Field label="Current password">
                <input
                  type="password"
                  value={deletePassword}
                  onChange={(event) => setDeletePassword(event.target.value)}
                  autoComplete="current-password"
                  className="w-full h-12 rounded-xl border border-neutral-200 bg-neutral-50/70 px-4 text-sm outline-none focus:bg-white focus:border-red-400 focus:ring-4 focus:ring-red-100 transition"
                />
              </Field>

              <Field label='Type "DELETE" to confirm'>
                <input
                  value={deleteConfirmation}
                  onChange={(event) => setDeleteConfirmation(event.target.value)}
                  placeholder="DELETE"
                  className="w-full h-12 rounded-xl border border-neutral-200 bg-neutral-50/70 px-4 text-sm outline-none focus:bg-white focus:border-red-400 focus:ring-4 focus:ring-red-100 transition"
                />
              </Field>
            </div>

            {deleteError && (
              <div className="mt-4 flex items-start gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">
                <AlertCircle size={17} className="mt-0.5 shrink-0" />
                <span>{deleteError}</span>
              </div>
            )}

            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={closeDeleteDialog}
                disabled={isDeleting}
                className="h-11 px-5 rounded-xl border border-neutral-200 text-sm font-semibold text-neutral-700 hover:bg-neutral-50 transition disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteAccount}
                disabled={isDeleting || !deletePassword || deleteConfirmation !== "DELETE"}
                className="h-11 px-5 rounded-xl bg-red-600 text-white text-sm font-semibold flex items-center gap-2 hover:bg-red-700 transition disabled:opacity-50"
              >
                {isDeleting ? <Loader2 size={15} className="animate-spin" /> : <Trash2 size={15} />}
                {isDeleting ? "Deleting..." : "Permanently delete"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Field({ label, children }) {
  return (
    <div>
      <label className="block text-sm font-medium text-neutral-700 mb-2">{label}</label>
      {children}
    </div>
  );
}

const formInputClass = "w-full h-11 rounded-xl border border-neutral-200 bg-white px-4 text-sm outline-none focus:border-violet-400 focus:ring-4 focus:ring-violet-100 transition";

function SectionHeader({ icon: Icon, title, buttonText, onAdd }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <h2 className="text-lg font-semibold tracking-tight flex items-center gap-2">
        <Icon size={18} className="text-violet-600" /> {title}
      </h2>
      <button type="button" onClick={onAdd}
        className="h-9 px-3 rounded-lg bg-neutral-900 text-white text-xs font-semibold flex items-center gap-1.5">
        <Plus size={13} /> {buttonText}
      </button>
    </div>
  );
}

function EducationForm({ form, setForm, editing, saving, onSubmit, onCancel }) {
  return (
    <form onSubmit={onSubmit} className="mt-5 rounded-xl border border-violet-100 bg-violet-50/40 p-4">
      <h3 className="text-sm font-semibold">{editing ? "Edit education" : "Add education"}</h3>
      <div className="mt-4 grid sm:grid-cols-2 gap-4">
        <Field label="Institution *">
          <input required maxLength={150} value={form.institution}
            onChange={(event) => setForm({ ...form, institution: event.target.value })}
            className={formInputClass} />
        </Field>
        <Field label="Degree *">
          <input required maxLength={150} value={form.degree}
            onChange={(event) => setForm({ ...form, degree: event.target.value })}
            className={formInputClass} />
        </Field>
        <Field label="Field of study">
          <input maxLength={150} value={form.fieldOfStudy}
            onChange={(event) => setForm({ ...form, fieldOfStudy: event.target.value })}
            className={formInputClass} />
        </Field>
        <Field label="Start date *">
          <input required type="date" value={form.startDate}
            onChange={(event) => setForm({ ...form, startDate: event.target.value })}
            className={formInputClass} />
        </Field>
        {!form.isCurrent && (
          <Field label="End date *">
            <input required type="date" min={form.startDate || undefined} value={form.endDate}
              onChange={(event) => setForm({ ...form, endDate: event.target.value })}
              className={formInputClass} />
          </Field>
        )}
      </div>
      <label className="mt-4 flex items-center gap-2 text-sm text-neutral-600">
        <input type="checkbox" checked={form.isCurrent}
          onChange={(event) => setForm({ ...form, isCurrent: event.target.checked, endDate: "" })} />
        I am currently studying here
      </label>
      <Field label="Description">
        <textarea maxLength={1000} rows={3} value={form.description}
          onChange={(event) => setForm({ ...form, description: event.target.value })}
          className={`${formInputClass} h-auto py-3 resize-none`} />
      </Field>
      <FormActions saving={saving} onCancel={onCancel} />
    </form>
  );
}

function WorkExperienceForm({ form, setForm, editing, saving, onSubmit, onCancel }) {
  return (
    <form onSubmit={onSubmit} className="mt-5 rounded-xl border border-violet-100 bg-violet-50/40 p-4">
      <h3 className="text-sm font-semibold">{editing ? "Edit work experience" : "Add work experience"}</h3>
      <div className="mt-4 grid sm:grid-cols-2 gap-4">
        <Field label="Job title *">
          <input required maxLength={150} value={form.jobTitle}
            onChange={(event) => setForm({ ...form, jobTitle: event.target.value })}
            className={formInputClass} />
        </Field>
        <Field label="Company *">
          <input required maxLength={150} value={form.companyName}
            onChange={(event) => setForm({ ...form, companyName: event.target.value })}
            className={formInputClass} />
        </Field>
        <Field label="Location">
          <input maxLength={150} value={form.location}
            onChange={(event) => setForm({ ...form, location: event.target.value })}
            className={formInputClass} />
        </Field>
        <Field label="Start date *">
          <input required type="date" value={form.startDate}
            onChange={(event) => setForm({ ...form, startDate: event.target.value })}
            className={formInputClass} />
        </Field>
        {!form.isCurrent && (
          <Field label="End date *">
            <input required type="date" min={form.startDate || undefined} value={form.endDate}
              onChange={(event) => setForm({ ...form, endDate: event.target.value })}
              className={formInputClass} />
          </Field>
        )}
      </div>
      <label className="mt-4 flex items-center gap-2 text-sm text-neutral-600">
        <input type="checkbox" checked={form.isCurrent}
          onChange={(event) => setForm({ ...form, isCurrent: event.target.checked, endDate: "" })} />
        I currently work here
      </label>
      <Field label="Description">
        <textarea maxLength={1000} rows={3} value={form.description}
          onChange={(event) => setForm({ ...form, description: event.target.value })}
          className={`${formInputClass} h-auto py-3 resize-none`} />
      </Field>
      <FormActions saving={saving} onCancel={onCancel} />
    </form>
  );
}

function FormActions({ saving, onCancel }) {
  return (
    <div className="mt-4 flex gap-2">
      <button disabled={saving}
        className="h-10 px-4 rounded-lg bg-violet-600 text-white text-sm font-semibold flex items-center gap-2 disabled:opacity-50">
        {saving && <Loader2 size={14} className="animate-spin" />} Save
      </button>
      <button type="button" onClick={onCancel} disabled={saving}
        className="h-10 px-4 rounded-lg border border-neutral-200 text-sm font-semibold text-neutral-600 disabled:opacity-50">
        Cancel
      </button>
    </div>
  );
}

function RecordActions({ onEdit, onDelete }) {
  return (
    <div className="flex gap-1 shrink-0">
      <button type="button" onClick={onEdit} aria-label="Edit record"
        className="p-2 rounded-lg text-neutral-500 hover:bg-white hover:text-violet-600 transition">
        <Pencil size={14} />
      </button>
      <button type="button" onClick={onDelete} aria-label="Delete record"
        className="p-2 rounded-lg text-neutral-500 hover:bg-red-50 hover:text-red-600 transition">
        <Trash2 size={14} />
      </button>
    </div>
  );
}

function LoadingRow({ text }) {
  return (
    <div className="mt-5 flex items-center gap-2 text-sm text-neutral-400">
      <Loader2 size={16} className="animate-spin" /> {text}
    </div>
  );
}

function InlineError({ message }) {
  return (
    <div className="mt-4 flex items-start gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600">
      <AlertCircle size={17} className="mt-0.5 shrink-0" /> <span>{message}</span>
    </div>
  );
}

function sortCareerRecords(records) {
  return [...records].sort((left, right) => {
    if (left.isCurrent !== right.isCurrent) return left.isCurrent ? -1 : 1;
    return right.startDate.localeCompare(left.startDate);
  });
}

function formatCareerPeriod(record) {
  const start = formatMonth(record.startDate);
  const end = record.isCurrent ? "Present" : formatMonth(record.endDate);
  return `${start} – ${end}`;
}

function formatMonth(value) {
  if (!value) return "Not specified";
  return new Date(`${value}T00:00:00`).toLocaleDateString(undefined, {
    month: "short",
    year: "numeric",
  });
}


function getProficiencyLabel(level) {
  switch (level) {
    case 1:
      return "Beginner";
    case 2:
      return "Basic";
    case 3:
      return "Intermediate";
    case 4:
      return "Advanced";
    case 5:
      return "Expert";
    default:
      return "Intermediate";
  }
}

export default ProfilePage;
