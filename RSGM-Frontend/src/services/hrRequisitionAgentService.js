import { getAuthHeaders } from "./authService";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

async function readResponse(response) {
  const text = await response.text();
  if (!text) return {};
  try {
    return JSON.parse(text);
  } catch {
    return { message: text };
  }
}

function getErrorMessage(data, fallback) {
  if (!data) return fallback;
  if (data.message) return data.message;
  if (data.errors && typeof data.errors === "object") {
    return Object.values(data.errors).flat().join(" ");
  }
  return fallback;
}

export async function analyzeRequisitionWithHrAgent(requisitionId) {
  const response = await fetch(`${API_BASE_URL}/api/requisitions/${requisitionId}/agent/analyze`, {
    method: "POST",
    headers: getAuthHeaders(),
  });

  const data = await readResponse(response);
  if (!response.ok) {
    throw new Error(getErrorMessage(data, "Failed to run HR Agent analysis."));
  }
  return data;
}

export async function submitRequisitionWithHrApprovalGate(requisitionId) {
  const response = await fetch(`${API_BASE_URL}/api/requisitions/${requisitionId}/agent/submit-with-approval`, {
    method: "POST",
    headers: getAuthHeaders(),
  });

  const data = await readResponse(response);
  if (!response.ok) {
    throw new Error(getErrorMessage(data, "Failed to submit requisition with HR Approval Gate."));
  }
  return data;
}

export async function decideHrApprovalGate(requisitionId, decision, comment = "") {
  const response = await fetch(`${API_BASE_URL}/api/requisitions/${requisitionId}/agent/decision`, {
    method: "POST",
    headers: getAuthHeaders(),
    body: JSON.stringify({
      decision, // 1: Approved, 2: Rejected, 3: RevisionRequested
      comment,
    }),
  });

  const data = await readResponse(response);
  if (!response.ok) {
    throw new Error(getErrorMessage(data, "Failed to process HR approval decision."));
  }
  return data;
}

export async function getHrRequisitionAgentStatus(requisitionId) {
  const response = await fetch(`${API_BASE_URL}/api/requisitions/${requisitionId}/agent/status`, {
    headers: getAuthHeaders(),
  });

  const data = await readResponse(response);
  if (!response.ok) {
    throw new Error(getErrorMessage(data, "Unable to load HR Agent workflow status."));
  }
  return data;
}

export async function getHrRequisitionAgentHistory(requisitionId) {
  const response = await fetch(`${API_BASE_URL}/api/requisitions/${requisitionId}/agent/history`, {
    headers: getAuthHeaders(),
  });

  const data = await readResponse(response);
  if (!response.ok) {
    throw new Error(getErrorMessage(data, "Unable to load HR Agent workflow history."));
  }
  return data;
}
