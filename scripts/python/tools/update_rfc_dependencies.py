#!/usr/bin/env python3
"""Update RFC dependency map using Notion export and architecture docs."""
from __future__ import annotations

import json
import re
from collections import OrderedDict, defaultdict
from pathlib import Path
from typing import Dict, List

JSON_PATH = Path("docs/status/rfc-dependencies.json")
ARCH_DIR = Path("docs/game-rfcs")


def load_dependency_json() -> Dict[str, any]:
    if not JSON_PATH.exists():
        return {"architecture": OrderedDict(), "dependencies": OrderedDict()}
    with JSON_PATH.open("r", encoding="utf-8") as fh:
        data = json.load(fh, object_pairs_hook=OrderedDict)
    data.setdefault("architecture", OrderedDict())
    data.setdefault("dependencies", OrderedDict())
    return data


def parse_architecture_dependencies() -> Dict[str, List[str]]:
    dep_map: Dict[str, List[str]] = {}
    for md_path in sorted(ARCH_DIR.glob("RFC-*-*.md")):
        match = re.match(r"RFC-(\d{3})-", md_path.name)
        if not match:
            continue
        series = match.group(1)
        arch_id = f"ARCH-RFC-{series}"
        text = md_path.read_text(encoding="utf-8")
        deps: List[str] = []
        for line in text.splitlines():
            if "Depends On" not in line:
                continue
            for dep_match in re.findall(r"RFC[-\s]?(\d{3})", line, flags=re.IGNORECASE):
                dep = f"ARCH-RFC-{int(dep_match):03d}"
                if dep != arch_id and dep not in deps:
                    deps.append(dep)
        dep_map[arch_id] = deps
    return dep_map


def collect_arch_transitive(arch_id: str, arch_deps: Dict[str, List[str]], cache: Dict[str, List[str]]) -> List[str]:
    if arch_id in cache:
        return cache[arch_id]
    seen: List[str] = []
    for dep in arch_deps.get(arch_id, []):
        if dep not in seen:
            seen.append(dep)
        for sub in collect_arch_transitive(dep, arch_deps, cache):
            if sub not in seen:
                seen.append(sub)
    cache[arch_id] = seen
    return seen


def update_dependency_data():
    data = load_dependency_json()
    architecture = data["architecture"]
    dependencies = data["dependencies"]

    arch_dep_map = parse_architecture_dependencies()
    transitive_cache: Dict[str, List[str]] = {}
    for arch_id, deps in arch_dep_map.items():
        entry = architecture.setdefault(arch_id, OrderedDict())
        entry.setdefault("title", arch_id)
        entry["depends_on"] = deps
        collect_arch_transitive(arch_id, arch_dep_map, transitive_cache)

    series_map: Dict[str, List[tuple[int, str]]] = defaultdict(list)
    for ident in list(dependencies.keys()):
        match = re.match(r"GAME-RFC-(\d{3})-(\d{2})", ident)
        if not match:
            continue
        series, micro = match.groups()
        series_map[series].append((int(micro), ident))

    for series, items in series_map.items():
        items.sort()
        arch_id = f"ARCH-RFC-{series}"
        arch_transitive = collect_arch_transitive(arch_id, arch_dep_map, transitive_cache)
        for idx, (_, ident) in enumerate(items):
            current = list(dependencies.get(ident, []))
            ordered: List[str] = []

            def add_unique(token: str):
                if token and token not in ordered:
                    ordered.append(token)

            add_unique(arch_id)
            for dep in arch_transitive:
                add_unique(dep)
            for existing in current:
                add_unique(existing)
            if idx > 0:
                add_unique(items[idx - 1][1])
            dependencies[ident] = ordered

    with JSON_PATH.open("w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=2)


if __name__ == "__main__":
    update_dependency_data()
