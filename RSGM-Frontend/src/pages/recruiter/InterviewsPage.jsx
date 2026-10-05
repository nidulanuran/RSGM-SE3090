import { useCallback, useEffect, useState } from "react";
import { AlertCircle, CalendarClock, Loader2, Sparkles } from "lucide-react";
import { getRecruiterRecommendations } from "../../services/panelistWorkflowService";
import {
  getRecruiterInterviews, getRecruiterOffers, saveOffer,
  submitOffer, withdrawOffer,
} from "../../services/hiringWorkflowService";

const card = "rounded-2xl border border-white/70 bg-white/75 p-5 backdrop-blur-2xl shadow-xl shadow-neutral-200/30";
const input = "w-full rounded-xl border border-neutral-200 bg-white px-3 py-2.5 text-sm outline-none focus:border-blue-400";
const button = "rounded-xl bg-neutral-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-neutral-800 disabled:opacity-50";
const small = "rounded-xl border border-neutral-200 bg-white px-3 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-50 disabled:opacity-50";
export default function InterviewsPage() {
  const [interviews, setInterviews] = useState([]);
  const [recommendations, setRecommendations] = useState([]);
  const [offers, setOffers] = useState([]);
  const [modal, setModal] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(async () => {
    const [sessions, drafts, decisions] = await Promise.all([getRecruiterInterviews(), getRecruiterOffers(), getRecruiterRecommendations()]);
    setInterviews(sessions); setOffers(drafts); setRecommendations(decisions);
  }, []);
  useEffect(() => {
    let cancelled = false;

    async function loadInitialInterviews() {
      try {
        const [sessions, drafts, decisions] = await Promise.all([
          getRecruiterInterviews(),
          getRecruiterOffers(),
          getRecruiterRecommendations(),
        ]);
        if (!cancelled) {
          setInterviews(sessions);
          setOffers(drafts);
          setRecommendations(decisions);
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

  async function run(action) {
    setBusy(true); setError("");
    try { await action(); await refresh(); setModal(null); }
    catch (e) { setError(e.message); }
    finally { setBusy(false); }
  }


  return <div>
    <div className="flex flex-wrap items-end justify-between gap-4">
      <div><span className="inline-flex items-center gap-2 rounded-full bg-blue-50 px-3 py-1.5 text-[11px] font-semibold text-blue-600"><Sparkles size={12} />RECRUITER WORKSPACE</span>
        <h1 className="mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">Interviews & offers</h1>
        <p className="mt-2 text-neutral-500">Track panelist interviews and draft offers after a positive recommendation.</p>
        <p className="mt-1 text-sm text-blue-700">Weekday office hours: 8:00 AM–5:00 PM (Asia/Colombo). Interviews last one hour; latest start is 4:00 PM.</p></div>

    </div>
    {error && <p role="alert" className="mt-5 flex gap-2 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700"><AlertCircle size={16} />{error}</p>}

    <div className="mt-8 grid gap-4 sm:grid-cols-3">
      {[
        ["Scheduled interviews", interviews.filter((i) => i.status === "Scheduled").length],
        ["Waiting for feedback", interviews.filter((i) => i.status === "Scheduled" && !i.feedback).length],
        ["Offers pending HR", offers.filter((o) => o.status === "Submitted").length],
      ].map(([label, count]) => <div key={label} className={card}><p className="text-xs text-neutral-500">{label}</p><p className="mt-2 text-2xl font-semibold text-blue-700">{loading ? "—" : count}</p></div>)}
    </div>
    <div className="mt-7 space-y-4">
      {loading && <p className="flex items-center gap-2 text-sm text-neutral-500"><Loader2 size={16} className="animate-spin" />Loading…</p>}
      {!loading && !interviews.length && <div className={card}>No interviews yet. Shortlist an applicant first.</div>}
      {interviews.map((i) => {
        const offer = offers.find((o) => o.applicationId === i.applicationId);
        const future = new Date(i.scheduledAt) > new Date();
        return <div className={card} key={i.id}>
          <div className="flex items-start gap-4">
            <div className="rounded-xl bg-blue-100 p-3 text-blue-600"><CalendarClock size={20} /></div>
            <div className="flex-1"><div className="flex flex-wrap gap-2"><h2 className="font-semibold">{i.candidate}</h2><span className="rounded-full bg-blue-50 px-2 text-xs text-blue-700">{i.status}</span></div>
              <p className="mt-1 text-sm text-neutral-500">{i.job} · {i.type} · {i.panelist}</p>
              <p className="mt-2 text-sm font-medium">{new Date(i.scheduledAt).toLocaleString()}</p>
              {i.locationOrLink && <p className="mt-1 break-all text-xs text-neutral-500">{i.locationOrLink}</p>}
            </div>
            {offer && <span className="rounded-lg bg-emerald-50 px-3 py-2 text-xs text-emerald-700">Offer: {offer.status}</span>}
          </div>
          {i.feedback && <div className="mt-4 rounded-xl bg-amber-50 p-4 text-sm text-neutral-700">
            <p className="font-semibold">{i.feedback.recommendation}</p>
            <p>Technical {i.feedback.technicalSkills}/5 · Problem solving {i.feedback.problemSolving}/5 · Communication {i.feedback.communication}/5 · Culture fit {i.feedback.cultureFit}/5</p>
            {i.feedback.comments && <p className="mt-2">{i.feedback.comments}</p>}
            <p className="mt-2 font-semibold text-blue-700">Expected salary: {i.feedback.desiredSalaryCurrency} {Number(i.feedback.desiredSalary).toLocaleString()}</p>
          </div>}
          {recommendations.find((r) => r.interviewId === i.id) && <p className="mt-3 rounded-xl bg-blue-50 p-3 text-sm text-blue-700">Panelist: {recommendations.find((r) => r.interviewId === i.id).selected ? "Recommended" : "Not recommended"} · {recommendations.find((r) => r.interviewId === i.id).rationale}</p>}
          {offer?.rejectionReason && <p className="mt-3 text-sm text-red-600">HR feedback: {offer.rejectionReason}</p>}
          {i.status === "Scheduled" && <div className="mt-4 flex flex-wrap gap-2">

            {i.feedback && !future && recommendations.some((r) => r.interviewId === i.id && r.selected) && (!offer || offer.status === "Rejected") && <button className={button} onClick={() => setModal({ kind: "offer", interview: i, offer })}>{offer ? "Revise offer" : "Draft offer"}</button>}
            {offer?.status === "Draft" && <button className={button} disabled={busy} onClick={() => run(() => submitOffer(offer.id))}>Submit to HR</button>}
            {(offer?.status === "Draft" || offer?.status === "Submitted") && <button className={small} disabled={busy} onClick={() => { if (window.confirm("Withdraw this offer?")) run(() => withdrawOffer(offer.id)); }}>Withdraw offer</button>}
          </div>}
        </div>;
      })}
    </div>
    {modal && <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm" onMouseDown={(e) => { if (e.target === e.currentTarget && !busy) setModal(null); }}>
      <div role="dialog" aria-modal="true" aria-label={modal.kind} className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl">
        <button className="float-right text-neutral-500" onClick={() => setModal(null)} aria-label="Close">✕</button>
        <h2 className="mb-5 text-xl font-semibold">Offer draft</h2>
        {modal.kind === "offer" && <OfferForm interview={modal.interview} offer={modal.offer} busy={busy} onSubmit={(data) => run(() => saveOffer(data))} />}
      </div>
    </div>}
  </div>;
}

function Field({ label, children }) { return <label className="block text-sm font-medium text-neutral-700">{label}<div className="mt-1.5">{children}</div></label>; }
function Save({ busy, children }) { return <button type="submit" disabled={busy} className={`${button} w-full`}>{busy ? "Saving…" : children}</button>; }
function OfferForm({ interview, offer, busy, onSubmit }) {
  const [salary, setSalary] = useState(offer?.salary || "");
  const [currency, setCurrency] = useState(offer?.currency || "LKR");
  const [startDate, setStartDate] = useState(offer?.startDate || "");
  const [notes, setNotes] = useState(offer?.notes || "");
  return <form className="space-y-4" onSubmit={(e) => { e.preventDefault(); onSubmit({ applicationId: interview.applicationId, salary: Number(salary), currency: currency.trim().toUpperCase(), startDate, notes }); }}>
    <p className="rounded-xl bg-blue-50 p-3 text-sm text-blue-700">{interview.candidate} · {interview.job}</p>
    <Field label="Salary"><input className={input} type="number" min="0.01" step="0.01" required value={salary} onChange={(e) => setSalary(e.target.value)} /></Field>
    <Field label="Currency code"><input className={input} minLength={3} maxLength={3} required value={currency} onChange={(e) => setCurrency(e.target.value)} /></Field>
    <Field label="Start date"><input className={input} type="date" required value={startDate} onChange={(e) => setStartDate(e.target.value)} /></Field>
    <Field label="Notes"><textarea className={input} rows={3} maxLength={2000} value={notes} onChange={(e) => setNotes(e.target.value)} /></Field>
    <Save busy={busy}>Save draft</Save>
  </form>;
}
