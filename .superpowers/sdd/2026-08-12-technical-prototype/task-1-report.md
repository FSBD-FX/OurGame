# Task 1 Report — Migrate verified foundation and establish build gate

## Status

DONE_WITH_CONCERNS

## Implemented

- Created the Godot 4.7.1 Mono C# project (`Godot.NET.Sdk/4.7.1`, `net9.0`, assembly `Knight`).
- Migrated only the requested project instructions, `asset-gen` skill, and the three verified development add-ons.
- Added all twelve shared `InputActions` constants and a Godot `SceneTree` configurator that registers keyboard and controller bindings, writes input settings through `ProjectSettings`, and saves the Godot-generated serialization.
- Added `TestAssert`, headless `RunPrototypeTests`, and foundation checks for all actions, assembly name, and actual scene pack/save behavior.
- Added validated `SceneBuildUtil.PackAndSave`: owner assignment with nested-scene recursion guard, pack/instantiate node-count comparison, and save gating.
- Added `tools/verify.ps1`, plus durable README status, engine/SDK references, input table, and asset manifest.

## TDD evidence

### RED — missing input map

Command:

```powershell
$env:DOTNET_ROOT = 'F:\Documents\游戏\tools\runtime\dotnet-9.0.317'
$env:DOTNET_ROLL_FORWARD = 'Major'
& $godot --headless --path . --script 'res://tests/RunPrototypeTests.cs'
```

Key output / result:

```text
ERROR: System.InvalidOperationException: Assertion failed: missing action move_left
TEST_EXIT=1
```

### GREEN — configured input map

Command:

```powershell
& $godot --headless --path . --script 'res://tools/configure/ConfigureProject.cs'
& $godot --headless --path . --script 'res://tests/RunPrototypeTests.cs'
```

Key output / result:

```text
CONFIGURE_EXIT=0
PROTOTYPE_TESTS_OK
TEST_EXIT=0
```

### RED — missing scene packing implementation

Command:

```powershell
& 'F:\Documents\游戏\tools\runtime\dotnet-9.0.317\dotnet.exe' build Knight.csproj
```

Key output / result after temporarily removing `SceneBuildUtil`:

```text
ProjectFoundationTests.cs(34,24): error CS0103: 当前上下文中不存在名称“SceneBuildUtil”
RED_BUILD_EXIT=1
```

### GREEN — scene packing implementation restored

Command:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\verify.ps1 -SkipSmoke
```

Key output / result:

```text
0 warnings
0 errors
PROTOTYPE_TESTS_OK
GREEN_VERIFY_EXIT=0
```

## Final verification

Command:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\verify.ps1 -SkipSmoke
```

Result:

```text
0 warnings
0 errors
PROTOTYPE_TESTS_OK
FINAL_VERIFY_EXIT=0
```

The verifier ran build, headless import, and unit tests. Its generated logs were scanned for `ERROR:`, `SCRIPT ERROR:`, and `Parse Error`; no matching log entries were found.

## Changed files

- `.gitignore` (already contained needed runtime/build exclusions; no Task 1 change required)
- `README.md`
- `AGENTS.md`
- `godot.md`
- `project.godot`
- `Knight.csproj`
- `.agents/skills/asset-gen/` (copied)
- `addons/godot_ai/` (copied)
- `addons/beehave/` (copied)
- `addons/com.gdquest.godot-steering-ai-framework/` (copied)
- `scripts/shared/InputActions.cs`
- `tests/TestAssert.cs`
- `tests/RunPrototypeTests.cs`
- `tests/unit/ProjectFoundationTests.cs`
- `tools/SceneBuildUtil.cs`
- `tools/configure/ConfigureProject.cs`
- `tools/verify.ps1`

## Self-review

- Confirmed all 12 serialized input entries are present in `project.godot`.
- Confirmed input action literals appear only in `scripts/shared/InputActions.cs` among C# source files.
- Confirmed Godot-facing `SceneTree` classes are `partial`; utility/assertion classes are static.
- Confirmed `SceneBuildUtil` skips recursion below instantiated-scene children and blocks saving if packed node count changes.
- Confirmed development add-ons are editor plugins and no gameplay runtime logic was added.
- The original report's broad `git diff --check` statement was not reproducible after the full third-party add-on migration because that command only checks uncommitted changes. The remediation uses a scoped check over the Task 1 self-authored files and records its fresh output below.

## Concerns

1. Windows execution policy prevents direct invocation of `.ps1` in this environment; verification was run with `powershell -ExecutionPolicy Bypass -File`, which executes the exact verifier script unchanged.
2. The installed Godot 4.7.1 Mono binary requests .NET 8 while the previous supplied runtime directory was .NET 9 only. The verifier now prefers `G:\godot\dotnet-sdk`, which contains side-by-side .NET 8 and 9, sets both .NET root variables, and uses `DOTNET_ROLL_FORWARD=LatestMajor`. This is a machine runtime prerequisite, not project logic.
3. Headless import reports `MCP | plugin disabled in headless mode` as an informational line. It is not an engine/script error and no scanned error patterns were found.

## Review remediation (2026-08-12)

### Changes

- `tools/verify.ps1` now prefers the stable English path `G:\godot\dotnet-sdk`, sets both `DOTNET_ROOT` and `DOTNET_ROOT_X64`, uses `DOTNET_ROLL_FORWARD=LatestMajor`, and falls back to the original local .NET 9 runtime only when necessary. It throws a clear error if neither runtime exists.
- `project.godot` now explicitly sets `physics/common/physics_ticks_per_second=60` and no longer references the nonexistent `res://icon.svg`.
- Foundation tests now assert the explicit physics setting, absence of the icon line, every keyboard/controller mapping, stick/trigger axis direction, and the 0.2 deadzone.
- Scene packing tests now cover both a valid local hierarchy and an unowned child beneath an instantiated nested scene. The latter must return `Error.InvalidData` and not save output. `SceneBuildUtil` reports this expected validation result by return value without emitting an engine `ERROR:` line that would poison the gate logs.

### TDD evidence

1. Configuration RED — after adding the explicit-setting test but before changing project configuration:

```text
Assertion failed: project does not reference a missing icon resource
RED_BUILD_EXIT=0 RED_TEST_EXIT=1
```

2. Scene validation RED — after repairing configuration but before correcting test construction and expected-rejection logging:

```text
ERROR: Invalid owner. Owner must be an ancestor in the tree.
ERROR: Scene node count mismatch: expected=3, actual=2
GREEN_VERIFY_EXIT=1
```

3. Verifier-hosting RED — after adding the verifier-source regression test and temporarily changing `LatestMajor` back to `Major`:

```text
Assertion failed: verifier uses LatestMajor roll forward
VERIFY_SCRIPT_RED_BUILD_EXIT=0 VERIFY_SCRIPT_RED_TEST_EXIT=1
```

4. GREEN — after the minimal fixes:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\verify.ps1 -SkipSmoke
```

```text
0 warnings
0 errors
PROTOTYPE_TESTS_OK
FINAL_VERIFY_EXIT=0
```

5. Explicit physics-setting RED — after adding the source-level assertion and temporarily removing the setting:

```text
Assertion failed: project explicitly configures the 60 Hz physics tick rate
PHYSICS_RED_BUILD_EXIT=0 PHYSICS_RED_TEST_EXIT=1
```

The same final verifier command was rerun after restoring the setting and produced the GREEN output above.

### Fresh self-authored diff check

The scoped `git diff --check` over `README.md`, `project.godot`, `tests/`, `tools/`, and this report completed with no whitespace errors before the remediation commit. No copied third-party add-on files were changed during remediation.
