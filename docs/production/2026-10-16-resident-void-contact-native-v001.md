# 锈甲常驻样段：真实坠落边界 v001

2026-10-16，Asia/Shanghai。开发实现与原生证据，不是整关、正式敌群或美术验收。

## 事实

本轮为**已显式配置residence的Rust**接入真实坠落边界。新增 `ResidentVoidContact.cs` 和 `ResidentVoidRuntimeTests.cs`，实际源码库存1660；normal准入精确库存及新消费者的源码/五保存区/Boss/Rust副本守护同步更新，没有关闭原保护门。

最终 `resident-void` 14项原生检查严格通过，engine0、13,370ms、无超时、guardFailures空，stdout/stderr/external三源无诊断。正确helper SHA为 `BF7B70B7D52D1C044E9B7551E351836E1E164A4B76BBF015022B2BB12D1CD441`，与恢复备份精确一致，没有遗留故意删掉扫掠的版本。

正式关卡仍原26普通名额＋1Boss；**新增实际敌人0，全部50正式Placement仍null，24子型正式ScenePath仍未实现。** 本轮并未复制旧敌冒充骨骸/幽灵，也没有缩减50及五族/六职能目标。

### 生产逻辑

- 现有KillZone在Ready登记NPC边界组，玩家原BodyEntered、mask、复活controller及伤害规则保持原样；新NPC语义由配置过的Rust消费，不自动赋予全部旧敌。
- 使用真实启用的CollisionShape2D，而非环境“潜在危险包络”、出生高度、Territory越界或整条sector底边。完整身体初始/末端重叠与固定basis平移扫掠均检查，不能用两端采样漏过薄条带。
- 仅同一World2D的活跃、在树内、未排队删除的来源有权生效；SeamlessWorld内还检查Actor/source属于同一world、双方所属sector正是registry中当前实例且非staged。逐tick取活来源，不缓存一个已卸载sector名称后继续杀替代实例。
- 接触后锁存fatal pending，撤近战/直接伤害/机械丝权限；经真实Health.ApplyDamage请求致死，无敌期拒绝后保留同一伤害身份重试。Health继续真实计时；不ResetFull、不改无敌时间、不直接QueueFree冒充击杀。
- 已经发生的接触不会因睡眠或来源之后移除而丢失；实际Health死亡沿原Died→Defeated链发生一次。Actor离树与checkpoint reset清自身pending，卸载不是死亡。
- 未配置Rust仍走旧wave分支；旧Skitter的home+240政策没有在本轮改写，不能称所有敌人的坠落行为都已统一。

### 14项原生检查

| 检查 | 可直接观察的结果 |
| --- | --- |
| 未配置旧Rust | 拉落后不被新resident政策误杀 |
| 普通远低层地板 | 实际拉下平台，落差392px后接地、HP3；不是home+240误杀 |
| 拉落穿真实void | 实际重力接触后Health0，Died/Defeated各1，不调用玩家恢复 |
| Disabled边界shape | 落到下层仍存活，不把潜在保留区当致死接触 |
| 外World同坐标边界 | 不得跨物理World杀死Actor |
| 初始完整身体重叠 | 不依赖下降速度，真实重叠也会致死 |
| 16px薄条带高速穿越 | 前后完整身体都分离，途中扫掠仍检出 |
| 无敌接触→睡眠→移除来源 | HP2等待真实无敌结束，再Health0/一次死亡；不是绕过无敌 |
| 原玩家Area入口 | 实际CharacterBody.MoveAndCollide入Area，仍只请求一次原恢复、无HP成本 |
| 接触前来源QueueFree | 不启动fatal pending |
| 半空休眠后接触 | 不因撤玩家伤害权限而获得环境永生 |
| pending Actor卸载/同名替代 | 旧Actor保留HP2且无Defeated，真实新实例HP3；不是卸载算击杀 |
| 保存FrozenExit→雕孔→Camp | 实际完整身体支撑、拉拽600及重力，下层命名地板接地、HP3/死亡0 |
| 保存恢复条带正控 | x1450实际落入保存shape，Health0、Died/Defeated各1 |

保存地形负控不是重建的示意图：使用五个原保存sector及其原祖先变换、独立World2D。Fixture只停止原sector的逐帧机械callback，不关闭碰撞、KillZone监测、Rust/Health或viewport physics；Ready先发生，随后才挂活动Rust。资源形状按原几何夹具复制并登记所有权，源shape不被Dispose或改写。该固定相位夹具不证明全部动态机关。

