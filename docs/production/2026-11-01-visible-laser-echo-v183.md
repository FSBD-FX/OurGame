# v183 — 可见激光接入固定 ECHO 提示

用户已明确允许“仅无窗口构建与测试”，工具目标由用户恢复ACTIVE。继续唯一Godot工程 `G:/knight/Knight` 和原十一项范围；不包含图形窗口、OS输入、真实存档改动、额外崩溃采集、付费或发布。误发视频不使用。旧10-30 blocked记录保留为历史，不继续当作当前许可缺失。

## 事实、推理与风险

事实：v182新增记录在真正headless下发现，枪手物理更新正常、正确玩家HP5、距离160/射程520、实际相机匹配且枪口在1280×720视口内；独立World LOS为false，首撞是保存场景的`Gameplay/Mechanisms/BellChain/Door`。之前只查钟链根位置、没正确计入子门的+150局部偏移，静态“未发现遮挡”推断不足，已由原生证据纠正。

推理：这是UI测试摆位前提错误，不是敌人、镜头、地图或力学错误。保留原5720坐标对真实关门的射线负控，测试枪手再放到同地板/门东侧6000坐标，玩家仍5880；真实门不打开、不移除，敌人LOS/射程/AI不改，原60次等待及55秒cap不变。新增独立墙反例放在两者中点。此隔离UI摆位不等于自然路线或通关证据。

风险：全部新执行为headless语义测试。手动提交真实pre-draw生产者不等于自然渲染，不证明像素、正常速度声音/画面、目标硬件性能或真人认可。历史Finalizer/Handle、完整Main/Explore失败与正式视听要求不因局部通过关闭。

## 生产连接

`FogReadabilityOverlay`只从本批实际已接受的保护副本保存验证/冻结后的帧回执；读取要求同一完整遮挡快照、当前可见且可处理的consumer/copy、CanvasLayer可见、帧未被重写/清除、同世界/viewport。只识别真实PrismLaserBeam/Capsule并按Tracking/Locked/Active取最强值。新批次/Process/Clear清空回执，不缓存敌人Node，不改原攻击/伤害或重新扫描敌人。

`PrologueGuidanceController`在同步FogObservation有效期内传入VisibleLaser；核当前Playing、普通V2、当前mounted sector、world/player/frame/physics/位置身份、HP与HUD占有。无有效证据时回原目标/附爪提示。使用原固定无边框ECHO列、动态InputMap盾键和双语三行短语，不增加世界目标标记，不自动完成教学或保证喷射无敌。

pre-draw文字只保留一次，绑定sector/world/player及实际mounted sector root身份；下一次没有新提交即普通Refresh。旧帧本身不再授权新危险。最终修正先撤销不再有效的卡，再检查语音和保留，Recovery/Completion/Boss HUD不会多保留一帧旧激光卡。

## 实际RED→GREEN与唯一审查

证据根：`G:/knight/验证证据/october13-stable-claw/`，均无ExpectedFailure豁免。

| 时间/案尾 | build/native ms | 实际结果 |
|---|---|---|
| 013311 / c1e3a973 | 无/无 | 操作者漏传SourceRows，旧默认1604导致前检FAIL；未启动build/engine |
| 013503 / 1eb32fff | 25589/8618 | headless前提失败；真实门挡LOS，严格FAIL |
| 013902 / 78370cea | 22008/5505 | 保留门负控/清线摆位后，真实Tracking副本已提交；reader仍None，实际桥接RED |
| 014119 / 220bb17e | 27581/8585 | 首次实际接通后headless专项严格PASS |
| models 014459 / 303650e0 | 23428/395 | 机械爪模型子集PASS，早于后续hold修正，不当最终同版资格 |
| 015126 / cdcd699a | 28874/6743 | 新真实HUD输入反例复现旧卡hold未撤销，RED |
| 015323 / 2ef41fb7 | 24687/8210 | 唯一I1修正后专项严格PASS，包含3个HUD覆盖撤销输入 |

第一案独立核当前source inventory=1870后显式传参1870，未降低核查或修改默认值来绕过。before/manifests/bin缺失保留，不以finally的after副本补造前后不变。

