# v123 · 返程站位分流与战后夹击取证

原十一项完整目标及Task15仍ACTIVE，真人显示记录仍NOTSEEN。本轮只修自动键盘试玩消费者和被动记录，不是生产物理/技能、地图、美术或难度升级。唯一工程为`G:/knight/Knight`。

## 事实、推理、风险

事实：物理v122→本轮仅3个测试C#变化：Main14+/2−、Placement33+/0−、Traversal43+/8−，合计90+/10−，源码增删0；Source1844/C#892，helper不变。没有改生产AI、HP、能量、地形、目标排序、原900/300/40/12/52预算或真实键盘/恢复否决门。

推理：两条不同问题均已有实际证据。Main旧晚期日志随被捕获Burst的HP1意图结束，不能解释后续Rust近战与远侧弹丸。Explore则按接触Top还是Step8分流，导致已经在Top右侧的角色跳过合法地面接近，原地返回后抓错地形。本轮分别补记录寿命和统一返程中心站位，不以同一原因解释两者。

风险：当前完整Main/Explore仍失败，前段还出现真实原生FATAL。局部1→1/2→2、安静后段、纯合同或代码审查，都不等于通关、资源修复或正式画面验收。

## TDD与实现

晚期观察：8个字面量合同，旧逻辑在“postShot、首K已真实冻结、Burst已死”案例实际RED070627（1/676ms）；最小GREEN070826（0/1797ms）。只对显式postShot策略把原同一次扫描的记录延长到首K冻结之后，默认原观察寿命保留。仍是32条facts/96行环形记录，保留丢弃/fault；只转抄既有候选shape数值、玩家速度/朝向/护盾和已捕获敌人状态，没有新resolver、射线或危险扫描，也没有据诊断数据改战术。

返程：10个字面量合同，旧无站位限制的行为实际RED071658（1/503ms），GREEN072111（0/1802ms）保留全部旧mixed/真实J交接用例。新条件同时要求实际身体中心和中性滑行终点处于有限、有序区间。纯测试的4930.075..4999.925只是手工夹具；实际保存场景区间不是这组数。

`Task4ReachSupport`原地面分支现在接纳真实Step8或FullTop，保持原enabled/translation-only/完整胶囊支撑/HP/动作次数/300门。目标仍是原公式的中点，没有写位置、速度、额外跳跃或固定等待。停稳及already-supported提前返回均须满足同一返程区间，不能凭Top接触跳过接近，也不通过删早返回落入普通跳跃。

实际保存场景区间为4960.08..4969.92，中点4965、规划速度上限73.79882，均来自原地形/胶囊/SafeMargin公式，不是新调参。该区间是中心站位区间，不把它夸称为整个胶囊与Step8绝对不重叠的连续几何证明。

## 原生执行记录

证据根`G:/knight/验证证据/october13-stable-claw`。12案均普通构建、0警告/0错误、ExpectedFailure空、guards0、无超时；未用额外Defines、fixed-FPS或movie。

| 案例短名 | strict | engine / ms | 实际结果 |
|---|---|---:|---|
| mixed070627 | FAIL | 1 / 676 | late8缺战后观察RED |
| mixed070826 | PASS | 0 / 1797 | late8及原用例通过 |
| Main070901 | FAIL | 1 / 129575 | 新晚期记录成立，仍Rust死亡 |
| mixed071658 | FAIL | 1 / 503 | Top10无区间约束RED |
| mixed072111 | PASS | 0 / 1802 | Top10/late8及原用例通过 |
| Explore072142 | FAIL | 1 / 160783 | 新FullTop132/300成立，后续bone死亡 |
| seam073144 | PASS | 0 / 2084 | 原Step8专项77/300、HP5保持 |
| Main073217 | FAIL | 1 / 131452 | 最终Main仍实际死亡恢复请求 |
| Explore073458 | FAIL | 1 / 163784 | 最终Top71/300、Grip2骨居民伤害 |
| pre073811 | FAIL | -1073741795 / 58205 | 实际Finalizer原生FATAL |
| post073941 | PASS | 0 / 196003 | 原75+owner8，原4条scoped WARNING |
| models074325 | PASS | 0 / 504 | 最终普通模型回归 |

