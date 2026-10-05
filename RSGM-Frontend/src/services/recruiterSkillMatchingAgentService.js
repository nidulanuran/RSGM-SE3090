import { getAuthHeaders } from "./authService";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

const BASE_PATH =
  `${API_BASE_URL}/api/recruiter/skill-matching-agent`;

/**
 * Reads an API response safely.
 */
async function readResponse(response) {
  const text = await response.text();

  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return {
      message: text,
    };
  }
}

/**
 * Shared request helper for the Agent API.
 */
async function request(path, options = {}) {
  const response = await fetch(
    `${BASE_PATH}${path}`,
    {
      ...options,
      headers: {
        ...getAuthHeaders(),
        ...(options.headers || {}),
        "Content-Type": "application/json",
      },
    }
  );

  const data = await readResponse(response);

  if (!response.ok) {
    throw new Error(
      data?.message ||
        "The skill-matching agent request failed."
    );
  }

  return data;
}

/**
 * Get active Hiring Panelists belonging to
 * the recruiter's company.
 */
export async function getSkillMatchingAgentPanelists() {
  return request("/panelists");
}

/**
 * Start the complete Agentic AI workflow.
 */
export async function startSkillMatchingAgent({
  jobId,
  panelistId,
  objective,
}) {
  return request("/start", {
    method: "POST",
    body: JSON.stringify({
      jobId,
      panelistId,
      objective: objective || null,
    }),
  });
}

/**
 * Get all Agent workflows owned by the
 * currently authenticated recruiter.
 */
export async function getSkillMatchingAgentWorkflows(
  jobId
) {
  const query = jobId
    ? `?jobId=${encodeURIComponent(jobId)}`
    : "";

  return request(query);
}

/**
 * Get one Agent workflow.
 */
export async function getSkillMatchingAgentWorkflow(
  workflowId
) {
  return request(
    `/${encodeURIComponent(workflowId)}`
  );
}

/**
 * Approve the Agent recommendation.
 *
 * IMPORTANT:
 * The backend performs the final deterministic
 * validation and only then dispatches the shortlist.
 */
export async function approveSkillMatchingAgentWorkflow(
  workflowId,
  comment = ""
) {
  return request(
    `/${encodeURIComponent(workflowId)}/approve`,
    {
      method: "POST",
      body: JSON.stringify({
        comment: comment || null,
      }),
    }
  );
}

/**
 * Reject the Agent recommendation.
 */
export async function rejectSkillMatchingAgentWorkflow(
  workflowId,
  comment = ""
) {
  return request(
    `/${encodeURIComponent(workflowId)}/reject`,
    {
      method: "POST",
      body: JSON.stringify({
        comment: comment || null,
      }),
    }
  );
}