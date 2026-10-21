# SDD ledger — plan: docs/superpowers/plans/2026-08-29-terrain-grapple-dense-canyon-revision.md

## Workspace verification

- Worktree: `E:\OurGame\Knight\.worktrees\player-art-pipeline`
- Branch: `codex/player-art-pipeline`
- Baseline HEAD: `eca4a004d37085efd33a3c2b9f93a366c468d391`
- Git common directory: `E:\OurGame\Knight\.git`
- This is the existing linked worktree; no additional worktree may be created.
- Preserve all unrelated dirty files and never terminate the external Godot PID 31500.
- Protected files unless a later approved task explicitly requires them: `project.godot`, `scenes/prologue/PrologueLevel.tscn`, `scenes/presentation/PrologueEnvironmentKit.tscn`.

## Rulings

1. The official SDD helper scripts are Bash-only, but Git Bash is unavailable and Windows `bash.exe` is an unconfigured WSL launcher. Equivalent briefs, reports, ledgers, and review packages will be created with `apply_patch` plus native Git output. Cost if wrong: scratch artifact compatibility only; runtime/source semantics are unaffected.
2. Keep `MechanicalClawController.CurrentTarget` and `ClawFrameChanged` typed as `GrappleSurface2D`. Ordinary terrain bodies receive a child `GrappleSurface2D` profile, while the resolver records the actual ray hit point in the snapshot. Cost if wrong: changing the public event type would cascade through HUD, VFX, audio, and tests.
3. Append new optional fields to `MechanicalClawInput` and `MechanicalClawFrame`; do not reorder required fields. Cost if wrong: existing constructor call sites would break source compatibility.
4. The catalog flag `OptionalSubarea` applies to the archive alcove; salvage and high-ruin paths are optional subsections within required micro-scenes and must be asserted by node paths rather than the catalog flag. Cost if wrong: tests would encode a false content contract.
5. Task 1 changes the pure model only. Runtime jump suppression and real input arbitration belong to Task 2, so Task 1 must not modify the controller to simulate unfinished integration.
6. Task 2 may update the obsolete attached-second-Space assertion in `tests/integration/PrologueCanyonTraversalTests.cs` now instead of waiting for Task 3. The old assertion requires Space to release while LMB remains held and directly contradicts the approved boost/release contract. Cost if deferred: materialized post phase cannot become GREEN, so Task 2 integration would remain uncertified.
7. Do not weaken the existing safe-landing assertion or change the approved `360f/220f` boost to force the old sparse canyon route fully green. Task 2 certification may temporarily accept materialized post progressing through all new boost assertions and failing only the legacy `ascent_grapple_one` 0.8 landing geometry; Task 3 owns the dense route/anchors and must restore the complete post phase. Cost if wrong: one atomic intermediate commit is not globally green, so Task 3 cannot be reviewed or committed until it restores full post GREEN.
8. CatchThree uses a real typed ceiling-underside ray notch, not a dedicated anchor or a 2.5 px clearance coincidence. Its proof must retain an actual hit in [360,520], AnchorTwo clearance >=12 px, an open LandingThree ray notch, AnchorThree separation, and >=12 px vertical headroom. Cost if wrong: a visually plausible but physically blocked return cast could pass a surrogate-distance assertion.
9. The recovery-distance structural test must intersect the authored collision rectangle along the actual center ray; center-to-center distance is not a collision proof. Cost if wrong: a real typed hit may be in range while a body-center proxy falsely fails (or the reverse).
10. `ArchiveFloor` is one-way to keep top-side support while allowing the optional ArchiveAlcove skip route under the 70 px opening. Cost if wrong: the 72 px player collider can pin between the solid floor and the lower skip floor.
11. BellChain's legacy OutOfRange test derives its probe from `MechanicalClawSettings.Default.MaxRange + 1` after Task 1 changed the default to 620. Cost if wrong: the old 521 px setup verifies a valid cast rather than OutOfRange.
12. Guidance and Completion integration fixtures own unique temporary save/settings paths. Cost if wrong: a persistent `user://knight/settings.json` state leaks across legacy cases and makes visual/pause assertions order-dependent.
13. Preserve the pre-existing common-root `VisualRoot/Effects/SectorLabel` in task-only materialization; Task 3 content must not add a duplicate label to compensate for a frozen user hunk. Cost if wrong: the legacy evidence contract would be validated against user-dirty state rather than BASE plus Task 3 blobs.
14. Canyon claw use is capped at exactly four generated `CanyonClawMechanism2D` instances at the approved sector-relative paths: Descent `Counterweight` and Archive `HiddenPanel`, Basin `CargoPlatform`, Temple `BellBody`. Cost if wrong: a fifth mechanism expands the approved route/content scope without a structural failure.

