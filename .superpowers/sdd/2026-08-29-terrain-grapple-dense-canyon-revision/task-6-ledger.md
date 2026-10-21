# Task 6 execution ledger — revised critical path and proof gate

## Isolation and protected overlap

- Baseline/candidate source: `e4e06989af73b4b98a61fbf40d3f103e1888fdcd` plus Task 6 blobs,
  materialized at `artifacts/task6-final-evidence/final-materialization-head-task6-overlay`.
- The shared worktree was never treated as a green dependency. Its Task 6 freeze has diff SHA-256
  `11506B411AB36C79F52CC7E85CFA7F2EECE55A785F31543928A0B8725DC27524` and manifest SHA-256
  `2C3E68AE02FF0367B112512A33C9460719CDB49C60BF4770CB853A0D42D4F665`.
- The final compatible overlay preserves the two shared `PrologueCanyonTraversalTests.cs`
  position hunks and the full Task 5 Pester user hunk. The final overlay manifest records its
  SHA-256 source, Task 6, and final blobs. Direct route-authority source overlays were marked
  superseded rather than imported, because they conflict with the Task 6 InputMap-only proof.
- No external Godot process was terminated. Generated scenes were regenerated leaf-to-main only
  inside the candidate and verified restored by hash.

## RED/GREEN evidence

- The Task 6 Pester contract and focused mutations are fail-closed for each new marker, direct
  route bypasses, movie specification/frozen evidence, 15 micro-scenes, four mechanisms,
  mandatory Dash, v3 fall data, human-report boundary, and performance/fatal/error/leak gates.
- Windows compatibility RED/GREEN repairs covered streaming AVI parsing, `SHA256.Create()` for
  Windows PowerShell, explicit fresh audit roots, and restoration of the byte comparator used for
  generated-scene snapshots. Each repair was followed by canonical Pester green and a fresh
  capture when a runtime source changed.
- Final Pester preverify: 2/2 green; log SHA-256
  `15BC1E378951CB5047B3EB924540A6EBC9F1E091409438805E2B73DA84B69050`.

## Final automated evidence

- Audit root: `audit-final-working-20260901T121429023Z`; Windows, fixed 60, 1280×720,
  five sectors, 15 micro-scenes, four claw roles, peak callback 6.201 ms, audit manifest
  `84CB6702976C74FA194AF82AC2AC63CAB10503B9A9A0A598A2DF5AAC107B4357`.
- Capture root: `capture-final-working-20260901T121429023Z`; capture manifest
  `062E760BF5E61087561B5190249C042F939035A7B2543EC095F80A7C7F8A42F0`.
- Runtime/capture candidate: `9FC8BDAE4B2947D834AF6ECA4981E538C29F5911BF2A98A27483B3FFF8624D9A`.
- Full-flow AVI: SHA-256 `9E3C73AC42DAE516653AE292D011F5AA7A3C6A7CE4434B3A020E8F77C5FB13AD`,
  11,443 frames. Boss-retry AVI: SHA-256
  `90F38361846E494532679284CCBF5683E40F64CE57092D34585DF4B7E6494C81`, 2,719 frames.
- All 36 extracted PNGs were opened at original resolution; their conclusions are recorded in the
  validation document and show no blank/frozen/teardown presentation.

## Two-verifier gate

| Pass | Fresh root | Outcome |
| --- | --- | --- |
| 1 | `verify-final-working-1-20260901T122127595Z` | `PROLOGUE_VERIFY_OK` exactly once; build 0/0; critical path uses 10,202/12,000 frames. |
| 2 | `verify-final-working-2-20260901T122425864Z` | `PROLOGUE_VERIFY_OK` exactly once; build 0/0; critical path uses 10,208/12,000 frames. |

Both summary files hash to
`5BC28F421A196188BE1FDB0334333880F2C6B8CCDC3CBDC5EE259F9690137B08`, bind evidence candidate
`3876653E541A024CAA29669C618A834BD5C84886123A18BC947FD142B008B6A3`, the same capture and scene
hashes, and reject fatal/error/leak output. The two-pass count is complete.

## Remaining user-owned gate

Step 8 is deliberately not started. Human acceptance is open until a user performs the
normal-speed playthrough and supplies their own `HandsOnLauncher` report and qualitative note.

## 2026-09-02 review-remediation ledger — current automated result

All preceding final-capture and verifier entries in this ledger are historical. The review fix
restored BASE plus retained Task 11 hard gates to the Task 6 verifier, switched AVI validation
and hashing to bounded-memory streams, and makes child-host execution compatible with Windows
PowerShell 5.1 and PowerShell 7. It also replaced source-string self-certification with actual
isolated source/evidence mutations. No production gameplay authority was added: critical and
presentation proof remain InputMap-driven, while the retired mandatory-Dash critical marker is
explicitly rejected.

| Gate | Current path / result |
| --- | --- |
| BASE hard gates | `artifacts/task6-final-base-gdignore-20260902T004409430`; all restored BASE and Task 11 gates green. |
| Runtime fingerprint | `artifacts/task6-final-fingerprint-20260902T005536978`; Windows PowerShell 5.1 and PowerShell 7 both report `49A6E23D6782668F04EE8061E3559E2B377765EBFF0ABCAB6E3D46A97AFD612D` for 39 scoped files. |
| Capture | `artifacts/task6-final-capture-20260902T005609288`; Windows fixed-60/1280×720, five sectors, 15 micro-scenes, exactly four claw roles, 6.614 ms peak; manifest `9B3FB1B3126D27C8254F91BC4A6A0DE54C13C757075A74B26FD8D8AC9A2AACC4`. |
| Full-flow / Boss movie | `6C6E5046F0653BD02C366D8CF0C8119FB5CF2971DC83B51068BA2DA5A62B48E5` / 4,053 frames and `FDF14343CF0A5C2755B076597DF2BDB5C526B77B64382A56959173409F0F261C` / 2,719 frames. |
| Original-resolution visual review | 46/46 PNGs opened at native 1280×720 (38 full-flow, six reticles, two Boss-retry); readable/nonblank/nonfrozen with named beats visible. See validation record. |
| Actual mutations | `artifacts/task6-final-pester-20260902T010329439`; Pester 5/5, 15/15 materialized mutations fail closed and restore hashes; both host probes green. |
| Verifier 1 | `artifacts/task6-final-verify-1-20260902T010601067`; exactly one `PROLOGUE_VERIFY_OK`, build 0/0, no fatal/error/leak. |
| Verifier 2 | `artifacts/task6-final-verify-2-20260902T012103825`; exactly one `PROLOGUE_VERIFY_OK`, build 0/0, no fatal/error/leak. |

The verifier pair share the runtime candidate above, scene SHA
`9C81ABD51F8C834295AF03FD7F2C8C4088AD2640B7C809ED392235C0E0FD7270`, and summary SHA
`BA94961F9965D0A2FE7191B08EA7B3F7947D244C91F3254A147DE5C5C5B0C9AA`. Documentation records the
finished automated result but is outside the runtime fingerprint and cannot alter it. Step 8
remains unstarted; human acceptance is still open and no automatic HandsOn/user-note substitute
exists.
