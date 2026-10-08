import { motion } from "framer-motion";
import {
  ArrowLeft,
  BrainCircuit,
  Check,
  Sparkles,
} from "lucide-react";
import { Link } from "react-router-dom";

import Brand from "../common/Brand";

function AuthShell({
  badge,
  title,
  subtitle,
  children,
}) {
  return (
    <div className="relative min-h-screen bg-[#FAFAFA] text-neutral-900 overflow-hidden">

      {/* ================= BACKGROUND GLOWS ================= */}

      <div className="absolute inset-0 pointer-events-none overflow-hidden">
        <div className="absolute -top-32 left-[20%] w-125 h-125 bg-violet-300/35 blur-[140px] rounded-full" />

        <div className="absolute top-[40%] -right-40 w-112.5 h-112.5 bg-blue-200/50 blur-[140px] rounded-full" />

        <div className="absolute -bottom-45 left-[35%] w-105 h-105 bg-cyan-100/50 blur-[130px] rounded-full" />
      </div>

      {/* ================= GRID ================= */}

      <div
        className="absolute inset-0 opacity-[0.035] pointer-events-none"
        style={{
          backgroundImage:
            "linear-gradient(rgba(0,0,0,.7) 1px, transparent 1px), linear-gradient(90deg, rgba(0,0,0,.7) 1px, transparent 1px)",
          backgroundSize: "70px 70px",
        }}
      />

      {/* ================= TOP LOGO ================= */}

      <header className="relative z-20 px-5 sm:px-8 py-6">
        <div className="max-w-7xl mx-auto flex items-center justify-between">

          <Brand />

          <Link
            to="/"
            className="
              flex items-center gap-2
              px-4 py-2
              rounded-full
              border border-white/60
              bg-white/40
              backdrop-blur-xl
              text-sm text-neutral-600
              hover:bg-white
              hover:text-neutral-900
              transition
            "
          >
            <ArrowLeft size={15} />

            Back to home
          </Link>
        </div>
      </header>

      {/* ================= CONTENT ================= */}

      <main className="relative z-10 px-5 sm:px-8 pb-10">
        <div
          className="
            max-w-6xl mx-auto
            min-h-[calc(100vh-110px)]
            flex items-center
          "
        >
          <div
            className="
              w-full
              grid
              lg:grid-cols-[0.95fr_1.05fr]
              gap-10
              lg:gap-16
              items-center
            "
          >

            {/* ================= LEFT ================= */}

            <motion.div
              initial={{
                opacity: 0,
                x: -30,
              }}
              animate={{
                opacity: 1,
                x: 0,
              }}
              transition={{
                duration: 0.7,
              }}
              className="hidden lg:block"
            >
              <div
                className="
                  inline-flex
                  items-center
                  gap-2
                  px-3.5
                  py-2
                  rounded-full
                  border
                  border-neutral-200
                  bg-white/70
                  backdrop-blur-xl
                  text-xs
                  text-neutral-600
                  shadow-sm
                "
              >
                <Sparkles
                  size={14}
                  className="text-violet-500"
                />

                Recruitment & Skill-Gap Matching
              </div>

              <h1
                className="
                  mt-8
                  text-5xl
                  xl:text-6xl
                  font-semibold
                  tracking-tighter
                  leading-none
                "
              >
                Hire smarter.
                <br />

                <span
                  className="
                    text-transparent
                    bg-clip-text
                    bg-linear-to-r
                    from-violet-600
                    via-blue-500
                    to-cyan-500
                  "
                >
                  Match better.
                </span>
              </h1>

              <p
                className="
                  mt-6
                  max-w-lg
                  text-neutral-500
                  leading-relaxed
                "
              >
                Connect candidate skills with job requirements
                and make recruitment decisions using meaningful
                skill insights.
              </p>

              <div className="mt-10 space-y-4">

                <Feature
                  text="AI-assisted skill matching"
                />

                <Feature
                  text="Transparent skill-gap insights"
                />

                <Feature
                  text="Human-controlled hiring decisions"
                />

              </div>

              {/* SMALL CARD */}

              <div
                className="
                  mt-12
                  max-w-md
                  p-5
                  rounded-2xl
                  border
                  border-white/60
                  bg-white/45
                  backdrop-blur-2xl
                  shadow-xl
                  shadow-neutral-200/30
                "
              >
                <div className="flex items-center gap-4">

                  <div
                    className="
                      w-11 h-11
                      rounded-2xl
                      bg-violet-100
                      flex
                      items-center
                      justify-center
                    "
                  >
                    <BrainCircuit
                      size={21}
                      className="text-violet-600"
                    />
                  </div>

                  <div>
                    <p className="text-xs text-neutral-400">
                      HIREON PLATFORM
                    </p>

                    <p className="mt-1 text-sm font-semibold">
                      Smarter recruitment starts here.
                    </p>
                  </div>

                </div>
              </div>
            </motion.div>

            {/* ================= FORM CARD ================= */}

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
                delay: 0.1,
              }}
            >
              <div
                className="
                  relative
                  overflow-hidden
                  rounded-[28px]
                  border
                  border-white/70
                  bg-white/75
                  backdrop-blur-3xl
                  shadow-2xl
                  shadow-neutral-300/30
                "
              >

                {/* LIQUID GLASS HIGHLIGHT */}

                <div
                  className="
                    absolute
                    inset-0
                    bg-linear-to-b
                    from-white/80
                    via-white/10
                    to-transparent
                    pointer-events-none
                  "
                />

                <div
                  className="
                    relative
                    p-6
                    sm:p-9
                    lg:p-10
                  "
                >

                  <div
                    className="
                      inline-flex
                      items-center
                      gap-2
                      px-3
                      py-1.5
                      rounded-full
                      bg-violet-50
                      text-violet-600
                      text-[11px]
                      font-semibold
                    "
                  >
                    <Sparkles size={12} />

                    {badge}
                  </div>

                  <h2
                    className="
                      mt-5
                      text-3xl
                      sm:text-4xl
                      font-semibold
                      tracking-tight
                    "
                  >
                    {title}
                  </h2>

                  <p
                    className="
                      mt-3
                      text-sm
                      sm:text-base
                      text-neutral-500
                      leading-relaxed
                    "
                  >
                    {subtitle}
                  </p>

                  <div className="mt-8">
                    {children}
                  </div>

                </div>
              </div>
            </motion.div>

          </div>
        </div>
      </main>
    </div>
  );
}

function Feature({ text }) {
  return (
    <div className="flex items-center gap-3">

      <div
        className="
          w-6 h-6
          rounded-full
          bg-violet-100
          flex
          items-center
          justify-center
        "
      >
        <Check
          size={13}
          className="text-violet-600"
        />
      </div>

      <span className="text-sm text-neutral-600">
        {text}
      </span>

    </div>
  );
}

export default AuthShell;