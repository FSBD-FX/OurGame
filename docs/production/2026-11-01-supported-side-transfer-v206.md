# v206 · 原平台右侧移动/制动已实证，战斗收尾与音频仍失败

## 事实、推理、风险

**事实：** 本轮仅修改G:/knight/Knight两份既有测试CS，生产玩法/物理/碰撞mask/地图/数值/键位/角度优先/存档未改。原十一项ACTIVE；v206只是内部工作记录，不是版本发布。只做隔离headless/internal keyboard，不开窗口或发系统输入。

**推理：** v205原平台左侧8gap区间无解，因此保留原away方案优先，只在它不能容纳且同Rust实际Stunned时考虑另一侧有限轨迹。当前原生已证明5force/3reverse/1neutral实际移动、原floor完整支撑、HP2和原计数守恒；换侧后按真实当轮dx刷新原strike/facing，不从预测端点推断。

**风险：** 实际移动通过不等于后续J或通关。当前同版Main原900末仍RustHP2/玩家HP1，且传播AudioDrain失败；前段超时，整套未绿。没有新正式美术/音频/正常显示/硬件/真人验收，没有减少原十一项范围或保证付费人数。

## 有界实现与TDD

- `TryTask10PostHopSupportedPlan` 先用原单侧计划；失败后才在实际Stunned授权下算相反unit axis，同样完整force/reverse/neutral包络、原floor中心域、原clock与900余额。只算候选，不创建第二输入lease；未新增K/J/跳/Shift、collision exception、姿态写入、恢复或等待池。
- 复用v205一次真实lease和支撑/身份/receipt/HP/跳喷J/K守恒。身体mask允许普通经过敌人是源码事实，不是无敌；真实攻击Area仍有效。
- 中立完成后仅等待一个原外循环tick，避免在旧strike布尔量上直接发J。下轮仅对同Rust已完成且实际retained/full-support的路径，用当前dx刷新side，再走原signedGap、strike、J朝向；其余演员/解析器不改。
- 原13个新控制包含左右镜像、未Stunned不能穿越、两侧都不可用、保留away优先、原测世界端点/5+4sample及当前dx作用域。7e0a16b0 native519ms具名五正例RED，非编译失败；3c91a003 native3757ms通过13＋原41制动/116Rust窗口及真实mixed fixture。

### 唯一fresh whole-change review：C0/I1/M0

I1：真实side在外层计算后，原松盾等候可推进最多3个样本；同Rust可能越侧或出strike带，不能拿旧几何发J。一次修补采用审查给出的保守方案：这条completed transfer路径如果实际releaseWaits>0，不发J，交给下一原循环重读全部几何/危险；无等待及其他演员保持原行为。66ec94e4 native524ms具名wait1/2/3 RED；756c5b6d native3874ms通过18side控制＋原41/116/真实fixture。无二审，无新Minor；旧v204两项覆盖仍延期。

这不是当前Main失败的已证根因：本段原J窗口从未被满足，后续J_release/J_command均0。修补防止未来合法窗口中用过期位置发J，不冒充已完成换侧后的实际J。

## 最终同版实际运动证据

当前Main264ba5a0（native141877ms）记录：

|边界|physics / loop / input sample|原生事实|
|---|---|---|
|一次lease取得|6880 /720 /6850|x4623.61，原floor中心域4619.9995..4780，axis+1，Force5/Stop4|
|五次D实际样本|6881–6885 /721–725|vx36.666668/73.333336/110/146.66667/183.33334|
|三次A实际反向制动|6886–6888 /726–728|vx116.66667/50/−9.166668；已过零不冒认停稳|
|真实松键停稳|6889 /729 /6859|vx0/move0、stage3→4、full_support/conserved均True、HP2|
|原floor交接|6889 /729|玩家4635.402,3423.9685；floor4881951757070、shape9223372074737731243；无额外动作|
|下一原tick新side|6890 /730 /6860|实际dx−23.9458、side1→−1；RustX4611.456，自然移动未冻结|

首轮f597dfcd也实际完成同链，但这里只以上述最终源案例作当前局部证明。真实敌人位置已不同于规划快照，不能以静态8.846gap替代实际23.9458。

## 下一处实际失败：双来源转盾与原J窗口

