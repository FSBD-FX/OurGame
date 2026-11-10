# v111：冰区落台制动与机关伤害归因

2026-10-25，KNIGHT，`G:/knight/Knight`。原十一项目标仍ACTIVE，不是完整试玩版/正式交付。用户“没看到窗口”仍为NOTSEEN，不追认教学遮挡或画面；本轮无前台窗口、系统键鼠输入、图稿或录像。

## 事实

### 1. 来源不明的一击来自真实右侧蒸汽，不是已释放的敌人

v110 Explore的physics2056、HP3→2、source86805318458原先只打印source:null。实际记录器只登记Hitbox/CharacterBody2D/PlayerMotor/RangedWeaponProjectile，没登记Area2D形式的CanyonSteamJet；有效但被类型过滤、无效native、错误祖先链当时无法区分。

本轮先仅在原有accepted-event fallback增加脱离native的标量：cacheHit、nativeValid、type/path、inTree、queued、levelAncestor，不增加InstanceFromId次数、不先改登记或伤害。075429案确认同ID是有效、在树、未排队释放、属于本Level的 `SteamDiverterFrozen/RightJet`，确切运行类型CanyonSteamJet。这不是由“向上180冲量看起来像蒸汽”推测出的身份。

然后把原来只在终点执行的zero-attribution-gaps检查也放进已有每帧健康检查，仍优先保留原callback/recovery异常。075816案实际得到 `TASK10_DAMAGE_ATTRIBUTION_RED gaps=1 rows=3`，发生在以后死亡之前。之后才明确增加CanyonSteamJet登记及mechanism归属，不放行任意Area2D/未知来源；原非零source/attack、真实HP下降、远程实际shot证明保持。

080030案同右喷口真实HP3→2已经attributed=true/cacheHit=true，最终5行伤害、0缺口；但仍在2860因Moth/E0死亡，整个案例仍FAIL。提前失败的RED不替代此前完整死亡事实，也不冒充整关修复。

### 2. 原落台策略在松D后惯性擦入喷口

原连续Explore在frozen-upper后直接用Task4ReachSupport走向FrozenRightCatch中心X1750，接近8px才松D。新增局部passive observer在Motor之后采样，最多32条，仅记录入口/输入释放/喷口状态/交叠/目标接触及实际来源ID扣血；不改输入、不增物理帧、不做假设碰撞查询。

080912案的真实RED：

- 入段X1383.033、Y2143.933、V0、HP3。
- 首次采样松D：X1746.737、Y2405.1665、Vx253.1583，喷口活动。
- 7个物理更新后：中立输入、X1773.3217、Vx210.05055，玩家右缘1793.3217已经跨过喷口左缘1792。
- 随后实际同右喷口damage1，HP3→2；原助手最终返回X1813.422、Y2603.933。
- 10/32行、无遗漏/观察故障，明确 `FROZEN_DROP_STEAM_RED`。所以不是持续按D穿过，而是松键后的被动惯性仍足以造成接触。

### 3. 在未改地图、陷阱和物理的情况下使用实际反向键制动

仅改纯键盘路线消费者，用当前Motor模块和MotionSettings的现有速度积分/Reverse预测选择A/D，不写位置/速度/HP、不使用新跳跃、短喷或抓爪、不切换或关闭蒸汽。预测只选择输入，真实碰撞与完整胶囊落台才是验收依据。

首个方案仅选目标台左侧安全空间，172845案停在X1675.094、Y2363.933的中间平台，300帧内没到原目标，且后来另损1HP；严格FAIL保留。实际保存的FrozenUpperResidentShelf横跨1316..1684、顶2400，解释了为什么“安全停下”并不等于完成垂直转移。

第二候选把完整越过中间平台右缘也纳入同一个安全区：左边界取目标完整支撑与中间台完整退出的较大值，右边界取目标完整支撑与右喷口左缘的较小值。实际为1704.08..1771.92、候选中点1738；两次仍前进的输入交接预测加有限Reverse算术决定何时制动，不写死新等待时长。

173334案局部通过：109/300帧，实际目标原生接触、完整横向支撑、三个连续停止/零轴样本，最终(1724.7812,2603.9333)、V0、HP3→3、右喷口伤害0、无新增Space/Shift/K。9条观察无遗漏/故障，机制仍选Right且未交接；没有把forecast当运动保证。

`reverse_commands=6`仅为发出命令数，不是六次已消费采样。见下方延期Minor；本案不能独立认证输入延迟上界。非键盘分支仍委托原助手，但外围新增观察/HP/预算检查，不能称非键盘全部行为已复验。

