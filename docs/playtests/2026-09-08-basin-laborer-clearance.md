# 盆地补给台下的敌人通行净空（2026-09-08，本地开发）

基于 `3ca7f3b7b0a96dda7f1f7dce55b6d7f104b6556e`。本轮修复右侧补给台下的真实卡位，
不重复上轮地形视线/命中过滤；不冻结r3、不创建交付ZIP、标签或上传GitHub。
提交SHA与最终证据回执位于外部 `E:/OurGame/Knight/.evidence/september08-basin-laborer-clearance/CHECKPOINT.md`。

## 事实、推理、风险

- 旧SalvageFloor底面世界Y1972、BasinFloor顶面2050，净空78px，低于原Laborer身体84px。
  `normal-red.log` 的真实InputMap路线先由EntryCatch落上层，停留300tick无伤，再右落地面等420tick。
  玩家停在(2867.5728,2013.979)，敌人仍x2650、Approach、反复碰上方平台，末次速度Y1192.3333；没有任何攻击周期。
  `pre-red.log` 也真实自动物理移动0、Approach、无攻击，准确失败。不是只检查场景文本或常量。
- 推断根因是身体被挤在两层实体碰撞之间；不能通过缩身体、关碰撞、提高速度或改追击状态掩盖。
  右侧补给台整体抬56px，净空变134px，给原身体50px余量，也使真实扇形最高点和台底分开。
- 恢复追击可能增加地面压力，升台可能破坏补给上返，因此两条补给路线、原Pass/Skip及主线都需回归。
  只修右侧台下；左侧SupplyCave两个低台仍有78px净空，不能称全盆地通行已完全解决。

## 最小改动

`tools/build/prologue/CanyonContentBuilder.cs` 中，SalvageFloor中心局部Y520→464；
台上PassStart/PassEnd/Reward各472→416，RecoverySupply中心488→432。
SkipStart/SkipEnd、台宽420/厚24、实体/可抓属性、EntryCatch、BasinFloor、敌人出生/身体/伤害/速度/时序全部保持。
builder生成 `CanyonBasin.tscn`，节点169→169；独立归一资源ID/unique_id后，120个显式节点只6个位置Y减56，
包括自动同步的台面可读外观，没有其他资源/节点配置漂移。
没有新增美术或付费。原SupplyRuntime的受控台上出生Y1912→1856，奖励/死亡/Continue/新局断言不放宽。

## 实际路线与测试

- `normal-green.log`：原上层停留仍HP5、0新起手/0伤害；右落地面后不按A靠近，敌人自行从作者x2650到2778.3423。
  总位移128.3423px，其中落稳观察开始后约100.34px；玩家停在(2867.5718,2013.9509)。
  地面观察记录到290次BasinFloor支撑采样、0顶碰、最大|Vy|0；两次实际Active与Recovery进入、该敌人Connected2，
  同tick玩家HP5→4→3。第一个Recovery自然退出后再开始第二击；探针在第二个Recovery进入时结束，不是两个完整Recovery退出。
  普通Jump0/Dash0/Recovery0，没有位置/速度/AI/Health注入、关AI或Present/RefreshNow。
- `supply-ground.log`：沿原抓EntryCatch底面/W收绳/D+一次Space/松LMB路线，落到新台中心Y1855.9783，缺1回1后左返主地面，
  HP5、一次抓附/助推/释放、Jump0/Dash0/Recovery0。原输入时序未修改；Y1880阶段还未高过新台沿，
  后续原助推真正越过台沿，不能沿用探针该条日志的“已经清台沿”字面说明。
- `supply-entry.log`：由原EntryCatch自然落新台，缺3回2、HP4，左返主地面；Jump0/Dash0/Boost0/Recovery0。
  这些都由作者高台/检查点预入树出生，中段任务及缺血是公开API受控前置，不是从入口全程真人实战。
  操作为InputMap/鼠标事件而非硬件键鼠；12支撑是不同采样，不保证每个physics tick均被记录；探针GUID存档仍可能残留。
- 新 `BasinLaborerClearanceRuntimeTests` 加载实际packed main，只停用玩家输入适配器以隔离外部输入。
  玩家在地面预入树放置，敌AI、运动/碰撞、Health、呈现自动运行。要求真实>=100px移动、Approach、地面支撑、
  同AttackId Telegraph→Active→Recovery及零恢复。其临时存档目录独立finally清理，事件显式退订，未入树失败用Free。
  原首版3次Physics/Process等待让headless在初始采样前多移动；现把唯一Process等待移到实例化前等根窗口就绪，
  AddChild后立即读实际起点/订阅。中间 `pre-final.log` 的CurrentSector为空是root未Ready的fixture错误，不冒作游戏RED；
  `pre-verified.log` 正确由x2650计到2778.342、同ID三阶段通过。阈值未降低。

