# 相机夹具被动三时钟 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 定位原前段180秒超时的耗时区域，保留全部真实案例、人口、原等待及断言，不用局部通过关闭完整序章欠账。

**Architecture:** 现有30个CAMERA_FIXTURE_BOUNDARY读取全局毫秒、物理帧、process帧；严格解析器保留全部原身份/顺序，新增三钟必需、UInt64与全流不倒退验证。只有读取，没有timer、await或运行时物理变化。

**Tech Stack:** 当前Godot/.NET/C#和现有PowerShell原生owner。

**Spec:** 原十一项Q01/Q05，及 `docs/production/2026-11-04-first-jet-lesson-v238.md` 的前段计时事实；本计划是该诊断设计的具体说明，不是新增玩法/动作批准。

## Global Constraints

- 唯一工程 `G:/knight/Knight`，原十一项保持ACTIVE；仅无窗口、隔离存储和已有键盘输入资格。
- 不改生产物理、难度、人口、场景、原180秒前段/其他时限、await与生命周期。
- root后必需 `engine_ms`、`physics`、`process` canonical UInt64；允许相等，所有30行逐钟不倒退。
- root/顺序/sector/high/阶段及三次不同owner原约束全部保留；不从时钟推FPS或硬件负荷原因。
- 原实际await语义额外要求每cycle的mount→eight物理/process均至少8帧、playing→cleanup均至少12帧；只限制必经推进，不给毫秒或FPS设下限，bigint计算差值。
- 根唯一原生执行者，运行期间不编辑其源/harness；不新建worktree、发布、删除、付费或自动化。

## Review Focus

- 缺失、负数、小数、前导零、溢出及未知尾部不能进入计时分析。
- 同步加载/实例化可有相同帧值，不把相等误判失败。
- 任一时钟跨周期回退必须拒绝，不能每cycle悄悄重置。
- 旧身份/少行/重复/错序反例全部保留。
- playing-return至cleanup-enter包括所有后续检查，不能冒称只测12次相机站立或人手FPS。

### Task 1: 精确回执和实际计时

**Files:** `tests/integration/BasinAscentCameraRuntimeTests.cs` 的原Observe输出；`.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1` 的Assert/Test-CameraFixtureBoundaryReceipts两函数。

**Interfaces:** 原30阶段字符串仅在root后附三个无符号十进制标量。worker只拥有两PowerShell函数，根只改C#输出并独占运行。

- [x] worker先写含钟合法行、相等正控及各类非法钟的Test，原Assert不改；根AST只加载两函数运行，预期旧Assert拒绝新合法行的RED。
- [x] worker实现严格Assert，根运行controls与旧真实无钟artifact拒绝；不运行整个owner主体来取代纯解析器验证。
- [x] 根在原Observe中仅读取 `Time.GetTicksMsec()` / `Engine.GetPhysicsFrames()` / `Engine.GetProcessFrames()`，不新增或移除任何原运行操作。
- [x] 运行原 `ranged-camera-ground -SourceRows 1888`，必须所有原案例与计时回执同时通过；按load、instantiate、mount、eight/playing、post-playing、cleanup差分如实报告。
- [x] 一次独立只读审查，Important先RED再修，原完整pre/post/models照常尝试且保留失败，不用旧通过代签新构建。
- [x] 修正文档历史段的当前状态歧义，更新README/报告，仍不勾动作、惯性、完整1–11或性能验收。

执行结果：步骤已执行，不等于整套通过。最终五门4PASS/1pre FAIL（下降教学HP4原要求5），不写Task1 complete、不签完整fix pass或全关。详见v239报告和本计划执行账；下一补实际伤害来源，原时限/危险/人数和正式质量欠账保持。

执行裁决：用户已要求直接持续推进、只有关键选择缺失才问，本次被动诊断不重复方案审批；既有G工程/codex脏分支不复制，不自动提交、发布或删除证据。
