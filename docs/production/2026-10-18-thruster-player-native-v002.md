# 实际Player短喷、键盘预算与回调边界原生证据 v002

2026-10-18 Asia/Shanghai。完整目标1–11 ACTIVE，未完成。本文件承接v001，保留旧证据；不是正式游戏发布、最终美术或真人试玩验收。

## 本轮实际变化

- 旧空中预算夹具改为真实物理键W/K/J/Shift/Space、固定pre-tree锚点、合法完整胶囊出生及completed-physics观察，不再跟随鼠标搬锚点、关地板碰撞或要求Shift与K同帧同时执行。普通完整跳为合法测试窗口，未改游戏跳高或增加次数。
- 旧拒绝Shift“速度恒定”断言改为独立两tick合法推力／空阻／重力对照；下劈按真实落体提前施放，目标与命中框不移动。旧测试换区房间补有效typed Art节点，不开启production fallback，不把这个节点称为完成美术。
- **修复已复现queued删除漏洞**：撤爪Idle回调QueueFree后，旧Motor仍起喷。现在实际角色或任一祖先排队删除即撤销本次移动权，旧tick不再提交／移动／花次数。
- **修复已复现回调暂停漏洞**：真实Space借力发布Boost后，回调暂停仍继续改变速度。现在按同Claw authority保存唯一未完成释放阶段；恢复不重发Boost/Space、不重复unpin或应用速度。reset使旧续段失效，不保存第二份速度或按墙钟补力。
- 已有合同证明补强：Aim/Travel/Reel/Swing四阶段分别接受接管；同四阶段耗尽空中预算时拒绝而保持爪；真实1200px/s来速不被850投影上限或旧620固定值直接压掉；false驻留回调重入reset；暂停Boost、unpin、Released及暂停中reset。
- 27个自动saved-Player物理场景通过，另有1个scheduled实际接爪对照。独立预算矩阵13cases明确为11自动键盘＋2旧scheduled适配器，不能全部冒认真人／自动输入证据。

## RED → GREEN

1. queued删除RED：`thruster-player-20261018-143233-601-23a0ac7906fc4de7919097b1562bdd90`，engine1/5828ms，accepted应0实际1。最小owner/祖先检查后GREEN `thruster-player-20261018-143406-644-b2dba2be4e9b457f9dc9ada26e8625be`，engine0/8208ms。
2. 回调暂停RED：`thruster-player-20261018-143743-895-fe65944039804cd48b1b5a69111f5c0f`，engine1/9357ms，暂停点速度(0,-36.613518)仍被改(220,0)。三暂停点GREEN `thruster-player-20261018-143917-959-b48a350f8c6e48ca8cc23fc42e4607bb`，0/14001ms；加入reset/重入后27cases GREEN `thruster-player-20261018-144045-241-e5c63862ced2422cb44c2657aa565f4b`，0/15747ms。
3. 旧鼠标fixture失败及中间不成立的前提均留档，不当作产品预算漏洞。预算矩阵first GREEN `thruster-traversal-20261018-143447-011-6420ad50bc334544b7e84da7d563234c`，0/7332ms。前摇窗口及真实落地前提纠正没有提高能力、关碰撞或扩大时间上限。

## 同一冻结源的18项回归：16 GREEN，2 FAIL

所有case在同一source1685下逐案fresh Rebuild，0编译警告/错误；每案source／helper／userdata库存／binary before-after逐字节清单哈希相同，guards=[]，无ExpectedFailure豁免。原生期间所有源码/辅助脚本/文档冻结，完成终态后才更新本文。

