# Task 8 Fix Round 2 Report

Base: `22fbb3d8ffb3931178acb665ed800e0055b5def2`.

## RED → GREEN

- RED: `G:\godot\dotnet-sdk\dotnet.exe build Knight.csproj --no-incremental; G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --script res://tests/RunTask8Fix1Tests.cs` failed with `Ascent local bottom x=50 ... uncovered`.
- GREEN: affected builders report `BuildCanyonDescent nodes_before=80 nodes_after=80` and `BuildCanyonAscent nodes_before=87 nodes_after=87`; focused run ends `TASK8_FIX1_FOCUSED_OK` with `TASK8_FIX2_STRUCTURE_OK`, real GripStone cast/latch/catch-zone landing, and all 33 traversal samples.

## Scope

`AddFallRecovery` now classifies an unambiguous edge with a named 24px tolerance, opens only actual bottom seams, fails closed at corners, and checks complement coverage. Ascent recovery physically falls through the real `Area2D` body path at local x=50. The second-Space probe captures the current attached state, exact locked target/cast, held LMB, same-cast release on the following frame, and zero jump impulse. GripStone now reels/swallows the launch support and lands in named `GripStoneCatchZone`.

Exact generated-main index blob: `2b5532a3047ca0b07adbc5bfcc938ff900fd41c4`. User worktree main restored SHA-256: `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`.

Historical concern, superseded by the final scoped re-review correction below: the earlier
dedicated recovery fixture intentionally started in Title and emitted `Title->Recovering`
warnings. The current final fixture instead uses normal autoplay and treats that transition as a
failure. The GPU proof was recorded before the later geometry-identity verification.

## Completion evidence

Fresh exact build and import succeeded: `G:\godot\dotnet-sdk\dotnet.exe build Knight.csproj --no-incremental` (0 warnings/errors) and the Godot headless import ended `VERIFICATION_EDITOR_GC_DRAIN_OK`. The final focused run ended `TASK8_FIX1_FOCUSED_OK`; its 33 route samples report `second_air_jump=0`, its generated direct fall emits the real `KillZone.OnBodyEntered` path, and `TASK8_GRIPSTONE_OK casts=1 latches=1 landing=(304.27887,587.96716)` confirms the named catch landing.

The exact index main was materialized only for verification, then restored. The headless boundary ended `TASK8_GENERATED_MAIN_HEADLESS_OK` after the Entrance→Descent seam. Windows/NVIDIA (`RTX 3060 Laptop GPU`) fixed-60 proof ended `TASK8_GENERATED_MAIN_PROOF_OK resolution=1280x720 fixed60=True display=Windows capture=True` and `TASK8_GENERATED_MAIN_ROUTE_OK` for all five sectors, all four seams, Bell, and physical Boss trigger (`respawns=0`, `mounted_max=3`, `max_step=16.80`).

Fresh proof: `artifacts/task8-fix2-proof/final-generated-main-route.avi`, 1280×720, 60 FPS, 2,469 frames, 41.15 s, 118,369,206 bytes, SHA-256 `2E330EEF1A770BFF91918B5DFF0D84ED7BDC8DC7C0B748A99EF64A06BDB725DB`; MP4 is 2,183,481 bytes, SHA-256 `0C1C14723A49CA374A2E1B1BD898A7C1B6C5836A58D52C14FADDDF37D2970C4F`. Six new PNGs plus `review/final-contact-sheet.png` and `review/final-static-run.png` were generated; the contact sheet was visually inspected: no blank camera, teleport/respawn, stuck frame, or invisible route was observed. Contact sheet SHA-256 `173B306A00F89B8E8A4504F7F7F6BC001211E3EDD0464C65F761221F17DC361A`; static SHA-256 `390DC973F4C3ABC7384D4E1ABAADDF4754C841B4144C974647F4D7C65A2B6C3F`.

The only user worktree main change remains intentionally unstaged; SHA-256 is restored to `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`. The staged/committed generated blob is `2b5532a3047ca0b07adbc5bfcc938ff900fd41c4`.

## Final exact regression pass

