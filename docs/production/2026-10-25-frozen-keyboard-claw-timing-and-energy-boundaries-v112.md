# 冰区键盘抓爪时序与能量边界 · v112

2026-10-25，Asia/Shanghai。工程 G:/knight/Knight，原十一项完整目标ACTIVE；不是整关交付、视觉认可或Task15完成。用户“没有看到窗口”继续有效；无新前台窗口、系统键鼠输入、付费、自动化、Git写入、删除或真实存档语义读取。

## 事实、推理、风险

事实：自动探索在同方向、一次真实K下，计划选中飞蛾，首次生产Aim却选中墙面。见证复用同一次旧规划查询，不额外解析授权。规划时飞蛾角误差0.644221°；首次Aim的生产原点下飞蛾3.494273°、墙面实际命中点2.812495°，按现有角度优先合同确实应选墙。

推理：本例是自动试玩的旧“切模式后等待两帧”让移动目标计划过期，不是按错方向、重复K或已证实的生产排序缺陷。纯键盘适配器SelectGrappleMode只同步设置规划意图，不发模式键，生产CanBeginGrapple也不依赖展示模式。

风险：修正的是测试消费者，不是游戏内机械爪手感、动画或全局锁定规则。两次成功不是所有移动目标或整路证明。资源短弱引用记录不证明原生资源已毁、释放归属或历史Handle/FATAL已修。

## 三份测试源的实际改动

1. MainRoute测试：仅Frozen键盘分支，在物理Space、CurrentFrame与同序列RawFrame的JumpPressed/JumpHeld全部中性时，把旧模式前缀2帧改0；其余条件/调用仍2帧。模式意图true/false调用保留，实际K采样再检查中性。原每次一次K、新cast、正确八向、目标身份、一次应用、ThreadStunned、HP不变及45帧等待都保留。无第二解析器授权、额外K、传送或速度/生命写入。
2. 同一测试的原Gear210帧：被动订阅原EnergyFrameChanged，复用原danger结果，累积正负变化、首末扣能量、L与延迟标量；finally解绑。控制、210、95%能量、热量、血量与支撑约束不改。零事件延迟为null，避免NaN伪值。
3. Camera测试与Task14诊断：仅REF编译在原默认GD.Load返回处直通记录前三个源的序号/sector/托管hash/null/短弱引用；工厂和直通方法禁内联。普通编译原表达式及诊断cache分支Load→loaded回调→返回保持。原相机边界只读IsAlive布尔值，不存Resource强字段、不取WeakReference.Target、不追加Load/Instantiate/GetState/GC/帧或旧ID反查。

没有修改生产物理、AI、能量、地图、攻击排序、资源缓存、正式图声/UI或输入键位。

## 实测结果

### 抓爪局部真实RED→GREEN

原181110错选墙后，184006首次修正、185000加RawFrame保护及最终185425都出现两次实际飞蛾施放：应用0→1→2，每次HP2→2/ThreadStunned，首次Aim选原目标，prefix实际0，原45内成立，观察无故障/遗漏。每次各一个K，不能写成“一次K触发两次应用”。

Explore仍FAIL：较早正常落伤3→2保留；后面715.99px落地2→1也保留，再到Gear准备失败。没有回血、取消摔伤或改地形使它消失。

### Gear能量不足，不是卡键

原210帧能量55.39983→64.59988，实际恢复44.800056/支出35.60002。两次J支出24；29帧真实持续盾耗约11.6，29防御请求/29采样L/29持续盾能量事件吻合，共212能量事件，差额闭合。最后扣能时物理L、RawFrame、采样、盾active/sustained均真，延迟0.55；清理后L与两个采样均假。

HP1→1、热0、未过热，失败的是原要求能量≥95。未增加预算、降低阈值或补能量。现有日志没有把最后danger actor ID映射到类型/稳定槽/实际攻击几何，不能猜成Rust，也不能认定防御无用。下一步从既有身份缓存/原danger扫描/实际攻击发布处复制完整Active、玩家胶囊与支撑事实；单帧距离不足以授权取消L。

### 原Load弱见证，不是资源修复

36条相机边界序号1–36/遗漏0，未放宽48上限。三个源包装器第一次弱死都在original-eight-frames-returned：边界9/21/33、physics52/155/212，Level仍InsideTree。没有源native ID，托管hash不作无碰撞身份或原生缓存别名证明。

REF pre native0/170518ms仍有一条Handle is not initialized（GCHandle.FromIntPtr / ScriptManagerBridge.SwapGCHandleForType），严格FAIL。此案无FATAL不关闭旧FATAL；弱句柄/日志/禁内联可能改变GC时点，未据此加生产永久缓存、强制GC或处置策略。

## 九次案例与最终冻结结果

证据根 G:/knight/验证证据/october13-stable-claw/。下表case完整名相对于此根。全部build0、native非timeout、guards=[]、源1844；前四是中间/诊断，最后五同冻结，不拼接资格。

