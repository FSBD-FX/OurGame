# v188 — 正常增援的真实预览已采到，Main仍失败

2026-11-01，KNIGHT / G:/knight/Knight。仅无窗口构建与隔离测试；原十一项ACTIVE，不是正式交付。无GUI/OS键鼠/付费生成/额外崩溃采集/自动化/删除/Git写入/真实存档改动，误发视频未用。本轮没有生产玩法、地图、能量数值或美术增量。

## 已验证的最小修正

v187真实Main的batch1已经到场，但mixedCandidateStart只在“增援压制后的恢复”分支赋值，独立监听因此漏掉正常增援。新增只读收据检查器对v187 aa7ef7a9运行，真实RED/exit1：已有owned batch1，但零生产Idle预览；不是语法或伪fixture失败。

只改独立监听入口：不依赖mixedCandidateStart，仍要求实际batch1、live>0、warning关闭、Idle、键盘控制，并记录batch/live/warning/原resume physics。原candidate窗口、HP1监听、K义务、输入、900循环、危险折叠均未改。

单次新案`thruster-route-main-diagnostic-20261101-045624-972-6033ea2597af4bf6a47e16e2bdad3a3a`：build29164ms/exit0/0warning/0error；native128063ms/exit1、未超时、原cap600000ms。真实headless，无fixed-FPS/movie，无guard差异。原死亡恢复位移断言仍严格FAIL：step3120.29，从(4753.8955,3423.9336)到(1680,3960)，observer恢复1/0、physics6618。没有重跑筛绿或用ExpectedFailure豁免。

同一收据检查器对新案GREEN/exit0；输出明确canonical_exit=1、whole_main/resource_fix/human/whole_goal=false。这个GREEN仅证明漏采修正。

| 实际Idle发布 | 位姿/实际选择 | 能证明什么 |
|---|---|---|
| Left physics6322 / sequence6293 | 玩家(4820.006,3491.999)，GripStone(4800,3491.999)，ID4883092607761 | 当时真实Left生产预览存在且LOS=true；不是后续战斗的候选证明 |
| Right physics6373 / sequence6344 | 玩家(4759.071,3423.9336)，DedicatedAnchor(5100,3420)，ID4885810516915 | 当时真实Right生产预览存在且LOS=true；不意味着抓到了敌人 |

两者batch1/live2/warningFalse、原resume physics0、CanGrappleTrue、盾未激活/Lfalse。END为2/8、关联2、Idle publication297、来源28/64、caps0/fault0、独立解绑True；其余六方向未观察，不能称不可用。

关联的是原有下一次pre-node危险扫描，不是同一生产调用：Left scan6323/seq6293，physics age1、同pose/velocity/input/authority，safe_counter .64999896；Right scan6374/seq6344，age1、同input/authority但pose/velocity已经变化，safe_counter .2999999。敌人pose新鲜性未测，未选中项LOS未测。不把这些值冒称当前完整后处理安全窗。

唯一新鲜只读审查C0/I0/M0，无修复轮/二审。审查者独立运行旧案RED/新案GREEN，认可仅此诊断增量。压制恢复分支实际覆盖、六方向、完整K/路线、资源根因、正常渲染/性能/真人与整个分支资格均保持OPEN。

## Main策略的新增事实（来自v187已保存原始日志，只读分析）

wave Burst16030478895826到场时HP2；resident Burst4895541302341仍在场，所以不能只按两只新敌人预算。6531/sample6500共享窗.600s、E24.266548刚满足两次J储备，6532/sample6502真实J后只剩12.733215。6559/sample6529再次真实L，早于该次J的.55s恢复延迟结束，随后约.4/tick，6590/sample6560实际耗到0；不是过热。6595由wave Burst攻击5造成HP2→1，6619由resident Burst攻击12致死，均有来源归属。

35% energy_gate只是诊断字段，不是实际J门；不能用它解释拒绝J。K引用只在“真实J打到本波Burst并使HP1”后建立，本案从未发生。闪避又要求这次K已完成，故两条自救链不是“已经尝试但技能失效”，而是被测试前提挡住。选中始终Rust，不能声称本案Burst K不可选或K缺能量。

下一步须在真实近敌/尚有能量的边界观察新鲜生产预览，或将该段测试应对独立成可验证的短状态链；不再靠盲调储备常数，不改敌人/玩家生产数值来让测试过。当前两张早期预览不足以支持直接对Burst发K。

## 资源只读审计结果

首Supply前原pre实际已实例化Level7次（Camera3，当前Laborer的off-tree/normal/blocked/walkway4），不是进程首次加载。未找到cached/借用PackedScene或保存Player Profile/SectorDefinition被显式Dispose的具体罪点：BasinClaw释放Duplicate胶囊，blockerShape为new；resident source-holder Dispose只Clear引用，不Dispose场景；音频/ranged释放私有副本；HealthRestore仅释放独占Node。

