# v149 — 屏幕边缘攻击预警保护

依据：已批准的 2026-10-07-keyboard-collection-headlamp-fog-design.md §6。

事实：FogReadabilityOverlay 目前按源叶节点原点是否在逻辑视口内裁选。真实攻击几何可跨过屏幕边缘；原点在外不表示危险轮廓全在外。当前没有证据证明这是 v148 Grip8 死亡的原因。

要求：改按当前权威 AttackPresentationFrame 的世界几何在源叶 canvas 上的保守包围盒与逻辑视口相交来筛选；矩形、地面带、椭圆、胶囊及世界坐标凸多边形都覆盖。包围盒允许保守多选，最终绘图仍用原形状且受视口裁切，不扩大伤害或揭露整个敌人。变换后无效或完全在外的形状不得保留旧命令。

保持：源活性、可见性、同一 viewport/world、620 距离、Telegraph/Active、完整遮挡快照及角色至原点的 LOS，不改伤害/AI/时间/地图/存档/目标优先级。不得把来源位置、形状原点、凸多边形世界点混为一谈；不能用 FinalTransform 将逻辑像素混为物理像素。

验证：独立 fog-edge-commands headless 阶段，以真实 SubViewport、AttackTelegraphVisual、FogReadabilityOverlay 和原生遮挡采样验证命令提交。先观察旧实现的屏外原点/屏内几何反例 RED，再最小修复 GREEN；补足不同形状、canvas 缩放平移、全屏外、清除/隐藏/暂停/排队释放/不同视口/距离/墙体与不完整快照负例。不新增 GC 或改变原退出 drain。它不是 GPU 像素、真人观感、幽灵自然攻击预警或整关通关证明。

用户当前不方便前台。仅无窗口工作，不启动可见测试、不发送 OS 输入、不生成付费资产、不恢复自动化。v148 原生机关崩溃及主线/探索失败继续保留，独立追查，不能用本修复覆盖。
