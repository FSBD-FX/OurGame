# 常驻空中敌人：侧袭／下冲真实分化 v001

日期：2026-10-16（Asia/Shanghai）。本轮为局部生产进展，不是第一关交付、完整Task2验收或新增50只入图。目标保持进行中。

## 事实

此前 `ConfigureResidence` 接受 Flyer / Diver，却丢弃职能，两者都用同一高处对齐和下冲。现在仅在显式常驻配置中保存该职能：

- Flyer 在一次接敌中选定左／右侧，向目标侧方80px、上方24px接近，完整身体被领地限制；回到家点后才重新选择侧向。默认主动攻击路程上限96px，速度由同一模型设置派生（默认约274.286px/s，0.35s），不追踪锁定后的目标移动。
- Diver 保留原高处对齐、固定方向下冲、实际世界首接触和上行物理返航。
- 模型、预警端点预测、逐物理步World查询与实际MoveAndCollide使用同一有效速度。显式检查点Reset重建时保留职能；未配置旧波次设置与行为保留。
- 只修改Moth actor／常驻适配和测试接线；没有新贴图、动画素材、正式族系场景、地图布置或manager。

原生记录中的实际数值，不是概念示意：

| 情景 | 预警锁定时身体中心 | 实际攻击结束中心 | 实际路程 | 目标生命 |
| --- | --- | --- | ---: | --- |
| 左侧侧袭 | (240.66643,205.66653) | (330.3354,239.95172) | 95.99996 | 9→8 |
| 高处下冲 | (320,96) | (320,239.99219) | 143.99219 | 9→8 |
| 右侧侧袭 | (369.0002,205.66653) | (279.09744,239.33386) | 95.999985 | 9→8 |
| 同一目标锁定后避开 | 同左侧侧袭 | 仍到原锁定端点 | 95.99996 | 保持9 |
| 侧袭碰墙（墙左344） | 同左侧侧袭 | (327.9836,239.05249) | 93.48213 | 9→8 |

各攻击至少42个原生Telegraph发布步之后进入Active，完整锁定端点与实际结束中心误差小于0.1px。默认侧袭单步最大约4.57144px，下冲7px，没有瞬移。真实现有机械爪投递打断侧袭预警后，旧攻击无Active／无伤害，保留拉力和重力，身体落至约415.9335再真实返回高处家点，最大单步10.491088px；不改pose或直接注入Health。

## 先失败，再实现；测试自身错误也保留

以下原始材料全部保留在 `E:/OurGame/Knight/.evidence/october13-stable-claw/`，没有覆盖、豁免或删掉失败。

1. `resident-air-roles-20261016-200733-990-493f48da59cb4291a041999eb1d822ad`：原实现两职能同轨迹，5案均失败；其中Diver对照坐标未保证真实触地、移动目标越过领地的前提不正确，不能把这两项算作生产缺陷。
2. `resident-air-roles-20261016-200939-336-f41e7b78a45748cf80397d8279c1e760`：同一保存身体／家点／目标／地板，Diver对照符合；Flyer短侧袭、Reset后职能、侧墙三项仍真实失败，均为原垂直143.99219px轨迹。目标移动的精确位置断言仍未容纳原生接触skin，作为测试问题保留，不计生产修复。
3. 实现上述最小职能分化，并将移动目标位置容差限定为0.2px原生接触skin，不放宽攻击路程／端点／伤害断言。`resident-air-roles-20261016-201117-493-11479ead703e471898d910222157fc2c`：5行为符合，但stdout仍4 Shape RID，严格FAIL。
4. 补右侧攻击、真实机械爪打断／物理返航正控和42步完整预警观察，保留旧14＋6案例及原预算。最终7角色案例行为符合，仍严格FAIL于4 Shape RID。新增正控不冒称均取得旧实现失败。

## 最终同一生产者回测

所有case使用规范owner `verify-claw.ps1 -Phase ... -SourceRows 1669`，原55s专项／180s完整回归cap；逐案fresh Rebuild，不改超时、资源缓存、GC、退出等待或借用Resource.Dispose。以下目录均位于上述证据根目录。

| 阶段 | case目录 | engine耗时ms | 严格结果 |
| --- | --- | ---: | --- |
| resident-air-roles | resident-air-roles-20261016-201404-022-ac40ece6ab244d25af314d7001d7d694 | 21075 | 7行为符合；4 Shape RID，FAIL |
| resident-air | resident-air-20261016-201539-316-1cdad83d582d4755a10c0baa25549292 | 40362 | 原14行为符合；4 Shape RID，FAIL |
| resident-air-boundaries | resident-air-boundaries-20261016-201648-038-6c4cd2e14e8b4754b45f55d00f7f57ac | 14210 | 原6行为符合；4 Shape RID，FAIL |
| resident-air-default | resident-air-default-20261016-201729-022-bbfb1545662b4e809f889cfcdc33e69a | 5065 | GREEN，完整旧模型／原生综合 |
| resident-allocation | resident-allocation-20261016-201806-929-45690ec9b81149debe8b54e49a6f8ee1 | 504 | GREEN；50身份／24子型，仍spawned=0 |
| keyboard | keyboard-20261016-201836-014-e4f59c1127ce4bc5a6a9f37518d055cd | 9628 | GREEN |
| actions | actions-20261016-201916-034-d7e52814e42143f69c46cf24e5c993d8 | 7110 | GREEN |
| prologue-pre | prologue-pre-20261016-201950-814-629297079d3e46d7b4605fd3f17a3ec2 | 88236 | Upper错台＋2 profile警告，FAIL |
| prologue-post | prologue-post-20261016-202146-673-ecde0dbb0bff48d7a0ffe3d097ec0037 | 8126 | BellChain落地／Recovery解析失败，FAIL |

