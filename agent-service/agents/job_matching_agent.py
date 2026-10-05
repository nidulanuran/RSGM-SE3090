from pydantic import BaseModel, Field
from langchain_core.prompts import ChatPromptTemplate

from career_schemas import CandidateProfileInput, JobInput, JobMatch, ProfileAnalysis
from llm import get_llm
from tools.matching_tools import calculate_job_match


class MatchExplanation(BaseModel):
    job_id: str
    explanation: str = Field(min_length=1, max_length=1000)


class MatchExplanationOutput(BaseModel):
    explanations: list[MatchExplanation]


class JobMatchingAgent:
    """Uses deterministic scores, then asks the LLM only to explain the strongest matches."""

    def run(
        self,
        candidate: CandidateProfileInput,
        jobs: list[JobInput],
        profile: ProfileAnalysis
    ) -> list[JobMatch]:

        print("==============================")
        print("JOB MATCHING AGENT START")
        print("Received jobs:", len(jobs))
        print("==============================")

        ranked = sorted(
            (calculate_job_match(candidate, job) for job in jobs),
            key=lambda item: item.match_score,
            reverse=True,
        )[:5]

        if not ranked:
            return []

        print("Total JOBS:", len(jobs))

        prompt = ChatPromptTemplate.from_messages([
            (
                "system",
                """You are the RSGM Job Matching Agent.
The numeric scores and matched/missing skills were calculated by deterministic code and MUST NOT be changed.
Write a short evidence-based explanation for each supplied job using only the supplied profile analysis and match facts.
Do not claim unrecorded skills or experience. Return structured output only."""
            ),
            (
                "human",
                "Profile analysis:\n{profile}\n\nRanked matches:\n{matches}"
            ),
        ])

        chain = prompt | get_llm().with_structured_output(
            MatchExplanationOutput
        )

        explanations = chain.invoke({
            "profile": profile.model_dump_json(by_alias=True),
            "matches": "\n".join(
                item.model_dump_json(by_alias=True)
                for item in ranked
            ),
        })

        lookup = {
            item.job_id: item.explanation
            for item in explanations.explanations
        }

        for match in ranked:
            match.explanation = lookup.get(
                str(match.job_id),
                "This recommendation is based on the deterministic skill and experience match."
            )

        return ranked
