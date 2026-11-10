# v329 · 主线落脚发布时钟取证

## 事实、判断、风险

上一轮v328是实质进展：空中Up动作原生反例后修补通过，最终11阶段10PASS/1MainFAIL及完整身份审计留存。本轮继续原十一项目标，不把诊断代替正式美术、体验或资源根因工作。

v328 Main缺相邻physics453→455；旧正常路线Input/Motor连续链已经覆盖454，但没有旧453鞋底/idle处理状态。只读追踪确认FramePublished来自Coordinator._Process的Formal.Apply之后，非每物理tick。假设是呈现与物理时钟不同；具体旧454仍不能追溯确定，也不能声称脚步已错/已正确。

本轮仅在`tests/integration/PrologueCanyonGeneratedMainRouteTests.GroundSeam.cs`加被动诊断：原窗口128条标量环形账本，I=节点处理前SceneTree.ProcessFrame、P=正式Formal应用后的发布；计publication/process/physics/inPhysics/time、原body位置和processing/canProcess/入树/paused/flow。原handoff-before/after鞋底回执移到原相邻断言之前。**所有旧相邻、stance、.15世界鞋底、18+18骨长、身份、原路线及最终失败门不变**，不主动刷新或制造缺失帧。

唯一代码增量SHA `85397241D86090A9695CF45E5CFFD167BB8171FE487CE11A39A5DAE449ACD69C`，精确v328基线`204DD790FD7972429952C50E400737029A7C84FE6FBC996706A054E4E74FE3AC`。canonical/生产/资源/地图不变。当前诊断输入1916清单SHA `BB73E836A39E62039907DDC4486BCF88A1307E4192A0D2C9AB466659108C2E52`、六产物清单`B2F2D3FF9A290262136D9A3B1660DB932289B203A5F208E622717392C0695D45`；测试程序集已变化，不借v328其他10阶段为本诊断产物签整版。

## 唯一一次新观测运行

普通canonical仅`-Phase thruster-route-main-diagnostic -SourceRows1916`，原600000ms cap。案例位于：

`G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261108-200014-875-015c20f703c24b2d91e113d8080f72f5/`

构建0/27042ms；原生0/250889ms、无超时、failure=null、guards空、stderr0、ERROR/WARNING0，Main严格PASS。owned session8002与engine47048已终结，不再轮询/重启。新Main整线JSON真实存在：原标题→同player→Boss死亡→实体支撑出口Completed，恢复0/0、encounter8/8、21普通跳均grounded，14242个实际Input/Motor连续样本tick31–14272，unsupported_edges2；completed count/events各1、held60位置/body/save保持，物理停用后velocity仍260而非速度已归零。实际save与terminal/held SHA均`0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517`。输入来源为引擎合成键盘事件，虽内部receipt名physical_keys=true，OSOriginProved=false，不称真实硬件键盘。

独立本案审计：300对输入副本差异0、6/6封存二进制实际匹配；当前rg+imported真实1916项/当前6产物/真实用户109及9helper与终案一致，canonical仍`1712937B83844D0A2419A92F30AB8D5A6A3D05A8B38CF4CE5A61DBEA0AB0059E`。该案原600000ms cap、headless1280×720、keyboard-route，无fixedFPS/movie/verbose/特殊flags。stdout/external clock回执129行逐行差异0（1pair+128samples=64I+64P）。

新测试程序集虽仅诊断变化，仍不借v328十项拼整版；root冻结同BB73/B2F2输入产物，owned52790按原普通cap执行其余10阶段各一次及独立cold，已真实终结exit0，无阶段失败。Main没有重复，最终十一阶段全为本版严格PASS，不把旧v328Main失败取消。

## 同版最终资格

所有案例均在`G:/knight/验证证据/october13-stable-claw/`，原cap及严格规则未改。每阶段只跑一次，非挑选重复成功；同版源/产物绑定上文完整SHA。

