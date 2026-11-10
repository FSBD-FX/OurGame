# v066 · 哨兵危险漏判与正常主线齿轮边界

日期：2026-10-23（Asia/Shanghai）。项目为 KNIGHT，开发根 G:/knight/Knight。Ourgame3d 是另一工程；用户已明确本聊天范围，不再因共享启动目录的另一项目规则反复询问切换。原序章第1–11部分范围不变，未完成，不建立自动化、不重新建立已不存在的 goal 对象。

## 实际改变与边界

本轮改变的是自动键盘路线的测试消费，不是生产敌人 AI 或玩家能力：正常危险判断此前漏掉不继承 RangedWeaponEnemy 的 BellBoltSentry。补入实际前摇／Fire、仍在飞行的未退休弹丸、暂停／死亡／排队祖先拒绝、HasRanged 与新鲜 J 准入。弹丸采用其实际位置作为防守来源；其飞行寿命不能借用发射者的 Recovery 时钟，故保持 Active／counter0；AttackId=0 明确表示此观察接口不知道弹丸独立攻击身份，不伪造 ID 或 ETA。

保留当前保存 Player 的 5HP、正常 Motor、无敌期、敌人 Settings、伤害、物理、地图、人口与原预算。Sentry 专项沿用已有 runner 的显式退出排空，不与不排空的 prefix 诊断混淆。accepted-damage 输出的 guardCommand 改名 genericGuardPlan，说明它不是所有特殊分支最终发送的输入；实际 shieldHeld／shieldActive 才是实际状态。

没有制作或接入新的最终美术、音频、动画或演出。本轮也没有真人试玩、正常显示性能证明、新包、Git 写入、付费生成、媒体采集、删除或 UE 修改。

## 真实 RED → GREEN

- [RED 原件](G:/knight/验证证据/october13-stable-claw/ranged-sentry-danger-20261023-113936-442-eb438b4447184b42872b34b239e64b7b/engine.stderr.log)：普通构建，原生 exit1／812ms／guards0；场景正常进入真实 Telegraph，但 Task10EnemyThreat=false，命中 SENTRY_WARNING_DANGER_RED。
- [GREEN 原件](G:/knight/验证证据/october13-stable-claw/ranged-sentry-danger-20261023-114135-061-f5ac9c5d81a04231ab2aa56244f67ced/engine.stdout.log)：原生 exit0／2315ms；非 steam 遭遇、selected=null/self 均识别真实预警；真实 Recovery 尾弹仍危险，键盘 L 原生挡住一次，HP 保持5；退休后危险消失，暂停与外来遭遇不能借威胁。
- 这是单哨兵／隔离平地专项，不是深渊原图、多威胁或正常整关证明。新 fixture 在加载提前失败、节点尚未挂父时的自有节点清理缺口仍待补；正常已构造 RED/GREEN 不依赖该失败分支，不能据此解释历史资源崩溃。

## 正常 Main 的实际推进与新失败

[Main stdout](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261023-114251-887-f7fcbd5e0f354f03b42c4ffe62939840/engine.stdout.log)／[stderr](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261023-114251-887-f7fcbd5e0f354f03b42c4ffe62939840/engine.stderr.log)，原生64334ms、exit1、guards0、无超时。

- Rift 仅记录新 Flyer 的一次 HP5→4；不能据此宣告所有居民战斗完成。
- 旧 controlled-abyss 真实完成一次，6次攻击／6次连接，HP4→4；MAIN_ABYSS_OK 后以同角色、零复活到 basin-camp。不是跳过旧波次或死亡传送。
- 实际越过 ShieldLane、普通跳落到 GearPistonOne。随后旧 CoolOnSafeFloor 先松 L、停止 A/D，最多210帧内只等冷却和检查脚底，没有应对新增居民。
- p2552：gear-pistons.05 新 Scatter ordinal2 实际命中 HP4→3，彼时 Overheated=true；p2593：gear-pistons.04 新 Flyer Hitbox 命中3→2，彼时 Overheated=false。最终 energy=1、heat=.020、非过热，但 HP 不符，原无伤断言正确失败。不是冷却超时或玩家死亡。
- 进入 lane 时 heat0；末观测 p2395 heat=.644、未过热。跳跃到稳定落地之间没有完整 heat 标量，不能把第一次记录过热的 p2552 当作过热起点，也不能先认定提前松 L 就解决问题：第二次伤害已发生在解除过热后。

实际 HP 观察记录3行、gaps0。source/local attack ID 从 Health 接受顺序读取，远程命中配对发射 ordinal；但一般伤害缓存的 cohort 分类仍弱于严格 saved-resident 场景身份门，gaps0 不等同全部敌人权威身份或全50自然战斗资格。观察不覆盖原失败堆栈。

## 同源最终执行：12条，9 PASS／3严格 FAIL

先保留当前 Sentry GREEN 与 Main FAIL，再执行一次有限10条回归（8 PASS／2 FAIL）。不是12条均新跑回归，也没有重复同一失败 Main 求安静结果。

