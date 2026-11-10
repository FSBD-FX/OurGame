# v222 · 转身时附件惯性与披风坐标接续

状态：生产接入，当前专项65769d0a严格PASS；唯一fresh审查C0/I0/M1，无C/I修补或二审，覆盖Minor延期。最终当前同源17阶段14PASS/3严格FAIL（world-jet/pre/Main）；探索线到Boss和有实体支撑出口通过。Task1仍in_progress，不是原十一项/正式动作/可感知惯性或操控验收完成。

## 事实与改动

当前已有地空加减速、反向制动、短喷余速与重力；普通Facing随实际速度过零改变，不是按反向键立即翻转。BodyLoad保存世界量、脚底保存支撑局部量，本次没有重写这些机制。

真实Timeline的零时间翻面却让披风惯性X从-2.195976变+2.195976：旧弹簧位置/速度仍在旧朝向局部空间。现在翻面前一次重基四通道X位置、X速度、角度、角速度及缓存输出，不加时间/外力，不清空尾迹。Y、弹簧频率、限幅、颜色、尺度保持。

后续真实Draw进一步暴露举盾披风.16rad附件旋转：即使弹簧已正确重基，相同惯性增量的世界Y仍从-.1906变+.2298。现在真实Timeline帧明确父空间来源，披风仅把有界变形增量通过附件逆basis投影为Draw局部；上沿rest点不动，不搬整个披风节点。旧手写帧及直接cloth接口保留原cloth-local合同，原完整mantle测试未删改。

静态不对称附件的rest位置依然随身体翻面，本项不声称所有顶点绝对世界位置不动；保证的是相对rest的世界惯性增量与速度。具体造型/翻身设计/正常速度美感仍待审阅。

## 原始失败（均保留）

证据根：`G:/knight/验证证据/october13-stable-claw`。

| 案尾 | 实际结果 |
|---|---|
| 92bd9db9 | 测试漏Unit命名空间，CS0103 BUILDFAIL，无native；不算行为RED |
| b52f201e | 原Timeline实际SECONDARY_FACING_CONTINUITY_RED，native541ms，世界横向偏移瞬间翻边 |
| 04df16ef | 弹簧6/24/840模型及旧spring通过；真实Draw的Mantle失败 |
| 746af91c | 只增加失败数据，明确为Shield/.16rad的局部与父空间混淆；未修改生产或筛选quiet |
| a883cc70 | 新Draw及旧mantle/air/landing/body/gait原生均通过，但严格HOSTFAIL；旧landing消费者提取的复合guard被新收据混入。随后把新guard独立出来，旧guard和旧测试保留 |

新host负控还曾因PowerShell未括号连接表达式得到16项而非12项，实际检查类型/长度定位后只修数组分组；不是删负控求绿。当前新2正12负、旧landing1正7负实际执行通过。

## 当前专项

`thruster-ground-gait-20261103-211410-870-65769d0a8912425d8bc63e810e16925d`：native9422ms/exit0，未超时，failure=null/guardFailures=[]/stderr0。

- 两初始朝向×30/60/120采样：6场景、24非零通道、840去驱动尾迹；零时间、反复翻面、隐藏弹簧速度继续收敛；原完整spring测试一并执行。
- 实际保存Formal叶场景：两朝向×无动作/攻击/盾6场景24通道，节点增量/旋转与真实披风Draw mapper，上沿固定、身份反例、单根镜像和层不缩放；原完整mantle测试也执行，包括4组真实Timeline竖直落地。
- 原普通空中、测量落地、共同身体承重、地形脚底、固定腿长、真实Motor/Coordinator接线及既有host控制均继续执行，55秒上限和错误门未改。

上述是受控原生几何/行为证据，不是新增角色图片、窗口显示、正常速度审美或硬件试玩。

## 决策与代价

1. 在已授权原动作/惯性范围自主选择具体接续修补，不再要求程序性确认；具体美术质量仍须后续授权审阅，不能当成动作稿获批。
2. 原地保留dirty codex工程，以七份精确基线、串行native和当前文件核对代替Git/worktree变更；代价是仍依赖本地基线/证据保管，没有新发布版本。
3. 先修可复现附件惯性接续，不盲目增加滑行；代价是高速制动语义、拖脚/换支撑与专属动作语言尚未由本项实现。
4. Draw实际失败要求扩展到旋转附件的坐标边界；采用明确父空间来源，同时保留旧手写帧语义。代价是增加一项init来源信息；真实变形包络在父空间水平12/竖直7，局部投影分量不再错误地二次裁剪。

