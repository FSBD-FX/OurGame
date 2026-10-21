# 炮台真实回调撤权与死亡重置生命周期 v001

2026-10-16，Asia/Shanghai。状态：实际修正两处生命周期问题，覆盖独立Sentry与四个共享远程actor；完整目标仍ACTIVE，**不是资源门、全Task2、50入图或最终交付**。上一报告 `2026-10-16-resident-turret-native-v001.md` 原件保留。

## 事实、推理、风险

事实：真实60Hz激光首发/导弹爆炸回调中的休眠撤权4案符合。两次自然AreaEntered致死之间，Sentry首次实测为Health.Died2但Defeated1；四远程首次实测重置后HP2却StateDead，无法再发射。分别修正旧生命状态写入顺序后，五actor都能经历完整新预警、再次攻击、第二次独立自然死亡/Defeated。六个常驻专项行为符合，但退出Shape RID ERROR使其全部严格FAIL；七项旧兼容/键盘/动作严格GREEN，完整pre/post仍实际失败。

推理：观察到的不是“重置没回血”，也不是失焦：原生Health与hitbox回执证实了真实死亡和显式checkpoint reset，坏值来自旧死亡回调在重置新生命后继续写状态。修正只提前提交旧生命的标记/模型/权限，不改攻击数值、输入、玩家存档或模型时间。

风险：这些是组件原生测试，不是自然Main/Explore、当前画面或性能证明。旧死亡通知重入的其他订阅组合、外力/void/负感知/配置、最终族系与人口manager仍需继续验证。资源身份还没有与退出计数精确关联；不因数量模式猜根因，不Dispose借用资源或放宽严格门。新增50、骨骸10/幽灵10等全范围保留，当前正式新增仍0、50Placement全null、24正式族系场景未实现；本轮没有入图普查或最终场景交付。

## 真正运行的新增9案

沿用独立World2D/1024×640 viewport、保存actor家点(320,300)、完整地板top329、领地Rect(256,224,368,144)、玩家capsule20/72与Health9/无敌0。只设置挂载前初始pose；不手调PhysicsProcess/模型Step、不直接写HP/移动actor造结果。明确checkpoint reset是被测生产接口，不是用sleep回血。

### 激光与导弹4案

- Laser首个真实 `ShotEmitted` 在beam首伤之前同步休眠，HP保持9、无旧Active/授权beam；90tick无迟发。唤醒新ID2，在完整75tick预警后真实伤1/HP8。它证明取消不是“根本没激光”；这4案原生产分支本来正确，没有凭空改战斗实现。
- Missile两接收者为(560,293)/(584,293)，分别独立Health/Hurtbox，第二人不入player组，避免替换锁定目标。真实同次爆心(531.984,292.8837)，radius56；爆炸回调只读native候选包含两精确Hurtbox，不用站位推断代替实测。
- 不取消正控：各伤1、HP8/8；`ExplosionResolved`内休眠：零伤、HP9/9；首次任一 `Health.Damaged`内休眠：恰一伤、HP8/9。实际投递顺序按实例ID，不把左/右假定成先后。
- 同栈退休真实弹体/光束和impact权限，之后90tick无重复伤害/尾发。普通60Hz不证明Burst hitch多个due尾发；该边界仍单列OPEN。

### 自然死亡-reset：Sentry1案＋共享Ranged4案

fixture仅自建64×68、位置(320,295)、Damage2/Knockback0、layerPlayerAttack/maskEnemy的Hitbox。第一次实际发射后，从正常测试续体Begin，真实AreaEntered→Hurtbox→Health→Died，不调用TryConnect/Receive制造死亡。死亡None发布中以HP0且未reset为门，先标记一次再调用ResetForCheckpoint，避免递归None重复reset。Connected返回正常续体后End并等真实帧解除重叠，再完整新预警/实际发射，第二次Begin自然致死且不reset。

