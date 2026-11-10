# v179 — 首段抓柱正式接入与探索地图记忆兼容

项目仍为 `G:/knight/Knight`，原十一项完整序章目标ACTIVE。本轮先由实际缺失迁移的失败推进到兼容通过，再将验证过的两份生成场景写回；不把局部兼容当作通关或正式美术验收。三误发视频继续作废不用。

## 已写入的实际内容

- 正式生成器仅改 `ShortcutStep3956Grip`：世界矩形由 `(4214,3816,36,60)` 改为 `(4214,3724,36,152)`。底部3876、下方80px通道和所有其他地形不动。碰撞、skin位置/尺寸、ReadableEdge位置同步；顶部56px缝隙不是72px角色的通行路线。沿用现有视觉资源，没有付费或声称完成美术。
- 原角度优先、近敌规则、620射程/禁抓/遮挡、原16/32/300预算和实际输入保持不变；路线仅将依实际柱顶计算的精确逻辑aim断言从 `(4232,3826)` 同步到 `(4232,3734)`。
- 小地图依实际五区新物理生成目录：143 solids、518400 cells、12460 boundaries、354696 bytes；revision `BEE5E8069591E12D83F10EB0477FFCA3CFEAC1A314DE6C6C958F7E257271E3C2`，完整值SHA `566F599CC762B2EBC7D6D3BAC94628422C61565B5D6081F77DFD5B2661E1F8B4`。

正式文件来自外部隔离case中已保存、reload及语义检查的生成产物，不直接重生成整个Basin/parent。Basin只放行5个存储字段：柱body位置/shape、skin位置/SkinSize/ReadableEdge位置；parent只放行Catalog。其余存储值、metadata、groups、persistent connections和嵌套/外部场景链接全部比较。发布前同时检查两目标仍为案内原SHA，以及两产物SHA，才固定两文件复制；发布后再逐SHA确认。原件已在原case before/after保留。

发布源 `resident-save-compatibility-20261030-203331-526-bd9205246579450f822f1a5b696524a4/storage/`：

| 正式文件 | 发布后SHA256 |
|---|---|
| `scenes/prologue/sectors/CanyonBasin.tscn` | `9054F91CAFF1E37AFCAC43C317FB365B061F2592D63E9DBC633EDCB7AA117839` |
| `scenes/prologue/PrologueLevel.tscn` | `5E6AC96A644281846A1B979F154C59E3485B54B07E18EEC68C8C9205C1A201F5` |

后续case再生成的tscn字节可能因序列化ID而不同；未重新复制，不混称同字节，只据各次语义/物理目录检查判断。

## 旧地图记忆

4802→BEE5只遗忘新柱footprint及接触边界的120 cells（cols526..531，rows465..484）与该owner的边缘。旧7348..7373舍弃，之后边缘+24；新7348..7397不凭空发现。全满旧图保留518280 cells/12410 boundaries。

更旧B264先保留原shelf48转换，再接新柱转换；两改动区不重叠，全满保留518175 cells/12366 boundaries。仅在新目录revision、全量SHA、网格和数量同时精确匹配时才迁移；未知source、畸形padding、缺边缘、伪造target均拒绝。旧4802历史值在新parent发布后由字面旧26段反向重构，并核实完整原SHA；旧B264原48段字面与28负控保留，不将旧期望改成新图。

真实隔离文件覆盖 sparse/full/zero、B264 chain、完整核心字段、原source SHA不变。独立不同PID冷读覆盖旧4802 sparse/full/zero与既有旧三证人。**B264→BEE5串联矩阵目前只在writer进程验证，不能称独立冷读串联已通过。** 新旧source读取从未写回；不接触真实玩家存档。

## 本轮原始验证（后续原生结果继续补充）

证据根 `G:/knight/验证证据/october13-stable-claw/`；原cap不变，失败全留存。

| 案例后缀（前缀resident-save-compatibility） | build / writer / cold ms | 严格结论 |
|---|---|---|
| `20261030-202456-530-2f2280112b8c4512a910a9b200dd62b4` | 49472 / 18586 / 未启动 | 实际POST_MAP_MIGRATION_RED，FAIL |
| `20261030-202814-095-1f27b04592ef460aba57ebcf6246e676` | 48327 / 18150 / 1894 | 兼容GREEN，未加scene staging |
| `20261030-203156-689-8fdeb2b2bbb842039c16ba5820c1a0b1` | 未启动 | root误填工具路径，输入预检FAIL，无初始完整清单/成功binpair，不造行为RED |
| `20261030-203331-526-bd9205246579450f822f1a5b696524a4` | 66243 / 22693 / 2123 | stage＋writer/cold PASS，随后受控发布两文件 |
| `20261030-203912-413-9e8de11798d449c2b790472826486f52` | 29112 / 15220 / 1347 | 发布后逆构造、实际目录、writer/cold和stage PASS |

