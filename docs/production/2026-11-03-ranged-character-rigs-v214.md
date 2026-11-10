# v214 · 四类远程完整程序身形、关节动作及换图接口

工程仅G:/knight/Knight；原十一项、V05及正式动作/美术验收仍ACTIVE/OPEN。状态：生产承载和一次审查修补已落地，最终同源16阶段14PASS/2FAIL，**不是正式交付**。主角R-I/R-C/R-P保持独立未验收，不以敌人改进抵消。

## 事实、判断与边界

- 实施前四原保存敌人只是同身体配不同枪。当前五生产CS（含一个新纯值CS）给出四套身体/头轮廓、不同站姿和刚性四肢；只有敌人表现合同变化，实际碰撞/HP/Muzzle/攻击/地图/输入/存档未调。
- 散弹宽肩锅炉、连发窄身弹链、导弹背筒低重心、激光细身棱镜是开发选择；属于可替换程序样段，不能当用户已批准身份图或最终族系美术。没有生成新PNG、付费、购买Spine或打开视觉看板。
- 真实攻击帧驱动brace/fire/recovery，臂18+18、腿13+13保持段长，脚底localY34；后坐来自唯一真实发射区间与按时间积分的临界阻尼弹簧，不靠整图缩放或重复发表泵动。零/非法delta不推进骨骼。
- 八借用Texture2D槽和IRangedWeaponVisual2D把AI同默认绘制解耦；检查点明确Reset，撤爪/死亡/退树Clear；普通Idle不偷清攻击身份。未来完整身体替换不需重写AI，不承诺运行中热换节点。
- 世界aim转换成local绘制方向，不变更权威枪口/危险几何。当前几何观察不是alpha/像素、正常速度艺术、控制手感或硬件FPS证明。

方案、接口和实际画布见[设计](../superpowers/specs/2026-11-03-ranged-character-rigs-design.md)、[实施计划](../superpowers/plans/2026-11-03-ranged-character-rigs.md)、[八槽接图说明](../art/2026-11-03-ranged-body-art-slots-v001.md)。精确六文件旧基线、新增纯值rig、唯一审查包与裁决在`.superpowers/sdd/2026-11-03-ranged-character-rigs/`；不使用整个dirty HEAD diff冒认为本次变化，没有提交/分支/索引/外发修改或删除。

## 测试先行与唯一审查

所有原案在`G:/knight/验证证据/october13-stable-claw/`，以下短号为目录GUID前缀，不覆盖旧失败。

| 过程 | case | 严格结果 |
| --- | --- | --- |
| 缺完整rig | 745eec59 | 真实具名RED，Source1873 |
| 模型/轮廓/关节/八槽/旧消费 | 1c5f9fed | 中间GREEN，尚无新替换生命周期 |
| 缺独立替换接口 | ab584e48 | 真实具名RED |
| 配置可达8发而只按3发处理 | 442fa954 | 真实具名RED；改表现边界至现有配置8，不改游戏发射数 |
| 真实4保存身体/暂停/后坐/撤爪/死亡/检查点/独立替换/旋转aim | 361ee956 | 中间严格GREEN，native46529ms |
| 唯一审查的回放/过顶翻面 | 7ae18d9f | I1 12个回放反例、I2 24个过顶过渡全RED |
| 一次C/I修补后的完整表现专项 | 016e5e0b | 严格GREEN，native46452ms，四种精确收据齐全 |

唯一fresh whole-change审查C0/I2/M3。I1同AttackId允许旧phase/progress恢复charge/heat；改为阶段/进度单调接管，并同真实发射区间去重分离，Recovery尾弹保留。I2原cos正负号瞬间切躯干和IK肘分支，过顶产生大幅跳位；改为连续水平投影承重、固定解剖肘分支，避免插值bend到零导致肢体缩短。四角色/正负垂直边界/brace-fire-death24组、定长及尾弹在修补后通过。没有第二审。

这些是当前生产几何与真实原生事件的证据，不是“已经像精美Spine动画”。旧3player/4ranged预修补通过保留为历史，不能代签修补后版本。

## 当前最终冻结回归

