# 盆地品质样板：单场遭遇与自然回路

日期：2026-09-08。范围承接已批准的盆地右半部小循环；这是开发检查，不是正式美术、真人品质或商业验收。

## 这次已验证的生产变化

- 在既有峡谷构建器生成的盆地上，打包独立 `BasinQualitySampleSector.tscn`，离树移除左侧 Rust；原整关双敌场景保留。
- 样板入口复制定义数组及盆地定义，仅替换 Scene；不改坐标、碰撞、补给、检查点、邻接和关键交互契约。
- 仅复制样板 Laborer 的调参资源，将感知范围由 420px 改为 200px。距离营地 270px，玩家可先阅读操作、向右靠近后接敌。
- 200px 同时改变 Approach／Guard 阶段的追击脱离距离，不是一次性触发器；已经开始的 Telegraph、Attack、Recovery 不因此中断。攻击、伤害、护甲、移速和破甲窗口均保持原值。
- Start、死亡恢复、重新加载后 Continue 和 New Game 都检查样板仍为一只 Laborer，默认整关仍含原双敌；营地等待 300 个物理帧后静止、正常 D 输入靠近可触发敌人。
- 修复 Q 后第一次攻击被尚未收回的机械爪同时解释为重复部署、误报 AlreadyDeployed：只有 E／Grapple 模式点击进入抓爪部署路径。E 模式重发守卫和 Q 收绳保留。证据没有证明第一刀被吞掉，不作该声称。

## 自动验证证据

证据根目录：`E:/OurGame/Knight/.evidence/september08-quality-sample-loop`。

| 项目 | 实际结果 |
| --- | --- |
| 独立遭遇 RED | `encounter-red.log`：真实样板 Start 后仍有 Rust，expected=0、actual=1 |
| 独立遭遇 GREEN | `encounter-green.log`：样板生命周期、默认整关和存档隔离通过 |
| 营地等待 RED | `camp-red.log`：等待时 Laborer 自动接敌，断言失败 |
| 营地等待／接近 GREEN | `camp-green.log`：全专项通过，包括真实 D 接敌；无警告／错误 |
| 编译 | `build-camp-green.log`：退出 0，0 警告／错误 |
| 场景生成 | `builder-camp.log`：节点 145→145，契约与封装入口检查通过 |
| 普通入口 | `entry-variant.log`：真实入口移动、暂停、返回标题通过；仅入口，不是整圈 |
| 独立代码审查 | 遭遇隔离及后续营地激活分别通过，无 Critical／Important／Minor；明确记录追击范围变化 |
| 抓爪交接 RED | `normal-loop-first.log` 实际走完整圈，但末尾发现抓爪失败；`normal-loop-diagnostic.log` 两次确认 Retract→Failed、AlreadyDeployed、mode=Attack、primary_pressed=True |
| 交接修复编译 | `build-handoff-fix.log` 退出 0，0 警告／错误 |
| 正常完整回路 | `normal-loop-green.log` 退出 0：挡盾 1、开甲 2、6 次生命伤害、击败 1、EntryCatch 抓附 1、助推 1、无抓爪失败／受伤／恢复，归营落地 |
| 抓爪专项契约 | `claw-contracts.log` 退出 0，模型、模式与运行时检查通过，包括重复部署守卫、Q 收绳、附着跳跃、冲刺和松手惯性 |
| 最终样板专项 | `sample-final.log` 退出 0，含营地等待／接近和完整生命周期 |
| 连续录制 | `movie-loop.log` 退出 0，硬件 Vulkan、1280×720、422 帧／30fps；同样完整回路通过 |
| 交接及证据独立审查 | 无 Critical／Important／Minor，确认没有位置／生命／AI／碰撞／进度注入；自动控制不计真人 |
| 最终完整 post | `post-final.log` 退出 0，PROLOGUE_TESTS_OK；与基线相同的 3 条刻意异常警告，无错误 |
| 普通启动复核 | `ordinary-sample-final.log`、`ordinary-main-final.log` 均退出 0，无警告／错误 |

`build-red.log` 最初因为新增测试漏引入 Laborer 命名空间而编译失败，不是行为 RED；
修正后的 `build-red-corrected.log` 编译成功，再运行 `encounter-red.log` 得到真实场景断言失败。
post 的既有异常用例警告为抛错的 ClawFrameChanged 观察者、无效 RoomPresentationProfile 根节点、拒绝 Completed→Recovering。

## 连续回路实测结果

正常样板 Start → 主动接近 → 挡横扫 → 机械爪开甲 → 普通攻击击败 → 返回营地附近 →
抓 EntryCatch 底面、W 收绳、D＋一次 Space 助推 → 落补给台 → 受伤则正常补给、满血则保留 → 返回营地。

Laborer 只有 Exposed 时取消护甲，Recover 仍有甲。正常窗口从左接敌，用实际右键挡住横扫，再两次 E 开甲与 Q 反击，六次生命伤害击败。
随后 A 返回营地附近，实际鼠标瞄准 EntryCatch 底面，左键抓住、W 收绳、D＋一次 Space 助推、松手落在 SalvageFloor。
无伤到达补给，满血时仍保留且未写 discovery；正常回营地在 `(2368.0864, 2013.9891)` 落地，0 恢复、0 抓爪失败。
录像使用同一输入代码、另一独立存档；最终位置 `(2364.1567, 2013.9823)`，关键计数一致。

这两次自然完整路线只覆盖满血保留分支，没有通过人为扣血制造奖励；不能声称受伤拾取分支也在这两段录像中出现。
探针根据实际敌人阶段定时输入，属于正常引擎处理下的自动控制，不是实体键鼠、无提示新手体验或真人时长证据。
约 14 秒录像不表示已有 5–8 分钟内容，也不为此添加等待或重复战斗。

## 录像审看与下一步

`loop-proof.avi` 为原始引擎录像，`loop-proof.mp4` 为含音轨的 H.264/AAC 转码；未插帧或改速度。
MP4 SHA256：`0FD6EF5EC6BCBAB941F965B92B1F3B3880729FED62943733B7E258712FFBB1DC`。
已审看 `loop-proof-review.png`（每秒 2 帧联系表）及关键完整尺寸截图：角色／敌人／平台未在路线中被裁掉，抓绳、开甲、攻击闪白和满血提示可见。
末尾极短灰帧是探针卸载清理，不是落坑；该录像证明运行路径，不是感官流畅度或音质认证。

画面仍明显原型化：劳动者及平台块状、空间材质层次不足；机械爪破甲与瞄准天花板上台的用途没有自然教学，返回只有静态路线文字，没有明确闭环反馈。
下一步先把这条已经可运行的路线做成可理解的体验：营地短提示／地标 → 盾挡和开甲反馈 → 可抓底面与落台辨识 → 满血保留及归营反馈；
随后在同一范围推进角色／敌人动作与统一场景／UI／音效，不扩建全关、不再重做入口或靠加长路线凑时长。

## 仍未完成

统一视听运行样板、无需讲解的上手／结束反馈、无需 SDK 的独立包及用户品质评审仍未通过。
旧盆地→上升接缝相机 RED 和未完成测试草稿保留；本轮未运行或宣称 full pre 通过。
没有新增付费、玩家招募、数据上传、公开发行、新版本号、ZIP、GitHub tag 或推送。
