# v073 · 原场景源保管、资源来源与被动牵引取证

2026-10-23。工程仍为 `G:/knight/Knight`；原十一项完整目标 **ACTIVE、未完成**。没有新美术/音频、付费生成、自动化、Git写入、删除或未经授权的前台显示。

## 事实、推理、风险

事实：旧敌人保护区的生成每轮检查9条场景路径。220340原生记录为9路径各62次完整读取，共558次返回、471个不同PackedScene native ID；不是558种敌人，也不能称558次磁盘读取。检查成功后还须在挂载/激活后再检查一次。

实现：每个居民管理器按原目录白名单保留最多9个PackedScene源引用，结束时只清引用，不Dispose借用资源；每次仍重新读保存几何、建立全部36个原敌人/候补出生保护区，并保留环境/玩家/现有及待提交演员/实际碰撞/路线/权限的全部检查。不把Rect2或准入结果永久缓存，不关AI、不改数量、血量、伤害、地图或牵引力。

推理：这能消除同一管理器内无必要的原场景重新构造。224553实际1377次源请求，9路径各153次、每路径始终1个native ID，总9个；这是源身份复用证据，不是帧率或崩溃修复证明。缓存命中后的诊断标为source-acquire，不再冒称实际Load。

风险/界限：源引用有意延长到当前管理器的生命周期，并非所有资源寿命不变。原地改Shape仍必须立即可见；无效wrapper重载，null/throw不毒化缓存，读取回调中管理器退出则拒绝返回/保管。运行中用TakeOverPath给同路径换成另一个仍有效场景，不在当前不可变原敌人目录合同内；若将来加入，须增加源失效事务，不能沿用此缓存。当前无此生产调用；没有静态跨管理器缓存或永久保活。

## 已验证改动

- 实际GD.Load依赖计数RED→GREEN；独立管理器不共享缓存，未知路径拒绝，借用场景不被清理销毁，失效wrapper和失败获取可重试。
- 原9类保存/原生身体字面比较、36个完整保护区、20种结构拒绝、实际构造器正控继续执行。
- NodeAdded中将原飞蛾共享身体半径16临时改为5000：第二次准入真实拒绝并只释放待提交演员。还原16后，同一管理器重新读取并正常接纳。测试结束还原内存资源，不改源文件。
- 同步读取期间退出管理器的真实RED→GREEN，避免清空后又加入新引用。
- PresentationProfile诊断构造序号只增长一次，记录纯托管创建调用链和当前同线程操作；主线程未声明、未开始追踪、活动上下文、结束上下文及其他线程均有原生合同。故障日志写入和上限不能中断资源构造，也不能伪称来源完整。静态诊断表不持有Resource，不在终结器查询native对象。
- 原生构造实际输出经过原严格日志分类器：70个正反例通过，追加六类危险文字仍否决；分类器本身未改。

## 被动Reel采集及独立审查

新增独立无窗口诊断，实际保存Level/初始受控位置/真实Playing屏障和原升降台路线；第一次侧壁K后记录最多150个post-Motor物理样本。无Space/短喷/强制位置、速度、目标或AI变化；正常Main默认路径不变。

本轮三次采集均在有效轨迹前FATAL，**没有**得到可用完整牵引轨迹，也没有尝试第四个距离阈值或宣布路线通过。

一次fresh-context gpt6-astra/high只读审查发现两项Important，已在同一修复轮验证：

1. 原谓词会接受“声称150样本、实际一条{}、没有结束收据”。实际AST谓词反例已RED；现要求唯一完整/结束/runner收据、真实JSON字段、声明数=实际数、连续索引/物理轮、递增输入序列、同cast及对应终止证据、无跳喷与严格native日志。永久实际消费函数测试26例GREEN。
2. 新诊断清理失败可覆盖原错误并遗留根。原生RED→GREEN两例：保留原始错误及清理错误的对象/顺序，补偿释放仅自己根，任何补偿错误仍报告，无关所有者存活。未改生产音频/GC/共享teardown或放宽日志。

辅助parser脚本首跑还暴露PowerShell OrderedDictionary没有Clone方法，已改为逐项复制后重新执行全部26例通过；这是脚本前提错误，不计作行为RED，也不计入下面22个原生案例。

源缓存读取中退出的额外根审查反例也已RED→GREEN。没有再派二次复审；不把局部修复或诊断合同称为整关认可。

## 本轮22个原生案例

原件统一在 `G:/knight/验证证据/october13-stable-claw`；目录名含下面phase、日期时间及唯一UUID。表内不同冻结不拼成“同版全绿”。

