# v219 · 真实起跳与普通空中关节动作

状态：已接生产，唯一审查C0/I1/M1；Important实际RED→GREEN，同一轮修补还修正完整后段暴露的机械爪自然收回顶点分类回归，当前48控制及原爪案例通过。最后完整同源13门10PASS/3FAIL：world-jet退出警告、pre原180秒超时、keyboard专项失败，不能称整套通过；Task1仍in_progress。不是正常速度精美度/物理手感、完整动作或原十一项完成证明。

## 事实、判断、风险

事实：原Timeline将任何踩地→离地都当成JumpStart；真实按D走离200px有限平台、J0/Dash0仍出现JumpStart的反例24e94240已保存。普通上升/顶点/下落原为固定整腿角度与默认局部脚底。地面步态已有真实支撑与关节，不重复宣称缺失。

本轮新增只读AcceptedJumpSequence，沿原StartedJump实际接受点递增，不改变现有AcceptedJumpCount/跳跃次数预算、力度、重力、碰撞、按键或存档。真实Coordinator传观察值；走落直接Fall，实际空格/土狼跳才进入JumpStart。旧无观察合成调用保留兼容，但正式入口有实际观察，不用兼容分支冒充因果。

普通空中采用真实物理tick/速度驱动骨盆与躯干、十个分件及不对称膝踝收拢→过顶展开→下落伸腿。原18＋18固定腿段解算保留；离地捕获已显示的局部脚踝/单位鞋朝向，再插值接入新姿态，避免高速上升时仍将脚钉在旧世界坐标。实际落地、死亡/恢复、机械爪承力和短喷优先，不累加两个空中负载。

判断：这是R-C/R-P连续动作承载的一段实质改进，不是“加状态/放大缩小图片”；仍未解决完整起跑/刹停/转身语言、全套正式分层美术或整个惯性手感。程序目标幅度/节奏为开发暂定，不能替代正常速度视觉与操控审阅。

风险：真实/可控需一起标定，不能用加大滑行拖慢响应；不为本轮动作而改力学或关卡。新增单调观察序号不参与存档或动作权限。正式风格/硬件/FPS/正常双路/Main899/历史资源错误及原十一项目标全部OPEN。

## 主要反例和证据

保存根 `G:/knight/验证证据/october13-stable-claw`；按案尾前缀可唯一定位：

| 案尾 | 结果 |
|---|---|
| 24e94240 | 实际walkoff却JumpStart行为RED，J0/Dash0 |
| d4070725 | 缺普通空中纯模型行为RED，非编译错误 |
| 0e1ce970 | 初始39控制＋旧短喷模型GREEN |
| 9853b92b | 实际Body未跟共同受力Draw RED；walkoff已Fall |
| 103246d5 | CS0117：ThreadRecovery是Cue非Action枚举；编译FAIL保留，不算行为RED |
| 98b80c85 | 实际Draw8组/80附件/16脚掌及离地接续、旧gait通过 |
| 1aaf1b81 / aa512809 | 完整跳阶段断言失败；诊断确认原一帧Space触发既有230截短，starts1/apexfallcontact全真，稳定上升段未覆盖 |
| d6dfadc3 | 保留短跳，另加完整按住至顶点；两朝向4落地及实际coyote/脚掌/关节通过 |
| 3b116589 | 自查真实落地被过旧原因数据绕过，行为RED后修正 |
| 18cf99c0 / 341e148f | 当前41模型控制＋真实cause/Draw/native和host2正16负通过；完整回归另计 |
| 294b4582 | 候选world-jet行为结束/exit0，但一条WARNING报告2ObjectDB实例泄漏，严格FAIL保留 |
| bbdf7046 | 审查Important：零时间/重复tick落地未消费原因，随后离地误播，实际RED |
| b955852a | 修补后43控制＋旧短喷68/观察和host2正16负通过，完整套件另计 |
| 00ed016b | 第一轮完整post实际FAIL：爪自然收回后vy=.0010886595，原期望Apex却得到Fall；host的scope收据缺失为后续现象，不掩盖原行为断言 |
| f771f843 | 字面量近零空中Apex反例实际RED，原post断言和物理值未改 |
| 4c3e3b6a | 修补后48控制＋旧短喷68/观察、host2正16负通过；原零时间/重复tick保护保留 |
| 2ec5e21d | 原formal-claw-phases严格PASS，实际自然收回与模式取消在微正vy下恢复Apex，未放宽旧断言 |
| ec71ec5d | 最后批次world-jet仍exit0/行为收据齐，但一条WARNING报告2个ObjectDB实例泄漏，严格FAIL；根因未证 |
| 57287f3e | 最后批次pre在原180000ms上限以180210ms终止，TimedOut=true/exit-1；不扩预算、不把前缀通过冒认为整段通过 |

