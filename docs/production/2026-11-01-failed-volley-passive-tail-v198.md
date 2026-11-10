# v198：补齐失败后的真实三弹证据，Main仍未通过

## 事实

仅KNIGHT `G:/knight/Knight`，原全部十一项目标仍ACTIVE。本轮改动仅Main测试及KeyboardAction测试两个CS；正式玩法、数值、物理、敌人、地图、美术、canonical与原Hop合同均未改。没有用尚未回复的“按枪手预警择时起跳”问题当作批准。

之前伤害发生后，原Hop的HP保持门会立即拒绝后续样本，随后Main失败清场。因此“只见两发”不是“第三发不存在”或“第三发已安全消失”的证据。本轮在原失败之前绑定独立只读journal：ChildEnteredTree先于Launch登记、ShotEmitted才映射真实ordinal、Retired只保存有效节点端点与缓存身份。无新的Shape getter、空间查询或生产Sweep hook；不能声称截获了生产Sweep内部结果。

原Main已经失败后，先停止原Hop观察/监听、冻结其计数，执行原有清键，再用有界的裸PhysicsFrame/ProcessFrame等待补收该轮真实后续事件。没有策略调用、补跳、攻击、拉爪、加资源、写姿态、强制退休或阻挡正常死亡/恢复。裸throw保留原异常；诊断回调故障只记数据，不替代主错。journal独立解除订阅并清空活节点/闭包引用，保留纯值记录。

## 当前真实Main证据

最终案例：`G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261101-115514-082-31c35302dae0421cb2aa561edaf2bdeb/`。

Build27773ms/exit0、零警告零错误；native142060ms/exit1，原build180s/Main600s上限，无超时、guards为空。**严格FAIL**，原异常仍为 `MIXED_RECOVERY_HOP_RED: airborne engagement identity cannot disappear`。stderr有1条该原始ERROR，external镜像一次不重复累计；stdout无ERROR，三路无WARNING/FATAL。不是完整游戏或资源历史全绿。

真实resident：`canyon.v2.resident.counterweight-lift.04` / `precision-burst.tscn`，本案source4894031351782，cycle5。实际枪口(4533,3459)、锁向(.9276568,−.37343386)、圆弹R5，三次真实发射间隔均11 physics ticks。

| 实际ordinal | 发射physics | 退休physics | 真实端点 / 经过距离 |
|---|---:|---:|---|
| 13 | 6733 | 6749 | (4635.041,3417.922) /109.9989 |
| 14 | 6744 | 6754 | (4595.0884,3434.0056) /66.93045 |
| 15 | 6755 | 6765 | (4595.0884,3434.0056) /66.93045 |

13号弹的真实已接受Health伤害在6749、inputSequence6721，HP2→1，来源和ordinal已归属。14/15没有新增已接受Health伤害记录，但**退休原因、被拒绝请求均未知**，不能由此说躲过、无碰撞或安全。没有把I-frame猜测当成事实。

journal的20条纯值记录覆盖3birth/3emitted/3retired/3endpoint；最终 `cycle_retained=True`、`closed_at_final_retirement=True`，fault/drop/outside-cycle-events均0。计数完整只表示观察窗口完整，日志仍明确Hop/Main accepted=false、counted_as_safe=false。

原Hop计数保持在失败处：stage3、shots2/planned2、passed0、faults2、raw Space1，没有借tail把它补成通过。失败后恢复仍0/0、新增key-down0。

### 时钟单位限制（延期Minor）

现有字段 `extra_ticks=13` 实际是13次await-pair调用，不是13个物理帧；本案physics6750→6765实际差15，Godot可能在一个ProcessFrame间执行多次physics。固定120限制由工具控制证明的是最多120次advance调用，**不是普遍的120实际physics-frame证书**。原600s进程硬限没有增加。原始physics边界保留，本文不复述成“只推进13物理帧”；字段改名/原生catch-up负控延期，不用于玩法、时长或性能认证。

## 一审与一次Important修补

唯一fresh审查C0/I1/新增M0。I1是真实诊断边界缺口：旧journal只在birth/emission看周期，第三发后、末弹退休前的取消/换周期可能被漏掉。不是说本案已发生该取消。

一次修补用真实保存actor及实际弹叶birth/Retired信号做控制；仅source-authority快照和emission回调受控注入，actor自然物理停用以固定控制，不是自然敌人齐射或真实Main证书。stable正例、retirement前换周期、post-third采样换周期再恢复、owner revoked四组；旧代码实际误放行后三组及恢复状态而RED。修正后Sample与Retire边界均锁存源authority/cycle失效，不能恢复资格；用真实 `HasPresentationAuthority` 识别同步取消。到最后真实端点闭合观察窗口，之后的未来生命周期不冒称已认证。

四组控制GREEN，原五个有界等待/原异常身份控制和全部既有actions均GREEN。没有二审或第二轮C/I修改。v196 GuardedDamage setup M1仍延期；本轮另保留上述计数命名Minor。

## 全部六案

