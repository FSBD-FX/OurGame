# 补给场景五次挂载对照 · v001

2026-10-16 / Asia/Shanghai。局部诊断进展，不是资源崩溃修复或序章交付。用户确认测试时在操作其他窗口；本轮全部无窗口，不抢焦点、不补键。

## 事实、推理、风险

- 事实：上轮完整原 Supply 四项／五棵 root 的独立运行发生 FATAL；本轮五次原挂载／满血接触／原清理对照严格通过，实际引擎3583ms。五个不同 root 均达到 Playing，玩家真实 overlap，生命5/5、补给未消费、运行及隔离保存均无 salvage_cache，原清理返回后 native root 无效。
- 推理：这次在没有测试额外伤害、补给治疗或治疗回调重入的条件下未复现。正常 AutomationAutoStart、ReturnToTitle→StartNew、Health.ResetFull 保留；不能说整个历程没有生命初始化或 Healed 事件。
- 风险：对照比完整 Supply 的分配量、持续时间和操作不同，clean control 不能证明 Heal 是根因，更不能关闭历史 FATAL。RELEASE_RETURN 和 root 无效只证明这五个根的原退出，不证明所有资源所有权。未给匿名错误指定资源身份。

## 实际变更

仅修改现有测试、runner 与 canonical owner：新增 RunMountOnly 和 basin-supply-mount-only phase；五个 root 使用原 OpenSample(false) 与原 FreeSceneAfterAudioDrain。仅保存标量 root ID，不保留 Resource 历史；新增断言不是空接触正控。owner 要求五份 control、case-return、release-return 以及汇总返回；三 raw 诊断仍严格否决，不放宽超时或错误门。

没有新增 C# 源文件，当前1670是 source/resource inventory 行数，不是1670个 C# 文件。没有生产玩法、地图、数值、美术、资源策略或初始化变更；没有新增 GC、KeepAlive、缓存更改、借用资源 Dispose、等待帧、退出 drain、升级或日志豁免。普通 build argv 未加入诊断 symbol，未扩展 IdentifyRefCounted 的 phase 许可。

## 本轮五案

所有 case 位于 `E:/OurGame/Knight/.evidence/october13-stable-claw/`，每案保留 provenance、native/contract/wait、三个 raw 和保管链清单。

| case目录 | 引擎实际/上限ms | fresh Rebuild实际/上限ms | 严格结果 |
|---|---:|---:|---|
| basin-supply-mount-only-20261016-223456-539-af764a50789745ccb69a6d239dd27ee4 | 3583/55000 | 24016/180000 | GREEN，五个真实控制点和原退出完成 |
| prologue-pre-20261016-223848-369-8dad5af75f584f2cb8fc3f7fe480685c | 88672/180000 | 21431/180000 | FAIL，UpperPendulum/lower_to_mid_swing sample0.5，另2个PresentationProfile WARNING |
| prologue-post-20261016-224044-331-d79875e32cb4467baf544a34dea931f2 | 8404/180000 | 25056/180000 | FAIL，BellChain落地断言及KillZone的ILevelRecovery路径 |
| keyboard-20261016-224123-609-a34e7b89284343a6ab199bb3bc31fdef | 9590/55000 | 24048/180000 | GREEN |
| actions-20261016-224202-226-17f8e97f52c342779a0eaec085b828ac | 7073/55000 | 22927/180000 | GREEN |

合计3 GREEN／2 FAIL。五次 fresh Rebuild 均exit0、0警告/0错误、build stderr空；无超时、guard failure、named expected failure、fixed-fps、movie、verbose或identity诊断模式。全为headless自动专项，human/naturalRoute/wholeGoalAccepted=false，不是正常Main/Explore、可见60帧或真人体验证明。

pre实际完整原Supply已返回BASIN_SUPPLY_RUNTIME_OK，随后Upper末点(264.99634,160.6107)，149/150帧，目标区crossings0；这次Supply返回不能否定其上轮独立FATAL。post实际末点(540.0704,1196.0139)，KillZone无法解析六层祖先`../../../../../..`。本次未出现Handle/FATAL/RID，不是历史问题修复。pre/post只完成failure drain，不是phase成功。

