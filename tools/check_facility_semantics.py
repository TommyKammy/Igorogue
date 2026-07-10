#!/usr/bin/env python3
"""Validate ADR-0012 facility intersection semantics and deterministic fixtures.

This is a specification checker, not the product Rules Kernel. M1 must port the
same fixtures to the shared kernel and golden replay suite.
"""
from __future__ import annotations

from collections import deque
from dataclasses import dataclass, replace
import json
import math
from pathlib import Path
from typing import Any, Iterable

ROOT = Path(__file__).resolve().parents[1]
FIXTURES_PATH = ROOT / "game_data" / "fixtures" / "facility_intersection_fixtures.json"
SYSTEM_PATH = ROOT / "game_data" / "balance" / "system.json"
ADR_PATH = ROOT / "docs" / "60_Decisions" / "ADRs" / "ADR-0012 Facility Sites Are Empty Intersections.md"
RULES_PATH = ROOT / "docs" / "20_Design" / "Rules Canon.md"
FEAT001_PATH = ROOT / "docs" / "20_Design" / "Feature Specs" / "FEAT-001 Territory and Facilities.md"
COMBAT_PATH = ROOT / "docs" / "20_Design" / "Combat Resolution Order.md"
FEAT009_PATH = ROOT / "docs" / "20_Design" / "Feature Specs" / "FEAT-009 Enemy Action Planning and Placement.md"
FIXTURE_DOC_PATH = ROOT / "docs" / "50_Validation" / "Spec Fixtures" / "ADR-0012 Facility Intersection Fixtures.md"

BOARD_SIZE = 7
VALID_SYMBOLS = {".", "B", "W", "K", "Q"}
EXPECTED_FIXTURE_IDS = {f"FAC-{n:02d}" for n in range(1, 10)}


@dataclass(frozen=True)
class Facility:
    instance_id: str
    facility_id: str
    owner: str
    point: tuple[int, int]
    build_sequence: int
    explicit_disabled: bool = False


@dataclass(frozen=True)
class Territory:
    owner: str
    points: frozenset[tuple[int, int]]
    anchor: tuple[int, int]

    @property
    def size(self) -> int:
        return len(self.points)


@dataclass(frozen=True)
class ActionResult:
    legal: bool
    reason: str
    board: tuple[str, ...]
    facilities: tuple[Facility, ...]
    events: tuple[str, ...]


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def validate_rows(rows: Iterable[str], label: str) -> tuple[str, ...]:
    normalized = tuple(rows)
    if len(normalized) != BOARD_SIZE:
        raise ValueError(f"{label}: expected {BOARD_SIZE} rows, got {len(normalized)}")
    for index, row in enumerate(normalized):
        if len(row) != BOARD_SIZE:
            raise ValueError(f"{label}: row {index} expected length {BOARD_SIZE}, got {len(row)}")
        unknown = set(row) - VALID_SYMBOLS
        if unknown:
            raise ValueError(f"{label}: row {index} contains unknown symbols {sorted(unknown)}")
    return normalized


def row_index(y: int) -> int:
    if not 1 <= y <= BOARD_SIZE:
        raise ValueError(f"y out of range: {y}")
    return BOARD_SIZE - y


def col_index(x: int) -> int:
    if not 1 <= x <= BOARD_SIZE:
        raise ValueError(f"x out of range: {x}")
    return x - 1


def point_tuple(raw: Any, label: str) -> tuple[int, int]:
    if not isinstance(raw, list) or len(raw) != 2 or not all(isinstance(v, int) for v in raw):
        raise ValueError(f"{label}: point must be [x, y]")
    point = (raw[0], raw[1])
    col_index(point[0])
    row_index(point[1])
    return point


def canonical_points(points: Iterable[tuple[int, int]]) -> tuple[tuple[int, int], ...]:
    return tuple(sorted(points, key=lambda point: (point[1], point[0])))


def to_grid(rows: tuple[str, ...]) -> list[list[str]]:
    return [list(row) for row in rows]


def from_grid(grid: list[list[str]]) -> tuple[str, ...]:
    return tuple("".join(row) for row in grid)


def symbol_at_rows(rows: tuple[str, ...], point: tuple[int, int]) -> str:
    x, y = point
    return rows[row_index(y)][col_index(x)]


