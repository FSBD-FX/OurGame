# v240 · 下降教学的已接受伤害来源与重入保护

状态：本地测试诊断增量，最终同源码/六二进制清单四门**3严格PASS/1pre超时FAIL**；**不是下降掉血已修复，不是完整序章或美术/动作/物理手感验收**。原十一项及R-I/R-C/R-P继续ACTIVE/OPEN，唯一工程G:/knight/Knight。仅无窗口、隔离存储与原键盘事件；没有付费、前台/系统输入、额外崩溃采集、媒体、自动化、Git发布、删除或真实存档更改。

## 实际改动

仅`tests/integration/DescentClawGuidanceRuntimeTests.cs`，1888个输入文件数量不变，无生产Health/Motor/伤害/敌人/机关、数值、等待、输入或原HP5/恢复0门调整。

- 同一Player局部订阅Health.Damaged/Healed；事件当场读取Health已接受身份队尾，输出阶段、物理/process/input、HP、来源二元组、源路径/类型、位置/速度、盾与能量、爪、LandingCount有限标量；finally解绑和summary，最多各32事件，溢出/观察故障不冒充完整证据。
- SourceRuntimeId等于该Motor ID时才给MotorSource；其他来源保留实际ID/路径，不猜敌人或机关。没有读取尚未在同步回调内更新的LastFallDamageApplied。
- 读取实际Health接受队列，不用包含被盾拒绝尝试的Guarded队列；不修改该队列，不保留额外源节点/包装跨await，临时NodePath释放。
- 控制与真实Run共用事件reader，检查当前HP并拒绝重复消费同一队尾，避免同步嵌套回血/伤害把旧事件误记为新来源。

## 实际TDD与唯一审查

以下原始目录在`G:/knight/验证证据/october13-stable-claw/`，原失败不删除：

1. `descent-j-observation-control-20261104-135639-270-243ff273030449759e35a4d603d90e82`：build退出0/31554ms；engine退出1/638ms，missing accepted damage reader行为RED，非编译失败。
2. `descent-j-observation-control-20261104-135745-804-6ea1daab44ef47fc8495ebbf393fa9f2`：初版reader控制严格PASS/22934ms；实际65次接受跨64条容量、重复/零伤拒绝、治愈及reset，并继续原second-J/下降教学。
3. `descent-guidance-20261104-140009-871-77805533b98b424e977127072aae0626`：实际接线后focused严格PASS/18283ms，HP5/0恢复、0Damage/0Heal/0Omitted/Failure null，stderr空，没有复现v239伤害。

唯一新范围独立只读审查C0/I1/M0。I1成立：先注册listener在A受伤时治愈1，再用B扣1，内外HP均99；原HP-only检查会把A和B都记录为B。未证明当前Descent有这种调用链，不把它说成历史HP4原因。

4. `descent-j-observation-control-20261104-140733-928-308067c94e7544a5ba354b20ba60e4de`：新增真实Health重入控制，engine退出1/660ms，期望归因1次、实际2次，行为RED。
5. `descent-j-observation-control-20261104-140924-322-605141457aa14a92937627953b94bdf1`：共享reader拒绝重复消费队尾后严格PASS/18169ms、stderr空；2真实事件、仅B当前归因1次、外层A明确拒绝1次；原65控制、second-J与DescentHP5/0恢复通过。这是一次Important修补，没有二审，不代表任意重入都能恢复完整事件历史。

## 首次完整上下文

`prologue-pre-20261104-140201-343-a61e8133b8e04e5cae89fefa4913be8b`原180秒门超时（180146ms），未通过。已完成前12项，在第13项MechanicalClawRuntimeTests期间终止；不能把最后一条成功marker当全前段通过。

该次Descent已完成15028ms、HP5，0Damage/0Heal/0Omitted/Failure null。v239的(4415,4814] HP5→4没有重现，因此**目前没有实际伤害来源可以报告，也没有依据修改落伤、盾或敌人参数**。观察器增加了反射/标量读取/打印，可能扰动时序；不能用新通过抹去旧失败。原native超时和匿名Handle/终结器故障仍开放。

## 当前最终验证

