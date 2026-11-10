# v115 · 乘梯真实格挡与后续战斗边界

状态：有界路线验证推进，原十一项与 Task15 继续 ACTIVE；不是整关通过或新游戏版本发行。工程仅 `G:/knight/Knight`，不涉及另一个 UE 项目。用户最近游戏显示结论仍是 NOTSEEN；单独的来源说明网页预览不算游戏显示。

## 实际改动与红绿证据

只改两份测试 C#：`tests/integration/BasinCounterweightRuntimeTests.cs` 与 `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`。生产物理、敌人、生命/能量、地图、美术、音频与输入规则均未改变。

- 被保留的旧失败：Explore214019 在 ride 中，Bell 首次 Telegraph 为 physics4584/sequence4555、剩余0.75秒；physics4629/sequence4600 的 attack2 命中致死。raw A/D/W/L、sampled L、active 均 false，能量1、热量0。这证明原 ride 没有防御命令，不是能量耗尽。
- ride 的每个原 Step 现使用原 `.35s Telegraph/Attack` 条件，按实际 Hitbox 来源双向举盾；刷新真实键盘方向，采样到朝向后释放自动 A/D，不持续走出甲板。转入后续阶段与清理时配对释放驱动内部盾意图，而不只是抬起 raw L。
- 只有原 Exploration passenger 分支要求一次实际左侧格挡；Main 和其他调用默认不强迫发生碰撞。原200帧、中心2720–2800、原生支撑、生命值、同 Bell 身份/HP与所有后续预算保留。
- 实际正证据不是“按过L”：同一 case 中 `Hitbox.Connected`、Normal正伤害1、盾 active、向左防御、HP1→1。候选与最终 Explore 都得到 contacts1/guarded1/left1；随后真实画廊落地135/150、返回96/109、完整PistonTwo停稳通过，原 Bell 仍活着且HP6。
- 最终观察器 rows10、omitted0、fault=none；上限48是变化采样，不是完整逐帧轨迹。`before-command` 已移到实际发键前；没有新增等待帧或物理查询。

未宣称右侧命中也已获得新的原生正证据；代码不再按左右筛除，但本轮实际新正例来自左侧。

## 后续失败，不能用局部通过覆盖

最终 Main 仍在原 physics7114 死亡后发生正常检查点恢复，首先被3117.98px位置变化断言否决；不是本轮证明的新传送缺陷。

最终 Explore 在后续 generic Bell fight 的 physics5698、loop816/900 被同 case 保留的 wave Bell 的 localAttack9 击杀，触发零恢复否决。此时L/active均false，能量0.8613297、热量0.4108323、overheated=true；后者存在过热迟滞，不能误说成能量不足。战斗入口physics4881是能量1、热量0、未过热；这排除了“直接遗留ride热量”的说法，但不排除先前站位等其他因果联系。

只读定位发现：通用中立反击要求完整胶囊支撑、两个fresh neutral样本、距离55–64、垂距≤46、朝内水平瞄准且无危险。移动升降台并非漏枚举：实际曾返回其ID并有11/23个中立样本。后续Bell x2766.13的右侧60px目标x2826.13超出原完整甲板中心2720–2800，日志support0，与站位冲突一致；有限日志尚不能证明后段每次失败条件或首次过热的完整因果。

下一步先记录原900循环内 readiness 失败mask与首次overheat变化，复用已有求值，不新增resolver或预算；再根据真实支撑/反击条件修正驾驶，不能先放宽55–64、甲板或HP合同。

## 全部七案与最终同冻结组

所有目录均位于 `G:/knight/验证证据/october13-stable-claw/`。七案build exit均0，build/engine均非timeout，guardFailures均空；只有最后五案属于同一冻结组。strict 3/7，最终组3/5，外层batch97504 exit0仅代表批次已结束。

| 实际目录 | Engine exit / ms | 结果 |
|---|---:|---|
| thruster-route-exploration-diagnostic-20261025-214019-760-23b6643c82194c32a8d49f7462c2479f | 1 / 85932 | passive RED，原ride致死 |
| thruster-route-exploration-diagnostic-20261025-215053-165-ee67493c9ff046bd8ae257e300315813 | 1 / 102639 | ride局部GREEN，后续fight FAIL |
| thruster-route-main-diagnostic-20261025-221142-920-e2fc4e88b020496c879647350255920a | 1 / 131572 | 最终Main FAIL |
| thruster-route-exploration-diagnostic-20261025-221425-285-14f132a42fa2463794a20c255a7e871c | 1 / 105224 | 最终Explore FAIL，ride/往返通过 |
| prologue-pre-20261025-221640-828-2b6b4f3e95ea4ffeb3e6a5e8713c0a77 | 0 / 159624 | 最终pre PASS |
| prologue-post-20261025-221950-580-32d061ed8f184ecbbf3a458032c67aa3 | 0 / 217058 | 最终post PASS |
| models-20261025-222400-844-4fd7603527474317a845e82478cd3475 | 0 / 520 | 最终models/普通Debug恢复 PASS |

post 原75项index0–74完整150次enter/return、owner8次nodes1→1完成；原4个WARNING与3个scope对账，未增加豁免。实际cap300000ms，不能称原180000ms通过，也不能用原生耗时当硬件帧率认证。此次安静的pre/post不关闭历史Handle/FATAL。

## 一次独立审查与全部边界裁决

fresh review C0/I0/新增M0，无需纠正性修复或第二次审查。既有 energy-unwind summary Minor继续延期：早期异常可能缺诊断汇总，不能把新ride日志算作关闭它。

以下为审查者15项Declined-to-judge的root裁决，均保留代价：

