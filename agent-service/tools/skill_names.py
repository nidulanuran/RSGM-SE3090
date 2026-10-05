"""Utilities for safely matching LLM skill names to canonical database skill names."""

from __future__ import annotations

import re
from collections.abc import Iterable

_WHITESPACE = re.compile(r"\s+")
_PARENTHESES = re.compile(r"\s*\([^)]*\)\s*")


def normalize_skill_name(value: str) -> str:
    """Normalize only for comparison; never use this as the displayed skill name."""
    return _WHITESPACE.sub(" ", value.strip()).casefold()


def skill_aliases(canonical_name: str) -> set[str]:
    """
    Return conservative aliases for one canonical skill.

    Example:
        SQL (Structured Query Language)
        -> {"sql (structured query language)", "sql"}

    The canonical database value remains the value returned to the application.
    """
    aliases = {normalize_skill_name(canonical_name)}

    without_parentheses = _PARENTHESES.sub(" ", canonical_name)
    without_parentheses = _WHITESPACE.sub(" ", without_parentheses).strip()
    if without_parentheses:
        aliases.add(normalize_skill_name(without_parentheses))

    # If the parenthetical part itself is a short acronym/token, accept it too.
    # Example: "JavaScript (JS)" -> "js".
    match = re.fullmatch(r"\s*(.*?)\s*\((.*?)\)\s*", canonical_name)
    if match:
        parenthetical = match.group(2).strip()
        if parenthetical and len(parenthetical) <= 12 and " " not in parenthetical:
            aliases.add(normalize_skill_name(parenthetical))

    return {alias for alias in aliases if alias}


def build_skill_alias_index(canonical_names: Iterable[str]) -> dict[str, set[str]]:
    """Map every accepted alias to the canonical skill name(s) it could mean."""
    index: dict[str, set[str]] = {}
    for canonical in canonical_names:
        for alias in skill_aliases(canonical):
            index.setdefault(alias, set()).add(canonical)
    return index


def resolve_skill_name(name: str, alias_index: dict[str, set[str]]) -> str | None:
    """
    Resolve a model-produced skill to exactly one canonical name.

    Ambiguous aliases intentionally return None so deterministic validation fails
    instead of guessing.
    """
    candidates = alias_index.get(normalize_skill_name(name), set())
    if len(candidates) != 1:
        return None
    return next(iter(candidates))


def canonicalize_known_skills(names: Iterable[str], canonical_names: Iterable[str]) -> list[str]:
    """
    Convert known aliases to exact canonical DB names while preserving unknown
    values so the deterministic validator can reject unsupported claims.
    """
    canonical_list = list(canonical_names)
    alias_index = build_skill_alias_index(canonical_list)

    result: list[str] = []
    seen: set[str] = set()

    for name in names:
        resolved = resolve_skill_name(name, alias_index)
        value = resolved if resolved is not None else name.strip()
        key = normalize_skill_name(value)
        if key and key not in seen:
            result.append(value)
            seen.add(key)

    return result
