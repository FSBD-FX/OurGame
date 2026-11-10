# v100 · 获批半钟刀正式接入与真实刀身观察器

2026-10-24。KNIGHT仍在 `G:/knight/Knight`，完整原十一项目标ACTIVE。本轮推进了实际角色资源，而非只增加诊断；但没有完成整关、全部美术或视觉验收。用户“没有看到窗口”仍有效，全程无前台游戏窗口/系统键鼠输入、付费生成、自动化、Git写入、打包发布或删除。

## 事实、推理、风险

- 事实：获批半钟刀原PNG已进入正常保存Player的Weapon层，替换原程序线条。36组状态/进度/左右朝向几何检查及原三项机械爪表现通过；独立审查发现的旧武器观察器也经真实反例RED→GREEN。最终普通pre通过，post却134401ms非超时FATAL、原75尚未进入，models恢复普通构建。
- 推理：旧NearArm手圆心为formal局部(13,19)，旧武器圆心(16,7)，垂直差12、欧氏差约12.369。两层独立旋转，固定移动Weapon节点不能保证动作中握持；应保持十slot/原typed旋转，仅让真实纹理的主握点随实际手部更新。旧线段端点不能继续代表新纹理边界。
- 风险：主手几何贴合不证明第二只手握持、手指遮挡、相机实际可见、正常速度动作自然或用户观感。历史资源句柄故障未定位修复；新刀不能作为其原因或修复的证据。完整双路、人口实战、正式视听/相机、硬件真人及发行继续OPEN。

上一目标轮为PROGRESS：实际预算消费及Ready诊断改变了下一定位动作。本轮没有重复安静Ready实验；并行只读所有权核验与获批单件接入，原十一项稳定ID、76普通敌人＋Boss/NPC及机关范围不缩减。

## 正式资源接入

来源与批准链沿用[v096逐项核验](2026-10-24-profile-use-serial-and-asset-provenance-v096.md)。原件：`G:/knight/美术源文件/迁移留存/2026-10-24_complete-layered-001_未发布/layers/half-bell-cleaver.png`；运行时：`assets/player/thread-spool-bearer/half-bell-cleaver.png`，SHA256均 `06E657BB4E3831473688E5CFE7B6BB4FF2FD2A80D77C4EE17514D4B2325C325A`。只复制认证单件，没有修改像素、生成新图、装入两个握持遮挡片或批准链未闭合的身体/披风，也未恢复被拒总装。

- 母版1402×1122，独立读取非零alpha边界(504,756,705,215)，86704像素。靠刃的原右握点(835,798)对应当前正X NearArm；柄尾(548,798)不冒充近手。
- `PlayerFormalLayer.WeaponTexture`为实际资源接入口，Draw使用 `DrawTextureRectRegion`；保留原PNG画布，仅在运行时排除透明空白。比例统一25/287，来自当前双手水平间距25与母版握点间距287，刀身约61.4×18.7世界单位。这是工程接入初值，不是用户批准的游戏内尺寸。
- `PlayerFormalVisual.ApplyPose`在全部层位姿完成后，以 `weapon.ToLocal(nearArm.ToGlobal(0,21))`更新Draw原点；不重挂节点、不双重翻转、不改碰撞/伤害/物理、十slot和原CurrentPose/武器旋转所有权保留。
- 原正常Player组件夹具先真实RED：202327缺绑定，build成功后native1/747ms。实现后202731真实纹理/哈希/alpha边界、36组几何及原三机械爪案例通过。36组中的非None帧均为Burst；.1/.5/.9是进度，不是完整前摇/命中/收招覆盖。

## 一次审查、一次Important修正

fresh只读review C0/I1/M1，无复审。Important：`ProloguePresentation.ObservePlayerMotion`仍用旧(-2,0)/(45,-6)端点及Weapon节点位置判断刀的边界/位移，会误收只改变Draw原点的裁切，也可能因旧端点误拒合法画面。这是验收消费者问题，不是当前实际相机裁切的证明。

先把原边界/位置读取原样提取为实际消费者调用的测试接缝；新原生203920保持原节点矩阵及旧脚/头/线段端点都在safe范围，只把真实Draw原点左移60，实际得到 `rejected=False motion=False`，native1/615ms。随后一次fix：边界读取真实DrawRect四个世界角点，仍保留脚与朝向；新增bitmap原点的父级局部motion样本，仍保留所有原slot样本及原32px/.95rad/.25限制，不把整个人物位移当动画抖动。最终204104反例拒绝/位移捕获、四角精确映射、36几何与原三案例严格通过。canonical精确要求两个完成收据，输入范围纳入源PNG/import及真实观察器。

