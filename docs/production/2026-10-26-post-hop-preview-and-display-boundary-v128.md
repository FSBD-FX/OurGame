# v128 · 真实W预判、终态保全和未显示边界

2026-10-26。KNIGHT，`G:/knight/Knight`。继续原十一项和原稳定待办ID，不缩减为诊断任务；原76普通敌人＋Boss/NPC、地图机关、正式视听动作UI相机、目标硬件真人与授权交付要求全部保留。目标ACTIVE，非整关完成。

## 事实、推理、风险

- 事实：用户明确没有看到上次约24秒游戏画面，记NOTSEEN。本轮没有再打开可见演示、系统按键或新截图。只改2测试C#，未新增生产玩法/地图/美术/音频。
- 推理：真实一次W准备得到新鲜现成预判，但选中上方AscentGrip2地形，不是拟打断的Moth；策略因此正确拒绝新K。不能称“没有任何可抓目标”“K命中失败”或“鬼魂不能抓”，也不能推定换方向必然安全。
- 风险：Main/Explore仍真实死亡。一次收据记账修复不等于新K实际成功、通关、显示或画面提升。普通前后段本轮安静，不关闭历史GCHandle/Finalizer FATAL和ShapeRID；旧4纯harness失败与9Minor仍开。

## 本轮实施与裁决

1. 仅在原hop完成、自然回能、原完整地面和全危险门允许时，发出一次真实W准备；下一原外循环只消费已有`KeyboardPreview`。被动回调记录physics/input sequence/authority/玩家pose/瞄准/盾与动作，不调用第二resolver、不在回调发输入，不加等待池、角度重试或目标强制。17字面量合同先真实RED411ms，再GREEN1800ms。
2. Main130241实际7296准备W，7297 `ready=True/fresh=True`；实际目标4755199890158，存在/LOS真、legacy假，同案身份为AscentGrip2地形。新lease `attempted=True/issued=False/stage=3/previews=1/target=0`；target0是没有批准新K，不是预判未选中任何物体。没有新K效果、释放或急防取消的正向原生证明。
3. 既有单跳69样本/一条Space/同地面/natural24仍成立，三弹分别1passed、2retired-unpassed，不能写三弹全过。Main后续7376被常驻`.counterweight-lift.03` ghost-diver attack3击杀，E0、L按住但盾不活跃、未过热；恢复请求1/完成0，严格FAIL。
4. Explore131438同样失败：8546 ghost使HP2→1；9045 bone-wanderer`.counterweight-lift.01`在Grip2准备首帧使1→0。这里physics9045、inputSequence9016，不能混用时钟；没有新K或二段跳证明。
5. 唯一审查发现I1：整场终态仍要求收据总数恰为本次K之后的数量，错误拒绝之后的合法施放。根维持Important，一次TDD pass：原错误条件的真实native字面量反例506ms RED，只有“后续合法cast仍保留指定收据”失败；最小修正为在原全账本接纳的实际归还点冻结完整不可变收据，终态按固定索引和全字段相等验证，原全账本继续检查全部后续K。12合同1809ms GREEN，17预判/原全部反例保留，无复审。不是实际正常路线新K成功。

实施Ruling及错判成本：保留一次W/原预判，不另算resolver授权K，成本是合法机会可能仍未被找到；拒绝地形而不盲改方向/排名，成本是整战未闭合。修正终态只保全指定历史条目，成本是必须防止替换/缺失收据，故保留全字段比较和原全账本。公平held-resource A/B留作尚未实现的下一实验，成本是资源归因延后；旧B的scene未跨释放保活，不能与新held-A作因果对照。显示原因未知、不自动重开，成本是本轮无新的真人画面验证，但不以后台事实冒充看见。

## 13次尝试，不能拼冻结版本

