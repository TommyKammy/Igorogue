#!/usr/bin/env python3
"""Validate FEAT-002 Momentum data, fixtures, and document integration.

This is a specification checker. It intentionally does not replace the M1
shared Rules Kernel; the same fixtures must be ported to product tests.
"""
from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Iterable

ROOT = Path(__file__).resolve().parents[1]
SYSTEM_PATH = ROOT / "game_data" / "balance" / "system.json"
STYLES_PATH = ROOT / "game_data" / "content" / "styles.json"
CARDS_PATH = ROOT / "game_data" / "content" / "cards.json"
FIXTURES_PATH = ROOT / "game_data" / "fixtures" / "momentum_gate_fixtures.json"
RULES_PATH = ROOT / "docs" / "20_Design" / "Rules Canon.md"
SUMMARY_PATH = ROOT / "docs" / "20_Design" / "Momentum and Acceleration.md"
FEATURE_PATH = ROOT / "docs" / "20_Design" / "Feature Specs" / "FEAT-002 Momentum.md"
STYLES_DOC_PATH = ROOT / "docs" / "20_Design" / "Styles.md"
FIXTURE_DOC_PATH = (
    ROOT / "docs" / "50_Validation" / "Spec Fixtures" / "FEAT-002 Momentum Gate Fixtures.md"
)
SYSTEM_CAP = 2
BOARD_SIZE = 7
EXPECTED_FIXTURE_IDS = {f"MOM-{i:02d}" for i in range(1, 20)}
EXPECTED_ELIGIBLE_CARD_IDS = {
    "card_basic_stone",
    "card_extend",
    "card_blood_stone",
    "card_seed_stone",
}
BLACK_SYMBOLS = {"B", "K"}
WHITE_KING_SYMBOL = "Q"
VALID_SYMBOLS = {".", "B", "K", "W", "Q"}


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def point_tuple(raw: Any, label: str) -> tuple[int, int]:
    if (
        not isinstance(raw, list)
        or len(raw) != 2
        or any(isinstance(value, bool) or not isinstance(value, int) for value in raw)
    ):
        raise ValueError(f"{label}: point must be [x, y]")
    point = raw[0], raw[1]
    if not (1 <= point[0] <= BOARD_SIZE and 1 <= point[1] <= BOARD_SIZE):
        raise ValueError(f"{label}: point out of bounds {point}")
    return point


def canonical_points(points: Iterable[tuple[int, int]]) -> tuple[tuple[int, int], ...]:
    return tuple(sorted(points, key=lambda p: (p[1], p[0])))


def neighbours(point: tuple[int, int]) -> tuple[tuple[int, int], ...]:
    x, y = point
    return canonical_points(
        p
        for p in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
        if 1 <= p[0] <= BOARD_SIZE and 1 <= p[1] <= BOARD_SIZE
    )


def validate_rows(raw: Any, label: str) -> tuple[str, ...]:
    if not isinstance(raw, list) or len(raw) != BOARD_SIZE:
        raise ValueError(f"{label}: board must have seven rows")
    rows: list[str] = []
    for index, row in enumerate(raw):
        if not isinstance(row, str) or len(row) != BOARD_SIZE:
            raise ValueError(f"{label}[{index}]: row must have seven symbols")
        invalid = sorted(set(row) - VALID_SYMBOLS)
        if invalid:
            raise ValueError(f"{label}[{index}]: invalid symbols {invalid}")
        rows.append(row)
    return tuple(rows)


def row_index(y: int) -> int:
    return BOARD_SIZE - y


def symbol_at(rows: tuple[str, ...], point: tuple[int, int]) -> str:
    x, y = point
    return rows[row_index(y)][x - 1]


def black_points(rows: tuple[str, ...]) -> tuple[tuple[int, int], ...]:
    return canonical_points(
        (x, y)
        for y in range(1, BOARD_SIZE + 1)
        for x in range(1, BOARD_SIZE + 1)
        if symbol_at(rows, (x, y)) in BLACK_SYMBOLS
    )


