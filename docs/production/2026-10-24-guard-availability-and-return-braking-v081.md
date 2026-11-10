# v081 · 护盾可用性、返程刹车与地形校验

v082复点说明：下文045248旧“20 Kinematic/2未知”的划分混用完整行与可识别token，原日志不改；准确为334完整unsafe错误头＋2穿插断裂消息共336，类型可辨311 Ray/21 Kinematic/3 InputEventKey，余1截断Ray前缀。详见[v082计数更正与未完成边界](2026-10-24-foreign-defense-and-full-gear-roundtrip-v082.md)。这不提供具体资源所有者证据。

唯一工程仍是 `G:/knight/Knight`（Godot），不是另一项目 Ourgame3d。原批准十一项继续 ACTIVE，未交付正式试玩版。用户最后更正“等一下，是没看到”保持有效：本轮没有显示窗口、系统键鼠输入或新的真人画面认可；不能从旧标题页确认推定本次看见了。

## 本轮实际改变

本轮代码变更限于一个路线测试、规范验证器及一个新纯测试，没有修改生产敌人、移动参数、地图、血量或能力数值，也没有新增正式美术、动作或声音。

1. **自动试玩防守不再擅用35%保留门槛。** 原 Main045248 在 physics4430、Bell Attack4 的前摇仍余0.183秒时，旧门槛为False，但实际 `shield.CanBeginGuard=True`、尚未过热。原战斗玩家4→3的失败保留。只把 GearBell 驾驶器门槛换成实际护盾可用性，原整段前摇、J/K75、900帧、HP及无恢复条件不改。后续真实原Bell战斗结束且HP4→4、伤害事件0；这不是完整主线通过。
2. **返回台阶采用真实模块刹车预测。** Main050914 的失败快照为 floor_contact=False、full_width=True、x_range=True。松A时位置2413.659、速度-260，实际摩擦1800，旧连续公式却用Tuning2800；真实离散中立滑行约16.667px，旧估算12.071px。最终停2400.001与邻墙边界相符，但本案没有精确墙碰撞收据，不把推理说成已证实的墙因果。仅复用现有 `FreeMotionForecast.Neutral`，传live MotionSettings/module、真实tick、原12步和2速度阈值；原目标2402、30+12、精确向上台阶接触、全宽、2399.75–2406及全部跳/盾/HP/140阶段合同保留。Main051506 实际释放2417.9922→停2401.3262/V0/完整支撑/HP4，随后在新的护盾助跑窗口失败，未完成完整返程。
3. **修正规范地形收据的旧127。** 实际生产者已报告143。纯测试先执行旧消费者的两个真实AST分支，26例中4个预期不符：有效143的LF/CRLF被拒绝，旧127及混合143+127被接受。提取纯 `Assert-ResidentGeometryReceiptContract`，要求唯一完整、大小写精确的143收据；原14行协议、保存叶证据、所有原生错误否决保留。新测试同时进入geometry的两份helper保管清单。26例最终通过，实际旧FATAL残缺日志仍被拒绝；两个真实geometry专项也严格PASS。不把收据修正说成资源崩溃修复、完整放置或动态通行资格。
4. **探索新增有界观察，没有先改防御。** 真实p2433与2454证明，选中Moth的ThreadStunned/Recover特例会把另一真实Active攻击者要求的generic防御True覆盖为False，护盾当时仍可用且玩家未攻击。p2454的危险Actor153343757993/Attack1与p2460致命原生收据完全配对，stableId为 `canyon.v2.resident.frozen-detour.04`、bound-ghost。下轮可只保留Moth分支的foreign-Actor既有generic结果；尚未实施，也不预称修正必然挡住或存活。

探索五次实际损失仍为：p934 rock-wanderer 5→4；p1557原Archive Skitter 4→3；p2056来源未知3→2（不能猜成某机关）；p2192真实639.999落差、Landing13产生1伤；p2460上述bound-ghost 1→0，恢复请求1/完成0。没有豁免落伤、删敌人或增加恢复。

## 全部本轮执行结果

原件根为 `G:/knight/验证证据/october13-stable-claw/`。表中时间是目录名 `20261024-` 后的唯一时间段；各案日志、原源文件/辅助器、原生运行合同和provenance均留存。

| 案例/时间 | native退出 / ms | 严格结果及边界 |
| --- | --- | --- |
| Main045027 | 未启动 | 构建FAIL：诊断误猜PhaseRemainingSeconds，改为实际RemainingSeconds；不是行为RED |
| Main045248 | -1073741795 / 100513 | 原HP4→3 RED，之后真实资源FATAL，FAIL |
| Main045725 | 1 / 98981 | 原Bell战斗及HP4保持；下一个80px起跳支撑断言FAIL |
| Main050914 | 1 / 114819 | 明确台阶接触False、宽度及位置True，FAIL |
| geometry051239 | 0 / 43399 | 修正收据后全部原协议/实际叶证据严格PASS |
| Main051506 | 1 / 134461 | 台阶真实站稳通过；下一护盾助跑窗口FAIL |
| forecast051906 | 0 / 611 | 实际模块离散预测正反例严格PASS |
| geometry051958 | 0 / 34082 | 最后冻结代码下规范geometry严格PASS |
| Explore052110 | 1 / 48414 | 真实HP0/恢复请求，foreign覆盖被观察到，FAIL |
| pre052245 | 0 / 175017 | 前半功能全部返回，但4条Handle错误，严格FAIL |
| post052615 | -1 / 180277 | 原180000ms上限TIMEOUT，严格FAIL |
| models052948 | 0 / 569 | 最后普通模型严格PASS |

总计12案：1构建失败、11原生中4严格PASS/7严格FAIL。最后纯入口96、旧真实producer消费33、新geometry收据26例均通过，不替代整关验证。

