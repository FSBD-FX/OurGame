# v074 · 玩家占位准入修正与首段机械爪实轨迹

权威工程仍为 `G:/knight/Knight`。原十一项完整目标 ACTIVE，不是本轮几项测试通过就完成。无新美术/声音、付费、自动化、前台窗口/系统输入、Git写入、删除或真实存档语义访问。用户最后的“没看到窗口”继续有效，不能追认显示、遮挡或视觉认可。

## 事实、推理与风险

原生231126复现：旧平台支撑夹具的玩家仍在原预置 `(2800,4080)`；gear.03 的实际保存身体与玩家重叠。隐藏准备只允许四种保护区错误延后，却把实际玩家身体和 Hurtbox 的碰撞笼统写成 `native_actor/native_area`，随后 any-current_player 分支一直等待。180物理边界后，其他已启动居民还走入了该巡逻区，实际返回混合错误。

这不是理由去移动夹具、删除敌人或放宽碰撞。修正需要区分**当前授权玩家的确切碰撞身份**与其他障碍；另一个事实是原 `CheckCorridor` 在端点失败时提前返回，可能漏查通道中段。因此仅扩大字符串白名单不够。

改动：

- 管理器每次生成前/激活后准入分别捕获当前玩家本体、BodyCollider、Hurtbox、HurtboxCollider四个即时标量ID。
- 同一次原生查询的 collider/shape-owner ID、Body/Area角色与纯Player层均吻合才标为玩家命中。没有把玩家RID加入Exclude，没有保存新的Resource/Node引用；同层外来身体/Area仍是普通拒绝。
- 隐藏准备必须有真实current_player保护区错误，且所有错误都属于精确玩家白名单，才保留Alive/未Attached并启动会话。混合错误明确失败，不再被any-current_player吞成180帧等待。
- 只有端点全部是玩家相关拒绝、且提供当前玩家身份时，继续原走廊的支撑/保护区/native/electric检查，保留端点问题。其他错误/无身份的原调用者仍走原早退。代价：以前提前跳过的走廊查询现在会执行；不是第二次重查，也不代表显示性能已经验收。
- 超时消息保留最后一次已返回的准入错误，不重跑TrySpawn或物理查询取证。

## 测试先行与审查

所有原件在 `G:/knight/验证证据/october13-stable-claw`，按下表完整目录名保留。普通自动开局原24身份、五族数量、真实准备/伤害/暂停/故意警告及退出终结屏障均保留。

新实测使用原保存完整Level、挂树前固定旧位置、不后置传送/改HP/关AI：纯玩家重叠应进入Playing且gear.03仍Alive/未Attached；外来同Player层的身体放出生点必须拒绝；中段Body和Area放 `(2894,4105)`、8×8，避开所有保存端点、玩家身体和Grow64保护区，仍必须拒绝。原生231825正例RED→232103GREEN；中段Body232310RED→232510GREEN。231626因缺Process边界的测试设置失败，不当行为RED。

一轮新上下文只读审查：无Critical/Important。Minor延后：尚未把**本次gear.03同一延后身份**接到玩家/视口离开后的一次补生验证；既有其他延后/streaming合同不能代替该精确补测。现有已验证的测试Level清理工具只由private改internal复用，主体不改。

审查边界及裁决：

1. 审查不替代root实际回归；下表失败均保留。误判代价是把整批当绿，因此不合并资格。
2. 原十一项、Main/Explore、全50实战/旧26共存、最终视听与发行不在窄审查完成范围，全部继续OPEN；不能以静态保留推断运行完成。
3. 历史Handle/FATAL/RID/ObjectDB、引擎strong/weak机制及本改动是否改变概率均未修复证明；不据此改变生产缓存/释放策略。
4. 缺失/禁用/替换组件、额外形状、改层、畸形身份、截断查询、世界/危险混合和回调替换，审查仅静态核对；不把它们说成新增原生覆盖。风险是尚存边缘行为。
5. 移动演员、精确延后身份补生、额外走廊查询性能未专项认证，180物理预算未加。新清理调用未重复故障注入，沿用既有受测工具；风险分别为动态重试、时延和故障清理边缘。
6. 建议的历史Approved工具/manager快照不完整，审查不确认其全历史逐字节等价；root确认本轮仅工具访问级别、manager即时身份参数和原生源命名变更，运行期65对保管清单见下。既有未完成工作不被删除。
7. 真人/硬件性能/真实档迁移/发布及其余脏工作区不据本轮核验；没有提交、升级、真实档访问或前台授权扩张。

## 十三次原生结果

不能把不同中间版本拼成绿色，也不是完成百分比。严格PASS六案、FAIL七案；最后代码冻结的八案为五PASS三FAIL。

