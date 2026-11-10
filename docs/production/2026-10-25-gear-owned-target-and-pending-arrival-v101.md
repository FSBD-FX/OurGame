# 齿轮混编选敌与到场过渡 v101

2026-10-24 夜间开始，10-25 完成此轮验证。工程 `G:/knight/Knight`；这是原十一项的局部推进，不是版本发布或整关交付。

## 事实、推理、风险

事实：改动仅在两个自动试玩消费者中，没有修改生产敌人、地图、伤害、生命、能量、物理或素材。原规则只把首个 owned 演员作为攻击目标，后续只允许更近的常驻敌人替换；同一编队的第二只机枪敌人不参与比较。现已在实际键盘的配重区 batch1、且确有 live owned 演员时，让全部存活同高 owned 与原常驻敌人按原距离规则共同参与。

推理：这可消除一个真实选敌盲点。最终正常标题主线实际在开火前打中第二只机枪手，HP2→1；不是只让纯函数或占位场景通过。

风险：一次命中不是战斗胜利。最终主线仍死亡，探索仍在冰区死亡；完整前后段回归通过也不能覆盖这两次失败、关闭历史资源崩溃，或代表正式视听/真人/硬件验收。

## 真实修改与红绿顺序

文件：

- `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`
- `tests/integration/RangedWeaponPlacementRuntimeTests.cs`

先原样抽出选敌方法，再用实际保存演员、记录的到场坐标查询，观察“更近的第二只 Burst 应参与”断言真实失败。最小修正让 opt-in 模式接纳全部 owned；原默认 owned-first＋更近 resident、同高74、正生命、在树/非排队删除、严格距离小于与相等时原顺序均保留。

第一次真实主线暴露范围缺陷：`CurrentBatch` 在旧 Moth 死亡排队删除时立即变1，而新演员尚未出生。新比较提前一帧改变了原 pending 行为。增加实际保存 Moth 死亡后的原生反例，先见 RED，再只以 `group.LiveActorCount > 0` 限制新模式；查询使用实际 group，避免测试和真实调用各写一套前提。

四个原 mixed-resource 夹具完整通过，每个都有真实 pending 与选敌收据，共4＋4。记录姿态只作选敌查询参数，没有移动玩家；原夹具自身已有明确 pretrial 设置，不能冒称从正常标题出发。真实路线另用下表的两个 Main 案例证明。

全部原900循环、24帧出手冷却、完整J＋6＋2反击门、原能量/盾/旧弹/敌群/支撑/恢复断言不变。没有新等待池、强制出生位置或难度放宽。

## 十个自有原生案例

公共证据根：`G:/knight/验证证据/october13-stable-claw/`。表内每个目录保留原始stdout/stderr、构建/引擎/等待契约、来源清单、隔离数据与封存二进制。所有构建 exit0；所有原生均实际终态，TimedOut=false、guardFailures=0。

| 目录（公共根下） | 构建ms | 原生exit / ms | 严格结果及意义 |
| --- | ---: | ---: | --- |
| `ranged-mixed-counter-20261024-234731-964-c3b0d97b127743d196e3fb2179aba130` | 29573 | 1 / 1470 | RED：真实 nearer-second-owned 断言 |
| `ranged-mixed-counter-20261024-234919-985-9a9f5913a67442888121d5ebe3ea7427` | 25660 | 0 / 4135 | 初次选敌GREEN，原Rust反击及攻击后6帧保留 |
| `thruster-route-main-diagnostic-20261024-235023-812-0fe2357c55d44a13898cc183d3a37d41` | 26312 | 1 / 126253 | 首候选Main失败，真实死亡p6998；新模式误入pending |
| `ranged-mixed-counter-20261024-235946-663-b055b4206e7c4397969bfd48feb796af` | 27531 | 1 / 813 | RED：真实旧Moth死亡、batch1/live0时须保留原选择 |
| `ranged-mixed-resource-20261025-000107-298-58a41df424eb40cba5e08796016c9f1a` | 24394 | 0 / 16126 | 最终四个原夹具＋4 pending/4 selection通过 |
| `thruster-route-main-diagnostic-20261025-000220-904-756074efed2940e4b55a86cb84fd3fb6` | 24890 | 1 / 130546 | 最终Main：真实打中Burst，但p7202死亡 |
| `thruster-route-exploration-diagnostic-20261025-000616-832-9dc62441a4124456b2dd7168ccf7fb34` | 27128 | 1 / 54453 | 最终Explore：Frozen p2860、E0死亡，未到Gear |
| `prologue-pre-20261025-000805-412-f21c3b207dcc474c89bcc8ca521996ac` | 31699 | 0 / 169626 | 最终普通前段严格PASS |
| `prologue-post-20261025-001154-576-796ac078778f4f04872653a352fff7eb` | 28437 | 0 / 199028 | 最终普通后段严格PASS；原75 enter/75 return、同owner重复8次及后置完整 |
| `models-20261025-002354-730-695ca522fcb849c5bf28e1e658f7bf5b` | 27774 | 0 / 667 | 最终普通models严格PASS，普通构建保留 |

