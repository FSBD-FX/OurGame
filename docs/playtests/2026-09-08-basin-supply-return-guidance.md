# 2026-09-08 盆地补给返程与抓附提示

本地小修，基于 `1c588517d682665fd345bfaba4ef97d9822e0832`。07:33触发周期开始取证，07:47接续完成同一主题，
没有按触发次数另开功能。最终提交与录像绑定于外部
`E:/OurGame/Knight/.evidence/september08-basin-supply-return/CHECKPOINT.md`。
不冻结r3、不生成交付ZIP/标签、不push或上传GitHub，不涉及付费/新资产。

## 事实、推理、风险

- 既有补给已可从EntryCatch高台下落领取，但此前未实走从主地面重返。
- 实际主地面起点(2380,2014)，光标(2380,1632)命中EntryCatch底面(2380,1636)，初始绳长378.0005，处于620px射程内。
  W收绳把身体抬至y1877.9193；D加一次附着Space后保持左键至助推实际应用，再松手向右落SalvageFloor。
  随后走到补给并向左返回主地面。这条现有路线可用，不能将未上台先归因于牵引不足或据此扩大地图。
- 真实复现的是提示冲突：任务仍为LearnShield时，抓附Swing和收绳Reel都显示RMB盾卡与两个盾牌目标。
  原模型附着教学的目标白名单遗漏了盆地LearnShield；实际Controller已提供正确爪状态与锚点。
- 因此仅修情境教学，不改普通跳跃、一次助推、收绳速度、碰撞、敌人、补给/存档、相机或输入规则。
  操作路线可达不等于真人能找到它，也未证明它是唯一可达方式或必须使用助推。

## 生产与测试范围

`PrologueGuidanceModel`只增加一个窄覆盖：CanyonBasin + LearnShield + Boss None/Inactive +
Latched/Reel/Swing/Boost时，复用既有双语`guidance.grapple_attached`与唯一`ClawAttachment`。
真实目标由原Controller依当前锁定目标/ID/AnchorPoint解析，不是额外固定导航点。
释放或非附着状态重新计算旧盾卡，真实Completion任务不被写入。

关闭指导、Complete、Boss活动/暴露链条、其他扇区及神殿任务不受这个覆盖影响。
原失败两次后的盆地Hint、局部提示标题和左上任务标题保留；展开Hint仍可能包含盾牌/RMB内容，
不宣称整张重试卡已清除盾牌文案。本轮只让即时操作正文符合当前爪状态。

单元测试先写后改生产：双语×全部爪/Boss枚举、扇区隔离、关闭/Complete、神殿目标、改绑替换、
失败Hint保留与释放恢复。原“盆地附着不替换盾卡”断言明确迁移，不是悄悄删除失败边界。

`BasinExplorationProbe --from-ground`扩展原探针；默认高台路线保留。
预入树唯一位置设置为作者检查点，公开API恢复中段LearnShield并扣1HP准备缺血；不是实战受伤或从关卡入口全程探索。
入树后通过InputMap与鼠标事件、真实自动物理/AI/呈现前进，不写位置/速度/爪状态、不手工RefreshNow/Present。
非实体键鼠、非真人。finally松开输入、退订并等待场景音频清理；GUID隔离存档仍会留在user://。

## RED与GREEN

- `normal-red.log` / `movie-red.log`：全程抓附1、助推1、松爪1、缺1回血1、返回完成；
  唯一失败为latched（实际采样Swing）和reeled提示仍是盾卡，`correct=False`，而文字容纳检查`complete=True`。
  两者退出1，ERROR1/WARNING1。中段入口EntranceCombat缺失是已知fixture警告，没有过滤。
- `pre-red.log`：新模型测试精确失败于ChineseSimplified/Basin LearnShield/None/Latched缺A/D，退出1；
  上一周期初次Probe误引命名空间导致编译错误已修复，不作为生产RED。
- `normal-cn-green.log` / `normal-en-green.log`：按普通窗口、非fixed/movie命令，四阶段指导正确且正文容纳检查通过，
  一次真实EntryCatch附着、一次Space Boost、一次释放；缺1HP恢复1、内存记录salvage_cache、回到主地面保持HP5。
  普通Jump0、Dash0、RecoveryRequested0，退出0/ERROR0/旧中段WARNING1。支撑断言是12个不同physics采样，非保证无漏采连续tick。
- `movie-green.log`同路线退出0，ERROR0/旧中段WARNING1。固定60轨迹：physics58附着、123收至y1877.9193、
  125助推实际应用、127松爪、177落台支撑、225走到补给、339回主地面支撑。
  规范化Settle日志说明后，前后21条完整阶段/爪状态/位置/速度/绳长/治疗记录有序一致；只代表这条路线，不推导全状态等价。

