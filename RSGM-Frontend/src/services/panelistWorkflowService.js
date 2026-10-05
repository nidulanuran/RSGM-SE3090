import { getAuthHeaders } from "./authService";

const base = import.meta.env.VITE_API_BASE_URL;
async function call(path, method = "GET", body) {
  const response = await fetch(`${base}/api/hiring${path}`, {
    method, headers: getAuthHeaders(),
    ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
  });
  const text = await response.text();
  let result;
  try { result = text ? JSON.parse(text) : null; } catch { result = null; }
  if (!response.ok) throw new Error(result?.message || `Request failed (${response.status}).`);
  return result;
}
export const getSentShortlists = () => call("/recruiter/shortlists");
export const sendShortlist = (jobId, panelistId) => call(`/recruiter/jobs/${jobId}/send-shortlist`, "POST", { panelistId });
export const getPanelistShortlists = () => call("/panelist/shortlists");
export const getShortlistedCandidate = (applicationId) =>
  call(`/panelist/shortlists/applications/${applicationId}/candidate`);

export async function downloadShortlistedCandidateCv(
  applicationId,
  fallbackName = "candidate-cv"
) {
  const response = await fetch(
    `${base}/api/hiring/panelist/shortlists/applications/${applicationId}/cv`,
    {
      headers: getAuthHeaders(),
    }
  );

  if (!response.ok) {
    const details = await response.json().catch(() => null);

    throw new Error(details?.message || "CV is unavailable.");
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);

  const link = document.createElement("a");
  link.href = url;
  link.download = fallbackName || "candidate-cv";

  document.body.appendChild(link);
  link.click();
  link.remove();

  setTimeout(() => URL.revokeObjectURL(url), 60000);
}
export const getHrManagers = (jobId) => call(`/panelist/jobs/${jobId}/hr-managers`);
export const getAvailableSlots = (jobId, hrManagerId) => call(`/panelist/jobs/${jobId}/slots?hrManagerId=${encodeURIComponent(hrManagerId)}`);
export const getBusyTimes = () => call("/busy-times");
export const addBusyTime = (details) => call("/busy-times", "POST", details);
export const deleteBusyTime = (id) => call(`/busy-times/${id}`, "DELETE");
export const proposeInterview = (details) => call("/panelist/interviews", "POST", details);
export const getCandidateInterviews = () => call("/jobseeker/interviews");
export const confirmInterview = (id) => call(`/jobseeker/interviews/${id}/confirm`, "POST");
export const requestNewTime = (id, reason) => call(`/jobseeker/interviews/${id}/request-new-time`, "POST", { reason });
export const changeInterviewTime = (id, startsAt) => call(`/panelist/interviews/${id}/new-time`, "PUT", { startsAt });
export const cancelPanelistInterview = (id) => call(`/panelist/interviews/${id}/cancel`, "POST");
export const recommendCandidate = (id, selected, rationale) => call(`/panelist/interviews/${id}/recommend`, "POST", { selected, rationale });
export const getPanelistRecommendations = () => call("/panelist/recommendations");
export const getRecruiterRecommendations = () => call("/recruiter/recommendations");
export const getHrRecommendations = () => call("/hr/recommendations");
