# 盆地重型敌人的地形遮挡（2026-09-08，本地开发）

基于 `f621a57d1f6d67191b8de6a95d133194b7ff9218`。本轮是两项有独立失败证据的战斗修复，
不冻结 r3、不新增 ZIP、标签或 GitHub 上传；没有新增美术、付费、地图和数值改动。
最终提交、录像与完整日志位于外部 `E:/OurGame/Knight/.evidence/september08-basin-laborer-encounter/CHECKPOINT.md`。

## 事实、推理、风险

- 实景敌人为 BellBackLaborer。原控制器只按水平距离发现玩家，Hitbox 接触时没有地形许可。
  在作者 EntryCatch `(2380,1568)` 出生，通过正常 A/D 下落到补给台后，玩家站稳 `(2597.784,1911.9714)`；
  下方敌人 `(2650,2011.769)` 的真实扇形最高约 `y1932.487`，穿过台面与玩家 Hurtbox 重叠。
  300 physics tick 观察内，真实 Laborer Connected 与同 tick Health.Damaged 各3次，HP5→2。
  其中只有2次新起手发生在观察窗内，第一次在玩家下落过程中已经开始；不能把它们都写成站稳后起手。
- 因此不是仅有绘图遮叠。准入缺少地形检查导致跨层起手，接触缺少地形检查允许已承诺攻击穿墙。
  两者分开修，不改变 Telegraph→Active→Recovery 的既有承诺与时序。
- 净空另有真实旧缺陷：84px高身体被夹在78px净空，正常窗口地面远处等待420tick，
  敌人仍在x2650、Approach并碰上方SalvageFloor，不能正常靠近。
  主动走近后能攻击不等于追击恢复。本轮保留该缺陷，下一轮优先调整真实遭遇净空/构图并重跑补给路线。

## 改动边界

1. Laborer 使用自身中心到玩家中心的 World-body 射线决定发现准入：只查实体地形，
   不查传感/攻击 Area；HitFromInside=true，排除自己的根 RID。原420px水平检测范围和全部时序不变。
2. Hitbox 新增默认空的可选 TargetFilter，仅 Laborer 配置自身中心到接触 Hurtbox 中心的同一地形判断。
   在目标去重登记、Receive 与 Connected 前拒绝，退出树时清理委托。
   其他未配置攻击保留原规则，不全局改变所有敌人/玩家/Boss的遮挡策略。

这不是整片扇形碰撞或墨像素的裁切；中心射线可能保守拒绝部分绕边命中，不处理World Area，
也不因移墙就自动重发AreaEntered。没有取消合法起手、缩短武器或改伤害/能量/输入/跳跃。

## RED / GREEN 证据

- `upper-normal-red.log` / `movie-red.log`：真实窗口/硬件录像均复现2个新起手、3个连接、3次掉血，退出1。
- `pre-sight-red.log`：真实自动物理的上下分层fixture仍发布新Telegraph/Active/Recovery，准确失败。
- 只加准入后 `upper-normal-sight-green.log`：相同原路线，上层HP5、0起手、0连接、0伤害。
- `pre-damage-red-forced.log`：已合法起手后加入World墙；真实Area仍重叠、HP仍5→4，准确败在expected5。
  同时无遮挡控制HP4/Connected1/Damaged1，排除测试根本碰不到目标。
- `pre-filter-green.log`：双向各有墙/无墙四组真实Area重叠。无墙HP4/连接1/伤害1；墙后HP5/0/0，
  均保留同AttackId的Active→Recovery。完整pre退出0。
- 新增显式接触边界保护被拒后同AttackId重新尝试的资格及成功后的去重；独立未配置Hitbox保留原规则。
  它们使用公开Begin/End/TryConnect，与上面的自动Area伤害回归分开，不冒称移墙会自动重新命中。

`normal-red.log` 最初因入树前读取动态敌人路径及Hurtbox旧名称失败，属于probe错误；
修正后真正RED另存，失败清理也补上未入树Free。`pre-damage-red.log`没有新damage marker，
不能作新增伤害测试的GREEN；强制Rebuild后的 `pre-damage-red-forced.log` 才是有效对应记录。

