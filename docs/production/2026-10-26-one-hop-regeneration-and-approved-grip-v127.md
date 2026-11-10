# v127 · 单跳自然回能、获批握持遮挡与实际显示边界

2026-10-26。KNIGHT，`G:/knight/Knight`。原批准十一项、全部稳定待办ID、76普通敌人＋Boss/NPC、地图机关和正式视听交付要求不缩减；目标继续ACTIVE。没有完成整关或精美成品交付，不涉及另一个UE项目。

## 事实、推理、风险

- 事实：获批右握持遮挡原PNG已进入正常保存Player的真实Weapon绘制层；保存Player的36组绑定检查、原武器观察器和原三项爪表现通过。自动试玩首次完成一个真实Space单跳、同地面落稳和松L自然回能至24，HP保持1；随后整主线仍被幽灵俯冲击杀。探索仍在下一抓点准备时死亡。
- 推理：新增观察器原先要求三发子弹全部飞越角色，与生产中“常驻炮台失去视线即撤销攻击”的既有合同不符。实测只有一发越过，另两发在源Idle/0的同tick撤销；必须分别记账，不能把撤销改称穿越，也不能为通过观察器修改生产AI。
- 风险：绑定/几何不是正常速度美术或相机验收。用户本次再次明确“没有看到游戏画面”，以NOTSEEN为准。炮台专项退出仍泄漏15个Shape RID；普通前后段本轮通过，不关闭历史GCHandle/Finalizer FATAL。四个旧纯合同harness也实际失败，不能称全套验证全绿。

上一游戏轮的缓存反证为PROGRESS；中间修复资料展示页不计游戏进展。本轮继续原计划/BASE/脏分支，没有重建目标、task-done、Git写入、发布、自动化、付费生成、系统键鼠或资料删除。

## 已进入运行时的正式单件

来源：`G:/knight/美术源文件/迁移留存/2026-10-24_complete-layered-001_未发布/layers/right-grip-overlay.png`，沿用[v096批准链](2026-10-24-profile-use-serial-and-asset-provenance-v096.md)和同目录`evidence/weaponHands-manifest.json`。独立核对三张武器评审图对应批准SHA；身体/披风/肩部的阶段manifest自洽不等于批准链闭合，未接入这些缺链部件或被拒总装。

- 原件与运行文件`assets/player/thread-spool-bearer/right-grip-overlay.png`的SHA256均为`AA83FADEF3D3DE26B3D004D7353D959FB4ED26CAE10C76CCC68A807ADAC37BD4`。原画布1402×1122，非零alpha边界为(810,777,49,45)；未改像素、裁写原图或重新生成。
- `PlayerFormalLayer.WeaponGripTexture`为替换接口；真实Draw在刀之后画握持遮挡，保留原PNG，仅使用区域绘制。与刀共用(835,798)主握点、25/287等比缩放及同一个实际NearArm挂点，根朝向只镜像一次，不新增rig slot，不改姿态/物理/伤害所有权。
- 实际缺绑定RED：115127，native1/718ms；实现后115521 native0/4489ms，最终同版120901 native0/4579ms。原36个武器状态/进度/左右样本、Draw-only观察器反例与三项原机械爪案例保留，新增36个遮挡层几何样本。样本的非None动作仍是Burst阶段，不冒称覆盖完整前摇/Contact/Recovery、非零瞄准或双手约束。
- 一次正常导入退出0，新增仅PNG/import/ctex/md5四项，source1844→1848，C#仍892；未新增UID或重写音频。纹理内存和正常显示性能没有新测量。

## 齿轮战斗与冷却：真实改善和仍未通过之处

只修改自动试玩消费者，生产移动/能量/攻击/敌人/地图数值未改。保持原900总循环、全来源危险扫描、J完整窗口＋采样/回盾裕量、24能量门、HP与恢复门、既有K/短喷和通用ledger。合法动作通过原键盘驱动，事件回调只观察，不发送输入。

