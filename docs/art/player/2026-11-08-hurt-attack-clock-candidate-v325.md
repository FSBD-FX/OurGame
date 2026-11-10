# 受伤后的攻击表现时钟 · 只读候选，未修／未native复现

v325地面Up专属姿态已通过局部／同版回归；本页不是新生产修复，不改变当前冻结产物，也不把旧测试要求的“同攻击恢复”当成完整动作时序正确。

## 源码事实

- 保存Player共用PlayerTuning默认startup .06、active .10、recovery .14，总.30秒。
- 实际PlayerCombat每物理步Tick；有效窗.06≤age<.16，非致死Hurt没有停止Combat模型。
- Presentation Timeline的ResolvePrimaryAction先处理Hurt；.055+.125=.18秒内返回Hurt，绕过下方_attackElapsed累时。Hurt后又以自己的累计.30配合!IsAttacking判断结束。
- 旧InterruptedDown用纯Timeline要求same-state resumed、连续和回握，不检查真实Hitbox；v325真W/J检查同攻击接续与无额外AttackStarted，也尚未对比恢复Phase与实际有效窗。

## 候选推理，不是已测帧数

理想60Hz、无outgoing hitpause、前两拍攻击后第3拍Hurt：第13拍真实age约.2167已收势，表现可能才.05仍准备；第14拍表现Burst而真实age约.2333，实际hitbox早在.16关闭。漏计约.1667秒来源是十次提前返回，实际Process/Physics节奏需原生采样，不能直接把这些帧号写作实测。

这可能造成“受伤结束后补播过期发力”，而不是新增输入或实际额外伤害。它属于用户V03/R-C动作可读性；仅凭旧测试resumed=true不能否定该质量问题。

## 下一实际门

保存Player、真实内部W/J、实体地面、无敌人／无Outgoing连接，匹配无Hurt对照；真实零击退非致死伤害。Combat后物理观察点记录AttackStarted身份、Hitbox.Monitoring开闭、physics序号；FramePublished同刻记录State/Phase/time。前提必须证明有效窗确实出现、伤害接受、身份未变、接续与最终回稳；无Hurt的正常发布容差先测，再判Hurt结束是否补播过期Burst/Contact，禁止空跑假绿。

若RED确认，仅让隐藏攻击年龄在Hurt期间前进，仍由同一时钟和原ID恢复为应有Recovery，可能同时保留旧same-state resume和实际Combat规则。不能直接改成!IsAttacking立即None，不能取消真实攻击或扩到爪／盾／所有接管，不能取消既有.025命中停顿。须先实际RED，不在当前冻结v325猜修。

本页由只读explorer精确追链：PlayerTuning.cs25、PlayerCombat.cs97、PlayerCombatModel.cs30、PlayerPresentationCoordinator.cs420/450，PlayerFormalVisualTests.cs1841及PlayerGroundUpAttackTests.cs149。没有代码变更／native／GUI／付费／GC或新的完成声明。
