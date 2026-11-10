# v104 · 首击目标衔接与区域定义释放归属

2026-10-25，KNIGHT，唯一工程 `G:/knight/Knight`。这是开发记录，不是发布版、整关完成或用户验收。

## 判断：事实、推理、风险

事实：用户最新更正是“等一下，是没看到”。显示状态仍为 NOTSEEN，不能记成看见窗口、教学仍遮挡或画面认可。本轮没有再开前台窗口、发送系统键鼠输入、生成付费素材、恢复自动化、提交/推送Git或删除资料。全部十一项目标保持 ACTIVE。

事实：本轮只改自动试玩的一个C#文件，没有改变生产AI、玩家能量/血量/物理、人数、地形、机关或正式图声。两条实际主线反例先失败，再实施各自最小修正；最终确实保持了首击目标，并消费了一次真实中性输入。但完整主线仍死亡，探索也仍死亡；普通post和REF pre均发生原生FATAL。10构建成功、10原生终态，严格4PASS/6FAIL，均非超时，不能交付整关。

推理：先修选敌与冷却意图冲突，比继续添加预警常量更符合当前证据；然而它并未给现有“先等敌人开火再K”的策略创造完整反击窗口。资源方面，本次能把一个FATAL定位到具体托管类型的释放调用内，但还不知道创建来源、对应native owner及错误原因。

风险：把一次中性样本当作整个冷却窗、把局部测试当作真实战斗改善、把某次pre安静当作资源修复，都会产生错误验收。也不能把REF中的区域定义错误直接推成存盘文件损坏或引擎根因。

## 实施范围与真实红绿

唯一源变化：`tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`。v103基线SHA `7F6B47242AF640EFC0EBD4DE876D065281F4B4080C4B8C24F267D22670445184`；当前SHA `7F4F5E8C10D5CD14B4A666ECE1CE2D0ABA7C44804CE7897A324950B40822B134`。

1. 实际首次J连接回调已捕获owned Burst，HP2→1。新增 `MIXED_FINISH_INTENT_RED` 先在physics6834失败：缓存Burst `17384098237831` 仍活着，旧nearest却选择Rust `17383662030336`。
2. 只对counterweight-lift、batch1增加可选的同目标续接。必须是有效、入树、未排队删除、actorRoot直接子节点、匹配prefix和原保存Burst场景、HP1、位置有限且纵距≤74。空值/不合格/尚未到场保持原路径，不筛掉其他危险。
3. 新鲜采样 `MIXED_COOLDOWN_NEUTRAL_RED` 随后实际失败：physics6829，sequence6799→6800，A=true/D=false、axis=-1、Vx=-36.666668。旧冷却分支仍向后退。
4. 仅在同首击目标、有效攻击距离、非攻击中且仍处原冷却时给中性A/D候选；后续全敌危险/防御覆盖仍优先。消费下一次真实InputReader sequence，核实际A/D、axis0、键盘模式、未持盾、地面支撑、同player/HP/恢复/跳跃/短喷计数；终态拒绝已观察却未消费的义务。没有额外等待、伪造输入样本或改物理瞬停。

完整Main最终事实（030927案）：首击后437条决策均选择同一owned Burst；physics6829实际axis0/V0、HP3/E0.51666576；真实K6922→Latched/Released6927→application/Stunned6928，HP3/E0.23066512。但后续第二次Burst J为0，E首次耗尽于7033，7046/7068受owned Burst伤害、7250被resident Burst击杀。死亡恢复后，原位移护栏拒绝3087.21的复位跳变；不是路线通关，也不是FATAL。

为什么尚未解决：首次J刚结束的6828，冷却还未结束、Safe只有0.4333333；6835冷却可用时Safe已缩成0.31666657。6919能量已低于原0.24储备；K后首次足够完整J时间的7086已E0/HP1。保留完整J+6+2、24帧冷却、0.24资源门/保留12、原900迭代和全部危险，没有为绿色删威胁、加血或放宽预算。

下一策略候选而非本轮成果：6828–6845存在18条未攻击/未持盾且有正短窗的“开火前K”候选，但没有当时真实LOS、claw-ready、飞行/护盾交接证明。现消费者要求owned实际先shot，属于策略约束而非生产机械爪权限。未来若修改，必须明确修订策略证明并保留独立的owned发射职能证据；不能偷偷删原每owned shot合同，也不能断言该策略必胜。

## 资源FATAL：只确认到调用边界

REF pre031517案，同一stderr中：