| case | strict | engine exit/ms |
|---|---|---:|
| [125740 预判RED](../../../验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-125740-778-6b9bc3792d684f0cab1b8fbeb58df40e/engine.wait.json) | FAIL | 1/411 |
| [125948 mixed构建](../../../验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-125948-933-bb5e877bcf0f432cbbefbb7e279966c1/build.wait.json) | build FAIL | 无engine |
| [130015 Main构建](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-130015-017-ed5685fef6324700a1a4ef3f8c786e87/build.wait.json) | build FAIL | 无engine |
| [130210 预判GREEN](../../../验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-130210-473-91b7e5f577324aa6bbd3cebbd8c4d13a/engine.wait.json) | PASS | 0/1800 |
| [130241 Main](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-130241-538-360dc31f4020414381cdbf4f7c2e12ec/engine.wait.json) | FAIL | 1/137227 |
| [131438 Explore](../../../验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-131438-027-be63404023e94ea5892685dfce260a4d/engine.wait.json) | FAIL | 1/163439 |
| [131754 pre](../../../验证证据/october13-stable-claw/prologue-pre-20261026-131754-984-5b35304acfea41b094d158eb29dc53b7/engine.wait.json) | PASS | 0/161833 |
| [132106 post](../../../验证证据/october13-stable-claw/prologue-post-20261026-132106-226-062d3ccab73a40dd9f1e7d90db464ee5/engine.wait.json) | PASS | 0/201947 |
| [132500 models](../../../验证证据/october13-stable-claw/models-20261026-132500-114-97374226911f49c996ca364e79612b78/engine.wait.json) | PASS | 0/512 |
| [132602 I1 RED](../../../验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-132602-971-e56ef81584ec44578d71712e2070814c/engine.wait.json) | FAIL | 1/506 |
| [132654 最终GREEN](../../../验证证据/october13-stable-claw/ranged-mixed-early-boundary-20261026-132654-433-a5583811b8d5402fa01d7668a17b075f/engine.wait.json) | PASS | 0/1809 |
| [132726 最终post](../../../验证证据/october13-stable-claw/prologue-post-20261026-132726-892-96656123052c44478f2176d60b0481bb/engine.wait.json) | PASS | 0/198677 |
| [133114 最终models](../../../验证证据/october13-stable-claw/models-20261026-133114-422-a2c8dbb27a954fe68e75797f9125ed98/engine.wait.json) | PASS | 0/412 |

7严格PASS、4原生FAIL、2buildFAIL；两个buildFAIL均CS0136局部target遮蔽，改名postTarget后构建成功，不能计行为RED或新native。11成功build均0warning/error，13build及11engine均无超时，guards0/ExpectedFailure空。四原生FAIL各ERROR为stdout0/stderr1/external1（镜像不是两个故障），本轮FATAL0不证明历史问题已修。两个post各4既有范围内warning，三类scope精确匹配；各自75enter/75return索引0–74、owner8 nodes1→1、PHASE_OK/TESTS_OK各1、实验CONTROL0。

最后132654/132726/133114才是同一修正后源码。Main/Explore/pre及早期post/models均在I1前，报告保留真实失败/通过但不拼成最终同版全套。未重复盲跑Main；最终只有专项/post/models新鲜资格，原完整正常双路与前段最终冻结资格仍待后续。

## 唯一审查、25条根裁决与成本

[审查处置](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v128-final-review.md)。唯一fresh C0/I1/新M0，根一次TDD修复I1，无复审，不篡改审查前“不建议合入”的原判词，不宣称全套绿色/merge。下表逐项处理全部Declined事项；不是默认忽略或完成。

