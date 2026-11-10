# v301 · 当前双语表通过实际解析器

Q05/G05/U03，范围为数据加载，不是当前程序构建或 Godot 运行。上轮推进了正式阀门严格入口；本轮没有堆叠新功能或改生产代码。机器可用物理内存读到约 131–264 MiB，仍频繁换页，没有运行编译或引擎，也未关闭其他程序。

核对 v292 规范冰墙通过案 `G:/knight/验证证据/october13-stable-claw/frozen-wall-sweep-20261106-195126-472-db48d552222d474eb2fcfdd8ae61aa2b`：封存源和二进制清单前后匹配；`binaries/Knight.dll` SHA256 `ABED6D0EB8C35B2270B9CA4A67798CA8ADEC873281F5182AD4AB66C9D7D6051A`。当前 `PrologueStrings.cs` 与声明语言枚举的 `GameSettingsData.cs` 分别和该清单完全一致。审阅 PE 的 `<Module>` 无初始化方法，另确认实际反射加载位置指向该封存 DLL。

在现有 PowerShell 进程中仅反射 `PrologueStrings.Load/Get/KeyCount` 与 `GameLanguage`，读取当前生产 `assets/localization/prologue-ui.json`（SHA256 `EFC94C1259C7FF99E8C4D07026D8B8B7AC3198377F4F796CD19DC96CE5E9EF1D`）。实际解析器接受两种语言各 107 个同键条目；读取并核对 24 条下降／方向／交互模板。两个内存反例（错误版本、缺英语交互键）都由解析器明确拒绝，原已加载双语表在失败后仍可用。执行退出 0、未加载 GodotSharp、输入文件与封存 DLL 的哈希不变。

该证据覆盖真实 JSON schema、双语键集、原始模板和加载失败隔离；不验证当前 `PrologueGuidanceModel` 或 `EnvironmentInteractionController` 的插值、场内 InputMap、Godot 导入／打包、雾上像素或长键布局。没有用旧代码替当前场内行为签收。下个原生入口仍是一次同源、原限时裂隙构建，随后复验同产物墙体、教学和正式阀门；当前资源故障与正式序章未完成。