The index main was materialized with `git checkout-index --force -- scenes/prologue/PrologueLevel.tscn` (`2b5532a3047ca0b07adbc5bfcc938ff900fd41c4`; materialized SHA-256 `3E55D713486553899C674E66F529D1726A743B75E319AF8BF27483115AFB447C`) before the final commands, then the protected user copy was restored.

- `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --script res://tests/RunPrototypeTests.cs` completed cleanly with `PROTOTYPE_TESTS_OK` (`artifacts/task8-fix2-gates/prototype.stdout.log`).
- `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=pre` completed cleanly with `PROLOGUE_TEST_PHASE_OK phase=pre` (`artifacts/task8-fix2-gates/prologue-pre.stdout.log`).
- The same approved executable with `-- --phase=post` completed cleanly with `PROLOGUE_TEST_PHASE_OK phase=post` and `PROLOGUE_TESTS_OK` (`artifacts/task8-fix2-gates/prologue-post.stdout.log`). Its direct canyon coverage includes `TASK8_CANYON_STRUCTURE_OK sectors=5 segments=11`, `TASK8_GRIPSTONE_OK casts=1 latches=1 landing=(304.27887,587.96716)`, all 33 `TASK8_ROUTE_SAMPLE_OK` entries (`second_air_jump=False`), and `TASK8_CANYON_TRAVERSAL_OK segments=11 samples_each=3 second_air_jump=0 dash=0`.

The combined final logs contain zero `ERROR:`/`FATAL:`, `Handle is not initialized`, ObjectDB/resource/memory leak signatures. Six expected negative-fixture warnings remain: two presentation-root validation warnings in pre and four intentional observer/profile/guidance/flow warnings in post; none are a native/runtime failure. The focused exact-index run remains the direct proof of the x=50 real Area/body recovery and current-attached same-cast Space release while LMB is held (`TASK8_FIX2_STRUCTURE_OK` and `TASK8_FIX1_FOCUSED_OK` in `artifacts/task8-fix2-final-focused4.log`).

Fresh local proof artifact hashes: `01-entrance.png` `0694D74125038B5EB5A203087B52CD92D361AD6B6C087DB3CFBB124ABCE8AC23`; `02-descent.png` `2744248A103DACCE08885D969C0D4F75BEBB9DE66156B6310FDB4FD7F61559F2`; `03-basin.png` `24605D8D851AE44708FB7D1C4BA76323F2F71346C6522C32FE3379E1C0F12F4C`; `04-ascent.png` `805DA12909031CD5A0DBB314145B68F0FC0CA6F6CEA33CB25741BD1ADDD4B873`; `05-temple.png` `63AC1C04A5A10027CC7BA938F3F4604AEE744CDAB72A107D97684519736A9D12`; `06-boss-trigger.png` `C3722736F03C647E2AA8BBC0F4616969EA68C9D2B858E6243A5ACCFA4A0906D4`. The AVI/MP4/contact-sheet/static hashes are recorded above; all are fresh from the final generated geometry.

## Review-correction completion (Fix Round 2, immutable-history addendum)

The independent review rejected the earlier evidence because it did not prove the bounded classifier, airborne edge, physical complement, recovery completion, GripStone safety margin, measured GPU recovery counters, and all-frame audit strongly enough. The historical commits are deliberately retained, not rewritten: `a627577` (initial implementation), `4071f7f` (initial report), `16284f6` (completion documentation), `fcd879f` (final regression documentation), and `421017d` (this review-correction implementation/tests). The aggregate immutable range is `22fbb3d8ffb3931178acb665ed800e0055b5def2..421017d`; the documentation completion commit follows this report update. The cost is additional auditable commits beyond the original two-commit expectation, rather than destructive history changes that would erase the rejected evidence.

### Three actual RED runs, before the corresponding GREEN implementation

All three used the approved console executable `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path .` and exited 1 with the asserted old behavior:

- `--script res://tests/RunTask8Fix2SeamClassifierRed.cs` → `TASK8_FIX3_SEAM_CLASSIFIER_RED_PROBE outside=True corner=False outside=(-25, 1439) corner_pos=(2, 1438)`, then `a bottom-near marker outside the bounded tangential X range must never carve recovery` (`artifacts/task8-fix3-red/seam-classifier-red.log`). The corner was already fail-closed by the old cumulative test; the actual regression was the tangential-outside false carve.
- `--script res://tests/RunTask8Fix2AirborneRed.cs` → `TASK8_FIX3_AIRBORNE_EDGE_RED_PROBE state=Latched cast=1 left=True on_floor=True accepted=True`, then `attached cumulative cast on floor must be rejected` (`artifacts/task8-fix3-red/airborne-edge-red.log`). This is a real E + held-LMB attach, not a reflected/reset claw state.
- `--script res://tests/RunTask8Fix2GripStoneRed.cs` → `TASK8_FIX3_GRIPSTONE_MARGIN_RED_PROBE player=(253, 588) catch_left=260.0 accepted=True`, then `edge overlap must not satisfy the GripStone safety oracle` (`artifacts/task8-fix3-red/gripstone-red.log`). This preserves real cast/latch/departure physics.

### Corrected behavior and narrow GREEN proof

- `AddFallRecovery` now chooses exactly one nearest valid bounded edge: normal and tangential tolerances are 24 px; tangential coordinates must be within the named edge bounds plus tolerance; nearest ties within 0.01 fail closed. The final classifier command above now exits 0 with `TASK8_FIX3_SEAM_CLASSIFIER_PROBE outside=False corner=False ...` and `TASK8_FIX3_SEAM_CLASSIFIER_OK` (`artifacts/task8-fix3-green/final-seam-classifier.log`). Real Ascent left seams remain rejected and the true Descent bottom seam remains open.
- The final airborne command exits 0 with `TASK8_FIX3_AIRBORNE_EDGE_PROBE state=Latched cast=1 left=True on_floor=True accepted=False` and `TASK8_FIX3_AIRBORNE_EDGE_OK` (`artifacts/task8-fix3-green/final-airborne-edge.log`). In the full route, immediately before the actual second Space InputMap edge each sample asserts `!player.IsOnFloor()` while the live frame is attached and LMB is held; the next physics frame holds LMB, accepts no upward impulse, and observes same-cast Released/Retract before mouse release. Final route marker: `TASK8_ATTACHED_SECOND_SPACE_OK ... airborne_edge=True same_cast_release=True`.
- The GripStone margin command exits 0 with `TASK8_FIX3_GRIPSTONE_MARGIN_PROBE player=(253, 588) catch_left=260.0 accepted=False` and `TASK8_FIX3_GRIPSTONE_MARGIN_OK` (`artifacts/task8-fix3-green/final-gripstone-margin.log`). The real departure/catch requires a `GripStoneCatchZone` interior inset of player radius 20 px plus 12 px safety margin and a named 48 px headroom check; final marker is `TASK8_GRIPSTONE_OK casts=1 latches=1 landing=(304.27887, 587.96716) inset=32 headroom=48`.
- Independent authored opening intervals—not the production classifier or in-memory build plan—are compared with actual packed `Area2D/CollisionShape2D` extents over all five sectors. The final exact focused log records `TASK8_FIX3_PHYSICAL_COMPLEMENT_OK sectors=5 descent_opening=1004..1196`.
- The Ascent x≈50 probe has no post-setup teleport/controller toggle. It starts the actual body in the real Area path, waits for body entry, `RecoveryRequested`, and `RecoveryCompleted`, then requires live return to the fixture round start and active player. Final exact marker: `TASK8_FIX3_RECOVERY_COMPLETION_OK request=CanyonAscent completion=CanyonAscent fall_local=(50, 1405.302) player=(2930, 2080)`.

### Final proportional matrix (exact generated main materialized, then user copy restored)

