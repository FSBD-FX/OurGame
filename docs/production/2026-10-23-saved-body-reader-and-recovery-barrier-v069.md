# 原身体保存数据直读与实际死亡恢复 · v069

2026-10-23，Asia/Shanghai。内部制作记录，不是新试玩发行。仅 `G:/knight/Knight`，原十一项高质量完整交付目标保持 ACTIVE；不恢复自动化、不缩减人口/地图/机关/正式视听及验收要求。

## 事实、推理和风险

旧 `CaptureOriginalSpawns` 为取得原波次的身体预留，每次会创建并释放九种完整离树 AI；一次成功出生事务前后各检查一次。现改为每次从真实保存场景读取标量矩形，仅在该次检查内按场景路径复用标量。没有长期场景/形状缓存，不创建 Node，不新增 GC/保活/共享 Resource Dispose。第二次准入仍重新读取，经理的环境、玩家、已附着/pending 身体、原生支撑、走廊、离屏及授权检查未放宽。

这减少了无必要的构造和释放工作；**未测显示帧率，也没有证明历史 `gchandle.is_released()` 根因已修复**。本轮最终12案没有重现FATAL/未初始化Handle，但三案仍严格失败，四类历史RID失败本轮未重跑，继续OPEN。

保存数据不能静态执行任意脚本。此读取器只用于现有九种受检作者场景；真实原生对照仍调用旧 `TryBodyBounds` 全合同，以发现构造器改变物理结构的情况。额外非Area物理身体、未知/继承结构、placeholder、物理嵌套、脚本化身体/形状、非支持变换等拒绝；Area受击/攻击形状不误算身体。部分拒绝分支尚无专项反例（例如继承、嵌套覆盖/深度），不能声称任意脚本或所有畸形数据已获认证。

## 实际改动

- `scripts/prologue/residents/ResidentSavedBodyGeometry.cs`：严格 SceneState 数据读取；只支持一个直接 `BodyCollider` 的原生圆/胶囊/矩形，平移、有限正几何；递归确认视觉实例无物理结构，节点/深度预算及循环拒绝；借用的场景、状态、形状不Dispose。
- `scripts/prologue/residents/CanyonResidentManager.Spawn.cs`：仅替换原身体预留取得方式。26原始slot/10 alternate合计36个预留身份和 Counterweight Rust +4px并集保留。
- 新 `ResidentSavedBodyGeometryTests.cs`、`SavedBodyConstructorProbe.cs`：九种字面矩形与真实旧原生全合同对照、真实经理36预留、构造器零执行/真实实例正控、构造器额外形状能被旧合同拒绝、20个负例、Area与纯视觉嵌套正控、资源变动不读旧缓存、非单位根变换与替换pose只乘一次。后者不是任意脚本静态认证。
- `RunStableClawPullTests.cs` 与唯一owner：加入上述验证和三个新源范围，不新增豁免。现在1775清单项/891 C#。
- `PrologueLevelRuntimeTests.cs`：旧死亡夹具先等真实Playing/人口准备提交，再发三种真实输入并确认采样序列推进。同步致死仍要求Requested1/Completed0/HP0、Current/Raw清空但序列不变、源输入仍held、立即停机且旧actor尚未reset。完成回调改核当前实际合同：准备已提交、处理标志启用、旧actor恰reset一次、同epoch/全50 revision精确差量、瞬态闭合及精确原起点。恢复后须再次真实采样且无旧PrimaryPressed；这不冒认第一个恢复采样的独立观察。生产恢复逻辑未改。

完成点位置在回调内采集，不用已恢复运动后的后帧位置替代原起点检查。专用marker明确 `resident_commit_barrier / sampled_resume`，不再冒称旧4帧等待未变。180物理边界只是上限，等待的是实际完成且Recovering所有权/无准备故障，不是睡满固定延迟。

## 测试先行与一次审查修正

证据根 `G:/knight/验证证据/october13-stable-claw/`；失败原件均保留。

