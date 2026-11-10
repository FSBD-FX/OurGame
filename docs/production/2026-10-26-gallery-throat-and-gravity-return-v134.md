# v134 · 横廊实体通路与自然下落返程

2026-10-26，KNIGHT，`G:/knight/Knight`。原十一项完整目标继续 ACTIVE，没有重建目标、自动化或切换UE。上一轮属于实际进展：定位了不能容纳真实胶囊的入口；本轮修改正式地图并完成其往返验证，不是只有测试记录变化。

## 事实、推理、风险

事实：正式 RightShelf 只收短右端16像素；碰撞、可见边缘和父场景小地图同步。真实保存Player在旧地形单跳失败，在新地形单跳成功；返程使用真实A/D制动和重力，在专项90/109、完整Main与Explore均89/109内落到PistonTwo并停稳，生命不变。最终Main已通过三段上行Gallery，后续counterweight-lift战斗仍失败；Explore在后续LowerBell交叉攻击中HP2→1，仍失败。

推理：当前解决的是一处真实实体通路及其自动试玩返程。前后段本次通过不证明历史资源错误的根因已修复；局部往返也不等于完整序章或正常速度的视觉质量通过。

风险：完整双路线仍未通；正式美术/动作/UI/声音/CG、目标硬件与真人试玩仍未完成。新地形改变catalog revision，旧小地图探索会按原规则失效；核心进度由独立兼容路径保护，未制作本次精确跨版地图迁移。用户最后显示结论仍NOTSEEN，没有新窗口、系统键鼠、付费或外部发布。

## 正式地图变更

- 原roof角(3008,3744)与Gallery角(3040,3756)间距约34.176，小于保存Player直径40。[202439实际RED](../../../验证证据/october13-stable-claw/thruster-impact-support-20261026-202439-470-da59e3e3128545118949f6309916e629/engine.wait.json)显示单跳0→1、Dash0，原300帧耗尽，同时接触两块地形而不能落台。
- RightShelf世界Rect由(2856,3720,152,24)改为(2856,3720,136,24)；左边、高度、材质及碰撞层不变。local中心1652→1644、SkinSize宽136、ReadableEdge ±68。
- 当前JSON的precision-heavy home2924、端点2892/2956、半径28，完整巡逻包络[2864,2984]，新平台左右各留8像素；JSON及数量未改。没有使用旧候选home2932替换当前居民依据。
- 生成器、原生保存/reload后才晋升对应5个属性值；Basin全部371个node/ext/sub_resource头保持一致。父场景逐字节仅替换完整生成的SerializedValues一行，没有改其他行或换行。
- 新catalog实际为143 solids、12436 boundaries、354024 bytes；来自修正Basin和另外四个当前正式叶，Revision与边界整表再生，不是手改base64局部。正式父场景仍引用原五个canonical路径。
- 旧203308阶段生成和原父/叶合同通过，但因正式场景尚未晋升，最终正确FAIL；没有将stage marker当整phase通过。晋升后204226完整resident-parent通过，包括实际canonical、catalog、原三检查点及原迁移合同。
- [204136 GREEN](../../../验证证据/october13-stable-claw/thruster-impact-support-20261026-204136-823-c92c2c6569d941859bbbe878d89d6d7c/engine.wait.json)：实际普通单跳落到Gallery，未用K/喷射，生命不变。原移动支撑案例全部保留。

没有缩小角色、增加跳力、扩大抓取/射程、移走Gallery/残片/锚点、减怪或削弱伤害；原90/300/900/210预算不变。

## 同轮返程问题与一次修复

完整Main204917及Explore205124都实际上到Gallery，但旧ReturnAcrossResidentShelf仍跳起绕到整块roof左外侧，反向制动后错过PistonTwo，109帧结束在约(2823.998,3960.388)，不能把专项上行绿当完整往返绿。

唯一fresh review原结论C0/I0/M1。根随后按实际失败新增自己的Important I1，纳入一次修复pass，不伪称审查员发现或再开审查：

1. 先将原算法机械抽到共享测试helper，实际路线也调用它；在真实Gallery抵达X3079.8955的独立初始出生fixture复现。[210330 RED](../../../验证证据/october13-stable-claw/thruster-impact-support-20261026-210330-110-2f6b15da60f44b04b92bfe9ce559b8d1/engine.wait.json)仍在原109帧越过落台左侧，原跳跃1，native错误非编译失败。
2. 仅更换返程驾驶：真实A/D低速靠近新喉口，D反向制动，利用实际Gallery侧壁约束横向移动，靠重力下降到具名PistonTwo；全程不写position/velocity，不发新J/K/Shift，不改HP/残片。
3. 保留原109、完整胶囊/真实floor接触、同Player/HP、空闲爪、零额外cast/dash和原碎片状态门。raised-return的真实普通跳数从1变0；总返回值也按实际0记账，旧direct-anchor 60+45分支没有扩时。
4. [210755 GREEN](../../../验证证据/october13-stable-claw/thruster-impact-support-20261026-210755-011-bdeab1ecc0504bada3bb16d4cb0cc147/engine.wait.json)：90/109，(3019.924,3799.9937)，完整PistonTwo、速度0、HP5保持，jump/dash/cast增量均0；原上行单跳仍通过。
5. 最终实际Main与Explore共用同helper，均89/109，分别HP4→4、HP2→2，碎片不变。I1在真实路线里的行为已闭合，不是fixture-only。整路后续失败仍保留。

