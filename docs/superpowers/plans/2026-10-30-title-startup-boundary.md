# Isolated Title Startup Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox tracking.

**Goal:** 修复当次获准标题显示在 Ready 前退出的问题，补真实独立入口合同；不宣称用户已看到。
**Architecture:** 复用现有 session，但以真实 SceneTree 生命周期作就绪边界；keyboard owner 在同一冻结构建后串行运行独立 headless 入口，保存独立日志与 wait receipt。live 启动报告必须区别 process-start 和 READY。
**Tech Stack:** Godot4.7.1.NET/C#、pwsh7、既有 canonical owner。
**Spec:** docs/superpowers/specs/2026-10-30-persistent-title-design.md；用户当次授权的标题限定仍不变。

## Global Constraints

- G:/knight/Knight，十一项完整目标 ACTIVE；root 唯一写入/构建/native owner。
- 挂树前隔离 save/settings/recorder；挂树后同步锁定 Start/Continue；X/Quit 幂等清理。
- headless 单进程上限55s、pre180s、post300s不扩大；错误严格拒绝。
- 本次失败已消费显示许可，不再开窗/系统输入/采集/付费/自动化/Git写入/删除；保留失败原件。

## Review Focus

1. _Initialize 早于 root 入树：独立 script entry 必须实际运行，不用延后调用替代。
2. 迟到 Ready 重新绑定开始：保持同步挂树锁定，合同检查两次挂载与菜单往返。
3. process start 被误报 READY：已退出/错误/无READY 均不作成功显示。
4. 旧 keyboard 案缺独立 entry 证据：资格必须拒绝；源/bin/helper漂移仍拒绝。
5. 独立 entry headless 只用于有限合同；live 不因合同自动退出，不接受合同 flag 偷渡显示。

### Task 1: 独立入口生命周期 RED→GREEN

Files: tests/RunIsolatedTitleHold.cs；.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1；tools/show-isolated-title-v001.ps1。
Interfaces: --contract-only 仅允许 headless 独立入口；RunContract(SceneTree)；Run-Owned title-entry 55000；资格校验 title-entry wait/raw markers。

- [x] 加独立入口合同与被动 root/level/UI ready 观察，当前 Mount 不改；实际 keyboard 主合同后独立入口因 quit binding=0失败。
- [x] 在 Mount 挂树前等待实际 root 入树边界，保持挂树后同步绑定检查；同一命令 GREEN。
- [x] 资格拒绝没有独立入口成功证据的旧案，保留完整原 keyboard。

### Task 2: 启动反馈与回归

Files: tools/show-isolated-title-v001.ps1；.superpowers/sdd/2026-10-30-persistent-title/test-launch-plan.ps1。
Interfaces: Assert-TitleStartupReceipt(stdout,stderr,external,processExited)；启动观察只读，READY只代表引擎标题合同，不代表用户可见。

- [x] 真实纯函数输入覆盖退出/初始化错误/重复或缺失READY，先RED。
- [x] 实现live启动有效READY且进程活着才报告ready；纯状态合同通过，实际Windows观察仍待当次显示，不称已经显示通过。
- [x] 完整 keyboard/models/pre/post 各一次；错误全保留不筛绿。最终4PASS，一审C0/I0，无重要修正，无二审。
- [x] README/backlog/本次失败与回归清单更新，20GiB仅顺带检查；显示另等当前许可。

持续执行沿用用户授权，不新增设计审批。指定dirty root继续；不运行会提交或清理的通用skill脚本。计划当前只证明入口技术，不取代游戏十一项验收。
