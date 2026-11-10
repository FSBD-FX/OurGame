# 玩家保护区延后出生与相机准备门 · v068

2026-10-23，Asia/Shanghai。仅 `G:/knight/Knight`；完整原十一项目标保持 ACTIVE。本轮不是正式试玩交付，也没有新最终美术、音频或录像。

## 事实、判断与实施边界

旧相机夹具预置玩家、禁用输入，只等八帧就要求 Motor 自动运行。新增人口准备期间，真实 Flow 会关闭 Motor，直到 Prepare/Commit 后才进入 Playing。因此该断言失败不能直接归为相机数值错误。

两个预置点与真实居民的玩家保护区相交：盆地 `(3915,3759)` 的 Grow64 区与 steam-valves.01 右巡点身体交 21×11；旧高台 `(4780,1283)` 还与 split-level.01 的实际身体重叠。角色被冻结、出生等角色离开、开局又等全部出生，构成等待环。

本轮开发决定：隐藏准备时，只有非空、全部为下列精确保护拒绝的 slot 才可留待 Playing 后补生：`reserved.Protected:current_player`、`start.reserved.Protected:current_player`、`end.reserved.Protected:current_player`、`corridor_reserved.Protected:current_player`。分支在真实事务返回后的授权复核之后。slot 仍 Alive/未Attached；不手工出生、不改50个身份，不取消玩家保护/离屏检查，不更改经理的环境、pending、身体、走廊或两次原生准入。物理碰撞、混合错误和异常消息不获此特例。

不采用增加固定等待、删除敌人、关闭 manager/AI 或强开 Motor。这里只解决玩家保护区导致的隐藏准备环，**不是证明所有延后 slot 的其余几何已合格**：现有 CheckCorridor 端点错误会提前返回，潜伏中段错误及 active Reconcile 失败不通知 Flow 的风险仍 OPEN，须在完整稳定性工作中闭合。

## 实际源变化

- `scripts/prologue/residents/CanyonResidentCoordinator.cs`：上述有限延后，正常活动调度仍走完整经理事务。
- `tests/integration/BasinAscentCameraRuntimeTests.cs`：等待真实 Playing/Pending=false，保留50布局；三个支撑点仍检完整胶囊、原生向上接触、±.25位置、16px屏幕边距、同玩家/世界及零恢复。
- `tests/integration/PrologueCanyonTraversalTests.cs`：同款旧相机夹具复用真实准备等待。高台夹具由4780改为同一 `AscentFinalCatch` 上的4870：胶囊4850..4890完整位于4660..4900，避开原物理出生重叠。**4870通过不等于原4780通过**，正常键盘路线相机仍须另验。
- `tests/diagnostics/Task14NativeResourceIdentity.cs` 与相机测试中的条件编译标记：仅记录原夹具构造/挂载/清理的标量边界，无追加 Load/GetState/Instantiate/GC/缓存/资源保活或等待。

没有修改生产HP、伤害、敌人AI、力学、地形、保存或资产。共1772清单项/888 C#，没有新增编译源。

## 原因验证与修正过程

全部证据根：`G:/knight/验证证据/october13-stable-claw/`，失败原件保留，不反复重跑求安静。

| 案例目录 | native exit/ms | 实际结果 |
| --- | --- | --- |
| prologue-pre-refcounted-identity-20261023-125152-831-8dd989b51950476690ab348c88bccf68 | 1/27422 | 首夹具完整构造、挂载和清理返回；旧Motor断言失败。本次无FATAL不算修复 |
| ranged-camera-ground-20261023-130709-686-33eb1a05fddb4074965d420dee2b1466 | 1 | 普通恢复构建，仍旧Motor断言 |
| ranged-camera-ground-20261023-131014-141-a7a1c1c080694e85a8dff2c3d8d01ee6 | −1/55037 | 只改真实准备等待后原生超时；没有证明生产180边界已走完 |
| ranged-camera-ground-20261023-131316-077-637556b2d1b641c0bc05c4f56be77d13 | 1/36318 | 第一版保护延后使前两例过；旧4780实体重叠仍等满180后回Title，未放行实际碰撞 |
| ranged-camera-ground-20261023-131724-401-29b41cca2e494c8da4750ec748b485ea | 0/28643 | 精确四类+合法高台位置，三个相机案例通过 |

首诊断覆盖：1206原离树探针根/7236完整操作阶段，4824观察、960正序号，1279 Dispose ENTER与1279 RETURN，无未配对、无同观察NativeID对应不同正序号；11条assigned路径两代为不同NativeID且旧代释放先返回。它只能否定覆盖内的特定重叠假设，不能证明所有内置包装、引擎句柄或普通路线均安全。五对清单及六封存bin自洽；诊断资格不转普通行为资格。

## A冻结：相机及九门回归，10案8PASS/2FATAL

| 案例目录 | native exit/ms |
| --- | --- |
| ranged-camera-ground-20261023-131926-413-7d6cd5e70f234d3da2b61c5b883e320d | 0/35871 |
| resident-auto-start-20261023-132140-918-e68f4558b4c842cd849c9834880693e5 | 0/5378 |
| resident-auto-session-20261023-132220-348-a3bdd05c803443e7bb27f88ad520e9b6 | 0/14640 |
| resident-auto-stream-20261023-132310-558-63d73cbcaaa04bb688f5c47b25931f5b | 0/21326 |
| resident-auto-teardown-20261023-132405-806-1e3920d2e5ae4bc1aa42a76b355bd6a1 | 0/5823 |
| resident-level-lifecycle-20261023-132446-400-c2d1e70b793446b28292e1b7f1e3406f | 0/5634 |
| prologue-pre-20261023-132527-154-f8c85b15eb93423e90da21b9dbd8c3a6 | −1073741795/13199 |
| prologue-post-20261023-132614-106-de988733cef041e488b5c83206765b46 | −1073741795/25010 |
| keyboard-20261023-132711-937-51a78783fb954206a8d4b861576cad36 | 0/9709 |
| actions-20261023-132757-790-76fdd2d2db56453a827d106d39c4e3aa | 0/7222 |

