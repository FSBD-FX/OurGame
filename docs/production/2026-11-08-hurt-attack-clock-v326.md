# v326 · 受伤期间的攻击表现时钟

## 事实、推理与风险

继续原V03/V04/R-C动作返工，不是新设定或战斗数值重做。保存Player的Combat在普通非致死受伤中继续推进攻击；表现Timeline先返回Hurt，原代码却不推进被遮盖攻击年龄，导致Hurt结束后补播已经失效的Burst/Contact。旧“同一攻击接续”只约束身份，不等于批准冻结旧攻击的年龄。

本轮先用保存Player、实体地面和真实内部W/J取得反例，而不是只凭源码猜改。实际攻击窗关闭后超过两物理步仍出现发力，才修表现计时；HP、伤害、Hitbox、根运动、能量、控制、素材、引擎与GC均没有改。

风险边界：自然Process/Physics节奏不完全同相，必须先检查累计时钟偏差，不能把渲染欠时误判为动画错误。这里观察的是生产Hitbox.Monitoring激活开关，不是碰撞服务器接触矩阵。无窗口几何/时钟检查也不是正常速度的流畅度、设计感、像素或真人审美验收。

## 实际 RED → GREEN

证据根 `G:/knight/验证证据/october13-stable-claw/`，原失败不删除。

| 案例 | 实际结果 |
|---|---|
| `thruster-rig-compatibility-20261108-172440-213-bd4577715aab4e129dcd9ad8b0259007` | 功能RED；两朝向无Hurt stale0，真实Hurt各stale15，首次过期发力在攻击+14tick。四案原有效窗均开+3/闭+9/完成+17，时钟偏差≤.015516s、发布间隔1tick，先满足时钟/伤害/回稳门再报功能失败。纯30Hz也在本应Recovery时返回Burst。 |
| `thruster-rig-compatibility-20261108-172642-157-a3d284ab4696477abb60f0738fd849a5` | 最小修复后严格PASS；两受伤案stale15→0，实际窗口仍+3/+9/+17，无额外攻击或有效窗。四原生案及20纯计时案通过。 |
| `thruster-rig-compatibility-20261108-172831-592-fc07afc49b3942cb975d052280fbb6f2` | 收紧测试后冻结版严格PASS，0/16446ms、stderr空；四案stale0，时钟最大偏差.015646s、间隔1tick。累积偏差检查包含零时差发布，短攻击在Hurt结束时立即为None，而非靠追加清理帧掩盖。 |

最小生产改动仅在`PlayerPresentationTimeline.ResolvePrimaryAction`：Hurt遮盖期间，已有且未取消的攻击按同一delta和原命中停顿前进；Hurt结束的同一步不会再次计时。仍显示Hurt优先，同ID按实际年龄回到Recovery；不暂停/重启/取消真实Combat，也不扩大修改到机械爪或旧丝线优先级。

纯控制覆盖30/60/120Hz×地面Up/空中横斩/下劈×有无Outgoing停顿18案，另有短攻击过期/freshID及自定义较长有效窗，共20案。明确保留原`.025s`停顿的整样本量化：30/60Hz扣.033333s、120Hz扣.025s；没有静默重做停顿算法。零/负/非有限delta在Hurt内外不推进，恢复仍可取消，较长真实有效窗允许受伤后合法接续Contact，不能粗暴取消所有接续。

原rigcompat11个formal委托、2个mantle委托、guard、v325真实W/J均保留，新探针仅追加。FramePublished只收集标量，观察失败计数单独核对，避免诊断订阅者异常被吞而假绿；priority100在实际Combat之后采样。

## 当前冻结与待验

当前1914输入清单SHA256 `64F78888924B185821C24BE13E1A942C3A7A039603443E41BA2D89465A8C4DFA`；六产物清单`2F7C26CD91D0A8C0FC545B941FFD014AE19F6FED40F65FDAFA5EE78706717A3C`，DLL`DCF9D4CA3E7C8DED5B438E3501E70DF54C17480AC88028640A324880833DCADC`，PDB`49DA2010B0478FF662AA66955C177A2289B024B095572E1EE890F56C13C0A9E0`。冻结后11个自动回归阶段10PASS/1FAIL，不能借v325的前段通过来拼接成当前整版通过。

