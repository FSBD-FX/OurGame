# 盆地品质样板：b98cc47 私人候选

日期：2026-09-09。源码冻结：`b98cc476fff9d1b3d0ffdcd2c6553ae51006849b`。

## 本轮交付

在现有样板范围内，把已回归的“收绳后 Space 助推起音”修复纳入新的独立候选。与旧 39b2eb8 的 runtime 源码相比，只改动 `PrologueAudioDirector.cs`；没有更换音色、扩大地图或改变技能数值。旧 ZIP、旧候选目录、旧源码检查点和工作树无关改动均保留。

- 候选目录：`E:/OurGame/Knight/.build-deliveries/BasinSample-2026.09.09-dev.b98cc47/`
- ZIP：`E:/OurGame/Knight/.build-deliveries/BasinSample-2026.09.09-dev.b98cc47.zip`
- ZIP bytes：`78425396`
- ZIP SHA-256：`95a29570493ef4d99b259222f5d5245600a3355ff6f48e0efeb0bf6aa134df9d`
- 解压检查目录：`E:/OurGame/Knight/.build-checks/basin-b98cc47-zip/BasinSample-2026.09.09-dev.b98cc47/`
- 199 文件：191 runtime、操作说明、已知问题、5 个 licenses/provenance 文件、manifest。manifest 精确绑定源码和其余 198 文件的路径/大小/SHA-256；ZIP 自身另附哈希文本。
- 全新解压后的 199 文件集合、198 文件大小/哈希，以及 manifest 本身哈希逐项一致。
- 旧 39b2eb8 ZIP 仍为 SHA-256 `87af179deccba6074e7765e1c987415d4827baacdcc5b144a1700191e01379c0`。

## 构建、导入异常与运行：分别记账

1. `tools/stage-basin-package.ps1` 从冻结提交生成新的 `E:/OurGame/Knight/.build-staging/basin-b98cc47/`，689 个白名单文件；staging 的生产构建实际退出 0，0 警告/错误，不包含未提交的音频总线、测试或 import 噪声。
2. 首次实际 Godot.exe `--headless --editor --import --quit` 在日志显示 86 项资源导入和编辑器布局完成后，进程 PID 86968 **实际退出 -1073741819 / 0xC0000005**。Windows Application Error 1000（01:39:55）和 WER 1001（01:40:00）确认访问冲突，故障模块为 Godot.exe、偏移 `0x3ec964d`。`b98-import.log` 没有错误文字不等于进程成功；根因没有确定，也没有修改引擎或通过反复重跑消除失败记录。
3. 随后独立导出到新的 runtime 目录；实际 Godot.exe PID 95384 通过进程句柄 WaitForExit 得到退出 0，`b98-export.log` 完成 dotnet publish/savepack 且无 ERROR/WARNING。这不覆盖首次冷导入异常。
4. 新候选和新解压目录分别在不同隔离 APPDATA、清空子进程 DOTNET_ROOT/ROOT_X64/ROLL_FORWARD、只保留系统 PATH 下，用 `BasinSample.console.exe --headless --quit-after 10` 启动，**两次均实际退出 0**。`native-b98cc47-headless.log`、`native-b98cc47-extracted-headless.log` 无 ERROR/WARNING。全程不占用键鼠；没有触碰通常使用的样板/整关存档。
5. EXE 实际为 NotSigned。此处是同机无窗口检查，不是图形游玩、扬声器输出或干净 Windows 环境认证；不建议用户关闭系统安全保护。

以上日志位于 `E:/OurGame/Knight/.evidence/september08-basin-package/`。

## 静态包与依赖核验

独立审计报告 `E:/OurGame/Knight/.evidence/september08-basin-package/pck-b98cc47-audit.json` 已读回核对：

- PCK 为 1,523,276 bytes，SHA-256 `29c7b5eca41eb64bc3c521b44e8779ccbc39b3b95fe02fe15bc681af5776c57a`；496 项，边界、MD5、重复路径和非零条目 flags 无异常。
- 14 JSON、76 WAV import/sample、10 PNG import/ctex、35 remap 闭包完整；237 个 C# entry 都是单换行 stub，没有源码正文、tests/tools/editor addons/调试符号或玩家数据。
- 与旧 runtime 的 191 个文件逐路径比较，只有 PCK 和 Knight.dll 改变，其余 189 个文件相同。仍是 .NET 9.0.19 / win-x64，核心运行时 commit `8381bdb01fe4a26e1f61370e779c78bb73ecc95b`；旧依赖 notices 适用对象未变，原样复制后四份原始 notices 的预期 SHA-256 再次通过。
- 新版重新生成操作说明、已知问题与 manifest，没有把旧 39b2eb8 的图形标题/营地或终态证据归给新二进制。

