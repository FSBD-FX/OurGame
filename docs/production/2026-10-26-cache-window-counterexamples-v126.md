# v126：缓存绕行反证与普通构建回归

KNIGHT继续在 `G:/knight/Knight`。原十一项、全部稳定ID和最终交付条件不变，本轮没有完成生产资源修复。没有修改正式玩法、地图、敌人或美术；用户最后窗口反馈仍为NOTSEEN。

## 事实

v110的真实场景持有器已在其限定测试中保住24场景/59个托管资源；因此本轮没有凭猜测加全局pin或通用强引用图。新增实验独立进程启动，不先跑旧生命周期实验；使用真实 `scrap-scatter.tscn`、`canyon.v2.resident.low-ceiling-bridge.03` 和未改的原工厂。

所有有效窗口都先确认：创建助手已经退栈、Scene弱引用死亡，两项明确临时强根仍保持Profile/Settings存活。然后进入有上限的测试专用CLR finalizer门，撤销两根并GC，确认两个弱目标都死亡、finalizer尚未完成。仅改变接下来是否重载、以及实际loader的缓存模式；finally释放门，不在生产运行GC或门。

| 模式 | 实际取得资源 | 解除门后 |
|---|---|---|
| 不重载 | 没有第二次工厂调用 | 旧Settings serial1和Profile serial2均ENTER/RETURN，正常结束 |
| 默认Reuse | 原工厂收到空Profile并拒绝 | 旧Settings serial1 ENTER后FATAL，无RETURN |
| Ignore | 原工厂成功；新Profile/Settings native ID及CLR serial5/4；全部7+23导出值一致 | 旧Settings serial1仍FATAL，无RETURN |
| IgnoreDeep | 同样获得两份新资源及30项相同值 | 旧Settings serial1仍FATAL，无RETURN |

Ignore第一次仍故意沿原GD.Load，101445实际null-rejected/FATAL，作为真实行为RED。之后只改变诊断Ignore分支；101730虽修好了这次取用的资源，却未通过终结安全条件，因此不是GREEN。Deep只比较已知外部依赖缓存因素，101956仍失败；不继续盲试，也不推广生产loader。

配置比较读取实际两类型的所有Export属性，仅保存标量字符串，不用Validate()==0代替配置相等，不在比较快照内暗藏Resource强根。私有副本若要通过，必须有不同的非零native ID和CLR serial、全部配置相等、原工厂成功、旧/新两类型完整终结配对、native0和严格无错误。当前Ignore/Deep都没有这样的成功证据。

本机Godot4.7.1 XML的CacheMode.Ignore条目7089–7092说明主资源/子资源不使用缓存、external仍Reuse；IgnoreDeep条目7104–7106递归处理external。这里未替换引擎、下载/安装调试工具、attach，也没有更改生产GC/Dispose/默认缓存或回填空配置。

## 推理与风险

不重载对照说明此窗口中的两份旧资源并非只要终结就必然崩溃。另一方面，拿到新资源及相同配置也不足以保证旧资源能安全终结；浅/深独立加载均不能作为已验证绕行方案。

这些是特定保存叶、特定pending-finalizer窗口的反证，不证明所有普通Main/pre/post历史FATAL都同因，也未定位Godot内部完整责任链。当前没有足够依据改生产加载策略。已有原生调试方案/成本的用户选择仍待答；不把预选或沉默当授权，其他安全制作继续。

## 九案原始结果

目录根 `G:/knight/验证证据/october13-stable-claw/`。9构建均0警告/0错误；5严格PASS/4FAIL，失败均为实际原生FATAL，不是只因校验器拒绝。无超时、guards0、ExpectedFailure均空。

