# 机械爪收绳反馈连续性

基线 `65c36d5dbf1d7d0a4337420ca91988b520575453`。本地小修，非新版本交付。
证据目录 `E:/OurGame/Knight/.evidence/september08-claw-reel-feedback/`，最终 SHA / 视频哈希见外部 CHECKPOINT.md。

## 事实、推理、风险

实际首台抓顶路线中，W+D 期间绳长由322.5持续缩短至150.7；角色先下沉再回升，并能松手落稳右台。
下沉样本绳长271.6→246.1，不能把下沉等同于W没生效。径向弹簧与右下切向输入叠加是现有模型行为；本轮不改它。
绳线张力接近饱和后变化很小，Reel/Swing共用抓附姿态；现有手腕环虽按真实绳长转动，却仅在遮挡时绘制。
因此仅扩大已有反馈的显示阶段，不增加文字、力道或空中能力。风险是手部局部更繁杂，真人能否理解仍待验证。

## 改动范围

唯一生产修改在 `ThreadShuttleVisual.DrawConnectedSpool`：机械爪Latched/Reel/Swing/Boost都画现有手腕环。
保留原半径12、双弧颜色/宽度、z10子节点、可替换NearArm手位和缺省退化、`-RopeLength/18`转角。
`ObscuredConnectionVisible`仍只报告遮挡；没有改绳索截断、实体出绳点、目标标记、父层级或任何玩法规则。
legacy ThreadCast、Travel、Released/Retract/Failed/Clear仍不显示此环；不加计时器、节点、资源或付费生成。

## RED / GREEN

- `raster-red.log`：四种朝向/抬手姿态×四附着状态，无遮挡环新增青色像素均0；真实模型W/S改变绳长但环像素变化0，退出1。
- `raster-green.log`及补齐负例后的`raster-final.log`：16组合均130–135青色像素；停绳变化0。
  模型真实cast→latch→W/停/S，收/放方向相反，像素变化各88，回到相同长度得到相同像素；最低48时继续W不空转。
  遮挡、松手、Clear、Travel、legacy、Retract/Failed、活旧骨架替换及RequestReady重入均通过。
  这是实际renderer中的模型/typed frame→呈现测试，固定角色运动以隔离反馈，不是InputMap或真人操作。
  Clear是独立无环基准，未再用“无遮挡”当off。测试不改生产力道以求通过。
- `pre-green.log` / `post-green.log`退出0、错误0；警告2/3均既有故意负向fixture，未删除或过滤。
- 最终build零错误警告，生产只有上述白名单门槛，独立审查无阻断问题。

## 实际运行

- `movie-before.log` / `movie-after.log`：同一真实InputMap路线，首敌格挡反击→首台→FrostVentCeiling→收绳→右台。
  各一次cast/latch/release、无Boost、落稳12physics；17条完整阶段记录逐条一致（包含位置、速度、绳长、目标和文字）。
  这仅证明该路线相同，不推广成全状态空间等价。
- `normal-cn.log`非fixed/movie正常窗口同路线通过，右台(2035.4543,703.98285)，无新普通跳跃或完成恢复。
  前置首敌接近路段本来有一次过缝跳；不能声称整个录像无跳跃。
- `route-normal.log`正常速度五区/四复活/两检查点/冰面/过热/门/Boss重试/结算通过，无失焦。
  回落18下降样本0边距违例/0道具栏相交、稳定12。
  2772进程回调p50=16.659ms、p95=17.294ms、p99=27.553ms、max=48.241ms，13次>33.3ms、0次>50ms。
  不是GPU帧时、稳定60fps或真人验收；此前60.930ms及57.208ms峰值仍保留。
  旧`--check-ascent-return`观察支段会RefreshNow，不称纯被动UI呈现。
- `route-visual.log`为原Task9 generated-main五区路线，硬件固定60fps通过，无错误警告；
  不是新首台FrostVent分支，含受控死亡/恢复，不重生成scene、不覆盖旧阶段artifacts、没有回落RefreshNow可选支段。
- `ordinary-main.log`普通窗口240帧与`headless-main.log` plain启动退出0，无错误警告。
  没有新导入型资产，未重导全项目；engine/build/ffmpeg均串行、前一个session结束后才启动下一个。

## 视觉与边界

前后短片来自RTX3060 Vulkan固定60fps，每片589帧约9.83秒。主/独立审7.9秒开始12Hz的4×6图板及英文附着/落地原图、中文附着原图。
清晰绳→遮挡→清晰绳的抽样中，手腕环不再因LOS变化消失；未见新增严重遮脸/脚下落点，落地后无环/绳残留。
环在近手部增加少量叠线；远端金/青双菱形、卡片换位仍在。不是全片逐60fps或真人感官认证。
图板结尾灰格含fixture退出，不能称关卡过程黑屏。原AVI仅外部保存。
主代理另审完整五区0.5Hz图板，所看样本未见手腕环盖脸/落台或退出附着后残留；不是五区逐帧或独立真人验收。

| 片段 | 帧数 | SHA256 |
| --- | --- | --- |
| before.mp4 | 589 | 8DCD11E4D174EBA14A8EC16FE64E0FDCFDAE9732FCE84887062910220F5CE750 |
| after.mp4 | 589 | 39660D2710C656BF87EEFDBBD64BB6BDF22B167A67EBCF19310C3271D94FDEE8 |
| route.mp4 | 2720 | 9D76A78D976FB59583593137D93AA10E276E38323B96DC9F5C3177F504CD988F |

下一轮转向盆地可选探索的入口、收益和回到主线的连续体验：先实走已有分岔，识别实际空洞或重复，
再做有证据的小范围改进，不继续只扩手腕环、移动卡片或堆测试工具。金/青叠标列为已知呈现欠账。
正式美术、独立Boost动作、真人首读/难度、旧认证、普通解压/源码一致性和真实商业数据仍未完成。
不冻结r3、不新ZIP/标签/GitHub上传，保留所有无关imports/总线/artifacts/旧UID。
