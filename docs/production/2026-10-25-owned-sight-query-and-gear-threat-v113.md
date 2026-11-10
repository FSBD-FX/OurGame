# 自有视线查询释放与齿轮区真实威胁 · v113

2026-10-25，工程 `G:/knight/Knight`。原十一项完整目标 ACTIVE；Task15、正常双路线、资源句柄根因、正式视听/相机、真人硬件验收及发行均未完成。用户最后的显示结论仍是 NOTSEEN，没有新开游戏窗口或发送系统键鼠输入。

## 事实、推理、风险

事实：RustShellSquire 的每次真实视线查询创建自有原生查询对象，但原先没有确定性释放。新增原生测试先在未修生产代码上失败：同步调用返回后 `ObjectCount` 最大净增加 1。最小释放修复后，左右方向、原始与嵌套自身碰撞体四组各64次查询最大净增加均为0，原追逐及墙体/外来Area遮挡仍通过。

推理：这支持闭合本调用返回边界的自有查询对象保留问题，不支持声称历史 Handle/FATAL 的根因已找到，也不能用全局净对象数代替所有内存分项或显示FPS。

风险：机械爪、全图可玩性和正式视觉没有因这项释放修复自动获验收。齿轮区自动探索仍不能在原210帧内回到95%能量，防御并非卡键：实际常驻墓灯蛾会进入接触范围，不能删除必要的L或改能量/时间阈值凑绿。

## 三文件有界变更

- `scripts/enemies/RustShellSquire.cs`：只释放本次新建 query、结果 Dictionary、自有RID数组底层和递归 `GetChildren()` 新数组底层；借用的Node/碰撞体/Shape/World不释放。碰撞mask、排除范围、遍历、视线返回与AI保持。
- `tests/integration/RustShellSightRuntimeTests.cs`：四组64次真实同步查询，无额外物理帧、显式GC或mock，保留原左右追逐/嵌套自身排除/外来World及Area遮挡。
- `tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`：只增加有界被动身份与几何记录。身份复用原出生缓存，不重新解析已结束实例；几何接现有danger遍历，不传改变排序的guardCapsule，不增加查询、控制或预算。

已安装GodotSharp的IL与独立审查确认，泛型Array显式转换返回其实际 `_underlyingArray`。这里只释放新拥有的数组，不推广到借用资源。

## 齿轮区下一步的实证

最终Explore的真实防御源是 `canyon.v2.resident.gear-pistons.04`，GraveLampMoth，源场景 `scrap-flyer.tscn`；不是先前推测的波次Rust。

首次警告physics3165、remaining0.7秒，源点 `(2568.5847,3856.6714)`，玩家 `(2496.51,3879.9941)`，即右侧近身而非头顶固定俯冲。Active在3207–3227发布21个半径22的圆；22条总几何记录，遗漏0、故障为空。15个Active样本与玩家胶囊的数学clearance为0，说明潜在接触，不是原生穿盾伤害的独立证明。210帧后能量64.59988，原95%要求仍FAIL。

下一候选是利用现有机械爪的真实目标/视线、实际键盘八向和足够提前量打断这只墓灯蛾；尚未实现/验证，不预先宣布成功。只允许在原210帧内非阻塞尝试一次，保留所有威胁防御、HP、完整支撑、原J与能量门，不固定W/A、不新增解析器授权或额外施放窗口。

资源侧只读定位还确认，v112 REF pre184408的Handle调用栈实际落在 Continue `OpenSample.AddChild` 中，不只是“最近的日志”。但是没有具体资源serial、句柄指针或所有者关联；邻近异线程记录/三个PackedScene弱引用死亡不能拼成根因。没有据此修改生产缓存、GC或全局保活策略。

## 八个实际原生结果

证据均位于 `G:/knight/验证证据/october13-stable-claw/`，目录名如下。全部build退出0、非超时、guardFailures为空；4严格PASS、4FAIL，不将故障负控的RED算成功版。

