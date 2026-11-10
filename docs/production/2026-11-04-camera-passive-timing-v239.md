# v239 · 相机夹具真实耗时分段与落地现状复验

状态：本地诊断增量；最终同源码/六二进制清单五门**4严格PASS/1FAIL**，完整pre在下降教学HP4（原要求5）失败；**不是完整序章、性能、美术或动作验收通过**。原十一项和R-I/R-C/R-P保持ACTIVE/OPEN。唯一工程仍为G:/knight/Knight，无窗口/系统输入/付费生成/额外采集/自动化/Git发布/删除或真实存档操作。

## 实际改动与独立审查

- 原`BasinAscentCameraRuntimeTests.Observe`只在已有30个边界附加engine_ms、physics、process读数；原await、案例、人口、生命周期与清理不变。
- 严格解析保留原顺序/sector/high/root/三不同owner；三钟canonical UInt64、全流非倒退。原无钟日志不能冒充新时钟证据。
- 唯一独立只读审查C0/I1/M0。I1指出冻结时钟也会被非倒退校验接受；根新增13反例实际RED（控制33–45全漏收）后一次修补。按真实等待要求每cycle的mount→eight物理/process均至少8，playing→cleanup至少12，bigint差分；同步边界仍可相等，未设毫秒或FPS下限。根AST实跑2正45负GREEN，并核验真实131149含钟日志接受、125329无钟拒绝。没有二审。

## 真实运行读数

证据均在`G:/knight/验证证据/october13-stable-claw/`；131149和132953两次原camera案例分别严格PASS/29840ms、29486ms，stderr均空。表内为已有边界差值，单位ms：

| 案例/周期 | Load | Instantiate | Mount | 原8组等待区域 | Playing等待 | Playing后全部检查 | Cleanup |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 131149/盆地 | 469 | 85 | 520 | 13313 | 2 | 2516 | 477 |
| 131149/上行低位 | 161 | 11 | 127 | 5462 | 1 | 239 | 136 |
| 131149/上行高位 | 128 | 8 | 153 | 5149 | 2 | 198 | 82 |
| 132953/盆地 | 507 | 65 | 449 | 13604 | 3 | 1999 | 301 |
| 132953/上行低位 | 197 | 8 | 140 | 5228 | 0 | 205 | 145 |
| 132953/上行高位 | 112 | 5 | 131 | 5256 | 1 | 247 | 144 |

131149的8组区域共23924ms/三个fixture跨度29263ms（约81.75%）；132953为24088/28763ms（约83.74%）。每区域process均恰好推进8，而physics分别57/42/42与52/42/45。原helper每次先await PhysicsFrame再await ProcessFrame，因此不能把组合等待当作单个物理tick；也不能从该读数推导硬件FPS、纯CPU用量或所有耗时都由一个生产方法造成。Playing后区域包括后续所有检查，不能命名为纯12帧相机停留。

## 耗时下一入口（只读结论，不是已实现优化）

启动准备器逐物理边界Reconcile，每次最多出生2个；每出生在AddChild/Ready/activation前后各做完整fresh admission。现有挂载时重新激活Hitbox/变形原共享shape负例保护这两个边界，不能合并或跨界缓存。

明确候选在同一次同步admission内：独立home和每段两端重复调用CheckBody，原PatrolPoints已首尾包含home却再Append(home)。50条布局理论每次全表552次body检查对应160个唯一transform；这是静态调用数量，不是实测成本比例，实际挂载子集也未计。若进一步优化，只能在本次同步上下文内复用完整精确Transform对应的纯值结果；保留未prefix端点Issues、collider+shape支撑身份、查询截断、每条corridor自身全部检查、零长段及公开fresh调用语义，不持有native资源，不跨前后两次admission。先量测真实占比，再实施与fresh路径等价的行为反例。

## 已有落地内容的复验与历史澄清

旧“同体跨shape仍红/尚未接纳三段”是更早记录，当前已修，不能据此重复改生产规则。新的`thruster-impact-support-20261104-133147-552-c0d1c4936820482ca5f5e2ff9df46d3c`严格PASS/19424ms，stderr空；覆盖移动台相对法向冲击、先墙后地、重叠、纯snap与持续携带不误记落地。三段静态同shape只给表现通道，伤害仍保留原可靠样本边界。该专项不签正常速度重量、美感或全地形自然度。

## 证据入口与剩余范围

- [唯一执行账](../../.superpowers/sdd/2026-11-04-camera-passive-clock/progress.md)
- [原十一项](2026-10-18-project-remaining-backlog-v001.md)
- [正式动作/物理返工要求](2026-11-03-character-motion-rework-requirements-v001.md)

