import logging
from typing import Callable, TypeVar, TypedDict

from langgraph.graph import END, START, StateGraph
from pydantic import ValidationError

from agents.career_coach_agent import CareerCoachAgent
from agents.job_matching_agent import JobMatchingAgent
from agents.planner_agent import PlannerAgent
from agents.profile_analysis_agent import ProfileAnalysisAgent
from career_schemas import (
    CareerAdvice,
    CareerStep,
    CareerWorkflowRequest,
    CareerWorkflowResponse,
    JobMatch,
    PlanStep,
    ProfileAnalysis,
    ValidationResult,
)
from tools.career_validation import validate_career_result

logger = logging.getLogger(__name__)
T = TypeVar("T")


class CareerState(TypedDict, total=False):
    request: CareerWorkflowRequest
    plan: list[PlanStep]
    profile_analysis: ProfileAnalysis
    job_matches: list[JobMatch]
    career_advice: CareerAdvice
    validation: ValidationResult
    steps: list[CareerStep]


class AgentExecutionError(RuntimeError):
    """Safe wrapper for provider/model failures. Raw exception details stay in logs."""

    def __init__(
        self,
        *,
        agent: str,
        action: str,
        code: str,
        user_message: str,
        retryable: bool,
        cause: Exception,
    ) -> None:
        super().__init__(user_message)
        self.agent = agent
        self.action = action
        self.code = code
        self.user_message = user_message
        self.retryable = retryable
        self.__cause__ = cause


def _classify_agent_error(agent: str, action: str, exc: Exception) -> AgentExecutionError:
    """Convert provider/library exceptions into stable, user-safe error categories."""
    if isinstance(exc, ValidationError):
        return AgentExecutionError(
            agent=agent,
            action=action,
            code="AGENT_OUTPUT_INVALID",
            user_message=(
                f"{agent} returned output that did not match the required structured format."
            ),
            retryable=True,
            cause=exc,
        )

    class_name = type(exc).__name__.casefold()
    message = str(exc).casefold()

    if "rate" in class_name and "limit" in class_name or "rate limit" in message:
        return AgentExecutionError(
            agent=agent,
            action=action,
            code="AI_PROVIDER_RATE_LIMIT",
            user_message="The AI provider is temporarily busy. Please retry shortly.",
            retryable=True,
            cause=exc,
        )

    if isinstance(exc, TimeoutError) or "timeout" in class_name or "timed out" in message:
        return AgentExecutionError(
            agent=agent,
            action=action,
            code="AI_PROVIDER_TIMEOUT",
            user_message="The AI provider took too long to respond. Please retry.",
            retryable=True,
            cause=exc,
        )

    if "authentication" in class_name or "api key" in message or "unauthorized" in message:
        return AgentExecutionError(
            agent=agent,
            action=action,
            code="AI_PROVIDER_CONFIGURATION",
            user_message="The AI service is not configured correctly. Contact an administrator.",
            retryable=False,
            cause=exc,
        )

    if (
        isinstance(exc, ConnectionError)
        or "connection" in class_name
        or "connect" in message
        or "unavailable" in message
    ):
        return AgentExecutionError(
            agent=agent,
            action=action,
            code="AI_PROVIDER_UNAVAILABLE",
            user_message="The AI provider is temporarily unavailable. Please retry later.",
            retryable=True,
            cause=exc,
        )

    if isinstance(exc, RuntimeError) and "groq_api_key" in message:
        return AgentExecutionError(
            agent=agent,
            action=action,
            code="AI_PROVIDER_CONFIGURATION",
            user_message="The AI service is not configured correctly. Contact an administrator.",
            retryable=False,
            cause=exc,
        )

    return AgentExecutionError(
        agent=agent,
        action=action,
        code="AGENT_EXECUTION_FAILED",
        user_message=f"{agent} could not complete its task safely.",
        retryable=True,
        cause=exc,
    )


def _run_agent(agent: str, action: str, operation: Callable[[], T]) -> T:
    try:
        return operation()
    except AgentExecutionError:
        raise
    except Exception as exc:
        raise _classify_agent_error(agent, action, exc) from exc


def _append_step(
    state: CareerState,
    agent: str,
    action: str,
    status: str = "Completed",
) -> list[CareerStep]:
    return [
        *state.get("steps", []),
        CareerStep(agent=agent, action=action, status=status),
    ]


def planner_node(state: CareerState):
    plan = _run_agent(
        "PlannerAgent",
        "CreateStructuredPlan",
        lambda: PlannerAgent().run(state["request"].objective),
    )
    return {
        "plan": plan,
        "steps": _append_step(state, "PlannerAgent", "CreateStructuredPlan"),
    }


def profile_node(state: CareerState):
    profile = _run_agent(
        "ProfileAnalysisAgent",
        "AnalyseCandidateProfile",
        lambda: ProfileAnalysisAgent().run(state["request"].candidate),
    )
    return {
        "profile_analysis": profile,
        "steps": _append_step(state, "ProfileAnalysisAgent", "AnalyseCandidateProfile"),
    }


def matching_node(state: CareerState):
    matches = _run_agent(
        "JobMatchingAgent",
        "RankPublishedJobs",
        lambda: JobMatchingAgent().run(
            state["request"].candidate,
            state["request"].jobs,
            state["profile_analysis"],
        ),
    )
    return {
        "job_matches": matches,
        "steps": _append_step(state, "JobMatchingAgent", "RankPublishedJobs"),
    }


