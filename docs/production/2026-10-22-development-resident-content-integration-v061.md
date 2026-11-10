# 24开发敌人资源与正常Level接线 · v061

2026-10-22。工程 `G:/knight/Knight`，完整1–11及人口Tasks1–4 ACTIVE。本版把既有开发子型资源从隔离生成输出接入项目；不是最终美术、自动50只出生或新试玩发行。

## 实际生产增量

1. 新增不可变 `ResidentSceneRegistry`，`FinalOnly`仍返回原24个Unimplemented目录；显式`DevelopmentV001`才选择固定项目路径、状态`ExplicitPlaceholder`，不接受任意外部路径/未知mode，不把占位标成Implemented。
2. 24个保存叶在 `scenes/enemies/residents/v001/`，24个可编辑作者配置在 `assets/data/residents/profiles/v001/`。实际正常Level按保存的选择把这份目录交给唯一manager；构建器也显式保存DevelopmentV001。旧sample/代码新建Level仍默认FinalOnly。
3. 正常父场景仅一个语义字段变为`ResidentContent=1`；旧版原字节保存在计划workspace `v061-before-canonical/PrologueLevel.tscn`。完整非该字段语义、catalog143、五区外链和持久连接检查保持；**序列化同时改名46个外部资源ID，共101行文本差异，不是其余原字节完全不变。**当前514234B/SHA `1F36A94604C8DD00EA486F9FB40ECA09EEF1E51539DF903D1B2E6570E84BF50B`。
4. 默认工厂/manager的`allowPlaceholder=false`及全部身份、支撑、完整身体、危险、玩家、可见晚生、旧spawn、重入/epoch/HP准入没有放宽。FinalOnly负控仍拒绝；实际正常Level的显式开发调用已走原TrySpawn完成1只真实Ready、正确作者位置、HP3、直接Actors父级、原生身份lease及重复出生拒绝。

这1只是隔离测试主动调用manager所得，**正常运行自动出生仍0/50**。没有自动出生调用者；没有据此完成全部新敌人的战斗、Returning、旧26共存或正常Main/Explore。原50运行布点/201路径与原26/9载体、骨骸10/幽灵10目标不改。

## 来源、版本与替换接口

| 产物 | 来源/状态 | 大小 |
| --- | --- | ---: |
| 24 `.tscn`开发叶 | 原项目九类载体＋原生行为/明确Profile，生成器`BuildResidentSubtypeScenes.BuildInto`保存并回读；旧visual明确占位 | 102053B |
| 24 `.tres`作者配置 | 保留A案`resident-map-census-20261022-120049-668-9a2387f46483492a8551808d46b8268e/storage/leaf-source-profiles`逐字节复制 | 7245B |
| 合计48个新项目资源 | 新版本目录，不覆盖旧叶/配置；不是48份新美术 | 109298B |

48成员与严格通过的stage输出逐字节一致；24配置同时与stage输入和原A输入一致。48行稳定来源清单SHA：`D5D859F61B3DFAE9C4C2323AA6D24FE08D2F66AC63D2B6AACEF939F663778F59`。定义：项目相对路径、`/`、ordinal排序、每行`路径|大写SHA256`、UTF-8无BOM/LF/含末尾LF，5594B。对应成员/哈希由本版原生案的`source-before/after.txt`和选定源副本保全，无须运行时读取旧证据。

独立核对336个直接外部引用、39个唯一目标，全部为有效项目res路径，无证据/绝对暂存路径或重解析越界。正常运行只从项目加载。叶内嵌Profile快照；可编辑作者配置不是热更新引用。后续修改应建立新版本、外部新暂存重建/验证后切换目录，不能只改作者配置就声称已改游戏。骨骸弱点/幽灵周期沿用A开发值（幽灵1.2/4秒），不是最终平衡批准。

没有新图像/音频生成或费用。原可替换Visual、Body、武器及Profile接口保留；最终角色/五族/24子型美术、动作、音效仍未完成。

## 测试先行与窄推广

