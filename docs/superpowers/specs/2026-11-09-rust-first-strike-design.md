# Rust首次近战可触及边界 · v335

完整KNIGHT原十一项ACTIVE，G:/knight/Knight，无窗口。前轮v334为PROGRESS：用户批准的联合落脚取证组件完成、当前1926/9909/2755原11回归严格PASS；未改生产动作/物理，也未修历史6ShapeRID。当前无拥有的原生进程。

## 事实、推理、风险

旧原生012227只在末态记录Player900/敌830.66547、gap69.33453及warning/active未命中；不能把所有Active逐帧归因到末态。保存attack矩形26×18、朝前34/向上7，Player胶囊R20/H72，在本竖带水平可达上界67，模型起手70。需先捕获首次完整AttackId的Telegraph/Active/Recovery及真实命中/伤害和两阶段几何。

共享runtime影响当前7波次槽＋16常驻槽（rock3/scrap4/bone5/lost-echo4），不是全76或同时存活数。四resident叶是扁平保存，并不继承基础tscn；profile没有攻击距离override。原70上限、360感知、95追击、.65/.14/.65时序、1伤害、已锁朝向/目标、Bone/Ghost权限与LOS/领地撤权必须保留。

## 方案

选择先原生RED，再由adapter实读保存形状收紧“停止接近并开始前摇”的上边界，模型仅接受起手资格；不改变已经承诺的攻击过程。具体生产切口须由首次完整周期证据确定，不能先扩武器或用魔法距离代替判断。

首次完整六周期现已实证：四个远距静止左右resident/base-wave在全部Active仍留下约2.33世界单位水平空隙；近处真实命中与D退避均成立。最小实现以共用ThrustOrigin(34,-7)投影保存攻击形状，按目标方向比较真实Hurtbox的外侧水平边界，要求实际穿入而非仅相切；模型原70上限仍保留。该资格只限制Idle/Chase/Recover进入新前摇，绝不取消已承诺攻击，不是全二维重叠/必中判定；垂直或贴身的旧落空行为不顺带改写。非标准目标缺规范Hurtbox时保持原range-only兼容；存在但坐标/宽度无效时拒绝起手。无需新shape、物理查询或借用资源Dispose。

当前第一次修复后原生案在5项完成后于清理发生原生终结器崩溃，第六项base-wave-left未运行。五项功能结果不是六项GREEN；严格门仍FAIL，资源根因独立OPEN，不能用重跑筛绿或扩大允许错误掩盖。

不采用扩大伤害形：会改变预警/受击距离及扁平叶；不采用统一改AttackRange数字：违背原70模型合同且绑定当前形状常数；不采用前摇追踪/强制命中：会破坏真实闪避。持续开发授权下这是开发判断，不是用户已批准新动作风格/范围或普通观感。

## 原生验收

复用RangeFixture真实保存Player/rock叶及作者home、同一保存floor/领地，另用保存未配置base wave对照。六案：近处720静止命中；720首警告后实际D退至至少+96并释放、原首次攻击必须可躲；900右侧静止接近；432左侧；对应base wave右/左。全部入树后不改pose/velocity/HP/AI、不直接TryConnect。

fixture只增加挂树后、12 settle前的测试订阅hook，以捕获近处首次警告；回调只记录标量和请求，真实D由等待循环发送。Damage先于Connected，不能倒置断言。正控受伤后允许真实击退造成短暂离地，但必须原生伤害且HP只降1、actor仍在原floor/领地；伤前/无伤案仍完整原接地门。

逐原生tick记录发布前几何与MoveAndSlide后几何、ID/相位/时长/锁向、真实Connected/Damaged/HP；完整收招结束才终止，第二ID只作边界，不补齐第一ID。保持原windup/active/recovery、独立完整左右命中与真实退避负控；全6功能控制和严格日志都需通过。共享影响另跑原patrol/Bone/Ghost及ghost实际绕背政策，不把单rock等同全族/全23。

资源错误独立严格FAIL；无豁免/GCcache/引擎/新dump。一次fresh全增量审查、C/I一次TDD修补/Minor延期，完整原11同版路线/保存冷读/前后段、source/binary/protected/helper核对及唯一媒体Check。无GUI/系统输入/付费/新素材/定时自动化/Git/worktree/删除/真实档实验。