五棵mount-only root ID依次46690993664、90294978048、133261428224、176362096128、211829130752。各有ROOT_CREATED、OPEN_RETURN、实际overlap/full-health/unspent控制、原RELEASE_RETURN、MOUNT_CASE_RETURN；AudioDrain五次，phase finalizer/exit-drain/STABLE_CLAW_OK齐全。满血时补给在Discover/RestoreHealth前返回，正常开局仍会初始化生命。

root扫描全部三raw诊断、native/build/contract/wait/provenance及五组保管链；长重复轨迹仅有限展开，不称逐条人工审阅。初次只读汇总把空stderr当null导致Matches参数错误，随后按string重扫，三个GREEN的stderr实际均0字节；未把汇总脚本错误计为游戏错误。另有只读独立证据核对，不是整计划验收或代码批准。

## 保管链与版本

五案25/25组前后相同；真实userdata只做哈希保护，没有语义读取或写入。全部消费者终态后才更新本文与状态文档。

| 清单 | 行数 | SHA256 |
|---|---:|---|
| source/resource | 1670 | 7DA0EFABFD0E13A55E21274724A3B58C9DCBE2D92474DB4E91CC8448F6D0DB12 |
| protected-user | 109 | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| continuation | 1 | 7EB70257593DA06F682A3DDDA54A9D260D4FC514F645237F5CA74B08F8DA61A6 |
| fresh-binaries→after | 6 | A620DC3D4DF68ECE9F4B2250459700CC64A25AD3528CC78147AB12FAAF2CD682 |
| helper，pre | 10 | 5AF1DA11D43493D96F3CDD7CC55ECAA671E7CD0F8A8E838D9545A616AA19E65A |
| helper，其余四案 | 9 | DFD4FA257C294F85C7FF0A401BA89F2F7A8A55A43157E5A811A2A42FD052AE1C |

当前关键源SHA：

| 文件 | SHA256 |
|---|---|
| tests/integration/BasinSupplyRuntimeTests.cs | 1134B52CDDA9E78FF230D882C83CA2FC64BA5638BE0A1B190E5AF77A2F17979A |
| tests/RunStableClawPullTests.cs | A04F41D8FA68290F069DF9F05DEFFD66CF67B17D89136970D89392976856C3C4 |
| .superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1 | 4F86CD18F8BB2C729C8D64FF357FD78A91564AFF8BBE4C72A7EAE6FAA361B0CD |
| 上轮supply-lifetime-isolation-v001.md原件，未覆盖 | 5C4DF2EAEAA0AD12B5B4D901046CEA4355EBACC74C0710B1054A1E682CEE2763 |

交付文档检查保留既有README EOF空行告警（当前1499行），未改无关历史尾部，不能称整个worktree或包含README的diff检查干净。新增报告、briefs、ledger及测试/owner无尾随空白；原件报告保留，HEAD/branch未改。

## 下一步和完整范围

这条五挂载对照已完成，不再把它列为待运行或无变化重跑。下一开发增量回到剩余六个骨骸/幽灵承载：先Bone Rust真实Recover/K-Stunned弱点权限；必须拆分当前Ghost-only profile判断，使用真实J/K和睡眠边界，不注入姿态/HP伪造通过。随后24正式场景、全部50实际活动/玩家通路和跨区manager。

资源另保留有区分力的下一入口：只执行原CheckSupply(lifecycle:true)的原伤害/接触/死亡/Continue两root序列，排除先前NewGame/Title治疗重入前缀是否必要；尚未实施或运行，clean仍不能证明前缀致因。不要用无变化完整回归/151类型计数循环替代生产实现，也不猜资源修复。

正式新增仍0，50 Placement全null、24 SceneState Unimplemented/ScenePath null；局部已有BoneHeavy/GhostWanderer开发适配不等于完全没有巡逻代码，更不是50只已入第一关。旧26＋新50＝目标76普通＋1Boss，骨骸10/幽灵10必在本关，ECHO1人格/独立友方身体0/商人0，不改既定分配。

Task1最终50点、整个Task2、Task3–4仍OPEN。资源、旧档、正常双路同角色Boss/有支撑出口、最终围合/家族美术/视听、可见稳定60帧、真人反馈与审阅归档录像门不减；原完整目标ACTIVE，无任务完成或整计划批准声明。无新媒体、付费生成、Git写入、版本发布、自动化、安装或删除。
