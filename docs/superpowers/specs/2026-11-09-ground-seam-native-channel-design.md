# 落脚交接双时序取证设计 · v332

原完整十一项ACTIVE；上一v331为实际进展（Arc落位、自然Explore、20组15PASS/5FAIL），不是完成。只在G:/knight/Knight无窗口制作。此设计是连续执行授权下的开发判断，不声称用户批准新玩法或修改验收。

## 事实、推理、风险

v331 Main物理450→454之间只有两次真实FramePublished，旧观察器明确拒绝。它监听的是已Apply正式rig的发布，而非每个物理tick；ProcessFrame新root加旧pose不能作为缺帧视觉。Motor已有完成观测OwnerId/Revision/PhysicsTick；可能合法撤权为default。直接移到PhysicsFrame又处于节点前边界。

选：保留原视觉/相邻物理/同脚stance/.15px全部断言，在同一原路线新增test-only post-Motor有限值账本，单独证明实际contact交接及缺发布区间。代价是即使物理记录完整，旧门仍可能失败，此轮不能自动签主线。

不选：放宽tick+1、强制每physics发布或固定帧率；改变合同/产品节奏，可能掩盖真正视觉问题。不选：重跑到自然相邻才通过；没有根因证据。

## 有界接口

新增测试partial `PrologueCanyonGeneratedMainRouteTests.GroundSeam.Physics.cs`。复用`BasinBoardingTickObserver`挂level、Always、priority=真实Motor+1；只在x1100..1420/y600±2范围复制256条标量/Vector2记录，无查询/输入/位置/HP/物理/发布写入。

每条含tick/process、motorID/revision、root、首个upward native slide的body/shape/native index/normal/point及upward候选数、有效性原因。Owner/revision/tick/delta和原生velocity、grounded/eligible核对；资源仅回调局部借用，不Dispose或留入账本。暂停/退出/撤权/不明确contact记无效，不能以旧条接续。

原视觉handoff处先读实际old..new tick区间：逐tick无缺/重复/倒序、同owner/revision、正确两shape、普通平移、恰一次左→右handoff。输出`WORLD_GROUND_SEAM_NATIVE_PAIR/STEP`，明确native不是视觉、实际published跨度/缺发布未补。然后原tick+1及脚底断言照常执行。没有handoff就不能输出完成证明。

观察器与I/P/TreeExiting订阅由证据类拥有；关闭优先、callback置null/停用、逐项解绑并保留错误，Node由level既有teardown释放。不得在回调里Free；正常Dispose不产生假失效。不要更改原路线、暂停流程或销毁原资源。

## 验证与完成边界

手工字面量450..454/shape11→12证明validator能找451→452；独立坏数据包含缺tick、重复、失效、错owner/revision/shape、未知contact、回退/垂直位移、无handoff、范围外，必须拒绝。先真实功能RED，再实现。模型controls不算native；普通Main/Explore实际原路线另跑，原11个指定phase与cold同版核对，全部失败保留。

本轮不修生产代码、不关闭v331两FATAL/两RID、不声称新美术/视觉顺滑/硬件或真人验收。一次全增量fresh审查，C/I一次TDD修补、Minor延期；不创建自动化/Git操作/删除/GUI/付费/真实存档实验/引擎GCcache或新dump。
