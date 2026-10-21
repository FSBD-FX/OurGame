# 旧高层碰撞准入与正常窗口复验 · v025

2026-10-19，Asia/Shanghai。开发证据版本，不是发布包。完整1–11目标 ACTIVE，Task6/7未完成。本轮仅修改测试规划器、被动取证与保留范围；生产Player、地图、敌人、伤害及美术未改。

## 事实、推理、风险

事实：原旧高层 `.2` 在真实借力后的下一步撞到回返天花板左侧，水平速度归零。单纯等待无碰撞且第一触地就完整支撑的窗口仍失败。独立真实圆角滚上控制成立后，规划器允许可行的部分接触及原预算内地面校正；三个原出生与完整post中的33个mandatory样本均通过。post下一首错是裸神殿Boss准入前提。当前同冻结正常Windows Main完整通过；Explore过蒸汽战斗及Step1–8，但Top额外起跳后触发FALL RESET，严格失败。

推理：需要分别修正消费者的碰撞／支撑假设与裸fixture准入接线，不能为测试反改获批自由力学。正常Explore的Top入口缓存支撑是Step8，而旧headless是Top；旧“已在Top”分支不足以覆盖两块同高重叠地板的接触选择。

风险：预测查询、独立midflight控制、原出生完整路线、正常窗口和真人是不同资格。当前没有正常双路全部通过、真人手感或稳定60FPS认证；五历史失败、音频2 ObjectDB、匿名FATAL、正式新50为0/50、24canonical、最终内容及交付全部保留。FALL RESET时HP仍1、fall_damage0，不称已测得致命伤害。

## 十二个原生案例

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。全部fresh Rebuild退出0、0警告／错误；无timeout、guards=[]。headless五阶段批及正常双路批包括阶段间隙冻结source/helper/docs，所有消费者终态后才写本记录。没有运行生成正式场景的`tools/verify.ps1`。

| case目录 | build ms | native退出／ms | 实际结果 |
| --- | ---: | --- | --- |
| thruster-legacy-ascent-high-20261019-045938-360-1a8238aa171f43a0bb3cab3c8c396019 | 27939 | 1／4983 | 原控制被动trace，原`.2`落台FAIL |
| thruster-legacy-ascent-high-20261019-050534-494-13f6c60bf8b44cdbb5070a9313d60bf1 | 26230 | 1／4945 | 仅碰撞veto仍FAIL，无Space／Boost |
| thruster-legacy-ascent-high-20261019-050939-137-84feea5e2a974be58c2993734eea84a0 | 25666 | 1／831 | 独立控制预树HP期望初始化错误，非物理RED |
| thruster-legacy-ascent-high-20261019-051040-490-596b35bd5b444945ac6e213a9834d2ef | 27482 | 1／5491 | 独立圆角控制GREEN，原`.2`仍FAIL |
| thruster-legacy-ascent-high-20261019-051313-777-0242d414bcc64ea09807bebdc0727121 | 28473 | 0／5494 | 原三个出生完整路线GREEN |
| thruster-free-forecast-20261019-051456-284-036487a1800a4871928582293cb7c18c | 26921 | 0／664 | 独立离散预测GREEN |
| keyboard-20261019-051531-034-10f6207222ff4f018afb4a397ed7c32c | 26415 | 0／9933 | 原键盘回归GREEN |
| actions-20261019-051613-748-bb272dad90dd43169f10d465a27be416 | 26812 | 0／7342 | 原动作／过热后攻击GREEN |
| prologue-pre-20261019-051654-860-a991d04d2ea048e48a4ec7f7d1132bfc | 30918 | 0／102635 | 完整pre GREEN |
| prologue-post-20261019-051917-577-1c8dd471b26e4a8dad66d069454f21e7 | 26547 | 1／103201 | 33 mandatory／四optional过，Boss前置复合断言FAIL |
| ranged-route-main-20261019-052245-717-7d4d55a76b9247eebdf64f46846c60be | 26594 | 0／199575 | 正常Windows自动键盘Main完整GREEN |
| ranged-route-exploration-20261019-052641-431-b9ea643b7b944b7ba1adb7df7d6eb28c | 26664 | 1／115575 | 正常Explore在Top恢复1/0 FAIL |

