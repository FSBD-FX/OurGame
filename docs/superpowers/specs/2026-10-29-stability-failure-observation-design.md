# 稳定性故障可观测边界

上位目标仍是完整原十一项序章，当前G:/knight/Knight，不是UE。上一目标轮属progress：v155真实五岩体绘制接入并取得新失败证据，非交付。当前全目标ACTIVE。

只读源码与实际v155日志确认两处观察缺口：Stable runner先await/强制终结器清理才上报捕获异常，故清理FATAL可能遮掉初始错误；pre在最后遍历测试附近180秒截止，既有成功日志在cleanup前且无事件时钟，不能区分累计耗时与晚段停顿。两者均未证明资源根因或玩家物理有错。

本切片只改测试/验证工具：把原完整异常的单次GD.PushError移到catch第一条语句，不新增GC、缓存/强根、资源Dispose或生产修复；在原exit-drain调用前增加独立ENTER日志。实际sentinel走同一个catch，保留原cleanup与Quit1，验证初始错误先于ENTER。只该明确负控可承认一个确切sentinel错误，其他错误/警告/FATAL/超时/普通成功收据全部拒绝；结果是失败记录合同PASS，不是游戏PASS。

pre只增独立OBS事件：最终traversal await的entry/return，fresh-sample entry与原finally cleanup之后的normal return；记录Time.GetTicksMsec、physics/process计数、原样本身份，不改原收据、调用顺序、输入、等待或预算。不把末个OBS解释成已知死锁，不变更主线/地图/数值/资产。

原55秒focus、180秒pre、300秒post预算与三日志严格门保留。纯parser需字面正反样本，真实sentinel必须RED顺序→GREEN顺序。随后普通recycling一次、models及pre/post一次，原错误全保留。无前台/OS输入/新采集/付费/自动化/Git/删除/真实存档语义读写。新源文件不入runtime；既有1857/897库存应保持。唯一fresh最终审查，Minor延期。完整Main/Explore/像素/正常速度AV/硬件/真人/正式交付仍OPEN。
