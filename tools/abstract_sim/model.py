"""Igorogue abstract design-proxy simulator.

This is intentionally NOT the production rules kernel. It explores design-level
relationships between setup, catalysts, player skill, komi, counterattack, and
reward randomness. Never use it to resolve real board positions.
"""
from __future__ import annotations

from dataclasses import dataclass, asdict
from enum import Enum
from random import Random
from statistics import mean
from typing import Iterable, Sequence


class Skill(str, Enum):
    BEGINNER = "beginner"
    INTERMEDIATE = "intermediate"
    EXPERT = "expert"


@dataclass(frozen=True)
class Style:
    id: str
    economy: float
    setup: float
    attack: float
    defense: float
    volatility: float
    preferred_tags: tuple[str, ...]


@dataclass(frozen=True)
class SealLoadout:
    id: str
    komi: int
    economy: float = 0.0
    setup: float = 0.0
    attack: float = 0.0
    defense: float = 0.0
    draw: float = 0.0
    catalyst: float = 0.0
    volatility: float = 0.0
    tags: tuple[str, ...] = ()


@dataclass(frozen=True)
class Enemy:
    id: str
    health: float
    pressure: float
    disruption: float
    counterattack_bias: float
    tags: tuple[str, ...] = ()


@dataclass
class RunMetrics:
    seed: int
    skill: str
    style: str
    loadout: str
    komi: int
    won: bool = False
    battles_won: int = 0
    medium_growth: bool = False
    exploded: bool = False
    early_explosion: bool = False
    setup_explosion: bool = False
    no_explosion_win: bool = False
    no_explosion_loss: bool = False
    first_explosion_battle: int | None = None
    first_explosion_turn: int | None = None
    max_turn_output: float = 0.0
    max_income: float = 0.0
    max_engine: float = 0.0
    final_pressure: float = 0.0
    rewards_taken: int = 0
    rewards_skipped: int = 0
    catalyst_count: int = 0
    setup_count: int = 0

    def to_dict(self) -> dict:
        return asdict(self)


STYLES: dict[str, Style] = {
    "territory": Style("territory", 1.25, 1.10, 0.78, 1.00, 0.18, ("territory", "facility", "conversion")),
    "attack": Style("attack", 0.72, 0.75, 1.35, 0.82, 0.30, ("capture", "contact", "tempo")),
    "thickness": Style("thickness", 0.88, 1.28, 0.94, 1.28, 0.16, ("center", "shape", "delayed")),
    "sacrifice": Style("sacrifice", 0.82, 1.05, 1.02, 0.72, 0.42, ("sacrifice", "draw", "recovery")),
}

LOADOUTS: dict[str, SealLoadout] = {
    "none": SealLoadout("none", 0),
    "territory_4": SealLoadout("territory_4", 4, economy=0.35, setup=0.20, draw=0.05, tags=("territory", "facility")),
    "attack_6": SealLoadout("attack_6", 6, attack=0.42, draw=0.20, catalyst=0.18, volatility=0.10, tags=("capture", "tempo")),
    "thickness_3": SealLoadout("thickness_3", 3, setup=0.34, defense=0.28, tags=("center", "shape")),
    "sacrifice_4": SealLoadout("sacrifice_4", 4, setup=0.20, draw=0.30, catalyst=0.10, volatility=0.16, tags=("sacrifice", "draw")),
    "growth_5": SealLoadout("growth_5", 5, economy=0.20, setup=0.34, catalyst=0.10, volatility=0.08, tags=("delayed", "facility")),
    "high_komi_9": SealLoadout("high_komi_9", 9, attack=0.40, setup=0.22, draw=0.25, catalyst=0.28, volatility=0.24, tags=("capture", "center", "tempo")),
}

ENEMIES: tuple[Enemy, ...] = (
    Enemy("bandit", 7.5, 0.54, 0.11, 0.05, ("attack",)),
    Enemy("invader", 8.7, 0.59, 0.24, 0.07, ("territory_hate",)),
    Enemy("merchant", 10.0, 0.54, 0.15, 0.09, ("scaling",)),
    Enemy("cutter", 13.2, 0.73, 0.32, 0.14, ("shape_hate",)),
)

