# 炮台配置、锁定后遮挡与贴墙预警 v001

2026-10-16（Asia/Shanghai）。**本轮是局部预警修正与11个新增原生检查；完整目标ACTIVE，不是新游戏版本或新增50只交付。** 上一环境/目标、形状身份、生命周期报告原件保留。

## 事实、推理、风险

事实：真实Sentry弹体半径7，墙左面x601。修前完整预警右沿608，实际弹体右沿600.9952即退休，躲开的玩家HP9。该偏差由原生断言复现，不是截图猜测。当前opt-in预警改用既有圆形CastMotion裁剪后右沿600.2168，实际弹体仍600.9952，偏差约0.78px，不再进入墙内；不是浮点完全相等。

推理：旧实现先以中心射线取墙表面，再加半径7胶囊，产生多出7px的圆帽。修正只在启用驻留配置的Sentry中，先保留原领地inset，再按真实弹体圆半径裁剪World。没有改弹速、伤害、命中、玩家或旧未配置波次；旧默认ray分支保持。既有ClipBeam自行释放新建临时Circle/query，本轮没有新增私有资源、cache/GC/KeepAlive/额外drain/借用Shape Dispose。

风险：这一个水平墙原生样本不认证全地图/全部方位/最终视觉。旧未配置Sentry预警样式、Scatter外侧圆帽及Missile飞行/爆炸危险带仍需完整可读性审查；Burst多due同栈尾发、睡眠首次触void/void内重入Reset/卸载边界仍未覆盖。配置和锁定后遮挡控制首次即符合，不虚构两处生产修复。

正式新增仍0，50个Placement全null、24正式族系场景未实现，骨骸10/幽灵10必须保留。无新地图、正式出生、manager、美术/媒体、正常Main/Explore、实际呈现60FPS或真人交付，没有付费、Git写入/上传、版本固定或恢复自动化。用户正在用其他窗口，本轮全程headless，不抢键鼠。

## 新增三个原生专项

1. 配置5案：完整身体比领地右界超1px、路点不等于home、四远程arena不相交均拒绝；失败后同actor可合法配置。重复挂载前/挂载后请求拒绝，不污染原领地。保留原home/HP2，真实玩家560/293在原领地内、在被拒较小领地外，经过完整预警后实际HP9→8。没有只比较私有字段或异常文字。
2. 锁定后新墙5案：真实living target已锁定ID1后，新增World墙中心440/285、18×88。实际native ray确认遮挡后90tick旧Active0、HP9、无round/beam/leaves；删墙并实际注销后新ID/完整预警/真实HP9→8。初始墙测试不能替代本组。
3. 完整预警1案：home320/300、territory Rect(256,96,368,400)，living target560/293；墙中心610/285、18×88，左面601在目标之后，不挡索敌。捕获真实radius7胶囊预警，实际弹体发射后让目标用MoveAndCollide向上躲开；真实bolt撞同墙退休，独立literal全圆右沿落在600…601.1，不能用远处territory624替代墙碰撞。没有actor挂载后pose/HP/Velocity/modelStep注入。

第一批三案相同manifest `2419C68B71714FDE004CDD4901B5A2FBABE6AFB13A45530BB0C36D29EA6E2611`，都保留在 `E:/OurGame/Knight/.evidence/october13-stable-claw/`：

- `resident-turret-wall-warning-20261016-192731-482-b3e0c75b532444d3a336bbf6ebc4f484`：engine1/2278ms，预警608与实际600.9952，完整预警断言真实FAIL；另stdout3 RID，不能称纯净资源负控。
- `resident-turret-configuration-20261016-192804-234-081e50b5645642f8a4fa43f823ffa3cb`：5行为符合，9385ms，预警66/66/78/75/66tick、HP8；stdout15 RID严格FAIL。
- `resident-turret-locked-wall-20261016-192839-185-8323f65f127d42a983be5ab3bb0c250e`：5行为符合，19687ms，旧Active0、新预警65/65/77/74/65tick、HP8；stdout15 RID严格FAIL。

三个进程均终态、root读完构建/provenance/三raw后才改生产。首个orchestration JS语法尝试在任何工具动作前失败，修正后重新执行；不是native负控。没有编译失败冒充玩法RED或故意弱化断言。

## 最终同版22门

固定预警一案先完成，再串行运行其余21项；**全部21终态后才改任何文档**。22项共同当前source1669，manifest `D597C67A248903E053C52A878E21009D99B6D9864B42CCEFBB26F81D324955E1`。每项FreshRebuild均exit0、0警告0错误；build原cap180000ms，focused55秒、保存远程120秒、完整pre/post180秒保持。

