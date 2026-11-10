# v125：真实锈甲格挡与方向交接

本轮是原十一项的局部验证进展，未完成整关。KNIGHT仍在 `G:/knight/Knight`；用户最后的窗口反馈是NOTSEEN。仅改两个测试C#，没有生产难度、数值、地图、人数或正式图声变化。

## 实际改动和结果

在原先同一轮危险扫描中，补充已知Rust的近身预告方向判断；保留真实Telegraph阶段、剩余时间、全部来源的危险和反击否决。只有完整扫描排除未知/重叠Active后才申请转向。只有同一实际Hitbox对同一玩家Hurtbox的成功Connected，与真实NormalGuarded零伤害、连续Active epoch、新鲜键盘样本、HP/支撑/身份不变同时成立，才将这个已消耗来源移出朝向竞争。没有创造J窗口，默认null策略不改变其他消费者。

先以空实现运行真实RED：13近身、5epoch、15receipt合同中恰好6个正例失败；随后最小实现通过新旧合同。Main实际请求Right在7218，剩余预告0.13333315秒；7226同Rust实际格挡并Connected，7227实际转回Left，HP均1且完整支撑。7227是A方向脉冲，Vx=-36.666668，不能说成不移动的瞬间瞄准；7226则axis0、rawA/D=false、V0。contacts1/returns_left1/fault0。

同case Rust actor17371431438934、hitbox17371498547784、playerHurtbox47278196258、attack5/epoch1、transition54；7226序列7197，7227序列7198。后续7255仍由resident Burst4771809332304/round18杀死，能量0、盾未激活。随后正常恢复使原监测器拒绝3123.57的位置重置，不是已证新的传送bug。更早7054与7078的伤害也未豁免。

探索091726：8547由ghost-diver `.counterweight-lift.03` 攻击1使HP2→1；8770完成FullTop132/300接近、HP1保持；9046在Grip2准备第一帧由bone-wanderer `.counterweight-lift.01` 攻击1使HP1→0，Space/K尚未发。不是v124的8546/9045，更不是旧8799/HP2→1案例。

## 本轮八案

全部目录位于 `G:/knight/验证证据/october13-stable-claw/`；八次构建均0警告/0错误，ExpectedFailure空、guards0、无超时。5严格PASS/3FAIL包含1次有意RED，不把父批次exit0当游戏通过。

| 目录 | strict | native exit/ms |
|---|---|---:|
| ranged-mixed-early-boundary-20261026-085629-263-db1e60784b3d4d3293f53756beddd02a | FAIL/RED | 1/534 |
| ranged-mixed-early-boundary-20261026-090345-138-13b80635254e44dea4cc6214df2efb31 | PASS | 0/1689 |
| thruster-route-main-diagnostic-20261026-090414-332-c9909d34e556494fbb86567b63e9f4fe | FAIL | 1/133508 |
| ranged-mixed-early-counter-20261026-091652-850-c4152ede486b409a9584772a076f11f9 | PASS | 0/4129 |
| thruster-route-exploration-diagnostic-20261026-091726-020-5a9ef849505646b9860634d3ad0d479e | FAIL | 1/160901 |
| prologue-pre-20261026-092038-542-a3cc910f84e747f2a63dbb7d00197c93 | PASS | 0/173705 |
| prologue-post-20261026-092402-803-c640b5a9ae714d0fbcedf3155facfbaa | PASS | 0/199520 |
| models-20261026-092751-139-670782b593cc4b539e7ea3bd7b8a6967 | PASS | 0/389 |

三个FAIL的ERROR按stdout/stderr/external分别0/1/1；post WARNING为0/4/4，其余WARNING为0。全部无FATAL，但本次安静pre/post不关闭历史FATAL，不代表实施了资源修复。post原75 enter/75 return，索引0–74齐全，同owner8次nodes1→1，普通完成标记各1、CONTROL0，三sink警告/scope多重集一致。

## 唯一审查、全部裁决与延期

fresh `/root/v125_guard_review`（gpt-6-astra/high）核对物理v124基线及当前SHA：新增C0/I0/M2。根按实际影响复评同意；两Minor只影响诊断或覆盖，不是本case实际7226/7227的误收，不进入修复pass、不复审。完整[审查处置](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v125-final-review.md)。

