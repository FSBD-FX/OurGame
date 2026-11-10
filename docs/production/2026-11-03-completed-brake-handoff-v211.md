# v211 · 命中后解除旧制动保持，主线仍未通过

状态：**最终同源八阶段7PASS/1MainFAIL，完整序章/正式动作物理体验未验收**。

## 事实、推理、风险

**事实：** 仅改原Main与RangedPlacement两份测试CS；没有修改生产角色、敌人、地图、伤害、能量、目标选择或美术。保留原900决策、真实输入、HP/恢复、J/K/单跳三弹和严格原生错误门。原十一项仍ACTIVE。

**推理：** v210的实际命中后，已完成的stage4保持仍覆盖原来的近距离退让。现在只有在完成制动后，更新的真实攻击开始且命中同一存活Rust，才解除旧保持；stage4本身不重置，原方向刷新和fresh-J规则继续生效。

**风险：** 本轮是测试驾驶修补，不是生产物理或动作返工。两次持键再松键至完全停止的预测，只是原平台走廊准入，不保证未来防御脉冲、碰撞或外力，也不能证明完整通关和美感。每次普通移动重新核对真实平台、胶囊、配置与收据，防御仍是最后移动所有者。

## 测试先行与接线

- 三十个独立字面量控制，覆盖真实新攻击/命中、旧事件/错目标/未完成制动、完整右/左/反向初速停止包络、剩余预算、缩放及非法输入。先见具名六项RED，再见30控制GREEN；不是用编译失败冒充RED。
- 完成制动时保存真实AttackStarted/Connected计数。既有同Rust Connected回调只锁存解除条件，不发键、不重启制动、不额外等待。
- 命中后的普通A/D，仍要求原平台/shape/transform、原motion/module/scale、原两K收据及跳跃/短喷/爪计数；使用实际vx、当前dt和剩余原900预算，检查两持键采样→中性→完全刹停的正负位移包络。没有把“当前踩地”当成未来支撑。
- 原reserve24、安全clock、冷却、盾释放、strike geometry和目标选择不变。真实新J之后不再拿旧AttackEdges计数冒充必须不变的收据。

## 已有实际结果

| 案尾码 | 阶段 | 已确认结果 |
|---|---|---|
|133efb14|纯helper旧/stub|native537ms、exit1，具名六项RED|
|67b66bd5|纯helper实现|native3784ms、exit0，30控制和旧fixture通过；尚未接实际回调的版本|
|76d98150|实际回调/支撑接线Main|native144009ms、exit1、未超时，原900仍FAIL；HP2/Rust1、7次J均命中|

I1修补后的最终Main c66c6d09：native127848ms/exit1、未超时、guards=[]，仍是原899终态HP2/Rust1/J7、gap41.217和原平台；同Rust真实release在physics7030/loop868，退让7035/loop874，geometry_retained=True；7036开始实际axis1/vx36.666668。与修补前本案的具体physics/actor ID不同，不互借身份；不能将局部成功写成主线通过。

同一最终源Exploration 3cfe2d4c：native327130ms/exit0、未超时、failure=null/guards=[]；明确THRUSTER_ROUTE_DIAGNOSTIC_OK normal_plan=true/actual_ranged_gates=true/boss_and_supported_exit=true，恢复0/0，audio5frame70ms/persist109。它是实际当前探索线的无窗口通过，不是主线通过、正常显示/真人或性能认可，也不是全部目标完成。

Main本案的独立事实：

- physics7032/loop868，真实同Rust Connected，starts6→7/contacts6→7，stage4，HP2；不是由命令推测命中。
- physics7037/loop874，原平台[4600,4800]上的退让准入。后续真实采样axis1、vx36.666668→73.333336→110，证明不只是日志里发了命令。
- 最终玩家X4676.069、RustX4634.852、gap41.217，仍在原平台；旧v210的近身gap1.505不再被保持状态锁住。
- 玩家仍HP2、敌人仍HP1、精力约16.1，最后仍举盾；原预算内没有完成下一次反击，不能称主线完成。
- 本案音频drain1frame/57ms、真实C应用数据保护109项通过，只证明本案，不关闭历史Handle/Audio问题。

