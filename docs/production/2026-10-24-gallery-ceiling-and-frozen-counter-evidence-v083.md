# v083 · 齿轮顶碰撞与探索反击事实

工程仍为 `G:/knight/Knight`。原十一项 ACTIVE，尚未完整交付。用户最后“等一下，是没看到”保持有效：本轮无窗口、无系统键鼠输入、无真人可见/遮挡或视觉认可；不把后台路线测试当作用户试玩。

## 本轮改变了什么

最终代码仅两个测试文件：Traversal中的既有循环观察，以及Main中的Frozen既有决策/攻击回调观察。没有修改控制策略、输入、等待、时间上限、生产物理、地图、敌人数值/数量/AI或正式图声。所有原支撑/跳数/HP/无恢复/原生错误门保持。这是定位进展，不是玩法修复。

### 齿轮：实际跳起后撞到了新增棚板

原Main061435仍失败，但已排除“Space未消费”：发起时P `(2950.9468,3799.9387)`、V `(260,0)`、Space松开，真实跳数10→11、首次采到V `(260,-590)`。最小feetY只有3816.0686，约升20px；最终真实GalleryOne侧面法线(-1,0)、wall/floor为True。

再加一次既有循环内首个IsOnCeiling值快照，Main061900直接证明：在P `(2968.2847,3780.0686)`、V `(260,0)`，碰撞对象是原生保存节点 **GearResidentRightShelf**，法线(0,1)。不是猜测撞墙，不是机械爪失败或输入失焦。两次均保留原Bell下层完整往返、HP4、4次返程跳和同Player。

保存棚板范围x2856–3008/y3720–3744，目标GalleryOne x3040–3280/y3756–3796，角色40×72；32px横向及12px竖向的局部角落不能简单当成开放跳台。事实足以否定旧90px前置助跑在这里的可行性，**但没有证明新的前进路线**。未试一串起跳距离、加跳力、缩身体、关碰撞、移走居民棚或扩大300预算。下一步要协调当前实际抓爪/配重通路与正常前进交接，保留必要回返验证，不为凑绿制造不必要重复乘梯，也不把已经到过Gallery的角色再无条件送回其底下。

### 探索：真实启动、真实命中和资格门已分开

在原K分支之前记录值类型资格快照，扩展既有AttackStarted/Connected，不新增监听/危险重扫/等待/资格门。启动事件来自真实扣费后；direction枚举与左右facing分开。Connected记录实际Hurtbox及父Actor，decisionAge明确只是最近决策的距离，不伪造跨await因果。128行封顶，finally输出；实际omitted3/fault0，内部await中的physics没有完整覆盖。

Explore062312仍真实E0死亡。7次真实启动各正常消耗12/100，总84点；6次Connected都在对应启动后4physics，实际parent等于当时所选对象，没有打错对象的证据。Connected仍不等于每次HP变化证明。

| Attack / 请求loop | 实际扣费后E | Connected对象 |
| --- | --- | --- |
| 22 / 28 | .82399976 | 原Rust4943104708832 |
| 23 / 99 | .5119991 | 同原Rust |
| 24 / 123 | .39199907 | 同原Rust |
| 25 / 163 | .31399915 | bound-ghost居民A155474464543 |
| 26 / 253 | .024665985 | bound-ghost居民B153024990942 |
| 27 / 306 | .0073327445 | 没有Connected |
| 28 / 434 | .024666768 | 原Moth5348912010180，向上J |

已直接复核的单门槛：Rust loop47几何、非攻击、能量及CanBegin均合、无Threat，只被测试驾驶器24帧节拍next52阻挡；51出现新前摇，52又转为Threat阻挡。后段475其他资格均合，只差无Threat（A前摇余.383333秒）；519威胁清除时E仅.076667，522几何也合仍缺能量，523新前摇再次进入。不能称全程没有反击机会，也不能凭.383秒称完整J安全：该案SafeCounterSeconds一直0。

Attack27的几何/幽灵显形原因仍未知。当前选择近居民只认活HP/同层/距离，源码中bound-ghost隐藏可以拒绝伤害；日志没记录27实际Active重叠/显形，不能先归因64px边界或幽灵拒收。下一最小观察是该次实际Active时已有Hitbox overlap、目标Hurtbox/ThreadTarget组与层、HP，分开几何挥空和隐藏拒收；不盲改节拍、Threat规则、范围、能量或900预算。

## 资源边界调查

只读源码追踪未找到Supply路径显式Dispose借入PackedScene/旧Definitions/保存Tuning的实证。CreateSampleFixture仍默认GD.Load→Instantiate，树外替换5个Definitions后AddChild；后者会做场景Instantiate/Free验证再实际挂载。保存assigned与被覆盖临时default资源在此交汇，不意味着7个错误就是7个Definition。

最窄候选是保存assigned PresentationProfile与临时默认Profile退役身份是否混淆，尚未获证明。若后续使用已有完整Supply-lifetime身份诊断，只在现存边界登记同serial/指针的assigned对象，不能持有资源改变寿命，也不能用Dispose(false)或安静一次认定误用。未添加新诊断模式、批量Dispose、强制GC、改缓存/引擎或忽略FATAL。

本轮pre062505最后完成DescentClawGuidance，v082则最后FirstCombat，二者均7Handle及gchandle finalizer崩溃；最后用例名称不是同对象所有权收据。

## 全部本轮原生结果

