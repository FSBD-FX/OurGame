# v190：普通穿位与刹停通过，完整主线仍失败

## 事实

继续工程为 `G:/knight/Knight`，原十一项完整序章目标保持 ACTIVE/OPEN。本轮仅修改测试驾驶与所选验证副本，不修改生产物理、血量、能量、地图、敌人、机械爪优先级或美术。

首先纠正前提：玩家与普通锈壳兵的身体只检测 World，因此敌人能遮挡机械爪射线，不等于会像实体墙一样挡住行走。新测试用现有 A 输入走到两敌之间，再让实际 Motor 自然减速，没有写入位置或速度。

| 验证 | 结果 | 限定 |
|---|---|---|
| Actions RED，c9b86f26 | 916ms / exit1，预期失败 | planner stub 尚不接受记录中的候选 |
| Actions GREEN，f243a7a9 | 8498ms / exit0 | 六项算术控制、实际原 D 输入和无所有权 Dispose 保留；不等于战斗通过 |
| Main，4f0650b8 | 151396ms / exit1，严格失败 | 新走位局部完成；后续旧战斗死亡恢复 |
| 机械爪模型子集，468cdda0 | 637ms / exit0 | 不是全项目模型或通关资格 |
| 完整前段，582c0a2f | 180058ms / timeout，严格失败 | 保留原180000ms上限，不延时或重跑筛绿 |
| 完整后段，1504a97b | 224194ms / exit0，严格通过 | 原300000ms预算；不等于旧180秒通过或性能认证 |

真实走位记录在 Main 原始日志：physics6352 / outer199 开始，X4788.7393；原始部分悬边8.739px，仅允许向内缩小。6355取得完整棚面支撑。持 A21步，中性减速9步；6382 / outer229实际 X4681.068、vx0、axis0、完整同棚支撑。离散预测 X4681.0728，与实际约差0.005px。包括完成观察共占31个原 outer tick，没有新增等待池、J、K、跳跃或短喷；此段玩家HP2保持。

之后旧策略在6383向右防守，后续又向右离开两敌间隙。最终发生死亡恢复：`(4780.373,3423.9336) → (1680,3960)`，恢复1/0；不是新站位成功就可以忽略的失败。所需 owned Burst K 发出数仍为0。

原 admission 共160次，仅保留64行、遗漏96行；保留行 own preview 全为false。不能把64行当全部机会，更不能据此断言所有未留存的生产预览从未选择过Burst。两敌间站稳也不是成功抓取的替代回执。

## 推理与下一步

当前证据支持普通行走可通过，以及同一实际动力学核能在这段静态水平棚面预测刹停。不支持“敌人之间选敌优先级错误”或“必须额外K拉开Rust”。

下一步是原防守交回后的方向准备：在真实全源安全窗口内，验证向左转向、中立、生产端 fresh owned preview，再交给原来那一次 owned K；仍保留 own Shot、首J/致命J、Defeated、cast/application/Latched/Stunned/Idle与后续账本。不改角度优先、LOS、900预算，也不重置额外边沿。本轮没有实施这项后续策略。

前段超时与历史资源故障继续开放。本次 Main 局部控制器并不在 pre 路线执行，不能仅凭同时出现超时就归因给它；本轮尚无新的耗时边界证据。

## 独立审查与风险

唯一只读终审：Critical0 / Important0 / Minor2，无修补轮或二审。

1. 新控制器超过48行后没有 omitted计数或终态保留。本次34行未触及，但更长计划的详细诊断可能不完整；延期到长计划诊断工作，未记录状态不作推论。
2. 已启动后玩家或祖先queued时，单独Dispose可能不释放自己持有的A。当前外围Main的ReleaseAll及driver.Dispose仍清键；在独立复用前应增加主动失效原生负例并修正owned-key释放接口，暂不认证这种独立用法。

Burst技能实证、交回后的战斗、完整双路线、原十一项、美术/动作/UI/音频、正常渲染、目标硬件性能及用户试玩认可均未完成。动态棚面、旋转/其他碰撞形状、任意模块或通用寻路也不在当前固定布局控制器的认证范围；几何变化会失败关闭。暂停/queued等主动失效组合没有本轮新增完整原生覆盖，作为上述明确风险保留。

