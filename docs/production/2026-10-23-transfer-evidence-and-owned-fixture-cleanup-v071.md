# KNIGHT v071：转移取证与自有失败清理

2026-10-23，Asia/Shanghai。唯一工程 `G:/knight/Knight`。原十一项总目标继续制作，**未完成交付**。

## 事实

- 测试准备失败且清理再失败时，原来可能遗留已挂载根。现独立尝试取消该根准备、QueueFree、两次ProcessFrame／离树Free，保留primary、cleanup及全部补偿错误。正常共享音频drain、GC和成功清理路径未改。
- 两个真实Playing后的故障注入RED→GREEN；原注册暂停／指引开关／可重入退出用例仍恰好执行一次。后来普通运行又出现Handle／Finalize FATAL，所以最新整项暂停仍严格FAIL。
- Main第二K确实采到并缓存Gallery；下一物理帧`Aim→Failed/Obstructed`。真实首碰为live `precision-heavy.tscn/Hurtbox`，HP6。不是没有收到K，也不支持修改目标优先级。
- Explore七项复合断言已分别采样，阈值不变。最新仅HP2→1失败；请求／恢复0、跳3、冲刺0、全胶囊横向支撑及地面接触都符合。
- 无生产移动／抓爪参数、AI、伤害、人口、地形或正式视听改动。生产文件新增内容仅身份诊断编译分支，普通原`TryRead(GD.Load(...))`字句保留。

## 路线候选全部未成立

| 内部候选 | 实际结果 |
|---|---|
| 距侧壁dock8px、仍Reel时借力 | dock41/150、全胶囊越棚和Gallery预览60/150；63/150被live Heavy挡住。旧日志至122/150受伤才失败，现保留最早失败 |
| 真正Arrived＋Swing后借力 | dock42/150、rope48；151/150仍未同时取得全净空与Gallery LOS |
| 既有Reel中、距dock≤100且vy＜−100借力 | 8/150位置(2775.7844,3804.202)、距离88.36、vy−111.74；一次Boost/Released，但仍151/150失败 |

始终保留一份首次真实锁定起150物理帧预算、零额外普通跳／冲刺／恢复／受伤、独立第二cast、原45落台＋15保持＋返程。最后又收紧到实际Boost发布时复核距离／速度；加强后的Main未再运行，不借旧结果判当前通过。

上述中间条件只是内部候选，不是用户必须这样通关。三种失败不等于地图不可达。停止猜距离阈值；下一先被动取得完整Reel逐tick实测，再以前述实际状态和生产同源二轴算术筛选窗口，最后真实核验碰撞、动态Heavy LOS及正常输入。预测不能替代可达证明，内部候选不能吞掉其余十一项。

## 原件与结果（不同冻结不拼绿）

共同目录前缀：`G:/knight/验证证据/october13-stable-claw/`。每个案例保存原始输出、before/after、实际进程和等待收据。

| 案例目录 | native退出／ms | 严格结果 |
|---|---:|---|
| thruster-completion-pause-20261023-174821-036-a956ac25fd7441ee9bd7ac34401100bb | 1／1547 | 旧Playing/Intro前提RED |
| thruster-completion-pause-20261023-175350-674-1d2d51f24570425b967d2e748499921a | 0／4661 | 原注册用例PASS；未含后加故障检查 |
| thruster-route-main-diagnostic-20261023-175428-496-51b88b85401b4210ad1b8dfab489f513 | 1／89744 | 第二K未锁定后HP4→3，原始原因未明 |
| thruster-route-main-diagnostic-20261023-180428-248-3503dee591f743ffae6442427fe17dbf | 1／90286 | 4042首失败Obstructed、63/150 |
| thruster-completion-pause-20261023-180811-326-441e68c151d442a38ce3c440710eea88 | 1／8062 | cleanup-failure=True时根未释放，RED |
| thruster-completion-pause-20261023-180950-237-29c72a2a1c3145ffa5a2e8a31d776a0a | 0／11482 | 两故障检查＋原暂停PASS；非最终冻结资格 |
| thruster-route-main-diagnostic-20261023-181036-763-fbaac03448c748269fd1bf62e6f37d49 | 1／96474 | 同首失败，首碰实名live Heavy |
| thruster-completion-pause-20261023-181742-230-9b77b592796c4081a15d03bc1df5334b | 0／14308 | 行为receipt齐，GD.Load处Handle，FAIL |
| thruster-route-main-diagnostic-20261023-181845-294-c804cf4cc1934f409c17253ab668382c | 1／102774 | 真Swing候选151/150，FAIL |
| thruster-route-exploration-diagnostic-20261023-182111-399-b6b933514d0a411d904e98aac42f9ad0 | 1／54594 | 七项仅HP失败 |
| prologue-pre-refcounted-identity-20261023-182730-708-2fec52047d7a4a51a05ade33924fa7cb | 未启动native | Environment名称歧义编译FAIL，已限定System.Environment |
| thruster-route-main-diagnostic-20261023-182806-556-a2151e81d48d4c4baedc317f48b09909 | 1／93615 | 早Reel候选151/150，FAIL |
| thruster-completion-pause-20261023-183019-235-924091de7cb44eea9262771c605848e0 | −1073741795／14904 | 两故障检查后、原暂停完成前Finalize FATAL |
| models-20261023-183105-591-910e57be43904c1084205bd0061cdbd4 | 0／640 | 普通PASS |
| keyboard-20261023-183140-223-69eb5a696f6341d7ab7408b9d7999b63 | 0／9823 | 普通PASS |
| prologue-pre-refcounted-identity-20261023-183328-423-f03317a00ec742d7bbfaadc2cf56b2d8 | −1073741795／65679 | 唯一有效身份诊断native，FATAL；非行为资格 |
| prologue-pre-20261023-183510-495-8db5fe1f5ae54706a591e88a238c1122 | −1073741795／70941 | 最终普通pre FATAL |
| prologue-post-20261023-183656-105-7b929eb234d64531829a0b5389517c29 | −1／180061 | 原180000ms上限TIMEOUT＋Handle，75/8owner未完成 |
| models-20261023-184034-479-1bf751b58d3f466399c2712bc09d5da2 | 0／544 | 最终普通构建／models PASS |

