# 同场景源配置保活对照与眩晕预测 v103

2026-10-25。唯一工程 `G:/knight/Knight`；原十一项仍 ACTIVE。本轮没有新增正式图声、地图、敌人数值或玩法，不是序章交付或正式版本发布。用户“等一下，是没看到”继续有效：本轮没有显示游戏窗口或发送系统键鼠输入，不能登记为看到了、遮挡复现或画面验收。

## 事实、推理、风险

事实：同一个保存的 PackedScene，原工厂实例化第一只敌人并 Free 后，测试中只做一次中途 GC，再用该场景实例化第二只；A 没有额外强引用，B 保留同一源 Profile 的 CLR 强引用。两轮 A 的弱引用都仍存活，B 也存活、二次赋值正常。A 的结论均为 `inconclusive-not-collected`，不是“回收后正常”。

推理：实验没有得到被回收的 A，因此既不能证明 CLR 包装器寿命是旧崩溃原因，也不能据此实施生产保活修复。原工厂的 432/480/Ready 路径未被这个独立实验插入或替换；中途固定一次 GC 与单独标识的进程退出清理不混为一谈。

事实：实际保存 Burst 在机械爪命中、已发布 Stunned 后，自动试玩原先把下一次攻击的预警下界读成0。新增真实反例先失败，再只让此状态贡献模型已经捕获的完整 Telegraph 下界，不虚构剩余眩晕时间。原生专项通过。

风险：完整主线仍死亡、探索仍 Frozen E0 死亡、完整前置仍原生 FATAL。后置完整75/75及后置所有者检查通过不覆盖前置失败。没有正式视听、相机、正常速度、目标硬件或真人试玩验收，不能称整关稳定或十一项完成。

## 实现与真实红绿顺序

三份现有 C# 测试文件修改，无新增/删除源项；1844项仍为1546 runtime＋298 imported，C#892：

- `tests/diagnostics/RunDetachedEnemyResourceRepro.cs`：REF专用、两个独立进程 A/B。同步 NoInlining 辅助函数读取同保存场景的源 Profile；验证第一实例同原生ID、同CLR包装器、非LocalToScene和一次非空 setter，Free唯一演员后仅返回弱引用、数值及B的额外强根。弱引用在中途 GC 前不读取；同一个 PackedScene 保持到二次原工厂结束，B的强根保持到二次赋值后。不 Dispose 借来的 Profile/SceneState，不改生产缓存、GC或固定保活策略。
- `tests/integration/RangedWeaponRuntimeTests.cs`：保存 Burst 的原机械爪打断案例，在实际 alive/Stunned、自己的攻击已撤销且没有表现权限时观察预测。020241实际 RED：期望1.099999、实际0；020544实际 GREEN，保留原眩晕不续期等后续断言。
- `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`：仅给实际 Stunned 的 RangedWeaponEnemy 增加捕获的 Telegraph 下界。原 caller 仍筛 alive/入树/未排除对象，外来攻击叶和真实 Active 仍能把全局窗口置0。

canonical owner 加两个精确 REF-only phase、准入、输入冻结和严格收据；新增38项纯合同脚本。所有生命期行与赋值/释放身份按同一stderr时序连接。普通构建不带REF常量，任意 ERROR/FATAL 仍严格失败。

原24能量门是“支付一次12攻击成本后保留12”的既有消费者安全策略，不是生产支付两次攻击费用。此次不改这个储备，不改完整J＋6＋2时间门、fresh复核、24帧冷却、900外循环、AI/血量/碰撞/原旧弹和外来威胁。未来若修订储备策略，必须明确记为策略变更并验证空挥后防守风险，不能声称原合同完全不变。

## 一次审查与一次有界修正

唯一新上下文审查 C0/I2/M0；无复审。

1. I1：第一/第二次真实 setter 的 serial/hash 未与已求值字段完整连接；同一CLR包装器的第二次字段hash也能矛盾。实际 A/B 上12个独立身份变异均能误收。root 先加真实行变异，亲见 `SOURCE_LIFETIME_IDENTITY_RED operation=1 fields=Setter.LastManagedSerial`，再把 setter↔field↔source 及同包装器hash连接放在所有分类返回前；最终38合同通过。原 null-rejection 要求不放宽。
2. I2：新增顶层 source 准入调用放在旧 FullPost 准入前，破坏旧“测量准入先于顶层执行”的合同。原24项脚本实际 RED；仅移动该调用到旧准入之后、所有build/IO之前，旧脚本不变，24项 GREEN。

早期26项变异脚本还发生过 PowerShell comma/+ 优先级错误：多构造出一条未变异原件。失败保留；仅给末项串接加括号后，独立四变异/arm均被拒绝，26项通过。之后I1扩成38，不能把最初失败回写为通过。

