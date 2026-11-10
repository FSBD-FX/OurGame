# v132 · 近处敌人优先，完整路线适配仍未完成

后续更正（v133）：下文历史记录及初步归因中的“StaticBody支撑排除moving lift”不成立，原helper已经接受继承StaticBody的Animatable/CanyonLift。实际胶囊在甲板右侧超边13.5664像素；拒绝完整支撑正确。没有放宽生产准入，详见[v133真实characterization与更正](2026-10-26-safe-claw-and-carriage-handoff-v133.md)。保留原记录，不将后续结论倒签为v132当时已知。

2026-10-26，KNIGHT，`G:/knight/Knight`。原十一项仍ACTIVE，用户显示结论仍NOTSEEN。当前主要推进第1项移动/力学、第3项战斗的机械爪联调，以及第9项技术验收；不是已经依次完成到第九项。

## 事实、推理、风险

事实：用户明确批准[近处敌人优先规格](../superpowers/specs/2026-10-26-near-enemy-keyboard-priority-design.md)。真实保存Moth(40,-200)与远处金属面(0,-300)的原生负例，旧规则选中轴线地形(0,-290)，实际RED；修改后选择近处可交互Moth，选点没有触发牵引。真实保存Player/HUD/K后，同一个敌人被锁定，Aim后转向仍保持目标，adapter应用一次，实际拉向Player，Latched→Released且无残留移动绳。

推理：局部规则已经实现，但不等于关卡整体更好玩或正常路线通过。新全局排序改变上游交互时序，原驾驶的一些站位/准备前提不再成立，必须按实际键盘和地形重新联调，不能强制目标或减怪凑绿。

风险：本轮完整Main/Explore均FAIL；pre虽engine0仍有7个Handle错误，post虽75用例和owner8完成仍有4个Handle错误，均严格FAIL。没有放行资源错误，没有重跑洗绿，没有新增窗口或系统键鼠输入。正常速度美观、准星像素、目标硬件、真人和正式交付仍未验收。

## 实现范围与有效验证

相对v131最终models实体基线，仅4 C#改变：`MechanicalClawTargetResolver`、`KeyboardClawTargetTests`、`KeyboardClawRuntimeTests`、`RunStableClawPullTests`；Source1852/C#892，源文件无新增/删除，资产、地图、敌人AI/HP/伤害、输入、物理和能量参数均未改变。

保留原完整角度→距离→稳定path baseline。复用原资格/材质/距离/22.5°半锥/真实LOS，收集全部通过门限的敌人候选；完成33射线后，仅在baseline是移动目标时，从严格更近且可分派的敌人中按原规则选择。提升池按path固定遍历，不用非传递的敌人/地形两两比较器。明确Mechanism/Enemy Kind不被误当普通地形；普通移动锚点与墙顶同类是已向用户说明的开发默认。

新增提升资格检查receiver支持、真实adapter生命周期/路径/速度，绝不试调用接收者。鼠标旧路径、已发射锁定、真实遮挡/射程/冰/禁抓面不改，命中时仍由原交互事务决定接受或拒绝。

- 171405原生有效RED：近Moth可用、距离203.96078、应用0，但旧零角terrain胜。
- 171628初次GREEN；172358扩充边界GREEN；173316真实HUD/K/锁定/应用GREEN。
- 29次边界检查覆盖距离、等距、清晰LOS的远敌、多候选过滤、稳定path、材质、锥外/超距、遮挡、死亡、撤组、receiver/adapter及明确机关等；其中8次是稳定路径重复验证，不是29种独立场景。
- canonical原keyboard只跑Runtime，本轮显式接上TargetTests，再跑原Runtime。独立收新target/priority/boundary/cast标记；实际原Runtime标记存在，但gate还未独立要求它，见延期M2。
- 最终helper补齐11条本轮直接依赖的选中实体路径；完整库存哈希与选中实体副本分开记录，不称全部传递依赖的离线重建包。

## 本轮全部12原生案

证据根：`G:/knight/验证证据/october13-stable-claw`。7 strict PASS / 5 FAIL；12次构建均0警告/错误、stderr空、无特殊Defines；ExpectedFailure空、guards0、无timeout。失败不被同轮安静案覆盖。

