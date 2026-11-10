# v170 · 幽灵收招反击验证（非通关交付）

## 目前已证明

- 用户已确认 v169 隔离标题可见。该窗口已结束：日志有 AudioDrain 与 `ISOLATED_TITLE_END result=0`，进程已不存在；没有保留 Windows 原生退出码，不将日志结果冒充该退出码。只确认标题显示，不代表玩法或画面认可；本轮不再打开窗口。
- 真实保存的 lost-echo 与 Player 在平移隔离场景中，L 防守后用三次独立 J 命中同一 Hurtbox，实际 HP **3→2→1→0**，玩家 **5→5**，309/600 物理帧，额外 K/跳跃/短喷均为零。
- Counter 是测试路线输入许可，不是生产机制修改。它约束同 actor、同 attack、当前同源 Recovery 出版、真实余时、落地、无活盾/机械爪、能量和候选命中范围；真正命中仍由原物理与 Health 决定。
- 原无盾受伤、举盾接触、预警锁向/绕背、隐藏转换及 stale/失效控制保留。修正审查预算问题后的最新专项严格通过：真实J尾段在120/120帧拒绝下一次等待；随后全新保存场景三次实际J、敌HP3→0/玩家5→5，310/600帧。

## 不能外推的结论

这不是实际 Grip8 混战，也不是 Explore 已通关。原 cast28 被另一实体 Hurtbox 遮挡，不能忽略碰撞体或靠相邻 ID 猜测父身份。新版本只在原 Grip8 返回后加一次同步、无输入/无 await 的已挂载 Actors/尾弹清单，原实际 locked-ray 增加父 ID/path/scene；这些诊断**不授权反击**。原 Grip8/9 各 300 帧和整路 600 秒不增加。

待补：实际阻挡父身份、foreign 危险完整覆盖、合法 Grip8Combat 接入，再走原 Grip9→Boss→实体出口。Task2 保持 OPEN，绝不以本组件代替整路。

## 本轮案例

路径均在 `G:/knight/验证证据/october13-stable-claw/`。

| 案例后缀 | build/native 毫秒 | 结果与边界 |
|---|---:|---|
| stationary-guard `071341…ad4b851e` | 36966 / 15963 | 行为 RED：真实 Recovery 缺 Counter API，native1；不是编译错误 |
| stationary-guard `071607…7e7044a8` | 32882 / 18995 | 严格 GREEN；真实三次命中，仍为 fixture_only |
| exploration diagnostic `073235…2d5134d0` | 未启动 | 命令遗漏当前 SourceRows，默认1604与1865不符，预检拒绝；不算游戏失败/通过 |
| exploration diagnostic `073306…f374b7bb` | 41247 / 250184 | native1，原Grip9 cast28 Obstructed；父身份与27 actor清单已得到，仍严格FAIL |
| stationary-guard `074021…90772bf4` | 91497 / 17183 | native -1073741795，进入新Counter前资源终结器FATAL；不是行为RED |
| stationary-guard `074316…e07db122` | 43719 / 19566 | 唯一有界重跑；真实嵌套deadline反例RED，旧实现在deadline后仍继续309帧才被新断言拒绝 |
| stationary-guard `074851…5b9e742f` | 79524 / 21855 | 修正后严格GREEN；120帧真实拒绝＋独立3J/310帧，原控制保留 |

前两案只读保管：10 对前后清单、928 选定源实体、20 helper、12 bin 逐件 SHA 匹配，案内零差；四个自有 build/native PID 已结束。旧1864案例不冒称覆盖新增诊断后的1865源。

新 trace：5 对清单、560 选定源实体、10 helper、6 bin 逐件 SHA 匹配；原始三 sink 只有同一原路线断言，build/native 两 PID 已结束。后3个 stationary 案：15 对清单、1392 选定源实体、30 helper、18 bin 逐件 SHA 匹配，案内零差；6个自有 PID 已结束。均不以旧失败案反向认证当前源/bin。

## 实际身份与审查

新诊断在 physics9425 原 Grip8 完成后采得27个 actor、0 live projectile、1 active hitbox（全已挂载 sectors 的 Actors 后代范围，不是所有未来攻击的安全证明）。同一实例 `6304995218672` 的 `@CharacterBody2D@62`，scene/subtype=lost-echo，HP3、Active余时0.0066666603；其 Hurtbox `6305028773106` 在 physics9467–9472 被**原实际 locked-ray**命中，parent_id 与清单完全相同。目标 Grip9Grip ID `6264729899809`，不是该敌人。实际父身份缺口已补，不再用数值相邻猜测；stable slot仍标not_observed。

唯一 fresh 审查 C0/I1/M0：重要问题是组件外层600次循环不能限制嵌套等待的实际物理帧，旧309/600结果真实但预算回归可能误过。已以真实J尾段故障控制完成RED→GREEN：每次等待前后使用同一个实际deadline，最终断言实际耗时，canonical只接受1..600分子。另从当前消费代码提取真实正则执行701边界控制（0..700），仅1..600通过。无二审；完整suite仍待结论，不能将专项通过外推正式稳定性。

## 范围与下一步

原十一项全部 ACTIVE，完整 Main/Explore、资源终结器错误、正式图声/动作/UI、目标硬件及真人试玩仍 OPEN。本轮没有新美术/音频、付费生成、截图录像、系统输入、自动化、Git 写入、删除或真实存档修改。

按既有计划继续：据新实际父身份及来源清单设计完整 counter 窗口，成功才接入独立 Grip8Combat；所有原始失败保留。当前模型/前后段最终回归进行中，尚不报整项完成。

最终 models 案 `075113…4d55d353` build79076/native1661，严格通过（原机械爪模型两组，不代表全部十一项）；未借旧 post 结果合并当前同版资格。

最终 pre 案 `075246…04fa5e1e` build43326/native104939，native -1073741795，BasinSupply 返回后资源终结器 FATAL，严格 FAIL，非超时；不重跑筛绿、不归因于新 Counter。

pre stderr/external 还各有同一个 `Invalid prologue sector definitions` 异常：definition[0..2] 为 null、缺 Entrance/Descent/Basin、不是5个唯一sector定义。它与 FATAL 的因果尚未确定，不能忽略，也不能据此把保存资源永久损坏作为既定事实。post 只有该次 FATAL。

最终 post 案 `075526…584f5114` build34726/native85458，native -1073741795，同一形态的资源终结器 FATAL，Task8 Basin 样本后退出。原75/owner8未完整执行，stdout资格亦不符，严格 FAIL；没有删用例、增cap、换引擎或新增采集。

最终 stationary/models/pre/post **2 PASS / 2 FAIL**。全本轮10尝试（9次native、1次SourceRows命令预检拒绝）为3 PASS / 7 FAIL，含2个行为RED、3个FATAL、1个原Grip9失败；失败不隐藏。Task1全suite、Task2真实清敌/整路及原十一项均未完成。

HEAD/index仍为原基线，无Git写入。无自有build/native继续运行，外部未知8760/bridge和另一UE工程进程均未操作。只做一次工作内媒体容量检查，7427750 bytes/86 files，Notify=false；无自动化。

最终3案保管15对清单、1440选定源实体、34helper、18bin全部SHA一致；6自有PID结束。全本轮9个已启动案例合计45对清单、4320选定源实体、94helper、54bin核对一致；预检拒绝案没有build/native，不虚构其保管对。当前重新安全枚举1865源/六bin/10helper与最后失败post完全一致；这是字节绑定，不是合格版资格。Task1全suite及Task2仍OPEN，计划workspace保留，未提交或删除。