| consumer与原证据 | 实际范围 | engine ms/cap | 严格结果 |
|---|---|---:|---|
| [resident-turret-wall-warning](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-wall-warning-20261016-193104-538-477f067507554985ac7241bc1067d377/provenance.json) | 1完整圆预警/实际贴墙 | 2163/55000 | FAIL：3 Shape RID |
| [resident-turret-configuration](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-configuration-20261016-193328-699-519f9246ea464e439e39532fbf1e8f8c/provenance.json) | 5配置原子性/真实攻击 | 9499/55000 | FAIL：15 Shape RID |
| [resident-turret-locked-wall](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-locked-wall-20261016-193403-545-455000e2e739469d96c1081f2fb81c68/provenance.json) | 5锁定后新增墙/重预警 | 19659/55000 | FAIL：15 Shape RID |
| [resident-turret-perception](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-perception-20261016-193451-615-86529641d8604c67913c3e979a3cbf9c/provenance.json) | 10跨World/初始墙 | 33405/55000 | FAIL：7 Shape RID |
| [resident-turret-target-death](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-target-death-20261016-193550-866-8a63fdea7fc44d31a0dc4815fe190127/provenance.json) | 5目标自然死亡/替代 | 19110/55000 | FAIL：15 Shape RID |
| [resident-turret](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-20261016-193636-974-4f0300969dbd48eba36fabe3e80e557f/provenance.json) | 15固定home/重力/领地 | 24785/55000 | FAIL：15 Shape RID |
| [resident-turret-sleep](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-sleep-20261016-193729-032-995f4909fd48498fab3567f5528d0ad2/provenance.json) | 5休眠撤弹/保HP | 20599/55000 | FAIL：15 Shape RID |
| [resident-turret-boundaries](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-boundaries-20261016-193817-714-c210efe1e87c4e30af07b6673594ed37/provenance.json) | 7完整攻击边界/ownedShape | 11803/55000 | FAIL：15 Shape RID |
| [resident-turret-reentry](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-reentry-20261016-193857-097-44dd26cc10d64fcba59792a96be46ac6/provenance.json) | 4激光/导弹同栈撤权 | 15800/55000 | FAIL：6 Shape RID |
| [resident-sentry-death](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-sentry-death-20261016-193938-712-ef7a409ac72f486195203f41d41b32a0/provenance.json) | 1Sentry两生命 | 2805/55000 | FAIL：3 Shape RID |
| [resident-ranged-death](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-ranged-death-20261016-194007-389-a9e3e63977e442d7a1c28729c6d4ff73/provenance.json) | 4远程两生命 | 10881/55000 | FAIL：12 Shape RID |
| [resident-turret-void](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-void-20261016-194045-952-b49bedce6b034a46ba046b8a68c1c690/provenance.json) | 10启用/禁用void | 10887/55000 | FAIL：15 Shape RID |
| [resident-turret-void-pending](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-void-pending-20261016-194123-334-d0d0a4f7646c449c8ee9075b287462fe/provenance.json) | 5保护期pending/删源/reset | 14493/55000 | FAIL：15 Shape RID |
| [resident-sentry-default](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-sentry-default-20261016-194206-717-3552bd81a2634ae6a288a9c4cb363d6b/provenance.json) | 旧Sentry完整综合 | 10715/55000 | GREEN |
| [ranged-models](E:/OurGame/Knight/.evidence/october13-stable-claw/ranged-models-20261016-194246-822-a08ffd13097c4d03a85fff2226aa90ac/provenance.json) | 原远程模型 | 519/55000 | GREEN |
| [ranged-ledger](E:/OurGame/Knight/.evidence/october13-stable-claw/ranged-ledger-20261016-194311-813-b8d66a6584f145eca445824d51cd7749/provenance.json) | 真实伤害账本 | 708/55000 | GREEN |
| [ranged-projectile](E:/OurGame/Knight/.evidence/october13-stable-claw/ranged-projectile-20261016-194337-258-f27eba705b164d3091a66ff954a8f6e3/provenance.json) | 真实弹体 | 1266/55000 | GREEN |
| [ranged-actor-saved](E:/OurGame/Knight/.evidence/october13-stable-claw/ranged-actor-saved-20261016-194406-852-074a823f174f4bb7ad3a3807f171c902/provenance.json) | 四保存角色/14组 | 58591/120000 | GREEN |
| [keyboard](E:/OurGame/Knight/.evidence/october13-stable-claw/keyboard-20261016-194533-466-e4334165c31d4482b27e0ad9a0e81b99/provenance.json) | 键盘机械爪 | 9577/55000 | GREEN |
| [actions](E:/OurGame/Knight/.evidence/october13-stable-claw/actions-20261016-194607-882-1e50a64609be45f6aab79a1466874714/provenance.json) | 键盘动作 | 7072/55000 | GREEN |
| [prologue-pre](E:/OurGame/Knight/.evidence/october13-stable-claw/prologue-pre-20261016-194639-434-c794e2aa53a1404a8cf74fdae65e2130/provenance.json) | 旧Upper错台/两profile警告 | 88034/180000 | FAIL |
| [prologue-post](E:/OurGame/Knight/.evidence/october13-stable-claw/prologue-post-20261016-194832-508-8bb4d17ed7b34dca9112e6e6bad85079/provenance.json) | 旧BellChain地板/Recovery | 8111/180000 | FAIL |

