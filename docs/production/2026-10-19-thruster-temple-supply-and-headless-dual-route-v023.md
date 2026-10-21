# 神殿补给架与后台完整双路线 · v023

2026-10-19，Asia/Shanghai。完整用户1–11目标 ACTIVE，未完成。沿获批Task7继续迁移旧路线消费者；Task1–5专项不重做，Task6资源／正常视听及最终内容、交付仍OPEN。本轮未修改生产Player、敌人、地图、美术或真实用户存档。

## 事实、推理、风险

事实：保原控制的诊断确认补给架失败发生在第一次硬检查：完整胶囊右界5934.666超过5930，vx50；其余已执行子项均通过，DoorClear因原短路未执行。只迁移入架键盘控制后，十二次原硬检查、残片6→7、一次保存替换、一次+2治疗和非消耗返回都真实通过。随后Explore及同冻结Main分别完成真实Boss与有支撑出口。

推理：旧跨目标松D加六帧尾部不能满足新摩擦下的站稳要求；应在真实完整站上原补给架后，按实际不可变Motion／Module的地面Neutral预测发物理A／D／松键，不改变生产力学。后台完整路线通过仅证明这两条自动操作可达，不证明正常显示、真人手感或所有旧消费者已迁移。

风险：候选第一次运行最后记录至更早Grip4后出现匿名gchandle FATAL，实际崩溃阶段／tick未知，严格保留；同源一次有限复验成功不能关闭该故障。正常窗口旧Explore死亡来源、legacy高层中途碰撞、音频2 ObjectDB、11文件剩余消费者、五历史严格失败、新50正式0/50、24canonical与最终地图／美术／UI／声音／存档组合／正常60FPS／真人／ZIP／GitHub仍OPEN。没有付费生成、新媒体、自动化恢复或发布。

## 六个原生案例

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。均headless，fresh Rebuild退出0、0警告0错误；source1695含296 imported，不是1695个C#。各无超时，guards=[]。整个owned batch及阶段间隙冻结source／helper／docs，全部终态后才记录。

| case目录 | build ms | native退出／ms | 结果 |
| --- | ---: | --- | --- |
| thruster-route-exploration-diagnostic-20261019-041012-031-d720f17c249d432c88047585cb658b1d | 28046 | 1／219446 | 原控制诊断RED；补给架第一次硬检查右界失败 |
| thruster-route-exploration-diagnostic-20261019-041621-645-471fed13b3724683910cde9818ef0dbc | 26332 | −1073741795／129541 | 候选未到补给架，匿名FATAL，严格FAIL |
| thruster-route-exploration-diagnostic-20261019-042108-583-55f648ecd1484e5caac7007dce740b6c | 28292 | 0／279581 | 有限同源复验；Explore完整GREEN |
| thruster-route-main-diagnostic-20261019-042626-379-94c0d49235cf4f299d209f515f8b18ee | 36753 | 0／198834 | 同冻结Main完整GREEN |
| thruster-free-forecast-20261019-043031-968-11f10f53306e420d90d88d91edec8097 | 29081 | 0／669 | 独立离散预测／旧2800负控GREEN |
| ranged-steam-missile-policy-20261019-043108-991-8be1931866fc4519951c5b9c4cc93c4c | 27061 | 0／7253 | 独立导弹策略基线GREEN，旧停距消费者尚未迁移 |

这里有两个冻结，不按4/6计算目标完成率。候选FATAL与后四案源码及实际六binary相同，不回写失败或以重复运行次数认证可靠性。

## 补给架：原控制诊断与最小迁移

041012记录30/96行，omitted=0、diagnostic_faults=0。仅缓存标量／字符串，未增加await或观察查询：记录实际原Supported射线，保其短路；未执行的射线／DoorClear明确not-evaluated。不能以六帧预测prefix当最终停止位置。

- 原Walk第15帧13061松D，x5898.3315/v260；六neutral尾分别230、200、170、140、110、80。
- 下一13068硬检查index0为5914.666/v50，右界5934.666>5930。三条原支持射线、左界、plate点、提交者为同Player、HasOpened／IsOpen／Traversable／DoorDisabled均true；仅已执行right_inside为false。
- DoorClear保持原短路未查询，indices1–11未执行。残片6→7、feedback1、saveReplacement1已真实发生，但没有治疗／返回／Boss／出口，不能提前称补给完成。

