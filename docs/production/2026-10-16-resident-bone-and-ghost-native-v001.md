# 常驻骨骸弱点／幽灵显隐机制 v001

日期：2026-10-16（Asia/Shanghai）。局部生产进展，完整目标保持ACTIVE；不是第一关交付、完整Task2验收、正式族系美术或新增50只已入图。

## 事实

本轮新增不可变 `ResidentCombatProfile`，仍与正式SceneState／Placement认证分离。所有50条正式Placement仍null，24个正式子型SceneState仍Unimplemented、ScenePath仍null，正式新增0。原26普通＋1Boss、目标追加50＝76普通＋1Boss，以及原生8／废弃12／精工10／骨骸10／幽灵10保持。

### 骨骸重装开发承载

- 仅显式Bone Heavy配置接受 `RearOrExposed`，缺失、空、未知、混入Ghost时间或错误家族profile在配置提交前拒绝。旧未配置Heavy保留原行为。
- 真实J沿当前朝向判正背面；未处于实际Exposed时先拒绝正面，再进入原Armor累计。不是仅调透明度，也没有local hit-point／精确骨骼命中能力。
- 原生正面3次都不连接、HP保持6；真实背面1点攻击前两次HP保持6、第三次HP5，证明被拒正面没有消耗原重甲余数。
- 实际保存机械爪适配器仍只开重装甲、不拉玩家或重装；下一物理步进入原Exposed，正面J造成6→5。原1.20秒窗口结束后再次拒正面，不改Health、护甲除数3或窗口数值。
- 休眠优先于Exposed、J/K权限，唤醒保留已累计背面余数。

这是骨骸Heavy一种开发策略，不是全部4骨骸子型、10骨骸居民完成。方向／姿态策略属于开发初始实现，不冒充用户最终数值／视觉批准。

### 幽灵游荡开发承载

- 复用保存Rust actor，显式Ghost profile启动隐藏→显形有限循环。测试使用0.6秒隐藏／0.8秒显形；不是正式平衡参数或剧情批准。
- J受击资格、K发现资格、最终K布尔事务分别受显隐约束。隐藏时真实J connected0／HP3；显形时connected1／HP2。捕获旧K目标后自然转隐藏，最终投递仍拒绝；不是只移除发现标记就假设旧引用失效。
- 新显式 `IThreadInteractionReceiver.TryReceiveThread` 在当前生命、睡眠、待坠落致死、同World、有效玩家及有效Settings上重查；缺失Settings先原生复现异常，再增加空值拒绝。接受时沿用原LightEnemy把敌人拉向玩家，不把拒绝当作牵引玩家成功。
- 隐藏不移除根身体CollisionLayer／Mask、BodyCollider或地板碰撞；受击Area保留可监测入口，由actor权限拒J。K目标层／两类发现组随显隐撤销，Monitorable延后按当前生命提交。
- 原生J致死后100物理步无自动复活／重新授权；显式checkpoint Reset才由原Health恢复并重新从隐藏期开始。休眠案例观察12步，没有用它冒充完整周期／长休眠时钟冻结证明。
- 视觉只对现有VisualRoot做30%／100%原色alpha；没有新贴图、正式幽灵身份或正常速度视觉验收。

## 先失败，再实现；原始反例保留

所有目录在 `E:/OurGame/Knight/.evidence/october13-stable-claw/`。没有删除、覆盖或豁免失败。

