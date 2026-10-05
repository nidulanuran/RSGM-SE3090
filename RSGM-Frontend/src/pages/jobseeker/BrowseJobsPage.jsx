import { useEffect, useMemo, useState } from "react";
import {
  AlertCircle,
  Briefcase,
  BriefcaseBusiness,
  CalendarDays,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Clock3,
  Loader2,
  MapPin,
  Search,
  Sparkles,
  WalletCards,
} from "lucide-react";

import { getJobPostings } from "../../services/jobPostingService";
import {
  applyToJob,
  getMyApplications,
} from "../../services/jobSeekerApplicationService";

const TYPE_LABELS = {
  FullTime: "Full-Time",
  PartTime: "Part-Time",
  Contract: "Contract",
  Internship: "Internship",
};

const MODE_LABELS = {
  OnSite: "On-site",
  Remote: "Remote",
  Hybrid: "Hybrid",
};

function BrowseJobsPage() {
  const [jobs, setJobs] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [query, setQuery] = useState("");

  const [expandedJobId, setExpandedJobId] =
    useState(null);

  const [applicationByJobId, setApplicationByJobId] =
    useState(new Map());

  const [applyingJobId, setApplyingJobId] =
    useState(null);

  useEffect(() => {
    let ignore = false;

    Promise.all([
      getJobPostings(),
      getMyApplications(),
    ])
      .then(([postings, applications]) => {
        if (ignore) {
          return;
        }

        setJobs(postings);

        setApplicationByJobId(
          new Map(
            applications.map((application) => [
              application.jobPostingId,
              application,
            ]),
          ),
        );
      })
      .catch((requestError) => {
        if (!ignore) {
          setError(
            requestError.message ||
              "Unable to load jobs."
          );
        }
      })
      .finally(() => {
        if (!ignore) {
          setIsLoading(false);
        }
      });

    return () => {
      ignore = true;
    };
  }, []);

  const filteredJobs = useMemo(() => {
    const normalizedQuery =
      query.trim().toLowerCase();

    return jobs.filter((job) => {
      const title =
        job.title?.toLowerCase() ?? "";

      const company =
        job.company?.toLowerCase() ?? "";

      const location =
        job.location?.toLowerCase() ?? "";

      return (
        title.includes(normalizedQuery) ||
        company.includes(normalizedQuery) ||
        location.includes(normalizedQuery)
      );
    });
  }, [jobs, query]);

  const handleApply = async (jobId) => {
    setError("");
    setApplyingJobId(jobId);

    try {
      const createdApplication =
        await applyToJob(jobId);

      setApplicationByJobId((previous) => {
        const next = new Map(previous);

        next.set(
          createdApplication.jobPostingId ?? jobId,
          createdApplication,
        );

        return next;
      });
    } catch (requestError) {
      setError(
        requestError.message ||
          "Unable to submit application."
      );
    } finally {
      setApplyingJobId(null);
    }
  };

  return (
    <div>
      {/* PAGE LABEL */}

      <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-violet-50 text-violet-600 text-[11px] font-semibold">
        <Sparkles size={12} />
        BROWSE JOBS
      </div>

      {/* TITLE */}

      <h1 className="mt-4 text-3xl sm:text-4xl font-semibold tracking-tight">
        Jobs for you
      </h1>

      <p className="mt-2 text-neutral-500">
        Browse active opportunities and review
        their complete requirements.
      </p>

      {/* SEARCH */}

      <div className="relative mt-8 max-w-sm">
        <Search
          size={17}
          className="absolute left-4 top-1/2 -translate-y-1/2 text-neutral-400"
        />

        <input
          value={query}
          onChange={(event) =>
            setQuery(event.target.value)
          }
          placeholder="Search jobs, companies or locations..."
          className="w-full h-11 rounded-xl border border-neutral-200 bg-white pl-11 pr-4 text-sm outline-none focus:border-violet-400 focus:ring-4 focus:ring-violet-100 transition"
        />
      </div>

      {/* ERROR */}

      {error && (
        <div className="mt-5 flex items-start gap-3 p-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-600 max-w-lg">
          <AlertCircle
            size={17}
            className="mt-0.5 shrink-0"
          />

          <span>{error}</span>
        </div>
      )}

      {/* LOADING */}

      {isLoading ? (
        <div className="mt-8 flex items-center gap-2 text-sm text-neutral-400">
          <Loader2
            size={18}
            className="animate-spin"
          />

          Loading jobs...
        </div>
      ) : (
        <div className="mt-6 space-y-4">
          {filteredJobs.map((job) => {
            const existingApplication =
              applicationByJobId.get(job.id);

            const isApplying =
              applyingJobId === job.id;

            const expanded =
              expandedJobId === job.id;

            return (
              <article
                key={job.id}
                className="p-5 rounded-2xl border border-white/70 bg-white/75 backdrop-blur-2xl shadow-xl shadow-neutral-200/30"
              >
                <div className="flex flex-col lg:flex-row lg:items-start justify-between gap-5">
                  {/* JOB INFORMATION */}

                  <div className="flex items-start gap-4 min-w-0">
                    <CompanyLogo job={job} />

                    <div className="min-w-0">
                      <p className="font-semibold text-neutral-900">
                        {job.title}
                      </p>

                      <p className="mt-0.5 text-sm text-neutral-500">
                        {job.company}
                      </p>

                      {/* JOB META DATA */}

                      <div className="mt-2 flex flex-wrap gap-x-4 gap-y-2 text-xs text-neutral-500">
                        <span className="inline-flex items-center gap-1">
                          <MapPin size={12} />

                          {job.location}
                        </span>

                        <span className="inline-flex items-center gap-1">
                          <BriefcaseBusiness size={12} />

                          {TYPE_LABELS[
                            job.employmentType
                          ] ??
                            job.employmentType ??
                            "Not specified"}
                        </span>

                        <span className="inline-flex items-center gap-1">
                          <Clock3 size={12} />

                          {MODE_LABELS[
                            job.workMode
                          ] ??
                            job.workMode ??
                            "Not specified"}
                        </span>

                        <span className="inline-flex items-center gap-1">
                          <CalendarDays size={12} />

                          Apply by{" "}
                          {formatDate(
                            job.applicationDeadline
                          )}
                        </span>
                      </div>

                      {/* DESCRIPTION */}

                      {job.description && (
                        <p className="mt-3 text-sm text-neutral-500 line-clamp-2 max-w-2xl">
                          {job.description}
                        </p>
                      )}

                      {/* SKILLS */}

                      <div className="mt-3 flex flex-wrap gap-1.5">
                        {(
                          job.requiredSkills ?? []
                        ).map((skill) => (
                          <span
                            key={skill}
                            className="px-2 py-0.5 rounded-full bg-neutral-100 text-neutral-600 text-[11px] font-medium"
                          >
                            {skill}
                          </span>
                        ))}
                      </div>

                      {/* EXPERIENCE / SALARY */}

                      <div className="mt-3 flex flex-wrap gap-3 text-xs font-medium text-neutral-600">
                        <span>
                          {job.experienceLevel ??
                            "Not specified"}{" "}
                          level
                        </span>

                        <span>
                          {getExperienceText(job)}
                        </span>

                        <span className="inline-flex items-center gap-1">
                          <WalletCards size={12} />

                          {formatSalary(job) ||
                            "Salary not disclosed"}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* ACTIONS */}

                  <div className="flex flex-wrap items-center gap-2 shrink-0">
                    <button
                      type="button"
                      onClick={() =>
                        setExpandedJobId(
                          expanded
                            ? null
                            : job.id
                        )
                      }
                      className="h-10 px-4 rounded-xl border border-neutral-200 text-neutral-600 text-sm font-semibold flex items-center gap-1.5 hover:bg-neutral-50 transition"
                    >
                      {expanded ? (
                        <ChevronUp size={14} />
                      ) : (
                        <ChevronDown
                          size={14}
                        />
                      )}

                      Details
                    </button>

                    {existingApplication ? (
                      <ApplicationStatusBadge
                        status={existingApplication.status}
                      />
                    ) : (
                      <button
                        type="button"
                        onClick={() =>
                          handleApply(job.id)
                        }
                        disabled={isApplying}
                        className="h-10 px-4 rounded-xl bg-neutral-900 text-white text-sm font-semibold hover:bg-neutral-800 transition disabled:opacity-60 flex items-center gap-2"
                      >
                        {isApplying && (
                          <Loader2
                            size={14}
                            className="animate-spin"
                          />
                        )}

                        Apply
                      </button>
                    )}
                  </div>
                </div>

                {/* EXPANDED DETAILS */}

                {expanded && (
                  <div className="mt-5 pt-5 border-t border-neutral-100 grid md:grid-cols-2 gap-6">
                    <DetailSection
                      title="Responsibilities"
                      text={
                        job.responsibilities
                      }
                    />

                    <DetailSection
                      title="Requirements"
                      text={job.requirements}
                    />

                    <div className="md:col-span-2 text-xs text-neutral-400">
                      {job.postedDate
                        ? `Posted ${formatDateTime(
                            job.postedDate
                          )}`
                        : ""}
                    </div>
                  </div>
                )}
              </article>
            );
          })}

          {/* EMPTY SEARCH */}

          {filteredJobs.length === 0 && (
            <div className="py-16 text-center text-sm text-neutral-400 rounded-2xl border border-neutral-200 bg-white">
              No jobs match your search.
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// =========================================================
// APPLICATION STATUS BADGE
// =========================================================

function ApplicationStatusBadge({ status }) {
  const config = {
    UnderReview: {
      label: "Applied",
      className: "bg-emerald-50 text-emerald-600",
      icon: true,
    },
    Shortlisted: {
      label: "Shortlisted",
      className: "bg-violet-50 text-violet-600",
      icon: true,
    },
    Interview: {
      label: "Interview",
      className: "bg-blue-50 text-blue-600",
      icon: true,
    },
    Offer: {
      label: "Offer",
      className: "bg-amber-50 text-amber-700",
      icon: true,
    },
    Rejected: {
      label: "Rejected",
      className: "bg-red-50 text-red-600",
      icon: false,
    },
    Withdrawn: {
      label: "Withdrawn",
      className: "bg-neutral-100 text-neutral-500",
      icon: false,
    },
  };

  const current =
    config[status] ?? {
      label: status || "Applied",
      className: "bg-neutral-100 text-neutral-600",
      icon: false,
    };

  return (
    <span
      className={`inline-flex items-center gap-1.5 h-10 px-4 rounded-xl text-sm font-semibold ${current.className}`}
    >
      {current.icon && (
        <CheckCircle2 size={14} />
      )}

      {current.label}
    </span>
  );
}

// =========================================================
// COMPANY LOGO
// =========================================================

function CompanyLogo({ job }) {
  if (job.companyLogoUrl) {
    return (
      <img
        src={job.companyLogoUrl}
        alt={`${job.company} logo`}
        className="w-12 h-12 rounded-2xl border border-neutral-100 bg-white object-contain p-1 shrink-0"
      />
    );
  }

  return (
    <div className="w-12 h-12 rounded-2xl bg-violet-100 flex items-center justify-center shrink-0">
      <Briefcase
        size={20}
        className="text-violet-600"
      />
    </div>
  );
}

// =========================================================
// DETAIL SECTION
// =========================================================

function DetailSection({
  title,
  text,
}) {
  return (
    <section>
      <h3 className="text-sm font-semibold text-neutral-800">
        {title}
      </h3>

      <p className="mt-2 text-sm leading-6 text-neutral-500 whitespace-pre-line">
        {text || "Not provided."}
      </p>
    </section>
  );
}

// =========================================================
// EXPERIENCE TEXT
// =========================================================

function getExperienceText(job) {
  if (
    job.employmentType === "Internship"
  ) {
    return "No experience required";
  }

  if (
    job.minExperienceYears == null
  ) {
    return "Experience not specified";
  }

  return `${job.minExperienceYears} year${
    job.minExperienceYears === 1
      ? ""
      : "s"
  } minimum`;
}

// =========================================================
// SALARY
// =========================================================

function formatSalary(job) {
  if (
    job.minSalary == null &&
    job.maxSalary == null
  ) {
    return "";
  }

  const formatter =
    new Intl.NumberFormat(undefined, {
      maximumFractionDigits: 0,
    });

  const min =
    job.minSalary == null
      ? ""
      : formatter.format(
          job.minSalary
        );

  const max =
    job.maxSalary == null
      ? ""
      : formatter.format(
          job.maxSalary
        );

  const range =
    min && max
      ? `${min}–${max}`
      : min || max;

  return `${
    job.currency ?? ""
  } ${range}`.trim();
}

// =========================================================
// DATE
// =========================================================

function formatDate(value) {
  if (!value) {
    return "Not specified";
  }

  return new Date(
    `${value}T00:00:00`
  ).toLocaleDateString();
}

function formatDateTime(value) {
  if (!value) {
    return "";
  }

  return new Date(
    value
  ).toLocaleDateString();
}

export default BrowseJobsPage;