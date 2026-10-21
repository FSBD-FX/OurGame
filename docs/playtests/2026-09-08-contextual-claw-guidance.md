# 机械爪分阶段提示 · 2026-09-08

基于 `7f24566de6c51dec910f0e189dbc2508a0df9254` 的本地开发小修，不是冻结 r3 或体验里程碑。
本轮不生成交付 ZIP、标签或 GitHub Release；不修改 r1/r2 归档，无新增美术或付费服务。

## 判断与实现

- 事实：原 LearnGrapple 卡同时说明切模式、瞄准、抓取、摆荡、收绳、助推、脱离和禁抓材质。
  真实第三锚点抓附后，提示却仍是“沿高处向右进入遗落机械神殿”，没有当前附着操作。
- 推理：将首次抓取和附着操作分开，减少一次阅读量，并在实际可使用这些操作时显示说明。
- 风险：减少文字不能丢掉操作；附着不能抢走 Boss、机关或护盾教学。Released/Retract 仍保留目标引用，
  若仅判断 CurrentTarget 非空，会在松手后继续显示过期说明。

`PrologueGuidanceContext` 增加可选 ClawState，默认 Idle，兼容既有调用。
Controller 每次刷新读取当前有效 player/claw，缺少组件回 Idle，不新增缓存或事件订阅。
Model 只在 LearnGrappleMode、ReachBasin、ClimbCanyon、EnterTemple 四目标且状态为
Latched/Reel/Swing/Boost 时切换正文；原 targets、失败提示、标题、开关与完成隐藏保持不变。

未抓附的 LearnGrapple 只说明 E、瞄准粗糙墙/边缘/顶面、按住左键；
附着卡说明 A/D 摆荡、W/S 收放绳、Space 一次助推、松开左键脱离。
中英均使用既有 InputMap 字形替换。禁抓规则仍在下降区提示，原 Climb 文案没有改动。
不改变实际目标推进、输入、机械爪/跳跃/冲刺/盾物理、镜头、HUD 位置、地图或资源许可。

## 验证记录

证据目录：`E:/OurGame/Knight/.evidence/september08-contextual-claw-guidance/`。

- `route-guidance-red.log`：正常窗口正确退出 1，实际 Latched 时仍显示进入神殿路线文案；不是卡关或失焦。
- `guidance-focused-red.log`：仅加入无行为可选上下文字段后编译成功，原行为触发“未附着卡不应提前显示 W/S”断言，退出 1。
- 初次 `guidance-focused-green.log` 与 `pre-final.log` 退出 0；独立审查指出英文 A/D 定向覆盖不足，
  随后补双语附着 A/D 与 SWING_LEFT/SWING_RIGHT 重映射检查，独立复核闭环。
- 最终 `guidance-focused-final.log`、`pre-reviewed.log`、`post-final.log` 各退出 0，错误 0；
  focused 无警告，pre/post 保留 2/3 条刻意异常用例警告（非法视觉/音效根、抛错 observer、非法房间视觉根、完成后恢复拒绝）。
- `route-guidance-green.log` 正常窗口、非固定帧、无 movie writer，五区/四次复活/门/Boss 重试/结算通过。
  实际 Latched/Reel 操作卡可见且文字容纳于挂载 Label；Released、target 尚存时恢复路线文案；落地稳定 12 帧后仍正确。
  原回落专项 18 个早期下降采样保持安全边距及 Loadout 零交叠，不外推到全关所有 HUD。

真实路线的断言显式调用 RefreshNow，证明真实状态的消费与显示；不单独声称它证明自动 `_Process` 刷新时序。
展示脚本中的三阶段卡来自人工指定模型上下文，只作排版检查，不冒充真实抓取。

`ui-resolutions.log` 退出 0、无错误/警告；1280×720、1920×1080、2560×1440、3840×2160
共 90 个展示状态、270 张连续截图，包含中英未附着/附着/下降卡。
这是逻辑1280×720视口的四种输出分辨率，不是所有UI缩放比例验证。
`ordinary-main.log` 无测试脚本的普通窗口240帧退出0、无错误/警告。
本轮编译及 JSON 资源导入成功，无错误/警告，未重建场景或改动启动地图。

