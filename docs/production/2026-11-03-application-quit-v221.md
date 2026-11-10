# v221 · 正常游戏退出的音频与世界关闭握手

状态：已接生产；唯一审查C0/I3/M1，三项Important均已有实际RED→修补后七个退出专项严格PASS。最终同源15阶段14PASS/1严格FAIL：原world-jet退出仍报告2个ObjectDB实例泄漏；不算整套通过或正式交付，Task1保持in_progress。原十一项、Main899/完整双路、历史资源故障、正式动作/美术/惯性手感、正常速度与硬件真人仍OPEN。

## 事实

在v220当前对象身份基础上，本轮不再重复Stop/Dispose或更换引擎：

- 新增独立终态`BeginApplicationQuit`，在音频同步回调前禁止新播放/发布者重绑/尾音/计数提交；普通`Shutdown`仍能复用，实测启用的音频可重启。
- 实际音频混音时钟发生回绕、再有两个不同process观察、所有播放器静音且Stream解除、自有WAV原ID失效，才返回Ready；上限2000ms，超时/无效观测不是成功。2ms仅为有界轮询让出，不是等待到时即放行；不GC、不查/释放借用播放对象、不禁音求绿。
- 标题菜单、实际应用根的Window.CloseRequested、暂停菜单默认退出处理共用同一准备任务；重复请求幂等。旧场景退出不能恢复/抢占新场景的窗口策略；异常退出也核验捕获的树、请求代次、当前场景与根。
- 退出前撤销排队/正在准备的恢复；下一物理边界、deferred世界重置和普通恢复续体不得重新启用玩法。暂停菜单不能在窗口退出期间恢复/返回检查点。
- 退出后的通关重试、收集、检查点和通用存档入口均在构造候选/写盘/改UI前拒绝。暂停菜单原有的“先保存→失败重试/取消/不保存退出”保持，不新增标题自动存档。

正常游戏的力量、惯性、输入绑定、伤害、地图和资产未重调。退出本身按设计停用玩法处理；这是生命周期修复，不是用户要求的角色动画/物理手感返工完成。

## 主要失败与修补证据

根：`G:/knight/验证证据/october13-stable-claw`，案尾前缀可唯一定位。所有原件保留。

| 案尾 | 原始结果与后续动作 |
|---|---|
| 7839c51f | 首测准备条件失败：标题本来静音；不算目标RED。改由现有公开接口实际播放音乐/UI音效 |
| 47f39305 | 原真实退出按钮留下播放/Stream，NOT_QUIESCED实际RED |
| 45b952c6 | 测试误用WakeHall枚举，CS0117 BUILDFAIL；无native，不算行为RED；已改实际default房间值 |
| 6a0d89c2 | 实际缺少混音/所有权/期限门，GATE_RED |
| dc17fcc3 | 新音频断言全过，但测试末尾自己直接Quit又产生2实例/1资源错误；严格FAIL保留 |
| 54048ab7 | 首次真实菜单退出严格PASS，Ready63ms/5观察；不是最终同源资格 |
| ea61c0c9 / 395f8ce0 | 旧窗口owner退场覆盖新owner、实际Playing暂停菜单绕过Flow，分别实际RED |
| c9bac160 / 961e8da3 | 退役菜单仍能发起全局退出、退出后还能保存，分别实际RED |
| 5b61c522 | 审查I1：旧owner同步回调替换场景后抛错，原catch仍Quit1。单帧存活标记曾先打印但严格ERROR/exit1拒绝；后加强为跨三帧再证明存活 |
| d1ab932d | 审查I2：四场景实际出现新恢复、迟到reset/完成及组件重启，RECOVERY_RED |
| e3a1dd30 | 审查I3：三类直接事务实际接受并改变隔离存档字节，PERSISTENCE_RED |

审查前四退出/三兼容门通过只作中间记录；不能代签审查修补后的最终候选。新host收据消费者也经历实际缺失/新scope/字符串解包反例后修正，当前执行7正20负拒绝控制；不是grep源码自证。

