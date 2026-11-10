# v186 — K打断与无窗口探索诊断走通；前段资源错误仍OPEN

最终状态：同版自动Explore诊断从标题、同一玩家、零恢复到Boss击败/有支撑出口严格PASS；动作/机械爪模型子集/后段通过。但普通pre有1条Handle ERROR，严格FAIL，整套不全绿。没有新增正式美术或正常渲染/真人/性能验收，不是完整十一项交付。以下保留本版本从准备、RED到最终结果的过程；以文末最终核验为准。

上一目标轮次v185属进展：完整Explore原失败真实复现，100条被动原外层采样改变了下一候选，从偏晚Recovery转为front下一Telegraph起始。完整十一项目标继续ACTIVE，唯一工程G:/knight/Knight；本轮仍仅无窗口构建/测试权限。

## 事实、假设、风险

事实：v185同源原路线在后laser2发射时HP1→0，盾朝前方Squire，E足够；Left实际候选可选rear，但生产preview在候选窗口仍朝前。原失败为行为RED，不把新类缺失导致的编译错误冒充RED。

假设：原900外层内取得真实Left→单K→同cast实际应用/撤束→及时回到+X，能够解除这次前后夹击。要验证该假设，不能只改danger winner或静态计算一个距离。

风险：L取消活爪，实际A改变惯性/站位，preview可能旧于Motor后的pose；K有旅行/缩回时间，前敌必须有足够当前余量。不能强等缩爪才开始回防，也不能在发生真实Latched/Released之前假装攻击已撤。完整Explore可能推进到更后的失败；局部成功不代表整路或序章完成。

## 所有权与实现边界

worker仅写新增`tests/presentation/CourtyardRearClawControl.cs`；root写原Main接线、KeyboardActionRuntimeTests真实角色负控和canonical选定副本，独占全部native。其他旧改动不回滚。任何构建/引擎运行时全源/helper冻结。

该类是测试驾驶，不进入生产操作或调整任何生产地图/伤害/AI/爪角优先规则。Main仅batch0调用，拥有本帧时只等待原1physics再continue，原900不增。原HP、同player、零恢复、无新jump/dash约束保留，开始的控制必须真正Completed才能通过encounter终结。无机会时Step不得清旧D或发K；未取得输入权的Dispose也不得清原owner。

新增一份CS使实际source manifest1871，canonical显式选定helper/Main/Driver以保存准确实体；原build180s、actions55s、Explore600s及所有断言/错误门不变。没有降低SourceRows默认值或原生放行标准。运行前不预判构建、K或路线通过；实际结果见后续。

## 首次实际验证

`actions-20261101-034340-565-6106e50024ad40449b022ce95dc62fe7`：build26945/native8416ms严格PASS，无超时。真实保存Player＋floor中的D输入在Step(null/default-danger)前后和未取得所有权的Dispose后均保留，无新增K；原动作、双向原地盾、暂停、护盾跳与过热测试仍全部执行。该负控通过不代表正向K链或完整路线通过。

运行中记录：`thruster-route-exploration-diagnostic-20261101-034505-287-7e61f1d8218d46c291c7e761c67e5eb6` build30604ms通过，实际headless，root当时持有exec66237，不因观察超时另启进程。源码/helper自actions起未改；随后终结结果如下，不借用历史绿案。

### 随后终结与唯一审查

7e61f1d8随后native270798ms/exit1/未超时/guard差异0，严格FAIL。这次不是原死亡，而是新控制错误：physics13981准备Left，13982真实采到A，13985以新鲜neutral生产preview发K/cast57；下一publication仍Idle、Next57、raw/accepted K均0，却被误判最终Idle。没有实际Aim、application、Latched或打断成功。失败原件保留。

唯一新鲜审查C0/I4/M2。I1为上述pending Idle误判；I2为K后未持续验证fullSupport；I3为历史撤权sticky bool代替Stunned当前结果；I4为Live漏查queued ancestor。本轮只进行一轮C/I修复，不二审。

先新增真实原生控制，保留未修判断语义，`actions-20261101-035314-232-864ab55f82d345dba5e5cbf406e82131` native800ms四项均真实RED，非编译错误：pendingIdle、支撑丢失许可、当前撤权许可、真实queued-parent关闭child权。后三类中的support/revocation为消费者输入契约测试；queued-parent是实际入树Node父QueueFree、child尚未self-queued的同步负控，不冒称强制游戏角色坠落或真实敌人重新授权。

