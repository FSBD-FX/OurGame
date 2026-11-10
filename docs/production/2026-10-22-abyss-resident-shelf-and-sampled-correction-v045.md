# 深渊侧棚、实际幽灵巡逻与惯性纠偏 v045

2026-10-22，Asia/Shanghai。权威工程 **G:/knight/Knight**，总目录 **G:/knight**。本轮为实现进展，不是新发行、完整首关或视觉验收。完整1–11目标ACTIVE；不使用目标工具文字中的历史E路径。前一存储确认轮对玩法为NO_PROGRESS，本轮有实际生产地形改动和原生RED→GREEN。

## 事实：本轮制作了什么

唯一生产布局变化是 `CanyonWorldLayout.AbyssSideExits[1]` 从 `(1920,2220,200,40)` 扩为 **`(1920,2220,216,40)`**。沿现有 `CanyonContentBuilder.Solid` 保存真实碰撞、同尺寸skin和minimap来源，不用背景画出假支撑。左/右制动墙、另两出口和安全底面不动；新棚右缘2136，40px玩家中心2160时左缘2140，仍有4px净空。

新增独立 `resident-abyss-alcove` 原生门：仅保存一份明确占位的lost-echo真实子型叶，真实五扇区/完整环境保护区/原36条主或备用出生记录；不重跑24份叶身份字段测试冒充新内容。原 `resident-frozen-alcoves` 的全50/24合同保留并另行运行，没有缩窄原门。

最终原生结果：

- 真实36×64幽灵在home `(2040,2188)`、端点1976/2104完成360帧巡逻：X范围1977.5962–2102.4038，最大单步0.8000488，持续有地面、返回原点、HP3，无回血/硬改位置。
- 同一个保存Player从主井初始夹具位置 `(2160,2140)` 仅用A/D落在具名AbyssExit2，再回主井继续下降。最终落点 `(2081.418,2183.9329)`、速度0、HP5，38个实际键盘/物理采样；入树后位置/速度写入0、jump0、dash0、healed0。
- 新棚与真实Player胶囊的主井中心通行，两向原生查询通过。完整132候选地形、小地图保存、同源父场景内嵌快照和移动后重载通过。
- 全50联合活动检查从43增至**44候选／6未解**，1619走廊查询、20228联合节点、20893条JSONL。深渊lost-echo联合候选home为 `(2042,2188)`，仍完整±64；与上述2040实际巡逻夹具是两个明确不同点，不混称同一点完整认证。

边界：生产生成器已改，但**正式五个canonical场景仍127地形、未覆盖**；132是含此前Frozen改造的隔离候选，不是新增132块。正式新增仍0/50、全50 Placement=null、24正式子型资源仍Unimplemented。1份实际Ghost占位叶不等于正式原创美术。44是完整身体/支撑/活动包围候选，非44只已入正式关卡，也不是全部攻击/追击/返航认证。

## RED→GREEN与判断

所有案例均保留在 `G:/knight/验证证据/october13-stable-claw/`：

1. `resident-abyss-alcove-20261022-001728-414-b6da838b9110405f9df8051050701513`：真正行为RED，native1/1201ms、guards0。home2040及左端1976通过，右端2104为`incomplete_support`；正反完整走廊分别失败end/start支持。不是编译、入口或夹具失败。
2. 仅扩16px后 `resident-abyss-alcove-20261022-001841-502-0c110d075f724894aee1cd6f2a39d544` strictGREEN，native8186ms。后续实际Player版 `002318-903-756b0204338f4778998b4fd6969572e8` strictGREEN，native8776ms；最终同冻版另列下表。
3. 第一同冻九门为8PASS/1FAIL：既有Frozen玩家路线 `resident-frozen-player-20261022-002750-038-464720f3c1c546bdb1c4d0c52ca6d68b` 在向上抓取阶段Obstructed、随后局部越界，native1/3818ms。其boost纠偏阶段 **0/50帧** 就撤销D，仍vx−63.7566。它没有消费新D采样，不能拿“x已到1872”代表反向制动完成。
4. 只把新棚恢复旧宽200作对照：`resident-frozen-player-20261022-003620-300-e10b240531e240f5a933c71dee98eb0c` 仍0/50、vx−50、Obstructed后越界，native1/3396ms。失败不以新16px棚为必要条件；不是所有浮点轨迹完全相同的证明。随后已恢复216。
5. 只加强自动驾驶阶段谓词：原x条件＋真实`CurrentFrame.MoveAxis>0.5`＋实际vx≥0，并要求至少一采样；保留原50帧、上拉180、回返120及全部最终HP/跳跃/两次真实到达断言。不改游戏Motor/Claw力、碰撞、通道或能力。`resident-frozen-player-20261022-003813-099-13b82b24ad9b41d0b1c282195bafdcd4` strictGREEN，native5416ms；实际D采样1、vx25.000002、上拉37/180、HP5→5。最终同冻复验再次通过。

推理边界：日志没有直接输出Obstructed四分支中究竟哪一支或准确碰撞shape；棚底/右下角与身体足迹相交是强推理，不写成已取到原生碰撞所有者。确认的是零步撤键、负惯性、旧尺寸对照仍失败，以及实际消费反刹后的同路线成功。

Ruling：不用历史未执行240px建议；它延至2160、侵入玩家中心通道。216满足本轮完整支撑与两向玩家净空。若后续真实战斗/返航失败仍须重构，不能用本轮候选作豁免。第二项ruling是迁移过时自动驾驶消费者，而不是修改游戏力学；未增加上限或减少最终要求。无延期minor、无wholeTask-done或whole-plan final review。

## 最终同冻十门

