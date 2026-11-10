# v252 · 扩图存档迁移、独立历史夹具与连续回归

状态：本轮实际生产修复已接入，最终同版12项检查为**9 PASS / 3 FAIL**，非整章完成/正式交付。工程G:/knight/Knight，原十一项与R-I/R-C/R-P仍ACTIVE/OPEN；无UE切换。

## 玩家数据行为的实际改变

当前158地图现在可加载四个此前支持的旧探索版本：紧邻扩图前的Baffle144，及B264/Shelf143/Post143。后面三种先按原严格解码/原delta到144，再走新增的摘要认证144→158迁移，不只支持最容易通过的一种旧版本。核心进度、检查点、收集和Boss标志不制造、不清空；加载源文件只读。

旧已探索的未变区域保留；新增附室、上唇/西岸接触cells和新1440条边保持黑暗。8px边缘接触采用保守清除，可能需要重新探索极少量变更边缘，不扩大到全地图遗忘。未知revision仍拒绝可选地图，不猜测旧几何。

新目标固定为158 solids/13908 boundaries、960×540、完整目录SHA D6EC6A12A6D3CF6BDB0C1A9C981565A6EC43D901BB8175C987DFE0A19ADC61CE。新映射有完整旧144固体/12468边身份比对：旧0..6663边保持ID，6664..12467加1440。不是单纯给旧位串改标签。

## 实际验证与未解决项

原存档完整暖/冷入口131224严格通过，warm11043ms/cold1501ms，writer52952/reader73752。当前144零/满/稀疏、20个异常/伪目标控制、三个真实旧wire独立literal链、原28控制/Post/Grip8原期望、实际158 stage/reload/links和原六旧冷见证均保留。最终同源132805再次严格通过，warm10924ms/cold1500ms，不使用已过期源资格。未用真实玩家存档作加载/写入实验，保护流程仅做哈希核验。

之前连续附室的特定managed Ascent PackedScene disposed错误源已修：helper不再Dispose借用cached场景，实例仍finally释放。131405连续原aggregate实际通过32059ms；但是最终同源132854在两条绕行receipt后出现原生`gchandle.is_released()/Dispose/Finalize`崩溃，exit-1073741795/native30983ms，**当前aggregate严格FAIL**。不是同一种managed异常；不以旧PASS冲销、不推定原生根因、不重跑筛绿或新增dump。

组合机关当前回归通过，shape158与raw body157区分。居民环境从85变91（新增池/移除旧32px恢复带使Hazard17不变、三段产生6个launch/landing）；实际63环境检查、原57交互/19来源/总110及50body/50activity已过。132258原geometry仍在两条旧endpoint SequenceEqual失败，未改208/20或原因顺序来制造PASS，旧匿名FATAL及222 RID历史故障保持OPEN。

对132258三状态的只读比较：20个跨态part几何/flags/transform差异0；wall加入时4part（fresh/batch×两个中间方向）合法新增`corridor_native_world`，其余16part仅原因排列不同。两个具体失败涉及mixed.Body排列与wall移除后各corridor排列。此证据指导下一步，**不等于所有world差异只有排序、缓存正确性已全部认证或原native崩溃已修**。

当前普通pre135830ms/post174294ms、keyboard/actions/structure/爪模型子集通过；Main149303ms仍原899失败，Explore310166ms正常plan通过Boss及实体支撑出口、恢复0/0。Explore不访问新附室，不证明全76/全机关。历史v251通过不代替当前资格。

| 当前同版检查 | case时间/短ID | native毫秒 | 结果 |
|---|---|---:|---|
| 原居民geometry | 132258-730-31fc4278 | 26185 | FAIL：原两处跨态SequenceEqual |
| 完整存档暖/冷 | 132805-859-7b78c8c0 | 10924 / 冷1500 | PASS |
| 连续附室aggregate | 132854-138-9b05023a | 30983 | FAIL：原生终结器FATAL |
| 原组合机关 | 133003-409-2ec98422 | 24710 | PASS |
| V2结构/33反控 | 133057-167-4356b7d6 | 1375 | PASS |
| 爪模型子集 | 133130-432-8ba74994 | 533 | PASS |
| 键盘 | 133206-288-8de75070 | 20096 | PASS |
| 动作/技能技术检查 | 133307-805-a8078325 | 9434 | PASS，不是美感认可 |
| 完整普通前段 | 133348-633-8cc7588e | 135830 | PASS |
| 完整普通后段 | 133636-891-67bc9fdf | 174294 | PASS |
| 正常Main | 134002-792-da0fa219 | 149303 | FAIL：原899未清 |
| 正常Explore | 134304-117-8119886b | 310166 | PASS：Boss/支撑出口/恢复0 |

