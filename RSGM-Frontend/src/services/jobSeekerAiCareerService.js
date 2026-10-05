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

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      ...getAuthHeaders(),
      ...(options.headers ?? {}),
    },
  });
  const data = await readResponse(response);
  if (!response.ok) {
    throw new Error(data?.message || data?.title || "AI career workflow request failed.");
  }
  return data;
}

export function startCareerWorkflow(objective) {
  return request("/api/jobseeker/ai-career/workflows", {
    method: "POST",
    body: JSON.stringify({ objective }),
  });
}

export function getCareerWorkflows() {
  return request("/api/jobseeker/ai-career/workflows");
}

export function getCareerWorkflow(id) {
  return request(`/api/jobseeker/ai-career/workflows/${id}`);
}

export function approveCareerWorkflow(id, comment = "") {
  return request(`/api/jobseeker/ai-career/workflows/${id}/approve`, {
    method: "POST",
    body: JSON.stringify({ comment }),
  });
}

export function rejectCareerWorkflow(id, comment = "") {
  return request(`/api/jobseeker/ai-career/workflows/${id}/reject`, {
    method: "POST",
    body: JSON.stringify({ comment }),
  });
}

export function reviseCareerWorkflow(id, comment) {
  return request(`/api/jobseeker/ai-career/workflows/${id}/revise`, {
    method: "POST",
    body: JSON.stringify({ comment }),
  });
}
