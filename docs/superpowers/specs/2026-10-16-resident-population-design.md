# 序章新增50常驻敌群设计

2026-10-18当前边界：[候选父场景／同源内嵌小地图交接](../../production/2026-10-18-candidate-parent-embedded-minimap-v001.md)。真实五区快照、六文件原字节搬移、catalog132、原生Ready／平台／存活唯一Player通过；修改叶后须受检重建父场景，未覆盖正式127。测试以真实死亡反例补存活门，并以精确cast／目标事件修漏采Arrived；局部往返再过，游戏能力不改。最终十一门6GREEN5FAIL，三Handle／154及31RID／Upper／Bell仍未解；50null／新0／24canonical未实现、43候选7未解不算完成。下一旧四身份／五记录AI活动兼容再余七及全50／manager；所有数量、族系、正常全关、视听、真人和录像条件不减。goal工具当前null，完整目标未完成，不新建／缩减／冒称完成；以下为历史时点。

2026-10-18当前边界：[冰区实际玩家往返／停靠净空修正](../../production/2026-10-18-frozen-player-roundtrip-and-dock-clearance-v001.md)。真实48px停靠＋40px身体揭示60px不足，平台收短20px后80px净空通过；同保存Player一跳／一次借力抓墙抓顶落台返回、HP5→5，但机关不活动，非正常全关／真人。两Ghost保持，全50仍43候选／7未解／20889诊断；最终十门6GREEN4FAIL，154／31资源／Upper／Bell保留。正式仍127／新增0／50null／24Unimplemented，父内嵌Minimap未交接；数量、族系、视听及全部交付条件不减。完整目标未完成，工具usageLimited未改，以下ACTIVE为历史文字。

2026-10-17最新执行边界：[冰封侧龛／两幽灵原生巡逻候选](../../production/2026-10-17-frozen-resident-alcoves-native-v001.md)。仅隔离候选132地形、两Ghost真实360帧往返／落地／回原点；全50联合43候选／7未解。Player60px胶囊查询不等于玩家／机械爪返程。正式仍127地形／新增0、父Minimap未接线，全部Placement=null／24canonical未实现；最终九门5GREEN／4FAIL，资源／Upper／Bell未解。数量／族系／视听／完整验收目标不变，ACTIVE。下方执行记录为历史时点。

2026-10-17执行状态：[实际叶点位及拒绝证据](../../production/2026-10-17-resident-actual-leaf-geometry-v001.md)。数量／族系／交付条件不变；50身体身份／24真实叶及factory覆盖24子型，41联合候选／9未解；首案通过但同版复测FATAL，最终八门3GREEN／5FAIL。全部Placement仍null、正式新增0，不把候选／统计当完成；全目标ACTIVE。

2026-10-16。用户要求：现有敌人之外追加约50只自主活动、有固定区域的敌人，含游荡/冲锋/俯冲/飞行/固定炮台等；骨骸、幽灵及其他系列必须进入本关，并先查看最终完整分配规划。

## Binding population brief

完整唯一数量/家族/区域分配见 `docs/production/2026-10-16-prologue-population-final-plan-v001.md`：旧26保留，新50=原生8/废弃12/精工10/骨骸10/幽灵10；全关76普通+1Boss，主50/支26。新角色六职能16/7/8/6/9/4。NPC为一ECHO指引角色、无独立友方身体/商人；不把设施当NPC。逐行表及24配置子型不得在实现中静默缩减；这些为目标，不是完成证据。

## Architecture

独立常驻名册，不用50个CanyonEncounterGroup伪造巡逻。world/level-lifetime manager跨V2卸载保留stable slot状态；直接把实例挂到sector的Gameplay/Actors，以适配当前Fog的直接子节点采集。所有既有9类型默认wave行为保持；新的opt-in residence配置只能在mount前应用。

活动带/空域+巡逻路点+追击许可+返航必须是真正移动/碰撞行为。原生/废弃/骨骸/幽灵的子型不能只是名字不同。骨骸姿态弱点和幽灵显形必须给现有J/K可行应对，不依赖未来武器。禁止外部每帧硬改位置、假玩家目标、跳过物理、无限追踪或睡眠回血。受击/拉拽等外力不能被硬夹紧到矩形；保留安全回返或原环境击杀。

生命周期：出屏/卸载不重生；普通checkpoint激活不重生；真实死亡恢复按现有目的/有效departing sector合同重置，manager与actor不可双重重置；new run显式清epoch；旧save加载首次初始化运行名册，不改9残片/6门槛、原保存迁移或通关保存。

## Placement and readability

当前50精确坐标未通过原生验证。必须对保存地形逐点验证完整身体支持/净空、Area陷阱与移动机关扫掠、出生/检查点/rest/boss/exit保护、抓爪落点和其他actor占位。需更多空间时做围合侧龛，不把人数压进营地或依靠穿墙。炮台射线有LOS、屏内前摇及危险边界，远处睡眠/唤醒必须撤销伤害并保持HP/身份，不能单独Disable processing。NPC/敌人/机关提示不增加跳动方框。

## Verification gates

1. Counts/IDs/type/route/family分配及同检查负控，旧26与50分开。
2. Real ground patrol/ledge turn/charge/air cruise/dive/return，ghost显形/骨械弱点/J-K应对；不通过模型单测假称运行。
3. 每个50点真实碰撞/陷阱/保护/完整身体/路点；全部50稳定ID实际mount，不能只目录存在。
4. Death/sleep/wake/unmount/remount/newrun/save restore/stale callback，不回血、不重复、没有旧击杀重放。
5. 正常Main+Explore新版本同角色通Boss/实体出口/保存，记录新增敌群实际接触；旧路径成功不能替代。完整原有回归失败按名保留，不豁免日志。
6. 正常显示帧率/感官/家族识别/短归档录像和真人反馈，所有展示先归档。未达到时全目标ACTIVE。

## Constraints and status

沿用权威脏linked checkout `E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`，不Git写入/付费/上传/自动化/安装升级/删除。root唯一native launcher，运行时源码/帮助器/构建冻结，原真实userdata只hash、不读取或改写语义内容。新功能必要新源必须按真实库存更新精确guard，不假装仍1647、不关闭guard。

用户最新要求“现在需要看到最后完整分配和规划”，本阶段交付规划而非擅自宣布最终美术/参数已批准或代码已完成。当前源码仍旧26/9+Boss，尚无50常驻；下一实施入口为单独实施计划。性能线索保持：旧Main max261.272ms附近有813条测试stdout同步排出及生产report持久化，原因未拆分；新增敌群不得靠降低画质或隐藏名额换绿。
