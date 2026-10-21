# 首敌视线自遮挡与刺击范围 · 2026-09-08

基于开发检查点 `e7b809c51b5f8a6210bda5c240c00e32e5ac2e35`，属于本地小修；r2冻结包不变。
没有新ZIP、命名试玩版本、标签或GitHub上传，没有新资产或付费生成。
本轮最终SHA与视频哈希绑定在外部证据目录的 `CHECKPOINT.md`。

## 事实、推理、风险

- 原完整路线允许跳过首敌，不能证明首场攻击提示可读。先用真实主场景、输入事件和自动调度靠近首敌。
- 原首敌只向左追击一帧约1.5833px，随后面对46.68px外的玩家持续Idle；并非暂停或教学冻结。
- 真实视线射线命中自己的AttackHitbox；额外排除该RID的只读对照射线命中Player或Player/Hurtbox。
  `Monitoring=false` 不等于不参与射线查询，原逻辑只排除了根Body。
- 恢复交战后，实际针尖远伸出26×18伤害矩形。直接扩大伤害会提高难度；本轮修视觉，不扩大判定。
- 恢复原本应该存在的追击与攻击可能改变首战难度感受；自动通关可检查阻塞，不能代替新玩家验收。

## 仅两项生产修改

1. `RustShellSquire.HasLineOfSightTo` 递归排除本actor及其子树的CollisionObject2D。
   保留原碰撞mask、Bodies/Areas与目标归属判断；不忽略外部墙、外部Area或其他场景节点。
   动态添加的自身嵌套传感器同样不自挡，不新增全局缓存或场景配置。
2. `RustShellFormalVisual` 的Active针剑部件目标从 `(42,-1)` 改为 `(-10,-8)`，
   武器主动作在Active前25%完成，身体继续原跟随；Recovery共用同一端点，阶段边界连续。
   不伸缩/隐藏武器、不改变模型、速度、输入、26×18伤害框、0.65秒前摇、0.14秒Active和0.65秒恢复。

## RED / GREEN

证据根：`E:/OurGame/Knight/.evidence/september08-first-enemy-readability/`。

| 检查 | 修改前 | 修改后 |
| --- | --- | --- |
| 真实主场景首次接近 | `encounter-red.log` 普通窗口；`sight-red.log`硬件录像均无Telegraph，预期退出1 | 普通窗口与最终录像完成两次完整攻击，各实录两次伤害、18个Active渲染样本 |
| 自碰撞视线回归 | `sight-regression-red.log` 在正向自身AttackHitbox遮挡断言失败，退出1 | `pre-final.log` 双向持续追击、自身嵌套Area、外墙、外部Area及清除墙后恢复均通过 |
| 针尖实际范围 | `needle-red.log`（仅LOS已修）两次攻击，最大超界52.27px，包络断言失败 | `movie-final.log`及`encounter-normal-final.log`最大2.56px，退出0 |
| 双向逐阶段针尖回归 | `needle-regression-red.log` 正向Active进度0.29时超界3.75px，退出1 | `post-final.log` 两朝向各101个Active进度样本、原主次运动及阶段连续性通过 |

3px只约束**针尖中心**距真实伤害矩形的距离，允许6px描边的一半；不是整根剑或所有墨像素都在框内。
真实样本同时读取当前碰撞体及发布的预警矩形，前后所有Active样本的origin差为零，排除移位造成的假差异。
`needle-red`与`movie-final`的9条阶段/受伤事件有序完全一致；40条渲染记录去掉针尖/超界字段后也有序一致。
这只说明本次实景序列的行为一致，不扩大为全状态空间证明。

## 验证结果与边界

- 最终构建零错误警告。串行重跑`pre-final`/`post-final`退出0、错误0、警告2/3为既有故意异常fixtures。
- `encounter-normal-final`为非fixed/movie的普通窗口InputEventAction专项，退出0，无错误警告。
  加载真正`PrologueLevel`，唯一出生点设置在入树前，隔离save/settings。没有手调敌人状态、伤害或姿态。
  不是实体键盘人工输入；两次命中来自实际日志，probe的通过条件不强制要求两次伤害。
- `route-green.log`正常窗口非fixed/movie完成五区、四次复活、门、Boss重试和结算，退出0，无错误警告/失焦。
  回落18下降采样0次超48px安全边距、0次道具栏交叠，稳定落地12帧。
  2774个进程回调样本：p50=16.659ms、p95=17.193ms、p99=24.042ms、max=44.146ms，
  14次>33.3ms、0次>50ms。这不是GPU帧时、稳定60fps认证或真人游玩证据。
- `ordinary-main.log`普通窗口240帧和`headless-main.log`普通无脚本启动均退出0，无错误警告。
- 首轮`post-green`返回session后曾紧接启动`movie-green`，不能排除短暂资源重叠；这组保留为历史，
  最终采用明确等待退出后串行运行的`pre-final`、`post-final`、`movie-final`，不把旧组当正常帧率证据。
- 旧诊断日志`FIRST_ENEMY_LOS`并列旧root-only射线命中与当前production visible，易误读。
  新probe已标为`FIRST_ENEMY_LOS_LEGACY_QUERY/production_visible`，注释澄清后重新构建及普通窗口专项通过。

## 录像与独立复核

RTX3060硬件Vulkan固定60fps：

- `sight-before.mp4` 605帧约10.08秒：实际首敌自遮挡、持续不反应。
- `needle-before.mp4` 355帧约5.92秒：只修LOS后，真实追击与两次旧过伸攻击。
- `encounter-final.mp4` 355帧约5.92秒：两项生产修改后的同路线交战。

主/独立审看前后218–229连续帧4×3图板与前摇/Active原图：旧后半段针尖越过玩家左侧继续伸出，
新版不再明显过伸。主代理另看最终恢复段3×3图板，恢复落针到下一次前摇未见明显跳变。
样本未见新增HUD/地形遮挡或严重断裂；近身身体、手臂、武器仍交叠，主动作也更小。
这不是整体可读性、正式美术、连续逐帧全片或真人审美验收。独立代码复核未发现阻止本轮提交的问题。

视频SHA256：

- sight-before：`875CB02FB35EDD844C245A6AC590BA243AFE1850A110217BDE5A74FA134B4561`
- needle-before：`FEA6B18F503557BBE9C49E9AE8B7CAAF54BC24B64CBC05C1876C46BD5874024B`
- encounter-final：`147C73B65AD568EEC39626C4B0EC4834D1C3B5F63B8284542120CA49201C0F1C`

入口：`res://tests/presentation/FirstEnemyEncounterProbe.cs -- --assert-tip-envelope`；
可加`--output=<项目外绝对目录>`保存实际帧。录像额外加Vulkan、`--fixed-fps 60 --write-movie <唯一.avi>`。
完整回归仍用`RunPrologueTests.cs -- --phase=pre/post`；全路线用`RunRealtimePrologueProbe.cs -- --check-ascent-return`。

## 接续

下一轮优先在正常操作中验证首敌前摇→举盾/避让→反击的提示是否易辨，再选小范围改进。
不扩大伤害弥补视觉，不凭静帧宣称初遇易懂。正式美术、真人首读/难度与回摆舒适度、
普通解压启动/源码一致性、旧独立认证和真实商业数据仍未完成。日常只存开发检查点，不发布伪里程碑。
