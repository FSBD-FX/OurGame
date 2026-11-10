# 默认验路回充与几何计时 · v245

状态：测试驱动增量；最终同源九门6PASS/3FAIL，默认Main仍失败。本轮没有生产物理、正式动作或美术改动，不代表完整序章完成。

## 事实、推理与风险

事实：旧默认验路没有选用已存在的配重安全松盾回充、升降幽灵防守。单标记对照清完配重但在升降损血；双标记及提升后的默认案均保持 HP2 通过这两段，随后摆锤仍在原 899 帧留一只 HP1 Skitter。默认案不是超时，也没有双标记案的 AudioServer 清理失败。

推理：本轮可确认两项既有策略的默认测试路由已接入实际输入消费，不能据此认定整个 Main、Explore 或角色手感已通过。几何案只留下入口和候选包两次时钟，随后在 activity 前发生 `gchandle.is_released()` FATAL；没有证据把旧 55 秒问题归为 I/O，也没有性能修复结论。

风险：降低防御储备会把测试驾驶问题转为生存风险。原完整攻击＋八拍保护窗、24 储备、来源/fresh/支撑/静止/发射否决链、HP、三弹、单跳及 899/55s/180s/300s 门均保留；不加等待池、不回满能量、不筛掉红案。原十一项保持 ACTIVE，R-I/R-C/R-P 保持 OPEN。

## 实施范围

按[计划](G:/knight/Knight/docs/superpowers/plans/2026-11-04-counterweight-recharge-and-geometry-timing.md)、[回充设计](G:/knight/Knight/docs/superpowers/specs/2026-11-04-counterweight-recharge-routing-design.md)与[几何计时设计](G:/knight/Knight/docs/superpowers/specs/2026-11-04-resident-geometry-stage-timing-design.md)，代码增量仅以下三个测试文件：

| 文件 | 本轮变化 |
| --- | --- |
| [PrologueCanyonGeneratedMainRouteTests.cs](G:/knight/Knight/tests/integration/PrologueCanyonGeneratedMainRouteTests.cs) | 提取两个 selector，分别为 `earlyPositioning && (normalPlan || explicitProbe)`、`keyboard && (normalPlan || explicitProbe)`；原三处回充消费及原 Ride 安全策略复用，向 Ride 显式传入 normalPlan。配置回执明确 `safety_authorization=false`。 |
| [RangedWeaponModelTests.cs](G:/knight/Knight/tests/unit/RangedWeaponModelTests.cs) | 同一组 8 个字面量真值组合分别断言两个 selector，保留显式实验入口并拒绝作用域外选择。先保留旧选择逻辑得到实际 RED，再启用 normal 得到 GREEN。 |
| [CanyonResidentPlacementTests.cs](G:/knight/Knight/tests/integration/CanyonResidentPlacementTests.cs) | 增加 11 个累计单调时钟回执及私有 leafSource 只读 `RecordCount`；原步骤、循环、JSONL、await、成功断言和异常传播不变。计时不参与放行。 |

未改玩家能量/护盾/热量、攻击/敌人/地图、Motor/动力数值、正式图片、引擎或真实存档。root 唯一执行无窗口原生验证，运行期间源码/harness 冻结；无 GUI、系统键鼠、付费生成、新媒体、额外崩溃采集、自动化、发布或删除。

## 已终态的路由与计时证据

下列各案 `TimedOut=false`、`guardFailures=[]`，provenance 均记录 `sourceRows=1888`。早期标记/RED案不混入最终同版九门；最终当前源输入/二进制绑定另见下文。表内链接直达原始证据，失败日志全部保留。

