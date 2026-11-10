# v338 · 探索材料奖励与Q事务（制作中）

## 本轮实际推进

54个奖励从纯规划增加了真实runtime固定目录/领取账本/运行cache组件。场景只给RewardId，不能随便编辑授予量；五类累计获得由已领取ID派生，不保存会失配的第二余额。builder读取runtime目录，原ID和金额不变。当前无花费，未来装备/消费需另做事务，不宣称完整经济系统。

沿既有纯键盘Q：范围/LOS/站定/活体/暂停/动作优先级/同输入序号用原controller；cache再次要求当前接受的Q边沿和同一个候选。流程先detached候选→原子存盘，再无回调发布live/Save；最后更新表现后发成功信号。失败保持可重试，不先把箱开掉。领取物不QueueFree，支持新局/Continue从账本重新决定状态。

旧v4缺新字段仍能读且不改原件；显式坏字段拒绝，schema2/3迁移清空注入新字段。原九残片/min6、回血、装备、键位、物理/敌人/Boss保持。本轮没有向现运行地图部署54点；所保存叶场景只是隔离的开发占位候选，不是正式美术或新24/10关卡。

## 证据（不混同源码版本）

|case尾标|事实|
|---|---|
|models130627-3f610b5f|实际54vs0功能RED，原生538ms；构建通过|
|models130812-e6a658cb|Task1模型GREEN619ms，54固定数/五类literal总量/旧v4/坏账本/迁移/原models齐|
|resource-rewards131426-2fdea48f|首次测试4个Environment命名歧义编译错误，engine未运行，不计行为RED；只加System.Environment别名修正|
|resource-rewards131611-94e1450c|真实初始player站定、接受freshQ后未领奖功能RED3235ms；构建通过|
|resource-rewards131836-e84108cd|warm12309ms/cold479ms、均exit0/未超时、stderr/ERROR/WARNING0/guards0；下面各项真实完成|

所有原件位于 `G:/knight/验证证据/october13-stable-claw/` 对应完整case目录。

最终专项真实从保存主场景取得player，经Q逐点领54、同ID重复实体不能再领；实际坏磁盘目标不消费/原Save引用和源字节不变，heldQ不自动重试、freshQ成功；真实死亡保留、泛保存和空draft复制不丢、卸载remount/Continue只读、新局归零；成功回调立即重入New/queuefree无旧局回写。packed占位叶在case storage回载并由NodeAdded自动绑定，未手工BindRuntime规避接入。

独立cold writer42996/reader2364与owner原生contracts匹配，3份SHA见证（全领/新局/旧v4缺字段）只读；54点总量纤维9/矿质33/零件44/数据12/残响2。这里是获得材料数，不是100个敌位或54物品UI。

该版1935源清单SHA `4FEF5CA55143996A867AE5BD2E7F58F1DE97F3425AD8379ABB4838E285160F2A`；六产物清单SHA `E9488CC41B65B41C584FE5BF8CD74D36855C989E664D026E8D3687DECAABF1BA`。当前完整审查/原有交互存档及全项目回归仍在执行，不提前宣称同版全通过。

### 当前追加事实

单次fresh审查C0/I0/M1，15pins匹配；延期Minor是逐ID的固定v1授予快照尚未独立锁定，当前未发现金额漂移，正式发布/后续平衡编辑前需补保护。旧v337 authoring原文件没有独立完整副本，不能仅凭原SHA和本轮成功匹配补丁宣称逐字节历史核验。[所有未验边界与判断成本](../../.superpowers/sdd/2026-11-10-resource-rewards/review-result.md)。

独立证据审计复核before233源码＋5helpers/after233＋5共476份实际复制及6份binary字节，全部匹配；source1935、user109行（104文件＋5目录/根占位）、helpers9、binary6前后清单一致；8日志无诊断，三冷读原件SHA吻合。这是奖励专项保管，不扩大为全版本资格。

当前同版原有三项严格PASS：models132423-c9fca7bb633ms、formal-environment-interaction132453-defcfb5a9073ms、resident-save-compatibility132532-a01554fc11540ms＋独立cold1607ms。其余九项也已一次执行终态，原十一测试9PASS2FAIL；加奖励和旧Q专项为13案11PASS2FAIL。前44/后75严格通过，但不能关闭旧资源FATAL；Explore仍是实际HP2与原HP1入口前提冲突，Main仍原899帧下层Bell未清，两完整路线无最终JSON。未改规则/时限/HP或重跑筛绿，不填入旧版资格。

最终独立3354对快照/6708实际复制＋78实际产物SHA匹配；当前root重新枚举1935源/六产物仍同冻结版。现有场景/位图/物理/AI/输入未改；新奖励组件有实际运行与存盘证据，不等于新图已布点。[完整阶段表、保管及限制](../../.superpowers/sdd/2026-11-10-resource-rewards/verification-v001.md)。本轮未新增媒体，21,736,453B/185文件未到20GiB，无自动化或删除。所有测试进程已结束。

## 仍未完成

新24/10实体地图及54点实际布置/全部分路回返、正式图像/动作/UI资源界面、原资源FATAL根因/全原十一项/正常声画/硬件/真人验收继续OPEN。v337前段终结器FATAL及原Main/Explore失败均保留，本专项不根治、不重判。没有付费/GUI/OS输入/录像/自动化/Git/删除或真实档操作。

[设计](../superpowers/specs/2026-11-10-resource-rewards-design.md)、[计划](../superpowers/plans/2026-11-10-resource-rewards.md)、[逐项台账](../../.superpowers/sdd/2026-11-10-resource-rewards/progress.md)。
