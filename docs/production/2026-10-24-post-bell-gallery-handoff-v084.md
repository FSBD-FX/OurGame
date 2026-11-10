# v084 · 原Bell战后横廊交接

工程仍为`G:/knight/Knight`、资源根`G:/knight`；原十一项目标ACTIVE、未交付完整试玩。用户最终更正“等一下，是没看到”有效：无窗口/系统键鼠/真人视觉确认，教学遮挡及悬浮灯画面待核对。后台路线不算用户看到或认可。

## 事实：仅改两份测试消费者

`tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`与`tests/integration/BasinCounterweightRuntimeTests.cs`；没有生产物理/地图/AI/人数/血量/图声/按键绑定改变，仍1776源/892C#。

旧驱动第二次真实乘梯落稳横廊后又回PistonTwo，再尝试已证明会撞棚底的普通跳。本轮显式区分None/PistonTwo/Gallery，仅keyboard Main在原下层Bell真实击败后的第二次转移请求Gallery。首次活Bell访问、默认辅助器和独立消费者仍执行原PistonTwo往返，Explore不启用新终点。

新终点保留真实配重台/侧壁K/Space借力/第二K/原150窗口/完整横廊支撑/自然减速/15帧保持，以及同Player/HP/无恢复。当前Gallery精确接触、轴0、A/D/Space松开、速度≤2；原返程普通跳恰3次，无额外棚返跳/新乘梯循环。Caller只跳过已接受的GalleryOne，继续原Two/Three。默认PistonTwo合同和finally不变，不将Gallery写成新PistonTwo往返完成。

### TDD时序必须如实更正

最初误用naturalRoute非空识别正常Main，而headless传natural=null。063926/064334仍失败于旧Gallery普通跳，**不是新合同RED**。提前加入但未执行的Basin选项全部撤回，Basin恢复v083原字节后，修正入口才跑出064646真实RED：当前PistonTwo(2881.8367,3799.9387)、V0、4返程跳/1棚返跳、HP4→4，不符合请求Gallery。

只在真实RED后重新实现最小选项，最终覆盖所有keyboard Main。065026真实GREEN：同Player47127201284、当前Gallery(3084.2834,3719.933)、HP4、3原返程跳/无额外跳或梯循环，两个Gallery终点收据均返回；随后GalleryTwo(3318.06,3639.933)、Three(3565.225,3563.933)及LiftEntryFloor支撑通过。再后`ApproachTask10LiftEncounter:10090`的Single多匹配，**整条Main仍FAIL**，原日志未公布全部冲突ID。

## 推理、风险与下一步

升降旧选择器把共享Actors里的GraveLampMoth载体类型当原波次；居民precision-flyer/ghost-diver也复用此类型。现有`CanyonEncounterGroup.OwnsLiveActor`按真实binding引用及活动/退役状态判归属，是保留Single唯一断言的窄修候选；还需原Scene、无ResidentBinding、父级、batch0/HP与后续Live续验。不能改First、删居民或按距离挑一个；本轮未实现。

独立Basin旧fixture只挂载等8帧，没有证明Playing和首次MoveAndSlide落地。spawn几何贴顶合法，但不是native Floor缓存；normalMain先真实跳落PistonOne。失败缺flow/pending/处理位，不能已称Intro冻结或盲加固定帧。下一观察分开准备未完、准备拒绝、首次运动未发生、其他接触，保留原支撑断言。

Explore仍p2861死亡/请求恢复，Attack27真实Active overlap/幽灵权限未观测；Frozen节拍/Threat/范围/能量/900不变。没有合法资源归属RED，不改Dispose/GC/loader/cache/引擎。整关未通、资源错误/崩溃、旧fixture前提、正式视听与人类容错仍是风险。

## 完整原生结果

Canonical根`G:/knight/验证证据/october13-stable-claw/`，目录日期20261024；guardFailures全0、headless/human=false/wholeGoal=false。前四Main跨消费者冻结，不拼同版绿。

| 案例时间 | native退出 / ms | 严格结果 |
| --- | --- | --- |
| Main063926 | 1 / 143400 | 旧Gallery普通跳FAIL，非新RED |
| Main064334 | 1 / 129099 | 同旧失败，非新RED |
| Main064646 | 1 / 122505 | 真正POST_BELL_GALLERY_HANDOFF_RED |
| Main065026 | 1 / 122855 | 新交接及Two/Three局部通过，后波次选择FAIL |
| Explore065533 | 1 / 57351 | request1/completion0/p2861，FAIL |
| pre065711 | 0 / 172615 | 行为/PHASE_OK返回，但10Handle ERROR严格FAIL |
| post070042 | -1 / 180121 | 原180000ms watchdog且此前finalizer FATAL，FAIL |
| models070420 | 0 / 693 | 最后普通模型严格PASS |

