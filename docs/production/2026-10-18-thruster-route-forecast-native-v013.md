# 短喷消费者快照与原路线迁移 · 原生证据 v013

2026-10-18，21:09后记录。完整1–11工具goal ACTIVE；短喷计划Task1–5仅在各brief范围complete，Task6/7仍in progress。此为开发证据版本，不是正式试玩版／整关完成。承接[v012](2026-10-18-thruster-world-jet-and-voice-native-v012.md)，历史原件不覆盖。

## 事实：本轮实现与真实运行

- PlayerMotionModel公开其原持有不可变settings的internal只读接缝；Motor.MotionSettings转发实际Ready快照，未Ready抛错而不造默认。没有新增一份配置状态或改运行积分。真实保存Player用非默认175/800/1600启动，作者Resource热改500/50/10后仍依原参数运动；15个真实完成move给600→402.19203、横移121.98890和重力400，D达到原175上限。
- tests/presentation/FreeMotionForecast为无Node／无运动写权限的纯值预测。Neutral和Reverse共享现有ThrusterDynamics，读取调用方实际motion/module；地面线性摩擦、空中指数阻力、反向过零分开。每tick以末速移动整步，区分峰值前向／末净位移、阈值／过零／耗尽。独立字面参考：空气15步121.9889；地面20步95；620空中反刹13步peak59/net58.75/vx−15；零空阻15步150。旧2800预测被115..130字面落区负控拒绝。
- Upper lower_to_mid原fresh .5由旧手动fallback迁移到已有真实物理键盘driver；实际Arrived→同cast Cancel，无Boost、额外Space／Shift／恢复。完整胶囊与原平台顶决定站立中心Y120，不再用zone中心132。真实81/150tick落原MidSwing，两次up-contact和原zone均通过；末横速196.66667，不冒称已经停稳。
- 相邻BrokenTooth与Upper entry同源回归通过99／98tick。释放首Motor速度由priority100同releaseTick观察，方向命令在PhysicsFrame/Input前逐tick发出，避免ProcessFrame跨多physics造成预测提前刹车。保持原平台、zone、150tick、HP／危险／碰撞；没有live teleport或手动Motor。
- Bell局部fixture原出生(520,574)穿过保存CourtyardFloor底部。仅pre-tree改为(640,358)，独立核对原CheckpointFloor矩形与全胶囊净空；owned KillZone重绑只计数、不能回血或传送。断言实际Door collider及左向normal，不再只看x阈值。实测blocked665、cross839.4、return652.3、一次cast/latch/adapter、0恢复且原支撑。此case仍是legacy adapter输入，不冒称正常全关纯键盘。

## RED → GREEN（各时点原件保留）

- 实际快照缺失：204550-5a1926e5 engine1/535ms → 204721-8f853f89 engine0/1184ms；fresh0/0，source1693，guards=[]。
- 共享预测缺失：204844-15fa60ae 1/520ms → 204954-62896e60 0/581ms；实现后source1695。
- Upper原精确case：205058-282983d2 1/3361ms，Reel第16帧提前取消，最终(264.99634,160.6107)从未进入落区 → 205245-e6f79b05 0/2150ms，81实际tick。随后逐physics采样补强纳入下方冻结版。
- Bell原精确case：205410-3ff151ec 1/1728ms，落至(540.0704,1183.0139)并Recovery路径未解析 → 205545-051a1064 0/4162ms。保存sector/门/伤害/原时限不改。
- 所有上述fresh均0警告/0错误构建，三sink已核对、guards=[]。first-green不代替下方最终冻结回归。

## 最终同源16门

batch82247终态0及72591终态4，全部终态后才更新文件。source1695；共同source inventory SHA256
`C7261624700E72EDA0A494D85816A44ED60542E8453D15FAD8889B13D75CF633`。
每门fresh Rebuild0警告／0错误；source/helper/user/binary四库存对全等，guards=[]。三sink全量扫描、关键失败段核对。**12 GREEN／4严格FAIL**，不是整关完成率。

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。每case保留provenance、stdout/stderr/external、build与before/after/binary。