没有新图稿、录制、付费生成、窗口、OS输入、原生dump、GC/cache实验、真实存档写入、自动化、Git写入或删除。历史资源Handle/终结器故障本轮前段再现，根因仍OPEN；其他单独阶段通过也不等于修好。原完整十一项目标、正式角色/敌人族系外观与连续动作语言、正常声画/硬件/真人/交付仍OPEN。未获回复的免费敌人美术路线和隔离GC实验均未默许执行。

### 完整前段本版失败，未筛绿

`prologue-pre-20261108-173354-787-1579bdbd56cd4d7494fbd539c50510cf`：engine退出`-1073741795`，14879ms、非超时；FATAL `gchandle.is_released()`，Dispose/Finalize/GC.RunFinalizers堆栈。最后日志是Camera夹具CanyonAscent的mount-return（engine8716ms），不能据此认定该挂载就是坏对象，也不能说原44项完整返回。属于同名历史错误再现，不证明本地同一根因，整版当前严格未通过。本轮新测试只追加在独立rigcompat入口；不凭通过动作专项掩盖这条失败，不调GC/cache或擅换引擎，不以重跑挑绿。后续独立阶段仍按原顺序单次执行。

## 最终同版11个自动回归阶段

均为上述同1914输入/六产物。前段只进入第0个case、返回0，不写为44项完成；后段75项、无退休，按普通300000ms cap运行，不是600000ms诊断或旧180秒性能认证。

| phase | engine ms | 严格结果 | case |
|---|---:|---|---|
| rig compatibility | 16446 | PASS | `thruster-rig-compatibility-20261108-172831-592-fc07afc49b3942cb975d052280fbb6f2` |
| presentation rig | 18481 | PASS | `thruster-presentation-rig-20261108-172959-326-4067df7cf61e471ba1094189890e2029` |
| ground gait | 29788 | PASS | `thruster-ground-gait-20261108-173049-043-85529c53408444f2935e1fb08eba434b` |
| recovery compatibility | 15048 | PASS | `thruster-recovery-compatibility-20261108-173153-908-8e2a382265b04f33b7938563328fda3f` |
| actions | 10776 | PASS | `actions-20261108-173239-602-56b83ee5732e4c8c88d4f3e769b88fef` |
| models | 503 | PASS | `models-20261108-173323-284-1aa86f6d02574ffe915668ff1a7aa9e5` |
| prologue-pre | 14879 | FAIL/FATAL | `prologue-pre-20261108-173354-787-1579bdbd56cd4d7494fbd539c50510cf` |
| prologue-post | 190557 | PASS | `prologue-post-20261108-173441-285-29bfc0b1b76c45a0acf8b7ce314e9bec` |
| headless Main | 251178 | PASS | `thruster-route-main-diagnostic-20261108-173824-131-f45ac7bc5dc14c879fb977c4a3c05f24` |
| headless Explore | 316779 | PASS | `thruster-route-exploration-diagnostic-20261108-174311-743-56bbcca4fe5c4e02ab7c92775abe6f98` |
| resident save compatibility | 11749 | PASS | `resident-save-compatibility-20261108-174901-016-6c600faceab44a3c85a05e1260cfc3d3` |

post只有原合同限定四条负例WARNING（观察者故障1、VisualScene根类型1、Completed→Recovering拒绝2），ERROR0；pre是独立FATAL，不能用post通过冲销。批次外层退出1，唯一失败阶段为prologue-pre；没有重跑筛绿，全部原件保留。独立最终快照审计见收尾段。

## 独立增量审查与继续边界

一次全新上下文只读审查`hurt_clock_fresh_review_v326`：精确v325前件与本次3个C#文件（1新增）＋canonical脚本增量、原始RED/冻结GREEN均检查，C0/I0/M0。没有第二审或额外修补；只是该时钟修复可接受，**不是整版可交付**。没有本轮新增Minor；v325暂停内部关节覆盖M325-1及其他历史延期并不因此消失。

审查明确未认证，根代理逐条保留以下边界及风险：

- 正常速度声画/硬件/真人仍OPEN；几何正确仍可能不好看、不好用。
- 正式角色/敌人美术及原十一项仍OPEN；计时修复不替代缺少的正式资源。
- 当前FATAL及历史Handle的对象级根因仍未知；不判与本改动无关，发布仍有崩溃风险。
- 最终路线/存档与所有快照由实际批次和独立审计完成；早期代码审查不能替代尚未结束的运行。
- 新探针仅证明Monitoring激活开关；不扩大为碰撞服务器瞬时接触/CCD认证。
- 空中/outgoing命中/Hurt的完整原生组合未全跑；纯Timeline控制不冒充这些场景实测。
- 爪/旧丝线优先级未重做；其他接管造成的过期表现须独立实测，不视为已批准永久冻结。

