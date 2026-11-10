# 纯键盘近处敌人优先

2026-10-26，KNIGHT。用户明确回答“允许调整近处敌人的优先级（推荐）”，对应问题是：同一瞄准锥内，距离更近且确实可交互的敌人优先于它后方地形；真实遮挡、射程和禁抓材质保留。不是恢复8月旧ThreadAbilityModel，也不是改K输入、能量、牵引物理或玩家伤害。

事实：v131同边界ghost available/HP2、221.39691远/9.795°，更远AscentGrip2在0°所以按原排序获选；未测该ghost LOS。新规则必须复用本次真实resolver的资格/LOS，不能用此记录直接发K。

## 方案与开发选择

不采用“所有敌人永远优先”：会抢更近墙面、机关或锥外目标。不采用遍历中两两敌人>地形比较：容易形成非传递比较、随遍历顺序变化。

保留原完整baseline（角度/距离/稳定path）。收集本次Consider已经过滤并通过真实LOS的全部敌人候选，再在baseline为移动目标时，仅保留严格更近的候选，按原角度/距离/path规则挑选。不能只记全体敌人的最佳角度者，否则其在墙后方时会漏掉另一只近敌。提升池固定path遍历，不把带epsilon比较冒称严格Sort全序。

开发默认，已向用户说明：普通移动锚点与墙/顶面同属移动目标；明确机关目标保持原规则。这不是用户逐项给定的新增配置。判别不能只看IsLegacyInteraction：缺adapter的Mechanism/Enemy仍应按其显式Kind保护，不被当普通地形。

提升资格：ThreadTargetKind.LightEnemy或HeavyEnemy；复用原TryGetEligibleReceiver结果，不试调实际交互；支持IThreadInteractionReceiver，或LightEnemy+IThreadPullReceiver。实际可分派的LegacyThreadInteractionAdapter须存在、未排队删除、指向本target且速度配置有限为正。只影响新增提升池，旧baseline准入不顺便改。死亡/隐藏/失效由现有available/group/health门过滤，命中时仍由原接收者决定是否接受。

原材料、同world、有限坐标/方向、620实际配置射程、22.5°现有半锥、caster排除和LOS规则全部保留。重用原射线结果，不添加绕墙Ray、第二Resolve或缓存保活。若敌人不更近、同距、不可交互/遮挡，则原baseline不变。鼠标旧适配、既有锁定快照/发射后转向、机关和自动Reel均不改。

## 验收

原KeyboardClawTargetTests/Runtime/Model全部保留。先真实保存Moth(40,-200)/金属面(0,-300)的有效RED（原轴线面胜），再实现GREEN；选点不调用adapter/拉动敌人。补近/等距/较远、多敌人过滤/稳定path、冰/禁抓/锥外/超距/遮挡/死亡/撤组、缺receiver/adapter及显式Mechanism反例。

原canonical keyboard入口原本只跑Runtime，本次明确接上TargetTests，不以旧绿冒认新测试执行。普通pre/post/models、Main/Explore及既有抓取转移必须回归；完整通关、正常速度美观、硬件真人、资源句柄问题仍独立OPEN。不付费、不自动弹窗、不改真存档、不Git写入/删除。