1. 实际锁向前绑定固定ID炮台，区分攻击周期、零基计划发射区间、真实伤害ordinal和具体投射物。第一版在同tick尚未确认的Space误判；修正只允许命令所在同physics的未确认样本，下一tick必须真实RawJumpPressed及AcceptedJump＋1，不能重按或扩预算。
2. 后续实际Retired端点证明：源在同actor/physics转Idle/0，原常驻观察LOSfalse，两弹在完整胶囊前撤销。`CancelResidentAttack`先置Idle再Revoke、随后FrameChanged发布；既有五项锁向遮挡行为检查仍成立。Retired本身没有取消原因字段，因此将LOS归因保留为生产路径与同tick事实对应，不假装事件直接报告原因。
3. 终态明确分开Passed和RetiredUnpassed，不能两者同时成立。后者必须有实际原生端点、同physics/input sequence/未变胶囊、仍存活的源与玩家、回调内Idle和同tick发布Idle，并保持原生HP不变；裸Retired不通过。撤销不计passed，不补造仍活着的危险叶。独立八条分辨合同先native RED396ms，再GREEN1804ms。
4. 最终Main115834：真实起跳16（仅增加一次）、7268同完整地面落稳，7296自然E24.00006、HP仍1；69个实际样本、一条raw Space、stage5/fault0，三条真shot身份完整，结果为passed1/retired-unpassed2。没有治疗、资源赠送、二段跳或第二次尝试。
5. 整Main仍FAIL：7377被常驻`.counterweight-lift.03` ghost-diver的AttackHitbox/attack3击杀。7297–7376的80条J决策全部先在0x2安全窗门拒绝；交回时共同窗.2833332秒，小于原约.433333秒要求，此后Rust Active/Burst弹/Moth Dive持续压缩窗口。重新举L耗尽能量，没有新J/K。不能把未执行的后续门也记成失败，或声称已证明存在合法K机会。

实施裁决及代价：撤回的是新增“三发必须全部穿越”的观察假设，不是用户要求或原战斗门；代价是本轮不再提供三发全穿越/扫掠安全证明。保留生产撤销和真实HP权威；代价是不能从静态状态推导普遍安全策略。暂不追加未验证的第四种战术或修改难度；下一先记录7297交回后的真实键盘目标/LOS和可否打断主导压力，代价是整战仍未闭合。

## 本轮17案

下列链接为各自原始终态，不拼不同冻结阶段。全部ExpectedFailure空、guards0、无超时；17次build均0warning/error、无特殊Defines。8严格PASS、9严格FAIL；其中3个为真实TDD RED。全部FATAL0不代表历史崩溃已修复。

| 实际case | strict | native exit/ms |
|---|---|---:|
| [110947 · trigger RED](../../../验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-110947-536-1af1cc0d88bb4dc3a9398f82c0c3785b/engine.wait.json) | FAIL | 1/406 |
| [111840 · trigger GREEN](../../../验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-111840-758-1b5536e99e374ef59250ba9dd7752d4c/engine.wait.json) | PASS | 0/1802 |
| [111910 · Main首次边沿](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-111910-123-2369aa78f07c4202a9a3fc20b2ed431d/engine.wait.json) | FAIL | 1/130724 |
| [112540 · Main退休观察](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-112540-152-1e10a4973e1d46e38af594e396beea9d/engine.wait.json) | FAIL | 1/130347 |
| [113436 · Main真实端点](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-113436-079-9d0bb0ee7738451fb4d8f6dc6939fc2e/engine.wait.json) | FAIL | 1/129338 |
| [115127 · 握持绑定RED](../../../验证证据/october13-stable-claw/thruster-formal-claw-phases-20261026-115127-668-e25a3018e0da4c0aba81ec4c1c6a04d0/engine.wait.json) | FAIL | 1/718 |
| [115521 · 握持初次GREEN](../../../验证证据/october13-stable-claw/thruster-formal-claw-phases-20261026-115521-666-2ac6554b00d348cd87dc2e785dec04d2/engine.wait.json) | PASS | 0/4489 |
| [115555 · 终态区分RED](../../../验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-115555-641-03af28e03bdc4722bac770dd4c96b282/engine.wait.json) | FAIL | 1/396 |
| [115803 · 终态区分GREEN](../../../验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-115803-915-181a67cf13144d7a8035417c31997060/engine.wait.json) | PASS | 0/1804 |
| [115834 · Main单跳完成后仍死](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-115834-937-0fa4893f19e14ac0bf22bbb465e2c255/engine.wait.json) | FAIL | 1/134332 |
| [120901 · 最终握持/原爪](../../../验证证据/october13-stable-claw/thruster-formal-claw-phases-20261026-120901-208-291bb1b9840344f3aa8c4032326a4a50/engine.wait.json) | PASS | 0/4579 |
| [120934 · 锁向遮挡/15 Shape RID](../../../验证证据/october13-stable-claw/resident-turret-locked-wall-20261026-120934-987-b662b865c318428eb1270410b20c4d2f/engine.wait.json) | FAIL | 0/19666 |
| [121021 · Explore](../../../验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-121021-442-35eee4653a9d4d18988ceb3a17a9b43c/engine.wait.json) | FAIL | 1/158542 |
| [121333 · 普通pre](../../../验证证据/october13-stable-claw/prologue-pre-20261026-121333-067-ae919646462642708b2fd82b53c0a4f0/engine.wait.json) | PASS | 0/154523 |
| [121635 · 普通post](../../../验证证据/october13-stable-claw/prologue-post-20261026-121635-437-613fa255311b489982d76e0a4d9abbad/engine.wait.json) | PASS | 0/196904 |
| [122026 · 普通models](../../../验证证据/october13-stable-claw/models-20261026-122026-082-05dd8c76aee943d29fd0060017406a77/engine.wait.json) | PASS | 0/501 |
| [122150 · 授权单次显示](../../../验证证据/october13-stable-claw/thruster-approved-lantern-visible-20261026-122150-482-8d6bd4c9340d43209e67f90e39b048ed/engine.wait.json) | PASS/NOTSEEN | 0/26108 |

