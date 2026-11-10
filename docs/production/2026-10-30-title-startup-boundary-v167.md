# v167 — 标题独立启动边界（技术修复，非美术或序章交付）

## 事实

用户同意完整回归后仅显示隔离标题，开始/继续禁用、无自动开局或关闭、无OS输入与前台激活。实际一次启动PID64228在Ready前失败，已经退出。`TITLE_WINDOW_STARTED`以及wrapper exit0只代表创建过进程，不代表标题成功显示。原件：

`G:/knight/验证证据/title-hold/live-20261030-052941-802-ddb3140cb1dd4431923a280cb42abf85`

stderr主错为quit binding expected1/actual0，无ISOLATED_TITLE_READY；用户是否看到短暂空窗未知，保持未确认。本次没有重复弹窗/截图/录像。

## 推理与实际修正

新独立headless入口直接执行同一个SceneTree._Initialize。RED案`keyboard-20261030-053924-161-97dcfcd643864311807c207e68f0a97f`：build31398ms PASS、原keyboard19395ms PASS、独立entry991ms FAIL。真实Root/Level/UI未入树/未Ready，quit0；原keyboard内延后合同仍通过，证明旧覆盖漏了启动边界。未声称Godot引擎损坏或资源崩溃同因。

Mount挂树前条件性等待root实际入树，挂树后同步要求Level/UI/Flow Ready再锁定Start/Continue，不把连接0放宽为可接受，不手调_Ready，不延长原cap。三通道仍挂树前隔离；两种AutoStart仍关闭；Quit/X仍一套清理。

GREEN案`keyboard-20261030-054223-596-6b9044c056e24b6e9a18c516ebd46f50`：原keyboard19290ms与直接entry7942ms均PASS；entry开始RootFalse，挂载后Root/Level/UI ReadyTrue、quit1，两挂载各180physics及菜单/无开局/双关闭/释放保留。

最终helper版default prepare案`keyboard-20261030-054416-119-3b68e9e6def14fd69e00e30d35ad86d6`：build25769ms、keyboard19415ms、独立entry7849ms全部PASS，完整资格核验通过，未启动live。PS实际同案验证PASS：模式2拒绝、receipt9拒绝、启动流程4拒绝、startup2阳性6拒绝、5个真实qualification负控clone；clone带title-control-fixture.json，不计native尝试。

PS startup状态测试从缺函数RED到GREEN。live启动器新增只读启动确认，区分process-start与READY，退出/错误/FATAL/重复READY/END均拒绝；55秒未确认不重复启动或自动关闭已admit进程。当前没有新live，因此该正常Windows观察环节尚未被实际显示验证。

## 风险与剩余工作

- 最终普通keyboard/models/pre/post四阶段均通过（下表），不外推完整路线、资源根因、正常Windows显示或性能。
- 原资源终结器FATAL、完整Main/Explore失败、正式视听美术、硬件性能与真人游玩认可仍OPEN。
- v165菜单跨帧按钮Minor仍未以本修复关闭；有限headless不会证明无限时间稳定或用户可见。
- 此次只修验证入口，不改变生产战斗/物理/地形/视觉，不完成原十一项。
- 源与helper在owned原生运行期间冻结；无付费、额外采集、OS输入、自动化、Git写入、资料删除或真实存档语义读取。

计划/ledger：`docs/superpowers/plans/2026-10-30-title-startup-boundary.md`、`.superpowers/sdd/2026-10-30-title-startup-boundary/progress.md`。完整序章十一项目标不变。

唯一fresh审查C0/I0，无新增Minor；原Settings/Controls按钮跨帧覆盖M1仍延期，不二审。审查未认证live观察、用户可见、完整十一项或资源故障已修复。已保管3keyboard的15清单对/1422选定源实体42helper18bin；9个owned构建/主/次引擎PID均结束。5clone非native。

本次容量检查2026-10-29T21:50:35.9154358Z：媒体7427750bytes/86files、Notify=false，未新增媒体或建立自动化。

## 最终普通回归

全部在`G:/knight/验证证据/october13-stable-claw/`，不删早先RED、live初始化错误、v166超时/FATAL。

| 阶段 / 案例 | build ms | native ms | 结果 |
|---|---:|---:|---|
| keyboard-20261030-054416-119-3b68e9e6def14fd69e00e30d35ad86d6 | 25769 | 19415＋独立entry7849 | PASS |
| models-20261030-054545-782-8ee4f6c604e143d780b7be7d99f68392 | 37515 | 3185 | PASS |
| prologue-pre-20261030-054645-742-1180d96f1e9f42d4944f4f45d9142ff4 | 30590 | 178764 | PASS |
| prologue-post-20261030-055028-246-4bbcecb00d7e4b8bac7cc366d9936a0e | 28990 | 209686 | PASS |

pre只余1236ms原cap余量，不能称稳定性能；post保留既有300000ms cap和原4限定WARNING（故意observer1、profile负控1、Completed→Recovering2），原180秒资格仍false，不消音、不外推FPS。当前全源1861与六bin仍能被最终keyboard严格资格接纳。

本修复6案例含前述RED与中间GREEN；9次native（含3独立entry）为8PASS/1RED。获准live失败另列，不混为headless通过；5个控制clone不计native。完整Main能量0恢复失败、ExploreGrip9遮挡、历史资源终结器异常仍OPEN。

新一次“回归结束后是否再次显示标题”的异步选择已提出，回复前不打开窗口。下一继续实际入口低噪岩土/遗落机械深度与雾下可读性，不新增假落脚或改碰撞；正常速度像素与用户审美仍需各自证据。

最终保管：6案例30组前后清单对/2862份选定源实体/76helper/36封存bin全部SHA一致、未列实体0；15个owned build/native PID结束，早先live64228亦结束，未触碰未知8760或桥接进程。post75 enter＋75 return索引0–74唯一、owner8及nodes1→1原收据齐备。当前1861源/最终keyboard全部11helper/六bin与资格案一致；HEAD和index仍为本计划基线，无提交。5个PS控制clone带非native标记，未混计原生结果。