| 目录 | strict | native exit/ms |
|---|---|---:|
| resident-cache-none-refcounted-identity-20261026-101409-285-3d49ed63457640d0a9f2ef2e47716e93 | PASS | 0/596 |
| resident-cache-ignore-refcounted-identity-20261026-101445-155-c43f19e3c2bf4e0e8ea894b389c20527 | FAIL/RED | -1073741795/5996 |
| resident-cache-none-refcounted-identity-20261026-101628-453-c140436845a8484b9c9e4246523d2c62 | PASS | 0/490 |
| resident-cache-reuse-refcounted-identity-20261026-101657-089-714a038e975044438237daeef672f375 | FAIL | -1073741795/6012 |
| resident-cache-ignore-refcounted-identity-20261026-101730-103-ffa851b62a56415db4af43f156446da6 | FAIL | -1073741795/5916 |
| resident-cache-deep-refcounted-identity-20261026-101956-040-1d4b034724c94e0aafd3c4396fe28aab | FAIL | -1073741795/6094 |
| prologue-pre-20261026-102434-748-e0b18d6ed94141978a38d4e9a6a4d73e | PASS | 0/158763 |
| prologue-post-20261026-102743-026-1b24275edc814da7ad4a27f0d90f4a40 | PASS | 0/197497 |
| models-20261026-103129-290-8d957c0b1b5249949bdc6440c33b43ba | PASS | 0/398 |

六案是REF-only诊断，最后三个是普通构建。101628/101657/101730为同源三臂；Deep新增模式后与最后三个普通案同源。不能把这些不同冻结阶段拼成同版四臂或整关验收。

4个FAIL的ERROR/FATAL按stdout/stderr/external均0/1/1，ERROR与FATAL同一行，后两个sink是镜像，不计成两次独立故障。post WARNING为0/4/4，其余WARNING为0。当前post原75 enter/75 return、owner8次nodes1→1、普通标记各1、CONTROL0，三sink预期警告/scope完全匹配。普通回归安静不关闭历史崩溃。Main/Explore本轮未修改、未重跑，v125的7255 E0死亡和8547/9046探索伤害仍未闭合。

真实none101628收据在当前消费者上的1正例及缺阶段/字段/身份/终结回执等反例共21检查通过；Ignore/Deep没有安静原生正例，不用合成数据冒充。一次汇总命令把空日志当null导致只读计数失败，随后显式字符串转换重读成功；它不是构建、游戏或行为RED。

## 一次审查与全部根裁决

fresh `/root/v126_cache_window_review`（gpt-6-astra/high）核实三份当前SHA及物理BASE，新增C0/I0/M1。根按实际影响复评同意。没有C/I修复pass，不复审；新Minor和继承7项合计8项延期。[审查处置](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v126-final-review.md)。

| 审查未认证项 | 根裁决 | 错判成本 |
|---|---|---|
| 1.原十一项/76/BossNPC/交付 | 所有原要求继续ACTIVE，实验不替代成品 | 缩减或虚报完成 |
| 2.Main7255 E0 | 本轮未改未跑，旧FAIL保留 | 把站桩策略局限当游戏无解或已修 |
| 3.Explore8547/9046 | 原失败继续OPEN，不拼跨case时钟 | 忽略真实受击 |
| 4.全部资源故障同源 | 不外推单叶窗口 | 错误宣布稳定 |
| 5.native最终责任链 | Settings身份已对应，内部因果仍未定位 | 改错引擎或所有者 |
| 6.Ignore/Deep生产推广 | 明确拒绝，两者实际FATAL | 引入无效绕行与额外驻留 |
| 7.private完整成功 | 当前不存在，不造成功收据 | 假证安全终结 |
| 8.其他GC/JIT调度 | 前提未成立必须FAIL，不认证全平台 | 把未执行当通过 |
| 9.mounted Ready/运行副本/战斗 | detached原工厂不替代真实世界 | 漏Ready fallback/物理问题 |
| 10.24场景59图长期寿命 | 不借本两资源实验续签旧图证据 | 误认全部图覆盖 |
| 11.生产GC/Dispose/pin/cache/升级 | 未实施，不把本实验当这些变更的授权 | 引入停顿、泄漏或破坏借用资源 |
| 12.性能/内存/正式素材成本 | 没有测量，不编数字 | 交付超负载版本 |
| 13.正式视听/相机/硬件真人/发行 | 继续OPEN/NOTSEEN | 无画面手感依据即发布 |
| 14.普通回归/保管 | 以本表和独立核验收口，不借review背书 | 混版或遗漏失败 |
| 15.七继承Minor | 未修、明确保留 | 遗漏历史缺口 |
| 16.真实存档语义 | 仅核字节清单，未读取内容、不称兼容验收 | 越界或破坏玩家进度 |

