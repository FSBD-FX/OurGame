# 常驻敌人首个真实行为样段：锈甲地面游荡者 v001

日期：2026-10-16，Asia/Shanghai。开发验证记录，不是美术定稿、正式50只接入或整关验收。

## 事实

当前在真实保存的 `RustShellSquire` 上实现了**显式配置才启用**的区域巡逻。未配置的旧波次继续原分支。新增5个C#来源后，实际源码库存为1658，canonical owner的精确库存门同步更新；保护库存109、源码/helper/二进制/副本守护没有豁免。

最终行为专项 `105803` 严格通过：7模型检查、15原生行为检查及原Rust模型/视线回归；engine0、44,964ms、无超时、守护失败0，stdout/stderr/external三源没有诊断。原生检查使用独立World2D、保存Rust场景及真实物理步进，不手动调用Actor的PhysicsProcess、不在挂载后写其位置、不注入其模型状态。

正式保存关卡仍为原26普通名额＋1Boss。**新增实际敌人0、全部50个正式Placement仍null；24配置子型的正式ScenePath仍未实现。** 测试中创建的Rust不是正式骨骸/幽灵场景，也不算正式新增名额。骨骸10、幽灵10及其余新增30仍是本关必做要求。

### 本次真正实现的边界

- 节点无关模型只输出自主移动方向、状态及攻击许可；真实Actor继续掌管重力、碰撞、战斗时序、HP、拉拽和击退。
- 完整身体越出活动区时进入返回，必须实际走回home才结束；不瞬移、不硬夹位置。已经在区外时只允许有真实支撑、碰撞安全且更靠近home的自主步进。
- 自主地面移动检查TestMove及整个前瞻脚宽的连续静态矩形支撑；中心射线、两端接地不能替代完整支撑。一个1px中间缺口或同一地板体的第二个墙形也会拒绝。
- 攻击需要同一World2D内真实可见、完整身体及伤害接触范围在活动区的目标；已锁定预警后目标出区会撤攻击，不能仅靠“失去视线”让旧预警继续。
- 休眠撤出自身抓爪/机械丝目标组和目标碰撞层，撤伤害区权限；根伤害入口及Hitbox.TargetFilter也拒绝休眠中的直接调用。唤醒恢复原配置、保留实际HP，重新预警并使用新的攻击ID，不恢复旧活动攻击。
- 死亡通过真实Health链撤自主动作与攻击；非法、非Wanderer、空Placement、重复或挂载后配置在部分写入前拒绝。
- 自主48px/s、2px到点容差、2px额外前瞻及0.25s撤攻击后的再获取间隔是本样段开发参数，不冒充用户已批准的全族系平衡。

### 15项原生检查与实际观测

| 检查 | 运行中的事实 |
| --- | --- |
| 未配置旧敌 | 无真实玩家时仍Idle，不启动常驻巡逻 |
| 双向巡逻 | x=257.59912～382.40073，最大自主步长0.80007935，持续接地 |
| 平台边缘 | x=300.0003～339.9997，保持接地并回头 |
| 同地板体墙形 | x=257.59912～345.60068，墙阻挡有效 |
| 中心窄台 | 32px台不能承托36px完整身体，自主位置保持320 |
| 中间1px缺口 | x=285.60022～299.2，不走进缺口 |
| 区外真实玩家 | 原检测距离内、Territory外的目标不被追击 |
| 区内真实玩家 | 实际Area攻击对其Health造成伤害 |
| 墙后区内玩家 | 真实墙遮挡，不起攻击 |
| 已锁定攻击后目标出区 | 真实目标MoveAndCollide出区后不进入Active、不造成伤害 |
| 真实机械丝外力 | 最远x=465.48，进入Returning并实际跨回home，无位置夹回 |
| 真实Hurtbox击退 | 最远x=462.59726，回返；HP3→2并保持2，不回血 |
| 活动攻击→休眠→唤醒 | 在下一物理帧前直接TryConnect也被拒绝；入伤/拉拽撤权；唤醒新预警/ID且HP不变 |
| 真实三次伤害死亡 | 实际Health归零后Dead，无自主移动或伤害权限 |
| 非法及挂载后配置 | 拒绝且不破坏已有有效巡逻 |

