# 旧小地图探索迁移 · 只读侦查，未实施

来源：`/root/minimap_migration_recon_v138`，只读当前源码及三份已引用的父scene。未改文件、构建、运行验证或读取真实玩家存档。此记录是原十一项P06/F03/F04的后续入口，不是迁移完成证明。

## 当前事实

- [DTO](../../scripts/save/ExplorationMapSaveData.cs)只保存formatVersion、terrainRevision、freeCellsBase64、boundarySegmentsBase64；没有旧bounds、坐标表、solid/boundary表或索引顺序。revision摘要不可逆，单凭旧DTO＋当前catalog不能可靠恢复旧目录。
- cells是8×8、row-major、LSB-first；boundary按solid/面/分段顺序编号。当前[codec](../../scripts/save/ExplorationMapSaveCodec.cs)及[model](../../scripts/presentation/minimap/ExplorationMapModel.cs)均拒绝revision mismatch；core先独立Normalize，再解析optional map。
- 旧实体仍在：`G:/knight/验证证据/october13-stable-claw/models-20261026-195230-215-a6f4588d04a642f39f33db5e375cc220/before/scenes/prologue/PrologueLevel.tscn:208`含完整embedded SerializedValues，物理父scene与原source-before清单相符。旁边旧CanyonBasin保存RightShelf中心1652、宽152。

| 字段 | 已核对v133目录 | v134及当前目录 |
| --- | --- | --- |
| 世界范围 | 0,0,7680,4320 | 相同 |
| 8px cells | 518400 | 相同 |
| solids | 143 | 143，仅solid48不同 |
| boundaries | 12440 | 12436 |
| changed solid边界ID | 6526–6573，48项 | 6526–6569，44项 |

旧revision：`B264EB610D985D63288925D90C9F239950A1D406AF7063E0BF5C470F8E62089E`。

当前revision：`48020BD483F79CC058185DB230C17608740B5FC43309E2F783F0C7C02B45CA15`。

两版boundary bitset都是1555字节，长度相同不能发现后续ID偏移。只按旧索引复制会错误揭示/遗失其他边界。

## 可行方向与边界

仅识别精确已核对old→current pair；其他revision保留现有fail-closed。保存并核验旧catalog值后，可用现有公开world cell/solid/segment接口做保守相交映射：只保留旧已置位、完全相同且不触及changed shelf footprint的world cells；boundary只复制owning solid与完整world segment都精确相同的已探索项。整个changed solid48可保守丢弃。旧未探索仍为0，不使用epsilon/最近邻/插值，不调用Advance重新揭示。

不要实例化旧父scene来“恢复历史”，其外部res路径会加载当前叶；只提取已核对的embedded catalog值并记录来源与完整SHA。历史物理文件与清单匹配不等于NTFS不可修改。不得信任任意自称同revision的输入。

下一测试入口：[ExplorationMapModelTests](../../tests/unit/ExplorationMapModelTests.cs)、[ExplorationMapSaveTests](../../tests/unit/ExplorationMapSaveTests.cs)、[MinimapCatalogTests](../../tests/integration/MinimapCatalogTests.cs)、[ColdReadTests](../../tests/integration/ExplorationMapColdReadTests.cs)。覆盖未变化已探索保留、未知保持0、changed footprint清0、boundary后续-4不串位、unknown revision/同长度伪造/Base64/padding拒绝、core逐字段不变、源文件hash不变及独立进程只读。只操作隔离测试存档，不碰真实用户存档。

迁移代码、fixture提取、冷读和当前版游戏验证均尚未执行，仍OPEN。
