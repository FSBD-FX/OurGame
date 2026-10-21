# 遥测分类、重置观察与显示未通过 · v028

2026-10-19，Asia/Shanghai。完整1–11目标ACTIVE，Task6/7仍OPEN；不是正式交付。前序[v027](2026-10-19-visible-window-dual-route-v027.md)及全部原始案例保留，不回填资格。本轮没有修改生产运动、地图或美术；新增的是测试消费者、严格核验和记录。

## 事实、推理、风险

事实：用户再次明确，两轮自动主线/探索“没有看到游戏窗口”。新两案实际argv为Windows/windowed、没有headless，owner已请求Normal，返回窗口身份与引擎HWND一致；但没有用户可见确认。当前六阶段后台批77070已终态exit1，五门GREEN，post为真实native失败。结束后未发现本工作树的owned Godot/dotnet消费者，未重开窗口。

推理：这些启动记录只排除了新两案仍走旧contract→Hidden分支；置前成功、Godot HasFocus、内部Visible都不能推翻用户未见反馈。没有Windows session/桌面、窗口矩形、最小化/cloaked或连续显示证据，不猜测屏外、跨桌面或遮挡原因。批次session34564不是Windows SessionId。

风险：显示验证未通过，不是已向用户展示，也不是已试玩认可。现成标题观察runner可复用，但旧专用owner仍钉1594而非当前1695库存，不能绕过guard直接运行。已询问新的短前台时段；未答复前不抢焦点、不重跑长路线。完整内容、正式新50常驻0/50、24canonical、历史资源错误、真人/视听/60FPS/ZIP/GitHub仍未完成。

## 精确遥测分类，不改历史日志

新增独立`normal-route-telemetry-diagnostics-v2.ps1`，委托原C96D分类器，原文件与四个byte pin不改。只识别三种完整typed producer：SteamDrop的三jet记录、ShortcutBottom的左右jet记录、SteamBell的两个False fatal状态。先原实现RED，再补“缺失一个record”实际RED；完整schema原子验证后才暂时移除合法字段值再交原分类器，原始日志不变。真实同一行ERROR/WARNING/FATAL、RID/ObjectDB、错误类型/ID、重复/缺失/溢出/非有限数字全部继续拒绝。

当前纯测试重跑：actual owner绑定与完整Get-Task11Qualification接缝4999检查、原分类器3975断言、原资格15合同、原启动选择6合同及reset诊断11合同均GREEN。它们不启动游戏，不把v027 Explore失败案追认为GREEN；本轮没有新的正常Main/Explore资格。

## 重置观察：先保留RED，再区分权限撤销

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。

| 窄案 | build退出/ms | native退出/ms | 严格状态与边界 |
| --- | --- | --- | --- |
| thruster-seamless-reset-observer-20261019-062925-782-703e703f4f6246f690eeb3c124379c25 | 0/30914 | 1/1047 | 原Later==1断言RED；Area1/Shape1/Canvas3/ObjectDB4，external漏终态六行 |
| thruster-seamless-reset-observer-20261019-063123-337-2d199be257724c5cb07c2836220ec3d1 | 0/28566 | 1/1065 | 仅失败路径finally自有清理；同断言仍RED，退出上述诊断消失，external完整 |
| thruster-seamless-reset-observer-20261019-063242-400-2f3a038fa87e499d9498d9c6f5603f0a | 0/28667 | 0/919 | 行为/来源计数校验通过，但旧strict warning veto仍FAIL；原件不回填 |

被动native事实一致：targetLost1、旧LaterFailed0、同步新Idle1、下一tick Idle2、authority3→4，同一案Player30987519495/Claw31037851147身份不变。异常后的Reenter已执行；它同步StartNewGame先撤销旧authority，Later不应再收到旧Failed。最终断言验证权限递增、同身份、新Idle和原Mounted/路径/原一次physics预算，未撤销生产epoch gate。

cleanup-only仅把本夹具自有订阅与自行离树target清理移到finally，原Level QueueFree及两Process等待不变。这个组合对照支持本失败路径的退出诊断改善，不提供匿名RID/ObjectDB逐实例配对，也不关闭其他历史故障。前两案wrapper未ValidateStorageRoot、storage空且fixture无SaveStore，不能追认隔离协议；后两案实际验证隔离根。

故意抛异常的负例继续输出原warning。新窄owner要求唯一runtime来源/函数/数量成功标记及恰好一条指定warning，缺失、重复、其他warning/ERROR/FATAL/资源诊断均拒绝；两个raw合流和external分别检查。actual owner guard先RED再11合同GREEN，不删除日志，不对未解释生产诊断开豁免。

