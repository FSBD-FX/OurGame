# v327 · 抓取接管后不再补播过期挥刀

## 事实、判断与风险

继续原V03/V04/R-C动作可读性，不是新玩法。真实独立键盘允许“J已接受→松J→后续另一拍K”，与被仲裁的同拍J+K不同；Claw接管表现但不暂停Combat，原Claw早返回却冻结旧攻击表现年龄，松爪后可能补播失效挥刀。先用保存Player实际实体地形复现，再修改，未改变伤害、命中盒、根运动、目标优先级、抓取模型、资源或数值。

采用旧StableClawPull真实天花板夹具：Player(400,400)、floor top436、天花板StaticBody(400,180)/260×20，子节点GrappleSurfaceProfile/GripStone/UseCollisionHitPoint；自然A/D确定朝向后W/J，下一采样K，抓住真实下表面，保持至Combat结束，再新K取消。不是冻结Motor/反射注入状态/专用虚拟锚点，也不向系统发送按键。

自然FramePublished与priority100的实际Combat后观察相互独立：先准入60Hz、累计时差≤2/60+1e-6及正时间发布间隔≤2tick，再检查真实窗口、唯一攻击/抓取/目标、取消/Idle、HP/普通跳/最终回握，最后才断言闭窗后超过2tick不能出现过期Burst/Contact。Monitoring仅是生产激活开关，不冒充碰撞服务器矩阵或正常速度观感验收。

## 实际失败及修补

证据根：`G:/knight/验证证据/october13-stable-claw/`，全部失败保留。

| case | 结果 |
|---|---|
| `thruster-rig-compatibility-20261108-180517-106-626e7631ab7c4b4198ce755ced5f4978` | engine1/28237ms。真实抓取两朝向均stale15，无K stale0；但新控制错误要求没有任何Claw回调。Idle也正常发布，故该轮记录为测试前提失败，不算完整匹配RED。只改控制为全部Idle/无cast/无release/无overlap/四期，不动生产。 |
| `thruster-rig-compatibility-20261108-180730-931-0e3077233c764a09a9d588d8a4e45f28` | 有效功能RED。四案所有时钟/窗口/路径/终态先通过，只有真实K的过期发力15/15及纯Timeline Aim30Hz被拒绝；无K控制0/0、四期、124次合法Idle发布。 |
| `thruster-rig-compatibility-20261108-180926-485-c251966488a44a86a44c5a7fab92ead9` | 最小生产修复后严格PASS，0/28115ms、stderr0；四案stale0、63纯控制与所有旧委托保持。 |

有效RED与GREEN的实际窗口都开+3/闭+9/完成+17，Claw与Combat重叠14物理步，原目标21次附着事件、唯一Cast、唯一Cancel+32、Idle+38均不变。原生时钟误差RED≤.015569s、GREEN≤.015665s，间隔1tick。Idle后继续至少30物理步，捕获198–199次自然发布，不能只检查最后一帧来掩盖中间补播。两受检方向均无受伤/普通跳/额外AttackStarted。

生产改动只有MechanicalClaw动作返回前，对已有且未取消攻击调用原AdvanceAttackClock(delta)，随后仍立即返回Claw姿态。Claw解除时已过期的攻击自然None，不强迫补齐其四期；GREEN抓取组只观察到接管前的准备期，而无K控制仍完整四期。这不是取消或重启真实Combat。原`.025s`命中停顿整样本量化、v326Hurt防双推进和旧Thread早返回均不改。

新增纯控制10种非IdleClaw状态×30/60/120Hz×有无停顿60案，另3频率短攻击过期/freshID，共63案；验证非法/零时间、取消后旧ID不复活、独立字面量年龄和新ID从零开始。旧guard/11formal/2mantle/v325WJ/v326Hurt四原生及20纯控制全部仍执行，不用复制断言替代旧委托。63模型案不冒充63种原生抓取组合。

## 当前冻结资格

1915输入SHA `745B9DBD8E4CA5D213EE72A6E06DE1E3FA1DB2DDBE89F40D98B2EEF1AA4AEA75`；六产物清单SHA `41595523CE6A8B54104B0E31C5FC0067C3D146CCE95E7ED51CF27760B634AFD6`，DLL`3AD489291C58DDFD351F56C4D3DAC3283810CD594CF4600E5E6D92F657919A3A`，PDB`A4DDD72447ADEDCB2FE9F91A6BA90440B42E15A1FCB632DEAD61918D8B0DECDE`。同源最终rigcompat直接采用上述GREEN，不重复跑同一门；其余十阶段各一次，最终11阶段严格PASS，不重跑筛绿、不用v326回执替代当前资格。