普通完整跳真实上升近腿lift约8.47→下落约.72；短跳有自身短弧线，不伪造稳定高上升。每段仅真实Space1/J1/Sequence1/Dash0，采样来自同一FramePublished中的实际脚/身体/Motor；不是直接写速度或摆拍。采样数量不等于独立物理帧数或硬件FPS资格。

## 审查与当前回归

唯一fresh-context审查完成，七个旧文件精确手工基线＋三新CS/新host helper；不使用包含此前dirty工作的HEAD全差异。C0/I1/M1：零时间或重复tick落地未消费的新序号会让下次walkoff误播起跳；bbdf7046实际反例后，仅令权威ground分支无条件消费最大序号，运动采样不额外推进，空中零时间仍延迟消费。b955852a的43控制与旧短喷68/观察、可执行host控制通过。一轮C/I修补，无二审。

完整回归又暴露了一个真实回归：将所有无新起跳、vy非负的空中状态归为Fall，会误伤机械爪自然收回后的近零顶点。保留第一轮原post失败，并先做字面量Apex反例；修正为仅有实际踩地→失足原因的持续下落保留Fall，通用爪释放空中仍按原±80带判定。新增持续walkoff与0/.0010886595/±60控制，当前总48；原爪阶段案例也实际恢复。不是改阈值、注入速度、改旧断言或增加二段跳。

延期M1为覆盖欠账：两个非零owner切换、部分格挡、显式机械爪接管/释放、真实缓冲跳及Motor重置后的序号连续性未被新增夹具穷尽。原M04/M06/M09/V03仍开放，不能将48项或priorities=true解读成全部组合通过。缺失Motor时default观察仍走旧兼容分支，这是未认证的异常组件韧性边界，不冒称组件丢失时已失败关闭。当前已有的原爪回归通过不补签所有新增组合边界。

正常速度审美/惯性、硬件FPS、完整双路/Main899、历史资源故障和全目标保持OPEN；理论ulong耗尽不做无实际玩家影响的改造。最终套件已终态，并非整套GREEN；不通过重跑筛掉警告或扩预算取得放行。

## 当前版本绑定与保留失败

最后11阶段批次97228终态exit1，连同同源模型4c3e3b6a和原爪2ec5e21d共13门。后段17ef64c6与动作73307c17、模型子集b237fc21通过；world-jet ec71ec5d、pre57287f3e、keyboard bc805bb9失败。逐案原始证据与后续元数据核对不替代正常游戏视觉审阅。

| 当前阶段 | Build ms | Native ms / exit | 严格结果 |
|---|---:|---:|---|
| presentation-model · 4c3e3b6a | 30334 | 547 / 0 | PASS，48普通空中＋旧68短喷控制 |
| formal-claw-phases · 2ec5e21d | 33941 | 4894 / 0 | PASS，原爪收回/取消案例 |
| ground-gait · 05471f3c | 30155 | 9261 / 0 | PASS，Draw8/80/16、4落地/coyote及旧540/569 |
| presentation-rig · d37295c6 | 31978 | 3052 / 0 | PASS |
| impact-player · d0b90642 | 29839 | 5941 / 0 | PASS |
| impact-negative · 302b126d | 31107 | 14851 / 0 | PASS |
| impact-support · 562d30e6 | 29534 | 19351 / 0 | PASS |
| world-jet · ec71ec5d | 28949 | 3693 / 0 | FAIL，1条警告报告2实例泄漏 |
| prologue-pre · 57287f3e | 29514 | 180210 / -1 | FAIL，原180000ms超时 |
| prologue-post · 17ef64c6 | 30860 | 224927 / 0 | PASS，4条预期限定scope警告；原75项/owner8/drain/full收据 |
| actions · 73307c17 | 30764 | 9522 / 0 | PASS |
| keyboard · bc805bb9 | 30345 | 20609 / 0 | FAIL，3条Handle ERROR |
| models · b237fc21 | 30061 | 629 / 0 | PASS，仅现有爪模型子集 |

