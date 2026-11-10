# v261 Air Load Handoff Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复非零喷射载荷退出到普通空中动作的可证跳变，而非只延续中性模板。
**Architecture:** Formal以独立纯角/竖直shift缓存复用共同髋轴映射，在指定边界跨2/15秒，足端entry共用边界进度。零时间inactive gap冻结纯视觉样本，同物理样本的正时间gap则有限卸载；不恢复lease。既有两个物理观察模型和action/secondary顺序不变。
**Tech Stack:** 当前Godot/.NET/C#和原严格PowerShell宿主。
**Spec:** G:/knight/Knight/docs/superpowers/specs/2026-11-06-air-load-handoff-v261.md

## Global Constraints

- 原地脏树保留，1898源集/原十一项ACTIVE，不提交、删除或新worktree。
- 无GUI/OS输入/付费/新媒体/自动化/真实存档实验/引擎或GC/cache/资源策略变更。
- 原2/15秒与presentation elapsed；无时间等价1/60步。稳定同模式载荷仍直接绘制。
- 只保留纯共同load及足端entry，不保存最终pose/lease或改变Motor、单跳、攻防。

## Review Focus

1. Zero-time inactive gap不可只保身体不保脚，更不可把失效lease或非法raw Active复活。
2. 中途暂停/回退、无时间及30/60/120Hz，过渡有限抵达真实目标而非永远低通。
3. 非零action/secondary、握持释放与强制地面/死亡/抓爪/cue不能混入过期载荷。
4. 翻面换基与新Jet打断，纯load不重复叠加，也不把全身turn误称连续。
5. 自然真实Player的正时间交接要区分根位移/根倾角和内部pose，不以world foot静止或raw旧等式作假验收。

### Task 1: 非零与gap消费者修补

**Files:** scripts/presentation/PlayerFormalVisual.cs；tests/integration/OrdinaryAirMotionRuntimeTests.cs；tests/RunThrusterCoreTests.cs。
**Interfaces:** 保留ApplyPlayerFrame；新增测试RunNonzeroAirHandoff(SceneTree)接既有ground-gait首项。生产共同映射只在原postmix位置应用一次。

- [x] 写真实保存Formal反例：Jet非零angle/lag/fold/trail→非零Ordinary（EntryWeight含0/中间/1），双朝向/None/AirAttack/Shield+非零secondary，检查十槽/world feet/握持；期中冻结、按三频率抵达、null步完成和终态/新Jet/非法/换基控制。
- [x] 原宿主`-Phase thruster-ground-gait -SourceRows 1898`实际RED，不把编译错误当反例。
- [x] 实现边界纯load缓存/共同映射/foot entry，包含真实同physics正render gap的有限视觉恢复；所有强制撤销立即清除。
- [x] 同命令验证完整分支GREEN，保留旧稳定通道合同及v260模板测试。

### Task 2: 自然样段、宽回归和审查

**Files:** tests/integration/ThrusterPresentationRuntimeTests.cs；本轮有限批次/审计/记录与README/backlog/反馈。
**Interfaces:** 原RunNaturalAirCarrierHandoff，真实Fixture/内部键盘不变；新增消费角/完整Formal逆变换比较和结束阶段，不改公开生产Frame。

- [x] 更新旧postmix现状断言为真实接续/到目标验证，保存raw world/root/消费者角；零时间与正render同physics gap由生产值模型和保存rig分别验证，不冒称新GUI证据。
- [ ] 原预算执行当前同源19阶段并取得完整严格GREEN：已实际执行最终18PASS/1FAIL，post原生FATAL未解决，故此资格仍未完成；旧17PASS/2FAIL不冲销，无刷绿。
- [x] 核对当前完整1898源、95保护对/6当前114封存binary/20宿主；一次fresh代码审查C0/I0/M1，原生门仍Important阻塞，Minor延期，无二审。
- [x] 更新当前入口/本轮报告/全部裁决，按正常一次20GiB检查；不宣布正式动作或完整游戏验收。

**Task2与本计划仍OPEN/WIP**：不能用局部几何、18个通过项或代码审查代替未过的原生稳定门。M1实际副手释放序列补测延期。原十一项目标ACTIVE。

## Self-review

五风险逐项给消费者控制与自然样段；没有给每帧美感新设阈值。已知Frame Base/raw通道不是物理权限，缓存不可修改它们。使用原地before拷贝作精确delta，continuous授权覆盖默认等待/commit/cleanup流程。后续真实画面仍需授权与人审。
