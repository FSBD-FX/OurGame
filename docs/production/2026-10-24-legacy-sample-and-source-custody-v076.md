# v076 · 旧样段可玩恢复、逐项长测边界与可编辑源保全

2026-10-24，工程`G:/knight/Knight`；完整十一项目标ACTIVE。上一轮是有证据的诊断推进；本轮有实际生产样段修复和资源保全，不是整关、最终美术或发行完成。没有打开窗口或发送系统输入，用户“没看到”反馈仍有效。

## 事实、推理、风险

事实：独立旧样段真实Start原先失败；Supply失败现场为Layout1/DevelopmentV001/Intro/pending=true、P=(2650,1856)、V=0、CanProcess=false，并非实际缺地板。正常居民生成上下文只支持V2，旧样段又继承新版内容开关。显式局部修复后，实际样段Start/死亡/Title/重载Continue/新局和完整四项Supply均严格通过，正常V2原24启动及四个占位正反例保持通过。

推理：这是旧样段的内容/几何适用边界冲突；不是增加几帧就能解决。正式V2准备不能按“任何Layout1都跳过”改成fail-open。另一方面，实体ProcessMode使用原有SetDeferred，不能要求它和同步Playing/checkpoint在同一个调用栈即时交付。

风险：本轮没有证明GCHandle根因修复，也没有正常Main/Explore资格。pre仍13条Handle ERROR和后续FirstCombat启动断言；post原180秒超时。局部样段绿灯不替代正常大地图/全76实战或最终视听。

## 生产与测试改动

`BasinQualitySampleEntry.ConfigureLegacyWorld`只用于实际旧样段和其受控Supply夹具、在挂树前选择原Layout1几何与现有FinalOnly模式。不修改全局NeedsResidentPreparation、V2布局/居民管理器、HP、碰撞、力学、位置、敌人数量、存档路径或自动启动差异。

真实sample生命周期原同步Start→Playing/原checkpoint断言不动。将原2帧放在SetDeferred模式观察之前；Title同步状态单独检查，再以2帧观察World/Player冻结。Supply仍原3+8帧，8帧前新增实际Playing/nonpending断言；原治疗回调同步ReturnTitle→StartNew清新run合同、伤害/恢复/保存/Continue均未异步化或豁免。

旧几何隔离测试不再把V1样段和V2正常布局强行比较：保留独立字面值offset(1280,1440)、bounds1600×720、前Descent/后Ascent、interaction(756,336,48,48)，另验证重新加载的正常Level仍current layout/DevelopmentV001且definition无alias。

正常Basin保存时Actors为空，保存Gear/Steam Encounter节点，原26按catalog运行生成。因此旧“正常Basin直系恰好1Rust/1Bell”不是现行生成合同。现在从真实保存Gear EncounterId查权威catalog，仍要求原Rust(2300,3960)/batch0及Bell(2860,3794)/batch1，加载其原leaf验证真实类型和Bell调参只允许sample DetectionRange差异。只Free自有实例，不Dispose借入场景/调参。sample自身仍0Rust/1Bell；不把保存记录核对冒充正常敌人已出生或战斗通过。

## post逐项定位：观察，不新增虚假资格

registry新增可选观察器，实际await前ENTER、成功完成后RETURN；不在finally伪造RETURN。同步成功、未完成Task、同步异常和异步异常均有真实行为测试；原异常身份保留，失败不启动下一case。普通共享调用默认无观察器。post记录exact name/index/physics/process/单起点elapsed；总catch在原清理await前增加managed异常类型标量，原75例、180000ms、清理和原完成集合/警告验收保持。

实际post93行=47ENTER/46RETURN，最后index46 `PrologueVfxRuntimeTests.VerifyRuntimeFrameBinding` ENTER；index30后来正常RETURN，不能把观察中的暂时未返当卡死。没有post failure-entry收据。

case46在挂载后等5个ProcessFrame、加runtimeRoom后再1个ProcessFrame、最多240个PhysicsFrame等音频计数，之后才打印TASK4_BINDING_DIAGNOSTIC再断言/清理。本次未到该诊断，仍不能区分同步load/mount、前6个ProcessFrame、物理循环或总成本。240物理步不等于墙钟上限。

计时勘误：22025ms是registry-relative，不是进程relative；`180059−22025≈158s`把未知最后日志到kill的尾段混入，不能证明registry在158秒才启动。本轮不增加预算、不判永久卡住。下一最小证据是在既有标量中附加同次读取的engine tick原值，再决定是否需单独原delegate对照；不以更小套件替代原75。

