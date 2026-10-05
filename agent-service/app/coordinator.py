from app.agents.application_agent import ApplicationManagementAgent
from app.schemas import Readiness, ReadinessRequest, ReadinessResponse, Step, Warning


class WorkflowCoordinator:
    def __init__(self, agent: ApplicationManagementAgent | None = None):
        self.agent = agent or ApplicationManagementAgent()

    def run(self, snapshot: ReadinessRequest) -> ReadinessResponse:
        # Fixed, allow-listed first plan: validate -> delegate -> validate result.
        steps = [Step(agent="WorkflowCoordinator", action="ValidateSnapshot", status="Completed")]
        try:
            readiness, warnings = self.agent.assess(snapshot)
            steps.append(Step(agent="ApplicationManagementAgent", action="ValidateReadiness", status="Completed"))
            eligible = readiness == Readiness.READY
            steps.append(Step(agent="WorkflowCoordinator", action="ValidateResult", status="Completed"))
            return ReadinessResponse(
                workflow_id=snapshot.workflow_id,
                application_id=snapshot.application_id,
                status="Completed",
                readiness_status=readiness,
                workflow_eligible=eligible,
                warnings=warnings,
                next_step="SkillMatching" if eligible else "ManualReview",
                steps=steps,
            )
        except Exception:
            # Do not leak personal data, stack traces, or internal exceptions.
            steps.append(Step(agent="ApplicationManagementAgent", action="ValidateReadiness", status="Failed"))
            return ReadinessResponse(
                workflow_id=snapshot.workflow_id, application_id=snapshot.application_id,
                status="SafelyFailed", readiness_status=Readiness.FAILED,
                workflow_eligible=False, next_step="RetryOrManualReview", steps=steps,
                warnings=[Warning(code="ASSESSMENT_FAILED", severity="Info",
                                  message="The assessment could not finish. A recruiter can review the application.")],
            )