def symbol_at_grid(grid: list[list[str]], point: tuple[int, int]) -> str:
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
    group_color = color(symbol_at_grid(grid, start))
    if group_color is None:
        return set()
    found: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque([start])
    while queue:
        point = queue.popleft()
        if point in found or color(symbol_at_grid(grid, point)) != group_color:
            continue
        found.add(point)
        for adjacent in neighbors(point):
            if adjacent not in found and color(symbol_at_grid(grid, adjacent)) == group_color:
                queue.append(adjacent)
    return found


def liberties(grid: list[list[str]], group: set[tuple[int, int]]) -> set[tuple[int, int]]:
    result: set[tuple[int, int]] = set()
    for point in group:
        for adjacent in neighbors(point):
            # Facilities deliberately do not appear in the Stone layer.
            if symbol_at_grid(grid, adjacent) == ".":
                result.add(adjacent)
    return result


def calculate_territories(rows: tuple[str, ...]) -> tuple[Territory, ...]:
    grid = to_grid(rows)
    unvisited = {
        (x, y)
        for y in range(1, BOARD_SIZE + 1)
        for x in range(1, BOARD_SIZE + 1)
        if symbol_at_grid(grid, (x, y)) == "."
    }
    regions: list[Territory] = []
    while unvisited:
        start = canonical_points(unvisited)[0]
        points: set[tuple[int, int]] = set()
        queue: deque[tuple[int, int]] = deque([start])
        adjacent_colors: set[str] = set()
        while queue:
            point = queue.popleft()
            if point in points or point not in unvisited:
                continue
            points.add(point)
            for adjacent in neighbors(point):
                adjacent_color = color(symbol_at_grid(grid, adjacent))
                if adjacent_color is None:
                    if adjacent in unvisited and adjacent not in points:
                        queue.append(adjacent)
                else:
                    adjacent_colors.add(adjacent_color)
        unvisited -= points
        if adjacent_colors == {"black"}:
            owner = "black"
        elif adjacent_colors == {"white"}:
            owner = "white"
        else:
            owner = "neutral"
        regions.append(Territory(owner, frozenset(points), canonical_points(points)[0]))
    return tuple(sorted(regions, key=lambda region: (region.anchor[1], region.anchor[0])))


def territory_at(rows: tuple[str, ...], point: tuple[int, int]) -> Territory | None:
    if symbol_at_rows(rows, point) != ".":
        return None
    for region in calculate_territories(rows):
        if point in region.points:
            return region
    raise AssertionError(f"empty point {point} not found in territory map")


def topology_key(rows: tuple[str, ...]) -> str:
    """Canonical stone topology in y-ascending, then x-ascending point order."""
    return "/".join(rows[row_index(y)] for y in range(1, BOARD_SIZE + 1))


def parse_facilities(raw: Any, label: str) -> tuple[Facility, ...]:
    if not isinstance(raw, list):
        raise ValueError(f"{label}: facilities must be a list")
    result: list[Facility] = []
    ids: set[str] = set()
    points: set[tuple[int, int]] = set()
    for index, item in enumerate(raw):
        if not isinstance(item, dict):
            raise ValueError(f"{label}[{index}]: facility must be object")
        instance_id = item.get("instance_id")
        facility_id = item.get("facility_id")
        owner = item.get("owner")
        point = point_tuple(item.get("point"), f"{label}[{index}]/point")
        build_sequence = item.get("build_sequence")
        if not isinstance(instance_id, str) or not instance_id:
            raise ValueError(f"{label}[{index}]: invalid instance_id")
        if instance_id in ids:
            raise ValueError(f"{label}: duplicate facility instance {instance_id}")
        if point in points:
            raise ValueError(f"{label}: multiple facilities at point {point}")
        if not isinstance(facility_id, str) or not facility_id:
            raise ValueError(f"{label}[{index}]: invalid facility_id")
        if owner not in {"black", "white"}:
            raise ValueError(f"{label}[{index}]: invalid owner {owner!r}")
        if not isinstance(build_sequence, int) or build_sequence < 0:
            raise ValueError(f"{label}[{index}]: invalid build_sequence")
        ids.add(instance_id)
        points.add(point)
        result.append(
            Facility(
                instance_id=instance_id,
                facility_id=facility_id,
                owner=owner,
                point=point,
                build_sequence=build_sequence,
                explicit_disabled=bool(item.get("explicit_disabled", False)),
            )
        )
    return tuple(sorted(result, key=lambda f: (f.point[1], f.point[0], f.instance_id)))


