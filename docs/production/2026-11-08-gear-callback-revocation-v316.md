# v316 · 齿轮回调撤权与实际多形状接触

## 事实与范围

上一轮v315是有生产修复和原生证据的progress，不是等待。本轮继续原T03的两项覆盖欠账，不缩减原十一项。唯一工程为`G:/knight/Knight`，只无窗口，root独占C#／canonical／build／native；独立子任务仅只读。

新增受控测试使用真实保存Player、native Player层查询、真实GuardedDamageReceiver，不直接写HP或伪造命中：

- 单个Player额外两处根Body形状，native预查询确认同一ID至少三个不同shape index；InvulnerabilitySeconds=0排除无敌帧掩盖。quarter sweep及后续接触只应扣1血、ContactSequence1、Damaged1、Contacted1。
- 双保存Player不相交、自然支持于fixture地面，full-period native预查询明确命中两个不同ID，不假设命中顺序；原HP各5，无敌帧0。
- 原8组moving controls全部保留，另测live／queue／remove／hide／disable-collider／stop-physics／pause／monitoring-off／pause-contact共9种同步回调。pause两例将两个Player和齿轮都设Always，故暂停不能靠目标CanProcess=false偶然挡伤害。

## 两次真实RED及最小修复

1. `thruster-moving-gear-20261108-101742-647-690bff9d76934c3bb154e7a68a55802f`，engine1／3107ms：原8组、多shape、live／queue／remove先通过，hide仍发布Contacted1，精确断言严格失败。
2. `thruster-moving-gear-20261108-102044-084-cd27cff4be0546528d4b5e80bb1cfea0`，engine1／3944ms：原生产代码下全部9场景执行，hide／disable-collider／stop-physics／monitoring-off各错误发布Contacted1；Damage-pause和Contacted-pause均真实伤害第二个Always目标，totalHP8／sequence2，不是只读检查或预期模型。

两个原始失败均保留，未加named waiver，guards=[]。第二RED的旧replay检查重复比较已错误的首批基线；它附带的同名失败不能证明下一次调用产生了新伤害。随后把replay断言改为调用前后真实HP／事件／sequence快照，未改变上面的首批RED条件，也未把这些重复失败当第二类游戏bug。

根因：`CanyonFixedGear.ProcessGearContacts`伤害回调返回后仅复核epoch／OwnerLive，下一body前仅复核epoch／危险getter；完整的pause／处理授权只在入口查一次。同步回调可能改变这些条件，旧batch不能继续沿用入口资格。

生产只改同一方法两处：在已保留epoch／OwnerLive短路之后，复查当前IsDangerTelegraphActive／tree.Paused／CanProcess；下一body前也采用完整当前门。已提交首笔HP和ContactSequence不回滚；若Damage回调撤权，立即停止Contacted及后续body；若Contacted自身撤权，保留首笔已提交Contacted1，但停止下一个body。未改伤害、cadence、动态轨迹、static危险getter、地图、Tuning、能量或存档。

canonical只追加两条新成功marker的精确一次要求，不删原marker、三日志否决、限时或保护。新测试在test utility中，没有向生产增加测试cleanup方法。

## 专项GREEN

`thruster-moving-gear-20261108-102309-366-083d14bd890e4c9984dd85d701b33bec`，engine0／5813ms：模型、原8组、实际多shape、9种回调、Fog及static gear原生控制均严格通过，guard空，无原生诊断。

live两人各4血／Damaged2／Contacted2／sequence2；所有Damage回调撤权恰有一人4、一人5、Damaged1／Contacted0／sequence1；pause-contact则Contacted1，其余同。每项撤权后的直接完整周期调用均保留调用前快照。queue在deferred free前确认排队，remove确认已离树，其孤儿gear在fixture释放借用shape之前释放；新query／capsule为测试自有，未Dispose场景借用形状。

同版旧机关 `recycling-traps-20261108-102525-395-68754a1783b8401691e486e0e5e4a943`，engine0／26178ms，原固定齿轮／弹板／组合／雾及mounted guidance控制和退出清理均严格通过，guards=[]。

当前1907输入manifest `7687513C469164131A7767B4965835CCDE2C02C4BBFB02D9E3C1F1016BEAC94A`，六产物manifest `B14A1C25A0E9E79744F8E724BB89D5B0C3E9E5958887941C530306D8C840F7C6`。其后的整体验证须同版本，不借v315或v314二进制签收。