## 整关测试的受伤时序问题

抬台恢复追击后，原整关路线在盆地检查点的受控死亡断言失败，
`route-normal.log` 与加入只读事件诊断的 `route-diag.log` 均保留，不冒称通关。
诊断记录实际 `Health.Damaged` 在tick1575发生，HP5→4，Laborer的AttackHitbox在同tick发出Connected；
tick1585才注入原单次致命请求，相隔10tick，小于公开无敌时间0.35秒（60Hz下约21tick），
因此 `ApplyDamage=false` 符合生产规则。不是死亡/复活失效，也不是该敌人伤害可以忽略。
测试准备现基于最近一次真实Damaged tick，等待 `ceil(公开InvulnerabilitySeconds / 实际physicsDelta)+2` tick；
新伤害发生则按新tick重计，最多120次AdvancePhysics调用，未观测伤害时保守从诊断窗口开始计时。
该准备窗口内任何自然Died/RecoveryCompleted或HP归零都会明确失败；不会让自然恢复冒充本次受控恢复。
随后仍只调用一次原致命ApplyDamage，保留原同帧恢复和四次死亡/恢复遥测断言。
只读伤害/死亡/敌人连接诊断保留，退出退订；全部生产AI/Health/无敌规则不变。
这是受控公开API恢复测试的前置修正，不是新增物理坠落或真人实战死亡证据。

`route-checkpoint.log` 的实际正常窗口复跑通过：额外等待13次推进/13个physics tick，等待期无死亡或恢复；
在tick1598、距真实受伤23tick时，单次致命请求返回true，同帧恢复到盆地检查点(2380,2014)。
日志的Died回调读取HP5是已有同步恢复先执行后的状态，不是未发生死亡；Damaged(0)、Died、同帧恢复断言共同记录。
完整五区、四次恢复、两检查点、冰面拒抓、过热、门、Boss重试与结算通过，实际进程退出0、ERROR0/WARNING0，无失焦。
调用参数为普通窗口 `gl_compatibility --max-fps 60`、无fixed-fps/movie；2704次进程回调样本（丢弃前120个间隔），
p50=16.656ms、p95=17.145ms、p99=26.700ms、max=46.714ms，13次>33.3ms、0次>50ms。
这不是GPU帧时或稳定60fps/真人验收；旧失败路线的44.936/51.447ms与更早历史峰值保留，不混算。
等待与诊断日志增加路线耗时，不能用旧帧数冒充本次结果。未启用可选ascent-return分支，无新增18回落采样主张；
可见argv/空movie路径不能独立排除宿主参数改写。

## 最终验证与复跑入口

所有引擎使用本机Godot 4.7.1 Mono，子进程的DOTNET_ROOT/X64指向 `G:/godot/dotnet-sdk`，
DOTNET_ROLL_FORWARD=LatestMajor；`build-checkpoint.log` 为 `dotnet build --no-restore -t:Rebuild`，实际退出0、0错误/警告。
当前DLL的09:39:36 UTC编译时间晚于Task9测试09:38:17的最后源码改动，此后仅改记录文档。
`build-basin.log` 为 `--headless --script res://tools/build/prologue/BuildCanyonBasin.cs` 的实际169/169打包验证；
`import.log` 为本轮导入，退出0、0错误/警告。未重新生成其他扇区。

| 验证 | 引擎主要参数 / 日志 | 实际退出 / ERROR / WARNING |
| --- | --- | --- |
| 完整pre | `--headless --script res://tests/RunPrologueTests.cs -- --phase=pre` / pre-serial-final.log | 0 / 0 / 8 |
| 完整post | 同上 `--phase=post` / post-serial-final.log | 0 / 0 / 3 |
| 正常五区路线 | `--rendering-method gl_compatibility --max-fps 60 --script res://tests/RunRealtimePrologueProbe.cs` / route-checkpoint.log | 0 / 0 / 0 |
| 普通主场景 | `--rendering-method gl_compatibility --max-fps 60 --quit-after 240` / ordinary-main.log | 0 / 0 / 0 |
| 无测试脚本启动 | `--headless --quit-after 120` / headless-main.log | 0 / 0 / 0 |

