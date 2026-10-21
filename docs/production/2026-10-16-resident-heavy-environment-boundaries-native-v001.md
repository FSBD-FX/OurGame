# 常驻重装：真实外力、环境死亡与目标边界 v001

2026-10-16。权威工作树 `E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`，linked HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，分支 `codex/task6-certified-final-20260902T0555`。源库存1667；原55秒专项预算、三份原始日志和109保护行保持。已有完整目标继续ACTIVE。

## 事实、判断、风险

事实：上一轮Heavy生产适配器已实现真实巡逻/返程/Armor/休眠，10案未证明真实击退出领地、void无敌等待及跨世界目标等边界。本轮只扩展保存Heavy的原生测试和准入owner，没有新增保留的生产行为。新增13案及原10案的行为断言符合；两个专项仍报退出4 Shape RID，严格FAIL。旧未配置Heavy相关完整综合严格GREEN。

判断：不能以稀疏终点代替运动连续性，也不能用“新目标还活着”续期旧目标的锁定。补强了逐tick外力轨迹及原攻击ID的完整窗口观察；单变量移除同World条件只使foreign case失败，精确恢复后重验。没有虚构一个生产bug或因行为符合而豁免资源错误。

风险：独立World夹具不是正式关卡流送或人口manager；固定60Hz的冲量步长边界不是任意tick配置通用结论；owned清理回执不证明全部引擎资源正确释放。正式新增仍0、50 Placement仍null、24正式族系场景未实现，骨骸10/幽灵10不减。

## 本轮实际修改

- `tests/integration/ResidentHeavyRuntimeTests.cs`：新增独立`RunBoundaries`13案；真实KillZone、同floor替换、RecoveryScope计数、原生shape扫掠和确切World射线见证。原`Run`10案和预算不缩。
- `tests/RunStableClawPullTests.cs`与canonical owner：准入`resident-heavy-boundaries`，要求完整13案回执；保留三raw veto、无ExpectedFailure豁免、source1667 normal guard。
- `BellBackLaborerResidence.cs`：仅临时单变量反例，最终恢复精确原SHA；未保留玩法、数值、存档或美术更改。
- 原2 ProcessFrame清理时序保持；没有新增GC/cache/drain/KeepAlive、借用资源Dispose或日志压制。

## 13项原生边界

实际保存Heavy，完整身体宽56/高84，home `(320,86)`，领地 `Rect2(192,32,256,144)`。仅入树前设置初始Actor.Position；运行中不写pose/Velocity/HP或注入model。外力均实际Hurtbox→Armor÷3→Health：Amount3扣6→5，1800或600为受控测试输入，不是新增武器平衡或Heavy爪拉动。

| 案 | 实际观察与边界 |
| --- | --- |
| 宽地板击退出区后返回 | floor顶128/x32..672；实际最大x476.61688超过全身体越界阈值420，maxStep30，真实回home停顿、HP5、Defeated0；没有硬夹坐标或Reset |
| 单次垂直冲量 | 实际Up600立即进入Velocity；10tick后符合980重力，继续落地；两个阶段每tick最大9.769074/9.600006，小于独立推导10.1 |
| 已受外力→休眠→唤醒 | 实际Right600后休眠，10tick真实移动47.70047、maxStep9.999908；休眠拒绝新伤害，醒来逐tick实际返程、HP5 |
| 窄台击退进入void | floor `Rect2(280,128,80,40)`；layer0 KillZone `Rect2(360,208,256,16)`；原生完整shape接触/扫掠、离地/Y增加、Health0/Died1/Defeated1/玩家Recovery0，持续重叠不重复死亡 |
| 无敌pending跨休眠/source removal | 唯一真实命中启动2秒Health无敌；真实接触后HP5、死亡0、抓取层0/攻击撤权；删除source并休眠后由正常Health计时最终致死一次，没有补刀 |
| pending卸载与同定义替换 | 旧actor真正销毁、Defeated0；同scope/同真实floor ID重建同定义，跨过旧无敌期后新actorHP6/可用，旧回调不污染新实例；不是sector-manager认证 |
| 完整身体/配置原子性 | 中心合法但完整胶囊出界的定义拒绝；随后合法配置仍可正常巡逻；入树后重配拒绝 |
| foreign World负控＋本地正控 | 独立真实World中活player/body/group不授权本Heavy；随后同World活player实际受到攻击，非全局关闭combat伪通过 |
| dead body/group | 实际Health致死但body和player组保留，不得授权Guard/预警/攻击 |
| 初始真实World遮挡 | 引入 `Rect2(352,32,8,104)`，实际射线命中确切墙体，不能开始combat |
| 锁后LOS消失 | 原锁已存在；真实墙体/射线正控后，原ID整个90tick无Active、玩家不扣血、直接旧Hitbox连接拒绝 |
| 锁后真实目标死亡 | 保留body/group，真实Health死亡；原ID全窗口无Active/直接连接拒绝 |
| 另一个活player不可续旧锁 | 原目标实际MoveAndCollide离区；即时取消后继续90tick，旧ID从未Active，新ID确实进入Active，旧目标HP9；不禁止新目标合法攻击 |

## 保留的case与严格结果

全部case根：`E:/OurGame/Knight/.evidence/october13-stable-claw/`。每案保留build/engine stdout、stderr、godot.external、native/contract/wait/provenance、source/helper/user/continuation/binary清单及隔离storage。

