# 真实开火后机械爪打断与终态核对 v102

2026-10-25，工程 `G:/knight/Knight`。原十一项继续 ACTIVE；这是自动试玩消费者的局部推进，不是正式版本发布、难度调整或序章交付。

## 事实、推理、风险

事实：正常标题主线真实 J 已使 owned Burst 从2血降到1血；本轮在它真实开火后，通过现有纯键盘适配器发出一次 K，实际经历 Aim→Windup→Travel→同目标 Latched/Released，LegacyAdapter应用一次、敌人发布Stunned，玩家仍3血。最终同冻结主线也再次得到该收据。

推理：机械爪本身可以在原群战中命中并打断，不是只按了键、直接调用伤害或伪造眩晕。原流程随后仍不能完成反击，尚未证明这段策略改善整场胜率。

风险：主线最终仍E0死亡；探索仍在冰区E0死亡。完整混编回归再次原生FATAL，完整pre超时，post虽执行结束但有Handle错误。因此不能称整关、稳定性、正式视听或真人验收完成，也不能用旧绿色结果覆盖本轮失败。

## 本轮实现与红绿顺序

仅修改两份测试消费者：

- `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`
- `tests/integration/RangedWeaponPlacementRuntimeTests.cs`

生产玩家、敌人、地图、AI、血量、能量、伤害、冷却、素材和资源策略均未改；无新正式美术、音频或媒体。

1. 原Main先加真实开火后短窗合同。010320实际于首J接触后记录p6919/Rust警告`.36666662`、HP3/E`.2426651`，旧策略没K，窗口结束触发具名RED。
2. 只用原900外循环推进一次K，不调用CastAt45、不另开等待池。每次仍调用原共享危险扫描与PhysicsFrames/monitor；保原完整J＋6＋2门和24帧出手冷却。方向意图先清，等待真实新Input样本再松K；真实全局危险逼近就放弃并合法守盾，不重试。
3. 010908为声明顺序导致的CS0165构建失败，没有启动native，不当玩法RED。移动事件lambda到其捕获状态初始化之后。
4. 011044实际K已在p6925到达同目标，下一决策已见adapter+1/Stunned，但消费者先把保守安全窗0读作取消，随后才验证成功；具名断言FAIL。仅调换实际成功观察和取消判定的顺序，仍独立检查真实Active/晚警告，不改生产眩晕或J门。
5. 011435及最终012343实际成功打断；整场仍失败。不是完全绿色。
6. 一次新上下文审查C0/I1/M0：遭遇结束可能跳过循环内K核对。I1先抽出旧的无义务终态，012138实际RED拒绝“观察到窗口但没发K”。随后把机会观察与发令分开，终态要求完成/实际handoff/恰好1输入样本与1原始K边/未取消；区分根本没有合格机会的合法路径。最终013733窄门和原混编counter夹具通过。整体套件仍失败，故不mark task-done或整任务完成；无第二次审查。

新K始终由KeyboardRouteInputDriver→InputEventKey→真实采样/MechanicalClawController/原resolver/原LegacyAdapter执行，没有直接ApplyThreadPull、强制目标或第二次resolver授权。Latched收据同时绑定原NextCastId和实际目标RuntimeId。K释放后的Retract也仍响应真实紧急危险。生产既有真实存档语义不读取或改写。

## 十三次自有尝试

全部原件在 `G:/knight/验证证据/october13-stable-claw/`；下列为完整子目录名。13构建尝试：12成功＋1失败；12 native：3严格PASS、9严格FAIL。构建失败另计，不是第13个native。