限定独立审查C0／I0／M0，无需修补：生产diff准确两处，tests新增区外与v314一致，replay相对第二RED仅换真实快照，canonical准确追加两个marker；三文件与GREEN冻结副本一致。审查未运行native、不代签路线或长期资源。直接Free／同回调reparent没有新增单独原生用例，不能把epoch源码推理冒认为实测；短暂撤权再恢复仍开放。既有两项Minor已得到上述真实覆盖，其余fingerprint范围及离线夹具路径Minor不冲销。

当前普通键序Main `thruster-route-main-diagnostic-20261108-102735-618-85c9eb61038e4ad89b1b89a69926be0c`，engine0／251629ms、guards=[]；原输入无需调整。完整详细路线与存档读回由独立只读核查，Explore同版开始后仍需终结结果，不将Main单项当双路线通过。

Explore随后终结：`thruster-route-exploration-diagnostic-20261108-103252-849-dd9aef10d9bd4010b43a5a31563b385f`，engine0／324245ms、guards=[]，同样普通键序无改、headlessNormalPlanDiagnostic=true而非正常显示。详细raw及实际磁盘存档复核继续独立进行。两条当前路线不以旧版结果替代，但也不外推全图／76配置位或真人。

Main独立只读复核15项差异0：source1907／six-bin／helper逐项与本机相符；真实Title焦点ui_accept，单一PID及同一Player到Boss／出口；BossDied／arenaDefeat／completion各1、恢复0／0。实际40×72 body越出口右边2.7603px，slide support确为原ArenaFloor；完成后的60次held D检查姿态／body／velocity及存档字节不变（编号差61不是60）。14242对连续Motor采样和同版源码支持21次普通跳全grounded、无coyote／buffer／assisted接受，2个unsupported边沿被拒绝。6个唯一fragment、无豁免，门禁三面状态及左侧通行成立；不是all-walls。实际磁盘save89899字节、SHA `0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517` 与completion／held一致。无每帧raw的部分由核过源码的连续断言支持，不说离线逐帧重放。

Explore独立18项核对差异0：source／six-bin前后同一，三日志零诊断、stderr空；真实Title→Start、单一PID42716及同一Player到终端，不能因为两进程的ID数字相同就称共享一个实例。Boss死亡／arena defeat／completion各1，恢复0／0；实体body越出口3.191px、仍支撑原Floor。完成18140→held结束18201，60wait的pose／velocity／body不变，实际save90050字节、SHA `24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24` 与双次声明一致。18111对Motor采样支持33普通跳全grounded，coyote／buffered／assisted接受0，8个unsupported边沿未接受。11激活／10击败，配重遭遇未击败，非全清；最终7片，但pre-Boss是6片，额外temple-upper是后续所得，不能写7片准入。mount_gaps=1保留：源码只允许所有门仍近侧、无穿越历史／授权／准入壳历史的epoch重挂，不是任意缺帧豁免。bell穿越2不是触发两次，准入壳三面实测且左通行，不签all-walls。

同版普通pre `prologue-pre-20261108-103918-729-9e604f5328114b12bd32156f937a08c7`，engine0／145686ms、原44案例／退出清理严格通过，guard空。本轮未复现旧句柄错误，不称根因关闭。后段和保存相关阶段仍须独立终结。

## 合并兼容检查发现并修正审计遗漏

768751旧测试产物的post `prologue-post-20261108-104240-314-716c4c4c3aa145c489809135a64fb671`，engine0／195058ms、原75项和退出清理通过；仅原四精确负例警告，沿用300s、不签旧180s性能。models `models-20261108-104730-972-1fadaa885c62418ab9958f24f4743400` 0／509ms及typed recovery `thruster-recovery-compatibility-20261108-104847-739-ff60e8e62c13489bb2b9dca8e27c5874` 0／16787ms亦通过。

额外resident-save案 `resident-save-compatibility-20261108-104806-686-d782fa9df90841a6a6531bd7171f5f68` 是真实严格FAIL／engine1／8591ms，不被外层batch掩盖：实际Quest保存／死亡／Continue先完成，随后Annex地图迁移的factory审计发现Basin实际61个形状、旧清单60个，唯一extra为`Gameplay/Mechanisms/UpperGalleryMovingGear/Collision`。stderr／external各一个同一断言，Handle0／warning0，不是存档损坏或GCHandle根因。整个batch退出1，所有失败保留。

