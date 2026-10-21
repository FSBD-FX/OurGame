# 推进模块保存与正式接入 v009

2026-10-18；完整1–11工具目标仍ACTIVE。短喷计划Task5按brief专项合同完成；不是整计划、正常整关或最终视听完成。Task6–7随后，五历史严格FAIL等仍OPEN。

## 本轮实际实现

Motor现在导出InitialMobilityModule，Ready验证并冻结八项值；旧无模块场景兼容Standard，但正式Player已经保存显式标准Resource，不再靠该fallback。运行中修改作者Resource不能热改快照。此前v008的真实站稳装配事务继续使用同一pulse配置epoch，无新增拾取、背包、模块UI或schema4字段。

BuildPlayer已改为安全候选入口：必须且只能指定一个绝对、已存在空目录的 --player-output-root；固定Player.tscn，拒绝项目/真实userdata祖先重叠、文件系统根、重解析路径、已有输出、未知/重复/缺失参数，不再无参覆盖正式角色。可选 --player-module-resource 读取实际作者tres并复制成自有嵌入Resource，校验在创建图和写入之前；不是CLI逐项调参或测试专用模块ID。实际源Player的Tuning被读出/复制，非默认GroundDeceleration/DashSpeed/DashSeconds报告来源和字段、要求显式映射；通用纯模型转发与合法Gravity覆盖保留。旧无参工具消费者需在Task7迁移，不能据此称整个工具链通过。

真实构建器保存/默认cache读回35节点及原过滤器/优先级/visual结构；标准和sentinel分别生成，producer均终态后逐字节复制到另一目录，再由新进程默认加载。模块ResourcePath分别归属搬移后的scene::，未注入模块；实际自动Shift首步93.333336与72.01667符合独立5600/60和4321/60。Root只采用标准模块。

## 保存默认值的实际缺口与修复

