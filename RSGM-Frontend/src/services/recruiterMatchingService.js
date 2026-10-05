import { getAuthHeaders } from "./authService";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

async function readResponse(response) {
  const text = await response.text();

  if (!text) {
    return {};
  }

  try {
    return JSON.parse(text);
  } catch {
    return {
      message: text,
    };
  }
}

async function request(path, options = {}) {
  const response = await fetch(
    `${API_BASE_URL}${path}`,
    {
      ...options,
      headers: getAuthHeaders(),
    }
  );

  const data = await readResponse(response);

  if (!response.ok) {
    throw new Error(
      data?.message ||
        "Candidate matching request failed."
    );
  }

  return data;
}

export function runRecruiterMatching(jobId) {
  return request("/api/recruiter/matching/run", {
    method: "POST",
    body: JSON.stringify({ jobId }),
  });
}
