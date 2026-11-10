# v321 · 正式世界落脚与当前合版取证（终态10通过／1严格失败）

## 事实、判断与边界

继续原完整十一项/V03/R-C/Q01，不换UE、不重写已通过支撑逻辑。本轮开始1910 current source87EB3740...逐项0漂移、无owned native。v320隔离几何已实际修复站脚4px／摆脚24px突跳，但正式world生命周期仍仅源码推断，不能据此签正式运行；本轮只补验证，没有新增美术或玩法，也不以测试数量声称精美度。

原Main/Explore首战后已有正常Task4WalkTo(1360)，因此只在真实Start成功后订阅现有FramePublished，保留所有原键盘/危险/HP/物理/地图/600s路线/通关门。观察器只读真实mounted Gameplay碰撞、物理world/Player身份、upward slide contact、同frame正式Draw脚底与18+18骨长，缺样/缓存错最终主动失败，退出纳入原Task10FinalizeRoute异常保护。不会要求后段永久保留入口，只看x1100..1420/y600附近的seam窗口。

## 同sector补证

`thruster-ground-gait-20261108-134924-333-cfb3d64f6cd749059f07a1e6f1cfa028`：1910输入／engine0／29887ms／failure=null／guards=[]。CanyonBasin原保存BasinCampFloor/GearEntry，world rect(1280,3996,800,40)/(2080,3996,300,40)，一次pre-tree spawn(2000,3960)后D走至2220；52walk/1handoff/1stance，root2077.7776→2082.1108，原standing sole(2078,3995.9197)不动，jump0，far继续原摆脚6.450789px。原7native／7pure／55s门不变，仅新增该第8个保存几何样段，明确不是整个sector或正式world。

## 验证器自身RED→测量实现

新test-only量测核先返回default；三个独立字面量controls覆盖双脚原地2/0、near4px突跳2/4、无共同stance0/0。`thruster-route-main-diagnostic-20261108-140245-276-611f6843f9fd46b1b40333cededabe78`，build0／无warning/error、1911输入，engine1／3306ms／guards=[]，在真实Title→Start后、订阅前因正例应2/0却default以WORLD_GROUND_SEAM_CONTROL_RED失败。109默认存档保护保持，未跑长路线；这是验证器未实现的实际RED，绝非新游戏bug或通关失败。

随后root只实现两个双拍共同stance的world DistanceTo／Max／计数，其他接线、阈值与游戏不动。成功controls marker明确native=false；真正world证据仍须来自原路线运行。

## 当前Main实际通过（不代表合版通过）

`thruster-route-main-diagnostic-20261108-140443-315-98924b7229f04f56afbbe0477c942146`：engine0／248223ms／1911输入，failure=null／guards=[]，从Title实际Start至同Player Boss与实体支撑出口，原result.AssertComplete/远程练习/普通跳/命名门/保存门保留。21普通跳全部grounded，coyote/buffered/assisted接受0；14242连续样本、三命名门各1／mount gap0，恢复要求仍0/0。

真实world接缝：level48016393730／world48721037204／Player48049948164／同物理world，old真实shape85245037130→new88013277935，tick453→454。root(1279.0477,599.933)→(1283.3811,599.933)，far两拍均stance，(1271.375,635.92)→(1271.3751,635.92004)，最差差0.00013647876px；near仍原摆脚。Entrance→Descent commit当拍实际old入口实例/碰撞仍有效。左右样本40/7、47walk、1handoff/1stance；新WORLD_GROUND_SEAM_OK在原全路线完成后才输出，不靠局部提前return取代整关。

冻结身份：source1911 `C5B80A6E2D79A0A0AB0DD44C21270DF4D6E78BFD849685141AC26BC1A129D793`；six-bin `49043E4D6FE916E3BF4C5A7D69D49AFA73C8176F5AA1C877B0F8B3FC02202AAA`。Explore亦已实际终结；独占batch21823在恢复专项严格失败后以exit1停止，不能先签当前整套资格。

Main独立只读收据核对无差：三sink原生诊断0、new controls/world marker各stdout/external1（空stderr不是marker1），五inventory对及sealed6一致。实际save SHA `0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517`与完成/held两收据同，Boss15006095644301、fatal attack86／defeat14092、completion14271、heldEnd14332，Boss/arena/completion四计数各1；实际支撑出口后60次held D的位置(7340.7603,1079.9943)、velocity(260,0)及save不变，Completed已停物理，不把velocity字段说成归零。Title有封存代码实际Title/UI/focus/ui_accept→Playing/onePlayer断言及artifact声明，无独立rawTitle起始marker；seam/jump/门在raw，不在result JSON，不虚构OS写入次数。

## Explore当前同版通过

