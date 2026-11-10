# v255 · 正常gallery使用已有严格早预警候选

状态：驾驶策略已作有界修改，模型实际RED→GREEN；本轮首个Main通过旧摆锤回廊899阻塞，但**完整Main仍FAIL于后续Grip9机械爪阻挡**。本轮同源16项已结束，13 PASS / 3 FAIL；独立审查C0/I0/M0，不能作完整交付。唯一工程G:/knight/Knight，完整原十一项目标ACTIVE。

## 改动与安全边界

仅两个测试CS：新增7个独立scope事实的纯路由与21个literal正反例；正常键盘gallery的owned Skitter/batch1/locallyeligible/Recover＋Recovery/AttackId>0可进入已有live检查。其他正常敌族、Laser、非Recovery/非owned不获选；显式probe选择不丢失，也不直接授权攻击。

原完整live链保持：绑定/HP、实际bone-diver当前scene/攻击ID、有限全源clock、缺项/未知/重叠/来弹拒绝、完整攻击＋8tick/双攻击能量、盾未举、真实接地/速度≤261、slide0为Grip5、胶囊走廊两边≥30。外层和原盾释放后fresh检查继续走同一live链，要求同来源/同攻击/releaseWaits0；最终预算与真实J边沿保持。没有改生产能力、HP、能量、难度、原899、预算或地图。

旧probe-only适配行为在 ranged-models161727-518-8df4a3d9 编译成功后实际失败：唯一新偏差为normal owned Skitter Recovery应进入live却false。最小GREEN161949-700-fd42c81c严格通过，旧HP2收集器及原远程模型控制仍在。不是用编译错误作RED。

## 当前实际输入与通过的旧阻塞

Main162209-959-fd3e1b93/native157625ms/exit1：loop317 physics8029，同来源4910556910712/attack2/safe.5833334，原RECHECK waits0/fresh=True后发实际EARLY_J_COMMAND，目标HP2。旧HP1记录随后见同目标HP1，表示实际伤害而不是只发命令；旧安全链及原J流程被真正执行。

摆锤回廊在physics8426原group完成：11次请求/11开始/11连接、K3，玩家HP2→2且playerDamageEvents0。后续Skitter预警仍有真实guard请求/采样与清除，未把反击后防守关掉。当前case不再停于原899末批到达；这是一处真实验证进展，不是完整主线/正式交付通过。

## 新的后续失败：不得与之前受伤混写

Main继续到Grip9：cast25在physics8867/input8838被真实接纳一次，Windup/Travel时仍在原Grip8地面；physics8884/input8856变成Released、Failure/Reason=Obstructed、target空。角色仍HP1、floorTrue且原Grip8接触保持，所以至少“无失败／保原target”两条断言确实失败，不是新的HP丢失或失去地面。尚未独立打印最终jump/dash数，不据此推断其余条款。

之前physics8830/input8801的HP2→1另有接受伤害源凭据：split-level.02 lost-echo，actor4919012627658 / hitbox4919079736526 / attack1。Grip9开始和stance基线已HP1，不拿这个更早伤害冒称Grip9保HP断言原因。旧selected/danger上下文已老405tick，不能当作伤害来源解释。

当前没有原始LOS/stall/预测拉动碰撞的具体branch/collider身份收据；不能由Obstructed直接断定哪面墙，不能从lost-echo受伤推定因果。下一需原查询的受限身份记录，不能删除Obstructed条件或放宽原地面/单cast/HP约束。当前只读可行性核对另行保存，不在正常binary轮混诊断宏。

## 验收与资源

本轮16项终态如下，所有新失败保留，旧结果不代签当前同源资格。资源Handle/FATAL/RID无根因，完整76居民、正式美术连续动作、R-I/R-C/R-P、正常速度/目标硬件/真人仍OPEN。本轮没有新图稿/动作设计/付费/媒体、GUI/OS输入、额外dump、GC/cache/KeepAlive/引擎改动、自动化、Git/发布、真实存档实验或删除。

## 本轮最终同源回归（16项，不含预期RED）

同一正常二进制且不混身份宏：13 PASS / 3 FAIL。原单项时限未改，不挑绿重跑。ranged-models RED另案保留，不计本表。Main93875、14项批94785均终态exit1，GREEN20440终态exit0；无本轮owned引擎作业继续运行。

