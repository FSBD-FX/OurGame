# 常驻空中运动与真实受力 v001

2026-10-16，开发验证记录，不是发行版本、最终美术或完整试玩验收。工作目录：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。承接[冲锋死亡边界与资源对照](2026-10-16-resident-charger-death-and-resource-differential-v001.md)，旧报告及原始失败保留。

## 事实

用户确认自动探索期间正在操作其他窗口，与095253前台切换后清空按键的记录一致。本轮保持无窗口工作，没有抢焦点、补键或重跑可见路线；不能用该回答解释更早094130录像失败。

新增的是保存版GraveLampMoth上的显式常驻空中适配器，不改变未配置旧波次的默认路径。它接受入树前一次有效配置，校验完整半径16身体、空域及航点；使用真实MoveAndSlide/MoveAndCollide、World2D/Health/领地/视线查询，不以home假玩家、挂载后改坐标或checkpoint reset冒充巡游/返航。

目前共用的空中行为包括二维巡航、真实目标对齐、锁定预警和原生首接触俯冲、Recovery后物理回升返航。俯冲绑定原目标身份，目标离区或撤权后取消旧攻击，不能换另一个活玩家维持旧锁定。真实机械爪拉力和伤害冲量会打断自主攻击，保留实际重力和外力，可离开自主领地后连续返航；不硬夹回边界、不回血。

休眠立即撤直接出入伤、旧抓取接收和发现组/层权限，但保留已经接受的拉力与重力。唤醒不回血，重新攻击需要新的完整预警和AttackId。Monitorable按合法延后边界读取当前生命，而不是提交旧布尔快照。实际原生致死发布中显式checkpoint reset后，第二生命仍可再次原生死亡并独立发出一次Defeated。真实零layer void的待致死状态可跨无敌、休眠及来源移除，不能用睡眠变成不死。

**最终14行为断言和逐案清理回执均符合，engine exit0；stdout仍有4个Shape2D RID退出ERROR，专项严格FAIL。** 这不是完整空中角色、全部资源问题或全关验收。Flyer/Diver目前允许配置但共用适配器，尚未实现各自职能/族系差异；旧飞蛾探针不是正式骨骸/幽灵资产。

## 原生证据

证据根目录：`E:/OurGame/Knight/.evidence/october13-stable-claw`。每案保留原始stdout/stderr/external三日志、provenance、输入副本、全新Rebuild与前后清单。耗时是引擎执行时间，不是制作工时或真人游玩时长。

| 阶段 | 精确目录 | 结果 |
|---|---|---|
| 最终空中14项 | resident-air-20261016-143008-845-f9867685e28145fb8ffa99db8c119ce0 | engine0/40249ms，14行为及14清理回执符合；stdout退出4Shape RID，严格FAIL |
| 旧未配置飞蛾完整模型/运行综合 | resident-air-default-20261016-143347-967-cfc5b0539c0349c0a6014e53e2cac936 | engine0/5061ms，三日志无诊断，GREEN |
| 当前键盘 | keyboard-20261016-143424-390-5b5a725e14d44ed3be83ae275856b70b | engine0/9564ms，三日志无诊断，GREEN |
| 当前动作 | actions-20261016-143459-124-ed9ac93c9606408e9666c8a739443237 | engine0/6972ms，三日志无诊断，GREEN |
| 真实流送来源10项 | resident-void-stream-20261016-143533-189-9b4853e715494b3187e1341978dac088 | engine0/1424ms，三日志无诊断，GREEN；不是人口运动或放置验收 |
| 完整pre | prologue-pre-20261016-143602-140-6496202e910745ebb1351228c5cfb00c | engine1/88106ms；旧UpperPendulum/lower_to_mid_swing sample0.5错台，149/150帧、landingCrossings0；两条刻意profile警告保留，FAIL |
| 完整post | prologue-post-20261016-143755-077-38fb5909fa144e01bb7cb12920aa5b04 | engine1/7914ms；旧BellChain地板起点540.0704,1196.0139与KillZone六层ILevelRecovery解析失败，FAIL |

