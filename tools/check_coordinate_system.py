#!/usr/bin/env python3
"""Validate Igorogue's canonical coordinate contract and point-symmetric setup.

This is a specification checker, not the product Rules Kernel. M1 must port the
same fixtures to the shared kernel and golden replay suite.
"""
from __future__ import annotations

from collections import Counter, deque
import json
from pathlib import Path
from typing import Any, Iterable

ROOT = Path(__file__).resolve().parents[1]
SYSTEM_PATH = ROOT / "game_data" / "balance" / "system.json"
FIXTURES_PATH = ROOT / "game_data" / "fixtures" / "coordinate_system_fixtures.json"
SPEC_PATH = ROOT / "docs" / "20_Design" / "Coordinate System and Initial Position.md"
RULES_PATH = ROOT / "docs" / "20_Design" / "Rules Canon.md"
BOARD_PATH = ROOT / "docs" / "20_Design" / "Board and Placement.md"
DOMAIN_PATH = ROOT / "docs" / "30_Technical" / "Domain Model.md"
DETERMINISM_PATH = ROOT / "docs" / "30_Technical" / "Determinism and Replay.md"
BOARD_REPETITION_CHECKER_PATH = ROOT / "tools" / "check_board_repetition.py"
FACILITY_CHECKER_PATH = ROOT / "tools" / "check_facility_semantics.py"
FIXTURE_DOC_PATH = (
    ROOT
    / "docs"
    / "50_Validation"
    / "Spec Fixtures"
    / "Coordinate System and Initial Position Fixtures.md"
)

EXPECTED_FIXTURE_IDS = {f"COORD-{n:02d}" for n in range(1, 13)}
VALID_COLORS = {"black", "white"}
VALID_ROLES = {"king", "guard"}
EXPECTED_INITIAL_STONES = {
    ("black", "king", (2, 2)),
    ("black", "guard", (2, 3)),
    ("black", "guard", (3, 2)),
    ("white", "king", (6, 6)),
    ("white", "guard", (5, 6)),
    ("white", "guard", (6, 5)),
}


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def point_tuple(raw: Any, label: str) -> tuple[int, int]:
    if not isinstance(raw, list) or len(raw) != 2:
        raise ValueError(f"{label}: point must be [x, y]")
    if any(isinstance(value, bool) or not isinstance(value, int) for value in raw):
        raise ValueError(f"{label}: coordinates must be integers")
    return raw[0], raw[1]


def canonical_points(points: Iterable[tuple[int, int]]) -> tuple[tuple[int, int], ...]:
    return tuple(sorted(points, key=lambda point: (point[1], point[0])))


def in_display_bounds(point: tuple[int, int], size: int) -> bool:
    x, y = point
    return 1 <= x <= size and 1 <= y <= size


def in_internal_bounds(point: tuple[int, int], size: int) -> bool:
    x, y = point
    return 0 <= x < size and 0 <= y < size


def display_to_internal(point: tuple[int, int], size: int) -> tuple[int, int]:
    if not in_display_bounds(point, size):
        raise ValueError(f"display point out of range: {point}")
    return point[0] - 1, point[1] - 1


def internal_to_display(point: tuple[int, int], size: int) -> tuple[int, int]:
    if not in_internal_bounds(point, size):
        raise ValueError(f"internal point out of range: {point}")
    return point[0] + 1, point[1] + 1


def canonical_index(point: tuple[int, int], size: int) -> int:
    if not in_display_bounds(point, size):
        raise ValueError(f"display point out of range: {point}")
    x, y = point
    return (y - 1) * size + (x - 1)


def point_from_index(index: int, size: int) -> tuple[int, int]:
    if isinstance(index, bool) or not isinstance(index, int) or not 0 <= index < size * size:
        raise ValueError(f"canonical index out of range: {index}")
    return index % size + 1, index // size + 1


def reflect(point: tuple[int, int], size: int) -> tuple[int, int]:
    if not in_display_bounds(point, size):
        raise ValueError(f"display point out of range: {point}")
    x, y = point
    return size + 1 - x, size + 1 - y


