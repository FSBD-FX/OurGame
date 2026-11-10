# v257 · 资源身份诊断契约的纯依赖

事实：v256普通16门15PASS1FAIL，pre22182原生FATAL归属未知；当前普通Main/Explore/Boss/支撑出口通过。既有pre身份契约真实在第17行拒绝，唯一命中是Get-ClawBuildArguments内已有Assert-ProfilePairAdmission调用。该依赖当前只读Name/Identity/Mixed并throw，不做外部操作。

修复旧契约，不改canonical宿主/游戏/诊断CS。解析实际owner，只允许唯一、精确命名与原签名的Get-ClawBuildArguments和Assert-ProfilePairAdmission；验证完整选中定义（含参数）后再导入。builder中仅允许对该依赖的直接静态调用；依赖内不允许命令。所有任意命令、成员访问/方法、重定向、带scope变量、非类型约束attribute和嵌套函数拒绝。依赖缺失/重名（含大小写覆盖）/改签名/未知或间接调用拒绝。不执行owner顶层，不声称通用PowerShell沙箱。

测试执行真实旧契约的纯导入前缀，给实际或受控AST；独立literal期望普通7argv/REF8argv及pair校验。负例只导入，不调用被污染函数，所以即使校验错误也不执行不安全body。先看实际旧前缀拒绝合法依赖RED，再最小GREEN；原269行中的全部argv/混用/身份回执正反控保留并完整运行，另跑v256的84控制。

审计确认现有pre REF入口不新增GC/cache/KeepAlive/engine/GetState/graphwalk策略：BeginOriginalProbeTrace写scalar/弱引用和日志，原Camera/Laborer/Claw/HealthRestore/Supply顺序保留，原reader已有GetState/GetScript不变。原process-exit drain两Collect也不改。Supply观察异常时会释放本次自有未交付root，不伪称零生命周期副作用；native字段读取/日志分配影响时序，不作行为资格。

纯控制全通过后，仅现有`prologue-pre -IdentifyRefCounted`一次/隔离存储，禁新dump/adapter/额外GC或新增观察；无论结果如何都用普通models Rebuild恢复，核当前1897源/6binary与v256绑定。新诊断只用于故障归属，不把quiet当修好，不重发未变化普通16挑绿。失败原件全部保留。

唯一G:/knight/Knight；root唯一编辑/build/native，运行前冻结。仅headless/internal keyboard，绝无GUI/OS输入/付费/新媒体/真实存档实验/引擎与GC缓存改变/自动化/Git/发布/删除。完整十一项及正式美术连续动作/R-I/R-C/R-P/正常显示硬件真人继续OPEN/ACTIVE。
