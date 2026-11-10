# 居民同次检查端点复用 · 内部等价优化

依据v239/v240真实计时与未通过原180秒前段，目标是减少实际启动计算，不改人数、难度、伤害、地形或原验收门。当前home先独立CheckBody，每条patrol corridor又重查两端；PatrolPoints本身首尾含home后还Append(home)。只读全50布局静态计数552次body对160个唯一Transform；尚未证明其毫秒占比，不保证本改动能解决全部超时。

## 选择与边界（开发判断，不冒称用户指定算法）

- 不选跨帧/manager/global缓存：挂载/Ready/activation可改变几何与Hitbox，现有负例要求前后fresh检查。
- 不选删除零长段或通道检查：独立corridor仍须发现中间墙/电场/容量截断、完整支撑及混合玩家障碍。
- 选单次同步admission内局部Dictionary，完整精确actor-world Transform为键，只存已有不可变纯值结果；无await/回调/挂载，结束即丢弃。原公开CheckBody/CheckCorridor每次fresh，旧拒绝顺序、start/end前缀与collider+shape双身份保持。

内部CheckPatrol在同一context内返回原独立home Body、原顺序所有Corridors、实际CheckBody调用数BodyCheckCount。初始intended基底可能不同于无旋转patrol基底，不能按位置或AABB合并。每条corridor自身查询不复用。manager每次CheckAdmission调用一次本方法，前后两次admission分别新建局部表。

测试必须用真实独立物理世界比对旧fresh路径：重复两点、原点零长段、不同基底、失败端点、中间墙/电场/容量、玩家单独及混合foreign；相同输入的bounds/transform/support IDs与Issues完全对应，跨调用改变形状/keepOut/世界不得复用旧值。调用数是实际cache miss后执行CheckBody的计数，不是另算Distinct的宣传数值；计时仅记录，不以不稳定微秒阈值过关。

原76普通目标、Boss/NPC、挂载事务和原主线/探索/原生回归全部保留。当前仅无窗口权限，正常速度动作/手感、美术、硬件和用户验收仍OPEN；不引入付费、自动化、GUI、Git发布或删除。用户已要求持续直接推进，内部等价实现不重复请求方案确认。