最终264b中，loop730–899的170条决策strike都True、mixed_counter_ready都False，没有clock≥约.4333333且能量≥24的联合样本。初交接能量29.599876但clock仅.16666664；长clock时仅11.733229或22.199924；能量24.333246恢复时clock仅.2999999。没有已合法的J被漏发，不能用ModeGrapple或最终L held单独解释。

首个HP2→1在physics6922/input6893，真实来源resident precision-burst4900004041770/LocalAttackId17、ordinal已观察。前一决策守左侧子弹；同6922 nearRust attack8 Telegraph剩余.13333315，source4645.456,3420.9336在右方，触发Rust_guard_direction_request。实际盾Held/Active=True、方向Right，未攻击、能量18.133223，旧左侧子弹仍造成伤害。次帧HP1，原HP2来源前提失效。

170条后续on_floor=True、末journal仍RustHP2/playerHP1；完整capsule corridor逐tick明确证明只覆盖lease阶段，不能把之后on_floor升级为全程完整支撑。当前journal没有清完遭遇的证据。

### 音频异常必须单独报告

当前264b stderr唯一传播的ERROR是：

```text
AudioServer did not mix after AudioDirector release
within 60 frames / 2000 ms
```

栈DrainAudioOwner:50→FreeSceneAfterAudioDrain:63→RunTask10:673。当前stdout/stderr未找到原counterweight900断言正文，不能从f597拷贝该异常，更不能把“清理异常遮蔽原断言”的推理写成事实。可证实的是：原900尾journal仍未清＋当前AudioDrain严格失败；首轮f597另有明确的原900断言失败。未放大60frames/2000ms、重跑筛绿、屏蔽错误或宣称新生产音频bug根因已证。

下一优先先保全这条失败链及清理时序（如原异常与清理异常并存，分别证明/记录），然后在同一候选扫描中被动取相邻投射物/玩家胶囊位置，核实际抵达、Rust到Active与盾换向提前量。不直接把nearRust优先改为永久Burst优先；两方向接触窗口可能冲突，必须保留unknown/不可行结论。此新的collector/仲裁/音频修复本轮都未实现。

## 当前回归与失败清单

canonical SourceRows1872；原build180s、policy/keyboard/actions/models55s、Main600s、pre180s、post300s；root串行，运行期间source/helpers冻结。

|case时间/短ID|阶段|native ms|严格结果|
|---|---|---:|---|
|183130/7e0a16b0|侧向缺实现|519|具名RED|
|183520/3c91a003|纯侧向策略|3757|13＋旧控制/fixture PASS，尚非集成源|
|183631/f597dfcd|首轮完整Main|142732|FAIL，原900未清；局部真实移动通过，随后HP1|
|184705/66ec94e4|I1松盾旧几何负例|524|具名RED|
|184906/756c5b6d|一次修补策略|3874|18＋旧控制/fixture PASS|
|185018/264ba5a0|最终Main|141877|FAIL，900尾仍未清，传播AudioDrain错误|
|185313/b25158d6|最终策略|3962|PASS|
|185349/6a645668|最终keyboard|20549|PASS；独立headless title7911ms亦PASS|
|185452/9d2e56f7|最终actions|9492|PASS|
|185532/bff67834|最终models|515|爪模型子集PASS|
|185604/c8f68aaa|最终完整pre|180060|原180000超时/exit−1，严格FAIL|
|185939/d647451a|最终完整post|210524|原300000内PASS|

共12案7限定PASS/2真实RED/3FAIL。最后七阶段5PASS/2FAIL，batch exit1；若另计同源756c重复policy则8案6PASS/2FAIL，不扩大验收范围。pre没有ERROR/WARNING不等于完成：末到Laborer LOS样本、没有完整完成链，不能续签v205旧pre通过。post完整75同名/index有序对、owner8/nodes1→1、finalizer/drain/phase链通过，仅原4限定负控WARNING，无ERROR/FATAL。

Explore本轮未跑，当前双路线资格OPEN；不借旧v201通过续签，也不把尚未清完的Main当Explore放行依据。完整suite非绿，Task1实际移动子目标已证、Main收尾目标仍OPEN，原十一项不勾完成。

## 全部裁定与未验范围