实施裁决：保留原计划/BASE/脏分支，无新goal/task-done/Git操作；代价是必须按实体快照归因。停止无依据额外pin和已经被反证的缓存推广；代价是资源阻断仍OPEN，但不将失败方案装入生产。保留真正native失败作为反例，不挑安静日志；代价是当前suite不能宣称全绿。

新增Minor延期：`test-cache-window-v126.ps1`进入helper哈希清单，但未加入Freeze-Inputs的freezeHelpers。6诊断案的before/after均缺该脚本实体，共12份；真正canonical消费者的实体仍保全。这个缺口影响历史纯合同harness的自包含复查，不改变native FATAL或当前严格拒绝，不补造历史文件。

继承7项：energy-unwind早失败flush、boundary的swept措辞、finally stage4摘要、fresh LateFact来源标注、rotated-AABB负例覆盖、withdrawal后的旧return诊断、unknown Active先后顺序的实际scanner负例覆盖。总8项未闭合，不声称已修。

## 保管、容量与下一实际动作

根37467/41538/34595/6345全部终态；独立审计 `/root/v118_custody_audit` ENDED，最后964bd9/exit0，无handle/子会话；18个记录native已死，CIM KNIGHT Godot/dotnet/owner各0。之后才写本轮收尾文档。

- 45/45前后manifest对字节一致，现存选中实体4218=4100源码+118helper，逐SHA匹配，集合对称、extra/读取/hash fault0。
- 仍明确缺上述12份cache-helper实体，不能称全部helper保全。canonical18份实体全部匹配各案对应版本。
- 54/54封存binary匹配；最终4案8份source同SHA，当前1844源/C#892/Debug6/6匹配最终普通models，无漂移。
- helper联集14路径，canonical/cache-helper各2版本，其余12单版本，逐case核对，非全单版本。18份user manifest同BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB，仅字节核对。
- 最终source E84D152E861BB5C3D14C979FFA5C359AC426196E2BDCA5A2EB21D6A54CEBF456；binary39E6978AFD12793DCA9A73EC7D3D9F7F1265244D44168D3A0D6366BC411FEA26；helper7B69FF6EF4CC07A03614CEDA8335EBEA6E10DB3571DF5A8E4A67C8BD0F6376C7。
- 源码仅RunDetachedEnemyResourceRepro.cs181+/0−，源清单零增删；canonical53+/2−，另新增纯合同helper。Main/Placement/Traversal、HEAD/branch/index/audio/Claw/Skitter锁定值未变，无生产资源/正式资产表增量。
- 单次手动容量Check UTC2026-10-26T02:44:53.8650833，current860284/base3508/growth856776/10files，Notify=false。无新图稿/录屏，没有自动化、后台监测、付费、OS前台输入、Git写入或删除。

下一战斗候选已有生产机制依据，但未实施：原wave Burst确实死亡、原jet/laterK交回、同Rust格挡和真实回左均成立后，针对已锁向的常驻Burst仅做一次Space错线并松L，保留全来源危险扫描/J完整窗口/24能量/900预算，记录真实起跳、三发、自然回能和完整落稳。腾空时须保持既有engagement身份以免原74px筛选导致提前continue而漏扫描。失败就保留失败，不重跳、不扩等待池、不猜弹速。此方案不是现成的安全路线或生产新功能。

资源内部责任链、正式视听动作/相机、原全部地图人口机关、正常双路、目标硬件/真人及授权打包GitHub交付仍须继续完成；不据本报告标记目标完成。