- `G:\godot\dotnet-sdk\dotnet.exe build Knight.csproj --no-incremental` → 0 warnings, 0 errors (`artifacts/task8-fix3-green/final-dotnet-build.log`).
- `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path . --editor --quit` → `VERIFICATION_EDITOR_GC_DRAIN_OK` (`final-godot-import.log`).
- `... --script res://tests/RunPrototypeTests.cs` → `PROTOTYPE_TESTS_OK` (`final-prototype.log`).
- `... --script res://tests/RunPrologueTests.cs -- --phase=pre` → `PROLOGUE_TEST_PHASE_OK phase=pre` (`final-prologue-pre.log`).
- `... --script res://tests/RunPrologueTests.cs -- --phase=post` → `PROLOGUE_TEST_PHASE_OK phase=post` and `PROLOGUE_TESTS_OK` (`final-prologue-post.stdout.log`). Its direct route contains all 33 `TASK8_ROUTE_SAMPLE_OK` samples, `TASK8_GRIPSTONE_OK`, and `TASK8_CANYON_TRAVERSAL_OK`.
- `... --script res://tests/RunTask8Fix1Tests.cs` → `TASK8_FIX3_SEAM_CLASSIFIER_PROBE outside=False corner=False`, `TASK8_FIX3_PHYSICAL_COMPLEMENT_OK`, `TASK8_FIX3_RECOVERY_COMPLETION_OK`, and `TASK8_FIX1_FOCUSED_OK` (`final-focused.stdout.log`).
- Exact generated-main headless proof records `TASK8_GENERATED_MAIN_HEADLESS_OK` and measured `recovery_requested=0 recovery_completed=0` (`generated-main-headless.log`). The authoritative Windows/NVIDIA RTX 3060 Laptop GPU 1280×720 fixed-60 route records `TASK8_GENERATED_MAIN_ROUTE_OK ... recovery_requested=0 recovery_completed=0 boss_trigger=True capture=True` and `TASK8_GENERATED_MAIN_PROOF_OK resolution=1280x720 fixed60=True display=Windows capture=True` (`generated-main-gpu.stdout.log`). `RouteMonitor` subscribes to real production `RecoveryRequested`/`RecoveryCompleted`, replaces the former literal `respawns=0`, and disposes its subscriptions.

The historical selected logs contained no unexpected `ERROR`, `FATAL`, leak, Handle, or
native-runtime failure signature. This older paragraph described an earlier dedicated-fall
fixture; the final warning-accounting correction below is authoritative for the current source:
two pre warnings, four post stderr negative-fixture warnings, and zero focused
`Title->Recovering` rejection. All final commands exited cleanly.

### Generated identity, retained visual proof, and frame audit

The builders were rerun with node counts preserved: Descent 80→80, Ascent 87→87, and main validation passed. Raw serializer UUIDs changed, but UID-normalized bytes are exactly equal to the committed/index outputs, so no visible/physical geometry changed and the previously fresh continuous recording is reused rather than falsely re-recorded:

- Ascent canonical SHA-256: `1EC4F2DAD92B1167DAF954F08E20C330EE51A1977A8718BEF45098A51F4DE528`.
- Descent canonical SHA-256: `C2CBF8D804533B335EFCE2C753ACC258D0E7DA9C929821F6EFB566F76B9EA864`.
- Main canonical SHA-256: `7EFD9CD10E34F17021DD8E3C9701CE2866FF52EACC80C148A5D8B87F3752BFE5`.
- Exact committed/index blobs remain main `2b5532a3047ca0b07adbc5bfcc938ff900fd41c4`, Ascent `cdedc8bca1cfbcb3c2927069504e590acad4c783`, and Descent `6ccfa552d064e868920490626d416697c5d7b6b1`.

The reused continuous AVI is `artifacts/task8-fix2-proof/final-generated-main-route.avi`: 118,369,206 bytes, 1280×720, 60 fps, 2,469 frames, 41.15 s, SHA-256 `2E330EEF1A770BFF91918B5DFF0D84ED7BDC8DC7C0B748A99EF64A06BDB725DB`. Its practical MP4 is 2,183,481 bytes, SHA-256 `0C1C14723A49CA374A2E1B1BD898A7C1B6C5836A58D52C14FADDDF37D2970C4F`. The six stills and contact sheet/static review retain the hashes above. All 2,469 AVI frames decoded through ffmpeg framemd5 (`all-frames.framemd5`, SHA-256 `670E243BCA8881A48EC5D26866EC3CDE4988DFA9E890EE27620E09683993AC0D`); decoder errors and blank frames are both zero. The audit (`analysis-summary.log`, SHA-256 `3D25922AAC6ED5725208582975605420BC160BD3641CBC0FA1CEEF4C177F8B6C`) finds three exact duplicate runs, longest 20 frames/0.333 s, and a 16.05–18.90 s near-static interval at `freezedetect n=0.001,d=0.5` (largest reported segment 1.48333 s). Visual samples at 16.05/17.50/18.88 seconds show changing heat/energy HUD, so this interval is honestly classified as live UI recovery activity—not a blank, teleport, or stuck-camera failure.

