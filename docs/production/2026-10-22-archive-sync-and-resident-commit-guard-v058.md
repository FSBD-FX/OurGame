# v058 · 档案范围同步与常驻出生提交边界

这是内部开发进展，不是新试玩发行。完整序章1–11与人口Tasks1–4均ACTIVE。唯一资源总目录仍`G:/knight`，工程`G:/knight/Knight`；用户询问Godot／UE不构成迁移批准，本聊天继续现有Godot工程。

## 本轮实际改动

1. **修复档案区源码／保存场景范围不同步。** 已批准源码宽704，保存场景仍640。独立期望先改到704，实际原生读取保存marker得到640而失败；随后只复制生成器输出的3个字段，不替换整个重序列化文件。原283节点、29个Descent碰撞、世界127实体、外部资源ID和其他字节均保留。没有推广候选143实体、改Archive／Abyss重叠区域的路线优先级或增加不存在的readability wash。
2. **拒绝尚未提交／仍排队的常驻出生上下文。** 真实`SectorMounted`在CurrentSector、玩家位置、镜头和旧区退休提交前触发；此前出生上下文仍可获准。`TryGetResidentSpawnContext`现在在`_transitioning`或`_queuedTransition.HasValue`时拒绝。其他屏内突然出生、玩家预测保护、旧26生成点、碰撞／支撑、HP／epoch／owner检查不变；未新增自动出生调度器。
3. 增加实际保存Level的边界反例和正控。没有测试写入CurrentSector／玩家位置：通过真实checkpoint接口转神殿，挂载回调调用公开ResetForNewRun产生真实排队请求；实际Flow.StartNew及标准4个physics帧后，同一玩家的已提交上下文重新获准。测试演员出生0，不冒称50只入关。

生产文件：`scenes/prologue/sectors/CanyonDescent.tscn`、`scripts/prologue/seamless/SeamlessPrologueWorld.cs`。测试文件：`tests/unit/PrologueCanyonStructureTests.cs`、`tests/integration/ResidentLevelLifecycleRuntimeTests.cs`、`tests/integration/SteamMetadataSynchronizationTests.cs`。未改原Snapshot算法、抓爪／推进器力量、玩家体型、危险范围、输入或原26遭遇。

## 先失败、后修正的证据

所有case位于`G:/knight/验证证据/october13-stable-claw/`；每个保留原始stdout／stderr／external日志、build／engine合同及wait、源码／helper／用户文件／二进制清单和选定副本。

- Archive stage：`resident-archive-metadata-20261022-114244-473-662911543ab04f82b5fd5f33b6b16892`。实际283节点、2节点3字段、非允许字段深层语义、持久化groups／connections／嵌套场景链接、6个原子拒绝、碰撞负控及原127小地图序列字节／revision／边界顺序检查完成；**退出255 Shape RID，严格FAIL**，不能把stage称为合格或资源修复。
- 保存数据RED：`canyon-structure-v2-20261022-114317-272-1846402c7a1d40fd97159b753e6c6381`，native1／880ms，实际`expected704/actual640`；不是旧source常量失败或编译错误。
- 精确三行：L220 CompositionBounds宽640→704；L1310 marker中心X320→352；L1311 local_bounds宽640→704。冻结before与修改前SHA一致`E091C51C8D46ED117FB6020DBED64750914B72D9544856E179A7CE0ACB9D1CD6`；完整生成artifact SHA`4F1D15FD897EAD2D6222D645A632DF6EFFD106659B84671E053CCCD678EC5FD0`；发布到内部canonical的SHA`E8419EA42A2731B554B50708879ABA36F04F690BE9F5605A6E5C0CDA1998F59F`。独立审计预先计算的三行替换结果与实际70710字节逐字节相同；1618行其余不变。stage的20个ext-resource重编号没有复制。
- 提交边界RED：`resident-level-lifecycle-20261022-121008-143-8600faf8cb4348f3bc3d9bffeaebc3e3`，native1／1444ms／guards0／无timeout；真实2次挂载回调全部准入，排队上下文仍True，命中新增边界断言。没有Handle／RID／编译错误抢先失败。
- 提交边界GREEN：`resident-level-lifecycle-20261022-121234-990-548252c862204ce6bfdb7efa085bbb5d`，native0／4436ms，2回调准入0、queuedFalse，实际physics排空后同Player正控通过。原newrun／validrestore／invalidsave、3种真实死亡恢复、HP保留、原子出生及重入／owner替换／屏内出生拒绝等同phase控制全部执行，三raw安静。

