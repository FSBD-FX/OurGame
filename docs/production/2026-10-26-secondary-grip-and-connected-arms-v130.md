# v130 · 双手握持与连通关节臂

2026-10-26，KNIGHT，`G:/knight/Knight`。v130是内部制作记录，不是发布版或GitHub tag。原十一项和所有稳定ID继续ACTIVE；没有缩成握持专项，也没有完成整关、美术或真人验收。

## 事实、推理、风险

- 事实：保存Player的378个持握样本原最大脱柄距离39.81586、目标肩腕距离42.530872，旧单杆半径19不够。获批左握持PNG未绑定。真实RED后，固定24+24关节臂和原图接入，最终最大脱柄距离0.000008997。
- 推理：需要完整的肩—肘—腕链，而不是只贴手指、平移肩或拉长图片。原十节点/typed姿态、主握点和碰撞不改；新腕点由实际武器空间变到远臂空间，根镜像一次。
- 风险：几何接触不等于肘部好看、布料遮挡自然或正常速度流畅。身体/披风仍不是新获批总装；没有付费生成。用户最后明确未看到窗口，继续记NOTSEEN，不自动重开。

## 实际制作

[规格](../superpowers/specs/2026-10-26-secondary-grip-draw-constraint.md)。近远臂只改Draw：固定两段各24，保留9/8宽和原陶瓷颜色；肩节点不挪，腿不改。无效腕目标回到自由手、超48有界夹取、零向量仍保持有限完整骨段。ArmDrawRect包含关节/线宽，实际相机观察器保留旧7点并追加两臂8角。

原左片完整1402×1122，SHA256 `058DB41E707619C25D1416DD5EDA4BD6BC00F8407DE2EBDF18B373D8FA7A6EBE`，alpha区域525,777,47,45。只复制原字节至[运行资源](../../assets/player/thread-spool-bearer/left-grip-overlay.png)，不改像素；主握835,798/次握548,798、比例25/287和刀身相同。只有真实副手抵达才画手指遮挡。

None/三种攻击保持握持，Thread/Shield/Hurt/Dead/Recovery松副手并保持整臂连接。独立审查发现真实机械爪常从normalized=1进入，旧新实现直接消费它会突跳。有效native RED测得切入38.835922、首None回握2.5597324；使用已有有界thread-entry与同一action-release步幅后，最终分别7.109601、0，途中反向切换/相位回退也通过原12单位上限。无额外Process/时钟，不修改原节点姿态或游戏判定。此上限是每次姿态发布，不是渲染帧或真人流畅度认证。

保留原36绑定、原3机械爪、完整75注册；新增378持握/2父变换、固定骨长/独立线宽、5无效目标、8自由模式、death/recovery/cue、2只改Draw的裁切负例、真实Timeline映射的三种终态/中断/回退。相机负例中旧点、所有slot矩阵、CurrentPose/武器不变，肩肘腕中心仍在安全框内，但真实腕墨边越界；旧观察器确实RED后才修正。

## 全部尝试与最终回归

共同证据根：`G:/knight/验证证据/october13-stable-claw`。19尝试=11严格PASS/7原生FAIL/1构建FAIL；18原生均无timeout/guards0/ExpectedFailure空，未宽免失败。直接普通build/import另保留在本计划workspace的v130-import日志，0编译警告/错误；新增仅PNG/import/ctex/md5四项，Source1852/C#892。

| 案例时间/类型 | strict | native exit/ms | 说明 |
|---|---|---:|---|
| 144921 focused | FAIL | 1/799 | 原始脱柄/缺左片有效RED |
| 150930 focused | PASS | 0/4570 | 第一版握持及原3爪 |
| 151228 focused | FAIL | 1/1040 | 实际手臂墨边裁切有效RED |
| 151350 focused | PASS | 0/4791 | 新15点观察器 |
| 151626 focused | PASS | 0/4549 | 增补父变换/关节检查 |
| 151702 pre | PASS | 0/168745 | 当时冻结 |
| 152025 post | FAIL | -1073741795/189371 | FATAL/Handle，registry49进入未返回 |
| 152405 models | PASS | 0/535 | 普通构建 |
| 152845 focused | buildFAIL | 无engine | 测试给只读NormalizedTime赋值，CS0200；不是行为RED |
| 153007 focused | FAIL | 1/690 | 反射找错Coordinator而非Timeline；不是I1行为RED |
| 153149 focused | FAIL | 1/994 | 同一无效探针；不算修复证据 |
| 153225 pre | PASS | 0/157445 | 该轮冻结 |
| 153532 post | FAIL | 1/184364 | 5 Handle及错误探针，registry52未返回 |
| 153906 models | PASS | 0/504 | 普通构建 |
| [154048 focused](../../../验证证据/october13-stable-claw/thruster-formal-claw-phases-20261026-154048-491-b4351af9f75049358a439676107e8c6d/engine.wait.json) | FAIL | 1/787 | 纠正探针/移除过早candidate后，真实I1 RED |
| [154212 focused](../../../验证证据/october13-stable-claw/thruster-formal-claw-phases-20261026-154212-949-716579de3be54c33bbc61b0a31ccf72f/engine.wait.json) | PASS | 0/4582 | I1 GREEN/原3爪及全部新检查 |
| [154334 pre](../../../验证证据/october13-stable-claw/prologue-pre-20261026-154334-852-c6ff12ce27724b01b97379d26f9d714d/engine.wait.json) | PASS | 0/156038 | 最终同源 |
| [154641 post](../../../验证证据/october13-stable-claw/prologue-post-20261026-154641-551-c74d261b8f3f430b8e7cb138e229b34e/engine.wait.json) | PASS | 0/196035 | 原75 enter/return，owner8 nodes1→1 |
| [155026 models](../../../验证证据/october13-stable-claw/models-20261026-155026-969-c75c175160c0464a8eb7207d3e1f873e/engine.wait.json) | PASS | 0/383 | 最终普通Debug |

