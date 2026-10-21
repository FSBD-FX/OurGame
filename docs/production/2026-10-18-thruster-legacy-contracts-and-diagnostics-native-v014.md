# 短喷回归：旧教学入口与拒绝诊断 · 原生证据 v014

2026-10-18，全部本轮 owned 消费者终态后记录。完整1–11工具目标保持 ACTIVE、范围不减；短喷整合 Task6/7 仍进行中。本文件是开发证据，不是正式游玩包、全关完成或真人体验认证。承接[v013](2026-10-18-thruster-route-forecast-native-v013.md)，不覆盖旧证据。

## 事实：本轮实际修改

- Upper 的 mid_to_high_swing 原 fresh .5 样本接入已验证的真实键盘／离散预测驱动。保留 HighSwing 原几何、原 launch/landing zone、150 tick、HP及碰撞；一次普通Space、两次K，真实同cast到达后取消，完整胶囊／精确原生上接触落入原区，零Boost/Shift/恢复。first GREEN为73/150tick；后续广域样本也过，不把落地时仍有横速当成停稳。
- PresentationProfile 增加只含枚举与字符串的拒绝诊断。默认生产wrapper和显式out重载共用唯一私有解析核心；正式运行仍在原拒绝点报警、同步Free错误根，原异常继续传播。测试保留无效visual回退的身份／可见性及无效Hurt cue的false/null，改为明确断言拒绝原因，并补生产fallback和无任何visual的异常路径；没有静音flag、第二次实例化或日志豁免。
- Motor只改诊断上下文：在树内才取真实GetPath，离树使用明确detached Name/type；有Tuning.ResourcePath仍优先资源路径。原四个离树ApplyPull fixture不再因生成诊断文字调用非法路径；五项原交互／620、540速度合同及两个非法Tuning/Module拒绝均保留。没有离树提前return或改变运动初始化。
- 将旧GripStone三项、墙／天花板／坠落自救三项、上行3×3视线矩阵绑定到明确的legacy保存场景。typed-terrain入口独立pin六矩形、20/36胶囊、one-way及16px margin、初始无实体重叠、GripStone hit-point材质；绝不拿V2的Ice FrozenCeiling替代旧GripStone FrostVentCeiling。
- 为其余旧11段教学、4处回返、4个普通跳不足负控及4条可选路线增加独立LegacyTeachingSectors参数与owned admission。原manifest在await失败时也释放；查询前由owned root接管，KillZone只计数且必须零恢复。V2 Sectors[]、正式generated Main、camera、shield和Boss触发入口保持，不把旧11改18凑通过。optional receipt由误写3修正为实际4。
- GripStone边缘反例保留原32px完整胶囊安全内缩与48px净空。新运动使原+89布置不再边缘落地；新的独立反例左缘410，实际中心约419，只进入9px，真实up接触，legacy判定true／safe判定false。圆角中心在外与精确垂直上接触不兼容的426失败原件保留；不声称完全相同的历史“中心外七像素”轨迹，正式地图未修改。

## 事实：两次冻结分别记录，不混算完成率

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。
每案保留fresh build、三个原生日志sink、provenance、各phase已配置的输入副本与前后库存。两批均source1695、每门fresh构建0警告/0错误、guards=[]、source/helper/user/binary四类库存对全等；五log全量扫描，关键失败全文读回。post的五个legacy叶均在完整source前后哈希清单，但尚未全部加入显式逐案副本列表；下次运行先补该列表，不反写本次receipt或冒称已全复制。真实userdata仅保护库存，不读取存档语义。

### 冻结A：诊断／Upper／GripStone，8门7GREEN／1FAIL

batch1115终态1；共同source inventory SHA256：
`ABBF1053A39F3A840A66B0DF2D92EA6BF1AC8E6CDB2850EA521DA5960B1E5CE6`。

| phase | case | engine exit / ms | strict |
| --- | --- | --- | --- |
| thruster-profile-diagnostics | thruster-profile-diagnostics-20261018-213838-205-528704baf73c4909b18ca862e19c0ffa | 0 / 551 | GREEN |
| thruster-detached-motor | thruster-detached-motor-20261018-213909-668-fef92b8fb9aa4769a04ec1282b245ef4 | 0 / 555 | GREEN |
| thruster-rig-compatibility | thruster-rig-compatibility-20261018-213938-976-e1f5975e9d5848f98f97a131930c5b27 | 0 / 914 | GREEN |
| thruster-upper-high | thruster-upper-high-20261018-214011-199-c1244957a97e431da1b83cbe095659de | 0 / 1964 | GREEN |
| thruster-legacy-grip-stone | thruster-legacy-grip-stone-20261018-214044-252-8c433f59713842dc8ccbe3f16bbca0e1 | 0 / 6591 | GREEN |
| thruster-player | thruster-player-20261018-214121-784-9f1ed2666fb345978b2e59c284e9a2db | 0 / 22597 | GREEN |
| prologue-pre | prologue-pre-20261018-214218-181-f9633c82f2fb42aebba78439c33d81fb | 0 / 100017 | GREEN |
| prologue-post | prologue-post-20261018-214434-600-a74fe67021e44f9eb7566909307b3c3d | 1 / 16119 | **FAIL** |

此版post已过Bell、盾区及GripStone，首失败推进至旧CraneShaftWall节点不存在；pre已完整过所有原room/segment。post失败还有离树fixture未清理的RID/ObjectDB/resource退出诊断，原件保留。