全部case在G:/knight/验证证据/october13-stable-claw/按phase+完整时间+UUID保管。当前geometry只采用已完成的132258案例，不另跑筛绿；其余11案由81372一次串行完成，终态exit1/明确failures2，加geometry共3失败。Root当前无owned live build/native，不重启旧handle。

60对source/helper/user/continuation/binary前后清单全部相同；12案source与binary manifests各唯一1。当前1896个源/资源/导入输入、完整文件集、6个binary与14个helper独立重哈希0差异。媒体正常报告一次Check成功，7,427,750B/86文件、Notify=false，无新增20GiB档位。

## 历史测试没有冒充当前地图

历史144目录以独立gzip值fixture保存，解压354960 bytes、完整SHA CD66FCE70550F1D5414634B3380D2E73A98ADA2963F94EC104976AE6D0DE3566。旧143/144字面位图控制由该固定历史值逆推，不再从live158逆推或重标。

实际当前五字段authoring仍从真实158叶开始，保留附室和baffle，核实际完整SHA、snapshot/两输出pack/reload/links/不可写canonical。旧专用143同步器未放宽。Basin工厂精确60个新建shape，Ascent精确66（移除旧恢复32px、补已有4×21 baffle），保路径/类型/尺寸/nativeID断言。

Root曾误读Ascent诊断为65，错误版本已原样留证；实际是66、少一个旧恢复带却多一个既有baffle。根据真实factory直接new代码与实际missing/extra更正，没有把错误假设保为通过事实。全部过程、每条裁决及其代价见[本轮ledger](../../.superpowers/sdd/2026-11-05-annex-exploration-compatibility-v252/progress.md)。

## 主线错误字段的解释纠正

独立RO核对旧Main122456日志与控制器：终端`actor_retired/last_actor/enemy_health`来自最后被观察的旧Skitter，不是实际剩余激光敌人。旧Skitter在loop866已HP0/queued；loop899结束时新批次仍arrival/live0，随后三cleanup帧才出现新HP2激光敌人，未进行新战斗绑定。旧终端缓存HP1不能当作“剩余敌人只差一血”的证据，energy末值也不代表起跳/攻击门当帧。

这不证明数值/角度/输入有缺陷，也不解释前面耗时；当前不调整899、HP、能量、敌人或自动续按。后续以实际新批次与有效输入窗口继续归因。原Main未通关资格不变。

## 审查、资源与权限

唯一fresh增量审查C0/I0/M1，允许保留有界迁移改动，非游戏Ready。M1延期：新增factory审计未传borrowed对照，空集合的borrowed_disjoint收据不能证明canonical隔离；Basin Stage实际有对照，Ascent内部ID唯一不等于外部隔离。全部10项declined与执行者裁决见[完整终审记录](../../.superpowers/sdd/2026-11-05-annex-exploration-compatibility-v252/final-review-v252.md)。无C/I修补、无二审，不借机更改GC/cache/KeepAlive/引擎。

| 本轮资源/入口 | 来源 | 状态 |
|---|---|---|
| 当前探索迁移 | `scripts/save/ExplorationMapSaveCodec.cs` | 实际四源暖/冷通过；不是任意版本通用迁移 |
| 历史144值夹具 | `tests/fixtures/HistoricalMinimapCatalogFixture.cs` | 测试专用90,980字符压缩值；运行时不依赖 |
| 实际当前迁移与负控 | `tests/unit/AnnexExplorationMapMigrationTests.cs` | 全cell/edge、旧wire literal/冷读及20反控 |
| 正式角色/披风/连续动作 | 原现稿、待后续重新审阅 | 未新增正式美术/动作，不转授旧V1批准 |

本轮没有新图稿/视频/付费生成/GUI/OS输入/额外崩溃采集/自动化/Git写/外部发布/删除；所有case保留，真实存档不动。媒体只在正常汇报一次Check，未因20GiB提示停工。