045248 stderr338条ERROR中有1条 `_instance_binding_free_callback/script_bindings.is_empty()` FATAL及336条unsafe引用错误：可辨311 RayQueryParameters、20 KinematicCollision、3 InputEventKey，另2条崩溃穿插不补名。不能误写成普通exit1或仅HP失败，也不能归成另一个 `gchandle.is_released()` FATAL。external的单条托管失败是镜像，不算独立第二次故障。后来安静案例不抵消它。

最后post三日志没有Handle/ERROR/FATAL，仍因超时失败。registry到engine161083ms才进入；37 enter、36 return，最后index36 `RealWorldObstructionUsesItsRayHitAndNamedFailuresRemainVisible` 在179749ms进入。不能把整段耗时或资源根因归于最后案例，也没有扩大180秒或“缺收据”豁免。本轮未重跑历史Bone7/Ghost4 ShapeRID专项，失败继续OPEN。

## 独立审查、裁决与代价

一次新上下文gpt-6-astra/high只读审查上述三个代码/辅助文件，与v080 models044011的before快照比较，不冒称整个dirty树已审。审阅者独立执行纯26例GREEN；Critical/Important/Minor均无新项，无修补轮或复审。实际Main候选后来证明台阶通过、下一助跑失败，已纳入本报告，不借审查意见宣称完整路线通过。继承的Title/replay失败清理Minor仍延期，未证明是资源根因。

执行裁决：

- 原位dirty制作、唯一root写入/构建/原生执行及运行期全文件冻结；不建新worktree、不Git写入/删除。代价：按快照归因，不能声称clean commit接受。
- 以实际CanGuard替代测试自设保留门槛，而不改生产盾或战斗难度。代价：真实耗尽/过热仍可能使其他战斗失败，不能外推无伤。
- 精确失败快照先于修正；不Dispose借用slide，不根据最后加载场景猜资源所有者。代价：资源身份根因仍OPEN。
- 旧连续刹车消费改为现有真实模块预测，最终仍验原物理支撑和位置。代价：预测不是碰撞解算器，完整往返与真人容错仍须验证。
- 新助跑失败只定位到x2356.7551越过原2354窗口；终态仍在整身宽度内，不冒称已失地面支撑。只用42/140、助跑7physics，不是预算耗尽。下一次须在首次observer失败原子记录floor/跳数/JumpHeld/scale/aim及上一physics的ready，不扩大窗口。代价：本轮完整Main仍不通过。
- geometry只窄修当前协议及唯一性，原资源错误否决不变；初始AST选择器歧义及一次只读审计PowerShell语法错误均是工具夹具错误，不算行为RED或额外原生案例。代价：纯协议测试不承担原生稳定或完整空间资格。
- foreign防御暂只观察，保留未知p2056来源和所有落伤/敌人。代价：探索仍死亡，下一单变量须真实ShieldResolved/HP证明。

审查全部“Declined to judge”逐项裁决：

1. W/旧预览：本轮不改排序，既有reader/controller不重签；代价是所有输入边界仍须专门验证。
2. 实体遮挡/接收资格：没有新射线授权施法，历史first-hit不外推；代价是全地图遮挡未合格。
3. Intro→Playing：不改准备/提交门；代价是geometry通过不能替代正常启动。
4. callback/deferred：有界日志不等同跨边界身份/父级/姿态证明；代价是历史生命周期待完整回归。
5. LoadLevel自有清理：本轮未改loader/双异常保全；代价是继承故障分支不重签。
6. 完整Main：根实际运行台阶通过、下一助跑FAIL；代价是不能交付主线通关版。
7. Explore：根实际运行foreign覆盖及死亡，修正未做；代价是探索生存仍OPEN。
8. FATAL/RID：045248真崩溃、pre4Handle、postTIMEOUT及历史RID全部保留；代价是稳定性尚不合格。
9. 全人口、正式视听/UI、硬件、存档与发行：76普通+Boss/NPC及原十一项不缩减，不读真实存档；代价是所有最终条件继续OPEN。
10. 真人显示：“没看到”是权威状态，不自动开窗；代价是教学/灯避让、正常速度和真人手感待重新安排。

## 保管与下一步

12案现存59对before/after全部相同；构建失败045027缺fresh-binaries/binaries-after的一对如实保留。66份封存二进制逐一匹配。最后普通1776源/892 C#、9个常规helper/6二进制实时漂移0；geometry额外第10个纯测试helper实时漂移也为0。

- Source manifest SHA256：`35A5ADD421821FD45264969BA465EDBD1E6BC78AD0A0AF7659950F2F77BBE4EB`
- Binary manifest：`AF284186B262D48CD5995E76EC28814FAABA0668EFA276E17E7FE228AA71C8EC`
- Knight.dll：`1E63C876A6E81F0BDBB9C2E0F22ACB47EE621CCEB28E0B898BFAECF91FBDDEF8`
- 分支/HEAD：`codex/task6-certified-final-20260902T0555` / `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`
- index：`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`，未改。

本轮所有root构建/原生已结束。无付费、自动化、Git变更、删除、真实玩家存档语义读取、新过程图/视频或新增正式图声；资产表不新增虚假交付。下一步按已取得的真实边界处理Moth foreign防御和护盾助跑，继续资源所有权、原完整双路/pre/post、正式视听及真人硬件验收。不能以一直完善测试替代游戏内容完成。

容量监测仅手动执行一次 `-Check`：UTC2026-10-23 21:34:07，当前860284 / 基线3508 / 增长856776 bytes，Notify=false，未达到新增20GiB；本轮新媒体0，没有建立通知或自动化，也不以容量提醒停止工作。
