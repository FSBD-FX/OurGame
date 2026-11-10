# v062：真实敌群准备、读档与死亡恢复接入

2026-10-22。正式开发目录仍为 `G:/knight/Knight`，总目录 `G:/knight`。本轮是内部功能增量，不是试玩发行。完整第1–11部分及人口计划 Tasks1–4继续 ACTIVE；不更换引擎、不重建自动化。

## 已进入运行代码的内容

- 新增 Level 生命周期的 `CanyonResidentCoordinator`，正常保存场景的 DevelopmentV001 目录有了自动调用者，不再只有测试手工 TrySpawn。协调器遍历既有50个稳定身份，仍通过原 manager 的完整身体、走廊、地形、危险区、旧波次占位、玩家预测位置与屏内突然出生检查；未放宽任何这些检查。
- 新游戏、有效 Continue 先真正隐藏世界和角色，跨 Process/Physics 边界后分批准备（每轮最多2只），成功提交后才进入 Playing。挂载事件早于世界提交，不在那个回调直接生成。
- 实际保存场景的新游戏初始两区自动生成24只，族系为原生8／废弃2／精工2／骨骸5／幽灵7。神殿前庭冷启动 Continue 实际准备上行12＋神殿1，共13只。不是全50同时驻留。
- 准备期间实际 Hurtbox 不消耗生命、护盾处理或攻击身份；公共暂停／返回检查点命令不能重入。取消初始准备后不会晚发 SessionStarted；之后新的接受世代只启动一次。
- Development 死亡恢复在 RecoveryRequested 发布前进入隐藏准备，保留原世界延迟重置和目的／有效出发区规则。既有演员仍由 World 重置，manager只重登，不二次调用 Reset。重置与补齐提交后最后发布 RecoveryCompleted；观察者返回标题后没有旧 await 再启用角色。
- 恢复专项实际击杀了自动居民和玩家，证明入口 ResetRevision=1、其他身份=0、恢复后居民 Alive/Attached/HP3、共24个附着身份、epoch不变，并验证恢复后的真实 Hurtbox／能量和暂停保护。这里没有额外证明该居民必须更换 native ID；原 World 合同允许重用被重置的演员。
- FinalOnly、默认占位拒绝和旧手工 custody fixture 保留。24开发叶仍是明确占位；本轮没有新增图像、音频或最终族系美术。

## 测试先行与失败原件

所有短ID均为2026-10-22，完整目录在 `G:/knight/验证证据/october13-stable-claw/`。

| 案例 | 事实与后续 |
| --- | --- |
| auto-start 153311-e78ae9fa | 实际 native RED：旧 StartNew 立即 Playing，未隐藏准备。 |
| auto-start 153907-f4cbf16b | 构建0错误／1条CS0108，Ready遮蔽Node.Ready；严格构建拦截，不是行为RED。仅改名 IsPrepared。 |
| auto-start 155102-36519ed9 | 24个真实根和保护断言完成，但故意重复开始产生1条警告，严格FAIL。 |
| session 155758-78316013 | 取消／冷读档断言通过；实际致死后 request 尚未隐藏，真实RED。随后才实施恢复屏障。 |
| session 160013-0ad54ad1 | 恢复状态断言符合，末项误查 ProcessMode；原实现实际用 SetPhysicsProcess。修正测试为角色及8个组件的实际 processing 状态，不改生产策略。 |
| session 160158-e9d24e10 | 取消、冷读档和死亡恢复严格GREEN；不是最终B冻结。 |
| stream 161156-3bc66712 | 已到50身份出生历史，回入口时屏内3只正确延迟，实际21而测试错误期待24。未绕过屏内保护。 |
| stream 162017-f15cb62c | 测试退至x64不可达：48宽左墙＋40宽角色要求中心至少68；修正独立目标80，墙／身体／60帧上限不变。 |
| stream 162319-aa63b53c | 同一受伤身份新owner返回保留2HP、再实际击杀断言符合；后续测试镜头仍朝神殿，使神殿居民正确延迟。仅成对修正视口夹具。 |
| stream 162548-a293b0c2 | 最后冻结实际 FATAL，exit -1073741795／14274ms，无完成标记。保留为A20严格FAIL，不原样重跑求安静。 |
| teardown 164458-579a92e6 | 第24个真实 NodeAdded 开始既有音频清理，接受的 Intro 任务仍访问已释放 AudioStreamPlayer，实际ERROR＋断言RED。 |
| teardown 164650-0922ecd8 | 测试清理器在 Shutdown 前调用既有 CancelResidentPreparation 后严格GREEN。未修改AudioDirector、GC、缓存或音频排空上限。 |

