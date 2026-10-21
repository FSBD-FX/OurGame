# 正常版本准入与峡谷惯性落台 · 原生 v017

2026-10-18晚至2026-10-19凌晨，Asia/Shanghai。完整用户1–11工具goal ACTIVE，未缩减、未完成。Task1–5专项完成状态不重做；Task6音频退出／正常视听与Task7全量消费者／正常双路线仍OPEN。本次改的是诊断及自动试玩路线，不是增加玩家技能、修改地形或完成最终美术。

## 事实、推理、风险

事实：正常launcher仍钉1675行，但实际源码／资源／导入清单已1695行（含296 imported）。迁移准入后，普通Windows Main／Explore均完成入口和前两个实际遭遇、各遭遇三次命中且HP5→5；随后分别卡在深井前错误高度、档案支路错误落台，尚未到远程区域或Boss。两个正常案与后续窄叶均原生保留失败，未命名豁免。

推理：Main松A后等待23个physics，实际仍有−140.80109横速，旧等待只认named floor接触，接受了LowerCatch左缘而非完整身体。随后掉进Archive，最终卡在SupplyRoof左壁，是错高度的后果。Explore静态release-X合格不保证指数空气阻力下的落点合格。应先迁移测试控制策略，不能由这些失败推导地图断路、机械爪拉力不足，或反改获批惯性。

风险：局部停靠修正不证明整关通关、真人上手或显示60FPS。两正常案Godot采样focus_loss_count=0；但OS ready／finally两个端点均报告foreground PID34616而非引擎PID，不能据此证明全程OS前台，也不能据此归因用户切窗。未改真实用户档语义；保护109项保持。

## 正常版本绑定：真实旧拒绝与保留门

准入迁移时对实际1675参考清单核对：20新增／0删除／42变化，不是仅改一个数。当前18条短喷／着陆／实际Motion／typed presentation依赖进入normal选中副本。原六份资格helper／参考byte pins、canonical原始与键盘参考、实际109项保护、SDK junction与dotnet哈希、前后NoConsumers门及四个正常资格函数保持。

新纯合同执行真实owner AST的pre-case前缀及实际GetCheckedSource／SDK定义，不启动build、engine或创建正常case。动态AST第一次尝试因PSScriptRoot上下文为空失败，属夹具错误，不冒称行为RED；只修文件上下文后，旧owner在1695实际触发1675拒绝。

- 真正RED：normal-version-binding-red-20261018-234505-539-4af4934ab2fd4bf284d5bc9cf2d9a1d5。
- 首GREEN：normal-version-binding-green-20261018-234553-947-aeb906ab538b46ed92d8e8a47d5647a9。
- 最终当前源码合同：normal-version-binding-descent-final-20261019-000933-517-16c7a25d573a46d4ab5121eab976e639，8例通过：两个phase接纳1695、拒绝1675／1694／1696；18实际依赖与SDK／原pins保持。其UTF8-LF manifest SHA为E079B1EB7F54A2462452E0299B9090A486B22BC2EE54D0FBF4F74F5BF01D410B，不与owner不同编码的inventory文件SHA混报。
- 同次原15例资格合同命令漏Launcher参数，原件保留，属调用错误而非行为失败；补齐原参数后的normal-qualification-descent-final-20261019-001048-819-e39ed9d3e559486da5f30feffc6081b4，15例全过。没有修改其原函数／pins或用纯合同冒称正常路线完成。

## 普通Windows双路线：本次均严格FAIL

证据根：E:/OurGame/Knight/.evidence/october13-stable-claw。以下后缀配各phase构成完整case名。

| phase／case后缀 | engine／ms | 首个失败 |
| --- | --- | --- |
| ranged-route-main／20261018-235214-365-ff0c9b061aa74bf2bf6dac41927d7c77 | 1／21904 | walk2140最终(1699.9983,1383.9995)，已掉到ArchiveShelf并碰SupplyRoof，不在主路高度 |
| ranged-route-exploration／20261018-235423-009-880be7c83add45cba766ba6d89752f1d | 1／24912 | Shelf→Floor全neutral落到FrozenLeftCatch(1330.6957,2143.9329)，原fall120耗尽 |

