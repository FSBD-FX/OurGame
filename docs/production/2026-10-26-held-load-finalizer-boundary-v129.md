# v129 · 共享初始化后的加载／终结崩溃边界

2026-10-26，KNIGHT，`G:/knight/Knight`。v129是内部制作记录号，不是新发布版或GitHub tag。原十一项和完整高质量序章目标保持ACTIVE，不缩减为诊断任务；全部稳定ID、原76＋Boss/NPC、地图机关、正式视听UI相机、硬件真人和授权交付要求保留。

## 事实、推理、风险

- 事实：公平两组在相同初始化后，A只默认重载场景并保留，不继续实例化；B用同一场景继续原工厂／配置比较／Free。两组都在旧Settings终结时实际FATAL，A不是成功对照。正常不重载对照与普通前后段／模型通过。
- 推理：重新加载**之后**的工厂延续不是这个观察窗口的必要条件。共享初始化Prime本来就实例化过角色，不能说“完全不需要实例化”。Load已经返回，也不能说Load调用当场崩溃或本机原生所有权根因已证。
- 风险：没有修复生产资源问题；不能用普通回归安静关闭间歇FATAL，也不把字面量正例当真实held完成。用户最后NOTSEEN继续有效，没有重开演示或新画面。正式左握持片只核清旧批准链，未实装。

## 实验与实际结果

保留旧none/reuse/ignore/deep四模式方法原文，新增两个exact phase编码load/factory，不添可混配的arm参数。仅REF、headless、无其他诊断／付费／waiver。复用旧Prime、双根drop和5秒gate；两边都一次默认GD.Load，将同一PackedScene保留到旧finalizer drain之后，才清carrier。A新增部分只用托管hash及NativeInstance字段观察，不读子资源、State、ObjectID、IsValid或WeakReference.Target。B的原工厂自身验证、30值、Free属于完整B，不冒称只测Instantiate或引擎内部零工作。

RELEASE_BEGIN在Set之前flush，允许原终结线程在RELEASED之前运行；外层确保释放gate并保留操作／日志异常，外层finally清carrier。内层Free双托管异常仍有Minor。严格consumer要求完整11阶段、稳定载体身份、原serial的同call ENTER→RETURN／RETURN指针清零，且落在释放前标记与held-drain返回之间；超时、缺行、FATAL均FAIL。

TDD：consumer stub真实纯RED，59字面量／准入GREEN；中间发现PowerShell自动$Matches被-notmatch覆盖，改独立$heldRows。新native入口stub真实RED510ms后才实现runner。后来强化实际ParamBlock／常量绑定／函数／topguard安全AST和实际build参数检查，默认81检查，加入两实际FATAL拒绝共83；不执行完整launcher冒充纯测试。

| 案例 | strict | native exit/ms |
|---|---|---:|
| [135345 新入口RED](../../../验证证据/october13-stable-claw/resident-cache-held-load-refcounted-identity-20261026-135345-942-6ff20429fa1f4eceaa8048047abcdc08/engine.wait.json) | FAIL | 1/510 |
| [135605 held A](../../../验证证据/october13-stable-claw/resident-cache-held-load-refcounted-identity-20261026-135605-930-a4406d2492c44056a5368e3f00d74db1/engine.wait.json) | FAIL | -1073741795/6539 |
| [135641 held B](../../../验证证据/october13-stable-claw/resident-cache-held-factory-refcounted-identity-20261026-135641-732-2562a8455ca4458fa8394672d58d1f58/engine.wait.json) | FAIL | -1073741795/6048 |
| [135900 普通pre](../../../验证证据/october13-stable-claw/prologue-pre-20261026-135900-320-b425c725fe81437484d55e2a8ef1b5a4/engine.wait.json) | PASS | 0/158121 |
| [140207 普通post](../../../验证证据/october13-stable-claw/prologue-post-20261026-140207-677-3af1d860b5534a28a3adacd557e46921/engine.wait.json) | PASS | 0/201731 |
| [140558 普通models](../../../验证证据/october13-stable-claw/models-20261026-140558-429-f9f1e28ce29e442c8f3fbc9671929371/engine.wait.json) | PASS | 0/506 |
| [140744 不重载对照](../../../验证证据/october13-stable-claw/resident-cache-none-refcounted-identity-20261026-140744-161-d348e00beeae434184792cb7d9895845/engine.wait.json) | PASS | 0/593 |
| [140812 普通models恢复](../../../验证证据/october13-stable-claw/models-20261026-140812-376-a7fb47cc1ef44f9cb894f04dc21e693d/engine.wait.json) | PASS | 0/379 |

8原生5PASS3FAIL，8build均0warning/error、无超时、guards0、ExpectedFailure空。另一次NONE启动因PowerShell if表达式把单flag数组拆成标量字符串，再被字符splat，未正确传REF，**运行前预检失败，无case/build/native**；显式switch重跑才得到140744结果，不把它记行为RED或第九native。

