from langchain_core.prompts import ChatPromptTemplate

from career_schemas import CareerAdvice, JobMatch, ProfileAnalysis
from llm import get_llm


class CareerCoachAgent:
    """Generates non-destructive coaching suggestions for the selected recommendation."""

    def run(self, profile: ProfileAnalysis, selected: JobMatch) -> CareerAdvice:
        prompt = ChatPromptTemplate.from_messages([
            ("system", """You are the RSGM Career Coach Agent.
Give concise, practical, non-destructive coaching for the selected job.
Use only the supplied profile analysis and match facts.
Do not claim the candidate has a missing skill. Do not promise employment outcomes.
Suggestions must require the JobSeeker to review them; never imply the profile was changed automatically.
Return structured output only."""),
            ("human", "Profile:\n{profile}\n\nSelected match:\n{match}"),
        ])
        chain = prompt | get_llm().with_structured_output(CareerAdvice)
        result = chain.invoke({
            "profile": profile.model_dump_json(by_alias=True),
            "match": selected.model_dump_json(by_alias=True),
        })
        # The selected job is a backend/algorithm fact, not an LLM choice.
        result.selected_job_id = selected.job_id
        return result
