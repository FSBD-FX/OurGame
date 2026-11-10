# v217 · 真实落地冲击驱动身体吸震（当前技术回归通过）

仅G:/knight/Knight；原十一项目标ACTIVE。落实U-20261103-01中的一段实际动作/物理表现，不是完整惯性、角色动作语言、正式美术或试玩交付完成。只在已有力学输出上增加表现，不改Motor、重力、推力、碰撞、HP/伤害、键位、地图、资源所有权或存档。

## 事实、推理、风险

事实：此前已有Rise/Apex/Fall及轻/重落地模板，不能称没有动画。实际缺口是Timeline不读取Motor LandingCount/LastLandingImpact，而短喷Dash优先会消费落地边沿。本轮保存Player、真实终端下落、正常内部键盘W/Shift晚喷，实测仍燃烧时以717.5相对法向速度落地、count1/HP4/状态Dash，随后明确缺少独立承重通道的断言RED。

推理：落地冲击必须在接触时独立作用于身体与支撑腿，不能等短喷结束才补播落地、直接结束推进或用整图缩放冒充。新值模型将实际owner/count/reliable speed转为有界短时承重，普通Dash/攻防状态仍保持权威。

风险：4px/650及.18–.28秒仅为程序样段映射，不是获批最终风格或现实单位标定。真实视觉、不同地形边缘/斜坡/移动支撑连续性、角色特色与操控仍需要实际正常速度验收；本轮没有打开窗口，也没有录制或付费生成。

## 当前生产实现

- 新增无原生引用的PlayerLandingLoadModel：新owner/历史计数只建立基线；正时间消费每个新count一次；无效/不可靠/离地/死亡恢复隔离旧事件；同count不能站立重播，零时间不推进或消费正常新冲击。大有限速度先double计算并限幅。
- Coordinator读取真实Motor身份、计数与落地相对法向测量。Timeline输出独立LandingLoad，不改变实际物理状态；旧调用者可不提供该字段，保留原模板。
- Formal在动作混合后对十个sibling槽作共同位移，髋下沉、刚性18+18腿继续求解实测世界脚底；不移动角色根节点/碰撞体，不拉伸图片。有效测量激活时替代原固定垂直落地偏移，避免双重下沉。
- 新原生绘制检查发现普通步态复入80ms淡入使首帧权重仅.20833334；保留绘制端完整支撑保护，在GroundGait本身增加仅限真实承重的立即落脚。仍经过原合法支撑/单物理帧门，普通未测量复入保留淡入，无效/复合地形门不放宽。
- 新Model/Support/Draw收据要求唯一完整行；对实际宿主AST执行1正例7拒绝控制。合成宿主日志控制明确不是原生或玩家通过证据。

## 测试先行与失败保留

原案全部在G:/knight/验证证据/october13-stable-claw，不合并旧title/v215证据。逐轮事实与裁决见[实施ledger](../../.superpowers/sdd/2026-11-03-measured-landing-load/progress.md)。

| 证据 | 结果与含义 |
| --- | --- |
| 3a5d02ca | actual717.5/Burning/count1后缺channel，真实RED5353ms |
| 462f65c4→2a58d6c0 | 缺值模型RED652→36模型/原步态GREEN3868 |
| 9dcc6e2a | 测试位置参数写错导致编译失败，无engine；未假算行为RED，原binary guard保留 |
| 72db7c3a | 正式Dash应下沉3、实际0，真实绘制RED616 |
| 5d2d7efd | 测试fixture旧动作收势未清，后段body4vs3；修正测试清理及左向镜像oracle，不改预期为4 |
| ea260cfb/07cbeb56 | 第一轮受控绘制与实际frame/站立通过，但未足以证明实际支撑绘制 |
| cf226f4c/24ef18d7 | 强化真实绘制失败；后者标量证明support/bounds/tick/root合法但weight.20833334，两脚stance=false |
| b8760cef | Timeline空中→可靠落地的完整支撑合同RED523，随后才改GroundGait |

## 当前同源五专项全通过（原55秒预算）

| 阶段／GUID | native ms | 实际证明 |
| --- | ---: | --- |
| ground-gait／34935673 | 3848 | 36模型、支撑复入/4拒绝、6组12脚底、原540绘制/569支撑及实际Motor链 |
| impact-player／c527edcd | 5800 | 实测717.5/Burning/count1/HP4；完整支撑weight1；移除仅LandingLoad的真实body绘制负控、根变换不变、实际刚性腿/世界脚底；站立不重复 |
| impact-negative／256d4061 | 14738 | 原晚/早喷非免伤、软/混合支撑/真实无敌帧/配置快照等负控 |
| impact-support／c1422d96 | 19269 | 原真实移动支撑/墙后地面/携带/初始穿透等合同 |
| presentation-rig／dd398bb5 | 2295 | 原燃烧/滑行/反刹、拒绝、动作/死亡/暂停优先，受控变换而非像素 |

