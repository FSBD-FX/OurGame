# v313 · 空中往返齿轮运行内核

## 事实：已实现，尚未接入正式地图

完整 KNIGHT 原十一项目标保持 ACTIVE。本轮推进 T03 的直轨往返齿轮；已有摆锤不重复算新增齿轮，圆弧悬吊款也不冒称已制作。唯一工程为 `G:/knight/Knight`。仅无窗口构建与测试，未开游戏窗口、发送系统输入、付费生成、采集新原生崩溃、建自动化、改 Git、删除资料或写真实玩家存档。

设计与顺序分别在 `docs/superpowers/specs/2026-11-08-moving-gear-design.md`、`docs/superpowers/plans/2026-11-08-moving-gear.md`，逐次失败与判断保存在 `.superpowers/sdd/2026-11-08-moving-gear/progress.md`。Task 1 为内核；保存地图候选和通行验证属尚未开始的 Task 2，完整正常双路线和正式视听属后续门。此次没有改变正式 Basin 或父场景，因此玩家当前正式路线还没有这个新齿轮。

### 生产变化

- 新 `MovingGearMotionModel`：有界 double 相位，正弦平滑往返、解析速度；先约减巨大 delta 再相加。实际已走区间涵盖折返及完整周期，避免“首尾同点”把中途运动抹掉。travel 长度 (0,512]、period [1,20] 秒、峰速不超过600，设置在构造时校验、运行后不随导出值漂移。
- 新 `CanyonMovingGear : CanyonFixedGear`：非实体阻挡的 Area，不新增 World solid 或抓爪目标。按实际已走圆并集构造原生 capsule/circle 查询，不用 AABB 当伤害形状。本次后续候选半径24、travel(0,80)、period3.2、伤害2是工程初值，不是用户逐值批准，也没有改玩家 HP 或旧固定齿轮；新类仍继承基类的半径／伤害默认值，放图时须明确赋候选值。
- `CanyonFixedGear` 最小提取共享 `ProcessGearContacts`，保留原 overlap 入口；两类只走同一 cadence／GuardedDamage／单调身份消费。增加防回调重入门，reset 的 epoch 可撤销当前 batch，仍保留无敌帧与不可格挡伤害。
- 移动类要求固定 mounted 世界锚点、单位平移 basis、作者圆与原生 Area transform 一致。原生反例揭示节点赋值尚未提交 native pose；依据匹配本机 GodotSharp.xml，仅在新 Area Ready／运动／复位的位置更新后调用 `ForceUpdateTransform`，不去掉一致性门、不增等待、不改引擎策略。
- 仅释放自身创建的查询和两种查询 shape，借用场景 shape 不 Dispose。退出撤销授权；同实例重新入树重新 Ready，重绑起点且不扫迁移路径。
- `ResidentEnvironmentInventory` 先识别移动子型，登记整个未来运动 AABB，即使 staged／disabled 也不释放未来占用。这是保守 reservation，不是实际伤害区域或路径可达证明。

七份 C# 输入有变：新模型、新运行类、新 unit、新 integration；旧固定类、inventory、runner 三份修改。canonical 只新增严格 `thruster-moving-gear` 入口／输入保全／精确回执；仍为55秒既有 core 预算，拒绝混合旗标与命名豁免，未放宽三日志错误门。

## 原生证据

均为现有规范 runner 普通 headless，目录根 `G:/knight/验证证据/october13-stable-claw/`。失败原件均保留。

| 阶段 | 结果 | case |
|---|---|---|
| 缺模型 RED | 1903输入，exit1／529ms，精确 `MOVING_GEAR_MODEL_RED` | `thruster-moving-gear-20261108-075844-460-d6589ddf226a4a159e1dc12f25d0987d` |
| 模型 GREEN、缺运行类 RED | 1904输入，exit1／503ms，模型通过后精确 `MOVING_GEAR_RUNTIME_RED` | `thruster-moving-gear-20261108-080020-496-f81d9b35c7354c72b25a1b7420126264` |
| 第一轮运行 | 1905输入，exit1／1053ms，停在接触／重入控制 | `thruster-moving-gear-20261108-080516-289-b62da72d383a40869b4bd559933b7edb` |
| 只增标量复测 | exit1／1071ms；节点(180,464)、native(100,464)、hp5／seq0，严格门正确拒绝 | `thruster-moving-gear-20261108-080737-512-e39acf385db54a40bb14f8732a376867` |
| 原生变换提交后 | exit1／2537ms；八项运行通过，Fog测试错误强求 queued world 新快照完整而失败 | `thruster-moving-gear-20261108-080902-785-944a52a1899d4a64bc6f6e0621976d3f` |
| 最终新专项 | **严格 PASS／4026ms**，1905输入 | `thruster-moving-gear-20261108-081107-044-47ebdf7b6a8a46f99fa0be048ededcca` |
| 原机关兼容 | **严格 PASS／26271ms**，1905输入 | `recycling-traps-20261108-081404-277-9d3d9e10ec40420c8984bf815d18dc65` |
| 普通完整前段 | **严格 PASS／159237ms，44项**，1905输入 | `prologue-pre-20261108-081858-454-50d7512d53954ba5bfb5a54a28017952` |
| 普通完整后段 | **严格 PASS／192130ms，75项**，1905输入 | `prologue-post-20261108-082231-243-34ead2a2460d4656b791b015f747ca1d` |