## 最终保管与权限记录

六案均构建成功、0 warning/0 error。构建耗时依上表顺序分别为28561、27591、30840、32262、30682、30440ms，均在180000ms原上限内。六案严格结果3PASS/3FAIL，其中一项是故意保留的TDD RED；最终同源五案为3PASS/2FAIL，不能称全绿。只有pre触及原上限，其余无timeout；全部guard差异为空。

post原75个enter/return按0–74同名有序配对，owner8次节点1→1。仅原4条限定WARNING：seamless1、room-profile1、flow-registry2；无ERROR/FATAL。pre没有标准ERROR/FATAL/WARNING，但没有完成/退出清理marker，仍按超时FAIL，不能据安静日志关闭历史资源原因。

本轮保管审计只核新六案，不重复重哈历史v189：30对manifest、2908份冻结source实体、64份helper实体、36份binary全部匹配。最终1872份运行源、6份binary和post的10项helper清单匹配。post为9项基础helper＋post-budget-consumer，不是11项。12个旧owned进程身份均已结束；其中一个旧Godot PID已被另一路径/新创建时间的node进程复用，未按旧PID操作该进程。

最终运行源清单SHA：`0739C5F3871946EBA9612814BF3DFA98666D73633888AA4908E276AB368B673E`；Knight.dll：`37FD940004F1C80CBA745749D0F4E937C4D70CD82A589A7BFAADA37E2117E051`。Main：`6FB8D3DCFD21D12DFDB3929268E1B74A7CB069B987E85B8046F81B36DEE9192F`；新控制器：`D158BB15CB4C3EA7FA02AA11C6A63D29D98FBA60C08B5EF723FB991DF54CE9BE`。canonical仅所选快照列表变动，SHA `51C7A18450CEE756172F7D35DA0F7611069A6616AE88EA114112F7DAC77017C7`；默认SourceRows1604未改，本轮显式1872。

post十项helper清单SHA：`48D515B7A1AB55E140806BE7E146CC24EF185CE77B91BB440974849706C9B28B`。

原始证据根：`G:/knight/验证证据/october13-stable-claw/`；本轮六个完整目录名如下，before/after、源/二进制/helper清单、实际argv/创建身份、build/native等待结果、stdout/stderr和失败均在各案内保留：

- `actions-20261101-062255-345-c9b86f267de9481f9fbff34c37b10e0c`
- `actions-20261101-062606-955-f243a7a965f04de8bd16f70277eed392`
- `thruster-route-main-diagnostic-20261101-062701-054-4f0650b88b594658a3a4875419e32825`
- `models-20261101-063327-667-468cdda0641046a5a3d3ec9cbf6a3cfb`
- `prologue-pre-20261101-063427-250-582c0a2f7f974bb3b24538f2660c937b`
- `prologue-post-20261101-063833-131-1504a97b6f924b40a5b607d62018bf8c`

HEAD仍 `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，分支未变，index SHA仍 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。没有Git变更操作。

06:43:18 +08手动媒体一次检查：7427750bytes /86files，baseline3508，Notify=false，无新媒体。仅隔离headless构建与测试、Godot内部键盘事件；未打开窗口、未发送OS输入，未操作真实存档、误发视频、付费生成、额外崩溃采集、自动化或删除。

下一独立任务的更精确证据：6438–6443实际Left防守、X4681.7905/vx0，但旧弹Active、safe0，不能取消必要防守。6444 / loop291首次原扫描safe0.56666666、generalDefend=false，实际aim仍Left/L仍被采样；旧代码此处却恢复move_right，6445变vx36.67/Right。优先尝试释放L并保持真实中立，而非默认再加A脉冲；如果实际方向不再Left，才用实际A样本修正。依旧等待现有生产端fresh预览与原post-Shot/short-window/Idle/CanGrapple等全部门。本轮没有实现或运行该后续方案。

计划：[mixed-lane-position](../superpowers/plans/2026-11-01-mixed-lane-position.md)；执行账本：`.superpowers/sdd/2026-11-01-mixed-lane-position/progress.md`。
