# v108：飞行敌人真实几何与交叉火力防守

2026-10-25，`G:/knight/Knight`。本轮是原十一项目标的局部进展，不是完整序章交付。用户“没看到窗口”保持 NOTSEEN；教学遮挡和画面未验收，本轮没有开窗、系统输入、新图稿或录屏。

## 事实与最小修正

上一轮已有证据改变了下一行动：短喷实际接受、反向停稳，但 physics6972 飞行敌人先被选作危险源，右侧枪手的贴身子弹未能替换它，盾朝上而受伤。

同一次扫描没有漏弹：两个攻击都是 Active/urgency0。Moth 自己已经发布世界空间半径22的攻击圆，自动试玩却未把其叶几何接入比较，形成 Infinity clearance；后来的子弹 clearance0 不能与未知值比较，因而保留旧赢家。

先增加独立实际 Main 断言：从该活体 Moth 的实际发布叶读取同 AttackId/Active 的圆，核对候选叶身份、世界形状与有限 clearance；断言在隔离回调之外。真实 RED 是055056案：physics6969、attack2，候选leaf0而已发布leaf4776624394400、Circle22，构建成功且非超时，没有以编译错误代替反例。

随后仅补接一个借用叶：Moth/Active、正AttackId、同攻击同阶段、节点有效且在树内未排队删除、Circle；失配或未知继续旧回退，不保留原生对象跨样本。没有改时间优先级、未知排序、任何 counter clock、原900循环、完整J+6+2、24能量储备、真实Shot/K/致死命中证明或生产难度。

## 实际防守变化，以及仍然失败的部分

修正后的 Main 055511案：

- `MIXED_MELEE_GEOMETRY samples=13 mismatch=False`；178个同遍历观察pass、601行、丢弃0、观察错误0；仅180tick观察窗，不声称整战几何覆盖。
- physics6971：Moth actor4774208475845/attack2 的真实圆 clearance87.09265；owned Burst actor17379820050325 的 projectile17700969515842、Circle5、clearance0，实际替换赢家。
- 同物理帧先记录旧采样盾朝上，随后实际输入/护盾方向朝右，`guarded=True damage_amount=0`，HP保持3。这次确有实际成功格挡，不只是断言不再触发。结合贴身弹替换赢家的记录，可判断方向误选得到改善；但该护盾回调标记request_source=unknown，不能只靠回调单独认证格挡来源的弹体ID。
- owned只在6814被J命中一次、HP2→1。其实际开火6948/6959/6970/7113，但后续未完成反击，player_connections=1/defeated=0。
- 7067、7089能量0/盾关闭，resident4775902975190子弹使HP3→2→1；7114由owned第四弹致死，恢复请求1/完成0。原位移重置断言拒绝4751.553,3423.933→1680,3960，距离3117.98，严格FAIL。

独立排除另一个猜测：虽然 Rust 时钟未筛空间，但同pass仍有 resident Burst 预警0.39999998，小于原0.43333334门；Rust控制器还叠加damageImpulse，不能仅凭模型主动速度0假定整段静止。本轮不排除它、不放宽攻击窗口，不宣称已解决能量耗尽或反击可行性。

## 全部八案，不拼接旧版通过结果

下列目录统一位于 `G:/knight/验证证据/october13-stable-claw/`。八次构建均exit0；八次原生均非超时。最终七案同一冻结源码；每案整个运行期间 source/helpers/docs 均冻结。

| 目录 | 构建ms | 原生exit / ms | 严格结果 |
| --- | ---: | ---: | --- |
| `thruster-route-main-diagnostic-20261025-055056-721-bb0736971ac24031bbeea96105319c19` | 28966 | 1 / 135270 | FAIL，缺几何实际RED |
| `thruster-route-main-diagnostic-20261025-055511-038-14bd0ad6a9054c7ba21607e818ad2280` | 26847 | 1 / 135417 | FAIL，几何/该次格挡成立，后续死亡 |
| `thruster-route-exploration-diagnostic-20261025-055803-936-7947275fb60c4baa94fa722e0a2240a6` | 29079 | 1 / 57230 | FAIL，2860恢复1/0 |
| `ranged-mixed-resource-20261025-055942-235-4129288ba16a4b23a0f53a4fb8e7dcff` | 28230 | 0 / 16124 | PASS，原mixed四夹具 |
| `ranged-mixed-counter-20261025-060035-220-35defd5559564c8ca24b1bcadc25e7ce` | 30927 | 0 / 4200 | PASS |
| `prologue-pre-20261025-060119-478-165123e632dd4623945ec49f39af772e` | 30770 | 0 / 161850 | FAIL，1条Handle未初始化ERROR |
| `prologue-post-20261025-060442-198-065c8ef6a8aa45cbb9cfaec0e2787a58` | 26198 | -1073741795 / 161598 | FAIL，ProfileMissing/Title及FATAL |
| `models-20261025-060801-033-ee5ee2216bf14dd98df861d1f90b096f` | 36350 | 0 / 1190 | PASS，最终普通Debug恢复 |

