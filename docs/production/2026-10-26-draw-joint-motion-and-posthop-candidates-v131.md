# v131 · 绘制关节连续性与主线候选取证

2026-10-26，KNIGHT，`G:/knight/Knight`。原十一项ACTIVE/NOTSEEN；本轮没有改变游戏画面、动作姿态、数值或选择规则。新近敌优先规则随后获得用户明确批准，另见[新规格](../superpowers/specs/2026-10-26-near-enemy-keyboard-priority-design.md)，不能倒签成v131已经实现。

## 事实、推理、风险

事实：旧节点矩阵不能发现Draw肘腕突跳。保存Player真实腕点跳80、所有节点/CurrentPose不变的负例实际RED后，新四点probe能够拒绝；472个1/60时间线帧/1884关节对最大28.994555，原32阈值未放宽。这里只证明采样与边界，不证明正常速度美观或像素。

主线同一次生产W预判的快照，记录4个仍在actorRoot直接子级的Moth对象（有效target、未排队删除；不冒称全部活敌）。相关ghost处于Align、HP2、available/group真，anchor(4794.886,3205.7988)、距离221.39691、偏W .17095155rad≈9.795°；被选中的AscentGrip2为(4757.222,3120)，距离303.969且角度0。未额外Resolve/LOS/等待/按键，unselected LOS明确未知。Main仍7376死亡，取证不是通关修复。

推理：不能通过盲换NE解决该冻结姿态；ghost对NE偏35.205°，超22.5°半锥。地形排名能够解释候选竞争，但未证明未选ghost的LOS。8月旧敌人优先模型已被8月29日修订/10月键盘计划替代，不能拿旧规则断言当前生产违约。用户这次新批准才提供修改权限。

风险：最终post虽原75完成、engine0，仍有4个Handle错误，严格FAIL；旧资源问题未解决。没有新显示授权，不重新弹窗或用后台测试冒认人眼认可。

## 实现与有效TDD

仅3测试C#，相对v130物理基线275+/0：ProloguePresentation49、PlayerFormalVisualTests170、MainRouteTests56；Source1852/C#892，无源增删、生产/资产/helper变化。

`PlayerArmMotionProbe`从真实Arm.Transform×ArmElbowLocal/ArmWristLocal取得4个formal-parent标量点，排除整个人物世界位移/root mirror，沿原32连续帧门。原节点/武器位移/世界相机15点不删。非有限/重复倒退帧拒绝，观察缺帧或换身份重建基线，不伪称连续；全部点通过后才提交，保留的只有标量，不持有native资源。实际presentation owner把4点送入原motion accumulator。

161752有效RED：actual_jump80/rejectedFalse；162055实现后GREEN。162618增加精确32/32.01、帧序/缺帧控制，以及真实Timeline+rig的472帧动作样本（非物理游玩/渲染证明）。原36/378/父变换/原3爪和75注册保留。

主线候选只在第一次posthop preview callback复制已有状态。审查发现新catch只增加旧mixedObservationErrors，旧C#/canonical均不消费该计数；之前声称“已有fault veto”不成立，明确撤回。根维持Important，并用共享capture/admission路径修正：

- 164425真实生成Player/机械爪Aim事件内读实际preview后故意抛错，事件隔离，attempt1/complete0/fault1，但空准入仍接受，真实RED。
- 164757相同事件与计数准入拒绝；14字面量完整/部分/掉行/错误/非法计数/可选未触发控制通过，原机械爪各阶段仍继续完成。
- Main正常遭遇完成后才检查：未尝试为可选；尝试须恰1次、完成1次、fault/drop0。保留原try异常先展开，不在finally用新诊断覆盖原通关失败。

一次fresh review C0/I1/M2，根同意分级，I1单pass有效RED→GREEN；无复审。最终完整suite仍因Handle严格红，不task-done，不声称全部测试或整关完成。

## 全部原生结果

证据根`G:/knight/验证证据/october13-stable-claw`；12案8严格PASS/4FAIL，全部build0/0warning/error，ExpectedFailure空、guards0、无timeout。

| 时间/类型 | strict | engine exit/ms |
|---|---|---:|
| 161752 Draw RED | FAIL | 1/793 |
| 162055 focused | PASS | 0/4570 |
| 162618 focused | PASS | 0/4591 |
| [162802 Main](../../../验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261026-162802-534-63511a1065a343e7958e9b6d0faab684/engine.wait.json) | FAIL | 1/132859 |
| 163046 pre | PASS | 0/151167 |
| 163345 post | PASS | 0/194173 |
| 163725 models | PASS | 0/402 |
| [164425 I1 RED](../../../验证证据/october13-stable-claw/thruster-formal-claw-phases-20261026-164425-564-a9df97119ce144cfbfe3061c446b211b/engine.wait.json) | FAIL | 1/783 |
| [164757 I1 GREEN](../../../验证证据/october13-stable-claw/thruster-formal-claw-phases-20261026-164757-200-563ff014bfb04d5ebeeaa2981a04aefb/engine.wait.json) | PASS | 0/4574 |
| 164918 final pre | PASS | 0/151611 |
| [165215 final post](../../../验证证据/october13-stable-claw/prologue-post-20261026-165215-856-9a7f48c131914d5eae2fde0db52756cf/engine.wait.json) | FAIL | 0/195971 |
| 165559 final models | PASS | 0/403 |

