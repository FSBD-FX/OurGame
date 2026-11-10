# v133 · 机械爪反制与升降台联调，整关仍未完成

2026-10-26，KNIGHT，唯一工程 `G:/knight/Knight`。用户要求“设立目标并继续，精美完整，高质量的完成”。本次实际查询 goal 为 null，因此按原十一项完整范围重新建立 ACTIVE 目标，没有缩成单个测试目标，也没有建立自动化或承诺十万付费玩家。原[十一项待办](2026-10-18-project-remaining-backlog-v001.md)全部保留。

## 事实、推理、风险

事实：本轮只改四个测试 C# 和 canonical 验证入口，没有新增生产玩法、地图、美术、声音或 UI。真实键盘路线已完成齿轮 Bell 战斗及机械爪先控制后攻击的衔接；探索路线等待、登上和乘坐配重台期间保持 HP2。完整主线仍卡横廊，完整探索线仍在后续交叉攻击中受伤。最终前段虽进程退出0，仍有8个 Handle 错误，严格 FAIL。

推理：这些结果证明现有操作有可执行的局部路线，不等于玩家已经获得内容或视觉更新。下一制作项应是已定位的实体通路修正，以及后段战斗衔接；不能继续把测试修正计成正式美术或关卡完成。

风险：资源生命周期、完整双路线、正常速度下的画面/动作/相机/UI、目标硬件与真人试玩均未闭合。用户最新显示结论仍是 NOTSEEN。没有新窗口、系统键鼠输入或付费生成；没有发布、Git 写入、删除或引擎安装。

## 纠正旧判断

最初“StaticBody 类型排除了升降台”的判断错误，撤回。Godot 的 AnimatableBody2D 继承 StaticBody2D；181154 原实现已经接受静态地面、实际双向 CanyonLift 和普通 Animatable。该案失败的是错误的 foreign 排除预期，不是有效生产 bug RED。没有为满足错误测试放宽生产准入。

实际原甲板是世界 X[2700,2820]；玩家半径20、中心2813.5664，使胶囊右边到2833.5664，超出13.5664像素。原完整支撑检查拒绝是正确的。本轮只将原 helper 暴露给测试，主体未改。182200及最终194008真实保存 Player、静态/双向 lift/generic 正例、实际按键走出边缘/Space 离地负例通过。未证明所有禁用、销毁或异常变换地形。

## 实际联调结果

### 主线：固定支撑与同台战斗

保留原900帧、HP/身份/零恢复、完整胶囊、两个中性样本、55–64距离和无跳/无喷射门。对 retained Bell 使用已有 PistonTwo 实际 AI 引诱路线，而不追到甲板外。

183033显示 Bell 实际登上同一个 PistonTwo 后，旧 near 仍假设它在左外侧，900帧内J0/K0失败。只在 stage2、同一真实 Bell、Guard、水平速度≤2、同台真实完整支撑时，允许一次 `near=BellX+原60`，且必须仍在原完整支撑走廊内。没有写玩家/敌人位置或改变 AI。

184115及最终[194053 Main](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-194053-427-d4127b09b214402389888abd168c7808/engine.wait.json)实际完成 Bell：J7 started/connected、K3，HP4→4、damage0。然后 UpperGearGalleryOne 仍 FAIL。失败的 side-claw 候选185718已撤回为普通跳；没有试装 UpRight 喷射、增力、移除碰撞或延长时限。

最终普通跳从实际完整边缘3017.744起跳，AcceptedJump5→6、Dash0→0，原300帧结束仍夹在 Gallery 左壁和 RightShelf 右下角。日志包含两者真实碰撞法线。独立只读几何核对：roof角(3008,3744)与Gallery角(3040,3756)净角距约34.176，小于玩家直径40；这支持地形阻断诊断，不是“加推进力即可通过”的依据。

### 探索：先 K，再两次 J

185921实际最近 Moth 一开始 Align/None、距离89.67；两次 J 后变成 Stunned 并下落，远 Moth 距离695又超出620。旧准备门只等 Telegraph，导致未发 K。Stunned 也可能来自受击冲量，不能假定它证明早前 K 成功。

