# v250 Patrol World State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans。Census观察单文件worker，root独占runner/host和所有build/native，修改全部冻结才运行；一次fresh终审，C/I一次TDD修补、Minor延期。保留原地dirty，无Git/清理。

**Goal:** 以真实三个world状态解释并修复已证的几何缺口，不放宽安全要求，推进完整原序章。

**Architecture:** 调用原私有巡逻全控制的独立入口；已有纯值结果有界观察＋真实wall ID前提。取证后才条件修补。

**Tech Stack:** G:/knight/Knight 当前Godot/.NET/C#，1888输入。

**Spec:** [v250](../specs/2026-11-05-patrol-world-state-v250-design.md)。Workspace `.superpowers/sdd/2026-11-05-patrol-world-state-v250`。

## Global Constraints

原checker/全部断言、位置、3×Sync/各时限、24/899/HP/能量/单跳不改；普通headless、无GC/KeepAlive/cache/引擎/dump/GUI/付费/媒体/自动化/真实存档/Git/删除。root唯一native、运行时冻结CS/host。完整原十一项/R-I/R-C/R-P/正式美术动作仍OPEN，T01只读覆盖独立，不以小缝代大型。

## Review Focus

- 原完整控制真的被调用，不复制checker、跳过失败或以metadata假装native。
- 有序issues/corridors/flags只消费已有fresh/batch值，3行上限、fault/缺行明确，不重复查询或改变原短路门。
- wall ID为同一真实对象；Add/Free后原Sync，两个新增HasNativeHit仅前提，禁止把它当生产结果期望。
- 不吞异常，主因与fixture清理错误都保留；不Dispose借用shape/profile，原NativeFixture不是新版全寿命安全保证。
- 条件修补须独立正负例仍拒绝危险遗漏/错误bounds/flags/段序；无依据不能把SequenceEqual改弱以取绿。

## Task1 · 初始观察（Census worker＋root入口）

Files：worker `tests/integration/CanyonResidentPlacementTests.cs`＋workspace `census-worker.md`/exact before；root `tests/RunThrusterCoreTests.cs`、原host。

- [x] 保存before Census86F96E94…0FE2/runner511C44BF…9B18/host2E563183…FB6D，不覆盖v249快照。
- [x] worker实现`public static Task RunPatrolWorldWindow(SceneTree tree)`及3组原Compare的纯值JSON、2壁面实际前提，保留所有原判定。
- [x] root新phase路由与exact final约束，普通55s/1888；freeze后一次`verify-claw-knight-v001.ps1 -Phase thruster-patrol-world-window -SourceRows 1888`，Expected：原断言若触发仍FAIL且已留真值，不是强求PASS。
- [x] 若隔离PASS而缺原失败数据，只追加一次原`resident-geometry`同记录；未证则记录边界不猜。

## Task2 · 依真实RED条件修补（root）

- [x] 逐项核对native wall、同态fresh/batch、跨态成员/bounds/flags/段序；列清实际哪一项不同。
- [ ] 若仅跨态排列，先写独立字面正负控制，确认原有序比较RED；再只更改两跨态比较为成员契约且原每段顺序/所有安全理由保留。若不是，只根据实测定位fixture/生产，另记最小修补，不并改其他规则。
- [ ] 重跑scoped与原geometry，失败原样保留；匿名FATAL不能因一次安静关闭。

Task2条件未成立：scoped有序结果一致，唯一原geometry在观察点前native FATAL，旧失败数组仍未知。后两项不进入；不是漏做修补或允许另跑筛绿，语义缺口继续OPEN。

## Task3 · 审查与当前证据

- [x] 一次fresh增量审查，C/I一次实际TDD修补、Minor及所有declined裁决记录，无二审。
- [x] 相关模型/保存/键盘及必要前后段、正常双线/完整当前技术门按实际修改风险执行；未执行不得称已验，任何旧失败不隐藏。
- [x] 当前源/binary/helper/保护清单与原始收据绑定，媒体正常汇报Check、README/原待办/反馈/报告更新，不markgoalcomplete。

Task3范围：C0/I0/M1，Minor延期；六案5PASS/1FAIL，含四相关门4PASS；没有执行本版正常双线或完整前后段，不把Task3勾选解释成整套绿。全部结果与裁决见production/2026-11-05-patrol-world-state-v250.md及本workspace。

Self-review：Census只产数据/入口，runner消费确切签名无共享写接口；root统一native。spec要求真实取证才修，不能把仅一个PASS说成游戏完成。T01无安全闲置矩形已证，后续须专门空间设计，不在本轮偷改安全深井。
