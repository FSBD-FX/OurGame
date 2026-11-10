# 配重验路回充与几何计时 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root唯一native执行；仅独立计时文件委派worker，所有native运行期间源码冻结。

**Goal:** 保留既有玩法/时限，修正默认自动验路漏用安全回充，查明geometry原55秒消耗阶段。

**Architecture:** 两个独立测试驱动任务，源输入仍1888。Task1仅提取/选择既有松盾安全策略，Task2只旁观时间；不以任何一项的局部通过缩减完整序章。

**Tech Stack:** 当前Godot/.NET/C#；G:/knight/Knight现有dirty分支，不建worktree或提交。

**Specs:** [回充路由](../specs/2026-11-04-counterweight-recharge-routing-design.md)、[几何阶段计时](../specs/2026-11-04-resident-geometry-stage-timing-design.md)。

## Global Constraints

共享完整攻击＋八拍/24储备及原所有松盾安全判据、人数/HP/899/55s/180s/300s与fresh检查不变；无游戏数值/正式素材/引擎/真实存档变更。仅headless；root唯一native、源码冻结；无GUI/付费/新媒体/额外原生采集/自动化/发布/删除。

## Review Focus

- 新normal路由不能使非早期定位/其他地图开回充：字面量8组合及实际normal/explicit回执。
- 默认使原安全条件实际消费，不能仅变布尔标签：真实松盾、能量变化、攻击/HP/三弹记账及899结果。
- 未测来源/即将发射/旧采样/静止与支撑失效仍必须拒绝：原来源反例及原guard链保留，逐差检查。
- 标记对照与默认主线不能混作同版验收：独立目录、explicit开关分别保留，默认不继承实验变量。
- geometry计时不得重排、漏掉原控制/改变JSONL/等待：仅调用前后clock，原严格门运行且失败照实记录。

### Task 1: 标记对照及默认回充路由

**Files:** root拥有tests/integration/PrologueCanyonGeneratedMainRouteTests.cs、tests/unit/RangedWeaponModelTests.cs。

**Interfaces:** internal static bool Task10PostHopRechargeRoute(bool earlyPositioning,bool normalPlan,bool explicitProbe)，原guard判据保持。

追加接口：internal static bool Task10LiftGhostGuardRoute(bool keyboard,bool normalPlan,bool explicitProbe)；原Ride私有方法显式接normalPlan，护盾源/预警/接触/支撑/24拍与零伤害不变。

- [x] 当前未改源码，单独KNIGHT_POSTHOP_RECHARGE_PROBE=1进行同899/HP2对照；不计默认验收，按实际结果判断是否继续。
- [x] 因单标记已清配重但lift7151损血，追加两旧开关的同版标记对照，验证已存在向下护盾；未实测前不启用默认lift选择。
- [x] 仅机械提取旧early&&explicit函数并复用原三消费点，单元8字面量组合要求默认early+normal成立；实际ranged-models应以缺少normal路由RED，不把编译失败当RED。
- [x] 修改函数为early&&(normal||explicit)，保持完整原guard/fresh/24门，原三调用只改清楚变量名；实际模型GREEN。
- [x] 若lift标记有效，同一轮将keyboard&&(normal||explicit)接原Ride；两个selector共同8字面量组合先RED后GREEN，原lift检查完整保留。
- [x] 无实验开关正常Main及Explore、完整pre/post/存档/键盘/原models、Task2几何严格回归，同源绑定，不筛绿/扩时。实际九门6PASS/3FAIL，不据“已运行”认定完整主线通过或Task1全合同完成。

### Task 2: 原几何55秒被动时钟

**Files:** worker独占tests/integration/CanyonResidentPlacementTests.cs，禁止其他写/构建/引擎。

**Interfaces:** Run边界累计Stopwatch诊断＋私有leafSource记录数只读访问。无生产/Task1接口，唯一共享为源码冻结和rootnative。

- [x] root确认2112标记native结束后才写；按计时spec增加无pass语义的回执，不改变逻辑/顺序/原输出。
- [x] root在原55秒门实际运行，查看完整时钟/JSONL终态及原错误；不要仅grep源或以末回执作根因。
- [x] 一次独立most-capable只读审查整个本轮增量，C/I一次RED→GREEN修补，Minor延期无二审。
- [x] 对照当前源/binary/真实用户保护和普通媒体Check，更新README/稳定待办/反馈与最终报告；保留全部失败。45对保护文本及当前1888/六binary差异0，媒体NotifyFalse；原十一项及用户三条体验返工仍OPEN。

Self-review: 两spec边界均有上述执行/校验；Task1是否可提升由实测而非预先结论决定。Task2是证据获取不是冒称优化。原十一项/R-I/R-C/R-P/正式美术、正常速度与真人保持OPEN。
