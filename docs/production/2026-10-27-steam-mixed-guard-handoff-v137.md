# v137 · 蒸汽区混合威胁与防御交接（进行中）

最新续制：[实体退场后的空Fire尾段及共享ACK](2026-10-27-steam-retired-fire-handoff-v137b.md)。同发真实Guarded→Retired→原生消失后，自动试玩不再仅凭Fire枚举持续守盾；主路线ACK共用同一实体证据。近距有效RED→GREEN、活弹丸/身份负控及全部旧专项通过，但最终Explore原900仍SentryHP2/玩家HP2、K4/J0，完整关卡及同一次C/I pass仍未完成。累计31native/10严格PASS/21FAIL；新增12案CHECKPOINT保管完成，不冒称此前19案已最终审计。原20Minor、历史资源错误、正式视觉/真人资格继续OPEN。

KNIGHT，2026-10-27，Godot工程 `G:/knight/Knight`。原十一项完整目标继续ACTIVE。本轮仍在唯一审查后的同一次C/I修复pass中，**没有完成完整关卡、最终回归或最终实体审计**。本页是制作检查点，不是成品版本或GitHub发布。

## 事实、推理、风险

事实：v136两次Steam受击的energy字段是比例，约42.2/100和66.3/100，不是能量耗尽。共享判断已选中冲锋居民的Active，但旧Sentry专用消费者会Neutral释放L，另一路旧末端命令也只看Sentry自己的威胁。生产AI、能量和盾规则没有因此更改。

推理：应修正自动试玩的多敌交接和确认逻辑，而不是降低敌人伤害、扩大护盾或改用户保留的敌人角度优先。特殊消费者也必须尊重所有其他真实危险源，并在正常空档恢复站位、完成合法反击。

风险：专项通过不是完整混战或整条路线通过。最新完整候选Main仍在配重混战死亡恢复；Explore曾被过严的旧炮台盾向断言拦住。前段又出现历史Handle错误，虽然engine返回0，strict仍FAIL。正式美术/动作/UI/声音/相机/硬件真人、旧地图迁移及完整交付都未完成，NOTSEEN保留。

## 已实际取得的证据

### 原防御交接

- 新夹具使用保存Player/Sentry和经正式接口配置领地/巡逻的保存scrap-charger。真实warning→实体弹丸→正面零伤格挡→Retired后才接管，没有伪造retirement。固定1215总时钟；演员位置只在挂载前设置，不写挂载后姿态/速度/HP，不禁AI。
- 初期Ready前检查、Completed后重复调用、charger出生过晚的失败均保留，不能当行为RED。实际记录表明charger首次落地会先进入默认.60秒Recover，再.65秒预警，因而调整的是受控夹具挂载时间，不是跳过敌人恢复或修改生产地图。
- 020732有效原生RED：physics179/180/181，真实冲锋Active、三个新样本、combat=false，但physicalL/currentShield/active全部false，能量约81.7、HP5。
- 021239先局部通过三次盾样本；完整Explore021647则实际在7418挡住冲锋伤害，Guarded=true、Damage0、HP2保持。不能只凭三次预警样本声称挡住了接触。

### 审查I1：格挡位移后的站位

唯一审查C0/I2/M1指出方向键格挡会带来真实移动；旧专用消费者没有近身恢复路径。第一次错误缺少终态坐标，未据旧Stage猜测穿越。022836仅增加失败分项，才实际证明：

```text
player=(3755.9392,4199.932) sentry=(3750,4207)
delta=(-5.939209,7.067871) gap=5.939209 vx=169
nonfinite=false too_near=true vertical=false
```

当时physical键已释放，但采样axis=1、盾仍active，确已越过Sentry且进入原8像素禁区。随后只在无当前防御接管、无待结算K/J的安静窗口，检查真实完整地板及目标胶囊支撑，使用实际module/motion的Neutral预测和真实反向A/D恢复原60像素站位。保留非有限/垂直分离/攻击禁区，不瞬移、不增加循环、不给生命或能量。

### 审查I2：合法反击被过度限制

两个问题分开处理：