### 冻结B：全部旧教学入口拆分，5门4GREEN／1FAIL

batch6265终态1；共同source inventory SHA256：
`9ADF852D115FF22CA63EC94EED53954BFAF1B679436AB5C91ABD2139D31B2F3D`。

| phase | case | engine exit / ms | strict |
| --- | --- | --- | --- |
| thruster-legacy-grip-stone | thruster-legacy-grip-stone-20261018-220803-766-78008413682440f0bae90a9e51181fc3 | 0 / 6679 | GREEN |
| thruster-legacy-terrain | thruster-legacy-terrain-20261018-220843-247-092a44d938cf4641af561b628d5eb963 | 0 / 5843 | GREEN |
| thruster-legacy-sightlines | thruster-legacy-sightlines-20261018-220940-159-195709a8cdfc428eb63938c07be463fb | 0 / 2654 | GREEN |
| prologue-pre | prologue-pre-20261018-221032-412-9fd349b7f6984e5bb70b56574a40cd10 | 0 / 103185 | GREEN |
| prologue-post | prologue-post-20261018-221304-674-a6729cb249f14de4bfc85ba2d66cd869 | 1 / 38173 | **FAIL** |

- pre再次严格GREEN，103185ms、无原生日志诊断。此前Upper/Profile/离树GetPath导致的本次pre失败已解除；不据两次安静运行关闭另有栈的历史Handle/FATAL。
- post已通过Bell、盾区、GripStone正负控、墙面first-hit579／同cast取消、天花板底面516的Swing→Boost/Released及原速度投影、空中自救后稳定12帧、9视线cells、全部4处回返，以及入口三样本。
- 四回返末位置分别为(646.5199,703.93286)、(806.00214,1155.9338)、(937.4126,975.93274)、(902.7405,677.9338)，均保留12稳定／15%margin／0 KillZone进入／0恢复，未禁碰撞或移动平台。
- 当前首失败是真实legacy mandatory `descent_grip_lesson` 的.2样本：239/240、1cast/0latch、Obstructed、最终(1128.3534,1205.9333)，原落地断言仍严格失败。原始ground preflight ray清楚，frame14从(167.65544,488.16736)到锁定(470,330)时，首命中为Counterweight/ThreadTarget/TargetCollision@(371.4934,381.53235)。
- 当前post三个sink只有该断言（stderr/external重复记录），没有本次退出RID/ObjectDB/resource诊断或节点缺失／空引用。owned入口清理已改善当前失败退出，但不能据此关闭别处历史资源故障，也不能把未到达的剩余教学样本算通过。

中间两次post失败也保留：214434-a74fe670缺CraneShaftWall、220302-87273fb1缺ascent_grapple_one。两次严格失败都不是正式Main已坏或Definitions-null旧FATAL的证明。

## 推理：下一处先核输入轨迹与阻挡语义

本次已从“旧节点找不到”推进到实际输入／运动／目标阻挡关系。初始正确选中锚点、移动后射线命中配重交互Area是事实；该Area中心(360,356)、半径28、layer32，被当前CollideWithAreas查询包含，mask0不代表射线忽略。尚无latch、第二Space或boost，不能归咎于提前借力预测。事后ray日志不是gate当帧全部子条件的receipt，是否仅LOS、实际碰撞也阻挡或输入轨迹与旧版不同，尚未证实。不能直接说爪力不足、只需改预测、关卡太难，也不能移走配重、改collision层、扩大240预算或删样本来取绿。

旧DriveAssisted／PredictLandingX仍热读Tuning和旧线性积分；boost预测还混用旧CasterFrame与当前速度，输入轴采样／neutral策略也需按实际时点迁移。全部11文件数值消费者及正常Main+Explore仍待完成。四回返本版通过不等于这些预测已迁移，也不等于V218段通关。

只读绑定核查还发现多处现有代码对GetSlideCollision共享wrapper使用using，尚未迁移。本轮未新增借用对象Dispose，也没有证据将旧ResourceLoader Handle栈归因于这些碰撞读取。该风险保留到专项验证，不做批量无证据重写。

## 风险、未完成与保留边界

- Task6短喷音频退出2 ObjectDB的问题本轮未重测／未修，继续OPEN；正常速度连续AV、最终像素／音色与真人未验。
- 历史冰区Handle、叶154Shape RID、profile31Shape RID、匿名FATAL／Definitions-null保持对应独立调查；本次pre GREEN不跨对象续签。
- 新50正式仍0/50、24canonical未实现；正式127／候选132、其余新地图／机关／敌群、正式美术与最终UI声音演出继续属于完整1–11。旧26+新50=76普通＋1Boss、骨骸10／幽灵10包含其中，不缩减。
- 正常双路线、存档／死亡／Continue／旧档隔离、720p–4K、真实显示稳定60FPS、真人、证明录像、Windows ZIP及授权GitHub读回归档仍OPEN。没有发布新包或Git写入。
- 正式Player SHA256仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。无新媒体展示、生成／采购／付费、OS键鼠、自动化恢复、引擎升级、GC／cache／drain／时限／日志豁免。所有旧失败与用户dirty改动保留。
- 本轮所有owned批次与消费者均终态，Task6/7未complete，尚不触发计划最后的whole-seven-task review。

持续账：[总待办](2026-10-18-project-remaining-backlog-v001.md)、`.superpowers/sdd/2026-10-18-thruster-player-integration/progress.md`。

