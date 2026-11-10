# v124 · 真实锈甲攻击几何接入与原路线边界

原十一项完整目标及Task15继续ACTIVE，工程仅为G:/knight/Knight；真人显示仍NOTSEEN。改的是自动键盘试玩的危险消费者，不是生产敌人、美术、手感或难度升级。没有把本轮局部验证当作完整序章交付。

## 事实、推理、风险

事实：物理v123基线至最终只改两个测试C#，Main66+/6−、Placement57+/0−，合计123+/6−，源文件增删0，Source1844/C#892；生产、helper、地图、HP、能量、键位及原900/300/52预算不变。原扫描只借用了Moth的Active圆形，没有接入生产Rust已经发布的矩形。

推理：补齐真实同attack/phase的矩形是后续防御判断的必要输入，但不自动改变“Active先于Telegraph”的原仲裁。本轮实际主线证明：只补Active距离仍晚于致伤，不能据此称转盾已成功。

风险：完整Main/Explore仍失败，后段还再次发生原生Finalizer FATAL。安静前段、纯数学通过、代码审查及保管一致，均不能替代通关、正式视听/相机、目标硬件或真人验收。

## 两次真实RED与最小实现

1. 原保存场景Rust自然进入首次Telegraph，原同一次扫描的候选HasShape=false、leaf0，而实际发布leaf51606718734存在；081321实际RED，native1/1712ms。最小修正仅借用live/in-tree/nonqueued、同非零AttackId/Phase、形状为Rectangle的Rust发布叶；Telegraph和Active均可记录，Moth原Active Circle保持。
2. 原生发布边界通过后，081522在独立手算的“右侧矩形表面间距10”处实际RED，原Circle函数返回Infinity，native1/1837ms。最小修正为纯数学胶囊轴线—旋转矩形距离：转入矩形局部坐标，slab交叉检测＋端点/角点距离，再减一次真实均匀缩放的胶囊半径。不是旋转后世界AABB近似。非有限、非正尺寸、未知形状、skew或不等缩放保留未知Infinity。

最终081716通过真实发布、14个独立字面量距离/退化拒绝及旧tight-warning否决。Points按内容比较：原被动扫描会复制只读Points，不能用集合引用的record相等冒充几何比较。真实AttackHitbox.GlobalPosition仍是GuardSource，矩形最近点不被用作盾方向。

没有新增射线、第二次危险扫描、敌人过滤、位置/速度写入、额外K、等待池或付费素材。所有来源的ConsiderCounter/SafeCounterSeconds保留。新距离只进入原Active并列仲裁；Telegraph优先级和未知回退不变。生产Rust在MoveAndSlide前发布，因此本轮证明的是实际发布形状，不是每个采样时刻postmove collider绝对重合。

## 原生结果

证据根：G:/knight/验证证据/october13-stable-claw。9案普通构建均0警告/0错误，ExpectedFailure空、guards0、无超时；无fixed-FPS/movie或额外诊断Defines。

| 案例 | strict | native/ms | 实际结果 |
|---|---|---:|---|
| ranged-mixed-early-boundary-20261026-081321-813-d455472ceb0e452ebfb301ef9531a75b | FAIL | 1/1712 | 缺真实Rust叶RED |
| ranged-mixed-early-boundary-20261026-081522-756-b2824e11710c4c45ae68c44632bf8ef3 | FAIL | 1/1837 | 发布已通过，矩形距离RED |
| ranged-mixed-early-boundary-20261026-081716-596-57a814921b304a07a73b85812dafb4c3 | PASS | 0/1770 | 发布/14数学/旧边界通过 |
| thruster-route-main-diagnostic-20261026-081746-591-0de7e2690eb14409befedf5a488eb0a0 | FAIL | 1/129590 | 7226真实死亡恢复请求 |
| ranged-mixed-early-counter-20261026-082201-231-d3781f689421436b9e7a43bb6279e3fc | PASS | 0/4230 | 原2次不同J真实接触，HP5保持 |
| thruster-route-exploration-diagnostic-20261026-082239-224-1b3d814d2d04452a90c0794fcb09e0b0 | FAIL | 1/162491 | 先ghost伤害，再Grip2准备首帧bone死亡 |
| prologue-pre-20261026-082555-694-e528b8d949414d2b9527a97124de8464 | PASS | 0/156841 | 普通前段通过，不关闭历史崩溃 |
| prologue-post-20261026-082901-853-291b1044c7504cc1bb0e6a8c37c08b76 | FAIL | -1073741795/186789 | index49进入后Finalizer FATAL |
| models-20261026-083238-415-d2ee8f957b5e4544a16d45809ddd36a3 | PASS | 0/502 | 普通模型回归通过 |

合计4严格PASS/5FAIL，含2次有意TDD RED，不计作普通成功。5个FAIL的ERROR按stdout/stderr/external分别0/1/1，是镜像而非两次独立事件。post WARNING为0/2/2，其余案0；其中FATAL与ERROR为同一条，不重复计数。

### Main：形状已真实接入，但原优先级仍来不及

仅081746本case：physics7223/7224/7225，Rust17367941780659、leaf17368310879641、attack5均已携带Rectangle，extent13x9/rotation0/source4773.1426,3420.9336；Telegraph剩余.049999893/.033333242/.016666591。玩家水平速度0、仍左L，左resident Burst4772195207679一直是原winner。没有观测到原扫描中的Rust Active安全转盾机会，7226即请求恢复。原900、同源危险/HP/恢复门未豁免。

early-counter的两个真实J分别attack1/2，enemyHP3→2→1、playerHP5，started58/185、completed76/203、marginEnd82/209、native152ticks，Active breaches0。这是原局部反击窗口，不是Main通关。