剩余原门核对（本案日志outer1548–1578/resource3420–3485，不把短路项写成通过）：

- loops869–873，strike通过后counter admission先失败；874–877先败strike，其余未求值。
- loops878–882退让恢复strike，但仍在攻击；883–889攻击结束，原冷却仍在。
- loops890–895几何、冷却和非攻击状态已具备，但精力18.13325始终低于原reserve24，剩余安全clock0.2167→0.1333也不足。
- loops896–899实际举盾，精力继续下降；全段未再进入fresh-J重查，没有遗漏的满足原条件的J窗口。
- outer27行只求值strike/counter两bit、4行只求值strike；后续mode/cooldown/afford等不冒认为已通过。资源日志的通用energy_gate不是reserve24专用标记，本结论依据真实精力读数与原counter条件。

第三案独立核账：build28741ms/0warning/error；native严格ERROR1/exit1；caps180000/600000、headless argv匹配，未超时。五清单对/570份选中源/10helper副本/6bin SHA一致，当前285选中源/9helpers/6bin匹配。两个owned进程按creation/path确认终止，无进程操作。sourceManifest149F3606B647AFABA99C54DFA68620431BE518D407898D0739C6C2F18669FC8B，binaryManifest48CF43D8E9A58F624B4065AD9D9D69C1C0D8BBD37DFA5FF28D5F569CE5F00CE7；它不是最终完整回归资格。

## 唯一审查与一次I1修补

fresh whole-change审查C0/I1/M0。I1指出同一RectangleShape2D的Size、同一CapsuleShape2D的半径/高度可以原地改变，节点/资源ID与transform不变，旧门仍可能采用扩大的走廊。

独立反例：原平台[0,42]、body[2,4]、两次持键再停总位移40，原本必须拒绝；保持中心21与资源ID不变，把宽42改成46得到[-2,44]，旧门误准入。新增真实可变Shape资源控制、胶囊参数/局部偏移/世界basis等负控后，101f90f8九项真实RED（native620ms），不是编译错误。

同一修补轮保存原始值快照：rectangle.Size、body节点/capsule资源ID、radius/height、collider局部transform、player世界basis（允许正常平移）；有限/有效性和精确未变检查同时进入制动守恒及后续退让。当前真实接触/胶囊走廊和原floor/shape/worldtransform检查仍保留，不持有新原生资源、不改玩法。06865ccd的41控制与旧fixture GREEN（native3756ms、exit0/noERROR），无二审/新增Minor。完整冻结回归尚待结束，不能把这一GREEN写成整套通过。

## 最终同源验证

| 案尾码 | 阶段 | native ms | 严格结果 |
|---|---|---:|---|
|06865ccd|policy41/旧fixture|3756|PASS|
|e379f761|actions|9503|PASS，仅接口/行为非动作审美|
|c0b5a705|keyboard|20331|PASS；隔离headless title7721亦PASS|
|0d8da7f3|models|618|爪模型子集PASS，不冒认全部物理模型|
|c66c6d09|Main|127848|原900 FAIL；HP2/Rust1/J7/gap41.217|
|3cfe2d4c|Exploration|327130|Boss＋实体出口/无恢复PASS|
|84d908cc|pre|166779|原180s内PASS，无ERROR|
|28c6c9ff|post|203363|既有原300s内PASS，不算180s性能资格|

最终八阶段7PASS/1FAIL，串行批次因此返回1；本轮12尝试合计8PASS/2具名真实RED/2MainFAIL。未用ExpectedFailure，没有增加原180s build、55s policy/键盘/动作、600s路线、180s pre、300s post。当前完整套件仍未全绿，不能把任务或序章标为完成。

