# v086 · 常驻增量保留与资源归属验证

工程 `G:/knight/Knight`，统一资源根 `G:/knight`。原十一项目标保持 ACTIVE；本号是开发记录，不是发行版。没有新增正式美术、音频、地图、敌人参数或玩法，不能据本轮测试工作宣称序章已完成。

用户最后更正“等一下，是没看到”有效：窗口显示、修正后的教学遮挡和悬浮灯观感均未获确认。本轮全程无窗口、无系统键鼠输入，无付费、自动化、Git 写入或删除。

## 事实：本轮实际推进

### 升降区不再把正常新增居民判作旧敌人重建

原 RouteEncounter074450 在同一共享 Actors 根增加一位居民后失败。初始12个演员全部保留、HP不变；原波次 Rust ID104824049236/HP3、active/batch0/live1不变，只新增 scrap-heavy ID2285375592368。stable slot `gear-pistons.06` 同时由未挂载变为挂载，但没有 native ID 与 stable slot 的直接关联，不能把两者写成已证明的一一对应。

根据原常驻规格及生产的尸体/离区语义，保留规则改为：**全部初始共享 ID 仍在＋原波次 owned ID 集合精确相同＋原 live count、active、batch0不变**；当前成员必须有效、在树内且未排队删除。允许新居民加入，不允许丢掉/重造原居民、替换旧波次或多生一个 owned 演员。没有删除居民或只筛 owned 求绿。

修改后 RouteEncounter075806 与最终普通 RouteEncounter083037 均完成实际 Gallery133/150、原15保持、返程97/109、HP5→5、同Player、残片不误取。两案随后都在下层退出的原HP断言失败，故整条 RouteEncounter **仍FAIL**。最终失败值：physics973、同Player47294973447、HP5→4、位置(2715.1855,4079.9424)、速度(260,0)、group active/batch0。伤害来源尚未归属，不能猜成摔伤或某一种敌人的攻击。

更后面的 `LeaveAndReenterGear` 仍有旧“所有共享 ID 重入后完全不相交”假设。常驻应保留，波次应退休/重生；这是已识别的测试源码问题，但本次没有运行到它，未把前面的掉血失败假称为该边界的 RED，也没有先修改它。

### 主线出招被阻断：不是只有 Moth 一项假零

只在原危险扫描已有计算点加入可选纯标量回调，并在已有有界 recorder 留下一次“范围/能量/冷却/可攻击/无即时Threat均满足，但安全时间为0”的实际样本。无额外扫描、物理查询、await、输入或准入修改。

Main075203 的 physics6601/loop28 记录13项贡献：4个Rust None0、4个居民Moth Hover/Align None0、原Moth ThreadStunned None0、Bell None0、Skitter None0、Prism None0、Burst Recovery剩.75。故只改原Moth时钟不能解释或解决这次聚合假零。原900战斗仍0J/K、敌HP2/玩家HP3，整条Main未通过。这一次样本不能证明900次决策的贡献始终完全相同。

只读核对表明这些已知被动状态必须先经过完整Telegraph；当前保存时长静态最小约.65，高于原完整J＋6实际裕量＋2采样提前tick约.433333。此为**下一可检验假设，不是已实现的预测修复**。模型捕获配置与Skitter实际持有的可变Resource必须区分；不能热读一个已被替换的作者字段、冻结原可变语义或忽略旧弹/Active burst gap。

### 资源实验：实际赋值字段与默认临时对象不混为一谈

仅 REF 完整Supply诊断声明主线程，并在原Instantiate之后同步用现有 registry 登记实际 `PlayerRoot/Player/Presentation.Profile`。没有新logger、资源图遍历、缓存模式、加载、GC、释放策略或帧等待；借用Profile不Dispose、不跨await保留。普通分支不增加观察。

原完整实验075947：四用例全部返回，五个根都使用已登记的同一Profile serial1/hash3390254/native ID9223372067322201658；其finalizer call128有匹配RETURN。但共有5条 `Handle is not initialized`（SwapGCHandleForType），154 ENTER/154 RETURN，native0/11865ms仍严格FAIL。不能据此释放共享Profile，也不能称资源已修。

后续081519和083141都在第三个原用例进入时FATAL，前两用例返回；末个未返回记录分别是 **PlayerTuning serial0** 的call28/call29，不是已登记Profile1。没有该Tuning的native归属，仍未知；这否定把当前故障锁死为某个PP的做法，不证明PlayerTuning自身就是根因。

## 审查修补：诊断抛异常时，工厂仍须释放自己拥有的根

一次新上下文审查只看指定三份快照差异，发现Important：新增观察可能在工厂返回前抛异常，调用者拿不到刚Instantiate的离树根，可能丢失清理拥有者。仅此进入一次修补轮；没有二次审查或第二轮行为方案。