def white_king_point(rows: tuple[str, ...]) -> tuple[int, int]:
    points = [
        (x, y)
        for y in range(1, BOARD_SIZE + 1)
        for x in range(1, BOARD_SIZE + 1)
        if symbol_at(rows, (x, y)) == WHITE_KING_SYMBOL
    ]
    if len(points) != 1:
        raise ValueError(f"board expected exactly one Q, got {len(points)}")
    return points[0]


def is_momentum_eligible(card: dict[str, Any]) -> bool:
    return (
        card.get("type") == "stone"
        and "frontline" in card.get("placement_tags", [])
        and card.get("black_place_stone_effect_count") == 1
    )


def printed_frontline_legal(rows: tuple[str, ...], target: tuple[int, int], card: dict[str, Any]) -> bool:
    if symbol_at(rows, target) != ".":
        return False
    tags = set(card.get("placement_tags", []))
    if "frontline" in tags and any(symbol_at(rows, p) in BLACK_SYMBOLS for p in neighbours(target)):
        return True
    # Other printed tags are deliberately not inferred by this FEAT checker.
    # A fixture that needs them should use the shared kernel after M1.
    return False


def momentum_sources(
    rows: tuple[str, ...], target: tuple[int, int]
) -> tuple[tuple[tuple[int, int], tuple[int, int]], ...]:
    tx, ty = target
    pairs: list[tuple[tuple[int, int], tuple[int, int]]] = []
    candidates = (
        ((tx - 2, ty), (tx - 1, ty)),
        ((tx + 2, ty), (tx + 1, ty)),
        ((tx, ty - 2), (tx, ty - 1)),
        ((tx, ty + 2), (tx, ty + 1)),
    )
    for source, midpoint in candidates:
        if not (1 <= source[0] <= BOARD_SIZE and 1 <= source[1] <= BOARD_SIZE):
            continue
        if symbol_at(rows, source) in BLACK_SYMBOLS and symbol_at(rows, midpoint) == ".":
            pairs.append((source, midpoint))
    return tuple(sorted(pairs, key=lambda pair: (pair[0][1], pair[0][0], pair[1][1], pair[1][0])))


def has_blocked_momentum_source(rows: tuple[str, ...], target: tuple[int, int]) -> bool:
    tx, ty = target
    candidates = (
        ((tx - 2, ty), (tx - 1, ty)),
        ((tx + 2, ty), (tx + 1, ty)),
        ((tx, ty - 2), (tx, ty - 1)),
        ((tx, ty + 2), (tx, ty + 1)),
    )
    for source, midpoint in candidates:
        if not (1 <= source[0] <= BOARD_SIZE and 1 <= source[1] <= BOARD_SIZE):
            continue
        if symbol_at(rows, source) in BLACK_SYMBOLS and symbol_at(rows, midpoint) != ".":
            return True
    return False


def evaluate_reach(fixture: dict[str, Any]) -> dict[str, Any]:
    rows = validate_rows(fixture.get("board"), f"{fixture.get('id')}/board")
    target = point_tuple(fixture.get("target"), f"{fixture.get('id')}/target")
    card = fixture.get("card")
    if not isinstance(card, dict):
        raise ValueError("card must be object")

    if symbol_at(rows, target) != ".":
        return {"mode": "none", "legal": False, "reason": "target_occupied", "momentum_cost": 0}
    if not is_momentum_eligible(card):
        return {
            "mode": "none",
            "legal": False,
            "reason": "card_not_momentum_eligible",
            "momentum_cost": 0,
        }
    if printed_frontline_legal(rows, target, card):
        return {
            "mode": "normal",
            "legal": True,
            "reason": "printed_placement_legal",
            "momentum_cost": 0,
        }

    pairs = momentum_sources(rows, target)
    if not pairs:
        reason = "momentum_midpoint_blocked" if has_blocked_momentum_source(rows, target) else "momentum_geometry_invalid"
        return {"mode": "none", "legal": False, "reason": reason, "momentum_cost": 0}
    if not fixture.get("full_legality", True):
        return {
            "mode": "momentum_reach",
            "legal": False,
            "reason": fixture.get("full_legality_reason", "full_legality_failed"),
            "momentum_cost": 0,
        }

    result: dict[str, Any] = {
        "mode": "momentum_reach",
        "legal": True,
        "reason": "legal",
        "momentum_cost": 1,
        "source_points": [list(pair[0]) for pair in pairs],
        "midpoint": list(pairs[0][1]),
    }
    facilities = {
        point_tuple(raw, f"{fixture.get('id')}/facilities") for raw in fixture.get("facilities", [])
    }
    if target in facilities:
        result["target_facility_destroyed_on_commit"] = True
    return result


