# 常驻炮台：真实撤权、完整弹道边界与生产资源所有权 v001

2026-10-16，Asia/Shanghai。状态：实际组件实现与原生验证进展，**不是全 Task2、50 常驻入图或第一关交付**。

## 判断：事实、推理、风险

事实：四种已保存远程 actor 与独立 BellBoltSentry 现在接受显式常驻配置。未配置时保留原策略。最终 foundation15、sleep5、boundary7 的行为断言与各自节点/自建资源清理回执符合，但三个专项各有 stdout 退出 **15 Shape RID ERROR**，严格 FAIL。不能用 engine exit0、external 没收录该行或 `STABLE_CLAW_OK` 覆盖严格失败。

推理：先前原生反例分别证明 Sentry 完整弹体越过领地、预警不对应实际半径，以及两枚动态生产 Shape 没有随原生 bolt 终结。分两步修复后，前两项先转为符合，随后两枚 Shape 的只读观察也转为已释放。它支持这三个具体修复，不支持把剩余15RID的来源归给某类资源或宣称全局释放问题解决。

风险：当前仍是五个旧 actor 的显式开发适配，不是九个正式炮台身份、24正式族系场景或新增50的运行人口。骨骸10、幽灵10等要求不减。高密度全图性能、真正自然路线上手/战斗观感、同栈重入、全部配置/感知/外力/死亡边界和跨区管理仍需证明。用户确认当时使用其他窗口，与该次失焦清键一致，但不能据此解释更早失败录像。

## 实际生产更改

- 新 `ResidentTurretResidence` 仅管理权限：完整胶囊固定家点/领地、同 World2D 活玩家、Health、实际 World 射线与枪口 viewport、休眠/当前生命权限。一次性树外挂载前配置；不写 actor 世界位置/HP/Velocity、不替换模型、不关闭重力或物理处理。
- `RangedWeaponEnemy` 与 `BellBoltSentry` 显式接入 `IResidentEnemyAgent`/伤害接收；休眠同步撤警告、真实飞行弹体/光束、受击、抓取组/层、旧 Sentry Hitbox 直接投递权限。保留已受伤 HP；唤醒使用更大的攻击 ID 并经历完整新预警。
- 两个模型增加保留 nextID/原 Stunned 时间的取消路径；唤醒不 new model。共享远程仍使用原 volley ledger / damage ordinal / 原生运动与弹体。
- 仅已配置 Sentry：实际圆半径7的弹体按完整圆内缩领地裁剪真实 `MoveAndCollide` 位移并退休，预警同半径/同边界。未配置的默认 MaximumTravel 与预警半径2策略不改。
- `BellBoltProjectile.CreateOwnedCollision` 明确创建并持有自己的两枚动态 CircleShape2D；原生 `_ExitTree` 只释放这两项，幂等且先清字段。premount 异常也归还自己已创建项；外部构建 bolt 的该字段为 null，不扫描/接管/释放借用或保存 Shape。此生产所有权修复覆盖默认与常驻生产，但不改默认战斗策略。

没有新增地图、族系美术、正式出生或人口 manager。没有 GC/cache-mode/KeepAlive/额外 exit-drain、借用 Dispose、日志屏蔽或门禁豁免。

## 原生夹具与证明边界

五个真实保存场景：RivetScatterGunner、BeltBurstGunner、FurnaceMissileGunner、PrismLaserGunner、BellBoltSentry。各自 radius25/height68/body offset(0,-5)，Health2，Muzzle(0,-8)。独立 World2D / SubViewport1024×640 / 实体地板 top329 / 初始家点(320,300)。默认领地 Rect(256,224,368,144)，完整边界 right624；玩家 capsule20/72、初始(560,293)、Health9/无敌0。只在挂载前设初始 pose，后续 actor 运动由自己真实物理流程所有；玩家离开/躲避使用真实 MoveAndCollide。

foundation15：每种默认无目标固定落地、显式单家点固定落地、锁定后玩家中心仍在但完整身体出领地，继续90原生tick不得授权旧攻击。

sleep5：真实 Hurtbox 先把敌HP2→1；四弹体角色等待 owner Recovery 且实际弹体仍存在，Laser 等实际 Active。休眠同步退休捕获弹体、撤全部授权 leaves/预警，旧 Sentry Hitbox 直接 TryConnect 也拒绝；90tick无后续攻击/伤害/再眩晕，仍HP1。唤醒实际预警 tick 为66/66/78/75/66，oldID1→freshID2，然后真实 Active/飞行。Sentry 不存在 ShotEmitted 计数，测试逐tick核对实际 LiveRounds，不能借恒为0的计数称未发射。

boundary7：领地加高 Rect(256,96,368,400)，真实玩家在发射后上移躲开旧锁线但完整身体仍在领地。实际错过目标的散弹/连发/完整导弹爆炸/Sentry圆分别按 inset6/5/56/7检查；最终最右完整x为624/624/618.48047/624并退休。真实 width5 laser 一次伤害内侧玩家9→8，外侧660接收者仍9，完整 capsule≤624。另验 Sentry 预警实际radius7/right624，及旧生产bolt原生删除后两枚生产Shape失效；观察者 Dispose=0。

