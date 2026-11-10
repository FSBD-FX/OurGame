# v187 — 无窗口被动边界调查

2026-11-01，仅KNIGHT / G:/knight/Knight。原完整十一项目标ACTIVE，未正式完成。本轮没有生产玩法、地图、数值或美术增量；用户仅授权无窗口构建与测试。未开图形窗口、未发OS键鼠、未用真实存档、未付费/额外采集/自动化/删除/Git写入。误发视频未使用。

## 增量与审查

Supply仅记录既有root/world/flow/guidance引用的标量，128行上限；原四案、AddChild、3+8等待、Continue、地面/清理检查不变。没有GetScript、子树遍历、新wrapper发现、资源强根、GC或缓存策略变化。

Main独立监听真实ClawFrameChanged，不提前启用原HP1控制监听；8实际方向快照、64条原扫描来源上限，不改输入、K状态机、选敌、危险折叠、旧窗口和900循环。未选中项LOS不测，缺方向仅记未观察到。

唯一新鲜只读审查C0/I0/M0：Supply精确+69/-0、Main+194/-0，无修复轮/二审。仅允许保留诊断增量，不批准完整分支或资源/路线修复。

## 实际运行

证据根：G:/knight/验证证据/october13-stable-claw/。

| 案例 | build/native ms | 严格结果 |
|---|---:|---|
| prologue-pre-20261101-044000-343-0be23641ac084f199fd661b5bc4ea73a | 27074/173186 | FAIL：native exit0但3条Handle ERROR |
| thruster-route-main-diagnostic-20261101-044352-998-aa7ef7a9f92b4625ad5a539d99d87a14 | 25845/130296 | FAIL：native exit1，死亡恢复触发原位移断言 |

两次build均0警告/0错误；原生实际headless、无fixed-FPS/movie、无超时、guardFailures空。原build/pre cap180000ms、Main600000ms，未豁免或重跑筛绿；不借旧Explore/post通过覆盖。

### Supply事实与边界

五root各8阶段，总40/128行，遗漏0/故障0，四案执行（第三案含Continue重开，故5root）。挂树前在树外、返回后在树内。stdout159的Supply OK、160观测END及518的pre phase OK不能覆盖stderr错误。

| stderr/external行 | 直接边界 | 不能推断的内容 |
|---|---|---|
| 1/110 | 首newgame_reentry的GD.Load→ResourceLoader.Load→CreateSampleFixture576 | 请求PrologueLevel.tscn已知，失败wrapper/依赖/脚本/handle未知 |
| 57/166 | 同首fixture的PackedScene.Instantiate→CreateSampleFixture576 | 与Load为不同调用，均在首root标量之前，不能合并同对象 |
| 112/229 | SetGodotObjectPtr→internal_refcounted_disposed→GodotObject.Finalize→GC.RunFinalizers | 顺序在首open-return后、NEW_RUN_REENTRY结果前；无业务caller或资源身份 |

返回的root3456727255202/world3457465454424/flow3466474818475只作相关性，不是失败对象身份证据。五挂载返回不代表资源安全，退出finalizer哨兵count1不是坏wrapper数。旧v186直接边界为第二fixture AddChild，本案不同；同桥接报错文本不证明同因，不能归罪onHealed/StartNew/teardown、新K模块或特定sector。

### Main为什么没有preview

stderr1：step3120.29，从(4753.8955,3423.9336)恢复到(1680,3960)；observer恢复1/0、physics6619。这是死亡后恢复，不称无故瞬移。实际混合战斗观察到能量0、不能挡/攻，随后BeltBurst造成伤害；尚未验证合理应对，不据此改生产数值。

stdout1225独立END：0/8样本、0publication/来源/故障，listener_detached=True；2268为start_physics=0。batch1 RustShellSquire/BeltBurst确已到场，但本次没有经过“压制增援→重新到场”的恢复分支，mixedCandidateStart只在该分支赋值。因此新增监听被错误的启动前提挡住；无实际preview/扫描新鲜性样本，不能回答Left、八方向、K或通关。

下一独立诊断应同时覆盖正常batch1和压制恢复batch1，不改旧candidate窗口/控制变量，不补A/K或额外调用resolver。本轮尚未实施该调整。

## 保管与决定

两案10对manifest相同，1070冻结source实体、22helper实体、12bin逐SHA一致。当前1871源/6bin/对应helpers绑定；四owned身份44900/49012、37652/54128按创建时间/路径核实结束，54128不按旧同PID误认。未重哈旧八案。

- source manifest：F6624F86A39545CD00FFE2849C04BB2D5BA254568C2E9E892082B6B7B566D6B5
- Knight.dll：35BA44E59CBA84653CAC50C79CBB2C577B42A3025B6EA6D13AEA01E71C82F778
- Supply：0BDE3E352E6178E5E4027C8DCB3B09B5C99EF3E28369654A9E9C9CC0E7655CA7
- Main：C3975A9C12FAD8149FEFE3D1B61FF35BD9CC6E6DC483B4CA34F63F3D53A768EC
- canonical未改：5F683E64A01EA7F211D05B1FC5AC94E49CDF9DC076339AD26B26C9B012DD5A9B

旧pre有Supply全源manifest而未保存选定文件副本；已找到历史同SHA57C2CCF5的完整基线，另逐字节保留当前Supply至证据根/v187-observer-source-20261101/BasinSupplyRuntimeTests.cs.snapshot（SHA同当前）。Main副本在新案before/after。未覆盖旧记录。

HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。媒体手动检查2026-10-31T20:49:29.6959831Z：7427750bytes/86files、Notify=false，基线3508未重设，无新媒体/自动化。

本轮决定及代价：

1. 不运行Git写入/新worktree/删除helper，在唯一脏工程保全副本及ledger。代价：无新提交的一键回滚，依赖指纹/副本。
2. 两处源码工作独立，原生全部root串行并冻结源/helper。代价：串行耗时，不并行冒用DLL。
3. 承认标量观察可扰动计时，不以静默或exit0推断修复。代价：根因仍待对象级证据。
4. 审查未判断的资源身份、八方向正向关联、旧控制策略及完整游戏资格全部保持OPEN，不把范围外当通过。代价：后续独立验证；无新延期Minor。

## 下一步与目标

先修正Main纯观测启动前提，取得实际preview/能量/危险窗口，再据证据调整测试应对。资源先只读审计原pre前缀共享生命周期，不拆链式Load/Instantiate延长资源持有，不加GC/等待/缓存绕过。原十一项、当前同源双路、资源根因、正式美术动作音频/UI、正常渲染、目标硬件与真人试玩仍未完成。诊断留档不是正式交付。