合计5严格PASS、7FAIL（含2个有意TDD RED，不当作普通成功）。post的stdout/stderr/external ERROR均0，WARNING为0/4/4，同组镜像不算8次独立事件；原scope多重集匹配。原75 enter/75 return、索引0–74、owner8次nodes1→1、普通完成标记各1/CONTROL0均由独立审计核实。300000ms后段诊断上限未改，不作目标硬件性能认证。

## Main新增证据与下一边界

诊断case`thruster-route-main-diagnostic-20261026-070901-094-3237931bbb83438487cd488a658f4224`有晚期96行、physics7117..7225，最后factsDropped0。只在本case中：Rust `17369653054051` attack5位于右侧，source `(4773.1426,3420.9336)`，玩家`(4757.222,3423.933)`静止，仍左盾；7219/7222/7225其Telegraph剩余`.116666496/.06666654/.016666591`。左侧resident Burst `4772497197589`的round16圆形clearance为105.142365/83.35641/61.629883，round17仍在。Rust候选HasShape=false，不能当零尺寸或据此签安全转盾。

最终Main073217是另一case，死亡为physics7225/input7197，来源Rust `17369132960395`的AttackHitbox `17369200069216`、attack5；左侧winner为resident Burst `4771826108896` round16。命中时L held/active真、朝左、E7.600038/heat42.233217、未过热。不能沿用070901的ID/7226时钟；最后仍96行、fault0、passes411，环形丢弃279，明确不是全战每个内部await/子步覆盖。

只读合同核对：Normal正面guard可零伤/零击退且不额外扣hit能量，但L持续每秒24E/+42heat照常。Hitbox同一Begin窗口只接收同Hurtbox一次；Begin会清集合，不泛称任意重复Begin仍按attackID去重。旧Burst/Scatter弹Launch内部为不可变配置、后续不转向，但无公开Velocity/Speed；owner当前Aim或Active寿命不是旧弹速度/ETA。Missile可导引和爆炸，不可套直线小圆合同。

安全转盾还缺实际右向/还左样本与D/A脉冲及惯性包络、真实Rust发布shape、所有同期弹体的可靠速度/身份/delta约束、同Rust attack/玩家Hurtbox的Normal Guarded+Connected及HP不变配对。未实现固定延时转盾，不猜440，不丢掉非nearest/未知shape/新生弹。下一先补这些事实或实际物理避让，不降储备、不加K重试/等待池。

## Explore：两个不同进入状态，不混日志

Explore072142实际已经FullTop，入口x5054.8467，较早8546已被ghost-diver命中HP2→1。新地面接近132/300到x4964.514/速度0，HP1→1，jumps20/dash0/nextCast16保持，随后真实LandingGrip、Grip1抓落台成立；到Grip2准备首帧9045，bone-wanderer`.counterweight-lift.01` attack1使HP1→0/request1，未发Space/K。这里132/300是新分流路径的实际证明，不是整返程无伤。

最终Explore073458进入Top时保留了Step8接触，原地面路径71/300到x4965.144、HP2→2，没有本case的8546 ghost伤害。随后真实LandingGrip落在`(4767.921,3423.9673)`，Grip1落在`(4463.868,3283.9885)`；Grip2准备10/40+settle1/12、真实ground jump20→21/space_edges1，原共享17/300时bone-wanderer `6165912097791`、Hitbox `6165979206659`、attack1在8799造成HP2→1，nextCast19未变、未发K，原HP门拒绝。

两case同源冻结但入口/时钟不同，不能把最终71误写为132，也不把最终HP2→1写成1→0。没有实现、批准或计入Grip2护盾/姿态修复。下一需要从Grip1抵达/Grip2准备前的实际敌人发布与输入所有权取证，不能盲加L，因为现有Ready/预测对Shield有约束。

