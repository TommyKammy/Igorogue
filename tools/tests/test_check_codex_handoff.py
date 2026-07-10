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
        self.assertEqual([], MODULE.validate_gate_statuses("review", "review", "blocked"))

    def test_unblocks_next_task_only_after_both_closures(self) -> None:
        self.assertEqual([], MODULE.validate_gate_statuses("done", "done", "ready"))
        self.assertTrue(MODULE.validate_gate_statuses("done", "review", "ready"))

    def test_keeps_next_task_blocked_before_runtime_closure(self) -> None:
        self.assertTrue(MODULE.validate_gate_statuses("review", "review", "ready"))


if __name__ == "__main__":
    unittest.main()