turret五个实际行为案例failures0，但stdout明确15个`P12GodotShape2D`退出泄漏，不能用engine0或marker改判。其他八个严格FAIL各stderr一条断言ERROR（external镜像，不算第二个故障）。仅post的WARNING为0/4/4，三类既有scope精确匹配；原75逐项enter/return、owner8次nodes1→1、PHASE_OK/TESTS_OK各1、CONTROL0。

Explore本案8546 ghost-diver造成HP2→1，9045 bone-wanderer`.counterweight-lift.01`在Grip2准备首帧造成1→0；此前Top与返程通过不覆盖后续死亡。当前旧原生资源FATAL、原B01–B05和完整正常双路资格继续OPEN。

### 纯合同并非全绿

11个既有无必填参数脚本实际7exit0/4exit1：失败为`test-exit-verbose-admission.ps1`、`test-full-post-budget-v098.ps1`、`test-level-cache-admission-v001.ps1`（69/72）和`test-sample-boundary-admission-v076.ps1`（46/48）。当前canonical在`$projectRoot`之前90242字符与物理v126/F649E712完全相同，本轮只改933之后的资源输入与最终marker，未改变这些入口。旧harness的位置/命令白名单假设暴露问题，未修、未删除负例或豁免；实际准入消费者仍需后续当前harness验证。

默认cache脚本只跑2个空证据反例，不冒称21；随后显式传入v126真实none-101628证据，21检查通过。Ignore/Deep仍无安静正例，不造成功资料。

## 一次审查与根裁决

唯一fresh `/root/v127_final_review`，gpt-6-astra/high，核对五份物理diff及当前hash，C0/I0/新M1。根按实际影响复评同意，无C/I修复pass、无复审。详见[审查处置](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v127-final-review.md)。

| 审查Declined事项 | 根裁决 | 错判成本 |
|---|---|---|
| 1.W目标排序 | 没有新W边界证据，不认证、不改排序 | 错误抓点仍可能存在 |
| 2.实体遮挡/receiver | 保留原resolver/LOS，无第二次查询授权K | 把观察当合法施放 |
| 3.Intro→Playing | 不借本两夹具认证所有启动/恢复 | 漏准备失败 |
| 4.一般回调/延迟退出 | 新hop已查，旧全体边界继续待验 | 旧回调可能越权 |
| 5.LoadLevel双异常保留 | 未改，旧义务不关闭 | 清理掩盖主故障 |
| 6.native资源寿命 | 本轮未修，历史FATAL保留 | 虚报稳定交付 |
| 7.完整Main/Explore | 两路本次均FAIL，不能通关认证 | 放过真实死亡 |
| 8.全部回归/稳定 | 以17案、四纯合同失败和原日志裁定 | 挑绿色或混版 |
| 9.撤销直接原因 | Idle/退休/既有LOS为同tick对应，不是原因事件 | 错归因 |
| 10.扫掠/三发全过 | 明确不声称；只有1过2撤销 | 假安全证明 |
| 11.普遍松L安全 | 只证明一次真实有限尝试 | 玩家别处仍受击 |
| 12.像素/手套可读/相机/真人 | 待机图不等正常速度；用户NOTSEEN | 交付不可见/不自然版本 |
| 13.全阶段/非零瞄准 | 36样本仍不覆盖，原v100缺口保留 | 收招/转场可能脱手 |
| 14.其他部件/另一只手 | 只接来源闭合的右握持单件 | 恢复被拒/未批总装 |
| 15.全部历史批准链 | 仅选定阶段证据；其他链未重审 | 冒认素材批准 |
| 16.全保管/真档/硬件/发行 | 保管另审；真档只核字节，硬件/发行不认证 | 混证据或越权发布 |