`thruster-route-exploration-diagnostic-20261108-141045-948-e04f253377ac489192e498359d1c4e97`：engine0／313826ms，failure=null／guards=[]，source/six-bin/helper/user/continuation与Main逐项相同。独立读取三sink诊断0、sealed6与五对清单同。PID45696与Main38940不同；两进程PlayerId数字同48049948164，不代表跨进程同一live对象。

本案实际Boss28401058451130、fatal attack99@17958、defeat17962、completion18141、heldEnd18202，四完成计数各1／恢复0/0；支撑出口、60held及physics关闭、pose(7341.191,1079.9327)、velocity(260,0)、save保持。实际save SHA `24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24`与完成/held声明同。普通跳33全grounded；三命名门10036样本、bell2／entry1／exit1，mount_gaps=1是原受严格近侧基线约束的epoch重挂，绝不误记为零或豁免任意缺帧。无独立gap逐事件raw，不能重建未打印时间线。11激活／10击败不等于全76清场。

本案world handoff实际454→455（非Main453→454），两live floor形状ID、world/level与物理空间匹配；far stance世界坐标差0.00013647876、Descent commit时原入口保留，42左／7右／49walk／1handoff／1stance。new controls/world两marker各stdout/external1，root底层门和正常原路线未改。不以两路线代签全墙、所有操作、像素/真人或性能。

## 恢复兼容失败与最窄已知边界

`thruster-recovery-compatibility-20261108-142033-538-21f895e87db44f488e6f864f9172d8f9`：engine0／14878ms／无超时、原业务marker达到；但严格runner以`Thruster external diagnostic veto`失败，guards=[]。stderr与external分别包含相同3条`Handle is not initialized`，不是合计6条，也不能由3条错误断言3个对象：两条发生于AudioTests:532的正式PrologueLevel `GD.Load`及紧接的`Instantiate`（SwapGCHandleForType），后一条在终结器SetGodotObjectPtr。没有FATAL不等于资源健康。

只读逆向追踪：首个正式PrologueLevel在AudioTests:196–197载入，其finally退订、释放输入、QueueFree并等1 ProcessFrame；随后两个Seamless boundary control用手工Level、正式Player和各5个内存PackSector，不是两次保存Main重载；各自QueueFree整fixture并等2 ProcessFrames。第二fixture清理返回／`SECTOR_COMMIT_TRANSACTION_CONTROLS_OK`之后，下一正式PrologueLevel Load/Instantiate、尚未AddChild即出现前两条错误；终结器错误在下一个fixture-enter后、ready前。栈上旧DisposeFixture／ProcessFrames是await继续链，不能据此说错误发生在QueueFree本体。

本轮world observer只在headlessNormalPlan的RunTask10中构造，recovery分支没有这条调用，故observer未执行；这不排除同程序集变化对时序或资源生命周期的其他影响。旧v320同专项`...133139-066-adbcb19542304511948b15612461903c`曾通过，不能将其拼接为当前通过。无效句柄所属对象、最后持有者、创建与释放来源仍未知，未修资源根因。

Ruling：不重复失败phase筛绿，不改引擎、缓存、GC、等待、预算或严格诊断。batch21823的ground／models／rig／rigcompat已通过；后续37053只执行该批原先尚未启动的actions／pre／post／save一次，各自真实结果单列。即使其余全过，本轮仍是合版FAIL，旧原始失败永久保留。下一安全观察范围限定首正式根清理完成→两fixture各自清理完成→Audio532 Load前后及Instantiate后，不把未知Resource身份写成已定因。

## 最终同源11阶段与独立冷读

下表case均位于`G:/knight/验证证据/october13-stable-claw/`。两完整路线独立先跑；21823遇恢复FAIL终止后，37053只运行尚未开始的四phase，最终exit0。没有重放恢复失败或删掉原门。

| 阶段 | case | engine耗时ms | 严格结果 |
| --- | --- | ---: | --- |
| Main | thruster-route-main-diagnostic-20261108-140443-315-98924b7229f04f56afbbe0477c942146 | 248223 | PASS |
| Explore | thruster-route-exploration-diagnostic-20261108-141045-948-e04f253377ac489192e498359d1c4e97 | 313826 | PASS |
| ground | thruster-ground-gait-20261108-141747-004-ec0bdd546f7345ecb9dbeb97b963fb55 | 29834 | PASS |
| models | models-20261108-141848-135-d33dd357eafd46678b2e5576611e7e69 | 407 | PASS |
| rig | thruster-presentation-rig-20261108-141916-368-bf0de3bcbf4e49fdb2dbb1abb124d4b1 | 18481 | PASS |
| rig compatibility | thruster-rig-compatibility-20261108-142003-392-fdbf5a931445434fab8ad38528c63684 | 1967 | PASS |
| recovery compatibility | thruster-recovery-compatibility-20261108-142033-538-21f895e87db44f488e6f864f9172d8f9 | 14878 | **FAIL，3 Handle ERROR** |
| actions | actions-20261108-142519-977-18a19211e34a46cbb3548aaee3a8af97 | 10900 | PASS |
| pre | prologue-pre-20261108-142601-218-a928199f0ea64483adf459896f4b4743 | 141957 | PASS，原44项 |
| post | prologue-post-20261108-142851-401-9401be0ed32d4fcf9ca55f63559568f1 | 187455 | PASS，原75项 |
| resident save | resident-save-compatibility-20261108-143228-103-cad079771f70460aa463a764cb7796bc | 11911 | PASS |