### Exact boundary and preservation ruling

`421017d` changes only these nine implementation/test paths: `tools/build/prologue/PrologueCanyonBuildParts.cs` (bounded classifier); `tests/integration/PrologueCanyonFixRound2Tests.cs` (independent physical complement and real completion); `tests/integration/PrologueCanyonFixRound3Tests.cs` and three `tests/RunTask8Fix2*Red.cs` runners (real RED/GREEN evidence); `tests/integration/PrologueCanyonTraversalTests.cs` (airborne exact-edge and safe GripStone); `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs` (measured recovery counters); and `tests/RunTask8Fix1Tests.cs` (focused inclusion). No generated scene was staged because canonical generated bytes are equal; the builder source and actual packed geometry were both verified. The protected user worktree `scenes/prologue/PrologueLevel.tscn` is again exactly SHA-256 `AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`, while its committed/index blob remains `2b5532a3047ca0b07adbc5bfcc938ff900fd41c4`. All unrelated dirty/untracked files, including the other SDD ledger, remain unstaged.

## Final scoped re-review correction — reflection-free normal-flow evidence

This addendum corrects the historical fixture descriptions above: the final recovery fixture
does not begin in `Title`, does not tolerate `Title->Recovering`, and the GripStone margin RED is
not an initial-overlap predicate check. Historical evidence is retained; these are the final
facts for the reviewed source and the log paths named below.

### Exact RED → GREEN evidence

- **Classifier API RED:** `G:\godot\dotnet-sdk\dotnet.exe build Knight.csproj --no-incremental`
  exited 1 with `CS0117: PrologueCanyonBuildParts does not contain a definition for
  IsUnambiguousBottomRecoverySeam` (`artifacts/task8-fix4-red/seam-internal-api-red.log`).
  GREEN exposes only the narrow `internal static bool
  IsUnambiguousBottomRecoverySeam(Vector2, Rect2)` builder seam; it delegates to the unchanged
  private classifier and exposes no gameplay control. The final approved-console runner
  `--headless --path . --script res://tests/RunTask8Fix2SeamClassifierRed.cs` exits 0 with
  `TASK8_FIX4_SEAM_CLASSIFIER_PROBE outside=False corner=False outside=(-25, 1439)
  corner_pos=(2, 1438)` and `TASK8_FIX3_SEAM_CLASSIFIER_OK`
  (`artifacts/task8-fix4-green/final-seam.log`). `PrologueCanyonFixRound3Tests` contains no
  `System.Reflection` import or private-method invocation.
- **Normal-flow recovery RED:** the deliberately old fixture disabled `HeadlessAutoStart` and
  directly activated gameplay; its approved-console run exited 1, emitted the unwanted
  `Rejected prologue flow transition Title->Recovering`, and failed its normal-flow assertion
  (`artifacts/task8-fix4-red/recovery-title-shortcut-red.log`). GREEN leaves autoplay enabled,
  arranges only the pre-tree sector/spawn fixture, waits for real public autoplay to reach
  `Playing`, then uses the actual Ascent x=50 `Area2D.BodyEntered` path. The final focused
  exact-index run exits 0 with `TASK8_FIX4_RECOVERY_FLOW_OK
  states=Playing>Recovering>Playing request=CanyonAscent completion=CanyonAscent
  fall_local=(50, 1405.302) player=(2930, 2080) rejected=`
  (`artifacts/task8-fix4-green/focused-final.stdout.log`). It asserts the live current sector,
  production request/completion, active player, round-start relocation, and an empty rejected
  transition; it makes a Title transition a failure rather than a tolerated warning.
