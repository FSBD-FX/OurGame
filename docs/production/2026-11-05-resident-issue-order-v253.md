# v253 · 巡逻几何诊断顺序与当前严格回归

状态：局部修复已取得真实 RED→GREEN，本轮同源15阶段 **13 PASS / 2 FAIL**，不能称完整序章通过。唯一工程 G:/knight/Knight；原十一项、正式动作/美术、R-I/R-C/R-P 和真人视听验收继续 ACTIVE/OPEN。

## 本轮实际改变

仅 `ResidentPlacementGeometry.cs` 的两个结果构造器规范原因快照：Ordinal 排序，Body 保留原重复次数，Path 保留原 ordinal Distinct。未排序路径段/支撑/原生命中，未改变任何查询、碰撞、生成准入、力学、伤害或输入。

生产消费者只依赖 Count/Any/All；字符串顺序用于诊断，不控制行为。实际 native 查询返回 body/hurtbox 先后不稳定，是 v252 两个跨态 SequenceEqual 的直接原因。保留原断言而在值边界修正，不通过测试端排序或删原因筛绿。

新增 `ResidentGeometryIssueOrderTests` 由原 geometry 入口实际调用：六种排列、body 重复/path 去重、两文化字面量顺序、输入变动/只读、空/单原因、几何/支撑/巡逻段顺序。旧实现真实失败于 body 顺序，修正后通过。原208/20、27条Expect和真实wall加入/移除保持不变。

## RED / GREEN 与不能改写的过程

- `resident-geometry-20261105-140744-815-c690b997dd9f44bb8595eff41d00ab06`：新值测试真实 RED；编译成功，尚未跑 native fixture。
- `resident-geometry-20261105-140906-321-2839692fa8e243fca37af7f40d85e92f`：新值测试和原 native208/20已 GREEN，engine退出0；严格宿主仍 FAIL，因为过期 raw_terrain143 匹配拒绝当前157，不能事后改写为 PASS。
- 实际消费者纯宿主控制34项：旧宿主产生四个预期偏差（拒绝当前157、误收143）；最小修正后34/34通过。唯一当前fixture157，63环境/91区域及原57交互/110总区域不放宽，新增新值/208×20完成回执要求。旧143/144、未知158、混合/重复、缺项和旧inventory继续拒绝。原外部ERROR/FATAL/泄漏 veto 和预算不动。
- `resident-geometry-20261105-141540-307-9ee46324d15147c5ad8f0efa440551fd`：完整严格 PASS，native30459ms，原55s预算，非固定fps/非movie/非OS输入。

三态 fresh=batch 的 body、各段bounds/issues/flags/transform完全一致；恢复后逐段与原状态一致。加墙只在两中间方向新增各一个 `corridor_native_world`，body和其余段保持，故未掩盖真实阻挡。3行/0faults/addedTrue/removedFalse。不能因此宣称所有未来动态运动或全部居民已安全。

## 当前同版清单

完整14阶段批66863终态exit1，前段和Main两项FAIL；另有当前geometry，共15阶段13PASS/2FAIL。没有重跑筛绿、增加预算或改899。v252匿名终结器FATAL及历史RID泄漏不因本轮局部PASS关闭。

当前前段已经严格FAIL：native140459ms，虽执行44项并engine退出0，仍有7条 `Handle is not initialized`。7条堆栈逐字相同，均发生在首次 BasinSupply.OpenSample 的同一次同步 AddChild；现有标量记录显示同一新根/flow成功进入树，直接源码未证 Dispose借用资源或挂载已释放节点，不能猜成缓存/GC根因。另见配置失败时新根清理覆盖缺口，但并不解释这次成功挂载的7条错误；仅记后续核验点，未实施修复。当前后段和连续熔炉aggregate已严格PASS，不能冲销前段或旧FATAL。

Main native153972ms仍在原摆锤回廊899 FAIL，玩家HP2、能量不足，不能把终端缓存的旧actorHP1当成实际剩余敌人血量；未改难度或循环上限。Explore native314681ms通过正常plan，从标题经Boss/实体支撑出口、恢复0/0，23路节点/11挑战种类，11激活/10击败，不遍历新附室或全部76只，且operationCoverageComplete/human/fullGoal仍false。

| 本轮阶段 | case时间/短ID | native毫秒 | 严格结果 |
|---|---|---:|---|
| 原geometry＋新原因值控制 | 141540-307-9ee46324 | 30459 | PASS |
| 完整存档暖/冷 | 141729-914-8072d3e3 | 12623 / 冷1613 | PASS，writer75404≠reader72252 |
| 组合机关 | 141822-739-3d4ea3a6 | 25675 | PASS |
| V2结构/33反控 | 141923-495-4e0e3987 | 1501 | PASS |
| 居民配置分配 | 142001-825-0a4ee4a8 | 648 | PASS，非全部实战 |
| 居民状态模型 | 142037-304-469fe654 | 552 | PASS |
| 居民实际巡逻 | 142113-363-527d655f | 45479 | PASS |
| 爪模型子集 | 142234-224-bf4be739 | 566 | PASS，非所有模型 |
| 键盘 | 142311-108-feddb0df | 20225 | PASS |
| 动作/技能技术 | 142416-027-05071f42 | 9527 | PASS，非美感认可 |
| 完整普通前段 | 142459-880-120783cf | 140459 | FAIL：7 Handle错误 |
| 完整普通后段 | 142754-647-e2d1a07b | 179037 | PASS，现行原300s预算 |
| 连续熔炉aggregate | 143128-548-9cc87790 | 34405 | PASS，不关闭旧FATAL |
| 普通Main | 143241-083-bfeb7e11 | 153972 | FAIL：原899 |
| 普通Explore | 143555-210-3e09d9b6 | 314681 | PASS：Boss/支撑出口/恢复0 |

完整case均在 G:/knight/验证证据/october13-stable-claw/，按phase+20261105+完整时间+UUID保留。75对source/helper/user/continuation/binary前后文本0差异，15案source与binary manifests各唯一1。当前1897个源/导入输入及完整文件集、6binary、14helper独立重哈希0差异。真实存档只做保护哈希，不作实验。所有本轮owned build/native已终态，不重启旧handle。

## 资源、审阅与授权

唯一fresh审查C0/I0/M1，允许保留本有界增量而不认定游戏Ready。M1延期：新增成功回执的Contains对前缀/后缀/重复不够严格，未来收紧大小写敏感整行唯一并补3反控；未见当前生产者异常或真实失败被掩盖。无C/I修补、无二审；全部11项declined及root逐项裁决/代价、执行裁决详见[终审与完整裁决](../../.superpowers/sdd/2026-11-05-resident-issue-order-v253/final-review-v253.md)。历史Minor仍保留，不借机扩改其他phase143合同或GC/cache/KeepAlive。

无新正式美术/动作资源。用户U-20261105-01选择暂找不到V1原图、后续重审现有稿：停止扩大恢复搜索，现有稿不承接旧批准，三段误给视频仍作废。正常速度的动作设计、重量/惯性、物理手感与正式身体/披风仍未验收。

before、计划、裁决及本轮完整证据指针见 `.superpowers/sdd/2026-11-05-resident-issue-order-v253/`。不删旧失败、不回退脏树、不提交/发布。仅 headless 与隔离存储；无GUI/OS输入/付费/新媒体/新dump/自动化/真实存档实验或删除。

正常汇报单次媒体Check：2026-11-05T06:48:16.5804790Z成功，7,427,750bytes/86文件/Notify=false，无新增20GiB档位；不新建自动化或因容量提示停工。