最终同版11个自动回归阶段：

| phase | engine ms | case |
|---|---:|---|
| rig compatibility | 28115 | `thruster-rig-compatibility-20261108-180926-485-c251966488a44a86a44c5a7fab92ead9` |
| presentation rig | 18454 | `thruster-presentation-rig-20261108-181127-684-7c4ad0f4434b48f3be446f85d50e2883` |
| ground gait | 29946 | `thruster-ground-gait-20261108-181216-992-e3f43ac1292244938db75b1686600eaa` |
| recovery compatibility | 14958 | `thruster-recovery-compatibility-20261108-181324-218-b70713ff127944c88a49ef680a7d289c` |
| actions | 10760 | `actions-20261108-181408-640-4a9e74d0e691495f9041b49d480a98c6` |
| models | 496 | `models-20261108-181450-624-7a36c6690bba451cbc20d08f02ac5d22` |
| prologue-pre | 144005 | `prologue-pre-20261108-181528-439-82d0c985f14746d9a5a3f099b3b363e8` |
| prologue-post | 188427 | `prologue-post-20261108-181825-832-521bae36bd414296b8b40196d16531b5` |
| headless Main | 250181 | `thruster-route-main-diagnostic-20261108-182208-612-1e092415fb954fc2ab15229ecffc40ca` |
| headless Explore | 314775 | `thruster-route-exploration-diagnostic-20261108-182658-702-5e9e2348826d48358c585804811f59e7` |
| resident save compatibility | 11535 | `resident-save-compatibility-20261108-183246-109-b25031f6437b4112898d7508b3f3ce4f` |

上述11案均strictPASS/engine0/未超时/guard空，ERROR0；post仅原合同四条负例WARNING（观察者故障1、根类型1、拒绝Completed→Recovering2）。pre enter44/return44；post执行/分类75、退休0，普通300000ms cap，不是600000测量模式或旧180秒性能认证。历史v326前段FATAL不因本次通过而冲销。

source1915/binary6/user109/helper各案前后不变；总2864对before/after输入副本差异0、66/66二进制复制匹配（6文件×11案，不是66个独立内容hash）。Main标题同actor到Boss/支撑出口Completed、恢复0/0、8激活/8击败；21普通跳全部grounded，coyote/buffered/assisted接受0，unsupported_edges=2。held60位置/body/save不变，物理禁用但velocity保留(260,0)，不是零速度；实际save等于terminal/held SHA `0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517`。

Explore同样从标题以进程内同actor到Boss/实体支撑出口Completed，恢复0/0；11激活/10击败，不是全部清怪；33普通跳全部grounded，coyote/buffered/assisted接受0，unsupported_edges=8，不能把未接受的离地输入也写成0。held60位置/body/save不变，velocity仍260。实际save与terminal/held同SHA `24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24`。两个进程各自保持actor身份，不声称跨进程同一活对象。

存档兼容11535ms、独立cold0/1588ms，writer12136与reader37944由两份native记录确认不同进程，6witness及source_read_only/read_only成立；不扩大为原回执未声称的旧工程完整Continue。新写档都在隔离storage，不是用户真实存档。

## 一次独立审查与裁定

`claw_clock_fresh_review_v327`精确基线/四文件增量及原始RED/GREEN只读审查C0/I0/M0；无新增Minor，无第二审或额外修补。当前仅本次3个C#文件（1新增）＋canonical脚本变动，不是重新审核整条脏HEAD。报告不以此冲销历史延期。

裁定及仍有的风险：

- 现有原工程inline由root独占修改/构建，保留用户改动和失败、不建worktree/Git/删除；缺少新checkout隔离，靠同源清单和封存副本核实身份。
- 只修已证的表现年龄，不改战斗与抓取政策；若理解错会造成错时，已以真实窗口/身份及无K控制约束。
- 审查与冻结回归并行节省等待，但早期审查不知道后续结果；必须由最终实际回执/当前源产物和用户文件审计补足。
- v326前段终结器FATAL与历史Handle原因仍未知，不因本轮代码审查或某一阶段通过而判与修复无关、已根治；仍有崩溃风险，不执行未获许可的GC/cache实验或新增dump。
- 原生30/120Hz、命中后的hitpause、Claw+Hurt全部组合未全测；纯模型和既有Hurt证据不升级为完整原生组合验收。
- 旧Thread生产输入default且无同等正常入口反例，政策未顺带改变；未来兼容路径可能仍需独立修补。
- 正常声画、正式角色/敌人美术、硬件真人与原完整十一项依然OPEN；headless不证明动作已经优雅/精美/成品。

