# v195：实测跳跃与弹体几何，停止盲调K时机

## 事实

KNIGHT唯一工程`G:/knight/Knight`。原十一项目标ACTIVE；本轮只给一个既有测试CS增加被动观测（17增/2删），未修改生产玩法、物理、敌人、地图、输入策略或美术。不是体验增量或正式交付。

唯一新Main案：`G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261101-092753-501-3ba89c4dbbb84987987e758baa4b540f/`。build29661ms/exit0，0警告/错误；native134278ms/exit1，原build180s/Main600s，无超时，guardFailures=[]。严格FAIL原样保留：第二发shot14于physics6754造成HP2→1，之后原空中保持检查拒绝继续；不是测试已修或通关。

新增观测复用原已验证的shape/capsule/gap，原96条journal上限，本次27条；无额外空间查询、按键、等待、位姿写入、资源赠送或补跳。最后已退休对象不再为日志读取native ID，ordinal沿用原真实独立发弹身份。

| 新案日志行 | 实测值与边界 |
|---|---|
| 1523 | 真正`PlayerMotor.MotionSettings`快照：Jump590、Gravity1850、JumpCut230；Hz60、TimeScale1。不是作者Resource或猜测默认。Hurtbox胶囊R20/H72、世界基向量(1,0)/(0,1)。本次没有直接记录回调delta，不能将Hz当作每次delta原始回执。 |
| 1525、1527 | Space命令6714；真实起跳6715/input6687，Vy−590，单条raw边沿、无盾；X4620.1094。 |
| 1532、1540 | 第13弹6733、第14弹6744，实际发射相隔11tick。两个实际圆弹均R5，枪口(4533,3459)，锁向(.9276568,−.37343386)，launch shape与signal origin一致。 |
| 1534、1545 | 同post-Motor观察边界下，第13弹发布点由6734的(4539.8027,3456.2615)到6750的(4648.6465,3412.445)。 |
| 1534、1535 | 6734采样最高点Y3324.8977/Vy−4.16657；6735的apex marker是首次Vy转正，不是精确最高点。 |
| 1547 | 第13弹6752通过完整胶囊，原观察器逐tick单点最小净距.67118263；不冒称帧间连续扫掠最小值。 |
| 1236、2742 | 第14弹6754实际伤害，source4896698927596/attack14，attributed=true、HP2→1；同帧有on_floor=true/Vy0着地采样，但未完成同floor、稳定两样本及Hop5验收。 |
| 1550 | stage3、raw Space1、shots2/planned2、passed1、faults2；第三弹未实际发射，伤害后的退休observer错误不是先行原因。 |

旧v194全局physics比本案多1；对应起跳input sequence同为6687，不能用不同进程的全局physics直接当策略提前或延后。两个版本均未提前松Space剪跳：只在首次非负Vy标记之后释放。

## 推理：条件化窗口，不是原生重放

独立只读分析与root独立算术复核得到一致表格。根据上述16tick发布点差拟合位移`(6.8027375,−2.73853125)/tick`；按60Hz的条件模型速率约440。当前实际Motion快照已读取，但运行中每步delta、风修正、原生碰撞边界并未逐步全部记录。

模型明确假设：枪口/锁向/两弹发射时刻不变，只将中立普通跳平移δ；按1/60秒半隐式积分，起跳首个实际中心Y3414.092、Vy−590，随后Gravity1850，碰到原地面后固定。世界胶囊中心线Y±16，合并圆半径25。每tick取同边界发布点开始的前向线段`[P(t),P(t+1)]`到该tick冻结胶囊中心线的最近距离减25。此为反事实几何筛选，不是新native尝试。

| δ/tick | 首弹完整模型最小净距 | 第14弹只算到6754 | 第14弹无取消完整外推 |
|---:|---:|---:|---:|
| 0 | +.67110 | −12.72071 | −25.00000 |
| 1 | +5.43135 | −12.72071 | −25.00000 |
| 2 | +9.04248 | −12.64633 | −25.00000 |
| 3 | +13.39472 | −8.82133 | −25.00000 |
| 4 | +17.13676 | −2.20850 | −25.00000 |
| 5 | +21.07206 | +4.98333 | −23.67381 |
| 6 | +24.95329 | +12.08443 | −19.81351 |
| 7 | +28.47491 | +18.87099 | −16.54688 |
| 8* | +32.08169 | +25.25194 | −11.33418 |

δ8只作为取整边界候选，未认证合法。δ0首弹模型+.671103与原记录相近，但第14弹模型首个负值在6753，实际伤害6754，仍差一个边界；错误地把后向段配当前胶囊还会假报首弹命中。因此这不是已校准的原生碰撞重放，不能依小数接近就宣称同步语义已证明。

重要结论：只截取到当前失败终点，会把δ5–8误看成安全；在“弹不被world或owner取消”的完整外推里它们后来仍与第14弹相交。单纯把K再后移几tick，没有足够依据当成完整修法。本模型没有第三弹，也没有证明K后移能让实际起跳等量平移，不能扩大成整个原合同无解。

