# 旧教学几何的版本隔离与后段回归 · v031

2026-10-19，Asia/Shanghai。完整1–11目标ACTIVE，Task6/7与最终交付未完成。[v030](2026-10-19-death-recovery-lifecycle-and-checkpoint-v030.md)死亡恢复与五门4GREEN/1FAIL的原件保留；[v029](2026-10-19-user-confirmed-title-visibility-v029.md)仅确认标题可见，不扩成长测授权。本轮全部后台、无新窗口或媒体。

## 事实、推理、风险

事实：原失败不只是单个坐标错。`PrologueCanyonLegacyReplacementTests`加载新版正式五区，却要求旧五区偏移、十一教学段、三种旧Descent目标、三个Ascent助力目标与八个方向标记。原生被动对照确认：正式Definitions与CanyonWorldLayout一致，四对正式Marker距离为4/4/170.04706/4；旧偏移将Basin从正式y3600放到了y1440，导致seam2先失败。

推理：这组原Task8合同实际描述冻结的layout-one教学几何，不应拿其96距离和十一段规则强套正式V2。按已批准[连续世界计划](../superpowers/plans/2026-09-09-continuous-canyon-world.md)的独立sample兼容边界，将此组输入改为`BasinSampleLegacyLayout.FrozenScenePath`，保留原五区顺序、Offsets、96及全部十八方法。75个历史case名/可执行delegate不删除、不退役；更新执行说明，明确这组是旧几何兼容，不再冒称正式V2验收。

风险：切到旧几何不能替代正式地图覆盖。新窄入口同时独立重跑完整V2结构合同，post原正式结构/真实世界生命周期/主场景合同及正常Main/Explore入口未改。正式V2本来就独立断言那对170px Marker的保存literal；Marker距离不是流送共享边或真实胶囊通行的等价条件，不为凑96移动Marker、恢复切口或地形。正式真实通路与正常双路线仍OPEN。

## 实现边界与原生结果

只改测试/严格入口：新增有界四行实际Definitions对照，原方法独立执行入口和真实依赖副本；十八方法主体除资源提示文字外不改。原生RED发生后才修输入来源；没有改生产Player、力学、地图、流送、HP、时间预算或日志规则。误命名的首个`formal-seams`仅保留为历史RED，后续入口更名`legacy-geometry`以准确标资格。

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw/`。五日志顺序为build.stdout/build.stderr/engine.stdout/engine.stderr/external，数字为行数。

| 案例目录 | build exit/ms | native exit/ms | strict | 实际范围 |
| --- | --- | --- | --- | --- |
| thruster-formal-seams-20261019-073124-925-d8874dbe399f4c3b9840c630b4ffce56 | 0/29478 | 1/1124 | RED | 原96/旧Offsets对正式场景混用；四正式事实实测，seam2原错误保留 |
| thruster-legacy-geometry-20261019-073549-636-a475dc8952c7403893f5be8fef94085a | 0/31146 | 0/1293 | GREEN | 原十八方法逐一完成＋另行正式V2结构；不是V2胶囊通行 |
| prologue-post-20261019-073811-674-a6f14a0b20424a8e96794bbdf223a2a3 | 0/30208 | 1/130681 | FAIL | 已越过原接缝首错；新首错为P1死亡恢复时序，另有真实Handle异常 |

各案build0警告错误、无timeout、guards=[]。post原cap180000ms不扩大。首RED日志7/0/6/16/22，唯一原seam2 ERROR、无附加退出资源错误，external完整。窄GREEN日志7/0/27/0/27，stdout7–24为十八原方法返回后的收据，25为独立V2结构：5区/14节点/12挑战/18有向边（16逻辑边）/12组/5视觉层。三raw无诊断，external逐行等于stdout；正式Marker事实仍为4/4/170.04706/4，不伪称正式96全过。

窄GREEN独立审计：1695source（含296 imported）/9helpers/109保护/continuation/6binary前后守恒，179份actual selected每侧（174source＋5helper）全部匹配。首RED171份每侧（166source＋5helper）同样匹配。五正式场景两案实际副本相同；五冻结场景两案清单SHA相同，GREEN实际副本前后匹配，但RED未保留这五份实际副本，不能称两套冻结实际副本直接比较。

```text
RED source DC868CEB5AEB8056FE689B80410E9FFE07E0881C2A84339F026440901CA616BA
RED helpers 3FD99D4B17200F15C82187EA1B1F0E99208E56420E3C767992CDCD416AEAA01E
RED binary 05B34CB16841457B24394D257319ED50775427E9AB10EBF712A028A8092D97D5
GREEN source 649B01881393FFC14EBC3CD8B92B4B4EC8592BD8FD7DC4854449E571A9511384
GREEN helpers 326D5F526219F5434CED1E9987B64D7269AE805C2330AF42EB7066F48C080861
GREEN binary 0CFC12E3BF1F4095ADA6EE0F5C68348B0ED8B910F6CF98E1F2CEB2020452ECC2
post source 649B01881393FFC14EBC3CD8B92B4B4EC8592BD8FD7DC4854449E571A9511384
post helpers 46E8D1F7295E58B79A81A9CE585F816A43604730831CEB0434A06266B6BBA84D
post binary 0CFC12E3BF1F4095ADA6EE0F5C68348B0ED8B910F6CF98E1F2CEB2020452ECC2
```

窄GREEN与post完整source/binary相同，但中间补了post实际依赖保留、helper库存不同；这是两次独立冻结，不拼成一个全阶段同冻通过批次。post未通过，完整1–11本来要求的最终同版全回归仍未取得。

post终态独立审计补齐：五日志7/0/355/124/479，external完整合并stdout355＋stderr124、漏尾0；1695source/9helpers/109保护/continuation/6binary前后守恒，178份actual selected每侧（173source＋5helper）全匹配。三raw共2WARNING/2ERROR，无FATAL/RID/ObjectDB。第一ERROR为P1断言，第二为一次Handle异常；既有finalizer observed count1并非对象身份。原33条路线样本、旧冻结seam委托、Main死亡及HUD前缀已执行，不扩成75全部完成。P1 fixture未在本案selected中，先核当前SHA与1695清单一致再只读，未回填旧案：`E4350E9B0FC86D5217D9CB4C02C539C376F30A20D5CD4EFCC122117FEE4FAB36`。

## 完整post的新首错

session60029已终态exit1；root完整读取build与stderr，严格失败原件不覆盖。

```text
P1LoopRuntimeTests.HiddenDiscoveryPersistsAcrossDeathRecovery:67
Assertion failed: Task 9 recovery request and completion both occur in the same physics frame

