# 居民同步端点复用 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 减少真实居民admission重复body查询，保持原安全语义，测实际启动及完整回归。
**Architecture:** 单次CheckPatrol的局部纯值表，以完整Transform为键；原public corridor仍fresh，内部core共享原通道算法。manager前后admission各自新建表。
**Tech Stack:** 当前Godot/.NET/C#、现有PowerShell owner。
**Spec:** `docs/superpowers/specs/2026-11-04-resident-local-endpoint-reuse-design.md`，v239/v240与原十一项Q01/Q03/Q05。

## Global Constraints

- 唯一G:/knight/Knight；原codex脏分支原地保留，不建副本/提交/删除。
- 根独占原生；任何native/build存活时所有人冻结源/harness。仅无窗口/隔离存储/原键盘输入。
- 不改人口/难度/HP/输入/等待/时限；不跨前后admission、不保留native资源或回调，不修改真实存档。
- 根拥有Geometry及Spawn两个生产文件，测试worker仅拥有CanyonResidentPlacementTests.cs；双方不还原他人改动。

## Review Focus

- 相同位置不同完整基底不能合并，错误端点/未知/截断及start/end Issues不丢。
- 所有零长/往返corridor保留原顺序，通道自身完整支撑/原生/电场/keepOut每次仍查询。
- 支撑保持collider+shape双身份，玩家-only不能吞foreign障碍。
- 缓存只在本次同步调用；两次调用形状/keepOut/世界变化必须fresh，挂载后反例不豁免。
- 参数/事务/生命周期无改变，实际减少查询与行为等价分开证明，不由微基准推硬件FPS。

### Task 1: 端点批次与实际消费者

**Files:** `scripts/prologue/residents/ResidentPlacementGeometry.cs`、`scripts/prologue/residents/CanyonResidentManager.Spawn.cs`；测试`tests/integration/CanyonResidentPlacementTests.cs`。
**Interfaces:** 新internal `CheckPatrol(CharacterBody2D actor, Transform2D intended, Vector2 worldSpawn, IReadOnlyList<Vector2> patrolPoints, PhysicsDirectSpaceState2D space, Node2D worldRoot, Rect2 permittedBodyBounds, bool requireGroundSupport, IReadOnlyList<ResidentKeepOut> keepOut, ResidentPlayerCollisionIdentity? currentPlayer=null)`；返回internal纯值结果，其public属性为`Body`、`Corridors`、`BodyCheckCount`。原公开API不变。

- [x] worker只写测试，Run入口反射发现缺失方法使build通过、行为RED；真实fixture检查置于controlled geometry可用后。根看见RED前不实现生产方法。
- [x] 根实现局部表/共用原corridor core，不改变公开fresh API；真实等价控制＋原resident-geometry全套GREEN，重复两点实际body数2而旧fresh9，stationary1而旧5。
- [x] 根接manager；跑resident-level-lifecycle/auto-start、camera及原pre/post/爪models子集，保留所有失败；视结果补当前普通Main/Explore，不借旧结果续签。
- [x] 一次独立审查，新Critical/Important实际RED→GREEN一次修补，无二审；所有原门继续尝试，原目标不缩。
- [x] 更新执行账/README/待办和实际报告，分清查询减少、加载变化与正式体验，不勾原十一项或硬件真人。

步骤均执行，不等于Task1完整通过。当前同源十一门8PASS/3FAIL：geometry原生FATAL、恢复首cue、Main原899未清；前后段及Explore通过，M1测试补强延期，整套仍红，不签complete。保留所有前后原始失败与源绑定，继续全目标。