正常窗口2772个有效进程回调样本：p50=16.659ms、p95=17.189ms、p99=24.513ms、max=44.290ms，
6次>33.3ms、0次>50ms，无失焦。自动驾驶/遥测下的回调间隔不是稳定60fps或GPU帧时认证；
不与历史样本合并，保留57.208ms历史峰值。

`route-guidance-movie.mp4` 为RTX3060硬件Vulkan固定60fps，2902帧约48.37秒，完整路线退出0、无错误/警告。
`claw-guidance-10s.mp4` 是32–42秒原速节选，包含抓附、收绳、松手和继续前进；非真人操作或正常帧率证明。
最终A/D测试加强后重跑focused/pre/post/UI；路线录像与正常路线生产代码相同。
随后诊断只在既有 `--trace-grapple` 日志补可见性/strand字段，没有修改玩法或录像时的生产状态。

## 画面复核与诊断发现

主/独立代理审看2秒原尺寸新旧图：同世界构图下，入口机械爪卡由7行缩至3行，右上空出更多背景视野。
34秒原尺寸帧有真实连接绳且三行附着操作完整；35秒松手后恢复路线提示，角色、当前锚点与回落台面仍可辨。
主代理另看全路线3秒间隔总览、33/37秒各4秒6fps图板，以及47.3秒起12fps结束图板（可见结算）；
独立代理复核33/37秒图板和中英6个展示卡。大图板在工具内缩小显示，文字检查以1280原尺寸图为准。
这不是逐60fps连续审片或真人手感验收。完整录像保留自动测试退出尾部，短片无清理灰帧。

展示卡无溢出/截断；下降主文和失败Hint仍重复禁抓/安全台内容，英文多占一行。
英文模型卡周围仍是中文HUD，附着模型卡中Q高亮且角色左侧出屏，均为人工上下文fixture边界，
不能冒充真实全界面语言切换、附着状态或全程无遮挡证明。原右下装备栏局部压平台、说明牌遮背景仍在。

独立审图发现37–38秒附近提示“已抓住”但连接线消失。按 systematic-debugging 继续检查而未忽略：
`route-rope-diagnostic.log` 同生产代码固定60fps、真实输入全路线退出0、无错误/警告。
第三锚点 `loop_53..148` / physics `2199..2294` 连续96个物理样本均为
`state=Reel failure=None cast=5 effective_visible=False line_state=Idle strands=0`，
目标仍为AscentAnchorThree，LMB仍按住，绳长362.49→160.96，牵引速度修正仍非零。
因此不是新提示滞留，而是既有“物理仍附着、显示整条绳清空”的反馈缺陷。
按记录的玩家位置与固定锚点(3694,1300)计算，距离约190.53–412.87px，小于620px射程；
结合EffectiveVisibility定义，指向LOS失效而非超距，但具体遮挡碰撞体尚未定位。

独立源码复核：`MechanicalClawModel.StepAttached`维持约束但另算可见性，
`ThreadLineVisual`在不可见时Clear，相关代码在本轮及前几轮均未改动。
本轮不把有效连接强制脱钩或让绳直接穿墙显示；下一轮优先修复附着关系的视觉反馈，
先明确遮挡时的可见提示规则，再验证真实松手清理、禁抓准入和原物理保持不变。

完整视频SHA256：`11420815E094CAFF6C0412ABE44F1297120EA7628CC386BFC3D26456636D3E67`。
短片SHA256：`F09E8EB2E88813E5CEBF1AB3FD8DE076535A61BEF18A26EDB17C212AB1F2368A`。
最终精确提交与证据由外部目录CHECKPOINT.md绑定。

## 保留边界

- 原目标推进在按 E 时进入 ReachBasin，在进入上升区时进入 EnterTemple；没有新增强制教学等待。
- 附着卡描述规则，不声称 Space 此刻一定可用：已耗一次助推或持盾时仍受原规则限制，持盾也限制收绳。
- 保留原路线 targets，不把标记声称为当前抓住的目标；第三锚点连接时旧目标仍可能是 AnchorOne。
- 旧独立认证、普通解压启动/源码一致性、正式美术、真人初次上手与手感仍待验证。
  没有玩家或价格数据，不宣称更接近十万付费意愿目标。
