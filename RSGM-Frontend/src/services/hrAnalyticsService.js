import { getAuthHeaders } from "./authService";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export async function getHrAnalytics() {
  const response = await fetch(`${API_BASE_URL}/api/hr/analytics`, {
    headers: getAuthHeaders(),
  });

  const text = await response.text();
  let data = {};

  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = { message: text };
    }
  }

  if (!response.ok) {
    throw new Error(data.message || "Failed to load recruitment analytics.");
  }

  return data;
}
