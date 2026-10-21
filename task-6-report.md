# Task 6 — Terrain-grapple canyon automated certification

## Scope and boundary

This report records only Task 6 automation performed from an isolated materialization of
`e4e06989af73b4b98a61fbf40d3f103e1888fdcd`, canonical Task 6 candidate blobs, and the
explicitly compatible preserved overlay. It neither starts Step 8 nor creates, edits, or
substitutes a `HandsOnLauncher` report or a user qualitative note.

The candidate drives the generated five-sector route through InputMap/physics proof. It does not
teleport across traversal proof or directly invoke grapple, model, or mechanism route authority.
Presentation repositioning occurs only between completed non-timing beats.

## Automated result

- Real-input critical path proves wall, ceiling, underside, edge, Ice, Forbidden, 620-range,
  one retained-cast attached-Space boost, second-Space rejection, and no ordinary air double jump.
- It records all 15 micro-scene IDs, four catch returns, exactly four claw mechanism roles, both
  checkpoints, pre-checkpoint death back to start, Boss death/retry/completion, and v3 fall
  context. `PROLOGUE_CRITICAL_PATH_OK` occurs exactly once per formal verifier.
- Presentation records all six reticle dwells, boost start/arc/second rejection/landing,
  recovery, optional alcove, two checkpoints, every Boss attack, death/retry, completion, and
  title return. The Task 11 markers are retained alongside the Task 6 markers.
- The final focused audit used the Windows display driver, fixed 60 FPS and 1280×720; it found
  five sectors, 15 micro-scenes, four claw roles, and a 6.201 ms callback peak (< 50 ms).

## Fail-closed TDD and compatibility repairs

`tests/powershell/Task6TerrainGrappleEvidence.Tests.ps1` runs the canonical evidence contract
and focused mutations. The mutation suite fails closed for missing Task 6 markers, direct
InputMap-route bypasses, empty/wrong-spec movie evidence, fewer than 15 micro-scenes or four
roles, mandatory Dash, v3 fall-context loss, fabricated human-report content, and
performance/fatal/error/leak violations. The final Pester precheck passed 2/2; its log hash is
`15BC1E378951CB5047B3EB924540A6EBC9F1E091409438805E2B73DA84B69050`.

Two Windows-host issues were also tested before their minimal repairs: AVI header validation was
changed to streaming reads rather than loading the full movie, and SHA-256 uses
`SHA256.Create().ComputeHash` instead of the unavailable `HashData` API. A subsequent verifier
exposed a missing small-scene `Test-ExactBytes` comparator after the AVI refactor; its RED was
recorded, the comparator was restored, Pester was green again, and the final capture was made
only after the repaired runtime fingerprint was stable. An explicit-audit-root mutation likewise
proved that capture refuses a missing or reused audit directory.

## Final recordings and review

Final capture root:
`artifacts/task6-final-evidence/capture-final-working-20260901T121429023Z`

| Evidence | SHA-256 | Frames | Manifest SHA-256 |
| --- | --- | ---: | --- |
| Capture bundle | `062E760BF5E61087561B5190249C042F939035A7B2543EC095F80A7C7F8A42F0` | — | capture manifest |
| Full-flow MJPEG AVI | `9E3C73AC42DAE516653AE292D011F5AA7A3C6A7CE4434B3A020E8F77C5FB13AD` | 11,443 | `F977AA9AE98088BE4C25696716589AB0306B3C3460EDD516FC5AABA0D51FA760` |
| Full-flow reticle set | same full-flow AVI | 11,443 | `3CDA77620453B19327AA16B53932D669CBCD446FFC3885B95EC8F9234EFED684` |
| Boss-retry MJPEG AVI | `90F38361846E494532679284CCBF5683E40F64CE57092D34585DF4B7E6494C81` | 2,719 | `6509775F406DF15C62AE728720E3E0054517C1975F80D8EA3578C8469FD015A0` |

All 36 extracted PNGs were opened individually at original resolution. They show live,
nonblank/nonfrozen title, HUD, terrain attachments, six distinct reticles, recovery, optional
alcove, both checkpoints, Boss attack phases, live death/retry, completion, and title return.
The detailed per-frame conclusions are in
`docs/playtests/2026-08-29-terrain-grapple-dense-canyon-validation.md`.

## Two fresh formal verifiers

Both independent verifier directories bind the same runtime/capture candidate
`9FC8BDAE4B2947D834AF6ECA4981E538C29F5911BF2A98A27483B3FFF8624D9A`, evidence candidate
`3876653E541A024CAA29669C618A834BD5C84886123A18BC947FD142B008B6A3`, and restored generated
scene hash `0F7BF41A79544BB8A37BAA66AB7927DD8DFFE75B346E681BBABED205660B50D1`.

| Pass | Fresh verification root | Result |
| --- | --- | --- |
| 1 | `artifacts/task6-final-evidence/verify-final-working-1-20260901T122127595Z` | Exactly one `PROLOGUE_VERIFY_OK`; build 0 warnings / 0 errors; critical path 10,202/12,000 frames; all gates green. |
| 2 | `artifacts/task6-final-evidence/verify-final-working-2-20260901T122425864Z` | Exactly one `PROLOGUE_VERIFY_OK`; build 0 warnings / 0 errors; critical path 10,208/12,000 frames; all gates green. |

Each `verify-summary.txt` has SHA-256
`5BC28F421A196188BE1FDB0334333880F2C6B8CCDC3CBDC5EE259F9690137B08`. Each verifier confirms
eight critical markers, 28 capture stages, six reticle stages, no fatal/error/leak signature,
and `human_acceptance=open`.