夹具的被感知目标是实际CharacterBody/Health/Hurtbox，不是完整玩家控制器；上述观察不能替代正式J/K/L、整路、跨区或真人验收。

## 推理与反例

第一轮空实现确实出现7模型及9原生行为失败，但同时有4Shape RID退出诊断，因此**不是干净RED**。后续按真实缺失行为推进，独立资源失败始终保留，未将编译错误、场景前提错误当成行为RED。

实现中的区外回返曾真实失败：Actor已被拉到x465.48/击退到x475.886，却因“下一步必须已经在区内”的规则无法回家。修正为在区外允许安全的逐步靠近home，随后实际跨回home；没有通过改坐标或削弱外力修测试。

还故意仅删去Hitbox过滤器的休眠谓词。`105605` 中其他14项原生及7模型检查通过，只有“下一物理帧前直接调用旧Hitbox”失败；这证明仅关闭Monitoring不足以撤权。该反例另有4RID，不能称干净资源RED。最终已精确恢复正确源码，`105803` 全部15项通过，没有遗留故意损坏版本。

## 资源、完整回归与空间普查

所有case保存在 `E:/OurGame/Knight/.evidence/october13-stable-claw/`：

| case完整目录名 | 当前结果 |
| --- | --- |
| `resident-patrol-20261016-102830-177-7785f0639a4a41ffa547c7679192583f` | 空实现行为失败＋4Shape RID；严格FAIL，不是干净RED |
| `resident-patrol-fixture-20261016-103442-511-29f44b8130db4d6ba8c2b560682568b3` | 3生命周期控制完成，退出4Shape RID，严格FAIL |
| `resident-patrol-fixture-20261016-103617-321-f6700c16c6504cb4ad8ce5ad571bff80` | 去掉不必要Shape复制后仍4RID，复制根因假设未证实 |
| `resident-patrol-20261016-104337-311-5cc2771717994fd6ba11be2293cd7002` | using位置编译失败；不是行为RED |
| `resident-patrol-20261016-104416-592-b68fa2ebc7104adeaeca93d136a050be` | typedArray不能using编译失败；不是行为RED |
| `resident-patrol-20261016-104620-994-044459e2ab904e8b9112d1cc4da27a8d` | 实际外力出区后回返失败＋4RID，严格FAIL |
| `resident-patrol-20261016-104919-154-68631d86695242feaf92638bf0ea8156` | 首13项原生＋7模型严格通过，41,008ms |
| `resident-patrol-fixture-20261016-105122-432-1edf0daffb874f229d030820aeb33326` | 当前实现独立夹具仍4RID，engine0但owner严格FAIL，798ms |
| `resident-patrol-20261016-105605-293-e614d0b075b84eb490d2e302111b83a8` | 故意休眠漏过滤反例只使该行为失败；另4RID，已恢复 |
| `resident-patrol-20261016-105803-183-cf13fee4183040818e5948181c079f8e` | 最终7模型/15原生/原Rust模型和视线严格通过，44,964ms |
| `prologue-pre-20261016-110104-991-58af5eee6b394c368afb2fd236dd1091` | 88,192ms，旧Upper/lower_to_mid_swing sample0.5落错台，149/150；两条刻意无效PresentationProfile警告，严格FAIL |
| `prologue-post-20261016-110509-577-4339c6aa1ff14bb99493553a8e8e685f` | 8,590ms，旧BellChain落地断言(540.0704,1196.0139)/KillZone无法解析ILevelRecovery，严格FAIL |
| `resident-geometry-20261016-110832-567-c2521d77826e4a929298749c28792973` | 19,981ms，空间/Archive5/旧几何各项严格通过；41活动候选/9未解，非运动认证 |
| `resident-allocation-20261016-111502-117-bdf58bd8b3854f82ba342719533f15f0` | 534ms，50身份/24子型/17负控严格通过；50点/50身份场景仍unready，spawned0 |

独立夹具中拥有的Shape已Dispose后失效、Actor Free后失效；借用保存场景的四Shape身份恒定。退出诊断只报告类型/数量，不提供泄漏RID号，**不能由“四个恒定引用”推断就是“四个泄漏资源”**。本次不增加GC、缓存模式、KeepAlive、退出drain、借用资源Dispose或诊断豁免。最终行为专项干净退出不等于独立资源故障已修复；历史Handle/FATAL也没有因此关闭。