190950真实531ms RED确认旧 Telegraph-only 门拒绝合法 Align/None；实现共享 `Task10GearMothCastWindow` 后191344及最终193925通过17个字面合同，以及原 Target/Runtime/近敌29次边界与真实 K 链路。17个合同不是穷尽全部浮点状态。

新门仅允许活且可交互/原射程内的 Hover/Align+None/0，或原 Telegraph 且剩余大于 lead；全来源 Safe 仍须大于原飞行时间+14 tick lead。准备、preview、approach共用；原 LOS/目标身份/完整支撑预测/210上限不变。排除旧 Stunned、Dead、Dive、Recover。真实同 cast/target 的 K Latched→Released、adapter恰+1、实际朝玩家的新速度及 Stunned 都成立后才解锁原两次 J。

191428、193123及最终[194304 Explore](../../../验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-194304-394-d79df7c349c04ad2b1b00b9a47212638/engine.wait.json)原189/210窗口：J2、K单样本/单cast、adapter0→1、fresh_pull=True、HP2、E95.33324。没有扩大窗口或强制目标。

### 等待、登台、上升：防御来源不再只有 Bell

191428在下方等待时，真实 Moth 新 Attack2 使HP2→1；当时盾未请求，旧消费者只盯地面 Bell。仅对原 verifyRideDefense 路线，每次原 Step 复用已有全来源攻击发布扫描和真实 capsule，保留 Active/最后.35秒预告、原等待510/乘台200/生命门；根据真实 winning source 发 L 和方向，没有加帧、射线或暂停 AI。

193123及最终194304实际等待/登台/乘台 HP2保持。193123 Moth 的预告和 Active 阶段出现真实 L/active shield 命令；Bell 实际 wait 接触1/格挡1。上升107步有107次原生回调，完整 deck 支撑、0 damage，原 native quiet ride 门成立。不是每个 Moth 攻击都有独立命中收据，也不宣称单面盾可挡双侧同时攻击。

随后 LowerBellRoundTrip 仍 FAIL：physics4667，常驻 `gear-pistons.02 / scrap-wanderer` Attack2 使HP2→1；盾朝另一侧 Active Bell。这是下一段交叉战斗问题，没有被局部升降台成功覆盖。

## 全部22次尝试

证据根 `G:/knight/验证证据/october13-stable-claw`，保留所有原始记录。6 strict PASS / 14原生FAIL / 2 buildFAIL。20次成功构建均0警告/错误；两次 CS0165 是声明流构建问题，不是行为RED。20个原生案无timeout、guards0、ExpectedFailure空；本轮FATAL0不关闭历史资源崩溃。

| 时间/phase | strict | engine exit/ms | 实际边界 |
| --- | --- | ---: | --- |
| 181154 impact | FAIL | 1/16058 | 错误foreign前提，非生产RED |
| 182200 impact | PASS | 0/16060 | 原完整支撑实际characterization |
| 182246 Main | FAIL | 1/91195 | 原甲板胶囊超边 |
| 182446 Explore | FAIL | 1/57483 | 未满足旧Telegraph门 |
| 183033 Main | FAIL | 1/91225 | Bell同台而旧near未调整 |
| 183232 Explore | FAIL | 1/57107 | 近Moth已Stunned，远Moth超距 |
| 184115 Main | FAIL | 1/93718 | Bell战斗完成，横廊跳失败 |
| 185458 Main | buildFAIL | 无engine | CS0165三错误 |
| 185526 Explore | buildFAIL | 无engine | 同一声明流问题 |
| 185718 Main | FAIL | 1/94679 | 实际side K后低顶棚落台失败 |
| 185921 Explore | FAIL | 1/58582 | 初始Align证据，未发K |
| 190950 keyboard | FAIL | 1/531 | 有效窗口行为RED |
| 191344 keyboard | PASS | 0/11921 | 17窗口合同及原键盘测试 |
| 191428 Explore | FAIL | 1/73972 | K-first成立，Moth等待伤害 |
| 193123 Explore | FAIL | 1/104540 | 升降台段成立，后续交叉伤害 |
| 193925 final keyboard | PASS | 0/11908 | 最终冻结键盘 |
| 194008 final impact | PASS | 0/16067 | 最终冻结支撑 |
| 194053 final Main | FAIL | 1/102005 | Bell成立，实体横廊仍失败 |
| 194304 final Explore | FAIL | 1/100483 | K/升降台成立，LowerBell失败 |
| 194521 final pre | FAIL | 0/160251 | 8 Handle ERROR，禁止翻绿 |
| 194835 final post | PASS | 0/202613 | 原75及owner8/原warning合同 |
| 195230 final models | PASS | 0/405 | 最终普通模型 |

