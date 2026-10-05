from enum import Enum
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid", alias_generator=to_camel, populate_by_name=True)


class CandidateSnapshot(StrictModel):
    has_cv: bool
    skill_ids: list[UUID]
    has_headline: bool
    has_bio: bool
    education_count: int = Field(ge=0)
    work_experience_count: int = Field(ge=0)


class JobSnapshot(StrictModel):
    id: UUID
    status: str
    required_skill_ids: list[UUID]


class ReadinessRequest(StrictModel):
    workflow_id: UUID
    application_id: UUID
    application_status: str
    job: JobSnapshot
    candidate: CandidateSnapshot


class Readiness(str, Enum):
    READY = "ReadyForMatching"
    ATTENTION = "NeedsAttention"
    SKIPPED = "Skipped"
    FAILED = "SafelyFailed"


class Warning(StrictModel):
    code: str
    severity: str
    message: str


class Step(StrictModel):
    agent: str
    action: str
    status: str


class ReadinessResponse(StrictModel):
    workflow_id: UUID
    application_id: UUID
    status: str
    readiness_status: Readiness
    workflow_eligible: bool
    warnings: list[Warning]
    next_step: str
    steps: list[Step]
    policy_version: str = "1"