SKILL = {
    Skill.BEGINNER: dict(plan=0.12, conversion=0.56, defense=0.68, reward=0.45, skip=0.10),
    Skill.INTERMEDIATE: dict(plan=0.48, conversion=0.78, defense=0.82, reward=0.72, skip=0.28),
    Skill.EXPERT: dict(plan=0.78, conversion=0.92, defense=0.91, reward=0.88, skip=0.48),
}

REWARD_TAGS = (
    "territory", "facility", "conversion", "capture", "contact", "tempo",
    "center", "shape", "delayed", "sacrifice", "draw", "recovery", "defense", "wild",
)


def _clamp(value: float, lo: float, hi: float) -> float:
    return max(lo, min(hi, value))


def _weighted_choice(rng: Random, pairs: Sequence[tuple[str, float]]) -> str:
    total = sum(max(0.0, weight) for _, weight in pairs)
    if total <= 0:
        return pairs[-1][0]
    pick = rng.random() * total
    acc = 0.0
    for value, weight in pairs:
        acc += max(0.0, weight)
        if pick <= acc:
            return value
    return pairs[-1][0]


def _reward_offer(rng: Random, preferred: set[str], owned: set[str]) -> list[str]:
    """Three-slot soft guidance: current engine, style, and wild are weights, not guarantees."""
    offer: list[str] = []
    for slot in range(3):
        pairs: list[tuple[str, float]] = []
        for tag in REWARD_TAGS:
            weight = 1.0
            if tag in preferred:
                weight *= 2.35 if slot < 2 else 1.25
            if tag in owned:
                weight *= 1.35
            if tag == "wild":
                weight *= 1.8 if slot == 2 else 0.55
            pairs.append((tag, weight))
        choice = _weighted_choice(rng, pairs)
        offer.append(choice)
    return offer


def _choose_reward(rng: Random, offer: list[str], preferred: set[str], owned: set[str], skill: Skill) -> str | None:
    s = SKILL[skill]
    scores: list[tuple[str, float]] = []
    for tag in offer:
        score = rng.uniform(0.0, 0.45)
        if tag in preferred:
            score += 1.15 * s["reward"]
        if tag in owned:
            score += 0.42 * s["reward"]
        if tag in {"conversion", "tempo", "draw"} and owned:
            score += 0.25 * s["reward"]
        if tag == "wild":
            score += rng.uniform(-0.2, 0.65)
        scores.append((tag, score))
    scores.sort(key=lambda x: x[1], reverse=True)
    best, best_score = scores[0]
    skip_threshold = 0.40 + 0.36 * s["skip"]
    if best_score < skip_threshold and rng.random() < s["skip"]:
        return None
    return best


def _apply_reward(tag: str, state: dict[str, float], metrics: RunMetrics) -> None:
    if tag in {"territory", "facility"}:
        state["economy"] += 0.20
        state["setup"] += 0.13
        metrics.setup_count += 1
    elif tag in {"center", "shape", "delayed"}:
        state["setup"] += 0.24
        state["defense"] += 0.08
        metrics.setup_count += 1
    elif tag in {"capture", "contact"}:
        state["attack"] += 0.20
        state["catalyst"] += 0.08
    elif tag == "tempo":
        state["attack"] += 0.12
        state["draw"] += 0.13
        state["catalyst"] += 0.10
        metrics.catalyst_count += 1
    elif tag == "conversion":
        state["conversion"] += 0.22
        state["catalyst"] += 0.13
        metrics.catalyst_count += 1
    elif tag in {"draw", "recovery"}:
        state["draw"] += 0.20
        state["defense"] += 0.05
    elif tag == "sacrifice":
        state["setup"] += 0.10
        state["draw"] += 0.14
        state["volatility"] += 0.10
        metrics.setup_count += 1
    elif tag == "defense":
        state["defense"] += 0.22
    else:
        # Wild cards are more variable and can be spectacular or awkward.
        state["attack"] += 0.08
        state["setup"] += 0.08
        state["volatility"] += 0.12


