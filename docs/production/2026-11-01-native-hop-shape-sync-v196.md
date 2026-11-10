# v196：真实移动后同步受击区域；资源退出错误仍未闭合

## 事实

仅KNIGHT `G:/knight/Knight`，原十一项目标ACTIVE。此次不是继续盲调K或跳跃时机，而是用保存Player、真实Space输入与原生查询校准v195的一帧差。没有修改跳跃/重力/伤害/敌人/地图/原Main合同或美术。

两个独立World2D fixture各自然落稳、真实一次起跳、采8个同physics的Motor前后样本并自然落地。探针用当前实际Hurtbox胶囊：新占据的顶部内1px应命中，旧底部已离开的内1px不应命中；步移>2px，探针半径.25，与World地面分离。另有深处确切Hurtbox positive和远处negative，不以预测器制造预期值。只在诊断force变体调用显式刷新；原节点姿态、HP、速度不写入。

实际delta均为1/60，Motor priority0、观察前−1/后+1。原状态8/8样本中，CPU变换已上移，PhysicsServer Area与原生Sweep仍使用上一帧受击位置：新点不命中、旧点仍命中。刷新角色根节点也8/8失败；仅刷新Hurtbox本身后8/8全部正确。不是从公式或仅CPU/native矩阵相等推断，而有精确目标原生查询对照。

局部生产修正只在`PlayerMotor`：Ready缓存可选已有Hurtbox，真实MoveAndSlide后对仍有效/在树/未queued的Hurtbox调用ForceUpdateTransform，再检查运动版本、所有权、暂停和生命边界。无测试开关/测试专用生产API，不改body位置或物理配置；缓存像既有body/input/health一样跨原树驻留保留。没有对所有碰撞系统、同优先级弹体顺序或完整Main作普遍认证。

修正后两变体16/16查询正确，原11个geometry行为案例保留；但该phase退出仍有一次“3个P12GodotShape2D RID泄漏”的ERROR汇总，所以严格结果仍FAIL。engine0或成功marker不是严格通过。

## 本轮案例

全部原canonical、显式SourceRows1872；build180s，native geometry/actions/models55s，Main600s，pre180s/post300s。每案串行且源码/helpers冻结，ExpectedFailure空，失败原样保留，无重跑筛绿。

| 案尾/阶段 | build ms / exit | native ms / exit | 严格结果 |
|---|---:|---:|---|
| afb78f0d 根节点刷新对照 | 28310/0 | 2944/1 | FAIL，根刷新也8/8错位；3shape退出ERROR另计 |
| b3975ac8 Hurtbox刷新对照 | 27168/0 | 2927/1 | baseline8错/force0错，真实baseline RED；仍3shape ERROR |
| aefd8e99 断言移至外层对照 | 28353/0 | 2899/1 | 同baseline RED，移出async局部未去掉3shape ERROR |
| 784ae8f4 生产同步修正 | 28237/0 | 2900/0 | 16行为GREEN，但3shape退出ERROR，严格FAIL |
| 716b0191 固定物理释放边界 | 27634/0 | 3006/0 | 16行为保持；仍3shape ERROR，严格FAIL |
| f2ffbd5c 当前actions | 28384/0 | 9488/0 | PASS |
| fe8f662c 当前爪模型子集 | 26223/0 | 505/0 | PASS，不是全部模型 |
| 981fe983 当前Main | 26843/0 | 128170/1 | FAIL，6749第13弹实际HP2→1，原首弹通过不续签 |
| 0f06e4ef 当前pre | 27372/0 | 172026/0 | 原180s内PASS，非性能或历史资源根因认证 |
| 43730922 当前post | 29919/0 | 207142/0 | 原300s内PASS，75案例/owner8/仅4限定WARNING |
| 526cdd77 当前Explore | 27297/0 | 324731/0 | 原600s内PASS，标题同角色→Boss→实体支撑出口，零恢复 |

前五个Native案都无超时/guard差异；前三个有断言错误，后两engine0仍被原严格入口拒绝。3shape是每案一次汇总，不能计成三条独立ERROR，更不能用external镜像重复累计。

完整案名位于`G:/knight/验证证据/october13-stable-claw/`：

