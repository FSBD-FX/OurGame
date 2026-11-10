# KNIGHT v160 · 同次真实攻击窗口与失败关闭

完整原十一项目标保持 ACTIVE/OPEN，没有缩成测试或兼容性目标。本轮没有新增玩法、美术、动作或音频资产，没有新版像素/正常速度/硬件真人验收，也没有正式交付。

## 事实：本轮完成了什么

- v159前段失败并不是第二J未响应：攻击ID2已经启动，但旧断言经过Settle后才读取，模型恰已自然完成。本轮用同一次真实J，**搬移原30次完成采样等待、不增加总量或补按J**，使迟到continuation确定性触发旧断言RED。
- 修正后，测试拥有的观察节点在真实Combat之后采集physics、Guidance之后采集process值。它不发输入、不手动Refresh、不在native回调抛断言。最终停止观察后核验全部实际live记录的同ID、K权威、完整自动指导，再检查同次自然结束的快照被拒绝。
- 正常路径与迟到路径均通过。保留原HP5、恢复0、真实抓取/上拉/Space借力释放、无额外普通跳及过热等后续控制。这里证明的是权威和指导合同，不冒称本次真的在J中执行了K连招。
- 只改变两个既有测试C#和canonical消费者：`DescentClawGuidanceRuntimeTests.cs`、`RunStableClawPullTests.cs`；没有生产代码、场景、地形、输入规则、战斗数值、缓存/GC或引擎更改。1857源/897C#不增减，143固体/9残片/76普通敌人目标保留。

### 唯一独立审查与修复

新鲜最终审查C0/I3/M1；三项Important全部接受，在同一个修复轮中完成反例验证，无二审：

| 问题 | 修复前真实控制 | 修复后 |
|---|---|---|
| 只读首行，后续攻击阶段锁K/隐藏卡片可能漏检 | 首行好、后行physics锁/后行process锁/后行隐藏卡片，三项均错误接受 | 所有实际live行逐项验证，三项反例均拒绝 |
| 普通路径原30次完成等待期间的观察异常可能漏报 | 初始验证后再记录错误，原边界仍成功返回 | 完成全部等待后先停止观察，再验证最终错误；迟到错误拒绝 |
| 观察器在前提检查/挂树保护之前分配 | 禁止处理与初始化抛错两条拒绝路径有对象净保留 | 前提先检查；自有Node的初始化/挂树失败统一清理，两条实测保留0 |

反例共6项；14个独立字面窗口资格控制、原4状态重复读取纯度控制也保留。失败路径控制只涉及本次自建测试节点；**不能把它当作历史未知2 ObjectDB实例的修复**。原始两个审查RED的precondition保留2、initialization保留1，不写成各1；一次正常创建/Free只用于建立成功生命周期基线，未取到额外那1个全局对象的身份，不命名为已确定的脚本元数据。

## 严格原生结果

最终普通7阶段6PASS/1FAIL。当前控制GREEN包含全部审查修复，前几个仅检查首行的中间GREEN不是最终证据。统一命令：

```powershell
pwsh -NoProfile -File .superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1 -Phase <phase> -SourceRows 1857
```

所有case根为 `G:/knight/验证证据/october13-stable-claw/`，下表保留完整后缀。