def apply_generation(start: int, style_id: str, source: dict[str, Any], system: dict[str, Any]) -> dict[str, int]:
    momentum = system["momentum"]
    request = 0
    kind = source.get("kind")
    if kind == "black_territory_established":
        if source.get("source_owner") == "black" and source.get("new_black_points", 0) > 0:
            request = min(
                momentum["universal_source"]["amount"],
                momentum["universal_source"]["per_atomic_command_cap"],
            )
    elif kind == "facility_built":
        if style_id == "style_territory" and source.get("owner") == "black" and source.get("successful"):
            request = 1
    applied = min(request, max(0, momentum["cap"] - start))
    return {"end_amount": start + applied, "applied": applied, "overflow": request - applied}


def min_front_distance(rows: tuple[str, ...]) -> int:
    king = white_king_point(rows)
    black = black_points(rows)
    if not black:
        raise ValueError("board must contain a black stone")
    return min(abs(point[0] - king[0]) + abs(point[1] - king[1]) for point in black)


def rows_with_added_black(rows: tuple[str, ...], point: tuple[int, int]) -> tuple[str, ...]:
    if symbol_at(rows, point) != ".":
        raise ValueError(f"after_added_black target occupied {point}")
    mutable = [list(row) for row in rows]
    mutable[row_index(point[1])][point[0] - 1] = "B"
    return tuple("".join(row) for row in mutable)