核对`CanyonContentBuilder.cs:AddUpperGalleryMovingGear`确为直接new r24 Circle后，才只改两个测试CS：审计追加精确路径／CircleShape2D／radius24，将Basin改为61路径／54矩形／7圆，其余区不变；没有删除精确集合、类型、尺寸、独占native ID、空ResourcePath及借用排除门。新增4个真实坏输入（r25、Rectangle代Circle、未列extra、借另一个fresh donor的空ResourcePath圆）必须拒绝，所有控制后原61资源ID／r24精确恢复。临时形状先脱离节点再由测试owner释放，别处donor资源不被本donor释放。没有改生产、地图、catalog字节／revision或存档wire。

新GREEN `resident-save-compatibility-20261108-105404-042-09b48488cedf4d24bdb6c565f815ea6e`，engine0／12382ms、独立minimap cold reader0／1593ms、guards=[]。真实收集持久化／恢复／Continue、旧版本权益与地图迁移、20个迁移负例及新4个精确审计负例均通过；收集是metadata受控夹具，不冒充contact pickup或正常路线，冷读亦明确是该隔离兼容资料。

**最新测试输入已变化**：1907输入manifest `7BD49E8EC23C81D3019EF7FBD86AD2888E650D81C2A18033F977B368CFAB1AB1`、六产物manifest `FAC8F585F365EE6161BDD5BCEA4F1B60F0E291221F9C7D6BECE0EB4AC8F90CC6`。生产齿轮修复未再变化，但不能把768751旧测试程序集的Main／Explore／pre／post不加限定拼成这份新产物的完整同版验证；最新完整合版复验继续待办。

审计新增子域独立review C0／I0／M0：来源、路径／类型／半径和四种坏输入准确；失败时out数组仅为同一资源alias，不另分配形状，不应重复Dispose；原owned数组保留被临时detach的原circle。此review未重审旧gear或代签native。新save案父PID39712与冷读PID20348明确不同，冷读三日志零诊断、6个witness和稀疏／全亮／全暗／旧版链均只读通过；不是两个进程共享一个live实例。

最新产物退出后再跑三个专项，均strict PASS且source／six-bin前后与最新7BD49／FAC8完全相同：

| phase／case | engine ms |
|---|---:|
| models `models-20261108-105711-587-3fc9f2a6847a4d3e92d1c161ca67a919` | 521 |
| gear `thruster-moving-gear-20261108-105745-036-b061f2b7949f48e0939178cc37d57c8a` | 5665 |
| recycling `recycling-traps-20261108-105829-709-a831831b1e5c4c9885a932cd36730fb2` | 25912 |

连同新save是**最新同版4专项通过**，不是把先前版本合并成“全套通过”。各案failure=null／guards=[]，三日志零诊断；受保护真实用户存档清单均为`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`且前后不变，helper清单前后亦相同。根最终逐项读回当前1907输入／6产物／9helper，零差异，无owned Godot／dotnet／MSBuild进程仍在运行。

本轮没有新过程图片或视频。正常汇报只运行一次既有媒体`-Check`，15,203,531字节／145文件、Notify=false，未达新增20GiB，没有自动化、单独提醒或停工。所有原始失败与两版成功案例保留。

下一步先对最新7BD49／FAC8做Main／Explore及完整pre／post的同版合并复验，再推进选定美术路线；moving-gear Task3及原十一项仍open。历史句柄根因、圆弧悬吊款、正常速度声画／硬件／真人及正式美术不因上述程序检查完成而关闭。

当前敌人家族盘点与脚图适配候选已存入[美术现状记录](../art/2026-11-08-enemy-family-current-state-v001.md)，明确未制作／未接入的边界，后续无需凭聊天记忆重新猜测路线批准。

## 边界与后续

上述修复处理回调返回时仍失效的source；同一回调内短暂关闭再恢复，没有新增epoch追踪，不声明任意瞬时切换都被捕获。多shape检查证明至少三native形状的用户可见单次消费合同，不能单独证明两层去重各自都不可替代。Fixture初始位置／无敌设置为受控条件，不是正常玩家路线或正常像素验收。

现有家族外形也只读核实：骨骸／幽灵仍共用同职能旧renderer，幽灵游荡只是1／.3透明度，不是已完成独立造型；24开发叶仍是ExplicitPlaceholder，不能改标签冒称FinalOnly。已向用户补问下一阶段先程序样段还是先分层稿，未自动批准或接入。另定位既有V05远程FootTexture居中适配悬空的候选，本轮没有修改它，更不把它当家族造型交付。

没有GUI、系统输入、付费生成、新native dump、引擎／cache／GC／预算更改、真实存档实验、Git或删除。完整十一项与正常视听／硬件／真人仍开放；资源句柄历史故障未因本专项绿色而关闭。