重复开始的故意警告仅在一个同步测试作用域由 ExpectedWarningLogger 核对消息／源文件／函数／数量。owner另独立核对 stdout／stderr／external 的精确scope和警告多重集，原始日志不删不滤；实际owner函数正控1、缺失／重复／变化／ERROR／FATAL等21个负控通过。这个许可只适用于 auto-start，不能覆盖其他资源警告。

## A冻结：20条结果，12 PASS／8严格FAIL

A包含19个串行新消费者＋当前同源的162548 stream崩溃原件，不是20次新运行。实际1771源项／887 C#；20次普通安静构建，guards=0，原生进程wait无超时。

| Phase | 时间／短ID | 结果 |
| --- | --- | --- |
| auto-stream（保留） | 162548-a293b0c2 | FAIL：GCHandle FATAL |
| auto-start | 163025-a6c4a8d7 | PASS：实际24；精确1条故意警告 |
| auto-session | 163101-fc3f3a53 | PASS |
| development-published | 163137-ee168568 | PASS：仍是原手工控制用途 |
| parent | 163208-288e9aa7 | PASS：143地形／正常catalog |
| map-census | 163249-d00570ae | PASS：50 Ready、41巡逻／9炮台、720 physics，隔离调查 |
| level-lifecycle | 163427-73386b81 | PASS：FinalOnly旧控制 |
| save-compatibility | 163507-98a94926 | FAIL：音频已释放＋旧12帧初始平台假设 |
| allocation | 163546-6fef15a7 | PASS |
| state | 163618-e441523d | PASS |
| lifecycle | 163648-b870da1f | PASS |
| scene-binding | 163718-2d3a91a8 | FAIL：138 Shape RID，不是上一版137 |
| subtype-profile | 163754-7e30a1fe | FAIL：31 Shape RID |
| Archive metadata | 163823-f478d93f | FAIL：287 Shape RID |
| Steam metadata | 163855-43d65d49 | FAIL：206 Shape RID |
| structure-v2 | 163931-a24ba1fc | PASS |
| keyboard | 164007-25731065 | PASS |
| actions | 164045-78e4964f | PASS |
| pre | 164120-7f39bed7 | FAIL：GCHandle FATAL／exit -1073741795 |
| post | 164159-8bc2c22d | FAIL：AudioServer排空未在60帧／2000ms观察到mix；不是进程wait超时 |

A的post未达到75案例／8 owner／完整scope，不能继承v061的post资格。独立保管链核对100对清单、8990份选定源码副本、204份helper副本、120份binary、40组build/engine进程记录一致；不声称1771项全部均有实物副本。

## B冻结：只修改测试消费者与清理顺序

旧收集 fixture 的固定12／5／4帧假设改为有界等待真实 Playing 且 Pending=false，落地另用真实条件等待；保留所有残片、schema3／4、旧资格、源档hash与实际死亡矩阵，没有关闭新增敌群或改成FinalOnly。测试音频清理先取消接受的准备任务，避免根仍存活时依赖先被释放。

B＝同源 teardown GREEN原件＋7个新受影响消费者：6 PASS／2严格FAIL，不能同A合成28项同版。

| Phase | 时间／短ID | 结果 |
| --- | --- | --- |
| auto-teardown | 164650-0922ecd8 | PASS：第24次出生开始真实清理，之后0 SessionStarted |
| auto-start | 164824-80ed50d3 | PASS：实际24和准备保护 |
| auto-session | 164858-bcc943b9 | PASS：取消／13只冷读档／死亡恢复 |
| save-compatibility | 164937-d2c12a17 | PASS：原进度／Boss／残片持久化和可选地图矩阵；不是实际旧127→143完整Continue路线 |
| keyboard | 165020-2cf1a3ed | PASS |
| actions | 165059-269b97b3 | PASS |
| pre | 165135-96e21977 | FAIL：FATAL＋Handle；调用边界含 CaptureOriginalSpawns:149 的 GD.Load／Instantiate→CheckAdmission→TrySpawn→Coordinator.Prepare，具体释放对象仍未知 |
| post | 165214-7c716bc9 | FAIL：BasinAscentCameraRuntimeTests:121要求实际Motor自动运行，但旧fixture尚未等待准备结束；未达到75案例 |

