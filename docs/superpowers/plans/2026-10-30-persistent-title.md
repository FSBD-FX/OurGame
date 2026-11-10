# Persistent Isolated Title Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让当前版本具备安全的常驻标题核对入口，而不再依赖短时自动展示。
**Architecture:** 独立test-only session加现有keyboard合同；薄PS入口默认只prepare，明确显示时才Normal启动，无自动关闭。
**Tech Stack:** Godot4.7.1.NET/C#、pwsh7、原canonical owner。
**Spec:** docs/superpowers/specs/2026-10-30-persistent-title-design.md

## Global Constraints

- G:/knight/Knight，完整十一项ACTIVE；root唯一写入/编译/原生owner，native期间源与helper冻结。
- 原keyboard55秒/pre180秒/post300秒及strict错误门不扩大；三通道隔离，不读写真实存档语义。
- 无新显示/OS输入/采集/付费/自动化/Git/删除授权；只准备不显示。指定dirty root继续，不换worktree。

## Review Focus

1. Start/Continue误触导致先写存档再发现违规：挂树后同步解绑并锁定，合同触发真实UI事件验证无新局。
2. 初始化或菜单往返后被重新启用：实际Settings/Controls往返与多帧状态断言。
3. X与Quit同时/重复关闭：同一幂等close，合同覆盖两种真实Godot信号和清理。
4. 默认运行或错误/旧案意外开窗：PS行为测试默认prepare、失败/漂移拒绝，无真实native调用。
5. 实际live误用headless/movie/fixedfps或编译漂移：只允许正常live命令，准备案与当前源/六bin一致；无窗口合同不宣称可见。

### Task 1: 常驻标题入口与合同

Files: 新tests/RunIsolatedTitleHold.cs、tools/show-isolated-title-v001.ps1、.superpowers/sdd/2026-10-30-persistent-title/test-launch-plan.ps1；修改RunStableClawPullTests.cs和canonical仅追加keyboard合同/选定输入与marker。
Interfaces: IsolatedPrologueFixture.Configure/ValidateStorageRoot，PrologueUiRoot公开事件，Root.CloseRequested，TestSceneTeardown.FreeSceneAfterAudioDrain；Get-SourceInventory/Get-BinaryInventory由AST只导入函数，不执行donor顶层。

- [x] 写实际Title保持/菜单与关闭合同，原默认HeadlessAutoStart下观察行为RED。
- [x] 实现session最小GREEN，入口无计时退出，无观察器或输入驱动。
- [x] PS launch plan默认无启动副作用；坏/过期案、漂移和显式模式行为RED→GREEN。
- [x] 完整keyboard/models/pre/post实际执行：3PASS/1FAIL，pre原恢复完成检查仍FAIL；不称绿色套件。唯一fresh C0/I2/M1审查，两重要项一次修正、菜单跨帧Minor延期，不二审。
- [x] 保管/README/backlog/显示文档/一次20GiB检查；尚未显示，不标整项目完成。

技术入口已验证，但合并套件仍红；本计划不发出全套通过或正式交付的完成声明。下一继续处理原恢复消费者的真实完成边界，显示另等当次许可。

持续执行授权沿用，不另开设计审批循环；本技术修复不新增游戏设定。无Git提交/清理本计划目录。
