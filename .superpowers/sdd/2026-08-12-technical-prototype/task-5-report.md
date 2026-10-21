# Task 5 report: Rust-Shell Squire

## Status

Complete. Added the typed, node-free `RustShellModel` finite-state machine and its settings/frame/state types. The model exposes only signed horizontal distance, visibility, stun, death, and delta inputs; it does not discover or depend on a player node. It produces `Idle`, `Chase`, `Telegraph`, `Attack`, `Recover`, `Stunned`, and absorbing `Dead` states.

`RustShellSquire` is the Godot adapter boundary. It finds the player through the `player` group, performs line-of-sight ray checks, applies model movement, gates the existing `Hitbox` to the attack state, receives damage through `HealthComponent`/`Hurtbox`, and implements `IThreadPullReceiver` without a player-specific type dependency. The thread target is configured as `ThreadTargetKind.LightEnemy`.

`BuildRustShellSquire.cs` generates the formal scene, then packs, reloads, re-instantiates, compares node counts, and validates required node paths, combat collision filters, and the thread contract. No paid or imported visual asset was used. The visual is procedural only: rust-brown rectangular shell, circular rear bell, and blade; telegraph is both amber bell and backward lean, while attack flashes the blade red.

## RED-GREEN evidence

- RED: registered the eight required enemy behavior tests and ran `G:\\godot\\dotnet-sdk\\dotnet.exe build .\\Knight.csproj`. It failed as intended with `CS0234`: `Knight.Enemies` did not yet exist.
- GREEN: added the smallest pure model needed by those tests. The direct unit runner reported `PROTOTYPE_TESTS_OK`; compile is clean with zero warnings/errors.
- Review hardening RED: added independent assertions inside the existing eight test methods, then temporarily mutated each relevant production branch. The suite failed specifically for ignored idle LOS, disabled chase exit, exclusive 360/70 boundaries, forced chase after a hidden stun, death bypass during telegraph/attack/recovery, and non-attack states incorrectly reporting `AttackActive`.
- Review hardening GREEN: restored the unchanged production model. The strengthened suite now covers no-LOS idle/stillness; both chase exit conditions; inclusive range boundaries; hidden stun expiry; immediate death from telegraph, attack, and recovery; and false `AttackActive` for idle, chase, and stunned states.

## Verification

- `G:\\godot\\Godot_v4.7.1-stable_mono_win64\\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --script res://tools/build/BuildRustShellSquire.cs` regenerated the enemy scene and reported `pre-pack node count=9; post-instantiate node count=9`.
- `G:\\godot\\Godot_v4.7.1-stable_mono_win64\\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --script res://tests/SceneLoadRustShellSquire.cs` passed with `RUST_SHELL_SQUIRE_SCENE_LOAD_OK`.
- `powershell -ExecutionPolicy Bypass -File .\\tools\\verify.ps1 -SkipSmoke` passed with a clean build and `PROTOTYPE_TESTS_OK`.

## Notes

- The standalone enemy scene does not require a parent/player: no player simply yields `Idle`.
- The adapter uses explicit exported node paths for all child contracts. It avoids core Beehave/Steering coupling; those plugins remain development-only.
