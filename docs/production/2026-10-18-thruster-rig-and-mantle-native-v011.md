# 短喷姿态与斗篷固定带 · 原生证据 v011

2026-10-18，19:53后记录。完整1–11目标仍ACTIVE，短喷计划Task6进行中；本报告不是正式试玩版号，不是整关或最终视听验收。继承[v010](2026-10-18-thruster-observation-and-teaching-native-v010.md)，历史原件不回写。

## 事实：新增实际消费者与修复

- Motor只读mobility事实沿既有Coordinator→Timeline→PlayerPresentationFrame→同一个Formal renderer消费。默认空的独立payload保留旧3／4参数构造、Locomotion／Action枚举和动画JSON。
- Timeline仅平滑视觉角，倾身依据实际横向速度，区分Burning／Coasting／Braking；没有第二速度、预算或余速计时器。Formal共同父节点只作最多0.2rad刚体旋转，不缩放图片、不旋转玩家碰撞。攻击／盾／受伤等action与死亡／恢复优先；真实mobility事实不因表现覆盖而伪造结束。
- 实际保存Player左右两向喷射→余速→反向制动、普通跑动制动不产生BurnId、J／L与死亡优先、暂停冻结通过；肩核喷口在同一发布采样距肩核8／7.9999995px。喷焰方向仍须未来独立消费锁定方向，不能从朝向或倾身倒推。
- 独立兼容检查暴露实际斗篷Draw多边形边界问题：点映射固定了Y≤−3的肩部，但跨带直边下端移动，Y=−5左边从−17偏到−17.467653。该断言在fixture调用ApplyPlayerFrame前直接读取局部mapper，不能归因新root旋转。
- 最小修正：四个填色多边形每条跨Y=−3边先插入精确原线性交点，再沿原Map变形，含填色闭合边。下摆原驱动／限幅／容差不变。原开放outline用同一个splitter但不插填色专属闭合边；本地绑定已核末bool是antialias而非closed，未新增原先不存在的半笔。原两mantle case与开放端点合同通过。

新增姿态是程序分层承载，不是已接入最终栅格角色素材。本轮没有新图片／声音生成、展示媒体或付费。

## 测试先行与诊断边界

- 缺typed rig真实RED：192418-ed14ec8e，engine1／561ms；先失败后实现。
- 192647-141cbbfb为CS1656编译失败，不冒称行为RED；已有静态Rotation helper遮蔽基类属性，随后仅明确base.Rotation。
- 192833-9a0a6a70／193124-12dfb3da的喷口合取失败经同步记录证实为测试混帧：发布时距离8，断言时人物已移动，旧nozzle对新core为10.709983。仅改同采样比较，不改8.1容差或帧限。rig初绿193237-8f4c04e5，2402ms。
- mantle独立真实RED：193958-a2b890e6，engine1／646ms、无Profile警告；最小边分割后初绿194137-9b7aaf46，628ms。未删断言或放宽容差。
- source-pre／instance-immediate／source-post诊断191812-85958813，engine1／90078ms：同一次PackedScene五个ID全有效且一致，仍旧Upper/profile/GetPath。GetState只读getter可能改变wrapper寿命，已显式标记；普通加载表达式不变，无额外Load、缓存变更、GC、跨await持有或帧等待。本次未复现185504的FATAL/null，不关闭它，也不原样盲跑取绿。

## 最终同冻结版本16门

原件根目录：`E:/OurGame/Knight/.evidence/october13-stable-claw`。全部source1692，共同source inventory SHA256 `9892150FEF96EB5E374BDDE32B96BBDB9E7270F75891FEA77A289D575E86165A`；fresh Rebuild均0警告／0错误，source/helper/user/binary四对库存全等、guards=[]。完整读取三运行sink后，结果为**12 GREEN／4严格FAIL**；不能按engine0或行为marker抹掉警告。