def neighbours(point: tuple[int, int], size: int) -> tuple[tuple[int, int], ...]:
    if not in_display_bounds(point, size):
        raise ValueError(f"display point out of range: {point}")
    x, y = point
    candidates = ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
    return canonical_points(candidate for candidate in candidates if in_display_bounds(candidate, size))


def parse_initial_position(system: dict[str, Any], size: int) -> tuple[dict[str, Any], ...]:
    initial = system.get("initial_position")
    if not isinstance(initial, dict):
        raise ValueError("system.json missing initial_position object")
    stones_raw = initial.get("stones")
    if not isinstance(stones_raw, list):
        raise ValueError("initial_position.stones must be a list")
    stones: list[dict[str, Any]] = []
    for index, raw in enumerate(stones_raw):
        if not isinstance(raw, dict):
            raise ValueError(f"initial_position.stones[{index}] must be object")
        color = raw.get("color")
        role = raw.get("role")
        point = point_tuple(raw.get("point"), f"initial_position.stones[{index}].point")
        if color not in VALID_COLORS:
            raise ValueError(f"initial_position.stones[{index}]: invalid color {color!r}")
        if role not in VALID_ROLES:
            raise ValueError(f"initial_position.stones[{index}]: invalid role {role!r}")
        if not in_display_bounds(point, size):
            raise ValueError(f"initial_position.stones[{index}]: point out of range {point}")
        stones.append({"color": color, "role": role, "point": point})
    return tuple(stones)


def group_from(
    occupied: dict[tuple[int, int], dict[str, Any]],
    start: tuple[int, int],
    size: int,
) -> set[tuple[int, int]]:
    color = occupied[start]["color"]
    found: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque([start])
    while queue:
        point = queue.popleft()
        if point in found or occupied.get(point, {}).get("color") != color:
            continue
        found.add(point)
        for adjacent in neighbours(point, size):
            if adjacent not in found and occupied.get(adjacent, {}).get("color") == color:
                queue.append(adjacent)
    return found


def liberties(
    occupied: dict[tuple[int, int], dict[str, Any]],
    group: Iterable[tuple[int, int]],
    size: int,
) -> tuple[tuple[int, int], ...]:
    empty: set[tuple[int, int]] = set()
    for point in group:
        for adjacent in neighbours(point, size):
            if adjacent not in occupied:
                empty.add(adjacent)
    return canonical_points(empty)


