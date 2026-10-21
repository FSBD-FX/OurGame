# 短喷输入事实与预算状态 — 原生阶段证据 v001

日期：2026-10-18，Asia/Shanghai。状态：**基础组件通过；实际Player接管待实施**。完整目标1–11工具ACTIVE，未来预留不并入，自动化保持取消。

## 已实现及真实验证

- 实际`PlayerInputReader.RawFrame`保留同采样、动作仲裁前的事实；Shift+Space+K原始边缘均保留，Current仍只有Shift。清帧／边界抑制／暂停恢复通知同样清Raw边缘，J/K/Q及L/Shift/Space原优先级保持。
- 纯`ThrusterPulseModel`只持有不可变配置、方向、有限燃烧、预算与身份，不持有第二速度或节点。只读预检与提交分开，ground/rawSpace/air预算不同；已成功sample、跨model／revision／life／config旧令牌不能重复提交，结束只发一个typed收据；配置值替换不退款。
- 源输入专项使用真实Godot物理键映射，但采样适配器及通知为手动调用，**不是自动Player能力链或真人试玩**。纯模型的landing notice不是实际floor认证；真实站稳／落地／拒绝sample消费仍由下一Motor整合负责。

| Fresh门 | engine退出／耗时 |
| --- | --- |
| pulse | 0／545ms |
| input | 0／508ms |
| module | 0／505ms |
| dynamics | 0／651ms |
| gravity-delta | 0／555ms |
| keyboard | 0／9827ms |
| actions | 0／7238ms |

全部fresh Rebuild成功、0警告/错误、source1684、guards=[]，stdout/stderr/external保留并核对无诊断。新四phase无drain，旧三门既有退出流程未改变。完整case见[执行台账](../../.superpowers/sdd/2026-10-18-thruster-player-integration/progress.md)，原件在`E:/OurGame/Knight/.evidence/october13-stable-claw`。

已补30/60/120Hz地/空制动0.1与0.25秒未饱和对照，30Hz末步按fractional时间；空中300／−75，地面200／−220，对独立分段公式最大观察误差<0.00011px/s。未改变生产动力学或扩大原容差。首次输入测试曾因long通知常量编译失败，保留为非行为失败；修测试后才观察RawFrame缺失真实RED，再写产品。

## 未完成与风险

实际Motor、旧PlayerMotionModel、Claw没有被本阶段替换，当前游戏仍旧Dash；不能称短喷已可试玩。下一按[实际整合计划Task3](../superpowers/plans/2026-10-18-thruster-player-integration.md)先做自动physics组合输入及拒绝不伤爪负控，再接原子接受／专用撤爪、来源与风／碰撞／落地等。

Claw发布／驻留队列只防递归，CurrentFrame相等不证明reset后仍有效；须独立authority revision。专用接管保留旧target的false驻留通知、保留NextCastId，不走普通Released的去inward／限速／boost，也不依赖全Reset或随后必起喷来清旧表现。

完整pre/post本阶段未重跑，最近组件阶段仍旧Upper／profile负例警告和Bell／恢复父链FAIL；另三资源FAIL仍OPEN。Bell是运行时恢复路径解析，不是C# parse/import错误。正式127／候选132、新50正式0/50、24canonical未实现及正常全关／最终视听／真人／发布均保持总账状态。

## 本轮取舍及代价

1. 先隔离数学／模块，再接实际Player：避免半接管，但此刻不能交付可玩短喷。
2. root串行原生与共享源码冻结、代理只读：较慢，但不让二进制和正在运行的源码相互错配。
3. apply_patch／本地台账与证据保留，不用Bash提交或删除scratch，不混推dirty：暂时没有新Git版本检查点，发布前还需核验范围。
4. 重力贡献包含无推力基线的终速修正、推力贡献是裁剪后有/无喷力差：避免限速冒充有效喷力，代价是后续消费者不能把字段当原始g×dt或长期来源账户。
5. 多Hz制动中间点曾重分为非阻断minor，因已有单步独立负控且未证产品缺陷；现已按下一计划补齐实测，不再留作未证覆盖。
6. 纯预检及拒绝不写预算；拒绝sample的一次性消费留给Motor：只有Motor知道真实同tick世界事实，代价是下一步必须用自动physics测试排除旧拒绝输入在状态改变后重试。

仍延后的minor：公开动力学结果字段旁的归因说明，随Task3消费者迁移补；已有spec、台账和数值负控，不改变算法。

无新媒体／付费／前台抢焦点／OS按键／真实userdata语义读取／Git写操作；没有恢复自动化。全部1–11目标未完成，不能拿七门局部GREEN替代。