最终七案实际源码库存1665、受保护真实用户数据109项、guards0、无超时；source/helper/user/continuation四组前后清单及fresh-binaries/binaries-after逐案相同，Rebuild均0警告/0错误。三日志分别读取，空stderr按0字节处理，未将扫描器输出截断或空值错误当引擎结果。当前pre/post未出现历史句柄/FATAL/RID，不等于历史问题已修好。几何、名册、Rust与Charger专项本轮未重跑，不把旧阶段证据改称1665新跑。

### 最终14项实际覆盖

| 检查 | 原生结果与边界 |
|---|---|
| unconfigured_hover_unchanged | 未配置保存Moth无玩家60帧保持原地Hover |
| actual_two_axis_air_patrol | 300帧实际范围(288.88733,96.88733)至(351.1127,159.1127)，最大步长0.800016；不是数据航点或纯水平移动 |
| complete_circle_rejected_and_same_body_wall_blocks | 中心在领地但完整圆越界时拒绝配置，随后有效配置可用；真实墙前最大x327.91934，之后继续左巡航 |
| locked_dive_contact_then_physical_home_return | 实际发布锁定预警、原生伤害与首接触y239.99219；撤目标后真实回升home，最大步长2.3926697，不回血 |
| locked_target_departure_withdraws_attack | 锁定后真实目标MoveAndCollide离区，90帧无Active/伤害，旧直接接触被拒绝 |
| real_claw_external_motion_then_air_return | 实际ThreadTarget接收拉力，越自主空域至x458.78604/y406.35596，420帧内连续返航、HP2；最大步长12.327951来自实际重力/受力 |
| active_sleep_rejects_direct_authority_and_fresh_wake | 真Dive休眠后直接出入伤、旧pull、两发现组与目标层撤权；无既有外力时保持静止，唤醒新完整预警/更大ID、HP不变 |
| native_damage_interrupts_dive_and_preserves_force | 真实Hurtbox伤害/600冲量打断Dive，x至365.16095/y384.75后物理返航、HP1，最大步长12.25 |
| death_is_absorbing_and_configuration_is_atomic | 拒绝入树后配置；真实Health死亡后睡醒不复活/不回血、一次Defeated，无新抓取/伤害权限 |
| zero_knockback_hurt_revokes_active_dive | 非致死真实伤害即便Knockback0也立即撤旧Dive与直接出伤 |
| locked_target_cannot_be_replaced_by_other_live_player | 另有真实合格活玩家时，原锁定目标离区仍取消旧攻击 |
| native_death_publication_reset_preserves_second_life | 实际AreaEntered致死，在Dead FrameChanged中显式reset一次，再第二次原生致死；两生命分别一次Defeated，最终Monitorablefalse |
| sleep_keeps_existing_external_physics_but_rejects_new_authority | 已接受拉力同帧休眠后10帧仍有x/y真实运动；新直接伤害拒绝，保HP；不是将已有外力冻结的假休眠 |
| real_void_pending_survives_sleep_and_source_removal | 真实零layer16px void、HP1/1.2秒无敌及向下600；穿越后撤目标权限，移除来源/休眠仍最终Health0、一次死亡，无玩家Recovery调用 |

夹具只拥有新建的World2D、节点及floor/player/void等形状。逐案finally释放自有节点图，经原两次ProcessFrame后Dispose自建形状/World2D，并在清字段前检查nativeGone、分配/释放计数相等、borrowedDisposed0。不Dispose保存场景借用形状，不新增GC/cache/KeepAlive/退出排空或诊断豁免。回执满足不能据此定位退出4RID；与此前同数量症状相同也不是因果证据。

## 推理：反例及实际修正