- `basin-claw-target-20261030-204214-643-ccbb0e0308dc4e6d81fe32b68d7a59e9`：build34977/native1855ms严格PASS。明确重建历史60高负控，正例使用保存的152高柱；五个旧真实姿态选中当前柱，native capsule离散净空、原wall K牵引/Ice仍通过。新姿态查询不是实际跳跃落地。
- `resident-parent-20261030-204403-609-7d0e3ab4b21f4a8e8922b1e02ba1ff99`：build31962/native13229ms严格PASS。实际五个保存区、生成器与嵌入目录一致，143solid结构及原有链接/父场景检查保留。
- `thruster-route-exploration-diagnostic-20261030-204553-987-f23438270fe94855b9df0c87bcfabfac`：build33041/native247476ms，严格FAIL。**首段上行实际通过**：12/16选中；原K牵引到达82/300、撤爪落稳111/300，玩家同ID、HP1、零恢复/零dash、一普通跳不变。实际落点 `(4162.6133,3919.9663)`、完整3956台面支持；随后3796、3636台支持也通过，不是仅仅能选中或孤立净空。之后到院落普通转移时在physics13847被 `canyon.v2.resident.sealed-courtyard.01`/precision-laser/actor28377738119886/attack1击杀，HP1→0触发恢复；完整路线不通过。
- 致死激光13772真实Telegraph→13847 shot；同actor/attack的接受伤害归属已记录，fall_damage0、shieldHeld/Active=false、heat0。日志 `energy=1` 来自 **EnergyRatio**，不是“只剩1点能量”。旧战斗决策age3345不是本转移的新危险许可。普通Task4ReachSupport无危险/应战消费者，不能据此判激光生产机制有错；下一应让真实移动阶段响应已存在的resident威胁，不先削弱敌人或补HP。
- `models-20261030-205201-475-05656253a40d4a1482f82f5a2a5d1b64`：build92906/native907ms严格PASS，仅机械爪模型子集。
- `prologue-pre-20261030-205526-679-8bcd549f806f49dca9751610a6dda2f2`：build35566/native176202ms，原180000cap内primary0/前段执行结束，但 `Handle is not initialized`/SwapGCHandleForType 错误使其**严格FAIL**。实际调用点为 `BasinClawTargetTests.RunFirstAscentLayout:55` 实例化整Player以复制capsule；同类错误历史存在不证明本次无关，根因未确证。下一去掉此不必要副作用、改从保存资源读取真实胶囊并重新验证；本轮不冒称已修。
- `prologue-post-20261030-210120-007-9b6bdd1d61f9493cb45611c4e9ca5482`：build32928/native207623ms，原300000cap内严格PASS，原后段合同及限定故障观察器warning策略未放宽。

## 审查和下一步

一次只读审查 C0/I0/M1，未需阻断修复，无二审；M1为上述串联冷读缺口，延期且明示。审查不认证原生运行或正式画面。

正式图声/动作/UI/性能/硬件和用户试玩、完整Main/Explore、原生前后段与历史GCHandle/Finalizer问题仍OPEN；普通Main旧版能量0死亡问题未修，不据局部地图修改假定已消失。无新窗口、OS输入、付费、崩溃采集、Git写入、自动化、删除或新媒体；目标不缩减，不标完整完成。

本轮当前Main未重跑；原生前后段不是全绿，不能合并局部PASS成完整资格。正常速度像素、当前用户游玩认可仍没有。

## 最终保管与继续入口

11案中7案严格PASS、3案原生严格FAIL，另1案input-scope前置失败；10次成功构建、10primary＋3cold共13native，全部原cap内终结。Post实际原75 enter/return及owner8齐全、原4限定WARNING，未扩大豁免。

独立核对50组清单对/4848份已配源实体/104份helper实体/60bin逐SHA一致，案内零差、未列/哈希错误0；前置失败仅有after部分172实体，无对应初始清单，单列、不补造。23个自有build/native PID均已结束。当前1869源/6bin/post实际10helper匹配末案；两正式scene SHA仍为203331发布值。

HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 不变。2026-10-30T13:11:34.5201331Z手动一次媒体检查7427750bytes/86files，基线3508，Notify=false；无自动化，不因20GiB政策停工。

下一具体入口：复用 `tests/diagnostics/RunDetachedEnemyResourceRepro.cs:215` 的SceneState保存图读取方式，精确读取根直下BodyCollider的实际Shape，只拥有Duplicate，不释放借用PackedScene/SceneState/Shape；对缺失/重复/继承/错误类型/非identity或disabled明确拒绝。它只排除不必要Player实例化路径，不证明历史GC根治。随后让院落普通转移实际消费已有危险并合理应战，保持全量敌人和原物理/预算。原目标ACTIVE，非交付。
