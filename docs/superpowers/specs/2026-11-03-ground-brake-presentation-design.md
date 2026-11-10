# 真实地面制动与承重动作 · v223设计

来源U-20261103-01、原M03/M06/M09、V03/V04/V09；完整十一项目标继续。用户已授权判断后合理直接推进，不重复程序性选型确认。本样段幅度/节奏是开发选择，不是最终动作稿或正常速度手感获批。

## 事实、推理与风险

已有Dynamics明确区分反向制动和同一步停零后反向；Motor目前只把这两个标志交给推进器表现。普通Timeline在速度大于8时仍播放Run，停稳后才进入通用Brake。仅再次加大BodyLoad倾角不能补足真实制动语义；改运动摩擦会同时改变平台/战斗落点。本轮选择冻结真实物理观察、驱动承重/低摆脚动作，不重调速度、力、重力、输入、伤害或关卡。

备选一是只读渲染速度差分，可能把碰墙/外力当控制制动、丢失同一步过零；备选二是把物理改得更滑，与当前未标定的落点和手感冲突。选择现有Motor完成门内的纯值事实→独立表现模型→最终共同承重和自由脚弧，保留未来正式骨骼/分件接入口。代价是仍须独立正常速度审阅和后续完整动作语言返工。

## 数据与生命周期

- 新纯值`Knight.Player.PlayerGroundMotionObservation(ulong OwnerId,ulong Revision,ulong PhysicsTick,float StepSeconds,float BeforeX,float IntegratedX,float ActualX,float MoveAxis,bool Grounded,bool Eligible,bool ReverseBraking,bool BrakeReachedZero)`；Motor只读属性`GroundMotionObservation`。
- 在StepFree前冻结原_state速度、真实缩放后输入、地面及控制所有权；StepFree结果保留IntegratedX/Braking/BrakeReachedZero。碰撞后ActualX只作事实，不用其差分冒认控制力。
- 仅在既有落伤回调后的authority/健康/未暂停及独立物理帧完成门内发布。跳跃、当前或刚结束燃烧、爪/自动拉/非零爪修正、不在真实地面、轴向阻挡均不具普通制动资格。复用原版本/帧门，不改变运动权或添加物理积分。
- 每次Physics入口先清旧观察；ExitTree、暂停/恢复、成功换模块、外部速度替换、Reset/death及爪控制接管/释放同步清空。仅改变未来控制缩放不抹掉已完成当步事实；观察保存的轴值不得从当前getter重算。
- Coordinator在原输入init区传观察，Timeline按同Ground物理tick消费。旧构造缺省观察保持原行为；零delta/重复tick不得推进承重或脚弧，换owner/revision、回退/断档/无效地形/传送须重基，旧样本不得补播。

## 受力与绘制合同

- 新`PlayerGroundBrakeModel.Step(PlayerGroundMotionObservation,PlayerGroundSample,bool grounded,bool suppressed,float delta)`输出`PlayerGroundBrakeFrame(bool Observed,bool Braking,bool Reverse,bool CrossedZero,float Strength,float PelvisDrop,float TorsoLeanWorld)`，只持纯值，不持Node/Resource。
- 只认可同tick、有效支撑的普通控制减速：积分前速度超过8、积分后同向速度减少，或真实ReverseBraking/BrakeReachedZero在同一步停零/反向。过零的incoming方向取BeforeX，不由已反向的最终速度猜。
- 强度目标为`clamp(abs(BeforeX)/260,0,1) * clamp(lostSpeed/StepSeconds/3200,0,1)`；过零lostSpeed为原速度绝对值。制动建立时间常数.035s，撤销后.10s回稳；最大髋下沉2.2px、躯干逆运动方向.13rad。数值为暂定可测映射，不改物理。世界倾角只在最终面对局部转换，避免v222已修坐标问题回归。
- 正在真实制动时即进入Brake locomotion，不等停零；真实制动帧使用新受力姿态而不叠加旧通用Brake的身体/整腿模板。共同承重替代同帧通用BodyLoad，不重复累计；动作混合后施加到全部十槽、原脚底IK之前，握持和附着关系保持。
- `PlayerGroundGait.Step`仅追加可选`brakeStrength=0`，默认完全保留旧轨迹。按真实物理时间将制动幅度限速到0–1（至少.08s建立/撤回），自由脚新增弧从8降至最低2px；不改stance点、步长、落点、相位和换脚时刻。停止分支本就不加弧，保持原.12s收脚；不得宣称旧已抬起脚的总离地高度立即小于2。
- 渲染髋膝踝固定18+18、不额外缩放；支撑脚世界位置不随下沉漂移，实际Draw须无夹紧。攻防/爪/短喷、落伤、恢复与原输入规则不借此更改。

## 验证

先写仅依赖现有API的真实Player测试：内部Action移动建立Run→松键时速度仍大于8，必须出现制动姿态；原代码应真实GROUND_BRAKE_NATIVE_RED，而非编译错误。再用纯模型字面量控制与真实叶Draw/支撑、同轨迹0/1脚弧比较和实际Motor完成快照核对生产接线。

覆盖被动/反向/同一步过零、低高速、左右、30/60/120、零/重复/回退/换owner/revision、无效/失测/空中/燃烧/爪/外力/死亡/暂停、动作附着和固定腿长。原ground-gait全部新旧委托、physics/跳跃/原55秒门保留；一次fresh审查，C/I一次TDD修补，Minor明确延期无二审。最终当前原完整回归/双路保留全部失败，不筛quiet或放宽预算。

当前单步观测不是事件日志，不承诺每个被渲染跳过的物理帧都有独立播放；连续承重按最新可靠样本及真实时间接续。正常速度/手感、正式分层美术、所有动作链、历史资源/Main899与原十一项仍OPEN。仅无窗口/内部事件；无GUI/OS输入/付费/真实C存档/额外dump/运行时更换/自动化/Git写入/删除。