def simulate_run(seed: int, skill: Skill, style_id: str, loadout_id: str) -> RunMetrics:
    rng = Random(seed)
    style = STYLES[style_id]
    loadout = LOADOUTS[loadout_id]
    s = SKILL[skill]
    metrics = RunMetrics(seed, skill.value, style_id, loadout_id, loadout.komi)

    state: dict[str, float] = {
        "economy": style.economy + loadout.economy,
        "setup": style.setup + loadout.setup,
        "attack": style.attack + loadout.attack,
        "defense": style.defense + loadout.defense,
        "draw": 0.90 + loadout.draw,
        "conversion": 0.75,
        "catalyst": 0.14 + loadout.catalyst,
        "volatility": style.volatility + loadout.volatility,
        "engine": 0.0,
        "income": 0.0,
        "pressure": 0.0,
        "heat": 0.0,
    }
    preferred = set(style.preferred_tags) | set(loadout.tags)
    owned: set[str] = set()

    for battle_index, enemy in enumerate(ENEMIES, start=1):
        enemy_health = enemy.health + battle_index * 0.58 + 0.21 * loadout.komi
        enemy_pressure = enemy.pressure + 0.025 * loadout.komi
        counter_rate = enemy.counterattack_bias + max(0, loadout.komi - 4) * 0.018
        progress = 0.0
        state["pressure"] = max(0.0, state["pressure"] * 0.25)
        recent_outputs: list[float] = []
        setup_events: list[int] = []
        battle_won = False

        for turn in range(1, 17):
            # Board opportunity is deliberately only partly controllable.
            opportunity = rng.betavariate(2.0 + 0.45 * s["plan"], 2.4)
            capture_window = rng.random() < _clamp(0.16 + 0.10 * state["attack"] + 0.08 * opportunity, 0.08, 0.72)
            territory_window = rng.random() < _clamp(0.18 + 0.11 * state["economy"] + 0.06 * opportunity, 0.08, 0.75)
            disruption = enemy.disruption * rng.uniform(0.65, 1.35)

            # Skilled play improves sequencing and conversion, not raw luck.
            plan = s["plan"] * rng.uniform(0.78, 1.12)
            setup_action = (state["setup"] + state["economy"]) * (0.28 + 0.25 * plan)
            if territory_window:
                income_gain = 0.30 + 0.18 * state["economy"] + rng.uniform(0.0, 0.18)
                state["income"] += income_gain
                state["engine"] += 0.30 + 0.12 * state["setup"]
                setup_events.append(turn)
            else:
                state["engine"] += 0.08 * setup_action

            # Delayed setup can mature at irregular times.
            delayed_mature = rng.random() < _clamp(0.05 + 0.035 * state["setup"] + 0.02 * len(setup_events), 0.04, 0.35)
            if delayed_mature:
                state["engine"] += rng.uniform(0.18, 0.48) * state["setup"]
                state["draw"] += 0.04
                setup_events.append(turn)

            state["engine"] = max(0.0, state["engine"] - disruption * 0.18)
            state["income"] = max(0.0, state["income"] - disruption * 0.10)

            available = 3.0 + state["income"] + state["draw"] * rng.uniform(0.7, 1.15)
            conversion = state["conversion"] * s["conversion"]
            attack_output = state["attack"] * (0.48 + 0.20 * opportunity) * available * 0.31

            setup_last_three = sum(1 for t in setup_events if turn - 3 <= t <= turn)
            catalyst_window = (state["catalyst"] >= 0.22 or state["engine"] >= 3.6) and capture_window and rng.random() < _clamp(
                0.055 + 0.070 * state["catalyst"] + 0.045 * state["engine"] + 0.065 * plan,
                0.04, 0.68,
            )
            # Explosion requires both opportunity and accumulated engine. Variance remains.
            burst_roll = (
                state["engine"] * (0.44 + 0.20 * state["catalyst"])
                + state["income"] * 0.22
                + setup_last_three * 0.28
                + rng.gauss(0.0, 0.55 + state["volatility"])
            )
            burst_threshold = 2.75 + (0.25 if battle_index <= 2 else 0.0) + 0.06 * battle_index + 0.06 * loadout.komi
            explosive = catalyst_window and burst_roll > burst_threshold

            pre_burst_engine = state["engine"]
            if explosive:
                burst = (2.6 + burst_roll * 0.75) * (0.72 + conversion)
                turn_output = attack_output + burst
                state["engine"] *= 0.48
                state["heat"] += 0.8 + max(0, loadout.komi - 6) * 0.12
            else:
                turn_output = attack_output * (0.88 + conversion * 0.62)

            progress += turn_output
            metrics.max_turn_output = max(metrics.max_turn_output, turn_output)
            metrics.max_income = max(metrics.max_income, state["income"])
            metrics.max_engine = max(metrics.max_engine, state["engine"])

            recent_avg = mean(recent_outputs[-2:]) if recent_outputs[-2:] else max(1.0, turn_output * 0.72)
            true_explosion = explosive and turn_output >= 7.0 and turn_output >= recent_avg * 1.65
            if true_explosion and not metrics.exploded:
                metrics.exploded = True
                metrics.first_explosion_battle = battle_index
                metrics.first_explosion_turn = turn
                metrics.early_explosion = battle_index <= 2 or (battle_index == 3 and turn <= 4)
                metrics.setup_explosion = setup_last_three >= 2 or (skill is Skill.EXPERT and pre_burst_engine >= 2.8) or (skill is Skill.INTERMEDIATE and pre_burst_engine >= 3.5)
            recent_outputs.append(turn_output)

            # Pressure rises with time, komi and heat; defense and good play reduce it.
            pressure_gain = enemy_pressure * rng.uniform(1.05, 1.50)
            pressure_gain += counter_rate * turn * 0.62 + state["heat"] * 0.23
            pressure_gain += max(0.0, state["attack"] - 1.45) * 0.08  # overextension
            pressure_mitigation = state["defense"] * s["defense"] * rng.uniform(0.20, 0.32)
            state["pressure"] += max(0.08, pressure_gain - pressure_mitigation)

            if progress >= enemy_health:
                battle_won = True
                metrics.battles_won += 1
                break
            defeat_limit = 8.2 + state["defense"] * 1.35 - 0.10 * loadout.komi
            if state["pressure"] >= defeat_limit:
                battle_won = False
                break

        if not battle_won:
            metrics.final_pressure = state["pressure"]
            metrics.no_explosion_loss = not metrics.exploded
            return metrics

        # Battle reward and mild recovery.
        state["defense"] += 0.04
        state["pressure"] *= 0.25
        if battle_index < len(ENEMIES):
            offer = _reward_offer(rng, preferred, owned)
            chosen = _choose_reward(rng, offer, preferred, owned, skill)
            if chosen is None:
                metrics.rewards_skipped += 1
                state["draw"] += 0.025
            else:
                metrics.rewards_taken += 1
                owned.add(chosen)
                preferred.add(chosen)
                _apply_reward(chosen, state, metrics)

    metrics.won = metrics.battles_won == len(ENEMIES)
    metrics.medium_growth = metrics.max_income >= 1.8 or metrics.max_engine >= 2.3 or metrics.max_turn_output >= 5.0
    metrics.no_explosion_win = metrics.won and not metrics.exploded
    metrics.no_explosion_loss = (not metrics.won) and not metrics.exploded
    metrics.final_pressure = state["pressure"]
    return metrics


def summarize(results: Iterable[RunMetrics]) -> dict[str, float]:
    rows = list(results)
    if not rows:
        return {}
    n = len(rows)
    pct = lambda predicate: 100.0 * sum(1 for r in rows if predicate(r)) / n
    exploded = [r for r in rows if r.exploded]
    return {
        "runs": n,
        "win_rate_pct": pct(lambda r: r.won),
        "medium_growth_pct": pct(lambda r: r.medium_growth),
        "true_explosion_pct": pct(lambda r: r.exploded),
        "early_explosion_pct": pct(lambda r: r.early_explosion),
        "setup_share_of_explosions_pct": (100.0 * sum(1 for r in exploded if r.setup_explosion) / len(exploded)) if exploded else 0.0,
        "no_explosion_win_pct": pct(lambda r: r.no_explosion_win),
        "no_explosion_loss_pct": pct(lambda r: r.no_explosion_loss),
        "mean_max_output": mean(r.max_turn_output for r in rows),
        "mean_max_income": mean(r.max_income for r in rows),
        "mean_battles_won": mean(r.battles_won for r in rows),
    }
