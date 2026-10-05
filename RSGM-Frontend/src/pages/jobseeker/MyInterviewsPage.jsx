import { useCallback, useEffect, useState } from "react";
import { CalendarClock, X } from "lucide-react";
import {
  confirmInterview,
  getCandidateInterviews,
  requestNewTime,
} from "../../services/panelistWorkflowService";

export default function MyInterviewsPage() {
  const [rows, setRows] = useState([]);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const [rescheduleInterview, setRescheduleInterview] = useState(null);
  const [rescheduleReason, setRescheduleReason] = useState("");
  const [reasonError, setReasonError] = useState("");

  const refresh = useCallback(
    () =>
      getCandidateInterviews()
        .then(setRows)
        .catch((e) => setError(e.message)),
    [],
  );

  useEffect(() => {
    refresh();
  }, [refresh]);

  async function act(action) {
    setBusy(true);
    setError("");

    try {
      await action();
      await refresh();
    } catch (e) {
      setError(e.message || "Something went wrong. Please try again.");
    } finally {
      setBusy(false);
    }
  }

  function openRescheduleModal(interview) {
    setRescheduleInterview(interview);
    setRescheduleReason("");
    setReasonError("");
  }

  function closeRescheduleModal() {
    if (busy) return;

    setRescheduleInterview(null);
    setRescheduleReason("");
    setReasonError("");
  }

  async function submitRescheduleRequest(e) {
    e.preventDefault();

    const reason = rescheduleReason.trim();

    if (!reason) {
      setReasonError("Please enter a reason for requesting a different time.");
      return;
    }

    if (reason.length < 10) {
      setReasonError("Please provide a little more detail (at least 10 characters).");
      return;
    }

    if (!rescheduleInterview) return;

    setReasonError("");

    await act(() => requestNewTime(rescheduleInterview.id, reason));

    setRescheduleInterview(null);
    setRescheduleReason("");
  }

  return (
    <div>
      <p className="text-xs font-semibold uppercase text-violet-600">
        Your applications
      </p>

      <h1 className="mt-3 text-3xl font-semibold">My interviews</h1>

      <p className="mt-2 text-sm text-neutral-500">
        Confirm a proposed interview or ask the panelist to propose a different
        time.
      </p>

      {error && (
        <p
          role="alert"
          className="mt-5 rounded-xl bg-red-50 p-3 text-red-700"
        >
          {error}
        </p>
      )}

      <div className="mt-7 space-y-3">
        {rows.length === 0 && (
          <p className="rounded-2xl bg-white p-5 text-neutral-500">
            No interviews yet.
          </p>
        )}

        {rows.map((i) => (
          <article
            key={i.id}
            className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm"
          >
            <div className="flex items-start gap-3">
              <CalendarClock className="text-violet-600" />

              <div className="flex-1">
                <h2 className="font-semibold">{i.job}</h2>

                <p className="mt-1 text-sm text-neutral-600">
                  {new Date(i.scheduledAt).toLocaleString()} · {i.type}
                </p>

                <p className="mt-1 text-sm text-neutral-500">
                  {i.locationOrLink}
                </p>

                <span className="mt-2 inline-block rounded-full bg-violet-50 px-3 py-1 text-xs font-semibold text-violet-700">
                  {i.status}
                </span>
              </div>
            </div>

            {new Date(i.scheduledAt) > new Date() &&
              ["Proposed", "Scheduled"].includes(i.status) && (
                <div className="mt-4 flex flex-wrap gap-2">
                  {i.status === "Proposed" && (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => act(() => confirmInterview(i.id))}
                      className="rounded-xl bg-violet-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-violet-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {busy ? "Please wait..." : "Confirm interview"}
                    </button>
                  )}

                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => openRescheduleModal(i)}
                    className="rounded-xl border border-neutral-200 px-4 py-2 text-sm font-semibold transition hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Request new time
                  </button>
                </div>
              )}
          </article>
        ))}
      </div>

      {rescheduleInterview && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 px-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="reschedule-title"
          onMouseDown={(e) => {
            if (e.target === e.currentTarget) {
              closeRescheduleModal();
            }
          }}
        >
          <div className="w-full max-w-lg rounded-3xl bg-white p-6 shadow-2xl">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-violet-600">
                  Interview reschedule request
                </p>

                <h2
                  id="reschedule-title"
                  className="mt-2 text-xl font-semibold text-neutral-900"
                >
                  Request a different time
                </h2>

                <p className="mt-2 text-sm leading-6 text-neutral-500">
                  Tell the panelist why you need another interview time. They
                  can review your reason and propose a new slot.
                </p>
              </div>

              <button
                type="button"
                onClick={closeRescheduleModal}
                disabled={busy}
                aria-label="Close dialog"
                className="rounded-xl p-2 text-neutral-500 transition hover:bg-neutral-100 hover:text-neutral-900 disabled:opacity-50"
              >
                <X size={20} />
              </button>
            </div>

            <div className="mt-5 rounded-2xl bg-violet-50 p-4">
              <p className="text-sm font-semibold text-neutral-900">
                {rescheduleInterview.job}
              </p>

              <p className="mt-1 text-sm text-neutral-600">
                {new Date(rescheduleInterview.scheduledAt).toLocaleString()} ·{" "}
                {rescheduleInterview.type}
              </p>

              {rescheduleInterview.locationOrLink && (
                <p className="mt-1 text-sm text-neutral-500">
                  {rescheduleInterview.locationOrLink}
                </p>
              )}
            </div>

            <form onSubmit={submitRescheduleRequest} className="mt-5">
              <label
                htmlFor="reschedule-reason"
                className="text-sm font-semibold text-neutral-800"
              >
                Reason for requesting a different time
              </label>

              <textarea
                id="reschedule-reason"
                rows={5}
                maxLength={500}
                autoFocus
                value={rescheduleReason}
                onChange={(e) => {
                  setRescheduleReason(e.target.value);
                  if (reasonError) setReasonError("");
                }}
                placeholder="For example: I have a university exam at the proposed time. Could I please have another slot later that day or on the following day?"
                className={`mt-2 w-full resize-none rounded-2xl border px-4 py-3 text-sm outline-none transition placeholder:text-neutral-400 ${
                  reasonError
                    ? "border-red-400 focus:border-red-500 focus:ring-4 focus:ring-red-100"
                    : "border-neutral-200 focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
                }`}
              />

              <div className="mt-2 flex items-start justify-between gap-3">
                <div>
                  {reasonError && (
                    <p role="alert" className="text-sm text-red-600">
                      {reasonError}
                    </p>
                  )}
                </div>

                <p className="shrink-0 text-xs text-neutral-400">
                  {rescheduleReason.length}/500
                </p>
              </div>

              <div className="mt-6 flex justify-end gap-3">
                <button
                  type="button"
                  onClick={closeRescheduleModal}
                  disabled={busy}
                  className="rounded-xl border border-neutral-200 px-4 py-2.5 text-sm font-semibold text-neutral-700 transition hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  disabled={busy}
                  className="rounded-xl bg-neutral-900 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-neutral-800 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {busy ? "Sending request..." : "Send request"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
