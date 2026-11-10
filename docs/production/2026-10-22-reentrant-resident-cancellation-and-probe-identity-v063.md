# v063：生成回调取消修复与资源对象归因

2026-10-22。实际工程 `G:/knight/Knight`，总目录 `G:/knight`。完整第1–11部分和人口 Tasks1–4继续 ACTIVE，本轮不是试玩发行、最终视觉或整关完成。上一轮引擎问答只提供评估证据，没有游戏实现增量；本轮回到真实运行代码，不切换UE。

## 实际修复

此前只在协调器的帧边界检查取消。manager 在 AddChild／激活的同步回调后虽检查世界、epoch、slot和实例，尚未检查调用方的会话世代。因此在第一个真实居民 NodeAdded 回调中返回标题，原事务仍提交第一只，并继续同一轮生成第二只。

- 原公开 `TrySpawn` 保持默认语义；自动调用改用内部带授权版本，沿原事务所有已有授权检查携带协调器 version、manager epoch、真实隐藏准备／活动状态。
- 挂载或激活回调取消／替换会话后，当前原生事务撤销并释放自己拥有的未提交演员；协调器立即停止这一轮，不等下一帧才发现取消。
- 没有放宽身体、走廊、原36出生记录、玩家预测区、屏内出生、碰撞或占位保护；没有改变HP／攻击／地形／暂停规则，也没有增加全局缓存、GC、借用资源Dispose或新资源释放策略。

实际失败先行：`resident-auto-session-20261022-172307-815-0512fbbf…` 在真实 NodeAdded→ReturnToTitle 后为 roots=2、committed=2、sessions=0、Title，严格FAIL（native exit1／2042ms／guards0）。修复后 `172536-784-9a026b58…` 已满足这组断言和新局24，但新测试重复退订产生真实ERROR，随后又有资源FATAL，整轮仍FAIL，不能从成功marker冒认通过。只移除重复的内部退订，保留finally一次清理；`172847-145-732854ff…` 完整session严格通过。

最终同版 `174155-790-5e07b495…` 再次证明：取消时只有1个实际根触发回调、0提交、0晚发会话、角色隐藏且物理处理关闭；新接受世代自动准备24个、发布1次会话。原先初始取消、神殿冷Continue13、真实敌人及玩家致死恢复仍在同一session用例内通过。没有手工TrySpawn／位置／生命伪造。这是一个会话取消边界修复，不是所有可能重入和整关人口验收已完成。

## 一次资源身份诊断：有新增事实，没有宣称修复

原件：`G:/knight/验证证据/october13-stable-claw/prologue-pre-refcounted-identity-20261022-173431-955-e7a400f26de5450ea8235021ded94b9a/`。PID4352，exit -1073741795，13278ms，无进程等待超时、guards0；它是诊断失败，不是普通pre资格。

诊断仅在 `REFCOUNTED_FATAL_IDENTITY` 编译分支记录既有原敌人探针的Load／Instantiate／Free边界、实际资源NativeID和四类先前缺失的managed serial，并观察七处原有默认new表达式；不新增Load、Instantiate、GetState，不把Resource保存在静态登记或跨Free观察表。finalizer只读预存标量，仍只执行一次原base.Dispose。

独立核对441个ENTER／440个RETURN，唯一缺失RETURN为：

| 项目 | 实际值 |
| --- | --- |
| call／managed serial | 441／476 |
| 类型／释放路径 | EnemyArchetypeTuning／disposing=False（终结器） |
| NativeID | 9223372494369460670 |
| 已观察来源 | GraveLampMoth.tscn::Resource_0ci4r，assigned Tuning，不是默认new |
| 关联 | op407，gear-pistons.02；该probe Free后该NativeID仍Alive=true |
| 对照 | 默认Tuning serial478／call438有RETURN |

stderr SHA256：`66D27287980E2DA92B91EB4BE23E262DBDA493938FB96885F220331B6860530E`。这只能归到本次诊断中的一个包装对象；不能由NativeID／路径推断它被后续op416共享，也不能把交错出现的op419 BellBack加载认作原因。

诊断限制和后续：原instantiate-return位于观察之后，且GetChildren引入额外Godot数组包装；之后已仅修正仪器为真实返回后立即打点、GetChildCount／GetChild遍历，并恢复普通分支原Values释放循环。修正版诊断只做编译检查，没有重复运行求安静。未来若加载或观察抛异常，还需finally清除当前诊断operation；本次未观察到该异常，不能将这项精度欠缺隐去。op416记录了assigned Profile但缺Tuning关联，下一应先用根包装类型／isMoth／Tuning是否null的标量区别原因，不假设共享，不据此改变缓存或释放策略。

## 相机失败已经区分原因，但未关闭

两套旧相机夹具都在入树前放置角色，再自动开始，只等8个物理帧便检查Motor。新人口准备保持玩家保护，因此只改成等待Playing也不足：