## 审查结论与全部边界裁决

唯一fresh审查：C0/I0/M1。确认四通道隐藏速度和缓存同样反射、Timeline真实来源传递、cue用record复制保留来源，以及Draw仅一次逆basis变换；未发现必须修补的生产缺陷。没有第二次审查。

覆盖Minor延期：新parent-space分支还缺非零root/mobility lean、实测BodyLoad、旋转/非均匀缩放附件、退化/非有限basis和连续同朝向零时间的直接案例。现有数学与保护未见错误；原旧mantle测试不被冒认为这些新分支的资格。本轮不顺手补Minor。

对审查明确未判的各项，根代理裁决如下（均保留成本）：

1. 正常速度审美、可感知惯性、重量/操控仍OPEN；当前仅无窗口权限，代价是不能关闭用户质量反馈。
2. 硬件、渲染器性能和真实像素仍OPEN；几何测试不证明这些，代价是后续设备/显示验收仍必需。
3. 完整十一项、Main899、物理标定、高速制动/换脚/动作特色仍ACTIVE/OPEN；代价是局部接续不构成整套成品。
4. 原world-jet立即Quit及历史Handle/FATAL资源失败保留严格判定；代价是不能宣布资源修复或整套全绿。
5. 原始结果/当前绑定/终态保管由根代理另验；审查未重跑所有案，代价是还须最终证据核对，不能以审查代签真实性。
6. 只保证Facing坐标接续，不承诺对任意同时变化的祖先/根/动作进行通用世界稳定；代价是独立旋转/移动还会产生额外姿态运动，需要完整质量样段。
7. 兼容限于已核实的工程重建/源码接口；未发现的外部二进制或序列化Frame使用者不冒认兼容，代价是未来接入须单独判断迁移。

## 最终同源回归

全部阶段使用同一冻结源码1885；focused24932、compat11116、final89299都已终态，最后两个外层因严格失败exit1。没有因失去观察而重启，也没有筛选quiet。构建均exit0/未超时，原build180s/普通native55s/pre180s/post300s/路线600s预算不变。

| 阶段 / 案尾 | Build ms | Native ms/exit | 原始E/W/F | 严格结果 |
|---|---:|---:|---|---|
| ground-gait · 65769d0a | 29222 | 9422/0 | 0/0/0 | PASS；新接续＋全部旧委托 |
| presentation-rig · 7ea081c2 | 27584 | 2745/0 | 0/0/0 | PASS |
| world-jet · 8bb0a11d | 27563 | 3584/0 | 0/1/0 | **FAIL**；2个ObjectDB实例泄漏警告 |
| actions · 6dfffca8 | 25877 | 9503/0 | 0/0/0 | PASS |
| quit guard-fault · a1967ec9 | 24840 | 4141/0 | 0/0/0 | PASS |
| quit guard-recovery · 0349a5ef | 26448 | 11470/0 | 0/0/0 | PASS |
| quit guard-persistence · 163cc386 | 27325 | 9235/0 | 0/0/0 | PASS |
| quit contracts · ef15d5ca | 31516 | 3219/0 | 0/0/0 | PASS |
| quit menu · c7d9439d | 28729 | 1452/0 | 0/0/0 | PASS |
| quit window · 87ec1eb9 | 25203 | 1448/0 | 0/0/0 | PASS |
| quit paused · 36ee9e58 | 24227 | 4048/0 | 0/0/0 | PASS |
| keyboard · 4a1fcc89 | 27659 | 20154/0 | 0/0/0 | PASS；独立title7718ms/0，两次挂载/关闭 |
| models · 8bd9f203 | 29118 | 531/0 | 0/0/0 | PASS；仍仅原爪模型子集 |
| pre · a566d66f | 27987 | 166867/0 | 3/0/0 | **FAIL**；虽原44项及drain完成，但3条Handle ERROR |
| post · 924d74ba | 26742 | 193041/0 | 0/4/0 | PASS；原75项/owner8与4条限定负控警告 |
| Main · 738c05c0 | 26697 | 125605/1 | 1/0/0 | **FAIL**；原配重区敌群未清 |
| Explore · 00ccd3b4 | 26876 | 313494/0 | 0/0/0 | PASS；原正常计划、Boss及有支撑出口、恢复0/0 |

