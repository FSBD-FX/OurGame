# 摆锤外国防守消费 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans。Root唯一native；独立worker仅本任务两测试文件，运行期间冻结。

**Goal:** 正常验路不再把已确认的外国骨骸俯冲防守覆盖为false，保留完整原安全检查。

**Architecture:** 提取窄范围选择函数，经实际RED后只提升normal；与角色生产动作任务无代码接口，只有源码冻结和最终同版验证共享。

**Tech Stack:** 当前Godot/.NET/C#，唯一G:/knight/Knight的dirty分支，不提交/新worktree/删除。

**Spec:** [设计](../specs/2026-11-04-gallery-foreign-guard-routing-design.md)。

## Global Constraints

不改真实玩家/敌人/数值/地图/输入规则，原foreignUp来源/.92/静止/AscentGrip5支撑/方向检查与原899/HP/55s/180s/300s不变；无GUI/OS输入/真实存档/付费/媒体/额外原生采集/自动化/发布/删除。Root唯一native，CS/harness必须在运行前冻结。

## Review Focus

- normal不能绕过keyboard/gallery scope或原foreignUp实际消费安全门。
- contact/earlyCounter/laser/neutral/brake/bait探针仍按旧旗标，不被新Enabled无意打开。
- 实际输入须在同源攻击接触前采样，配置回执不代签防守成功。
- 目标Skitter真实威胁优先级不被外来无关攻击抢占；非静止/非完整支撑仍拒绝。
- 原失败/预算、退出资源和用户资料不因局部路由绿而豁免。

## Task 1 · 两测试文件的真实RED→GREEN

**Owner:** worker仅tests/integration/PrologueCanyonGeneratedMainRouteTests.cs、tests/unit/RangedWeaponModelTests.cs，不触碰其他CS/harness。

**Interfaces:** internal static bool Task10GalleryForeignGuardRoute(bool keyboard,bool gallery,bool normalPlan,bool explicitProbe)。新galleryForeignGuardEnabled只替换foreignUp首条件；旧galleryForeignGuardProbe及其后探针链不改。

- [ ] 提取旧keyboard&&gallery&&explicit选择，接唯一consumer并记录一次配置；16行字面量真值表预期仅两scope真且normal或explicit真时true，覆盖原空scope等反例。
- [ ] root实际ranged-models/1888先RED；必须是normal缺失而不是编译失败。
- [ ] 无native时worker仅加入normal||explicit；root模型GREEN，保留原来源覆盖负控。
- [ ] 无KNIGHT_*覆盖默认Main；实读外国guard请求/早采样/同源接触与HP，原899/HP失败照实保留，不把全场通关预定为结果。
- [ ] 当前同版全部必要门、一次独立最终审查C/I一次TDD修补/Minor延期，无二审；输入/binary/用户保护/媒体Check与报告对齐，无Git操作。

Self-review: 一个selector无生产接口，两个scope的16组合能检出漏normal和scope绕过；实际consumer另由Main+审查验证。全关/视觉/资源验收不缩减。

## Final I1 · 唯一修补通道

worker仍只拥有原两测试文件，root45133已终态后才写。新增内部scalar预算helper，legacy `!required || energy>=attackCost`先接原外层/释放后fresh/实际J前的current预算门；normal/gallery/keyboard/foreign-resident/当前owned-live Skitter范围严格限定。字面量17.3/12.3预算反例必须先实际ranged-models RED，再按原`drain*(.20+RushSeconds+3/physicsHz)`加入储备GREEN；见证/当前能量等待后重读，不改tracked观察器/实际玩法/原预算。随后正常Main与全套同版严格门，所有红门保留，无二审。此为审查I1集成修补，不顺带实现延期运动/落地组合或额外接触采集。
