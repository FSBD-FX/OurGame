# 蒸汽下落、上行与捷径回返适配 · v022

2026-10-19，Asia/Shanghai。完整用户1–11目标 ACTIVE，未完成。沿已批准计划继续Task7；Task1–5专项不重做，Task6音频及全部最终内容／交付仍OPEN。本轮只修改自动路线消费者、被动诊断和记录，未修改生产Player／敌人／地图／美术。

## 事实、推理、风险

事实：实际胶囊进入固定喷口足迹的反例已复现；原100/90内反向刹车后安全落沟。Top入口实际已经完整站在同名平台，旧操作却多跳一次；现原300内中性停稳。Grip1和Grip2站位分别真实通过，探索已通过九处上行、17步捷径返回、底部两次普通跳和两次机械爪切阀。当前探索首错推进到神殿外侧补给架的复合落台断言。两次后台Main均到真实Boss和有支撑出口，但不是最新源的正常窗口双路资格。

推理：获批惯性改变了松键后的运动，旧连续停距／固定六帧收尾／松键即停等消费者假设必须逐域迁移，不能为了旧自动操作反改产品力学。地面Neutral、空中阻力和反向刹车不是同一种减速。已测路径通过不能证明所有入口或参数都可靠。

风险：当前探索仍严格FAIL；正常窗口025940死亡来源仍未唯一配对，新的固定足迹安全合同不等于已证明其致死来源或正常修复。legacy高层中途天花板碰撞、11文件全量迁移、五历史严格失败／音频2 ObjectDB、正式新50仍0/50、24canonical、最终地图／视听／UI／存档组合／真人／正常显示60FPS／ZIP／GitHub全部保留。没有付费生成、自动化恢复或新展示媒体。

## 十三个原生案例

根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。以下均headless；各fresh rebuild退出0、0警告0错误，source1695含296 imported，并非1695个C#。无超时，guards=[]，不豁免失败。全批含阶段间隙冻结，终态及CIM无owned后才改源。

| case目录 | build ms | engine退出／ms | 结果 |
| --- | ---: | --- | --- |
| thruster-route-exploration-diagnostic-20261019-031636-567-e9e1b7c704914102b8a61448c2e26ecc | 26952 | 1／88566 | 独立喷口固定足迹RED，hazard=false也拒绝侵入 |
| thruster-route-exploration-diagnostic-20261019-032147-045-772e644a648543a9aa02c4d968ade583 | 26580 | 1／113692 | 下落局部通过；Top多余跳后FALL RESET |
| thruster-route-exploration-diagnostic-20261019-032546-356-002f50aa5a4945f886e395730d09790a | 28699 | 1／113712 | Top被动诊断确认实际早到，原控制失败 |
| thruster-route-exploration-diagnostic-20261019-033001-656-28649ddfb532479bba971447480b20aa | 27863 | 1／116377 | Top原300内6帧停稳；下一Grip1未停稳 |
| thruster-free-forecast-20261019-033350-366-012e6dfb709d41f2824594064b914c28 | 26975 | 0／673 | 独立离散预测及旧2800负控通过 |
| prologue-pre-20261019-033424-745-b2a4e61180e7423ba24f75bba20ef4f6 | 27155 | 0／100386 | 完整pre通过 |
| prologue-post-20261019-033640-160-5621678a6d464c39a482318d967586a4 | 26325 | 1／70695 | legacy ascent_grapple_three原稳定落台要求失败 |
| thruster-route-exploration-diagnostic-20261019-033930-321-c5f9ece4955f452e8e5cd27596b4aa35 | 26619 | 1／118824 | Grip1原60内52帧准备、真实K／落台通过；Grip2未停稳 |
| thruster-route-main-diagnostic-20261019-034202-968-8b41b09650804d8f95a8c6e0be2cd2a5 | 28443 | 0／197964 | 同冻Main真实Boss／支撑出口／held D60通过 |
| thruster-route-exploration-diagnostic-20261019-034726-302-00f229dd746947b4b154a76d43bc2a72 | 28484 | 1／173524 | Grip2与九台上行通过；捷径底部站位偏3.5242px |
| thruster-route-main-diagnostic-20261019-035056-866-11b2b0fb5e29432380f2963e50541ee3 | 27138 | 0／197952 | 同冻Main再次真实Boss／支撑出口／held D60通过 |
| thruster-free-forecast-20261019-035449-263-43a8bb829f1e43d4841f2f3156905f2b | 27554 | 0／590 | 同冻独立预测通过 |
| thruster-route-exploration-diagnostic-20261019-035644-357-8134bc83547f40e38104eaf7225acb59 | 29507 | 1／219440 | 底部双向合同通过；神殿补给架复合落台检查失败 |