Minor M1延期：补全前摇/Contact/Recovery/释放转场和非零瞄准的独立握持样本；当前36收据不声称全阶段。当前真实机械爪原三消费者虽执行，不把它们的旧位姿断言自动当新位图所有阶段的握持断言。没有在Important修正中顺手改这一Minor。

### 审查Declined逐项root裁决及代价

1. W读取/旧preview未动，没有新reader边界；不关闭操作排序问题，误推会让正常路线仍失败。
2. 敌人实体遮挡/receiver权限未动，没有新first-hit；不改爪优先级，代价是原遮挡工作继续。
3. Intro→Playing生产拒绝未动，组件夹具不是准备认证；代价是正常启动/恢复仍须各自实测。
4. 回调/延迟退出没有新同actor/parent/pose/sequence证据；不以静态36帧代替，生命周期义务仍在。
5. LoadLevel及主/清理异常未改，新fixture finally只保自有Player；继承故障注入债务继续，不能外推全失败路径。
6. native所有权/Variant/GCHandle/缓存因果未证，最终post仍FATAL；安静专项不关资源故障，代价是稳定交付仍受阻。
7. review时全回归待执行，root以实际终态裁定：最终pre/models通过、post在原75前FATAL；不能拼中间完整75通过，代价是原整关测试仍无资格。
8. 像素/正常速度/双手/正式视听/相机/硬件真人/发行全部未新验；用户NOTSEEN，不复用旧显示授权，代价是不能交付最终观感。
9. review未审全部1844源或真实存档；另行保管核验，用户save仅原清单保管不作语义读取，不能据此称存档兼容通过。
10. 观察器分配/I/O/完整清理异常全集未重证；新增观察有成本，现无全局时序安全声明，后续正常速度预算仍可能否决。
11. 审查未找到v099测试原件，不冒充其已对旧测试做全diff。root另找183553 frozen before、SHA `7EDAA86FB0F22BAE52759FE1E98E4D37802F4CF8B6E50EC297F96237E20BC951`，与v099 manifest逐字节一致；实际diff为117行新增、无旧行删除。成本是审查与root旧版核验覆盖应分别陈述，不称第二次review。
12. 审查只独立核源字节，历史批准链沿用v096而非全套重审；只接其可证单件，不把未核身体或总装搬入，成本是完整角色链仍待闭合。

## 资源寿命只读核验

当前 `ResidentSubtypeProfile.ConfigureActor`把纯 `ResidentCombatProfile`数据传给actor，并非把Resource交给actor保活；真正字段在ResidentBinding。manager的 `_originalSources`是几何路径PackedScene缓存，spawn另按路径Load。同路径并不自动证明同一C#包装器。可证伪下一假设是实例释放后，缓存仍持native子资源而托管包装器进入finalizer，再取场景时出现桥接失效；必须把具体失败serial/已记录native来源/Dispose(false)与失败前后同路径生命周期连上才能支持。v099的437只在原drain后Dispose(true)，不满足此条件。

本轮未新增来源读取/GC/常驻pin、生产cache模式或引擎升级，也未把普通FATAL的GodotObject栈猜为某种Profile。资源问题不能靠重复Ready安静对照宣布修复。

## 导入及所有原生终态

一次普通headless editor import终态0，新增PNG/import/ctex/md5四项及64个此前缺失的脚本/Shader UID元数据，源清单1776→1844/C#仍892。UID保留，不清理为“多余”。导入还重序列化了 `default_bus_layout.tres`；root核实原v099 SHA与HEAD的LF字节完全一致后，仅用apply_patch恢复本次已知导入差异，最终仍 `CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A`。没有恢复未知用户编辑或Git checkout。

证据根 `G:/knight/验证证据/october13-stable-claw`。目录完整，严格结果不以native0代替；同一行内before/after对，不跨不同冻结版本拼资格。