- `ranged-native-geometry-20261101-095714-989-afb78f0da1b24c1c8b6b7aba08461892`
- `ranged-native-geometry-20261101-095946-137-b3975ac8ce884617a655c203c151ffab`
- `ranged-native-geometry-20261101-100421-517-aefd8e9902344634a9de7f74c1fb65c7`
- `ranged-native-geometry-20261101-100658-187-784ae8f413534cfcbdae3a1bad11a600`
- `ranged-native-geometry-20261101-101153-941-716b01911a3b45c88a8bc81f5dc29475`
- `actions-20261101-101355-994-f2ffbd5c2f08446cb309946b92ceb029`
- `models-20261101-101624-128-fe8f662c3c66455991a4f372e4dc02e7`
- `thruster-route-main-diagnostic-20261101-101810-585-981fe98338db4b6a82000fe24554aef6`
- `prologue-pre-20261101-102414-362-0f06e4efae194a43a21e40991077fff1`
- `prologue-post-20261101-102850-404-437309221d88451d9172c24011c31d50`
- `thruster-route-exploration-diagnostic-20261101-103418-612-526cdd772bea491abd86f529cd3a4f99`

## 推理与风险

局部Root Body刷新无效、Area自身刷新有效，符合已安装GodotSharp.xml对CanvasItem累积变换及ForceUpdateTransform的说明；但只证明该保存Player/该post-Motor查询边界，不能泛称引擎bug或一帧差的全部来源。真实弹体组内顺序、相对运动扫掠与完整三弹仍需真实整线证据，不据新查询结果放宽原Hop/HP门。

资源方面：Player保存场景恰有3个borrowed shape（受击胶囊/攻击矩形/身体胶囊），数量吻合是候选，不是RID身份或持有根证据。新floor由fixture登记owned并Dispose；没有Dispose共享Player形状。断言等校准async返回纯int结果后再抛，以及节点释放/资源释放两处各固定等1个PhysicsFrame，都没有去掉3shape错误；两项假设未确认，不继续堆GC/延时直到安静。这些只改私有fixture释放边界，原Main900循环/预算未变。

当前Main实际6715腾空、6735首次非负Vy，6749被固定resident `4906496821789` 的shot13击中，HP2→1。Hop终止stage3、raw Space1、shots2、passed0、faults2，无恢复；不能沿用v195首弹通过，也不能只凭更早受伤就认定修复错误或正确的全部几何原因。当前actions、模型子集及原预算pre/post通过，不表示全套、资源或通关完成。post原75逐项/owner8节点1→1/退出标记完整，4条限定警告均为既有负控范围、无ERROR/FATAL，不认证历史资源原因或性能。

同版完整Explore324731ms严格PASS：结果JSON为startedFromTitle=true、persistentPlayer=true、同player47546631684、BossDefeated/SafeExitReached=true、flowCompleted、recovery0/0。真实键盘J99/K89/L59，mouse/legacy事件0；Boss死后走到完整支撑出口，完成后D保持60帧、完成信号/次数各1、完成存档bytes不变。109真实默认存档保护条目不变。artifact自身明确proofKind=automated、syntheticFixture=false，但humanValidated/operationCoverageComplete/fullGoalAccepted均false；不能将它翻译为正式正常显示/全操作/性能/真人或完整成品验收。

本轮11次build均成功、无编译警告/错误；11次native严格5PASS/6FAIL。最终同源7阶段（geometry/actions/models/Main/pre/post/Explore）为5PASS/2FAIL，失败仍是geometry的3shape退出ERROR与Main实际受伤。原十一项、76敌人/Boss/NPC、正式美术/动作/UI音频、正常显示/硬件/真人及真实存档兼容验收仍OPEN。

只读输入事件链追查没有发现正常退出漏解绑：PlayerInputReader没有外部事件订阅；driver.Dispose归还静态Current，root级输入observer.Stop对原缓存Window解绑FocusEntered/Exited并关闭输入/QueueFree。局部driver/observer引用环不能单独证明泄漏。Stop在第一次解绑抛异常时可能跳过第二次，是未在本案发生的未来候选，不把它当3RID原因或本轮新修复。

## 源码、审查与保管

本轮只改生产Motor、既有Geometry测试CS，以及canonical中该phase归档依赖名单（原有10个文件，已存在者去重）；没有新增CS/UID、入口权限或预算。当前冻结：

