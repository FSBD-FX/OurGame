# 同高落台、神殿准入与窗口显示纠正 · v026

2026-10-19，Asia/Shanghai。开发记录，非发布包；完整1–11 ACTIVE，Task6/7 OPEN，正式新敌人0/50、24 canonical未完成。生产Player、地图、美术及数值未改。

## 事实、推理、风险

事实：Step8缓存支撑、完整身体已在Top内时，旧自动路线仍多跳；窄控制RED→GREEN。裸V2 Boss正例未绑定准入进度，生产fail-closed本应关门；仅纠正正例后，原完整准入测试（含null负控）通过。最终九门批八GREEN、一post严格FAIL，headless Explore到Boss及支撑出口。

推理：两项是消费者/fixture前提，不应反改获批力学或关门禁。窗口问题独立：用户明确上一轮Main/Explore都未看到；实际owner Mode=contract使借用Run-Owned对engine也传WindowStyle Hidden。Root.Visible/focused不能替代用户面前可见的窗口。

风险：撤回旧“已展示/显示已确认”解释，原日志和路线结果仍保留。尚无当前可见双路/真人/FPS/最终视听资格；post资源诊断、五历史失败、音频2ObjectDB及匿名FATAL均不因quiet案关闭。完整1–11不缩减。

## 十三案

根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。全部fresh Rebuild退出0、0警告/错误，无timeout、guards=[]。90004九门批含间隙source/helper/docs冻结；终态exit1，failed phases仅post，CIM无本案Godot/dotnet后才修改。未运行生成正式场景的tools/verify.ps1。

| case目录 | build ms | native退出/ms | 实际结果 |
| --- | ---: | --- | --- |
| thruster-steam-top-seam-20261019-053955-124-900d7e8d3e54407d84fabb60835c7862 | 28971 | 1/1046 | 实际额外普通跳RED |
| thruster-steam-top-seam-20261019-054312-300-f07ed3ddbaa74b39b504f002be84472b | 25000 | 0/2135 | 同入口地面转移窄GREEN |
| thruster-temple-trigger-admission-20261019-054512-782-bb908fd1efaa4555927e8ee4b2c43781 | 29114 | 1/928 | 正例缺进度，原开放断言RED |
| thruster-temple-trigger-admission-20261019-054645-706-e4ec46b1f1ce462f874321d3d6b28366 | 25205 | 0/3867 | 合法准入及真实跨触发GREEN |
| thruster-steam-top-seam-20261019-055013-403-51d8f55470ff4b909d01e01de792069b | 27391 | 0/1952 | 最终冻结地面转移GREEN |
| thruster-temple-trigger-admission-20261019-055049-669-0c6b87e5a55441018abacddc15feedb8 | 30270 | 0/3890 | 最终冻结正例GREEN |
| thruster-boss-admission-regression-20261019-055129-624-7a00fc1174e84309bf63706a8fa2ea77 | 26173 | 0/6578 | 原完整BossAdmissionRuntimeTests含null负控 |
| thruster-free-forecast-20261019-055208-458-17e17a6d04b548d182f40a6fb1b58559 | 24974 | 0/559 | 原预测及旧2800负控GREEN |
| keyboard-20261019-055240-105-d839d79327cb417c8d9684ea2fe97a1c | 29964 | 0/9912 | 原键盘GREEN |
| actions-20261019-055326-779-332c5c8fb6204686a203430024af4685 | 25948 | 0/7231 | 原动作及过热后攻击GREEN |
| prologue-pre-20261019-055406-312-2131c60b2f2449879f85f3890847ce4d | 33590 | 0/100945 | 原完整pre GREEN |
| prologue-post-20261019-055630-714-8737de3c4820435f9b5663fbe6a0d1ed | 27800 | 1/121866 | laterObserver expected1 actual0及退出资源诊断 |
| thruster-route-exploration-diagnostic-20261019-055908-185-b314e755999c417182e23fdd4c12744f | 28275 | 0/278885 | headless Explore到Boss/支撑出口/heldD60/恢复0 |

## 窄修正与整套范围

Step8 RED实际setup physics2 P(5011.9365,3563.9253)/v(165.52658,0)、Step8=true/Top=false；physics3真实jump0→1，physics4回调失败，非setup失败。新分支仅keyboard/非claw/SteamTop且cached Step8、Idle/nonburn。保存Step8完全位于Top同平面；验证完整胶囊后，actual Motion/Module Neutral/单Left预测，仅发A/neutral去Top独有左段4965，不写实际速度/姿态/形状。73.79882为输入规划上界。

同原300内77完成Motor，到(4964.5674,3563.9253)/v0，精确named Top/full/速度≤2/fresh全松、HP5、jump/jet0、nextcast1、恢复0。缓存曾Top又Step8，不能称77tick全为Top；最终不把Step8算Top。trace17/77samples/omitted0/fault0，pre-tree受控入口不等于正常整路。

Boss RED原三physics后P(940,360)，active=False/entryDisabled=False/RequiresAdmission=True，未跨trigger。正例pre-tree纯内存newProgress六合法唯一ID（entrance/rift/abyss-floor/gear-gallery/ascent-mid/courtyard）、CourtyardSeal/Bell公共方法、Evaluate全准入、非legacy/未败，再BindRuntime(progress,null)。不Restore/save IO、不关闭门禁/直接Begin、不live挪角色。原3→Drive180（原4尾）→3不改；实际Player39023806250在physics40 BodyEntered，最终1267.1294,350.3917、activeTrue/entryDisabledFalse。cleanup没有追加等待。

