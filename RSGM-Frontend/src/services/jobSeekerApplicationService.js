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
    return { message: text };
  }
}

function getErrorMessage(data, fallback) {
  if (!data) {
    return fallback;
  }

  if (typeof data === "string") {
    return data;
  }

  if (data.message) {
    return data.message;
  }

  if (Array.isArray(data.errors) && data.errors.length > 0) {
    return data.errors.join(" ");
  }

  if (data.errors && typeof data.errors === "object") {
    return Object.values(data.errors).flat().join(" ");
  }

  return fallback;
}

// GET /api/jobseeker/applications
export async function getMyApplications() {
  const response = await fetch(
    `${API_BASE_URL}/api/jobseeker/applications`,
    {
      method: "GET",
      headers: getAuthHeaders(),
    },
  );

  const data = await readResponse(response);

  if (!response.ok) {
    throw new Error(
      getErrorMessage(data, "Unable to load applications."),
    );
  }

  return data;
}

// POST /api/jobseeker/applications
// Body: { jobPostingId }
export async function applyToJob(jobPostingId) {
  const response = await fetch(
    `${API_BASE_URL}/api/jobseeker/applications`,
    {
      method: "POST",
      headers: getAuthHeaders(),
      body: JSON.stringify({ jobPostingId }),
    },
  );

  const data = await readResponse(response);

  if (!response.ok) {
    throw new Error(
      getErrorMessage(data, "Unable to submit application."),
    );
  }

  return data;
}

// DELETE /api/jobseeker/applications/{id}
// Backend permits this only while status == UnderReview.
export async function withdrawApplication(id) {
  const response = await fetch(
    `${API_BASE_URL}/api/jobseeker/applications/${id}`,
    {
      method: "DELETE",
      headers: getAuthHeaders(),
    },
  );

  if (response.status === 204) {
    return true;
  }

  const data = await readResponse(response);

  if (!response.ok) {
    throw new Error(
      getErrorMessage(data, "Unable to withdraw application."),
    );
  }

  return true;
}
