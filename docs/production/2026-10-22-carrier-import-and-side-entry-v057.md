# 子场景私有导入与侧壁复验 · v057

2026-10-22，工程G:/knight/Knight。完整1–11／Tasks1–4继续ACTIVE；不是正式试玩或最终美术。用户当前问Godot／UE取舍，未授权或执行切换；本记录只收尾此前已运行的工作。

## 事实

用户批准的侧壁候选普通复验通过：GearLeft111639-206-8b7fefaa，native11972／exit0／19206ms，289观察／580physics；同Player抓壁、既有借力越角、完整落左棚、原活塞出口，2casts／2Arrived／1Cancel／1Boost，HP5→5，无后置改位／治疗／dash。机关关闭、无常驻占用，不代表正常战斗或真人容错。

诊断104216确定位到RangedWeaponSettings finalizer FATAL的canonical carrier来源；另104544安静诊断首次Alive=false在old36-begin行1564，早于finalizer248 ENTER行1584，不能反写成finalizer导致原生失效。跨进程相同数字也不是同一对象证明。

新增只读标量诊断不增加Load／Instantiate或保留Resource。原生110243-7853f10f的新增导入合同实际RED：首个rock场景cache false→true。仅将BuildResidentSubtypeScenes carrier改为自有IgnoreDeep PackedScene lease，actor.Free先于lease.Dispose，无借入Settings／shape Dispose、GC、缓存驱逐或引擎补丁。110533-7e39e128诊断exit0／26168ms，24唯一根、24条cache false→false；不是普通玩法或全部Handle修复。

保存语义检查复用既有original／copy，仅允许根新增一个Binding、外层SceneFilePath变化，其余节点集合双向一致；检查根／后代类型、Transform、嵌套链接、碰撞层掩码、shape类型／边界／启用状态及全部23个Ranged Export值。24保存／迁移／Ready和7负控完成，但退出136ShapeRID仍严格FAIL。旧同用途054758已有154RID，两版本并非单变量实验，数量下降不证明修复。

新增测试曾错误using当前API不支持IDisposable的Array<Node>，111125／111149／111212为CS1674构建FAIL，无engine，非行为RED。已核验并停止自有dispatcher18464，孩子结束后改为GetChildCount／GetChild；普通构建恢复。失败原件保留。owner合同67控制、诊断准入24控制GREEN，未放宽原始三日志否决。

## 最终同冻普通13项：8PASS／5严格FAIL

全部1710源／880C#，普通编译无诊断符号；所有案无超时、guards0。

| phase／case时间与前8位ID | native PID | exit／ms | 严格结果 |
|---|---:|---:|---|
| scene-binding111348-61830116 | 52352 | 0／7939 | FAIL：136ShapeRID |
| allocation111448-acdca357 | 46360 | 0／473 | PASS |
| census111512-0dbc1b38 | 50380 | 0／61794 | PASS |
| GearLeft111639-8b7fefaa | 11972 | 0／19206 | PASS |
| Pendulum111724-3e071b6b | 35500 | 0／19750 | PASS，非历史FATAL关闭 |
| Frozen111815-3822b580 | 53016 | 0／5422 | PASS，非历史Handle关闭 |
| Archive metadata111850-6c68db00 | 22940 | 0／4112 | FAIL：255ShapeRID |
| Steam metadata111922-ac11c4cd | 41160 | 0／4484 | FAIL：266ShapeRID |
| keyboard111954-f594d385 | 31700 | 0／9570 | PASS |
| actions112028-f8dd642b | 18372 | 0／7062 | PASS |
| structure112104-0995cbee | 18372 | 1／906 | FAIL：Archive expected640／actual704 |
| pre112134-2316d859 | 29168 | 0／99454 | PASS |
| post112338-ec9edc85 | 27092 | 1／898 | FAIL：同Archive断言，后续scope未到 |

完整case在G:/knight/验证证据/october13-stable-claw，保留三日志、source／helper／binary／user前后清单与副本、PID／argv／wait／provenance。

普通census真实50唯一Ready／51身体，41完整有序闭环＋9静止炮台，720连续physics14→733、489次落稳走廊检查，healed0／error=null。JSON SHA256：13F8E648D99B3A69CBD1B6D7E860B869987431B50B0D9DE11B0473193BD4DE78。无目标、attackTested=false、formalSpawned0。

共同source清单94EFFEB5F2630D9CEC731F258FA0D512123366FD02356DF8F7DBAA4D52E7DBFA；DLL8F954EA934A84C9CCDED8A4D6285F6BA7EA761602ABB1F24E2976BBBDB37B9F1；六binary清单D5B6D7612ABAA9BC0D59461ABE8A99F9C38CE230EBBAF8D758AF5BDC36A19700。HEAD e7c70498…／index21610FDC…未改。

## 推理、风险与下一步

维护者记录缓存复活脚本资源的句柄问题，并建议可行场景用Ignore／IgnoreDeep；支持限定实验，不证明本项目所有故障同因。[官方issue83762](https://github.com/godotengine/godot/issues/83762)

4.7.1文本加载器在IGNORE判断前仍get_ref既有子资源键，故根cache不变不等于完全不碰子资源。私有导入也有内存／构建成本与共享语义风险，保存语义通过不等于退出泄漏已修。[官方实现](https://github.com/godotengine/godot/blob/4.7.1-stable/scene/resources/resource_format_text.cpp#L548)

正式127／候选143／新增正式0/50／全50Placement=null／24正式子型Unimplemented／新最终视听0均不变。旧26共存、50战斗／Returning、正常双路与Boss出口、最终视听、真人及正常显示60FPS／发行仍OPEN。不能拼不同版本安静案例关闭历史Handle、FATAL、族系或void RID。

后续最小资源反例候选：两个全自有图仅差是否Snapshot遍历PackedScene._bundled，只释放明确自有图／PackedScene／shape，比较预录ID退休；无Load／Save／GC／借入Dispose。尚未执行，不预判原因。Archive704不能全局抢占旧Abyss轴，正式同步仍被资源门阻止。

无新付费、自动化、Git写入、删除或媒体。手动媒体4838bytes／基线3508／增长1330／Notify=false；G可用203.29GiB。引擎问题只作建议，未启动迁移。