| phase | case | engine exit / ms | strict |
| --- | --- | --- | --- |
| traversal-broken-tooth | traversal-broken-tooth-20261018-205717-029-3f399cb9ea214062af273b2741746a77 | 0 / 2376 | GREEN |
| traversal-upper-entry | traversal-upper-entry-20261018-205748-625-8039ab8edc414145b7bcfed68f3b4e62 | 0 / 2509 | GREEN |
| thruster-upper-mid | thruster-upper-mid-20261018-205820-297-164b41200fc6493c98902ac8050c20cd | 0 / 2023 | GREEN |
| thruster-motion-snapshot | thruster-motion-snapshot-20261018-205852-249-5d64408af0c64feea2a6222fab0b7596 | 0 / 1188 | GREEN |
| thruster-free-forecast | thruster-free-forecast-20261018-205929-056-21df0e96f9524c71b82ee73f79a070eb | 0 / 535 | GREEN |
| thruster-bell-gate | thruster-bell-gate-20261018-210011-792-89542e144af74b04ac55fb74f9e6c426 | 0 / 4079 | GREEN |
| thruster-player | thruster-player-20261018-210043-907-de4c34857932439386d770e23001c3e5 | 0 / 22823 | GREEN |
| thruster-pulse | thruster-pulse-20261018-210137-836-7d27b6036f9648f78fa9302d921dd099 | 0 / 517 | GREEN |
| keyboard | keyboard-20261018-210209-528-f109512b7edd46eb9bbe7ce316ec0d4c | 0 / 9720 | GREEN |
| actions | actions-20261018-210246-969-31b9108810334334bb574834f0446647 | 0 / 7130 | GREEN |
| thruster-impact-reentry | thruster-impact-reentry-20261018-210323-548-6fb75d0a3e5642c4b50ccfb9bffb1ff9 | 0 / 1247 | GREEN |
| thruster-formal-module | thruster-formal-module-20261018-210353-258-0dcb0f3ea423416a946be8794c3e173d | 0 / 684 | GREEN |
| thruster-rig-compatibility | thruster-rig-compatibility-20261018-210420-903-a4b5e5fc37de4924a354f02490e6a80b | 0 / 688 | **FAIL** |
| thruster-world-jet | thruster-world-jet-20261018-210450-108-f966f0c96c374788a4ab9a99ccee43d3 | 0 / 3604 | **FAIL** |
| prologue-pre | prologue-pre-20261018-210521-892-9c95e06286b746eab037a83d41d61145 | 1 / 89488 | **FAIL** |
| prologue-post | prologue-post-20261018-210719-338-44bb25de70ae4f7b8384f2ca5360584f | 1 / 10563 | **FAIL** |

## 推理：前置失败已推进，后续失败不能遗漏

1. world-jet全部行为marker仍通过，engine0，但stderr退出2 ObjectDB，strict FAIL；不改清理帧／GC／cache／borrowed Dispose／日志策略，不关闭v012对应自有WAV额外引用的问题。
2. compatibility仍原两Profile警告。只读定位明确来自PlayerPresentationTests刻意无效Visual override和非Node2D Hurt cue；不是正式Player配置损坏证据。应保持同一生产拒绝／fallback核心，并结构化验证诊断；不删负例、测试flag静音或豁免日志。尚未实现此接缝。
3. pre本次已过entry_to_lower及lower_to_mid（广域78tick），首路线失败推进到**mid_to_high_swing**：旧fallback第12帧Reel即取消，最终(474.924,119.99598)，149/150、未进原落区。另保留两Profile和四GetPath离树错误，后者来自ThreadInteractionTests手动离树Motor._Ready的诊断路径。本次无FATAL/七Handle，不据未复现关闭历史故障。
4. post本次已过真实Bell和盾区，但随后**AssertGripStoneLatchAndLanding**读取保存CanyonDescent的Gameplay/GrappleSurfaces/GripStoneLesson与Gameplay/Collision/EntryShelf失败，target.GetInstanceId空引用（3997）。不再把本次首失败写成旧Bell/Recovery，也不等同此前Definitions-null/FATAL。须按当前V2场景保留原教学/落地覆盖并迁移，不能删case取绿。

## 风险与下一步

- Task7只完成快照来源、纯预测和三条fresh辅助路线／Bell局部fixture，未迁移全11文件数值消费者、旧fallback／continuous helper／正常Main+Explore。正确的燃烧判据、抓爪环境风/V1相机权限与旧Step兼容unit不一刀切删除。
- SentryCounterEnergyBudget当前驱动是地面松键停距，实际持续全胶囊支撑；应使用Neutral(onFloor:true)与真实模块摩擦，不误用Reverse。其60/64/8px窗口、真实速度≤2、原deadline与相关BellBolt时限须共同验证；尚未更改。
- 先迁移已复现mid_to_high与当前GripStone覆盖，再处理Profile结构化负例／离树诊断、其余停距与正常双路线。Task6音频退出／最终像素连续听感／真人仍OPEN；最后才一次fresh whole-seven-task review。
- 五历史FAIL、七Handle、FATAL/null与居民RID不因本轮局部通过关闭。新常驻正式仍0/50、24canonical未实现；正式127／候选132、全部正式资源／UI声音演出、同版正常双路线／旧档／显示稳定60FPS／真人／Windows ZIP及授权GitHub读回归档仍OPEN。
- 正式Player SHA256仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。无新展示媒体／图像或音频生成／付费／Git写／发布／自动化／OS键鼠；真实userdata只保护库存哈希。旧版本、失败原件及用户原dirty保留。

持续账：[总待办](2026-10-18-project-remaining-backlog-v001.md)、`.superpowers/sdd/2026-10-18-thruster-player-integration/progress.md`。

