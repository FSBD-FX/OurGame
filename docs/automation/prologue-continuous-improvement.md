# 序章持续优化与版本归档

更新：2026-09-08。用户确认先用 2–4 周验证一个 5–8 分钟品质样板，再依据实际制作产能和真人反馈重估完整序章。
允许评估改变游戏内各项设计，但不默认推倒重做，不自动批准付费、外测招募、公开发行或对外内容承诺。
原有重大体验提升后冻结版本、编号并在 GitHub 存档的授权与安全边界继续有效。

自动化最新状态（用户“设立目标并进行”之后）：`automation-9`，名称《无名钟楼》序章持续打磨与版本归档，已通过正式工具恢复为 **ACTIVE**；
调度间隔仍为每 10 分钟，附着于当前任务 `019ff213-88ef-7af2-8cd5-61ec3ff4bf9c`，完整字段读回一致。已建立品质样板活动目标。
本节实时状态优先于早先 PAUSED 记录；没有恢复任何无关项目的暂停自动化。

## 当前阶段：品质样板，先验证再扩展

- 最新入口开发检查点 `5a0ba3b7a67de5f1cdfadf531aece3ebd000466e` 已完成独立审查：样板正常入口、真实初始盆地检查点、独立存档／设置／记录、暂停返回标题、双语开发中标识；构建、专项、普通窗口、短录像、默认主场景启动和完整 post 通过。post 的 3 条刻意异常警告与基线相同，旧相机 pre RED 不被覆盖。详见 [入口证据](../playtests/2026-09-08-basin-sample-entry.md)。
- 样板回路新增进展：已生成独立 Basin 样板移除左侧 Rust，复制 Laborer 调参为 200px 感知／追击脱离范围，营地等待安全、主动走近接战。其他战斗时序与默认整关不变。正常窗口及硬件录像完整走完挡盾→开甲→击败→EntryCatch 抓台助推→满血补给保留→归营，且修复实际暴露的 Q 后攻击误报机械爪 AlreadyDeployed。详见 [回路证据](../playtests/2026-09-08-basin-sample-loop.md)。
- 样板专用阶段卡和世界目标反馈已接入：蓄力/开甲/反击、真实底面抓取及落台、补给保留、归营独立反馈，暂停/标题/恢复边界受控；专项及中/英放大 UI 正常整圈通过，实图暴露的键位省略已修正。详见 [逐步引导证据](../playtests/2026-09-08-basin-sample-guidance.md)。
- 下一轮应推进同场劳动者和角色的起手/执行/恢复姿态、轮廓与反馈，然后继续场景、UI、声音统一；不要停留在反复改写卡片。约 14 秒自动录像不是 5–8 分钟真人内容；禁止复做已通过入口、扩大整关或以等待凑时长。统一品质、独立包及用户评审仍未完成。
- 已确认的范围与验收边界见 [品质样板阶段约定](../production/2026-09-08-quality-sample-brief.md)。
- 第一目标是代表性连续空间中的可读战斗、机械爪探索和明确奖励，不是追加整关地图、敌人数量或反复调整提示卡。
- 2–4 周是资源就绪并恢复执行后的低置信验证窗口；当前没有已落实的外部美术／动画团队、报价或新玩家样本。
- 盆地右半部“营地—战斗—机械爪上返补给台—返回”路线已获执行批准，不重复询问是否开工；先按实施计划打通正常入口与恢复，再完成整圈和实际运行的视觉样板。未批准的美术不批量铺满整关。
- 用户和首次玩家的主观验收必须分开记录，失败可以要求删减或重设计。自动化测试、代理评分、文件数量不能代替这些结果。
- 当前生产基线 `3b1406bf6a532c4f7ec5a0b5c74cb54b9ca2a357` 保留；相机接缝 probe 与集成测试草稿、RunPrologueTests 注册改动仍未完成，不能记为 GREEN。
- 原旧四类发行认证缺口继续公开保留；如果新设计需要改变契约，须明确记录替代要求和回归影响，不能通过修改计数断言伪造旧版验收。
- 首个样板评审后再估算后续。4–8 个月只是在稳定二维美术／动画协作、每周试玩、有限 PC 序章范围下的低置信规划；现有 AI＋用户为主的 6–12 个月以上也不是保证。

## 目标与真实证据

- 长期商业目标：获得 100,000 名愿意付费订阅后续内容的玩家。这是待验证目标，不是现有成绩或可保证的结果。
- 当前没有已核验的真实玩家样本、价格与订阅承诺、获客渠道、意愿转化或续订数据。不得把缺失数据记为零，也不得凭代理评分估计付费人数。
- 将试玩完成、首次掌握操作、重复失败点、重玩/探索、明确价格下的自愿付费意愿、实际订阅和续订分别记录，标明日期、样本数、去重与来源；意愿不等于付费，机器人不计入真人样本。
- 没有商业数据时，继续可验证的产品改进，但不宣称接近十万目标。需要外部试玩、定价、发行或推广决策时向用户提出具体需求；不自行投放广告、收费、承诺后续交付或采集上传个人数据。

## 自动迭代方式

现为 ACTIVE，默认每 10 分钟在当前任务触发推进一轮，不保证每轮在 10 分钟内完成；实际执行受本机在线、运行资源与账户额度限制，不代表全天无人值守算力已获保证。

每轮先读本文件、README、godot.md、最近版本/试玩记录，核对工作树、目标状态和正在运行的进程。
按事实、推理、风险区分当前最重要问题；只选一个体验主题，做 1–3 个可验证改进。
先排除当前样板的卡关／崩溃／恢复阻塞，随后按完整体验交付推进关卡、动作、场景、声音与 UI；美术与动作不再无限推迟到所有小修之后。
每周以样板的一段完整游玩体验评估优先级；没有用户／新玩家证据时标为候选，不以提交次数或触发次数判定质量提升。
不要把长期优化变成反复维护测试工具或只写报告；基础诊断能支持判断后回到玩家可感知的改动。

保持峡谷遗落机械神殿、多密闭多场景多层级、连贯 U 形主线、复杂但易上手的方向。
复杂度来自有意义的路径、敌人与场景互动，不靠地图堆大、重复跑腿或增加记忆负担。
Q/E 模式、鼠标执行/护盾、机械爪抓墙抓顶与 Space 助推是当前基线，不是不可修改的设计禁区。
如样板证据支持修改，先说明学习收益、代价及回退方式并确认具体方案，再同步输入、教学、关卡与回归；不在后台无提示更换控制。
可新增与既有技能组合的原创小机制，但先做可回退原型/可选支路，说明收益、学习成本、失败回退和验收。
未经验证不把实验强塞进必经路线；没有普通二段跳的路线不能暗中依赖它通关。
既有美术替换接口、许可与免费边界继续有效，首次新增付费资源/服务仍需用户明确批准。

## 验证与重大提升标准

修复先复现；保留失败及通过证据。按影响运行测试、真实窗口的正常速度路线、普通启动与短录像复核。
保持主线可达、合法抓取/禁抓、一次助推/空中冲刺边界、能量与护盾过热、死亡检查点、门与下一场景衔接、Boss/结算回归。
固定帧录像和自动驾驶不能充当正常帧率、真人首次上手或市场认可。失焦清键造成的测试中断单列，不能抹掉或误归为关卡问题。

日常小修是开发检查点，不因定时触发就发布一个“重大版本”。重大体验版本须同时具备：

1. 相较上一里程碑，整段试玩有清楚的体验差异，例如核心移动/战斗循环明显改进，或完整且非重复的探索遭遇组合；改色、补日志、单个小修不够。
2. 该差异有前后运行证据、可重复玩法检查与独立审查，没有新的严重卡关或基础机制回退。
3. 普通解压启动可用，保留既有尚未完成的认证要求；初次上手与主观体验未获真人反馈时明确标为候选，不能代签验收。
4. 说明新增复杂度是否值得、删减了什么负担、已知问题及下一轮待验证假设。没有改善则不升级正式里程碑。

## 版本与 GitHub 归档

- 当前项目：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。不要在旧 `F:/Documents/游戏` 工具目录重造游戏。
- 已配置远端：`https://github.com/FSBD-FX/OurGame.git`。每次上传前重新核对仓库身份、可见性、权限、已有分支和标签；不擅自新建公开仓库或改变可见性。
- 开发候选沿用日期编号；正式体验里程碑用 `prologue-m001`、`prologue-m002` 等。发行候选明确标 `rc`/prerelease，不假装最终验收。
- 每个被命名并交付的版本绑定精确已提交 SHA、版本说明、带说明的唯一标签、源码 ZIP、可运行包（若已具备）、SHA256 清单、精简自包含证据与已知问题。
- 所有已命名版本最终均需远端存档；日常未命名提交不等于正式版本。r1/r2 本地历史包可在核验来源后补归档，明确是历史开发候选，不计作新体验提升。
- 上传前筛查秘密、个人信息、无关文档和资源许可；使用交付白名单，排除 `.git`、`.godot`、`.worktrees`、私有任务状态、原始大录像、缓存和 SDK。保留实际运行所需的场景、资源、导入设置及许可。
- `docs/automation` 属于本地执行记录，默认不进对外归档；对外更新说明另写 `docs/releases`，不夹带未公开商业计划或真人试玩原始数据。
- 只上传确定的提交/标签和对应文件，不盲推整个脏工作树。不强推、不移动旧标签、不删除旧版本、不覆盖同名附件；普通快进也先核对远端状态。
- GitHub Release 必须包含玩家能理解的更新内容和附件；附件不能只有本机绝对路径。上传后读回标签 SHA、Release 和附件信息并核对校验值，才记录“已归档”。
- 连接或权限失败时，保存本地唯一版本和待同步清单，继续独立可做的本地工作；远端归档保持未完成，不伪报成功，不无限快速重试。

