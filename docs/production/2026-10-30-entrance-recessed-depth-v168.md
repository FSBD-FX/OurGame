# v168 — 入口岩土与埋藏机械纵深候选

## 实际修改

生产绘制已接入真实入口`VisualRoot/Midground/CanyonLandmark`的`DrawEntranceNaturalApproach`，不是未调用的旧Cavern/Foundations。保留四片原staging背景，再加页岩凹面/纵断层、埋藏长肋及暗铜切面、折断导管及切面、沉积岩体、低alpha渐隐光面。12个静态polygon，无新粒子/碰撞节点/逐帧处理，无付费位图。

源：`scripts/presentation/CanyonBackdropVisual.EntranceDepth.cs`与当前`.Entrance.cs`。本地基准1280×720，按SectorBounds.Size缩放，现有_Draw再应用Position一次；Z仍-20，前景真实落脚带和五块实体岩壁未改。只对入口/staging/sector角色采用新计划；其他区域、Coverage、Distant和Challenge保持原路径。

这是原十一项V07/V08/R02的实作增量，仍是原生矢量候选，不称正式手绘/Ori品质、完成全区美术或用户已认可。

## 事实与严格结果

原件根：`G:/knight/验证证据/october13-stable-claw/`。

| 案例 | build / native ms | 结果 |
|---|---:|---|
| entrance-rock-roles-20261030-060533-848-7d1c3d57ba53446eb236429fe2e7ac79 | 38940 / 1409 | 缺实际新计划断言RED；raw另有195Shape2D RID退出错误，未忽略 |
| entrance-rock-roles-20261030-060842-514-16a5577fb9db4fa3b0d804b4bf090f36 | 27194 / 1284 | native exit0/新结构checks PASS，但旧材质加载及终结器有3条Handle-notinitialized ERROR，整案严格FAIL |

第二案两实际builder/packed消费者均有12层。独立L洞腔包含/跨凹角反例、NaN/重复/零面积/自交等13字面几何控制、三角面积、颜色/alpha、y608..636落脚保护带、层序、只读集合、缩放及非入口角色隔离已通过。真实保存叶挂树收到2次native Draw请求，无手调_Draw/伪造Draw信号；静态碰撞/抓面/ReadableEdge所选数据指纹未变。

Draw信号发生在_Draw之前，故日志明确`command_submission_observed=false`、`draw_request_only=true`、`pixels=false`；不能拿该计数证明命令实际提交或雾后混合质量。实际消费的代码路径由独立审查核对。未获新显示/采集许可，本轮无新截图或影片，不以合同冒充玩家看到。

两案10前后清单对/902份选定源实体/20helper/12封存bin全SHA一致、无未列或缺失实体，4个owned进程已终结。失败原件保留。当前1863源（新增一个生产CS和一个测试CS）。

## 推理与风险

新增几何和实际入口数据已落地，但专项有未解释的资源句柄错误，不能称专项GREEN或资源已修。错误栈发生在旧MaterialSkin加载与终结器路径，不据此证明与新代码完全无关，也不换引擎/CacheMode/GC策略凑通过。不做无改动反复重跑筛绿。

模型已PASS；pre061157在BellChainMechanism路线sample0.5返回179838ms、sample0.8进入后触原180000ms上限，严格TIMEOUT FAIL，不扩大cap、不把尚未运行尾段算完成。post PASS，见下表。正常游戏画面、雾/HUD混合后可读性、美感和用户确认仍OPEN；隔离标题许可不等于允许开局/录屏/付费。

完整Main/Explore失败及历史资源FATAL继续OPEN；目标原十一项未缩减或完成。无键位/数值/地图/存档规则改动，无付费/新前台/OS输入/额外崩溃采集/自动化/Git/资料删除。

计划与裁决：`docs/superpowers/plans/2026-10-30-entrance-recessed-depth.md`、`.superpowers/sdd/2026-10-30-entrance-recessed-depth/progress.md`。

唯一fresh代码审查C0/I0/M0，确认当前_Draw逐项Submit到DrawPolygon且旧分支保留；本次不二审，不因审查无问题而重跑同一失败案筛绿。未认证正常雾/HUD混合、假平台/锚点最终误读、真人审美、双路或资源根因。

## 完整回归与当次显示请求

| 案例（同证据根） | build ms | native ms | 严格裁决 |
|---|---:|---:|---|
| models-20261030-061122-852-4790bc54f9394c96abb3b99583a68155 | 26835 | 537 | PASS |
| prologue-pre-20261030-061157-861-2db28cd601d94a67bf8d0cfef6f6e76d | 32306 | 180094 | TIMEOUT FAIL，原cap180000 |
| prologue-post-20261030-061540-020-6ebc1dc4dcc04916b800bec5c0691a2c | 31782 | 226721 | PASS，既有cap300000／原180Accepted=false |

post原75enter＋75return索引0–74唯一、owner8/nodes1→1通过；保留4限定WARNING（observer1/profile1/Completed→Recovering2），不是原始日志全静。普通focused/models/pre/post最终2PASS/2FAIL；不能称GREEN套件。

用户当次选择“可以，再打开标题页”，root于完整回归结束后运行既有`show-isolated-title-v001.ps1 -Show -DisplayConfirmed`。fresh当前版预检案`keyboard-20261030-062131-568-74d21a34a0564880a5bb20338cb679a3`：build27604/native19485，native exit0、两个Mount Ready/quit1及Title合同均在，但原始日志有5条Handle-notinitialized ERROR，严格FAIL。堆栈在第二个title Mount的Root.AddChild，不能无证据定为新背景所致或Ready时序回退。

启动器因此拒绝窗口；没有进入独立title-entry次引擎，也没有创建live进程/READY或新录屏。用户并没有看到本轮新美术；不重跑相同失败预检筛绿，不绕过门槛。权限仅标题显示，仍无开局/系统输入/采集或付费授权。

全6案例6次构建成功，6次native严格2PASS/4FAIL（含缺功能RED、资源专项FAIL、pre超时、显示预检FAIL）；一次foreach外壳exit0不能盖过单案FAIL。显示预检没有启动第二引擎，不补造次引擎收据。原始失败和来源全部保留。

## 下步依据

旧PASS pre178764ms与本轮超时比较：到Traversal enter之前已多6776ms；共同Traversal部分只多29ms，16次audio drain合计还少165ms。当前证据不支持把超时归因新12polygon或删路线样本。最早有绝对时钟的Descent BaselineStart前已累积4561ms，但此前各测试缺耗时，不能猜是哪一个。下一从已有前缀测试边界的最小被动耗时观察定位，保留原180秒、全部样本、独立出生与终端负控；不更改缓存/GC/引擎凑通过。

最终保管：6案例30组清单对/2816选定源实体/68helper/36封存bin全SHA一致、未列实体0；12个自有build/native PID终结。当前1863源、6bin、失败keyboard案11项helper一致，只证明字节绑定，不表示显示资格通过。HEAD/index基线不变，无Git写入。未知8760/bridges未触碰。

正常汇报前手工容量Check于2026-10-29T22:26:54.5684161Z通过：媒体7427750bytes/86files、Notify=false，无新图片/实录或自动化。
