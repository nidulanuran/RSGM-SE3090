import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getPanelists } from "../../services/hiringWorkflowService";
import {
  getSentShortlists,
  sendShortlist,
} from "../../services/panelistWorkflowService";
import {
  AlertCircle,
  ArrowDown,
  ArrowLeft,
  ArrowUp,
  BriefcaseBusiness,
  Loader2,
  Medal,
  Sparkles,
  Star,
  Trash2,
  Users,
} from "lucide-react";
import {
  getRecruiterApplicants,
  rankRecruiterShortlist,
  reviewRecruiterApplicant,
} from "../../services/recruiterApplicantService";

const card =
  "rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30";

export default function ShortlistsPage() {
  const [applicants, setApplicants] = useState([]);
  const [panelists, setPanelists] = useState([]);
  const [sent, setSent] = useState([]);
  const [chosen, setChosen] = useState({});
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;

    Promise.all([
      getRecruiterApplicants(),
      getPanelists(),
      getSentShortlists(),
    ])
      .then(([items, staff, dispatches]) => {
        if (!alive) return;

        setApplicants(items);
        setPanelists(staff);
        setSent(dispatches);
      })
      .catch((requestError) => {
        if (alive) {
          setError(
            requestError.message ||
              "Unable to load shortlist information."
          );
        }
      })
      .finally(() => {
        if (alive) {
          setLoading(false);
        }
      });

    return () => {
      alive = false;
    };
  }, []);

  const groups = useMemo(() => {
    const jobs = new Map();

    applicants
      .filter(
        (applicant) =>
          applicant.status === "Shortlisted"
      )
      .forEach((applicant) => {
        if (!jobs.has(applicant.jobPostingId)) {
          jobs.set(applicant.jobPostingId, {
            id: applicant.jobPostingId,
            title: applicant.jobTitle,
            candidates: [],
          });
        }

        jobs
          .get(applicant.jobPostingId)
          .candidates.push(applicant);
      });

    return [...jobs.values()].map((job) => ({
      ...job,
      candidates: [...job.candidates].sort(
        (a, b) =>
          (a.shortlistRank ?? 9999) -
            (b.shortlistRank ?? 9999) ||
          b.matchScore - a.matchScore
      ),
    }));
  }, [applicants]);

  const isSent = (jobId) =>
    sent.some(
      (dispatch) => dispatch.jobPostingId === jobId
    );

  async function dispatch(job) {
    const panelistId =
      chosen[job.id] || panelists[0]?.id;

    if (!panelistId) {
      setError(
        "Select a hiring panelist before sending the shortlist."
      );
      return;
    }

    setBusy(true);
    setError("");
    setSuccess("");

    try {
      await sendShortlist(
        job.id,
        panelistId
      );

      setSent(await getSentShortlists());

      setSuccess(
        `Sent ${job.title} to the hiring panelist.`
      );
    } catch (requestError) {
      setError(
        requestError.message ||
          "Unable to send the shortlist."
      );
    } finally {
      setBusy(false);
    }
  }

  async function move(job, index, offset) {
    const other = index + offset;

    if (
      other < 0 ||
      other >= job.candidates.length ||
      isSent(job.id)
    ) {
      return;
    }

    const reordered = [...job.candidates];

    [reordered[index], reordered[other]] = [
      reordered[other],
      reordered[index],
    ];

    setBusy(true);
    setError("");
    setSuccess("");

    try {
      const updated =
        await rankRecruiterShortlist(
          job.id,
          reordered.map(
            (candidate) => candidate.id
          )
        );

      setApplicants((previous) =>
        previous.map(
          (applicant) =>
            updated.find(
              (item) => item.id === applicant.id
            ) || applicant
        )
      );
    } catch (requestError) {
      setError(
        requestError.message ||
          "Unable to save the new rank."
      );
    } finally {
      setBusy(false);
    }
  }

  async function remove(id, jobId) {
    if (isSent(jobId)) {
      return;
    }

    setBusy(true);
    setError("");
    setSuccess("");

    try {
      await reviewRecruiterApplicant(
        id,
        "UnderReview"
      );

      setApplicants(
        await getRecruiterApplicants()
      );
    } catch (requestError) {
      setError(
        requestError.message ||
          "Unable to remove this candidate."
      );
    } finally {
      setBusy(false);
    }
  }

  const total = groups.reduce(
    (sum, job) =>
      sum + job.candidates.length,
    0
  );

  return (
    <div>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="inline-flex items-center gap-2 rounded-full bg-blue-50 px-3 py-1.5 text-[11px] font-semibold text-blue-600">
            <Sparkles size={12} /> RECRUITER WORKSPACE
          </div>

          <h1 className="mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">
            Ranked shortlists
          </h1>

          <p className="mt-2 text-neutral-500">
            Review match evidence, arrange the candidates,
            and send the final shortlist to the hiring panelist.
          </p>
        </div>

        <Link
          to="/recruiter/applications"
          className="inline-flex h-11 items-center justify-center gap-2 rounded-xl bg-neutral-900 px-4 text-sm font-semibold text-white transition hover:bg-neutral-800"
        >
          <ArrowLeft size={16} /> Review applications
        </Link>
      </div>

      <div className="mt-8 grid gap-4 sm:grid-cols-3">
        <Metric
          icon={Users}
          label="Shortlisted candidates"
          value={loading ? "—" : total}
        />

        <Metric
          icon={BriefcaseBusiness}
          label="Positions with shortlists"
          value={loading ? "—" : groups.length}
        />

        <div className={`${card} flex items-center gap-3 p-5`}>
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-blue-100 text-blue-600">
            <Medal size={19} />
          </div>
          <p className="text-sm text-neutral-600">
            Rank <strong className="text-neutral-900">#1</strong>{" "}
            is the recruiter's first choice for each position.
          </p>
        </div>
      </div>

      {success && (
        <p
          role="status"
          className="mt-5 rounded-xl bg-emerald-50 p-3 text-sm text-emerald-700"
        >
          {success}
        </p>
      )}

      {error && (
        <div
          role="alert"
          className="mt-6 flex gap-2 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
        >
          <AlertCircle size={17} className="shrink-0" />
          {error}
        </div>
      )}

      {loading ? (
        <div className="mt-10 flex items-center justify-center gap-2 text-sm text-neutral-400">
          <Loader2 size={18} className="animate-spin" />
          Loading shortlists...
        </div>
      ) : groups.length === 0 ? (
        <div className={`${card} mt-8 py-16 text-center`}>
          <Users
            size={28}
            className="mx-auto text-blue-300"
          />
          <h2 className="mt-3 font-semibold">
            No shortlisted candidates yet
          </h2>
          <p className="mt-1 text-sm text-neutral-500">
            Review applications and shortlist the candidates you want to consider.
          </p>
          <Link
            to="/recruiter/applications"
            className="mt-5 inline-block rounded-xl bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white"
          >
            View applications
          </Link>
        </div>
      ) : (
        <div className="mt-8 space-y-6">
          {groups.map((job) => {
            const locked = isSent(job.id);
            const sentDispatch = sent.find(
              (dispatch) =>
                dispatch.jobPostingId === job.id
            );

            return (
              <section
                key={job.id}
                className={`${card} overflow-hidden`}
              >
                <div className="flex flex-wrap items-center justify-between gap-3 border-b border-neutral-100 px-5 py-5 sm:px-6">
                  <div className="flex items-center gap-3">
                    <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-blue-100 text-blue-600">
                      <BriefcaseBusiness size={19} />
                    </span>

                    <div>
                      <h2 className="font-semibold text-neutral-900">
                        {job.title}
                      </h2>
                      <p className="mt-0.5 text-xs text-neutral-400">
                        {job.candidates.length} candidate
                        {job.candidates.length === 1
                          ? ""
                          : "s"}{" "}
                        ranked
                      </p>
                    </div>
                  </div>

                  {locked ? (
                    <span className="rounded-full bg-emerald-50 px-3 py-1.5 text-xs font-semibold text-emerald-700">
                      Sent to {sentDispatch?.panelist ?? "panelist"}
                    </span>
                  ) : (
                    <div className="flex flex-wrap gap-2">
                      <select
                        aria-label={`Panelist for ${job.title}`}
                        className="rounded-xl border border-neutral-200 px-3 py-2 text-sm"
                        value={
                          chosen[job.id] ||
                          panelists[0]?.id ||
                          ""
                        }
                        onChange={(event) =>
                          setChosen((old) => ({
                            ...old,
                            [job.id]: event.target.value,
                          }))
                        }
                      >
                        {panelists.map((panelist) => (
                          <option
                            key={panelist.id}
                            value={panelist.id}
                          >
                            {panelist.name}
                          </option>
                        ))}
                      </select>

                      <button
                        type="button"
                        disabled={
                          busy ||
                          !panelists.length
                        }
                        onClick={() =>
                          dispatch(job)
                        }
                        className="rounded-xl bg-blue-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                      >
                        Send shortlist
                      </button>
                    </div>
                  )}
                </div>

                <div className="divide-y divide-neutral-100">
                  {job.candidates.map(
                    (candidate, index) => (
                      <div
                        key={candidate.id}
                        className="flex flex-col gap-4 px-5 py-5 transition hover:bg-blue-50/25 sm:flex-row sm:items-start sm:px-6"
                      >
                        <div
                          className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl text-sm font-bold ${
                            index === 0
                              ? "bg-blue-600 text-white"
                              : "bg-blue-100 text-blue-700"
                          }`}
                        >
                          #{index + 1}
                        </div>

                        <div className="min-w-0 flex-1">
                          <div className="flex flex-wrap items-center gap-2">
                            <h3 className="font-semibold text-neutral-900">
                              {candidate.fullName}
                            </h3>

                            {index === 0 && (
                              <span className="rounded-full bg-blue-50 px-2.5 py-1 text-[11px] font-semibold text-blue-600">
                                Top choice
                              </span>
                            )}
                          </div>

                          <p className="mt-1 text-xs text-neutral-400">
                            {candidate.email}
                          </p>

                          {candidate.matchExplanation && (
                            <p className="mt-2 max-w-3xl text-xs leading-5 text-neutral-500">
                              {candidate.matchExplanation}
                            </p>
                          )}

                          <div className="mt-3 flex flex-wrap gap-1.5">
                            {(candidate.matchedSkills ?? [])
                              .slice(0, 6)
                              .map((skill) => (
                                <span
                                  key={skill}
                                  className="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-medium text-emerald-700"
                                >
                                  {skill}
                                </span>
                              ))}

                            {(candidate.matchedSkills ?? [])
                              .length === 0 && (
                              <span className="text-xs text-neutral-400">
                                No required skills matched.
                              </span>
                            )}
                          </div>

                          {(candidate.missingSkills ?? [])
                            .length > 0 && (
                            <div className="mt-2 flex flex-wrap gap-1.5">
                              {(candidate.missingSkills ?? [])
                                .slice(0, 5)
                                .map((skill) => (
                                  <span
                                    key={skill}
                                    className="rounded-full bg-red-50 px-2.5 py-1 text-[11px] font-medium text-red-600"
                                  >
                                    Gap: {skill}
                                  </span>
                                ))}
                            </div>
                          )}
                        </div>

                        <div className="flex flex-wrap items-center gap-3 sm:justify-end">
                          <span className="inline-flex items-center gap-1 rounded-xl bg-amber-50 px-3 py-2 text-sm font-semibold text-amber-700">
                            <Star
                              size={14}
                              className="fill-amber-400 text-amber-400"
                            />
                            {candidate.matchScore}% match
                          </span>

                          <div className="flex gap-1">
                            <RankButton
                              icon={ArrowUp}
                              title="Move up"
                              disabled={
                                busy ||
                                locked ||
                                index === 0
                              }
                              onClick={() =>
                                move(
                                  job,
                                  index,
                                  -1
                                )
                              }
                            />

                            <RankButton
                              icon={ArrowDown}
                              title="Move down"
                              disabled={
                                busy ||
                                locked ||
                                index ===
                                  job.candidates.length -
                                    1
                              }
                              onClick={() =>
                                move(
                                  job,
                                  index,
                                  1
                                )
                              }
                            />

                            <RankButton
                              icon={Trash2}
                              title="Remove from shortlist"
                              danger
                              disabled={
                                busy || locked
                              }
                              onClick={() =>
                                remove(
                                  candidate.id,
                                  job.id
                                )
                              }
                            />
                          </div>
                        </div>
                      </div>
                    )
                  )}
                </div>
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
}

function Metric({ icon: Icon, label, value }) {
  return (
    <div className={`${card} p-5`}>
      <div className="flex items-center justify-between">
        <span className="text-xs text-neutral-500">
          {label}
        </span>
        <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-blue-100 text-blue-600">
          <Icon size={17} />
        </span>
      </div>
      <p className="mt-2 text-2xl font-semibold tracking-tight">
        {value}
      </p>
    </div>
  );
}

function RankButton({
  icon: Icon,
  title,
  onClick,
  disabled,
  danger,
}) {
  return (
    <button
      type="button"
      title={title}
      aria-label={title}
      onClick={onClick}
      disabled={disabled}
      className={`flex h-9 w-9 items-center justify-center rounded-xl border transition disabled:cursor-not-allowed disabled:opacity-40 ${
        danger
          ? "border-red-200 text-red-600 hover:bg-red-50"
          : "border-neutral-200 text-neutral-500 hover:border-blue-200 hover:bg-blue-50 hover:text-blue-600"
      }`}
    >
      <Icon size={15} />
    </button>
  );
}