## 最终路线与全回归

[Main211106](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-211106-842-2e6e60c85d5d4046a26e940392d29341/engine.wait.json)：返程成功；齿轮Bell J7/K3、HP4保持；普通跳上GalleryOne，继续GalleryTwo/Three。后续counterweight-lift组仍active/batch1/live2，原战斗900内未完成，攻击4次、能量耗低并发生受伤，严格FAIL。没有削弱敌人或加回能量解决。

[Explore211331](../../../验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-211331-233-6a6cd0a0c3014579aed6a3d6dc65c88d/engine.wait.json)：新返程成功；后续LowerBell原900帧战斗被交叉攻击，HP2→1，严格FAIL。下一步应查这两段真实威胁、站位与技能衔接，不把后续失败倒归为喉口仍未通。

17案全部原生，10 strict PASS/7 FAIL，0 buildFAIL；17构建0错误/警告、stderr空，ExpectedFailure空、guards0、无timeout/FATAL，不覆盖历史故障。

| 时间/phase | strict | engine/ms |
| --- | --- | ---: |
| 202439 impact旧喉口RED | FAIL | 1/21430 |
| 203308 parent尚未晋升 | FAIL | 1/11545 |
| 204136 impact | PASS | 0/16904 |
| 204226 parent | PASS | 0/13108 |
| 204535 census | PASS | 0/73844 |
| 204720 gear | PASS | 0/10145 |
| 204802 structure | PASS | 0/1352 |
| 204835 save | PASS | 0/12371 |
| 204917 Main旧return | FAIL | 1/91172 |
| 205124 Explore旧return | FAIL | 1/88728 |
| 210330 return RED | FAIL | 1/18853 |
| 210755 impact GREEN | PASS | 0/18488 |
| 211106 final Main | FAIL | 1/115823 |
| 211331 final Explore | FAIL | 1/98158 |
| 211539 pre | PASS | 0/155209 |
| 211843 post | PASS | 0/211447 |
| 212253 models | PASS | 0/496 |

最终post原75 enter/return、索引0–74各一次、owner8 nodes1→1、PHASE_OK/TESTS_OK各1、CONTROL0，ERROR0，四条原warning/scope合同一致。前后段quiet不关闭历史Handle/FATAL/ShapeRID。前面的census和Gear专项是受控无玩家目标/无机关敌人组合的验证，不冒称全部居民自然实战：50身份真实ready/720步、41闭合巡逻/9固定炮台，以及右棚单跳访问/步行返回HP5保持。

## 存档与小地图边界

现有`ExplorationMapSaveCodec`只接受同TerrainRevision，`ExplorationMapModel.Restore`在不匹配时清空可选探索位图。实际204226的原MinimapCatalogTests包含“几何改变只丢弃可选地图、核心进度和磁盘原字节不变”的隔离合同；不是读取或改写真实用户档案。

因此不能泛称“旧小地图探索跨版完整兼容”。本次catalog变化会触发该旧规则；精确v133→v134的保守地图探索迁移尚未实现，列入P06/F03/F04的交付前待办。不能直接按旧boundary索引复制，旧12440到新12436的编号发生变化；仍须保持未知区域不被提前揭示。核心进度兼容专项通过，不代表这项迁移完成。

## 美术盘点与完整目标

[现有素材盘点](../art/2026-10-26-existing-art-reuse-v134.md)：`assets`仅13张PNG，独立批准且正常Player实际接入的原图为灯、半钟刀与两握持片4张，已经在用。其余场景/怪物/Boss/角色主体/VFX多是原型色板、平涂图集或程序绘制，不是另有未启用的成熟绘画包；UI是矢量/文字。没有本轮新美术或付费生成。

后续继续原十一项，不把“全部资源final:true”当最终视觉批准。正常速度、遮挡、整体画风、动作/音频/UI/CG、目标硬件和真人体验仍OPEN/NOTSEEN。

## 审查与保管

[唯一审查和全部根裁决](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v134-final-review.md)：独立C0/I0/M1，根新增I1并有效RED→GREEN及实际双路线局部通过；没有第二审查。M1仅暂存外观允许字段未来覆盖不足，延期；继承15+本轮1=16类Minor OPEN。

[有界审计ENDED](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v134-custody.md)：仅最终5案+最后focused GREEN六案独立实体哈希，30对/3048实体/36binary匹配；当前Source1852/C#892/Debug6一致，其余11案只核原始provenance/wait/build/log，不冒称全17案实体独立审计。相对v133正好6C#和2scene变化，没有source增删或JSON/物理/AI/energy/save代码漂移。

所有根owner/审计结束后才写本记录。媒体本goal工作段仅一次Check，UTC13:36:47.5673212，1431609 bytes/16文件、Notifyfalse；无新增媒体，不重复初始化。无自动化、前台/OS输入、付费、Git写入、删除、安装或真实存档语义读取。完整目标仍ACTIVE，未交付。
