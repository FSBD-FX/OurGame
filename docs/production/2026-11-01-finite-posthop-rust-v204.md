# v204 · 主线真实第二次Rust K已完成，必需遭遇仍未清

## 事实、推理与风险

**事实：** 原全部十一项目标ACTIVE，只继续G:/knight/Knight。本轮仅改Main自动试玩及其policy测试两个CS，未改生产玩法、角度优先解析器、键位、地图、敌人/能量/伤害/物理数值或存档。v204是内部工作记录，不是正式发布版本。

**推理：** v202已实证普通Rust可在第一次完整Idle后自然进入新warning，再正常被K中断。Main现在复用原来一次postHop准备机会，在同一Rust经原J变为HP2后使用该能力；不是创造新技能或删除旧one-owner suppression规则。

**风险：** 已完成一次新的真实施放，不等于清遭遇或通关。Main原900循环仍失败；资源Handle/finalizer问题、完整当前双路线、正式视听美术/动作/UI/音频、硬件性能和真人体验仍OPEN。不能用本局部测试减少原十一项或宣称获客人数。

## 有限调度与真实边界

原W/Moth-only准备替换为中立Rust准备，共享原 `mixedPostHopAttempted/Issued/KStage` token：最多准备一次、最多K一次，失败直接stage3，不重新瞄准/查询resolver/增加等待池或帧预算。

- 仅Hop5、原首K/Jet/Burst fatal完成、同一live group-owned Rust/父节点/世界/玩家HP2/恢复0保留、RustHP2、真实第一Rust receipt按原index/value保留时准备。
- warning需正常RustShellThrust、非零同attack ID、当前剩余与原全来源SafeCounter都大于 `distance/ShuttleSpeed+8/Hz`，缺来源/observer fault/unknown或overlapping Active拒绝。
- 当下直接读取能量，有限且≥原两次AttackCost=24、不过热；不拼接事件缓存的旧能量与新时钟，不改原J完整clock门。
- 释放当前held keys一次，记住真实keyboard aim/warning，等待一个原外循环tick。下轮必须当前/raw输入和physical mask中立，真实生产Idle preview的physics/sequence/authority/pose/aim仍新鲜，精确选中同Rust target并且LOS/legacy/available一致，才直接按K。
- 沿原LaterClaw链要求实际新sample/cast/latch/application/effect/Idle及玩家HP/恢复/跳/喷守恒；回Idle另核RustHP2和原第一receipt不变。旧Up/W纯helper及旧suppressed-owner HashSet保留原义。

## 测试先行

新增116合同用例：窗口、当前/raw中立输入、每个physical held bit、非W的真实方向预览；负例包括token已用/Hop未落地/首回执失效/不支撑、非法能量reserve/lead、状态/kind/id/来源/危险观察、stale/future/authority/pose/aim等。

ec741fdf（native518ms）在三个新正例上具名 `MIXED_POSTHOP_RUST_REPEAT_RED`；不是编译失败。纯gate实现后728c567f（3950ms）全部116和原真实mixed fixture严格PASS。原fixture仍Rust/Burst正常动作，未以矩阵冒充实际K。

## Main实证：新的正常回执已经取得

完整Main3d9387b9，native136883ms、exit1/noTimeout/guards[]，原900未清。实际时序：

| 边界 | physics / loop | 事实 |
|---|---|---|
| 原foreign Rust K回执 |6652 /497|cast13、sample6613→6624、应用1/眩晕/Idle、HP2保持|
| 一次中立准备 |6867 /708|同Rust16033951779516，warning7，真实Right，energy26.3332，remaining.33333325，lead.15562372|
| 当前真实预览决策 |6868 /709|ready/fresh/selected_rust均true；原preview physics6867/sample6839，目标16034052442740、authority20、pose不变、无held/extra resolver|
| 新K回Idle |6878 /719|cast14、sample6841→6850、edges14→15、应用1/实际Stunned/Idle、原第一Rust receipt保留，玩家/Rust均HP2|
| 原攻击意图交接 |6879 /720|postHop stage3，ledger2；不是planner“按下K”即算完成|

新receipt Player47546631684/Actor16033951779516/Parent4886263501820/Target16034052442740一致。主线最后仍为同必需Rust HP2，玩家HP2；没有第三次K或降低门限来补通关。

## 之后为何没有J：已有原始记录

postK后180条原决策（6879–7058/loop720–899）无一次同时达到idle完整clock≈.4333333与energy24。足够clock的两段6938–6951、7025–7038，能量分别仅11.733229、22.199924。能量≥24时最大clock只有.3333333。旧35%日志不是实际J门，ModeGrapple也不是键盘J的模式前置；7026已有无盾/Right/静止/strike/can_attack仍被真实reserve+clock门挡住。

最早可见空间变化：6879两者过近dx2.945，原generic `signedGap<8` 退让触发；6880实际A/vx−36.67，6887进入strike时vx已−260，移动意图虽归零却继续惯性滑行；6891首次on_floor=false，6892防守方向又带来A+S/L，6903落到较低处。原resource仅逐tick记on_floor，不是完整capsule corridor，不能把此前所有on_floor=true写成原平台完整支撑。

最后96条late-mask中14条明确失败0x0040，82条失败0x0002并短路后续；该日志dropped399，不能称覆盖所有早期mask。连续180条实际counter_ready=false和资源/clock记录支持“没有出现已满足原J条件却漏发”的结论。

下一独立战术应核有限退让/制动/完整支撑，保持现实惯性和原数字，不再追加第三K、降低24reserve或改成忽略其他敌人危险。当前没有实现这一步。

