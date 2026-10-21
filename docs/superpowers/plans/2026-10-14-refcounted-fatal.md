# RefCounted fatal identity diagnostic implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Root alone owns all source edits, builds and engine runs; other agents are read-only reviewers/test-script authors with explicit ownership.

**Goal:** Identify the managed type of a naturally occurring finalizer fatal without changing gameplay, suppressing errors, forcing GC or exposing userdata.

**Architecture:** Use a separate `REFCOUNTED_FATAL_IDENTITY` compile symbol, reusing existing test-only virtual disposal probes. Record only type, managed hash, raw pointer, call, thread and disposing state around exactly one unchanged base call. Keep the normal route, strict raw diagnostic gates, isolation and all ownership guards unchanged; label this run instrumented and not acceptance.

**Tech Stack:** Pinned Godot 4.7.1/.NET9; PowerShell7 owned-process launcher.

**Spec:** `.superpowers/sdd/2026-10-14-refcounted-fatal/progress.md` and the three read-only audit reports beside it.

## Global Constraints

- Keep current 1645 source paths; do not create C# or scenes. Preserve all prior failures and dirty worktree edits.
- No native query from Dispose; NativeInstance is only a managed field getter and not liveness evidence. Native instance ID remains unknown.
- No global hook, engine/binding modification, stack/dump capture, strong retention, KeepAlive, forced GC or suppress-finalizer change.
- Console logging may alter scheduling; even a passing diagnostic is not a fix, performance proof or ordinary acceptance.
- Root serial build/run only; no other active engine/build consumer; all before/after/source/helper/binary/user guards preserved.

## Review Focus

- A missing self-owned Resource/Logger override must be detectable against the frozen 19-type metadata inventory.
- Diagnostic switch on unrelated phases must reject before file or process side effects; ordinary build arguments unchanged.
- Finalizer probe must not turn logger callbacks into native calls or swallow base.Dispose failures.
- Truncated logging must emit a cap marker; missing RETURN means unknown until paired by call/thread with the fatal.
- No successful instrumented route may be reported as uninstrumented whole-goal acceptance.

### Task 1: Bounded type identification

**Files:** Modify `tests/diagnostics/Task14DisposeIdentification.cs`, `tests/diagnostics/Task14NativeResourceIdentity.cs`, `.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1`; create test script `.superpowers/sdd/2026-10-14-refcounted-fatal/test-identity-admission.ps1`.

**Interfaces:** `Get-ClawBuildArguments([string]$Phase,[bool]$IdentifyRefCounted)` returns the original six build arguments when false, appends only `-p:DefineConstants=REFCOUNTED_FATAL_IDENTITY` on Main/Explore when true, rejects all other phases. `Task14DisposeProbe.Enter/Returned` accepts GodotObject; diagnostic mode uses managed-only scalars. Existing symbol behavior remains available separately.

- [ ] Write and run admission tests against actual AST-extracted function; RED is missing bounded diagnostic admission. Test both route phases, rejection outside them, exact ordinary argv and no file/process effects.
- [ ] Add function/switch before any launcher side effects and instrumented case/receipt labels. Add four missing partial overrides only under diagnostic symbol; broaden outer guards so existing serial fields compile. Keep each base.Dispose exactly once and RETURN after success only; cap20000 with one explicit marker; explicit stderr Flush.
- [ ] Run all owner admission tests plus the existing 43 normal guard tests. Review source delta independently before engine launch.
- [ ] Run **one** isolated Main route through the same owner with `-SourceRows 1645 -IdentifyRefCounted`; fresh build, no fixed time/movie/verbose/forced GC; no other consumers. Preserve failure as FAIL, never named RED or waiver.
- [ ] Read complete stderr, pair ENTER/RETURN by call/thread; retain hashes and metadata coverage. If failure does not reproduce, report inconclusive, no retry/fix claim. Update README and ledger; no Git write/cleanup/publication.
- [ ] After that engine has terminated and guards finish, use the same owner without the symbol for `ranged-models -SourceRows1645` to restore ordinary binaries and run the relevant model regression. Preserve both fresh binary sets and verify the ordinary build excludes diagnostic overrides. This is not a second Main attempt or a whole-suite pass.
