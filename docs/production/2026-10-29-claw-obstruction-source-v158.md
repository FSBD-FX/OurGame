# v158 · 实际碰撞取证、普通回归与入口实图

完整原十一项目标继续 ACTIVE。工程仍为 `G:/knight/Knight` 的 Godot.NET KNIGHT，不切换UE、不删改旧失败。本轮改进诊断与组件复现条件，**没有修好旧机械爪受阻，也没有新增正式美术或完成整关交付**。

## 先看真实画面

用户批准本轮无窗口回归结束后显示约30秒入口，不发送系统键鼠。原有短展示夹具实际native26835ms、Compatibility像素负控通过，并自动关闭；但用户明确回答**没有看到游戏画面**。显示确认仍NOTSEEN，不能拿引擎exit0、截图存在或此次授权代替“用户看见”。未强抢前台或反复弹窗。

同次两张实际视口PNG已先保存原件、相同字节展示副本及来源/状态/SHA索引，然后直接内联展示：

- `G:/knight/过程图稿与视频/2026-10-29_入口岩壁洞顶核对_v158/`
- 原始case：`G:/knight/验证证据/october13-stable-claw/thruster-approved-lantern-visible-20261029-205729-970-2f3e8a673c534a25bd4eae4bf36fbe04/storage/`

这是保存关卡Title→StartNew→Playing后的脚本左右Idle截图。只有该隔离夹具冻结角色Motor/Input/动作协调器；不是自然动作、完整游玩、正常速度视听或性能录像。没有重绘、裁剪、调亮或新增付费生成。

**root对实际图片的观察，不冒充用户评价：**教学文字与大块半透明区域仍覆盖主角头部/上身和部分路线；场景的材质、光照、远近层次仍是明显技术原型。当前画面不满足“精美完成”。下一轮优先用同版实际画面纠正教学遮挡和入口可读性，而不是继续仅凭几何/日志检查宣布美观。完整正式角色、怪物、场景、动效、音频/UI及目标硬件/真人验收仍须完成。

## 实际原生碰撞取证

新增显式 `-TraceClawObstruction`，只允许精确指定的碰撞控制、完整探索诊断与新受控入口；不混用ExpectedFailure、其他诊断、前台输入或延长预算。构建符号 `CLAW_OBSTRUCTION_IDENTITY` 之外没有这些日志语句；普通成功运行另拒绝stdout/stderr中混入身份日志。

记录取自**原控制器已经执行的** TestMove 和 locked LOS 射线结果，保持原查询、碰撞选项、短路条件、包装器作用域和释放顺序。只在原查询结果仍有效时复制标量ID/路径、位置、法线、motion/travel/remainder/depth、目标/状态/时钟以及原阻挡门状态；没有第二次查询、回调、跨await资源根或生产公开测试API。

先让原真实contact-blockers用例在“缺少身份收据”条件下实际RED（native本身exit0，不误算游戏错误），再加入符号日志，原墙体/偏轴胶囊障碍断言保持，实际诊断GREEN。Thin/Inserted墙的LOS与ClearLineFlankSweep的实际native collider均有原始收据。另直接执行真实Get-ClawBuildArguments函数的10个字面正反例，通过普通7参数、诊断8参数/唯一符号以及错误phase/大小写/混合标志拒绝；这是纯命令控制，不是原生游戏通过。

### 完整trace没有到达旧失败点

本轮唯一带trace的完整探索运行316739ms，先在Grip8收爪返程时被 `canyon.v2.resident.split-level.02` 的 `lost-echo.tscn/AttackHitbox` 命中：physics9416，HP1→0，恢复请求1/完成0。原来Grip9受阻没有运行到，不能称已识别其碰撞体。

### 受控样段通过，不等于旧问题修好

为避免反复重跑整个前缀碰运气，增加明确的受控组件入口：保存CanyonAscent/Player，树挂载前使用v157真实pre-K姿态 `(4773.679,1603.9601)`，保存Camera、1280×720、原E与一次物理K采样、原向左净空动作。净空边界4724来自原post左边4748−实际半径20−原余量4。