## 一次独立审查与裁决

无Critical、新Minor；一个Important已实际RED→GREEN。新两phase的ValidateSet接收大小写，而专用检查- ceq且未禁止ExpectedFailure，可能让`POST-BOUNDARY-CONTRACT`加“unknown stable claw phase”被命名RED接受，实际没有执行测试。当前入口在副作用前拒绝非canonical及waiver/movie/verbose/identity/trace混用；实际早期AST测试48例先46误接收，修正后48PASS，没有为测试此风险启动引擎/窗口。

新合同实际脚本、owner和最终执行输出保留于`G:/knight/验证证据/october13-stable-claw/v076-admission-20261024-0056`，副本hash相同。一次修正后无重复审查；最终五专项全绿，不称全套绿。

审查未判断事项逐项裁决：

1. live批次终态/总数由root真实记录核验，见下；代价是绝不把审查意见代替FAIL。
2. 脏工作区/同SHA范围无法代表本轮全部历史归属；保留用户其他修改，只判本轮限定文件和快照。未覆盖全仓差异的风险仍在，不做Git写入。
3. 二进制/各清单由root逐项重核，99现有对一致，构建失败的一对缺失如实保留；不借此认证完整运行稳定。
4. GCHandle/FATAL/RID及quiet概率没有根因结论，继续OPEN，不改生产cache/GC/引擎；否则会隐藏真正故障。
5. 未新增150行全post消费器；诊断行不参与通过资格，原硬编码75实际完成集合与收据仍必需，不用部分行接受post。
6. Drain打印不等于root释放；failure-entry只定位进入catch，不是完成清理。风险仍是异步清理/等待未定位，保留原逻辑不假设。
7. 任意抛错/取消/重入observer和新caller清理故障未专项认证；当前唯一post回调固定为标量日志，默认共享调用无observer。若将来扩展为任意插件回调，需另立错误/取消合同，不能沿用本次覆盖外推。
8. 全部sample localization/所有旧sample测试、真实档/迁移未全验；本轮只对实际独立生命周期及完整Supply声称通过，真实档未语义读取。
9. 93份美术源root独立hash核验，16漂移原因未判；索引批准不代替当前像素和总装批准，不接入遭拒A2。
10. 完整十一项/Main+Explore/全人口实战/最终视听动作/硬件真人/发行一律未关闭，不将窄样段/静态源留存作为最终交付。

## 可编辑美术源：漏迁已留存，版本未冒认

明确旧源`E:/OurGame/ArtProduction/thread-spool-bearer-v1/masters/complete-layered-001`在旧Knight树之外，不在过去那棵树的迁移清单中。现93文件/10,276,779字节无损复制到：

`G:/knight/美术源文件/迁移留存/2026-10-24_complete-layered-001_未发布`

复制前后E源和G副本逐一SHA相同，无reparse；E原件未删未改。目标另外新增说明和两CSV，不覆盖历史JSON/SVG/PNG。35层PNG、6SVG都存在；旧final-manifest中19层相符/16不符/0缺失。差异包括torso、左肢、胸背、mantle及两段tether，原因未确认。

review-state的六组部件阶段为accepted，但dressedMaster仍ready-for-review，smallScale/animationReady not-started。当前35层不能整体当原批准字节，不直接塞进Body、重复肢体或重走被拒整图拉伸路线。原合成body-001虽被接受为参考，不是正式分层动作。旧绝对E路径作为历史原件保留，不直接执行内含脚本。G副本有`KNIGHT-留存说明.md`、`source-custody-20261024.csv`及`layer-version-audit-20261024.csv`。这是约9.8MiB暂时重复的源保全，不是付费生成、发布或最终美术完成。

## 全部二十案

根`G:/knight/验证证据/october13-stable-claw`。19次原生=11严格PASS/8FAIL，另1次构建失败；不同中间冻结不可合成绿色或完成比例。最终B五专项全部PASS，A完整pre/post的失败仍保留。