def validate_system(system: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    size = system.get("board_size")
    if size != 7:
        errors.append(f"system.board_size expected 7, got {size!r}")
        return errors

    coordinates = system.get("coordinate_system")
    if not isinstance(coordinates, dict):
        errors.append("system.coordinate_system missing or not object")
        return errors

    expected_fields = {
        "display_min": 1,
        "display_max": size,
        "display_x_direction": "left_to_right",
        "display_y_direction": "bottom_to_top",
        "internal_min": 0,
        "internal_max": size - 1,
        "diagram_row_order": "y_descending",
        "canonical_point_order": "y_ascending_then_x_ascending",
        "canonical_index_formula": "(y-1)*board_size+(x-1)",
        "point_reflection_formula": "(board_size+1-x,board_size+1-y)",
        "center": [(size + 1) // 2, (size + 1) // 2],
    }
    for key, expected in expected_fields.items():
        if coordinates.get(key) != expected:
            errors.append(
                f"system.coordinate_system.{key} expected {expected!r}, got {coordinates.get(key)!r}"
            )

    initial = system.get("initial_position", {})
    if initial.get("id") != "standard_v0_2":
        errors.append(f"initial_position.id expected standard_v0_2, got {initial.get('id')!r}")
    if initial.get("symmetry") != "point_reflection_with_color_and_role_swap":
        errors.append("initial_position.symmetry must declare point reflection with color and role swap")

    try:
        stones = parse_initial_position(system, size)
    except ValueError as exc:
        return errors + [str(exc)]

    points = [stone["point"] for stone in stones]
    actual_initial_stones = {(stone["color"], stone["role"], stone["point"]) for stone in stones}
    if actual_initial_stones != EXPECTED_INITIAL_STONES:
        errors.append(
            f"standard_v0_2 stones expected {sorted(EXPECTED_INITIAL_STONES)}, "
            f"got {sorted(actual_initial_stones)}"
        )
    if len(stones) != 6:
        errors.append(f"initial position expected 6 stones, got {len(stones)}")
    if len(set(points)) != len(points):
        errors.append("initial position contains overlapping stones")

    counts = Counter((stone["color"], stone["role"]) for stone in stones)
    for color in sorted(VALID_COLORS):
        if counts[(color, "king")] != 1:
            errors.append(f"{color} expected one king, got {counts[(color, 'king')]}")
        if counts[(color, "guard")] != 2:
            errors.append(f"{color} expected two guards, got {counts[(color, 'guard')]}")

    by_key = {(stone["color"], stone["role"], stone["point"]) for stone in stones}
    opposite = {"black": "white", "white": "black"}
    for stone in stones:
        reflected = reflect(stone["point"], size)
        counterpart = (opposite[stone["color"]], stone["role"], reflected)
        if counterpart not in by_key:
            errors.append(f"missing reflected counterpart for {stone}: expected {counterpart}")

    center = ((size + 1) // 2, (size + 1) // 2)
    if center in set(points):
        errors.append("initial center point must be empty")

    occupied = {stone["point"]: stone for stone in stones}
    for color in ("black", "white"):
        king = next((stone for stone in stones if stone["color"] == color and stone["role"] == "king"), None)
        if king is None:
            continue
        group = group_from(occupied, king["point"], size)
        color_points = {stone["point"] for stone in stones if stone["color"] == color}
        if group != color_points:
            errors.append(f"{color} initial stones must form one connected king group")
        group_liberties = liberties(occupied, group, size)
        if len(group_liberties) != 7:
            errors.append(f"{color} initial king group expected 7 liberties, got {len(group_liberties)}")

    return errors


def validate_fixture(fixture: dict[str, Any], system: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    fixture_id = fixture.get("id", "<missing-id>")
    operation = fixture.get("operation")
    size = system["board_size"]

    try:
        if operation == "display_to_internal":
            point = point_tuple(fixture.get("input"), f"{fixture_id}/input")
            expected = point_tuple(fixture.get("expected"), f"{fixture_id}/expected")
            actual = display_to_internal(point, size)
            if actual != expected:
                errors.append(f"{fixture_id}: expected internal {expected}, got {actual}")

        elif operation == "round_trip_and_index":
            point = point_tuple(fixture.get("display"), f"{fixture_id}/display")
            expected_internal = point_tuple(
                fixture.get("expected_internal"), f"{fixture_id}/expected_internal"
            )
            expected_display = point_tuple(
                fixture.get("expected_display"), f"{fixture_id}/expected_display"
            )
            internal = display_to_internal(point, size)
            round_trip = internal_to_display(internal, size)
            index = canonical_index(point, size)
            if internal != expected_internal:
                errors.append(f"{fixture_id}: internal expected {expected_internal}, got {internal}")
            if round_trip != expected_display:
                errors.append(f"{fixture_id}: round trip expected {expected_display}, got {round_trip}")
            if index != fixture.get("expected_index"):
                errors.append(f"{fixture_id}: index expected {fixture.get('expected_index')}, got {index}")

        elif operation == "display_bounds":
            expected = fixture.get("expected_in_bounds")
            for index, raw in enumerate(fixture.get("points", [])):
                point = point_tuple(raw, f"{fixture_id}/points[{index}]")
                actual = in_display_bounds(point, size)
                if actual != expected:
                    errors.append(f"{fixture_id}: point {point} in_bounds expected {expected}, got {actual}")

        elif operation == "canonical_indices":
            for index, case in enumerate(fixture.get("cases", [])):
                point = point_tuple(case.get("point"), f"{fixture_id}/cases[{index}].point")
                expected = case.get("expected_index")
                actual = canonical_index(point, size)
                if actual != expected:
                    errors.append(f"{fixture_id}: {point} index expected {expected}, got {actual}")
                if point_from_index(actual, size) != point:
                    errors.append(f"{fixture_id}: index {actual} did not round-trip to {point}")

        elif operation == "neighbors":
            point = point_tuple(fixture.get("point"), f"{fixture_id}/point")
            expected = canonical_points(
                point_tuple(raw, f"{fixture_id}/expected") for raw in fixture.get("expected", [])
            )
            actual = neighbours(point, size)
            if actual != expected:
                errors.append(f"{fixture_id}: neighbours expected {expected}, got {actual}")

        elif operation == "reflection_involution":
            point = point_tuple(fixture.get("point"), f"{fixture_id}/point")
            expected_reflection = point_tuple(
                fixture.get("expected_reflection"), f"{fixture_id}/expected_reflection"
            )
            expected_round_trip = point_tuple(
                fixture.get("expected_round_trip"), f"{fixture_id}/expected_round_trip"
            )
            reflected = reflect(point, size)
            round_trip = reflect(reflected, size)
            if reflected != expected_reflection:
                errors.append(
                    f"{fixture_id}: reflection expected {expected_reflection}, got {reflected}"
                )
            if round_trip != expected_round_trip:
                errors.append(
                    f"{fixture_id}: reflection round trip expected {expected_round_trip}, got {round_trip}"
                )

        elif operation == "initial_position_symmetry":
            initial = system["initial_position"]
            stones = parse_initial_position(system, size)
            if initial.get("id") != fixture.get("expected_setup_id"):
                errors.append(f"{fixture_id}: setup id mismatch")
            if len(stones) != fixture.get("expected_stone_count"):
                errors.append(f"{fixture_id}: stone count mismatch")
            expected_stones = {
                (
                    item.get("color"),
                    item.get("role"),
                    point_tuple(item.get("point"), f"{fixture_id}/expected_stones"),
                )
                for item in fixture.get("expected_stones", [])
            }
            actual_stones = {(stone["color"], stone["role"], stone["point"]) for stone in stones}
            if actual_stones != expected_stones:
                errors.append(
                    f"{fixture_id}: stones expected {sorted(expected_stones)}, got {sorted(actual_stones)}"
                )
            expected_roles = fixture.get("expected_role_counts_per_color", {})
            counts = Counter((stone["color"], stone["role"]) for stone in stones)
            for color in VALID_COLORS:
                for role, expected in expected_roles.items():
                    if counts[(color, role)] != expected:
                        errors.append(
                            f"{fixture_id}: {color} {role} expected {expected}, got {counts[(color, role)]}"
                        )
            center = tuple(system["coordinate_system"]["center"])
            center_empty = center not in {stone["point"] for stone in stones}
            if center_empty != fixture.get("expected_center_empty"):
                errors.append(f"{fixture_id}: center empty expected {fixture.get('expected_center_empty')}")
            errors.extend(f"{fixture_id}: {error}" for error in validate_system(system) if "initial" in error or "reflected" in error or "center" in error)

        elif operation == "initial_king_groups":
            stones = parse_initial_position(system, size)
            occupied = {stone["point"]: stone for stone in stones}
            expected_by_color = fixture.get("expected", {})
            for color in ("black", "white"):
                expected = expected_by_color.get(color, {})
                king = next(stone for stone in stones if stone["color"] == color and stone["role"] == "king")
                group = group_from(occupied, king["point"], size)
                group_liberties = liberties(occupied, group, size)
                actual = {
                    "stone_count": len(group),
                    "connected": group == {stone["point"] for stone in stones if stone["color"] == color},
                    "liberty_count": len(group_liberties),
                    "contains_king": king["point"] in group,
                }
                if actual != expected:
                    errors.append(f"{fixture_id}: {color} expected {expected}, got {actual}")

        elif operation == "diagram_and_canonical_order":
            expected_rows = fixture.get("expected_diagram_row_to_y")
            actual_rows = list(range(size, 0, -1))
            if actual_rows != expected_rows:
                errors.append(f"{fixture_id}: diagram rows expected {expected_rows}, got {actual_rows}")
            all_points = tuple((x, y) for y in range(1, size + 1) for x in range(1, size + 1))
            expected_first = tuple(
                point_tuple(raw, f"{fixture_id}/expected_first_points")
                for raw in fixture.get("expected_first_points", [])
            )
            expected_last = point_tuple(
                fixture.get("expected_last_point"), f"{fixture_id}/expected_last_point"
            )
            if all_points[: len(expected_first)] != expected_first:
                errors.append(
                    f"{fixture_id}: first points expected {expected_first}, got {all_points[:len(expected_first)]}"
                )
            if all_points[-1] != expected_last:
                errors.append(f"{fixture_id}: last point expected {expected_last}, got {all_points[-1]}")
            stones = parse_initial_position(system, size)
            symbols = {("black", "king"): "K", ("white", "king"): "Q", ("black", "guard"): "B", ("white", "guard"): "W"}
            occupied = {stone["point"]: symbols[(stone["color"], stone["role"])] for stone in stones}
            actual_board = [
                "".join(occupied.get((x, y), ".") for x in range(1, size + 1))
                for y in range(size, 0, -1)
            ]
            expected_board = fixture.get("expected_initial_board")
            if actual_board != expected_board:
                errors.append(f"{fixture_id}: initial board expected {expected_board}, got {actual_board}")

        else:
            errors.append(f"{fixture_id}: unknown operation {operation!r}")

    except (KeyError, StopIteration, TypeError, ValueError) as exc:
        errors.append(f"{fixture_id}: {exc}")

    return errors


def validate_document_integration() -> list[str]:
    errors: list[str] = []
    required = {
        SPEC_PATH: [
            "CanonicalPoint = (x, y)",
            "reflect(x, y) = (8 - x, 8 - y)",
            "standard_v0_2",
            "幾何学的な初期条件",
        ],
        RULES_PATH: [
            "[[Coordinate System and Initial Position]]",
            "`(x,y)`",
            "`reflect(x,y)=(8-x,8-y)`",
            "点対称",
        ],
        BOARD_PATH: ["[[Coordinate System and Initial Position]]", "Canonical point order"],
        DOMAIN_PATH: ["CanonicalPoint", "InternalPoint", "canonical index"],
        DETERMINISM_PATH: ["CanonicalPoint", "Coordinate System and Initial Position"],
        FIXTURE_DOC_PATH: ["COORD-01", "COORD-12", "standard_v0_2"],
        BOARD_REPETITION_CHECKER_PATH: ["rows[row_index(y)]", "range(1, BOARD_SIZE + 1)"],
        FACILITY_CHECKER_PATH: ["rows[row_index(y)]", "range(1, BOARD_SIZE + 1)"],
    }
    for path, needles in required.items():
        if not path.exists():
            errors.append(f"missing required document {path.relative_to(ROOT)}")
            continue
        text = path.read_text(encoding="utf-8")
        for needle in needles:
            if needle not in text:
                errors.append(f"{path.relative_to(ROOT)} missing required text {needle!r}")
    return errors


def main() -> int:
    errors: list[str] = []
    try:
        system = load_json(SYSTEM_PATH)
    except Exception as exc:  # pragma: no cover - CLI diagnostic
        print(f"Coordinate system checks failed:\n- cannot load system.json: {exc}")
        return 1

    if not isinstance(system, dict):
        errors.append("system.json must contain an object")
    else:
        errors.extend(validate_system(system))

    try:
        fixture_data = load_json(FIXTURES_PATH)
    except Exception as exc:  # pragma: no cover - CLI diagnostic
        errors.append(f"cannot load coordinate fixtures: {exc}")
        fixture_data = {}

    fixtures = fixture_data.get("fixtures") if isinstance(fixture_data, dict) else None
    if not isinstance(fixtures, list):
        errors.append("coordinate_system_fixtures.json must contain fixtures list")
        fixtures = []

    fixture_ids = {fixture.get("id") for fixture in fixtures if isinstance(fixture, dict)}
    if fixture_ids != EXPECTED_FIXTURE_IDS:
        errors.append(
            f"fixture ids expected {sorted(EXPECTED_FIXTURE_IDS)}, got {sorted(str(value) for value in fixture_ids)}"
        )
    if len(fixture_ids) != len(fixtures):
        errors.append("coordinate fixture ids must be unique")

    if isinstance(system, dict):
        for fixture in fixtures:
            if not isinstance(fixture, dict):
                errors.append("coordinate fixture must be object")
                continue
            errors.extend(validate_fixture(fixture, system))

    errors.extend(validate_document_integration())

    if errors:
        print("Coordinate system checks failed:")
        for error in errors:
            print("-", error)
        return 1

    print("Coordinate system checks passed — 12 deterministic fixtures, point-symmetric setup")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
