# 固定联合50输入与同场真实巡逻 · v056

2026-10-22，工程 `G:/knight/Knight`；完整1–11／Tasks1–4保持ACTIVE。这是内部人口接线与一次幽灵巡逻修正，不是正式试玩、最终美术或发行交付。最终同冻23相回归已全部结束：13PASS／10严格FAIL，以下按实际案例区分，历史失败不被当前安静案例覆盖。

## 事实：用户批准的侧壁入口

独立复核当前生成器／测试与v055的 `resident-gear-left-access-20261022-083809-736-bc0bc246a8154744b40ebecd07d88a44` 副本一致：真实墙 `(2280,3700,24,116)`，原营地→旧深井→接落台→D/K抓西壁→A+Space后D借力越角→反向制动→完整左棚→原活塞出口。289观察／580physics，2casts／2Arrived／1Cancel／1Boost，普通跳0、dash0、治疗0、HP5。完整40×72和潜在危险域未改。

当前v056同源复测 `resident-gear-left-access-20261022-100038-872-bd6520a5c4a94d59b8f88db77a690b00` 严格PASS：289观察／579physics、2casts／2Arrived／1Cancel／1Boost，HP5。580是v055历史值，不混作当前计数。

这是停机关、无常驻敌人占用的自动键盘样段证据；不是任意站位容错、真人手感、正常战斗世界或正式地图已发布。Frozen回营前缀是另一个Player leg，不能拼成同身份全程。

## 事实：只导出真正的最终联合选择

新增测试工具 `CanyonResidentPlacementTests.Joint.cs`。保留原搜索预算、全部50／24、每ID原生复核、跨区Grow24间距及原地图不变断言，只在最终选中全部50后按名册固定顺序输出：

- `candidate-final-joint.json`：数值Home／原Territory／全部有序Points／完整Occupancy，不是accepted桶或仅min/max。
- `candidate-final-joint-context.json`：实际owner source/helper清单、五个保存sector、全部24leaf＋24profile及实际environment／旧26的36主备占位记录之SHA／数值。没有native实例ID。
- `candidate-final-joint-receipt.json`：独立可信payload/context SHA；明确test-only、正式新增0、完整运动未认证。
- `candidate-joint-native-patrol.json`：实际物理观察统计，不是游戏存档或媒体。

读取先验证外部可信SHA／context、schema、精确50身份顺序、有限向量和矩形End、原活动域、home／闭环及单点炮台。所有50真实保存leaf离树配置、双向完整身体corridor／Grow24／联合间距全部验证后才允许第一次AddChild；失败释放本批已配置root，不Dispose借入资源。

纯数值编码与读取各自有实际RED→GREEN；18负控包含缺失／重复／未知ID、换行序、改home、扩territory、坏形状、开环／退化路线、多点炮台、空／溢出矩形、schema／kind／context／digest／损坏JSON及几何可行但顺序被改。全50逐字段与全部点序、四点飞行正例和防御性复制单独核对。合成中心仅用于codec，不是合法原生落点。

实际原生负控为缩小最后一个Occupancy、进入真实保存实体、进入真实蒸汽Hazard；必须命中预期stableID和具体世界碰撞／Hazard SourceId，且都0挂载。初版实体负控先选到完整胶囊越界的位置而在Configure被拒，真实失败被保留；随后只修负控选择，不改地形、身体或危险区。

## 事实：真实同场活动与幽灵修正

实际缺挂载RED：`092802-628-9cb29003…` 在全部离树准备后expected51／actual1，非编译或资源崩溃。随后真实挂50至准确sector的Gameplay/Actors，唯一初始姿态写入发生在AddChild之前；沿用fixture-owned形状副本，Ready后原HP／Healed0。每个唯一physics ID观察完整身体、基底、原区域、已验证路径、所有keep-out、支撑和连续位移，结束检查原地图碰撞体未变。

