import { Link } from "react-router-dom";

function Brand() {
  return (
    <Link
      to="/"
      className="inline-flex items-center gap-3 group"
    >
      <div className="w-10 h-10 rounded-full bg-neutral-900 text-white flex items-center justify-center font-bold text-sm shadow-md transition-transform duration-300 group-hover:scale-105">
        H
      </div>

      <div>
        <p className="font-semibold tracking-tight text-lg text-neutral-900 leading-none">
          Hireon
        </p>

        <p className="mt-1 text-[10px] text-neutral-400">
          Recruitment & Skill-Gap Matching
        </p>
      </div>
    </Link>
  );
}

export default Brand;