当前1658源码的几何回归仍为100保留源（81环境＋19交互）、旧36主/备出生记录、50单身体候选、41不重叠活动包络/9未解，search_capped=true；1553走廊检查、20228搜索节点不是AI运动证明或地图容量结论。`TASK4_MANAGED_WRAPPER_FINALIZER_OBSERVED count=1` 观察仍保留，不借无FATAL宣称生命周期完成。

## 风险、成本与下一步

1. 先补真实外力坠落的环境合同，再推进冲锋/飞行/俯冲等角色。当前V2 KillZone是**玩家恢复**，不是通用NPC死亡；不能复制Skitter的home+240px阈值，否则合法下层地板和跨区洞会误杀。需要为opt-in居民明确绑定真实虚空shape，保持玩家回调/mask不变。
2. 首批TDD：同一真实外力落到普通下层地面存活、穿过实际虚空shape才死亡；随后16px条带高速扫掠、真实接缝雕孔、无敌窗口锁存重试、死亡事件一次、休眠/卸载不冒充击杀。通过Health死亡链，不直接QueueFree凑击杀。该环境合同**尚未实现**，也不能从本次水平回返推断已通过。
3. 当前地面守卫保守支持静态矩形地板；旋转、单向、非World层、查询满容量等实际地形边界仍须测试，不能外推全地形。保守停步可能减少可用位置，最终50点可能需要更多builder围合空间。
4. Rust样段只接Wanderer。其他5职能、全部族系差异、真实飞行巡航/俯冲、远程弹体休眠撤权、跨sector状态和新run/reset仍未完成。不能复制Rust外观并更名冒充骨骸/幽灵。
5. Task1全部50点最终原生认证仍OPEN：真实族系身体/攻击/返回包络出来后重新验证，再进入Task3出生/流送。当前41候选不自动升级为41正式位置。
6. 用户确认Explore失焦时在操作其他窗口。因此本轮只做无窗口验证，不抢前台、不补键、不立即重跑旧路线；该回答不能倒推较早失败录像原因。没有新成功录像、正常新Main/Explore、呈现60FPS、最终美术或真人反馈。

全目标仍ACTIVE。没有Git写入、付费生成、上传、自动化恢复、安装升级、清理删除或用户存档语义读取/改写。现有素材与历史媒体不变，未新增或展示美术草稿。

## 源码身份

| 文件 | SHA256 |
| --- | --- |
| `scripts/enemies/RustShellSquire.cs` | `B24D07B8FFCE97ADDD0F22CABFECE0CD2CD4B4CC42F171FF5EA9C05B1F0FE54E` |
| `scripts/enemies/residents/ResidentPatrolSettings.cs` | `6885D5B18453D8B5C5F7B5226E2A6DAABA5944EBEC914176C83A4F8D4791E00F` |
| `scripts/enemies/residents/ResidentPatrolModel.cs` | `4168E7DAB41AE7B58CC40328F6DDEE2452208B3667E36CEA0D07FF1A245309EF` |
| `scripts/enemies/residents/IResidentEnemyAgent.cs` | `BAB3356D7F02894211B6824A866E737921921EB87DFCCEBE7464670DDEC719FE` |
| `tests/unit/ResidentPatrolModelTests.cs` | `D7AFD87FD79FE5A2C5470B850945AD589C4E40683930C34FC47C392E44C6AA45` |
| `tests/integration/ResidentPatrolRuntimeTests.cs` | `42271C944029CE55038D5B5F36C7C4974BCAEFC0FF665432F7C8908F8FAE0D29` |
| `tests/RunStableClawPullTests.cs` | `12CF7AE01A0761FE977EDF1A03E697107BD2FDC807BC378928D7BABC60DA9B13` |
| `.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1` | `1D28760BA1B41D1DEC97C340E5C7DC1DB0B669AA6FF329939659C87A2A962777` |

备份保留于 `.superpowers/sdd/2026-10-16-resident-population/before-rust-patrol-20261016/` 及 `before-dormant-filter-control-20261016/`。正确Rust与恢复备份精确一致。
