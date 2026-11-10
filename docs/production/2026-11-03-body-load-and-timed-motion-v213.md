# v213 · 实测承重与按时间衔接动作

状态：**生产表现已接入，一次独立审查C0/I1/M2；I1真实RED→GREEN修补。最终同源11门10PASS/1MainFAIL，Task1与完整序章仍未完成，不是正式动画/观感验收。**

## 事实、推理、风险

**事实：** 本轮在现有分层接入实测加减速的骨盆下沉/上身受力响应；动作衔接从每次发表固定推进改为生产累计时间。未改Motor力、速度、重力、碰撞、输入、战斗/精力或地图，没有新图片/素材生成/Spine采购。程序幅度与节奏是开发侧样段，不是获批最终动作稿。

**推理：** v212脚已能支撑，但躯干仍主要按起跑/停步模板移动；松键实际减速期间仍可能是Run。新增承重基底按真实速度差分，在仍移动时就表现制动受力；修正同一时间重发继续推进混合/收势/后手接管的问题，避免把调用次数当动画时间。

**风险：** 这些是原生几何/程序行为证据，不证明优雅、角色特色、正常速度观感或实际手感已达标。程序层与正式身份总装/美术资源的欠账仍保留；没有用短喷参数或更大倾角替代完整动作设计。v212 Main899失败和原生资源fatal均OPEN，本轮不能借旧通过签当前完整版本。

## 生产链与边界

- Timeline提供可选累计PresentationTimeSeconds，真实零delta事件不加时；Formal每次只消费一次，loco/thread/收势/后手接管共用。无时间的旧帧保兼容，明确没有生产时间保证；非法时间冻结、回退重基，重复时间不推进。
- BodyLoad只存数值，按真实physics stamp/step和速度差分推进一次，区分加速、被动减速、反刹；面对转换仅转局部坐标。空中/短喷/死亡/恢复/失测/非法输入清理，传送/长断档重新起算，不制造假加速度。
- 现有髋rest平均点为枢轴，腿仅承重下沉，八个上身/附件槽作共同刚体群组变换；一次审查修正了原“先承重再混动作”的内部冲突，改为动作混合后、二级运动前统一承重。动作保留相对躯干的手/武器设计，而非固定绝对世界位置；披风二级运动仍仅一次、最终支撑脚IK。无角色根平移、贴图拉伸或肢段缩放。
- 当前幅度：2200px/s²参考，.085rad最大上身力学响应、1.2px承重项＋最多.15px速度项，.06s指数响应；入口2/15s、收势1/6s。是待审阅参数，不等于现实单位标定或用户批准的风格。

## 测试先行

证据根G:/knight/验证证据/october13-stable-claw/。

| case尾码 | 原生结果 | 证据边界 |
|---|---|---|
|52db7b38|526ms/exit1|真实模型RED：加速/承重、被动减速、镜像、断档/三速率非空及累计时间缺失|
|94f94531|607ms/exit1|生产模型19项通过，实际Formal零时间步态衔接仍RED|
|06637f47|603ms/exit1|六组实际Draw RED全部保留：loco/release+hand/thread重复时间、采样率、非法/回退时间、躯干/髋|
|bcb85b22|3763ms/exit0|首次专项GREEN，实际Motor承重60观察/前倾16/被动回收17|
|0fd9d348|3852ms/exit0|扩展专项GREEN：持盾后收势、爪/后手15点几何30/60/120、上身及死亡/恢复优先级|
|a9688077|715ms/exit1|审查I1实际附件RED，full GroundAttack肩核相对身体错位|
|46a2d82d|621ms/exit1|扩展I1 RED，full/partial×两朝向共32锚点/轴向不一致|
|6ed7c0e5|3857ms/exit0|一次修补后四组真实附件/轴向通过，全部Body与原Gait专项保留|

专项保留全部v212旧4审查合同、19步态模型、540绘制样本/569支撑、实际Motor60次/65支撑；新增Body模型19项和真实身体/髋/手部几何。内部Action输入不是OS/完整键盘路线，采样率不是硬件FPS，Draw观察不是像素或人类认可。

唯一fresh whole-change审查在0fd9d348后发出；期间冻结源串行原thruster-presentation-rig2c34636d(2288ms)与thruster-rig-compatibility89d1d42f(1169ms)严格通过，但修补后不借这些旧结果签新版。6ed7c0e5为当前最终源码专项，build28854/native3857、exit0/noTimeout/failure=null/guards[]；现在冻结全部CS/helpers，串行重跑原表现/兼容/策略/actions/keyboard/models/Main/Explore/pre/post。SourceRows1873，build180s/专项55s/路线600s/pre180s/既定post300s不改；无ExpectedFailure。

