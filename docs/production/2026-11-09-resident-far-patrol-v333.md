# v333 · 远距脱战后恢复作者巡逻

状态：生产仲裁已修，五项原生行为控制成立；最终同版13阶段11PASS/2FAIL，原十一回归子集10PASS/1Explore缺样FAIL。新增专项退出6ShapeRID错误另列，整版未验收，原完整十一项ACTIVE。项目仅为`G:/knight/Knight`，与UE工程无关。

## 事实、推理、风险

旧RustShellSquire以LOS及完整身体同领地授权战斗；战斗模型另用水平360距离判断追击，巡逻层却以同域LOS进入Engaged。于是玩家可见但超360时，战斗Idle、巡逻Engaged/方向0，敌人站住。实际保存作者slot已先复现120物理步停巡；真实D撤离反例又记录248步Engaged、恢复巡逻0，HP3/5不变。

修补只用原DetectionRange协调巡逻输入，保留原战斗seesPlayer/Step/授权、已起手攻击、LOS/出域撤权、伤害/形状/速度/数量。原生夹具用实际Player、v001 rock-wanderer、作者low-ceiling-bridge.01的home(666,604)、领地/路线及保存BridgeFarBank。入树后不写pose/velocity/HP/AI；逐物理步查真实接触、同独立World2D、全身同领地和实际LOS。引擎键盘不是OS输入或真人体验，独立夹具不代表自动出生/全50/全76。

## 原件与TDD

原件根`G:/knight/验证证据/october13-stable-claw/`，按时间/尾ID定位；保留失败，不跨源拼绿：

- 011437-fd194f67：缺namespace构建失败，无原生，不算行为RED。
- 011648-1b8be21e：120步远距冻结RED，Owner warning/3ShapeRID并存；只修夹具旧Owner结束顺序。
- 012035-d271113f：同冻结RED，Owner warning消失，3ShapeRID仍在；随后才改生产仲裁。
- 012227-fcfbb0d8：远距已恢复，900目标追至约69.33却反复起手未中，退出4ShapeRID。保存hitbox可触及约67而起手70，独立E08候选保留；拆开追击和720可触及命中，不擅改武器参数或称空挥已修。
- 012744-360b9590：四功能案成立，退出21ShapeRID，严格FAIL。
- 013718-6906e9d2：新增撤离误用整路线J/K/L测试门，未到行为断言，不算撤离RED；仅校准单D观测合同。
- 014026-d0987fa6：临时故障控制复现旧仲裁，真实D撤离248步Engaged248/resumed0/homefalse；功能RED成立。终态后立即恢复精确range条件，没有故障代码遗留。
- 014626-7849f0f5：最终五功能案齐，engine0/11824ms、cap55000未超时；stdout退出6ShapeRID ERROR，wrapper严格FAIL。stderr0/externalERROR0不覆盖stdout错误，功能回执两sink一致。

最终真实撤离248步：Engaged64→Patrolling184、经过home继续左行、Returning0；远距120步全巡逻；近距60步追击；贴近29步warning27/active2、同attack ID1、Connected1、玩家5→4；旧wave120步Idle/不移动。各案均有实际owned节点退役断言，借用shape/Profile/PackedScene不Dispose。泄漏根因未证。

## 最终同版资格

冻结1925源清单SHA`DC48864E9D88C7CFAF4BE86A640DA10F87E3DB7A9556BDF3085A099A3816AA40`；六产物清单SHA`480114664D1FE4FC0D7518C021764E63488C1F79F48D2245BF635798EE0DF959`。与v332源集比较只有RustShellSquire/测试runner两处变化及一个新CS，canonical helper单列，没有地图/美术/武器参数或数量变动。

八个小阶段严格PASS：原patrol45534ms、models529、rig38480、presentation18573、gait30079、recovery16175、actions10876、save12952；独立cold1595ms，两不同native进程、6只读witnesses，两款gear各4负控/精确恢复。独立审计1963快照对0差异/48实际binary复制匹配。