唯一审查及I1已按RED→GREEN修补，新增Minor0/无二审，旧延期不清除。所有最终阶段使用I1修正后冻结源；post结束前没有修改CS/helpers。pre本次quiet不能消除历史Handle/finalizer错误。最终保管审计单独追加于本页，不从退出0推断。

post150边界组成75组index0–74同名有序enter→return、错配0，owner8/nodes1→1，finalizer/drain/phase/fullOK齐；仅4原限定WARNING（1/1/2），ERROR/FATAL0。全部构建0warning/error；当前各phase无timeout、guard=[]，headless且无fixedFPS/movie。

## 最终保管

独立增量审计总计12案、60清单对、6018份选中源副本、136份helper副本、72份bin逐SHA一致。最终8案同源同bin，当前完整1872源、post专用10helpers、6bin与最后post零差异。2026-11-03 02:13:30 +08按保存creation/path复核25owned（12build＋12engine＋1title）全部结束，旧PID均不存在，KNIGHT工具进程0；无进程控制。

sourceManifest `CAE87419F5FE68A8489DCD8054144AE8DCB41DBD9F8D4D82820BFF4A3BC19147`；binaryManifest `2EA7D6D4B9F9A30964C4C17375DD3BF6B60D48722A25F36516F62BAB8BF716D7`；DLL `CC0084172F3F8E00BD908CCF58C11605E61B67F52F88E90BF57D0221F5196F20`；postHelpers `C5EF45802948AA866E2659414BDBE4BD8D22B0D980D459CAA292638C213429E7`。

Main `E30A8A548D597B7376DD9CA486E83E793BC78273ADEDC68C5B9B57EAE70DBE68`、Placement `D7FA627B62092B933BD4E234928B42C5A6B73B0057932F271E9223783D311D1D`；canonical `7A79C29AEBC02A9226B5ACA987D1C7B26FAF5CE5A4AF534CFC12E2123669BE19`未改；HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`保持原值。diff-check无空白错误，仅LF→CRLF提示。媒体检查7,427,750bytes/86files、Notify=false/NewThresholds0，无新媒体/自动化。

后续优先进入原V03的支撑脚、膝踝和重心连续样段，再联动原V04灯/披风/武器；源码核对只指出明确实现缺口，不等于新动作稿已认可或生产返工已做完。主线剩余能量/安全窗、间歇资源错误与原全部要求继续并行保留，完整目标不关闭。

## 裁定及代价

1. 保留dirty状态/失败/基线，manual ledger，不Git/worktree/commit/删除；代价是无clean release资格。
2. 已批准范围内继续headless测试驾驶，不扩GUI/输入/付费等权限；代价是不能作为正式玩法/视听验收。
3. 两held samples再完整停止的包络而非只看当前支撑；代价是可能保守拒绝，需要真实Main核对。
4. 审查未判的剩余Rust/整路由继续原900严格失败，另核能量/安全窗；代价是未获整Main资格。
5. 审查未判的生产惯性/动作/美术保留为强制返工，本轮不冒认；代价是无手感/视觉认可。
6. 历史Handle与Burst17精确配对继续OPEN，不猜原因；代价是资源及来源归属未认证。
7. 完整回归由root在最终冻结源执行，不借旧GREEN；代价是有任何错误仍不放行。
8. 审查限定两份保存基线差异，不吞无关历史dirty；代价是本轮审查不是整分支批准。

## 原目标边界

完整Main/Explore、历史资源错误、正式连续动作/物理手感/美术/UI/声音/相机、目标硬件和用户正常速度试玩，仍是原十一项的未完成要求。用户最新反馈见[角色动作与物理返工](2026-11-03-character-motion-rework-requirements-v001.md)，参数存在不抵消体验反馈。本轮没有新视觉或付费生成，也没有前台/系统输入、额外崩溃采集、自动化、真实存档操作、Git发布或删除。

全部原始案例保留于G:/knight/验证证据/october13-stable-claw的具名目录；精确计划/基线/账本保留于.superpowers/sdd/2026-11-03-completed-brake-handoff。本轮不是发行版本。