1. 保留现dirty checkout/基线/失败，禁Git写入/新worktree/提交/删除；使用保留式manual ledger替代会提交或删除的skill wrapper。代价：无clean-branch/版本交付资格。
2. 已授权持续inline，不重复问程序性确认。代价：没有新的视觉、玩法数值或真人认可授权。
3. away优先，只有实际Stunned才开放一次相反轴候选；无第二input lease。代价：只签真实观察的该布局，不签所有敌种或姿态。
4. 中立交接占下一原tick，不复用旧strike。代价：消耗原900一tick，可能错过短窗口，不能增加池补回。
5. 双来源仲裁不混入本局部修复。代价：Main仍未解；单纯翻转优先级可能只把一种受伤换成另一种，须后续相对抵达证据。
6. 审查未判断Main/后续HP根因：root只签实际移动、实际伤害链、未清journal及当前AudioDrain错误，不签通关。
7. 审查未签Explore/双路：延期且原验收OPEN，代价无当前双路资格。
8. 审查未运行最终suite：root实跑七阶段，5PASS/2FAIL；代价不能声明全绿或修补已完成整套验收。
9. 旧checkout和v205旧改动非本轮fresh审查范围。代价不代表整个脏树已审或旧风险关闭。
10. 其他演员/路线的旧J freshness未改。代价本次I1不是所有J路径的通用修复。
11. 跨越其他敌人、布局、状态没有通用native保证。代价只引用两次同布局右侧实际运动，不把mask或镜像算术冒充所有原生情况。
12. 生产balance/mask/input/selector原样。代价没有新玩家正式美术或手感改良声明。
13. 旧v204捕获/one-shot覆盖仍延期。代价新18controls不替代旧缺口；无本轮新Minor可延期。
14. Handle/finalizer/journal及音频释放/长程稳定性未修。代价普通post通过不能消除历史及当前AudioDrain失败。
15. 正式visual/audio/UI/art/硬件/真人没有本轮认可证据。代价原相应质量门继续OPEN。
16. 原十一项与release未完成。代价不能正式交付或承诺十万付费人数。
17. custody/hash/process/capacity/HEADindex由root另核，不依赖代码审查。代价不将审查当保管或发布回执。
18. 新镜像native crossing和native threat-interruption未追加。代价这两类未验，不称通过。
19. 当前Audio错误按实际传播异常记录；原断言被遮蔽只留推理。代价需要后续保全异常链，不能把猜测或另一case错误当证据。

## 最终保管、版本与容量

独立只读增量审计仅核本轮12案，不重哈v205：60清单对/5932选中源副本/138helper副本/72bin全一致。保存argv逐一对应各contract，均headless、无fixedFPS/movie；不据此签OS输入或正式显示资格。UTC11:10:53当前1872源项、post consumer10helpers、6bin全部匹配。最终GREEN756c至post共8案同源同DLL，未要求不同consumer helpermanifest相同。

UTC11:11:24按创建时间/路径复核25 owned身份全部终结（12build＋12engine＋1独立title），当时25旧PID均不存在，已知KNIGHT Godot/dotnet路径活进程0。未操作或误杀PID，不把其他线程进程纳入KNIGHT。

sourceManifest `77F2E8351BFBF9AA0833BBE2AB96E829C8B699375056FF1E06DBC6D5818E9F8B`；binaryManifest `5393FEED753C34D9E8AC9BAE09540A3C4688B8734511A9BB42361A1E9EF74FC6`；DLL `2D8AEE4E59449FD03C52C22CC0BBDC039A5CF07B2A5579E9E1F7C6BB0484D1BD`；postHelpers `C5EF45802948AA866E2659414BDBE4BD8D22B0D980D459CAA292638C213429E7`。

Main `5C21505E7B981A9FE23220CB217D03020480DABD0EA62446DDBB122869040A5D`，Placement `893302C488F7D26F34F3CF5AF0023788ECF0475377D793828891FA6E27608031`；既有Keyboard/Actions/Projectile/ProjectileTest/Motor/Geometry/Camera与canonical hashes均未变。HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、branch `codex/task6-certified-final-20260902T0555`、index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 未变，无Git写入。

手动媒体UTC11:08:36为7,427,750bytes/86files，Notify=false/NewThresholds0；未到新20GiB，不建立自动化或停工。没有新正式媒体或截图，既有原件与失败均保留。

无新收费/正式媒体、GUI/前台/OS输入、真实C档操作、额外崩溃采集、运行时替换、定时自动化、Git写入发布或删除。原始资料全保留，原十一项ACTIVE。
