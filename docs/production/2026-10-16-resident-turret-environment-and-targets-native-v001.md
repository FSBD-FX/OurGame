# 五保存炮台：真实目标边界与环境坠落 v001

2026-10-16，Asia/Shanghai。状态：**组件局部修正与新增原生证据，不是全关完成或正式新增人口验收；完整目标 ACTIVE。** 旧炮台、生命周期、形状身份报告原件均保留。

## 事实、推理、风险

事实：用户再次确认自动探索时正在使用其他窗口。这与该次失焦清键一致；本轮没有可见游戏、OS模拟按键、抢焦点或自动补键。该回答不能解释更早失败录像。

事实：保存散弹、连发、导弹、激光四 actor 与独立 BellBoltSentry 在显式启用驻留配置后，原先没有调用既有 ResidentVoidContact，也没有保存无敌期内的待致死状态。新增真实 native-contact 控制先复现缺口，再接入最小共享状态。最终10个炮台专项的66个行为/清理回执符合，但所有10个专项都有退出 Shape RID ERROR，**严格仍 FAIL**。原有相关七个门严格 GREEN；完整 pre/post 仍 FAIL。

推理：首轮真实接触后致死/待致死断言失败，与源码缺失检测路径相吻合。保留修正仅处理 opt-in 驻留 actor，不改玩家 KillZone、原未配置波次、武器数值、攻击 ordinal 或 Health 无敌期。不能把匿名退出 RID 数量映射成具体资源根因，也不能从单项行为成功推断整关体验。

风险：正式新增仍0，50个 Placement 全 null，24个正式族系子型场景尚未实现；骨骸10/幽灵10仍必须入本关。未完成全部活动包络、跨区 manager、完整双路线/旧档/有支撑出口/实际呈现60FPS/真人/最终美术/审阅短录像。没有固定新版本、Git提交/上传、付费生成或恢复自动化。

## 首轮证据与测试强度

证据根目录为 `E:/OurGame/Knight/.evidence/october13-stable-claw/`，下列历史目录均保留：

- `resident-turret-perception-20261016-182753-398-1c84113d765b44a1afe78cd0a5b5710f`：原10行为符合，engine0/33448ms，stdout5 RID，严格FAIL。
- `resident-turret-target-death-20261016-182918-240-2851b37b569f4337a496034b3ebf406b`：原5行为符合，engine0/19114ms，stdout15 RID，严格FAIL。
- `resident-turret-perception-20261016-183101-044-255b69d551dc45258b259d194da736a4`：仅暂时去掉共享同World条件；恰好五个 foreign 负控失败、五个墙体正控仍符合，engine1/17634ms。另有15 RID，不是纯净资源负控。终态后精确恢复历史 helper SHA `F48C66DA5D1F51C770E9669410805FFBBB22C2EEF572B95DF46571D18F0BAAEB`，之后才开始 void 生产修正；该历史SHA不是当前文件SHA。
- `resident-turret-void-20261016-183702-949-3c6de2a7cb0445cebf2e37ebe98346a9`：GD0002 构建失败，包含嵌套 Godot RecoveryScope 的容器需 partial。引擎未启动，不能称行为负控；只修容器声明并保留 no-successful-binary 守护。
- `resident-turret-void-20261016-184115-761-fadbfbc14cf44c50b1cf50cc9c163032`：五启用源在确认实际几何接触后致死断言失败；五禁用源对照确实穿过同高度、继续掉落且 HP2/两死亡事件0。engine1/10612ms，10清理/owned20。没有打印的启用失败HP数值不补造。
- `resident-turret-void-pending-20261016-184341-551-8d6e6f51b2c1470790ff2ead9c09e74d`：五真实 AreaEntered Damage1/HP2→1 前提符合，均在首次接触待致死/撤权断言失败；后续睡眠/删源/reset步骤尚未执行。engine1/3390ms，5清理/owned15。两首轮 void 均另有15 RID，严格FAIL。

