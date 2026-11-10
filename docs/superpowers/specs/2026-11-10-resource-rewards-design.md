# KNIGHT 探索材料奖励：持久领取与Q接入

用户要求每支路有真实收益；上一轮54点仍只是规划。本轮把同一批奖励变成运行组件/事务，不宣称已完成新地图布置。只在G:/knight/Knight，沿用Godot/.NET/现有Q，不改鼠标、武器槽、食取、回血箱、九残片/最低六枚。

## 事实与取舍

现有Q已走PlayerInputReader→EnvironmentInteractionController，站定/80范围/LOS/单输入序号/活体/暂停/动作仲裁齐全。现有回血补给先改live再经历回调，不能当材料事务模板。采用现有碎片的durable-first路径；保留schema4，新增可缺省`ClaimedRewardIds=[]`，不把它加入旧v4强制字段，旧档缺字段合法且不重写。显式null/非数组/未知ID拒绝；schema1/2/3迁移清空注入的未来奖励。

比较方案：复用Discovery只会记录到访/回血，没有材料；单独保存“余额＋已开箱”有双真源失配风险。开发选择“固定v1奖励目录＋领取ID账本”，五类累计获得量从账本派生，当前没有花费因而也等于持有量；未来消费需独立事务，本轮不实现经济系统。目录旧ID及授予量一旦发布不追溯改动，未来奖励用新ID/版本；旧二进制降级写档不在兼容承诺内，不能回写覆盖新字段。

## 接口与行为

- `ResourceRewardCatalog`为runtime固定权威，不依赖Build.Candidate。54定义、10箱/44散点与上一轮相同；builder改为读取该目录，不保存第二份可漂移金额。
- Progress维护领取集合；ToSave、Normalize克隆、Restore/Reset、flow PopulateProgressSnapshot全部保留。只保存IDs，不保存独立余额；全54领取总计纤维9/矿质33/零件44/数据12/残响2。
- `PrologueResourceCache : Node2D, IEnvironmentInteractable`服务箱子和散点；仅导出RewardId，不给场景独立金额。已领取不QueueFree，同一节点跨New/Continue从当前账本恢复。`BindContinuousNode`自动绑定新streamed节点。
- TryInteract必须是实际player、同world/level、存活/Playing/可见、未暂停；要求现有controller.CurrentCandidate就是本物体且当前接受Q边沿。范围/LOS/站定/动作仲裁沿现有路径，不另建挑选器。flow内部再次核对来源授权。
- flow制作detached candidate，加入ID、Normalize、SaveAtomic成功后无await/回调发布Progress/Save；失败不消费、不改旧存档/live/Save引用，可用freshQ重试。更新节点状态和反馈后，成功信号是最后可观察操作；监听器重入/New/回标题/卸载不能再写旧局。
- 新节点使用明确标注的开发占位分层Available/Claimed/Status接口，不做正式生成图，不把模型/布局当审美通过。提示沿已有本地化live键位，无第二套浮动目标。

## 验证与交付门

模型先RED：缺领取或54目录；随后真实Normalize/独立保存读取、旧v4缺字段、坏字段、旧schema注入、Reset/Restore/泛保存脱离性、原九残片保持。运行专项先RED：保存主场景里站定Q无法领奖；再完整新组件实际Q、held/重复ID、全54逐点、失败重试、死亡/泛保存、真实Continue/remount/new、成功回调重入。packed候选叶保存在隔离证据目录，回载验证自动绑定，不向当前地图偷偷摆54处。

规范wrapper增加`resource-rewards`无窗口阶段，warm输出三个固定证据：已全领/新局空/旧v4缺字段及各SHA/writerPID。wrapper同版开启独立cold SceneTree读取，核不同PID/字节只读/总量、原配额不变；不自行绕过owner启动进程。

随后原保存/交互回归、普通pre/post及完整原十一测试按影响核对；保留全部旧失败。新地图24/10地形部署、真实所有支路路线、正式材质/UI/动作/声画、资源终结器根因/硬件/真人仍是大目标未完成项。无GUI/系统输入/付费/自动化/Git/删除/真实档/新dump/GC缓存引擎试验。