此前Snapshot冷wrapper假说已被对照否定：113720中无Snapshot的冷handoff同样留下Shape，不能把它归因于Snapshot或据此Dispose借用资源。现保留2个显式自有Variant／Shape／PackedScene的暖图控制；冷handoff失败原件保留在113532／113720冻结case。没有新增GC／cache／drain／engine补丁来掩盖资源失败。

## 两个冻结版本，不能混算

### A：三字段地图同步后、提交guard之前

14门12PASS／2严格FAIL；Archive255、Steam266 RID。源清单SHA`9336CD8A3AC42CA269BE20E0C44F2D1E21C174F51E70FB5122650A0A6C87B1E2`，DLL`DFEB4E5626955E70FB2E2E66E3610B4EF1972902EEF2E2563D26460F7DA27EE1`。

实际结构、Archive上下层、Gear侧壁、Pendulum、Frozen、parent、联合census、allocation、键盘、动作及完整pre／post通过。A的联合census：50真实Ready、41完整巡逻＋9静止炮台、720唯一physics、489落稳走廊复核、无治疗；Archive612观察／620physics，Gear289／579。均是隔离候选，没有目标／战斗／正式出生，**B没有重新执行这些通路或census，不能跨源续签**。A post147574ms保留75原案例、8次owner与限定4条负测试警告。

### B：最终内容源，同源14门10PASS／4严格FAIL

全部native exit0不等于全部PASS。四项退出Shape RID均由原严格raw规则否决，无日志豁免、安静重跑或资源问题关闭。

| 阶段 | case时间／唯一后缀（完整前缀为阶段名） | native PID／ms | 严格结果 |
| --- | --- | --- | --- |
| resident-level-lifecycle | 121234-990-548252c862204ce6bfdb7efa085bbb5d | 29892／4436 | PASS |
| resident-state | 121401-205-af43023070de47da892b4e6698e6b085 | 36196／523 | PASS |
| resident-lifecycle | 121427-967-6a7005cce02840c1972231c9ba8e407f | 45540／1316 | PASS |
| resident-save-compatibility | 121455-045-3376666eab9148329cba3574946d7311 | 43084／2617 | PASS |
| canyon-structure-v2 | 121523-608-148a7e1681954b55af343ebb1eacdb11 | 51876／1319 | PASS |
| resident-parent | 121551-049-79559cdf8bd444a282ac4a331ad5aa2a | 35064／2159 | PASS |
| resident-scene-binding | 121620-283-1834714eec804887bc9e280ef55dc5a1 | 14928／7814 | **FAIL：137 Shape RID** |
| resident-subtype-profile | 121654-710-f88d7e85964841799eaa1091983bbe1e | 17128／722 | **FAIL：31 Shape RID** |
| resident-archive-metadata | 121729-716-64101708f2594fd88d6e82045dc21baf | 47904／4110 | **FAIL：255 Shape RID** |
| resident-steam-metadata | 121807-064-f80350e8a9e54de4b41135e39db5bca7 | 42512／4750 | **FAIL：266 Shape RID** |
| keyboard | 121839-296-8f769ff130094122b11136d4460b5e35 | 50328／9692 | PASS |
| actions | 121917-799-5b39481393bb478c90cdad96f464f267 | 20844／7099 | PASS |
| prologue-pre | 121956-419-a89451b0f4284791a49cbaf88d41080c | 42816／99762 | PASS |
| prologue-post | 122205-119-a94dcd7ac90946a5a5de184b13920579 | 50612／147191 | PASS，限定4条负测试警告 |

