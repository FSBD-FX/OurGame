# 正常地图地形与小地图同步 · v060

2026-10-22。实际工程 `G:/knight/Knight`；完整1–11与人口Tasks1–4继续ACTIVE，非成品发布。用户最新引擎询问是评估请求，未授权迁移；继续本Godot工程，不操作另一UE工程或恢复已删除归档。

## 已发生的生产改动

- 原正式五区127碰撞现为 **143：6 / 37 / 31 / 57 / 12**。Descent、Basin、Ascent分别增加8、5、3组实际生成的实体/抓取profile/矩形碰撞与skin/edge；不是仅候选父场景。
- `ResidentTerrainSynchronizer.Apply`从实际C#生成器窄复制16组，完整预检后追加，保留旧节点顺序、Owner、存储属性、metadata、资源共享和持久链接。新碰撞使用私有矩形。
- 批准的既有AbyssExit2例外为中心740→748、宽200→216，以及对应skin位置/尺寸与edge ±100→±108；保留GripStone。复制旧矩形后改私有副本，不修改借用资源。
- 正常父场景仍引用五个原canonical外部路径，仅替换从实际五区提取的完整小地图catalog。143几何、完整序列化字节、revision、边界顺序与三个checkpoint已对照；不复制候选父的内嵌五区图覆盖正常父。
- 五个旧v059区域原字节保存在 `tests/fixtures/resident-terrain-v059/`，供明确的旧127控制使用。四个被推广文件的旧字节保存在计划workspace的 `v060-before-canonical/`。未删除历史材料。

四文件仅在严格通过的135221暂存案结束、原1719项源清单读回无漂移后推广，Entrance/LostTemple未改。v060推广时四文件与该暂存输出逐字节一致（随后v061仅父内容选择作了新版本，见对应报告）：

| 正常文件 | 字节 | SHA256 |
| --- | ---: | --- |
| CanyonDescent.tscn | 80929 | 57291CA6CC3E4F50D75C60BF43D2F363BBBD049BE8579B5E457D4689B1F265DA |
| CanyonBasin.tscn | 79227 | 663197ADF7F14FAAEE7C4C0D438E8DB4A03F6350A9ED11913F5B57FFD87A6B9D |
| CanyonAscent.tscn | 108702 | DB1FBC689FFC4EFE45182BCF69E93F84E314732F647706E0A92B81DA2859E5F5 |
| PrologueLevel.tscn | 514214 | E51812A1614320196CD3369892998F68B86ADA775B38ABD02D729B6DB063D640 |

区域位于 `scenes/prologue/sectors/`；父为 `scenes/prologue/PrologueLevel.tscn`。105个直接外部引用均为项目res路径，没有fixture、证据目录或绝对暂存依赖。新最终美术/音频为0，沿用可替换skin，不能把新地形计为最终美术。

## 测试先行与边界

所有案位于 `G:/knight/验证证据/october13-stable-claw/`，前缀为下表phase，时间均20261022。根唯一构建/原生owner，运行期间全部源/helper/build/docs冻结。

| 案例后缀 | 观察与结论 |
| --- | --- |
| resident-parent 134018-954-b6b05d17955c4f0e9aeea74001578850 | 可编译no-op真正RED：expected37/actual29；另88RID保留，不是编译失败证明 |
| resident-parent 134241-827-54409526ab2149c5a0d536d6031d8694 | 新地形/字段通过，测试持久链接过滤错误未归一`./`；只修精确新增路径过滤 |
| resident-parent 134617-888-0314740b79914f3f82749f754f10fa70 | 第六负控last_donor_edge实际RED；生产完整预检补support-edge几何校验，不放松负控 |
| resident-parent 134744-385-5258b8a5d22c464fb97f865807c93c7b | 三个窄生成区域/六拒绝/完整旧语义/保存回读严格PASS |
| resident-parent 134946-239-dbfb80b263254cb68ed8c2e51683f847 | typed Godot Dictionary非IDisposable导致编译FAIL，明确不是行为RED |
| resident-parent 135058-830-42687ce7c26b478eb85198911f6d0480 | 父catalog no-op真正RED：expected143/actual127 |
| resident-parent 135221-224-95c3864b6aef4977bb4c347ab5c2f2fa | 三暂存区域及仅catalog父、外部路径/完整字节/revision/边界/checkpoints/非catalog语义严格PASS；四文件推广来源 |
| resident-parent 135557-079-0e62d0a07c4841e4860fae2d74234427 | 已推广正常143及完整MinimapCatalog套件严格PASS |