原件根 `G:/knight/验证证据/october13-stable-claw/`；时间为唯一目录名 `20261024-` 后段。每案原stdout/stderr/external、源/辅助器/存档字节清单、二进制与provenance保留。

| 案例/时间 | native退出 / ms | 严格结果 |
| --- | --- | --- |
| Main061435 | 1 / 125426 | 实际跳起但只升20px、终端侧碰撞，FAIL |
| Main061900 | 1 / 131013 | 首次顶碰撞确认为RightShelf，原失败仍在，FAIL |
| Explore062312 | 1 / 57213 | 7启动/6连接，原Moth Attack3在p2860击杀E0玩家，FAIL |
| pre062505 | -1073741795 / 106003 | 7Handle＋gchandle.is_released FATAL，FAIL |
| post062728 | -1 / 180067 | 原180000ms TIMEOUT，FAIL |
| models063104 | 0 / 551 | 最后普通模型严格PASS |

共6原生、1严格PASS/5严格FAIL、构建失败0，guardFailures均0。三条路线各stderr单条错误与external是镜像，不重复累计；pre8错误含7Handle/1FATAL，post ERROR/Handle/FATAL均0但超时仍FAIL。最后batch退出0只代表models，不等整套绿。纯geometry26、focused96、真实producer消费33本轮重跑全部PASS，不替代原生验证。

pre确切FATAL仍 `gchandle.is_released()`/mono_object_disposed_baseref:1788、Dispose→Finalize→GC.RunFinalizers；最后DESCENT_CLAW_GUIDANCE_RUNTIME_OK及audio-drain，没有pre phase OK。post registry在engine162502ms才开始，35 enter/34 return，最后index34 CooldownHoldNeverEntersPreviewOrAutoFires于179480ms进入。不能把总耗时归于最后case或声称75项全部执行。

## 一次独立审查与root裁决

新上下文最终审查仅比较本轮两个测试文件与v082 models060254 before，不冒称整个dirty树已审。新Critical/Important均无，未进行修补轮/复审。

**新增Minor延期：**Frozen finally开头的GD.Print flush未单独保护；若诊断输出抛错，可能替换原路线错误并跳过后续清理。当前实际fault0且原失败保留，不是已复现故障；列为有条件诊断健壮性债务，不捏造资源原因。既有Title/replay清理Minor也未重审/关闭。代价：极端日志输出失败仍可能影响错误保全，应以明确故障注入合同另修，不能无依据宣称现已安全。

全部Declined逐项裁决及代价：

1. W/排序不变，未重新认证该输入边界。
2. 没有新增resolver/施法授权；顶碰撞不是全图敌人遮挡合格。
3. Intro/Playing准备不变，局部日志不替代启动生命周期。
4. callback/deferred行为未变，攻击身份日志不证明退出父级/姿态/序列。
5. LoadLevel所有权和双异常合同本轮未改，不重签。
6. Gallery只定位未修，代价是正常Main仍不可交付。
7. Root已直接读终态Explore，确认真实启动和E0，但并未完成战斗或平衡验收。
8. decisionAge/lastStarted不是原生逐事件因果，跨await仍须专门证明。
9. 128行/变化去重/omitted3不算完整反击时间线，不能排除未记录状态。
10. Gallery只在现有loop取样、最多8contacts，fault/提前异常可能缺记录，不声称捕获所有失败。
11. 两角bounds仅用于当前已知轴对齐命名夹具，不承诺任意旋转/缩放AABB。
12. Getter/string/输出有成本，未测零开销，不是资源寿命修复。
13. 原十一项、76普通＋Boss/NPC、完整双路/正式视听/存档/硬件/发行/真人均未获两文件资格；真实存档只做原保护字节核验，不读语义。
14. 历史Title/replay Minor仍延期，未挪作本轮已解决问题。

其他执行裁决：root唯一写入/构建/native、运行期间冻结源/帮助器/文档，代价是按dirty快照而非clean commit归因；保留失败与原预算/人口/HP/碰撞，代价是本轮只有新因果事实而非可玩性修复；资源无合法所有权RED不改生产释放，代价是稳定性继续OPEN。

## 保全与后续

6案30对前后清单一致，36封存二进制匹配，缺失0；普通1776源/892C#、9 helpers、6 binaries实时漂移0。最终source清单SHA `B3B6BA0795462A0455D7842091187237AAB29CD31FDE8171690E6E6A9DCA952A`，binary清单 `310DBCAEB4129F8FFA24CD3B6E6A721FC01B043A3099982D54A952948DA5DCAB`，Knight.dll `2D0B87585CD40EB62E6027E6D8719E9AF57027C49844D203189C0B4B7C81AB65`。

分支/HEAD/index仍与v082相同，无Git写入或发行。全部本轮root构建/native已结束，普通构建保留。无新美术/声音/媒体、付费、自动化、前台或删除。正式资产表不添虚假条目。下一继续真实通路/反击/资源所有权，再完整回归和全部十一项；不把观察代码或又一份报告当作完成。

最后手动媒体检查UTC `2026-10-23T22:34:22.4018005Z`：current860284、baseline3508、growth856776 bytes，Notify=False、新增媒体0；无调度/监视器或停工。`git -c core.whitespace=cr-at-eol diff --check`退出0，169行既有LF→CRLF警告保留。目标工具读回仍ACTIVE；没有完成/暂停/阻塞状态变更。