- 初始141137（1664，1821ms）实际只有8案：默认Hover符合，7配置案在缺适配器处停止，不能称7个运动反例。owner预期9的数量不一致随后通过增加真实完整圆越界/墙碰撞第9案修正，未下调门槛。141353（1689ms）默认1符合/配置8缺适配器，均同时有4RID，不称干净RED。
- 141705旧完整飞蛾综合在1664阶段先严格GREEN，保留旧合同。新增partial适配器后真实库存1665；142104（37017ms）9案有8符合，只剩拉拽返航在420帧仍距home约11px，同时原10.1步长断言错误地只考虑初始拉力。
- 拉力后0.75秒真实980重力产生约735垂直速度，加衰减水平拉力，实际12.327951px/60Hz合理。独立推导后步长上限改13，未削重力、改运动、加帧或放宽原native时限；原420帧保留。返航从巡航48改用已有Align速度140，仅影响显式RETURN，巡航48不变，无姿态/HP写入。
- 在修生产代码前新增5个原生边界。142742（40357ms）14案恰4失败：返航过慢、零击退伤害不撤Dive、另一个活玩家维持旧目标锁、死亡发布内reset被旧死亡标记覆盖；其余10案符合，包括既有外力休眠和真实void待致死。此案仍4RID，不称干净RED。
- 最小修正分别是上述RETURN速度、所有非致死受伤撤攻击但仅非零冲量请求物理Stun、绑定锁定目标身份、先写本生命死亡标记再发布可重入事件。最终143008的原14案均符合。没有把睡前已接受外力继续运动判成bug或冻结来凑绿。

旧完整综合重新执行原GraveLampMothModelTests及GraveLampMothRuntimeTests，不只是新夹具两个默认控件。其默认Hover、原Align/Telegraph/Dive/Recovery/Stun参数、锁定预警胶囊、真实Active移动后Circle、直墙/斜面首接触精度、实际机械爪/落地/显式reset合同保持；不能扩展解释为全9旧敌种或整关已通过。

## 风险与下一步

- 这是共同空中行为适配，不是最终Flyer/Diver两个职能或五族差异完成。目前未单独原生证明foreignWorld/dead/遮挡目标拒绝、天花板以及锁定后LOS/目标死亡撤权；源代码有过滤不等于这些分支已经实测。下一步先补这些有区分力的检查，不重跑无改动14案或空名册凑进度。
- 小空域真实回家不等于任意复杂障碍导航。全部50正式身体/攻击/返航包络、玩家接近/关键抓点、机关组合与跨区生命周期还须逐点认证。真实receiver拉拽不是新版配置敌人的整路键盘K证明。
- 正式新增仍0；全部50正式Placement为null、24正式族系子型场景未实现。保存版仍原26普通名额＋Boss；骨骸10、幽灵10、五族/六职能及原分配不减。此前41活动候选/9未解是1663几何时点，不改称1665新验收。
- 完成其余空中边界、重型/炮台/远程/骨骸/幽灵差异后，回到全部50正式地形认证，再接manager和实际Main/Explore。配置Moth/Charger及历史独立4RID、旧完整pre/post、旧档/呈现60FPS/真人/最终美术/成功短录像门仍OPEN，完整目标ACTIVE。
- 本轮无可见游戏/新UI、图稿、录像、新美术、付费、Git写入、上传、自动化恢复、安装、删除或真实用户存档语义读写。原始证据在项目`.evidence`；新报告不冒充过程美术。所有拥有的消费进程终止后才更新文档。

## 最终核心源码身份

| 文件 | SHA256 |
|---|---|
| scripts/enemies/moth/GraveLampMoth.cs | B33FD1398293FBC7D0DC4F5DCA8892B3A88DF8403D183FB48A29762C72A0257F |
| scripts/enemies/moth/GraveLampMothModel.cs | 11549032FC899D25808963CD36B3A058210C7309718D705EBA0F2976758822CC |
| scripts/enemies/moth/GraveLampMothResidence.cs | 9F890FAAAF1489D2C279DFC971AD9B974B857D45C151AD067A87C122CFBF5D32 |
| tests/integration/ResidentAirRuntimeTests.cs | FC298294ED93121504DB5ADD6E1FA1247779D635F32A1C72192F129EDB186C39 |
| tests/RunStableClawPullTests.cs | C9573A28FCF1D002A4BDAC4BE81AFEDAA78F2E3709D62F35FD055F1B065DE329 |
| scripts/enemies/residents/ResidentVoidContact.cs | BF7B70B7D52D1C044E9B7551E351836E1E164A4B76BBF015022B2BB12D1CD441 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | 7F07D7ED017675A8D3A6C04804327D8D557F8F6CFD7F8716F8093EB5114B0B60 |

实际命令：固定pwsh执行`.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 -Phase <表中phase> -SourceRows 1665`；底层固定dotnet使用`-t:Rebuild --no-restore -m:1 -p:UseSharedCompilation=false`。resident-air保留55s、完整pre/post180s及三日志严格否决，不设置ExpectedFailure。