合计严格3PASS/5FAIL；最终七案3PASS/4FAIL，不能称绿色suite。

pre虽然输出PHASE_OK/进程exit0，stderr仍有 `GCHandle.FromIntPtr→SwapGCHandleForType→Node.AddChild→BasinSupplyRuntimeTests.OpenSample:463` 错误，严格保留FAIL。

post在资源标签夹具准备时，low-ceiling-bridge.03导出Profile缺失，状态Title而非Playing，随后 `gchandle.is_released()` / GodotObject.Finalize FATAL。原75案例0enter/0return、原owner重复收据0；只取得seamless与room-profile两scope，缺flow-registry。外层缺scope提示是执行未完成的后果，不能当格式误报。普通符号不能确定本案FATAL的具体自定义CLR资源类型；历史REF归属不能移植到这一案。

## 一次审查、裁决与延期

新上下文审查对已认证v107快照与本轮单文件差异给出C0/I0/M1，无第二审查或纠正回合。Minor延期：这条断言可在180tick内没有Moth样本时空过；本案13正样本/0错误已由实际收据核实，但未来路线移动时应明确报“未执行”，不应把空观察当验证。此项只影响未来回归证据强度，不否定已经观察到的RED和格挡；不强迫无Moth的其他遭遇出现Moth、不扩等待预算。

审查逐项未认证事项，root裁决如下，均不静默删除：

1. W读取/控制器边界：未改，原义务保留；本轮不授新资格。
2. 首物理命中遮挡与接收者资格：无新resolver/ray，不据几何圆证明瞄准或穿墙规则。
3. Intro/准备与Playing：未改，post恰因准备失败仍严格拒绝，不能强置Playing。
4. 回调/延迟退出中的actor/parent/pose/sequence：只核同步借用范围，广泛生命周期仍原要求。
5. LoadLevel主异常/清理异常聚合：未改，不据这一patch关闭。
6. 全局完整J/能量/冷却/所有威胁可行性：保门槛且Main仍失败，尚未解决。
7. 实际Shot/致死终态/900完成：保原证明；本案Shot发生但owned未Defeat，不授通关。
8. stale/noncircle/nonMoth完整运行矩阵：检查了回退条件，原圆形优先级夹具运行，但没有声称覆盖全部退出竞态；扩大用途前仍需真实负例。
9. Main/Explore与同时输入：实际两路FAIL；只证明6971这次方向采样及时挡弹，不泛化到全操作。
10. 资源根因与全回归：pre/完整post严格FAIL，最终models通过不抵销；不猜GC/缓存/Dispose政策。
11. 原76普通敌人、Boss/NPC、正式视听相机、真人硬件和发行：完整目标仍ACTIVE；NOTSEEN有效，测试和审查不等用户认可。

## 保管、成本及未变边界

独立核对八案共40/40清单配对、48/48封存二进制，无缺失或坏项。最终七案source清单相同，SHA256 `E6C40D3D477F3DA5D7C9CECF3F3608C2918FE97A3969F6900CBE2D443812E9ED`，1844项；live逐项missing0/drift0，最终普通Debug6/6匹配。相对v107新增0/删除0、仅Main测试C#变化；该文件当前SHA `A9E8CDE44EC159ECAE314A2E289ADCBACC993E5FB469050BC525D03C70108FC6`。helper因phase为9/10项不同集合，各案配对相同，不冒称全部清单字节相同。

canonical owner `A864A34C13ABD1787E5D12373D4C67EE6635FD0B921252F8BAA0FA1D589B23E8`、原总线 `CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A`、分支HEAD/index均未变；diff-check exit0/已有LF→CRLF提示169/其他0。首个test patch因空hunk被原子拒绝，无部分修改；少数只读路径核对未找到文件不计原生测试或行为反例。

全部root句柄确认终止后才更新本文及状态文档。无生产物理/数值/地形/人数/正式图声变更，无Git写入、删除、付费、自动化、真实存档语义读取或前台OS输入。八案的实际成本和全部失败保留，不重复相同Main搏安静结果。

既有媒体检查只执行一次，UTC2026-10-24T22:09:57.8429536Z，Current860284/Baseline3508/Growth856776 bytes、Notify=false、新媒体0；不建立通知或停工。

## 下一步

当前可用事实是“方向误选修正并挡下一弹”，不是“该战斗完成”。下一步仍需解决全局实际反击机会与耗能，以及BasinSupply/标签准备的资源寿命错误；不能靠再猜预警常数、删来源、回填缺失Profile或扩大等待预算通过。正常双路、死亡/继续/出口、正式分层视听动作/相机、真人硬件及版本交付保留原十一项要求，未标目标完成。
