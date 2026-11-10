# v079 · 首战身份、真实恢复与横廊落台

2026-10-24。唯一工程 `G:/knight/Knight`；原十一项目标继续ACTIVE。本轮是实际修正与局部路线进展，不是序章完成或试玩交付。用户最新“没看到”有效，未弹窗、未发送系统输入，未取得新的显示/画面认可。

## 实际改变及其证据

### 首战教学不再误认游荡居民

`HasNearbyFirstCombat` 原先寻找Actors内第一个Rust类型；原敌人死亡并移除后，同类型常驻居民会接替它，战斗提示错误地继续显示。现在V2只认原 `LowCeilingBridgeEncounter` 的活跃成员；legacy布局仍认原保存路径。角色状态、HP、屏内、距离340/400、层高100、原生LOS等限制全部保留，不隐藏/删除居民、不豁免挡射线的身体。

- `first-combat-20261024-024317-575-0a82ab96dc17495a9ab9e0e329be90ef`：同步失败射线实际打中右侧resident身体，不能据此改LOS。受控UI测试改在原角色左侧安排原敌人，真实J仍在右侧证明，真实K仍命中原地形，正常路线声明仍false。
- `first-combat-20261024-030517-345-2150916d143b45f3b6af1b9464e22dc8`：实际RED，原group complete=true/live0，另一个可见居民仍存活，旧教学仍显示战斗。
- `first-combat-20261024-031221-620-3e9e4d37eacf47b7a99742063255c73e`、`first-combat-20261024-033111-761-57d32e60b3ec4898b4a6662ec51ed7ae`：完整原专项严格PASS。死亡后上下文为空；两次恢复后新原波次实例与紧凑重试教学正常；原范围/遮挡/高度/实际K/八语言字号组合仍执行。

### 完成存档后新局的第一次死亡：真实Playing再验证

旧测试在Intro、居民仍准备且场景隐藏时就致死；生产此时正确拒绝恢复请求。不是已证明“正常Playing死亡不恢复”的生产故障。

- `completion-flow-20261024-024745-816-7053ebfc1a0e47a5a4875027ce643dea`：实际RED，physics31 Intro/Pendingtrue，真实HP5→0但request0，physics37仍未提交。
- 保留原setup延迟，然后等待真实Playing；真实致死一次，沿现有180physics提交上限观察，不增加全post180秒。加强同一Player、原始起点、未推进checkpoint、完整HP、请求/完成各一次、离开伤害回调后完成的断言，拥有者失败清理保留原错。
- `completion-flow-20261024-030428-810-846ec439ad2a42939123958ed7917eee`、`completion-flow-20261024-033029-295-dc7d7609b3db41bdad174adf4203d156`：原五delegate严格PASS，真实首次死亡提交均10physics。Title/replay保留原延迟后也等待实际Playing。10帧是本次观察，不是性能承诺。
- canonical owner同时要求原五delegate及唯一、锚定、1..180范围的实际恢复收据；缺失或重复不能放行。

### SteamValve保护：修正一个测试坐标，不改危险或通路

真实中心3160/4040、半径22产生 `(3138,4018,44,44)`；旧预期X3139偏1px。实际manual/变换控制也支持3138。只改这一测试字面量，没有放宽tolerance或改生产保护范围。

`resident-geometry-20261024-025738-301-23762fd5da894982853dec4a2095f98f` 中原51环境、19来源/57玩家控制/11目标/100保护区及5档案空间控制返回；之后旧场景Probe重载期间gchandle FATAL，native=-1073741795/8679ms。完整geometry仍严格FAIL。

只读追踪将活动边界定位到Body census→OriginalSpawnReservations→Probe/GD.Load及Instantiate；前一个Archive刚释放离树整Level、重建catalog与旧探针。当前Load不是finalizer对象身份；未证明借入资源被双Free/Dispose。没有改缓存、GC、资源寿命或引擎来求安静。

### 真实主线已落稳横廊，返回仍受阻

