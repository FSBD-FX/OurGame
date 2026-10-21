# 盆地样板：独立 Windows 包前置检查

最新接续：`39b2eb8` 已重新冻结并生成私人开发 ZIP，原生标题/Start、包内运行时加载、独立 PCK 审计及全新目录解压哈希验证已完成；原生整圈/死亡/继续尚未完成。详细事实、导出退出码边界及候选哈希见 [2026-09-09 私人候选](2026-09-09-basin-private-candidate.md)。下文“尚未 ZIP”与“尚待重导出”属于此前检查状态，不是当前候选状态。

## 2026-09-09 最新：原生入口通过，死亡修复后需重导出

`6671d3bb91bc6821e33559d38cc2111644f902e7` 已冻结资源读取修复并实际重导出，`BasinSample.exe` 普通标题与 Start 进入营地可见。Windows 模块清单证明 hostfxr/hostpolicy/coreclr 从包内数据目录加载。完整自包含目录 191 文件；PCK SHA-256 `333c73b933b1f8ad340ed66b211d2c3d1081cae770191e190ffe3fa4bdeead0d`，496 entries / 14 JSON / 76 音频 remap+sample 均逐项通过静态核验，237 C# entries 全为一字节空白 stub，无测试/工具/证据/存档泄露。

但该原生试玩在实际操作后出现 22 条跨区死亡恢复 collision flush 错误，退出 0 不能覆盖此失败。当前优先修复，并补真实 AreaEntered 跨区致死和默认流程回归，见 [死亡恢复检查](2026-09-09-basin-death-contact.md)。`6671d3b` 及旧 `7e13912` 导出均原样保留为诊断候选，不能当最终可交付版本。重导出、原生死亡后完整循环、退出继续、装包/ZIP 新目录解压仍待验证。

已取得完整 Godot 4.7.1 / NuGet runtime pack 9.0.19 notices；`packaging/SAMPLE-README.txt` 有开发状态、完整运行时和操作说明。尚未复制成正式交付目录或 ZIP。无新付费、源码上传、公开发行或用户品质确认。

## 较早检查记录：导出已跑通，正在核对资源读取候选

下方“未下载/缺模板”是 9 月 8 日前置审计的历史结论，不是当前状态。

- 官方模板已按原记录的 bytes/SHA-256 验证，仅安装 4.7.1 Mono Windows x64 六个匹配成员。
- `7e13912aa289b6e512750e76c00f1f55dec9570f` 冻结了样板 feature override、solution、preset 和 commit 白名单 staging。
- 该提交实际导出退出 0，完整运行时为 .NET 9.0.19；原生普通窗口启动却出现音频 manifest missing 与本地化 DirectoryNotFound，后续菜单初始化失败。`--quit-after` 返回 0 不代表无运行时错误，必须核对日志和画面。
- 已最小修复两条资源读取路径，并保留音频绝对路径工具接口；null/空白路径契约有 RED→GREEN。真实临时 PCK 同时验证音频清单和双语菜单表，不假装此小 fixture 等于整个发行包资源验证。
- 包源脚本新增 Windows NT final-path 身份检查，消除 junction 等路径别名绕过；所有非 transform 文件按 Git blob 核对，插件清空和拒绝覆盖/别名案例均有有效 RED 与 GREEN。
- Godot / runtime pack notices 与版本来源在 `packaging/licenses/PROVENANCE.md`；实际 runtime pack 文件与 NuGet 9.0.19 按 SHA-256 核对，不能改用整个 SDK 的许可。
- 源运行的 packed-resource、Task4AudioVfx、Basin 生命周期、普通窗口误用过热→完整回路已通过。源短片 `source-proof.mp4` 为 571 帧/30 FPS，SHA-256 `c4ad5cd2c41a076ed9e55bddbbf9f09c3861e6abb993fa1adc289cd1f4fd6b74`，已审看 2Hz 抽帧板和返回营地原图。它不是原生独立 EXE 的完整回路证明。
- 官方 release template 不接受外部 `--script`；同版 editor host 直接加载 PCK 的尝试也未加载 release C# class，失败日志保留。未修改模板或加入测试后门。已发现可用 Windows Computer Use 接口，后续直接检查独立窗口。
- 证据目录：`E:/OurGame/Knight/.evidence/september08-basin-package`。新冻结候选的实际重导出、native 新局/继续和 ZIP 解压核对尚待继续；不得沿用旧失败包为成功证据。

---

日期：2026-09-08。只读审计基线为 `90b7696eab299e60298fa8c6f7530f7d06890c0c`；当时正在修改战斗教学，工作树非干净。本文件不是导出成功证明，也不是发行许可。

## 事实

