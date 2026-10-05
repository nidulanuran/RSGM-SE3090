from app.schemas import Readiness, ReadinessRequest, Warning


def validate_readiness(snapshot: ReadinessRequest) -> tuple[Readiness, list[Warning]]:
    """Approved, deterministic tool. No CV contents or free text are sent here."""
    if snapshot.application_status in {"Withdrawn", "Rejected", "Hired", "OfferDeclined"}:
        return Readiness.SKIPPED, [Warning(
            code="APPLICATION_NOT_ACTIVE", severity="Info",
            message="This application is not available for automated matching.")]

    if snapshot.application_status != "UnderReview":
        return Readiness.SKIPPED, [Warning(
            code="APPLICATION_ALREADY_PROCESSED", severity="Info",
            message="This application is already in another recruitment stage.")]

    if snapshot.job.status != "Published":
        return Readiness.SKIPPED, [Warning(
            code="JOB_NOT_PUBLISHED", severity="Info",
            message="This job is not currently published.")]

    warnings: list[Warning] = []
    if not snapshot.candidate.has_cv:
        warnings.append(Warning(code="CV_MISSING", severity="Critical",
                                message="Upload a CV for automated matching."))
    if snapshot.job.required_skill_ids and not snapshot.candidate.skill_ids:
        warnings.append(Warning(code="SKILLS_MISSING", severity="Critical",
                                message="Add skills for this skill-based job."))
    if not snapshot.candidate.has_headline:
        warnings.append(Warning(code="HEADLINE_MISSING", severity="Warning",
                                message="Add a professional headline."))
    if not snapshot.candidate.has_bio:
        warnings.append(Warning(code="BIO_MISSING", severity="Warning",
                                message="Add a short profile summary."))
    if snapshot.candidate.education_count == 0:
        warnings.append(Warning(code="EDUCATION_MISSING", severity="Info",
                                message="Add education details if applicable."))
    # No work experience and no social links are never blockers: entry-level
    # candidates can legitimately lack them.
    if any(w.severity == "Critical" for w in warnings):
        return Readiness.ATTENTION, warnings
    return Readiness.READY, warnings
