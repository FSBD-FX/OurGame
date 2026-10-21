# 盆地可选回收台：入口、补给与回主线

基线 `9155d2098425676d6d9daf630b8fa319745b771e`。本地开发检查点，不是新交付版本。
外部证据：`E:/OurGame/Knight/.evidence/september08-basin-exploration/`；最终 SHA 与视频哈希见该处 CHECKPOINT.md。

## 事实、判断与风险

盆地 SupplyCave 和 SalvageCache 原来的 Reward 都只是空 Marker2D，没有实际奖励或发现触发。
旧可选路线测试从已经在上层的 PassStart 起跑，只证明横穿与反向行走，不能证明从主线进入上层或实际获得奖励。
本轮真实输入从既有 EntryCatch 高台自然向右下落到 SalvageFloor，再从左缘落回 BasinFloor，路线本身可达。
因此不加台阶、不改跳跃或机械爪数值，仅闭环一处回收台；SupplyCave 仍未填充，不能声称所有支路都已完成。

补给价值是假设：为受伤玩家提供一次至多2格生命的恢复，不把它变成必经任务。满血时保留，需受伤后再次接触领取。
自动路线不证明真人能自然发现它，也不证明2格恢复量是最优平衡。地面向上重新进入这条支路仍未在本轮实跑。
沿用 `generate2dmap` 的玩法/视觉分离要求，使用程序化占位外观；没有新付费生成或正式美术验收。

## 实现边界

- `HealthComponent.RestoreHealth` 返回实际恢复量，发出独立 Healed；不复活死者、不超过上限，不清无敌时间或攻击去重记录。
- `PrologueSupplyCache` 只接受 Playing 中真实 Player 的 Area 接触。以 `salvage_cache` 发现记录保存一次性状态；
  不增加检查点或重排主任务，死亡和 Continue 保持已领取，New Game 重置。
- 先记录消费，再触发同步 Healed，回调后核对原 flow.Save 身份和发现记录；新局不受旧领取污染。
  仅回标题/暂停不阻止原会话保存，Playing 只限制短时反馈。没有实现任意回调异常的通用事务/回滚系统。
- controller 订阅/退订 Healed，驱动既有 HUD 生命格更新。Status 每次读取当前标签，换皮后旧标签仍存活也能正确填入文字。
- `CanyonContentBuilder.BuildSalvageCache` 增加48×40非实体阻挡触发、可替换 AvailableVisual/SpentVisual 与中英状态文字。
  显示“补给 +2 / 满血 · 补给保留 / 生命 +N / 已回收”，英文对应 SUPPLY/FULL/+N HEALTH/SALVAGED。
  用 builder 重生成 CanyonBasin，不手写场景。pack 前后173节点一致；语义比对原111个显式节点及属性不变，仅新增13个补给子树节点。
  资源编号与 unique_id 重排是生成噪声，不是439行地图重构。

## 失败证据与回归

- `health-red.log`：治疗 API 初始空实现，预期恢复1、实际0，退出1。随后单元测试覆盖部分/上限/极大值/无效/满血/死亡，
  并验证原无敌到期点、已处理攻击去重与其他攻击来源。单元中手调计时是隔离测试，不冒称真实运行调度。
- `exploration-red.log`：首版探针把 EntryCatch 路径写为 Platforms，实际在 Collision；是测试准备错误，不是游戏缺陷。
- `reward-normal-red.log` / `reward-movie-red.log`：修正准备后走完进入与返回，生命仍2、治疗0、发现为空，最后因无收益退出1。
- `supply-event-red.log`：真实接触的同步 Healed 观察到 spent=False/discovered=False；预期退出1。
- `supply-new-run-red.log`：同通知中真实 ReturnToTitle→StartNew，reset_clean=True，但旧外层随后把新局发现与磁盘都写成True；退出1。
- `supply-label-red.log`：交易修复后新局已无污染；相同状态替换活旧 Status 时，预期“满血 · 补给保留”、实际空白；退出1。
- `supply-title-mutation-red.log`：临时加入错误的非Playing总返回条件，真实回标题后 spent=True/saved=False，退出1。
  这是保护测试的变异验证，不是原发行版另一个已发生问题；临时条件已移除。
- `pre-final.log`：完整 pre 退出0、ERROR0。新真实场景回归验证2格/缺1格上限、满血不消费、重复接触、HUD、真实落盘、
  死亡回主线检查点、全新场景Continue、新局重置、同步回标题/新局以及Status替换。新局回调后 discovered_after/saved_after=False；
  仅回标题则 spent/saved=True，继续保持已领取。场景自动物理+InputMap，不注入 BodyEntered 或手动调用物理过程。
  预设受伤通过公开伤害 API、检查点通过 Progress 公共接口，是受控 fixture，不称真人打怪或走到检查点。
  WARNING7 = 5次中段Basin出生时旧EntranceCombat目标不在本区 + 2次原故意异常PresentationProfile；未隐藏或过滤警告。
- 独立源码复核未发现剩余 P0/P1/P2；暂停回调、磁盘写失败、回调直接销毁节点尚未专项实测，不能宣称穷尽异常交易。

