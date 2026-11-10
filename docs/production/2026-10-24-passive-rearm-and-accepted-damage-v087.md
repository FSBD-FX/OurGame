# v087：待机再武装时钟与真实扣血归属

2026-10-24。唯一工程 `G:/knight/Knight`、资源根 `G:/knight`。原十一项完整目标 ACTIVE，未交付完整序章。用户最后更正“等一下，是没看到”有效：没有新的前台窗口、系统键鼠输入、真人显示或教学/悬浮灯认可。无付费生成、自动化、Git 写入、打包或删除；真实玩家存档未作语义读取。

## 事实

1. **原生反例后修正自动验路的假零时钟。** 保存场景中原 Rust/Burst 同步出生、尚未首次 physics 时均为活的 Idle/None；旧危险消费者给 safe=0、admitted=False，真实测试失败。现在仅已知待机状态读取实际模型持有的完整 Telegraph，并扣除该模型的边界容差；同一反例 safe=.65/admitted=True，原混编格挡测试保持通过。五模型/五 actor 只增加只读时长通道，不改变任何 AI、时长、伤害、血量、站位或数量。Ranged 失去权限、未知状态、Active、旧弹及连射间隙仍不能借用新预警时间。保留完整 J、六个实际完成后裕量帧、两个采样/命令帧和松盾后的 fresh 复验。
2. **主线实际有两次 J 启动，但仍失败。** 当前 Main 在 physics6600、6645 启动两次真实攻击，不再是旧900循环始终0J。不能据此声称两次命中或整组击败。随后原波次空且出生被抑制，进入原 recenter 分支：6686开始、6713实际停稳4700.3164、6718出现新的到达警告；该分支未重采危险/守盾，6731由常驻 precision-burst 的真实 shot7 扣 HP3→2，6732原无伤护栏失败。来源 actor4777530364657、stableId=`canyon.v2.resident.counterweight-lift.04`；最后普通战斗决策6685，不能把46帧旧观察当新时钟。Skitter实际 post-node配置观察三段为3/1320、2/1040、1/161，changed=False，未冻结其 live Resource。
3. **下层退出不是摔伤。** 两次旧 Basin 独立路线都在 physics973保持同一玩家47294973447而 HP5→4；已接受伤害身份为 Hitbox165322691052/attack1，不是 Motor47378859541。第二次补得直接父 actor165255582184、`RustShellSquire`、保存场景 `res://scenes/enemies/residents/v001/scrap-wanderer.tscn`，与初始居民快照同 ID。落差120.00903、fall_damage=0、fall_applied=False。只读取 Health 已接受队列并订阅原 Damaged，没有替换 receiver、伤害、路线或输入。一次事件/观察异常0；仍失败，不用观察结果假造修复。
4. **资源身份补齐，不等于故障修复。** 先前两个 REF 崩溃停在 PlayerTuning.Dispose(false)，serial0只是该类型未登记，不是 native ID0。现诊断符号下为 PlayerTuning 加 managed serial/释放前缓存身份，原一次 base.Dispose 与 ENTER/RETURN保留；现有、受自有根异常清理保护的同步观察点读取实际 Motor/Combat.Tuning。此次五个根两字段均共享 serial1/nativeId9223372067238315573、路径 `res://scenes/player/Player.tscn::Resource_y7r3h`；四个原 Supply 案例返回，所有已进入释放返回。该案安静并不能把以前未匹配的失败对象归为默认对象，也不能证明修复 GCHandle。没有额外 Load、GC、cache 改动、持有 Resource、延时或 Dispose。
5. **完整回归仍红。** 普通 pre有三条 Handle 错误及 FATAL（共4条 ERROR），96521ms崩溃；post134797ms同类 FATAL崩溃，不是180秒超时。post已返回原11段地形和33样本的旧教学检查，但未完成后续registry/精确scope；工具报缺scope不能覆盖真实原生崩溃。Explore64518ms在physics2860死亡，request1/completion0；同步死亡点(1480.6831,3243.9343)、HP0、落差22.285156/落伤0。正常双路、资源寿命、相机/正式视听/硬件真人仍未闭合。

## 本轮全部执行原件

canonical根：`G:/knight/验证证据/october13-stable-claw/`。下列目录保留 stdout/stderr、native退出、保护与来源清单及实际构建副本；各冻结有差异，不能拼成一次同版整关资格。