### 三个明确失败

- world-jet保持原立即Free/Quit路径，一条WARNING报告2实例；非verbose本案没有具体对象ID，不能复制v220的ID来冒认本案归因。真实退出七案通过不等于该诊断或历史资源门修复。
- pre完成原44有序项和phase/drain，但stderr第1/57/112行仍3个`Handle is not initialized`：两次SwapGCHandleForType经GD.Load/Instantiate到`BasinSupplyRuntimeTests.CreateSampleFixture:576`、`NewGameDuringHealingCannotSpendTheNewRun:360`；一次SetGodotObjectPtr→Dispose→Finalize。这是观察到的错误路径，不证明根因或与本次坐标改动的因果；不因exit0而忽略。
- Main仍在`canyon.v2.encounter.counterweight-lift`原预算内未清：pre_cleanup_frame899，玩家HP2/恢复未触发，Rust剩1HP，7次攻击均连接，距离41.217，能量原始记录.161。未削敌、加血、补资源、加跳或延长原窗口；这是自动主线验路仍未完成，不推断手动必定不可通关。

post四条警告仍严格对应seamless观察者故意抛错1、room-profile非法根1、flow-registry拒绝2，原精确scope收据齐；没有通用warning放行。Explore保存`storage/task10-route-result.json`，实际正常计划与远程实例收据、Boss/有支撑出口成功标记齐；provenance仍human=false/normal_display=false，不借此代签正常速度或真人质量。

## 保存清单与当前绑定

- 最终17案源码清单同一SHA256：`5C5868F02AB7C9A9B7B66CCDFA7808F7CDCFB1D5F73FFD9BEF136DDC941F4572`；二进制清单同一SHA256：`2FB5A8F9D8AD3EC5C8AB71D30A191FC289C343850BAFA3A0A92F1C0D4789634A`。
- 根代理独立重哈当前1885个源码/运行资源文件、18个去重helper、6个二进制，mismatch=0。当前Knight.dll SHA256：`4F6B0C160F7E4C4FD03B206B5C573EAAC9E416CE35A938C9333B58EEF913AF33`。独立只读分批核实17案85/85保存文本对相等（source/user/helpers/continuation before↔after及fresh-binaries↔binaries-after），35个保存owned身份的argv/合同/终态wait一致；source1885/user109/bin6/continuation1。不宣称逐字节重哈全部封存载荷。
- 当前CIM未见本轮KNIGHT native/build/outer残留；Git分支仍`codex/task6-certified-final-20260902T0555`，HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，index SHA256 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变。
- 2026-11-03 13:45:17UTC流程内媒体检查7,427,750bytes/86文件，Notify=false/NewThresholds=0；无新增过程媒体，不建立自动化。G盘当时核对仍有约132.6GB可用，不清理用户资料。

## 下一步与未完成范围

本轮让真实生产接续更符合已记录反馈，但不是整个动作设计完成。下一动作/力学入口是让实际普通制动/过零事实进入表现，再制作高速减速、承重换脚与转向的连续样段；不能仅在停稳后播放通用Brake，或用加大滑行冒充重量感。具体幅度/节奏仍要后续正常速度验证。

Explore证据的进一步边界：从标题开始、同一persistentPlayer、23 routeNodes/11 challengeKinds、10 encounterDefeats/4 mechanisms/双向捷径2/检查点2，physics17975 Boss Defeated/HP0、18140有支撑越过出口/Completed，完成计数和信号各1、恢复0/0。OS写盘次数标为unavailable，不能把完成信号数误称写盘一次；artifact仍`operationCoverageComplete=false/fullGoalAccepted=false`。这些局部覆盖不代签全部敌人/机关/收集与原十一项。

Task1/完整十一项仍ACTIVE或in_progress；资源/Handle/FATAL、Main899和完整双路、R-I/R-C/R-P、正式素材动作/声音/UI/正常速度与硬件真人都保持OPEN。保留一项Minor及上述全部裁决，无二审。无GUI/系统输入/付费/新媒体/自动化/真实C存档/额外dump/引擎更换/Git写入或删除。
