# v192：本波机枪手的K后首击、短喷与收尾已实测，完整主线仍失败

## 事实

本轮只改两个既有测试文件：`tests/integration/PrologueCanyonGeneratedMainRouteTests.cs` 与 `tests/integration/RangedWeaponPlacementRuntimeTests.cs`。生产输入、机械爪排序、敌人、伤害、地图、物理、美术均未改；不能把测试驾驶的进步当成新生产能力或美术交付。原十一项完整序章目标仍 ACTIVE/OPEN。

上轮原Main取得实际本波HP2机枪手的K/应用/眩晕/Idle/冻结首边沿后，驾驶又去追其他目标，缺少同一目标的非致命首击。本轮选择器仅增加显式可选的HP2接续；默认仍保留原HP1行为。Main的授权不是任意布尔值：要求真实登记、当前batch1/group-owned、确切actor/保存场景/前缀/target/adapter层级、自己的Shot、已完成K、Idle和冻结边沿，且没有后续未结K。

一次首击复用原J链的范围、冷却、精力、全源危险扫描、松盾与新鲜重检。只在精确HP2情形允许一次尝试，出手后不中途换敌；须有实际`AttackStarted`、同Hurtbox的实际Health接收、HP2→1及原firstJ回执。攻击结束未命中则FAIL，不自动重试。没有写位置/速度/HP、补精力、禁用敌人、另造攻击器或等待池。

首K冻结的同一轮即取得setup义务，避免后面的Jet块抢先。只有真实首击接触且原冷却中立样本结清后才允许创建新Jet机会；已发出的Jet继续履约。此路径的致命J还须等原Jet stage5完成，不人工设置机会或完成标记。原Hop、后续K账、恢复/HP、900外层循环及所有终端条件保留。

### 真实RED与本轮运行

全部经既有canonical入口，显式`-SourceRows 1872`；默认1604未改。每次fresh build原180000ms上限，native串行并冻结源码/helpers；没有延长失败案期限。

| 案尾/phase | native ms / exit / cap | 严格结果与边界 |
|---|---|---|
| 7ad535 / ranged-mixed-policy | 2016 / 1 / 55000 | 真正行为RED：真实保存pair、默认Rust及HP2所有权前提通过后，显式setup仍未保留Burst；不是编译失败 |
| 22b103 / ranged-mixed-policy | 3940 / 0 / 55000 | GREEN：同组实际pair、错误root拒绝、默认HP1及精确HP0/1/2/3控制；仅策略合同，不声称有K/J执行 |
| f2d022 / actions | 9142 / 0 / 55000 | 当前完整输入专项PASS |
| 570459 / Main diagnostic | 142149 / 1 / 600000 | 下述局部链真实通过，但后续原Hop保留断言FAIL；整案不通过 |
| 75444f / models | 513 / 0 / 55000 | 当前机械爪模型子集PASS，不等于全部游戏模型 |
| addaef / pre | 178239 / 0 / 180000 | 当前前段严格PASS，余量仅1761ms，不认证此前超时/资源根因已修复 |
| 0a81cb / post | 210782 / 0 / 300000 | 当前原75案例与8次owner生命周期PASS；不是旧180s通过或性能认证 |

前两案与后五案不是同一源清单。最终五案actions/Main/models/pre/post同源，4PASS/1FAIL；本轮七次native共5PASS/2FAIL（含有意保留的RED）。没有把预期RED抹去，也没有以专项或旧Explore通过替换当前Main失败。

### Main中确证的局部顺序

证据案：`G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261101-075547-202-570459680f804787b1696a683b78b4b2`。本波actor=`16025781275375`，不是同区常驻远程actor。

| physics / input sample | 确证事件 |
|---|---|
| 6528 / 6499 | 真正接受setup J，玩家attack35，accepted1 |
| 6532 / 6502 | 同Hurtbox16025831606951/原Health，HP2→1、contact1、damage event1、原firstJ成立 |
| 6547 / 6517 | 原冷却中立样本结清，速度0 |
| 6549 / 6519 | 实际短喷接受一次 |
| 6553 / 6523 | 原反向制动命令 |
| 6560 / 6530 | 完整实体支撑、中立速度0、dash增量1 |
| 6619 / 6589 | 同actor致命J；自己的Shot ordinal6/physics6588先发生；最终damage2/connections2/Defeated1/HP0 |

`AttackConnected`来自该PlayerCombat的实际Hitbox接受伤害之后，关联实际AttackStarted；未新增独立DamageRequest源ID观察，因此不把这项关联伪称为独立payload-source测量。现有`MixedResourceSnapshot`的危险attack_id与追加玩家attack_id同名，需按事件上下文读取；不要取第一个同名字段误认玩家attack35为危险attack3。

### 仍然失败的后续

原Hop在6619登记常驻远程source，6701锁定cycle5，6734/6745记录两发真实弹。结束时stage1、issued=false、Space边沿0、shots2、faults0：尚未发跳，不能称为“跳跃已发但失效”。