下面均为**最终冻结**，不拼接上面的中间九门、旧尺寸对照或局部GREEN。全部build exit0、普通七参数Rebuild、0警告/错误；native均exit0、无超时、guards0。批脚本aggregate exit0只是收集完毕，不是全通过。

| case（均在上述证据根） | build ms | native ms | 严格结果 |
| --- | ---: | ---: | --- |
| resident-abyss-alcove-20261022-003943-395-e5307864d17c493d991ebb574989616a | 22196 | 8685 | PASS |
| resident-frozen-alcoves-20261022-004019-007-71b2c2fd42104e64a942962fa650b88c | 25532 | 29576 | PASS |
| resident-frozen-player-20261022-004117-456-0605ad4acddb4cfb992e412f28d528b2 | 21652 | 5328 | PASS |
| resident-parent-20261022-004147-732-9b7b6584e3624090bf9aad050f22520d | 21356 | 2155 | PASS |
| resident-allocation-20261022-004214-407-25bb212e0abd4390afe11b20e4eb8999 | 21393 | 479 | PASS |
| resident-void-stream-20261022-004239-235-7e97538ce11a44eaa8717939090e7fc8 | 22606 | 1324 | **FAIL** |
| keyboard-20261022-004306-447-15e99f7bd69440089984ca16b9eddd25 | 21866 | 9584 | PASS |
| actions-20261022-004341-176-21674bf255f84a9ba0767fb216d3cae8 | 26639 | 6993 | PASS |
| prologue-pre-20261022-004417-977-c8d29e5e5d1f44f3b9ffc96dc30126af | 22489 | 99775 | PASS |
| prologue-post-20261022-004623-661-a5f593a9740943adb00cc610fb0d2c56 | 24946 | 147562 | PASS |

严格**9PASS/1FAIL**：void stdout第20行一条`187 RID allocations of type 'P12GodotShape2D' were leaked at exit.`，stderr/external均无此诊断。是187个分配的一条错误，不是187次错误或镜像双计；`STABLE_CLAW_OK`不覆盖它。根因/资源所有者未证实。post四条授权WARNING完整镜像，独立四条而非八条，三个scope为1/1/2；75原案例执行/0退休、8次同owner实例化节点1→1。其余无ERROR/WARNING，本轮未见Handle/FATAL；不据此关闭历史Handle、FATAL、154/31RID或音频失败。

50/50前后custody对一致，root与独立只读审计一致：

- Source1702／C#872：`9176AE70E620175D356D04EF5AF576B7884B931E9D3091BFDE01E17985F555A3`
- Binary6：`1865AE50BBDFA4229DAB5CE07D2D58775DDE308556B03DB79DA50F1D0B15C041`
- common helpers9：`9D3361C94C8289D7DBDB17821A72A0DD3CFE8CEA46587E164D3DE0C48861EC92`
- pre helpers10：`173EAD34008DC4EC56935F37BEE910346E1CA8A5AE4D6229364BAE421184BA19`，只多既有clearance帮助器。
- 真实存档保护109项不变；continuation仅2字节CRLF空清单，不能计作1个存档文件。
- 全批终态后root逐项重哈希现行1702文件：0漂移。HEAD仍`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，index SHA仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。

## 剩余六处与下一步

统一ID前缀`canyon.v2.resident.`：

| ID后缀 | 子型／角色 | 本轮联合结果 |
| --- | --- | --- |
| archive-side-cave.03 | bone-charger／冲锋 | candidate_set_unselected |
| archive-side-cave.04 | bone-diver／俯冲 | candidate_set_unselected |
| gear-pistons.06 | scrap-heavy／重型 | joint_budget |
| gear-pistons.07 | precision-heavy／重型 | joint_budget |
| steam-valves.02 | scrap-wanderer／游荡 | candidate_set_unselected |
| pendulum-gallery.04 | precision-laser／固定远程 | candidate_set_unselected |

全50、24子型、骨骸10／幽灵10和原26+新50+1Boss不减。Gear是区域共享20000联合预算停止，不等于无解；不提升预算来代替布局。

已取得下一步实际数据，而非无执行计划：Archive charger有3个完整往返候选，diver仅1个，均与现有两wanderer的联合活动包络重叠；只加几px宽度不能解决。只读提议的y1510低棚在旧Shelf底1460之下只有50px头部净空，不能直接采用为完整Player可进入空间。下一须同时原生验证玩家与22pxcharger，不为怪物造不可进入的夹缝。

Gear两heavy的16个接受候选全挤在y4074。下一可先测试左右不同高度的两支撑层，再核全机关扫掠、旧Heavy84px头高、完整玩家路线及GearPistonOne右端2652→配重台左端2700的48px落回道；具体候选坐标仍未认证，不直接写成正式Placement。

## 风险、留档与完整目标

本轮两段Player与三个Ghost实际运动均为隔离候选、机制停止回调，不是敌人与玩家共存接战、完整Claw路线、正常Main/Explore或真人验收。正式原26与五canonical原件保留；还需六缺口、全50攻击/追击/返航/玩家/机关相容、全部角色状态与正式资产、实际运行接入、正常双路、最终UI/视听/60FPS/真人/WindowsZIP/授权GitHub归档。完整1–11继续ACTIVE，不把本轮9PASS当完成。

没有新付费生成、窗口/前台操作、Git写入、删除、自动化或GC/cache/KeepAlive/borrowed Dispose/drain绕过。没有新展示图稿或视频，普通原生调试不频繁采集原帧。正常汇报前手动容量检查：4838字节／基线3508／增长1330，Notify=false；没有重置基线或创建后台。后续关键媒体仍仅存G:/knight/过程图稿与视频；不恢复已删除旧归档项目。