另一个真实ERROR：Handle is not initialized.
Godot.Bridge.ScriptManagerBridge.SetGodotObjectPtr
```

generated Main死亡专项在此post再次通过（一次request/completion、原输入清理、start160/600、actor延后一次），但不能据此冲销P1发现保留合同的失败。P1用例仍加载当前正式Main和隔离存储：原两次Physics后反射报告隐藏发现，再直接公开RequestRespawn；**没有实际Health致死、输入指令、HP或位置写入**。它在请求返回后没有await便要求请求/完成各1。生产接受请求后CallDeferred恢复，与同组已适配的同步1请求/0完成前提冲突；但本案P1自己的request/completed/physics子值未打印，不能借Main的数字补出它的实测值。

P1失败也跳过其正常Dispose且没有finally，自有level/事件闭包/排队恢复存在可证清理风险；这是下一独立对照点，不是Handle归因证据。Handle栈无对象身份，不归因为已确定的同一fixture、旧音频或资源对象，也不据前一安静案关闭历史故障。此方法仍是正式Main的进度/恢复测试，不能因同suite含旧教学合同而改挂冻结地图。

抛异常观察者与Room错误root负控warning仍原样存在。由于P1异常中断75case计划，GameFlow调用方AssertExact仍未执行，不写成已观察到GameFlow计数失败或全部75完成。下一先保持原fixture调用/出生/HP/await与预算，补被动恢复时序，再核对批准的deferred恢复合同及失败路径自有资源清理；不把生产改回同步恢复来迎合旧断言。

## 当前状态与成本

本轮进展为真实错误输入来源的RED→GREEN及新首错推进，不是新增地图、美术或敌人数量。正式新50仍0/50，24canonical、最终地图/机关/原创视听/UI/演出、历史五失败/音频/匿名资源、正常双路线/存档矩阵/真人/连续AV/目标硬件60FPS/唯一ZIP/获授权核验GitHub全部继续OPEN。

保存Player SHA仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。所有owned消费者终态，CIM未发现所钉Godot/dotnet遗留；source已解冻。E盘余2343784448字节（约2.18GiB），未删移旧证据，无付费生成、新展示媒体、Git写入、ZIP、发布或自动化。
