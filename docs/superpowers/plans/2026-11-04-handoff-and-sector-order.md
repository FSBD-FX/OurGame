# 换批输入与跨区清理观察 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans。Root唯一native；两独立测试文件可并行编辑，运行前必须全部冻结。

**Goal:** 取得换批最后时窗真实输入/支撑及跨sector Task10 key/节点/clear先后证据，再按确定来源修正，不能从旧日志猜根因。

**Architecture:** 两独立诊断规格、两个测试内有界记录器，无共享代码接口；共享root原生串行和最终source/binary绑定。不新增CS，SourceRows仍1888；旧生产与所有原等待/判据不变。

**Tech Stack:** G:/knight/Knight当前Godot/.NET/C#，dirty分支不Git写操作。

**Specs:** [换批](../specs/2026-11-04-gallery-handoff-observation-design.md)、[跨区](../specs/2026-11-04-vfx-sector-order-observation-design.md)。

## 2026-11-05续作状态（不要重跑已完成观察）

Task1/Task2的被动诊断、确证RED及局部控制/生产修补均已实际执行；唯一终审C0/I1/M1，I1一次真实故障注入RED→GREEN，Minor与继承覆盖延期，无二审。session30140已终态17案15PASS/2FAIL，完整矩阵和当前1888输入/六binary/85保护对绑定见[当前报告](../../production/2026-11-05-sector-feedback-and-neutral-handoff-v247.md)。旧的未勾步骤保留原流程历史，不表示应重复取得旧数据；shared ledger记录每个实际案。Task3/整个序章尚未complete，不commit/删除workspace。

下一工作：仅为HP1 Skitter最后恢复周期补同次J门/来源的有界观察（840–843/859–862缺口），不直接扩24/899/HP/阶段窗；匿名资源FATAL继续单独归因。U-20261105-01已选后续重审现有候选，停止扩搜旧批准源，不当作美术/动作已批准。完整原十一项/R-I/R-C/R-P及真实体验门继续ACTIVE/OPEN，不把本轮缩成替代总目标。

## Global Constraints

不改真实玩法/输入/数值/地图、原899/HP/55s/180s/300s或退出合同，不设置新GC/KeepAlive/等待/查询；只headless、root唯一native且CS/harness运行时冻结。无GUI/OS输入/付费/媒体/额外采集/真实存档/自动化/发布/删除，完整原十一项及R-I/R-C/R-P不关闭。

## Review Focus

- 诊断不能改变动作/帧数/危险查询，原断言与异常传播必须保留。
- 字符串有界，clipped/overflow/faults不隐藏；不能保存native引用或Dispose借用对象。
- 原loop终点与cleanup帧、raw样本与physical键、cached与fresh身份不得混同。
- VFX字典/实际节点/事件观察/真正clear generation分开；不能把audio count0当清理证明。
- 严格案例仍红或诊断失败必须保留，不筛绿/豁免资源或代签正常速度视听。

## Task 1 · 跨区顺序（worker独占PrologueAudioDirectorTests.cs）

- [ ] 原文件保全到本workspace的非覆写before.txt，按spec加32×8诊断，只改原方法/测试私有辅助，finally解绑。
- [ ] root原thruster-recovery-compatibility/1888实际运行；读原断言结果及有序cue/NodeAdded/key/clear/physics/process，不据单次绿称修复。
- [ ] 若根因已证，补确切行为RED与最小修正设计；否则记录证明上限与下一需要的数据，不猜改生产。

## Task 2 · 换批末段（worker独占PrologueCanyonGeneratedMainRouteTests.cs）

- [ ] 原文件保全，按spec加128条尾部被动记录；现有null-await前后与两terminal点不重排，旧监听不接管。
- [ ] root普通Main/1888/无新旗标实际运行，读输入序列、真实WASD、支撑、live身份及原899/cleanup各自范围。
- [ ] 只对确证的控制缺口做行为RED→最小修正，不能同时打开旧所有探针或扩时限。

## Task 3 · 当前状态收口（root）

- [ ] 一次独立增量审查，C/I一次TDD修补、Minor延期无二审；相关完整pre/post/模型/存档/键盘/Main/Explore按风险同版原门，不跳过旧失败。
- [ ] 核对当前1888输入/六binary与各案5组保护、普通媒体Check，更新README/总待办/用户意见/报告；原完整目标继续ACTIVE，未绿不称正式完成。

Self-review: 两spec无代码接口共享；不为未取得的数据预写修复结论。诊断是证据获取，不是“让原assert绿”的实现。

## Task 4 · 确证后的生产反馈边界与换批中立（续步）

Spec: [生产提交边界](../specs/2026-11-04-sector-feedback-commit-design.md)及换批spec追加。前两任务的诊断保留；不以首轮recovery绿认修复。

文件责任：Main worker只写MainRoute与自己的notes；VFX worker只写AudioDirectorTests、RunThrusterCoreTests及自己的notes；root写World、Audio、Seamless事务控制测试与共享ledger。无共享修改接口；root开始native前全部冻结。

- [ ] RED：Main原无actor await后HANDOFF_WAIT_NEUTRAL；recoverycompat先独立生产场景事务断言sector commit返回时旧key0，保留两原delegate与原receipt。root按原1888/普通无flag串行，必须读到精确新失败，而非compile error或旧失败遮挡。
- [ ] GREEN最小修正：Main退场/死亡/错owner目标不再驱动、等待松A/D，不改实际Motor；World真实成功commit通知，Audio绑定同步clear/baseline/seed，不动既有正常cue/爪/盾/能量。
- [ ] 控制：Audio同root不重复clear、新拒绝cue跨idle保留、退出旧owner不再响应；Seamless activation失败0通知、reentrant非递归/成功序列。若事件异常会妨碍后续清理，先写失败控制再最小隔离，不猜加通用框架。
- [ ] 一次新上下文终审，只针对v247增量；C/I一次TDD修补，Minor明确延期。完整pre/post、geometry、动作/模型/保存/键盘与Main/Explore原门同版验证，所有旧/新失败原样报告。最终逐项source/binary/保护绑定、媒体Check、当前文档同步，目标仍ACTIVE。

Plan续步自审：新接口只有World→Audio，消费方明确定义owner/root及同步baseline；Main控制与其无接口。事务夹具不声称正常输入路线，原900循环（终点899）预算不改。保留dirty分支/不commit或清理workspace的用户边界高于skill模板。