| Phase | Case | Engine exit / ms | 严格结果 |
| --- | --- | --- | --- |
| mantle | thruster-mantle-20261018-194253-707-55993a2a5ba0428ea889461033347d8d | 0 / 636 | GREEN |
| 原显示兼容子集 | thruster-rig-compatibility-20261018-194323-257-5de226ea55924becafb2493cd01c4635 | 0 / 695 | **FAIL，两Profile警告** |
| 实际typed rig | thruster-presentation-rig-20261018-194351-696-b4b272a3325b47e5b2c7caea23de1e72 | 0 / 2173 | GREEN |
| 观察模型 | thruster-presentation-model-20261018-194422-967-a9bcf05397ca4e4dad7f90c682a2268f | 0 / 528 | GREEN |
| 制动跨零 | thruster-brake-crossing-20261018-194452-821-ac42f09dab6f413581da9fb84c6eca04 | 0 / 539 | GREEN |
| 实际Player观察 | thruster-presentation-player-20261018-194523-734-fbd8077ecc6149d1be6857301165b48a | 0 / 3691 | GREEN |
| 教学／UI／首段出生 | thruster-teaching-20261018-194555-160-eb19a24d71f1478bb96609d74f4dbe9a | 0 / 1655 | **FAIL，退出2 ObjectDB** |
| 音乐自有wrapper | thruster-audio-owner-20261018-194624-063-e10ad543222c44d6aed511c6eda10145 | 0 / 1280 | GREEN |
| Player33＋scheduled | thruster-player-20261018-194655-183-c598f4fbec5c4a3489392982b085b362 | 0 / 22668 | GREEN |
| pulse | thruster-pulse-20261018-194749-689-85a6a58a38d84e2d85c1abe87292b061 | 0 / 547 | GREEN |
| keyboard | keyboard-20261018-194821-103-834ae5c388294399a496efffd0cd6a22 | 0 / 9742 | GREEN |
| actions | actions-20261018-194859-974-64e1d6b7174d4d36a0928e6e115c366d | 0 / 7211 | GREEN |
| 着陆重挂 | thruster-impact-reentry-20261018-194935-109-2ed71af51f974b848e3dbd4697084ab1 | 0 / 1279 | GREEN |
| 正式module | thruster-formal-module-20261018-195006-986-8e9c18f48ce24ca3962dff6747b0ff58 | 0 / 827 | GREEN |
| 广域pre | prologue-pre-20261018-195034-028-d0c07338b6c94459984db953b1e1e6fb | 1 / 88475 | **FAIL** |
| 广域post | prologue-post-20261018-195229-815-1134d9362c744ee9b89f4b28bb56130a | 1 / 8240 | **FAIL** |

完整batch终态exit4，随后才写文档。新phase无GC／cache／drain／时限／日志豁免；旧keyboard既有清理不搬入新phase。

## 推理：四个FAIL分别保留

1. 显示兼容：原timeline／pose／map／combat测试、11个原formal叶delegate和2个mantle行为完成，但原故意错误Profile负例发两警告，严格失败；不是正式视觉已损坏，也不是可直接过滤的无关日志。
2. 教学：中英字形／操作布局／暂停／真实正常出生卡均过，stderr退出2 ObjectDB，外部日志没有此警告。先前183731同类失败从未关闭，音乐wrapper专项通过不能认定已修这两对象。尚不知道具体身份；只读检查未发现恰好两件的可靠归属，不能据数量猜测。
3. pre：UpperPendulum lower_to_mid_swing sample.5仍(264.99634,160.6107)，frame149／budget150；另两Profile warning与四离树GetPath error。当前未出FATAL/null不关闭185504和历史七Handle／匿名析构。
4. post：BellChain真实落地仍(540.0704,1196.0139)，KillZone找不到../../../../../..的ILevelRecovery。保留夹具／正式接线区分，不能直接宣称所有生产恢复失败。

## 风险与下一步

- Task6尚缺持续世界喷焰、同完整身份专属voice、旧IsDashing→Dash cue迁移，低闪／暂停／重绑／退场的实际视听证明。现有dash.wav仅占位音色，未新生成。headless变换／Draw几何不等于像素、正常速度观感或真人手感。
- 教学下一诊断可在原teardown前只存节点／自建query的ulong ID，在既有Free／await后输出valid位图；不增加帧／Load／引用持有，不Dispose借用资源。全部失效也只排除具体身份，不关闭ObjectDB。
- Task7窄审已核：lower_to_mid旧分支不是已有fresh自动键盘白名单，关闭消费者后手动采样且旧预测硬60Hz。其zone中心Y132不是MidSwing顶156减72高胶囊半高后的站立中心120；后续应读Motor实际不可变motion快照，迁移真实键盘与release预测，不改150tick、落区或危险。600px/s neutral15tick@60Hz：当前drag1.6约vx402.1920／横移121.9889，旧2800则0／59.3333，可作独立负控。该调查不是已实现／已通过路线。
- 完整1–11仍包括正式0／50新常驻、24canonical、正式127／候选132整图、机关、最终美术／UI／音频／演出、正常双路线、旧档／真人／实际显示60FPS及Windows ZIP／GitHub归档。没有缩减、发布或完成声明。
- 正式Player仍v009标准builder原字节，未重写tscn。没有新媒体／付费／Git／自动化／OS键鼠；真实用户数据仅库存哈希。旧版本与失败原件保留。

持续状态：`.superpowers/sdd/2026-10-18-thruster-player-integration/progress.md`；完整清单：[总待办v001](2026-10-18-project-remaining-backlog-v001.md)。