| 夹具起点 | 独立几何事实 |
| --- | --- |
| 3915,3759 | 玩家Grow64区3831,3659,168,200与steam-valves.01巡逻身体走廊3688,3848,164,64相交21×11 |
| 3915,3547 | 未发现相同居民包络冲突，但仍须实测全部准入条件 |
| 4780,1283 | 玩家Grow64区4696,1183,168,200与split-level.01身体4732,1256,36,64直接相交 |

玩家在隐藏准备中不会自行离开冲突点。没有改成FinalOnly、关闭AI／居民、扩大时限或强写Playing获取绿色。下一入口是保留真实人口，由合法basin-camp Continue开始，复用现有键盘主路、升降与Grip9→FinalCatch，在实际抵达支持面的边界采样镜头。现有StageTask10Grip9ControlApproach确有WalkAndSettle(4780)和完整FinalCatch支持检查，但当前50常驻下未实测；它的x±3不能冒充旧静止夹具±0.25。

## 最终普通同版17项：10 PASS／7严格FAIL

均为2026-10-22，完整目录在 `G:/knight/验证证据/october13-stable-claw/`。不是与早前RED、诊断或v062拼接的合计。

| Phase | 时间／短ID | 结果 |
| --- | --- | --- |
| auto-start | 174119-578c9ab1 | PASS：真实24；精确1条故意重复开始警告 |
| auto-session | 174155-5e07b495 | PASS：新增重入取消＋原取消／13只Continue／致死恢复 |
| auto-teardown | 174235-144f1501 | PASS |
| development-published | 174307-4eeb6a31 | PASS：仍是手工控制用途 |
| level-lifecycle | 174336-22dbb4f2 | PASS：保留FinalOnly控制合同 |
| save-compatibility | 174408-ab4a956a | FAIL：FATAL＋1 Handle，native -1073741795／8790ms；本版全存档矩阵未完成 |
| lifecycle | 174444-67e3f6f3 | PASS |
| state | 174511-35eefc71 | PASS |
| parent | 174539-c17a115c | PASS：正常143地形／小地图合同 |
| keyboard | 174616-ecc147f4 | PASS |
| actions | 174651-2ca491e8 | PASS |
| scene-binding | 174730-41d4be75 | FAIL：138 Shape RID（不是137） |
| subtype-profile | 174810-f7f2ec8e | FAIL：31 Shape RID |
| Archive metadata | 174838-a5caa474 | FAIL：287 Shape RID |
| Steam metadata | 174912-3fc9d015 | FAIL：206 Shape RID |
| pre | 174945-5aea5b62 | FAIL：FATAL；本案Handle0，不沿用旧B的2条 |
| post | 175016-d84c3f01 | FAIL：ground sample Motor automatic断言；scope receipts0，未达到75案例／8 owner／4故意警告 |

独立保管核验：85对清单、7618份选定源码副本、172份helper副本、102份binary、34个build／native进程合同闭合。实际1771源项／887 C#，不是全部源项都复制了实物；所有构建安静、无等待超时、guards0。17案普通构建未带诊断符号，身份诊断标记没有混入普通证据。

四项RID诊断在stdout，虽stderr为空且native exit0仍严格FAIL；不能只看stderr或进程退出码。save的Handle边界在ResidentSceneBinding.Instantiate:23→TrySpawnCore:55，普通案没有身份观察，不能继承诊断案serial476归因。post缺scope是更早Motor断言的后果，不放宽owner。

| 同版封存项 | SHA256 |
| --- | --- |
| source manifest | DFA755D9876DEC359ED0E0AC3C81F094896FED40EE27704A9EE07AEB9FBFF467 |
| six binaries manifest | DAF5605CBAC39860AEDC135FD2DCD631803C9D59C2449ADFB2A3CB3FEAE9CAF5 |
| Knight.dll | 76C14E07EAEE150BD5C9C59AF7A9EFBAC7BF097F93A85447524E4399984FB19A |
| helpers9 | 88235F8629D60B151E517260515899DC7180946789A6BEED3AC8FC7075F16588 |
| user109（仅哈希，未读语义） | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |

修正版诊断分支仅编译0警告／0错误后，已普通Rebuild恢复0警告／0错误；根逐项复核1771当前输入、全部helpers和6个普通binary均与最终17案一致，漂移0，无活动owned Godot，之后才写文档。没有用这个编译检查补签诊断运行或普通回归。HEAD／index保持既有分支，无Git写入。

## 下一步与完整范围

继续明确Moth资源包装生命周期／缺失关联原因，迁移真实人口下的路线／镜头验证，补自动stream与原26共存和正常Main／Explore。全50自然实战、全部机关、最终原创角色／五族／24子型／场景／UI／动作／VFX／音频／范围明确演出、旧档隔离、720p–4K、正常显示稳定60FPS、真人试玩、归档证明、唯一Windows ZIP和经授权读回的GitHub版本仍未完成，不从本轮局部修复勾选全项。

本轮没有新增图像／音频／最终资产或展示媒体，没有付费生成、UE迁移、前台操作、自动化、旧资料删除或新发行。媒体手动检查：4838B／基线3508B／新增1330B，Notify=false；20GiB只随正常汇报提示，不停止工作。媒体根仍为 `G:/knight/过程图稿与视频`，不会恢复已删除的旧归档工程。
