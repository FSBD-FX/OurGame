# v260 Air Carrier Transition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复同一移动状态内普通空中模板切换的突跳，保留完整动作/物理验收边界。
**Architecture:** Formal 的 locomotion 过渡记录有效模板签名；只复用现有未叠加姿态和计时。保存节点的中性几何反例与真实短喷自然交接分别验证，不混为美术认可。
**Tech Stack:** 当前 Godot/.NET/C#、既有 PowerShell 受控宿主。
**Spec:** G:/knight/Knight/docs/superpowers/specs/2026-11-06-air-carrier-transition-v260.md

## Global Constraints

- 原地脏树保留，1898 源集；不提交、清理或新建 worktree。
- 无 GUI/OS 输入/付费/新媒体/自动化/真实存档实验/引擎或 GC/cache 策略变更。
- 原十一项 ACTIVE；旧 native 故障保留；几何测试不签正常速度动作/正式美术。
- 保留 2/15 秒、时间回退和无时间兼容；只捕获 locomotion pose，不重复混入动作/二级载荷。

## Review Focus

1. raw Active 被 Grounded/AirJet/动作/非有限值否决时，不应重置模板过渡。
2. 普通模板退出和状态同时改变，只捕获一次；未叠加姿态不重复加载 action/secondary。
3. 重复/回退时间冻结，正时间仍最终抵达模板；无时间兼容不锁死。
4. 双朝向实际世界脚底/ankle，而非局部值，不拉伸或夹紧。
5. 自然非零 Coast 到期必须真可达，保留单次短喷/零跳和实际握持；残余 postmix 不伪称修复。

### Task 1: 有效模板交接

**Files:** scripts/presentation/PlayerFormalVisual.cs；tests/integration/OrdinaryAirMotionRuntimeTests.cs；tests/RunThrusterCoreTests.cs。
**Interfaces:** 原 ApplyPlayerFrame(PlayerPresentationFrame)；新增测试 RunCarrierHandoff(SceneTree)，放在既有 ground-gait 首项，不改生产公开接口或宿主范围。

- [x] 写真实 Formal 中性 JumpStart/Rise/Apex/Fall × 双朝向反例：模板入/出同帧保留节点和世界脚底，重复/回退冻结，正时间到目标；加入拒绝通道控制和动作握持。
- [x] 原宿主 `-Phase thruster-ground-gait -SourceRows 1898` 看见正确行为 RED，而非编译失败。
- [x] 在 Formal 用有效普通空中四态签名扩充原过渡边界；初始化/reset 同步签名，捕获 _locomotionPose。
- [x] 同命令 GREEN；既有 ground-gait 完整分支必须继续执行。

### Task 2: 自然交接、回归与一次终审

**Files:** tests/integration/ThrusterPresentationRuntimeTests.cs；tests/RunThrusterCoreTests.cs；本轮 progress/report 与当前入口记录。
**Interfaces:** 使用该文件现有无地面真实 Player Fixture 和内部 SendKey/Frames；新 RunNaturalAirCarrierHandoff(SceneTree)，接 Task 1 后。

- [x] 双朝向真 A/D+Shift，释放后等待一次自然 Coast 到期；记录连续发布的时间、Fall、非零旧载荷、世界脚底/身体位移，验证 rigid links、握持、dash=1/jump=0、observer=0；不设置美感通过阈值。
- [x] 同源运行 models、thruster-presentation-model、thruster-ground-gait、actions 和既有普通全17门；不放宽180/300/600秒等原预算。所有失败按名保留，不挑绿。
- [x] 当前源码/宿主/二进制封存清单校核，唯一 fresh-context 终审；C0/I0无需修补，M1记录延期，无二审。
- [x] 更新 README/backlog/反馈的一个当前摘要与本轮报告，媒体仅正常一次容量检查；全目标不标完成。

## Self-review

覆盖 spec 的有效模板边界、真实消费者、时间与后续风险；自然非零记录是诊断，不是已修该通道。没有新增物理/风格设计。采用 root 单作者，代理仅只读；用户持续推进和保留资料指令覆盖默认等待审批/提交/删除脚本。任务不做相互独立系统重构。
