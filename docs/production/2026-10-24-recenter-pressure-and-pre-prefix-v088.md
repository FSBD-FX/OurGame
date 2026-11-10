# v088：回位旧弹压力与完整前缀资源诊断

2026-10-24。工程 `G:/knight/Knight`，统一资源根 `G:/knight`。原十一项目标保持 ACTIVE，未交付完整序章。用户最终更正“等一下，是没看到”继续有效：没有窗口显示、教学遮挡、悬浮灯或视听效果的真人确认；本轮没有打开窗口或发送系统键鼠输入。

## 事实、推理与风险

1. **实际 Main 重现了回位漏防。** 未改源码的095316案再次在 physics6731 被常驻 precision-burst 的 shot7 扣 HP3→2，原无伤护栏失败。最后普通战斗决策停在6685，不能把46帧前的危险观察当成当前观察。保留原900循环、原演员、资源、支撑与回位范围。
2. **只补自动验路的同一回位分支。** 每个原有tick重新采一次现有危险集合，沿用既有格挡条件；守盾时清除A/D移动意图，让键盘瞄准完成一次转向后保持中立，不在朝左挡弹时继续持有向右回位指令。原一次physics等待、HP、无跳/冲刺/恢复、4700±6与出生检查不变；没有生产AI、数量、攻击、伤害或资源数值调整。
3. **原局部衔接通过，整个 Main 仍失败。** 095723案实际守住旧弹，6762松盾停稳于(4701.148,3423.933)、速度/输入为0、HP3；6767观察到新警告，6803原双演员出生并继续战斗，HP3、恢复0。独立pre-node时间观测仍明确 `independent_full_060_proof=false`，不能把36个端点样本升级为独立完整.60秒证明。更后6881/6903受到新波次右侧机枪伤害，7049 HP0、能量0并真实发起死亡恢复；下一采样回起点触发step3005.01的无传送断言，不是已证明的随机地图传送。
4. **两侧交叉火力未解决。** 当时守盾朝左，当前危险选择仍为左侧常驻旧弹；右侧新波次可造成真实伤害。所有Active贡献同优先级的当前选择值得后续检验，但不能据此直接更换排序、删来源或加血；最近目标规则也尚未证明能解决同时到达的两侧攻击。未把两次J启动当两次命中或整组击败。
5. **完整原前缀的资源身份诊断本次安静，不等于修复。** 在原pre中相机/Laborer/机械爪等前缀及HealthRestore后的原Supply位置，仅REF编译分支调用现有完整寿命观察；四原例及顺序、清理和等待不变。100244案0/177206ms，1845个ENTER/RETURN逐call匹配，重复/缺返/孤立返回/先返后入/身份不一致均0。五个原根实际Motor/Combat共享Tuning serial1、native9223372067238315573、路径`res://scenes/player/Player.tscn::Resource_y7r3h`；其call1830真实disposing=True正常返回。另225次PlayerTuning释放身份仍unknown，不能归为默认对象；观察关联的owner不等于独占拥有者。没有新增Load、GC、cache、借用资源Dispose或资源持有。
6. **普通构建仍真实崩溃。** pre101044于108180ms、post101317于158357ms均退出-1073741795，`gchandle.is_released()`/Dispose→Finalize原生栈，没有项目Resource类型身份；两案Handle错误均0，各1条FATAL，不是超时。post还有独立HUD准备失败：`low-ceiling-bridge.03`出生报“Require one exported binding and explicit admission for placeholder visuals”，fixture预期Playing实际Title，未进入语言尺寸测量。不同日志流不能合并成精确先后，不能声称HUD清理导致FATAL。wrapper缺scope只是未完成资格的结果，不替代原始行为失败和崩溃。
7. **原suppression回归没有通过。** 五模式与取消/重入覆盖保留，但首个recover在原A8/release8后速度仍(-19.999998,0)，位置(4694.6206,3424)，原停稳断言失败；后四模式及后续取消覆盖未执行。不能把源码保留写成测试通过，亦未证明这是本轮三文件改动引入的回归。应先用真实采样停稳检验旧固定帧假设，保原时限/支撑/HP/出生与取消要求。
8. **探索仍死亡。** 最终普通Explore在physics2861发起恢复request1/completion0，严格失败。完整J专项和过短预警边界通过，不替代两条正常路线、Boss出口、资源寿命或正式视听验收。

## 本轮全部原生执行

证据根：`G:/knight/验证证据/october13-stable-claw/`。原件含stdout/stderr、退出/来源/保护清单和实际构建副本；不同冻结不拼成一次同版整关资格。

