# 正式蒸汽区域窄同步与完整回归 v048

2026-10-22，Asia/Shanghai。唯一资源根G:/knight，Godot工程G:/knight/Knight。上一v047有实际生产/运行进展，路径确认本身不算新游戏制作；本轮修复真实正式场景错误并生成新证据。完整序章1–11与常驻Tasks1–4继续ACTIVE，未完成，不是新的最终试玩交付。

## 事实：已实际接回正式场景

v047完整post在正式Steam标记上提前失败：源码是世界(3040,3800,1160,520)，保存节点仍是旧(3040,3960,1160,360)。没有回退源码或放宽结构断言。

新增窄生产操作`tools/build/prologue/SteamMetadataSynchronizer.cs`：一次预检两个不同且detached的Basin根、三个实际节点/类型/非零有限范围/中心一致性，之后只复制5个primitive/packed值，不复制节点、资源或子树。三个现有节点保持原名称/UID/其他字段：

| 现有节点 | 实际复制的生成字段 |
|---|---|
| Gameplay/MicroScenes/SteamValves | local_bounds=(1760,200,1160,520)，Position=(2340,460) |
| VisualRoot/GameplayReadable/MicroSceneReadability/SteamValves | Position=(2340,460)，四顶点=(-576,-256),(576,-256),(576,256),(-576,256) |
| VisualRoot/Midground/ChallengeCompositions/SteamValves | CompositionBounds=(1760,200,1160,520)；自身Position仍0，避免重复平移 |

原Basin26块/世界127块碰撞、旧演员/操作/检查点、完整Minimap的SerializedValues/Revision/边界顺序未变。背景绘制范围确实扩大，不宣称像素完全相同或完成最终美术。135仍只是独立候选地形，没有整包推广；正式新增常驻仍0/50，全部Placement=null，24个正式子型仍Unimplemented。

## 失败先行与保存证明

新增`tests/integration/SteamMetadataSynchronizationTests.cs`，实际原生运行，不是搜索源码字符串：

- 全部子树的路径、顺序、类型、Owner、groups、Storage字段、完整metadata；立即冻结深层Resource内容、外部文件SHA及共享引用拓扑。不把活动Resource/Variant留作before；未知存储对象失败关闭。SceneState另外核对保存连接、持久组与嵌套实例引用。
- 独立字面值要求5个目标字段正确；旧输入必须恰好改5键，现输入恰好0键幂等。保存279节点后回读，无节点丢失。
- 六个原子输入拒绝：null、同图、外国sector、缺节点、同路径错误类型、零范围；两个额外变更反例：非allowlist碰撞字段、私有复制形状的深层尺寸。foreign测试不是“任何同Basin来源认证”；来源仍由冻结SHA负责。
- 两次真实BuildLevel含明确五路径，127小地图字节、revision、边界顺序与实际canonical父场景一致。没有写canonical父场景/minimap。

所有案例在G:/knight/验证证据/october13-stable-claw，日期前缀20261022。早期7例均保留，不与最终批次拼绿：

| metadata案例后缀 | 实际结果 |
|---|---|
| 031611-783-bc09df9f2c8648ae9c1ecb7f6ff55b7d | no-op真实RED：native1/1208ms，旧bounds360/360不符200/520；stdout83 ShapeRID也保留 |
| 031816-304-36b7883e1912417a952d3079546ce9a5 | native0/5741ms，早期较弱专项严格PASS，不是后来加强版的替代 |
| 032113-468-845ec425dec84a5881de59ccd3a12ff7 | 加强语义/连接/深层反例均满足，但stdout254 ShapeRID，严格FAIL |
| 032414-831-ed43aee47e33455db7fd797e40da08be | 四个CS1674：typed Array不直接IDisposable，未启动engine；不是行为RED |
| 032523-454-270fa6ff47bc41e5aa87967f6c9db53f | caller-owned临时Variant/容器已确定性释放，native0/5260ms仍stdout309 ShapeRID，严格FAIL；不能声称它治好资源问题 |
| 032818-926-ddce3fbb4f814024991ef7d4758efa11 | native1/5322ms，新增明确owned资源ID释放断言失败；根因是这次新建authoring Definitions数组仍持有它们 |
| 032941-287-bb62280266d64d21b695bfba7c78e2e0 | 仅清理自己新建根的数组/字段及已审计owned资源后，native0、严格PASS；随后才发布生成5字段 |

本次owned集合是完整builder两份各43个直接new的形状、两个新BuildLevel的10 Definition+2 Catalog、CacheMode.Ignore私有PackedScene，以及caller-owned临时容器/Variant。形状构造点经独立只读源码审计，严格43/无路径/与两份canonical形状ID不重合只是补证，不是通用收养规则。释放后精确nativeID无效，借入sector scenes继续有效。未来builder变化须重审。没有Dispose借入canonical Shape/Scene/Script，没有GC/cache/KeepAlive/退出drain/等待或依赖引擎变更；不据某次安静关闭任何历史资源故障。

## 发布边界

最终隔离生成完整artifact SHA256：E84717D4955070E6B0F92D98A6E29F448E74C97C56082EA79A91E00E1B446A25。发布前实际canonical与source-before一致：A39C0DED8AB052FDE502DAE744BD916F325B6E939CBB2C322CDBE5C42E472687。

