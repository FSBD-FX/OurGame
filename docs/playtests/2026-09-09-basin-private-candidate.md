# 盆地品质样板：39b2eb8 私人候选

日期：2026-09-09。源码冻结：`39b2eb8c5af57c59a030d04712ccb8dc82f90950`。

## 本轮真实进展

- 继续既有 active goal，不缩小或宣称完成品质目标。上一轮的死亡恢复修复已有源码检查点；本轮推进独立可运行交付，并为未稳定复现的问题补充观测。
- 当前工作树仍在隔离分支。原 import、音频总线、相机测试草稿等无关改动均保留；只从提交的 runtime 白名单生成新 staging，共 689 文件。
- `death-final-generated-trace-2.log` 的实际失败是最终跨缝跳跃后仍在 CanyonAscent，并非前次第三爪后横移超时。第三爪落地及横移与通过记录连续 229 行一致，不能据此改地图。
- 只在测试的最终跨缝窗口增加 `FINAL_SEAM_TRACE`；8 帧按住拆为 1+7 帧用于首帧观测，输入顺序、120 帧等待、断言和生产玩法均不变。独立只读审查无可行动问题。
- 构建 0 警告/错误；`death-final-generated-trace-3.log` 实际退出 0，五区、4 次恢复、Boss 重试/结算通过。新增记录显示 held-1 仍跳数 6，held-8 为 7，最终进入 LostTemple。本轮没有捕获失败时的跳跃状态，因此两种间歇失败仍未定因；结束有界复现，不无限重跑直至全绿。

## 导出与原生启动

本次模板和 .NET 版本均未升级。初次 console 导出到 `runtime/`，savepack 完成、191 文件生成，但 console wrapper 在无子进程后仍驻留；以 Ctrl-C 结束该会话，退出 1，不写成导出成功退出 0。

第二次使用实际 Godot.exe、显式 `--quit` 导出到全新 `runtime-checked/`。savepack 完成、进程随后消失、文件生成；GUI 程序异步启动，未取得引擎自身退出码，不能把启动 shell 的 0 当成引擎 0。后续实际原生运行与资源核验独立记账。

从新私人候选目录启动 `BasinSample.console.exe`，清空子进程 DOTNET_ROOT / DOTNET_ROOT_X64 / DOTNET_ROLL_FORWARD，PATH 仅 Windows 系统目录；APPDATA 指向新的证据隔离目录，不覆盖用户原样板/整关存档。

Windows Computer Use 已观察标题，正常点击开始新游戏后观察到营地、角色、敌人、台面和教学 UI。实际进程模块 hostfxr.dll / hostpolicy.dll / coreclr.dll 均位于候选包自己的 `data_Knight_windows_x86_64`。这是同机检查，不等于干净 Windows 无 SDK 验证。

前台在检查过程中被切换到非游戏窗口。停止键鼠操作，并已向用户提出空闲时段请求；不把未完成的菜单继续、整圈和原生跨区死亡写成通过。打包时原生检查会话仍 live；后续只读检查确认 PID 86308 已自行退出，原会话 15263 返回实际退出 0，终态 `native-39b2eb8-first.log` 仅引擎/GPU 信息，无 ERROR/WARNING。这只结束该次启动检查，未补造未操作的流程。

## 私人开发包

不是正式发行、重大体验版本或用户品质认证，未上传 GitHub，也未覆盖旧 r1/r2。

- 候选：`E:/OurGame/Knight/.build-deliveries/BasinSample-2026.09.09-dev.39b2eb8/`
- ZIP：`E:/OurGame/Knight/.build-deliveries/BasinSample-2026.09.09-dev.39b2eb8.zip`
- ZIP bytes：`78423672`
- ZIP SHA-256：`87af179deccba6074e7765e1c987415d4827baacdcc5b144a1700191e01379c0`
- 199 文件；`BUILD-MANIFEST.json` 记录完整源码提交、引擎/运行时/入口及其余 198 文件的相对路径、bytes、SHA-256。manifest 本身由 ZIP 哈希覆盖，另附 SHA 文本文件。
- 包含完整 191 文件 runtime、操作说明、5 个第三方 notices/provenance、已知问题及 manifest；不复制旧 VERSION.txt，不夹带 staging 源归档、测试、工具、证据、SDK 或玩家存档。
- 已解压到全新 `E:/OurGame/Knight/.build-checks/basin-39b2eb8-zip/BasinSample-2026.09.09-dev.39b2eb8/`；199 文件集合、198 文件哈希/大小与 manifest 本身哈希逐项一致。后续在新的隔离 APPDATA、清空子进程 DOTNET_ROOT/ROOT_X64/ROLL_FORWARD、仅系统 PATH 下，从解压目录执行 `BasinSample.console.exe --headless --quit-after 10`；实际退出 0，`native-39b2eb8-extracted-headless.log` 无 ERROR/WARNING。该检查不占用键鼠，只证明解压目录无窗口启动，不代替图形窗口启动和完整游玩。

独立静态审计：新 PCK 为 1,518,252 bytes，SHA-256 `3cb4863e979878cd91aef31a12b69739885c32e337b5b2165ed2d3cd38497c5c`，496 entries；逐项边界/MD5、重复路径和 flags 无异常。14 JSON 与新 staging 一致且可解析；76 WAV import→sample、10 PNG import→ctex、35 remap 目标完整，237 C# entries 都是单换行 stub。完整 runtime 为 191 文件（185 DLL、3 EXE、2 JSON、1 PCK），无 tests/tools/docs/source/PDB/XML/log/zip。runtimeconfig 与 deps 指向 Microsoft.NETCore.App 9.0.19 / win-x64，关键 CLR 文件版本绑定官方 commit `8381bdb01fe4a26e1f61370e779c78bb73ecc95b`。此审计不代替原生流程检查，也不补造未取得的导出退出码。

声音仅补技术读数：对较早 `source-proof.mp4` 的实际音轨执行 volumedetect，48kHz 双声道，mean -19.5dB、max -2.6dB；未据此判为音色/混音/提示时机优秀，更不是修复后独立包的声音验收。

## 尚未完成

1. 用户空闲后，在解压目录检查图形窗口启动、正常继续、真实跨区死亡后完整样板循环。不并发启动多个游戏，也不抢占键鼠。
2. 保留上升段两种间歇失败与相机裁切；下次发生时使用新增观测区分输入未接收和运动/碰撞/触发失败。
3. 声音、统一视听、首次真人上手与用户品质评审。5–8 分钟和十万付费均未验证；goal 保持 active。

源码后续已有机械爪助推起音修复，见 `2026-09-09-basin-boost-audio.md`；本页的 39b2eb8 包和所有哈希保持不变，不将源码改动描述为已进入这个 ZIP。
