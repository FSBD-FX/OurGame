# 齿轮区真实反制与实体支撑证明 · v114

2026-10-25，唯一工程 `G:/knight/Knight`。原十一项完整目标 ACTIVE，Task15未完成。本轮修改的是自动试玩驾驶和测试消费者，不是生产机械爪手感、正式美术或整关交付。没有游戏前台窗口、系统键鼠输入、新付费、自动化、Git写入、删除或真实存档语义读取；用户 NOTSEEN 不追认。

## 事实、推理、风险

事实：探索路线原齿轮区210帧回能失败，实际常驻墓灯蛾需要防御。新增真实反制从实际键盘和生产preview出发：两次原有J完整结束后，真实方向键接近并更新八向瞄准，一次K命中同一墓灯蛾，同cast、同目标、adapter仅增加1、Latched→Released、后续ThreadStunned、一次采样和松键均成立。最终冻结版在189/210帧恢复至95.33324能量、热量0、HP1→1；没有补能量、回血或删除敌人。

推理：此处不需要增加原回能预算或取消防御。先打断真实威胁，原回能规则即可工作。实际能量见证为55.39983入场、增加63.933407、支出24（原两J），本次L命令/采样均0，因为威胁被真实K打断，不是威胁过滤。

风险：完整探索仍在后面的升降台战斗死亡；Main也仍失败。局部K与回能通过不能代表新机械爪、全人口战斗、正式视听或全关通过。历史Handle/FATAL没有因为这次普通回归安静而关闭。

## 有界实现与两处真实失败

仅三个测试C#变化，生产玩家/敌人/地图/AI/能量/物理/正式资源未改，源1844、添加0、删除0：

1. `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`：反制仅由显式参数启用于Exploration的键盘Gear初始冷却。Main保留原J、防御、回能和清理流程；最终Main仍原两J及95.333236%回能，没有强加K要求。
2. `tests/presentation/FreeMotionForecast.cs`：新增纯值 `HeldThenNeutral`，按实际Motion/Module逐步计算保留方向输入、随后中性减速；没有Node、碰撞查询或运动所有权。
3. `tests/unit/FreeMotionForecastTests.cs`：独立字面期望验证双向停距、原总预算、初始静止、零保留、非法预算及反转的峰值/最终位置差异。

第一候选在真实水平preview选中 GearPistonTwo 地形时没有发K，保留FAIL。后续真实D接近让目标相对方位跨入右上八向，实际W+D preview才选中墓灯蛾；没有重新调用resolver授权、改排序、强写目标或增加等待窗口。施放仍保留所有owner的危险检查、完整J、足够提前量、精确生产目标/LOS/legacy、原HP/恢复/210/.95约束。

一次独立审查发现停距只算“一次方向输入后中性”，没有覆盖K被采样前仍保留的另一次方向输入，且初始瞄准缺少相同门。新预测功能先原生RED（204711，功能缺失），随后GREEN；字面反例为原2.1667px不足以覆盖实际两采样5.5556px，3px边界必须拒绝。反向样本最终仅0.5556px、过程峰值3.3333px，因此调用方检查完整峰值/终点包络。初始瞄准、继续接近、发K前都使用该门，原210总预算不增。

第二个失败不应归为“角色掉出平台”：旧 `HasFloorContact` 要求当前slide事件列表包含指定地板向上法线；原生失败时 `IsOnFloor=true`、胶囊完整位于指定平台内、速度0，slide列表只有真实活塞侧面的 `(-1,0)`。失败后独立足下探测证实同一地板实际碰撞：底部3915.9995、地板顶3916，间隙0.00048828125，小于实际SafeMargin0.08。

只迁移该Exploration反制冷却消费者：旧slide地板证明仍直接接受；缺失时必须同时具备原生着地、完整胶囊、实际SafeMargin内垂直接触，以及新足下形状查询精确命中同一指定StaticBody。没有空中宽限/额外帧/改姿态。真实正例加7个单字段负控均执行通过：离地、部分身体、过大正间隙、过深穿透、零/NaN margin、缺失同一碰撞体证据全部拒绝。最终整段只增加1次该支撑查询，不用于机械爪授权；自有query/shape/结果包装释放，借用body/Shape/地板不释放。

## 最终冻结版结果与下一个真实阻塞

最后六案同一源和二进制：Main/Explore仍FAIL，free-forecast/pre/post/models严格PASS。pre159434ms；post199640ms原75项索引0–74，各enter/return一次、retired0、owner重复8次节点1→1。原四条负例warning仍限定seamless1、room-profile1、flow-registry2，没有扩大豁免。post既有300000ms预算不能当作原180000ms内通过或显示性能。

