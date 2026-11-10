# v241 · 居民出生的同步端点查询复用

状态：已接实际生产消费者，原前段44项本次通过；最终同源码/六binary清单十一门**8严格PASS/3FAIL**。**原十一项与R-I/R-C/R-P继续ACTIVE/OPEN，未作正式交付、美术/动作/手感或硬件FPS认证。** 唯一工程G:/knight/Knight，仅无窗口、隔离存储、原内部键盘事件；无付费/GUI/系统输入/额外dump/新媒体/自动化/发布/删除/真实存档操作。

## 真正改变了什么

`CanyonResidentManager.CheckAdmission`现在把home和原全部patrol段交给同步`CheckPatrol`。每次方法内部用完整精确Transform作键，只复用不可变的body bounds/transform/Issues/双身份support结果；方法返回即丢弃表，不持有native资源。`BodyCheckCount`在实际执行CheckBody处计数，不另算Distinct。

- 原公开CheckBody/CheckCorridor仍每次fresh；通道core仍逐条检查零长/往返、keepOut、完整支撑、native占据和电场；start/end前缀及错误顺序保留。
- 两次CheckAdmission仍分别位于挂载/Ready/activation前后，各自重新采集环境和新建表，没有跨事务缓存。
- 不改76普通目标、Boss/NPC、出生策略/版图、危险/伤害、HP/键位、输入/等待或原预算。不是通过少生敌人或省略安全判据获得提速。
- 仅两个生产CS与一个测试CS修改，source输入文件数量仍1888；不新增全局缓存/日志/资源引用。

## 实际测试与旧夹具迁移

原始证据在`G:/knight/验证证据/october13-stable-claw/`，全部失败保留：

1. `resident-geometry-20261104-144255-183-b29233b6b7d54d3892d59d2393f255c0`：build0/30858ms，engine1/634ms，缺新批次入口的行为RED。
2. `resident-geometry-20261104-144521-334-a3cc8d64c56f4118971a3e1f6697ae8c`：engine1/36764ms。新20组208检查通过，但旧clear正例800,604被未改的public CheckBody因真实springboard预留拒绝，整套FAIL；不称全部绿色。
3. `resident-geometry-20261104-145616-897-dc401b22fc0e4b61ae5fae373cbe1d86`：完整原geometry入口严格PASS/37700ms、stderr空；新20/208、原29corridor及新增2原址危险控制均通过。随后才接manager实际路径。

新控制含重复home/两点（真实body调用2、原fresh9）、stationary（1、原fresh5）且所有零长段保留；完整及1.00001/1.000004微小basis、失败前缀/无效intended、中间薄墙/同体不同shape/缺口/容量/电场、跨调用私有shape/同keepOut对象/世界变动，以及真实player body/hurtbox与foreign混合。比较bounds、完整transform、Issues顺序、clear/proof flags、support collider+shape+rect及每条corridor顺序，不只比较数量。

旧positive迁移是测试适配，不是改变游戏：当前保存PracticeSpringboard在780,636，child0,-6/48×12，保留区756,624,48,12与Rust原800,604的身体实交22×12；ground正反例迁到同FarBank的928↔1040，仍传完整environment并验全支撑。旧Moth928,450交fixedGear870,400,60,60，三个air正例整体Y+50。原800/928450另保留实际Body拒绝与start/end前缀，原cases29接收器不改；所有原危险和安全条件保留。

微计时只作诊断：两次20组fresh/batch总ticks分别9242122/4226631与8400210/3086837；包含反射/调用等成本，非稳定微基准阈值，不推硬件FPS或全关速度。

## 实际生产路径与加载读数

接manager后的三个原门均严格PASS：

| 阶段/完整案例名 | 原生ms | 结果 |
| --- | ---: | --- |
| resident-level-lifecycle-20261104-145850-651-50377bf27e4d43dfaf2854b2277bb2d0 | 5949 | PASS，0ERROR/0WARNING |
| resident-auto-start-20261104-145937-245-4735fd240b60499a86424d01a276a39d | 25053 | PASS，1原Intro→Intro负例WARNING |
| ranged-camera-ground-20261104-150043-382-d0fc3ae184164db591596f9251b11e85 | 21505 | PASS，0ERROR/0WARNING |