| 项 | 根裁决 | 错判成本/保留风险 |
|---|---|---|
| 1 原十一项 | 全部ACTIVE，不缩范围 | 提前交付不完整序章 |
| 2 Main资格 | 保留7376死亡FAIL | 正常路线仍不可可靠完成 |
| 3 Explore资格 | 根核实际9045死亡FAIL | 探索后继路径仍不可可靠完成 |
| 4 pre/post/models | 区分I1前后，最终只专项/post/models | 拼版会掩盖未测变更 |
| 5 whole-suite | 不称全绿，旧4harness失败未修未重跑 | 准入测试债务继续存在 |
| 6 新K游戏效果 | 未执行，不认证 | 无法证明打断可缓解压力 |
| 7 直接K释放/急防 | 接线经审，原生正向与打断分支未证 | 仍需真实按键生命周期案例 |
| 8 移动目标/未来LOS | 保留生产重解析，不给未来安全保证 | 目标变化仍可拒绝或中断 |
| 9 通用排名/遮挡 | 不改排序，当前只证选中地形 | 抓点竞争体验可能仍差 |
| 10 Intro/Playing | 原准备拒绝/等待不变，不重新全认证 | 未测准备组合仍可能失败 |
| 11 callback/恢复矩阵 | 保留同身份/队列释放和旧全账本 | 不能声称所有异步边界全覆盖 |
| 12 LoadLevel双异常 | 未改、不冒认重新验证 | 主异常/清理异常矩阵仍待 |
| 13 全游戏资源身份 | 只证当前局部live target检查 | 全地图保存数据所有权未全认证 |
| 14 FATAL/ShapeRID | 历史故障OPEN，无生产资源修复 | 随机退出崩溃/泄漏仍有风险 |
| 15 held A/B | 尚未实现，不推导Load或Instantiate因果 | 原生资源定位延后 |
| 16 全76＋BossNPC | 范围保留，无全体生命周期验收声明 | 数量保留不等可玩质量 |
| 17 三弹/扫掠 | 仅1passed＋2retired，不普遍安全 | 不能替代连续碰撞证明 |
| 18 正式角色/动作 | 本轮无新资产，不关闭完整动作/双手等 | 正式视觉仍不完整 |
| 19 视听/UI/相机/密度 | 无当前真人正常速度认可 | 精美度和可读性仍待验证 |
| 20 用户显示 | NOTSEEN权威，原因未知不归咎用户 | 后台运行不等用户可见 |
| 21 目标硬件 | 不给FPS/加载/资源预算结论 | 性能仍可能不达标 |
| 22 存档/冷启/真人 | 真实存档不读语义，未新增迁移测试 | 旧验证不覆盖所有后续组合 |
| 23 保管终态 | 根采用独立本轮63对审计，不扩到全历史 | 历史12缺份仍不能补证 |
| 24 未来/付费/商业/发行 | 无新付费或发布，不保证十万订阅 | 商业目标需真实外部验证 |
| 25 继承9Minor | 明确继续延期，不追认已修 | 技术验证债务继续存在 |

Final: minor (deferred): 九项为energy-unwind早失败flush、boundary swept措辞、finally stage4摘要、fresh LateFact来源、rotated-AABB负例、withdrawal旧return诊断、unknownActive真实scanner负例、v126十二份cache-helper实体缺失、v127 shot上游physics/input/capsule比较独立破坏负例。新W17例不关闭旧shot项。四旧harness为`test-exit-verbose-admission.ps1`、`test-full-post-budget-v098.ps1`、`test-level-cache-admission-v001.ps1`、`test-sample-boundary-admission-v076.ps1`，本轮未修未重跑，不能借最终post绿色覆盖。

## 保管、容量和下一步

[独立保管](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v128-custody.md)：root所有native/batch先终态，审计最后327ece exit0/ENDED后才写收尾文档；63对清单/6514实体/66sealed匹配，新增missing/extra/drift0，当前1848源/892C#/Debug6匹配最终models。仅两测试C#193+/2−，源新增删除0，production/helper/assets不变。HEAD/branch/index/audio/Claw/Skitter/Traversal保留原锁定值。13build+11engine精确身份均已终态，owned CIM0，无关godot-ai attach不动；真实存档仅清单字节核验。

本轮唯一手动容量Check UTC05:32:28.3291593，Current1431609、Baseline3508、Growth1428101字节，16files/Notify=false。无新媒体、通知自动化、打包或删除；20GiB提醒仍只随正常汇报提及、不停工。上次图稿仍在既有v127媒体目录，不生成替代“用户看见”的新截图。

下一步先解决可见标题页的实际显示核对，再进行正常速度动作/画面审阅，需当次新前台确认；后台可继续原合法打断与完整攻击窗口、资源公平分段诊断和正式素材/场景UI密度工作。没有新小目标、暂停或完成标记，没有Git写入/发布、付费生成、自动化、引擎安装或资料删除。
