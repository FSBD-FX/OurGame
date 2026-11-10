# v262 — A方向决策与同源post身份诊断

状态：本次限定调查与方向记录已完成；**资源故障未修、完整目标ACTIVE**，不是交付或普通稳定性验收。

## 新用户决定

U-20261106-02已确认A轮廓/比例/配色、减少噪纹的外观方向，B结构参考不接入，肩背和灯保留。精确范围及源图SHA在`G:/knight/过程图稿与视频/2026-11-06_角色现稿重审_v001/approval-v002.md`；12份源/原件/展示哈希均匹配。不修改原像素/首次索引，不批准最终分层/动作，不生成付费稿，不自动接入。

## 事实、推理、风险

- 事实：v261当前普通19项是18PASS1FAIL；post案例`prologue-post-20261106-030051-783-8444f545914448ceadee9ec16f284c4a`159901ms、0xC000001D，case20 RETURN后case21 ENTER，匿名gchandle FATAL。不能从边界推定具体load/mount/等待/断言/清理子阶段，也不能套用旧pre序列1037。
- 推理/可检验问题：现成REF类型Dispose日志若覆盖当前失败对象，可能在同进程FATAL前留下未RETURN的具体类型、managed serial和已登记owner。它是定位证据，不证明致因；未覆盖对象或没有native登记时仍可能匿名。
- 风险：180000ms诊断预算不同于普通300000ms，插桩有时序效应；Dispose20000条调用上限、其他身份/分配上限和已有after-rejection GetState观察均保留。到不了原边界/截断/观察差异都要报告，不提高预算，不重跑筛绿，不新增dump或GC/cache/lifetime干预。

## 本次准入与冻结

根G:/knight/Knight，dirty codex/task6-certified-final-20260902T0555，HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6。执行前逐行重算当前1898原生输入及10helper，与上述普通post封存清单全相同。已有未知Godot PID54356不属于本次，不查询/操作其状态或终止它。

唯一诊断命令：canonical `verify-claw-knight-v001.ps1 -Phase prologue-post -SourceRows 1898 -IdentifyRefCounted`。父会话59851已终态exit1；新案例`prologue-post-refcounted-identity-20261106-034813-587-9631532dae1545558fa415d09de3893b`。源/测试/host/资源冻结，不混waiver/trace/movie/MeasureFullPostBudget。

## 结果与普通恢复

### 唯一REF post：严格FAIL，不是超时

- 诊断Rebuild exit0，42975ms；native exit **-1073741795 / 0xC000001D**，**163566ms**，TimedOut=false。未调整180000ms预算。宿主完整post scope收据不齐继续拒绝；不是仅宿主误拦。
- stdout最后registry是**case18 ENTER** `MountedGuidanceRendersAndClearsWithoutHidingObjective`，无RETURN；并非上一普通案case21。更早stderr9809是`Handle is not initialized`，调用栈SwapGCHandleForType→PackedScene.Instantiate→ResidentSceneBinding.Instantiate:62→TrySpawnCore。
- 原camera fixture无法提交Playing：`canyon.v2.resident.staggered-rift.04`生成拒绝、profile_missing=True，停在Title；failure detail由WaitForPlaying:262、Guidance:637/704报告。不能当作已执行相机/HUD断言，也不据此删除合法拒绝门。
- 具体不是一个可合并的事件：Handle ERROR附近operation162同slot当时GateMissing=false且configured-return成功；之后operation234才出现一次null setter、FieldState=null/GateMissing=true。时间先后不足以证明因果。缺失profile是ResidentSubtypeProfile，也不是后续FATAL的EnemyArchetypeTuning。
- stderr13106–13120：唯一未RETURN的Dispose **call1674**，`disposing=False`、`type=Knight.Config.EnemyArchetypeTuning`、thread1、managed_hash46555465、managed_serial1610、raw_native_ptr0x0000021A0B8222E0；随后FATAL栈明确包含该类型Dispose:105→GodotObject.Finalize→RunFinalizers。1676 ENTER/1675 RETURN是日志总对数，不是完整post证明。
- **native_identity=unknown**，只有此条serial1610记录，没有已登记nativeID/path/first-owner；raw pointer不是ObjectID或存活证明。本次定位到终结器类型，尚未定位具体资源归属或原因。不能将此对象同一化为普通案匿名对象，不能证明前面的Handle错误导致它，也不把最后清理过的资源当元凶。
- 同地址在本日志较早曾被另一类型使用，不能按地址近邻拼接owner。同类可来自GraveLampMoth默认new、_Ready空值fallback或多个场景导出Resource；本post未启用original-pre的来源观察，现有日志不能区分。因此不把serial1610指定给rift-flyer或任何具体场景。
- 本次stderr13146行/9631074B，ERROR2（Handle及FATAL）、WARNING2（原observer/profile负控）；无观察到的CAP/LOSS/INCOMPLETE/FAULT标记、StackTruncated=true为0。Dispose未到20000，已有lifetime最大Sequence7869未达8192。resident assignment234/234、fault0/truncated0/after-rejections0/after-sources0；它只说明此局部记录配对，`whole_post=false`，不意味着分配/所有资源/原post完整性成立。

### 普通输出已恢复

之后唯一普通命令`thruster-profile-trace-normal -SourceRows 1898`：案例`thruster-profile-trace-normal-20261106-035233-335-58b60b50c0ec45a489f15d5cce79332a`，父54887已终态exit0；完整Rebuild40325ms、0error/0warning，native **666ms PASS**。真实loaded assembly收据`lifetime_type_absent=true native_identity_absent=true`，stderr空。当前6个输出逐SHA匹配封存普通binary，并与上一普通post6项完全相同；不留诊断DLL。

### 同源绑定

两案各5组before/after（source1898、user109、helper13/9、continuation1、binary6）共**10对0差异**。两案1898行分别逐项匹配当前实际磁盘及旧普通post同一清单；13独立helper每个仅一个hash，当前逐项重算匹配；12份封存binary实际重算匹配，当前6份普通output匹配。REF和ordinary的DLL/PDB不同是预期条件编译，不混记为普通二进制。详见本计划workspace的`audit-v262.json`及两案原始manifest。没有任何生产/测试/host/资源改动。

本轮调查结果有新增类型证据，**资源故障仍未修复**。不追加第二轮诊断、不筛绿；下一先只读追溯该类现有加载/持有路径与日志可关联范围。若需要新增插桩、原生采集或改变资源生命周期，先明确方案与相应权限，不擅自改GC/cache/引擎。

## 计划与审查

Task1方向决策及保全核对完成；Task2运行/分析/普通恢复及唯一fresh只读审查已完成，C0/I0/M0，仅认定限定调查可保留。无修补pass/二审，不把失败规范化为PASS。最终目标仍ACTIVE，v261 Task2仍OPEN。所有ruling及其错误成本见本计划workspace `progress.md`；完整审查摘要`final-review-v262.md`，具体source→setter缺口见`next-seam-readonly-v262.md`。根只读媒体检查10491889B/97项/Notify=false，未达到20GiB档位，不建立自动化、不清理资料。

## 验收边界

诊断通过也不消除普通post历史失败、不转为19/19、不证明76全覆盖或真人/硬件/正常速度观感。v261 Task2/完整十一项及正式美术/连续动作/R-I/R-C/R-P仍OPEN。无新窗口、系统输入、实录、生成、真实存档实验、自动化、Git写入、删除或发布。