证据均在 `G:/knight/验证证据/october13-stable-claw/`，以下时间前缀20261022。根唯一构建/原生owner；源/helper/build/docs在每个拥有的消费者存活期间冻结。

| phase及案号后缀 | 实际证据 |
| --- | --- |
| resident-development-stage 144830-256-5bda02f4cf4a409eafd4f43dcc6d63bd | 编译安静/native11704退出1；真正RED：首rock expectedExplicitPlaceholder/actualUnimplemented，不是编译错误 |
| resident-development-stage 144950-880-78df9b3b3a624d78b3e52e2f8af6e1f4 | 最小immutable选择实现后严格PASS：24路径/未知mode拒绝/Final目录不变、48文件、作者原字节、实际保存工厂/preReady绑定、默认占位拒绝、父非选择字段与链接回读 |
| 窄推广 | owned终态后逐项读回1721源及helper，复制48到新项目目录/每项SHA验证；旧父原字节备份并验证，再复制单语义字段暂存父并读回SHA。没有手改TSCN或让BuildInto写保护工程根 |
| resident-development-published 145334-433-cf5b9f91a7904b8593c555e9bf7a82a3 | CS0104 FileAccess歧义，明确仅编译FAIL，无原生行为RED；修正Godot.FileAccess限定 |
| resident-development-published 145440-269-9a8f5c62cef84d658803243750a11ba2 | 真正消费者RED：实际父mode1/资源存在，但Level仍交原Final目录，原TrySpawn报scene_not_ready；native1/1538ms |
| resident-development-published 145603-194-d53ceeedf4984ddb9c8beace9dd75544 | 构建器缺namespace，CS0103编译FAIL，无原生行为证明；仅补using |
| resident-development-published 145653-538-b6d9f05dd3cb48fc9196e5dd33cbde81 | 实际Level传selected目录后严格PASS，原manager完整真实出生事务/默认拒绝/FinalOnly/去重；native0/1814ms |

旧LevelLifecycle fixture现在明确在Ready前选FinalOnly，保留其“未完成最终目录＋手工custody演员”的原控制用途；不把该fixture当正常开发默认。真正正常保存选择由独立消费者测试，不修改其mode来取绿。未知mode/不完整最终目录/默认占位不通过，没有旧载体fallback。

## 最终同源17门：12严格PASS / 5严格FAIL

本版1769源项/885 C#；以下仅最终冻结批，不混入早期TDD/编译案或v060的两个冻结。finite手动脚本是一次性验证，不是定时器/监控/自动化。

| phase | 时间/身份后缀 | 严格结果 |
| --- | --- | --- |
| resident-development-published | 145849-720-a2d15e6399dc40d5a95fdef2b057e311 | PASS，正常Level/实际manager手动1出生，自动0 |
| resident-parent | 145920-047-5894397844374d9fb88e6bbafa8ab7f8 | PASS，正常143/完整catalog/旧127控制/三窄区域与负控 |
| resident-map-census | 145958-945-67755e86b57b46299f02236bb2a1e913 | PASS，实际50运行输入/41闭环9炮台/720physics；仍隔离无目标，不是正常50 |
| resident-level-lifecycle | 150134-963-01323e4b85fc4793abc7ac0aca4450f3 | PASS，显式FinalOnly旧控制/实际Flow与恢复 |
| resident-save-compatibility | 150209-815-e25f35e877404a3e9ca9c53b8192c13b | PASS，含可选地图版本保核心矩阵，非实际旧127→143完整Continue |
| resident-allocation | 150242-003-2d7b7fd7937e4172b5859f6d2540bb0f | PASS |
| resident-state | 150311-150-f5ff840c5e284220b04e6cf7e0da10fc | PASS |
| resident-lifecycle | 150339-005-917af301e59c4734b9d39c52174646ef | PASS |
| resident-scene-binding | 150409-415-a835282355f74e5e85deba1e50849e58 | FAIL，137 ShapeRID/stdout；native0/7737ms |
| resident-subtype-profile | 150445-004-da551da68ee44d0b86d57314fb9aebb8 | FAIL，31 ShapeRID/stdout；native0/666ms |
| resident-archive-metadata | 150513-578-a3aca03cac68483c9f03effbefb89c6a | FAIL，287 ShapeRID/stdout；native0/4464ms；不是v060的96 |
| resident-steam-metadata | 150545-181-a4f32d1160864011b4a5cea405f5cd60 | FAIL，206 ShapeRID/stdout；native0/5142ms |
| canyon-structure-v2 | 150618-632-862ed9ad46034fc7a9d697ffe52b7d08 | PASS |
| keyboard | 150651-591-2df3c42fcbf74640a769fa208747b680 | PASS |
| actions | 150727-565-f2d82cea489d49f49ae504d4f365c93f | PASS |
| prologue-pre | 150803-884-2d10026cfc0b4bd98bd585f544847bd7 | FAIL，1 Handle ERROR；PID51268/native0/100340ms，完成marker不覆盖错误 |
| prologue-post | 151016-004-59401f3ddf1c448b83a2625e43c58ea8 | PASS，PID51356/native0/147194ms，75原案例/10suites/0退役/8owner/节点1→1/4精确故意警告 |

