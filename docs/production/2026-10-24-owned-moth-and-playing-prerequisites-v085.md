# v085 · 原波次身份与真实可操作前提

工程`G:/knight/Knight`、资源根`G:/knight`，原十一项目标ACTIVE，未交付整关。用户最后“等一下，是没看到”为准：没有前台窗口/系统键鼠/新画面认可，教学遮挡及悬浮灯视觉仍待核对。后台键盘驱动不冒充真人试玩。

## 事实：只改两份测试消费者

`PrologueCanyonGeneratedMainRouteTests.cs`和`BasinCounterweightRuntimeTests.cs`；未改生产移动/地图/AI/HP/人数/按键/资源释放/正式图声，仍1776源/892C#。现有目标与规格、所有76普通＋Boss/NPC和原十一项没有缩减。

### 原Moth不再与四种居民混认

修改前重读Main065026的Single多匹配原失败，并确认当前Main字节等于v084 models070420快照（SHA C51EF1094AE3C252120E20A28134813730CF46656F54F05692BCFCDA239477C1）。仍保留SingleOrDefault，筛选改用现存`group.OwnsLiveActor`真实binding；另要求原Scene、无ResidentBinding、直接父级、batch0/live1，后续Live持续验证同ID/归属/HP。没有改First、按距离挑敌或删居民。

Main071310实际输出**五个**候选，不是三个（早期Select-Last截断漏前两行，已按原件更正）：precision-flyer4768034460801、ghost-diver4771909996430、bone-diver4787563137513、bound-ghost4800481596397均owned=False；唯一原Moth16277808616089为owned=True。入口/公共侧路/两个台阶/后平台/诱导/真实K cast10应用0→1/战斗交接均返回；同Player无恢复。玩家途中HP4→3，不声称无伤。

随后原900帧战斗仍FAIL：玩家HP3、原敌HP2，J/K发令均0。900条决策都选对原Moth；868条在攻击范围，333条同时无Threat，但HasRanged一直True、SafeCounterSeconds一直0。loop28/p6599已有范围/冷却/能量/CanAttack/无Threat，仍被mixed窗口拦在J发令之前。已读源码：共享活居民使HasRanged=True，而原Moth在ThreadStunned/Align的None timeline本身贡献0，聚合最小值清零。其他居民是否还有零值来源尚未逐源记录。

这不是已经证明的按键或Grapple模式失效，也未验证真实J能命中。现有完整攻击＋6帧及采样提前量保护不能直接删；下一步须取实际最小值贡献者并选择真实可行交战顺序/完整窗口，不能跳过居民威胁、强切模式、加射程、降敌血或放宽900。本轮没有修改此战斗策略。

### 独立Basin测试不再把“挂载”当“已落地”

先加同步只读值快照，在原8帧之后及原Floor失败之前记录，复用原Floor结果、诊断异常不覆盖原失败；无等待/监听/新物理查询或借用对象Dispose。RouteTransfer071901真实RED：physics9/process8、Intro、Pending/NeedsPreparation=True、Failure/Rejected为空；Player/Motor/Input物理处理全False、sample0、OnFloor=False/slide0。40×72身体底3916恰等于平台顶3916，平台未禁用；几何贴顶并未形成原生接触。

之后仅三个自动新局消费者（RouteTransfer/RouteEncounter/RoutePhysicsCadence）在原Start后复用现有WaitForPlaying，仍在原try/finally清理内。共享Start、合法Title/Continue生命周期不改；不强制状态/开输入/加固定帧，也不动Floor、出生点、150/109物理窗或旧55秒原生上限。

原生结果：RouteTransfer072117完整Gallery134/150、原15保持、返程97/109；RoutePhysicsCadence072229为136/150及96/109。两案HP5→5、残片不误取、同Player、原落地跳1/无额外冲刺爪、最终PistonTwo，严格PASS；都是受控初始出生/headless fixed60，非正常标题主线或真人性能证据。