仅`Walk(5896)`的keyboard入架域改为：空中及部分站边时保原D；实际全胶囊且三条射线命中原架后，使用Motion／Module地面Neutral终点和±1.5内带发A／D／neutral。预测取原Walk90余额，要求fresh neutral／raw A-D关闭、完整原架支持、原目标与速度≤2；六尾只验证，不用来纠偏。原等待90、十二逐帧硬检查、唯一K／Space、几何／门／plate／HP／伤害／保存全部不放宽。其他preciseStop／pointer／回返旧预测不冒称已迁移。

042108实际13057全支持x5885.331/v260，13058反A后v193.333，随后neutral；原Walk90实际20样本至13065 x5897.386/v0。六尾均v0；13072–13083十二次硬检查全部通过，36条原支持射线与12次实际空DoorClear逐项通过。记录158/256，无omitted或fault；新增控制支持查询明确标记landing-acquire／landing-control-support，后者51条射线，不伪称其无额外控制查询。

同Player完成残片6→7、feedback1、saveReplacement1，HP1→3仅一次+2治疗，overlap／persist各1，返回BodyEntered仅一次且不消耗残片；13210完整返回原lower／v0。jump33、dash0、恢复0。HEALED时x6025.3354/v80，不能把它称为所有Walk都已停稳。

## 同冻结完整路线资格

跨进程ID相同不代表同一原生实例；以下身份只在各自进程内关联。

| 项目 | Explore042108 | Main042626 |
| --- | --- | --- |
| Player／Boss | 46858765828／1080721150063 | 46858765828／469174849431 |
| 最后真实J／致命伤害 | attack81，16378→16382 | attack69，11504→11508 |
| Died／arena事件 | 各1 | 各1 |
| 支撑越门完成 | 16561，166/188 | 11687，166/188 |
| held D60结束 | 16622 | 11748 |
| 冻结位置 | (7341.191,1079.9327) | (7340.7603,1079.9331) |
| activation／defeat | 11／10 | 8／8 |
| 恢复 | 0 | 0 |

两案完整身体、原地板支持和越过右门均实际验证；held D60期间位置／body／save bytes不变，completion事件与count各1。完成时velocity260保持冻结，不写成速度0。Explore的11activation／10defeat不证明counterweight encounter被击败。

Explore实际Missile628608733036、Laser871089834807各telegraph1／shot1／damage2／connection2／defeat1，22条raw与receipt一致。Main的Scatter207366393235、Burst529354722098、Laser746200240407分别telegraph／shot为1/1、3/6、1/1，各两次真实近战伤害及连接、一次死亡，46条raw逐字段匹配。不是外部注入伤害或禁AI路线。

| artifact | 字节 | SHA256 |
| --- | ---: | --- |
| Explore `storage/task10-route-result.json` | 4950 | 3A4D6B86304F259926B161D086AA6D1DB61CC195C31E699E63F1EB8F80F665F9 |
| Main `storage/task10-route-result.json` | 4350 | BCD6C6D73C69D1AD335DBCD769CC6DABC212768D0AC4257EA00D385EDF71E8E9 |

隔离save实际哈希分别`ED26388E9B3EA64F25B107C8C6DC9402F3B5249B40DEF8A476E86B7C1E32ADBA`与`E4357B681B2DB8CE2A49F615BB9A6693419AA72EAF5F52D03586C57C6FD441F6`，与artifact相符。human／normal_display／performance／whole_goal均false；旧真实用户存档语义未读取。

## FATAL与资源边界

041621 native exit为0xC000001D，不是普通engine1。stdout最后是Grip4 travel-clear physics7323／cast16／HP1；这不是实际崩溃tick。Temple／Supply／诊断／音频drain／持久化成功／路线完成marker均未出现，不能归因新入架控制已执行。

stderr保留`gchandle.is_released()`、`mono_object_disposed_baseref:1788`、`GodotObject.Dispose:137／Finalize:100`、`GC.RunFinalizers`及CLR Fatal error。无对象ID，归属未定。五日志行数7/0/827/11/833；external收stdout与stderr前6，CLR后5仍完整在stderr，不称全合流。一次同源复验目的仅验证候选能否到达，未加GC／cache／drain／清理等待或Dispose豁免；安静成功不关闭间歇FATAL。

