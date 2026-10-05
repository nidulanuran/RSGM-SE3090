import hmac
import os

from fastapi import Depends, FastAPI, Header, HTTPException

from app.coordinator import WorkflowCoordinator
from app.schemas import ReadinessRequest, ReadinessResponse
from career_schemas import CareerWorkflowRequest, CareerWorkflowResponse
from graph.career_workflow import run_career_workflow

app = FastAPI(title="RSGM Internal Application Agent")


def require_service_key(x_agent_service_key: str | None = Header(default=None)) -> None:
    configured = os.getenv("RSGM_AGENT_SERVICE_KEY", "")
    if not configured or not x_agent_service_key or not hmac.compare_digest(configured, x_agent_service_key):
        raise HTTPException(status_code=401, detail="Unauthorized service request")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/internal/workflows/application-readiness", response_model=ReadinessResponse,
          dependencies=[Depends(require_service_key)])
def application_readiness(snapshot: ReadinessRequest) -> ReadinessResponse:
    return WorkflowCoordinator().run(snapshot)


@app.post("/internal/workflows/jobseeker-career", response_model=CareerWorkflowResponse,
          dependencies=[Depends(require_service_key)])
def jobseeker_career_workflow(request: CareerWorkflowRequest) -> CareerWorkflowResponse:
    return run_career_workflow(request)
