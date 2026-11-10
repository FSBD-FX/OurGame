# v144 · 钟背战固定支撑与配置资源终结器证据

2026-10-27，**KNIGHT / G:/knight/Knight / Godot.NET**。这是制作记录编号，不是新发行版、GitHub版本或完整序章交付。

## 结果

- 实际探索线两次完成此前卡住的钟背战：**6次J开始、6次命中、3次K，敌人实际死亡、敌群完成，玩家生命1→1**。两次分别取得502、516个连续、新鲜的原生物理采样，始终完整支撑在PistonTwo，没有额外跳跃、冲刺或恢复。
- 只改测试驾驶与验证：复用现有150°护盾允许的W/S方向，避免斜向防守同时带出A/D横移。生产键位、盾范围、敌人/伤害、能量、地图、美术和原K/J门槛未变。
- **完整探索仍FAIL**：更后的AscentGrip1释放下落被常驻骨骸击中死亡。Main仍在双枪混战能量0时死亡恢复。
- **完整post本轮发生原生FATAL**，不是仅缺少完成标记。最终普通八项为 **5 PASS / 3 FAIL**，不能沿用上一轮6/2结论。
- 追加的身份诊断再次崩溃，将出错类型明确到`ResidentSubtypeProfile.Dispose(false)`，并关联到一次非空配置赋值；没有证明出生原生ID、具体所有者或根因。诊断后已恢复正常构建，6份运行文件与诊断前精确一致，恢复models通过。
- 原十一项保持ACTIVE。继续无窗口、NOTSEEN；未发送系统键鼠、生成新美术/过程媒体、付费、创建自动化、Git写入/发布、删除资料或读取真实存档语义。

## 事实、推理、风险

### 钟背战为什么卡住

既有固定平台引诱已经在第315轮完成，首次K和三击也有效，并非需要重做引诱。旧驾驶在第660轮对下方接近的Bell使用左下盾向；663轮原生记录显示A仍按着、横速−146.66667，玩家x2862.6157，虽仍接触地面却已失去完整平台支撑。

PistonTwo实体世界矩形为`[2844,3040]×[3836,3956]`；玩家半径20，完整支撑中心x范围`[2864,3020]`，站立y约3799.994。上端Bell中心约`(2809.5684,3801.9336)`，其右侧55..64站距与平台重合约9px；该站位已有首次K/三击实际证据。PistonOne不满足这组高度和水平距离。先前717..820只能当候选估计，现有日志并未连续证明整个区间安全。

本轮在既有一次全源扫描中组成最多16个来源位置的值列表，保留原观察者调用顺序；缺失、超量、回调错误或无效位置时不启用替代。复用v143已验证的`StationaryGuardDirection`，由原盾模型检查主来源及所有已观察来源；仅在原本就应防守、且W/S确实覆盖时选择该方向，否则保留原世界点。初始盾命令、后续瞄准与最终fallback盾命令一致使用该选择，不能在同一轮末又改回左下。

原900**外层迭代**、既有2/8/45等待、24节奏、两次neutral支撑采样、原目标选择/LOS及K/J准入全部保留。没有把900外层迭代误称900总原生tick；观察器也覆盖原有内部等待。

### 真实支撑证据

测试专用priority100观察器在Motor之后逐物理帧采样，要求实际地面碰撞、完整胶囊投影、同一玩家/同一Bell、新输入序号、连续物理帧、生命及恢复/跳跃/冲刺计数保持。它只释放自己取得的临时碰撞快照和自己的观察节点，不释放借用地形。

仅加观察器的9505案取得有效行为RED：physics5522/outer663、378个先前有效采样后，左下盾/A带出完整支撑。首个ad92案是lambda参数`_`遮蔽`out _`导致4条CS1503的构建失败，**不是行为RED**，原件保留。

方向消费者修正后：

| 实际探索案 | 连续物理采样 | 战斗结果 | 更后结果 |
|---|---:|---|---|
| c33459f1a025443d984ba7dd207d7f93 | 5144–5645，共502 | 6J开始/命中、3K，Bell死亡、HP1→1 | AscentGrip1死亡，整程FAIL |
| 4e90146579c94a48a02af66a1894120f | 5144–5659，共516 | 同上，HP1→1，零恢复/额外跳跃/冲刺 | 同一后续落点死亡，整程FAIL |

这是实际战斗和真实输入证据，不是只靠模型或静止姿势通过。502与516没有被伪写成相同；自然运行节奏不同，约束均满足。没有精确记录某次Bell攻击被盾抵消的接触链，不能再额外声称“已证明真实挡击”的细节。

## 普通八项最终资格

全部用现有canonical和SourceRows1856；各案三份原始日志由根代理检查。native毫秒不是玩家游玩时长。

