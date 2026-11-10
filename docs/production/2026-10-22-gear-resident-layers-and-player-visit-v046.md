# 齿轮区重型敌人分层与实际玩家往返 v046

2026-10-22，Asia/Shanghai。权威工程 `G:/knight/Knight`，总目录 `G:/knight`。本轮为实际地图生产与原生局部验证进展，不是完整序章交付。完整1–11目标及总待办保持ACTIVE；不恢复已删除的归档项目或自动化。

## 事实：实际修改

`tools/build/prologue/CanyonContentBuilder.cs` 的正式地图生产器增加两块独立实体承重层，复用现有TempleMetal灰盒外观与可替换skin，不手改canonical场景：

| 实体 | 世界矩形 | 专项实际Heavy家点 | 完整巡逻身体范围 |
|---|---|---|---|
| GearResidentLeftShelf | (2304,3792,152,24) | (2380,3750) | (2320,3708,120,84) |
| GearResidentRightShelf | (2856,3720,152,24) | (2932,3678) | (2872,3636,120,84) |

两个实际保存的独立占位叶为scrap-heavy、precision-heavy，均使用真实84px高Heavy身体和±32巡逻，不缩碰撞体、路径或24px活动间隔。实际保存Descent+Basin加另外三份canonical，物理与同源小地图共134个地形；父场景六文件搬移、五份内嵌快照和小地图同为134，并逐一核对原五个冰区矩形与新增两个矩形。默认生产入口仍指向原五份canonical/127，不冒称已经发布新人口版本。

新增测试在 `tests/integration/CanyonResidentPlacementTests.Gear.cs`；原类改为partial，沿用原真实世界、保存叶、环境保留和诊断工具。`RunStableClawPullTests`及现有PowerShell所有者增加独立专项/全图census准入，严格保存失败原件、输入和日志，不添加豁免。

## 实际RED与边界修正

全部原件位于 `G:/knight/验证证据/october13-stable-claw/`，case前缀为resident-gear-alcoves：

- `20261022-011609-285-a486f2fd75c24b86ab9daf1bf841aecf`：生产器尚未加新层；native exit1/1387ms、guard0。两只真实保存Heavy在两端与正反方向均因`incomplete_support`失败，随后才增加两个Solid。这是生产行为RED。
- `011922-847-6df0b779a4d64e7aa3f22ed59e32b0d4`：测试误引用不存在的AcceptedClawBoostCount，fresh build失败；不是行为RED。改用现有ClawFrameChanged/Idle/CastId及实际输入观测，不给生产类新增测试计数器。
- `012027-303-c6d27defed3a47e3917f1a5149b7c965`：两只完整支撑/双向路径和134地形已通过；原敌人出生位被保守检查器报native_hazard，后续未执行，不能说巡逻已过。
- `012311-291-de17f8e8f9a843a5a3e88325f1b73ba6`：真实未修改canonical127对照显示旧Rust已有native_hazard、旧Heavy已有native_world，候选结果完全相同。ground=false没有支撑面切线豁免，旧Heavy脚底3836正好是旧平台顶面；不是新增右棚造成的碰撞。
- `012445-348-326c12c1f7e545128a455b5616c7a224`：首次完整专项严格GREEN，native0/9667ms、guard0，后续又在最终冻结矩阵复验。

裁定：旧记录兼容性用真实127→134顺序独立世界对照，要求旧身体bounds/完整问题集合及全环境SourceId/Kind/Bounds完全不变，并要求两个实际新棚矩形均不与旧身体相交。不能把已经有保守否定结果的旧记录改称全清，也不能降低新常驻的全环境/完整支撑规则。

电磁区域实际为(2130,3870,260,120)，旧Rust身体与之重叠，但其运行时只伤害Player。此处native_hazard不证明旧Rust受到实际伤害。旧Heavy12px悬边没有被静默改成新敌人的全脚掌标准。

## 实际局部结果

最终专项中，两只Heavy真实运行360次PhysicsFrame/ProcessFrame观察：