整suite及正常双路均未全绿；不按通过比例计算整个项目完成度。全部失败原件保留，无named waiver、日志忽略或删除。

## 旧高层：被动边界与最小消费者修正

045938记录第二Space请求起12个physics、55/128行，omitted/fault均0。原P80请求；P81同步Boost／Released的payload及当时body速度均为施力前`(239.1569,-41.23653)`，Claw后实际`(370.166,62.835815)`，Motor后`(360.42535,93.66915)`。P82首个已采样slide index0/count2命中CeilingThree **32631686833**，normal(-1,0)、point(849.92,630)、depth.109313965，vx归0。不能把同步payload称施力后速度，或把后续缓存接触称新碰撞；本记录只覆盖已采样index0。

原trace字段`rise_protected`实际读取`HasGrappleReleaseMomentum`，并非独立`GrappleRiseProtected`观测；新版本已准确命名`release_momentum`，旧日志不回写。原045938实际selected165漏复制观察器定义文件，完整source库存虽有hash也不等于已实际复制。root在 **2026-10-19T05:05:33.2305109+08:00** 另存到`legacy-high-observer-source-supplement-20261019-045938-v001`；只补静态来源，不追认原before/after资格。后续focused/post明确保留该定义，SHA `7B7A60CD0F0A8EB4115CCC91D0883611B51B787249343B7FB749634FD9A97DC6`。

首次veto原`.2`63候选／1003查询／52阻断，没有允许的完整落点；未请求Space，因此trace未armed，不是记录丢失。独立控制预树设置midflight `(806.74164,647.04816)/v(240,41)`，挂树后仅原D／neutral，无live pose/velocity写、跳喷爪或救援；真实partial `(848.8465,678.8758)`滚到`(899.84,677.999)`，29/166、stable12／原15%／HP5。首跑误把Ready前CurrentHealth0当期望，改为保存MaxHealth5，没有改真实HP。独立控制不证明原出生路线。

最终planner仅用于`targetOverride=null`的原`ascent_grapple_three`：实际Motion/Module自由步和完整胶囊TestMove预测至LandingThree可行up-normal接触；部分触角须在原剩余额度内用原D／neutral地面平面预览到完整支持、原15%／速度2，并保留12稳定帧。平面预览不是live snap或实际运动。原最终AssertLanding、240预算、碰撞、爪力、HP及全部出生不变。

| 原出生 | 实际路线帧／240 | 最终实际位置 | cast/latch | Dash／第二空跳 |
| --- | ---: | --- | --- | --- |
| .2 | 81 | (900.1201,677.9982) | 1/1 | 0／False |
| .5 | 76 | (901.16754,677.9949) | 1/1 | 0／False |
| .8 | 72 | (900.37427,677.9977) | 1/1 | 0／False |

三个query首触预测X841.5973／841.62787／842.2816均为partial，低于full-left875.08；地面预览22／22／20步至900.0301／902.8734／899.2443，不能与上表实际最终位置混报。实际同名支持／12连续稳定／15%／恢复0之后才取得GREEN。完整post独立重复三原路线，无额外midflight控制前置；11 mandatory／33原出生及四optional pass/skip/returnable均过。

post唯一ERROR为`AssertTempleArenaLocksOnlyAfterTrigger:5088`的`!arena.IsEncounterActive && entry.Disabled`；raw未分别打印两值，不拆成两个实测结果。只读源码发现此fixture仍加载V2 LostTemple（RequiresFragmentAdmission=true），却没有BindRuntime进度；null progress按BossAdmissionModel fail-closed，不能把正例缺准入称正式Main坏。原出生合法、不重叠trigger；下一正例须绑定纯内存6合法残片＋CourtyardSeal＋BellChain，保留准入门／原断言／实际跨触发，不关闭quota、不读用户存档。尚未实施该修正。

## 当前同冻结正常窗口

两案实际Windows/windowed/1280×720、time_scale1、physics60，无headless／fixed-fps／movie。Main从Title同Player **47596963338**，8 activation/8 defeat、恢复0/0；深井4.633秒。Boss致命实际attack70/11500，damage/defeated11504，Died及arena defeat各1。同Player在physics11683有支撑越过出口，completion/event/count各1；held-D60/60至11744仍在(7340.7603,1079.9941)。冻结时velocity保留(260,0)，不称v0；隔离save hash保持，OS写入次数unavailable。

