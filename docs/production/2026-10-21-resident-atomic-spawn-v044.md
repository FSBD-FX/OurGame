# 常驻敌人原子装载 · v044

2026-10-21。总目录 `G:/knight`，实际工程 `G:/knight/Knight`。完整1–11目标仍ACTIVE；本轮是实际装载底座，不是50只正式敌人、最终美术或试玩交付。

## 事实：实际实现

新增 `scripts/prologue/residents/CanyonResidentManager.Spawn.cs` 和一个实际Level集成测试源，扩展原manager与World内部上下文。库存1702项源/资源，其中872个C#，比v043新增两源。未修改地形、旧26分配、玩家数值、键位、9残片/6门槛、美术或音频。

事务按稳定ID读取构造时复制的slot/subtype，不接受调用者替换玩家、区域矩形或“验证通过”predicate。它从World已捕获的真实Player、Level、注册sector、作者布局获得上下文，而非重新解析可变PlayerPath。验证Ready、所属关系、未退休与范围后，实际读取独立保存子型叶，在AddChild前通过原binding配置真实actor、安静准备历史HP。

实际原生检查覆盖完整身体、支撑/净空、固定姿态巡逻走廊，并合并现有环境全相位保留、旧26的36条主/备出生身体记录、当前玩家、同帧已挂载及pending占位。新增保守出生政策是玩家身体与0.25秒速度预测包络合并后Grow64px；显示中的玩法viewport内拒绝突然出生。这不是完整跳跃/攻击/追击保护证书。

pending隐藏、暂停行为、休眠；AddChild/Ready与重新激活后复核epoch、resetRevision、确切World/Player/sector/Actors身份与实际pose，再做原生检查，最后登记lease。public TryTrack不能提前或跨slot登记pending actor。失败只清理本次确切自有root、绑定和占位，不按名字或ID误删另一个owner的新实例。借用保存Shape不被扫描Dispose。

canonical自动出生仍未开启：50个Placement全null、24种正式SceneState全Unimplemented，正式新增0/50。测试仅在每个独立case的storage里保存一个明确占位开发叶；未发布到正式资产或靠标志把旧美术改称正式。

## 事实：测试先行与反例

证据根 `G:/knight/验证证据/october13-stable-claw`。下表均为`resident-level-lifecycle-`目录后缀，失败原件保留。

| 后缀 | 实际结果 |
|---|---|
| `20261021-231901-166-bff212d1f70946e081973b25eb75fd0a` | 行为RED：旧Level/三死亡通过，新保存叶因`spawn_not_implemented`失败；engine1/3628ms、guards0。 |
| `20261021-232532-701-9c4a76cd792a4f11a395d80ebdca2f6f` | 构建CS1061，误用Sector而真实合同为SectorId；不是行为RED。 |
| `20261021-232655-785-4552ab19d5b849b3b1429153680ada40` | 首轮严格GREEN，engine0/3497ms：实际Ready/世界pose/直接父、HP3→2→卸载→Ready2且无合成事件、真实死亡、重入回滚。 |
| `20261021-232857-076-d7b8c1cc50f74f6b91296eb754f6aa9a` | 整个PlayerRoot移走却因相同玩家/直接parent/World2D而错误准入，实际RED。 |
| `20261021-233009-299-8d4b4120ae2e4447bf993feabdc1cadb` | 补真实Level、PlayerRoot与WorldRoot所属链后严格GREEN。 |
| `20261021-233149-095-d0f78d46ea6e443f9cc4d58066c23cc7` | activation回调经public TryTrack提前占有pending actor，实际RED。 |
| `20261021-233320-921-b60df131eb334c28815b71aeefa01549` | 私有claim-aware commit、public pending身份/身体保护后严格GREEN。 |
| `20261021-233604-085-ecc6f4fb507d47c390899977c2729d29` | 替代实例测试错误要求在取消中实体仍占据的同一位置立即成功，失败保留；随后改成不同身体候选位置，没有放宽生产净空检查。不是资源故障定位或修复证据。 |
| `20261021-233902-613-11a656454ea34a3fbfd0a0a0173409e1` | 最终严格GREEN：另覆盖同/跨slot提前登记拒绝、activation实际pose变更拒绝、旧owner回滚保留新owner同ID异位置实例、真实canvas可见出生拒绝、玩家/旧未来波次/无支撑拒绝。 |

玩家移动、canvas调整、候选home1510/1228与1586/1388均为显式隔离夹具，不是正常游玩路线或正式位置。当前九类carrier及保存后代的只读检查未见Ready改借用Shape尺寸；不覆盖任意未来自定义Ready，也不证明原生资源故障已关闭。最后原生检查后的pose也再次复核；测试复现的pose变动发生在activation，不冒称已重现更晚的潜在回调路径。

