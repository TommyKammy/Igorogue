from __future__ import annotations

import importlib.util
import json
import tempfile
import unittest
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("content_sync", ROOT / "tools/content_sync.py")
assert SPEC and SPEC.loader
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


class ContentSyncTests(unittest.TestCase):
    def test_key_order_does_not_change_content_hash(self) -> None:
        first = MODULE.canonical_json_bytes({"b": 2, "a": 1})
        second = MODULE.canonical_json_bytes({"a": 1, "b": 2})
        self.assertEqual(first, second)

    def test_path_and_content_boundaries_affect_hash(self) -> None:
        file_type = MODULE.CanonicalFile
        one = [file_type("content/a.json", b"{}\n", "sha256:x")]
        two = [file_type("content/b.json", b"{}\n", "sha256:x")]
        self.assertNotEqual(MODULE.calculate_content_hash(one), MODULE.calculate_content_hash(two))

    def test_write_and_check_round_trip(self) -> None:
        files = [MODULE.CanonicalFile("content/a.json", b"{}\n", "sha256:x")]
        expected = MODULE.expected_output(files)
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory)
            MODULE.write_output(output, expected)
            self.assertEqual([], MODULE.check_output(output, expected))


if __name__ == "__main__":
    unittest.main()