这些是不同验证目的和七个冻结，不以通过数计算目标完成率；最新035644源未重跑Main，不能拼接不同冻结为同版双路成功。

## 四处已测消费者迁移

### 蒸汽下落

031636 priority100完成Motor后实际x4061.73，Hurtbox右4081.73越过RightJet左4078约3.73px，尽管hazard=false／damage0仍严格失败。独立oracle每tick检查完整真实Hurtbox与两侧固定喷口足迹，不依赖喷口时钟或预测自己的正确性。

仅keyboard局部控制采用实际Motion/module Reverse峰值；原100内16帧D＋6帧A，横速过零立即释放，之后原90内9帧全neutral落原Trench并停稳。最终4039.5078/v0、实测最大4042.2102，HP1、跳喷数不变。过零不等于停稳；最终由真实named floor、完整胶囊、fresh neutral、4040±3和速度≤2裁决。pointer原分支、地形／喷口钟／伤害不改，无额外settle尾。

### Top早到交接

032546 entry physics6684已完整站在SteamRejoinTop，x5009.1777/v170，真实同名接触；旧D+Space使普通jump16→17并最终FALL。不能把Step8替名当Top支持，实际诊断直接验证原Top。

仅在keyboard非爪、已经完整原Top／Idle且实际ground-Neutral终点仍在原平台时，原300内全neutral停稳，每完成tick保同名支持／HP／动作数／cast。033001实际6帧到5015.845/v0，跳16与next11不变。未到或预测越界的入口保原路径，不外推全部入口。原LiftBackLanding爪转移也实际通过。

### Grip1与Grip2

Grip1旧连续停距＋4px与速度20强制反向导致原60内未停稳。迁移到实际不可变ground-Neutral终点、±1.5引导松键；原±3／速度3／完整named支持／fresh样本／HP／零恢复／Idle／目标／LOS不变。033930原60中ready20＋准备32到4667.0854/v0；后续真实K sample6858、cast13→14，实际原Grip1落台，整个转移103/300。

Grip2旧同类控制40＋12仍4453.513/v23.655916。实际4000反刹与2200余tick加速的对称固定点约23.655914吻合此失败，但有限日志不证明周期次数。只迁移其Steer；approach40、settle12、准备和真实上升共52、整个转移300全部保留，settle预测取自身及共享余额较小值。

034726实际11帧到4449.9346/v0，12帧唯一地面Space，28帧近顶点NE选原Grip2Grip左面、真实TestMove清通；真实K、next14→15，落原Grip2和neutral tail共154/300。HP1、恢复0，无Boost／喷。九段上行共享预算分别103/154/147/141/141/130/141/141/141，各≤300；此处Grip9仍不等于FinalCatch，后续另按实际回返合同验证。

### 捷径底部双向返回

034726首错不是12帧耗尽或仍有余速：Settle仅1帧后x3883.5242/v0，误差3.5242>3。旧共享Steer拿AirAcceleration估地面停距，日志直接记录±23.6559往复。

只迁移keyboard且非flight的Walk地面域，actual ground-Neutral终点内带±1.5；保Walk90、Settle12、Leg120、整次Switch120、原3960/3880/3760目标、完整支持、两普通跳和两唯一K/application。pointer与air Leg算法未改；不把air Neutral约183tick全停塞进120。

035644 Walk实际8/27/27帧到3960.618／3879.175／3958.6143，均v0。两Leg实际普通jump18→19→20，落原Walkway3762.346/v0，再原Step3877.1714/v0；两真实K/cast29、30，每次43帧自然Idle，applications1→2→3，恢复原Left，HP1／恢复0／喷0。此证明该次旧air分支真实可过，不代表其算法全域迁移完成。RIGHT_PREP_NEUTRAL_PLAN本案未执行，不借right-facing字段冒称该D准备分支覆盖。

## 当前首错与下一个证据边界

035644已实际到神殿：beam cast53→54、Arrived、同帧D+Space Boost及真实残片6→7／feedback+1／一次保存替换有收据；不能据此说补给已领取或整段完成。

TempleSupply:795是十二次逐帧硬断言，每次同时检查完整支持、胶囊X、plate点、gate提交／可通行／门碰撞关闭及DoorClear，不是可重置的十二帧等待池。失败physics13067为(5914.666,923.9949)/vx50；胶囊右5934.666超过原5930约4.666px，已证实至少这个conjunct失败。日志未拆每个index／射线／plate／gate／door，其他条件不可默认全绿，也不能仅称没落地。

