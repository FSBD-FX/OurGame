# v223 · 真实地面制动、承重和低摆脚

状态：已接生产；唯一fresh审查C0/I2/M1，两Important各自实际RED→GREEN，一次修补无二审。最后同源实际18阶段17PASS/1MainFAIL，另保留两次调用者错误入口的预检拒绝，不算路线执行。原整套未通过，Task1仍in_progress；完整十一项和正常速度动作/物理手感未验收。

## 事实、判断与范围

当前物理已有被动摩擦、反向制动和同一步停零再反向，本次没有重调它们。原真实Player松键后有8次高速减速采样，却0次Brake姿态：普通表现主要等速度降到8以内才播刹停。新数据链让动作读取完成的控制积分，而非仅从渲染速度或按键猜受力。

Motor在原StepFree前冻结速度和真实缩放后的输入，结果保留碰撞前IntegratedX、碰撞后ActualX，最终落伤回调后的authority/存活门内才发布owner/revision/physics tick。退出、暂停、重置、外部替换、模块/爪控制等清观察，停用时getter拒绝旧值；墙壁冲击、燃烧/跳跃/爪接管不冒认普通控制制动。

纯模型按完成物理帧读取被动/反向/过零，保存世界倾角和连续强度；正在减速就进入Brake。共同髋下沉/上身受力替代同帧旧BodyLoad和通用Brake身体模板，不叠加两次。自由脚新增弧以限速过渡降低，支撑点、步长、相位和落点不因“刹车动画”改变。

风险：更低摆脚可能增加腿伸展，因此检查实际18+18连杆和是否夹紧，不能只看目标脚点。2.2px/.13rad等是暂定程序映射，不是用户已认可的精美动作稿，也不等于物理数值已经标定。没有新位图、Spine采购或付费生成。

## 先失败再修补与测试时机更正

根：`G:/knight/验证证据/october13-stable-claw`，以下案尾均保留。

- 72548771：原生2014ms，原代码8次实际减速/0次提前brace，GROUND_BRAKE_NATIVE_RED。无需新接口就触发，非编译失败。
- 8d42144d：原生435ms，新独立控制消费不存在的行为RED，之后才实现两个生产CS。
- 09ac8f7f/a0efa23f：初版模型/实际Draw/全部旧委托通过，不代签后续加强版本。
- 406e6c97：矛盾的“反向输入但声明非反刹”原被接受为被动减速，实际RED；校正为反刹标记与真实轴方向一致，当前模型30控制。
- 077592f2/836e45f9：加强的当前快照断言失败；后者打印snapshot tick70/current71、其余速度/身份一致。既有Frames等待PhysicsFrame信号是在节点物理处理前，原测试注释的“postMotor”不准确。改用已有priority100观察器，实际在Motor0后采样，保留严格同tick断言；没有接受旧一帧或修改生产来遮掩。
- 97b25fb1：校正后的真实完成快照＋9处控制/生命周期否定、无重复BodyLoad、低摆脚/固定支撑/旧全套通过。
- 新host缺少消费者实际RED→精确消费者/2正16负GREEN；没有只grep源码或屏蔽native错误。

## 审查前封存专项（中间版本，不是最后同源结果）

model `18223d66`和gait `74256600`使用Source1888严格PASS；gait原生10969ms/exit0、failure=null/guards=[]/stderr0。

- 模型30控制：被动/反向/同一步过零incoming方向、低高速/左右、owner/revision/回退/断档/失测/无效状态、零/重复时间及30/60/120Hz手算闭式收敛。
- 实际保存Player和priority100采样：松键时速度尚大于8已有Brake；完整Motor owner/revision/tick/control速度与身体速度匹配。9处立即清空检查含爪约束/释放/外部停稳/Reset、暂停/恢复与处理禁用；清空后零时间发表不会保留测量brace。普通测试运动不加外力，清理负例调用明确的控制API。
- 同实际地面轨迹比较旧/新脚弧，持续支撑点和相位保持；真实叶场景200绘制、218支撑、131次明显降低摆脚，包含两个朝向、反向、攻击/盾，固定18+18且无夹紧/无拉伸。
- 同时间把旧BodyLoad从1.5/.1换到0/0，实际CurrentPose相等，证明测量制动承重替代而非重复相加。原spring/mantle/转身接续/air/landing/body/gait等委托保留；host新2正16负与旧控制同跑。

## 开发选择与成本