## 既有WER离线诊断：有材料，但仍缺对象级证据

只读定位纠正了“必须先做新GC实验才有材料”的前提：旧9月runtimeHeap与10月ProcDump授权采集实际均0dump，不能写成采集成功。旧19072/50876/11640/48696原C路径现已不存在，限定G证据也未找到dump副本；旧报告不能当作当前存在性证明。

系统后来自动留下的13696.dmp、34496.dmp确实存在，分别用PID/出生/退出时间绑定v326/1914的pre173354与v322/1912的pre145722，**不是v327失败，也不是本轮新采集**。13696为6,187,509B，SHA `6C394A42DD93FED96A6BEE658B80C0E85E688FF4D4E8637A54301A4EB9343A2E`，其对应案的封存DLL/PDB仍在且匹配，不把当前v327程序集当它的崩溃源码。

头/流/模块/内存范围元数据只读核验：MDMP、flags0x200121、77模块、1012内存范围合6,067,543B；无FullMemory标志、无Memory64流；异常线程42176、0xC000001D。详见[原始元数据](../../.superpowers/sdd/2026-11-08-claw-attack-clock/existing-wer13696-header-v001.json)。这些事实不等于证明托管堆完整，也不等于已识别故障Resource。

在游戏批次结束后，使用已安装官方dotnet-dump9.0.661903，配置禁用符号下载、仅指定本地9.0.19DAC，一次≤30秒离线预检。分析器PID44344，10:35:38.435Z→10:35:39.632Z，实际exit1/未超时/stderr空；dump前后hash相同。`runtimes`识别9.0.1926.36724及本地DAC、签名检查启用；`threads`列42条OS线程，异常42176为index31。随后`clrthreads -special`失败`Failed to request ThreadStore`，工具退出1；那条OSID0/全specialflags输出不是可信的托管线程列表，不能据此识别Finalizer对象。没有执行stack/pe/heap/dumpobj，没有对象地址/类型/ObjectID/GCroot/释放者，不猜根因，不改引擎/GC/CDAC或重试到通过。

完整argv/原始stdout/stderr/输入前后hash见[离线原始记录](../../.superpowers/sdd/2026-11-08-claw-attack-clock/wer13696-runtime-precheck-v001.json)。这一结果只证明当前这份小dump的读取边界；不是游戏测试失败、新采集成功或资源问题已修。更完整的崩溃对象证据仍待后续明确方案/独立授权，其他制作继续，不把整个目标改成等待。

## 收尾与目标状态

独立重新枚举当前真实文件（不只比清单文本）：rg退出0，1915源路径/hash差异0；实际六产物6/6匹配；真实用户目录109项hash差异0。关键当前文件：Coordinator `A39A6ACE087A1A10ABDB986D8773B2238855532B56F38606DAF808E7C85DBE4C`；ClawAttackClockTests `769866DD2087B59DF35CAC5D964405954CCB41AD1246EBEE5C2DB15A77722DE9`；ThrusterPresentationRuntimeTests `2D2EE2B24CDDA762DBE150D5602E7FB4B41A3DB6484DCCD53FB826573FE17E20`；canonical `05F4565E5BC4B750573700756E75BEF7A264BCB101AF1F003D5CECAB1A280A0F`。

媒体仅在正常汇报前一次-Check：2026-11-08T10:37:41Z、15,203,531bytes/145files、Notify=false，无新媒体/20GiB跨档；没有自动化/Watcher。当前项目Godot/dotnet/离线分析器均已终态，不终止其他godot-ai attach。G盘只读容量检查曾为剩余78.06GiB，不据此自动清理或停工。

收尾技能按用户权限保留原工程/脏树、计划和失败证据；不合并/推送/发布/Git写入、不删工作区，不以Git菜单阻塞持续制作。代价是没有新增checkout隔离，身份由上述封存和当前文件审计证明。没有本轮新增Minor，旧延期不冲销。

本轮Claw时钟小计划Task1的受检改动、一次审查及当前同源11阶段资格完成；**原完整十一项目标继续ACTIVE，未完成、未缩减**。未新增付费、窗口、OS输入、GC/cache实验、原生采集、真实存档写入、自动化、Git或删除。正式动作语言/角色和敌人美术、正常速度声画、硬件/真人验收及历史崩溃对象根因仍OPEN，不能称完整序章正式交付。