| 目录 | native退出/毫秒 | 结论 |
| --- | --- | --- |
| `thruster-route-main-diagnostic-20261024-095316-946-b6aa562c40c545eaa1bb0b2149388bb5` | 1/145626 | 未改Main的真实回位漏防RED |
| `thruster-route-main-diagnostic-20261024-095723-196-25ed0531488f4cc78372247d10998d98` | 1/133070 | 原局部回位交接通过，后续交叉火力死亡，整路FAIL |
| `prologue-pre-refcounted-identity-20261024-100244-550-46ef8f0f94724c4baba547f755735017` | 0/177206 | 完整原前缀诊断通过；behaviorAccepted=false |
| `prologue-pre-20261024-101044-574-9071e492786743b9aff0dab1f60ee5ca` | -1073741795/108180 | 真实FATAL，严格FAIL |
| `prologue-post-20261024-101317-045-afc1ca81af8440b8bd657945496b02ab` | -1073741795/158357 | HUD准备失败及FATAL，未完成scope |
| `ranged-suppression-recovery-20261024-101640-225-a23dfcb319e84343a6e0970bec707078` | 1/4946 | 第一模式A8/release8未停稳；后续未执行 |
| `ranged-mixed-counter-20261024-101744-259-5e031de4d1124404bebd25c1eeb9af16` | 0/4148 | 原真实J接触、玩家HP5、完整攻击及六尾帧保护通过 |
| `ranged-mixed-early-boundary-20261024-101842-243-9e12b8346cea42efbe694097e3a06a59` | 0/1841 | .41666663短预警仍拒绝出招 |
| `thruster-route-exploration-diagnostic-20261024-101932-540-186f7b3693604813aad37b987889e1d9` | 1/54860 | 真实死亡恢复请求，严格FAIL |
| `models-20261024-102119-369-5babadfb12fc4fdabb983821217b70ac` | 0/529 | 最终普通机械爪模型专项通过，不是全项目套件 |

共10个canonical原生案、构建失败0：3个普通专项和1个REF诊断严格通过，6个失败；全部未超时、运行保护失败0。post两条有原scope的注入WARNING不豁免真实FATAL或未到达的flow-registry范围。新跑纯检查：pre身份入口20、几何收据26、focused入口96、诊断消费33，均通过且未启动引擎；不能替代原生资格。

## 本轮源码、保管与审查

相对v087最终源清单恰三个文件变化：Main自动验路、Supply可选prefix标签与Runner的REF-only调用。候选Main执行后仅把错误的pre-command诊断标签改为after-command并注明selected_context=none；没有行为差异，但不声称最终三文件冻结已完整Main通过。无正式图片/动画/音频/地图资产新增，资产表不增加虚假条目。

10案50/50对source/helper/user/continuation/binary前后清单逐字节相等，60/60封存二进制匹配各自清单；缺失、额外或hash不符0。源1776项/892C#；普通最终helper9、pre10、REF-pre13（以实际清单为准）。最终枚举与当前源/帮助器/6二进制漂移0、额外bin0。旧冻结只与其自有清单核对，不拿来与当前源码混比。

- 最终source清单SHA：`6F2F16CDB63E989FEF0DF89B07FCD9A4EDEEC084023FA472A7F432D2E6E1CC8F`。
- binary清单SHA：`BA5830EC4F9419C0107905A432072CB71D0979FA48E293D03A93754E6A4A39FB`；Knight.dll：`2F7E12F2555DE40C08E8BB088F52E60C2228C136A9875A88A4B458FC82416840`。
- index仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`；分支`codex/task6-certified-final-20260902T0555`、HEAD`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，gitdir/common均`.git`。diff-check退出0，169条既有换行警告/其他0。
- 全部root构建/native句柄终态、相关进程清单空后才写本报告及状态文档；最终普通构建已恢复。

一位新只读审查员检查精确三文件增量，Critical0/Important0/新Minor0；root按实际影响同级接受，无新fix pass或第二轮审查。独立证据核对不充作代码重审。审查不扩大到整个历史脏树或未运行范围。

## 执行裁决与审查未评项（逐项成本）

前一v087轮是有实际反例/修正与新归属的PROGRESS，不是纯状态汇报。本轮同样是局部验路改进和资源排错进展，不是新增正式内容或任务完成。

Ruling：以完整原Main本身作为回位压力反例，而不新增一个新鲜Idle/Burst夹具冒充原第三周期shot7；原五suppression例完整保留。原因是新鲜模型阶段并不匹配真实旧弹压力；成本是测试更长，局部交接通过仍不能替代全路及独立五例。

