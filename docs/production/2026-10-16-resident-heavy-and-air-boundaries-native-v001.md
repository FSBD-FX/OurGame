# 常驻重装行为与空中边界 v001

2026-10-16，开发验证记录，不是发行版本、最终美术或完整试玩验收。工作目录：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。承接[共同空中运动v001](2026-10-16-resident-air-motion-native-v001.md)，旧报告和原始失败保留。

## 事实

本轮实际新增保存BellBackLaborer的显式常驻适配器。未配置的旧波次仍走原路径；没有换场景、缩小半径28/高度84的胶囊，或改旧攻速、伤害和重甲参数。新版会真实双向巡逻、检查完整身体的连续脚底支撑，追击合格目标后物理返回home并短暂停稳，再恢复巡逻。没有挂载后写姿态或调用checkpoint reset冒充返程。

重装仍不同于轻型敌人：机械爪经原Heavy adapter开启1.20秒Exposed窗口，不拉动身体；其他状态经原Armor累计÷3，Exposed才直接进Health。新休眠权限位于Armor前，不能悄悄消耗或重置累计余数。真实Health伤害的冲量与重力加入原生运动，不被巡逻指令覆盖。睡眠撤新出入伤、抓取接收/发现和攻击表现，唤醒保留HP并需要新完整预警/更大AttackId。

常驻锁定攻击绑定实际玩家身份/领地/视线；目标出区则撤销旧攻击。**这只适用于opt-in**：旧重装的合法承诺攻击即使随后丢视线仍完成同ID Active→Recovery、真实接触层由World墙挡伤的合同保持。原生致死发布中显式复活后，第二生命仍能原生受击、独立死亡；Monitorable合法延后且读取当前生命，不提交旧生命周期快照。

另补六项真实空中边界：foreignWorld同坐标活玩家拒绝＋同world原生伤害正控；保留身体/组的死目标拒绝；初始World遮挡；完整圆天花板阻挡；锁定后新遮挡撤攻击；锁定后目标死亡撤攻击。没有为已经正确的分支虚构生产修正。故意仅去掉Moth同World校验时，只有对应跨世界断言失败；随后源码精确恢复。

**重装10项和空中边界6项行为断言均符合，但两专项stdout各有4个Shape2D RID退出ERROR，严格结果仍FAIL。** 旧重装完整相关综合、旧飞蛾完整综合、当前键盘/动作严格GREEN。完整pre/post仍旧Upper/BellChain失败，未豁免。不是4RID根因已找到或全关可交付。

## 原生证据

证据根目录：`E:/OurGame/Knight/.evidence/october13-stable-claw`。每案保留原始stdout/stderr/external、provenance、输入副本、全新Rebuild及前后清单。下列耗时是实际引擎运行时间，不是制作工时或真人游玩时长。

| 阶段 | 精确目录 | 结果 |
|---|---|---|
| 新重装10项 | resident-heavy-20261016-151445-026-2508aa23b3a8492f91bf9ef3c28f59b2 | engine0/21539ms，10行为/10自有清理回执符合；stdout4Shape RID，严格FAIL |
| 旧重装相关完整综合 | resident-heavy-default-20261016-151727-777-9fb2a783a8e246a388390250d615c172 | engine0/19902ms，三日志无诊断，GREEN |
| 空中真实边界6项 | resident-air-boundaries-20261016-151821-590-15c267fc041e4c9898b75fc2f1968886 | engine0/14211ms，6行为/7夹具清理回执符合（含独立foreignWorld）；stdout4Shape RID，严格FAIL |
| 旧飞蛾完整模型/运行综合 | resident-air-default-20261016-151901-449-55c28308ea9f4e908fe70b39800a43be | engine0/5067ms，三日志无诊断，GREEN |
| 当前键盘 | keyboard-20261016-151935-012-05c63c12ac0440f38a9387aa957b61fd | engine0/9586ms，三日志无诊断，GREEN |
| 当前动作 | actions-20261016-152011-346-8a80fbd1e7144fe49c63ae877bc8d944 | engine0/7077ms，三日志无诊断，GREEN |
| 完整pre | prologue-pre-20261016-152048-919-97a479bb3e88463b93718b1f176bfd34 | engine1/88550ms；旧UpperPendulum/lower_to_mid_swing sample0.5错台，149/150、landingCrossings0、final264.99634,160.6107；两条刻意profile警告保留，FAIL |
| 完整post | prologue-post-20261016-152245-798-9bf52fe58fa8461ba0d8d17774ee58ed | engine1/7909ms；旧BellChain地板540.0704,1196.0139及KillZone六层ILevelRecovery解析失败，FAIL |