合计9门：4严格GREEN／5严格FAIL。27项空中行为符合，28份native_gone=True清理（foreign-world多一个fixture），68枚自建Shape分配／释放匹配，borrowed_disposed=0。所有9个build exit0／零编译警告错误；没有超时／guard失败。27行为、68形状不是27只或68只新增妖怪。

三份空中专项的Shape RID ERROR在stdout，外部日志未包含此退出行；严格owner仍读stdout并拒绝验收，不能以engine exit0或外部日志安静代替资源过关。所有三raw保留并扫描。完整pre长轨迹有界展示／原文保留；最终同为149/150步落在(264.99634,160.6107)，crossings=0。post仍在输入前WaitForFloor失败于(540.0704,1196.0139)，KillZone找不到六祖先ILevelRecovery。本次未出现历史6 Handle异常，不代表已找到原因或修复。

### 保管链／当前源码

9案源码清单同一SHA256：`62826100FEB5D0D351914E9098E0532149063EC1A7A76AD6B01C74C62E9189D2`。source1669、受保护真实userdata清单109；source／user／helper／fresh-binary／continuation五组共45对before-after逐字相同。全部headless、非固定帧／非movie／非verbose／无ExpectedFailure，human/naturalRoute/wholeGoalAccepted均false。真实userdata仅哈希，不读存档语义。所有消费者终止后才修改本文、README、计划、brief、意见台账。

| 文件 | SHA256 |
| --- | --- |
| scripts/enemies/moth/GraveLampMoth.cs | CD26C6BBEC2575F19B1705A4455AF46F596BE75A1694DCF22C416FA37E827581 |
| scripts/enemies/moth/GraveLampMothResidence.cs | ADA6B727B30CCD95B79E8B5A6C59E49406188EB8B771FAFC2C79A0925ADAD5EC |
| scripts/enemies/moth/GraveLampMothModel.cs（未改） | 11549032FC899D25808963CD36B3A058210C7309718D705EBA0F2976758822CC |
| tests/integration/ResidentAirRuntimeTests.cs | 7BA03675286C32B1B509164738F304D62E63B7DCFA01FCCA57E268516E41CAB8 |
| tests/RunStableClawPullTests.cs | 9DF9E6F0D1DA4C34C2838A839E3D24A1E841A39A2551ACB6E833454A878C3BF8 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | B773B3FCD72A965CF9B4A66C684021B27B475745DB519DA730887ECD80EBD3D7 |

## 推理

这轮实装了可测的职能差异，而不是重命名相同下冲。基础巡逻／拉力／睡眠／void／死亡重置的原生控制保留，但不能据此把9个开发probe称为24个正式族系场景。

并行只读调查进一步明确了旧完整回归前提：Upper lower→mid仍用手调physics／反射MouseWorld，预测中心y132而真实站立应y120，并未采用生产释放速度投影；BellChain当前sector却用旧y574出生点，且fixture不具生产六层恢复祖先。这些是已定位差异，不是所有失败的完整因果证明；尚缺释放前后速度／碰撞法线等证据，不能豁免回归，也不能统称当前纯K主路已证实不可达。本轮未修改这些夹具或生产地图。

## 风险与下一步

- 侧袭80／24／96是开发初始参数，不是用户最终数值或视觉批准；尚须正式地形、正常速度与真人手感检验。
- 预警锁定中心／实际身体端点已核对，不等于半径22接触伤害体、所有角度／墙背目标及最终美术可读性均验收。
- 4 Shape RID与历史Handle/FATAL根因仍OPEN；不靠计数推断匿名资源身份，不延长等待／强制GC／Dispose共享保存资源。
- 正式新增仍0；50个Placement全null，24子型正式SceneState仍Unimplemented／ScenePath null。旧26普通＋Boss1、目标新增50＝76普通＋Boss1保持；骨骸10／幽灵10必做。ECHO仅1个贯穿引导身份，独立友方身体／商人0。最终14区域／5家族／6职能分配不变。
- 下一直接实现Bone方向／姿态弱点和Ghost显形J/K权限：不可变profile树外配置；真实J必须在Armor累加前判方向；K须同时控制发现和最终事务，不能仅改sprite透明度。睡眠／死亡／void优先于显形。再做24正式场景及ALL50身体／攻击／返航／玩家接近的原生几何和world-lifetime manager。
- 完整Task1／Task2／Task3–4、资源、旧档、正常Main＋Explore同角色Boss与支撑出口、实际呈现60FPS、最终美术／声音／真人／审阅短录像仍OPEN。未结束任务、未作whole-plan最终审查、未缩减目标。
- 用户确认使用其他窗口，仅解释该次失焦清键。继续无窗口／不抢键鼠；无新媒体、付费生成、Git写入、版本固定、上传或自动化。