FrozenExit起始world `(1820,3248)`，真实拉拽后经Descent两恢复shape间的开口，最后落到BasinCampFloor：`(1878.192,3963.9585)`、HP3、Died0/Defeated0、最大实际步长19.600098。原两条恢复shape仍为 `Rect(1280,3584,336,16)` 与 `Rect(1996,3584,564,16)`，没有合并成覆盖开口的大矩形。正控x1450最后 `(1450,3552.666)`，HP0、Died1/Defeated1。没有挂载后Actor位置写入或回家瞬移。

## 推理与反例

新测试先于生产void实现运行：`113505` 四项新死亡行为实际失败，默认/普通低层/Disabled/外World四负控通过；但退出还有4Shape RID，因此不是干净RED，严格失败保留。编译GD0002及缺using的两案没有运行native，均不算行为RED。

初始实现的8项行为虽都满足，`113748` 仍有4RID并严格FAIL；之后加入真实保存场景及原玩家回调的11/14项过程干净退出，**这不说明4RID根因已修复**。没有为了凑零加GC、cache模式、KeepAlive、退出drain、借用资源Dispose或诊断豁免。

另作针对性反例：只移除helper的CollideWithMotion，其他13项仍满足，唯一高速薄边界用例HP仍3而失败。这个案无退出RID、源码清单只有helper一项不同。恢复精确正确源码后14项严格通过，证明端点检查不能替代中间身体扫掠。

## 本轮原始证据

全部位于 `E:/OurGame/Knight/.evidence/october13-stable-claw/`，原始stdout/stderr/external和before/after、副本、二进制、provenance均保留：

| 完整case目录 | 结果 |
| --- | --- |
| `resident-void-20261016-113304-674-92e1619449da428595bc9bf887f2266d` | GD0002编译失败，非行为RED |
| `resident-void-20261016-113505-158-f03302a4feaa4778b4810c9113762c45` | 8项中4新行为失败＋4Shape RID；9,346ms，严格FAIL |
| `resident-void-20261016-113748-129-3b1ef770f1d847938501de0819e8ee14` | 8项行为满足，但4Shape RID，严格FAIL |
| `resident-void-20261016-114255-117-fce678cf18754719a82a176139ca689b` | 共享fixture缺namespace编译失败，非行为RED |
| `resident-void-20261016-114357-959-710fdd17e0d94e25891d8c72e23429f0` | 11项严格通过，10,791ms；此时雕孔从空中起落，不冒充平台拉落 |
| `resident-void-20261016-114525-060-1a1c670110bd44908f63fd31bbb8a317` | 强化真实FrozenExit支撑→拉落后11项严格通过 |
| `resident-void-20261016-114725-141-32d41770f5bd417eaa533dae86fc042b` | 增加queued/sleep/unload控制，14项严格通过，13,275ms |
| `resident-void-20261016-114927-181-90733a6ca57148f5bb9083017f576e2c` | 故意删sweep，仅薄条带失败；13,363ms，严格FAIL，无命名豁免 |
| `resident-void-20261016-115105-059-1f27b3b47ce3485b95b77ec5bd92eb84` | 精确恢复后的最终14项严格通过，13,370ms |
| `resident-patrol-20261016-115215-690-8a99530048c147cdb93ec53973a2eb8c` | 原7模型/15原生/default Rust模型和视线通过，44,890ms |
| `prologue-pre-20261016-115429-665-b816026df92349559e4f456b240e1a20` | 88,853ms，旧Upper/lower_to_mid_swing sample.5落错台149/150＋两条刻意无效profile警告，严格FAIL |
| `prologue-post-20261016-115717-670-404ca4ed91f843a496da6fda1b105ab3` | 8,212ms，旧BellChain地板(540.0704,1196.0139)/KillZone无法解析ILevelRecovery，严格FAIL |
| `resident-geometry-20261016-115910-984-8dc457cab5e1428ab25ee6a2806670ad` | 空间/交互/Archive等检查通过，20,311ms；仍41候选/9未解，不是正式点位 |
| `keyboard-20261016-120247-063-04041afc1d47415db6c1b7d57811fcf6` | 原纯键盘/机械爪专项通过，9,690ms |
| `actions-20261016-120404-847-b8a0eb99fe224b01929d18f5397316c8` | 原动作/气力/过热专项通过，7,086ms |
| `resident-allocation-20261016-120546-116-d8fe39a747f34e93922bd11ad8c17e1c` | 50身份/24子型/17负控通过；全部正式点/身份场景unready、新增0 |