| 子目录 | build exit / ms | native exit / ms | 严格结果 |
|---|---:|---:|---|
| thruster-route-main-diagnostic-20261025-010320-043-1347d233eaad40c1b000080ad5a393c2 | 0 / 24736 | 1 / 134053 | 缺K合同RED |
| thruster-route-main-diagnostic-20261025-010908-537-8df26c8b96434e1c9821bd427a1453e3 | 1 / 22877 | 未启动 | CS0165，build-only |
| thruster-route-main-diagnostic-20261025-011044-548-681deca84de2493b945e459c0d10c556 | 0 / 25647 | 1 / 126935 | 成功先后判断错误 |
| thruster-route-main-diagnostic-20261025-011435-476-af5cfb2436e240f8a1ea28d4d34fd4bd | 0 / 29490 | 1 / 147199 | 局部K成立，后续死亡 |
| ranged-mixed-resource-20261025-011942-804-0fd86ff44b6d4003855f5721e1f33c5f | 0 / 32061 | 0 / 16143 | 中间冻结四夹具PASS |
| ranged-mixed-counter-20261025-012138-928-a8ff27ac1b5c49819d99f12c061c3dd4 | 0 / 32192 | 1 / 622 | I1漏终态RED |
| ranged-mixed-resource-20261025-012247-418-ff61c5c19c6140ccab72f575aa364543 | 0 / 31647 | -1073741795 / 15798 | FATAL；仅2/4完成，不借中间绿 |
| thruster-route-main-diagnostic-20261025-012343-584-f527231a88c246c69dc32e6a8f8a8eed | 0 / 28837 | 1 / 142872 | K成立，主线仍死亡 |
| thruster-route-exploration-diagnostic-20261025-012644-780-b0e0f40d17494b5ab7703875b23f7fc3 | 0 / 27743 | 1 / 64115 | Frozen E0/request1/completion0 |
| prologue-pre-20261025-012825-606-2671e817c1cf4f2ba06dff498e1b08ab | 0 / 29094 | -1 / 180401 | 原cap180000实际timeout |
| prologue-post-20261025-013206-128-0e69a100a22a464dade0e46882eff253 | 0 / 29926 | 0 / 226702 | 原75/75及后置结束，4条Handle ERROR，FAIL |
| models-20261025-013635-127-ea6bc00ad5cf407795e9e08594709f7b | 0 / 30724 | 0 / 676 | 普通PASS |
| ranged-mixed-counter-20261025-013733-230-e9f1e78016fb49de92c8ae70457f2ca3 | 0 / 32453 | 0 / 4380 | I1反例及原counter窄门PASS |

最后七个native从012247开始同一source冻结，2严格PASS/5FAIL；前面RED/中间绿不拼资格。post的一条生命周期收据为 `repetitions=8 owner=RunPrologueTests nodes_before=1 nodes_after=1`，不是8个owner。post使用原本已批准实现的300000ms cap，不是性能测量；pre原180000未改。outer顺序批次最终exit0仅表示遍历完成，不能覆盖各phase的owner exit1或native崩溃。

### 最终主线与探索线

最终Main正常owned Burst真实首击后，K在loop349、physics6928记录成功（cast11、同一目标、HP3）；新K义务并未被“遭遇完成”静默跳过。后续仍无J、能量耗尽并自然死亡，monitor看到从上行区回检查点的大位移而拒绝原零复活路线；这不是本轮输入直接写位置。

Explore在physics2860由原zero-recovery门拒绝，request1/completion0；尚未抵达本轮K区域，不把它解释为新K导致。两路都仍未通，不是可交付版本。

## 一次审查与全部裁决

审查 `/root/v102_post_shot_claw_review`：Critical0、Important1、Minor0。I1已完成真实RED→窄门GREEN及原生重跑，完整suite仍红；没有Task完成标记或复审。以下逐项保留Declined成本：

1. 当前Main结果：root最终实际FAIL为准，局部K不替代全路。成本：战斗策略仍需继续修。
2. 新Stunned下界/HP1目标聚焦：本轮没有实现或审查，不混入绿色。成本：后续可能仍不够能量/全局窗口，不能承诺一改就过。
3. 全Main/Explore/十一项：全部完整要求保持ACTIVE。成本：正式关卡与交付工作仍大量未完成。
4. 资源寿命/cache/GC：生产未改、FATAL/Handle继续OPEN。成本：旧实验安静不能关闭故障。
5. W排名/物理首hit遮挡：本轮只证明一次真实同目标K，不重新认证全解析器。成本：其他方向和场景仍需实际覆盖。
6. Intro/preparation→Playing：原入口不变，不称本轮重新证明全部准备边界。成本：组合生命周期仍需验收。
7. 广义callback/deferred生命周期：只新增局部订阅/退订和真实同cast观察。成本：其他生产边界没有被本次审查豁免。
8. LoadLevel主错/清理错：未改，保留既有失败/证据；完整post不是所有异常注入的替身。成本：原广义清理合同仍OPEN。
9. 正式AV/硬件/真人/发行：用户NOTSEEN、没有新显示或物理输入。成本：必须在适当时段重新确认并实测，不能现在交付。

