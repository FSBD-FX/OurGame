# v138 · 已知旧版小地图兼容

KNIGHT，工程G:/knight/Knight。原十一项目标继续ACTIVE。本轮是原P06/F04的限定生产兼容实现，不是新地图、美术或整关交付。v137混战联调及历史资源错误仍OPEN。

## 事实、推理、风险

事实：旧v133与当前地图均518400个8px cell、143个固体；只修改了一处平台，但边界12440→12436，旧后续边界ID整体错4。两份位图都是1555字节，不能靠长度或直接复制ID迁移。旧目录的物理来源与完整摘要均已核实。

实现：可选地图codec只对这一个精确版本对转换，并核验目标完整序列化SHA256。保留未变世界位置的旧已探索位；变化平台及触边105个cell保持黑暗，旧48条该平台边界全部丢弃，其他边界精确重映射。未知版本或冒用当前revision的不同几何继续拒绝可选地图，不重置核心进度。没有运行时历史路径依赖，不读取真实玩家存档，不自动改写旧文件；当前版本普通读取和model Restore原合同不变。

推理：这是保守保留已知探索，不能重建过去的观察位置或动态遮挡。105个cell/44条当前边界可能需重新探索，代价有限；盲目补亮会泄露原本未发现的路线。其他旧版本、未来新地图仍需各自的可信映射，不宣称通用迁移。

风险：专项与独立冷读证明该转换，不等于完整Continue交互、正常画面/帧率或全部现实存档通过。首关Main/Explore和稳定性仍有实际失败，禁止据本轮发布成品或缩减原十一项。

## 实际RED→GREEN与独立冷读

九案均在 `G:/knight/验证证据/october13-stable-claw/`。所有构建原始exit0、零警告/错误；无超时或guard失败，ExpectedFailure全部空。不是通过豁免错误取得结果。

| 案例目录（日期均20261027） | strict | native ms / 结果 |
| --- | --- | --- |
| resident-save-compatibility-034422-980-fab3b7d030cc4e9ebd8725b9b19eca9b | FAIL | 11279，原读档丢已知旧地图；旧/current目录SHA及未变边界证明先通过，是真实行为RED |
| resident-save-compatibility-034540-941-529318061ff94c34b6bb758d74c3935d | PASS | 11277，稀疏探索与核心/源字节不变GREEN；此时尚未声明cold |
| resident-save-compatibility-034851-580-ae97eed0b6a3427ba1bd25406ed588cc | PASS | writer11366，28负控、零/满/稀疏、原存档/IO/容量矩阵 |
| 同案engine-cold | PASS | 470，独立reader读取原writer的三份真实旧wire证人 |
| resident-parent-035117-068-55fc207e64894a5b895fb4d857d7eab4 | PASS | 16284，保留未知几何不迁移合同 |
| models-035206-820-42362907459a4c6f922656f4e183c7df | PASS | 493 |
| prologue-pre-035237-640-0549f9d266b1446fac34132d5a7284c5 | FAIL | engine0/155990，但7个Handle ERROR严格否决 |
| prologue-post-035541-804-9f258cd9286f41b8ab6ac9a399ebe69b | PASS | 195913，75 enter/return及owner8完成；保留原允许warning多重集合 |
| thruster-route-main-diagnostic-035931-512-fd3b9e364ac84219bf41fb0035e3d591 | FAIL | 106085，死亡后复位2987.11，不满足无恢复主线 |
| thruster-route-exploration-diagnostic-040146-201-4dbd933a9bdf43a4a99025f4e2decf81 | FAIL | 142648，原900轮后Steam仍SentryHP2/玩家HP2，非完整探索通过 |

主案例严格5PASS/4FAIL（含初始RED），另cold1PASS。后七案同一冻结源码。前段资源错误有历史同型记录，但本轮不作新因果归因，也不通过反复重跑将它洗绿。所有案例FATAL0不等于无ERROR。

最终writer PID36056、reader PID17108，reader在writer结束后启动，同一隔离storage，绝非第二个writer。三份文件与64字节sha证人均早于reader，cold三sink ERROR/WARNING/FATAL均0。旧迁移文件SHA `B7568B726805AEDB23A4A228047C7E705CA7C861C8DC6DCC9CF3138E6216DEA3`，读后仍是v133 revision/format1，源文件未被转换后DTO覆盖。

测试逐位验证518400 cells及12436 boundaries，独立字面sentinel检查旧6525→新6525、旧6574→新6570、旧12439→新12435；验证新padding为0。伪目标、未知pair、错误格式/长度/Base64/非规范补位/超量/坏core/legacy core均保留原安全边界。完整core非默认检查点、残片、门/机关、教程、Boss/完成、真人最佳记录逐字段不变。

## 审查、实体保管与边界

[唯一独立审查及根裁决](../../.superpowers/sdd/2026-10-27-known-minimap-revision/final-review.md)：C0/I0/M0，无C/I修复或重审。本轮没有新增Minor；原20项延期仍OPEN。

[独立实体核对](../../.superpowers/sdd/2026-10-27-known-minimap-revision/custody.md)已ENDED，UTC20:11:16.4203610，才解冻写本报告。45/45清单字节对、4492实际副本SHA、54/54 sealed binaries一致，新增缺失/额外/未映射/哈希错均0。当前1852/CS892、Debug6文件与最终清单一致；只有codec/SaveTests/ColdReadTests三CS及canonical改变，无源增删、无新UID。HEAD/branch/index旧pin不变。旧14helper及LeftPNG缺口未重审、未关闭。

没有新美术/动作/图稿/录像或资产表变更，不把程序资源兼容冒称Ori级效果。手动容量Check仅一次，UTC2026-10-26T20:12:42.4735375Z：current1431609、baseline3508、growth1428101字节，16文件，Notifyfalse；未重置基线或创建自动化。无付费、前台/OS输入、Git发布、删除、真实存档语义读取或UE工程修改。

## 下一步

保存本轮限定兼容结果；不重复这九案。下一回到Main混战/Explore资源与威胁交接。已核实K不扣能或刷新regen：成功攻击/Heavy及持续盾才刷新.55s；第四K回收后当前窗口即便无阻立即回复，首双J准备前的乐观上界约51仍小于原严格70预留。仅能证伪“本窗口等一下就能反击”的前提，不能覆盖被淘汰的中段或据此修改生产平衡。先建立被动资源时钟与可达性合同，再形成战术交接方案，不猜第四套操作、不增加900预算、不改用户指定的敌人角度优先。

正式美术分层、连续动作、UI/音频/演出、完整真实主线/探索、历史资源根因、目标硬件与用户试玩和版本交付继续按[原完整待办](2026-10-18-project-remaining-backlog-v001.md)推进。目标不关闭、不缩小，NOTSEEN不变。