Sentry还只读观察两代真实bolt及四枚生产CircleShape：旧/新bolt原生gone，两代自建Shape均失效；保存actor身体Shape保持有效，观察者Dispose=0。共享Ranged检查所有旧散弹siblings/其他真实弹体终结，新生命真实发射，第二次自然死亡撤入/出攻击与目标权限。sleep→wake不能恢复尸体HP或抓点。

## 原始失败、修正与再次实测

证据根 `E:/OurGame/Knight/.evidence/october13-stable-claw`。所有原件保留；准入/编译失败不冒充玩法RED。

| case | 原始观察与边界 |
| --- | --- |
| resident-turret-reentry-20261016-172018-584-25bbc091fb8c40ebb9e993678c96979c；resident-sentry-death-20261016-172022-785-0367a2c2587243e688944388495f6014 | 新input scope写错ImpactRing文件名，build/engine未启动。改为实际RangedWeaponVisual.cs；不是行为RED或资源证据 |
| resident-turret-reentry-20261016-172117-579-a770478bf3f04db3b9607c13c6f9639d；resident-sentry-death-20261016-172148-716-0d63752eeeee405ea55e78d03fd3a12b | Rebuild成功，但runner明确phase白名单漏两名字；engine1/543、506ms，尚未进入新案例。补两明确名字，保留未知参数拒绝，不是生产行为RED |
| resident-turret-reentry-20261016-172340-574-3ba492b99dab4afc8837bce0f91387b6 | 实际4案首次全部符合，engine0/15774ms；4清理/自建18释放，stdout6RID严格FAIL，不改正确生产分支 |
| resident-sentry-death-20261016-172423-859-dc86e71b918647b19f0b7768675841c4 | 实际connections2/deaths2/reset1/HP0，但Defeated1；fresh66tick/两boltgone。engine1/2826ms，预期第二生命断言失败；另stdout3RID，非干净RED |
| resident-sentry-death-20261016-172652-292-0170e397f9a7404f88cb17ec4296187f | 最小修正后两死亡/两Defeated/HP0/66tick，所有后续Shape/尸体权限断言符合；engine0/2914ms，仍3RID严格FAIL |
| resident-sentry-default-20261016-172723-199-4817ee498c934927ac650e05d56f4a3b | 整个旧model/runtime，engine0/10704ms，三raw无诊断，严格GREEN |
| resident-ranged-death-20261016-173037-813-5cee47b468574233a1a678d9fcb56634 | 四角色均firstresetHP2却StateDead/只发1次，4/4在新预警/新发射前失败，尚未执行第二次自然死亡。engine1/15152ms，另12RID严格FAIL |
| resident-ranged-death-20261016-173243-620-4b84d00efa284f5c84aea7cc3c6801f5 | 最小修正后四角色firstresetStateTelegraph、真实二次发射，connections/deaths/defeats各2，最终HP0，预警66/66/78/75tick；engine0/10913ms，4清理/自建16释放。仍12RID严格FAIL |

Sentry根因是 `_defeatedEmitted` 在同步死亡None发布之后才写，Reset清出的新生命标记被旧回调覆盖。只把原有guard check/write移至所有死亡发布之前。

共享Ranged根因是先 `RevokeAttacks` 同步发布None并允许Reset，再把新model Step成吸收Dead、撤新抓点并写新生命flag。只把旧生命guard/Dead模型/目标权限/抓点撤销提交在Revoke发布之前；发布后不再写这些旧状态。没有调整数值/模型时间、重置damageOrdinal或改变正常默认攻击策略；默认综合保持必跑。

## 最终同版验证

Ranged4的173243是最后生产修改后的独立最终专项；随后固定source1669串行运行下面14项。运行期间所有source/helper/docs冻结，所有句柄terminal后才写本报告；无并发engine/build或观察超时后重启。