A/B均只有8行到RELEASED，GateTimedOut=false；载体hash/指针自LOADED保持到RELEASED。A Outcome=load-only；B=null-rejected。两组均旧SettingsSerial1 ENTER1/RETURN0，随后gchandle FATAL1，无HELD_DRAIN_RETURN、ROOT_DROPPED或COMPLETE，不补造，也不以managed finally声称原生abort后清理完成。无后续factory必要性的推理以当前一次窗口为限，不给故障概率。

普通post原75 enter/return索引0–74、owner8 nodes1→1、PHASE_OK/TESTS_OK各1、实验CONTROL0；仅post有既有4条warning，三个scope multiset精确匹配。新held正向native未完成；none严格consumer的21检查是真实正常对照，但不是held正例。Main/Explore本轮未重跑，v128已记录失败继续OPEN。

十二个无必填参数纯脚本实际8PASS/4FAIL；失败仍为test-exit-verbose-admission.ps1（prefix命令白名单）、test-full-post-budget-v098.ps1（top-level effects位置）、test-level-cache-admission-v001.ps1（变体混flag）、test-sample-boundary-admission-v076.ps1（46/48）。新canonical前缀有意增加安全函数，不能再声称与v126旧前缀逐字节相同，也不把这四失败覆盖成全绿。

## 官方依据与不采用的绕行

只读检索：agent-reach Exa＋Godot官方记录／源码；GitHub CLI不可用，不安装。Exa首次函数式调用参数解析失败，按本机help使用命名参数后成功。