1. 最终冻结批次：按上述五案真实终态，不把外层exit0当全绿。代价：错判会伪造验收。
2. 完整双路：Main/Explore仍FAIL，不关闭Task15。代价：跳过真实通关障碍。
3. generic-fight因果：保留冷入口与死亡标量，但不把支撑/过热推理当完整根因。代价：错误调参与掩盖站位问题。
4. 右侧新原生正例：尚无，不能由代码对称性代替。代价：漏掉方向边界。
5. 其他Bell时序/交叉火力/几何：仍需各自实测，不扩张本轮证明。代价：遗漏组合失败。
6. W/preview排序：只证明此交接真实成功，生产排序未改、不认证全局。代价：其他方位仍可失败。
7. 遮挡/receiver：无新增授权性resolver，不重签所有first-hit合同。代价：漏非法抓取或遮挡。
8. Intro→Playing：准入代码未改，原门保留，不以局部重签所有夹具。代价：准备态假通过。
9. callback/deferred：新局部配对清理及同体断言保持，不替代全生命周期矩阵。代价：卸载后的旧回调。
10. LoadLevel双异常与自有清理：本轮未改，独立要求保持。代价：丢主错或漏根节点。
11. 有界观察器：明确48行变化采样、omission/fault，不能当完整轨迹。代价：诊断遗漏。
12. 历史资源/全部内存/FPS：继续OPEN，没有新所有权修复、GC或缓存策略。代价：漏崩溃或虚报性能。
13. energy-unwind Minor：延期，不在本轮借机修补。代价：早失败时诊断信息不足。
14. 全树/二进制：以另一次独立保管审计补足，不能拿窄审查代替。代价：跨版本拼接。
15. 原十一项/正式视听相机/硬件真人/发行：仍ACTIVE/NOTSEEN，未展示游戏或制作新正式资产。代价：提前交付未验作品。

其他root裁决：在用户选定的dirty codex checkout继续，root唯一写入/构建/原生；所有源/帮助器/文档在native及后续保管审计期间冻结。旧handle9220确已缺失，并由215053的终态与进程清查补证后才继续，未因观察超时重启。代价是依靠逐案快照而非新Git提交归因。没有task-start/新BASE/task-done/提交/推送/工作树或删除。

## 保管、实物基线与存储

独立只读保管审计结束后才写本文：7案35对/70份清单、42封存二进制、3702份before/after输入实体全部对应；missing/drift/malformed/duplicate/extra均0。final5的源与binary manifest一致；当前1844源/C#892与Debug6/6逐实物匹配finalmodels。各案user metadata109、continuation0，仅读清单，不读真实存档正文。14组build/engine PID、contract argv与wait/provenance一致。

相对v114只有上述两测试C#变化，无add/delete，生产源不变。重要纠正：v114 models清单虽记录Basin，却没有复制该实体；Basin的真实旧实体取 `thruster-route-exploration-diagnostic-20261025-210826-734-0c0ee72c80934b94a1808b871c500c91/before/tests/integration/BasinCounterweightRuntimeTests.cs`，SHA为55B72433EBE1BCC6EBB81E2DD3DAA1E0AF0E9443C233A9F738B4AAFEDDE7E218。Main旧实体仍取v114 models的before，SHA09720BC51D3853B2C4BE519B01BB7367394170C24F12CBC3A14CB69B3E9980FC。不得把不存在的副本称作物理基线。

- 最终source manifest：`CD0DBCCAA6DBA79E59D465DBA85AD273F0FB9DD0F8EAD2FCB97F44FB4A6E31F9`。
- 最终binary manifest：`727815AECA6471612FB408D30612C7327787C467FD60B0FC26F01E925CFC8C55`。
- Basin：`EB844A9749C76B24C0DFD2246D617C3EEF43251BCD6AD00D0EC492718734CDFA`。
- Main：`E587804AD54121B14B50051F799F226BC6B8EF2E744095E2B074AC4781EA0E2D`。
- owner：`309DD6C42FB5E96F27390711FF91D2D419890CF17BA256869E2D2A22961255F2`。
- Git index：`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。
- 音频布局：`CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A`。
- HEAD：`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`。

所有共同9项helper/runtime及pre/post各自第10项均与v114对应案一致。所有自有Godot/dotnet已终止。仅一次既有手动媒体检查UTC14:38:22.8011100：Current860284、Baseline3508、Growth856776字节，Notify=false；没有新游戏媒体/付费生成/自动化或删除。

文档写入后再次核验：报告七案目录/exit/ms/timeout/guards与实际provenance全部一致；两C#、owner、index、音频布局哈希不变，`git diff --check` exit0，原LF→CRLF提示170、其他诊断0，自有Godot/dotnet仍0，目标仍ACTIVE。

## 之后的真实物理测试准备，不是已实现改动

legacy敌人/机关“命中→效果→收回、不拉入内部”的批准规则与生产代码存在待测疑点：未安装实际tether，却可能进入通用Released速度投影。尚无新的原生RED，不能宣布根因或已修复。

可复用 `ThrusterPlayerRuntimeTests.FreeFixture(false)` 与保存BellChain，均在入树前正常布置：Player初始(0,-500)、机关根(300,-380)、原抓点(300,-460)，门原几何保留。朝向案真实D十个采样后松键K；非朝向负控真实A十采样、D一采样恢复朝向后松键K。如实记录反刹，不写运行中Player姿态/速度、不关Motor。必须先实测Right锥、620射程、入射方向与合速限制；前提不满足就报夹具失败，不搬目标凑绿。-110/-75只读观察以adapter application0→1挑选真正命中tick；K按下tick仅Aim，不是命中。排除碰撞/receiver脉冲后才断言无未授权caster速度改写，保留真实地形release投影和所有原自动物理回归。此准备不替代眼前generic Bell战斗、资源句柄和相机验收。
