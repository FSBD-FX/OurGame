# v257 · 纯依赖契约修复与区域配置资源身份

状态：旧资源身份契约的误拦已通过真实RED→GREEN修复；本轮一次身份测试仍严格FAIL，但获得明确资源类型/调用括号。普通构建已恢复，与v256普通1897源/6binary及15宿主逐字节一致。唯一新审查C0/I1/M0；I1已完成一次实际RED→GREEN修补，最终267项纯契约控制通过，无二审/原生重试。原完整十一项继续ACTIVE，不宣称资源已修或正式交付。

## 修复范围与控制

只改旧test-pre-finalizer-identity-admission-20261021.ps1的导入前缀，新增独立实际前缀测试。canonical宿主、C#、玩法、碰撞、美术、场景和配置未改。旧门因Get-ClawBuildArguments调用已有纯Assert-ProfilePairAdmission，按任何CommandAst一律拒绝，导致合法入口无法做完安全验证。

新门先验证两个唯一精确命名/原签名/完整AST定义，builder仅可直接静态调用该纯依赖，依赖内部不得调用命令；任意其他命令、成员访问、重定向、scoped变量、非类型约束属性、嵌套函数、间接调用、缺失/大小写重名/改签名仍拒绝。审查I1还补拒绝没有CommandAst的直接文件读取语法switch -File。两函数全部验证后才导入。这是可信项目中两个指定标量边界的校验，不是通用PowerShell沙箱。

新测试执行实际契约导入前缀，供给实际/受控AST，独立literal普通7/REF8 argv及pair许可。污染体只测试导入，不会执行；旧门RED为6项合法正例全被拒，非语法失败。初修24项通过。审查I1的builder/依赖两个switch -File负控先实际RED（被错误接受，但从未执行读取），最小File标志拒绝后GREEN26；原完整157 argv/回执/负控及v256相关84项也通过。最终新鲜重跑26+157+84=267全部通过，不启动native。没有删原断言或跳过拒绝来换绿。

| 本轮执行 | 实际结果 |
|---|---|
| 新前缀与受控AST | RED6个合法正例失败→GREEN24；I1两个负例RED→最终GREEN26 |
| 原完整身份契约 | 157通过，未启动engine |
| v256 trace契约 | 84通过，未启动native |
| 现有pre身份测试 | 77783ms，原生FATAL，严格FAIL，无超时/guard0 |
| 普通models恢复 | 523ms，PASS，无身份符号 |

## 本次故障身份：同一调用括号，不是相邻日志推测

证据根G:/knight/验证证据/october13-stable-claw，case `prologue-pre-refcounted-identity-20261105-184654-607-e80494bae2d643319882315e32ed9040`。

stderr37482：ENTER call810，disposing=False、type=Knight.Prologue.Seamless.PrologueSectorDefinition、thread1、serial1076、managed_hash20852350、pointer0x000001CD8E54AF60、native_identity=unknown。37483紧接gchandle.is_released FATAL；37486–37489栈明确包含该类型的Dispose(bool)探针249→Finalize。这个实例的终结调用触发本次失败；不把它自动等同于此前所有普通FATAL的对象。

逐call配对810 ENTER/809 RETURN，仅810未返回，0重复/格式/类型-线程-disposing-hash-serial-已登记身份错配；没有把所有进程死亡后未返回记录都说成原因。三raw无本次Handle/SwapGCHandle或managed测试异常。未见cap/fault/incomplete标记，但进程未到检查/END，不能宣称整体记录无丢失/无fault。

stderr37356的serial1076分配堆栈未截断：ResourceLoader.Load→GD.Load→BasinSupplyRuntimeTests.CreateSampleFixture576→OpenSample520→CheckSupply317（lifecycle Continue重挂），thread2/no-active-probe。它给出分配来源，不是native ID/实际owner/sector归属。stdout表明Camera/Laborer/Claw/HealthRestore已返回，Supply生命周期例也返回，第四个damage1例进入后未返回；跨线程FATAL后的Player构造输出不当因果顺序。

## 下一项测量已缩到现有替换点

只读源码链：保存PrologueLevel场景在229行导出五个authored definitions；world.Definitions为普通Array auto-property。Supply589–593已有before-replace→ConfigureLegacyWorld→after-replace位置；BasinQualitySampleEntry92换成BasinSampleLegacyLayout新建的五个definition数组，发生在AddChild前。

这条路径没有对旧数组/元素using、Dispose、Clear或Free。属性替换移除了该属性的旧引用，不能证明旧对象已析构或PackedScene不再持有它。

现有REF构造hook只有serial/stack；现有sector flag在替换点有native ID但没有serial，且混开被禁止、会在Descent走额外GetState路径。不能简单混flag，也不能把别进程ID与serial1076关联。下一应在原before/after位置增有界serial→native ID→root/world/index/sector/stage标量关联，不额外Load/GetState/图遍历/保资源；本轮未实施，不预先宣称根因或更改释放策略。

## 权限与绑定

