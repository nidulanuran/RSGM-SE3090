from pydantic import BaseModel, Field
from langchain_core.prompts import ChatPromptTemplate

from career_schemas import PlanStep
from llm import get_llm


class PlanOutput(BaseModel):
    steps: list[PlanStep] = Field(min_length=4, max_length=8)


class PlannerAgent:
    """Creates the visible multi-step plan. It cannot call business-action tools."""

    def run(self, objective: str) -> list[PlanStep]:
        prompt = ChatPromptTemplate.from_messages([
            ("system", """You are the RSGM JobSeeker Planner Agent.
Create a short, structured plan for the user's career objective.
You must delegate visible work to these distinct roles in this order:
ProfileAnalysisAgent, JobMatchingAgent, CareerCoachAgent, DeterministicValidator.
The final step must be HumanApproval before any application submission.
Do not invent user data or job data. Return structured output only."""),
            ("human", "Objective: {objective}"),
        ])
        chain = prompt | get_llm().with_structured_output(PlanOutput)
        result = chain.invoke({"objective": objective})
        return result.steps