## 风险与下一步

生产实际采用圆弹扫掠：`RangedWeaponProjectile`先Sweep、移动、PresentActive，再交付伤害/Retire；Hop观察器在Motor之后。`RangedAttackGeometry`实际native CastMotion与候选查询，不能用单点日志代替。普通Player保存负优先级属于Input/ActionModes/Claw/Environment，Motor没有显式设置优先级，不能误称Motor为负值；Hop观察器100。Motor与弹体的实际同优先级树顺序、原生space shape同步快照，本轮没有逐实例记录，不归因为引擎缓存bug。

下一优先用独立无窗口原生夹具校准“输入→Motor/Hurtbox→弹体Sweep→观察”的实际顺序与delta，并补齐完整三弹/退休端点，再判断同阶段回盾后立即跳的合法时域是否可行。若测试驾驶合同确实过窄，应明确区分用户要求与新增测试假设、保留原零伤格挡/单跳/HP和真实通关证据后讨论战术组织；不能删断言、给资源、跳帧或把局部绿色换成整线证书。

当前完整Main仍FAIL，Explore本轮未重跑；旧pre/post资格不借给新增日志版。正式美术、连续动作、UI音频、正常显示、硬件性能与真人验收均OPEN。没有为观测字段重复跑前后段筛绿色。

## 审查、保管与权限

本轮计划：`docs/superpowers/plans/2026-11-01-hop-ballistic-feasibility.md`。唯一fresh只读最终审查C0/I0/M1：冻结diff、原始失败、五清单对、27条日志均核对，审查者还独立重算九行表格一致。无C/I修补或二审；只允许保留观测delta，不是完整Hop/Main/三弹或目标验收。

延期Minor：模型可复算文档尚未显式写正δ方向、原地面数值和完整外推终止规则，后续应补明或保留小型纯算术脚本。当前限制与失败结论不依这些细节冒认安全，本轮不修Minor。

| 审查拒判事项 | root裁决 | 错判成本 |
|---|---|---|
| 1.完整三弹可行或全局无解 | 只认两发实测与受伤，第三发/退休端点继续待测；不认证任何普遍结论 | 误判安全窗口或不可能性 |
| 2.精确原生重放/顺序/space原因 | 保留一tick差异；下一原生校准，不称缓存bug | 错误归因、修错生产机制 |
| 3.K/跳跃修法及合同调整 | 本轮未实施或授权改合同；保留旧门，未来修法另做行为TDD及当前回归 | 静默放宽验收或盲调 |
| 4.旧逻辑/双路/广域/平衡/资源原因 | 不借单案或旧pre/post资格；原失败与完整范围仍OPEN | 虚报稳定、通关或平衡 |
| 5.全量保管/当前源/进程身份 | 采用另一个只读核账及root原始回执，不把代码审查当全量custody | 混版、遗失证据或漏活进程 |
| 6.全目标/正式视听/硬件真人/真档认证 | 维持全部原十一项ACTIVE/OPEN；本轮未写真实存档，不等于完成全部存档兼容验收 | 越权或错误正式交付 |

唯一新案5对manifest前后相同；所选source285+285=570份、helper5+5=10份、归档bin6份逐SHA匹配。source清单1872行/helper9行/user109行，continuation空白。09:33:34 +08当前1872 source/6 bin/9 helpers匹配本案；09:34:07按creation/path核对2个owned身份（42640/47356）终结且PID均不存在。未重哈旧v194案例。

- sourceManifest `5A967C12270C3ABBA789913F58619A5F72DCAFA5D790C43DA2E77B9EE53C42F3`
- helperManifest `39437A3388DEDFD597F3F912F49B865AE52628107029935C3A0322D716DFB213`
- binaryManifest `34F56F3C98F314C5E874DA5ED6815B5BDEE31A2C3A12218B4F00A6E0BA1BA753`
- userManifest `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`
- continuationManifest `7EB70257593DA06F682A3DDDA54A9D260D4FC514F645237F5CA74B08F8DA61A6`
- DLL `220303ECB197A6EEB9652D5BFA8D52D1A0C9534184C65BB0DA9DF453FB6E8DF6`
- Main CS `539BF8D40271B1739DABD05CF5A9C4D40671331314837FCC793D2A2BA724C7D5`

实际argv为本案storage/log、RunThrusterCoreTests、headless、1280×720、route-main-diagnostic、keyboard-route；无fixedFPS/movie，automated-suite，不认证OS来源。stderr1ERROR/0WARNING/0FATAL，external只镜像一次断言，不重复累计。

09:41:43 +08手动容量检查为7427750bytes/86files，Notify=false，无新档位或媒体。HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index SHA21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C，Main SHA未再变；root复核两个本案PID均不存在。

仅隔离headless构建/测试、内部Godot键盘事件、只读调查与本项目记录；没有GUI、系统键鼠、真实存档写入、付费生成、额外崩溃采集、误发视频分析、自动化、Git写入/发布或删除。本轮无新媒体，20GiB仅提醒不停止工作。
