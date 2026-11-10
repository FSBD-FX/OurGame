# v242 · 正式提示用例的真实启动、空间和发布边界

状态：原恢复兼容两个方法本次已实际完整通过，一次独立范围审查C0/I0/M0；最终同版十门**7严格PASS/3FAIL**，**不是完整序章或动作/物理手感已完成**。唯一G:/knight/Knight；仅无窗口内部输入、隔离存储，无GUI/系统输入/付费/额外dump/新媒体/自动化/发布/删除/真实存档操作。

## 本轮实际改动与边界

只修改`tests/integration/PrologueAudioDirectorTests.cs`的两个Task10生产组件夹具；没有改正式输入、音效/VFX、爪/推进/动力、地图、敌群、HP或任何数值。仍是1888个原生输入文件，原runner、原时限与严格错误拒收器不变。这是修复之前不能真实执行完整提示链的验证，不冒充新增玩法、美术或手感。

- 启动：保留原3/2帧观察，随后等待实际Playing/居民准备完成与一次真实InputReader新采样，才固定组件夹具的Motor；原正式游戏的激活顺序不改。
- 首次提示：追加Slot2实际被消费、Slot1还原，以及首次PrimaryRejected/RejectionChanged/AttackStarted/Task10CueEmitted精确1/1/0/1。原首事件无假cue、第一次EnergyEmpty音/VFX各1与成功后两次重入各2不降。
- 空间：不再把Player移动到默认viewport cursor左180px的地图外，保留实际受支撑spawn。只用viewport-local PushInput送一条真实指针事件，要求序号恰好前进1、实采世界坐标匹配；不直接调用Reader或写帧、不送OS鼠标。专用锚点在spawn上180px，挂真实mounted sector，以保持完整residency/射程/LOS规则，而不是关掉世界保护。
- 非预期恢复：明确死亡合同前必须零请求且setup站位不漂；恢复观察在finally解绑。没有用复活后状态掩盖前段失败。
- 爪发布：实际Space在同一非零cast同步发布Boost→Released；改为原20帧内观察恰好两条同cast、BoostConsumed、ReleaseKindBoost、FailureNone事件，不再轮询不存在的常驻Boost。原16cue、两次ClawTension、拒绝二次boost、VFX和暂停/死亡/卸载/完成清理均保留。W仍参与兼容输入，但不再误称W启动自动收绳或松开primary完成取消。

该夹具本来标注`legacy_component_fixture=true native_player_movement=false`，改后仍如此；当前纯键盘正式双路线、真实角色动力和正常速度审阅另验。

## 实际RED→GREEN证据

全部原始证据保留在`G:/knight/验证证据/october13-stable-claw/`。下表案例均完整前缀`thruster-recovery-compatibility-20261104-`，无timeout；失败没有重命名为通过。

| 案例后缀 | 引擎ms/exit | 证明 |
| --- | --- | --- |
| 155003-627-8fb87f5189604f59bace3c33d930eb09 | 4922/1 | 首次输入前后Intro/pendingTrue，Input和Combat禁用、sequence1不动，四事件均0，原EnergyEmpty断言失败 |
| 155212-942-c19ce416cb7b4e079eaa5f2a49ec90d8 | 6680/1 | 第一方法修正后完整返回、首次事件1/1/0/1；第二方法normal Hitbox拒绝 |
| 155431-097-cb7bbe707e3b4a9499a4727521217beb | 7186/1 | 第二方法格挡现场实际Intro/pendingTrue、sequence0/Input及Shield禁用，证实同一启动前提 |
| 155622-171-0dceb8f19342482ba1bd25e5add0b9a1 | 15192/1 | 第二方法原盾/过热/冷却已过，后续Latched失败 |
| 155844-126-f584eade0eec43c98e2e6cffb68dd9db | 15012/1 | 发爪实际距离894.4272，发布Aim→OutOfRange→Idle；玩家已回160,600而旧目标在0,-280 |
| 160235-362-0515a3e3e13547ac84656b055e12d49e | 6865/1 | 新指针实采前提失败，未跳过该门 |
| 160509-415-67a9bf9a9fa844e390415e5b07786da6 | 7155/1 | 两路送同一指针导致sequence0→2、160420实采约32008120；不是等待不足 |
| 160659-827-65c7f0dad38b4753b3e151765dad67c8 | 15117/1 | 单一路由实采正确、站位不漂/零恢复；level-root锚点Travel前被residency拒绝TargetLost |
| 161045-366-9f78d1023e7f42238bc5f91c49f37aae | 12813/1 | sector-owned锚点实际Travel/Latched；同步Boost/Released各1但原跨帧Boost轮询失败 |
| 161248-940-b3427a40f5c94f9bb1cd2781fe9b5c0e | 13084/0 | 原两个方法全部通过、stderr空；首次/重入/16cue/VFX/清理保留，唯一恢复为明确DEATH RESET |