夹具仅释放自己显式 new 的地板/玩家 Shape 和 World，原有两次 ProcessFrame 清理边界保持。保存场景 Shape、生产动态弹体 Shape 仅借用观察，不能被夹具 Dispose。上述不是自然键盘通关、实时呈现帧率或最终视觉证明。

## 保留的失败与因果顺序

原始证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。下面时间标识对应完整 case 目录，旧证据不覆盖。

| case | 观察 | 严格结果 |
| --- | --- | --- |
| resident-turret-20261016-161524-153-ebd118df278c467d983a5de2c5fadec8 | source1668；默认5符合，配置10在缺适配器处失败，尚未运动/锁定 | FAIL，另有15RID；非干净行为RED |
| resident-turret-sleep-20261016-161906-913-71d36a0e0e444a86b4b328725e4645b9 | source1668；5个在缺适配器处失败，尚未HP/休眠行为 | FAIL，另有15RID；非干净行为RED |
| resident-sentry-default-20261016-162148-689-703ad194b88b4a8a84e9c50663b5ad89 | 旧完整model/runtime，engine0/10599ms | 严格GREEN |
| resident-turret-20261016-162622-744-d9c7229edac34e0d8048f18b1c831cab | 首个producer；15行为符合、15清理回执/25自建Shape释放 | FAIL，15RID |
| resident-turret-sleep-20261016-162755-852-7e44dc96f5fe4dbc902a9b6a6ef83ded | 5行为符合、5清理回执/15自建Shape释放，真实新预警 | FAIL，19RID |
| resident-turret-boundaries-20261016-163524-116-73b30882762043b7aa9e6ef9c614654c | CS0136新测试局部变量重名；没有启动引擎、无成功fresh binary | 构建FAIL，不是原生行为RED；只更名beamRight |
| resident-turret-boundaries-20261016-163757-392-9288bec05533499a89321b5546620f23 | 3实际失败：Sentry fullx627.6641未退休、预警radius2/right961.9944、boltGone但两枚生产Shape仍有效；另4边界符合 | FAIL，另19RID |
| resident-turret-boundaries-20261016-164106-071-09e2ec6b3e57447eb7cffc3900c080cc | 先只修完整边界/预警，两项符合；仅Shape终结仍失败 | FAIL，仍19RID |
| resident-turret-boundaries-20261016-164351-785-8c971afab2a04669a166579987c2c321 | 再修明确生产所有权；7行为/7清理/23自建释放；boltGone与两ShapeDisposed均True；engine0/11886ms | FAIL，剩15RID；不认定其精确来源 |

原 default Sentry 聚合没有调用旧 Keyboard Shape collector，不能把其GREEN归因于那个 collector。后者若将来涉及必须另查是否掩盖生产所有权，不能在此追认。

## 最终同版串行回归

最后生产修改后固定 source1669，所有消费进程严格串行、使用同一返回句柄等到 terminal，整个消费窗口无源码/helper/文档编辑。以下11项全部终止后才更新本报告。

| case | engine elapsed/cap ms | 结果与边界 |
| --- | ---: | --- |
| resident-turret-20261016-164725-470-b946a2b40b70420da21136b9c2df8a0e | 24911/55000 | 15/15行为＋15清理/25释放；stdout15RID，严格FAIL |
| resident-turret-sleep-20261016-164818-840-4dfc81c2c3464620a8855200726cdb1e | 20687/55000 | 5/5行为＋5清理/15释放；完整新预警/ID/HP符合；stdout15RID，严格FAIL |
| resident-sentry-default-20261016-164910-474-fd2efafc0586418ea941fecc1f05f84b | 10611/55000 | 原完整model与保存actor runtime，严格GREEN |
| ranged-models-20261016-164949-458-72a01a7935c542e0b055cd0ea4d65907 | 416/55000 | 原完整model，严格GREEN |
| ranged-ledger-20261016-165015-819-337868569f0f4394adfe9a32bdb1fea9 | 683/55000 | actualHealth/rejectionConsumed/sharedReceiver，严格GREEN |
| ranged-projectile-20261016-165358-825-8c3c3753b2e941ccbba525a35405e8c2 | 1255/55000 | 原生Hurtbox/sweptWorld/guidance/explosionOnce，严格GREEN |
| ranged-actor-saved-20261016-165429-575-7779d1e71745482193a0a2fb3d48a4c1 | 58395/120000 | 四角色真实actor及保存场景14组，严格GREEN；自身原cap120s，不是55s |
| keyboard-20261016-165558-833-bca1aa1e9d9042049d51fa48058ed001 | 9586/55000 | 原键盘机械爪runtime，严格GREEN |
| actions-20261016-165638-899-f385c86e09bd473db82839c66aea6b7c | 7210/55000 | 原键盘动作/过热runtime，严格GREEN |
| prologue-pre-20261016-165722-225-1a2f86e41fc74fb781a39c8d32aafe3a | 87922/180000 | engine1；旧Upper/lower_to_mid_swing sample0.5错台149/150，final(264.99634,160.6107)，landingCrossings0；两刻意无效profile WARNING也保留；严格FAIL |
| prologue-post-20261016-165916-373-47339ace2c0e4af183534d8446c82e4d | 8515/180000 | engine1；旧BellChainGate/WaitForFloor在(540.0704,1196.0139)未落稳＋KillZone六层路径无法解析ILevelRecovery；严格FAIL |

