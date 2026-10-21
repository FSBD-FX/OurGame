# 盆地样板：机械爪助推声音起音

日期：2026-09-09。基于 `c6e2a6d`；沿用现有品质样板目标，不扩建整关。

## 事实、判断与边界

- 只读核查确认满血补给保留符合当前设计：Guide 仍显示“补给已保留”和归营完成；`discovery=False / used=False / healed=0` 不能当作没有探索反馈。现有补给测试另覆盖受伤后重新进入、治疗、消费和持久化，本轮不强迫玩家受伤或改奖励语义。
- 教学要求先 W 收绳再 Space 助推。生产 `MechanicalClawModel` 为已接受的输入发布独立 Boost 状态，但音频将 Reel/Swing/Boost 都映射到 ClawTension，随后同一 CastId 的同 cue 去重把助推起音吞掉。
- 修复只允许独立 Boost 状态穿过同音色家族去重，保留重复帧去重以及返回 Reel/Swing 后的防重复。没有新增声音资源、enum、VFX、技能规则或数值；这是一次额外起音，不是新增专属音色，也未据此判断音色或混音优秀。

## 已执行验证

证据目录：`E:/OurGame/Knight/.evidence/september08-basin-package/`。

1. 先加真实 AudioDirector 边界测试，再改生产代码。`boost-audio-red.log` 实际退出 1，明确失败为已接受 Space 助推预期计数 20、实际 19；RED 提前退出另有清理告警，未掩盖。
2. 最小生产改动后构建实际退出 0，0 警告/错误。`boost-audio-green.log` 的 RunTask4AudioVfxTests 实际退出 0，无 ERROR/WARNING。测试覆盖持续 Reel/Swing 一次、助推新增一次、重复 Boost 及助推后 Reel/Swing 不重复。
3. 扩展既有 Task10 生产夹具：真正 InputMap 的 W 收绳、Space 接受为 Boost 且 BoostConsumed=true，随后 cue 计数精确增加一次；第二次空中 Space 被拒绝，不再增加。`boost-audio-production.log` 的 `RunTask10Tests --cue-vfx` 实际退出 0，无 ERROR/WARNING；同时覆盖原模式/盾/过热/机械爪及暂停、死亡、卸载、完成清理。
4. 独立只读审查未发现可行动问题；确认重复帧、助推后连续状态、finally 按键释放和原生命周期均保留。同一次挂接落地后再次合法助推未在本轮直接跑到，作为低风险覆盖边界保留，不称已测。

这条生产夹具沿用受控目标和停止物理的 Motor，证明输入→模型→发布者→声音事件，不冒充正常移动路线或真人试听。普通窗口完整回路的助推声音与混音仍未审查；用户可用键鼠时再验证，不抢占当前前台。

## 交付状态

39b2eb8 原生检查进程已自行退出，原会话实际退出 0，终态日志无 ERROR/WARNING。原 ZIP 解压目录的隔离无窗口启动也实际退出 0；图形解压启动和原生完整回路仍未通过。详见 `2026-09-09-basin-private-candidate.md`。

现有 `BasinSample-2026.09.09-dev.39b2eb8.zip` 不覆盖、不重命名，**不含本次声音源码修复**。没有新付费生成、GitHub 上传、发行标签或品质完成声明；goal 保持 active。
