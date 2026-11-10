# v319 · 普通零伤格挡的双臂接招

## 事实、判断、风险

继续原完整十一项的V03/V04/R-C，而不是把v318脚图接口当成可见角色动作完成。唯一工程`G:/knight/Knight`；仍只无窗口。本轮先核对：地面/空中挥刃、制动承重、转身附件均已有，不能重复从零算进展。

新确定的制作缺口是成功Normal正面挡：真实`ShieldResolved`结果零伤、零击退，Hurtbox提前返回而不经过Health.Damaged，因此既有Hurt姿态不能补它；协调器之前没有把该事件送到实际腕肘。原程序并非没有举盾/持盾/放盾，这也不是新发现的游戏规则错误或开发者已看见坏画面的证明。

选择独立接招表现而不复用Hurt（会混入错误受伤取消/姿态），也不只新增盾面闪光。root暂定单次峰3px、omega25，作为现程序关节样段，不伪称用户批准参数或最终优雅。不会产生真实击退、整图放大或人物根位移，不改伤害、能量、输入、地图和保存资源。

## 实际缺功能RED→动作GREEN

测试使用保存Player、真实逻辑L举盾和Hurtbox.Receive的Normal请求；另一保存Player的Formal重放同一已发布frame，形成匹配姿态/时钟的no-hit对照。冻结原生processing后手动推进Coordinator._Process，明确是受控表现时钟，不是正常速度、OS键盘或主线通关。

`thruster-rig-compatibility-20261108-121833-229-0953a5a2b4a44712a4428e02147e05a1`，1908输入、engine1／815ms：真实零伤命中及重复身份拒绝先过，.10s近腕对照差为(0,0)，在`GUARD_IMPACT_MOTION_RED`功能断言失败。不是缺类、反射或编译假RED，guards=[]，原始失败保留。

随后才实现：

- Coordinator仅接成功普通零伤事件，过滤无效/暂停/排队/非当前身体来源及被其他真实动作接管的情况；正式Visual变化、源退出、恢复等清理。
- 新Formal partial只存纯值的世界空间临界阻尼弹簧，无新Resource／第二个Process或Timer。消费原PresentationTimeSeconds的正elapsed，单次速度增量`-aim*3*25*e`；速度≤255、位移≤6，限制处撤销外向速度，密集不同合法身份不能无限堆积。
- 世界offset转换到当前双臂局部，近臂全量、远臂.55，均乘未回握权重；接在原近腕→原武器主握点→副握点求解顺序，保留两个24px骨段。已显示offset在放盾/动作接管中回零；相同clock不推进、死亡/恢复/源撤权清除。

初次GREEN `thruster-rig-compatibility-20261108-122106-800-fb7b89efb444491dac66ade67ddc4be4`，1909输入、engine0／1639ms：左右×30/60/120共6组真实事件／腕肘／原bitmap主握点、HP/根/velocity/骨长、重复身份／17合法来源、有界脉冲、零时钟／.60s回稳和放盾连续性均过，原11leaf／2mantle委托保持。

## 新回调租约反例与修复

追加lifecycle后实际RED：`thruster-rig-compatibility-20261108-122647-007-23e0a72fd3a340e5bda7d0764942715e`，engine1／1265ms。原6组和普通detach立即重画／reattach不重放／fresh事件均先过；测试在真实ShieldResolved的先行observer内移出再挂回同一coordinator。C#旧调用列表随后继续调用原handler，虽对象“现在已入树”却属于旧租约，`GUARD_IMPACT_LEASE_RED`错误出现腕动。原件保留，不混成资源FATAL。

只在该真实反例后把新监听改为捕获sourceId和epoch的delegate；Detach先增加epoch再撤销该delegate，旧调用快照即使对象重新入树也拒绝。没有改变测试预期或玩法。

审查前专项GREEN：`thruster-rig-compatibility-20261108-122839-281-a8eef5d890184cf18a4707a3ed8c6f08`，engine0／1811ms、failure=null／guards=[]、三sink诊断0。原CONTROLLED及新增LIFECYCLE marker在stdout／external各精确1次；旧11leaf／2mantle同样通过。真实Health致死与公共ResetFull／ResetRecoveryPresentation后无旧脉冲，typed GroundAttack同clock接管／8次零时钟重复／.60s回稳通过；后者不是实际J攻击或关卡恢复路线。此版本随后发现下述两个遗漏，不作为最终修补清单。

## 审查前中间版回归（非最终修补清单）

