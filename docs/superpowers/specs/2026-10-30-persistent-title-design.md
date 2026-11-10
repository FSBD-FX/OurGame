# 当前版隔离常驻标题核对

修复旧短时自动退出probe不能承担用户显示确认的问题，不改变KNIGHT游戏内容、正式标题文案或真实存档。仅供显示核对：三通道挂树前隔离，两个AutoStart关闭，Start/Continue禁用并解绑开局，Settings/Controls可往返，Quit或窗口X进入同一幂等清理。明确这两个禁用按钮属于核对fixture，不把它当正式菜单/玩法验收。

独立tests/RunIsolatedTitleHold.cs复用IsolatedPrologueFixture；不复用旧观察器/120秒循环，不录按键或媒体，不抢前台。live没有时间截止，正常速度，仅用户关闭或真实安全错误退出。关窗清理取消准备、音频drain、释放自有level；不引入新的GC/引擎策略。

同一session由现有keyboard阶段追加真实无窗口合同：实际Ready与多帧Title保持、菜单往返、无存档/试玩记录、Start事件拒绝、真实Quit/X信号的重复关闭幂等、释放后level无效。它不证明用户可见，也不证明无限时间实测；保留原keyboard55秒cap和原测试。

tools/show-isolated-title-v001.ps1默认只准备并无窗口验证；只有明确Show及当次显示许可才启动Normal窗口。发起前验证刚完成keyboard案、当前源与六bin一致；不硬绑旧1695行。无显示许可本轮绝不运行live分支。完整十一项与v163/v164所有未完成门不关闭。

## 2026-10-30 启动边界补充

当次获准live在SceneTree._Initialize初始阶段失败：Root/Level/UI未入树或Ready，quit连接0。延后调用的session合同不能作为直接启动证据。Mount须在root已入树后创建场景，AddChild返回时严格确认Level/UI/Flow已Ready，随后同步锁定，禁止挂树后再留一帧未锁菜单。

原keyboard保持不变；同一冻结构建串行增加真正`--headless --script RunIsolatedTitleHold.cs -- --case-storage-root=... --contract-only`，其独立55秒owner及原日志/wait/marker全部纳入资格。合同参数只允许headless，live不接受合同自动退出。旧案无此entry证据不得继续用作live资格。

进程创建不等于标题Ready，更不等于用户可见。live只读观察启动最多55秒；失败/退出/缺Ready不得报Ready，不重试、不发送输入；启动观察结束不自动关闭已经admit的进程。健康live仍由用户关闭。此次失败已消耗显示尝试，本修复阶段不重复开窗。
