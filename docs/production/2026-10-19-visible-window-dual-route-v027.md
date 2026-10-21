# 实际窗口启动与双路线复验 · v027

2026-10-19，Asia/Shanghai。仅启动边界和证据更新，生产游戏未改；完整1–11 ACTIVE、Task6/7及正式新敌人0/50等内容仍OPEN。前序[v026](2026-10-19-thruster-coplanar-top-and-temple-admission-v026.md)原件不回填。

## 事实、推理、风险

事实：用户说旧Main/Explore没有看到窗口。normal实际继承Mode=contract→WindowStyle Hidden；六AST执行合同真实RED→GREEN后，仅normal Main/Explore改live，build/headless仍Hidden。八版本准入合同通过，新批普通Windows1280×720/时间1/physics60，无headless/fixed-fps/movie。返回唯一实际Knight(DEBUG)窗口并执行置前：Main一次成功，Explore首次失败、重新识别同窗口后仅一次重试成功。两路线实际到Boss及有支撑出口；Main严格GREEN，Explore原owner仍严格FAIL。

推理：隐藏启动是用户未见画面的直接相关原因；新窗口对象/OS边界证据比内部Root.Visible更强，但不证明用户确已看见。Explore失败是原文本核验器把新增被动遥测中的warning/fatal布尔当诊断，不是实际路线未完成。不能改原件或追授其GREEN资格。

风险：用户尚未确认新窗口可见；未做真人、连续AV或60FPS验收。原完整post观察器失败及退出RID/ObjectDB、音频2ObjectDB、匿名FATAL、五历史失败不能借本轮quiet关闭。后续前台需新可用时段，不自动重跑。

## 两案与原件

证据根`E:/OurGame/Knight/.evidence/october13-stable-claw`。批34564全程source/helper/docs冻结（含间隙），终态exit1，仅Explore失败。fresh build均0/零警告错误，无timeout、guards=[]。终态后CIM无owned Godot/dotnet；另一个UnrealBuildTool dotnet不属本案，未触碰。用户已获告知可切换窗口。

| case | build ms | engine退出/ms | 严格owner结果 |
| --- | ---: | --- | --- |
| ranged-route-main-20261019-060755-216-a9db17f1a6e34cc4a2c90c424208fa4b | 26803 | 0/199755 | GREEN，自动实时键盘Main完成，非真人 |
| ranged-route-exploration-20261019-061151-821-0c7f202a1a13475290140d83e651d375 | 31404 | 0/282665 | FAIL Unexpected raw diagnostic，无normal qualification文件 |

窗口观察保存在`normal-visible-admission-20261019-060736-d92be188bf0d4ae58ab564e076df0efc/window-observations-v001.json`。只记录返回的本游戏窗口，未截图、没有虚构精确activation时间或用户认可。

Main PID37004/HWND11600882，sky返回Knight(DEBUG)同窗口，activate一次成功。OS ready2.287083s/physics4及finally199.118966s/physics11754均own=True，Godot loss0。只是两点OS历史，不是持续前台证明。

Main原Title同Player47596963338，8activation/8defeat、恢复0/0、深井4.633s。Boss510933339939实际fatal attack70/11502，damage/defeated11506、Died/arena_defeat各1。支撑出口11685，heldD60到11746，位置7340.7603,1079.9331、冻结保留v260,0，completion/event/count各1。Scatter236038655295、Burst572757379818、Laser788898253387都有原真实warning/shot/伤害/反击击败收据。五日志7/0/2346/0/2346、全quiet、external逐行匹配。callback12820样本p95/p99 25.175/35.132ms、max314.978、>50ms12，不是显示/GPU FPS。

Explore PID26012/HWND34474938；首次activate失败，按工具规则刷新唯一窗口、重新get_window，仅一次retry成功，实际focused UI为Godot Engine region/标题Knight(DEBUG)。OS ready前台PID46256非own，finally own26012=True，Godot loss0；不猜操作者或失焦因果。

