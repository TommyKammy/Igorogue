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

    def test_all_dev_wrappers_use_checked_native_invocations(self) -> None:
        wrapper_paths = sorted((ROOT / "tools/dev").glob("*.ps1"))
        wrapper_paths.remove(ROOT / "tools/dev/_Common.ps1")

        for path in wrapper_paths:
            relative = path.relative_to(ROOT).as_posix()
            with self.subTest(wrapper=relative):
                text = path.read_text(encoding="utf-8")
                self.assertIn("Invoke-CheckedNative", text)
                direct_native_lines = [
                    line
                    for line in text.splitlines()
                    if line.strip().startswith(
                        ("& $PythonBin", "& $DotnetBin", "& $env:GODOT_BIN", "& git")
                    )
                ]
                self.assertEqual([], direct_native_lines)


if __name__ == "__main__":
    unittest.main()
