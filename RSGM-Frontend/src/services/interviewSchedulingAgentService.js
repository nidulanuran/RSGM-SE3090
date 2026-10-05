import { getAuthHeaders } from "./authService";

const base = import.meta.env.VITE_API_BASE_URL;

async function call(path, method = "GET", body) {
  const response = await fetch(
    `${base}/api/agents/interview-scheduling${path}`,
    {
      method,
      headers: getAuthHeaders(),
      ...(body !== undefined
        ? { body: JSON.stringify(body) }
        : {}),
    }
  );

  const text = await response.text();

  let result;

  try {
    result = text ? JSON.parse(text) : null;
  } catch {
    result = null;
  }

  if (!response.ok) {
    throw new Error(
      result?.message ||
        `Request failed (${response.status}).`
    );
  }

  return result;
}

export const startInterviewSchedulingAgent = (
  applicationId,
  hrManagerId,
  objective
) =>
  call(
    "/start",
    "POST",
    {
      applicationId,
      hrManagerId,
      objective:
        objective?.trim() || null,
    }
  );

export const getInterviewSchedulingWorkflows = () =>
  call("/workflows");

export const getInterviewSchedulingWorkflow = (
  workflowId
) =>
  call(`/workflows/${workflowId}`);

export const approveInterviewMode = (
  workflowId,
  type,
  locationOrLink,
  comment
) =>
  call(
    `/workflows/${workflowId}/mode-approval`,
    "POST",
    {
      type,
      locationOrLink,
      comment:
        comment?.trim() || null,
    }
  );

export const decideInterviewSchedule = (
  workflowId,
  approved,
  comment
) =>
  call(
    `/workflows/${workflowId}/schedule-decision`,
    "POST",
    {
      approved,
      comment:
        comment?.trim() || null,
    }
  );