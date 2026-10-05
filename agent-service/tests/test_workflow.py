from uuid import uuid4

from career_schemas import (
    CandidateProfileInput,
    CareerAdvice,
    JobInput,
    JobMatch,
    ProfileAnalysis,
    RequiredSkillInput,
    SkillInput,
)
from tools.career_validation import validate_career_result
from tools.skill_names import canonicalize_known_skills, resolve_skill_name, build_skill_alias_index


def test_sql_short_name_resolves_to_canonical_database_name():
    canonical = ["SQL (Structured Query Language)", "React"]
    index = build_skill_alias_index(canonical)

    assert resolve_skill_name("SQL", index) == "SQL (Structured Query Language)"
    assert canonicalize_known_skills(["SQL", "React"], canonical) == [
        "SQL (Structured Query Language)",
        "React",
    ]


def test_unknown_skill_is_rejected_but_sql_alias_is_accepted():
    sql_id = uuid4()
    job_id = uuid4()

    candidate = CandidateProfileInput(
        headline="Student",
        location="Colombo",
        bio="AI undergraduate",
        has_cv=True,
        skills=[
            SkillInput(id=sql_id, name="SQL (Structured Query Language)", proficiency_level=4),
        ],
        education=[],
        experience=[],
    )

    job = JobInput(
        id=job_id,
        title="Data Intern",
        company="Example",
        location="Remote",
        employment_type="Internship",
        work_mode="Remote",
        experience_level="Entry",
        min_experience_years=0,
        description=None,
        requirements=None,
        required_skills=[
            RequiredSkillInput(id=sql_id, name="SQL (Structured Query Language)", weight=100),
        ],
    )

    profile = ProfileAnalysis(
        primary_career_area="Data",
        experience_level="Entry",
        strong_skills=["SQL"],
        developing_skills=["Imaginary Skill"],
        strengths=["Learner"],
        profile_gaps=["Limited recorded experience"],
        suitable_role_types=["Data Intern"],
        summary="Entry-level candidate.",
    )

    match = JobMatch(
        job_id=job_id,
        title="Data Intern",
        company="Example",
        match_score=50,
        matched_skills=["SQL"],
        missing_skills=[],
        experience_score=0,
        explanation="Deterministic match.",
    )

    advice = CareerAdvice(
        selected_job_id=job_id,
        headline_suggestion=None,
        learning_priorities=[],
        application_tips=[],
        summary="Review before applying.",
    )

    result = validate_career_result(candidate, [job], profile, [match], advice)

    assert not result.valid
    assert any("Imaginary Skill" in error for error in result.errors)
    assert not any("SQL" in error and "not recorded" in error for error in result.errors)
