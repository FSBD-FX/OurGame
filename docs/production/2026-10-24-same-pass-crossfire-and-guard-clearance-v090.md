# v090 · 同遍历交叉火力取证与有限朝向修正

2026-10-24开发续记，不是发行版本。唯一工程 G:/knight/Knight，统一容器 G:/knight。原十一项目标 ACTIVE，所有原稳定ID、76普通敌人＋Boss/NPC、正式视听、正常双路、真人硬件及交付要求均保留，未据本轮局部工作勾完。

## 事实、推理与边界

事实：本轮只有两个C#测试消费者变化：tests/integration/PrologueCanyonGeneratedMainRouteTests.cs、tests/integration/RangedWeaponPlacementRuntimeTests.cs。没有改变玩家操作、生产AI/物理/HP/能量、地图或正式图声资产。

上一目标轮的新事实是显示计时在加载之前启动、没有前台/会话/窗口证据；分类为显示排障PROGRESS，不是游戏制作完成。用户最终“等一下，是没看到”继续有效，本轮没有开窗或系统键鼠输入，也不把GPU图片、标题历史确认或后台测试当作真人已见。

推理：原自动试玩把同为Active的所有候选都赋优先级0，严格小于比较使先遍历到的左弹始终胜出。真实Main同遍历记录证明：当右弹已近身时仍向左举盾。仅修这一排序可以避免最初一击，但不能让单向盾同时挡住对向到达的两发，也不能解决长期不进攻造成的耗能。

风险：当前表面距离不是ETA、闭合速度或未来命中预言。导弹曲线、复杂形状、缺少几何和未采样的内部await不能由此证明安全。完整Main、Explore和资源门仍失败，本轮不是可交付稳定试玩版。

## 已改内容与真实反例

1. 在原Task10ObserveEncounterDanger同一次Consider中追加可选值观察口。记录顺序、演员/叶/弹体ID、原时间线、世界几何和是否替换winner；多边形点复制为自有只读数组。每次快照构造和回调分别隔离异常并计数，不保留Node/Resource，不增加第二次resolver或物理查询。
2. 原mixed-resource-prefix1的110帧夹具复用既有Burst叶读取作为独立预期。验证完整行数、原演员/叶/时间线/形状、真实Active零叶gap、winner、暂停/错组/排队删除的0候选，以及一次故意回调异常后的继续与叶清理。断言在callback之外，避免被诊断catch吞掉。
3. Main只从真实mixed arrival起记录180物理tick，最多4096行；原扫描之间的await缺口明确未覆盖。记录实际玩家胶囊半径/高度和六个世界变换数值；弹体位移按同实例ID和采样间隔记录，首见未知、间隔位移不冒称瞬时速度。live战斗中不打印，退出时排出。
4. 仅在两个同为零优先级的Active候选都有可计算Circle几何时，用到当前胶囊表面的较小间距解并列。保留全部Threat/SafeCounter、时间优先级、未知形状与等间距的原顺序。只mixed Main消费者及专用观察夹具提供胶囊，其他调用保持原规则。胶囊支持正交等比变换；错切/非等比/非法值回退未知，不捏造圆。
5. 独立字面用例验证表面距离15/75/9、旋转平移、错切/非等比拒绝、已知/未知/相同间距及warning优先级。观察口缺失与距离缺失都先在真实原生夹具中RED，再实现GREEN。

### 原Main反例与修改后结果

原Main111304：arrival p6802/HP3；窗口178次扫描、476行，丢行/观察异常均0。p6880玩家中心(4664.0947,3423.933)、半径20/高度72/单位基，左Circle5在(4561.545,3452.245)先胜出，右Circle5在(4689.337,3423.8325)仍败；实际右Shot1使HP3→2，盾向左但能量仍0.3686652。p6902再伤，之后E0死亡并触发真实DEATH RESET；位置跳变断言不是随机传送证据。

修改后Main112049仍FAIL：第一处旧右弹损伤未再出现，HP3维持到后来的Shot3；p6902左间距6.523926、右间距7.2387695，分别小于上一采样位移约7.136与7.333的横向量。实际仍向左挡、右Shot3使HP3→2；p7050/p7072在E0时最终死亡。窗口178次扫描、474行，丢行/观察异常均0。以上是近乎同时对向压力的证据，不宣称两条轨迹的完整预测或整场已修。

