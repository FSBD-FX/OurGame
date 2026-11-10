# 死亡飞行怪碰撞与当前准备态消费者 · v070

2026-10-23，Asia/Shanghai。工程仍为 `G:/knight/Knight`，原十一项完整、高质量序章目标 ACTIVE。本轮是制作与故障闭合进展，不是完成发行或新试玩。没有新付费生成、自动化、前台接管、Git写入、删除或新图稿/录像。

## 事实、推理、风险

实际主线路线先后证明两种不同阻挡：活的常驻 scrap-flyer 可合法拦截机械爪；真实上挑 J 击败它后，HP0 的 CharacterBody 仍挡机关射线。原实现只撤受击/目标 Area，居民经理又不像旧波次那样删除尸体。现只为配置居民保存原本体层，并在真实HP0时撤本体层；重置后恢复。mask、Shape、活体休眠、幽灵隐身、目标排序、敌人/玩家数值与地形未改。

真实原生回归先 RED，修后14项行为全部通过，包括本体射线、死亡吸收、显式重置及死亡回调内第二生命；但该案例退出仍报4个Shape RID，**严格FAIL**，不冒认资源根治。主线随后实际恢复机关预览/LOS并乘梯到上端；新的首错是横廊转移，W+D合法选中新居民棚侧壁，而旧驾驶仍要求远处锚点。没有隐藏棚台或改变优先级来放行。

资源稳定性仍是高风险：最终前段原生Finalize FATAL、后段两条真实Handle错误、恢复退出4 ObjectDB/2资源均未关闭。安静的单次运行不替代根因与同冻回归。最终美术、音频、正常速度视听、显示性能和真人验收本轮无新增完成量。

## 实际改动

- `GraveLampMothResidence.cs`：初始化保存原碰撞层；仅死亡时置0，活体/检查点恢复原层，无携带旧状态的延后setter。
- `ResidentAirRuntimeTests.cs`：真实Enemy本体射线、原mask保留、显式复活与同信号第二生命。仍14个原案例，不加Shape或释放借入资源。
- `BasinCounterweightRuntimeTests.cs`：有界被动输入/预览跟踪和断言后的单次首碰查询；实际W/Up采样正确。只在真实近身Moth阻挡时使用现有上挑J，要求真实命中同一Hurtbox/Health的source与attack ID、同支撑、玩家HP4和旧Bell HP6不变。不是直接伤害/强制目标。
- `P1LoopRuntimeTests.cs`：保原启动步骤，等待实际Playing/居民提交；helper失败自行清理并保全双异常。恢复同步停机与Requested1/Completed0不变，实际完成上限180 physics；完成快照核提交、瞬态、原Level/Player及精确事件顺序。两种退出方式保持，核原Player/Input/Motor引用，经过Process→Physics→Process后才检查离树无执行。该额外边界用于完成物理轮观察，不是资源drain。
- 唯一owner：P1收据严格锚定1..180/0..4及process2/physics1；仅准入一次窄verbose退出诊断，8个纯准入检查通过，日志拒绝不放宽。一次verbose安静不计资源修复。
- `CurrentBasinLaborerClearanceRuntimeTests.cs`：精确原Steam batch0路径、原场景/无ResidentBinding，消除全场同类`Single()`歧义。真实Playing后才观察攻击。负例障碍在SectorMounted、首AI步前安装；Ready仅捕获真实出生身体/障碍包络，正式三阶段攻击不使用准备期事件。
- 两净空案例统一真实Ready总位移起点；正例额外要求Playing期间也移动≥100px，不能借准备期过门。负例仍要求出生总位移≥0且<100、真实78px净空、slide接触、300步支撑、零恢复。观察到Playing阶段约−.02px的接触退让，不能冒称原Playing净位移非负合同未改。92px通道/84高r28胶囊/顶4116与24次上层支撑要求保持。修正Ready重复断开及有序完整AttackId选择。

## 调查链（不同冻结不可拼绿）

证据根：`G:/knight/验证证据/october13-stable-claw/`。下列均完整保留普通原始日志、清单、产物与退出记录。

- Main143952：W实际采样Up/Idle/静止，预览Moth；145351：首碰真实居民Hurtbox。151030：上挑真实击杀后首碰同Moth本体HP0，机关LOS false。
- resident-air151710误含mask0期望，修为原mask后151929仍真实RED；152139修后14行为通过，但退出4ShapeRID严格FAIL。两RED/修后均有该资源错误，不能按数量认领资源所有者。
- Main152725：击败Moth→机关首碰/LOS正确→真实乘梯；下一Gallery断言失败。没有全路线成功。
- hidden145230严格PASS；exit145310两行为通过但资源FAIL；verbose150000一次quiet。review加固原实例与完整物理轮后exit152643严格PASS；最终A同一实现再次资源FAIL，不能取安静一次关闭。
- clearance152536：normal Playing位移169.17847，negative旧“初始”检查误用准备后包络。153141标量证实障碍原位、gap78、实际圆弧接触。153548出生快照正确，暴露重复disconnect与Playing净位移−.019。修正后153851被旧55s owner墙钟截断，未完成，不能当行为PASS/资源quiet。

## 最终A：同冻结9案，2严格PASS / 7严格FAIL

