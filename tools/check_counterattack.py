#!/usr/bin/env python3
"""Validate FEAT-003 counterattack data, curve, fixtures, and docs.

This is a specification checker, not the product Rules Kernel. M1 must port the
same fixtures to the shared kernel and golden replay suite.
"""
from __future__ import annotations

import json
import math
from dataclasses import dataclass
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
SYSTEM_PATH = ROOT / "game_data" / "balance" / "system.json"
STYLES_PATH = ROOT / "game_data" / "content" / "styles.json"
FIXTURES_PATH = ROOT / "game_data" / "fixtures" / "counterattack_curve_fixtures.json"
RULES_PATH = ROOT / "docs" / "20_Design" / "Rules Canon.md"
FEATURE_PATH = ROOT / "docs" / "20_Design" / "Feature Specs" / "FEAT-003 Komi Counterattack and Heat.md"
ADR_PATH = ROOT / "docs" / "60_Decisions" / "ADRs" / "ADR-0013 Baseline Pace and Burst-Driven Counterattack.md"
BAL_PATH = ROOT / "docs" / "50_Validation" / "Balance Changes" / "BAL-0001 Counterattack Curve v0.2.6.md"
FIXTURE_DOC_PATH = ROOT / "docs" / "50_Validation" / "Spec Fixtures" / "FEAT-003 Counterattack Curve Fixtures.md"
OLD_ADR_PATH = ROOT / "docs" / "60_Decisions" / "ADRs" / "ADR-0008 Setup Ignition Counterattack Curve.md"

EXPECTED_CURVE_IDS = {f"CTR-{i:02d}" for i in range(1, 11)}
EXPECTED_EVENT_IDS = {f"CTR-{i:02d}" for i in range(11, 26)}


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


@dataclass
class State:
    gauge: int
    pending: bool


def maybe_prime(state: State, threshold: int, battle_active: bool = True) -> tuple[State, bool]:
    if not battle_active or state.pending or state.gauge < threshold:
        return state, False
    return State(state.gauge - threshold, True), True


def advance(state: State, delta: int, threshold: int, battle_active: bool = True) -> tuple[State, bool]:
    if not battle_active:
        return state, False
    return maybe_prime(State(state.gauge + delta, state.pending), threshold, battle_active)


def heat_delta(config: dict[str, Any], komi: int, before: float, after: float, used: bool) -> tuple[int, bool]:
    heat = config["heat"]
    crossed = before < heat["brilliant_crossing_threshold"] <= after
    if used or not crossed:
        return 0, used
    return heat["base_units"] + heat["per_komi_units"] * komi, True


def attack_sequence(config: dict[str, Any], style_id: str, count: int) -> dict[str, Any]:
    data = config["overextension"]
    if style_id != data["style_id"]:
        return {"total_delta_units": 0, "counted_cards": 0, "capped": False}
    excess = max(0, count - data["free_successful_cards_per_player_turn"])
    raw = excess * data["gain_units_per_excess_card"]
    total = min(raw, data["per_player_turn_cap_units"])
    return {"total_delta_units": total, "counted_cards": count, "capped": raw > total}


def sacrifice_capture(config: dict[str, Any], style_id: str, remainder: int, captured: int) -> dict[str, Any]:
    data = config["sacrifice"]
    if style_id != data["style_id"]:
        return {"delta_units": 0, "end_remainder": remainder, "batches": 0}
    total = remainder + captured
    batches, end_remainder = divmod(total, data["non_king_black_stones_per_batch"])
    return {
        "delta_units": batches * data["gain_units_per_batch"],
        "end_remainder": end_remainder,
        "batches": batches,
    }