def validate_expected_subset(fixture_id: str, actual: dict[str, Any], expected: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    for key, value in expected.items():
        if actual.get(key) != value:
            errors.append(f"{fixture_id}: {key} expected {value!r}, got {actual.get(key)!r}")
    return errors


def validate_fixture(fixture: dict[str, Any], system: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    fixture_id = fixture.get("id", "<missing-id>")
    operation = fixture.get("operation")
    try:
        if operation == "generate":
            actual = apply_generation(
                fixture.get("start_amount"), fixture.get("style_id"), fixture.get("source", {}), system
            )
            errors += validate_expected_subset(fixture_id, actual, fixture.get("expected", {}))

        elif operation == "player_turn_start":
            actual = {"end_amount": fixture.get("start_amount"), "approach_draw_used": False}
            errors += validate_expected_subset(fixture_id, actual, fixture.get("expected", {}))

        elif operation == "battle_start":
            actual = {"end_amount": system["momentum"]["start_amount"], "approach_draw_used": False}
            errors += validate_expected_subset(fixture_id, actual, fixture.get("expected", {}))

        elif operation == "validate_reach":
            actual = evaluate_reach(fixture)
            errors += validate_expected_subset(fixture_id, actual, fixture.get("expected", {}))

        elif operation == "resolve_reach":
            reach = evaluate_reach(fixture)
            accepted = reach.get("legal") and reach.get("mode") == "momentum_reach"
            if accepted:
                actual = {
                    "accepted": True,
                    "end_momentum": fixture.get("start_momentum") - 1,
                    "end_qi": fixture.get("start_qi") - fixture.get("card", {}).get("qi_cost", 0),
                    "card_zone": "used",
                    "reason": "legal",
                }
            else:
                actual = {
                    "accepted": False,
                    "end_momentum": fixture.get("start_momentum"),
                    "end_qi": fixture.get("start_qi"),
                    "card_zone": fixture.get("card_zone"),
                    "reason": reach.get("reason"),
                }
            errors += validate_expected_subset(fixture_id, actual, fixture.get("expected", {}))

        elif operation == "approach_draw":
            before = validate_rows(fixture.get("before_board"), f"{fixture_id}/before_board")
            point = point_tuple(fixture.get("after_added_black"), f"{fixture_id}/after_added_black")
            after = rows_with_added_black(before, point)
            distance_before = min_front_distance(before)
            distance_after = min_front_distance(after)
            used = bool(fixture.get("approach_draw_used"))
            can_draw = (
                not fixture.get("battle_ended")
                and not used
                and distance_after < distance_before
                and fixture.get("projected_black_income", 0)
                >= system["momentum"]["approach_draw"]["projected_black_income_threshold"]
            )
            actual = {
                "front_distance_before": distance_before,
                "front_distance_after": distance_after,
                "draw": 1 if can_draw else 0,
                "approach_draw_used": used or can_draw,
            }
            errors += validate_expected_subset(fixture_id, actual, fixture.get("expected", {}))

        else:
            errors.append(f"{fixture_id}: unknown operation {operation!r}")
    except (KeyError, TypeError, ValueError) as exc:
        errors.append(f"{fixture_id}: {exc}")
    return errors


def validate_system_and_content(system: dict[str, Any], styles: list[Any], cards: list[Any]) -> list[str]:
    errors: list[str] = []
    momentum = system.get("momentum")
    if not isinstance(momentum, dict):
        return ["system.json missing momentum object"]
    expected = {
        "scope": "global_player_battle_resource",
        "start_amount": 0,
        "cap": 2,
        "persist_across_turns": True,
        "reset_at_battle_start": True,
        "reset_at_battle_end": True,
    }
    for key, value in expected.items():
        if momentum.get(key) != value:
            errors.append(f"system.momentum.{key} expected {value!r}, got {momentum.get(key)!r}")
    if "max_momentum" in system:
        errors.append("system.json must not duplicate momentum cap in max_momentum")

    universal = momentum.get("universal_source", {})
    expected_universal = {
        "id": "black_territory_established",
        "amount": 1,
        "per_atomic_command_cap": 1,
        "requires_source_owner": "black",
        "requires_newly_black_owned_point": True,
    }
    for key, value in expected_universal.items():
        if universal.get(key) != value:
            errors.append(f"system.momentum.universal_source.{key} expected {value!r}, got {universal.get(key)!r}")

    reach = momentum.get("reach", {})
    reach_expected = {
        "distance": 2,
        "alignment": "orthogonal",
        "midpoint_stone_layer": "empty",
        "target_stone_layer": "empty",
        "normal_printed_legality_takes_precedence": True,
        "facilities_count_as_empty": True,
        "cost": 1,
    }
    for key, value in reach_expected.items():
        if reach.get(key) != value:
            errors.append(f"system.momentum.reach.{key} expected {value!r}, got {reach.get(key)!r}")

    approach = momentum.get("approach_draw", {})
    approach_expected = {
        "cards": 1,
        "per_player_turn_cap": 1,
        "projected_black_income_threshold": 4,
        "distance_metric": "minimum_black_stone_to_white_king_manhattan",
        "requires_strict_distance_reduction": True,
        "evaluate_after_atomic_resolution": True,
        "suppressed_after_battle_end": True,
    }
    for key, value in approach_expected.items():
        if approach.get(key) != value:
            errors.append(f"system.momentum.approach_draw.{key} expected {value!r}, got {approach.get(key)!r}")

    style_by_id = {item.get("id"): item for item in styles if isinstance(item, dict)}
    territory = style_by_id.get("style_territory")
    if territory is None:
        errors.append("styles.json missing style_territory")
    else:
        if territory.get("rules") != ["facility_build_grants_momentum"]:
            errors.append(
                "style_territory.rules must be exactly ['facility_build_grants_momentum'] for A-5"
            )
        expected_bonus = {
            "event": "facility_built",
            "owner": "black",
            "amount": 1,
            "per_atomic_command_cap": 1,
            "requires_successful_build": True,
        }
        if territory.get("momentum_bonus_source") != expected_bonus:
            errors.append(
                f"style_territory.momentum_bonus_source expected {expected_bonus!r}, "
                f"got {territory.get('momentum_bonus_source')!r}"
            )
    for style_id, style in style_by_id.items():
        if style_id == "style_territory":
            continue
        if "facility_build_grants_momentum" in style.get("rules", []):
            errors.append(f"{style_id} must not have facility_build_grants_momentum")
        if "momentum_bonus_source" in style:
            errors.append(f"{style_id} must not declare momentum_bonus_source")

    eligible_ids: set[str] = set()
    for card in cards:
        if not isinstance(card, dict):
            continue
        effects = card.get("effects", [])
        effect_count = sum(
            1
            for effect in effects
            if isinstance(effect, dict)
            and effect.get("op") == "place_stone"
            and effect.get("stone") is not None
        )
        normalized = {
            "type": card.get("type"),
            "placement_tags": card.get("placement_tags", []),
            "black_place_stone_effect_count": effect_count,
        }
        if is_momentum_eligible(normalized):
            eligible_ids.add(card.get("id"))
    if eligible_ids != EXPECTED_ELIGIBLE_CARD_IDS:
        errors.append(
            f"eligible card ids expected {sorted(EXPECTED_ELIGIBLE_CARD_IDS)}, got {sorted(eligible_ids)}"
        )
    return errors


def validate_document_integration() -> list[str]:
    errors: list[str] = []
    required = {
        FEATURE_PATH: [
            "status: accepted",
            "全流儀共通",
            "facility_build_grants_momentum",
            "momentum_reach",
            "minimum Manhattan distance",
            "MOM-01〜MOM-19",
        ],
        RULES_PATH: [
            "version: 0.2.7",
            "余勢は全流儀共通",
            "地合い流だけは",
            "`momentum_reach`",
            "[[FEAT-002 Momentum]]",
        ],
        SUMMARY_PATH: ["全流儀共通", "地合い流の追加生成", "[[FEAT-002 Momentum]]"],
        STYLES_DOC_PATH: [
            "全流儀共通の余勢",
            "facility_build_grants_momentum",
            "他流儀は施設建設だけでは余勢を得ない",
        ],
        FIXTURE_DOC_PATH: ["MOM-01", "MOM-19", "normal placement precedence"],
    }
    for path, needles in required.items():
        if not path.exists():
            errors.append(f"missing required file {path.relative_to(ROOT)}")
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
        styles = load_json(STYLES_PATH)
        cards = load_json(CARDS_PATH)
    except Exception as exc:
        print(f"Momentum checks failed:\n- cannot load content: {exc}")
        return 1

    if not isinstance(system, dict):
        errors.append("system.json must be object")
    if not isinstance(styles, list):
        errors.append("styles.json must be list")
        styles = []
    if not isinstance(cards, list):
        errors.append("cards.json must be list")
        cards = []
    if isinstance(system, dict):
        errors.extend(validate_system_and_content(system, styles, cards))

    try:
        fixture_data = load_json(FIXTURES_PATH)
    except Exception as exc:
        errors.append(f"cannot load momentum fixtures: {exc}")
        fixture_data = {}
    fixtures = fixture_data.get("fixtures") if isinstance(fixture_data, dict) else None
    if not isinstance(fixtures, list):
        errors.append("momentum_gate_fixtures.json must contain fixtures list")
        fixtures = []
    ids = {fixture.get("id") for fixture in fixtures if isinstance(fixture, dict)}
    if ids != EXPECTED_FIXTURE_IDS:
        errors.append(
            f"fixture ids expected {sorted(EXPECTED_FIXTURE_IDS)}, got {sorted(str(v) for v in ids)}"
        )
    if len(ids) != len(fixtures):
        errors.append("momentum fixture ids must be unique")
    if isinstance(system, dict):
        for fixture in fixtures:
            if not isinstance(fixture, dict):
                errors.append("momentum fixture must be object")
                continue
            errors.extend(validate_fixture(fixture, system))

    errors.extend(validate_document_integration())

    if errors:
        print("Momentum checks failed:")
        for error in errors:
            print("-", error)
        return 1
    print("Momentum checks passed — 19 deterministic fixtures, global gate and territory-style source")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