下一轮只读候选：普通键盘先J再下一拍K可被接受（同拍J+K会被正确仲裁，不混用），Claw不取消Combat，Claw→Idle只零时间刷新，可能让旧攻击补播。已确认源码可达，尚未native，当前不改。独立Thread输入在生产Coordinator中固定default，不能误报为同等实际路径。

当前Main已严格PASS（251178ms，case见表）：从标题同actor经Boss至实体支撑出口Completed，恢复0/0，8激活/8击败，21普通跳全部grounded，coyote/buffered/assisted接受0；unsupported输入边缘2，不写成完全无离地输入。held60位置/实体矩形/存档不变，物理禁用但velocity保留(260,0)，不是零速度。实际`storage/save.json`与terminal/held同SHA `0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517`。

Explore也严格PASS（316779ms）：从标题同actor到Boss/支撑出口Completed，恢复0/0；11激活/10击败，counterweight-lift未击败，不能说全场清怪。33普通跳全部grounded、coyote/buffered/assisted接受0，unsupported输入边缘8。完成事件/次数各1，held60位置/实体矩形/save不变、velocity仍保留260。实际save与terminal/held同SHA `24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24`。两个独立进程恰巧都分配player ID48049948164，不据此声称跨进程是同一个活对象；各自进程内身份连续才是断言。

保存兼容阶段严格PASS，另有独立coldreader0/1594ms，六witness与只读合同；不把原回执明确否认的actual_old127_to143_continue写成做过旧工程完整Continue。真实用户文件不参与写入，隔离storage才是fixture写档位置。

## 最终收尾审计与目标状态

独立审计逐案一次读取，11案共**2863对**封存before/after输入文件、差异0；**66/66份二进制复制**实际hash一致（6文件×11案，不是66个不同内容hash）。各案1914输入、六产物、109真实用户文件及各自helper清单前后不变。冷读writer21304/reader11616，native记录确认不同进程；6witness，source_read_only/read_only为真。

最后重新枚举当前真实文件，而非只比清单：rg原recipe＋`.godot/imported`共1914项/rg退出0，与lastcase source-after路径/hash差异0；当前六产物6/6相同；真用户目录重新递归hash109项、差异0。当前关键文件：

| 文件 | SHA256 |
|---|---|
| PlayerPresentationCoordinator.cs | `80FC737ABE15395C5B12ABDE198F9DEDB5AAA1046DCDF8F7DF96767715A54E0A` |
| PlayerHurtAttackClockTests.cs | `E31166ABA6D2BD7CB47AC520620AC6A09816F4198E28EDB88D4B38A91C4E8B18` |
| ThrusterPresentationRuntimeTests.cs | `2EA5F8665E7A9C75C648C31080A0D1B1082BF9B637B8D9FFB31DA1E7DB0628B1` |
| canonical verify-claw-knight-v001.ps1 | `BDDAD62B3C012601B92DC199673F8BB7647714CEFCF50AFCD153CF328E9D33B7` |

媒体仅在正常汇报前一次`-Check`：2026-11-08T09:50:37Z，15,203,531bytes/145files，Notify=false，无新增媒体或20GiB跨档，不建立自动化/Watcher。当前项目Godot/dotnet运行数0，不触碰其他godot-ai attach。

执行裁定：只修原批准动作可读性的隐藏年龄，不改Hurt战斗规则（若判断错会让表现错时，已用真实窗口和控制防护）；在原脏树root独占修改/构建，保留用户改动、失败证据、不做Git或删除（没有独立新worktree，靠冻结清单和副本审计避免混版）。一次代码审查与末段冻结回归并行节约等待，但审查不替代后续运行；已由独立最终清单与回执审计补足这项风险。其余逐条验收边界见上节。

原完整十一项目标保持ACTIVE，不完整/不缩减/不伪称稳定。该计时修复已实际RED→GREEN且代码审查无新增问题；本小计划的整版严格资格仍因前段FATAL未通过，不能标Task1全完成。下一步先对可达J→K接管候选做原生受控验证；资源原因继续仅现有授权范围内排查，GC实验仍待单独明确授权，正常声画/正式美术与真人验收也仍欠。