case全名格式`<阶段>-20261022-<时间/后缀>`。复验命令使用固定pwsh执行`.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1 -Phase <阶段> -SourceRows 1710`，无ExpectedFailure、诊断符号、movie或时间加速。所有source／helper／docs在所属原生consumer存活时冻结，结束后才改文档。

B post确有75项原案例／零退役、8次主场景owner实例／释放、nodes1→1。四条警告来自故意负测试：seamless1、room-profile1、flow-registry2，三个raw中的实际来源／scope已逐项核验，没有额外ERROR／FATAL。这是程序回归，不是正常Main／Explore连续试玩、零警告正常游戏或显示60FPS验收。

独立B14保管审计：70组manifest一致，4622份选定源码副本、142份helper副本、84份binary副本及28个build／engine PID／argv／contract／wait匹配；全部guard0、timeout0、14次quiet build。不是声称1710文件每案全部复制。

- 当前1710项源／880 C#；B源清单SHA`0BE6889BBC2FC04D42D69AC7CB3B8049FFE8CA9FAE6947E732B39CB91EB7F445`。
- B DLL SHA`D34433AFD19257921A4E560DEDB623075EF81AF3BA4DACE0F859D30656E4F57C`。
- 六binary清单SHA`CFCD05AA92FF0BBF8CB155344DBC8E3EB0A33549C1D7F4B5706EDBBD89C0F1AC`。
- 受保护用户文件109行清单SHA`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`，只做原hash／存在性守卫，不读取真实存档语义或改写。
- 普通构建无诊断DefineConstants；DLL内MetadataResourceTrace类型0，所查identity／leaf-operation／carrier-import诊断标识及日志标记不存在。
- 主代理在B终态后逐项读回当前1710源，和冻结清单差异0。相对A仅World与Level测试两项source hash改变；正式地图仍三字段同步后的127碰撞。

## 判定、风险与下一步

上一目标轮PROGRESS，本轮也有实际权威源改动与两条RED→GREEN链，不是仅重述状态。Ruling：独立地图数据错误可在内部修正，不以退出RID失败阻止所有其他开发；相应失败仍记录，严格最终交付门不放宽。Ruling：出生权限取自已提交且无排队的世界，不在早到的SectorMounted内直接出生。两项的错误成本分别是隐藏序列化漂移、永久拒绝合法出生；精确字节／语义／结构回归与实际排空后正控覆盖这些风险。

尚未完成：正式新居民0/50、全50Placement仍null、24canonical子型Unimplemented，正式127／候选143。原26＋新50＝76普通＋1Boss、骨骸10／幽灵10、连续围合地形／机关、真实攻击与Returning／旧26共存、最终全关视听、正常双路／存档／真人／目标硬件显示60FPS、Windows解压包与获授权新版本GitHub归档全部继续。137与历史136/154不是资源原因的定量证明；安静的Level／pre／post不关闭历史Handle／FATAL／音频等失败。

后续入口：把已获原生几何／活动证据的完整50与24保存子型接入真实Level的唯一owner，先完成可复核的地图／出生清单与实际战斗、返航、旧26共存，再正式接线。初始准备应在真实隐藏准备阶段完成；流式／恢复的出生协调须等提交、真实物理边界与reset完成；保留可见范围及玩家保护，拒绝槽位不得靠临时隐藏世界或撤guard取绿。此处只是执行顺序，不替代未做的实现或缩减最终范围。

本轮没有新最终美术／音频、付费、前台输入、Git写入、删除、自动化或新媒体，也未发布ZIP／标签。历史素材表与替换接口保留。关键媒体根`G:/knight/过程图稿与视频`手动Check读回4838B／基线3508B／增长1330B／Notify=false，不重建旧归档或自动化；G盘余量约201.85GiB（本轮检查时点）。

文档更新后主代理再次核对：当前1710源与最后owner的9个helper、6个Debug二进制hash均无漂移；diff-check0、无自有Godotconsumer。HEAD仍`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，index SHA`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`；目标工具读回ACTIVE，无budget，不标完成或blocked。没有继承旧E根或另一个UE工程。