最终各专项及完整pre/post均source1660、无超时、guardFailures空；GREEN三raw无诊断，源码/user/helper/二进制/continuation前后清单一致。真实保护库存109，只读hash、不读写用户存档语义。历史Handle/FATAL在这两次完整回归未出现，不代表已修复；独立4RID仍OPEN，完整pre/post未豁免。

## 风险与下一步

1. **真实streamed registry分支尚未原生验证。** 保存地形用例在普通Scope下，双方streamWorld ancestor为null，日志明确 `streamed_registry_tested=false`。下一步用真实SeamlessWorld、裸persistent body/PlayerRoot、五个保存定义及公开RestoreCheckpoint，验证注册来源、卸载、同SectorId新实例、未注册副本、staged和同物理World不同streamWorld，不用反射改registry或存档。
2. 本版检查固定basis的身体前后直线扫掠，不是旋转/变形、移动虚空shape或多段slide轨迹的完整证书。当前14用例覆盖指定直线坠落；复杂轨迹仍需独立真实反例。
3. 原7条恢复shape不覆盖所有外世界边缘。未覆盖处需显式NPC虚空volume；不能用任意Y截止填掉合法跨区开口。
4. pending在Actor离树时清除；未来manager是否跨卸载保留已发生fatality，仍需其epoch/HP/死亡状态合同，不得把当前局部卸载负控当成完整跨区持久化。
5. 逐tick查询边界组的50人性能未测。没有正式50、正常新版Main/Explore、呈现稳定60FPS、真人或成功新录像；用户使用其他窗口期间不抢前台、不补键、不无改动重跑。
6. 下一实际行为生产者仍包括冲锋/飞行/俯冲/重装/炮台及骨骸/幽灵差异。Skitter需独立处理锁向、真实Rush/Retreat距离账、600限幅外力和opt-in的真实void，不能将home当玩家或把旧home+240直接带入resident。

完整目标ACTIVE。Task1全部50点最终认证、完整Task2、Task3 manager、正式美术/十区围合/旧档/资源/整路/性能/真人均未完成。本轮没有新媒体、付费生成、上传、Git写入、自动化恢复、安装升级、删除或用户存档语义改写。

## 关键源码身份

| 文件 | SHA256 |
| --- | --- |
| `scripts/enemies/RustShellSquire.cs` | `C94406B44A43679C5E0EDC650AF85BD5851656F16CFACA4EC96A2EE659EA612A` |
| `scripts/world/KillZone.cs` | `5E38EDD36A3739945E36200DE44DBCE2C663038768308202A1A1BD5307D9617F` |
| `scripts/enemies/residents/ResidentVoidContact.cs` | `BF7B70B7D52D1C044E9B7551E351836E1E164A4B76BBF015022B2BB12D1CD441` |
| `tests/integration/ResidentVoidRuntimeTests.cs` | `08A98811452EFCB1D02F986B2988101A518CCB12381A2B44962C7D943AAE7169` |
| `tests/integration/ResidentPatrolRuntimeTests.cs` | `AD8CC43B4ABC1B60A2618F6B054B99E667AE1555CCF139FAC12CE3353A926E0C` |
| `tests/integration/CanyonResidentPlacementTests.cs` | `96F55E80AA1944E303141742851C09D09122BEC3A260A6A09E33CEBCC2190773` |
| `tests/RunStableClawPullTests.cs` | `A16B70B2E3DB3B355C236359F116FB170F649B79EBFE6A54017B81DFA774705D` |
| `.superpowers/sdd/2026-10-13-stable-claw/verify-claw.ps1` | `6E0CB39082B430B9EE6CE17AC9DF36A552E9EAD770220A0929A5CE6D41E20DD4` |

备份：`.superpowers/sdd/2026-10-16-resident-population/before-void-contact-20261016/` 与 `before-void-sweep-control-20261016/`；没有覆盖此前版本。
