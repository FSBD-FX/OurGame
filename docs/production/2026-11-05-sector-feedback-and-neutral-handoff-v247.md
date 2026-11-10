# v247 · 换批中立与跨区反馈提交

状态：本轮生产与定点修补已有实际RED→GREEN，最终同源17阶段已终态15PASS/2FAIL；不能作为完整序章交付或全套稳定证明。唯一工程仍为G:/knight/Knight；原十一项、R-I/R-C/R-P及正常速度/硬件/用户体验验收保持ACTIVE/OPEN。

## 事实：本轮实际改变

1. World在真正成功提交active区域/实例、完成outgoing retirement后通知Audio。初始化和同root刷新不重复通知；预加载/失败激活不假装成功提交。Audio同步清旧Task10反馈、同步区域及action基线，避免下一idle误清新反馈；复活期间保留已经由RecoveryStarted发出的新反馈。
2. 自动试玩只在normal键盘gallery局部拒绝退场/死亡/错owner目标，并在原无目标等待前松开A/D。没有改变实际Motor、选敌角度优先、地图、HP/能量、单跳、原900循环或任何等待预算。这不是生产惯性或角色动作返工。
3. 终审发现并实际复现：postcommit监听器异常会跳过Audio/VFX，稳定检查点元数据误回填、真实恢复busy/input卡住。World提交和Audio清理现在逐监听器隔离、每次失败公开原生WARNING、其余订阅者继续，不重试不明副作用；正常路线任何此类警告仍严格失败。

## 实证：不是从构建成功推断完成

所有案例位于`G:/knight/验证证据/october13-stable-claw/`，原始失败保留。

| 行为 | 实际RED | 局部GREEN/证明边界 |
| --- | --- | --- |
| 同步跨区旧key | `thruster-recovery-compatibility-20261104-233607-477-dc509bd94bde4e0298dc9b39a02de9ab`：Restore返回True/区域Basin，旧key仍2，无await | `...235235-057-34a32dc1283b4df8ad3f45edfae7cee4`14889ms：同步旧key0，同root一次清理，真实EmptySlot/id3跨idle保留，复活Entrance2/4帧，两原delegate通过；后续最终版需另计 |
| 换批等待漏D | `thruster-route-main-diagnostic-20261104-233732-957-e7676b5128a5491d9b31f40a001873f1`：867/899真实D、Current/Raw1，154883ms严格失败 | `...235353-158-4ab3d4c485ff47c6910f1fbfdfc86f06`晚段33对实际启用等待门，A/D及两轴均0、vx0，完整支撑；**原899完整战斗仍FAIL** |
| 提交/清理异常隔离 | `thruster-sector-feedback-faults-20261105-001457-828-6702d45e743a4f01befd2adb1bbfcd56`3726ms：Audio keys2/later0、World已Basin却返回失败、真实恢复完成0/4、busyTrue/inputFalse | `...001910-427-f7ad86e855bd42cca7bb84c8021f52fa`3986ms：两层后续监听继续、keys0、提交元数据正确、恢复完成1/2帧、busyFalse/inputTrue；精确2World＋1Audio故障源/函数/次数匹配 |

独立故障阶段是新负例，不放宽旧正常阶段：ExpectedWarningLogger核对实际源/函数/次数，再打印两个scope；host分别检查stdout/stderr/external精确多重集、唯一成功回执，任何其他警告/ERROR/FATAL/资源泄漏仍失败。真实当前host gate的2正18负包括漏/多警告、错消息、错scope来源/函数/次数、错sink、错误与资源失败、普通阶段拒绝。没有过滤或删改原始日志。

### 保留的夹具失败与修正依据

`234625`与追加被动事实的`234931`均保留FAIL。后一案证明，camp点经真实BodyEntered激活了Basin存档：恢复完成1/3帧、clear与generation未变，只有“仍是入口存档”的前提错误。当前保留初次同步RED的canonical点(1680,3960)，在同root第二次事务、首次await前移至Basin bounds内(1680,3760)，并在恢复前断言未激活的入口存档。没有重置进度、关闭存档触发器、把同区恢复改算跨区或扩大4帧预算。该fixture Motor禁用且有直接事务定位，只证明提交/反馈生命周期，不冒称正常玩家通路。

## 当前Main尚未通过的精确边界

首次修后普通案中，867–899共33对等待返回都真正走到中立断言，位置(4342.146,2323.9612)、bodyX4322.146..4362.146始终在平台4260..4500内，清理后也不再滑出台沿。新owned Laser `20237399362134`在physics8625清理帧才出生，8626新读取HP2，警告/发射/连接/死亡均0；终点stderr的`enemy_health=1`是旧Skitter死亡前缓存，不是LaserHP1。不能把清理帧到场算成899内可操作机会。