pre最后完成index11 GrappleSurfaceTests.Run，engine_ms174725；index12 MechanicalClawRuntimeTests.Run在174726进入，内部最后GRAPPLE_DAMAGE_INPUT_INVALIDATION_OK，未返回即达原180000ms限制，终态180210/exit-1/TimedOut=true，stderr0。只确认超时位置，不声称该夹具完整通过，也未证明慢测根因。

keyboard stderr第1/38/74行均为`Handle is not initialized`，首个调用栈为GCHandle.FromIntPtr→SwapGCHandleForType→ResourceLoader.Load→IsolatedTitleHoldSession.Mount（RunIsolatedTitleHold.cs:121）。这是已观察到的资源/Handle错误，不是已观察到的玩法断言失败；exit0、两次Rust与夹具内title合同标记齐也不能覆盖严格失败。本轮没有额外独立title-entry原生记录，不冒称它通过；具体根因未证实。

只读助手核对最终13案的65/65份before/after清单文本对均相等，各案source1879/user109/continuation1/bin6；26个保存的owned进程身份均有匹配合同与终态wait，构建原180秒/专项55秒/前180秒/后300秒预算未扩大。其范围仅为元数据和日志，没有重新哈封存载荷、当前文件或复审代码；当前绑定由根执行者单独核验。旧失败均保留，没有用当前安静案例代签历史资源根因。

根执行者在批次终态后只做一次当前绑定核验：13案source-after/bin-after清单各自一致；当前1879个源码、16个helper并集、6个二进制全部逐项匹配，无不一致。未独立重哈每个封存源码副本。当前无KNIGHT Godot/dotnet原生进程；Git分支/HEAD/index保持原值。

- source清单SHA256：`6C50D5C9DD14BF79D6D30ECF84AE200649DC14E1EE788428E20E76360F0CE27B`
- Knight.dll：`57B1F2CC023CEDF17F59790718E261CBFF4767800194BD55916FC76FDA2B52BA`
- canonical helper：`73CF17888ACB83DC169423676A43F6AAC2CAE419C77EEF8C2C8AC32CA17731BF`
- Git HEAD：`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`；index：`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。

媒体流程内检查：7427750 bytes/86 files，Notify=false/NewThresholds=0；本轮没有新增过程媒体、没有重建自动化。

## 实施边界与保留问题

- 采用真实事件＋连续速度目标，不新购Spine/生成位图；暂定映射可能需正常速度返工。
- 局部踝点插值不是永久踩地或图片拉伸，真实动作不延后等待动画。
- 额外完整跳与短跳只改测试驾驶，不改JumpCut或放宽完整跳门。
- 原地单写者/手工基线/串行原生；无Git、清理、自动化、前台输入、付费或真实存档操作。
- 本轮证据核对范围为原始manifest/进程合同/实际结果及最终源码/二进制绑定，不声称另行重哈每个封存源副本。
- 退出泄漏只读线索：world-jet末叶仍在新燃烧后2frame直接Free→Quit；历史v017曾指向自有重复WAV/PlaybackWAV，但本次52fa的两个对象身份未证。当前生产已有voice/stream/ownedWAV/tween清理，不盲目Dispose共享资源，不加退出等待/GC掩盖失败。运动任务不改音频；此线索不是已修根因。

当前没有新窗口、图稿、录屏、付费生成或引擎替换。

下一步先针对已有退出ObjectDB/标题ResourceLoader Handle和pre耗时证据做窄范围无窗口诊断，不重复整套回归筛绿，不盲目释放共享资源或加退出等待。动作后续仍需起跑/制动/反向与支撑相位耦合、角色化关键姿态/收势和有授权的正常速度完整样段；三项R-I/R-C/R-P继续按各自质量门验收，不由48模型或13阶段数量代签。
