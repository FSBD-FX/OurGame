# v075 · 缓存边界取证与真实回归结果

2026-10-23深夜至2026-10-24；完整十一项目标仍ACTIVE。本轮是诊断和防误报校验，不是美术/整关交付，也没有改变正式游戏的资源加载策略。用户最后更正“等一下，是没看到”：v072c真人显示未通过，不追认遮挡复现或视觉认可，不自动再开窗口/发送输入。

## 事实、推理、风险

- 事实：同一游戏源码下，Reuse与IgnoreDeep各一次完整三夹具安静退出，Ignore三夹具也完成且退出0，但出现`Handle is not initialized`，严格FAIL。普通pre落地断言失败，post原180秒超时；均保留原件。
- 推理：浅层Ignore不能消除本次错误；由于Reuse对照本就未复现，不能把Deep的一次安静当修复。运行期敌人等其他Load仍是混杂因素。没有证据把此次Swap绑定到历史serial101/497或证明与某次FATAL同一个native owner。
- 风险：深层不复用会改变共享、内存及加载时序；诊断也可能扰动终结时机。因此没有生产cache变通、额外GC/等待/永久保活、引擎升级或错误豁免。

## 实施范围

`BasinAscentCameraRuntimeTests`保留原三个相机姿态、Playing屏障、真实落地/延后居民/HP和检查点断言；仅新诊断入口传入Reuse/Ignore/IgnoreDeep。普通调用仍为原`GD.Load`；诊断临时fallback在finally恢复，复用现有保全primary错误的自有Level清理。未增每帧查询或调整原物理预算。

每个新模式独立原生进程，记录15条load-enter/loaded/mounted/playing/released标量边界，三个root各自非零且不同，Playing/释放的owner必须相同。原退出终结屏障之后才发诊断完成，禁止冒称`STABLE_CLAW_OK`。

### 一次独立审查及修正

无Critical，两个Important有真实RED→GREEN：

1. PowerShell的ValidateSet接收大小写变体，而原StartsWith区分大小写，导致`LEVEL-CACHE-REUSE -CaptureMovie`绕过早期禁用。实际参数/早期guard AST测试72例先有48例误接收；新增副作用前的canonical spelling拒绝后72例全PASS。未执行构建/原生/窗口来测试这个风险。
2. 原消费器只检查四个完成收据各出现一次，提前到文件首的complete也被接受。以真实Reuse stdout及当前实际消费AST、仅替换三处文件IO，先复现提前完成误接收；改为最后release→behavior done→finalizer→exit drain→complete严格顺序。新增五种移位反例，新的真实Reuse原生案加全部27消费用例PASS。

修正只触及上述测试owner；没有用lowercase重写其他旧phase语义。一次修正后未反复索要审查。Minor：辅助测试不在原生owner的helper自动快照列表中，本轮不扩张owner清单；另保留最终脚本字节、原owner与真实执行输出，明确不把这份手动副本说成原生运行期guard覆盖。该通用自动custody改进仍可后续处理。

最终合同证据目录：`G:/knight/验证证据/october13-stable-claw/v075-contracts-20261024-0018`。入口72例和实际原生绑定消费27例输出、两份测试、owner均保留，复制SHA核对相等。consumer SHA `A8C79A5A8FEC4327175B6ED36DB01422886F5B654DEF00CB087A0A4D35B60336`；admission SHA `098D651E8939882863304882C8105276FB2223296066DB21ACA8456056C53E5B`；owner SHA `03D4990BAD7D12084F7BAF7FC807B8720D2E3CB2E8DCAE57B60A25B1E662F3B0`。

审查不判断事项的裁决：

- 所给旧before不含完整camera文件，不能据此认证其全部历史归属；本轮实际三个保存Level断言/控制点与冻结源码可核，但不追溯不存在的快照。代价是历史归属有限，不补造原件。
- 审查时ordinary批次未完成，以下以root真实终态为准，不用审查评价覆盖FAIL。
- GCHandle所有权、因果/概率/生产缓存适用性、长期可靠性、内存和硬件性能均未证实，继续OPEN，不升级引擎或调整正式策略。
- Main/Explore、全敌群实战/回返、真实存档兼容、正式视听/人类可见性/玩法认可、十一项与打包发行未验收，不能从局部三夹具外推。
- 脏工作区、Git/base全局清洁与全仓安全不在此窄审查范围；仅本轮保管/原index核对，其他工作保留。真实存档未作语义读取，无Git写入。

## 八次原生终态

全部原件在`G:/knight/验证证据/october13-stable-claw`。两个helper冻结阶段不能拼成“同一版本全绿”；游戏源码1776项/892C#未再变。总五严格PASS/三FAIL不是完成比例。前三案及ordinary三案为修正前owner；最后Reuse与models为修正后owner。

