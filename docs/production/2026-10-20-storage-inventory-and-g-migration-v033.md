# 存储盘点与 G 盘迁移 · v033

2026-10-20，Asia/Shanghai。依据用户“盘点大小、区分历史可归档清理与继续使用内容、保存后迁往G:\game”的要求。本轮完成**当前开发版、Git、运行工具、必需历史依据与第一批小型旧档案**的复制/核验和G盘重新构建；不是把全部275GiB档案搬走，也没有删除E原件。完整1–11目标仍ACTIVE，玩法/资源失败不被存储迁移冲销。

## 事实：游戏有多大

以下为文件逻辑字节，跳过重解析点，不等于磁盘分配量。根盘点1,182,753文件、222,016目录，跳过11个测试Junction、0枚举错误。盘点与迁移快照之间新增了v032文档，精确复制以单文件清单为准。

| 内容 | 盘点字节 | 约占用 | 含义 |
| --- | ---: | ---: | --- |
| E:/OurGame/Knight全部 | 295,546,194,669 | 275.25GiB | 包括大量历史记录，不是游戏安装体积 |
| 过程图稿与视频 | 159,845,688,359 | 148.87GiB | 唯一历史媒体及展示副本，必须保留 |
| .evidence | 111,910,212,946 | 104.22GiB | 原始测试、失败、冻结副本；部分仍被脚本精确引用 |
| .worktrees全部 | 19,758,666,897 | 18.40GiB | 包括当前版及26个其他工作树，不能整棵当缓存 |
| 当前开发树（盘点时） | 337,909,525 | 322.26MiB | 含测试、验证资料和派生缓存；不含共同Git库 |
| 当前素材/场景/生产脚本/插件 | 10,712,855 | 10.22MiB | 不含引擎、运行依赖、测试、导出包装，不能叫发行包 |
| 当前.superpowers | 259,893,322 | 247.85MiB | 严格验证与准入资料，现阶段必需，不是普通临时文件 |
| 当前.godot | 48,451,263 | 46.21MiB | 派生缓存/编译输出；部分当前资格依赖imported库存 |

媒体与.evidence合占约91.95%。当前没有为这版新导出最终发行包，因此**无法把10.22MiB或322MiB当作最终下载/安装大小**。现存releases等是旧版包，不能混报为当前第一关成品。

## 推理：迁移什么、归档什么

当前开发版需要完整保留，包括dirty/untracked文件、`.uid`、`.import`、被忽略的`.superpowers`、源码/资源/测试/文档，不能只clone提交记录。源`.git`原为指向E共同库的77B文件，因此新位置必须拥有真正独立Git库。

275GiB里最大的两块是历史保留数据，不是当前运行依赖，先不为了腾空间删除它们。重复可执行文件虽有62份同名同长度线索，但只抽验两份哈希一致，不能据此删除全部，尤其不能破坏原始证据/索引。两个大旧工作树player-art-pipeline（13.60GiB）和task6-review-isolation（3.89GiB）还有独有源码/资料，未整体归档前不可删除。

## 已迁到 G 的位置

| 位置 | 用途 | 核验 |
| --- | --- | --- |
| `G:/game/Knight` | 后续唯一开发主目录；入口`Play-Knight.cmd` | 复制时4520文件/337,916,711字节（不含旧.git指针），逐文件大小/SHA256零差 |
| `G:/game/Knight/.git` | 独立历史/refs/index/当前分支 | HEAD/25refs/index与源一致；fsck完整性exit0；仅登记G自身 |
| `G:/game/Tools/dotnet-9.0.317` | 新SDK，非F盘junction | 10357文件/1,489,091,793字节，全SHA零差 |
| `G:/game/Tools/Godot_v4.7.1-stable_mono_win64` | 相同Godot mono工具链 | 82文件/267,921,725字节，全SHA零差 |
| `G:/game/Tools/export-template-archives` | 原4.7.1导出模板压缩包 | 1文件/1,201,759,011字节，全SHA零差，未假称本轮已导出成品 |
| `G:/game/Tools/dotnet-sdk` | 仅供验证环境使用的G→G SDK Junction | 目标为上一新SDK，不指向F |
| `G:/game/历史依据/E-OurGame-Knight/.evidence` | 原始支持文件及17个近期/冷继续案例镜像 | 17案5837文件/544,414,503字节，全SHA零差；原文/原pin不改 |
| `G:/game/验证证据` | 后续新验证输出 | 位于项目目录外，不触犯证据/项目祖先重叠保护 |
| `G:/game/历史归档/2026-10-20-v001` | 下表六组已备份旧文件 | 逐文件SHA全部一致，E源未删除 |
| `G:/game/迁移记录/2026-10-20-v001` | 清单、哈希、复制日志、原Git登记、核验结果 | 作为迁移证据保留，不当游戏运行资源 |
| `G:/game/过程图稿与视频` | 今后新展示媒体保留根 | 新README明确旧E媒体尚未整体镜像，不冒充已迁历史 |

Git复制时robocopy的完整路径/XD未排除旧worktrees登记，初始摘要如实保留；随后**只把新G复制出的253个登记文件/6,612,853字节可恢复地移到迁移记录目录**，逐项与E原件SHA零差。未运行E的repair/move/prune，未修改原登记/refs/历史，未提交或推送。G最终无commondir/gitdir/alternates，worktree list仅G自身。fsck列出的历史dangling对象保留，没有垃圾回收。