- 原完整场景注入081349：四原用例先返回，native1/11479ms，只有 `SUPPLY_PROFILE_OWNER_RED`，证明异常后原根仍有效；反例自己的finally清理，不留下故意泄漏。
- 两次复测被上面的原生FATAL挡在控制之前，均不算GREEN。
- 将同一个拥有者保护提取到REF测试工具内；实际工厂调用该保护。独立控制使用真正Pack/Instantiate的微型原生Node，不是mock，也不冒充完整Level。控制放在现有identity-contract，不再给完整Supply加前缀或额外游戏实例；其原四用例/五根序列恢复不变。
- 083905/084111暴露控制前提错误。实际值为根已释放、异常正确、无关节点仍有效/同parent，但 `_Initialize` 中尚未进树。不能要求此时IsInsideTree。原生parent归属先独立断言，然后以正确前提重做未保护RED084240：根仍有效，其余条件满足。
- 恢复同一最小catch：观察失败时释放本工厂根；成功释放后重抛同一个原异常；若释放也抛异常，按原异常、清理异常的顺序聚合。不给Profile做Dispose，不触碰无关根。
- GREEN084404：native0/722ms、无ERROR/WARNING/FATAL、guards0；两个实际实例化根都释放，无关parent保留，两种异常顺序正确；原5个真实PP分配/sink/cap合同与3个资源身份/释放合同继续通过。此证据仅关闭新增观察的清理路径，不证明完整Level、音频清理或引擎GC故障已关闭。

最终差异共四份测试源文件：Main、BasinCounterweight、BasinSupply及identity-contract调用点。没有生产行为改动。

## 本轮全部原生结果

Canonical证据根：`G:/knight/验证证据/october13-stable-claw`；下表时间前缀均为20261024。各目录保留完整UUID、原件和provenance。中间冻结与REF版本分开，不拼成当前完整双路证书。

| 案例 | native / ms | 严格结果 |
| --- | --- | --- |
| models074252 | 0 / 546 | PASS，上一显示更正时已在途 |
| Main075203 | 1 / 170252 | FAIL，原900战斗0J/K |
| models075631 | 0 / 538 | PASS |
| REF Supply075947 | 0 / 11865 | FAIL，四用例返回但5Handle |
| pre080601 | 0 / 173745 | PASS，修补轮之前 |
| REF Supply081349 | 1 / 11479 | 拥有者真实RED，FAIL保留 |
| REF Supply081519 | −1073741795 / 10394 | FATAL，2/4返回 |
| pre081914 | 0 / 163708 | PASS，在最终REF微型控制提取之前 |
| REF Supply083141 | −1073741795 / 10466 | FATAL，2/4返回 |
| post083249 | −1 / 180129 | 原180秒TIMEOUT；34进入/33返回，停index33；ERROR0/预期warning2，无75完整资格 |
| REF identity083748 | 1 / 546 | 未保护控制FAIL；尚混有进树前提，不作为最终独立RED |
| REF identity083905 | 1 / 552 | 错误IsInsideTree前提FAIL |
| REF identity084111 | 1 / 526 | 实际分项证明上述前提错误，FAIL |
| REF identity084240 | 1 / 542 | 正确parent前提下未保护根仍有效，真实RED |
| REF identity084404 | 0 / 722 | PASS：2拥有者＋原PP/身份合同；非游戏验收 |
| Explore084527 | 1 / 67085 | FAIL，physics2861死亡request1/completion0 |
| 最终普通models084825 | 0 / 885 | PASS，恢复普通构建 |

17 canonical原生6严格PASS/11FAIL，构建失败0。一次REF mount-only参数被既有准入提前拒绝，没有构建/原生/案例，不计入17；没有绕过准入增加模式。旧Basin三案074450/075806/083037单列，均native1、非超时。没有把ExpectedFailure、warning或watchdog放宽。

最终普通源上Explore仍失败，Main的战斗修复尚未实施；两次pre及post属于前述冻结，后来的改动仅在REF分支，但不拿它们冒充最终字节冻结版的完整前后段/双路认证。

纯合同重新执行：geometry26/26、focused-admission96/96、diagnostic-consumer33/33，均PASS。

## 保管与剩余风险