下一只归因Skitter末击为什么到loop863/865才发出/连接，核对最近真实恢复周期的J门及外来攻击来源拒绝；保留原HP/能量/单跳/899，不再把等待漏D或激光选择当剩余原因。

只读续核已进一步收束：J816针对resident骨骸俯冲者而非Skitter，现24循环驾驶门到840才再开放；819已选回owned Skitter HP1。844是攻击8警告，850盾仍开；857/858为Retreat且Recovery余时约.283/.267，小于完整J .30＋2/60，按现门不能发J。863首次记录Recover/Recovery .600、eligible=True且同循环发J，能量26.4大于22.8储备门。没有“已经满足门却继续等待”的证据。840–843与859–862具体外层拒绝/外国danger来源仍缺，不能凭缺日志指定原因；约865连接来自终点帧差而非独立来源绑定接触回执。

下一有界观察只复用已算出的skitterCanCounter/eligible/issueJ、nextAttackFrame、当前阶段/余时、gap、盾/攻击、能量/储备与同次danger来源身份；不启用旧策略旗标、不加查询或等待，不据当前资料直接放宽阶段门。

## 终审、裁决与延期

唯一新上下文审查C0/I1/M1。I1已做真实故障注入RED→最小生产隔离GREEN，不安排二审；整套是否绿取决于下方最终同源结果，不借局部修补代签。M1延期：Shutdown回执只证明即时解绑，原生命周期允许后续refresh重新绑定；替换旧source/跨idle的额外控制未完成，不改成永久Shutdown。

全部开发裁决和成本、九项Declined逐条处理见[本计划ledger](../../.superpowers/sdd/2026-11-04-handoff-and-sector-order/progress.md)。关键裁决：在原dirty分支继续，不创建worktree/提交/清理；保留899而非未证实扩时；不把precommit mount当成功边界；故障仅独立阶段精确匹配；不自动重试任意监听器以免重复副作用或清掉新generation。实际坏监听器自身功能可能降级，但明确记录且不应再卡住已提交的复活。

审查未代判的Main/fullsuite/source-binary/保护绑定由root续验；Runner最初完整before缺失，不补造历史快照；正式十一项/R-I/R-C/R-P、正常速度/硬件/真人、匿名资源FATAL、美术来源及其他未审范围均不因此关闭。诊断虽无新等待/危险查询仍有同步开销，不宣称零时序扰动。无关dirty编辑保留，不触其他项目。

美术来源的本次新依据：[限定本地Git核对](2026-11-05-approved-body-mantle-git-history-readonly-v001.md)未在指定路径/最多八条相关路径历史中找到批准原字节，已有reference body仅早期Gate B。用户已选择“暂时找不到，后续重新审阅现有稿”（U-20261105-01）；不再扩搜，后续冻结具体候选再实际审阅，不当作批准当前稿，也不暂停其他无窗口工作。

## 最终同源回归（已实际终态）

唯一串行session30140已实际终态（wrapper exit1是因两案严格失败），不再poll或重启。17阶段15PASS/2FAIL，全部build0、无原生超时，原55/180/300s等时限与严格保护不变；SourceRows1888，无新CS/UID。完整结果为[final-results-v247.json](../../.superpowers/sdd/2026-11-04-handoff-and-sector-order/final-results-v247.json)，脚本只本次顺序运行，没有调度/周期自动化。

