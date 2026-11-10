# v082 · 外来攻击防御保留与齿轮下层完整往返

工程仍为 `G:/knight/Knight`，原十一项 ACTIVE，未完成整关交付。用户最后更正“等一下，是没看到”是本次显示状态的依据：未确认看见，不记为遮挡复现或视觉认可；没有再次显示窗口或发送系统键鼠输入。

## 实际变更及证据边界

最终代码仅改变 `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`；没有改生产物理、AI、HP、人数、地图、能力或正式图声。原900战斗预算、140阶段预算、2350–2354助跑窗口、原跳数/完整支撑/无恢复条件均保持。

1. **只保留另一攻击者的既有防御要求。** frozenMoth分支仅对非零且不同Actor保留 `genericGuardBeforeSpecialization`；同Actor前摇/俯冲时机与GuardSource选择不变。实际Explore054056及055515在旧p2433处generic True→True、真实L held/active，HP1越过旧p2460死亡点。但p2861仍被原waveMoth Attack3击杀：E0、盾held但inactive、未过热，完整Explore严格FAIL。现有Moth日志的source是所选怪物位置，不是真正GuardSource，不能据此声称方向或来源配对的挡击已证明。
2. **先记录助跑首次失败，而不是扩大窗口。** Main054349 p5081完整地面、x2353.9385、V146.66669，尚未ready；p5082才到V169，但x2356.7551已越过原2354。跳数8/8、Space松开、scale .65、轴1/右向盾及完整地面仍成立。不是漏掉现成ready帧或键盘失焦。
3. **采用普通中立刹车，仍计入同140。** 保持L，松A/D，核对raw及采样均为0、完整原台支撑、无额外跳；5个physics从x2348.9387/V-133.33333停到2345.05/V0，再D＋右向L。实际Main054853在x2353.9775/V169 ready，总阶段45/140，原盾跳和第二GalleryTraverse完成。原Bell真实击败、同Player/HP4保持、4次返程跳、最终真实PistonTwo `(2881.8367,3799.9387)`，下层完整往返首次成立。之后首次到达的UpperGearGalleryOne普通跳仍FAIL；不能称完整Main通过，更不能用脚本4px窗口证明真人容错。

后续GalleryOne目标x3040–3280、顶3756，比PistonTwo高80；终点x3019.9883/脚底约3836不满足完整落台。右缘接近左墙只是位置事实，没有实际collider/首次跳消费/轨迹收据，不能先断言撞墙或起跳过早。下一步只在原失败清键前记录首次accepted jump P/V、最低feetY、raw/sample输入、身体/目标bounds及实际slide collider/normal，不先增预算或改地形。

## 借用资源假设被实际反例否定

只读审计发现原support观察器在两个GetSlideCollision返回上使用using，生产注释又称借用；注释不足以成立资源缺陷。临时同步合同在原firstPistonOne-return调用前取得结果，原观察器调用次数/时机不变；之后核对valid、normal/collider及第二真实消费者。Main054349 p3282实际nativeId `9223380196537669076`（有符号 `-9223363877171882540`）：`shared_wrapper=False`，观察后原结果仍有效，字段及第二消费者全部PASS。

因此没有合法RED，**没有删除原两处using或宣称所有权修复**。临时合同/帮助函数在原件和源快照封存后移除，最终保留原调用。Bell/Rust/Skitter的无显式Dispose自有ray创建只是源码候选；延迟RefCounted回收或总Performance计数不等于特定所有者泄漏。未批量Dispose、强制GC、换引擎/缓存/loader，未豁免ERROR/FATAL/RID。

