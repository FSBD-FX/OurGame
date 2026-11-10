# Ranged Recovery真实安全时钟 · v224

原十一项目标不变；本轮推进正常Main通关证据，不改正式玩法或美术。事实来自当前Source1888、v223 Main0e3db618和两项独立只读调查。前轮是PROGRESS（真实制动生产修补与18当前阶段证据），不是无进展或等待。

## 事实、判断、风险

Main最后Rust1HP不是盾卡死/过热：生产J成本12，驱动保留24并要求完整J .30s＋8tick。尾部7030真命中后余18.133，7051冷却好但安全窗只.2167；不能靠降成本/缩安全边界或加899帧预算解决。前段Hop6780后Rust仍3HP，6849第一J到2HP，6868才满足既有后跳K；不能声称早按朝向就能提前K。

当前ObserveEncounterDanger把ranged Recovery剩余时间当新危险截止。生产RangedWeaponModel每Step只处理一个阶段，Recovery→Idle→完整新Telegraph→Active；公开只读AttackTelegraphSeconds来自私有模型数值。已捕获BeltBurst为1.10s，不是Rust的.65。恢复中取消/休眠/reset会丢弃剩余Recovery，因此“r+T”过于乐观；但正常挂载代次仍须T。真实旧弹、授权Active间隙仍独立产生0窗口。

选择：仅对有效、活着且有authority的RangedWeaponEnemy.Recovery，使用模型捕获T减模型相对边界容差；不加Recovery、Idle、样本步数。Telegraph仍取当前剩余；其余敌人/未知状态保持原值或0。每次原始观察和实际J释放后重验，不缓存未来window。人手强行_Ready/换代后旧窗口不可续用；正常路线冻结源/配置，不能把actorID当任意重配置保证。

替代方案：保持阶段截止会继续错失可行动作，但安全保守；直接减安全余量/改单刀能量或扩帧数改变原合同，不采用；相加r+T对取消不安全，不采用。本选择属于开发判断，不是用户批准了新的战斗机制。只验证当前正常挂载/原输入，正常速度和完整质量仍待验收。

## 实现与证据

只改测试/验路消费者和测试入口，不改production C#、资源、场景、数值、敌人排序或原合同。既有Task10PassiveRearmAllowance扩大已知Ranged.Recovery白名单；ConsiderCounter优先在该状态调用它，所有真实leaf/Active-gap否决保持。

先写保存Player+BeltBurst、真实键盘L防住自然三发后的短Recovery原生夹具。支撑地面/HP5不恢复；剩余Recovery小于完整J+8，但下一强制T仍足够时，原消费者必须实际RED。修后真实J命中/消费12、完整结束再8物理帧零新Active/发射、HP不变；本夹具是显式隔离布局，不是Main通关证据。模型字面量测试补正常循环/Cancel/冻结设置后重建，保守T而非r+T。已知Active/在途弹仍0、Telegraph精确剩余、未知/取消/reset清理和旧混战所有负控保留。

原“短共同Recovery必拒绝”应改为“实际危险界限短于完整J+8才拒绝”，保留自然负例和实际无Active/发射证据，不能仅删除旧安全检查。最后必须跑当前Main/Explore原headless normal-plan600s、前180s/后300s、models/actions/keyboard和全部受影响ranged混战/模型/actor项目；保存全部失败。原899、不改HP/恢复/爪键序号/角度优先/攻击账。一次fresh整体审查，C/I一次TDD修补，Minor延期，无二审。

仅G:/knight/Knight，无窗口/OS/付费/新媒体/真实C存档/额外dump/运行时替换/自动化/Git写入/删除。根唯一writer/nativeowner，任一owned build/native/outer存活时C#/helpers全冻结。保留dirty工程和精确旧文件基线，最终当前源/二进制与保存清单绑定，媒体只流程内20GiB检查。

## 审查后一次修补说明

新增括号意外改变了旧已知Stunned无自有攻击时钟，原saved-actor实际RED后恢复旧合同，不通过改测试期望消除失败。新Recovery仍要求authority。全Main更早Burst收尾暴露Hop周期认领早于真实Rust返回的集成问题：只将真实完成前提加到新Telegraph周期准入，保留stage1来源绑定避免Rust返回租约循环等待；原认领后全部退休/三发/HP/跳数/899断言不改。20准入负控先失败再实现，整线结果单独验收。两项Minor按v224报告延期，无二审。
