# First-ascent same-query candidate trace

v176最终Explore184908…9f5a133d机关往返已过，后first-ascent原16上升姿态未选精确3956目标：1次Step3636、10次实际Missile ThreadTarget、5次Step3636。K未发，NaN clearance/no native first-hit是身份门提前拒绝，不是无墙证明。完整十一项不缩，源基线1867。

实现者裁定：不猜改跳跃、站位或全局角优先，不重发另一组33ray冒称原查询。给原同步ResolveKeyboard增内部可选固定容量标量trace，普通公开入口传null。原一次33ray、Consider原短路LOS、最终baseline与enemy promotion分别记录；不多查LOS/不调用receiver探资格/不调用任意可重入Action。新增独立记录类仅含managed标量/枚举/Vector2及已有路径字符串，不持有Node/Dictionary/资源跨await。容量0..512，溢出明确Dropped/Fault，不能从缺行说目标不存在；计数接点在原实际query调用处，独立于行buffer容量，但不是引擎外部query仪表。

trace开时才复制元数据；null不创建trace行/buffer、不额外取path或物理query。原选择、敌人相对地形及敌人间角优先、range/LOS/材质与词典序不动。原查询返回后再输出；metadata捕获故障只增fault，不影响正常选择或授予动作。

先用保存Moth＋真实地形的小native fixture验证ordinary/explicit-null/full/0/1/已饱和trace完整resolution相等，33cone及既有LOS调用计数不随饱和改变，enemy与terrain fallback均覆盖。inert内部重载先实际RED，再接入记录。它是局部fixture，不等当前地图通过，也不将查询计数自报当独立引擎仪表。

实际地图只在原16次上升预检启用，每次新固定buffer，以3956真实surface ID为focus；最多16×512标量，原16/32/300/600s、输入/HP/恢复/形体/地图/资源权限不改。记录只用于定位下一动作，不保证本轮可越过。唯一fresh审查、C/I一轮TDD、Minor延期；原Main/pre/post资源/正式图声UI/硬件真人全部OPEN。无窗口OS/付费/新采集/自动化/Git/删除/真实存档。
