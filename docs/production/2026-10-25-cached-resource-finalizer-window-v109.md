# v109：缓存资源再次取用与托管寿命对照

2026-10-25，KNIGHT / `G:/knight/Knight`。本轮是原十一项的诊断进展，不是生产修复、视觉验收或正式交付。用户“等一下，是没看到”仍为 NOTSEEN；不能记为看到了窗口或教学仍遮挡。没有新窗口、系统键鼠输入、图稿或录屏。

## 事实

原 `RunSourceLifetime` 六阶段、两次真实赋值全部保持，之后追加独立的缓存取用实验。使用实际保存的 `scrap-scatter.tscn`、原工厂及 `canyon.v2.resident.low-ceiling-bridge.03`；不生成备用 Profile、不重试、不修改准入、默认缓存、生产 GC 或 Dispose。

创建助手返回后，先在 Profile 仍有强引用时回收并排空场景包装对象，必须实际观察 Scene weak-dead / Profile weak-live。随后测试专用 CLR finalizer 门进入，最多等待5000ms；finalizer 不调用 Godot、不写日志。主线程等待进入/返回各最多1000ms，前提未建立就失败、不重复直到成功。门占据 finalizer 线程期间，仅 A 丢弃 Profile 强引用，再执行一次不等待 finalizer 的 GC，才调用实际缓存加载与原工厂；finally 必须释放门。

第一对照 A 得到 Profile weak-dead、真实工厂 null-rejected，随后 typed `ResidentSubtypeProfile` finalizer FATAL。但 B 虽保持同一有效 Profile，退出仍在另一个 `RangedWeaponSettings` finalizer FATAL。这个 B 不是干净对照。

精确保存图只有这两个自定义 Resource，因此第二对照只消除已证实的兄弟变量：两臂同样保留实际 authored Settings，仍仅 Profile 强引用不同。没有扩张为“全局永久保活所有资源”。

- A063647：Scene weak-dead、Profile weak-dead、Settings weak-live；真实 reload null-rejected；门正常返回，随后 typed Profile serial2/hash48342081 FATAL。只有五个新阶段、无 COMPLETE，严格FAIL。
- B063734：两资源存活，reload 获得同 ProfileId `9223372065896138270`、SettingsId `9223372065829029402`；六阶段、门进入/返回且未超时；之后两种 typed finalizer 都有 ENTER/RETURN，native0/863ms。
- B064322：增加新严格消费者后再执行，native0/849ms；原六行、新六行与两种资源退出回执通过，输出明确 `behavior_accepted=false`，不是游玩验收。

新消费者独立调用在旧生命周期消费者返回之后，避免旧的“不确定”早返回绕过新证明。检查精确六阶段、真实布尔、无符号身份范围、模式/路径/slot、两资源身份、门状态、旧→新 stderr 顺序，保留原错误/警告/FATAL/非零退出 veto。不比较 stdout 完成标记与 stderr 的拼接偏移。

先删除新行，通过 AST 执行真实 owner 分支，实际发生 `SOURCE_RESURRECTION_RECEIPT_RED`（旧 owner 错误接受）；增加消费者后，实际B正例及篡改/缺失/错误标记等58条纯合同通过，预算实际入口24条通过。失败A没有最终诊断完成标记，整份生命周期收据应拒绝，不能补造完成行。

## 推理与风险

已证实的是此隔离场景中“CLR弱目标已收集、finalizer尚未完成、再次从缓存使用”的异常及受控保活差异。它为资源寿命方向提供具体证据，尚未把全部普通 Main/pre/post 历史故障逐一归因，更未实施生产修复。