最终实际纯合同：157、96、26、33、11、24、11、64、38均通过。Ready64使用本轮最终control与旧已封存mounted日志，不冒充本轮运行了mounted arm。一次旧v077脚本调用漏mandatory LeafLog，未执行测试，随后带实际Leaf/Replay日志33通过。另额外尝试的旧 `test-exit-verbose-admission.ps1` 拒绝当前较长准入前缀；同一AST白名单在v102基线已拒绝10种调用，当前12种。该历史脚本仍失败，不算当前套件绿色、未静默放宽白名单；本轮真正引入的FullPost准入顺序回归则已修复。

## 十五个自有原生案

证据根 `G:/knight/验证证据/october13-stable-claw/`。以下全部15构建成功、15 native均终态且未超时；11严格PASS、4严格FAIL，PASS含6个纯诊断案。最后9案期间没有源/帮助器编辑；帮助器清单按phase集合不同，不声称各清单字节相同。没有由于等待工具返回而重复启动。

| 完整子目录名 | build ms | native exit / ms | 严格结果 |
|---|---:|---:|---|
| resident-resource-source-unrooted-refcounted-identity-20261025-015848-105-ac7257c31976449682e43bdd38eb05c4 | 25315 | 0 / 655 | 诊断PASS，A未回收/不确定 |
| resident-resource-source-rooted-refcounted-identity-20261025-015922-789-e4136be811f74504ad40527186fbccf3 | 23238 | 0 / 624 | 诊断PASS，B保持/二次赋值 |
| ranged-actor-saved-20261025-020241-313-2760e8b0e6814e04bdf958b20c3f2394 | 26109 | 1 / 28789 | 真实Stunned预测RED |
| ranged-actor-saved-20261025-020544-160-b5cda7fbb4314baa885f3f825ba84db6 | 30679 | 0 / 58669 | 专项GREEN |
| thruster-route-main-diagnostic-20261025-021128-125-724bbab70d304744a5ffa63844135535 | 29911 | 1 / 160742 | 主线死亡，严格FAIL |
| resident-resource-ready-control-refcounted-identity-20261025-021701-693-b86423182fe842e6b6c63a3c8b026721 | 29391 | 0 / 3909 | 原Ready control诊断PASS |
| resident-resource-source-unrooted-refcounted-identity-20261025-022530-490-8e86cf8612c544518ca54a4b653e146e | 30115 | 0 / 857 | 最终冻结诊断PASS，仍不确定 |
| resident-resource-source-rooted-refcounted-identity-20261025-022612-561-32504afe98dd4933a768b80f8c99ac10 | 32662 | 0 / 723 | 最终冻结诊断PASS |
| resident-resource-ready-control-refcounted-identity-20261025-022653-374-9b40cfb0d9e5406eb7fdd5761d23f228 | 25897 | 0 / 3626 | 最终原control诊断PASS |
| prologue-pre-20261025-022730-109-38db5c335ca3477bb6c74335b215fcaa | 26177 | -1073741795 / 102804 | 3 Handle ERROR＋1 FATAL ERROR，FAIL |
| prologue-post-20261025-022947-186-091a3fc3a2874096a7b5bf8705cf1eec | 27448 | 0 / 203319 | 原75 enter/75 return及后置完成，PASS |
| thruster-route-exploration-diagnostic-20261025-023346-444-e2275f16c514402cb16efbdbe0ad0052 | 28139 | 1 / 53478 | Frozen E0，恢复request1/completion0@2860，FAIL |
| ranged-mixed-resource-20261025-023517-203-de5494f3fff646e99a6f2635a5ffc6d6 | 31107 | 0 / 16246 | 原四混编夹具PASS |
| ranged-mixed-counter-20261025-023613-532-06cb847c5f8b4d2b857f01616077e65d | 26615 | 0 / 4192 | 原counter及终态合同PASS |
| models-20261025-023653-122-9a9f8d0ebf92414eaa0a371224b48deb | 29442 | 0 / 543 | 普通models PASS/普通二进制恢复 |

原生stdout＋stderr合计7条ERROR：具名Stunned RED、Main、Explore各1，pre的3 Handle及1 FATAL；post有原合同接受的4条故意负例警告，其余无新ERROR/FATAL。post是一个所有者收据的 `repetitions=8`，不是8个所有者收据。原普通pre cap180000、post300000未改，203319ms不当作性能验收。

## 主线改善边界与下一步

本轮与v102各555条决策按physics完整匹配，所比较的输入、选敌、能量、热量、HP、冷却等全部相同，仅39条SafeCounter变化；K后counter-ready及实际J启动仍为0。实际K p6920发出，p6926同Burst Latched/Released，p6927 application/Stunned，玩家HP3；不是伪造打断。

