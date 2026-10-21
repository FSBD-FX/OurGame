# 首战松盾与反击衔接 · 2026-09-08

基线 `ce115e33ce107d7a6aefffec859626bc2627cb2f`。本轮只处理防御退出后的新攻击输入，不改变第一关难度、攻击范围、能量数值或新增机制。

## 事实、推理与风险

- 真实主场景、正常速度、自动输入：先按 Q，接近首敌，在前摇开始至少0.25秒后瞄准它举盾；真实格挡后，于敌人 Recovery 松开右键并新按左键。
- 延迟一个完整物理 tick 的对照能反击；同一输入批次松盾并新按左键，两轮均被 `ShieldActive` 拒绝。拒绝时实际采样已经是 `ShieldHeld=false / PrimaryPressed=true`，但盾组件仍为 `IsActive=true`。
- Input先采样，Combat先于Shield更新；攻击许可读取旧盾状态。不是新点击未送到，不是按住旧左键后松盾的正确拒绝。
- 自动反应时间不代表新玩家理解。现有规格允许过热期间攻击，不能改成“只要仍按右键就禁攻”。本轮不重定义全新同时按 RMB+LMB 的起始优先级，也不新增攻击缓冲。

## 复现与诊断

外部证据：`E:/OurGame/Knight/.evidence/september08-first-combat-guidance/`。

| 文件 | 结果 |
| --- | --- |
| `defend-delayed-before.log` | 普通窗口退出0：格挡1、新反击1、敌人实际HP 3→2、玩家无伤 |
| `defend-same-tick-red.log` | 普通窗口退出1：格挡2、反击请求2、接受0、敌人伤害0；physics232/319真实误拒绝 |
| `counter-movie-red.log` | RTX3060 Vulkan固定60fps同样复现，physics229/316真实误拒绝；退出1 |
| `handoff-regression-red.log` | 首版集成测试在physics信号中投递输入，反击被延后接受，expected输入序号7/actual8；这是测试调度问题，不充当生产RED |
| `handoff-input-red.log` | 改从ProcessFrame投递后，sample11真实 `ShieldHeld=false / PrimaryPressed=true / IsActive=true` 被拒绝，预期攻击数1/实际0；正确RED退出1 |

Probe扩展为可选 `--defend-counter`，加 `--same-tick-counter` 对照同批次松盾和新点击；默认旧观察模式保持不变。
加载真正主场景、隔离存档与设置，只在入树前指定入口出生点；之后通过InputEventAction和鼠标事件，全部控制器、碰撞、AI与呈现自动运行。
没有直接调用格挡、伤害或攻击模型，没有人工设置敌人阶段。成功条件是至少一次真实零伤格挡、玩家无伤及实际反击造成敌人掉血，不承诺两轮或任意场景归因。
finally释放输入、退订事件并QueueFree；不声称磁盘零残留或完整退出时序证明。原始AVI保存在外部证据，不进入源码。

## 修改与最终验证

生产只改 `PlayerActionModeController`：攻击许可与拒绝原因共用 `ShieldBlocksAttack`，条件为实际盾激活且当前输入仍持盾；依赖尚未初始化时保持拒绝。
没有改全局调度、Shield/Energy/Combat模型、攻击成本12、普通跳跃/冲刺/助推、抓爪规则、场景、资源或导入。

- `pre-green.log` 四个真实Player场景InputMap回归通过：同批次松盾新攻击在首个采样tick接受，能量99.2→87.2；
  持有效盾新点击被拒绝且仅正常持盾消耗；旧held-LMB松盾不重放；自然过热后仍持RMB，新点击在首tick接受，42.8→30.8。
  用真实输入批次，不手调 `_PhysicsProcess`、采样帧、护盾状态或能量。持续举盾自然触发过热，未缩短时长。
- 完整pre/post均退出0、错误0、既有故意异常fixture警告2/3；构建零错误警告。
- `defend-same-tick-green.log` 正常窗口、非fixed/movie：实际格挡1、反击请求/接受1、敌人伤害1、HP3→2，玩家HP5未变。
  正确新边缘在physics232接受，没有等待盾刷新的一tick补丁或自动重放。
- `counter-movie-green.log` 固定60fps硬件复现同结果：physics229接受，233实际敌人掉血。
- `observe-final.log` 保留原观察/针尖包络路径，两次完整攻击、18Active样本、两次实录伤害、最大针尖超界2.56px，退出0。
- `route-green.log` 正常窗口、非fixed/movie五区通关、四次复活、门、Boss重试和结算通过，无失焦。
  回落18下降样本0超安全边距、0道具栏交叠、落地稳定12帧。
  2774个进程回调样本：p50=16.661ms、p95=17.355ms、p99=24.954ms、max=42.518ms，8次>33.3ms、0次>50ms。
  不是GPU帧时、稳定60fps或真人上手认证，不与其他轮次混算。
- `ordinary-main.log` 普通主场景窗口240帧、`headless-main.log` plain启动均退出0，错误/警告0。
  本轮所有构建、Godot和转码明确串行；post即使打印退出0仍等待其session最终结束才运行下一项。

## 录像与独立复核

RTX3060 Vulkan固定60fps，前后使用相同输入驱动和判定：

- `counter-before.mp4`：340帧约5.67秒，SHA256 `427B3AF15572C5EF03976A02F220BD59A8D9E872231B9CD36AAAF86BBB3981A6`。
- `counter-after.mp4`：253帧约4.22秒，SHA256 `52563C51DE71C92F9603F78509676D850FF9023C61488C0ABDD992ED3412DD5E`。

旧片两次反击失败后退出，新片第一次成功后退出；时长不同来自结果条件，不代表性能提升。
主/独立对照同一首轮松盾后n228–251的隔帧4×3图板，以及举盾/起手原图：旧版没有反击序列，
新版有手臂/武器动作、敌人白闪受击和回收，盾弧随收盾消失。样本没有新增严重脱肢、穿台或遮挡；近身手臂和武器仍交叠。
只抽查指定序列，不声称逐60fps审全片或真人审美/手感验收。
独立代码复核未发现阻止提交的问题；新集成测试的正常/断言失败路径退订、释放输入、QueueFree并等待两次physics。
fixture构造在try外，未来缺少必需玩法节点导致构造失败时尚无完整清理保障；非本轮现存场景问题，不称穷尽异常资源测试。

入口：`FirstEnemyEncounterProbe.cs -- --defend-counter --same-tick-counter`；不加后一个参数为延迟松盾对照，不加两者保留原观察模式。
`--output=<项目外绝对目录>`保存实际帧；录像加Vulkan、`--fixed-fps 60 --write-movie <唯一.avi>`。
完整回归用 `RunPrologueTests.cs -- --phase=pre/post`，正常路线用 `RunRealtimePrologueProbe.cs -- --check-ascent-return`。

## 后续

实际首战已显示另一个引导缺口：按Q后主目标/右上卡立即切到机械爪教学，举盾和反击时仍显示E抓取。
本轮先闭环输入衔接；下一轮再处理首战情境提示，不强制击杀或擅自重排主线目标。
正式美术、真人首次上手/难度、回摆舒适度、普通解压/源码一致性、旧独立认证和真实商业数据仍未完成。
日常只保存本地开发检查点，不冻结r3、不发布新整包、标签或GitHub版本。