## 审查修补后的七专项（当前）

| 案尾 / 阶段 | 原生ms | 资格 |
|---|---:|---|
| 41e9c6c3 · guard-fault | 4436 | 原抛错回调，替换节点跨三process帧仍存活；旧故障记retired、无全局Quit；再经正常应用退出 |
| b49534aa · guard-recovery | 11944 | 暂停窗口关闭后命令拒绝、先排队再退出、请求回调中退出、实际普通恢复完成后的next-physics取消；无迟到reset/组件重启 |
| 807f5fa4 · guard-persistence | 8887 | 实际通关失败重试、已知残片和有效检查点均拒绝，原隔离save bytes不变 |
| 34a6c972 · contracts | 3218 | 真实Shift/新burn拒绝重启、同步声音重入、普通Shutdown启用复用、自有ID释放、owner取消/preReady、窗口替换/恢复 |
| 16fcf6b9 · menu | 1547 | 原标题按钮＋重复请求＋实际观察回收 |
| adb0a584 · window | 1658 | headless原生Root.CloseRequested信号＋重复请求；不是Windows系统输入证明 |
| 8809e556 · paused | 3966 | 正常完整场景StartNew→Playing→真正PauseMenu退出；保留预先保存，禁止迟到保存 |

七案严格exit0/0ERROR/0WARNING/0FATAL，独立host收据控制为7正20负。不把七门当作全关或正常速度体验验收；其后原回归的实际结果如下。

## 判断与风险

这是实际游戏退出入口的生产改动，不是把旧立即Quit叶子延长等待。原`ThrusterPresentationRuntimeTests.DelayedWorldJet`及last-leaf输入/帧数/Free/Quit未改；它仍是明确的引擎边界诊断，历史严格FAIL不消失。普通world-jet安静一次不能关闭它或原Handle/FATAL。

实际混音/自有ID释放是有根据的放行边界，不冒充直接读取AudioServer内部播放列表或对所有设备/负载的性能保证；正常退出允许最多2秒后显式失败。真正Windows关闭、设备听感/硬件/正常速度/人类试玩仍需独立授权和验收。

## 实现取舍与延期项

1. 按已授权资源/失败关闭范围自主执行，不再次要求大范围方案批准；未扩大付费/GUI/发布权限。代价：关闭在异常设备情况下可能等到2秒并失败。
2. 在现有codex分支原地作业，五个旧文件精确基线、串行native、当前绑定核验替代Git/worktree/commit；代价：保管依赖本地基线/证据，尚无新发布存档。
3. 原立即Quit诊断不改、不屏蔽；代价：不声称整个历史资源门已修复或全绿。
4. 保留暂停菜单原先的存档失败/重试/取消与不保存退出；代价：其正常预退出保存仍会运行，本测试仅写隔离G路径。
5. 新contracts最终走替换后应用的真实菜单握手，旧测试直接Quit的失败保留；代价：额外挂载上下文，不能把它误当旧bare退出通过。
6. Level拥有独立终态锁存、复用既有居民取消版本；代价：终态Level和Flow/audio一样，需要新应用实例，不能重新开始。
7. 只核saved manifest文本、进程合同/结果和最终current文件；代价：不宣称逐字节独立重哈每个封存副本。
8. 审查延期Minor：同步通知没有直接嵌套调用Prepare并比较Task，现有Task身份断言在回调后；源码为先缓存再回调，但新增直接覆盖仍欠账。本轮不补minor、不二审。

完整目标、Main/Explore当前正常路线、动作设计/正式素材/物理操控、GUI/设备/硬件真人及历史资源故障均保持开放；独立审查未评判这些行为，不能借其技术审查代签。

## 最终同源验证与未通过项

七专项之后，同一冻结源码串行完成原八阶段；外层批次63020终态exit1，仅world-jet失败，没有重跑筛绿。构建均exit0/未超时，原build180s、普通native55s/pre180s/post300s上限不改。

