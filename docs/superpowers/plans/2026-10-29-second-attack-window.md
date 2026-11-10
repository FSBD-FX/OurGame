# Second attack live-window evidence implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans。root实现与唯一原生执行；两个explorer只读，不作代码改动。

**Goal:** 让真实第二J的K权威/自动指导在实际攻击窗口取得可靠同次证据，同时保留迟到窗口拒绝及自然结束控制。

**Architecture:** 既有Run/Boundaries增加默认false的lateContinuation测试参数；既有读取控制phase追加同夹具的迟到反例。实际RED后，只在测试文件内用双优先级只读观察器保存同攻击窗口值；异步continuation验证值，不再要求早已完成的攻击现在仍活跃。布局资源未复现，不作猜测补丁。

**Tech Stack:** pinned Godot4.7.1.NET/C#、原canonical/pwsh、隔离存档。

**Spec:** docs/superpowers/specs/2026-10-29-second-attack-window-design.md

## Global Constraints

- G:/knight/Knight，1857源897C#，原143/9/76及完整1–11范围不改。
- 一次J、原3输入采样/Settle/30完成采样总量，不加预算/HP/攻击时长/强制状态/生产Refresh。
- callbacks只记录不可变值及错误，断言和输入在awaited夹具；完整同ID窗口/实际process指导必需，缺证据FAIL。
- 原180/300/55秒、所有raw严格门和既有四限定postWARNING范围保留，失败原件不覆写。
- 不开窗口、不发OS输入、不新增dump/付费/自动化/Git/删除/真实存档语义。

## Review Focus

- 起手事件在Tick之前，必须拒绝用起手回调冒充post-Combat窗口；Task2双观察器合同检验。
- 迟到/另一次/重置/未起手/零ID/非有限时钟不能混成同次证据；Task2同资格函数负控。
- process指导可能没在live期间更新；Task2要求启动后实际Guidance process见证，不读旧卡片或手动刷新。
- 回调异常、测试失败、移除观察器及撤自有键不能形成泄漏/引擎异常；Task2清理与原strict退出检查。
- late模式不能混presentation或扩大等待，默认既有调用语义保持；Task1/3实际入口及完整回归。

### Task1: deterministic real late-continuation RED

**Files:** tests/integration/DescentClawGuidanceRuntimeTests.cs；tests/RunStableClawPullTests.cs；canonical选定copy依赖。

**Interfaces:** Run(SceneTree,bool presentation=false,bool lateContinuation=false)默认不变，传给Boundaries；既有descent-j-observation-control先RunObservationReadControl再await Run(lateContinuation:true)。只在该控制中搬原30采样到旧live断言前，并验证自然已结束的模型事实；普通Run保持旧断言以获得真实RED。

- [x] 先写late模式/同ID实际自然完成前提及原phase追加，保留旧live断言。
- [x] 223223 native1/14861ms在真实第二J自然完成后的旧live断言严格RED；两次后来CS0136构建失败另行保留，不当行为RED。

### Task2: live-window scalar proof GREEN

**Files:** tests/integration/DescentClawGuidanceRuntimeTests.cs；canonical窗口回执。

**Interfaces:** 同文件test-only Node双回调优先级100，值记录中包含Clock/Start/Model/CanBeginGrapple及guidance/render标量。只有同攻击live且physics/process分别合格的样本可入选；ended快照由同资格函数拒绝。回调错误在continuation抛，observer附实际level并在finally撤回调/释放。

- [x] 构建第二J前armed的双只读观察器，正常与late共用验证，不新增第三J。
- [x] 真实自然结束拒绝/同ID计数/全部physics权限与post-guidance完整卡片，后续原爪/盾/落地等保留；审查I1/I2/I3以6反例两次实际RED→230625全部GREEN，不只验首行或吞晚错。
- [x] 控制phase230625 native0/16511ms、最终普通231056 native0/16008ms；原4状态read-control及14资格负控、6审查控制全部通过。两自有Node失败路径保留0，不关闭未知历史ObjectDB。

### Task3: current regression and bounded handoff

**Files:** 既有models/pre/post/layout入口；README/backlog/report/本workspace。

- [x] 最终普通models/pre/post、入口12状态与layout已运行，不扩预算。7阶段6PASS/1FAIL；pre30935ms原生Finalizer FATAL在新J之前，post212605ms/75＋owner8通过；不关闭历史资源/完整双路。
- [x] 唯一fresh最强审查C0/I3/M1；3项Important一轮6反例RED→GREEN，Minor延期不二审，set-aside及全部root裁决/成本记录到报告。
- [x] 18尝试88现存清单对/2缺bin对/96封存bin/8492选定实体和当前1857/897/6bin/canonical/HEAD/index/PID终态已核；手动媒体Notify=false，无新媒体。README/backlog/report更新，完整goalACTIVE。
- [ ] 整个计划的合并可靠性资格：pre FATAL仍未修，旧ObjectDB/全MainExplore/正式视听与硬件真人未验；不能task-done或正式交付。