### 4. 后续真实失败仍保留

173334 Explore在这个局部无伤段之后，2169仍发生PlayerMotor正常落伤3→2，位于FrozenExit，未回血或豁免。随后CastAt在issue2317→sample2319期间，计划Moth而实际生产键盘选择AbyssBrakeLeft4；一K、同octant的证据保留，不修改排序、不重复resolver来授权，也不称探索已通。

最终Main仍在7115被原wave Burst打至0，死亡后回保存Basin检查点，触发原无重置位移断言；不是独立于死亡的无故传送。5条伤害均有归因。最终pre在78024ms非超时原生FATAL，栈为GodotObject.Dispose/Finalize和mono_object_disposed_baseref；普通构建无REF，未识别具体资源，前一条Supply完成日志不能用来归属崩溃对象。v110的限定持有修复不等于全部资源故障修复。

## 推理与未采用的改动

对v110主线原日志另作只读核查：盾的24能量/秒和三次J的各12成本解释了耗能；没有证据证明24帧冷却压住一个同时满足范围、原完整安全余量和native攻击许可的窗口。`.4333333`与`.43333334`也不自动构成float错误：对应Rust处理边界只有25个完整非Active节点更新，不能向上取整/加epsilon冒充26个安全tick。

K6829确实命中并眩晕原Burst，但其他Rust/常驻Burst以及该同Burst真实开火的既有演练要求仍独立约束J。Rust确实发布26×18 Rectangle，而当前候选观察只借用Moth Circle；补Rectangle元数据并不等于未来运动/碰撞安全证明。本轮没有改这些时间门、资源数值、威胁成员或排序，也没有把矩形按保存尺寸估算的距离当当前原生碰撞保证。

## 全部十个原生案例

证据根 `G:/knight/验证证据/october13-stable-claw/`。十次构建exit0、十次原生均非超时。严格2PASS/8FAIL；两个局部RED→GREEN不改变案例最终严格结果。最终同源五案仍只有post/models通过。

| 目录 | 构建ms | 原生exit / ms | 严格结果 |
| --- | ---: | ---: | --- |
| `thruster-route-exploration-diagnostic-20261025-075429-437-cc1df54ccaa04f2a9ae5f667b97de661` | 31617 | 1 / 63357 | FAIL，来源补证/原死亡 |
| `thruster-route-exploration-diagnostic-20261025-075816-211-a0a08a308c22432197082bc4d584941c` | 25537 | 1 / 43964 | FAIL，归因检查实际RED |
| `thruster-route-exploration-diagnostic-20261025-080030-739-20428f9ca6b34be4acee6b0ab78a72fa` | 28486 | 1 / 55262 | FAIL，归因已闭合/仍死亡 |
| `thruster-route-exploration-diagnostic-20261025-080912-244-2616040045624bdfafea790987eb6396` | 27776 | 1 / 40588 | FAIL，原惯性蒸汽接触RED |
| `thruster-route-exploration-diagnostic-20261025-172845-288-e1c6ef433538430ba33e8847dce13165` | 24101 | 1 / 43372 | FAIL，首方案停在中间平台 |
| `thruster-route-exploration-diagnostic-20261025-173334-955-2d9f2c1168f54b369960ef7670e1ab49` | 20498 | 1 / 44271 | FAIL，局部落台通过/后续K错选 |
| `thruster-route-main-diagnostic-20261025-173747-911-b62b25c9d6fe47db88eecb4ee85f49da` | 24285 | 1 / 134636 | FAIL，原7115死亡/恢复位移 |
| `prologue-pre-20261025-174033-627-fc7472f731d041d7bb32ecd1e8411f0b` | 22690 | -1073741795 / 78024 | FAIL，原生finalizer FATAL |
| `prologue-post-20261025-174220-614-4b44e6355cda471cb9c3e33f571685bd` | 25876 | 0 / 199141 | PASS，原75及owner8次 |
| `models-20261025-174611-595-55c55dcd474d47a1b37c6ecc72cae8d7` | 23206 | 0 / 375 | PASS，普通Debug恢复 |

post原75 ENTER/75 RETURN、index0–74、retired0、同owner8次完整；seamless1/room-profile1/flow-registry2为三预期负例scope，无新增豁免。stderr/external各四条是同一批警告的两个副本；pre的两个sink也不是两个独立FATAL。post199141ms在既有300000ms时限内，不是性能认证。

