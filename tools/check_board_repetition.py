#!/usr/bin/env python3
"""Validate ADR-0011 board repetition fixtures and document integration.

This is a specification checker, not the product Rules Kernel. M1 must port the
same fixtures to the shared kernel and golden replay suite.
"""
from __future__ import annotations

from collections import deque
from dataclasses import dataclass
import json
from pathlib import Path
from typing import Any, Iterable

ROOT = Path(__file__).resolve().parents[1]
FIXTURES_PATH = ROOT / "game_data" / "fixtures" / "board_repetition_fixtures.json"
ADR_PATH = (
    ROOT
    / "docs"
    / "60_Decisions"
    / "ADRs"
    / "ADR-0011 Battle-Local Stone Topology Repetition Ban.md"
)
RULES_PATH = ROOT / "docs" / "20_Design" / "Rules Canon.md"
COMBAT_PATH = ROOT / "docs" / "20_Design" / "Combat Resolution Order.md"
FEAT009_PATH = (
    ROOT
    / "docs"
    / "20_Design"
    / "Feature Specs"
    / "FEAT-009 Enemy Action Planning and Placement.md"
)

BOARD_SIZE = 7
VALID_SYMBOLS = {".", "B", "W", "K", "Q"}
EXPECTED_FIXTURE_IDS = {f"KO-{n:02d}" for n in range(1, 8)}
REPETITION_REASON = "stone_topology_repetition"


@dataclass(frozen=True)
class PlacementResult:
    legal: bool
    reason: str
    board: tuple[str, ...]
    captured: tuple[tuple[int, int], ...]


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def validate_rows(rows: Iterable[str], label: str) -> tuple[str, ...]:
    normalized = tuple(rows)
    if len(normalized) != BOARD_SIZE:
        raise ValueError(f"{label}: expected {BOARD_SIZE} rows, got {len(normalized)}")
    for index, row in enumerate(normalized):
        if len(row) != BOARD_SIZE:
            raise ValueError(
                f"{label}: row {index} expected length {BOARD_SIZE}, got {len(row)}"
            )
        unknown = set(row) - VALID_SYMBOLS
        if unknown:
            raise ValueError(f"{label}: row {index} contains unknown symbols {sorted(unknown)}")
    return normalized


def to_grid(rows: tuple[str, ...]) -> list[list[str]]:
    return [list(row) for row in rows]


def from_grid(grid: list[list[str]]) -> tuple[str, ...]:
    return tuple("".join(row) for row in grid)


def row_index(y: int) -> int:
    if not 1 <= y <= BOARD_SIZE:
        raise ValueError(f"y out of range: {y}")
    return BOARD_SIZE - y


def col_index(x: int) -> int:
    if not 1 <= x <= BOARD_SIZE:
        raise ValueError(f"x out of range: {x}")
    return x - 1


def symbol_at(grid: list[list[str]], point: tuple[int, int]) -> str:
    x, y = point
    return grid[row_index(y)][col_index(x)]


def set_symbol(grid: list[list[str]], point: tuple[int, int], symbol: str) -> None:
    x, y = point
    grid[row_index(y)][col_index(x)] = symbol


def neighbors(point: tuple[int, int]) -> list[tuple[int, int]]:
    x, y = point
    result: list[tuple[int, int]] = []
    for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
        if 1 <= nx <= BOARD_SIZE and 1 <= ny <= BOARD_SIZE:
            result.append((nx, ny))
    return result


def color(symbol: str) -> str | None:
    if symbol in {"B", "K"}:
        return "black"
    if symbol in {"W", "Q"}:
        return "white"
    return None


def group_at(grid: list[list[str]], start: tuple[int, int]) -> set[tuple[int, int]]:
    group_color = color(symbol_at(grid, start))
    if group_color is None:
        return set()
    found: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque([start])
    while queue:
        point = queue.popleft()
        if point in found or color(symbol_at(grid, point)) != group_color:
            continue
        found.add(point)
        for adjacent in neighbors(point):
            if adjacent not in found and color(symbol_at(grid, adjacent)) == group_color:
                queue.append(adjacent)
    return found


def liberties(grid: list[list[str]], group: set[tuple[int, int]]) -> set[tuple[int, int]]:
    result: set[tuple[int, int]] = set()
    for point in group:
        for adjacent in neighbors(point):
            if symbol_at(grid, adjacent) == ".":
                result.add(adjacent)
    return result


