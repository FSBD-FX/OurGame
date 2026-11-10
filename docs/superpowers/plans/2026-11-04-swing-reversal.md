# Swing Reversal Implementation Plan

> Required: executing-plans and TDD; root writer, read-only independent review.

**Goal:** Keep re-started foot swings continuous within their authored envelope.
**Architecture:** Bound the added arc inside the existing presentation-only gait; preserve projection and motion authority.
**Tech stack:** Existing Godot/.NET and canonical headless ground-gait phase.
**Spec:** `docs/superpowers/specs/2026-11-04-swing-reversal-design.md`

## Global Constraints

Only G:/knight/Knight, headless, no changes to mechanics, saves, assets or Git. Preserve original goal and failures. Use user-directed autonomy for the existing motion plan, not new aesthetic approval. Preserve dirty baselines.

## Task 1

- [x] In `tests/unit/PlayerPresentationTests.cs` use literal mirrored supported steps 0→18→10 and interrupted stop/resume 0→18→18→22. Assert the re-targeted sole stays within authored lift and the opposite stance does not slip. Old native case 797eafed fails `GROUND_SWING_RETARGET_RED` at 28→23.331354 sole Y.
- [x] In `scripts/presentation/PlayerGroundGait.cs`, `MoveSwing`, retain existing ease/endpoints, bound added arc by lift budget remaining at departure; no new state or API.
- [x] Rerun `thruster-ground-gait`, inspect real draw/binding and original jump/brake/landing markers; independently review exact diff. d597242d native14373ms strict PASS, review C0/I0/M1. Full same-version goal remains open.

Review focus: zero lift at contact, non-restarted arc unchanged, rotated support uses its normal, brake attenuation unchanged, stationary settle unaffected.