## Preflight dependency matrix

| Pair / task | Shared contract | Ruling / required check |
|---|---|---|
| T1 self | settings, input, frame, state machine | One boost per latch/ground reset; 620 px default; optional fields preserve callers. |
| T2 self | resolver, terrain profile, controller | Raycast must return actual hit point and typed profile; no scene-name inference. |
| T3 self | catalog, route, mechanisms | Exactly 15 named micro-scenes; critical path remains readable and recoverable. |
| T4 self | HUD, guidance, audio/VFX | First-level-only help and reticle states must reflect actual runtime state. |
| T5 self | telemetry schema and diagnostics | Version 3 fields are additive and must capture the pre-fall context. |
| T6 self | verifier and proof gates | Two consecutive greens, reviewed recordings, then human playtest gate. |
| T1 -> T2 | `MechanicalClawInput`, `MechanicalClawFrame`, settings/model state | Task 2 consumes the appended boost/grounded fields and performs runtime jump arbitration. |
| T2 -> T3 | `GrappleSurface2D`, hit resolver | Builder-created walls, ceilings, undersides, edges, ice, and forbidden surfaces use typed profiles. |
| T2 -> T4 | `CurrentTarget`, frame/event signature | Preserve public surface profile type so reticle/HUD consumers do not cascade. |
| T2 -> T5 | current target and claw frame | Telemetry reads resolved surface, hit point, latch state, and input context. |
| T3 -> T4 | generated content paths | Guidance targets stable builder node paths and cannot depend on placeholder art internals. |
| T3 -> T5 | sectors, platforms, recovery layers | Last-grounded platform and sector values use stable IDs emitted by the dense route. |
| T3 -> T6 | route and micro-scene evidence | Proof must traverse all five sectors and validate the 15-scene structure. |
| T4 -> T6 | HUD/audio/VFX evidence | Recording review verifies readable Q/E, reticle, energy, heat, attack, shield, and claw feedback. |
| T5 -> T6 | fall-report v3 | Final verifier archives the exact fall context and local diagnostic output. |

## Baseline

- Status: characterized; canonical HEAD baseline remains green, while the shared dirty checkout has known protected-file divergence.
- `dotnet build Knight.csproj --no-restore`: GREEN, 0 warnings / 0 errors.
- Raw `RunPrototypeTests.cs`: expected checkout-local failure because user-owned dirty `project.godot` restores legacy `attack`/`thread`; canonical HEAD has neither action and the latest materialized verifier was green.
- Raw `RunPrologueTests.cs -- --phase=pre|post`: expected checkout-local failures because user-owned dirty generated scenes are not the canonical five-sector materialization. The initial no-phase invocation was an invalid diagnostic command and is not a product failure.
- Baseline logs: `artifacts/task11-revision/baseline-20260829/`.
- Canonical materialized verifier reference: `artifacts/task11-verify/20260828T142515982Z-92dbf42556414dd3824c255434a4b282`, SHA-256 `82BAAE3238960552006BB85D5958A788921AA2B896D7B3D501E28A053E60CA68`.
- Task GREEN checks must either use focused assertions whose execution is observable before the known dirty-checkout failure, or run from committed/materialized HEAD; no task may edit protected files merely to make the dirty checkout pass.

