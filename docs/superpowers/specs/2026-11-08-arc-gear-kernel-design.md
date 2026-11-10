# 悬吊圆弧齿轮内核

## 意图、事实与取舍

推进原T03用户已要求的空中摇摆齿轮，丰富机关而非重复直轨或给旧摆锤换名。当前直轨只支持TravelOffset和单capsule扫掠，不能表示弧顶访问/弦内空区。v329同1916输入/六产物11阶段+cold通过，但不关闭旧资源/缺帧原因、正式声画或原十一项。

考虑：自由刚体可响应碰撞，但需新碰撞/稳定性/通路时序设计；直接改旧直轨会改变已保存默认值及旧行为。开发选择独立电机驱动固定支点arc model/runtime，继承FixedGear的伤害/身份/节奏/撤权；旧直轨默认值、保存地图均不动。周期驱动不冒称自由重力钟摆，也不宣布最终皮肤/正常观感已批准。用户已要求直接按合理方案持续完成，例行实现不再次阻塞审批；新费用/窗口/发布等边界不扩张。

## 本轮目标和非目标

交付可保存/重载、真实Player接触、全访问弧精确窄相、未来占用与当前圆雾表现的独立内核；先独立原生证明，正式点位/地图导出/全路线集成是之后同T03工作，不把内核成功当T03整项完成。

不改Player/原动力/HP/直轨/固定齿轮/旧图；无新付费/正式素材、GUI/OS输入、GC/cache/引擎/新dump、Git/删除/真档/自动化。root唯一编辑/构建/原生，助手只读。

## 运动与几何

- 新`ArcGearMotionModel`快照PivotOffset（从作者起始圆心到支点，长度(0,512]）、SwingRadians（非零signed、abs<=Mathf.Pi）、PeriodSeconds（1..20且峰速`L*abs(angle)*pi/T<=600`）。开发默认(80,-60)、-1.8545904、3.2，只是机构样段参数，不改旧直轨。
- phase有界，fraction=.5*(1-cos(2pi*t/T))；当前位置`pivotOffset+(-pivotOffset).Rotated(angle*fraction)`，速度为其解析导数。Reset精确作者位置/零速度；非正/非有限delta不推进或积累，完整周期/折返保整个visited fraction区间，不以首尾同点抹掉路径。
- 新`CircularArcSweep`双精度计算弧到点/有限线段距离；候选含端点、目标端点径向/反向、线段法向/反向、支持线交点、投影分区根，映射到实际closed角区间。不采稀疏点，不用弦capsule或AABB授权伤害，不放大真实齿轮半径。
- native Body-only/Player-mask/零margin矩形query仅作辅助宽相，饱和撤权，并联合有界同World真实player组补齐本拍候选。按body-global shape index→owner-local index读取借用Circle/Capsule/Rectangle，核对native RID/local transform/尺寸与启用状态后，以本拍CharacterBody姿态×native-local的实际similarity world transform精确窄相；不要求必须有CollisionShape2D子节点。非均匀/剪切/不支持或不一致的玩家shape拒绝授权并显错，不silent fallback；不把滞后的BodyGetState root transform当本拍姿态。
- 圆判弧点距<=半径和；胶囊判弧到实际core segment距<=半径和；矩形先测弧首点在内部，否则到四边距离<=齿轮半径。全圆并集宽相只向外取float边界（至多float ULP），窄相不加伤害epsilon；二进制浮点极近数学切点仍有数值分类边界，不称任意实数精确。
- 查询全部候选验证后才交给继承的单一ProcessGearContacts；同body有多shape须在至少一shape窄相命中后去重，不让先遇空shape屏蔽后续命中。
- 实际回调反例补充：一个候选已通过原生形状/本地变换/维度校验并按本拍姿态获接触授权后，若前一伤害回调改变了该候选场景Transform，则撤销这次旧授权，等下一正常物理步重新查询；不强行提交服务器位置、不使用旧位置伤害，也不把合法回调撤离抛成配置错误。代价是回调中移动后即使仍位于危险内，也不使用旧batch补伤。
- 实际普通键盘反例纠正上述root-pose前提：`BodyGetState.Transform`及原生宽相可落后本拍MoveAndSlide/合法重定位，不能要求它等于本拍CharacterBody场景姿态。保留原生查询完整性/饱和撤权，并联合有界同World真实player组补全候选；原生RID、全局shape index、owner-local transform、形状尺寸与启用状态仍逐项校验，世界姿态用本拍CharacterBody.Transform（同现有Incinerator做法）。回调改变已授予目标姿态仍撤旧授权，不强行写physics server；此非相对运动CCD。
- source仅单位平移world basis，native Area圆与当前pose一致；Ready快照、checkpoint/回调撤权、单调攻击ID、hidden/staged/paused/queue/重入/owned query释放沿旧合同，不释放借用shape。Reset teleport不是扫掠攻击。
- Inventory在Fixed分支前识别arc，保全未来弧AABB Hazard；Fog仍只画当前native圆，不泄露未来弧/支点后区域。悬吊线/支点为程序辅助表现，不新正式美术。

## 原生反例与覆盖

先写reflection native测试取得缺runtime RED，再加仅继承Fixed的最小stub：真实保存Player在(180,464)、原floor top500，trap起点(20,624)、PivotOffset(160,0)、SwingPi、period4、gear r24。完整周期首尾同点但上弧顶(180,464)必须真实HP5→4/sequence1；配对起点(20,464)的弦内Player应HP5/sequence0。静态stub、端点-only、弦capsule、AABB伤害均不能通过两案。测试保存/CacheMode.Ignore重载后使用，原生前置证明BodyCollider capsule20/72、不与端点圆接触；不拿缺类RED替代实际几何RED。

unit手算独立literals：点/胶囊/矩形的弧顶hit与弦内miss、法向最小距3、x90支持线交点0、内部矩形包含、零core、角跨±pi、signed反向、fullperiod/turn、30/60/120、非法参数/巨大delta。无镜像builder自算expected。

运行时还验自动物理推进、原native圆同步、重复接触cadence/真实Unblockable、reset/回调、staged/hidden/paused/disabled/queue/重入、多个shape且空shape先于命中、未来占用和实际雾命令。原直轨八组/多shape/9回调/fog、固定/弹板、最终前后段/模型/保存cold及正常双路同产物回归保留；不强制帧率/改cap/删旧门。

当前只对实际本physics玩家姿态扫掠移动齿轮，**不声称已解决高速相对Player CCD**。正式位置、全图通行/数量图、正常速度视听/真人、正式资产、旧所有Minor和资源FATAL/发布采样原因仍OPEN。
