# v341 · 前半深渊实体扩展（开发进展，不是整关交付）

KNIGHT正式工程仍是 `G:/knight/Knight`，Godot/.NET。完整目标保持24段连续主路＋A–J十分路，Boss在23→24门前场，第一关止于真正的大门前；门后第二关不在本轮。旧76普通敌位、原族系/远程职能/机关能力及前段自然生态、密度要求不缩减。原十一项不能按测试phase数量替代或计算完成率。

## 已制作的实际内容

- 新可选开发图 `knight.first-gate.abyss.v1`：6144×8192，436节点，48结构实体，实际岩壁/顶壁/通路/桥/承接落台，01–12/A–F原型。不是24个切换小房间；共同父图中只有一个Entrance运输容器。
- 05双层廊、06横行接07内壁长降、08/09回折、10双井承接、11三石柱、12谷底右行。两处沿用现有500px/s制动气流，不重设重力/伤害/落差起点。03/04以前的旧入口、A和原默认正式运行图保留。
- B–F各有箱与两堆材料；全图30个已注册资源落点，总授予预算 mineral28/fiber9/salvage6/data3/echo-memory2。**摆放30不等于实际领取30**。D下层与E入口故意共用交汇，不按两间独立房或重复密度计数。
- B上路跨320px井口，需真实机械爪/短喷路线验证，未宣称普通单跳可达。C/D/E/F的入口、回接与承接层已实体建造，但全部分路Q/回返和一次完整30点探索尚未通过运行验证。
- 起点/A保留，新加深渊避难点与谷底避难点，四个稳定检查点，实际地图提取786432个8px格、五个雾场。显式独立默认存档 `user://knight/knight.first-gate.abyss.v1.json`；旧 `knight.first-gate.development.v1` 的3072×2048、旧槽和边界不变，不暗迁旧进度。
- FormalHUD根据实际profile显示中英01–12/A–F开发范围，不再误报旧01–04/A。开发终点不提交正式Completed/Boss成绩。

这是可替换程序/既有皮肤原型，不是正式精美环境、敌人或角色动作资产。本轮没有把旧坐标敌群强行放进新图，也不因此获得删减敌人数量/类别的资格。13–24/G–J、所有新图敌群与机关、Boss/门禁迁移仍未完成。

后半图的容量风险也未隐去：786432格已占现有1048576格上限的75%。若保留8192高度，密集整矩形地图最多8192宽，仅比当前多2048；不能为塞入后12段而直接放宽上限、缩坏路线或假称整图已适配。后续先核实际后半外包围与分块/稀疏图表示的成本和保存兼容，再落实最终范围；本轮不改这项全局上限。

## 实际主路与保存证据

审前同版实际主路 `abyss-main-20261110-181602-810-e2b1dd0fee984c33af045f5000e0b3e2`：真实保存父图/Player/UI/Flow，6398观察物理步，07连续下落262物理步约4.3667秒，结束HP4，四次分别接地普通跳、零短喷；真实Q领取11份主线材料，**不含07侧台与任何分路**。实际终点死亡恢复同一角色至谷底检查点，实际Continue只读/New清空；独立reader PID504与writer45464不同，冷读三SHA守恒、11奖励与谷底坐标。控制性无窗口自动输入，不是系统键鼠、真人或自然全关验收。

审后冻结1951源的主路再次严格PASS108574ms，writer41404/独立reader12240冷读1316ms，仍为11Q/6398步/262长降步/4接地单跳/HP4。到达终点后主动触发致死伤害验证恢复，不是终点必死机关。最终16组14严格PASS/2FAIL，终态与保管审计见 [最终验证](../../.superpowers/sdd/2026-11-10-first-gate-abyss/verification-v001.md)，不能把审前不同字节当审后证明。

## 长降伤害测量修补

实际长降首先暴露死亡：500px/s风已限速，但落差2272导致测量MotionMismatch，回退高度伤害5。被动同tick证据定位到净位移误差约.00805，其他接触/预测差为0。安装引擎固定commit的原生停止分支会取消剩余运动，小于margin的已走位移也回退；旧测量误以为总会继续滑动。

[固定引擎源码与反例记录](2026-11-10-abyss-impact-source-v341.md)。只修测量的分支重建，捕获FloorStopOnSlope/UpDirection/SafeMargin并拒绝中途变化，原.002范数容差、单slide/唯一完整支撑、重叠/深度/RID/平台速度/旋转拒绝及伤害公式全部保留。没有改物理、HP、风速、重力、地图落差或给角色补血。