| 目录 | 原生结果/毫秒 | 严格结论 |
| --- | --- | --- |
| `ranged-mixed-policy-20261024-091130-077-9e14c8b6976e4b35bc08aaf5d7b3a005` | 1/1443 | 正确前提的 false-zero RED |
| `ranged-mixed-policy-20261024-091919-856-636b9c4dec3946a6bdb357a564f40f03` | 0/3398 | GREEN，原格挡合同保留 |
| `thruster-route-main-diagnostic-20261024-092351-081-00d73bf3564243e8b7e74b373effec4b` | 1/124141 | 两次真实J，后续 recenter HP失败 |
| `basin-supply-lifetime-refcounted-identity-20261024-092838-388-c6cb20ef19f04367b0575ff0ef18ee51` | 未启动native | 新观察缺类型命名空间，build失败；不是行为RED |
| `basin-supply-lifetime-refcounted-identity-20261024-093008-341-5b1759e70ad140e2a31e8abe9709f553` | 0/12060 | 诊断严格通过；behaviorAccepted=false |
| `prologue-pre-20261024-093114-388-b2944e57f09e4cc7aaf6f31736c2c136` | -1073741795/96521 | Handle/FATAL，严格FAIL |
| `prologue-post-20261024-093340-081-b1d49f7757d3436cb60fc0e5623d51bf` | -1073741795/134797 | FATAL/未完成scope，严格FAIL |
| `ranged-mixed-early-boundary-20261024-093704-047-88b6654daebb45c09ec1b367f59bcc9a` | 0/1733 | .41666663短预警仍拒绝攻击，GREEN |
| `ranged-mixed-counter-20261024-093904-297-6bf1f493a48648458f24c8feecb4f933` | 0/4152 | 真Rust HP3→2、玩家HP5、完整J＋六尾帧、旧弹/间隙保护GREEN |
| `thruster-route-exploration-diagnostic-20261024-094148-247-c2a06f67bfeb4c459c70b8a0e29d0d9f` | 1/64518 | 死亡恢复请求，严格FAIL |
| `models-20261024-094405-590-00da2dd1b2844d88805bb0a027fe34ff` | 0/648 | 普通机械爪模型专项GREEN；不是全项目套件 |

共11canonical案：1构建失败、10原生案5严格通过/5严格失败。构建缺命名空间已改全限定类型名，未改测试判据。纯几何收据26、入口96、诊断消费33均新跑通过；不能替代原生、整关或视听验收。

旧 Basin 根：`G:/knight/验证证据/october12-basin-counterweight/`，两案均native1、无超时：

- `routeencounter-20261024-092652-010-4c02c0b8a6b24f8d93b3b31b62bb6703`：已接受Hitbox/Motor分离。
- `routeencounter-20261024-094013-028-e88f30cb4aa24216bc9bb582a0b5c5da`：同一事件补得实际父演员类型/保存场景。

## 保管、审查与裁决

相对v086最终冻结恰16个源文件变化：十个只读时长 getter所在文件、Main/Mixed/Basin三个测试消费者、Supply观察及两份诊断源。没有新增/改动正式图片、动画、声音、地图或资源资产表条目。当前1776输入、892 C#；canonical54/54前后清单对一致、60/60实际封存二进制匹配，运行期native保护失败0。构建失败案保留其缺新二进制护栏，不伪造5组清单。旧 Basin8/8运行清单对、28/28保存副本与各自冻结manifest匹配；原失败后的afterScope清单缺失仍保留。不能把不同冻结的历史副本拿来与当前文件直接比较后误称运行期漂移。

