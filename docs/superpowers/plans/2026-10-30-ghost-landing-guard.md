# Ghost Landing Guard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用既有真实键盘盾修复自动探索Grip8撤爪落地策略，保留完整路线与全部游戏权限。
**Architecture:** 一个测试专用的保存Ghost来袭源采样与实际native对照；既有落地输入作用域只在Grip8启用此采样，Grip1不变。原始全线断言继续验收。
**Tech Stack:** Godot4.7.1.NET/C#，固定pwsh7原生owner。
**Spec:** `docs/superpowers/specs/2026-10-30-ghost-landing-guard-design.md`

## Global Constraints

- 工程G:/knight/Knight；原完整十一项ACTIVE，不换UE。
- 只测试路线合法输入；无额外K/Space/Shift、HP/速度/位置写入或actor省略；原300tick/600s预算不变。
- 窗口NOTSEEN，无新窗口/媒体/OS输入/collector/真实存档/付费/自动化/Git写入/删除。
- 原生产盾/命中/幽灵阶段为权威；严格保留资源错误，组件不等全线资格。

## Review Focus

1. 旧Telegraph跨隐藏/休眠/离树继续授权L：native转换后采样必须拒绝。
2. 同cast但目标不符或未撤爪就L：扩展既有字面量正负控，活爪不可被策略提前取消。
3. 八方向盾改变横向落点：完整原路线必须仍达原平台完整胶囊支撑且HP/恢复/动作预算不变。
4. 没发生接触被当成格挡：无盾必须实际伤害，有盾必须同保存hitbox Connected＋实际ShieldResolved＋HP保持。
5. 失败/清理留L或订阅：保留原fault注入检查；新增域沿用原独立清理，不保留原生资源跨等待。

### Task 1: 实际保存Ghost与方向采样

Files: create `tests/integration/GhostLandingGuardRuntimeTests.cs`; modify `scripts/enemies/RustShellSquire.cs` new Telegraph facing entry; `tests/RunThrusterCoreTests.cs` stationary-guard branch; canonical添加选择依赖/严格标记。
Interfaces: `TrySample(RustShellSquire, CharacterBody2D, out Vector2, out ulong)` 供路线使用；`Run(SceneTree)`供既有stationary-guard入口。

- [x] 新测试先实际无盾接触与有盾接触对照；早期无接触经取证确认起手朝向缺陷，修正后采样缺席有真实掉血RED，未拿超时冒称格挡。
- [x] 实现同发布/活节点/Normal hitbox/Hurtbox方向采样；原真实Player输入和盾负责生效。
- [x] 原stationary合同、新保存ghost四控制、六失效负控及自然隐藏／新攻击转换，最终031240原生通过；保留所有失败。

### Task 2: 完整探索撤爪落地策略

Files: modify `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs` existing landing guard and ascent loop; Basin原策略正负控同步新命名/明确Grip8。
Interfaces: 消费Task1当前来袭源；既有driver真实L/八向键、同cast/target Cancel准入和原支撑完成返回。

- [x] 当前完整trace9416失败作为首调查证据；新的共享准入反例先RED，再让Grip8接入原作用域。
- [x] 只在index8且纯键盘探索启用；当前发布无效立即松L，Grip1保持原向下策略。
- [x] 实际完整trace025859及最终普通Explore032356均执行：Grip8原141／300、HP1、原动作预算通过；完整路线后续Grip9仍FAIL，不判定通关。

### Task 3: 同版回归与交接

- [x] 最终普通六阶段已执行，3PASS／3FAIL：组件、机械爪模型、post75＋owner8通过；pre恢复、Main战死恢复、ExploreGrip9未通过。原scope／退出规则未放宽。
- [x] 一次fresh审查C0/I1/M0；I1覆盖门RED→真实自然转换GREEN，无二审。项目整套仍未全绿，不授予正式完成资格。
- [x] 16案保管审计／当前1860源900CS、9helper、Debug6bin匹配；README/完整backlog已更新，手动媒体Check未到新增20GiB。历史实体缺口不回填。

执行清单完成不等验收目标完成：原十一项、当前完整双路、资源因果与正式视听／硬件真人仍OPEN。详见 `docs/production/2026-10-30-ghost-warning-and-landing-guard-v163.md`，下一步定位前段恢复边界和合法清敌／抓取衔接。

本计划承接用户持续执行授权，root原地实现、只读代理辅助、最后一次fresh审查；不重复请求开发方式，不提交/建worktree/清理用户资料。完整验收失败不标全部任务完成。