真实postmount改变旧源shape、重激活Hitbox仍被后检查拒绝，回滚/恢复后重试等原约束保留。当前camera三cycle的原8组等待区域为8560/3599/3306ms，共15465ms，physics57/44/38、process各8；v239132953为24088ms、整体29486ms。当前整体21505ms，本次观察有缩短，但不是受控硬件A/B，不能将全部差值都当成已证明的cache因果或宣称FPS达标。

## 当前原pre

`prologue-pre-20261104-150449-488-894beeb983424d9eb431a0fb3c35917f`严格PASS/164291ms，原44个top-level均返回，最后Traversal23942ms，在163917ms返回；原180秒门未扩，stderr空。下降教学0Damage/0Heal/0Omitted/Failure null；旧HP4未复现、不称已修。一次通过不关闭历史Handle/匿名FATAL、超时稳定性或正常速度体验。

## 一次独立审查与延期

指定三文件新增/提取/接线块审查C0/I0/M1，无阻塞修补、无二审。M1延期：新batch的invalid仅覆盖intended，后续可补无效patrol点往返的end/start.invalid_transform literal控制；public fresh原NaN端点、缺actor、缺inventory仍在。未发现当前错误，不把可选测试补强扩成本轮新玩法。

审查不覆盖整dirty worktree/历史逐字基线、native日志真实性、长期泄漏/崩溃、任意多线程/自定义可变列表/未来新增回调或全关体验；当前结论限定已复制只读Candidate、现同步调用链和实际差分。若未来改变context稳定性须重审，不把局部表升级成跨帧通用缓存。所有原postmount/start/lifecycle及v240来源观察继续保留。

## 最终同版宽门：十一门8PASS/3FAIL

已依原预算完成同版消费者、geometry、存档/恢复、完整pre/post、爪models子集和普通Main/Explore；以下全部终态，未重跑筛绿：

| 阶段/完整案例目录名 | 原生ms | 严格结果 |
| --- | ---: | --- |
| resident-level-lifecycle-20261104-145850-651-50377bf27e4d43dfaf2854b2277bb2d0 | 5949 | PASS |
| resident-auto-start-20261104-145937-245-4735fd240b60499a86424d01a276a39d | 25053 | PASS，1原负例警告 |
| ranged-camera-ground-20261104-150043-382-d0fc3ae184164db591596f9251b11e85 | 21505 | PASS |
| prologue-pre-20261104-150449-488-894beeb983424d9eb431a0fb3c35917f | 164291 | PASS，原44项/180秒 |
| resident-geometry-20261104-151149-216-4fc4626ade934a24bbdef0ddc3728543 | 9825 | FAIL，原生FATAL |
| resident-save-compatibility-20261104-151243-885-e6043d3e3390493b837e25a2f2afcef7 | 15197 | PASS |
| thruster-recovery-compatibility-20261104-151343-377-8aadd7c37c3d4a35b0112766adbc8e5a | 4730 | FAIL，首次EnergyEmpty提示缺失 |
| prologue-post-20261104-151434-321-1fc55d8dd1d04d9f94cce830e287b58c | 197449 | PASS，原300秒 |
| models-20261104-151836-919-dc214539a48e4527b24366f7a90d3577 | 3813 | PASS，仅爪模型/到达几何子集 |
| thruster-route-main-diagnostic-20261104-152002-581-e0e7cf3adf6541a1ac19fb876bc3a063 | 150930 | FAIL，原配重战斗899未清 |
| thruster-route-exploration-diagnostic-20261104-152328-161-62635affae134a7d828473efd45b94e8 | 334287 | PASS，经Boss/实体支撑出口 |