| Case | build ms | native exit/ms | 严格结果 |
| --- | ---: | --- | --- |
| thruster-route-exploration-diagnostic-20261025-181110-125-bd198c7e28c645569167567c20bfeece | 22696 | 1/44948 | FAIL，旧前缀后K选墙 |
| thruster-route-exploration-diagnostic-20261025-184006-711-bbaccc03653d4c7db063f6d81cdaef7e | 24393 | 1/66198 | 两次K局部通过，Gear FAIL |
| prologue-pre-refcounted-identity-20261025-184408-474-aba24a9c31d346eab2430177ccc0cde9 | 22931 | 0/170518 | FAIL，一条Handle |
| thruster-route-exploration-diagnostic-20261025-185000-164-46d58b8b6e064b08b964c108a7ece569 | 22932 | 1/66653 | 能量归因，Gear FAIL |
| thruster-route-exploration-diagnostic-20261025-185425-681-8ff0fb4b388446e59bc4bdbce51ec1e3 | 25439 | 1/70482 | 最终Explore FAIL |
| thruster-route-main-diagnostic-20261025-185608-433-36d99b541bad4a6989d4ba82ae5f60fe | 23247 | 1/135631 | 最终Main FAIL |
| prologue-pre-20261025-185853-741-d98a67f990744f23a12c7b0d0f1d6b69 | 27324 | 0/168413 | PASS |
| prologue-post-20261025-190216-008-6c0ac05cf2814d59a9feeadb8eb1043b | 26465 | 0/202953 | PASS，完整原75及后置 |
| models-20261025-190611-600-93881d2282dc4d18b74346bfec10a4f8 | 23671 | 0/406 | PASS，普通构建恢复 |

九案严格3PASS/6FAIL；最终五案3PASS/2FAIL。Main仍为原BeltBurst战斗死亡后由(4751.553,3423.933)恢复到(1680,3960)，零恢复/无重置路线约束拒绝，不称无故存活传送。Main接受伤害5条、归因缺口0；最终Explore接受伤害4条、缺口0。

最终post独立核对原0–74各75次enter/return、retirements0；顶层同owner实例8次、节点1→1。四条故意负控warning保留：throwing observer1、room profile1、flow-registry2，三声明scope；不称零warning，也不把多sink重复转写计作更多独立事件。普通pre无raw诊断。没有本轮新独立mixed/counter窄案资格。

## 一次独立审查与全部裁决

物理v111三基线审查C0/I0/M1，无阻断修复项，不复审；包见 `.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/review-v112.md`。

延期Minor：能量汇总在try/finally正常完成后打印，若更早死亡/失撑/PhysicsFrames抛错，累计见证不会输出。当前“.646”断言在打印之后，不受影响。未来可做不覆盖原异常的标量unwind输出，本轮不顺手改。

主执行者对全部13项Declined-to-judge裁决及误判成本：

1. 最终五案已逐真实终态核对，3PASS/2FAIL；不能把审查时live当通过。成本：误拼/漏报资格。
2. Main/Explore整体仍未完成，局部两K不能代替终点。成本：仍有战斗/准备阻断。
3. Gear真实支出已知，安全策略未证；保210和95，下一查owner几何。成本：无用格挡或真实受击仍需区分。
4. 全局排序不改，时序交叉不证明所有目标选择最优。成本：其他竞争目标待测。
5. 无新增首碰撞/receiver因果证明，不用二次resolver授权。成本：其他遮挡未关。
6. pending-Space及非Frozen保原2帧；本轮成功只覆盖Frozen0，非穷举。成本：特定分支缺新原生覆盖。
7. Intro/Playing原准备拒绝保留，不关闭所有转换。成本：其他入口仍可能有前提错误。
8. finally解绑不等于全部回调/延迟退出重新认证。成本：历史生命周期路径仍需原验证。
9. LoadLevel原异常与清理合同未改，不据安静追认。成本：旧清理风险继续开放。
10. 弱死不是native销毁/所有权/崩溃归属，REF也可能扰动GC。成本：根因未知，不作缓存修复。
11. 三相机源不代表普通应用冷首次获取或所有资源图。成本：pending-finalizer首次窗口未关。
12. 正式图声动作/UI相机、76普通+Boss/NPC全玩法及目标硬件无本轮端到端验收。成本：原十一项不能结项。
13. 未读真实存档语义、未真人验收、打包/Git写入/发布，NOTSEEN有效。成本：发行和真人性能/键盘认可待授权实测。

## 保全与边界

独立只读核验9案45对清单/90份文件、54个封存binary：缺失/漂移/坏行/重复/多余binary全0。ROOT/DIR元数据、helper绝对路径及空continuation是既有合法格式。当前源1844/1844、Debug6/6匹配models，最终五案十份源清单同hash。

相对v111仅3测试C#变化，无增删；C#892，源1546+imported298。当前hash：

```text
MainRoute DDF0112F7F1D5BF0C181BBB153F2D0CEE6971531B24B81CBB960CE497A7BF3B0
Camera    C5699EBC546FC2F3EA1F9AD5F79FAD88BBFFB4B15C2C8879A7DED9ED6F8E0070
Task14    C9CBBD6943F4037924033751E2EF37FBBDB21D0BF091267AFD6A128D7AEB7B93
Source    3D1CC18DC389D1D59DC8C9BCF46ACA6A930C9B09F8F66F13F2DAACEC1CF620B6
Binary6   C4DA07A159B408F7C1763D52EF1E0C120D993090BBB3270E98490BA063CF0AA7
Owner     309DD6C42FB5E96F27390711FF91D2D419890CF17BA256869E2D2A22961255F2
Index     21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C
Audio     CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A
HEAD      e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6
```

主执行者唯一写入/编译/启动，全部native与独立source审计结束后才更新文档。文档后diff-check exit0，既有LF/CRLF提示169、其他0；三份C#、owner与index hash仍匹配，owned Godot/dotnet0。新正式图声/图稿/实录0；一次既有媒体check UTC11:10:31.8431493，当前860284/基线3508/新增856776字节，Notify=false，不新建或恢复任务。

下一步继续Gear真实威胁身份/Active几何和正常战斗衔接，以及资源冷首次获取与未关Handle/FATAL，再完成其余原批准内容。完整正常双路、正式视听/相机正常速度、目标硬件、真人键盘与授权版本交付都未完成，不缩减目标，不标完成。