pre/post不执行BossAdmissionRuntimeTests，故另strict phase先ValidateStorageRoot，原runner一ProcessFrame后完整委托未改原整套。null分支无条件BindRuntime(null,null)/2frames/门壳关闭/Begin拒绝，后续标记及exit0证明实际执行；包含受控元数据/恢复/API调用，不是自然收齐/整Boss战/真人。

pre实际28samples/12segments通过，Upper lower/mid/high分别96/78/71帧，high→mechanism各40帧。原pre stable2不混成post stable12/15%。post原11mandatory/33samples、四optional、Boss真实触发（body1058843660188/physics6128）、Traversal总门、camera及seamless10往返通过，首错`ThrowingObserverReentryAndSameTickRemountStayTransactional:1517` laterObserver expected1 actual0。1515 targetLost==1已过，1519 Idle断言未执行。

post五日志7/0/317/75/386；主动注入throwing-observer WARNING后，行为异常未到原AssertExact；终态另有Area2D RID1、Shape2D RID1、CanvasItem RID3、ObjectDB4，无Handle/FATAL。external缺终态六行；完整stdout/stderr仍保留，不能只审external称quiet。源码1481 RemoveChild(target)，target.Free在1530成功路径；失败finally只QueueFree(level)，离树target失败路径未释放是明确候选，但无实例ID不能认领全部资源数或归旧音频。尚未修正。

Explore实际11activation/10defeat（counterweight-lift仅activation，ControlledAbyss不在此路线），同Title Player46858765828到Boss及支撑出口，heldD60/恢复0。Top entry6684已有exactTop，走旧ALREADY_SUPPORTED neutral6/300→5015.845/v0；未执行新coplanar77tick分支，后者仅窄案证明。最终出口7341.191,1079.9327、保留速度260,0且Player停止物理，非v0。全保管链审计补记见后续，不以exit0自动代替。

## 冻结与下一窗口批

Explore独立终态审计补记：五日志7/0/2481/0/2481、无ERROR/WARNING/FATAL/Handle/ObjectDB，external逐行匹配；actual214=209source+5helper，每侧实际副本/四pair/continuation/六binary全匹配。Boss1079043427051实际attack81/16378，damage/defeated16382、Died及arena_defeat各1；出口16561、held至16622。原result SHA2BFF0C4D5831B14CFC8CB499066C25969E0B46018617AA27316576F5D574F74D，隔离save仅hash ED26388E9B3EA64F25B107C8C6DC9402F3B5249B40DEF8A476E86B7C1E32ADBA且held未变，不读内容。只授予此headless案资格。

最终source1695（含296 imported）/六binary相同；common新增QuestFragmentProgressModel实际保留，不追认旧案。前六actual逐侧171/167/167/167/155/155；pre182=176source+6helper；post173=168+5。四对库存/continuation、实际副本及六binary匹配；pre额外既有clearance helper，原teardown/finalizer/drain未改。

```text
source 1E49DC322BCB952F519D13135CD28CCEFA71797B9A07E06278FDC4C697D2B259
helper9 3D2CFCAC272D98AB047E25E470A20A0E845AA3E746788923AC99D1F05EA49FB7
binary6 5BB228DD1D2FCC8F47E220FD6ADB5F1465B39E387B954149B1C55403933D20FF
DLL 16D6E4CB6648E7F7EC8F4410F726E382582F2E71F0627D374E3A9E9E087D0972
PDB AE81E6E5DFC9BE51CB68350A2F411DAEC5DD90733C298A8437E95567684CF270
Player 2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5
```

用户未见旧窗口后，实际owner Mode赋值及借用WindowStyle表达式六合同先RED（main engine expectedNormal actualHidden），仅normal Main/Explore改live后GREEN；build/headless仍Hidden。原借用owner/保护/预算/资格门不改，新合同加入normal helper保留。此只证明启动选择，尚未启动新可见验证；新helper不倒填上表旧冻结。下一单独窗口批要用实际返回窗口确认并让用户核对，不能用Root.Visible再冒认显示成功。

M09更正：旧continuous Upper不是“固定60tick neutral尾”或GroundDeceleration/2800/v²；实际DriveAssisted手动sample Input/ActionModes/Claw、Motor/碰撞自动，PredictDetachedLandingX热读Tuning、固定1/60和150horizon、zone中心Y/近中心松键，没有不可变快照/完整支持/剩余预算/停速门。调用在CriticalPathSmoke/Presentation，非V2 Main/Explore；尚待原helper可区分RED→迁移。其他11文件/正确燃烧/环境授权/兼容负控保留。

当前E余3671568384字节约3.42GiB；新增证据迁G未答，不移删旧件。无新展示媒体/付费/Git写/ZIP/发布/自动化。媒体目录`E:/OurGame/Knight/过程图稿与视频`。当前可见双路、真人/连续AV/UI720p–4K/FPS、存档/死亡/Continue/旧档、历史故障与1–11最终内容全部保留。