## 风险、单次审查与全部裁决

一次全新只读增量审查C0/I0/Minor1，无第二审查/修复回合。Minor延期：32条trace没有专门保留首个已采样A边沿；命令行仍显示上一帧D，下一留档转换已是中立负速度。应以后补一个首反向采样/sequence，不把命令次数称已消费次数。本次实际停止/支撑/HP判定仍有效，不扩大为时延证明。

审查未认证范围由root逐项裁决，代价明确保留：

1. Explore仍以Moth→墙的真实K差异FAIL；未修排序。误判会掩盖目标切换。
2. FrozenExit2169落伤保留，局部无伤不推广到整个冰区；未证明它相对旧轨迹的因果变化。误判会漏掉真实落伤。
3. Main死亡/资源耗尽仍FAIL；不改冷却/epsilon/资源。误判会把保守观察或机器策略问题变成难度偷改。
4. 最终Main/pre/post/models依本轮结束收据分别FAIL/FAIL/PASS/PASS，不从review或旧绿补齐。误判会拼接不存在的绿色版本。
5. pre FATAL及跨manager首次缓存/finalizer交错继续OPEN。误判会把仍崩溃的版本交付。
6. W实际reader与preview、首物理遮挡/receiver资格仍原义务；本轮不授权第二resolver或新排序。误判会掩盖穿墙或输入采样变化。
7. Intro/Playing及原callback/deferred actor/parent/pose/sequence不由此续签。误判会绕过准备门或生命周期错误。
8. 全局LoadLevel主/清理双异常义务未改；只查了新局部观察器清理。误判会遗漏旧节点/丢主异常。
9. 非键盘虽仍调用原助手，外围新检查未在非键盘路线实跑；不宣称兼容性全通过。当前获批主操作是纯键盘，未改变生产鼠标能力。误判会造成旧诊断入口提前失败。
10. 算术是当前保存几何/模块的候选，不是任意帧率/模块/外力保证；原生接触否决仍有效。误判会把预测当物理事实。
11. 实际HP/来源及交叠转换不是连续扫掠证明，也不证明无敌帧期间没有接触。误判会高估通用避险能力。
12. 原十一项、76普通＋Boss/NPC、正式视听/相机及发行仍ACTIVE；本轮只有测试消费者改动，没有新正式游戏美术/UI/音效。误判会把验证进展冒充完整游戏。
13. headless InputMap不等系统键盘/真人硬件/正常速度观感；NOTSEEN保持。误判会错认用户已看到或认可画面。
14. root独立完成源/封存二进制/保护清单/HEAD/index审计，不由review单文件SHA代替；其他历史dirty变动未被续签。误判会混入未审版本或保护状态变化。

## 保管与继续入口

独立审计十案50/50前后清单对、60/60封存二进制SHA，缺失/漂移/额外/格式错0。最终五案source清单相同，1844项、SHA `EAEECD2FDFDF9AF5348A9C4492C82D9EC419539E6D9E4E53B3CBA39B959CC129`；live1844/1844与普通Debug6/6匹配，C#892。相对v110仅 `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs` 改变：A9E8CDE44EC159ECAE314A2E289ADCBACC993E5FB469050BC525D03C70108FC6→69E810D181D76FF2E48D70B2CC481697D830692983523D4B1A365787CB2185D0，无新增/删除源文件、帮助器或生产资源变化。

canonical owner仍309DD6C4…1255F2，分支codex/task6-certified-final-20260902T0555、HEADe7c70498…、index21610FDC…、音频CAD39DF…不变。所有root句柄和源审计结束、工程Godot/dotnet进程0后才更新文档；没有Git写入/标签/Release、删除、付费、自动化或真实存档语义读取/写入。v111仅开发记录号。

仅执行既有媒体Check一次，UTC2026-10-25T09:49:03.5716917：Current860284/Baseline3508/Growth856776 bytes，Notify=false，新媒体0。不新建后台提醒，也不停工。

文档更新后diff-check exit0，既有LF→CRLF提示169/其他0；当前测试源69E810D1…、owner309DD6C4…、Git index21610FDC…再次匹配，没有再改候选代码或启动原生。

下一优先核对新的Frozen K issue→reader/controller边界和计划/目标变化，保留原真实octant、一次K及物理首命中规则；并继续资源首次取用/finalizer归因。不猜排序、不增加重试、不删敌人或伤害、不降低完整十一项交付要求。正常双路、正式视听/相机、目标硬件、真人与版本交付仍须全部实证完成。
