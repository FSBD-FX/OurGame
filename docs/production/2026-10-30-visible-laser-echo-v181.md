# v181 — 激光短提示模型与无窗口入口修正（功能未完成）

唯一工程仍为 `G:/knight/Knight`，原完整十一项目标保持 ACTIVE/OPEN。没有新增美术方向或采用误发素材。本轮未改伤害、敌人数量、地图、机械爪排序、物理、输入或存档规则；这不是新的可交付版本。

## 事实、推理、风险

事实：CN/EN 的三阶段 ECHO 短提示模型已有一次真实执行通过；实际可见性桥尚未实现。`FogReadabilityOverlay.ReadVisibleLaser` 仍返回 None，GuidanceController 也未接通激光提示。新真实场景测试先在 60 次原有限等待内未达到未锁定 Telegraph，尚未触及预期的桥接失败断言，不能把这次失败记作可见性功能的 RED。

推理：应先区分实际 Camera/可见视口、授权来源与 LOS 等前提，再验证桥接；不能通过延长等待、摆拍攻击、削弱敌人或改变原路线预算取得通过。现有测试使用保存 Temple、手动 pre-draw 提交和测试内来源隔离，不是自然探索、正常速度画面或真人验收。

风险：新增 phase `ui-laser-guidance` 意外匹配旧的 `StartsWith('ui')` 图形分流。下列三次实际 engine 均没有 `--headless`，而是 GL compatibility/1280×720/position40,40。没有取得针对这些新显示的许可；不得继续声称它们是无窗口执行，也不证明用户看见或认可画面。发现后已停止进一步游戏启动，原进程均结束；未发送系统键鼠。本轮只做不启动游戏的参数测试和本地修正，不把自动目标续跑或素材撤回当作显示/新采集授权。

## 已有四案（保留原始失败）

证据根：`G:/knight/验证证据/october13-stable-claw/`。

| 案例 | build/native ms | 实际结果与显示方式 |
|---|---|---|
| `ui-laser-guidance-20261030-222944-197-dffaafac80314170babed62f02178fac` | 32863/2165 | 原短提示模型 RED，graphical，严格 FAIL |
| `ui-laser-guidance-20261030-223147-557-da771854b69b44ad923976266b95fbae` | 44659/3524 | 当案模型合同 PASS，graphical；不是后来追加的完整语义合同通过 |
| `ui-laser-guidance-20261030-224851-714-4d762223442f45d4829aa1251b3d59df` | 29532/无 | CS1061/CS0122，build FAIL，无 engine、无 fresh binary pair |
| `ui-laser-guidance-20261030-225243-682-dba710650e8d480d8c27d7e82a19d425` | 30481/10334 | fixture 前提未达状态，graphical，严格 FAIL |

均非超时，build/native 原 cap180000/55000ms 不改。模型通过仅证明双语、三状态、改键字形、空世界标记、附爪时提示优先以及 Boss/禁用等模型边界；不证明实际玩家能看到该提示。

四案只读保管核查：19组清单对、1812份选定源实体、40份 helper 实体、18份 bin 逐 SHA 一致，案内 source1870/helper9/continuation1/user109 前后零差，未列/哈希错误0；7个自有 build/engine PID均已结束。构建失败的 fresh binpair 缺失原样保留，不补造。当前 helper 已修正，不再宣称等于旧案封存 helper。

## 只运行纯 PowerShell 的安全修正

改动限定于 canonical 和新参数回归脚本：

- `ui-laser-guidance` 在广义 UI 分流之前明确选 `--headless`，使用 `-ieq` 覆盖 ValidateSet 接受的大小写变体；仅该 phase 的 runnerPhase 归一成实际 C# runner 所需的规范拼写。
- `Assert-LaserSemanticLaunch` 紧邻最终 `Run-Owned 'engine'` 之前检查已经完成全部追加/整体替换的真实 argv。只约束新 phase，旧 UI、硬件、电影和普通路线分流不改。
- 必须恰好一个精确 `--headless`，位于唯一精确 `--` 之前；引擎参数只允许唯一 path/script/log-file、headless和可选唯一 verbose，runner 必须精确匹配。窗口、显示、渲染、录像、帧速等其他参数，不论分离/等号/大写形式都拒绝。用户参数只允许精确 phase、非空隔离 storage 和既有两种 trace 标记，重复/未知参数拒绝。
- 新 `test-laser-launch-headless-v001.ps1` 从实际 canonical AST 只提取 runner/argv 构造与纯 guard 函数。构造片段唯一允许的命令为 Join-Path，guard不得执行命令；不执行整个 canonical，不调用 Run-Owned、构建器或引擎，也不启动窗口/采集。

实际 TDD 日志在 `.superpowers/sdd/2026-10-30-visible-laser-echo/`：

1. `launch-red-selector-v001.log`：旧分流真实输出无 headless，`LASER_HEADLESS_SELECTION_RED`，exit1。
2. `launch-red-guard-v001.log`：专用分流修正后最终 admission guard 缺失，`LASER_LAUNCH_GUARD_RED`，exit1。
3. `launch-green-v001.log`：8阳性/58拒绝/6旧分流对照，exit0。独立只读审查也执行同一纯PS脚本得到相同结果。

阳性包含四种 phase 大小写、各带/不带 verbose；拒绝包含 headless/分隔符缺失重复错位、大小写错误、显示渲染电影等参数、必需值缺失空值重复、错误 runner/phase/storage、未知用户参数及绕过早期电影许可检查的构造结果。旧分流对照包括2个 GL UI、2个 headless、原 Vulkan movie 和 Windows route 整体替换。这里只证明参数行为，**没有新的 Godot/build/pixel/硬件/真人执行**，不报完整游戏回归通过。

## 独立审查

本安全切片一次最终只读审查 C0/I0/M1，无必须修正项、无二审。M1延期：纯PS测试独立调用真实 guard，但未另以 AST 结构断言钉住最终 engine 调用前的 guard 接线；未来删除该调用仍可能测试 GREEN。当前实际接线已经审查，位于所有 argv 变动之后、紧邻 engine 启动，后无追加/替换。该 Minor 不等于此接线已有运行旁路，也不证明完整 ECHO 功能通过。ECHO 功能本身尚未进入最终审查。

## 状态与下一步

激光实际桥接、fixture 前提诊断、既有 UI/模型/前后段/两条完整路线仍待继续；历史 Finalizer/Handle 及完整主线、探索线失败不关闭。正常视听/UI/动作、正式美术、目标硬件与用户试玩也仍 OPEN。当前1870源，参数修正后未重新构建/运行引擎，不能拿旧二进制或旧模型 PASS 冒充最终同版资格。

HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6` 与 index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 未动。没有付费生成、额外崩溃采集、自动化、Git写入、删除或真实存档改动。上述三次非预期图形启动必须保留为执行偏差，不被后续本地修正抹去。

2026-10-30T15:22:10.3750254Z 工作内手动容量检查：7427750bytes/86files，基线3508，Notify=false；无新过程图稿或视频，没有建立自动化。
