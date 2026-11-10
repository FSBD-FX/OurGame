# v191：防守释放后取得真实目标预览与一次K，主线仍未通过

## 事实

本轮仅改两个现有测试文件：`PrologueCanyonGeneratedMainRouteTests.cs`、`KeyboardActionRuntimeTests.cs`。生产地图、敌人、数值、爪优先级、LOS、美术和v190穿位控制器不变。原十一项完整序章目标仍ACTIVE/OPEN。

问题是测试驾驶交回：角色已经站在两敌之间，实际朝左防守。全源危险允许停止防守时，旧D计划却恢复，破坏了站位与方向。新函数先清驾驶器自己的方向计划，再真正释放它的盾状态；没有直接写位置/速度、ReleaseAll、新J/K或另一次resolver。无所有权、非实际左向/中立或竞争输入时不执行。

Main仅在实际穿位完成、同一存活目标自己的Shot已发生、原shortWindow与完整全源保守余量足够、不需防守、同名实体棚面完整支撑时开始一次准备。释放后只消费原outer单tick，等实际新输入及生产Idle预览；原physics/sequence/authority/pose/velocity/aim/anchor/own target/LOS门全部保留。只有原K块能登记机会、发K并填写回执。

| 案 | native结果 | 限定 |
|---|---|---|
| fd9db0eb | 未启动；build24729ms/exit1 | CS0136局部变量重名；非行为RED，缺fresh binary pair如实保留 |
| 85cd99f2 | 1823ms/exit1 | 实际Left盾/中立/残余速度前置成立后，stub在释放入口预期RED |
| 7e60f50e | 8998ms/exit0 | 原输入专项＋16次实际中立/自然刹停/支撑/无额外动作通过 |
| b94edb36 | 135031ms/exit1 | 初版真实预览与K链通过；后续战斗死亡，整案FAIL |
| 98489359 | 793ms/exit1 | 审查I1期限边界的真实RED，非编译错误 |
| 0cf53ced | 9133ms/exit0 | I1的5项边界控制及完整输入专项通过 |
| 8e5e506b | 144609ms/exit1 | 最终同源再次真实预览与K链通过；后续死亡，整案仍FAIL |
| 8655cea1 | 519ms/exit0 | 既有机械爪模型子集，不是全项目模型资格 |
| d305dc31 完整pre | 180044ms/exit-1、超时FAIL | 原180000ms；没有phase完成或退出清理标记，不借安静日志称通过 |
| 651be86c 完整post | 207692ms/exit0、严格PASS | 原300000ms；不是旧180秒通过或硬件性能认证 |

前两次Main均无timeout或guard差异；均只有原死亡错误，不是通关资格。首个编译失败已只重命名新夹具变量并保留原案，不冒称TDD RED。

### 最终同版Main的真实链

案：`thruster-route-main-diagnostic-20261101-070928-279-8e5e506b079547b0ba496b4619f3763d`。

- 实际目标机枪手root `16036904569489`，ThreadTarget `16036988455537`。
- physics6444 / loop291：释放准备开始，原sample6415，玩家X4681.068/HP2。
- 6445 / loop292：新生产sample6416，expected/producer/current target一致，own/LOS与所有同源校验通过；盾已关闭，Left/neutral/速度0。准备仅一个原tick。
- 原K发cast12；6446 Aim、6447 Windup、6448 Travel，6450同target Latched→Released，6451原application/Stunned判定通过，6455实际Idle。
- 6456 / loop303冻结原首K：edge12→13，press6418，handoff6427，cast12。没有额外准备K，也不把后续账重置为零。

应用归属由原同演员适配器检查、锁定target、后续Stunned与Idle链交叉支持；`application_stunned_OK`单行没有独立application source/target字段，不能把该行的resource/danger source（另一敌人的预警来源）当成爪的应用来源。

之后角色仍被旧战斗策略带离站位，能量耗尽并死亡恢复：`(4810.2354,3426.8164)→(1680,3960)`，原断言step3175.32、恢复1/0。这里是死亡恢复，不冒称新发现的无故传送。首J、fatalJ、Defeated、短喷/借力跳/后续K及完整Boss出口并未因这一局部K而完成。

