# 居民几何55秒门：只读阶段计时

v244 geometry在55秒原cap杀进程时写出3,387,392字节/8680行JSONL，末行截断；它位于WriteDiagnostics，后面仍有原body/走廊/分布控制及两ProcessFrame释放。不能当成仅清理超时，也不能从末行认定I/O耗去大部分55秒。

本次仅在原Run调用边界加累计Stopwatch阶段回执：入口、候选包后、activity前后、原leaf controls后、diagnostics写前/后（写前含已收集记录数）、原body控制后、corridor及selection后、fixture退出后。原步骤顺序、所有数据/循环、原JSONL内容、await/预算、成功回执和异常传播均不改。没有新GC/保活/延时/查询或场景构建；计时是诊断，不是性能优化或放行条件。

只改tests/integration/CanyonResidentPlacementTests.cs。若捕获到阶段耗时，再按证据选择后续优化，本次不删猜测冗余条件、不提高55秒。不制造新媒体/自动化，不启动额外崩溃采集。严格门仍由既有owner检查，不因新回执或局部完成叫绿色。