| 阶段 | 原始目录 | 实际结论 |
| --- | --- | --- |
| Bone初始3案 | resident-family-bone-20261016-203738-432-faa3d673f4c043e68bb6ea07cf6ebe5a | 2行为失败：原actor忽略profile，保护正面／闭甲正面可受击 |
| Bone加配置第4案 | resident-family-bone-20261016-203959-079-413e3fcbde19421a8b01d8b940f5d5a6 | 3行为失败，包括错误profile被接受 |
| Bone最小生产门后 | resident-family-bone-20261016-204131-385-d320b4e9203d4b6b962dff389e4485df | 3符合，唤醒后所谓背面失败；尚不能归因生产 |
| Bone原生朝向取证 | resident-family-bone-20261016-204331-547-557db9c6f0b24378be7a690d58c6c5c1 | id743初始Facing−1、碰撞时实际Facing＋1，夹具仍沿−1构造；实际是正面，connected0／HP6 |
| Bone夹具修正 | resident-family-bone-20261016-204504-898-0867f2eff0ed46af8eef3a3db92dbff4 | 唤醒后等1实际物理步，再按实际朝向布置原生攻击；4符合，未修改生产Facing；仍4 Shape RID严格FAIL |
| Ghost初始2案 | resident-family-ghost-20261016-205038-127-f133cf22013d4be8af9a2e7b7b26a8ac | 隐藏K仍报告接受、没有有限显隐，两案真实失败 |
| Ghost生产显隐后 | resident-family-ghost-20261016-205510-411-121de7b243094810b29003e96c5a2a21 | 2符合，仍4 Shape RID严格FAIL |
| Ghost第3案先失败 | resident-family-ghost-20261016-205858-139-f32264834e0940d39b5c7d1ccd2004ca | 前2符合，捕获K缺Settings产生NullReference，第三案失败 |
| Ghost空值门后 | resident-family-ghost-20261016-210226-421-cf3f981abb814166b1d9a02e631b02ff | 3符合，仍4 Shape RID严格FAIL；也是下表最终源码首案 |

Bone早期和Ghost早期反例也有退出资源告警，不能称干净资源RED。原生J均经真实Hitbox AreaEntered→Hurtbox→actor→Health；K经保存适配器／布尔投递，不注入Health、pose或手动推进模型时钟。配置拒绝等正负控不拆成更多独立case充数。

## 最终同一源码的11项回测

规范owner：`.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 -Phase ... -SourceRows 1670`。新增唯一C#源文件 `RustShellGhostResidence.cs`，正常库存guard准确1669→1670；没有减小保护库存、放宽日志或超时。

| phase | case目录 | engine耗时ms | 严格结果 |
| --- | --- | ---: | --- |
| resident-family-ghost | resident-family-ghost-20261016-210226-421-cf3f981abb814166b1d9a02e631b02ff | 6562 | 3行为符合；stdout4 Shape RID，FAIL |
| resident-family-bone | resident-family-bone-20261016-210405-719-0fe88a78c00947a9af156514f4fe1864 | 3566 | 4行为符合；stdout4 Shape RID，FAIL |
| resident-patrol | resident-patrol-20261016-210436-847-10b7ed8a0d854ed4bb5019222a1c10d2 | 44812 | GREEN；原7model＋15native及默认Sight |
| resident-heavy | resident-heavy-20261016-210549-521-905532a3d89a4d9984d584e5f4a3dee4 | 21529 | 原10符合；stdout4 Shape RID，FAIL |
| resident-heavy-boundaries | resident-heavy-boundaries-20261016-210642-234-36739694162e4c059dcccd02b4e7c14d | 30001 | 原13符合；stdout4 Shape RID，FAIL |
| resident-heavy-default | resident-heavy-default-20261016-210738-702-6dd352b239764b2d889eb413dee57575 | 19918 | GREEN；旧完整聚合、configured=false |
| resident-allocation | resident-allocation-20261016-210828-382-014ec42591614b6bb32d8989f6547b6e | 523 | GREEN；名册范围，不是50入图 |
| keyboard | keyboard-20261016-210858-165-b82c9147e753427493b656296e006b61 | 9691 | GREEN |
| actions | actions-20261016-210935-450-5d59a1bd9a2d49838698f3c126852202 | 7086 | GREEN |
| prologue-pre | prologue-pre-20261016-211007-760-f8cb4ee18e384881bf370e12578b6e38 | 27830 | 原生FATAL，exit−1073741795，FAIL，非超时 |
| prologue-post | prologue-post-20261016-211405-473-0f8f3918fb814353b1205f64d5238177 | 8604 | BellChain地板／Recovery解析错误，exit1，FAIL |

最终5严格GREEN／6严格FAIL。新的7机制案例行为符合不等于资源过关或新增7只怪物。

