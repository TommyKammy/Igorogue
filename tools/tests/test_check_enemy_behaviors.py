from __future__ import annotations

import copy
import importlib.util
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "check_enemy_behaviors",
    ROOT / "tools/check_enemy_behaviors.py",
)
assert SPEC and SPEC.loader
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


class EnemyActionBudgetSchemaTests(unittest.TestCase):
    def test_runtime_budget_contract_is_derived_from_enemy_schema(self) -> None:
        schema = MODULE.load(MODULE.ENEMY_SCHEMA_PATH)

        self.assertEqual(
            {
                "normal_actions": 1,
                "counterattack_bonus_actions": 1,
                "max_actions_per_enemy_turn": 2,
            },
            MODULE.action_budget_contract(schema),
        )

    def test_schema_invalid_action_budget_is_rejected_without_code_constants(self) -> None:
        schema = MODULE.load(MODULE.ENEMY_SCHEMA_PATH)
        contract = MODULE.action_budget_contract(schema)
        bandit = copy.deepcopy(MODULE.load(MODULE.ENEMIES_PATH)[0])
        bandit["action_budget"] = {
            "normal_actions": 2,
            "counterattack_bonus_actions": 0,
            "max_actions_per_enemy_turn": 2,
        }

        errors = MODULE.validate_enemy(bandit, contract)

        self.assertTrue(any("unexpected action_budget" in error for error in errors))

    def test_schema_change_updates_validation_contract_without_code_change(self) -> None:
        schema = copy.deepcopy(MODULE.load(MODULE.ENEMY_SCHEMA_PATH))
        properties = schema["items"]["properties"]["action_budget"]["properties"]
        properties["normal_actions"]["const"] = 2
        properties["counterattack_bonus_actions"]["const"] = 0
        bandit = copy.deepcopy(MODULE.load(MODULE.ENEMIES_PATH)[0])
        bandit["action_budget"] = {
            "normal_actions": 2,
            "counterattack_bonus_actions": 0,
            "max_actions_per_enemy_turn": 2,
        }

        errors = MODULE.validate_enemy(bandit, MODULE.action_budget_contract(schema))

        self.assertFalse(any("unexpected action_budget" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
