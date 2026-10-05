import { useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";

import {
  Briefcase,
  CalendarClock,
  FileStack,
  LayoutDashboard,
  ListTree,
  LogOut,
  Menu,
  ScanSearch,
  Shield,
  Users,
  X,
  Bot,
} from "lucide-react";

import Brand from "../components/common/Brand";
import NotificationsNavLink from "../components/common/NotificationsNavLink";
import {
  getCurrentUser,
  logout,
} from "../services/authService";

const NAV_ITEMS = [
  {
    to: "/recruiter",
    label: "Dashboard",
    icon: LayoutDashboard,
    end: true,
  },
  {
    to: "/recruiter/requisitions",
    label: "Requisitions",
    icon: ListTree,
  },
  {
    to: "/recruiter/postings",
    label: "Job Postings",
    icon: Briefcase,
  },
  {
    to: "/recruiter/applications",
    label: "Applications",
    icon: FileStack,
  },
  {
    to: "/recruiter/matching",
    label: "Candidate Matching",
    icon: ScanSearch,
  },

  {
    to: "/recruiter/ai-shortlisting",
    label: "AI Shortlisting Agent",
    icon: Bot,
  },
  {
    to: "/recruiter/shortlists",
    label: "Shortlists",
    icon: Users,
  },
  { to: "/recruiter/schedule", label: "My Schedule", icon: CalendarClock },
  {
    to: "/recruiter/interviews",
    label: "Interviews",
    icon: CalendarClock,
  },
];

function RecruiterLayout() {
  const navigate = useNavigate();

  const [mobileOpen, setMobileOpen] =
    useState(false);

  const user = getCurrentUser();

  const handleLogout = () => {
    logout();

    navigate("/login", {
      replace: true,
    });
  };

  return (
    <div className="min-h-screen bg-[#FAFAFA] text-neutral-900">
      {/* BACKGROUND DECORATION */}

      <div className="fixed inset-0 pointer-events-none overflow-hidden">
        <div className="absolute -top-32 left-[15%] w-105 h-105 bg-blue-300/25 blur-[140px] rounded-full" />

        <div className="absolute top-[50%] -right-40 w-100 h-100 bg-cyan-200/30 blur-[140px] rounded-full" />
      </div>

      <div className="relative flex">
        {/* DESKTOP SIDEBAR */}

        <aside className="hidden lg:flex lg:flex-col lg:w-72 lg:shrink-0 lg:h-screen lg:sticky lg:top-0 border-r border-neutral-200/70 bg-white/60 backdrop-blur-xl">
          <div className="px-6 py-6 border-b border-neutral-200/70">
            <Brand />
          </div>

          <nav className="flex-1 px-4 py-6 space-y-1.5 overflow-y-auto">
            {NAV_ITEMS.map((item) => (
              <SidebarLink
                key={item.to}
                {...item}
              />
            ))}
            <NotificationsNavLink to="/recruiter/notifications" accent="blue" />
          </nav>

          {/* USER INFORMATION */}

          <div className="px-4 py-5 border-t border-neutral-200/70">
            <div className="flex items-center gap-3 px-3 py-2.5 rounded-xl bg-neutral-50">
              <div className="w-9 h-9 rounded-full bg-blue-100 flex items-center justify-center shrink-0">
                <Shield
                  size={16}
                  className="text-blue-600"
                />
              </div>

              <div className="min-w-0">
                <p className="text-sm font-medium truncate">
                  {user?.email ?? "Recruiter"}
                </p>

                <p className="text-xs text-neutral-400">
                  Recruiter
                </p>
              </div>
            </div>

            <button
              type="button"
              onClick={handleLogout}
              className="mt-3 w-full flex items-center justify-center gap-2 h-10 rounded-xl text-sm font-medium text-neutral-500 border border-neutral-200 hover:bg-neutral-100 hover:text-neutral-900 transition"
            >
              <LogOut size={15} />

              Sign out
            </button>
          </div>
        </aside>

        {/* MOBILE SIDEBAR */}

        {mobileOpen && (
          <div className="lg:hidden fixed inset-0 z-40">
            <div
              className="absolute inset-0 bg-black/30 backdrop-blur-sm"
              onClick={() =>
                setMobileOpen(false)
              }
            />

            <aside className="absolute left-0 top-0 h-full w-72 bg-white flex flex-col shadow-2xl">
              <div className="px-6 py-6 border-b border-neutral-200 flex items-center justify-between">
                <Brand />

                <button
                  type="button"
                  onClick={() =>
                    setMobileOpen(false)
                  }
                >
                  <X
                    size={20}
                    className="text-neutral-500"
                  />
                </button>
              </div>

              <nav className="flex-1 px-4 py-6 space-y-1.5 overflow-y-auto">
                {NAV_ITEMS.map((item) => (
                  <SidebarLink
                    key={item.to}
                    {...item}
                    onClick={() =>
                      setMobileOpen(false)
                    }
                  />
                ))}
                <NotificationsNavLink to="/recruiter/notifications" accent="blue" onClick={() => setMobileOpen(false)} />
              </nav>

              <div className="px-4 py-5 border-t border-neutral-200">
                <button
                  type="button"
                  onClick={handleLogout}
                  className="w-full flex items-center justify-center gap-2 h-10 rounded-xl text-sm font-medium text-neutral-500 border border-neutral-200 hover:bg-neutral-100 transition"
                >
                  <LogOut size={15} />

                  Sign out
                </button>
              </div>
            </aside>
          </div>
        )}

        {/* MAIN CONTENT */}

        <div className="flex-1 min-w-0">
          {/* MOBILE HEADER */}

          <header className="lg:hidden flex items-center justify-between px-5 py-4 border-b border-neutral-200/70 bg-white/70 backdrop-blur-xl sticky top-0 z-30">
            <Brand />

            <button
              type="button"
              onClick={() =>
                setMobileOpen(true)
              }
              className="w-10 h-10 rounded-xl border border-neutral-200 flex items-center justify-center"
            >
              <Menu size={18} />
            </button>
          </header>

          <main className="relative z-10 px-5 sm:px-8 py-8 max-w-6xl mx-auto">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  );
}

function SidebarLink({
  to,
  label,
  icon: Icon,
  end,
  onClick,
}) {
  return (
    <NavLink
      to={to}
      end={end}
      onClick={onClick}
      className={({ isActive }) =>
        `flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition ${isActive
          ? "bg-neutral-900 text-white shadow-sm"
          : "text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900"
        }`
      }
    >
      <Icon size={17} />

      {label}
    </NavLink>
  );
}

export default RecruiterLayout;
