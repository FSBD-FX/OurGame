# v249 · 首周期反击证据与资源阶段控制

## 事实、推理、风险

事实：没有生产动作、力学、地图或正式图稿改动。补留首32个已完成的原pending字符串、原128尾部与所有决策保持；新增独立资源stage及实际故障清理回归。旧Main/资源失败、原十一项仍未完成。

推理：首周期也未出现“上一击已结束且满足反击条件却漏发”。不能据最终899失败就降低原能量、攻击阶段、24循环或通关时限。单leaf阶段PASS只显示这次普通路径能完成，不能识别匿名Finalizer或取代原完整geometry。

风险：诊断读取/输出仍可能影响调度；没有正常速度美术、视听/操控和用户验收，不用后台检查代替。资源原生abort/OOM无法由managed cleanup保证恢复。

## 原Main前缀

Case `G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261105-015642-114-fbc1ea7c97a2434cafc43efd5e8d5e6f`：engine160452ms/exit1/nonTimeout，原899严格FAIL；本case发生于cleanup I1修补之前，不冒称最终当前二进制。

Prefix stdout3942–3973，ordinal1–32＝loop720–751，只输出一次；END3974。tail仍total146/overflow18/faults0。

| 原循环 | 实际证据 | 结论 |
| --- | --- | --- |
| 720–730 | Recover6、余.50→.33333334，能量34.86666，strike/canCounter真，但仍attacking | 首失败0x8/next741，不是已证可立即再J |
| 731–733 | 原J仍在攻击，余窗≤.31666666 | 已不满足原.33333334窗口 |
| 734–737 | 原J才结束，余.26666662→.21666658 | 无已证“结束且canCounter真”的机会 |
| 741–751 | Recovery尾段→Idle0→Telegraph7 | 0x1000后0x2，后续未求值仍未知 |

Cycle7旧案例781/P8500距离34.432617、Telegraph7；782/P8501距离31.988281变Recover/None/0。模型锁定后distance≤34确有ClearAttack→Recover分支，吻合但缺实际取消分支收据；不能把它命名为claw-stun。直接ApplyThreadPull进入Stunned；旧日志没有同actor的该次K/adapter收据。

Recover0并不意味着游戏禁止普通J，但没有公开真实Recover余时；当时foreign bone-diver attack3为Telegraph→Active，不能据strike/energy就断言安全窗口。下一若追该段，只需同actor取消转移、cast/adapter计数及可追溯Recover起点/余时，保留同窗foreign威胁；不再重查已完成的末窗口/首18行，也不把未知填False。

## 单leaf真实生命周期及I1

初stage `thruster-rock-leaf-stages-20261105-015453-619-3b185662d84b46b4bc5c9775cf982800`：engine1558/build33538ms均0/nonTimeout，普通默认加载，24资源包、仅1raw＋1configured实例；hiddenReady6/visible6、树/shape/world/raw释放16边界均返回。它不是完整冻结地图或body query：只一个地板，没有五sector/140keepOut/原首6查询。

唯一终审C0/I1/M1。I1实际RED020520的managed双故障覆盖首因，唯一修补后GREEN020827两真实夹具通过（engine2408/build31658ms、stderr0）：原异常对象/堆栈、独立尽力owned节点退休、自建资源随后、raw最后、其他owner不改。详细[审查、全部裁决与延期项](../../.superpowers/sdd/2026-11-05-prefix-and-rock-stages-v249/final-review-v249.md)。没有二审或借此声称原geometry根因已修。

host实际consumer先缺失RED，再2正/15负GREEN；曾因PS负例数组优先级误拆，修正literal括号后才作为有效结果，不把测试输入问题写成生产故障。

## 当前完整回归

当前完整旧17＋新stage＋当前I1 GREEN共19案**已终态：17 strictPASS/2FAIL**，root批session77858终态exit1，不再poll/restart；不是全部测试通过。所有build0、nativeTimeout0，原时限未放宽。[全部逐案矩阵](../../.superpowers/sdd/2026-11-05-prefix-and-rock-stages-v249/final-matrix-v249.md)、[机器收据](../../.superpowers/sdd/2026-11-05-prefix-and-rock-stages-v249/final-results-v249.json)。前段142902ms、后段180262ms、保存/键盘/动作与模型等通过；models仍只是该入口机械爪/到达子集，不借其名称宣称所有模型。