1. 原全源时钟包含自己的Sentry；Stunned发布None时旧一般扫描给它0，覆盖了专用消费者已有同cast回执支持的精确眩晕时钟。023340新增实际合同先RED：自身K眩晕且另一敌人仍有正预警，ambient却是0。随后在同一次原扫描中单独折叠外国危险源，保留一般扫描的原winner/globalSafe，不全局放宽它。
2. 默认charger的最大一般安全量约.65秒，旧新代码又要求它覆盖约.8667秒的双J准备窗，因而永远不能开启第一次反击。改为自身精确时钟负责原双J准备与能量储备；每一击仍需所有外国源各自覆盖完整.30秒J加8个样本，Active和活弹丸仍否决。允许在两次反击之间正常防守，不赠送时间。

023715及025951，两种实际几何均通过：敌人分处两侧，以及Sentry/charger同在右侧；各有实际K、正的外国时钟、三次真实盾样本、真实物理J1、Sentry实际受伤1、完整J后6个新样本、HP5保持。同侧案例还观察到真实反向移动。原pre-arm及两个完整旧Sentry案例未削弱，仍有2J、4K上限和退休演员后21个实际完成样本。

这些新案例**不是杀完双敌的完整战斗证明**，也不代替正常地图实测。

### 根新增Important I3：旧盾向确认误报

首组完整候选Explore024315停在7515：盾已held/active，方向DownRight，旧断言仍要求对Sentry方向dot≥.9。正式盾从前就使用150°弧，45°偏向仍在75°半弧内。

025717先取得真实生产投射物证据：实际键盘45°盾挡住右侧Normal弹丸、0伤害并退场；同盾向的背后弹丸仍造成1伤害并退场；实际dot=.7071068。之后同一个旧确认谓词才RED，证明不是靠改断言筛绿。

仅将测试确认方向谓词委托给原 `PlayerShieldModel(PlayerShieldSettings.Approved)` 的Normal判定；held/active仍独立要求，**没有扩大生产防御范围**。025951整套专项GREEN，含上述前后接触、8个方向/零值/NaN/Infinity控制，以及两种混敌反击和全部旧案例。该投射物夹具沿用旧受控Motor-disabled/无敌时间0设置，只能证明输入/生产盾/原生接触，不冒称正常运动或保存Sentry发射链。

## 032715 有界诊断：已结束，但完整探索仍失败

真实案例 [032715 原始输出](../../../验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261027-032715-757-d1e8a6e9caf54abc81b9cbfd85d9d027/engine.stdout.log) 已结束，build 0/25892ms，native 1/148304ms、非超时、guardFailures空；原900轮不变。玩家HP2、SentryHP2，未再出现旧min8或斜盾确认错误，但没有击败Sentry。前一030930也为相同终末HP的完整探索FAIL，native144115ms，不能漏掉。

这次只增加被动观察，没有改变输入策略、伤害、能量、AI、瞄准排序或时间预算。保留first24及last128条、总1400条、tail淘汰1272次、faults0；中段serial25–1272未保留，不能声称已证明整个900轮的唯一根因。`next_signal_old_decision`明确是新physics信号/旧决策和旧扫描，不能拼成同刻证据。最后的`terminal_before_dispose`先于松键与清理后的坐标。

可证尾段链路：loop859在反击reserve不足时发出第4次K，loop866应用、872完成回收；873–890进入NaturalRegeneration，实际reserve=false、prepare_two=false，`can_begin`和cadence因短路而**未查询**，不是它们返回false。873自身安全窗1.35秒足够原双击准备窗，但foreign=.4333333对single=.43333334，后续外国窗口继续下降；不能单独归因于临界浮点差。891–899转为真实外国盾需求提前返回，J门没有执行。终末K=4/4是命令/应用，J=0/0/0是命令/接受/完成，**不是攻击连接数**。

推理边界：已证明防御需求、能量预留、K握手和外国时钟之间存在耦合；仍未证明手动玩家必然卡死，或某一生产数值必须修改。不能把“反复发爪”误说成K直接扣除了这段能量。固定Sentry反击对象与全源防御分离，属于自动试玩架构限制；暂不猜第四套反制或通过增加900预算筛绿。下一步先形成有明确可证合同的资源/战术交接方案，再决定实施范围。现有专项局部GREEN不冒称完整混战GREEN。

截至本案共19次native，6 strict PASS、13 FAIL，0 buildFAIL。未进行最终当前版全套回归或独立实体审计，单次C/I pass仍IN PROGRESS，20项Minor仍OPEN。用户U-20261027-01再次确认敌人之间保持角度优先；近敌相对其后地形的既有规则也不撤销。