实际native命令Windows／windowed／1280×720，无headless／fixed-fps／movie／verbose。均fresh Rebuild0警告0错误、无timeout、guards=[]；每案只有一个独立ERROR，stderr与external为同一失败镜像，无额外WARNING／FATAL／Handle／ObjectDB。两案ranged observer均instances0，不能冒称新远程敌人已实际交战。

各五对inventory前后相等：source1695、helper17、user109、binary6、normal-protected109。两案共450对selected副本、12份binary实际副本均匹配。helper文件SHA不同仅因行顺序，路径／哈希集合相同；不是内容漂移。source共同7DA7796F5CC44C22E1919D2FE5762FAD3ECC4679B4D97433DCBCAA8E3A0DC05F。

键盘tail各只保留最后256条，focused=true且after_flush raw／owned一致，不能代替完整输入历史或OS来源证明。Main20／Explore24个>33.3ms样本、均无>50ms；早失败的诊断样本不能认证60FPS。用户已收到前台测试结束通知，不继续抢占或自动补键。

## 两处路线控制迁移：真实RED→GREEN

新增显式thruster-descent-handoff，仅从原Title／原(160,600)出生、同保存Player、两实际遭遇，到ArchiveFloor即停。返回DescentHandoffDiagnostic，full_route=false，不进入正常资格门或替代全程结果。预测仅选择原InputMap键，不写身体位置／速度、关AI或碰撞。

1. 两处Rift停靠：只在GeneratedMain调用处把旧90-frame被动wait改成同90预算内真实反向键制动，随后sampled neutral、named native contact、完整胶囊和≤0.5横速才交棒。旧Task4通用helper及walk180／180不改；实际Ready Motion／Module、实际Hz及Reverse预测不引入第二运动owner。
2. 各keyboard左离台：在原departure120内，按当前实际快照预测两held样本后立即neutral的自由落地，再接ground-neutral停距，据此选择A或松A准备较低离台速度。真实release后保持全neutral，逐tick核对；原fall120／stop60及完整目标／jump／dash／claw计数保持，pointer旧路不改。
3. 无风预测不包含墙／圆角碰撞，因此保留真实named落台收口。只有无障碍ArchiveShelf→Floor另核对预先算出的停止X与实际X误差≤0.03；ArchiveEntry受原左墙限制，预测1314.528而实际1348.0747，明确不冒称该步无碰撞数值一致。
4. 成本公开：Rift测试不再证明“松键无需反向操作也会被动落稳”；Archive测得release后的neutral合同则完整保留。不修改角色惯性／地形／敌人HP／危险／等待上限来过测试。

| thruster-descent-handoff case后缀 | engine／ms | 证据 |
| --- | --- | --- |
| 20261019-000257-714-d50f430af0ca4731ae3fcaa85dfdc7b8 | 1／17305 | 真实RED：Lower(1383.2135,1047.2533)、vx−140.80109，不满足完整中心1420..2080 |
| 20261019-000534-641-2dcafb09f9684e86af303ed62ac82355 | 1／22321 | Rift修正已过：24／23帧，各反刹6；下一RED推进到Archive错台1330.6954 |
| 20261019-000730-854-586abd1617324e6390e60466566699e4 | 0／20611 | 首GREEN：真实ArchiveFloor停止1440.0554；后补neutral逐tick与预测误差断言再冻回归 |

两RED均fresh0/0、guards=[]、只有目标行为ERROR，原件不覆盖；各版source／binary不同，不合并称同源。

## 最终同冻结三门

batch6827终态exit0。所有owned消费者终态、无Godot／dotnet遗留后才维护文档。三案fresh Rebuild0警告0错误、source1695，guards=[]、四类inventory前后相等，五log完整读回无错误／警告／Handle／ObjectDB／FATAL。

| phase／case后缀 | engine／ms | 范围 |
| --- | --- | --- |
| thruster-descent-handoff／20261019-001050-682-24d6ce16870f449b80a90699fffd0e56 | 0／20573 | 原Title到ArchiveFloor局部，新增逐tickneutral及无障碍预测误差断言通过 |
| thruster-free-forecast／20261019-001148-233-49323b1ddeb74a49ba20af3a2d7a5a1c | 0／680 | 独立几何和／地面离散和、反刹过零、peak与net、旧2800负控 |
| updraft／20261019-001222-373-1fb4def24c104a03ac4c241d7f8719d7 | 0／9625 | 旧独立保存深井：实际6帧反刹、4.633秒、HP5；不是本轮正常Main已经过深井 |

