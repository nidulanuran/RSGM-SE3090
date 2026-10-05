import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { AlertCircle, CalendarClock, CheckCircle2, Download, Loader2, Sparkles, Star } from "lucide-react";
import {
  downloadPanelistCandidateCv, getPanelistCandidate, getPanelistInterviews,
  saveInterviewFeedback,
} from "../../services/hiringWorkflowService";

import { cancelPanelistInterview, changeInterviewTime, getAvailableSlots, getPanelistRecommendations, recommendCandidate } from "../../services/panelistWorkflowService";

const card = "rounded-2xl border border-white/70 bg-white/75 p-5 backdrop-blur-2xl shadow-xl shadow-neutral-200/30";
const criteria = [["technicalSkills", "Technical skills"], ["problemSolving", "Problem solving"],
  ["communication", "Communication"], ["cultureFit", "Culture fit"]];
const recommendations = ["Strong Hire", "Hire", "Leaning No", "No Hire"];

export default function MyInterviewsPage() {
  const [interviews, setInterviews] = useState([]);
  const [decisions, setDecisions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selected, setSelected] = useState(null);
  const [viewing, setViewing] = useState(null);
  const [recommendFor, setRecommendFor] = useState(null);
  const [choice, setChoice] = useState("yes");
  const [rationale, setRationale] = useState("");
  const [busy, setBusy] = useState(false);
  const [rescheduleFor, setRescheduleFor] = useState(null);
  const [newSlot, setNewSlot] = useState("");
  const refresh = useCallback(async () => { const [sessions, recommendations] = await Promise.all([getPanelistInterviews(), getPanelistRecommendations()]); setInterviews(sessions); setDecisions(recommendations); }, []);
  useEffect(() => {
    let cancelled = false;

    async function loadInitialInterviews() {
      try {
        const [sessions, recommendationItems] = await Promise.all([
          getPanelistInterviews(),
          getPanelistRecommendations(),
        ]);
        if (!cancelled) {
          setInterviews(sessions);
          setDecisions(recommendationItems);
        }
      } catch (e) {
        if (!cancelled) setError(e.message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    loadInitialInterviews();
    return () => { cancelled = true; };
  }, []);

  async function submit(id, feedback) {
    setBusy(true); setError("");
    try { await saveInterviewFeedback(id, feedback); await refresh(); setSelected(null); }
    catch (e) { setError(e.message); }
    finally { setBusy(false); }
  }

  async function act(action) {
    setBusy(true); setError("");
    try { await action(); await refresh(); }
    catch (e) { setError(e.message); }
    finally { setBusy(false); }
  }
  async function change(i) {
    try {
      const slots = await getAvailableSlots(i.jobPostingId, i.hrManagerId);
      const choices = slots.filter((slot) => slot !== i.scheduledAt);

      if (!choices.length) {
        setError(
          "No other free office-hour slots are available. Check each participant's busy schedule.",
        );
        return;
      }

      setRescheduleFor({
        interview: i,
        choices,
      });

      setNewSlot(choices[0]);
    } catch (e) {
      setError(e.message);
    }
  }

  async function submitReschedule(e) {
    e.preventDefault();

    if (!rescheduleFor || !newSlot) return;

    setBusy(true);
    setError("");

    try {
      await changeInterviewTime(
        rescheduleFor.interview.id,
        newSlot,
      );

      await refresh();

      setRescheduleFor(null);
      setNewSlot("");
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  async function submitRecommendation(e) {
    e.preventDefault();
    setBusy(true); setError("");
    try { await recommendCandidate(recommendFor.id, choice === "yes", rationale.trim()); await refresh(); setRecommendFor(null); setRationale(""); }
    catch (err) { setError(err.message); }
    finally { setBusy(false); }
  }

  return <div>
    <span className="inline-flex items-center gap-2 rounded-full bg-amber-50 px-3 py-1.5 text-[11px] font-semibold text-amber-600"><Sparkles size={12} />ASSIGNED INTERVIEWS</span>
    <h1 className="mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">My interviews</h1>
    <p className="mt-2 text-neutral-500">Propose interview times, review candidate profiles and CVs, then forward recommendations to HR.</p>
    <Link to="/panelist/shortlists" className="mt-3 inline-block text-sm font-semibold text-amber-700">View assigned shortlists →</Link>
    {error && <p role="alert" className="mt-6 flex gap-2 rounded-xl bg-red-50 p-3 text-sm text-red-700"><AlertCircle size={16} />{error}</p>}
    <div className="mt-8 space-y-3">
      {loading && <p className="flex items-center gap-2 text-sm text-neutral-500"><Loader2 size={16} className="animate-spin" />Loading interviews…</p>}
      {!loading && !interviews.length && <div className={card}>No interviews assigned yet.</div>}
      {interviews.map((i) => <div key={i.id} className={card}>
        <div className="flex flex-wrap items-center gap-4">
          <div className="rounded-xl bg-amber-100 p-3 text-amber-600"><CalendarClock size={20} /></div>
          <div className="min-w-0 flex-1"><h2 className="font-semibold text-neutral-900">{i.candidate}</h2><p className="mt-1 text-sm text-neutral-500">{i.job} · {i.type}</p>
            <p className="mt-1 text-xs text-neutral-500">{new Date(i.scheduledAt).toLocaleString()}{i.locationOrLink ? ` · ${i.locationOrLink}` : ""}</p></div>
          <span className="rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold text-amber-700">{i.status}</span>
        </div>
        {i.feedback && <p className="mt-3 flex items-center gap-1.5 text-sm text-emerald-700"><CheckCircle2 size={15} />Feedback submitted: {i.feedback.recommendation}</p>}
        {decisions.find((r) => r.interviewId === i.id) && <p className="mt-3 rounded-xl bg-emerald-50 p-3 text-sm text-emerald-700">Decision sent to HR: {decisions.find((r) => r.interviewId === i.id).selected ? "Recommended" : "Not recommended"}</p>}
        {i.status !== "Cancelled" && <div className="mt-4 flex flex-wrap gap-2">
          <button type="button" onClick={() => setViewing(i)} className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-2.5 text-sm font-semibold text-amber-700 hover:bg-amber-100">View candidate & CV</button>
          {i.status === "Scheduled" && new Date(i.scheduledAt) <= new Date() && !decisions.some((r) => r.interviewId === i.id) && <button type="button" onClick={() => setSelected(i)} className="rounded-xl bg-neutral-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-neutral-800">{i.feedback ? "View / edit feedback" : "Give feedback"}</button>}
          {new Date(i.scheduledAt) > new Date() && i.hrManagerId && <button type="button" disabled={busy} onClick={() => change(i)} className="rounded-xl border border-amber-200 px-4 py-2.5 text-sm font-semibold text-amber-700">Propose new time</button>}
          {new Date(i.scheduledAt) > new Date() && !i.feedback && <button type="button" disabled={busy} onClick={() => { if (window.confirm("Cancel this interview?")) act(() => cancelPanelistInterview(i.id)); }} className="rounded-xl border border-red-200 px-4 py-2.5 text-sm font-semibold text-red-700">Cancel</button>}
          {i.status === "Scheduled" && new Date(i.scheduledAt) <= new Date() && i.feedback && !decisions.some((r) => r.interviewId === i.id) && <button type="button" disabled={busy} onClick={() => setRecommendFor(i)} className="rounded-xl bg-amber-600 px-4 py-2.5 text-sm font-semibold text-white">Send recommendation to HR</button>}
        </div>}
      </div>)}
    </div>
    {recommendFor && <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4"><form onSubmit={submitRecommendation} className="w-full max-w-lg space-y-4 rounded-3xl bg-white p-6 shadow-2xl"><div className="flex items-start justify-between"><div><h2 className="text-xl font-semibold">Recommendation to HR</h2><p className="mt-1 text-sm text-neutral-500">{recommendFor.candidate} · {recommendFor.job}</p></div><button type="button" aria-label="Close" onClick={() => setRecommendFor(null)}>✕</button></div><label className="block text-sm font-medium">Decision<select className="mt-2 w-full rounded-xl border border-neutral-200 p-3" value={choice} onChange={(e) => setChoice(e.target.value)}><option value="yes">Recommend</option><option value="no">Do not recommend</option></select></label><label className="block text-sm font-medium">Rationale<textarea required maxLength={2000} rows={5} value={rationale} onChange={(e) => setRationale(e.target.value)} className="mt-2 w-full rounded-xl border border-neutral-200 p-3" /></label><button disabled={busy || !rationale.trim()} className="w-full rounded-xl bg-amber-600 py-3 text-sm font-semibold text-white disabled:opacity-50">Submit to HR</button></form></div>}
    {rescheduleFor && (
      <div
        className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm"
        onMouseDown={(e) => {
          if (e.target === e.currentTarget && !busy) {
            setRescheduleFor(null);
            setNewSlot("");
          }
        }}
      >
        <form
          onSubmit={submitReschedule}
          role="dialog"
          aria-modal="true"
          aria-label="Reschedule interview"
          className="w-full max-w-lg rounded-3xl bg-white p-6 shadow-2xl sm:p-8"
        >
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase text-amber-600">
                Reschedule interview
              </p>

              <h2 className="mt-1 text-xl font-semibold">
                Select a new interview time
              </h2>

              <p className="mt-2 text-sm text-neutral-500">
                {rescheduleFor.interview.candidate} ·{" "}
                {rescheduleFor.interview.job}
              </p>
            </div>

            <button
              type="button"
              aria-label="Close"
              disabled={busy}
              onClick={() => {
                setRescheduleFor(null);
                setNewSlot("");
              }}
              className="text-neutral-500"
            >
              ✕
            </button>
          </div>

          <div className="mt-6 rounded-2xl bg-amber-50 p-4">
            <p className="text-xs font-semibold uppercase text-amber-700">
              Current interview time
            </p>

            <p className="mt-1 text-sm font-medium text-neutral-800">
              {new Date(
                rescheduleFor.interview.scheduledAt,
              ).toLocaleString()}
            </p>
          </div>

          <label className="mt-5 block text-sm font-medium">
            Available shared slot

            <select
              value={newSlot}
              onChange={(e) => setNewSlot(e.target.value)}
              className="mt-2 w-full rounded-xl border border-neutral-200 bg-white p-3 text-sm outline-none focus:border-amber-400"
            >
              {rescheduleFor.choices.map((slot) => (
                <option key={slot} value={slot}>
                  {new Date(slot).toLocaleString()}
                </option>
              ))}
            </select>
          </label>

          <p className="mt-3 text-xs text-neutral-500">
            These available times consider the shared schedules of the recruiter,
            HR manager, and hiring panelist.
          </p>

          <div className="mt-6 flex justify-end gap-3">
            <button
              type="button"
              disabled={busy}
              onClick={() => {
                setRescheduleFor(null);
                setNewSlot("");
              }}
              className="rounded-xl border border-neutral-200 px-4 py-2.5 text-sm font-semibold text-neutral-700 hover:bg-neutral-50"
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={busy || !newSlot}
              className="rounded-xl bg-amber-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-amber-700 disabled:opacity-50"
            >
              {busy ? "Rescheduling…" : "Confirm new time"}
            </button>
          </div>
        </form>
      </div>
    )}

    {selected && <FeedbackDialog key={selected.id} interview={selected} busy={busy} onClose={() => setSelected(null)} onSubmit={(feedback) => submit(selected.id, feedback)} />}
    {viewing && <CandidateDialog key={viewing.id} interview={viewing} onClose={() => setViewing(null)} onFeedback={() => { setSelected(viewing); setViewing(null); }} />}
  </div>;
}

function FeedbackDialog({ interview, busy, onClose, onSubmit }) {
  const prior = interview.feedback;
  const [ratings, setRatings] = useState(Object.fromEntries(criteria.map(([key]) => [key, prior?.[key] || 0])));
  const [recommendation, setRecommendation] = useState(prior?.recommendation || "");
  const [comments, setComments] = useState(prior?.comments || "");
  const [desiredSalary, setDesiredSalary] = useState(prior?.desiredSalary || "");
  const [desiredSalaryCurrency, setDesiredSalaryCurrency] = useState(prior?.desiredSalaryCurrency || "LKR");
  const [error, setError] = useState("");

  function save(e) {
    e.preventDefault();
    if (criteria.some(([key]) => ratings[key] < 1) || !recommendation || Number(desiredSalary) <= 0 || desiredSalaryCurrency.trim().length !== 3) {
      setError("Rate every criterion, choose a recommendation, and record the candidate's expected salary."); return;
    }
    onSubmit({ ...ratings, recommendation, comments, desiredSalary: Number(desiredSalary), desiredSalaryCurrency: desiredSalaryCurrency.trim().toUpperCase() });
  }
  return <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm" onMouseDown={(e) => { if (e.target === e.currentTarget && !busy) onClose(); }}>
    <div role="dialog" aria-modal="true" aria-label="Interview feedback" className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl sm:p-8">
      <button type="button" className="float-right text-neutral-500" onClick={onClose} aria-label="Close">✕</button>
      <h2 className="text-xl font-semibold">Interview feedback</h2>
      <p className="mt-1 text-sm text-neutral-500">{interview.candidate} · {interview.job}</p>
      {interview.candidateEmail && <p className="mt-1 text-xs text-neutral-500">{interview.candidateEmail}</p>}
      <form onSubmit={save} className="mt-6 space-y-5">
        {criteria.map(([key, label]) => <div key={key}><p className="mb-2 text-sm font-medium">{label}</p><div className="flex gap-2">
          {[1, 2, 3, 4, 5].map((score) => <button type="button" key={score} aria-label={`${label}: ${score} out of 5`} onClick={() => setRatings((old) => ({ ...old, [key]: score }))}><Star size={25} className={score <= ratings[key] ? "fill-amber-400 text-amber-400" : "text-neutral-200"} /></button>)}
        </div></div>)}
        <div><p className="mb-2 text-sm font-medium">Recommendation</p><div className="grid grid-cols-2 gap-2">{recommendations.map((value) => <button type="button" key={value} onClick={() => setRecommendation(value)} className={`rounded-xl border px-2 py-2 text-sm ${recommendation === value ? "border-neutral-900 bg-neutral-900 text-white" : "border-neutral-200 text-neutral-600"}`}>{value}</button>)}</div></div>
        <div className="grid grid-cols-[1fr_110px] gap-3"><label className="block text-sm font-medium">Candidate's expected salary<input required min="0.01" step="0.01" type="number" value={desiredSalary} onChange={(e) => setDesiredSalary(e.target.value)} className="mt-2 w-full rounded-xl border border-neutral-200 p-3 text-sm" /></label><label className="block text-sm font-medium">Currency<input required minLength={3} maxLength={3} value={desiredSalaryCurrency} onChange={(e) => setDesiredSalaryCurrency(e.target.value)} className="mt-2 w-full rounded-xl border border-neutral-200 p-3 text-sm uppercase" /></label></div>
        <p className="rounded-xl bg-blue-50 p-3 text-xs text-blue-700">Submitting feedback sends the expected salary to the recruiter as a notification.</p>
        <label className="block text-sm font-medium">Comments<textarea className="mt-2 w-full rounded-xl border border-neutral-200 p-3 text-sm outline-none focus:border-amber-400" rows={3} maxLength={2000} value={comments} onChange={(e) => setComments(e.target.value)} /></label>
        {error && <p role="alert" className="text-sm text-red-600">{error}</p>}
        <button type="submit" disabled={busy} className="w-full rounded-xl bg-neutral-900 py-3 text-sm font-semibold text-white disabled:opacity-50">{busy ? "Saving…" : "Submit feedback"}</button>
      </form>
    </div>
  </div>;
}

function CandidateDialog({ interview, onClose, onFeedback }) {
  const [candidate, setCandidate] = useState(null);
  const [error, setError] = useState("");
  const [downloading, setDownloading] = useState(false);

  useEffect(() => {
    let alive = true;
    getPanelistCandidate(interview.id)
      .then((details) => { if (alive) setCandidate(details); })
      .catch((e) => { if (alive) setError(e.message); });
    return () => { alive = false; };
  }, [interview.id]);

  async function download() {
    setDownloading(true); setError("");
    try { await downloadPanelistCandidateCv(interview.id, candidate.cvFileName); }
    catch (e) { setError(e.message); }
    finally { setDownloading(false); }
  }

  const links = candidate ? [
    ["LinkedIn", candidate.linkedInUrl], ["GitHub", candidate.gitHubUrl],
    ["Portfolio", candidate.portfolioUrl],
  ].filter(([, url]) => /^https?:\/\//i.test(url || "")) : [];

  return <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm" onMouseDown={(e) => { if (e.target === e.currentTarget) onClose(); }}>
    <div role="dialog" aria-modal="true" aria-label="Candidate details" className="max-h-[90vh] w-full max-w-xl overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl sm:p-8">
      <button type="button" className="float-right text-neutral-500" onClick={onClose} aria-label="Close">✕</button>
      <h2 className="text-xl font-semibold">Candidate details</h2>
      <p className="mt-1 text-sm text-neutral-500">{interview.job} · {new Date(interview.scheduledAt).toLocaleString()}</p>
      {error && <p role="alert" className="mt-4 rounded-xl bg-red-50 p-3 text-sm text-red-700">{error}</p>}
      {!candidate && !error && <p className="mt-6 flex items-center gap-2 text-sm text-neutral-500"><Loader2 size={16} className="animate-spin" />Loading candidate…</p>}
      {candidate && <div className="mt-6 space-y-6 text-sm text-neutral-700">
        <div className="rounded-2xl bg-amber-50 p-4">
          <h3 className="text-lg font-semibold text-neutral-900">{candidate.fullName}</h3>
          <p className="mt-1 text-neutral-600">{candidate.headline || "Job applicant"}</p>
          <p className="mt-2">{candidate.email}{candidate.phoneNumber ? ` · ${candidate.phoneNumber}` : ""}</p>
          {candidate.location && <p className="mt-1">{candidate.location}</p>}
        </div>
        {candidate.bio && <section><h3 className="font-semibold text-neutral-900">About</h3><p className="mt-1 whitespace-pre-wrap">{candidate.bio}</p></section>}
        <section><h3 className="font-semibold text-neutral-900">Skills</h3><div className="mt-2 flex flex-wrap gap-2">
          {candidate.skills.length ? candidate.skills.map((skill) => <span key={skill.name} className="rounded-full bg-amber-50 px-3 py-1 text-xs text-amber-800">{skill.name} · {skill.proficiencyLevel}/5</span>) : <p className="text-neutral-400">No skills provided.</p>}
        </div></section>
        <section><h3 className="font-semibold text-neutral-900">Work experience</h3><div className="mt-2 space-y-2">
          {candidate.workExperience.length ? candidate.workExperience.map((item, index) => <div key={index} className="rounded-xl bg-neutral-50 p-3"><p className="font-medium">{item.jobTitle} · {item.companyName}</p><p className="text-xs text-neutral-500">{item.startDate} – {item.isCurrent ? "Present" : item.endDate || ""}{item.location ? ` · ${item.location}` : ""}</p></div>) : <p className="text-neutral-400">No experience provided.</p>}
        </div></section>
        <section><h3 className="font-semibold text-neutral-900">Education</h3><div className="mt-2 space-y-2">
          {candidate.education.length ? candidate.education.map((item, index) => <div key={index} className="rounded-xl bg-neutral-50 p-3"><p className="font-medium">{item.degree} · {item.institution}</p><p className="text-xs text-neutral-500">{item.fieldOfStudy ? `${item.fieldOfStudy} · ` : ""}{item.startDate} – {item.isCurrent ? "Present" : item.endDate || ""}</p></div>) : <p className="text-neutral-400">No education provided.</p>}
        </div></section>
        {links.length > 0 && <div className="flex flex-wrap gap-2">{links.map(([label, url]) => <a key={label} href={url} target="_blank" rel="noopener noreferrer" className="rounded-xl border border-neutral-200 px-3 py-2 text-xs font-semibold text-amber-700 hover:bg-amber-50">{label} ↗</a>)}</div>}
        {candidate.hasCv ? <button type="button" onClick={download} disabled={downloading} className="flex items-center gap-2 rounded-xl bg-amber-600 px-4 py-2.5 font-semibold text-white hover:bg-amber-700 disabled:opacity-50"><Download size={16} />{downloading ? "Downloading…" : `Download ${candidate.cvFileName || "CV"}`}</button> : <p className="text-neutral-400">No CV uploaded.</p>}
        {interview.status === "Scheduled" && new Date(interview.scheduledAt) <= new Date() && <button type="button" onClick={onFeedback} className="rounded-xl bg-neutral-900 px-4 py-2.5 font-semibold text-white hover:bg-neutral-800">Give interview feedback</button>}
      </div>}
    </div>
  </div>;
}
