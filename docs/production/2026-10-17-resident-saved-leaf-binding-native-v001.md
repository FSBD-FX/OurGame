# 24常驻子型：实际叶场景绑定与原生初始化 v001

2026-10-17，Asia/Shanghai。权威checkout：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。完整目标ACTIVE；本轮有实际场景制作／运行证据，但不是正式美术、全50入图或整关交付。

## 事实、判断、风险

事实：上一轮Resource已能保存／重载，但没有实际叶场景引用，也没有装载者在Ready前应用它。本轮新增真实builder、导出绑定节点与显式装载器；24候选叶由旧真实actor图另存，保存后重载，再在独立World2D实际Ready。骨骸／幽灵policy不是只留在文件里。

判断：沿现有 `BuildBasinQualitySample.BuildSampleLaborer` 模式，只清最外层SceneFilePath、保留嵌套visual，增加actor直接子绑定节点。**这是另存真实actor图，不是保留outer PackedScene继承引用。** 不向VisualRoot叠加renderer，也不临时Configure后期待私有字段被Pack。

风险：旧外观仍是明确placeholder；候选文件只在隔离storage，canonical24Scene仍Unimplemented/null、50Placement仍null、实际地图新增0。Resource错误和整关失败未解；候选Ready／单项policy生效不能替代全部移动、攻击、返航、50点位、玩家通路、manager及最终感官验收。

## 实际新增

- `ResidentSceneBinding : Node` 导出Profile和PresentationState，只存数据，无自动Ready配置。显式 `Instantiate` 验实际PackedScene资源路径、非probe别名、状态、一份直接绑定、真实profile；占位默认拒绝，只有显式development admission允许。调用已有Profile.ConfigureActor后才返回未挂树actor；失败释放它拥有的新root，不释放借用形状。
- `BuildResidentSubtypeScenes` 为build-only SceneTree／静态BuildInto：读取显式提供的24profile文件，拒绝不匹配输入；仅写已存在、空、非reparse且不交叉project/userdata祖先的隔离output root。Pack节点数检查后，再Load验证导出属性及placeholder状态。无游戏逻辑／新平衡常数／美术生成。独立CLI入口尚未单独执行；实测路径为canonical runner调用静态BuildInto。
- `ResidentSceneBindingTests`：实际保存24positive叶＋24输入profile，另保存missing／duplicate两份negative叶；24actor经过factory配置、原生Ready、睡眠／唤醒及Health保持；空中actor确实移动而非默认Hover。使用测试fixture时长，不是正式平衡。
- 新增三份实际C#源，source/resource库存及正常路线精确guard为1675；canonical owner独立scene-binding phase、原始日志否决及隔离保管链不减。

未修改原actor物理／战斗时长／伤害／旧场景／VisualRoot／存档；旧26与新50／24子型／Bone10／Ghost10分配不变。无新付费、美术、媒体、上传、Git或自动化；未抢用户键鼠。

## 两个真实先失败阶段

所有案例根为 `E:/OurGame/Knight/.evidence/october13-stable-claw`；均保留严格FAIL，不接受named RED。

| 完整案例目录 | Build ms | Engine ms／exit | 反例 |
| --- | ---: | --- | --- |
| resident-scene-binding-20261017-025023-489-426073a1706d439ba1a70dc1376964b8 | 26618 | 898／1 | 24叶文件存在，但重载rock-wanderer无导出Profile；stdout另35Shape RID |
| resident-scene-binding-20261017-025234-267-92fd41d45e744ff987a15fda942cb5cc | 22275 | 863／1 | 24导出Profile往返已到，但loader没有在Ready前提交真实actor配置；stdout另122Shape RID |

第一阶段之后只补builder绑定和重载检查；第二阶段之后补显式loader。测试源码三轮未变，未调时长／距离／预期去变绿；没有新的GC、cache、借用Dispose、drain、超时或日志豁免。

## 最终同版七门：3 GREEN／4 FAIL

| 完整案例目录 | Build ms | Engine ms／exit | 严格结果 |
| --- | ---: | --- | --- |
| resident-scene-binding-20261017-025529-865-162fcc690cdd4514b736440178a71ad6 | 28198 | 5942／0 | FAIL：24保存绑定／24原生Ready／7negative符合，但stdout130Shape RID ERROR |
| resident-subtype-profile-20261017-025838-767-e69fc06e6c7c447f9cfeb9175961dfab | 25050 | 811／0 | FAIL：原24Resource／14negative符合，但stdout31Shape RID ERROR |
| resident-allocation-20261017-025910-588-5cf5470716ff40cb8e607fe262355e0c | 24323 | 538／0 | GREEN：原名册／17负控，未置50、正式新增0 |
| keyboard-20261017-025940-975-d9807bbbe6d1415ebc7bd8c8a4535847 | 23870 | 9729／0 | GREEN；引擎合成输入，不是OS真人／前台路线 |
| actions-20261017-030019-682-176b878b294d4a19b0832fab5b492aae | 23820 | 6976／0 | GREEN |
| prologue-pre-20261017-030056-030-9721b59ee40d43f9b7d8107097cc6f7f | 22678 | 88376／1 | FAIL：旧UpperPendulum/lower_to_mid_swing sample=.5，final=(264.99634,160.6107)，149/150；两条PresentationProfile WARNING |
| prologue-post-20261017-030252-504-4402b2aa82644679b9ec76344436d0ba | 28544 | 8435／1 | FAIL：BellChainWaitForFloor final=(540.0704,1196.0139)，一条六祖先ILevelRecovery ERROR；external是重复记录 |