720physics按真实48px/s路径长度、60Hz、固定启动和路点余量预先确定；41个非炮台必须在原2px范围依序到达包括末尾home，9炮台保持原地。地面SafeMargin落稳不是放大未经检查的包络：每次实际Y改变都重新验证完整实际高度的双向corridor，并复核所有50个实际高度走廊的Grow24两两间距，结束再复核一次。独立代码复核曾指出缺少后者，已用相切正例／实际0.1px重叠反例先RED，再实现共享谓词后GREEN；原输入点序、活动域和间距不变。

`093138-873-373c4552…` 已有50Ready／51身体，但第313physics幽灵飞行从0.8变为2.333343px，未完成720。源码与独立审查确认：每5.2秒hide无条件CancelAirAttack(true)，把从未有目标的Hover也强制按140速返家；不是传送证据。它还有使较深路点持续未访的风险，但该深点饥饿尚未被该案实测证明。

现只改hide调用的return条件：非Hover、实际combatTarget或锁定攻击才新增返航。隐藏仍撤销攻击，原Returning的OR、已接受K／ThreadStunned外力、life-version、权限、相位时长、速度／力／身体／地图均保留。专项新增Flyer／Diver无目标跨相位长闭环，先真实失败；修后 `093951-729-f5433bdb…` CORE8／0，双方各720唯一physics、5相位边沿、5点全部有序、HP2／Healed0。**退出4ShapeRID使整案仍严格FAIL。**

最初整批沿用旧55s宿主watchdog，`094141-899-ff7a0906…` 真实超时／退出-1／55050ms；不能说已完成巡逻。新增工作量包含约30s搜索／事务及50实际活动，只为普通census设120s宿主watchdog并优先报告TimedOut；其他phase／诊断和720物理上限不变，不是游戏性能修复。加入少量Ready／120步进度和落稳复核计数。

`resident-map-census-20261022-094607-148-d46f755cbc3349838700e79c3aed797d`：native0／66928ms，完成全部50Ready／51身体、41个完整有序闭环＋9静止炮台、720连续唯一physics、原HP／零治疗／零后置改位；489次实际高度完整双向复核，巡逻观察耗时36912ms。JSON50行、error=null，SHA `DE6FF3A41B860306409BFC2F14CC073FA2325379EF3D1ABD714057F33C31A08A`。**该案仍严格FAIL：stderr／external有一次Handle未初始化，经SwapGCHandleForType→GD.Load→CensusLeafSource.Configure:948／Activity:1353，发生在候选加载；不是FATAL或未完成。** 不因成功marker或engine0豁免。

## 推理与风险

- 当前证实的是隔离候选143地形中的完整身体／真实无目标巡逻，不是旧26共存、正常Main／Explore战斗、Rush／Dive／战斗Returning、屏内炮台伤害、存档生命周期或玩家体验验收。
- 正式仍127地形、新增0/50、全部Placement=null、24正式子场景Unimplemented；测试leaf明确ExplicitPlaceholder，没有将复用外形冒认最终家族美术。新最终视听资源0，既有可替换素材接口与资产表保留。
- 720物理步的重型测试耗时不证明正常显示60FPS，也不能据此给出硬件性能归因。句柄／资源泄漏及历史FATAL、Archive640/704和post后75scope仍须实际关闭，不借安静重跑冒认修复。
- 主线／探索／全部最终视觉、UI动作特效音频／范围明确演出／真人键盘和60FPS／Windows包与获授权GitHub版本仍OPEN；不承诺十万付费人数。

## 最终同冻23相回归

所有案例目录位于 `G:/knight/验证证据/october13-stable-claw/`，目录格式为`phase-20261022-下表后缀`。表中是严格结论，不把引擎exit0或行为marker当成整案通过。全部TimedOut=false、guardFailures=0；原生退出／宿主毫秒如下。

