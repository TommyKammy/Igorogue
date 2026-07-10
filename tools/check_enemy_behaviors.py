#!/usr/bin/env python3
"""Validate FEAT-009 enemy behavior data and deterministic decision fixtures."""
from __future__ import annotations

import json
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
ENEMIES_PATH = ROOT / "game_data" / "content" / "enemies.json"
FIXTURES_PATH = ROOT / "game_data" / "fixtures" / "enemy_behavior_decision_fixtures.json"
SPEC_PATH = ROOT / "docs" / "20_Design" / "Feature Specs" / "FEAT-009 Enemy Action Planning and Placement.md"

PLACEMENT_MODES = {
    "white_frontline",
    "white_contact",
    "white_terminal",
    "white_invasion",
    "white_facility_invasion",
}
SCORE_PROFILES = {
    "king_execution",
    "king_defense",
    "capture_value",
    "king_pressure",
    "king_advance",
    "invasion_escape",
    "facility_trample",
    "territory_invasion",
}

EXPECTED_CONFIG = {
    "enemy_bandit": {
        "mandatory_overrides": ["capture_black_king", "defend_white_king"],
        "plan_priority": ["capture_non_king", "pressure_black_king", "advance_toward_black_king"],
        "counterattack_priority": ["capture_non_king", "pressure_black_king", "advance_toward_black_king"],
        "parameters": {"defense_threshold": 2, "opportunistic_capture_min_stones": 1},
    },
    "enemy_invader": {
        "mandatory_overrides": ["capture_black_king", "defend_white_king"],
        "plan_priority": [
            "escape_active_invasion",
            "trample_facility",
            "capture_non_king",
            "invade_largest_territory",
            "pressure_black_king",
            "advance_toward_black_king",
        ],
        "counterattack_priority": [
            "escape_active_invasion",
            "trample_facility",
            "capture_non_king",
            "invade_largest_territory",
            "pressure_black_king",
            "advance_toward_black_king",
        ],
        "parameters": {
            "defense_threshold": 1,
            "opportunistic_capture_min_stones": 2,
            "minimum_remote_invasion_territory_size": 2,
            "remote_invasion_cooldown_enemy_turns": 1,
            "remote_invasion_max_per_enemy_turn": 1,
        },
    },
}

REQUIRED_INTENTS = {
    "enemy_bandit": {
        "capture_black_king",
        "defend_white_king",
        "capture_non_king",
        "pressure_black_king",
        "advance_toward_black_king",
    },
    "enemy_invader": {
        "capture_black_king",
        "defend_white_king",
        "escape_active_invasion",
        "trample_facility",
        "capture_non_king",
        "invade_largest_territory",
        "pressure_black_king",
        "advance_toward_black_king",
    },
}