已有150帧重放显示Swing借力可到Gallery；不再追逐内部失败的第二K候选。正常驾驶器改为原生同cast Arrived/Swing→新鲜A+Space→D→真实完整胶囊和Gallery上法线接触→松D自然减速。预测使用当前Motor/推进器设置，实际停止还要求raw/sample轴均0、A/D释放、clawIdle、无推进/借力惯性；原45帧地板门和15帧保持仍在同一首次锁定150帧内。只读实际残片形状/拾取状态，不修改物品。直接Gallery锚点分支及两种诊断保持独立；借用slide碰撞不Dispose，新建TestMove探针由本方法拥有。

`thruster-route-main-diagnostic-20261024-032550-258-b8d3b78249a64bd389603b9fb1d3018f`：从真实标题开始的同一Player47127201284，在135/150落稳到 `(3084.3445,3719.9336)`，HP4→4，普通跳/额外冲刺0，残片未误拾取，15保持通过。然后原 `Walk(2975,60)` 失败，末位置 `(3028.003,3723.9229)`；native1/82159ms，恢复0/0。此案严格FAIL，不算Main或完整往返通过。

保存RightShelf X2856..3008/Y3720..3744，Gallery左缘3040、顶3756，水平间隙32小于胶囊直径40，台阶高36。失败X与Shelf右缘+半径20一致，但尚无失败现场collider身份，不把几何推理写成已证明碰撞根因。先前无障碍26tick估算撤回。下次在现有post-Motor观察器、撤键之前取最后return的输入、身体及精确碰撞标量；不延长60/45、不盲加跳跃或改地形。零普通跳是该内部脚本断言，不自动等同用户禁止普通跳，也不能为了绿测试静默删除。

## 完整回归与最后边界

全部原件位于 `G:/knight/验证证据/october13-stable-claw/`，上述完整目录名可直接定位。

| 项目 | 实际结果 | 边界 |
| --- | --- | --- |
| 最后completion-flow033029 | 严格PASS，7783ms | 原五delegate、真实首次死亡10physics；不是整关 |
| 最后first-combat033111 | 严格PASS，8068ms | 原波次身份与原全部控制；受控UI非正常路线 |
| pre031435、033151 | 严格FAIL，97825/99870ms | 通过首战后在落台保护断言失败；资源错误保留 |
| 最后pre033537 | native0/170490ms，严格FAIL | 全部pre行为/phase返回，但stderr有10条Handle is not initialized，无WARNING/FATAL |
| post031648 | -1/180091ms TIMEOUT | registry0在engine158493ms才进入，43enter未返回；不增加180秒，不归因全部于VFX |
| 最后models033906 | 严格PASS，549ms | 当前普通构建；不是视听或整关验收 |
| geometry025738 | FATAL严格FAIL | 上述57控制不等于全case通过 |
| normal Main032550 | 严格FAIL | 实际横廊落台成立，旧返回仍失败；Explore未重跑 |

落台保护的失败观测033151为Intro/Pendingtrue、failure空、World/scope/platform/collision全不可见、合法160×32/layer33/catchShelf=true、regions0。生产适配器本来就拒绝隐藏地形。最小修正只在原case现有try/finally内等待真实Playing，保留原3Process setup；不改可见性或区域计算。最后033537不再有该失败，全部pre行为返回；资源错误使严格资格仍失败。

本轮共20案：1次构建严格失败（旧第二cast变量CS0219，编译成功但owner拒绝警告；删无用状态后安静重建，没有启动引擎或伪造fresh二进制对），19次原生中7严格PASS、12严格FAIL。历史错误/超时与所有本轮RED保留，不拼成全绿。纯入口96例、实际旧producer日志消费33例重新PASS，均非新正常路线证明。

## 一次独立审查、裁决与代价

新上下文审查未发现本轮新增Critical/Important实现缺陷；完整返回仍为Important未完成验收，不是可忽略问题。根执行者保留失败，不发第二次审查。

