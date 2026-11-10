# T03 · 空中轨道往返齿轮

## Intent and authority

兑现原完整序章 T03 的真实移动齿轮，增加观察节奏与通行选择，不把已有摆锤／活塞算新增。用户已要求丰富机关且不限制难度，并要求缺少关键资料才问、合理事项直接推进；以下轨迹、参数和工程接法是 root 的实现选择，不伪称用户逐值批准。完整十一项目标不变，候选、原生通过与正式观感分开记录。

仅无窗口代码／构建／测试；不打开窗口、发系统输入、付费生成、动真实存档、删覆盖用户资料、操作 Git 或改引擎／缓存／GC／预算。G:/knight/Knight 是唯一工程，不切 UE。

## Approaches

选择一个同层 `CanyonMovingGear : CanyonFixedGear`，在现有 Gameplay/Mechanisms 下沿直轨平滑往返。保留固定齿轮和危险圆形视觉，实际圆心、原生查询、受保护雾提示一起移动。继承可复用现伤害身份／冷却／绘制／Fog类型发现，不嵌套一个会漏掉Canonical发现的运动父。

不选择仅移动贴图／只轮询延迟 overlap：两端不重叠时可能漏掉中途接触。不选择套用现摆锤或改成World圆形solid：前者不实现新齿轮，后者不符合现矩形遮挡采样且会改变通路。悬吊圆弧另有扫掠包络要求，本次明确为直轨往返款，不冒称圆弧款也已制作。

## Motion and native contact

- 新纯值 `MovingGearMotionModel(Vector2 travelOffset, float periodSeconds)`；travel有限、非零、长度≤512px，period有限且1..20s，峰值速度≤600px/s。不接受部分无效设置。`Step(double delta)` 只推进正有限时间；零／负／非有限输入不推进。Reset回到起点。
- 位置系数 `u=(1-cos(2*pi*phase))/2`，从A零速出发，到B零速折返；世界位置是作者起点＋travelOffset*u。以double保存有界周期相位，不将无限时钟转float。
- 返回不可变 `MovingGearMotionFrame(Offset, Velocity, SweepFromOffset, SweepToOffset, Phase01)`。SweepFrom/To为这段时间实际走过的最小／最大系数位置，必须计入折返点和完整周期；不能仅连本帧两端。直线运动的圆形并集正好为圆角胶囊，允许用原生 capsule IntersectShape 查本帧完整已走路径；静止时用circle。不是用AABB扩大伤害，也不假定 IntersectShape 的Motion能做sweep。
- 新Area直接继承现Gear，不成为World solid或抓爪目标；一个实际CircleShape2D，局部形状和全局basis均为单位平移；实时Area transform必须与GlobalTransform一致。导出设置在Ready快照，后续外改不重配运行状态。
- Mounted世界起点固定；外部移动／缩放该区域根不是运动授权，几何门须拒绝。正常Camera只改变canvas，不改变世界锚点。移出再入树可重新Ready绑定新父，但不能扫过迁移路径。
- 仅自身创建的query/circle/capsule由自身释放；借用场景碰撞shape不Dispose。真实Player层body查询，去重后只调用一次共享消费路径，保留group、world、Health、GuardedDamage、epoch、单调ContactSequence、正常无敌帧、Unblockable与零击退语义。新gear的author伤害2、cadence .5s是root初值，不修改全局HP／能量或静态gear。
- base仅最小提取protected `ProcessGearContacts(IEnumerable<Node2D>, double)`，默认静态overlap路径不变；属性／reset改virtual供派生状态正确参与。加防重入门防回调内再次消费。同步死亡／reset／排队卸载／隐藏／禁碰撞之后旧batch立即停止。
- 暂停、禁物理处理、staged、hidden、queued时不移动也不伤害；checkpoint先撤销旧epoch，再重置时钟和原位置，不扫过复位传送路径。退出撤销授权并释放owned query；再次入树须重新Ready，不能沿用旧运动段。

## Fog, occupancy and presentation

保留原FogRecycling的Canonical、原生transform、620px、同帧整段LOS、pause／queued／staged清空条件；不改FogMovingGeometry或HeadlampOcclusionSampler。当前危险圆边随真实圆心走，不能留旧位置。轨道本体按当前机械palette绘制，轻量固定轨迹和端点可替换，不做新付费素材或新跳动方框；正常观感仍待验。

ResidentEnvironmentInventory必须在base gear分支前识别moving gear，登记全行程圆包络 `moving-gear:` Hazard，而非当前圆。暂停／隐藏／初始disabled仍保留将来占用；不把保守AABB当伤害形状或路径可达证明。

## Candidate integration, not yet accepted

先验证runtime，再由现C# builder写一个Basin候选：UpperGearGalleryTwo上方，r24，world A=(3400,3550)、B=(3400,3630)，local A=(2120,-50)、travel=(0,80)，period3.2s，damage2。全行程world Rect2(3376,3526,48,128)。不改158原solid、原敌人域、抓面、碎片、门或玩家数值。

GalleryTwo真实floor world Rect2(3280,3676,240,40)，40×72胶囊在(3310,3640)/(3490,3640)两侧完整站立；离运动包络各46px。A端齿轮底3574，比站立玩家头3604高30px；B端底3654，实际进入通道且距floor22px。允许观察上升后走过、退回或走原爪路线；不强制受伤或隐式二段跳。局部y<0须真实验证相机／流送，不能靠几何推理放行。

BuildCanyonBasin→保存leaf→BuildPrologueLevel重读leaf；候选父若嵌入快照必须同步重建。先stage到隔离case，结构、通路和现AI／物品保留证明后才接正式文件。新点入图后更新环境inventory、当前地图标注；不沿用v307图称已标新机关。

## Required evidence / non-goals

模型：literal quarter/half/full cycle、反向travel、完整周期端点相同但包络完整、跨折返、分区30/60/120、invalid/reset。原生：真实保存玩家、首尾均不重叠中途击中、圆角空区负例、同帧去重／cadence／无敌帧、同步reset／queued／reentry／paused／staged、owned资源退出；原static gear回归。

雾：直接canonical fixture正例和墙／远距／隐藏／禁碰撞／paused／staged／queued反控；随后实际保存controller发现新子型、运动后原位置不残留。放图：A/B两安全站位、等待净空通行和错误时机受伤、原上下两路／9碎片／76配置位／Boss及出口不丢。最终同源完整pre/post、正常主支双路，后续正常视听／真人不由headless替代。

本轮没有授权新增武器、背包、CG、付费资产、无限盾、无敌推进器或绕路传送。未通过项留OPEN，不能把runtime样段宣布成T03或完整序章正式完成。