| 案例 | 配置与实测 | 终态 |
| --- | --- | --- |
| [单标记 Main · 192749](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261104-192749-970-c18cc004526b4776a80680a743169e8f/provenance.json) | 仅本次显式加入 `KNIGHT_POSTHOP_RECHARGE_PROBE=1`；配重 8 次真实攻击，HP2→2。升降 physics7151 被 ghost-diver 攻击2命中，HP2→1，恢复0/0。 | exit1 / 156223ms，严格 FAIL；不是默认验收。 |
| [模型 RED · 194136](G:/knight/验证证据/october13-stable-claw/ranged-models-20261104-194136-862-9c2bc569891e45c3b0e30860c1f6b97e/engine.stderr.log) | `NORMAL_SAFETY_ROUTING_RED` 同时列出 `recharge/True/True/False`、`lift/True/True/False`；真实缺失 normal 路由，不是编译失败。 | exit1 / 537ms。 |
| [双标记 Main · 194218](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261104-194218-672-7ada1691067044189a549a5f0bde8fc0/provenance.json) | 另显式加入 `KNIGHT_LIFT_GHOST_GUARD_PROBE=1`；配重8击/HP2→2；Down+L 早采样7145，同一 hitbox4877388354107/攻击2于7150零伤接触；乘台7199、落 Grip2 7238均HP2/恢复0。 | exit1 / 178795ms。摆锤原899留SkitterHP1/playerHP1；另有原60帧/2000ms Audio清理失败，实耗3044ms，两因均保留。 |
| [几何计时 · 194557](G:/knight/验证证据/october13-stable-claw/resident-geometry-20261104-194557-759-ca98d18deaba4a84821c063cc5e34e4e/engine.stdout.log) | 原55秒门内仅记录 `run_enter=0.178ms`、`candidate_package_ready=822.690ms`；activity前出现[句柄释放 FATAL](G:/knight/验证证据/october13-stable-claw/resident-geometry-20261104-194557-759-ca98d18deaba4a84821c063cc5e34e4e/engine.stderr.log)。 | exit−1073741795 / 8577ms，严格 FAIL；不能称计时解决性能或所有权问题。 |
| [模型 GREEN · 194946](G:/knight/验证证据/october13-stable-claw/ranged-models-20261104-194946-293-ae7eb4ab01b848a095e57183c7acb31e/engine.stdout.log) | `cases=8 policies=2 default_selected=true explicit_preserved=true outside_scope_rejected=true`；原完整来源及发射/fault否决覆盖通过。 | exit0 / 527ms；stderr空。 |
| [默认 Main · 195028](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261104-195028-098-ddb30d40ba5743c6aa431a6e26a482e5/provenance.json) | 两项实际回执均为 `normal_plan=True explicit_probe=False selected=True`。配重及升降结果见下文。 | exit1 / 199531ms，严格 FAIL；原摆锤899失败，无Audio清理失败。 |

标记配置另存于各自 `marked-experiment-settings.json`，仅为 annotation。它们不证明所有历史环境都已洁净；默认案实际 `explicit_probe=False` 只证明这两个实验旗标没有代替 normal 路由。

### 默认 Main 的实际消费

[原始日志](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261104-195028-098-ddb30d40ba5743c6aa431a6e26a482e5/engine.stdout.log)记录44次 `posthop_recharge_defer` 松L指令，并有后续真实 `held=False/active=False` 样本。原生能量记录由15.1999回升至31.066603，中间必要举盾仍会消耗能量；没有直接改写资源。配重 `attacks=8 started=8 connected=8 thread_casts=3 health=2->2 damage_events=0`。

升降按原Down+L在7144早采样，同一hitbox4876767597619/攻击2于7149零伤接触；7198乘台完成、7237落Grip2均HP2，恢复0/0。后继摆锤原899帧仍余SkitterHP1、playerHP1，[原严格错误](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261104-195028-098-ddb30d40ba5743c6aa431a6e26a482e5/engine.stderr.log)保留，不能续签完整Main。

## 审查、开发判断与延期

[本轮账本](G:/knight/Knight/.superpowers/sdd/2026-11-04-counterweight-recharge-and-geometry-timing/progress.md)记录一次独立只读三文件审查，root复评 C0/I0/M0；核对包含实际安全消费链，不只检查布尔选择。没有本轮C/I修补或二审，也不据此代签整个dirty历史。

