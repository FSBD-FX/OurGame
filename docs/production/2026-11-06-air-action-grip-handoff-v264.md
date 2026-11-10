# v264 · 短喷到期时的握持、举盾与收势

本轮只增加已有动作的回归测试，不改游戏动作、物理或素材。此前 v261 M1 指出稳定动作样本没有证明副手在动作切换时实际松开和重新握住；这轮增加两条互补证据。原资源崩溃和序章验收仍开放。

## 新证据

- **保存角色几何，6 例。** 左右朝向 × 30/60/120Hz，非零短喷角度/骨盆与 secondary 存在时切为普通空中。相同动作的零载荷角色对照分离姿势与载荷；在举盾时允许 authored 姿势变化，不把它误判为载荷跳变。观察主手贴武器、副手沿真实自由腕—图像握点逐步松开、到自由腕 `(0,19)`，在交接中段切 None 后逐步握回。重复时间戳检查十槽、足踝/脚底、双腕/肘和 contact flag 不前进。
- **真实保存玩家内部键盘，2 例。** 原无地板 Player 夹具、原控制器、A/D+Shift→晚 Coast 的 J→到期后 L，左右各一次。由真实 AttackStarted 证实横向攻击；到期相邻两发布帧均 AirAttack.Recovery、同 Fall、正时间、非零旧载荷，双髋局部几何测点步差 0。之后实测 Shield active、自由副手、L-up、Shield.Recovery→None 和实际副手回握；各自 164 个发布样本，边界前一帧至回握主手最大间隙 4.5935038e-5 / 3.5014265e-5px。一次 Shift、零跳、无退款或观察异常。物理根仍下落，未拿世界脚静止冒充连续。
- 原无动作自然双朝向测试和既有 ground-gait 阶段保留。前一证据证明模拟动作交接与副手过渡可重叠；后一证据验证真实控制器发出的顺序，**不声称自然 Shield.Recovery 正好跨短喷到期**。是游戏内输入事件、非系统键鼠、非真人或正常速度视觉验收。

## 执行、保全与边界

canonical `thruster-ground-gait -SourceRows 1898` 严格执行两次，第二次是加入真实输入分支后的不同测试源码，不是同输入重试。证据根 `G:/knight/验证证据/october13-stable-claw`：

- `thruster-ground-gait-20261106-042331-246-d08c93bab7c74ff788bc61f3e27b4fe9`：16061ms，6 controlled + 既有自然分支 PASS。
- `thruster-ground-gait-20261106-042741-345-c06ead157d76454ba0d99478ddf8b748`：Rebuild34942ms、native18263ms，完整阶段 PASS；stderr 空，宿主无保护异常。新增 `AIR_ACTION_GRIP_HANDOFF_OK` 和 `AIR_ACTION_NATURAL_OK` 及原通过标记均到达。

逐项复核：两案10组前后清单0差异，13 helper逐案复核，12封存二进制及当前6输出一致于各自清单，当前1898源均匹配最终案。与v262普通输入相比只变三份测试源码（先OrdinaryAirMotionRuntimeTests，后再加ThrusterPresentationRuntimeTests/RunThrusterCoreTests），没有生产或资产哈希漂移。最终源清单SHA `4EC74DD01EBC98A792FB9FC8E8541B4F7C090B76E5CB306F3943FED8128A384D`，stdout SHA `5B4E58DFDBF55B38CC963F9D69321F657B760A3D3930C9A9C7ECFC2B41B476DD`。

测试现状通过，没观测到需要修的新增生产问题，不伪称 RED→GREEN 或动画重做。唯一独立只读审查 C0/I0/M0，限定测试可保留；未独立重跑。测试通过并不代签正式分层、动作风格、手感、正常速度视听相机、硬件或用户试玩。已知普通后段仍有崩溃，v261宽门资格不转授；本轮没有重跑完整序章。全部裁决/限制见 `.superpowers/sdd/2026-11-06-air-action-grip-handoff-v264/progress.md`。容量单次检查10,491,889 B/97项、Notify=false；无新增媒体、开窗、系统输入、付费、自动化、删除或发布。