最终八案源码1667、真实用户保护109项、guards0、无超时；source/helper/user/continuation四组前后清单及fresh-binaries/binaries-after逐案一致，Rebuild均0警告/0错误。三个raw分别检查，0字节stderr显式处理，不因external安静忽略stdout错误。pre/post此次未见历史句柄/FATAL/RID，不等于历史问题修好。真实stream、几何/名册、Rust/Charger及原空中14项本轮没有重新运行，不改称1667新验收。

### 重装10项覆盖

| 原生检查 | 实际证据 |
|---|---|
| 未配置Idle | 无玩家60帧原地Idle，未擅自常驻化旧波次 |
| 双向巡逻 | 360帧实际x257.59912至382.40073，最大步长0.80007946，原84高胶囊/重力/真实地板保持 |
| 80px窄台完整支撑 | 原56宽身体在真实平台内x310.40015至329.59985往返，最大步长0.7999878，不走到边缘坠落 |
| 32px仅中心支撑 | 身体真实IsOnFloor但不足全宽，90帧不授权自主横移，不能用中心触地当安全证书 |
| 追击→出区→返程 | 真实Approach先离home到x>334；仅移动玩家出区；原生回home±3且X速度停稳后才恢复巡逻，保HP6 |
| 锁定出区撤权 | 实际锁定Telegraph后玩家出区，该ID无Active/伤害，空攻击发布、直接TryConnect拒绝 |
| Armor余数与Heavy爪 | 两次1伤仍HP6；睡眠第三次拒绝；唤醒合法第三次恰HP5；真实Legacy adapter一次开甲且身体不被拉；Exposed命中到HP4，原1.20秒后关甲，无回血 |
| Active睡眠/唤醒 | 真凸扇形Active中立即拒绝直接出入伤/Heavy爪，撤两发现组/目标层/表现；醒来HP6、新ID且完整预警剩余>.70秒 |
| 实际伤害冲量 | 真实Armor将3伤归1进Health/HP5，600冲量使前三帧实际X位移>15，再连续回home，最大步长<11且不回血 |
| 原生两生命 | 真实AreaEntered/Armor/Health致死，Dead FrameChanged内显式reset一次；第二次独立原生致死、两次Defeated，最终不可检测/抓取，睡醒不能复活尸体 |

旧重装综合明确执行整个BellBackLaborerModelTests、整个BellBackLaborerSightRuntimeTests，以及原BellChainRuntimeTests.LaborerArmorExposureAndResetUseRealScene。新public测试转发入口只调用原方法，未删原断言；并非整个BellChain机关套件或全部9旧敌种都通过。其左右方向真实凸形重叠、墙阻伤/无墙受伤及旧实际玩家机械爪/Armor/reset合同保留。

### 空中边界与反例

天花板为真实World `Rect2(256,80,128,4)`，底84＋实际R16要求中心≥100；航点中心96自身在屋顶下方，但完整圆会侵入。实测最小Y100.2816、最大Y159.11057，仍可沿另一合法航段巡航，最大步长0.80001056。初始及锁定后LOS墙均为`Rect2(256,164,128,8)`，先用实际射线证明命中确切新增body，之后才判断攻击撤权；不以“墙挡住伤害”冒充未进入Active。

- 145758-97c22085（1665，engine0/14350ms）：原生产6行为符合，但4RID严格FAIL。
- 145947-00b5db71（1665，engine1/13593ms）：只去掉AirTargetAllowed中的同World2D校验，foreign-only玩家真实世界/存活/组前提通过后，对应授权断言准确失败，其他5符合。仍4RID，不称干净RED或生产自然bug。
- 150105-15df3a88（1665，engine0/14211ms）：精确恢复Moth residence SHA后6行为符合，仍4RID；最终1667的151821再次符合，不留任何故意变体。
- Heavy150855-4640d12f（1666，engine1/1906ms）：原默认Idle符合，9配置案在缺适配器处停止，不能称9个运动反例；同时4RID严格FAIL。旧相关综合150958-b7579e0a先严格GREEN/19782ms，随后才新增实际适配器到1667。