## Task 3 protected scene snapshots

| Working file | Required restored SHA-256 |
|---|---|
| `scenes/prologue/PrologueLevel.tscn` | `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115` |
| `scenes/prologue/sectors/CanyonEntrance.tscn` | `3F7844D5AFF22308D471EC5A99BCA9FB56F18BFA0C5F0618259FC87264A2C14D` |
| `scenes/prologue/sectors/CanyonDescent.tscn` | `D5B981D9E350CD116D232F5762CC4E20B867795662314D9881455A1BA37236FC` |
| `scenes/prologue/sectors/CanyonBasin.tscn` | `6594F7A707E42B74BABC27EA275FC8CBE81981E8CB9A1C0A3567FCD255104B38` |
| `scenes/prologue/sectors/CanyonAscent.tscn` | `028B88B64EF11118CD36D2BB075260E72AA86E04A2DF61B6216C58B5BF14CC9C` |
| `scenes/prologue/sectors/LostTemple.tscn` | `C50CF4CE9180834F09C42D61DB77F49AD97AA10DC0DCD77F55FCE4029B71DEBE` |

## Task status

| Task | Implementer | BASE | HEAD | Spec review | Quality review | Status |
|---|---|---|---|---|---|---|
| 1 | `/root/task1_claw_model` | `eca4a004d37085efd33a3c2b9f93a366c468d391` | `5b2c97b57b245307c7891b608342bfe05e1af86d` | PASS | PASS | complete |
| 2 | `/root/task2_typed_terrain` | `5b2c97b57b245307c7891b608342bfe05e1af86d` | `e992423333997b9cdafaa6679674366b8b38ee4c` | PASS | PASS | complete |
| 3 | `/root/task3_dense_canyon` | `e992423333997b9cdafaa6679674366b8b38ee4c` | `6b2d1a9` plus review repair pending | not requested | review repair GREEN | repair verified; commit follows |
| 4 | `/root/task4_guidance_ui` | `64aa74217bab8cc377512615ec3fc75fb7a449a5` | task-only isolated-index amendment | PASS — TDD, semantic mutation evidence, and report | PASS — build, PRE/POST, real four-resolution capture | complete |
| 5 | `/root/task5_fall_context` | `b92de9402bb297ca032e5c7c7962299ba99f85ff` | task-only isolated-index blob set + two repairs | PASS — TDD, migration, boundary, ordering, filter, duplicate-frame, Markdown-cell, and recovery-before-process timing RED/GREEN | PASS — build, synthetic JSON/Markdown, repaired PRE/POST and transition contract | complete |
| 6 | `/root/task6_final_evidence` | `e4e06989af73b4b98a61fbf40d3f103e1888fdcd` | final `HEAD + Task 6 + compatible overlay` candidate; commit pending | PASS — fail-closed evidence mutations and two fresh verifier summaries | PASS — final audit/capture, 36 original-resolution frame reviews, no fatal/error/leak | automated gate complete; human gate open |

## Audit log

