from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "check_codex_handoff", ROOT / "tools/check_codex_handoff.py"
)
assert SPEC and SPEC.loader
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class HandoffGateStatusTests(unittest.TestCase):
    def test_allows_runtime_gate_in_review(self) -> None:
        self.assertEqual(
            [], MODULE.validate_gate_statuses("review", "review", "blocked", "blocked")
        )

    def test_unblocks_next_task_only_after_all_three_closures(self) -> None:
        self.assertEqual(
            [], MODULE.validate_gate_statuses("done", "done", "done", "ready")
        )
        self.assertTrue(
            MODULE.validate_gate_statuses("done", "done", "review", "ready")
        )

    def test_allows_next_task_normal_lifecycle_after_runtime_closure(self) -> None:
        for task_2_status in ("ready", "in_progress", "review", "done"):
            with self.subTest(task_2_status=task_2_status):
                self.assertEqual(
                    [],
                    MODULE.validate_gate_statuses(
                        "done", "done", "done", task_2_status
                    ),
                )

    def test_keeps_next_task_blocked_before_runtime_closure(self) -> None:
        self.assertTrue(
            MODULE.validate_gate_statuses("review", "review", "blocked", "ready")
        )


if __name__ == "__main__":
    unittest.main()
