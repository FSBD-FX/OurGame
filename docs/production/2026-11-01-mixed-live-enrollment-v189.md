# v189 — 移除测试登记前置，实际K仍被当前预览挡住

2026-11-01，工程仍为G:/knight/Knight。原完整十一项目标ACTIVE，未正式完成。本轮仅两个现有测试CS改动；无生产玩法、地图、血量、能量、优先级或美术变化。仅无窗口构建/隔离测试，无GUI/OS输入/付费/额外崩溃采集/自动化/删除/Git写入/真实存档改动，误发视频未用。

## 事实、推理、风险

事实：批准的键盘规格§4规定J/K独立；稳定E06/E07/P01/Q01未要求“J先把本波Burst打至1血”。v121明确把post-Shot称为内部测试顺序。生产ThreadTarget只拒绝死目标，Ranged.ApplyThreadPull也无HP1前提。因此旧J→HP1登记是测试驾驶前置，不是玩家技能门。

最小修正：仅现有post-Shot opt-in可在目标活着时登记本波确切Burst，保留完整祖先Live、group ownership、actorRoot、前缀、精确场景及真实target/adapter校验。默认消费者仍等HP1。监听只订阅一次、无first-J也退订；登记不是施法。实际first-J使HP1另行观察并加入提前登记的终端要求，原own Shot、致命J、Defeated、同cast/target/application/Stunned/Idle、raw边沿和later ledger均保留。

K仍在该演员真正Shot之后，提前登记的流程还要求实际生产own preview的physics/sequence/authority/位姿/速度/瞄准/anchor一致。没调用第二解析器，没强行发K。K基线在真实first-J/stage0和实际K命令时采集，只更新玩家HP/恢复/jump/dash基线，不重置cast/edge/application/later账；pending或completed意外恢复明确拒绝。

风险：移除一个错误前提不代表已解决混合战斗。原生实测登记成功，但两次Main仍同处死亡，实际K均未发出；不可宣称玩家K失效、K已成功或Main修复。

## RED→GREEN与实际失败

新actions反例先跑旧HP1条件，27758ms build通过，native803ms严格FAIL于MIXED_LIVE_ENROLLMENT_RED（不是编译或资源故障）。允许opt-in正生命值、保持其他限制后11项布尔策略控制通过；它们不是实际居民/queued-parent的新负例，后两类本轮仅源码审查。最终同源actions也通过，明确actual_K_proved=false。

第一次Main（66CF版6dff61b4）真实于physics6321登记满血HP2的正确本波Burst，但没有K机会/命令/回执；仍触发原3120.29死亡恢复位移断言。

为避免猜修，仅再加有上限的被动记录：原post-ownShot、shortWindow、L关闭、Idle、CanGrapple边界最多64行，读既有缓存预览/标量，finally安全输出，不改准入/输入/等待，不查额外物理射线。第二次Main（最终AAD5版b43ef412）有61/64行，omitted0/fault0；全部own=false/admitted=false。same_pose=True46行、same_velocity=True45行，sequence/authority无错配，所以不能把未发K都归因移动过期。

代表性physics6467：玩家(4784.1255,3423.9336)静止朝左，实际选中16036938123997，TempleMetal，anchor(4723.7524,3427.9336)；希望作用的本波Burst target16037357554281，anchor(4630,3431)。两次Main都没有真实K，仍死亡恢复1/0；没有重跑筛绿或放松匹配。

## 目标与几何边界

当前选中坐标与本波RustShellSquire实际坐标相同；其保存ThreadTarget的Kind/TempleMetal/legacy adapter也匹配。**缺少选中ID→父路径的直接回执，故仍是强几何/源码推断，不冒认身份测量。TempleMetal不等于地形。**

解析器先做LOS过滤，再角度→距离近似平局→路径排序；近敌提升仅针对非legacy地形，不能用来解释两个敌人之间的选择。该静止样本中Burst偏左轴2.625°/154.287px，当前anchor3.791°/60.506px。如果两者均合格且可见，单凭距离优先解释不成立，不能据此改动用户要求保留的角度优先。

假定当前anchor是Rust，其保存radius18/height64胶囊会与玩家→Burst线在(4741.7524,3425.87634)相交，且解析器LOS考虑敌人碰撞。这使遮挡成为有依据的候选，**但未选中Burst的实际LOS没有测量**，其他资格或碰撞物未排除。