运行探针只在Swing/Reel与落地/返回Idle采样教学，瞬时Latched/Boost/Released覆盖主要来自模型单元测试。
本轮补给Probe只断言内存发现，不自己读盘；真实持久化仍由常规pre中的既有补给集成回归验证。
Label检查可见、包含正文、矩形容纳与高度，不等于所有分辨率/缩放无横向裁切/世界遮挡。

## 视觉审查

RTX3060 Laptop硬件Vulkan固定60fps，前后各345帧约5.75秒，不是正常帧时或真人证据。

| 片段 | SHA256 |
| --- | --- |
| before.mp4 | `822A27108C14CFA810D836EDA2BB727B73A1900F9429EADE25DB29C17EBAE511` |
| after.mp4 | `FD82EEDBA041419ECB9BBD8FA73E50D7D244278973EB136577FC9BA82E8C25FA` |

主代理审看前后6Hz图板、前后收绳原图、英文抓附原图；独立审查另看前后各五阶段原图及英文收绳，总14文件。
新卡中英文完整；目标移到真实上方锁点，样本不挡锚点、绳、角色或落台边缘；落台盾卡恢复，领取显示生命+1/HUD回满，
返程开箱/已回收可见。不是逐60fps审完整片。灰格包含fixture退出，末黑格为补位；原AVI仅外部保存。
金色目标与青锁点叠合、左上盾牌任务标题与爪卡信息竞争、下层敌人武器与台上角色局部叠合仍在。

## 接续与发行边界

本轮仅恢复正确即时指引，不是新增探索关卡、正式美术或真人首读验收。旧Task6四类机关发行认证仍要求4，当前3，
审计与发行链未修改；pre/post通过不能替代旧门槛。r2及已上传历史归档不变。
下一轮转向盆地隔台敌人的攻击预判与探索遭遇：先实走辨别真实伤害范围、台面遮挡与收益，
避免继续只搬教学卡、重复加回血箱或为凑四类机关塞回假平台。
SupplyCave空奖励、正式美术、独立Boost动作、真人理解/难度、旧认证/解压一致性与真实商业数据仍缺。

## 串行回归收尾

下表均由进程工具确认退出后才启动下一项，全部构建/Godot/转码作业串行。无资产/场景改动，不重新导入或重生成场景。

| 记录 | 已观察结果 |
| --- | --- |
| build-green.log | dotnet build退出0，错误0/警告0 |
| pre-green.log / post-green.log | 完整两阶段退出0，ERROR0；原警告7/3 |
| normal-cn-green / normal-en-green / movie-green | 退出0，ERROR0/旧中段WARNING1；一次附着/助推/释放，HP4→5、返回，四阶段提示正确 |
| entry-regression.log | 原默认高台下落路线仍退出0；HP2→4，Jump0/Dash0/Boost0/RecoveryRequested0，旧中段WARNING1 |
| route-normal.log | 正常窗口非fixed/movie命令，五区/四次恢复/两检查点/冰面拒抓/过热/门/Boss重试结算通过；退出0，ERROR0/WARNING0，无失焦 |
| ordinary-main.log / headless-main.log | 各用子进程独立APPDATA，普通窗口240帧/plain120帧，实际退出0、ERROR0/WARNING0 |

pre的7条警告是5条既有中段出生EntranceCombat缺失与2条故意异常profile；post3条是既有throwing observer、非法房间profile、
Completed→Recovering拒绝。没有过滤或删除负向测试。本轮没有重跑旧四类发行审计，它上轮已实测失败，代码及场景均未因此更改。

正常五区路线2589个进程回调采样：p50=16.661ms、p95=17.140ms、p99=26.440ms、max=55.385ms，
10次>33.3ms、1次>50ms；起始120间隔排除。不是GPU帧时、稳定60fps或真人证据，历史峰值保留。
未启用`--check-ascent-return`，不新增18个回落样本主张。日志能核对可见argv与空movie路径，但不能独立排除宿主改写参数。

正常窗口路线使用`--rendering-method gl_compatibility --max-fps 60`；新支路脚本为
`res://tests/presentation/BasinExplorationProbe.cs -- --from-ground --output=<外部绝对目录>`，英文另加`--english`。
旧路线去掉`--from-ground`；完整主线脚本为`res://tests/RunRealtimePrologueProbe.cs`。
电影对照改用`--rendering-method mobile --rendering-driver vulkan --write-movie <外部AVI路径> --fixed-fps 60`，
pre/post使用`--headless --script res://tests/RunPrologueTests.cs -- --phase=pre`或`post`。

独立代码与证据审查无本地提交阻断；视觉边界如上，不覆盖旧发行门槛。08:00 UTC进程检查无活动Godot、dotnet或ffmpeg。
本轮6个白名单文件，不暂存原beehave导入、总线、artifacts、reference导入或10个旧UID。最终精确SHA见外部CHECKPOINT。
