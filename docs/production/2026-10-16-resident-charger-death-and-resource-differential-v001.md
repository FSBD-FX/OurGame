# 常驻冲锋死亡边界与资源对照 v001

2026-10-16，开发记录。工作目录：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。承接[冲锋与区域归属v001](2026-10-16-resident-charger-and-stream-native-v001.md)，不覆盖旧报告，也不是发行版本或完整关卡验收。

## 事实

用户确认自动探索测试期间正在使用其他窗口，与095253的前台切换、按键清空记录相符。本轮只进行无窗口验证，没有抢焦点、自动补键或重复可见路线；不能用该回答解释更早094130录像失败。

普通伤害死亡确实存在一个具体缺口：已配置常驻Skitter只退出thread组，仍留在grapple组，原生抓点Area的layer32未撤销。现有可用性/Health检查已拒绝选择死敌，但残留Area会阻挡第一命中射线，使更远合法抓点不可见。不能把这个问题误述成“死敌仍能被选中、拉拽”。

本轮通过实际保存Skitter、独立World2D、真实Hurtbox/Health及键盘目标解析器复现。射线位于actorY−32，穿过R25目标圆上部、避开身体矩形，先证明活体Area确实是首个命中，再观察普通死亡后远抓点是否可选。尸体保留在树内，不以QueueFree、休眠补清理或改地形替代死亡边界。

生产修正仅限显式常驻配置路径：普通死亡调用既有撤权逻辑，立即撤两类发现组、抓点碰撞层、受击层和直接出入伤权限；不移除尸体实体身体，也不改变旧未配置波次策略。原生AreaEntered致死还暴露了引擎不允许在in/out信号内同步写Monitorable的错误。死亡和同帧显式checkpoint reset现在统一延后提交该属性，提交时读取当前生命状态，而不是排队保存旧false/true。

新增真实原生链路覆盖死亡→同一Connected回调复活→第二次独立原生致死，最终确认第二生命可受击、每生命一次Defeated、第二死亡最终不可检测。普通死亡的组/layer/直接伤害仍即时检查，Monitorable在正常延后边界检查，没有删除最终属性断言或延长原时间门槛。

最终20项行为断言均符合，原生in/out错误不再出现；退出stdout仍有4个Shape2D RID告警，整个常驻冲锋专项依然严格FAIL。

## 原生证据

证据根目录：`E:/OurGame/Knight/.evidence/october13-stable-claw`。每案保存原始stdout/stderr/external日志、provenance、输入副本、全新Rebuild与前后清单。以下时间为实际引擎耗时，不是制作工时或真人游玩时长。

| 阶段 | 精确目录 | 结果 |
|---|---|---|
| 最终常驻冲锋20项 | resident-charger-20261016-134841-870-b25dd905e3484b72840d92fe02c3a9ef | engine0，42530ms；20行为/20清理回执符合，但stdout退出4Shape RID，严格FAIL |
| 旧未配置冲锋完整模型/运行综合 | resident-charger-default-20261016-134451-092-86bf0d4aa7fa48a9a236f8d28040d4a4 | engine0，38761ms，三日志无诊断，GREEN |
| 当前键盘 | keyboard-20261016-135100-891-847f8d9db8b54481a49e57cac9b1bab6 | engine0，9573ms，三日志无诊断，GREEN |
| 当前动作 | actions-20261016-135135-977-41e0e0a91295438897fc6e1a1ce1af23 | engine0，6970ms，三日志无诊断，GREEN |
| 当前真实流送来源10项 | resident-void-stream-20261016-135208-957-b55bdf3ee2b84a67bf0e76f3fd8a0795 | engine0，1323ms，三日志无诊断，GREEN；不是NPC运动/放置证明 |
| 完整pre | prologue-pre-20261016-135237-516-246224906a4e49d4a82fecc60fc5cf7b | engine1，88365ms，旧UpperPendulum/lower_to_mid_swing sample0.5错台，149/150帧、landingCrossings0；两条刻意profile警告仍保留，FAIL |
| 完整post | prologue-post-20261016-135431-843-076f863a82324fdbacb69db864aa7af3 | engine1，8195ms，旧BellChain地板起点540.0704,1196.0139与KillZone六层ILevelRecovery解析失败，FAIL |

全部实际库存1663、受保护用户数据109项、guards0、无超时；source/helper/user/continuation四组清单及fresh-binaries/binaries-after逐案一致，构建0警告/0错误。当前pre/post没有出现历史句柄/FATAL或退出RID，不等于这些历史问题已修好。几何、名册与Rust巡游/void本轮未重新运行，不把此前证据改称新跑。

先失败和中间错误均保留，未将不干净失败称为干净RED：

- 131450-2c7b9e75：18项只有普通死亡撤权、真实远抓点两项失败，同时退出4RID。
- 132042-845a6103：19项再增加真实AreaEntered致死，恰三项失败，同时退出4RID。
- 132326-34e26465：19行为符合，但同步Monitorable写在原生致死信号内报错，严格FAIL。
- 132643-d41d0a9b：只延后OnDied还不够，Connected内Reset仍同步写而报错；普通死亡旧断言还错误地要求该延后属性即时生效。统一延后所有配置入口，并保留最终属性断言，不豁免原生错误。
- 133348-71953c3e：20行为符合、原生回调错误消失；独立4RID仍在。最终134841又覆盖第二死亡的反向提交。