合计10原生：5严格PASS、5FAIL。FAIL包含两个预期断言RED和三个真实路线失败，不互相豁免。stderr ERROR共5；唯一4条WARNING出现在完整post，并分别由原seamless1、room-profile1、flow-registry2精确收据限定。没有本轮Handle/FATAL，不以安静结果关闭此前失败。

post生命周期是**一条** `TASK7_MAIN_PACKEDSCENE_LIFETIME_OK repetitions=8 owner=RunPrologueTests nodes_before=1 nodes_after=1` 收据，不是8条owner回执或8个拥有者。普通post cap300000，199028ms通过不意味着旧180000预算通过或硬件性能认证。

### 最终Main的有限改善与剩余问题

自然 suppression p6686 → 实际中立x4701.148/p6761 → 原混编出生p6801、live2/p6802。没有强制该出生。正确选右 owned Burst `17382101749038`；J请求p6809、Started p6810/玩家AttackId33，p6814同actor出现 `health_damaged` 与 `player_attack_connected`、HP2→1。首次Active p6868，命中领先54个物理帧。

J结束p6828时，原24冷却仍在；原 cooldownRetreat 开始A，p6834最近目标转到Rust。冷却结束p6835时全局安全窗只剩`.31666657`，不满足原`.43333334`，第二J被正确拒绝。p6828之后连续六个决策样本仍HP3、同Player、on_floor=true；这只证明实际攻击完成后有限尾段未掉血，不是全程稳站或安全战斗证明。

到p6998才再获完整反击资格、E约`.24466528`，J34在p6999打Rust；Burst仍HP1。p7053 E0；左侧resident Burst在p7059、7081造成HP3→2→1；p7202最后伤害来自Rust，当时盾向右防owned Burst，不能再归为同一个盾tie错误。死亡loop622，未耗尽900。

首候选235023中两次J只属于batch0 Moth，不得计入混编。该案实际出生姿态4635.214/Vx−260、Rust4700/Burst4770，没有进入suppression；最终案与它不是同一出生前提。

### Explore

Frozen实际最后损失p2860，盾键held但E0/盾未激活，Moth真实命中；request1/completion0触发原zero-recovery veto。128行记录/omitted3/faults0，内部await间隙并未逐帧观察。不归因于本轮尚未抵达的Gear新选择，不以主线一次命中覆盖该失败。

## 资源归因：排除一个具体时序假设

原件：`G:/knight/验证证据/october13-stable-claw/prologue-post-refcounted-identity-20261024-162742-777-ab62c16543c1444285c572bdfa890a69/engine.stderr.log`。

同一stderr：op272成功使用serial1808/hash12677791（4746）→ op273开始（4747）→ op292原setter收到null、gate拒绝（4892）→ serial1808才进入Dispose(false)（4913）→ FATAL（4914）。同步ConfigureActor已在下一工厂操作前返回；无需再补“使用完成”或重复raw pointer日志。这个顺序否定“serial1808先终结导致op292为空”，不否定其他资源或所有生命周期原因。

该次没有实例化前的Profile source行：post只启用assignment trace，失败后同scene的source为wrapper-null/invalid。因此不能把别次有效source日志与该次null拼成同操作因果，也不能据此改缓存、保活、GC或升级引擎。

## 一次独立审查与裁决

新上下文审查 `/root/v101_gear_owned_target_review`：Critical0 / Important0 / Minor1。没有第二次审查或Important修复轮；本轮两个红绿循环都是送审前的实现验证。

Deferred minor：原12320附近“Preserve first-child attack targeting”注释过时，与新opt-in行为不一致；运行行为不受影响，本轮按审查流程延期，不据旧注释恢复错误选择。

审查Declined逐项root裁决（保留不判断的成本，不静默缩减原目标）：