13个炮台专项共77行为/77次native_gone=True、owned260/260、borrowed_disposed=0，行为符合；这不是77只怪物。全部13专项有stdout退出资源ERROR，严格FAIL。最终22门为7 GREEN、15 FAIL，不能用STABLE_CLAW_OK、engine0或external干净覆盖stdout。

fresh pre194639：UpperPendulum/lower_to_mid_swing sample0.5落错台，149/150帧、final(264.99634,160.6107)、landingZoneCrossings0；两个PresentationProfile警告也保留。fresh post194832：KillZone无法解析六祖先ILevelRecovery，旧BellChain等待真实地板失败于(540.0704,1196.0139)。本次pre没有重现上一190557的六Handle异常，**缺席不证明历史Handle/FATAL/RID已修复**。完整门仍FAIL，不把其归因于本轮预警修正或当作可豁免旧项。

root检查全部provenance/build/三raw诊断流，完整读取新增专项与主要回归stdout/stderr；部分重复external回执及完整pre/post长trace为有界展示/全诊断扫描，原文件未删改。两名独立只读审计分组核对22案与三初始案，非whole-plan review。每案source、helpers、protected-user、continuation、fresh-binaries/binaries-after五对SHA一致，110/110对；protected109仅哈希，guardFailures0/无Timeout/无expectedFailure或verbose/fixedFPS/movie；human/naturalRoute/wholeGoalAccepted全部false。全部owned consumer已终止后才写本报告。

## 当前源身份

| 文件 | SHA256 |
|---|---|
| scripts/enemies/sentry/BellBoltSentry.cs | 081DD7B37BA51B738D5709DA5DEE478B7AE410EFB0CFF6D36E33FF7AE90D45D1 |
| tests/integration/ResidentTurretRuntimeTests.cs | AC7037BF74A26E297B0D6476DB317E57F458964B553B4FC8ADED53495519E5F7 |
| tests/RunStableClawPullTests.cs | 28C12267AFCA4A6475C33859AA9E0A84C9240414F0922C4EF82293403D9576B1 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | C240A14934C4F7F477C15C8FBF82F44643752CD048B3364BA08576E7B760A06B |
| scripts/enemies/residents/ResidentTurretResidence.cs | 8D21690A50D80A71676E7FC6DB697F2514B3442313F297DD76A70A7634AEA904 |
| scripts/enemies/ranged/RangedWeaponEnemy.cs | 74BDD7942A7BD2EB35EB61BF5A11BFE917198FB6CF14EB8ED33A7D2B231CA4F4 |

后两生产文件本轮未改变。Sentry未配置分支也未改变；不能称全关所有炮台视觉已统一完成。

## 下一实际入口

下一直接实施Flyer/Diver真正分流，不重复已答的77检查/名册/保存RID共享问题。保留Diver既有高位对齐→锁定下冲、旧unconfigured默认；Flyer在目标侧面有界接近、短横/斜侧袭、首接触后物理返航。共用模型的有效攻击速度/距离必须同时驱动预测与真实MoveAndCollide，checkpoint第二生命不能退回Diver。先写同身体/同区域/同目标的原生轨迹反例，再实施；旧14/6案多数显式Diver，其落点合同不能改成侧袭。

随后实现Bone真实姿态/方向弱点及J/K应对、Ghost显形/虚化与爪提前显形。Ghost的J伤害权限和K可发现权限必须分开，sleep/dead/void最高优先撤权；始终保留World碰撞，不穿关墙。现有DamageRequest无局部命中点，不能宣称已有精确骨骼部位检测。24正式scene/runtime profile经builder与原生验证后，再认证全部50活动/返航/玩家接近包络，接world-lifetime manager；不是拿ProbeScene替代骨骸或幽灵。

历史Handle的最小下一判别仍为“原DamageSource.Run”与“仅增加原EncounterDefinition.Run前缀后同DamageSource.Run”双臂，保持GD.Load/清理/GC/cache/等待原样；尚未运行，也没有根因结论。资源门FAIL不能成为停掉所有已批准生产工作的理由，但不得被局部功能成功掩盖。

Task1最终50点、wholeTask2、Task3–4、全关整路/旧档/有支撑出口/实际呈现60FPS/真人/最终美术/审阅归档短录像保持OPEN；完整目标ACTIVE，不缩减、完成、暂停或阻塞。无新媒体资产；这是本地开发证据，不是发布、最终画面或用户验收。

