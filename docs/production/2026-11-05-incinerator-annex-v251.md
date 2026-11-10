# v251 · 完整熔炉附室接线与当前验证

状态：制作增量已接入，当前同源16项终态为**11 PASS / 5 FAIL**，不是正式交付/整套通过。原十一项与R-I/R-C/R-P继续ACTIVE/OPEN。工程仍为G:/knight/Knight，未切换UE。

## 实际内容

盆地在原世界7680×4320边界内向东扩1280像素，旧五区offset、检查点、12挑战及76普通敌人/9残片/1Boss配置不移动。新增720×276实际致死池、实体底板、安全上桥、东岸及两侧单跳服务返程。使用真实GuardedDamage/Health/Died→DEATH RESET，明确不可举盾硬挡，但保留无敌帧/正常伤害拒绝规则，不直接改HP或调用复活代替入池。

原SteamTop旁32像素旧Ascent恢复残段会在仍踩地时触发FALL RESET；实际失败已留证，生成器接缝和保存场景只校正该残段。新版图将下SteamValves→附室与上CounterweightLift↔附室区分；只有精确具名的上路返回被允许，不放宽Boss/出口门禁。

场景由生成器detached打包、逐节点回装校验，父场景与小地图目录成对接线。输出digest、父输入leaf digest、当前源/二进制/helper及目标exact-before均受检查；旧产物无digest的不追认。两次pair和原件都保留在[本轮工作记录](../../.superpowers/sdd/2026-11-05-incinerator-annex-v251/progress.md)。本地场景接线不是GitHub或外部发布。

## 已实测与未通过

当前源码1894个受检输入。最终同源五专项：原结构及33负控、实际保存附室/父定义/小地图一致性、熔炉机制、连续上路东岸返回/独立西岸返回、实际入池死亡后营地同角色恢复，均严格通过。均为受控起点或专项，**不是标题自然游玩、全敌人清场、正常速度视觉或用户认可**。

宽回归当前发现并保留：

- 连续附室aggregate：上路/西路实际完成后，再次检查Ascent时出现`Godot.PackedScene`已释放引用；独立两案通过不能覆盖此失败。需修测试资源所有权并重新验证，不认定为旧匿名finalizer故障已归因。
- 存档兼容：旧Grip8测试把“当前目录”固定为144，实际已158而严格拒绝。生产codec另外确实只接受三个旧目标：有效旧核心进度保留，但旧地图探索将被清空。当前不得交付为兼容通过。
- 机关回归：旧断言固定143 structural solids，实际158而失败；不能删断言跳过，须按真实新增14结构与既有baffle同步，并保留危险Area不计入地形的验证。

原居民geometry本次也严格失败：旧raw terrain body预期143、实际157，未进入原端点复用控制；退出另报222个Shape RID泄漏。不能只说“旧数字不符”、不能声称匿名finalizer已修，也不能把157个body与158个shape混用。

当前普通pre严格PASS/native134116ms；post严格PASS/174469ms，均保原180/300s预算。历史v249/v250绿项不充当本版通过。Main899、原geometry匿名资源FATAL及正式视听/动作/硬件/人工验收继续欠账。

当前Main仍在原摆锤战斗899前未完成清场：12攻击/11连接、enemy HP1、player HP2、恢复0/0，energy约.18；没有改HP/能量/899窗来筛绿。当前Explore严格通过/310177ms，真实normal plan、Boss及实体支撑出口、恢复0/0；这条既有路线不经过新附室，也不证明全76敌人/全机关或真人视听验收。

| 当前五专项 | case时间/短ID | native毫秒 | 严格结果 |
|---|---|---:|---|
| 原V2结构+33负控 | 120806-796-49722143 | 1577 | PASS |
| saved叶/父/小地图+具名图正反控 | 120857-041-00f12825 | 1611 | PASS |
| 实际致死体积与生命周期 | 120938-872-b3866c00 | 6470 | PASS |
| 连续上路东返/独立西返，均单跳 | 121023-887-bfd6bbe4 | 26702 | PASS |
| 实际入池/唯一死亡恢复/同角色移动 | 121121-341-8fda794c | 9525 | PASS |

