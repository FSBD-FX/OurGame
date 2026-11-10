# v194：真实同阶段转盾与一次起跳，Main仍被第二发击中

## 事实

仅修改三个既有测试CS：MainRouteTests、RangedWeaponPlacementRuntimeTests、KeyboardActionRuntimeTests。未修改生产输入、移动/重力/跳跃、伤害、敌人、地图、爪排序或美术。原十一项目标仍ACTIVE/OPEN，不把测试驾驶推进当作成品视觉/手感增量。

原失败有两个时序缺口：Rust右格挡的Active/receipt结束后，旧generic策略才采样左盾；旧return日志还可能把receipt.Sequence0的历史transition记成回交。只调K或只转左均缺少实际交集。

本轮保留所有原K条件，只对取得prelude义务的确切普通Rust加较后输入窗口：`8/hz < remaining <= 8/hz + .14`，60Hz约(.1333,.2733]。`.14`来自普通Rust的不可变默认配置/模型构造重置，非未来Active的运行时观测或相位成功预测，实际采样Active可为9ticks。原解析器/LOS/24精力/停稳/支撑/全源安全余量不变。

左盾租约由原same-source/attack/current-epoch回执、实际支撑、无竞争动作及Hop stage1授予；只能覆盖无危险winner或原固定远程source，不能压掉别的紧急攻击。实际输入前检查driver所有权，清自身旧方向意图，使用原键盘驱动Left+L和自然刹停，不写方向/位置/速度。租约仅活到当前Active receipt结束或原Hop接管，非无限盾。

回交必须是contact之后的真实Left+L样本，默认/过期/重复回执不成立；未采样就过期严格FAIL。取得租约后终端还要求实际回交及原Hop stage5，不允许波次完成绕过债务。原中立/无盾单跳、三弹、HP/恢复、落地、回能及退休判据全部保留。

## RED→GREEN与最终回归

始终原canonical入口、显式SourceRows1872，未新增CS。build180s，policy/actions/models55s，Main600s，pre180s，post300s，原900外层循环未改；全部串行，native期间冻结源码/helpers。

| 案尾/阶段 | native ms / exit | 严格结果 |
|---|---|---|
| 805d48 policy RED | 979 /1 | 真实聚合RED：过早/非法late、未授予lease、过期/错transition return分支均暴露 |
| 515596 policy GREEN | 3781 /0 | 17 late＋9 lease＋8 return控制及原trial；仅策略合同 |
| a80bc6 actions初次 | 未启动 | CS0136局部motor命名冲突，build23888ms/exit1；非行为RED、缺fresh binary pair原样保留 |
| b8cd86 actions RED | 1728 /1 | actual Right+L/支撑与无租约保持已走过后，stub在接管入口真实RED，尚无Left成功 |
| 9fb194 actions GREEN | 9492 /0 | 6真实Left+L样本、自然neutral、无额外动作；早期版，不冒充最终Main同源 |
| f2c97c Main | 129461 /1 | 下述真实转盾/起跳/首弹通过，第二发实际伤害导致整案FAIL |
| 2a4a5f 最终policy | 3752 /0 | 最终冻结版专项PASS |
| c03d3f 最终actions | 9496 /0 | 最终冻结版完整输入专项PASS |
| 81a8a5 models | 507 /0 | 最终机械爪模型子集PASS，不是全部模型 |
| 6c2b70 pre | 174594 /0 | 原180s内严格PASS，不认证历史资源原因或性能已修 |
| 67dc2f post | 206200 /0 | 原300s内严格PASS，原75案例/owner8/退出清理齐全；非旧180s或性能认证 |

共11次尝试，含1次仅编译失败；10次native为7PASS/3FAIL（2个有意行为RED、1个完整Main失败）。最终Main/policy/actions/models/pre/post同源为5PASS/1FAIL。pre/post这次均0ERROR，post只有原4条限定WARNING（observer1、非Node2D fallback1、状态拒绝2）；没有据安静运行关闭历史Handle/Finalizer资源根因。

两个失败patch操作因hunk倒序被原子拒绝，没有文件变化/native，不算TDD。编译错误修复仅重命名新局部returnMotor，之后才得到真正动作RED。不得把这些失败合并洗成一次通过。

## Main实际执行

实际Rust=`16032190171906`，常驻远程=`4899567834185`，是不同owner。日志事件按physics排序：

| physics | 实际证据 |
|---|---|
| 6641 | 原K plan实际选中同Rust、LOS，remaining=.26666653；不是等到旧8tick门槛之外 |
| 6654 | 原同Rust守恒账本解锁，随后真实机枪手fatal/Defeated |
| 6714 | Rust真实零伤Right guard：attack5/epoch1/transition57，contact seq6684 |
| 6714→6715 | Left命令→实际Left+L seq6685，current receipt真，consumed=returned=57，ready mask15 |
| 6715→6716 | 一次Space命令→真实腾空，acceptedJump16→17、Vy=-590；没有二段跳/额外短喷 |
| 6736 | 真实顶点 |
| 6753 | 第13弹实际通过完整胶囊，采样最小间隙.67118263；自然回能已增加 |
| 6755 | 第14弹实际伤害，玩家HP2→1；不是Rust命中，也不是按键未生效 |

伤害独立回执为`TASK10_ACCEPTED_DAMAGE`：source/ActorId=`4899567834185`，LocalAttackId14，stableId=`canyon.v2.resident.counterweight-lift.04`，attributed=true；盾实际关闭。后续原`MixedGuardRetained`包含HP保持条件，因此报`airborne engagement identity cannot disappear`。退休observer的两条衍生fault不能倒置成受伤前原因。恢复0/0，无死亡reset。

