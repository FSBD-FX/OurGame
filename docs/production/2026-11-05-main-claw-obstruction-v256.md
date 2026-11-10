# v256 · 主线原LOS阻挡定位与真实战斗清障

状态：本轮普通无窗口Main/Explore已严格PASS，含真实Boss/实体支撑出口/60帧保持/恢复0；当前同源16项全部结束，15 PASS / 1 FAIL。独立审查C0/I0/M1，原生前段FATAL仍未解决，不宣布完成原十一项、正式图稿或发行资格。

## 事实、推理与风险

旧v255普通Main停在Grip9：接纳cast25后Obstructed/target空，但原HP1/Grip8地面保持，没有拒绝branch/collider。此时不应猜改墙、牵引力或HP/时间门。

本轮先只改canonical宿主两处exact trace准入，新增84个实际AST行为控制：新Main早门/build均缺失的真实RED→最小两行GREEN。原3阶段保留，普通7argv/trace8argv、Rebuild/no-restore、混用8旗标/waiver拒绝和零输出、普通3类身份日志污染拒绝、trace行为资格false都实际执行，不只grep源码。新契约未执行整个宿主或启动引擎。

首次trace Main172250-230-97d19f3573a44c5182e22fedb0412d48严格FAIL，158744ms/无超时。stdout4387–4393原查询表明：8880–8885连续命中collider4916160500924，8885/cast25的实际门为losFalse、arrivedFalse、saved_contactFalse、stalledFalse；原分支短路，不据此说预测拉动撞墙。该collider精确等于同案stdout4347/8831接受伤害源ActorId，scene lost-echo.tscn、slot canyon.v2.resident.split-level.02；不是旧ID/顺序推断。它是当前居民CharacterBody，不是其Hitbox。trace allocations可能影响时序，释放比旧案晚1physics，诊断不代签普通运行。

根本的测试策略缺口：现有Grip8真实战斗清障只接到Exploration，Main漏接。保留移动敌人真实遮挡，Main应通过已有操作清障，而非直接移除碰撞/敌人。新的纯scope选择keyboard && GripIndex8 && (Main||Explore)只让两条键盘线进入原函数，选择不是战斗许可。12行literal先以旧Explore-only行为真实RED，再最小GREEN；唯一正常Main调用门随之替换。原GhostClearance、CanCounter、foreign窗口、fresh、原600physics/HP/完整原地面/单player/无额外K/jump/jet/恢复、真实Hurtbox每击减1HP和自然死亡撤碰撞均未改。

这是测试驾驶修复，不是新的玩家能力、地图、美术或难度修改。不能由后台路线成功冒称美术/动作已精美。

## 当前普通Main实际结果

证据根G:/knight/验证证据/october13-stable-claw，case `thruster-route-main-diagnostic-20261105-173806-190-a75d69af370a41cb8c07efc85e6bc4a2`。root18872终态exit0，native250587、failure=null/guard0/无超时、无trace符号。

- 先前受伤仍在：stdout4347/physics8831/input8801，lost-echo actor4919247508682命中，HP2→1。本轮没有阻止或撤销这次伤害。
- 清障stdout4380从physics8838开始，目标ID正是该当前伤害ActorId，HP3/player1；4381/4383/4385真实J，AttackStarted/Connected/目标Damaged各1、rawJ与新reader样本，目标3→2→1→0。4386完成195/600，玩家HP1→1、J3/K0/L2，满足原自然死亡撤碰撞，不删除重置敌人。
- Grip9 stdout4392–4429：真实单cast25，9081清边，jump16→16；原Grip8地面保持至Latched→Reel/failureNone，9150同cast到Swing/arrivedTrue，手动Cancel/failureNone/无boost，最终TASK4_SUPPORT_OK到Grip9实体地面。后续SplitLevel从clawIdle/nextCast26开始，非二次抓取冒充。30条被动样本无遗漏/错误。
- Boss14093致死/Died与arena各1；14272实际越过出口右侧、完整实体支撑、Completed事件/计数各1、save由未完成变完成；之后保持右键盘方向60帧至14333，支撑、位置和完成save bytes保持。
- 结果JSON为8个distinct encounter激活和8个defeat ID同集、3机关/2检查点、恢复请求/完成0/0。它不是76普通敌人总击杀或整地图清场凭据。headlessNormalPlanDiagnostic=true、normalDisplayAccepted=false、performanceCertification=false、wholeGoalAccepted=false。

## 两个源时期，不混用资格

