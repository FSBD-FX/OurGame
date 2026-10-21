# 短喷运动身份收尾与着陆减伤开始 — v003

2026-10-18，Asia/Shanghai。开发中，完整1–11目标ACTIVE。权威目录为 `E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`；HEAD仍e7c70498，未提交，原dirty保留。无付费生成、媒体展示、Git/ZIP发布或自动化。

## 本版实际修复

真实Swing→Space借力的Boost回调完整替换速度为(0,120)后，旧pending释放仍把速度投影/加回(220,0)。反例 `thruster-player-20261018-145918-271-3ca43b9126f1438f838e8005e5d7eb26` engine1/15098ms。现在在Boost发布前冻结Motor运动revision，后续分阶段恢复同时核对claw与motion身份；旧motion被替换时只中性撤销旧爪，不写速度、不普通Released、不清高度/输入、不补预算。没有pending借力时，普通ExternalImpulse仍保留Reel/Swing，不扩大取消范围。

first GREEN `thruster-player-20261018-150126-704-60a78375f660416aa0d3ce002d61b27a` engine0/22718ms，33自动physics场景＋1scheduled对照。随后同source1685完整18项回归，16专项GREEN/2广域FAIL；build全部0警告/0错误、guards为空，source/helper/binary/user四类库存前后哈希一致。完整表保存在SDD ledger和外部原始case，不将headless自动键盘称为真人或显示FPS。

最终player `thruster-player-20261018-150324-654-d9c414cf9d38418f8ceedbd16161055d`0/22795ms；预算 `thruster-traversal-20261018-150418-347-03940f7253f5427b9afb001b5c017fef`0/7284ms，11自动键盘＋2scheduled适配器。Task3仅在brief约定实现/专项范围完成；Task4–7仍待继续。

## 广域结果与句柄观察

pre `prologue-pre-20261018-151316-555-f585db3fd2154185956dda94e56ef1e1`1/88444ms，仍Upper转移失败及两条旧profile警告，此外记录七次Handle error。前五为GD.Load，六为PackedScene.Instantiate，均进入Descent工厂同一表达式；第七只有RefCounted.Dispose/Finalize匿名终结器栈，不得归属同一Main实例。前六续体含完整Supply结束后进入Descent；旧teardown栈帧不证明旧根仍挂树，也不能确定坏Resource/缓存/GC根因。最小prefix验证尚未实施，异常保留OPEN。

post `prologue-post-20261018-151515-074-1f761dc773b34ef79331ef4823d50689`1/8429ms，仍Bell落地/Recovery路径。候选Handle、保存叶154 Shape RID、子型31 Shape RID等历史严格失败也未据局部绿关闭。无新增GC、缓存、等待、时限或日志豁免。

## Task4：已实现纯公式与角色配置，尚未接实际落地

公式先宽数值高度基数，再相对法向冲击系数平方、向上取整，最后cap；不把已经按HP封顶的旧Damage再乘系数。无效/缺失测量回退旧高度。独立预期包括4600px/540px/s/max5→3，极大/无限高度不溢出，安全速度先处理避免∞×0。

- 缺公式真实RED `thruster-impact-formula-20261018-152453-289-d590e22b97b24dd3a6c2573f5af66622`1/534ms；GREEN `thruster-impact-formula-20261018-152611-970-5b1f9c23b31a4fea8a28b04e3c509530`0/550ms。
- 缺角色字段真实RED `thruster-impact-config-20261018-152725-380-793f6ce8e8bf41fc9379bbebd95f1a1f`1/652ms；GREEN `thruster-impact-config-20261018-152819-728-8cbec9b2bc564b17941f75dc20df5ffc`0/559ms。FallSafeImpactSpeed默认300，参考读实际MaxFallSpeed，必须有限且参考>安全；原重力1850/跳跃590/终端780未改。

以上source1686，fresh build0/0、guards=[]、四类库存保持。只证明纯公式/配置，尚非实际Motor减伤。

新增保存Player、原800px硬地、自动物理W+Shift真实反例 `thruster-impact-player-20261018-153001-639-ff7352d865814576b2047dc37fe49cf4`1/3617ms/source1687：终端780落地1伤；晚短喷前gap73.986，实际撞地前111.16656，原高度仍799.9961，却仍1伤/HP4（应0伤/HP5）。无teleport、禁碰撞、手动Motor或OS输入。下一接typed可靠采样及保守复杂接触回退，再验证移动地面、混合支撑、旧爪/风/危险/存档边界；不能把本真实RED称完成。

## 剩余

Task4实际碰撞；Task5保存模块/更换；Task6typed视听/UI；Task7旧消费者/正常双路，随后完整地图/正式50常驻/资源及最终交付。正式新居民仍0/50，最终76普通＋1Boss范围不变。所有展示媒体仍须先进入项目过程归档，本版无新媒体。