pre链为GCHandle.FromIntPtr→SwapGCHandleForType:1284→Node.AddChild→BasinSupply.OpenSample:459→CheckSupply:252，stderr/external是同一事件镜像，不计两条。减少到1条不是根因修复；Archive数增为287也不直接证明本版哪个改动导致。所有原资源/Handle/FATAL历史继续OPEN，无无改动安静重跑或GC/cache/drain/借用Dispose规避。

共同source清单SHA `080FE34ADE388C1FEE6A0E8DFB1BD358166C734ABE015472F4C8C0DBAD488D83`；六binary清单 `3B752C1C415D65D93DE354038FE204C89C48133D9B37147A82FA7D762C30BBCD`；DLL `F125F8FA66AFFD85F302E501D07DC648D4C6941B9C61F1239D7D845EAA4ED00D`。用户109项hash清单仍`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`；不语义读取或修改真实存档。

独立审计精确17案，85对前后清单一致；7644份**选定**源码副本、174helper、102binary实际复哈希匹配，不是全部1769源都复制。48新资产在每案before/after共1632份实物副本包含在上述选定源码中，全等stage来源。34个实际build/native PID、argv/合同/wait匹配，16条跨案顺序无重叠；17次构建安静、无DefineConstants、无诊断运行标记、guards0、无timeout。post三个scope receipt精确对应4条故意警告，无额外ERROR/FATAL/RID；不据native全0改判五项严格失败。通用9helper清单SHA `1C2507CCDD92E26CBFDC806D54CCA09767544C99E1DABB8976B2729A051E7A96`。

## 下一步及未完成交付

- 自动出生必须在真实提交后和原生几何同步后执行；SectorMounted早于CurrentSector/Player提交，不能在该回调直接生成。新局/Continue须真正隐藏准备，避免可见突生；实际Player伤害、取消/暂停/退标题及重入token需一并验证。
- ResetFinished目前是逐sector复位通知，不是整个恢复或物理同步完成；死亡恢复协调、跨区HP/死亡保管及真实旧26共存仍需实现/复验，不能只每帧硬重生。
- 全50实战/返航、全部新机关与地图体验、最终原创视听/UI/演出、正常Main和Explore到Boss/实体出口、真实旧档隔离回归、720p–4K/正常显示60FPS、真人键盘、归档审阅、唯一Windows ZIP与获授权GitHub读回都仍在完整目标中，不缩减成目录/单只证书。

本版未新建截图视频、付费/上传、Git写入、UE迁移、自动化或删除旧资料；测试仅处理自己的临时隔离存档，不是清理真实存档。媒体手动Check4838B/基线3508B/增长1330B、Notify=false；没有20GiB新档位。最终所有拥有的原生进程结束后才写本报告；完整目标保持ACTIVE，无Task/Goal complete标记。