现有pre入口effect审计：原Camera/Laborer/Claw/HealthRestore/Supply顺序、原reader的GetState/GetScript、原exit两次GC/等待保持；flag新增的弱引用/标量/native字段读取与日志会扰动时间。Supply观察异常时释放本次自有未交付root的原异常清理保留，不伪称绝对零生命周期变化。没有旧adapter/新转储/新增GC/cache/KeepAlive/引擎/图遍历策略。

root44579终态exit1（identity1/restore0），只跑一次身份案，无筛绿重试；restore case184857-850-bdbee350356d432783d1fbd1e509d603为原7argv Rebuild/native523通过。两案合计10组保护对在运行内均无差异。I1修补前的[audit-v257](../../.superpowers/sdd/2026-11-05-refcounted-preflight-v257/audit-v257.json)记录当前1897完整源集合、6普通二进制、13个涉及宿主工具零差异。I1仅在两案结束后更改旧身份契约：相对两案的当前13helper必有且只有这1项已知差异，其余12项不变，不能继续把旧审计称为最终13项零差异，也不重写原始manifest。普通source/binary与v256完全相同；旧REF契约不在v256普通15宿主中，故[普通15宿主绑定](../../.superpowers/sdd/2026-11-05-refcounted-preflight-v257/v256-runtime-helpers-binding.json)仍全部不变。最终结果另存final-binding-v257.json；不为修平hash而重复身份测试。

普通全16没有重跑，最近一次仍是v256的15PASS1FAIL；只读契约更改不冒充新通关/普通全绿。身份案行为资格false/wholeGoalfalse，恢复普通后没有遗留诊断符号。

| 本轮资源 | 来源/状态 |
|---|---|
| 契约前缀与最终26控制、文本日志 | 本地before/初审diff/最终diff/两轮RED与GREEN/raw保存；测试基础设施，不是美术资产 |
| 新图稿/实录/付费图/音频/模型 | 无新增；V1主体原图暂缺，后续审现稿、不转授批准 |

## 唯一审查、修补及全部裁决

唯一新审查为C0/I1/M0、With fixes。I1是直接文件读取语法逃过CommandAst检查，根实际补两个ImportOnly负例并RED→GREEN；267项纯控制通过后保留本有界增量，不进行第二次审查。新Minor为0，历史延期（含v256真实wholeGoalAccepted初始化覆盖）不因此关闭。详见[完整审查与八项Declined裁决](../../.superpowers/sdd/2026-11-05-refcounted-preflight-v257/final-review-v257.md)及[逐项进度账](../../.superpowers/sdd/2026-11-05-refcounted-preflight-v257/progress.md)。

| 根裁决 | 理由与代价 |
|---|---|
| 1. 原地继续有界Native/契约任务，不重复询问路线选择 | 用户要求直接判断后推进；保before/不动Git，代价是脏树隔离有限。 |
| 2. PS契约修补不重跑未变的普通16门 | 无C#/canonical/runtime变化；保留v256的15/16，代价是无新普通全套资格。 |
| 3. I1后不重复身份案以消除helper差异 | 一次身份目的已达到，禁止筛绿；代价是身份证据绑定修补前helper，最终明确1项已知差异。 |
| 4. 不扩为任意恶意PS沙箱 | 只限可信工程两函数；代价是alias/session/type转换等不提供通用安全承诺。 |
| 5. 不把终结调用类型当真正owner或所有旧故障根因 | native ID/root/sector未知；代价是崩溃仍未修，下一需有界关联。 |
| 6. 无END不声称全日志无丢失/无fault | 只有已见810/809本地配对；代价是最终完整性未知。 |
| 7. 根重算绑定，非审查员第二次全量重算 | 原始保护对与最终hash单独核对；代价是不具双份独立全量审计。 |
| 8. 审查员不跑native，根不追加重复身份案 | 保留真实一次FAIL与普通恢复；代价是无重复发生率保证。 |
| 9. 不据此普遍认证旧reader/drain/异常清理 | 仅效应审计及本案证据；代价是既有生命周期疑问仍欠。 |
| 10. 原十一项/正式动作/完整视听验收保持OPEN | 不以测试计数代替游戏质量；代价是后续仍有实质制作与人工验收。 |
| 11. 保持headless等现有权限边界 | 无GUI/付费/新媒体/dump/GC-cache变更/真实存档/自动化/Git/删除发布；代价是视觉交付须后续授权。 |

最终[root实际重算绑定](../../.superpowers/sdd/2026-11-05-refcounted-preflight-v257/final-binding-v257.json)与只读子任务独立重算相符：10保护对、1897源与完整集合、6普通binary均无差异；REF案13helper仅上述旧契约1差异，restore案9/9相同，v256普通15/15相同。原契约尾段26853原始字节完全相等，分支/HEAD未变。单次容量Check：2026-11-05T11:22:36Z，7,427,750B/86files/Notify=false，没有新增媒体或自动化。

全十一项、正式美术连续动作/R-I/R-C/R-P、76居民全量、正常显示从标题/硬件/真人认可仍OPEN。V1原件暂缺按U-20261105-01后审现稿，不转授旧批准、不扩大恢复查找；三段误给视频继续作废。本轮无生产美术/动作/物理增量，不作为体验重大提升版本发布。
