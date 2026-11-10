# Ghost Recovery Clearance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root owns edits/native, agents read-only.

**Goal:** 通过真实键盘恢复期反击清除Explore抓取路径敌人，保留原物理和通关标准。
**Architecture:** test-only Counter许可与真实保存敌人组件，之后在独立Grip8Combat阶段调用；原生产规则不变。
**Tech Stack:** Godot4.7.1.NET/C#、现有stationary-guard与完整Explore owner。
**Spec:** docs/superpowers/specs/2026-10-30-ghost-recovery-clearance-design.md

## Global Constraints

- G:/knight/Knight原十一项ACTIVE；root唯一写入/构建/native，运行期间源/helper冻结。
- 保留敌人间角度优先、真实遮挡、HP/伤害/能量/时钟/位置和原旧控制；组件不冒充整路。
- 原native55/pre180/post300/route600秒与单transfer300tick不增加；无窗口/OS输入/付费/采集/Git/自动化/删除。

## Review Focus

1. 借另一actor的Recovery许可：对真实ID、当前attack和同源当前publication全部约束。
2. J有输入没伤害却计击杀：同Hurtbox Connected＋Health实际减少才计；死亡才结束。
3. 隐藏/持续盾/活爪/foreign危险：必须拒绝，真实L松开与重新采样后再发J。
4. 多次击退离开射程：实际连接验证，不假定固定三连J必杀；失败保留。
5. 将combat偷偷扩成transfer：独立计数但占全程，原Grip8/9各300tick仍保留。

### Task 1: 真实保存幽灵的Recovery counter组件

Files: 新tests/integration/GhostRecoveryCounterRuntimeTests.cs；GhostLandingGuardRuntimeTests.cs只加许可方法；RunThrusterCoreTests.cs与canonical追加原stationary-guard后合同/封存。
Interfaces: GhostLandingGuardRuntimeTests.CanCounter(RustShellSquire,CharacterBody2D,ulong expectedActor,ulong expectedAttack,bool foreignDanger):bool。

- [x] 新真实fixture先在Recovery查许可，缺方法实际RED。
- [x] 实现同源/时间/能量/姿态许可，真实L→fresh J→三次实际Health下降/死亡，不改生产规则。
- [ ] 原guard/hide控制与新组件、模型/前后段实际执行，全部失败保留不外推整路。

### Task 2: 原完整Explore Grip8Combat接入

Files: PrologueCanyonGeneratedMainRouteTests.cs或同partial的新小文件、同版完整路线日志准入。

- [ ] 先绑定实际blocker父身份，不猜ID；组件成立后单独combat计账。
- [ ] 原Grip8返回→清敌→原Grip9一次cast/300tick→完整Boss/实体出口尝试。
- [ ] 唯一fresh最终审查、当前源/bin保管、README/backlog与真实成功/失败更新；不将未完成Task2缩掉。

指定dirty root不提交或清理。能证明一次合法反击不等于完整清敌/整路；Task2未过则原Explore保持FAIL。

2026-10-30 v170 执行边界：Task1组件/预算修正已实际通过，但全suite pre/post均FATAL，任务不记完整完成；Task2实际locked-ray父身份已直接配对lost-echo，仍未接入真实Grip8Combat/foreign安全窗口。后续接着本计划，不重新做已通过的组件、不删未完成Task2、不将被动清单当许可。详见production/v170报告与本计划ledger。