最终四案同一源清单；post的PHASE_OK/TESTS_OK各1、CONTROL0、E/FATAL0，4个原有故障注入warning精确合规。早前FATAL未被后来的安静运行覆盖；不知道本轮加载/时序是否改变复现概率，不作资源归因。Main/Explore没有重跑，保留v128失败；4个既有纯harness失败未重跑或关闭。不宣称全目标/全部harness绿色。

过程失误：初次I1探针选错类型，根过早应用candidate；发现后只移除该未验证修正、纠正真实Timeline映射，先得到154048有效RED再恢复修正。不是广泛回退，也不把编译/探针错误当产品RED。一次review/同一个I1修复pass，无复审。

## 唯一审查、裁决及成本

[审查处置](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v130-final-review.md)：C0/I1/M1，根按用户实际影响维持分级。I1有效RED→GREEN，最后普通前后段/模型重跑；新M1延期。全部19项Declined根裁决如下：

| 项 | 根裁决 | 若错判的成本/保留边界 |
|---|---|---|
| 1 正常速度艺术/肘形 | 未验，不给美观结论 | 可能仍突兀，需实际观看 |
| 2 像素/披风遮挡 | Draw次序不替代GPU检查 | 手指/身体可能穿插 |
| 3 人眼可见 | NOTSEEN权威，不自动重开 | 不能冒认用户已看见 |
| 4 硬件/性能 | 无新增实机结论 | headless耗时不是FPS |
| 5 全音画 | 本轮未补全声音/环境 | 正式视听仍OPEN |
| 6 身份/身体/A2 | 只用weaponHands批准链 | 不接入缺批准总装 |
| 7 历史批准 | 复用已核链，本轮核原件SHA | 若旧链错仍需重核；不冒称新批准 |
| 8 全状态笛卡尔积 | 明示378+2非穷尽 | 未覆盖组合可能回归 |
| 9 极小腕距肘方向 | 有界但连续性未证 | 潜在奇点跳动待测 |
| 10 零/奇异父缩放 | 非正常场景配置，未认证 | 不能承诺任意外部变换 |
| 11 像素裁切 | 几何包围盒为保守准入 | 不等GPU边缘证明 |
| 12 全cue中断/超时 | 仅现有回归及clear/return | 尚需正常速度全cue核对 |
| 13 死亡恢复观感 | 生命周期与几何不等视觉认可 | 死亡动作仍可能不好看 |
| 14 资源FATAL | 保留实际失败/未知因果 | 不把握持修正当资源修复 |
| 15 完整回归 | 采用最后同源四案，不拼历史 | 历史FATAL仍未闭合 |
| 16 Main/Explore | 未重跑，沿用失败状态 | 不宣称完整通关 |
| 17 导出/异机 | 本地import不等export | 异机缺资源仍需验证 |
| 18 保管/文档 | 仅审计ENDED后写入 | 不把审查pending误记完成 |
| 19 原十一项/发布 | 全部未完成门保留，无Git写入 | 不交付未验正式版 |

其他Ruling：采用固定骨段而非肩平移/图片拉伸，是已批完整手臂的开发默认，不冒称新角色批准，成本是仍需审美迭代；沿现有每次姿态发布的有界进度，不另建时钟，成本是同帧多发布及连续渲染表现还需验证；保留现checkout/用户其他改动、不建worktree或Git写入，成本由源/索引保管控制；错探针/过早candidate显式撤回再做有效RED，成本是额外测试时间而非伪造成功。

Canonical没有改动：真实测试断言已在原注册消费者内失败即退出，最终源清单含新资源；没有为了新打印行扩大旧launcher。新四资源不在canonical选中实体快照范围，但原件/当前文件与全源清单认证保留；不冒称每案封存全部1852文件，也不事后回填。计划中扩大实体集合/独立新marker门未执行，不勾完该子项；成本是只凭单案选中实体不能重建全部工程，非原件丢失或新增选中缺失。

Final: minor (deferred): 新M1——连续motion observer还只看节点矩阵/武器起点，不能发现仅Draw肘腕的抖动；本轮新15点解决的是bounds，不是全连续抖动。正常速度动作门保持OPEN。

继承10类Minor：energy-unwind早失败flush、boundary swept措辞、finally stage4摘要、fresh LateFact来源、rotated-AABB负例、withdrawal旧return诊断、unknownActive真实scanner负例、cache-helper实体缺失、v127 shot上游physics/input/capsule独立破坏负例、v129内层Free双托管异常掩盖原错。加新M1共11类；旧14份helper实体缺口未追补，不静默宣布已修。

## 保管与下一步

[独立保管](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v130-custody.md) ENDED后才写本文：94清单对、9178已选实体、108封存binary全部匹配，新增缺失/漂移0。最终Source1852/892C#/Debug6一致；原音频/Claw/Main/Placement/Skitter/Traversal、HEAD/branch/index未变。早前崩溃子PID8760只有零线程/零句柄、无image/argv残留记录，不声称全局CIM0、未kill/attach。

唯一手动容量Check UTC08:00:18.1162306：1431609字节/16files，基线3508、增长1428101，Notify=false。无新过程媒体/付费/自动化/OS输入/删除/引擎安装/Git写入。

下一步：继续原十一项；补Draw关节的实际连续motion观察与正常速度证据，继续独立资源MRP及完整Main/Explore闭环。显示未成功的问题单独保留；后续应优先核实可直接查看的画面/短片链路，而不是反复短时弹窗后询问“看到没”。没有新展示授权，不自动重开。