| 阶段 | 20261027目录时间 / UUID | native毫秒 | 结果 |
|---|---|---:|---|
| thruster-stationary-guard | 165534-839 / a703a8ee54cb4afebca80b513ca46707 | 1271 | PASS |
| models | 165609-713 / 5430406344b842cdb7edaa75ce526242 | 520 | PASS |
| keyboard | 165640-169 / 2debb8003b17479ca810470ff39b2710 | 11906 | PASS |
| recycling-traps | 165723-610 / da76cde6216a463784cc9496b272d451 | 28195 | PASS |
| Main | 165820-264 / 1731ced94de24a5986dbdfcd61f37bf9 | 120759 | FAIL：E0死亡恢复 |
| Explore | 170049-653 / 4e90146579c94a48a02af66a1894120f | 137925 | FAIL：骨骸击中、恢复请求 |
| prologue-pre | 170339-516 / 7f3c9bfc00094b919b93a1c814728cad | 157169 | PASS |
| prologue-post | 170648-691 / 2c9d47bd753747229884d905933a9bdb | 116644 | **FATAL / 0xC000001D** |

Main本次未触发这条opt-in post-lure观察分支，不能把Explore钟背证据套给Main。它仍在physics6593被wave Burst伤害2→1，6617被resident Burst伤害1→0，当时能量0，死亡恢复触发原零恢复门。

Explore的新失败发生在Grip1真实抓取成功之后：查询first-hit正确、K被接受，Reel→Swing→上方手动回收→Released→Idle，随后自然下降、尚未着陆时遭`canyon.v2.resident.counterweight-lift.01 / bone-wanderer`的attack1击中。首案7732、复案7747；同一局部94/300处HP1→0。日志的danger=None来自约890帧前的旧战斗决策，不能当作当前没有预警的证据。

Grip1实体`[4260,4500]×[3320,3360]`，完整支撑中心x`[4280,4480]`。该时x4463.868右侧只余16.132px；命中时脚底约3273.797，距台顶仍46.203px。下一步须补真实预警/形状与输入记录，再评估下降段真实盾向，不能先假定固定方向必安全。

## post原生崩溃与身份诊断

普通post最后输出Task8可选路线SupplyCave和ArchiveAlcove成功，尚未完成其余可选路径、原75及owner8。stderr与external均有：

`gchandle.is_released()` → `mono_object_disposed_baseref(csharp_script.cpp:1788)` → `GodotObject.Finalize` → `GC.RunFinalizers`，native退出`-1073741795 / 0xC000001D`。

外层脚本先报告“Post Stdout scope receipts differ”，是崩溃导致后续scope未到达，**不是仅有验证器格式问题**。本轮post不能认证，也不能用上轮通过回填；历史资源根因继续OPEN。

现成`-IdentifyRefCounted`诊断案2386240fd85c4ad7809df3e5f8d90c52在157666ms再次同类FATAL。其分配与时序已被诊断影响，崩溃位置到了seamless与room-profile的两条原WARNING scope之后；不把它当普通资格或原失败的同一精确时刻。

新取得的具体边界：

- stderr共1268条ENTER、1267条RETURN，唯一未配对call1268。准确类型是`Knight.Prologue.Residents.ResidentSubtypeProfile.Dispose(false)`，managed serial850/hash6778409，随后base.Dispose触发FATAL。
- 同一serial/hash只在Operation110配置赋值行及fatal-enter出现。serial由Interlocked递增，可关联**同一托管实例**；原生指针此前被多个不同类型/serial复用，绝不能用地址将它们拼成同一对象。
- Operation110为`low-ceiling-bridge.03`的`scrap-scatter.tscn`，BindingSerial851的一次非空setter，原null gate通过。记录发生在ConfigureActor之前，**不证明完成配置、挂树或提交manager lease**。
- 后来的Operation134虽为同slot/path，却是BindingSerial1206、null/serial0；拒绝后的二次SceneState读取也不能证明其缺失对象就是serial850。
- 该scene中的profile是内嵌`SubResource("Resource_lut5t")`，不是外部`.tres`；外部引用的是profile脚本。没有据此认定资产损坏。
- `native_identity=unknown`来自现有setter记录只保存托管serial/hash，未调用ObserveResource。它不是“该对象已没有native ID”的证明；出生阶段、原生ID、source SceneId、manager/cache世代、挂载/退休和释放时间线仍缺失。

没有改强引用、清理、缓存、资源类、引擎或关闭终结器来让测试过关。最小下一诊断是：纯托管构造日志覆盖source-get前后的阶段；原非空gate通过后对**已有profile**用既有主线程ObserveResource记录身份；在既有manager/cache释放及actor退休边界记录标量关联。不得新增Load/Instantiate/GetState，不从终结器调用native、不按ID重建wrapper、不用强根引用抑制问题充当修复。日志仍可能改变时序，结果不能代替因果证明。

诊断后，正常restore案8748c1c55ab545e1934d4ef5661dbf06以原始无诊断编译models PASS（431ms），三份日志干净，6份Debug文件精确恢复到普通post前的hash。没有把一次models通过升级为post修复。

## 唯一复核与裁决

针对v143确切保存副本的单文件差异，唯一新鲜复核 **C0 / I0 / M2**；未开C/I修复窗口，未二次审查。两项本轮Minor延期：

- **V144-M1**：观察器terminal条件中的Health原生读取在try外；若异常会成为原生回调错误而不是命名fault。严格日志仍拒绝该案，当前未展示假通过，但诊断健壮性不足。
- **V144-M2**：借用helper控制并未直接覆盖新collector的17候选溢出与前观察者抛异常路径；当前代码fail-closed，未来接线回归的负控需补。