最终Explore再次得到189/210、两J/一K/同cast/application0→1/眩晕/HP1不变，随后通过左侧安全棚，但physics4629被原wave Bell击杀：

- accepted damage第5条，HP1→0，`SourceRuntimeId=9328383759996`、LocalAttackId2；出生缓存精确映射 `BellBackLaborer.tscn`、`canyon.v2.encounter.gear-pistons`、batch1_1，不是resident/投射物/落伤。
- 同tick实际Hitbox.Connected对应同actor9328266319383；stage=`ride`，Bell在玩家左侧，两者都实际支撑于同一BasinCounterweightLift。
- sampled/active盾均false，能量100、热量0；不能归为能量耗尽。旧升降台测试Step只在lower-window/boarding阶段、且Bell在右边才防守，未覆盖这次左侧乘梯来袭。
- `decisionAge=1303`的旧通用战斗记录不能代表ride当前无危险。致死tick的raw A/D/J/K/L和第二次Telegraph完整起点仍unknown。

下次最小范围：保留同一Bell/HP、原乘梯deadline/甲板与支撑，记录ride告警→实际方向/L采样→接触，再覆盖双侧防御；键盘朝左可能产生真实横移，不能仅加stage字符串或固定右盾。没有在本轮未经实测实现该后续候选。

最终Main在physics7114能量0/HP1→0后发生正常死亡恢复，原no-recovery/无位置重置门仍否决。其首异常是3117.98px恢复位移，旁路观察也记录同tick recovery request1/completion0；不把正常死亡恢复误称为新传送实现或通过路线。

## 全部十五案（均build0、非超时、guardFailures为空）

证据根 `G:/knight/验证证据/october13-stable-claw/`。6严格PASS、9FAIL；早期RED/候选失败不回填成后来绿色。

| 目录 | 原生退出/毫秒 | 结果 |
| --- | --- | --- |
| thruster-route-exploration-diagnostic-20261025-201956-956-fc146f67a42844bbbc9891531731ee27 | 1 / 88277 | RED，无真实K |
| thruster-route-exploration-diagnostic-20261025-202435-099-9aca2c71d154462b9dd385e7b8b5810e | 1 / 78186 | FAIL，真实preview地形胜出 |
| thruster-route-exploration-diagnostic-20261025-203559-921-c2d916663dad4f499c0fcfb06af66281 | 1 / 63894 | FAIL，原支撑断言；当案收据不完整 |
| thruster-free-forecast-20261025-204711-807-6465ad92f33f4162a1ec59bc38295f03 | 1 / 401 | RED，保留方向输入预测缺失 |
| thruster-free-forecast-20261025-204957-964-7c3265103fcc46fd890ab5371e33dc01 | 0 / 378 | PASS，新预测及原专项 |
| thruster-route-exploration-diagnostic-20261025-205026-070-6eba0e8c6e7d4022bf1f8e39083c5283 | 1 / 63381 | FAIL，实证K/app/stun后旧支撑断言 |
| thruster-free-forecast-20261025-205524-517-5f7ade1f89ba46d69ef6d9bb313e418d | 0 / 428 | PASS，含反转峰值 |
| thruster-route-exploration-diagnostic-20261025-205558-925-228ad37ac8a7417b8ae3f111f7272702 | 1 / 65061 | RED，旧支撑否决但原生足下同地板为真 |
| thruster-route-exploration-diagnostic-20261025-210100-191-5a5b631b092a48a4a4aca77e19126793 | 1 / 87977 | 局部189/210通过，后继ride死亡FAIL |
| thruster-route-main-diagnostic-20261025-210544-024-75e5f7942173440d845e3dcf9b61026d | 1 / 132530 | 最终Main FAIL |
| thruster-route-exploration-diagnostic-20261025-210826-734-0c0ee72c80934b94a1808b871c500c91 | 1 / 86788 | 最终Explore FAIL；局部反制再通过 |
| thruster-free-forecast-20261025-211023-249-668f762d127a4ca8b6731bb924db560f | 0 / 374 | 最终预测PASS |
| prologue-pre-20261025-211053-048-07c10e1721184383b7b87f3d3416a894 | 0 / 159434 | 最终pre PASS |
| prologue-post-20261025-211401-460-f9f58d778b4a46dc9a0675cbdf1a16bc | 0 / 199640 | 最终post原75＋owner8 PASS |
| models-20261025-211749-380-afc333e339c44225a5c4cfb2a864adff | 0 / 492 | 最终普通Debug/models PASS |