旧地图外setup触发复位的归因有原位置及RecoverOutsideWorld的96px保护调用链支持，但当时没有捕获原恢复事件，不冒称拥有该同步请求证据；新零恢复门专门防止再被掩盖。源码中的正式空间保护和全局指针适配并未修改。

## 最终同版宽门：十门7PASS/3FAIL

已经重新复现当前同版资源失败：`resident-geometry-20261104-161554-680-84f5b1c1353f4b9b8f4f05c824a84536`，9547ms/exit-1073741795、非timeout。gchandle.is_released/mono_object_disposed_baseref→GodotObject.Finalize→GC.RunFinalizers；stdout到fixture/environment/player-access/archive和首BODY_CANDIDATE。不能凭最后一条日志指定具体资源，不因本轮提示通过关闭此FATAL，也未启动额外原生采集。

当前另有原顶部`RecoveryFeedbackSurvivesOwnBoundary`夹具启动失败：`thruster-recovery-feedback-20261104-161745-528-5a3aebda7e88453cad681638849a09a4`，1881ms/exit1，原expectedPlaying/actualIntro断言。它不是本轮已修的两个方法，原4帧恢复观察预算不改，不能据兼容门通过关闭此失败。下一同就绪前提需先分清启动等待与原恢复观察时限，不直接加帧或改生产状态。

`resident-save-compatibility-20261104-161646-971-2b4f7a0ead4f4c51886c0b2015dcb11d` PASS/15342ms。`prologue-pre-20261104-161829-038-12a618e143c947c783c2c4f56cade3f1`严格PASS/161440ms，原44项均返回、原180秒门未扩、stderr空。

一次独立范围审查C0/I0/M0，审查者实际读取161248日志并核对当前测试CS SHA `ACEFDB2374957770DFBBA643FD88167678A4C0A588AFEF0C0EFB5EAD53845100`与该案source前后相同；未二审或新增顺手修补。此审查只覆盖指定新方法块/现调用契约，非整个dirty branch、生产全模块、完整存档harness或所有生命周期排列的保证；只证明组件cue，不代签纯键盘路线、音质/像素/正常速度、资源稳定或完整十一项。未独立执行宽门的部分由根读取终态及保护清单，不从审查结论推断通过；具体全部裁定见过程账，无本轮延期Minor。

原owner全部终态，失败未重跑筛绿；下列为完整案例目录名和实际原生读数，均非timeout：

| 阶段/完整案例目录 | 原生ms | 严格结果 |
| --- | ---: | --- |
| thruster-recovery-compatibility-20261104-161248-940-b3427a40f5c94f9bb1cd2781fe9b5c0e | 13084 | PASS，两个原方法/16cue |
| resident-geometry-20261104-161554-680-84f5b1c1353f4b9b8f4f05c824a84536 | 9547 | FAIL，原生finalizer FATAL |
| resident-save-compatibility-20261104-161646-971-2b4f7a0ead4f4c51886c0b2015dcb11d | 15342 | PASS |
| thruster-recovery-feedback-20261104-161745-528-5a3aebda7e88453cad681638849a09a4 | 1881 | FAIL，另一个旧反馈夹具expectedPlaying/actualIntro |
| prologue-pre-20261104-161829-038-12a618e143c947c783c2c4f56cade3f1 | 161440 | PASS，原44项/180秒门 |
| prologue-post-20261104-162317-549-15ea6a4a9b56414f8fdff448f132a519 | 223229 | PASS，原300秒门 |
| models-20261104-162806-452-1ef2b264c4e94c5a91413db5f5934de1 | 889 | PASS，仅爪模型/到达几何子集 |
| keyboard-20261104-162918-904-50b2012764a44b608847f1a4311aa9ae | 21923 | PASS，独立headless标题入口另8970ms/0 |
| thruster-route-main-diagnostic-20261104-163056-189-e0daa91e9b22467c974681884713ae23 | 192825 | FAIL，原899配重战斗未清 |
| thruster-route-exploration-diagnostic-20261104-163503-674-8e792989940a41068f9470d7d67e0ad4 | 380410 | PASS，Boss/实体支撑出口 |

