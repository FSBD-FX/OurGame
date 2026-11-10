# KNIGHT 句柄故障身份诊断 v043

2026-10-21。资源根 `G:/knight`，Godot工程 `G:/knight/Knight`。完整1–11目标 ACTIVE。本轮为严格故障排查，**没有新玩法、美术或正式人口交付，也没有修复原生崩溃的证据**。正式新增仍0/50、24 canonical未实现、全部50位置null。

## 事实

[v042](2026-10-21-resident-level-reset-integration-v042.md) 普通冻结11门10PASS/1pre原生崩溃；后续五定义标量追踪仍6条Handle错误。它们继续是严格失败，不因为本轮诊断完成撤销。

两份独立只读资源路径调查没有找到生产路径中误Dispose借用Resource/Script/PackedScene的确定代码。主场景除五定义外还有MinimapTerrainCatalog，Player/Boss及动态敌人还会引入Tuning/Profile/Settings；不能用“六错误”推成“五定义+小地图”。Player四处导出默认Resource被保存值覆盖是实际构造路径，但不是已证实的错误，不据此改默认值。

核对了固定提交的[GodotObject.Dispose源码](https://raw.githubusercontent.com/godotengine/godot/a13da4feb/modules/mono/glue/GodotSharp/GodotSharp/Core/GodotObject.base.cs)、[桥接转换源码](https://raw.githubusercontent.com/godotengine/godot/a13da4feb/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs)与[原生引用转换源码](https://raw.githubusercontent.com/godotengine/godot/a13da4feb/modules/mono/csharp_script.cpp)：原生引用变化可触发managed强弱句柄转换，业务没有显式Dispose不等于该路径不会执行。[官方类似报告#112067](https://github.com/godotengine/godot/issues/112067)是不同项目/版本的未闭环复现报告，仅作线索，不证明本项目同因，也不复制其中缓存/保活方案。

## 本次最小实现与测试先行

复用项目原有 `REFCOUNTED_FATAL_IDENTITY` 编译诊断，不新建生产Dispose覆盖或运行时资源缓存。规范owner只改五处：

1. 准许精确 `prologue-pre` 使用原有诊断编译符号；原Main/Explore/Supply诊断仍保留。
2. 该入口提前拒绝混合诊断、movie与named waiver。
3. 新测试helper纳入前后inventory和冻结副本。
4. provenance明确 `behaviorAccepted=false`。

两处登记helper算两个文本改动点；普通七参数fresh rebuild、引擎参数、180秒pre上限和严格错误分类均不改。**C#源码未改**，保护清单仍1700项/870 C#。

新 `test-pre-finalizer-identity-admission-20261021.ps1` 首次运行真实失败：原argv函数拒绝合法pre诊断。最小接线后20项通过：1新入口、6普通构建、3旧诊断、5非法阶段、5实际顶层混合参数拒绝；AST0、未启动引擎。原生命周期4项拒绝测试也通过。没有把编译错误当RED。

## 一次身份诊断，不是普通pre通过

完整case：`prologue-pre-refcounted-identity-20261021-224724-397-752010c8b7344d7ea46163a38b3eab29`，在 `G:/knight/验证证据/october13-stable-claw/`。

- 构建0/0警告/0错误；native exit0、100062ms、无超时/guard、failure=null。
- stderr299597bytes，867 ENTER＋867 RETURN，call1..867连续、无重复/缺失/畸形/20000上限触发；本案没有ERROR/WARNING。不是867个已证明资源。
- 835 disposing=false/thread1，32 disposing=true/thread2，14个被记录类型；所有native_id均unknown。raw_native_ptr与managed_hash仅作本进程关联，不是ObjectID或所有权证明。
- 有9个内层RETURN暂时保留非零指针：每组均为 ENTER outer→ENTER inner→RETURN inner(nonzero)→RETURN outer(0)，不是外层释放结束遗留指针。涉及五定义、两个PresentationProfile、一个PlayerEnergyTuning和一个Catalog；**不能据此认定它们是崩溃对象**。
- 五组custody一致，源码SHA仍 `7E2C48C69175C4D9B6A816E6977E36F70F009869C9ECBC1CD3390115A2A14603`。13 helper manifest SHA `7217D70FC19F1D851FAC4B7493033D9BDAC1C0FF5B13CC8EA7F00DDEFF73BDB8`。

已有覆盖仅执行managed类型/hash/thread、raw NativeInstance字段与Console.Error记录，原base.Dispose仍每次调用一次；没有额外Godot getter/反查/跨await保留Resource。独立审查确认既有SceneWalk只在另一TASK14入口调用，本pre没有启用它。

另用惰性PE/IL元数据读取证据中的实际GodotSharp.dll，未加载Godot类或执行原生代码。DLL SHA256 `7B1D06575DB78E4EA4353186BE5ADD207E43617B836DB24D65EC4088E6FAD18C`、版本4.7.1.0、MVID `01b8d73a-8498-4b10-b64a-b095911e368e`。Dispose(bool)在IL000B先置_disposed，已处置分支0008直接返回；原生调用正常返回后007E清NativePtr，再0092取消weak-self注册。九组嵌套日志与这个已有重入保护一致，不要求修复“内层返回指针”。静态IL不能证明JIT活跃区间、native回调或普通崩溃对象。

**限制：** 覆盖/日志本身会改变JIT/时序；本案没有复现坏call，故无法归属出错对象。MobilityModuleDefinition及普通native资源等没有被全部覆盖；14类不是全资源覆盖。即使全部记录有返回也不关闭普通失败，不扩大为正常路线/性能/真人结论。不原样再跑这条诊断等待崩溃。

## 已恢复普通构建

诊断结束后串行原七参数fresh rebuild，三项通过：

| 普通阶段 | engine ms | 完整case ID |
| --- | ---: | --- |
| 实际Level/新游戏/有效读档/三死亡 | 2873 | `resident-level-lifecycle-20261021-225247-204-ac1431264d6d401998af8c7c378183a5` |
| 键盘 | 9584 | `keyboard-20261021-225319-406-dc9c491de74d4513b8e39730e6ae4eb8` |
| 动作 | 6976 | `actions-20261021-225354-102-2940097c60c3478da9ca483f51c2e864` |

三项均native0/无超时/guard0，build与engine stderr空、外部日志无错误/警告/资源故障；15/15前后custody一致。普通binary manifest SHA `948145683350C6F62EBE746F964414B3F9F65F81D1372DBF7DA1C820641CF34D`，与V042普通原件相同，当前6个磁盘二进制也逐项相符，诊断DLL/PDB未留作试玩版本。共同9 helper SHA `2E7D52D636ABF9FE2549BDBA9435E9BA96AF640C73D21F3F98E3633F1B11E50B`。未原样重跑普通pre来取得偶然绿。

## 推理、风险及接续

实际进展是补齐受控身份诊断入口、核清本次记录没有缺失RETURN且九个非零RETURN只属于嵌套内层，**不是排除普通失败、找到根因或改善了画面**。下一次资源调查必须能识别实际失败对象/低扰动native句柄边界，不再重复五定义有效性或同一867调用观察。若需修改专用引擎/依赖或其他超出当前约束的调试方式，先询问必要授权。

常驻Task3的正式原子准备/出生事务、失败回滚、全部50身体/位置/活动/身份与7处缺口继续按原批准计划推进；资源故障保留为不可豁免的独立验收门，不能发布。全9HP/幽灵姿态连续性、最终原创视听/UI/丰富机关、同冻自然Main/Explore、旧档/正常60FPS/720p–4K/真人与最终ZIP/GitHub等均未完成。无task-done或整计划通过标记。

全部消费者终态后才写本记录。无付费生成、自动化、前台按键、Git写入、引擎/依赖改动、删除或真实用户存档语义读取。没有新展示媒体；手动容量检查仍4838bytes、增长1330bytes、未到20GiB，不恢复旧归档项目。