各短ID对应本证据根目录内同前缀完整目录，完整路径亦记录在人口进度台账。

## 推理：资源对照能说明什么

夹具仅Dispose自己创建的floor/player/void/anchor/attack形状与独立World2D；不Dispose保存场景借用形状或GD.Load返回的缓存场景。增加纯值回执：native节点图已无效、自建形状分配/Dispose次数相等、World2D.Dispose正常返回。必须在清空字段前观察nativeGone，不能用null冒充已销毁。

| 对照 | 精确目录 | 实际结果及限度 |
|---|---|---|
| 原20项，保留managed字段 | resident-charger-20261016-133649-774-0dd1f3f9aa494f70a330976129084963 | engine0/42604ms；20行为和清理回执符合，仍4RID |
| 仅在原清理后释放六字段引用 | resident-charger-20261016-133859-011-2f45abccaf544f52a580b41e6d788d1c | engine0/42563ms；结果仍4RID，未支持这六字段为原因 |
| 同夹具20次未配置生命周期 | resident-charger-fixture-20261016-134143-982-107fc35227ed458687d808755a44a3ba | engine0/24249ms；10真实idle＋10原生攻击、20清理回执符合，仍4RID。opt-in执行不是该症状的必要条件；工作负载并非与配置20项完全相同，不能据此定位具体RID |
| 仅将既有两次ProcessFrame替换为两次PhysicsFrame | resident-charger-fixture-20261016-134652-672-fbcb76ef8551433e8903e2675309e9cb | engine0/24474ms；确实经过2物理/4–5过程边界，仍4RID。未支持该边界假设；已恢复原两次ProcessFrame，没有保留额外等待 |

对照各自guards0、109保护及五组清单一致、严格FAIL。独立旧完整综合GREEN不能替代新夹具严格失败，也不能证明生产游戏资源完全健康。四个RID恰好与某场景形状数相同不是归因证据。两类测试使用相同GD.Load().Instantiate链，未找到不同的显式场景缓存/Dispose策略。

未新增GC、缓存模式、KeepAlive、退出排空、借用资源Dispose、日志抑制或命名失败豁免。保留原55s专项/180s完整回归上限及固定Rebuild配置。停止无区分力重复对照，精确资源来源仍OPEN。

## 风险与下一步

- 只完成开发样段的死亡边界修正，不是全部常驻冲锋、管理器生命周期或全关验收。待致死Actor卸载后的状态继承、同实例离树重挂、组件替换没有由本轮认证；未来manager仍须epoch＋实例＋稳定身份校验。
- 正式新增仍0，全部50正式Placement仍null、24正式族系子型ScenePath仍未实现；保存游戏仍原26普通名额＋Boss。骨骸10、幽灵10及五族/六职能要求不减。41几何活动候选不是41只敌人或容量上限。
- 下一实际生产入口为Moth显式空中适配：无玩家时二维巡航、完整圆身体碰撞/空域、真实目标World2D/存活/领地/视线、锁定俯冲后物理返航；受机械爪/伤害的真实外力不能被Dive速度覆盖。不能以原地Hover或home假玩家替代。
- 空中、重型、炮台及族系差异完成后，必须回到全部50实际活动/攻击/返回包络认证，再接跨区人口管理、正式Main/Explore与旧档/Boss/实体出口。
- 本轮无新正常Main/Explore、可见UI、图稿、录像、真人或呈现60FPS证据，也无新美术、付费、Git写入、上传、自动化恢复、安装、删除或真实用户存档语义读写。完整目标ACTIVE，不缩减历史未验收项；所有拥有的消费进程结束后才更新文档。

## 最终核心源码身份

| 文件 | SHA256 |
|---|---|
| scripts/enemies/skitter/CanyonSkitter.cs | D8294740EAFCE2A72EB872C4E6387E758B713CC1BA510856F56A33102628AAC8 |
| scripts/enemies/skitter/CanyonSkitterResidence.cs | 59AB12C41984F20BDAD04D367C998F907E95AFEDAB123A656A2CD0CC96698344 |
| tests/integration/ResidentChargerRuntimeTests.cs | A25C21A9BB94B2CDB26E9B29212E853E24403B87CE173031E94A564C381E9529 |
| tests/RunStableClawPullTests.cs | 19BC0AC20C82695E59C29207E92618C5B75B266F43970041BC57D5E8971CAB46 |
| scripts/enemies/residents/ResidentVoidContact.cs | BF7B70B7D52D1C044E9B7551E351836E1E164A4B76BBF015022B2BB12D1CD441 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | 9111CE00A8075C094C6DCB7DBEEDC7465CC91400ECA61B347B13276D1DF6BF0A |

实际命令：固定pwsh运行`.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 -Phase <表中phase> -SourceRows 1663`；底层固定dotnet使用`-t:Rebuild --no-restore -m:1 -p:UseSharedCompilation=false`。新增resident-charger-fixture是严格资源诊断对照，不能代替resident-charger或旧完整default综合。