| 案例目录 | native exit/ms | 实际结果 |
| --- | --- | --- |
| resident-level-lifecycle-20261023-135041-349-1dde54f493bc4f999bf71250a6d2fd07 | 1/543 | 空实现行为RED：saved_body_reader_not_implemented |
| resident-level-lifecycle-20261023-135316-746-d48e3b02ffaf43ddae83467bb96f7541 | 1/656 | SceneState路径实际含`./`，unsupported_body_path；修正路径规范化 |
| resident-level-lifecycle-20261023-135604-861-cdb1e50d69b24952986ad2d565324bcc | 0/6041 | 读取器9种/15负例绿，经理此时仍旧探针 |
| resident-level-lifecycle-20261023-135951-458-2d4a99d8b00d49008e880e710faa95b2 | 0/4705 | 实际经理接入及36预留绿 |
| thruster-generated-main-death-20261023-140534-358-88915f004646441b98720604816dae7e | 1/1666 | 当前读取器下仍复现旧死亡夹具未采到held输入 |
| thruster-generated-main-death-20261023-140816-191-904406af218645c7aeaf6cb62f6dba65 | 0/5125 | 实际准备/恢复合同迁移后绿，8 completion物理边界、1 resume边界 |
| resident-level-lifecycle-20261023-141033-523-aaff0d1869974db1b35bd1e05cc64297 | 1 | 审查反例RED：额外StaticBody被忽略 |

前两次失败除断言外，stdout退出各报告4个Shape2D RID泄漏，**不是四条WARNING**；external并未重复这一stdout错误。初四案独立核20对清单/24封存bin，各自前后相符，跨案source/bin不同，不能拼作同冻。后两案运行日志无E/W/F。

独立审查两个Important：非根物理身体被错误排除、原生oracle只看命名形状而非旧全合同。一次修正：只允许Area成为非身体分支，新增直接Static/groupedRigid/Area内Character反例；oracle真正调用旧函数，构造器动态加形状的对照必须被拒绝。另补scripted Shape/root TopLevel与替换pose控制。审查已确认两项在源级闭合，以下原生最终复验另列。

## 最终同冻12案：9 PASS / 3断言FAIL

| 案例目录 | native exit/ms |
| --- | --- |
| resident-level-lifecycle-20261023-141213-535-ad86682c0be649ee9150f390ea53e2ea | 0/4643 |
| thruster-generated-main-death-20261023-141335-490-4f9f54a6224a46fc8a99c70a0124bc68 | 0/4722 |
| resident-auto-start-20261023-141412-731-27abd820626e46b691c0da1eb9d124c8 | 0/4712 |
| resident-auto-session-20261023-141451-583-106cc3678c324c568b22cf247d4a6db0 | 0/12818 |
| resident-auto-stream-20261023-141539-112-b47146400ba149ce9a861b4afb0bc1ed | 0/20017 |
| resident-auto-teardown-20261023-141630-036-26355c1605df4e8c87f50019c6051467 | 0/4992 |
| ranged-camera-ground-20261023-141707-949-cf95f5f818be45568092e8ef049ae351 | 0/28533 |
| prologue-pre-20261023-141808-818-e988b159cf5d48c8bef38ba98511dbab | 1/39099 |
| prologue-post-20261023-141921-430-805f6cea39694bacadb60115b62541a9 | 1/165832 |
| keyboard-20261023-142239-972-23c49c581ba042d5a6853be87385f7fe | 0/9813 |
| actions-20261023-142324-787-003cfb5f2c8444fd99bac9ee5ba9429f | 0/7206 |
| thruster-route-main-diagnostic-20261023-142404-243-e8a329a7a4384dcf837bb86b1f228226 | 1/73280 |

实际通过正常24、取消/Continue/死亡恢复、跨区50身份及HP/死亡保留、退出、36相机样本/精确补生Health、键盘/动作，以及20负例的读取器经理控制。不等于正常全50战斗、最终视觉或真人体验。

三失败真实原因：