| 当前宽回归 | case时间/短ID | native毫秒 | 严格结果 |
|---|---|---:|---|
| 附室连续aggregate | 121242-957-22cfd3cd | 44475 | FAIL：第三fixture已释放Ascent PackedScene |
| 爪模型子集（不是全部模型） | 121415-915-1aac0d41 | 521 | PASS |
| 键盘 | 121452-180-b7953a52 | 19969 | PASS |
| 居民/探索存档兼容 | 121552-267-6c96cdad | 7676 | FAIL：旧144目标检查 |
| 组合机关 | 121630-396-cd919b0a | 22018 | FAIL：旧143→实际158结构数 |
| 原SteamTop边界 | 121728-245-a62e6180 | 2000 | PASS |
| 原居民geometry | 121800-417-bc5dcc7c | 1544 | FAIL：raw143→157，另222 Shape RID泄漏 |
| 完整普通前段 | 121832-987-61ffedf9 | 134116 | PASS |
| 完整普通后段 | 122123-407-598a27f0 | 174469 | PASS |
| 正常Main | 122456-434-7a020a9a | 149559 | FAIL：原899未清场 |
| 正常Explore | 122803-949-cf3f49c0 | 310177 | PASS：Boss/支撑出口/恢复0 |

所有case在`G:/knight/验证证据/october13-stable-claw/`按phase+完整时间+UUID保管，原生stdout/stderr、provenance、等待结果及五组保护清单均保留。两个当前批65578/84498均终态，后者exit1且明确failures5，不把脚本catch后状态当全绿；不重启旧handle。

终态保管核对：16案80对source/helper/user/continuation/binary前后文本，差异0；全16案源清单与binary清单各唯一一份。当前1894文件SHA及完整文件集合、6个binary、14个helper独立重哈希均0差异。普通用户路径没有因本轮测试改变。关键媒体一次`-Check`成功：7,427,750B/86文件、Notify=false，未达新增20GiB；无新图稿/视频，不重建自动化。

## 裁决、审查及风险

仅一次fresh增量终审C0/I1/M1。I1发布绑定已实际RED→GREEN，两正控/三负控均在正确边界；M1临时overlap collection释放一致性延期。全部13项declined与设计/整合裁决见[完整裁决记录](../../.superpowers/sdd/2026-11-05-incinerator-annex-v251/final-review-v251.md)和ledger；未二审，后续整合修改不追认为原审查覆盖。

原池/桥/炉壁目前为程序表现和正式素材替换入口，不是精美手绘资产通过。用户U-20261105-01已明确暂找不到旧V1身体/披风原图，后续重审现有候选；停止扩大旧原图搜索，旧批准不转授。原三段误给的视频仍作废，不采纳。

| 本轮表现资源 | 当前来源 | 资格 |
|---|---|---|
| 炉池/炉壁/余烬边界 | `scripts/mechanisms/CanyonIncinerator.cs` | 程序表现，可替换；非正式手绘批准 |
| 上桥、池岸、双侧服务路及警示 | `tools/build/prologue/CanyonContentBuilder.Incinerator.cs`及生成保存叶 | 空间/碰撞专项通过；正常速度视觉未验 |
| 雾下实际池边保护cue | `PrologueFogController.cs` / `FogRecyclingReadabilityOverlay.cs` | 已接同一危险边界；像素/人工审美未验 |
| 主体/披风旧V1原件 | 当前找不到限定原图，依U-20261105-01后续审现稿 | 不跨稿转授批准、不新增付费生成 |

下一优先：[旧144→新158探索迁移设计](../superpowers/specs/2026-11-05-annex-exploration-compatibility-v252-design.md)及独立历史夹具；连续fixture借用场景资源所有权；完整当前回归/正常主线瓶颈。该设计尚未实施，不能称旧探索已保留。继续仅无窗口/隔离存储，无GUI、系统键鼠、付费生成、新媒体、额外崩溃采集、自动化、Git写入、真实存档操作或删除。