1. 原十一项/全人口/平衡及E06/E07/M09：仍OPEN；本轮只是选敌消费者。成本是还有完整玩法与成品工作，不能报完成率。
2. 整Main/Explore：以两份最终FAIL为准，不验收。成本是尚无同版正常双路通关证明。
3. 普通pre/post/models：root后来实际完成同冻结三门PASS；原75/后置未删。成本是这只覆盖该套回归，不覆盖失败路线或历史间歇崩溃。
4. tie/死/离树/排队/超高/owned-resident竞争的全部新原生负控：原谓词静态保留，不冒称每项新执行。成本是组合覆盖仍不足，完整正常路由不因此豁免。
5. W实际读入/预览：本轮未改目标解析排名；旧边界仍按原证据，不能新增正确性结论。成本是它不在本轮新验收内。
6. 实体遮挡/receiver资格：保原resolver/LOS；一次Burst命中不等于全抓取遮挡通过。成本是未来K必须在真实原解析下验证。
7. Intro/准备/Playing：不强置状态或增加猜测等待。成本是旧准备门仍需组合验收。
8. callback/deferred退出：仅本轮旧Moth同步batch++/排队间隙实际覆盖；其他同引用/姿态/父级/序列合同不扩大。成本是广义生命周期仍OPEN。
9. LoadLevel主异常/清理异常及历史资源/FATAL/RID：生产代码未改，保留旧失败和本轮有限时序否定。成本是不得把pre/post安静称为资源修复。
10. 生产AI/血能量/物理/盾/危险/旧弹：不改生产规则，不宣称全面审计。成本是最后群战与平衡仍需实跑。
11. 全工作树/Git/资源原因：以独立custody与root复核补全本轮来源；不重置用户脏改、不写Git。成本是快照归因而非干净提交/发行资格。
12. 真实存档语义/像素/真人/前台/硬件/发行：只保护hash，不读真实存档语义；NOTSEEN继续有效。成本是正式视听/相机/真人性能/发布均仍未验收。

## 下一步，不当作已实现

玩法：在首次J真正完成、尚差一击的同一Burst被退避丢弃前，用现有真实K尝试一次打断，再保原24冷却、其他所有威胁和完整J＋6＋2门寻求第二J；只用原900迭代，不另加45帧等待。现有生产 `ApplyThreadPull` 会撤销该owner攻击、模型有保存配置的`.80s` stun；这不是已证明K实际选中、LOS可达或能清除另一侧旧弹。若选到地形、被L/J取消或受伤，必须保留失败；不直接调弱敌人。

资源：已确认旧432/480/Ready实验没有“同一PackedScene仍持有时，Free actor→GC/finalizer→再Instantiate”这个顺序。可另立**测试专用、两进程**有/无同sourceProfile CLR强根的对照，先核source/instance原生ID相同且非local-to-scene，固定测试GC/终结阶段后再走原工厂；明确强根/JIT局部/延迟终结的干扰。此方案尚未编码/执行，不改旧前缀、不改生产GC/cache，不以A静默或B静默直接断言原post原因或宣布修复。

## 范围与保管

最终source清单SHA256 `223BFE4E13437B8865A0B86E9A526A7C7E2A05EE52D44CA96334C34798AAD259`；1844行、892 C#。canonical帮助器未改；helpers9 SHA `98124AB3FB8BE90A2DEC3ABBF9B871BEE0995BE073DD26D87C4C2B94896BC7E5`，post10 `3532E84386F19FB7350E36E722015D6F8A93761E80A7908B449892A12C303A3A`。最终bin清单 `C79D19CCBE996D1A41FB10CBC1E69ED086D94879F55D97D5383D8B049370B435`，Knight.dll `379B67E21C0D69C2A5B7B72B309249F87303A6FD9E571A45329735396A5AE490`。

root实际执行九组纯合同157/96/26/11/24/24/11/33/64全部exit0；这些没有启动native，不能算额外玩法或普通资源修复。

独立只读custody核验：10案50对source/helpers/user/continuation/binaries清单逐字节一致，60个OWN封存bin、4930份源输入副本及112份helper副本均匹配各案清单，缺失/额外/漂移0。最终live实枚举1546 runtime＋298 imported＝1844，C#892；对最终models零漂移，相对v100无增删且只有上述2测试文件变化，生产字节未改。最后六案从000107起共用最终source冻结，前四案为明确RED/中间冻结，不拼资格。最终9项helpers及post额外第10项与current匹配，Debug实际6文件与最终封存bin一致。

Git仍branch `codex/task6-certified-final-20260902T0555`、HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`；index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 未变。原音频bus仍CAD39DF…E4EC7A。实际Godot引擎/dotnet/MSBuild/VBCSCompiler均0；另有既有godot-ai attach进程，不属于本轮引擎/构建拥有者，未触碰。

所有root构建/native终态后才写本报告。用户“没看到”不追认；无前台、系统按键、付费生成、自动化、Git写入、打包、删除或真实存档语义读取。没有新媒体。一次正常汇报内容量检查UTC `2026-10-24T16:28:51.2599338Z`：Current860284/Base3508/Growth856776 bytes、Notify=false；未达20GiB，不创建任何调度器。

原始完整十一项目标保持ACTIVE，未complete/paused/blocked；没有task-done、finish、工作区清理或交付声明。