两个post均75 enter/return、owner8 nodes1→1、完成marker各1、CONTROL0，原4warning合规；最终post另有4个Handle is not initialized（E0/4/4，FATAL0），不得以engine0或之前旧post覆盖。最后四案同源，早期Main不冒认最终冻结资格。Explore未重跑、旧失败保持。

## 全部审查裁决与成本

[唯一审查](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v131-final-review.md)的26项Declined逐项处理：

| 项 | 根裁决 | 若错判的成本/未闭合边界 |
|---|---|---|
| 1 项目/基线 | G根KNIGHT，使用纠正后的v130实体 | 不触碰UE，错基线会误归因 |
| 2 Draw来源 | 与实际DrawArm同getter | 不拿节点原点代替腕点 |
| 3 肩点 | 原节点motion继续保留 | 不漏旧节点位移 |
| 4 世界移动 | 排除整体travel | 否则正常行走误报 |
| 5 root mirror/lean | 按原bitmap通道排除，另做世界bounds | 不是全屏像素抖动证明 |
| 6 预算 | 两处仍32 | 不宣称32以内即美观 |
| 7 非有限/overflow | 有限检查/超限否决 | 专门负控仍为M2 |
| 8 重复/倒退帧 | 同身份在采样前拒绝 | 不能用重采擦基线 |
| 9 缺帧 | 新基线，原accumulator也只比邻帧 | 不补造连续性 |
| 10 换actor/arm | 按native ID区分 | 替换负控待补 |
| 11 部分提交 | 四点全验后提交 | 不允许错误样本污染 |
| 12 返回数组可变 | 当前internal caller只读 | 未来误改仍有风险 |
| 13 多player | 当前演示owner一个player | 并行多角色不是本轮认证 |
| 14 节点缺失 | 抛错而非放行 | 诊断质量/负控仍待 |
| 15 80负例 | 真实保存Player Draw改变 | 不只是算术fixture |
| 16 阈值 | 32及32.01真实控制 | 未放宽 |
| 17 时间线 | 真Timeline/rig，输入合成 | 非自然游玩或像素 |
| 18 原动作 | 原注册保留 | 新序列非穷尽 |
| 19 生命周期 | 原Player finally；probe标量 | 不猜资源owner |
| 20 性能 | 测试侧小分配/一次扫描 | 不是实机性能合格 |
| 21 callback只读 | 不Resolve/LOS/输入/等待 | 不宣称绝对零时序扰动 |
| 22 available含义 | 不是LOS或交互成功 | 原始记录明确unknown |
| 23 遍历人群 | 仅直接子级有效Moth/target | 不冒称全76/全部活敌 |
| 24 完整性 | cap4096不增，I1后尝试须完整无fault/drop | 不追认旧准入 |
| 25 生产/资产 | 本轮未改 | 不声称增加美术内容 |
| 26 旧目标门 | FATAL/双路/硬件真人/发布继续OPEN | 窄审不减原十一项 |

其他Ruling：在原测试presentation文件内加入标量probe而不重构SceneTree，成本是仍须owner-path mutation证明；使用真实保存Moth/实际Timeline接口而不伪造normal路线，成本是无法由合成输入直接判审美；原Main异常先展开保留，成本是失败路线上新完成准入不执行，不能据此宣称已成功通关；不按新优先规则倒签旧Main，也不因新许可跳过当前失败留档。

Final: minor (deferred): M1实际presentation owner hook移除的mutation负控；M2非有限/缺失或disposed节点/视觉或手臂替换的专门负控。v130节点盲区的实现已补，但其完整链覆盖延续为M1，不当作所有质量门已关。原10类继续：energy-unwind早失败flush、swept措辞、stage4 finally摘要、LateFact来源、rotated-AABB负例、withdrawal旧return、unknownActive真实scanner、cache-helper实体缺失、v127 shot上游物理/input/capsule独立破坏、v129内层Free双错归因。共12类；旧14份实体缺口不追补。

## 保管、容量和后续许可

[独立保管](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v131-custody.md) ef4d77 exit0，UTC09:01:13.4292519Z ENDED后才写本文；60清单对/5876实体/72binary全部匹配，新增缺失0，当前1852/892C#/Debug6一致。24记录owner均结束；8760只有零线程/句柄、无path/argv身份不定残留，不称全局CIM0，不kill/attach。

本目标回合唯一手动容量Check UTC09:03:26.3131603：1431609字节/16files，基线3508，增长1428101，Notify=false，无新媒体。没有付费/自动化/Git/删除/新前台输入或弹窗。

用户随后明确允许“近处、可交互敌人优先于它后方地形，真实遮挡/射程/禁抓材质保留”。此许可解决新规则选择，不等已实现或视觉认可。下一实施需真实native RED→GREEN、距离/遮挡/死亡/缺adapter/机关/稳定排序及正常路线回归，不恢复8月旧算法或伪造选中目标。
