from __future__ import annotations

import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class PowerShellWrapperTests(unittest.TestCase):
    def test_checked_native_helper_throws_on_nonzero_exit(self) -> None:
        helper = (ROOT / "tools/dev/_Common.ps1").read_text(encoding="utf-8")
        self.assertIn("function Invoke-CheckedNative", helper)
        self.assertIn("$LASTEXITCODE", helper)
        self.assertIn("throw", helper)

    def test_godot_wrappers_use_checked_native_invocations(self) -> None:
        for relative in ("tools/dev/godot-smoke.ps1", "tools/dev/export-windows.ps1"):
            with self.subTest(wrapper=relative):
                text = (ROOT / relative).read_text(encoding="utf-8")
                self.assertIn("Invoke-CheckedNative", text)
                direct_native_lines = [
                    line
                    for line in text.splitlines()
                    if line.strip().startswith(("& $PythonBin", "& $env:GODOT_BIN", "& git"))
                ]
                self.assertEqual([], direct_native_lines)


if __name__ == "__main__":
    unittest.main()