窄静态审计发现Grip4路径有GetSlideCollision的using和Array泛型转换处；离线PE/IL确认GodotSharp Array<T>显式转换返回其同一个_underlyingArray，不复制。GetSlideCollision桥接返回包装器，但IL／XML不证明独占、转移或重复getter身份。以上不是错误Dispose或FATAL对象的身份证据，未据此删Dispose、延长持有或修改引擎。对应原GodotSharp.dll SHA为`7B1D06575DB78E4EA4353186BE5ADD207E43617B836DB24D65EC4088E6FAD18C`。

## 导弹独立基线与剩余消费者

043108 HIGH真实4J后8个D/noL早期预警样本，gap减少21.444427、HP5、energy.52、shot0；随后返回，不是HIGH击败证明。LOW真实7J后，在实际≤.35能量Active阶段前向D+L挡爆，shot1／guard1；两fresh J精确命中同Hurtbox，HP2→1→0／Defeated1，玩家HP5、末gap60.261536/v0。8..64为实际StrikeRange，不是距离穷举；不证明正常group／batch。既有finalizer-count1／EXIT_DRAIN不能关闭资源故障。

该夹具Drive仍`v²/(2*legacy GroundDeceleration)+2tick`，GREEN不是迁移完成。只读消费清单还包括：TraversalRoute的fixed60 PredictDetachedLandingX；SteamDiverter的地沟停距／时窗；PreparedShieldLane空中地面停距；BasinReturn、FragmentReturn、NaturalFragments；GeneratedMain的Boss站位、ExitSteamTrench、LowerBellAndReturn、counterweight suppression及旧Task8/9 PredictLandingX；TempleSupply其余preciseStop。此为源覆盖缺口，不冒称全部是新原生失败。

已正确采用actual Neutral的Sentry／BellBolt及mirror站位、燃烧排除、合法抓爪环境授权和旧兼容负控保留，不一刀切删除。下一先为导弹地面消费者增加独立可区分的行为证据，再最小迁移；legacy碰撞与匿名资源身份分别处理。

## 保管链与保存位置

各四对source1695／保护109／binary6／helper清单及continuation前后一致，六binary实际副本匹配。路线实际selected为213=208source+5helper，已显式保存TempleSupply partial；forecast166，Missile175=169+6，入／退出实际副本逐项匹配。Missile helper10较其余helper9仅多既有ranged-red-admission，不能称helper清单完全相同。

| 冻结／清单 | SHA256 |
| --- | --- |
| 原控制诊断source | F5CA29C7FADF2FACC8D1D0BE67183967692FCD9DCD1AAB985F7E14BDF3FCCBC1 |
| 候选、复验、Main、forecast、Missile source | 0F97F2997026F7A8D889C8E6626D39A8213A0AA8A3B9837626E93A417BF89EE9 |
| 当前路线／forecast helper9 | 46DE6E4A4070BCDDCB7C9E3BA600EF7F169724AB1F76A4C954470DD50F9E327D |
| Missile helper10 | 0DD32496F63838A478350D0EE512D81C153E34FB5EA4522B20092B08126A1BF3 |
| 后五案binary6 | FD43B675F634B3B5F6C07FBB8F786E2C39D44628B018A7ECAE31345B9E24CC51 |
| 候选及后四案TempleSupply partial | 11A5CE5D9284ECB8FC5FD1DAF756746B099706D61AF28B3FD9590B5E3915FBC3 |

后五案DLL `E85E44FCB47573B021D19A38D84427831A8958CC588EDEE4D67D506EF9633898`，PDB `71D634DB17B20BDAE3CE2BC302A3A3C46EB9421B6DC7328F21F2441EC0951E5B`。正式Player仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。

原控制诊断仅对应行为ERROR；候选保留上述FATAL；其余四案20日志无ERROR／WARNING／Handle／FATAL／ObjectDB，external逐行匹配stdout。安静退出不是资源修复。本轮没有正常窗口／OS输入，之前前台时段已释放。

全部owned消费者终态、CIM无owned后更新本报告／README资产表／总账／ledger。E空闲5481213952字节约5.10GiB，F约26.85GiB，G约356.09GiB；未来新增证据存G的询问未答，不移动／删除旧件。技术证据在上述证据根；展示图稿与录像仍按`E:/OurGame/Knight/过程图稿与视频`归档，本轮无新展示媒体。

完整1–11目标保持ACTIVE；Task6/7、正式新50与全部最终内容／资源／正常显示双路线／真人体验／验收交付仍未完成。