- 1909输入source manifest：`EEC89D47B5E08EC2A1100D20FBD4E8A98D3A2B7D2BA30905324D37A00E74E1A6`。
- 六binary manifest：`BD42C4043761D03EF05B465CA3C339210D9205AFE3AEA17FEC8D16A20CF64C33`。
- 公共9helper manifest：`0DB303C2B17EC8ACEDFC419947051512BC1067EE54A21A1B218598413452ADCF`；canonical仅增加新partial的case源码快照，原marker／错误门／旗标／预算保持。
- 真实用户保护清单仍`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`。

完整input对v318：仅Formal／Coordinator两个原生产文件、新Formal partial、两个原测试注册及新test partial，共6个C#变化（4改／2增），无删除；没有Tuning／素材／场景或地图变化。

中间版串行batch60213已真实exit0：models9896ea...、原rig d79cf1...、普通pre01e7a7...（44项）、post afa2c5...（75项）及save5ee72...均runner0。它们只属于EEC89／BD42，不能覆盖随后两项review修补或拼成最终版完整路线；最终输入的回归另列。

唯一增量review为C0/I2/M1。root认可两个需修的生命周期问题：活体调用公共ResetRecoveryPresentation没有清pulse（原测试先死亡已掩），以及Shield节点自身exit/reenter没有触发coordinator epoch撤权（原用例移除的是coordinator）。独立review只做源码审查；root在batch60213真终结后才补以下实际聚合RED与一次修补，不另开二审。

## 两个审查遗漏的聚合RED→最终专项GREEN

`thruster-rig-compatibility-20261108-124246-313-774279b20aa0442ab532004c4006d036`：1909输入、engine1／1625ms、guards=[]。三个独立saved-player对照完整执行；活体且已有脉冲的公共reset、仅Shield离树／重入、ShieldResolved先行observer内Shield自己移出再挂回三种场景均满足严格前条件，三组fresh contact均有效。五项行为检查失败：前两组即时near3／far1.65、.1s后near.8619／far.474；旧来源回调错误触发near1.6735／far.9204。统一`GUARD_IMPACT_REVIEW_RED`在各组执行后才失败，不是首例掩盖后例、假fixture或永久关闭动作。

只在真实RED后，root同一轮修Coordinator：公共Reset明确Clear；Shield.TreeExiting同步增加source epoch、移除resolved observer并清当前绘制；TreeEntered重新绑定捕获source epoch的delegate。原coordinator epoch仍保留，两个租约独立；Detach对称撤销resolved／exit／enter三个delegate。原冻结测试期望不改，无Resource、玩法数值或新时钟。

最终专项 `thruster-rig-compatibility-20261108-124448-068-376c014e8b9441c7980a0a0cf45323c6`：engine0／2086ms、failure=null／guards=[]。CONTROLLED／LIFECYCLE／REVIEW_BOUNDARIES三个新成功marker在stdout和external各精确1次；8条review-case两sink一致且全部true，五项撤权near/far均0、三次fresh均有效；stderr空、三sink诊断0，原11leaf／2mantle委托保留。

最终冻结source1909：`1D555425EE055A0C7DF1754C002E459C42F3798345DFF4BB9434D0290A3F8480`；six-bin：`0E7191B6D1BF218658EF4DE23D5BC0DA5054A2A63755BBD99089736983B23E19`。common9helper及true-user仍分别0DB303C2...与BDDA0FBE...，案内前后一致。专项provenance SHA256为`35275FCBAE8F41B484924EF215F7F6DEACDDF9FF093419820D1A0BC9928DAEBB`。

最终串行batch37884真实terminal exit0，无重试筛绿；连同最终专项，7个指定phase均属于同一1D555425／0E7191B6：

| phase | case（均在`G:/knight/验证证据/october13-stable-claw/`） | engine ms | 结果 |
|---|---|---:|---|
| thruster-rig-compatibility | `thruster-rig-compatibility-20261108-124448-068-376c014e8b9441c7980a0a0cf45323c6` | 2086 | strict PASS，新三marker各1／原11leaf+2mantle |
| models | `models-20261108-125045-524-2cb24b45d36e4a98a7fed755074dc376` | 501 | strict PASS |
| thruster-presentation-rig | `thruster-presentation-rig-20261108-125115-216-0718264183234a2787d09cfe401820e2` | 18469 | strict PASS |
| actions | `actions-20261108-125202-767-1ec0fbc343bc4f09bc3bd3ef50df9205` | 10783 | strict PASS |
| prologue-pre | `prologue-pre-20261108-125247-479-64a47aacf0cf440986af45a38533c430` | 146223 | strict PASS，原44项 |
| prologue-post | `prologue-post-20261108-125545-984-0e2a24ce816e456097def6480b65cc09` | 187689 | strict PASS，原75项／retired0 |
| resident-save-compatibility | `resident-save-compatibility-20261108-125920-866-07a6bb640c294214b618720804f97acf` | 11721 | strict PASS；独立冷读1513ms亦PASS |

