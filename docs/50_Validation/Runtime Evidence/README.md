---
type: validation-index
status: active
project: Igorogue
updated: 2026-07-10
---
# Runtime Evidence

Store machine-executed build, test, simulator, Godot smoke, and export evidence here.

## Privacy

Redact:

- usernames;
- home-directory absolute paths;
- machine serial numbers;
- tokens and credentials;
- private remote URLs when not required.

Use placeholders such as `<REPO_ROOT>`, `<GODOT_BIN>`, and `<TEMP_DIR>`.

## Required identity

Every report includes:

- TASK ID;
- Git commit;
- game/schema version;
- content hash;
- host architecture and macOS version;
- exact tool versions;
- exact command and exit code;
- artifact hashes;
- known gaps.