## 全部原生验证

命令为既有 verify-claw-knight-v001.ps1 -Phase 表中阶段 -SourceRows 1776；无豁免、混合flags、新阶段或放宽cap。所有案例均在 G:/knight/验证证据/october13-stable-claw/，下列时间＋GUID前缀唯一对应原目录。

| 阶段／案标识 | native exit／ms | 严格结果与范围 |
| --- | ---: | --- |
| prefix1 111004／25106ce4 | 1／1665 | RED：缺少实际候选行 |
| prefix1 111133／5f084374 | 0／3394 | 观察口诊断GREEN；110样本、49多候选、fault1、gap，HP5 |
| Main 111304／7bce0817 | 1／131565 | 原规则交叉火力死亡 |
| prefix1 111830／24f1a4e8 | 1／1540 | RED：独立15/75/9间距断言 |
| prefix1 111944／de31c71e | 0／3499 | 几何/优先级＋原110样本诊断GREEN |
| Main 112049／b783bb7e | 1／134536 | 首处改善，但对向Shot3及后续E0死亡 |
| prefix1 112622／bae75891 | 0／3393 | 最终同源诊断GREEN，110/49/fault1/gap/HP5 |
| mixed-resource 112658／6119b932 | 0／16096 | ordinary原4个fixture完整PASS |
| suppression-recovery 112748／faa0a169 | 0／67281 | 原五16tick/HP5及取消重入/三guards返回，但106 Handle ERROR，strict FAIL |
| Explore 112934／f60b5f15 | 1／53614 | 原Frozen p2861/E0/HP0，death request1/completion0，FAIL |
| pre 113102／62bcf755 | 0／164487 | 原phase OK返回，但10 Handle ERROR，strict FAIL |
| post 113421／f48c7e90 | −1073741795／161198 | HUD前提断言＋untyped finalizer FATAL，FAIL，未进registry |
| models 113736／d14ff7d1 | 0／522 | 最终普通模型专项PASS、普通构建已恢复 |

共13原生：5严格PASS（含三个有限prefix诊断）、8FAIL；构建失败0、TimedOut全false、guardFailures全0。prefix均保留behaviorAccepted=false/wholeGoalAccepted=false，不升级成整场行为证书。总批owner最后exit0只表示最后models成功，不是全批PASS。

stderr合计122个ERROR header：116 Handle、5行为断言、1FATAL；external是镜像，不能合计为244个原因。ShapeRID本轮0不关闭历史128/136等故障。post另有原seamless/room-profile两个qualified WARNING，不作为新故障。

纯合同另有实际26/96/33全部通过；不替代原生、正式视听或正常双路。

## 新资源线索，尚未修复

post实际没有进入Task7 registry，ENTER/RETURN均0。HUD_RESOURCE_LABEL_STARTUP_FAILURE为Title/NeedsTrue，低顶桥 canyon.v2.resident.low-ceiling-bridge.03：

root_is_body=True，named_binding=True，binding_count=1，profile_missing=True，actual_state=not_evaluated，expected_state=1，allow_placeholder=True。

stderr尾部PROLOGUE_TEST_FAILURE_DETAIL已flush PrologueFormalHudTests.AssertResourceLabelsFitBesideMeters原断言：expected=Playing、actual=Title。另有untyped gchandle.is_released finalizer FATAL/0xC000001D；两者跨流相邻不能证明因果，wrapper的scope mismatch不是原始原因。v089补的已求值字段在本案给出了真实拒绝条件，但仍不是修复。

suppression的Handle堆栈在原SavedSuppressedBatchRecoversAfterPlayerClearance的GD.Load处；pre在原BasinSupply.OpenSample.AddChild等边界。不能按错误条数归属某个资源、Dispose借入Shape/Profile、强制GC或改变cache/timing来拼绿。

后续只读来源核对已完成：低顶桥.03为scrap-scatter，registry指向 scenes/enemies/residents/v001/scrap-scatter.tscn；这里为空的是ResidentSubtypeProfile，不是PresentationProfile/PlayerTuning/武器Settings。该scene第29行有Resource_lut5t（SubtypeId=scrap-scatter、Family1/Archetype4/Carrier6），第97行明确将其保存给ResidentBinding.Profile；当前文件与post113421/before封存SHA一致。因此不能解释为源码漏保存。尚缺的是同一次实际加载PackedScene里ResidentBinding/Profile值，与该次Instantiate后字段的身份对照；当前只知道后者为空，不能判断加载源值已缺还是实例化/赋值丢失。已有Task14NativeResourceIdentity支持ResidentSubtypeProfile，可复用既有serial/nativeId/path，不增加通用记录平台或擅自处置借入资源。

