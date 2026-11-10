# v136 · 机械爪输入确认与下层战斗返程

KNIGHT / Godot，2026-10-27，工程 `G:/knight/Knight`。原十一项完整目标继续 ACTIVE。本轮只有自动试玩测试消费者变化，没有新增生产玩法、美术、地图或数值。完整主线、探索线仍失败，不是精美成品交付。

## 事实、推理、风险

事实：用户已明确选择[保持敌人之间的角度优先](../玩家修改意见与世界设定.md)。生产选择器未改；v132已批准的近处可交互敌人优先于后方地形仍保留，不能扩大成敌人之间的最近距离优先。

事实：此前LowerBell受击者是保存的 `scrap-wanderer / RustShellSquire`。有效被动时间线证明它的预警已进入原扫描，Bell的Active仍获更高紧迫度；不是已有证据支持的“扫描漏看预警”。后续真实K失败则由自动试玩过早释放方向造成：发键时的计划不等于游戏实际采样时的方向。

推理：修正自动驾驶输入确认，不能据此改生产选敌排序、敌人伤害或玩家能量。两次合法D短移使原八方向出现可用目标；之后保持同一K与方向组合直到生产Aim确认该目标，再松键。没有强制目标、重发K、写位置/速度或增加原900战斗循环。

风险：本轮真实LowerBell控制、战斗和完整返程已局部通过，但完整探索随后在Steam被冲锋居民击杀；Main仍在配重混战死亡并恢复。energy字段的小数不能直接解读为能量耗尽，受击后仍有正常J出招；未激活护盾与专用驾驶接管的关系须单独验证。正常窗口、真人操作、正式图声及目标硬件仍未验收，最新显示结论仍NOTSEEN。

## 从可见性到实际输入的证据链

1. 初版000401的512尾环丢失了首次受击前历史，不能用它诊断原因。保留原失败后，改为首次实际伤害后再保留2个physics，并显式计遗漏；001000才有有效222行、fault0的前因。Rust发布在Move之前，Rust FrameChanged在Move之后；伤害回调4693与另一接受伤害观察4692不是同一个原子边界。
2. 003238的首个全源时钟折叠显示13项余量均为正，Safe=.4166666大于lead=.13950916；撤回“安全时钟阻止K”假说。真正的计划目标是角度更正的远Bell，而不是近Rust。
3. 原位置一次查询八方向，均不选近Rust。最多两次真实D接近，始终要求完整支撑、原生12px净空、所有危险源安全余量和HP保持，才使近Rust可合法选中。此处没有修改全局规则。
4. 003948揭示 `AimAt` 只是规划元数据，不会替直接K消费者按下WASD。补实际方向后，004405仍复合断言失败。唯一审查指出缺少真正接受样本，不能据Bell Exposed推断命中，也不能臆测Aim/Windup重新选敌；生产控制器实际会缓存Cast时目标。
5. 按唯一I1修复pass先补被动pre-Claw真实采样、完整publication及断言分项。005705真实RED：4663物理K已按下但raw_K=false；4664才raw_K=true，此时方向已松成Right，生产Aim当场接受Bell，后续正确保持这个错误的初始锁定。应用近Rust次数0，K边沿1，HP2保持。实际原因不是Aim后重新选择。
6. 仅此后修正消费者：保持原K+WASD组合，直到输入序号前进且生产Aim发布预定目标的ACK，再释放。010147已完成同cast/一次application/一次K/实际Stunned/Latched/Released及整个下层返程，但成功K样本被普通尾环淘汰。其实际效果不能替代保留完整输入证据。
7. 同一I1 pass仅补64条独立标量控制证明，与512尾环按serial合并、明确遗漏量；stage3后不再收集无关控制publication。最终011306保留实际raw_K、目标ACK与效果，控制证明21/64、遗漏0，普通尾环512/512、总1209、淘汰676、fault0。未改变动作策略或再开第二次审查。