## 最终冻结版本回归

以下均为6ed7c0e5修补后的当前同源；无旧通过拼接，单位ms。各案位于G:/knight/验证证据/october13-stable-claw/，短号对应目录的唯一GUID前缀。

| 阶段 | build | native | 严格结果 | case短号 |
|---|---:|---:|---|---|
| ground-gait/body | 28854 | 3857 | PASS | 6ed7c0e5 |
| presentation-rig | 33043 | 2386 | PASS | 62b53557 |
| rig-compatibility | 32507 | 1375 | PASS，formal11/mantle2子集 | 0147d02b |
| ranged-mixed-policy | 31365 | 3769 | PASS，仅策略/隔离接线 | 4e7976e7 |
| actions | 29581 | 9405 | PASS | 11c034f2 |
| keyboard | 27488 | 20276 | PASS，额外headless标题7721 | 1864a720 |
| models | 28646 | 512 | PASS，仅原爪模型子集 | 6cb4cb06 |
| Main diagnostic | 29281 | 129012 | FAIL，原配重混战899 | a462db8f |
| Exploration diagnostic | 31228 | 335581 | PASS，Boss/实体支撑出口 | 4e1a5d63 |
| prologue-pre | 27081 | 163934 | PASS，本次无ERROR | 1d0a3a63 |
| prologue-post | 28653 | 205701 | PASS，原300s门 | ef77779b |

最终**10PASS/1FAIL**。全部build0警告/错误，原cap不变、无timeout、guardFailures=[]、无ExpectedFailure。post仅原4条限定负控WARNING（seamless1/profile1/flow2），无ERROR/FATAL，不是新增静音规则。键盘标题是隔离headless契约，非用户看到窗口；采样率不是硬件FPS，子集不能签全部模型/兼容。

Main仍为原counterweight-lift真实混战断言：末帧899、playerHP2/RustHP1/J7、原floor/gap41.217、精力16.133251，未放宽24储备/时钟/HP/三弹/单跳门。当前正常计划诊断未完成主线，不称新动作修复了通关。

Explore当前完成Boss与实体支撑出口、恢复0/0及持键60/完成一次；不把save SHA不变当成OS写入次数证明。pre本次quietPASS不解释或修复v212/更早的间歇Handle/终结器故障；历史失败保留，资源稳定性继续OPEN。完整目标与Task1不因本轮10/11而完成，也不立即重跑Main/pre筛绿。

## 唯一审查、修补与延期

I1：动作full weight只覆盖Body/两臂/武器，旧premix load却留在肩核/披风/灯/能量丝，NearArm–Core距离约4.47→1.97px。先取得真实锚点RED，再覆盖GroundAttack/Contact1、Shield/Recovery.5及两朝向的4锚点/轴，32检查均RED；修补后全通过。采用统一父级空间的后混合承重，既不把动作姿态抹平，也不在攻击时取消物理反馈。原世界槽绝对相等测试改为实际手/武器相对躯干几何守恒；这是一项明确设计修正，绝对战斗轮廓随受力改变，需后续正常速度审阅。没有第二审。

## 最终保管

独立只读核账按增量7＋3＋9＋1案完成，不重哈旧批；累计20案14PASS/5真实RED/1MainFAIL，100清单对、9732份选定源实体、210份helper、120份bin及41个owned身份逐SHA核对。最终current1873源/10个post helpers/6 bin全部匹配。05:10:23 +08按保存creation/path核实41个身份已结束；PID74784/75276被其他程序复用，不误判旧owned，KNIGHT Godot/dotnet为0。

sourceManifest8FD57724AAF0F2A7EEA9C0DD5AC5B1538851C005149C82E8C74F4D134EB552C5；post helpersManifestA63F3B193D6740114D91376AA3603B109E096716DEC051E19C44CAEA89F7DC05；binManifest18C5EB3F5AD1E5618D3400FB6DC87615498BB5F6F80C7605B84D68222FBBA931；Knight.dll3A08B3DFF11FAAB11C071AE1E9B0AFC0E030DF87F8D3DD06DE10BF6871CFAE00。各阶段helper数量9/10/11按consumer不同，自身前后相符，不把阶段间差异称漂移。

HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6、branch codex/task6-certified-final-20260902T0555、index21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C保持原值。保护的109份用户条目与默认应用数据未改变；无外部发布、删除或真实存档操作。无新过程媒体，20GiB检查仍随正常工作汇报，不建立自动化。

延期小项（未修）：

