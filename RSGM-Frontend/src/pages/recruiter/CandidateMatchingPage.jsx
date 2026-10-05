import { useEffect, useMemo, useState } from "react";
import {
  AlertCircle,
  Loader2,
  ScanSearch,
  Sparkles,
  Star,
} from "lucide-react";

import { getRecruiterJobPostings } from "../../services/recruiterJobPostingService";
import { runRecruiterMatching } from "../../services/recruiterMatchingService";

export default function CandidateMatchingPage() {
  const [jobs, setJobs] = useState([]);
  const [selectedJob, setSelectedJob] = useState("");
  const [results, setResults] = useState([]);
  const [loadingJobs, setLoadingJobs] = useState(true);
  const [running, setRunning] = useState(false);
  const [generatedAt, setGeneratedAt] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;

    getRecruiterJobPostings()
      .then((items) => {
        if (!alive) return;

        const publishedJobs = items.filter(
          (job) => job.status === "Published"
        );

        setJobs(publishedJobs);

        if (publishedJobs.length > 0) {
          setSelectedJob(publishedJobs[0].id);
        }
      })
      .catch((requestError) => {
        if (alive) {
          setError(
            requestError.message ||
              "Unable to load job postings."
          );
        }
      })
      .finally(() => {
        if (alive) {
          setLoadingJobs(false);
        }
      });

    return () => {
      alive = false;
    };
  }, []);

  const selectedPosting = useMemo(
    () =>
      jobs.find(
        (job) => job.id === selectedJob
      ),
    [jobs, selectedJob]
  );

  async function runMatching() {
    if (!selectedJob) {
      return;
    }

    setRunning(true);
    setError("");
    setResults([]);
    setGeneratedAt(null);

    try {
      const response =
        await runRecruiterMatching(selectedJob);

      setResults(response.candidates ?? []);
      setGeneratedAt(response.generatedAt ?? null);
    } catch (requestError) {
      setError(
        requestError.message ||
          "Unable to run candidate matching."
      );
    } finally {
      setRunning(false);
    }
  }

  return (
    <div>
      <div className="inline-flex items-center gap-2 rounded-full bg-blue-50 px-3 py-1.5 text-[11px] font-semibold text-blue-600">
        <Sparkles size={12} />
        CANDIDATE MATCHING ENGINE
      </div>

      <h1 className="mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">
        Candidate Matching
      </h1>

      <p className="mt-2 text-neutral-500">
        Compare applicants against weighted job requirements
        and review the reasons behind each match score.
      </p>

      {error && (
        <div
          role="alert"
          className="mt-6 flex gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
        >
          <AlertCircle
            size={17}
            className="mt-0.5 shrink-0"
          />
          {error}
        </div>
      )}

      <div className="mt-8 flex flex-col gap-3 sm:flex-row">
        <select
          aria-label="Job posting"
          value={selectedJob}
          onChange={(event) => {
            setSelectedJob(event.target.value);
            setResults([]);
            setGeneratedAt(null);
            setError("");
          }}
          disabled={loadingJobs || running || jobs.length === 0}
          className="h-12 rounded-xl border border-neutral-200 bg-white px-4 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100"
        >
          {jobs.length === 0 ? (
            <option value="">
              No published jobs available
            </option>
          ) : (
            jobs.map((job) => (
              <option
                key={job.id}
                value={job.id}
              >
                {job.title}
              </option>
            ))
          )}
        </select>

        <button
          type="button"
          onClick={runMatching}
          disabled={loadingJobs || running || !selectedJob}
          className="h-12 rounded-xl bg-neutral-900 px-5 text-sm font-semibold text-white transition hover:bg-neutral-800 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {running ? (
            <Loader2
              size={16}
              className="mr-2 inline animate-spin"
            />
          ) : (
            <ScanSearch
              size={16}
              className="mr-2 inline"
            />
          )}
          {running
            ? "Running matching..."
            : "Run matching"}
        </button>
      </div>

      {selectedPosting && (
        <div className="mt-5 rounded-2xl border border-blue-100 bg-blue-50/50 p-4 text-sm text-neutral-600">
          <span className="font-semibold">
            Selected position:
          </span>{" "}
          {selectedPosting.title}
          {" · "}
          {selectedPosting.requiredSkills?.length ?? 0}
          {" required skills"}
        </div>
      )}

      {generatedAt && (
        <p className="mt-4 text-xs text-neutral-400">
          Matching completed at{" "}
          {new Date(generatedAt).toLocaleString()}
        </p>
      )}

      <div className="mt-8 space-y-4">
        {results.map((candidate, index) => (
          <CandidateCard
            key={candidate.id}
            candidate={candidate}
            rank={index + 1}
          />
        ))}

        {!running &&
          results.length === 0 &&
          selectedJob && (
            <div className="rounded-2xl border border-dashed border-neutral-300 py-16 text-center text-sm text-neutral-400">
              Run matching to generate ranked candidates.
            </div>
          )}
      </div>
    </div>
  );
}