资源证据精度：Bone4清理为native_gone=True，18 allocated＝18 disposed、borrowed_disposed=0；原Heavy10为17／17，原Heavy边界13有14夹具为34／34，均native_gone=True／borrowed0。Ghost只打印12枚不同owned Shape释放记录（5＋4＋3）及3份借用Shape观察，夹具检查actor失效；没有aggregate native_gone／allocated／borrowed-disposed数字回执，不能补写成完整平衡统计。Patrol同样不补造aggregate清理数据。四个资源FAIL只在stdout有退出4 Shape RID；stderr空／external没有镜像退出行，不以engine exit0或STABLE_CLAW_OK抵消。

### 本次完整pre再次原生崩溃，不能沿用旧Upper结论

本次stderr和external均有 `FATAL: Condition "gchandle.is_released()" is true`，位于 `mono_object_disposed_baseref (modules/mono/csharp_script.cpp:1788)`；栈为 `GodotObject.Dispose(bool) → GodotObject.Finalize() → GC.RunFinalizers()`，stderr另有 `Fatal error. 0xC000001D`。最后stdout是BasinSupply重入／HEALED_OBSERVER及AudioDrain记录，无阶段成功／阶段退出drain。

此run在旧Upper检查前退出，旧Upper未执行；不能把上一轮Upper错台／2profile警告抄为本轮pre原因。本次pre/post没有WARNING或RID退出header，不代表历史问题解决。FATAL栈没有给出对象身份，末尾日志只是时间边界，不证明BasinSupply、Ghost profile或某个Shape是根因。

post仍在BellChain输入前WaitForFloor失败，实际位置(540.0704,1196.0139)，KillZone无法解析六祖先 `../../../../../..` 的ILevelRecovery。原失败保留，不改生产地图／玩家数值来迁就旧夹具，不豁免。历史Upper与句柄／资源门仍OPEN。

### 构建及保管链

11次fresh Rebuild均exit0、0警告／0错误、stderr空；既定参数 `build Knight.csproj --no-restore -t:Rebuild -m:1 -p:UseSharedCompilation=false --nologo`，cap180000ms。原生专项cap55000ms、完整pre/post180000ms，全部TimedOut=false。所有headless、非fixedFps／movie／verbose、无ExpectedFailure，human／naturalRoute／wholeGoalAccepted均false。

11份source清单同一SHA256：`179E1AD761923593DB4E6E505C79701EEFD4EC7BB857B5638A46B269289D0CF8`。source1670／真实userdata受保护109／continuation1／binary6；source、user、helpers、fresh-binary、continuation共55对before-after相同。**helper并非全部9：pre为10，额外纳入test-clearance-native-red-admission.ps1；其余10案为9，各自前后相同。** 真实userdata仅哈希、不读存档语义。

root读取构建／原生contract、native、wait、provenance及55对守护；新Bone/Ghost原生stdout全文、pre stderr全文／全部三raw诊断扫描，较长重复轨迹只做有界展示。独立只读审计覆盖11案，小raw全文、pre/post全诊断扫描但轨迹非逐值审查；不声明全trace逐值复核或最终whole-plan审查。所有消费者终止／owned0后才更新本文、README、计划、brief和意见台账。

| 文件 | SHA256 |
| --- | --- |
| scripts/prologue/residents/ResidentPopulationDefinition.cs | 26AFD77FC516E2575574E7A40DF09EE67F40CB5545C6C18ABC8C9523B8B0D5A1 |
| scripts/enemies/laborer/BellBackLaborerResidence.cs | AD847106349A291D95C7173701F761EEB539F7C5DBEF544A1552D626EA657260 |
| scripts/enemies/RustShellSquire.cs | 15BE8A98CBC7283381642EA56E45301D7D13057BAF5321A38255E0F2538D4B98 |
| scripts/enemies/RustShellGhostResidence.cs | DD2834D2ADAED070820F59B56F942C7741746A51115AA85825198C75275BED3A |
| tests/integration/ResidentHeavyRuntimeTests.cs | 0CDA964ABAD53F28BEBEB6198FAF955EBDCADDDE3F9E18BCD50E10D6E84519F6 |
| tests/integration/ResidentPatrolRuntimeTests.cs | D6BDBAFF0EFB7620AA55BE3ED0786C6FA25A4EC1E8C192643768E4A4AF8B792B |
| tests/RunStableClawPullTests.cs | D8ED50CEC6E77B6F3B2F73B01D04E3283E711EE9D5C5DE3D83F0787941404079 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | CE5CB663DB0FF9ED34E1E290B90C9861EFCA03BF261F0C43F54208633787CADD |