- 固定低额末击预算被否决：58个候选不足保守防御储备，下一轮实耗17.2；不把一次窗口费用当成全三弹费用，不将24降为12。代价是完整路线仍可能失败。
- 本轮只提升既有安全策略的测试驾驶路由；单标记揭出后继升降漏选后，先双标记实证再接normal，不新增防守算法。代价是仍可能暴露后续路线问题。
- 沿持续制作约定继续唯一dirty工程，不新建worktree、不提交、不重复询问已确定的项目/制作选择。代价是不能宣称干净发行或已获新风格批准。
- 审查未判断geometry根因/性能、完整Main/Explore、标记环境全部历史、dirty全历史/最终binary绑定及视觉硬件。保留崩溃与红门；最终宽门启动前由root断言无 `KNIGHT_*` 覆盖，当前完整绑定另核。代价是不能把局部通过当全路线、发行或观感验收。

继承v243两项Minor继续延期：三秒退出控制不证明接管当帧；同一次攻击中翻面/Timeline向上组合未独立补测。它们不是本轮新发现，也不被v244腕轨迹或本轮路由覆盖自动关闭。

## 最终同版九门与绑定

root会话38169已实际终态exit0；这只是逐案执行脚本完成，不是全部测试通过。最终资格集合为上表GREEN模型、无旗标默认Main，加下表七门，共九门6PASS/3FAIL；九案build均exit0，engine均非超时，guardFailures均空，失败仍按原始语义保留。运行期间源码/harness冻结，没有因为红门重跑筛绿或扩预算。

| 门 | 当前已知结果 |
| --- | --- |
| [prologue-pre · 200647](G:/knight/验证证据/october13-stable-claw/prologue-pre-20261104-200647-716-9c1fe247c52943ecbaae53773186d5b3/provenance.json) | exit1 / 88685ms，非超时、guardFailures空；`DescentClawGuidanceRuntimeTests.cs:756` 首个精确失败为 `same default-health player naturally lands without recovery or objective injection`。Audio drain与phase退出成功；下述只读核对界定失败，不声称已修。 |
| [prologue-post · 200858](G:/knight/验证证据/october13-stable-claw/prologue-post-20261104-200858-628-cfbf66e563b64ffba87a2cbca80689e8/provenance.json) | exit0 / 194534ms，原300秒门通过；仅原有4条有意负控warning（throwing observer、非Node2D、两次Completed→Recovering拒绝），不是全游戏零warning。 |
| [resident-geometry · 201251](G:/knight/验证证据/october13-stable-claw/resident-geometry-20261104-201251-527-dc2134b70e77472191439c5391d4e585/provenance.json) | exit−1073741795 / 8547ms，非超时、严格FAIL；仍为匿名gchandle终结器FATAL。只观测run0.163/package832.035ms，未到activity，不能证明此前55秒性能问题解决。 |
| [存档兼容 · 201338](G:/knight/验证证据/october13-stable-claw/resident-save-compatibility-20261104-201338-580-8547cab59f0948ff9aaba281b1a26d09/provenance.json) | exit0 / 13918ms，严格通过。 |
| [键盘 · 201436](G:/knight/验证证据/october13-stable-claw/keyboard-20261104-201436-764-07fcb4c82bc34d7b81d9aa63c7487c8c/provenance.json) | engine exit0 / 20843ms，严格通过；独立标题title-entry exit0 / 7927ms、stderr空。仍为内部真实按键，不是OS键鼠或真人试玩。 |
| [原models/claw子集 · 201547](G:/knight/验证证据/october13-stable-claw/models-20261104-201547-580-3f1fb524769f43efbcb279a57c3e3ddd/provenance.json) | exit0 / 530ms，严格通过；它是原爪/抵达模型子集，不冒称全模型。 |
| [Explore · 201627](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261104-201627-191-ad6099479a4c46e196822cac647a918d/provenance.json) | exit0 / 338088ms，stderr空、严格通过；普通标题→同Player→Boss→实体出口、恢复0/0；23访问/11挑战，11遭遇激活但仅10击败，不是全敌群/全操作或真人验收。 |