## 实际输入与视觉

`normal-final.log` 在上层保持HP5后，D到x2857.57松键，刹停/自然落地在BasinFloor `(2867.57,2013.979)`，
再A靠近敌人，停在 `(2697.4644,2013.979)`。真实两次Active→Recovery、Laborer连接2、HP5→3；
普通Jump0/Dash0/Recovery0。没有位置/速度/AI状态/Health注入，没有关AI或调用Present/RefreshNow。
出生点和中段任务恢复是fixture前置；不是从关卡入口的全程探索、实体键鼠或真人体验。
outside_upper_damage统计所有非上层Health事件，不单独作为伤害归因；同层归因另看Laborer Connected及对应tick。
支撑12是12个不同physics采样，不保证逐tick无漏采；GUID测试存档仍会残留。

前后片均RTX3060 Vulkan固定60fps、420帧/7秒，不能作正常帧率证据：
三个玩家路线阶段的physics编号和位置记录前后有序完全一致；只说明此路线，不是所有游戏状态等价。

| 录像 | SHA256 |
| --- | --- |
| before.mp4 | 8C7CE32596338905EDDB49B549DFE96D5950C5A155E7860EBA45198DFE0AAD97 |
| after.mp4 | 653B35D4DBF56FF0C62C8D276A6FB892F046FD4F0DE90D984DF8305368AE493E |

主代理已看前后3Hz整段图板及上层/同层Active原图；独立审看8个文件（两张图板、六张上层/同层原图）：旧版站台期间白色扇形穿台并掉血，
新版相同停留不再挥空和掉血。正常同层仍有很大的白色覆盖面、角色与武器重叠，
不能称整套敌人美术或攻击首读已经精致。没有新增素材，原AVI只保存在外部证据目录。

## 最终回归

最终build强制重编退出0、无错误警告；`pre-final.log` 和 `post-final.log` 均实际结束、退出0，
ERROR0、既有WARNING7/3。pre包括新增的显式过滤后重试、默认Hitbox对照；没有吞掉或过滤负向警告。
pre为5次旧中段出生EntranceCombat缺失与2个故意异常profile；post为原throwing observer、非法房间profile、非法状态转换。
`route-normal.log` 正常窗口非fixed/movie命令：五区、四次复活、两检查点、冰面拒抓、护盾过热、
门与Boss重试/结算全部通过，实际退出0、ERROR0/WARNING0、无失焦。
2591个进程回调样本（剔除前120间隔）：p50=16.664ms、p95=17.174ms、p99=27.480ms、
max=47.578ms，8次>33.3ms、0次>50ms；这是自动输入负载下的回调墙钟，非GPU帧时、稳定60fps或真人验收。
未启用旧 `--check-ascent-return`，不新增18回落样本主张；argv与空movie路径仍不能独立排除宿主改写参数。
`ordinary-main.log` 普通窗口240帧、`headless-main.log` plain120帧，各以子进程独立APPDATA启动，
工具确认退出0、ERROR0/WARNING0。所有Godot、build、ffmpeg严格串行并等进程结束后继续。
没有资产变更，未重导入或重建场景；最终进程检查无相关活动作业。

运行命令：普通专项 `--path . --rendering-method gl_compatibility --max-fps 60
--script res://tests/presentation/BasinLaborerEncounterProbe.cs -- --ground-control --output=<外部绝对目录>`；
上层对照去掉 `--ground-control`。录像改用 `--rendering-method mobile --rendering-driver vulkan
--write-movie <外部AVI> --fixed-fps 60`。pre/post为 `--headless --script res://tests/RunPrologueTests.cs -- --phase=pre|post`，
完整正常路线为 `--script res://tests/RunRealtimePrologueProbe.cs`、gl_compatibility/max-fps60，不加movie/fixed。

独立生产代码审查无新增P0/P1/P2提交阻断；本轮8文件白名单，保留原导入、总线、artifacts与10个旧UID。
旧Task6四类机关发行门槛和actual3失败保留，本轮pre/post不能替代其认证。