| 案例时间/phase | strict | engine exit / ms | 结论 |
| --- | --- | ---: | --- |
| 171405 keyboard | FAIL | 1 / 1332 | 有效旧规则RED |
| 171628 keyboard | PASS | 0 / 10476 | 初次近敌GREEN |
| 172358 keyboard | PASS | 0 / 11535 | 边界29检查 |
| 173316 keyboard | PASS | 0 / 11895 | 加真实K链路 |
| 173533 keyboard | PASS | 0 / 11885 | 冻结批次keyboard |
| [173613 Main](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-173613-341-710764ec31e0412fb4eec9186c00b019/engine.wait.json) | FAIL | 1 / 93909 | 齿轮Bell站位准备未成立 |
| [173820 Explore](../../../验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261026-173820-392-2dbbdf03041a427a830b9e0d42050541/engine.wait.json) | FAIL | 1 / 57709 | 原窗口未发出K |
| 173946 pre | FAIL | 0 / 151660 | 行为返回，但7 Handle错误 |
| 174247 post | FAIL | 0 / 193160 | 75 enter/return、owner8，仍4 Handle错误 |
| 174630 models | PASS | 0 / 391 | 原模型 |
| [174806 final keyboard](../../../验证证据/october13-stable-claw/keyboard-20261026-174806-152-2872141357e94a8d8e55912c62eaf3d9/engine.wait.json) | PASS | 0 / 11897 | 最终实体依赖/准入验证 |
| 174844 final models | PASS | 0 / 401 | 最终普通模型 |

173316起9案的18份源清单逐字节一致。最后补的是helper选中实体清单，不是生产代码；不拼称全部最终helper版本均已通过pre/post。FATAL本轮0，历史FATAL/ShapeRID仍OPEN；post四条原精确预期warning/scope匹配，不包含Handle错误豁免。

## 完整路线的实际分歧

Main最早保留的语义分歧在深渊：新案stdout63–65的wave Moth已ThreadStunned，本场K1；v131 Main162802仍Telegraph→Dive→Recover、本场K0。两案深渊落地均4.633秒、P(2331.4773,3523.9934)、HP4、完整支撑、跳跃/喷射增量0；全局physics相差1。该处没有首个preview/K/lock-ID/LOS记录，不能补造首次resolver事件或精确归因。

新Main的gear-pistons最后在`BasinCounterweightLift`上，gap60.832275、速度0，但旧完整StaticBody2D支撑门失败，900原预算内J0/K0；旧Main在`ShortcutBasinWalkway`通过准备并实际K。地图、AI、HP和driver字节没有变化，但上游时相已变，不能直接说某一次未记录选点导致升降台站位，更不能称“齿轮K抓偏了”。

新Explore原189/210窗口达到E95.33324、HP2，issued=False、samples0、K edges2→2，无本窗口aim/preview记录；因此不能认定resolver拒绝或锁错敌人。v128的131438案有原成功preview→K→adapter，但属于不同冻结；v131没有Explore重跑，不存在可冒充的同版对照。

下一步：保持原输入/危险/HP/时限，先验证通向固定地面的真实站位与齿轮K各准备门，再继续全关路线；资源句柄因果另行处理。不是关闭机械爪新规则，也不是放宽地图碰撞或减少敌人。

## 审查与延期

唯一fresh review C0/I0/M2；根重分级后维持，没有C/I修复或复审。详见[审查与全部14项裁决](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v132-final-review.md)。

- M1延期：新enemy HUD用例未直接断言reticle实际屏幕坐标，实际controller ID/可见状态不能代替像素位置证据。现生产HUD使用preview anchor，没有已证实坐标错误。
- M2延期：canonical未独立要求原`KEYBOARD_CLAW_RUNTIME_OK`恰一次；本轮runner和日志证明原Runtime执行，但防未来绕过保护仍不足。
- 继承12类Minor加本轮2类，共14类OPEN，不以审查C0/I0宣称全质量通过。未修Minor符合单轮边界，不静默丢弃。

## 保管、目标与权限边界

[独立审计ENDED](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v132-custody.md)：60/60完整manifest对、5804选中实体SHA、72 sealed binaries匹配；当前1852/892、普通Debug6匹配最终清单。原HEAD/branch/index、Layer/Visual/Main/Controller/Placement/音频/Skitter/Traversal pin均不变。canonical本轮5个计划内版本，逐案自身无漂移；旧14份cache-helper实体缺口仍OPEN。

根的两个批次23185与5940、所有注册原生身份及审计命令均已结束。PID8760仍0线程/0句柄空镜像残留，不叫可运行Godot也不声称全局CIM0；被svchost/WmiPrvSE复用的PID不误判成旧engine。

本goal turn媒体Check已于UTC09:03:26执行一次，本轮没有新媒体，未重复运行；Current1431609/Baseline3508/Growth1428101/Notifyfalse。无自动化、付费生成、新窗口、系统输入、Git写入、删除、引擎安装或真实存档语义读取。原十一项、76普通敌人+Boss/NPC、正式视听/动作/UI/相机、正常双路线、硬件真人和发行继续ACTIVE/OPEN；不是交付完成。
