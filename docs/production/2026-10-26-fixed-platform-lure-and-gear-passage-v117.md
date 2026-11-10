# v117 · 固定平台诱导与齿轮区实际通过

状态：**齿轮区自动探索卡点已突破；完整序章、Task15和原十一项仍ACTIVE。** v117是开发轮次，不是发布包或GitHub标签。工程仅为 `G:/knight/Knight`，另一个UE项目不在范围内。

## 事实、推理、风险

事实：仅改 `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`，相对认证v116实物基线201行新增、6行删除，无源码新增/删除。生产力学、AI、地图、碰撞、HP、能量、敌人数量、正式图声未改；v116旧式机械爪命中惯性修正保持。

推理：旧驾驶在移动升降台旁寻找60px近战站位，不能同时满足完整平台支撑。新驾驶用原AI的Approach/Guard，在固定PistonTwo上先退远诱导，再走回合法站位。这证明现有机制能够实际执行这一战术，不是新生产技能、整体手感或美术升级。

风险：探索随后仍在Steam死亡，Main仍发生死亡回档；历史Handle/FATAL没有根因修复。无新游戏前台/系统键鼠、付费、自动化、Git写入、打包或删除。用户最近游戏显示结论仍为 **NOTSEEN**；网页显示不算游戏验收。

## 1. 实际失败与最小修正

- `002543-385`：原AI确实Approach→Guard，loop220/ticks167时Bell=(2809.582,3828.541)。仅6轮后玩家x2886.4893/vx−220；立即松键会停2874.8226，继续两次A采样再松键却到2861.2114，越完整支撑左界2864。原包络断言在坏移动前正确拒绝，不是已发生的掉台/伤害。
- 修正：没有真实guard需求、且实际方向包络将越界时，真实释放A/D、重算同一包络，只消耗原外层一tick。真实guard不为通过包络而取消；完整支撑、55–64横差、1.5位置误差、900外层迭代均不放宽。
- `004903-966`：四次实际松键制动后，loop237/ticks185完成实际采样交接。Player=(2870.433,3799.9253)/V0，Bell=(2809.582,3805.7415)，HP1保持。但旧generic后来仍失败。
- 同控制观察案 `005432-499`：原75节拍的300/375/450/525/600/675/750/825轮全部ready=false、planCaptured=false，resolver根本未被调用；239–243及301以后却出现实际ready。不能把“平台可能竞争”的几何推算写成本案已选错目标。
- 最终仅借用实际保留Bell、完成新诱导的Explore调用改为真实gearBellReady时尝试K；其他调用保持原75节拍，包括Steam。原同目标/LOS、完整支撑、两次新鲜中立、无威胁、前缀2/采样8/完成45、实际application+1/Exposed/HP不变均保留。

诱导为原循环中的独占一tick分支。await前后检查同Player、同Bell/parent/HP6、group所有权、完整指定PistonTwo支撑、无跳喷及恢复计数；未完成时不落入旧的其他移动/J/K/L命令。近/远站位由保存形状、SafeMargin和原GuardRange推导，没有写角色姿态、强选目标、第二resolver授权或能量回填。

## 2. 最终原生齿轮战斗

最终Explore `005855-809`：

- loop237/ticks185有独立 `handoff_sampled`，不是仅stage4/finally摘要。
- loop239同次计划selected==target、Left/LOS=true，原CastAt实际application0→1、Exposed、HP1。
- loop305同一Bell第二次application1→2、Exposed、HP仍1。
- 原 `TASK10_ENCOUNTER_INPUT_OK`：Gear attacks=7/started=7/connected=7/thread_casts=2；最终Player=(2867.8445,3799.9253)、health1→1、damage_events=0，随后实际进入Steam。

七次Connected不改写成七次有效扣血。原敌人、机关和伤害仍活动；局部Gear通过不等完整路线通过。

## 3. 后续失败及资源边界

Explore在physics6289恢复request1/completion0。同步AcceptedDamage锁定常驻 `canyon.v2.resident.steam-valves.03`、`scrap-charger.tscn`、RushHitbox、attack2，HP1→0；错误名“RANGED”不是致死者类型。

致伤时盾held/active=true且朝Left；energy=.41399932、heat=.175、未过热、attacking=false。6288最近决策选择另一只BellBoltSentry为danger，Rush命中来自右下。阀已关闭喷口、此前落地fall_damage=0、Steam Bell末次J在6163。不能拿伤害后清键日志覆盖同步致伤时盾已开的事实。下一只在现有同次危险候选观察口核Sentry/Rush顺序、urgency/replaced、弹体与攻击身份，再决定合法防御/闪避；本轮未实施，不先删敌人或加血。