- Motor `CB65CA765DDD2803503DC003D7C146E4110597A556148DC49513C6A1F1AAF14A`
- Geometry `37E0B5C602BFED16BE042E02E632B9752E6573ACC3035B44FF50C9F2BB0137A2`
- canonical `7A79C29AEBC02A9226B5ACA987D1C7B26FAF5CE5A4AF534CFC12E2123669BE19`
- Main仍v195 `539BF8D40271B1739DABD05CF5A9C4D40671331314837FCC793D2A2BA724C7D5`

Motor物理基线为aefd8e99案before副本，SHA9161427BF1548B0D19F0D850DC45D137A3B4A5215B6EF7C70010E93FF9B1C53B。Geometry原文件未跟踪，编辑前只记SHA；随后仅逆转本轮已知新增块/两条清理等待/import并校正新增空行，机械重建结果与原SHA **B958E66DFF0959E36BC3EAF072D2DD314DC7DAAAD4381164C7461FC702A9C861**完全一致，保存为本计划workspace中的`baseline-RangedAttackGeometryRuntimeTests.cs.txt`。这是验证过的重建，不冒认原已归档实体。

唯一fresh最终审查C0/I0/M1，无C/I修补、无二审；允许保留局部delta，不代表release或完整验收。延期M1：当前保存场景实际仍绑定GuardedDamage，但新setup未独立断言ResolveDamageReceiver等于该节点，未来改成Health时本测试可能漏报；本轮不修Minor。

| 审查拒判事项 | root裁决 | 错判成本 |
|---|---|---|
| 1.3shape资源身份/持有根 | 数量吻合仅候选；下一先RID/引用身份证据，不Dispose共享shape | 错归因或破坏共享素材 |
| 2.资源释放是否完成 | 两项释放实验未消除错误，严格FAIL保留；不称退出清洁 | 虚报稳定或掩盖泄漏 |
| 3.弹体组内顺序/相对扫掠/下降横移 | 当前只认上升post-Motor零motion查询；完整运动场景继续待验 | 将局部同步误当全面碰撞安全 |
| 4.Main更早受伤的原因 | 保留实际13弹命中与整线FAIL，不自动归因为新错或旧错被修；不据此回退取绿 | 修错机制或掩盖真实失败 |
| 5.pre/post/全模型/硬件真人真档发布 | root只认各当前终态，旧案不续签；全部原十一项/正式门保持OPEN | 错误正式交付 |

独立保管全部闭合：11终结案5PASS/6strictFAIL；55对manifest、5496份所选source副本、124份helper副本、66 bin逐SHA匹配。10:43:51 +08当前1872 source/6 bin/本案9 helpers匹配最终Explore；10:43:59按创建时间/路径核对22个owned身份全部终结，共21个PID，20个不存在，50152已复用为另一Node进程。56780的两个不同创建时间build分别核算；未操作复用进程。没有运行中native或计划重启。

末版sourceManifest=`C4212399B6D418A4B2DADFBEBBADB97B22B3F0787F1225BBF78AEC758DD98CAB`，binaryManifest=`4C241606535509AE0D1563CB8F9F54987A920CA38725894E323D29F1791F51AC`，DLL=`349084A657E82B36AAB79E7A23D312912C38540F0E03FEA4FD3B505A2BA7912E`。最终helper9项manifest=`2FEB90DD967F9BADBFE51584BC475689479C7233828C53636A6917AD0FA9834D`；post helper10项manifest=`C5EF45802948AA866E2659414BDBE4BD8D22B0D980D459CAA292638C213429E7`。Explore case保存SHA=`98D0BA53C67807B685D386DF0889865F3EFE30A94E10B73589A56E2E6946FF77`，完成/保持两端一致；OS写入次数不可用，不能说“只写一次”。

02:42:51 UTC手动媒体检查7427750bytes/86files、Notify=false，无新媒体/档位；HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index SHA21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。一次只读JSON投影因Select-Object重复大小写别名失败，已按实际字段修正读取；不是原生/游戏测试失败，未改变artifact。

下一自动试玩战术的用户选择已异步询问：是否保留同阶段零伤格挡/左转证据，但允许按枪手预警择时单跳，仍不改数值、HP、三弹及无恢复要求。未收到选择前不改该合同；当前同版探索与其他安全工作照常继续，不将问题变成停工或目标完成。

计划/裁决与原始结果在`.superpowers/sdd/2026-11-01-native-hop-shape-sync/progress.md`。只隔离headless/内部键盘/本项目源码与记录，无GUI、OS输入、真实存档写入、付费生成、额外采集、自动化、Git写入/发布或删除；无新媒体。