1. 采用现有Godot的完成控制事实，不改摩擦求“更滑”、不买中间件；代价是动作/力学整体仍须后续正常速度标定。
2. 用户自主推进授权下直接选择有限样段；代价是不能将参数或样段当作风格批准。
3. 原地dirty codex工程、七精确基线/串行native，替代新worktree/commit；代价是仍靠本地保管，无新发布。
4. 最新单步观察和连续承重，不建立新事件日志；代价是被渲染跳过的物理帧不保证逐个单独播放，完整质量仍OPEN。
5. 普通墙面用实际IsOnWall排除，而不是复用要求推进器identity的AdvancedAxisBlocked；代价是侧壁冲击归到独立反馈，不在普通制动动作内伪造。

## 审查、最终回归与保存绑定

不预填最终通过。历史world-jet立即Quit泄漏/Handle/FATAL、Main899和完整双路、R-I/R-C/R-P、正式美术动作/UI/声音/硬件/正常速度真人仍OPEN。无GUI/系统输入/付费/新过程媒体/自动化/真实C存档/额外dump/运行时更换/Git发布或删除。

### 审查修补与证据边界

- I2：原先重复tick/零delta先返回旧值，支撑失效仍保留brace。`69cf5d45`实际429ms RED（unobserved/0）；物理有效性改在缓存/时间门之前，清空时消费拒绝stamp，不恢复同帧旧负荷。七失效上下文×三种时机×清空/不复活新增42检查，`1dfa5307`模型72检查严格PASS（当时源尚未加I1修补，属中间证据）。
- I1：四个爪API只清观察、不改变原运动版本，回调返回后外层可重新发表旧候选。`cf448eea`实际3683ms RED：真实800高落地的Health.Damaged同步Clear后，外层发表Owner40382760811/tick183。新增观察专用generation，在所有清理时递增，Physics入口捕获，最终只否决观察发表；不改变原motionRevision、真实速度、推进器消费或力学语义。真实Clear/Release两个回调都验证；Release的75水平速度不被回滚。
- 当前修补后`3d0cf318`：Source1888，原生13500ms、exit0、failure=null/guards=[]，72模型控制、9处立即失效、2处真实同步回调、200实际叶绘制/218支撑/131低摆脚及全部旧委托严格PASS，host2正16负。
- Minor延期：`actions=true`只表示输入了攻击/盾动作并检查同帧腿、脚底和承重一致，**不证明**武器握持点/手/盾附着坐标已逐个核对。无已观察到的附件缺陷，但正式动作验收仍欠这组证明。

审查前五中间案25/25保存文本对一致；模型/步态/rig/actions通过，world-jet `afa9215d`仍报告2个ObjectDB实例泄漏而严格FAIL。退出码0/末尾OK不抵消警告。这些旧二进制不能给修补后源代签；最后同源回归单独保管。

### 审查未裁定部分的根裁决及代价

1. 原生复现与保存真实性由root串行运行/最终当前绑定，辅助者只读关闭案元数据；代价是技术证据不等于独立硬件或真人验收。
2. 缺失兼容输出先恢复同handle，再以无在途进程及关闭案文件判定；代价是world泄漏仍保留，不能算缺失输出即失败或即成功。
3. 为真实重入增加两回调测试，其余反向/过零/碰撞/外力/死亡沿用已有分层证据但不宣称原生穷尽；代价是完整物理门仍OPEN。
4. 附件正确性按上方Minor延期，不把共用变换的结构推理当实际附件坐标证明；代价是正式攻防连贯性未验收。
5. 保留最新单步连续负荷，不扩展事件日志；代价是渲染跳过的每个中间物理帧不保证逐个播放。
6. 200帧夹具不推广成任意坡度、移动支撑、全攻击链或极限可达；代价是普适地形和动作覆盖尚欠。
7. 仅无窗口，不越权开窗口或发系统按键；代价是像素、美感、正常速度操控和硬件性能继续OPEN。
8. 原Main/资源/world/pre门按原合同/预算重验且保留所有失败；代价是局部增量不等于完整十一项完成。

9. 调用者路线入口错误由调用者纠正，不修改旧live-mode的1695来源保护：原批次40970的`ranged-route-main/exploration`在创建案例/启动引擎前被拒绝，不是路线行为RED，也不表示任何画面已显示。根据v222实际保存provenance改用`thruster-route-main-diagnostic/exploration-diagnostic`，仍无窗口、原600秒预算、无waiver。代价是两次拒绝调用另记操作失败，不能从最后真实18阶段的统计中暗中消失，也不能冒称完成正常显示路线验收。

### 最后同源实际执行表（18阶段17PASS/1FAIL）