| 阶段 | 严格结果 | 原生耗时ms | 保留case目录 |
| --- | --- | ---: | --- |
| thruster-sector-feedback-faults | PASS | 3992 | thruster-sector-feedback-faults-20261105-002125-611-51bc56a3b8914874b1604db0d16ea1ee |
| ranged-models | PASS | 541 | ranged-models-20261105-002207-841-61fb14bf038b408e8e1f3992a0c14d65 |
| thruster-rig-compatibility | PASS | 1284 | thruster-rig-compatibility-20261105-002244-977-1df60493a33d4f80a49dbb7388bf3fcb |
| thruster-ground-gait | PASS | 13628 | thruster-ground-gait-20261105-002324-733-c06d62017d2d48ba8c9fcfa4bed490d5 |
| thruster-presentation-rig | PASS | 18615 | thruster-presentation-rig-20261105-002415-364-69672a7475104d92afb13a4112b4af5d |
| actions | PASS | 9532 | actions-20261105-002509-513-8431bc63f7e94179bff65ed52376b7d2 |
| thruster-formal-claw-phases | PASS | 4884 | thruster-formal-claw-phases-20261105-002553-926-e4c8a59b64fb4e818259b61f6296ed60 |
| thruster-recovery-compatibility | PASS | 15920 | thruster-recovery-compatibility-20261105-002637-969-b169e0fed98941b59cbbcf8059e3ff71 |
| thruster-recovery-feedback | PASS | 3602 | thruster-recovery-feedback-20261105-002734-449-47e97fbbc9bb4b63a9588782f1629d35 |
| prologue-pre | PASS | 153369 | prologue-pre-20261105-002823-997-87b2913e37614c96baef25f030fddae7 |
| prologue-post | PASS | 203658 | prologue-post-20261105-003136-022-193e2912de8b4c9fb27902d2e03ecbe0 |
| resident-geometry | FAIL | 8835 | resident-geometry-20261105-003554-246-ea1e50cf59c24ec18ee5c57bb7515cf2 |
| resident-save-compatibility | PASS | 12967 | resident-save-compatibility-20261105-003709-579-4b414d03d7a544d58622205580963cbd |
| keyboard | PASS | 20272 | keyboard-20261105-003803-228-c35762634b0e493db0a1536e7cbdb835 |
| models | PASS | 547 | models-20261105-003910-663-3f30cd9623d84414a765eb2871c4769b |
| thruster-route-main-diagnostic | FAIL | 180265 | thruster-route-main-diagnostic-20261105-003951-568-4fceb085ae93485ead9893c10b48edc7 |
| thruster-route-exploration-diagnostic | PASS | 333637 | thruster-route-exploration-diagnostic-20261105-004334-126-617ab4154f8949a7957a3b57450a47f1 |

两FAIL精确保留：resident-geometry在8835ms以原生-1073741795退出，gchandle.is_released()/mono_object_disposed_baseref cpp1788、GodotObject.Dispose/Finalize；资源身份与因果未证，不能归因本轮或宣称引擎已修。Main在180265ms以原899未清摆锤区失败，HP2/恢复0/0、vx0；current日志867–899各33条before与after满足中立门且0不良输入/支撑样本，ownedLaser20204767679828于8625清理帧才BOUND。原state缓存enemy_health1仍非新Laser健康证明。没有放宽或重跑筛绿。

当前Explore333637ms通过：从title、同player47563408900，23节点/11挑战，11激活/10击败，Boss28372201638627真实Died与arena/completion各1，17961受伤/死亡、18140出口完成、18201为D保持60采样终点。body(7321.191,1043.9327,40,72)由floor(6080,1116,1600,40)支撑，位置(7341.191,1079.9327)不变；完成后physics停用，缓存vx260不是仍在移动。存档前后Boss与完成状态正确，held前后SHA均D68DEC7993D42D14D26D4F98E88C00B907350568CA6F6B52E552E87F08A56779。proofKind=automated、syntheticFixture=false，但human/operationCoverageComplete/fullGoalAccepted均false；不代表全部76敌人/操作/正式画面或完整目标验收。

### 当前绑定与留存

- 17×5=85组source/helper/user/continuation/binary前后保护清单文本逐组相同。user/continuation各跨17案唯一清单；109默认应用数据保护未变。
- 全17案source清单同一SHA256：5F05EA5AD3AC285D8FC859EEC6A6C2DCFCB6FA27AAD1D1D05C42A3AD7A758079；当前1888文件逐项重哈希0差异。
- 六binary清单全17案同一SHA256：43461CF5B78372EEF09A736B85BE77A8CC31B8E6C3D4B1A491D20D0C149C4C85；当前Debug六文件重哈希0差异；21条去重helper当前重哈希0差异。
- 除新fault阶段精确3注入warning与原post四条限定负例warning外，没有新增诊断豁免；正常Main/Explore仍全原始诊断否决。
- 正常汇报前媒体Check：7427750字节/86文件，Notify=false；本轮没有新增图稿/视频，未到新增20GiB档位，无自动化或停工。

唯一审查I1局部修补已由真实RED→GREEN及本次对应阶段验证；整套仍非绿色，Task3/完整序章不作complete。继承v246非零Aim/非Dash落地组合、v243接管实际帧/同攻击翻面Up等覆盖欠账也没有被本轮清理控制代签。

## 授权与完整交付边界

本轮仅无窗口构建/测试、既有内部输入与隔离存储，未启动GUI、发送系统键鼠、拍图/录像、付费生成、额外崩溃采集、自动化、Git发布或资料删除，未动真实存档。没有新正式美术、惯性/重力调参或用户认可；不能把程序基础及后台通过当作精美、优雅、流畅或完整序章验收。完整范围继续见[原十一项总待办](2026-10-18-project-remaining-backlog-v001.md)和[动作/物理返工要求](2026-11-03-character-motion-rework-requirements-v001.md)。
