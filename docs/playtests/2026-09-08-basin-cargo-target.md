# 2026-09-08 盆地货台误导交互清理

本地小修，基于 `c8ac60ea737d22a063d1bbbd2496eda25ba27abf`。最终提交和录像哈希绑定在外部
`E:/OurGame/Knight/.evidence/september08-basin-cargo/CHECKPOINT.md`；不冻结r3、不新发ZIP/标签或上传GitHub。

## 事实、判断与边界

- 原Cargo只有72×72图形，上移120px，没有承重碰撞、新增路线、发现记录或持久化收益；通用接收器是一次性机关而非持续牵引。
- 货台目标世界坐标(2040,1806)，与真正的BasinGripStone(2060,1800)相距20.88px。主地面与既有可选台面不需要它补齐。
- 检查点位置(2380,2014)的正常输入实际选中货台，但发射在LOS阶段报Obstructed。此次**没有激活货台或发生自动松爪**。
  通用legacy路径会同tick Latched→Released只由源码/既有独立fixture证明，不冒充本次实走结果。
- 单纯加承重碰撞仍没有已验证的目的地，且原抓点不跟随图形。此次选择去掉无效交互，降低瞄准负担，不扩建可选升降机关。
  这属于现有操作纠错，不是新增探索内容或完整盆地上手验收。

## 改动

`CanyonContentBuilder` 不再给Basin生成CargoPlatform接收器/ThreadTarget/Area/shape/adapter子树。
原位置改为 `VisualRoot/Midground/CargoRemnant`，仅普通Node2D与两块低对比Polygon，逻辑和外观依旧分离。
使用项目已获准的程序化占位方式，不生成或采购新美术，不宣称正式资产。

只重建Basin：pack前后均169节点，上一版173。独立场景语义复核确认删除旧7节点、增加背景3节点；
其他原有显式节点的地形碰撞、敌人配置、任务/检查点和补给参数保持。文本资源ID及unique_id重排不是新地图改动。
Q/E、瞄准吸附规则、所有抓附/伤害/跳跃/气力/盾牌数值不变；通用Cargo枚举和四类接收器fixture保留。

## RED/GREEN与控制条件

- `normal-red.log`、`movie-red.log`：旧near坐标选Cargo；硬件录像明确 `visible=False / Failed=Obstructed`，附着0、收绳0，预期退出1。
- `exact-anchor-control.log`：旧版仅将光标移到真实石锚中心，就能保持附着并W收绳，384.96262→324.96262，松手返回，退出0。
  因此不是W键或全局牵引模型坏了。
- `normal-green.log`、`normal-en.log`、`movie-green.log`：新版同旧near坐标选择石锚，1附着/1手动释放，W20tick绳长384.96262→324.96262；
  返回原主地面，12次不同physics tick的支撑采样，RecoveryRequested0/普通Jump0/Dash0/Boost0。不是连续无漏采12tick的保证。
- `BasinClawTargetTests` 通过真正packed Basin与实际物理resolver检查中心、旧货台位置、中点、左邻四个光标：
  选择同一活石锚，LOS=true、非legacy，锚点不变。query固定原点不是InputMap移动；真实输入由上述独立窗口探针覆盖。
- `resolver-baseline-red.log` 使用git archive导出的精确c8ac60e旧Basin文件，仅临时改测试加载路径、不还原生产场景。
  中心控制通过、第二个旧货台位置断言失败，退出1；测试路径已恢复当前res://，重新编译。

探针预入树仅一次放到作者检查点，公开恢复中段LearnShield目标；之后不写位置/速度、不手工驱动模型、不关闭敌人。
使用InputMap事件与鼠标事件，非硬件键鼠/真人。GUID存档与设置隔离，目录残留在user://；finally释放输入、退订并等待场景音频清理。
每个中段窗口存在1条旧EntranceCombat目标缺失警告，不作普通新局启动故障，也没有隐藏。

初版探针编译错用Snapshot.AnchorPoint是测试代码错误，已改为真实属性，不算生产RED。
`exact-anchor-before.log` 初次60tick收绳控制在physics101变Idle；没有Health/Damaged观察，原因未证实，不将它归因本轮缺陷。
后续控制缩为20tick只隔离目标选择与短时收绳，**不证明长时举爪安全、无伤或能登上补给台**。
最终探针另断言释放1次、Boost0、回到Idle；相关最终执行记录列于下节。

## 本轮收尾验证

- `pre-final.log` / `post-final.log` 退出0、ERROR0；警告分别7/3。pre包含5条中段出生导致的旧EntranceCombat目标缺失和2条原负向profile，
  post的3条为原观察者异常、非法房间profile与非法完成态转换。四点resolver、通用四kind能力、11路段×3采样、四条可选Pass/Skip均通过。
- `normal-final.log` 在补齐最终断言后按普通窗口命令运行，退出0、ERROR0/旧中段WARNING1。
  近旁瞄准实际抓石锚；W绳384.96262→324.96262，physics110回Idle/空目标/绳0，最终附着1/释放1/Boost0/RecoveryRequested0/Jump0/Dash0。