| 未认证范围 | 根裁决 | 错判成本 |
|---|---|---|
| 1.错来源Shield事件 | 必须实际RustHitbox与THIS Hurtbox配对，单盾事件不够 | 错认其他命中为已消耗 |
| 2.重入 | callbacks只记录，输入由原外层发出 | 回调中乱序改键 |
| 3.旧样本/间隙/新attack | epoch及原身份/时钟门失效即拒绝 | 过期格挡放行 |
| 4.withdrawal/rearm/外部Begin | 正常Rust发布entry有epoch；不认证任意外部同ID Begin | 超域重启被误认 |
| 5.parent/receiver/recovery变化 | 原门和订阅清理保留 | 恢复后借用旧身份 |
| 6.未来碰撞 | 发布几何是pre-move观测，不是下一帧碰撞预言 | 迟转或碰撞 |
| 7.瞄准原点 | 使用实际Hitbox→Hurtbox来源及实际输入；不泛化任意原点 | 错盾方向 |
| 8.J时序 | 所有owner Counter/Threat及原窗口不变 | 借朝向豁免造J窗 |
| 9.未知Active顺序 | 实现完整扫描后否决；负例接线覆盖不足列Minor | 顺序变动漏威胁 |
| 10.默认兼容 | optional null保留旧消费者 | 其他路线被改变 |
| 11.运动/支撑 | 原生证明真实方向脉冲与完整支撑，不称瞬间瞄准 | 把位移隐去 |
| 12.失败清理/借用 | 原finally解绑；借用资源不Dispose，旧flush风险仍延期 | 漏日志或资源寿命破坏 |
| 13.速度/ETA/数值/查询/预算 | 没有猜440、没有从导出Settings冒充私有runtime速度，无新扫描/await/等待池 | 不安全松L或放宽取绿 |
| 14.五旧Minor | 未改、仍明确延期 | 把遗留问题冒称已修 |
| 15.整关/资源/正式视听相机/硬件真人/发行 | 全部仍OPEN；保管独立核查，不借review背书 | 交付未完成游戏 |

实施裁决：继续原脏分支、计划、BASE，不新建goal或task-done，不Git写入；代价是必须使用物理快照归因。保留成功局部握手和后续E0失败，暂不实现缺少物理上界证明的松L策略；代价是原Main仍FAIL。只读核查现成工具和官方资料不等于允许下载/安装/attach原生调试器，先前相关选择仍待答；其他安全工作继续。

新增延期Minor：M1 withdrawal后旧contacts/transition仍可让后续无关Left样本增加诊断returns_left；未来须保留非零同transition/currentepoch或撤销pending-return。实际7227的receipt非零且同epoch，因此不据此推翻本case局部事实。M2 “unknown Active before/after Rust”纯predicate反例没有实跑scanner两种顺序/consumed Counter/Threat接线，未来需真实消费者夹具。

五旧Minor：early-failure energy-unwind flush、boundary的swept措辞、finally stage4摘要、fresh LateFact来源标注、旋转矩形world-AABB错误替代的负例覆盖。总7项延期，未宣称修复。

## 保管与边界

审计 `/root/v118_custody_audit` ENDED，最后fe34fd/exit0，无审计handle/子会话；根97835/14768/24804均terminal，16记录native已结束，CIM KNIGHT Godot/dotnet/owner各0，之后才写收尾文档。

- 40/40 manifest对字节一致，4032实体副本=3942源码+90helper逐SHA一致；48sealed binary一致，缺失/extra/fault0。
- 最终7案14份source清单同SHA；当前1844源/C#892/Debug6匹配，漂移0；helper联集12路径单版本且当前一致。
- 16份user manifest同BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB，未读真实存档语义。
- 最终source SHA53B9A2EC76966EB34AA0027D796F79059E37230AF6DA79A3360508CF7F703141；binary44B80318511D37E43EC896511491C3DDBD91D4940F00A10F4460D985FDFE624B；helperA1AC76A1CBEEAF2061E4787C7AA4191B2FE6831CCBD6640BC2EE66C2E403B8F1。
- 物理v124→最终仅Main250+/12−、Placement70+/0−，合计320+/12−、零源增删。当前Main6B3EBB6529354D6601A0546C6CBC64389D8DB46EA83BCB442B5CD7E5E732F59C；Placement0D3C6E3F36BB6416CC7CCAD676CF60D3CC2387E881D1DA12B3B14D36B3FB66D3。
- HEAD/branch/index/owner/audio/Claw/Skitter锁定值未变。未改生产代码、正式资产表，无新过程媒体/付费/自动化/OS前台/Git/删除。媒体单次Check UTC2026-10-26T01:24:34.0706258/current860284/base3508/growth856776/files10/Notify=false，不重复运行。

原十一项所有稳定ID、原76普通＋Boss/NPC、机关、正常主线/探索、死亡存档继续出口、正式分层视听动作UI/相机、目标硬件真人和授权交付仍ACTIVE。下一步优先区分可玩性缺陷与自动策略限制，推进有确定性证据的资源寿命修复；不把继续增添诊断合同本身当成游戏完成。
