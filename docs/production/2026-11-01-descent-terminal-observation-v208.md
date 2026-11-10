# v208 · 下降教学终态可观察；既有预警单跳规则保留

## 事实、推理、风险

**事实：** 只改 `G:/knight/Knight` 两份现有测试 CS。新增五项终态差异标记及八个独立字面量控制，在原下降教学最终断言之前输出一次真实当前标量；原断言、输入、等待、生产物理/数值均未改。原十一项目标继续 ACTIVE；v208只是内部工作编号，不是冻结或发布版本。

**推理：** v207 的五项合取失败没有记录哪个条件不成立，不能凭旧日志断言摔伤或恢复。当前 focused 和完整 pre 中该边界均为 mask0，原失败本轮未复现；诊断更明确不等于根因修复。完整 pre 另在原180秒上限超时，仍是严格失败。

**风险：** 一次日志仍有分配/输出成本；最后一次落地历史字段不单独证明伤害来源。当前没有新增正式美术、动作、UI、声音或玩家可感知玩法改动，也没有当前完整双路线资格。历史音频/资源、Main混战、正式视听与真人验收仍 OPEN。

## 最新批准的对应关系

用户对 `call_ba525be9431349b0a1c4bd05269903cf` 的批准已经记在 U-20261101-01，并由 [v199](2026-11-01-warning-timed-hop-v199.md) 实现和实测；此次不重复改同一规则。

- 实际 Main 保存已完成的同阶段零伤格挡与左转事实，随后等待枪手同周期的新鲜来弹线索，才发唯一 Space；不要求起跳时旧近战阶段仍有效。
- 旧立即跳 trigger 只供控制用例，不是实际 Main 入口。当前实际入口使用 `Task10MixedWarningHopTrigger`。
- v199 实测单跳、三发完整胶囊避让、HP2、自然回能与稳定落地；后续完整 Main 仍失败。不能将局部通过改写为整线通关。
- 当前 Main 文件 SHA `C1ED9D16290D37161E8DCA32293154754D32E98CF8FA1EEDCFC7DE08B986D208` 与 v207 相同。本轮没有再次运行 Main/Explore，不借历史通过签当前双路。

## 最小诊断与测试先行

`TerminalFailureMask` 只映射原五个不等式：角色不同1、请求非零2、恢复非零4、HP不为5是8、目标不为ReachBasin是16。不作为原断言的替代或新通过门。八控制的期望为独立字面量 `0/1/2/4/8/16/31/6`，包括负计数仍不等于0。

先用返回0的stub运行729a4b99：构建成功，具名RED精确列出七个非零失败，all-correct没有失败。实现后62efb888通过八控制、旧十一finalizer控制和原真实键盘动作。无编译失败冒认RED。

唯一 fresh whole-change review **C0/I0/M0**；没有修补轮或二审。原五项合取断言逐字保留，新增日志无额外input、await或空间/resolver查询；读取既有HP、身份、计数、目标、时钟、坐标/速度、floor/claw及已提交落地字段。

## 实际下降边界

| 上下文 | 当前 player＝expected | 请求/恢复 | HP/目标 | physics/input | 结果 |
|---|---|---|---|---|---|
| focused 7b162e4d |47546631683|0/0|5 / ReachBasin|509 / 479|mask0，完整focused PASS|
| 完整pre d8a411f8 |3990074951978|0/0|5 / ReachBasin|2327 / 503|mask0，局部下降通过；整体timeout FAIL|

两者均自然停稳、floortrue、ClawIdle、velocity0。最新落地均count3/damage0/Appliedfalse；height分别51.950867、47.08667。冲击采样均NaN/MultipleContacts/unreliable，不能声称是可靠冲击或伤害因果证据。

pre末迹为 BellChainMechanism `chain_floor_to_completion_dais sample=0.2` enter，engine_ms179498；native180063ms触及原180000上限，无总phase完成。它与v207 native164361ms的下降终态断言失败是两个不同结果，均保留；不延长上限，不立即重跑筛绿。

## 同源验证记录

SourceRows1872，原 build180s / focused与输入策略模型55s / pre180s / post300s，执行期间source/helpers冻结。全为隔离headless与内部Godot键盘事件，无fixedFPS/movie/ExpectedFailure。以下均已终态，不以日志里的局部成功替代超时失败。