| 完整目录名 | 退出 / 毫秒 | 结论 |
|---|---:|---|
| basin-supply-lifetime-20261024-002420-558-7220fe067c5b471bac6de62a96a22d97 | 1 / 1915 | 原floor失败，真实Intro/pending现场 |
| basin-sample-lifecycle-20261024-002627-071-6793cd087adf424d805a96e851a70315 | 1 / 1648 | 实际Start/Playing原断言RED |
| basin-sample-lifecycle-20261024-002816-607-0b9f713b0f9847c4875ab8990e99f5b2 | 1 / 1753 | Start已过，旧同步ProcessMode观察失败 |
| basin-supply-lifetime-20261024-002849-094-6392a5b3833844c6826f4dd3940cc66b | 0 / 11676 | 原完整四项/同步Healed/保存严格PASS |
| post-boundary-contract-20261024-003132-449-9bf8800fc961407fae03d9a75c0d78a3 | 1 / 784 | 真实观察顺序RED，只有body无边界 |
| post-boundary-contract-20261024-003418-305-d59415a740c041908fffd3e71229147f | 0 / 532 | 同步/异步/异常/未返合同GREEN |
| basin-sample-lifecycle-20261024-003455-033-d9148bb9583048f1a2b3e941ff8738bc | 1 / 1839 | 旧V1与正常V2几何比较失败 |
| basin-sample-lifecycle-20261024-003746-096-92b4a8d9d3ef47178267fa5248799e3f | 未启动 | CurrentLayoutVersion类型名写错，构建FAIL，非行为RED |
| basin-sample-lifecycle-20261024-003906-474-5398e5e59fb749209c38393a19be7f76 | 1 / 1965 | V1隔离已过，旧直系Rust=1比较失败 |
| post-boundary-contract-20261024-003940-698-5b737c0a1c2144ea9fd049885b197e42 | 0 / 530 | 原registry变异/分类＋新观察合同PASS |
| resident-auto-start-20261024-004015-896-8cfb520032e246808a5cc0d167b83230 | 0 / 35349 | 原24/占位四正反例/真实准备PASS |
| basin-supply-lifetime-20261024-004130-675-08fee83d52aa400a9ced37c52b9320ac | 0 / 12080 | 完整Supply PASS |
| prologue-pre-20261024-004213-938-4d27de3618864bc99885d3f527ac4406 | 1 / 92262 | 13Handle ERROR＋后续FirstCombatGuidance启动Title断言，严格FAIL |
| prologue-post-20261024-004419-742-77df41d2f05b42babab2e61c2bed9246 | -1 / 180059 | 原watchdog，93诊断行/46成功返回，不给整post资格 |
| models-20261024-004755-474-8b8df945616e4ef088ffb95102aeaa3c | 0 / 532 | 普通模型PASS |
| basin-sample-lifecycle-20261024-005206-876-3653763680054185ac2343f7c9cf4577 | 0 / 2023 | 实际独立生命周期完整GREEN |
| post-boundary-contract-20261024-005240-252-77c941605eea4f29a61f17de710d9517 | 0 / 523 | 原registry全部变异/分类＋新边界PASS |
| basin-supply-lifetime-20261024-005309-272-b9968cc05b14445682de234d9edfbd00 | 0 / 11200 | 完整Supply及同步新局/保存PASS |
| resident-auto-start-20261024-005350-771-60665887893e4663a6e94ce5c5a2dde6 | 0 / 27523 | 正常V2原24及四占位正反例PASS |
| models-20261024-005446-166-01b0266f19c3490fb6b5992cb7e9b528 | 0 / 442 | 最终普通构建/模型PASS |

## 保管与下一步

99现有source/helpers/user/continuation/binary清单对一致、差异0；构建失败003746没有fresh binary pair，唯一缺失如实保留，不补造。最终1776源/892C#逐行live漂移0，普通二进制保存，root原生全部终止。source manifest `9F4FEEB2C19D44F165FF9DCEBE9A5D546AAEA5A88B4375B2D483C18890541FAC`；binary manifest `E49CCC2E4909A9947885978C7C08C98846D0BF846B0DDE7CD22D3C9FE327892D`；Knight.dll `F31580064D6E7FA96B888E81B43953C8C5AB5C87E7844EB0B940BC78B98CEB3D`；Git index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变。

下一安全工作：先以真正时间边界/原VFX delegate定位post成本，资源与FirstCombat准备失败继续按实际原因排查；机械爪仍需同一失败输入下的release/body-clear/Idle/Gallery/LOS独立边界，不试第四个猜测阈值；已找回的美术源先厘清漂移与母版批准，不能继续让测试替代正式视觉制作。完整Main/Explore、全76/新50实战、全部正式视听/UI/音频演出、硬件/真人/显示、ZIP/GitHub仍OPEN。

无付费/新生图或声音/展示媒体/自动化/前台/系统输入/Git写入/用户资料删除。一次正常汇报手动媒体检查UTC16:56:17：860284 bytes、基线3508、增长856776、Notify=false；可编辑源不是过程录屏，不以20GiB阈值停工或建立调度。