def facility_state(rows: tuple[str, ...], facility: Facility) -> str:
    if symbol_at_rows(rows, facility.point) != ".":
        return "destroyed"
    if facility.explicit_disabled:
        return "disabled:explicit_effect"
    region = territory_at(rows, facility.point)
    if region is not None and region.owner == facility.owner:
        return "active"
    return "disabled:territory_control_lost"


def facility_state_map(rows: tuple[str, ...], facilities: tuple[Facility, ...]) -> dict[str, str]:
    return {facility.instance_id: facility_state(rows, facility) for facility in facilities}


def capacity_for_size(size: int, system: dict[str, Any]) -> int:
    matches = [
        entry["slots"]
        for entry in system.get("facility_capacity", [])
        if entry["min"] <= size <= entry["max"]
    ]
    if len(matches) != 1:
        raise ValueError(f"facility capacity table must have exactly one match for size {size}")
    return min(matches[0], system.get("facility_slot_cap", matches[0]))


def facility_type_limit(facility_id: str, system: dict[str, Any]) -> int:
    limits = system.get("facility_type_limits_per_region")
    if not isinstance(limits, dict):
        raise ValueError("system.json missing facility_type_limits_per_region")
    value = limits.get(facility_id, limits.get("default"))
    if not isinstance(value, int) or value < 1:
        raise ValueError(f"invalid facility type limit for {facility_id}")
    return value


def installed_in_region(facilities: tuple[Facility, ...], region: Territory) -> tuple[Facility, ...]:
    return tuple(facility for facility in facilities if facility.point in region.points)


def territory_summary(
    rows: tuple[str, ...],
    point: tuple[int, int],
    facilities: tuple[Facility, ...],
    system: dict[str, Any],
) -> dict[str, Any]:
    region = territory_at(rows, point)
    if region is None:
        raise ValueError(f"territory query point {point} is not empty")
    installed = installed_in_region(facilities, region)
    capacity = capacity_for_size(region.size, system)
    return {
        "owner": region.owner,
        "size": region.size,
        "basic_income": math.ceil(region.size / system["territory_income_divisor"]),
        "construction_capacity": capacity,
        "installed_count": len(installed),
        "is_over_capacity": len(installed) > capacity,
    }


def simulate_placement(
    rows: tuple[str, ...],
    facilities: tuple[Facility, ...],
    actor: str,
    point: tuple[int, int],
) -> ActionResult:
    if actor not in {"black", "white"}:
        raise ValueError(f"invalid actor {actor}")
    if symbol_at_rows(rows, point) != ".":
        return ActionResult(False, "occupied", rows, facilities, ())

    grid = to_grid(rows)
    set_symbol(grid, point, "B" if actor == "black" else "W")
    opponent = "white" if actor == "black" else "black"
    captured_groups: list[set[tuple[int, int]]] = []
    seen: set[tuple[int, int]] = set()
    for adjacent in neighbors(point):
        if color(symbol_at_grid(grid, adjacent)) != opponent or adjacent in seen:
            continue
        group = group_at(grid, adjacent)
        seen.update(group)
        if not liberties(grid, group):
            captured_groups.append(group)

    captured = set().union(*captured_groups) if captured_groups else set()
    for captured_point in captured:
        set_symbol(grid, captured_point, ".")

    own_group = group_at(grid, point)
    if not liberties(grid, own_group):
        return ActionResult(False, "suicide", rows, facilities, ())

    result_rows = from_grid(grid)
    events: list[str] = [f"StonePlaced:{actor}:{point[0]},{point[1]}"]
    for group in sorted(captured_groups, key=lambda g: (canonical_points(g)[0][1], canonical_points(g)[0][0])):
        anchor = canonical_points(group)[0]
        events.append(f"GroupCaptured:{opponent}:{anchor[0]},{anchor[1]}:{len(group)}")

    destroyed = [facility for facility in facilities if facility.point == point]
    remaining = tuple(facility for facility in facilities if facility.point != point)
    for facility in destroyed:
        events.append(f"FacilityDestroyed:{facility.instance_id}:stone_occupied")

    return ActionResult(True, "legal", result_rows, remaining, tuple(events))


