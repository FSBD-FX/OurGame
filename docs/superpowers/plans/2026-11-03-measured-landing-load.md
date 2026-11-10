# Measured Landing Load Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox syntax. Root is sole writer/native owner; assistants are read-only.

**Goal:** Make real landing impact produce coherent body/leg absorption during ordinary and still-burning jet contact without changing gameplay.
**Architecture:** Pure one-shot measured-load model→Timeline/frame→Formal common load and planted leg solve. Coordinator supplies Motor identity/count/reliable speed; old callers remain optional/fallback.
**Tech Stack:** Existing Godot/.NET C#, canonical PowerShell verifier, no runtime/dependency change.
**Spec:** docs/superpowers/specs/2026-11-03-measured-landing-load-design.md

## Global Constraints

OnlyG:/knight/Knight; existing source1874 becomes1875 only when the new model file is added. Preserve all original eleven items. Headless/internal Godot input only, original build180s/focused55s/pre180s/post300s. Freeze all CS/helper during any owned process. No gameplay force, collision, damage, HP, input, map, save, cached resource ownership or engine changes. Manual exact baselines; no Git mutation/deletion/paid generation/GUI/OS/new dump/automation.

## Review Focus

1. Same count, zero delta, invalid/unreliable measurement or repeated standing must not manufacture another impact.
2. Changed owner, counter rollback, death/recovery and immediate jump must clear/quarantine history, not replay old impact.
3. Dash/action priority stays intact, with synchronous grounded absorption and observed fixed-length planted feet; no delayed clip or stretch/penetration.
4. Finite extreme speeds and30/60/120Hz elapsed samples remain finite/bounded and comparable, without treating sampling tests as hardware FPS proof.
5. Common torso/arm/weapon/core/light/cloth attachment geometry remains coherent when action mixing and measured landing overlap; fallback constructors/templates remain usable.

### Task1: Actual impact channel and coherent absorption

**Files:** create scripts/presentation/PlayerLandingLoad.cs; modify scripts/presentation/PlayerActionFrame.cs, PlayerPresentationCoordinator.cs, PlayerFormalVisual.cs, PlayerGroundGait.cs (actual-contact full plant, ordinary blend retained); tests/unit/PlayerPresentationTests.cs; tests/integration/LandingImpactRuntimeTests.cs, PlayerFormalVisualTests.cs; tests/RunThrusterCoreTests.cs; existing canonical capture/receipt checks. Exact baselines of existing files belong in this plan's workspace.

**Interfaces:** PlayerLandingObservation(ulong OwnerId,ulong Count,float Speed,bool Reliable); PlayerLandingLoadFrame(bool Active,float Compression,float Strength); PlayerLandingLoadModel.Step(PlayerLandingObservation observation,bool grounded,bool suppressed,float deltaSeconds)→PlayerLandingLoadFrame. Input.Landing/frame.LandingLoad optional init properties. Coordinator supplies real IDs/count/impact; Timeline never writes Motor state.

- [x] Add actual saved-player still-burning landing assertion to existing impact-player phase using its real late-jet Drop case. Actual3a5d02ca buildquiet/native5353ms exit1, realfloor/Count1/reliable717.5/Burning/Dash/formal proven before expected LANDING_LOAD_CHANNEL_RED. Source1874, guards[], no timeout/waiver; original two Drop cases returned before third. This is a missing real presentation channel, not proof all physics or artwork is fixed.
- [x] Write pure model cases in existing PlayerPresentationTests: one shot, monotonic severity, owner/counter baselines, zero delta, unreliable/invalid rejection, jump/death clear, no replay, elapsed-rate/bounds. Actual462f65c4 missing-model assertion RED, quiet build/native exit1 under original55s, not compile error. Pure model implementation follows this observed RED; consumer/renderer still unimplemented.
- [x] Implement exact value API/model, optional frame/input plus actual Motor bridge and grounded consumption. Model36 and initial actualcontact GREEN2a58/07cb; newsource1875 captured. Stronger first-impact full support remainedFAIL, separately reproducedandfixedinsideGroundGait; final current-source regression pending, not whole-feature complete.
- [x] Write and witness formal-consumer RED72db (actual0vs3), then six cases/twelve soles/two facings/actions/terminal/no-support and template controls GREENea260; known compile9dcc/fixture5d2d failures retained/corrected without gameplay change. Current-source rerun pending after genuine support-handoff REDb876 and owner-side plant fix.
- [x] Finish actual saved-player contact proof/actual body-removal negative control, rigid world soles and standing no-replay; current3493/c527/256d/c142/dd39 five focused PASS under original55s. New exact receipts with1positive7negative controls reject partial/duplicate forms. No production test-only APIs.
- [x] One fresh whole-change review against nine manual baselines/newmodel+purehelper/spec/ledger; C0/I0/M2, no C/I fix pass required, two minors deferred, no re-review.
- [x] Current ordinary source-frozen pre1db171075/posta84204700/actions4d7b9522/keyboard5b5e20152+standalone title7712/modelsb892509 all strictPASS, original caps. Earlier6behaviorRED/fixtureFAIL/BUILDFAIL retained; historical resource failures/Main899/full route and normal human gates remainOPEN. Final10technicalgates green is scoped to this increment, not original eleven-item completion.
- [x] Update README/backlog/U-20261103-01/motion requirements/report/ledger; independent24-case custody119completepairs+1missingbinarypair/11440source/248helper/138bin/48owned, current1875/14helpers/6bin match, oldidentities ended. Media7427750bytes/86files/Notifyfalse; no newmedia/automation. Local technical increment complete, original eleven items/R-I/R-C/R-P/normal-speed/hardware/human/Main899/historyresource stillOPEN.