Main资格是`AUTOMATED_REALTIME_KEYBOARD_MAIN_COMPLETE_NOT_HUMAN`，不是用户键盘、最终视觉、稳定60FPS或完整1–11。三种远程Scatter/Burst/Laser均有真实warning/shot/伤害/反击击败收据；不能当新50正式接入。

Explore真实Valve cast5／关闭Jet无位移、下落22/100＋9/90逐步足迹安全、蒸汽战斗J10／thread4且HP1→1、Diverter及RejoinStep1–8均过。Top入口physics6669位置(5009.1777,3563.925)、v(170,0)、OnFloor，但缓存支撑为Step8而非Top；原普通分支6670使jump16→17，6673近中心松D，6708完成步到(5124.293,3561.2158)/v(99.55214,581.6668)，没有floor接触；6709首次恢复请求1/0。pre-recovery为 **FALL RESET／HP1／landings34／fall_height22.953125／fall_damage0**。这是坠落恢复，不是已观测HP0死亡；接触事件不等于伤害，不能将较早Archive/Frozen伤害归作直接原因。Top trace14/64、40samples、omitted0/fault0。下一诊断／修正限定原300内的同高地面转移，最终同名支撑条件仍保留；尚未实现。

Godot两案start/end focused=True、focus_loss_count0。OS sidecar仅ready/finally两点，均foregroundPID22460且own=False，无进程名或连续历史；不能推全程OS前台或操作者／失焦因果。Main callback p95/p99 25.229/34.695ms，Explore29.043/42.318ms；这是带自动操作和取证开销的callback wall-clock，不是显示/GPU FPS。Explore无成功route artifact／qualification，不授予完成资格。本次窗口已全部关闭并向用户释放前台；后续窗口需另行安排。

## 实际保留与剩余合同

最终focused、五回归、两normal同source1695（含296 imported）／同六binary。actual selected每侧：focused166=161source+5helper、forecast166、keyboard/actions154、pre181=175+6、post172=167+5、normal各227=214+13。四对库存＋continuation、全部实际副本及六binary逐项匹配；normal另有保护109／四byte pins／SDK Junction及双检查。Main/Explore helper17的文本hash不同，仅行序差，path/hash集合相同。前四开发失败各自库存/原件保留，不能拿最终source回填。

最终精确SHA256：

```text
source   010F8FE72EE426597ABA2312EDCE267F2BFCDEBDCB627EFBBEF74C9609718795
binary   C6D574B997D3AB75CF606D7F662195A3D386A5639081A4C49CA14DAE3CD264F1
DLL      7D220B7799972512648EF3A343D2AA10679BCEFB2F5DA226AB0CA9A40EDC15A5
PDB      59F349F18F7021B3419D29104C80017D81E1F261FCD6BDF9B5504D79A4E73A33
Traversal797C153AB4A27E2D0DDD238C0F1C7FF68F51B74B2623833F826D0F79FF54A635
Player   2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5
```

日志：focused7/0/190/0/190、forecast7/0/4/0/4、keyboard7/0/7/0/7、actions7/0/10/0/10、pre7/0/335/0/335、post7/0/304/17/321、Main7/0/2356/0/2356、Explore7/0/677/41/718（build stdout/stderr、engine stdout/stderr、external）。external完整匹配stdout/stderr合流；post/Explore各一行为ERROR，无新Handle/FATAL/ObjectDB/API ERROR或WARNING。既有finalizer/EXIT_DRAIN未改，quiet不关闭历史资源故障。

本次所有owned消费者终态、CIM无owned后记录。E余4768296960字节约4.44GiB；未来新增证据迁G询问未答，不迁移或删除旧证据。无新图稿／录像／音频文件、付费、Git写入、ZIP、发布或自动化恢复；媒体归档仍是`E:/OurGame/Knight/过程图稿与视频`。Task6/7、其余11文件迁移／五历史失败／资源／同冻正常双路／真人／性能及全部正式内容交付继续OPEN。