RouteEncounter072155在(3015.0588,3684.1836)失败于原演员集合保留断言，group当时active=True/batch0。该旧断言比较共享Actors全部CharacterBody2D ID；没有初始/当前ID差值，**既不能称原wave退役，也不能先认定只是居民增减**。下一失败前并列记录原owned IDs、所有居民native IDs/HP、Basin stable slot的Alive/Attached/Epoch/ResetRevision。Snapshot没有ActorInstance，不能按Subtype猜映射；不先过滤掉居民求绿。

## 本轮全部原生与纯合同

Canonical根`G:/knight/验证证据/october13-stable-claw/`，日期20261024；旧Basin根`G:/knight/验证证据/october12-basin-counterweight/`。前面三案是中间冻结，最后四案同最终两文件；Main071310在后续Basin准备前提修改之前，不能宣称最后同冻Main通过。

| Canonical案例 | native退出 / ms | 严格结果 |
| --- | --- | --- |
| Main071310 | 1 / 147206 | 身份/通路/真实K局部通过，原900战斗FAIL |
| models071745 | 0 / 663 | PASS，诊断快照版 |
| models071935 | 0 / 533 | PASS，准备前提版 |
| Explore072635 | 1 / 55572 | 原request1/completion0/p2862，FAIL |
| pre072806 | 0 / 167122 | 完整严格PASS，stderr0字节 |
| post073128 | -1 / 180038 | 原180000ms TIMEOUT，FAIL |
| models073507 | 0 / 547 | 最后普通模型严格PASS |

7canonical原生4PASS/3FAIL、构建失败0，guardFailures全0；最后batch0只是models。两路线的stderr/external镜像同断言不重复计故障。pre原PHASE_OK/finalizer/exit-drain均完成，ERROR/WARNING/Handle/FATAL均0；这是单次严格通过，**不是历史资源根因已修**。

post ERROR/Handle/FATAL0，2个既有负例WARNING，仍超时；registry在engine159655才开始，40enter/39return，index38于179488返回、index39 LockedTargetRevalidationCoversGroupQueueRangeAndInsertedLos于179490进入未返。没有post phase OK/75全项完成；不归因最后用例，不扩大上限。

| 旧Basin独立案例 | runtime.exit | 严格结果 |
| --- | --- | --- |
| RouteTransfer071901 | 1 | 原首Floor RED，Intro/pending实证 |
| RouteTransfer072117 | 0 | 原完整往返PASS |
| RouteEncounter072155 | 1 | 原全集合Retention FAIL，变化归属未知 |
| RoutePhysicsCadence072229 | 0 | 原完整往返PASS |

旧owner无独立Elapsed收据，不从时间戳补造。四案均原保护比较相等，外部godot.log归一化等stdout＋stderr；两失败各1原断言ERROR、两成功0，WARNING/Handle/FATAL全0；都有audio及process-exit drain。纯geometry26/focused96/真实producer消费33本轮重跑PASS，不替代原生或整关。

## 资源调查：已收窄，未实施猜测补丁

现有完整Supply identity入口没有登记实际Player/Profile绑定，直接原样重跑很可能仍unknown。可复用private ManagedSerial的PP分支、现有分配栈和Dispose同serial/hash/rawptr；下一最小方案限定PP，不扩5Definitions/新phase/新源文件：纯托管default-role行、同步current-field行和现有main-safe ObserveResource；仅REF诊断开启，普通Load/时序/cleanup不变，不跨await持资源。

已有真实PP合同可复用no-active-probe那只，保留real_profiles=5及原3资源合同，验证同引用/serial/hash/ptr/现有registry；sink/cap必须使完整性失败、不reset计数或加GC。只能判断未返回serial是否对应该实际字段，不能把绑定角色当native所有者，未匹配仍unknown。本轮未写这些诊断、不新跑已知无新增辨识力的探针，未更改Dispose/cache/引擎或屏蔽FATAL。

## 一次独立审查、全部裁决及代价