| case目录 | engine/时长 | 行为/严格结果 |
| --- | --- | --- |
| `resident-heavy-boundaries-20261016-154818-152-c02cacd6ff9c4814b7ef0657a8d3dad3` | 0 / 28507ms | 初13符合，14 teardown/34 owned disposed；stdout4RID，严格FAIL |
| `resident-heavy-boundaries-20261016-155122-652-9ae66ac7da8944d3bae0d3c38cf700bc` | 0 / 29858ms | 加强逐tick/90tick13符合，14 teardown/34 disposed；stdout4RID，严格FAIL |
| `resident-heavy-boundaries-20261016-155307-626-0cea80b51dab4c03a7d2e055a68e2c57` | 1 / 28278ms | 只去掉同World条件，仅foreign断言失败/其余12符合；14 teardown/32 disposed，stdout2RID另行严格失败；不是clean RED或资源修复 |
| `resident-heavy-boundaries-20261016-155520-829-273da340334445b698e4af2f5f87bf50` | 0 / 29968ms | 精确恢复后13符合、14 teardown/34 disposed；stdout4RID，严格FAIL |
| `resident-heavy-20261016-155620-908-da0a1dc4bb91474f86db81bd3d59f79a` | 0 / 21536ms | 原10符合、10 teardown/17 disposed；stdout4RID，严格FAIL |
| `resident-heavy-default-20261016-155708-494-4a7f14a0b2114fc2bac23c4ae2d00122` | 0 / 19844ms | 全旧Heavy model＋Sight runtime＋原BellChain ArmorExposureAndReset真实K测试，严格GREEN/三raw无诊断；不是全BellChain/九类型综合 |

四RID只出现在成功行为专项stdout的退出段，external没有该行也不豁免；反例的foreign异常则保留于stderr/external。每个teardown均native_gone=True、allocated=disposed、borrowed_disposed=0。不同失败路径少建两个本地player shape，不把RID2和4的差异当精确原因。

根代理逐份读最终三案build/三raw/provenance；五组前后清单SHA相同，109保护行保持，Rebuild0警告/0错误、guardFailures空、未超时、55秒cap/headless/无fixedFps/movie/verbose。只对真实用户目录做哈希保护，无存档语义读写。只读协作者另核前三案行为和custody，不替代实际引擎观察。

## 最终关键源身份

| 相对路径 | SHA256 |
| --- | --- |
| `scripts/enemies/laborer/BellBackLaborer.cs` | `571047BC0658F333F9BA595870566D2A1C43D931F64243FFA8CD791D7D6E1777` |
| `scripts/enemies/laborer/BellBackLaborerModel.cs` | `65AAA73514147110995E59A6E9A934A1F5F20F412D0A55377FD626DCD0740FAE` |
| `scripts/enemies/laborer/BellBackLaborerResidence.cs` | `7F8F57C4C65139D7C69AA3571EB735A044A2C74FA2B88BDCF98F78AD3D09AB25` |
| `scripts/enemies/moth/GraveLampMothResidence.cs` | `9F890FAAAF1489D2C279DFC971AD9B974B857D45C151AD067A87C122CFBF5D32` |
| `scripts/enemies/residents/ResidentVoidContact.cs` | `BF7B70B7D52D1C044E9B7551E351836E1E164A4B76BBF015022B2BB12D1CD441` |
| `tests/integration/BellChainRuntimeTests.cs` | `F308B29DC4532EA3F27CFB1647ED7D11F0B8C0842C152EA80DDF7BD1BA138D3A` |
| `tests/integration/ResidentHeavyRuntimeTests.cs` | `899071D2113806441162ACD41422875AE3690B6A558746BB505AF505139A8C51` |
| `tests/RunStableClawPullTests.cs` | `C5E3E03879EB5C43CC8A8E19C90D4EE7B2355056E16293CBB39555B8F059489F` |
| `.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1` | `2DF639CA4CF69B9FB211D0990F1F0F5917A2ED70E64D4F26321318E121F3D116` |

先前报告 `2026-10-16-resident-heavy-and-air-boundaries-native-v001.md` 保留，SHA `2023755A1C0B3A4D146FD386918CBEF0D2FBA5DDABA68C07814087532565BA71`，未覆盖。临时反例已恢复，不留source variant。

## 下一生产入口及未完成项

下一进入实际Turret/ranged opt-in：四共享ranged actor与独立BellBoltSentry都需固定home/完整领地、真实目标权限、整条弹体/beam即时撤权、保HP唤醒与新完整预警身份；保持旧未配置锁后承诺射击、散弹共享尝试/连发独立ID/导弹仅爆炸/激光单尝试/Heavy爪规则。

只读调查已经核对五个保存scene共同胶囊R25/H68/offset(0,-5)，可用home(320,300)、floor顶329、territory(256,224,368,144)、player(560,293)、1024×640实际viewport；导弹56半径枪口内缩合法。该调查不是炮台适配已实现或九个正式炮台已入图；先native fail-first再最小生产适配，不重复Heavy13/空气6/空名册或无区分力资源实验。

Task1最终50完整位置/攻击/返程/玩家接近认证、Task2其他族系/真实Flyer-Diver差异、Task3跨区manager/旧档、Task4正式双路线/呈现60/真人/最终美术/短证据录像均OPEN。上一1667完整pre152048旧Upper lower_to_mid_swing错台＋两profile警告、post152245旧BellChain地板/ILevelRecovery失败不豁免；本轮测试扩展及精确恢复没有重跑它们。没有新Main/Explore/媒体、美术、付费、Git写入/上传、自动化恢复或依赖升级；用户使用其他窗口，本轮不抢焦点/补键，回答仅证明095253测试环境，不倒推更早094130录像原因。
