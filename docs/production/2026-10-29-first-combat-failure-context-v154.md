# v154 — 首战失败后的同步取证；未修复教学根因或原生崩溃

2026-10-29，KNIGHT Godot工程`G:/knight/Knight`。这是诊断增量，不是玩家内容/画质更新或交付版；完整原十一项目标仍ACTIVE。

## 事实与实际改动

v153的完整pre在隐藏原始敌人后恢复显示时退回普通机械爪提示，独立first-combat却通过。初始中文、英文重开、330/390迟滞提示本已通过，不能先认定ECHO文案错误，也不能把7条Handle错误直接归为同因。

本轮只改一个既有测试C#：`FirstCombatGuidanceRuntimeTests.cs`。原语言/显示判据先计算，失败后才采样，最后仍对同一布尔值作原断言；不刷新、补键、改坐标、等待、冻结敌人或改变判定。快照标量记录当前指令、玩家/原群组、当前影子选择、340/400双范围、层高、视口、原生LOS首碰及前后帧/输入序列。它不是原Refresh历史，实际迟滞半径明确unknown。

一条与生产同参数的LOS查询，排除敌人全部自有碰撞，areas/bodies均true、默认mask；自有查询/容器/NodePath包装确定性释放，不读取借用Shape/Resource或释放节点。缺失/异常明确UNKNOWN，不能冒认无碰撞。没有生产玩法或资源生命周期改动。

## 全部运行与冻结边界

证据根：`G:/knight/验证证据/october13-stable-claw/`。

| 尝试 | 结果 | 可证明范围 |
| --- | --- | --- |
| pre 161346-434-20f4cb9d475a4054a152503624bb37c5 | build1/35158ms，CS1674；无engine | 自有泛型Godot Array误当IDisposable；改用项目既有非泛型backing using。不是行为RED |
| pre 161546-943-1d831a1916224f02a1da7fe8b97d4e72，epoch A | build0/native−1073741795/84874ms，无超时，FAIL | gchandle.is_released()/0xC000001D FATAL，另有Minimap catalog ArgumentNull/RestoreExploration NullRef；停在BasinSupply，未进入首战，原意外边界快照未执行 |
| first-combat 162528-995-ea33b195012d4ad2a0256f82d0f653cc，epoch B | build0/native0/8286ms，PASS | 新增的真实隐藏敌人负控执行快照；原首战断言全部保留 |
| models 162617-941-f394b64ae5b74ccb96e5d8f288607437，epoch B | build0/native0/671ms，PASS | 原模型门 |
| post 162701-035-9126fab9bf5a465f8e70c0c0ce0023a5，epoch B | build0/native0/216350ms，PASS | 原75案例逐一enter/return＋同owner8次、4条精确故意负测WARNING |

A与B相差测试中观察器完成bool和真实隐藏负控。**B-pre没有重跑，不能与A-pre拼为同源四阶段套件，也不能用B-post安静关闭历史FATAL。** 没有重复pre筛绿或扩大预算。

负控是同一实际保存/挂载原Rust，不是fake actor：expected/selected/owned ID94942268370，敌人不可见但仍有95左右水平速度；射线首碰实际玩家47546631683，LosNow=true。physics111→111、process115→115、input88→88；原生测试还断言玩家/敌人ID、位置、速度与指令不变。本证据只证明诊断在受控负谓词下真实执行且不改变同步状态，不证明v153异常原因。真实意外失败仍缺同刻证据。

## 唯一审查、取舍与延期

一次独立只读审查C0/I0/M1，可保留诊断增量；无C/I修复或第二审查。

- 延期Minor：`HitCount`实际为单次IntersectRay首碰字典的字段数（本案6），不是六个碰撞体；以后可改名HitFieldCount。当前NoHit/Collider/LOS含义正确。本轮不为命名打磨重跑pre，解释随原始记录保留。
- 取舍：沿用指定工程和已有教学行为，只作失败后观察；成本是本轮没有玩家可感知的新机制/画面，不称修复。
- 取舍：当前影子选择/LOS不还原原Refresh或原迟滞半径；成本是不能据观察器自身合格关闭历史首战问题。
- 取舍：新增控制查询会改变后续分配时序；没有隔离因果证据，不用安静案例证明资源修复。需要进一步原生级诊断时另列具体方案并获授权，不再用一次ProcDump许可。
- 取舍：本次窄审查不重新认证v153、其他脏树、完整路线、正式视听或全项目；这些缺口全部继续保留。

没有猜改缓存/GC、强保活、默认资源或引擎，也没有用第二次转换结果伪装第一次导出值。

## 保管与当前状态

五尝试实际24清单对；1178份before选定源码、28份before helper、24份封存bin逐件核验，零不符/现存未列副本。编译失败案只有4对且无成功bin/engine，不伪造第5对或把同一编译器摘要的重复行当两个错误。全部9个历史build/engine精确PID均不存在，不声称全系统空闲。

当前1857运行源/897 C#匹配B三案例，源清单SHA256 `63ADD7A1DABDAF2C3C48995FB778473F07CE6BD487AEA01F97B93FB6502884C3`；普通6bin清单`9E2D2B945A741B512614E40BFD043E98E6E0F4B2D8C55AFC61C59D176C2AEAE6`。11条当前helper一致，canonical仍`CD810E4523BA4015AE40087DB03DC9C1ABA864756B9061D6E4D47800F0252C1B`；HEAD/index未变。仅选定副本保管，不声称每案复制全工程。

原十一项、正式美术动作/UI声音、完整Main/Explore、存档兼容/原生稳定、目标硬件/正常速度/用户试玩及交付继续OPEN，目标ACTIVE。没有付费生成、窗口/系统输入、再次崩溃采集、自动化、Git上传/写入、ZIP/删除或真实存档语义操作。

并行只读核出下一视觉切片：[入口岩土角色样段候选](../art/2026-10-29-entrance-rock-skin-next-v154.md)。不是已完成的新画面。