## 尚未完成与下一步

目标没有完成。新包的死亡/完整盆地循环、实际声音时机和混音、短录像审看、用户视觉方向及品质评审均待进行。图形启动、正常继续的后续证据见下节。上升邻区两种间歇失败和镜头裁切也未被修复；不以本段完成卡代表整部序章完成。

窗口空闲/可操作请求在 39b2eb8 交付、b98 音频修复、本轮新包三个连续目标回合均未获得用户回复；此前仍分别完成了不需接管桌面的工作。现在没有能替代真实窗口与用户品质判断的后台检查可继续推进这一验收。保留完整目标和新旧候选，等待用户明确允许操作窗口后进行下一步，不再重复导出或靠素材/测试数量冒充体验进展。

不新增付费、外测招募、个人数据上传、GitHub 上传或公开发行标签。5–8 分钟真人时长、参考游戏品质和十万付费均没有取得相应验证。

## 用户允许窗口操作后的实际检查（02:02 起）

用户回复“方便”后恢复窗口检查，既有目标已恢复 active；上方三轮等待是历史记录，不沿用为新的 blocked 计数。

- 从上述已核验的新解压目录启动 `BasinSample.exe`，实际 PID 42604。子进程清空 DOTNET_ROOT/ROOT_X64/ROLL_FORWARD，PATH 仅保留 Windows 系统目录。
- 使用新隔离 APPDATA `E:/OurGame/Knight/.evidence/september08-basin-package/native-b98cc47-window-appdata-20260909-020252`，未改动正常用户存档。
- 原生窗口实际显示标题；新隔离档的“继续”不可用。鼠标打开“控制”并返回，再点“开始新游戏”正常进入盆地营地。窗口证据为 `native-b98-controls.png`、`native-b98-newgame.png`，位于同一 evidence 目录。这些才是新 b98 二进制自身的图形证据。
- 鼠标点击攻击后可见能量下降；随后恢复。窗口进程 Responding=True。不能据静止截图声称完整动画、声音或整圈通过。
- Computer Use 的 `press_key e` 与 `press_key Escape` 后未观察到切换/暂停。重新激活、核对 Godot 焦点、点击画布后再试一次 Esc 仍未观察到暂停。已停止键鼠注入，请用户亲手按 E/Esc 作对照；尚不能区分工具模拟按键与游戏输入问题。
- 独立只读核对本地 GodotSharp 4.7.1 常量：DeviceIdKeyboard=16、DeviceIdMouse=32，故项目绑定的设备值不是异常设备号。E/Esc 绑定与生产轮询未发现不一致；现有自动输入测试使用 ActionPress/InputEventAction，绕过原生窗口的物理键事件投递，不能由既有通过记录证明这一输入路径。实体键同样失败时，先记录实际收到的键盘事件再定位，不猜测修改绑定设备。
- 主菜单/暂停的通用控制说明缺少“按住左键抓附、W/S 收放绳、Space 助推、松开左键脱离”，且 A/D 被合写为“移动/瞄准”。样板情境卡已有相应教学；这是参考说明缺口，不应误写成整个样板完全没有机械爪教学。当前只确认问题，未修改冻结包。

### 实体键盘对照与继续流程：随后取得的新证据

用户明确回复亲手按 E/Esc“可以”。主代理随后重新查看同一 b98 窗口，实际见到机械爪模式选中与暂停菜单同时显示（`native-b98-user-keyboard-pause.png`）。因此该次键盘故障未在实体操作复现；不修改设备号/输入映射，也不把 Computer Use 注入失败称作游戏 bug。

鼠标正常点击暂停菜单“返回标题”，标题页的“继续”变为可用；再点“继续”，实际回到营地，角色、满血资源、教学卡和劳动者正常可见（`native-b98-continue-camp.png`）。读取本次隔离存档也确认 `checkpointSector=CanyonBasin`、坐标 `(2380,2014)`、`completed=false`，没有误记整段或整关完成。

随后停止输入，窗口留给用户试玩。尚未取得退出码、死亡恢复/完整原生路线或新录像；本次证据只覆盖实体 E/Esc 对照及标题继续流程，不是完成验收。