修复后`actions-20261101-035616-257-16c9b1aec63c467b9e89e8abefb534b1` build25626/native8394ms严格PASS：四项契约和原动作/不抢D负控全部执行。pendingIdle只允许未ACK、零raw/accepted、同NextCast/authority、物理K仍按住的当前Idle，绝不算完成；继续等同一K的真实Aim，最多8个原有tick，不补按/不扩900。postedge每次及返回false的完成边界都要求完整Step支撑＋接地。Stunned及回防用当前撤权查询，不用历史bool替代；Live覆盖全父链。

M1延期：cleanup catch里GD.PushError本身的极端异常仍未独立保护/验证。M2延期：旧PLAN_END未直接统计outer-loop缺口，文档披露而不称连续覆盖，新K_END标outer_loop_only。原资源根因/全范围资格不因此关闭。

随后启动最终同源Explore `...035743-874-fd80707f813f4e189e8abde35c1916d4`，root当时持有55431/PID35008，实际headless/隔离存储，源码/helper保持冻结。负控GREEN不替代真实K链；该进程后续实测如下。

### 该进程取得的局部实际GREEN（当时全路线仍在跑）

原完整探索实际进入院落后，同一正常玩家：13986以neutral生产preview发K/cast57；13986/sample13957出现raw0/accepted0的pending Idle，被正确保留为“未ACK”而非完成。13987/sample13958实际Aim、raw/accepted各1，13988松同一K。14004产生同cast/next58/target28359585173362的Latched→Released(null)，adapter真实+1、当前授权/leaf均撤；随后同一global physics14004内的下一次敌AI执行发布真实Stunned/None，不把AI执行顺序说成global physics+1。

14005在Released、缩爪尚未结束时开始回转；14006有实际D采样，14014有当前右向L/盾ACK；14035实际Idle，14036/loop111/900完成并交回原策略。END为18/64行、omitted0/faults0、一个raw/accepted K、Aim/Latched/Released/Stunned/撤权/Right/Guard/Idle全部true。完整Step支撑、接地、HP1、同player、原jump/dash及零恢复检查持续通过。模块没有新增等待帧或独立resolver。

这是原失败路径上的实际局部GREEN，不是孤立预摆角色/直接ApplyThreadPull。其后原院落继续通过，并进入Boss后段，最后观察到physics17754；此时进程仍live，不能先判完整Explore、Boss或实体出口已完成。旧被动PLAN是222个外层样本，192条保留、30容量截断，故更不能当连续全战斗观测。其余完整回归/保管审计待该进程终结后安排。

## 最终实际结果（覆盖上述运行中状态）

| 本轮案例 | build/native ms | 严格结果 |
|---|---:|---|
| actions034340/6106e500 | 26945/8416 | PASS，原动作＋未取得所有权不抢D |
| Explore034505/7e61f1d8 | 30604/270798 | FAIL，pending Idle误判；实际K ACK=0 |
| actions035314/864ab55f | 28088/800 | FAIL，四个契约反例真实RED |
| actions035616/16c9b1ae | 25626/8394 | PASS，四反例修复＋原动作 |
| Explore035743/fd80707f | 24336/326137 | 严格headless自动诊断PASS，非正式显示认证 |
| models040650/a099de9e | 24846/502 | PASS，仅机械爪模型子集 |
| pre040721/45c99a6b | 24374/172768 | FAIL，native exit0/未超时但1条Handle ERROR |
| post041131/163a43f1 | 26330/207882 | PASS，原300000ms，非旧180秒或性能资格 |

8/8构建成功，8原生均实际headless、未超时，guardFailures均空；严格5PASS/3FAIL。前段失败后原串行40821终结、未自动启动post；随后只单独跑一次同源post28198，没有重跑pre筛绿，也没有错误豁免。全部句柄现已终结，不因观察超时重启。

### Explore诊断终态

`storage/task10-route-result.json`和原始stdout均已核：automated、syntheticFixture=false、startedFromTitle=true、persistentPlayer=true，player47546631684，恢复请求/完成0/0；23路线节点、11挑战种类、11遭遇激活/10清除、4机关、2方向捷径、2检查点。该计数不等于杀死全部76居民，不作这种推断。