1. **pre** `CurrentBasinLaborerClearanceRuntimeTests:119` 全Actors按BellBackLaborer类型Single，多旧/居民Heavy使匹配不唯一。未执行到后续通道证明。正确旧目标是实际波次路径 `Gameplay/Actors/canyon_v2_encounter_steam-valves_batch0_1`，须交叉核SceneFilePath/无ResidentBinding/group batch合同，不能改成First或删新敌。固定准备等待也须另核；92/84/r28/4116、78px负例、300预算和同AttackId相位要求不减。当前fixture没有独立HP数值断言，不冒称HP验收。
2. **post** 原死亡夹具已通过（stdout327），继而 `P1LoopRuntimeTests:231` 失败：请求时仍Intro/准备中，Requested/Completed均0。owner先报scope缺失只是提前失败的次生收据缺失。须迁移P1真实可玩前提及当前完成/退出合同，保留隐藏发现/同实例/public request等要求；本轮未改，不把75案例写通过。
3. **Main** 零恢复推进至Gear passenger-route后，`BasinCounterweightRuntimeTests:379` 的 `Basin route W previews static lift control` 失败。实际旧Abyss已过、Gear两次J、HP4、能量0.95333236、热0且未过热，保原210预算；不是整条主线通过，不能以挂载观察记录冒认居民战斗全覆盖。下一须区分路线驱动假设与真实爪目标可用性，不强设pose/目标/地形或关敌。

## 保全与恢复普通构建

独立只读核60/60五类清单对逐字节一致、72/72封存bin完整匹配，无额外bin。12案source1775/891 C#共用SHA `55445E86E3986F0586B05E147298FF1383287AB4B40A3B89701E151747CFCBB2`；六bin清单SHA `068B05D5607871F5A25154B1CFDDAB88DED64B8A0E9822D22DD3AA3C80A6C8D5`；Knight.dll `897636C5E3C4E06D3AE17F747B03EE94576348E5E4F601AE246E8E5618EDDBAF`。

24组build/native PID/精确argv匹配；12次ordinary Rebuild0错误/0警告，无额外DefineConstants、诊断flags或timeout，guards全空。九共同helper路径SHA一致，camera/pre各多一个适用admission helper，不是漂移。109项用户元数据清单前后相同，continuation是空清单，不读真实存档语义。封存DLL五个已知identity类型/ManagedSerial字段均0。

stdout E/W/F全0，stderr合计E3/W3/F0，external是对应镜像，不重复计事件。start一条Intro→Intro负例WARNING；post两条为故意observer抛错及VisualScene根类型负例，均有严格分类。实际未初始化Handle/FATAL均0，`System.RuntimeMethodHandle`只是堆栈类型名。

根复核live1775源零漂移、index仍 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。没有owned Godot/dotnet消费者遗留，不干扰godot-ai桥。所有记录只在消费者结束后更新。没有Git写入/上传/删除/付费/前台输入/新媒体/新试玩包。

## 完整目标及下一交接

原十一项/Tasks1–4不勾完。先闭合P1与旧波次消费者、升降台实际键盘目标，再同冻结Main+Explore、全50实际接触/返回/战斗与旧26共存、历史资源门。正式五族/Boss/角色/环境/机关/UI/音频/批准演出、正常速度审美/动作、目标硬件性能、真人键盘和ZIP/GitHub交付均仍必做。

独立后续诊断已核：P1 `LoadLevel` 只等2physics，且调用者在cleanup try之前await；迁移真实Playing时失败清理必须由该helper自己保管。hidden/两种exit完成合同仍是旧4process/禁用flag，须改实际resident commit与移出树后不再真实执行，不能把 detached 节点的 enabled flag 当执行事实；正常registry/两种移除/故意observer/原cleanup不得跳过。

Main W窗口失败在K之前，当前缺少实际sample Aim/preview/LOS标量，不能归因居民抢靶。保存control世界锚点(2492,3816)，原生站位(2492.709,3879.9333)约64px/偏上0.64度，名义上在620/22.5度预算内；原5/80刹车已vx0/axis0/HP4保持。下一复用已有被动cast trace，只记录W前、真实reader→claw边界及断言前的输入序列/方向/状态/preview/锚点/LOS，不再次调用resolver或逐帧IO；只有拿到本案原因后才决定是否改测试驾驶或生产可用性。保留原断言/站位/目标排序/AI与地形。

视听资产只读核对给出一个无需新付费的小切口：已有批准独立 `lantern.png`，来源见 `docs/art/player/player-art-production.md` 与生成日志，后续可接正常Lantern层并核枢轴、透明边缘、摆动/锚点/遮挡。不恢复被否决A2/idle、不冒认总装已批准；本轮尚未接入，新最终美术/音频仍0。

媒体一次工作流程内检查：UTC06:30:41，4838B/基线3508B/增长1330B，未到20GiB，Notify=false。无新图/视频；不建立通知任务或自动化。