最终7案14份源清单只有一个版本，4PASS/3FAIL。post独立核对75 enter/75 return，索引0–74各1、owner8 nodes1→1，PHASE_OK/TESTS_OK各1，CONTROL0、ERROR0，四条原warning/scope multiset一致；不抹去pre的8 Handle错误或历史FATAL/ShapeRID。

## 下一制作点：原子修正横廊入口，尚未实施

开发候选：只将 RightShelf 宽152→136、local centerX1652→1644，保留world左边2856、height24/材质/层，right3008→2992。当前JSON唯一明确顶面巡逻居民 `gear-pistons.07 / precision-heavy` home2924，巡逻端点2892/2956、半径28，完整包络[2864,2984]；新平台两端各留8。不能缩32，否则会切掉8px居民完整支撑。当前JSON不移动；旧Gear候选home2932的边界另验，不能冒充当前居民坐标。

必须同批完成：

1. `CanyonContentBuilder` RightShelf、保存 `CanyonBasin.tscn` 独立bu6gd形状、body/skin中心、SkinSize和ReadableEdge±68。左Shelf独立ux7vx保持不动。
2. 从实际几何重新生成父场景嵌入minimap，不能只改base64内一个solid。当前schema1/143 solids/12440 boundaries，RightShelf index48；新边界/Revision应以真实builder结果为准，不把只读推算当生成结果。
3. 更新精确geometry合同及Gear候选完整支撑边界，旧 `<2988` 不再安全；新玩家full-center[2876,2972]。原同步器会拒绝已存在不一致节点，不能当通用覆盖器。
4. 真实保存 Player 普通单跳+1、Dash/Cast不增、HP不变、零恢复、完整Gallery支撑；再跑居民census/gear-alcove/Main/Explore。左Shelf、Gallery/fragment/anchor及原90/300/900/210预算不动。

这是可验证的最小候选，不是已经可通行。随后处理探索交叉火力与资源稳定性；正式美术、完整动作/UI/演出、目标硬件和用户体验仍在原十一项内。

## 审查、保管与留档

唯一fresh review C0/I0/新M1，根维持Minor延期，无C/I修复或复审。新M1：all-source guard after-sample日志混用await前danger与await后演员时刻，takeover source仍写Bell；实际控制用正确guardPoint，但不能据单条日志复原完整因果。继承14+新1共15类Minor OPEN。[全部24项根裁决及代价](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v133-final-review.md)。

[独立保管审计ENDED](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v133-custody.md)：108清单对、11640选中实体、120封存binary匹配；当前Source1852/C#892和Debug6匹配最终models。相对v132仅Main/LandingImpact/KeyboardTarget/Basin四测试CS及canonical变化，生产Resolver/Traversal/Layer/Visual/音频/地图/人口/数值/HEAD/branch/index未漂移。LandingImpact旧实体未被基线选择保存，只有清单行；不伪造逐字节旧实体diff，也不归为本轮新增缺失。旧14份cache-helper缺口继续OPEN。

所有根构建/原生/批次和审计命令均结束后才写本记录。PID8760仍0线程/0句柄空镜像残留，未kill/attach；无关UE进程及复用PID不当KNIGHT运行身份。没有真实存档语义读取。

本goal turn只执行一次手动媒体Check，UTC12:09:42.6428968，Current1431609/Baseline3508/Growth1428101/16文件/Notifyfalse；无新增媒体，不重置基线、不恢复旧归档或自动化。原十一项完整目标ACTIVE，未交付、未获视觉/真人认可。
