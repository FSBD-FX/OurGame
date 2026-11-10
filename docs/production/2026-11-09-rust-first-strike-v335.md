# v335 · 首次近战起手的实际可触及边界

状态：本轮同版回归与审计结束，**未严格验收通过**。当前16指定阶段10PASS/6FAIL；其中原十一测试集合8PASS/3FAIL。首次完整周期的几何原因已实证，已实施最小起手资格修正；修复后五个原生案例完成，第六个前发生原生终结器崩溃。原完整十一项制作目标仍ACTIVE，Task1未完成，不以组件或代码审查替代交付。

## 事实、推理与边界

原保存攻击矩形26×18、偏移(朝向×34,-7)，Player胶囊半径20/高72；原模型水平70以内即可起手。六个真实首周期反例中，常驻和旧波次左右四个远端静止目标在全部有效帧留下约2.33世界单位间隙、无Connected、HP保持5；近处目标可真实受伤，实际D退避能安全躲开。原因不是必须扩大武器或让前摇追踪，而是停止接近的资格早于本案真实形状的外侧接触。

adapter现读取保存攻击形状和规范Hurtbox边界，以共用ThrustOrigin投影起手方向，只收紧外侧水平接近资格；模型仅Idle/Chase/Recover的新前摇使用该资格。原70上限/360感知/95速度/.65前摇/.14有效/.65收招/1伤害、实际伤害形、LOS/领地/睡眠/骨骸/幽灵权限及已承诺攻击保持不变。没有新增查询形状、Dispose借用资源、物理调参或美术资产。

这不是全二维重叠或必中判定；垂直和极近距离的原有落空不顺带改写，规范形状缺失的自定义目标仍沿用原range-only兼容，存在但边界无效时拒绝起手。实际受伤后才允许正控Player因真实击退短暂离地，未伤/负控仍须保持原接地门。入树后无pose/velocity/HP/AI写入，也没有直接TryConnect。新起手不延伸到原70外，真实逃离仍可躲避。

共享当前峡谷影响为7波次槽＋16常驻槽（rock3/scrap4/bone5/lost-echo4），不是全76、同时存活数或全23自然实战通过。四个resident叶为扁平保存，不通过修改基础场景扩伤害形。

## 原件与严格结果

证据根：`G:/knight/验证证据/october13-stable-claw/`。

| 案例ID后缀 | 内容 | 真实结果 |
| --- | --- | --- |
| 032400-518e3f88 | 原首次六周期 | 近处命中/D退避成立；远端四项功能RED，另7ShapeRID，严格FAIL |
| 032621-b43544ba | 起手资格忽略stub | 精确RUST_REACH_INITIATION_RED，483ms，入fixture前失败 |
| 032853-fd326fa9 | 最小修正后的六案例 | close-hit/D退避/resident左右/wave右五项完整；清理FATAL后wave左未运行，严格FAIL |
| 033404-e75dafc7 | models | 严格PASS，489ms |
| 033501-bc92c7a0 | 旧巡逻/LOS | 严格PASS，45411ms；7模型/15原生 |
| 033654-5b8b5fb4 | BoneWanderer | 7功能0失败/native0，7808ms；退出7ShapeRID，严格FAIL |
| 033832-1a2e42a7 | Ghost | 3功能0失败/native0，6414ms；退出4ShapeRID，严格FAIL |
| 033938-71df7c6f | stationary-guard | 严格PASS，20983ms；真实A绕背和三接触控制、J反击 |

前三案独立原件审计699快照对0差异/18份实际binary复制匹配，跨案不同源/产物不混合。修复后冻结源1927行清单SHA`461760F7A1846295CB68A140C9B8CDCB4E14A68BBC7347C4BC289876BBC6D470`、六产物清单SHA`8081D05FB0FC9381590A7489850E228DEF3FE6F2976B88A3964F539B509F84B9`。原生PID13564退出-1073741795/19532ms，无超时；gchandle.is_released FATAL及两条Handle错误不能据堆栈直接证明具体资源对象或原因。五项成功不是六项GREEN，也不冲销旧6RID/历史崩溃。

最终同版原十一测试集合如下（不是原十一项制作完成数）：

| 阶段 | native耗时ms | 严格结果 |
| --- | --- | --- |
| models | 489 | PASS |
| Explore | 211370 | FAIL，捷径入口HP1前提不符实际HP2 |
| rig | 38368 | PASS |
| presentation | 18452 | PASS |
| gait | 29874 | PASS |
| recovery | 15055 | PASS |
| actions | 10803 | PASS |
| save | 11735 | PASS，另独立cold1494 |
| Main | 96150 | FAIL，原899帧底部Bell未清场 |
| pre | 142153 | FAIL，44项全返回但10条Handle ERROR |
| post | 190158 | PASS，75项/原四限定负例WARNING |