## 推理与下一步

原K的合法目标预览、真实一次输入、锁定、应用/眩晕和收回现已取得正向证据；证据不支持为了这次问题修改生产爪选敌优先级。

下一阻碍在旧K→J驾驶合同。对初版Main的冻结源码与原始事件只读对齐：

- continued-owned-Burst选择及finish intent仍只接纳HP1；实际K后该机枪手仍HP2，所以没有被接回。
- K后471条原outer决策为415次Rust、56次居民Moth、0次owned Burst；不能写成“每帧都只选Rust”。唯一后续J确实在6527发出、6528接受，但对象是Rust，不是机枪手。不是J键没发或遗留K intent。
- 更后的fresh-J边界也拒绝owned Burst的非HP1状态。它在本案没被实际走到，属于后续retarget后仍会遇到的静态门，不能冒充本案已经触发的失败点。
- Rust的desiredActorSide只在选中演员改变时更新；穿过同一Rust后旧side仍缓存，符合随后向右走回的实际行为。

下一独立任务需要明确区分HP2→HP1的真实非致命反击与原有HP1致命J收尾，并核对接回目标/中立/短喷阶段的次序；不得仅放宽成“任意HP都算fatalJ”，也不能删除own Shot、真实首J/致命J/Defeated或原K账。本轮只完成上述读证，没有实现这些后续改动。生产规则、敌人难度和完整验收要求保持不变。

## 审查、风险与明确边界

唯一只读终审：Critical0 / Important1 / Minor1，无二审。

- I1已做唯一TDD修补：旧`elapsed<=8`在第8步未ready时仍会再等待一步。真实RED793ms→GREEN9133ms；现允许第8步ready交接，但第8步未ready在再次释放/等待前失败，第9步无论ready与否都拒绝。当前Main实际只准备1步，不把未触发缺陷写成此前已经正确。
- Deferred minor M1：false-owner与wrong-facing负例都在实际D移动状态，其他检查也能使其拒绝；尚不能独立捕获删掉所有权/朝向门的突变。当前源码有保护，新增真实输入效果已验证；独立负例覆盖延期，不在本轮补修。

Rulings与代价：

1. 连续目标及仅headless授权覆盖此测试侧衔接；不反复要求偏好确认。代价是新驾驶策略仍需本轮和后续真实证据，不等于用户认可画面。
2. 唯一dirty工程不执行worktree/commit/delete脚本，改用冻结清单与案内副本。代价是本轮没有新远端版本，不能代替未来发布。
3. 准备过程中若新危险、身份、HP、支撑或期限失效，本隔离诊断严格失败并清理，不擅加未经验证的防守重试。代价是可能拒绝可恢复玩法，但绝不伪造成功；开始阶段不会取消仍需要的防守，原K后的abort/guard门不变。
4. I1修补后重跑Main是为了当前已改变消费者的同版资格，不是重跑未改输入筛绿；首个Main FAIL保留，最终Main仍FAIL也不抹除。

审查set-aside逐项保留：v190控制器内部及两项旧Minor未改、仍按原记录延期；后续混战/耗能/死亡未修；pre/post及保管由root执行，不借审查作运行证明；暂停/queued/祖先失效/同步callback主动组合未全部新增原生覆盖；其他玩家/场景、动态地形、竞争输入、通用复用不认证；完整原十一项、美术/UI/正常渲染/硬件性能/用户试玩均OPEN。未来预留仍只按既定接口，不将测试通过冒充正式制作完成。

## 保管与权限

原始证据仍在 `G:/knight/验证证据/october13-stable-claw/`，各案完整before/after、源/helper/binary清单、实际argv/创建身份、stdout/stderr及失败保留。首个无engine/fresh binary的案不补造材料。原四案审计时点与最终修补后current指纹分开记录。

十次尝试、九次实际native：4PASS/6FAIL（失败包括1编译失败、2预期行为RED、2Main死亡、1pre超时）。最终同源五案是actions/models/post三项PASS，Main/pre两项FAIL。没有为了筛绿重跑同一输入版本，原失败全保留。I1修补已获专项RED→GREEN，但整个序章的回归资格仍未全绿。