- `Play-Basin-Sample.cmd` 调用 `tools/play-basin-sample.ps1`。它检查本机 Godot Mono 和 .NET SDK 9，现场构建源码，再打开样板场景；脚本本身已声明不是 standalone package。
- 本机有 Godot `4.7.1-stable.mono`、.NET SDK `9.0.317` 与匹配的 Godot C# 构建包。不需要为编译侧重复安装 SDK。
- 工程没有 `export_presets.cfg`。`C:/Users/22499/AppData/Roaming/Godot/export_templates` 存在但为空；审计未在所查用户目录和 `G:/godot` 中找到匹配的模板包。
- `project.godot` 默认仍启动完整序章 `PrologueLevel.tscn`；直接导出默认工程不等于导出盆地样板。
- 样板入口具有正常标题页、Start、独立存档/设置根；它通过 C# 字符串加载完整关卡及样板扇区，不能假定单场景依赖扫描能找到全部资源。
- `Knight.csproj` 没有排除测试/工具代码。活跃工作树还包含未跟踪 C#、证据图片、导入噪声与旧相机草稿；不能直接把它当成可复现发行源。
- 现有 `VERSION.txt` 指向旧 r2 和旧启动入口，不能原样用于新的样板包。

## 推理：下一次最小交付路线

1. 冻结已审查的精确开发提交；不修改或删除用户的未完成工作来追求表面干净。
2. 从官方来源核实并取得精确匹配的 Mono Windows x64 导出模板，不升级当前引擎；先记录下载文件、大小和校验依据。
3. 建立可追溯的样板导出预设/入口隔离。可评估独立 feature override 或白名单 staging 项目，先实际验证默认整关入口未改变。
4. 从冻结提交制作白名单 staging 工程，包含实际运行依赖，排除测试、工具、证据、代理配置和未跟踪文件。动态加载依赖须通过实际导出运行验证，不能只看导出退出码。
5. 导出后保留完整 runtime 目录，不能只复制 EXE。包内放真实版本/SHA、启动与操作说明、已知问题和第三方 notices；只对该目录压 ZIP。
6. 解压到新目录验证标题、新局、整圈、退出重启、继续与死亡恢复。优先使用无 SDK/编辑器的干净 Windows 环境；同机清理子进程 PATH/DOTNET_ROOT 仅能作为较弱证据，不证明未访问其他已安装运行时。

## 风险与边界

- 禁止递归打包工作树根；`.git`、`.godot`、`.worktrees`、`.agents`、`.superpowers`、测试、工具、原录像、日志和存档/设置不应进入试玩包。
- 包资源过滤并不自动排除已编译进 `Knight.dll` 的测试代码，源码 staging 和资源清单都要检查。
- 不触碰或删除本机 SDK/编辑器来模拟干净机器。测试环境设置只影响被启动的子进程。
- Godot/第三方许可证清单尚未整理；未签名 EXE 的 SmartScreen 提示也不能被解释成安全认证通过。
- 当前没有生成新 ZIP、GitHub 标签/附件或公开发行。模板、导出预设、独立包和解压运行验证均待执行。无付费生成、安装收费软件或用户数据上传。

结论：独立交付有明确可执行的前置工作；现阶段只能称本机开发入口，不能称免 SDK 成品。此项与用户审美/真人试玩验收分别记账。

## 同日官方来源核实（未下载、未安装、未导出）

- 官方 [4.7.1-stable Release](https://github.com/godotengine/godot-builds/releases/tag/4.7.1-stable) 的正确资产是 `Godot_v4.7.1-stable_mono_export_templates.tpz`，为跨平台 Mono 模板合集，Windows x64 没有单独的此类下载。官方 [Release API](https://api.github.com/repos/godotengine/godot-builds/releases/tags/4.7.1-stable) 报告 `1,201,759,011` bytes，SHA256 `ef9a708be51ecd974cd7dccdcafd7a1870da3d3e1c24c072bdbb9818c7a7db63`。下载后仍须本地校验；当前没有实物核验内部模板文件名。不要把114,763,108 bytes的 `mono_win64.zip` 编辑器或非Mono模板当成它。
- [Godot 4.7 C# basics](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/c_sharp_basics.html) 与 [4.7.1 BuildSystem.cs](https://github.com/godotengine/godot/blob/4.7.1-stable/modules/mono/editor/GodotTools/GodotTools/Build/BuildSystem.cs#L230-L240) 确认导出器带 RID 执行 `dotnet publish --self-contained true`。因此不是再给目标玩家安装 SDK；构建机仍需 SDK。[官方 .NET 编译说明](https://docs.godotengine.org/en/4.7/engine_details/development/compiling/compiling_with_dotnet.html) 要求一并分发导出生成的数据目录。
- 新确认第三项缺口：工作树没有 `.sln`/`.slnx`；[4.7.1 ExportPlugin.cs](https://github.com/godotengine/godot/blob/4.7.1-stable/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs#L130-L136) 在存在C#而无solution时终止导出。因此需要可追溯地生成含 `Knight.csproj` 的 solution，再实际验证导出。
- [Feature tags](https://docs.godotengine.org/en/4.7/tutorials/export/feature_tags.html) 支持导出 custom feature 覆盖 Project Settings，且不影响编辑器普通运行；结合 [main_scene定义](https://docs.godotengine.org/en/4.7/classes/class_projectsettings.html#class-projectsettings-property-application-run-main-scene)，可推导用 `basin_sample` 隔离导出主入口。此方案仍需导出后实测，不能把文档支持当作本项目运行成功。
- [4.7.1 ExportPlugin.cs选项](https://github.com/godotengine/godot/blob/4.7.1-stable/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs#L60-L100) 显示默认不带C#源码，但默认带调试符号；评审包可明确关闭符号。无论是否嵌入输出，都须审计整个runtime目录，而非递归ZIP工程。