资源只读核查：Basin `OpenSample.AddChild` 包含整根Ready；World先对全部五定义各离树Instantiate/Free校验，再真实mount初始三区。正常V2也走共同链，不能据此归罪Supply或临时校验；ResidentManager创建更晚，不能假定覆盖此前冷启动窗口。尚未找到已证实的缺失owner。本轮pre/post安静不关闭历史Handle/FATAL；未加GC/缓存/保活/引擎补丁、未附加调试器或抢前台。native调用者owner仍缺针对性证据。

## 4. 全部原生案例

根目录 `G:/knight/验证证据/october13-stable-claw/`。八案均build exit0、未超时、sourceRows1844、expectedFailure空、guardFailures=[]。严格 **3 PASS / 5 FAIL**；最后同冻结五案 **3 PASS / 2 FAIL**。批次51905 exit0只表示子案例均终态。

| 案例目录 | engine exit / ms | 严格结果 |
| --- | --- | --- |
| `thruster-route-exploration-diagnostic-20261026-002543-385-5c1a359003c84dfcb1b30af8343f1e56` | 1 / 102162 | FAIL，stage3 held包络越完整支撑 |
| `thruster-route-exploration-diagnostic-20261026-004903-966-7a3f89a42a5443f7be299eada0a48014` | 1 / 103479 | 交接局部通过；整路旧generic原900剩余停距FAIL |
| `thruster-route-exploration-diagnostic-20261026-005432-499-07de8209b4b14d0897ff564972a30f8b` | 1 / 110238 | 同控制观察；同项停距FAIL；所有75门未求值计划 |
| `thruster-route-exploration-diagnostic-20261026-005855-809-d859d9d4e4574a199054b3e8584a1144` | 1 / 121114 | Gear局部通过；Steam6289死亡/恢复请求FAIL |
| `thruster-route-main-diagnostic-20261026-010336-935-75e3fbc2a3854b59acc313d0bf4f71f3` | 1 / 136643 | FAIL，回档3117.98px，(4751.553,3423.933)→(1680,3960)被原无重置门拒绝 |
| `prologue-pre-20261026-010626-689-5826bbbc9e4a4800a635c53150adca6b` | 0 / 167736 | PASS，stderr0 ERROR/0 WARNING |
| `prologue-post-20261026-010946-202-8c5f0ba1178145ea9a4ea4ee1b5f8111` | 0 / 205098 | PASS，原75项0–74各enter/return，原owner8次节点1→1；0 ERROR/4原负控WARNING |
| `models-20261026-011337-998-92b6c017fd6d4c489b48638856a8e20a` | 0 / 419 | PASS，普通构建保持 |

post四WARNING由原seamless1/room-profile1/flow-registry2三个具名scope覆盖，不写成零warning。205098ms是完整测试耗时，普通post原cap300000保持，不是显示FPS。v116新增命中后Motor证明未单独重跑，不冒称本轮新增验收。

## 5. 唯一审查、裁决及延期

唯一fresh只读审查：**Critical0 / Important0 / 新Minor1**，认证物理v116基线和完整201+/6−差异，无Important修复轮/第二审查。新增Minor：finally `native_handoff=(stage==4)` 在实际await检查失败时也可能true；测试仍严格抛出。本次正例另有handoff_sampled及后续完整战斗。继承energy早期unwind摘要缺失和几何消息“swept”过强两Minor仍延期；三项均未顺手修改。

实施裁决与代价：保留用户dirty checkout/root唯一写入/无Git或新BASE，以物理快照而非清洁提交归属；用原AI诱导而非移动实体/放宽支撑，原生高度/LOS/预算仍可FAIL；无guard时松实际键替代越界held，可能暂时停在目标外、仍需原900内实际站稳；先同查询观察再仅新策略按ready请求K，规划次数改变但所有真实准入保留；复用已结束同源Explore配合后续回归，不拿旧失败拼成功；Steam与资源及三Minor保留，代价是整关仍不可交付。

逐项裁决review的26项Declined（均非豁免）：

