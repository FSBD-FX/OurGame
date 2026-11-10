# v077 · 释放实轨迹与首次战斗启动边界

2026-10-24；唯一工程`G:/knight/Knight`，完整十一项目标ACTIVE。本轮是有实际证据的诊断推进，不是整关、美术或资源稳定性验收。上一轮只更正“没看到窗口”，没有新增制作进展；本轮重新核对工程、Git与空闲原生进程后继续。没有打开窗口、发送系统输入、新美术/声音、付费、自动化、Git写入或删除资料。

## 事实、推理、风险

旧失败的Arrived/Swing→A＋Space→D现在有150个连续post-Motor实际样本，保留原150总物理窗口和跳跃/短喷/HP/AI/地图。第一次重放因诊断外层误继续正常行走而整案FAIL；修正诊断返回后两次严格PASS。正常早Reel与原passive无Space驾驶器不变。

最后013750案：physics720真实Released，Motor速度`(-70.04907,-532.79486)`，释放动量=true；发布回调的`(-13.418116,+14.086057)`是释放前速度，不可混用。physics725首次完整身体越边且Idle，底边3719.312＜原3719.75阈值，净空共24行。150行GalleryPreview均false。

首净空仅一次独立native ray命中活HP6的`precision-heavy.tscn`，路径`CanyonBasin/Gameplay/Actors/BellBackLaborer/ThreadTarget/TargetCollision`，Gallery LOS=false。它是`canyon.v2.resident.gear-pistons.07`，home(2924,3678)，巡逻2924→2892→2956→home，站GearResidentRightShelf，不是旧Gear Bell波次。其他预览运行期ID没有路径映射，不冒认都是此Heavy。

判断：释放、上升、完整身体净空实际成立，不能再简单归为牵引力不足；下一查真实敌人/落点/战斗衔接。一次ray不证明它挡满150行。初次失败案的意外后续控制虽然曾站稳Gallery，也不作为正常路线通过。

FirstCombat专项和完整pre均明确Title/pending=false，原因同为`canyon.v2.resident.low-ceiling-bridge.01: end.native_hazard|start.native_hazard`。专项观察44physics/761ms且没有Handle错误；pre观察35physics/464ms。不是已证实“60 ProcessFrame太短”。只读候选为rock-wanderer左巡逻端(602,604)的radius18/height64 capsule与原Rust攻击Area相交；Hitbox.End只关闭Monitoring，不移除layer/shape。实际admission当下的hit身份/状态未留，不能笼统忽略EnemyAttack或移动起点绕过。

资源故障仍OPEN。fullpost在registry前发生托管失败，随后失败清理时gchandle finalizer FATAL；原始托管消息尚未在清理崩溃前输出，不能猜首错或把旧最后VFX case当原因。本轮没有registry engine_ms行，新增时钟尚未证明旧timeout时间起点。

## 实现与审查

`BasinCounterweightRuntimeTests.cs`新增独立旧输入重放，沿用原owner清理、160槽/150tick，记录完整身体、raw/sample输入、current/published帧、PreviewId/ReleaseMomentum；首body-clear独立唯一LOS，不用它授权选靶。`RunStableClawPullTests.cs`新增两诊断入口，不输出普通成功；VFX只选原注册的VerifyRuntimeFrameBinding delegate，经原registry执行，5＋1Process/240Physics/typed音效特效/原清理不变，fallback恢复原值。

`FirstCombatGuidanceRuntimeTests.cs`只加旧断言前失败标量；等待与Playing断言不改。`RunPrologueTests.cs`仅增加同一次读取的engine_ms/elapsed_ms，75项/180s/清理不变。owner增加规范拼写、无混合flags/waiver、三个raw sink否决、唯一有序完成/排空、实际150行/cast/输入/净空一致性；helpers纳入新诊断保全清单。

实际入口AST准入RED2/96→GREEN96。消费者两个本地缺口RED→GREEN：额外畸形exit被接受；无body-clear的完整诊断被误拒。唯一fresh gpt-6-astra/high只读审查无Critical/Minor，一Important：消费者未验证定义性的输入顺序。真实producer变异“去掉全部Space”先RED，再修成arrival→一次同cast fresh raw/sample A＋Space→松Space且D，检查合法CurrentCast/连续sequence。最终33个真实owner函数合同GREEN（包括错A、缺D、外来cast、释放先于到达和完整但未净空），原生再复测PASS；无复审、无游戏手感修改。

