import { useState } from "react";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";

import {
  ArrowRight,
  ArrowUpRight,
  BrainCircuit,
  BriefcaseBusiness,
  Check,
  Menu,
  Minus,
  Plus,
  Search,
  Sparkles,
  Users,
  X,
} from "lucide-react";

function LandingPage() {
  const [mobileOpen, setMobileOpen] = useState(false);

  const features = [
    {
      icon: BrainCircuit,
      title: "Smart Skill Matching",
      description:
        "Match candidate skills with job requirements and identify relevant skill gaps.",
    },
    {
      icon: Search,
      title: "AI-Assisted Shortlisting",
      description:
        "Support recruiters with candidate insights and recommendations during shortlisting.",
    },
    {
      icon: BriefcaseBusiness,
      title: "Recruitment Workflow",
      description:
        "Manage job postings, applications, interviews and offers through one connected workflow.",
    },
  ];

  const steps = [
    {
      number: "01",
      title: "Create a Job",
      description:
        "Recruiters create job requisitions with the required skills and qualifications.",
    },
    {
      number: "02",
      title: "Review Applications",
      description:
        "Candidates apply and recruiters manage applications through one platform.",
    },
    {
      number: "03",
      title: "Match Skills",
      description:
        "Candidate skills are compared with the requirements of the position.",
    },
    {
      number: "04",
      title: "Hire with Confidence",
      description:
        "Recruiters review candidate insights before interviews and offers.",
    },
  ];

  return (
    <div className="min-h-screen bg-[#FAFAFA] text-neutral-900 overflow-x-hidden">

      {/* =========================================================
          NAVBAR
      ========================================================= */}
      <header className="fixed top-0 left-0 right-0 z-50 pt-3 sm:pt-4 px-4 sm:px-6">
        <div className="max-w-6xl mx-auto">

          <nav className="relative h-14 sm:h-16 px-4 sm:px-6 flex items-center justify-between rounded-full border border-white/40 bg-white/15 backdrop-blur-3xl shadow-[0_8px_32px_0_rgba(0,0,0,0.06)] transition-all duration-300 hover:bg-white/25 hover:border-white/60 hover:shadow-[0_8px_32px_0_rgba(0,0,0,0.1)]">

            {/* Glass highlight */}
            <div className="absolute inset-0 rounded-full bg-linear-to-b from-white/40 via-white/5 to-transparent pointer-events-none" />

            {/* LOGO */}
            <Link
              to="/"
              className="relative z-10 flex items-center gap-3 group"
            >
              <div className="w-8 h-8 sm:w-9 sm:h-9 rounded-full bg-neutral-900/90 text-white flex items-center justify-center font-bold text-sm shadow-md transition-transform duration-300 group-hover:scale-105">
                H
              </div>

              <span className="font-semibold tracking-tight text-lg text-neutral-900 drop-shadow-xs">
                Hireon
              </span>
            </Link>

            {/* DESKTOP NAVIGATION */}
            <div className="relative z-10 hidden md:flex items-center gap-1 text-sm font-medium text-neutral-800 bg-neutral-900/5 p-1 rounded-full border border-white/30 backdrop-blur-md">

              <a
                href="#features"
                className="px-4 py-1.5 rounded-full hover:text-neutral-950 hover:bg-white/40 transition-all duration-200"
              >
                Features
              </a>

              <a
                href="#how-it-works"
                className="px-4 py-1.5 rounded-full hover:text-neutral-950 hover:bg-white/40 transition-all duration-200"
              >
                How it works
              </a>

              <a
                href="#about"
                className="px-4 py-1.5 rounded-full hover:text-neutral-950 hover:bg-white/40 transition-all duration-200"
              >
                About
              </a>
            </div>

            {/* DESKTOP AUTH BUTTONS */}
            <div className="relative z-10 hidden md:flex items-center gap-2">

              <Link
                to="/login"
                className="px-4 py-2 rounded-full text-sm font-medium text-neutral-700 hover:bg-white/40 hover:text-neutral-950 transition"
              >
                Log in
              </Link>

              <Link
                to="/register"
                className="flex items-center gap-2 bg-neutral-900/90 hover:bg-neutral-950 text-white px-5 py-2 rounded-full text-sm font-semibold shadow-md backdrop-blur-md transition-all duration-300 hover:scale-[1.02] active:scale-95"
              >
                Get Started
                <ArrowUpRight size={15} />
              </Link>

            </div>

            {/* MOBILE MENU BUTTON */}
            <button
              onClick={() => setMobileOpen(!mobileOpen)}
              className="relative z-10 md:hidden p-2 text-neutral-900 hover:bg-white/30 rounded-full transition"
              aria-label="Toggle mobile menu"
            >
              {mobileOpen ? <X size={22} /> : <Menu size={22} />}
            </button>

          </nav>

          {/* MOBILE MENU */}
          {mobileOpen && (
            <div className="md:hidden mt-2 p-5 rounded-3xl border border-white/40 bg-white/70 backdrop-blur-3xl shadow-xl">

              <div className="flex flex-col gap-2 text-neutral-800 font-medium">

                <a
                  href="#features"
                  onClick={() => setMobileOpen(false)}
                  className="px-3 py-2.5 rounded-xl hover:bg-white/60 transition"
                >
                  Features
                </a>

                <a
                  href="#how-it-works"
                  onClick={() => setMobileOpen(false)}
                  className="px-3 py-2.5 rounded-xl hover:bg-white/60 transition"
                >
                  How it works
                </a>

                <a
                  href="#about"
                  onClick={() => setMobileOpen(false)}
                  className="px-3 py-2.5 rounded-xl hover:bg-white/60 transition"
                >
                  About
                </a>

                <div className="my-2 border-t border-neutral-200" />

                <Link
                  to="/login"
                  onClick={() => setMobileOpen(false)}
                  className="px-3 py-2.5 rounded-xl text-center border border-neutral-200 bg-white hover:bg-neutral-50 transition"
                >
                  Log in
                </Link>

                <Link
                  to="/register"
                  onClick={() => setMobileOpen(false)}
                  className="bg-neutral-900 text-white text-center py-3 rounded-full font-semibold shadow-md active:scale-95 transition"
                >
                  Create Account
                </Link>

              </div>
            </div>
          )}

        </div>
      </header>

      {/* =========================================================
          HERO
      ========================================================= */}
      <section className="relative min-h-screen flex items-center pt-28 sm:pt-32">

        {/* BACKGROUND GLOWS */}
        <div className="absolute inset-0 pointer-events-none overflow-hidden">
          <div className="absolute top-[5%] left-[42%] w-150 h-125 bg-violet-300/40 blur-[140px] rounded-full" />

          <div className="absolute top-[40%] -right-37.5 w-100 h-100 bg-blue-200/50 blur-[130px] rounded-full" />
        </div>

        {/* GRID BACKGROUND */}
        <div
          className="absolute inset-0 opacity-[0.04]"
          style={{
            backgroundImage:
              "linear-gradient(rgba(0,0,0,.6) 1px, transparent 1px), linear-gradient(90deg, rgba(0,0,0,.6) 1px, transparent 1px)",
            backgroundSize: "70px 70px",
          }}
        />

        <div className="relative max-w-7xl mx-auto px-5 lg:px-8 w-full">

          <div className="grid lg:grid-cols-[0.9fr_1.1fr] gap-12 lg:gap-16 items-center">

            {/* LEFT SIDE */}
            <motion.div
              initial={{
                opacity: 0,
                y: 30,
              }}
              animate={{
                opacity: 1,
                y: 0,
              }}
              transition={{
                duration: 0.7,
              }}
              className="relative z-10"
            >

              {/* BADGE */}
              <div className="inline-flex items-center gap-2 px-3.5 py-2 rounded-full border border-neutral-200 bg-white text-xs text-neutral-600 shadow-sm">

                <Sparkles
                  size={14}
                  className="text-violet-500"
                />

                Recruitment & Skill-Gap Matching
              </div>

              {/* HEADING */}
              <h1 className="mt-7 text-[48px] sm:text-[58px] lg:text-[62px] xl:text-[68px] font-semibold tracking-[-0.055em] leading-[0.96]">
                Find the right
                <br />

                <span className="text-transparent bg-clip-text bg-linear-to-r from-violet-600 via-blue-500 to-cyan-500">
                  talent faster.
                </span>
              </h1>

              {/* DESCRIPTION */}
              <p className="mt-7 max-w-130 text-base sm:text-lg text-neutral-500 leading-relaxed">
                Hireon connects job requirements with candidate skills to make
                recruitment smarter, faster and more transparent.
              </p>

              {/* BUTTONS */}
              <div className="mt-9 flex flex-wrap gap-3">

                <Link
                  to="/register"
                  className="group flex items-center gap-3 bg-neutral-900 text-white px-5 py-3 rounded-full font-semibold text-sm hover:bg-neutral-800 transition"
                >
                  Get Started

                  <span className="w-7 h-7 rounded-full bg-white text-neutral-900 flex items-center justify-center">
                    <ArrowRight size={14} />
                  </span>
                </Link>

                <a
                  href="#how-it-works"
                  className="flex items-center gap-2 px-5 py-3 rounded-full border border-neutral-200 text-neutral-600 text-sm hover:bg-neutral-100 hover:text-neutral-900 transition"
                >
                  See how it works
                </a>

              </div>

            </motion.div>

            {/* =====================================================
                DASHBOARD PREVIEW
            ===================================================== */}
            <motion.div
              initial={{
                opacity: 0,
                x: 40,
              }}
              animate={{
                opacity: 1,
                x: 0,
              }}
              transition={{
                duration: 0.8,
                delay: 0.15,
              }}
              className="relative"
            >

              <div className="absolute -inset-12 bg-violet-300/30 blur-[110px] rounded-full" />

              <div className="relative rounded-[22px] border border-neutral-200 bg-white shadow-2xl shadow-neutral-300/40 overflow-hidden">

                {/* macOS BAR */}
                <div className="group/mac h-11 border-b border-neutral-200 flex items-center px-5 gap-2 bg-neutral-50/50">

                  {/* RED */}
                  <div className="w-3 h-3 rounded-full bg-[#FF5F56] border border-[#E0443E] flex items-center justify-center">
                    <X
                      size={8}
                      className="text-[#800000] opacity-0 group-hover/mac:opacity-100 transition-opacity"
                    />
                  </div>

                  {/* YELLOW */}
                  <div className="w-3 h-3 rounded-full bg-[#FFBD2E] border border-[#DEA123] flex items-center justify-center">
                    <Minus
                      size={8}
                      className="text-[#996600] opacity-0 group-hover/mac:opacity-100 transition-opacity"
                    />
                  </div>

                  {/* GREEN */}
                  <div className="w-3 h-3 rounded-full bg-[#27C93F] border border-[#1AAB29] flex items-center justify-center">
                    <Plus
                      size={8}
                      className="text-[#006600] opacity-0 group-hover/mac:opacity-100 transition-opacity"
                    />
                  </div>

                  {/* SEARCH BAR */}
                  <div className="ml-4 h-6 flex-1 max-w-67.5 rounded-md bg-neutral-100 border border-neutral-200/60" />

                </div>

                {/* DASHBOARD CONTENT */}
                <div className="p-4 sm:p-5">

                  {/* HEADER */}
                  <div className="flex items-center justify-between">

                    <div>
                      <p className="text-[9px] sm:text-[10px] text-neutral-400">
                        RECRUITER DASHBOARD
                      </p>

                      <h3 className="mt-1 text-base sm:text-lg font-semibold">
                        Good morning
                      </h3>
                    </div>

                    <div className="w-8 sm:w-9 h-8 sm:h-9 rounded-full bg-violet-100 flex items-center justify-center">
                      <Users
                        size={16}
                        className="text-violet-600"
                      />
                    </div>

                  </div>

                  {/* STATS */}
                  <div className="grid grid-cols-3 gap-2.5 mt-5">

                    <div className="p-3 sm:p-4 rounded-xl bg-neutral-50 border border-neutral-200">
                      <p className="text-[9px] text-neutral-400">
                        Applications
                      </p>

                      <p className="mt-2 text-base sm:text-lg font-semibold">
                        128
                      </p>
                    </div>

                    <div className="p-3 sm:p-4 rounded-xl bg-neutral-50 border border-neutral-200">
                      <p className="text-[9px] text-neutral-400">
                        Shortlisted
                      </p>

                      <p className="mt-2 text-base sm:text-lg font-semibold">
                        24
                      </p>
                    </div>

                    <div className="p-3 sm:p-4 rounded-xl bg-neutral-50 border border-neutral-200">
                      <p className="text-[9px] text-neutral-400">
                        Open Jobs
                      </p>

                      <p className="mt-2 text-base sm:text-lg font-semibold">
                        12
                      </p>
                    </div>

                  </div>

                  {/* TOP MATCH */}
                  <div className="mt-4 p-4 sm:p-5 rounded-xl bg-neutral-50 border border-neutral-200">

                    <div className="flex items-center justify-between">

                      <div>
                        <p className="text-[9px] text-neutral-400">
                          TOP MATCH
                        </p>

                        <p className="mt-1 text-xs sm:text-sm font-semibold">
                          Software Engineer
                        </p>
                      </div>

                      <div className="text-right">
                        <p className="text-xl sm:text-2xl font-semibold text-violet-600">
                          92%
                        </p>

                        <p className="text-[8px] text-neutral-400">
                          SKILL MATCH
                        </p>
                      </div>

                    </div>

                    {/* MATCH BAR */}
                    <div className="mt-4 h-1.5 bg-neutral-200 rounded-full overflow-hidden">

                      <motion.div
                        initial={{
                          width: 0,
                        }}
                        animate={{
                          width: "92%",
                        }}
                        transition={{
                          duration: 1,
                          delay: 0.7,
                        }}
                        className="h-full rounded-full bg-linear-to-r from-violet-500 via-blue-400 to-cyan-400"
                      />

                    </div>

                    {/* SKILLS */}
                    <div className="flex gap-1.5 mt-4 flex-wrap">

                      {["React", ".NET", "SQL", "REST API"].map(
                        (skill) => (
                          <span
                            key={skill}
                            className="px-2.5 py-1.5 rounded-full bg-neutral-100 text-[9px] text-neutral-500"
                          >
                            {skill}
                          </span>
                        )
                      )}

                    </div>
                  </div>

                  {/* CANDIDATES */}
                  <div className="mt-4 space-y-2">

                    {[92, 87, 81].map((score, index) => (
                      <motion.div
                        key={score}
                        initial={{
                          opacity: 0,
                          x: 15,
                        }}
                        animate={{
                          opacity: 1,
                          x: 0,
                        }}
                        transition={{
                          delay: 0.8 + index * 0.15,
                        }}
                        className="flex items-center justify-between p-2.5 sm:p-3 rounded-xl bg-neutral-50 border border-neutral-100"
                      >

                        <div className="flex items-center gap-2.5">

                          <div className="w-7 h-7 rounded-full bg-linear-to-br from-neutral-200 to-neutral-100" />

                          <div>
                            <p className="text-[9px] sm:text-[10px] font-medium">
                              Candidate {index + 1}
                            </p>

                            <p className="text-[7px] sm:text-[8px] text-neutral-400">
                              Skills matched
                            </p>
                          </div>

                        </div>

                        <span className="text-[9px] sm:text-[10px] text-violet-600 font-medium">
                          {score}%
                        </span>

                      </motion.div>
                    ))}

                  </div>
                </div>
              </div>

            </motion.div>
          </div>
        </div>
      </section>

      {/* =========================================================
          BENEFITS
      ========================================================= */}
      <section className="border-y border-neutral-200 bg-white">

        <div className="max-w-7xl mx-auto px-5 lg:px-8">

          <div className="grid md:grid-cols-3">

            <div className="py-6 sm:py-7 flex items-center justify-center gap-3 md:border-r border-neutral-200">
              <BrainCircuit
                size={19}
                className="text-violet-500"
              />

              <span className="text-xs sm:text-sm text-neutral-500">
                AI-assisted skill matching
              </span>
            </div>

            <div className="py-6 sm:py-7 flex items-center justify-center gap-3 md:border-r border-neutral-200">

              <Search
                size={19}
                className="text-blue-500"
              />

              <span className="text-xs sm:text-sm text-neutral-500">
                Candidate insights
              </span>

            </div>

            <div className="py-6 sm:py-7 flex items-center justify-center gap-3">

              <Check
                size={19}
                className="text-cyan-500"
              />

              <span className="text-xs sm:text-sm text-neutral-500">
                Human-approved decisions
              </span>

            </div>

          </div>
        </div>
      </section>

      {/* =========================================================
          FEATURES
      ========================================================= */}
      <section
        id="features"
        className="py-24 sm:py-28 lg:py-36"
      >

        <div className="max-w-7xl mx-auto px-5 lg:px-8">

          <div className="max-w-2xl">

            <p className="text-xs sm:text-sm text-violet-600 font-semibold">
              FEATURES
            </p>

            <h2 className="mt-4 text-4xl sm:text-5xl font-semibold tracking-tight">
              Everything you need
              <br />

              <span className="text-neutral-400">
                to hire better.
              </span>
            </h2>

            <p className="mt-5 text-neutral-500 leading-relaxed">
              Hireon brings the important parts of the recruitment process
              together in one simple platform.
            </p>

          </div>

          <div className="grid md:grid-cols-3 gap-4 sm:gap-5 mt-14 sm:mt-16">

            {features.map((feature, index) => {
              const Icon = feature.icon;

              return (
                <motion.div
                  key={feature.title}
                  initial={{
                    opacity: 0,
                    y: 25,
                  }}
                  whileInView={{
                    opacity: 1,
                    y: 0,
                  }}
                  viewport={{
                    once: true,
                  }}
                  transition={{
                    delay: index * 0.1,
                  }}
                  className="group p-6 sm:p-8 rounded-3xl border border-neutral-200 bg-white hover:shadow-xl hover:shadow-neutral-200/60 hover:-translate-y-1 transition duration-300"
                >

                  <div className="w-11 h-11 rounded-2xl bg-violet-100 flex items-center justify-center">
                    <Icon
                      size={21}
                      className="text-violet-600"
                    />
                  </div>

                  <h3 className="mt-7 text-lg sm:text-xl font-semibold">
                    {feature.title}
                  </h3>

                  <p className="mt-3 text-sm text-neutral-500 leading-relaxed">
                    {feature.description}
                  </p>

                  <div className="mt-7 flex items-center gap-2 text-sm text-neutral-400 group-hover:text-neutral-900 transition">

                    Learn more

                    <ArrowRight
                      size={15}
                      className="group-hover:translate-x-1 transition"
                    />

                  </div>

                </motion.div>
              );
            })}

          </div>
        </div>
      </section>

      {/* =========================================================
          WHY HIREON
      ========================================================= */}
      <section
        id="about"
        className="py-24 sm:py-28 lg:py-36 bg-white border-y border-neutral-200"
      >

        <div className="max-w-7xl mx-auto px-5 lg:px-8">

          <div className="grid lg:grid-cols-2 gap-14 lg:gap-20 items-center">

            {/* LEFT */}
            <div>

              <p className="text-xs sm:text-sm text-violet-600 font-semibold">
                WHY HIREON
              </p>

              <h2 className="mt-4 text-4xl sm:text-5xl font-semibold tracking-tight">
                Recruitment decisions
                <br />

                <span className="text-neutral-400">
                  backed by better insights.
                </span>
              </h2>

              <p className="mt-6 text-neutral-500 leading-relaxed max-w-xl">
                Hireon helps recruitment teams understand how well candidates
                match job requirements while providing useful skill-gap
                feedback throughout the process.
              </p>

              {/* CHECKLIST */}
              <div className="mt-8 space-y-4">

                {[
                  "Understand candidate skill matches",
                  "Identify skill gaps clearly",
                  "Support faster shortlisting",
                  "Keep humans in control of decisions",
                ].map((item) => (
                  <div
                    key={item}
                    className="flex items-center gap-3"
                  >

                    <div className="w-6 h-6 rounded-full bg-violet-100 flex items-center justify-center">
                      <Check
                        size={13}
                        className="text-violet-600"
                      />
                    </div>

                    <span className="text-sm text-neutral-600">
                      {item}
                    </span>

                  </div>
                ))}

              </div>
            </div>

            {/* RIGHT IMAGE */}
            <div className="relative">

              <div className="absolute -inset-8 bg-violet-300/30 blur-[90px] rounded-full" />

              <div className="relative overflow-hidden rounded-3xl border border-neutral-200 shadow-xl shadow-neutral-200/50">

                <img
                  src="https://images.unsplash.com/photo-1556761175-b413da4baf72?auto=format&fit=crop&w=1400&q=85"
                  alt="Recruitment team collaboration"
                  className="w-full aspect-4/3 object-cover"
                />

                <div className="absolute inset-0 bg-linear-to-t from-black/70 via-black/10 to-transparent" />

                <div className="absolute bottom-6 left-6 right-6">

                  <p className="text-[10px] text-white/70 uppercase tracking-wider">
                    Smarter Recruitment
                  </p>

                  <p className="mt-2 text-lg sm:text-xl font-semibold text-white">
                    Better insights for better hiring decisions.
                  </p>

                </div>
              </div>

              {/* FLOATING CARD */}
              <motion.div
                initial={{
                  opacity: 0,
                  y: 20,
                }}
                whileInView={{
                  opacity: 1,
                  y: 0,
                }}
                viewport={{
                  once: true,
                }}
                transition={{
                  delay: 0.3,
                }}
                className="absolute -bottom-5 -left-3 sm:-left-5 bg-white border border-neutral-200 rounded-2xl p-4 sm:p-5 shadow-xl shadow-neutral-200/60"
              >

                <div className="flex items-center gap-3">

                  <div className="w-9 sm:w-10 h-9 sm:h-10 rounded-xl bg-green-100 flex items-center justify-center">

                    <Check
                      size={17}
                      className="text-green-600"
                    />

                  </div>

                  <div>
                    <p className="text-[9px] text-neutral-400">
                      CANDIDATE MATCH
                    </p>

                    <p className="text-sm font-semibold">
                      Strong Match
                    </p>
                  </div>

                </div>

              </motion.div>

            </div>
          </div>
        </div>
      </section>

      {/* =========================================================
          HOW IT WORKS
      ========================================================= */}
      <section
        id="how-it-works"
        className="py-24 sm:py-28 lg:py-36"
      >

        <div className="max-w-7xl mx-auto px-5 lg:px-8">

          {/* HEADING */}
          <div className="text-center max-w-2xl mx-auto">

            <p className="text-xs sm:text-sm text-violet-600 font-semibold">
              HOW IT WORKS
            </p>

            <h2 className="mt-4 text-4xl sm:text-5xl font-semibold tracking-tight">
              From job posting
              <br />

              <span className="text-neutral-400">
                to better hiring.
              </span>
            </h2>

            <p className="mt-5 text-neutral-500 leading-relaxed">
              A simple workflow designed to help recruitment teams move from
              applications to informed decisions.
            </p>

          </div>

          {/* STEPS */}
          <div className="grid md:grid-cols-2 lg:grid-cols-4 mt-14 sm:mt-16">

            {steps.map((step, index) => (
              <motion.div
                key={step.number}
                initial={{
                  opacity: 0,
                  y: 20,
                }}
                whileInView={{
                  opacity: 1,
                  y: 0,
                }}
                viewport={{
                  once: true,
                }}
                transition={{
                  delay: index * 0.1,
                }}
                className="relative p-6 sm:p-7 border-t lg:border-r last:border-r-0 border-neutral-200"
              >

                <span className="text-sm text-violet-600 font-medium">
                  {step.number}
                </span>

                <h3 className="mt-7 text-lg sm:text-xl font-semibold">
                  {step.title}
                </h3>

                <p className="mt-3 text-sm text-neutral-500 leading-relaxed">
                  {step.description}
                </p>

              </motion.div>
            ))}

          </div>

          {/* WORKFLOW IMAGE */}
          <div className="mt-14 sm:mt-16 relative">

            <div className="absolute -inset-10 bg-blue-200/40 blur-[100px] rounded-full" />

            <div className="relative overflow-hidden rounded-3xl border border-neutral-200 shadow-xl shadow-neutral-200/50">

              <img
                src="https://images.unsplash.com/photo-1521737711867-e3b97375f902?auto=format&fit=crop&w=1800&q=85"
                alt="Team collaboration"
                className="w-full h-75 sm:h-95 lg:h-105 object-cover"
              />

              <div className="absolute inset-0 bg-linear-to-t from-black/70 via-black/15 to-transparent" />

              <div className="absolute bottom-6 sm:bottom-8 left-6 sm:left-8">

                <div className="flex items-center gap-2 text-sm text-white/80">

                  <Users size={16} />

                  Connected recruitment workflow
                </div>

                <h3 className="mt-2 text-2xl sm:text-3xl font-semibold text-white">
                  From application to interview.
                </h3>

              </div>

            </div>
          </div>
        </div>
      </section>

      {/* =========================================================
          CTA
      ========================================================= */}
      <section
        id="get-started"
        className="px-5 lg:px-8 pb-10"
      >

        <div className="max-w-7xl mx-auto">

          <div className="relative overflow-hidden rounded-4xl bg-linear-to-br from-violet-600 via-indigo-600 to-blue-600 p-8 sm:p-12 lg:p-20">

            {/* GLOW */}
            <div className="absolute -right-32 -top-32 w-96 h-96 bg-white/20 blur-[100px] rounded-full" />

            <div className="relative max-w-2xl">

              <p className="text-sm text-white/70 font-semibold">
                Hireon
              </p>

              <h2 className="mt-4 text-4xl sm:text-5xl lg:text-6xl font-semibold tracking-tight text-white">
                Make better hiring decisions.
              </h2>

              <p className="mt-5 text-white/75 text-base sm:text-lg">
                Connect job requirements, candidate skills and recruitment
                workflows in one place.
              </p>

              <div className="mt-8 flex flex-wrap gap-3">

                <Link
                  to="/register"
                  className="flex items-center gap-3 bg-white text-neutral-900 px-6 py-3.5 rounded-full font-semibold hover:scale-[1.02] transition"
                >
                  Get Started
                  <ArrowUpRight size={17} />
                </Link>

                <Link
                  to="/login"
                  className="flex items-center gap-3 border border-white/30 bg-white/10 backdrop-blur-md text-white px-6 py-3.5 rounded-full font-semibold hover:bg-white/20 transition"
                >
                  Log in
                </Link>

              </div>

            </div>
          </div>
        </div>
      </section>

      {/* =========================================================
          FOOTER
      ========================================================= */}
      <footer className="py-10">

        <div className="max-w-7xl mx-auto px-5 lg:px-8">

          <div className="flex flex-col md:flex-row md:items-center justify-between gap-5">

            {/* LOGO */}
            <Link
              to="/"
              className="flex items-center gap-3"
            >

              <div className="w-8 h-8 rounded-lg bg-neutral-900 text-white flex items-center justify-center font-bold text-sm">
                H
              </div>

              <span className="font-semibold">
                Hireon
              </span>

            </Link>

            {/* DESCRIPTION */}
            <p className="text-sm text-neutral-400">
              Recruitment & Skill-Gap Matching Platform
            </p>

            {/* COPYRIGHT */}
            <p className="text-sm text-neutral-400">
              © 2026 Hireon
            </p>

          </div>
        </div>
      </footer>

    </div>
  );
}

export default LandingPage;