## 最终同版验证：五门4PASS/1FAIL

下列五案全部终态，原时限不改；未重跑筛绿：

| 阶段 | 完整案例目录名 | 原生ms | 严格结果 |
| --- | --- | ---: | --- |
| 相机原案例 | ranged-camera-ground-20261104-132953-687-f437bbc4935a4d5499a7c06e342bc5ad | 29486 | PASS |
| 移动/混合支撑 | thruster-impact-support-20261104-133147-552-c0d1c4936820482ca5f5e2ff9df46d3c | 19424 | PASS |
| 完整pre入口 | prologue-pre-20261104-133330-447-b623a8b03bb841f08b96898a8c29ab89 | 177790 | FAIL：原HP5断言，非超时 |
| 完整post入口 | prologue-post-20261104-133712-352-4bc0624daae14110b159d49f420b2d95 | 260523 | PASS：原300秒门 |
| models（爪模型/到达几何子集） | models-20261104-134239-999-4e8dd7690d684d97b86e6eb0413ddca8 | 902 | PASS，非全游戏模型全集 |

五案各自source/helper/user/continuation前后清单字节SHA均一致；1888项source清单跨案SHA256为`D4B7C16F82305DCC3C58F3E75A55E56E34A247867141160C59606555F6D2AC4F`，6项fresh binary清单为`43BFEAE21FF8ED9F7B21D180F343DA75BF1ED669F99D2F24FE418B4A3F5A90FB`。这是清单SHA，不冒充单程序集或发行包SHA。helper各案纳入集合不同，不要求跨案清单相同，但每案均前后一致。真实玩家文件未改，全部引擎进程已终态。

post四警告逐项来自原明确负例：throwing ClawFrameChanged observer、VisualScene非法根、两次Completed→Recovering拒绝；未出现未解释ERROR。pre一条ERROR为原下降教学断言，未屏蔽。根汇总空stderr时首次命令因PowerShell的null字符串触发工具异常，随后以显式string读取完成全五案核验；没有因此重跑引擎或改变结果。

## pre失败的新证据与下一步

pre只完成原44项的前五项，Descent第六项失败，不能说全44项通过。相机30396ms、劳动者107568ms、爪目标1132ms、恢复10ms、补给11199ms；150687ms才进入Descent。原native180秒门仍紧张，本次非超时不证明性能合格或旧超时已修。

`DESCENT_SECOND_J_OBSERVATION`在physics4415/input405仍为同Player/HP5；末`DESCENT_TERMINAL_OBSERVATION`在physics4814/input804为HP4，mask只含8，身份、请求/恢复0和ReachBasin正确。故仅能定位伤害到(4415,4814]。末LastFallDamage0/Appliedfalse描述最近一次落地，不能排除更早落地或敌人/机关伤害，也不能凭MultipleContacts归因。夹具只隔离保存路径，未冻结敌人、机关或Motor，盾过热/松开仍正常运行；不能偷偷屏蔽危险以满足HP门。

下一最窄动作是给Descent的同Player局部订阅Health.Damaged，在既有同步事件内读取已接受伤害身份队尾并拍stage/physics/input/HP/位置/盾/爪/landingcount有限标量，finally解绑；现有Main有同类只读做法，不新增生产全局journal。Health先登记来源再扣HP发事件；落伤来源是Motor runtime ID、local attack是LandingCount。事件当场的LastFallDamageApplied可能仍是上一笔，不能用它取代来源。该观察尚未实现，不在本报告中声称已找到或修复原因。

旧v238 Main原899余敌HP1、pre超时与历史Handle/终结器故障不因camera局部通过关闭；旧Explore不自动给新构建续签。没有生产动作/动力或新美术增量，不以本报告代替用户可感知工作；继续真实启动性能、原完整主线和角色品质欠账。一次审查的I1已由真实RED→2正45负GREEN及新原生相机验证，但宽套件仍红，整项计划不签complete、不合并/发布。

## 取舍及代价

- 本轮先完成已有计时入口，不凭历史旧红重复修落地；代价是没有直接动作、美术或操控增量。
- 独立审查不覆盖整个脏worktree/全关；根补当前native门，但仍有pre失败，不能当整库质量证明。
- 仅无窗口证据，不签正常速度画面/操控/硬件；三钟也不能推FPS、纯CPU或唯一耗时根因。
- 保留原人口、危险、预算和失败，不扩时限/删敌人制造通过；没有commit/worktree副本/媒体采集或外部归档，保留所有原件。
