# v248 · 主线末击真实条件与现有稿后续重审

## 当前结论

事实：本轮只新增一个测试文件中的有界被动观察，没有生产动作、力学、地图或美术修改。原normal Main仍在899循环终点失败，不能称主线通关或稳定性修复。

推理：已补到此前缺失的末机会窗口；本次没有发现“满足末击条件却停着不发”的证据。更早周期仍需按实际选择、原能量储备与短路值判断，不能因最终失败就放宽敌人Recovery、24循环门或899终点。

风险：观察没有额外await、输入或危险查询，但读取/分配/输出仍可能影响调度；128尾部实际丢了18条较早行。记录不能替代正常速度动作、真实操控或用户审阅。

## 用户原件决定

U-20261105-01：V1中的批准原件目前找不到，后续重新审阅现有候选。停止扩搜旧源，不等待此材料继续其他无窗口工作；旧批准不转给当前PNG、身体/披风分层、总装或动作。本轮没有展示/生成/批准任何图稿，也未获得付费、开窗、系统输入或新媒体采集授权。[原决定与限定来源核对](2026-11-05-approved-body-mantle-git-history-readonly-v001.md)。

## 原Main实际收据

Case：`G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261105-012646-913-2a7d7cfa3b1f4adc9f28de14c5811f08`。

- build31963ms/exit0；engine165099ms/exit1、TimedOut=false。真实键盘内部路径、正常策略，无旧实验flag。
- 原899：玩家HP2，恢复0/0，站稳AscentGrip5；batch2尚arrival/live0。清理三帧期间才见Laser，原strict失败保留；stderr中的旧enemy_health1是缓存，不是新Laser的独立生命证据。
- `TASK10_GALLERY_COUNTER_PENDING`每个原loop至多一个完整行，阶段各自physics/process/input，实际selected与tracked/cached分开；128字符串尾部，total146、overflow18、faults0。
- 840–843：实际选择Skitter、Telegraph，距离门0x0001失败；不能说后面的能量/外国反击门为false，因为短路未求值。
- 859–862：实际选择Skitter，Retreat/Recovery余0.25→0.20秒，原要求attack0.30＋2/60秒且Recover；0x1000失败，其后门未知。
- 863：Recover/Recovery0.6秒，strike真、energy26.4≥12＋10.8、eligible真；实际全部mask0x1FFFF通过，同loop发J，无release等待。两原physics后AttackStarted已记同loop；不把此后contact相对数冒充独立来源接触收据。

### 完整保留尾部的独立只读复核

128行覆盖738–865，日志3942–4069。owned Skitter的`canCounter=true/eligible=true`只见863；`eligible=true && issueJ=false`为0条。

| 原循环 | 已记录阶段 | 实际首个失败门/结论 |
| --- | --- | --- |
| 738–740 | Recover6，余0.20→0.167，next741 | 0x00008冷却；不能推后续门真或假 |
| 741–750 | Recover6短余时→Idle | 0x01000资格 |
| 751–781 | Telegraph7 | 0x00002自身威胁 |
| 782–794 | Recover但Attack0，外国bone-diver攻击3 | 0x00002外国威胁；自身份本已不合格，不是已证“抢掉合法窗” |
| 795–815 | 先Skitter，806起选bone-diver | 0x00001距离 |
| 816 | 实际选bone-diver，tracked仍Attack0 | 外国目标J，全部门通过；不是owned Skitter的漏发 |
| 817–849 | 先外国目标，再Skitter Telegraph8 | 先自身威胁、后距离 |
| 850–856 | Telegraph8→Retreat8；856仍monitoring | 0x00002自身威胁 |
| 857–862 | Retreat/Recovery0.283→0.20 | 0x01000资格 |
| 863–865 | 首次合格即J，之后next887且攻击中 | 同轮发出；随后不是第二个被吞机会 |

外国来源ID4892135527592可由原日志3694绑定resident bone-diver；其攻击3在782–813为实际危险来源，但Attack0本来不满足原非零身份门，所以不据此修改全局威胁优先级。

