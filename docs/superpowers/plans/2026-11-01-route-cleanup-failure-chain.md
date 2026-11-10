# Route cleanup failure-chain preservation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline. Root alone runs builds/native; one final fresh whole-change review.

**Goal:** Preserve the exact primary Task10 route failure and every ordered cleanup failure, without turning failure into success or weakening AudioDrain, then use one actual Main run to inspect the real current failure chain.

**Architecture:** A test-only ordered async finalizer in the existing Main test file collects original exception objects. Zero errors returns; one error uses ExceptionDispatchInfo; multiple errors aggregate in primary-first/cleanup-order without flattening. Explicitly owned keyboard/ranged/monitor resources are finalized once, including early setup unwinds, with original audio release and persistence protections retained. No new production lifetime behavior or retry.

**Tech Stack:** Existing Godot4.7.1/.NET9; two existing test CS files, SourceRows1872.

**Spec:** docs/production/2026-11-01-supported-side-transfer-v206.md (actual current AudioDrain failure and unproven original exception); original eleven-item backlog Q01/Q05/A02/D04 remains active, not replaced by this local diagnostic goal.

## Global Constraints

- Only G:/knight/Knight; headless/internal-input tests only; no GUI/OS input, real saves, paid generation, new crash dumps, runtime replacement, automation, Git writes, publishing or deletion.
- Original Main/Explore600s, pre180s/post300s, build180s/actions55s; AudioDrain60frames/2000ms unchanged. No error suppression, pass marker shortcuts, extra GC/native release/fallback or retries.
- Preserve dirty checkout and all failures; manual retained ledger replaces commit/worktree/cleanup wrappers.

## Review Focus

- Successful return/early return must still execute every owned cleanup exactly once, while primary-only failures retain their original object and stack.
- Synchronous and asynchronous cleanup failures must all survive in deterministic order, including an already-aggregate primary without flattening.
- A first cleanup fault must not prevent persistence/input finalization or silently claim scene/audio release completed.
- No implicit using Dispose outside the guarded path may replace the final aggregate; early setup must retain existing keyboard/ranged ownership protections.
- Audio failure remains a real failure with original deadlines; controlled injected failures do not prove the real mixer or underlying route was fixed.

### Task 1: Ordered finalization and actual chain diagnosis

**Files:** Modify tests/integration/PrologueCanyonGeneratedMainRouteTests.cs and tests/integration/KeyboardActionRuntimeTests.cs. Preserve tests/TestSceneTeardown.cs unchanged. Retain exact baseline copies under .superpowers/sdd/2026-11-01-route-cleanup-failure-chain; update README/backlog/report/ledger after terminal results.

**Interface:** internal static Task Task10FinalizeRoute(Exception primary,IReadOnlyList<Func<Task>> cleanupSteps). It attempts each step once; captures original exception objects; EDI rethrows a sole failure; otherwise AggregateException primary-first then ordered cleanup errors. RunTask10 captures route failure, owns driver/ranged/monitor explicitly from the first previous using acquisition through cleanup, and invokes the same helper for original ordered finalizers. Setup-before-mount does not acquire new fallback/native release behavior.

- [x] Add helper stub with old stop-on-first behavior and async controls in existing actions phase. Test success, primary-only, cleanup-only, primary+AudioDrain-shaped error, multiple sync/async errors, later protection steps, already-aggregate primary, early return and exact original stack/identity. Run canonical actions -SourceRows1872; expect named behavioral RED, not compiler failure.
- [x] Implement minimal finalizer; run actions expecting controls plus original actual actions PASS.
- [x] Integrate explicit ownership/primary capture into current RunTask10, no hidden using unwind; run full Main once under original600s to retain its exact native result. Actual AudioDrain may or may not recur; never rerun just to select a quiet outcome.
- [x] One fresh readonly whole-change review; one C/I TDD fix pass, minors deferred, no re-review. Final-source actions/keyboard/policy/clawmodels/pre/post under original budgets; Main rerun only if review source changes require current binding. Explore remains unqualified until needed encounter actually clears.
- [x] Audit exact provenance/custody/terminal process identities, update durable status and media check. Local task can finish only at its preservation scope; original eleven, audio root cause, Main/Explore, art/normal display/hardware/human/release remain open unless independently proven.

Outcome: preservation controls11 and currentrealprimary-onlypathverified; naturalnativeprimary+audiofailureNOTobserved, M1actualownershipcoverageDEFERRED. MainstillFAIL; prehasnewDescent553unknown-conjunctFAIL, so fullsuite/Task1broaderqualification notgreen. No task-done/wholegoalcompletion/mergeclaim; originalelevenACTIVE.