最终普通9帮助器/6二进制、源/帮助器/二进制当前漂移0、额外bin0。source清单SHA=`BA0D4309A9AABFEAB97E8B7E8FFEDDB964471FED07997D02A3D0CDCA2F973876`；binary清单SHA=`35FB26784A0F721AE3B1FA4DF29D220C7FC33221A2161C010C682B6AD4DC0289`；Knight.dll=`7CE7680D93972B13CDE23408BFD9DB30AF7C5854871E81CDE246F35E0EB6B48A`。Git index仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`；diff-check0，169条既有换行警告/其他0。root所有native/build句柄终态、进程清单空，以下仅文档更新。

一位新只读审查员完整检查本轮增量：Critical0/Important0/新Minor0；没有第二审查或新修复轮。root逐项处理审查Declined及成本如下，均不因局部GREEN关闭：

1. 历史脏树/分支整合不在本轮资格：不Git写入；成本是尚无可发布提交。
2. Main仍失败：保留无伤与900；成本是不能交付主线。
3. recenter修复尚未实现：下一压力fixture验证，不直接添L取绿；成本是机枪手仍可击中。
4. Explore/Boss/出口/死后继续未完整通过：当前Explore新案明确失败；成本是正常双路缺失。
5. W采样/预览/rank未改变：不归因到这次时钟补丁；成本是旧专项开放边界保留。
6. LOS/receiver不扩张：沿用现物理与资格；成本是其他遮挡问题可能仍存在。
7. Intro/Playing生产门不改：保原准备拒绝；成本是其他readiness失败未认证。
8. callback/deferred取消不变：本轮没有此类修复证据；成本是旧生命周期门仍须跑。
9. 全部LoadLevel/工厂清理不冒认：仅现有本地观察拥有者保护；成本是别处清理异常未关闭。
10. 更后Basin重入全集合disjoint断言暂不动：当前尚未越过真实伤害；成本是到达后仍可能遇到旧测试误报。
11. 下层攻击者已由第二次native补为实际scrap-wanderer；稳定slot尚未独立映射：不由显示名猜slot；成本是不能据此重排名册。
12. E06全四远程/各近战飞行/76身份/负载未验收：本轮仅当前组合；通用danger对非selected Skitter亦非完整守盾覆盖；成本是仍需全人口实战。
13. E07平衡与体感未验收：不改能量/热/难度；成本是专项无伤不是最终手感。
14. 资源根因未知：静态没找到显式共享Tuning.Dispose；quiet REF不关Handle/FATAL/RID，不用未命中serial断言默认对象；成本是仍存在崩溃。
15. 同冻结完整套件未绿：新pre/post都保留真实FATAL，最终普通models不拼全绿；成本是无发行资格。
16. 继承Minor延期：counter来源容量/观察错误完整性、旧Title/Supply/replay清理及Frozen诊断flush异常保护；成本分别是诊断可能不完整或历史异常仍可能掩盖主错，不能作为无缺口取证证明。
17. 视觉/音频/目标硬件/真人未验收：用户NOTSEEN控制，未开窗；成本是画面/动作/音效/60FPS没有最终认可。
18. 原十一项、人数、存档兼容、授权包/GitHub、许可与交付保持OPEN：不缩目标或把FTR混入；成本是尚不能宣称完成。
19. 审查员未启动原生/写入/Git，root负责以上实际原件与保护核验：其审查不扩为其他运行资格；成本是未列出的测试没有新增证据。

## 下一步

1. 保留原五个 suppression-recovery案例，另加实际保存 precision-burst＋suppressed-recenter 压力反例；把每个原900tick的危险采样/短时守盾接到同一消费者。守盾时暂停A/D意图而非改速度/位置，真实keyboard瞄准可能带来一次位移；不能持原D意图却假称中立瞄左。
2. 必须验证实际旧弹结束→释放L/W/S→原4700±6完整支撑/中立→完整新.60s到达→双演员出生，保HP/原时限。不能因Recovery就松盾，也不能假定尚未实际观测的第三发已结束；若中立出生与尾弹冲突，保留新真失败再处理。
3. 下层返程依据实际scrap-wanderer预警/接触处理，不改落伤或删居民；保持无伤、同Player、原回程和存档规则。随后才处理更后重入断言。
4. PlayerTuning已赋值共享身份可在后续真实FATAL匹配；未匹配仍未知，不直接Dispose、去默认创建或换cache。继续资源寿命/普通pre-post、正常两路与相机；待稳定后才安排新的真人可见时段。

媒体单次检查UTC2026-10-24T01:45:34.0515197Z：860284bytes、基线3508、增长856776、Notify=false。本轮无新媒体；20GiB只随正常报告提醒，不停工、不建自动化。全部必做制作、正式视听、正常速度审阅、硬件性能、真人与版本交付仍需逐项完成。