## 运行与视觉复核

`normal-cn-final.log` / `normal-en-final.log` 均为非fixed/movie普通窗口：实际恢复2格、一次Healed、生命2→4，
回到主线分别为(2368.1128,2013.978)/(2363.7795,2013.978)，12tick落稳、Jump0/Dash0/RecoveryRequested0。
各退出0、ERROR0、WARNING1，为上述中段受控出生的EntranceCombat目标缺失，不是通路启动警告；没有修改生产指导来消音。

`post-final.log` 全部通过，11路段×3起点、四可选支路及原机制/流程测试退出0、ERROR0、原负向WARNING3。
其中旧可选Pass/Skip/反向测试仍从作者标记起跑，不把它当作本轮入口和实际收益的证据。
`route-normal-final.log` 为非fixed/movie五区完整路线，4次恢复/2检查点/冰面拒绝/护盾过热/门/Boss重试/结算通过，ERROR0/WARNING0、无失焦。
回落18下降采样0超边距/0道具栏交叠、稳定12tick。2773进程回调样本 p50=16.661ms、p95=17.115ms、p99=24.907ms、max=55.902ms，
10次>33.3ms、2次>50ms；保留历史峰值，不称GPU帧时或稳定60fps。`--check-ascent-return` 旧观察支段会RefreshNow，不称纯被动呈现。

`build-final.log` 零错误警告；`import-final.log`、普通240帧`ordinary-main-final.log`及plain headless `--quit`均退出0、ERROR0/WARNING0。
两次普通启动仅通过子进程APPDATA隔离 user://，未改主场景/生产项目设置；实际新settings/save写入证据内的隔离目录，原文件时间/大小未变。
所有dotnet/Godot/ffmpeg串行，确认前一个session结束后再下一项；构建/导入没有重写其他扇区。

`movie-final.log` 是 RTX3060 Vulkan 固定60fps，实际同路线治疗2、回主线成功；0错误、同一受控出生警告1。
before 与 after-final 各257帧，视频轨道约4.28秒（含音轨的容器约4.31秒）；6条阶段physics/位置/速度完整记录有序相同，仅证明这一条路线。
主代理审看最终4Hz图板、中英原尺寸入口/领取图；独立审看before/after-final图板、四张after-final阶段原图和英文reward原图。
新增补给、开箱和HUD2→4可辨，所看样本无新增严重遮角色/落点；返程“已回收”文字仍增加注意力噪声，非全60fps/真人验收。

| 视频 | SHA256 |
| --- | --- |
| before.mp4 | B97F49A45ABFF6E20B5A0446BEE7690195047F938AA71398BBB3AC1040549AEE |
| after-final.mp4 | 79CDC03858869FCEE698EE15F5B63379C99FEA70E604468AD309580440766A6F |

探针仅在入树前定位到已存在的盆地入口高台(2380,1568)，之后只发实际A/D；没有入树后位置/速度改写、跳跃、冲刺、关闭敌人AI。
生命2来自一次公开伤害准备，不是实战难度证据。实际向上碰撞支持每处12个不同physics tick；返回数统计RecoveryRequested。
补给在 upper-arrival 截图时已因真实胶囊接触领取，不能把它标为“领取前”。
专项使用独立GUID存档目录，集成测试临时目录会清理；可视探针的GUID存档仍留在 user://，不是磁盘零残留。
固定60fps硬件录像不是性能证明。旧前后4Hz图板、原尺寸阶段图已由主/独立审看；最后灰格含fixture退出，黑格为图板补位。

复现入口（项目根，使用本机Godot4.7.1 Mono及.NET SDK9，输出为外部绝对目录）：

```text
dotnet build --no-restore
godot --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=pre
godot --headless --path . --script res://tests/RunPrologueTests.cs -- --phase=post
godot --path . --rendering-method gl_compatibility --max-fps 60 --script res://tests/presentation/BasinExplorationProbe.cs -- --english --output=<external-directory>
godot --path . --rendering-method gl_compatibility --max-fps 60 --script res://tests/RunRealtimePrologueProbe.cs -- --check-ascent-return
godot --path . --rendering-method mobile --rendering-driver vulkan --fixed-fps 60 --write-movie <external-output.avi> --script res://tests/presentation/BasinExplorationProbe.cs -- --output=<external-directory>
```

## 剩余内容

货台机关目前只移动金色 Polygon2D，未提供可承载碰撞，不能称已完成升降平台或永久捷径。
下一轮先实测它的抓取结果与地图价值，再考虑小范围可选机关原型或清理误导性反馈；不直接塞必经路线。
SupplyCave空奖励、地面返回回收台、补给标签持续存在的视觉注意力、下层敌人透过上层平台的视觉交叠仍需后续判断。
正式美术、独立Boost动作、真人首读/难度、旧认证、解压一致性与真实商业数据仍缺。
不冻结r3、不新ZIP/标签/GitHub上传；保留全部原有无关imports、音频总线、artifacts和旧测试UID。
