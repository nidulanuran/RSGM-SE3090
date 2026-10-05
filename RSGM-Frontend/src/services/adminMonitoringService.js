import { getAuthHeaders } from "./authService";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

async function request(path) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "GET",
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
    throw new Error(data.message || "Unable to load admin monitoring data.");
  }

  return data;
}

export const getAdminAuditLogs = (limit = 100) =>
  request(`/api/admin/audit-logs?limit=${limit}`);

export const getAdminAgentWorkflows = (limit = 100) =>
  request(`/api/admin/agent-workflows?limit=${limit}`);

export const getAdminStatistics = () =>
  request("/api/admin/stats");