最终11项为7严格GREEN/4严格FAIL；另外前面的最终boundary7也仍严格FAIL。pre/post不是“仅旧warning”，包含真实断言/ERROR，未改检查或豁免；旧房间失败也不冒充当前V2主路的新地图提升。

根代理读各provenance/build/三raw并扫描全部诊断，巨大pre轨迹仅限制显示行长、未编辑原件，不声称将巨行全文展示。独立只读审计同样区分行为与严格结果。7绿色项三raw无诊断；pre/post本次未见RID/句柄/FATAL不等于历史根因闭合。

最终boundary加11回归每案五组保管链（source、helper、真实user散列、continuation、freshbinary→after）逐项相等；source1669，共同source manifest SHA256 `3274489734BDCCFCDFFBC013B54CBA20EABB49E666C0B909E77D70B008B0B389`；真实user保护109行。全部guard0、无超时，fresh Rebuild0警告0错误、build stderr空。全部headless/nonmovie/nonfixed、隔离存储，human/naturalRoute/wholeGoalAccepted均false。没有语义读取/改写真实玩家存档。

## 当前关键源码SHA256

| 文件 | SHA256 |
| --- | --- |
| scripts/enemies/residents/ResidentTurretResidence.cs | F48C66DA5D1F51C770E9669410805FFBBB22C2EEF572B95DF46571D18F0BAAEB |
| tests/integration/ResidentTurretRuntimeTests.cs | 951C2DFB90E9607B1725D9AB100B08A1FF26CDF0E59DB7348340B1D55646D12E |
| scripts/enemies/ranged/RangedWeaponEnemy.cs | 4406B5A9E84089E886B7A800FEFD4EFA806EDBADF2FBEB33994125CC931CE357 |
| scripts/enemies/ranged/RangedWeaponModel.cs | BF2759A998E92AE1AFC67FC8E579429C490F8C935D32493AE3F04293FBC3EB1A |
| scripts/enemies/sentry/BellBoltSentry.cs | 6A500A2E7D8AAA28DBBB56863F8B8EBB0981932B6041D1212340819FF55C23A0 |
| scripts/enemies/sentry/BellBoltSentryModel.cs | CA66AEEEA4A9B13F66A9A0EBC3AAC571CECC80217BDF41CB62F16D25AB814924 |
| scripts/enemies/sentry/BellBoltProjectile.cs | 442ED508877C9A0C4B274FDEA3C1234EF61D44DBC3DB695BAB1FF265E5F8FEED |
| tests/RunStableClawPullTests.cs | 4DD4F5AF69BC3CE88A47AC83439AC4612FB86F58CE6DC9A612FFE7E6C8023B8E |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | 6BE54E4C5847EC42C31E483EB218703750531AF561E604B934076C2D794C1878 |

## 下一实际入口，不重复空名册或无区分力资源试跑

1. 真实60Hz首个 Laser ShotEmitted 回调内休眠，证明同栈 beam 建立后、首伤前撤权；唤醒真实首伤作正控。普通 AttackFrameChanged 在伤害后，不能替代这个触发点。
2. 真实 Missile ExplosionResolved 内休眠，以及两名真实接收者中首个 Health.Damaged 内休眠；先不取消正控证明实际爆炸候选有两人。分别应零伤与恰一伤，不能靠漏撞通过。真实60Hz不能冒充Burst hitch多个due尾发，后者仍另列OPEN。
3. Sentry fixture-owned真实PlayerAttack/自然AreaEntered致死，死亡None发布中显式checkpoint reset；观测第二生命、新bolt与旧creator Shape终结、第二次独立死亡。不是直接写HP或用reset冒充sleep回血。
4. 再补负感知/配置原子性、真实外力/void/pending、最终职能和族系差异；所有50的最终活动包络/返航/攻击/玩家接近和跨区manager保持必做，不以旧基础actor名称替代骨骸或幽灵。

保持55s专项/180s完整回归、三raw与全部守护。Task1最终50、整个Task2、Task3–4、资源、Main+Explore正常同角色整路/旧档/支撑出口、实际呈现60FPS、真人、最终美术及审阅短录像全部OPEN，完整goal ACTIVE。无新媒体、付费生成、发布/上传、Git写入、自动化恢复、安装/删除；继续无窗口，不抢正在使用的桌面。