- 左侧X范围2349.5981..2410.4019，右侧2901.5981..2962.4019；完整2D最大单次位移0.8000488。
- 始终保留radius28/height84身体、在指定层与完整活动域内、有真实落地、双向巡逻并返回家点；逐帧HP6，Healed事件0。
- 全局原敌人36个主/备选记录保留；Gear本区原记录是2个，不是36只或5只。
- 原静态下落口宽48px，40px Player两侧各4px净空；有限上下双向胶囊查询通过。完整潜在活塞区域仍与该通道相交，负控保留，**不是全时相安全通道**。

Heavy释放并同步后，实际同一保存Player仅在入树前放置于原UpperGearGalleryOne：一次Space+A普通跳跃，落到新右棚后自然制动，位置(2941.8323,3683.9333)、速度0；再用D走回原横廊。60次真实采样观察，原生命5→5、Healed0、Jump1、Dash0、Claw全程Idle/无cast，没有入树后位置或速度注入。观察次数不冒充精确物理tick/FPS。

这是居民不在场、机关回调停止的隔离地形往返。左棚真实抵达、完整战斗共存和活动机关路线仍OPEN，不以碰撞查询或高处初始摆放替代。

## 全50联合空间调查

新组合地图真实保存叶/配置factory覆盖50身体身份与24子型；49个配置候选身份覆盖24子型，20893条诊断。联合可选数量从44增至46、剩4；1609条路径查询、235个联合节点。仍有桶内option_limit，`search_capped=true`不表示地图已被证明无解。未增加原搜索预算。

Gear7/7全部选出，两个全图调查点分别是(2372,3750)和(2924,3678)，与上面的专项家点各差8px；这是另外两个几何候选，**不是这些精确点已获原生运动认证**。

尚未解决的精确身份：

| ID后缀 | 子型 | 区域 |
|---|---|---|
| archive-side-cave.03 | bone-charger | ArchiveSideCave |
| archive-side-cave.04 | bone-diver | ArchiveSideCave |
| steam-valves.02 | scrap-wanderer | SteamValves |
| pendulum-gallery.04 | precision-laser | PendulumGallery |

ID公共前缀为`canyon.v2.resident.`。全50的正式Placement仍null，24正式子型资源仍Unimplemented，正式新增0；46不能说成46只正式入图或50只完成运动/攻击验收。

## 最终同冻结版本回归

统一串行会话48158已终态，外层exit0仅表示批处理结束。按每案provenance、原生退出与原始诊断，**11案10PASS/1严格FAIL**。日期均20261022，以下后缀接在对应phase后；时间ms。

| Phase | Case后缀 | Build | Native | 严格结果 |
|---|---|---:|---:|---|
| resident-gear-alcoves | 012625-233-8fa2ff69a97d43359098699b5422c6f0 | 21518 | 9692 | PASS |
| resident-map-census | 012701-218-5225389e85d44910b3191c73fcf74d9b | 23045 | 24859 | PASS |
| resident-parent | 012752-404-91b81ed457f24ac48bee56bbb645ff91 | 23310 | 2847 | PASS |
| resident-abyss-alcove | 012822-241-e260769831de48f4960ad3a1e55db852 | 22230 | 8659 | PASS |
| resident-frozen-player | 012856-337-ae87319e9768458fae7af28d9819b851 | 22632 | 5419 | PASS |
| resident-allocation | 012927-658-cb71db307cd64c96896027332ca76714 | 23807 | 516 | PASS |
| resident-void-stream | 012955-109-f2ac83a574014cf087dc8cb36c9ff88f | 22222 | 1525 | FAIL |
| keyboard | 013022-201-44ae25f02cfe4cdfbeefdf2c0a6502f7 | 23456 | 9572 | PASS |
| actions | 013058-355-48ee41e213a6423db214fc9d590f47ea | 22499 | 7075 | PASS |
| prologue-pre | 013131-222-4c9194fa5d1445849a2d66f3ec527374 | 24894 | 100002 | PASS |
| prologue-post | 013340-602-10158af9703d478c8f24d36a6509a542 | 25037 | 148157 | PASS |

