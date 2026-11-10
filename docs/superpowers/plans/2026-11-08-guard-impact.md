# Guard-impact implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 成功普通零伤格挡有真实双臂接招／回稳，保留原十一项。

**Architecture:** ShieldResolved由coordinator过滤后发布给Formal；独立partial存纯值世界弹簧，用原表现elapsed并入原双臂／握点求解。不改Timeline或玩法权限。

**Tech Stack:** 当前Godot4.7.1/.NET/C#／规范runner。

**Spec:** `docs/superpowers/specs/2026-11-08-guard-impact-design.md`。

## Global Constraints

- 唯一G:/knight/Knight，原十一项active。只无窗口；无新图／paid／GUI／OS输入／真档／dump／Git／删除／自动化／引擎缓存GC或预算改动。
- Root独占生产／canonical／build/native；测试worker只拥有新partial。保留所有脏树及原始失败。
- 预计RED1908输入（新test partial），实现后1909（新Formal partial），按规范实际清单校验，不改规则凑数。

## Review Focus

- 真正零伤事件驱动实际腕肘，非只添加字段；旧HP／根／velocity／两段骨长／握点不变。
- 身份重复、多个来源及同帧多挡有界；无效和未挡不能虚构接招。
- 零时间／暂停不推进，30/60/120同一实时间结果；兼容无时间输入不暗中泵动。
- 放盾／新动作／镜像接管保存显示位移，不能添加新的突兀跳切或副手假握持。
- 死亡／恢复／解绑清除、重新挂载无旧事件；仅自身纯值状态，无借用Resource释放。

### Task 1: 真实受控反例及新动作

**Files:** create `tests/integration/PlayerShieldRuntimeTests.GuardImpact.cs`、`scripts/presentation/PlayerFormalVisual.GuardImpact.cs`；modify原`PlayerShieldRuntimeTests.cs`仅partial／调用、`ThrusterPresentationRuntimeTests.RunRigCompatibility`调用；modify`PlayerFormalVisual.ApplySecondaryGrip`／Coordinator事件绑定；必要case快照input白名单仅增加新partial，不改错误门。

**Interfaces:** Formal `PresentGuardImpact(ulong,ulong,Vector2)`／`ClearGuardImpact()`／private `StepGuardImpactWorld(frame,elapsed,resetTransitions)`；现ShieldResolved(result,transitionId)、PresentationTimeSeconds及ArmWristLocal／武器握点。

- [x] 先test-only保存Player受控真实Normal命中，对照无命中Player，实际腕肘无差异则以`GUARD_IMPACT_MOTION_RED`失败；无新生产类型依赖造成假编译RED。
- [x] root `-Phase thruster-rig-compatibility -SourceRows 1908`实际RED；保留原phase及11leaf/2mantle委托，不伪装全关。0953a5...engine1／815ms腕差0。
- [x] 按spec实现源／Formal／握点接线，1909同phase完整GREEN；新成功marker精确一次，raw三sink严格，无新资源策略。初次fb7b89...engine0／1639ms；最终lifecycle补控后还须同源再绿。
- [x] 相同冻结输入跑models／完整普通pre/post及必要动作专项；一次增量review，C/I一轮TDD修补，Minor记账，不重复审查。最终376c专项＋batch37884六phase真实终态，source1D555425／six-bin0E7191B6；两个Important以774279实际聚合RED→376c GREEN同一轮关闭，M1动态专项保留。
- [x] README／稳定待办／production更新，正常视听及原十一项不签完成，不混v317/v318旧程序集双路线证据。小计划程序实现／受控回归完成；不是原V03／R-C或十一项完成。

## Execution rulings

用户已选择持续自主、仅关键缺信息才问；本次已说明具体表现路线，按已批准的防守动作返工范围制作程序样段，不假称用户批准3px或最终风格。使用现唯一脏树，不运行Git/worktree/清理型SDD脚本。测试worker单文件并行，其余root串行；只有独立终审一席。成本边界是正常速度可能仍需调幅／重审，几何不能代签美感。