可用的现有接口已只读核对：FreeMotionForecast.Neutral/Reverse/HeldThenNeutral、Motor真实MotionSettings/ThrusterState.Module，和绑定原floor的TryTask10CapsuleCorridor/MixedGuardFullSupport。下一候选仅“一次取得movement token→预测一tick力及完整反向制动包络在原支撑中心区间→真实反向键→实际中立后一次交接”，占用原900、不直接调用额外90帧等待；预测不是碰撞/支撑证明，一tick退让也未必已进入strike带。资源第一失事件封存仍属独立未实现项。

## 当前回归与保管

canonical SourceRows1872、原build180s/policy与keyboard/actions/models55s/Main600s/pre180s/post300s，源码/helper在每个native运行时冻结。证据根 `G:/knight/验证证据/october13-stable-claw/`。

| case | 阶段 | native ms | 结果 |
|---|---|---:|---|
|170603/ec741fdf|policy|518|新正例真实RED|
|170812/728c567f|policy|3950|合同116＋原mixed严格PASS|
|171114/3d9387b9|完整Main|136883|严格FAIL，局部新K回执已完成但原900未清|
|172136/a3f5fda5|最终源policy|3958|116合同＋原mixed严格PASS|
|172219/dd5b51ef|最终源keyboard|20238|严格PASS，含普通两次Rust K；独立title另计|
|172325/c75c7de8|最终源actions|9514|严格PASS|
|172414/804bdd7a|最终源models|500|爪模型子集严格PASS|
|172450/a986b345|最终源完整pre|180057|原180000上限超时，严格FAIL|
|172828/17dd1231|最终源完整post|212424|原300000预算内严格PASS|

全部9案 **6限定PASS、1真实RED、2FAIL（Main未清与pre超时）**；最终同源7案5限定PASS/2FAIL。Root读取最终policy/keyboard/actions/models/post provenance均exit0/noTimeout/failure=null/guards[]；keyboard另有独立title7812ms通过。pre原180000上限超时180057ms，最后到FallenBellkeeperArena/retry_to_arena_floor/sample0.5进入，无完成链，严格FAIL。后段原75同名/index有序对、owner8/nodes1→1、完整finalizer/drain/phase完成链通过，只有原四限定负控WARNING，无ERROR/FATAL；pre没有原生ERROR也不能算成功。

**整套未绿、此计划清遭遇目标未达成、原十一项未完成**。没有C/I修补或二审；既有资源失败未被当前post通过消除。未重新运行Explore，不借v201旧结果签本轮当前双路资格。没有重跑已饱和的full identity-pre。

## 唯一审查、延期及裁定

唯一fresh whole-change review **C0/I0/M2**，没有C/I修补或二审。两项Minor延期：第一Rust真实capture index0→追加→替换的专门回归控制；准备拒绝/施放完成后实际one-shot不重开transition的控制（当前有TokenAvailable=false标量测试及源码审查）。当前未见重开/重拍原receipt缺陷，不顺手扩成其他修复。

裁定：

1. 保留现codex脏checkout，不Git/worktree/提交/删除/发布；代价是无集成或正式版本资格。
2. 使用原postHop一次token、HP2后真实中立方向，不改玩家机制或另加重试；代价是错误方向/窗口仍可拒绝，实际Main仍未清，计划完成门OPEN。
3. 审查未判断未改生产/地图/数值/解析器、旧脏改动/延期、后续Main根因、资源/journal缺口、回归/Explore、保管/进程、OS/真人/视听性能/发布/原十一项：Root只据实签运行范围，其余保持原门；代价是不把局部K或源码审查推广为完整完成。
4. 本轮Explore延期至必需遭遇进一步完成，不为当前失败Main签通关资格；代价是当前双路线验收OPEN。

资源侧新增静态更正已附v203：identity-pre成功路径也缺journal AssertComplete，不只是timeout早于检查；下一候选是首失事件封存并即时标记、pre成功也验同一sticky状态。此独立诊断代码本轮未改，Handle问题仍未修。

保管：9案45清单对/4394选中source副本/104helper副本/54bin逐SHA相符；早期纯gate GREEN的Main源与最终集成源不同，未混认资格。09:37:04UTC当前1872源项/post consumer10helpers/6bin一致。09:37:10UTC19旧owned创建/路径身份均终结（9build＋9engine＋1title）；18PID不存在，53708已由不同创建时间/路径的node_repl复用，不是旧Godot存活，未操作。KNIGHT工具路径活Godot/dotnet为0。

sourceManifest `AE533C55109A8B05085B9BC6FB0586C2B550A396D59A6E94C18A9AE3FF2946C1`；binaryManifest `B4032BF15D6FC210226D9B7EE9E1E8B0A05AE6D43627A23EEE4EC45BDCACF7EA`；DLL `068FC9E0A8F29281E64AC28823B6E86B8FC4CF2463D0C2AE218157B867331A82`。

当前Main `13179A45CA0808DF7CC0626407B0582EB77107D36B75D6FB11D06FAE28A8F459`、Placement `03E9BD2125A207E1711EC49E462FF4223863C4A647B6CE02630CDC358201900F`；生产/KeyboardAB320F4/canonical7A79/HEAD e7c70498/index21610不变。RustHP2来自实际返还断言，不从receipt的玩家HpBefore/HpAfter冒认敌人血量。无新正式美术/动作/过程媒体、付费、GUI/系统输入、额外原生采集、自动化、Git写入发布、删除或真实存档操作。

09:39:54UTC手动媒体检查7,427,750bytes/86files，Notify=false/NewThresholds0；未达到新20GiB，不建立独立提醒。所有原失败、基线与本轮资料保留。