新路径所有存储字段/metadata/深层资源与生成donor匹配（新增的顺序按追加约定），旧路径包括顺序全部保留；同一检查器能拒绝无关碰撞修改。幂等再次Apply不分配新形状/不改变图。保存回读证明语义而非数值serialization unique_id字节不变。

Donor创建形状为显式拥有的Descent48、Basin45、Ascent66（62矩形/4圆），核对精确路径、类型、大小、原生身份及与借用资源不交叠；私有图Free后仅释放这些创建时拥有的wrapper。没有增加借用Resource.Dispose、GC/cache/drain/KeepAlive规避。

旧三种hybrid fixture此前仅生成新Descent37，其余从canonical载入；推广Basin26→31/Ascent54→57后world135必然变143。B只迁移这三fixture的13行数量/协议，不全局替换135、旧127控制或坐标1350/其他预算；原完整Player/Ghost/Abyss通路必须实际再执行。旧MinimapCatalog130同样迁移为实际143，不降低全字节/几何比较。

## A21最终批：15严格PASS / 6严格FAIL

同一A冻结1719项、883 C#；不是B冻结。所有build安静，guards0、无timeout。每项都有独立before/after、源副本/helper/binary、实际PID/argv/wait与三原始日志。

| phase | 时间/身份后缀 | 严格结果 |
| --- | --- | --- |
| resident-parent | 135838-133-297a67d47add4930b809766c194c0eb9 | PASS，实际canonical143及完整catalog |
| resident-map-census | 135919-774-5e94ccc675084015a6b678802c7713a5 | PASS，精确运行输入50Ready/41闭环9炮台/720physics；仍非正常出生或战斗 |
| resident-gear-left-access | 140058-773-165234d75ad248c89ebd19f7457daf62 | PASS |
| resident-archive-alcoves | 140157-610-cc3c5d24e0ec48208e8e49142a827ee3 | PASS |
| resident-steam-alcove | 140246-212-4cf24385a0be43deab4b29bbdf658c58 | FAIL：2 Handle ERROR；行为跑完/native0不覆盖错误 |
| resident-pendulum-alcove | 140331-872-e8dd7fcf5e3b45d4954cd14dd52f2054 | PASS |
| resident-frozen-player | 140429-106-b20f6ccc8b6746d3a318d273a50477e4 | FAIL：前置expected135/actual143，另251 ShapeRID；未执行路线 |
| resident-level-lifecycle | 140501-866-9ed5ea8f19624ebd826167eb89ac6754 | PASS |
| resident-save-compatibility | 140534-370-7cbded336b714685b842ae2b2ed2561a | PASS |
| resident-allocation | 140604-209-9f9075b38c10460e962ea552c26b4170 | PASS |
| resident-state | 140637-526-d721d0d22de34498b2d804c0ae03d327 | PASS |
| resident-lifecycle | 140708-917-f5683572d0254870a0de87bb0274c2ee | PASS |
| canyon-structure-v2 | 140737-491-dc792b27a4314306b910060843d9170a | PASS |
| resident-scene-binding | 140805-827-24164e47f0704812bad3db0cf4c1588f | FAIL：137 ShapeRID/stdout，native0 |
| resident-subtype-profile | 140840-122-cc27a62ce2da4bcb94136830b6e02e6b | FAIL：31 ShapeRID/stdout，native0 |
| resident-archive-metadata | 140915-002-16a50c92b3cc4ea29b26e365772da374 | FAIL：96 ShapeRID/stdout，非旧255 |
| resident-steam-metadata | 140946-799-6e33aaa5edaf419995b67b354af419c7 | FAIL：206 ShapeRID/stdout，非旧266 |
| keyboard | 141021-132-261685081a1549e88b609e43fd42deca | PASS |
| actions | 141103-744-45091399145b4b76846dc0aa6d855292 | PASS |
| prologue-pre | 141139-367-2912bc61a751415b8ff86d18542cc670 | PASS，PID52516/99882ms |
| prologue-post | 141347-296-9bce3d389bc04138b8a8b66dd1f5f252 | PASS，PID53124/147347ms，75原案例/8owner/4精确故意警告 |

