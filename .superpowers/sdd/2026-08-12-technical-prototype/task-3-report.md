# Task 3 report: player combat and health

## Status

Complete. Added a typed combat request, health/invulnerability component, hitbox/hurtbox nodes, deterministic four-direction attack model, and the player combat adapter. `BuildPlayer.cs` now generates `Player.tscn` with `Health`, `Hurtbox`, `Combat`, `AttackHitbox`, and the required collision shapes.

## RED-GREEN evidence

- RED: added `HealthComponentTests` and `PlayerCombatModelTests`; `dotnet build` then failed as intended with `CS0234` because `Knight.Combat` did not yet exist.
- GREEN: implemented the smallest typed model/components and passed the complete verifier with `PROTOTYPE_TESTS_OK`.
- Review-fix RED: added scene/model regressions for player hitbox collision filters, self-source exclusion, one external hurtbox hit per attack, active monitoring, aim precedence, and dead-zone facing fallback. The build failed as intended because the new collision-layer constants and the required `Hitbox`/`PlayerCombatModel` APIs did not exist.
- Review-fix GREEN: introduced explicit dedicated hurtbox collision filters, source-owner self exclusion, and aim-derived attack facing; all regressions pass.

## Verification

- `powershell -ExecutionPolicy Bypass -File .\\tools\\verify.ps1 -SkipSmoke` passed: build clean and unit test runner printed `PROTOTYPE_TESTS_OK`.
- `Godot --headless --path . --script res://tools/build/BuildPlayer.cs` passed with `pre-pack node count=12; post-instantiate node count=12`.
- Headless editor load and log scan found no `ERROR:`, `SCRIPT ERROR:`, or parse errors.
- Review fix verification: regenerated `Player.tscn` with `BuildPlayer.cs` (`pre-pack node count=12; post-instantiate node count=12`), then reran `verify.ps1 -SkipSmoke` successfully.

## Notes

- Godot generates `unique_id` and external-resource identifiers while serializing `.tscn`; the generated scene's node names, hierarchy, resources, and collision paths are deterministic and are validated by the builder.
- The combat adapter emits Godot-compatible integer attack-direction payloads; the model keeps the strongly typed `AttackDirection` API.
- `Hurtbox` is on a dedicated layer; `AttackHitbox` has layer zero and only masks the hurtbox layer. `Hitbox` also refuses a hurtbox owned by the same source root, as a defense in depth guard.