| # | root裁决 | 代价／仍需证据 |
| --- | --- | --- |
| 1 | 最终Main确实FAIL，未闭合 | 同版正常标题至Boss/出口待验 |
| 2 | Explore整路FAIL，Gear仅局部通过 | Q01不关闭 |
| 3 | 另份同步伤害收据锁定Steam Rush，不靠review猜测 | 候选竞争原因仍未知，待同次查询 |
| 4 | 三个旧候选FAIL保留 | 最终正例不追改历史 |
| 5 | root实际跑完pre/post原75＋owner8，按其范围PASS | 历史间歇故障与双路不关闭 |
| 6 | Handle/FATAL策略未改、根因OPEN | native owner未明，不盲目保活 |
| 7 | 独立保管审计已结束 | 字节一致不代表玩法/画面 |
| 8 | M01–M09整体未验收 | 地面局部预测不认证全部频率/碰撞/接管 |
| 9 | R类整图/回返仍OPEN | 一站位不证明全部路线/秘密/时长 |
| 10 | 原76普通＋Boss/NPC未删除，E类验收不关闭 | 未重做全量census/AI/生命周期/战斗 |
| 11 | T类全机关组合仍OPEN | 指定支撑不等全图危险验证 |
| 12 | P类全操作/存档/恢复仍OPEN | 单路径按键不替代全部继续/死亡/改键 |
| 13 | G/F指引/雾/小地图未改未验 | 待真实显示/交互/性能 |
| 14 | V01–V10/D01–D02正式美术动作仍OPEN | 无新像素/完整动作审美认证 |
| 15 | U/A/C全UI/听感/演出仍OPEN | 未做正常速度视听，不扩大CG |
| 16 | Q03目标硬件性能仍OPEN | headless耗时不等FPS/长帧/资源上限 |
| 17 | Q04真人仍NOTSEEN | 无抢前台或代用户认可 |
| 18 | D05–D07打包/GitHub/发行仍OPEN | 无新运行包或Release资格 |
| 19 | 全局W/reader/stale-preview不新签 | 局部A/D与同查询快照不授权改ranking |
| 20 | 全局遮挡/receiver组合仍OPEN | 两次真实同目标应用仅局部证据 |
| 21 | Intro/准备→Playing整体不新签 | 原拒绝和真实等待保持 |
| 22 | 全局callback/deferred边界仍OPEN | 局部same actor/parent/sample不覆盖全部旧回调 |
| 23 | LoadLevel主/次清理异常合同未改 | 不把冷启动错误归因未证清理缺口 |
| 24 | 仅当前保存居中胶囊/矩形站位有证据 | 不称任意偏移/旋转/改图通用 |
| 25 | 继承energy-unwind Minor延期 | 摘要不替代完整能量收据 |
| 26 | 继承“swept”措辞Minor延期 | 边界包围盒不称连续子步证明 |

## 6. 独立保管及容量

全部native和批次51905终态、所属Godot/dotnet/验证owner核为0，独立只读审计结束后才写MD。所有native、批次间隙及审计期间保持source/helper/docs冻结。

- 八案40组manifest对逐字节一致，每案1844源/C#892。
- 每侧2091个源码输入＋42个helper实物，前后4266副本SHA匹配；48封存二进制匹配。
- 最后Explore＋Main/pre/post/models的源码/二进制manifest分别一致；当前1844/C#892及Debug恰好6文件匹配最终models，无新增/删除，仅Main相对v116改变。
- helper联集11路径无冲突且当前/v116公共项匹配；owner、MechanicalClawController、音频、HEAD/index未变。Controller标签已明确，不是PrologueLevelController（后者也未变）。

```text
v116 Main    F0B8C909AAE8D463F7C6EF4C49A3C06CABC92D9CE63D282E8A3E1ED2259F43DF
v117 Main    985A7D8B1DBFE8689A63DC1FB1B9752843BC8CAF418238B103332A7B7320031A
source       AF12B11E169044319609E8F1CCFE955A4B476C9F034E0D5B24B3816F52F38E1A
binary list  A03666012ED3C96EC2041A821EEDEAE51837D79767C77CA11584044E6BE5E1CE
Knight.dll   8865F5E72655CD09C320C7C1BBB8EE61518CF19A8D1ACFEB7398430042031A68
Knight.pdb   46385C01536033307DB1CB76697FAC850F1C3620A73268C5C65746E6D4770BDC
Claw         6DAF77B293D1F7F04FFA05E31990143E4BED8D82B463B0BFFF492C009CC3F4AE
owner        309DD6C42FB5E96F27390711FF91D2D419890CF17BA256869E2D2A22961255F2
audio bus    CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A
HEAD         e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6
index        21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C
```

既有媒体工具仅手动 `-Check` 一次：UTC2026-10-25T17:21:59.3130364，current860284/baseline3508/growth856776字节、Notify=false，未达新增20GiB。本轮新游戏媒体0；未重建旧归档或自动化。原生证据仍在上表G盘目录，普通排错不频繁采大帧。

## 下一步

继续原十一项稳定ID：先核Steam同次两侧威胁与合法闪避，随后正常双路、资源原生owner、相机/正式视听动作、目标硬件及真人；到对应阶段再确认前台/付费/演出范围。原76敌人/Boss/NPC、地图机关、操作收集存档、指引/雾/小地图、正式资源/UI/音频和GitHub交付范围不减。没有task-done、完成目标或发行声明。