- 17 canonical的85/85保护清单对一致、102/102实际封存二进制SHA一致，全部guards0；普通helper9、pre10、REF12。
- 旧Basin三案12/12 runtime清单对一致，42/42 before范围文件一致；因三案失败，after范围manifest均缺，保留这个缺口，不补造。
- 最终普通实况：1776源/892C#、9helper、6binary；新增/缺失/哈希漂移及额外binary均0。
- 最终源清单SHA `1C2960046654B0910413DF88EC2D6FF9C7C3946331EC202310EE0D301F89AF2A`；binary清单SHA `84AE45F95C0D36A491EA54576F45EC0E1FBAE020051165CAAEB1FF53F8549920`；Knight.dll SHA `77568A126C7D60D6EF5B375BF1E3ABEA4231DB549546B08FECDF992DB5D94B90`。
- branch/HEAD保持 `codex/task6-certified-final-20260902T0555` / `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`；index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 不变。diff-check退出0、169条既有LF→CRLF提示、其他输出0。
- Minor延期：新增贡献观察超过32或回调异常时缺完整性标识（本次记录13，未据此保证所有900样本）；历史Title/replay/其他Supply清理遮蔽异常风险、Frozen诊断flush保护继续OPEN。未为修Minor另开修补轮。
- 本轮结束前手动容量检查：新媒体根860284字节，较基线新增856776字节，未达到20GiB提醒；无新图稿/录屏。所有本轮原生拥有者均终止，无遗留测试进程，目标仍ACTIVE，未完成发行。

## 本轮裁决及代价

1. 保留全初始集合并精确核对wave，允许居民增加；代价是实际居民消失仍会失败，需要进一步证据而非放宽。
2. 不做Moth单点时钟修正、不忽略远处/Idle；13项记录推翻单点假设。代价是战斗本轮仍未修，下一须验证全载体真实再武装下界。
3. 不建设新PP来源logger；只用既有单字段registry。代价是其他类型/未登记临时对象仍未知，不能据邻近日志猜owner。
4. 观察失败保留失败、清理本工厂根；若清理本身也失败只证明异常被保留，不承诺任何不可恢复native释放失败也能自动恢复。
5. 将拥有者验证移到同一helper的真实微型原生实例；代价是它不认证完整Level/资源GC，完整Supply故障仍保留。
6. `_Initialize`只要求真实parent/有效性，不要求进树；代价是该控制没有处理/显示资格，不能当作已运行玩家。
7. 不改180秒watchdog或900/150/109等界限，不以缺失marker作成功；代价是完整post、Main和整关继续未验收。

审查未判断的项目由root逐项保留，不静默丢弃：

| 项目 | root裁决与判断错误的代价 |
| --- | --- |
| W/排序 | 沿原实现，无新认证；其他目标排序仍可能阻路。 |
| LOS/receiver | 集合保留不证明爪无遮挡；后续真实K仍须验证。 |
| Intro/Playing生产流程 | 本轮未改，保原等待和拒绝；未覆盖准备异常不得宣称安全。 |
| callback/deferred取消 | 不借新控制认证旧回调；旧越权风险仍需既有专项。 |
| LoadLevel及其他拥有者清理 | 未扩改，原成功/失败原样保留；其他入口仍可能泄漏或遮蔽原异常。 |
| 历史Supply teardown | OPEN；新增局部catch不等于旧音频/退出聚合已修。 |
| 更后重入共享ID不相交 | 已知源码问题，未实际走到；修前方掉血后仍可能在此FAIL。 |
| 下层退出伤害源 | 只确认同角色5→4和实际位置，不猜来源；下一不能盲加盾/改伤害。 |
| Main900策略 | 未修；模型下界候选必须另做真实RED→GREEN，不能丢威胁。 |
| Explore/其他分支 | 最终Explore真实死亡仍FAIL，其他分支不续签；双路未交付。 |
| 完整pre/post及资源 | 中间pre通过不关闭FATAL/超时/RID，75与8owner仍无当前全资格。 |
| 生产AI/HP/地图/美术 | 无改动；本轮没有增加用户可见的正式内容，不冒认精美度提升。 |
| 原十一项/视听/硬件/发行 | 全部最终要求保留，不能用诊断成功替代。 |
| 真人视觉 | NOTSEEN为准；无新增窗口许可，不能验收教学/灯/动作。 |
| 历史dirty tree/存档语义 | 仅指定差异获审，保管另核；无真实存档语义读写或Git发行。 |

## 下一实际依赖顺序

继续同一原十一项目标，不重建自动化：

1. 在原聚合器范围内，用真实模型配置验证已知None状态的再武装下界，保持完整J＋6＋2tick、fresh释放重验、活弹与Active gap的0；Skitter保可变语义，参数漂移必须使资格失效。先做独立反例，再跑原Main，不先改目标/血量/人数/900。
2. 下层退出先归属真实伤害请求，再决定实际键盘避险；保原回返、血量、跳跃/物理预算。之后才修旧wave与常驻重入集合的不同语义。
3. 资源方向转向实际未返回的PlayerTuning归属及既有加载/析构边界；不因PP配对返回、普通pre安静而称故障消失。
4. 原正式分层/动作/场景/敌群机关/UI音频、正常完整双路、相机/资源/硬件/真人及发行全部仍须完成。本轮不新生成或覆盖任何获批美术。
