# v298 · 蒸汽阀与环境提示使用实际交互键

推进 P03/G05/U03。默认通用提示和蒸汽阀提示在中英两语均把 Q 写死。`EnvironmentInteractionController` 现在使用与控制页一致的 `InputGlyphResolver.LiveKeyboardText(InputActions.Interact, language)`。普通世界绘制和迷雾上方的绘制继续共用同一个格式化入口：默认输出逐字不变，改绑会显示现有绑定，没有键盘绑定则显示 `未绑定`／`UNBOUND`。本次不新增改键菜单、不改变实际 InputMap、候选搜索、交互范围／遮挡、开启蒸汽阀和存档逻辑。

`.superpowers/sdd/2026-11-06-interaction-glyph/test-interaction-glyph-data.ps1` 读取实际生产双语表：改前 4 个默认样本正常，4 个替代、4 个未绑定样本全部错误保留 Q；改后 12 项数据检查通过。还原四处占位符后，全表 SHA256 `287265D0BEBCBC9C24E81924A73AFFEE5537C363495602925CFC192BA3B142B8` 与 v297 相同；v296/v297 字形数据回归通过。

在现有 `RunFormalEnvironmentInteractionTests.cs` 使用的真实蒸汽阀专项中新增本进程内临时改绑 F、未绑定及恢复原绑定的断言，要求候选身份不变，恢复后继续原 Q 真实激活与 K 远程对照。原测试和新断言本轮均未运行。雾上层现有像素测试仍需单独实测，不因共用文案签收；长修饰键在 200 px 提示宽度内的可读性也待验证。

机器可用内存读到 247／397 MiB，仍频繁换页，本轮未运行构建、Godot 或系统键鼠。裂隙挡墙、前段资源与完整视听仍开放。