8原生1严格PASS/7FAIL，构建失败0；最后batch0仅因models成功。路线每案stderr单错误与external镜像不重复计数。pre10Handle/noFATAL；post Handle0但1个gchandle.is_released FATAL及Fatal error0xC000001D，另2原负例WARNING，**不能称仅安静超时**。post registry engine163320才开始，31enter/30return，index29暂停控制176332返回，index30 HeadlessAutomationDoesNotCreateHumanEvidence同刻进入未返回；末用例名不归属finalizer对象，75项未全执行。

另在`october12-basin-counterweight`用既有purpose-built owner跑RouteTransfer065406/RouteEncounter065415/RoutePhysicsCadence065423：无Build/Generate、pins不变、受控headless fixed60而非性能/真人；均runtime.exit.txt=1、首个PistonOne支撑断言失败，未完成转移。旧owner无ElapsedMs不伪造；godot.log归一化等stdout＋stderr，各1断言ERROR、无WARNING/Handle/FATAL，有失败audio/phase exit drain，不能扩大成全部native资源无残留。纯geometry26/focused96/真实producer消费33本轮重跑PASS，不替代原生。

## 一次独立审查与全部裁决

一名新上下文最终审查只比本轮两测试文件与v083快照；无Critical/Important/新Minor、无修补轮/复审。历史Title/replay清理Minor及v083 Frozen输出flush异常保护Minor延期，极端异常保全风险未关闭。

全部11条Declined的root裁决和代价：

1. W/排序不变，不重新认证输入边界。
2. 首命中/资格不变，局部通路不证明全图LOS。
3. Intro/Playing不变，独立fixture失败保留，生命周期不重签。
4. callback/deferred身份父级/姿态/序列未获本轮资格。
5. LoadLevel主错及清理所有权未改，不扩展审查。
6. 旧Minor仍延期，需另做真实异常合同，不能冒称安全。
7. fragmentReturnFromCamp保留旧PistonTwo路径，后续Gallery未认证。
8. direct-anchor与resident-shelf分支证据分开；当前Main侧壁通过不掩盖独立默认三FAIL。
9. Reviewer未审原生RED时序，root按完整撤回再RED的实际顺序纠正，前两案不充数。
10. 审查时后续native待定；root现补完整后继FAIL/默认三FAIL/pre错误/post超时＋FATAL，不能挑局部GREEN。
11. Frozen/资源/生产物理AI/原十一项/全人口/正式视听/硬件真人/发行不在两文件资格内；没看到不追认，不自动前台。

执行裁决：dirty原checkout/root唯一写入构建native，运行冻结源/辅助器/文档，按快照而非clean commit归因。全部原预算/HP/人口/支撑/错误门保留，代价是整关仍不交付；不把测试驱动进展当正式内容。无Git/worktree/付费/自动化/删除。一次文档多文件patch因backlog上下文不匹配原子失败，读回证实未写入，再按正确锚点重做，不是行为RED。

## 保全与完整目标

Canonical8案40/40前后清单对一致，实际binaries文件48/48项SHA匹配，缺失0；source每案1776、pre helper10/其余七案9、user109/continuation0。models终态source1776、helpers9、live binaries6的新增/缺失/哈希漂移均0。旧Basin口径单列：每案4对source1776/bin6/helper5/user109逐字节相等；before/manifest14项及副本全匹配，SHA`EE29B6E4AE10E8728AEC7CC04CE0FEA0C2233D35F9962B24016DB563AD7EC740`。三案**无after/manifest或after副本**，失败先抛出，不能称前后归档完整；timeout=false是保存owner路径推理，非独立布尔收据。

最终普通source清单SHA`FA91EED61A48B28FD4E2DC738DCB96A39DB18231F3A4C8B0AF6E044D9EDF9F0D`，binary清单`FE37A6B3016E71A3F154B8A8C8270591878F11F38A261A0C6DFD027AE15B8990`，Knight.dll`77BBE6539BB92D3ECD6A53C54BEC60C1F694ABBAC9177EAAC8E9820B3DA80B1C`。HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6、index21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C，分支未变。

Root构建/native均结束、普通构建保留；无窗口/系统输入/新正式图声或媒体/Git写入/删除，真实save只保护清单不读语义。手动媒体一次UTC2026-10-23T23:05:34.5025519Z：current860284/baseline3508/growth856776 bytes、Notify=False/新媒体0，无调度/停工。目标ACTIVE，原十一项、76普通＋Boss/NPC、双路、最终视听动作、硬件/真人/发行全部仍必做；不以局部测试或文档勾完。

审计细分：pre10个Handle中2个GD.Load/CreateSampleFixture:509 Swap、7个OpenSample/AddChild:460 Swap、1个SetGodotObjectPtr:288→refcounted_disposed→Dispose→Finalize，不能统称10个Swap。post两WARNING分别intentional Task7 observer fault及RoomPresentationProfile非Node2D根；FATAL不在其许可内。最终helpers清单SHA`39847954B85D64F61D73C62589E4BADDB6CBB2EFCDC7594D1C07FB400DEBB044`。

最终`git -c core.whitespace=cr-at-eol diff --check`退出0，仅169条既有LF→CRLF warning、其他输出0；index复核未变。目标工具本轮读回ACTIVE，无完成/暂停/阻塞变更。