| phase / case后缀 | build exit/ms | native exit/ms | 严格结果 |
|---|---:|---:|---|
|descent-j-observation-control / 20261029-230625-465-9d40b8e2c98a47adaa22beddca14b162|0/31715|0/16511|PASS，6审查控制＋14资格控制＋真实17physics/8process全部记录验证；迟到拒绝/同ID自然结束|
|descent-guidance / 20261029-231056-766-3dbe65f17fcb49c8a567f32c4fc523d6|0/40678|0/16008|PASS，普通默认入口及完整原专项|
|models / 20261029-231205-878-6d9119b267734b9580515eae24bbc0dc|0/45014|0/574|PASS，仅既有机械爪模型/到达几何，非短喷全单测|
|thruster-approved-lantern / 20261029-231300-981-b5d81a5f20e5491f998fe32783a7e8ac|0/40175|0/6054|PASS，保留入口12状态几何合同，非像素/真人|
|thruster-approved-lantern-layout / 20261029-231356-707-c7521d60d5ed48a9b4040dff56322040|0/37462|0/8198|本次安静PASS，不关闭旧2ObjectDB失败|
|prologue-pre / 20261029-231453-596-d443713dec4743b482806b11b4bc4f96|0/33461|-1073741795/30935|FAIL，原生Finalizer FATAL；未到新第二J窗口|
|prologue-post / 20261029-231609-588-09c9308c74a940039671cdd6f4964b8b|0/38472|0/212605|PASS，原75案例＋8次owner、原4条限定WARNING|

post四WARNING仍是seamless观察者1、room-profile负控1、Completed→Recovering拒绝2，未添加新豁免或声称全日志零警告。pre未超时，保留原180秒上限；post保留原300秒。完整Main/Explore最新失败仍为v158，本轮测试改动没有修复它们，也未盲目重跑两路筛绿。新窗口逻辑在独立native已验证，但**本次合并pre在更早处退出，不能称已在完整pre覆盖到**。

### 全部中间尝试（不覆盖失败）

| phase / case后缀 | build exit/ms | native exit/ms | 实际含义 |
|---|---:|---:|---|
|verbose layout / 20261029-222124-296-70cc873788e6494cb8d29b2314718fc3|0/45634|0/8804|诊断PASS，未复现ObjectDB，非修复；无再次dump采集|
|second-J control / 20261029-223223-280-405df6485b984c54be347443ed9c67d0|0/50016|1/14861|同次真实攻击自然完成后旧live断言RED|
|control / 20261029-223845-889-c7d8b7bfe67842d68452841fa8a72e4f|1/24964|未启动|CS0136命名冲突，非行为RED/无fresh bin|
|descent / 20261029-223918-112-7165f6eb8fd44b17af211f64155ae6cb|1/23304|未启动|同一构建错误，原件保留|
|control / 20261029-224204-623-2da3baedb1ec424da9b6680fce69ea68|0/30726|0/15075|首次GREEN，首行17/15观察，后经审查加强，不当作最终全行证明|
|descent / 20261029-224337-745-a52fcf6f1ed849d490c30ac2fffeb3a3|0/41564|0/16149|首次普通GREEN，17/9观察，非最终审查修复版|
|models / 20261029-224631-812-b8cd01daafd14aeabf0511db1acb4ec7|0/34969|0/1050|中间PASS|
|binding / 20261029-224717-948-7e32a87a2a314d18bb1697af20bbed63|0/35230|0/4697|中间PASS|
|layout / 20261029-224806-179-0df8b92ae5444a8f926173e9840b6f84|0/27541|0/7054|严格FAIL，实际2ObjectDB警告|
|review control / 20261029-225909-837-f5ae73f365af44dd9cb727811d793995|0/34584|1/655|全部6审查反例RED|
|review control / 20261029-230231-731-96c80bda410141f0bdc8e062d7b8c1ea|0/30204|1/652|成功生命周期基线后仍6反例RED，拒绝路径计数2/1|

18次共11PASS（含1诊断）/7FAIL，其中2次没有启动engine。绝不把构建错误算成行为RED，或把中间通过拼成正式完整通关。

## 事实、推理、风险：原生崩溃的新依据

当前pre原始栈：`gchandle.is_released()` → `mono_object_disposed_baseref :1788` → `GodotObject.Dispose(bool)/Finalize/GC.RunFinalizers` → `0xC000001D`。受查14文件表明：Camera先Basin、再低Ascent、再高Ascent，之后才会到Descent；低Ascent的12样本和CASE_OK已完成。最后audioDrain标记不代表Level销毁已结束：标记后仍有一帧，再QueueFree根和等待两帧。只有之后才可能进入高轮同一PackedScene的GD.Load/Instantiate，原始日志没有证明已经到达。

