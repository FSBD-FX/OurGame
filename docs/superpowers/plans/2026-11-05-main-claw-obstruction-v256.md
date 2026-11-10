# Main claw obstruction v256 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为当前完整Main获得原机械爪拒绝身份，并接入有实证依据的既有战斗清障，不放宽普通验收。
**Architecture:** canonical host两个exact allowlist同步增加Main，AST契约RED→GREEN；实际原LOS证据确认同一lost-echo后，正常Main通过纯scope选择复用Explore已有清障，原live/物理/失败门不改。
**Tech Stack:** PowerShell7 AST、现有Godot/.NET verifier，无新依赖。
**Spec:** docs/superpowers/specs/2026-11-05-main-claw-obstruction-v256-design.md

## Global Constraints

- 唯一G:/knight/Knight，保脏树/原件/失败，无Git写/删除。
- 仅headless/internal keyboard/隔离存储，无GUI/OS/付费/新媒体/dump/GC/cache/KeepAlive/引擎/真实存档实验/自动化/发布。
- root唯一host/source/build/native；运行前冻结。SourceRows1897，无新CS/UID。
- 原普通Main/Explore/Boss/支撑出口/严格suite与所有十一项不降低，trace不等于行为资格。

## Review Focus

- 只改早门会在build拒绝、只改build会在早门拒绝：实际两边Main行为分别测。
- 大小写/混旗标可能意外放行非独立诊断：exact4正例、外域/后缀/8旗标拒绝且拒绝前不输出。
- 普通构建可能遗留诊断符号：literal7/8argv及无trace污染三类门，真实Rebuild/绑定。
- trace可能被误当普通通过或掩盖失败：原provenance行为false控制，实际失败不筛绿、不改caps。
- 新清障路由不得把选择当安全授权：literal外域/非keyboard/非Grip8/未知variant拒绝，原CanCounter/foreign/fresh/真实Hurtbox/自然死撤碰撞保持，当前Main实际验证。

### Task1: 两门准入和一次原始Main诊断

**Files:** 修改`.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1`两allowlist；新增本计划workspace的`test-main-claw-trace-admission-v256.ps1`。不改生产CS或测试驾驶。
**Interfaces:** 既有Get-ClawBuildArguments(string,bool,bool,bool)->string[]；既有TraceClawObstruction switch。

- [x] 保存host before/branch/HEAD，读spec/plan；新增AST契约手写literal4正例、普通7/trace8、无输出拒绝、混旗标、污染与元数据控制。
- [x] pinned pwsh7运行契约，Expected：只有新exactMain的早门/build行为RED；语法/缺依赖不计RED，保存输出。
- [x] 两allowlist各新增exactMain；同命令Expected：全部契约GREEN，旧规则/argv/原失败保持。84checks通过。
- [x] `verify-claw-knight-v001.ps1 -Phase thruster-route-main-diagnostic -SourceRows 1897 -TraceClawObstruction`一次，Expected：诊断记录或保留当前原失败；不会拿失败当完成。冻结源码/宿主，原cap不改，观察超时续同handle。实际158744/FAIL/LOS同lost-echo。
- [x] 原查询身份与route结果关联、保护对/源/二进制/宿主核查。构建普通`models`恢复无符号并验证普通污染门。实际645msPASS、10对0diff、普通binary等于v255，封存epoch1。

### Task2: 正常Main接原Grip8战斗清障

**Files:** `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`、`tests/unit/RangedWeaponModelTests.cs`。原GhostClearance.cs不改。
**Interfaces:** `Task10Grip8GhostClearanceRoute(bool keyboard,bool main,bool exploration,int gripIndex)->bool`；选择条件仅keyboard && gripIndex==8 && (main||exploration)。现有异步清障独立拥有全部安全/战斗许可。

- [x] 保存2CS before；注册literal12行：Main/Explore、noKeyboard/noKnownVariant、indices0/7/9/-1/intMax拒绝。初始helper仅旧Explore分支；ranged-models实际RED，编译失败不计。
- [x] helper最小GREEN并替换唯一调用门，原清障函数/600/fresh/HP/K/jump/jet/碰撞不改；同阶段GREEN。
- [x] 普通Main无trace一次，核清障实际J/Hurtbox/减HP/撤碰撞、Grip9、Boss实体出口/恢复0；失败保留、不改原门。其余Explore/pre/post/save/traps/structure/geometry/patrol/allocation/state/models/keyboard/actions/aggregate14阶段同源各一次，原cap不改。实际当前16=15PASS1FAIL(preFATAL)，未达全绿，不记Task2完整交付。
- [x] 当前epoch2源/二进制/宿主与保护对核查；初始trace/restore不混算当前同源。一次fresh四文件全增量审查，C/I一次TDD修补、Minor延期、所有裁决入账；文档/单次媒体Check，完整目标ACTIVE，不Git/清理。当前80对/1897完整源集/6binary/15helper0diff，一审C0/I0/M1、13裁决；M1延期、无二审。最终复核附档，preFATAL仍使整项目不合格。

执行结论：有界诊断和主线策略已实证；完整原十一项目标未完成，项目suite15/16仍红，正式动作/美术/正常显示和真人等继续OPEN。不因勾完操作步骤创建全目标/发行完成标记。
