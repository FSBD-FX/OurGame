# Incinerator Annex v251 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL superpowers:executing-plans，按已明确持续执行偏好；独立文件机械worker可并行，root唯一build/native，全部CS/host冻结才执行。一次fresh终审，C/I一次TDD修补，Minor延期，不二审。无Git或清理。

**Goal:** 将完整大型销毁池和可避险回返附室接进原连续盆地，不冒充FALL RESET或缩成小缝。

**Architecture:** 独立持续接触致死Area复用现GuardedDamage/Health/DEATH RESET；扩Basin东边界而保持旧offset/场景内容；生成叶与parent/minimap同步。

**Tech Stack:** G:/knight/Knight Godot4.7.1/.NET9/C#，当前源1888，新增文件后以实际inventory为准。

**Spec:** ../specs/2026-11-05-incinerator-annex-v251-design.md

## Global Constraints

仅headless/隔离storage；旧24/899/HP/能量/单跳/iframe与固定55/180/300s不放宽。保76/9/1及旧offset/DesignBounds/layoutVersion2。无窗口/OS输入/付费/新媒体/额外dump/GC/KeepAlive/cache/引擎/自动化/Git/真实存档/删除。原十一项与正式动作/R-I/R-C/R-P继续OPEN。

## Review Focus

- GuardedDamage拒绝iframe也消费identity；每次合法pulse必须新编号，离开不能追杀，同receiver多shape不重复。
- 同步Died/reset/queued祖先或禁处理导致权限变化，必须在外部回调前后检查，不杀安全复活实例。
- 熔炉底板在4304旧FallReset之上但要使Player持续实际重叠；高速度/旧overlap不可仅靠名字或AABB声明。
- 三80px one-way右岸返回与上桥/墙不能挤压72高身体或只有二段跳可达。
- 旧steam路径/居民territory/保存定义/小地图一致，新增节点不能吞掉旧fallback或只改未加载生成器。

## Task1 · 完整致死体积（独立worker）

Files：新增scripts/mechanisms/CanyonIncinerator.cs、tests/integration/CanyonIncineratorRuntimeTests.cs及必要uid；root独占runner/host。Consumes真实Player.tscn/GuardedDamage/Health；Produces `public static Task CanyonIncineratorRuntimeTests.Run(SceneTree tree)`及机制Area公开Size/PulseSeconds/AttemptSequence/DangerBounds，无测试专属生产接口。

- [ ] 先反射真实机制缺失RED，完整tests通过实际shape/native overlap而不是fake receiver；回归entry为新thruster-incinerator，55s。
- [ ] 实现spec .1s/MaxHealth/Unblockable/当前几何/单调identity/reset/epoch与lifecycle；实测GREEN，保原Health/Shield数值。
- [ ] host精确要求机制最终收据，generic错误/退出/timeout门不变；所有source修改冻结才root跑。

## Task2 · 完整东附室接线（root）

Files：CanyonWorldLayout.cs/CanyonRouteNodeId.cs、CanyonContentBuilder.cs（必要partial新文件）、ResidentEnvironmentInventory.cs、fog controller/overlay；生成CanyonBasin.tscn和PrologueLevel.tscn；新地图原生测试。Consumes Task1 Area及spec全部literal几何。

- [ ] 先literal空间测试RED：完整720池/两路/三80px one-way返回/旧5offset/76-9-1不变/旧fallback不被新node吞并。
- [ ] 实装room/图边/discovery/focus/告警/环境hazard/fog真实边界，不替换原安全深井。返回步态真实键盘及单跳验证，失败修正空间不暗加二段跳。
- [ ] 生成器在隔离临时输出pack/instantiate逐节点与contract先核，保存前exact snapshots；叶与parent/minimap同步。预期output scope明确，旧源不得被无声重写。
- [ ] 跑独立saved-map/绕行/回返/致死恢复，全部保原生日志。未通过的完整路线不记完成。

## Task3 · 唯一审查与当前整体验证

- [ ] 一次fresh精确增量审查；C/I一次具名真实RED→GREEN，Minor/所有declined记录；无二审。
- [ ] 根据实际生产/地图改动跑当前模型/机关/保存/keyboard/geometry/pre/post和普通双线，历史/当前失败都保留；不称局部绿为全绿。
- [ ] 绑定源/binary/helper/用户数据保护清单，正常汇报媒体Check；更新README/原稳定待办/反馈/报告，保持全目标ACTIVE，完成要求未满足不得markcomplete。

Self-review：Task2消费Task1 Size和Area路径；root/worker只读接口与freeze门避免边测边改。Task3不得把新fixture当真实标题/路线证明。三右岸平台为one-way，避免80px级差挤头；旧Steam路径优先fallback。用户持续执行偏好覆盖重复设计确认/新worktree/commit/删除步骤，开发选择均明记而非用户批准。