| 完整目录名 | 退出 / 毫秒 | 实际结论 |
|---|---:|---|
| level-cache-reuse-20261023-235317-385-864d53bee8794c138990fa450cab2882 | 0 / 22801 | 三相机/Playing/自有释放/原退出屏障，诊断PASS |
| level-cache-ignore-20261023-235528-021-b907e1cfadf94f5dae8e261da9cb1da8 | 0 / 23996 | Handle ERROR，严格FAIL；不是进程FATAL/崩溃 |
| level-cache-ignore-deep-20261023-235629-905-3b70b4e013bf427fb32d7b9003da4553 | 0 / 22996 | 同三个夹具，诊断PASS，不证明root fix |
| prologue-pre-20261024-000306-456-36ff5b32099040e08828212bd1ac0ede | 1 / 66443 | BasinSupply OpenSample实际落地断言失败，整pre FAIL |
| prologue-post-20261024-000445-757-5c2a85c1426b4dc6a0df44109c0c7949 | -1 / 180052 | 原180000ms watchdog，整post FAIL；没有ERROR/FATAL记录 |
| models-20261024-000821-151-45d84405cd534ab6943dd2ad7f2dbb9c | 0 / 649 | 普通模型专项PASS |
| level-cache-reuse-20261024-001426-848-d2e0d35b9495491eb9b2c7f745fc0b7b | 0 / 23068 | 修正后owner，真实三循环/严格顺序及退出，诊断PASS |
| models-20261024-001603-799-a187e0d13d4a498b8b29e5fba0793ddf | 0 / 536 | 最终普通构建/模型专项PASS |

Ignore的错误栈为GCHandle.FromIntPtr→SwapGCHandleForType→Node.AddChild→CheckPosition:98，前一自有释放continuation可见；不是仅Load边界。它仍打印全部15行和退出收据，所以严格错误veto不可放松。此前口头把它简称“崩溃”已更正为“记录ERROR、正常退出”。

## 普通回归下一步：先补真正缺失的边界

pre在三个相机/净空/键盘抓爪之后，到`BasinSupplyRuntimeTests.NewGameDuringHealingCannotSpendTheNewRun`的OpenSample落地失败。只读追踪确认一个确定前提冲突：旧sample明确Layout1，但继承保存Level的DevelopmentV001内容；NeedsResidentPreparation不看layout，实际居民spawn context只接受CurrentLayoutVersion2。OpenSample又只固定3+8帧，无Playing核对；单纯加等待不能解决无效准备。

下一先在原失败处留一次layout/content/state/pending/reason/P/V/CanProcess标量，再判断legacy sample内容边界。不得放宽V2原生准入/削减正常50居民，也不能把同步Healed回调内clean Playing断言改成异步等到通过。实际独立BasinQualitySampleEntry也用Layout1，需明确适用边界，不把问题仅归咎测试速度。本轮尚未改该生产或测试逻辑。

post三raw全行核对为stdout373+stderr100=external473多重集；只有两条原预期WARNING及各自scope收据，无ERROR/FATAL/Handle/ObjectDB/RID。最后实名收据stdout367为中文挂载guidance，随后六AudioDrain完成到373。Drain打印后仍有ProcessFrame/QueueFree/两ProcessFrame，且没有case/root ID，不能把最后drain等于精确fixture释放或断言某处永久卡死。原75registry await没有逐例ENTER/RETURN，下一只加exact case名/physics/process/elapsed的前后标量，保持75例/180s/cleanup不变，再据最后未返回者收窄。未增加预算或静默重跑。

## 机械爪分析勘误

v074实际42样本最终P=(2805.4468,3728.6482)、V=(17.792732,-4.9219728)，anchor=(2856,3721.5059)、dock=(2808,3721.5059)，身体底3764.6482、棚顶3720。加0.25净空仍需44.8982上升。

按实际StepFree/ThrusterDynamics二轴模型，A+Space释放约V=(-12.9838,-582.7586)，理想上升91.79、离散无碰撞约86.95；D+Space仅147.085向上、理想5.85，不够。g1850/airAccel1500/reverseBrake3000/drag1.6且RiseProtected，不使用旧一轴预测代替当前代码。但这些只证明必要能力，不是实际碰撞/LOS路线通过。

旧181845冻结源码**已经**Arrived/Swing+A+Space、松A后D；实际boost1/release1，combined full-clear/Idle/Gallery/LOS未满足且151/150失败、galleryCast0。故这不是新候选，不重复同方案叫“第四次改进”，也不能借181036另案的Heavy遮挡解释尚未发第二K的181845。后续拆分同一失败输入下release→完整身体clear→Idle→真实Gallery preview/LOS的独立边界；仍共用原150预算，不改力/墙/HP/AI/目标优先级，不混用被动no-space诊断资格。本轮未改正常驾驶器。

## 保管与交付边界

八案40对source/helpers/user/continuation/binary清单完全相同、missing0/mismatch0；binary正确配对为fresh-binaries.txt→binaries-after.txt。一次审计误用不存在的binaries-before.txt产生8missing，核实命名后重核为0，未补造/修改原案。live1776/892C#逐行hash漂移0；最终普通二进制与6份保存字节保留，root原生全部终止，无其他桥接进程操作。

最终source manifest `8ECAF359A7CA3C7954952F8653067E574E0BA7A0F3624189AFDD6653DE7BCAB9`；binary manifest `180233F0E8F895F8F9F8541BD15982BA1AD64F0BFAAF8E541C0FEF5CCC5AC0BB`；Knight.dll `0CF7C9767FB7EE66C9A28AD6981A072C75348AD0F525E964B27868398BE07C6F`。Git分支/HEAD未写，index仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。

本轮新增最终图/声音0，无新媒体/付费/自动化/窗口/系统输入/Git写入/删除。一次正常汇报手动容量检查UTC16:17:19：current860284/base3508/growth856776 bytes，Notify=false，不停工/不建调度。全部十一项、正常Main/Explore/全76与新50实战、正式视听动作/UI/声音、自然路线、资源根因/历史RID、目标硬件/真人、ZIP与GitHub版本仍OPEN，不标完成或blocked。