所有declined事项的裁决及错误成本：

1. fullpost终态以root原件FATAL/FAIL为准，不据审查推定通过；否则会掩盖崩溃。
2. 正常Main/Explore、全76＋Boss、全部十一项OPEN；诊断不能替代，成本是后续完整验收仍需做。
3. 最终视听、真人可见、60FPS未审阅/未确认/未测；可能仍有相应缺陷，不追认显示。
4. quiet VFX叶不证明GCHandle/FATAL/RID修复；真实长流程资源风险仍在。
5. FirstCombat有独立无Handle失败，先追hazard；不关闭另一资源问题，可能有两条独立故障。
6. LOS只确认首净空一次Heavy遮挡；未测时段可能还有其他障碍。
7. 不改生产排名/receiver资格，不称新路线成功；战斗/落点策略仍需实测。
8. 回调退出/LoadLevel保持原所有权，未提供本轮新异常注入；未测边界可能仍有缺陷。
9. 原VFX异常清理不改；正常清理PASS不证明所有异常安全，旧异常泄漏风险未关。
10. Basin旧快照含更早normal变更，仅核当前分支隔离；无法把所有脏改动归为本轮。
11. 全仓脏变更、真实存档迁移、发行不作批准、不读真实存档/发布；仍需完整兼容交付审计。

仍按用户指定已有codex工作区执行，不建worktree/提交/删计划；成本是靠明确快照限定归因，不能声称清洁分支全量验收。

## 原生结果

原件根`G:/knight/验证证据/october13-stable-claw/`；目录=phase＋下列时间＋唯一UUID，provenance/完整三日志/前后清单/源码与工具副本/二进制保留。

| phase／时间 | exit／ms | 严格结果 |
| --- | --- | --- |
| vfx-frame-binding-leaf 011648 | 0／5473 | PASS；实际ENTER365→RETURN5213，不是wholepost |
| basin-swing-release-trajectory 011752 | 1／34621 | FAIL；150行后外层误继续Walk，原件保留 |
| basin-swing-release-trajectory 012129 | 0／32246 | 修正返回后PASS，真实净空及Heavy阻挡 |
| first-combat 012355 | 1／2315 | native_hazard/Title FAIL，无Handle |
| prologue-post 012618 | -1073741795／157483 | registry前失败＋清理FATAL，非timeout |
| prologue-pre 013042 | 1／111174 | 3Handle ERROR＋FirstCombat Title FAIL |
| basin-first-reel-trajectory 013325 | 0／23703 | 原无Space/jet被动轨迹PASS |
| vfx-frame-binding-leaf 013420 | 0／3951 | 原VFX叶/退出排空再次PASS |
| post-boundary-contract 013452 | 0／521 | 原registry分类/变异＋观察合同PASS |
| models 013525 | 0／518 | 模型PASS |
| basin-swing-release-trajectory 013750 | 0／26474 | 审查修复后原生150行/有序输入PASS |
| models 013848 | 0／523 | 最后普通模型/产物PASS |

12案8严格PASS/4FAIL，不是同冻结整关全绿或完成比例。60组source/helper/user/continuation/binary前后清单字节相同，missing0/mismatch0；live1776源/892C#漂移0。source manifest SHA256`9C0434CD6325C2E3FCDB721CE3CCF68DB27B6DCAE9E3E4F702A194A880666002`，binary manifest`AF5F76937719C8A005F87FFE66343A77D714DE502CB572918EA3BD8956E4B4DA`，Knight.dll`120F8E0BC4D700CA0350EEFD66A249A957B2B435087AC8114981E55CA7C4B58F`。HEAD仍e7c70498…/index21610FDC…；root原生/构建已终态，无关桥未操作。

汇报前一次媒体Check UTC17:40:42：current860284B/base3508B/growth856776B/Notify=false，新媒体0；没有自动化、独立通知、停工或删除。

## 下一步

出生拒绝处同步保全实际hit/shape/监测/攻击状态，不额外Load或第二次选靶；以有效/无效危险反例确定修法。齿轮区按实际支撑、敌人身份和战斗/落点衔接，不猜第四个发爪阈值、不改HP/关敌/搬地图。失败清理前保全原托管原因，继续资源身份调查。正式美术/动作/声音、正常Title双路线、全人口实战、真人/硬件及发行等原十一项均未完成，目标不标完成。