实际正常24、取消/Continue/死亡恢复、跨区50身份出生历史、伤后2HP保留、死亡不复活、准备中退出和经理事务保持。这些不是正常全关50实战。auto-start有一条严格限定的重复Intro负测WARNING，其余PASS原始日志无未豁免诊断。两失败均 `gchandle.is_released()` → Finalize → GC.RunFinalizers；post外层先报scope收据缺失，但原生实际FATAL，不能写75通过或仅scope错误。

独立只读审计：50对清单一致、60封存bin匹配、10案1772/888同源同bin、全部普通Rebuild0构建警告/错误、guards空、无超时/额外DefineConstants。source清单SHA `AB237BAD8A568DCEF633445D7589EB1E2744E1E598254E6B618C973A5E73056A`；六bin清单 `E15CE84A21765D344D8885853BC75BDDD822952B2193D5F8962BB1A2C5E3BACD`。user109行一致，continuation为空清单，不是一个续玩文件。

## 独立审查修正与B最终冻结：4案1PASS/3FAIL

审查发现补生测试只比较缓存world自身、计数root未与slot提交形成完整收据。已一次修正：从level路径重新解析当前World；要求计数root的作者subtype/坐标/实际当前sector父级；对该root走真实Health伤害事件，只有目标稳定slot由3变2，其余49项血量不变。这是受控保管证据，不是键盘实战。A与B不是同一冻结，不合并为一套绿测试。

| 案例目录 | native exit/ms | 结果 |
| --- | --- | --- |
| ranged-camera-ground-20261023-132937-172-97a1a71ee73f4874b67b3c7737ef41df | 0/32114 | 36相机样本、延后近身不生、真实public checkpoint转移后恰一次自动补生、精确Health收据 |
| prologue-pre-20261023-133043-359-ca766ad147ee4d659dcbe07cf966a6b7 | −1073741795/26997 | 原生FATAL；已完成首相机/补生收据后，未完成全pre |
| prologue-post-20261023-133144-058-3df95803e3364fa69a12dc85dd7c1e78 | 1/159222 | 无FATAL；三相机后暴露旧死亡夹具held-input断言，未完成75/收据范围 |
| thruster-route-main-diagnostic-20261023-133455-807-7583293ee32c4d2b98a96bbad1e14fe9 | −1073741795/54071 | 旧Abyss6出手/6连接、HP4→4后FATAL，未到Gear；最新冷却策略仍未评价 |

B相机三处最小屏幕边距284/184/184；全部原生向上接触，零恢复。转移为隔离fixture调用真实World checkpoint API，不是手工TrySpawn、键盘行走、正常Continue或真人体验证明。

Bpost真正失败为 `PrologueLevelRuntimeTests.GeneratedMainDeathDefersRestoreAndPreservesRecoveryClosure:240`：`recovery fixture begins with real held movement, primary and shield input sampled`。该旧夹具挂载后只等两physics就发键，尚未适配人口准备。后续恢复次数/检查点/时序断言未执行，不提前判定全为过期断言，更不直接放宽。命令层先报scope收据缺失不替代真实stderr原因。实际两条已分类负测WARNING，FATAL/Handle为0；其三相机均已完成12样本。

B独立保全核20对清单及24封存bin一致；四次普通Rebuild0警告/错误、8个原生合同PID/argv匹配、guards空/无超时/无额外DefineConstants。source清单SHA `12472512572DDE3173ED6CC4BFBB841C086FCC7DA7A28E857228FA2C03545CA7`；六bin清单 `418CF960A372766AABD31045728FB008EA737BF51501AF1DA81C427520E6C188`。诊断类型/运行标记缺席；user109行一致，continuation仍为空清单。

最后普通二进制已恢复：根核live1772源及六bin零漂移、诊断类型不存在、git index仍 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。没有Git写入、付费、上传、删除、OS抢焦点或真实玩家保存语义读取。

## 下一步与完整目标

1. 资源FATAL继续OPEN，不用当前相机绿抵消。独立性能方向已只读确认可行：对原9保存场景做严格SceneState身体直读，替代反复造/销毁离树AI；必须先与原探针原生对照并有继承/未知嵌套/额外身体/脚本形状/变换负控，保留全部36原出生包络和经理两次native检查。尚未实现，不叫崩溃修复或缓存方案。
2. 迁移旧死亡夹具的实际准备前提，重新核对新居民恢复与旧输入/检查点/单次恢复合同，不仅增加固定等待。
3. 齿轮真实交战/能量衔接、同版正常Main+Explore及全部50自然接触仍未闭合；历史四类RID本轮未重跑，继续OPEN。
4. 原十一项所有正式角色/五族敌人/Boss/地图/机关/UI/音频/演出、正常速度审美/动作审阅、目标硬件性能、真人键盘及ZIP/GitHub交付要求不减。不勾完总项，不把Future/FTR拉入范围。

本次没有新图/视频，媒体一次检查4838B、基线3508B、增长1330B，未到20GiB。没有调度/自动化；20GiB只随正常汇报提醒，不停工。
