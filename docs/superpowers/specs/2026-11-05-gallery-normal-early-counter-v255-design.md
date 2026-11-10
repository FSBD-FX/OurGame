# v255 · 正常gallery的严格早预警反击候选

## 事实与推理

v254的17条同次HP2记录证明：Main在loop317–326有足够完整反击时间、37能量、13个来源统计、无已报告缺失/未知/重叠/来弹否决，却因所有预警统一否决。Explore的首次Recovery立即J并在原预算内完成；换批两线同为37physics。当前不应改游戏难度、能量或899，而应让正常驾驶使用已有严格候选检查。

这些数据不证明未执行的floor/slide0/ownership/fresh/命中安全。本轮实际走既有live检查与J，不把日志bool当授权。

## 决策

新增测试驱动纯路由：`Task10GalleryEarlyCounterRoute(bool keyboard, bool gallery, bool normalPlan, bool explicitProbe, bool owned, bool skitter, bool recoveryScope)`。

选择条件为 keyboard && gallery && (explicitProbe || normalPlan && owned && skitter && recoveryScope)。这是“是否评估live候选”，不是反击许可。正常路径仅当前owned Skitter、batch1、现有locallyeligible、Recover/Recovery/AttackId>0；Laser正常路径不新增资格，旧显式probe选择保持，再由原live门拒绝不合格对象。

live入口先确认keyboard/gallery，避免无关区域增加native读取；从当前actor/group/frame取三个独立scope事实。保留完整 `GalleryOwnedRecovery()`、HP、真实bone-diver类型/scene/当前攻击ID、有限最小余量、缺失/未知/重叠/来弹拒绝、完整攻击+8ticks/双攻击能量、未举盾、接地/速度261/真实slide0为Grip5/实际胶囊走廊两边至少30等原条件。外层和盾释放后的fresh-danger复查都使用该入口，匹配来源/攻击且原releaseWaits0；最终能量预算和真正J边沿不变。

新增路由单元先以旧probe-only行为实际RED，再GREEN；期望来自手写布尔表，不用生产公式计算。不得只改outer绕过fresh复查，也不让所有Telegraph/敌族自动counter。

## 运行验证与风险

正常Main先一次：要见原HP2首窗路由、RECHECK fresh、EARLY_J_COMMAND、真实命中/下次防守与整个Main Boss/实体出口/恢复0，原899/HP/预算不变。失败保留，从新事实继续分析，不筛绿。Explore及完整原前后段、保存/地图/机关/居民几何/键盘/actions/模型同源回归，所有错误/泄漏严格否决。

仅修改两个测试CS，不改生产玩法/美术/物理。现有观察器/HP1和此前失败保留。候选读取仍可能被实际floor/时序拒绝，完整主线不保证本轮必过；资源Handle/FATAL无根因，继续OPEN。正式美术连续动作/R-I/R-C/R-P、76全量与正常速度/硬件真人仍未完成。

## 权限

G:/knight/Knight原地脏树，root唯一build/native。仅headless隔离存储，无GUI/OS输入/付费/新媒体/dump/引擎/cache/GC/KeepAlive/自动化/Git/真实存档实验或删除。无额外身份构建混入普通同版回归；资源归属采样另案处理。