初始epoch1：host trace/普通restore两个case源仍为v255；10保护对0diff，普通restore models645msPASS，普通6binary/source manifest与v255完全相同，[epoch1核对](../../.superpowers/sdd/2026-11-05-main-claw-obstruction-v256/epoch1-binding-v256.json)。

epoch2：两个既有测试CS新增路由、12控制和调用门，SourceRows仍1897但内容已变。模型GREEN173649/native637＋普通Main173806和原14阶段现已全部结束；root10376终态exit1，当前16项15PASS/1FAIL。不以v255的13/16代签，不把epoch1 trace/restore或预期RED混进普通最终源时期。Task2/full goal不标complete。

当前Explorer174448-865-53fb112efbbf44d5a40b76cfc76618ce原生318102ms：Boss17962/完成18141/保持至18202，恢复0/0，11激活/10defeat ID（仅counterweight-lift缺遭遇击败凭据，非只剩一敌）；原Grip8真实三J/213physics600预算/HP1→1/K0，Grip9单cast141/300及jump16→16保持。汇报媒体此前单次Check09:49:24.8913562Z/7427750B/86files/Notify=false，无20GiB新档位，不停工。

## 当前16项最终回归与绑定

| 阶段 | 原生毫秒 | 严格结果 |
|---|---:|---|
| actions | 9716 | PASS |
| canyon-structure-v2 | 1476 | PASS |
| keyboard | 20642 | PASS |
| models | 535 | PASS |
| prologue-post | 179550 | PASS |
| prologue-pre | 22182 | FAIL |
| ranged-models | 637 | PASS |
| recycling-traps | 26039 | PASS |
| resident-allocation | 720 | PASS |
| resident-geometry | 30494 | PASS |
| resident-patrol | 45507 | PASS |
| resident-save-compatibility | 12137/冷1600 | PASS |
| resident-state | 643 | PASS |
| thruster-incinerator-traversal | 34199 | PASS |
| thruster-route-exploration-diagnostic | 318102 | PASS |
| thruster-route-main-diagnostic | 250587 | PASS |

唯一FAIL为本次pre175046-908-30ef8fc764714127bc03d9543048ac1a：22182ms、exit-1073741795/无超时/guard0；stderr1–11为gchandle.is_released/mono_object_disposed_baseref→Dispose→Finalize的FATAL，没有旧Supply Handle/SwapGCHandle错误。stdout78为Camera第3root cleanup-return、79–80 Camera/index0返回（14294ms）、81 Laborer/index1进入、82 off-tree factoryOK，之后没有Laborer返回或Claw/Supply进入。external83–88记录同次FATAL。这是最近观察边界，不指认故障资源或因果。

本次save175529暖12137ms、独立冷进程1600ms均通过；暖stdout14实际144→158/新1440边保持暗/20负控，23确认原3legacy sources，89–91 writer完成；冷stdout3–4证实writer61884/reader51656分离及6annex+6legacy witnesses，7–11完整cold-read完成。两stderr均空。这次通过不冲销当前pre或历史save原生故障，也不声称资源已修。

[audit-v256.json](../../.superpowers/sdd/2026-11-05-main-claw-obstruction-v256/audit-v256.json)实际80保护对无差异：16案source/helper/user/continuation/binary各一对；源/二进制各1variant。当前1897输入及完整文件集、6运行二进制、15宿主工具SHA256无差异；没有只用数量或旧构建冒称同源。最终普通构建无身份符号，AST契约在批次结束后重跑84通过，当前无owned引擎/构建作业。独立审查C0/I0/M1，最终复核另附，不发布Git或删除工作区。

## 独立资源问题仍未归因

并行只读核对旧save164441案的最后标记，未发现明确借用/缓存Resource被显式Dispose：场景loader同一PackedScene返回并实例化，居民source holder只清字典，自建/复制音频和视觉对象有自有释放。第三条AUDIO_DRAIN标记不是整树清理返回，不能据stdout/stderr拼出精确跨线程故障时点或指定某个正常Dispose为根因。原生Handle/FATAL/RID欠账仍OPEN，不改GC/cache/KeepAlive/engine；最终当前suite另报。

## 资源、授权与未完范围

| 本轮资产/记录 | 来源与状态 |
|---|---|
| 两行宿主准入、AST契约、两测试CS | 本地before/差异与真实RED/GREEN保留；策略/诊断代码，不是新游戏美术 |
| 正式身体/披风原件 | V1暂缺，后续重审现有稿，不转授旧批准，不自动付费重绘 |
| 新图稿/实录/付费图/音频/模型 | 无新增 |