所有build／engine均exit0无超时，native PID与contract一致、headless／非fixedFPS／无movie／无OS输入，案内source/bin/helper/user/continuation前后均相同。root读回1909当前输入、6当前binary、42封存binary及12唯一helper全部0差异；当前无owned Godot/dotnet遗留。common9相同，rig/pre/post等各phase原额外helper不误判为漂移；true-user保护清单始终BDDA0FBE...，没有真实存档实验。

普通post只保留原合同4条负例warning：observer故障隔离1、VisualScene错误根类型1、Completed→Recovering拒绝2；external／stderr各记录同一组4条，不是8个独立错误，stdout无warning。其余6phase和保存冷读三sink诊断均0；无未解释ERROR／FATAL／Handle。post187689ms使用原普通300000ms门，不等于旧180s性能认证，也不因本轮未复现而关闭历史资源原因。

保存writer PID28816／独立reader PID46024，6witness只读、旧core／稀疏／全／零及grip8链原marker在对应stdout/external存在；冷读实际1513ms exit0、隔离storage，不使用真实玩家档。

最终Coordinator SHA256：`1BFC33814F62BA969442B4B9AFB9BBAFBC36E3CF72F1F9527726CC28667FDF74`；最终新test partial SHA256：`8C813FBF97EC64F1730434B27CFEA69A1848715D771EEF74B67EF18196F00ADD`。小计划程序实现／回归／一次审查修补已完成，完整双路线最近仍只绑定v317；不将其拼给新程序集，也不签正常速度／正式美术／原十一项。

本次新增覆盖Minor保留：真实暂停／排队来源／direct API异常乱序／脉冲中翻面／真实J与爪接管专项；17个攻击者身份不是controller替换测试。Review也没有代签正常声画、参数优雅、正式美术、实际checkpoint或整关、历史资源原因与整个脏树；旧shield注册前原件缺失，不宣称其独立逐字节历史比对。

## 未覆盖与继续项

当前还不覆盖正常速度像素、正式角色总装／完整动作设计，不能据3px与测试数称“优雅”或R-I/R-C/R-P完成。真实checkpoint RecoveryRequested/Completed路线、实际J／claw接管、direct API异常／乱序输入、queued-source各组合未单独新增动态用例；源码门和已有旧测试不冒称这些已新实测。原资源根因、最终同版双路线、声画／硬件／真人及原十一项ACTIVE。

另一个只读候选已证实源码行为但未native：coplanar A[-100,0]/B[0,100] top36，root -28→-10→2，owner切换会把near(-20,36)/far(-19,28)同拍夹到(2,36)，清swing／phase而weight仍1；不是v227重复抬脚。先须建立真实相邻support及旧支撑撤权的证据，不直接删clamp或用缓存虚构已销毁地面，不声称地图全处复现。

后续有界只读定位已找到真实保存场景，不再只靠手写矩形：

| 地图接缝 | 世界顶面与边界 | 保存／消费依据 | 未证明范围 |
|---|---|---|---|
| 入口BridgeFarBank → 裂谷RiftUpperFloor | y636；x400..1280 → 1280..1900，gap0／step0 | `CanyonEntrance.tscn:244`、`CanyonDescent.tscn:901`；parent offset(1280,0)，`CanyonContentBuilder.cs:277,322`；`canyon.v2.route.bridge-to-rift`为GroundTransfer/None，起1190..1260落1300..1420 | 不同StaticBody下矩形真实shape，净空236；可在桥后x1200→1400普通走行检验，尚未native确认切换帧或实际腕脚跳变 |
| 盆地BasinCampFloor → GearEntry | y3996；x1280..2080 → 2080..2380，gap0／step0 | `CanyonBasin.tscn:1035,1049`、parent offset(1280,3600)、builder:428 | 同sector对照可排流送；保存合同虽标Jump不等于物理必须跳，仍未实跑 |

实际SupportId生产者为`PlayerPresentationCoordinator.cs:901..939`的slide collision／shape instance ID。不可混用入口StartFloor→BridgeFarBank，它们有80px真实缺口，不属于连续共面交接。下一轮须先实际Player走过及旧支撑消失负控，再决定连续交接规则；本轮没有改步态或地图。

无GUI／OS输入／付费／新native采集／真实存档实验／引擎cache/GC/预算更改／Git／删除／自动化。无新过程图片或视频；正常汇报时才执行既有一次媒体容量检查。

本轮最终一次媒体`-Check`已执行：15,203,531bytes／145files、Notify=false，未达新增20GiB；没有独立提醒／自动化／停工。原完整十一项goal保持ACTIVE。本次沿用自主推进、唯一脏树和无窗口范围；暂定幅度／骨架几何只能支持程序样段，不能替用户认可正式动作美感。