- 2026-08-29: User selected execution option 1; subagent-driven implementation authorized.
- 2026-08-29: Verified existing linked worktree, branch, baseline HEAD, and dirty-tree preservation constraints.
- 2026-08-29: Characterized baseline: compilation is green; raw Godot suite failures are caused by protected user-dirty project/generated scenes and do not reproduce in canonical materialized verification.
- 2026-08-29: Dispatched Task 1 to `/root/task1_claw_model` with exclusive ownership of the pure mechanical-claw model/test slice.
- 2026-08-29: Task 1 implementer committed `5b2c97b57b245307c7891b608342bfe05e1af86d`; focused GREEN and all three mutation checks reported successful, while the characterized dirty InputMap failure remains isolated.
- 2026-08-29: Generated exact BASE..HEAD review diff SHA-256 `23ED2177046D442E69E7EC1B914258C3AB1C52B9B9AB5E5A61450AF2A5BA9A0D` and dispatched `/root/task1_revision_review` for combined spec/quality review.
- 2026-08-29: Task 1 review returned `SPEC: PASS`, `QUALITY: PASS`, no findings.
- 2026-08-29: Task 2 preflight found a pre-existing 13-line user-dirty HUD delta. Frozen diff SHA-256 `D0000B94E3E1847F8397A71367AB8CAF1F3E578FA3322458DF52B086B8F885AB`; file SHA-256 `DA5BA0F9C486A1C8672424CFEEAF01311D5EA604536701A2111012709B69D1DB`. Task 2 must preserve it unstaged.
- 2026-08-29: Dispatched Task 2 to `/root/task2_typed_terrain` with exclusive ownership of terrain resolver, runtime arbitration, presentation compatibility, and focused tests.
- 2026-08-29: Materialized post exposed an obsolete cross-task assertion at `PrologueCanyonTraversalTests.cs:341`. Reassigned the minimal assertion update from Task 3 to Task 2. This file also has a pre-existing two-line position delta; frozen diff SHA-256 `6038DFF508368948E15C6B01D4D38AA0986DE8A2248EA0AA8C82EF005C364D21`, file SHA-256 `724526F61973E5E039B162022E8C8013F6D5049C18B289F6A88EBDB64EF556FB`, which must remain unstaged.
- 2026-08-29: After correcting the obsolete Space-release assertion, materialized post proves the new boost contract across all assisted samples through CanyonAscent but the old sparse route's `ascent_grapple_one` 0.8 sample lands outside its legacy safe zone. Deferred only this geometry incompatibility to Task 3; prohibited weakening the assertion or changing approved boost tuning.
- 2026-08-29: Task 2 implementer committed `e992423333997b9cdafaa6679674366b8b38ee4c`; exact review diff SHA-256 `E3B34B645E0D18F9AB7FE428BAC4F50CEF889F8820D46B2C71F69B71E2C2FB45`; dispatched `/root/task2_revision_review`.
- 2026-08-29: Task 2 review returned `SPEC: PASS`, `QUALITY: PASS`, no findings.
- 2026-08-29: Task 3 preflight froze pre-existing source/test diff SHA-256 `2A2FFEB29122E8D407FA8E031AF0EA7ED632B963400B848060B05BA4BF4A5663` and protected generated-scene diff SHA-256 `DBEB903021A9346B0145EE1D694CF7C531ABBC599EAB260FD591FB63AEBF35D1`. Task 3 must stage only new source/test hunks and restore all six generated scene hashes after build verification.
- 2026-08-29: Dispatched Task 3 to `/root/task3_dense_canyon`; full materialized pre+post GREEN and protected-scene hash restoration are hard gates.
- 2026-09-01: Task 3 added the exact 15-entry catalog, five density builders, typed platform surfaces, recovery layers, four one-shot claw mechanisms, physical Catch1/2/3 return probes, and structural/micro-scene contracts. CatchThree now uses a physical ceiling-underside ray notch with 514.29 px real hit distance rather than the rejected 2.5 px clearance attempt.
- 2026-09-01: Materialized builders packed CanyonEntrance=84/3, CanyonDescent=154/4, CanyonBasin=158/3, CanyonAscent=201/3, LostTemple=125/2, then main. `pre-final.console.log` ended `PROLOGUE_TEST_PHASE_OK phase=pre`; `post-final.console.log` ended `PROLOGUE_TEST_PHASE_OK phase=post` and `PROLOGUE_TESTS_OK`.
- 2026-09-01: Root-caused ArchiveAlcove pinning as a 70 px solid-floor gap against a 72 px player body; made ArchiveFloor one-way. Final post proves pass/skip/returnable. Isolated two persistent-settings fixtures and updated BellChain's obsolete 521 px OutOfRange test to `Default.MaxRange + 1` after the approved 620 px Task 1 range change.
- 2026-09-01: Ran five task-only materialized negative mutations: missing catalog marker, missing typed profile, catch layer at 352 px below launch, repeatable mechanism, and mandatory Dash. All failed directly and each modified source hash was restored; logs are in `artifacts/task11-revision/task3-mutations-task-only/`.
- 2026-09-01: Rechecked all six original protected generated-scene SHA-256 values against the Task 3 snapshot and kept the frozen user source/test delta `2A2FFEB29122E8D407FA8E031AF0EA7ED632B963400B848060B05BA4BF4A5663` unstaged. See `task-3-report.md` for immutable proof paths and hashes.
- 2026-09-01: Audit found an early materialization incorrectly carried the four frozen user hunks. Discarded its final proof, reconstructed the materialization as BASE/prior commits plus Task 3 blobs, and reran full PRE/POST (`pre-task-only.console.log`, `post-task-only.console.log`) to GREEN before staging.
- 2026-09-01: Independent review found an unapproved fifth generated claw mechanism,
  `CanyonAscent/Gameplay/Mechanisms/AscentCounterweight`. Added a test-first global inventory
  contract that enumerates packed `CanyonClawMechanism2D` descendants, asserts four total and
  locks the four approved sector-relative path/kind pairs. Fresh detached task-only RED failed
  `expected=4, actual=5`; deleting only the Ascent builder call made focused Catch1/2/3 and full
  task-only PRE/POST GREEN (`PROLOGUE_TESTS_OK`). Reran the repeat-activation mutation to its
  expected RED and restored its detached source SHA; review logs are in
  `artifacts/task11-revision/task3-review-repair/`. Generated scenes existed only in the detached
  materialization and were not staged.