代码Walk(5896)跨目标松D后固定6neutral尾，与实际1800摩擦下260→80、下一tick50吻合。下一先保原控制补松键／六尾／十二硬断言各子项诊断；再根据实际full shelf边界迁移局部控制。不直接启用默认CheckpointFloor的preciseKeyboardStop，不加预算／挪架／改门／放宽±3。

legacy post另有独立问题：ascent_grapple_three `.2`的自由飞行预测878.4692忽略实体CatchReturnCeilingThree，原239/240、1cast/1latch后仍未落台。中途首contact来自保留的v015原生诊断，本轮post未记录同样的采样边界，不能把旧physics编号当本案测量。12px静态净空和另一回返路线不证明该输入可达。下一保持原输入／240／12稳定／15%margin，以test-owned采样前后和Claw／Motor边界记录精确release／首contact；不再猜摆荡方向或延迟阈值。

## Main成功的范围与身份

034202／035056分别与033930／034726逐字节同源及六binary；各自在本进程内由同一Player46858765828从Title到出口，Boss分别473167826817／472362520450。跨进程ID相同不代表同一原生实例。attack69于11503开始，11507真实致死、Died／arena各1；原188内166帧于11686完整支撑越门，held D60至11747位置／body／velocity／保存不变，结算各1、恢复0/0。035056位置为(7340.7603,1079.9941)。

035056 artifact4350字节SHA256 `2E45565CFF3D12D92DA8D184BBA2819F84AE2005C698F2AA1B05ED577875F003`；完成和held保存SHA均`E4357B681B2DB8CE2A49F615BB9A6693419AA72EAF5F52D03586C57C6FD441F6`。Scatter207886487011、Burst534656322256、Laser752491694867共46条原始事件与receipt逐字段匹配，真实玩家近战击败；human／operationCoverageComplete／normal_display／performance／whole_goal均false。035644无Boss／成功出口artifact。

## 保管链、保存边界与目标

十三案五日志均完整审计，external逐行等于stdout＋stderr。失败各仅对应行为ERROR，035644 Aggregate内层为同一断言；没有新增API／Handle／FATAL／ObjectDB／WARNING。pre的既有sentinel finalizer count1／EXIT_DRAIN不等于资源故障修复；未新增GC／drain或借安静退出关闭历史间歇故障。

各四对source1695／helper9（pre10）／保护109／binary6及continuation前后字节一致；实际selected两侧后台路线212、forecast166、pre181、post171逐项匹配，各六binary实际副本匹配。不同selected范围不等于源变化。最新两冻结：

| 冻结 | source清单SHA256 | binary清单SHA256 |
| --- | --- | --- |
| 034726＋035056＋035449 | 32DCF68A86B455CBF4F89A2D621E4874169ACDA11FBDCC29492C293AD0C160BB | D343077082FBCC6C24920475B349251BEC689DCE18070D33585BA2E2DC6A5FE2 |
| 035644当前 | 28C6001A9AA5DF5020A0CE9B6E962CADA17BF7F70749E2D1DD7FDD3553211DCE | D43F9BE27B131112F4CBAF669D347B73A941EAF735E701E9284FEE5E60329B14 |

当前DLL `48DBECC6DD9B1E6EA8D149BF9909144D4ED7EF851E4A6E8EC7BA7A90F728FE04`，PDB `B3BE44F08FD3B625392E493322F62E2336A3EDCA7D815DEF3DEF27613A4CA4AE`。正式Player实际SHA仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。

TempleSupply partial虽在035644 source库存，但未被212 selected实际保留。Root在任何修改前，于04:03:53另外保存65061字节同库存SHA `C3BF39547AA66C2914BA385E76AEAEE8B64E7BC66BE30B3A6BFD5FF08DED7891`及索引到证据根`temple-supply-source-supplement-20261019-035644-v001`；不回填原案、不冒称原案入／退出已复制该文件。下一phase须补显式selected后再修改／运行此partial。

用户上一前台时段已释放，本轮没有打开正常窗口或发送OS输入。全部消费者终态、CIM无owned后才更新本报告／README／总账／ledger。E空闲5746454528字节约5.35GiB，F约28.01GiB，G约356.09GiB；未来新增证据存G的询问未答，旧文件未移删。完整1–11目标继续ACTIVE，不用这些局部GREEN代替正式资源、新50、五失败关闭及最终体验／交付。