- 7条 `Handle is not initialized` 在29120/29161/29202/29243/29284/29325/29366，栈均为 `ScriptManagerBridge.SwapGCHandleForType → Node.AddChild → BasinSupplyRuntimeTests.OpenSample:463 → ReturnToTitleDuringHealingStillPersistsTheClaim:372`。不是根据stdout最后FirstCombat行归因。
- 867条typed ENTER、866条RETURN。唯一未返回call867：`disposing=false`、`Knight.Prologue.Seamless.PrologueSectorDefinition`、thread1、managed hash8268111、serial1133、raw pointer0x000001B08937F6A0，stderr29664。紧接29665 FATAL `gchandle.is_released()`；ENTER在`base.Dispose(false)`前、RETURN在后，因此失败发生于该wrapper的base释放调用内。
- call866是同类型但serial1025，已返回；不能用邻行或可复用指针合并身份。该次未返回类型不是ResidentSubtypeProfile，native_identity仍unknown。
- stderr29679另有 `Invalid prologue sector definitions`，五个definition均null、缺五区定义。共9条ERROR（7Handle+1FATAL+1定义校验），不是仅8条。此日志顺序不证明三种错误的因果，也不证明原保存资源本身为空。

普通post032514案另在118335ms非超时FATAL，未到三条必需scope（seamless/room-profile/flow-registry）。`failure.txt`先报告scope差集：缺3、多0、重复0；真正native已异常退出，不能只修日志/放宽scope使其通过。该普通案没有typed attribution，也不能直接套用REF serial1133。

没有修改生产缓存、GC、Dispose或保活策略。现有Sector identity trace和REF identify诊断互斥，未混用或重跑至安静；后续先核已有来源记录能否形成实际创建→释放身份关联，再决定最小复现，不从类型名称猜修复。

## 十次原生终态

统一证据根：`G:/knight/验证证据/october13-stable-claw/`。表中目录名可直接定位原build/engine的wait、stdout、stderr及before/after；耗时单位ms。每案TimedOut=false，均不豁免ERROR/FATAL。

| 案例目录 | build exit/耗时 | native exit/耗时 | 严格结论 |
|---|---:|---:|---|
| thruster-route-main-diagnostic-20261025-025953-859-0489f6b871454d479eedcdf742bd374d | 0/27357 | 1/128524 | FAIL，实际目标续接RED |
| thruster-route-main-diagnostic-20261025-030434-076-19516be9ad724ac8bff34bc9f4a036ed | 0/31227 | 1/134620 | FAIL，真实冷却输入RED |
| thruster-route-main-diagnostic-20261025-030927-886-eddc6af3771647afa44b732ca46436f6 | 0/28959 | 1/142613 | FAIL，局部两合同成立、完整Main死亡 |
| prologue-pre-refcounted-identity-20261025-031517-282-6ba6a39a629b44daaf85c5011ce7720a | 0/29901 | -1073741795/86975 | FAIL，7Handle+定义校验+FATAL |
| prologue-pre-20261025-032158-086-841e946268cb4f54bfefc2cd59c86beb | 0/26047 | 0/159801 | PASS，不关闭历史FATAL |
| prologue-post-20261025-032514-117-4cbe3f9a07df41f583f249a242e7191b | 0/30802 | -1073741795/118335 | FAIL，未到scope的FATAL |
| thruster-route-exploration-diagnostic-20261025-032752-321-cf8f6c7445f645ca971e99c01e6cbdb2 | 0/28828 | 1/53779 | FAIL，Frozen E0、request1/completion0@2860 |
| ranged-mixed-resource-20261025-032923-826-4dadd79736e64d238dc4620f197a9cba | 0/32515 | 0/16161 | PASS，原四mixed夹具 |
| ranged-mixed-counter-20261025-033021-826-8524c38727a941a4b1747dee33ac7f9d | 0/27184 | 0/4222 | PASS，反击专项 |
| models-20261025-033101-617-ab7cd573da6245ce9579a3b78f945947 | 0/30104 | 0/554 | PASS，普通构建恢复 |

根循环session24413现已终态；外层exit0只是foreach结束，不代表六phase都通过。全部10案共14条ERROR/0WARNING，两案FATAL。

纯验证九组分别157/96/26/33/11/24/11/64/38通过。33组使用实际vfx-frame-binding-leaf与basin-swing-release日志；64组使用原Ready control/mounted两案；38组使用v103原source-unrooted/rooted两案，未新开native。A依然是inconclusive-not-collected，B是rooted-second-assigned。这些是对应收据/准入的反例，不是新的资源修复或全部历史helper绿色证明。旧exit-verbose helper已知的过时prefix限制没有在本轮修改。