官方线索只作关联，不作本项目根因：[Godot #112067](https://github.com/godotengine/godot/issues/112067)在本次读取时open/confirmed，包含相似SwapGCHandleForType错误；[#124587](https://github.com/godotengine/godot/issues/124587)为open/needs testing且维护者要求复现项目；[#124498](https://github.com/godotengine/godot/issues/124498)closed/not_planned不是已修复。没有下载运行陌生复现或据评论采用缓存绕过。

## 本轮全部原生结果

原件根：`G:/knight/验证证据/october13-stable-claw/`。时间均为唯一目录名 `20261024-` 后段；保留各案stdout/stderr/external、provenance及前后源/辅助器/存档字节清单。

| 案例/时间 | native退出 / ms | 严格结果 |
| --- | --- | --- |
| Explore054056 | 1 / 55076 | 旧foreign防御覆盖纠正，后续E0死亡，FAIL |
| Main054349 | 1 / 99991 | 借用假设被否定；原助跑首次失败，FAIL |
| Main054853 | 1 / 128163 | 下层原Bell完整往返通过；UpperGearGalleryOne失败，FAIL |
| forecast055333 | 0 / 554 | 严格PASS |
| heavyDefault055410 | 0 / 19900 | 原Bell sight/model/default行为严格PASS，非资源修复 |
| Explore055515 | 1 / 55314 | 最终代码仍E0死亡，FAIL |
| pre055651 | -1073741795 / 109777 | 7 Handle＋1 FATAL原生崩溃，FAIL |
| post055917 | -1 / 180052 | 4 Handle＋原180秒TIMEOUT，FAIL |
| models060254 | 0 / 541 | 最后普通模型严格PASS |

合计9原生、3严格PASS/6严格FAIL，无构建失败；批处理最后退出0仅代表最后models，不能给整套续签。三个纯合同本轮最后重新执行：geometry26、focused96、真实诊断producer消费33全部PASS，不是原生通关资格。

**pre055651确切FATAL**是 `gchandle.is_released()`，`mono_object_disposed_baseref (csharp_script.cpp:1788)`，托管栈Dispose→Finalize→GC.RunFinalizers、0xC000001D；不是旧045248的 `script_bindings.is_empty()`。7条Handle栈经过BasinSupply/OpenSample/AddChild，不能据此认定最终finalizer对象。最后明确完成标记是FIRST_COMBAT_GUIDANCE_RUNTIME_OK，末行audio-drain；没有PROLOGUE_TEST_PHASE_OK pre，不能写成前半功能全部返回。

post registry在engine160213ms才进入，38 enter/37 return；最后index37 TargetFreedDuringTravelFailsWithoutPullOrStaleReference在179902ms进入。整段180秒不归因最后用例，也未扩大上限。全部guardFailures0；stdout ERROR0；失败Main/Explore的stderr单条托管错误与external镜像不是两次独立故障。pre的8和post的4错误同样不跨日志重复累计。

**v081计数更正，不改原件：**045248有334条完整unsafe错误头，另2条崩溃穿插断裂消息，共336。完整可识别类型token为311 Ray、21 Kinematic、3 InputEventKey（普通字符串形式），余1条截断PhysicsRayQuery前缀无法补全。Kinematic包含19完整单行、1换行及1与Fatal交错的可识别token；旧20/2未知划分混用了行计数与类型计数。合计统计不能定资源所有者。

## 独立审查、裁决与代价

一次新上下文最终只读审查，仅比较本轮单文件与v081 models052948 before；新Critical/Important/Minor均无，不做无依据修补轮/复审。原生证据由审查者直接检查；继承Title/replay清理Minor未重审或关闭。root保留唯一写入/构建/native权限，运行期间冻结全部源/辅助器/文档；代价是dirty快照归因，不声称clean commit验收。

审查全部Declined项目的root裁决：

1. **W与排序：**本轮不改；代价是旧reader/ranking边界未重新认证。
2. **物理遮挡/资格：**无新授权射线或resolver改动；代价是历史first-hit不等全图LOS合格。
3. **Intro→Playing：**准备/提交未变；代价是局部路线不替代全生命周期。
4. **callback/deferred身份：**只保留值观察；代价是其他退出身份/父级/姿态仍需实际证明。
5. **LoadLevel/双异常清理：**本轮未改；代价是继承故障合同未重签。
6. **完整Main：**保留下层往返局部进展及下一个真实Gallery失败；代价是主线不可交付。
7. **Explore/平衡：**保留E0真实死亡，不增能量/HP或减敌；代价是持续反击空窗与真人节奏仍未合格。
8. **资源：**特定借用假设反证优先于注释，aggregate不足定所有者；代价是Handle/FATAL及历史Bone/Ghost RID仍OPEN。
9. **原十一项/人口/正式视听/硬件/存档/发行：**本轮一文件测试不承担这些资格，未读取真实存档语义；代价是原76普通＋Boss/NPC及完整必做范围不缩减。
10. **真人显示/认可：**以“没看到”为准；代价是显示、教学遮挡和真人可玩仍待新的方便时段核对，不自动开窗。

## 保全、当前状态与下一步

9案45对前后清单一致、54封存二进制全部匹配、缺失0；最后普通源1776/892C#、9 helpers、6 binaries实时漂移0。最后source清单SHA `13A58FE0A87B41EC4EA5DF140A8608DED4411140F52383285583BDE3C714439F`；binary清单SHA `EC208042C925C3E31665EAFC4C73CF06DFBC593AB21FE457CA375D618D8E3FDC`；Knight.dll `5F1FA7EED2892ABF315F01D165BB80CA38BC36ABC2A386D071EA0A422EC84B05`。

分支仍 `codex/task6-certified-final-20260902T0555`、HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。全部本轮root构建/native已结束，保留普通构建；未Git写入、删除、付费、新媒体、美术声音生成、自动化或读取真实存档语义。

最后文档后复点仍45对/54二进制匹配、1776源/892C#/9 helpers/6 binaries实时漂移0，`git -c core.whitespace=cr-at-eol diff --check`退出0；169行既有LF→CRLF警告不作新缺陷。一次手动媒体检查UTC `2026-10-23T22:11:13.8330654Z`：current860284、baseline3508、growth856776 bytes，Notify=False，本轮新增媒体0；没有监视器/自动化或停工。四个已有godot-ai连接服务不是本轮native测试进程，未关闭它们。

下一有依据的次序：Gallery首个失败原子观察；Frozen反击资格位图及既有AttackStarted实际E收据；特定资源所有者定位；完整同冻回归，再原十一项的正式视听/正常双路/硬件性能/真人验收/版本交付。不能把又一轮绿专项替代这些未完成项。完整目标保持ACTIVE，不完成/暂停/阻塞。