| case | engine elapsed/cap ms | 严格结果 |
| --- | ---: | --- |
| resident-turret-reentry-20261016-173552-444-7c625a071dd641869904ad745488a139 | 15803/55000 | 4行为/4清理/18自建释放，stdout6RID：FAIL |
| resident-sentry-death-20261016-173634-812-a9fc2c604ea84797a950a3c260ef7afb | 2954/55000 | 1案两生命及资源/权限行为符合，清理4自建释放，stdout3RID：FAIL |
| resident-turret-20261016-173704-461-954658196bb74ee2bf57e0363e5b2bca | 24777/55000 | 原15行为/15清理/25释放，stdout15RID：FAIL |
| resident-turret-sleep-20261016-173800-957-b3a1db18306242edaa466f1b569bd60d | 20701/55000 | 原5行为/5清理/15释放，HP1/新ID/完整预警，stdout15RID：FAIL |
| resident-turret-boundaries-20261016-173854-432-cdf2a8bed8e74d7698138b882eb54a74 | 11877/55000 | 原7行为/7清理/23释放，Sentryfullright624/radius7/生产Shape终结，stdout15RID：FAIL |
| resident-sentry-default-20261016-173933-329-e1c4ab7ef59848d3890cc4d9b3ed9c89 | 10599/55000 | 整个旧model/runtime严格GREEN |
| ranged-models-20261016-174011-845-daf13d5ffbc94aeca76e2196b856952e | 390/55000 | 原model严格GREEN |
| ranged-ledger-20261016-174039-512-3a9b0671535348e19abaab8bb450a112 | 706/55000 | 实际Health/共享receiver/拒绝消耗合同严格GREEN |
| ranged-projectile-20261016-174106-355-14eefc04170e4d0dadefdb66554e11d3 | 1237/55000 | 原生Hurtbox/World扫掠/追踪限制/爆炸一次严格GREEN |
| ranged-actor-saved-20261016-174134-963-9fc796ddfa3b4372b73404e46993c6e4 | 58318/120000 | 四actor/保存14组严格GREEN；原cap120s保持 |
| keyboard-20261016-174301-877-b8ef50da13eb4039b0aaf8a8d3cfd6b0 | 9574/55000 | 原键盘机械爪严格GREEN |
| actions-20261016-174340-354-ed0d724bd4f74c45ac5a327db8d0d381 | 7074/55000 | 原键盘动作/过热严格GREEN |
| prologue-pre-20261016-174416-382-5740fb5806604f919206197233d17244 | 88605/180000 | engine1；旧Upper/lower_to_mid_swing sample0.5，final(264.99634,160.6107)、149/150、landingZoneCrossings0，真实落点断言＋两PresentationProfile WARNING，FAIL |
| prologue-post-20261016-174615-947-4e19f29238d74e22a299544438ea8199 | 8236/180000 | engine1；旧BellChainGate/WaitForFloor在(540.0704,1196.0139)未落稳＋KillZone六层ILevelRecovery解析ERROR，FAIL |

串行14项为7严格GREEN/7严格FAIL；加独立最终Ranged4为15项7GREEN/8FAIL。六个专项合计36行为案符合（本轮新增9＋原27），**不是36个正式怪物或36项交付**。它们的资源ERROR均保留在stdout，external/engine0/成功marker不豁免；pre/post是真实断言失败，不是仅旧warning。旧房间测试失败也不冒充V2地图体验变化。

根代理独立读各build/provenance/三raw并全量扫描诊断，pre巨大轨迹仅限制显示行长、原件未改；只读独立审计确认计数/时长/保管链。所有15案fresh Rebuild0警告0错误、build stderr空、无超时、guards0；source1669共同manifest SHA256 `383A43E3FDEBE79C2098A6324F6A4A47FB7D8F42B0A09B86216ECC7860D29555`。source/helper/user/continuation/freshbinary→after五组逐案相等，真实user保护109行，存储隔离。全部headless/nonfixed/nonmovie，human/naturalRoute/wholeGoalAccepted均false。没有语义读取或改写真实玩家数据。