| phase / 时间 | 严格结果 |
| --- | --- |
| census-identity220010 | 构建FAIL，两个Environment歧义；不是行为RED，已限定System命名 |
| census-identity220126 | 扩展构造/线程/写入故障/上限合同PASS |
| pre-identity220340 | FATAL；52ENTER/51RETURN，唯一RangedWeaponSettings serial101，native owner未知 |
| first-reel220907 | FATAL，准备阶段，无有效轨迹 |
| resident-lifecycle221632 | 源重复获取合同RED |
| resident-lifecycle221851 | 初始源保管GREEN |
| resident-lifecycle222141 | 新负控的./路径前提FAIL，保留原件，不算生产缺陷 |
| resident-lifecycle222330 | 修正路径规范化，实际挂载变形负控与原合同PASS |
| pre222517 | 1/90049ms；正/负净空完成，第三walkway实际准备超180物理轮，gear-pistons.03未提交 |
| post222724 | FATAL/168855ms；未完成75/8owner完整资格，不因早期进度算通过 |
| first-reel223045 | FATAL/17796ms，无有效轨迹 |
| profile223135 | PASS |
| models223206 | PASS |
| resident-lifecycle223544 | 读取期间退出源所有者RED |
| first-reel-contract223626 | 原错误/清理/根释放负控RED |
| first-reel-contract224129 | 两项故障合同GREEN |
| resident-lifecycle224202 | 全现有合同及新负控PASS，4615ms |
| first-reel224238 | FATAL/20866ms，严格否决，无轨迹资格 |
| census-identity224520 | 最后同源构造诊断PASS；实际日志分类70例再通过 |
| pre-identity224553 | FATAL/26785ms，详见下面根因边界 |
| profile224651 | 普通PASS |
| models224724 | 普通PASS，诊断构建已恢复 |

合计10严格PASS、12严格FAIL（包含行为RED、前提/构建失败和真正资源崩溃），绝不是完整十一项进度比例。109组已有source/helper/user/continuation/binary前后清单逐字节一致，0差异；构建失败220010无成功binary清单对，缺1对，不冒称其构建有效。

## 最新资源证据与下一步

源复用并没有根治资源崩溃。最新224553有190ENTER/190RETURN，受现有派生Dispose探针覆盖者没有未返回调用；随后未命名的基类终结仍FATAL，不能借上次serial101或更早serial497冒充此次对象。

FATAL之前还有明确的主线程错误：`GCHandle.FromIntPtr → ScriptManagerBridge.SwapGCHandleForType`，发生在 `ResourceLoader.Load → GD.Load → BasinAscentCameraRuntimeTests.CreateLevelFixture:299` 重载Level期间。终结器FATAL为 `godotsharp_internal_refcounted_disposed / mono_object_disposed_baseref`。这说明下一步要围绕真实加载/句柄强弱转换与释放边界取证，而不是继续只追PresentationProfile、按地址复用猜重复释放、或用缓存/额外GC隐藏错误。没有完成native ownership归因。

机械爪不能靠猜释放阈值继续迁移驾驶器。必须先恢复可稳定进入该诊断的运行，再采到完整Reel轨迹和二维力学/真实碰撞/LOS证据，之后才决定路线或能力变化。

## 项目与审查边界

最终源码1776项/892C#，当前逐项hash0漂移。最后source清单SHA256 `58CA3E83D3CB01974D9455D3679F1E143DE7A9AD46F144A770ADB97C18F48FC7`；最后普通binary清单 `862507665155259E62E39C1F8D2297D510C6A90CC5C81E4B11D0BF8A60D75796`。Git index仍 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。

审查未裁定的事项由root明确保留：完整pre/post/Reel按真实失败记录；完整十一项和正式视听/敌群实战/正常Main+Explore/真人性能/发行均不由这次窄审查验收；Basin缺少指定历史快照限制完整delta归因，不为旧Main背书；旧P1/deferred退出不续签；native资源owner和根因仍未知。

用户已更正v072c“没看到窗口”，不是仍有遮挡。本轮不重开、不发送系统键鼠输入；原图渲染与真人前台可见性继续分开。没有新展示媒体，只保存普通验证日志/源码及当前报告；过程媒体20GiB规则仍为随正常工作单次检查、只提醒不停工、绝不建立自动化。

原76普通+Boss/ECHO、地图与陷阱丰富度、操作/收集/存档、正式美术动作/UI/音频与批准演出、同冻结双路、真人输入/画面/性能、ZIP/GitHub交付等完整原十一项仍未完成。暂不交付为最终可玩版，也不缩减目标。