pre的stdout488行，最后为BellChainMechanism的arrival_to_chain_floor：engine_ms179237返回sample.2，179238进入sample.5后被原上限终止。它没有标准ERROR/WARNING/FATAL，也没有完成标记；原因尚未查明，不能从最后停留位置断言链桥本身或本轮Main局部代码是耗时原因，历史资源/性能问题仍OPEN。

post仅原四项限定WARNING（观察者故意抛错1、room-profile1、已完成流程拒绝恢复2），无ERROR/FATAL。本轮pre/post没有被换成较小子集；模型一行则明确只指既有爪模型子集。Explore未在本轮重跑，不能借旧版探索诊断关闭当前双路线门。

最终运行源清单SHA：`71804DE86B2E9CBFEA60770AA1B4AD2FEB625563F385792C8F67EEF4C65E5F8D`；Knight.dll：`4EF9C74ABAC43D8557E992B84606C388AF60C129C11574A9FEE9265C55A7EBB7`。Main：`6341E0AF175CC1A6BBAF53DB097CFAC649BDF9D966F7763546A5C7103D28D402`；Actions：`6968F31E0E2E026D3BAD8414C1C41438E76571C72F7AE916CA06B8FBB00B3BA2`。v190 lane仍D158BB15…，canonical仍51C7A184…，无变化。本轮显式SourceRows1872，canonical默认1604未改。

完整案目录：

- `actions-20261101-065501-931-fd9db0ebbaa343b69bde1017e1815cc6`
- `actions-20261101-065557-339-85cd99f27e4c46a289fa22ff4c15c9a3`
- `actions-20261101-065752-689-7e60f50e005d4420af4a71778b5d8a33`
- `thruster-route-main-diagnostic-20261101-065902-918-b94edb36e8f646d1a57df76dde7c99bc`
- `actions-20261101-070515-926-9848935934f64a9c8f6596aa6b77522a`
- `actions-20261101-070808-865-0cf53ced847543b5a99a4cbc6c8d7f5e`
- `thruster-route-main-diagnostic-20261101-070928-279-8e5e506b079547b0ba496b4619f3763d`
- `models-20261101-071354-504-8655cea178734eafb55d28a80aee368f`
- `prologue-pre-20261101-071518-253-d305dc310bfb4aef83a8ce757db48f21`
- `prologue-post-20261101-071915-897-651be86c68864a07b23ec12b27d9e35d`

07:24:09 +08 root核对：HEAD仍 `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，分支未变，index SHA仍 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。手动媒体一次检查：7427750bytes/86files，baseline3508、Notify=false，无新媒体、无自动化。

最终保管审计只核本轮新案，不重复重哈v190：49对有效manifest、4804份选定source副本、104份helper副本、54个归档binary逐SHA匹配；首编译失败缺失binary pair不计成功。19个旧owned身份均已结束，对应18个不同PID；56240曾先后属于两次不同创建时间的测试进程，按身份而不是PID数字核账，未操作其他进程。07:24:15的最终current核验为1872 source/6 binary/post10 helper一致；不把07:03:35的pre-fix核验混为最终状态。

post实核75组同名有序enter→return，0–74共150行，owner=`RunPrologueTests`、repetitions8、节点1→1，退出清理与完成标记齐全。其十项helper清单SHA为 `48D515B7A1AB55E140806BE7E146CC24EF185CE77B91BB440974849706C9B28B`；binary清单SHA为 `DF34D8CE0916756E896F12DF0218FFD1ED4277DFA83FD86F425842213F852BAA`。

各案构建均在原180000ms内，按上表顺序耗时24729、25156、26068、24710、25120、26753、28183、28243、25209、25909ms。除首案1个编译错误外，其余均0 warning/0 error。九次native均实际headless、argv与contract一致、无fixedFPS/movie；只有pre触及原上限。后段通过不抵销两次Main死亡及pre超时，原资源根因、完整双路线与正式交付仍未关闭。

仅隔离headless构建和Godot内部键盘事件，无图形窗口、OS键鼠、真实存档写入、误发视频分析、美术/付费生成、额外崩溃采集、自动化、Git写入或删除。完整目标不改为小范围测试目标，保持ACTIVE。
