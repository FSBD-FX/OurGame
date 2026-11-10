# v092 · 配置导出赋值边界

2026-10-24，KNIGHT，唯一工程 `G:/knight/Knight`。原十一项目标仍 ACTIVE。本轮是诊断进展，没有新增玩法、美术、声音、地图或整项完成。用户更正“等一下，是没看到”为准：没有显示确认、教学遮挡复现或视觉认可；没有新开前台窗口或发送系统输入。

## 事实

实际失败的 `canyon.v2.resident.low-ceiling-bridge.03` 已不只是“读取Profile为空”：REF完整post的第12次观察中，绑定存在，setter调用1次、收到null 1次、观察故障0；原准入表达式同次求值为null。不是没调用setter，也不是先赋正常值后被清空。前一次同slot则收到正常ResidentSubtypeProfile。

这不证明原Variant是Nil。对案内同哈希GodotSharp.dll的静态IL及Release PDB行号核验：生成分派先ConvertTo再调用setter；转换null出口包括非Object/零ObjectId、native实例或GCHandle/Target桥接失败。类型不符的非null对象不是简单被`as`吞掉；转换也没有独立“已Dispose就返回null”检查。核心源码未嵌入PDB，未读取完整原生C++实现，不能从本案选择一个出口归因。

REF post随后FATAL：1841次Dispose观察ENTER、1840次RETURN；最后未返回的是ResidentSubtypeProfile、disposing=False、serial1747、managed hash27020240、native identity unknown。前两次同slot正常Profile也记录hash27020240，但缺少其Profile serial/native ID；hash可能碰撞、地址亦有复用。不能认定它就是第12次null的来源或FATAL原因。

普通post再次同slot ProfileMissing、HUD停留Title、原Playing断言失败，原生进程FATAL。同一stderr中FATAL在101行、失败detail在112行；不能称本次detail早于崩溃写出，也不能从跨流先后推因果。

## 实现边界

相对v091恰4个C#文件：ResidentSceneBinding、RunPrologueTests、Task14NativeResourceIdentity、RunResidentCensusIdentityContract；另改canonical帮助器与纯合同测试。

- REF setter先保持原赋值，再记录CLR次数/null/类型/managed hash/故障；不查询native或额外持有Resource。普通编译仍原自动属性。
- 同原Instantiate和原OR已求值字段配对；实际post不启用旧来源trace，不前置GetState/Load/Instantiate，不加缓存/GC/drain或回填。旧reader只在独立控制中保留。
- ENTER/ROW/END上限512，sink失败被包含；完整END之后Flush失败仍否决成功。真实513操作控制截断并拒绝完整性。
- 原75案例、raw/scope检查、180000ms上限、生产准入、ConfigureActor与清理路径未放宽。CLR分配/日志仍扰动时序，不能称零观察效应。

## 实际原生结果

证据根 `G:/knight/验证证据/october13-stable-claw/`。时间短名唯一对应案目录；TDD中间冻结不拼成同版完整资格。

| 时间／phase | native exit／ms | 严格结果 |
|---|---:|---|
| 125943 REF identity-contract | 1 / 534 | 实际导出setter观察RED |
| 130130 REF identity-contract | 0 / 730 | 真PackedScene导出/null/重赋值GREEN，仅诊断 |
| 130353 REF identity-contract | 1 / 618 | 赋值行与生命周期合同RED |
| 130518 REF identity-contract | 0 / 720 | 行、未赋值区分、sink/END Flush否决GREEN |
| 130937 REF identity-contract | 1 / 660 | 真实513操作上限RED |
| 131123 REF identity-contract | 0 / 715 | 上限恢复后完整合同GREEN，仅诊断 |
| 131201 REF prologue-post | -1073741795 / 174816 | FATAL、非超时；12 ENTER/12 ROW/END0、25 registry enter/24 return，FAIL |
| 132032 ordinary resident-scene-binding | 0 / 8034 | 原24挂载/7负例/3诊断返回，但stdout 136 ShapeRID allocations，FAIL |
| 132701 ordinary prologue-pre | 0 / 167171 | phase OK但7条Handle ERROR，FAIL |
| 133028 ordinary prologue-post | -1073741795 / 159561 | 同ProfileMissing/Title/FATAL，registry0/0，FAIL |
| 133344 ordinary models | 0 / 533 | PASS，最终普通构建恢复 |

11构建均成功，11原生4严格PASS/7FAIL，无超时，guards0。3条反例断言、7条Handle、2条FATAL和1条ShapeRID共13个ERROR头；136是allocations数量，不是136条错误。external镜像不双计；两个post各两条原qualified WARNING不冒充新故障。最后批处理汇总exit1，不因最后models通过变成全绿。

REF案：`prologue-post-refcounted-identity-20261024-131201-123-4bb66e54f0b14fca849d7e7de8b4d662`。普通post：`prologue-post-20261024-133028-405-6b8c9dffec824d9c8923e5f5384c2946`。最终模型：`models-20261024-133344-695-4207f82b3241449db4f3e244ee6a0959`。

