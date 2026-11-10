# v250 · 巡逻三态诊断，原问题仍开放

## 事实、推理、风险

事实：只改三个测试/验证文件，增加调用原完整巡逻控制的隔离入口、三组已有结果的纯值记录和两个真实wall注册前提。原27条Expect、两处跨态SequenceEqual、原同态差分、位置/等待/时限全部保留；没有生产动作、动力或地图改动。隔离入口通过；唯一一次原完整geometry在到达三态记录前发生原生终结器崩溃。当前六案五严格通过、一失败，**不是完整回归通过，也不是故障已修**。

推理：本次隔离状态下加入wall确实增加两条中段危险理由，移除后恢复原有序结果，fresh与batch一致；这支持新增诊断能忠实记录本样本，却不能解释v249的真实跨态失败。没有原失败数组，不能认定“只是排列差异”，因此本轮不做条件语义修补，不放宽断言、不改生产排序/缓存。

风险：原匿名finalizer故障和Main899尚未解决。隔离fixture不等于真实玩家、全50驻留census或全76敌人覆盖，局部通过不代表自然路线/正常速度美感/人工验收；旧NativeFixture及原内部finally也未得到全生命周期安全证明。原十一项、正式美术动作、惯性/特色动作/物理体验R-I/R-C/R-P继续ACTIVE/OPEN。

## 实现与真实三态

- `tests/integration/CanyonResidentPlacementTests.cs`增加`RunPatrolWorldWindow`，按原Initialize/Mount/Sync/StopCallbacks/native及controlled前提执行，再调用原完整`RunPatrolEndpointReuse`；外层保留收到的主异常与fixture清理异常。未承诺原方法内部旧finally也能保留所有主因。
- 仅复制原Compare已经计算的fresh/batch body与四段corridor：有序issues、bounds、clear/proof、transform与count。至多三行、fault计数明确，不重复生产checker、不持native引用。原208 cases/20 comparisons计数不改。
- 两个新增`HasNativeHit`使用已恢复为64高的私有shape、真实同wall ID和原Add/Free后的Sync，只证明wall注册/移除前提，不替代生产危险断言。
- 新runner/host phase为`thruster-patrol-world-window`，普通headless/55s/1888输入，原错误/退出码/超时/泄漏门保留。实际host consumer先缺失RED，之后两正十七负GREEN；本轮复核仍是2/17。此schema控制不是条件集合语义修补的正负控制。

隔离收据位于`G:/knight/验证证据/october13-stable-claw/thruster-patrol-world-window-20261105-025900-985-804248744853440285d07d156d556695`。真实wall `80580972158`，added=True在physics36/process15，removed=False在physics41/process19。三态均freshCalls9/bodyChecks2，四段顺序0–3保留，fresh=batch：

| 原状态 | body有序理由 | 段1/2新增wall理由 | flags及边界 |
| --- | --- | --- | --- |
| current_player_only（P28） | reserved.Protected:current_player → native_current_player_body → native_current_player_hurtbox | 无 | clear/proof均false |
| 加入foreign wall（P36） | 同上，顺序一致 | 两段均含corridor_native_world | 原bounds不变 |
| 移除foreign wall（P41） | 同上，顺序一致 | 消失 | 全有序理由恢复初态 |

body及零段0/3为`[10982,968,36,64]`，段1/2为`[10982,968,164,64]`。clear=false源于受保护玩家，不是可以在此安全生成的证明。END rows3/faults0，原208/20及最终严格范围收据均唯一。

唯一原geometry收据`resident-geometry-20261105-030029-754-b6ab09a1a93c4066ae28899b574fd9d7`：9330ms、native exit−1073741795、非超时，`gchandle.is_released()` / `mono_object_disposed_baseref` / `GodotObject.Finalize`，WORLD_STATE零行。保留739B stderr及原日志，不把历史诊断对象serial套到本次；不再重跑碰运气。

## 当前同源相关验证

下表前两案是本轮诊断本身，后四案是本轮独立普通回归；并非重跑v249全19或正常双线。全部build exit0/native非超时，五PASS案stderr0；geometry失败原样保留。

| phase | 收据目录尾部 | engine ms | 严格结果 |
| --- | --- | ---: | --- |
| thruster-patrol-world-window | 025900-985-804248744853440285d07d156d556695 | 2863 | PASS，208/20、3 rows/0 faults |
| resident-geometry | 030029-754-b6ab09a1a93c4066ae28899b574fd9d7 | 9330 | FAIL，native finalizer，0三态行 |
| models | 031020-964-7996867e3f634009acf63e78b9cb2d29 | 546 | PASS，仅该入口爪/到达子集，不是全部模型 |
| keyboard | 031102-570-fbbf786918bd4ea2a10573bcaae6c1e0 | 20373 | PASS |
| resident-save-compatibility | 031214-508-21aa1ee2d752497397f3176f9c808cb3 | 12970 | PASS |
| thruster-profile-trace-normal | 031307-470-7bb0fa6d062e47348199f4a427eabd30 | 446 | PASS，普通构建，不是所有资源寿命安全 |

相关批root session5873已终态exit0，四案4PASS/0FAIL；不能据此抹掉前面geometry失败。scoped20926、geometry74834均已终态，旧v249批77858不重启。原Main899仍开放，当前Main/Explore/pre/post没有重跑，不沿用历史v249结论冒称当前整套绿。

[六案机器绑定](../../.superpowers/sdd/2026-11-05-patrol-world-state-v250/bindings-v250.json)：30组source/helper/user/continuation/binary前后文本对零差异；当前1888输入、6个Debug二进制及12 unique helpers重新哈希零差异。统一source manifest `D97BCA89036B35E0CC5B6EFC2351E7784E5D718A2F00DB334F7723911A75A315`，binary manifest `7A55E01912492628659C8FC097F736958A0A8EB1E57262983349887FD661EA79`。Knight.dll `E30657E46EA1DBCA33D882FDEDFE0DDD203B0C8A9EF7553FBF81DD400BAF9048`；这是新测试构建，不能与v249旧assembly混称同版。

## 审查、选择及下一步

一次fresh增量审查C0/I0/M1。原控制/真实wall/数据上限/新增外层异常传播成立；Minor为host未强制freshCalls/bodyChecks且bounds布尔值可转数值，当前C#及原native断言仍正确，按规则延期，不顺手修、不二审。继承旧Minor保留。[全部11项declined裁决、三项原裁决及风险](../../.superpowers/sdd/2026-11-05-patrol-world-state-v250/final-review-v250.md)。dirty原地无Git写/commit/删除，原branch/HEAD不变。

用户U-20261105-01：旧V1批准body/mantle原件暂找不到，停止扩搜，后续重新审阅现有候选；不是批准当前PNG/分层/总装或动作，不自动沿用旧批准，也不新增付费/开窗权限。

下一步仍是完整原序章：资源故障须有真实对象/所有权实证才修；主线须解决原899下的真实遭遇交接，不放宽HP/能量/单跳或通过条件；T01大型销毁区需专门完整空间与安全路线设计，ControlledAbyss、已有返回路线和80px小缝不替代。正式分层身份稿、连续特色动作、正常速度视听/硬件及用户体验仍未验收，不能以这轮诊断充当画面提升或正式交付。

媒体正常汇报前单次Check：7,427,750B/86文件，Notify=false；无新增媒体、GUI/OS输入、付费生成、额外dump、GC/KeepAlive/cache/引擎调整、自动化、真实存档改变、外部发布或删除。