仅已授权headless/internal keyboard/隔离存储。无GUI/OS输入/新媒体/额外dump/付费/真实存档实验/GC/cache/KeepAlive/引擎/自动化/Git/发布或删除。完整原十一项、正常版标题至双线、76全量、R-I/R-C/R-P、正式美术连续动作、UI视听、目标硬件/真人认可仍OPEN/ACTIVE。

## 独立审查与本轮全部裁决

唯一fresh审查C0/I0/M1，根代理按实际影响复核同级；保留此四文件有界增量，但整项目仍有真实原生故障，不具备完整交付资格。无C/I修补或二审，审查记录在[final-review-v256.md](../../.superpowers/sdd/2026-11-05-main-claw-obstruction-v256/final-review-v256.md)。

本轮13项裁决与代价，按作出顺序：

1. 延续现有KNIGHT/Native原地工作，不再重问引擎或工作树；代价：隔离有限，必须保before和现有用户改动。
2. 初始只作trace准入与一次原查询测量，不猜修Grip9；代价：诊断时序可能不同、普通资格不更新。后由实际证据扩展Task2，不伪称起初已有根因。
3. 依据同一当前居民挡线证据，Task2将Main接入既有清障而非删碰撞；代价：范围增两测试CS，需要新的普通源时期和完整回归，战斗仍可能失败。
4. 根据用户“判断后直接执行”的明确要求落实此有界策略修复；代价：这是开发者的测试驾驶选择，不代表用户批准新玩法或美感。
5. 原生FATAL资源/归属继续未知且严格FAIL；代价：稳定性仍阻断完整交付，不能盲改缓存/Dispose/引擎。
6. 本轮新调用路径不等于所有继承战斗状态已认证；代价：其他夹具/速率/时序组合仍未覆盖。
7. 无目标或多个Grip8居民时保留原fail-closed，不自动跳过/虚构目标；代价：改变或恢复后的居民群体需另验。
8. 只认当前配置物理tick的实证；代价：其他Hz配置尚未认证，原计时假设不因路由更改获得扩展。
9. trace只定位身份，普通Main/Explore单独实测；代价：有1physics差异的诊断不能代替普通时序/性能。
10. 更早HP2→1受伤继续单独登记，清障不冲销；代价：攻击公平性/可读性和伤害仍有后续核对。
11. 遭遇ID不当作76居民个体普查，Explore缺击败ID不说成只剩一个敌人；代价：全量覆盖仍需补证。
12. 全十一项、正式连续动作、美术、R-I/R-C/R-P、正常显示从标题、硬件与真人认可全部保持OPEN/ACTIVE；代价：仍有大量制作与体验验收，后台通关不是精美成品。
13. 根代理回填当前文档/计划/终态记录，同时保历史过程；代价：旧live条目须按时间读，当前头部和终态附记才是现状，不记Task2/full goal完成。

延期Minor M1：新AST契约自己给wholeGoalAccepted=false，再执行trace分支，没有覆盖真实宿主初始化值未来被误改为true。当前真实初始化值和实际trace元数据均false，故非现存错误放行；未来应测真实初始化，不以改测试种子为true冒充修复。本轮延期、不新开修补；以前延期项保持。该审查覆盖代码正确性，不授权Git发布。

最终普通绑定：2026-11-05T10:23:59.4295251Z，实际16案例/80保护对/1897完整源集/6binary/15helper仍0差异，branch/HEAD保持，新增AST契约SHA256也保留；见[最终绑定](../../.superpowers/sdd/2026-11-05-main-claw-obstruction-v256/final-binding-v256.json)。本次汇报媒体Check10:22:34.4152312Z仍7,427,750B/86files/Notify=false；不设自动化，不停工、不删除。

下一资源身份测试的只读前置检查：现有test-pre-finalizer-identity-admission-20261021.ps1真实运行在17行拒绝。按相同AST规则枚举，唯一命中是canonical host960行已有的Assert-ProfilePairAdmission纯校验调用；不是新原生错误，也没有启动身份引擎或转储。下一应以独立负控验证/导入精确纯依赖，不删除副作用检查或放行任意命令；并先查明BeginOriginalProbeTrace具体作用，不能使用旧含额外GC/图遍历的适配器。当前尚未修改该旧契约，细节见[前置证据](../../.superpowers/sdd/2026-11-05-main-claw-obstruction-v256/next-resource-preflight.md)。这不改变本轮普通16门15PASS1FAIL，也不属于独立审查批准的新代码。