| 阶段 | 案尾 | 实际原生ms | 严格结果 |
|---|---|---:|---|
| 测量模型 | f4b6b3c8 | 511 | PASS |
| 完整步态/新旧动作委托 | 3d0cf318 | 13500 | PASS |
| 表现rig | 8de81381 | 2736 | PASS |
| world-jet | 4df32ee3 | 4147 | PASS；不关闭旧泄漏 |
| actions | 1cdfa3a1 | 9410 | PASS |
| application-quit七阶段 | 068e/0bf1/b1f1/cecf/7e00/a61d/7931 | 1547/1540/4140/3309/4351/12030/9049 | 7 PASS |
| keyboard/独立title | ca0d7678 | 19969/7623 | PASS，始终user_visible=false |
| models | a62b8455 | 404 | PASS |
| 原前段 | 2e3397d3 | 156410 | PASS，原180000门 |
| 原后段 | d6a9352b | 192594 | PASS，原300000门 |
| 完整Main无窗口计划 | 0e3db618 | 125762 | FAIL |
| 完整Explore无窗口计划 | 993881c3 | 313956 | PASS，Boss/有支撑出口，非全敌群与正常显示验收 |

前段44有序案例、后段75有序案例完整执行。后段4警告精确对应1条seamless故意抛错、1非法room-profile、2 flow-registry拒绝，限定scope/源函数/计数一致，不是通用warning放行。后段仍`original180Accepted=false/performanceCertification=false`，本轮未扩大预算；前段当前没有Handle错误不证明历史gchandle/Finalize根因已经修复。

Main当前仍在原899帧配重敌群收尾失败：Rust剩1HP、玩家HP2、7次攻击均连接、3次爪、能量原记录.161、gap41.217，恢复0/0，单一原断言ERROR；无额外资源/泄漏警告。不改变敌人/伤害/爪排序、能量、辅助输入或窗口来让它通过，也不据此推断手动一定无法通关。旧Main失败继续保留，完整关卡未通过。

Explore从标题、同一persistentPlayer47546631684完成Boss/支撑出口，恢复0/0；artifact明确23 routeNodes、11 challengeKinds、11 encounterActivations但只有10 encounterDefeats（配重敌群被激活但不在击败列表）、4 mechanisms、2 shortcutDirections、2 checkpoints。`operationCoverageComplete=false/fullGoalAccepted=false/humanValidated=false`；OS写盘次数未提供，完成信号不冒充写盘一次。实际普通计划成功不代签原全部敌人/机关覆盖、正常速度、硬件或真人体验。

### 保管、当前绑定与容量

- 原批次40970与补正无窗口路线批次82846均已终结，后者exit1仅Main；没有为安静日志再跑一轮，没有筛掉任何失败。两次错误live-mode调用在预检被拒绝，未创建路线案或启动引擎，所有真正执行的原生路线均headless。
- 根独立重哈当前1888源码/运行资源、19去重helper、6二进制，全部mismatch=0。源码清单SHA256 `772EBA3E43B278A6F34B6E36E5BEBD93E6D43B2AAACDF1AACF84A73D26272C97`，二进制清单SHA256 `AA03E9DE77355E317836F2E9636FC043E1AD55A65FC5D0FC5C791A509A60076A`，实际Knight.dll `6BD60095D925D87924904C317BA8B919EE341E2D4B9F81CB3FFDE4F65387C6AC`。
- 只读辅助者逐批核对最后18关闭案的90/90保存文本对一致（source/user/helpers/continuation before↔after及fresh-binaries↔binaries-after），37/37保存owned原生身份的PID/executable/argv/终态wait与合同一致（含独立标题）。36份保存源码清单及36份二进制清单各自同一SHA，19helper无冲突。最后原生日志只有4条精确限定post警告与1条原Main断言ERROR。辅助检查不含全部封存载荷重复重哈，根当前绑定另行完成；中间五案不替代最后18案。
- Git分支`codex/task6-certified-final-20260902T0555`、HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA256 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变。无Git写入/发布、原件删除或真实C存档更改。
- 2026-11-03 15:45:35UTC流程内媒体检查7,427,750bytes/86文件，Notify=false/NewThresholds=0；没有新过程媒体、自动化或独立通知。G当时剩130,611,986,432bytes；不因20GiB规则停工或自动清理。

## 下一步和未完成范围

当前完成控制事实已能进入高速减速表现，但幅度/节奏仍只是开发样段。下一动作目标是起步→短喷余速→反向承重→转身换支撑→跳落→攻防/爪完整接续，并把实际停止距离、反向可控性、关节与披风/灯/武器放在同一正常速度样段验收，而非只证明单个状态能切换。仅无窗口授权下继续程序与兼容制作，不冒认精美动作已获认可。

Task1和完整十一项继续in_progress/ACTIVE；Main899、历史Handle/FATAL/ObjectDB因果、完整双路正式验收、R-I可感知惯性/R-C角色专属动作/R-P物理与手感、正式美术动作/UI/声音/演出、正常显示和硬件真人都仍OPEN。两Important有真实RED→GREEN但整套不绿，不能写task-done或complete；Minor如上延期，无二审。