## 原始尝试与当前资格

截至025951的历史检查点共17次native，6 strict PASS、11 FAIL，0 buildFAIL；最新19次累计见上节。原失败全部保留。尚无最终独立保管审计，不把根读取raw等同于所有历史实体审计。

| 案例 | 结果 | native ms / 边界 |
| --- | --- | --- |
| focused020109 | FAIL | 703，Ready前检查，不是行为RED |
| focused020309 | FAIL | 4220，Completed后重复调用 |
| focused020519 | FAIL | 4115，外国警告窗口尚未到达 |
| focused020732 | FAIL | 3560，真实外国防御需求被撤销RED |
| focused021239 | PASS | 20326，三样本交接及旧三例 |
| Explore021647 | FAIL | 141330，实际挡伤后近身几何拒绝 |
| Explore022836 | FAIL | 141971，分项证明gap5.939209 |
| focused023340 | FAIL | 3294，自身眩晕抹去外国时钟RED |
| focused023715 | PASS | 27381，两几何实际反击与旧三例 |
| candidate focused023948 | PASS | 27295 |
| candidate Main024051 | FAIL | 109065，配重死亡/恢复2987.11px |
| candidate Explore024315 | FAIL | 135261，旧dot.9确认拒绝斜盾 |
| candidate pre024601 | FAIL | engine0/161881，但Handle ERROR，严格否决 |
| candidate post024912 | PASS | 197667，不关闭旧资源根因 |
| candidate models025258 | PASS | 406 |
| focused025717 | FAIL | 1432，真实前挡/背伤后旧确认RED |
| focused025951 | PASS | 28217，全部新旧专项 |
| Explore030930 | FAIL | 144115，原900轮仍SentryHP2/玩家HP2 |
| passive Explore032715 | FAIL | 148304，K4/4、J0/0/0；有界门控证据，不改策略 |

首组六案batch75108已ENDED，failures3，之后源码因I3有效RED→GREEN再变化。因此那组pre/post/models或Main结果**不合并成当前源码的最终资格**。下一步先重跑真实Explore，确认能否越过旧几何及确认阻断，再完成相应当前版回归与有界审计；不为获得安静输出重跑旧资源错误。

最新专项原始记录：[025951](../../../验证证据/october13-stable-claw/ranged-steam-sentry-recovery-20261027-025951-305-1f2e9538784048e0a9f240d0334348c2/engine.stdout.log)。原十一项目标和完整Main/Explore仍ACTIVE，单一C/I pass尚未宣布完成；无第二审查，20项Minor保守OPEN。

## 范围、审查与后续

本轮改动限四测试C#（MainRoute、BellBoltSentryRuntimeTests、RunRangedWeaponTests、SentryCounterEnergyBudget）和canonical验证脚本；没有生产AI、玩家物理、能量/伤害、地图、资产或全局瞄准排序改动。Source仍1852/C#892，无新CS/UID；正式资产表不变。canonical要求真实斜盾前后接触、8控制、两个混敌反击、pre-arm及两个旧完整恢复marker，不允许error豁免。

[唯一审查与全部根裁决](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v137-final-review.md)。原19项加新trace尾环未计淘汰量1项，保守计20项OPEN，未去重、不顺手修。原历史资源原因、旧14份helper离线缺口及未选中LeftPNG边界继续OPEN。

并行只读检查还确认旧地图实体并未丢失：v133 models195230保存父scene中有完整旧catalog；当前DTO自身不含旧坐标，未来需限定已核对revision pair的保守迁移。旧/new均518400个8px cells，边界12440→12436，不能按旧边界ID直接复制；迁移尚未实施，不读取真实玩家存档，也不实例化旧父scene去加载当前外部叶资源。

旧地图后续依据与风险已保存为[只读侦查](2026-10-27-minimap-migration-recon-v001.md)，不代表新迁移实现。

没有新截图/视频。正常报告前唯一手动容量Check UTC2026-10-26T19:06:57.2942897：current1431609、baseline3508、growth1428101字节，16文件，Notifyfalse；未重置、未建自动化，不因本记录再检查。没有付费生成、前台/OS输入、Git写入、删除或UE项目修改。所有最终成品视听、正常速度、目标硬件和真人体验仍待验证，不能宣称Ori级或十万付费玩家目标已经实现。