它明确不包括敌人、正常流程owner的准备动作、完整尾段和主线，不是传送通关。原 `Task4ReachSupport` 函数体没有改动，也没有伪造PrologueLevel owner或放宽它的断言。

前三个组件尝试是**测试配置失败**：摄像机未开启、原helper要求真实流程owner、手工截取的K循环在Claw处理前过早结束且漏原E。保留全部失败，不称三次游戏故障。校准后脚本K/净空区间1115ms通过，最终普通编译+新增精确收据门再实际1131ms通过；旧受阻未复现，不能用组件阳性覆盖完整路线失败。

没有忽略Floor、跳过Latched sweep、修改margin/重力/推力/地形/伤害或给玩家补血。固定GodotSharp本地API核对还明确：2D TestMove只有五参数，KinematicCollision2D没有3D式多碰撞/index接口；Floor normal或travel方向不能独立证明实际true“只是恢复接触”。没有依据用它放行真实阻挡。

## 最终普通回归仍有四处失败

原55/180/300/600秒上限保留，原生过程全部结束。所有15次构建exit0；本轮15尝试严格6 PASS／9 FAIL，包含日志门RED和3次配置失败，不是整关6/15“完成率”。最终8个普通阶段为4 PASS／4 FAIL，其中短展示仅像素/夹具通过、用户仍NOTSEEN。

| case（时间均20261029） | build ms | native结果 | 严格结果 |
| --- | ---: | --- | --- |
| contact-blockers-201414-231-e9c9dc4df786457b910b7aa1efeacbf2 | 57418 | exit0/5217ms，缺身份收据 | 日志门RED/FAIL |
| contact-blockers-201643-349-9a1251c763994aa8b7f193d71cfd34eb | 51743 | exit0/5205ms | 诊断PASS |
| thruster-route-exploration-diagnostic-201832-200-83b99ed1c60b4637917db8e31b843019 | 53877 | exit1/316739ms，Grip8被lost-echo杀死 | FAIL |
| thruster-ascent-nine-entry-203006-471-a01761f69d8d4a62986f005f3bbd03a4 | 38418 | exit1/1049ms，onscreen配置 | FAIL |
| thruster-ascent-nine-entry-203220-425-c1f9612f6f0f43068d7f73c4c792521f | 33089 | exit1/818ms，owner配置 | FAIL |
| thruster-ascent-nine-entry-203551-655-66e6ca3bb24c46a998eea4b865f3fe24 | 28940 | exit1/841ms，K配置 | FAIL |
| thruster-ascent-nine-entry-203817-644-9f2969c11b1547129bad905592bbb086 | 30100 | exit0/1115ms | 诊断组件PASS |
| contact-blockers-204038-290-a263bba540084823966667e1a5d446e1 | 30249 | exit0/5185ms | 普通PASS |
| models-204123-046-1d7111c0fce04571951bcd525f676618 | 28216 | exit0/549ms | 普通PASS |
| prologue-pre-204159-375-d2929a691f004567b54cb3bd6a05bd22 | 26710 | exit−1073741795/68667ms，FATAL | FAIL |
| prologue-post-204343-703-d9dfacb9a8e54c8192e34d3b4f9b6355 | 30240 | exit−1073741795/172484ms，primary+FATAL | FAIL |
| thruster-route-main-diagnostic-204716-633-dfd32076947d4333adc0e5cdb6442f9f | 36694 | exit1/159796ms，实际战死恢复 | FAIL |
| thruster-route-exploration-diagnostic-205045-307-a1e74274bac849b4aa54055d63ef2954 | 41237 | exit1/212240ms，原Grip9组合断言 | FAIL |
| thruster-ascent-nine-entry-205558-700-647df7e5cc1343b2881255bccf74c167 | 36930 | exit0/1131ms | 普通组件PASS |
| thruster-approved-lantern-visible-205729-970-2f3e8a673c534a25bd4eae4bf36fbe04 | 38377 | exit0/26835ms | 夹具/像素PASS，NOTSEEN |