11案engine都exit0、无超时、guardFailures=[]，但只10案failure=null，不能据退出码误判恢复案。post仅原合同4条负控警告：seamless1、room-profile1、flow-registry2；stderr/external是同4条，不算8次，ERROR/FATAL/泄漏诊断0。post继续原300000ms上限，本次187455ms不作旧180秒或目标硬件性能认证。其余成功案三sink原生警告/错误/泄漏0。

save暖写PID35576，独立冷读PID39484／1494ms／exit0／无超时；cold合同有真实`--headless --minimap-cold-read`，无movie/fixed-fps/OS输入，stderr空且stdout/external仅行尾差。原MINIMAP_COLD_READ_OK声明6见证，附室/旧三版/POST/GRIP8等原门均在；root另逐项核验storage全部19个现存`.sha256`侧车与源字节匹配，而不是把19说成冷读主合同6见证。当前109行真用户存档清单与此前一致。

## 全量身份审计

root对11案55对source/bin/helper/user/continuation before-after逐字节核对相同；source、六bin、user、continuation跨案亦一致，66个封存二进制实算逐项匹配。16个不同helper路径全部与当前文件相同；各phase helper清单有合法附项，不能说整份helper清单都相同。当前完整1911源集重新枚举（包括导入缓存）逐路径/大小写/哈希比对零漂移，当前6产物亦零漂移；不是只看四个变更文件。

首个只读审计把LF连接的内存文本与带末尾CRLF的保存清单直接相比，误报最后一行及空末行；随后仅规范行尾、逐行Ordinal核对为0差异。未为此改canonical/source/日志，也未重跑任何native。前后清单自身仍按原文件字节SHA比较，不隐去真实内容差异。

v320完整1910源清单→当前1911仅三处：已有`PlayerFormalVisualTests.GroundSeam.cs`、已有`PrologueCanyonGeneratedMainRouteTests.cs`改变，新增`PrologueCanyonGeneratedMainRouteTests.GroundSeam.cs`；没有生产、资源、地形、数值或导入字节更改。canonical为另一个已审helper接线。当前未剩自有Godot游戏/测试进程，5个现有`godot-ai attach`客户端未触碰。

真档清单SHA仍`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`。上述同源和10个成功案不关闭本轮恢复FAIL、历史原生资源原因、正式动作美术／声画／硬件真人或原十一项。

## 一次新增验证器审查

唯一独立本增量review C0/I0/M1，精确归档BASE及4当前SHA一致。旧route只四处生命周期接线，same-sector只参数化新增case，canonical仅source快照，无原门放宽；没有重审/代签v320生产。

新增M321延期：字面量突跳负例只移动near，未单独移动far或两脚不同delta来锁Max分支；当前far实现正确且真实Main是far共同stance，不据此推定当前测量错误。它与旧M320（phase/swing回绕专门回归缺口）分开保留，本轮不顺手扩修Minor。

review当时不代签未来live路线／全部回归、旧carry生产或M320、正常像素声画/用户认可/目标硬件/原十一项、历史Resource原因或全树一致性；root现已按上述终态逐项读回输入/bin/helper/user与实际结果，保留恢复FAIL。当前正常显示、正式美术连续动作及R-I/R-C/R-P均未完成。

下一步的最小诊断设计已只读确认、未实施：只在原对象已有引用处记录标量phase journal，关联首正式根清理、两synthetic fixture清理、下一Audio532的Load前后/Instantiate前后。借用原Load返回立即透传，不新增跨await强引用或资源集合，不额外Load/GetState/GetScript/WeakRef/InstanceFromId，不改原顺序/等待/GC/cache。Definition导出是Scene、不是Script；底层CSharpScript及报错oldGCHandle对象仍不可观测，日志会扰动时序，必须保留`timing_perturbed=true, cause_proved=false`，安静一轮仍不等于修复。

没有GUI/OS键鼠/付费/额外dump/真实档/引擎cache或GC或预算/Git/删除/自动化；无新图片或视频。本轮媒体Check只在最终正常汇报一次执行：15,203,531bytes／145files／baseline3508／Notify=false，没有达到新增20GiB，不独立打断或停工。