| phase／原始 provenance | native exit | native ms | 结果 |
| --- | ---: | ---: | --- |
| [ranged-sentry-danger](G:/knight/验证证据/october13-stable-claw/ranged-sentry-danger-20261023-114135-061-f5ac9c5d81a04231ab2aa56244f67ced/provenance.json) | 0 | 2315 | PASS |
| [thruster-route-main-diagnostic](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261023-114251-887-f7fcbd5e0f354f03b42c4ffe62939840/provenance.json) | 1 | 64334 | 严格 FAIL |
| [ranged-threat-policy](G:/knight/验证证据/october13-stable-claw/ranged-threat-policy-20261023-114931-268-5897400b6e0e41d59c06f812fb87f129/provenance.json) | 0 | 4674 | PASS |
| [ranged-mixed-policy](G:/knight/验证证据/october13-stable-claw/ranged-mixed-policy-20261023-115020-904-ebd165ed21b1419fb4d0b324f4f779ed/provenance.json) | 0 | 3958 | PASS |
| [ranged-mixed-resource](G:/knight/验证证据/october13-stable-claw/ranged-mixed-resource-20261023-115058-781-be2f9e50a2c24039ab0d5e0cc2b8d156/provenance.json) | 0 | 16200 | PASS |
| [resident-auto-start](G:/knight/验证证据/october13-stable-claw/resident-auto-start-20261023-115149-879-2ff8db6ef18f42bcacc2c307a3657410/provenance.json) | 0 | 4575 | PASS |
| [resident-auto-session](G:/knight/验证证据/october13-stable-claw/resident-auto-session-20261023-115224-366-f38a5bae0f614f3ea5df4a7066584d92/provenance.json) | 0 | 15330 | PASS |
| [resident-save-compatibility](G:/knight/验证证据/october13-stable-claw/resident-save-compatibility-20261023-115314-071-20306a0e63e541f8b2fcf8c5f8a67a9e/provenance.json) | 0 | 21635 | PASS |
| [keyboard](G:/knight/验证证据/october13-stable-claw/keyboard-20261023-115417-443-513abb88a3dd402fb0ed32a76453004c/provenance.json) | 0 | 9813 | PASS |
| [actions](G:/knight/验证证据/october13-stable-claw/actions-20261023-115505-934-dac3d4cd7d1d46b69204205450b19a0b/provenance.json) | 0 | 7201 | PASS |
| [prologue-pre](G:/knight/验证证据/october13-stable-claw/prologue-pre-20261023-115550-512-e736f27f51854baabdf0abcc00f23008/provenance.json) | -1073741795 | 35402 | 严格 FAIL |
| [prologue-post](G:/knight/验证证据/october13-stable-claw/prologue-post-20261023-115705-560-29593d2cfb004e29a56737b07819169d/provenance.json) | 1 | 7700 | 严格 FAIL |

- auto-start 有唯一已准入的故意负测试警告 Rejected prologue flow transition Intro->Intro；stderr／external 为同一警告两份记录，不称零警告。
- 当前 pre：2条 Handle is not initialized 和一次 gchandle FATAL，exit −1073741795／35402ms；调用栈含 CanyonResidentManager.CaptureOriginalSpawns:177。不能继承 v064 的“pre无Handle”或把 quiet mixed-resource 当历史资源根因已解决。
- 当前 post：首因仍是 BasinAscentCameraRuntimeTests:121 的真实 PlayerMotor automatic 断言，scope receipts 缺失是后果；完整 post 未完成，不继承历史75案例。
- 本轮没有专门重跑四项 binding/profile/Archive/Steam RID 门，历史严格失败继续 OPEN。当前 mixed-resource 完成四轮只说明本案完成，不关闭旧 FATAL。

## 冻结与保管链

12案 source-before／after、helpers-before／after、user-before／after、continuation-before／after、fresh-binaries／binaries-after 均一致，60对清单核验；全部1772清单项／888 C#，相同普通 Rebuild，无额外 DefineConstants。72份封存二进制逐个核哈希匹配。各案 helper 按 phase 可多出额外准入项，不要求跨 phase 全文相同。

共同 source 清单 SHA256：39275CB7E6BC66F9D95B491CA03FA21F4EEE5F5297D716BAB09A0EFAFF42A896。
共同6bin清单 SHA256：7788308F83D619E1FD4D406F17D73332E9E8584F44F5243432B322A2D071BE8D。
Knight.dll：FA2B33EA04DA0B46FB32F945E81ACECE0894DED317F81FA29FF7B50EDC5A68BD。

独立只读审计 Sentry/Main 两案：1002份选定 source copies、22份 helper copies、12份封存产物匹配；不是全部1772源码均有副本。根执行者另核12案清单和72产物。全部 owned consumer 终态后，根读取当前1772源、post所列helper与6bin，无漂移；Git index 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C 未变。随后仅更新文档，不改本次编译源。

一次只读汇总脚本初版未把空stderr转成空字符串，导致汇总器自身报null异常并遗漏8行；修正字符串读取后重新核完整12案。该汇总错误不是游戏错误，也没有重跑游戏或借不完整汇总报通过。

## 下一步与仍未完成

先在正常路线真实离开 lane、首次接触 PistonOne、稳定落地三个边界记录 heat／overheated／实际 L 标量，保留原210帧和无伤断言；然后依据实际威胁与资源条件设计避弹／交战／冷却，不移敌、不关AI、不写HP或姿态。过热和裸等受伤不能混成一个已修原因。

另继续定位 pre 的具体资源拥有边界，旧相机夹具须消费真实准备／路线而非禁敌或 FinalOnly。原26+新50=76普通＋Boss、骨骸10／幽灵10、24子型、所有机关、最终统一角色敌人场景UI动作特效音频与演出、正常 Main+Explore、保存恢复旧档、720p至4K、真人及正常显示60FPS、最终ZIP/GitHub读回归档均不能因本轮局部进展勾完。