可复核命令入口：指定工具链pwsh执行 `.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1 -Phase <表中phase> -SourceRows 1844`；只有REF pre另加`-IdentifyRefCounted`。普通pre180000、post300000、正常路线600000、mixed120000、专项55000的原预算保留。

## 一次独立代码审查与逐项裁决

`v104_engagement_review`只读对照已认证v103基线及当前唯一源差异：C0/I0/M0，无请求修正、无修复复审。下面每项仍由root裁决，不以“审查无缺陷”免除覆盖成本。

| 审查不予认定项 | root裁决与成本 |
|---|---|
| 1. 最终Main成功/生存改善 | 真实最终Main仍死亡，明确FAIL；保持局部证据但不给整战进度完成资格。 |
| 2. 所有无效续接引用的native矩阵 | guard已审、空值与pending旧路径保留；未单独穷举dead/detached/foreign/wrong-scene/nonfinite，保留覆盖缺口，不宣称全负例矩阵已跑。 |
| 3. 整个冷却窗持续中性 | 只证明一次合格新鲜采样；成本是连续时窗未认证，不升级成每帧保证。 |
| 4. V0/位置不变 | 不作瞬停断言；本案V0是观察事实而非物理合同，生产惯性不改。 |
| 5. post-shot K及遮挡 | root真实同cast/target/application/Stunned证据成立一次；未扩大为所有LOS/遮挡或整战资格。 |
| 6. Intro/Playing及callback/deferred退出 | 本轮未改，不重新认证全部；保留原五ReviewFocus和现有未完项。 |
| 7. LoadLevel双异常/清理聚合 | 未改未新增资格；既有风险保留，不捆绑重构。 |
| 8. 资源及pre/post/Explore/models | root实际跑表中案例；pre/mixed/counter/models通过仅各自范围，post/REF/Explore保留失败，不因局部通过覆盖原生错误。 |
| 9. 原十一项/76普通+Boss/NPC/正式视听/硬件真人/发行 | 所有整项仍ACTIVE/未验收，NOTSEEN；本轮无正式资产增量，没有发布/合并/清理/完成目标。 |

## 冻结、保管及存储

root为唯一写入/构建/native启动者，所有活跃owned handle期间源/帮助器/文档全冻结；代理只读。既有用户指定dirty checkout原地继续，不新建worktree、不重置、不提交；成本是使用运行前后快照与SHA保管，而非干净HEAD资格。

- 独立核前4案20对清单/24件封存bin；final6另30对/36件封存bin；合计50对/60件均匹配，缺失/额外/坏件0。
- final6 source清单完全相同；helper行数10/10/9/10/10/9，共同9项哈希相同，不称所有helper整表一致。六次普通build argv均无REFCOUNTED_FATAL_IDENTITY。
- root最终实文件重哈希：runtime1546+imported298=1844，C#892，missing/extra/drift0；对v103仅上列一个测试C#变化，无新增删除。当前普通Debug六件均等于最终models封存件。
- 最终source清单SHA `F324CD84521BE27494F3152CFFB3D55C9D79BACFC9237E05F7C671FE881F375C`；helper清单 `B7903AE501CCC8218F498CCE018A5C7737EF94977410690807770FBE5A89E3AE`；bin清单 `63FF0E1CC8130C793C2B7EC94CA3A9CA5AA3C851A0C1ED93F6237B717BD2FFD0`。
- canonical owner SHA `6914E66CA017F851F81851548FFBDDCF9AED4291CD16B0524AE00816ED07DA42`、原`default_bus_layout.tres` SHA `CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A`均未改。
- branch `codex/task6-certified-final-20260902T0555`、HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变。`git diff --check` exit0，仅169条既有LF/CRLF提示；实际工程Godot/dotnet/MSBuild/VBCSCompiler owner数0后才写文档。
- 无新增展示媒体。正常汇报前仅运行一次既有容量Check，UTC2026-10-24T19:34:41.2484701Z，当前860284字节、基线3508、增长856776、Notify=false，未到20GiB；不设自动化、不因容量停工。

已更新两层README、原十一项待办、玩家意见记录（明确开发续记而非新用户设定）、原计划与ledger。目标仍ACTIVE：资源因果/正常双路/完整恢复退出/相机遮挡/正式视听与正常速度动作/目标硬件真人/授权版本交付都未结项。下一工作先处理能被原始证据证伪的资源身份和路线策略边界，不再以重复安静运行、增加预测常量或放宽储备作为完成依据。