| phase | 原生ms | case |
|---|---:|---|
| Main | 250889 | `thruster-route-main-diagnostic-20261108-200014-875-015c20f703c24b2d91e113d8080f72f5` |
| rig | 38534 | `thruster-rig-compatibility-20261108-200809-623-7998219f0602484088b7e9289bdc2fef` |
| presentation | 18472 | `thruster-presentation-rig-20261108-200920-831-83799a9b9ae74548bd0a2370190a73a4` |
| gait | 29851 | `thruster-ground-gait-20261108-201010-508-11f7ee6da26148948720ab027a4abf83` |
| recovery | 14763 | `thruster-recovery-compatibility-20261108-201115-477-11f5846745784fd3bc0d9c0d2448b6c8` |
| actions | 10762 | `actions-20261108-201201-113-a32f9dcc6c234fe19e2b5c520c4d7aa1` |
| models | 503 | `models-20261108-201244-150-4188a93a6764407a9b27d64a75e4acf2` |
| pre | 145857 | `prologue-pre-20261108-201316-627-d7fa1f1e8ff5468d832427d7edbee2b6` |
| post | 186305 | `prologue-post-20261108-201616-608-b03837be49af4ae5bd4497717ffa59c6` |
| Explore | 313886 | `thruster-route-exploration-diagnostic-20261108-201954-793-6a9c121eba4643189d53ece02f86232c` |
| save（cold1498） | 11841 | `resident-save-compatibility-20261108-202539-967-ccf9611a28084773b8995992cea8f02b` |

全部failure=null/guards空/engine0/未超时。pre enter44/return44/executed44、stderr0；post执行/分类75、retired0，普通300000ms cap，ERROR0且仅原合同四WARNING（观察者故障1、根类型1、Completed→Recovering拒绝2），stderr23551字节，不伪称全零警告或旧180秒性能认证。其他十阶段均stderr0/ERROR/WARNING0。

Explore整线JSON真实记录标题→同player→Boss死亡→实体支撑出口，恢复0/0、11激活/10击败、33普通跳均grounded、unsupported_edges8；完成次数/事件各1，held60位置/body/save保持，velocity仍260。磁盘/terminal/held SHA均`24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24`。这不是全76清场或全操作证明。

save writer35912与cold reader29940由native身份记录证明独立进程，6witness及只读冷读成立。独立审计累计2865对输入副本差异0、66/66封存二进制实际hash匹配；终态rg+imported重枚举1916项/rg0、当前6产物、真实用户109及最后案9helper与封存清单一致。helper数量按phase实际为9/10/13等、内部前后一致，不统一硬写9。canonical仍`1712937B83844D0A2419A92F30AB8D5A6A3D05A8B38CF4CE5A61DBEA0AB0059E`。root查询无owned Godot/build残留；8002/52790均terminal，不重启、不再轮询。

早期实际handoff是454→455，本次相邻，不复现旧失败：old/new均Farstance、world鞋底(1271.375,635.92)→(1271.3751,635.92004)。publication687→689/process694→696，其中同物理454有重复发布。原断言仍会执行，没放宽门。

更重要的是同一有限账本直接捕获了普通发布跨物理tick：

| 事件 | physics | process | publication | 可处理/暂停/flow |
|---|---:|---:|---:|---|
| P | 444 | 684 | 677 | true / false / Playing |
| I、P | 446 | 685 | 678（P后） | true / false / Playing |
| I、P | 448 | 686 | 679（P后） | true / false / Playing |

I/P事件次序在环形账本ordinal102–107连续，process与publication各+1，而physics各+2；并非发布callback漏记、同一process已有idle却Coordinator未发布。该局部范围直接支持“物理更新可先发生两次，再有一次idle/正式呈现”的机制；只能说明本轮该区间，**不能补写v328旧454的具体原因**。I是pre-node边界，不能当节点已完成处理或正式Draw帧。

## 本轮取舍及未完成项

- 根代理inline保留脏树，禁Git/worktree/delete，以canonical前后快照替代提交范围；代价是无隔离checkout，需严格身份审计。
- 这是系统排错Phase1，不是新生产行为修复，不造测试RED→GREEN冒充修复；代价是旧Main缺帧资格仍需处理，下一未复现不解决它。
- 使用既有Input/Motor连续性证据，不再添重复物理观察器；代价是本次未独立记录跳过tick的floor owner，不能把滞后局部腿姿态当该tick正式Draw。
- 延续自主无窗口制作，不等待例行确认；不扩大到窗口/OS键鼠/付费/新图/GC/新dump/引擎/Git/发布/自动化/真档。
- 日志和多一个测试回调可能扰动时序，不能把本次成功归因于修复，也不能反复同版筛绿；尚未核对正常声画/像素/硬件真人、历史FATAL和完整十一项。
- v328 M328-1双肘/远腕正时间专项、v320等所有历史延期保留，没有顺带改动。

## 一次独立审查

`seam_clock_fresh_review_v329`核精确一文件差异及本机匹配Godot时钟文档、原发布链和解绑流程，C0/I0/M1；无生产缺陷或门槛放宽发现，不执行二审/修补。

M329-1延期：只有符合walking左→右handoff才输出账本；若整个跨缝都无发布或较早身份/Draw检查失败，积累I账本不会在AssertComplete失败时输出。间接影响是未来诊断可能还需额外运行，不影响当前已输出的128样本；不借此顺带改冻结代码，后续可做只读标量fallback，原异常不覆盖。v328 M328-1和所有历史延期继续保留。