全部9案fresh Rebuild为0警告／0错误；build cap180000ms，focused engine55000ms／完整pre-post180000ms；无timeout／guard failure。串行外层exit0只表示全部owner终态，不覆盖case结果。消费者全部结束后才编辑文档；真实userdata109只hash不读语义且保持一致。

### 新原生证据的准确边界

- 24实际actor确实Ready于独立World2D，之后sleep/wake不回血；9carrier不是新名字或新实例ID充数。此处stable slot仅fixture；不是地图50出生史。
- `lost-echo`：Ready后真实Hurtbox API请求被Hidden拒绝；经过80真实PhysicsFrame后，Manifest请求到原Health并扣1。**native_area=false**：不是AreaEntered碰撞、OS按键或K→J接触证明。
- `bone-charger`：Ready后中性姿态真实guard、HP未变。**full_counter=false**：不据此声明完整骨骸机械爪反击／所有姿态已验收。
- 7负控：默认拒绝placeholder、Unimplemented、probe/path别名、不匹配family、未放置slot、实际保存missing及duplicate绑定；同一loader拒绝，不靠字段grep模拟。
- `full_motion=false/map_spawned=0/formal_visuals=0/full_population=false`始终明确；完整角色战斗、路线和显示60帧未由本阶段证明。

### 实际产物位置

最终scene-binding案例的 `storage/leaf-source-profiles/`：24非空tres、7245bytes；`storage/resident-leaves/`：24positive tscn、100942bytes，另2份明确negative tscn。每个positive为真实CharacterBody2D图、单一ResidentBinding及对应profile引用，保留嵌套visual引用。这些是隔离候选产物，未发布到project scene/assets或canonical ScenePath，不是最终美术。

## 资源调查与保管链

独立只读源码调查：9carrier的Instantiate／Configure-before-Ready路径没有可达的私有Shape2D创建；Configure只借用保存的body形状并建托管模型。ranged私有capsule只在Ready创建并在ExitTree释放；Sentry两circle只在发弹创建并有退出／构造失败释放。**未找到据此可修的挂树前私有形状漏释放合同**，因此没有把借用场景形状改为actor-owned Dispose。

31、35、122、130的数量不能推出资源身份或原因；stage改变了装载／保存／native分配量。原始3sink保留：new leaf／profile RID只在stdout也严格否决；pre/post警告／断言在stderr与external保留。当前无FATAL/Handle不关闭历史异常。

最终七门共同manifest SHA256：source `7D6D8D90C37DD5756EC2506FA682DDF13E8685FEDA4B0F9FBBDE233EA59D8950`；binary6 `3F1AAC2CC1280B184B81900DA79E788857D7D72C6E965D702FAB9E8F2396B186`；helper9 `B701279CE6B2F853B7195833934B4EEE8B556613548FC076522C8BF8C44DC4FD`；pre helper10 `439291FCD3258AF41D15D330D8E8C5528B301A81FDBDB4C3B61760F57CBD4989`。每案1675source／109protected／6binary，before-after保管链一致。

独立只读保管链核对：全部9案45／45 pairs一致、54份二进制副本及2748份选中源码副本匹配；新增binding／builder／test双侧副本齐全。两个RED及最终leaf共888份选中源码副本，后续6案1860份。核对是事实保管，不构成代码评审／验收，不覆盖130／31 RID失败。

主要源码SHA256：binding `E7C8463EB49C0C70A1B35DAFDB93897BD2DEEB780342C23721245600331D0ED9`；builder `1BC42825BEDE8AD434286F4754E4BEC31D3E5B41C6090034D5C57E675855D81F`；tests `F4585BECEC36363BCDD0CE60C08E3DCA358B624073B69C1495B89F3C0834188C`；runner `FC648E560CEB9C166705B8EDA74A5EDA2D589CD0AA263BF7B3153614346BE50C`；owner `A097AD2E604A879385CFE4C688EF50E91F1A8E392FD5FDC269FB8FDDCD5F6B06`。

## 下一实际入口

已有Resource保存／叶引用／显式preReady装载与24初始化切片，不要反复重做空字段或24挂树检查充当下一制作。下一把真实候选叶接入有精确库存／引用保管的本地prototype路径，保持placeholder与严格失败分类；再完成全部50实际身体／活动／攻击／返航／玩家接近和保护区点位、缺口侧龛及world-lifetime manager。当前41候选／9未解未由此改变，不能把null变原点或缩减数量。

canonical正式族系／美术／完整50仍未验收；Task1最终50、整个Task2、Task3/4均OPEN，不勾完成／task-done／whole-plan review。资源及旧Upper/BellChain失败继续实查；正常Main/Explore同角色至Boss及支撑出口、旧档／回返、稳定显示60帧、最终美术／音效、归档短录像审阅与真人反馈全部保留。目标ACTIVE，没有付费人数承诺。