SourceRows1875（仅新增一个纯C#模型），无ExpectedFailure、扩cap、fixed-FPS/movie或新增GC/原生采集。五案failure=null/guards=[]，stderr空。

## 唯一审查与延期

一次fresh全改动审查C0/I0/M2，比较九份精确旧基线及两个新代码文件，另独立运行纯宿主1正7负控制；无C/I修补、无二审。M1：宿主测试依赖一份绝对路径历史日志，丢失会失败关闭但影响可移植性；M2：新共同位移回归独立断言body/core/weapon/腿，尚未分别断言双臂/斗篷附件/灯/绳索，当前十槽循环未发现遗漏。两项延期，不顺手扩大审查修补。

计数历史的同payload、零时间安全清理、跳过中间计数不排队、允许跑动摆腿、长帧已过去则不播旧动作、采样不冒充硬件FPS等裁决及代价均保存在ledger。无新玩法/资源所有权变更；审查不能关闭历史fatal或完整序章要求。

## 保管与尚未完成

截至五专项：19尝试/18native，11PASS/6行为RED/1fixture失败/1编译失败；94完整清单对＋编译案缺失1binary对、9036选定source实体/190helper实体/108bin/37保存owned身份。所有已存在配对/实体SHA一致。14:10:07+08当前1875源、五phase helper并集10项、6bin匹配；新14owned身份已结束，Git HEAD/分支/索引不变。

## 当前完整普通回归：5PASS

同源/同helper冻结的串行51015已明确exit0，逐案核实结果，不从外层0推断中途通过。原build180s，所有case SourceRows1875、failure=null/guards=[]、无ExpectedFailure。

| 阶段／GUID | native ms／原cap | 实际资格 |
| --- | --- | --- |
| pre／1db4ae8a | 171075／180000 | 原44有序对＋计划/退出drain/phase，stderr空；不是性能或资源修复证书 |
| post／a84ba8aa | 204700／300000 | 原75有序案例＋8 owner/node1→1，原4条限域警告按原规则处理，无新豁免 |
| actions／4d7b1461 | 9522／55000 | 既有真实内部键盘动作合同 |
| keyboard／5b5efa17 | 20152／55000 | 原抓取/双次Rust/挂载标题合同；另独立title-entry7712／55000通过，无显示/系统输入资格 |
| models／b892115b | 509／55000 | 仅既有爪模型子集，不是全项目所有模型 |

加上五专项，当前要求的10个技术门全部通过，唯一审查C0/I0/M2；它完成本次承重增量的技术验收，不完成原十一项中的任何整项，也不证明正式精美度。第一次真实接触不再被80ms淡入挡掉，body/装备吸震与刚性脚底的实际链条已证；所有早期失败原件仍保留。

历史Main899/HP2/Rust1、v215 keyboard3HandleERROR、pre间歇超时/资源fatal、完整当前双路、R-I/R-C/R-P及正式美术/音频/UI/正常速度/硬件真人继续OPEN。此次普通keyboard安静通过不是定位或修复历史Handle故障；未重跑未改变的Main/Explore战术，不借旧通过冒充当前完整路线资格。无Git发布、删除、真实存档或自动化。正常媒体检查7427750bytes/86文件、Notify=false；没有新过程媒体。

最终独立保管累计24尝试（16PASS/6真实行为RED/1fixture失败/1编译失败），119完整清单对＋编译案缺失1binary对，11440选定source实体/248helper实体/138bin，所有已存在配对/捕获SHA一致；48保存owned身份为24build＋23主engine＋keyboard独立title。14:28:19+08当前1875源、ordinary helper并集13项＋focused checker共14项、6bin匹配，新增9个creation/path身份均结束、当前KNIGHT Godot/dotnet0。HEAD/分支/索引仍原值，无提交或发布。

当前sourceManifest `B78EDF18FB2534F91062D9A6A85AF2D2ABA32AC8D50F6BF2B48B9FCFC9641EEB`；binaryManifest `28C19C791ED4CA2FD85B9A184383B4BB9C1487F1C5E5B4E2A4C6C8ECAB188BFF`；DLL `6367BC90A5BC9795B96EA7E048E0A351A70F1F39573CA3D9DB8FD3FD80137526`；canonical `EE44C45DC2151C2706DEBE4F098B22143946845AA6B4F2C4FC2DCF3282E5E6DC`。本轮只手工更新文档/原有容量监测，不建立自动化或删除基线。