最终RiftMiddle(1943.9219,819.93365)／Lower(1454.2289,1039.9338)，均vx0，24／23帧、各反刹6，full＋sampled neutral。Lower→ArchiveEntry departure18／fall27，1准备neutral帧；Shelf→Floor departure66／fall28／ground-stop2，17准备neutral帧；release_x1465.442、vx−77.894905／vy30.833336，预测1440.055、实际1440.0554。ArchiveFloor目标合法中心1420..1860，HP5、同Player、恢复0。

共同source inventory SHA256 C2DBC1742910CD02B6F460CB144ED7EB83C77B8B008D7AE6EAA9E6F9009FB736；binary inventory FAE0CF5728AA653FD2AF587B0DDE47CECF14FF701A41475F14BE55DE3D60389A。真实用户inventory仍BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB。正式Player保存字节仍2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5。first GREEN加最终三案共655对selected与24份binary副本核验相符。

updraft保留既有FINALIZER_OBSERVED count1／EXIT_DRAIN；窄叶保留既有finally音频收尾。没有新增GC、缓存、drain、额外预算或日志豁免，不能把历史清理策略改写为本轮根因修复。

## 音频最后叶：缩小复现，不宣称关闭

新last-leaf只是委托原DelayedWorldJet原输入／帧数／同步Root.Free→runner立即Quit，增加标量ID诊断，没有生产音频改动，也不查／Dispose借用Playback。

- 严格FAIL：thruster-world-jet-last-leaf-20261018-233725-968-189a305785264e1bb44b61fdd8b67f28，engine0／1304ms但退出WARNING2 ObjectDB。Root／Audio／Voice均invalid；剩余WAV ID9223372073060009680精确对应本次自有副本，另有PlaybackWAV ID9223372073076786853，各ref1。之前场景不是复现所必需。
- 同source／binary安静诊断：同phase 20261018-234740-968-2a55db98cb0d492a97376523214d5c7d，0／1529ms；同样清Stream后refs2、RootFree后WAV仍valid，却最终无警告。behaviorAccepted=false，不能由时序相关GREEN抵消RED。
- 同7DA源码普通world-jet 234817-224534c0（3821ms）、audio-owner 234855-dabfc103（1398ms）、Sentry 234930-7f18ec2c（17791ms）本次严格GREEN；四案五日志／库存已核。Sentry仍公开既有finalizer／drain，历史音频故障保持OPEN。

固定引擎[AudioStreamPlayer内部stop](https://github.com/godotengine/godot/blob/a13da4feb/scene/audio/audio_stream_player_internal.cpp#L256)清自身list，而[AudioServer stop](https://github.com/godotengine/godot/blob/a13da4feb/servers/audio/audio_server.cpp#L1196)对non-sample标记延后删除，mix／update后续清理。这支持服务器延迟持有推断；尚未直接观察private持有节点，未证实可用的小型应用修复。不以持久voice、换资源类型、静音或借用Playback disposal移动／掩盖问题。

## 下一步与完整范围

下一先验证Main正常深井入场：其TraverseTask10ControlledAbyss仍只松D，独立SavedDescent已验证的反刹策略尚未迁移；再验证Explore冰区／返回、其余continuous／Steam／missile／Boss站位及legacy高层天花板碰撞。取得同版本完整Main／Explore证据仍需新的合适前台时段，当前局部GREEN不能补填正常失败结果。

十区正式地图、全50真实活动／manager与发布（当前新50正式0/50、24canonical未实现、正式127／候选132、43联合候选／7未解）、机关、最终原创美术／动作／UI／特效／音频／范围明确演出、历史五FAIL关闭、存档／死亡／继续／旧档、正常AV／真人／显示60FPS、七任务末review、Windows ZIP与授权GitHub读回全部保留OPEN，完整1–11 ACTIVE不缩减。十万付费人数不是可保证验收。

本轮无新图像／视频／音频文件或展示媒体、付费生成、下载、OS补键／抢前台、Git写入／发布或自动化恢复。旧版本与失败原件保留。后续展示媒体仍先存E:/OurGame/Knight/过程图稿与视频的日期／主题／版本目录并维护originals／presentation／index；真实用户存档只查保护inventory，不读语义。

