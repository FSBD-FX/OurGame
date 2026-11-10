# 齿轮左侧接落台与回营物理兼容 v050

2026-10-22，Asia/Shanghai。权威资源根 `G:/knight`，实际 Godot 工程 `G:/knight/Knight`。这是内部制作进展，不是新发行版或完整序章验收。完整1–11、常驻Tasks1–4继续ACTIVE；不恢复旧归档项目或自动化。

## 事实：实际制作与运行

在既有批准的围合侧层方向中增加一个可替换TempleMetal实体：`GearWestLaunchShelf`，最终世界矩形 **(1932,3746,168,40)**。独立碰撞、skin与小地图仍由现有生成器接线，无新图片、声音或付费生成。没有改机械爪选点规则、拉力、角色40×72胶囊、重力、惯性、原危险区或机关扫掠。

真实保存Player从原营地开始，通过实际W/K到原Abyss西墙，取消后到原底板；A/D绕西沿落上新台，站稳跑动后K抓新左棚上表面、实际Arrived、取消并完整落脚，再从东沿自然下落至原GearPistonOne。最终冻结案276次观察／553次逐physics检查，真实两次施放／两次Arrived／两次Cancel，HP5→5、回血0、跳跃／冲刺／借力Boost均0。入树前仅设置一次受控起点，之后没有位置或速度写入；逐physics排除整个电区及活塞／升降台潜在占用，不选取恰好安全的机关相位。该夹具机关回调停止、居民不在场，不是占用战斗或正常主线证明。

另一个独立保存Player从原FrozenExit开始，保留原D离边→neutral落营操作：实际落到原营地(1899.9253,3959.969)，只有一次落地，715.969高度与原1点摔落伤害，HP5→4。随后同一角色从营地经真实W/K到原西墙、原AbyssSafeBottom，再A返回原营地，HP4→4、无治疗／额外能力，完整支撑及12个新鲜中立输入样本成立。这个角色与后面的齿轮侧棚访问角色是**两个独立夹具leg**，不拼成Frozen→Piston同一身份全程。

两重型敌人完整巡逻与HP保持、旧Gear两个实际身体基线、旧26敌人的36个主／备用出生身体占位、新棚对应的同五区小地图均复验。新棚不碰这36个身体，但与营地rest:0区域仍相交148×40；该区域是新居民禁占政策，不自动禁止地形。不能写“所有保护区无重叠”。新棚不碰实际checkpoint保护框，棚底至原营地地面净高210。

候选五区、可搬移六文件父包及内嵌地图现在为139实体，独立检查原5冰区＋2齿轮＋1蒸汽＋3钟摆＋1接落台共12个字面矩形。父包真实Ready／唯一存活Player与搬移加载通过；正式保存场景仍127，**没有覆盖正式关卡，正式新增0/50、全50 Placement=null、24正式子型Unimplemented**。

## 推理：保留的反例与修正依据

证据根为 `G:/knight/验证证据/october13-stable-claw`，下列时间均20261022；原失败保留，不与最后矩阵拼绿。

- 043152：真实resolver边界选中新top后仍未Arrived；不能把选点成功当通路成功。
- 043345：第二次Travel→Released/Obstructed，无已发布Latched。旧营地地面可能拒绝了拟议Latched身体运动，但没有同步命中身份，不能确称唯一根因。
- 044146：旧顶板中继真实Arrived，但第三次瞄准选中GearEntry，未通过整路。
- 044718：真实完整胶囊在(2000,3710)只有incomplete_support，先留此RED才新增实体。
- 044955、045229：额外测试要求“整个外接矩形先离开台边再Reel”失败；045229同步真实胶囊查询却clear=True。圆角胶囊与AABB不等价，因此改为保持真实同目标／合法状态，并以原生完整胶囊无重叠为Reel清角门；不排除新台碰撞，也不改产品物理。
- 045715：宽台的新侧棚通路通过，尚不能代表旧回营兼容。
- 050108：宽台(1840,3746,260,40)真实截住原Frozen落线，停1899.9253,3709.973、0落伤、原营地未达。没有放宽原一次落地／1落伤断言；将左缘收至1932，右缘2100、顶3746、厚40不变。
- 050242、050530：收窄后原Frozen直接落营和新侧棚通路通过，之后补营地往返深井及新鲜样本／共享camp90上限检查；最终资格仍只取下面同冻矩阵。

## 同冻结14门：12PASS／2严格FAIL

外层session91158已TERMINAL exit1，所有子案均终态、build exit0、timeout=false、guards=0、命名豁免为空。时间后附GUID前8作为唯一检索前缀，完整名称／argv／日志在上述证据根。耗时单位ms。