证据根 `G:/knight/验证证据/october13-stable-claw/`，日期均20261101；表内为phase后时间/唯一号。全部build exit0、零警告错误，无超时、guards为空；失败原样保留，ExpectedFailure为空，无筛绿或加预算。

| phase / 案例 | build ms | native ms / exit | 范围结论 |
|---|---:|---:|---|
| actions-113412-868-5b461133418241558a9b837077bbd7c7 | 27672 |692/1 | 首个工具控制真实RED，非编译失败 |
| actions-113802-464-dc455ca3cc494d809e1c991a745af37e |27916|9387/0|五工具控制及既有actions PASS |
| Main-113947-445-b2089b3c2f964d8a88702f0364e0b5b8 |26123|126095/1|原HP失败保留；补丁前I1资格不完整，三端点事实保留 |
| actions-115040-364-7759279ba5e04ee98747c9e639a7427d |25559|788/1|I1实际journal边界控制RED |
| actions-115252-820-312ed04d18184b4a938fc39d03826a4d |24846|9596/0|最终五工具+四journal控制+全部actions PASS |
| Main-115514-082-31c35302dae0421cb2aa561edaf2bdeb |27773|142060/1|最终Main仍FAIL；当前三弹端点、周期窗口证据完整 |

共2限定PASS、2真实RED、2完整Main FAIL，不是全绿。Main完整目录前缀为 `thruster-route-main-diagnostic-20261101-`；actions前缀为 `actions-20261101-`。

## 推理、裁定与风险

- 第一弹的6748 post-node样本与6749退休端点/距离相同，玩家则从Y3377.8835下落到3385.5225。这与下落阶段进入已有弹体位置的重叠一致；不是生产Sweep内部日志，也不能拿旁路位置证明所有执行顺序或普遍碰撞根因。
- 不再以“第三发未出现/尾弹自然取消/未受伤所以安全”为前提，也不把只到旧失败点的外推当完整三弹可行域。
- 继续用实际Main，而非另搭HP5、枪口/地面不同的近似夹具；代价是tail已受伤且释放输入，不能当原策略无伤反事实。
- 按连续已授权的无窗口方式执行，用冻结副本保管dirty tree，不建worktree、提交或删辅助资料；代价是不产生Git发布资格。
- Main/Hop保持FAIL、原全部十一项ACTIVE；代价是正常路线与正式交付仍未完成。
- 14/15退休原因、拒绝请求、安全通过均不判断；代价是还需相应原生证据，不能猜测。
- I1只在末端点之前的观察窗口修正并RED→GREEN；代价是不宣称无限未来周期、任意故障或全部资源生命周期安全。
- 120是有界await调用策略，真实physics差另看原日志；代价是严格模拟时间/physics上限及性能不获认证，命名Minor延期。
- 起跳时机/旧Main失败本轮不修；代价是原始失败持续，待已发时机问题实际回复或在原合同内取得更强证据，不能静默放宽。
- 正常显示、正式美术动作/UI音频、硬件性能、真人、真实存档兼容、发布与全部历史资源问题均不由本次证明；原十一项所有缺口/旧延期保留。

## 保管与下一步

六案累计30对manifest、2908个选中source副本、60helper副本、36bin逐SHA一致；12个owned进程身份按creation/path均终结。12:07:21+08当前1872source/9helpers/6bin匹配最终actions/Main；旧五案不重哈，不强求旧版本等于当前。

最终Main SHA `3C7DE9FD16FC204FC4C47304E08E0CAC6EAF1056742D74486FA5CED9B76454EA`，actions `EE5B623C39A507E989104685A51708D14E98A3FA49439F28301C00E2968CE746`；MotorCB65、GeometryA359、canonical7A79不变。sourceManifest `6A44278C3C97D4626FEF67B0A55608950DB7AE66B35B8DF62754BBA785093EC7`；helpers `2FEB90DD967F9BADBFE51584BC475689479C7233828C53636A6917AD0FA9834D`；binaryManifest `4CD38AB2E96EFB2E5EE5184A6940C66D1DB6D46AE009FE0B7281463CBA4D328C`；dll `6A4956214B8CDDDD8F478F18713C4C2465D2D8E5143E6F1F05FA9651982D992D`。HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6、原codex分支及index21610均未改。

下一可独立推进的是G02/U03固定阅读轨道在真实受支撑移动/落地后的避遮挡检验：当前布局只避HUD但未消费实际角色/攻击保护区，这是明确覆盖边界，**不是已复现玩家Bug**。沿用保存关卡Title/StartNew→Playing与实际投影，先取得真假证据；若红再做保持固定anchor的局部修正，不恢复被否决的随玩家跳动摆位器，也不未经验证隐藏必要信息。T01/T03等机关具体设计仍待细化，不为了制造增量擅自补设定。

本轮无新媒体、GUI/系统键鼠、付费生成、额外崩溃采集、自动化、Git写入/发布、删除或真实存档写入。20GiB仅随手动容量检查正常汇报，不暂停工作。计划、ledger与全部失败/控制证据保留，原完整目标不标完成。
