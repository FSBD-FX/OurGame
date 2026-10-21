# 首战后出口指引与资源标签（开发检查点）

基线：`1b2b8bd15420342d3e76aed8e924454a06b2d9dc`。仅本地小修，不命名新发布版本。
证据根：`E:/OurGame/Knight/.evidence/september08-entrance-route-guidance/`。
最终提交与录像校验值由该目录 `CHECKPOINT.md` 绑定。

## 事实、推理与风险

- 入口真正按 E 后任务已到 ReachBasin，但旧模型仍要求仅下降区可解析的 DescentIce；入口没有路线标记。
- 入口现有 NextSeam 为 (1278,574)，首敌后的 AttackFloor 与下降区 EntryShelf 均顶面 y610、在 x1280 接续。
  实际分区转换依赖玩家中心进入下一 WorldBounds；NextSeam 是导航点，不是触发器或抓点。
- 应先指向当前脚下平台的右端，真正进入下降区后才展示下行/冰面教学；不改任务进度或强制击杀。
- 英文 ENERGY 的真实字宽62px，但原标签到条的距离只有50px，尾字被覆盖。UI缩小不缩标签字号，会进一步挤压。
- 可通行不代表新玩家懂路线，标记有数量不代表指对位置；因此检查真实节点身份、前方位置、实际E+D跨区及原生截图。

## 修复前证据

- `entrance-route-red.log`：真实挂载主场景、InputEventAction E 后，RenderedTargetCount=0，预期退出1。
- `route-movie-red.log`：硬件 Vulkan 固定60fps首战格挡/反击后，用真实 E、D 继续走；入口目标无法解析，
  玩家从 (893.126,573.98737) 到 (1291.239,573.98737)，真正进入 CanyonDescent，Ice目标才解析/绘制1个。
  未完成复活、采样生命5；统一在走完后报告入口目标失败，退出1，不把仅到达导航点当作转场。
  `before/exit-after-E.png` 清楚显示错误下行卡与 ENERGY 尾字遮挡；`before/exit-descent.png` 保留切区后画面。
- 首次 `resource-label-red.log` 是测试初始化问题：GameFlow延迟应用默认中文，覆盖首个英文样本。
  expected ENERGY/actual 能量不充当尺寸RED；等待真实Playing后再测，无生产修补或断言放宽。
- `resource-label-layout-red.log` 是有效尺寸RED：12组合（2语言×2分辨率×3缩放）24标签。
  1280/英文/1.0 的ENERGY、1280/英文/0.8的两个标签、1920/英文/0.8的ENERGY与条相交。
  中文切换后还测到一次保留旧宽62px的Label矩形相交，不等同于中文墨字本身遮挡。整体预期退出1。

## 修改范围

- GuidanceTargetKey末尾追加EntranceExit，不改变已有枚举值；Resolver绑定现存入口NextSeam。
- 仅ReachBasin且CanyonEntrance使用双语“沿当前平台向右”卡；真正进入Descent后沿用原Ice教学。
- 首战情境/抓附优先级、当前任务、场景与地图、输入、跳跃/冲刺/抓爪、战斗与资源数值均不改。
- FormalHud在SetLanguage/ApplyLayout后按真实字宽重排标签列，保持字号、完整译文、资源区矩形和条右端。
  默认英文起点x330→344，条宽136→122；0.8缩放条宽108.8→85.2。不改变归一化资源值或过热规则。
  切回中文重新取原始布局宽度，不累积偏移；过热标记随条宽保留4×scale右内边距。
  旧过热线装饰绘制未改。本轮没有新增美术、场景重生成或付费生成。

## 验证与边界

- `pre-final.log`/`post-green.log`退出0、错误0，警告2/3均为原有故意异常fixture；入口缺目标警告已消除。
  新runtime真实E后验证目标节点身份和前方位置；resolver实际挂载入口/错区拒绝；既有抓爪、护盾和通路测试保持。
  资源排版12组合/24标签均完整、无Label矩形相交且留在资源区内，包含EN→ZH、1→0.8→1.5切换。
  这是headless Control测量，不是12组人工视觉验收；旧post另覆盖资源精确值与过热可见性。
- `exit-normal-en.log`和`exit-normal-cn.log`为非fixed/movie的正常窗口：各真实格挡1、反击1、敌HP3→2、玩家无伤，
  其后真实E/D进入下降区、任务维持ReachBasin、入口/下降实际绘制目标各1。没有传送或关闭AI，退出0无警告错误。
- `route-green.log`正常速度、非fixed/movie五区路线通过，4次复活、门、Boss重试/结算通过，无失焦记录。
  回落18下降采样0超安全边距、0道具栏交叠、落地稳定12帧。
  2773进程回调样本p50=16.661ms、p95=17.314ms、p99=25.524ms、max=46.066ms，12次>33.3ms、0次>50ms。
  不是GPU帧时或稳定60fps认证，也不覆盖人类的任意操作路径。
- `import-green.log`、`ordinary-main.log`普通窗口240帧、`headless-main.log`均退出0且无错误警告；构建0错误警告。
  所有构建/Godot/ffmpeg串行；post返回session后已等待真正结束再启动下一引擎。
- 前后`route-before.mp4`/`route-after.mp4`均RTX3060硬件Vulkan、固定60fps、355帧约5.92秒。
  SHA256分别为`AAE584D7302A416D6A688203DDDCC66AF9B272A14D5DABDBD0FFB37DDD54A814`、
  `96AB8FC9C5F533868337CF84201FCB5A8113E69A08BE60F787B0AEFFF691B2EF`。
  主/独立审看每秒2帧图板和前后入口/下降原图、中文入口原图：新卡明确当前平台向右，金色标记位于右端约(1252,574)，
  没指回旧入口，入口角色/敌人/脚下平台未新增严重遮挡；ENERGY六字母完整。不是逐60fps全片审看。
- 独立代码审查无新增P0/P1/P2。非阻断边界：探针允许正确目标之外有额外目标，复活计数只观察RecoveryCompleted；
  排版测试未单独断言条右端和过热标记精确间距，当前源码与旧过热测试支持这些行为但不称专门像素验收。
  测试使用GUID隔离存档，有磁盘残留；不删除旧UID/导入/总线/artifacts等无关改动。

## 后续

下降区英文卡仍在约x984…1232、y348…490，盖住第一块下降小平台（约x917…1077、y470…502）右上部分。
修复前后相同，主/独立已确认。下一轮先沿正常下降路线复现教学卡与落脚点冲突，再做最小避让或精简，
保留可抓/禁抓和安全回落信息，不重做本轮入口/标签，不改跳跃距离凑通关。其他loadout/大字世界遮挡不在本轮解决范围。
固定帧录像不作性能或真人理解证明；探针使用自动InputMap事件，不是实体键鼠或真人试玩。
旧独立认证、普通解压/源码一致性、正式美术、真人首读/难度与真实商业数据仍未完成。
本轮只提交本地开发检查点；无新ZIP、标签、r3冻结或GitHub上传，原AVI只保留外部证据。