Fog 的那次失败只修测试合同：queued world 的新 Capture 应不完整；再用上一有效 frame 验证 overlay 仍因排队祖先清空。不改生产 sampler／overlay，也没有忽略原错误。

最终专项包括：

- 模型的 literal 周期／解析速度／整周期扫掠／折返／负向及竖直／30、60、120分区／非法时间和构造／reset。
- 八组真实保存 Player：首尾均不碰但中途扫中一次；AABB圆角空区不伤；端点与扫掠同一消费、回调递归不再推进；实际伤害回调内reset；staged／暂停／hidden／禁处理／禁圆／错误basis／父锚点移动／shape变更拒绝；真键盘护盾仍被不可格挡伤害击败一次；引擎自动物理推进；同实例退出再入树。
- canonical 同层 Fog 夹具：真实移动、来源去重、墙／范围／hidden／disabled／暂停／staged／queued清空；整个原固定齿轮接触测试仍通过。

当前查询是在本帧真实 body 形状上检查齿轮已走路径，不承诺所有相对高速玩家运动的连续时间接触。Fog 夹具 `controller_saved_map=false`、`pixels=false`、`human=false`；这不是保存 controller 发现、正常像素或真人测试。

输入清单 SHA256：`58B74F1E87DDC8326589D01957921756E33A7FA3CDB7B38ACE66E70C720099E6`。

六二进制清单 SHA256：`6A259F3656677386E37A6F869ECB66FA348E4EFEECAAB643934F8FEA5C4B9801`。

最终四阶段前后输入／六产物清单全部相符，failure=null、guardFailures=[]。根逐项读回当前1905输入、6产物与helper也无漂移。普通post只有原合同四条负例警告：seamless 1、room-profile 1、flow-registry 2，无未解释ERROR；使用既有300000ms上限，不签旧180000ms或硬件性能。

## 推理、风险与下一步

一次独立增量源码审查 C0／I0／M2，不代签运行。两个 Minor 如实开放：尚无多 body-shape 的独立 native 去重反例；尚未分别测伤害 callback 内立即 queue／remove。已测 queued 是调用前、reentry 是调用后，不把它们扩写为上述 callback 分支的实测。

候选点位在 Basin UpperGearGalleryTwo，世界 A(3400,3550) 到 B(3400,3630)，半径24、周期3.2秒。两侧完整站位(3310,3640)／(3490,3640)，A端拟有30px头部净空。此为静态几何推理，尚未原生证明玩家可以择时自然往返，也未实测局部 y=-50 的相机／流送。当前不扩大 bounds、不改158原solid、原敌人域／9片／门或玩家数值来“凑通过”。

Task 2 将先构建独立 Basin 和 parent snapshot，重载真实 controller 后证明发现、运动后旧位置清空、相机、流送与自然通行，才晋升正式 leaf／parent并更新地图。已查出旧 Incinerator StageParent 最终会重绑正式 leaf，不能直接冒充未发布候选；改用现 BuildPrologueLevel.Build 路径字典＋SaveCandidate 的嵌入快照接口。

历史资源句柄根因、长期泄漏、全图相对运动碰撞、正式角色 A方向分层／动作设计／布料观感、正常视听／硬件／真人仍未验收。此次新增运行内核不能关闭 T03 整项或原十一项。

Task 1 内核及其规定的同版兼容门已完成，Task 2／3 未完成；不勾 T03 整项。README及稳定总待办同步记录此边界。

本轮无新图稿或实录；正常汇报前仅运行既有容量脚本一次，12,807,896字节、Notify=false，未达新增20GiB。不创建自动化、不删除资料。