## 同冻结六门终态

整个batch包括阶段间隙均冻结source/helper/docs，无并发native消费者；每项fresh build零警告错误、无timeout，guards=[]。

| case | build ms | native退出/ms | strict | 日志行数¹ | actual selected |
| --- | ---: | --- | --- | --- | ---: |
| thruster-seamless-reset-observer-20261019-063523-999-bddb1dbe1c544cdcba9b3150d092f2a8 | 32774 | 0/2024 | GREEN | 7/0/5/62/67 | 167=161+6 |
| thruster-free-forecast-20261019-063614-875-e675d38a872b4112926325ba84fbfad1 | 26242 | 0/536 | GREEN | 7/0/4/0/4 | 167=162+5 |
| keyboard-20261019-063650-163-a15c927caf5e47e987064809c404ddcb | 27398 | 0/9988 | GREEN | 7/0/7/0/7 | 155=150+5 |
| actions-20261019-063734-906-97f636d21da44d438a6e61055c1c68d4 | 35532 | 0/7355 | GREEN | 7/0/10/0/10 | 155=150+5 |
| prologue-pre-20261019-063826-570-8448e5ee909348acb3a4ab800e354382 | 27713 | 0/101223 | GREEN | 7/0/334/0/334 | 182=176+6 |
| prologue-post-20261019-064043-717-8b80d46125894740844132730e7f2f05 | 29933 | 1/129163 | FAIL | 7/0/319/117/434 | 173=168+5 |

¹ build stdout/stderr、engine stdout/stderr、external。reset有唯一故意注入warning，不能写成零warning；其余四GREEN三raw无诊断。六案四库存对与continuation字节守恒，1695源码（含296 imported）、109用户保护hash、6binary；actual selected前后及实物逐hash一致。生产Player SHA仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。

```text
source 3AF1297A3FDC41A4ED1B433D50EE95466D87D956354CC0F7CA722D1463726481
binary6 78831E9427B87A61390395F2D6AF91E754515E213B17452182B925CC7F73C38C
DLL 176D444F666E430BC30F4E77C7723615090EB96B51455FBE33164DD24527A9AC
PDB 95CEC14B9EB5FE785046E1B9BDAA50F83891612448EB7F0CDEE3DF2E018CBC1B
final owner 94BBB66405B5AE13F9F0F734E4F1975697DB4102DA645007E61EAC5421DC61AE
```

post已过旧33样本/11段、四optional、真实Boss trigger及Seamless压力/reset；RunPost214原throwing-observer来源计数matcher已正常返回。但首行为ERROR在stderr101、`PrologueLevelRuntimeTests.GeneratedMainDeathDefersRestoreAndPreservesRecoveryClosure:256`：`requested==1 && completed==0 && HP==0 && requestSawHeldInput`，各实际子值尚未记录，不能判具体哪项失败。stderr56 RoomPresentationProfile负控warning不在该matcher内，GameFlow后续matcher未执行。stderr116另有真实2 ObjectDB，无对象ID，不能认领为旧音频或reset。external漏该warning及位置两行，stderr完整保留；没有新Handle/FATAL/RID。

首错测试和Room负控未进入本案selected副本，独立审计先与本案1695库存核对当前文件SHA才读取；不补写原件。下一复现须把这些真实依赖纳入新案例实际保留。不能把这次post称native0或仅被warning挡住，也不能扩大时限/忽略资源警告让它通过。

## 下一步与保留范围

1. 新短显示准入先停标题，用户确认可见后再角色阶段；无确认就不接长测。`RunIsolatedKeyboardProbe.cs`会在入树前覆盖三路外部存储并禁用AutoStart，只被动观察；旧owner资格需先精确迁移，不直接普通启动读取真实存档。
2. 后台先拆死亡复合断言实际值，保原输入/health公共路径及预算；再独立定位Room负控与匿名2 ObjectDB，不把测试前提推理当修复。
3. 原continuous Upper热Tuning/固定1/60/150horizon/zone中心Y、旧nested-reset测试及11文件余消费者仍OPEN；当前pre及V2证据不代替其迁移。

目标不缩减；无付费生成、新美术/音频素材、Git写入、ZIP、发布或自动化。保护存档只清单/hash，不读真实语义。本轮无新图稿/截图/录像展示；已有诊断PNG若要展示，仍须先按`E:/OurGame/Knight/过程图稿与视频`规范归档。E余3227172864字节约3.01GiB，未移删旧证据。
