# 下降第一小平台的可选机械爪指引

基线 `498e6a8c46f552434aff07be29a7bf8d283c4fd6`。这是本地开发检查点，不是r3或新整包。
证据根 `E:/OurGame/Knight/.evidence/september08-descent-first-claw/`，最终SHA由外部 `CHECKPOINT.md` 绑定。

## 事实、推理、风险

- 实际Q/E教学完成后，E只代表切换模式，任务便进入ReachBasin；即便没有成功抓附，下降卡也只讲禁抓与安全下行。
- 第一小台GripStoneCatchZone实际站位约(1655,648)。旧指引指VisibleIceWall，距离约870px，超过620px射程。
  这不证明关卡断路，只证明“提示禁抓”不等于“给出可立即执行的第一次抓取”。
- 几何调查候选是FrostVentCeiling下表面，不能把body中心当命中点。
  `candidate-normal.log` 用真实输入走到首台、瞄准(1960,511.53)，实际命中(1950.1046,516)，距离约320px、屏内。
  一次LMB抓附后D/W牵引、松手，实际落(2035.4543,703.98285)，GripLanding向上接触连续12个物理帧。
  该段没有普通跳跃、Boost或完成恢复；首敌接近路段原有一次过缝跳，不能泛称整片从未跳跃。
- 保留直接下行和失败回退；不强制抓取，不修改地图、射程、跳跃、助推、物理、能量、敌人或战斗规则。
  首爪牵引会先下沉再回升，可能让新玩家误以为失败；成功自动路线不代表已解决这种理解风险。

## 两项教学改动

1. ReachBasin+CanyonDescent的真实首台支持，提供可选短卡：E、瞄准标记粗糙顶面、按住LMB；也可A/D下行。
   `DescentClawGuidance` 从现有Rectangle底边内4局部单位派生瞄准点，用原MechanicalClawTargetResolver复核。
   验证指定surface身份、可用/可见、材质GripStone/TempleMetal、实际首碰/LOS、620距离及48px屏内边距。
   黄金标记显示真实Snapshot.WorldAnchor，不显示实体中心或内部cursor。不增加新抓点。
2. 同目标/分区在Latched/Reel/Swing/Boost时，沿用既有A/D、W/S、Space、松手文案，目标改为当前真实AnchorPoint。
   先检查当前target有效、可用且锁定ID一致；Released/Retract不因残留CurrentTarget继续显示附着标记。
   其他扇区及任务的目标规则不变，静态sector resolver不读取玩家，新动态key由Controller消费。

机会阶段限Idle/Aim/Windup/Travel，且每次仍需站在首台、满足目标资格；发射短程不突然切回冰面卡。
候选解析至多每物理tick一次，同结果驱动文字与标记；Hide、换区或许可失效清缓存。移动/几何在一tick内可有短暂旧值。
持盾、恢复、死亡/不可处理、结算/Boss等不提供机会。Q攻击模式仍可以看见“按E”建议，没有直接拿CanBeginGrapple隐藏它。
新目标沿用原标记样式并提供32×32保护区，无新美术文件、付费资源、卡片字号/候选布局重写或动作修改。

## RED / GREEN

- `guidance-red.log`：真实main经过Q→E，受控摆到首台后真实物理支持成立；目标实际解析可抓，但卡没有E/LMB，退出1。
  这是受控集成fixture，不冒充实际走完全路线。
- `movie-red.log`：RTX3060硬件Vulkan固定60fps，完整实际首敌格挡反击→下降首台→抓顶→右台。
  抓取前缺E/LMB、抓附后仍指Ice，两处教学断言失败；先完成路线和截图再统一退出1。
- `movie-green.log`：同输入路线成功，pre与latch各渲染一个非Ice目标、真实文字完整，单cast/latch/release、落稳12tick，退出0。
  前后17条阶段物理/渲染序号、位置、速度、绳长、可见性等记录去掉instruction字段后有序完全一致；不是全状态空间等价。
- 独立审查补出持盾边界：过热后IsActive=false但ShieldHeld=true，真实抓爪仍被禁止，机会卡却恢复。
  `held-shield-red.log` 在148个循环样本后真实过热、held=true/active=false，错误显示机会，退出1。
  仅教学gate补读原Input.CurrentFrame.ShieldHeld。`pre-final.log` 同条件fallback，松RMB后恢复机会；未改盾或抓爪门禁。
- 新runtime回归覆盖中英、Q/E、Ice、隐藏、禁用shape、目标移动700px（超距与离屏合并样本）、实际World墙及恢复、离台、开关指导。
  真实LMB精确瞄准当前碰撞hit(1951.0294,516)可抓附，非仅点击实体内部；真实松手采到Retract即撤附着目标，再自然grounded+Idle六样本。
  暂无通用自动断言直接比较渲染marker坐标；实际点击与前后原图是补充证据，不等同该自动覆盖。
