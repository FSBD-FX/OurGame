# 下降教学卡与安全落脚台（本地开发检查点）

基线：`1cecdafa839a3fa27e90c0df0254c2d45fa2a168`，不改写r2，不发布新整包。
证据根：`E:/OurGame/Knight/.evidence/september08-descent-card-clearance/`。
最终提交与视频哈希由外部 `CHECKPOINT.md` 绑定。

## 事实、推理与风险

- 上轮真实下降入口截图中，英文卡遮住第一小平台右上角；该台实际为GripStoneCatchZone，不是GripLanding。
- 入口当前平台顶y610，第一台GripStoneCatchZone顶y684；继续D自然下落接到GripCatchShelf顶y854。
  不需要跳跃，不能用上方GripLanding作为第二次自然落台的错误验收目标。
- 保护适配器此前会避让角色、敌人、攻击、绳和锚点，但所有UseCollisionHitPoint地形都被排除。
  现有CanyonContentBuilder已经用 `catch_shelf=true` 标记安全台，无需猜测矩形宽高或改场景。
- 保护整块地形会消耗过多卡片空间；本轮只保护这些作者台面的薄带，并验证没有靠隐藏卡片通过。
  该规则影响17个既有标记平台（下降5、盆地1、上升10、神殿1），不是全部平台；需要额外检查上升区显示。

## RED与正常路线对照

- `platform-lip-red.log`：真实挂载fixture先失败在平台实际顶边没有保护区域，预期退出1。
  用例已加入常规 `RunPrologueTests --phase=pre`，不只留在旧专项入口。
- `descent-normal-red.log` 虽按计划名为red，但实际退出0：两个平台自然D落稳，136采样没有交集。
  当时镜头把第一台移到屏幕x816…976，卡片从x984开始；这是未复现样本，不能充当RED。
- `descent-movie-red.log` 硬件Vulkan固定60fps复现：同样两台自然D落稳，第一台顶边x917…1077、y466…474，
  卡片x984…1232、y348…490，交集744px²；136采样中1次，走完后统一因UI断言退出1。
  两个版本完全相同的272条采样前缀（render/physics/位置/支持体/屏幕顶边）有序一致；这不是全状态空间等价证明。
- 新probe只在原首敌格挡/反击及E+D跨区后继续D，不注入位置/速度/抓附状态，不人工Present或关闭AI。
  首次真实向上碰撞指定StaticBody后松D，要求连续6个不同物理帧支持，再前往第二台；6秒总物理预算。
  “落稳”指持续地面支持，不是水平速度已归零；当时松D减速到73.33px/s。未增加AcceptedJumpCount。

## 修改范围

- 唯一生产改动：RoomProtectionRegionAdapter对World层、StaticBody2D、`catch_shelf=true` 的节点，
  从启用的直接子RectangleShape2D取真实canvas变换，在碰撞顶边上下各6局部单位建立保护带。
  保留Placer原8px避让余量；不改变原候选顺序、卡片尺寸/文字、目标选择、HUD或任何物理规则。
- 不改材质的可抓/禁抓、地图/场景/碰撞、跳跃、助推、护盾、能量、伤害或输入，无新资产或付费。
  未标记/false、禁用碰撞、隐藏body均不保护；整个平台内部、无标记墙面保持可供卡片使用。
  缓存存的是节点，每次重新求区域，原候选缓存已包括CollisionObject2D，无需更改缓存机制。
- `--descent-platforms` 为可选探针，必须有 `--exit-guidance`。每PostDraw独立投影两台实际碰撞顶边±4局部单位，
  检查与卡片交集、可见非空、实际Label文本容纳；先截图/走完再统一报错，隐藏卡不算通过。
  当前默认zoom1下可用像素表述；不泛化成任意缩放下恒定±4屏幕像素。

## GREEN与回归

- `pre-green.log` / `post-green.log` 全部退出0，ERROR为0，2/3条WARNING仍为原故意异常fixture。
  新fixture验证标记true→缺失→false→true、disabled/hidden与非整块保护，真实Visual也收到顶边区域。