| phase | 结果 | engine毫秒 | 独立case |
| --- | --- | ---: | --- |
| thruster-player | GREEN | 15942 | `thruster-player-20261018-144230-731-783d7c9654c5431583f7f8794f3b477b` |
| thruster-traversal | GREEN | 7324 | `thruster-traversal-20261018-144318-791-e4bf7d5d8b85480a937bf430cfb0f016` |
| thruster-module | GREEN | 671 | `thruster-module-20261018-144359-214-9f5856a0a9974e489296f4165cdfbcfc` |
| thruster-dynamics | GREEN | 670 | `thruster-dynamics-20261018-144431-323-2bd653ca91e04501ae7ec6190ece751c` |
| thruster-input | GREEN | 667 | `thruster-input-20261018-144513-094-309b0e0ab9b34ec5b908cdcae22d928b` |
| thruster-pulse | GREEN | 553 | `thruster-pulse-20261018-144546-819-cc58841674ca47adbe6d441f9e06124d` |
| gravity-delta | GREEN | 666 | `gravity-delta-20261018-144619-691-9b035870b544461a8e4ee4a02eeb85e9` |
| keyboard | GREEN | 9824 | `keyboard-20261018-144651-087-471b238e1a4546e89de7cf701f56c174` |
| actions | GREEN | 7088 | `actions-20261018-144731-956-a188e2412ef041328a64928e0de57619` |
| updraft | GREEN | 9619 | `updraft-20261018-144808-678-bc67ae3d20eb4047b21ad707c50a9efa` |
| fall | GREEN | 11561 | `fall-20261018-144847-549-7498b00608854817934ccd7eb2904765` |
| fall-claw | GREEN | 9091 | `fall-claw-20261018-144928-328-a88fb9c340244be0b76a2bb56164e0f0` |
| boost-presentation | GREEN | 1534 | `boost-presentation-20261018-145007-736-26143656deb7441bb1af3644b5b1277f` |
| contact-safety | GREEN | 7704 | `contact-safety-20261018-145040-730-821e2845ff094560839bebde8569ab3f` |
| arrival-transition | GREEN | 12597 | `arrival-transition-20261018-145119-769-b4c49124f9e64450a71cf595405de9ff` |
| swing-loss | GREEN | 7291 | `swing-loss-20261018-145203-342-5e43fc8909e1470bbc1b5355e0957078` |
| prologue-pre | FAIL | 88514 | `prologue-pre-20261018-145240-364-dc56626063c54c2e8b0997a7dde831e9` |
| prologue-post | FAIL | 8445 | `prologue-post-20261018-145440-874-2134b1e0edc4436b8490d79670c2c847` |

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`；各case保存build、engine stdout/stderr/external、provenance、源/辅助脚本/二进制/用户库存清单。不读取真实用户存档语义。所有owned消费者已终态，未遗留原生进程。

- pre已越过旧鼠标预算fixture，首失败回到UpperPendulum `lower_to_mid_swing`，sample0.5，start(152,172)、final(264.99634,160.6107)，150帧内未到落地区；另两条旧PresentationProfile警告仍使严格验收FAIL，未豁免。
- post仍Bell测试真实落地(540.0704,1196.0139)失败及KillZone恢复父路径未落ILevelRecovery。已有裸fixture出生与父链的源码候选原因，尚未原生修复；不把此测试失败直接等同正式Boss仍发生同一掉落故障。
- 风区保存深井：主动A反向制动6帧后下落4.633秒，253风采样，原底台(2220.1887,3523.9749)、HP5。不是“松键被动必落底”证明；普通新玩家的刹车提示／正常路线仍待体验。
- 长Boost/到达日志未逐一重列所有姿态向量，不冒称已审阅全部drawn画面。Headless drawn=0不是实际显示帧率、原速音频、最终视觉或human验收。

## 仍未完成与下一步

**Task3仍in progress，不能在上述GREEN后悄然标完成。** 只读检查发现下一待原生复现边界：Boost回调内或pending暂停期间调用Motor.ExternalImpulse，仅推进Motor motion revision，Claw pending目前只验Claw authority，旧BoostDelta可能叠回替换后的速度。下一先用真实Swing＋自动post-Claw/pre-Motor观察器复现；新运动身份必须在Publish(Boost)前冻结，不能在回调后才把新revision当旧授权。此处是源码支持的待验假设，不是本轮已修复结论，也不意味着ExternalImpulse应全局取消所有合法附爪。

此后继续Task4可靠着陆减伤、Task5保存模块装配、Task6typed喷射/制动视听、Task7全部旧消费者／正常双路线。着陆规则不能先把高度基数截成MaxHealth再乘factor；仅完成调用调查，尚未实施新算法。

完整1–11范围保持：正式新常驻0/50；地图、76普通＋1Boss、全部机关与正式原创资产、最终UI/动作/特效/音频/演出、正常Main/Explore、旧档/死亡/继续、720p–4K、显示稳定60FPS、人体验、版本Windows ZIP／GitHub归档均未最终完成。三项历史资源/Handle/RID失败此轮未重跑，连同pre/post共五项仍OPEN。没有新媒体/资产、付费、前台按键、Git提交/发布/打包或自动化恢复。已有资产表无增量。

技术工作区：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`；HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，branch `codex/task6-certified-final-20260902T0555`；原dirty保留。代码diff whitespace检查未报告错误，Git仅提示以后触碰时LF→CRLF的既有换行策略。