全部build与native exit0、无超时、guard0、无named waiver；原生exit0不能覆盖日志失败。void的**engine.stdout.log**有一条`ERROR: 187 RID allocations of type 'P12GodotShape2D' were leaked at exit.`；stderr为空，external不镜像。不是187条ERROR，也不是行为10/10与成功marker可以豁免。根因仍未关闭，不改GC/cache/borrowed Dispose/drain等来换绿。

post有4个限定警告，stderr及external分别同样4条，是镜像而不是8次事件；精确scope为seamless1、room-profile1、flow-registry2。原75项均执行、retired0，8次同所有者主场景节点1→1。其余9阶段无ERROR/WARNING/FATAL；本批pre/post安静不关闭历史Handle/FATAL、154/31或187RID、音频故障。

源码、辅助输入、真实用户文件哈希、continuation和二进制55/55前后清单一致；主代理终态后逐项读回1703个源/导入输入，漂移0。独立只读审计同意；其void日志sink文字曾误写stderr，已按主代理实际文件读回纠正为stdout，不重跑。

| 冻结清单 | SHA256 |
|---|---|
| source1703，其中873C# | EA1649FAB06B42BC5A16435F7936268C16ABC73AC39BEE28CFF125E15927ED7B |
| 常规helpers9 | BEB3B86AEF39B93EC658E064F0C9B8FC6BFB807CD7F938E883CAEDE80A2E9F79 |
| pre helpers10，仅多既有clearance | E5057481D382D5E03C318558FB966A7D11C7AD07C7EC7A444FFCDD947EEF808C |
| user109记录 | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| continuation空CRLF，0非空记录 | 7EB70257593DA06F682A3DDDA54A9D260D4FC514F645237F5CA74B08F8DA61A6 |
| binary6 | E436D5CC6A60EDF7397F8B5CE54D38FF521D2C979D0627D80B267E5D45F13E83 |

user109包含104 FILE+2 DIR+3 ROOT，不是109或104个玩家存档；没有读取真实存档语义。continuation两字节空行不是一个输入文件。HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。所有消费进程终态后才写本报告；PowerShell AST零错误，所改tracked代码diff-check退出0，仅既有LF→CRLF提示。

## 推理、风险与下一步

本轮消除的是两只重型敌人的实际生产支撑缺口，并证明有限原生巡逻/玩家路线与同源小地图交接；两块承重层不等于完成围合峡谷、最终美术或完整关卡。下一步先从实际营地走现有上游返回路线，抓AbyssBrakeLeft4、抵达AbyssSafeBottom，再从其右边缘下降后向西南抓左棚上表面；必须真实预览/同cast抵达/完整身体避开旧电磁和活塞区后落台，不使用高处初始摆放冒充可达性。该路线只有只读推演，尚未实测。

其后四个空间缺口继续完整活动域与原生玩家接近/返航验证。档案1510棚方案仅50px净空仍拒绝；Steam6px薄棚/2px顶距的推演不作为正式方案或已批准布局，优先调查原下层空间与稳健层级。Pendulum侧棚2520方案也是未实测候选；任何墙/顶围合都要验证不会截断既有抓点与路线，不能画非碰撞背景充当围墙。

全部50原生攻击/返航/家族姿态/HP/活动机关/manager正式出生，统一原创美术动作视听、正常Main+Explore完整Boss/出口、完整存档恢复/旧档隔离、720p–4K、目标硬件正常60FPS、连续视听和真人、唯一WindowsZIP及GitHub读回验收都仍未完成。无新付费、外测、前台输入、Git写入、删除或新发行；V1未覆盖。

没有新图稿/录屏，过程媒体仍4838字节/基线3508/增长1330，手动检查Notify=false；没有重置基线或建立自动化。后续关键媒体仍只保存于 `G:/knight/过程图稿与视频`，20GiB随正常汇报提醒、不停工。
