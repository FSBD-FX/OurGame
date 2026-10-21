# 常驻子型配置资源／重新装载接入 v001

2026-10-17，Asia/Shanghai。权威 checkout：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。完整目标 ACTIVE；本轮为制作进展，不是整关、正式美术或50入图验收。

## 事实、判断与风险

事实：现有 `ConfigureResidence` 只在挂树前消费普通 C# record／私有字段；这些字段不会因为 Pack 场景而自动保存。此前没有可保存的常驻家族 Resource，也没有 `ConfigureFamilyProfile` 入口。上一轮只读调查因此改变了下一实施步骤；不是画面改善。用户再次确认测试时使用其他窗口，继续无窗口／不抢焦点，不据此排除所有输入问题。

判断：先补现有计划要求的身份／policy 持久化交接，再由后续叶 builder 保存实际绑定，不能把“Configure 后 Pack”、改节点名称或旧 probe 路径当作正式家族身份。

风险：Resource 能落盘不代表叶场景已引用它，更不代表运行时已经自动恢复或50个点位可用。本轮明确不自动挂树、不生成新视觉、不指定正式平衡、不将 `SceneState` 改为已实现。原资源失败、正常双路、旧档、60帧、真人及最终视听门继续 OPEN。

## 实际改动

- 新 `ResidentSubtypeProfile : Resource`：导出 subtype、family、resident role、九种真实 body/weapon carrier、骨骸姿态弱点及幽灵有限显隐时间。
- `Bind` 对规范 subtype 与具体 slot 做一致性检查；保留原 StableId／Placement，生成不可变 combat 快照，不让可变 Resource 后续改值污染已交接 policy。不接受未放置身份、冲突 policy、混合家族字段或无效时长。
- `ConfigureActor` 验实际九类 actor、off-tree／非退休图，在挂树前调用原 `IResidentEnemyAgent.ConfigureResidence`；保留 actor 自有身体／路径验证与只配置一次合同。不会给四种不同枪械仅凭同为 Turret 就互换。
- 规范 catalog 增加明确 Carrier 元数据；旧26／新增50／24子型分配不变。全部50 Placement仍null，24正式 Scene仍Unimplemented/null，正式新增仍0。
- 新 `ResidentSubtypeProfileTests` 独立 phase。原 allocation 恢复纯名册检查；资源／实际 off-tree actor 消费不混入它的严格绿结果。新增两份实际 C#源，实际 source/resource 库存及正常路线精确 guard 为1672。

没有改现有 actor 的物理、战斗时长、伤害、场景、资产或存档。Resource 与消费者是新的制作接入口，尚无正式叶／manager 自动装载消费者。

## 先失败的三步

统一 evidence 根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。以下均严格 FAIL，不接受 named RED／警告豁免。它们验证新增合同的缺失，不冒称修复了一项已正式发布的场景丢配置故障。

| 完整案例目录 | Fresh Rebuild ms | Engine ms／exit | 实际反例 |
| --- | ---: | --- | --- |
| resident-allocation-20261017-021909-878-3a6b3bf3a145451e87b19200c645afc6 | 25341 | 515／1 | 新 Resource 骨架的普通属性未序列化，rock-wanderer 重载 ID为空 |
| resident-allocation-20261017-022043-394-f4eadf4a764545ebbbeeb36b5568efd8 | 24611 | 595／1 | 导出字段后24磁盘往返已到；Bind原样返回slot，Bone policy仍null |
| resident-allocation-20261017-022236-420-b950add37da744f28cd0cf96525e1cbf | 25851 | 940／1 | 24 bind已到；空Configure未提交真实actor的一次配置；另报4Shape RID |

三步依次只补真实字段持久化、不可变 policy 绑定和原 actor 接入；未改失败预期、添加GC／等待／资源缓存策略或 Dispose 借用形状。

## 最终同版六门：3 GREEN／3 FAIL

| 完整案例目录 | Build ms | Engine ms／exit | 严格结果 |
| --- | ---: | --- | --- |
| resident-subtype-profile-20261017-022455-735-d02dbba95b6d40b0952e33016ded54fe | 26961 | 900／0 | FAIL：24保存重载、24绑定、24实际保存actor挂树前消费及14错误绑定均符合，但stdout **31 Shape RID ERROR** |
| resident-allocation-20261017-022637-038-f7e0f30d7aba43428f04335dd38a5388 | 25429 | 628／0 | GREEN：原50名册／17负控，未置50／未就绪50／新增0 |
| keyboard-20261017-022708-636-7218e336f166488b85c70661c7c66a23 | 25393 | 9801／0 | GREEN；引擎合成输入，不是真人OS／前台路线 |
| actions-20261017-022749-727-8e1f66cf48da48bd8545f576870e4f19 | 24676 | 7212／0 | GREEN |
| prologue-pre-20261017-022827-586-fb1fd3b13c0c4e1d95555610c33fd0dc | 25023 | 89803／1 | FAIL：旧 UpperPendulum/lower_to_mid_swing sample=.5，final=(264.99634,160.6107)，149/150；另2条PresentationProfile警告 |
| prologue-post-20261017-023029-208-7d6010deb88645e69ccc5b3a5ea752da | 27478 | 8917／1 | FAIL：BellChainWaitForFloor final=(540.0704,1196.0139)；一条六祖先ILevelRecovery解析ERROR，外部log重复，不算多项独立错误 |

