# 地面挥刃重心接续 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans。Root生产实现及唯一native；测试worker只编辑指定测试，原生期间全部冻结。

**Goal:** 实际脚底不滑的前提下，让挥刃具有由同一权威阶段驱动的蓄力/前移/压低/回稳，而非只有肩和武器变化。

**Architecture:** 单生产文件post-mix common-load，旧转角/真实Gait/双手求解保持；不新增正式素材/动作时钟或改变力学。测试在既有typed与保存Player J入口接实际关节/节点观察。

**Tech Stack:** Godot/.NET/C#，G:/knight/Knight现有dirty分支1888输入，不Git写操作。

**Spec:** [设计](../specs/2026-11-04-ground-attack-weight-transfer-design.md)。

## Global Constraints

原十槽/18+18腿/24+24臂、PNG哈希与scale、脚底支撑、真实root/碰撞/Motor/攻击时机和全套HP/预算不变；只无窗口，root唯一native，代码运行时冻结；无付费/媒体/真实存档/自动化/发布删除。没有新用户风格批准，不关闭原十一项/R-I/R-C/R-P。

## Review Focus

- 支撑无效、非有限或空中/短喷/死亡/恢复不消费地面攻击位移；真实释放/重置不残留。
- 同一发布/零elapsed不偷跑，反向/向上锁定Aim正确，采样率比较不能用不同动态输入假装完全等价。
- 既有Body转角只应用一次，十附件共同位移且实际腿/武器/副握点保持，不只检查新变量。
- 切入其他动作保留平滑回稳；运行步态、落地压缩及原地攻击不把脚底或root拖走。
- Typed通过不替代真实保存Player J或正常速度观感，完整Main/Explore/资源红门不得删改。

## Task 1 · 行为RED

**Owner:** test worker仅tests/integration/PlayerFormalVisualTests.cs及tests/integration/ThrusterPresentationRuntimeTests.cs，生产root暂不动；与gallery两测试文件无交叉。

**Interfaces:** 在现有RunBodyLoadOnly末尾调用新的异步ground-attack-weight typed fixture；实际J位于ThrusterPresentationRuntimeTests，复用原保存Player J主动臂观察场合增加实际hip/body/sole测量（不能覆盖原断言/新造重复fixture）。不修改runner/harness或增加CS。

- [ ] 记录相同静止/满支撑四期几何，正反朝向：原地蓄力hip相对局部基线X<−.25，发力>+.25，压低>.5且≤2.2；向上X≈0。零时间/后撤时钟/30/60/120固定终点、中断/归零及invalid support等逐项控制；实际sole误差<.01、root不动、刚性链/现握持保持。阈值不调用生产目标函数推导。
- [ ] 保存Player实际J增加双朝向当期测量并记录累积范围/完整阶段，实际Action/physics/time/同Formal/脚底而非合成frame。失败后仍完成原观察/清理，保留所有实测范围。
- [ ] root原thruster-ground-gait与thruster-presentation-rig/1888实际RED，必须为缺少hip motion而非构建/旧前置失败。

## Task 2 · 最小生产接线与完整验证

**Owner:** root仅scripts/presentation/PlayerFormalVisual.cs，前一步RED完成前不写实现。

**Interfaces:** private ApplyGroundAttackWeight(PlayerPose,PlayerPresentationFrame,float? elapsed,bool resetTransitions)与private phase-target helper；ApplyBodyLoad后/ApplyLandingLoad前调用，十槽同位移，原各阶段相对顺序及后续求解不重排。

- [ ] 按spec端点/Smooth/30/sec及支撑谓词接线，旧Body转角不加倍，禁用状态清源，可靠支撑的正常取消平滑归零。
- [ ] root同两实际门GREEN，原rig/步态/动作/爪/恢复/模型和完整pre/post/geometry/存档/键盘/Main/Explore严格回归；失败原样记录，不据局部绿标全部完成。
- [ ] 一次独立增量最终审查，C/I一次RED→GREEN修补、Minor延期无二审；同版绑定和报告/README/意见台账/总待办对齐，普通媒体Check，不Git发布。

Self-review: 生产只消费既有动作/测地，不产生玩法接口；Task1覆盖全部边界及实际消费者，Task2保留原控制与整体失败。测试动态跨率只要求合理有界，不伪造精确等价。