- **Physical GripStone RED:** after a real `E` mode action, held-LMB cast/latch, launch-support
  departure, and physics settle, the explicit legacy runner exits 1 only by design with
  `TASK8_FIX4_GRIPSTONE_MARGIN_RED_PHYSICAL casts=1 latches=1 departed=True
  landing=(304.27887, 589.10974) catch_left=311.0 legacy_accepted=True safe_accepted=False`
  (`artifacts/task8-fix4-red/gripstone-physical-margin-red.log`). The dedicated runner is not
  part of a production suite. The focused and post GREEN execute the same physical fixture and
  emit `TASK8_FIX4_GRIPSTONE_MARGIN_GREEN_PHYSICAL` with the same cast/latch/departure/landing
  facts and assert that the old standing oracle accepts while the collision-radius + 12px safety
  margin + 48px headroom contract rejects.

The final route also retains all 33 real-input samples. Every actual second-Space edge has
`!player.IsOnFloor()` immediately before the InputMap action, a live Latched/Reel/Swing frame
and held LMB; the next physics frame retains LMB, sees no upward impulse, and observes same-cast
Released/Retract before mouse release. The final focused marker is
`TASK8_ATTACHED_SECOND_SPACE_OK ... airborne_edge=True same_cast_release=True`; the separate
adversarial floor-contact runner exits 0 only after proving `on_floor=True accepted=False`
(`artifacts/task8-fix4-green/final-airborne.log`).

### Final proportional gates and warning accounting

The indexed main was materialized only for the gates, then the protected user copy was restored.

- `G:\godot\dotnet-sdk\dotnet.exe build Knight.csproj --no-incremental` → 0 warnings, 0 errors
  (`artifacts/task8-fix4-green/focused-final-build.log`).
- `G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe
  --headless --path . --editor --quit` → `VERIFICATION_EDITOR_GC_DRAIN_OK`
  (`artifacts/task8-fix4-green/final-godot-import.log`).
- The same approved executable with `RunPrototypeTests.cs` → `PROTOTYPE_TESTS_OK`
  (`artifacts/task8-fix4-green/final-prototype.log`).
- `RunPrologueTests.cs -- --phase=pre` → `PROLOGUE_TEST_PHASE_OK phase=pre`
  (`artifacts/task8-fix4-green/final-prologue-pre.log`).
- `RunPrologueTests.cs -- --phase=post` → `PROLOGUE_TEST_PHASE_OK phase=post` and
  `PROLOGUE_TESTS_OK` (`artifacts/task8-fix4-green/final-prologue-post.log`); it includes the
  five-sector structure and traversal suites plus the physical margin GREEN.
- `RunTask8Fix1Tests.cs` → `TASK8_FIX1_FOCUSED_OK`
  (`artifacts/task8-fix4-green/focused-final.stdout.log`).

Final focused/prototype/post/seam/airborne logs have zero `ERROR`, `FATAL`, leak, Handle, or
native-runtime signatures. Warning accounting is deliberately split by process: pre has exactly
two expected `PresentationProfile` negative-fixture warnings (`VisualScene root must implement
IEntityVisual2D` and `cue 'Hurt' root must be Node2D`); post has exactly four expected
negative-fixture warnings in its stderr—intentional throwing claw observer, RoomPresentationProfile
root validation, missing `CourtyardToCorridor` guidance target, and intentional rejected
`Completed->Recovering` flow transition. They are expected test fixtures, not failures. The final
focused real recovery fixture has zero `Title->Recovering` rejection and proves
`Playing>Recovering>Playing`; it does not make the post `Completed->Recovering` negative fixture
absent.

Warning-bearing artifact paths and SHA-256: the pre invocation was originally run with PowerShell
`*>`, so no separate `artifacts/task8-fix4-green/final-prologue-pre.log.stderr` exists; its
combined stdout/stderr log is `artifacts/task8-fix4-green/final-prologue-pre.log`, SHA-256
`2FA0FE6C550FD9362DC296775471801BBAE3D56851B48705B9F512E6600E8F84`, and contains exactly those
two warning lines. Post has the separately captured warning-bearing stderr
`artifacts/task8-fix4-green/final-prologue-post.log.stderr`, SHA-256
`370329C051D0071FE3192D945BEC1566275810E85BC907E6DA52EA916B7C53D6`, containing exactly the
four warnings listed above. Post stdout remains
`artifacts/task8-fix4-green/final-prologue-post.log`, SHA-256
`6BFEC8260401A800A56B670AA3C196AA66FDF5ED02A9401CFA74FC05D25D42D3`.