## 一次独立复核与root裁决

复核本轮两文件完整实际delta，对比v089保留副本而非整个用户脏分支。Main基线取models104947/before；Ranged副本取suppression104712/before，其SHA与v089清单76EFF35B…E09354E0一致。models104947/before缺Ranged副本的事实保留，没有虚构双文件齐全。审查Critical0/Important0/Minor1；root同意，无fix pass或二次review。

逐项处理reviewer保留项：

1. W读取/preview：本轮未改；维持原独立验收，代价是本轮候选记录不能替它认证。
2. 首物理遮挡/receiver：未新增查询或cast授权；代价是几何快照不证明LOS/碰撞许可。
3. Intro/Playing准备：仍按原合同，本次post实际Title拒绝保留；代价是启动仍未通过。
4. callback/deferred退出身份姿态：本轮只证候选回调隔离；代价是另一路生命周期不得据此勾完。
5. LoadLevel拥有者及主/清理异常：未改；代价是旧独立验收和清理缺口继续保留。
6. 对向同刻和后续耗能：不是已证的新几何计算错误，保持Main FAIL；代价是玩家正常可玩与自动路线都还需真正闪避/打断验证，不继续猜tie常量。
7. Handle/ShapeRID/FATAL与整体验证：严重未解，不归Minor、不据quiet片段关闭；代价是不能发行稳定版。
8. 真人可见/正式视听/硬件/完整十一项：没有新证据；代价是后台测试不能代替这些交付门。

root额外裁决：保留当前用户选择的脏checkout和原计划，不执行Git/worktree/task-done/删除工作区脚本；代价是本轮未提交发行，所有未过项继续在案。父owner活着时冻结源码/帮助器/文档，未因观察等待到期重启原生进程。

Minor deferred：新增“非单位等比缩放”的独立数值用例尚缺；代码已处理但未靠字面测试证明，后续补。继承的贡献cap/异常完整性、旧Title/Supply/replay清理、Frozen flush保护三组Minor仍未关闭；真实资源失败不属于Minor。

## 保管、媒体与状态

13案65/65前后manifest对逐字节相等、78/78封存binary匹配各案OWN清单；无缺失/额外binary。最终同源7案的source/bin清单一致。1776源/892 C#，当前9帮助器/6binary逐项哈希漂移0；相对v089只有上述两个C#源变化。最终普通models：

- source manifest SHA256：5E61D332F71EF00DA30056F6F3A830819DA5DB49B946D7D0E33FC6085356C510
- binary manifest：F404E133B3AC061DC17478A56E9C197680B3337E49F81BC9C1A56120913E223D
- Knight.dll：0393D80AB1EAB50989DAD007C811441EA6BB2F620E3EB77AE88884CB023A161D
- index：21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C，branch/HEAD未变。

git diff --check exit0，169条原有换行提示、其他输出0。全部原生/build owner已终态，最后过程清单核对后只改文档。一次PowerShell只读汇总管道语法错误已修正重跑，不算native反例。

没有新增过程图稿/实录。手动一次媒体检查UTC2026-10-24T03:42:46.3444023Z：Current860284、baseline3508、growth856776字节、Notify=false；不建自动化、不停工。G剩余约182.65GiB仅为当次读数。无付费、发布/推送、压缩包、删除或真实玩家存档语义读取。

本轮分类PROGRESS：观察口与独立几何实际RED→GREEN，原Main第一处错误朝向改善且暴露真正的对向近同时压力；资源拒绝字段也首次在当前失败给出ProfileMissing。不是完整双路、美术或整关交付。

下一最小动作：资源线先对照同次scrap-scatter加载源Profile与实例字段，区分加载/赋值边界，不放开准入；战斗线用本案实际双侧压力验证合法键盘闪避/机械爪打断，保原900预算/HP/能量/所有敌人与机关。随后仍须正常Main+Explore、恢复/Continue/Boss出口、正式资源/动作/UI/音频/演出、相机、目标硬件和真人验收，再按授权发行。原十一项完整目标保持ACTIVE。