夹具逐案finally释放自己的native图，经原两次ProcessFrame再Dispose自建形状/World2D，清字段前观察nativeGone，不Dispose保存场景借用资源。重装10案17自建形状全部Dispose，空中6案含第二world的7份清理均符合；回执不能据此定位4RID。没有新增GC、缓存模式、KeepAlive、退出排空、日志抑制、命名失败豁免或时间上限扩张。

## 推理与风险

这次改变了实际角色行为，而非新增空目录或名额。先保留原生缺适配器失败、旧完整相关综合，再实现最小opt-in；Moth新增边界原本正确，故仅增加有反例证明的验证，不虚构修复。重装的巡逻支撑和目标规则只约束自主意图，已接受伤害冲量和重力在后合成，不以夹回矩形或reset解决返程。

但10案没有原生证明Heavy的实际void/无敌待致死跨休眠与卸载、foreignWorld/死目标/新LOS、垂直伤害冲量、休眠保留已有冲量及非法/挂载后配置。源码中的分支不是完成证据。其600水平受力案也没有证明越出完整领地后的一般返航；不同高度/复杂障碍还需地形与回返路径认证。Moth的6项不能替代Heavy这些边界。

正式新增仍0、50正式Placement仍null、24正式族系场景仍未实现。保存游戏仍原26普通名额＋Boss，骨骸10/幽灵10及五族/六职能分配不减；现有Heavy/Moth样段不是正式家族资产或全50验收。此前41活动候选/9未解是1663阶段，不改称本轮新普查。

## 下一步与剩余门

先补Heavy真实环境致死/外力/目标/配置边界，再继续炮台/远程常驻和骨骸/幽灵等族系差异；Moth Flyer/Diver仍共用行为，最终职能差异不能遗漏。实际身体/攻击/返程包络完成后，回到全部50正式地形/玩家接近/抓点/机关安全认证，再接跨区人口管理和新run/旧档/死亡恢复。

完整目标ACTIVE；严格资源/全回归、正式新版Main/Explore同角色Boss/实体出口/保存、呈现60FPS、最终美术/感官、真人反馈及归档审阅短录像仍OPEN。本轮因用户操作其他窗口继续无窗口，不抢焦点、不自动补键，不以失焦回答归因更早录像失败。

没有新可见游戏/UI、图稿、录像、付费/美术生成、Git写入、上传、自动化恢复、安装、删除或真实用户存档语义读写。原始证据在项目`.evidence`，此文为开发记录，不是过程美术。全部拥有的消费进程终止后才更新文档。

## 最终核心源码身份

| 文件 | SHA256 |
|---|---|
| scripts/enemies/laborer/BellBackLaborer.cs | 571047BC0658F333F9BA595870566D2A1C43D931F64243FFA8CD791D7D6E1777 |
| scripts/enemies/laborer/BellBackLaborerModel.cs | 65AAA73514147110995E59A6E9A934A1F5F20F412D0A55377FD626DCD0740FAE |
| scripts/enemies/laborer/BellBackLaborerResidence.cs | 7F8F57C4C65139D7C69AA3571EB735A044A2C74FA2B88BDCF98F78AD3D09AB25 |
| tests/integration/ResidentHeavyRuntimeTests.cs | 7FD95EC3095020DAF13A10665C0B8E9D994E039EFA7D3AFAAA43AC2DEA2A2079 |
| tests/integration/ResidentAirRuntimeTests.cs | 2F86B43010983937EBAD08B0AE337E2E81236330255851F73EC24EB35777A774 |
| tests/RunStableClawPullTests.cs | E11491F34C004157D3436E8EA4200B705E342FDEA66A94C829F5F34607B60B4D |
| tests/integration/BellChainRuntimeTests.cs | F308B29DC4532EA3F27CFB1647ED7D11F0B8C0842C152EA80DDF7BD1BA138D3A |
| scripts/enemies/residents/ResidentVoidContact.cs | BF7B70B7D52D1C044E9B7551E351836E1E164A4B76BBF015022B2BB12D1CD441 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | B5A5408476CB9DE4F6959268E94EADBE1DCF3923D6021772D1E326A6A02F0F5D |

Moth生产residence仍精确为`9F890FAAAF1489D2C279DFC971AD9B974B857D45C151AD067A87C122CFBF5D32`，故意移除World条件已完全恢复。实际命令：固定pwsh执行`.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 -Phase <表中phase> -SourceRows 1667`；固定dotnet `-t:Rebuild --no-restore -m:1 -p:UseSharedCompilation=false`，focused55s/pre-post180s及三日志严格否决保持。