| 阶段 | 原生毫秒 | 严格结果 | 原始证据目录（G:/knight/验证证据/october13-stable-claw/） |
|---|---:|---|---|
| actions | 9644 | PASS | actions-20261105-165229-225-47de043b20d244b790013e53be9c9eb4 |
| canyon-structure-v2 | 1485 | PASS | canyon-structure-v2-20261105-164627-923-de88d984c60d4bd3a517f6181c59d1e2 |
| keyboard | 20210 | PASS | keyboard-20261105-165126-603-71a4bef226554f75bae7ff7c13c929b6 |
| models | 639 | PASS | models-20261105-165049-464-ca115514901c433784f5f5e1eff9d743 |
| prologue-post | 180060 | PASS | prologue-post-20261105-164103-345-4f20b004ce254b56a3e3663fff2e934d |
| prologue-pre | 140301 | FAIL | prologue-pre-20261105-163807-472-f287b69c935e45dea929a7306a7a91b1 |
| ranged-models | 548 | PASS | ranged-models-20261105-161949-700-fd42c81cc27640a8bf2c5bc3e1dca6df |
| recycling-traps | 25953 | PASS | recycling-traps-20261105-164528-009-bf408b2a36c341f5a31703f2fe11f73c |
| resident-allocation | 732 | PASS | resident-allocation-20261105-164935-536-9b17162482844c7eb12a4304ced75620 |
| resident-geometry | 30376 | PASS | resident-geometry-20261105-164704-680-30878b8a3ebb46acb2a1d26158431c4d |
| resident-patrol | 45702 | PASS | resident-patrol-20261105-164810-591-b77cb401e8ac45d2bf3b32f2cca1cc18 |
| resident-save-compatibility | 11363 | FAIL | resident-save-compatibility-20261105-164441-621-feffa5c35d4e4f708d026135143593a7 |
| resident-state | 650 | PASS | resident-state-20261105-165014-281-640432dcfc584093abe81d9bec914ec3 |
| thruster-incinerator-traversal | 34672 | PASS | thruster-incinerator-traversal-20261105-165315-236-0c1fe9355d3443e8800e396c50b39e89 |
| thruster-route-exploration-diagnostic | 316323 | PASS | thruster-route-exploration-diagnostic-20261105-163212-440-2aad325d7b6e4b1190f4f4a0048a559a |
| thruster-route-main-diagnostic | 157625 | FAIL | thruster-route-main-diagnostic-20261105-162209-959-fd3e1b9364f64b58ab894912182ab760 |

pre进程虽exit0且执行44案例，外部ERROR仍严格否决。stderr八个错误头在1/42/83/124/165/206/247/288行，完整41行栈逐字一致：GCHandle.FromIntPtr→SwapGCHandleForType1284→Node.AddChild848→BasinSupplyRuntimeTests.OpenSample525/CheckSupply241。external log144–473把八组夹在同一check lifecycle=true damage=3、physics1246的mount-enter/return之间；这仅证明观察区间，不是八个不同fixture，也不能由此判定生命周期原因。stdout615执行44、617–618 exit-drain/phase-OK仍不能冲销ERROR。

save进程11363ms在原生gchandle.is_released/Dispose/Finalize处退出（-1073741795），本轮未得到完整暖/冷存档资格，不能沿用v252的历史通过。两者归属仍未知；既有原生错误/泄漏欠账不因其他测试quiet关闭。

Explore实际stdout5028–5034为Boss attack99/HP1→0、Defeated/Died及arena_defeat各1、physics18140实体支撑且越过出口的Completed、再保持60physics/save bytes不变；5051恢复0/0、faultnone。task10-route-result.json为11个distinct encounter激活ID、10个defeat ID；差集仅counterweight-lift，是缺少一个遭遇完成凭据，不等于只剩一个敌人，更不是个人击杀总数。另4机关、2检查点。fullGoalAccepted=false；不得冒称76普通敌人/整图清场完成。

[audit-v255.json](../../.superpowers/sdd/2026-11-05-gallery-normal-early-counter-v255/audit-v255.json)实际核对80份前后保护对：source、helper、user、continuation、fresh/binaries各16，无差异；source/binary各只有一种。当前1897输入及完整文件集、6个运行二进制、15个宿主/工具的SHA256无差异。不是仅检查源码数量或使用旧构建冒充同源。

## 后续受限测量与尚未归因

机械爪只读核对：现有原查询identity已有LOS、原TestMove及状态门三个位置，普通Main当前未准入TraceClawObstruction。后续最小下一步是独立宿主TDD为exact Main增加trace准入（入口/构建两处一致），保留三旧正例、混合/错误旗标反控、未trace污染拒绝、Rebuild/冻结绑定及原失败语义；随后一次实际原branch/collider记录。PULL_HIT可能是允许的地面/目标接触，不能单凭它判定拒绝。未实施、未更改生产碰撞/路线/断言，不凭guess把Grip9改成跳跃。