前段只读核对结果：末态谓词mask8仅为HP不等于原要求5，实际HP4；同一玩家、request0/recovery0、objective=ReachBasin、realLowerCatch6帧、floor=True、velocity=0、claw=Idle均成立。因此不能把整句断言概括为落地、恢复或落伤失败，原HP门继续保留。

[stdout236损伤记录](G:/knight/验证证据/october13-stable-claw/prologue-pre-20261104-200647-716-9c1fe247c52943ecbaae53773186d5b3/engine.stdout.log:236)在physics2873/input625、`release-grounded-idle`阶段接受HP5→4；来源路径为`CanyonDescent/Gameplay/Actors/@CharacterBody2D@527/AttackHitbox`，类型Hitbox/id5033198358966、攻击2、`MotorSource=false`。当时盾未启、过热、energy=.5913、LastFallDamage=0。日志未给出来源敌人的稳定actor ID，不能按通用节点名命名敌种，也不把这条记录扩成生产根因结论；stdout237为原终态。

探索[结果JSON](G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261104-201627-191-ad6099479a4c46e196822cac647a918d/storage/task10-route-result.json)明确automated、syntheticFixture=false，humanValidated/operationCoverageComplete/fullGoalAccepted均false。Player47563408900、Boss28360138821453、真实J攻击99击败，Died/arena各一次；出口fresh D后18140完成、18201持键60拍仍停在(7341.191,1079.9327)，40×72实体受(6080,1116,1600,40)地板支撑，completion count/signal各1。完成后Player物理关闭，保留velocity260是冻结字段，不代表仍在移动；完成及持键后隔离保存SHA同为D68DEC7993D42D14D26D4F98E88C00B907350568CA6F6B52E552E87F08A56779。缺失击败仍是counterweight，不把激活当清场。

root在所有native终态后读取并核对九案各5组source/helper/user/continuation/binary前后保护文本，共45组SHA全部相同；这证明各案保护集合内未漂移，不是给整个历史发行打包签名。九案source清单SHA统一为`D706AB557823120FEB8E72B5E6D531FFDC9F8CF55526703E696B60829E0E6F9D`，六binary清单SHA统一为`8CC8D2AEE727AAE64C73DC923F5F3F0A2587A0CF9C2ECAB25D34EF5D013E219E`。另逐文件重读当前1888输入与`.godot/mono/temp/bin/Debug`六文件，差异均0；这些是清单SHA，不是可发布ZIP。已核对各案native身份没有仍运行的本次case进程，不操作其他Godot实例。

本轮未增加过程媒体；普通一次`-Check`结果7,427,750bytes/86文件、Notify=false，未达到新增20GiB。没有建立自动化。scoped `git diff --check`未报空白错误，仅既有LF→CRLF提示；未提交、改行尾或发布。

用户要求以[角色动作与物理返工记录](G:/knight/Knight/docs/production/2026-11-03-character-motion-rework-requirements-v001.md)及U-20261104-01为准：惯性、角色动作特色、物理对应/操控手感仍须返工与正常速度联合样段验收。本轮没有新显示、正式素材、硬件/真人或用户体验认可，R-I/R-C/R-P与原十一项范围均不缩减。

## 下一有证据的入口（尚未实施）

双标记摆锤案首个HP2→1实际接受伤害在physics8081/input8051，来源为居民`pendulum-gallery.03`的bone-diver攻击2，而非当前被选中的Skitter。前一帧通用foreign guard已成立，但Skitter专用分支在外国防御探针未开启时以false覆盖它。下一优先以这个确定来源建立局部RED，核对方向/真实早采样后保留有效防御；不同时开启全部站位探针或调敌人/玩家数值。

Skitter在距离≤34时可真实取消锁定攻击，回到Recover/PhaseNone/AttackId0；这种恢复按原有效攻击ID规则不准反击，不能当成“J没响应”。691确有一击但没有独立owned-Skitter命中行，779最后J目标又是bone-diver，不能据总connected数或最后攻击时间补造具体命中结论。预检下降则沿上文实际Hitbox父actor/攻击2/释放后接触时序核对。资源FATAL继续独立保留，不用测试驾驶修复解释它，也不把这些后台修复代替玩家可感知的动作/物理返工。