复制时零差不等于构建后整目录仍与E逐字相同：G的两个启动器改用新Tools；新增G专用验证owner/helper，旧版文件保持；restore/编译缓存重新生成并明确绑定G；本报告及README等维护文档随后新增。已保留复制时快照与各次变更边界。

## 已保存、可列入下一批 E 盘清理的文件

**仅列候选，本轮未删除。**六组共2,646,142,016字节，约2.46GiB；已在G完成逐文件大小/SHA校验。删除E副本前还应确认不再从E旧入口运行，并另行明确删除范围；G备份必须保留。

| E原目录 | 字节/文件 | G归档目录末段 | 分类 |
| --- | --- | --- | --- |
| .build-staging | 1,395,796,705 / 5919 | build-staging | 构建暂存候选 |
| .build-checks | 392,778,963 / 398 | build-checks | 旧构建检查副本候选 |
| .derived-cache-backups | 37,348,432 / 52 | derived-cache-backups | 派生缓存旧备份候选 |
| .build-deliveries | 549,628,241 / 402 | build-deliveries | 旧交付包；G按原版本保留后可清E副本 |
| releases | 236,576,768 / 11191 | releases | 历史发布目录；不是当前成品 |
| archives | 34,012,907 / 1865 | archives | 原历史归档；保留G校验副本 |

`.build-deps`的1.12GiB模板也已保存在G Tools，可作为第二批候选，但未重验旧入口的导出路径，暂不和上述六组混报成已获删除授权。F原SDK仍被旧G:/godot/dotnet-sdk及旧项目引用，不能因为新项目迁移就删它。C盘NuGet/Git/Codex pwsh/rg属于机器工具，不是本游戏可随意删除的数据。

## 尚未整体迁移、不能删除

- E完整过程媒体148.87GiB、绝大部分原.evidence104.22GiB。
- 其他26棵工作树合计约18.09GiB，尤其美术管线/旧审阅树的独有资料和身份。
- 原E共同Git库与主原型仍为这些旧树提供身份，不能删除；G当前独立不等于其他旧树已经迁移。
- 11个测试Junction未跟随复制。它们属于原fixture身份，其绝对目标仍在E原case；不得原样跟随搬走或假称已重定向。

若要大幅回收E空间，需要另外按批次为上述271GiB左右历史资料做完整校验归档/链接身份处理，再逐批批准删除，不能按日期一键清空。G盘空余会实时变化：盘点时356.09GiB，最后采样338.79GiB；不能以目录逻辑字节和替代磁盘实际容量，也不能将全部变化归因本轮复制。完整冷档全复制会显著压缩后续余量，执行前应逐批重新核容量，避免重复多份整包。

## G 实际构建与运行验证

已用新G SDK本地NuGet缓存执行fresh restore，`Knight.csproj.nuget.dgspec.json`、project.assets和生成editorconfig实际绑定`G:/game/Knight`，不拿复制来的E缓存当G构建证明。两启动器ValidateOnly及版本检查通过，14项新owner准入合同通过。

新`verify-claw-g-v001.ps1`完整差异仅新路径/历史镜像/G-specific helper；原准入前缀、行为/时间预算与严格诊断判据不变。旧`verify-claw.ps1`仍原字节保留作为历史入口，不应继续用于新G日常验证。

| G新案例 | 实际结果 |
| --- | --- |
| thruster-module-20261020-183928-076-04b08362d7834856af5a298de9318920 | fresh build0/34465ms、native0/662ms、strictGREEN |
| thruster-p1-recovery-exit-20261020-184013-653-708fb50a218446fbaf75bb7c09d9d9fd | fresh build0/43649ms、native0/1814ms；两退出行为/无GetTree错误，但4 ObjectDB/2resources，strictFAIL，原件保留 |
| keyboard-20261020-184220-388-97d57b812b33455dbf24c126770bdc53 | fresh build0、native0/9813ms、strictGREEN；自动夹具，非真人试玩 |

三例均无timeout、guards=[]；project/存储/日志/dotnet/Godot确实指向G。前两例独立审计source1695、helpers9、user109、binary6/continuation/actual前后守恒，source仍`E5AFE9940F0BC12509F1F7419EB27B316007FF1D9F21EB6BEB72507C84DFD749`，新G binary6为`A70A51CDE05BDD5415A8B9C9D67888C43B0471580646BB8CA27BDCB4EB674A06`。exit四行资源诊断不在external，完整stderr仍严格裁决；没有证据归因迁盘，不以此隐去已知历史资源问题。

这证明当前副本可重新构建、核心/键盘夹具可运行，不等于全部迁移验收门GREEN，更不是新G显示/手感/正常双路线/真人/FPS或整关质量验收。C机器工具与109行用户保护保持，未迁动真实游戏存档、未读取存档语义。

## 风险与接续

当前G主目录与常用后台owner已切换；部分旧独立Title/normal-version-binding/录像脚本仍按旧E资格，必须逐个另立G版本，不能搜索替换历史原件/重算旧pin冒认通过。Title待比较原wait、Task14/Task10 frozen三份条件原件已另补镜像校验；相关旧入口并未因此自动适配。

工具goal的目标文字保留迁移前E路径（工具不提供修改进行中目标正文的接口），**后续实际执行根以用户新授权和本v033为准：G:/game/Knight；完整1–11范围不变，不关闭/重设目标。**E active README仅新增迁移指引，其余原件不用于继续并行开发。下一游戏工作仍按[v032](2026-10-20-recovery-exit-lifecycle-v032.md)保留的资源失败与Guidance结构首错推进，正式50新增仍0/50、24canonical及最终内容/验收/交付全部OPEN。