上述命令还需 `--path .`，工作目录为活动worktree。普通与headless启动通过外部 `verify-startup.ps1` 逐个独立APPDATA运行，
退出值来自Process.ExitCode和执行工具，不是从日志头推断；GUID目录保留在证据目录，不触碰用户常规存档。
pre8条warning为6次中段Basin出生时既有EntranceCombat目标缺失（新净空fixture增加1次）和2条旧负向profile；
post3条为既有故意异常观察者、错误profile、非法Completed→Recovering负向fixture，均保留不隐藏。
最终净空集成实际起点2650、位移128.34204、181次支撑采样、同ID三阶段/零恢复；不要沿用较早pre-verified的184次采样。
post含11路段×3起点、可选Pass/Skip、4个catch返程与75项旧case；没有代替旧四类机关发行审计。

串行说明：较早 `pre-verified/post-verified` 是改Task9诊断之前的串行记录。
09:42排队 `pre-checkpoint/post-checkpoint` 时，未等前一执行session确认退出就启动后者，无法排除短暂重叠；
这两份日志虽然最终各退出0，也不作为最终串行依据。保留该失误记录，确认post关闭后单独重跑
`pre-serial-final`，明确收到退出0后才启动 `post-serial-final`；后者完全退出0后才运行普通启动脚本。
末次进程检查无相关Godot引擎或ffmpeg作业。没有把潜在资源干扰归咎于游戏或删除失败记录。

本轮12文件白名单：README、自动化记录、本记录、CanyonContentBuilder、CanyonBasin场景、RunPrologueTests、
BasinSupplyRuntimeTests、新BasinLaborerClearanceRuntimeTests及UID、BasinLaborerEncounterProbe及本次导入生成UID、
PrologueTask9GeneratedMainTests。原beehave导入、default_bus_layout、artifacts、reference导入、旧UID（包括上轮Sight测试UID）不暂存/不还原。
独立审查已核对场景语义、真实净空测试及Task9等待逻辑，无代码提交阻断；这是本地开发检查点，不是正式里程碑或旧认证完成。

## 局部录像与审看

证据根为 `E:/OurGame/Knight/.evidence/september08-basin-laborer-clearance/`。
`movie-red.log` / `movie-green.log` 对应RTX3060、Vulkan Forward Mobile、固定60fps，分别实际退出1/0。
RED命令包含前后两个rendering-method参数，实际引擎日志确认使用后面的mobile，不把它说成OpenGL录像。
旧版等待420tick超时；新版在第二次攻击进入Recovery后完成，退出条件造成933帧/804帧的不同长度，
不是性能提升或同轨迹等价证明。第二次Recovery尚未完整退出；原AVI仅留外部证据，不入Git。

| 视频 | 帧数 / 时长 | SHA256 |
| --- | --- | --- |
| before.mp4 | 933 / 15.55秒 | `AFF531D9C3F3670D0104972DD330F1A6DA08B95CE32D0AB1D4AA6CF1DF9624EC` |
| after.mp4 | 804 / 13.40秒 | `79B25ED141C392AE1E27CB18495D56E9A0F3FE261830A6F57D5735B27F32C92D` |

主审看前后2Hz图板、普通窗口Active原图和地面上返的upper-arrival原图；独立审看12文件：
旧上层/等待失败、新上层/Telegraph/Active/地面结束、地面收绳/上返落台/奖励/主线返回，以及前后图板。
新版敌人身体、攻击扇顶与平台底分开；补给箱随台移动并保持支撑，实际落台、开箱、生命+1可辨。
最右缘角色部分出画、返回检查点附近多演员重叠仍可见，未用录制构图掩盖。
灰色末格包括测试卸载，黑格为图板补位；不是逐60fps全片、真人首读/审美或性能认证。

## 已知画面边界

台下身体与上层空间已经分开；原大白扇形、贴身武器重叠仍非正式美术。
最右边界站位x2867处，角色仍部分出画，镜头边界本轮未改；这是实际可见欠账，不签署整段攻击首读或真人审美验收。
下一主题优先核查盆地右缘到上升区接缝的提前视野与角色裁切，先实走不盲扩整张地图。
独立只读定位：Basin边界右端2880，玩家中心未越界时仍选Basin并把相机Limit限制在该边界；
Ascent预加载不会自动扩大相机视域。按1280宽/Zoom1/无shake的稳态推算，x2867.57的玩家中心只剩12.43px右余量，
小于胶囊半宽20px，与裁切吻合。下一轮仍须记录真实CanvasTransform/Zoom/Offset验证，不能当成本轮已修。
旧四类机关发行门槛仍保留此前actual3失败，本轮新测试不能替代发行认证。
