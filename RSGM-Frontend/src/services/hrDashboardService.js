import { getAuthHeaders } from "./authService";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

async function get(path, fallbackMessage) {
  const response = await fetch(`${API_BASE_URL}/api/hr/dashboard/${path}`, {
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
    throw new Error(data.message || fallbackMessage);
  }

  return data;
}

export const getHrDashboardStats = () =>
  get("stats", "Unable to load HR dashboard statistics.");

export const getHrWorkflows = () =>
  get("workflows", "Unable to load recruitment workflows.");