Ruling：继续用户选定的已有脏分支和同一plan/ledger，root唯一写入/编译/native，协作者只读；不运行worktree/提交/task-done/删除工作区脚本。原因是完整严格套件和原十一项未完成且未授权此时发布；成本是尚无可发行提交，证据按实际冻结保存。

以下为审查Declined逐项root裁决；没有任何一项默认为通过：

| 项 | 裁决及理由 | 保留成本/风险 |
| --- | --- | --- |
| 1 脏树/整合/发行 | 不扩大三文件审查，不Git写入 | 无干净发布提交 |
| 2 构建/native/保管 | root负责上列真实执行与清单核对 | 未列测试不获资格 |
| 3 REF-pre结果 | 以上独立核对仅证明本案安静 | 历史Handle/FATAL未关闭 |
| 4 Main后续交叉火力 | 保留真实死亡，不改HP/能量/敌人或900 | 主线仍不能交付 |
| 5 Explore/Boss/出口/继续 | 当前Explore明确失败，未测段OPEN | 正常双路/恢复未闭合 |
| 6 W/预览/排序 | 本轮未改，不借回位结果认证 | 旧瞄准边界仍待检验 |
| 7 物理LOS/接收资格 | 无resolver或遮挡改动 | 其他遮挡问题仍可能存在 |
| 8 Intro/Playing | 保生产准备拒绝；HUD失败独立列出 | 未完成的启动/准备不能放行 |
| 9 callback/deferred | 未改执行边界/演员连续性 | 对应生命周期范围仍须验证 |
| 10 清理异常 | 不借已有局部拥有者保护认证所有LoadLevel/工厂 | 主错/清理异常完整性仍开放 |
| 11 全敌群/Skitter | 当前selected=null不认证非selected Skitter全部危险；不改76身份义务 | 远程/近战/飞行组合与负载未验收 |
| 12 原专项合同 | counter/early新跑通过；五suppression首例失败，后续未执行 | 不能声称原五例回归绿 |
| 13 平衡/资源/体感 | 不把验路无伤当最终难度指标 | 两侧火力和能源经济尚未真人验证 |
| 14 Basin下层伤害 | 先前scrap-wanderer真实伤害归属保留，不动更后重入断言 | 下层无伤及slot映射/重入仍未闭合 |
| 15 GCHandle/RID根因 | 不由共享Tuning或quiet推断失败拥有者 | 真实崩溃风险仍在 |
| 16 继承Minor | counter容量/错误完整性、历史Title/Supply/replay清理、Frozen flush异常保护延期，不在本轮顺手修 | 诊断可能不完整或清理异常可能掩盖主错 |
| 17 同冻结完整资格 | 不拼不同冻结的成功；最终普通models只是专项 | pre/post、人口/相机/输入仍缺完整通过 |
| 18 存档/许可/包/GitHub | 不读真实存档语义、不打包/发布或推送 | 兼容、许可和交付仍未验收 |
| 19 正式视听/硬件/真人 | NOTSEEN控制，未开窗；无新正式图声 | 画面/动效/正常速度/60FPS未获认可 |
| 20 原十一项 | 稳定ID、76普通+Boss/NPC及全部必做交付保留；FTR仍排除 | 目标ACTIVE，不能局部成功即完成 |

延期Minor为第16项三个继承族；无新Minor。不会把未知资源类型、未跑到的范围或失败套件隐去。

## 后续顺序与边界

1. 先利用本次首例原生RED检验suppression旧A8/release8的固定帧停稳假设，保持原预算/真实键盘/支撑/HP/无跳冲刺/完整出生与取消合同；不增加一个更容易的替代验收。
2. Main仅在实际两侧命中时序和当前危险选择事实基础上检验下一正常键盘策略；不能直接猜排序能解决，亦不buff能量或删常驻敌人。
3. 普通pre/post的FATAL及HUD绑定准备失败分别保留。REF关联serial1正常返回不能填补225个unknown或普通未命名失败对象；没有归属证据前不批量Dispose、删默认Tuning、换cache或引擎。
4. 下层返程、完整主线/探索、相机与所有原十一项继续；稳定后再征求新的可见窗口时段，不能复用已被“没看到”撤销的显示确认。

媒体一次性检查UTC2026-10-24T02:23:32.8343605Z：current860284bytes、baseline3508、growth856776、Notify=false，无新媒体。没有自动化/付费生成/Git写入/打包/删除/真实存档语义读取。20GiB只随正常汇报提醒，不停工；本轮未达到新档位。正式视听、正常双路、目标硬件、真人与版本交付仍OPEN。
