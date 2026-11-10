# v249 · 单rock leaf的普通生命周期阶段控制

事实：原geometry003554已生成24 source profile＋24 leaf tres/tscn，最后rock-wanderer detached/query witness，未到完整census或新leaf Ready。匿名Finalizer FATAL不等于该rock导致。需要分阶段缩小窗口，不猜生产修复。

选择：新增独立普通headless `thruster-rock-leaf-stages`。同`ResidentSceneBindingTests.BuildCandidatePackage`生成全部24，复用`CanyonResidentPlacementTests.CensusLeafSource.Raw/Configure`默认GD.Load，仅实例化一个rock-wanderer raw及一个同类型configured root。不是“只生成一个资源”。与只重跑完整geometry相比能分开Ready/visible/teardown；不采用GC/KeepAlive/cache mode/引擎更换或native dump。

借用原保存leaf的SubViewport独立World2D＋地板生命周期模式：raw保持detached；fixture-only stableId输入`fixture.rock-leaf-stages`、spawn(320,300)、territory(100,100,600,400)、巡逻(240,300)/(400,300)，不改正式地图/目录。configured隐藏挂载，等待6个原物理＋process周期核对真实Ready、独立世界、正确profile、正Health及未删除；显示后再6周期核对可见与同native实例/Health无重置。stableId是factory真实输入，不冒称本case核对了一个未公开的actor身份getter。无SetResidentDormant强制切换或玩家输入，非运动/几何全覆盖。

阶段有序标量enter/return：package、raw、configured、hidden-ready、visible、owner-teardown、fixture-resources、raw-owner。最终恰好8个return及唯一`ROCK_LEAF_STAGES_OK subtype=rock-wanderer package_leaves=24 raw_roots=1 mounted_roots=1 hidden_ready=true visible=true teardown=true same_census_source=true geometry_census=false human=false whole_goal=false`。Godot异步输出/Finalizer仍可能跨阶段交错；不得从最后一行精确认领崩溃对象。

所有原加载/所有权保持：CensusLeafSource自然持有并最后释放raw；viewport树释放后才dispose自建floorShape/World2D；不dispose借用profile/PackedScene，不添加静态持有/缓存。异常也释放本case拥有节点，保留原异常。host默认55s/严格error-warning-leak门、正常源/二进制/存档保护保持；只有新phase要求8阶段唯一有序及exact final。host真实validator先失败控制→最小实现→正负例，非文本grep测试。

一次普通native阶段控制；即使PASS，也不关闭原geometry或其他资源失败；若失败按实际最后返回边界记录，不自动重跑或加flag。只测试CS＋现runner＋host verifier，1888输入不新增CS/UID，source/harness运行时冻结。用户授权仅headless，不显示/录制/付费/原生额外采集/自动化/真实存档/Git或删除。

## 唯一终审I1的实际回归控制

终审指出managed body异常＋清理异常会相互覆盖，且原using可能早释放shape/world。按原异常保全要求新增独立`thruster-rock-leaf-faults`，不是放宽普通stage：2个fresh保存leaf夹具，第一在真实visible返回时抛特定primary、再在owner清理入口抛特定cleanup，第二仅抛cleanup。正常stage不注入故障；fault夹具只持有自己的Node供检查，不持borrowed Resource或修改GC/cache。

测试先对原finally观察确切错误保全失败，再实现尽力清理：保存body错误身份/堆栈，清理错误逐个记录，确认拥有节点退休再释放自建资源，raw最后释放；1错原身份抛回、多个按先body后cleanup聚合。不以catch吞错误或照常final成功；其他已挂载owner不得改变。标准strict门及55s不放宽。

Health前后相等仅证明两个采样值一致，本case没有受伤后再显隐的无重置证明；不把该采样扩称通用reset安全。原生abort/OOM等不可捕获失败不承诺managed恢复。