官方仓库仍保留已确认的同类资源缓存／终结崩溃问题；维护者讨论指向弱句柄失效后的缓存重引用，并指出简单存活检查到增引用之间仍存在竞态，部分外部补丁也受质疑。这与本轮结果一致，但不是本机原生因果链或可直接使用的补丁证明。[Godot #83762](https://github.com/godotengine/godot/issues/83762)

另一个4.7.2报告仍标needs testing，维护者要求最小复现，不能据“换新版本”推定修好。[Godot #124587](https://github.com/godotengine/godot/issues/124587) 按当前报告revision读取的[官方C#资源生命周期源码](https://github.com/godotengine/godot/blob/a13da4feb8d8aefc283c3763d33a2f170a18d541/modules/mono/csharp_script.cpp)提供假设方向，不等于已采到本机native owner或断点。

Ruling：不再盲改工厂或推广已失败的pin/Ignore/Deep方案；不改GC开关、关闭断言、吞日志或重写人口数。代价是根因修复未完成；下一先缩小共享初始化依赖，形成更小独立复现，必要的引擎下载／构建／调试仍须对应授权，不覆盖现用引擎。完整序章要求不因实验变小而变小。

## 正式素材的安全下一步

只读核清旧weaponHands批准链：left-grip-overlay原件SHA058DB41E707619C25D1416DD5EDA4BD6BC00F8407DE2EBDF18B373D8FA7A6EBE，三评审图同时符合阶段manifest和review-state批准记录；当前正常Player没有此绑定。原件及[批准清单](../../../美术源文件/迁移留存/2026-10-24_complete-layered-001_未发布/evidence/weaponHands-manifest.json)保留。

但刀／右片跟随NearArm，FarArm独立，没有次握点约束。静态坐标推导有2单位差异，只是代码几何推理，不是本轮实测失败。下一先在真实保存Player测次握点、完整攻击阶段／非零aim／释放过渡，再决定最小约束。此单件批准不扩大到身体／披风／肩部或被拒总装；本轮无图像编辑、付费生成或画面提升声明。

## 唯一审查及全部根裁决

[审查处置](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v129-final-review.md)：C0/I0/新M1，根维持Minor，无修复pass／复审，不宣称merge或whole-suite绿色。下表完整处理38条Declined，均保留范围与成本。

| 项 | 根裁决 | 错判成本／未完成边界 |
|---|---|---|
| 1 W／预判 | 未改，不重新认证 | 当前路线输入问题仍待 |
| 2 遮挡／receiver | 无新游戏查询证明 | 不授权绕过LOS |
| 3 Intro／Playing | 原准备门保留，不全认证 | 准备组合仍待 |
| 4 callback／deferred | detached实验不算执行证明 | 异步生命周期仍待 |
| 5 LoadLevel清理 | 未改未故障注入 | 双异常矩阵未全证 |
| 6 原十一项 | 全部ACTIVE | 不完整交付风险 |
| 7 Main | v128失败保留，本轮未跑 | 通关不可靠 |
| 8 Explore | v128失败保留，本轮未跑 | 后继路径仍待 |
| 9 新K效果 | 无运行证明 | 不声称战斗改善 |
| 10 none对照 | 根核593ms、typed pairs和21检查通过 | 只证不重载模式 |
| 11 pre | 根核158121ms严格PASS | 不覆盖历史间歇故障 |
| 12 post | 根核201731ms／原75+owner8 | 不是整关验收 |
| 13 models恢复 | 最终379ms普通Debug已核 | 不代替真人／性能 |
| 14 held-load正例 | 原生FATAL，无完整正例 | 只保留反证 |
| 15 held-factory正例 | null-rejected再FATAL | 不认证30值成功 |
| 16 native根因 | 仅局部触发边界与官方一致性 | 原生所有权链未知 |
| 17 生产修复 | 未实现／不推广绕行 | 资源故障仍在 |
| 18 引擎内部工作 | 只限制诊断直接调用 | 不能称内部零工作 |
| 19 abort清理 | 不声称finally执行完 | 崩溃无可靠托管清理 |
| 20 概率 | 每新arm一次，不估失败率 | 不推普遍确定性 |
| 21 正向consumer | 正例为字面量fixture | 没有新native正向资格 |
| 22 历史保管 | 只审本轮8案／基线 | 不全历史认证 |
| 23 旧缺12份 | 新none又缺2，共14OPEN | 不冒认完整副本 |
| 24 unwind flush | 旧Minor延期 | 早失败取证不足 |
| 25 swept措辞 | 旧Minor延期 | 易误读连续安全 |
| 26 finally stage4 | 旧Minor延期 | 终态摘要不足 |
| 27 LateFact来源 | 旧Minor延期 | 新鲜度证据债务 |
| 28 rotated-AABB | 旧负例延期 | 几何回归覆盖不足 |
| 29 withdrawal旧return | 旧Minor延期 | 撤回诊断不足 |
| 30 unknownActive scanner | 旧负例延期 | 真实扫描覆盖不足 |
| 31 shot上游变异 | 未修，新W测试不抵账 | 旧比较缺独立负例 |
| 32 左握持／FarArm | 来源闭合，未约束／实装 | 不能盲贴或当动作批准 |
| 33 正式视听／相机 | 本轮无新展示验收 | 美观与流畅度未完成 |
| 34 用户可见 | NOTSEEN权威，无新授权 | 后台事实不算看到 |
| 35 硬件／FPS | 不给结论 | headless耗时非FPS |
| 36 存档兼容 | 仅清单字节，不读真存档语义 | 不认证新兼容组合 |
| 37 本轮最终保管 | 根采用独立40对审计，明确缺2 | 不是零缺失 |
| 38 发布／全目标 | 不merge／打包／GitHub发布 | 所有最终门仍需完成 |

Final: minor (deferred): 新内层Free双托管故障掩盖原异常，根判定为诊断归因缺口，不是假通过或已证生产回归；原生abort不能用聚合异常修好。旧9类为energy-unwind早失败flush、boundary swept措辞、finally stage4摘要、fresh LateFact来源、rotated-AABB负例、withdrawal旧return诊断、unknownActive真实scanner负例、cache-helper实体缺失、v127 shot上游physics/input/capsule独立破坏负例。共10类，缺份类别现在14份，未静默修复。

其他实施Ruling／成本：两exact phase简化准入轴，代价是必须验证先于旧prefix分发，实际entry RED和安全AST已覆盖；两臂同carrier避免旧unheld-B不公平，代价是仍只测该保活条件，不证明生产pin；预检参数错误显式重跑，代价是增加一次无产物失败而非隐去；helper强化前后分开保管，代价是不能让历史案拥有未来测试；不追补缺2，代价是本案档案不完整，但不制造历史时间来源。只核批准左片而不盲改动作，代价是本轮没有正式视觉改善。

## 保管、容量与交付边界

[独立保管](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v129-custody.md)：所有root owners先终态，审计59c437 exit0／ENDED后才写本文。40对清单相同、3746实存实体／48sealed匹配，**新增helper缺2**，无其他extra/unmapped/hash fault；当前1848源／892C#／Debug6与最终models一致，entry外后7案14份source同版。只diag C#147+/0、canonical52+/3、新PS1；Main/Placement/production/assets和全部锁定值未变。

早期三held案例helper52A675F1209FFBBC1891E09424763A941E81B1F87B5EE1F54487651046A563CB符合当案清单；当前强化版A1C0B6C0E6F4B0A161E2EB98FB51FCF90413EE7D026780CA6B79276E3642081F不是该历史版本，不回填、不当漂移。十六份user manifest仅核字节，不读真存档。8engine＋8build精确身份均结束，owned CIM0。

唯一手动容量Check UTC06:11:51.1956180，Current1431609／Baseline3508／Growth1428101字节，16files、Notify=false。无新增媒体、自动化、通知、打包或删除；20GiB仍只随正常汇报提示不停工。没有重开游戏显示、系统输入、引擎安装／切换／调试器attach、付费生成或Git写入；原目标保持ACTIVE。