B与A的生产代码相同，但测试和helper不同。独立核对B的40对清单、3520份选定源码副本、82份helper副本、48份binary和16组build/engine记录均一致；无wait超时、全部guards=0、普通构建安静。A→B仅QuestFragmentPersistenceRuntimeTests、ResidentAutomaticSpawnTests、RunStableClawPullTests和TestSceneTeardown四个测试C#变化，无清单项新增／删除；通用helper仅owner变化。

B pre在stderr和external均有1条FATAL及2条Handle，第二条被交错到Fatal error行后，不能只按行首漏计；两个日志是事件镜像，不相加。应用栈分别经过GD.Load和Instantiate至CaptureOriginalSpawns:149，但交错日志不证明对象身份或因果先后。B post实查旧fixture为AutomationAutoStart＋固定8 physics后开始ground hold；还须核对其预置出生点与人口准备条件，不能假定只加等待就必定成功。

B最后一次根核对1771项全部当前hash一致、887 C#、index未变；无活动游戏消费者后才写本报告。B通用helpers9 SHA为88235F8629D60B151E517260515899DC7180946789A6BEED3AC8FC7075F16588，pre H10为EF3864601BEBA92D17E40F5CA6C729FC9094105E2440432FA3FC088A443112F7。两冻用户109项共同SHA仍BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB；没有读取真实存档语义。

| 封存项 | A SHA256 | B SHA256 |
| --- | --- | --- |
| source manifest | 279734EA0D816CE202399E4F794E840F2D7CEF94F518A12691F38AFECF320FA5 | 6A6F16599CC27B38E0F93E6116B4B5DCC8A0EBFFCF588BD9E6E12BD02338DDB8 |
| six binaries manifest | 8962207D15C421CD97F2B3DED864FFA847F43DBE4037A9516447969BB3FD5F1C | 90A813B4E1833DBEDF65F21C0A02B7748282CCB07D79FBD2539A521F3F5285D9 |
| Knight.dll | 6C26D4FD4E7512E4F3D47503F537CE2BB7070ADB80508B28A36876ABC468D06F | 326393DF0706F8708429ABDA1349E9CE2F369D00EA3A887E33DC133160776450 |

## 未完成与下一执行入口

1. 已有覆盖50身份的自动协调器，但最终严格stream未通过；普通镜头下自然提前装载、返回、真实战斗／返航、原26共存与正常Main／Explore仍要完整验证。受控检查点／视口测试不替代这些。
2. B pre给出了应用层具体加载边界，但不是已识别资源拥有者；下一从CaptureOriginalSpawns既有加载／临时实例边界做可区分的归因，不用GC／全局缓存／借用Dispose／随机重跑掩盖FATAL。A的四项RID及所有历史Handle／音频失败继续OPEN。
3. 迁移BasinAscent相机fixture到真实准备完成条件，并补准备期间回调取消、常驻永久失败的玩家可见报告等生命周期覆盖；不能以CurrentState强写／禁用新敌人取绿。
4. 全50正常活动、所有新机关、最终原创角色／五族／24子型／场景／UI／动画／特效／音频、已明确范围的演出、正常双路线／旧档／真人／720p–4K／正常显示60FPS、归档短证明、唯一Windows ZIP和获授权GitHub读回仍全部在第1–11部分目标中。没有发布新试玩或最终视觉。

无付费生成、OS按键／抢焦点、Git写入／标签、旧资料删除、引擎切换或自动化。没有新过程媒体；手动容量Check仍4838B／基线3508B／新增1330B、Notify=false，不重建旧归档。新媒体位置仍 `G:/knight/过程图稿与视频`。