def simulate_build(
    rows: tuple[str, ...],
    facilities: tuple[Facility, ...],
    actor: str,
    point: tuple[int, int],
    facility_id: str,
    system: dict[str, Any],
) -> ActionResult:
    if actor not in {"black", "white"}:
        raise ValueError(f"invalid actor {actor}")
    if symbol_at_rows(rows, point) != ".":
        return ActionResult(False, "facility_target_has_stone", rows, facilities, ())
    if any(facility.point == point for facility in facilities):
        return ActionResult(False, "facility_target_occupied", rows, facilities, ())
    region = territory_at(rows, point)
    if region is None or region.owner != actor:
        return ActionResult(False, "facility_target_not_owned_territory", rows, facilities, ())
    installed = installed_in_region(facilities, region)
    capacity = capacity_for_size(region.size, system)
    if len(installed) >= capacity:
        return ActionResult(False, "facility_capacity_full", rows, facilities, ())
    same_type = sum(1 for facility in installed if facility.facility_id == facility_id)
    if same_type >= facility_type_limit(facility_id, system):
        return ActionResult(False, "facility_type_limit_reached", rows, facilities, ())

    next_sequence = max((facility.build_sequence for facility in facilities), default=0) + 1
    new_facility = Facility("generated_facility", facility_id, actor, point, next_sequence)
    result = tuple(sorted((*facilities, new_facility), key=lambda f: (f.point[1], f.point[0], f.instance_id)))
    events = (
        f"FacilityBuilt:{new_facility.instance_id}:{facility_id}:{point[0]},{point[1]}",
        f"FacilityActivated:{new_facility.instance_id}:built_in_controlled_territory",
    )
    return ActionResult(True, "legal", rows, result, events)


def compare_mapping(fixture_id: str, label: str, actual: Any, expected: Any) -> list[str]:
    if actual != expected:
        return [f"{fixture_id}: {label} expected {expected!r}, got {actual!r}"]
    return []


def compare_common_expected(
    fixture_id: str,
    expected: dict[str, Any],
    rows: tuple[str, ...],
    facilities: tuple[Facility, ...],
    system: dict[str, Any],
    query_point: tuple[int, int] | None = None,
) -> list[str]:
    errors: list[str] = []
    if "facility_states" in expected:
        actual = facility_state_map(rows, facilities)
        errors += compare_mapping(fixture_id, "facility_states", actual, expected["facility_states"])
    if "facility_owners" in expected:
        actual = {facility.instance_id: facility.owner for facility in facilities}
        errors += compare_mapping(fixture_id, "facility_owners", actual, expected["facility_owners"])
    if "territory" in expected:
        if query_point is None:
            raise ValueError(f"{fixture_id}: territory expected but no query point supplied")
        actual = territory_summary(rows, query_point, facilities, system)
        filtered = {key: actual[key] for key in expected["territory"]}
        errors += compare_mapping(fixture_id, "territory", filtered, expected["territory"])
    return errors