实际`TASK10_ACCEPTED_DAMAGE`（stdout L1234）在physics6746记录玩家HP2→1：SourceRuntimeId=`16025412176459`，实际actor=`16025345068684`，LocalAttackId5，保存Rust的`AttackHitbox`。当时危险winner却是另一个常驻Burst=`4892370408558`/shot13，盾朝Left且实际激活。不能把危险winner当成真正伤害来源，也不能说是远程弹击中。恢复始终0/0，本案不是玩家死亡/reset。

原`MIXED_RECOVERY_HOP_RED`随后拒绝HP已变化的等待状态。失败原样保留，没有放宽HP或把未取得的Hop/后续K义务标成完成。

## 推理与下一步

已有证据支持问题进入了“自动试玩的两侧应战顺序”：左侧远程威胁和右侧近战攻击重叠，驾驶一直朝左防守，原Hop尚未获得触发回执。盾有朝向且受背面攻击掉血是正常规则，不能据此宣称生产护盾随机失效。

下一步在原预算内核对真实Rust打断/右向防守接触/回左防守/单次跳跃的交接，沿既有后续K账验证，不从静态推断直接跳到加免伤、删敌人或改爪角度排序。最佳应对尚待下一轮实际运行，当前Main仍未过；当前完整Explore未重跑，历史v186诊断不能当作本版资格。

## 风险、审查与范围

唯一fresh最终审查C0/I0/M2，无C/I修补、无二审。两Minor明确延期：①新增详情复用attack_id/player字段，易误读；②setup/Jet阶段否决在原late诊断mask外，需将来独立标量，不改变原mask含义。未为日志抛光再改源重跑。

审查set-aside逐项保留：原十一项的美术/UI/音频/渲染/硬件/真人验收OPEN；完整Main已知FAIL；Hop故障因果及修复不属审查delta，由root实际伤害回执单独确认来源；仓库全局正确性/平衡/真实存档不在局部审查内；审查者未独立执行native，只读冻结证据。没有静默丢弃任何原验收项。

前段这次0ERROR且接近上限，后段0ERROR、只含原4个限定WARNING（observer fault1、非Node2D fallback1、Completed→Recovering拒绝2）。安静的一轮不证明历史Handle/Finalizer资源原因已解决，也不是正常帧率、画面质量或商用品质认证。原完整目标保持ACTIVE。

## 保管与授权

全部新案在`G:/knight/验证证据/october13-stable-claw/`，保留source/helper/bin快照、原始stdout/stderr/external、实际native身份/argv/cap/退出及provenance。完整案名：

- `ranged-mixed-policy-20261101-074309-902-7ad535ce198d4db3a8428c0500b2fa50`
- `ranged-mixed-policy-20261101-074543-896-22b103f3fa0d4275a5da2c7e73740587`
- `actions-20261101-075449-444-f2d0225988fc4b05a1367984e94c39a6`
- `thruster-route-main-diagnostic-20261101-075547-202-570459680f804787b1696a683b78b4b2`
- `models-20261101-080018-543-75444f4e47b34ebeaa5f2f67cc29733b`
- `prologue-pre-20261101-080110-042-addaefa1c95c476ab36d881cec967eb9`
- `prologue-post-20261101-080501-448-0a81cb6515b84bd4a8373fbcf98e39a1`

最终源清单SHA=`49780396FC59E0CD1F4FDCAA6D970B2EA0BA32F6C86927E56D3D7C487E5B095A`；DLL=`DA2788C6BB0CE7F48BF54DA3ABB13EE1BD783394B1B2230A8E167935AB95AE22`。Main=`4CD04A50E8BB056D19CF1C5614AD82388BA2496B3671F4A2D9B9D8DB38B9095D`，Placement=`83F53938AF99F39DD2440B89B064A91E3033057216772B4F102EA567A5DAE2B2`。post10-helper清单SHA仍`48D515B7A1AB55E140806BE7E146CC24EF185CE77B91BB440974849706C9B28B`。

HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，分支仍codex/task6-certified-final-20260902T0555，index SHA仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。无Git写入/发布/删除。

保管审计闭合：本轮35对manifest、3434份选定source副本、78份helper副本、42个归档bin逐SHA匹配；只逐次核新案，未反复重哈已审旧案。08:09:39 +08当前1872 source/6 bin/post10 helpers匹配最终post；08:10:14 +08本轮14个owned进程身份按创建时间/路径核查全部终结，14个不同PID均不存在，未操作其他进程。最终binary清单SHA=`554EE88F4A5917683FC061DE49FE69C9542C1C321EED6A7E03A943C8E4EDE37B`。七次build分别27566、26736、25717、26167、27171、26054、27500ms，均exit0/0warning/0error，原cap180000。

08:09:31 +08手动媒体检查：7427750bytes/86files、基线3508、Notify=false，本轮无新媒体。不创建自动化或额外提醒。只隔离无窗口构建/测试与Godot内部键盘事件，没有图形窗口、系统键鼠、真实存档写入、付费生成、误发视频分析或额外崩溃采集。
