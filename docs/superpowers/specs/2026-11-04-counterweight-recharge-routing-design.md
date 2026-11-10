# 配重后段：默认验路的安全松盾回充

## 原因与选择

原v244 Main仍899帧余RustHP1。逐行核对表明，保留完整攻击＋八拍保护窗的58个末击候选都没有足够保守防御储备：759–771只有12.3999195，895–897最高12.933285。现行下一轮实际连续举盾43拍耗17.2；4.8或7.2不是已证全三弹费用。因此否决直接24→12或凭空加入固定低额末击例外，不能把窗口有一次费用当生存预算成立。

另一路是既有KNIGHT_POSTHOP_RECHARGE_PROBE：在同一扫描完整来源、相邻真实形状/输入、单拍发射空档、完整支撑和静止等条件均成立时，仅对下一拍正常L松开，保留必要防御与原J完整窗口/24储备。先在当前代码单独标记开启该实验，保持原899/HP2；只有确认该边界有效才考虑默认纳入。不承诺整条路线会通过。

## 开发范围与不变项

全部修改限测试驱动。玩家能量/热量/护盾/敌人/地图/攻击/数值、输入排序、真实J/K/Space、保存与恢复规则均不改。共享Task10MixedCounterWindow的完整攻击＋八拍及双费用24完全不改，所有fresh复核保留。旧完整来源/新发射/形状净距/支撑/静止等松盾条件不降，不增加等待池或回满能量，不取消原三弹、单跳与HP要求。

如果标记对照证明配重清场仍HP2，提取现有selector为内部测试驱动函数Task10PostHopRechargeRoute(earlyPositioning, normalPlan, explicitProbe)。先保留旧earlyPositioning&&explicitProbe观察新增真值表RED，再改为earlyPositioning&&(normalPlan||explicitProbe)。它只选择是否收集/执行原完整回充判据，不本身授权松盾。8个字面量组合覆盖普通与显式试验路线，不能只测常量字符串。变量改为mixedRechargeEnabled，原PROBE环境入口保留。

只在原counterweight的早期定位、post-hop目标/状态、HP1等原作用域内执行；所有安全来源和fresh条件均仍由原代码判定。一次简洁路由回执记录normal/explicit/early/selected，不能当有效松盾或通关证据。

## 验证与授权边界

### 标记对照揭出的后继升降防守

当前单标记对照已证配重8击/HP2→2，随后真实ghost-diver攻击2在7151命中升降乘客、HP2→1；不是回充清场失败。为避免已知后继输入遗漏，追加同版双标记对照：保留回充，仅另启既有KNIGHT_LIFT_GHOST_GUARD_PROBE。它已按同一物理来源/攻击/锁定预警、完整支撑、真实Down+L早采样、零伤害Connected回执及24拍释放上限实现，不改策略本身。

只有双标记实际证实该段保持HP2，才将同一布尔选择提取为Task10LiftGhostGuardRoute(keyboard,normalPlan,explicitProbe)，旧keyboard&&explicit先RED，再改keyboard&&(normalPlan||explicit)。由Drive将headlessNormalPlan传给原Ride方法；所有原检查和必须实际同源零伤害接触的断言保留，使默认测试更严格而非放松。8种路由组合同时覆盖两个独立selector。此为同一默认验路修复任务的后继边界，不是改变玩家功能；仍可能在之后的Grip/战斗/资源门失败。

标记对照不等于默认主线；默认改动后必须无新增实验开关，实际执行原Ranged模型/来源覆盖/完整pre/post/geometry/存档/键盘以及Main、Explore。本地输入与binary清单同版、真实存档保护不变，所有红门照实保留。仅无窗口，root唯一native，运行期间源码冻结；不付费/不显示/不采集额外原生dump/不改引擎/不自动化/不发布/不删用户资料。

这些是沿持续制作授权作出的测试驾驶决策，不是新游戏机制或用户新的风格批准；正常速度美术、惯性/物理手感、硬件真人及原完整十一项均不由本项代签。