## 事实：同一冻结版本完整回归

命令为`verify-claw-knight-v001.ps1 -Phase <phase> -SourceRows 1702`，普通构建，无identity/verbose/movie/命名豁免。root串行启动，消费者存活时所有源码/helper/docs冻结。

| 完整case目录 | engine ms | 严格结果 |
|---|---:|---|
| `resident-level-lifecycle-20261021-233902-613-11a656454ea34a3fbfd0a0a0173409e1` | 3869 | PASS |
| `resident-state-20261021-234053-066-84d8c181915649598e0863b0fd0de7df` | 536 | PASS |
| `resident-lifecycle-20261021-234119-406-9e14fc8b76da4a5cbe10736387e3df49` | 1355 | PASS |
| `resident-void-stream-20261021-234147-939-d95580ee469d4d3a90f959a4a4acb54d` | 1463 | **FAIL** |
| `resident-allocation-20261021-234216-335-983cbd1998aa43158ed24ad94757c420` | 516 | PASS |
| `resident-save-compatibility-20261021-234242-978-42f6617f70674571bb8d70aaf138184f` | 2718 | PASS |
| `checkpoint-lifecycle-20261021-234309-329-f967615062c341dfb14400905308c6a3` | 1346 | PASS |
| `keyboard-20261021-234334-673-01e59f4bf84247b88e546ac9f7a1c04b` | 9675 | PASS |
| `actions-20261021-234409-884-c713f3eeb466446f8120542c0180fda8` | 7104 | PASS |
| `prologue-pre-20261021-234443-952-7d2a1959e467446f963222352aee1253` | 99678 | **FAIL** |
| `prologue-post-20261021-234649-551-0a05e994a32848cf930e6f6f41f7bce6` | 147175 | PASS |

**9PASS/2FAIL。** 全11 engine exit0、无超时、guards0；所有build为普通七参数、0警告0错误。退出码不覆盖严格失败：

- void：10行为检查通过，但stdout退出有**一条**ERROR报告187个`P12GodotShape2D` RID未释放；stderr空、external无此句，不计成187次或镜像错误。
- pre：stderr有5条`Handle is not initialized`，均`SwapGCHandleForType→Node.AddChild→BasinSupply.OpenSample:459`；external五个29行块逐行镜像，不计成10次。PHASE_OK与drain标记不构成通过。
- post：75原用例、0退休、2次替换/0退休和8次同owner实例nodes1→1通过。4条受控WARNING为seamless1/room-profile1/flow2，外部镜像不计成8条；无ERROR/RID/FATAL。其余8案三个引擎日志均无ERROR/WARNING。

55/55组source/user/helper/binary/continuation前后清单完全相同，native PID/参数/wait/provenance相符。跨11案源1702/872C#共同SHA256：`FF15A2041F084307DACB29203B710139B35E98C596BC271D90BDF4F006C5C7E0`；6项binary清单共同`F40D510BB06F800AC30C3A1C87911D10876DAB5CDD4086979F6789B7733EF202`。共同9项helpers为`076F132B90E4DDD4E72A600369A4D8EA804E7CA703C351AAD7B6A8597CAE6AD0`；pre额外原clearance helper使10项清单为`0AE6B62FAB4B80516CEBFD117100812640525C8798E8164923DDF07038D62F2A`，对应文件未变。109项真实用户保护清单只校验身份/hash，不读取存档语义。

## 推理

本轮有生产实现、真实保存叶和原生装载/回滚证据，修正了两个实际重入权限漏洞，不是重复诊断汇报。但不是完整Task3：尚缺全部9类跨卸载HP/姿态连续性、全部50正式位置/活动/攻击/返航与历史，不能把AABB走廊或一次身体查询称为完整证书。两个资源失败的具体所有者和根因仍未证实，不能据数量巧合归因，或据本次无FATAL宣布旧FATAL已修复。

## 风险与后续边界

完整[1–11待办](2026-10-18-project-remaining-backlog-v001.md)不变：全部敌群/家族/位置、剩余生命周期、最终视听、同版本正常Main/Explore、存档、720p–4K/真实60FPS/真人试玩、Windows包与授权GitHub存档均保持OPEN。历史Handle/FATAL、形状RID、音频退出等仍是发行门。

无GC/cache/KeepAlive、额外wait/drain、借用Dispose、引擎/依赖改动或日志豁免；无新付费生成、自动化、前台输入、媒体、Git写入、ZIP/标签或删除。HEAD仍`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，index SHA仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。全部消费者终态后才更新本文。一次手动媒体检查为4838B/基线3508B/增长1330B、Notify=false，不初始化基线、不设自动化，20GiB只随正常汇报提醒、不停止工作。
