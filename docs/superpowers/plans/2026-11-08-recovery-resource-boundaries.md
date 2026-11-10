# Recovery resource boundary implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在原恢复序列明确资源阶段与已有引用身份，推进Q05而不宣称定因或修复。

**Architecture:** test-only标量helper在recovery分支启用；旧Audio/Seamless只加即时借用观察。纯ID核实际RED→GREEN，原native严格门保留。

**Tech Stack:** 当前Godot4.7.1/.NET9/C#及规范headless runner。

**Spec:** `docs/superpowers/specs/2026-11-08-recovery-resource-boundaries-design.md`。

## Global Constraints

只G:/knight/Knight无窗口，无OS输入/paid/真档/dump/Git/删除/自动化/GC/cache/引擎/预算/原等待变化。root sole build/native/canonical；worker只新增helper。原四delegate与全部严格门不删。

## Review Focus

- disabled也触原生、诊断fault遮掉主异常。
- 透传返回被字段/闭包/新局部或集合跨await保活。
- invalid身份错误归零或从原生重查旧ID。
- packed/sector临时Free后的身份边界，内部Resource不可观测。
- 原测试时序、顺序、await或source保护被意外改写。

### Task 1: test-only边界与一次严格取证

**Files:** create `tests/RecoveryResourceBoundaryTrace.cs`；root modify `tests/RunThrusterCoreTests.cs`、`tests/integration/PrologueAudioDirectorTests.cs`、`tests/integration/SeamlessPrologueWorldTests.cs`与canonical必要source快照。无生产。

**Interfaces:** static `RunControls()`、`IDisposable Begin()`、`SetScope(string)`、`Point(string stage,string role)`、`ulong Observe(string stage,string role,GodotObject borrowed,ulong knownId=0)`、`PackedScene Loaded(PackedScene borrowed,string role="scene")`、`AssertComplete()`。只有标量状态，JSON行上限512；错误计fault，summary保留complete=false。

- [x] helper纯ResolveKnownId暂return0，三个独立字面量controls；root规范1912 actual RED adb83342...／build0/32768ms、engine1/543ms，valid42实际0，guards[]。不是编译/游戏故障，尚未正式根加载。
- [x] root只实现valid?observed:known，原controls GREEN；原恢复8299f18...／14940ms、I1修补后d4043d9...／16964ms各自严格通过；133行双sink、14Pack/14Free/10definition/4load/8cleanup精确核对，五保护对及sealed6一致。不重复筛绿，quiet不签修复。
- [ ] 当前同源models与原普通pre44/post75严格回归，原错误/警告按合同记录；不拿v321旧两线代签新整版。第一候选models PASS、pre618a1f...35579ms原生FATAL；post27618b...误传MeasureFullPostBudget只能算诊断。I1最终4phase2PASS/2FAIL，普通pre47e680...143531ms有4Handle／post e7df0f...186722ms有6Handle，虽44/75执行完仍strict FAIL；原180/300秒cap不放宽，Task1保留OPEN。
- [ ] 一次独立增量review已完成C0/I1/M2；I1正常summary末检验经8630992...功能RED→d4043d9...GREEN，异常主因保留，未二审；Minor延期。README/稳定待办/ledger/失败定位和媒体Check已更新（15,203,531bytes/145files/Notify=false），但完整suite仍红，不写本Task1或原十一项complete。

## Rulings

用户持续自主无窗口执行已明确，纯内部定位不再停等计划批准。原位唯一脏树，不运行Git型SDD提交/清理脚本，以case快照及ledger留证。若1912实际不符先查，不凑数。诊断时序扰动和可见范围有限是成本，不把新增日志当资源修复。