- 新unit覆盖双语全部claw阶段、其他objective/sector不被机会覆盖、关闭/重试、改绑glyph；未单独穷举新分支的Boss状态。
  集成加入常规pre。局部改变均finally恢复、事件退订、释放输入/fixture；GUID隔离存档仍有磁盘残留。

## 正常运行与完整通路

- `pre-final.log` / `post-final.log` 退出0，ERROR为0，WARNING 2/3均既有故意异常fixture；未过滤或取消负向测试。
- `normal-en.log` / `normal-cn.log` 非fixed/movie正常窗口，均真实格挡1、反击1、敌HP3→2、玩家无伤，然后单次首爪到右台12tick。
  两处教学文字可见完整，各渲染一个当前目标，退出0无错误警告。自动InputMap/鼠标事件，不是实体键鼠或真人首读。
- `walk-only.log` 不加首爪flag，仅D自然落GripStoneCatchZone和GripCatchShelf，各6个不同物理帧向上支持。
  约137个渲染样本中的实际顶边/文字检查通过，无新跳跃/完成恢复；非水平速度已经归零的断言。
- `route-final.log` 正常非固定帧、非movie完整五区：四复活、两检查点、Ice拒绝、过热、门/Boss重试/结算通过、无失焦。
  回落专项18下降样本0边距违例/0道具栏交叠，落台稳定12帧。旧--check-ascent-return分支会RefreshNow，不冒充纯被动呈现。
  2771进程回调样本：p50=16.660ms、p95=17.381ms、p99=27.454ms、max=60.930ms，12次>33.3ms、1次>50ms。
  保留该峰值，不证明由本轮引入，也不称GPU帧时、稳定60fps或真人手感认证。
- `route-visual.log` 使用RunTask9Tests --generated-main，五区通过无错误警告；不是新首台分支，而是原DedicatedLessonAnchor路线。
  未加回落可选flag、不人工RefreshNow、不改场景、不覆盖旧artifacts截图；含受控死亡/恢复自动验证，非真人通关录像。
- `ordinary-main.log` 普通窗口240帧及 `headless-main.log` plain启动退出0无错误警告；最终build零错误警告。
  无新导入型资产，未重新导入全部项目。Godot、build、ffmpeg严格串行，所有session关闭后才启动下一项。

## 录像与审看

| 文件 | 帧数 / 时长 | SHA256 |
| --- | --- | --- |
| before.mp4 | 589帧 / 约9.83秒 | 27610CC698D1301D0B7EB56B0FF9464BBD2BE3A333200F8095B98E65796D4A91 |
| after.mp4 | 589帧 / 约9.83秒 | 5C0ADD5D3D84FEFCE551311160D81164AFE73CADF9954BCC57EC229FF37559C4 |
| route-final.mp4 | 2720帧 / 45.33秒 | 785837A0C439AA9BEA5983B3F7F8AB065121C340902451630F4F3213AB28C632 |

三片均RTX3060 Vulkan固定60fps，不是正常性能证据，原AVI只留外部证据。after录制后仅补持盾机会gate，正常中英/通路和完整录像使用最终gate。
主与独立审阅实际看过前后2Hz板、英文pre/latch/landing原图、中文pre/latch原图；英文卡4行/中文3行完整。
标记与文字更贴实际粗糙顶面，所看样本未新增严重遮住角色、主绳段或落台顶边。
另看完整0.5Hz板，以及4–8秒原路线前后6Hz板（旧片来自september08-descent-card-clearance/route-visual.mp4）：
普通专用锚点路线附着卡仍可见，上升/转场所看样本未见卡被完全挤掉。末尾有fixture退出灰帧，不称过程黑屏。
不是逐60fps全片审查、任意UI尺度或真人审美/理解验收。

独立代码复核无剩余P0/P1/P2，原持盾问题已闭环。金色教学菱形与cyan抓点叠在一处，增加局部视觉噪声；
中文pre卡右下、latch卡右上，读者需要重新找卡。专用高锚点与资源HUD的旧局部交叠仍在。
不能将这些已知边界或首次下沉误判写成已解决。

## 接续

下一轮优先实测首爪先下沉再回升时的受力/收绳反馈：玩家是否容易把仍有效的牵引误判为失效。
先比较已有绳长、姿态/视觉与实际W输入，不先改牵引参数或塞更多文字；双重锁点符号列入同一可读性主题。
正式美术、独立Boost动作、真人首读/难度/回摆、旧认证、普通解压/源码一致性及真实商业数据仍缺。
不冻结r3、不新ZIP/标签/GitHub上传，保留所有无关import/总线/artifacts/旧UID；每10分钟自动化继续。