| 案尾 / 原阶段 | Build ms | Native ms / exit | 原始E/W/F | 严格结果 |
|---|---:|---:|---|---|
| 81688253 · gait | 28366 | 9260 / 0 | 0/0/0 | PASS；48模型控制、8姿态/80附件/16脚掌、4实际落地与coyote，不是正常速度审美验收 |
| 38e8c877 · rig | 25574 | 2861 / 0 | 0/0/0 | PASS；原八方向 |
| 24d5a60d · world-jet | 28206 | 3598 / 0 | 0/1/0 | **FAIL**；一条WARNING报告2个ObjectDB实例泄漏，failure非空，不因exit0/功能标记通过而放行 |
| e689316f · pre | 25835 | 173929 / 0 | 0/0/0 | PASS；原44项有序enter/return、index0–43，原180s内 |
| 17b761ee · post | 25584 | 205147 / 0 | 0/4/0 | PASS；原75项有序enter/return、index0–74，原300s内，4条限定负控警告 |
| a3de263c · actions | 27736 | 9428 / 0 | 0/0/0 | PASS |
| 175937e7 · keyboard | 25616 | 20087 / 0 | 0/0/0 | PASS；另有独立title-entry 7816ms/exit0、两次挂载/关闭，不含GUI显示或真实硬件按键 |
| 438e1862 · models | 24342 | 526 / 0 | 0/0/0 | PASS；仍只是原爪模型子集，不冒认所有模型 |

post四条警告仍由原精确收据约束：故意抛错的ClawFrameChanged观察者1、非法VisualRoot负控1、Completed→Recovering拒绝2；不是新增通用warning白名单。原75执行计划、owner重复8次/nodes1→1、finalizer/drain/full标记均齐。

新真实退出七阶段与原八阶段合计14PASS/1FAIL。world-jet是原立即Free/Quit路径，本轮没有修改；其非verbose日志未记录本次具体对象ID，不能仅凭“两个实例”就将v220对象ID冒认为本次ID。新的应用关闭流程通过，不等于该诊断或历史Handle/FATAL/资源故障已解决。

### 保存清单与当前文件绑定

- 15阶段75/75保存清单文本对相等：source/user/helpers/continuation各before↔after，以及fresh-binaries↔binaries-after。不是不存在的binaries-before。31个保存owned进程的身份/argv/合同/终态wait相符，全部headless，无fixed-FPS或movie。
- 全15案源码清单同一SHA256：`D38558087367D223112BD0203127C51ADAF6A71881D0E1C430F8FF58B774FD2D`；二进制清单同一SHA256：`3865F476BE535409078F51D925975DF07D3C9F8F01E8E76C6026CE8BC3A5B320`。
- 根代理独立核对当前1884个源码/运行资源文件、17个去重helper、6个二进制，mismatch=0。当前Knight.dll SHA256：`69B7CD76C01DF9A71B01A4CD8AE7D6FB7725FFB3599B49AC52D24245EE168720`。只重哈当前文件并比较保存清单文本，未逐字节重新哈全部封存载荷。
- 当前CIM未见本轮残留KNIGHT build/native/outer进程。分支`codex/task6-certified-final-20260902T0555`、HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA256 `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变；无Git写操作。
- 2026-11-03 12:39:34UTC正常工作流程媒体检查：7,427,750 bytes/86文件，Notify=false/NewThresholds=0；本轮没有新过程媒体，不创建自动化。

### 下一步与完成边界

保留唯一审查的嵌套Task重入直接覆盖Minor，不二审；本轮只完成退出生产增量与证据闭环，不宣布Task1/完整目标完成。剩余资源诊断、Main899和当前完整双路仍需继续。U-20261103-01下R-I可感知惯性、R-C角色专属动作、R-P物理与操控均保持OPEN；后续应以实际速度/停止距离、身体受力与完整连续动作的对应样段推进，再按授权核对正常速度画面和操控，不能用本轮生命周期回归替代用户验收。