修补后按原预算顺序执行pre/post/models，全部终态。以下不是扩大时限后的结果：

| 阶段 | 完整案例目录名 | 原生ms | 严格结果 |
| --- | --- | ---: | --- |
| 新来源/重入控制＋原下降/second-J | descent-j-observation-control-20261104-140924-322-605141457aa14a92937627953b94bdf1 | 18169 | PASS |
| 完整pre入口 | prologue-pre-20261104-141109-617-9cb6ff6fc4de43158d77eeba5f6cecb8 | 180099 | FAIL：原180秒超时 |
| 完整post入口 | prologue-post-20261104-141455-191-ed55f2a855464d99845f3ab98141afa0 | 220193 | PASS：原300秒门 |
| models：仅爪模型/到达几何子集 | models-20261104-141917-784-bc3405254146451bab6d1fdc759055e2 | 544 | PASS，非全游戏模型全集 |

四案均1888项source清单、六项fresh binary清单一致：source清单SHA256 `A4F39F9DDE7407231D5D6844BAA8CE5AF04937A153555F5C09CA7A120B6ED9B2`，binary清单SHA256 `F0FB2BCA102B4060781AD6C69D2720C462D468A932CA14CFB7E920B2722C3F5B`。这些是清单文件SHA，不冒充单程序集或发行包。每案source/helper/user/continuation前后均一致；终态后逐一重读当前1888文件也与最后清单一致，真实玩家文件未变，原生进程无残留。

最终pre完成43个原top-level案例，在第44项PrologueTraversalRouteTests中超时，没有phase通过资格。相机29468ms、劳动者58532ms、Descent15097ms；172249ms才进入最后一项，原门仅余7751ms。Descent同Player HP5、0恢复、0Damage/0Heal/0Omitted/Failure null，没有原HP4归因证据。相比首次pre耗时变化不是完整同输入/同调度A/B，不称本次观察修好了性能。

post四WARNING逐项均为原明确负例：throwing ClawFrameChanged observer、VisualScene非法根、两次Completed→Recovering拒绝，0ERROR；其他三案stderr为空，pre即使0ERROR也因超时严格FAIL。Main/Explore本轮未重跑，不能用v238旧探索给新构建续签；历史Handle/终结器、HP4和原十一项均不关闭。I1技术修补真实RED→GREEN且新控制/原下降通过，但宽套件仍红，Task1不签complete，不提交/合并/发布。

## 动作品质和后续工作

独立只读核对暂未发现新的可确证地面动作缺陷；v227摆脚反向/再次起步已修，不重复报旧问题。当前真实Motor的两条ground-gait/brake入口主要是D→松键停，实际过零转身→停止→再起步的整个Motor/GroundMotion/Brake/Facing/Gait/Draw链仍缺连续证明。后续沿保存Player与平地补真实键盘样段，失败才据证修，不随意调惯性、姿态或已批准资源；正常速度优雅、特色和手感仍待授权后的实际审阅。

性能方向仍以v239实际边界为依据：先量测同次居民admission端点重复检查成本，再考虑仅局部纯值复用；不跨挂载前后fresh边界，不删人口/通道检查或扩时限求绿。完整Main最后一击、当前双路、资源稳定、全图美术/UI音频与所有原十一项继续，而非将目标缩为本观察器。

## 执行取舍与范围

- 复用测试已有私有Health队列读取先例，不扩生产API；私有字段改名会明确观察失败，这是维护成本。
- 审查只覆盖指定新增块，不覆盖整个dirty worktree或所有安装/日志故障路径；finally结论限于进入原try的路径。原生命周期/完整门保留，不声称任意异常都已修复。
- 所有native由root串行、源/harness冻结，原分支原地保留、不复制worktree或提交；完整目标仍进行中。
- 无窗口证据不能签视觉、操控、性能或真人；新观察自身有开销，历史HP4仍未归因，不以新记录取代玩家可感知的后续制作。

[执行账](../../.superpowers/sdd/2026-11-04-descent-accepted-damage/progress.md) · [原十一项](2026-10-18-project-remaining-backlog-v001.md) · [正式动作返工](2026-11-03-character-motion-rework-requirements-v001.md)