### 2026-09-08 连接修复与历史归档完成

早先 `Connection was reset` / `USER_NOT_LOGGED_IN` 是当时的检查结果，现已被以下实测替代，不要继续报告尚未连接。
GitHub 连接器与现有 Git Credential Manager 身份均已可用；仓库 `FSBD-FX/OurGame` 为 private，具备 push 权限。
本机 Git 直连 github.com 超时，而现有系统代理 `http://127.0.0.1:7897` 可用。
已在 `E:/OurGame/Knight/.git/config` 增加仅此仓库的 GitHub 代理项，不是全局设置、没有关闭 TLS 校验；
该仓库的工作树共享此配置。后续若代理关闭或端口改变，应重新检查，不能认为此通道永久可用。

独立归档分支：`codex/prologue-archives`，仅包含两个经过筛选的导出提交，不带原始私人开发历史。
`main` 保持 `2d7b0440a012cac87019e273d90e8aaeb670faeb`，未变更默认分支或可见性。

| 历史版本 | 原开发 SHA | 独立筛选归档 SHA | 标签 / Release |
| --- | --- | --- | --- |
| 2026.09.08-r1 | `719af81d9f7880a0035976427e3957b0270e9510` | `1d35aa5ff3d375bb0859b09db020ed1fcc8ca215` | [prologue-2026.09.08-r1-archive1](https://github.com/FSBD-FX/OurGame/releases/tag/prologue-2026.09.08-r1-archive1) |
| 2026.09.08-r2 | `1da26b6bd4732ef1967dc0a01de0b726c5b8938f` | `a45eab72d047e94b894f5c88bbb394b17abfc9fd` | [prologue-2026.09.08-r2-archive1](https://github.com/FSBD-FX/OurGame/releases/tag/prologue-2026.09.08-r2-archive1) |

两者均为 published prerelease，未标为 latest。每版有筛选源码 ZIP、历史自动路线 MP4、脱敏证据 ZIP、SHA256SUMS 四附件。
2026-09-08 05:08（北京时间）读回仓库/标签/Release 后，已实际下载全部 8 个附件；大小、服务器 digest 和本地 SHA256 全部一致。
原 ZIP 保留未覆盖；筛选归档移除内部文档、代理状态和美术生成记录，游戏代码/场景/资源/配置逐文件保持字节一致。
补充了已与本机来源核验的 GDQuest steering 插件 MIT 许可，但没有为项目自有内容新增开源许可。

重新在独立筛选副本执行 .NET 9 编译、Godot 4.7.1 Mono 导入、普通主场景 headless 120 帧，均退出 0；r2 材质检查为 52 地形/10 抓点通过。
历史录像没有重新录制，不能用于正常速度帧率或真人验收；历史全流程 5 条警告、post 3 条警告保留。
部分保留的历史认证/美术验证工具依赖未发布的原始证据或辅助脚本，筛选包不是完整原开发认证环境，也不是免安装 EXE。

本地完整上传与下载回执：`E:/OurGame/Knight/releases/2026-09-08/github-archive-prep/remote-verification.json`。
发布脚本、计划和脱敏材料均在同一准备目录；已发布的同名附件/标签不能重写。后续需另建唯一版本并重复内容审查，不直接推当前脏树或全部私人历史。
此轮只是补齐历史归档，不计作新的游戏体验里程碑；r3 未冻结、未上传，下面接续点保持有效。

## 本次接续点（未冻结 r3）

- 已归档的本地开发基线：r2，提交 `1da26b6bd4732ef1967dc0a01de0b726c5b8938f`。
- r1/r2 包在 `E:/OurGame/Knight/releases/2026-09-08/`，不能覆盖；r2 的回执包含 ZIP SHA256 和解压验证。
- r2 之后的机械爪释放惯性开发修复不属于 r2。真实调度测试复现 -360 上升速度被短跳截至 -199.17；修复后释放仅受重力影响，Travel 取消及新冲刺边界也有 RED/GREEN。
- 续审补齐“已经冲刺，再松爪”的相反顺序：单元和真实调度均复现错误尾速 -589.1667（应为 -199.1667），修复为活动冲刺每帧清除释放保护；不返还空中冲刺，也不添加普通二段跳。新用例已接入完整 pre。
- 新驾驶使用助推后的轨迹并在真实落地后校正；`post-driver-correction.log` 完整 post 回归通过。
- 证据根目录：`E:/OurGame/Knight/.evidence/september08-r3-motion-diagnostics/`。普通窗口 trace03 记录失焦与 move=1→0，入口停止不是地形阻挡。
- 实时探针已加入采样器确定性测试和完整命令行防误用检查。`realtime-guard-fixed.log` 正确拒绝固定帧；正常运行的六项采样器测试通过。
- `realtime-background-01.log` 的失焦中断仍保留。`heartbeat-0545/focus-guard-cleanup.log` 已验证后台模式在窗口仍有焦点时明确拒绝（预期退出 1），且不再错误断开未连接信号。后台窗口隔离没有完成，不要重复盲试或改生产输入凑通过。
- 正常前台窗口现在已连续两次完整通过：`heartbeat-0524/realtime-normal-window.log` 与 `heartbeat-0545/realtime-confirm.log`，均非固定帧、无 movie writer、time_scale=1、无失焦、退出 0。覆盖五区/三段上升抓附/四次复活/门/Boss 重试/结算；自动来源不写真人报告。第二次 2610 个有效回调采样 p95=17.110ms、p99=21.984ms、max=57.208ms（一次超过 50ms），不能宣称稳定 60fps。
- `heartbeat-0524/pre-final.log` / `post-final.log` 均退出 0、无错误，保留负向测试产生的 2/3 条警告；独立审查已核实来源。`heartbeat-0545/grapple-focused.log` 本次重跑通过。普通主场景窗口 `ordinary-main.log` 240 帧退出 0，仅为启动检查。
- `heartbeat-0545/route-motion.mp4`：硬件 Vulkan 固定 60fps，2736 帧/45.60 秒，全路线通过；另留 27–39 秒的 `ascent-motion-12s.mp4`。总览与三组每秒 6 帧图板已由主代理和独立代理查看，不充当逐 60fps 帧审查或真人操作验收。
- 本轮已完成上升区上方视野小改：只有当前扇区为 Ascent 时，相机局部 Position 平滑上移 120px，普通退出平滑返回，复活/新局/远距离切区立即复位；world 单独退出时还原作者设置。没有改 Offset、缩放、限位定义、地形、控制或资产。
- 站立可见性先红后绿：第三抓点从 `y=-35.994507` 到 `84.00549`，三个站立发射点均满足 64px 中心内边距。`september08-ascent-camera-readability` 内保留原始 standing RED、zero-headroom mutation RED 和 exit-restore mutation RED；临时变异全部恢复。最终 lifecycle 还验证非零静态 Offset，但不声称直接测试动态震动共存。
- 新 `pre-final.log` / `post-final.log` 均退出 0，错误 0，负向测试警告仍为 2/3；正常窗口 `realtime-green.log` 五区/三段抓附/四次复活/门/Boss/结算通过、无失焦，2589 个回调样本 p95=17.194ms、p99=26.968ms、max=44.455ms（8 次 >33.3ms），不是稳定 60fps。普通窗口 `ordinary-main.log` 240 帧退出 0。
- `after-route.mp4` 为硬件 Vulkan 固定 60fps 自动全路线，2720 帧/45.33 秒；`ascent-headroom-12s.mp4` 为原录像 27–39 秒原速节选。主代理审看全程总览、27/31/35 秒起的 6fps 图板及原尺寸帧；独立代理复核原尺寸前后帧与 31/35 秒图板。未见样本内新增明显遮挡，不是逐 60fps 或真人验收。代码独立复审提出的退出还原盲点及过强 shake 标记已修正并复审闭环。
- 下一轮先验证第三锚点平台“站立瞄准→抓取→主动松手回落”的正常速度可见性，确认上方预留没有牺牲回落预判；再按证据选一项下方视野、右侧空间或教学层级改进。不要因偏移增加就继续盲目加大视野；Ascent 的横向限位宽度等于视口，单纯 X 偏移不能解决贴右问题。详细证据见 `docs/playtests/2026-09-08-ascent-camera-headroom.md`。
- 普通解压启动/源码包一致性、旧认证门槛、真人上手和正式审美仍待完成；没有冻结 r3、没有新标签/Release。详细命令及证据边界见 `docs/playtests/2026-09-08-grapple-release-motion.md`。
- 不相关的 beehave 导入元数据、音频总线、用户 artifacts 保留，不批量还原、暂存或上传。

## 最新开发接续

### 2026-09-08 06:32 轮：主动松爪回落视野（本地小修）

- 本轮接续 `be98aaac8e3279a747ede0eb6f7a319c3ad3c4d3`，未重做上一轮站立抓点改动。
  第三锚点站立抓附、W 收绳上升 180px、主动松手的真实返回，已复现并验证。
- 原台面 y=1732 经核实；`return-baseline.log` 初用1714的错误取点仅作诊断历史。
  正式 RED 是 `return-corrected-red.log`：回落物理成立，18 早期下降采样中11个超48px安全边距，
  首下降台面 y=706.3318/720。不是完全出屏、不是坠落卡关。
- 当前只在 Ascent、空中、现有释放惯性标记有效、抓爪 Released/Retract/Idle/Failed 时随上升减速回中；
  站立或新 Aim/抓取恢复120px目标，使用原指数平滑。没有改地图、运动参数、Zoom、Offset 或资源。
- `return-realtime-green.log` 正常窗口无固定帧/movie writer/失焦，完整五区、专项返回、4次复活、门/Boss/结算通过。
  18个下降采样均在48px边距内；首下降台面643.73486，和正式RED同世界位置/速度。
  2776回调样本p95=17.284ms、p99=23.932ms、max=42.044ms，5次>33.3ms，0次>50ms；
  非稳定60fps认证，新增支段不和旧路线样本混算，保留57.208ms历史峰值。
- 真实回落专项必须带 `--check-ascent-return`，不宣称默认主路线覆盖此分支。
  默认post新增fresh Aim相机消费端fixture；第一次立即手动采样准备失败单列，改为真实调度后通过。
  `fresh-aim-whitelist-red.log` 删除白名单后命中指定方向断言；finally已还原并重新build。
  `pre-final.log` / `post-final.log`均0退出0错误，负向警告2/3；原lifecycle及新FRESH_AIM均通过。
  `ordinary-main.log`普通窗口240帧退出0。未改生产失焦清键或输入规则。
- `return-route-green.mp4`硬件Vulkan固定60fps，2902帧/48.37秒；`return-camera-10s.mp4`是32–42秒原速片段。
  主/独立代理审看前后原尺寸帧、33/37秒6fps图板与总览；落点更清楚，角色及后续攀升可见。
  道具栏仍盖台面左端小段；主要落点不受挡。没有直接逐60fps播放验收，落地后回摆舒适度待真人。
- 完整记录：`docs/playtests/2026-09-08-ascent-return-camera.md`；外部证据：
  `E:/OurGame/Knight/.evidence/september08-ascent-return-visibility/`。
  最终精确SHA与录像哈希由该目录CHECKPOINT.md绑定。未冻结r3、未打标签或上传；r1/r2保持原样。
- 下一轮优先处理道具栏/教学卡与落点的视觉层级，根据运行构图做一项局部改进，不继续盲移相机。
  原速真人落地停顿再抓取、旧独立认证、普通解压启动与源码一致性、正式审美仍待完成。

### 2026-09-08 07:00 轮：道具栏右下停靠（本地小修）

- 接续 `d3928efb298708e0e83015afc8a7780cf2cdcb01`，只改Loadout横向停靠，不动Q/E、字体、尺寸、相机、输入或运动机制。
  左下相邻方案经独立审图发现会盖AnchorOne后未实施；改为默认右下 `(1012,644,220,36)`。
- `september08-loadout-dock/route-hud-red.log` 正常窗口复现18个早期下降采样中5个HUD交叠，最大3600px²，预期退出1。
  `ui-layout-red.log`独立矩形断言也失败。GREEN同专项18/0，真实LandingTwo碰撞形状和HUD实矩形投影相交，不改轨迹。
- 四分辨率×三档缩放的界面布局、模式/四槽标签及原有状态回归通过。
  初次新增小字号测试错误地要求字高完整放入无裁切背景，纠正为实际HUD根/横向栏位/真实裁切边界，保留日志，不缩字号。
  focused最终0错误1条既有音频fixture目标/扇区不同步警告；完整pre/post 0错误，负向警告2/3。
- `route-hud-green.log`正常窗口五区/4次复活/门/Boss重试/结算通过，无失焦/错误/警告。
  2775回调样本p95=17.271ms、p99=25.312ms、max=41.648ms，7次>33.3ms，不是稳定60fps或真人认证。
  普通主场景240帧通过。Vulkan固定60fps录像2902帧约48.37秒，最终投影版专项及全路线通过。
- 主/独立抽帧复核目标落台顶边露出、关键角色与抓点可辨；右下其他平台末端仍有局部遮挡转移，UI1.5也未解决世界遮挡。
  不把限定18/0说成全路线无遮挡。独立代码审查的Canvas/FinalTransform坐标混用已修正并闭环。
- 详细记录 `docs/playtests/2026-09-08-loadout-dock.md`；外部目录 `E:/OurGame/Knight/.evidence/september08-loadout-dock/`，
  10秒原速短片为 `loadout-return-10s.mp4`，精确开发SHA/哈希由CHECKPOINT.md绑定。没有冻结r3、新zip、标签或GitHub上传。
- 下一轮优先核查入口教学密度，依据真实流程做一项减少阅读负担的改进，不继续只搬HUD或微调相机。
  解压启动/源码一致性、旧独立认证、真人上手和正式审美仍待完成；保留无关导入/音频总线/artifacts。

### 2026-09-08 07:31 接续轮：机械爪分阶段提示（本地小修）

- 接续 `7f24566de6c51dec910f0e189dbc2508a0df9254`。上轮已写未完成测试，本轮继续而非重做。
  未附着LearnGrapple卡只讲E/瞄准/按住左键；真实Latched/Reel/Swing/Boost在四个遍历目标中显示
  A/D、W/S、Space一次、松手。目标推进、失败hint/chrome/targets、Boss/机关/盾、关闭/完成不变。
- `september08-contextual-claw-guidance`保留正常路线RED（真实Latched仍说进入神殿）与focused RED（未附着提前给W/S）。
  最终focused/pre/post通过，错误0、警告0/2/3；后两组均刻意异常用例。导入、普通窗口240帧0错误警告。
  四输出分辨率90状态/270图通过，无错误警告，不冒充所有UI缩放或真实抓取。
- 正常窗口完整五区/4次复活/门/Boss重试/结算通过，真实Latched/Reel卡挂载可见，Released且目标尚存就恢复路线。
  2772回调样本p95=17.189ms、p99=24.513ms、max=44.290ms，6次>33.3ms，不是稳定60fps或真人认证。
  原回落专项仍18/0安全边距、18/0道具栏交叠。Vulkan固定60fps完整录像2902帧约48.37秒，短片32–42秒。
- 主/独立审图：同2秒构图入口卡7行→3行；34秒真实绳+完整附着提示、35秒松手恢复。
  33/37秒6fps图板及真实原图已复核，主代理另看结束结算图板；不是逐60fps或真人体验。
  双语展示卡完整，但下降正文/hint重复仍在；人工模型卡的中文周边HUD、Q高亮/角色出屏不能当真实联动证据。
- 独立代码审查无P0/P1/P2问题；英文A/D定向覆盖P3已补中英断言和自定义左右键映射并独立复核。
  真实route手动RefreshNow证明状态消费/挂载Label，不单独声称自动_Process时序测试。
- 重要新发现但未在本轮改动：37–38秒绳不可见并非提示残留。`route-rope-diagnostic.log`复跑完整路线通过，
  第三锚点loop53..148连续96帧Reel/有效目标/LMB按住、牵引继续，但EffectiveVisibility=false、line Idle、strands=0。
  距固定锚点约190.53–412.87px<620，源码定义指向LOS失效；具体遮挡碰撞体尚未定位。
  原ThreadLineVisual因不可见清整条绳，而模型继续附着；相关代码早于本轮。
  下一轮优先修复附着关系的连接反馈：先明确遮挡表现规则，保留发射LOS/禁抓/约束/真正松手清理；
  不顺手加自动脱钩，也不未经验证让完整绳穿墙。暂停继续挪HUD或微调镜头。
- 详细记录 `docs/playtests/2026-09-08-contextual-claw-guidance.md`，外部同名证据目录CHECKPOINT.md绑定最终SHA。
  仍未冻结r3、无新ZIP/标签/GitHub上传。既有认证、普通解压/源码一致性、真人与商业数据缺口均保留。

### 2026-09-08 08:05 轮：机械爪遮挡反馈（本地小修）

- 接续47a6678，生产只改resolver/Line/Shuttle；不改模型、控制器、输入、镜头、地图或美术资源。
  遮挡时近侧单绳沿首碰法线退4px、末段24px渐隐，局部收绳环与远端锁定标记保留；
  源点内部/距离过近/无可靠截断点仅保留局部提示。恢复视线还原三股绳，松手/卸载清理。
- focused真实插墙RED与正常窗口第一上升平台RED均保留。实际首碰已定位：
  第一段LandingOne、第二段CatchThree、第三段LandingThree；第三段frame origin(3791.7048,1694.8181)，
  hit(3737.0593,1474)。不再说具体遮挡体未知。
- 独立审查P2发现原LOS忽略内部起点，已用inside+EffectiveVisibility=true真实fixture复现RED。
  修复为每个attached帧独立视觉射线（HitFromInside=true），原准入false不变；
  Line保存视觉状态，Shuttle读取owned child最新结果，不依赖订阅先后。独立复审闭环。
- 最终focused/pre/post通过，错误0、警告0/2/3。新增测试覆盖截断/渐隐/恢复/释放/卸载、
  超距、掠射角、内部起点、自身目标排除与状态边界，未用普通二段跳补路线。
- 最终正常窗口 occlusion-route-final.log 完整五区/4次复活/门/Boss重试/结算通过，无失焦/错误/警告。
  三段视觉遮挡采样分别63/90/117，截断51/78/92，局部提示12/12/25；
  其中10/10/21个原模型可见但视觉仍需遮挡。物理路线落点与前版一致。
  2772有效回调采样p95=17.190ms、p99=23.687ms、max=42.803ms，9次>33.3ms、0次>50ms。
  多一次附着射线的成本仅由该次正常窗口数据记录，不宣称稳定60fps或零开销。
- 详细记录 `docs/playtests/2026-09-08-claw-occlusion-feedback.md`；外部目录
  `E:/OurGame/Knight/.evidence/september08-claw-occlusion-feedback/`，CHECKPOINT.md绑定最终SHA与录像。
  第一版候选录像/内部RED保留，不与最终版混用。不冻结r3、不打新包或上传GitHub。
- 最终硬件Vulkan录像2902帧约48.37秒、普通窗口240帧启动均退出0、无错误警告。
  上轮/首版/最终各582条loop trace去掉仅显示字段后无差异；不将日志等价扩大为全状态物理证明。
  主/独立复核37.3短绳、38.5内部清线、38.7仍附着恢复三股绳及35松手清理；39.1已松手，不能充当LOS恢复。
  图板/原图样本内无新严重遮挡，但局部青环偏小易混入手部造型，不能称为独立强提示。
- 下一轮优先角色收绳/抓附动作与局部提示可辨识性，依据真实运行图区分手部/肩部装饰/悬浮灯与连接反馈。
  不继续只挪HUD或加文字，不盲改牵引强度。范围保持局部且可回退，先复现辨识问题再决定最小表现改进。
- 正式美术/真人上手和手感/旧独立认证/普通解压与源码一致性/真实商业样本继续未完成；
  保留无关导入元数据、总线、artifacts及上轮测试UID，不随白名单检查点提交。

### 2026-09-08 08:45 轮：手腕连接提示（本地小修）

- 接续aa1d6b8，生产仅改ThreadShuttleVisual。独立局部cue z10绑定真实NearArm手端(0,21)，
  双弧半径9→12并加窄描边，去掉中心金点；父Shuttle/Line/目标层级与物理起点不动。
  绳长驱动旋转、原附着/遮挡白名单、恢复LOS和松手清理保留，无新付费资产或场景生成。
- 新像素Probe加载真实Formal场景，四姿态手腕环带可辨青色0/0/0/12→130/135/131/135；
  静止不自转、收绳转动、release/Clear无残留。人工展示帧不冒充真实输入或全动作验证。
- 独立审查提出两个P2均真实复现：旧rig脱树但存活时替换手腕像素0，RequestReady重入child2→3。
  修复为每次绘制解析当前可选手臂路径、重用唯一owned cue。最终替换135像素、重入child2且身份相同，复审闭环。
- 最终focused/pre/post退出0、错误0、既有负向警告0/2/3。普通窗口240帧及plain headless启动0错误警告。
  正常窗口五区/4次复活/门/Boss重试/结算通过，无fixed/movie/失焦；原返回专项18/0安全边距、18/0道具栏交叠。
  2773回调样本p95=17.159ms、p99=23.455ms、max=44.948ms，8次>33.3ms；不是稳定60fps认证。
- 硬件Vulkan录像2902帧约48.37秒，日志无错误警告；与前轮634条完整GRAPPLE_TRACE按顺序完全相同。
  主/独立同时间原图及图板审看：纯局部提示更清楚，未见新增遮脸/挡平台，恢复完整绳/松手后正确消失。
  手环与身体出绳点仍分离，只能称手部状态指示；不是全部动作连续性、真人体验或正式美术验收。
- 记录 `docs/playtests/2026-09-08-claw-wrist-cue.md`，外部 `E:/OurGame/Knight/.evidence/september08-claw-wrist-cue/`。
  CHECKPOINT.md绑定最终SHA、完整和10秒短片哈希。无新ZIP/标签/GitHub上传，r2与旧证据不改写。
- 下一轮先验证既有Boost姿态映射缺口：Coordinator里Latched→ThreadLatch、Reel/Swing→ThreadApply，Boost落到None。
  这是源码发现，尚未用真实助推输入证明可见卡顿；先复现再做最小动作连续性修复，不盲改助推/跳跃/冲刺规则。
  不反复扩大青环或挪HUD。正式美术/真人/旧认证/解压与源码一致性/商业数据仍缺，保留无关脏文件。

### 2026-09-08 09:20 轮：助推抓附姿态连续性（本地小修）

- 接续ffe8280，生产仅Coordinator三处映射：Boost继续ThreadApply/Contact、线轴Contact、ThreadApplied cue。
  真实调度RED确认动作清空且下一Swing手臂从Identity重入；默认空CueScenes并无必现攻击闪光。
  没有改输入/物理/助推力度/二段跳/冲刺边界，无新美术或付费。
- 新专项加载真实Player、注入已采样输入帧，Modes/Claw/Motor/Coordinator自动调度，实际采到Boost。
  不是实体键盘/InputMap端到端测试，不手工Present或注入Boost。前后手臂 -1.8010→-0.2796 的重入
  变为保持 -1.8010→-1.7895；线轴不再瞬时松弛，配置扩展攻击cue1→0/抓附0→1。
- focused/pre/post通过，错误0、既有负向警告0/2/3；非固定帧窗口助推专项、普通240帧和plain启动通过。
  正常五区路线/四复活/门/Boss重试/结算通过，无失焦；回落18采样0超边距、0道具栏交叠、稳定12帧。
  2773回调样本p95=17.163ms、p99=23.480ms、max=47.628ms，6次>33.3ms；不称稳定60fps。
- 前后硬件Vulkan短录像各83帧约1.38秒；13条位置/速度/机械状态有序一致。主/独立查看65–76连续帧，
  旧68帧垂手再抬，新版保持；既有朝向翻转仍存在，无新严重样本遮挡。不是独立Boost动画或真人验收。
  独立代码审查无新增P0/P1/P2；资源与事件清理检查通过。
- 详细记录 `docs/playtests/2026-09-08-claw-boost-continuity.md`，外部证据
  `E:/OurGame/Knight/.evidence/september08-claw-boost-continuity/`，CHECKPOINT绑定最终SHA和前后视频哈希。
  未冻结r3、无新ZIP/标签/GitHub上传，保留无关导入/总线/artifacts/3 reference imports/4旧test UID。
- 下轮优先首场敌人攻击预兆和伤害区域的真实可读性，检查场景/UI遮挡及初遇学习节奏后选最小改进。
  不重新扩大青环或挪HUD，不凭源码推断卡顿、不盲加必经机制。正式美术/真人/旧认证/解压一致性/商业数据仍缺。

### 2026-09-08 09:53 接续轮：首敌视线与刺击范围（本地小修）

- 接续e7b809c。真实入口遭遇先复现首敌只追击1.5833px后Idle；自身AttackHitbox挡了LOS。
  只读排除该RID后命中Player/Hurtbox，非暂停/教学冻结。生产改为排除本actor全部碰撞子节点，外部遮挡不变。
- LOS恢复后真实两次攻击复现针尖过伸52.27px；只改RustShellFormalVisual的Active目标和武器前25%插值。
  26×18伤害框、0.65/0.14/0.65时序、模型、输入、素材不动。原针剑长度及阶段连续性保留。
- 两项各有实景/回归RED；最终pre/post退出0、错误0、既有负向警告2/3，双向追击、动态嵌套自Area、
  外墙/外部Area、两向202个Active进度采样和原阶段边界通过。普通窗口交战两次/两伤害/18Active，最大2.56px。
  3px为针尖中心容差，不是整剑墨像素包络。测试未强制两次伤害，结果来自本次真实日志。
- 正常窗口五区/四复活/门/Boss重试/结算通过，无失焦；回落18样本0超边距/0道具栏交叠/稳定12帧。
  2774进程回调p95=17.193ms、p99=24.042ms、max=44.146ms、14次>33.3ms，不称稳定60fps。
  普通主场景窗口240帧与plain headless启动均退出0无错误警告。
- 硬件Vulkan针剑前后片各355帧约5.92秒；9条阶段/伤害事件、40条去除针尖字段的渲染记录有序一致。
  主/独立前后218–229帧审图确认过伸消除，无新增严重样本遮挡；近身身体和武器仍重叠，主动作更小。
  独立代码复核无阻止提交的问题；旧LOS诊断查询已明确标LEGACY，普通窗口重跑通过。
- 首轮post-green与movie-green不能排除短暂资源重叠，保留历史但不作最终串行/性能证据；最终已依次重跑。
  详细记录`docs/playtests/2026-09-08-first-enemy-readability.md`；外部证据`september08-first-enemy-readability/`，
  CHECKPOINT绑定最终SHA和三个视频哈希。无新ZIP/标签/上传，无付费，保留原无关脏文件。
- 下一轮在正常操作中检查首战前摇→举盾/避让→反击是否易辨；不要扩大伤害范围或重调手环/镜头。
  正式美术、真人首次上手/难度、回摆、旧认证、解压源码一致性和商业数据仍缺，不能据通路通过代签。

### 2026-09-08 10:29 接续轮：松盾同帧反击（本地小修）

- 接续ce115e3。正常窗口实战先举盾、真实格挡，然后同一输入批次松RMB并新点LMB，两次均误拒绝ShieldActive；
  采样已是ShieldHeld=false/PrimaryPressed=true，但Combat先于Shield读取旧active。等一tick对照能实际反击命中。
- 只改PlayerActionModeController：许可与拒绝共用实际IsActive且当前ShieldHeld判断。持有效盾仍禁攻，
  过热仍持RMB可攻击，不重放旧LMB，不改起始同时RMB+LMB优先级、全局调度、能量/攻击/跳跃/抓爪数值。
- 新集成首版在PhysicsFrame送输入造成边缘拆帧，保留handoff-regression-red但不充当生产RED；
  改从ProcessFrame投递后handoff-input-red确实复现sample11误拒绝。GREEN四边界全部通过，首tick只扣12能量。
- 正常窗口/硬件录像均真实格挡1、接受反击1、敌HP3→2、玩家零伤。完整pre/post退出0，错误0，既有警告2/3。
  原观察/包络两次攻击18Active最大2.56px仍通过；普通窗口240帧和plain启动通过，无新增错误警告。
- 正常五区路线/四复活/门/Boss重试/结算通过，无失焦；回落18样本0超边距/0道具栏交叠/稳定12帧。
  2774回调样本p95=17.355ms、p99=24.954ms、max=42.518ms，8次>33.3ms，不称稳定60fps或真人验收。
- 前后Vulkan录像340/253帧（5.67/4.22秒），长度来自旧两次失败/新一次成功退出，不是性能对比。
  主/独立审同区间隔帧4×3及原图确认新版反击/受击/回收，未见新增严重样本遮挡，近身武器交叠仍在。
  独立代码复核无阻止提交问题，构造失败清理/隔离存档残留及非硬件输入等边界见详细记录。
- 外部证据`E:/OurGame/Knight/.evidence/september08-first-combat-guidance/`，CHECKPOINT绑定最终SHA和视频哈希；
  详细记录`docs/playtests/2026-09-08-shield-attack-handoff.md`。仅本地开发检查点，无新ZIP/标签/GitHub上传或付费。
  保留无关imports/default_bus_layout/artifacts/旧UID，不批量暂存或还原。
- 下一轮优先首战情境提示：实景已确认按Q后顶部任务/右上卡都跳到E机械爪，而底部仍Q攻击，防守反击无对应教学。
  不重新修LOS/针尖/手腕/镜头，不强制击杀或擅改主线目标；先做稳定、短句、按情境展示并验证退出恢复。
  正式美术、真人首读/难度、回摆、旧认证、普通解压/源码一致性与真实商业数据仍缺。

### 2026-09-08 接续轮：首战情境提示（本地小修）

- 接续6e7641d。有效guidance-red复现真实Q后，任务LearnGrappleMode不变，但右卡/左上都催促机械爪。
  只加情境观察与显示：入口、攻击模式、双方存活可见同高、屏内及真实LOS，340px进入/400px退出。
  模型限制早期目标/Boss未激活/爪Idle；不要求击杀、不发任务进度事件、不更改战斗或移动数值。
- 首战卡讲Q/1、鼠标瞄准、收针举RMB盾、刺后松盾再点LMB。HUD私有临时标题覆盖最终显示，
  CurrentModel.Objective/过渡时钟保持原值，退出显示最新任务；双语文本统一走现有localization/InputMap。
- 独立审查发现失败2次仍展开。retry-card-red真实两次Recovery后expanded=True退出1；
  仅非context普通失败提示才展开，GREEN中expanded=False，离开context又恢复普通展开Hint。
- 最终pre-verified/post-final均退出0无错误。pre3警告=2旧故意异常+新测试显露的旧入口DescentIce目标缺失；
  post3仍为既有故意异常。禁止把新增显露警告说成刻意异常或悄悄过滤。
  双语显示/开关恢复、E推进后恢复最新目标、滞回、可见/高度/墙/敌死与8组实际Label排版通过。
- 中/英文正常窗口分别真实格挡1/反击1/敌3→2/玩家5；前摇、格挡、反击三个阶段均断言卡/标题一致。
  普通五区路线、四复活、门/Boss重试/结算通过，无失焦。2777回调p95=17.192ms、p99=25.182ms、
  max45.995ms，9次>33.3ms；回落18样本0超边距/0道具栏交叠/稳定12帧，不称稳定60fps。
- import、普通窗口240帧、plain headless均退出0无错误警告；最终构建0错误警告。引擎/构建/转码全部串行。
  新导入产生的其他旧测试UID留在工作树，除本轮新增runtime的UID外不批量暂存。
- 前后中文Vulkan片各253帧4.22秒。主/独立每秒2帧概览+中英文举盾/反击原图，卡片均5行且标题完整，
  已采样阶段稳定，未新增遮住首战对象；英文原ENERGY尾字遮挡仍在。不是整片60fps审查或真人理解验收。
  记录：docs/playtests/2026-09-08-first-combat-teaching.md；外部september08-first-combat-teaching/CHECKPOINT.md绑定最终SHA。
- 下一轮优先修入口按E后路线目标缺失，再处理英文能量标题与条的重叠；先真实复现，保持非强制击杀与原主线。
  不重做首战卡/松盾/针剑/LOS/手腕/镜头。正式美术、独立Boost动作、真人首读/难度、旧认证、
  解压源码一致性与商业数据仍缺。仅开发检查点，不冻结r3、不新ZIP/标签/GitHub上传，无付费。

### 2026-09-08 接续轮：入口出口指引与资源标签（本地小修）

- 接续1b2b8bd。真实E后入口ReachBasin索要DescentIce无法解析，runtime与硬件movie均有效RED；
  movie继续真实D从x893到1291，实际进入Descent才解析Ice1个，无完成复活。不是地图断路或必须二段跳。
- Guidance只追加EntranceExit键并指向现存NextSeam(1278,574)，入口卡提示沿当前平台向右，进Descent恢复原卡。
  旧enum顺序、任务、首战/抓附优先级、地图/输入/物理/战斗数值不变。
- 原ENERGY真实宽62px、条起点50px，默认覆盖12px。初次UI RED是deferred中文覆盖English的fixture错误，
  等真实Playing后才得到有效尺寸RED。FormalHud现按字宽重排，固定cluster/条右端、字号/值不变，必要时缩条。
  默认English条宽136→122，切回中文可恢复；过热marker保持右内距，未改原过热线装饰。
- 新12组合24标签全部完整/不重叠/包含，pre-final/post-green退出0无错误，原故意警告2/3。
  中英正常窗口各格挡1/反击1/敌HP3→2/玩家无伤，E+D真实跨区、两区目标各1，退出0无错误警告。
- 正常五区/4复活/门/Boss重试/结算通过，无失焦；回落18采样0超边距/0道具栏交叠/稳定12。
  2773回调p95=17.314ms、p99=25.524ms、max46.066ms、12次>33.3ms，不称稳定60fps或真人认证。
  import、普通窗口240帧、plain启动与build通过；engine/build/ffmpeg均串行。
- 前后Vulkan固定60fps各355帧约5.92秒，主/独立2Hz图板+中英入口/英文下降原图复核：入口指向前方，
  角色/敌人/当前平台无新增严重遮挡，ENERGY完整；不是全帧视觉验收。独立代码无新P0/P1/P2。
  详细记录`docs/playtests/2026-09-08-entrance-route-guidance.md`；外部`september08-entrance-route-guidance/CHECKPOINT.md`绑定SHA/视频。
- 下一轮优先下降区教学卡遮住第一小平台右上角：before/after的exit-descent.png均可见，属于旧问题。
  正常下行复现后最小避让/精简，保留禁抓和安全回落信息，不新增必经机制，不重修已完成入口/标签等。
  其他平台/大字遮挡、真人首读/难度、正式美术/独立Boost动作、旧认证、解压一致性与商业数据仍缺。
  保留无关导入/总线/artifacts/旧UID；仅本地检查点，无新ZIP/标签/r3冻结/GitHub上传或付费。

### 2026-09-08 接续轮：下降卡与安全落脚台（本地小修）

- 接续1cecdaf。新增pre用例先复现平台顶边没有保护；现有catch_shelf=true作者标记可直接使用，不猜地形宽高。
  唯一生产改动为World静态体直接矩形碰撞顶边±6局部单位薄带，原Placer余量/候选、文字/尺寸、物理/地图/规则均不改。
  影响既有17个标记平台，不是全地形；未标记/false/disabled/hidden保持不保护，平台内部与墙不被整块占用。
- 自然D依次落GripStoneCatchZone和GripCatchShelf，每台6个不同物理帧真实向上支持，无跳跃/传送/人工Present。
  normal-red命名日志实际未复现：镜头位置已把台右端移到卡左侧，136采样0交集；不能算RED。
  硬件movie-red在入口复现744px²、136采样1次，走完才统一退出1；修复后136采样0交集且卡文字完整。
  两次硬件采样的272条render/physics/位置/支持体/顶边记录有序一致，不推广为全物理状态等价。
- 中英正常窗口各137采样0顶边重叠/0文字不完整/无完成复活或新增跳跃，原格挡反击前置也通过。
  pre/post退出0，错误0、原故意警告2/3；普通窗口240帧、plain启动与最终build通过，无资产变化无需导入。
- 正常五区/4复活/2检查点/冰面/过热/门/Boss重试结算通过、无失焦；回落18样本0边距/0道具栏交叠、稳定12。
  2775回调p95=17.239ms、p99=28.404ms、max47.268ms、13次>33.3ms，不作稳定60fps/真人证据。
- 前后Vulkan固定60fps490帧约8.17秒；另Task9 generated-main完整2720帧45.33秒，无RefreshNow可选分支、无覆盖旧截图。
  主/独立审同帧原图、前后2Hz、上升27–39秒3Hz和全程0.5Hz：小平台顶缘露出，所看样本无新增卡消失或严重新遮挡。
  卡片位置切换、盆地长平台/神殿门内部、下降左上锚点与HUD局部重叠仍存在；不能泛称全平台无遮挡。
  独立代码无P0/P1/P2，±局部单位/Completed恢复计数/磁盘GUID残留/非真人边界见详细记录。
- 记录`docs/playtests/2026-09-08-descent-card-clearance.md`；外部`september08-descent-card-clearance/CHECKPOINT.md`绑定SHA/视频哈希。
  全部engine/build/ffmpeg串行；仅本地检查点，无新ZIP/标签/r3冻结/GitHub上传/付费，保留所有无关修改。
- 下一轮检查下降段机械爪首用：玩家是否能在当前视野及620px射程内找到可抓地形，而不只是看到远处禁抓点。
  先真实复现再考虑分段目标/短提示，不强制抓取或修改地图/数值凑通过。正式美术、独立Boost动作、真人首读/难度、
  旧认证、普通解压/源码一致性与真实商业数据仍缺，不重做已完成入口/资源标签/首战/松盾/LOS/针剑/手腕/镜头等。

### 2026-09-08 最新接续：下降首爪机会（本地小修）

- 基于498e6a8。先实际走到首台，用LMB抓FrostVentCeiling下沿约320px、W/D牵引、松手落GripLanding12tick；
  无该段新普通跳跃/Boost/完成恢复。旧E只切模式就进入ReachBasin，导致卡只讲远处Ice，已保存有效runtime/movie RED。
- 仅新增可选首爪情境卡和实际锁点标记：真实首台支持、可用/可见/材质/首碰/620/屏内复核，一物理tick最多一次查询。
  从现有矩形派生cursor并显示真实hit，不增加地形/抓点。Q模式保留“按E”建议，持盾/恢复/完成等不提供新机会。
  Idle/Aim/Windup/Travel保留短卡，实际Latched/Reel/Swing/Boost指当前AnchorPoint，松手撤销；不强制、不改变任务/物理/数值。
- 持盾边界独立审发现并先RED：真实过热后held=true/active=false错误重现机会；仅教学补raw ShieldHeld，GREEN恢复。
  新pre覆盖双语/实际Q/E/材料隐藏禁用/World墙/离台/开关/真实边界点LMB附着与Retract撤标，unit保护阶段/任务/改绑。
- pre-final/post-final退出0错误0，原负向warning2/3；normal-en/cn各单抓附/松手/右台12tick、完整卡与单目标，0错误警告。
  walk-only仅D两台6tick、无额外跳跃/完成恢复，仍可绕过首爪；普通窗口240帧/plain启动通过。无新美术/付费/导入型资源。
- 正常五区/四复活/两检查点/冰面/过热/门/Boss重试结算通过，无失焦；回落18样本0边距/道具栏交叠、稳定12。
  2771回调p95=17.381ms、p99=27.454ms、max=60.930ms、12次>33.3/1次>50，不抹峰值或冒称稳定60fps。
- 前后硬件Vulkan60fps各589帧约9.83秒，17条阶段物理前缀有序相同；最终原主路线Task9固定60fps录像2720帧45.33秒通过。
  主/独立看前后2Hz及原图、中英阶段图、全路线0.5Hz和原下降专用锚点4–8秒6Hz前后板；未见新增严重遮挡/卡全消失。
  金/cyan叠标、卡片换位、上方抓点与HUD旧交叠仍在；先下沉再回升可能被误认失败，未获真人验证。
- 独立代码无剩余P0/P1/P2；渲染坐标暂无通用自动断言，超距/离屏是合并样本。GUID磁盘残留及受控fixture/自动驾驶边界保留。
  详细记录docs/playtests/2026-09-08-descent-first-claw.md，外部september08-descent-first-claw/CHECKPOINT.md绑定最终SHA/三片哈希。
- 下一轮围绕首爪下沉—回升的受力/收绳反馈实测，先比较实际输入与可见连接，避免只堆更多文字或先改物理参数；
  双重连接点符号可作为同主题的轻量清理。正式美术/独立Boost动作/真人/旧认证/解压一致性/商业数据仍缺。
  仅本地检查点，不新ZIP/标签/r3冻结/GitHub上传；保留所有不相关imports/总线/artifacts/旧UID，自动化保持10分钟。

### 2026-09-08 接续轮：收绳反馈连续性（本地小修）

- 实际HEAD已为65c36d5，自动化旧prompt落后；补齐外部首爪CHECKPOINT.md及三片既有哈希，不重做首爪教学。
- 首爪真实W+D下沉时绳长仍缩短，322.5→150.7后松手右台12tick；不把下沉误判输入失效，不改物理/地图/数值。
  唯一生产修改为ThreadShuttleVisual绘制门槛：四个机械爪附着阶段持续显示现有手腕线轴环，非仅遮挡时显示。
  原12半径/z10/手位/-RopeLength/18/Obscured属性不变；legacy/Travel/松手/失败/清理无环，无新资产/付费/节点。
- 像素RED：16无遮挡组合均0cyan、模型W/S0变化。最终GREEN均130–135cyan，W/S各88变化，停绳与48限位不空转。
  遮挡/释放/Travel/legacy/Retract/Failed/换皮/重入通过。模型→renderer不是InputMap或真人；完整pre/post通过，负向警告2/3保留。
- 正常中文首爪到右台、固定帧前后首敌→首爪路线均通过，17条完整阶段记录一致。前后短片589帧约9.83秒，
  主/独立看12Hz板与中英阶段原图，环贯穿LOS变化，未见严重遮脸或台面；非全60fps/真人验收，金青双标仍在。
- 正常五区/四复活/两检查点/冰面/过热/门/Boss重试结算通过，无失焦；回落18样本0边距/道具栏重叠、稳定12。
  2772回调p95=17.294ms、p99=27.553ms、max=48.241ms，13次>33.3/0次>50；旧高峰仍保留，非GPU帧时/稳定60fps。
- 独立代码无阻断；详细记录docs/playtests/2026-09-08-claw-reel-feedback.md及外部同主题CHECKPOINT.md绑定最终SHA。
  普通窗口240帧/plain启动0错误警告；Task9 generated-main五区2720帧Vulkan60fps通过，主审0.5Hz板无新增严重样本遮挡。
  日常检查点，不新ZIP/标签/r3冻结/GitHub上传。所有engine/build/ffmpeg串行，不触碰无关imports/总线/artifacts/旧UID。
- 下一轮转向盆地现有可选探索入口、收益和回到主线的连续体验，先实走再确定1–3项改进；
  不无限扩环/搬卡/堆测试，不把新机制未经原型验证塞必经路。正式美术、真人、旧认证、解压一致性和商业数据仍缺。

### 2026-09-08 接续轮：盆地可选补给闭环（本地开发）

- 接续9155d20，跨两个触发周期收尾同一主题，没有再开重复功能。原SalvageCache/SupplyCave只有空Reward标记；
  正常输入从EntryCatch落回收台、再左落主线能走通，但旧奖励实际治疗0/发现空。保留有效normal/movie RED，不因它重改地形。
- 仅回收台接入一次至多2格生命，满血不消耗，领取后开箱/中英文字/现有HUD更新并落盘；死亡/Continue保持、新局重置。
  Health治疗不重置无敌/去重/死亡状态。Area、逻辑与程序化可替换闭开箱分离，无付费或正式美术，未改输入/跳跃/抓爪/战斗数值。
  builder重生成Basin pack前后173节点；原111个显式节点属性不变，只有13个新增子树节点，资源ID重排不算地形重构。
- 独立审发现同步Healed先于消费记录；真实RED复现通知观察spent=False，及回调新局reset_clean=True却被旧外层写入发现/磁盘。
  现先Discover，捕获原Save身份，避免新局污染；仅回标题仍保存。错误Playing总gate的mutation RED验证不能漏存。
  活旧Status标签替换空字也先RED再修为读取当前Label；无通用异常事务或自动重生。
- pre-final退出0、ERROR0、WARNING7：5次Basin中段出生的旧EntranceCombat目标不在本区，另2原故意异常；不称全是负向fixture。
  post-final退出0、ERROR0、原负向WARNING3。新实际Area/InputMap、HUD/磁盘、死亡/Continue/新局、回标题和换皮通过；
  受伤和检查点是公开API受控准备，非真人实战。暂停回调、磁盘写失败、回调直接销毁节点尚无专项，独立源码无剩余P0/P1/P2。
- normal-cn/en-final均实际生命2→4，一次治疗事件，主线落稳12tick，Jump0/Dash0/RecoveryRequested0；退出0，同中段fixture警告1。
  本轮只从作者入口高台开始，不称从入口全程探索，也未验证地面向上重返回收台；旧可选Pass/Skip测试不能代替这个缺口。
- 正常五区/四恢复/两检查点/冰面/过热/门/Boss重试结算通过，无失焦无错误警告；回落18采样0边距/道具栏交叠、稳定12。
  2773进程回调p95=17.115ms、p99=24.907ms、max=55.902ms，10次>33.3/2次>50；不称稳定60fps/真人验收，历史峰值保留。
- before/after-final均Vulkan60fps257帧约4.28秒，6条阶段位置/速度/physics记录有序相同；主审最终4Hz与中英原图，
  补给、开箱、HUD变化可见。详细独立视觉范围和SHA/哈希见docs/playtests/2026-09-08-basin-salvage-supply.md及外部同主题CHECKPOINT。
  ordinary240、plain headless、import、最终build均通过，普通启动使用子进程APPDATA隔离存档；所有engine/build/ffmpeg串行。
- 下一轮优先盆地抓取货台：当前只移动Polygon2D，没有可承载碰撞，先实测用途再决定可选机关原型或清理误导。
  不直接加必经负担，不把它称已完成升降平台/永久捷径。SupplyCave空奖励、地面重返补给、持续标签/下层敌人视觉重叠仍待判断。
  不再重复已闭环补给或无限堆测试；正式美术、真人、旧认证、解压一致性、商业数据仍缺。
  仅本地检查点，无新ZIP/标签/r3冻结/GitHub上传。保留原imports/总线/artifacts/8旧UID及本次导入发现的2个旧脚本UID；只提交4个本轮新增UID。
  自动化仍每10分钟、ACTIVE，通知/目标与归档门槛不变。

### 2026-09-08 接续轮：盆地无效货台交互清理（本地开发）

- 基于c8ac60e。独立几何/调用链确认Cargo原来只移图形，无可承载碰撞、路线收益或存档依赖。
  实际主地面瞄旧Cargo点(2040,1806)选中legacy目标但LOS Obstructed，未激活；相差20.88px的石锚中心控制可抓。
  不是此次已发生自动松爪，也不将通用接收器fixture等同实走。
- 不为无用途平台扩建地图：只将Cargo的7节点互动子树替换为Midground/CargoRemnant纯Node2D+2个暗色Polygon，
  既有石锚/地形/敌人/补给/任务/数值不变。Basin builder pack前后169（旧173）；外观仍程序化占位，无付费。
- 活跃结构明确要求三保留机关+无任何抓取/碰撞/adapter的残骸，通用四kind能力测试仍在。
  **旧Task6四类机关发行认证链未修改且当前不满足**；新pre/post不是旧认证通过，后续必须明确迁移设计/发行契约，
  或验证有价值的第四机关，不能为数字塞回无效目标。本轮不冻结r3、不发新版ZIP/标签/GitHub。
- 真实InputMap与movie RED→GREEN：同旧near位置现在选石锚，W20tick绳384.96262→324.96262，手动释放返回主地面，
  无RecoveryRequested/普通Jump/Dash/Boost；12个不同tick支撑采样不是无漏采连续12tick。预入树仅作者检查点出生，非全程探索。
  初次W60控制中途Idle原因未观测到Health/Damaged，仍未证实；短窗口不证明无伤/长时安全/补给台可上。
- 新实际packed Basin四点resolver回归经精确旧c8ac60e场景反测，第二near查询选Cargo且LOSfalse失败；测试加载路径已恢复。
  pre-final退出0、ERROR0、WARNING7（5旧Basin中段目标缺失+2故意异常）；post-final退出0、ERROR0、3旧负向警告。
  四可选Pass/Skip、11路段×3采样与既有核心流程通过。normal-final新增Released1/Boost0/Idle断言实际通过，仍保留中段警告1。
- 按普通窗口非fixed/movie命令跑五区/四复活/两检查点/冰面/过热/门/Boss重试/结算通过，无失焦及错误警告；
  未启用--check-ascent-return，不新增回落18样本主张。2588回调p95=17.098ms、p99=27.023ms、max56.378ms，
  13次>33.3/1次>50，非GPU/稳定60fps。可见argv与空movie路径不等于独立排除宿主改写参数。
  普通窗口240帧/plain120帧在各自子进程APPDATA隔离下由工具确认退出0、ERROR0/WARNING0；不是仅由引擎头日志推定成功。
  未修改旧Task6审计实跑退出1：requires exactly 4 claw mechanism roles; actual=3。作为发行阻挡保留，不降低门槛凑认证。
- 07:19 UTC接续周期完成同一未提交改动的收尾，没有新开功能。新build/pre/post/normal/ordinary/headless的checkpoint日志均通过，
  ERROR0，pre/post/中段normal仍为7/3/1旧警告；新旧认证checkpoint仍精确失败actual3。
  route-checkpoint正常命令五区/四复活/门/Boss结算退出0、无失焦及错误警告；2590回调p95=17.084ms、p99=28.522ms、
  max45.280ms，11次>33.3/0次>50。保留前次峰值和argv/非真人限制。所有引擎/构建串行关闭后才下一项。
- 主/独立实际审前后6Hz图板与中英原图，旧金块退背景，绳/石锚可辨，无新增严重样本遮挡；近身手环/敌人及多重青轮廓仍在。
  Vulkan60fps前101/后110帧约1.68/1.83秒，不是性能/真人证据。外部september08-basin-cargo/CHECKPOINT.md绑定最终SHA/哈希；
  详细边界见docs/playtests/2026-09-08-basin-cargo-target.md。保留无关imports、总线、artifacts和10个旧UID，仅新2个UID入提交。
- 下一主题：盆地主地面经现有爪/一次Space助推重返补给台的实际路线，并核查机械爪操作时仍显示盾牌卡的引导冲突。
  先实走再决定1–3项改进，不盲增二段跳/地图/第四机关；SupplyCave空奖励、正式美术、真人、旧认证、解压一致性与商业数据仍缺。
  自动化保持每10分钟ACTIVE，其他设置、付费/归档与大提升才交付整包边界不变。

### 2026-09-08 接续轮：地面重返补给台与即时爪提示（本地开发）

- 基于1c588517d682665fd345bfaba4ef97d9822e0832；07:33开始取证、07:47接续同一未提交主题，未另开重复功能。
  真正地面路线已实测：作者检查点(2380,2014)瞄(2380,1632)，命中EntryCatch底面(2380,1636)，绳378.0005；
  W抬至y1877.9193，D+一次附着Space，助推实际应用后再松LMB，落右补给台、缺1回1、左返主地面。
  普通Jump0/Dash0/RecoveryRequested0，无地图/物理/牵引/战斗/资源数值变化；不证明唯一通路或真人已会找路。
- normal/movie RED已走完全过程，只因Swing/Reel仍显示盾卡失败；新unit pre-red也精确缺A/D失败。
  生产仅Model11行窄覆盖：Basin + LearnShield + Boss None/Inactive + Latched/Reel/Swing/Boost，
  复用已有爪卡、唯一实时ClawAttachment；其他阶段恢复盾卡，不推进真实任务，不改Controller/输入/资产。
  原“附着不替换盾卡”测试明确迁移；双语、全爪/Boss状态、扇区/任务/关闭/Complete、改绑和失败Hint边界通过。
- 中英normal-green与movie-green退出0/ERROR0/旧中段WARNING1；真实锁顶/助推/释放各1，HP4→5并返回，四阶段卡完整与正确。
  真实卡采样为Swing/Reel、落台及返回Idle，瞬时状态主要由unit覆盖；原失败2的盆地盾牌Hint与左上任务标题保留。
  Probe只查内存发现，磁盘持久化来自常规pre既有补给回归；受伤/出生/中段任务为公开API受控准备，非真人实战。
  后续无位置/速度/爪状态注入、无RefreshNow，InputMap/鼠标非硬件键鼠。GUID存档残留，finally松键/退订/音频清理。
- build-green0错误警告；完整pre/post退出0、ERROR0、既有WARNING7/3（pre5中段缺目标+2负向，post3负向），未过滤。
  默认entry-regression仍HP2→4、无普通跳/冲刺/助推/恢复，退出0/旧中段警告1。ordinary240/plain120分别子进程APPDATA隔离，
  工具确认退出0、ERROR0/WARNING0，不由引擎头日志猜测成功。无资产变化未重导入，所有engine/build/ffmpeg作业串行关闭。
- route-normal非fixed/movie命令五区/四复活/两检查点/冰面/过热/门/Boss重试结算通过，退出0，无失焦无错警告。
  2589回调p50=16.661ms、p95=17.140ms、p99=26.440ms、max55.385ms，10次>33.3/1次>50；不是GPU或稳定60fps，历史峰值保留。
  没有--check-ascent-return，不新增18回落样本主张；可见argv/空movie不能独立排除宿主参数改写，不是真人认证。
- before/after.mp4为RTX3060 Vulkan60fps，各345帧5.75秒；前后21条阶段/爪/奖励记录仅规范化Settle文案后有序一致，
  不是全状态等价。主审6Hz板/中英原图、独立审14文件：爪卡/实际锁点可辨、不新遮锚点/绳/落台；回落盾卡、生命+1、开箱可见。
  非逐60fps/真人审美；金青叠标、盾牌任务标题竞争、隔台武器重叠及重试Hint仍在。灰格含fixture退出，原AVI外部保存。
  详细证据与哈希见docs/playtests/2026-09-08-basin-supply-return-guidance.md及外部september08-basin-supply-return/CHECKPOINT.md。
- 独立代码/文档审查无本地提交阻断；旧四类发行审计本轮未再跑，保留上轮actual3失败及原审计链，不能因本轮pre/post通过就冻结r3。
  6文件白名单本地检查点，无新ZIP/标签/push/GitHub/付费；保留原29beehave imports、总线、artifacts、3reference导入、10旧UID。
  下一主题转向盆地隔台敌人攻击的预判和探索遭遇，先实走真实伤害范围/视觉遮挡，不继续只搬卡或重复加回血箱，
  不为四类数字塞回假平台。SupplyCave空奖励、正式美术、独立Boost动作、真人/旧认证/解压一致性及真实商业数据仍缺。
  自动化每10分钟ACTIVE，其他目标、通知及重大提升才交付整包边界保持。

### 2026-09-08 接续轮：盆地重型敌人地形遮挡（本地开发）

- 基于f621a57d1f6d67191b8de6a95d133194b7ff9218；08:06只读调查被用户调整频率请求打断，08:17继续同一主题。
  旧BellBackLaborer仅按水平距离发现玩家，真实InputMap从EntryCatch走落补给台，站稳后300tick内HP5→2，
  Laborer Connected和同tick Health.Damaged各3次；新起手只有2次，第一攻击在下落时已开始。
  普通窗口和硬件录像都有RED，不能再仅称视觉重叠。
- 两项最小修正：Laborer发现准入查World实体地形；已承诺攻击继续原Active/Recovery，但接触前也检查地形。
  Hitbox默认空TargetFilter，仅此敌人配置；先filter再去重/Receive/Connected，退出时清委托。
  不改其他敌人/玩家/Boss的默认命中、不改模型/时序/伤害/能量/输入/地图/美术，无付费。
- 真实自动物理pre-sight-red准确败在上下平台仍起手。只修准入后原实景安全，但pre-damage-red-forced仍证明
  合法起手后加World墙，实际Area重叠仍HP4（expected5）。过滤后双向四组皆实际重叠，无墙HP4/连接1/伤害1，
  有墙HP5/0/0；同AttackId Active→Recovery和实体Monitoring保留。
  新增显式同ID被拒后重试、成功去重及未配置Hitbox对照，通过最终pre；不称移墙会自动重发AreaEntered。
- normal-final从原高台路线站稳后HP5/0新起手/0伤害，再右落BasinFloor(2867.57,2013.979)，
  A主动走近(2697.46,2013.979)，两次真实Active→Recovery、Laborer连接2、HP5→3，Jump0/Dash0/Recovery0。
  出生/中段任务恢复为fixture前置，之后无位置/速度/AI/Health注入、无RefreshNow/Present；非实体键鼠/真人。
  外侧伤害计数与Laborer来源记录已分开；12支撑为不同physics采样而非穷尽每tick，GUID磁盘残留仍在。
- 地形旧缺陷单列：84px敌人卡在78px平台净空，远处正常等420tick仍x2650/Approach/接触SalvageFloor顶，
  后续主动走近可攻击不证明追击正常。本轮未调图；下轮最高优先修该遭遇净空与贴身范围遮挡，
  不重做地形LOS或塞假第四机关，不因已知旧卡位无限推迟这项独立安全修复。
- 最终强制build0错误警告；pre/post实际退出0、ERROR0、既有WARNING7/3。正常专项0/旧中段WARNING1。
  最初probe入树前动态路径错误/清理错误仅测试错误；pre-damage-red无新marker，不计为该测试GREEN，
  forced重编才得到有效伤害RED。保留日志，不重写历史。
- route-normal正常窗口非fixed/movie命令五区/四恢复/两检查点/冰面拒抓/过热/门/Boss重试结算通过，
  退出0、ERROR0/WARNING0、无失焦。2591回调p50=16.664ms、p95=17.174ms、p99=27.480ms、max47.578ms，
  8次>33.3/0次>50；非GPU/稳定60fps/真人，历史峰值保留。没有--check-ascent-return/新增回落样本，
  argv/空movie不能独立排除宿主参数改写。ordinary240/plain120各子进程独立APPDATA，实际退出0/ERROR0/WARNING0。
  无资产变化未导入/重建场景，所有Godot/build/ffmpeg严格串行关闭，末次无相关活动进程。
- 前后Vulkan60fps各420帧7秒；三条玩家路线physics/位置记录完全相同，仅代表此路线。
  主审3Hz图板/原图，独立看8文件：上层旧HP2/新HP5，同层金色前摇→白色Active与HP3可辨，未见新严重遮挡。
  同层大白范围、敌人/玩家重叠及越过台面的绘图仍旧欠账；射线不是整扇形/墨像素裁切，不覆盖World Area。
  非全60fps审查/正常性能/真人，原AVI仅外部。证据和哈希见docs/playtests/2026-09-08-basin-laborer-terrain-occlusion.md
  与外部september08-basin-laborer-encounter/CHECKPOINT.md；独立生产审查无本地提交阻断。
- 本轮8个白名单文件，不冻结r3、不新ZIP/标签/push/GitHub上传。原四类发行门槛及actual3失败保留，本轮未重跑。
  不新增资产/UID，保留原imports、总线、artifacts和10旧UID。正式美术、真人首读/难度、旧认证、解压一致性、
  SupplyCave奖励与真实商业数据仍缺。自动化每10分钟ACTIVE，其余目标/通知/重大提升才交付整包边界不变。

### 2026-09-08 接续轮：盆地右侧敌人通行净空（本地开发）

- 基于3ca7f3b7b0a96dda7f1f7dce55b6d7f104b6556e；09:00开始、09:35触发接续同一未提交主题。
  先补齐上一轮september08-basin-laborer-encounter/CHECKPOINT.md并纠正旧自动化prompt落后于HEAD，未重复LOS/命中过滤。
  当前normal/pre/movie RED都证明84px Laborer被78px净空卡住：玩家右落地面等待420tick，敌人仍x2650/Approach、0攻击。
- 只把右SalvageFloor、同台可读外观、PassStart/PassEnd/Reward与RecoverySupply上移56px，净空134px。
  builder生成Basin pack169→169；独立归一资源ID后120显式节点只6个位置改变，不改其他资源语义。
  敌人尺寸/速度/伤害/时序、左侧低台、玩家输入/移动/爪/盾/生命、任务、镜头和美术保持原样，无新资产或付费。
  SupplyRuntime受控上层出生Y1912→1856，所有奖励/生命周期断言保留。
- 正常窗口上层300tick仍HP5/0新起手/0伤害；右落后不主动向敌人靠近，敌人自行x2650→2778.3423，
  总128.3423px、落稳观察后约100.34px，记录290次BasinFloor支撑采样、0顶碰/max|Vy|0。
  两次Active/Recovery进入、Laborer连接2及同tickHP5→4→3，Jump0/Dash0/Recovery0；第二个Recovery尚未完整退出。
  原地面抓顶/W/D+一次附着Space/松爪路线仍落新台、缺1回1/HP5后返回，原输入时序未改；自然高台下落仍缺3回2/HP4后返回。
  均是作者中段出生/任务前置后的真实InputMap/鼠标事件，非实体键鼠/真人；不称从入口全程自行探索。
- 新实际packed main净空集成要求>=100px真实移动、Approach、地面支撑、同ID前摇/Active/Recovery及零恢复；
  只禁玩家外部输入适配器，真实AI/物理仍调度，finally退订并清自有GUID目录。
  首版额外等待漏掉初始位移已改为根窗口就绪后再挂载；pre-final的空sector是fixture错误，保留但不作游戏RED。
- 恢复追击使原整关受控致命请求落在正常i-frame中；route-normal/route-diag退出1均保留。
  真实伤害tick1575，致命tick1585，间隔10tick<0.35秒；只在AfterBasinCheckpoint准备阶段按最新真实Damaged等待完整窗口+2tick，
  最多120次推进，新的伤害重计，自然死亡/恢复明确fail。单次致命请求及原四恢复/遥测/同帧断言不变，没有生产无敌改动。
  route-checkpoint实际等待13tick、距伤害23tick时一次致命成功并同帧回检查点，五区/四恢复/两检查点/冰面/过热/门/Boss重试结算通过。
  正常窗口非fixed/movie命令、退出0/ERROR0/WARNING0、无失焦；2704回调p50=16.656/p95=17.145/p99=26.700/max46.714ms，
  13次>33.3/0次>50。不是GPU/稳定60fps/真人，历史失败与旧峰值保留；未启用ascent-return，不新增18回落样本。
- 前后RTX3060 Vulkan固定60fps分别933帧/15.55秒、804帧/13.40秒，长度由旧超时/新第二Recovery进入决定，不作性能或轨迹等价。
  主审2Hz板与原图，独立审12文件；台底/身体/攻击扇顶已分开、补给箱仍支撑、抓返/开箱/HP+1可辨。
  右缘玩家裁切和近身交叠仍在；非全60fps/真人审美，原AVI仅外部。详细证据/哈希见
  docs/playtests/2026-09-08-basin-laborer-clearance.md及外部september08-basin-laborer-clearance/CHECKPOINT.md。
- 最终build-checkpoint退出0、0错误警告；pre-serial-final/post-serial-final均真正退出0、ERROR0/WARNING8/3，
  pre为6次旧中段缺目标+2负向profile，post3旧负向；净空测试最终支撑181次，不混用旧184计数。
  post继续11路段×3样本、Pass/Skip、4catch返回与75旧case。ordinary240/plain120各子进程独立APPDATA启动0错误警告，
  退出值由工具与Process.ExitCode确认，末次无相关引擎作业。build-basin169/169、import也0错误警告。
  09:42的pre-checkpoint/post-checkpoint排队未先确认前进程退出，可能短暂重叠；日志保留但不作最终串行依据，
  已在post确实退出后重新逐个运行上述pre-serial-final、post-serial-final，再普通启动，未删除错误或改生产规则。
  场景/净空回归/Task9等待已有独立代码复核，无本地提交阻断；最终回执绑定12个白名单文件，不含原有无关导入改动。
- 下一主题为Basin→Ascent接缝的角色裁切与提前视野：只读定位相机仍按当前Basin边界2880夹紧，中心尚未越界就不切换，
  x2867.57仅剩约12.43px右余量。此投影仍需正常输入记录真实CanvasTransform/Zoom/Offset验证，当前未修；不改任务扇区来掩盖。
  左SupplyCave两块低台仍净空78px、空奖励、正式美术、真人首读/难度、旧认证、解压一致性及真实商业数据继续待办。
  旧四类发行审计actual3失败保持，本轮未修改/重跑，不以pre/post冒充r3认证。不冻结r3、无新ZIP/标签/push/GitHub上传。
  仅白名单本地检查点；原beehave导入、总线、artifacts、reference导入与旧UID均保留。自动化每10分钟ACTIVE，其他目标/通知/重大提升才整包交付边界不变。

## 用户反馈节奏

日常小步结果写入 README/迭代记录。明显体验跃升时再提交整包试玩、简明更新与旧版对比；关键失败、需要新授权或真实试玩数据时说明具体缺口。
不能凭主观认为“足够好”停止商业目标，也不能在缺少外部证据时无限堆功能。某一项被阻塞只暂停该项，仍有安全且有价值的本地工作则继续。