两项只读所有权核查没有找到 manager/binding 或保存图读取器 Dispose 借用资源的违规。实际 GodotSharp 返回的 Variant/NodePath/StringName 值容器有自己的释放责任，不应凭猜测删除合法 using。官方 [Godot issue83762](https://github.com/godotengine/godot/issues/83762) 与 [issue112067](https://github.com/godotengine/godot/issues/112067) 有相似缓存/弱句柄问题；外部相似性不证明本工程所有故障同因，也不保证升级或 IgnoreDeep 一定修好。

下一生产候选所有者是随关卡/world生灭的 `CanyonResidentManager`，不是全局永久cache。但“首次实例化前可靠取得完整 authored 托管图”必须先实测：仅持 PackedScene 不等于持 C# 子资源，仅在成功 actor 后采集也遗漏首次失败。24个 resident 保存图还含 PresentationProfile 与各 tuning/settings，PresentationProfile 又可引用嵌套场景，不能仅推广本次两字段对照。暂不改生产保活/缓存/Dispose/引擎版本。

## 全部八个原生案例

根目录 `G:/knight/验证证据/october13-stable-claw/`。八次构建均0，八次原生均非超时；严格5PASS/3FAIL。最终新owner的B与三个普通案例均通过，不抹去早期失败。

| 目录 | 构建ms | 原生exit / ms | 严格结果 |
| --- | ---: | ---: | --- |
| `resident-resource-source-unrooted-refcounted-identity-20261025-062820-249-8458975670d949b993fdba61e3d5b55f` | 27004 | -1073741795 / 10313 | FAIL，Profile缓存取用异常 |
| `resident-resource-source-rooted-refcounted-identity-20261025-062910-639-53fb102843784e848dcb04feba7af7d4` | 28611 | -1073741795 / 6722 | FAIL，Settings兄弟变量干扰 |
| `resident-resource-source-unrooted-refcounted-identity-20261025-063647-119-e7c1c5bdb9a14cd78e1f676b071f54b6` | 30404 | -1073741795 / 7254 | FAIL，Settings活而Profile空/FATAL |
| `resident-resource-source-rooted-refcounted-identity-20261025-063734-816-0c2c2d8bb1cf491592ffde356c4ce2fd` | 32574 | 0 / 863 | PASS，新行当时仍人工核对 |
| `resident-resource-source-rooted-refcounted-identity-20261025-064322-902-fb92bd308e05430980af6892c3316452` | 33608 | 0 / 849 | PASS，新严格消费者 |
| `prologue-pre-20261025-064739-933-4eb58493631b4c93862cde6f68c2e3bb` | 33198 | 0 / 171873 | PASS |
| `prologue-post-20261025-065117-590-94017fca3ecd47ccb422bd6bb33fa0f4` | 30958 | 0 / 206294 | PASS，原75及owner8次 |
| `models-20261025-065524-273-2be1bb7ebe544ab989074105b51a31f0` | 27148 | 0 / 559 | PASS，恢复普通Debug |

post 原75案例完整enter/return、retired0、原owner重复8次，seamless/room-profile/flow-registry预期负例警告scope完成，没有新豁免。它不是正常标题到出口的 Main/Explore；两路本轮未改策略、未重跑，v108死亡失败继续保留。

非原生尝试亦保留：一次外层PowerShell引号解析失败，未启动构建/游戏；纯测试一次AST条件引号错误后修正，随后才取得有效删除新行RED；把失败A当完整正例的命令被旧完成标记检查正确拒绝。它们不计作原生游戏测试，不冒充有效行为RED。

## 单次审查与全部裁决

一次全新只读审查 C0/I0/Minor0，无第二审查、无修复回合。指定v108普通models快照没有诊断C#实体文件；实际比较采用015848案保留、SHA相同的 `ED082F945EEABFA730DA3C58155FA9B7D5BD4884BDFE42A25DB506272F678D11` 文件。审查的13项未认证范围逐项裁决如下：

1. 生产根因/修复仍OPEN：隔离窗口不是全部历史故障确诊；误判会把不稳定版交付用户。
2. 生产保活/缓存/Dispose/版本不变：完整图和寿命证据前不推广；误判会引入驻留、泄漏或首次取用漏洞。
3. 早期B保留Settings FATAL/FAIL，不回填最终通过；误判会混淆对照变量。
4. 当前没有完整A正例，不补造完成标记；误判会把负面证据包装成绿色合同。
5. 消费者要求完整A/B都复用原ProfileId，比C#对A成功的断言更严格，目前没有误收。未来A若合法换身份须另行裁决，不能静默放宽；代价是可能拒绝合法新策略，不是当前误收。
6. 门上限仅诊断边界，不是调度/性能保证；前提未建立即失败/不重试，否则会把未执行当成功。
7. W与实际reader/controller未改，不新增资格；误判会掩盖输入时序问题。
8. 物理遮挡/receiver资格无新证明，也无第二次resolver授权；误判会掩盖穿墙或选敌错误。
9. 本实验不入树，不替代Intro拒绝/自然Playing准备；误判会绕过开局准备。
10. detached Free不证明真实callback/deferred执行，原actor/parent/pose/sequence义务保持；误判会遗漏生命周期故障。
11. LoadLevel自有mounted清理与主/清理双异常保留义务不变；误判会丢主错误或遗留节点。
12. 仅表中普通阶段通过，完整双路/原十一项/正式视听相机/真人硬件/发行仍原要求，用户NOTSEEN；误判会把headless诊断冒充画面、手感和FPS验收。
13. 审查仅三文件增量，不背书全部历史dirty branch；root另核清单/二进制/分支/index，否则其他修改可能被错误续签。

## 保管与未变边界

独立核对八案40/40前后清单对、48/48封存二进制SHA，缺失/漂移0。每案1844源；兄弟变量修正后的六案源清单相同，SHA `60B4B60C0C8AA308F1687222BD03C8C9AD1B471BE61311B4A454AEEF67687E31`。最终live1844/1844逐项匹配、普通Debug6/6匹配。helper因phase为12/10/9项，各案前后相同，不谎称跨phase同一集合。

相对v108源清单仅 `tests/diagnostics/RunDetachedEnemyResourceRepro.cs` 改变，SHA `2A356E83814BA6904FE049C6B3667EA209D0DB6ED3E3D3FEE172B17C07B92C63`；新增0/删除0/C#892不变。另改canonical owner及现有纯合同脚本；owner SHA `178B0E22F151A607995F9AB98BFBAA912724CCBFC2AF0921A12304A983D04F7C`。原Main测试、生产玩法/物理/地图/人数/正式图声未改。原总线CAD39DF12...、HEAD e7c70498...、index21610FDC...不变。

所有root句柄结束、无本工程Godot/dotnet存活后才更新文档；无Git写入、版本标签/Release、删除、付费生成、自动化或真实存档语义读取/写入。v109只是开发记录号，不是发布号。首份文档合并patch因最后一段上下文不匹配被原子拒绝，未留下半份文档；随后分开写入。最终diff-check exit0，已有LF→CRLF提示169/其他0，index精确SHA不变；普通条件编译构建不复制该诊断C#，有其源哈希及同哈希实体基线，不冒称普通快照包含不存在的副本。

仅执行既有媒体Check一次，UTC2026-10-24T22:56:33.8184829Z：Current860284/Baseline3508/Growth856776 bytes，Notify=false，新媒体0；不新增通知/停工/后台任务。

原十一项目标继续ACTIVE。下一步先证明适当所有者在首次工厂前取得并保持完整资源图的接口，再验证生产路径/正常双路；不回填空Profile、不删怪物、不放宽原门槛、不挑选安静日志称完成。正式美术动作/声音/相机、真人硬件和打包发行仍须实际完成。
