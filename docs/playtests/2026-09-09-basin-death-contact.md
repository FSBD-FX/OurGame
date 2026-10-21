# 跨区碰撞致死：恢复时机

日期：2026-09-09。生产修改基线 `6671d3bb91bc6821e33559d38cc2111644f902e7`。

## 事实

原生 `BasinSample.exe` 已正常 Start 进入营地，实际加载的 hostfxr/hostpolicy/coreclr 均位于包内数据目录。子进程清除了 DOTNET_ROOT/DOTNET_ROOT_X64/roll-forward，PATH 仅保留 Windows；这是同机证据，不等于无 SDK 的干净虚拟机验证。

Windows Computer Use 检测到用户手动操作后停止键鼠操作。原生进程最终退出 0，但 `native-first.log` 包含 22 条 `Can't change this state while flushing queries`。调用链是：真实 Hitbox.AreaEntered → 伤害 → Died → RequestRespawn → 同步 RestoreCheckpoint → 新邻区的 CollisionShape2D.Disabled 激活。因此导出成功/退出 0 不能作为完整可交付的结论。

## 修复及复现

保留同步 RecoveryRequested 信号，先让记录器读取死亡前输入。请求未取消时停用人物的 gameplay physics，再单次 CallDeferred 执行整个恢复；BeginRecovery 在消费队列前检查取消和节点有效性。没有把单个 shape setter 变为 deferred，因为场景激活事务随后立即验证其真实状态。

独立审查指出取消的表现边界：Completed 状态应在发请求前拒绝，避免 HUD、pose 和记录器看到一个不会完成的恢复。前置许可归 GameFlow 负责；New/Continue 的 relocation 清理人物死亡/恢复表现，覆盖完成后实际 Health.Died 再 Replay 的情况。旧房间 TransitionTo 也不再抢占已排队的恢复。三项分别有取消、真实死亡重玩和同帧切房 RED；未通过发送虚假的 RecoveryCompleted 清理画面。

新增测试在样板正常 Start 后受控摆位至右侧 Ascent 接缝，实际流式卸载/挂载发生后，用真实 Hitbox/Area2D overlap 造成致死伤害。此测试不是直接调用 Health.ApplyDamage 模拟物理回调。

- `death-contact-stream-red.log`：旧实现产生同类引擎错误，且“接触回调内不应已恢复满血/发完成信号”断言失败。
- `death-contact-stream-green.log`：连续两次致死；同回调重复请求只恢复一次；满血、原营地、IsOnFloor、Playing 和唯一敌人均通过，无引擎错误。
- `death-fix-lifecycle.log`：样板标题、新局、死亡、重启继续、存档隔离、教学及默认整关入口通过。
- `death-contact-regression.log`：旧第一死亡测试仍要求同步满血而失败；改为有界等待实际 RecoveryCompleted 后，保留原起点/不激活未来检查点断言。不是删除检查点测试或改出生位置。
- `death-contact-regression-2.log`：样板碰撞、GameFlow 和人物 Formal 整套通过；一条预期 Completed→Recovering 拒绝警告，非碰撞异常。
- `death-cancel-red.log` / `death-replay-red.log` / `death-legacy-race-red.log`：分别复现无效请求残留、真实死亡后重玩卡 Dead、恢复被同帧切房吞掉。后者观察到 completed=0 且错误落入 BrokenTooth 房间。
- `death-cancel-green.log` 名字只是首次候选检查：取消用例通过后，legacy fixture 因测试入口漏设 AllowDiagnosticFallbacks 失败；按现有 Task9 runner 契约仅为测试房间启用回退，生产配置未放宽，不能将该文件称为全绿。
- `death-final-contracts.log` 最终退出 0，0 ERROR、2 条刻意完成态拒绝警告：真实跨区碰撞/去重、GameFlow/重玩、人物 Formal、死亡前输入记录、legacy 全组和真实生成主场景瞬态恢复均通过。记录器检查明确跨过实际 RecoveryCompleted，而非在尚未清输入时假装检查过重置。
- `death-final-lifecycle.log` 最终样板生命周期全部通过，0 ERROR/WARNING。
- `death-final-normal-loop.log` 最终源码普通窗口、正常 InputMap 自动路线退出 0，0 ERROR/WARNING：故意过热后恢复、1次格挡、2次开甲、6次伤害命中/击败、1次地形抓取及 Space 助推、上台后满血保留补给、归营；0伤害/复活。自动路线不是真人首次上手或难度验收。
- `death-fix-generated-route.log` 较早普通整关回归通过五区、4次 deferred 恢复、检查点和 Boss 重试结算。最终版 `death-final-generated-route.log` 前两次恢复通过，但第三抓点之后在 Ascent 下一物理条件等待超时（停在约 `(3843.75,1397.93)`）；保留该失败，不将较早成功移作最终全绿。日志有一次鼠标目标偏差，尚不能断言来自用户输入或生产问题；需受控复现。此失败阻止最终交付，不据此盲改地图。
- 只读定位：失败在 `PrologueCanyonGeneratedMainRouteTests` 的第三爪后纯右移阶段，目标 X>=4080、320帧上限，尚未进入后续 Space 跨缝步骤。原命令仅加现成 `--trace-grapple` 的一次 `death-final-generated-trace.log` 退出 0、完成五区/4次恢复/Boss重试结算；未改源码或放宽断言。该成功没有复现超时，不能倒推先前失败来源；两次证据均保留，需在捕获失败时检查 input_move/vel/focus/contacts。
  独立只读复核：成功追踪从相同第三爪落点开始，横移 input_move=1、vel=(260,0)、floor/focus=True，contact 只有 LandingThree，连续通过 X>=4080。当前证据不支持该位置有固定地图墙；旧失败缺失的输入/瞬态运动状态仍不能由成功记录补全。

## 可见证据与边界

最终 `E:/OurGame/Knight/.evidence/september08-basin-package/death-final-proof.mp4`，1280×720，30 FPS，338 帧/11.27 秒，H264/AAC 保留声音；SHA-256 `d5cf484c66e20ec64070d2841f9b066920229b9ac5058624cd46a05ac2260d17`。早期 `death-proof.mp4` 仍保留，没有覆盖替换旧证据。

硬件 RTX 3060 Vulkan 普通窗口受控复现退出 0，日志无 ERROR/WARNING。已审看全段 2Hz 抽帧板：两次右接缝摆位后回到有地面的营地，教学重新出现。接缝人物裁切仍可见，属于保留的旧相机缺陷，不在本次恢复修复中谎报解决。

录像中的摆位与致命碰撞器为诊断夹具，不是实际路线通关、敌人平衡或真人首次体验。旧源完整回路短片另行保留；修复后的独立包仍需重导出并验证正常新局/退出继续/死亡后整圈。当前不生成发布 ZIP、版本标签或上传 GitHub，不改旧 r1/r2 档案。

## 待继续

恢复契约独立审查已通过。先对 Ascent 自动路线偶发等待失败做有界输入/碰撞诊断，不以追踪重跑成功冒充根因修复；再重导出、原生窗口运行、整套 runtime/notices/版本哈希装包与新目录解压验证。goal 保持 active；没有美术、真人或商业目标完成声明。