| 完整目录名 | 原生退出 / 毫秒 | 结论 |
|---|---:|---|
| basin-clearance-20261023-231126-191-a35e31f6bd124b57926bd2501b96ad59 | 1 / 66977 | 实际混合准入/旧平台准备失败；净空正反例已执行 |
| resident-auto-start-20261023-231626-771-bfa718c88eeb4eba8bf6d8d9d6f06aab | 1 / 36468 | 测试缺Process设置，非行为RED |
| resident-auto-start-20261023-231825-216-24f3f2582d6545f2ae8be641df6f22dc | 1 / 36030 | 真实玩家重叠/180physics RED |
| resident-auto-start-20261023-232103-814-044ce5e824f945e2b16b51e23e1e4468 | 0 / 18233 | 玩家正例＋出生点外来身体＋原24 GREEN |
| resident-auto-start-20261023-232310-113-ce7140349e01430b8fe38b02c2a13f9d | 1 / 27137 | 中段外来身体被漏掉，RED |
| resident-auto-start-20261023-232510-837-1adc00eff5fe4808995a64494814324c | 0 / 27773 | 四反/正例＋原24，严格PASS |
| basin-clearance-20261023-232737-409-4ab0fc143b554bd6ab9fe40c5a277161 | 0 / 44780 | 原92/84净空、78阻挡、同AttackId三阶段、300预算和旧平台24hold/零恢复，严格PASS |
| resident-auto-session-20261023-232855-849-79a69c0927124377b1873c5ca7bd306a | -1073741795 / 10274 | 重入取消行为后原gchandle终结FATAL，严格FAIL |
| resident-level-lifecycle-20261023-232936-629-cacb526c3b6342feb96511af4d159f18 | 0 / 4716 | 当前manager/几何/保管原回归，严格PASS |
| prologue-pre-20261023-233016-540-dc7629e725b34cb4bb2ace27ac142da2 | -1073741795 / 70640 | 三相机/延后补生/净空/键盘抓爪已执行，随后终结FATAL，整pre FAIL |
| prologue-post-20261023-233157-386-b8e6656e1f854d47b65949658ca26465 | -1 / 180063 | 原180000ms超时；已走到P1恢复/挂载教学等，但没有完整75/8owner及退出资格 |
| models-20261023-233529-105-892be3d9f218452799b55356fe8f7133 | 0 / 540 | 普通模型专项PASS |
| basin-first-reel-trajectory-20261023-233627-013-d7e6e67200c443f9ad30425865599ae6 | 0 / 24276 | 完整诊断PASS，不是正常Main或玩法接受 |

pre三相机各12静止样本完成，最小屏内余量284/184/184；steam.01真实离开后补生一次、HP3→2也有收据。但发生后续FATAL，所以不能称完整pre、相机全局/自然路线或资源稳定通过。post保留原故意警告合同，不提高超时、不自动补跑或豁免。

## 首段机械爪有效轨迹

最后一案终于到达实际首个侧壁K（整个夹具cast=2），完整42个连续post-Motor样本，原生退出0/无超时/严格日志通过。被动观察不按Space/短喷，不改位置、速度、AI、目标、力量或HP；初始受控夹具位置仍不冒认正常Title主线。

- 状态为Travel4、Latched1、Reel36、Swing1；physics678…719，样本0…41。
- 实际侧壁命中 `(2856,3721.5059)`，dock `(2808,3721.5059)`，不是Profile中心。
- 样本12最大向上速度约297.68；最终 `(2805.4468,3728.6482)`，Arrived=true/Swing/Failure=None，最终Wall/Ceiling均false。
- 最终身体底3764.6482，原棚顶3720，完整身体还低44.6482；若保留0.25净空要求，则还需约44.8982。停靠到侧壁不等于站上棚顶；active时PreviewNone亦不等于LOS失败。

下一步从这份真实轨迹分析原有Space借力释放、完整身体净空和Gallery实际视线/活Heavy阻挡，再决定正常键盘路线；不做第四个猜测距离阈值，不提高力量或移墙来求通过。

## 上游资源依据（假设，未实施缓存变更）

只读检索核对Godot官方仓库：[83762](https://github.com/godotengine/godot/issues/83762)、[112067](https://github.com/godotengine/godot/issues/112067)为目前强匹配报告；被重复关闭的问题不代表修好。[已合并101006](https://github.com/godotengine/godot/pull/101006)是另一种旧handle清理竞争，4.7.1已含该逻辑，不支持简单“升级就好”。

4.7.1 [缓存增加native引用](https://github.com/godotengine/godot/blob/4.7.1-stable/core/io/resource.cpp#L852)、[弱转强handle](https://github.com/godotengine/godot/blob/4.7.1-stable/modules/mono/csharp_script.cpp#L1855)、[SwapGCHandleForType](https://github.com/godotengine/godot/blob/4.7.1-stable/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs#L1284)与[终结断言](https://github.com/godotengine/godot/blob/4.7.1-stable/modules/mono/csharp_script.cpp#L1785)支持一个可能机制：缓存重新引用managed已收集但尚未终结的资源对象。仍缺本次swap与FATAL是否同一native owner的闭环，不能归给旧serial101/497。

后续可用独立测试进程对同一保存Level周期只比较Reuse/Ignore/IgnoreDeep；[官方枚举说明](https://docs.godotengine.org/en/4.7/classes/class_resourceloader.html#enum-resourceloader-cachemode)区分Ignore仍复用外部依赖，IgnoreDeep递归。这里只是实验提案，未新建phase/运行/改变生产loader；manager其他运行时Load仍是混杂边界。可能增加加载成本/内存峰值/资源重复，不使用额外GC、SuppressFinalize、永久保活或静默引擎升级。

## 保管与剩余目标

十三案65对source/helpers/user/continuation/binary前后清单一致，缺失0/不一致0；最终1776源/892C#，逐行live哈希漂移0。最终普通二进制由无特殊编译符号的被动诊断构建留下；所有本轮root原生已终止，其他桥接进程未动。

sourceManifest `D50B5D227AC87DA0A76C6A3CDC6B9888AC3A5C35D916BF221542BF491D444C87`；binaryManifest `30A8600F005E2ACB7C278FBE4C2BEE2BE3944205E6E38E732F524F33F0633567`；Git index仍 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。

原十一项不勾完：完整Main/Explore、全76/新50正常实战、正式动作美术/UI/音频演出、自然路线相机、资源根因及历史RID/ObjectDB、目标硬件性能、真人键盘/可见性、最终打包与GitHub版本均继续按总待办执行。本轮无新展示媒体，容量按现有手动20GiB规则，只正常汇报提醒、不停工/建自动化。