- p6927下界从0变`.24999985`，仍不足完整`.43333334`。
- p6942真Rust Active仍把全局窗口置0；p6951窗口`.4`且E`.17466517`，两个门都不满足。
- p6994首次时间足够`.5833333`但E`.13466524`、盾仍active；6998已松盾，能量仍不足。
- p7055首次E0，7059 owned Burst shot5使HP3→2，7083 resident Burst shot15使2→1，7248其shot18致死。stderr的3066.98位移断言来自死亡恢复，不是本案资源FATAL或超时。
- 目标意图在p6835冷却A撤退后已由1血Burst换成Rust；K另行命中Burst，并未把后续选敌交还给它。

下一窄候选：先验证首击后保留同一活着HP1目标的合法意图，使用真实中性刹车替代这段无条件冷却A撤退；保全部危险、J时间/储备/fresh/L门。不实施能量补充、强制目标或免伤，不提前承诺节能/开出J；本轮尚未写入或验证该候选。资源下一步仍需能区分真实失败的证据边界，不重复相同GC直到“好看”。

## 审查未裁定事项的root裁决

| 审查设旁事项 | root裁决与代价 |
|---|---|
| 生产资源/原崩溃因果 | 保留OPEN，不改缓存/保活/GC；代价是崩溃仍阻止交付。 |
| 其他JIT/GC条件下可回收性 | 延期到有区分力的独立实验，不无限重试；当前A不能证明可回收。 |
| collected/null/finalizer收据分支穷尽 | 38项不称穷尽；未来真正收集A前须补相应原件与反例，不能借现在的quiet推因果。 |
| 整战/选敌/能量/难度 | 实际Main和Explore仍FAIL，下一仅合法驾驶候选；代价是不宣称战斗体验提升已完成。 |
| 最终普通pre/post/models/Ready | root已实际运行，如上逐案；pre失败不被post或诊断PASS覆盖。 |
| 完整保管/普通恢复/文档 | root与独立只读核验分工；冻结二进制恢复普通，文档只在所有owner退出后写，见下。 |
| 历史ERROR/FATAL | 全保留，既不豁免也不归咎某个邻近Profile；当前原生pre继续证实问题未闭合。 |
| 视听/相机/硬件/真人 | NOTSEEN有效，另需未来获准的显示时段；无新图声或系统输入，不挪用旧26秒授权。 |
| 原十一项/76普通＋Boss/NPC/机关/包装/Git | 全部原稳定ID仍在总待办；本轮不算整任务done，不提交/推送/清理或改变另一UE工程。 |

## 冻结与保管

canonical SHA256 `6914E66CA017F851F81851548FFBDDCF9AED4291CD16B0524AE00816ED07DA42`；38合同脚本 `A284C7EF0CA2220C98C035AC0901236BC1D655EB8B85E88623B6BAF4C2031C75`。

最终models全源清单SHA `5EC60CDAD58CAE9F62930423EC75D40990E2E90D0F646FC68150E5D1EBF4DB20`；帮助器清单 `B7903AE501CCC8218F498CCE018A5C7737EF94977410690807770FBE5A89E3AE`；普通bin清单 `D82B44F727130A977852E227E6491267888A4B7FD2939D2B2D0F2AF4E185A9B4`。清单的逐案before/after及封存原件保存在各case，不拿当前helper改动回判旧案内漂移。

branch/HEAD保持 `codex/task6-certified-final-20260902T0555` / `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`；index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 不变。root diff-check exit0、169条既有LF/CRLF提示、其他0；自有Godot引擎/dotnet/MSBuild/VBCSCompiler均0，godot-ai attach不当引擎owner、不触碰。没有Git写入、删除、付费、自动化或真实玩家存档语义读取。

一次正常汇报内媒体检查 UTC `2026-10-24T18:39:08.7456946Z`：Current860284、Baseline3508、Growth856776 bytes，Notify=false。本轮新过程媒体0，不建立监控任务，不因20GiB提示停工。

独立只读保管：初始6案24对 source/helpers/user/continuation 清单＋6对 fresh/after binary清单、36封存件；最终9案36对＋9对、54封存件，合计75对清单、90/90封存bin匹配，缺失/额外/坏SHA均0。最后9案 source-before原文相同、均1844项；helper集合12/12/12/10/10/9/10/10/9，共同9项SHA一致，不能说各清单全相同。最终models清单逐1844项对当时live源 missing0/drift0，当前普通Debug6件与其封存6件匹配。独立核验未重判严格receipt、未重新枚举全项目所有额外文件，不能扩大其结论；各原生严格结果由root canonicalowner实际回执及保留原件确定。

本报告/两个README/总待办/意见档/计划均在所有native终态后写入，source/helper不再改动。所有完整交付义务仍OPEN。
