from langchain_core.prompts import ChatPromptTemplate

from career_schemas import CandidateProfileInput, ProfileAnalysis
from llm import get_llm
from tools.skill_names import canonicalize_known_skills


class ProfileAnalysisAgent:
    """Interprets only the supplied candidate snapshot; it has no write permissions."""

    def run(self, candidate: CandidateProfileInput) -> ProfileAnalysis:
        canonical_skill_names = [skill.name for skill in candidate.skills]

        prompt = ChatPromptTemplate.from_messages([
            ("system", """You are the RSGM Profile Analysis Agent.
Analyse only the candidate facts supplied in the input.
Never invent skills, qualifications, work history, employers, certificates, or experience.

STRICT SKILL RULES:
1. strongSkills and developingSkills may contain ONLY skills from RECORDED SKILLS.
2. Copy each recorded skill name EXACTLY as provided.
3. Do not abbreviate, shorten, expand, rename, translate, or normalize a skill.
4. Example: if the recorded skill is \"SQL (Structured Query Language)\", return exactly
   \"SQL (Structured Query Language)\", never \"SQL\".
5. A skill must not appear in both strongSkills and developingSkills.
6. Treat proficiency 4-5 as strong and 1-3 as developing.

You may reason about broader strengths and gaps in prose, but structured skill arrays are
strictly constrained to RECORDED SKILLS.
Profile gaps must be phrased as missing/limited recorded evidence, not as absolute facts
about the person.
Return concise structured output."""),
            ("human", """RECORDED SKILLS (canonical names; copy exactly):
{recorded_skills}

Candidate JSON:
{candidate_json}"""),
        ])

        chain = prompt | get_llm().with_structured_output(ProfileAnalysis)
        result = chain.invoke({
            "recorded_skills": "\n".join(f"- {name}" for name in canonical_skill_names) or "- None",
            "candidate_json": candidate.model_dump_json(by_alias=True),
        })

        # Protection layer 1b: canonicalize harmless aliases emitted by the model.
        # Unknown values are deliberately preserved so validation can reject them.
        result.strong_skills = canonicalize_known_skills(
            result.strong_skills, canonical_skill_names
        )
        result.developing_skills = canonicalize_known_skills(
            result.developing_skills, canonical_skill_names
        )

        return result