- Main当前`022813-830-a74057202bb84254a404b09d89716284`：154942ms/exit1、原899，HP2/恢复0，仍未清摆锤编队。下一批Laser在清理期才出现；旧enemy_health1是缓存，不能说新Laser为1HP。prefix32完整、tail146/overflow18/faults0；原规则未改，失败不豁免。
- geometry当前`022434-320-0ade7a9da9074672a7190ab7b5470f9f`：27669ms/exit1的两项managed断言，详见下节。不是nativeFATAL，也不是已修好所有资源问题。
- Explore当前`023127-970-66c5e5686594451bbdddfb3207e04e90`：319366ms/exit0，从标题、同一player47563408900、23节点/11挑战/11激活/10击败；Boss28385925402131真实fatal99在17962发生，18141实体支撑出口Completed，之后持D60帧至18202位置保持。脚下floor(6080,1116,1600,40)，body(7321.191,1043.9327,40,72)，恢复0/0；physics在Completed后停用，缓存vx260不是继续位移。保存前后SHA同`D68DEC7993D42D14D26D4F98E88C00B907350568CA6F6B52E552E87F08A56779`。proofKind=automated、synthetic=false，但human/operationCoverage/fullGoal=false，不是清完76敌/全操作/真人体验验收。

### 最终绑定与保全

95组source/helper/user/continuation/binary前后文本无差异：只读子审计核对首18的90组，root补最后5组，并在整批结束后重哈希当前1888输入/六binary/21 uniquehelpers，全部0差异。唯一source manifest `4AB7AA4FACAFD0607C7058073D5080705A774AD8E07DA5D8640E47BD311BCEA1`，唯一binary manifest `FD4F01F4C6B2DFA6C85F14DEE811333A352A7CADEB5899E875A5CDD1EDD65EE7`；root批脚本SHA保持`9D11A3B9A7F2DE54AF03ACB44A3D57480BC2CBFD76B66053A07B8138451109EE`。详见[绑定留档](../../.superpowers/sdd/2026-11-05-prefix-and-rock-stages-v249/bindings-v249.json)。branch/HEAD保持原值，无Git写。

Sector-faults3条、post4条为原严格限定的故障负控制warning，source-bound实际consumer已校验，不称全stderr为空；两个失败各有真实managed ERROR并保留。未见其他本次原生FATAL/leak签名，不关闭历史匿名故障。真实用户数据保护前后相同；Explore内部109项保护也明确default_application_data_unchanged=true。

正常汇报一次媒体Check：7427750字节/86文件，Notify=false；无新截图/录制/付费/自动化。完整原十一项、正式动作美术/R-I/R-C/R-P及正常速度、目标硬件与用户试玩仍OPEN/ACTIVE，不是正式交付。

### 当前geometry失败改变下一行动

原geometry本次`022434-320-0ade7a9da9074672a7190ab7b5470f9f`实际27669ms/exit1/nonTimeout，不是原匿名native FATAL。body census及activity均走完50身份，activity selected50/unresolved0（仍diagnostic/未发布）；随后`RunPatrolEndpointReuse`在两个原断言失败：`player_only_endpoints_do_not_hide_foreign_middle_obstacle`、`cross_call_world_removal_is_fresh`。不是完整geometry通过，且尚未到后续leaf controls/JSONL写出。

当前下一行动改为核对这两项测试的真实query结果与障碍布置/同步，判断生产查询还是夹具原因，保留原期望不放宽。此前“全图首6query”只是旧匿名FATAL阶段的候选，现在不继续按旧假设实施。此轮没有FATAL不证明旧独立故障已修，原失败继续保留。

只读假设：两个跨world状态断言使用有序SequenceEqual；原同次batch-vs-fresh比较未列入失败，而每次CheckPatrol建立独立局部dictionary。native IntersectShape命中顺序经过Distinct但未排序，增删wall可能只改变player body/hurtbox理由的排列。日志没有实际issues数组，不能据此改断言或宣布不是生产bug。下一有界控制应保留三个原状态/断言，输出有序issues/bounds，并用实际wall ID验证加后命中/移除后消失；只有证明仅顺序差异才讨论比较契约，其他结果按真实生产/fixture缺口修补。

U-20261105-01维持后续重新审阅现有候选，旧批准不转授。R-I/R-C/R-P、正式分层动作/美术、正常速度/硬件/用户认可与完整原十一项继续ACTIVE/OPEN。无付费、GUI/OS输入、新媒体、额外dump、自动化、Git写/发布/真实存档改变或删除。
