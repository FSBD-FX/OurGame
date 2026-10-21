### Task 4: Generated scenes, first-level balance gate, and visible proof

**Files:**
- Modify: `tools/build/BuildRustShellSquire.cs`
- Modify: `tools/build/prologue/BuildGraveLampMoth.cs`
- Modify: `tools/build/prologue/BuildBellBackLaborer.cs`
- Modify: `tools/build/prologue/BuildFallenBellkeeper.cs`
- Modify: `scenes/enemies/RustShellSquire.tscn`
- Modify: `scenes/enemies/GraveLampMoth.tscn`
- Modify: `scenes/enemies/BellBackLaborer.tscn`
- Modify: `scenes/boss/FallenBellkeeper.tscn`
- Modify: `tests/unit/P0RoomStructureTests.cs`
- Modify: `tests/unit/P1LoopStructureTests.cs`
- Modify: `tests/unit/BossSceneStructureTests.cs`
- Modify: `tests/presentation/ProloguePresentation.cs`
- Modify: `tools/verify-prologue.ps1`

**Interfaces:**
- Stable child path on every actor: `VisualRoot/AttackTelegraph`.
- Builders validate the telegraph script, z-order, mouse/collision neutrality, and exact first-level tuning.

- [x] **Step 1: Add structure RED assertions**

  Require all four stable paths, no collision layer/mask on the visual, visibility behind player hit flashes but above room gameplay skins, and exact generated tuning.

- [x] **Step 2: Regenerate leaf scenes and run GREEN**

  Run the four builders in leaf-to-parent order, then build and run `RunPrologueTests.cs`. Review cached scene diffs and stage only semantic lines; preserve unrelated unique-id/import noise.

- [x] **Step 3: Add deterministic evidence dwell**

  Update the presentation driver to hold before and during each ordinary attack plus all four Boss attacks. Record enough frames to show gold preparation, locked red danger, white impact, and recovery for each.

- [x] **Step 4: Run the production gate**

    powershell -ExecutionPolicy Bypass -File .\tools\verify-prologue.ps1

  Expected: 0 build warnings/errors, all builders validated, exactly one each of `PROLOGUE_TESTS_OK`, `PROLOGUE_CRITICAL_PATH_OK`, and `PROLOGUE_VERIFY_OK`.

- [ ] **Step 5: Capture and review**

    powershell -ExecutionPolicy Bypass -File .\tools\capture-prologue.ps1 -FramesOnly

  Automated portion complete: fresh 2280+600 PNGs and 1280×720/60 FPS AVIs passed the capture contracts; seven dense continuous-frame contact sheets were reviewed with `view_image`. Subjective readability at normal playback speed remains pending human playback because `view_image` is a still-image review surface.

- [x] **Step 6: Commit**

    git commit -m "feat: deliver first-level attack readability"
