# Bell 地形视线查询的调用内所有权 · v336

KNIGHT原完整十一项ACTIVE，G:/knight/Knight，仅无窗口；上一轮为实际进展但当前16阶段10PASS6FAIL，原十一测试8PASS3FAIL，全部失败保留。本轮不改HP前提、路线控制、数值、验收门或地图，不猜造Bell移动/受击缺陷。

## 事实、推理、风险

读取当前Motor/ThrusterDynamics/Bell/model/hitbox及两份真实战斗后，未发现可证的Bell自身移动或K后新J受击缺陷，不能擅改物理。独立确定的生产缺口是Bell.HasClearTerrainPathTo每次创建PhysicsRayQueryParameters2D、exclude Array backing和IntersectRay Dictionary而不调用内释放。Rust同类代码已使用三项scope，已有无新增帧/GC的原生ObjectCount控制模式。

先验证真实Bell方法连续同步调用的最大native ObjectCount增量应≤0：旧方法若返回仍留一个query，精确功能RED；不是以日志文本、语法或编译失败充当反例。额外查询会扰动分配但没有额外物理步/GC或输入，不用这个受控fixture替代正常显示/自然整图。

## 方案与边界

只对query、自己创建的exclude backing、查询返回Dictionary建立调用内scope，World mask/Bodies=true/Areas=false/HitFromInside=true/self RID exclude/射线端点原样保持。不借用Rust的全mask/Area/自有碰撞树遍历语义，不改攻击、弱点、伤害、实际伤害形或动作。

不选GC/额外等待/缓存/引擎替换：它们改变全局时序或资源模型，并非修复这项确定的局部所有权。不调参或移动地形：当前尚无物理异常证据。持续开发授权下这是开发者技术裁决，不冒称用户新增批准；没有需用户选择的美术/付费/玩法范围问题。

该修补最多证明局部native query在返回前释放，不能证明GCHandle/FATAL/ShapeRID根因或Main/Explore失败已修。旧melee/Bone/Ghost不因本无关源变更重跑筛绿。保持已批准角度优先与联合落脚规则、原时限和严格日志，历史失败不可重判。

## 证据

改动仅scripts/enemies/laborer/BellBackLaborer.cs和tests/integration/BellBackLaborerSightRuntimeTests.cs。private生产方法通过测试内reflection调用，不新增生产test API。两朝向、开放/实际墙阻四个64-call控制核真返回值、原生身份/位置/状态/HP/Physics/Process不变及最大保留量≤0。保留完整旧11个BellSight fixture的实际世界遮挡、已承诺攻击及原生重叠/伤害/filtered retry；旧armor/claw兼容亦执行。

真实RED后最小生产scope→同原生阶段，再冻结同版原十一测试＋保存独立cold一次及现有Bell资源门。任何错误/崩溃/泄漏仍FAIL，不因native0/局部功能成功豁免。一次fresh全增量审查，C/I一轮TDD修补、Minor留账；当前源/产物/109保护行104文件/助手与审核pin审计及一次近终态媒体检查。原完整目标/正常AV/正式美术动作/硬件真人仍OPEN。