下一最小缺口：720–737首18条已overflow。只补留原字段的有界prefix，核对J717后的最早HP1阶段即可；不重新做最后窗口的同一调查，不把缺行填成False，也不预设原24/899一定不合理。本轮未实施该下一增量或新行为修补。

## 资源只读边界

旧geometry003554是8835ms/nonTimeout/exit−1073741795的实际匿名gchandle/Finalize FATAL。producer24资源包已返回；最后有rock-wanderer detached/query witness，尚无完整census、activity/新leaf Ready、JSONL或teardown完成。外部与stdout跨线程输出顺序不能证明触发因果。下一可考虑独立的单rock分阶段控制，但本轮未写入或运行；不能以建议当修复，也不增加GC/KeepAlive/cache模式或额外崩溃采集。

## 审查与验证边界

一次新上下文审查C0/I0/M1，root接受；逐row诊断fault归属Minor延期，六条declined逐项裁决见[审查留档](../../.superpowers/sdd/2026-11-05-gallery-counter-window-v248/final-review-v248.md)。精确before→current与原始失败保留，无二审。

本轮四案均已终态，同一1888输入/六binary；无Timeout，不留待轮询的本轮native：

| Phase | strict结果 | engine ms | 本次case后缀 |
| --- | --- | ---: | --- |
| thruster-route-main-diagnostic | FAIL，原899 | 165099 | 012646-913-2a7d7cfa3b1f4adc9f28de14c5811f08 |
| models（机械爪/到达子集，不是全部模型） | PASS | 651 | 013238-078-ebe6c5fe333d4860ad1ba9a0aacc6235 |
| keyboard | PASS | 20457 | 013320-588-b90593f6be374beb9f2f332ebedf57f6 |
| thruster-route-exploration-diagnostic | PASS | 320187 | 013422-603-e1d508fe221e414cb2cdbd6848f86828 |

Case均位于`G:/knight/验证证据/october13-stable-claw/<phase>-20261105-<后缀>`。三项补跑严格结果见[原始批收据](../../.superpowers/sdd/2026-11-05-gallery-counter-window-v248/increment-results-v248.json)，它有意不把已跑Main列为通过。四案合计3PASS/1FAIL，不是3/3全部序章。

Explore的当前JSON证明：从标题、同一player47563408900、23路段/11挑战类/11激活/10击败，经Boss28384314789679的真实致死attack99（17961physics）及18140实体出口完成；其后持D60帧到18201位置不变且实体脚下有支撑。完成后physics已禁用，缓存velocity260不是继续移动；保存SHA前后同`D68DEC7993D42D14D26D4F98E88C00B907350568CA6F6B52E552E87F08A56779`，恢复0/0。`syntheticFixture=false`，但human/operationCoverage/fullGoal均false；不代表清完76敌或全操作，不是用户试玩认可。

20组source/helper/user/continuation/binary前后文本全部一致；四案source清单同`147F17522026183BA4A39BF3C553B7F570FD03F8AD0A959D39A371C7FA9E241F`，当前1888文件重哈希0差异。六binary清单同`1357FCE43FB09FB294BF9E96D174D068F59FA0E3FDE20E7CE2C745539015B059`，当前0差异；各phase按作用域的两个helper清单并集11条，当前0差异。详情见[绑定核对](../../.superpowers/sdd/2026-11-05-gallery-counter-window-v248/bindings-v248.json)。Git branch/HEAD未改。媒体正常汇报一次Check：7427750字节/86文件，Notify=false，无新图稿或录制，不建立自动化。

本轮不是交付节点。v247的15PASS/2FAIL是历史整套，不称本轮同构建全17已通过；计划Task3完整17及原十一项验收仍欠。下一先补最早HP1 prefix或独立资源阶段控制的必要证据，不因未完成而重复最后窗口调查。

R-I惯性、R-C角色专属连续动作、R-P物理操控、正式分层/正常速度美感与用户认可均继续OPEN。仅无窗口，无付费/新媒体/自动化/发布/Git写/真实存档改变或删除。