def coach_node(state: CareerState):
    matches = state.get("job_matches", [])
    if not matches:
        return {
            "steps": _append_step(
                state, "CareerCoachAgent", "PrepareCareerAdvice", "Skipped"
            )
        }

    advice = _run_agent(
        "CareerCoachAgent",
        "PrepareCareerAdvice",
        lambda: CareerCoachAgent().run(state["profile_analysis"], matches[0]),
    )
    return {
        "career_advice": advice,
        "steps": _append_step(state, "CareerCoachAgent", "PrepareCareerAdvice"),
    }


def validator_node(state: CareerState):
    result = validate_career_result(
        state["request"].candidate,
        state["request"].jobs,
        state["profile_analysis"],
        state.get("job_matches", []),
        state.get("career_advice"),
    )
    return {
        "validation": result,
        "steps": _append_step(
            state,
            "DeterministicValidator",
            "ValidateAgentOutputs",
            "Completed" if result.valid else "Failed",
        ),
    }


def build_graph():
    graph = StateGraph(CareerState)
    graph.add_node("planner", planner_node)
    graph.add_node("profile_analysis", profile_node)
    graph.add_node("job_matching", matching_node)
    graph.add_node("career_coach", coach_node)
    graph.add_node("validator", validator_node)
    graph.add_edge(START, "planner")
    graph.add_edge("planner", "profile_analysis")
    graph.add_edge("profile_analysis", "job_matching")
    graph.add_edge("job_matching", "career_coach")
    graph.add_edge("career_coach", "validator")
    graph.add_edge("validator", END)
    return graph.compile()


CAREER_GRAPH = build_graph()


def _validation_failure_summary(validation: ValidationResult) -> str:
    if not validation.errors:
        return "Deterministic validation rejected the agent output."

    first_error = validation.errors[0]
    return f"Deterministic validation rejected the agent output. {first_error}"[:1000]


def run_career_workflow(request: CareerWorkflowRequest) -> CareerWorkflowResponse:
    try:
        state = CAREER_GRAPH.invoke({"request": request, "steps": []})

        validation = state["validation"]
        matches = state.get("job_matches", [])

        if not validation.valid:
            return CareerWorkflowResponse(
                workflow_id=request.workflow_id,
                status="SafelyFailed",
                current_step="SafeFailure",
                plan=state.get("plan", []),
                profile_analysis=state.get("profile_analysis"),
                job_matches=matches,
                career_advice=state.get("career_advice"),
                selected_job_id=matches[0].job_id if matches else None,
                validation=validation,
                steps=state.get("steps", []),
                error_summary=_validation_failure_summary(validation),
            )

        if not matches:
            return CareerWorkflowResponse(
                workflow_id=request.workflow_id,
                status="SafelyFailed",
                current_step="SafeFailure",
                plan=state.get("plan", []),
                profile_analysis=state.get("profile_analysis"),
                job_matches=[],
                career_advice=None,
                selected_job_id=None,
                validation=validation,
                steps=state.get("steps", []),
                error_summary="No eligible published jobs were available for recommendation.",
            )

        return CareerWorkflowResponse(
            workflow_id=request.workflow_id,
            status="AwaitingApproval",
            current_step="HumanApproval",
            plan=state["plan"],
            profile_analysis=state["profile_analysis"],
            job_matches=matches,
            selected_job_id=matches[0].job_id,
            career_advice=state.get("career_advice"),
            validation=validation,
            steps=[
                *state.get("steps", []),
                CareerStep(
                    agent="HumanApproval",
                    action="AwaitJobSeekerDecision",
                    status="Pending",
                ),
            ],
        )

    except AgentExecutionError as exc:
        logger.exception(
            "Career workflow %s failed in %s (%s): %s",
            request.workflow_id,
            exc.agent,
            exc.code,
            exc.__cause__,
        )
        retry_hint = " You can retry this workflow." if exc.retryable else ""
        return CareerWorkflowResponse(
            workflow_id=request.workflow_id,
            status="SafelyFailed",
            current_step="SafeFailure",
            plan=[],
            profile_analysis=None,
            job_matches=[],
            selected_job_id=None,
            career_advice=None,
            validation=ValidationResult(
                valid=False,
                errors=[f"{exc.code}: {exc.user_message}"],
                checks=[],
            ),
            steps=[
                CareerStep(
                    agent=exc.agent,
                    action=exc.action,
                    status="Failed",
                )
            ],
            error_summary=(exc.user_message + retry_hint)[:1000],
        )

    except Exception as exc:
        # Last-resort boundary: never expose raw provider/stack-trace details to clients.
        logger.exception(
            "Unexpected career workflow failure for workflow %s",
            request.workflow_id,
        )
        return CareerWorkflowResponse(
            workflow_id=request.workflow_id,
            status="SafelyFailed",
            current_step="SafeFailure",
            plan=[],
            profile_analysis=None,
            job_matches=[],
            selected_job_id=None,
            career_advice=None,
            validation=ValidationResult(
                valid=False,
                errors=[
                    "WORKFLOW_INTERNAL_ERROR: The workflow encountered an unexpected internal error."
                ],
                checks=[],
            ),
            steps=[
                CareerStep(
                    agent="WorkflowCoordinator",
                    action="RunCareerWorkflow",
                    status="Failed",
                )
            ],
            error_summary=(
                "The AI workflow could not finish safely. Please retry. "
                "If the problem continues, contact an administrator."
            ),
        )
