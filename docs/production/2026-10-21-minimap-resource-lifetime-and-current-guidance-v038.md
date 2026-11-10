# v038：小地图所有权修复与当前关卡合同回归

2026-10-21，当前Godot工程 `G:/knight/Knight`。这是V1之后的本地开发改动，不覆盖或移动GitHub V1，不是完整序章、最终画面或发行版本验收。完整1–11目标保持ACTIVE；本轮没有付费生成、前台按键、媒体采集、删除、提交、推送或自动化。

## 实际产品修改

`PrologueMinimapView`原本创建两个ShaderMaterial、Image和ImageTexture，却没有最终释放这四个实例自有资源。新增最终`NotificationPredelete`清理：解绑两个子节点材质及边界层回指、清除材质的纹理参数、释放四个自有wrapper。临时离树不清理，以保留重挂后的缓存；`GD.Load`共享Shader绝不Dispose。

`known_mask`的Texture→Variant隐式参数另持有native引用，清材质参数不能释放调用方临时Variant。现在调用处使用显式`using Variant`，不把这份引用留给GC。未更改小地图尺寸、柔边、探索范围、揭示/遮挡算法或存档格式。

真实测试先后保留了：四资源仍活着的RED；三个已释放但纹理仍活着的RED；纠正观察后的同一RED；最小参数所有权修复后的GREEN。测试也验证另一独立实例、共享Shader、清空表现及离树重挂不被误伤。不加GC、退出等待或日志豁免。

诊断纠正：实装GodotSharp DLL的`IsInstanceIdValid`会经`InstanceFromId`取得/重建wrapper；不能在Texture wrapper已Dispose但Variant尚持有native资源时用它作“无扰动”中间观察。控制现在只在两owner均释放后终检，最终四资源释放合同未放宽。

## 当前测试合同迁移

1. 已批准ECHO的英文/中文标题和hint前缀替代旧“First-Level Guidance/第一关引导”。保留原真实恢复、live改键、语言切换、关闭清除及Objective保护。
2. 当前遍历合同原fixture把五个区域独立实例化，无法解析真实`../CanyonDescent/...`。改用与生产相同的共同父、区域名称和偏移；全部原Validate、Assisted目标、路线节点、正反向边检查保留，不改正式地形或validator。该离树结构测试不是实际玩家通路证明。
3. 原union小地图fixture的完整8×8格位于新版实体顶棚内。只把两处Y从80/76迁到512/508，保留跨x1280、完整格、原2/3帧、真实灯光、卸区及探索保留。新增独立字面格、左右区均不能单独覆盖、真实无遮挡前置；没有让小地图穿墙揭示。

## 同一冻结版本的六项回归

证据根：`G:/knight/验证证据/october13-stable-claw`。每项均fresh rebuild、1695源库存、真实用户目录仅哈希保护、原生普通时间/原预算及退出诊断；无guard failure。

| Phase | 结果/原生ms | 唯一案例目录 |
|---|---:|---|
| thruster-minimap-lifecycle | GREEN / 5993 | thruster-minimap-lifecycle-20261021-154128-544-bd5570696dce434c89fafe1785aac555 |
| keyboard | GREEN / 9586 | keyboard-20261021-154201-076-27150e14924d4b8cad67a3aceca92fe0 |
| actions | GREEN / 7055 | actions-20261021-154236-889-c611a78463324d768f7842b6d8dafd91 |
| thruster-p1-recovery-exit | GREEN / 1437 | thruster-p1-recovery-exit-20261021-154308-004-449baefdf89444238a7533c2d8e8c495 |
| prologue-pre | GREEN / 99767 | prologue-pre-20261021-154337-306-911682517f9d4dc9a934b7b0c7ce8be3 |
| prologue-post | **FAIL / 131442** | prologue-post-20261021-154541-643-188ba3d67ec748f6aa26630dc6a382e4 |

六案source-before清单SHA256均为`8F15E668B6437DE47E083050E125151845E30631C932201E04EA6B52D1C62108`；fresh-binaries清单SHA256均为`D0F2023F884FBD255FFDAF4892EF59D13FD4F75236FA54FF52BF48F558CFF316`。不是跨版本拼接资格。

完整小地图执行原13案加资源所有权1案，共14案通过，包含实际原子写入/失败处理、新局、死亡保留、Continue、暂停、旧观测拒绝、跨已挂区联合及卸载保留。后段post已通过本轮修正的ECHO与跨区结构合同，但首个ERROR推进到`PrologueCompletionTests.PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit:91`：`injected quit releases pause state`。原始记录还含两条刻意负控WARNING；不隐去，不把post称绿。

## 尚未关闭的风险与下一步

- post退出暂停断言待迁移/复验。已核对2026-10-12小地图计划明确“退出不提前Close”；`ProloguePauseMenu`成功保存后保持菜单/暂停，调用原QuitHandler一次。原旧测试仍要求注入退出后解除暂停。下轮应保留原registry身份、一次退出及存档规则，按已批准的新合同验证，不能为了旧断言让游戏在退出前继续跑。
- 另外的P1恢复窄案仍资源严格FAIL。153503-f89ce696的verbose实际列出两AudioStreamPlaybackWAV、两AudioStreamWAV，具名`recovery-end.wav`仍in-use；不能归因小地图或把另一个匿名WAV猜成start。现有Shutdown已Stop并清Stream，start/end均借用缓存WAV，不能Dispose。需只在实际已加载对象处采标量ID对应，不能额外GetStreamPlayback创建wrapper。
- 静态发现恢复cue随后被同一生命周期信号同步清掉，但它与上述退出对象的因果关系尚未证明。应分别写真实公共恢复反馈RED再修复事件顺序，不以消警为由静音、加等待、清全局缓存或Dispose共享WAV。
- 历史五故障、居民正式0/50、24canonical、常驻管理、机关/地图内容、最终美术声音、正常双路/真人/FPS及发行ZIP仍OPEN；本轮不缩减这些目标。

## 独立有界审查

独立只读审查比较保全V1源与当前四个C#文件，并审新owner phase接入；未发现Critical或Important。其范围不是全脏分支或整个游戏。新phase沿用普通55秒、原用户/源/helper/binary保护及严格诊断，不加入waiver。

Minor（推迟）：重挂/另一实例的少量移动未强制触发纹理cache rebuild；后续可增加确定重建输入和upload增长观察。这是覆盖强化，不是当前已证实产品故障；不在本轮冻结中顺手扩改。

审查未判定的全suite、音频、实际穿越/自然驾驶/画面/FPS/真人/发布及全部历史owner改动均保持未通过/未验收边界，不从窄测试推导批准。Git HEAD与原index哈希保持，V1不变。