实际目录在 `G:/knight/验证证据/october13-stable-claw/`，表中phase后补 `20261029-` 即完整名。未超时不等于稳定或性能改善。

### pre资源故障边界

在 `CURRENT_BASIN_BLOCKED_LANE_REJECTED` 之后、walkway支持成功标记之前发生 gchandle.is_released Finalizer FATAL。blocked预期negative异常已被本地catch识别，其finally已返回；后续walkway隔离目录存在，只证明走入配置，不能证明mounted/Playing/24次支持检查或清理返回。无runner primary/退出drain标记，属于前段中间fixture生命周期，具体对象、实例、分配/释放双方栈和GC触发原因仍未知。

### post先有真实primary，随后FATAL

Task7 index11 `HiddenDiscoveryPersistsAcrossDeathRecovery` 的首个LoadLevel未激活：`low-ceiling-bridge.03`（真实scrap-scatter叶）的ResidentBinding读取Profile为C# null，gate在actual_state计算前短路。日志中的profile_missing不是“null或Godot对象invalid”合并判定；保存叶确有导出的Resource_lut5t声明。现有日志没有失败Profile的类型/ID或setter历史，也没有证据表明已执行的index0–10直接改写/Dispose它。

随后出现同类Finalizer FATAL。两者不能仅因临近就认定同一对象/同一根因。原75 executable与owner8没有完成；canonical scope receipt veto是后续资格拒绝，不是原始故障原因。没有补跑筛绿、再次ProcDump或更换引擎/缓存/GC策略。

### 双路线

- 普通Main又在physics6617，被居民precision-burst的局部弹体12杀死，E0/未过热，恢复到盆地检查点 `(1680,3960)` 后触发3120.29位移/零恢复断言。归因5条、缺口0、恢复1/0，未到Boss/出口；不是已经证明的非法传送。
- 普通Explore又到原cast28：Grip9 Travel→Released/Obstructed，HP1与Grip8支撑保留，恢复0/0，原净空组合断言失败。普通编译没有身份日志，实际碰撞体仍未知。不能把带trace那次Grip8战死描述替换这次失败。

## 主线防御储备：不编造阈值

只读核验发现原混合反击已要求完整J+8tick安全时间、两次J的原始能量24；0.35只是诊断比较，不是准入。真实扣费前还有一个回复tick，不能把旧日志决策快照当扣费输入。连续盾既耗能又累热；SafeCounterSeconds是下一危险前安全下界，不是所有危险结束上界，赢家RemainingSeconds也不代表全部弹体/后续射击/两侧威胁结束。

当前未封闭全源未来危险、热量和盾方向可守性，不能用固定0.35或仅“能量足”认证存活。纯储备计算候选未实施，不以Unknown永远拒绝J来刷通过；现有HP/难度/能量/敌人保持。

## 审查、取舍与延期

唯一 fresh源审查 `claw_obstruction_final_review_v158`：C0/I0/M2，仅认可保留诊断增量，不是合并/发布或正式交付。无C/I修复或二审。两项新增Minor延期，旧延期项继续保留：

1. 普通身份输出veto只查stdout+stderr，未包括external独有身份行；当前GD.Print有stdout实际证据，但第三流覆盖缺口仍在。
2. raw pull-hit没有完整GlobalTransform及最终braking/expected-contact许可判定；不能据单行独立重建许可理由。本轮未据此改物理，歧义明确保留。

全部执行判断（含代价）：