| phase | case时间／GUID前8 | build | native耗时／exit | 严格结果 |
|---|---|---:|---:|---|
| resident-gear-left-access | 050827-193-c77c73e2 | 23418 | 18955／0 | PASS |
| resident-gear-alcoves | 050915-292-6b7505e9 | 23017 | 9767／0 | PASS |
| resident-pendulum-alcove | 050953-719-8fbb7ae2 | 24631 | 20864／0 | PASS |
| resident-map-census | 051047-955-51c2ee88 | 29609 | 8227／-1073741795 | FAIL，原生崩溃 |
| resident-parent | 051134-304-86f8a4fe | 28572 | 3251／0 | PASS |
| resident-steam-alcove | 051213-923-7d742bc3 | 27887 | 11146／0 | PASS |
| resident-frozen-player | 051300-767-3e91fbff | 28939 | 5745／0 | PASS，Descent局部132夹具 |
| resident-allocation | 051343-522-006949fa | 28396 | 647／0 | PASS |
| resident-void-stream | 051420-335-11c8cc0c | 28295 | 2046／0 | FAIL，退出187ShapeRID |
| keyboard | 051457-546-bc759916 | 27360 | 9817／0 | PASS |
| actions | 051541-544-e2e8dd4 | 23540 | 7198／0 | PASS |
| canyon-structure-v2 | 051618-593-01cad9b8 | 24176 | 1362／0 | PASS，正式127 |
| prologue-pre | 051650-002-2b460941 | 24182 | 99982／0 | PASS |
| prologue-post | 051900-349-f5974816 | 23619 | 148126／0 | PASS |

post仍执行原75案例／0退役、8次同owner主场景生命周期1→1。三日志没有ERROR/FATAL；四条精确故意负测warning在stderr与external镜像，不算八个独立事件。上述pre/post、Frozen局部门也不冒充正常139 Main＋Explore验收。

联合人口检查stdout只有fixture139后，stderr记录 `gchandle.is_released()`／`mono_object_disposed_baseref:1788`／`GodotObject.Finalize`，C000001D。没有完成本版联合选择或完整JSONL，**当前139版不能报告48/2，也不沿用旧135版47/3为新调查人数**。首body日志在若干环境、旧体、Raw及查询操作之后才输出；没有body行不能识别坏对象或证明未开始查询。void虽然10行为通过且engine0，stdout仍有一条187ShapeRID错误，保持严格FAIL。当前干净pre/post不关闭历史metadata252、Handle／FATAL／音频问题。

## 风险与尚未证明的范围

- camp↔Abyss是局部连续物理兼容控制，未直接执行原完整BasinReturn的精确1820停点、flow／片段收集／保存／恢复门，receipt明确checkpoint_save_proved=false。实际返回营地1693.1677有完整支撑，但不是原精确停点认证。
- 新helper的bottom到达上限180之后另有8次neutral；原WalkTo把尾帧也纳入180。因此不能声称这条新helper完全保留原WalkTo整段180语义。实际到达98native，没有观察到超时；后续应加共享尾帧上限硬化，不据此伪称本轮产品路线失败或完整验收通过。
- 本轮没有最终美术、正常显示／完整新人口主探双路、真人试玩、渲染60FPS、占用战斗或发行包证明。保留完整1–11和原26＋新50＋Boss／骨骸10／幽灵10要求，不缩人数。
- 下一步先补最小的资源创建来源→Dispose ENTER/RETURN标量身份关联及严格诊断准入，再做一次有新证据目的的联合检查；不做原样抽绿、GC/cache/KeepAlive/drain、借用资源Dispose或引擎升级掩盖。当前只读审计没有证明具体所有权错误；身份方案还未实现。档案区剩余联合空间也未验收。

## 版本保管

同冻1707项源／资源、877 C#；source manifest SHA256 `5175E3B60C69D8ABEA245278CB23F75A3ACB82D985A23F281B688274E9FFC585`。共同9 helper manifest `42AC94BC9D42A40DD7A814FCD469B25FBB71A3AFD90B5B9716A948B560F2C3FF`，pre额外一原脚本的10项manifest `AF16475C4FA5EFCED2600096A0611BAEE8D16FECA9F8B9824B00AEE941673A68`。六binary manifest `5B6D94D1C4AD579F12DEB2DF668635D18C00ED7B4E59727EDD91921907A3B250`，Knight.dll `255030EAF25F8A473B69B2554F14EED46ACA66ABAB79F427D308C0F2E24B9AB3`。

独立保管复核14案70对清单、5008份before/after实际输入／脚本副本、84份二进制副本零差；当前1707源、10 helper、6 binary与实际hash-only用户109行／104文件也零差。用户manifest仍 `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`，未读取真实存档语义。当前commit仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index SHA仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C，与前轮检查点一致；不声称每案五对清单覆盖.git。

独立进程审计还核对28次build／engine PID、完整argv、合同、wait／cap及provenance，错配0；十四次构建均0错误／警告。实际case与原始三日志保留，不通过修改文本或忽略诊断合成结果。

所有owned native终态后才写状态文档。没有Git写入、付费、OS／前台输入、自动化、删除、资源寿命工作绕过或新媒体。手动容量检查current4838／baseline3508／growth1330字节，Notify=false，不重置基线；没有恢复已删除的G:/OurGameArchive。
