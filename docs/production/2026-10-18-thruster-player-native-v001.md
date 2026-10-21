# 实际Player短喷接入原生证据 v001

日期：2026-10-18，Asia/Shanghai。完整1–11工具目标ACTIVE、未完成。工作区 `E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`；本报告是未发布的机制中间版本，不是试玩交付、最终美术或整个序章验收。

## 已改变的实际行为

保存Player仍是原CharacterBody2D／碰撞形状；实际Motor已从固定速度Dash改为唯一pulse＋StepFree／ThrusterDynamics。Shift开始有限推力，喷完保余速，A/D有限反向制动；自由空中仍有重力和一次整tick风量。唯一MoveAndSlide后回写受阻分量，薄墙撤除后不会弹出隐藏速度。

同采样原始Space参与地面组合预算，Shift+Space+K仅喷、不普通跳、不cast；长按／松开无旧边缘重放。燃烧中或空中资格耗尽的Shift拒绝不撤爪。接受时专用退出旧cast／驻留／修正，保留cast序号，不使用普通Released的boost、去inward投影或release限速；实际身体速度冻结继承一次。已附着K接管会结束余喷但不退款。

Claw有独立单调AuthorityRevision与发布epoch，早期观察者Reset后旧Reel不再交给后续观察者；同步驻留false通知期间仍可读旧target。Motor完整替换／重置使旧tick失效，不能继续积分覆盖回调的新速度。死亡通过真实Health信号同步结束运动，落地清旧抓爪环境授权。抓爪和喷气的上升JumpCut保护与环境授权已分开；仍需完成下列边界证明。

以上按测试驱动逐项复现后修复。原纯PlayerMotionModel.Step暂供未迁移的旧预测器，不是实际Motor预算／速度权威；不可因此发布中间版本。

## 最新专项

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。每案fresh Rebuild，source库存1685、guardFailures=[]；不是用行数代替版本身份，每案仍保留完整source/helper/binary/user before/after。真实用户档案仅库存哈希保护，没有语义读写。Godot4.7.1 a13da4feb／dotnet9.0.317不变。

| 专项 | case | engine结果 |
| --- | --- | --- |
| 最新实际Player | `thruster-player-20261018-140652-051-3c5af06cb28b49db8956586a71a78fad` | 0／7426ms |
| 实际键盘及落地权限 | `keyboard-20261018-140132-989-a8902ad526994706bcfa995b21d175dc` | 0／9728ms |
| 风区及保存深井 | `updraft-20261018-140508-042-f78eb222043b46faa9dd8cb15e03209c` | 0／9443ms |
| 最新广域pre | `prologue-pre-20261018-140248-957-0e2a1ff21f204af1a0b17df998b3069a` | 1／72771ms，仍FAIL |
| 本轮广域post | `prologue-post-20261018-135707-236-85d9ecb75f6943cf9360ac58f4787306` | 1／8420ms，仍FAIL |

最新Player包含16个真实自动Physics场景：组合原始预算、燃烧中拒绝保爪、耗尽空中次数时Aim／Travel／Reel／Swing四阶段保爪、接受Reel专用交接及实际vy独立对照、早期observer重置、ExternalImpulse／完整Reset两种回调替换、地面第一tick有限喷力与余速／反向制动、真暂停恢复、薄墙、低顶、实际风量、真实Health死亡。另外运行一项旧scheduled fixture的真实附爪终止余喷对照，明确不是物理键组合证据。

新Player专项没有退出GC／managed-wrapper drain；三sink没有ERROR／WARNING／FATAL，fresh build0警告0错误。旧回归runner的既有退出策略未改，不能把旧runner证明力冒认为新无drain证据。所有操作为Godot进程内输入事件，不是OS键／真人操作；没有前台窗口、抢焦点或自动补键。

## 本轮回归及未通过项

第一次同版18门（Player＋17项批次）15GREEN／3FAIL：pulse、input、module、dynamics、旧gravity、keyboard、actions、fall、fall-claw、boundary、contact-safety、arrival-transition、swing-loss、boost-presentation和Player通过；updraft、pre、post失败。其后修复真实落地权限遗漏、迁移两条与批准规格冲突的旧断言并补四阶段NoAirUse矩阵，专项再次通过；**未重跑最后全部同版18门，不能称全suite绿**。大体积boost姿态记录只核对摘要与严格owner全sink扫描，不作为实际渲染／感官证据。

updraft旧断言要求Dash无风、pre旧断言要求喷射与Reel双owner，均与批准规格冲突。迁移后分别验证独立−50px/s风量、真实附爪终止燃烧／不退款／新抓爪释放尾部，而不是为旧测试反改产品或给失败命名豁免。

最新pre通过该交接，首失败转到PlayerTraversalInputTests.SpaceUpDashClawThenDashRejectsSecondDash的旧E＋左鼠标夹具：最终stateIdle，player(0,1640.0693)，surface(-80,-203.65723)，mouse(-80,1546.7122)。源码表明夹具把锚移到上一采样MouseWorld，随后摄像机运动使Aim／Windup重选窗口漂移；raw未记录首次失败phase，不能断言没有cast或产品拉力不足。下一迁移为入树前固定锚＋真实键盘预览／fresh K，记录原始边缘／phase，保留全部预算负控和真实floor，不靠关碰撞／扩大范围／超时来通过。

最新pre没有到旧Upper，所以本案不能抄写“Upper失败／两个profile警告”。旧Upper仍OPEN。post仍BellWaitForFloor(540.0704,1196.0139)与KillZone运行时找不到ILevelRecovery；不是C# parse/import错误。三项旧Handle／154Shape RID／31Shape RID本轮未重跑，仍OPEN；不存在五旧FAIL已关闭的结论。

## 深井操作迁移的取舍

新空中惯性会让原被动松D路线漂到侧台；失败案 `updraft-20261018-135938-650-6e3f04b5ed6d4b0f971667795004f0f8`保留。采用已批准A反向短刹、过零松开，未改地形／风／重力／时限。实测反向6帧、下落4.633秒、253个风样本，原安全底台(2220.1887,3523.9749)，同Player／HP5／零跳喷爪。

这证明可操作的4–6秒直落路线，不证明被动松键必到井底。被动飘到侧台的风险及首关制动提示／正常路线验证仍待处理，不删旧失败记录。

## 剩余边界和完整目标

Task3仍进行中：完成旧PlayerTraversalInput纯键盘迁移；补接受Aim／Travel／Swing、高入射动量、更多回调卸载／重入／暂停与来源撤销边界；同版focused和广域回归；后续Task4可靠接触减伤、Task5保存模块／稳定装配、Task6typed短喷视听／UI与authority失效清旧表现队列、Task7全部旧Dash预测消费者及正常双路线仍待实现／验证。

新50常驻正式仍0/50、24canonical未正式接入，地图127／候选132状态未变。全部1–11中的地图、76普通＋1Boss、机关、正式美术／UI／音频演出、旧档、60FPS／720p–4K、正常AV／真人、唯一版本Windows ZIP／GitHub归档不缩减。没有付费生成、Git提交／推送／标签、ZIP、媒体展示或自动化恢复。资产表没有新资产增量。

计划／rulings／精确分步RED→GREEN保存在 `.superpowers/sdd/2026-10-18-thruster-player-integration/progress.md`，下轮从Task3续做，不重做已完成组件任务，不先宣布短喷整计划或完整1–11完成。