审查未裁决项逐项保留：Main终态/整JSON及身份/cap由根代理与独立实际审计补证；旧v328具体原因/缺JSON无法追溯回填；无生产修复/确定复现/时序中性证明；新产物其他十阶段仍须实际执行；资源/WER/长期脏树全部模块不是本增量审查范围；正常声画/硬件真人/正式资产/原十一项不是headless能签；所有历史Minor未关闭。相应代价依次是晚期回归仍可失败、旧454原因未知、采样脆弱性和日志扰动仍在、额外运行成本、其他历史缺陷仍在、可见品质和实际手感未证、全动作覆盖仍欠。

本轮没有新媒体；汇报前一次容量check于2026-11-08T12:25:33.699Z成功：15,203,531 bytes/145文件、增长15,200,023、Notify=false，未达20GiB。不建自动化/通知器、不停止、不触发打包或删除。测试之外未开窗口、发OS按键、付费生成、新资产、原生采集、GC/cache/引擎实验、Git/发布或读写真用户存档。

本轮取证任务完成、当前同版自动资格已核实，**原完整十一项仍ACTIVE未完成**；诊断成功/新11绿不等于生产根因修复、正式声画、硬件真人或正式交付。依明确权限保留当前脏工作区和所有证据，不执行通用skill的Git合并/推送/清理菜单，不以“完成一诊断”要求用户决定集成。相应代价是仍需后续授权的正式版本归档，不假称GitHub已有v329。

## 原目标待办的现状纠偏（只读核实）

为避免继续围绕测试小修重复消耗、误把历史缺口当现存实现，独立explorer只读核E01/E02/E05。当前保存正常Level的ResidentContent=1，经`PrologueLevelController.cs:123`读取强制50条的`ResidentAuthoredLayout.cs:40`并配置唯一manager；`ResidentPopulationState.cs:39`及`CanyonResidentCoordinator.cs:95`有HP/death/lease/epoch/generation和重返自动协调。v291同封存产物全50自动出生（manual_spawns=0）、代表身份`canyon.v2.resident.low-ceiling-bridge.01`跨区HP3→2→2→0及死者不复生，已不是v046四个摆放缺口或从零实现manager。v134全50Ready/720物理步是无玩家目标的受控巡逻，不是全50实战。

已直接纠正总待办三个条目的过时表述，同时保留历史、原范围及未验收状态。真正缺的是可核对的逐身份发现/攻击/脱战/Returning/恢复巡逻及逐身份跨区HP/death覆盖账本，当前材料不能给出可信的尚欠ID差集；不能把50个都说成从未战斗，或把run-lifetime标量托管夸成敌人生命永久写入存档。下一安全内容证据可从已有跨区身份low-ceiling-bridge.01的原位置真实Player实战返航开始，不强移敌人/直接切AI/放宽危险与数值。此为现状核实，不是新原生测试通过或正式美术/全76验收。

### 下一项有实质内容差异的机关：悬吊圆弧齿轮

T03当前正式地图只有直轨，悬吊圆弧款仍未制作，不拿已有摆锤重计。独立只读核实最小接口如下，供下一内容任务沿用，不在当前冻结回归中修改：`MovingGearMotionModel.cs:62`的Frame输出都乘同TravelOffset、`CanyonMovingGear.cs:22/79/133`的端点AABB/单capsule扫掠/轨道绘制均为直线；弧线若照搬，会漏弧顶或误伤弦内空区。

更稳妥的后续方向是独立arc model/runtime，共享`CanyonFixedGear`真实GuardedDamage/Unblockable/cadence/attackID/回调撤权，保旧直轨默认值与保存节点不变。需同时做全弧访问/未来占用（`ResidentEnvironmentInventory.cs:47`）、雾只画当前真实圆及canonical Gameplay/Mechanisms直接子节点、保存原生Circle及精确shape白名单、配置图的真实弧语义；不能只改外观或两端连线。

首个有意义原生RED应为独立保存/重载trap+真实Player的配对几何：完整周期首尾同点，弧顶访问处真实伤害/接触序列成立；端点弦或包络内、弧访问外的玩家应零伤害。仅缺类reflection失败不能代替此几何反例。先独立原生内核，随后再选正式点位验证通路并更新地图；没有本轮新点位/参数/素材/更难数值或T03完成声明。相关旧消费者是moving-gear八案/多shape/九回调/fog、saved parent/placement、未来占用、保存cold白名单、配置图/旧机关和当前完整双路线/pre/post，不能漏掉。
