# KNIGHT 单次原生采集 v001 — 未复现，无转储，不是修复

2026-10-28。游戏内容仍v151。本轮仅执行用户明确同意许可后批准的一次隔离采集，不是玩法/画质更新，也不覆盖v150/v151历史FATAL。

## 事实

用户明确回复：“同意许可，继续一次隔离采集”。此前方案和随包Eula.txt已提供。使用已核验Microsoft签名的ProcDump12.02 x64，首次help明确-accepteula，随后仅一次附加；没有安装WinDbg/SDK或注册系统默认调试器。

原有canonical普通prologue-post -SourceRows1857，新鲜Rebuild，不附加identity/verbose/movie/MeasureFullPostBudget标志。原300000ms上限保持；源码、helper、引擎、GC/资源缓存/强引用/Dispose规则、真实存档均未改。

唯一原生案例：G:/knight/验证证据/october13-stable-claw/prologue-post-20261028-032439-266-0852d80858044a8c980b5f460e1a4ef0。

- 根拥有canonical PTY18339和collector supervisor PTY12874均终止0；没有重启这两个会话或补第二轮。
- build0；engine0/211421ms（约3分31秒）/TimedOut=false；原验证器failure=null、guardFailures=[]。
- 原后段75组进入/返回、索引0–74且名称配对；owner8与最终PROLOGUE_TESTS_OK均有真实收据。三类warning scope、stderr4条精确允许警告及external镜像保留；0 ERROR/FATAL。这是附加原生观察器条件下的原检查PASS，不是无调试器基线或性能认证。
- 精确目标PID47648、CreationTimeUtc2026-10-27T19:25:12.1568827Z；collectorPID43980、CreationTimeUtc2026-10-27T19:26:11.6752545Z。附加前保留目标句柄，复核PID/原生出生时间/程序路径/完整原始命令行/普通headless-post/本案例StorageRoot。不是按所有Godot进程名附加。
- 实际参数-accepteula -ma -e -g -n 1 47648及唯一输出目录。实际程序日志显示Unhandled monitor、dump limit1、WER/Kill/clone/terminate monitor Disabled；普通已处理CLR异常只记录日志，没有first-chance转储。
- collector0、TargetExited=true、DumpCount0；原始UTF16LE日志明确目标exit0x00000000和Dump count not reached，无权限失败标记。没有dmp，不存在可供本轮离线分析的崩溃现场；不安装后续分析器。

## 推理边界与风险

仅能判定“本轮未复现”。调试器是否改变了资源/终结器时序尚未证明，不能归因为已修复、故障消失或无资源问题。旧v151未附加post的实际gchandle.is_released()/0xC000001D FATAL和更早证据继续开放。

collector在目标出生约59.518秒后才启动，观察没有覆盖完整启动期；元数据检查并不是暂停屏障。虽然本轮全程原日志及退出未报FATAL，也不能把捕获覆盖说成从进程创建起完整覆盖。一次采集已用完，**未自动重跑，也没有后续自动采集权限**。

原始进程日志与任何未来完整转储均按敏感本地资料处理，不上传GitHub/云端；本轮没有转储可公开分享。源码、资产和所有历史失败不删除、不修改以换绿。

## 保管与落盘

采集根：G:/knight/原生诊断/2026-10-28_单次原生崩溃采集_v001/。

- attach-contract.json、target-verified.native.json、target-original.contract.json：实际附加对象/参数与原合同副本。
- collector.native.json、collector.wait.json、collector.stdout.log、collector.stderr.log：实际采集器身份、终态及原始日志。
- procdump-help.stdout.log/stderr.log：许可接受后的原帮助；stdout无BOM的UTF16LE，不改原文件。collector.stdout.log同为UTF16LE；部分早先只读GetContent/ReadAllBytes观察因编码/共享模式失败，不是目标或采集器权限失败，也没有因此重跑采集。
- artifact-index.json：原案例日志/清单和采集器原件的长度/哈希索引、匹配引擎哈希及原案例六二进制位置；原案例保留完整选定源码与六二进制/PDB，不重复复制大型引擎和工程。

终态后独立只读审计：5/5清单对字节一致；488选定源副本+12helper副本+6封存二进制各匹配，0不符/未列文件。是选定实体保管，不声称全源副本。当前1857运行源/897C#、序列化源清单9380CA28BBBE1B19D43F7251AE38B3E07E23605BC7096949FEFB249593046288；当前普通6二进制DA5DC34EFF8C7C4A5AEB1F35D77F2828E10935CCE9DE92D1F15ACB685A25CDA5，与基线完全相同。canonical6A7ED7DD…、HEAD e7c70498…与index21610FDC…未改。精确build5428/target47648/collector43980当前均不存在；不声称全系统空闲。

## 原目标与后续

原完整十一项范围和所有未完成项保留。此次get_goal实查返回blocked，未擅自重建、恢复、暂停或完成目标；此前文档ACTIVE是各轮历史记录，不能用来覆盖当前工具状态。单次采集结束不等整关、美术、动作、完整双路、资源、硬件真人或交付验收完成。

本轮无生产/测试/验证器代码变化，无付费、前台游戏/系统输入、新过程图片视频、自动化、Git写入或删除。下一步可继续不依赖本次崩溃归因的制作与现有证据分析；若以后需要再次采集，应另行明确范围，不能暗中把本次一次授权改成循环诊断。

收尾实查：artifact-index所列27份原件长度/哈希全部一致，0dmp；仅文档diff检查通过（保留既有LF/CRLF提示），未为收尾重复启动测试。过程媒体一次Check仍1431609字节/16文件，较本地基线增长1428101字节，Notify=false；没有自动化、重置基线或20GiB停工操作。