| 目录 | build exit/ms | native exit/ms | 严格结果 |
| --- | --- | --- | --- |
| thruster-formal-claw-phases-20261024-202327-215-00b010c221ef4cd68828e087bb971850 | 0/27459 | 1/747 | RED，保存Player未绑定获批图 |
| thruster-formal-claw-phases-20261024-202552-031-5c8e4034403c45f5bc9518cb79800e5a | 未启动 | 未启动 | 前置guard FAIL：要求1780行，实际重新枚举1844；该案无原始census/before配对，不能补造 |
| thruster-formal-claw-phases-20261024-202731-232-3e9c13fbd6204557a81a0de7cc778742 | 0/30029 | 0/4607 | 初始绑定36＋原3 PASS，临时重序列化音频 |
| prologue-post-20261024-203211-303-1797770fe694429ca0ea1381a876b1b5 | 0/30591 | 0/203184 | 中间全post PASS，原75及后置；非最终音频/观察器冻结 |
| thruster-formal-claw-phases-20261024-203920-222-2f168216d4c84ad1ad593648277a4fbd | 0/25011 | 1/615 | 真实观察器RED，两个误收条件均false；音频已原字节 |
| thruster-formal-claw-phases-20261024-204104-425-e7d4e0563cdc49c0920a0ff7e96b4844 | 0/30884 | 0/5421 | 最终握持36＋observer反例＋原3 PASS |
| prologue-pre-20261024-204218-343-69805d7617f54321be1fe0a4eb84a7b8 | 0/23731 | 0/156952 | 最终普通pre PASS |
| prologue-post-20261024-204545-689-abee46fe7dde45b794d0722cf6e77ba7 | 0/32388 | -1073741795/134401 | 最终普通post FAIL，非超时FATAL，原75 enter/return 0/0 |
| models-20261024-204903-974-af0207faf53f46229350405918b31a05 | 0/26216 | 0/526 | 最终普通models PASS |

共9次canonical尝试，8次实际原生，5严格PASS/3原生FAIL，另1前置失败；8次build全部0，无native超时。主要ERROR3＝两真实assert＋最终FATAL1；WARNING4仅中间完整post且原限定scope覆盖，Handle/ShapeRID0。最终post未进入原75/其scope，最后 `TASK18_ROUTE_RESULT_ARTIFACT_CONTRACT_OK`；FATAL只显示GodotObject.Dispose(bool)/Finalize，不能据此指定Resource类型或沿用旧ProfileMissing。中间PASS不能覆盖最终FAIL。

最终纯合同157/96/26/33/11/24/24/11/64全exit0，均不启动native。原75注册案例没有增删；新检查由既有Setup调用。模型通过也不是整关、像素或真人资格。

## 保管、媒体与当前下一步

8原生40对清单/48个OWN封存bin案内匹配；前置失败案只有after内237份单侧副本，无before/native/bin，不列入配对成功数。最终源1844＝runtime1546＋imported298/C#892；相比v099新增68项＝64 UID＋4资源项，原文件只4个C#发生有意变化，原音频恢复精确旧字节。最终models普通无REF构建，所有root原生进程终态后才更新状态文档。

| 范围 | SHA256 |
| --- | --- |
| 最终source manifest | 7D4C001F63CEC640C25092EE3BE87F79BCD23098E35170BE5A31BE533B2B2BA6 |
| models helpers9 | 98124AB3FB8BE90A2DEC3ABBF9B871BEE0995BE073DD26D87C4C2B94896BC7E5 |
| 最终post helpers10 | 3532E84386F19FB7350E36E722015D6F8A93761E80A7908B449892A12C303A3A |
| canonical helper | 224A8035C12ED449A5F76B421D4F285F055667344A9A326F9C777E8130D00501 |
| bin manifest | 67BF4C9896D13BFC8B393D4611D7AC1C038E2372AF9B93DB5FEAC4946AA82711 |
| Knight.dll | F638567B844840F8DEEEBC488F8A797169BD698E88BAA5A916DA307253FFB86F |
| Git index | 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C |

branch `codex/task6-certified-final-20260902T0555` / HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`未变。媒体仅一次正常工作内检查2026-10-24T12:51:35.3706028Z，Current860284/Base3508/Growth856776 bytes，Notify=false；本轮未显示新画面/录制视频或生成新媒体，不重建自动化、不以20GiB为停工条件。

文档写回后root再次逐项rehash1844源与最终post的10个helpers，漂移0；无root拥有的Godot/dotnet进程。`git diff --check` exit0，169条既有LF→CRLF提示、其他消息0；没有修改任何旧当案回执或在owner存活时改源/帮助器/文档。

下一步仍沿原十一项推进：资源寿命的实际因果复现/正常全回归、齿轮战斗与冷却/完整双路；武器补真实全阶段/瞄准/手指遮挡和正常速度画面，身体/披风批准链、敌人场景正式美术、UI/音频与真人硬件交付继续保留。没有把本轮单件或测试通过当完整、高质量序章交付。
