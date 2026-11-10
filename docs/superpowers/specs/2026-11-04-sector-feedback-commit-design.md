# 跨区反馈成功提交边界 · v247续步

## 意图与证据

原v246严格恢复夹具在sector已切后仍有一个ModeAttack VFX登记；v247首轮被动诊断通过仅证明一次idle清理先于原断言，不能关闭时序缺口。当前World在Physics同步提交active sector，Audio仅在后续Process轮询清旧状态；precommit SectorMounted既可能来自预加载也可能看见旧CurrentSector，不能用于此清理。

目标：真正成功的active区域/实例改变返回后，旧Task10反馈已结束；新区域的真实发布者反馈不被下个idle重复清空。不修改地图、物理/战斗、爪驻留、音画资源、既有等待/时限或原十一项标准。

## 方案取舍（开发者裁决，不冒称用户新设定）

1. 延长测试等待或只看Audio count：遮蔽同一帧次序，拒绝。
2. 复用SectorMounted：它在CurrentSector提交前且含预加载，拒绝。
3. World成功提交末尾的内部通知：采用。只通知真实active ID或root实例改变；Audio按owner身份订阅、同步clear并同步sector基线及当前action基线，原idle轮询保留作fallback。

用户已要求直接持续制作；这是既有反馈生命周期修复，不是新增玩法或付费/显示选择，不重复提出引擎或美术审批。

## 生产接口与边界

- World内部事件拟为 `Action<SeamlessPrologueWorld, PrologueSector> ActiveSectorCommitted`，携带真实owner与当前已挂载root；不是CurrentSector枚举冒充root。
- ExecuteTransition入口保存旧ID/root，通知点在CurrentSector、玩家/相机、outgoing移除及RetireDetached完成之后。初始化不需要replay反馈；同ID同root的residency刷新不重复通知。Prepare/activation失败不得通知。仍在原_transitioning边界内，reentrant请求继续排队而不递归。
- Audio依现有Ready/Refresh/Unbind生命周期持有唯一world订阅，源身份须与当前绑定一致。成功通知中先更新baseline，再ClearTask10Transitions，重新seed真实action基线；不重置真实爪/盾/能量或取消跨区驻留目标。已退出/Shutdown owner不应继续受旧源影响。
- 恢复例外遵守已有正式合同：Level.RequestRespawn在deferred world恢复前已由RecoveryStarted清旧并发新反馈。`RecoveryInProgress`期间的commit只同步sector/action baseline，不再次clear；否则会取消本次恢复本身的音画。新事务用例须从Basin通过真实RequestRespawn回入口并在原4 ProcessFrame内核对完成与clear generation未再增长。
- 通知不是新public扩展点；异常仍须有实际失败记录，不得静默吞异常或伪装回滚。不得借修复扩大为通用事件框架。
- 终审I1的实际修补：World成功提交与Audio清理通知逐监听器隔离，指定故障以原生WARNING逐条公开，后续必需订阅者继续；不向已经提交的事务/复活调用者重新抛反馈异常，也不重试不明副作用。独立故障注入阶段用真实生产发布者与恢复证明，正常路线原严格诊断门不改变。

## TDD与证明范围

先补独立事务夹具：保存正式level、真实mode按键建立Attack/slot2/None及旧ModeAttack key；现有 `world.RestoreCheckpoint(CanyonBasin,(1680,3960))`同步返回后、无新await立刻要求旧VFX为0。这对当前轮询实现应真实RED。该API本身不证明正常路线操作；fixture可沿既有Audio夹具禁用Motor并明确标注。

同夹具继续验证同ID/root恢复不重复clear；同步调用真实 `modes.RejectAttackRequest()`产生slot2拒绝，登记的新cue跨首个完整idle仍在，clear generation不再变化；不可调用虚构SetMode或直接audio.Present来冒充真实mode发布。

夹具前提修正（234931实际证据）：camp点在两个idle后会被真实BodyEntered保存。故同root第二次事务在第一次await前移至Basin bounds内(1680,3760)，避开camp触发器；恢复前必须独立确认Level仍为未激活的入口检查点，再按原4frame要求回Entrance。初次同步旧key RED仍使用原camp点，原失败保留；不能放宽为同区恢复成功。

事务控制复用现有Seamless测试：activation失败0通知、同root预加载0重复、reentrant通知顺序/非递归；目标实例与PersistentPlayer不变。所有原恢复断言、原完整Main899/HP/单跳/无恢复保留；专项通过不能签整关、真人手感或画面质量。

无新CS/UID，SourceRows1888；root唯一native，全部CS/harness运行时冻结。无GUI/OS输入/新媒体/付费/额外崩溃采集/自动化/发布/删除或真实存档操作。
