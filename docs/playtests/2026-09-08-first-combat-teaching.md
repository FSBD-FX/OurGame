# 首战情境提示 · 2026-09-08

基线 `6e7641dccc8365bc5592709a824ee64d2d744395`；仅本地开发检查点，不是新发布版本。
外部证据：`E:/OurGame/Knight/.evidence/september08-first-combat-teaching/`。

## 事实、推理与风险

真实主场景中，Q使学习目标推进到LearnGrappleMode；角色接近首敌、举盾和反击时，
右侧教学卡与左上标题仍要求选择E机械爪。原暂停操作表已有RMB说明，缺的是当前交战中的指引。
临时替换Completion目标会破坏顺序；缓存旧标题后恢复也会覆盖期间已推进的新目标。
因此保留真实目标状态，增加独立的情境观察与最终显示覆盖，不强制击杀。

## 修改范围

- Controller观察入口首个锈壳侍从：攻击模式、爪Idle、双方存活可见、同高≤100px、敌人在屏内且真实LOS。
  横向距离≤340px进入，≤400px维持，避免在一个阈值处来回闪动。没有修改敌人感知或物理射线规则。
- Model只在入口早期四种目标、Boss None/Inactive、爪Idle时选首战卡；不改变后续Boss/附着教学。
  卡片不带旧路线目标箭头、不混入旧失败Hint；失败两次后仍保持紧凑卡，离开后普通失败提示照常展开。
- `ContextTitle`只影响FormalHud最终标题显示。普通任务模型、目标过渡计时及Completion进度不受覆盖；
  恢复/结算/Boss界面优先。关闭指导、切机械爪、离开、遮挡或敌死时显示最新普通任务。
- 双语卡复用InputMap和本地化资源：Q选攻击、1选武器，鼠标瞄准，收针时持RMB，刺后松RMB再新点LMB。
  英文初稿实景7行，收短为5行；中文也是5行。未新增资产、付费生成、机制、地图、数值或战斗动作。

## RED / GREEN

| 记录 | 实际结果 |
| --- | --- |
| `guidance-red.log` | Q真实输入后LearnGrappleMode，实际卡片教机械爪、标题选择机械爪；缺RMB反击说明，退出1 |
| `pre-initial.log` | 新卡已出现，旧测试的标题词列表没认可“敌人”；这是断言过窄，不作生产RED |
| `retry-card-red.log` | 两次真实Recovery，failure=2/context有效，但expanded=True；紧凑卡断言失败，退出1 |
| `pre-verified.log` | 两项修复后的完整pre退出0、0错误、3警告，重试卡expanded=False |
| `post-final.log` | 完整post退出0、0错误、3条既有负向fixture警告 |

pre的3条警告包含2条旧故意异常，以及新测试暴露的旧问题：在入口按E推进ReachBasin后，
原模型直接索要只属于下降区的DescentIce，当前区域解析失败。映射本轮未改，警告没有过滤，列为下一轮优先项。

单元覆盖双语目标×区域×Boss×爪状态组合、关闭/完成隐藏、旧Hint抑制和自定义四键映射。
运行集成加载真正主场景，使用隔离save/settings，入树前近敌摆位，ProcessFrame投递Q/E真实InputEventAction，
之后由实际控制器更新。滞回、隐藏/显现、不同高度、临时World墙和致死伤害是明确的测试安排，不冒充玩家行走。
E后真实目标推进ReachBasin，再Q显示情境，退出恢复新的盆地目标而非旧机械爪目标。
两次真实Recovery后重新获取挂载敌人、安排近敌位置、真实Q；先确认计数和context，再断言紧凑/普通展开恢复。

8组布局=1280×720/1920×1080 × 中/英 × UI缩放0.8/1.5。
读取挂载Label的真实成形高度、标题宽度、卡片屏内与标题避让；每组还关闭/启用指导，
按中英literal分别核对普通标题与不变的HUD模型。这是headless的实际Control测量，不是8组人工观感验收。
finally恢复窗口/ContentScaleSize，释放Q/E、临时墙、退订恢复事件及QueueFree；GUID磁盘残留仍在。

## 真实运行与画面

- `encounter-chinese-final.log` / `encounter-english-final.log`：正常窗口、非fixed/movie，两者均实际格挡1、
  反击1、敌HP3→2、玩家HP5。前摇、格挡、反击三阶段断言普通目标仍LearnGrappleMode且卡片/标题对应首战。
  自动输入经InputMap及鼠标事件，不是实体键鼠或真人首次学习。
- `teaching-after.log`：RTX3060 Vulkan固定60fps重复同一交战，三个阶段检查通过。
- `route-green.log`：正常窗口非fixed/movie，五区、四复活、门、Boss重试/结算通过；没有失焦。
  回落18下降采样0超安全边距/0道具栏交叠，落地稳定12帧。
  2777进程回调样本p50=16.662ms、p95=17.192ms、p99=25.182ms、max=45.995ms，
  9次>33.3ms、0次>50ms。不是GPU帧时、稳定60fps或真人手感验收。
- `import-final.log`、`ordinary-main.log`普通窗口240帧、`headless-main.log`plain启动：退出0，错误/警告0。
  最终构建0错误警告。构建、引擎、转码串行；post进程即使已打印退出0，也等待session结束才继续。

前后中文录像各253帧、约4.22秒；同一自动输入路线与格挡反击判定：

- `teaching-before.mp4` SHA256：`52563C51DE71C92F9603F78509676D850FF9023C61488C0ABDD992ED3412DD5E`。
- `teaching-after.mp4` SHA256：`61674FCD3812C46B16D895F22D83D06057A6BB923C386CC56EDBA3C3C416478D`。

主/独立审看每秒2帧概览以及举盾、反击原图：新版近敌后显示战斗卡，已采样阶段未见反复切换、
移动或闪隐，未新增遮住人物、盾弧、武器、平台或主要HUD。英文最终普通窗口原图确认五行卡片与标题完整。
中文录像后只收短了英文文案，中文卡与运行逻辑没有再变。
概览末格为排版补空白，不是游戏黑屏；不是全片逐60fps审查，也不能排除采样间瞬态。
英文旧ENERGY标签尾字被能量条覆盖仍可见，不能称整个HUD无缺陷。独立代码复核无阻止提交问题。

运行入口：`FirstEnemyEncounterProbe.cs -- --defend-counter --same-tick-counter --assert-combat-guidance`，
加`--english`经真实flow.ApplySettings切英文，仅语言变化；`--output=<项目外绝对目录>`保存实际帧。
完整回归为`RunPrologueTests.cs -- --phase=pre/post`；正常五区路线加`--check-ascent-return`。

## 后续与边界

下一轮先修入口E后的路线目标缺失，再检查英文资源标签排版。不要重复本轮卡片、松盾、针剑或镜头修复。
正式美术、独立Boost动作、真人首读/难度、回摆舒适度、普通解压/源码一致性、旧独立认证及商业数据仍缺。
保留r2与历史GitHub归档，不冻结r3、不发新ZIP/标签/Release。本轮新增测试UID随源码保存，
导入产生的其他旧测试UID和既有无关脏文件保留但不批量暂存。最终精确提交绑定在外部CHECKPOINT.md。
