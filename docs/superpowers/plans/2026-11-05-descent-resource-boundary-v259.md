# Descent resource boundary v259 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在原Descent挂载边界关联真实字段，并禁止不完整pre journal被标作成功。
**Architecture:** 复用REF scalar observer和owned-root guard；挂树后依赖原try/finally；pre实际完整性guard与独立原生控制调用同一方法，PS实际收据负控参与验证。
**Tech Stack:** 现有Godot/.NET C#与PS7，无新依赖。
**Spec:** docs/superpowers/specs/2026-11-05-descent-resource-boundary-v259-design.md

## Global Constraints

- 唯一G:/knight/Knight；root唯一作者/native，脏树保before不动Git。
- 新合同CS预计源1898；原pre180/post300/双线600/模型55秒，不提高cap/增加原pre帧。
- 无原pre额外Load/GetState/Instantiate/GC/cache/KeepAlive/跨await借用；挂前自有根清理、挂后原音频finally。
- 仅headless隔离；无GUI/OS/付费/媒体/dump/真实存档/自动化/删除发布。原十一项ACTIVE。

## Review Focus

- 新observer失败在AddChild前必须释放同一个自有root且保primary，不误Free无关节点；mounted失败必须由原音频finally处理。
- before/after实际字段与inside-tree状态分别读取，不假定Ready绝不换资源，不从输出反推expected值。
- guard真的查Active fault/scopes，越cap不产生成功marker，临时合同journal正常恢复；不改真实pre日志策略。
- 新补充收据与pre完整性marker缺失/重复/伪造/三raw错误皆拒绝，不破坏原v258合同与exactREF准入。
- 普通构建无新诊断调用/类型/事件，原前后段与双线/保存当前同版重验，不将quiet或新字段当根因已修。
- 生命周期修订：自有根在第一个await前封闭延迟初始化及后续StartNew，音频退休先于QueueFree；不得引入全局Quit、额外drain/GC/cache，既有应用退出仍保持幂等和owner检查。

### Task1: 两边界与完整性guard

**Files:** 新tests/diagnostics/DescentResourceBoundaryContracts.cs（真实native控制）；改DescentClawGuidanceRuntimeTests.cs（observer/原调用）、BasinSupplyRuntimeTests.cs（既有helper internal）、Task14NativeResourceIdentity.cs（只读scope属性）、RunPrologueTests.cs（实际pre guard）、RunResidentCensusIdentityContract.cs（旧小合同后调用新控制）、canonical verifier（补充收据/输入/冻结/pre guard）；新workspace PS纯控制。旧v258控制文件不编辑。
**Interfaces:** Descent.ObserveBeforeMountOwned(PrologueLevelController)->void；Descent.ObserveAssignedStartupResources(PrologueLevelController,string)->void；Task14.OriginalProbeTraceActive bool只读；RunPrologueTests.AssertPreLifetimeTraceCompleteForSuccess()->void；DescentResourceBoundaryContracts.Run(SceneTree)->Task；PS Assert-DescentBoundaryContractReceipt(int,string,string,string)和Assert-PreLifetimeTraceReceipt(string,string,string)。

- [x] 保存before/branch/HEAD；写真实补充PS负控与新native反射缺失API控制，先观察合法行为RED（非语法/构建错误）。
- [x] 最小实现两个位置观察、挂前helper/挂后原finally、pre actual guard与host两收据；native小合同与新旧纯控制GREEN，保全部失败。
- [x] 一次原pre REF关联/完整性原始结果，无论结果普通Rebuild恢复；不重试筛绿、不把未知当修复。
- [x] 当前同版普通17门、源全集/二进制/helper/保护清单绑定；保留预期负控warning与任何FAIL。
- [x] 唯一fresh审查，C/I一次TDD修补、Minor延期、全部裁决/代价；更新状态/反馈/容量Check，原目标ACTIVE，无Git或清理。

### Task1 prerequisite amendment: 退休权限与原混音drain

**Files:** scripts/prologue/PrologueGameFlowController.cs（提取共用同步退休入口）、tests/TestSceneTeardown.cs（复用退休，不改drain）、tests/diagnostics/DescentResourceBoundaryContracts.cs（即时真实清理/首await latch）、canonical input/freeze补这两生产与测试文件。
**Interfaces:** GameFlow.internal void BeginSceneRetirement(); consumes existing Level.BeginApplicationQuit()/Audio.BeginApplicationQuit(); actual helper DrainAudioOwner remains Task with same public signature/caps.

- [x] 写首fixture实际DrainAudioOwner首await前的flow.IsApplicationQuitting/audio.IsQuiescing/旧Flow状态不重启断言；先看DESCENT_TEARDOWN_STARTUP_RED而不是反射缺API或语法错误。Run exact existing sector-assignment-identity-contract -IdentifyRefCounted -SourceRows1898，保所有raw。
- [x] 最小提取/复用同步退休，去掉旧首fixturePlaying等待；不变原混音算法、QueueFree/await位置、GC/cache/时限。重复同命令，期望该断言GREEN；任何独立原生错误仍严格FAIL，不筛绿。
- [x] 当前退出版与原应用Quit门、ordinary/pre/post/save/双线纳入同版17和最终一次联合审查；异常先于首次deferred启动、已经准备中、重复退休、无Flow音频root和无关根不受影响需检查。实际pre/post/REF失败保持OPEN，不由此复选框声明全回归或游戏稳定性通过。