- `route-normal.log` 按普通窗口、非fixed/movie的命令运行，退出0、ERROR0/WARNING0且无失焦，五区/四缝、四受控死亡恢复、
  两检查点、冰面拒绝、盾过热、门、Boss重试和结算通过。本轮没有启用 `--check-ascent-return`，不新增回落18样本验收主张。
  2588个有效进程回调样本：p50=16.662ms、p95=17.098ms、p99=27.023ms、max=56.378ms，13次>33.3ms、1次>50ms；
  排除起始120间隔。这不是GPU帧时或稳定60fps/真人验收。日志能检查可见argv与空movie路径，但不能独立证明宿主未改写/遗漏启动参数。
- `ordinary-main.log` 普通窗口240帧和 `headless-main.log` plain主场景120帧由进程工具确认退出0、ERROR0/WARNING0。
  两次各使用仅子进程生效的独立APPDATA目录，避免接触真实用户存档；启动日志只有引擎头，不单凭日志推断时长或玩法验收。
- `legacy-four-role-gate.log` 实际运行未修改的旧Task6审计，退出1，唯一错误为 `requires exactly 4 claw mechanism roles; actual=3`。
  该结果是保留的发行阻挡，不是应该忽略的警告或已通过认证。下方发行边界继续有效。

2026-09-08 07:19 UTC接续周期重新串行验证上述未提交内容，不另增功能：

| 新鲜复跑记录 | 结果 |
| --- | --- |
| build-checkpoint.log | dotnet build退出0，0错误/0警告 |
| pre-checkpoint.log / post-checkpoint.log | 完整两阶段均退出0，ERROR0，原警告7/3及来源不变 |
| normal-checkpoint.log | 正常窗口退出0，ERROR0/旧中段WARNING1；绳长同前，附着1/手动释放1/Boost0，无恢复/跳跃/冲刺；physics111回Idle |
| route-checkpoint.log | 同正常窗口命令五区/四复活/门/Boss重试结算通过，退出0、ERROR0/WARNING0、无失焦 |
| ordinary-checkpoint.log / headless-checkpoint.log | 独立子进程APPDATA下普通窗口240帧/plain120帧，进程工具各确认退出0、ERROR0/WARNING0 |
| legacy-four-role-checkpoint.log | 退出1，唯一错误仍是旧认证要求4、当前3；不修改该门槛 |

新整关采样2590次：p50=16.661ms、p95=17.084ms、p99=28.522ms、max=45.280ms，11次>33.3ms、0次>50ms。
采样和argv限制同前，不删除前次56.378ms峰值，不作稳定60fps主张。所有引擎/构建会话确认结束再开始下一项，末次进程检查无活动Godot编辑器、dotnet或ffmpeg。
主代理重看既有前后6Hz图板；独立代码、场景语义和文档复核无新增提交阻断，旧发行认证缺口不受此结论覆盖。

## 视觉证据

硬件RTX3060、Vulkan、固定60fps，前101帧约1.68秒，后110帧约1.83秒；时长差来自实际返回路程，不是性能提升。

| 片段 | SHA256 |
| --- | --- |
| before.mp4 | `16254F108D7D21D8A47D939ACB18C8F932BEB2921A368578EE9087BBBD28966A` |
| after.mp4 | `060DD1F6BAB965289CDE6FCACA53C09CE60CE98DF9B6B83CC529E18F5F3DC20E` |

主代理与独立审查实际看前后6Hz图板及原尺寸aim/reel，独立另看英文reel。旧亮金方块抢眼，新暗残骸退入背景；
绳连续可见，样本中没有新增遮人物、石锚、平台边缘或HUD文字的问题。近身敌人/手腕环和目标多青轮廓仍局部重叠。
不是逐60fps全片审查、真人易读性或正式美术验收；图板灰格含fixture退出，黑格为补位。原AVI仅外部保存。

## 发行边界与下一轮

活跃关卡结构契约明确改为三个精确保留机关，加无碰撞/无抓取/无接收器的背景残骸，并非删除所有机关断言。
旧 `PrologueVisualAuditTask6.CertifiedClawMechanismRoles=4`、capture/verify/focused-audit脚本和Pester认证仍原样保留。
**当前三类场景不满足旧四类发行门槛**。新pre/post通过不能替代这条门槛，也不能用于宣称r3已认证。
后续需单独明确迁移发行契约，或先设计验证有真实用途的第四机关；不能为了计数再塞回无效目标。

下一主题：实际从盆地主地面用现有机械爪/一次Space助推重返补给台，判断路线与收益是否清楚，保留易走主线。
SupplyCave仍空奖励；机械爪在盆地LearnShield目标时仍显示盾牌卡，模式/指导冲突可在该主题实测后处理。
不重做已完成补给/手环/首爪或盲扩地图。正式美术、真人首读/难度、旧认证、解压一致性、真实商业数据仍缺。
