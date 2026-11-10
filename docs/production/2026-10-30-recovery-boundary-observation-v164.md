# v164 · 恢复事件边界观察（调查中，不是修复交付）

后续v166已实现测试消费者的真实完成等待与失败记录，见[完整事实和仍失败的回归](2026-10-30-recovery-completion-consumer-v166.md)。下文保留当时调查事实，不追改旧失败；新最终pre仍资源FATAL，不能据局部修正宣称生产恢复或整关完成。

## 事实

工程仍为 `G:/knight/Knight`。完整原十一项目标ACTIVE；本次用户对旧自动关闭窗口仍回答“没有看到游戏画面”。无新窗口、系统键鼠、录屏、付费生成、崩溃采集、自动化、Git写入、资料删除或真实存档语义访问。

本调查承接v163前段 `RecoveryCompleted expected1 actual0`，没有修改生产恢复、居民生成、数值、地图或等待上限。先直接运行原完整FirstCombat类，再仅在其原等待边界加入有界只读标量观察；原请求、两次精确完成计数、同角色/同区域等断言保留。观察回调不新增await，最多32行，回调自身错误单独记录。canonical补选恢复相关依赖实体，不扩大原55/180/300秒上限，不使用失败豁免。

| 本轮实际运行 | 构建 | 原生 | 结果与边界 |
| --- | --- | --- | --- |
| `first-combat-20261030-033749-004-fcb3d37fc5f84646865db9fa0bb16c84` | exit0 / 39907ms | exit0 / 8808ms | 无本次观察代码的原完整独立类通过；不覆盖旧组合失败 |
| `prologue-pre-20261030-034324-341-de7dd77192f244048f0d534257712230` | exit0 / 29845ms | exit0 / 170379ms | 带被动观察的完整pre通过；未超180秒，strict guard为空；不是最终同源全套回归 |

两案位于 `G:/knight/验证证据/october13-stable-claw/`。各自源码版本不同，不拼成同源整套资格。

实际pre记录20/32行、观察错误0、`original_waits=true extra_waits=0`：

- 第一次请求物理帧2325，第二次请求2328；Requested各同步出现一次，流程进入Recovering，Pending为真、Prepared为假、Failure为空。
- 各自第二次原PhysicsFrame返回时（2327、2330）完成计数仍分别为0、1，仍处于上述有效准备状态。
- 后续真实Completed各在同一物理帧号内出现，计数变为1、2，流程Playing、Pending为假、Prepared为真、Failure为空。
- 原三个ProcessFrame结束后，原精确计数和同区域等断言通过；完整FirstCombat类和完整pre随后通过。

这不是新增回归修复的RED→GREEN，未宣称观察代码治好了生产恢复。当前代码是诊断中状态，尚未执行此切片的最终代码审查或最终models/post/Main/Explore回归。

## 推理

源码和当次读数共同说明“经过两个PhysicsFrame信号”不等于“居民准备已提交”。现有独立恢复消费者本来就用真实Completed和有限180物理边界，不能用输入采样等待替代：恢复期间输入会被禁用。

但旧v163失败没有这些状态读数，本次日志也可能改变调度。因此不能断言旧失败一定只是早读，不能把本次pre通过当作生产恢复修复，不能关闭历史资源句柄/终结器问题。

## 风险、未完成项与下一步

1. 保留原始v163失败和本次两案，不补造旧案状态或缺失实体。
2. 若继续修正测试消费者，先验证请求未接受、准备失败/所有权丢失等拒绝条件，再按真实完成事件等待；保留精确事件、同角色/同区域/HP和原phase预算。不得延长时间凑绿或手发完成事件。
3. 若出现真实恢复拒绝/取消/失败，应先定位该生产分支；目前没有证据授权猜改恢复语义。
4. v163完整Main能量耗尽战死恢复、Explore Grip9活Ghost Hurtbox阻挡、正式视听/UI/动作和真人硬件验收仍OPEN，不能因本轮两次PASS关闭。
5. 显示核对保持NOTSEEN。现有短时probe不适合用户显示确认；常驻隔离标题入口仍待准备，当次显示须另获许可，不能自动弹窗。

当前计划、诊断源码和未完成门见 `.superpowers/sdd/2026-10-30-recovery-event-boundary/progress.md`；这不是完整十一项或此修复计划已完成的声明。

## 保管收尾（不等于代码或玩法验收）

后续v165前段 `prologue-pre-20261030-042617-243-9862af9d43134502beb93e7072afefdf` 原生99650ms/exit1再次出现原完成计数expected1 actual0。这次保留了失败时点的被动观察：after-settle物理2511、请求1/完成0、Recovering/Pending=true、Prepared=false、Failure/Rejected为空、同Player/HP5，9行/fault0/extra_waits0。它补强了“实际失败断言早于准备提交”的证据，但不证明若不清理就一定会成功，更不替旧v163案补造状态。尚未实施按真实完成事件等待的消费者修正，不能称恢复已修好。

只读保管核对：两案共10组source/helpers/continuation/user/binaries前后清单对，案内零差；938份选定源实体、22份helper实体及12份封存bin逐件SHA匹配，未列实体0。不是1860源逐案全实体备份。两案所有stdout/stderr/external未检出ERROR/WARNING/FATAL/Exception，stderr为空；四个自有build/engine PID55216、64888、43308、28140已不存在，不触碰或归因旧8760/bridge。当前1860源、10项helper、6bin匹配后一案。前后两案仅FirstCombat诊断源码不同，仍分开记账。

HEAD、branch和index SHA保持原基线，没有Git写入。正常汇报前一次手动容量Check：2026-10-29T19:57:11.4605346Z，7,427,750 bytes／86文件、Notify=false；没有新增过程媒体、重置基线或创建调度任务。