Steam两Handle共同入口为PackedScene.Instantiate→MountSavedSectors:1935→Steam:41；stderr/external镜像同两事件，不能加成4。独立核验105对清单、7604选定源副本、214helper副本、126binary、42实际进程合同完全匹配。Source SHA `C09CB60F1B53F85FF01BC234F173B475976DB6CA1ED5ED804481A2B76FEC82D4`；DLL `6C9B4E156A3ABD4CD8C013710D65F17B18997E11A187CC2AC7A5108770104343`。

## B10补测：9严格PASS / 1严格FAIL

仅两个测试C#文件相对A改变，生产地形/脚本hash不变。不能把31次运行合为“同源31项”。除旧hybrid数量修正，还把既有ExplorationMapSave OptionalMatrix纳入save phase。

| phase | 时间/身份后缀 | 严格结果 |
| --- | --- | --- |
| resident-parent | 141924-226-b547705fddf847078b883cfc26ea26e7 | PASS |
| resident-frozen-player | 142003-856-bae2ec6d5f3f43b48aa29395a2ccfa99 | PASS，实际完整旧通路；不是历史资源根因修复 |
| resident-abyss-alcove | 142038-688-95c73a886e3e4b29a07d717c2b0520b0 | PASS |
| resident-frozen-alcoves | 142115-974-5016acdc6b154dcc93dfbb7fc9deb974 | PASS |
| resident-save-compatibility | 142214-851-b7df2bd661304d749d4d30411ca3031a | PASS，含可选地图版本/主进度矩阵 |
| resident-level-lifecycle | 142247-278-be9d5766ceea404ab750e926446cbf98 | PASS |
| keyboard | 142319-166-1608dda057384b03b8a8189063704c3e | PASS |
| actions | 142354-165-6a5cf33f8e98432d9154d085bfd54a50 | PASS |
| prologue-pre | 142430-031-04584f4a63f043f7aea63cf734d0b0cd | FAIL：7 Handle ERROR，native0/PID33472/100663ms |
| prologue-post | 142640-706-769f2147082a432f9c90dbf6c4d9b27c | PASS，PID8512/148041ms/75原案例/8owner/4精确故意警告 |

pre实际链GCHandle.FromIntPtr→SwapGCHandleForType:1284→Node.AddChild→BasinSupplyRuntimeTests.OpenSample:459→CheckSupply:252。完成marker不是通过；没有FATAL/timeout，仍严格否决。不据此断言本次地形改动导致错误，不重复无改动试运至安静。

独立核验50对清单、3624选定源副本、102helper副本、60binary、20进程合同完全匹配；build安静/guards0/无timeout。Source SHA `6AF16E7566001E9339D659C35BF7B8B8FAE5A49A176DB11C90A2468FD78635E6`；DLL `F48CD17F54CC4036FFED2021DE1BA559D0F777E112EDC48B148793838722C853`。两个批次所有拥有的进程终态后才记录本报告。

## 存档与未完成范围

合法schema4/layout2旧档遇到catalog revision变化，仅旧探索迷雾被丢弃/重新探索；残片稳定ID、6枚门槛、checkpoint、Boss等核心先独立恢复，读取不写原文件，下次正常保存才写新revision。现有OptionalMatrix对exemption两值、主进度与读取前后文件SHA验证通过；**不是实际旧127 revision→新143完整Continue端到端证明**，后者仍待完成。

50/201运行布点已接真实Level与唯一manager，实际正常地形现143；但24正常子场景仍Unimplemented、自动出生未接，正式新增仍 **0/50**。隔离50Ready/巡逻不算76+Boss全关战斗、Returning、旧26共存或正常主线/探索验收。

下一为24明确开发叶与可编辑配置、正常世界隐藏准备/提交后出生协调、实战/返航/重置，随后最终统一角色/敌族/场景/UI/动作/VFX/声音及明确演出。全部机关、正常Main/Explore到Boss和有支撑出口、隔离旧档、真人键盘、720p–4K/正常显示60FPS、归档审阅与唯一Windows ZIP/GitHub读回等完整目标不减；资源Handle/RID/FATAL历史与本次严格失败继续OPEN。

没有付费生成、Git写入、UE迁移、OS输入、自动化、新截图视频、发布或删除旧材料。测试仅清理自己的临时隔离存档，不等于读取/清理真实用户存档。媒体20GiB按手动工作流在正常汇报中提醒，不恢复监控自动化。