十一案source1888清单SHA256均`08E5381E8184D43D27B91DBCA3CEB3D10B67C8AFC69595510B44C726014CB4B3`，六fresh binary清单均`40FCCE76FB0AEBB81D86FBC8858EDAFA763CECE69C845D04480BAC4C43517E33`。这是清单文件SHA，不冒充单程序集或发行包。各案source/helper/user/continuation前后一致；终态后逐一重读1888当前文件与最后清单一致，真实玩家文件未动，无剩余KNIGHT引擎进程。review后的源冻结，未靠换源码拼绿。

post四WARNING均为原明确负例：throwing ClawFrameChanged observer、VisualScene非法根、两次Completed→Recovering拒绝；auto-start一条为原Intro→Intro拒绝。三失败各有一条ERROR（geometry含FATAL），没有因process exit或success marker忽略它们。

最终geometry复验`151149-216-4fc4626ade934a24bbdef0ddc3728543`已严格FAIL：9825ms、非timeout、exit -1073741795，FATAL gchandle.is_released / mono_object_disposed_baseref，GodotObject.Finalize→GC.RunFinalizers。最后成功记录到archive，尚未到新增批次控制，不能据最近日志定位具体资源或根因；不重复运行筛绿。它证明当前同版资源稳定仍未通过，早先geometry通过和加载缩短不能覆盖此失败。未新增dump采集、改引擎或扩大授权，原始日志保留，其他隔离阶段继续。

### 两条普通路线的实际边界

Main仍在原899配重战斗门失败：7次J命中、3次爪、Rust余HP1，原错误现场玩家HP2；当前能量约0.14–0.157，未改原攻击/防守/储备门。失败没有生成完成result JSON，不能宣称Main通关、无恢复或真人不可通关。

Explore的`storage/task10-route-result.json`来自同一Player/标题入口，23次路线访问、11挑战、Boss与SafeExit均true、恢复请求/完成0/0。真实玩家第99击击败Boss，实体floor6080,1116,1600,40支撑出口；Completion计数/事件均1，向右保持60输入帧后位置及存档SHA保持。该路线有11次遭遇激活/10次击败，未清配重遭遇，不是全敌群/全操作覆盖；`operationCoverageComplete`、`humanValidated`、`fullGoalAccepted`均false。两个普通入口均无PROBE/旧post-prefix环境，未注入实验回充策略。

### 新恢复兼容失败的下一入口

`Task10ActionCueBaselineAndReentryAreExact`在首次拒绝期望EnergyEmpty/vfx1，实际audio空/vfx0，4730ms退出1；随后同方法重绑/成功攻击/两次重入未追加失败，第一方法汇总异常使后续ProductionPublishers方法未执行。标准post仅调用AudioDirectorTests.Run的值帧映射，**不能据post通过关闭此特定生产夹具失败**。

只读核对最有依据的前提缺口：夹具只等3 PhysicsFrame，没有确认Playing/居民准备完成/Input已采样，而真实启动跨帧并在BeginPlaying重新启用Motor/Input/Combat；Intro下Audio消费者合法不呈现cue。当前日志没有首次按键处的flow/input/拒绝事件，因此它仍是待检验假设，不归因新缓存，也不直接认定消费者漏播。下一在原独立Task10入口或同phase的第一次耗能/Primary tap附近记录有限同步状态与PrimaryRejected/RejectionChanged/AttackStarted/Task10CueEmitted；若确证早于Playing，再按真实就绪迁移夹具，不加任意sleep、不放宽断言、不补播。

## 后续与未完成范围

本轮真正优化了生产出生计算并取得完整pre/post与当前探索证据，但当前geometry原生FATAL、首次cue兼容和Main均未闭合，旧HP4也未归因。Task1实现/专项已证，完整套件仍红，不签complete、正式冻结或外部归档。原十一项、正式分层美术/角色特色与连续动作、惯性与操控、正常速度UI/音频/相机、硬件性能和用户试玩仍保持原范围；没有新美术或手感调参，不以本轮后台性能工作替代这些要求。

[执行账](../../.superpowers/sdd/2026-11-04-resident-local-endpoint-reuse/progress.md) · [设计边界](../superpowers/specs/2026-11-04-resident-local-endpoint-reuse-design.md) · [原十一项](2026-10-18-project-remaining-backlog-v001.md)