被动记录仍只装饰原一次扫描，额外capsule距离仅诊断，不传回原决策的null guardCapsule。后来主动K计划会进行正常选择/LOS查询，不能把整个新增消费者混称无查询的被动取证。有限tail、一次演员绑定不代表全时段或未来生成演员覆盖。

## 最终实际局部结果与剩余失败

[Explore011306原始输出](../../../验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261027-011306-176-a02a5ac1f479454883aea213ff72b28d/engine.stdout.log)：

- 实际接受：physics4664、sample4636，命令sample4634，方向DownRight，Aim目标773245112203等于预定目标。
- 实际效果：cast6、application1、K边沿1、真实Stunned与Latched/Released均成立，HP2→2；原900循环不变。
- 原LowerBell战斗与完整返程：defeats1、HP2保持，回到(3019.924,3799.9985)，原路线普通跳3次。不是只过K专项；也不等于全探索通过。
- 后续Steam居民 `canyon.v2.resident.steam-valves.03 / scrap-charger` 的RushHitbox在7420使HP2→1、7622使HP1→0。日志energy字段是EnergyRatio（当前能量÷最大能量），.422/.663实际约42.2/100、66.3/100；两次均未过热、护盾未激活，不能称能量耗尽。伤后真实J的.422→.302正好扣12点。仍按原恢复/位置重置监视器严格FAIL，没有治疗、放宽或重试筛绿。

[Main011039原始失败](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261027-011039-613-db30d19ac40c4b869a0863e0a11f2774/engine.stderr.log)：仍是配重混战后的真实死亡恢复，位置从(4618.6143,3423.9336)重置到(1680,3960)，step2987.11。未将恢复瞬移当合法通关。

以上均为headless实际Godot输入事件与原生组件结果；不是物理OS按键、真人试玩、正常显示或性能认证。

## 全部尝试与最终同版回归

18次尝试＝16次native（5 strict PASS / 11 FAIL）＋2次CS0123 buildFAIL。16次成功构建均原始exit0、0警告/0错误、stderr空；两次构建失败无engine，不是有效行为RED。全部native无timeout、guards0、ExpectedFailure空、FATAL0；11个native FAIL各自stdout/stderr/external ERROR为0/1/1，不删除。

| 案例时分秒 | strict | engine exit / ms |
| --- | --- | ---: |
| Explore000401，初始尾环不足 | FAIL | 1 / 100748 |
| Explore001000，有效原候选时间线 | FAIL | 1 / 102938 |
| keyboard001822，窗口false-stub真实RED | FAIL | 1 / 520 |
| keyboard002209 | buildFAIL CS0123 | 无engine |
| Explore002244 | buildFAIL CS0123 | 无engine |
| keyboard002408，12控制与旧合同 | PASS | 0 / 12288 |
| Explore002455 | FAIL | 1 / 89791 |
| Explore003238 | FAIL | 1 / 98117 |
| Explore003948 | FAIL | 1 / 88847 |
| Explore004405，唯一审查输入 | FAIL | 1 / 91447 |
| Explore005705，实际错误接受RED | FAIL | 1 / 88372 |
| Explore010147，下层通过、后续Steam失败 | FAIL | 1 / 151875 |
| final keyboard010955 | PASS | 0 / 11892 |
| final Main011039 | FAIL | 1 / 116919 |
| final Explore011306，下层完整通过、Steam失败 | FAIL | 1 / 145055 |
| final pre011605 | PASS | 0 / 164330 |
| final post011920 | PASS | 0 / 206444 |
| final models012320 | PASS | 0 / 545 |

最终六案使用同版源码/helper。post独立核验原75 enter/75 return、0–74每项各一次、owner8节点1→1、PHASE_OK/TESTS_OK各1、特殊CONTROL0；四条原warning与三scope收据多重集合精确一致。stderr/external的四warning是镜像，不是八条独立warning。当前前后段通过不关闭旧Handle/FATAL/ShapeRID根因。

## 下一轮只读侦查（不是已实施的修复）

