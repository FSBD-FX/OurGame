# v180 — 读取保存胶囊，排除不必要的角色构造

本轮仍为 `G:/knight/Knight` 的原完整十一项目标，没有缩减、换引擎或启动新自动化。上一轮正式抓柱/地图记忆已接入，本轮未改任何生产玩法、几何、素材、数值或真实存档；完整Main/Explore本轮未重跑，沿用已知院落转移失败作为后续问题，不判已解决。

## 判断与实际改动

v179前段错误实际落点是布局测试为复制碰撞胶囊而实例化整名Player。只有保存的几何值是本检查所需，因此直接读取保存图更合适；这不是已经证实的通用Handle根因。

`BasinClawTargetTests.ReadSavedBodyCapsule`现在从借用的PackedScene/SceneState定位根下唯一BodyCollider，核对根/节点类型、非继承/实例边界、启用及identity、单个实际Capsule和有限物理尺寸，再返回自有Duplicate。临时NodePath/StringName/Variant释放，借用scene/state/sourceShape不Dispose、不改动；初始化也进入布局finally清理范围。布局不再为读取胶囊构造Player。原历史60高负控、正式152高抓柱、原33ray/LOS、native72px净空、旧wall真实K牵引及Ice禁抓保留。

新增11个真实Native Pack反例：disabled、position、rotation、scale、skew、root-position、missing、null-shape、rectangle、nested、wrong-root。旧读取器确实接受disabled，真实RED后再改实现；当前全部明确拒绝。实际保存Player读取为radius20/height72，修改返回副本为21后再读仍20/72，不以手造proxy冒充保存值。

继承图、损坏bundle重复属性/nonObject没有可靠构造性负例，拒绝分支不等于这些输入已实测，不报全覆盖。私有test-only接口不是通用不可信场景导入器。

## 实际案例

根 `G:/knight/验证证据/october13-stable-claw/`，原cap未改。

| 案例 | build/native ms | 严格结果 |
|---|---|---|
| `basin-claw-target-20261030-213508-274-f8f8088d1598443cb5ded398361e38f6` | 26851/554 | 真SAVED_BODY_READER_RED：旧方法接受disabled，FAIL |
| `basin-claw-target-20261030-213815-555-2c08c66ca1164ef5a943cd9dcdc93a99` | 30513/1889 | PASS：11controls、保存副本与旧布局/wall/Ice全执行 |
| `prologue-pre-20261030-214102-307-24852facad414f399e11cea0372e3f31` | 35491/81488 | -1073741795/FATAL，严格FAIL，不是超时 |
| `models-20261030-214832-452-75ec2594b691434fa7764ed50998791a` | 31289/562 | PASS，仅机械爪模型子集 |
| `prologue-post-20261030-215650-469-1ad349f86a374950b46a36da15026951` | 36303/215274 | 原300000ms内严格PASS，原75 enter/return与owner8齐全 |

前段先完成新reader/layout及wall/Ice，后在BasinSupplyTitleReentry附近崩溃，记录 `gchandle.is_released()`/GodotObject.Dispose/Finalize FATAL以及Minimap catalog null的ArgumentNullException。输出顺序和阶段接近不证明哪一个先因，也未识别被错误终结的对象。不能因为移除一个触发点就宣称通用资源问题修复。后段只有原4限定WARNING（observer1/profile1/flow2），未增加豁免；不把215274ms改称旧180秒资格。

## 审查、保管与边界

唯一审查C0/I0/M1，无C/I修复轮次、无二审。M1延期：如果awaited teardown本身抛错，其后的两处Shape.Dispose可能跳过；当前无Audio布局正常路径未见该异常，不扩大本轮顺手修。未知继承/raw-bundle负例缺口仍明示。

5案3严格PASS/2FAIL；25组清单对、2334份选定源实体、54份helper实体、30bin逐SHA相同，案内零差，未列/哈希错误0。10个自有build/native PID已结束。当前1869源/6bin/post10helper匹配末案，正式Basin/Parent仍为v179发布SHA `9054F91CAFF1E37AFCAC43C317FB365B061F2592D63E9DBC633EDCB7AA117839` / `5E6AC96A644281846A1B979F154C59E3485B54B07E18EEC68C8C9205C1A201F5`。

HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 未动。无新媒体/窗口/OS输入/付费/崩溃采集/自动化/Git/删除/真实存档操作。完整资源、当前双路线、正常速度视听/硬件/用户认可及原十一项仍ACTIVE/OPEN，非正式交付。

## 既有采集与后续

核查事实修正：唯一获准的旧采集位于 `G:/knight/原生诊断/2026-10-28_单次原生崩溃采集_v001/`，collector.wait.json为DumpCount0、Dumps[]、OneAttempttrue；当时目标post退出0，未复现，**没有dmp可做离线堆/handle分析**。只有12份合同/日志/索引/README，未证明任何wrapper/resource类型。项目PDB不是Godot私有原生符号，后者未验证可得。

已通过非阻塞问题请求新的“一次隔离采集”许可，**尚未收到答复，不视作授权**。未启动采集、调试器或安装新工具，不因这个选择停止其他制作。现阶段可做同案stdout/stderr/external与封存DLL/PDB/源码关联分析，不能从null配置推断错误终结对象。

院落后续的可执行方向已核对：普通Task4WalkTo/ReachSupport没有威胁消费者，FrameObserver仍在运行，不能偷偷把observer当Steer覆盖器；也不应直接搬入900帧整遭遇函数。应在原移动预算/合法J/L/WASD内处理近resident、面对真实授权危险，保留原跳跃和全量敌人。

玩家提示可沿已批准ECHO固定无边框列扩展：仅从FogReadabilityOverlay已通过可见性/同世界/viewport/620范围/授权leaf/当前遮挡快照的实际PrismLaserBeam提交提取Tracking/Locked/Active，不扫未见NPC，不加新世界框、不另查物理、不跨帧保存敌人Node。Controller目前Basin限定需与新语义同步扩展，并保留pre-draw不被普通文字抢写的保护；Context/Model保持Targets空与动态改键文本。短喷不是无敌翻滚，也不保证安全，举盾时不能短喷。该提示与应战**尚未实现**，不拿研究当成游戏增量。

2026-10-30T14:08:52.5775679Z工作内手动容量检查：7427750bytes/86files，基线3508，Notify=false；本轮无新过程媒体，没有创建自动化。