| 案尾码 | 阶段 | build ms | native ms | 严格结果 |
|---|---|---:|---:|---|
|729a4b99|stub actions|28675|620|具名真实RED|
|62efb888|最终actions|31802|9604|PASS|
|7b162e4d|普通focused descent|28729|17101|PASS|
|d8a411f8|完整pre|30675|180063|timeout FAIL|
|d688fa42|keyboard|34466|20455|PASS；独立headless title7993亦PASS|
|c6a6764c|远程混合策略|26777|3839|PASS|
|a08fbcd9|爪模型子集|26222|481|PASS，不称全游戏模型|
|1f4ea72f|完整post|25393|213993|原300000内PASS|

共8次尝试：6限定PASS、1真实RED、1完整pre timeoutFAIL；最终同源7阶段为6PASS/1FAIL，整套未绿。所有构建0警告/错误，guard=[]。post原75个同名/index有序边界、owner8/nodes1→1、finalizer/drain/phase均齐；仅4个原限定负控WARNING（observer1、profile1、非法Completed→Recovering2），没有ERROR/FATAL。不拿post成功解决历史音频/资源问题。

## 下一处Main取证边界

只读复核v207原Main7df：physics6921/sample6891盾Left、winner为Burst17；6922/sample6892仍Left但winner改为右侧Rust8近战预警；同一physics6922/input6893已实际Right，左侧Burst17接受伤害HP2→1。不是“过了一整物理帧以后才受击”。

关键6922完整候选已被末96行环覆盖，专用candidate journal passes0，不能拿6921旧winner位置当6922当前几何，也不能从剩余寿命当到达时间。下一最小工作是在既有ObserveLateCandidate复制同一capsule/原candidate circle/身份、physics/input/dt、角色速度与实际盾向；不新增扫描或resolver。仅相邻同身份物理边界可派生observedVelocity，首次/重复/缺帧明确unknown；优先保留转盾窗口少量行。随后才决定是否调整驾驶，不凭现有信息直接翻优先级。

## 裁定与未验范围

1. 保留dirty checkout/所有旧失败和基线，不Git/worktree/commit/删除，沿用manual ledger；代价是无clean-release资格。
2. 已批准的预警择时规则已实施，不重复改，也不将其扩成改游戏数值许可；代价是本轮不新增玩法，后续混战必须另按真实证据处理。
3. mask仅诊断，原断言不变；代价是原失败未复现时不能宣称修复。当前标量仅当前边界，不倒填v207未知值。
4. 不增加tick/query/input或等待上限；日志非零开销，代价是不能声称对时序和性能绝对无影响。
5. 静态审查不代替native证据/保管；root实跑、独立审计分责，代价是C0/I0不等于全套通过。
6. 历史pre原因、音频/资源、Main/Explore、正式图声动作UI/硬件真人与原十一项均未验收；代价是不能正式交付、签双路或保证十万付费玩家。
7. 未审无关dirty源、完整checkout、Git发布与通用释放正确性；保持既有边界，代价是本轮不是全项目审查。

本轮新增延期Minor为0；旧v204/v207等延期覆盖不被本轮清除。无GUI/前台/OS输入、真实C存档写入、付费生成、额外崩溃采集、引擎替换、自动化、发布或资料删除。

## 最终保管与容量

独立只读核账8案40清单对、3754份选中源副本、92份helper、48份bin，逐SHA差异0。UTC12:31:21当前1872源项、post专属10helpers、6bin均与最后post相符，最终7案同源同bin。UTC12:32:01按保存creation/path确认17个owned身份（8build＋8engine＋1title）已结束，旧PID当时均不存在，已知KNIGHT工具路径进程0；无进程操作。

sourceManifest `6191BE16616FF299E304FB026A32F291EC6EE52EFE56665260F61701A50EC963`；binaryManifest `4921CFF8FC55316DA7D3A53C89424CF0E208E1AD9799DCB19EDF28D0A689A0B3`；DLL `14781A23B755528EA617B0F2A75E2C2B28EDD7E90DD832079AF5C9313A8DB12A`；postHelpers `C5EF45802948AA866E2659414BDBE4BD8D22B0D980D459CAA292638C213429E7`。

Descent `08D917BDEC900CE1F419A69F3EB31DD49238EB28728F6C5F845D69A77E03373C`，Actions `A99F98C5969EA2AA3CA83AFE0A62F6168162988861ED695807A74279AEFE2A75`。Teardown `26EF0C40…A6E26`、canonical `7A79C29A…69BE19`与基线一致；HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`不变。只读diff-check无空白错误，提示既有LF→CRLF规范化可能性，不作源修改。

媒体手动检查UTC12:31:01为7,427,750bytes/86files，Notify=false/NewThresholds0；未到新20GiB，不新增自动化或停工。不保存常规调试过程图/视频，原始测试日志位于 `G:/knight/验证证据/october13-stable-claw` 各具名案目录。