最初保存的standard子资源仅有script行，依赖未来脚本默认值；并不满足八字段显式持久化。174333-fa2966ba304c41bea746388aaad5619e严格RED在standard/ModuleId expected1 actual0。引擎文本saver会跳过等于脚本默认值的资源字段，且默认值查询不经过Inspector revert。[当前引擎文本saver](https://github.com/godotengine/godot/blob/a13da4feb/scene/resources/resource_format_text.cpp#L1857)、[默认值查询](https://github.com/godotengine/godot/blob/a13da4feb/scene/property_utils.cpp#L138)。

改为官方动态native property storage：八个typed C#属性仍唯一参数权威；property list只存类型/usage，无第二份参数字典。没有伪造非法0默认值或revert，不手写tscn/改引擎/忽略cache。Tool Inspector可见、可恢复有效Standard，native setter/duplicate与C#属性同源。174446-b1ca93d77d924eaa9dca6ec23d5bfbf2首次全字段保存GREEN；最终同版再次验证两模块全部八字段各一处实际序列化。

## 真实资格覆盖

v008基础矩阵继续过：两不同完成physics tick（不是请求次数）、相对支撑切向速率严格<1、world/support约120的移动台站稳、空气/跑动/刚停第一步/燃烧/暂停与控制禁用/死亡/queued owner与support/完整速度替换/null与七字段NaN拒绝、六采样前后Space/Shift窗口。成功原子换快照/epoch，不改位置、速度、HP、预算或落地证据。

新增自动W/K/Space真实抓爪流程观察Aim/Windup/Travel/Latched/Reel/Swing/Boost七阶段，以及pin/unpin合法引用交接窗口。每次真实拒绝原因均ClawActive，立即核对pulse、pose/velocity/HP/landing、claw frame/authority/target/residency不变；随后自然到Swing并真实Space一次释放。没反射伪造不可达状态，没有在physics回调内注入按键。此为覆盖补强，首个运行前的KeyboardPreview所属类编译错误不是产品RED。

## 候选冻结十二门

source1689；源清单共同SHA256 C1E4A31D480B8F3A685765418A83A6B78FCEDE35910E0220FCBCAFE078B0B40F。全部fresh Rebuild0警告0错误、ExpectedFailure空、guards=[]；source/helper/user/binary四清单每门前后相同，三sink与终态已核对。4920 batch终态0后才新增正式接入测试/采用场景。不是十二项对应用户1–11已经完成。

| phase | exit/ms | case |
|---|---|---|
| thruster-module | 0/554 | thruster-module-20261018-175101-837-00436c6706234ef49602e525ddee61b0 |
| thruster-authoring-contract | 0/667 | thruster-authoring-contract-20261018-175137-980-d01c8c83ce574fe192f328ab4f0f1609 |
| thruster-initial-module | 0/1094 | thruster-initial-module-20261018-175211-789-10f61570cc8844a4a7dd6193b96769d9 |
| thruster-equip | 0/1227 | thruster-equip-20261018-175244-047-9b95d58932f0474097164350375b832c |
| thruster-equip-support | 0/1491 | thruster-equip-support-20261018-175319-093-97934aec2e054ba2ab0954e23d9cf0cb |
| thruster-equip-guards | 0/3237 | thruster-equip-guards-20261018-175352-120-0d14bb9674f442af98fd68df40e34647 |
| thruster-equip-claw | 0/2041 | thruster-equip-claw-20261018-175426-716-d7b13983b0c942fd82b9362d1c16d291 |
| thruster-saved-module | 0/962 | thruster-saved-module-20261018-175504-977-64d124eb538f43fd9083e2f7766295fb |
| thruster-player | 0/22784 | thruster-player-20261018-175546-212-3027c3fcf8c843f180ca62a429a5b334 |
| thruster-traversal | 0/7519 | thruster-traversal-20261018-175639-848-446dca11d6d0450e91fcb1f1693045e3 |
| thruster-impact-reentry | 0/1398 | thruster-impact-reentry-20261018-175722-575-cb66e57531c04476a405907d3fa28564 |
| keyboard | 0/9810 | keyboard-20261018-175758-127-eb38735f0a0a4f24b262af97b84a2fed |

## 正式采用与五门重验

正式缺字段RED：thruster-formal-module-20261018-175923-381-dc8c3a305e0643d0ad5ea64adfeb9427。确认无Godot consumer，核对producer及所有scripts/tools与旧正式作者输入哈希未变，保留旧正式文件，然后机械复制真实builder生成的标准候选原字节；未手改tscn，未采用sentinel。

- 旧正式SHA256：B1E9DFCB88020DFB7CBD18779CAE9EF04058C13899912D437386E05D30C26BBC
- 新正式/候选SHA256：2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5
- 可恢复备份：[Player-before-standard-module.tscn](<E:/OurGame/Knight/.evidence/october13-stable-claw/player-module-adoption-v001-20261018-180035-407-62fe728fae7e457c81c040bfb0fb7fa2/Player-before-standard-module.tscn>)
- 同目录adoption.json保存producer、双SHA与精确目标。首次PowerShell journal序列化用了无$的true，发生在复制之后；只读核对双SHA恢复journal，没有盲目重复复制/回滚。
- 原正式及候选均无header UID，正式父/Prototype按路径引用；未手改UID或父资源。

正式采用后source1689，源清单共同SHA256 C27653DBA31EA7FA34BE1FA397F41D2034B955921A40B58A913B652F7DE5D66A；39148 batch终态0，fresh0/0、四清单相同、guards=[]、三sink无诊断。正式Resource归属res://scenes/player/Player.tscn::Resource_q05wf，未注入，标准实际推力及热改冻结通过。

| phase | exit/ms | case |
|---|---|---|
| thruster-formal-module | 0/1034 | thruster-formal-module-20261018-180103-026-50c8713bc4cc422bbcf1034ac3fa684b |
| thruster-player | 0/22827 | thruster-player-20261018-180140-689-aad29b7f84974077bd2d76d414b0442b |
| thruster-traversal | 0/7519 | thruster-traversal-20261018-180234-316-0f7fede051324ccf89f7c46cb8042ae3 |
| thruster-impact-reentry | 0/1384 | thruster-impact-reentry-20261018-180313-535-f72fd996e4404f5788ab2585d9d1a0ef |
| keyboard | 0/9727 | keyboard-20261018-180347-408-38cd145bb2f042bf99e7fdb5d9834e61 |

证据根：E:/OurGame/Knight/.evidence/october13-stable-claw/<case>/。17门属于候选版12＋正式采用版5，不能说同一源版本17门。旧keyboard已有wrapper finalizer观察1及drain原样保留，新thruster阶段未新增GC/cache/drain/时限或日志豁免。

## 失败记录与完整目标边界

新增测试一次Array using编译失败使最初batch四门未进入engine；root核实精确owned batch PID14152/parent48212后仅停止后续调度，让当前子owner自然终态，保留四build FAIL（174615-506afe、174643-b5fba9、174711-7597c7、174740-ded14f），不算行为RED。修正后module 174918-2c25799258c94fbd9ea0d85b8209824b又因测试boxed Variant对象等值而失败（文字值相同）；改成typed string/float语义比较，未改产品，最终native8字段/revert/duplicate通过。

当前正式模块版没有重跑完整pre/post或三历史资源FAIL；Upper/profile/Bell/Recovery、七Handle观察和五历史严格FAIL继续OPEN，不以局部安静退出关闭。地图仍正式127/候选132，新50正式0/50、24canonical/全部正式资源/正常Main与Explore/旧档/真人/显示性能/录像/ZIP/GitHub待完成。Task6 typed开始/结束、短余速/制动、喷焰/声音/UI还未制作；旧IsDashing展示不是这些新能力。没有新美术/音频/展示媒体/付费/Git发布/自动化。