### Geometry/proof identity and preservation

No production geometry or generated-main route-proof code changed in this correction, so the
fresh final GPU/video evidence is validly reused rather than re-recorded. Rerunning Descent,
Ascent, and main builders kept node counts 80→80 and 87→87; canonical normalized hashes of the
working output equal the index exactly: Descent
`F7A9F1254FFED4F4A1515E5117190353185149CCFC70B7D343E332B249354F25`, Ascent
`E13A722EE88B1311DE83F3E1DC5FAAE58250A703C54E05A45AD5A98BFDE0A703`, main
`B6CAB125CBC43CB4BA69FB38097A9A0789707BA917989BE065C8D1E762F2A7A6`.
The committed/index blobs are still main `2b5532a3047ca0b07adbc5bfcc938ff900fd41c4`, Ascent
`cdedc8bca1cfbcb3c2927069504e590acad4c783`, and Descent
`6ccfa552d064e868920490626d416697c5d7b6b1`. Therefore the retained GPU 1280×720 fixed-60
route/video and 2,469-frame audit cited above remain byte- and geometry-fresh for this source.
The restored user `PrologueLevel.tscn` SHA-256 is
`AE09F35C5A9C8A65AC18C2933973D5EA0BED061F7424BEB24BEB092A45D77115`.

### Immutable boundary

The immutable reviewed chain remains `a627577`, `4071f7f`, `16284f6`, `fcd879f`, `421017d`,
and `58126b8`; implementation commit `4ac68e4` follows `58126b8`, and this documentation-only
commit follows `4ac68e4`, without rewriting any prior evidence. The implementation boundary is
exactly five paths: builder classifier seam; Round2 recovery flow; Round3 classifier adversary;
traversal physical GripStone adversary; and the explicit legacy GripStone RED runner. This
documentation commit updates only this report and the correct 2026-08-26 plan ledger. The cost
is two more auditable commits beyond the initially planned pair; it preserves rejected evidence,
warnings, and prior reports instead of destructive history edits.

### Log integrity hashes

The local, gitignored evidence logs are reproducible from the commands above and have these
SHA-256 values: seam API RED `97678D0FCA58CFAC2E8227D302CC9868B131C63547277F8094069A8E62FBECD1`;
normal-flow recovery RED `211A4090CCEC10BC05567613B6B97E1CACF736F946FCDB125FC3BCCA3345C879`;
physical GripStone RED `E8CBC3027AD1D9C228389E35FABDA564DF561910CD2A4687F6916DD30669D1AE`;
focused GREEN `87AC323FA41709475F2D2CE1ED6380FD0E5AB8F7F6037AE539A67657F85CBC8D`;
Prototype `8F9196C1F953BEBD38D01833F57BE2144F8516FFBBAA4260CA2A439D4D1AAB5C`;
pre `2FA0FE6C550FD9362DC296775471801BBAE3D56851B48705B9F512E6600E8F84`;
post `6BFEC8260401A800A56B670AA3C196AA66FDF5ED02A9401CFA74FC05D25D42D3`;
seam GREEN `80475064C3EC5D01A3120D93FF7395D55EDE5F0A136B2671B7251F71FCF7A8ED`;
and airborne adversary GREEN `6AB161AB810DA728699F58818D9ED5BFD30397F1570359FFB5DF11B37D3E914D`.

### Warning-accounting documentation boundary

The next immutable commit after documentation `402698f` is docs-only warning accounting. Its
boundary is exactly this report and the correct 2026-08-26 plan ledger; it changes no code, test,
scene, generated output, or gate result. It records the actual 2-pre/4-post warning split and
the distinction between the expected post `Completed->Recovering` fixture and the focused real
recovery's zero `Title->Recovering` rejection.
