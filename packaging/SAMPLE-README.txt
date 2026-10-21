无名钟楼 · 盆地品质样板（开发中 / 私人评审候选）

解压整个文件夹后运行 BasinSample.exe。
不要只复制 EXE：BasinSample.pck 和 data_Knight_windows_x86_64 必须留在旁边。
本包包含 Windows x64 .NET 运行时，不需要用源码启动器现场构建。
版本与文件校验以同目录 BUILD-MANIFEST.json 为准。

这次只体验：营地 → 劳动者战斗 → 机械爪上台 → 补给 → 返回营地。
返回营地出现本段完成提示，不代表整部序章已完成。
美术、音频、UI 和新手体验仍在迭代；不是正式发行或商业成绩证明。

操作
- A / D：左右移动；空格：跳跃；Shift：冲刺。
- Q：切换攻击模式，然后鼠标左键攻击。
- E：切换机械爪模式，指向可抓表面后按住鼠标左键。
- 抓住后 W 收绳、S 放绳，A / D 调整；空格助推，松开左键脱离。
- 鼠标右键：按住举盾。攻击/举盾消耗能量，长时间举盾会过热。
- 1：当前武器。2–4 是预留空插槽；选中空槽不能攻击。
- Esc：暂停、查看操作、返回标题或退出。

战斗提示
劳动者收锤蓄力时先准备防御；盾不可用时避开攻击。
机械爪可以开甲，随后切 Q 反击。详细提示会读取当前能量、过热和插槽状态。
满血时补给保留，受伤后可回来使用。沿上台路线返回营地即可结束本段。

存档
本样板使用独立用户目录 TheNamelessBelfry/BasinSample，内部为
knight/basin-quality-sample/save.json 和 settings.json，不覆盖原整关存档。
“开始新游戏”重建本样板进度；“继续”读取本样板检查点。

已知验证边界
同机清理启动进程 PATH / DOTNET_ROOT 不等于干净 Windows 虚拟机测试。
自动输入路线不代表首次真人游玩时长或审美认可。
此包尚未签名；不要据此关闭系统安全保护。
无自动更新、无支付或订阅入口；本次交付不包含公开发布授权。
第三方组件许可见 licenses 文件夹。

English quick controls
Run BasinSample.exe after extracting the whole folder. Keep the PCK and data folder.
A/D move · Space jump/attached boost · Shift dash · Q attack mode · E claw mode
LMB attack or hold grapple · W/S reel/pay out · RMB hold shield · 1 weapon · Esc pause.
This is a private development sample, not the complete prologue.