新上下文最终审查只比较本轮两测试文件与v084指定快照；无Critical/Important/新Minor，无修补轮/复审。数量/HP对象的事实更正已按原件落实。旧Fixture/Title/replay清理异常保全风险及Frozen flush保护Minor仍开放，不冒称修好。

全部15条Declined的root裁决与代价：

1. W/排序未改，不重新签输入边界。
2. 物理遮挡/receiver资格未变，身份正确不等无遮挡。
3. 仅消费者等真实状态，生产准备拒绝全路径仍不在本审查资格内。
4. callback/deferred演员父级/姿态/序列未改，不扩展证明。
5. LoadLevel调用者try之前的拥有者/双异常不在两差异，不重签。
6. 旧Fixture.Dispose覆盖主错及Title/replay Minor延期，极端清理失败仍有保全风险。
7. Frozen输出flush Minor延期，需独立故障合同，不能归为资源原因。
8. Main900策略不修；实际零窗口/零J原失败保留，代价是主线仍未交付。
9. Explore/Frozen仍p2862失败，未给生存/平衡资格。
10. direct-anchor/resident-shelf/默认返程/fragmentReturn各证据分开；两独立默认往返通过不等全部分支或正常Main。
11. 审查未获最终三案；root补齐2PASS/1RetentionFAIL，不选择性报绿，不猜变化归属。
12. 完整pre本次PASS、post超时，历史Handle/FATAL/RID仍OPEN；单次quiet不关闭资源。
13. 原26＋新50/Boss/NPC/全部十一项/正式视听/硬件真人/发行未因两测试文件获验收。
14. 用户明确没看到，不追认显示，不自动前台/输入。
15. 历史dirty树、完整保管与真实save语义不在代码审查范围；root另查字节清单，只hash、不读语义，不把局部审查等于整树或Git发行资格。

其他执行裁决：保留用户指定dirty非main checkout/root唯一写入构建native，期间冻结源/辅助器/文档；代价是快照归因而非clean commit全树保证。按既有执行计划Task1推进，用apply_patch记账，不跑会提交/删workspace的脚本；完整回归失败时不task-done或缩小十一项目标。

## 保全和下一步

Canonical七案35/35前后清单对一致、42/42实际封存binary重新计算SHA均匹配；source各1776、pre helpers10/其余六案9、user109/continuation0。最后按原inventory规则复算live1776源/892C#、helpers9/binaries6均新增0/缺失0/哈希漂移0，binary目录无额外文件。旧Basin单列4对/案source1776/helper5/bin6/user109全相等，before各14副本全匹配；两GREEN有相同after14副本，两FAIL没有after scope，缺口不补写。旧exit及保存owner路径支持未走超时分支，但不是独立TimedOut布尔收据。

最终source清单SHA`4B31317BC3DDA7F7E58250BF76A3319754783C067E22B22B064A4A9C3D54FBAC`，binary清单`4CDC6C9B62F7D03F93D829322DCD912F6D900646C2D09A04AE4064621E14AFE4`，Knight.dll`6910B85258BE420630F33976BBCA7870ABAD39F195613211EAA1EAC11A2E187D`。HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6、index21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C，分支未变；没有Git写入/打包发行。

Root构建/native已全结束，普通构建保留。无新正式美术/声音/过程媒体、付费、自动化、删除、前台或系统输入。一次手动媒体检查UTC2026-10-23T23:36:18.7637566Z：current860284/base3508/growth856776 bytes、Notify=False/新媒体0，无调度或停工。目标工具本轮ACTIVE，不完成/暂停/阻塞。原十一项/正式视听动作/完整双路/Boss出口/硬件真人/发行全部继续，下一实际战斗窗口、Retention变化归属、PP绑定身份及完整制作，不用局部测试替代最终游戏。

最终`git -c core.whitespace=cr-at-eol diff --check`退出0、169条既有LF→CRLF warning、其他输出0；index再次核对未变。目标工具最后读回ACTIVE13531969tokens/53806s、无显式预算；没有状态变更。
