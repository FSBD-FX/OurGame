# 第二次J因果观察

完整原十一项目标、G:/knight/Knight和全部未验收项不变。上一目标轮是progress：真实错误顺序RED→GREEN，当前pre失败给出新动作状态边界；不是关卡或美术交付。

现有DescentBoundaries第二J按下→Frames3→释放→Settle→IsAttacking断言在v156普通pre126899ms失败。先复用Run里原有AttackStarted监听保留计数、ID/方向和engine/physics/process/inputsequence标量，传活的Func快照而非冻结值；订阅/取消订阅生命周期不变。第二J原命令前取基线，原Settle后、原断言前输出一次状态；不改输入/等待/预算/断言，不补按、不注入姿势。

为区分完成与重置，测试内只读反射现有combat._model和model._activeAttackId/_elapsed/_settings，使用实际设置总时长及公开IsAttacking；即时返回只含标量的readonlyrecord，不存scene/resource/model跨await，无新生产API/事件/强根/GC/cache/Dispose。原模型自然完成保留ID与elapsed>=duration，Reset清零ID/elapsed；观察只能说明当前保留状态，不能证明所有中间历史。缺字段/失败不得编造默认成功。

纯模型控制用非默认.2/.3/.5共1.0秒，验证未启动/实际启动.25/实际完成1.0/Reset四态及重复读取不改变模型；实际native原委托的状态记录还核对同步读取前后inputsequence/physics/process/位置速度/生命/flow/攻击等不变。时间、反射分配和输出会扰动执行耗时，不能声称零时序影响。

先观察再决定修复：仅在真实证据明确后，对已证明的测试时窗或生产错误做最小TDD修复，不通过拉长攻击、放宽断言或降低地图要求换绿。若未复现，保留未知状态并推进完整无窗口normal-plan Main/Explore诊断，各原600000ms；严格区分正常显示/焦点/真人/FPS资格。现有显示入口硬编码1695与当前1857冲突，不在无授权情况下启动窗口或偷偷改门。

原focused55s/pre180s/post300s及source1857/897、143地形/9残片/76普通敌人目标保持。原失败不删除，不改真实存档，不付费/前台OS/再次采集/自动化/Git/删除。仅关键媒体；一次fresh最终审查。完整Main/Explore实际显示/正常AV/硬件/用户仍OPEN，目标ACTIVE。