SourceRows1874（仅新增RangedCharacterRig.cs），build180s；ranged-presentation/actor-saved120s、其他原专项55s、Main/Explore600s、pre180s、post300s均保持。无ExpectedFailure、固定FPS、电影写帧、额外GC/运行时升级或静音规则。最后15阶段已串行终结，下表保留实际结果；任何失败都保持Task1/完整目标未完成。

Main9347323d严格FAIL139223ms/exit1，无timeout/guards[]。首错仍counterweight-lift原899，HP2/Rust1/J7/gap41.217，但本次精力0.161与v213的16.133251样本不同，必须保留差异，不认定表现改动造成或修复了该差异。完整攻击/防守/机械爪驱动仍未清场，原时限/血量/三弹/单跳/资源门未豁免。实际存档保护109条未改变；不是完整关卡验收。

Explore9905702e严格PASS333288ms，结果文件确认title起步、同Player47546631684、Boss/arena/completion各1、实体支撑出口、无恢复，D持键60后位置/存档SHA不变；不推断OS写次数或正常显示/真人/性能。pre67a2bdac严格TIMEOUT180064ms/exit-1，原180000ms门未变，stderr空；最后仍执行UpperPendulum通路后进入LowerBuriedMachinery，非停在旧终结器标记，尚不能判定超时因果。没有重跑筛绿或扩限；已只读比对共享阶段耗时，资源历史fatal继续OPEN。

## 最终同源结果（全部终态后）

下表均为016e5e0b修补后同一生产源/二进制，各案原始失败完整保留。单位ms；keyboard另有7905ms的独立headless标题合同，不是正常显示。完整当轮为**16阶段14PASS/2FAIL**，不是原十一项已完成14项。

| 阶段 | build | native | 严格结果 | case短号 |
| --- | ---: | ---: | --- | --- |
| 四远程完整表现专项 | 25948 | 46452 | PASS | 016e5e0b |
| ranged-models | 26258 | 509 | PASS | 862a6b0c |
| ranged-geometry | 30841 | 660 | PASS | 1f6a16dd |
| ranged-actor-saved | 32930 | 58483 | PASS | 2d80159b |
| ranged-protection | 28935 | 488 | PASS | c98170dd |
| ground-gait/body | 23599 | 3734 | PASS | d1035c09 |
| presentation-rig | 24927 | 2365 | PASS | c4af192e |
| rig-compatibility | 27398 | 1062 | PASS，formal11/mantle2子集 | 67dc2731 |
| ranged-mixed-policy | 28758 | 3752 | PASS，仅策略/隔离接线 | a9fa31f8 |
| actions | 31251 | 9494 | PASS | 264801a1 |
| keyboard | 32549 | 20159 | PASS，另title7905 | 76544b3f |
| models | 31373 | 512 | PASS，仅既有爪子模型子集 | 8ff7ce3e |
| Main diagnostic | 32235 | 139223 | FAIL，配重混战原899 | 9347323d |
| Explore diagnostic | 31243 | 333288 | PASS，Boss/实体出口 | 9905702e |
| prologue-pre | 33483 | 180064 | TIMEOUT，原180000上限 | 67a2bdac |
| prologue-post | 31774 | 210669 | PASS，沿用既定300000上限 | a6ee3825 |

post为原75有序案例/owner8及nodes1→1，只有原4条限定负控WARNING（observer1/profile1/flow2），无ERROR/FATAL；300秒是此前已存在的门，本轮未扩限，也不声称在原180秒内完成。本次后段通过不能关闭历史资源finalizer/Handle故障。全部build0警告/错误，所有阶段guardFailures=[]，仅pre有真实timeout，无ExpectedFailure。

Task1与完整原十一项继续**INCOMPLETE/ACTIVE**，没有Git提交/发布、关闭目标、删workspace或用局部绿色结果替代当前Main/pre失败。下一应先给pre前缀顶层fixture做有界进入/返回时间与physics/process观察，保持功能/等待/原cap，定位慢25.391秒的来源；同时保留未获答的Main地面跳战术边界，其他正式动作/惯性/美术继续待做。

### pre耗时的只读对照（不是根因结论）

