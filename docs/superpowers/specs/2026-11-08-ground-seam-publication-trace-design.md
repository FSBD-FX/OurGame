# 主线落脚的发布时钟诊断

## 事实、判断、风险

v328 Main严格失败：相邻发布样本的physics为453→455，未生成整线JSON。该同案原Input/Motor观察有14242个逐拍样本（30..14271）且completed stamp成立，454物理不是漏执行。普通FramePublished由Coordinator._Process在Formal.ApplyPlayerFrame后发布，不承诺每物理tick；旧观察器却要求相邻physics。缺453鞋底及idle/lifecycle账本，不能判断具体原因或脚步正确。

本轮只补test-local被动标量取证，不修游戏、不更改旧合同。优先辨别没有idle处理机会与Coordinator处理被抑制。日志会扰动时序，下一次未复现不构成修复；保留旧失败，不能以新成功冲销它。

## 范围与输出

只改`tests/integration/PrologueCanyonGeneratedMainRouteTests.GroundSeam.cs`。原观察器构造时成对绑定同SceneTree.ProcessFrame，Dispose成对解绑；这是节点处理前的边界，不当作post-node采样。

- 每次有效发布入口、物理去重前增加publication序号；记录process/physics/inPhysics及输入presentation time/elapsed。
- 仅原seam窗口x1100..1420、root y600±2存有界环形标量账本，记录I（idle边界）/P（发布）、body位置、coordinator入树/IsProcessing/CanProcess、paused、flow。不每帧写磁盘或保媒体。
- 原handoff相邻断言前，先输出old/now原始owner/root/phase/stance/实际双鞋底及walking资格，并输出有限账本；无论相邻与否原断言仍执行。
- 不主动调用Coordinator/Formal，不加物理计时或移动所有者、不修改frame/input/HP/position/velocity/地图/引擎/频率。

## 验证与边界

原canonical只用`-Phase thruster-route-main-diagnostic -SourceRows 1916`，原600000ms cap，无额外flags。仅一次有新观测的Main；既有source/6binary/true-user保护照旧。完整旧路线仍然运行、错误缓存/最终抛出不变。读取stdout与external中的同一标量回执并核账本，结果按严格PASS/FAIL原样保存；若无缺帧，也只称该次自然成功/诊断未复现，不称修复。

无任何新生产行为，故本轮是系统排错的取证阶段而非生产RED→GREEN实现；不会伪造一个feature/test-only RED冒充根因修复。以后真正修复必须另有因果反例与TDD。当前产物若因测试编译身份变化，则不能借v328其他10阶段为它签整版资格。

只G:/knight/Knight，无窗口/OS输入/付费/新美术/自动化/Git/worktree/删除/真档/GC/cache/引擎/新dump；所有原十一项与历史FATAL、正式声画/硬件真人/素材、v328 M328-1仍OPEN。
