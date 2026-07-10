from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "verify_toolchain", ROOT / "tools/verify_toolchain.py")
assert SPEC and SPEC.loader
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class ToolchainVersionTests(unittest.TestCase):
    def test_accepts_pinned_dotnet(self) -> None:
        self.assertEqual("8.0.422", MODULE.validate_dotnet_version("8.0.422\n", "8.0.422"))

    def test_rejects_wrong_dotnet_patch(self) -> None:
        with self.assertRaises(MODULE.VerificationError):
            MODULE.validate_dotnet_version("8.0.421\n", "8.0.422")

    def test_accepts_godot_dotnet_build(self) -> None:
        output = "4.7.stable.mono.official.abc123"
        self.assertEqual(output, MODULE.validate_godot_version(output, "4.7-stable"))

    def test_rejects_standard_non_dotnet_editor(self) -> None:
        with self.assertRaises(MODULE.VerificationError):
            MODULE.validate_godot_version("4.7.stable.official.abc123", "4.7-stable")

    def test_rejects_wrong_godot_release(self) -> None:
        with self.assertRaises(MODULE.VerificationError):
            MODULE.validate_godot_version("4.7.1.rc2.mono.official.abc123", "4.7-stable")


if __name__ == "__main__":
    unittest.main()