function CandidateCard({ candidate, rank }) {
  return (
    <article className="rounded-2xl border border-white/70 bg-white/80 p-5 shadow-xl shadow-neutral-200/30">
      <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
        <div className="flex min-w-0 gap-4">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-blue-100 text-sm font-bold text-blue-700">
            #{rank}
          </div>

          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="font-semibold text-neutral-900">
                {candidate.fullName}
              </h2>

              <span className="rounded-full bg-blue-50 px-2.5 py-1 text-[11px] font-semibold text-blue-600">
                {candidate.status}
              </span>
            </div>

            <p className="mt-1 text-xs text-neutral-400">
              {candidate.email}
            </p>

            <p className="mt-3 max-w-3xl text-sm leading-6 text-neutral-600">
              {candidate.matchExplanation}
            </p>
          </div>
        </div>

        <div className="shrink-0 rounded-2xl bg-blue-50 px-4 py-3 text-right">
          <div className="flex items-center justify-end gap-1 text-blue-700">
            <Star
              size={15}
              className="fill-amber-400 text-amber-400"
            />
            <span className="text-xl font-semibold">
              {candidate.matchScore}%
            </span>
          </div>
          <p className="text-[11px] text-neutral-400">
            weighted skill match
          </p>
        </div>
      </div>

      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        <section>
          <h3 className="text-xs font-semibold uppercase tracking-wide text-neutral-400">
            Matched skills
          </h3>

          <div className="mt-2 flex flex-wrap gap-1.5">
            {(candidate.matchedSkills ?? []).map(
              (skill) => (
                <span
                  key={skill}
                  className="rounded-full bg-emerald-50 px-2.5 py-1 text-[11px] font-medium text-emerald-700"
                >
                  {skill}
                </span>
              )
            )}

            {(candidate.matchedSkills ?? []).length === 0 && (
              <span className="text-xs text-neutral-400">
                No required skills matched.
              </span>
            )}
          </div>
        </section>

        <section>
          <h3 className="text-xs font-semibold uppercase tracking-wide text-neutral-400">
            Skill gaps
          </h3>

          <div className="mt-2 flex flex-wrap gap-1.5">
            {(candidate.missingSkills ?? []).map(
              (skill) => (
                <span
                  key={skill}
                  className="rounded-full bg-red-50 px-2.5 py-1 text-[11px] font-medium text-red-600"
                >
                  {skill}
                </span>
              )
            )}

            {(candidate.missingSkills ?? []).length === 0 && (
              <span className="text-xs text-neutral-400">
                No skill gaps identified.
              </span>
            )}
          </div>
        </section>
      </div>

      {candidate.matchBreakdown?.length > 0 && (
        <details className="mt-5 rounded-xl border border-neutral-200 bg-neutral-50 p-4">
          <summary className="cursor-pointer text-sm font-semibold text-neutral-700">
            View score breakdown
          </summary>

          <div className="mt-4 overflow-x-auto">
            <table className="min-w-full text-left text-xs">
              <thead>
                <tr className="border-b border-neutral-200 text-neutral-400">
                  <th className="px-3 py-2">Skill</th>
                  <th className="px-3 py-2">Weight</th>
                  <th className="px-3 py-2">Proficiency</th>
                  <th className="px-3 py-2">Contribution</th>
                </tr>
              </thead>
              <tbody>
                {candidate.matchBreakdown.map((item) => (
                  <tr
                    key={item.skillId}
                    className="border-b border-neutral-100"
                  >
                    <td className="px-3 py-2 font-medium text-neutral-700">
                      {item.skillName}
                    </td>
                    <td className="px-3 py-2">
                      {item.requiredWeight}
                    </td>
                    <td className="px-3 py-2">
                      {item.proficiencyLabel ?? "Missing"}
                    </td>
                    <td className="px-3 py-2">
                      {item.contributionPercentage}%
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </details>
      )}
    </article>
  );
}