| 案例目录 | 原生退出/毫秒 | 严格结果 |
| --- | --- | --- |
| thruster-route-exploration-diagnostic-20261025-192201-810-61215eee518b4ff39bf14e3b4b6e451b | 1 / 67686 | FAIL；身份记录建立，原Gear能量失败 |
| prologue-pre-20261025-192948-007-5196f0f222994069958cf52ddba6dd02 | 1 / 104933 | RED；新自有查询断言最大净保留1 |
| prologue-pre-20261025-193324-655-e9bd9baf163a4e55bf5f9a7192d268a5 | 0 / 156139 | PASS；生产释放后四组净保留0 |
| thruster-route-exploration-diagnostic-20261025-193928-373-1f98f26a70cd4c51a4eaa1c12f5ac4a0 | 1 / 63675 | FAIL；真实圆形威胁记录，Gear能量仍不足 |
| thruster-route-main-diagnostic-20261025-194059-232-c94003a5e95040c598f46204c5d323fd | 1 / 132635 | FAIL；physics7114死亡恢复request1/completion0 |
| prologue-pre-20261025-194339-660-34d8543dbc854df7975cef162e2f5e20 | 0 / 158320 | PASS；最终冻结普通前置回归 |
| prologue-post-20261025-194646-722-3ac382688b17495298656bd3cd29a180 | 0 / 199156 | PASS；原75项及owner8 |
| models-20261025-195035-363-46c142dd6b3641aeaa79b7f26fe7473f | 0 / 400 | PASS；普通Debug恢复 |

最后五案同一冻结源。post原75项索引0–74，150条enter/return，各一次且顺序/名称与v112相同，retired0；owner为RunPrologueTests，重复8次、节点1→1。四条已分类负例警告仍在原三个精确scope内：seamless1、room-profile1、flow-registry2；没有扩大豁免。普通post使用既已记录的300000ms范围预算，不宣称原180000ms内通过或性能合格。

## 一次独立审查与全部裁决

三文件实体基线差异审查：Critical0 / Important0 / 新增Minor0；没有第二次审查或新修复轮。继承的 energy/unwind 提前异常时未输出汇总的Minor继续延期，不借此关闭玩法失败。

以下逐条承接审查者的12项“不作判断”；都是root边界裁决，不是默许完成：

1. 完整Main/Explore：保留两条真实FAIL，最终普通批次不替代正常显示/玩家验收。错误代价：把局部通过当可交付。
2. Handle/FATAL根因：维持OPEN；查询释放与具体坏句柄无因果关联。错误代价：随机崩溃被掩盖。
3. 全部内存/FPS：不认证；ObjectCount只是同步窗口净值。错误代价：资源或长帧问题漏验。
4. 新K对策/移除L：未实现，不因潜在几何接触擅改防御。错误代价：测试人为避错或玩家受伤。
5. 实际W/旧preview排序：本轮只观察，不改reader或排序。错误代价：将时序问题误改为全局优先级。
6. 遮挡/receiver准入：只认证本轮Rust视线原合同，不延伸到机械爪首hit/receiver。错误代价：越墙或非法目标。
7. Intro/preparation/Playing：未改，不重新认证全部准入。错误代价：准备态被当成正常游玩。
8. callback/deferred退出：未改，不以query释放代替退出生命周期证明。错误代价：卸载后旧回调越权。
9. LoadLevel挂载失败/双异常：未改，既有合同仍独立要求。错误代价：泄漏或主错误丢失。
10. energy/unwind汇总：保留既有延期项；此次不扩修复范围。错误代价：早失败时证据不足，但原失败仍应保留。
11. 原十一项/76普通+BossNPC/正式视听相机/硬件真人/发行：全部原验收门保留ACTIVE/NOTSEEN。错误代价：缩减用户交付目标。
12. 全树/二进制/终态：交给独立只读保管审计，不拿窄审查替代。错误代价：跨版本拼接或漏报漂移。

## 终态与保管核对

旧handle70666已不存在；权威provenance和当前CIM确认原批次终态且无自有Godot/dotnet在运行，未因观察中断重启。全部原生和独立保管审计结束后才更新本文及索引。

独立审计：8案40清单对/80清单文件/48封存二进制；missing、drift、malformed、duplicate、extra均0。当前1844源逐项匹配最后models，Debug6/6实物匹配；相对v112仅上述3个C#变化，添加0/删除0。

- 最终五案源清单SHA256：`A78DE5F7EB3E502129D2989E9E9196DE0AD48A7D1BBD8E24AF3905D26685CAA3`。
- 最后普通binary manifest：`E5D3F3D94114D34040EE3C878F7B3FD72D8644D05B1A56CF9B9209D93EB3B595`。
- canonical owner：`309DD6C42FB5E96F27390711FF91D2D419890CF17BA256869E2D2A22961255F2`。
- Git index：`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`，HEAD仍 `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`。
- 根音频布局：`CAD39DF12EC97BA5DC25AE5B5CF274E064AAA65DC557B5C9415089ADF8E4EC7A`，没有改正式声音。

唯一现有手动媒体检查UTC12:12:58.8238603：860284字节、基线3508、增长856776、Notify=false；新游戏过程媒体0。没有创建任何调度、自动化、付费生成、Git写入、删除或真实存档语义读取；没有处理另一个UE项目。迁移盘点、测试绿色和本报告都不等于完成原十一项。