全部原cap、日志门保持，无超时；save独立cold的writer38400/reader3268、6只读witnesses、straight/Arc各4坏输入与精确恢复齐全。post10suites、75executed/classified/retired0、ERROR0，stderr23551字节仅原四限定WARNING（观察者故障1、根节点类型1、拒绝Completed→Recovering2）。pre虽native0和44返回，10条Handle错误仍严格FAIL，首条经BasinSupply.OpenSample→AddChild；不是资源原因已确定或被修复。

Explore捷径首次上行入口硬性要求捕获hp==1，实际failure-state是hp2/2、同Player/Playing/恢复0/0/dash0、尚未开始跳跃或K。本案缺最终route JSON，不能称Boss/出口完成，也不能未经因果核对称为游戏退化。只读确认该helper入口已捕获实际HP，后续没有需要恰好1的伤害预算/动作分支；邻段使用正入口HP并全段守恒。历史最差HP1样本被固化是推断而非已证设计意图。下一独立增量先以HP1/HP2正控和无生命/伤害/治疗/同拍伤后治反例校正自然消费者；仍须同入口HP、身份、无恢复、一跳一cast、原16/300预算及原地形。不能用每帧重采baseline或强迫玩家受伤来凑1，本次失败不重判。

Main035013-f1c56d27在原899帧内gear-pistons底部Bell仍HP4（保存初始6，实际已有两次HP损失）、PlayerHP1、能量.095，7攻击/3爪/5连接/4伤害事件；缺最终JSON，不签主线/Boss/出口。只读对照发现同底部handoff原Player位置/HP4/能量1保持，但首个常驻选择的位置和真实输入时机不同；当前frame58已有实际受伤，不是从一开始就无能量。两次Bell K均实际成功且进入Exposed，不能误记成抓取失败或没有弱点。下一按多敌威胁/站位/能量的真实顺序核对，不猜成Rust新资格直接导致，不提高899限额、补能量或改怪物血量。原完整路线质量仍未具备当前版资格。

最近完整十一测试严格PASS仍绑定旧v334，不能自动续签到本版程序集。当前16案独立审计合计4089快照对0差异/96份实际binary复制匹配；两个先前不同源RED另计，不混入当前16案。六个严格失败分别为新melee崩溃、Bone7RID、Ghost4RID、Explore入口、Main战斗、pre10Handle，全部保留、不重复同源筛绿。

## 审查、存档与待办

一次fresh全增量审查C0/I0/M1，无C/I补修、无第二审；六CS＋一个PS的精确pin及全部不作判断的边界保存在[review package](../../.superpowers/sdd/2026-11-09-rust-first-strike/review-package.md)。M335-01：新阶段缺被直接调用的unit源文本副本，完整source清单仍保护其哈希，非完整性失败；后续维护补必需输入，不为此重跑。其他旧Minor不关闭。

最终当前身份核对于19:59:49.9772933Z通过：重新枚举1927源/六产物与全部16案after一致、16唯一helper/7review pins、109保护行内104实际文件/BDDA一致，父场景D4AE/BasinA038保持，KNIGHT native0，G空闲76,138,639,360字节。收据[identity audit](../../.superpowers/sdd/2026-11-09-rust-first-strike/current-identity-audit-v001.json)明确`identityChecksPassed=true/allSixteenStrictPass=false/wholeGoalAccepted=false`；身份匹配不是全部测试通过。审查七pin后续只读取哈希确认仍一致，没有第二语义审查。

当前整个修补未具备严格完成资格：第六个新案缺证、原生资源错误与当前两条完整路线失败未解。正式角色/敌族素材、连续优美动作、正常速度声画/性能/硬件真人、全76覆盖与原十一项继续OPEN。用户已批准的联合落脚取证按v334保留，没有造中间帧或更改原.15世界单位保护。Task1仍inprogress，不新建/暂停/完成目标。

仅无窗口构建/测试和文档；无新图片/视频、付费、系统输入/前台、引擎替换、额外崩溃采集、GC/cache/资源身份模式实验、定时自动化、Git写入、删除或真实档实验。near-final唯一媒体Check于20:00:13.0091870Z：18,882,711字节/182文件、增长18,879,203、Notify=false/档位0，未达20GiB；不新增自动化或打包删除，提醒门永远不停止工作。
