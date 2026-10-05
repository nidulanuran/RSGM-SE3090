from typing import Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class CareerModel(BaseModel):
    model_config = ConfigDict(extra="forbid", alias_generator=to_camel, populate_by_name=True)


class SkillInput(CareerModel):
    id: UUID
    name: str = Field(min_length=1, max_length=100)
    proficiency_level: int = Field(ge=1, le=5)


class EducationInput(CareerModel):
    degree: str = Field(min_length=1, max_length=200)
    field_of_study: str | None = Field(default=None, max_length=200)
    institution: str = Field(min_length=1, max_length=200)
    is_current: bool


class ExperienceInput(CareerModel):
    job_title: str = Field(min_length=1, max_length=200)
    company_name: str = Field(min_length=1, max_length=200)
    duration_months: int = Field(ge=0, le=720)
    is_current: bool


class CandidateProfileInput(CareerModel):
    headline: str | None = Field(default=None, max_length=150)
    location: str | None = Field(default=None, max_length=150)
    bio: str | None = Field(default=None, max_length=1000)
    has_cv: bool
    skills: list[SkillInput] = Field(max_length=100)
    education: list[EducationInput] = Field(max_length=30)
    experience: list[ExperienceInput] = Field(max_length=50)


class RequiredSkillInput(CareerModel):
    id: UUID
    name: str = Field(min_length=1, max_length=100)
    weight: float = Field(gt=0, le=100)


class JobInput(CareerModel):
    id: UUID
    title: str = Field(min_length=1, max_length=200)
    company: str = Field(min_length=1, max_length=200)
    location: str = Field(min_length=1, max_length=200)
    employment_type: str
    work_mode: str
    experience_level: str
    min_experience_years: int | None = Field(default=None, ge=0, le=60)
    description: str | None = Field(default=None, max_length=5000)
    requirements: str | None = Field(default=None, max_length=5000)
    required_skills: list[RequiredSkillInput] = Field(max_length=100)


class CareerWorkflowRequest(CareerModel):
    workflow_id: UUID
    user_id: UUID
    objective: str = Field(min_length=5, max_length=500)
    candidate: CandidateProfileInput
    jobs: list[JobInput] = Field(max_length=100)


class PlanStep(CareerModel):
    order: int = Field(ge=1, le=20)
    agent: str = Field(min_length=1, max_length=80)
    action: str = Field(min_length=1, max_length=200)


class ProfileAnalysis(CareerModel):
    primary_career_area: str = Field(min_length=1, max_length=120)
    experience_level: str = Field(min_length=1, max_length=80)
    strong_skills: list[str] = Field(max_length=30)
    developing_skills: list[str] = Field(max_length=30)
    strengths: list[str] = Field(max_length=10)
    profile_gaps: list[str] = Field(max_length=10)
    suitable_role_types: list[str] = Field(max_length=8)
    summary: str = Field(min_length=1, max_length=1200)


class JobMatch(CareerModel):
    job_id: UUID
    title: str
    company: str
    match_score: int = Field(ge=0, le=100)
    matched_skills: list[str]
    missing_skills: list[str]
    experience_score: int = Field(ge=0, le=100)
    explanation: str = Field(min_length=1, max_length=1000)


class CareerAdvice(CareerModel):
    selected_job_id: UUID
    headline_suggestion: str | None = Field(default=None, max_length=180)
    learning_priorities: list[str] = Field(max_length=8)
    application_tips: list[str] = Field(max_length=8)
    summary: str = Field(min_length=1, max_length=1200)


class ValidationResult(CareerModel):
    valid: bool
    errors: list[str] = Field(max_length=20)
    checks: list[str] = Field(max_length=30)


class CareerStep(CareerModel):
    agent: str
    action: str
    status: Literal["Completed", "Failed", "Skipped", "Pending"]


class CareerWorkflowResponse(CareerModel):
    workflow_id: UUID
    status: Literal["AwaitingApproval", "SafelyFailed"]
    current_step: str
    plan: list[PlanStep]
    profile_analysis: ProfileAnalysis | None = None
    job_matches: list[JobMatch]
    selected_job_id: UUID | None = None
    career_advice: CareerAdvice | None = None
    validation: ValidationResult
    steps: list[CareerStep]
    error_summary: str | None = Field(default=None, max_length=1000)
    policy_version: str = "career-v1"