完整Main严格PASS252552/600000ms，标题到Boss/实体支撑Completed、同一Player、8/8遭遇、恢复0/0、21普通跳全部grounded；完成事件/次数1、held60位置/body与save保持，SHA0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517。视觉452→453/pub557→559和原生2STEP相邻交接完整，并非旧稀疏gap重现/补全。前段150405/180000ms、44/44返回且无诊断；后段191752/300000ms、10suites/75项执行分类/retired0，ERROR0，stderr23311字节为原四条限定负例WARNING，不称stderr为空。

最终阶段表（原11加旧patrol/新range，不把两种11混淆）：

| 阶段 | native耗时ms | 严格结果 |
| --- | ---: | --- |
| 新resident-patrol-range | 11824 | FAIL：退出6ShapeRID，五功能案成立 |
| 原resident-patrol | 45534 | PASS |
| models | 529 | PASS |
| rig | 38480 | PASS |
| presentation | 18573 | PASS |
| gait | 30079 | PASS |
| recovery | 16175 | PASS |
| actions | 10876 | PASS |
| save | 12952 | PASS，另独立cold1595 |
| Main | 252552 | PASS |
| pre | 150405 | PASS，44项 |
| post | 191752 | PASS，75项/原四限定WARNING |
| Explore | 320727 | FAIL：visual接缝453→455缺相邻物理样本 |

Explore020821-f0cd17e7原生exit1、未超时、stderr7956字节/external ERROR1，无RID/Handle。实际native新通道这次记录453/454/455三步、同motor/revision2、原生upward/shape及body在454交接、PAIR complete=true；原视觉仍453→455，不能把原生root乘旧pose补造缺失视觉，也没有最终Native_OK。该次真正捕获稀疏visual时的完整native交接，但**不是已补齐视觉或已证无滑脚**。未生成最终route JSON/全路线OK。stdout局部虽有Boss击败/支撑出口Completed/held60/33普通跳均grounded/恢复0及一次自然Arc无伤，仍不能据此签完整JSON/camera/save SHA链；summary明确whole_route_GREEN=False。保留失败、不重跑筛绿；下一先使用这条双通道实际差异查视觉发布/取证合同，不降低相邻帧门，再处理独立E08首次攻击几何。

所有拥有的进程已终结。独立审计3346快照对0差异、78实际binary复制匹配；最终一次当前重新枚举审计在2026-11-08T18:14:54Z通过：1925源、6产物及全部13案after一致、16唯一helper/4review pins、109保护行内104实际文件未变，父D4AE/BasinA038保持，KNIGHT native0，G可用77,582,471,168字节。清单文件SHA与规范化当前文本SHA分别记录于[身份审计](../../.superpowers/sdd/2026-11-09-resident-far-patrol/current-identity-audit-v001.json)，没有重枚举刷绿。

本轮无新过程媒体，near-final唯一20GiB检查18:15:24Z：18,882,711字节/182文件，新增18,879,203字节，Notify=false。未达提醒档位，不新建自动化/打包/删除。此前FATAL/句柄/RID及旧Minor原因没有因此关闭；Task1因严格门失败仍未完成，完整目标ACTIVE。

## 一次审查及裁决

一次fresh增量审查C0/I0/M0、4pin匹配，旧严格门/55秒cap/清理未松；无第二审或Minor修补。审查保留五个边界：资源根因未知即保留严格失败；69.33空挥独立待做；整线/cold以终态为准；独立fixture不证明自动出生/全人口/声画真人；无关dirtytree和历史故障不签收。代价分别为稳定验收受阻、边界仍可能空挥、不可继承旧版资格、局部证据不可代签全关、无项目整体review结论。

执行裁决另见[ledger](../../.superpowers/sdd/2026-11-09-resident-far-patrol/progress.md)：持续执行无需例行审批，但不改变感知数值；不动Git而保留原dirtytree，代价是要有精确增量审计；受控场景换取可判因反例，不等正常manager/lease；保持旧combat授权，不把巡逻CombatAllowed强行变成攻击撤销；近控拆分保留独立空挥缺口；单D新增真实撤离证明，但不冒充整路线J/K/L资格。

无GUI/系统输入/付费新美术/过程媒体/自动化/Git写入/删除/真实存档实验。正式美术动作、正常速度声画/相机、硬件性能与真人认可未完成；所有历史失败与Minor保留，不暂停或标完整目标完成。
