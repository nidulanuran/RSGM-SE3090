from career_schemas import (
    CandidateProfileInput,
    CareerAdvice,
    JobInput,
    JobMatch,
    ProfileAnalysis,
    ValidationResult,
)
from tools.skill_names import (
    build_skill_alias_index,
    normalize_skill_name,
    resolve_skill_name,
)


def _resolve_many(names: list[str], canonical_names: list[str]) -> tuple[list[str], list[str]]:
    """Return (resolved canonical names, unresolved model values)."""
    index = build_skill_alias_index(canonical_names)
    resolved: list[str] = []
    unresolved: list[str] = []

    for name in names:
        canonical = resolve_skill_name(name, index)
        if canonical is None:
            unresolved.append(name.strip())
        else:
            resolved.append(canonical)

    return resolved, unresolved


def validate_career_result(
    candidate: CandidateProfileInput,
    jobs: list[JobInput],
    profile: ProfileAnalysis,
    matches: list[JobMatch],
    advice: CareerAdvice | None,
) -> ValidationResult:
    """Deterministic validator that rejects unsupported or inconsistent agent output."""
    errors: list[str] = []
    checks: list[str] = []

    # ------------------------------------------------------------------
    # 1. Candidate profile skill safety
    # ------------------------------------------------------------------
    candidate_skill_names = [skill.name for skill in candidate.skills]
    profile_skill_claims = profile.strong_skills + profile.developing_skills
    resolved_profile_skills, unsupported_profile_skills = _resolve_many(
        profile_skill_claims, candidate_skill_names
    )

    if unsupported_profile_skills:
        errors.append(
            "Profile analysis referenced skills that are not recorded in the candidate profile: "
            + ", ".join(sorted(set(unsupported_profile_skills), key=str.casefold))
            + ". Use the exact recorded skill names."
        )
    else:
        checks.append(
            "Profile skill claims resolve to canonical skills from the candidate snapshot."
        )

    strong_resolved, _ = _resolve_many(profile.strong_skills, candidate_skill_names)
    developing_resolved, _ = _resolve_many(profile.developing_skills, candidate_skill_names)
    overlap = {
        normalize_skill_name(name) for name in strong_resolved
    } & {
        normalize_skill_name(name) for name in developing_resolved
    }
    if overlap:
        display_overlap = sorted(
            {
                name
                for name in resolved_profile_skills
                if normalize_skill_name(name) in overlap
            },
            key=str.casefold,
        )
        errors.append(
            "Profile analysis classified the same skill as both strong and developing: "
            + ", ".join(display_overlap)
            + "."
        )
    else:
        checks.append("Strong and developing skill classifications do not overlap.")

    # ------------------------------------------------------------------
    # 2. Job-reference and required-skill safety
    # ------------------------------------------------------------------
    jobs_by_id = {job.id: job for job in jobs}
    for match in matches:
        job = jobs_by_id.get(match.job_id)
        if job is None:
            errors.append(f"Match references unknown job {match.job_id}.")
            continue

        if match.title != job.title or match.company != job.company:
            errors.append(
                f"Match metadata for job {match.job_id} does not match the supplied published job."
            )

        required_names = [skill.name for skill in job.required_skills]
        resolved_matched, invalid_matched = _resolve_many(match.matched_skills, required_names)
        resolved_missing, invalid_missing = _resolve_many(match.missing_skills, required_names)
        invalid_required_claims = sorted(
            set(invalid_matched + invalid_missing), key=str.casefold
        )

        if invalid_required_claims:
            errors.append(
                f"Match for job {match.job_id} references skills that are not requirements for that job: "
                + ", ".join(invalid_required_claims)
                + "."
            )

        matched_keys = {normalize_skill_name(name) for name in resolved_matched}
        missing_keys = {normalize_skill_name(name) for name in resolved_missing}
        duplicated = matched_keys & missing_keys
        if duplicated:
            duplicated_names = sorted(
                {
                    name
                    for name in resolved_matched + resolved_missing
                    if normalize_skill_name(name) in duplicated
                },
                key=str.casefold,
            )
            errors.append(
                f"Match for job {match.job_id} marks the same skill as both matched and missing: "
                + ", ".join(duplicated_names)
                + "."
            )

    if not any(
        error.startswith("Match references unknown job")
        or error.startswith("Match metadata")
        or error.startswith("Match for job")
        for error in errors
    ):
        checks.append("Every recommendation references a supplied published job.")
        checks.append(
            "Matched and missing skill lists resolve only to canonical job requirements."
        )

    # ------------------------------------------------------------------
    # 3. Career-advice selection safety
    # ------------------------------------------------------------------
    if matches and advice is None:
        errors.append("Career advice is missing for the highest-ranked recommendation.")
    elif matches and advice is not None and advice.selected_job_id != matches[0].job_id:
        errors.append("Career advice does not reference the highest-ranked job.")
    elif matches and advice is not None:
        checks.append("Career advice references the highest-ranked deterministic match.")

    return ValidationResult(valid=not errors, errors=errors, checks=checks)
