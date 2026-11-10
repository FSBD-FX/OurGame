# 当前敌人分配与正式族系目录缺口 · 只读核对

2026-11-03，仅G:/knight/Knight。正常保存父场景ResidentContent=1，Level读取正式固定布局并交manager；当前定义为26旧遭遇名额＋50常驻＝76普通敌人。50行JSON摘要与绑定一致，24个开发叶场景存在。此为静态接线/配置核对，本轮没有重跑自然世界census或实战，不把定义数当同时活跃数。

| 区域 | 旧＋新＝总数 | 地／远／空 | 新骨骸／新幽灵 |
| --- | ---: | ---: | ---: |
| 入口低顶桥 | 1＋3＝4 | 3／1／0 | 0／0 |
| 错层裂谷 | 1＋5＝6 | 4／1／1 | 0／0 |
| 可控深渊 | 3＋5＝8 | 1／3／4 | 0／1 |
| 档案侧洞 | 2＋5＝7 | 5／1／1 | 4／1 |
| 冰冻绕路 | 4＋6＝10 | 4／1／5 | 1／5 |
| 齿轮活塞 | 2＋7＝9 | 7／1／1 | 0／0 |
| 蒸汽阀道 | 3＋6＝9 | 5／3／1 | 1／0 |
| 配重升降井 | 3＋5＝8 | 3／2／3 | 1／1 |
| 钟摆廊 | 3＋4＝7 | 4／2／1 | 2／0 |
| 回声横廊 | 2＋3＝5 | 3／0／2 | 1／2 |
| 封闭庭院 | 2＋1＝3 | 2／1／0 | 0／0 |
| 合计 | 26＋50＝76 | 41／16／19 | 10／10 |

营地/Boss厅/安全出口普通敌人为0，Boss本体与ECHO/NPC另计，本次未重复审计其完整分配。地＝游荡/冲锋/重装，空＝巡航/俯冲，按主要职能互斥。六职能合计为游荡23、冲锋9、巡航8、俯冲11、远程16、重装9；主路50、支路26。新50的代码族系为原生8/废弃12/精工10/骨骸10/幽灵10；旧敌按目标世界观归族不等于其旧代码已有统一族系标签。

## 依据与不能冒认的部分

- 保存选择：[PrologueLevel.tscn](../../scenes/prologue/PrologueLevel.tscn)；接线：[LevelController](../../scripts/prologue/PrologueLevelController.cs)；[固定布局](../../scripts/prologue/residents/ResidentAuthoredLayout.cs)。
- [旧26](../../tools/build/prologue/CanyonEncounterCatalog.cs)、[新增50](../../tools/build/prologue/CanyonResidentCatalog.cs)、[批准最终分配表](2026-10-16-prologue-population-final-plan-v002.md)、[独立字面量合同](../../tests/unit/CanyonResidentCatalogTests.cs)。
- **不是只差视觉验收：** [ResidentSceneRegistry](../../scripts/prologue/residents/ResidentSceneRegistry.cs)正常入口仍显式使用ExplicitPlaceholder开发叶，FinalOnly目录未实现。骨骸弱点/幽灵虚实相位已有[配置行为合同](../../scripts/prologue/residents/ResidentSubtypeProfile.cs)，但正式族系资产、场景目录与其生命周期/实战/视听验收均未完成。不能复制占位并改名FinalOnly来关项。
- [Joint census](../../tests/integration/CanyonResidentPlacementTests.Joint.cs)限定无目标/未测攻击/非正常世界；[自动流送夹具](../../tests/integration/ResidentAutomaticSpawnTests.cs)用检查点切区。它们不能替代自然全关实战，也不证明画面密度、种类观感或资源稳定。

结论：76配置数与点位接线未发现缺口；原V05/E类正式内容仍OPEN。接下来应按正式资产/族系目录及自然战斗/分布体验分别推进，不把数量已配齐当丰富度已合格。无源修改、游戏启动或其他工程操作。