两个目标首轮同 manifest `445E2D919D9167458ABC91FC8D09922FE3BB2F14709C6726321F58B6FD6D9C84`；仅World条件反例为 `91EA57BBEF8F2AA5009611917001EA957DB30FFA9ED93A961240062D1889EE2C`。不能用它们替代下方最终生产版本的重新验证。

## 实际修正与原生夹具

共享 ResidentTurretResidence 增加 BodyPose、VoidPending、真实接触观察与 Health 死亡请求重试。Available 在 pending 期间为 false，撤销目标、受击/抓取、弹体/光束权限；睡眠/唤醒不能清除 pending。只由既有显式 checkpoint reset 清除，Health 发布可能重入 reset 后不再提交旧生命写入。

四远程与独立 Sentry 仅在存在 residence 时：tick 开始重试 pending；真实 MoveAndSlide 前采实际 GlobalTransform，之后用既有 ResidentVoidContact 检查 before/sweep/end 并撤权、请求死亡。不按Y阈值判死，不 teleport、直接写HP、注入Velocity、调用手动模型Step或冻结重力。

夹具 home(320,300)、floor top329、territory Rect(256,224,368,144)，保存身体25半径/68高度及原受击/抓取形状。移除真实 floor 后自然下落穿越真实 KillZone direct Collision Rect(256,400,368,16)，layer0；独立 native Shape before/sweep/end 观察确认实际接触。禁用碰撞源同几何穿越为对照，不调用玩家 recovery。

pending五案仅在挂载前配置正常 Health.InvulnerabilitySeconds=2，通过真实攻击 AreaEntered 将HP2→1。保护期实际触源后 HP1/撤权；sleep、删除源、wake 后仍保留 pending，Y继续变化；Health保护到期请求合法死亡一次。恢复真实floor并调用生产Reset后 HP2/真实IsOnFloor/抓点恢复，再自然触新源产生第二次 Died/Defeated。每案 recovery0。**尚未新增“睡眠中第一次触源”或“void死亡发布内重入Reset”原生边界，不扩大证明。**

perception五组使用独立 foreign World2D 的更近玩家、实际 World 墙与射线；90tick无旧警告/弹体，随后本World新玩家或实际删墙后完整新预警并造成真实HP9→8；foreignHP9。target-death五组用实际 AreaEntered 杀死旧锁定玩家，其尸体故意保留player组，Died回调才让预建replacement加入组；旧ID1的Active始终0，新目标完整预警后实际HP9→8。初始墙体控制不等于已覆盖锁定后新墙撤权。

清理维持原两 ProcessFrame，图中native节点消失后只释放自建 Shape/World；借用保存Shape观察不Dispose。没有新GC/cache模式/KeepAlive/退出drain/日志抑制或延长超时；旧actor-saved的既有resource_cache=ignore仅为旧测试原合同，不是本轮改动。

## 最终同版19个原生consumer

先保留固定 void10/pending5，再串行运行其余17项；**全部17终态后才编辑本报告或任何交接文档**。每次独立 FreshRebuild，所有代码/owner/文档在任意consumer存活期间冻结。