## 资源故障仍为发布阻断

pre073811不是超时：exit -1073741795/58205，原文`FATAL: Condition "gchandle.is_released()" is true`，`mono_object_disposed_baseref`，`GodotObject.Dispose/Finalize`、`GC.RunFinalizers`和refcounted_disposed栈均保留。最后可见普通受阻通路案例完成，不把其邻近时间当作native owner归因。post安静不能关闭本轮FATAL，也不能把它与旧`GCHandle.FromIntPtr`零输入错误擅自认定为同一根因。

当前没有匹配native符号或已验证混合调试入口，未attach、下载/安装调试器、猜C++布局、改engine/GC/pin策略或Dispose借用资源。已向用户提出非阻塞问题：是否允许先核验一次隔离原生调试方案的来源/符号/体积/磁盘成本；截至收尾尚未获答复，未据预选项当授权，不直接安装或启动。其他安全制作继续，目标不因此暂停或改为blocked。

## 唯一审查与根的全部范围裁决

一位fresh gpt-6-astra/high reviewer `/root/v123_top_and_late_review`，对指定物理v122三SHA差异只读核对：新增C0/I0/M0，根按实际效果重评同意，不做修复pass或复审。这不是合并/整关批准。

以下为全部22项Declined的根裁决及错判成本：

| # | 根裁决 | 若错判的成本 |
|---|---|---|
| 1 | 最终六阶段已实际结束，按上述strict结果而非review批准 | 混用未结束或失败测试 |
| 2 | Main仍死亡，晚期记录不算战斗修复 | 虚报通关 |
| 3 | Explore仍骨居民伤害，Top局部通过不算整路完成 | 把卡点后移当验收 |
| 4 | 只证明本次FullTop及原Step8入口，不保证所有姿态/抓点 | 其它进入仍失败 |
| 5 | 手工纯区间不冒充实际保存地形；实际4960.08..4969.92另列 | 自证错误几何 |
| 6 | 96环形决策样本有限，丢弃/内部await缺口明示 | 漏记被当安全 |
| 7 | 无旧弹速度/ETA或转盾闭环通过，未知项继续拒绝 | 玩家被反向弹击中 |
| 8 | 纯8/10与真实输入/移动各有独立证据，不互替 | 有条件没有执行 |
| 9 | 后续Bell/Moth实例未全跑，不续签K全局成功 | 未记账或错K假绿 |
| 10 | 不保证所有火力组合窗口，不放宽900/储备24 | 用削难度掩盖策略问题 |
| 11 | 未改W/preview/排名，不新增整体通过结论 | 破坏正确选靶 |
| 12 | 无新native first-hit实验，形状/站位不替代遮挡资格 | 误把可见当可抓 |
| 13 | 原Intro/Playing提交拒绝合同保留，未重签所有边界 | 准备期错误执行 |
| 14 | 新内容同步被动；旧deferred同actor/parent/pose/sequence仍须原专项 | 释放后状态污染 |
| 15 | 未改LoadLevel清理/双异常，不据此签新通过 | 泄漏或主异常被覆盖 |
| 16 | 边界full-support不冒称连续swept | 中间碰撞漏验 |
| 17 | 全源/helper/binary由独立60对/实体审计证明，不由3文件review推断 | 混版证据 |
| 18 | native owner/GCHandle/FATAL/RID继续OPEN，已实际FATAL更不能关 | 发布崩溃版 |
| 19 | 原十一项/76普通+BossNPC/地图机关全部稳定ID保持，FTR/取消/UE不纳入 | 缩小或扩张范围 |
| 20 | 正式图声/动作/UI/相机无新验收，资产表无新增正式项 | 以测试代替精美度 |
| 21 | 未做目标硬件/真人新验收，NOTSEEN保留 | 虚报可见性/FPS体验 |
| 22 | 不具发行资格，不提交/推送/删除/打包冒称交付 | 对外发布未完版本 |