v143的次清理异常诊断、独立primary负控等既有延期未被悄悄关闭；也不宣称这里只剩这几项全项目问题。

开发裁决均不是用户另行批准新机制：沿用现有盾向优先于新跳跃补救；保持已有固定平台而非假设掉下后能直接走回；完整来源才推荐方向；保留原K/J、站位与预算；真实后续失败照常FAIL。错误成本由原生支撑/HP/整程门承担，不加输入豁免或削敌。

复核没有认证的后续Grip1修复、完整双路、生产图声/硬件/真人、历史dirty改动/资源根因、v143延期、旧碰撞helper所有权、Bell死亡后的后续路线和真实存档/OS/Git/保管资格，均保持各自边界。新观察器只管理自己的快照和节点，不替旧helper或后续路线背书。逐条裁决见本计划ledger。

## 有界保管、代码与正常版本

只读审计截止2026-10-27T09:42:36.7464236Z：

- 13次本轮尝试：12次构建成功且0错误/0警告；ad92一次构建失败、4条不同CS1503、0警告。12个原生运行6退出0、6非零；所有timeout=false。
- 52对source/helper/user/continuation清单逐字节一致；6484份选定源码副本、142份选定helper脚本副本与各自epoch清单及应选集合一致，共6626份实体副本。没有读取/哈希真实C盘用户数据，只比较保留清单。
- 72份封存二进制与12份各自fresh清单一致，12对fresh/after清单一致；早期RED与诊断二进制不强配普通版本。
- 普通最终八项＋恢复案，共9×1856运行文件/9×896 C#及54份二进制匹配当前；独立当前枚举1856/896无路径差异。
- 当前helper联集14个pin（10个PS1、4个可执行文件）匹配；可执行文件是依赖pin，不冒称完整离线副本。
- 25个owned构建/引擎PID＋CreationTime身份均已结束，未杀进程或检查其他Godot。12案guard为空，ad92只有预期的“没有成功fresh binary pair”。

ad92明确是部分案例：四类清单及before/after选定树齐全，但无fresh/after二进制清单、binaries目录、engine合同/进程/等待/三份引擎日志。没有补造，也不称其运行保管完整。所有保管结论限本轮，不是全历史/全资源离线包认证。

当前源/普通版本pins：

- source清单：`7C6B35D3B7B09EEDF7DC970F37F78729B5B8FE555E8799BA0DFBEAD60F616D27`
- ordinary binary清单：`932D84C4F3A75CBDE1E5244195B0926E41262459ECFBD84B8B33EA912BDECF36`
- Main C#：`CB07B3E59759319DA22042306D55FF23CBD553EC02A2A9EC0C0D6FC8C1D02A6C`
- canonical：`D9BEDF619D282362A2755BF1C6F89FD012AACB760BCAA2D33308A759F1BD0006`
- ordinary Knight.dll：`4AF7BD6BF7D06B56E73FAE0C9CC894E9A19553BF22EB22AC8A8568DF4E657ED5`
- ordinary Knight.pdb：`7C2DC515E68642C8CF2EF408B73952CA3DA6DB41469A25E14D95ED5FFFFADCD2`

诊断案独立binary清单为`C054A3CC3292ED5831A4546689368687BBC6B4901AA1AD95786C23F94774F5AE`，并非当前普通DLL；原件保留。HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，分支codex/task6-certified-final-20260902T0555，index SHA仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。

本轮只改一个既有测试C#，没有新增运行文件/UID；Basin helper、canonical、生产资源、既有143实体/9残片/76普通敌人布局未改。审计后只更新状态文档。

## 下一步与留档

优先补配置资源出生/赋值/释放证据，处理实际FATAL，不能只重复普通post期待转绿。随后补Grip1当前骨骸预警与下降防守证据；Main可先验证**现有**Grip2高层掩体的自然可达性，不应无依据先造新掩体或恢复v142被否定的被动储能银行。

只读几何侦察显示现有Grip2完整支撑中心x4680..4780、y3044，在原遭遇`Rect(3840,2800,1280,800)`内。针对本案actual alternate wave muzzle(4630,3423)与resident muzzle(4533,3459)，保守遮挡内区候选约x4704.13..4780，建议先验证x4720..4760。不是错用primary(4770,3431)的位置；解析投影不是原生安全证明，已发出的尾弹、其他骨骸/幽灵和真实转移仍须验证。现有资料不能断言“没有掩体”或“已经可安全回能”。

本轮没有新图稿/视频。2026-10-27T09:44:30Z手动Check：当前1431609字节/16文件，基线3508、增长1428101，Notify=false、Initialized=false，没有自动化/基线重置。20GiB只随正常汇报提醒，不停工。

完整范围见[原十一项剩余总待办](2026-10-18-project-remaining-backlog-v001.md)。正式视听、连续动作、UI/相机、正常速度/目标硬件、真人试玩、完整双路与交付仍OPEN；未将总目标设为complete、paused或blocked。