一次fresh审查C0/I1/M2，root将浮点阈值M1按可能致死的效果升为Important，在同一轮TDD修补；不二审。真实边界RED184257后，比较匹配原生double `0.01`。最新专项 `abyss-impact-20261110-184746-326-76c50df19b4c4ce19dcf45efc65ab736` 严格PASS，包含七个原生kernel对照、三项属性变化拒绝、方向边界以及真实风中Player落地/不支持多接触负例。kernel的受控高度2272只用于原伤害消费者核对，不冒充正常路线注入。

继续模式的单接触正例是垂直运动，切向剩余投影为0；100px/s切向样本实际产生两接触，作为MultipleContacts负例保留，不改MaxSlides或伪造唯一支撑。中文气流标签尚未本地化，作为Minor延期，英文安全视听仍未合格。详见 [审查与所有取舍](../../.superpowers/sdd/2026-11-10-first-gate-abyss/review-result.md)。

## 未解决的严格门

审前14组回归11PASS/3FAIL：场景authoring断言完成却退出48ShapeRID；pre44返回但七次无效GCHandle；post在index66实际未进入Playing，archive-side-cave.03缺profile触发统一绑定拒绝，九resident未准备，随后原生终结器fatal0xC000001D，非超时。该位是bone-charger，日志明确allow_placeholder=True；不是用户没授权占位，也未证明其余九个都缺profile。表层scope回执错误不是完整原因。未证明这些故障由本轮改动引入，更未修复；旧50RID/Handle/FATAL/Main/Explore失败原件保留。不能拿偶然退出0或局部绿当稳定版。

最终同版16组14PASS/2FAIL：当前authoring仍48ShapeRID；当前pre44严格通过，但post在index51 ContinueAndCorruptRecovery因staggered-rift.03的Profile为空未进入Playing，随后/交错原生终结器fatal0xC000001D、exit−1073741795，非超时。不是审前index66骨冲失败；两份原件保留，不从相似消息推断同因。完整原十一项、全部分路、密度、最终美术动作、正常声画/目标硬件/真人游玩与交付仍OPEN，完整目标ACTIVE。仅无窗口制作，没有GUI/OS输入/付费生成/自动化/Git/清理/新崩溃采集/GC或引擎试验。

终态独立核验：当前1951源A6F9/六产物C8F8与全部16回执一致，7896实际前后快照文件、96实际二进制复制、12helper当前路径和36终态进程合同全部核对；104受保护用户文件/109行清单SHA9FB16不变。不是将清单行数冒充每案实体备份。正常流程内一次容量检查为21736453字节/185文件，未到新增20GiB档，不新建自动化。

只读后续调查没有把问题过早定因：骨冲磁盘叶仍显式绑定Profile，manager持有来源、holder不Dispose借入Scene/Profile；该进程普通记录缺setter/对象身份轨迹，不能认定终结器处理的正是这份Profile，也不能从九个未准备位推断九次空值。可复用原 `thruster-formal-recovery` 核对64/65/66恢复前缀，不沿用含强制GC的旧实验。纯authoring的原始图已有Free根后Dispose形状逻辑，且除48结构还创建3检查点形状；只凭48退出计数不能断言“48个形状未释放”。下一最窄入口是创建起身份账本＋一块实体的原PackAndSave成功/拒绝控制，严格区分自己new与借入/重载资源；ObjectID失效也不替代RID退出检查。本轮未据这些假设改生产代码或执行资源实验。

## 来源和保存位置

- 可编辑几何：`tools/build/prologue/FirstGateAbyssTerrain.cs`，共享旧入口 `FirstGateEntryTerrain.cs`；共同父图 `BuildPrologueLevel.cs`。
- 新测试/冷读：`tests/integration/FirstGateAbyssRuntimeTests.cs`、`AbyssWindLandingTests.cs`、`tests/RunAbyssColdRead.cs`及两个unit文件。
- 实际保存叶、父图、三SHA奖励见证和逐行原日志在 `G:/knight/验证证据/october13-stable-claw/<对应case>/storage/` 及同case根，不是覆盖正式运行图或真实存档。
- 设计/计划/过程账：`docs/superpowers/specs/2026-11-10-first-gate-abyss-design.md`、`docs/superpowers/plans/2026-11-10-first-gate-abyss.md`、`.superpowers/sdd/2026-11-10-first-gate-abyss/`。
- 不新增普通过程截图/视频；只关键节点留档，新增20GiB只在正常汇报顺带告知，不停工、不设定时自动化。