| Phase | 完整目录后缀 | Native退出／ms | 严格结论 |
| --- | --- | --- | --- |
| resident-allocation | 095421-070-3f4f92f36bc0492c98a0c1fe747a5207 | 0／480 | PASS：18 codec负控及2实际间距控制 |
| resident-family-ghost-air | 095449-077-06aecd18dc884c80bebc45b86b07e541 | 0／34680 | FAIL：CORE8／0，但4 ShapeRID |
| resident-family-ghost-air-boundaries | 095552-627-5dc24663f2e94bb698319912206914a2 | 0／20714 | FAIL：10／0，但7 ShapeRID |
| resident-air-default | 095640-914-c78ce63883ed45918ce5485410590b8e | 0／4960 | PASS |
| resident-air-roles | 095712-400-1456fb32c88a4c35a666008aee79c37b | 0／20975 | FAIL：7／0，但4 ShapeRID |
| resident-family-bone-air | 095807-465-b1146a842eff44d69c5a939475676b9f | 0／11245 | FAIL：8／0，但7 ShapeRID |
| resident-archive-alcoves | 095847-334-723020a51a8b4574a53771433a27c370 | 0／18400 | PASS：613观察／618physics |
| resident-archive-metadata | 095933-887-b7d21959afee461b9208304b8375ec57 | 0／4297 | FAIL：266 ShapeRID |
| resident-steam-metadata | 100005-296-e11f0b6a5da542c5a49ea42b68565b2d | 0／5055 | FAIL：266 ShapeRID |
| resident-gear-left-access | 100038-872-bd6520a5c4a94d59b8f88db77a690b00 | 0／19334 | PASS：289观察／579physics |
| resident-gear-alcoves | 100125-091-67db6f9bf75940e981c2750fa234b876 | 0／9761 | PASS |
| resident-parent | 100202-679-2bb35e2058234ab5ae4b6aa119723b3b | 0／2283 | PASS |
| resident-steam-alcove | 100233-949-ea4d07cddd6240f4834e8d314a237d7e | 0／10343 | PASS：80观察／161physics |
| resident-pendulum-alcove | 100318-912-1091940cec15460ea1b2d0cbeb4c746e | -1073741795／11894 | FAIL：GCHandle Finalize FATAL |
| resident-abyss-alcove | 100405-445-489005ca5a294133845b0c46b4952114 | 0／8765 | PASS |
| resident-frozen-alcoves | 100443-493-e287141a87ef4e66904ec9f18b3f4353 | 0／31078 | FAIL：两次Handle is not initialized |
| resident-map-census | 100543-463-b96b7dc78dfd4318a91da5c4917d5be8 | 0／67699 | PASS：真实50Ready／41闭环＋9炮台／720physics |
| resident-void | 100744-204-e3fea9792611485f94735f9b1d9f966d | 0／13407 | PASS，非历史187RID根因修复 |
| keyboard | 100831-474-e2b4ce116fae4ab69551fe4b306f08b5 | 0／9663 | PASS |
| actions | 100912-533-0e506eb44a2345168426d2d27fcb712a | 0／7239 | PASS |
| canyon-structure-v2 | 101004-203-67ea385e179f4047adba702dbf5d7c62 | 1／1004 | FAIL：ArchiveSideCave期望640／实际704 |
| prologue-pre | 101045-950-5b062f872e63402f853d5e4fff6bcf99 | 0／101622 | PASS：完整pre专项集合，非正常双路试玩 |
| prologue-post | 101308-043-8be888c7cc8a47e99ab6eb8eafadfbd3 | 1／1004 | FAIL：同一Archive断言先失败，后续post集合未执行 |

