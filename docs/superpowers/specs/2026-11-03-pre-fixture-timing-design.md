# pre前缀计时定位 · v215

目标是定位v214原pre180s超时，不以增加诊断替代游戏完成。前一轮是真实进展：四远程身体/动作已接入，但最终16门14PASS/2FAIL；Main899和pre超时、历史fatal、正式动作/惯性/美术等仍OPEN。v213→v214在Traversal进入前慢25391ms，同名Traversal片段只多11ms；当前原日志无法再细分前缀。

采用既有Time.GetTicksMsec/physics/process标量，对RunPrePhase原44项(index0Camera/4Supply/43Traversal)逐条直接插入enter/return，不包装await或执行委托、不重排/跳过/合并测试、不捕获Node/Resource。普通Supply.Run和条件诊断RunCompleteLifetime保持原分支/位置；BeginOriginalProbeTrace不移动，不在普通计时启用。失败不打印return，原异常、清理及成功门保持。

值型边界记录器只保留index/name/3时钟，检查匹配、顺序、单调，允许同步同毫秒/同帧返回；拒绝嵌套/跳号/错误返回/回退/非法名称/未结束计划。日志包含每案真实elapsed_ms/physics_delta/process_delta及44完成receipt，不能称资源已修或行为/观感验收。既有post-boundary-contract容纳纯计时合同，旧post合同保留；canonical对完整pre要求44有序配对/原名称和准确差分，解析器含缺失/重放/伪造/回退负控。

仅修改tests/RunPrologueTests.cs、tests/unit/Task7CaseRegistryTests.cs和原canonical；无新CS/资产/运行时，SourceRows1874。Root唯一writer/串行native；运行时冻结所有CS/helpers。原build180s、focused55s、pre180s/post300s、完整路线600s不变，无ExpectedFailure/新GC/保活/强制帧率/新崩溃采集。只无窗口/隔离存储，不碰真实存档、Git、媒体/付费、删除或自动化。

通过标准：纯记录器与原post合同先RED→GREEN，实际普通pre产生可信阶段分布；完整pre/post及输入相关回归真实结果全部保留。若pre仍失败，不能称超时或资源问题已修；依据最新分段证据决定下一修复，不重复整轮筛绿。主线额外地面跳仍待原问题答复，不改策略。采用原地inline实现+一次fresh整变更审查、一轮C/I修补，精确基线/ledger代替未授权Git操作。