## 当前9项源码SHA256

| 文件 | SHA256 |
| --- | --- |
| scripts/enemies/residents/ResidentTurretResidence.cs | F48C66DA5D1F51C770E9669410805FFBBB22C2EEF572B95DF46571D18F0BAAEB |
| tests/integration/ResidentTurretRuntimeTests.cs | 835B1A333701E1D501FB2CA5C0107AA269059DBBF2E380F0BF6EF76025FA0990 |
| scripts/enemies/ranged/RangedWeaponEnemy.cs | AB32D85E3EF5AA9B349BCA6F53EEE013E326EDD64C77A8EE1A39448320904CE1 |
| scripts/enemies/ranged/RangedWeaponModel.cs | BF2759A998E92AE1AFC67FC8E579429C490F8C935D32493AE3F04293FBC3EB1A |
| scripts/enemies/sentry/BellBoltSentry.cs | BCFC7BEE0680D68E6443CFE527D24A553D5BA19B3B1DD805F90A7DD50509CD59 |
| scripts/enemies/sentry/BellBoltSentryModel.cs | CA66AEEEA4A9B13F66A9A0EBC3AAC571CECC80217BDF41CB62F16D25AB814924 |
| scripts/enemies/sentry/BellBoltProjectile.cs | 442ED508877C9A0C4B274FDEA3C1234EF61D44DBC3DB695BAB1FF265E5F8FEED |
| tests/RunStableClawPullTests.cs | B857452699A2816019F082090007DF6D1743D89548FED939D7BFDF749F9E8AA9 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | E9E228FA6C8DB05D78C29CBFC734A153D72178DC50C8D966036BA8201458BCB8 |

## 下一实际资源定位入口与其他必做项

只读源码调查发现新fixture用GD.Load/保存Shape借用，Own仅地板/玩家/攻击等自建项；旧Ranged saved suite使用既有CacheMode.Ignore并显式收集/释放资源及PackedScene，二者不是相同所有权合同。旧普通Sentry aggregate没有调用SteamRecoveryKeyboard的出生bolt Shape collector，不能混为一谈。五保存scene各有Body/Hurt/Thread三Shape子资源，但这不能把3/6/12/15计数精确归因给它们。

下一做**纯标量身份诊断**，先从已挂载CollisionObject RID经 `PhysicsServer2D.BodyGetShape/AreaGetShape(...,0).Id` 记录实际保存碰撞来源，并区分fixture自建/已知生产来源；同scene不同fixture是否复用身份、来源集合是否相交才是可证问题。保持原cache/等待/清理/输入/帧数，不额外加载、不GC、不Dispose借用资源、不追加drain。先不读 `Collider.Shape` 或取 `GetCachedRef`，因为首次创建managed wrapper可能扰动寿命；若之后确需Resource ID必须明确该观察风险，只保存标量、不InstanceFromId重取引用。pinned SDK XML已核对上述server getter、IsInstanceIdValid及**Rid.IsValid仅非0，不是销毁后存活证明**。该身份诊断尚未运行，也不能单靠无ID退出总数建立精确映射或宣布根因。

随后继续真实负感知/配置原子性/外力/void/pending、Burst hitch尾发与完整墙预警；Flyer/Diver及五族/24子型真实差异、ALL50最终身体/活动/攻击/返航/玩家接近和人口manager仍必做。Task1最终50、整个Task2/Task3–4、资源、旧save/正常Main+Explore同角色Boss/支撑出口、实际呈现60FPS、真人、最终美术与归档审阅短录像全部OPEN。用户使用其他窗口，继续无窗口不抢键鼠；该次失焦回答不解释更早失败录像。无新媒体/付费/Git写入/发布上传/自动化恢复/安装/删除，目标不缩减、不标完成。
