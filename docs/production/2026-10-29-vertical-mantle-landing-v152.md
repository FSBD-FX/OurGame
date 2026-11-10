# v152 — 竖直运动与落地披风余振，整关资格仍未通过

2026-10-29 收尾；实际原生运行发生在2026-10-28。本版不是正式发布版，原十一项范围不缩减。

## 实际制作

- `SecondaryMotionModel`沿用原解析弹簧与12×7偏移/.42角度限制，将真实竖直速度和既有落地信号接入披风Y目标。根部固定，末梢在上升/下落时拖拽，落地后有收敛余振；未改物理、输入、碰撞、伤害、资产或动作时长。
- 原生实际缺失行为RED后，同一真实Timeline生产链4组合（两朝向×180/650落速）GREEN。独立空中零速反事实区分落地冲量与减速；原27采样点、固定上带、升降符号与300帧归静保留。最大末梢Y=2.0666656。不是像素、正常速度或用户审美验收。
- 回归另暴露`PlayerFormalVisual`在合法离树逻辑姿势调用时直接索引未Ready的Weapon/NearArm/FarArm。仅改三次TryGetValue，原离树测试不改成挂载来隐藏问题；实际612ms RED→聚焦GREEN，随后最终同源rig专项亦通过。
- 仅三既有C#文件与canonical的一个选定源码副本条目改变，无新运行文件，1857源/897 C#。

## 全部实际运行，不拼绿

证据根：`G:/knight/验证证据/october13-stable-claw/`。19案均终态，未重启其句柄。

| 批次 | 结果 |
| --- | --- |
| 初始披风RED `040753-366-e942fb652ea34d1db610cbf89e1ac5ab` | build0/native1，命名缺失落地末梢断言 |
| 披风初始GREEN `040924-822-0d03d217f753479082e256e65c4b72a3` | build0/native0，原两delegate及4真实Timeline组合 |
| 加入升降独立符号控制后首八阶段 | 6PASS/2FAIL：rig的真实Weapon KeyNotFound；pre虽native0仍7条Handle ERROR |
| 三TryGetValue修正后原rig专项 `042358-094-0c1cba6808eb4fdfb9ee237516a2c2fe` | strict PASS，未重写原离树fixture |
| 最终同源八阶段（042611至043328） | models/mantle/rig/formal-claw/presentation-rig/keyboard六PASS；pre/post两FAIL |

最终pre `prologue-pre-20261028-043007-049-47f7976ed0aa40878432cfc0c39bed95`：native0/167919ms，但stderr9条Handle ERROR，external镜像9条，不能算18个独立故障或改绿。

最终post `prologue-post-20261028-043328-916-2a4b738743f3444baee3b918f15225bf`：native−1073741795/165715ms，实际gchandle.is_released()/0xC000001D FATAL。Task7仅12enter/11return，停在index11 HiddenDiscovery；另见low-ceiling-bridge.03配置缺失、会话仍Title。仅两个故意负测试warning scope到达，原75＋owner8和四warning完整门没有完成；不能称单纯wrapper误报，亦不能据此确定断言与FATAL共同根因。

19案总计14严格PASS/5FAIL（含两个预期行为RED），19build均0，无超时。Main/Explore未重跑，其历史死亡/恢复失败仍OPEN。

## 审查与保管

唯一代码复核C0/I1/M1；I1以原生RED→GREEN窄修正。M1为ledger最大值误抄，已按原日志纠正，不改原件；无第二轮代码审查。正常速度/像素、资源根因/双路、完整原十一项和候选Moth/UI未实现边界继续保留。实际协调器使用_Process，60Hz测试不证明不同渲染delta下单帧落地幅度等价。

独立审计分两次：前11案55清单对、5182选定源副本/114helper副本/66封存bin；后8案40清单对、1886选定源副本/42helper副本/48bin均匹配各案自己的清单。不是每案完整复制全部源码。中间一次代理额度错误未完成后八审计，后续2026-10-29另一次只读核对才收齐，不能倒签。

最终源清单SHA256 `989383F848B458456A47AD54F74B376D60AF2AEDD220E6A0C623142C5600C6D3`；普通6bin清单`F604B94B180556FC73491034FDADC646E5CF3809A942F0374AF2D686F4826FA7`；canonical `6A516509714E9BDAA181C787806D72FE78C66A0C97F3C3D365D50261C6A6CAFE`。HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6/index21610FDC…未变。精确原进程身份均终态；一PID已被不同出生时间的新进程复用，未触碰，不声称全系统空闲。

当前helper例外：历史Codex c602dfdab3808440/rg.exe已不在，当前为0354bc6c584f210c/rg.exe、SHA8F1E7C142E486F0727A6CF631BEB286E13104A05E756B009F8AA50AD5CA1711D；其余后八涉及现存helper一致。旧归档副本匹配，但不能说全部当前helper不变，亦不伪造补回旧rg。

## 后续与边界

原十一项/正式美术动作UI音频/完整路线/资源稳定/存档兼容/硬件与用户试玩/交付全部仍OPEN。下一修墓灯蛾已批准的金色起手眼灯，同时只读推进具体原生资源边界，不以随机保活、GC或升级引擎猜修。没有再次ProcDump采集、前台/系统输入、付费、美术生成、新媒体、自动化、Git写入或删除；真实存档未读取内容或改动。