def topology_key(rows: tuple[str, ...]) -> str:
    """Canonical stone topology in y-ascending, then x-ascending point order."""
    return "/".join(rows[row_index(y)] for y in range(1, BOARD_SIZE + 1))


def canonical_points(points: Iterable[tuple[int, int]]) -> tuple[tuple[int, int], ...]:
    return tuple(sorted(points, key=lambda point: (point[1], point[0])))


def simulate_placement(
    rows: tuple[str, ...],
    actor: str,
    point: tuple[int, int],
    history_keys: set[str],
) -> PlacementResult:
    if actor not in {"black", "white"}:
        raise ValueError(f"unknown actor: {actor}")
    grid = to_grid(rows)
    if symbol_at(grid, point) != ".":
        return PlacementResult(False, "occupied", rows, ())

    placed_symbol = "B" if actor == "black" else "W"
    opponent = "white" if actor == "black" else "black"
    set_symbol(grid, point, placed_symbol)

    adjacent_opponent_groups: list[set[tuple[int, int]]] = []
    seen_opponent_stones: set[tuple[int, int]] = set()
    for adjacent in neighbors(point):
        if color(symbol_at(grid, adjacent)) != opponent or adjacent in seen_opponent_stones:
            continue
        group = group_at(grid, adjacent)
        seen_opponent_stones.update(group)
        if not liberties(grid, group):
            adjacent_opponent_groups.append(group)

    captured_points: set[tuple[int, int]] = set().union(*adjacent_opponent_groups) if adjacent_opponent_groups else set()
    for captured in captured_points:
        set_symbol(grid, captured, ".")

    own_group = group_at(grid, point)
    if not liberties(grid, own_group):
        return PlacementResult(False, "suicide", rows, canonical_points(captured_points))

    result_rows = from_grid(grid)
    if topology_key(result_rows) in history_keys:
        return PlacementResult(
            False,
            REPETITION_REASON,
            result_rows,
            canonical_points(captured_points),
        )

    return PlacementResult(True, "legal", result_rows, canonical_points(captured_points))


def point_tuple(raw: Any, label: str) -> tuple[int, int]:
    if not isinstance(raw, list) or len(raw) != 2 or not all(isinstance(v, int) for v in raw):
        raise ValueError(f"{label}: point must be [x, y]")
    point = (raw[0], raw[1])
    col_index(point[0])
    row_index(point[1])
    return point


def compare_result(
    fixture_id: str,
    result: PlacementResult,
    expected: dict[str, Any],
) -> list[str]:
    errors: list[str] = []
    if result.legal != expected.get("legal"):
        errors.append(f"{fixture_id}: legal expected {expected.get('legal')}, got {result.legal}")
    if result.reason != expected.get("reason"):
        errors.append(f"{fixture_id}: reason expected {expected.get('reason')}, got {result.reason}")
    expected_captured = tuple(
        point_tuple(raw, f"{fixture_id}/expected/captured")
        for raw in expected.get("captured", [])
    )
    if result.captured != canonical_points(expected_captured):
        errors.append(
            f"{fixture_id}: captured expected {canonical_points(expected_captured)}, "
            f"got {result.captured}"
        )
    if "result_board" in expected:
        expected_board = validate_rows(expected["result_board"], f"{fixture_id}/expected/result_board")
        if result.board != expected_board:
            errors.append(f"{fixture_id}: hypothetical result board differs from expected")
    return errors


