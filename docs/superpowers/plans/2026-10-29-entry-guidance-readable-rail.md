# Entry guidance readable rail implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans。root实现/唯一编译原生owner，探索agent只读。复选框记录实际执行，不代表全关验收。

**Goal:** 修复实际入口机关教学遮挡角色和灯，保留完整指导及固定阅读位置。

**Architecture:** 真实saved-level/实际Combo测试先RED；现有WorldTargetMarkerVisual按已有expanded+context语义选择固定下阅读区，JSON只重排硬换行；同消费者GREEN与原回归。无需新运行时文件/场景。

**Tech Stack:** pinnedGodot4.7.1.NET/C#、JSON、既有canonical/pwsh。

**Spec:** docs/superpowers/specs/2026-10-29-entry-guidance-readable-rail-design.md

## Global Constraints

- G:/knight/Knight原dirtycodexbranch，1857/897库存；143/9/76目标与完整11项不改。
- 不移动真实入口玩家/关闭器件/隐藏教学/减少必要文字或改变按键/优先级/物理/恢复。
- 普通阅读列保留；器件阅读区固定，不依世界移动选位；测量实际完整文字，不固定裁剪高度。
- 原55/180/300秒和strict3sink门保留；源/helper/bin/input执行时冻结，失败原件不覆写。
- 无前台/OS/付费/collector/automation/Git/delete/真实存档语义；新GL必须新授权。

## Review Focus

- 真实优先级Recycling Combo不能被旧J/K假设或任意非空指导替换来绿。
- 实际Backdrop/头灯/胶囊使用同一final*canvas坐标，字形完整且不靠隐藏/截断/错误最小尺寸造绿。
- Context器件下阅读区和普通固定列隔离，世界运动不能使卡片跳动；长文仍完整。
- 中英四条器件正文、标题与重绑占位符保持，触发/优先级/器件状态未削弱。
- headless几何、原生strict回归、实际像素及用户看到/审美/性能是不同证据，失败不因局部GREEN消失。

### Task1: actual entry RED→GREEN

**Files:** tests/integration/ApprovedLanternRuntimeTests.cs；tests/integration/PrologueGuidanceRuntimeTests.cs；tests/integration/KeyboardTeachingRuntimeTests.cs；scripts/ui/WorldTargetMarkerVisual.cs；assets/localization/prologue-ui.json；canonical选定copy依赖。

**Interfaces:** 既有privateAssertInitialEntryReadability由RunBinding调用；真实Combo source为CanyonSpringboard/FixedGear和PrologueStrings。WorldTargetMarkerVisual.Present内部记已有expanded+context语义，ApplyPromptLayout择固定rail；无新publicAPI。

- [x] 仅测试先修过期前提：原位真实Combo/五语义/当前SPACE/D/未弹射/未完成LearnAttackMode；矩阵真实Backdrop和head/capsule/lamp完整避让。实际213344 native1/6581ms在真实遮挡RED，原件保留。
- [x] 然后实现固定器件下rail，普通720列不改；器件840宽/144预留/原文字测量。JSON四正文键×双语仅换硬换行，第五标题键不变。214035实际入口12状态GREEN，原binding/cleanup/fog通过；215844最终同源再验证，不等像素/真人。
- [ ] 原layout12组合、键盘/回收教学和formalHUD/model回归，预期copy/重绑/稳定锚点无损；实际任何错误均记录。

布局回归已执行，暂不勾通过：前三次保留旧J/K设置、缺move_up、旧SHIFT前提三个FAIL；唯一审查I1后单轮修正实际Combo+显式ordinary renderer probe。215526 native0/7624ms、ordinary12/device48/完整教学均实际通过，但退出2ObjectDB warning导致strictFAIL，不豁免。最终回收/formal/model均严格PASS；不抵销layoutFAIL。审查M1生命周期/no-slot与M2非identitycanvas延期，详见progress.md；不二审。

### Task2: current verification, optional pixels, handoff

**Files:** 现有验证入口、docs/README/backlog/workspace；若新授权，仅现有短GL夹具和媒体目录。

**Interfaces:** Task1普通source→既有currentpre/post/局部UI，只有actualGREEN证明本UI缺陷局部修正。Main/Explore旧FAIL不关闭。

- [x] 已运行普通models/pre/post，不延时/豁免。modelsPASS，pre实际第二J接受后过期观察FAIL，post223496ms/75+owner8/原4限定WARNING通过。无游戏行为变化不重复Main/Explore筛绿，其历史FAIL仍OPEN。
- [x] 核对显示权限：v158授权已使用且用户NOTSEEN；本轮未获新GL授权，未启动窗口/新截图，不虚构新版像素。可选实际展示仍待新授权，不计验收完成。
- [x] 一次fresh最终审查C0/I1/M2，I1一轮修正/Minor延期不二审；12案60清单对72bin/5700选定实体、当前1857源897CS/6bin/canonical/HEAD/index核对，原engine均终止。一次手动mediaCheck Notify=false；README/backlog/report/意见记录及全部Ruling已更新。局部几何有证据，layout/pre资格未通过、GL/真人未验，全目标保持ACTIVE。
