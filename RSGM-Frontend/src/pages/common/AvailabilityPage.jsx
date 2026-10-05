import { useCallback, useEffect, useState } from "react";
import { CalendarClock, Trash2 } from "lucide-react";
import {
  addBusyTime,
  deleteBusyTime,
  getBusyTimes,
} from "../../services/panelistWorkflowService";

const emptyForm = {
  title: "",
  date: "",
  startTime: "08:00",
  endTime: "09:00",
  description: "",
};

const pad = (value) => String(value).padStart(2, "0");

function localDateValue(date = new Date()) {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function minutesFromTime(value) {
  const [hour, minute] = value.split(":").map(Number);
  return hour * 60 + minute;
}

export default function AvailabilityPage() {
  const [events, setEvents] = useState([]);
  const [form, setForm] = useState(() => ({
    ...emptyForm,
    date: localDateValue(),
  }));
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const path = window.location.pathname;
  const isPanelist = path.startsWith("/panelist");

  const theme = path.startsWith("/hr")
    ? "emerald"
    : isPanelist
      ? "amber"
      : "blue";

  const accent =
    theme === "emerald"
      ? "text-emerald-600"
      : theme === "amber"
        ? "text-amber-600"
        : "text-blue-600";

  const button =
    theme === "emerald"
      ? "bg-emerald-600"
      : theme === "amber"
        ? "bg-amber-600"
        : "bg-blue-600";

  const today = localDateValue();

  const refresh = useCallback(
    () =>
      getBusyTimes()
        .then(setEvents)
        .catch((e) => setError(e.message)),
    [],
  );

  useEffect(() => {
    refresh();
  }, [refresh]);

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setError("");

    try {
      const start = new Date(`${form.date}T${form.startTime}:00`);
      const end = new Date(`${form.date}T${form.endTime}:00`);
      const now = new Date();
      const weekday = start.getDay() >= 1 && start.getDay() <= 5;

      if (start <= now) {
        throw new Error("Start date and time must be in the future.");
      }

      if (
        !weekday ||
        start.toDateString() !== end.toDateString() ||
        minutesFromTime(form.startTime) < 480 ||
        minutesFromTime(form.endTime) > 1020 ||
        end <= start
      ) {
        throw new Error(
          "Busy times must be on one weekday between 8:00 AM and 5:00 PM.",
        );
      }

      await addBusyTime({
        title: form.title.trim(),
        description: form.description.trim() || null,
        startsAt: start.toISOString(),
        endsAt: end.toISOString(),
      });

      setForm({ ...emptyForm, date: localDateValue() });
      await refresh();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function remove(id) {
    setBusy(true);
    setError("");

    try {
      await deleteBusyTime(id);
      await refresh();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  const update = (key) => (e) =>
    setForm((current) => ({
      ...current,
      [key]: e.target.value,
    }));



  return (
    <div className="space-y-6">
      <div>
        <p className={`text-xs font-semibold uppercase tracking-wider ${accent}`}>
          Interview planning
        </p>

        <h1 className="mt-3 text-3xl font-semibold">My schedule</h1>

        <p className="mt-2 text-sm text-neutral-500">
          {isPanelist
            ? "View your unavailable periods here. Manage your availability from the Hiring Panelist mobile app."
            : "Add meetings and other unavailable periods during weekday interview hours, 8:00 AM to 5:00 PM. Past dates are not allowed, and all remaining times are treated as available automatically."}
        </p>
      </div>

      {error && (
        <p role="alert" className="rounded-xl bg-red-50 p-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {!isPanelist && (
        <form
          onSubmit={submit}
          className="grid gap-4 rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm sm:grid-cols-2"
        >
          <label className="text-sm font-medium sm:col-span-2">
            Meeting or event name
            <input
              required
              maxLength={150}
              value={form.title}
              onChange={update("title")}
              placeholder="Weekly team meeting"
              className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5"
            />
          </label>

          <label className="text-sm font-medium sm:col-span-2">
            Date
            <input
              type="date"
              required
              min={today}
              value={form.date}
              onChange={update("date")}
              className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5"
            />
          </label>

          <label className="text-sm font-medium">
            Start time
            <input
              type="time"
              required
              min="08:00"
              max="16:59"
              step="60"
              value={form.startTime}
              onChange={update("startTime")}
              className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5"
            />
          </label>

          <label className="text-sm font-medium">
            End time
            <input
              type="time"
              required
              min="08:00"
              max="17:00"
              step="60"
              value={form.endTime}
              onChange={update("endTime")}
              className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5"
            />
          </label>

          <p className="text-xs text-neutral-500 sm:col-span-2">
            Available scheduling window: Monday-Friday, 8:00 AM-5:00 PM. You can choose any minute within this window, for example 8:15 AM to 9:15 AM.
          </p>

          <label className="text-sm font-medium sm:col-span-2">
            Description <span className="font-normal text-neutral-400">(optional)</span>
            <textarea
              maxLength={500}
              rows={2}
              value={form.description}
              onChange={update("description")}
              className="mt-2 block w-full rounded-xl border border-neutral-200 px-3 py-2.5"
            />
          </label>

          <button
            disabled={busy}
            className={`rounded-xl ${button} px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50 sm:col-span-2`}
          >
            Add busy time
          </button>
        </form>
      )}

      <div className="space-y-2">
        <h2 className="font-semibold">Upcoming busy times</h2>

        {events.length === 0 && (
          <p className="rounded-2xl bg-white p-5 text-sm text-neutral-500">
            {isPanelist
              ? "No unavailable periods recorded."
              : "No busy times added. Your office hours are currently available for interview scheduling."}
          </p>
        )}

        {events.map((event) => (
          <div
            key={event.id}
            className="flex items-start gap-3 rounded-2xl border border-neutral-200 bg-white p-4 shadow-sm"
          >
            <CalendarClock className={accent} size={19} />
            <div className="flex-1">
              <p className="text-sm font-semibold">{event.title}</p>
              <p className="mt-1 text-sm text-neutral-600">
                {new Date(event.startsAt).toLocaleString()} - {new Date(event.endsAt).toLocaleString()}
              </p>
              {event.description && (
                <p className="mt-1 text-xs text-neutral-500">{event.description}</p>
              )}
            </div>

            {!isPanelist && (
              <button
                type="button"
                aria-label="Delete busy time"
                disabled={busy}
                onClick={() => remove(event.id)}
                className="rounded-lg p-2 text-red-600 hover:bg-red-50"
              >
                <Trash2 size={17} />
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