| phase / 案例时间与唯一后缀 | native exit / ms | 实际结果 |
| --- | --- | --- |
| models / 154126-394-8354383a83754c888ecee5c52bb2a5ec | 0 / 541 | PASS |
| resident-air-default / 154200-079-af64effec576423493d3778bbff6d9ea | 0 / 5082 | 原旧综合PASS |
| resident-air-boundaries / 154238-498-b9b6d72d7e0e426c9626480abe4cb6ca | 0 / 14342 | 6行为通过；退出4ShapeRID，FAIL |
| resident-family-ghost-air / 154327-899-e5bc93d7aac04639ba0735c2723f1df1 | 0 / 34728 | 8行为通过；退出4ShapeRID，FAIL |
| prologue-pre / 154435-343-a4973636b2604634a5ef2b360d415aa8 | −1073741795 / 42619 | Finalize FATAL；未完成clearance |
| prologue-post / 154551-873-08daa57dea3247c9b6e0257f6c901c9e | 1 / 178756 | 2真实Handle错误；Pause夹具expectedPlaying/actualIntro |
| thruster-p1-recovery-exit / 154926-072-27542f0c5567436ba605443b4a991b4b | 0 / 8329 | 两行为/cleanup通过；4ObjectDB/2资源，FAIL |
| thruster-route-main-diagnostic / 155008-706-96920ea59bd74db18085339934afe34c | 1 / 90707 | 机关/乘梯推进；Gallery W+D选新侧壁，FAIL |
| thruster-route-exploration-diagnostic / 155212-805-b34782f71d9049bd8c3a53a735d1fd02 | 1 / 53815 | Frozen-lower Rust接近复合支撑/HP/动作断言FAIL，未拆分子因 |

post不是“75案完成，仅scope过旧”：只有seamless与room-profile scope，flow-registry scope尚未执行到。真实暂停夹具仍只等Process2/StartNew/Process1，人口未提交；另有真实Handle错误。保留原scope/警告拒绝，不改owner来掩盖case中断。75注册案例与8owner全覆盖仍未通过。

## B：仅净空phase墙钟改90s，仍严格FAIL

审查后仅`basin-clearance`三个完整Level专项的owner墙钟55→90秒，保留所有300物理步、24hold、数值、AI/HP和严格日志。其他phase/整关deadline不动。旧timeout保留；这不是显示FPS或性能通过。

`basin-clearance-20261023-155421-955-cb1c392f425542c98822edc161b9717f`：native−1073741795/57196ms，未timeout。normal出生总210.0144/Playing169.17847px、完整一ID攻击、支撑82/零恢复；negative真实出生不相交/gap78/支撑300/slide接触/出生总32.63379/零恢复，并由共享移动合同正确拒绝。随后第三上层夹具前后发生Handle/FATAL，没有24hold或全clearance成功终态。不可称三夹具全通过，也不与A合成同冻结绿色。

## 保全与未完成

A普通1775清单项/891C#，source SHA `A128648D48E03DC6B66BC621885A154A6A74AC186D13E6115D2251E67A824DB0`，binary manifest SHA `DFDF0591F6166855FF885D3D3FCCBA55B691291B77715B8577EBCDAFF4395AA6`。独立审计A45/45清单对、54/54封存bin完整匹配；B5/5清单对、6/6封存bin匹配，游戏源/DLL与A相同。B helper SHA为`29372E2BE3B5EC2708437550B47A321F1F7B9E9F8D6AEDE7C97287744C6C7619`，只差owner三行说明及90秒分支，不与A拼同冻。

全部guards0、普通Rebuild0警告/0错误、无额外DefineConstants/identity类型或timeout（A及B最终案）；PID/精确argv/wait/provenance一致。用户元数据109项、空continuation前后不变，未读真实用户存档语义。退出RID/资源有时仅出现在stdout或stderr、external缺失，严格结果已核全部原日志，未过滤。

root最终live核验1775源、10 helper、6bin均零漂移；Git index仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`；diff-check与8纯准入检查通过。失败原件不删除。所有普通case无identity编译开关；没有将无窗口注入视作真人或显示性能。独立审查的两个P1身份/物理轮观察问题已修；真实Ready测量与Playing测量差异明示，任务仍未勾完。

媒体工作流一次检查UTC07:59:58：当前4838B、基线3508B、增长1330B，Notify=false；没有新图/片。不是恢复自动化。

下一顺序：先按实际RightShelf侧壁K→匹配附着/新Space借力→完整胶囊越棚→水平预览旧Gallery→第二K，验证现有键盘机制的真实转移；两段统一原150物理预算，不各开150、不增加普通跳/冲刺/恢复、不写pose或速度。此方案尚未实现或实测。另迁移Pause夹具的实际Playing与helper自有失败清理；保留LearnAttackMode精确目标与全部暂停/quit/registry。资源FATAL需可命名身份诊断，不能猜GC/保活/共享Dispose修复。

原十一项、全76普通敌人/Boss/NPC实战、连续复杂地图/机关、正式分层美术/动作/UI/声音/批准演出、正常主线和探索、恢复继续/出口、显示性能、真人键盘及ZIP/GitHub交付均不减。已有批准独立lantern仍未接正常层；本轮新最终美术/音频完成量0，未发布新试玩。
