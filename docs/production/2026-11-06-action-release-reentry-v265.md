# v265 · 下一招接住正在显示的收势

当前生产修补已在真实保存角色和保存玩家中验证；本轮六个最终源阶段5通过、1严格失败，资源门未通过。原完整十一项目标 ACTIVE，不是正式交付。

## 事实与修补

旧动作状态已是 None、画面却仍在混合 `_actionReleaseFrom`。旧代码从已归 Identity 的 `CurrentActionPose` 捕获下一动作的起点，出现突然掉回基础姿势。现在 `PlayerFormalVisual` 只在选起点时读与上一显示帧一致的纯动作来源：若上帧是 None，取其释放姿态；否则保留原来源。仍用现有上一帧权重、所有权和 elapsed；reset 优先不变。没有缓存最终组合姿态，未改物理、按键许可、受击窗口、资源、动作贴图或输入时钟。

## 原失败与修补后原生证据

统一原宿主 `thruster-ground-gait -SourceRows 1898`；保留案在 `G:/knight/验证证据/october13-stable-claw`：

- `thruster-ground-gait-20261106-044013-580-b8b37e9f3e6c423489a4cce567961a08`：只读属性测试编译错误，不算行为反例。
- `thruster-ground-gait-20261106-044149-840-ac655866b49e48afbaac77ab9abbbd35`：构建成功，原生15284ms exit1。地面挥击仍在实际收势时以同一时间戳开始下一挥击，最大测点跳变17.41132px。
- `thruster-ground-gait-20261106-044420-394-61d6db9548e949c8aacdd1b71fd12043`：新增真实保存Player的合法 J→自然None→新J，6044ms exit1。两次独立攻击都已接受；排除共同髋角后，近臂在二次起手先从 −0.10378249rad 反摆至 −0.015317754rad（进度0.11473333）。不是按键被吞或重新设置的样例。
- `thruster-ground-gait-20261106-044558-349-bf18fc1663154b0fbab6d14ba219449d`：修补后19660ms安静PASS。左右朝向、两种出招、两种重入共8例，零时间最大步差0；同时间重绘不动，正时间继续新动作，收势已结束不复播。真实J两例各两次实际ID、零跳/喷，近臂约 −0.1225→−0.13353rad（向新起手而非中立）。原ground-gait和v264真实握持分支保持。真实结果是headless几何，不是正式角色风格/正常速度美感证据。

上述两个实际RED和GREEN核对15组前后清单、18封存binary与最终当前1898输入。相对v264，最终只变一个生产类和三份测试；编译失败案另存不计入3个原生案例。最终manifest SHA `8DA79E7C2A5307396183FD6174A3B62862255B89E3FF07D1424DEBE11AEE99CC`，stdout SHA `2754BBEC5485EEA20DECE4CAE29DBF32A81CEC6C11E963598242595A2A4ACA97`。

## 同源回归终态

一轮五阶段普通回归由 `.superpowers/sdd/2026-11-06-action-release-reentry-v265/run-regression-v265.ps1` 执行，父会话 **98805 已终态 exit1**，唯一结果 `regression-results-v265.json`。表现模型770ms、实际角色Rig19244ms、动作9794ms通过，三者无原生错误/警告。普通前段 `prologue-pre-20261106-045104-091-7b1669fcea3d419ab7b98de474b31452` 原生147261ms exit0，但7条 `Handle is not initialized` 被原宿主严格否决；都在 `SwapGCHandleForType→Node.AddChild→BasinSupplyRuntimeTests.OpenSample:525` 这条可见栈上，**不是已定位具体资源或动作补丁致因**。

普通后段 `prologue-post-20261106-045427-205-50934a4875a3436e9922a44ec296b075` 183102ms严格PASS，原75 legacy案例及8次owner、退出drain和最终标记齐；原受控警告4条，原生错误0。不是修复了v262历史终结器故障。含单独ground-gait共**5通过／1失败**；两个实际RED保留，不与最终候选计数混合，也不因后段通过而抵消前段错误。

8个原生案例（两RED、专项GREEN、附加5）共40对保护清单0差异，实际重算48份封存binary、16个宿主依赖、当前1898源和6输出一致于相应manifest；6最终候选仅一个源/普通binary变体。唯一独立只读源码/专项审查 C0/I0/M0，没有修补轮或二审；v265完整严格门未GREEN、计划不关闭。后续可在不扩大采集的前提下核对前段实际挂载的资源边界，不重复同一失败跑法筛绿。一次容量核对10,491,889 B／97项、未达新20GiB档，无调度或清理。

原生资源故障不靠本补丁关闭；完整同版主线/探索、独立存档/全内容、正常速度视听/相机、硬件与用户试玩仍在原范围。未开窗、未发送系统输入、未新增媒体或dump、未付费、未改引擎/GC/cache、未动真实存档、未自动化/Git写入/删除/发布。