Boss通过真实攻击99于physics17962死亡，D在sequence17946发出/17947采到；physics18141以完整body越过出口右面、仍有真实地面支撑而进入Completed。完成信号和完成次数均1，结束面板可见；继续按D60帧后位置和存档SHA不变。源码/记录明确measured_OS_write_count=unavailable，不能宣称操作系统只写盘一次。

artifact明确`humanValidated=false`、`operationCoverageComplete=false`、`fullGoalAccepted=false`；diagnostic helper也保留normal_display/performance/human/whole_goal=false，原正式资格门未改变。技术路线诊断通过不等于正常画面、全操作覆盖、用户认可或全十一项完成。

### pre错误的已证实边界

`prologue-pre-...45c99a6b/engine.stderr.log:1` 是唯一标准ERROR：`Handle is not initialized`，栈为GCHandle.FromIntPtr→ScriptManagerBridge.SwapGCHandleForType→Node.AddChild→BasinSupplyRuntimeTests.OpenSample:463→ReturnToTitleDuringHealingStillPersistsTheClaim:372。它直接发生在第二Supply fixture挂树；前案NewGameDuringHealing完成/audio drain的continuation在栈下部，不证明错误在teardown内部。第二案的onHealed回调在OpenSample返回后才订阅，不能归咎其内部重入。

同external sink110–160行显示前案NEW_RUN_REENTRY/audio drain→ERROR→后案TITLE_REENTRY→SUPPLY_RUNTIME_OK，最终pre确有phaseOK/正常退出，但严格门禁仍FAIL。具体wrapper/type/script/old-new handle身份缺失，不能把某个sector、Player Profile、新K模块或历史Finalizer定为同因。下一安全动作是保留原完整pre前缀和四个Supply案，给原AddChild前后/既有生命周期接口补有界标量；不加GC/等待/常驻强引用、改cache模式或免错。原RunLifetimeBoundary只跑第一案，不能替代本次第二案覆盖。

### post及保管

post原75 enter/75 return按0–74逐项同名有序配对；owner8、nodes1→1。恰好原4限定WARNING（seamless1、room-profile1、flow2），无ERROR/FATAL；不是运行零警告，也不关闭历史资源原因。pre原180s内结束，post按既有300s通过，均不认证目标硬件性能。

独立只读保管：本轮40对manifest前后相同，3906个选定source实体、84helper、48bin逐SHA匹配。当前1871源、6bin、最终post10helper全部与最终案绑定；仅新增test-only control/Main/actions及canonical选定副本差异，无生产源差异。本轮16个owned进程身份均已核结束，无复用误杀。

最终source manifest SHA`19E72B408A1D4F9139CA441F382955A0882CC784A18044E9DD88FF4567A25C5D`；Knight.dll`60973B7A9D3B222A40D68CC828895EC10F041E9050661381AE7ACA176AFABD94`；canonical`5F683E64A01EA7F211D05B1FC5AC94E49CDF9DC076339AD26B26C9B012DD5A9B`；post helper manifest`14F75FD3ABF9AC9F9753656A797DCEA00D894439F43AEF7EC02A84A98EC46FD3`。HEAD及index未动。手动媒体检查2026-10-31T20:18:34.2745994Z为7427750bytes/86files/Notify=false，基线3508未重设，无自动化或新媒体。

## 未关闭事项与下一步

原十一项目标全部范围保留ACTIVE，不标完整交付；Task2实际控制及Explore诊断取得GREEN，但完整回归仍因pre错误未全绿。Main当前版未重跑，旧能量困境仍OPEN，不能拿Explore代替Main。下一先缩小Supply第二fixture挂树错误，再验证Main真实前置K/能量节奏；之后仍有正式图声/连续动作/UI、正常渲染/目标硬件和真人验收。

审查四项C/I均经过对应反例及修复后动作套验证；支撑/重新授权反例是消费者输入契约，尚无强制真实坠落/敌再授权的动态负控。M1极端日志自身异常保护、M2旧PLAN_END缺口计数字段继续延期。没有新窗口、系统输入、付费生成、额外崩溃采集、真实存档修改、Git写入/发布或删除。

无新窗口/OS输入/真实存档/付费/新采集/媒体/自动化/Git写入/发布/删除权限；误发视频不用。原完整Main/Explore、资源根因、正式美术动作UI/声音、硬件/真人与交付门仍全部OPEN。
