from career_schemas import CandidateProfileInput, JobInput, JobMatch


def estimate_experience_months(candidate: CandidateProfileInput) -> int:
    return sum(item.duration_months for item in candidate.experience)


def calculate_job_match(candidate: CandidateProfileInput, job: JobInput) -> JobMatch:
    """Allow-listed deterministic scoring tool. The LLM never calculates the numeric score."""
    candidate_skill_ids = {str(skill.id): skill for skill in candidate.skills}
    matched = []
    missing = []
    matched_weight = 0.0
    total_weight = 0.0

    for required in job.required_skills:
        weight = float(required.weight)
        total_weight += weight
        candidate_skill = candidate_skill_ids.get(str(required.id))
        if candidate_skill is None:
            missing.append(required.name)
            continue
        matched.append(required.name)
        proficiency_factor = max(0.2, min(candidate_skill.proficiency_level / 5.0, 1.0))
        matched_weight += weight * proficiency_factor

    skill_score = 100 if total_weight == 0 else round((matched_weight / total_weight) * 100)

    months = estimate_experience_months(candidate)
    required_months = (job.min_experience_years or 0) * 12
    if required_months <= 0:
        experience_score = 100
    else:
        experience_score = round(min(months / required_months, 1.0) * 100)

    # Skills are the strongest signal; experience is a secondary deterministic signal.
    match_score = round((skill_score * 0.8) + (experience_score * 0.2))

    return JobMatch(
        job_id=job.id,
        title=job.title,
        company=job.company,
        match_score=max(0, min(match_score, 100)),
        matched_skills=sorted(matched),
        missing_skills=sorted(missing),
        experience_score=max(0, min(experience_score, 100)),
        explanation="Pending agent explanation.",
    )
