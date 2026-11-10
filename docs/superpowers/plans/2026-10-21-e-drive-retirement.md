# E-drive Knight Retirement Implementation Plan

> **For agentic workers:** Use test-driven implementation and independent review; no worker may delete or rename the old E tree. Only the main agent may execute the final guarded cleanup.

**Goal:** 将当前执行依赖迁往 G，验证独立性后删除唯一授权的 E 旧树。

**Architecture:** 只读历史输入与新输出分离；保留历史来源身份。全量内容校验精确承认已记录的 Git 元数据重连差异；同卷改名隔离旧路径后运行迁移专项，再执行原生 PowerShell 精确清理。

**Tech Stack:** PowerShell 7、.NET 9、Godot 4.7.1 Mono、C#。

**Spec:** `G:/knight/Knight/docs/superpowers/specs/2026-10-21-e-drive-retirement.md`

## Global Constraints

- No paid generation, automation, save migration/reset, V1 mutation, broad cleanup, or fallback deletion after a policy denial.
- Work in the existing user-selected checkout; preserve unrelated dirty edits and the index.
- Historical logs/frozen snapshots are evidence, not current configuration; do not rewrite them.
- Existing game regression failures stay reported; storage acceptance is a separate bounded claim.

## Review Focus

- A migrated baseline exists but has changed bytes: require its original SHA256.
- A path is only a historical literal or parser fixture: distinguish it from live filesystem access.
- Reparse points escape the source/target: never traverse them; validate only the exact recorded mappings.
- Files change during validation or before deletion: require quiescent trees and stable metadata/hash checks.
- Validation fails after temporary rename: preserve the source and restore the original path; never delete on failure.

## Task 1: Migrate executable dependencies

**Files:** Current `tests/**/*.cs`, three `tools/*fall-damage*.ps1` / `archive-keyboard-proof.ps1`; baseline helper and path audit under existing tests/tools as needed.

- [x] Preserve before copies/hashes outside the current project.
- [x] Reproduce the real fog baseline read failure with the old path temporarily unavailable, or a bounded actual baseline consumer that rejects E dependency before changing it.
- [x] Move read-only historical inputs to the G historical mirror; new output to G verification/media roots; exact-checkout guards to the current G checkout.
- [x] Keep all original path, save, process, and content-identity checks.
- [x] Compile and run the actual baseline consumer; audit all current executable/config paths, including hidden helpers.

## Task 2: Verify retained historical tree

**Files:** New bounded-memory verifier and tests under `G:/knight/迁移记录/2026-10-21-e-retirement-v001/`; existing migration records are inputs only.

- [x] Test controlled identical, missing/extra/changed, allowed-metadata, wrong-hash, and escaped-link cases before relying on the verifier.
- [x] Compare every ordinary source file against retained G; allow only exact recorded before/after metadata identities and exact link targets.
- [x] Independent review of verifier and migration diff; resolve important findings before cleanup.

## Task 3: Isolate, validate, clean

**Files:** Final receipts under the new migration directory; current README and production report.

- [x] Resolve exact source/target, confirm no reparse ancestors and no active old-root process; record disk free space.
- [x] Rename the E tree within `E:/OurGame` to one unique, explicitly resolved retirement-check path, with a restoration plan.
- [x] With `E:/OurGame/Knight` absent, run G build, engine smoke, current keyboard/action/model checks and actual fog baseline reading. No foreground input or media capture.
- [x] If every storage gate passes, delete only the verified renamed source using literal PowerShell targets; if denied, stop without alternate methods.
- [x] Recheck source absence and retained G baseline/Git identities, report recovered space, update durable status. No commit/push or V1 changes.

## Latest user clarification: all project media stays inside KNIGHT

- [x] Correct public and current hidden archive outputs to `G:/knight/过程图稿与视频`; retain frozen history and never restore the deleted other archive project.
- [x] Copy dedicated offline Python/Pillow and ffmpeg to `G:/knight/Tools`, verify exact contents and actual invocation; preserve strict existing Edge art-suite failures separately.
- [x] Establish an explicitly NEW KNIGHT-local baseline and one-shot manual capacity checker (20/20 controls and actual check); no automation, stopping, implicit reset, packaging or deletion.
- [x] Repair only existing-target current README links; distinguish already-cleaned V1 media from retained source files.

Actual receipts and all failures remain under `G:/knight/迁移记录/2026-10-21-e-retirement-v001/`. Storage migration does not complete the game's items 1–11.