四项继承Minor继续延期，未计已修：energy-unwind早失败flush（可能少日志）、boundary samples的swept措辞（可能夸大覆盖）、finally stage4 summary（可能混淆异常交接）、fresh LateFact来源标注（可能误读outer/fresh求值）。本轮没有新增Minor。

## 保管、归档和后续

根82510/93199/59191/77868/8560均terminal；8560 exit0只代表子阶段全部执行结束，各case仍独立strict。独立custody `/root/v118_custody_audit` ENDED，最后1b8465/exit0，无运行handle；24个记录native和KNIGHT Godot/dotnet/owner各0，之后才写本轮durable文档。

- 60/60 manifest对字节一致；6124实体副本（source5992/helper132）逐SHA匹配，72 sealed binary全匹配，缺失/extra/fault0。
- 最后GREEN、Explore、seam及最终5案16份source前后清单同SHA；当前1844源/892C#、Debug6/6匹配models，无源增删改漂移。
- helper最终联集12单版本且当前匹配（预审7案只有10条，未提前计12）；owner/HEAD/branch/index/audio/productionClaw不变，24份user manifest同SHA BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB，未读真实存档语义。
- final source `371AB3C8F4267F585F88E544D22DC93D4C453A123AFA07F0ECD6B559C55355F9`。
- binary manifest `5386F4ED960AAA2CD3211DE2D15417EE24899DE702FC619DE7F9D96621734E55`。
- helper manifest `A1AC76A1CBEEAF2061E4787C7AA4191B2FE6831CCBD6640BC2EE66C2E403B8F1`。
- Main `AB2D3EAF670F8C4BF74FAE1F99853798677181BEEF7ADDE46B26BCD6A45E9830`；Placement `64775AECD21A4F611D54D452F4946A122555BCA55B2CCA54BBA737C2139720E1`；Traversal `2846CC7BAEDA4F69161A96234E862DB2D2DF671D40B3699A27C511A15BCC3E99`。

本轮唯一手动媒体Check UTC2026-10-25T23:51:36.0659047：Current860284/Base3508/Growth856776/Notify=false，无新20GiB档位，不暂停。G盘free182241488896字节为收尾只读观测。无新正式素材或过程图稿/实录、付费、自动化/独立通知、前台OS输入、Git写入/推送、项目或历史资料删除。

下一：返程骨居民同次攻击发布/输入边界、Main实际盾方向与弹体/近战几何及可行避让、经用户选择后的原生调试方案核验。完整原十一项、同冻结正常双路、可靠死亡/继续/出口、正式分层视听动作/相机、目标硬件/真人及授权发行仍必须真实完成，不标目标或Task15完成。

证据：[FullTop新分流实际日志](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-072142-114-1ee33f74fe69434f8dbf75d3edaba2d4/engine.stdout.log)、[Main晚期取证](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-070901-094-3237931bbb83438487cd488a658f4224/engine.stdout.log)、[原Top专项](G:/knight/验证证据/october13-stable-claw/thruster-steam-top-seam-20261026-073144-357-6d69be0fe8aa480c80b721ae22e2fdd7/provenance.json)、[最终Main](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-073217-901-c68aa4a64e014f6697d8e966b333282e/provenance.json)、[最终Explore](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-073458-453-8c8ac582126241b9a231a61b69b33a93/engine.stdout.log)、[前段FATAL](G:/knight/验证证据/october13-stable-claw/prologue-pre-20261026-073811-348-7cfdbf4bf7ab4d4680c195019d4bd61f/engine.stderr.log)、[后段完整来源](G:/knight/验证证据/october13-stable-claw/prologue-post-20261026-073941-465-4e938a4abc96478da66b18fa1793a995/provenance.json)、[最终models](G:/knight/验证证据/october13-stable-claw/models-20261026-074325-098-2c47afda6a154b66ad1d764e74b85ce1/provenance.json)。