## 推理

本轮实装真实J/K的家族机制差异，但这里只是Bone Heavy与Ghost Wanderer两条开发承载。旧模型／巡逻和未配置Heavy回归通过，不等于所有子型、跨区生命状态或美术已完成。显式profile也不能将旧probe场景提升为正式骨骸／幽灵。

新FATAL是当前优先诊断证据，不能因前轮缺席就称历史句柄问题修复。下一应沿原始pre实际调用顺序隔离最小有区分力的生命周期边界，而不是反复跑整关等偶然通过，或按资源计数猜Dispose目标。调查结果未出前不指定匿名对象身份、不施加生命周期补丁。

只读调用顺序核对进一步缩小下轮诊断：`RunPrologueTests.RunPrePhase:147` 是Camera→Clearance→Claw→HealthRestore→BasinSupply→Descent；`BasinSupplyRuntimeTests.Run:27` 是NewGame重入→Title重入→CheckSupply(3,2,true)→CheckSupply(1,1,false)。raw没有BASIN_SUPPLY_RUNTIME_OK。lifecycle检查:141释放旧level后还会Continue、NewGame及Status replacement；最后两次AudioDrain没有root／调用点身份，不能断言第二个CheckSupply是否已开始。

现有RunLifetimeBoundary:57只跑NewGame重入，即使其三prefix相位通过也未覆盖本次较后边界。下一单一对照应新进程隔离**完整原BasinSupplyRuntimeTests.Run**，不跑前缀和后续Descent，保留原资源策略／teardown／自然继续，只在旧root释放、Continue、Status replacement、finally与下一个CheckSupply入口加具名边界记录。能复现则前缀非必需；不复现也不证明已修复。此诊断尚未执行。

资源所有权只作源码事实：CreateSampleFixture:355新实例树由FreeSceneAfterAudioDrain清理；sample Definitions自建Resource／Array但Scene字段来自GD.Load；保存Shape／Profile／纹理为借用，没有显式Dispose；Status replacement:304自建Label与脱树原Label分别随root或finally释放；Send:422自建InputEventAction未显式Dispose。TestSceneTeardown的AudioDrain打印在root.QueueFree之前，不是整树已释放证明。栈没有资源身份，以上均不是根因裁决或授权Dispose借用资源。

## 风险、下一步及完整范围

- 原生FATAL、4 Shape RID与历史Handle/FATAL仍未定位修复；不升级／强制GC／KeepAlive／缓存变更／Dispose借用资源／延长退出等待／日志压制或失败豁免。
- Ghost长休眠／长hitch／真实void-pending显隐组合、完整攻击相位与跨区phase capture/restore未验收。隐藏身体仍能遮挡对远处锚点的LOS；这是独立设计边界，不能清根碰撞层冒充修好。
- 当前名册实际为Bone4子型＋Ghost4子型＝8；这轮只覆盖其中2条开发机制。剩余**6**条是Bone Rust／Skitter／Moth，以及Ghost Moth Flyer／Diver和独立Sentry，不是Ghost三种远程或7条；此前下一步摘要中的该计数不准确，以`tools/build/prologue/CanyonResidentCatalog.cs:32–39`为准。未更改名册、人口或用户规划。24个builder-owned正式场景／profile绑定、ALL50完整身体／活动／攻击／返航／玩家接近与地图扩容及world-lifetime manager仍必做，不削减50或骨骸10／幽灵10。
- ECHO仍1引导身份、独立友方身体／商人0；14区域与5族／6职能分配保持。Task1最终50、完整Task2、Task3–4不打完成勾，不执行task-done／whole-plan最终审查。
- 资源、旧存档、正常Main＋Explore同角色标题→Boss→有支撑出口、实际呈现60FPS、最终美术／声音／真人手感／审阅归档短录像全部OPEN；本轮没有新的正常路线、图稿或录像，不以headless结果交付视听品质。
- 用户确认当时使用其他窗口，这与该次失焦清键一致，不能解释其他历史失败。继续无窗口／不抢键鼠／不自动补键。无新媒体、付费生成、Git写入／版本固定、上传或自动化；旧报告及媒体原件保留。