原生支撑矩形LiftBackLanding X[4600,4800]/top3460、玩家宽40，几何全支撑中心范围[4620,4780]；当前X4784.1255虽有接地，仍超出完整宽度核心约4.13px。候选中心X4680可全支撑、Rust在右、Burst在左；只是几何候选，穿过实体Rust、顶上Grip和窄站位均未实测可达，不作路线证明。下一应先核对合法近敌打断/站位办法，再取得新鲜本波Burst own preview；若加额外K，必须独立守恒记账，不能重置或冒用原本波K回执。

另已排除普通波Rust前甲假设：其HP3、Hurtbox直连Health，普通J伤害1；两个有效J后仍可剩1血。日志和受伤冲量支持两刀受伤，但本案没有逐刀HP原始输出，不能写成直接HP回执。frozen/后段Rust防守策略不属于这只前置普通Rust的受伤权限。

## 本轮实际运行

证据根G:/knight/验证证据/october13-stable-claw/，以下全部保留：

| 案例 | build/native ms | 严格结果 |
|---|---:|---|
| actions052805/4978126d | 27758/803 | 预期HP2合同RED，FAIL |
| actions053025/1d3ec6bf | 27828/8386 | 中间候选GREEN，非最终同源资格 |
| Main053237/6dff61b4 | 28251/129825 | FAIL，登记成功但未发K、死亡恢复 |
| Main054111/b43ef412 | 27087/125913 | 最终同源FAIL，61行均非own preview |
| actions054755/81c8069a | 28043/8404 | 最终同源PASS，11布尔控制及原动作 |
| models054838/3bd43a9f | 26266/531 | 最终同源PASS，仅爪模型子集 |
| pre054913/2a3a1085 | 28652/176969 | 最终同源严格PASS，原180000ms |
| post055246/36e2964f | 26254/208094 | 最终同源严格PASS，原300000ms |

8构建均0错误/0警告，8原生均实际headless、未超时，无fixed-FPS/movie或guard差异；整体5PASS/3FAIL，最终5案同源但Main仍FAIL。最终四项串行回归全部PASS，总exec39304返回0不覆盖此前Main失败。没有把旧中间GREEN拼成最终资格。

post原75enter/75return按0–74同名有序配对，owner8、nodes1→1，恰好原4限定WARNING（scope1/1/2），无ERROR/FATAL；非旧180秒或性能认证。pre本次0ERROR/WARNING只证明本次通过，不证明历史Handle资源根因已修复。

只读时间前沿核查：v188超时前进入Traversal已比v187晚11213ms，而共同遍历段19594/19611/19613ms几乎相同。主线监听是DefeatTask10Encounter内局部代码，不能在pre调用链执行；不能归咎它或BellChain。延迟前沿在Laborer negative setup之后、首Supply之前，缺wall时间边界，不能指定Load/GC根因；未改变180秒上限。

## 审查、保管与决定

唯一新鲜源码审查C0/I0/M0，同一个席位完成最后被动记录差异，无二审/修复轮。源码审查允许保留增量，不批准完整分支、Main或原十一项；实际K/整关/资源/正常视听/硬件真人均未由局部通过替代。

8案40对manifest、3906冻结source实体、84helper实体、48binary逐SHA相同，指每案内部前后保管；早期3案为各自版本，**最终5案**才完全同源。05:59:18 +08当前1871源/6bin/最终post10helpers绑定，16owned身份均按创建时间/路径核实结束。

- final source manifest E6498BA242D3E91D54F39B6D047C83A69AF8D4168F4771D8FBC5E5E4E1F4A36F
- Knight.dll 0B041AF4AE41C3EA028A9D56B9908028D4163C46160B94ECC3562FB7FE9BC144
- Main AAD5A0C58EDCC25487A9536FAA35A0FFCF33DC540042B110C3A36C33F77DA4D2
- KeyboardActions 0D3CE60767947FAD64D806BE76371750B8BD14498B63E65F20F41D939FBB1A59
- canonical5F683E64与Supply0BDE3E35未改，HEAD/index未动。

手动媒体检查2026-10-31T22:00:18.5810012Z：7427750bytes/86files、Notify=false、基线3508未重设；无新媒体/自动化。

本轮决定及成本：沿既有授权修正内部登记顺序而不另询偏好，但保留全部正向证据，不能把它当已玩通；保留唯一脏工程、不新建worktree/提交/删除helper，回退依赖副本；全部原生串行并冻结源/helper，增加等待成本；首Main未发K后仅加一次被动取证而非猜改控制，增加一次运行成本；审查未覆盖项继续OPEN，仍需未来实测，无新增延期Minor。

原完整十一项目标继续ACTIVE。当前不是试玩交付：Main/K未完成，历史资源根因未解，正式美术/动作/UI/音频、正常渲染/目标硬件/真人认可均仍有未验收事项。