最终profile证明的是 `mounted=false/formal_scenes=0/spawned=0/native_motion=false`。磁盘实际24份非空tres、7245bytes、24独立SubtypeId，子型family文件为4/6/6/4/4；不是50人口的8/12/10/10/10。产物位于该profile案例的 `storage/resident-subtype-profiles/`，未发布入项目assets或正式scene。

14负控为：slot subtype/family/role错误、catalog carrier错误、Placement缺失、调用者policy冲突、NaN及零显形时长、Bone/Ghost混合、未定义family、错误实际actor、同Turret但错枪、错误Bone姿态、普通家族偷带Ghost字段。错枪拒绝后还有真实Scatter一次合法绑定正控。测试时长仅序列化／消费fixture值，不是正式平衡。

三原始sink均核对。31 RID只在profile stdout；其stderr为空、external无该诊断，仍严格否决。pre两警告／Upper断言及post Recovery／落地断言在stderr和external保留。当前没有FATAL/Handle不能关闭历史问题，也不能根据31这个数量推断资源身份或因果。串行外层exit0只表示所有case owner结束。

## 保管链与版本

全部9次 fresh Rebuild均0警告／0错误，build cap180000ms；focused engine55000ms、完整pre/post180000ms，全部无timeout／无guard failure。每案source1672、protected109、binary6；helper一般9，pre10。所有消费者终态后才更新文档。真实userdata只hash，不读取语义；无 Git、付费、上传、自动化、前台按键、新媒体或终态声明。

独立只读保管链核对：全部45／45 before/after pairs一致、54份二进制副本及2718份选中源码快照hash匹配；新增Profile／tests与definition／catalog／runner的各轮选中副本齐全。这是证据保管核对，不是代码验收或whole-plan review，不覆盖任何资源FAIL。

最终六门共同manifest SHA256：

- source：`A6C4B299B6D3B7ED7A7CAEA13302A401A9D3EA34CBA056F078B92D2CC304BC65`
- binary6：`C55D85A974D800305EF00567A0EF55DB58ADE1340C3D982A876208341F86202A`
- helper9：`CAFABC3B576D0F9FF8E82154F9D142179D8790055B3F39316E1EA246950155D8`
- pre helper10：`35789E7EA9FEA268F62776B5179F4BFF311233D705EFC6F662EA0D62711483FE`
- protected109：`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`

当前主要源码SHA256：Profile `CF69FB7DEB3EAA2DCC061E2425DE4620E942D95A670F61C5D45F98EBF723AE21`；definition `E4F6B9456C0DC9892CB4765DFB3B2398A4A7F56A59FC0CB9329BD40BD3BF15BC`；catalog `66B5B9A2FF9E69E48E86902A6B797204026FF08401D22A91FA9F8ED007C22D21`；profile tests `54BB9B207E8BEAB6F46267A0255315865C8198AA4210952917A9809FF485001B`；runner `F0054DDBCC1D0980096CAD14BA9E5955DEAF1C694235E6324985BA88ED3791A2`；owner `A236B44A250F4C6047317B5E65081B5B5D06CCE615E7F951FC4DE3910774A1FD`。

## 下一实际入口与未完成范围

1. 叶builder在实际保存PackedScene中绑定本Resource；保存重载后验证实际引用与9种carrier，并由明确装载者在AddChild前消费。不能在builder临时Configure后期待私有数据被Pack，不给已有Formal叠加第二套Renderer。家族视觉未做／未验收时仍显式placeholder，不改名冒充正式美术；付费生成仍需另获授权。
2. 验实际挂树后的24子型身份／role／Bone-Ghost行为；再全部50真实身体、活动、攻击／返航、玩家接近与保护区域、世界生命周期manager、旧档／回返／死亡规则。不能重复本轮空字段检查来替代这些制作。
3. 31RID及历史资源故障仍需有区分力诊断，不做计数彩票／新GC／借用Dispose／超时放宽／日志豁免。旧Upper/BellChain失败仍OPEN；完整Main/Explore、实体出口、正常显示60帧、录像审阅及真人反馈未由本轮证明。

Task1最终50、整个Task2、Task3/4都未完成；不运行task-done、不做最终whole-plan review、不勾完成或缩小目标。
