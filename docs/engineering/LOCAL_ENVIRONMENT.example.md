# Local Environment (example template)

| Field | Value |
|---|---|
| ID | DOC-LOCAL-ENV |
| Document status | Draft |
| Purpose | Neutral template for recording machine-specific values discovered in TASK-0001 |
| Responsible role | The Implementer fills in the local copy; the project owner reviews it |
| Related documents | [WINDOWS_SETUP](WINDOWS_SETUP.md), [TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md), [SECURITY.md](../../SECURITY.md), [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) |
| Last updated | 2026-10-18 |

## How to use this template

1. Copy this file to **`.local/LOCAL_ENVIRONMENT.md`** in the project root. That folder is intended to be git-ignored: confirm that the ignore rule exists before creating the file.
2. Fill in the copy only, never this example. Keep every placeholder here neutral.
3. **Never commit** real paths outside the project root, account names, store IDs, license keys, tokens, or machine names. If a value must appear in shared documentation, record a sanitized form in [WINDOWS_SETUP](WINDOWS_SETUP.md) §2 and versions in [TOOLCHAIN_MATRIX](TOOLCHAIN_MATRIX.md).
4. Every value records **when** and **how** it was obtained: a command labeled as in [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) §11, or "owner statement".
5. OneDrive syncs the project root. The owner decides whether `.local/` is acceptable inside the synced folder (OPEN-028).

---

## A. Machine and workspace

| Field | Value | Date | Obtained by |
|---|---|---|---|
| Windows edition and build | `<WINDOWS_EDITION_AND_BUILD>` | `<YYYY-MM-DD>` | `<command or owner statement>` |
| Project root | `C:\Users\Michel\OneDrive\Documentos\Jogos\DriveTheCityII` (fixed) | — | Owner brief |
| OneDrive state of the project folder | `<ALWAYS_AVAILABLE / ON_DEMAND / UNKNOWN>` | | |
| Free space on the project drive | `<FREE_SPACE>` | | |
| Build output location decision | `<INSIDE_ROOT_ARTIFACTS / OUTSIDE: BUILD_OUTPUT_DIR>` | | Owner decision (OPEN-028) |
| `<BUILD_OUTPUT_DIR>` (if outside the root) | `<BUILD_OUTPUT_DIR>` | | |

## B. Cities: Skylines II

| Field | Value | Date | Obtained by |
|---|---|---|---|
| Store platform | `<STORE_PLATFORM>` | | Owner statement |
| Edition | `<EDITION>` | | |
| Game version (as shown by the game or store) | `<CS2_VERSION>` | | |
| Installed DLCs (names only) | `<DLC_LIST>` | | |
| Installation folder | `<CS2_INSTALL_DIR>` | | |
| Managed assemblies folder (build references) | `<CS2_MANAGED_DIR>` | | `CSII_MANAGEDPATH`, if the toolchain is installed |
| User-data folder | `<CS2_USER_DATA_DIR>` | | |
| Local mods folder | `<CS2_LOCAL_MODS_DIR>` | | `CSII_LOCALMODSPATH`, if present |
| Game log folder | `<CS2_LOG_DIR>` | | To be discovered |
| Code-mod support confirmed for this edition | `<YES / NO / UNKNOWN>` | | OPEN-033 |

## C. Official modding toolchain

| Field | Value | Date | Obtained by |
|---|---|---|---|
| Toolchain installed | `<YES / NO / UNKNOWN>` | | |
| `CSII_*` variables present (names only) | `<NAMES>` | | |
| `CSII_TOOLPATH` | `<CSII_TOOLPATH>` | | |
| Target framework in Mod.props (read-only) | `<TFM>` | | OPEN-003 |
| C# version in Mod.props (read-only) | `<LANGVERSION>` | | |
| Unity Editor version | `<UNITY_VERSION>` | | OPEN-406 |
| Unity license status | `<PERSONAL / COMMERCIAL / NONE / UNKNOWN>` | | Owner statement |
| .NET SDKs (`dotnet --list-sdks`) | `<SDK_LIST>` | | |
| .NET runtimes (`dotnet --list-runtimes`) | `<RUNTIME_LIST>` | | |
| IDE and version | `<IDE_AND_VERSION>` | | |
| Node.js (`node --version`) | `<NODE_VERSION>` | | |

## D. Test isolation

| Field | Value | Date | Obtained by |
|---|---|---|---|
| Lab city name | `<LAB_CITY_NAME>` | | |
| Lab save copies folder | `<LAB_SAVE_COPY_DIR>` | | |
| Isolation method for mods and playsets | `<METHOD>` | | OPEN-035 |
| Mods active in the lab set | `<MOD_LIST>` | | |
| Primary saves are untouched (confirmation) | `<OWNER_CONFIRMATION>` | | Owner statement |

## E. Optional applications

| Field | Value | Date | Obtained by |
|---|---|---|---|
| Melty installed | `<YES / NO>` | | Owner statement only |
| Melty evaluation stage reached | `<STAGE_0 … STAGE_4 / NOT_EVALUATED>` | | [MELTY_EVALUATION](../research/MELTY_EVALUATION.md) |
| Motor Town installed | `<YES / NO>`, not required (OPEN-030) | | Owner statement |

## F. Discovery log

| Date | Command or action (with its label) | Result summary | Notes |
|---|---|---|---|
| `<YYYY-MM-DD>` | `<label: command>` | `<summary>` | `<anything sanitized out>` |