- Ruling：原同类型居民不能代替首战原群组成员；缺原成员则收起教学。代价：错误的群组接线会漏提示，需保留原路径/生命周期回归，而非隐藏敌人。
- Ruling：受控UI前提可在左侧摆原敌人以保留真实LOS，真实J/实际K另证；不当作主线。代价：不能据此保证正常路线教学时机。
- Ruling：真实Playing和现有180physics恢复提交取代旧固定帧猜测，报告实际10帧而不承诺3秒或FPS。代价：整关恢复延迟、硬件和真人仍待测。
- Ruling：SteamValve按独立中心/半径修1px预期，不动生产。代价：当前精确夹具以保存形状为准，后续改形状需更新有依据的预期。
- Ruling：内部第二K并非用户要求，换为现有Swing落台且保留原限制；旧返回失败不删除。代价：仅落台成立、整关仍不可交付；下一动作/地形若需变更必须先解释真实障碍和约束。
- Ruling：Boost发布先于ApplyMotor，不能用发布前Vy当实际施加后速度；新鲜输入收据与后续真实上升分开。代价：两类观察都必须保留，不能以任一个替代另一个。
- Ruling：保护测试要在实际可见的Playing前提运行，不强制开场景或改算法。代价：准备失败照旧失败，完整测试耗时仍受180秒限制。
- Minor deferred：Title/replay旧case在失败时的自有fixture清理包裹仍比first-death弱，可能留到进程结束；下次触及该case再做失败路径验证。未证明它是资源崩溃原因，不借机改生产资源寿命。

审查所有Declined项均保留并裁决如下：

1. 完整Main/Explore、76敌人/Boss/ECHO和十一项：未完整证明，继续OPEN。代价：不能交付或宣传完成。
2. 真人显示、正式美术/声音、正常速度、目标硬件/60FPS：headless不能证明，没看到记录不追认。代价：画面/动作质量尚无真人验收。
3. GCHandle/FATAL/RID根因：历史及当前失败继续OPEN，quiet案不修复。代价：运行仍有崩溃风险。
4. 保护909原因：审查时仅是假说；随后033151实际确认隐藏准备，最小consumer修正后033537全pre返回。代价：仅修测试前提，未凭此宣称所有UI保护/可读性已完成。
5. 完整geometry：57控制后仍FATAL，资格不通过。代价：后半覆盖缺失。
6. post registry：180秒超时，未完成75案例。代价：完整回归覆盖缺失，不延时绕过。
7. 真实存档语义/迁移/Git及其他dirty文件：未改/未读语义、不擅自归档或审查旧改动。代价：此审查不是存档迁移/发行版本批准。
8. 任意坏场景、未来多首战成员、其他生命周期及新legacy原生覆盖：仅认当前已测路径。代价：未来改定义需新对应测试，不承诺穷尽兼容。
9. P1退出回调/延后移除及LoadLevel故障：此前代码/证据保留，本轮未重新原生测这些分支。代价：不冒认本轮新资格。
10. 全历史保管和dirty归因：审查者未重hash全历史；根执行者对本轮20案逐对核验。代价：只证明本轮快照及当前字节，不把整个脏树当独立审查通过。

现有项目约束要求原位制作、不Git写入/删资料；因此计划账本和证据保留，不执行通用worktree/提交/清理助手。代价是依赖逐案快照归因，不能假称干净Git提交范围。

## 保管、媒体与下一步

20案99对现存source/helper/user/continuation/binary清单逐字节相同；唯一构建警告案缺fresh binary pair如实保留。19原生共114份封存二进制逐项匹配。最后普通1776源/892C#、9辅助路径/6二进制live漂移0。

- source manifest：`CE25C0CBAB56F63B3B5CFA8CCEF404FDA435C6498602DA63A78F66B892054730`
- binary manifest：`7A5DE12E33F69354D018C740B84D6D8C3B5291FEC7C734BE36CBA93AB7753CFC`
- Knight.dll：`BDDC51810025004EEC2F599A39C866B5451311FB63EB06EF1B822C64BDADA21D`
- HEAD/branch不变；index仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。只读tracked diff检查0，169条既有LF/CRLF提示未导致改写配置/文件。

UTC19:41:07手动媒体检查一次，current860284/base3508/growth856776 bytes、Notify=false；没有新增过程媒体/图像/声音、付费、自动化、Git写入或资料删除。root自有构建/原生均已结束，普通构建保留；不操作另一个UE工程。

下一步先在旧返回的实际失败现场收精确contact/输入/身体标量，决定符合真实地图与用户既有能力的返回路径；保留本次落台135/150及残片/HP证据。资源句柄根因、全post成本来源、正常Main+Explore/旧敌人与全50共存、正式分层美术动作/UI声音、相机/视听/硬件/真人及授权打包GitHub均继续。目标不缩为测试通过，不标完整十一项完成。