旧对照是v213 `prologue-pre-20261103-050111-750-1d0a3a63df8d431c81fd6fff944c91ab`。同Godot/phase/cap、RunPrologue及相关Traversal测试哈希相同，但生产远程6个CS实体/1873→1874不同，不能称相同binary控制。

| 同名engine_ms里程碑 | v213 | v214 | 累计差 |
| --- | ---: | ---: | ---: |
| PRE Traversal进入 | 139782 | 165173 | +25391 |
| Corridor教学台进入sample.2 | 143923 | 169323 | +25400 |
| UpperPendulum下摆进入 | 147660 | 173063 | +25403 |
| Upper高处最后sample返回 | 154274 | 179678 | +25404 |
| Lower entry_to_arena sample.2进入 | 154283 | 179685 | +25402 |

共同段耗时旧14501/新14512，仅多11ms；33条观察顺序匹配、16个已完成sample结果文本相同。旧版该点后仍有12sample/4段、Traversal返回还需9442ms，新版仅余315ms，不能解释成“只差成功标记”。更早前缀缺累计时间戳，尚不能把25391ms归因渲染、死锁或具体fixture；Main慢10211ms而Explore快2293ms，也不支持简单统一变慢断言。本次只有只读比较，没有启动新验证/更换旧binary/额外采集。

## 延期与验收缺口（仍开放）

本次Minor只记账不在审查后顺手实现：M1宽脚贴图居中适配可离地约1px，需脚底对齐/专测；M2极小非零det父变换的aim回退；M3逐件重复IK/轮廓分配和无条件redraw的性能优化（未测得硬件失败）。历史v212/v213的极端有限步态、timed兼容cue及跨速率/重入等延期保持原状态。

pre超时已是实际整体时限失败，不能据M3初审为Minor就宣称没有性能风险；当前只读证据尚未把25.391秒归因到渲染开销，下一仍要测量，不在没有因果证据时扩大cap或随意改渲染/等待。

同代旧帧、跨检查点异步帧、全部槽alpha/左右遮挡、枪口视觉对应、实际正常速度可读性和手感、硬件性能等边界按ledger裁决，不扩大现有测试证明范围。原主线899、历史原生资源fatal及正式美术/动作/全图/硬件/玩家验收不会被专项GREEN关闭。主线第二次普通地面跳询问未获答，不执行新战术。

## 留档与权限

### 最终保管核验

06:42:23+08独立核验当前1874源无缺失/额外项/SHA变化，10个post helpers和6份二进制与最后封存案相同；59个保存creation/path身份均已终结，3个复用PID不是旧owned，KNIGHT Godot/dotnet为0。全29案23PASS/4真实RED/1MainFAIL/1preTIMEOUT，145清单对/14352选中源实体/330helper实体/174bin逐SHA相符；不重哈旧案筛绿。

| 最终对象 | SHA256 |
| --- | --- |
| sourceManifest | 68A990AEF1F6EEB450CE445A72F0886745E6A02F5C4C665A9B57D9E62E3F2980 |
| post helpersManifest | 5E8E3022B6FF8418487D11984D776C68D37D48457E1A8D844265D8C1C1B19ACA |
| binManifest | 5CD7615EE6785B40AF33720154E6B8F805C4A881B2080EA33D1633C717A06CB8 |
| Knight.dll | 5149BA2DA1931FB7F895B5BDDBBD2312598E60F85680491657B757D85238529D |
| RangedCharacterRig.cs | 6621C20D4807A7037960564CDE3C3B3BE8A65AD8F98B8DBEC2F24843F3A09A50 |
| canonical | C163A67F9E95A5FFB2A415412C7AE164C73B2A77A76002D0397C94ED8DC5DDE8 |

HEAD e7c70498/现分支/索引21610FDC均未变；保留全部未提交用户改动及本轮精确基线。目标工具仍ACTIVE，原范围和正式验收未缩减。

本轮没有过程图/录像，只保留必要源码、测试原始记录及少量文档；未付费/显示窗口/使用OS键鼠/额外崩溃采集/自动化/删除/真实存档或发布。正常汇报前运行本项目媒体监测，新增20GiB仅顺带提示，不停工，不恢复旧UE归档目录。
