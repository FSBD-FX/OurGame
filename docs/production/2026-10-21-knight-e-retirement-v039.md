# KNIGHT 剩余依赖迁移与 E 旧目录清理 v039

2026-10-21。仅处理本聊天指定的 Godot 项目；本次是存储收尾，不是整关、美术或真人体验验收。完整 1–11 游戏目标仍未完成；未建立自动化，未提交、推送或更改 GitHub V1。

## 事实：现行目录与完成状态

| 内容 | 现行位置 |
|---|---|
| 资源总目录 | `G:/knight` |
| 当前 Godot 工程 | `G:/knight/Knight` |
| 专用 Godot、.NET、Python/Pillow、ffmpeg | `G:/knight/Tools` |
| 当前验证证据 | `G:/knight/验证证据` |
| 保留的 E 历史完整资料 | `G:/knight/历史资料/E-OurGame-Knight` |
| 后续关键图稿、实录、展示副本 | `G:/knight/过程图稿与视频` |
| 本次复制、隔离、验证、清理收据 | `G:/knight/迁移记录/2026-10-21-e-retirement-v001` |

用户再次明确所有本项目资源归入 KNIGHT。已删除的旧 `G:/OurGameArchive` 属于另一个项目，不能继续查找、依赖或恢复；此前沿用共享旧路径是本次执行中的错误，现已纠正。当前项目 AGENTS、总目录与媒体说明同步到本表。

### 文件与独立运行核验

两轮整树核验均通过，最近一轮 10:46:02Z–10:55:44Z：源/目标各 **1,075,455 普通文件、206,404 目录**，源逻辑长度 **135,700,518,496 字节（126.38 GiB）**。1,075,399 文件字节一致；另 56 个 Git 路径元数据文件仅接受既有清单记录的精确前后 SHA，11 个 junction 仅接受记录的映射目标。目标因 Git 元数据重连多 1,232 字节。缺失、额外、内容差异及目录集合差异为零，没有排除整类顶层目录。

当前 C# 消费者、工具和隐藏验证脚本已迁到 G。冻结源码、历史日志与原始收据中的旧路径文字保留，不通过改写历史来伪造迁移成功。当前 README 的 71 处现存目标文档链接改为 G 历史根；69 处此前已清理的媒体历史链接不冒充现存文件。

旧 E 根不可用时，最新串行模型、键盘、动作、小地图生命周期、序章 pre、实际雾基准读取 **六项通过**，并保持真实存档保护。整个文件核验已先结束，不与此轮时序敏感引擎检查并发。删除后再次 fresh build + 实际基准读取也通过：build/engine exit 0，protectedUnchanged=true；不是渲染质量、FPS 或真人证明。

### 实际清理

先将旧树同卷隔离为 `E:/OurGame/Knight.retirement-check-20261021-v001`。首次矩阵有一项失败，按约恢复原名且未删；保留失败原件。随后重新隔离、全量校验及六项运行门全部通过，才执行唯一固定目标的原生 PowerShell 删除。

清理于 **11:08:47Z–11:14:27Z** 实际完成，exit 0，Deleted=true，Error 为空；原 `E:/OurGame/Knight` 和隔离名均已不存在。E 可用空间读回增加 **137,856,872,448 字节（128.39 GiB）**。这是本轮实际盘面差值，不与逻辑文件长度混为一谈。G 历史副本仍在；删除不经过回收站，需要时从保留的 G 资料恢复。

删除后实际读回：当前 HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 和原雾基准 SHA 均保持。V1、真实 C 盘玩家存档、其他项目及共享原工具没有删除或迁移。

## 推理：为什么这次可以删除旧副本

不是因为 G 上“有同名文件夹”，而是完整内容与目录集合已核验、仅承认精确记录的元数据/链接变化，而且旧 E 路径实际缺席时 G 消费者仍通过上述运行门。被清理的是已验证有保留副本、已不提供当前执行依赖的 E 旧树，不能由此推导可删除 G 的历史资料或其他备份。