资源只读核对：REF条件探针覆盖当前找到的19类自定义Resource，但只有11类有唯一serial，8类只有type/hash/thread/call/pointer，serial0无法加入serial-owner表；观察上限或日志故障也可能缺失。未观察的实例仍能进入Dispose override，Finalizer以false虚调用，不能把缺少owner等同于缺少终结器探针。没有证据指认某个类型。下一可以在已有Camera/CurrentClearance的owning try中读实际Profile/Motor.Tuning/Combat.Tuning并关联fixture/cycle/root标量；这是赋值字段快照，不是完整setter历史/所有资源归属。避免旧适配器的GC.Collect、SceneWalk、GetState；不新建强根、不改缓存/engine/Dispose节奏。此处只有只读设计，尚未实现或采集。

## 资产与用户授权记录

| 本轮资源 | 来源/保存 | 状态 |
|---|---|---|
| 纯路由及21控制 | 两个既有测试CS；before/diff/RED/GREEN本地保留 | 测试策略增量，不是美术/玩法新资产 |
| 身体与披风现稿 | 原有本地候选，V1批准原图暂缺 | U-20261105-01后续重新审阅；不转授旧批准 |
| 截图、实录、付费图、音频、模型 | 无新增 | 不把后台数字当视觉认可 |

独立审查C0/I0/M0，允许保留本次有界测试策略增量，不是完整Task1/整关通过；没有新增修补或二审。当前不merge/commit/push、不删除工作区；完整十一项与R-I/R-C/R-P、正常速度视听/目标硬件/用户认可继续OPEN/ACTIVE。

本轮仅一次正常汇报媒体Check：2026-11-05T09:03:48.2001530Z，7,427,750 bytes/86 files、Notify=false。没有达到新增20GiB档位，不建立监视器或自动化，也不因容量检查停工。

## 本轮全部裁决与代价（按作出顺序，13项）

1. 延续已确认的KNIGHT/Godot原地工作，不重问引擎/工作树；代价是未另选隔离方式，但本轮仅两测试CS且保before。
2. 用本地apply_patch/账本保原件，不提交Git或删除工作区；代价是手工记录/本地增长且没有发布检查点。
3. 只让正常owned Skitter Recovery进入原live链，不全局放行预警或扩899；代价是候选和完整Main仍可能失败，必须实测。
4. 保留新Grip9失败并把其余14阶段各跑一次，不削弱原断言或反复挑绿；代价是本轮Main资格仍不具备。
5. 独立审查只判断有界增量能否保留，不能凭局部通过记Task1完成；代价是完整验收/交付继续延期。
6. Grip9根因及与新反击时序的关系均保持未知，下一采原branch/collider；代价是可能的时序回归尚未排除。
7. 更早lost-echo受伤单独登记，不先定玩法合理或声称修好；代价是后续HP余量/体验问题可能持续。
8. pre/save资源错误保持严格FAIL/归属未知，不盲改Dispose/cache/引擎；代价是稳定性和存档资格欠缺。
9. 保持旧live链不等于认证所有敌族/物理状态；代价是未覆盖状态的风险仍在，不能整体背书。
10. 显式Laser只保静态/literal语义，本轮未新增其独立原生等价测试；代价是该专门运行覆盖仍缺。
11. Explore/gallery不代签Main的Boss/实体出口/恢复0；代价是正常Main完整验收继续OPEN。
12. 保留原十一项、正式美术连续动作、R-I/R-C/R-P、76居民与正常速度/硬件/真人门；代价是仍有大量制作，后台技术结果不等于精美成品。
13. 审查只覆盖before/v255差异，不延伸到全部脏树/历史版本可发布性；代价是整版集成仍需另验，当前不发布。

新增延期Minor：0。既有延期问题不关闭，详见原报告链；唯一审查/全部8条Declined对应裁决保存在[完整审查记录](../../.superpowers/sdd/2026-11-05-gallery-normal-early-counter-v255/final-review-v255.md)。

审查及文档更新后最终绑定复核：2026-11-05T09:11:41.6001302Z，实际16案例/80保护对/1897完整源集/6二进制/15宿主工具仍0差异，branch/HEAD保持；见[最终绑定](../../.superpowers/sdd/2026-11-05-gallery-normal-early-counter-v255/final-binding-v255.json)。没有为汇报再开引擎或重跑挑绿。当前目标继续ACTIVE，不标完成或blocked。