最终普通pre的正常旧Laborer出生总左移210.0144、Playing左移171.51196、支撑87、同AttackID三阶段；随后FATAL。不能继承v070负例／上层24hold为本版已完成。post不增加墙钟凑绿；超时不等于已证明卡死根因。

## 资源归因边界

有效诊断call连续1–551，551 ENTER／550 RETURN；无缺号、坏行、重复或配对元数据不一致。thread1终结器529／528，thread2显式Dispose22／22。唯一未返回：call551、`Knight.Config.PresentationProfile`、managed serial497、hash34786562、disposing=False、thread1、pointer `0x0000029AA3600B10`。首FATAL为`PresentationProfile.Dispose(false):155→GodotObject.Finalize→GC.RunFinalizers`。

全部1101条Dispose均native_identity=unknown；同serial无NativeId／资源路径／owner映射。本案三份raw无Handle，不证明与其他Handle同根因。相同pointer早先属于正常返回的RangedWeaponSettings／serial161，地址复用不是同对象／双释放证明。最后operation3285的Load／ID／readerTrue均完整返回，不能借给最终器当owner。

新增诊断只包原Load／reader：纯load-return先于一次新增IDgetter，保存operation／slot／path／thread标量，finally清context；拒绝重入，Begin日志失败清自身context。无新增Load、GetState、Instantiate、Resource缓存或最终器内native读取。IDgetter及IO明确扰动时序；PackedScene ID不是其内部坏资源的归属。

下一须取得**同一managed serial创建／绑定时的来源**，而非给unknown包装对象借配最近Load；因果未明前不做保活、GC、共享Dispose或引擎配置猜修。

## 原十一项与下一依赖顺序

[完整稳定ID总账](2026-10-18-project-remaining-backlog-v001.md)范围不变：移动、围合多层地图、76普通敌人及Boss/NPC、机关、键盘/收集/存档流程、AI指引、灯/雾/小地图、正式分层美术动作、UI/音频/批准演出、稳定性能、版本交付；排除FTR和另一个UE工程。

资源归因与真正可玩闭环两线推进；正式视听可从已有获批灯V02等明确样段独立接入，不必无限等待内部驾驶器迁移，不复活被拒总装、不擅自付费。完整同版Main/Explore、全50实战及恢复、正常速度视听、真人键盘、真实显示性能、Windows ZIP/解压核验及获授权GitHub版本仍是最终门。本轮新最终栅格／动画／声音／过程图像或录像均0。

## 保全、审查与目标状态

- 19活动目录76/76 source/helper/user/continuation前后manifest相同；18 native案例18/18 binary对、108/108 sealed项一致。唯一额外guard是编译失败无fresh binary，不忽略。
- 最后普通1775项/891C#，live零漂移。source manifest SHA256 `D926F4C35CC541A506963BBB844CBD7052AD614A3A4898E4A9C333101D1F04B9`；binary manifest `CFA9875563CACDA7AA1854D72CFDB7120331E93A24D8D42649F6F6CC02487820`。
- Git index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未改。普通重建已恢复，无identity define遗留；全部root owned进程终止。无前台输入、真实存档内容读取、Git写入、删除、自动化或付费。
- 窄审查Important及两Minor均修：未到Playing原错误保留；containment不覆盖检查错误；精确两项原异常，补偿错误不能混进成功。审查不认证整关／资源／视觉。
- 仅一次正常汇报内媒体检查：UTC10:43:21，新媒体根4838B／基线3508B、新增1330B，未到20GiB。此数不是游戏总大小；未建立监控自动化。
- 现有goal工具仍返回旧blocked状态，本轮未设置blocked、未重建缩小目标、未标完成。用户已要求续推，工程和完整总账照常更新；目标面板恢复由用户控制，当前工具无恢复入口。