## Final overlay resolution

The final materialization preserved only compatible user-owned overlay: the two traversal test
position hunks (`new Vector2(170, 574)`), the full Task 5 Pester hunk, README state outside this
Task 6 section, and the pure audit enhancement. The source overlays containing direct
`Instantiate`, `player.Position`, `Traverse`, direct mechanical-claw authority, or route-before-
direct model/runtime calls were explicitly superseded by the InputMap-only Task 6 canonical path;
their original bytes remain in the frozen preexisting diff and manifest. The per-file three-way
resolution and SHA-256 records are in `task-6-final-overlay.manifest.txt`.

## Human gate

Automated acceptance is complete; human acceptance remains open. Step 8 has not started. A user
must separately play the normal-speed build, provide their own `HandsOnLauncher` report and
qualitative note, and only then can a proportional follow-up fix and the remaining human gate be
considered.

## 2026-09-02 review-remediation certification — supersedes prior recording references

The 2026-09-01 movie and verifier paths above are historical and are not the current
certification. Review remediation restored every BASE and retained Task 11 hard gate to
`tools/verify-prologue.ps1`, changed all AVI parsing and hashing to bounded-memory streaming,
made child-host selection work in both Windows PowerShell 5.1 and PowerShell 7, and replaced
in-memory substring mutations with isolated source/evidence materializations that actually run
the verifier/capture gates. The migration also explicitly rejects the obsolete mandatory-Dash
critical marker; Shift Dash may remain optional gameplay but is not part of this proof.

The exact runtime candidate is
`49A6E23D6782668F04EE8061E3559E2B377765EBFF0ABCAB6E3D46A97AFD612D` (39 scoped files), based on
`e4e06989af73b4b98a61fbf40d3f103e1888fdcd`. Both hosts produced that fingerprint. The clean
BASE pass is retained at
`artifacts/task6-final-base-gdignore-20260902T004409430`; it includes the restored BASE/Task 11
marker, ordered-stage, source-snapshot, forced-smoke, 7-Boss-attack, teardown, performance,
fatal/error/leak and intentional-warning gates.

### Current capture and original-resolution review

Current capture root:
`artifacts/task6-final-capture-20260902T005609288`

| Evidence | SHA-256 | Frames | Bound manifest SHA-256 |
| --- | --- | ---: | --- |
| Capture manifest | `9B3FB1B3126D27C8254F91BC4A6A0DE54C13C757075A74B26FD8D8AC9A2AACC4` | — | runtime candidate above |
| Full-flow MJPEG AVI | `6C6E5046F0653BD02C366D8CF0C8119FB5CF2971DC83B51068BA2DA5A62B48E5` | 4,053 | `351921FACDC603B277C872A653EEF34B988FCCD31711B8E75CA7007AA5EB805D` |
| Full-flow reticle set | same full-flow AVI | 4,053 | `52AC04E88447A337DF674023AC9DEFDAB63774EB5F375D1121B5BDD85CAE79AA` |
| Boss-retry MJPEG AVI | `FDF14343CF0A5C2755B076597DF2BDB5C526B77B64382A56959173409F0F261C` | 2,719 | `C93E1805E38B546B12E42F91392155B41465CB904AB03721668475CE796EBF18` |

The audit was Windows/display-backed at fixed 60 FPS and 1280×720; it found five sectors,
15 micro-scenes, exactly four claw roles, and a 6.614 ms callback peak. The full-flow timeline
has 30 ordered stages, 4,053 frames, an 84-frame title tail, six 18+ frame reticle dwells, and
independent post-draw/motion-bound boost and Boss frames. I opened all 46 exported PNGs at their
native 1280×720 resolution: 38 full-flow/adjacent-motion frames, six reticles, and two Boss-retry
frames. They are readable, nonblank, nonfrozen, and match their named state; the detailed frame
ledger is in the validation record.

### Actual RED/GREEN and final verifier pair

The formal Pester run at `artifacts/task6-final-pester-20260902T010329439` passed 5/5 tests and
15/15 actual isolated mutations. Each mutation launched the child contract against a modified
temporary source/evidence root, returned nonzero for its expected label, and restored the
canonical source hash. The fifteen risk categories cover terrain marker loss, 15→14,
four→three roles, old/mandatory Dash, second-boost rejection, Task 11 input/mouse coverage,
BASE ordered-stage deletion/reorder, Boss Hammer Throw, thread-death cleanup, budget,
fatal/error/leak injection, AVI/header/manifest corruption, and missing/duplicate terminal
marker cases. Host probes passed under both Windows PowerShell and PowerShell 7.

| Pass | Fresh verification root | Exact result |
| --- | --- | --- |
| 1 | `artifacts/task6-final-verify-1-20260902T010601067` | Exactly one `PROLOGUE_VERIFY_OK`; build warnings/errors `0/0`; no fatal/error/leak; scene SHA `9C81ABD51F8C834295AF03FD7F2C8C4088AD2640B7C809ED392235C0E0FD7270`; summary SHA `BA94961F9965D0A2FE7191B08EA7B3F7947D244C91F3254A147DE5C5C5B0C9AA`. |
| 2 | `artifacts/task6-final-verify-2-20260902T012103825` | Exactly one `PROLOGUE_VERIFY_OK`; build warnings/errors `0/0`; no fatal/error/leak; same candidate, scene and summary SHA as pass 1. |

Documentation is intentionally outside the runtime candidate fingerprint; it records this
completed automated proof without changing production, capture, verifier, or test bytes.
Human acceptance remains open. Step 8 has not started, and this automation did not create or
claim a `HandsOnLauncher` report or a user qualitative note.