## 一次审查与修正

唯一fresh review：Critical0 / Important1 / Minor1。root核对原表达式后同意Important：GateMissing与FieldState来自同次求值，不能矛盾；Binding不存在也不能读出对象。

追加窄静态核验（不是再次review）：案内GodotObject→RefCounted→Resource及ResidentSubtypeProfile均无Equality/Inequality运算符重载，原`== null`与ReferenceEquals引用判空一致，非null disposed wrapper不会因此等于null。这项关系只适用于已求值字段，不扩到not_evaluated。

唯一C/I修正轮：先加反例，真实consumer报`assignment-null-field-false-missing expected=False actual=True`，再最小收紧null=>GateMissing=true、object=>GateMissing=false且BindingPresent=true。新增三负例与“无binding但字段null”“managed hash不同仍是合法观察”两正例，纯合同77→82通过。不强迫setter必须被调用，不因hash不同自动判资源损坏。

其他纯回归96/26/33通过。真实12组行仅做内存schema回放：原缺END日志拒绝，内存补合成END后schema接受；不保存合成END，不作原生完成证明。三组原生RED→GREEN证明setter、行/sink和cap；普通全套仍FAIL。没有复审、task-done、合并或工作区清理。

新Minor：未知或半截`RESIDENT_ASSIGNMENT_`前缀行可能被忽略。本轮延期，不混入唯一Important修正；成本是诊断前缀完整性仍有缺口，不能用consumer单独授普通资格，原raw/scope/registry仍先执行。

审查Declined的root裁决、理由及成本：

1. W/stale preview未改，不授新认证；成本：Main选择/战斗仍OPEN。
2. 遮挡/receiver无新增native first-hit，不改cast准入；成本：完整目标选择未验收。
3. Intro/Playing保留真实拒绝，不因诊断通过认启动修复；成本：准备失败仍存在。
4. callback/deferred无新执行证据，保同actor/parent/pose/sequence要求；成本：生命周期整体验收OPEN。
5. 旧LoadLevel/root.Free异常语义不混修；成本：旧清理问题未关闭。
6. 原Variant、桥接及FATAL因果未知；成本：继续取证，不回填交付。
7. CLR计数/分配/日志仍扰动时序，不称零观察效应；成本：REF不能代替普通运行。
8. 完整post/资源稳定以实际FAIL为准；成本：没有完整75案例或稳定试玩交付。
9. 旧SceneWalk/借用资源Dispose不在增量，不背书图寿命；成本：独立审计项保留。
10. reviewer仅核指定manifest/快照/纯函数；root及只读custody另核55对/66 OWN bins；成本：无独立副本的历史helper归属边界仍保留。
11. Main/Explore、相机、十一项、正常速度正式视听、硬件真人及发行不由审查替代；成本：完整目标ACTIVE。

沿用用户dirty checkout，不Git写入/建worktree/删工作区；成本是没有新提交式检查点。此前四族Minor（贡献/观察完整性、旧Title/Supply/replay清理、Frozen日志Flush保护、等比缩放独立用例）保留；资源崩溃不是Minor。

## 保管与下一步

55/55案内manifest pairs、66/66各案OWN sealed bins匹配，缺失/额外/重复0。当前1776源/892C#、9帮助器、6普通二进制，live哈希漂移/缺失0、额外bin0。重新执行原源清单枚举，差异0，不只核对已列路径。最终普通pre/post/models源码清单同哈希；pre另有原额外帮助器（10项），post/models为9项，不称三者helper清单完全相同。更早REF不冒称同一helper冻结。

- Source manifest：`C1DB92D9B1DE476542128CA3CF155ADD214D1364C2D6BEA9F8B2657AE225AC64`
- Binary manifest：`0CA974A158E66FEFDD22B9E4AE9FC50E723068724AB1732867D7A3AF8F630D4F`
- Knight.dll：`B9342A81C55BD1AB2BBAA4C6A0C2A4EF2A89658FFB73BCAE057D498533146B0C`
- index：`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`，branch/HEAD未变。

下一轮只在原空字段拒绝后，考虑同一个PackedScene的有界保存值快照；保原赋值行，不回填/重试准入，明确“拒绝后、非原转换时刻”。二次桥接可能重建wrapper并扰动后续清理，不能倒推原Variant或FATAL同因。当前尚未实施。未改策略的完整Main/Explore本轮不反复重跑，旧死亡失败仍OPEN，没有同冻结双路资格。

git diff --check退出0，169条既有LF/CRLF提示，其他输出0。无新正式资产，资源表无新增正式项；无新过程媒体、付费、自动化、打包、删除、前台或OS输入。真实存档只受原哈希保护，不读语义或改写。媒体一次手动检查UTC05:35:15.3998040，860284字节、基线3508、增长856776、Notify=false；20GiB只正常汇报顺带提醒，不停工。root所有构建/原生owner已终态。完整十一项、齿轮战斗/冷却、资源稳定、相机、正式视听/硬件真人与交付仍OPEN。