def validate_fixture(fixture: dict[str, Any], system: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    fixture_id = fixture.get("id", "<missing-id>")
    try:
        operation = fixture.get("operation")
        rows = validate_rows(fixture.get("board", []), f"{fixture_id}/board")
        facilities = parse_facilities(fixture.get("facilities", []), f"{fixture_id}/facilities")
        for facility in facilities:
            if symbol_at_rows(rows, facility.point) != ".":
                errors.append(f"{fixture_id}: facility {facility.instance_id} coexists with stone")

        expected = fixture.get("expected", {})
        if operation == "inspect":
            queries = fixture.get("queries", {})
            if "group_point" in queries:
                point = point_tuple(queries["group_point"], f"{fixture_id}/queries/group_point")
                grid = to_grid(rows)
                group = group_at(grid, point)
                actual_liberties = canonical_points(liberties(grid, group))
                expected_liberties = tuple(
                    point_tuple(raw, f"{fixture_id}/expected/group_liberties")
                    for raw in expected.get("group_liberties", [])
                )
                errors += compare_mapping(
                    fixture_id,
                    "group_liberties",
                    actual_liberties,
                    canonical_points(expected_liberties),
                )
            territory_point = (
                point_tuple(queries["territory_point"], f"{fixture_id}/queries/territory_point")
                if "territory_point" in queries
                else None
            )
            errors += compare_common_expected(
                fixture_id, expected, rows, facilities, system, territory_point
            )
            return errors

        if operation == "place":
            point = point_tuple(fixture.get("point"), f"{fixture_id}/point")
            result = simulate_placement(rows, facilities, fixture.get("actor"), point)
            errors += compare_mapping(fixture_id, "legal", result.legal, expected.get("legal"))
            errors += compare_mapping(fixture_id, "reason", result.reason, expected.get("reason"))
            errors += compare_mapping(fixture_id, "events", list(result.events), expected.get("events", []))
            destroyed = sorted(set(f.instance_id for f in facilities) - set(f.instance_id for f in result.facilities))
            remaining = sorted(f.instance_id for f in result.facilities)
            errors += compare_mapping(
                fixture_id, "destroyed_facilities", destroyed, sorted(expected.get("destroyed_facilities", []))
            )
            errors += compare_mapping(
                fixture_id, "remaining_facilities", remaining, sorted(expected.get("remaining_facilities", []))
            )
            if "result_board" in expected:
                expected_board = validate_rows(expected["result_board"], f"{fixture_id}/expected/result_board")
                errors += compare_mapping(fixture_id, "result_board", result.board, expected_board)
            errors += compare_common_expected(
                fixture_id, expected, result.board, result.facilities, system
            )
            return errors

        if operation == "build":
            point = point_tuple(fixture.get("point"), f"{fixture_id}/point")
            before_key = topology_key(rows)
            result = simulate_build(
                rows,
                facilities,
                fixture.get("actor"),
                point,
                fixture.get("facility_id"),
                system,
            )
            errors += compare_mapping(fixture_id, "legal", result.legal, expected.get("legal"))
            errors += compare_mapping(fixture_id, "reason", result.reason, expected.get("reason"))
            errors += compare_mapping(fixture_id, "events", list(result.events), expected.get("events", []))
            remaining = sorted(f.instance_id for f in result.facilities)
            errors += compare_mapping(
                fixture_id, "remaining_facilities", remaining, sorted(expected.get("remaining_facilities", []))
            )
            if expected.get("topology_unchanged") is True and topology_key(result.board) != before_key:
                errors.append(f"{fixture_id}: build changed StoneTopologyKey")
            errors += compare_common_expected(
                fixture_id, expected, result.board, result.facilities, system, point
            )
            return errors

        if operation == "transition_sequence":
            current_rows = rows
            current_facilities = facilities
            current_states = facility_state_map(current_rows, current_facilities)
            events: list[str] = []
            for index, next_raw in enumerate(fixture.get("next_boards", [])):
                next_rows = validate_rows(next_raw, f"{fixture_id}/next_boards[{index}]")
                survivors: list[Facility] = []
                for facility in current_facilities:
                    if symbol_at_rows(next_rows, facility.point) != ".":
                        events.append(f"FacilityDestroyed:{facility.instance_id}:stone_occupied")
                    else:
                        survivors.append(facility)
                current_facilities = tuple(survivors)
                next_states = facility_state_map(next_rows, current_facilities)
                for facility in current_facilities:
                    old = current_states.get(facility.instance_id)
                    new = next_states[facility.instance_id]
                    if old == "active" and new.startswith("disabled"):
                        reason = new.split(":", 1)[1]
                        events.append(f"FacilityDisabled:{facility.instance_id}:{reason}")
                    elif old is not None and old.startswith("disabled") and new == "active":
                        events.append(
                            f"FacilityActivated:{facility.instance_id}:territory_control_restored"
                        )
                current_rows = next_rows
                current_states = next_states
            errors += compare_mapping(fixture_id, "events", events, expected.get("events", []))
            remaining = sorted(f.instance_id for f in current_facilities)
            errors += compare_mapping(
                fixture_id, "remaining_facilities", remaining, sorted(expected.get("remaining_facilities", []))
            )
            errors += compare_common_expected(
                fixture_id, expected, current_rows, current_facilities, system
            )
            if "owner" in expected:
                owners = {f.owner for f in current_facilities}
                actual_owner = next(iter(owners)) if len(owners) == 1 else None
                errors += compare_mapping(fixture_id, "owner", actual_owner, expected["owner"])
            if "build_sequence" in expected:
                sequences = {f.build_sequence for f in current_facilities}
                actual_sequence = next(iter(sequences)) if len(sequences) == 1 else None
                errors += compare_mapping(
                    fixture_id, "build_sequence", actual_sequence, expected["build_sequence"]
                )
            return errors

        errors.append(f"{fixture_id}: unknown operation {operation!r}")
    except (KeyError, TypeError, ValueError) as exc:
        errors.append(f"{fixture_id}: {exc}")
    return errors


def validate_documents(system: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    required = [ADR_PATH, RULES_PATH, FEAT001_PATH, COMBAT_PATH, FEAT009_PATH, FIXTURE_DOC_PATH, FIXTURES_PATH]
    for path in required:
        if not path.exists():
            errors.append(f"missing required file {path.relative_to(ROOT)}")
    if errors:
        return errors

    adr = ADR_PATH.read_text(encoding="utf-8")
    rules = RULES_PATH.read_text(encoding="utf-8")
    feat = FEAT001_PATH.read_text(encoding="utf-8")
    combat = COMBAT_PATH.read_text(encoding="utf-8")
    feat009 = FEAT009_PATH.read_text(encoding="utf-8")

    if "status: accepted" not in adr:
        errors.append("ADR-0012 must be accepted")
    for token in (
        "facility_site_is_empty_intersection",
        "Stone layer",
        "建設時の上限",
        "stone_occupied",
        "territory_control_lost",
    ):
        if token not in adr:
            errors.append(f"ADR-0012 missing required token {token!r}")
    if "## 施設交点の意味論" not in rules:
        errors.append("Rules Canon missing facility intersection semantics section")
    if "[[ADR-0012 Facility Sites Are Empty Intersections]]" not in rules:
        errors.append("Rules Canon must link ADR-0012")
    if "id: FEAT-001" not in feat or "status: accepted" not in feat:
        errors.append("FEAT-001 must have stable ID and accepted status")
    if "## Edge cases" not in feat or feat.count("**") < 6:
        errors.append("FEAT-001 must define multiple explicit edge cases")
    if "FacilityDestroyed? at placement point" not in combat:
        errors.append("Combat Resolution Order missing facility destruction placement step")
    if "ADR-0012 Facility Sites Are Empty Intersections" not in feat009:
        errors.append("FEAT-009 must reference accepted facility semantics")
    limits = system.get("facility_type_limits_per_region")
    if not isinstance(limits, dict) or limits.get("development") != 2 or limits.get("furnace") != 2:
        errors.append("system.json missing accepted facility per-type limits")
    return errors


def main() -> int:
    errors: list[str] = []
    try:
        system = load_json(SYSTEM_PATH)
    except Exception as exc:
        print(f"Facility semantics checks failed:\n- cannot load system data: {exc}")
        return 1
    errors.extend(validate_documents(system))

    try:
        fixtures = load_json(FIXTURES_PATH)
    except Exception as exc:
        errors.append(f"cannot load fixtures: {exc}")
        fixtures = []
    if not isinstance(fixtures, list):
        errors.append("facility fixtures must contain a list")
        fixtures = []

    fixture_ids: set[str] = set()
    for fixture in fixtures:
        if not isinstance(fixture, dict):
            errors.append("each facility fixture must be an object")
            continue
        fixture_id = fixture.get("id", "<missing-id>")
        if fixture_id in fixture_ids:
            errors.append(f"duplicate fixture ID {fixture_id}")
        fixture_ids.add(fixture_id)
        errors.extend(validate_fixture(fixture, system))

    missing = EXPECTED_FIXTURE_IDS - fixture_ids
    extra = fixture_ids - EXPECTED_FIXTURE_IDS
    if missing:
        errors.append(f"missing facility fixtures {sorted(missing)}")
    if extra:
        errors.append(f"unexpected facility fixtures {sorted(extra)}")

    if errors:
        print("Facility semantics checks failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    print(
        f"Facility semantics checks passed. {len(fixtures)} deterministic fixtures, "
        "ADR-0012 and FEAT-001 integration verified."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
