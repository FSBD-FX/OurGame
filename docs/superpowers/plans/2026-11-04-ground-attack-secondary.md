# 地面挥刃附件受力 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 真实地面挥空具备来自既有攻击四阶段的连续附件惯性，保留原玩法和命中差异。
**Architecture:** 权威动作→朝向局部无单位驱动→原有界弹簧→现Formal消费者；测试先验证未接入的真实缺口。
**Tech Stack:** 现Godot/.NET C#及原native owner。
**Spec:** ../specs/2026-11-04-ground-attack-secondary-design.md

## Global Constraints

唯一G:/knight/Knight，1888输入文件数保持；无付费/GUI/OS输入/dump/新媒体/自动化/外部Git写入/删除/真实存档。原数值/时限/敌群/单跳/动作ownership与所有失败不降。native期间冻结源码/harness，根唯一执行者。

## Review Focus

- Facing翻转、Up aim或来源接管不能形成错误朝向/第二次起势。
- 相位边界/取消/完成需要连续回稳，不持有新的计时/攻击状态或补假hit。
- 新驱动叠加真实加速度/命中/落地后仍有界有限，原零delta和分帧合同保留。
- 真实J→Combat→Timeline→Formal/脚掌支撑不能由手造表现帧替代。
- 组件/无窗口专项不代签资源/Main/正常速度与整十一项；原恢复4帧窗口不扩大。

### Task 1: 原地挥刃的真实二级来源

**Files:** production scripts/presentation/PlayerPresentationCoordinator.cs、SecondaryMotionModel.cs；tests/unit/PlayerPresentationTests.cs、SecondaryMotionModelTests.cs；tests/integration/ThrusterPresentationRuntimeTests.cs。
**Interfaces:** SecondaryMotionInput原七参数保留，末尾optional Vector2 GroundAttackDrive=default；Timeline私有GroundAttackSecondaryDrive(PlayerActionFrame,int)返回局部Vector2，无native持有。Formal接口不变。

- [x] worker只写现Timeline行为反例，根写保存Player零速J的实际源/节点/支撑反例；分别跑thruster-rig-compatibility、thruster-presentation-rig，观察行为RED再改生产。
- [x] 根实现两个生产文件的四段驱动/有限化/有界target合成，补极值/非法/归零/分帧/翻面控制，原无驱动输入逐项保持。
- [x] 原两门GREEN后跑动作/地面步态/爪与恢复兼容；一次独立范围审查C0/I0/M2，两项测试补强延期，无二审。

### Task 2: 已证恢复夹具启动前提及同版宽门

**Files:** tests/integration/PrologueAudioDirectorTests.cs仅顶部RecoveryFeedbackSurvivesOwnBoundary，其他两个原Task10方法不碰。
**Interfaces:** 原签名和4帧恢复窗口不变，消费既有WaitForPlaying。

- [x] worker按v242实际RED仅迁移真实就绪边界，根验证thruster-recovery-feedback；后续退出6/2资源strict RED经原mix释放握手GREEN，不改4帧。
- [x] 冻结同版跑pre/post/模型/keyboard/geometry/存档及普通Main/Explore，读全部终态和source/helper/binary/user/continuation保护，更新README/稳定待办/反馈/报告。

两个任务共享的是同一编译/验证时序，不共享生产接口；全部原十一项和R-I/R-C/R-P未验收，不将本计划步骤执行完等同正式完成。

全部步骤执行，整体仍未通过：当前同版十五门12PASS/3FAIL（pre/geometry原生finalizer FATAL及Main899）。生产来源与恢复指定夹具已证，但没有完整序章、正常速度美感/力学/硬件/真人认证，不签complete或发行冻结。