| consumer及原始证据 | 本案实际范围 | engine ms / 原cap ms | 严格判定 |
|---|---|---:|---|
| [resident-turret-void](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-void-20261016-184639-247-0de12e9215474d3fbefee0066a6f7a1c/provenance.json) | 10 native-contact cases; enabled5 fatal / disabled5 alive | 10985 / 55000 | FAIL — stdout15 Shape RID |
| [resident-turret-void-pending](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-void-pending-20261016-184901-485-afde227e7231488184a3f661113a9abf/provenance.json) | 5 protected-contact/source-removal/reset cases | 14532 / 55000 | FAIL — stdout15 Shape RID |
| [resident-turret-perception](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-perception-20261016-185157-106-fc1a6ab158a942f7b2c240aac9fba162/provenance.json) | 10 foreign-World / initial-wall controls | 33422 / 55000 | FAIL — stdout7 Shape RID |
| [resident-turret-target-death](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-target-death-20261016-185257-458-d708f86001354e8497c30e6a9365aad6/provenance.json) | 5 locked-target native deaths / fresh warning | 19247 / 55000 | FAIL — stdout15 Shape RID |
| [resident-turret](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-20261016-185344-482-564abb3cdaf54161a22cb56cfb471364/provenance.json) | 15 fixed-home / gravity / target territory | 24989 / 55000 | FAIL — stdout15 Shape RID |
| [resident-turret-sleep](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-sleep-20261016-185438-102-9c07711b93c94ff98a0f2ed4d5e90a09/provenance.json) | 5 real-tail withdrawal / fresh wake | 20801 / 55000 | FAIL — stdout15 Shape RID |
| [resident-turret-boundaries](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-boundaries-20261016-185526-344-5170fc1b9f1e450f81749515d0252e56/provenance.json) | 7 whole-projectile / beam / owned-shape cases | 11907 / 55000 | FAIL — stdout15 Shape RID |
| [resident-turret-reentry](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-turret-reentry-20261016-185606-643-cf0f3d5335d44ff292772913f28dc9ae/provenance.json) | 4 laser / two-recipient missile callbacks | 15673 / 55000 | FAIL — stdout6 Shape RID |
| [resident-sentry-death](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-sentry-death-20261016-185652-185-c73658ff120e4805be166db0a523ca3c/provenance.json) | 1 Sentry native two-life case | 2906 / 55000 | FAIL — stdout3 Shape RID |
| [resident-ranged-death](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-ranged-death-20261016-185726-171-43befe516c5a446fb96c657063dcc8a4/provenance.json) | 4 ranged native two-life cases | 10879 / 55000 | FAIL — stdout12 Shape RID |
| [resident-sentry-default](E:/OurGame/Knight/.evidence/october13-stable-claw/resident-sentry-default-20261016-185806-672-5e3de0b05c9f4cb1a31729f998e83e80/provenance.json) | full original unconfigured Sentry aggregate | 10717 / 55000 | GREEN |
| [ranged-models](E:/OurGame/Knight/.evidence/october13-stable-claw/ranged-models-20261016-185847-214-5587d33a94e84c7eb8243f7a1203d929/provenance.json) | original models marker | 511 / 55000 | GREEN |
| [ranged-ledger](E:/OurGame/Knight/.evidence/october13-stable-claw/ranged-ledger-20261016-185920-240-2eeb35295bda443e9ef50614dbd21fd9/provenance.json) | actual Health / rejection / shared receiver | 714 / 55000 | GREEN |
| [ranged-projectile](E:/OurGame/Knight/.evidence/october13-stable-claw/ranged-projectile-20261016-185950-068-b8a43bba7091473fba7844d5c3a9df03/provenance.json) | native hurtbox / swept World / bounded guidance | 1135 / 55000 | GREEN |
| [ranged-actor-saved](E:/OurGame/Knight/.evidence/october13-stable-claw/ranged-actor-saved-20261016-190019-602-1667bf28e2814188840390f10f389f29/provenance.json) | 4 saved roles / 14 original groups | 58411 / 120000 | GREEN |
| [keyboard](E:/OurGame/Knight/.evidence/october13-stable-claw/keyboard-20261016-190147-957-af2c8404880c4ed98bdc7a5f3c5b5823/provenance.json) | KEYBOARD_CLAW_RUNTIME_OK | 9586 / 55000 | GREEN |
| [actions](E:/OurGame/Knight/.evidence/october13-stable-claw/actions-20261016-190525-015-508a600531f64d598ef1b1fda1de2375/provenance.json) | KEYBOARD_ACTION_RUNTIME_OK | 7099 / 55000 | GREEN |
| [prologue-pre](E:/OurGame/Knight/.evidence/october13-stable-claw/prologue-pre-20261016-190557-933-8a803ab4e8974554a989fca8cafc1303/provenance.json) | Upper wrong landing + 6 Handle errors + 2 profile warnings | 90555 / 180000 | FAIL |
| [prologue-post](E:/OurGame/Knight/.evidence/october13-stable-claw/prologue-post-20261016-190757-525-d08db77b30464c9ca9462c585240f9c1/provenance.json) | BellChain unsupported landing + unresolved recovery | 8030 / 180000 | FAIL |

