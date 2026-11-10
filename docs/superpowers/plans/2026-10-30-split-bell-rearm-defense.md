# Split Bell Rearm Defense Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans；root唯一source/helper/build/native owner，agents只读。

**Goal:** 修复闭甲回位分支的危险消费者覆盖，推进原完整Explore。
**Architecture:** 复用原Task10ObserveEncounterDanger/Task10MixedDefendWindow与真实键盘L；不改变生产机制。当前Armor分支仍负责原1.5/2站位门和真实K application；原Exposed分支保留真实J门。
**Tech Stack:** Godot4.7.1.NET/C#、现有完整Explore与canonical验证。
**Spec:** docs/superpowers/specs/2026-10-30-split-bell-rearm-defense-design.md

## Global Constraints

- G:/knight/Knight原十一项ACTIVE；不切UE，运行期间source/helper冻结。
- 原900physics、全程600000ms、1.5位置/2速度阈值不扩大，不写HP/时钟/pose/velocity。
- 不用L取消J/K，不借旧Exposed，完整支撑/同玩家/零恢复保留。
- 不新窗口/OS输入/付费/采集/Git/删除/真实存档/自动化。

## Review Focus

1. 非Exposed提前continue是否仍跳过foreign危险；同一原扫描必须先运行。
2. L防守是否取消正在飞行的K/J；只在实际Idle消费防御优先。
3. 盾松开尚未采样时偷发K；原neutral外加真实盾/物理L已释放。
4. 改日志或观察计数变成许可/增加等待；日志有界无await，原900守卫不变。
5. 旧Exposed/Armor误发最后J；保留原当前状态/receiver与实际HP条件。

### Task 1: 闭甲分支消费真实危险

Files: 修改tests/integration/PrologueCanyonGeneratedMainRouteTests.cs的splitBell nonExposed分支与局部有界记录；canonical不放宽。
Interfaces: 复用Task10ObserveEncounterDanger(Node,string,Node2D,bool,List<AttackTelegraphVisual>)、Task10MixedDefendWindow(PlayerShieldController,Task10EncounterDanger)。

- [x] 原完整Explore084014 native246799 exit1：HP1 Bell闭甲/Telegraph后真实攻击杀玩家，危险消费者被continue绕过，作为行为RED。
- [x] 原分支先采样真实危险，在无J/K活动时真实L防御；否则原回位/neutral/K门不变。
- [x] 保留状态/方向/error/vx/guard有界记录并执行完整Explore同源实测；第三K已实证、后继阶段已到，独立最后伤害行缺失，整体仍FAIL，不计完整成功。
- [ ] 唯一fresh review，C/I一次RED→GREEN修正；原完整回归/模型按风险执行并报告全部失败，原scope不缩。

不提交dirty root。测试完成不代表完整原十一项或资源故障修复。

v172执行结果见production报告：唯一审查C0/I0/M1，方向日志Minor延期；旧失败点已越过，后续Moth能量耗尽致死，全套与Boss出口未完成。保留workspace与原完整目标，下一为追逐可达性前沿，不重复重装组件。
