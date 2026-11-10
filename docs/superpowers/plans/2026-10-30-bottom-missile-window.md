# Bottom missile window implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root executes, agents read-only.

**Goal:** Determine and, if proved feasible, consume a legal missile-safe Bottom launch window using existing keyboard actions and original route budgets.
**Architecture:** Bounded passive event/tick observation first; choose at most one evidence-backed input-planning change afterward. No production dynamics or enemy weakening.
**Tech Stack:** Godot.NET/C#, original canonical full Explore.
**Spec:** docs/superpowers/specs/2026-10-30-bottom-missile-window-design.md

## Global constraints

Root sole writer/build/native owner; runtime/helper freeze during orchestration. Original Leg120/12, control120, fullroute600s unchanged. Keep two ordinary jumps/two exact K applications/Left→Right→Left, full support and HP/recovery guards. No window/OS input/paid generation/new native capture/automation/Git/deletion/real saves.

## Review focus

- Real fired/authorized projectile versus just a Node entering tree; never call an unlaunched child a flight proof.
- Instant explosion/retirement and callback failure must not be missed or change damage permission.
- Missing/invalid geometry and Infinity cannot become a safe-launch result.
- Source/leaf/Player/world identity, mounted-at-entry observation scope and cleanup/borrowed wrappers.
- Any later waiting shares original120; no hardcoded old frame/attack, extra jump or guard-contract weakening.

## Task1: Actual trajectory and legal response

Files: new tests/integration/PrologueCanyonGeneratedMainRouteTests.BottomMissile.cs; existing BottomReturn Tick/scope; canonical selected full-route dependency.

- [x] Read actual v174 RED and prior warning/shot records; verify existing missile public events and original source/keyboard contracts.
- [x] Add bounded passive trace without input changes, run original full Explore, inspect actual flight/explosion and complete support at launch.
- [ ] From evidence either implement one small legal shared-budget response and verify real full Explore, or document why no response is yet proven. Never claim trajectory implies guaranteed future safety.
- [x] One fresh review; C/I one TDD pass, Minor deferred; relevant regression and immutable evidence audit, README/backlog/report/media check. Original full qualification remains open on any unresolved failures.

Partial milestone: actual blast guard and both original landing stances verified. Task1 is not complete: full Explore still fails at control2's next missile; post also has an unqualified Handle ERROR despite original75/owner8 finishing. No suite-green or completion claim; next narrow task must establish legal whole-operation admission without borrowing budgets or deleting Neutral rules. See production report v175 and ledger.

No commits, workspace cleanup or evidence deletion; dirty user tree preserved.