- `descent-movie-green.log`：与RED同路线136采样，两个顶边均0重叠、0文字不完整；平台实际落点一致，退出0。
  head卡从y348移到y300，小平台浅绿顶缘完整露出。
- `descent-normal-en.log` / `descent-normal-cn.log`：非fixed/movie正常窗口，各137采样，两个顶边0重叠、文字完整，
  实际D落两台、无完成复活或新增跳跃。前序首战各格挡1/反击1/敌HP3→2/玩家无伤，退出0无错误警告。
- `route-green.log`：正常速度、非固定帧/非movie五区全路线，4次复活、2检查点、Ice拒绝、过热、门、Boss重试/结算通过，
  无失焦；回落18下降样本0超安全边距/0道具栏交叠、稳定12帧。该旧专项会RefreshNow检查提示，不冒充纯被动呈现。
  2775进程回调样本p50=16.658ms、p95=17.239ms、p99=28.404ms、max=47.268ms，13次>33.3ms、0次>50ms；
  不是GPU帧时、稳定60fps或真人验证。
- `route-visual.log` 用现有RunTask9Tests的 `--generated-main`（无 `--check-ascent-return`）录像，
  不重生成/还原场景、不人工RefreshNow，自动InputMap路线含受控死亡/恢复测试；五区全程通过，无错误警告。
  captureFrames=false，不覆盖artifacts里的旧阶段截图；GUID隔离SavePath/SettingsPath仍有磁盘残留。
- `ordinary-main.log` 普通窗口240帧、`headless-main.log` plain启动均退出0无错误警告；最终build零错误警告。
  无资产变化，因此不需要重新导入。所有构建、Godot与ffmpeg串行，返回session后等待真正退出再运行下一个。

## 录像、独立复核与边界

- 前后 `descent-before.mp4` / `descent-after.mp4` 均490帧、约8.17秒，RTX3060 Vulkan固定60fps。
  SHA256分别为 `24DDD3819DA1C597E9FFE970C2F95BAFF96C89DF359C1E2A08207E615E3F0440` /
  `11118B40806AAEDE4504EEE3067509F9BEFA6A59D5852B8809F3585780CF495A`。
- `route-visual.mp4` 共2720帧、约45.33秒，同硬件固定60fps；
  SHA256=`C768AE76D5956D28CE91225CBE2B99CD7ACFA5F460C543774D2D56CEF3CADFFA`。
  原AVI只在外部证据，不进Git；不把这些固定帧片作为正常性能证据。
- 主/独立看过前后2Hz图板、英文head前后及两次落台原图、中文落台图。
  关键重叠发生于短瞬间，主要由同帧原图及逐帧交集证明，不说2Hz板本身捕获了这一帧。
  主/独立另看全路线0.5Hz总览、27–33与33–39秒各3Hz上升/转场板；所看样本未出现新增卡片消失或严重新遮挡。
  英文五行、中文三行原图完整，角色、当前关键抓点/落脚台露出；图板末尾含退出边界灰帧与补格，不当作过程黑屏。
- 独立代码复核未见P0/P1/P2。边界：没有专门测试StaticBody/World阴性及缓存路径变异；恢复计数只听Completed。
  原fixture部分异常路径和GUID存档残留仍在；这些测试并非硬件键鼠/真人首读、难度、审美或任意操作路径验证。
- 卡片依然会随候选位置切换，未承诺全帧位置稳定；盆地中层长平台右端、神殿金门内部、下降处左上锚点与资源HUD
  在审图样本里仍有局部重叠。没有旧全路线对照，不判为本轮新增；未标记平台/大字号/任意zoom仍未全面验收。

## 接续

本轮只保存本地检查点，不冻结r3、不新ZIP、标签或GitHub上传，保留所有无关导入/总线/artifacts/旧UID。
下一轮优先检查下降段机械爪的首用可理解性：当前ReachBasin强调禁抓与安全回落，但是否能让玩家立即找到
视野/620px射程内的可抓地形尚未验证。先真实输入复现，再考虑分段目标和短提示，不强制抓取、改距离或加入二段跳。
正式美术、独立Boost动作、真人首读/难度、旧认证、普通解压/源码一致性与十万付费意愿的真实商业数据仍缺。
