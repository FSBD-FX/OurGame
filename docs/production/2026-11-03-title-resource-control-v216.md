# v216 · 独立标题资源对照（未修复资源故障）

仅KNIGHT/G:/knight/Knight，无CS/helper/运行库变更的只读对照；不是正式玩法或美术版本发布。原十一项、Main899、完整双路、正常速度视听动作/物理/真人及历史资源故障继续ACTIVE/OPEN。

## 事实

v215 keyboard520a1d90的3条唯一HandleERROR中，前两条栈是第二轮IsolatedTitleHoldSession.Mount的GD.Load和Instantiate；external日志把它们定位在第一轮挂载/audio drain后、第二轮挂载前。第三条是终结器内部SetGodotObjectPtr，未给失败包装身份。不能从相邻日志指认最近一个资源。

本轮仅运行既有`-Phase title-resource-entry -SourceRows1874`一次，case `title-resource-entry-20261103-074254-446-2a690eb6bfa7481cb846eec967602306`，原build180s/native55s、普通编译、headless/contract-only，无fixed-FPS/movie或符号诊断：build33107ms/exit0/0warning/error；native8039ms/exit0/noTimeout/failure=null/guards[]/stderr空。实际独立_Initialize入口、两次mount、每次180physics持有、两次close及starts0齐全；audio drain分别6帧83ms和4帧55ms。

独立保管：5清单对，选定捕获source444/helper14/bin6逐SHA一致；总源1874/helper11/user109/continuation1/bin6前后相同；07:45:32+08当前绑定也相同。两owned创建/路径身份已结束。sourceManifest1BEFF770113AA66E12BA528109537A32FDDB9C1B5D7A967023D017AAACA91242、binManifest78BCECFB9E2BA476D8123BB1A69283DD285E7F2B035C904C451514689709E6EB、DLL11B944A816C123197CD22BF0647F642B0AD723770862C77751E0CE869E3A8CC7，均为此案当时的冻结状态；后续新落地模块不借此旧程序集资格。

## 只读prefix核查

keyboard是KeyboardClawTargetTests→KeyboardClawRuntimeTests→Title。前两测试共8个保存PackedScene的inline Load.Instantiate，无using/Dispose/Free共享PackedScene或Script；退出只释放自建节点树、键盘driver以及Motor自建LandingImpactSampler/query/collision/array。PlayerFormalLayer四个Texture2D读取保存到节点字段，未见提前释放。没有“prefix人工销毁共享缓存”的证据；正常包装回收可能发生于后段，但不是根因证明。

## 推理、风险与下一步

冷进程标题本案安静，不足以证明prefix导致错误或标题本身稳定；间歇时序可不同。保留原keyboard3ERROR严格FAIL，不重跑筛绿、改CacheMode、添加GC/强引用、修改引擎或申请外采来猜修复。需要失败handle实际资源/包装身份链，而不是更多相同完整长测。

并行的动作只读核对发现另一项真实数据缺口：Timeline不接Motor LandingCount/LastLandingImpact，Dash可能吞掉落地状态。它按[独立落地承重计划](../superpowers/plans/2026-11-03-measured-landing-load.md)继续，是原M04/M06/V03/V04增量，不是资源修复，也不关闭R-I/R-C/R-P。所有生产实现先有实际断言RED，正常速度观感仍待后续授权审阅。

无新GUI、OS输入、真存档、付费、媒体、自动化、Git变更或删除。20GiB仅正常汇报时检查提醒，绝不阻塞工作。