所有native终态后，root用apply_patch复制该产物的5条字段行，保原header/UID/外部引用，没有手填TSCN值或替换完整重序列化文件。与冻结原件逐行比对恰5行不同、总行数相等，其余1705源/资源零漂移。发布后的Basin SHA为DDDD16FF82FB6B34D1813538769585CAA85D056086A597470712D92FE7CB8876。整个HEAD diff有先前脏树编辑，不能用它冒认本次只有5行；证据是本轮冻结before比较。原生owner没有添加canonical写入例外，所有运行仍要求全source不变。[发布细账](../../.superpowers/sdd/2026-10-16-resident-population/steam-metadata-publication-20261022-v048.md)。

## 最终同冻结版本：10门8PASS／2严格FAIL

外层session24302已终态exit1，全部native/build均exit0、timeout=false、guards0、无waiver；原生exit0不代表严格PASS。以下均日期20261022，单位ms；先前7案不属于这个冻结批次。

| phase | case后缀 | build | native | strict |
|---|---|---:|---:|---|
| resident-steam-metadata | 033207-231-ac5328ff89a04a9faf99e004bcc45d5d | 22640 | 5054 | FAIL |
| canyon-structure-v2 | 033240-801-1e83027f99d24cd8a959124cc3f0248d | 22608 | 1539 | PASS |
| resident-steam-alcove | 033310-709-2b7da5f5cac54d1c813d00ed8ca4de42 | 22637 | 10596 | PASS |
| resident-map-census | 033350-387-3f63d25e2d754d1f8f7144b33e50bd83 | 24291 | 24132 | PASS |
| resident-parent | 033444-925-17752853d59f4f0788caadee9718c066 | 23673 | 2540 | PASS |
| keyboard | 033516-827-09e87d04ac1c4fd183417d6413ac3e05 | 23199 | 9694 | PASS |
| actions | 033555-813-80d56ab8043a4b8bbb60054f32a2f80d | 22838 | 7122 | PASS |
| resident-void-stream | 033632-514-119726e7712047b4b5f79f6e4ba333c6 | 23611 | 1643 | FAIL |
| prologue-pre | 033704-276-c71547935b534abc8d7a37d511512787 | 21324 | 99873 | PASS |
| prologue-post | 033911-036-e5ced7bc653d404c9a7d25982956be68 | 25415 | 149336 | PASS |

build累计232236/native累计311529，不是端到端工时或游戏性能测量。所有build0警告0错误。

两严格失败：metadata stdout一条252 ShapeRID（不是254），void stdout一条187 ShapeRID；均stderr空，external无ERROR/WARNING/FATAL。不能因日志未镜像或nativeexit0放行。新metadata加强版最终批次仍FAIL，未宣称其资源故障修复。

post实际完成原75案例/0retired，18个兼容几何名称保留；8次同owner Main PackedScene实例nodes1→1。post有4个严格限定负控warning事件，scope来源/函数为1+1+2，stderr4条，external镜像4条，不是8个事件。无ERROR/FATAL。完整pre此次安静，不关闭历史Handle/FATAL/音频或其他资源问题。它们不是正常显示Main/Explore、真人或60FPS证明。

新全50调查仍47候选/3未解，1641走廊查询/76联合节点/search_capped=true；缺ArchiveSideCave.03骨骸冲锋、.04骨骸俯冲、PendulumGallery.04精工激光。不是已完成47只敌人。候选五区135/父内嵌minimap保持；专项Steam.02/home3780运动证不能转给联合Steam.01/home3770。

## 冻结与安全核验

独立只读审计与root读回一致：50组五类before/after清单零变，20份source一致；3540份保留input实体逐项hash零差，60份六binary跨例一致；20次实际PID/完整命令行与contract一致，10次wait匹配终态。1706项源/资源、876 C#。终态live1706source/10helper/104user文件/6binary均零漂移；真实userdata仅HASH，没有读语义。

| 清单 | SHA256 |
|---|---|
| source1706 | 0F9ADD3244710DB5D388994F673CECC9BE0D6D880BE736DACCF1599B45E72215 |
| 普通9helper | F4D8DE714939582739C12BB22521C667FE33AAAFFF3BD36A0C77A14A91E770A3 |
| pre10helper | ED89F167795A85CF55561FEB6B9F2BD57F694BB4456669D4E24F46CA1C4D3FB7 |
| 六binary | D704F22E4788434147527735076A984AC47F01F25CED0F8102D53F7933789C4B |
| Knight.dll | E680A4A46927F1718150F465657AA045EE7067DD6AADC6586DC23C0086AF48F3 |
| user109行 | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| continuation EMPTY | 7EB70257593DA06F682A3DDDA54A9D260D4FC514F645237F5CA74B08F8DA61A6 |

user109=104FILE+2DIR+3ROOT，其中存档/设置3文件；continuation是0条输入、2字节CRLF，不是一份继续存档。HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6/index21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C保持。无新窗口/OS输入/付费/自动化/Git写入/清理或删除；无新媒体。工作内一次容量检查4838bytes/baseline3508/growth1330，Notify=false，未重置基线。

## 下一步与未完成边界

继续真实常驻地形与玩法制作，下一优先核对钟摆东侧有限实体龛；只读建议见resident ledger，不是已跑通的新地形。须先保原Grip4/5真实K入口与完整胶囊，再完整身体/危险扫域/同Player真实进出、全50联合重算，不调拉力/碰撞/搜索预算冒认通路。Archive两点、Gear左层真实连续抵达、全部50活动/攻击/返回/占用战斗/正式保存与manager装载、完整围合/统一原创视听/正常双路/旧档死亡恢复/720p–4K/真实60FPS/真人/ZIP/GitHub最终交付继续OPEN；第12未来预留不并入。252/187及历史资源错误要独立识别真实所有者，不反复重跑相同case求绿。
