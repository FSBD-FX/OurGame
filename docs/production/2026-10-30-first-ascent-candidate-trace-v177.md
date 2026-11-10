# v177 · 同次抓点候选与真实首撞

原十一项和正式交付目标继续ACTIVE/OPEN。本轮增加可选同次查询诊断与对应验证，**没有修复上行通路，也没有新增美术/技能或改变选点规则**。原八方向、角度/距离/稳定路径排序、近敌相对后方地形提升、LOS/range/材质和16/32/300/600s原预算均不变。

## 事实：当前路线为什么没抓到3956

实际案例：`G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261030-192441-608-c1174e2cc64e43528efe434023afe360`。219884ms、未超原600s，仍于physics11760耗完原16次上升预检而严格FAIL，HP1保留、K未发。此前机关往返继续完成，不能用诊断代替通关。

本次原16个同步ResolveKeyboard调用各有33个不同ray索引，共528条真实首撞记录，全部dropped0/faults0；每次同原生查询记录，不额外发射另一组ray。计数来自原调用点，不是外部引擎仪表；原返回后才输出数据，任何记录都不授予抓取许可。

| 上升预检 | 实际观察 | 最终选择 |
|---|---|---|
| 1–7 | 原33条首撞没有采到3956目标，不能推断具体遮挡者或目标不存在 | 1为Step3636；2–7为近处Missile ThreadTarget |
| 8–9 | 各有一条首撞采到3956，但原后续LOS返回false，Candidate=Occluded | 更近敌人合法提升 |
| 10–11 | 目标可见且合法，但角度输给原baseline，Candidate=BaselineRetained | 更近敌人合法提升 |
| 12–16 | 每次至少一条3956合法候选仍输角度；13/15另有部分候选Occluded | Step3636原baseline保留 |

第10次：3956候选距313.8044、偏角0.14726204rad（约8.4375°），已实际LOS=true；Step3636 baseline距476.1893但偏角约0，故按用户保留的角优先赢得地形基线，再由距187.87767的合格Missile敌人提升。第16次：3956距约293.86–298.34、偏角12.656–14.063°，仍输给距447.2566但几乎正对的Step3636。

推理：这不是单一“爪牵引力不足”或“NE键没有按下”，也不能概括为一直上方平台抢占。至少涉及当前起跳位置下的首撞/LOS与既有合法排序。风险：本次LOS只记录原bool，不含其具体遮挡collider，不能宣称已找到第8/9次遮挡实体；未采到也不等于超射程或全锥无路。下一应据实际几何验证原普通跳可用起点/方向窗口或局部布置，不改全局角优先、加二段跳或凭空延长预算。

## 实现与边界

新内部固定buffer只持枚举、数值、Vector2及已计算的managed路径字符串，无Node/Dictionary/资源跨await。普通公开入口传null，不创建记录或额外取路径；trace模式在原33ray/原短路LOS、baseline及最终enemy promotion复制已有数据。元数据读取故障只计Fault，饱和只计Dropped，不回调任意Action、不试用receiver资格，不改变选择或跳过查询。容量0/1/已满也继续原查询。

已有真实保存Moth＋原生地形的小fixture验证ordinary/explicit-null/full/0/1/已满时**完整resolution所有字段一致**，并覆盖enemy胜出与terrain/显式交互fallback；30个样本均完成。该fixture不是当前整图，也不外推全世界选择正确或目标硬件性能。

## TDD、回归与审查

| 案例（均在同一证据根） | build/primary native ms | 严格结果 |
|---|---:|---|
| keyboard190759…46e07de3 | 27537 / 1470 | inert重载未记录同次查询，实际KEYBOARD_TRACE_RED |
| keyboard191107…88d6ddd6 | 29196 / 19078 | 当案合同PASS；另独立title-entry7737ms PASS |
| keyboard191430…033271f6 | 37032 / 19295 | native0，但新裸OK整行门exact0，严格FAIL |
| keyboard191625…8a69fd19 | 48012 / 24190 | native0，新OK虽唯一但有字段，旧裸门仍exact0，严格FAIL |
| keyboard192118…2a9dc104 | 32786 / 20289 | 完整字段整行唯一新门及原keyboard PASS；独立title-entry8046ms PASS |
| Explore192441…c1174e2c | 28761 / 219884 | 新证据齐全、原16失败保留，完整Explore FAIL |
| models193203…f5edceae | 43091 / 573 | 当前机械爪模型子集PASS，不是全部游戏模型 |

唯一fresh审查C0/I1/M1。I1是成功marker与严格整行合同不符：旧门不是检测到“30条合格重复”，而是匹配裸OK得到0。保留两次strictFAIL；root将新门固定到完整字段、仍锚定整行且唯一。用**实际canonical foreach AST**的内存探针确认真实单条允许、缺失/重复/错误后缀/尾缀junk拒绝，再由192118原canonical整案GREEN完成唯一C/I修复，不二审。

Minor延期：自动化测试目前验证结果/计数/Result身份，但未直接钉住focus拒绝、Ray索引及首撞ID、baseline与promotion的内容；未来漏写某类行仍可能逃过该小测试。当前实际16块已逐项核33不同ray/0drop/fault、focused理由与最终身份，但不把一次实图核验冒充补齐未来回归。metadata异常注入未实测，历史v175/v176问题、其他dirty或正式图声不属本次源审查结论。

## 裁定与保管

选择原查询取证而不复制33ray或猜改优先级，代价是增加可选诊断和约4分钟实图验证，未保证马上通过。使用有限无回调buffer，代价是饱和/故障时证据必须保留不完整边界；调用点计数不冒称引擎外部仪表。审查范围外的历史资源/dirty、完整地图、视觉/硬件/真人继续未验，不借fixture或源审查关闭。

7案例均成功build，7个primary及2个title-entry secondary合计9native；案例严格3PASS/4FAIL，两secondary各自PASS，不替代任何失败整案。35清单对/3384选定源实体/90helper实体/42bin逐件SHA一致，前后零差/未列及hash错误0；16个owned build/native PID均结束。当前1868源/6bin/models实际9helper绑定末案，HEAD/index仍 `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6` / `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。源码/helper冻结到native结束；无付费/新原生采集/窗口OS/自动化/Git/删除/真实存档写入，无新增过程媒体。

最终单次媒体检查2026-10-30T11:39:12.7944804Z为7427750bytes/86files/原baseline3508/Notify=false；只随正常工作检查，无自动化。相关git diff --check退出0，仅原LF→CRLF提示。审查未核的历史dirty/资源、metadata故障注入和完整视觉硬件真人维持OPEN，不因source审查或局部fixture提升资格。全suite未绿不写task-done/正式完成，完整原目标持续ACTIVE。

原Main配重区能量0被常驻连发枪手击杀、pre原180秒超时/Handle、post1Handle及历史FATAL、正式美术/连续动作/UI/音频/正常速度视听、硬件性能及用户试玩等原十一项缺口全部保留；不称完整suite或正式交付通过。
