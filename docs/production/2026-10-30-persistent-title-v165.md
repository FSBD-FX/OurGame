# v165 · 当前版隔离常驻标题入口

这是显示核对工具，不是新美术、正式菜单、正常游玩或完整序章交付。用户旧窗口反馈仍NOTSEEN；本轮尚未启动live，不抢前台、不发送系统键鼠、不采集媒体或崩溃、不付费、不创建自动化、不进行Git写入或资料删除。

## 已实现与实际证据

- `tests/RunIsolatedTitleHold.cs`：独立test-only入口，挂树前把save/settings/recorder绑定到唯一外部case；关闭两种自动开始。同一session供真实无窗口合同调用。
- 仅核对fixture禁用开始/继续并同步解绑开局，避免误触写入新局；设置/操作页仍可往返。它不是生产菜单改版或正式菜单验收。
- live循环没有计时截止；窗口X和菜单Quit汇入同一幂等关闭，清理取消准备、排空音频并释放自有level。不使用旧120秒观察器、不记录按键、不调用前台激活。180物理帧的合同不是无限时长实测。
- `tools/show-isolated-title-v001.ps1` 默认只准备，实际已完整执行且没有进入live分支。显示必须同时明确 `-Show -DisplayConfirmed` 且由当次用户许可；启动前复核当次普通keyboard案、完整当前源、六bin、实际封存实体、指定helper及原始日志。
- PS测试调用真实入口控制流，以可数的准备/资格/启动spy替代外部进程；默认准备、准备失败、首次/第二次资格拒绝均零启动；显式模式只生成正常隔离title命令，启动spy一次，实际原生进程0。另以实际成功case及三组明确标记的源码/bin/helper清单漂移克隆验证拒绝；克隆不是原生运行证据。

关键原生案位于 `G:/knight/验证证据/october13-stable-claw/`：

| 案例 | 构建ms | 原生ms/exit | 结果 |
| --- | ---: | ---: | --- |
| keyboard040609 `38e1ae1d60f9468386ffeb919eec0b13` | 28682 | 14935/1 | 真实默认自动开局RED：`isolated title must not auto-start` |
| keyboard040831 `ab1e6c37246246258b1dfa0cd60721da` | 30676 | 19410/0 | 原键盘＋两次标题挂载/各180物理帧、误触开始/继续、菜单和两种关闭GREEN |
| keyboard041312 `08fc00bc25c14da7aab8075732244c8a` | 28212 | 19300/0 | canonical通过，外层wrapper因空stderr的null调用失败，不称入口成功 |
| models041524 `5948775791e24f03b54fe2c0bd8bf546` | 33607 | 431/0 | 较早模型组通过，不是整关 |
| keyboard042341 `376c97d8f3354d0d85594655c1de52fb` | 30795 | 19383/0 | 最终默认prepare及实际完整qualification通过，未显示；前向补齐两份PS实体 |
| models042539 `4c35e86dbcb748108ac8a805af293f06` | 28326 | 535/0 | 最终模型组通过 |
| pre042617 `9862af9d43134502beb93e7072afefdf` | 28916 | 99650/1 | 原首战恢复完成计数expected1 actual0，严格FAIL |
| post042835 `bfbe6400f97b4d8c9c17a4ef161dc1db` | 28341 | 218809/0 | 原75各enter/return＋owner8通过；原4限定WARNING，不是raw全静 |

最终pre保留被动观察：原after-settle物理2511、请求1/完成0、Recovering/Pending=true、Prepared=false、Failure及Rejected为空、同Player且HP5；9/32行、fault0、extra_waits0。说明当次断言确实发生在准备仍进行的时点，不证明后续本来必能完成，更不证明所有生产恢复正确。下一步按真实完成事件与有限准备合同修正测试消费者，遇拒绝/失去所有权/失败应立即报错，不盲延时。

## 一次审查与修正

唯一fresh审查C0/I2/M1。I1空日志在真实wrapper和直接读取中RED，显式null→空文本修正；文件缺失仍拒绝。I2补真实控制流测试，临时绕过prepare返回时实际触发`TITLE_ENTRY_RED`，恢复后通过；不是只检查函数返回的模式。源码/二进制/helper实际漂移拒绝已执行。

另补保管重要缺口：旧041312案两份新PS只有清单SHA，没有before/after实体，不能补造；后续keyboard案补实体并在资格检查中强制核验。该缺件拒绝经历`TITLE_FROZEN_HELPERS_RED`→GREEN。

延期Minor：菜单合同仍直接同步调用公开方法，尚未覆盖Settings/Controls/Back真实按钮信号及子菜单跨帧。生产绑定经只读检查存在；不称完整菜单/真人操作已验收。没有第二次代码审查。

## 裁决与边界

1. 沿用用户指定dirty G根原地实现，不建worktree/commit/清理；代价是审查仅此切片，不给整个历史dirty分支背书。
2. 这是已存在显示流程的技术修复，不新增游戏设计，不重开设计审批；代价是仍须当次显示许可，后台目标不因显示待确认而暂停。
3. Start/Continue锁定只属于显示fixture；代价是此窗口不能拿来正式游玩或批准生产菜单。
4. 缺失实体按重要问题前向修补，不回填旧案；代价是旧案一直保持hash-only边界。
5. 审查未判定的整dirty分支、v164观察器、全十一项/完整路线/资源根因、实际可见/视觉品质、无限时长、真实存档兼容、生产菜单/共享teardown全面正确性及对抗性证据篡改防护都不由此关闭。代价是仅完成技术入口，不具备正式发布资格；冻结工作流不是恶意篡改防线。

原十一项、v163 Main耗能战死恢复/Explore Grip9阻挡、历史资源问题、正式视听动作/UI、硬件真人与交付仍ACTIVE/OPEN。计划及逐次结果见 `.superpowers/sdd/2026-10-30-persistent-title/progress.md`。

## 最终核验与保管

最终普通keyboard/models/pre/post为3PASS/1FAIL；全8次真实原生案6PASS/2FAIL，含最初RED，不能当成功率或完整游戏资格。post原75索引0–74各唯一enter/return，owner8节点1→1，原4限定WARNING分别为故障观察器1、RoomPresentationProfile1、Completed→Recovering2；未改原300000ms上限，`original180Accepted=false`和非性能认证保留。整套未全绿，不称可合并或正式完成。

只读保管核对共40组清单对、3772份选定源实体、88份helper实体、48份bin逐件匹配；不是1861源逐案全实体备份。旧041312两PS实体缺口保留，三组明确无native的漂移克隆不计运行。所有16个本轮自有build/engine PID已结束，旧8760及bridge不触碰/不归因。当前1861源、10helper、6bin匹配最后post；随后再次读取最终keyboard资格仍与当前源码/六bin一致。三组控制拒绝原因也复核：source/bin不匹配，helper冻结实体匹配失败；没有启动窗口。

HEAD、branch与index SHA保持原基线。正常汇报前已有一次手动媒体Check：2026-10-29T20:32:19.6311019Z，7,427,750 bytes／86文件，Notify=false；没有新过程媒体、重置基线或自动化。本轮已发出新的显示时段询问，但尚未收到当次显示许可，不因自动目标续行而启动live。
