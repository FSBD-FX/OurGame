# 可靠着陆减伤接入 — v004，专项首轮通过

2026-10-18 Asia/Shanghai。开发中，完整1–11目标ACTIVE，Task4尚待最终同版回归，不是正式交付。权威checkout仍 `E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`，原dirty保留，HEAD e7c70498未变。无新媒体/资产/付费/Git/ZIP/自动化。

## 实际变化

Motor唯一MoveAndSlide前冻结指令世界速度；零位移恢复查询排除原地嵌入，motion-only与recovery两次只读查询必须一致。实际唯一floor、完整支持面清单、travel/remainder/位移和接触点速度一致时才启用相对法向冲击。复杂/多支撑、未知运动或速度按typed原因回退旧高度，缺失/非法数据不当作安全0。诊断查询不移动身体，不使用GetRealVelocity或碰撞后归零的Y。借用collision/shape/directState不Dispose；只释放自有probe与参数。

正常未来碰撞也会有正GetDepth，例如0.08215332；其含义是检测点处重叠，不直接等于初始推挤。实测zero-motion recovery=false，motion-only/recovery相同，随后实际native travel/depth一致。没有放宽SafeMargin或以depth为0猜可靠性；真实嵌入负例仍InitialOverlap回退。

高度基数宽数值→冲击系数平方→ceil→最后cap。FallSafeImpactSpeed角色配置默认300，参考为实际运动快照MaxFallSpeed780，重力/普通跳不改。Health前消费一次，i-frame拒绝不延后补扣。原高度fallback入口保留。

## 真正的RED→GREEN

- 晚喷800px真实键盘反例：原规则撞前111.16656仍1伤/HP4。接入后 `thruster-impact-player-20261018-154321-941-fb75fd97cca94fc486f0b0490915cd81` engine0/4076ms，780仍1伤，111为0伤，原高度保留。
- 作者资源热改曾使执行780但落地参考变2000、错误免伤：`thruster-impact-negative-20261018-154515-551-df400cc99e154389a81742384be77231`1/14243ms。落地安全/参考与运动模型同一启动快照后 `thruster-impact-negative-20261018-154656-478-ecd1c363f6364596b09478f79666b8d6`0/14733ms，八项通过。
- 相对法向数值边界缺失RED `thruster-impact-formula-20261018-155728-103-e43a14152d754309a2c5bc701ef6b76e`1/552ms；GREEN `thruster-impact-formula-20261018-155844-706-3c2416d40e6a405cbce84ed8b23e7ce5`0/699ms，包括同速0、切向不计、斜法线340、非法/非有限normal/velocity回退。此部分是纯数值，不冒认一次零相对速度的原生新碰撞。

## 已验证的范围

新18个自动physics场景：2基础对照、8负例、8支持面案例。晚喷717.5仍伤、喷后再次780仍伤、全软免伤、真实一条slide但第二硬支撑回退、非bool软标识不免伤、真实i-frame拒绝后站地不补扣。其后真实战斗仍可伤害，无永久免伤。

支持面最新 `thruster-impact-support-20261018-155533-534-c6741f13dfff4f458cf65e9fe8442c6a`0/14856ms：真实升降台向下120，身体780测相对660；向上120测900；慢喷身体419.5、相对299.5免伤。墙→地三接触MultipleContacts回退，初始嵌入InitialOverlap。水平乘台58px、非零原生平台速度、不新结算；0.5px自动snap，前后持续floor、指令Y0、普通运动未碰撞且恢复向上，实际下移0.496，不新结算。竖直乘台700多px后走出，独立自由落差1120.1875/追踪1124.9323，仅2伤，不计乘台下降。

旧fall `fall-20261018-155921-901-3041cb498fe948b2ac6b074d5d1912c4`0/11668ms，fall-claw `fall-claw-20261018-160012-029-1c86f26a10df4762a9b0d4f42439d553`0/9085ms。两者沿原runner既有退出策略；新thruster-*没有新增GC/drain/日志豁免。

补跑旧完整13个摔伤fixture时，旧640宽落台不适配新余速：`thruster-impact-legacy-20261018-160406-682-296d17ecd51c456e96448c14bb74b2a4`1/21537ms实测越过地面高度时x687.5265、平台右缘640。仅该测试dash落台/对应测试皮肤改1600宽，不改正式地图、落差、喷力、伤害或碰撞。其余普通/暂停/盾/无敌/软垫/真实爪/保存底台案例保留；first GREEN `thruster-impact-legacy-20261018-160543-928-d853a937a96d49769a6d80a70309c261`。该旧集合含手动InputEventAction/鼠标适配器以及抽取保存底台，不称纯键盘完整路线；新18场景另用真实物理键与自动Motor。

以上current source1688；成功case build0警告/0错误、guards=[]，已读回的四类库存source/helper/user/binary一致。少数早期API编译错误及fixture首次位置未预设导致的前置失败均保留，不当作产品RED。当前无owned遗留后，将冻结同版运行全部相关专项及pre/post。

## 未完成

Task4最终同版回归；Task5保存模块/装配；Task6typed视听/UI；Task7旧消费者与正常Main/Explore。五历史严格失败、pre七Handle新观察仍OPEN，不因专项绿关闭。正式新常驻0/50、地图与最终美术/声音/真人/性能/版本交付继续按完整目标推进。未展示新图稿或录像，过程媒体目录无本版新增。