计数是测试行为，不是怪物数量：10个炮台专项66行为/66次native_gone=True，owned221/221释放、borrowed_disposed=0；全部66行为符合，但各专项资源门仍FAIL。最终19门合计7 GREEN、12 FAIL。每项stdout/stderr/external原始文件均保留，不能只看STABLE_CLAW_OK、engine0或external干净判GREEN。

fresh pre190557：UpperPendulum/lower_to_mid_swing 在149/150帧落错平台，final(264.99634,160.6107)、landingZoneCrossings0；另有两条 PresentationProfile WARNING，以及**六条 Handle is not initialized**，首条为 GCHandle.FromIntPtr→ScriptManagerBridge.SwapGCHandleForType→PackedScene.Instantiate→DamageSourceIdentityRuntimeTests.cs:24。源码未证实其具体根因，不能归于此次void改动或省略成“仅旧路线问题”。post190757：KillZone 无法解析六祖先 ILevelRecovery，旧 BellChain 等待地板失败于(540.0704,1196.0139)。两完整门均未豁免，也不从本案未出现旧匿名RID推断历史资源/FATAL已修复。

## 保管链与当前源身份

19项共同 source1669，manifest SHA256 `26D053C767425B05AB678CE0075C8F94B00BA198AF434C86BFFB3F34556C72B4`；protected user-data109项，仅哈希、无语义读取/改写。每案 source、helpers、protected user、continuation、fresh-binaries/binaries-after 五对清单SHA均一致；guardFailures0，无Timeout，无expectedFailure/verbose/fixedFPS/movie，human/naturalRoute/wholeGoalAccepted均false。

所有19次独立构建退出0、0警告0错误，原build cap180000ms；focused55秒、saved-ranged120秒、完整pre/post180秒均未放宽。root已读构建/provenance与三raw，完整pre超长trace仅作有界展示和全诊断扫描，原日志未截断/删改。一次人工日志读取路径拼接错误已仅修正只读命令；不是engine错误或门禁豁免。独立只读审计分组复核最终19案，不是whole-plan review。

| 当前文件 | SHA256 |
|---|---|
| scripts/enemies/residents/ResidentTurretResidence.cs | 8D21690A50D80A71676E7FC6DB697F2514B3442313F297DD76A70A7634AEA904 |
| scripts/enemies/ranged/RangedWeaponEnemy.cs | 74BDD7942A7BD2EB35EB61BF5A11BFE917198FB6CF14EB8ED33A7D2B231CA4F4 |
| scripts/enemies/sentry/BellBoltSentry.cs | 501BFFC86D122D35E507B6037CC4C5359A69BF051911F42C82A484C8862DA3A2 |
| tests/integration/ResidentTurretRuntimeTests.cs | DD36A320B536E75A22CDD51577817102E5E480D7AFE31FCEE0BAB6D46596135E |
| tests/RunStableClawPullTests.cs | B81B27CCAA483B8F4C86CED5386314FDE59025009BDDAAD9542AB5E12572FE51 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | 76E0D19815A1EA0228ED449BA7FF4BF0F643E71C9D40CA042328A27E85291DFC |

## 后续入口与验收边界

下一补已批准的配置原子性/真正外力、锁定后LOS撤权与完整墙前预警、Burst hitch、多生命/睡眠首次触源/卸载边界，再完成最终职能与族系差异、ALL50活动包络及正式manager入图。配置兼容的未定义情况先核对合同，不把源码猜测称已复现bug。已回答的资源身份问题不重复无区分力重跑；仍需有因果证据的资源修正，不能用强行释放共享资源或改日志规则换GREEN。

Task1最终50点、wholeTask2、Task3–4与完整目标仍OPEN；不勾选done、不创建/缩减/完成/暂停目标，不做最终whole-plan review。全部正式数量、骨骸/幽灵与视觉要求保持。无新的图稿/录像/美术资产，本报告是本地开发证据，不是发布/用户验收或新游戏版本。