Final: minor (deferred): 八条shot终态Boolean合同未独立破坏实际的physics、input sequence和capsule新鲜度比较；现有比较存在且本案通过，但仍缺上游比较被移除时能抓住的负例。另八项继承Minor继续延期：energy-unwind早失败flush、boundary swept措辞、finally stage4摘要、fresh LateFact来源、rotated-AABB负例、withdrawal旧return诊断、unknownActive真实scanner负例、v126十二份cache-helper历史实体缺失。共9项，不声称已修；四个旧harness失败另列验证债务，不隐入“全通过”。

## 资源崩溃：只读上游核对改变下一实验

使用agent-reach/Exa核对官方来源，没有安装、下载落盘、升级或附加调试器。

- [运行提交csharp_script.cpp](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/csharp_script.cpp)与[ScriptManagerBridge.cs](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs)：弱→强切换、死目标和released handle的终结路径构成可核查候选链，但尚无本地Settings对应native调用现场，不能宣布同一缺陷。
- [官方问题83762](https://github.com/godotengine/godot/issues/83762)含同类Finalizer/released-handle FATAL；[112067](https://github.com/godotengine/godot/issues/112067)含FromIntPtr/SwapGCHandleForType缓存资源症状，报告类型不同且仍需验证。
- [关闭未合并的87045](https://github.com/godotengine/godot/pull/87045)只放宽检查，被维护者认为隐藏症状，不能据此关检查或修改生产引擎。

下一分段实验：在同源独立进程、原pending-finalizer门/旧包装器weak-dead前提下，A仅默认GD.Load并保持PackedScene，不Instantiate或读取两个子资源；B保持A前提并追加原工厂。核对旧serial的ENTER/RETURN，保留none对照。A即FATAL则加载足够；A完整而B FATAL则边界缩至后续实例化/取用；前提失败或未复现不归因。本轮仅确定下一动作，尚未实施，不推广GC/pin/cache绕行。工具版本只读检查提示Agent Reach1.6.0可用，未更新。

## 显示、留档和视觉判断

用户在回归结束后新授权约30秒显示。只运行一次已存在的可见入口夹具，Godot窗口标题/句柄存在、实际GPU头灯负控两朝向分别191/190变化像素、noise0，native26108ms；没有系统键鼠输入。该夹具固定角色并呈现左右Idle姿态，不验证自然移动或连续动作。用户明确没有看到，所以显示确认仍NOTSEEN；未核前台/最小化/屏幕位置，不擅自判为渲染坏了，也不自动重开。

两张全视口PNG原件和两张逐字节展示副本、索引/状态说明已先保存至[本轮媒体目录](../../../过程图稿与视频/2026-10-26_122225_握持遮挡与入口显示_v127/README.md)。头灯像素负控不是握持层像素负控。根实际检查截图：仍有明显程序占位感、场景大平面/留白、角色细节小、宽提示条抢眼，不能称精美完成；这是开发观察，不冒充用户新增设定或审美批准。

## 保管与容量

所有root native/batch先终态，独立只读`v118_custody_audit`最后db57e0 exit0/ENDED，无live handle/subsession，UTC04:34:46.0392606，之后才写本报告。

- 85/85 manifest字节对相同；8632选中实体副本＝source8450＋helper182，逐SHA/路径集匹配，无新增missing/extra/fault。102/102各案封存bin匹配。
- 34份user manifest同`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`，只核清单字节。helper联集12路径，canonical两版本，其余11单版本；历史v126缺12实体仍OPEN，未补造。
- 当前1848源/C#892、Debug6与最终models匹配；最终9案18份source manifest相同。四C#实际增412行、删0（Main324、Placement49、Layer14、VisualTests25）；canonical2+/1−；仅新增四资源项，无其他源增删。HEAD/branch/index/audio/Claw/Skitter/Traversal锁定值未漂移。

| models最终清单 | SHA256 |
|---|---|
| source | 0E467C6150637A47487DF073B223CD36A0C632AB58A3C525AE40E248E327D8E1 |
| helpers | E546D446C9E64B6B8FEC6F93014E1C076C893019BAFF2DFE311BCE2D4140A0CA |
| binaries | A921FB78061578E855B6239EE5CAC29D6BD355FFB64E38D33A52E5D07ECF0094 |

本轮一次手动容量检查UTC04:36:04.8054141：Current1431609、Baseline3508、Growth1428101字节，16files，Notify=false。没有独立通知、自动化、打包或删除；20GiB仍只随正常汇报提示、不停工。

下一步保留原十一项顺序：真实合法K打断/完整攻击窗口，资源加载与实例化分段，先核窗口显示通路再申请正常速度画面，继续正式素材链与场景/UI密度。完整正常双路、正式全部视听/相机、目标硬件/真人及授权版本交付均未完成。