一次最终只读审查C0/I1/M2，唯一一次C/I修复、无二审：I1为单次hold缺少HUD/mounted-root撤权且语音先检查；新增实际HUD Present输入先RED，再改撤权顺序和根身份/存活/挂载复核，GREEN。

两Minor明确延期，不暗改或冒称全覆盖：M1，OnFog对非Basin/旧layout也可能走旧Refresh中的first-combat LOS，不能声称所有路径零额外查询；新laser resolver自身不查询物理或扫描敌人。M2，Locked/Active当前验证reader阶段与激光标题，未独立断言每阶段实际controller文案；多源择强/顺序/部分失效缺构造性反例。三个真实HUD输入验证了卡撤销，语音先撤权的顺序由代码核对，不当正常混音/听感证明。

## 已有保管核查与未完成门

前7案：1前检失败、3原生RED、3严格GREEN；6/6构建成功，6次引擎均为实际headless且无movie/fixed-FPS/超时。30对manifest、2728源实体、60helper实体、36binary逐SHA匹配，12个原始owned身份均结束；被系统复用为conhost的旧PID不当遗留Godot。不读/改真实存档，保护用户inventory109前后相同。

最终同源models：`models-20261101-015439-891-52a2df366cc84c098e8749e8423689ef`，build26348/native376ms，严格PASS。

最终同源pre：`prologue-pre-20261101-015513-328-e94983b8f883487bb3b889b67b10a50a`，build24118/native160362ms，原180000ms内严格PASS，原生日志无ERROR/WARNING/FATAL。本次安静通过不能证明旧Finalizer/Handle根因已修复。

最终同源post：`prologue-post-20261101-015824-165-76416afaed3d4cde8f41ae60f491b59c`，build24460/native197715ms，原300000ms内严格PASS。旧180秒预算不作为本次资格，非性能认证。

最终post另经只读核查：原75 enter/75 return按index0–74有序成对、唯一名称75，executed75/retired0/classified75；owner8次节点1→1，所有原终结marker精确一次。三sink无ERROR/FATAL，原4限定WARNING为seamless1/room-profile1/flow2，重复sink不额外加数，没有新豁免。

全部10案：1前检失败、3原生RED、6严格GREEN；9/9构建成功，9次引擎全为实际headless。45对manifest、4174源实体、94helper、54binary共4322个保留实体SHA全部匹配。18个原始owned进程身份已结束，旧PID被复用的conhost不误杀。前检没有成对证据的after234实体不计成对成功。

当前完整1870源、6binary、最终post的10helper逐SHA等于最终案，无增删/差异。三份改动CS：Overlay `2DC0629BF3EF99DB844770D8824E027E9182BFEACFE697CAD41A9053545ACA3C`；Controller `62E3DC87FD402995CC736EA533B66BC6F66BEA3AB1F75C0BBFD7D7DB4198D724`；test `E0B01EBFC9CE9B7803C102895EB0999FF9D0B0D9C7D55787CD2AC032FAD738B8`。HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index SHA `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未动。

完整Main/Explore、旧资源根因、UI高缩放/正常渲染、正式美术/动作/音频/硬件/真人与原十一项继续ACTIVE/OPEN，非正式交付。既有graphical的ui-guide-style本轮未运行，不能把本专项的固定视图几何检查冒充旧像素回归。没有新媒体或自动化，没有Git写入、删除、引擎替换或额外采集。

下一实际前沿仍是院落跨台的真实危险/应战消费者，以及Main能量储备与原输入预算；新ECHO本身不会替自动驾驶挡弹。保持现有敌人、伤害、地图、普通跳次数及路线预算，用合法输入和新鲜实际危险信息处理，再跑当前完整Main/Explore。此项尚未实现，不能借本轮前后段通过关闭整路线失败。

工作内手动媒体检查2026-10-31T18:02:06.5269170Z：7427750bytes/86files，基线3508，Notify=false；本轮没有新媒体，也没有创建自动化。工具目标实际仍ACTIVE，原完整十一项没有改小或标完成。