确有World.Ready→stream model→definition.Validate→PrologueSectorDefinition.ValidateSceneRootTransform对保存sector执行off-tree Instantiate再Free；但进一步展开五个sector后，不能把“Player导出Resource”算入该路径：保存节点图没有Player或保存敌人实例，EncounterGroup在激活后才创建演员，BossArena仅持BossScene引用。Player默认new Tuning/Profile被保存值覆盖属于Level的另一条构造路径，不是sector验证已触及它的证据。仍未证明哪个Resource与坏handle相连；未改cache、GC、资源持有/验证策略。v187三条ERROR保留，不能用安静的后续测试消除历史根因要求。

最终窄展开为9个保存场景/34附着脚本，此实例图内未找到exported C# Resource以new初始化再被保存值覆盖的节点类。嵌套仅checkpoint的GraveLampVisual/PrologueMechanismVisual及Temple BellChain的Formal/Door；BossScene是无new的auto-property，实际SpawnFreshBoss在Ready后，此路径不运行。Entrance名RustShellSquire的节点实际Marker2D，不是保存敌人。故停止“sector验证创建Player/敌人settings”这一不成立候选，不据此修改生产验证逻辑。

## 保管与本轮决定

Main单案5对manifest、568个冻结source实体、10helper实体、6bin逐SHA一致，owned49448/44896已按创建时间/路径核实结束。05:00:53 +08当前1871源/6bin/9helper绑定：

- source manifest F36105E68B30E9B8DE8493C1304F21A2868D4AEBF01B8F264CA508E5F03F91F2
- Knight.dll 18086BBD67ADE91F1FA90A2A7E60379A0F688CFABD78E1657C5250A9BE5AB932
- Main 263390286AF3DB7178AD61CDCE82387675F93250E5B772F543D62CD468AC38E7
- checker 14C06DC92533894460809DCD99A78E9AF3D1C77BDA847475EC1EE4A8FBA1CE1B
- Supply0BDE3E35与canonical5F683E64未变。

决定：原用户持续目标覆盖此诊断修正，不再询问偏好；唯一脏工程不新建worktree/提交/删除helper，以副本和ledger保全，代价是无新提交的一键回退。原生由root串行并冻结全部源/helper，代价是测试耗时。诊断GREEN不覆盖严格FAIL，代价是完整交付仍不能放行。审查未覆盖项全部继续OPEN，未新增延期Minor。

## 同源回归最终结果（覆盖运行中状态）

root exec3970按actions→models→pre→post串行，独立阶段继续但总返回码exit1保留pre失败，不是豁免。全部句柄已终结；未因观察超时重开。最终五案：

| 阶段/案号 | build/native ms | 严格结果 |
|---|---:|---|
| Main045624/6033ea25 | 29164/128063 | FAIL，死亡恢复断言，native exit1 |
| actions050103/d3f0ed84 | 31102/8471 | PASS |
| models050150/3850a75c | 37490/498 | PASS，仅原机械爪模型子集 |
| pre050235/cbc92307 | 27214/180054 | FAIL，原180000ms上限超时，native -1/TimedOut=true |
| post050609/7027b46b | 28303/214631 | PASS，原300000ms预算，非旧180秒或性能认证 |

五次build均exit0、0警告/0错误；原生3PASS/2FAIL，非全绿。pre停在BellChainMechanism遍历途中；截至中途未见ERROR，既不证明资源修复，也不把缺marker视为行为通过。没有延长预算、暂停/重跑/筛绿；post按原独立阶段运行。

post实核75enter/75return、0–74逐项同名有序配对，owner8次节点1→1；恰好原4限定WARNING（scope1/1/2），无ERROR/FATAL。不会称为零原生警告。

独立只读保管：本轮5案共25对manifest、2458冻结source实体、54helper实体、30binary逐SHA匹配，不重哈旧案。05:11:57 +08当前1871源/6bin/最终post10helper绑定同F36105E6 source manifest/18086BBD DLL；共10个owned身份均按创建时间/路径核实结束，无误杀。HEAD/index未动。手动媒体检查2026-10-31T21:11:17.6505251Z：7427750bytes/86files/Notify=false，基线3508不重设；无新媒体或自动化。

诊断入口修正的RED→GREEN已完成，但Main/pre仍未通过；原完整十一项、当前同版双路、资源根因、正式图声动作UI、正常渲染/目标硬件/真人试玩全部保持ACTIVE/OPEN。下一重点是近敌边界的新鲜真实预览及独立的混合战斗应对，不拿首张墙壁预览外推Burst可抓，不用改能量常数或弱化敌人掩盖驾驶前提问题。