执行裁决：保留逐owned的既有Warning/Shot/Damage/Connection/Defeat合同，本轮用真实开火后的K，不用resident补该证明。成本是没有证明开火前击杀策略；该内部验收条件与玩家可先手击杀的合理性需要另行对账，不把它误当用户新增玩法要求。

执行裁决：短K还必须有同一扫描有限正SafeCounter，不能只看几何winner；非selected Skitter可能没有winner但仍贡献时间线。成本是未知0会保守拒绝真实可行操作；不以放宽J门求绿。

## 下一步：先证明，不当已实现

实际Stunned的owned Burst在p6927贡献保守安全窗0，但没有逐pass来源表，不能声称它是唯一0源。p6952能量已`.17466517`，低于原双AttackCost所需`.24`；即使改善这个预测，也不保证此时能发J。下一步可在现saved Burst真实ThreadInterrupt用例中建立消费者RED，仅用model捕获的完整Telegraph作为下界，不臆造Stun剩余；保留其他源/旧弹/失权/死亡/NaN的fail-closed。正常路线仍必须验证资源与接续，而不是只让预测函数绿。

资源另有尚未执行的同PackedScene、Free→固定GC/finalizer→再Instantiate、source CLR强根A/B计划，保持诊断与生产策略分开；不重跑旧quiet实验或猜升级、缓存、全局保活。前台/视听/相机/真人硬件/完整发行也仍待做。

## 保管、边界与容量

root开工前重核v101全1844 source零漂移。独立只读审计前12尝试：59对source/helpers/user/continuation/bin前后清单一致、66个OWN封存bin匹配；build-only案没有伪造native/bin配对。root核最后counter5对及6封存＋当前Debug6项匹配，合计64对、72封存bin，无已知缺失/额外/漂移。最终source1844＝1546runtime＋298imported，C#892；相对v101只上述两测试文件变动，无增删/生产变化。最后七案同source冻结；phase特有helper不冒称清单完全相同。

最终source清单SHA256 `F6B591E14A6970D7753770A69D1AD01305F48F014386D2D2E6071B34C135D1AE`；最后counter helpers清单 `248962A4636C5FFBB4B20FEB67989019400D035849F9591BB7C03AB8AC40B098`，bin清单 `878BA70911CD5775545FF5CC00675692CBDFF5E55136010F974F55E14525B6CC`。Main源 `ECEFD091593414AB5A949EF873CC587BC7C19363C6F08DCC48E8BEDA20CDEBFF`；RangedPlacement `7586435AB640B9CFD37BEDA3F14860A835BA327CD18A0A1467A2522D48EDDA38`。

canonical仍 `224A8035C12ED449A5F76B421D4F285F055667344A9A326F9C777E8130D00501`。branch/HEAD保持 `codex/task6-certified-final-20260902T0555` / `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`；index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 未变，原audio bus仍CAD39DF…E4EC7A。当前实际Godot引擎/dotnet/MSBuild/VBCSCompiler为0；godot-ai attach不当引擎owner且未触碰。所有owner终态后才写本报告/README。

diff-check exit0，仅169条既有LF/CRLF提示、其他0。本轮未重跑未改动的九个历史纯PowerShell辅助合同，不冒称重新通过；本轮实际原生结果以上表为准。无新前台/OS输入、付费生成、自动化、Git写入、打包、删除或真实存档语义读取。2026-10-24T17:41:06.2072006Z手动一次容量检查：Current860284/Base3508/Growth856776 bytes，Notify=false，未达新增20GiB。无新媒体，不建任何调度器。

原完整十一项目标仍ACTIVE，未complete/paused/blocked；没有task-done/finish/工作区清理或交付声明。