专用 Python 为本机既有完整标准库与 Pillow 的离线复制，3,615 文件、96,795,374 字节，双侧 SHA 及 Pillow RECORD 通过，非简单搬移仍依赖 C base 的 venv。实际调用统一 `-E -s -B`，拒绝受控外部 PYTHONPATH 注入，保留项目脚本相邻导入；8 个 stage/QC 导入通过。专用 ffmpeg 是 36,638,720 字节精确副本，实际小型受控编码/解码通过。没有网络安装或付费生成。

## 风险、失败原件与尚未完成

- 首次并发矩阵的 pre 在 `real second J is active` 失败；单独原样复核及最新完整串行矩阵通过。源码可推导观察窗口风险，但未证明 SHA 并发是唯一原因。未改动作、等待帧、断言或游戏数值来换取通过。
- 清理算子首次在删除前因 JSON 自动日期类型转换产生时区偏移而拒绝。保持原收据、门槛与所有其他门，仅将 reader 改为保留原 UTC 字符串并要求 PowerShell 7.5+；真实 RED→GREEN 十项测试及独立复核完成。失败运行收据仍被拒绝，未跳过保护，也无政策拒绝后的换工具绕过。
- 原美术工具完整测试仍 **79 tests / 18 Edge PNG-output errors**；新旧 Python 均有同类失败，隔离前后失败名称不变。运行时复制/模块独立性通过不等于此旧渲染管线通过；本次未修 Edge 或放宽原测试。
- 历史原生音频退出、完整 post、正常双路线/真人/FPS、最终视觉与完整 1–11 仍需继续处理。未以存储收尾宣称游戏完成。
- 文件验证是静止目录的普通默认数据流、目录和已知链接核验，不是 NTFS ACL/ADS/硬链接拓扑的原子磁盘镜像。保留用户已确认清理的 V1 媒体恢复索引，不声称本地仍有已由用户删除的 16 个大媒体 ZIP。
- Windows、Git/PowerShell/Edge 等共享主机工具及真实玩家存档不属于本次专用资源目录搬迁或清理对象。

## 工作内容量提醒

新固定媒体根的 `.monitor/check-process-media-size-v001.ps1 -Check` 是操作者一次性调用即退出的手动检查器。明确新建基线 **3,508 字节**，不冒称恢复已删除旧项目的基线；媒体说明更新后实际 4,838 字节、增长 1,330 字节，Notify=false。

20/20 控制测试由主代理另行复跑通过；阈值及状态持久化、损坏拒绝、单调高水位与链接拒绝均有记录。每新增 20 GiB 只随正常工作汇报提及，绝不停止工作、等待确认、自动打包或删除。没有定时任务、后台、自动化、订阅或游戏启动挂钩；只保存关键功能/重大视觉节点的少量媒体。

## 原始依据

均位于 `G:/knight/迁移记录/2026-10-21-e-retirement-v001/`：

- `reports/runner-20261021T104602085Z-0d5d9f5b24d042f2afd4a0aa89906616.json` 与对应 `retirement-*-summary.json`、差异清单。
- `reports/runtime-20261021-185910-962-4d36f7baa714479db01a8e82b2ad115c.json`，首轮失败及恢复收据另存不覆盖。
- `reports/cleanup-bda3f0b64a7a4621ad62927ddc4f07c2.json`、`reports/post-cleanup-readback-v001.json`。
- `reports/fog-baseline-20261021-191513-885-a3bcf7a7eb7941f89668351dc71a7ffa/` 为删除后实际消费者检查。
- `reports/date-control-*`、`reports/cleanup-date-guard-red-v001.json`、`reports/cleanup-failed-runtime-guard-v002.json`。
- `python-runtime/python-migration-summary-v001.json` 与完整复制 SHA 清单/真实失败日志。
- `reports/knight-media-helpers-*-v001.json`、`reports/ffmpeg-copy-v001.json`、`reports/ffmpeg-actual-v001/receipt.json`。
- `reports/readme-links-v001/`、`reports/media-monitor-v001/test-results-main-review-v001.json`。