当前census的payload SHA为`CBC7F9C168036F66178019F80182149B7291D2AE051D966E0A31EC2702F21E53`，context SHA为`279EC0B21DA9D8B305EDC1E1856DC3A11F89D9A57D242C7BC056F4B9144EE6C2`。实际JSON50行／error=null／489次落稳全走廊复核，720个连续physics为14→733、最大单步0.80352926；实际高度Grow24间距守卫也通过。该巡逻JSON SHA为`84B0A60A5EA407C64CE8AFDC4B748C779B926216642CB219D168333510672AD2`。三原生日志无ERROR／WARNING／FATAL，且三个真实几何反例均0挂载拒绝。这只认证该冻结候选的无目标活动，不认证战斗、旧26共存、正式出生或正常世界。

钟摆case先完成双K玩家片段，在后续候选fixture出现`mono_object_disposed_baseref:csharp_script.cpp1788 → GodotObject.Dispose(bool) → Finalize → GC.RunFinalizers`的0xC000001D。不能归咎于玩家路线，也不能因路线marker先通过而忽略崩溃。冰区case完成行为仍有两次Handle错误，`GD.Load → CensusLeafSource.Configure:948/953 → RunActivityCandidateCensus:1353`，同样未通过。资源根因尚未关闭。

## 保管链与最终只读封核

独立审计核对23案的115对前后清单、实际8074份选定源码副本／234份helper副本／138份binary、46组build／engine PID与argv／contract／native／wait／provenance，全部匹配；不是声称所有1710文件都有实际副本。23次构建exit0且零warning／error，全部运行WARNING=0，但上表的RID／ERROR／FATAL仍严格否决。

共同清单SHA：

- source1710／880C#：`DB2542B49667F615B994DA53B92F22876DD9869D7667E9EFB91D9286AD23645B`。
- 六文件binary：`70F6B6BE1D82433661497538A36D04B48AD4506028CCBE5E746BF14EE7A91CB2`；Knight.dll为`E788AA9E827EC16982AA906E68A58E6FAF40445B5C0014A82DDD89B0E299DCA0`。
- 用户109行／104文件仅哈希：`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`，未读存档语义。
- 21案共9项基础helpers：`52024C7ABCB7FE11C0D96FAEF9819A2AA984F61DA6E41156B010396F338A7D39`。Census只增verdict helper，清单为`120E3C94BC8A3742735BA1C8C9BDA7E2BA7218223DD2EC2C525884A29CCD58FF`；Pre只增clearance admission，清单为`5AE00B647844B85C326CDE03015556047F80DCF744859D3E92E0D5A33EF8A1F3`。不是三版运行代码。

普通DLL无DefineConstants，已查同SHA DLL TypeDef中MetadataResourceTrace=0、69个日志出口无对应诊断标记。文档更新后的root只读复核：1710源码／109用户行／六binary与最终case完全相同，三种helper清单共29条逐条匹配，owned消费者0。HEAD仍`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，index SHA仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。此一致性不覆盖清单外仓库文件。

独立复核的重要问题（实际落稳Grow24联合间距）已经实际RED→GREEN及最终50全量原生检验关闭；没有据此关闭资源根因或完整Tasks1–4。容量仅手动单次检查，Current4838／Baseline3508 bytes、增长1330、Notify=false；未重建自动化或旧归档目录。

## 后续顺序与未交付项

1. 沿实际加载／临时实例与finalizer边界定位资源拥有者；保留上述失败，不通过GC、缓存、额外drain或Dispose借入资源来猜修复。
2. 处理明确的Archive区域合同不一致并严格跑通完整post；不得只改期望值逃避实际空间要求。
3. 在已冻结50候选上验证实际战斗追击、Rush／Dive、目标撤销后的Returning及旧26共存，再接正式Placement与24正式族系场景。正式新增仍0，不提前发布。
4. 正常主线／探索、最终美术／UI／动作特效／音频演出、真人键盘容错与显示性能、Windows包和获授权版本存档仍须完成，完整1–11／Tasks1–4保持ACTIVE。

未进行Git写入、删除、付费生成、OS前台输入、自动化或新增媒体采集。23相owned进程已全部结束；本次只是有边界的内部实测增量，完整目标仍未交付。