1. 在用户指定dirty KNIGHT根原地继续，不建新worktree/UE/Git提交；代价是本次审查不背书无关历史更改，workspace作为未提交证据保留，不删除。
2. 延续已批准完整目标，root按计划直接执行、并行只读调查，未重复要求引擎/执行方式批准；代价是无逐任务独立实现者，依赖一次fresh最终审查。
3. 使用显式符号的原查询标量日志，不加生产公开getter/事件或第二查询；代价是诊断编译/日志可能扰动时序，不能代替普通资格。
4. 拒绝未经封闭威胁/热量/方向证明的Main固定阈值；代价是主线仍失败，需要真正策略证据。
5. 完整trace在更早敌人处失败后用明示受控组件隔离，不重复前缀筛绿；代价是组件不覆盖敌人/正常前缀。
6. 原owner/相机/输入前提不满足时修正夹具并缩明“仅pre-K区间”，不伪造owner或松开旧helper门；代价是不再声称复现完整准备/尾段。
7. 原生执行冻结源/helper/bin/input/Git，仅允许实际清单不含MD的忽略workspace与MD记账、一次并行只读审查；代价是root必须核对所有终态，审查不验收pending。
8. 公共2D文档和Floor hit不足以许可floor-ignore或跳过Latched sweep；代价是旧受阻仍未知，未做碰撞“试绿”。

审查所有未判断项的root裁决（没有从总目标移除）：

1. 全十一项仍ACTIVE/OPEN，代价是不能交付完整声明。
2. Grip9根因/物理修复仍未知，代价是旧玩法问题未解决。
3. 最终Main/Explore已按真实FAIL记录，代价是Boss/出口/探索未验收。
4. 最终models通过、pre/post失败，代价是完整技术资格不通过。
5. 最终组件新增门已实际通过，但只限组件，代价是无因果修复背书。
6. 旧J/FATAL/ShapeRID/pre超时均保留，代价是稳定性闭环仍欠缺。
7. 硬件/任意变换与物理组合不由固定fixture穷尽，代价是泛化/性能仍未认证。
8. 真实存档只核字节库存不读写语义，代价是本轮没有完整存档兼容证明。
9. 最终保管由root和独立只读核对完成，实体快照缺口仍明示；代价是不能称每案1857源全实体备份。
10. 无关dirty历史不由本轮审查接受，代价是不能把本轮C0/I0当整分支质量。
11. 窗口执行完成但用户回答NOTSEEN，不重开/抢前台；代价是可见运行确认仍未达成。
12. 静态实图明确暴露教学遮挡/原型视觉，音画动作UI/硬件真人未接受；代价是“精美完成”仍不成立，下一轮处理可见问题。
13. 媒体原件/副本/SHA和手动容量Check已保存，仅说明保管；代价是本地保存不等接受/发布。
14. 不从不确定引擎文档推导跳过真实碰撞的权限；代价是仍需实际故障见证。

## 最终保管

独立核对15案75/75清单对（source/helpers/user/continuation/bin）、90/90封存bin、7330份实际选定before/after实体均匹配。当前1857源／897 C#逐SHA无缺失/漂移；相对v157仅四CS改变：Controller、Resolver、Traversal组件测试、Thruster runner。当前canonical及Debug6bin匹配最终清单；无源新增/删除、地图/资产/数值改变。

不是逐案全源实体备份：完整Main/Explore缺Descent测试实体副本，清单与当前hash存在，窄阶段只存选择依赖；明确缺口，不事后补造。15个原Godot进程均终态；14个PID消失，46328已被无关cmd.exe复用，未误杀该进程。

- source清单SHA：`36E70F0897E79471AEB790F4EC3979BB8088EC940DA998FC17740F7F74243243`
- helper清单SHA：`4FED725973EB861286554CE89EA61CDA609DBA190591ACBC7FE06D5C533720D0`
- binary清单SHA：`6EA91D91B1016682BEE0F8EF94E42DC26D83FF2B12F6615CFB257AF7E907032B`
- canonical SHA：`6821A6214D9826B90DA53A68EEA19B06B7BCFE980B82D31BA9BCBD0995D0E3B5`

HEAD/index保持。媒体手动Check：2,012,371 bytes／21文件／Notify=false，没有跨新增20GiB；没有自动化、OS键鼠、新付费、新采集、Git发布或删除。正常启动窗口不等无前台变化保证，但没有调用抢前台API。正式目标继续ACTIVE，不标完成/阻塞。
