# 主动肘腕与握持 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root实施生产与native；并行worker仅负责一个测试文件。

**Goal:** 在地面挥刃中增加真实近臂内部形状变化，保持双手握持与既有玩法。

**Architecture:** PlayerFormalVisual在十槽之后、双手绑定之前消费权威阶段和既有elapsed，产生有界近腕目标；PlayerFormalLayer原链求解/绘制不改。无新CS/UID、新计时器或引擎依赖。

**Tech Stack:** 当前Godot/.NET/C#，唯一G:/knight/Knight dirty分支原位制作。

**Spec:** [开发设计](../specs/2026-11-04-ground-attack-arm-design.md)

## Global Constraints

保持十槽、现有PNG字节/等比缩放、腿与支撑脚、攻击ID/四阶段/时钟/命中/数值/输入/Motor全部不变。仅无窗口；root唯一native、运行期间全部源码冻结；原预算/HP不降。无commit/新worktree/删除/自动化/GUI/付费/额外采集/真实存档/发布。

## Review Focus

- 静止点断言改成实际腕点不能遮蔽新轨迹为空：新增肩空间内收/伸出/肘变化断言。
- Up与左右镜像不得使副握点不可达：真实双手接触和链长原严格断言继续。
- 伤害/盾/爪接管、死亡和恢复不得残留攻击轨迹：立即状态与后续有限回稳分别验证。
- 重复时间/30、60、120采样不多消费运动：零时冻结与同一连续段分帧对照。
- 正常入口J不是手造帧：实际发布阶段、真实腕/握点及root/次数/HP联合验证。

### Task 1: 主动腕轨迹和保存角色接线

**Files:** root修改scripts/presentation/PlayerFormalVisual.cs、tests/integration/ThrusterPresentationRuntimeTests.cs；worker独占tests/integration/PlayerFormalVisualTests.cs。原Layer/生成场景/素材不改。

**Interfaces:** 消费PlayerActionFrame、已有elapsed与resetTransitions；输出原NearArm.ArmWristLocal/ArmElbowLocal与武器实际握点，不新增公共测试专用生产接口。

- [x] worker新增可由原ApprovedWeaponFollowsActualHand调用的非刚体形变/左右Up/采样/中断控制；旧硬编码(0,21)握点改为真实腕点，原哈希/链长/接触断言保留。
- [x] root扩展现有真实J观察器记录真实近肘腕、握点和四阶段内收/伸出，保留全部旧断言。
- [x] 全部编辑冻结，root分别运行thruster-rig-compatibility与thruster-presentation-rig，观察实际缺失形变RED；原SetupAndBasicContract已覆盖握持叶，未增registry项。
- [x] 实现设计中的目标与同时间追踪、先腕再主握再远腕、清理；实际运行GREEN并处理确证问题。
- [x] 原actions、ground-gait、formal-claw-phases、recovery-compatibility及完整pre/post、resident geometry/save、models、keyboard、Main/Explore当前同版运行，逐案读结果和严格错误，不省略红门。最终12PASS/3FAIL，执行完成不等于整体验收通过。
- [x] 一次独立most-capable审查，仅本次增量不追认整条dirty历史；结果C0/I0/M0，无修补/新Minor或二审。
- [x] 保存真实结果、源码/二进制/真实存档保护比对，更新README/稳定待办/反馈记录；媒体只执行一次正常20GiB检查，无新录像。

Task1整体及原目标验收仍未完成：三道宽门红、正式主体/正常速度/物理手感/硬件真人证据缺。上述已实测步骤不重做；后续按[v244未闭合项](../../production/2026-11-04-ground-attack-arm-v244.md)继续，而不是重复从腕轨迹RED起步。

Self-review: 设计各项均映射上述步骤；幅度/轨迹仍开发候选，正常速度和真人仍未授权/未通过。原十一项持续ACTIVE，不能因窄门成功勾完成。