- 2026-09-01: Task 4 retained live glyph-based first-level claw instruction, generated terrain
  profile targets, six color/shape/cue-distinct reticle states, and deterministic existing
  audio/VFX reuse. A real 1280×720 visual audit found the reticle crossing Formal HUD; a
  runtime protected-region RED was added, then a nearest-legal-position avoidance rule made
  the four-resolution real OpenGL capture GREEN. Task-only runner, full PRE/POST, and eight
  targeted negative mutations are documented in `task-4-report.md`; all frozen user deltas and
  six generated-scene SHA-256 values remain preserved and unstaged.
- 2026-09-01: Task 5 introduced report version 3 fall samples, real v2 migration with no invented fall data, exact floor-support paths, a 1.5-second stable input-edge window, and pre-reset `FALL RESET` capture ordered as death, fall, recovery, immediate persistence. Isolated RED/GREEN evidence, synthetic JSON/Markdown, capture-boundary, migration, ordering, and input-filter mutations are documented in `task-5-report.md`. The Task 3 generated route was rebuilt only inside the Task 5 materialization before full PRE/POST; the frozen shared Pester hunk and all six protected scene hashes remain unchanged.
- 2026-09-01: Task 5 repair added a monotonic `PlayerInputReader.CurrentFrameSequence` and recorder claim gate, eliminating duplicate just-pressed/released edge sampling when `_Process` outruns `_PhysicsProcess`. It also added dynamic Markdown-cell escaping for null, backslash, pipe, CR/LF data. Isolated RED/GREEN and three matching negative mutations passed; rebuilt Task 3 materialization then finished task-only PRE/POST with `PROLOGUE_TESTS_OK`. The Pester user hunk remains frozen and is merged only after the repair blob is committed; see `task-5-report.md` and `task-5-repair-preexisting.manifest.txt`.
- 2026-09-01: Task 5 second repair closes recovery-before-process input loss. `RecoveryRequested` now claims its current physics input sequence before fall capture and before `BeginRecovery` clears the frame; the existing sequence gate makes normal `_Process` idempotent and prevents reset-default releases. A real signal-ordering fixture, its omit-observation mutation, the Task 3 materialized PRE/POST, and the transition contract all passed. See `task-5-report.md` and `task-5-repair2-preexisting.manifest.txt`.
- 2026-09-01: Task 6 froze the shared task-owned overlaps, then performed all implementation,
  audit, recording and verification from `e4e06989af73b4b98a61fbf40d3f103e1888fdcd` plus a
  Task 6-only materialization. It added fail-closed proof/mutation gates, real InputMap critical
  path and presentation evidence, Windows-compatible streaming AVI/hash handling, and a
  viewport-dispatched input helper without touching the user-owned position/Pester hunks.