post四WARNING均为原明确负控：throwing ClawFrameChanged observer、非法VisualScene root、两次Completed→Recovering拒绝。七项严格PASS没有额外ERROR；geometry/feedback/Main的原ERROR各自保留，不能因部分标记或父loop最后退出码忽略。

### 当前两条普通路线

Main原899门仍Rust余HP1、玩家HP2，7次J均started/connected、3次爪；signed gap18.279，能量约.143/.157。当前累计192825ms并非预算扩张或通关；失败没有完成result JSON。不改怪物HP、原储备/计时/真实键盘规则来取得通过，也不据此判定真人不可能通关。

Explore实际JSON为automated、syntheticFixtureFalse、从标题进入同一Player，23访问/11挑战、11遭遇激活/10击败，未清配重遭遇；Boss/SafeExit/Completed为真，恢复请求/完成0/0。玩家第99击真实击败Boss；floor6080,1116,1600,40支撑出口，胶囊完整40×72。死亡/场地/完成各事件一次，向右保持60输入帧后位置7341.191,1079.9327和存档SHA保持`D68DEC7993D42D14D26D4F98E88C00B907350568CA6F6B52E552E87F08A56779`。`operationCoverageComplete`/`humanValidated`/`fullGoalAccepted`仍全false，不是全敌群/全操作或正常显示认可。

### 同版与保护核验

十案source1888清单SHA均`548FCAF20B20F8BFC982806C1EEC45401F12D1D65E6CDC4C6AD48FFD4A244D23`，六fresh binary清单SHA均`154A14DA7B4D9111B20AD2DF6C62196A4C058CA88E986522607D6E86B1DBA9DB`；这些是清单文件Hash，不冒充单DLL/发布包。source/helper/user/continuation前后及fresh/after binary共50文本对一致，零保护差异；全部终态后再逐项读取1888个当前输入仍与最后清单一致，无剩余自有Godot/dotnet构建进程。真实玩家文件未动，无新GUI/OS输入/媒体或原生dump。

媒体一次性Check仍86文件/7427750字节，NotifyFalse；没有恢复任何自动化或独立提醒，也不因阈值检查停工。

## 后续与完整范围

本轮是一个真实验证修正，不是生产动作或数值升级。原顶部反馈用例的启动前提、geometry资源finalizer与普通Main仍不闭合；Task1实现/专项已证，但整体门仍红，不签complete、冻结版本或发布。

资源侧新增[限定路径只读所有权表](2026-11-04-resident-geometry-ownership-readonly-v001.md)：匹配当前七文件SHA，未发现明确重复Dispose/悬空借用，当前匿名失败所有者仍未知；早先builder/已Free探针/仍持有探针皆不能仅凭时间位置排除。不得凭猜测加GC/保活/Dispose/延时；只先复用已有身份记录，不扩大采集授权。

原十一项及R-I/R-C/R-P、正式美术分层与完整连续动作、角色动作设计/惯性与操控、正常速度视听/UI/相机、目标硬件和用户试玩仍保持ACTIVE/OPEN。用户动作返工不因后台测试而缩减或代签，下一生产样段需对应真实位移、重心/支撑、发力收势和附件接续，而非只堆日志或加滑行。

[设计](../superpowers/specs/2026-11-04-task10-cue-readiness-design.md) · [计划](../superpowers/plans/2026-11-04-task10-cue-readiness.md) · [过程账](../../.superpowers/sdd/2026-11-04-task10-cue-readiness/progress.md) · [完整原十一项](2026-10-18-project-remaining-backlog-v001.md)