`/root/v135_lowerbell_recon`只读本次最终Explore和当前消费者，没有改文件/构建/native。其事实边界：

- accepted_damage的energy来自[MainRoute:20340](../../tests/integration/PrologueCanyonGeneratedMainRouteTests.cs:20340)的 `energy.EnergyRatio`；[生产控制器:18](../../scripts/player/PlayerEnergyController.cs:18)是当前值除最大值。[默认参数:10](../../scripts/config/PlayerEnergyTuning.cs:10)为Max100/AttackCost12，不能把比例误读成剩余.4点。
- raw1260下层返程、1310 Steam入场、1322落沟均HP2。1407的 `counter_energy_reserved` 是[测试保留量策略:21](../../tests/presentation/SentryCounterEnergyBudget.cs:21)拒绝J，不是生产缺能量；默认60Hz攻击前要求>58点，双J准备>70点。
- 7394专用Sentry驱动释放盾，7395输入采样L=false，7408 K使Sentry Stunned，7420才被charger Attack2命中。共享危险决策7391、年龄29；此前charger候选/预警未逐帧记录，不能断言7394就该格挡，更不能称scanner lag。
- 更强的第二次证据：7621共享决策已选charger Attack4 Active，genericGuardPlan=true；7622仍L=false并受击。[Main末端:16699](../../tests/integration/PrologueCanyonGeneratedMainRouteTests.cs:16699)只用sentryGuardRequired生成专用盾命令；另一路[15094](../../tests/integration/PrologueCanyonGeneratedMainRouteTests.cs:15094)接管后continue，绕过共享扫描。[专用驱动:274](../../tests/presentation/SentryCounterEnergyBudget.cs:274)只看本Sentry/bolt后Neutral释放L。

下一最小表征拟在现有 `RunSteamRecoveryKeyboard` 做真实Sentry＋scrap-charger双敌命令交接，记录Stage/handled、共享赢家与年龄、最终L命令写者/方向、下一实际采样及盾可用性。先区分“专用命令撤销外国威胁格挡”和“正确L已采样但生产关闭”；不拿单Sentry叶测代替混场、不改生产数值、不再跑下层。这个表征及修复尚未执行，不能记作下一项完成。

## 审查、保管及原十一项

[唯一审查与全部20项根裁决](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v136-final-review.md)在004405时为C0/I1/新生命周期Minor。I1按实际RED→输入ACK局部GREEN及最终保留证据闭合；这不是审查员第二次批准或整个路线通过。新Minor不顺手修复，旧18项加1保守计19项OPEN，尚未去重，不默默关闭。

[最终六案独立实体审计](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v136-custody.md)UTC17:38:56.6757747明确ENDED后才写本报告。30清单对、3056实体、36binary匹配，当前1852源/892C#与Debug6一致。相对实际v135仅MainRoute测试392+/4−、KeyboardClawTarget测试23+/0−；无源增删，canonical未改，生产/scenes/assets/AI/数值/save/HEAD/branch/index保持。其余12次仅核raw元数据，旧14份helper离线缺口仍OPEN，不冒称全部历史实体审计。

完整十一项仍ACTIVE，不勾整项：正常Main/Explore、历史资源原因、旧地图探索跨revision保守迁移、正式美术/动作/VFX/UI/声音/CG、正常速度/硬件/真人、授权版本交付均未完成。下一步核对Steam专用Sentry驾驶与其他危险源的防御命令交接；不重跑已通过LowerBell来制造进度，不据小数能量字段单独改生产平衡。现有素材仍以[实际盘点](../art/2026-10-26-existing-art-reuse-v134.md)为准，本轮没有正式视觉升级。

无新媒体。正常汇报前手动Check UTC17:40:09.2123917：current1431609、baseline3508、growth1428101字节、16文件、Notifyfalse；未重置基线、未建立自动化。没有付费生成、抢窗口/OS输入、Git写入、真实存档语义读取、删除或修改另一UE项目。
