from __future__ import annotations

import os
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class PosixWrapperTests(unittest.TestCase):
    def assert_default_dotnet_name_is_not_exported(
        self,
        environment: dict[str, str],
    ) -> None:
        result = subprocess.run(
            [
                "sh",
                "-c",
                '. tools/dev/_common.sh; '
                'test "$DOTNET_BIN" = dotnet; '
                'if printenv DOTNET_BIN >/dev/null 2>&1; then exit 1; fi',
            ],
            cwd=ROOT,
            env=environment,
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(
            0,
            result.returncode,
            msg=f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}",
        )

    def test_unset_dotnet_name_is_not_exported_to_strict_verifier(self) -> None:
        environment = os.environ.copy()
        environment.pop("DOTNET_BIN", None)
        self.assert_default_dotnet_name_is_not_exported(environment)

    def test_empty_dotnet_name_is_not_exported_to_strict_verifier(self) -> None:
        environment = os.environ.copy()
        environment["DOTNET_BIN"] = ""
        self.assert_default_dotnet_name_is_not_exported(environment)

    def test_explicit_dotnet_path_remains_exported(self) -> None:
        environment = os.environ.copy()
        environment["DOTNET_BIN"] = "/explicit/dotnet"
        result = subprocess.run(
            [
                "sh",
                "-c",
                '. tools/dev/_common.sh; '
                'test "$DOTNET_BIN" = /explicit/dotnet; '
                "sh -c 'test \"$DOTNET_BIN\" = /explicit/dotnet'",
            ],
            cwd=ROOT,
            env=environment,
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(
            0,
            result.returncode,
            msg=f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}",
        )


if __name__ == "__main__":
    unittest.main()
