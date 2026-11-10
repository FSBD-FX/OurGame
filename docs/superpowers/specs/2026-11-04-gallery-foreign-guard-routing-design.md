# 摆锤验路：保留真实外国攻击防守

## 依据与边界

v245普通Main实际physics8080/input8051接受bone-diver攻击2，HP2→1；前一拍同源Active且genericGuardPlan=true，却实际ShieldHeld/Active=false。当前Skitter专用分支在外国防御实验旗标关闭时以false覆盖通用防守。它是测试驾驶的确定消费遗漏，不是伤害、物理或角色输入失效。

继续用户已授权的无窗口完整制作，不重复询问已定项目/方法；本设计的具体路由为开发选择，不冒认新的玩法或风格批准。唯一G:/knight/Knight，仍用原引擎/dirty分支，不Git变更、额外采集、GUI/系统输入、付费、媒体、真实存档、自动化或删除。

## 选择

考虑了直接通用guard OR、开启全部旧站位探针、只提升已有窄范围foreignUp。前两者分别可能丢掉守向/支撑条件和混入多变量，选择第三者：正常键盘摆锤案可进入原foreignUp完整检查，旧显式探针链继续原样；任何非键盘/非摆锤不能启用。selector不是举盾授权，原同Actor/攻击/骨骸俯冲场景/正上方.92/静止/踩地/完整AscentGrip5走廊及generic条件全部保留。

新增internal `Task10GalleryForeignGuardRoute(bool keyboard,bool gallery,bool normalPlan,bool explicitProbe)`，先提取旧scope&&explicit并跑16字面量组合RED，再改为keyboard&&gallery&&(normalPlan||explicitProbe)。新Enabled只用于原foreignUp首条件，不使contact/earlyCounter/laser/neutral/brake/bait等探针默认开启。原实时guard消费、真实W+L输入、现有记录/清理和原899/HP不改。一次配置回执说明normal/explicit/scope/selected及safety_authorization=false。

## 唯一最终审查I1：外国反击不得花掉存活Skitter防守储备

提升后默认Main确实8058实际向上举盾并保持HP2，但之后resident J390/421/468共耗36点，8234原必须防守时能量耗尽。现有选中Skitter本身时已要求攻击费之外保留`ShieldDrain*(.20+RushSeconds+3/physicsHz)`；当前Rush=.20、drain=24/Hz60即10.8点，J前应至少22.8。选中resident却只判断12攻击费，17.3/12.3都能发J，丢掉同场仍存活冲锋者的储备。

本次一次C/I修补仅补这个消费者缺口：normal键盘gallery、选中外国resident且有实际同组owned-live Skitter时，将同一既有储备沿用到该J门；实际身份/树内/未排队释放/健康/组所有权必须fresh，不改原tracked初始化/事件绑定。原外层门、释放等待后的fresh门与最终J前均核对当前能量和见证，不能只凭陈旧布尔。无owned-live Skitter或不在作用域时本门不额外限制，仍由原各门决定；未知/非法预算在需要储备时拒绝。

内部scalar `Task10GalleryForeignCounterBudget(bool reserveRequired,float energyUnits,float attackCost,float shieldDrain,float rushSeconds,int physicsHz)`先按旧仅攻击费语义提取，字面量17.3/12.3拒绝、29.3允许及22.8邻域/非有限/作用域反例实际RED，再加入原储备计算GREEN；不改原Skitter/共享24保护窗、游戏能量、回血、HP或899。不是增加所有探针或假称foreign提前开盾是唯一原因；实际同源接触仍须单独证据，只有Up+L样本/HP2不作命中认证。

## 验证

16字面量组合对两scope和normal/explicit全覆盖，真实模型RED→GREEN；原回充/来源反例继续。无新旗标实际Main必须观察早采样、同源方向与零伤或明确后继失败；不把配置布尔或未掉血本身替代实际消费。保留原完整pre/post、geometry、存档、键盘、模型及Main/Explore严格门与输入/binary/用户保护，所有原生由root串行，运行期间CS/harness冻结。任何后来Skitter反击/资源问题不通过放宽时间/HP解决；本项不签主角动作、惯性或正式美术。