- M1：公开兼容PlayCue在timed basis后，爪类cue可只有clip标识而没有可见动作姿态；正常Coordinator明确绕过此兼容入口，不能混为正常游戏机械爪失败。适配器回归仍欠修。
- M2：新增Body测试尚未覆盖瞬移、反向跨零、反复非法后恢复、timed cue重入与跨速率Body producer→renderer整条链。I1补的是附件full/partial/两朝向，不因此追认其余覆盖。
- 历史v212极端有限值派生溢出欠账沿用，未以本轮Body的double计算关闭。

## 本轮裁定与代价

1. 用户要求判断后合理直接执行，沿既有身份/分层做程序承重样段；幅度/节奏仍可能被审美否决，不冒认最终稿批准。
2. root独写、精确基线、一次独立审查，保留现有脏工程，不建新worktree/提交/发布/删除；不具有干净发布分支资格。
3. 仅实测时间路径获计时保证，旧未测时帧保兼容；旧适配器仍可能依调用次数推进，不能泛化计时通过。
4. 审查未判断Main/pre fatal，保持原失败与未知根因；完整路线与稳定性仍不合格。
5. 无窗口几何不能判断正常速度观感与节奏品味；美感/手感仍未验收。
6. Body承重可使用真实grounded速度而不要求精确collider/bounds；脚底另用严格步态支撑，承重值不能当精确接触证明。
7. 旧帧按调用与timed重入丢首区间保留既有兼容语义；没有多耗实测时间，但首区间可能暂不推进。
8. 极大有限时钟/delta没有已证真实生产触发路径，不扩大为故障修补；不签极值全范围鲁棒性。
9. 同tick缓存的传送需等新物理样本；无已证严重玩法影响，仍可能有最多一物理tick暂态。
10. 旧动作/附件的原有设计偏移不作本轮美术重画；只修新增load不一致，旧审美欠账不消失。
11. I1迫使修改原premix方案，改为动作后的共同承重群，保证角色化动作相对关系并保留战斗惯性；绝对姿态改变仍待用户观感确认，不改变输入/命中/数值。

## 资源崩溃的本轮新增只读定位

v212 pre7c7580c3 stdout最后在CurrentBasin负例BLOCKED_LANE_REJECTED，下一句是walkway夹具；入口没有标记，尚不能判定它已创建或挂载。到此已有3个camera完整Level、1个off-tree factory、CurrentBasin normal与blocked两个Level原清理返回，共6个根；清理/AudioDrain返回不等于其全部托管包装器已finalize。

本次尚未执行BasinClaw、HealthRestore、Supply和RunPrologueTests退出GC，可以排除它们为这次已执行的同步触发点；不能排除更早对象的异步终结。当前original keep-out由ResidentSavedBodyGeometry.TryRead读取，不是在此前缀反复实例化/Free original enemy probe。没有nativeID/类型/handle值，仍不能指定失败对象。

现成basin-resource-pre-prefix保留camera→CurrentBasin业务前缀，但Stable入口多一帧，且不自动激活RunPrologueTests中的BeginOriginalProbeTrace。故不能把它称为等价身份观测或把通过当修复。本轮没有重跑该入口/完整identity-pre，没有加GC、强引用所有资源、改变缓存/引擎或额外dump。下一资源步骤应补当前walkway原生边界的可靠观察/归属，而不是继续猜最后一个资源。

## 未完成

原十一项与R-I可见惯性、R-C角色专属动作、R-P物理/操控反馈继续OPEN；尚需完整当前双路线、严格资源稳定、全动作设计/成品美术、正常速度视听与真人/硬件验收。当前只无窗口工作，没有真实存档、GUI/系统按键、付费生成、媒体采集、额外dump、自动化、Git发布/删除权限。

下一V05入口只读核对：四保存远程场景确有双管/弹链/筒/棱镜武器区别，但共用RangedWeaponVisual身体、头、腿和后坐/死亡，只有Role导出且表现器硬取具体类，没有四套正式身体/武器动画或直接资源替换合同。当前工程assets/production/prologue-assets.json只有通用enemy-atlas，final:true不证明四类专属美术。它不是嵌套Knight副本；先前只读报告链接多写一层的手误已纠正。此为原V05缺口，不是新美术稿批准。

下一Main策略边界已发非阻塞问题、尚未获答复/未实施：原预警Hop的唯一普通跳限制只覆盖选定Burst周期，Stage5真实落地后该观察器不再累计全场跳数；不能据此假称用户已批准另一避弹跳。额外地面跳仍会破坏当前brake lease的跳数守恒及未完成LaterK守恒，不能重拍基线/清历史伪装原证据。已询问是否允许完成原三弹证明并真实落地后另用普通地面起跳；不增加空中二段跳、不加血/改敌人/延时限，答复前不改该策略，其他原十一项可继续。