Explore实际route_completed，Boss/支撑出口/held60与artifact完成，恢复0/0；同Player47596963338、Boss1130264268546，出口physics16561/held至16622、位置7341.191,1079.9327、保留v260,0。Top已exactTop，走旧ALREADY_SUPPORTED neutral6/300，6690到5015.845,3563.925/v0，HP1/jump16/jet0/nextcast11不变；未执行新coplanar77tick。五日志7/0/2515/0/2515，原stderr空，锚定native ERROR/WARNING/FATAL/Handle/ObjectDB/RID leak未发现；不是用忽略项消除诊断。callback15689样本p95/p99 33.327/55.550ms、max293.353、>50ms226，非FPS。

## 原核验器可重现误判，不追认原案

纯执行未改的Test-Task18UnexpectedDiagnostic逐行得到16条：stdout307–318 TASK10_STEAM_DROP_TRACE的jets里各3个warning布尔；431 TASK10_STEAM_BELL_TRACE_END的fatal_stack=False/fatal_connection=False；1251/1263/1282 TASK10_SHORTCUT_BOTTOM_WALK_END的left/right各2个warning布尔。它们不在旧限定producer/token规则内，触发旧WARNING/FATAL正则。

原owner在Get-Task11Qualification的raw gate即抛Unexpected raw diagnostic，native路线完成不等于strict GREEN；不删除原失败、不回写qualification、不修改既有pin或扩大日志豁免。本轮尚未实现分类器修正。下一需要直接原输入RED、精确typed字段合同与同一行真实ERROR/重复坏字段/畸形注入负控，所有真实资源诊断继续拒绝，再单独验证；不立即占用新的用户前台。

## 冻结、状态与下一步

Main独立保管链四pair/continuation/normal保护109保持；actual229=215source+14helper每侧，全部副本及六binary逐项匹配。SDK Junction/三exe、四byte pins及两次消费者准入齐全。source1695/六binary与v026九门批一致，helper因Mode和新合同明确变化。

```text
source 1E49DC322BCB952F519D13135CD28CCEFA71797B9A07E06278FDC4C697D2B259
helper(Main) D841B7E70ED2C7B54B9AD91C26DF1A50641A0DE69F235ACC6CA70EBBC413B3C8
owner 02FA093F9F0ECE7B00B55FB79EBBFD11059E22547CE19AEEB47075B6FEC7437A
binary6 5BB228DD1D2FCC8F47E220FD6ADB5F1465B39E387B954149B1C55403933D20FF
DLL 16D6E4CB6648E7F7EC8F4410F726E382582F2E71F0627D374E3A9E9E087D0972
PDB AE81E6E5DFC9BE51CB68350A2F411DAEC5DD90733C298A8437E95567684CF270
```

Explore独立终态审计：四pair/continuation/normal保护109保持，actual229=215source+14helper每侧及六binary全部匹配，与Main实际副本逐文件一致。helper18项path/hash集合相同，仅行序导致文本SHA D7BBACD92F73C8F56D4946BDA2631FC1C7181947CD049B43656D7A689971AB87不同。真实native诊断0，stdout/external相同16条遥测命中不是32个独立错误。11activation/10defeat、Boss实际attack81/16378→damage/defeated16382/Died1/arena1；Missile678772608597、Laser920951720715各warning1/shot1/2damage/2连接/defeat1。result SHA FCC7F891AB7B4611BC652220CF233EF0AD441A1E89AFCB11CE1BEB7E0DB3E310，隔离save仅hash 6800CCCDAC0133BA4CB7D17A0AF0D137E6050F082C2D2D96B19C9B6B45CF2BC8且held不变。OS ready2.470192s/physics4非own，finally281.073989s/16638为own同HWND34474938；无连续OS历史，长帧位置523条未保留、keyboard仅256/6712、pointer未armed，不假装完整逐tick输入记录。

保护存档仅清单/hash，不读真实用户内容。无付费/新生成美术/Git/ZIP/发布/自动化；本轮未展示新截图或录像，已有运行诊断PNG仍在各case storage，若后续展示先归档到`E:/OurGame/Knight/过程图稿与视频`。E余3562266624字节约3.32GiB，不移删旧证据。

下一后台工作仍在原目标内：修正精确遥测分类合同；处理post“异常隔离”和“reset撤销旧authority”混写的测试前提及离树target失败清理，需真实新Idle/身份/authority证据，不撤销生产epoch gate、不先认领匿名资源数。旧continuous Upper及11文件其他迁移、最终内容/可见双路/真人/性能/交付全保留。
