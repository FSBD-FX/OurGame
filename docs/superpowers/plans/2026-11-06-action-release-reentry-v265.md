# v265 · 接住尚未结束的动作收势

> Inline execution with test-driven-development, systematic-debugging and one final read-only review. Do not commit, clean or publish.

**Goal:** 按已批准的 V03/R-C 连续动作要求，修复收势仍可见时开始下一招却瞬间掉回基础姿态的消费者缺陷。
**Spec:** `docs/production/2026-11-03-character-motion-rework-requirements-v001.md` 的真实动作连续性，及 v261 纯 action／载荷／secondary 各自所有权。No new creative direction or changed gameplay timing.
**Architecture:** First prove actual saved-rig geometry. During new action entry select last displayed *pure overlay* and its actual weight; reuse the existing transition and elapsed clock. Do not preserve final pose, old action status, or action/ability authority.

## Scope and review focus

- Production `scripts/presentation/PlayerFormalVisual.cs` only if actual RED. Test `tests/integration/PlayerFormalVisualTests.cs` in existing body-load portion of ground-gait. Natural producer verification in existing fixture if supported by legal input; do not fabricate a controller state.
- Pure None release versus CurrentActionPose, overlay weighting, old Mantle ownership on entry and its eventual exit; thread timed progress, both facing, zero elapsed/repeat, already finished release, explicit reset and no double-baked secondary/load.
- Keep physical timing, input, user save, art, engine/GC/cache/lifetime, all normal routes and original strict gates unchanged. Only headless, no system input/window/capture/spend/automation/Git/deletion/release.

### Task 1 — reproduce and repair

- [x] Add real saved Formal test in `RunBodyLoadOnly`: nontrivial visible release, attack/thread reentry, fixed clock geometry then live progress and finished-release control. Existing forced resets retained, no new isolated reset case claimed.
- [x] Run canonical `-Phase thruster-ground-gait -SourceRows 1898`, preserve actual saved-rig RED and distinct live J RED, then select displayed release overlay when previous state is None; retain weight/reset. First compile failure not counted.
- [ ] Final canonical phase PASS19660ms incl real input; presentation model/rig/actions and ordinary post PASS. Ordinary pre strict FAIL7 Handle errors; exact batch terminal and original errors retained. Full strict gate remains OPEN, no replay or qualification transfer.
- [x] Bind actual 1898 source/40 protection pairs/48 sealed binaries/16 helpers/6 current outputs, report scoped actual evidence, one independent review C0/I0/M0. Original goal stays ACTIVE, no commit/cleanup.