def load(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def validate_enemy(enemy: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    enemy_id = enemy.get("id", "<missing-id>")
    if enemy.get("implementation_status") != "specified":
        return errors

    if enemy.get("behavior_spec") != "FEAT-009":
        errors.append(f"{enemy_id}: behavior_spec must be FEAT-009")
    if enemy.get("tie_break") != "canonical_y_then_x":
        errors.append(f"{enemy_id}: tie_break must be canonical_y_then_x")

    budget = enemy.get("action_budget", {})
    if budget != {
        "normal_actions": 1,
        "counterattack_bonus_actions": 1,
        "max_actions_per_enemy_turn": 2,
    }:
        errors.append(f"{enemy_id}: unexpected action_budget {budget}")

    permissions = set(enemy.get("placement_permissions", []))
    unknown_permissions = permissions - PLACEMENT_MODES
    if unknown_permissions:
        errors.append(f"{enemy_id}: unknown placement permissions {sorted(unknown_permissions)}")

    intents = enemy.get("intents")
    if not isinstance(intents, list):
        return errors + [f"{enemy_id}: intents must be a list of objects"]
    intent_ids = [intent.get("id") for intent in intents if isinstance(intent, dict)]
    if len(intent_ids) != len(set(intent_ids)):
        errors.append(f"{enemy_id}: duplicate intent IDs")
    intent_set = set(intent_ids)

    missing = REQUIRED_INTENTS.get(enemy_id, set()) - intent_set
    if missing:
        errors.append(f"{enemy_id}: missing required intents {sorted(missing)}")

    for ref_field in ("mandatory_overrides", "plan_priority", "counterattack_priority"):
        refs = enemy.get(ref_field, [])
        if not isinstance(refs, list):
            errors.append(f"{enemy_id}: {ref_field} must be a list")
            continue
        for ref in refs:
            if ref not in intent_set:
                errors.append(f"{enemy_id}: {ref_field} references unknown intent {ref}")

    for intent in intents:
        if not isinstance(intent, dict):
            errors.append(f"{enemy_id}: intent must be an object")
            continue
        intent_id = intent.get("id", "<missing-intent-id>")
        modes = set(intent.get("placement_modes", []))
        if not modes:
            errors.append(f"{enemy_id}/{intent_id}: placement_modes is empty")
        if not modes <= permissions:
            errors.append(
                f"{enemy_id}/{intent_id}: modes outside enemy permissions {sorted(modes - permissions)}"
            )
        profile = intent.get("score_profile")
        if profile not in SCORE_PROFILES:
            errors.append(f"{enemy_id}/{intent_id}: unknown score_profile {profile}")
        fallback = intent.get("fallback")
        if not isinstance(fallback, list):
            errors.append(f"{enemy_id}/{intent_id}: fallback must be a list")
        else:
            for ref in fallback:
                if ref not in intent_set:
                    errors.append(f"{enemy_id}/{intent_id}: fallback references unknown intent {ref}")

    expected = EXPECTED_CONFIG.get(enemy_id)
    if expected is not None:
        for field in ("mandatory_overrides", "plan_priority", "counterattack_priority"):
            if enemy.get(field) != expected[field]:
                errors.append(
                    f"{enemy_id}: {field} differs from FEAT-009 expected order "
                    f"{expected[field]}"
                )
        expected_parameters = expected["parameters"]
        actual_parameters = enemy.get("parameters", {})
        for key, value in expected_parameters.items():
            if actual_parameters.get(key) != value:
                errors.append(
                    f"{enemy_id}: parameter {key} expected {value!r}, "
                    f"got {actual_parameters.get(key)!r}"
                )

    params = enemy.get("parameters", {})
    for key in ("defense_threshold", "opportunistic_capture_min_stones"):
        value = params.get(key)
        if not isinstance(value, int) or value < 0:
            errors.append(f"{enemy_id}: invalid parameter {key}={value!r}")

    if enemy_id == "enemy_invader":
        for key in (
            "minimum_remote_invasion_territory_size",
            "remote_invasion_cooldown_enemy_turns",
            "remote_invasion_max_per_enemy_turn",
        ):
            value = params.get(key)
            if not isinstance(value, int) or value < 1:
                errors.append(f"{enemy_id}: invalid parameter {key}={value!r}")
        if not isinstance(enemy.get("state_rules"), dict):
            errors.append(f"{enemy_id}: state_rules must be an object")

    return errors


def choose_fixture(fixture: dict[str, Any]) -> tuple[str, list[int] | None]:
    candidates = fixture.get("candidates", {})
    for intent_id in fixture.get("execution_priority", []):
        intent_candidates = candidates.get(intent_id, [])
        if not intent_candidates:
            continue
        chosen = min(intent_candidates, key=lambda item: tuple(item["sort_key"]))
        return intent_id, chosen["point"]
    return "pass", None


def main() -> int:
    errors: list[str] = []

    if not SPEC_PATH.exists():
        errors.append("FEAT-009 specification file is missing")

    enemies = load(ENEMIES_PATH)
    if not isinstance(enemies, list):
        errors.append("enemies.json must contain a list")
        enemies = []

    by_id = {enemy.get("id"): enemy for enemy in enemies if isinstance(enemy, dict)}
    for required_enemy in REQUIRED_INTENTS:
        if required_enemy not in by_id:
            errors.append(f"missing required enemy {required_enemy}")
    for enemy in enemies:
        if isinstance(enemy, dict):
            errors.extend(validate_enemy(enemy))

    fixtures = load(FIXTURES_PATH)
    if not isinstance(fixtures, list):
        errors.append("enemy decision fixtures must contain a list")
        fixtures = []
    seen_fixture_ids: set[str] = set()
    for fixture in fixtures:
        fixture_id = fixture.get("id", "<missing-fixture-id>")
        if fixture_id in seen_fixture_ids:
            errors.append(f"duplicate fixture ID {fixture_id}")
        seen_fixture_ids.add(fixture_id)
        enemy_id = fixture.get("enemy_id")
        if enemy_id not in by_id:
            errors.append(f"{fixture_id}: unknown enemy {enemy_id}")
        actual_intent, actual_point = choose_fixture(fixture)
        expected = fixture.get("expected", {})
        if actual_intent != expected.get("intent_id") or actual_point != expected.get("point"):
            errors.append(
                f"{fixture_id}: expected {expected.get('intent_id')} {expected.get('point')}, "
                f"got {actual_intent} {actual_point}"
            )

    expected_fixture_ids = {f"F09-{n:02d}" for n in range(1, 9)}
    missing_fixtures = expected_fixture_ids - seen_fixture_ids
    if missing_fixtures:
        errors.append(f"missing FEAT-009 fixtures {sorted(missing_fixtures)}")

    if errors:
        print("Enemy behavior checks failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    specified_count = sum(
        1 for enemy in enemies if isinstance(enemy, dict) and enemy.get("implementation_status") == "specified"
    )
    print(
        f"Enemy behavior checks passed. {specified_count} specified enemies, "
        f"{len(fixtures)} deterministic fixtures."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