def validate_single_fixture(fixture: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    fixture_id = fixture.get("id", "<missing-id>")
    try:
        history = [
            validate_rows(rows, f"{fixture_id}/history[{index}]")
            for index, rows in enumerate(fixture.get("history_boards", []))
        ]
        current = validate_rows(fixture.get("current_board", []), f"{fixture_id}/current")
        if not history:
            return [f"{fixture_id}: history_boards must not be empty"]
        if history[-1] != current:
            errors.append(f"{fixture_id}: current_board must equal last committed history board")
        history_keys = {topology_key(rows) for rows in history}
        actor = fixture.get("actor")

        if "candidate_points" in fixture:
            rejected: list[dict[str, Any]] = []
            chosen: tuple[int, int] | None = None
            chosen_result: PlacementResult | None = None
            for index, candidate in enumerate(fixture["candidate_points"]):
                point = point_tuple(candidate.get("point"), f"{fixture_id}/candidate[{index}]")
                result = simulate_placement(current, actor, point, history_keys)
                if result.legal:
                    chosen = point
                    chosen_result = result
                    break
                rejected.append({"point": list(point), "reason": result.reason})

            expected = fixture.get("expected", {})
            expected_chosen = point_tuple(expected.get("chosen_point"), f"{fixture_id}/expected/chosen")
            if chosen != expected_chosen:
                errors.append(f"{fixture_id}: chosen point expected {expected_chosen}, got {chosen}")
            if rejected != expected.get("rejected"):
                errors.append(
                    f"{fixture_id}: rejected candidates expected {expected.get('rejected')}, got {rejected}"
                )
            if chosen_result is None:
                errors.append(f"{fixture_id}: no legal candidate found")
            elif "result_board" in expected:
                expected_board = validate_rows(
                    expected["result_board"], f"{fixture_id}/expected/result_board"
                )
                if chosen_result.board != expected_board:
                    errors.append(f"{fixture_id}: chosen result board differs from expected")
            return errors

        point = point_tuple(fixture.get("point"), f"{fixture_id}/point")
        result = simulate_placement(current, actor, point, history_keys)
        errors.extend(compare_result(fixture_id, result, fixture.get("expected", {})))
    except (KeyError, TypeError, ValueError) as exc:
        errors.append(f"{fixture_id}: {exc}")
    return errors


def validate_documents() -> list[str]:
    errors: list[str] = []
    required_files = [ADR_PATH, RULES_PATH, COMBAT_PATH, FEAT009_PATH, FIXTURES_PATH]
    for path in required_files:
        if not path.exists():
            errors.append(f"missing required file {path.relative_to(ROOT)}")
    if errors:
        return errors

    adr = ADR_PATH.read_text(encoding="utf-8")
    rules = RULES_PATH.read_text(encoding="utf-8")
    combat = COMBAT_PATH.read_text(encoding="utf-8")
    feat009 = FEAT009_PATH.read_text(encoding="utf-8")

    if "status: accepted" not in adr:
        errors.append("ADR-0011 must be accepted")
    for token in (
        "StoneTopologyKey",
        "battle_local_stone_topology_superko",
        "stone_topology_repetition",
        "特殊石",
        "非石",
    ):
        if token not in adr:
            errors.append(f"ADR-0011 missing required token {token!r}")
    if "## 盤面反復禁止" not in rules:
        errors.append("Rules Canon missing board repetition section")
    if "[[ADR-0011 Battle-Local Stone Topology Repetition Ban]]" not in rules:
        errors.append("Rules Canon must link ADR-0011")
    if "stone_topology_repetition" not in combat:
        errors.append("Combat Resolution Order missing repetition reason code")
    if "ADR-0011 Battle-Local Stone Topology Repetition Ban" not in feat009:
        errors.append("FEAT-009 must reference accepted ADR-0011")
    return errors


def main() -> int:
    errors = validate_documents()
    try:
        fixtures = load_json(FIXTURES_PATH)
    except Exception as exc:  # pragma: no cover - explicit CLI diagnostic
        errors.append(f"cannot load fixtures: {exc}")
        fixtures = []

    if not isinstance(fixtures, list):
        errors.append("board repetition fixtures must contain a list")
        fixtures = []

    fixture_ids: set[str] = set()
    for fixture in fixtures:
        if not isinstance(fixture, dict):
            errors.append("each board repetition fixture must be an object")
            continue
        fixture_id = fixture.get("id", "<missing-id>")
        if fixture_id in fixture_ids:
            errors.append(f"duplicate fixture ID {fixture_id}")
        fixture_ids.add(fixture_id)
        errors.extend(validate_single_fixture(fixture))

    missing = EXPECTED_FIXTURE_IDS - fixture_ids
    extra = fixture_ids - EXPECTED_FIXTURE_IDS
    if missing:
        errors.append(f"missing board repetition fixtures {sorted(missing)}")
    if extra:
        errors.append(f"unexpected board repetition fixtures {sorted(extra)}")

    if errors:
        print("Board repetition checks failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    print(
        f"Board repetition checks passed. {len(fixtures)} deterministic fixtures, "
        "ADR-0011 and rule references verified."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
