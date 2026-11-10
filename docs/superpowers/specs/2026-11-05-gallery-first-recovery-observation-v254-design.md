# v254 · 首次满血反击窗口：先补同次危险证据

## 事实

v253 Main143241 与 Explore143555 的换批观察均37physics；Skitter准入到击败则665 vs207physics。Main首次局部eligible为8030/loop317/input8000，持续至8045，energy37.3/HP2/Recover/.583s/strike可达，却未发J；Explore对应首次eligible即发J。Main首次J到8435/loop717，比初次eligible晚405physics。现有外层敌威胁否决可能过宽，但早期窗口缺少全源SafeCounterSeconds/缺失/未知/重叠/来弹否决实值，不能据估算直接启用早反击或改难度。

原详细gallery logger只保HP1，不能替代缺少的HP2证据。本轮只在原J决策完成处读取已有变量，留前24个“正常键盘gallery、selectedSkitter、本批1、目标HP2且locallyeligible”的完整字符串快照。原HP1前32/尾128、原mask、所有查询/决定/输入/await/899和生产源码不改。

## 设计

测试驱动辅助 `Task10CaptureGalleryHp2Decision(List<string> rows, bool enabled, bool eligible, int health, string completedRow)` 返回是否写入；仅enabled/eligible/health2/Count<24且非空已完成行时append，保留首次窗口，不覆盖、不持有native引用。先写stub及真实列表行为测试获得RED，再实现。

集成局部生成JSON：当前loop/physics/process/input、actor与HP、现有J许可/局部counter/strike、模式/能量/盾/速度、已有danger全部标量与完整反击窗布尔、现有foreign-budget结果（仅使用原outer已执行值，不额外调用有副作用的预算函数）。不再做物理扫描、不ObjectFromId、不改变选择；所有诊断异常只计faults，不授权输入。flush前24已完成字符串一次，原HP1 flush原样。

`extraQueries=0`特指不增加世界碰撞/危险扫描；快照仍读取已有控制器属性及Engine物理/输入序号，不声称没有任何原生标量getter。布尔完整反击窗仅为原纯算术的同次观察，不授权J。nullable foreignBudget不得把短路未执行当false。

测试保正常/禁用/不可反击/非HP2/空行/满容量边界、首次内容不被后续覆盖；新增测试由现有远程模型入口运行。实际Main/Explore各一次，记录全源早期窗口再决定下一实现，不在本轮偷偷更改早反击规则。

## 资源错误调查边界

当前官方a13da4feb/4.7.1引用切换源码说明Swap发生于RefCounted强弱handle变更，不能推出本地哪个资源负责；网页源行号与本机PDB行号不同，不以网页定位本地精确帧。仅运行现有basin-resource-control/pre-prefix各一次作上下文对照；不新增dump、引擎/缓存/GC/KeepAlive变更。若再次安静也不关闭pre7Handle/历史FATAL。

## 权限和验收

唯一KNIGHT原地脏树/根代理唯一build-native；作者完成后冻结。仅headless隔离存储，无GUI/OS/付费/媒体/自动化/Git/删除/真实存档。原十一项/正式图稿连续动作/R-I/R-C/R-P/硬件真人及当前主线/资源资格保持OPEN。本轮是诊断推进而非感官提升或游戏完成。