受查路径存在自定义Resource定义/Profile及正常的PackedScene源缓存再加载，但没有发现对借用定义/Profile/PackedScene显式Dispose、WeakRef/实例ID取回、assembly reload或SerializeState证据。清引用之后后轮可再次加载同路径，不等于已证明wrapper复活竞态；不能仅凭audioDrain后崩溃认定音频有错。

只读官方检索提供了强相关线索，而不是项目根因证明：

- [Godot #83762](https://github.com/godotengine/godot/issues/83762)：当前仍open/confirmed。2026-10-28维护者说明线程安全修复仍需处理，同日4.7复现记录具有同形1788/Dispose/Finalize/GC栈。未核实任何合入修复或可用修复版本；不运行其中主动GC/阻塞终结器的MRP。
- [PR #87045](https://github.com/godotengine/godot/pull/87045)：候选被认为绕过症状而非修根因，2024-08-08关闭，不能称已合入解决方案。
- [Godot #112943](https://github.com/godotengine/godot/issues/112943)：相似栈另有程序集/SerializeState背景，关闭理由是暂未再见，不能据此证明我们同因或已修复。
- [官方4.7.1发行](https://github.com/godotengine/godot/releases/tag/4.7.1-stable)的短hash `a13da4f` 与本机banner相容；页面未完整展示九位hash，不据此否认本机记录。

下一有效动作是区分低轮teardown返回、第三轮Load进入/返回、Instantiate返回及实际出错类型/身份，再判断生命周期修法。不得先加强引用常驻、改cache/GC、抑制断言或更换引擎。若后续确需额外原生工具/dump，另需用户授权。

## 原十一项状态澄清：M01–M08不是“尚无代码”

以下是只读源码和实际调用链核对，**不是本轮执行全部移动专项**；不勾任何完成框。已将旧“待实现”统一改成“核心代码/接口已接入、当前完整资格待验”。

| ID | 已有实际接入 | 独立专项入口（本轮未执行，不借models背书） |
|---|---|---|
|M01|MobilityModuleDefinition Resource→ToSettings/Validate→PlayerMotor校验后提交；稳定ID/不可变快照|thruster-module、initial-module、authoring-contract、saved-module|
|M02|KeyboardAim归一化、ThrusterPulse有限燃烧/锁向/拒重复，实际Motor走StepFree|pulse、dynamics、input、player|
|M03|ThrusterDynamics余速/摩擦/指数空阻/有限反刹；碰撞速度写回|dynamics、player|
|M04|真实重力/终速、Space仲裁/空中次数与真实落地恢复|pulse、dynamics、player、traversal|
|M05|eligible→撤爪→回调后身份/生命/authority再核验→commit，仅接受时继承速度|player中的各阶段接受/拒绝/回调失效|
|M06|拉近/摆荡/绳重力/风区分权，下劈ExternalImpulse替换速度|player、ThreadInteraction、LegacyMomentum，整组合仍开放|
|M07|LandingImpactSampler唯一接触，冲击伤害及不可靠时高度回退/独立即死边界|impact-formula/config/player/negative/support/reentry/dangers/legacy|
|M08|TryEquipMobilityModule真实支撑/状态资格、快照校验提交/epoch|equip/support/guards/claw/formal/initial/saved-module|

确切源入口包括`scripts/config/MobilityModuleDefinition.cs`、`scripts/player/MobilityModuleSettings.cs`、`ThrusterPulseModel.cs`、`ThrusterDynamics.cs`、`PlayerMotor.cs`、`LandingImpactSampler.cs`、`PlayerFallDamageModel.cs`。专项由`RunThrusterCoreTests`分发；当前stable `models`只运行MechanicalClawModel/ArrivalGeometry两组，不能给M01–M08续签。旧motion冲刺代码仍存在；不称已全仓删除。换装仅接口，不是背包/掉落/所有权存档。M09全消费者/正常双路/30-60-120Hz/硬件手感仍OPEN。

## 本轮裁决与延期（全部）

1. 继续已授权的完整目标，原有验收缺陷用native inline修正，不为每个微任务重复暂停。代价：一位作者＋一位最终审查，不当作逐任务独立验收。
2. 保留用户指定dirty codex根与所有证据，不建新worktree、不Git写入/删除；用apply_patch账本代替提交型技能脚本。代价：这些是未提交工作及隔离快照，不是GitHub新版本。
3. 两个未知ObjectDB实例未复现时不猜修；音频已有Shutdown/自有流释放，不把drain差异当根因。代价：历史资源资格未关闭。
4. D03旧E路径改为当前G媒体根，保留历史含义。代价：仅文档纠正，不执行迁移/恢复/删除或暗示旧档还在。
5. 接受3项Important，唯一修复轮的6反例RED→GREEN；未做二审。代价：合并pre仍FATAL，整个计划/可靠性资格不能声称全绿。
6. M01–M08改为源码已接入而非待实现；当前models不覆盖短喷专项。代价：静态事实不能代替同版专项、全路线或真人手感。
7. 对象计数多出的1未取得身份，先有一次成功自有Node创建/Free基线再测拒绝路径；最终两条实测0。代价：不推断旧未知ObjectDB或engine元数据已修复，不改全局GC/cache。
8. agent-reach既有CLI包装的引用/运行目标失败后，只读官方网页检索回退；不安装/登录/更新工具。代价：相似上游记录只作线索，未把候选PR当可用补丁，不运行MRP。

审查set-aside逐项root裁决：当前post-Combat顺序由真实优先级0→100及elapsed>0支持，但未来对称合同保留为Minor；另次/零ID/重置/非有限时间已由资格和结束ID计数约束；当前Descent的实际Guidance.Refresh/process优先级证明本夹具更新，不推广到Basin提前返回；只读回调不发指令，晚错I2必须修而非忽略；正常cleanup不覆盖异常分配，I3已补；卡片字段/字符串不是像素；canonical严格门不放宽；所有历史资源/完整路线/GL/硬件真人均仍开放。

**延期Minor M1：**后续增加与Guidance对称的Combat实际优先级/启用合同，以及起手尚未Tick的明确阶段负控。当前不是实际提前采样故障；代价是未来调整Combat优先级时的保护不足。v159生命周期/no-slot与非identitycanvas两项延期不因本轮关闭。

## 保管、容量与当前交付边界

18案88对现存清单相同，2对bin清单因构建失败未产生；实际封存96/96bin匹配。8492份现存选定实体（源8290/helper202）逐SHA与两侧路径一致，是有界选定保管，不是每案1857源实体全备份。两个build FAIL的engine=null、无engine合同/wait/native或fresh bin，保留guardFailures，不回填冒充。

最终1857源/897C#逐SHA匹配，0新增/删除/漂移；Debug6bin/canonical匹配最后清单。HEAD/index不变；16个实际engine身份均已结束，另2个从未启动，不误算为终结进程。

```text
source-after A107094340A0599D8ECE767407EA3D97C269CC68C88B072ACDFFD8733C433F48
helpers-after F1C71811931AF6F3BFE3F6977F711323FB7F00126B37F4874A6BBE2A61FCEBDF
fresh-binaries 8CB41933779BFE888E4688A2220812D19E90C05F3D6325304D8AAB80AD06D09B
canonical 773B53587F82F7E8F14E8F4508B48E03E6019B113C3046708D02813968A5A5BD
HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6
index 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C
```

一次手动媒体检查2026-10-29T15:31:05Z：2,012,371bytes/21文件，Notify=false，无新媒体。未开窗/OS输入/付费/collector/自动化/Git写入/删除/真实存档语义访问。上一窗口仍NOTSEEN。完整原十一项、正式美术音画动作UI、正常Main/Explore、资源稳定性、硬件真人和发布均尚未完成。