### Explore：本case属于FullTop入口，不混用旧Step8案

082239中8546由ghost-diver resident.counterweight-lift.03（actor6170324503007、hitbox6170408389092、attack1）使HP2→1。之后真实FullTop地面接近132/300到4964.514，HP1保持，原抓落台/Grip1完成。Grip2准备9045首帧，bone-wanderer resident.counterweight-lift.01（actor6164116935680、hitbox6164184044548、attack1）使HP1→0/request1；Space/K均未发，原外层HP/300门拒绝。

此case不是v123最终Step8入口的8799/HP2→1。不能把132与71、1→0与2→1、不同source ID或时钟拼成一次成功。只读代码核对没有证明预告漏画；仍缺实际显示/像素及真人可读性证据。

### post：主要事实是原生崩溃，不是单纯scope不匹配

原文FATAL为gchandle.is_released()，mono_object_disposed_baseref、GodotObject.Finalize及GC.RunFinalizers，0xC000001D/refcounted_disposed栈保留。registry50 enter/49 return；0–48各完整，index49 CompletedSaveStartsCleanAndFirstDeathReturnsToRoundStart仅enter，50–74未进入。owner8完成收据、普通PHASE_OK/TESTS_OK均0。只到seamless、room-profile两项warning scope，flow-registry未到。

helper首先报scope-receipt mismatch，不能取代native exit/FATAL，也不能称75或owner8通过。前段quiet不构成修复。当前仍没有native owner归因、匹配符号或用户确认的隔离原生调试方案核验；未下载、安装、attach或改engine/GC/pin。既有非阻塞问题未答，不据预选当授权，其他安全工作继续。

## 唯一代码审查、全部范围裁决与延期

一位fresh gpt-6-astra/high reviewer /root/v124_rectangle_review，对两个物理v123基线及当前四SHA逐一核实。新增C0/I0/M1；根按用户实际影响重评同意，不做修复pass或复审。M1是测试覆盖不足，不是静态发现的数学实现错误。

| 审查未判项 | 根裁决 | 错判成本 |
|---|---|---|
| 成功转盾/Main完成 | 实际Main7226死亡，继续FAIL | 把必要数据链误当通关 |
| postmove collider相等 | 只承认实际pre-move发布 | 用过期几何给动作签安全 |
| ETA/后续输入时序 | 未推导旧弹速度，瞬时距离不签未来接触 | 反向弹命中或迟转盾 |
| near-warning/consumed豁免 | 本轮未实施，原优先级/所有来源反击否决保留 | 隐去真实危险/制造J窗 |
| 新查询/生产难度/地图/预算 | 两测试delta未改这些，旧门仍强制 | 削难度或放宽测试取绿 |
| native与保管 | 以独立原生/45对审计为准，不借review背书 | 混版或把崩溃记成scope小问题 |
| 原十一项/76/BossNPC/正式AV相机/硬件真人/资源/发行 | 全部稳定ID及原完成条件保持OPEN/NOTSEEN | 缩小目标或发布未完游戏 |
| 四继承Minor | 本轮未修，继续明确延期 | 日志缺失/措辞被误读成证明 |

另两项实施裁决：继续现有脏分支/计划/BASE，不新建任务或Git历史，代价是必须按物理快照归因；先修缺发布并仅接原Active距离，不猜转盾策略，代价是本轮仍可能无法存活，实际已保留该FAIL。Points内容比较避免引用伪失败，代价是仍需独立原生边界证明，不用纯值代替实际叶身份。

新增Minor延期：现有90°分离矩形及45°交叉测试不能区分错误的world-AABB捷径。建议下轮加入独立非轴对齐分离案例origin40,50/half10,5/rotationπ/4，现有capsule20/72预期间距74/√2−30≈22.3259018。本轮未改该测试，不冒称覆盖已齐。

四旧Minor未修：energy-unwind早失败flush、boundary samples的swept措辞、finally stage4摘要、fresh LateFact来源标注。总5项延期，非已闭合。

## 保管与容量

根18543/59927/22893/74303均terminal；74303 exit0只是五子阶段已结束。独立custody /root/v118_custody_audit ENDED，最后baf657/exit0，无handle/子会话；18个记录native已结束，KNIGHT Godot/dotnet/owner各0。之后才写本收尾文档。

- 45/45 manifest对字节一致；4518实体副本（源码4416/helper102）逐SHA匹配，54sealed binary一致，缺失/extra/fault0。
- 最终7案14份source清单同SHA；当前1844/C#892/Debug6一致、漂移0；helper联集12单版本且匹配。
- 18份user manifest同BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB，未读真实存档语义。
- source121F6E8E3EECDAEDA817ED11D82FD6B46984C5910C2065C03845B392880FBCD8；binary2D7589F0672903A68E75F2195DAE39BAE0CCBA9AD1639AC5A1BC1F1980D0424C；helperA1AC76A1CBEEAF2061E4787C7AA4191B2FE6831CCBD6640BC2EE66C2E403B8F1。
- HEAD/branch/index/owner/audio/productionClaw/Skitter锁定值不变，0源增删；本轮无新正式资产，资产表不新增正式项。
- 手动媒体Check仅一次，UTC2026-10-26T00:25:53.4101535，current860284/base3508/growth856776、10files、Notify=false。无新增过程媒体/20GiB档，不重建自动化。

下一步：主线需在真实预告结束前做合法方向输入，并以同Rust实际Guarded+Connected和回左样本证明交接，不能把Active寿命当ETA；探索防御须覆盖抵达Grip1/准备前，不盲在已受致伤后增加L/等待池。资源native owner、正式视听动作/相机、目标硬件/真人及最终授权交付仍未完成。原十一项目标不缩减、不标完成或blocked。