结束时Hop stage3、Space边沿1、shots2、passed1、natural_increase=true；第三发、完整落地、stage5及Main均没有通过。未知退休原因仍不因同帧格挡而被认证。

## 推理与下一步

两项输入衔接确已跨过原断点，但当前一跳在第二发到来时接近落地。下一步先用实际`PlayerMotor.MotionSettings`、胶囊、弹体公开shape/位置与发射时序量化完整三弹安全窗口，不再凭感觉逐次挪K。名义默认JumpSpeed590/Gravity1850与观测相符，但不能代替实际快照；ranged作者Resource也不等于私有运行副本。

原Hop明确要求中立、无A/D、无L/盾、一次普通跳、不加K/Shift，Space持有至实际顶点；落回同一完整floor、稳定两样本、自然回能≥24且三弹全部被确认通过或合法ownerIdle退休。不能通过空中横移、落地早开盾、删弹/补血或扩大预算绕过。若量化发现合同与现行物理不相容，须明确指出并核对，而非冒称可行或静默改验收。

当前Main失败不自动证明真实玩家不能用其他已存在操作躲弹，也不能说游戏已经平衡。当前完整Explore未重跑，旧证书不续签；正式画面、UI、音频、硬件与真人感受均OPEN。

## 审查与裁决

唯一fresh最终审查C0/I0/M1，无C/I修补或二审。M1延期：旧`fallback_shield_command`在lease决策前记录generic候选，6714可能写False而实际Left+L为True；应以后重命名候选或在实际分支后记录。实际输入/回执/严格门不受影响，解读以真实Left命令与输入样本为准。v192两日志Minor仍历史延期，不重复计数。

拒判事项逐条保留：后续第14弹修法不属本次delta（代价Main仍OPEN）；退休原因未证实（需独立源证据）；审查未跑广域suite/Explore（root跑当前必要回归，Explore保持OPEN）；未认证全仓库旧逻辑/生产平衡/资源/真实存档（不据局部覆盖）；原十一项及渲染UI音频硬件真人仍未验收；限定SHA/log审查不代替完整新案保管，另由只读核账闭合。租约新增严格stage5债务而非放宽要求；未来可行性需实际证明。

## 保管与权限

证据根`G:/knight/验证证据/october13-stable-claw/`。完整案名：

- `ranged-mixed-policy-20261101-084802-318-805d487994e9464f8819a6525231e545`
- `ranged-mixed-policy-20261101-084926-352-5155961fc398450a8c45a4d98694c2f2`
- `actions-20261101-085124-754-a80bc63ed4ab428f82d8183875389c69`
- `actions-20261101-085247-555-b8cd866d875540b1a40266212fd2879a`
- `actions-20261101-085438-984-9fb194a19023414fa306c7b5d407c89c`
- `thruster-route-main-diagnostic-20261101-085725-638-f2c97ceea7404798aa3aabf5f46b0d72`
- `ranged-mixed-policy-20261101-090327-264-2a4a5f3981d54efba275a7be3a15d391`
- `actions-20261101-090511-328-c03d3f5987c44a4e81dfbca8331f461a`
- `models-20261101-090736-604-81a8a521078e43209f27bf6b83b1a747`
- `prologue-pre-20261101-090854-731-6c2b70d4c60b472780b8a31f7b1a5d0a`
- `prologue-post-20261101-091256-490-67dc2f0407bb405b800afefab204043b`

最终Main及后续冻结源清单SHA=`DC054B0A53ACCA52BD040A35A2D80B6D775AB1F56A76DAB7D0924CAC764CC117`，DLL=`9F64F555C0C0AC7DC2B5AA144AA6FFFCB55546C3925C2C52B373AE11606EEBD8`。Main=`AA5D22F21D33008FC75AE31DDF1BC0726A7F61154BA34FA39BC9CF1212264A75`；Placement=`5D16F41AE4CF727EE134EC69CCA9A5ED868AF3C9795CA2C74D2BD27A23BD2533`；Actions=`D386C59A84847D4F64D615EB4803E0DC7FD3E9CD30EAC8D22C1B6ED9C210F820`。

独立只读核账已闭合：54对有效manifest、5244份所选source副本、120份helper副本、60份归档bin逐SHA匹配；编译失败案缺失binary pair未计成功。最后post build26887ms/exit0，native206200ms/exit0；前十案未重复重哈。09:18:23 +08当前1872 source、6 bin和post10 helpers匹配最终post冻结版；09:19:09 +08按路径/创建时间核对21个owned进程身份全部终结，21个不同PID均不存在。

最终binary manifest SHA=`1D5661FA46433B1AE61543F29A3F641782E88F17DBB145608BC7F9671B81E9D3`，post helper manifest SHA=`48D515B7A1AB55E140806BE7E146CC24EF185CE77B91BB440974849706C9B28B`。原canonical SHA=`51C7A18450CEE756172F7D35DA0F7611069A6616AE88EA114112F7DAC77017C7`、lane SHA=`D158BB15CB4C3EA7FA02AA11C6A63D29D98FBA60C08B5EF723FB991DF54CE9BE`未变。

09:17:42 +08手动媒体检查7427750bytes/86files、基线3508、Notify=false，本轮无新媒体。HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index SHA仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。全部root执行句柄已终结，无重复native或继续取样以筛选通过。

只隔离headless构建/测试及Godot内部键盘事件，无图形窗口、系统输入、真实存档写入、付费生成、额外崩溃采集、误发视频分析、自动化、Git写入/发布或删除。原完整目标保持ACTIVE，不宣称完整Hop、Main、双路或成品完成。