## 一次审查、一个修复轮及全部边界裁决

一次fresh review：C0/I1/新增M0。I1按上述测试先RED后局部GREEN修复；完整suite仍有两条路线FAIL，不称Task15完成或Ready to merge，没有第二次审查。继承的energy/unwind提前异常未汇总Minor继续延期；新增counter cleanup标量不是修复原energy汇总。

以下13项全部承接审查者Declined-to-judge，均为root明确裁决：

1. 第二候选的cast/app/stun：其旧FAIL不回填；只采用后续各案自己的完整收据。代价：若混案会伪造因果。
2. 当次地板失败根因：后续独立原生足下证据支持消费者不完整，不推断是预测或生产物理坏了。代价：错误会改坏真实运动。
3. Main/Explore及全回归：最终同冻实际运行且两个FAIL保留。代价：把局部当整关。
4. 原十一项：原稳定ID、76普通+Boss/NPC及完整交付门不缩减。代价：遗漏用户要求。
5. W/preview排序：只改驾驶的实际站位/八向，无生产排序更改，不认证全局。代价：其他方位仍可能有未验问题。
6. 全目标遮挡/receiver：本次只有精确Moth原生合同，不扩成所有目标首hit证明。代价：漏掉非法目标或遮挡。
7. Intro/preparation/Playing：原准入未改，不据本轮重签所有夹具。代价：准备态假通过。
8. callback/deferred退出：新局部observer清理/收据已审，整体身份和退出仍独立验收。代价：卸载后旧回调。
9. LoadLevel清理与双异常：未改，旧合同保持，不以counter finally替代。代价：漏资源或丢主错。
10. Handle/FATAL/全部内存/FPS：保持OPEN。临时Level PackedScene、pre-Ready原生Variant容器缺少显式CLR源字段本身不证明所有权坏；生产Continue是RestoreFromSave，不是Supply测试的新根重开，不能照搬manager缓存修复。代价：盲目保活/GC变更掩盖崩溃。
11. 继承energy-unwind：继续延期，如早失败缺汇总仍保留FAIL。代价：诊断信息不足而非功能已合格。
12. 正式AV/相机/硬件真人/发行：NOTSEEN及全部原验收保留，无新游戏显示授权。代价：提前交付低品质或不可见版本。
13. 全树/二进制：由独立只读保管审计补足，不拿窄审查代替。代价：跨版本拼接。

其他root裁决：支撑门迁移只接受更直接的指定实体证据，代价由原生着地/完整胶囊/实际margin/同碰撞体及七反例防守；初始反制显式限Exploration，避免把此路线策略变成Main新增要求；所有失败保留，不以额外预算、资源补充、关闭危险、借用资源Dispose或新缓存策略换绿。

## 终态与文件保管

最终batch69574 exit0指外层串行批次终止，不代表子案全通过；六子案各自结果如上。所有原生进程及独立保管审计结束后才写本文和索引，CIM无自有Godot/dotnet。

独立审计：15案75清单对/150清单文件/90封存二进制；missing/drift/malformed/duplicate/extra均0。各案自身前后对应；只有最后六案作为同一冻结组，不把不同候选拼绿色。当前1844/1844源和Debug6/6实物匹配最终models，relative v113仅上述3测试C#改变，生产Rust及RustSight测试保持。

- 最终源清单SHA256：`6E927ABCCBB1F1C030DAE8D9435F9DA24D84F9B08FFD2DC66BAE22C6ED9E5028`。
- 最终binary manifest：`00A1C1CD009BAD1A86A31454BD98122CDC5437784C011928174667E3597CE287`。
- Main测试：`09720BC51D3853B2C4BE519B01BB7367394170C24F12CBC3A14CB69B3E9980FC`。
- 预测工具：`7119A59376626A2686F61637955D3C0D1CD06FC45EEAABF0430F64DD06623662`。
- 预测测试：`EC5DEC72A5F154C16C7910059A5E1FE871928F3279DA7FC0D1E573854CA25CAB`。
- owner/helper `309DD6C4…255F2`、index `21610FDC…945C`、音频布局 `CAD39DF1…EC7A`及HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`均完整核对不变。

本次仅一次既有手动媒体检查（UTC12:12:58.8238603）：860284字节、基线3508、增长856776、Notify=false；没有新游戏媒体或自动化。此记录及所有测试通过均不等于正式视听/正常窗口/真人/硬件性能/版本发布；完整原十一项继续ACTIVE。
