# Entrance Recessed Depth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root executes; agents read-only. Steps use checkbox tracking.

**Goal:** 在实际入口背景加入低噪岩土纵深和埋藏机械遗构，不改变地形权限。
**Architecture:** 只读静态polygon计划由现有实际_Draw消费；真实builder/packed消费者及挂树保护检查复用entrance-rock-roles phase，不新建渲染/美术工具链。
**Tech Stack:** Godot4.7.1.NET/C#，现有canonical/headless，原生2D。
**Spec:** docs/superpowers/specs/2026-10-30-entrance-recessed-depth-design.md

## Global Constraints

- G:/knight/Knight；完整原十一项ACTIVE，root唯一写入/构建/native owner。
- 不改碰撞/抓面/ReadableEdge/通路/数值/存档，不触碰其他项目；保留四旧背景及非入口旧分支。
- 无付费/新显示/采集/OS输入/自动化/Git/删除；所有native运行期间源/helper冻结。
- 原focused55s/pre180s/post300s不扩大，所有失败原件保留。

## Review Focus

1. 改到未调用的旧美术方法：实际DrawEntranceNaturalApproach消费计划，核对实际saved consumer。
2. 顶点均在L内但边穿凹角：独立ClipPolygons面积余量＋跨角负控，不只点包含。
3. 细节像平台/锚点：y608..636保护带、新层低alpha/冷暗非发光/12polygon预算，正常像素验收保持OPEN。
4. 简单正面积仍自交：非邻边相交、重复/NaN/零面积与独立五角星负控。
5. 测试以Draw信号冒充提交：仅标绘制请求；真实_draw代码审查，不证明像素和混合后观感。

### Task 1: 实际入口低频背景及结构合同

Files: 新scripts/presentation/CanyonBackdropVisual.EntranceDepth.cs、tests/integration/EntranceRecessedSceneryRuntimeTests.cs；修改CanyonBackdropVisual.Entrance.cs、RunStableClawPullTests.cs、canonical仅选定输入和marker。
Interfaces: BuildEntranceDepthPaintPlan():IReadOnlyList<CanyonEntranceDepthPaintCommand>；Role:string、Points:IReadOnlyList<Vector2>、Colors:IReadOnlyList<Color>。测试Run(SceneTree):Task。

- [x] 写实际builder/packed消费者反射合同（旧代码缺新plan/native内容必须RED），并跑focused。
- [x] 实现12polygon只读计划与当前_Draw消费，保留旧四层未staging路径。
- [x] 测试点/色/层序/简单性/三角面积/L containment/保护带及13个几何字面控制；差异大小/偏移、其他sector/role隔离。
- [x] 真保存叶挂树有限观察Draw请求，所选静态物理/ReadableEdge指纹不变；不声称提交/像素验收。
- [ ] focused GREEN＋模型/原生前后段各一次，失败照录；唯一fresh最终审查，重要项一次TDD修正，不二审。
- [ ] README/backlog/资产表记录原生替换层与未验收状态，关键媒体无许可不采集，正常汇报前20GiB手工检查。

指定dirty root及现有基线继续，不提交/清理通用skill目录。此计划技术结构可验证，但正式美术完成仍以正常游戏画面与用户认可为门，不能以数据合同替代。