- 2026-09-01: Final Task 6 overlay audit/capture root pair
  `audit-final-working-20260901T121429023Z` /
  `capture-final-working-20260901T121429023Z` passed Windows fixed-60 1280×720 requirements,
  5 sectors, 15 micro-scenes, exactly 4 claw roles and a 6.201 ms callback peak. All 36
  keyframes were opened at original resolution and recorded in the validation document.
- 2026-09-01: Two fresh Task 6 formal verifiers
  `verify-final-working-1-20260901T122127595Z` and
  `verify-final-working-2-20260901T122425864Z` each emitted exactly one
  `PROLOGUE_VERIFY_OK`, build 0/0 and a clean fatal/error/leak gate while preserving scene,
  runtime/capture candidate `9FC8BDAE4B2947D834AF6ECA4981E538C29F5911BF2A98A27483B3FFF8624D9A`, and
  evidence candidate `3876653E541A024CAA29669C618A834BD5C84886123A18BC947FD142B008B6A3`. Automated
  acceptance is complete. Step 8 remains intentionally unstarted; no `HandsOnLauncher` report or
  user qualitative note has been created by automation.
- 2026-09-02: Independent Task 6 review remediation superseded the preceding Task 6 recording
  references. Restored all BASE and retained Task 11 fail-closed verifier gates; moved large AVI
  parsing/hashing to bounded-memory streams; exercised Windows PowerShell 5.1 and PowerShell 7
  child-host resolution; and replaced source-string checks with 15 actual isolated mutation
  materializations. The final BASE pass is
  `artifacts/task6-final-base-gdignore-20260902T004409430`; the two-host runtime candidate is
  `49A6E23D6782668F04EE8061E3559E2B377765EBFF0ABCAB6E3D46A97AFD612D`.
- 2026-09-02: Fresh capture `artifacts/task6-final-capture-20260902T005609288` passed Windows
  fixed-60/1280×720, five sectors, 15 micro-scenes, four claw roles and 6.614 ms callback peak.
  Full-flow AVI `6C6E5046F0653BD02C366D8CF0C8119FB5CF2971DC83B51068BA2DA5A62B48E5` is 4,053 frames;
  Boss-retry AVI `FDF14343CF0A5C2755B076597DF2BDB5C526B77B64382A56959173409F0F261C` is 2,719 frames.
  All 46 bound keyframes were opened at original resolution and recorded in the validation doc.
- 2026-09-02: Formal Pester 5/5 with 15/15 real mutations passed at
  `artifacts/task6-final-pester-20260902T010329439`. Fresh verifiers
  `task6-final-verify-1-20260902T010601067` and
  `task6-final-verify-2-20260902T012103825` each emitted exactly one
  `PROLOGUE_VERIFY_OK`, build 0/0, no fatal/error/leak, and the same restored scene SHA
  `9C81ABD51F8C834295AF03FD7F2C8C4088AD2640B7C809ED392235C0E0FD7270`. Step 8 remains deliberately
  unstarted; human acceptance/open qualitative feedback remains user-owned.