def validate_subset(fid: str, actual: dict[str, Any], expected: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    for key, value in expected.items():
        if actual.get(key) != value:
            errors.append(f"{fid}: {key} expected {value!r}, got {actual.get(key)!r}")
    return errors


def main() -> int:
    errors: list[str] = []
    system = load_json(SYSTEM_PATH)
    config = system.get("counterattack", {})
    fixtures = load_json(FIXTURES_PATH)
    styles = {item["id"]: item for item in load_json(STYLES_PATH)}

    expected_config = {
        "version": "1.0.0",
        "gauge_unit_scale": 2,
        "display_threshold_points": 100,
        "threshold_units": 200,
    }
    for key, value in expected_config.items():
        if config.get(key) != value:
            errors.append(f"counterattack.{key} expected {value!r}, got {config.get(key)!r}")

    if config.get("battle_start", {}).get("base_units") != 20 or config.get("battle_start", {}).get("per_komi_units") != 4:
        errors.append("counterattack battle_start formula must be 20 + 4K units")
    if config.get("enemy_turn_end_gain", {}).get("base_units") != 12 or config.get("enemy_turn_end_gain", {}).get("per_komi_units") != 1:
        errors.append("counterattack enemy-turn gain formula must be 12 + K units")
    heat = config.get("heat", {})
    if heat.get("enabled_for_all_komi") is not True or heat.get("base_units") != 48 or heat.get("per_komi_units") != 4:
        errors.append("heat must be enabled for all komi with 48 + 4K units")
    if heat.get("brilliant_crossing_threshold") != 3.0 or heat.get("per_player_turn_cap") != 1:
        errors.append("heat crossing threshold/cap mismatch")

    if "overextension_counterattack" not in styles.get("style_attack", {}).get("rules", []):
        errors.append("style_attack missing overextension_counterattack rule")
    if "sacrifice_counterattack" not in styles.get("style_sacrifice", {}).get("rules", []):
        errors.append("style_sacrifice missing sacrifice_counterattack rule")

    curve = fixtures.get("komi_curve", [])
    curve_ids = {row.get("id") for row in curve}
    if curve_ids != EXPECTED_CURVE_IDS:
        errors.append(f"curve fixture IDs mismatch: {sorted(curve_ids)}")
    threshold = config.get("threshold_units", 0)
    for row in curve:
        komi = row.get("komi")
        if not isinstance(komi, int) or not 0 <= komi <= 9:
            errors.append(f"{row.get('id')}: invalid komi {komi!r}")
            continue
        start = config["battle_start"]["base_units"] + config["battle_start"]["per_komi_units"] * komi
        gain = config["enemy_turn_end_gain"]["base_units"] + config["enemy_turn_end_gain"]["per_komi_units"] * komi
        turns = math.ceil((threshold - start) / gain)
        actual = {
            "expected_start_units": start,
            "expected_start_points": start / config["gauge_unit_scale"],
            "expected_enemy_turn_end_gain_units": gain,
            "expected_enemy_turn_end_gain_points": gain / config["gauge_unit_scale"],
            "expected_threshold_cross_after_enemy_turn_end": turns,
            "expected_first_bonus_enemy_turn": turns + 1,
        }
        errors.extend(validate_subset(row.get("id", "?"), actual, {k: row[k] for k in actual}))
        if turns + 1 > 20:
            errors.append(f"{row.get('id')}: first bonus enemy turn exceeds 20")

    events = fixtures.get("event_fixtures", [])
    event_ids = {row.get("id") for row in events}
    if event_ids != EXPECTED_EVENT_IDS:
        errors.append(f"event fixture IDs mismatch: {sorted(event_ids)}")

    for row in events:
        fid = row.get("id", "?")
        op = row.get("operation")
        expected = row.get("expected", {})
        if op == "heat_crossing":
            delta, used = heat_delta(config, row["komi"], row["multiplier_before"], row["multiplier_after"], row["heat_used"])
            end, created = advance(State(row["start_gauge_units"], row["pending"]), delta, threshold)
            actual = {"delta_units": delta, "end_gauge_units": end.gauge, "pending": end.pending, "heat_used": used}
        elif op == "attack_sequence":
            actual = attack_sequence(config, row["style_id"], row["successful_attack_cards"])
        elif op == "sacrifice_capture":
            actual = sacrifice_capture(config, row["style_id"], row["start_remainder"], row["captured_non_king_black_stones"])
        elif op == "advance":
            end, created = advance(State(row["start_gauge_units"], row["pending"]), row["delta_units"], threshold, row.get("battle_active", True))
            actual = {"end_gauge_units": end.gauge, "pending": end.pending, "pending_created": created}
        elif op == "resolve_pending":
            if not row["pending"]:
                actual = {"error": "no_pending"}
            else:
                state = State(row["start_gauge_units"], False)
                state, created = maybe_prime(state, threshold)
                actual = {
                    "end_gauge_units": state.gauge,
                    "pending": state.pending,
                    "next_turn_pending_created": created,
                    "bonus_actions_this_enemy_turn": 1,
                }
        elif op == "enemy_turn_end":
            delta = config["enemy_turn_end_gain"]["base_units"] + config["enemy_turn_end_gain"]["per_komi_units"] * row["komi"]
            end, created = advance(State(row["start_gauge_units"], row["pending"]), delta, threshold, row["battle_active"])
            actual = {"delta_units": delta, "end_gauge_units": end.gauge, "pending": end.pending, "pending_created": created}
        elif op == "battle_start":
            komi = row["komi"]
            actual = {
                "end_gauge_units": config["battle_start"]["base_units"] + config["battle_start"]["per_komi_units"] * komi,
                "pending": False,
                "heat_used": False,
                "attack_cards_counted": 0,
                "overextension_units_this_turn": 0,
                "sacrifice_remainder": 0,
            }
        else:
            errors.append(f"{fid}: unknown operation {op!r}")
            continue
        errors.extend(validate_subset(fid, actual, expected))

    required_docs = {
        RULES_PATH: ["version: 0.2.7", "2 units", "24 + 2K", "[[FEAT-003 Komi Counterattack and Heat]]"],
        FEATURE_PATH: ["status: accepted", "CTR-01", "Pending", "overflow"],
        ADR_PATH: ["status: accepted", "supersedes: ADR-0008"],
        BAL_PATH: ["status: accepted", "evidence_level: E1-desk-calculation"],
        FIXTURE_DOC_PATH: ["CTR-25", "counterattack_curve_fixtures.json"],
        OLD_ADR_PATH: ["status: superseded", "superseded_by: ADR-0013"],
    }
    for path, needles in required_docs.items():
        if not path.exists():
            errors.append(f"missing document: {path.relative_to(ROOT)}")
            continue
        text = path.read_text(encoding="utf-8")
        for needle in needles:
            if needle not in text:
                errors.append(f"{path.relative_to(ROOT)} missing {needle!r}")

    if errors:
        print("Counterattack checks failed:")
        for error in errors:
            print(f"- {error}")
        return 1
    print("Counterattack checks passed — 10 komi curve rows, 15 event fixtures")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
