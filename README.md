# knight

## V1 当前源快照（2026-10-21）

版本标识：**V1**，对应独立归档分支 `codex/knight-v1` 与 [GitHub V1标签](https://github.com/FSBD-FX/OurGame/tree/V1)。本地规范工程为 **`G:/knight/Knight`**，资源总目录 **`G:/knight`**。V1是当前开发源快照，不是完整序章或最终视觉验收；[归档范围、启动与已知问题](docs/releases/V1.md)均明确保留。

迁移最新事实：[v037](docs/production/2026-10-21-knight-consolidation-completion-v037.md)。用户已手动清理16个V1媒体ZIP（137.77GiB），恢复索引仍在；历史树1,075,455普通文件/126.38GiB全量双侧SHA与目录集合一致，随后28个历史工作树/11个测试Junction已重连并读回。旧E副本与4个小快照ZIP仍保留，**没有把这些说成已经删除**。新路径环境、Git完整性、短喷模块专项通过，先前键盘专项通过；无新玩法修改或真人长测。完整1–11目标仍未完成，长期目标工具此前为usageLimited，不冒称完成，也不建立自动化。

Git源快照不包含工程外引擎/SDK、完整历史证据与旧媒体；新本地验证owner仍被既有.gitignore忽略。下方v036/v035等是原时点历史，不代表那些已被更新的“V1尚未清理”等状态仍有效。现有资产表继续保留。

## 最新存储状态（2026-10-21 v036）

用户暂定项目名为 `knight`，统一总目录为 **`G:/knight`**，当前 Godot 工程为 **`G:/knight/Knight`**。入口是总目录的 `启动knight.cmd` 或本工程的 `Play-Knight.cmd`。当前工程已同卷移动并核对5,159文件全部SHA256、Git HEAD/index/25 refs/未提交状态一致；专用工具、当前证据和历史依赖也已移入新总目录，启动器改用相对工具路径。新验证入口为 `.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1`，仅作原owner的9处根路径机械映射，旧owner保留。

E旧历史资料正在复制至 `G:/knight/历史资料/E-OurGame-Knight`；尚未以复制冒称迁移清理完成。用户要求删除其他备份，但工具拒绝批量和单个已核验ZIP删除命令，这一轮尚无备份ZIP被删除。10份已变化的旧工程文件已从旧包单独保全并校验。新媒体目录是 `G:/knight/过程图稿与视频`，旧文档中的 `G:/game` 是历史路径。详见[统一命名与迁移v036](docs/production/2026-10-21-knight-consolidation-v036.md)。游戏整体目标仍未完成；此轮暂停玩法改动处理存储，不建立自动化，不触碰其他UE项目及真实存档。

## 当前推进目标（2026-10-21；完整1–11未完成，工具显示usageLimited）

最新：[V1上传与历史原件清理v035](docs/production/2026-10-21-v1-upload-cleanup-v035.md)。用户确认上传后，仅删除与V1逐文件SHA一致的E旧107,301文件（148.87GiB），本地17个V1 ZIP及其他4个工程备份保留；当前G工程、依赖、存档和E其他证据/工作树不删。空清单收据错误已如实保留，依据原日志独立补核，没有重跑删除。用户同时明确本聊天继续**G:/game/Knight的Godot工程**，不切到共享AGENTS提到的另一UE工程。规则仍为关键节点留档、20GiB只随汇报提醒、不停止、不建立自动化。

历史留档范围：[v034](docs/production/2026-10-20-key-milestone-retention-and-archive-scope-v034.md)，其中原件保留/等待上传为20日时点，最新清理事实以v035及V1新确认收据为准；冻结ZIP及原收据不回写。长期目标工具`usageLimited`并非20GiB或用户暂停，不能冒称ACTIVE，也不建立自动化绕过；本轮依用户消息继续制作。

当前主目录已按用户要求迁到 **`G:/game/Knight`**。详见[存储盘点与迁移v033](docs/production/2026-10-20-storage-inventory-and-g-migration-v033.md)：原项目275.25GiB主要是媒体148.87GiB与证据104.22GiB；当前开发快照约322MiB。源码/资源/dirty文件、Git与必需工具已复制校验，后续新验证输出在`G:/game/验证证据`，常用新入口为`.superpowers/sdd/2026-10-13-stable-claw/verify-claw-g-v001.ps1`。六组旧缓存/旧包约2.46GiB已备份校验但**尚未删除E**，大历史档案尚未整体迁移。G fresh build与module/keyboard GREEN，恢复退出仍匿名资源strictFAIL；不以迁移冒充游戏完成。后续勿在旧E与新G同时开发；从本目录`Play-Knight.cmd`启动。真实用户存档位置未改。

当前最新：[恢复退出边界与回归v032](docs/production/2026-10-20-recovery-exit-lifecycle-v032.md)。两种公共完成回调退出先真实触发GetTree/ToSignal生产错误，最小guard后专项GREEN；同源六门4GREEN/2FAIL，普通P1仍匿名资源退出FAIL，完整post首错推进rift-to-abyss复合结构条件，未认作真实驾驶落台失败。全部历史故障与完整目标继续OPEN。用户已要求优先盘点占用并保存后迁移未来开发内容到G:/game；现无游戏进程，不删除E原件。

当前最新：[旧教学几何的版本隔离与后段回归v031](docs/production/2026-10-19-legacy-geometry-version-isolation-v031.md)。原Task8的V1偏移/十一教学段误读正式V2，现明确读取冻结兼容场景；保原96与十八方法/75case名，不改生产地形。窄原生十八兼容合同＋独立V2结构GREEN；完整post130681ms仍FAIL，已过原seam2，首错转到P1发现保留的死亡恢复同帧断言，另有真实Handle异常，根因未定。新Main死亡在同案再次通过但不能冲销P1失败。完整1–11、Task6/7、正式0/50/24canonical、历史资源/最终内容/正常双路/真人/FPS/交付继续OPEN；未重开窗口。

阶段历史v030：[死亡恢复生命周期与检查点](docs/production/2026-10-19-death-recovery-lifecycle-and-checkpoint-v030.md)。保原死亡前提实测及cleanup-only RED，修正旧输入/检查点测试预期，窄GREEN与同冻五门4GREEN/1FAIL；当时seam2根因未定的原件保留，后续由v031更新，不回填旧资格或冒称生产地图修复。

阶段历史v029：[用户确认标题可见](docs/production/2026-10-19-user-confirmed-title-visibility-v029.md)。独立隔离入口停正式Title，用户明确“能看到标题页”，没有自动Start/长路线；窗口已自行结束。此资格仅为标题显示，不是角色动作/玩法/画面质量/真人/FPS或长测授权，旧两轮未见与失败原件不回写。

阶段历史v028：[遥测分类、重置观察与显示未通过](docs/production/2026-10-19-telemetry-and-seamless-authority-v028.md)。保留用户两轮未见、精确typed遥测合同及六门5GREEN/1死亡恢复真实FAIL/2 ObjectDB原件，不追认旧显示；v029仅新增用户标题确认，不改历史资格。

阶段历史v027：[实际窗口启动与双路线复验](docs/production/2026-10-19-visible-window-dual-route-v027.md)。保留当时实际返回Knight(DEBUG)/置前、新Main严格GREEN与Explore原遥测误判strict FAIL的原件；其中“用户尚未确认”由v028更新为用户明确未见，不追认已展示或双路GREEN。

阶段历史v026：[同高落台、神殿准入与窗口显示](docs/production/2026-10-19-thruster-coplanar-top-and-temple-admission-v026.md)。两测试消费者RED→GREEN及原准入null负控/pre通过；同冻headless Explore278885ms完成Boss/支撑出口/heldD60/恢复0。post仍laterObserver expected1 actual0并有RID/ObjectDB诊断。保留当时normal启动选择已改、实际窗口未验的时点；最新由v027更新，旧Root.Visible不是用户已看到。

下方v025是原时点历史；其中“正常窗口”不再解释为已向用户展示或显示通过，原件不回写。

阶段历史v025：[旧高层碰撞准入与正常窗口复验](docs/production/2026-10-19-thruster-legacy-high-collision-and-contact-v025.md)。保留三原出生81/76/72帧及33mandatory/四optional通过、裸Boss前提失败、旧Main路线完成和Explore多跳FALL RESET失败的原件。后续用户明确没看到窗口，Hidden启动与实际可视复验由v026–027纠正，不追认旧显示通过或回写旧证据。

阶段历史v024：[导弹自动操作实际快照隔离v024](docs/production/2026-10-19-thruster-missile-snapshot-isolation-v024.md)。保留三导弹夹具及预测／键盘／动作／pre通过、旧高层落台FAIL的原时点；当前高层及正常路线状态由v025更新，不回写旧证据。

阶段历史v023：[神殿补给架与后台完整双路线v023](docs/production/2026-10-19-thruster-temple-supply-and-headless-dual-route-v023.md)。保留原十二硬检查／残片保存治疗返回通过、同冻headless Main／Explore Boss／支撑出口／heldD60通过及候选匿名FATAL原件；这是上一冻结资格，不代替当前正常／真人或资源验收。

阶段历史v022：[蒸汽下落、上行与捷径回返适配v022](docs/production/2026-10-19-thruster-steam-and-ascent-handoffs-v022.md)。保留当时安全下落、Top／Grip1/2／双向捷径通过与补给架复合断言失败、最新源尚未跑Main的时点；补给架及后台双路由v023更新，旧原件不回写。

阶段历史v021：[蒸汽战斗与正常窗口复验v021](docs/production/2026-10-19-thruster-steam-bell-and-window-revalidation-v021.md)。保留当时正常Main成功、正常Explore更早下落死亡及诊断API修复；正常死亡唯一来源仍未闭环，不能借v022后台安全路径冒称正常已修复。前台时段已释放，旧原件不回写。

阶段历史v020：[正常主线通关与蒸汽支路适配v020](docs/production/2026-10-19-thruster-normal-main-and-steam-branch-v020.md)。当时正常Main严格GREEN199703ms、Explore仍在下降带外护栏失败；释放窗口后入口／Valve通过，但Steam接战HP0来源未记录。上述来源、后续战斗和新的正常下落失败由v021继续，不回写旧证据。

阶段历史v019：[短喷主线诊断通关与窗口双路验证v019](docs/production/2026-10-19-thruster-main-completion-and-window-validation-v019.md)。当时headless Main严格GREEN197908ms，Explore在阀前170/170落回PistonOne，正常双路仍FAIL；旧失败和尚未执行阀门的时点保留，由上方v020更新当前状态，不回写旧证据。

阶段历史v018：[完整路线后段与惯性站位适配v018](docs/production/2026-10-19-thruster-full-route-and-native-stance-v018.md)。实际Title／同Player的headless双路已迁移Archive离台、Gear零步举盾、Frozen防御／站位、Basin控制站、Lift诱敌及镜像上行。最终同冻仍两严格FAIL：Main已过九段上行、Scatter／Burst／Laser与Split，首错末端Grip9共享90停靠预算；Explore已无伤过Frozen下组、716高度1伤回Camp及Basin升降／横廊，首错Gear Bell跨await预检选错地形。未到Boss，不是正常显示／真人／60FPS。8版本／15原资格纯合同过、保护109保持；安全Frozen K分支本案未执行，不借Main Moth证明。生产Player／地图／美术没改，Task6音频、Task7／正式0/50／全部最终内容与1–11仍OPEN；E盘约7.6GiB，已询问后续新增证据存G但未迁移。

阶段历史v017：[正常准入与峡谷惯性落台v017](docs/production/2026-10-19-thruster-normal-admission-and-descent-native-v017.md)。正常launcher按实际1695及18依赖重新绑定，原保护／资格门保留；用户前台时段中Main／Explore均严格失败于旧惯性路线假设，尚未到Boss。随后两窄行为RED→GREEN：同90预算真实反刹、完整支撑停稳；原120离台内准备安全速度，release后全neutral落ArchiveFloor1440.0554。同冻三门GREEN20573／680／9625ms，非正常双路线、真人或整关验收。音频最后原叶可独立复现2 ObjectDB，同源安静退出不冒称修复。生产Player／地图／美术没改，完整1–11、Task6/7和正式资源／交付仍OPEN。

阶段历史v016：[哨兵中性制动与原时限v016](docs/production/2026-10-18-thruster-sentry-neutral-and-clock-native-v016.md)。主／备用测试路线分别真实RED→GREEN：gap83.499756旧2800仍持A／vx−260，实际1800应松A／vx−230；现读取Ready不可变快照并验证fresh Input及完成Motor。原global1215／first-K73 cap保留，新推导1219／76分别公开取min，不扩大等待。最终同冻两门GREEN，17728／672ms；两条受控路线各3K／2J、HP5，非正常Main／Explore或真人证明。生产Player／敌人／地图／美术未改，旧高层碰撞及音频2 ObjectDB仍OPEN；Task6/7、完整1–11、正式资源／正常双路线／真人／交付未完成。

阶段历史v015：[旧教学助跑与真实支撑预测v015](docs/production/2026-10-18-thruster-legacy-runup-and-support-forecast-native-v015.md)。原下行`.2`配重遮挡已逐帧定位；保原出生／LaunchZone／EntryShelf，实际区内助跑并从原循环budget扣除后通过，不改全局遮挡或地图，也不称原静止立即跳路径已修复。四assisted开始使用实际Motion／Module、平台顶和完整胶囊预测；最终同冻4门2GREEN／2严格FAIL，post70724ms已过9视线、4回返与前7段21样本，首失败推进到ascent_grapple_three `.2`中途撞回返天花板后掉落。摆荡替代策略失败已撤回，碰撞可达性／11文件全量迁移仍OPEN。无新媒体或生产美术改动；完整1–11、Task6/7、正式资源／正常双路线／真人／交付未完成。

阶段历史v014：[旧教学入口与拒绝诊断v014](docs/production/2026-10-18-thruster-legacy-contracts-and-diagnostics-native-v014.md)。已迁移Upper高层fresh键盘样本、结构化验证Profile真实拒绝，并修正离树Motor诊断路径；旧教学与V2主线明确分离，查询失败时释放自有fixture。诊断／兼容冻结8门7GREEN／1FAIL；随后旧教学冻结5门4GREEN／1FAIL，pre再次严格GREEN103185ms，post已过9视线与4处回返，首失败推进到legacy descent_grip_lesson .2样本被配重TargetCollision遮挡（239/240、1cast/0latch）。本次post没有退出资源诊断，但历史Handle/RID/FATAL与短喷音频2 ObjectDB仍OPEN；完整1–11、Task6/7、正常双路线／正式资源／真人／交付均未完成。

阶段历史v013：[短喷快照与原路线迁移v013](docs/production/2026-10-18-thruster-route-forecast-native-v013.md)。已公开实际不可变运动快照，加入区分空阻／摩擦／反刹的离散预测；三条fresh辅助路线和真实Door／原支撑Bell局部fixture通过，原平台／zone／150tick／HP不改。source1695同源16门12GREEN／4严格FAIL：world-jet仍退出2 ObjectDB、compatibility仍两Profile负例警告、pre已过lower_to_mid但推进到下一mid_to_high旧预测失败、post已过Bell但推进到GripStone旧节点缺失。Task6/7及完整1–11仍进行中；不是正常双路线／最终视听或真人验收，历史资源故障不据未复现关闭。

阶段历史v012：[短喷世界喷焰与独立声音v012](docs/production/2026-10-18-thruster-world-jet-and-voice-native-v012.md)。实际保存Player已接肩核世界喷焰、锁向反喷、独立SFX voice及唯一journal消费，长模块／尾声／低闪等行为通过；当时source1692冻结17门13GREEN／4严格FAIL，音频自有WAV额外引用尚未修复。旧证据保留，不覆盖上方当前失败入口。

阶段历史v011：[短喷姿态与斗篷固定带v011](docs/production/2026-10-18-thruster-rig-and-mantle-native-v011.md)。当时左右倾身／余速／反刹、J／L／死亡优先和原两mantle几何通过，16门12GREEN／4FAIL，喷焰／声音当时尚未接入；旧证据保留。

阶段历史v010：[短喷观察与首段教学v010](docs/production/2026-10-18-thruster-observation-and-teaching-native-v010.md)。唯一身份观察、双语操作／暂停／真实出生教学已接；当时冻结13门11GREEN／2FAIL，另追踪版普通三门过。该历史绿色不覆盖v011教学退出泄漏；Audio开关／标量追踪未确定FATAL原因。

阶段历史v009：[推进模块保存与正式接入v009](docs/production/2026-10-18-thruster-saved-module-native-v009.md)。Task5按brief专项完成：source1689候选同源12门通过，备份后采用真实builder标准模块原字节，正式版5门再过；八参数显式保存／搬移读取／实际两模块运动、legacy诊断、站稳与抓爪拒绝已验证。无拾取／背包／UI换装，正式Player原字节继续保持。

阶段历史v008：[装配原生v008](docs/production/2026-10-18-thruster-equip-native-v008.md)，source1689同版六门通过。当时仅值事务/支撑和部分资格，Task5未完成、正式Player未覆盖；该历史状态由上方v009更新，不回写旧证据。

最新进展：[着陆边界补齐v007](docs/production/2026-10-18-thruster-landing-native-v007.md)。短喷计划Task4在brief专项范围完成：21新自动physics场景、旧13摔伤及相关专项通过；低冲击111免落地伤后，真实电磁机关仍扣血、KillZone仍发恢复请求（仅请求observer，不称完整复活）。v006冻结16项14GREEN／2FAIL保留，旧Upper及Bell/Recovery未关闭。下一Task5保存模块与真实站稳装配；整计划和1–11未完成。正式地图/重力/普通跳不改。

Task3已在brief专项范围完成：33自动Physics＋1scheduled对照、预算11自动键盘＋2scheduled通过，pending旧借力不再覆盖新ExternalImpulse。其source1685十八项16GREEN／2FAIL及pre七Handle观察（根因未定）保留；新pre暂未再出Handle也不算关闭。Task4新增准星/探针问题已复现修正，五历史FAIL仍OPEN。其后保存装配／typed视听／旧消费者与正常双路线；旧深井主动刹6帧4.633秒不冒称被动松键路线。

完整1–11工具目标保持ACTIVE、未缩减；总范围见[剩余总待办](docs/production/2026-10-18-project-remaining-backlog-v001.md)。正式新常驻仍0/50、原地图127／候选132、24canonical及全部正式资源／最终UI／正常双路线／旧档／真人／60FPS／Windows ZIP／GitHub版本交付仍待完成。当前新增快照与路线预测／验证接缝和证据，无新图像或音频文件生成、展示媒体、付费、Git写入或自动化；已有修改与旧版本均保留。

## 阶段历史（以下保留旧时点，不能覆盖上方当前状态）

最新阶段：[输入与预算原生证据v001](docs/production/2026-10-18-thruster-input-and-budget-native-v001.md)。实际InputReader原始事实及纯pulse预算/身份已实现，fresh pulse/input/module/dynamics/gravity/keyboard/actions七门通过（source1684），并补30/60/120Hz未饱和制动。实际Player接管是下一Task3，Motor仍旧Dash；本阶段未重跑广域pre/post，五FAIL／正式0/50／最终视听等继续OPEN，不称完整1–11完成。

最新接入：短喷核心之后，已给实际`PlayerInputReader`加入同采样原始帧，保留Shift+Space+K原始事实但不改变原动作授权；清帧／边界抑制／通知同样清Raw边缘。输入事实专项及模块／动力学／旧键盘／动作／重力六门fresh通过（source1682），详见[实际Player整合计划](docs/superpowers/plans/2026-10-18-thruster-player-integration.md)。这是手动采样适配器证据，非实际短喷自动Player链；下一预算/身份与原子撤爪，Motor仍旧Dash。完整1–11目标ACTIVE、五FAIL及正式0/50仍OPEN。

用户已明确“设立目标并完成1–11”；goal工具已正式创建为 **ACTIVE**，范围为上轮清单1–11完整序章交付，未来预留不并入，不恢复自动化。验收包括新短喷、完整地图／76普通＋1Boss与机关、正式视听／UI／存档、五项失败关闭、同版正常双路线／实际性能／真人及Windows ZIP／获授权GitHub归档，不能以局部完成缩减。首个[模块与自由动力学执行计划](docs/superpowers/plans/2026-10-18-thruster-module-and-dynamics.md)已实现不可变移动模块、有限喷力／重力／阻力／互斥控制／反向制动及输入验证，逐步真实RED→GREEN；30/60/120Hz独立参考收敛通过。[组件原生证据v001](docs/production/2026-10-18-thruster-core-native-v001.md)：fresh五专项通过、广域pre/post仍旧失败、一次独立组件审阅无阻断产品问题，下一实际Player接入。**实际Player仍旧Dash，尚不能称可游玩短喷**。此前本页及总账“goal=null”是建目标前时点，不覆盖本段。

最新总对账（2026-10-18）：[完整序章剩余总待办v001](docs/production/2026-10-18-project-remaining-backlog-v001.md)按用户要求列明全部待实现、候选待接入、已有待验收、未来预留及已取消／替代方向；不是新的运行通过结论。当前正式127／候选132、新常驻正式0／50、24canonical未实现和五项严格失败保留；旧正常Main／Explore成功与局部功能证据不混报成整关正式完成。自动化按用户取消要求不恢复；该总对账中goal=null属于本次建目标之前的核对时点，当前工具ACTIVE以上段为准。

新增用户需求（2026-10-18，书面规格已批准，尚未实现）：用户已明确批准[Shift短喷与可控惯性设计](docs/superpowers/specs/2026-10-18-shift-thruster-inertia-design.md)，包含有限推力、余速、水平反向制动、独立上升保护、模块扩展及可靠着陆冲击减伤／复杂接触回退。先说明待实现点，再开始；用户随后要求先列此前全部剩余内容，本轮先完成总账，未改移动代码、正式地图或资产状态。拒绝不撤爪的接受事务、权限分离、积分顺序及旧减速迁移仍属待实施；不重复索取同一批准。详见[U-20261018-001～003意见记录](docs/玩家修改意见与世界设定.md)及[运动／资源消费者调查](docs/production/2026-10-18-thruster-and-resource-design-audit-v001.md)。全部资源接入、50常驻、资源错误及完整关卡交付目标保留，不把候选／占位资源提升为最终成品。

最新接线进展：[候选父场景与内嵌小地图v001](docs/production/2026-10-18-candidate-parent-embedded-minimap-v001.md)。真实父builder支持完整五区显式路径，同源132地形小地图；六文件原字节搬移后加载五个不同父内嵌快照，实际Ready／Model同catalog／新平台原生碰撞／Title存活唯一Player通过。叶场景编辑后须受检重建父场景，不是热引用或独立游戏包；默认仍正式五区／127，未覆盖正式资源。

修正测试漏采短暂Arrived，记录两个精确目标／唯一cast的真实事件并核对当前附着和姿态；同保存Player实际局部往返再次通过、HP5→5／跳1借力1冲刺0／无治疗，未改游戏能力或地形。最终同版十一门6GREEN／5FAIL：parent2312ms／player5508ms／原127几何26127ms／名册500ms／键盘9715ms／动作7126ms通过；候选点位三次Handle、154／31RID、pre89772ms旧Upper＋2警告、post8436ms旧Bell／Recovery仍失败。source1675／保护109及31案154份manifest对、5458份选中副本对、180份binary全部匹配；自有消费者终态后记录。

正式新增仍0／50、24canonical未实现；43候选／7未解／20889诊断不算入图。下一同132候选旧AI活动兼容，再七缺口／全50活动与manager，并定位资源生命周期；正常全关／最终视听／真人／录像门保持未完成。本轮goal工具返回null，不新建或缩减目标，不抢键鼠／付费／Git／发布／恢复自动化。以下旧CURRENT／ACTIVE／usageLimited均为历史时点，不覆盖本段。

最新局部运行：[冰区玩家抓爪往返／停靠净空修正v001](docs/production/2026-10-18-frozen-player-roundtrip-and-dock-clearance-v001.md)。发现60px身体通道容不下48px墙爪停靠，真实胶囊先拒绝；只收短下侧平台20px，80px通道复测通过。同一保存Player实际抓墙→一次借力→抓顶→落新台→回原出口，HP5→5、普通跳1／借力1／冲刺0／无治疗；机关不活动的局部fixture，不是正常全关或真人手感证明。两幽灵巡逻保持，全50联合43候选／7未解、20889行诊断；正式新增仍0／50。

最终同版十门6GREEN／4FAIL：玩家5624ms／候选34548ms／原127地形26190ms／名册540ms／键盘9732ms／动作7122ms通过；154／31RID、pre89377ms旧Upper＋2警告、post8232ms旧Bell落地／Recovery仍失败。1675库存／109保护／保管链匹配，全部消费者终态后更新。候选父场景及内嵌Minimap尚未接线，正式地图仍127；下一候选父引用交接／旧活动兼容，再余七缺口／全50活动与manager。完整目标未完成，工具实际状态usageLimited，未新建或缩减目标；不抢键鼠／付费／Git／恢复自动化。下方为历史时点，不以旧ACTIVE文字覆盖当前工具状态。

最新地图制作：[冰封侧龛与两只幽灵原生巡逻候选v001](docs/production/2026-10-17-frozen-resident-alcoves-native-v001.md)。同case真实builder保存132地形候选、三段围合石壁与两处支撑平台；两只计划内幽灵实际Ready／Physics360帧往返、落地和返回起点通过。实际Player胶囊60px双向查询通过、36px拒绝再恢复，但未证明玩家运动／机械爪返程。全50重查43联合候选／7未解，20901行拒绝证据保留；49曾配置不等于49已选，正式新增仍0／50。

最终同版九门5GREEN／4FAIL：候选29570ms／原127地形几何24523ms／名册513ms／键盘9595ms／动作7079ms通过；叶154RID／配置31RID／pre87992ms旧Upper＋2警告／post8668ms旧Bell落地＋Recovery失败。当前无FATAL不关闭历史匿名析构问题。1675库存／109保护与保管链不变、消费者终态后记录。候选小地图是独立数据证据，父场景内嵌Minimap未接线；正式保存地图仍127／41候选9未解，未发布。下一玩家真实返程／旧活动兼容／候选父引用交接、其余七缺口和全50活动／manager；全目标ACTIVE，不抢键鼠／付费／Git／恢复自动化。以下保留历史时点。

最新后台检查：[真实子型场景点位与拒绝证据v001](docs/production/2026-10-17-resident-actual-leaf-geometry-v001.md)。先复现新保存身体被旧Probe替代，再改成实际24叶；50身体身份／24子型参与，47身份曾factory配置、24子型覆盖，联合仍41候选／9未解。20594行拒绝／预留／限额证据已保存，不能把共享桶或配置过47称为47合法出生；尚未实际Ready／AI运动／全50正式入图。

首次原生几何23309ms通过，但最终同版八门3GREEN／5FAIL：几何2112ms再次匿名资源析构FATAL，叶154RID／配置31RID／Upper＋2警告／Bell落地＋一条Recovery仍失败；名册642ms／键盘9569ms／动作7121ms通过。1675库存／109保护及全部保管对不变，消费者终态后记录。正式新增仍0／50、24 canonical未实现；下一强化实际查询身份／定位9缺口与资源拥有者，再完整活动／接近／manager。全目标ACTIVE，不抢键鼠／付费／Git／发布／恢复自动化。以下为历史时点。

最新后台制作：[24子型可迁移场景包＋保存工具局部所有权v001](docs/production/2026-10-17-resident-portable-package-and-pack-ownership-v001.md)。24叶嵌入显式配置快照，另保留24作者tres原字节；48文件搬移后24实际factory绑定新包，一次额外幽灵原生Ready／时钟及原24初始化／7negative保持。作者配置修改须检查重建，不是热更新或独立游戏包。真实自有shape反例后，只确定释放工具自己创建的PackedScene，成功／拒绝两路径不再继续持有；不释放借用形状，不改GC/cache/drain。

最终同版七门3GREEN／4FAIL：名册410ms／键盘9596ms／动作7229ms通过；leaf7376ms仍154RID、profile910ms仍31RID，pre88757ms旧Upper＋2警告、post9209ms旧Bell落地＋一条Recovery错误，严格保留。中间pre曾有3条Handle异常，本次未复现不称关闭。1675完整库存／109保护不变，全部消费者终态后记录；新50仍未入图／24canonicalScene未实现。下一实际全50活动／玩家接近／保护及manager；资源、正常双路、旧档、60帧、最终视听、真人和录像门保持ACTIVE，不抢键鼠／付费／Git／发布／恢复自动化。以下为历史时点。

最新实际场景制作：[24候选叶保存绑定／显式装载与原生Ready v001](docs/production/2026-10-17-resident-saved-leaf-binding-native-v001.md)。新增builder／导出绑定／preReady factory，真实actor图另存并重载，24个在独立World2D实际初始化；幽灵真实时钟开关受击、骨骸中性guard生效，7错误装载拒绝，未添加第二renderer。候选文件在隔离测试storage，旧外观明确placeholder；不是保留outer继承、新美术或地图50出生。

最终同版七门3GREEN／4FAIL：名册／键盘／动作通过，leaf130RID／原profile31RID仍严格FAIL；完整pre88376ms仍Upper错台＋2警告，post8435ms仍BellChain落地＋一条Recovery错误。source/resource1675／保护109保持，所有消费者终态后记录。正式新增仍0／50、canonical24正式Scene未实现；下一受保管的prototype场景引用与ALL50活动／玩家接近／保护区、manager。完整资源／双路／旧档／60帧／最终视听／真人／录像目标ACTIVE，不恢复自动化或抢键鼠，无Git／付费／媒体／发布。下方为历史时点。

最新制作接入口：[子型配置Resource／保存重载与真实actor挂树前消费v001](docs/production/2026-10-17-resident-subtype-profile-resource-v001.md)。新增可保存的24子型身份／family／role／九种carrier／Bone-Ghost policy，保StableId及原Placement、不可变交接，拒绝14类错误绑定。先复现空字段、缺policy、未提交消费者三步反例，再补最小实现；24tres实际落在隔离测试目录，不是正式叶场景或新美术。

最终同版六门3GREEN／3FAIL：原名册／键盘／动作通过；profile各数据／off-tree消费符合但退出31Shape RID仍FAIL，完整pre仍Upper错台＋2profile警告，post仍BellChain落地＋一条Recovery错误。实际source/resource1672／保护109保持，消费者终态后才更新记录。正式新增仍0／50、24正式Scene未实现；下一实际builder叶绑定与挂树证明，再全50活动／接近／manager。完整资源／双路／旧档／60帧／最终视听／真人／录像目标ACTIVE，不恢复自动化／新付费／Git或前台抢键。下方为历史时点。

最新实际机制：[幽灵炮台／显隐、飞弹撤销与停炮反击v001](docs/production/2026-10-17-resident-ghost-sentry-native-v001.md)。Sentry独占Ghost策略与最终bool机械爪接收，共享炮台分别管理生命／战斗权限，其他四枪默认不变；Hidden保真实身体及受击接触、撤J/K/AI/旧飞弹，Reset保持双组隐藏。真实反例后保留Hide前刚接受的K停炮请求，不拉身体或延长原眩晕。12行为符合；保存Player近身K→J首轮已可命中，不冒称新增可达性。

最终同源码15门4GREEN／11FAIL：旧Sentry完整综合／名册／键盘／动作GREEN；新12＋旧炮台58行为符合，但九专项退出3／6／7／15 Shape RID仍FAIL。pre015308仍Upper错台＋2profile警告，post015505仍BellChain落地＋一条Recovery祖先错误。source/resource1670／保护109／75保管链对保持，全部消费者终态后更新记录；当前无FATAL不关闭历史资源问题。

下一24子型真实builder场景身份／profile绑定及验证，再全50完整活动／玩家接近点位和manager。正式新增仍0／50、全部Placement=null／24正式Scene未实现；Bone10/Ghost10及完整双路／资源／旧档／60帧／最终视听／真人／录像目标不减，ACTIVE。继续无窗口、不抢键鼠，无新媒体／付费／Git／发布／自动化；以下为前轮时点记录。

最新实际机制：[幽灵飞行／俯冲显隐与生命周期v001](docs/production/2026-10-17-resident-ghost-air-native-v001.md)。typed Ghost独占配置，Hidden分别撤J、K发现／最终接收及旧攻击，保身体碰撞／已接受外力／void；显形保持原侧袭／俯冲与完整预警。真实同栈Reset反例后修旧K尾部和pending坠落误杀新生命；16行为符合，两角色保存Player K→J原生命中。没有压缩原时间或增加拉力／HP，不是OS真人或正式美术。

最终同源码12门4GREEN／8FAIL：默认完整综合／名册／键盘／动作GREEN；Ghost16、Bone8、旧空中27行为符合，但六专项退出4／7 Shape RID仍FAIL。pre011648仍Upper错台＋2profile警告；post011845仍BellChain落地＋一条Recovery祖先错误。source/resource1670／保护109／60保管链对保持，全部消费者终态后更新记录。当前无FATAL不关闭历史资源问题，旧Skitter默认资源故障未复测。

下一实做唯一剩余Ghost炮台BellBoltSentry，再24正式场景／全50活动和manager。新增仍0／50、全部Placement=null／正式Scene未实现，Bone10/Ghost10与正常双路／60帧／旧档／最终视听／真人／录像门不减，完整目标ACTIVE。用户再次确认其他窗口使用，继续无窗口、不抢键鼠；无新地图／媒体／付费／Git／发布／自动化。以下为前轮时点记录。

最新生产机制：[骨骸俯冲／真实姿态与K→J反击v001](docs/production/2026-10-17-resident-bone-moth-native-v001.md)。Bone+Diver独占策略区分真实机械爪来源与普通受伤的ThreadStunned，正常Recover可受击，sleep／死亡／void／Reset撤旧权限；最终bool机械爪事务拒绝坏设置、失效链及跨World。先8案7失败再最小实现，8行为符合；保存Player向上K→J首轮已能命中，不冒称新修时长。原模型／.75秒K／HP／碰撞／轨迹未改，不是OS真人输入或正式骨骸资产。

最终同源码10门4GREEN／6FAIL：旧默认完整综合／名册／键盘／动作GREEN；新Bone8及旧空中14／角色7／边界6行为符合，但退出7／4／4／4 Shape RID仍FAIL。普通pre004023仍Upper错台＋2profile警告，post004217仍BellChain落地及一条Recovery祖先错误；本次Supply返回无FATAL不能关闭历史崩溃，旧Skitter默认资源问题未复测。source/resource1670／保护109／50保管链对保持，全部消费者终态后更新记录。无新地图／美术／付费资产；前轮报告原件保留。

下一实际Ghost Moth的Flyer／Diver，再Ghost Sentry（剩余3族系承载）、24正式场景、全50活动与manager。新增仍0／50、24正式场景未实现，骨骸10／幽灵10与完整资源／旧档／正常双路／60帧／视听／真人／录像门不减，目标ACTIVE。[完整76普通＋1Boss／NPC分配](docs/production/2026-10-16-prologue-population-final-plan-v002.md)仍是制作基线，不是已入图数量。继续无窗口、不抢键鼠，无新媒体／付费／Git／发布；下方保留历史时点。

最新真实族系机制：[骨骸弱点／幽灵显隐v001](docs/production/2026-10-16-resident-bone-and-ghost-native-v001.md)。显式Bone Heavy在Armor累计前判实际正背面／Exposed，真实K开甲后可正面J；Ghost Wanderer有限显隐，分别撤J、K发现与最终布尔投递，保身体碰撞，修缺Settings事务异常。先原生失败再最小实现，4骨骸＋3幽灵行为符合，但均stdout4 Shape RID严格FAIL，不是正式身份或新增7只。

最终同源码11门5GREEN／6FAIL：旧巡逻／未配置Heavy／名册／键盘／动作GREEN；新旧四机制专项资源FAIL。**本次完整pre211007再次原生FATAL：gchandle.is_released()／Dispose→Finalize，exit−1073741795，27830ms，旧Upper尚未执行，不能抄上一轮Upper／警告结论。** post211405仍BellChain地板／恢复路径失败。source1670／保护109／55保管链对保持（pre helper10，其余9），全部消费者终态后更新记录。

正式新增仍0、50Placement全null、24正式族系场景未实现，骨骸10／幽灵10与全目标ACTIVE不减。下一先对当前FATAL做有区分力的生命周期隔离，不猜资源身份／反复等偶然绿；随后补剩余家族承载、24正式场景与ALL50活动／玩家接近／manager。全部资源／正常双路／60帧／旧档／最终美术／真人／录像门仍OPEN。继续无窗口、不抢键鼠，无新媒体／付费／Git／发布；下方为历史时点。

最新真实职能分化：[空中常驻侧袭／下冲v001](docs/production/2026-10-16-resident-air-role-split-native-v001.md)。显式Flyer左右侧接近、默认96px短侧袭；Diver保留高处下冲／首接触／物理返航。模型、预警预测、原生运动同速度，Reset保留职能，旧未配置波次不改。先取得同轨迹原生反例，再实现；7角色＋原14运动＋原6边界共27行为符合、28清理／68自建Shape释放匹配，但三空中专项仍stdout4 Shape RID，严格FAIL。最终9门4GREEN／5FAIL；旧模型综合、名册、键盘、动作GREEN，完整pre201950仍Upper错台／2profile警告，post202146仍BellChain地板／恢复祖先失败，没有豁免。

source1669／真实保护109／45保管链对保持；全部消费者终态后更新记录。正式新增仍0、50Placement全null、24正式族系场景未实现；骨骸10／幽灵10、完整目标ACTIVE与全部资源／正常双路／60帧／旧档／最终美术／真人／录像门不减。下一实做Bone方向／姿态弱点、Ghost显形J/K及正式profile／场景，再做ALL50活动几何和manager；不重复无区分力的炮台／名册测试。继续无窗口，不抢用户键鼠。下方为历史时点。

最新局部预警修正：[炮台配置与贴墙预警v001](docs/production/2026-10-16-resident-turret-configuration-and-wall-native-v001.md)。真实墙左601/弹体外沿600.9952，旧预警伸到608；现opt-in Sentry用完整圆裁剪，预警600.2168，不再穿墙，未改伤害/弹速或未配置旧波次。新增配置原子性5项、锁定后新墙撤权5项原本即符合，没有虚构修复。最终13炮台专项77行为/清理符合，但均有退出Shape RID，严格FAIL；22门7GREEN/15FAIL。完整pre194639仍旧Upper错台/2profile警告，post194832仍旧BellChain落地/Recovery失败；本次没有6Handle诊断不代表历史问题已修。

source1669/保护109/五组保管链保持；全部consumer终止后才更新记录，无新地图/美术/正式出生/整路或视听验收。新增仍0、50Placement全null、24正式族系场景未实现，骨骸10/幽灵10与完整目标ACTIVE不减。下一直接实做Flyer/Diver轨迹分化，随后骨械弱点、幽灵显形/J-K、正式子型与ALL50活动/manager；不无区分力重跑77检查或身份名册。仍无窗口，不抢用户键鼠。下方为历史时点。


本轮真实目标边界与坠落续推：[五保存炮台环境与目标v001](docs/production/2026-10-16-resident-turret-environment-and-targets-native-v001.md)。新增跨World/隔墙正负控与锁定目标自然死亡15项，复现并修正五actor未接入真实void接触/无敌待致死；pending保留跨休眠、删源及显式Reset第二生命，未改玩家KillZone或未配置旧波次。最终10专项66行为/清理符合，但各有3/6/7/12/15Shape RID，严格FAIL。最终19门7GREEN/12FAIL；完整pre190557另有6句柄异常、2profile警告及旧Upper错台，post190757仍旧BellChain地板/Recovery失败。源码1669/保护109/五组保管链保持，全部consumer终态后才更新记录。

正式新增仍0、50Placement全null、24正式族系场景未实现，骨骸10/幽灵10与完整目标保持ACTIVE；没有新美术/正常整路/呈现60FPS/真人/录像或版本发布。下一补配置原子性、锁定后墙体/预警、Burst hitch与未覆盖生命边界，再完成最终职能族系/ALL50活动/manager。用户使用其他窗口期间仅无窗口，不抢焦点；回答仅解释该次失焦。下方保留历史时点。


最新资源诊断：[炮台挂载形状身份v001](docs/production/2026-10-16-resident-turret-shape-identity-native-v001.md)。同场景三次独立actor/World挂载复用身体/受击/抓取三枚形状；共15个保存RID，与25个自建RID无交集。原15行为/15清理符合，24887ms/55000ms，但stdout仍15Shape RID ERROR，严格FAIL。数量吻合不是匿名退出告警的身份映射，资源根因仍OPEN；未改生产、cache/GC/清理等待或借用Dispose，私有beam查询Shape明确未覆盖。源码1669/保护109/五组保管链保持，无新地图/正式出生/美术或双路验收。此有限问题已回答，下一推进实际负感知/配置/外力/void及最终职能族系，不无区分力重跑。完整目标ACTIVE，用户使用其他窗口期间继续无窗口；下方历史记录保留。

最新实际生命周期续推：[炮台首发／爆炸撤权与自然死亡重置v001](docs/production/2026-10-16-resident-turret-callback-lifetimes-native-v001.md)。真实AreaEntered两次致死复现并修正两处旧回调污染新生命：Sentry漏第二次Defeated、四远程reset后HP2却Dead。现五actor都完整新预警/真实新攻击/第二次独立死亡；激光首发前、导弹爆炸入口及两接收者之间休眠撤权4案也符合。新增9案＋原27行为符合，但六专项退出3/6/12/15Shape RID，严格仍FAIL，不称资源已修。当前七项原Sentry/四远程相关/键盘/动作严格GREEN，完整pre174416仍旧Upper落错台/两profile警告，post174615仍旧BellChain落地/ILevelRecovery FAIL。

source1669/保护109/五组保管链保持，全部consumer终止后更新记录；无新地图/正式出生/美术。下一先以物理服务器纯RID值区分保存/自建/生产Shape身份，不改cache/GC/等待或Dispose借用资源；身份诊断尚未运行、数量模式不是根因。随后负感知/配置/外力/void、最终职能与族系、ALL50活动/返航/入图manager继续必做。正式新增仍0/50Placement全null/24族系场景未实现，骨骸10/幽灵10与完整资源/正常整路/实际呈现60FPS/真人/最终美术门均OPEN，目标ACTIVE。继续无窗口，不抢键鼠；下方保留历史时点。

最新实际炮台续推：[固定家点／休眠撤弹／完整领地与生产Shape所有权v001](docs/production/2026-10-16-resident-turret-native-v001.md)。四远程＋独立Sentry已增加显式常驻权限，保HP唤醒及完整新预警；真实反例后修Sentry完整弹体边界、radius7预警及仅两枚自建动态Shape的终结。最终15基础＋5休眠＋7边界行为符合，但三个专项均stdout退出15Shape RID，严格FAIL，不称资源门过关。旧Sentry完整综合、原远程model/ledger/projectile/保存actor、键盘/动作共7项严格GREEN；新完整pre165722仍旧Upper错台/两profile警告，post165916仍旧BellChain地板/ILevelRecovery FAIL。

当前source1669/保护109/五组保管链保持，全部消费进程终止后才更新文档；未新增地图/正式出生/美术。下一补真实60Hz Laser首发前伤害撤权、Missile爆炸入口/两接收者之间撤权及Sentry原生死亡-reset第二生命，不重复空名册/无区分力资源重跑。正式新增仍0、50Placement全null、24正式族系场景未实现，骨骸10/幽灵10、人口manager及资源/正常整路/呈现60帧/真人/最终美术门均OPEN，完整目标ACTIVE。用户确认正在使用其他窗口，仅继续无窗口、不抢键鼠；这不解释更早失败录像。下方保留历史时点。

最新边界续推：[重装真实击退/坠落/pending与目标13项v001](docs/production/2026-10-16-resident-heavy-environment-boundaries-native-v001.md)。真实外力出完整领地后保HP返程、单次垂直冲量、休眠保已受力、layer0陷阱死亡、无敌待致死跨休眠/源删除、卸载替换及World/Health/LOS/旧锁身份13项符合；原10项也符合，两个专项仍退出4Shape RID严格FAIL，旧重装相关完整综合155708严格GREEN。World漏条件反例只使foreign断言失败，最终精确恢复生产SHA7F8。当前source1667/保护109/五组custody保持，无保留的生产玩法或美术更改。

下一实际入口为固定炮台/散弹/连发/导弹/激光常驻适配，重点是整条弹体/光束即时撤权、保HP唤醒及新预警；不重复已证Heavy13/空中6或空名册。正式新增仍0、50Placement全null，骨骸10/幽灵10、24正式族系场景/人口manager及完整资源/整路/呈现帧率/真人/最终美术门保持OPEN。用户使用其他窗口，继续无窗口、不抢键鼠。下方各段为保留的历史时点。

最新实际重装行为：[原生巡逻/重甲/开甲与空中边界v001](docs/production/2026-10-16-resident-heavy-and-air-boundaries-native-v001.md)。保存Heavy新增opt-in真实双向巡逻、完整56px支撑、追击离home后物理返程；休眠在Armor累计前撤权限，机械爪仍只开甲、不拉重装，真实Health冲量/重力不被自主速度覆盖。原生致死回调复活后的第二生命可再次独立死亡；旧未配置的承诺攻击/Armor/Heavy爪合同保持。10行为及清理回执均符合，但退出4Shape RID，专项严格FAIL。

空中补6项真实World/Health/遮挡/完整圆屋顶/锁定失效边界均符合；只故意去掉同World条件时对应断言准确失败，已精确恢复；该专项也退出4RID，非资源修复。当前1667旧重装相关完整综合151727、旧飞蛾完整综合151901、键盘151935、动作152011严格GREEN；完整pre152048仍旧Upper错台/两profile警告，post152245仍旧BellChain地板/ILevelRecovery FAIL。保护109/五组清单保持，全部消费进程已终止。

**正式新增仍0、50Placement全null、24族系场景未实现；骨骸10/幽灵10及完整目标不减。** Heavy还需真实void/外力/目标/配置边界，Moth Flyer/Diver差异、炮台/远程/族系与全50地形/跨区manager仍OPEN。旧1663几何41候选/9未解不是本轮新跑。用户在操作其他窗口，本轮无窗口、无新Main/Explore/呈现60FPS/真人/媒体/最终美术或付费/Git/上传。目标ACTIVE，不以局部行为通过验收全关；下方保留历史时点。

最新实际空中行为：[巡航/俯冲/受力/返航v001](docs/production/2026-10-16-resident-air-motion-native-v001.md)。保存飞蛾新增显式常驻共同适配器，真实二维巡航、完整圆身体碰撞、原目标锁定俯冲/首接触后物理返航；机械爪和真实伤害中断攻击但保留重力/外力，休眠撤新权限而不冻结已接受外力。修正零击退不撤Dive、替换活目标维持旧锁及死亡发布内reset影响第二生命；旧未配置路径保持。最终14行为及清理回执均符合，但退出4Shape RID，专项仍严格FAIL。**不是最终Flyer/Diver两职能、骨骸/幽灵或全部空中行为验收。**

当前1665新旧飞蛾完整default143347、键盘143424、动作143459、真实stream10项143533严格GREEN；完整pre143602仍旧Upper错台/两profile警告，post143755仍旧BellChain地板/ILevelRecovery FAIL。保护109/五组前后清单保持；资源精确原因未证实。正式新增仍0、50正式Placement全null、24族系场景未实现；骨骸10/幽灵10及全目标不减。用户正在操作其他窗口，本轮仅无窗口工作，无新Main/Explore、呈现60FPS、真人、媒体/最终美术或付费/Git/上传。目标ACTIVE，下一补真实空中遮挡/天花板/目标死亡/跨World边界，再做其余职能/族系与全50活动认证，不重复空名册。下方历史时点保留，不替代本段。

最新死亡边界续推：[实际修正与资源对照v001](docs/production/2026-10-16-resident-charger-death-and-resource-differential-v001.md)。已配置常驻冲锋普通伤害死亡后，现立即撤两类抓取发现组/目标碰撞层与直接伤害权限，不再让尸体抓点Area遮住远处合法抓点；原生死亡及同帧checkpoint复活统一按当前生命延后提交Monitorable，第二生命可再次原生受击/死亡。最终20行为断言均符合、in/out错误消失，但stdout退出4Shape RID，专项仍严格FAIL，不能验收样段。释放夹具字段、未配置同夹具20次、替换物理帧边界都没有消除4RID；已恢复原清理时序，精确资源原因未证实。

当前1663新旧冲锋完整default134451、键盘135100、动作135135、真实流送来源10项135208严格通过，三raw无诊断、保护109及五组清单一致。新完整pre135237仍旧Upper错台＋两条profile警告，post135431仍旧BellChain地板/ILevelRecovery失败，未豁免。**正式新增仍0、50正式Placement全null、24族系子型场景未实现；骨骸10/幽灵10及全目标不减。** 用户确认使用其他窗口，本轮不抢焦点/补键/重跑可见路线，没有新Main/Explore、呈现60FPS、真人、图稿录像或最终美术验收。目标ACTIVE；下一实际生产入口为空中巡航/俯冲/物理返航与外力组合，不再重复空名册或无区分力资源重跑。下方保留旧时点，不替代本段。

最新续推：[常驻冲锋与真实区域归属v001](docs/production/2026-10-16-resident-charger-and-stream-native-v001.md)。配置后的冲锋样段已能真实巡游、完整脚底避险、巡游后冲锋/后退、限速600牵引出区后保血返回；已修正无敌待致死仍自主后退及唤醒重开目标权限。15项原生行为断言均符合，但退出4Shape RID，因此专项仍严格FAIL，不作为完成样段。未配置旧冲锋的完整模型/原生综合严格通过。**正式新增仍0、全部50正式Placement仍null、保存版仍原26＋Boss；骨骸10/幽灵10及全族系要求不减。**

当前源码1663/保护109，源码/helper/二进制/继续存储前后清单保持。真实五区world/Ready/registry/卸载/同ID重挂/不同世界隔离10项在当前源码严格通过；去掉Actor所属分区校验时两项准确失败，已精确恢复。它是来源权限证明，不是全NPC运动或人口管理认证。旧Rust7＋15/void14、键盘与动作最近独立通过在1660阶段，不改称本轮新跑。用户确认Explore失焦时正在使用其他窗口；本轮无窗口测试，不抢焦点、不补键、不无改动重跑。更早失败录像原因仍未证明。

当前1663几何125836与名册125929严格通过，仍100保留区/41活动候选/9未解/search_capped、50点及族系场景未完成；[岩台与父小地图](docs/production/2026-10-16-resident-player-access-and-archive-space-v001.md)保持，未改地图/数量。新完整pre125518仍旧Upper错台＋两条profile警告，post125724仍旧BellChain地板/ILevelRecovery失败；配置冲锋4RID及历史资源/FATAL根因未闭合。Task1最终50认证、Task2其他角色/族系、Task3人口流送、正式双路线/旧档/有支撑出口/呈现60FPS/真人/最终美术和成功录像仍OPEN，目标ACTIVE。无新媒体/付费/Git写入/上传/自动化恢复/安装或用户存档语义改写。下方为历史时点，不替代本段。

最新活动范围进度：[完整走廊与活动包络普查v001](docs/production/2026-10-16-resident-corridors-and-activity-census-v001.md)。生产检查器新增固定姿态全段净空/连续支撑/完整保留与layer0电磁补查，28原生走廊＋4联合选择控制先RED后GREEN；故意漏支撑ShapeId仅对应墙反例失败，恢复精确源码。三个旧冲锋身体点左右64px均真实拒绝；1735次走廊检查和16045搜索节点为50身份找到38个不重叠活动包络、12未解，search_capped=true。**38不是实际出生/真实巡逻，也不是容量上限；全部Placement仍null，实际新增0，目标仍50。** 后续先分散候选、补玩家关键交互保留，再通过builder增加围合侧龛。完整pre仍旧Upper摆荡错台和两profile警告，post仍旧BellChain地板/Recovery失败；本次未复现旧句柄错误不等于已修复。库存1653/保护109/所有守护保持；名册17负控通过，全目标ACTIVE。无新地图/玩家/敌人AI/数值/存档/美术、正常整路/媒体/真人/60FPS验收或付费/上传/Git写入。

完整规划复核：已保存[敌人及NPC合并总表v002](docs/production/2026-10-16-prologue-population-final-plan-v002.md)，逐区同时列原26的实际敌种与新增50配置，五族/六职能/主支路/NPC总数相互一致；骨骸/幽灵仍本关必做。仅整理文档，不改变v001数量或游戏版本，不把身体候选称正式巡逻点，也不声明76已可玩。展示前另存原件/展示版于[过程归档v002](E:/OurGame/Knight/过程图稿与视频/2026-10-16-序章敌群完整分配-v002/README.md)，旧稿保留；本轮未新运行、生成美术、付费或发布，全目标仍ACTIVE。

最新用户扩展：现有敌人之外追加约50只、固定区域自主游荡，骨骸/幽灵等系列明确必须进入本关；用户当前先要看完整最终分配。已核对实际保存版仍12波次组/26普通名额/9基础类型+1Boss，尚无独立巡游人口，ECHO为1指引系统、0独立NPC身体。已保存[完整分配v001](docs/production/2026-10-16-prologue-population-final-plan-v001.md)：新增原生8/废弃12/精工10/骨骸10/幽灵10=50，合计76普通+1Boss；主50/支26，安全营地/检查点保护/Boss普通群/出口不增怪。24新增配置子型/6职能，不等于24套已完成美术或AI。该文含全部14区、旧26、新50、NPC、族系、行为和计数口径；[架构规格](docs/superpowers/specs/2026-10-16-resident-population-design.md)与[实施路线](docs/superpowers/plans/2026-10-16-resident-population.md)保留全范围。

规划展示已[归档](E:/OurGame/Knight/过程图稿与视频/2026-10-16-序章敌群完整分配-v001/README.md)：文字原件/展示副本2项、25548字节，逐项源/副本SHA一致。没有新增图片/视频或付费素材；规划副本不是新增群已实装/用户已验收。

**最新续推：50身份/24子型的不可变分配名册已写入代码，仍不是新增50已可玩。** 先保留Godot运行下实际expected50/actual0失败，再验证数量；还复现了“互换稳定身份所在区、总数却不变”被漏检，现绑定身份/子型/路线/sector。最终名册专项严格通过17项同检查错误输入，保留旧26；所有50点仍逐一报告未放置，24配置子型的正式场景都标明未实现，旧敌scene仅是开发探针，结构候选永远不能冒充原生安全认证。此前1650阶段V2结构与动作相邻专项通过；最新1652阶段又通过原生单体几何28项及名册17项，三raw无诊断、保护109及二进制守护保持。旧场景、玩家、敌人行为、存档、数值及美术字节未改，尚未实装巡逻/出生/流送管理，也未进行新版整路验收。

原生几何最新进度：精确挂载五个保存sector到独立World2D，确认127原始地形体、Ready实际生成1Boss/0普通、缓存形状不被夹具改写及延后初始化稳定；九种旧敌身体探针、完整地板支撑、中心缺口、同体另一墙形、真实电磁Area、查询截断、祖先变换和占用预留均有正/负检查。先保留干净行为RED，再补检查；另用故意去掉支撑形状身份的反例证明新检查确实抓住穿墙漏判，随后恢复精确源码、28项通过。**这只是单身体检查器，不是50点/巡逻/全环境安全验收。** 7个零layer恢复区、5股蒸汽全相位、4个运动实体、条件门和全部落点/安全区仍须统一覆盖及逐点证明。[证据与边界](docs/production/2026-10-16-resident-native-geometry-v001.md)。最新完整pre仍旧Upper的lower_to_mid_swing错台及两条刻意profile警告，post仍旧BellChain夹具落地/ILevelRecovery错误，严格失败未豁免；当前没有新Main/Explore、录像、真人或稳定60FPS证明。

**最新1653阶段：已实现已知保存环境保留工厂，环境51项＋单身体28项严格通过，并为50身份各取得一个原生身体候选。** 覆盖13危险/恢复源、4完整运动包络、10条件门形状、36起跳/落地区、11奖励接近区及7保护区，共81；旧26主/备出生另预留36条，327次查询找到50个身体见证。摆锤非均匀缩放反例先失败，独立一次性夹具修正后恢复源码通过，保存五区快照仍精确不变。**候选不等于正式位置**：三个冲锋候选及多处游荡点余量狭窄，完整活动/返航/攻击和玩家通路尚未验证；全部Placement仍null，巡逻/出生/跨区manager未接入，当前仍原26＋Boss。最新名册17项通过；完整pre又出现10条未初始化句柄＋旧Upper/profile失败，post仍旧BellChain floor/Recovery失败，全部严格保留。源码1653/保护109/helper/二进制守护保持，无新Main/Explore/媒体/正式美术或60FPS验收。[候选坐标、反例、风险与原始证据](docs/production/2026-10-16-resident-environment-and-body-census-v001.md)。全目标ACTIVE，不以身体普查替代50居民和全关目标。

原AI缺home/patrol，V2跨区会卸载，需要真实巡逻/返航/休眠撤攻击/跨区击杀状态及50点原生几何验证，不能复制50波次凑数。[地形候选初查v001](docs/production/2026-10-16-resident-geometry-census-v001.md)保留了实际127原始地形体、蒸汽/电磁/摆锤/升降占用与狭窄点；127不是127个合法出生台，几类此前设想的齿轮/弹板/熔炉还未存在于保存V2。空域/完整支撑/安全区/抓爪落点尚待逐点验证，必要时通过builder补围合侧龛，不缩减50。此前双路成功不能验收新增群。性能调查的813条stdout同步排出及生产4报告写入尚未拆开因果；本轮无新增媒体/付费/Git写入/上传/自动化，素材表不变。完整目标ACTIVE，保留旧资源/FATAL/旧档/60帧/十区/最终美术/真人等全部未验收项。[本轮范围、原始失败与验证](.superpowers/sdd/2026-10-16-resident-population/progress.md)。下方保留历史实现时点。

最新续推：当前V2护盾机关的验证已迁到实际保存地形与纯键盘L/D/Space：受支撑起点、完整胶囊跨危险区、精确PistonOne落稳，保留原150物理帧限制。最终专项050901严格通过：无盾真实脉冲HP5→4；举盾132/150帧挡住7次脉冲、HP5不变；松L后100帧真实回能/降热。三个同检查损坏夹具均按预期拒绝。独立窄审发现准备阶段/清理异常可能漏释放自建场景，实际两个原生反例后补上外层所有权及独立清理，两种异常均在负控兜底之前释放根节点、输入驱动与键。**本轮是验证与夹具清理修复，不是新增玩家技能、场景或美术，也不是历史资源故障已解决。**

最终代码的键盘、动作、V2结构专项均严格通过；完整pre051246仍失败于旧Upper的lower_to_mid落错台和两条刻意无效profile警告，完整post051507已通过护盾/清理检查，随后仍在旧BellChain夹具的地板/六层复活路径失败，均未豁免。新正常Main051601（engine0/203341ms/guards0）和Explore052014（engine0/277627ms/guards0）均Title→同角色Boss击败→有支撑实体出口/D60与完成存档稳定，失焦0、三raw无诊断。主线实际散弹/连发/激光，探索实际导弹/激光，合并覆盖四职能，不称每条路线都有四种。深井仍4.633秒；普通715.999px坠落在探索中仍扣1HP。

当前尚不能称稳定60FPS：Main过程回调p95/p99/max=25.706/36.448/261.272ms，Explore24.013/32.530/57.085ms，均不是显示呈现帧率或真人手感证明。源码1647/真实保护库存109及二进制守护保持，未改生产玩法/地图/数值/存档/素材；历史FATAL/资源、旧档、十区完整围合、最终美术/真人及新整路录像仍OPEN，完整目标ACTIVE不缩减。下一优先以真实正式路线的性能/画面与围合体验为准，不再把逐段修旧夹具当作关卡提升。无新媒体/付费/Git写入/上传/自动化，素材表不变。[本轮原生反例、审查取舍、双路线与剩余门](.superpowers/sdd/2026-10-16-shield-lane-keyboard/progress.md)。下方为历史时点。

本轮双路记录补充：探索支路两次检查点均保留1HP，之后实际神殿补给重叠一次，将1→3/最大5并保存；不是测试注血、复活或检查点自动回血。“Boss HP=3/3”是当前值/入战参考值，不是最大生命变3。两路都只证明自动通行，不将约203/278秒机器执行时长当作真人试玩时长。长帧侧写把主线最大261.272ms定位在配重战完成边界附近，Boss阶段另有7个>50ms区间；这里只是时间关联，尚无CPU/GPU/GC根因证据，下一步需范围内被动计时。

最新续推：已把实际V2保存地图的结构检查从旧15区域/11段迁到独立字面5大区、14区域节点、12挑战和18有方向通路；核对精确尺寸、标记身份/可选支路、真实碰撞与机关、敌人组稳定ID、实际父变换、Boss门/捷径/出口合同。先保留原生漏检反例，再补检查；042141专项严格通过，33项故意损坏数据均由同一检查按预期拒绝。**这是验证修复，不是新增33项玩法、地图或美术，也不证明18条边已实际键盘走通。**

新post042258已越过旧目录错误，材质对齐127平台/8目标及三处落地相机检查通过，随后仍严格失败于护盾机关独立夹具的起点落地和KillZone复活路径。静态核对发现它出生在当前地板下方、缺正式六层复活控制器祖先，旧过线阈值也没到当前危险区；下一步迁移夹具后真实复测，不能只改Y或改生产路径凑绿。未修改或豁免此失败；本轮未重跑完整pre及正常Main/Explore。深井已批准气流仍为先前实测4.633秒，未改玩家/地图/数值/美术。历史资源/FATAL、旧档、稳定60帧、十区围合、最终美术、真人与新整路录像仍OPEN；目标ACTIVE不缩减，无新媒体/付费/Git写入/上传/自动化，素材表不变。[本轮原始反例、专项与剩余门](.superpowers/sdd/2026-10-16-v2-structure/progress.md)。下方为历史时点。

最新续推：旧Upper首段验证已改用真实键盘/实际到达后K取消，专项99/150帧及完整pre内96/150帧落稳；断齿回归100/150，键盘/动作相邻回归严格通过，独立窄审无C/I/M。禁爪时普通跳跃也能落台，测试仅因缺少规定抓取回执而拒绝，**不能称该地形必须用爪**。本轮没有新增正式关卡内容、技能或美术。

重要优先级校正：已沿实际消费者确认，旧六房间/Upper不是正式V2 Main/Explore运行区域；正式主场景走LayoutVersion2、五sector的seamless分支，V2钟摆画廊另在CanyonAscent。旧夹具仍有共享能力/兼容回归价值，但不能当作V2地图提升。最新pre034007仍失败于下一旧lower_to_mid落错平台，post034207仍旧15/现14结构断言＋186Shape退出ERROR，全部保留未豁免。下一优先处理真实V2结构/物理合同和新鲜正常Main＋Explore，不再自动逐段迁移旧房间。资源/FATAL、旧档、稳定60帧、十区围合、最终美术、真人与新整路录像均OPEN；目标ACTIVE不缩减，无新媒体/付费/Git写入/上传/自动化，素材表不变。[本轮证据、消费者核对与下一步](.superpowers/sdd/2026-10-16-upper-entry-keyboard/progress.md)。下方为历史时点。

最新续推：旧断齿横廊自动抓取测试已迁到真实键盘与当前自动牵引：一次Space、K抓取、实际到达后再K取消，自然落回原平台；没有二段跳、Boost、改地形或延长原150物理帧。最终专项100/150、完整pre内96/150均有精确锚点、实际取消/速度投影与两次完整胶囊支撑证据；键盘、动作、抓爪落地和高度摔伤四项相邻回归也严格通过。独立窄审完成，源码1647/保护109库存保持。**本轮主要修的是测试驱动与取证，不是新技能或美术提升。**

最新完整回归仍未通过：pre031418已越过断齿横廊，下一处旧UpperPendulum抓取夹具149/150帧坠落；两条刻意无效profile警告仍严格保留。post031615仍旧15/现14目录断言＋186Shape退出ERROR。零宽负控正确拒绝非法落台，针对性释放未挂载房间后大部分退出泄漏消失，但16Shape退出ERROR仍在，不称资源已修复。目标继续ACTIVE；旧档/历史句柄FATAL、正常60FPS、十区围合、最终美术、真人及新整路录像仍OPEN。本轮无新Main/Explore、媒体、付费、Git写入、上传或自动化，素材表不变。[本轮实测、原始反例与下一步](.superpowers/sdd/2026-10-16-broken-tooth-keyboard/progress.md)。下方为历史时点。

最新续推：已实证并修正机械爪Boost动作测试的旧快照误报。完整回归中同一物理帧发生三次正常姿态发布，每次视觉与协调器完整值均相等，旧测试却把第二次历史快照和当前第三次比较。现改为逐次同边界完整相等（含Secondary）、溢出/观察器错误否决及最新帧最终相等；保留原Boost→Released、提示、Contact→Recovery和手臂连续性，不加等待、不删字段。真实故意传错Secondary时严格失败，恢复后022218三次发布专项严格通过；独立窄审C0/I0/M0。**本轮修的是验证，不是新增游戏动作或已改善美术。**

同时修正canonical验证入口的旧时间戳缓存风险：发现源码精确恢复后增量构建仍复用故意损坏的程序集，保留失败反例；验证入口现明确Rebuild，22项实际参数/符号准入和43项保护门测试通过。023103保留恢复文件原旧时间戳、不触碰刷新，实际重新编译为正确DLL/PDB并严格通过，生产Coordinator源码字节完全恢复。只覆盖本canonical owner，非所有旧启动器，构建耗时会增加。无付费/Git写入/上传/自动化或新增媒体。

**最新完整回归仍未通过，目标ACTIVE**：最终pre023242 engine1/85722ms/guards0，已通过三次完整姿态检查，后续仍两条刻意无效PresentationProfile警告及旧BrokenTooth抓取路线149/150帧坠落失败；post023437 engine1/764ms/guards0，仍旧15/现14目录断言＋186Shape退出诊断，均未豁免。本轮没出现历史句柄错误不代表修复；正常Main/Explore是下段上轮证据，本轮未重跑或声称稳定60帧。资源/旧档、十区围合、最终美术、真人及新整路录像继续OPEN。[本轮反例、实际二进制、审查与下一步](.superpowers/sdd/2026-10-16-boost-presentation-boundary/progress.md)。下方为历史时点。

最新续推：已修复自动试玩在摆锤走廊激光兵前过早举盾、又因35%门槛拒绝足够气力防守的问题。保留原生HIGH/LOW严格失败反例后，014550专项严格通过：真实低能量28.733358下挡住实际激光全段，HP5不变，完整Active及松盾收尾保留；三项相邻战斗回归通过，独立窄审C0/I0/M0。只改自动试玩/证据，不削弱敌人、不加血/加时、不改生产玩家技能。

当前两条正常路线均严格通过：Explore014835为engine0/275878ms/guards0，Main015341为engine0/203383ms/guards0，均零失焦/死亡恢复、三raw无诊断，Title→同角色Boss击败→有支撑实体出口并D60不重复完成/不改已完成存档。Explore以HP1完成摆锤走廊三批敌人；真实激光发射、存活和击败可直接观察，正常路线的具体格挡属于结合专项/源码的推理，不冒充该路线记录了ShieldResolved。Main深井下落仍4.633秒。

**完整目标仍ACTIVE，未整关验收**：新完整pre015743严格失败（60759ms），重现两个SwapGCHandleForType及另一个SetGodotObjectPtr句柄错误，且提前停在机械爪动作测试的历史恢复帧与当前视觉Secondary不一致。测试固定取第二个发布快照，本次却已有第三次发布，需同边界取证，尚不能判为生产视觉脱同步；没到旧BrokenTooth失败也不代表它已修好。新post015851严格失败（773ms），仍有旧15/现14目录断言和186Shape退出诊断。Main回调p95/p99/max=26.551/37.483/461.832ms，Explore为25.001/34.735/123.200ms；不是绘制FPS，稳定60帧、真人、旧档/资源、十区围合与最终美术继续OPEN。无新增媒体/付费/Git写入/上传/自动化；[本轮证据、复核、性能边界与下一步](.superpowers/sdd/2026-10-16-gallery-laser-carried-policy/progress.md)。下方为旧时点，不能替代本段。

最新续推：深井上升气流保留4–6秒合同，实际4.633秒。Steam弩哨兵的自动试玩策略已通过原生反例、修复与复测：最终010139专项严格通过（engine0/9383ms/guards0/三raw无诊断），真实挡弹/退休、K3命中/打断、两个J精确命中，敌HP2→1→0；致死后敌人真实QueueFree，最后J仍完成收招和六安全采样。提前由普通J击败的未接管场景也保留零助手输入/所有权。保存默认HP5且零受伤/死亡/回血；原900轮、能量预留、敌人数值均不改。能量负例005008在58.2提前J时真实失败，恢复正确预留后通过。独立审查C0/I0/M0；这是自动试玩/证据修复，不是新增玩家技能或美术。

新整路证据：正常Main010939严格通过（engine0/202671ms/guards0/零失焦/三raw无诊断），Title→同角色Boss击败→有支撑实体出口，继续D60不重复完成，零死亡恢复，深井仍4.633秒。新Explore010546仍严格失败（engine1/137058ms/guards0/零失焦）：实际HP1下Sentry用K2/J2完整尾段在原900的531轮结束，导弹兵于672轮两击后整组完成、HP1无伤，已通过旧卡点；之后摆锤走廊batch2激光兵在physics8057击杀玩家并请求恢复，不能把局部推进当完整探索通过。下一步核对激光出招和自动防守，不预先削怪/加血/加时。Main回调p95=25.419ms、p99=34.756ms、max274.578ms，**未达到稳定60FPS或真人手感验收**。

完整目标ACTIVE：完整pre/post、旧存档/历史资源问题、十区围合、最终美术与真人反馈仍OPEN。本轮只重新查看已归档v002井中截图，无新增媒体/付费/Git写入/上传/自动化；[本轮原生证据、复核与剩余门](.superpowers/sdd/2026-10-16-steam-sentry-recovery-policy/progress.md)。下方旧时点不替代本段。

最新续推：已修正自动试玩的Steam导弹兵策略，不是改敌人或新增技能。旧整段预警举盾/停车和35%主动撤盾分别取得原生失败反例；最终234256专项严格通过（engine0/6971ms/guards0/三raw无诊断）：HIGH真实D接近8/8、LOW低能量实际D+L挡住导弹爆炸，HP5下两次不同J命中精确受击盒，敌HP2→1→0。仅在实际支撑且所有攻击源水平同向在前时允许盾行进；松L后重算实际打击距离与完整反击窗口。独立审查C0/I0/M0；无生产AI/数值/地形/存档或原900验证时限更改。[本轮原生反例与证据](.superpowers/sdd/2026-10-15-steam-missile-keyboard-policy/progress.md)。

当前整路结果分列：正常Main234745从Title到同角色Boss击败/有支撑实体出口，engine0/203111ms/guards0/三raw无诊断/零失焦，深井仍4.633秒；当前Explore234446仍严格失败，engine1/105251ms/guards0/零失焦。最后导弹兵到physics6080才出现，原900结束时仍2HP，虽然真实L保持且已接近到约143.6px，但未完成攻击，不能把局部通过当Explore通过。下一步验证前段Sentry的合法接近/反击耗时，不加时凑通关。Main回调p95为23.653ms、max270.029ms，不是稳定60FPS或真人手感证明；完整pre/post、历史资源/存档/十区围合/最终美术仍OPEN，目标ACTIVE。没有新媒体、付费生成、上传、Git写入或自动化。

最新续推：首战专项已迁移到当前动态敌人与纯键盘合同。225623严格通过（engine0/4575ms/guards0/三raw无诊断）：真实J对应精确敌Hurtbox与HP3→2，真实W+A/K抓桥顶右面、取消自然落回FarBank；完整按住L/松L/再按J双语提示、可见敌人下的抓取优先级、八组布局与两次显式重试均保留。同玩家HP5、无受伤/死亡/回血，只发生两次测试主动请求的恢复。不是真人、OS按键来源或自然整路通行证明；未改生产玩法/美术。旧generic引导标题单元也已迁移到现有破损AI双语标题，文案资产未改。

完整回归仍未通过：pre225806进入旧BrokenTooth抓取路线后坠落，含两条故意构造无效PresentationProfile的WARNING；post230157仍15/14微场景目录断言与186Shape退出诊断。已单独修复旧路线测试拥有的离树manifest异常时漏Free：对照pre230500保持完全相同路线失败，三raw退出RID/ObjectDB/仍占用资源诊断从有变无，未新增GC/Dispose/日志豁免，也不是历史句柄/FATAL修复或完整pre通过。后续优先实际Steam盾行/反击共享策略及正常Explore，不把旧鼠标夹具兼容当成最终目标；所有全关、存档、历史资源、60FPS、真人、美术与十区围合要求继续OPEN。[本轮完整台账](.superpowers/sdd/2026-10-15-first-combat-keyboard-migration/progress.md)。

本次续推补记：已把深井机械爪指导专项迁到实际LowerCatch出生(1600,1040)/MiddleCatch底面880，并加强真实碰撞形状、完整胶囊支撑、无上拉空间、侧向阻挡和重复L负例。最终223052严格通过（engine0/7085ms/guards0/三raw无ERROR-WARNING-FATAL）；222730 UI专项通过但不代替完整负例。普通Main仍为下段213547既有证据，本轮未重跑。Explore215237重复失败；220821仅诊断性延长观察，935轮真实死亡，证实过早空举盾耗能、低于.35又撤盾迎向活导弹的驱动问题，不是单纯超时。临时1500扩展/强制断言已移除，原900仍保留，**没有把诊断案判通过，也没有削弱敌人**。完整pre222850走过新版深井指导，之后因旧FirstCombat把Marker2D强转Rust敌人失败；其旧Q/E、鼠标提示契约待正确迁移。详情见[本轮诊断与迁移台账](.superpowers/sdd/2026-10-15-steam-route-diagnosis/progress.md)。完整目标仍ACTIVE，未宣称整关验收。

最新续推：用户明确选择在现有深井加入有提示的上升气流，保留原4–6秒要求。已接入保存场景的局部下降制动、井壁风口/向上流线和固定无边框文字；全局重力、跳跃、冲刺、机械爪参数及地形/存档坐标未改。专项下落4.633秒、原安全垫落地HP5；普通地面763.996px落差仍扣1HP。强化212054还实测有风时真实跳跃/下冲/挂爪/放爪余势，精确free候选反事实避免空通过；暂停/重置/离开/卸载/staged/不同World2D控制通过，四项旧动作/键盘/爪退出/落伤回归严格通过。当前库存1647；[规格](docs/superpowers/specs/2026-10-15-abyss-updraft-design.md)、[计划](docs/superpowers/plans/2026-10-15-abyss-updraft.md)、[证据边界](.superpowers/sdd/2026-10-15-enclosed-canyon-entrance-pilot/updraft-progress.md)。

本轮入口测试已迁移到真实平岸/一次跳缺口，不再依赖七个已删台阶。早死两轮真实五击→HP0→同角色160,600复活通过；Main曾在下落3.117秒严格失败，促成本次用户选择。最终正常Main213547已Title→同角色Boss→有支撑实体出口，engine0/202927ms/guards0/三raw无诊断/零恢复与失焦；时长现用实际引擎物理帧计数，仍4.633秒，阈值未放宽。Explore经档案/冰封支路返回盆地后仍在steam-valves剩余导弹兵战斗失败，整案不通过；三次独立资源隔离未复现历史句柄问题，不等于修复。正常60FPS/真人/最终美术、十区完整围合与历史资源问题仍OPEN；完整目标ACTIVE，旧BLOCKED仅为已解除的重启前时点。

本轮媒体：[气流v001旧提示](E:/OurGame/Knight/过程图稿与视频/2026-10-15-深井上升气流-v001/README.md)及[v002修订/正式雾层截图](E:/OurGame/Knight/过程图稿与视频/2026-10-15-深井上升气流-v002/README.md)。Root归档后审看v002六张局部图/视频采样与三张正式HUD/雾层截图；文字上移45px避开头灯，方向流线可辨，整体规则色块仍NEEDS_WORK。v002追加后70项23,369,081字节逐项核对；旧索引/原片不覆盖。5.17秒310帧固定60局部录像实际耗6.51秒，不能当正常60FPS证明；音频流保留但本段实为静音，不是听感验收。无付费/Git/上传/自动化。

用户已明确选择“按该方向，先做入口样段”。当前已开始[入口实施计划](docs/superpowers/plans/2026-10-15-enclosed-canyon-entrance-pilot.md)，**不是十区地图已经完成**：保存版入口已新增可见实体左岩壁(0,0,48,720)，起点/对岸地基从40加厚至84，脚面636、出生160,600及稳定ID不变；入口及父场景/小地图目录已重新生成，旧文件保存在实施目录与各构建案中。

首次原生验证从真实标题Enter开始，A确实走出旧边界；新增墙后同一合同严格通过，实际碰撞法线X>.7、角色停在x68.00347，同角色/世界且零恢复。两个生成器只各改一个准确场景，源码/保护库存/二进制及副本身份已独立核实。此绿只属于185703冻结的“左边界”验证，不覆盖之后扩展的测试或完整入口。

后续已取得干净的横廊行为反例（191417），并移除保存场景的七阶：入口改为净236px横廊，上部裂谷三层间隔220px，顶岩/封端/遭遇/残片/恢复点/路线归属和父小地图已同步。192500专项严格通过：标题Enter、实体左墙、碰顶、一次独立跳坑、实际W/K抓顶上拉约89px、取消自然落地、冰面负控、跨口支撑及原路回返；同角色/世界5HP且零恢复，没有位置写入。冰面负控仅临时改此夹具的顶面材质并还原，不是未改世界的完整探索证明。

193545真实32px夹口RED后Lower左端改1400，保留右端2100；193854通过。194019真实Archive封底RED后Floor左端及FrozenCeiling左端同步改1400，保留右端，形成连续72px下降窗，避免40px角色零余量越界绕冰沿。194248专项26.8秒严格通过到原FrozenLeftCatch，5HP／同角色／零恢复。194552硬件录制同合同通过，六张实景与25.83秒原片/MP4已[完整归档](E:/OurGame/Knight/过程图稿与视频/2026-10-15-围合入口样段-v001/README.md)。旧阶梯背景停用，岩土色块仍是结构原型，整体视觉NEEDS_WORK；不是十区或入口最终验收。早前“无新媒体”为当时状态。

192657起五项键盘/动作/边界/检查点生命周期/六残片回归严格通过；195013旧schema迁移／菜单／隔离保留专项0/3546ms，原旧档／新档sentinel匹配。该迁移负向测试含七条精确预期拒绝WARNING（6×Title→Playing、1×Completed→Recovering），由原有分类器核对来源／函数／次数，不是stderr为空的运行证明。pre192749有两SwapGCHandleForType句柄诊断和另一个SetGodotObjectPtr→Finalize诊断，之后旧LowerCatch支撑夹具失败，不能合为单一已证因果；post192818仍15/14目录断言和186Shape退出泄漏，均严格失败未豁免。新完整Main/Explore和早死夹具仍引用旧七阶，尚待正确迁移再跑；旧terrainRevision地图应按合同拒收、核心进度保留，不能强留错误地图bits。冷继续／全支路闭环、真人及实时60FPS未验收，完整目标ACTIVE，无付费/Git/上传/自动化。

规划阶段记录：重新规划区域尺度和岩体/建筑围合，入口不要重复爬楼梯，泥土、岩壁、厚顶盖和真实不可通过处必须清楚；[空间规划v001](docs/production/2026-10-15-enclosed-canyon-layout-proposal-v001.md)保留为当时设计稿。十个功能区、连续U形主路、有限支路；尺寸是设计值，不是已验证全图。三张第三方参考与提案原文/展示副本已[归档](E:/OurGame/Knight/过程图稿与视频/2026-10-15-峡谷围合与区域尺度重规划-v001/README.md)，八项8,505,764字节逐项校验零差；参考不是可发布素材，无付费新美术。先入口样段验证包裹感，再处理全图、旧档/抓取/小地图/出口联动，不能拿旧地图成功代替新布局验收。

重启后已实际解除进程阻挡（启动17:44:04.5、09:48:45Z复查无Godot），四冻结源码/owner哈希保持。175111普通Main21.765秒失焦FAIL，用户确认当时切回聊天；环境调整后的175435 Main **199.463秒严格通过**：同角色Title→Boss→有支撑出口并D60，三种主路真实远程发射/击败、配重混编完成，零恢复/失焦，保护库存保持。175916 Explore8.093秒因失焦清Space、终点Minimized失败，仍在入口，**探索未完成**。均非真人/稳定60FPS/最终美术，历史原生FATAL、pre旧契约/post资源门仍OPEN；不再无改动重跑旧地图。工具目标重启后返回空，依此前明确设立目标的持续要求原文恢复登记，历史证据/工作量留在台账，不声称工具累计计数已续接。完整目标ACTIVE。下文BLOCKED和早前“主线未运行”为当时历史状态。

本轮素材/产物表（运行视觉仍为灰盒，不是正式素材）：

| 产物 | 位置 | 状态 |
| --- | --- | --- |
| 用户确认标题可见（2026-10-19） | [独立隔离标题与完整范围v029](docs/production/2026-10-19-user-confirmed-title-visibility-v029.md)；无新美术/音频/付费资产 | 用户明确看到标题；未自动Start、无角色/长路/玩法品质验收，历史失败与完整1–11保留 |
| 显示未通过/遥测与重置合同（2026-10-19，历史） | [后台六门与用户可见边界v028](docs/production/2026-10-19-telemetry-and-seamless-authority-v028.md)；无新美术/音频/付费资产 | 原两轮未见及六门5GREEN/1真实死亡恢复FAIL＋2 ObjectDB保留；v029只补独立标题可见 |
| 实际窗口双路线（2026-10-19，历史） | [正常启动与严格资格边界v027](docs/production/2026-10-19-visible-window-dual-route-v027.md)；无新美术/音频/付费资产 | 原窗口/置前与Main资格原件保留；Explore原FAIL不回填，用户未见由v028记录 |
| 同高落台/准入/窗口纠正（2026-10-19，历史） | [十三案例与启动边界v026](docs/production/2026-10-19-thruster-coplanar-top-and-temple-admission-v026.md)；无新美术/音频/付费资产 | 两窄RED→GREEN及headless Explore完成；post观察器/资源FAIL，旧Hidden启动不授予用户显示资格 |
| 旧高层碰撞／正常窗口复验（2026-10-19，历史） | [十二案例与支撑／前台资格边界v025](docs/production/2026-10-19-thruster-legacy-high-collision-and-contact-v025.md)；无新美术／音频／付费资产 | 原件保留；旧Main仅路线完成，当前由v026更新，非用户已见/真人/FPS |
| 导弹自动操作快照隔离（2026-10-19，历史） | [独立行为RED→GREEN与完整回归v024](docs/production/2026-10-19-thruster-missile-snapshot-isolation-v024.md)；无新美术／音频／付费资产 | 保留当时三夹具及pre通过、post旧高层落台FAIL；当前由v025更新，旧原件保留 |
| 神殿补给架／后台完整双路（2026-10-19，历史） | [六原生案例与严格资源边界v023](docs/production/2026-10-19-thruster-temple-supply-and-headless-dual-route-v023.md)；无新美术／音频／付费资产 | 上一冻结入架原十二硬检查及事务通过，headless Main／Explore到Boss／支撑出口；匿名FATAL仍OPEN，资格不搬到当前binary |
| 蒸汽下落／上行／双向捷径（2026-10-19，历史） | [十三原生案例与保存边界v022](docs/production/2026-10-19-thruster-steam-and-ascent-handoffs-v022.md)；无新美术／音频／付费资产 | 保留当时独立安全下落、Top／Grip1/2／双向捷径通过及补给架FAIL时点，当前后台状态由v023更新 |
| 蒸汽战斗／正常窗口复验（2026-10-19，历史） | [六原生案例与诊断修复v021](docs/production/2026-10-19-thruster-steam-bell-and-window-revalidation-v021.md)；无新美术／音频／付费资产 | 保留当时正常Main通关／正常Explore下落死亡／后台Top失败时点，当前后台状态由v022更新；正常唯一致死来源仍OPEN |
| 正常主线／蒸汽支路（2026-10-19，历史） | [七原生案例与冻结身份v020](docs/production/2026-10-19-thruster-normal-main-and-steam-branch-v020.md)；无新美术／音频／付费资产 | 保留当时Main成功及Steam接战待归因时点，当前状态由v021更新 |
| 短喷主线诊断／窗口双路（2026-10-19） | [五原生案与最终同冻v019](docs/production/2026-10-19-thruster-main-completion-and-window-validation-v019.md)；无新美术／音频／付费资产 | headless Main到Boss／有支撑出口严格通过；Explore在阀前下降FAIL，正常双路／真人／性能／最终内容仍OPEN，正式0/50不变 |
| 完整路线后段／惯性站位（2026-10-19） | [全部13原生案与最终同冻双路v018](docs/production/2026-10-19-thruster-full-route-and-native-stance-v018.md)；无新美术／音频／付费资产 | Main到九段上行后三role／Split，Explore到Basin横廊；两首失败仍OPEN，不是Boss／正常显示／整关交付；正式0/50不变 |
| 正常版本准入／峡谷惯性路线（2026-10-19） | [两正常FAIL、两窄叶RED及最终三门证据v017](docs/production/2026-10-19-thruster-normal-admission-and-descent-native-v017.md)；无新美术／音频／付费素材 | 局部真实完整停靠与ArchiveFloor neutral停点通过，正常双路／Boss／最终视听未完成；音频最后叶故障仍OPEN |
| 哨兵测试路线中性制动／时限适配（2026-10-18） | [两真实RED、主／备用原生反击及原cap证据v016](docs/production/2026-10-18-thruster-sentry-neutral-and-clock-native-v016.md)；无新美术／音频／付费资产 | 同冻两门GREEN；受控HP5、实际模块1800与原1215／73 cap，非正常整路、最终视听或玩家手感证明；完整1–11仍OPEN |
| 24常驻候选叶／装载器（2026-10-17） | [实际保存／重载／原生初始化](docs/production/2026-10-17-resident-saved-leaf-binding-native-v001.md)；隔离24positive＋2negative叶及24profile，无新美术 | native Ready／7negative符合，130RID严格FAIL；最终七门3GREEN／4FAIL。地图新增0，最终家族美术／全50未验收 |
| 子型配置持久化接入口（2026-10-17） | [24磁盘重载／实际挂树前消费](docs/production/2026-10-17-resident-subtype-profile-resource-v001.md)；无新视觉／音频／付费素材 | 14错误绑定拒绝；profile31RID严格FAIL，最终六门3GREEN／3FAIL。正式叶／实际入图／最终美术尚未完成 |
| 幽灵炮台开发承载（2026-10-17） | [真实显隐、停炮K／近身J与旧弹撤权](docs/production/2026-10-17-resident-ghost-sentry-native-v001.md)；无新视觉／音频／付费素材 | 12行为符合；新旧九专项资源FAIL，15门4GREEN／11FAIL。未正式入图／视听验收，不将probe重标正式身份 |
| 幽灵飞行／俯冲开发承载（2026-10-17） | [真实显隐、原生K→J与生命重入证据](docs/production/2026-10-17-resident-ghost-air-native-v001.md)；无新视觉／音频／付费资产 | 16行为符合，六专项资源仍FAIL；12门4GREEN／8FAIL。非正式族系／新增50入图，最终视听与整路未验收 |
| 常驻重装环境/外力/目标边界（2026-10-16） | [13项真实原生边界及恢复反例](docs/production/2026-10-16-resident-heavy-environment-boundaries-native-v001.md)；仅测试/owner扩展，无新美术、音频或付费资产 | 13项及原10行为符合，两个专项4RID严格FAIL；旧相关综合GREEN；正式新增0，不是全50/manager/最终族系验收 |
| 常驻重装与空中边界（2026-10-16） | [真实巡逻/Armor/Heavy开甲及6空中边界](docs/production/2026-10-16-resident-heavy-and-air-boundaries-native-v001.md)；复用保存场景，无新视觉/音频/付费资产 | 重装10/空中6行为符合，但两专项4RID严格FAIL；旧相关综合/键盘/动作GREEN，完整pre/post FAIL；正式新增0，非最终族系或50入图 |
| 常驻空中共同适配器（2026-10-16） | [实际二维运动/原生受力/返航与剩余门](docs/production/2026-10-16-resident-air-motion-native-v001.md)；复用保存Moth，无新视觉/音频/付费资产 | 14行为符合但4RID严格FAIL；旧飞蛾完整综合/键盘/动作/stream GREEN，完整pre/post FAIL；正式新增0，非最终Flyer/Diver或骨骸/幽灵资产 |
| 常驻冲锋与区域归属（2026-10-16） | [实际行为/严格失败/来源与剩余合同](docs/production/2026-10-16-resident-charger-and-stream-native-v001.md)；复用保存Skitter与原表现，无新视觉/音频/付费素材 | 15行为符合但4RID严格FAIL；旧冲锋完整综合及真实stream来源10项严格通过；正式新增0，不是最终族系资产或50入图 |
| 常驻冲锋死亡边界（2026-10-16） | [真实远抓点/原生死亡复活/资源对照](docs/production/2026-10-16-resident-charger-death-and-resource-differential-v001.md)；沿用原保存场景，无新媒体/美术/付费资产 | 20行为符合但4RID严格FAIL；旧冲锋/键盘/动作/stream通过，完整pre/post仍失败；正式新增0 |
| 锈甲真实坠落规则（2026-10-16） | [14原生检查/保存开口拉落/反例与边界](docs/production/2026-10-16-resident-void-contact-native-v001.md)；复用现有KillZone/旧表现，无新视觉素材 | 1660阶段普通低层存活/真实void致死/高速扫掠/睡眠/卸载负控通过；来源registry10项见新行，其他敌种/人口管理/50入图/独立4RID未验收 |
| 锈甲常驻真实行为样段（2026-10-16） | [代码/原生证据与剩余合同](docs/production/2026-10-16-resident-rust-patrol-native-v001.md)；复用保存Rust场景和原表现，无新美术/音频/付费素材 | 7模型＋15原生检查通过，未正式入图；其他职能/族系/外力坠落/跨区及独立4RID仍OPEN，不是50已可玩 |
| 档案洞扩宽岩台/当前小地图 | scenes/prologue/sectors/CanyonDescent.tscn；scenes/prologue/PrologueLevel.tscn；[证据](docs/production/2026-10-16-resident-player-access-and-archive-space-v001.md) | builder实际保存160→240px；原生几何/局部键盘通行通过，仍灰盒，非完整新路线/最终美术 |
| 岩台改造失败诊断录像v001 | [原件/原速MP4/viewport/采样/索引/审阅](E:/OurGame/Knight/过程图稿与视频/2026-10-16-档案岩台改造-录像失败诊断-v001/README.md) | 323项102493480字节逐项核验；未到新岩台，严格FAIL，不能冒充成功展示或实时60FPS |
| 新50常驻的放置准备 | [100保留区/41候选/9未解](docs/production/2026-10-16-resident-player-access-and-archive-space-v001.md)；[完整分配v002](docs/production/2026-10-16-prologue-population-final-plan-v002.md) | 实际新增0/正式Placement全null；真实Rust首样段见上行，正式族系美术、其余行为/管理/全50验收待做，不付费 |
| 区域尺度/实体边界提案v001 | [规划原文](docs/production/2026-10-15-enclosed-canyon-layout-proposal-v001.md) | 草案，未生成地图/锁定坐标 |
| 用户三张第三方参考及展示副本 | [本地归档](E:/OurGame/Knight/过程图稿与视频/2026-10-15-峡谷围合与区域尺度重规划-v001/README.md) | 已保存/哈希核对，仅作参考，未获素材授权 |
| 入口左边界及厚地基中间检查点 | scenes/prologue/sectors/CanyonEntrance.tscn；[原生台账](.superpowers/sdd/2026-10-15-enclosed-canyon-entrance-pilot/progress.md) | 保存场景局部碰撞已验证，不是新完整入口或最终美术 |
| 围合入口结构试稿v001 | [原片／MP4／实景及采样](E:/OurGame/Knight/过程图稿与视频/2026-10-15-围合入口样段-v001/README.md) | 局部结构运行已证；规则色块／背景空旷NEEDS_WORK，非最终或整关 |
| 深井上升气流原生占位表现 | scripts/mechanisms/CanyonUpdraft.cs；[v002原片/正式截图](E:/OurGame/Knight/过程图稿与视频/2026-10-15-深井上升气流-v002/README.md) | 实际下落4.633s；固定提示/风口/流线局部可读，整体原型NEEDS_WORK；不付费 |

最新审查修正：冰面负控不再只看最终锁定状态；现要求真实新K进入Aim并解析，连续观察100次发布、全窗口零roof锁定。故意漏K的反例200140严格失败，恢复真实K后的200302全局部合同0/26807ms，三raw均无原生诊断、1645源/109保护库存/二进制不变。入口owner拒绝命名RED豁免，external日志也独立严格检查。媒体194552与最终只差Traversal测试哈希，生产和场景1644条身份相同；不借录制旧测试结论替代新门。深层逆返、反向跳缺口及封端绕行负控仍未验。

最新自动试玩改进（Task11；仍待整场主线证明）：旧策略把全部预警都当成禁止反击，现只在所有来源都留足完整J、六实际回防帧及两采样/命令帧时反击，活弹／出招／未知来源仍硬拒绝。真实保存混编场景116帧完成两个不同J／指定受击盒／敌人3→2→1、玩家5HP无回血恢复。独立窄审指出紧边界漏算输入时间，经原生反例（约.417秒放行后出招侵入回防期）修正；该边界现拒绝，两击仍通过。M1最终elapsed≤180断言覆盖延后，实际116已记录，不冒称全时序穷尽。

这只是自动试玩判断改进，没有新增玩家技能、削敌或增加美术。五项旧专项通过；最终连续夹具实为18.857秒原生释放FATAL（0xC000001D），不是仅两条旧句柄报错；第四反击及整组未完成。pre旧类型断言及两ObjectDB警告／post目录及189Shape仍严格失败。新Main尝试在构建前被残留Godot PID50648拦住，**主线尚未运行**；该进程关联崩溃测试父进程，但当前路径／命令／退场状态读取被Windows拒绝。07:56最低权限只读查询的自身对照正常，目标仍拒绝访问，不能认定已退出；07:59第三轮复查仍列出同一PID／创建时间。安全诊断已耗尽，完整目标标为BLOCKED（不是完成、主动暂停或缩小范围），等待用户处理唯一匹配PID后恢复。未盲杀或绕过守护，用户对切窗的不确定回忆仍不用于归因。原生根因／普通Main+Explore／真人／美术与性能仍OPEN；无新媒体、付费、Git／上传／自动化。[反例、实测、复核和边界](.superpowers/sdd/2026-10-15-mixed-threat-policy/progress.md)。以下保留Task10时点。

最新局部修复（Task10）：下降区机会引导不再查找已移除的V1节点，改为检查当前实际支撑、W瞄准的可抓顶面、身体上拉空间和原生碰撞路径。真实J→K教学、取消落台、第二次上拉／Space借力释放严格通过；冰面、不可见目标、命中但无上拉空间、身体侧面受阻、按住L自然过热等边界均实测。正式Q交互／J攻击／K爪、地形、数值、AI和素材未改；局部夹具从第二层开始，不冒称自然走到此处。

已保存并审阅[7.4秒完整局部原片／展示MP4及九帧](E:/OurGame/Knight/过程图稿与视频/2026-10-15-下降区键盘抓取引导-v001/README.md)，目标／绳线／关键词可辨，整体仍NEEDS_WORK；没有付费新美术。独立窄审C0/I1/M1：UI测试入口误接纳额外原生错误的I1经真实反例修正并补验，中文“菱形”断言遗漏M1延后，实际中文已包含该词。最终局部专项7.052秒、键盘／动作／盆地目标回归严格通过；pre越过本修复后停在FirstCombat旧静态敌人类型期待，post目录／189Shape仍严格失败。原句柄/FATAL、失焦起因、持续混编战、普通Main+Explore、真人／最终美术／性能仍OPEN，完整目标ACTIVE。不会据用户不确定回忆归因失焦，也不自动补键或盲重跑。[实际证据、版本与裁决](.superpowers/sdd/2026-10-15-mixed-threat-policy/progress.md)。下文保留前阶段事实。

最新普通主线观察（Task9，仍未通关）：新攻防策略首次正常时速运行98.647秒，严格FAIL保留。已到配重混编战；原900观察结束时角色仍3HP、两敌存活，未到激光／Boss／出口，不延长预算凑通关。1692条资源记录完整，原35%门主动撤盾问题本案不再发生；但最后一次反击后597次决策都未满足完整反击门，60次清窗最长仅.333秒，小于完整J＋回防余量约.40秒，战斗进度停住。22次真实防住、一次角色4→3伤害来源仍未知；不把敌人受击统计误当角色伤害，也不凭相邻事件判来源。失焦0／旧FATAL未复现不算修复；真实前台尾迹仅首尾两次且非自身，不能据此归因用户。下一步验证持续第二次反击的各来源否决原因，再决定战术，保留全部生产数值／预算／活弹危险。无原样重跑或Explore。[完整运行事实与后续边界](.superpowers/sdd/2026-10-15-mixed-threat-policy/progress.md)。

当前续推（Task8，键盘／真实地形验证迁移，尚非整关交付）：旧V1石头／鼠标测试已改为正式V2保存盆地＋保存玩家的原生方向键／K测试。普通墙的预览与锁定均是实际墙面碰撞点，松开D/W后真实Reel前进10px；同一碰撞体只将抓取材质改为Ice后，墙仍挡射线但不可抓，K合法改选前方地面。两案1.569秒严格通过、玩家5HP／同世界保持；这里只验起拉，不声称穿过敌人或已抵达远端。未改地图、AI或力度。

fresh pre已通过新验证与补给，下一处旧Q/E模式教程测试失败；当前正式操作仍Q交互／J攻击／K爪，不恢复旧模式迁就测试。post仍15/14目录断言＋189Shape退出ERROR，严格保留。原键盘9.593秒及动作／过热7.076秒回归严格通过，新专项原生错误判定8项通过；独立窄审完成C0/I0/M2，固定夹具X边界与异常初始化清理覆盖延后，不外推全地形。另确认下降区生产机会指引仍引用旧节点，实际V2迁移待做，不能只改测试。原句柄／FATAL／失焦原因、持续混编战、普通Main+Explore、真人视听与美术性能仍OPEN；全目标ACTIVE。无新素材／媒体／付费／Git／上传／自动化，素材表不变。[原始结果、边界及后续门](.superpowers/sdd/2026-10-15-mixed-threat-policy/progress.md)。以下保留Task7历史时点。

当前续推（Task7，验证迁移，不是新玩法/美术交付）：旧劳工入口已复用正式V2保存Main的真实低顶通道，保留原300次观察预算、向玩家至少100px移动、连续地面支撑和同攻击ID三个阶段。先取得原生阻塞负控RED，再验证：92px通道内劳工移动约195px并完成攻击；隔离78px局部低顶确实产生碰撞、只移动约20px，现能被共享检查拒绝；原上路顶4116的24次支撑保持。实际旧测试工厂也迁到V2，树外隔离/三存储通道/不播种检查通过，最终专项12.058秒严格通过。

工厂坐标反例另出现一行189Shape原生退出ERROR，故保留为严格FAIL、不能算干净RED；已用原始日志复现判定器漏检并补强，新专项8项实际判定检查通过。fresh pre已越过劳工，下一处仍因V1机械爪坐标/已移除BasinGripStone失败；post仍15/14及189Shape，不豁免。反击、动作、键盘、四角色/14保存组四项回归严格通过，独立窄审完成C0/I0/M1，允许保留本迁移；负控同世界身份显式断言覆盖延后，证据版本边界已记账，不外推全关。原资源崩溃/持续混编胜利/普通Main+Explore/真人视听仍OPEN，完整目标ACTIVE；正式场景/数值/素材未改，无媒体、付费、Git/上传/自动化，素材表无新增。[本轮原生反例、实际结果和未完成项](.superpowers/sdd/2026-10-15-mixed-threat-policy/progress.md)。以下为前阶段记录。

当前续推（Task6，诊断不是修复）：有界出生身份记录仅专用编译启用，22项入口参数实测通过。独立窄审发现记录器注册/清理可能盖住首个异常；以真实失败反例修正共享清理流程，单组原生3.417秒严格通过，包括真实注册后受控失败的注销/释放。唯一四组连续诊断16.102秒完成：88条记录完整、20个出生/预检操作及8个预检释放可配对，原低能防御/短窗口否决/真实反击保持、玩家5HP不变。本次句柄错误未复现，**不等于旧句柄错误或FATAL已修复**；原生错误回调尚无实际错误见证，诊断开销也可能改变时序。

已在诊断进程退场并完成原件守护后，单独恢复普通构建；真实加载程序集检查确认诊断类型不存在。反击、混编、单远程、动作、键盘、保存场景六项严格通过（四角色/14组），其中后三项保留既有测试退出清理，不算原生问题修复。fresh pre仍在V2镜头四项通过后因旧劳工/地板静态路径失败；post仍15/14断言和一行189Shape退出ERROR，未改数字或豁免。全目标ACTIVE，持续混编胜利、普通Main/Explore、旧崩溃与真人视听仍OPEN；本轮无玩法/美术变更，无新媒体、付费、Git/上传/自动化，素材表未新增。[诊断、回归、窄审裁决和剩余门](.superpowers/sdd/2026-10-15-mixed-threat-policy/progress.md)。以下保留Task5原结果。

最新续推（Task5；自动战斗策略局部验证，完整目标仍ACTIVE）：已把配重混编自动试玩器的“全预警禁止反击”与“即时举盾需求”分开，不改玩家、敌人、场景或气力数值。两项具名RED后，早期预警真实松L、晚期提前举盾、授权Active空叶仍危险；独立资源专项8.603秒严格通过，原生近战1＋连发3次实际Guarded、角色5HP不变，七次真实预备J降能后仍观测到35%以下有效防御。反击改为双方真实Recovery共同窗口覆盖完整J＋采样余量，且保留一次攻击能量；不足窗口具名RED成立。单独反击对照4.162秒严格通过：真实Rust受击盒连接1次、敌人3→2HP、玩家5HP，完整动作后再观察6物理帧。

**本轮尚未验收**：四夹具连续进程虽打印行为成功，第四组出生时出现`SwapGCHandleForType / Handle is not initialized`，严格FAIL保留；单独对照通过不等于修复该句柄错误或旧FATAL。当前只定位到出生Instantiate链，具体资源/所有权根因未知，不强制GC或补生命周期猜测。五项冻结专项回归严格通过；fresh pre仍旧静态路径失败、post仍15/14和189Shape错误，独立窄审完成C0/I1/M1：I1保留为句柄验证阻断，M1为24能量反击保留门的边界覆盖待补；无新普通Main/Explore、真人/美术/性能完成结论。素材表无新增，无新媒体、付费、上传、Git写入或自动化。[Task5证据、裁决和后续门](.superpowers/sdd/2026-10-15-mixed-threat-policy/progress.md)。以下保留前阶段事实。

最新续推（破损AI复核与自动战斗验证，未整关交付）：回声/ECHO保留“声音破碎、信息完整”——短电流配稳定屏幕关键词，目标/动作/条件/风险/取消方式不截断；语气暂按低声、断续、努力恢复通信。当前**没有念出关键词的人声或TTS**，数据流来源、修复身体及专属小关卡仍候选。本次声音＋ECHO原生专项9236ms严格通过，源码复核保留首战反击/抓爪出口/过热和真实Boss缺项；连续听感、全部提示组合与部分WASD/W方向改键仍未全面验收。[功能与故事边界](docs/production/2026-10-14-enemy-factions-and-echo-guidance.md)。

自动试玩策略最新进展（尚非整关修好）：先对“选中近战就漏看旁边连发兵”取得真实RED，再接入同遭遇全部现行威胁/正确来源/松盾重查。局部真实近战1次＋连发3次Guarded、5HP不变；单角色、四角色、原动作/键盘回归通过，独立审查只允许一次普通Main收证。**该Main仍严格FAIL**：89.108秒，连发第三发前一帧真实J开始，后续玩家4→0HP、5151帧请求恢复；未到激光/Boss/Explore，focus0/守护0，无FATAL复现但原崩溃未解决。

进一步核对到“连发仍Active、当前弹叶暂空”的实际漏洞；新原生具名RED后，只让自动观察保留该授权Active承诺，未改敌人/血量/能量/时序/预算。新局部GREEN实际观察空档，近战1／连发3Guarded、5HP不变、无过热；独立窄审及四项冻结回归通过。**随后普通Main仍FAIL（89.365秒）**：首轮三连射与Recovery期间均未再提前攻击且保持4HP；后续出现能量.348／盾关闭，4→0HP/5169帧请求恢复。没有连发击败、激光、Boss或Explore验收，不能称持续混编战已修好；没有连续热量/精确伤害来源证据。下一门是实际持续防守与反击窗口，不盲降阈值、削敌或增预算。两版普通Main各一次，旧88.515秒及全部原件保留；目标仍进行中。

最新诊断续片（Task3，仍未整关验收）：只增加同一次威胁选择的标量身份与有界资源/决策记录，原生metadata具名RED→GREEN，混编/单威胁/四角色/动作/键盘五项严格通过；未改生产玩法、能量阈值或自动战术。独立窄审后唯一普通Main在26.598秒因失焦边界W/L实际状态清空、自动持键意图仍在而严格FAIL，**尚未进入配重资源观察，BEGIN/END均0**；不能解释上一案松盾或死亡原因。用户记忆不确定、倾向没有切窗/操作，不归因用户。精确版本源码确认应用失焦先清键再通知节点，窗口焦点标志走另一条事件路径，解释了当时HasFocus=true，并未查明谁导致失焦。下一步仅补必要的被动焦点证据，不自动补键或盲重跑。[引擎顺序与证据边界](.superpowers/sdd/2026-10-15-mixed-threat-policy/native-focus-source-audit.md)。

本轮续推（Task4，被动焦点取证）：已在原实时测试中加入独立、事件触发的64条前台窗口/PID尾迹，不读取标题或内容、不抢焦点、不补键、不逐帧轮询；与旧按键日志的冻结及玩家退场分离。具名RED后，新原生boundary专项6.029秒严格通过：真实撤回W仍被原检查拒绝，旧日志不变，新记录器可在driver停止后继续接收模拟焦点回调；缓冲/未知与竞争分类/停止/隔离不覆盖写入均验证。该专项无显示窗口、不证明真实窗口信号。冻结后的混编、单威胁、动作、机械爪和四角色专项均通过；独立窄审C0/I0，两项诊断限制延后并逐条裁决。

随后唯一普通Main **89.794秒仍严格FAIL**，但此次到达配重混编战、focus0，实际获取非零自身HWND/PID，失败退场写出独立sidecar（首尾2行均自身前台、错误/未知/覆盖0；不是持续前台证明，也未复现真实失焦信号）。资源记录1..676完整输出、丢弃/错误0，终于确认**本案自动试玩器**在能量34.8%且当前敌人Active仍余.14秒时，被35%防守门主动撤盾并向敌移动；当时盾可用、未过热。随后反击支付12能量，且两次回到35%以上就短举盾、耗能跌破又撤盾，恢复等待反复刷新。最终4→0HP/5169帧请求恢复；伤害来源仍unknown，不能追认旧案原因或称玩家护盾坏了。下一步整体分开预警/即时防守/恢复/反击职责，不再用单阈值补丁冒称混编战已解决；无新Explore/盲重跑。

全量pre仍被旧静态劳工/地板路径阻断；post仍15/14目录断言和一行189Shape分配退出ERROR，不改数字或豁免。已静态核对V2应测真实SteamValves动态劳工低顶通道，不是简单改路径或换开放高台，运行迁移/堵塞负控仍待做。真实用户109项/原始参考/源码1645及工具二进制守护保持。原资源FATAL、持续混编战、普通Main/Explore及真人视听仍OPEN，完整目标ACTIVE，无新媒体、付费、自动化、发布或Git写入。[本轮混编反例、结果、审查和剩余门](.superpowers/sdd/2026-10-15-mixed-threat-policy/progress.md)；[前一轮单远程记录](.superpowers/sdd/2026-10-15-ranged-threat-policy/progress.md)。以下保留历史时点。

最新续推（23:34；诊断有进展，整关仍未验收）：核对官方精确源码与冻结程序集后，补齐19个自有RefCounted的**条件诊断**，仅记录托管类型/指针标量/释放前后，不改玩法和资源所有权。独立审查发现日志名称被严格FATAL分类器误判，以真实producer/classifier反例修正名称、保留原错误门；相关21＋15＋43项通过。一次正常时间、但带诊断的Main跑到Boss真实死亡、物理出口并稳定60帧，90次观察释放（71次finalizer）均配对返回，本次无原生FATAL；**不等于原崩溃修复或普通Main验收**。

最终事件审计还发现旧自动战斗器未把新远程前摇计入威胁，所以该次激光兵0发射不等于它的射击逻辑损坏，也未证明必须挪动场景。下一门先核对自动试玩攻防策略的远程覆盖，再依据真实体验决定站位/节奏调整；保留正常可打断机制，不按发射计数硬等凑验证。

该次196.660秒最终严格FAIL：激光兵只前摇、尚未实际开火就被两次正常攻击击败，不能称三种主路远程职能已完整展示。诊断focus0/恢复0不外推人工手感/性能。随后已恢复普通无探针二进制，相关模型隔离测试通过；其既有退出GC清理只算测试退场，不作为崩溃或整关证据。当前源码仍1645，仅两条件诊断C#变化，真实数据109项和全部运行守护保持；无新媒体/付费/发布/Git。下一步检查激光兵遭遇站位与展示时机，同时保留资源FATAL待定位项。AI仍为短电流＋完整固定关键词，尚无人声；完整目标ACTIVE。[诊断计划、审查、原始案例及边界](.superpowers/sdd/2026-10-14-refcounted-fatal/progress.md)。以下均为历史时点。

最新续推（22:50；局部修复，整关仍失败）：实际Main发现配重下一批被玩家站位挡住后永久不重试。先以真实键盘让位仍不能恢复取得反例，再修为每.25秒低频全批重查；合法后完整新.60秒预警、出生时重验，保留原安全距离/几何/原子双敌。67.089秒严格专项通过：正常5HP同玩家让位、暂停/重置/退出、两状态普通生命周期、真实blocker途中变化、无信号/预警storm及真实击败才完成。不是性能/人工或同步回调重入证明。

随后普通Main真实让位到4700.5107并恢复2live，原连发兵出生/前摇已出现，但在其后发生原生C#资源释放FATAL（GodotObject.Dispose/Finalize；90.143秒异常退出），本版整关仍严格FAIL；责任对象/与新改动的因果未明，不能用局部通过替代。原始日志/二进制与失败均保留，真实存档109项/原始参考身份/源码/工具/二进制守护稳定。V2镜头通过、普通Space+A安全脱困；K等待到达路线、旧V1劳工与目录/Shape门、完整Main/Explore及最终美术/听感仍未完成。AI当前为短电流＋完整屏幕关键词，尚无人声；背景/修复身体/专属小关卡仍候选。完整目标ACTIVE，下一步先定位释放崩溃；无新媒体、付费、自动化、发布或Git写入。详见[实际证据与边界](.superpowers/sdd/2026-10-14-enemy-expansion/progress.md)。以下保留历史时点。

最新续推（20:32；局部实装与原生验证）：四角色已加入运行时Catalog，保留旧22名额后为26（主路17／可选9），原12组不变。散弹/激光/连发在主路，导弹在蒸汽支路；配重区原近战＋连发同批。实际保存关卡的四角色／八候选出生、真实前摇／发射／Health击败、混编两者存活／两次击败后完成、全批0.60s警告与生命周期专项通过（35.382秒）。配重旧Rust仅允许经原生扫掠的4px自然沉降，并加一个明确备选点，原primary/batch不变；中央／端点落脚均通过。**这些不是实际Space/K逃脱、公平性、密度或新整关验收**，蒸汽／电磁Area2D配合亦待实测。

新声音/引导已通过真实四角色1/3/1/1、去重/取消/暂停/死亡；独立审查追出同帧抓爪取消后的旧事件仍可播放，实际反例后接入actor既有同步权限，完整专项40.770秒通过。复用已有音库，非四套新音色或人工听感。旧声音＋ECHO的两ObjectDB警告经标量ID确认属于自建短电流WAV；保留即时Shutdown断言，仅测试退场等待实际AudioServer混音后，普通非verbose严格通过（9.125秒，实际4帧/55ms混音清理）。**不是一般生产泄漏或旧186/189Shape已修复**，所有失败原件保留。

AI语气明确为短电流＋完整关键词；数据流来源、修复身体及专属小关卡仍候选，当前无完整人声配音。源1645（相对1621新增20 C#＋4叶tscn），父场景未重生成，无付费/新媒体/Git/自动化/真实存档修改。完整目标未完成；最新用户续推后目标工具回读ACTIVE，继续真实配重键盘逃脱、V2相机门迁移和正常路线新兵种事件验证，较早blocked状态仅为历史，未恢复取消的自动化。详见[实施记录](.superpowers/sdd/2026-10-14-enemy-expansion/progress.md)。下方均为历史时点，不替代本段。

最新续推（实体远程隔离战斗）：四种真实actor与武器已实现：五散弹同轮一次伤害、三连发独立轮次、有限导引且只爆炸一次的导弹、锁向/挡墙的真实宽激光。当前完整actor原生专项14行为组通过，模型/账本/几何/弹体/真实保护范围及原键盘攻防回归亦通过；已复现并修正同步抓爪回调仍漏发尾弹的问题。旋转光束/世界polygon保护区也有真实消费者反例后修复。程序绘制的四种武器轮廓和短爆炸余光只算功能占位，不是最终家族美术。

**尚未完成正式地图接入，仍原22个普通出生名额。** 四叶场景已由C#生成并非覆盖加入，实际重新加载的完整14组原生攻击专项通过（58.578秒，无固定帧率）。最新修复正式FogController漏掉五颗Recovery活散弹的实际反例；四角色正式挂载/逐叶/暂停/打断/死亡/卸载/标题专项及真实NVIDIA GPU的预警、每颗活弹/斜激光、遮挡、取消旧白边和双来源独立显示均通过，原Moth GPU回归通过。测试准备失败原案与独立审查发现的两项测试缺口亦保留并补验；局部GPU并非整关观感/性能或真人验收。

主路三职能/局部混编、声音接入及新录像仍待完成。运行源清单1621→1643为新增18 C#＋4 tscn，原始生成副本/哈希/逐步记录保留；没有重生成五区/主场景（当前group运行时解析Catalog，无需重复覆盖），无新栅格/付费/媒体，真实用户109项及运行守护稳定。完整目标ACTIVE，旧V1测试迁移/失焦/186Shape告警仍OPEN，无自动化/发布/Git写入。[本轮原始反例、通过范围与下一门](.superpowers/sdd/2026-10-14-enemy-expansion/progress.md)。以下保留此前时点，不替代本段。

最新续推：四种远程的共享时序基础已实现（19个行为组），包括真实不同的发射节奏、锁向、打断、独立身份及只读参数快照；伤害账本已通过真实生命值/盾牌/重入/双受击框/受击组件替换检查。还修复了受击路径改变后仍向旧缓存对象投递的实际反例，原纯键盘攻防/过热操作回归通过。**当前仍只有原22个普通出生名额，四个新actor、实体弹道、导弹爆炸、宽激光、造型与正式摆放均尚未接入，不能说本轮已增加四种可玩敌人。**

新源清单逐路径1614→1621，仅7个C#新增，无新图片/付费；真实用户库存109及运行守护保持。全量pre/post仍分别被旧V1盆地镜头坐标和旧15目录期待阻断，保留严格FAIL、不调数字凑绿；失焦输入和旧资源告警也未闭合。下一门是原生实体投射/宽光束/遮挡、每活弹预警生命周期，随后主路三职能与局部混编实测。完整目标ACTIVE，无自动化/发布/Git写入。[新远程基础的真实反例、验证与待办](.superpowers/sdd/2026-10-14-enemy-expansion/progress.md)。以下保留先前时点。

最新用户意见已纳入v013台账：增加散弹/机枪/导弹/激光敌兵，区分精工、废弃组合、骨骸与灵能来源；破损指导AI开发名“回声/ECHO”，材料修复/获取身体与数据流来源小关卡为后续支线构想，未冒称实装。核对现有5普通种/22顺序名额、多数单敌串行、仅1远程类型，丰富度仍未完成。

当前先修最直接的眼痛问题：固定320宽阅读列、游戏UI去装饰框但保留键盘焦点/资源/选中标记；GUIDANCE改为破损关键词，必要动作、条件、解除方式保留；隐约幽光缓入缓出；接入0.19秒独立短电流声及8秒限频。Boss条跳动、长抓附文案不容纳、同提示跨会话静音均有实际反例后修复，当前局部UI/声音、正式双语场景与菜单说明strict专项通过。Root已审看5张归档原尺寸图，固定关键词可读，整体仍NEEDS_WORK。

当前ECHO版实际路线录像在第一场战斗后因应用失焦通知附近的模拟D状态撤回而严格FAIL，未到盆地/自然死亡；不自动补键、不无诊断重跑或拿旧片充本版。暂停布局断言通过但退出186个Shape RID error，仍严格FAIL。完整失败片和原件分版保留，正常整关/真人/连续听感未验收。敌人数量/种类本轮尚未增加，不以UI改进替代它；首批主路至少3种新远程的候选位置正重新核对。[当前进度及证据](.superpowers/sdd/2026-10-14-echo-guidance/progress.md) · [敌人/世界观/AI功能记录](docs/production/2026-10-14-enemy-factions-and-echo-guidance.md) · [无边框双语过程图](E:/OurGame/Knight/过程图稿与视频/2026-10-14-回声无边框引导-v001/README.md)。无付费或发布，完整目标ACTIVE；以下保留先前时点。

最新批准精修已实现：电磁机关改为暗铜细连续轮廓＋清晰角标，右侧线圈增加侧面厚度、裂纹和断绕组，保留左行电弧及原危险/伤害/通路。正式双语场景与实际死亡恢复录像通过，Root已审看原尺寸图片/录像采样；整体仍有矩形提示感、敌人叠加和原型背景，继续美化。GPU像素断言通过但第一次退出strictFAIL保留；随后仅显式释放夹具自建物理世界/临时形状，一次严格复核通过，未扩大成修复所有旧资源故障。

新版正常Explore已完成Boss及支撑出口/D60稳定，失焦1次、max回调94.338ms，因此手感/流畅度仍未验收。新版Main补验在首战因模拟持有L被撤回而FAIL，同帧有失焦记录；源/用户/工具守护保持，不以重试碰绿或旧Main成功代替。先核对输入环境，真实OS/人工整关及两项V2聚合迁移仍待完成。[实现、故障与运行记录](.superpowers/sdd/2026-10-14-coil-refinement/progress.md)；[原片、7秒预览和过程图位置](E:/OurGame/Knight/过程图稿与视频/2026-10-14-电磁线圈精修-v002/README.md)。原件及历史版本均保留，无付费或发布。完整目标ACTIVE，下文保留先前时点。

最新续推（出口完成保存保护）：修复实际文件锁拒绝时“画面已通关、磁盘未完成”的顺序错误。现在保存成功才结算；失败会安全暂停，支持松开确认键后重试或返回标题，已保存六件收集物和Boss记录保留。文件替换/读取拒绝、持键Echo、返回再Continue、正常物理出口、旧存档迁移及样片专项通过，独立两个生产文件窄审无剩余阻断。当前正常Main自动Title→Boss→有支撑出口/D60通过；是引擎输入，尚非OS/人工整关。

全量聚合仍有两项FAIL：旧盆地镜头坐标与当前V2地图不符、旧15个场景目录与当前14个路线节点不符；正核对迁移覆盖，不改数量凑通过。6.7秒受控诊断片及原件已[归档并抽帧审阅](E:/OurGame/Knight/过程图稿与视频/2026-10-14-出口保存保护-v001/视觉审阅-root-v001.md)。[本轮范围、反例及结果](.superpowers/sdd/2026-10-14-completion-save/progress.md)。线圈精修按最新批准另行推进，完整目标ACTIVE；以下保留较早时点记录。

最新续推（首次加载故障定位，未修复）：原fragments完整前缀只增加一次verbose加载日志，保持原strict判定、隔离保存和强制冷进程读档；本次聚合及冷读退出0、stderr为空，但日志模式会改变时序，**不抹去080648案的两条Handle错误/严格FAIL，不以未复现宣布修复**。详细stdout仍有38个退出string-name诊断。当前产品DLL、场景和玩法未改；source834、真实用户库存109、六binary前后守护一致。

精确版本源码将旧错误进一步限定为`GCHandle.FromIntPtr`收到零句柄；显式资源图及当前DLL默认资源创建路径已核对，仍不能确定责任对象或原生调用点。暂不强制GC/KeepAlive、不乱改缓存或换引擎，也不重复运行碰绿。外部wrapper唯一诊断开关及其记录已保存；本案没有helper冻结副本/前后hash，不夸大全量保护覆盖。线圈外观沿用已批准工作版，橙框偏强、局部拥挤和原型感未解决；无新媒体或付费，完整目标ACTIVE。[本轮证据、线索与下一步边界](.superpowers/sdd/2026-10-14-fragment-lifetime/progress.md)。下文是先前时点记录。

最新续推（试玩构建优化，验证未全部闭合）：发现原Debug程序集实际禁用了编译/JIT优化。真实强制重编、核对PE标记并对照运行后，`Knight.csproj`默认开启Optimize，保留Debug/TOOLS、原C#玩法/地图/素材；同一近线圈电弧同步调用中位11.09→4.28ms、整体fog入口17.41→7.89ms，效果数量和伤害不变。**局部计时不是FPS，优化也作用于测试观察代码，不能将全部收益归因于电弧。** 精确调试及返回试玩都应强制Rebuild，详见下方记录，避免增量复用旧DLL；优化模式会影响源码单步/局部变量观察。

当前新默认Main与Explore正常时间自动路线均从Title经同角色完成Boss、有支撑出口及D60稳定检查，恢复0、失焦0。回调p95分别35.093→26.804ms、33.325→24.764ms；>33.3ms比例分别7.118%→1.856%、5.019%→1.559%。仍有59.666/54.933ms尖峰；优化同时作用于观察代码，每版一次且Explore输入观察帧数略异，**不是稳定60FPS、严格同输入因果A/B或真人OS输入验收**。

原13项机械爪/输入/碰撞/摔伤/复活/保存/第六残片专项，以及正式Q、独立Boss门禁通过，但**fragments综合严格FAIL**：首次加载正式场景时出现2条Godot `Handle is not initialized`，虽engine0且断言完成仍不能通过，强制冷读阶段未执行。历史旧默认配置运行已有同签名故障（旧聚合DLL无留存副本，未直接验PE），具体资源生命周期根因未明，也未排除优化改变发生概率；不过滤错误、不重试到绿、不盲改资源缓存。真实用户存档及运行版本守护不变。最终视觉/内容丰富度、正常流畅度、人工整关及旧UI退出诊断仍OPEN，完整目标ACTIVE；无新媒体、付费、自动化、发布或Git写入。[本轮证据、失败与调试命令](.superpowers/sdd/2026-10-14-optimized-build/progress.md)。以下保留历史时点，不替代当前状态。

最新续推（电弧重复遮挡扫描优化）：保留已批准的右侧损坏线圈、左行电弧、角色软避让、原危险范围和全部效果，仅将四边形的四次重复查询合并。具名缺失/旧成本RED后，等价、native头灯及实际GPU可读性专项通过；近线圈电弧同步调用中位15.72→11.09ms，绘制段仍189个。**这不是所有情况都加速**：专门构造的晚遮挡案例内核耗时约为原2.49倍，已保留为风险；组件诊断也不是FPS认证。

最终同版正常时间Main与Explore均从Title经同一角色完成Boss、有支撑出口及D60位置/隔离存档稳定检查，恢复0、失焦0；源1614/真实用户库存109/六binary/七helper守护不变。回调p95分别38.902→35.093ms、36.410→33.325ms；>33.3ms占比9.174%→7.118%、7.392%→5.019%。只是每版一次带观察开销的自动运行；Explore自适应输入略不同，不能称严格同输入因果A/B或真人键盘验收。**盆地仍有持续慢帧，整体流畅、美术/内容丰富度与真实OS/人工整关仍OPEN**。按测试先行与运行复核保留改动，不以构建成功代替试玩；原UI资源退出诊断未闭合。无新媒体、付费、自动化、发布、Git写入或真实存档修改，完整目标ACTIVE。[本轮修改、反例、双路线与边界](.superpowers/sdd/2026-10-14-lane-quad/progress.md)。以下均为历史时点，不替代此段。

最新续推（盆地组件直接计时）：已在实际 Windows/NVIDIA 场景确认线圈附近电弧提示的同步调用中位耗时约15.72ms，地形3.59ms、引导布局0.87ms；整体fog绘制前入口（含同步观察者，另测）21.82ms。这是受控位置的重复调用诊断，不是FPS、GPU耗时或性能达标；近处189个电弧绘制段/39个地形段及原伤害保持，正式存档守护稳定。下一步优先减少电弧四边形的重复遮挡扫描，不减少效果或更改玩法。

本轮没有生产修复或新媒体。新正常Main自动路线完成，90秒采样也已保留；已纠正“采样栈占比可当CPU热点”的解释，并将非GC暂停单列。两个受控组件案真实运行、清理及前后guard通过，原编译失败保留；新增test-only入口经独立静态窄审无阻断。正常性能、真实OS/人工整关、视听与内容丰富度仍OPEN，完整目标ACTIVE。[最新测量、边界与下一步](.superpowers/sdd/2026-10-14-basin-profiler/progress.md)。以下保留历史时点。

最新批准回执（线圈发射器）：用户确认右侧损坏线圈＋向左持续电弧方向。核对发现工作版已含该实现，本次没有重复修改生产代码。当前 assembly 重跑真实 NVIDIA GPU 可读性专项通过，包含线圈方向、原边界、墙体遮挡和角色核心避让；源码1610项／真实用户库存109项／六binary／七helper前后保持。既有7秒预览及其余17份展示媒体大小/SHA复核零差，线圈源码与录像归档版本一致；该片是此前固定60自动运行记录，不冒称本次新录、真人或正常性能证据。橙框过强、机关造型和整关流畅度仍待改善，完整目标保持ACTIVE。[本次限定复核](.superpowers/sdd/2026-10-14-coil-actor-clarity/progress.md#批准回执后的现版复核)。

最新续推（表面遮挡查询复用）：按测试先行，将四边形内重复局部点变换12→5，并复用同一查询已算出的不相交边结果；保留原float世界坐标、薄墙、旋转、边界接触和实时遮挡。旧版成本RED、第一版未达门槛FAIL均保留，最终两patch内核中位耗时约为原76%/78%；旧wedge、native头灯和实际GPU可读性回归通过，独立限定代码复核无阻断。

同版正常Main/Explore均通关且原数据守护稳定，但**本轮没有证明整关明显更流畅**：Main p95约38.9ms基本不变，攀升段出现约198ms尖峰；Explore p95约36.4ms、小幅改善。Main盆地仍有至少7.65秒连续慢帧。局部优化不替代正常性能验收，下一步重新测量当前盆地/攀升热点；真实OS/人工整关、连续视听及最终美术/丰富度仍OPEN。完整目标ACTIVE，无新媒体、付费、自动化、发布、Git写入或正式存档修改。[本轮改动、失败及双路线结果](.superpowers/sdd/2026-10-14-patch-reuse/progress.md)。以下保留历史时点。

最新续推（迷雾重复计算优化）：已按系统排错和测试先行，将单次遮挡查询最多9次局部坐标变换减为3次，首边命中仍只算2次；保留原薄墙、旋转地形、边界接触及实时遮挡规则，不降低画面效果。旧版成本反例、首次未达标版本均保留；最终几何差分、native头灯和真实GPU可读性专项通过，独立限定审查无Critical/Important阻断。

同一最终版本的正常Main与Explore都已完成原标题→Boss→有支撑出口／D60／一次结算，恢复0、失焦0，源及正式用户库存守护保持。回调墙钟p95由约50.4／47.8ms降到39.0／37.3ms，>50ms比例由5.21%／4.50%降到0.84%／0.61%；这是带测试观察的同路线诊断改善，不是稳定FPS或隔离因果性能认证。**盆地区仍有至少7.75／7.57秒连续慢帧，流畅度未验收。** 下一步继续减少已测热点的重复工作，真实OS/人工整关、连续视听、美术及内容丰富度仍OPEN。完整目标ACTIVE；无新媒体、付费、自动化、发布、Git写入或正式存档修改。[本轮改动、失败保留、双路线证据及剩余问题](.superpowers/sdd/2026-10-14-wedge-reuse/progress.md)。以下保留历史时点。

最新续推（正常慢帧归因）：对同版正常Main的自有隔离进程做90秒本地采样，原路线终态通过，生产源码/六binary及真实用户库存未变。独立直接重算原始栈，fog回调线程约61.42%的重建采样时间位于迷雾绘制前处理，测试观察约15.47%；这支持先减少遮挡几何的重复计算，不能只归咎于测试工具。原trace解析成功、报告丢失0事件；121个GCStart只证明事件出现，不代表暂停耗时或分配来源。

本轮**没有性能修复**，采样占比也不是CPU/GPU占用或帧率承诺。下一步是验证并复用同一查询的三点坐标，保留薄墙/旋转地形/边缘接触及实时几何；不直接减少迷雾、放宽遮挡或加跨帧缓存。带采样器的路线不能冒称无开销性能A/B。无新媒体、付费、自动化、发布、Git写入或正式存档修改；完整目标继续ACTIVE。[采样证据、已知边界及下一步](.superpowers/sdd/2026-10-14-basin-profiler/progress.md)。以下保留历史时点。

最新续推（正常时间完整双路线）：当前同版Main与Explore均在真实GL窗口、TimeScale1、无fixed-fps／movie下，从原标题经同一角色完成Boss和原有支撑出口；完成后继续D60帧，位置／一次结算／隔离存档保持。Main遭遇8激活／8击败，Explore11／10并通过双向支路，两案恢复0／0、失焦0；独立严格原始日志复核通过。Root核对每案源1604／真实用户库存109／工具7前后保持，跨案六binary及源清单相同。仅新增当前验证入口，未改变生产玩法或美术。

新发现而非已修复：两条路线都在盆地区持续慢帧，已保留连续段至少7.84秒／11.25秒；回调间隔p95为50.384／47.791ms。它是包含测试观察与渲染等待的墙钟统计，不是GPU归因或显示FPS；详细长帧位置有容量缺口，不能称后段流畅。下一步先做test-only被动耗时归因，不凭猜测改雾或线圈。上述自动引擎键事件不是真人／OS输入或最终美术验收；本轮没有新媒体，完整目标继续ACTIVE。无付费、自动化、发布、Git写入或真实存档修改。[本轮证据、性能边界及下一步](.superpowers/sdd/2026-10-14-normal-full-route/progress.md)。以下保留历史时点。

最新续推（电弧避让角色）：已先用真实GPU复现“电弧压住身体核心”，再仅为角色附近的电弧/箭头/光点做软淡出，保留线圈、危险边界、伤害、墙体遮挡和通路。三个位置GPU检查、正式场景中英文/三张实际角色受控图及原标题→盆地→实际伤害死亡→同角色墓灯恢复录像通过，独立窄审无Critical/Important阻断。它是透明度近似，不是真正后层特效或免伤区；失败原案及退出告警保留，不称全套通过。

原件、完整32.833秒录像、7秒预览与图已[分版归档](E:/OurGame/Knight/过程图稿与视频/2026-10-14-电弧角色避让-v001/README.md)，初始1007项242,159,786字节经Root全量目标大小/SHA核对零差。Root原尺寸看三张角色图和七视频采样，身体可辨、整体仍NEEDS_WORK；橙框抢眼及敌人/效果拥挤未解决。固定60自动输入和抽帧不替代真人整关/正常性能/连续视听验收；完整目标继续ACTIVE，无新付费美术或真实存档修改。[本次范围及运行记录](.superpowers/sdd/2026-10-14-coil-actor-clarity/progress.md)。以下保留历史时点。

最新续推（第六件残片与 Boss 门）：新增原 `temple-upper` 实际接触的保存失败→五件物理挡门→离开重试→六件准入组合验证。headless及真实GL均通过：失败不消失/不报成功，重试只有一次反馈，已拾取的第三次接触不重写存档；实际D经过原trigger、同一Boss仅一次进入Intro。独立新进程从GL真正写出的五/六件存档副本，通过标题Down/Enter继续，两种门禁结果均正确。不是手工播种JSON，也不是自然整关键盘路线；cold为同一新进程内两个新场景，不称各自独立进程或显示冷续关。

本轮未发现新生产故障，未修改玩法或美术；独立窄审查无Critical/Important阻断，保留离树初始化失败时测试清理可能遮蔽错误的非阻断风险。三案源码1604/真实用户库存109/工具9前后核对一致，真实存档未变。[三案与五图已归档](E:/OurGame/Knight/过程图稿与视频/2026-10-14-第六残片与门禁-v001/README.md)，初始728项大小/SHA经Root全核零差；实景02–05已原尺寸审阅，功能反馈可辨，整体视觉仍NEEDS_WORK。完整目标继续ACTIVE；Q负对照、自然整关/真人输入、连续视听性能及场景/敌人/机关美化仍未验收。[事实、运行与范围记录](.superpowers/sdd/2026-10-14-fragment-six-admission/progress.md)。以下保留历史时点。

最新续推（墓灯保存一致性）：已实际复现并修复“保存失败但墓灯已亮、运行复活点与磁盘不一致”。正式layout2现在先保存成功再提交复活点和成功反馈；失败显示中英文提示，离开后重新接触可重试。两种真实文件锁、最终GL画面、替换锁下重复接触、独立冷进程标题Continue、旧世界装卸/恢复均通过。额外发现并修复旧样例离树配置引导时的启动异常，layout1接触与Continue回归通过。失败原案均保留，不改变真实用户存档。

最终同版标题→盆地原路→5次实际混合伤害死亡→同角色墓灯满血复活→D移动的Vulkan录像通过，3件残片保留。自动固定60不是真人、正常帧率性能或整关验收；机关/场景/小地图仍需美化，整体目标继续ACTIVE。过程图与证据按[墓灯一致性v001](E:/OurGame/Knight/过程图稿与视频/2026-10-14-墓灯保存一致性-v001/README.md)、[后续失败及最终回归v002](E:/OurGame/Knight/过程图稿与视频/2026-10-14-墓灯保存一致性-v002/README.md)、[完整原片与7秒预览v003](E:/OurGame/Knight/过程图稿与视频/2026-10-14-墓灯保存一致性-v003/README.md)分版保存；三批初始673/1,824/513项目标大小/hash均经Root全量核对零差。局部录像不是保存拒绝重试过程录像，也不是整关成片。[本轮事实、测试、审查和剩余风险](.superpowers/sdd/2026-10-14-checkpoint-save/progress.md)。以下保留历史时点。

最新续推（已批准的电磁线圈发射器）：原电磁场右侧加入损坏绕组外壳、两条持续向左流动电弧，保留橙色危险边界与护盾操作提示。只改变显示，不改伤害、碰撞范围、地形或存档，不增加机关数量。真实GPU发现并修复极端缩放绘制卡死、横向拉宽后的波纹反向混叠；最终GPU与正式保存场景双语检查通过，独立限定复审无剩余阻断。

最终自动标题→盆地→原伤害死亡→同角色营地复活录像通过，原3件残片保留；[完整原件和短片入口](E:/OurGame/Knight/过程图稿与视频/2026-10-14-电磁线圈发射器-v003/README.md)。第一段录像因窗口失焦后模拟L被撤回而失败，原因只定位至焦点事件，失败原片另存不覆盖。Root实际画面审阅认为线圈来源可辨，但橙框仍显眼、场景/敌人仍偏原型，整体NEEDS_WORK；固定60引擎键事件不是实体键盘、正常速度性能或整关验收。完整目标继续ACTIVE，不恢复自动化、不付费、不发布、不改真实存档。[本轮事实、反例和下一步](.superpowers/sdd/2026-10-14-coil-emitter/progress.md)。以下保留历史时点。

最新续推（护盾通道操作引导）：在盆地持续电磁场真正可见且靠近角色时，提示现在明确说明「电流从右侧来→朝右按住L举盾→离开后松L散热」，兼容中英文及实时改键；不会提前泄露墙后／未挂载区域，不改变原目标、教学存档、伤害或地图。还修复了一个实际画面问题：提示字符串已更新，但逐帧刷新互相覆盖，屏幕一直显示旧机械爪提示。新增真实字形像素回归先失败再修复，最终实景可见，暂停、隐藏、遮挡、卸载与重新挂载均有专项覆盖。

最终 Vulkan 自动标题→盆地→真实混合伤害死亡→同角色营地恢复录像通过；最新 GPU 方向层、保存场景引导及独立串行无窗口自然死亡回归均严格通过。原件／失败版本／中英图／4.83秒展示片已[分版本保存](E:/OurGame/Knight/过程图稿与视频/2026-10-14-护盾通道引导-v004/README.md)。固定60引擎键事件不是实体键盘、正常性能或连续视听验收；Root已审看原尺寸截图及六个原片采样，整体仍NEEDS_WORK。较早GPU退出资源告警及通用UI186-Shape失败原因仍OPEN，不能被本次安静通过覆盖。完整目标继续ACTIVE，无付费、自动化、发布、Git修改或真实存档修改。[本轮事实／反例／审查／下一步](.superpowers/sdd/2026-10-14-shield-lane-guidance/progress.md)。以下保留历史时点。

最新续推（电磁通道可读性）：原持续伤害区已加入右侧来源标记、两排朝左箭头和向左流动光点，让危险方向不再只有橙框表达；原伤害／盾耗能／地形／存档规则不变，没有虚构安全倒计时。窄缩放来源错位已真实GPU先失败再修复，补了镜像、两排、光点方向、暂停和墙体遮挡检查。正式标题→盆地→5次实际混合伤害→死亡→同角色营地恢复／继续移动的Vulkan录像通过并[按版本归档](E:/OurGame/Knight/过程图稿与视频/2026-10-14-电磁通道方向-v002/README.md)，固定60自动输入、32.833秒，不是真人或正常性能证据。

边界：早一次录像因模拟D状态撤回在伤害段前失败，原片完整保留；加了已有被动按键尾部的保存后下一次未复现，原因尚未查清。最后同版GPU回归的全部画面断言通过，但退出有1Area＋1Space＋2Shape RID及ObjectDB1告警，**严格结果仍FAIL**，不以更早安静通过覆盖它。独立代码／原始证据复核已完成，保存场景控制器及录像退出安静；机关仍偏功能标记、环境及提示仍待打磨。完整目标保持ACTIVE，真实整关键盘／视听性能／保存与门禁全矩阵／丰富度仍OPEN。无付费、自动化、发布或真实存档修改。[事实、失败、测试和下一步](.superpowers/sdd/2026-10-14-lane-direction/progress.md)。以下保留先前时点记录。

最新续推（首次检查点前连续死亡）：已从空隔离存档的真实标题开始，正常走跳到第一只原有敌人面前，让其每轮实际攻击5次直至死亡，连续验证两轮。无窗口与实际GL窗口均通过：两次均回到起点 `(160,600)`，检查点仍未激活，同一角色满血站稳、处于镜头内且能继续用D移动；入口残片在内存和磁盘始终只有1件，没有丢失或重复计数。新assembly上的盆地冷Continue邻接检查也通过。

本次没有复现这条路径上的“死亡跳到后面节点”，所以只新增验证、不冒称新修复；未改玩法或美术。两轮死亡采用引擎键事件／固定60，不是实体键盘或流畅度证明。审查要求的固定5点血断言与初始化失败清理已补齐，最终双模式重跑及独立代码／证据复核通过。其他恢复与保存失败边界、真实键盘整关、连续视听性能、丰富度和美化仍待完成，目标保持ACTIVE。无新媒体／付费／自动化／发布。[本轮证据与剩余边界](.superpowers/sdd/2026-10-14-early-death/progress.md)。以下保留先前时点记录。

最新续推（重启后Continue）：已用上一轮实际GL游玩产生的隔离存档原字节副本，在全新进程从标题按“下→回车”继续。无窗口与实际GL窗口均验证回到盆地 `(1680,3960)`、原3件残片与非空探索记忆完整保留、仍缺3件才满足数量要求；同一角色处于镜头内并站稳地面，可重新用D移动，整个加载/游玩窗口无额外死亡恢复，源存档和真实用户数据不变。原自然死亡路线在新测试assembly上也回归通过。

本次只补验证与修正测试工具的重复按键/起点假设，没有改玩法、美术或宣称整关验收。独立代码复核指出的提前恢复漏检和相机覆盖已补齐；失败案保留。目标继续ACTIVE，首次检查点前死亡、保存失败/全量门禁、真实键盘整路线、连续视听性能及场景美化仍待推进。无新媒体/付费/自动化/发布。[本轮事实、运行记录和边界](.superpowers/sdd/2026-10-14-cold-continue/progress.md)。以下保留先前时点记录。

最新续推（真实伤害死亡与恢复）：已在隔离的正式关卡中从标题沿原路到盆地，亲自触碰并落盘检查点；实际受伤5次（4次原电磁通道、1次附近敌人），通道最后一击使HP归零，死亡／恢复请求／完成各1次。同一角色满血回到 `(1680,3960)` 并重新用D移动；原3件残片的内存与磁盘ID均保留。没有传送、扣血注入、存档播种或玩法改动。实际GL窗口与最终无窗口专项通过，但都是固定60引擎键事件，不是实体键盘。三张过程图已[归档并审阅](E:/OurGame/Knight/过程图稿与视频/2026-10-14-自然死亡复活-v001/README.md)。机关橙框的危险表达、重复场景、小地图黑块仍待改善。

本轮补的是验证工具和运行证据，不宣称修好了未复现的游戏bug或完成美术。首个检查点前死亡、冷Continue、其他复活边界、保存失败与全量门禁、真实键盘和正常视听性能仍OPEN；完整目标继续ACTIVE。旧UI186-RID strict FAIL仍保留。已修正工具将fixed60误标为false的元数据，旧原始记录未改。[本轮证据、审查及下一步](.superpowers/sdd/2026-10-14-natural-death/progress.md)。以下均为先前时点记录。

最新续推（攀升操作引导）：已在真实完整支撑的第一座攀升平台加入“到右侧→普通起跳→右上瞄准→机械爪自动牵引”的双语/实时按键提示；不提前标记下一平台。本局真正站稳上层平台后不再重复，关闭引导期间也记住成功。暂停时撤卡、恢复后按当前状态恢复，并保留原首战提示的距离记忆。局部实际窗口验证了文本排版、改键、窄柱/边缘/空中区分、新会话重置，以及真实菜单返回合法初始检查点后再进入仍保留学习记录；后者不是自然死亡。

同版最终 Main / Explore 再次通过标题→同一角色→Boss→有支撑出口，恢复均0次；通关后持续D60帧仍保持位置、一次结算和隔离存档不变。原运动四文件及C526路线驱动未改。最终GL与模型专项通过，图片已[保存并原尺寸审阅](E:/OurGame/Knight/过程图稿与视频/2026-10-13-攀升操作引导-v002/README.md)，场景与小地图仍NEEDS_WORK。通用UI聚合虽功能断言通过，但退出有186个GodotShape2D RID泄漏，严格状态仍FAIL；旧仅加载场景案例也存在同错误，不归因于新引导。自然死亡/Continue/保存失败、真实键盘、正常视听性能和整体美化仍未闭合，完整目标保持ACTIVE。无付费、自动化、Git提交/发布或真实存档修改。[本次详细证据与限制](.superpowers/sdd/2026-10-13-stable-claw/ascent-guidance-continuation.md)。以下保留先前阶段记录。

事实（本轮机械爪修复）：已修正张紧悬挂后约44像素下坠再回拉、抓点丢失后突然恢复隐藏下落速度、已抓稳沿同一天花板移动时误脱钩三项问题。原停靠8.01像素断言未放宽；120实际物理帧观察均在包络内，但仍有小幅摆动，不能称永久静止。独立障碍、原目标异面，以及先贴顶再撞侧柱的固定场景均正常解除且无穿透；跳跃、冲刺、释放、输入边界和原摔伤专项通过。

**新修复版（Controller6083／MotorEF20）Main与Explore已分别从标题、同一角色完成Boss→有支撑出口，恢复0次。** Main记录8次遭遇激活／8次击败；Explore记录11次激活／10次击败，不称清空全部敌人。两案通关后持续D60帧，角色位置、一次结算和隔离存档保持稳定。正式关卡路线测试C526未改；这些是新版本实际运行结果，不借用修复前C526/535的通过记录。未改地图、怪物难度或生产存档规则。

判断与风险：本轮解决的是具体运动缺陷及自动通关回归，不是整关交付。上述完整路线为隔离存储、固定60、无窗口的引擎键盘事件；正常窗口／实体键盘、自然死亡与Continue／保存失败、完整视听性能及美术丰富度仍未验收。“同一物理帧顶棚与侧墙双接触，首预测命中是否遮蔽第二接触”仍未复现／未覆盖，保留审查风险，不因顺序接触通过而关闭。下一阶段继续正常运动表现、操作引导和视听优化，并补齐这些边界。完整目标ACTIVE、未缩减；无新媒体、付费、自动化、提交、发布或真实用户存档修改。[本轮证据、独立审查及待办](.superpowers/sdd/2026-10-13-stable-claw/floor-brake-continuation.md)。以下保留历史时点，不替代当前状态。

最新全关衔接（10月13日，06:46）：**当前同版固定60无窗口Main再次通过标题→同一角色→8场遭遇→Boss→有支撑出口，恢复0次**，持续D60帧的完成次数／位置／隔离存档均稳定。**Explore的下层Bell完整往返及蒸汽支路已实际通过**：原缺口自然下落及入口复位117/170物理帧、HP1不变；原阀门、战斗、分流、8级台阶、LiftBackLanding及Grip1均通过。**完整Explore仍FAIL于Grip1→Grip2转移**：新被动诊断13条无遗漏／异常，正确目标cast17进入Reel后发布Released/Obstructed；随后真实滑动接触为旧Grip1抓取柱下沿，法线(0,1)，血量／同玩家／跳跃27／恢复0保持。已明确错误边界，尚未修复；不把随后接触冒作先前预测查询的直接结果，也不删碰撞或放宽要求。Main走配重升降台直达Grip2，不能替探索路径验收。Root和独立审计核对同版0F8EB5／49A两案1599源码／109真实用户清单／7工具、内层清单及六二进制一致。正常窗口、实体键盘、自然死亡回收、连续视听和性能仍未验收。全部证据及失败保留在[推进记录](.superpowers/sdd/2026-10-13-stable-claw/full-route-integration-continuation.md)。完整目标ACTIVE；本次仅测试走法／诊断变更，无生产玩法、场景、美术或真实存档修改，无付费、自动化、Git提交或发布。

小地图局部修订（10月13日）：已按用户批准改为柔和椭圆外沿；中央未知黑暗、已探索地形与已发现检查点权限不变。原矩形黑角、椭圆外图标及1.5倍输出下标记跨羽化边界均真实GPU先失败后修复；最终HUD、正式接入、13项生命周期、原HUD／引导回归及七张GL实景采集通过，五案源码／原档清单／辅助工具与六件二进制核对无差。独立代码审查无阻断；纯父变换动态变化而逻辑尺寸／角色／模型均不变的重绘边界留后续验证，不声称任意变换全覆盖。已[归档并审看原尺寸实景](E:/OurGame/Knight/过程图稿与视频/2026-10-13-探索小地图-v008-柔和椭圆边缘/视觉审阅-root-v001.md)：局部外沿改善、整体仍NEEDS_WORK；1327索引项413,966,256字节校验0差，含中间失败、源码及诊断，无新视频，不等于整关美术、真人或性能验收。[本轮证据](.superpowers/sdd/2026-10-13-minimap-ellipse/progress.md)。

最新盆地时序复核（10月13日）：确认并修正的是**测试驱动在批量物理帧下晚松D**，不是已经解释了历史游戏过冲。受控12fps绘制/60Hz物理先失败（晚2帧）再通过（同帧松键）；当前正常时间GL局部路线完成登台→自动拉升→抵达→有支撑落台，六组邻接回归通过。历史过冲的坐标与机关时钟仍不能由该缺陷单独解释，保持OPEN。独立限定审查无阻断项，三项测试工具小问题留档。六张原尺寸图与四案证据已[归档](E:/OurGame/Knight/过程图稿与视频/2026-10-13-盆地区正常时序-v001/README.md)，主代理全核171项/9,580,411字节零差异并审看六图；[视觉仍NEEDS_WORK](E:/OurGame/Knight/过程图稿与视频/2026-10-13-盆地区正常时序-v001/视觉审阅-root-v001.md)，无新录像。全关实体键盘、Main/Explore、自然死亡/回收及视觉性能验收仍未闭合，完整目标ACTIVE。[事实与审查边界](.superpowers/sdd/2026-10-13-stable-claw/normal-boarding-continuation.md)。

本轮迷雾续推（02:39；局部验证通过，整体仍进行中）：将遮挡阴影的可见侧硬边改成短距离柔和渐变；墙后原有黑暗及CPU探索／碰撞／存档规则不变。真实GPU先RED后GREEN，原11项及新阴影／变换／灯锥检查通过。初版超出原5%开销门槛，保留FAIL后优化；新同进程旧／新／旧入口静止对照p95为18.7736／17.4409／18.2931ms，独立CSV和版本审计吻合，仅代表这次短时门槛，不是整关FPS提升。**正常时间盆地登台实际过冲失败，原因尚未确认、未修复**；其后735帧固定60局部录像通过，不替代该失败或真人验收。原件、失败、源码与展示副本已完成[迷雾阴影柔化v001归档](E:/OurGame/Knight/过程图稿与视频/2026-10-13-迷雾阴影柔化-v001/README.md)：840项、68,442,592字节经主代理独立哈希复核无差异，八张关键帧已原尺寸查看，[视觉仍NEEDS_WORK](E:/OurGame/Knight/过程图稿与视频/2026-10-13-迷雾阴影柔化-v001/视觉审阅-root-v001.md)。全关物理键盘、自然路线、正常性能／连续视听、丰富度和美化仍OPEN。目标ACTIVE，无付费、自动化、发布或真实存档修改。[本轮详细证据与边界](.superpowers/sdd/2026-10-13-stable-claw/fog-edge-continuation.md)。下方保留历史时点，不将旧PASS冒作当前完整验收。

最新推进（10月13日）：全关机械爪已修正自动拉升、抵达及目标丢失时旧速度回灌；盆地区左／中／右登台均真实抵达锚点并稳定落到横廊。漏乘的24px夹缝改为48px后，可自然落入原通路再两次普通跳跃返回；相关局部物理／机关／残片／地图回归通过。GL六阶段画面、完整734帧Vulkan局部录像已[归档并抽帧审阅](E:/OurGame/Knight/过程图稿与视频/2026-10-13-盆地区配重转移-v001/视觉审阅-root-v001.md)，整体视觉仍NEEDS_WORK：硬边雾、小地图黑块、原型场景与错阶段fixture提示待修。中英文帮助已明确“抵达后A/D摆荡，SPACE途中也能跃出”；既有UI排版断言通过，但独立UI进程退出Shape2D资源诊断尚未闭合，不能称整项通过。**全关真实键盘、正常速度／整体视听与丰富度仍OPEN，不宣布整关完成**。固定60录像非实时性能／真人证据；历史失败全部保留。无付费／自动化／发布／真实存档修改，整体目标ACTIVE。下方均为历史时点，不替代此段。[当前实施记录](.superpowers/sdd/2026-10-13-stable-claw/progress.md)。

本轮UI补验（02:04）：菜单帮助单独运行正常；盆地旧样例通过实际W/K抓取进入Reel，发现英文正文越出面板，缩短措辞后中英文真实卡片的可见范围／文字非重叠及原战斗／槽位／护盾检查通过，干净退出。该样例为V1，不能替代当前V2配重路线。冷进程load-only仍复现186个Shape2D退出诊断，已隔离到关卡资源加载图，具体持有者未证实；不改全局GC或放宽验证。详见同一实施记录。

最新隔离键盘诊断：正式场景从Title进入Playing，同一角色、独立保存路径；唯一两分钟WindowsGL观察完成，5077条记录无丢失，1594源／109行104真实用户文件／六二进制守卫不变。**没有通过玩法键盘验收**：桌面工具的J/K/L/Q逻辑事件全带physical Pause，攻击／盾／抓取没有触发；Esc尝试在窗口已结束之后，不算暂停验证。暂不改物理绑定迎合工具，也不重跑择优；此结果不能代替真人、完整路线、视觉或性能验收。新测试工具的清理／启动前二进制核验问题已修，编译失败和所有限定证据保留；无产品玩法／地图更改或新媒体。配重转移仍未交付，共同机械爪稳定自动牵引设计已再次明确询问，未当作批准。整体目标ACTIVE。[本轮事实、推理与风险](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-isolated-keyboard-probe/result-v001.md)。

最新整路线复验（正常时间Main／Explore各一次）：新版有界外层通过真实进程RED→GREEN并经限定复核，但两条当前路线均在入口后的按键撤回／失焦处提前FAIL，未到Boss或出口，不借旧版PASS。完整250条事件尾迹及同引擎提交源码支持“应用失焦时先清按键、窗口焦点稍后变化”的可能机制，**尚未定位触发者，也不是已证明的玩法松键问题**；不补按、不强制前台、不重跑择优。独立核验两案1593源／109行104真实用户文件／六二进制／11工具与52冻结副本一致，真实存档未改；失败分支fresh-six自动核验缺口由本次独立比较补证，未来缺口仍保留。下一步为隔离保存路径的实际键盘／被动焦点验证；配重升降→横廊转移、完整视听／丰富度与全目标验收仍OPEN。目标ACTIVE，无新媒体、付费、自动化、提交或发布。[本轮结果、完整FAIL与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-current-route-revalidation/result-v001.md)。

最新限定优化（机械爪空闲目标搜索）：保留键盘预览频率、全部33条射线与原目标／遮挡规则，只减少节点遍历和同一次查询的临时创建。实际正式起点64次查询分配16,929,072→3,932,952B（减少76.7681%），但**仍超524,288B原预算，局部分配门未通过**；中间失败与编译错误完整保留，不追认GREEN。原抓取／输入／释放及新增双角色、同帧重挂载、释放、内部子节点、鼠标与绳索遮挡专项通过。唯一同生产版正常WindowsGL观察2163端点完整，完整画面on分配较上一版观察少45.5454%、p99约30.6ms；只是每版一次静止场景描述，不是配对因果FPS或整关流畅验收。1593源／109行104用户文件／六二进制与工具守卫不变。独立限定源码复核无功能阻断项，预算未达与动态地形射线排除专项覆盖缺口保留；配重升降→横廊转移、整体路线／视听及旧窗口／性能缺口仍OPEN，目标ACTIVE。不改操作手感、真实存档，不恢复自动化，不付费或发布，无新媒体。[本轮事实、失败与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-claw-query-allocation/progress.md)。

最新限定优化（压力门后台遍历）：真实具名RED后，仅替换全树防夹检测的临时容器／子节点遍历，不缩小检查范围或改变碰撞规则。正式起点64次闭门回调分配6,968,320→24,576B（减少99.6473%，非整关提升）；原模型／防夹／暂停／重置及新增同帧生成／换父节点、精确血量和即时scratch清理通过。旧整场景fixture传送扣血在原生产门也复现，仅修测试传送后的运动／坠落起点，保留正常摔伤和原不扣血断言；新版实际重新挂载通过。唯一新版正常WindowsGL采集2163有效端点，完整内容on分配较上一版观察少19.8585%，**p99仍约37ms、没有下降，不称流畅或整关完成**。最终1593源／109行104用户文件／六二进制守卫不变，失败与旧窗口／性能缺口保留；Basin转移仍未完成，新共同机械爪设计仍待确认。无新媒体、付费、自动化、提交、发布或真实档修改；目标ACTIVE。[本轮事实与证据](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-pressure-gate-allocation/progress.md)。

最新限定排错（绘制采样中断）：已用具名RED→GREEN修正测试诊断的“收到失焦事件却继续等绘制”问题，补充有界窗口／回调时间线；不改生产玩法或真实存档。原采样器控制测试通过；本版唯一正常Windows GL静止采集取得2163有效端点，焦点变化0、恢复及音频清理完成，补到当前完整内容on观察。**仍未称流畅或查明根因**：p99约35ms／最大约50ms；独立最小化负控55秒超时，只有START、没有最终trace，完整FAIL保留，不证明最小化请求已执行，不延长／重跑择优；旧失败配对性能、窗口停顿、整体路线／美术及Basin转移门仍OPEN。新诊断有额外观察开销，不能与旧采集比较为FPS改善。独立源码／原始数据复核已收到：2163端点／29心跳和重算数据吻合，最终1593运行资源／109行104真实用户文件／六二进制／十工具与构建溯源守卫不变；快照异常隔离留Minor，不把负控缺口降级。目标ACTIVE，无新媒体、付费、自动化、提交或发布。[事实、证据与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-draw-interruption/progress.md)。

最新限定优化（萤火虫可达路线查询）：真实具名RED后，仅去掉最近路标筛选中重复的路线计算；同一正式起点64次刷新分配3,587,584→1,177,624B（减少67.1750%，仍约18.4KB/次），原支撑点／模型／攻击预警／暂停／生命周期／layout1及新增10＋13路标与门锁结果断言通过。独立原始数据审计吻合；**不称整关变流畅**：改前正常Windows GL取得2163有效端点，改后正常测量只保留454个off端点，完整内容on臂未开始，出现一次焦点变化后110秒内部超时，原因尚未证明；完整FAIL／partial保留，不延长预算或重跑择优，正常性能门仍OPEN。11次构建／运行前后源、工具、真实用户109行／104文件守卫不变，三张Basin相关产品原字节不变；无新美术、媒体、付费、自动化、提交或发布。整体目标ACTIVE，Basin配重转移仍未完成，新机械爪设计尚待确认。[本轮事实、证据与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-firefly-reachability/progress.md)。

最新限定排错（原路线／Fog验证）：恢复地图后证实原Ascent失败来自测试驾驶固定向左、入站实际已在目标左侧，现仅按实际侧选择方向；原双向乘台、完整站稳、三个起跳偏移及相邻生命周期／Steam通过。Fog原超时定位为GPU像素测试被headless启动，新局部图形launcher保持55秒上限；headless／错误实际renderer两负控明确拒绝，NVIDIA实际Compatibility像素专项通过。最终四案同源同二进制，1593来源仅两tests改变，109行／104用户文件与三地图原字节未变；不改生产玩法、像素阈值或存档。整体仍ACTIVE，原新Basin三个转移失败不因测试修正关闭；完整运行／性能／视觉和新牵引设计仍待完成／确认。见[本轮证据与范围](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-route-regression-diagnosis/progress.md)。以下保留此前时点，不将历史OPEN状态当本轮结论。

当前局部改造检查点：用户已批准盆地区“机械爪启动可乘坐配重升降台 → 乘台上升 → 抓向横廊”。候选的乘台、失手回返和存档专项曾通过，但三个转移几何均真实失败，不能交付；最后抓环候选正确锁定却离台不足、碰台脱爪。失败候选／新测试／完整日志已保留，本轮builder及两份生成场景已精确撤回到开始前的dirty bytes，重新构建0警告／0错误，未撤其他修改或写用户存档。此恢复只证明文件与构建一致，不是运行验收。原Ascent回归失败、Fog超时及完整新路线／视觉仍OPEN。下一步建议稳定自动牵引→抵达后保留摆荡；涉及共同机械爪行为的新设计，待用户确认，尚未修改玩家物理。整体目标仍ACTIVE，序章未完成。见[实施记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-basin-counterweight/progress.md)、[撤回结果](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-basin-counterweight/rollback-result.md)及[候选归档](E:/OurGame/Knight/过程图稿与视频/2026-10-12-盆地区配重升降-v001/README.md)。

当前小地图可读性修订（Task9；限定完成，整体未完成）：扩大外沿渐隐、增强角色点／方向线；GPU最小UI亮芯26像素（旧15，固定20门）。渐隐参数仅尺寸变化更新，五个检查点越界负控／正控通过，独立限定复核C0/I0/M0。真实逐次反例768→576B只验证移除两项重复包装，未归因尖峰和六个既有moving调用仍保留，不是零分配或FPS结论。九个同版安全阶段通过；Root核10案source1591／用户109／binary6守卫0差、24冻结＋8当前身份一致。[v007原件、完整6秒受控短片及审阅](E:/OurGame/Knight/过程图稿与视频/2026-10-12-探索小地图-v007-移动开销修正/README.md)已保存，全核1183目标／1142来源副本0差，原尺寸查看GL03／04／07及七视频帧；旧v006和失败／无效测量不覆盖。标记可辨，但黑暗占幅、重复阶梯及原型美术仍NEEDS_WORK。下一步优先UI协调与场景丰富度；具体Basin配重升降重排仍PROPOSED。未改玩法／地形／默认存档，无付费／自动化／发布。完整序章、正常速度、真人与最终美术验收仍OPEN；目标工具显示blocked，已告知用户需面板恢复。[限定复核和边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-9-review-result-v002.md)。

当前可看：[24秒小地图／机械爪诊断预览](E:/OurGame/Knight/过程图稿与视频/2026-10-12-探索小地图-v005-新版路线诊断/presentation/preview-052-076-v001/preview-052-076-original60fps-1280x720.mp4)。原片、展示副本、19张原尺寸采样帧、版本／来源／状态索引均已留存；Root采样审阅仍NEEDS_WORK，不代表连续动作、听感或实时流畅验收。下一轮优先小地图可读性／黑块感、UI协调及减少重复阶梯；具体配重升降重排仍待独立设计答复。

最新核验（新版Main＋Explore完成；整体仍进行中）：当前Task8候选按原正常时间Windows GL键盘路线各一次完成标题→Boss→有支撑出口／D60／结算恰一次，1591运行资源／109用户数据／六二进制均与审后冻结版一致。Main实际收集6件，Explore7件并完成双向捷径和神殿上层收集返回；两案死亡恢复均0/0，不冒称覆盖死亡。正常callback仍有长帧（p95约50.5／45.0ms，含测试诊断开销、非渲染FPS），**不称已经流畅或整体美术达标**。新同版完整录像与七张原尺寸帧已[归档v005](E:/OurGame/Knight/过程图稿与视频/2026-10-12-探索小地图-v005-新版路线诊断/README.md)，Root抽帧确认地图与角色可辨，但黑色块感、过小标记和原型场景仍NEEDS_WORK；固定60录像不是实时性能，且外层850秒保护INVALID，不追认完整录制合规。配重升降转移仅为[待确认局部方案](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/basin-counterweight-proposal-v001.md)，未实施。完整目标ACTIVE，无付费／自动化／发布／真实存档写入。[双路线报告](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/current-route-verification-v001-report.md)、[Root核验与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/current-route-root-audit-v001.md)、[原尺寸画面审阅](E:/OurGame/Knight/过程图稿与视频/2026-10-12-探索小地图-v005-新版路线诊断/视觉审阅-root-v001.md)。

最新推进（Task8限定完成，完整目标继续）：真实反例后只修改萤火虫支撑点搜索，正式起点该引导查询分配减少65.9183%（仍约56KB/次），原11＋新16断言／两个隔离邻项通过，独立限定Spec／quality复核C0/I0/M0 Approved。新版首次原strict正常时间比较也通过**静止小地图额外开销门**：off/on/recovered p95为22.9314/23.3502/21.8877ms，Root重算2163端点与109用户／1591运行资源／6二进制守卫无差异。**这不是稳定60FPS或整关流畅／精致验收；约23ms高分位帧间隔、整体美术和场景丰富度仍需推进。** 原v003失败及旧版路线证据保留，不倒绑新版，不同源借PASS或重跑择优。目标ACTIVE，无新付费／自动化／发布／真实档写入。见[本轮优化审计](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-8-root-audit-v001.md)、[新版有限性能证据与风险](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/post-task-8-normal-validation-result-v001.md)。

当前动作（Task7诊断已交接，完整目标继续）：首正常案因钩子顺序无效完整保留；具名反例修正后、经明确授权的一次独立验证取得2163个有效端点。四新诊断文件独立Spec／quality复核C0/I0/M0，原1587运行源／资源未变，109行用户数据保持。数据支持继续调查重复managed分配／GC与长帧共现，不证明暂停根因或游戏已修复；多数保护区树遍历早已优化，不拿旧热点冒充新发现。新诊断assembly与下方v003候选分开，原性能FAIL／INVALID仍OPEN，无第三采集／预算择优／付费／发布。[本轮事实与风险](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-7-root-raw-audit-v001.md)、[独立限定复核](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-7-review-result.md)。

最新推进（10月12日；无边框小地图v003功能候选）：正式连续场景已接入右上角局部探索地图，未知黑暗、已探索保留，仅显示角色和已发现检查点；死亡／Continue／暂停隐藏及大位移标记专项通过。同版正常主线、探索路线均完成标题→Boss→有支撑出口，原键盘＋独立冷Q、收集＋独立冷读、正式输入、头灯迷雾和Boss准入六项／八实际引擎严格回归通过，真实用户109项未变。完整源码独立复核C0/I0/M1，唯一非阻断地图拒绝诊断缺失留后续窄修。**尚非验收完成：最终正常时间性能对照因恢复基线漂移而INVALID／FAIL，性能验收OPEN；整体美术、丰富度和连续视听／真人体验仍未完成。** 已保存并抽帧审阅[最新版过程画面v004](E:/OurGame/Knight/过程图稿与视频/2026-10-12-探索小地图-v004-层级兼容修正候选/README.md)，固定帧率受控演示不能证明实机流畅。完整目标ACTIVE，无付费／自动化／提交／发布／真实用户存档写入。[事实、判断及风险](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-6-root-final-acceptance-v003.md)。以下时点保留历史，不替代最新状态。

最新推进（10月12日；小地图集成复核）：24文件candidate-v002完成正常Main和Explore的标题→Boss→有支撑出口；独立重算首次局部2000样本与正常三臂样本符合限定预算，不能推论整个游戏流畅。独立源码审查发现builder对未来Control／plain Node地形祖先未拒绝的I1，当前130块目录未受影响，正在用保存重载负例做最小修正；新版本将按原门槛复验后再授予交付。已保存并审阅[探索小地图v003过程画面](E:/OurGame/Knight/过程图稿与视频/2026-10-12-探索小地图-v003-大位移修正候选/README.md)：大位移标记恢复、暂停隐藏，整体场景／美术仍NEEDS_WORK。原历史native错误未归因、最终相邻回归与完整特性复核仍开放，完整目标ACTIVE；不付费／自动化／提交／发布／写真实用户存档。

最新推进（10月12日10:22；小地图最终候选验证中）：22文件候选已冻结；正式目录130块／10488边界、保存重载、生命周期、九组显示布局与缓存四边负控、原HUD／引导专项通过。最终同版正常Main已完成Boss与有支撑出口，Explore仍在运行；首次优化后局部性能与严格正常时间三臂即将采集，未先行称通过。两版原件／展示副本及来源索引保存在[探索小地图v002最终候选](E:/OurGame/Knight/过程图稿与视频/2026-10-12-探索小地图-v002-最终候选/README.md)，v001中途与失败案不覆盖。原尺寸中途画面可见探索／旧路保留／暂停隐藏，但整体美术仍NEEDS_WORK；最终视觉、相邻回归及独立审阅待完成。完整目标ACTIVE，无付费／自动化／提交／发布／真实用户存档改动。

最新推进（10月12日09:50；小地图正式目录接入与性能修正中）：正式保存场景已生成130块结构地形／10488段边界，目录重载、动态门与移动体排除、失败暂存不覆盖旧场景等专项通过。实际目录的局部性能首次测量未达预算：移动p95 .5789ms／p99 1.1021ms（要求≤.5／1），原2000样本保留；正在做不改变候选顺序的初始建堆优化。正常Main整路线运行中，双路线、正常三臂性能、独立最终复核及归档短片仍未完成，不从专项或编译宣布交付。历史native错误未归因，完整目标ACTIVE；没有付费、自动化、提交、发布或正式用户存档改动。[实施判断与证据](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/progress.md)。

最新推进（10月12日09:20；显示层独立通过、准备正式接入）：Task5 v002独立复核Spec符合／质量Approved、C0/I0/M1；唯一Minor是缓存边缘专门渲染负控缺口，明确移交正式接入阶段补证。根代理另核formalinput／原fog与压力门／Boss admission三案main和wrapper均0，801源／6binary与冻结版完全配对，109项真实用户数据不变。开始同源结构地形目录与保存后重实例化绑定；正式关卡回返、正常双路线、性能和归档录像尚未通过。历史native错误未归因，不称已修复；完整目标ACTIVE，无付费／自动化／提交／发布。[独立审阅与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-5-review-result.md)。

最新推进（10月12日09:04；小地图显示层冻结待复核）：v002的真实GPU九组布局／整输出像素遮挡、模型17／几何10、生命周期13、存档真冷读、完整HUD＋三引导回归均干净通过，13范围文件与六二进制已冻结，独立审阅进行中。根代理同版完整键盘＋冷Q、残片＋冷读也通过；此前原生释放／加载错误的根因仍未解释，不称已修复。正式地形目录、完整关卡显示／回返和正常性能／录像尚未验收，因此暂不作为可玩小地图交付。无新付费素材／自动化／提交／发布，完整目标ACTIVE。[实施报告与失败留存](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-5-report.md)。

最新推进（10月12日08:45；小地图显示层验证中）：真实GPU夹具已通过九组实际设置布局、最小尺寸整像素黑暗遮挡、动态引导避让和暂停隐藏；闲置120次显示更新测得托管分配0。尚未冻结／独立批准，正式地形目录和正常关卡画面仍待接入，不能称为已交付的小地图。完整HUD回归停在旧提示文案预期不一致，失败保留；此前键盘退出FATAL及残片加载ERROR仍未闭合。用户数据保持隔离，目标ACTIVE，无新付费／自动化／提交／发布。[当前实施与证据边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/progress.md)。

最新推进（10月12日08:08；显示层继续、发布门槛保留）：Task4退出误报修正已独立复核C0/I0/M0；13生命周期／save真冷读／formalinput通过。现存完整键盘退出FATAL／coldQ缺口及残片首加载ERROR仍为发布阻断，固定一次cold加载pair和一次DriverOnly clean都不能关闭或证明根因。按已审API继续Task5无边框显示层，不将Task4标complete，保留精确冻结版本和所有FAIL，最终仍须原严格回归与正常运行／媒体／性能通过。目前尚无可见小地图，无新美术／付费／自动化／提交／发布，完整目标ACTIVE。[执行判断和未闭合边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-4-root-acceptance.md)。

最新推进（10月12日07:51；生命周期修复冻结、整体回归未通过）：退出误报成功已真实反例复现并实施最小私有结果helper；13组生命周期、存档独立冷读与正式输入专项通过，限定复核进行中。但新版键盘回归在OK后出现原生对象释放FATAL（actual−1073741795／wrapper1，冷Q未启动），连同此前首资源加载ERROR均保留调查，不宣称全绿或根因已修。根代理核四案同798源／6binary、用户109项前后不变；Task4未complete，HUD／正式目录未派发，无可见地图。本轮没有新美术／付费／自动化／提交／发布，完整目标ACTIVE。[根代理验收与未闭合边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-4-root-acceptance.md)。

最新推进（10月12日07:40；小地图生命周期审后修复）：初版12组生命周期及可选存档冷读、模型／遮挡、键盘冷Q、原迷雾专项通过，但独立审阅发现退出保存的两次准入之间可能误报成功，Task4尚未批准，已交原实施者最小修正与反例验证。更广残片回归又有首资源加载GCHandle ERROR（engine0、严格wrapper1、冷读未启动），原失败留存并调查，不借通过标记或旧版通过关闭。真实用户109项守卫保持；HUD与正式目录仍未接入，没有可展示的小地图画面。整体目标ACTIVE，无付费／自动化／提交／发布。[当前证据、修复与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/progress.md)。

最新推进（10月12日07:13；小地图可选存档完成）：Task3在真实合法核心丢图RED后实现兼容codec，52种可选行、6种旧迁移对照、实际替换拒绝保旧槽／清临时文件、不同进程冷读均通过；独立Spec／质量复核C0/I0/M0 Approved。根代理同源残片＋冷读、完整键盘＋冷Q也通过；与模型17／几何10五案796源／6binary对齐，109项真实用户数据未变。旧档没有地图则从空白探索开始，坏地图不改变合法残片与旧豁免。接下来接入死亡／新局／Continue／保存失败退出；HUD与正式目录仍未接入，没有可展示的小地图画面。完整目标ACTIVE，不付费／自动化／提交／发布。[存档实施与证据](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-3-report.md)。

最新推进（10月12日06:50；小地图探索底层完成）：Task2密集边界／同格移动排序缺陷已真实反例RED→修复→17组模型＋10组遮挡GREEN，原独立复核I1已解决、C0/I0/M0 Approved；同源完整键盘＋独立冷Q、原头灯／压力门均0，793源／6binary四案一致、用户109项保持。近格反例1stamp、准备受限样本8stamps、预热512批次0bytes，仅局部模型证据；新调度最大固定payload112MiB+29584另计，真实目录及连续移动延迟／正常性能仍留Task6。开始可选地图存档阶段，生命周期／HUD／正式目录尚未接入，无可展示小地图画面；整体目标ACTIVE，不付费／自动化／提交／发布。[本轮报告与证据](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-2-report.md)。

独立复核补充（10月12日；Task2修复中）：审阅发现I1：整桶边界链会让更近空格延迟显示，同一网格内移动也未重新排序，故Task2尚不符合已批近到远要求，未批准其完成。已交原实施者做具名反例RED→有界调度修复→回归→复核，不以有限重试或已有14组通过替代此项。可选存档／生命周期／HUD／正式生成仍未接入，没有可展示小地图画面；目标ACTIVE。[修复要求](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-2-findings-round-1.md)。

最新推进（10月12日06:24；小地图探索模型待独立复核）：模型已实际RED→GREEN，14组覆盖当前存活位置、全格／全段半径、加载并集缺口、传送不画中路、旧观察重判、重复stamp、静止开门重试及独立两位集；几何计数扩展10组通过。同源完整键盘＋独立冷Q、原头灯／压力门回归均0，792源／6binary跨四案一致、用户109项各前后相同。预热后模型闲置512样本／活动512个64候选批次均0 managed bytes，仅是纯模型局部测量，不是整关性能。极端百万边界同桶重试上界很高，实际目录延迟／p95留后续验证。Task2源码与收据已冻结交独立复核；可选存档codec、生命周期、HUD和正式builder尚未接入，无可展示小地图画面，完整目标ACTIVE。无新美术／付费／自动化／提交／发布。[本次模型报告](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/task-2-report.md)。

最新推进（10月12日05:55；小地图Task1底层）：整格凸包／整段对真实变换矩形的保守可见性已实际RED→GREEN，9组几何含中心射线可见但薄墙整格拒绝、角点射线漏检、旋转镜像、256／257容量；未改原头灯frame。新同源完整键盘＋独立冷Q与fog／原压力门均通过，788源／6binary／109用户项保持。独立实现复核Spec符合，Critical0／Important0／Minor0；开始有界探索模型。Catalog仅值类型／Resource骨架，探索模型、地图存档、HUD和正式场景尚未接入，不把底层测试称作小地图已显示。采用已审计划三项接口／遮罩勘误与缓存越界例外，完整目标ACTIVE。[小地图实施台账](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-borderless-exploration-minimap/progress.md)。

最新推进（10月12日05:38；自动反击储备）：真实能量模型先复现低能量J导致下一轮盾归零，再修测试驾驶Sentry反击准入与await后重判，不增强角色或改场景。完整键盘＋独立冷Q通过；同版正常Main／Explore各一次均完成标题→同player→Boss→有支撑出口，D60结算一次、恢复0/0、失焦0，755源／42场景／12冻结／6binary／109用户项保持。回调p95分别54.944／51.034ms，仍非流畅或真人验收；旧FAIL、编译FAIL与焦点根因保留。小地图开始按独立审阅计划推进，当前无可见实装，完整目标ACTIVE。[本窄片证据与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/sentry-counter-energy-progress-20261012.md)。

最新推进（10月12日05:14；当前关卡实体净空修正）：真实SteamValves劳工84px胶囊被80px通道卡住，480ticks移动0；仅把原上步道底部收薄12px、保持顶面4116，builder重生成后真实劳工可前进206.5px并完成预警→攻击→收招，原上层24ticks稳定支撑通过。键盘提示生产本来已为J/K，本轮修旧测试Q断言及失败场景清理，原4case、实际失败清理、实时改键、存储隔离、完整键盘＋冷Q均通过。同版正常Main完成Boss和有支撑出口但失焦1，仍FOCUS_LIMITED；**Explore实际FAIL**于Sentry第二轮防御要求，held且方向正确、但能量0／盾未生效，非过热，本次无失焦。缺终局文件是后果，不伪称双路线全绿或靠重试覆盖。两案源753／场景42／六二进制配对，原用户109项不变。旧六family及aggregate外沿隔离、耗能／焦点诊断、视觉与正常体验验收仍开放，小地图仅设计未实装，完整目标ACTIVE；无新媒体、付费、自动化、提交或发布。[本轮修改、失败与验证边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/current-basin-clearance-progress-20261012.md)。

最新推进（10月12日04:42；Temp隔离与真实失败诊断）：六类Temp实际工厂及pre-seed根已迁到validated外部owner，保存后重挂沿用同一Save／Settings／Recorder；14实例、6根、4项真实拒绝off-tree专项通过，旧核心8mounted＋12冷准入及完整键盘＋冷Q同版回归通过。真实生命周期诊断仍**FAIL**：Flow五case、暂停及sample补给原断言跑通，但旧地形／敌人节点／guidance glyph与失败清理尚不适配；两条原拒绝warning也未过滤。原用户109项保持，生产资源未改，十份scope源码和所有失败工件已留存。完整目标ACTIVE，旧aggregate仍不可全量跑；下一步修有效v2契约及失败清理，继续外沿隔离与体验验收。小地图未实装，无新美术。[证据、审阅和剩余边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/temp-fixture-storage-progress-20261012.md)。

最新推进（10月12日04:15；测试写入前隔离）：已修旧核心1默认＋5 user:// GUID挂载点，原创建流程保留；八实例安全off-tree实际RED→GREEN、实际mounted写入及12项冷进程准入合同通过，同版完整键盘＋独立正式Q也通过，原用户109项保持。正常与probe共用唯一入口校验；临时撤回该调用的安全负控被准确拒绝，probe即使错误接受也不进入pre/post，恢复后全项GREEN。只读复核无Critical／Important，Recorder仅路径绑定的限制保留。仅测试侧变化，无新玩法／美术，小地图未实装。**旧综合仍不能全量运行**：Temp helpers、独立IO和外层smoke／presentation等未闭合；未宣称流畅／真人或完整验收。目标保持ACTIVE。[本轮运行证据、审阅与剩余边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/fixture-storage-progress-20261012.md)。

最新推进（10月12日03:48；应用焦点取证与成对路线）：仅测试侧补齐application／window两类焦点观察，合成通知夹具实际RED→GREEN，完整键盘＋独立冷Q通过。当前同版正常Main／Explore均完成真实标题→同角色→Boss→有支撑出口，D60后结算仍一次，隔离存档分别6／7唯一残片；但失焦分别1／2次，仍FOCUS_LIMITED，不宣称原持键丢失已修复或流畅／真人验收。256条尾环已覆盖掉早先焦点行，缺行不能说明没发生。源码748／场景42／冻结12／用户109／六二进制配对保持；未改生产玩法或美术，小地图未实装。下一步先封闭旧综合测试的前置隔离，再扩展回归；完整目标ACTIVE。[本轮证据与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/keyboard-event-trace-progress-20261012.md)。

安全审计增量：旧核心pre/post实际挂载已确认1处共享生产默认槽、5处user:// GUID、12处局部外部隔离及6类Temp helper；加载阶段即可重写settings，关auto-start不能保护。旧aggregate本轮未运行，不能把事后hash当写前隔离。[可定位入口、排除误报与下一步清单](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/full-regression-storage-audit-20261012.md)。

最新推进（10月12日；输入来源诊断续片）：仅测试侧补记鼠标／动作事件／手柄按钮及raw和最近采样，首次legacy守卫前冻结；三种真实投递fixture、两项撤回钩子的断言RED负对照、恢复GREEN及键盘综合＋冷Q通过。新版正常Main完成同角色标题→Boss→有支撑出口，actual/wrapper0但失焦1，仍FOCUS_LIMITED；Explore则在physics376撤销Space持键而FAIL。新记录证明Space在374已经received／flush接受，376 raw键掩码全零但owned=18、当时focused=true，377才记录失焦，末尾最小化；这缩小了边界，不证明谁触发或原生清表根因。旧legacy主攻击失败本轮未复现，不能称已修复，也不能直接归因鼠标（同一动作还绑定手柄）。两案原样保留，源748／场景42／冻结12／用户109／六二进制跨案一致。未改生产玩法／小地图，不补按、不放宽守卫或预算；完整目标ACTIVE。[全部失败、回归、审阅与诊断边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/keyboard-event-trace-progress-20261012.md)。

新增方向（10月12日；小地图未实现）：用户已确认右上角无边框、跟随角色的局部探索小地图，并以“确认”批准[书面设计v001](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/docs/superpowers/specs/2026-10-12-borderless-exploration-minimap-design.md)。走过显露／未走过黑暗，保留旧路，只标角色和已发现检查点，不提前显示敌人、残片或秘密路线。[六任务实施计划](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/docs/superpowers/plans/2026-10-12-borderless-exploration-minimap.md)已完成主代理自检，待用户审阅（含缓存视窗越界更新的必要勘误）与执行方式选择；未开始小地图产品代码。意见台账v006的U-20261012-01已同步批准边界；原ACTIVE目标和既有用户存档保持。

最新推进（10月12日；查询开销最小修正）：真实动态fixture先RED→GREEN，保护区域64查询托管分配1761776→32256 bytes；正式场景Adapter同样64查询3201536→971264 bytes。生产仅复用固定player组名，不缓存成员或区域；独立只读复核Critical0／Important0／Minor1，后者是IsCandidateNode尚缺单独分配门禁。前后正常四臂均完整600有效／臂、无失焦，不能据此声称整关FPS收益；原A p95后测更高仍披露。新键盘综合＋冷Q、保护提示GPU像素专项均actual0且用户／源码／二进制守卫保持，fixture像素不冒称真人路线或最终美术。[本轮证据与限制](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/protection-query-performance-progress-20261012.md)。正常Main／Explore既有持键中断仍未解决，完整目标ACTIVE，无付费／自动化／发布。

当前同版回归：键盘＋冷Q、残片＋冷读回、门禁实体、保护提示GPU像素及限定摔伤专项通过。但正常速度Main／Explore各一次均在自有按键状态被撤销后中止（D、Space；均观察失焦，后者观察当帧focused=true），实际／wrapper1、未完成Boss及出口；缺结果文件只是下游症状，不把旧版成功借为本版成功，也不靠自动补按或重试掩盖。源码／场景／冻结／用户数据守卫保持；独立记录的fog shader hash也相同，旧正常工具的资源清单覆盖不足仍披露。下一步定位该边界并继续性能／视觉工作，详见[本轮完整证据](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/keyboard-input-boundary-progress-20261012.md)。目标ACTIVE，不宣称精致／流畅／全套验收完成。

最新推进（10月12日；输入生命周期）：修复了Continue及真实死亡复活后首采样可能重放旧J/K/Q/Shift/2的问题，仅新增生命周期专用重置，不改变机械爪普通取消、移动、物理或能量规则。Continue先真实RED再GREEN；复活以同一首帧fresh=true做恢复旧逻辑的负对照，确实再次攻击，恢复修复后五键均通过。正式保存场景的15项首采样观察、原起点复活／接触收集物保留、后续新J有效通过；暂停证据是持键恢复，不冒称新鲜边沿或真人硬件操作。独立复核确认负对照仅生产控制器不同，当前源码已精确恢复，用户109项清单无变化。[修复、失败记录与证据](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/keyboard-input-boundary-progress-20261012.md)。

相邻键盘综合＋独立冷进程Q、残片综合＋独立冷进程读回均exit0且保护清单保持。旧Handle/FATAL根因未解释，不能因新案通过而关闭；旧全项目综合入口仍有默认存档写入风险，不直接执行。下一步是最终同版回归与正常速度Main／Explore，再处理视觉／性能及自然缺额回返缺口。完整目标保持ACTIVE，未付费、恢复自动化、提交或发布；地图扩充方案仍未实装，下方10月11日媒体不是本次修复的录像证明。

最新图稿增量（10月11日；非生产玩法）：[关卡分布demo v005](E:/OurGame/Knight/过程图稿与视频/2026-10-11-关卡分布-demo-v005/presentation/canyon-demo-v005.png)补齐22个G点的类型／数量、逐组中文明细、当前22蓝点敌种和19陷阱唯一编号。v004两处字母遮挡保留为诊断，v005仅移标签／加引线，不动出生点或方案数量；仍是9类64普通名额（替换旧22，主46／支18）及Boss1的待审方案，未实装／未试玩／非最终美术。本版4096×2560原PNG／展示副本、SVG／JSON／源码快照和索引15项2551975字节已保存；完整键盘／残片／迷雾目标继续进行，不以地图图稿替代运行验收。[方案与边界](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/docs/design-drafts/2026-10-11-canyon-enemy-trap-demo-v005.md)。

最新推进（固定60完整键盘录像与21:01媒体核验）：当前Mobile/Vulkan自动Main从标题同Player完成Boss及有支撑出口，J66/K43/L47、mouse/legacy0、恢复0/0、D60结算及事件各一次且位置／保存hash不变。新[键盘迷雾v002归档](E:/OurGame/Knight/过程图稿与视频/2026-10-11-键盘迷雾-v002-完整路线/README.md)保留原AVI、约3分钟完整MP4、两个20秒原速有游戏音轨预览、14张原尺寸帧、三案源码／六二进制／保护清单与正常时间性能记录。三个MP4完整解码10805/1200/1200帧exit0；三独立索引2482项940190369字节经限定独立复核，包含源码／工具／诊断，不是新美术数量。原始漏存的一份冻结脚本另补副本，不重写初始索引／收据。

原尺寸审阅仍NEEDS_WORK：硬三角明暗、重复平台背景、教程卡片贴近机关及动作特效／抓点重叠，不能声称视觉精修完成。正常时间Main失焦0、Explore失焦2，callback p95分别49.011/48.850ms，非独立GPU/present或流畅认证；固定60片不算FPS。正式Esc／真实死亡复活／Continue组合、收集综合Handle/FATAL和跨进程读回、更广自然缺额回返／反绕门、连续视听及实时性能仍未闭合，详见[当前证据缺口](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/current-goal-evidence-audit-20261011.md)。完整目标ACTIVE，未付费／自动化／提交／发布；怪物布局图仍待审，未借录像实装。

当前优先事项（10月11日；关卡分布demo）：用户取消怪物／关卡难度上限，要求丰富敌人数量和行为种类，并先直观看标明生成点、数量、机关的平面图。已保存[横版侧视总图v003](E:/OurGame/Knight/过程图稿与视频/2026-10-11-关卡分布-demo-v003/presentation/canyon-demo-v003.png)与[设计说明](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/docs/design-drafts/2026-10-11-canyon-enemy-trap-demo-v003.md)，待用户审阅。现有源码配置5类22普通名额；拟议新版9类64名额（主46／支18、22编队），另保留Boss1，6类新陷阱19点。新版64替换旧22，不相加；不是同屏或实跑计数。蓝点／现有区域与橙色候选分开，内部地形未验证，不把图稿当已实装／试玩或正式美术验收。纯键盘、收集门禁、迷雾和原存档要求保留；可预判、合法碰撞和稳定性不因取消难度上限而取消。下一步需细化并确认架构级实施，不直接改旧数量上限来伪装新机制完成。完整目标仍ACTIVE，无付费、自动化或发布。

本次非运行资源表（未放入assets，不作为游戏美术导入）：

| 名称 | 用途／状态 | 尺寸／游戏内大小 | 保存位置 | 费用 |
|---|---|---|---|---|
| 迷雾阴影柔化v001局部验证 | 可见侧柔化；八帧已审仍NEEDS_WORK，正常登台FAIL未关闭，非最终资产 | 完整735帧／12.25秒、1280×720/60，含原游戏音轨和清理尾帧；固定步非FPS | [原件／完整MP4／失败／索引](E:/OurGame/Knight/过程图稿与视频/2026-10-13-迷雾阴影柔化-v001/README.md)、[有限视觉审阅](E:/OurGame/Knight/过程图稿与视频/2026-10-13-迷雾阴影柔化-v001/视觉审阅-root-v001.md) | 0，无新付费素材 |
| 探索小地图v005新版路线诊断 | 当前Task8来源；地图可辨，整体视觉NEEDS_WORK；外层保护INVALID，非最终资产 | AVI与完整MP4各10805帧，1280×720/60，原游戏混音；固定步非FPS；七原尺寸审阅帧 | [原片／完整MP4／帧／来源索引／审阅](E:/OurGame/Knight/过程图稿与视频/2026-10-12-探索小地图-v005-新版路线诊断/README.md) | 0，无新付费素材 |
| 关卡分布demo v005 | 逐点敌种与数量、陷阱唯一号，待审／未实装 | 4096×2560；游戏内不加载 | [原图／SVG／JSON／源码快照／索引](E:/OurGame/Knight/过程图稿与视频/2026-10-11-关卡分布-demo-v005/README.md) | 0，无付费生成 |
| 关卡分布demo v003 | 结构化设计图，待审／未实装 | 4096×2560；游戏内不加载 | [原图／SVG／JSON／源码快照／索引](E:/OurGame/Knight/过程图稿与视频/2026-10-11-关卡分布-demo-v003/README.md) | 0，无付费生成 |
| 键盘迷雾v002完整路线证据 | 原运行媒体／有限抽帧审阅NEEDS_WORK，非最终资产 | 原AVI10805帧；MP4完整10805及两个1200帧，1280×720/60，实际游戏音轨；固定步非FPS | [原片／展示副本／14原尺寸帧／源码／性能与三索引](E:/OurGame/Knight/过程图稿与视频/2026-10-11-键盘迷雾-v002-完整路线/README.md) | 0，无新付费素材 |

v001／v002原件也已留存；三批分别14／15／16索引项、2350201／2374269／2393411字节，本次逐项大小／SHA256与来源副本核对0差异。数量含源码与说明，不是新增美术张数；README／index／review元数据排除，旧批次未重新全量复核，本地保存不等于GitHub发布。

最新续推（10月11日19:56；图稿交付后继续原验收）：只完善测试驾驶的输入留存检查和首失败事件见证，未改生产玩法／地图／敌人。真实物理W撤销专项RED→GREEN，丢失自有按住键时只报错、不补按；完整键盘专项与独立冷进程Q均通过。同版正常Main／Explore现均从真实标题完成Boss及有支撑出口、恢复0／0、持续D60仍只结算一次；Main无失焦，Explore两次失焦，残片分别6／7。p95约49.011／48.850ms，仍不能称稳定流畅；历史TempleSupply FAIL本次未复现，不当作根因已修复。第一次fresh尝试提前停于WalkTo1360也保留FAIL，失焦因果未证。原综合suite仍有默认user://写入反例，未冒险直接运行；完整视听录像／人工体验和历史GCHandle／超时仍开放。目标ACTIVE，新增布局仍待审，无付费／自动化／发布。详见[输入连续性与同版路线证据](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/keyboard-input-continuity-progress-20261011.md)。

此前热点阶段：Firefly私有Descendants仅把GetChildren数组遍历改为GetChildCount/GetChild，未缓存或降低刷新频率；真实支持查询预算RED→GREEN，64查询3057664→698880 bytes（fixture节点标签136随后更正135，不称完全不变测试源码）。静态四臂后两臂大量失焦，不能据此认证受控性能收益。早先Main有限通过／Explore实际失败及ClawHeld日志字段编译失败均作为历史保留，不能被当前新案覆盖。

意见与设定长期入口：[玩家修改意见与世界设定记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/docs/玩家修改意见与世界设定.md)。2026-10-11新增熔炉致死坑、活动／固定齿轮、定向弹板把人弹回齿轮的组合、固定暗箭／电磁射线，以及“用于销毁机器人的机械神殿”、遗骸藏未来装备、废料自主意识首Boss等想法；记录v005含10条本次意见、20项有限历史回填及现有源码基础核对，重复消息已去重。最新用户明确改为先遇致死组合、再单件教学，取代开发者原先建议的相反顺序；未要求首次必死。尚未找到固定齿轮危险面或环境定向弹板，已有活塞／冲量接口不能算新机关完成。记录与设计候选不等于已实装或正式剧情定案，未来逐次追加。

最新排错（10月11日18:22；分配边界定位）：20项相邻同线程分配合同完成真实RED→GREEN，原20项采样合同与真实早退六断言保持通过。新的正常窗口四臂各600有效、失焦／无效／溢出0、原消费者恢复及用户／源码／工具／二进制守卫通过；11个背景与38个平台表现节点各臂重绘0，不能再把它们的绘制数组当作此静止窗口的持续分配根因。撤雾C仍有约578MB同线程分配，全部在上一PostDraw到下一PreDraw之间（含更新、重绘和观测开销），并非已归因某方法或整关性能修复。下一步量化教学提示等更新路径；完整路线／历史异常／正常视听证明仍未关闭。详见[阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。目标ACTIVE，无新机关实装／削减内容／媒体／付费／自动化／发布。

最新排错（10月11日17:57；早退留存与完整有界诊断）：真实主动early-quit先复现数据丢失，再同步Finalize保留partial；首修退出视口错误保留FAIL，修正清理顺序后六断言及20项采样合同通过。正常StartFloor四臂（原A／只隐藏native绘制B／撤原controller-canvas C／恢复A）现各600有效间隔、invalid/focus/overflow0，原对象／新material恢复成功、actual/wrapper0、stderr0、各守卫保持。但这不是游戏性能修复：四臂p95约40.1/36.0/33.5/36.9ms，C仍有大量全进程分配；不能宣称整关流畅、GPU/GC根因或真人验收。旧~75s缺口未重现、触发者未明，历史GCHandle／两55s超时与正常视听证明仍开放；旧全项目suite入口有user://写入风险，未无隔离执行。下一步定位非fog表现和路线热点，目标ACTIVE；未改生产玩法／地形、付费、自动化、提交或发布。完整证据见[阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

此前隔离诊断（10月11日；失败保留）：首次A/B各600、C80但恢复失败；后续engine0提前结束无终结／数据文件，均保持FAIL，不因上述新案通过而覆盖。此后只修诊断观测／留存，不把它称为生产迷雾性能改善。

最新增量（10月11日16:14；正常速度路线及性能排错）：quad修订后的正常速度自动纯键盘Main/Explore均完成真实Boss、有支撑出口和持续D60的一次结算，stderr0，五次地形绘制错误未再出现；两案均记录一次失焦，只能分类为FOCUS_LIMITED。进Boss前隔离保存各有六个不同残片ID且无旧档数量豁免；Explore最终七件，不能称九片全收齐或用保存代替逐片事务验收。**整体仍NEEDS_WORK**：p95约79–82ms、p99约130ms，尚不流畅；后续控制器专项两次55秒超时保留FAIL，新增阶段计时后一次约48秒通过不代表原因已修复。其余native/正式雾像素/raster/Q及参数合同通过，但未宣称全项目综合回归通过。下一步分离脚本与渲染耗时，继续查超时和历史GCHandle异常，再补正常速度视听录像与审阅；目标保持ACTIVE。文档及证据在[阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)，无新媒体、付费、自动化、提交或发布。

最新增量（10月11日15:47；真实速度排错）：正常窗口纯键盘Main实际完成Boss和有支撑出口，但出现五次地形边缘多边形绘制错误，严格按FAIL保存；不是已完成交付。已用真实GPU稳定复现“灯光圆边界切出一float ULP窄条”，仅把已知四角提示改为quad primitive，不改范围、遮挡、地形或存档。全可读性GPU回归 `fog-20261011-154602-623-cf345770bf5948878f1fb0d51fdae62a`通过，正在重测正常速度Main/Explore。旧实时诊断p50/p95/p99=26.537/71.624/99.508ms，不能称稳定60fps；无录制/人工体验，历史GCHandle问题仍开放。目标ACTIVE，无付费、自动化、提交或发布，原失败记录不覆盖。详见[本轮排错记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日；原机关提前预警保护）：活塞与摆锤现在复用原始预警的颜色、位置和自然时序，在雾上逐小块检查真实遮挡后显示；不穿墙、不另造计时，不改变机关碰撞、伤害或用户存档。六项同版专项（其中CPU/native非像素、controller明确pixels=false）以及fresh纯键盘Main/Explore均通过：真实标题→同一角色→Boss死亡→完整支撑出口，恢复0/0，持续D60仍只结算一次；探索支路残片6→7、补给HP1→3及双向捷径通过，11遭遇激活/10击败，不称全清。两个最终case为 `route-result-telegraph-main-20261011T150820958-94efdc677d68431d881db618fe4b2473`、`route-result-telegraph-explore-20261011T151135128-603eb32a0169420b98cc0ec8bc52a26b`，实际/wrapper0；只读审计确认同版运行来源及用户数据保护。路径含warning触发严格诊断的旧案保持FAIL，未改过滤器或旧工件。**目标仍ACTIVE，整体视觉NEEDS_WORK**：上述不是正常速度连续视听、实时性能或人工手感验收，历史综合GCHandle失败也未解决。本轮未生成/展示新媒体，上一归档不是本轮预警版本的视觉证明；下一步为真实正常速度验证与资源寿命定向诊断，无付费、自动化、Git提交或发布。详见[迷雾阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

以下保留此前时点记录，不能将旧待办或“雾隐藏”描述当当前状态。

14:38当前结果：正式雾候选接入后，同版五GPU专项和fresh纯键盘Main/Explore均通过。两路线从真实标题起步、各案同Player→Boss死亡→有完整支撑出口，恢复0/0，持续D60仍只结算一次；原用户数据、场景、冻结清单和二进制保持。路线case分别 `route-result-fog-main-final-20261011T142724637-b659dfd66f474860851b66725fd455a3`、`route-result-fog-explore-final-20261011T143058834-73542fbc664d46e989e24f9dba9dfe8a`，实际/wrapper0。它们是headless固定60自动键盘，不是正常渲染整关/人工/实时验收；此前58.78秒录像仍FAIL，未被改标通过。新原片、15秒预览、八帧、源码/日志已归档至[头灯迷雾v001](E:/OurGame/Knight/过程图稿与视频/2026-10-11-头灯迷雾-v001-正式接入/README.md)。下一步保护两处原机关提前预警，之后正常速度视听/性能和历史GCHandle根因；整体视觉NEEDS_WORK，完整目标ACTIVE，无付费、自动化或发布。

14:26增量：正式显示、保存controller、保护提示像素、fog raster及Q交互五GPU专项在同一最终来源通过，用户数据/源码/二进制前后稳定。残片夹具仅改查父层隐藏后的实际可见性，新增双雾负控，未改像素阈值。约58.78秒Vulkan自动Main原片仍FAIL：到升降机/捷径交界后fresh K组合断言失败，日志中实际方向/目标正确，但没有单独记录当时motion布尔值，不能只凭该日志坐实根因；已移除11处已过时的被动motion否决条件，其他方向/样本/键盘来源/碰撞/时限守卫不变，fresh Main/Explore待跑。原片、15秒预览、八张原尺寸抽帧及来源保存在[头灯迷雾v001](E:/OurGame/Knight/过程图稿与视频/2026-10-11-头灯迷雾-v001-正式接入/README.md)。抽帧近场可辨，但角色、几何和UI仍偏原型，整体NEEDS_WORK；连续视听、实时性能、提前预警及历史GCHandle仍开放，目标ACTIVE。以下为旧阶段事实。

最新增量（10月11日14:05；正式迷雾接入候选）：正式关卡不再永久隐藏迷雾，只在Playing、当前真实灯位/遮挡帧有效及九类保护消费者均可见时显示；缺保护层则保持明亮。保存环境实际GPU像素验证了角色近处清晰、远处加雾、降低迷雾设置以及暂停/恢复/标题撤销，保存controller全族也通过。**不是整关视觉或性能验收**：活塞/摆锤提前阻路预警的雾下可读性、完整正常渲染路线/新归档短片及历史GCHandle综合FAIL仍开放。Vulkan录像入口已定位测试守卫把被动鼠标motion/生命周期全空帧误认作鼠标操作，已用真实motion、真实LMB拒绝和恢复J专项修正；生产输入未变，三段13帧失败录像保留。目标继续ACTIVE，未付费、恢复自动化或发布。详见[迷雾阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。下方保留历史增量，不将旧“雾隐藏”描述当当前实现。


13:37当前增量：连续地域补捡0→6已真实跑通；同Player、原31段及六个原残片接触/即时保存、HP5/恢复0、回原Temple地面与数量准入均通过。仅修正测试驾驶的真实战斗/站位前提，不注入ID、不改角色/敌人/能量/时限；该案披露机关/检查点前置，**不是自然标题漏捡全程**。当前719源码/42场景版本的fresh Main与Explore也均从标题完成Boss及有支撑出口，D60只结算一次；三案build/engine/wrapper0，六binary及source/scenes/protected109/frozen12跨案一致/前后保持。详见[阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/quest-fragments-task3-progress.md)。完整目标保持ACTIVE：自然更广缺额回返/Temple显式多段合同、综合资源句柄异常、正式雾、正常速度视听/性能与归档审阅录像仍待闭合；不把自动键盘或本增量当人工手感/最终视觉验收。无新媒体、付费、自动化、提交或发布。

12:39同版回归：已把真正跨区抓墙/自然落台表达为Thread-only ClawDrop合同，并仅发布已实走的盆地→深渊精确反向图边，不改620px爪范围或角色物理。全部18方向生成合同专项、`published-chain-20261011T122922446-3452ade67cca41e7821fd0a5db609c53`、`published-main-20261011T123121702-fe1b2c7658b946d48c7077bd251b2afc`、`published-explore-20261011T123446698-55679e51cd5e4a969b410e31f050bf37`均build/engine/wrapper0。chain仍是披露前置的同Player地域fixture、31处支撑、0→2/HP5/恢复0；Main/Explore均真实标题起步、同案同角色→Boss死亡→有支撑出口，D60后一次结算及保存字节保持，Explore支路6→7/HP1→3保持。718源码、42场景、109用户数据、12冻结清单与六binary跨三案同版/前后稳定。**完整目标ACTIVE**：下一步补同角色0/4→6回返，之后正式雾、正常速度视听/性能与归档审阅录像；多段Temple合同和综合句柄异常仍未闭合。上述是原生自动键盘证明，不是人工手感、最终视觉或整体验收。无新媒体、付费、自动化、提交或发布。

最新增量（10月11日11:40；连续补捡链及同版整关回归）：已修复最后回返缝的零余量问题：仅由C# builder把GearPistonOne左沿右移20px、宽300→280，保留右沿/上下沿及全部原危险；60px缝给40px角色留下20px总净空。先记录实际伤害来源为ShieldLane，再用真正的D/L右向防御与A间隙移动通过，不改角色物理、攻击、能量/热量或时间预算。同Player地域链现在真实走完Temple→Split→17阶→Camp→Abyss拾取→Camp，31处完整支撑、HP5、恢复0、实际0→2；这仍是披露前置的地域fixture，不是门前0→6。fresh完整Main `113233792-2570aa1885c54a5694a01aede8cadef3`与Explore `113553553-480b3f02fc964bf19ddababc496799af`在同一715源码/42场景版本通过：真实标题起步、各案同Player、Boss死亡/受支撑出口各一次，持续D60后保存hash不变；Explore保留双向捷径、支路补给回返，残片6→7。两案build/engine/wrapper均0、stderr空，109项用户数据保护和12项冻结清单稳定，六二进制跨案同版。纯内存收据守卫97/97通过；下方失败记录保留为历史，未删改。**完整目标仍ACTIVE**：更广0/4→6回返、显式多段跨区路线合同、综合资源句柄FAIL、正式迷雾、正常速度视听/性能及新归档录像审阅尚未完成。这不是人工试玩或最终美术验收；没有新媒体、付费生成、自动化、Git提交或发布。

历史增量（以下按原记录保留，不代表最新结果）：

最新增量（10月11日；同角色连续补捡链仍未通过）：已将原Temple七段、17阶长捷径和Basin/Abyss探针连接为独立地域fixture，不重新seed、换角色、重置残片或降级Temple检查点。五个原生失败案均完整保留，最远真实到达ShortcutCampReturnStep；自然等待独立下层SteamJet安全周期后能保持HP5，近边单跳也能到低台，但最后GearEntry回返缝仅40宽（等于角色直径），同时进入防御教学区并掉血，尚未到Camp或取得0→2。两次伤害的实际来源ID仍待核实，不能只凭击退参数定论。最新独立Basin `105738105-be27c3bcfdb94bc18b2c390037651ede`、Temple `105829899-cc47d4c4b4ea4f7d99f657ca203d7236`在同一715源码版本重新GREEN、实际/wrapper0、stderr空；它们不是连续链或0→6通过。此前完整Main/Explore通过对应上一714源码快照，生产地图本增量未改。纯内存witness89/89、时限合同30/30通过；用户数据、场景和冻结保护稳定。下一步核实最终伤害来源并修复实际净空/防御走法，保留原物理；整体目标仍ACTIVE，正式雾、综合句柄FAIL、完整缺额回返、正常速度视听/性能和归档录像仍未闭合。

最新增量（10月11日；盆地→深渊补捡通道）：修复真实W+K向上回返被旧坠落KillZone误复活的问题。先留存实际FALL RESET失败，再仅由C# builder新增有限BasinReturnSeam；生成场景只新增该Marker并缩短首段恢复区域，原墙、平台、残片、敌人和角色物理不改。新地域fixture已实际走完营地→原Left4抓面→深渊底台拾取abyss-floor→返回营地：HP5保持、恢复0、普通跳/冲刺0、唯一cast/两次K，隔离存档从0→1；两侧坠落恢复仍保留。旧Temple七段回返和fresh完整Main/Explore也同版通过，均engine/wrapper0、stderr空、用户数据/源/场景/冻结保护稳定。**这不是Boss门前0/4→6整条自然回返通过**；显式多段跨区路线合同、完整补收集、综合资源句柄FAIL、正式迷雾及正常速度视听/性能/归档录像仍待完成。目标继续ACTIVE，无新媒体、付费、自动化、Git提交或发布；详见[回返阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/quest-fragments-task3-progress.md)。

最新增量（10月11日；残片保存失败反馈）：原QuestFragment真实写盘失败时，附近240候选范围且无遮挡的原本地化Status文字现在可由正式layer2消费者独占显示为固定像素短条；不另造保存结果、不自动重试，离开/遮挡/暂停/卸载/成功重试即撤回并释放世界标签。真实锁盘失败→原Player再次接触成功、中英文完整GPU字形、1.5镜头缩放、实际保存controller和生命周期负控通过；GPU、controller、CPU/native、raster、Q GPU及独立正式Q六专项同版通过。fresh Main `093823236-ca34cf7511e649d19275d7f16ffcec24` 与Explore `094335415-efb6b3c6d1ee47419fd279e8a3ef24a4` 均wrapper/engine0，标题同角色→Boss→有支撑出口，D60仍一次结算。**残片综合仍FAIL**：`093300-070-84a6205d7d9c468b9b19d0e0f60592f0` 同案出现首次场景加载Handle异常和随后释放FATAL，跨进程读档未执行，未重跑到通过。专项和路线通过不能抵消该崩溃。正式雾仍隐藏；更广缺额跨区回返、资源寿命根因、正常速度全UI/视听/性能及归档审阅录像尚未完成。目标ACTIVE，无新媒体、付费、自动化、提交或发布；详见[阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日；原门体边界批次）：八处原seal/钟链/长捷径/Boss准入罩/独立Entry与Exit/两压板门已接入保存父场景独立layer2 Barriers，仅读实际启用native矩形的当前中性薄边。三片准入罩逐shape遮挡，不整body豁免；原receiver/deferred真正提交开门后撤边，不凭视觉或IsOpen先行显示可通过。八原saved源GPU和保存controller、CPU/native、fog raster、Q GPU、独立正式Q六项在同一744项source快照通过，旧键盘综合释放FATAL仍未解决。fresh Main `090022458-c3347e33425d4198a8d3f8844ca0974e` 和Explore `090342940-c0f809405611416196ea6251fbf49441` 均实际/wrapper0：标题同角色→Boss→有支撑出口，恢复0/0，D60保持一次结算；Explore还保留支路6→7、压板补给1→3/5与双向捷径。独立产物审计确认同版源码/场景、保护文件与六binary对齐；原134父节点/13子资源共享不变，仅新增Barriers节点/script。测试中发现并纠正原路径绑定、读档重启重力及墙内观察点等fixture前提，没有修改生产地形、碰撞、伤害、机关条件或存档规则。上述是自动键盘和受控GPU证据，不是真人、正常战斗全路线或实时视听/FPS验收。正式雾继续隐藏；保存失败文字保护、更广0/4缺额跨区回返、资源释放崩溃、正常速度视觉/性能与新归档审阅录像仍待闭合。完整目标ACTIVE，无新媒体、付费、自动化、提交或发布；全部RED/FAIL/同版审计详见[迷雾阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日08:27；运动几何批次）：原升降台、活塞和转动摆锤已接入保存父场景独立layer2 MovingBodies。提示为真实启用native矩形内的中性薄边，不把移动/通用HazardActive误作HP伤害或保证安全落地；旋转/镜像/owner offset精确局部face授权，仅豁免同一终端面，同body其他shape与所有墙仍遮挡整笔画。保存controller、CPU/native、三原saved来源自然运动GPU、fog raster、Q GPU及独立正式Q六专项均在同一741项源码快照通过；不是键盘综合通过。fresh Main `081817193-0feba682772342a6bec7cb6946f3706e`、Explore `082307576-f8c55583ba6e4bf49a4564107fd3757d`均actual/wrapper0，标题同角色→Boss→有支撑出口，恢复0/0，按D保持60帧仍只结算一次，源/场景/用户数据/冻结12与二进制守卫稳定。已记录并纠正三处CPU fixture前提和一处流式邻居预期错误，没有放宽生产碰撞/遮挡/运动或守卫；原父场景133节点/13子资源共享不变，仅新增脚本引用和一个节点。键盘综合释放FATAL仍未解决，不重复运行到通过。上述是headless自动键盘与受控GPU专项，不是人类手感、正常战斗全路线或实时性能/完整视听验收。正式雾仍隐藏；其余门边界、保存失败UI、跨区0/4缺额回返及新归档审阅录像仍待完成。目标ACTIVE，无新媒体、付费生成、自动化、提交或发布。详情见[迷雾阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日07:52）：两扇原压板门的关闭线/五刻度现共享原visual的不可变paint，正式父场景由C#builder新增独立layer2 PressureGates节点，controller只提交当前mounted来源及fresh native遮挡帧；不复制关闭时钟或改变防夹、碰撞、宽限和预警时间。post-pack GPU、保存controller（含真实钟链关闭遮挡→原receiver实际开门后可见）、CPU/native＋原门全部模型/14原生runtime族、fog raster、Q GPU五项通过，独立正式Q另通过。**键盘综合本轮失败**：`keyboard-20261011-074719-732-da41fdcd768f41ebb09f4cdf9a5d0984`在sample UI检查后触发Godot/.NET RefCounted释放FATAL，actual -1073741795，必跑formal Q未执行；不称六项全绿、不重试到通过、不推断与历史fragments句柄错误同因。源/场景/用户数据/六运行二进制前后守卫稳定、冻结12不变。builder仅增加脚本引用/一个节点，原132节点与13子资源共享不变。正式雾仍隐藏，本次没有新的完整Main/Explore路线或媒体；旧路线属于此前版本。移动机关/其余关键边界、缺额回返、释放崩溃定向诊断、正常速度视听/性能及归档录像仍未完成。完整目标保持ACTIVE，无付费、自动化、提交或发布；所有RED/FAIL/GREEN与fixture裁定保存在[阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日本轮）：原Basin持续伤害区已接入保存关卡独立layer2 ShieldLane提示。只标实际启用矩形内的短橙边，使用当前native owner变换与整笔画遮挡检查；不把伤害脉冲计数解释为预警或安全窗。停掉真实伤害回调、关闭监测、暂停、卸载或排队删除祖先时立即撤旧提示。原Area自身layer=0、Monitorable=false仍可合法造成身体轮询伤害，显示不误排除这两种状态。缺类、缺保存节点及删除Monitoring检查均取得行为RED；恢复后六专项（GPU可读性、CPU/native、雾raster、Q GPU、通用键盘＋必跑formal Q、保存controller）全部GREEN。C#父builder只增一个脚本引用和ShieldLane节点，原131节点与13子资源共享不变。新fresh Main `071530501-c1a9ebbf41e04fc284460d36549ea837` 与Explore `071849861-61e5ba79403b4cd2bcc0cfcd30fa1c82` 完整自动回归均通过，实际Boss后有支撑出口成立；用户数据、冻结12及运行来源保护稳定。正式雾仍隐藏，压板门关闭预警、移动障碍/升降台落点、更广缺额回返、历史GCHandle综合FAIL及正常速度视听/性能和归档录像仍待完成；目标ACTIVE，不称整关已完成。没有新增媒体、付费生成、自动化、Git提交或发布；失败与当前证据详见[迷雾阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日06:51）：五个真实蒸汽喷口的预警/喷发边界已接入正式保存关卡独立layer2 Steam节点。读原机关时序与当前native碰撞形状，断续黄线区分预警、较粗橙线标喷发；整条笔画通过同帧遮挡检查，不画穿墙或填充安全区域。GPU实际RED检出并修复Disabled+Remove后旧危险提示残留；切换、原阀停机、隐藏/远距、暂停、卸载/重入均撤旧提示。GPU覆盖Basin standalone及Trench两喷口，保存controller另验证包括Frozen在内的全部五源；不冒称五源均作像素验收。C#父builder只增加Steam节点/脚本，原130节点属性及13子资源共享不变。最终readability、CPU/native、雾raster、Q GPU、通用键盘及必跑formal Q、保存controller均GREEN；新fresh Main `064156761-45d53fae23cc40e0ad50a9155a96e4c7`和Explore `064457500-4df014ab2e34450288e26c2c263e3c49`完整自动回归通过，标题同角色→Boss→有支撑出口及D60一次结算，源/场景/冻结12/用户数据保护稳定。正式雾继续隐藏；剩余ShieldLane/压板门等危险提示、更广缺额回返、综合资源句柄错误和正常速度视听/性能归档录像未闭合，目标ACTIVE。无新媒体、付费生成、自动化、提交或发布；详见[迷雾阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日本轮）：真实静态落脚边缘保护已接入保存关卡的独立layer2节点，使用同帧native矩形快照，只标短顶边，不补空隙或把深渊/机关声明为安全落点。像素测试实际检出并修复贴面薄遮挡漏检和1px碰撞体外假边缘；最终GPU、保存场景Title/Playing/Pause/Resume/Title、CPU遮挡、雾渲染及Q回归均GREEN。C#父builder只新增Terrain节点及脚本，原129节点属性/13子资源共享不变。新版本fresh完整Main `060254276-cdfff38935b64e2ca6b96ede61cac509`、Explore `060620280-9acc5c93bb474299a9f165a10b16c52f`均engine/wrapper0、stderr空、用户/源/场景/冻结12/运行二进制清单稳定；标题同角色→Boss→有支撑出口及D60一次结算通过。以上仍为自动化，不是最终美术、真人手感或实时性能验收。正式雾继续隐藏；下一片为五个真实蒸汽喷口提示，更广缺额回返、综合资源句柄错误和正常速度视听/性能归档录像仍待闭合。目标ACTIVE，无新媒体、付费、自动化、提交或发布。详见[迷雾阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日05:32）：新增独立自然漏捡路线已实跑GREEN：真实标题→原Main战斗/地形，仅正常Space跳过gear-gallery；门前原始schema4存档确为5件、非豁免，封印和钟链已开，三面准入罩仍关闭。角色实际D被挡于6121.924/1079.9331后，普通A返回、原梁W+K配合Space取得temple-upper，5→6仅一次BodyEntered/Collected/Feedback/Save替换，不领补给、不掉血（HP3→3）、不增普通跳/冲刺且恢复0/0，再从同一门进入Boss并完成有支撑出口与D60一次结算。独立case `route-result-natural-five-six-20261011T052731120-ca93aab8040e4f6590368e3dd8b88b62` engine/wrapper0、stderr空、所有库存保护稳定，结果不冒用原Main artifact；正式生产/场景本轮未改。门开时刻字段为回门复查而非实际转换。尚缺0/4件等更广补收集/反向图边、资源加载句柄错误闭合、正式雾全部提示及实时视觉/性能归档录像；不将这一自动化GREEN当人类体验、美术或整体交付。目标保持ACTIVE，无付费、自动化或发布。细节见[阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/quest-fragments-task3-progress.md)。

最新增量（10月11日05:06）：Boss战前准入从无顶部硬界证明的world-height单柱改为同体三片有限闭合碰撞，与原ArenaFloor四角正重叠；builder生成，原地板/梁/机关/残片和资源共享保留。真实缺顶RED `044851-669-3a7e65b98fac40f7891f6a8210ce1c4e` 后，实体GREEN `045435-505-e69362af8fac4ca98a6657f1a702d36f` 验证各片native shape、四角真胶囊查询、顶部K/下降dash与东侧dash阻挡、同帧六件/复活/合法旧记录/战后恢复统一解除及fog几何容量。fresh完整Main `045654663-ab63fb231d5f4668bc74b15db23e6f72`、Explore `050013403-13d4658e63cb46c68b69f37c1a6ee947`均engine/wrapper0、stderr空，标题同角色→Boss→有支撑出口，恢复0/0、D60仍只完成一次；用户/源场景/冻结12/六二进制稳定。以上是自动键盘回归，受控外部姿态不等于自然可达或人类画面验收。下一步仍为真实漏捡5→6及更广缺额回返、综合加载句柄错误定位、正式雾可读性和实时视觉/性能归档录像；整体目标ACTIVE，正式雾隐藏，无新媒体/付费/自动化/提交/发布。详细见[阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/quest-fragments-task3-progress.md)。

最新增量（10月11日04:39）：实体准入修订后的fresh完整Main `043015746-7ea44878770041edbfed89c271603fdd`、Explore `043336293-4b809dac30b24e09b6bbd3ca2f54ce60`均engine/wrapper0，正式标题Enter→同角色→Boss真实击败→有支撑出口→D60保持一次Completed；原用户存档、源/场景、冻结12与运行二进制保护稳定。Explore保留支路6→7、HP1→3及返回下层，不是漏捡5→6。独立同帧准入RED→GREEN保持有效；整体目标仍ACTIVE。下一阶段优先自然缺额回返/防绕门和加载错误定位，再闭合全部雾可读性、正常速度视觉/性能和新归档录像；正式雾仍隐藏，不视为完成交付。详细证据和未审定边界见[阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/quest-fragments-task3-progress.md)。

最新增量（10月11日04:29）：同帧“第六件提交→立即BeginEncounter”先实际RED `admission-042711-156-a3c62e132f7244bf938450aee4af948f`，定位成功分支未自行打开准入柱。补一个deferred开放指令后实体全专项GREEN `admission-042811-345-d92daccea7cb46c3997864bbe00b3ece`，包含原生命周期/复活/旧记录负例，用户/源/二进制清单稳定。完整Main/Explore将以此版重新运行；自然漏捡回返和全可达反绕行仍未完成，整体目标保持ACTIVE，不恢复自动化。

最新增量（10月11日04:22）：最低6件＋原庭院封印＋钟链AND已接正式LostTemple实体入口和开战二次检查，独立不可抓准入柱防止旧180高门上方直接穿行；仅当前layout2 opt-in，旧样片保留。189→192节点的builder生成审计确认原地板/机关/补给/残片/资源共享不变。实体专项`admission-20261011-041855-140-7a6dab5602704e478409b3c3bb32c701`实际GREEN：5件原生顶门/有支撑回退、真实单跳/空中dash/高位K负例、同Boss原地5→6与补两机关自动开门、真实trigger、真实检查点恢复保持6、合法schema3仅数量豁免、已败0件恢复；并以实际RED修复未开战死亡误记胜利与同挂载败态恢复缺口。独立门可视生命周期`041947-821-39873d32a51047e9832a5529614a23ba`、显示模式fog controller`042009-840-4a2507d7b70c49dfbfa3d4a81c1390e7`及原生物理出口7例`042117805-9bc05feacdd846998824437d2b89462a`通过，源/用户/二进制保护稳定。以上为明确受控metadata/entity/exit证据，不是自然漏捡回返、所有可达高空绕行或完成版；本次production后fresh完整Main/Explore仍待复跑。综合收集首次加载句柄错误仍FAIL，正式雾仍隐藏；安全补六、更广0→6回收、其余关键提示、实时性能与新归档录像均保留完整目标ACTIVE。无新媒体、付费、自动化、提交或发布。细节见[收集/门禁阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/quest-fragments-task3-progress.md)。

最新增量（10月11日03:44）：Q唯一环境提示已接入保存父场景layer2，复用当帧native遮挡快照与原权威候选；接管时清掉世界层旧文字，暂停、离场、消费及真实死亡首帧撤提示。中英文真实字形、原对象挡墙、层级负控、source/renderer/controller重入GPU专项`033017-167-273bb36c4572497ab8545b812c56265c`通过，独立代码复核无本增量阻断。fresh完整Main `033628911-22e2842fb470481098337bbb505c3455`与Explore `033939206-c13d3a2afc2f43998c8e19dabbd4006c`均engine/wrapper0、标题同角色→Boss→有支撑出口→D60一次结算；源/场景/冻结12/用户数据稳定。Explore为11组激活/10组击败、原支路6→7及补给返回，不是缺件4→6证明。重要未解决项：本轮收集综合`fragments-20261011-033147-490-939fd0d182b745b491f9885b20a629d4`虽engine0/行为标记齐全，但首次GD.Load出现`Handle is not initialized`，wrapper按FAIL保留，第二进程读回未执行；不宣称全套通过或根因修复。正式雾仍隐藏，实体数量门/安全补六、其余关键提示、实时性能与归档录像尚待完成，目标ACTIVE。无新媒体、付费、自动化、提交或发布。详细证据见[迷雾/Q阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日03:02）：正式 CanyonBasin 蒸汽阀已明确支持站稳、80单位内且无遮挡的 Q 交互；只为这一手动阀门增加入口，其他曲柄、锁链与庭院阀不自动获得 Q。原 K 远距路径、能量、地图和存档事务保留。修复停控/离场及隐藏、撤销交互资格、移出范围后的旧提示；中英文动作文字来自同一权威候选。必跑的通用键盘＋独立真实关卡两阶段回归 `024616-676-2eba2fbb6bf146d7b7bf0b1d16e1139e` 两引擎均0，用户数据/源码/六项二进制清单稳定。fresh完整 Main `024809796-736d609aab29462082da64e3c3d375b8`、Explore `025145693-221fba6cce554f019a9996fb2ea26321` 也均engine/wrapper0、标题同角色→Boss→有支撑出口→D60一次结算通过，独立复核已闭合。Explore为11遭遇激活/10击败、原支路残片6→7与HP1→3/安全返回，不是漏捡4→6证明。混合测试进程的句柄异常历史case仍FAIL，冷进程隔离不等于修复引擎问题。目标ACTIVE：Q雾上层、其余关键提示、实体数量门/安全补六、正常速度画面/性能与新归档录像仍待完成；正式雾继续隐藏。本轮无新媒体、付费、自动化、提交或发布。详细证据见[键盘/Q阶段记录](E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-07-keyboard-controls/progress.md)。

最新增量（10月11日01:50）：真实攻击预警保护已接入保存父场景layer2；controller只读取当前mounted五类敌人及当前arena Boss的权威AttackTelegraph，以同一帧native遮挡快照批量提交，不改攻击/伤害/运动。真实飞蛾锁定俯冲→移动攻击→收招GPU、保存场景暂停/恢复与Boss Sweep/重置通过；实际RED复现“敌人排队删除而子预警仍存活”后最小修复，删除当帧即撤保护。父scene语义审查确认127→128节点，仅新增Attacks，原节点属性/顺序未改。fresh完整Main `013939853-52ad375c7557449193cc58bf633b3152`、Explore `014233519-ecca55f4e29e4b28bee36f8959970d22`均engine/wrapper0、KEYBOARD_ROUTE_V1_AUTOMATED_NOT_HUMAN、源/场景/冻结12/用户清单稳定；标题同角色、Boss、有支撑出口及D60一次结算通过，Explore原支路6→7/HP1→3与安全返回通过。墙钟134587/225112ms不代表玩家时长/实时性能。目标ACTIVE；正式雾仍hidden，Q正式对象、实体quota/安全补六、机关/危险落点等剩余提示、实时性能与新归档录像尚待完成，不称最终视觉/真人验收。详细失败、负控和SHA见阶段记录；无新媒体、付费、自动化、提交或发布。

最新增量（10月11日01:06）：本轮残片重入修复与新正式父scene上的fresh完整Main `005644562-1074fec9a8384975a6fe7b9e16f1c780`、Explore `010037566-7d3542154cab415e9b59b03528eb539f`均engine/wrapper0、KEYBOARD_ROUTE_V1_AUTOMATED_NOT_HUMAN，failure/guardFailure=null，源/场景/冻结12/用户数据稳定。标题同角色完成Boss、有支撑出口及按D保持60帧一次结算/保存字节不变，恢复0/0；Explore真实17台阶双向、temple-upper一次6→7、补给HP1→3/原lower安全回程成立。11组激活/10组击败，不冒称11全清。engine178698/245237ms仅隔离自动运行墙钟，不是玩家时长/实时性能。目标ACTIVE，尚非最终视觉或真人体验验收；实体quota/安全补六、Q正式对象和剩余雾上危险/落点提示、实时性能及新归档录像仍需继续。记录见[迷雾与残片阶段记录](.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。

最新增量（10月11日00:56）：附近未收集残片的小型可替换提示已接入正式保存父场景的layer2；当前mounted邻区、240候选范围、真实native遮挡与同帧快照决定显示，未扩大灯光/抓爪范围。真实GPU检查通过近/远、层级负控、16px墙、卸载/重入、原Player碰撞拾取一次及重新开局状态恢复。该测试同时复现并修复QuestFragment重入后拾取/SessionStarted订阅丢失和重复解绑错误；存档事务不变。父builder只生成允许的父scene，重新加载controller与post-pack GPU均GREEN；收集/迁移/失败保存/补给聚合及第二进程只读核验`fragments-20261011-005348-893-6746b8b5a220446abfe15e610dd2f1fc`实际通过，源与用户数据清单稳定。旧004034/004531失败保留，不以engine0冒称通过，也不据本次安静退出宣称修复历史GCHandle异常。正式整层雾继续隐藏；其余危险/落点/Q实体/数量门和安全补六回路未完成，现准备在新源码与父scene上fresh Main/Explore，00:27旧路线不替代本轮结果。无新媒体、付费、自动化、提交或发布。

最新增量（10月11日00:27）：修正诊断标签后的fresh完整Main `001614604-0be8734de661477e8adf2fbce7e68070`及Explore `002005259-2c1cd69d4ed34d07a3d8678cadddf1e5`均engine/wrapper0、KEYBOARD_ROUTE_V1_AUTOMATED_NOT_HUMAN；实际engine耗时151613/207313ms不是实时帧率或玩家游玩时长。两case source/scenes/frozen12/protectedStable=true、guardFailure/failure=null，三原日志固定classifier无fault、stderr空。标题同角色完成Boss、有支撑出口、D60只结算一次且保存字节保持，恢复0/0；Explore激活11遭遇/击败10组（counterweight-lift未计defeat，不能称11全清）、17台捷径阶梯双向、原补给HP1→3及temple-upper一次收集6→7/回程不重复消费成立。先前235804仍FAIL。仅自动路线验收通过，目标ACTIVE：实体quota/安全补六回路、正式迷雾剩余提示/实时性能与归档录像仍未完成。下一窄片为真实saved残片的近距提示；240像素为可调候选，非已验收数值，不扩大620抓爪射程，不生成付费素材。

最新增量（10月11日00:15）：只改三处新测试日志的歧义标签，未改生产玩法、真实状态、输入/等待预算或固定故障分类器。FrozenLower单一error字段改stance_delta；Steam/Shortcut两处新preflight只在本次Snapshot插值把warning布尔标签改telegraph_visible，原Snapshot保留。实际先RED后GREEN的文本回归12+24项，以及旧courtyard14项再次通过，classifier SHA仍C96D…82C8；MainRouteTests SHA B909…9CD0。历史235804仍FAIL，接下来冻结全部源/场景/文档，fresh完整Main与Explore，不用局部测试替代整关或视觉验收。目标ACTIVE，无新媒体/付费/自动化/提交或发布。

最新增量（10月11日00:08）：精确回程停车候选fresh Explore `235804211-d0cf3fee865847dfa8014d0007083d9d`实际engine0/246535ms、stderr空、source/scenes/frozen12/protectedStable=true；同标题角色激活11遭遇、击败其中10组、通过17台捷径阶梯双向及支路补给安全返回、Boss击败和有支撑出口7341.8877/1079.9801、HP3/恢复0，结算后D60帧位置与保存SHA不变。原fragment事务6→7/两Body仅一次消费、HP1→3/唯一heal与disk/HUD都成立。但wrapper1/UNEXPECTED_FAILED：30条固定分类器拦截来自FrozenLower新error=标签24条、两新keyboard preflight内真实warning布尔6条（含external重录）；其他原生/管理错误未见。本历史case维持FAIL，不归一化为GREEN。仅修新producer标签、保留classifier原SHA/regex/白名单与所有行为判据，纯测试先RED再GREEN，随后fresh整轮；目标ACTIVE，实体quota/安全补六回路/正式迷雾与实时归档录像尚待完成。

最新增量（10月10日23:55）：strict Explore `235034651-49c308bec66b4339a9613ee5d63b50e9` actual/wrapper1、179738ms、source/scenes/frozen12/protectedStable=true、guardFailure=null。严格一次原生temple-upper Collected/Body、Feedback/HUD/磁盘6→7与唯一Save引用handoff实际通过；12连续fullShelf/压板门通过，原补给HP1→3、heal/overlap/persist各1，回程第二Body不重复消费。新失败仅return5896旧单边走法neutral后中心5884.9077、胶囊左缘5864.9077低于shelf5865约.0923；同角色/生命/恢复保持，无Boss交接或整轮通过。正在仅对这一次键盘回程采用原90+6内A/D制动停车，不放宽fullCapsule/门/保存/动作基线，不改地形或生产数值；本case保留FAIL。目标ACTIVE。

最新增量（10月10日23:49）：真实W/K抓点预览、同native hit锁定、取消、上雾cyan像素与同HUD层级负控已通过；detach仍instance-valid的Player实际复现旧cue残留（234440），仅HUD改为live依赖重绑、离场清缓存/无有效Input隐藏frame后234550实际GREEN，source/userdata稳定。正式雾仍隐藏，不等同整个正式HUD布局/暂停/卸载全部验收。新增真实D→A头灯180度回归234642 GREEN，临时snap234720和linear-vector234805分别行为RED，精确恢复controller原SHA后234844再GREEN；生产平滑原已存在，不冒称新美术改进。探索补给支路已补严格一次temple-upper原生收集/Save事务及独立caller收据，编译通过但fresh Explore待实跑；原12次完整落台/生命/恢复/输入预算不变。目标ACTIVE，无新媒体/付费/自动化/提交或发布。

最新增量（10月10日23:20）：停车候选fresh Explore `231304047-27b94439b97c4279b27f87f9a9aa6150` actual1/188555ms、wrapper1、source/scenes/frozen12/protectedStable=true/guardFailure=null。真实A/D在原90+6内停5826.426，上抓actual anchor5826.426/860、normal(0,1)，同cast Arrived/Space双receipt后已到搁板高度5870.574/923.9793，但尚未完成12次完整落台。新失败是原综合Safety的保存对象身份断言；实际前后磁盘唯一新增`canyon.v2.fragment.temple-upper`（6→7），不是archive，其他JSON字段及HP1/动作/恢复基线未变。正在核对真实收集事件/重叠和严格存档handoff，不直接撤Save保护；本case仍FAIL，不能报Explore完成或生产玩法修复。目标ACTIVE；正式迷雾关键抓点GPU验证并行准备，实体门禁/合法补收集回返/实时录像尚待完成，无媒体/付费/自动化/提交或发布。

最新增量（10月10日23:00）：fresh Explore `225620493-fd0496c7f9024de985a6352770344abb` actual/wrapper1、211764ms、source/scenes/frozen12/protectedStable=true/guardFailure=null。全部17台捷径双向、六side和神殿机关通过；修订庭院策略实际三次K/九次J、HP1→1/伤害0/恢复0，并自然到神殿checkpoint。新失败在补给支路12次完整搁板支撑要求：真实W+K、同cast Arrived及同tick D+Space Boost/Released均成立，但落回原下层地面，无死亡/加血/恢复。旧鼠标成功日志与当前键盘日志同样释放后vY92.5；键盘直上抓点X5813.2036比旧5827.75偏左约14.55px。正在仅修测试driver原预算内真实A/D停车，不改生产冲量/法线/地图/射程或落台守卫；W+D不会改变当前Boost方向，不作为修复。Main225124的独立只读复核另确认自然preBoss保存有六个不同合法残片ID、非legacyExempt，字节SHA与日志相符。目标ACTIVE，Explore整轮及正式迷雾/门禁/补六回返/实时录像仍待完成，无新媒体/付费/自动化/提交或发布。

最新增量（10月10日22:55）：修正新日志label后fresh Main `225124085-918f70bdeecb4930ad748b758c7f8293` actual/wrapper0、175145ms、stderr空、source/scenes/frozen12/protectedStable=true/guardFailure=null，分类KEYBOARD_ROUTE_V1_AUTOMATED_NOT_HUMAN。标题开始、同角色8遭遇全部击败、Boss、有支撑出口7341.5254/1079.9941、HP3、恢复0/0、Completed真实保存、保持D60帧只结算一次、artifact与自然exit0成立；不是真人/实时/视觉/全部目标完成，也不修复此前原生资源异常。庭院真实三次K相位和站位修订已在该完整Main成立，Explore尚未当前通过。label纯文本14项GREEN，原classifier SHA/regex/pin保持不变；旧224224仍FAIL。目标ACTIVE，准备完整Explore，正式雾/实体quota/补六回返/实时与归档录像待完成，无新媒体/付费/自动化/提交或发布。

最新增量（10月10日22:50）：Main `224224447-d017eba14e324b0b9c5e2f54b48bd0c6`实际engine0/180102ms、stderr空、保护稳定；庭院三次真实K分期打开重甲、9次有效攻击（含前RustShell）完成遭遇，HP3→3无新增伤害，随后Boss/有支撑出口/Completed及route artifact/runnerOK全部产出。但wrapper仍FAIL：新courtyard打印字段`fatal=False`（以及spacing的`error=`）触发原固定故障regex；96条拦截全属该producer，无其他拦截。仅内存替换这两个label后0条只是定位证据，历史case仍FAIL，不冒称整轮GREEN。新增label单测先实际RED，正在只rename新日志标签，不改原classifier/regex/pin/白名单/行为断言，再fresh Main/Explore。目标ACTIVE，完整功能/视觉/性能与录像尚未完成。

最新增量（10月10日22:34）：带分项诊断Main `223051083-87c5dcc1f2c1482d98072ec5cd0781e2` actual1/127489ms（非timeout）、保护稳定。cycle2实际Bell available/eligible/receiver都正确，Right解析却选中原生CheckpointFloor抓面（5946.5312/1116、LOStrue、非legacy），不是Bell5762.929/1073.9338；角色5738.677/1079.9338、HP3/恢复0。明确是当前测试站位的真实候选差异，不是遮挡/接收对象损坏；只准备普通键盘调整站位后重新验证同一Bell候选，不改生产角优先/射程/碰撞/数值。完整Main/Explore仍未通过，目标ACTIVE。

最新增量（10月10日22:26）：新Main `222155991-4491db5cab3241cabcb8e16966237482`不是超时：actual1/127658ms、源/场景/冻结12/用户保护稳定。庭院第一轮真实K→Exposed→三次J使Bell6→3；角色HP3/恢复0/fullCheckpointFloor。回甲后的第二次K准备在exact native target/LOS断言失败，尚未发出第二K；正在只读定位真实方向/候选差异，不放宽exact target/LOS或补造选择。52项键盘结果守卫、38项有限回返守卫及30项预算选择单测重新通过；不代表整关或性能通过。目标ACTIVE，正式雾仍隐藏，无新媒体/付费/自动化/提交/发布。

最新增量（10月10日22:18）：Explore `220647884-618ac13cf1ce42d49d6401d17ab90227` actual/wrapper1、源/场景/冻结12/用户保护稳定；真实开场K已精确命中Bell、Latched/Released与随后独立Exposed成立，J造成3点伤害，之后测试driver仍打Armor并耗能，physics12060同角色HP1→0（恢复前）。此为相位操作证据，不加血或改敌人，正在只改测试为Exposed才J、护甲恢复且Idle再K，保留原900物理帧。两次Main180s超时后，只对exact默认完整纯键盘Main将OS诊断上限调240s，Explore360s不变；AST实际RED（240000期望/180000实际）→GREEN30项，大小写/重复/附加参数、build/局部均仍180s。所有物理预算/生命/存档/自然exit0与artifact守卫不变，不是性能修复或当前整关GREEN。目标沿用ACTIVE，无新媒体、付费、自动化、提交或发布。

最新增量（10月10日22:06）：带收尾计时的fresh Main `220053812-f23c992dd75e456e8f597102e151e780`再次180s timeout、wrapper1；源/场景/冻结12/用户保护稳定、stderr空。终止于BossHP1、physics10469，只有RUN_TASK10_BEGIN而没有RETURN，因此本次尚在路线执行，不是收尾写盘卡死；不据此归因具体生产成本或主机。自有PID52148两次采样：22:02:29 CPU80.609s、22:04:06 CPU180.109s，StartTime22:01:09；这不是实时帧率数据。预算未扩大，两次超时仍FAIL，不能以旧Main通过替代当前版本通过。现在只跑原360s Explore验证庭院opening K，完整目标仍ACTIVE。

最新增量（10月10日22:00）：雾上攻击提示新增一次性多source提交与可复用绘制副本，保持每source真实范围/同world/screen/native LOS筛选。两个真实Sentry同时预告、单独取消第二个、真实Active弹体7px轮廓及旧预告退场实际GPU通过；缺batch API的215402实际RED后，215547 GREEN。故意改成last-source-wins的215712被第一提示像素检出，精确恢复overlay SHA `1248B56DE3AD870D64317EAA19B7BE273B9B8ED761A15CD87EFCD0B88BFDF028`后215829再次GREEN（actual0/stderr空、源/用户稳定）。仅独立512场景两同类Sentry，原1280/1920单提示回归保留；不是所有敌人/正式层/性能验收，正式雾仍hidden。庭院opening K和收尾有限单调计时已写入测试driver，待fresh Main/Explore，不改生产战斗或预算。目标ACTIVE。

最新增量（10月10日21:48）：当前设置/减雾接线版本Main `213550100-848a5e5f850741ac962c0ec77f550c64`被180秒墙钟上限终止，wrapper1/UNEXPECTED_FAILED；源/场景/冻结12/用户保护稳定、stderr空。日志确有同角色HP2/恢复0、Boss击败、有支撑出口、Completed保存与D保持60帧只结算一次、键盘输入/音频排空/持久化守卫，但尚无route artifact、runner成功标记和自然exit0，**整轮不算通过**。旧192736同约10915物理帧在146022ms退出，不能把10920/60直接当此次墙钟原因，正在核对收尾；未扩大Main预算或用重试声称修复。庭院仅准备键盘测试driver的一次真实K打断与有限相位/J消费诊断，不修改敌人/能量/地图。多威胁雾上提示测试开始，正式雾仍隐藏。沿用既有ACTIVE目标，无重复目标、自动化、付费、媒体、提交或发布。

最新增量（10月10日21:35）：fresh Explore `212901693-4dee084cf7f748918c073e8320100057` source/scenes/frozen12/protectedStable=true、actual/wrapper1。全部捷径双向与六side再次通过，神殿控制4780/Right实际88/90walk、3/12settle、fresh neutral/exact control/真实K通过；后庭院RustShell击败并单普通jump完整落CheckpointFloor，HP1。紧接BellBackLaborer近距31px、energy.479开战，frame202受击HP0→自动恢复营地，随后adjacent-sector断言失败；**死亡是先行事实，恢复断言不是致死根因**，尚不认定游戏机制或能量问题。正在只读核对实际attack/shield输入和敌人相位，并另跑当前版本Main；没有加血/改数值/跳敌/取消守卫。完整Explore、正式迷雾可读性/门禁/补六/实时录像仍未完成，目标ACTIVE。

最新增量（10月10日21:28）：ReducedFog已接到实际雾强度。GPU先实际RED `212029-642-b64f30647d7244d7be515a84ab436155`，仅Canvas最终alpha候选减半后`212221-140-cf2cfe57cf314752ba5d812e29744377`原10组加新减雾组全部GREEN；距离/光束半径不扩大、原生16px墙与旧快照仍遮挡，同实例true→false复原。正式controller接线另有实际RED `212339-747-344851536b4a4c9c95a3ff492d225235`→GREEN `212551-547-18db857ebd9a429a9c86a39575c8a105`：每帧读取最新Flow设置、四种ReduceFlashes组合独立、新native快照和暂停/复活/Override回归通过。上述case actual与wrapper退出均符合预期、stderr除RED单一断言外为空、源/用户保护稳定；正式雾仍隐藏，减半是可调初值，非正式关卡像素/性能/最终美术验收。完整Explore `211147798-a992a9eb7a4d4904b36271e872f23a00`已同角色HP1/恢复0完成捷径全部17台双向与六side，后在神殿机关测试driver自设NE站位的90帧预算处失败；仅把键盘测试站位改同台中央4780/Right，保留真实候选与全部预算/支撑/身份守卫，待fresh整关。目标ACTIVE，实体门禁/合法补六回返、其余保护提示、实时性能及归档录像仍待完成。无新媒体、付费、自动化、提交或发布。

最新增量（10月10日21:12）：完整Explore `210046319-3f442bae740e4f18b62295c0fd2a9895`实际失败在ShortcutStep2036预发射的候选断言，前3796/3636/2996/2676均已同角色HP1、恢复0完整落原台；源/场景/冻结12/用户保护稳定。2036在同一原始post干扰边界选中远AscentGrip7Grip，不改生产瞄准/射程/地图，仅加入第6处明确side/Boost测试路线。独立2036局部`211005-528-0ccabadb6e3a4be495765d092f58334a`actual0/stderr空/保护稳定，106/原300、单jump/K、HP5落原台；明确seed已开winch，非标题完整路线。准备再次fresh完整Explore。目标ACTIVE；设置菜单已保存但正式雾仍隐藏且renderer未读ReducedFog，门禁/补收集、实时性能及新录像尚待完成；无新媒体、付费、自动化或发布。

最新增量（10月10日21:00）：标题设置重置缺陷已取得真实RED→GREEN。实际加载主音量.42后点标题设置会还原默认值；现菜单使用Flow同步的完整独立设置副本，新增中英“降低迷雾”选项，并修正13行实际布局底部越界。最新`fog-20261010-205834-311-d9f9ea2513ff4fbfa946495570f25446`actual0/stderr空、源/用户稳定，覆盖真实标题/Back、两向开关保存、重开、重新实例化读取及独立ReduceFlashes/Guidance/音量保留；仅菜单与持久化通过，renderer尚未接该开关，正式雾保持隐藏。完整Explore `203802320-83e83ad3506b4af795d2be89e8d5880c`已同角色HP1通过原3796/3636侧抓跃出，后在2996的八向候选断言失败；不改地图/射程/运动，2996加入第5处原平台side/Boost测试路线。2996/2676/1716独立局部fixture均完整落原台（106/110/110，原300预算、单普通jump/K/HP5保持），明确seed已开捷径，不是完整路线或原生错误修复。现准备fresh完整Explore；目标ACTIVE，实体门禁/完整补收集、其余雾提示/renderer设置、实时性能与归档录像尚待完成，无新媒体/付费/自动化/发布。以下按时间保留历史。

最新增量（10月10日19:54）：高分辨率攻击保护实测RED→GREEN：`194957-491-6800ff62d69e4e638c5d2dc1fee50a36`在1920物理/1280逻辑画布中错误剔除屏内来源，实际像素仍被雾压暗；只将屏内判断统一为逻辑canvas坐标后，`195054-306-ebe5a6de9a944ee38f4dc6ff4b4ae466`原三组及1280/1920两组实际GPU均通过，actual0/stderr空、源/用户数据稳定。正式雾仍隐藏，未证明所有分辨率/多威胁/Active攻击。四处捷径回返的测试改用现有原平台侧抓＋附着Space跃出，地图/射程/运动数值未改，独立静态复核后待完整Explore实跑。Explore测试进程有限墙钟上限仅对默认完整纯键盘探索从180改360秒，原300/24物理帧和所有生命/支撑/输入守卫不变；12项精确选择测试及52/38项守卫通过，不是游戏耗时或性能提升。目标ACTIVE，无新媒体/付费/自动化/发布。

最新增量（10月10日19:33）：新攻击绘制后端与隐藏迷雾controller版本的fresh Main `192736918-bc87fba3d35b4e4fabda9099974040cf`实际engine/wrapper0、stderr空、源/场景/原冻结12/用户保护stable=true。真实从标题、同一角色完成8组遭遇、Boss与有完整地面支撑的出口，终点7341.5254/1079.9941、HP2、恢复0/0、Completed实际保存、保持D60帧只结算一次。自动化而非真人/实时/视觉验收，fullGoalAccepted=false；不消除之前GCHandle失败，完整Explore及门禁/其他雾提示仍未验收。独立静态review确认普通queued世界预告绘制语义未变；发现高分辨率屏内资格存在physical/logical坐标域风险，正补真实Sentry专项，不将512像素结果泛化为所有设置。

最新增量（10月10日19:26）：Task3单一攻击提示切片实际GPU RED→GREEN。真实Sentry锁定预告、相机平移/非均匀缩放、16px原生墙遮挡、实际取消→None及detach/free同引用清理三组像素均通过；最终case `192404-792-99bcf3d65a824df2b2de2f22afaea93d` actual0/stderr空/源与用户数据稳定。故意移除LOS的`192316`被透墙金色像素断言检出，已精确恢复。修正的是FramePreDraw之后QueueRedraw延迟/提前清除问题：只给保护副本增加复用原几何的即时绘制，普通世界预告保留旧queued路径。正式迷雾仍隐藏，多威胁/Active覆盖、抓爪/落点/边缘/残片提示及性能录像仍未验收。降低迷雾的数据兼容亦实际RED→GREEN（191338→191455），旧音量/语言保留、两种开关独立存取；菜单/实际雾强度尚未接通，不能称完整设置完成。回返其余阶梯的八向抓点与碰撞净空存在冲突，正在核对合法替代地形路线，未放宽守卫或改地图。目标ACTIVE；非整关完成/发布，无新媒体、付费生成或自动化。

最新增量（10月10日18:59）：Explore fresh case `185444548-f97c4155bb0a43c9ae27123be59de60e`实跑证明首段回升修订有效：3/16同tick起跳观察、真实NE＋K命中原ShortcutStep3956Grip，95/300等待内松爪并完整落台(4162.065,3919.994)，HP1/恢复0保持。接下来ShortcutStep3796的通用helper仍按旧鼠标路线站位，八向候选先命中LiftEntryFloor，actual1/UNEXPECTED_FAILED；源/场景及用户保护稳定。正在统一核对余下回返阶梯的八向可达性，不改地图/射程、不强制目标。完整Explore、补收集门禁与正式迷雾验收仍未完成，目标ACTIVE。

最新增量（10月10日18:56）：Explore `183851222-7847a101f22548d3b0df59aacb4f8886`实际exit1，源/场景与用户保护稳定。首段回升已找到真实NE原抓点的安全窗口（净空12.74px），但PhysicsFrame信号内派发K导致按下边沿下一tick才可读、D/W先移动，测试在真正Aim前提前拒绝。仅FirstAscent测试改为同tick物理处理后观察/派发，并硬断言无额外physics tick；射程/真实first-hit/8px净空/±2与16/32/300预算不变。Main测试SHA `573E3B8F60C10E167FBF10287929D960EEB36803505120D3C1233EC01EB1EDED`，尚待fresh Explore，不能声称修复或整关通过。正式迷雾保持隐藏，目标ACTIVE。

最新增量（10月10日18:38）：实际保存场景的迷雾controller取得行为RED→GREEN：`183225-701-41b044f2d2334acaba3b2d854f142235`先因缺controller失败；C#父builder加入根级layer1隐藏canvas/controller后，`183701-156-3f9c66b83b3d4359842fc5decb251af9`实际exit0/stderr空/源和用户数据稳定。真实Formal替换与Profile Override、同帧灯位和相机投影、native门快照更新、Title/暂停/恢复冻结与复活后刷新均通过，observer异常0；正式雾仍关闭，非视觉验收。Explore首段新候选按真实射线安全选发射时机，尚待完整复跑。实体门禁/完整补收集、雾上预警/设置、整关与录像/实时性能仍待完成，目标ACTIVE。

最新增量（10月10日18:25）：探索case `181756315-c6a83d896b2e4b2eb8f052ce088dd57f`再次通过捷径底部，但首段回升的真实八向NE解析选择了另一处较高的LiftAccessStepOne，而非旧指针走法的ShortcutStep3956Grip。实际断言失败exit1，源/场景及用户保护稳定；正在核对合法键盘路线，不强制命中或放松支撑/生命/恢复要求。正式迷雾controller的保存场景集成测试并行编写中；关键提示层未就绪前正式雾保持关闭。目标继续ACTIVE，完整Explore及整体验收仍未完成。

最新增量（10月10日18:06）：Explore新case`180232425-44b305505a4c4701a7d582fc96fa70e3`在同一HP1角色上完成捷径底部两次真实neutralRight＋K控制及两次普通跳跃，唯一应用/无牵引/位移0/恢复0/完整支撑成立，每次控制43/原120帧；旧E/LMB仅保留legacy分支。随后上升首段在真实斜向采样时被旧MoveAxis/Aim零契约拒绝，actual1/UNEXPECTED_FAILED，保护stable=true/guardFailure=null；当前只迁移该测试契约，**完整Explore仍未GREEN**。资源诊断wrapper的日志拼接漏错及保护失败后错误保留CLEAN已取得实际内存RED→GREEN，11组通过；原12/10/13守卫通过。这只修正测试分类，未修复原生GCHandle异常。目标ACTIVE，正式迷雾/门禁/完整补收集/实时录像仍待完成。

最新增量（10月10日17:51）：Explore真实键盘case`174349258-71a4c48c248740bbb561629950e6d177`通过Grip1–9共享原300帧上限（112/171/152/152/152/143/152/152/152），Grip3实际D站位准备19+6帧；之后通过上层分岔战斗和ShortcutStep1556→4116全部17处下行落台，HP1/恢复0至捷径底部。当前失败是bottom helper仍要求旧E/LMB采样，actual1且保护stable=true/guardFailure=null；整关仍未GREEN，正在迁移此测试输入验证，不改正式地形/数值。两个新单次离树加载诊断（placement-load175016、load-only175107）均无错误；它们只表示该窄边界未复现，**没有修复或排除前述原生GCHandle问题**。迷雾十组GPU结果保持前段范围，正式接入/提示/设置/性能/录像、实体门禁与完整补收集回返仍待完成。以下较早段落为历史状态。

最新增量（10月10日17:36）：独立迷雾GPU检查扩为10组，已实际通过同实例重入、非对角投影、真实1280/1920物理像素、深井/庭院/出口末行渐变、双原生墙体和异常投影保护；最后GREEN为`fog-20261010-173326-502-c542a115d9c14d78b27255b7f5281aa3`，CPU/native专项`173358-690-e9d300fa90d74a2ba12e116dc4e9350b`亦GREEN。四次故意破坏shader均被独立像素断言检出，恢复后shader SHA与原件一致。**正式场景未挂迷雾，提示保护/设置/实时性能/录像仍待完成**。后续键盘聚合173425出现原生GCHandle释放崩溃，未改源重跑173536通过；残片聚合173625虽然exit0/OK但有多次`Handle is not initialized`错误，wrapper判FAIL且未运行跨进程读回。两项原生资源生命周期问题原因尚未确认，不能用重试或OK标记抹去。原型聚合173654通过、原用户数据保护稳定；当前不是全套GREEN。Explore停在Grip3的站位准备正在复用原通用驱动修订，未改正式地形/抓爪数值。目标保持ACTIVE。

最新增量（10月10日17:10）：独立GPU迷雾显示组件已实际RED→GREEN（`fog-20261010-170926-859-567cdd108a5b4affa707bf7659324d12`）：RTX3060/Compatibility真实像素验证距离渐变、独立头灯位置、16px薄墙同时挡住角色清楚圈与光束、门新旧快照和真实相机平移/2倍缩放；源/场景/用户保护稳定。共享区域场重构后的CPU专项`171008-237-a1558a378dfd41d7b6153fc1a4fc13b3`也通过。组件尚未挂正式场景，提示层、减雾设置、高分辨率/实时性能和录像未验收；新增同节点重入回归正在验证。探索路线`171048579-ec051e6d65a041afa444d9d29d7d30db`又通过Grip2，准备35帧、完整转移171/原300帧、HP1/恢复0；停在Grip3发射前的平台侧面选点，仍不是整关GREEN。以下较早“最新”段落为历史状态。

最新增量（10月10日16:39）：Explore实际纯键盘通过沟槽出口（W+K，唯一激活、完整抓爪生命周期、无拉走/新增伤害），继而在原60帧内通过后落台→AscentGrip1抓取（自然等待21+站位33=54帧，HP1/恢复0）；当前停在AscentGrip2站位导致斜向先碰平台侧面的断言，**Explore仍未通关**。case为`route-result-keyboard-explore-20261010T163908624-43f58f10d9ad441ea566df231efe6854`，源/场景/用户保护稳定。迷雾CPU专项最新`fog-20261010-163759-301-4f55e2ed76d14e43ab9d0d3e444efb3e`实际exit0/三个OK/保护稳定；开始独立GPU像素fixture，正式画面尚未启用迷雾。以下较早“最新”段落为阶段历史，不能代替本段状态；门禁/完整回返、雾可读性、录像/实时性能仍待完成。

用户已要求设立目标并继续：完成已确认的纯键盘/Q环境交互、9件残片/最低6件门禁及Boss前头灯迷雾修订。以真实纯键盘Main/Explore从标题完成Boss与有支撑出口、旧存档保护、专项回归和已保存/审阅短录像为验收；不以十万付费人数作工程完成标准。先推进键盘输入，随后收集/门禁与迷雾，摔伤后的完整路线难度一并复测。目标已建立且ACTIVE，**不是修订已经完成**。现有工作树/脏改动保留，无付费生成、自动化或未经授权发布。阶段记录见`.superpowers/sdd/2026-10-07-keyboard-controls/progress.md`；下方10月7日设计阶段待审文字为历史状态。

10月10日10:47历史阶段（下述未通过/未实现仅指当时，最新结果见下一段）：J独立攻击、K方向抓取/再次取消、L防御、八向瞄准/可见候选与Q近距交互接口已接入；真实键盘局部测试及摔伤/自动牵引/松爪/原型回归通过。中英操作页、暂停、引导和场景触发提示已迁移；教学只认实际出刀/有效抓取/举盾，空槽不推进，旧进度可继续。普通举盾跳跃允许，空中再次空格仍不会普通二段跳。输入记录新增J/K/L/Q与来源版本，保留旧日志兼容。扩展guidance/四分辨率菜单和物理键driver专项GREEN为`keyboard-20261010-104735-637-54762b54acd9413fabee17ad88ff516d`。当时Main/Explore键盘整关未通过。Q仅为opt-in环境接口，未把旧抓爪机关改成Q。9残片目录/6件计数与schema4数据层、事务接触收集和真实计数HUD已接入（下述专项范围）；九处现场摆放已完成静态检查；当时物理门禁/回返和头灯雾尚未实现。原存档/设置/试玩记录独立复核与旧基线一致，但本轮原引擎日志发生轮转，详见阶段记录，不能声称整轮原日志未变。已录制约13.25秒隔离键盘运动证明（并非整关）；v001为当时源码，后续修改不回写历史。

最新实跑与数据状态（10月10日16:07）：纯键盘Main `150957102-d8388c39af4c4582960b27d48a2b3508`已从标题同一角色完成Boss与有完整支撑的安全出口；实际exit0，J68/K45/L48、10913采样、鼠标/legacy事件0，恢复0/0，终点HP2，Completed保存及单次结算成立。分类为自动化而非真人验收，operationCoverageComplete/fullGoalAccepted仍false；后续测试侧修订不自动算该新版重跑。Explore最新`160654216-17e5bda4ccdd44108a6c512e3619f5a8`通过冰谷、真实营地摔伤落地、齿轮和蒸汽战斗，HP1/恢复0到沟槽出口；因该段driver仍检查旧E/LMB失败，正在迁移真实K，**Explore未GREEN**。零残片回返fixture `150851013-c3594d772d934e499ad9de1670455a22`通过神殿→上层分岔七处完整支撑，HP5/恢复0，途中自然得到庭院1片；明确seed区域来程的BellChain/封印/捷径，**不是标题漏捡或完整补六回路证明**。这些case保护stable=true、guardFailure=null。迷雾16族纯模型、实际Lantern姿态getter和精确native矩形遮挡最新专项`fog-20261010-160542-270-1b581a99d6ee4731afb800b4ef90450a`已实际GREEN并独立复核，空间查询0；覆盖薄台、立柱、门提交/移动/旋转/流式激活等，**尚无shader/controller或正式雾画面**。九残片摆放/持久化最近专项GREEN仍为`fragments-20261010-135254-129-6dedb1c95bf54aed9d6f800cf49d2fb1`。实体门禁、完整补收集、雾渲染/可读性、Explore整关及录像/实时/真人体验仍未完成；中央准入模型未接正式门，避免在回返未证明时锁关。此前原生Handle异常未复现但原因未定。详见[Task3阶段记录](.superpowers/sdd/2026-10-07-keyboard-collection-fog/quest-fragments-task3-progress.md)和[迷雾阶段记录](.superpowers/sdd/2026-10-07-keyboard-collection-fog/headlamp-fog-progress.md)。以下按时间保留早期记录，不代表最新状态。

后续正常Main实跑已通过升降机：齿轮左行真实140帧8脉冲均挡住、钟背工匠与蛾怪抓取通过。`route-result-keyboard-main-20261010T112434443-82e2267f68ec4e69b3724079368ba207`实际单次K激活车厢，上升约520单位并完整落在AscentGrip2，HP2、恢复0/0未变；随后AscentGrip3实际选择平台边角而非上方立柱，未通过。当前修正测试临时方向键过早释放的采样时序，及Temple/Boss旧E/Q/LMB正向证据；不改正式目标排序/数值、不强行命中。新wrapper的52项独立负控已实跑通过，最新失败case保护清单稳定true，但这不是通关。**尚无纯键盘整关GREEN，收集与迷雾尚未实现**。这些后续测试没有新录像，不属于v001录制源码。

最新`route-result-keyboard-main-20261010T114923359-be6633fa65e246b092e62e8a4882e7ab`已验证AscentGrip3：真实NW+K保持到fresh采样，选到上方立柱并完整落台。前段可选战斗抓取现仅在真实八向候选为敌人时尝试，避免把地形误当敌人抓取；这一实跑的深渊/齿轮前段HP5未变。下一Grip4的45°轴先碰目标平台侧边，正在原台全支撑范围内调整真实发射站位；仍未通关。Temple/Boss输入证据已编译和独立静态复核，但尚未实跑到，不计行为通过。

本轮录像、原片/源码与核验日志已保存于[键盘操作v001归档](E:/OurGame/Knight/过程图稿与视频/2026-10-10-键盘操作-v001-局部实跑/README.md)：13.25秒隔离真实输入，抽帧可见候选、牵引和能量盾。整体视觉NEEDS_WORK；短Boost/攻击连贯度尚未视觉验收，不能替代正式整关试玩。

最新`120359471-5ac9dcaa4cc2434997c4ea6d05eafc1f`实跑通过Grip4/5的站位准备、真实抓取与完整落台（HP3/恢复0保持）。现停在Skitter：测试斜向盾按键带着角色向敌走，进入过近距离，敌人取消冲刺，故没有合法反击窗口。仅调整键盘测试的诱敌站位，正式敌人/地形/伤害不变；尚未验证修订，不计整关通过。case保护清单稳定true，9残片与迷雾仍待实现。

| 本轮表现/资产 | 状态 | 替换边界 |
| --- | --- | --- |
| 键盘抓爪目标菱形 | 复用HUD程序绘制，读同一权威候选；Aim/Windup连续性已测 | 不改锁定点/射程/物理模型；不是最终视觉 |
| Q环境候选提示/正式手动蒸汽阀 | Basin明确opt-in已生成保存；同候选中英动作提示、真实Q/原K与生命周期专项、完整Main/Explore回归通过；唯一提示已接正式layer2 | 可替换程序文字；本机512×384真实GPU字形/挡墙/层级/重入/死亡首帧通过；高分辨率Q排版、跨平台字体与最终视觉未验收，不代替道具使用，不给其他机关自动增加Q |
| 任务残片程序化菱形 | 9件已保存至五sector；Main自然6件、Explore支路6→7以及独立自然漏捡5→6回门→Boss/支撑出口均有原生自动化GREEN | 外观/状态标签与稳定ID、碰撞、保存分离；更广0/4件补收集、全部支路/反向边和最终视觉仍未完整验证，无付费生成 |
| HUD残片数量/缺额/旧准入提示 | 双语实际状态测试通过；尺寸/视觉尚待验收 | 来自目录交集真实计数，不把旧豁免显示成6件 |
| 头灯雾纯模型/真实灯位/World遮挡 | CPU/native、独立GPU和保存controller已接入；正式Playing条件显示 | 只读实际灯具和native矩形快照，双原点精确遮挡；未知几何fail-closed，不改角色动作/碰撞或射程，不等于正式视觉验收 |
| 头灯雾兼容canvas shader与RGBA32F几何/区域纹理 | 独立及正式环境GPU像素RED→GREEN，正式场景已条件显示 | 黑色程序化雾，可调外观；原生矩形精确双原点遮挡与只读区域场，不复制地图/碰撞，尚非最终视觉 |
| 头灯迷雾正式场景controller/根级layer1 | 正式GPU和保存生命周期通过；Playing、实际雾帧及九消费者有效可见时显示，否则明亮 | 只读Coordinator当前实际灯具/最新native遮挡及镜头投影；暂停/标题撤销，不把节点存在当全部提示验收 |
| 头灯真实键盘180度平滑/抓点HUD离场保护 | fixed60实际转向与snap/vectorLerp两个行为负控通过；真实W/K唯一native hit预览/锁定/取消、上雾cyan像素/层级负控及detached旧cue清除GREEN | 半转生产原有，本片新增覆盖；HUD仅读live依赖，无Input隐藏旧准星，不改牵引/攻击；非正式整包布局/全部提示/实时视觉验收 |
| 雾上层攻击提示保护副本/正式layer2 | 当前mounted五类敌人与arena当前Boss已接线；真实双Sentry/Moth GPU、保存场景Moth/Boss重置及排队删除当帧退场通过，源筛选负控检出 | 复用原攻击几何/同帧native遮挡，不扩大驱雾；正式雾条件显示，非全部敌人各相位像素/逐段遮挡/正式视觉或性能验收 |
| 近距残片小型提示/正式layer2 | saved父scene已接线；真实GPU近远/墙体/层级/卸载重入/真实一次拾取与新局恢复通过 | 程序化可替换菱形，240为可调候选；保存失败短条另列专项，不覆盖所有边缘遮挡或全部UI布局；正式雾条件显示 |
| 原残片保存失败文字/正式layer2 | 实际原Status文案/字体、FileShare锁盘失败与原接触重试，中英文完整GPU字形、saved消费者、缩放/遮挡/暂停/卸载撤销通过 | 可替换程序背景与排版，不改变失败/保存/重试事务；最近一个近距无遮挡来源独占显示，底缘距屏幕底120×resolutionScale为候选；正常速度完整UI布局与正式视觉未验收，无新图像或付费生成 |
| 真实静态落脚短边缘/正式layer2 | 原StartFloor与BridgeFarBank GPU、薄遮挡/薄支撑负控、保存关卡生命周期与fresh两路线GREEN | 借用原ReadableEdge颜色和实际native顶面；只支持当前配对的水平静态平台，620是候选显示预算，不扩抓爪/光照、不覆盖移动机关或保证落地无伤；正式雾条件显示 |
| 真实蒸汽预警/喷发边界/正式layer2 | 原Basin standalone及Trench两喷口GPU、保存controller五源接线、Disabled/Remove负控修复与fresh两路线GREEN | 可替换程序线条；仅显示原真实native矩形边内、同帧整笔画可见的预警/危险，不改变时序/伤害/安全窗；Frozen两源为保存接线验证而非GPU颜色验收，正式雾条件显示 |
| 原持续伤害区短边界/正式layer2 ShieldLane | 原saved Basin Area/native rectangle GPU、实际停机/监测独立负控、保存controller暂停/跨区卸载重挂与fresh两路线GREEN | 新display-only程序线条，620为显示候选上限；读取真实运行伤害源与native owner，不复制脉冲、伤害或安全窗；不是最终美术或正常速度视觉验收，正式雾条件显示 |
| 原压板门关闭线/五刻度/正式layer2 PressureGates | 原visual不可变paint与原gate倒计时共享；原saved GPU、保存controller、CPU/原门及最新两路线回归通过 | 不复制时钟，不改防夹/宽限/预警；不是新的HP危险语义或最终视觉，正式雾条件显示 |
| 原移动机关当前薄边/正式layer2 MovingBodies | 原升降台/活塞/摆锤自然PhysicsProcess的GPU、保存controller及最新两路线回归通过 | 逐native矩形面/旋转/镜像/owner offset与整笔画遮挡；中性轮廓，不把HazardActive当HP伤害或保证安全落地，正式雾条件显示 |
| 原活塞/摆锤提前预警/正式layer2 MovingBodies | 原自然Frame、固定sibling Polygon2D颜色/变换；真实GPU正负控、保存controller生命周期、六同版专项及fresh两路线通过 | 可替换原程序预警；逐小块普通native遮挡，不另造clock、不随摆锤body旋转、不套solid终端豁免；正式连续路线正常速度观感待验收，无新图像或付费 |
| 原门体当前薄边/正式layer2 Barriers | 八原saved来源GPU、保存controller及六同版专项/最新两路线通过；三片准入罩独立shape和长410×1930捷径已覆盖 | 可替换程序线条、620候选上限；只读当前碰撞，不复制开门条件/额度/存档；门原生解除后撤提示，非最终美术/真人或实时性能验收，正式雾条件显示 |
| 降低迷雾双语设置与显示接口 | 真实标题/Back持久化和13行布局、同实例实际GPU减雾、正式controller最新设置提交分别RED→GREEN | 最终alpha候选减半，不扩大半径/绕过遮挡；正式雾条件显示，未完成全部关键提示、界面缩放、正式视觉及性能验收 |

## 摔伤机制（2026-10-07；专项已接入）

沿用现有Godot运动/生命系统，新增按一次连续下降的最高点计算的落地伤害：600世界单位内安全，超过后每400单位增加1点伤害（向上取整），极高落差可致死。普通跳跃不伤；冲刺、空放机械爪或仅附着但未减速不能清除落差，真实抓爪制动/向上跃出可重新计算后续落差。暂停、重生/传送、重复站地及现有0.35秒受伤无敌规则已保留；护盾不吸收摔伤。

唯一明确缓冲点是长落深渊的 `AbyssSafeBottom`，新增绿色分节软垫表现，不改原640×40碰撞或普通接台。新增专项、原型聚合、自动牵引及松爪回归均通过；隔离实跑验证400/800/1200/1600/2400落差与实际保存深渊底部的约3024单位下降。**尚未重跑加入摔伤后的整关主线/探索路线、真人操作和最终难度；本节不是序章完成声明。** 运行与审查记录见[摔伤专项记录](docs/production/2026-10-07-fall-damage-v001.md)。无付费生成、版本发布或GitHub推送。

| 本轮资产/表现 | 状态 | 替换边界 |
| --- | --- | --- |
| 深渊底部绿色分节缓冲垫 | 程序化原型，已接入保存场景；v002隔离录像已抽帧审阅 | 外观可替换，显式soft_landing标记与原碰撞分离 |
| 高落差伤害反馈 | 复用现有受伤反馈；专项已通过 | 独立安全高度/每点伤害高度参数；摔倒/翻滚动作未新增 |

## 当前设计调整（2026-10-07；尚未实现）

以下保留摔伤专项开始前的历史设计阶段记录；其中“当前运行代码”“本轮没有运行”仅指10月7日当时。10月10日已推进输入/交互局部实现（见顶部），但收集/迷雾、内容深化及整关迁移未完成，不因专项通过而变成整关验收。

用户已确认[纯键盘/Q环境交互、9件残片/最低6件、Boss前头灯迷雾规格](docs/superpowers/specs/2026-10-07-keyboard-collection-headlamp-fog-design.md)；“拾取/使用道具”按键仍暂缓。已拆成[三个待审实施计划](docs/superpowers/plans/2026-10-07-prologue-revision-index.md)，涵盖旧存档兼容、最终Boss门禁和实际补收集回返。追加的敌人/机关增量及地形复杂度已整理为[内容深化候选规格](docs/superpowers/specs/2026-10-07-prologue-content-and-terrain-expansion-design.md)，具体7种/30个体、10类/16核心安装及四处空间重排尚待确认，不是已完成计数。静态现状为5种/22个有限敌人、8类/10套核心结构安装（部分待验收），钟链另计1套；地形仍有重复阶梯和未证明的回溯。当前运行代码依然使用鼠标模式，头灯尚无真实照明；本轮没有运行、增加美术资产、付费生成、打包或发布。以下已核验历史与未验收披风候选均保留。

| 本轮待制作资产/表现 | 当前状态 | 替换边界 |
| --- | --- | --- |
| 任务残片外观 | 尚未制作，计划程序化占位 | 不改变稳定ID与收集规则 |
| 头灯驱雾/雾层（早期计划对照） | 已有兼容shader/正式隐藏controller及独立GPU，当前状态以上表为准 | 只影响表现；完整关键提示/正式启用/性能和视觉验收仍待完成 |
| 悬壁/潜岩敌人与塌落/双态桥 | 候选规格，尚未制作 | 行为/预警/几何与可替换外观分离 |

## 最新核验（2026-10-04）

披风已实现“原动作连接点＋下摆分段随动”的候选，专项姿态/数值测试通过，但独立复核发现：绘制直边跨过固定区时仍会让领口附近轮廓偏移。因此**尚未接受此动作修订**，正在补实际轮廓边线回归并修正分段；正常主线录像暂缓，不能把测试通过写成视觉完成。修改前材料已独立核验留存（60项、12,810,448字节），见[披风修改前基线](E:/OurGame/Knight/过程图稿与视频/2026-10-04-披风动作-v001-修改前/README.md)。原操作、碰撞、地图、存档和付费生成边界不变；完整目标仍ACTIVE。以下为此前已核验进展。

Boss提示避让v001的限定源码、实际运行和原尺寸画面独立复核已通过；最终100项668,956,038字节已逐项校验留存。新增实际抬起装甲的避让范围，修复前专项测试准确失败、修复后通过，正常自动Main完成8组遭遇、Boss和有支撑出口。匹配的暴露画面中装甲完整露出、提示文字仍可见，不是简单隐藏。原片、20秒片段、新旧各4帧及复核记录保存在[Boss提示避让v001](E:/OurGame/Knight/过程图稿与视频/2026-10-04-Boss提示避让-v001/README.md)。整体美术仍需完善；不证明连续动作、声音、真人、实时或全量套件通过。下一项披风动作已完成源码链和修改前实际顺序帧检查：固定轮廓整片随动，缺乏独立下摆变化；正在测试肩部固定、下摆滞后的显示层修订。修改前原片引用、三段剪辑及42张原尺寸帧已保存于[披风基线](E:/OurGame/Knight/过程图稿与视频/2026-10-04-披风动作-v001-修改前/README.md)，尚无新动作验收。完整目标ACTIVE。[实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task27-boss-guidance-actual-20261004.md)。

角色材质v001已进入实际画面：统一近远侧骨瓷底色，披风增加连续布面与宽褶皱，未改轮廓、动作、碰撞或操作。六项现有姿态契约和正常自动Main（8组遭遇、Boss、安全出口）通过，限定实际证据及原尺寸画面已独立复核。布面/远侧肢体局部改善，**整体角色仍偏原型**；新增胸甲大多被披风遮住，不能把源码增加当成显著视觉提升。角色/武器轮廓、布料动态、敌人和战斗提示覆盖仍待完善；未达重大体验版本或最终美术。原片、12秒预览、新旧各12张原尺寸画面已保存于[角色材质v001过程目录](E:/OurGame/Knight/过程图稿与视频/2026-10-04-角色材质-v001-布面与骨瓷/README.md)，最终102项672,494,252字节逐项路径/大小/SHA256一致。固定60Vulkan录制不是实时、真人或连续视听验收。下一处已定位的可读性问题是Boss抬起装甲未纳入提示卡避让范围；当前仅只读定位，尚未修复，不归因于此次材质改动。完整目标ACTIVE。[运行记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task26-player-material-actual-20261004.md)；[独立复核](.superpowers/sdd/2026-09-09-continuous-canyon-world/task26-player-material-actual-review-20261004.md)。

入口承托v007已完成限定实际与独立复核：台面与已有斜撑相接，原尺寸对照显示悬空感局部减少；碰撞、抓取面和路线未改。限定结构回归及正常自动Main通过8组遭遇、Boss和安全实体出口，结算后右移60帧位置稳定。原片10494帧、12秒预览720帧完整解码，原片/新旧各8帧/源码与记录保存于[v007过程目录](E:/OurGame/Knight/过程图稿与视频/2026-10-04-入口承托-v007-连接修订/README.md)，最终84项668,954,395字节逐项核对路径/大小/SHA256一致。**整体视觉仍需完善**：阶梯节奏、平面背景、角色/敌人和UI仍偏原型；本轮只是抽帧审阅，不是连续视听、真人或实时验收。下一步转向角色身体与披风材质、整体构图；只读核对已确认实际角色使用绘制层而非旧图集，尚未据此改动身份、姿态或生成付费素材。完整目标ACTIVE。[本轮记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task25-support-contact-actual-20261004.md)；[独立复核](.superpowers/sdd/2026-09-09-continuous-canyon-world/task25-support-contact-actual-review-20261004.md)。

诊断日志延后输出已完成一次正常窗口实际主线：同一角色通过8组遭遇、Boss和有支撑出口，188条护盾观测完整保留，11项延后输出与16项计时测试通过；限定实际证据已独立复核。实时仍有245个间隔超过33.3毫秒、9个超过50毫秒，不能称作稳定流畅，也不能把测试工具开销的减少当作正式游戏性能提升。入口已有承托与台面的可见连接修订已进入源码审查，不改变碰撞、抓取面和路线，实际视觉尚未验收。完整目标ACTIVE，整体美术、角色/敌人动作、UI/声音、重复路线、真人及Windows交付仍未验收。[本次实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task11-shield-deferred-output-actual-20261004.md)。以下保留此前状态。

谷底长帧已找到一项实测开销：正常窗口主线完成8组遭遇、Boss及安全出口，新增12项测试通过；最慢84.819毫秒的区间里，测试日志集中输出占64.218毫秒。**这是测试工具的开销，不是已修复游戏卡顿**；全程仍有274个间隔超过33.3毫秒、10个超过50毫秒，其他原因待查。611项源码、42场景、109项原用户数据及12项冻结约束前后未变。限定实际证据已独立复核；下一步把诊断输出延后到测量结束，同时完整保留记录，再核验正常运行。完整目标保持ACTIVE。[实测及时间对齐](.superpowers/sdd/2026-09-09-continuous-canyon-world/task11-shield-end-timing-actual-20261004.md)。下方较早“像素偏差”指Godot世界/局部坐标距离，并非独立测得的物理屏幕像素。

抓链坐标观测已完成一次实际正常主线：新增13项测试通过，同一角色从标题完成8组遭遇、Boss和有支撑出口，结算后右移60帧稳定。最后抓链保留36条记录，无丢弃/记录错误，采样偏差约0.11像素；**之前约553像素的偏差未复现，不能算已修复**。完整实时数据仍有228个回调间隔超过33.3毫秒，最长约81毫秒；尚不能称作稳定流畅，也不是真人或美术验收。609项源码、42场景、109原用户数据和12项冻结约束前后未变。下一步针对反复出现长帧的谷底区间定位，不猜测性改抓取判定；完整目标保持ACTIVE。[本轮实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task11-pointer-trace-actual-20261004.md)。以下失败与更早基线均保留，不被本次通过覆盖。

长帧定位记录已接入，新增12项边界测试实际通过；但最新带记录的主线在Boss最后一次抓链时失败，未通关。日志显示采样鼠标偏离链条并选中了其他地形，原因仍待区分，不能认定为真实玩家操作问题。部分诊断数据已保存且与原采样计数一致：最严重间隔出现在神殿、谷底、上升段等位置，也包括失败后的卸载；不能直接归因于场景加载。下一步核对自动发出的瞄准坐标与游戏实际采样，不放宽抓链判定或调整正式玩法来掩盖失败。原有成功基线和失盾失败均保留；完整目标ACTIVE。[本次失败与部分记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task11-longframe-context-actual-20261004.md)。以下为此前已核验基线。

当前正常主线已补上一次非录像实时实跑：默认OpenGL、1280×720，同一角色从标题开局完成8组遭遇、Boss和有支撑出口，结算后持续右移60帧稳定，原存档及源码/场景未变。12,290个有效回调间隔的p95为27.029毫秒、p99为37.380毫秒，228个超过33.3毫秒、9个超过50毫秒；**还不能称作稳定流畅，长帧所在场景及原因尚未定位**。本次没有观察到失焦，构建零警告/错误。这是自动路线的本机测量，不是真人手感或最终画面验收；原护盾失效仍未解决。下一步补充有界长帧定位证据，再做针对性优化。完整目标仍ACTIVE，美术/动作/声音、生命周期、重复路线、真人及Windows交付继续保留。[实时实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task11-realtime-main-actual-20261004.md)。以下保留历史。

护盾诊断新实跑已完成正常主线：188条有界观测无丢弃或错误，护盾通道没有失盾、损血或复活，同一角色完成8组遭遇、Boss及有支撑出口，结算后右移60帧稳定；源码和实际证据均已独立复核。**上一次失盾未复现，原因未明，不能算已修复**，没有修改正式玩法参数或覆盖v006失败记录。下一步把现有实时帧采样接到当前正常路线，在非录像窗口实测；固定帧率录像不能证明流畅度。整体美术、动作/声音、生命周期/重复路线、真人体验和Windows交付仍未完成，目标ACTIVE。[诊断实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task24-shield-observation-actual-20261004.md)。以下为保留历史。

入口→裂谷v006石材分节已实装，原尺寸对照显示短石台接缝/端面更清楚，光滑长条感有局部改善；**整体美术仍NEEDS_WORK，本次正常Main在护盾通道失败，未完成Boss/出口**。已确认角色先失去护盾和前进状态，随后受伤死亡、回到检查点，最初关闭原因尚未记录；正在增加有界只读状态观测，不改护盾数值、地图或路线断言。构建和限定结构回归通过不能抵消路线失败。原片、12秒入口预览、22张新旧/故障原尺寸帧及记录共97项125479322字节已保存并逐项校验，见[v006过程目录](E:/OurGame/Knight/过程图稿与视频/2026-10-04-入口与裂谷-v006-石材分节/README.md)及[实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task24-material-fix1-actual-20261004.md)。完整目标仍ACTIVE，视听精修、生命周期/重复路线、真人/实时与Windows交付仍未完成。以下v005及更早结果为历史，不代表当前整关通过。

入口→裂谷光色材质v005已进入实际运行并保存前后对照：断钟/斜向遗迹暖亮面和冷色凹处有局部纵深改善，但平台仍是重复光滑长条，部分粗糙材质感退步，**整体视觉仍NEEDS_WORK**。独立源码检查、两区场景重建及限定结构测试通过；同一角色正常自动Main完成8组遭遇、Boss及有支撑出口，结算后右移60帧稳定。原片、12秒短片和新旧各8张原尺寸帧已留存于[v005过程目录](E:/OurGame/Knight/过程图稿与视频/2026-10-04-入口与裂谷-v005-光色材质/README.md)，原片/短片完整解码；画面审阅是抽帧比较，非连续视听或真人/实时验收。下一步修选定台面的材质区分和已有承托连接，不扩大平滑渐变，不改真实碰撞/抓面/平台边缘；角色/敌人、动作/UI/声音、重复路线与Windows交付仍未完成。不接入UniMate、不新增付费生成或发布。[本轮记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task24-depth-staging-actual-20261004.md)。完整序章目标仍ACTIVE。

开场教学专项在修正旧地图测试布置后已实际通过：使用当前真实平台与抓取面，验证完整卡片、暂停/缩放/语言、Q→E切换、真实左键抓取及回到原出生支撑面。修订仅涉及测试，不是新增玩法或“角色物理修复”；独立源码检查与实际证据复核均通过。旧失败证据保留。下一步回到实景的光色、材质与远近层次，整关视觉、动作/声音、真人/实时和Windows交付仍未完成，完整目标ACTIVE。[专项实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task22-layout2-fix4-actual-20261004.md)。

以下保留早先运行与诊断的历史状态；其中专项失败已由上方新案例推进，不覆盖原始失败日志。

新正常开局自动主线已完成8组遭遇、Boss与安全实体出口，结算后持续右移60帧稳定。实景对照确认开场重复目标条消失，完整教学卡和Q/E、装备栏保留；战斗提示条仍显示。这只是局部UI清理，场景平涂、重复平台、角色/敌人及动作仍需精修，不是整体视觉完成。原片、10秒短片和12张原尺寸新旧对照已保存在[开场教学UI v001](E:/OurGame/Knight/过程图稿与视频/2026-10-04-开场教学UI-v001-实景核验/README.md)。下述专项测试失败继续保留，正常主线通过不能替代它；完整目标ACTIVE。

开场重复教学UI的显示层修订已通过限定源码复审。实际检查通过完整开场卡片、暂停等生命周期、真实全局缩放矩阵，以及受控敌人战斗提示/离场恢复；后续下落区用例的“区域、落地、无目标标记”组合前提失败，具体失败项未记录，整组尚未通过。已停止追加猜测性修补，另行独立核查用例与当前地图布局；同时准备正常路线实景核验，不用录像替代失败测试。Q/E、装备栏、任务进度和抓取/战斗提示必须保留；完整目标保持进行中。[本项实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task22-early-lesson-actual-20261004.md)。

完整序章目标继续 ACTIVE。本轮只增加了捷径2996台阶的只读诊断，没有修改输入、地形、跳跃或难度。正常开局的自动探索实跑完成了往返捷径、神殿补给、Boss击杀与有支撑的实体出口，结算后持续右移60帧仍稳定；源码/场景与109项原用户数据均未改变。目标台阶此次正常通过，但上一版的停滞原因仍未证实，不能称作修复。独立源码及限定实际证据复核均通过；未获得真人、实时性能、完整操作覆盖或正式交付验收。

原片、3秒台阶短片与8张原尺寸诊断帧保存在[捷径落台观测 v001](E:/OurGame/Knight/过程图稿与视频/2026-10-04-捷径落台观测-v001/README.md)，均为本地过程证据，不是新美术版本。抽查仍见重复悬空平台、平涂背景与原型式角色/UI；下一步继续开场提示精简与场景、动作精修，并保留未复现卡点的复验项。没有恢复自动化、付费生成或发布版本。[本轮详细记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task23-shortcut2996-actual-20261003.md)。以下保留历史。

## 当前进度与视觉方向（2026-10-03）

入口→裂谷v004已移除覆盖美术的五块区域色罩，保留引导节点、真实地形与碰撞；构建器/打包场景专项检查通过，同机位原尺寸对照及独立审阅确认原竖向色差接缝在已检查画面中消失。但这次正常探索在捷径下行的ShortcutStep2996落台检查失败，录制止于约2分23秒，未完成Boss/出口，不沿用上一版通关结论。失败录像、18秒入口预览、来源与对照均已保存；原片/预览完整解码通过。下一步先观察该处输入、速度和碰撞，定位边缘停滞原因，不放宽落台判定；随后精简重复教学UI，继续场景/动作/声音精修。整体画面仍NEEDS_WORK，真人/实时、完整回归和Windows候选未完成；完整目标ACTIVE，自动化仍取消，无新增付费生成或发布。[本轮实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task21-region-tint-actual-20261003.md)；[v004预览及失败原片存档](E:/OurGame/Knight/过程图稿与视频/2026-10-03-入口与裂谷-v004-去除区域色罩/README.md)。以下为历史记录，不代表当前版本通关。

入口→裂谷构图v003已实装并完成实际运行检查：两区重复装饰立柱减少，岩壁/支撑轮廓调整；同一角色再次从正常开局完成探索、补给、Boss和安全出口，构建器及打包场景的两组结构检查通过。原始录像、18秒预览、完整预览和原尺寸对照均已保存，三份视频完整解码通过。实际画面审阅仍是“局部改善、整体NEEDS_WORK”：区域边缘的竖向明暗线没有消失，已进一步定位到覆盖在美术上方的整块区域色罩，下一步修正并重新对照，不宣称接缝已修好。角色/敌人动作、平台单调、教学UI、声音、真人与实时体验、Windows交付仍未完成；完整目标ACTIVE，不恢复自动化，不新增付费生成或发布。[本轮实际记录与下一项](.superpowers/sdd/2026-09-09-continuous-canyon-world/task20-entry-rift-actual-20261003.md)；[v003预览、来源索引与保存目录](E:/OurGame/Knight/过程图稿与视频/2026-10-03-入口与裂谷-v003-连续构图/README.md)。以下保留历史，不代表重复完成。

入口实景构图已改出一版：重复门架/吊篮收为一个断钟地标，加入围合岩壁、斜向遗迹和台阶下支撑；碰撞、抓取与正式操作数值未改。新源码独立检查通过；同一角色从正常开局完成探索、补给、Boss和安全实体出口，硬件录像正常结束（约4分19.5秒自动路线），两组材质/美术分离回归通过。原片、12秒入口预览、全片及对照截图已保存。画面抽查结论仍是“局部构图改善、整体视觉NEEDS_WORK”：大块折面、重复柱体/阶梯、区域竖向分界、角色动作与教学UI仍需精修；真人、连续运动/听感、实时性能和Windows交付未完成。完整目标ACTIVE，不恢复自动化，不使用新付费生成。[实际记录及证据边界](.superpowers/sdd/2026-09-09-continuous-canyon-world/task19-entrance-paint-actual-20261003.md)；[过程预览与保存目录](E:/OurGame/Knight/过程图稿与视频/2026-10-03-峡谷入口-v002-实景构图/README.md)。下方保留历史。

本轮过程材料已完成独立归档核验：33项原片、原始记录、截图和预览的路径、大小、哈希及来源副本均一致。约99秒片段是录制超时后的诊断预览，不是完整通关片；六帧构图评审仍为NEEDS_WORK，动作、听感和正式游玩验收未通过。完整目标继续ACTIVE，下一步回到实际场景精修，不将归档或自动测试通过算作美术提升。

实际画面检查已开始：硬件NVIDIA Vulkan录制初始化通过；完整探索录制在180秒上限停止，只留下前约99秒，不能算完整通关视频。原片、预览和六张原尺寸抽帧已保存至过程图稿目录并保留来源索引。看过六帧后，当前视觉仍判定NEEDS_WORK：重复横板/矩形柱体、大片空背景、薄弱的角色战斗剪影和占空间的文字提示仍明显。下一步优先实景环境层次、遗迹支撑/地标、角色与攻击辨识度及UI，不再用自动测试通过代表画面提升。动作连续性、听感、默认OpenGL与本次Vulkan画面一致性仍待验收。源码未因此改动，完整目标ACTIVE。[本轮录制与审阅边界](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-vulkan-movie-actual-20261003.md)；[已归档诊断预览及索引](E:/OurGame/Knight/过程图稿与视频/2026-10-03-正常探索-v001-录制超时诊断/README.md)。

神殿补给支路已接入并通过正常开局的完整探索验证：同一角色以1点生命到达，真实抓取地形、空格跃出、落稳踩板开门，领取一次补给恢复到3点，返回原路线后完成Boss与安全出口；普通跳跃次数不增加、无复活。未走支路的主线回归也通过，源码和两条新实跑均已独立复核，正式玩法与数值未因测试而放宽。这一轮补齐的是整合测试，不是新增美术或最终手感验收。接下来以正常尺寸运动画面检查场景、技能、UI与声音；先单独验证项目要求的硬件Vulkan录像路径，不擅自改变项目默认渲染器。完整操作/生命周期、当前版本重复路线、真人/实时表现和Windows候选仍未完成。完整目标ACTIVE，自动化不恢复，无新增付费生成或发布。[本轮真实记录与验收边界](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-normal-supply-actual-20261003.md)。下方保留历史。

最新实跑：当前主线与探索线都已从正常开局完成Boss、实体出口和结算后60帧稳定检查，原通关数据契约及外层验证全部通过，并保存了非合成通关记录。已修复自动试玩漏采遭遇记录和日志误判；3975项分类断言仍检查真实故障，不改游戏难度或旧失败记录。独立复核已确认源码及这两次新路线运行。这些是自动运行结果，不代表真人手感、画面或成品验收。本轮没有新增画面素材；下一项是将神殿补给支路接入完整探索流程，之后继续场景、动作、UI和声音精修。完整操作覆盖、正常窗口/真人试玩、实时表现和Windows交付仍未完成。目标ACTIVE，自动化不恢复。[本轮真实记录与验收边界](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-log-classifier-actual-20261003.md)。以下保留历史。

最新正常开局实跑：当前地图的主线与探索线都已实际击败Boss、走到有完整地面支撑的实体出口，并在结算后持续向右60帧保持稳定。但最终数据验收发现“已击败遭遇”缺少对应“已激活遭遇”记录，两条测试仍判失败，没有写入成功结果。独立复核确认失败被正确保留、真实存档保护未变；正在定位自动试玩的记录采样差异，尚未据此修改游戏逻辑。本轮没有新增美术或交付包，完整操作覆盖、正常窗口/真人体验、视听精修和Windows候选仍未完成。完整目标ACTIVE，自动化不恢复。[当前两路线证据与局限](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-route-result-actual-20261003.md)。以下保留历史。

已修复神殿中的机械爪操作提示：真实抓住墙顶/横梁后，提示切换为“A/D可选摆荡、空格跃出并松爪、Q/右键取消”；松爪后恢复原任务指引，不改正式牵引参数。新增检查先复现失败，再修复通过；实际场景中同一角色以真实按键完成一次抓取与空格释放，界面文字随状态更新，保持5点生命、无普通二段跳。旧三种时机的跃出/落台回归和完整旧提示检查也再次通过，源码及运行记录均经独立复核。这是局部无窗口运行证据，不等于最终画面、真人手感或整关交付。下一步回到正常开局的完整路线与Boss/出口收尾验证；完整目标仍ACTIVE，美术动效、视听精修与Windows候选仍未完成，自动化不恢复，无新增付费生成或新展示媒体。[本轮修复与实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-temple-guidance-actual-20261003.md)。以下保留历史。

新增机械爪“向右＋空格跃出”真实按键检查已通过：首次到达后立即、12帧及24帧三种预定时机，都完整落稳上层、打开压板门、进入补给室并安全返回；每次只有一次跃出，没有普通二段跳、冲刺或生命损失。旧闭链/普通取消路线也再次通过，未改正式牵引参数或地形。源码及两轮实际记录均已独立复核通过。这是固定帧率局部物理证据，不是画面、真人手感或整关交付；接下来核对提示是否清楚传达已验证的操作。完整目标仍ACTIVE，未恢复自动化、生成付费图或覆盖旧存档。[实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-space-exit-actual-20261003.md)。以下是保留历史。

机械爪体验诊断补充：下方记录的“约1.4秒取消等待”是自动测试等待自定落台姿态的时间，不能据此认定游戏有1.4秒输入延迟。实际横移已立即响应；是否需要修改摆荡手感仍待验证。现准备在不改游戏参数、不放宽旧测试的前提下，实跑教学推荐的“到达后向右＋空格跃出”，比较三种预先固定的放手时机；当前尚无新运行结果，不宣称操作已经更顺滑。完整目标继续ACTIVE。

神殿补给支路的真实运行回归已通过：实际开链与检查点、三次机械爪攀爬和补给入室均跑通；满血保留补给，受伤后只恢复一次（2→4，HUD数据同步），重复进入不回血；占用时重置门不夹人，离开后正常关闭；死亡回到神殿检查点，完整卸载再继续游戏后补给仍已领取。原闭链物理通路也再次通过，源码和两轮实际记录均已独立复核通过。本轮补齐测试覆盖，没有新增美术或调整正式数值；这是区域起点/预置上游条件/合成受伤的固定帧实跑，同进程重新加载不等于跨进程或整关真人验收。约1.4秒取消等待、更多生命周期组合、正常窗口视听精修与Windows交付仍未完成；完整目标ACTIVE，自动化不恢复。[本轮实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-regional-reward-actual-20261003.md)。以下保留历史。

已修复机械爪到达后横移却错误切回牵引的状态问题，完整指定的56项机械爪回归及旧西侧路线通过并经独立复核。新实跑在完整挂载的神殿中完成：闭门阻挡普通行走/跳跃、拒绝两次隔门抓梁/屋顶、真实抓锁链开门，随后同一角色附着横移→普通取消→上层落台→压板开门→补给室→安全返回，保持5点生命。新闭门测试的地面观察误判已用实际三条射线校验修正，未改游戏碰撞；此前失败保留。这是局部实跑，不是正常开局整关验收：上游开门条件为内存预置，补给未绑定，取消等待约1.4秒仍待手感优化。补给收益/恢复、区域联动、原创美术、动作、UI、声音、正常窗口体验和Windows交付继续未完成；目标ACTIVE，自动化不恢复。[本轮实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-closed-chain-actual-20261003.md)。以下保留历史。

新支路仍未通过上层进入验证：站地直抓的同帧记录确认绳索被新增平台侧边挡住；另行尝试自然起跳接一次抓爪，绳索虽畅通，角色接近平台仍受阻，未到达落点。不会把微调到某个精确按键时机作为可玩性通过，下一步重排局部抓点与进出路径，并检查绕关风险；尚未改正式牵引力或跳跃参数。测试场景拆装警告已修正，下层真实重型敌人双向完整通过及预警→攻击→恢复已实跑并复核通过，双方生命不变；这不代替真实庭院追逐。上层门、防夹、补给实际收益、视听精修与Windows交付仍未完成。[本轮实际记录与边界](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-access-physics-actual-20261002.md)。

神殿上层补给支路已写入实际场景：新增相连抓取梁、上层平台、压板门和一次性补给；区域配置保存、奖励存档和生成场景检查均已通过，旧庭院/检查点/Boss及原路线结构检查保持通过，本阶段独立复核通过。下一步验证真实移动、抓取落点、门的防夹和补给收益；当前不是完成版，不能据此宣布好玩或流畅。视听精修、正常窗口试玩和Windows交付仍未完成。[本轮运行记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task18-production-actual-20261002.md)。以下保留历史。

两条路线的实际通关记录已接入严格校验并通过独立复核：正常开局、Boss击杀、实体出口和结算后持续移动均保留真实运行证据，没有改变正式操作或数值。接下来优先推进神殿上层可选补给机关，让机械爪攀爬、压力门探索与安全返回形成实际玩法；目前仍是待验证的布局候选，尚未增加到已完成机关数量。完整操作覆盖、正常窗口体验、美术动作及Windows交付继续未完成。[本轮实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-terminal-receipt-actual-20261002.md)。以下保留历史。

主线和探索线现均已从正常开局实际完成Boss击杀、开门和有地面支撑的实体出口；结算后持续按右60个物理帧，位置、生命、支撑和存档保持稳定，两条路线的这段证明均通过独立复核。下一步把实际通关证据接入验收记录，继续补齐机关真实操作、重复路线、恢复/继续游戏及最终视听体验；目前不能把局部自动实跑通过称为完整序章交付。[两路线击杀与出口记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-fatal-exit-actual-20261002.md)。以下保留历史。

主线新实跑已从正常开局完成整场Boss战、真实开门和有地面支撑的出口通关；结算后持续右移60个物理帧，角色位置、生命与存档均稳定，没有重复结算。限定源码审查通过，运行记录正在独立复核，探索线同版本验证已开始。这是自动实跑的局部证明，不代表整关美术、流畅度、真人体验或Windows交付已完成。[击杀与实体出口实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-fatal-exit-actual-20261002.md)。以下保留历史。

主线和探索线从正常开局均已实际完成Boss第二阶段连续六次破防命中：首次7→6之后，再连续五轮降至1点生命，角色分别保持3点/1点生命、完整地面支撑和同一运行身份；两条路线独立复核通过。修正的是自动试玩事件时序假设，正式技能、动作和数值没有放宽。下一步验证最后一击、战后开门和实体出口；正常窗口/真人试玩、最终视听与Windows交付仍是未完成项。[连续战斗实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-nonfatal-repeat-actual-20261002.md)。以下保留历史。

Boss连续战斗验证已越过资源身份检查的卡点：首次破防命中后，攻击自然结束，Boss正常恢复装甲，角色保持3点生命与完整地面支撑。当前自动试玩停在事件帧与等待恢复帧的时序检查，两者并不是同一时刻；正在修正这项测试假设并保留首个窗口的严格对应关系。尚未完成后续连续五轮、最终击杀或实体出口，也未改正式动作/数值。最终视听、真人游玩与Windows交付仍未完成。[连续战斗实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-nonfatal-repeat-actual-20261002.md)。下方是保留历史。

正在扩展Boss连续多轮破防验证。新主线实跑仍完成了首次真实破防和攻击，但连续战斗检查在交接时失败，尚未进入后续五轮；角色保持3点生命、站在原地面，存档及正式游戏参数未改变。正在核对测试记录的对象/资源身份一致性，未把编译成功或单次命中当作完整胜利。整场Boss、实体出口、美术/动作/UI/声音、正常窗口体验及Windows交付继续保留为未完成项。[本轮失败与原始证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-nonfatal-repeat-actual-20261002.md)。下方保留历史。

主线和探索线均已实际完成并通过独立复核：在Boss第二阶段用机械爪抓中真实锁链、触发破防、自然收爪，再切回普通攻击，造成一次真实伤害（Boss生命7→6）；角色分别保持3点/1点生命，无受伤或复活。本轮修正的是自动试玩过严的鼠标小数精度检查，改为验证真实碰撞区域与实际选中对象，没有修改正式技能射程、破防时长或角色数值。下一步验证连续多轮破防、最终击杀及安全出口；完整战斗、美术/动作/UI/声音、正常窗口试玩与Windows交付仍未完成。[两条路线本轮证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-phase2-one-exposure-actual-20261002.md)。下方保留排查历史。

第二阶段的自动试玩卡点已定位：真实E键、左键、模式和目标可用性均正常；鼠标采样位置距目标中心约0.059游戏坐标单位，却被测试中0.01的极严精度条件拒绝。诊断记录通过独立源码复核，没有改变输入或游戏数值；正在核对正常鼠标/镜头坐标转换与合理的目标命中证明。尚未实际完成本轮机械爪破防或后续伤害，不能宣布修复或通关。[第二阶段实跑记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-phase2-one-exposure-actual-20261002.md)。以下保留历史。

第二阶段实跑已到首次锤击窗口，角色通过正常移动站稳，实际攻击碰撞与角色伤害区之间保持10像素垂直间距，生命值未变。E键已切换机械爪模式，但抓手刚进入瞄准状态，自动试玩的输入断言失败，尚未证明破防或后续攻击成功；正在核对真实输入消费与事件顺序，不据此断定正式技能损坏。主线/探索线第一阶段仍已通过，完整胜利、实体出口与最终视听/Windows交付继续保留为未完成项。[第二阶段实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-phase2-one-exposure-actual-20261002.md)。以下保留历史记录。

主线和探索线的Boss第一阶段均已通过实际验证与独立复核：各自由同一角色从正常开局进入战斗，以五次真实攻击将Boss生命12→7并自然进入第二阶段，分别保持3点/1点生命，无受伤或复活。修改仅涉及自动试玩的点击与刹车时机，实际出刀、命中、安全检查和正式游戏参数未放宽。下一步验证第二阶段的机械爪破防与攻击；最终胜利、安全出口、美术/动作/UI/正常窗口试玩和Windows交付仍未完成。[Boss第一阶段实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-phase1-positional-actual-20261002.md)。下方保留排查历史。

Boss第一阶段正在验证：正常开局的主线实跑已进入真实Boss场地并避开多轮攻击，但自动试玩仍未打出有效攻击，完整通关未通过。新增被动诊断与独立复核已确认：五次反击窗口都在距离尚差约1–2像素时被脚本提前判晚、转身撤退；不是能量不足或模式未切换。正在修正自动试玩的移动、刹车与点击采样时机，实际出刀距离、窗口、掉血与无伤检查保持不变，不改Boss难度或角色参数。第一阶段、第二阶段、胜利出口、最终画面/动作/手感和Windows试玩交付均继续保留为未完成项。[第一阶段实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-boss-phase1-positional-actual-20261002.md)。

神殿入场已通过主线和探索线的实际验证及独立复核：同一角色真实开门、触发检查点并写入测试存档，再完整进入Boss场地，关门后无重叠或卡门；这段分别保持3点/1点生命，无复活、额外跳跃或冲刺。修正的是自动试玩过早检查输入的问题，没有改游戏操作和数值。整条测试仍明确停在Boss入场，下一步接续第一阶段战斗，完整胜利与出口、画面/动作/手感精修、Windows交付尚未完成。[神殿两路线实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-temple-entry-actual-20261002.md)。下方为保留的排查历史。

神殿入场自动试玩正在续接：新代码已通过编译及限定源码复核，主线仍能到达同一神殿前平台，生命值与此前路线记录一致。目前实跑停在开门前的 E 键/模式/鼠标瞄准联合检查，尚未施放机械爪，不能认定神殿检查点、Boss入场已完成。正在补齐实际输入采样记录并去掉“等待一次即已采样”的测试假设；暂不改正式操作、射程、地图或角色数值。[神殿入场实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-temple-entry-actual-20261002.md)。完整通关、视听精修和Windows试玩交付继续保留为未完成项。

探索线已实际完成谷底捷径回升并到达神殿前平台：保持同一次普通跳跃按住再接机械爪，真实抓取、主动松爪、落稳均通过，随后走完剩余15级上行地形、返回上层并击败庭院敌人，保持1点生命、无伤、无复活。限定修订与实际记录通过独立复核；修改的是自动试玩操作，没有改正式地图或角色参数。主线和探索线现在都已到达原测试的神殿暂停点，但神殿检查点、Boss战、实体出口、最终美术与Windows交付仍未完成。下一步续接真实开门、检查点存档及Boss入场。[本轮实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-shortcut-first-ascent-actual-20261002.md)。以下保留排查历史。

探索线上行诊断：普通起跳后已真正抓中地形，但仍未落上平台。新实跑在释放同帧记录到连线先碰到附近蒸汽分流开关的碰撞区，目标地形本身仍有效；角色保持1点生命、无复活，回落到原台阶。被动诊断与实际记录经独立复核，这不是控制器内部具体分支的直接记录。正在验证保持同一次空格按住再接机械爪的自然操作，尚未修改牵引力、地形或宣布通过；完整探索线仍未完成。[抓取中断实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-shortcut-first-ascent-actual-20261002.md)。

2026-10-02主线已实际推进到神殿前平台：补回自动试玩遗漏的普通跳跃后，角色完整落上庭院台阶，再正常击败两批庭院敌人，保持3点生命、无伤、无复活；限定修订与实跑经独立复核。测试随后停在原有尚未续写的神殿阶段，尚未真正触发神殿检查点，也未完成Boss和实体出口。整条记录仍保留未通过，未改地图或角色数值。下一步验证探索线的上行抓取，再继续检查点与Boss流程；最终美术、真人手感和Windows交付仍未完成。[庭院实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-courtyard-step-actual-20261002.md)。以下记录为排查历史。

2026-10-02探索线谷底往返已实测通过并经独立复核：角色实际切换分流器，普通跳到通道，再普通跳回台阶，最后恢复喷口方向；全程保持1点生命，无复活、冲刺或二段跳。两次控制施放均命中实际机关，落点均完整受地面支撑。整条路线仍失败在随后上行抓取：瞄准射线先命中附近开关的碰撞体，尚未回到上层；庭院、Boss和最终画面验收仍未完成。[本轮实际往返记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-shortcut-safe-return-actual-20261002.md)。

2026-10-02主线衔接已验证：站稳等待自然收爪38个物理帧后，下一次抓取被实际接受，并完整落稳到下一平台，保持3点生命、没有复活；限定测试修订通过独立复核。随后到达庭院入口，但后续纯行走被挡住，尚未完成庭院战斗或Boss。探索线正验证已有分流开关配合普通跳跃的安全往返；本次没有改变正式玩家参数，也不代表机械爪手感、美术或完整序章已完成。[本轮实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-finalcatch-idle-actual-20261002.md)。

2026-10-02两个衔接卡点已实测区分：主线下一次点击发生在收爪期间，没有产生新抓取；探索线返回通道时先受伤死亡，再由正常复活流程送回营地，并非已证明从台阶掉落。新增诊断经独立复核，不改实际输入；两条完整路线仍失败。正在修正自动试玩衔接并验证安全走法，不据此修改玩家数值或宣布游戏已完成。[实际诊断及证据边界](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-post-control-handoffs-diagnostic-actual-20261002.md)。

2026-10-02探索线捷径开关也已真实触发，并写入本次独立测试存档；接近开关这一段保持1点生命、无复活，修订源码与两条路线的局部实跑已通过独立复核。探索线随后走完下行阶梯，但谷底连接落点仍失败，尚未完成往返或第二次庭院操作。主线则停在庭院开关后的抓取落点。两次整条路线仍保留失败，不算完整通关；正在分别定位这两个新卡点。[接近机关验证记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-post-split-supported-stage-actual-20261002.md)。

2026-10-02主线庭院机关已实际触发：修正自动试玩的起跳站位后，角色先退开并站稳，完成一次普通跳跃，落上第9个平台，再用机械爪触发庭院联动机关；全程保持3点生命、无复活。原起跳与完整地面支撑检查均通过，未改地图、射程或角色数值。整条路线仍失败在随后一次抓取落点；探索线同版本验证进行中，完整Boss/出口、视觉精修与Windows包仍未完成。[本次实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-post-split-supported-stage-actual-20261002.md)。以下保留排查历史。

2026-10-02机关接近验证进行中：自动试玩能以普通跳跃登上第9个平台，但新增的“起跳输入与落地支撑”记录检查尚未通过。已修复升降段丢失记录回调的问题；重新运行确认回调恢复，仍需对齐实际输入采样与跳跃计数的时序。这不是已确认的游戏二段跳，也未证明机关已可用；保留全部失败记录，暂不改地图、射程或放宽检查。[接近验证与证据边界](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-post-split-supported-stage-actual-20261002.md)。完整目标仍在推进，美术精修、Boss/实体出口和Windows试玩交付未完成。

2026-10-02战后机关已定位到实际拒绝原因：主线点击庭院开关时，系统记录超出射程（705.1>620）；探索线点击捷径开关时，系统记录被遮挡，独立碰撞查询也命中身前平台。两次按键和机械爪模式均正常，没有受伤或复活，整条路线仍保留失败。下一步先验证真实跳上平台并站稳后的操作，不改射程、取消碰撞或直接激活机关。[机关实际诊断](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-post-split-control-diagnostic-actual-20261002.md)。

2026-10-02分层区飞蛾布置已修复并完成两路线局部实跑：将这一只飞蛾移至平台上方的净空，不改敌人数量、血量、AI或平台碰撞；新增布置回归先失败后通过，源码经独立复核。主线和探索线都实际用机械爪击中飞蛾，再以攻击将其击败，这段战斗无损血、无复活（分别保持3点/1点生命）。整条路线仍明确失败：主线停在庭院联动机关，探索线停在上层捷径机关，正在检查真实接近与输入条件。旧日志、真实存档、历史版本未覆盖；完整Boss/出口、画面精修和Windows试玩包仍未完成。[本次真实运行证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-split-moth-gallery-placement-actual-20261002.md)。以下进度保留为历史记录。

2026-10-02飞蛾空间问题已实测确认：正常开局角色已真正跳上第9个平台并刹停，完整受地面支撑，生命值保持3点、没有复活；飞蛾进入感知范围后飞来，却顶在该平台底面，无法到达可交战的上方。限定测试路线修改已通过独立复核，但整段战斗仍明确失败，不算修好关卡。下一步修正这处遭遇的飞行净空/布置，保留敌人数、强度与实体平台，再重跑主线和探索线。这一轮没有新美术或Windows包；完整Boss、出口及最终视听交付仍未完成。[实际接近与碰撞记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-split-moth-supported-actual-20261002.md)。

2026-10-02最新通关排查：主线和探索线现已分别完成摆锤区无伤战斗、真实抓取第8/9个平台、落到分层战斗平台，并用实际攻击击败第一批地面敌人，生命分别保持3点和1点。修正的是自动试玩的防守策略和接敌顺序，地图、敌人和角色数值未变，限定源码修订通过独立复核。第二批飞蛾仍未打通；新增实际记录确认：向右输入生效，但角色顶住第9个平台的左侧，飞蛾又不满足现有自动抓取条件，704次循环里没有发出一次针对它的抓取。被动诊断通过独立复核，下一步验证正常跳上80像素高台后的接近路线，不先改机械爪判定。进程信息采集超时已用经过独立复核和实际运行的新脚本绕开，旧脚本与真实存档保留，没有重启系统服务。这不是美术更新，完整Boss/出口、最终视听、Windows试玩包尚未完成。[飞蛾接敌实际诊断](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-split-moth-gate-diagnostic-actual-20261002.md)；[分层接敌实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-split-gallery-approach-actual-20261002.md)。

用户指定《Ori and the Will of the Wisps》作为最终游戏美术、特效动效、场景交互及 CG 的品质与表现参考。保留原创机械峡谷遗落神殿题材；制作重点为绘画感的远中近景、统一光色与材质、流畅且可读的角色/敌人动作、环境动态响应、机械爪/护盾反馈及镜头演出。不是复制参考游戏的角色与资产，也不是已经达到其品质的声明；正常运行画面、交互、性能及完整通关仍须分别验证。

用户希望适用素材优先使用 GPT Image 2.5。已核实[官方 Sunburst 模型文档](https://developers.openai.com/api/docs/models/gpt-image-2.5-sunburst)：模型支持图像生成/编辑并可由 API 指定；但当前内置绘图工具没有模型选择参数，不能确认或保证其后端为 2.5，API 权限与计费路径也尚未验证。本次没有生成、配置密钥或付费调用；首次新增付费生成仍须先确认费用。图像用于候选概念、场景层、道具、UI 与 CG 关键画面；连续动作、实时特效、碰撞交互和 CG 动画需要另行制作集成。优先以一段真实可玩的视觉样段验证整套表现，再扩展全关；不以静态概念图替代交付，展示材料仍按既定目录先归档。

2026-10-02探索线最新实跑：哨兵战已再次无伤完成；战后实际用机械爪切换沟内分流器、关闭右侧喷口，并在完整地面支撑下停到阶梯前。随后走完八级回升台阶，抓到升降区落脚点，期间未加血或复活。修正的是自动试玩的行走预算、按键采样和刹车操作，没有降低敌人强度、改变地图或角色数值；这段限定修订已通过独立复核。整条路线仍失败在后续第一次上行抓取：角色停在原地，尚未明确是输入时序还是其他原因，不能认定地形阻挡。原抓取路线与完整Boss通关仍未通过；最终美术和Windows包也未完成。[本轮实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-steam-trench-exit-actual-20261002.md)。

2026-10-02哨兵致命命中已定位：正常开局实跑在物理帧4832记录到哨兵第一发弹丸命中角色，同帧HP1→0；角色当时仍在攻击，盾未开启，能量约31.9%、热量0。新增诊断不改变操作或敌人行为，已通过独立复核。下一步修正自动试玩对哨兵预警、弹丸存活和防御时机的处理；不能据此认定敌人过强或盾判定损坏。整条探索路线仍失败，后续原抓取路径与完整通关未验证，没有新Windows包。[哨兵实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-steam-sentry-actual-20261002.md)。

2026-10-02蒸汽支路实跑：调整自动试玩操作顺序后，角色已在上层用真实机械爪关闭蒸汽阀门，未损血或复活，并实际进入下层遭遇、从劳工战推进到哨兵战；随后死亡，回到谷底营地，整条探索路线仍明确保留失败。独立复核要求补上收爪与不误拉角色的检查：新一轮实际记录到命中→释放→收回→空闲，角色施放位移0，生命值、跳跃/冲刺与恢复次数均不变，该局部测试修改已通过独立复核。后续哨兵死亡仍复现。此次只改测试策略及检查，未改地图和敌人数值，也不是美术升级。原Grip1→Grip2→Grip3回归尚未到达，正在进入哨兵致命命中的短时诊断；无新Windows试玩包。[蒸汽支路实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-steam-approach-actual-20261002.md)。

2026-10-02探索线补查：同一份升降机出口修复场景下，正常开局的探索测试走过档案室、冰封段和谷底营地，完成齿轮区战斗后，停在蒸汽支路遭遇未激活。因此原有 Grip1→Grip2 抓取路线尚未实际复验，不能算通过，也不能据此认定这次上移造成了回归。主线的真实乘梯下台与后续两个平台已通过，但摆锤区和蒸汽支路仍待解决。旧存档保持不变，没有新Windows试玩包；[最新实际证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-supported-lift-actual-20261002.md)。

2026-10-02升降机出口修复实跑：已将挡路的 AscentGrip2Grip 实体及对应画面一起上移80像素，保留抓取用途和碰撞。正常开局测试现已实际完成乘梯、上端下台并落稳，随后抓取到达Grip3、Grip4；生命值3点，无新增跳跃/冲刺或复活。整条主线仍失败在后续摆锤区遭遇，探索路线对旧抓取路径的回归和独立复核尚待完成，不能据此宣布首关完成。单区生成器的快速退出导致进程身份采集缺失，该生成记录仍保留失败；生成文件差异已逐行核对，后续主线实跑使用这份确定场景并完整记录了真实进程。旧存档与历史版本未覆盖。[最新乘梯证据](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-supported-lift-actual-20261002.md)。

2026-10-02乘梯最新实跑：正常开局自动试玩已实际上梯停稳，采到E后进入机械爪模式，真实点击并命中开关；角色完整受同一台面支撑，上升约515像素到上端，期间生命值3点、没有复活、跳跃或冲刺。输入等待修订通过独立复核。随后左侧下台失败，新增实际记录已确认原因：角色在上端左移时撞到 AscentGrip2Grip 的右侧，水平速度归零，直到平台回程仍未下台。这是实测碰撞，不再只是源码推断；正在独立复核诊断并准备出口净空修复，尚未修改地形或停靠时间。完整路线、最终视听和Windows候选仍未完成。[乘梯实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-supported-lift-actual-20261002.md)。用户提供的Godot AI仓库已完成只读评估；本地插件3.1.5已启用，但实时检查仍为0个编辑器会话，未安装或升级。[工具评估](docs/production/2026-10-02-godot-ai-assessment.md)。

2026-10-02当前实跑：正常开局已实际完成齿轮区及升降机前各两批战斗，生命值保持3点，没有复活。机械爪测试先前混用了缩小窗口坐标与逻辑视口坐标，站位又被立柱挡住；修正测试坐标比较和真实走位后，现已实际抓中同一只飞蛾、拉至可攻击位置并击败它，未改游戏相机、地形或敌人数值。当前改动是自动试玩修复及覆盖补充，不是美术或内容升级。整条路线仍未通过，后续卡在旧AscentGrip1攀爬路径；操作升降机开关不等于角色实际乘梯，下一步须验证真实上梯、随梯运动及下梯。公共平台距战斗边界余量小的风险也仍需不同操作条件验证。完整Boss通关、重复路线、画面精修及新Windows试玩包均未完成，原始失败记录和存档保留。[路线实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-route-repair-actual-20261002.md)；[机械爪实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-moth-admission-actual-20261002.md)。

2026-10-02齿轮区最新实跑：正常开局后，改用真实移动、及时防御及反击的自动试玩策略，两批齿轮区敌人均实际击败，生命值保持3点且没有复活；随后走到升降机入口，测试因遭遇未激活而停止，整条路线仍未通过。独立复核发现新策略漏查两处防御条件，现先补齐检查再继续诊断升降机，不修改敌人数值或地图来凑通关。原存档、失败日志及旧包未覆盖；Boss、完整重复路线、最终视听与新Windows试玩包仍待完成。[实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-route-repair-actual-20261002.md)。

2026-10-02正常路线检查：已补上自动试玩中遗漏的谷底检查点行走，角色实际进入触发区后，进度、实时存档与磁盘存档均记录营地，生命值没有被补满；随后原齿轮区打法死亡时，确实复活在营地(1680,3960)。检查点断言先复现失败、补真实行走后通过，独立代码复核通过；整条路线仍因齿轮战斗失败而停止，没有改成通关。新增诊断的已卸载节点读取报错本次不再出现。下一步修正齿轮区操作策略并继续正常战斗路线，最终美术、完整Boss战和新Windows包仍未完成。[实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-route-repair-actual-20261002.md)。

2026-10-01齿轮区归因补充：正常开局重跑的旧位置、血量与操作记录复现。新增记录确认盾在战斗第6帧已过热，而非能量耗尽；测试随后从安全平台回到仍生效的盾巷，三次伤害来自该机关。复活后的记录代码还读到已卸载的旧地面节点路径，整次仍明确保留失败，先仅修新增诊断的节点读取条件，再复核同一路线。[实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-gear-diagnostic-actual-20261001.md)。没有改玩家数值、地图或正式战斗逻辑，也没有新试玩包；完整目标active。

2026-10-01主线实际检查：正常开局测试到达齿轮区，但自动战斗操作返回仍在生效的危险区并死亡，尚未到达Boss。独立复核确认该测试绕过了谷底真实检查点，存档仍在起点，复活回起点符合当前存档；路线监控又误判了合法复活跨区。下一步仅补充实际盾状态与受击记录，测清原因后分别修测试走位与监控，不加血、跳战斗或降低首关难度来凑结果。原存档、失败日志和场景均保留；[实际事实与边界](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-normal-main-baseline-actual-20261001.md)。完整目标仍active，没有新试玩包。

## 展示材料长期保存（2026-09-27）

用户要求所有展示的图稿、过程截图和视频留在项目文件内。统一归档目录为 `E:\OurGame\Knight\过程图稿与视频`；[分类与索引入口](E:/OurGame/Knight/过程图稿与视频/README.md)。保留原始版本、展示版及校验索引，后续先存档再展示，不依赖临时目录，不覆盖旧稿。历史失败/诊断材料与正式资产须区分；本地归档不等于 GitHub 发布或整关验收。

## 首次交付估算（2026-09-23）

2026-09-30再次回答工期：首个可以从头玩到结尾、画面与UI有明显改善的评审稿，暂按**还需5–10个有效开发日**预留。它是基于尚缺的内容接入、整关通关/复活/存档检查、整体表现修订和Windows可靠启动而作的低置信度排期，不是实测生产速度推算或保证日期。间歇性Godot/.NET退出问题的解决时间仍无法量化；本次估算不宣布它已修好，不重新启动已取消的自动化，也不缩减完整目标。下方3–7日保留为最初估算历史，而非重新开始的倒计时。

最新目标补充：用户要求继续增加敌人与机关的种类、数量和画面密度，并允许适当提高首关难度。入口仍易学，中后段增加换位/防御/机械爪与机关地形配合要求；清楚预警、公平反制、安全营地和最多两名同时有效攻击者保留。完整目标已通过目标工具重新设为 active（该工具在新的用户请求后返回空），不恢复自动化。数量及难度安排不是已实装成绩。

用户要求继续推进并询问首次交付时间。初始粗估为 **3–7 个实际开发日**，面向可打开、能完整试玩、内容和区域画面明显扩充的首个集成候选，不是最终精修版的保证日期，也不在每次更新时重新倒计时。这个估算置信度较低：新增敌人的正式区域接入正在独立审查，机关扩充、完整通关及复活回归、Windows 打包仍有工作；若出现较大的整关问题需重新估算，不删减完整目标来凑期限。有效开发时间不包括会话停滞或机器不可用时间，不恢复自动化。依据与交付边界见[首次交付估算](docs/production/2026-09-23-first-delivery-estimate.md)。

钟矢哨兵与峡谷疾行虫现均已通过独立组件的实现及修订审查，已保存实际输入短片与专项/安全邻接回归记录；详见下方当前状态。当前已验收的地图数量仍为3种普通敌人、17个有限出生位，不能把未验收的新接入计入正式关卡。区域布景本轮局部审查已通过，现在进入两类敌人的正式路线编排，再继续机关扩充；整关通关、最终画面、真人手感和新试玩包均未因此验收。

## 视觉品质要求补充（2026-09-20，待制作与验收）

用户再次明确：继续丰富玩法的同时，必须提升画面的精美程度。视觉不是“关卡能通了就算完成”：保留机械遗落神殿方向，重点提升远中近景层次、光影与材质统一、角色与怪物动作、机械爪/护盾/机关反馈，以及 UI 排版、图标和动效。减少杂乱噪点、贴图拉伸、僵硬过渡；装饰和特效不能遮挡角色、敌人前摇、落脚点及可抓地形。素材继续分层并保留替换接口，新增付费生成须事先确认。

验收需对照正常游戏窗口中的代表场景前后画面，审阅运动中的镜头、动作与 UI，并记录性能及未通过项；构建成功、元素数量或静态截图不能代替视觉品质验收。目前画面升级尚未完成，完整新试玩包也未交付。目标栏本次检查返回空，已按此前“设立目标并继续”的授权恢复完整序章目标，并明确加入以上视觉门槛；不恢复已取消的自动化。

当前表现绑定核查：正常入口中的玩家、普通敌人与 Boss 主要由程序绘制的分部件图形呈现，已有部件动画；资源清单或场景中的图集裁切元数据，不代表对应图片已经实际显示。旧盆地样板中更细的劳工分关节形象及样板指导面板，也尚不能算作正常序章的视觉升级。后续以实际正常入口中的显示结果、动作和可读性验收，不能拿资源清单或样板截图替代。

实际打开素材后的补充：现有 `environment-atlas.png` 是条纹色板，`player-layers.png` 是占位方块，`mechanisms.png` 是简单符号；敌人/Boss 图集是简单平涂部件。这些可以保留用于原型或配色参考，但清单中的 `final: true` 不等于精美成品。已纠正后续计划中“直接复用这些图集即可”的前提，素材质量不足处必须明确记录，不靠拉伸色块充当正式场景。

已完整核对用户提供的美术方案，将具体视觉标准、现有素材状态与验收边界保存在[序章视觉品质要求](docs/production/2026-09-20-visual-quality-requirements.md)。文档中的素材数量和引擎节点属于规划建议，不会未经判断地覆盖已批准的玩法；但画面品质缺口仍是完整目标的未完成项。

## 当前地图接入状态（2026-09-30）

2026-10-01路线检查最新：存档原始路径保护与主线/探索线分开判定已修正，三组判定测试实际通过；正常标题菜单的启动、设置和新档保存也实际通过，并完成独立复核。测试新档仍在160/600初始出生点，没有提前激活检查点、Boss或结算，原始存档保持不变。[实际记录与范围](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-route-contracts-actual-20261001.md)。现开始正常开局的主线前置诊断；测试判定通过不代表真实路线/Boss/最终画面完成，仍无新Windows试玩包。

2026-10-01路线检查补充：测试启动握手修订后，存档路径检查已拿到真实进程身份与退出结果，并实际复现“相对路径先被转成绝对路径”的测试保护缺口，正在限定修正。主线与探索线将分别按真实路线要求验证，主线不强制完成所有可选支路；测试记录中的Boss/出口标记不充当真实通关证明。[路线测试实际记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-route-contracts-actual-20261001.md)。正常Boss战、四次完整路线、最终视听与Windows试玩包仍未完成，目标保持active。

2026-10-01出口邻接最新结果：限定修订只识别规定诊断堆栈中的精确返回类型行，真实错误与泄漏检查保持不变。重新运行的五项流程检查和旧样片的出口/继续游戏检查已实际通过；加上此前旧档迁移与存档隔离通过，出口修订的邻接兼容检查现已补齐。原始失败不改判，实际存档未变。[运行记录与限制](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-exit-neighbors-actual-20261001.md)。下一步补正常开局的主线/探索线真实Boss战与实体出口验证；最终画面、真人手感和Windows试玩包仍未完成，不是新整关交付。

2026-10-01相邻检查：旧档迁移与存档隔离已实际通过，原始存档保持不变。重新游玩的五项断言与规定的两条错误复活诊断均已到达，但严格日志检查将诊断堆栈中的 `Godot.Error` 返回类型名命中为错误，整次仍保留失败，正在独立核对并准备限定修订；样片出口因此尚未运行。没有放宽真实错误/泄漏检查，也未改旧测试。[实际结果与边界](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-exit-neighbors-actual-20261001.md)。正常战斗完整路线、最终画面和Windows试玩包仍未完成；完整目标保持active。

2026-10-01定位补充：一次失败瞬间诊断确认，远处哨兵不在有效交战区，但画面内的右侧蒸汽真实处于预警，按“敌人或机关预警均优先”的规则仍会压下路线提示。旧全场景测试的“此时没有任何预警”前提已不成立；原测试、原失败和存档保持不变，尚未宣称整体回归通过。诊断退出还出现2项实例泄漏及1项资源未释放警告，原因未确认，保留为未解决风险。下一步准备正常序章的实体出口专项：Boss击败不应直接替代角色活着、有真实地面支撑地通过出口；补充测试先行，不以控制存档/直接伤害的专项场景冒充完整试玩。

实体出口首次专项现已复现：角色仍在门左侧(7200,1080)、5点生命且站在真实地面上，Boss死亡后却提前记录为通关。该运行没有其他退出警告，但独立复核发现测试保护漏查一个插件源码和根目录音频资源，因此保留为明确的问题复现，不称完整验收；先补齐保护再修游戏。结算后停止移动、保留结算UI、正常战斗后走出门和四次完整路线仍待验证，尚未交付新试玩包。

2026-10-01出口修订进展：补齐全项目保护后的复现成立，已实现“击败Boss后沿真实地面穿过出口才结算”，限定代码审查通过。首次修订运行中，提前结算和四种无效出口状态均未出现误判，真实按键过门触发一次结算；但结算后仍能移动，整组未通过。另有测试退场期间音频已释放、角色仍继续落地触发结算的异常，正在仅修新测试的退场顺序，再独立验证正式终态；不以局部成功冒充完整通关或新试玩包。

出口专项最新结果：修正新测试退场顺序并停止正式结算后的玩家输入/物理处理后，七项实际运行检查全部通过。Boss击败后仍须活着、有真实地面支撑且完整穿过出口才结算；门左侧、空中、地面外和死亡状态不会误判；真实按键过门只结算一次。结算后再持续按移动120帧，位置不变、普通结算面板可见，错误复活被拒绝且不重复结算。真实存档与场景未变，无额外退出异常；人为测试错误复活产生的一条规定诊断原样保留并核对来源/次数。[专项记录](.superpowers/sdd/2026-09-09-continuous-canyon-world/task10-physical-exit-green-20261001.md)。这是控制前置条件的无界面专项，旧档迁移/重新游玩/样片边界、正常战斗完整路线和最终窗口UI仍待验证，不是整关完成或新试玩包。

2026-10-01最新：第四轮蒸汽改为分层、错开高度的浅色雾带，实际绘制的顶点颜色向上淡化。一次覆盖检查通过，十八次真实采样均有明显透明度衰减，原有检查未退化；限定代码审查通过，但画面复审仍需修订：右预警主体改善，尖叶形体和完整危险宽高仍未通过。v008实际输入往返两次机械爪切换成功、196次观测均5点生命且无回血或复活；原片、十张原尺寸图、预览和审阅记录已保存至 [v008归档](E:/OurGame/Knight/过程图稿与视频/2026-10-01-双向蒸汽-v008-透明度与雾形修订/README.md)，37项大小/SHA256重新核对0问题。它是6.80秒固定帧率、无声、OpenGL局部诊断，不是整关、真人手感、实时性能、音效或Windows证明。六组命名相邻回归入口已准备；结构、蒸汽及旧阀门实际运行通过，敌人组在“离开交战区后恢复萤火虫”的既有断言失败，因此停止批次并定位原因，压力门及旧样本组尚未运行；整体回归未通过，旧断言与失败记录不改写。完整序章目标保持active，首稿仍是低置信度约5–10有效开发日估算，尚无新试玩包。

新描边检查首次运行因夹具漏掉首次绑定调用而停止，失败日志保留。仅修测试初始化后，三种真实条件均复现粗描边越界；随后仅修可移除表现，保留日志中三种条件的实际边距均为2.600，旧18组检查也通过，限定代码审查确认边界问题已处理。新版局部实际输入原片、十张截图与预览已先保存于[v007归档](E:/OurGame/Knight/过程图稿与视频/2026-09-30-双向蒸汽-v007-扩散修订/README.md)，再审看形体和危险辨识。碰撞、场景和真实存档未变；本版画面仍被独立审阅否决：轮廓更柔和但仍像实心竖柱、危险宽度不明确。第四轮新增实际绘制透明度检查：首次运行被新预警用例名称误触警告扫描，保留原失败；仅修用例标签、不放宽警告守卫后，18次真实采样均确认顶部透明度没有减弱，已有检查通过。新实现者现仅修改雾形与透明度，修复通过和窗口审看尚未完成；60秒相位回绕未修复。当前没有新Windows试玩包；数值或局部短片不能代替整体美术与整关验收。

v006上行卷曲表现已完成新的局部实际输入录制，保存[原片、十张原尺寸图、预览及审阅记录](E:/OurGame/Knight/过程图稿与视频/2026-09-30-双向蒸汽-v006-流动修订/README.md)。两次真实机械爪操作及安全往返通过此次局部检查，真实存档未变；但独立画面审阅仍发现尖顶柱状主体、右预警与背景混合、危险宽度不够清楚，限定代码审查还发现粗描边稍超真实边界。下一轮仅修现有程序特效和对应描边检查，不修改玩法碰撞、不调用付费生成。完整序章目标及整关/动作/UI/声音/实时性能/Windows验收仍未完成，不能由6.80秒静音固定帧率片代替。

最新 v005 连续烟柱与状态轮廓修订已通过原有12组及新增3组检查、限定代码复审；冰封局部录制完成。原片、10张原尺寸截图及MP4预览已先保存至[过程归档 v005](E:/OurGame/Knight/过程图稿与视频/2026-09-28-双向蒸汽-v005-轮廓修订/README.md)。随后原尺寸帧和整段顺序抽帧评审仍未通过：形体像叶片，右侧预警偏弱，双端收束使危险宽度不易判断。正在修订喷口起源、向上流动和范围提示；不改碰撞来迁就图片。下方 v004 被否决记录继续保留。用户要求的长期保存规则已写入三处项目入口：此后所有展示媒体先按日期/主题/版本归档，原稿和被否决稿均不覆盖。

冰封支路已完成首次实际按键局部往返：从上一块真实平台落下，以E/左键切换分流器，横穿后再次换向并返回；196次观测均保持5点生命，未回血或复活，真实存档未变。已审阅10张原尺寸状态图及整段顺序抽帧，保存6.8秒短片。它使用一处明确预置起点、真实角色和敌人、固定60FPS及静音，不是正常开局通关、真人手感、战斗或实时性能验收。

蒸汽支路实际输入路线现已首次通过：击败劳工与实际到场的哨兵，三次在安全窗口穿过旧喷口，正常走入中央空隙后完成新分流器三次操作、往返，再通过八次普通跳跃回到升降区。两次此前失败来自测试站位被真实平台遮挡，均保留原记录；本次只修正测试走位，没有移动机关、地形或放宽机械爪穿墙规则。角色在战斗中受伤一次，战后全程4点生命，无回血或复活；实际存档、源文件及场景保护检查通过。已保存并审阅顺序抽帧和关键原尺寸画面：[30.27秒区域测试录像与24张截图](E:/OurGame/Knight/过程图稿与视频/2026-09-27-蒸汽沟壑-v003-区域路线验证/README.md)。这是明确预置区域起点、固定60FPS、静音的真实输入测试，不是从正常开局完成整关、真人手感、音效或实时性能验收。

蒸汽表现已完成一轮修改与实录对照：外部箭头改为管路接头指示，矩形能量板改为透明烟柱；原有12组表现检查通过，冰封支路再次实际输入往返，196次观测均保持5点生命。新旧10个关键画面的角色位置与机位一致，[v004录像与截图已留档](E:/OurGame/Knight/过程图稿与视频/2026-09-27-双向蒸汽-v004-表现对比/README.md)。但本轮画面评审未通过：烟柱像规则叠圆片，预警与喷发轮廓相同，活动态灰烟危险性不够清楚。正在修连续上升形态与不依赖颜色的状态区别，不改危险范围、地形或时序。背景、人物、UI仍偏原型，局部测试通过不代表最终精修完成。

完整地图的两项专项此前已首次运行通过：路线记录连续经过蒸汽回升阶梯，进入升降区和离开路线时正确切换；两处装置在真实暂停菜单、区域复位、卸载及重挂后恢复正确状态，不重复记录旧实例。构建和引擎退出均为0，真实存档未变。这是使用独立测试存档、预设位置、手动记录观察及暂停敌人更新的功能测试，不是实际按键通关、真人试玩或自动试玩记录。

### 较早制作检查点（保留历史，最新状态以上方为准）

表现与地图绑定检查：喷口到现有地板的支撑连线、变换后的接地点和可选装饰失效隔离已通过组件测试；预警已去掉连续竖边/横档，改为断开的斜短线，真实碰撞范围不变。两处正式场景现已绑定各自真实地板，并补齐六个机关边界记录；打包场景和实时构建器各自通过检查。完整场景差异仅为两处支撑路径及六个边界记录，原地形、敌人和碰撞体未移动，真实用户数据未变。首次新测试有数组接口编译错误，已保留失败并修正；随后真实失败和修复通过分别留档。尚未通过正常尺寸动态画面验收，不把几何测试通过称作美术完成。回升阶梯现使用共享路线判断：两套消费者的54项位置、边界及无效坐标检查全部通过；三份重新生成的场景语义无变化，旧档案区精确八项边界检查也通过。

最新双向蒸汽独立组件及退场修订已通过审查：单独移除闲置喷口时，另一侧此前会停止计时并保持危险。新增两向实际释放/移除测试先复现失败，再验证修复后下一次物理更新关闭存活喷口，经过140次更新及旧回调仍无伤，新实例恢复默认并正常喷汽；完整组件回归通过，同一审查者复核两文件修复后批准，无新增严重或重要问题。按[地图接入计划](docs/superpowers/plans/2026-09-27-steam-diverter-installation.md)，两处装置现已通过原构建器生成，首次激活状态与场景/构建器四组结构检查经过先失败后通过。新增记录绑定也经过先失败后通过：每处首次激活各记一次，忙时重复操作/换向不重复计数，复位、卸载、重挂及迟绑定遵守各自生命周期，旧阀门/压力门记录仍通过。此项使用真实组件、直接调用接收器及手动观察，不等于整关自动记录或实际输入验收。仅Descent、Basin及父场景被重新生成；真实用户数据未变。喷嘴支撑/预警的正常窗口观感、完整场景记录及实际往返仍未验收。旧回升阶梯的路线识别限定修复已实现并通过位置检查，运行中的消费者与实际通行仍待验证。历史退出故障仍阻止发布，但不冻结可回退的本地内容制作。现有11秒可见短片来自退场修复前，修复后另有真实引擎回归，未把旧短片标成新录制；两处实际路线尚未验收。

本轮双向蒸汽机关已从预期失败推进到最终九组组件回归通过，其中包含五组显示状态和四组真实物理接触检查；切换模型、喷汽时钟、默认挂载、接收器与复位、旧机关模型及旧阀门生命周期同时回归。测试启动器的进程信息握手和日志失败清理已通过18项模拟检查及独立复审。最新组合检查构建与引擎退出均为0，日志无错误，真实存档未变；这不是整关或最终画面的验收，没有追加内存采集，历史退出崩溃仍阻止发布。

**当前新增制作：双向蒸汽分流器。** 控制器切换左右喷口后双方无伤0.60秒，新喷口再走完安全和预警阶段；忙时重复操作不会重启过渡，同一触发帧不会提前消耗过渡时间。真实双喷口及角色夹具已验证自动完整周期、暂停、两种复位顺序、移除喷口可选视觉、无效调参保护；卡顿大时间步也不能直接跳过预警。旧喷口仅公开原有时钟接口，伤害逻辑未改，旧阀门暂停/复位/重挂回归通过。四组隔离物理检查验证模拟E键/左键事件经真实输入读取和机械爪适配器切换、旧喷口触发前受伤与切换后无新增伤害、新喷口完整预警后连续两周期各造成实际伤害、中央两向六周期无伤及退场重挂。新增可移除表现已通过方向/真实碰撞范围/暂停/缺失节点检查；11.42秒硬件OpenGL组件短片记录E/左键及A/D实际输入路径和无伤往返，已审阅顺序抽帧和原尺寸画面。连续在换向完成后再次切换可保持无伤，已如实记录，未追加冷却或资源消耗。**短片为固定60FPS、静音、预置初始站位和零受伤无敌时间的隔离验证，不是人工实时试玩、正式地图或最终美术。** 预警横纹像梯子、测试地板遮住部分喷嘴，正式整合需改进。完整组件独立审查现已通过；两处地图安装、真实地形/敌人往返及区域引导仍待完成；不恢复自动化。

**正在接入实际关卡：** 档案区压力门与补给小室已通过原构建器生成；奖励存档、打包结构与开门记录各有先失败后通过的测试。实际整关加载中已补齐玩家绑定，并验证踩板开门、满血保留补给、受伤后最多回复两点、领取存档和重复领取防护。移动后隐藏板及新装置的边界元数据也已同步验证。死亡测试又发现延迟重置选错区域，现已补上死亡来源区域重置，同时保留目的区域重置、延迟时序和已领取奖励；原失败断言及同区／跨区／来源已卸载／完整占用复位生命周期五项新检查通过，独立限定复审未发现严重或重要问题。真实用户数据未变。

**当前测试限制：** “占门加载”测试的旧前提有误：角色被放进早已关闭的预加载门，发生约39.4像素物理推出后开门，血量不变；不把这个旧场景称为已修复。修正为先验证区域真正卸载后，新挂载的相同位置、防夹和碰撞断言已通过，角色横坐标保持1740。记录器重新绑定、已领取／未领取补给的区域重新加载与跨进程继续游戏、相邻功能回归也已通过。正常窗口录像的第0帧参数读取错误、随后助推按键被消费前就断言的探针时序错误，均已仅在测试代码中修正；旧失败证据保留，未更改玩家能力。以上是区域功能证据，不是整关通关或最终品质验收；历史原生稳定性问题仍阻止发布，完整目标不降低，未交付新试玩包。

**9月27日正常窗口检查：** 新的25.28秒区域按键演示完整跑通：实际击败锈壳敌人与疾行虫，验证不可抓门面、屋顶自动牵引与一次空格助推、踩板进入补给室、满血保留补给、右墙碰撞、往返和预警关门，随后沿原左侧跳落路线到达安全平台。构建与引擎退出码均为0，真实用户数据未变。根代理已审阅21张原生截图及录像中80个顺序抽帧，确认这段可见动作与路线；旧路线有16帧重新触地，不是全程腾空。录像没有发生受伤或治疗，不能冒充实战回血证据。独立接入审查未发现新的严重或重要游戏逻辑问题；指出的测试启动前保护缺口已用8行检查修正，六项临时文件/模拟进程测试通过并复核。它没有再运行游戏或内存采集，也没有把旧录像标成新启动器版本。尚缺“未领取补给后死亡”这一独立测试分支，未称全部生命周期验证完毕。局部接入已有有限验收依据，完整序章、最终美术和稳定性验收仍未完成。

**画面与稳定性边界：** 场景仍有大片平涂空背景，角色/敌人属简化原型，指导框会遮住局部环境；这些保留为后续精修项。此次为固定60FPS、静音、初始区域预置的录像，不能证明正常开局全通、真人手感、音效或实时性能。较早11.28秒失败录像的4个对象／2个资源未释放警告仍未归因；本次未重现，不称已修复。详细日志另有此前也出现过的NVAPI启动诊断，不能说全部日志毫无异常。一次内存采集授权已用完，本轮没有再采集。见[本轮画面与证据审阅](.superpowers/sdd/2026-09-09-continuous-canyon-world/task16b-root-visual-review.md)。

**最新补充：压力门临时查询对象的释放整理已通过专项回归和独立复审。** 仅修改机关与其测试两个文件，不改变玩法、地图、碰撞条件或视觉。首次编译因 Godot 泛型数组接口判断错误而停止，未启动游戏；核实当前引擎接口并修正后，一次完整组件回归正常退出（构建78264/0、游戏52360/0），日志无错误，真实用户数据及冻结源码检查未变。根代理已核对32项留档、10份源码及6份运行文件，均匹配；[限定范围复审](.superpowers/sdd/2026-09-09-continuous-canyon-world/task-16a-query-scope-review-report.md)未发现严重或重要问题。这是临时资源所有权整理，不是已证实的旧崩溃修复；原演示录像仍只对应当时源码，没有追加内存采集，也没有新试玩包。

**最新进度：压力门修订通过完整组件回归及局部复审，正常窗口演示及演示工具复审也已完成。** 新增真实占门加载测试、初始开关状态测试、可见门板范围测试，以及宽限期/警告期中途暂停恢复测试；先确认有效失败，再修正加载时的安全状态、开门记录和画面与碰撞错位。[修订复审](.superpowers/sdd/2026-09-09-continuous-canyon-world/task-16a-fix1-review-report.md)确认三项原问题已处理。一次正常窗口组件录像完成踩板、穿门、停留、真实跳跃/落地、返回及倒计时关门；构建和游戏退出码均为0，日志无错误/警告，存档及冻结源码未变。已审阅11张原生画面和逐阶段记录：状态、门板范围及倒计时可辨认。[演示工具复审](.superpowers/sdd/2026-09-09-continuous-canyon-world/task-16a-presentation-review-report.md)未发现该补丁的严重或重要问题。录像为1280×720、60FPS固定帧率的8.18秒脚本按键演示，使用项目默认OpenGL兼容渲染及静音驱动，不能证明真人手感、实时性能、音效或完整序章品质；整体Task16A的稳定性门槛仍未关闭。

**尚未解决的稳定性风险：** 此前压力门测试跑完断言后，在退出阶段发生原生错误（`_instance_binding_free_callback`，原始退出码`-1073741795`）；它仍是失败记录，精确源码、DLL/PDB及日志已保留。本轮修订测试和短演示正常退出，不能证明间歇性故障消失，也不能证明它与Task14的另一对象释放报错同因。没有追加内存采集，没有发布新试玩包，完整序章目标保持不变。

原[独立静态审查](.superpowers/sdd/2026-09-09-continuous-canyon-world/task-16a-static-review-report.md)及所有失败仍保留；上述挂载、几何和暂停测试缺口已由本轮修订处理，独立组件的正常窗口状态可读性已有有限证据。另一个非阻断项仍待性能验证：关闭状态每个物理帧都会遍历场景树，并非只在关门提交时遍历。历史原生退出故障、正式安装与整关验收仍不能用本轮局部证据替代。

此前发现的临时查询与结果容器缺少显式清理，已由上述限定改动处理；这仍不能确定崩溃根因，也不能将退出泄漏清单当作肇因对象清单。机关长期持有的形状与场景借用资源不在这次清理范围内。[实现与失败报告](.superpowers/sdd/2026-09-09-continuous-canyon-world/task-16a-report.md)保留每次失败、修订及旧夹具限制；[只读原生退出分析](.superpowers/sdd/2026-09-09-continuous-canyon-world/task16a-native-exit-source-note.md)区分事实、推理和未知项。本次未制作新试玩包。

压力板机关按拆分方案推进：独立组件及防夹、复位、暂停和真实碰撞测试已有实现与证据，正式档案区安装、补给存档与记录器接入现按上述新顺序继续制作，但不能在完整验证前计为正式新增机关。退出崩溃尚未定位，单次内存采集已结束，不继续重复同类未复现实验。目前没有新增试玩包；原交付要求保留，接入仍可能需要局部返工。

压力门已实现计时、实际碰撞提交、防夹、暂停/复位、移除回调与可拆视觉，并保留先失败后修订的测试记录。部分早期通过使用了后来确认无效的敌人碰撞夹具，本轮最初两次挂载测试也缺少Ready前提，均没有作为有效修复证据。最新完整组件回归使用修正后的真实碰撞与初始化前提，正常窗口动作演示已留档，完整接入仍待完成。

本轮安全修订已通过专项检查和同一审查代理的局部复审：新敌人的整个身体不能侵入营地，落脚面须连续支撑，不能仅靠两侧射线跨过中间空隙。另外，测试进程中间歇出现 Godot/.NET 对象释放崩溃，当前没有确认根因；最后一次专项和命名回归通过不能抹去此前失败。此稳定性风险仍阻止本轮集成验收和试玩交付，正在独立资源生命周期夹具中排查，不修改引擎或真实存档。

此前一次仅用于诊断的类型记录完成了四轮测试，没有复现原崩溃，但退出时报告资源未释放，不能算干净通过，也不能认定它与原崩溃同因。诊断构建已恢复为普通构建，记录探针不会进入普通游戏 DLL；真实存档与已冻结的游戏源码均未改变。该问题尚未解决，未新增试玩包。

用户授权的一次隔离采集现已完成：同一个测试进程执行四轮现有适配器加载/释放检查及最终排空，退出码0，没有观察到原崩溃、泄漏警告或其他错误，也没有生成转储。运行前已保存精确匹配的 DLL/PDB 等文件；真实存档等受保护数据、普通 DLL 和12份冻结源码前后未变。没有追加运行，没有修改游戏逻辑、GC、缓存或注册表，没有上传数据。这是一次未复现结果，不能关闭原稳定性问题。报告：`.superpowers/sdd/2026-09-09-continuous-canyon-world/task14-single-crash-capture-report.md`。

随后核对发现：旧崩溃入口只执行一次内层夹具，最近诊断则执行四次并穿插回收；内层夹具相同，但旧入口源码及旧 DLL/PDB 未完整保留，不能声称原样复现。为检验这一差异，另外完成了**不采集内存的普通对照**：同一次构建、同一测试入口，分别执行1轮与4轮，各一进程，均正常退出且日志无错误。两组使用同一保留 DLL，真实存档和冻结源码未变，没有第三组。这提供了可比的阴性结果，仍未确定根因，不能作为修复或试玩交付依据；不再重复这组实验。报告：`.superpowers/sdd/2026-09-09-continuous-canyon-world/task14-exit-schedule-comparison-report.md`。

普通产品菜单退出已补一次有限验证：暂停菜单没有返回标题，退出直接调用 `SceneTree.Quit()`，不走测试专用强制GC排空。首轮在绘制帧末尾模拟Esc，未观察到暂停，测试断言退出1；旧失败证据完整保留。随后只调整测试发键到每帧更新前，并增加输入状态观测，在新的独立case中一次完成标题→开始→Esc暂停→鼠标点击真实退出按钮→进程退出0。实际Escape事件匹配暂停，按下状态与刚按下状态均为真；游戏暂停/退出逻辑未修改。三张1280×720原生窗口画面均已审阅，使用项目默认OpenGL兼容渲染，无Godot错误/警告或原生FATAL；另有未阻止运行的NVAPI启动诊断，不能声称所有日志毫无异常。真实存档、冻结源码及本次保留的构建文件前后未变，没有追加内存采集。这支持此前暂停失败属于测试输入时序问题，**不代表旧间歇对象释放崩溃已修复**：带测试观测的短菜单路径不能代替完整生命周期、整关通关、真人体验或最终画面验收。报告：`.superpowers/sdd/2026-09-09-continuous-canyon-world/task14-product-quit-input-boundary-report.md`。完整序章目标仍继续，Task14稳定性门槛未关闭，未开始Task16或发布新试玩包。

进一步源码核对发现，加载时的区域校验还会临时创建并销毁未加入场景树的区域，单看已显示场景的记录会漏掉这类生命周期。随后的一次仅测试构建身份观察已经完成：将5个区域定义、1个既有Boss配置引用和2个表现配置的托管序号对应到当时有效的原生ID/路径；共44对释放进入/返回日志，其中8次有身份对应、36次仍未知。一次完整适配器测试退出0，无运行时错误、泄漏警告或原FATAL；普通构建恢复成功，探针类型不在普通DLL内，真实存档及冻结实现未变。诊断编译有2条旧测试分支的不可达代码警告，普通恢复编译无警告，不能笼统称所有阶段零警告。这些记录不是Boss实际生成证明，也没有覆盖离树临时区域的全部对象；观察本身会影响时序，原崩溃根因仍未知。不新增内存采集或重复此实验，不验收Task14、不发布新试玩包。报告：`.superpowers/sdd/2026-09-09-continuous-canyon-world/task14-native-identity-report.md`。

此前已按用户授权在项目独立目录安装微软官方 `dotnet-dump 9.0.661903`，并完成对原 PID19072 小型崩溃转储的有限离线分析。原文件哈希仍未变；该次离线分析没有运行游戏、更换引擎/运行时或下载额外符号。工具识别出本地匹配的 CLR/DAC 元数据和38个线程，但 SOS 读取 ThreadStore/TLS 失败（分析退出码1），后续托管栈与异常查询未执行，未定位到具体资源对象。报告：`.superpowers/sdd/2026-09-09-continuous-canyon-world/task14-offline-dump-analysis-report.md`。后续获授权的单次隔离运行没有产生新转储，因此仍没有可供继续离线定位的新崩溃样本；不能通过反复重试或忽略失败宣布验收。

两类新敌人的五个有限出场位已在工作区接入，原17个位置和12组稳定ID保留，当前源码计数为5种/22位，**独立集成审查尚未结束**。五组真实生成地形与角色碰撞体、窄台双向移动、出场提示、暂停/死亡/退场/重置专项已有运行证据；档案区和冰封区各保留一段实际输入短片。暂停/死亡补测是在现有逻辑上首次通过，没有虚构失败历史。实际存档未变；区域预置起点和固定帧率短片不代表正常开局整关通关、真人手感、最终美术或实时性能。验收前仍将3种/17位列为上一正式地图检查点，不混淆“代码已接入”和“整个目标完成”。

区域布景的第二轮九机位正常窗口对照已制作并逐张审看：档案架补入成组文件和落地容器，营地/维修区补入轴承与断管机座，神殿增加破损叠层拱框，部分真实台面下增加低对比支撑。九组玩家位置、镜头与画布变换一致；这是局部结构陈设改善，仍是简化几何画风，不是最终精美美术。构建器、打包场景及1280/1920窗口可读性专项均有新运行日志；正常开局至齿轮区的输入录像也已重录并检查接缝关键帧。独立审查发现的截图夹具越界写入风险和安全测试缺口已在第一轮修订关闭，最终15项拒绝检查与九帧捕捉通过，真实存档未变。本任务局部验收通过；固定机位或这一段录像不代替完整通关、真人手感或实时性能验收。

新增疾行虫已通过第一轮修订复核，原两项重要问题均关闭，仅验收独立敌人组件：修正尚未真正落地就提前攻击预警的问题，补齐暂停恢复、无遮挡向左冲刺和坠落阈值测试。最终专项及安全邻接回归通过（构建46376/0、引擎69912/0），哨兵回归通过（52264/0→52576/0），真实存档目录未变。新的6.17秒独立实际输入录像显示格挡、追击后退破绽、机械爪打断及普通攻击击败；已检查原尺寸关键帧，形象仍属简化功能视觉，不能当作整关美术或实时流畅度验收。两类新敌人的正式路线接入现已开始，包含有限出场提示、安全落点和真实区域战斗检查；在该集成通过之前，数量仍按已验收的3种/17位报告，完整目标继续进行。

本轮新增“钟矢哨兵”的独立组件已通过实现与修订轮审查——蓄力预警、半途锁定方向、单发直线弹道、护盾拦截与机械爪打断。任务范围和真实输入/碰撞/清理/录制要求见主计划 Task12；正式区域接入状态以上段为准。现有远程预警与程序绘制的临时轮廓不能代替最终美术。区域陈设已有上述局部改善，但大片平涂背景与简化台面的精修仍未完成。

当前验证边界：哨兵的专项行为与独立真实输入录制已有通过结果；异常时间步、死亡目标排除和恢复期真实弹丸只伤害一次已修复/补测并通过复核。实际受伤/死亡事件也证明现有 VFX→Audio 间接链路有效，初审“漏转发”判断已纠正，未添加重复声音。一次安全邻接回归暴露旧结构测试的15项字面表与已迁移的14项目录冲突，已归因，尚未修订该旧测试。完整旧 pre/post 聚合另有仍会写入真实用户目录的测试入口，已禁止运行，等待独立隔离修正；不能将专项通过或早先部分隔离修正称为完整回归通过。

最新用户反馈追加：敌人和机关都需要增加种类及数量，场景仍显得空、单调。此项扩充尚未实现，旧的12组遭遇/6类机关不能据此当作满足新的丰富度要求。接下来依据实际个体与机关实例清单安排新增行为、区域组合和环境陈设；保留连续大场景、首关易上手、安全营地、清晰预警与素材替换接口。数量增加不通过无限复刷、堆血或提高同屏围攻压力实现，视觉填充不改变可落脚/可抓取语义。当前局部台面修正先完成证据保存，再扩充内容并重新验证完整路线；不恢复自动化或启动新的付费美术生成。

已核对的基线是3种普通敌人、17个固定出生位，核心六类机关各一套。第一批以5种普通敌人/22个出场位、8类核心机关/12套安装为实施目标，并按区域补足遗迹陈设、台面支撑、环境动静和光色区别。详见[本轮内容密度扩充](docs/production/2026-09-21-prologue-content-density.md)。这是待实现与试玩的目标值，不是已完成数量，也不是最终充分丰富的保证。

护盾持续生效的后续实跑中，入口七次脉冲均成功格挡、生命值保持4点，但一次普通跳跃仍碰到首块齿轮台面的左上角并落下。现批准只将该台面左缘延伸40像素，保持顶部、右缘和后续跳跃不变，同步可见外形和碰撞；正在验证不同起跳位置的容错及实际渲染效果，不改跳跃物理或机关伤害。此修订尚未验收，也不意味着原间距在所有起跳位置都无法通过。完整目标继续有效，最终场景、动作和UI美化仍为独立未完成项。

该局部修订现已通过三处真实起跳位置、后续台阶、机关/布局/可读性回归及独立复核；原旧间距失败证据保留。正常窗口录制中已检查举盾跳跃与台面落脚采样。只验收这一处台面容错，不是完整路线通过，更不是本轮新增敌人/机关或精美度完成；步行阶段的逐帧防御观察和提前出现的机械爪指导卡仍保留为后续加固项。

齿轮区的后续验证定位到方向性防御问题：入口脉冲要求朝右侧来袭方向举盾，盾的方向由鼠标瞄准决定，不是移动方向；自动角色原先只举盾但追着敌人瞄准，仍会受伤并回到尚未激活检查点前的起点。当前修正的是测试操作，不改伤害或强行补血。正常窗口还须检查这处机关能否让玩家看懂危险来源、盾朝向及格挡反馈；精确坐标的自动操作成功不代表视觉提示和真人体验合格。完整路线仍未通过。

完整路线验证正在接入：新的顺序遥测已取得独立的失败→通过证据，试玩报告升为 schema4 并保留旧报告迁移；这不是存档版本变更，也不是整关已通过。测试还发现当前引擎忽略了两个预期的用户目录隔离参数，已在正常场景加载前拦截。经过独立读写入口核查，改用场景入树前注入外部存档、设置和记录路径；正常标题→真实开始输入的隔离专项已通过，原存档、试玩记录及日志目录的109条记录前后字节清单一致。主代理已核对实际进程 `29204/0 → 64672/0`、完整日志和清单哈希；这不表示引擎的整个用户目录已迁移。完整路线运行仍在进行，场景、动作与 UI 的最终精修和新试玩包交付仍未完成。

正常开局的首轮路线检查实际在庭院封印前失败（`16440/0 → 65828/1`），之前的连续攀爬已通过，失败退出仍保持原应用数据不变。独立核对确认玩家正被尚未解锁的门挡住：旧移动测试没有完成分层遭遇和拉动机关，两项都是开门所需条件；机关已正确存在于上升区，并非关卡文件漏生成。当前正在补全真实攻击、防守和机关操作的整关测试，不删除解锁条件或把预置状态的旧录像算作通关。

后续真实输入验证已击败入口、裂隙、档案支路的三组地面敌人，以及冰封回廊上下层的完整遭遇（含延迟出现的飞行怪），没有减血或直接调用击杀接口。上下重叠平台的三处下落已用实际走出边缘、松键、接触地面验证；档案→冰封段也完成了一次普通跳跃，并修正不符合实际走法的起跳区标注，未改碰撞地形。后续尝试从支路直抓深渊侧出口被中间实体墙挡住；独立核对确认支路原定去向是盆地，主深渊与档案/冰封本来就是分叉后汇合的两条路线。因此改为分别从正常开局完整验证主线与探索线，每趟保持同一角色并到达 Boss 后实体出口，合并覆盖全部玩法但不伪称一趟走完所有内容。完整长坠落仍须从深渊顶部实跑，不能用支路抵达谷底代替。近距离武器朝向、命中表现及窄台边缘的遭遇重置仍列入真人手感/动作精修，自动战斗成功不等于好用。旧测试的外部存档路径修正已通过独立源码复核，完整回归仍待运行；记录逻辑的恢复、重试和旧报告校验问题也在修正。完整路线、最终视觉与试玩包尚未验收。

主线长下落现已有正常开局实跑证据：从顶部落台开始，按物理帧测得约4.267秒到安全底板，没有额外跳跃、冲刺或机械爪速度修正；底部飞行怪实际击败后继续落到盆地支撑面。该次运行在预先设置的“盆地段完成”断点主动失败退出，不是整关通过，也不是真人感受或实时画面流畅度测量。记录器的恢复状态、重试事件、动态挂载和捷径端点问题已补测修正，并通过本轮独立复核；最终专项实际进程3480/0→5596/0，109条原应用数据记录前后不变。完整主线/探索线、完整回归和画面品质仍未验收。

场景分层与可读性阶段已完成验收，开发检查点 `d19b142`（初版 `89706a4`）。初审发现入口→深渊连接处的新前景遮暗角色，已移除两侧侵入通行区域的装饰，并增加双侧驻留场景遮挡与移除美术后的真实遭遇触发检查；独立复审确认两项缺口已关闭，没有新增重要问题。主代理在最终提交重新构建和运行，实际进程 `53288/0 → 26084/0`，没有编译警告/错误或运行错误、泄漏标记，仍保留八条既有故意负例警告。新录像 5.0/5.5 秒的原尺寸画面中，头部、悬浮灯、身体和武器已不再被暗色前景遮住。

这只是场景分层与可读性的阶段验收，不是最终精美度验收。另一次上升区机械爪起手后停滞的录制失败仍未归因：旧片末段角色还站在平台上，生命显示非空，未出现死亡菜单；复跑成功不能说明原因已解决。接下来完成正常开局、主支路、战斗、Boss 后实体出口和死亡/继续的完整输入验证，并继续保留角色/怪物动作、UI、场景光色与材质、音效和实时流畅度的最终精修门槛。

场景美化已有实际运行对照，但仍未完成最终验收。新一轮约57秒的同路线录像已完整解码并检查12张同位置原尺寸画面：此前明显的背景分界与大块棕色竖井封挡得到改善，平台端帽、机关凹面和钟体边缘增加了厚度，角色、落脚边和青色机械爪仍清楚。大钟与台面重复、整体光色接近，以及部分提示卡覆盖下方场景的问题仍在后续精修清单。该录像预置了机关前置条件且止于Boss前，不能作为完整通关、最终动作/声音或实时流畅度的验收。当前不发布新的正式试玩包，也未新增付费生成或外部美术导入。

短暂萤火引导和教学卡安全边缘布局已完成本阶段验收，开发检查点 `8e98120`（初版 `ee8829a`）。实际键盘移动录像的前后抽帧确认：无本地目标时，完整教学文字由中央移到左侧，角色和前方平台保持清楚。初审发现的暂停计时清零、远处同区机关压住提示和完整旧引导回归缺口，均已修复并通过独立复审；完整保留 15 项教学检查、所有辅助抓取目标解析，以及教学卡隐藏后完整恢复。默认测试也实际执行新旧引导覆盖。主代理从最终修复提交重新构建和运行，实际进程 `13500/0 → 52536/0`，无编译警告/错误，无运行错误或泄漏标记，仅保留八条既有故意负例警告。接下来升级连续峡谷的场景层次、材质和机关可读性；角色与敌人动作、UI 整体品质、完整通关与正式新包仍未验收。

稳定检查点与旧档迁移已完成本阶段验收，开发检查点 `533f818`（实现 `bf9ce4b`）。旧存档须明确选择迁移或重新开始，原文件保留；损坏的新槽在明确替换前另存原字节。实际保存、销毁场景后重新继续，已验证捷径/封印/阀门状态、普通敌人重新出现以及首次死亡回到起点，并修复了“捷径画面已开、碰撞仍挡路”的载入顺序问题。独立复核通过，主代理新构建/完整连续依赖回归 `48648/0 → 45204/0`，八条故意触发的负向测试警告已逐类核对。接下来制作短暂萤火引导与教学卡布局，再进行场景分层美化；完整真实输入通关、动作/UI/声音和最终视觉验收仍未完成。

主路八处、支路四处遭遇已完成本阶段验收，检查点 `76a307d`。独立复核发现的主路漏触发、空标记冒充玩法结构，以及真实击杀后的物理回调生成错误，均已有失败→修复→复测证据，复审通过；主代理最终新构建/回归 `17880/0 → 50156/0`。敌人起始位置记录的次要问题仍留在最终审查清单，未隐去。本阶段不等于正式成品或真人通关。

当前版本另外保存了约57秒的实际渲染路线录像，已完整解码并查看八张抽帧。大片灰底、突兀的棕色竖井封挡、重复台面与大教学卡仍清楚可见，保留为美化前对照。该路线预置封印条件且没有完成Boss战，不能用来证明完整通关、自然动作、音效、真人时长或最终视觉品质。

六类功能机关已完成本阶段验收，开发检查点 `016d883`：乘客升降台、四阶段活塞、阀门/蒸汽、摆锤、双条件封印门和双向捷径绞盘。两轮修正关闭了封印门下穿、关闭捷径绕行、伤害蒸汽不可见及生命周期检查缺口，独立复审通过。捷径现在分别从真实上、下入口验证关闭受阻、开启可达，并保留开启后完整双向路线、容错站位和独立主路。主代理在最终提交重新构建/回归，实际进程 `49904/0 → 66040/0`，末尾为 `CONTINUOUS_CANYON_TESTS_OK`；六条故意拒绝流程切换与一条故意观察者异常警告已核对。主路专项明确注入两个封印前置条件后，才用同一角色真实输入到 Boss 前、0 次越界恢复，因此不是正常开局完成战斗与机关的通关证明。蒸汽和竖井封挡仍是功能性图形，不是最终美术。接下来实现主路八处、支路四处遭遇；跨进程存档、场景美术、动作/UI与最终完整试玩仍待完成。

连续地形骨架已完成本阶段验收，提交为 `f0262ee`：独立复核通过，主代理新构建并运行完整结构依赖回归，实际构建/游戏进程 `33556/0 → 69208/0`，六条故意拒绝 Continue 和一条故意观察者异常的预期警告已核对，无其他运行错误。入口贴顶裁切已有局部地形修订，六张连续采样中的角色头部与悬浮灯完整可见。此为开发检查点，不是正式美术或新试玩包验收。

工作区主场景已接入 `LayoutVersion=2` 的扩大布局，不是下面历史记录中的原尺寸地图，也不是新的交付包。存档与旧样板已独立保护；真实深渊下落、三个侧出口、上下支路衔接、抓取站位容错及完整双向捷径已通过本阶段检查。地形阶段的正常渲染探针以同一个角色连续经过五区、到达 Boss 前检查点，记录 3439 个物理帧、0 次越界恢复，全程逐物理帧检查完整碰撞体位于视口内，未放宽防穿墙保护或增加普通二段跳。这不是包含战斗、Boss 出口和继续游戏的整关通关证明，也不是真人时长。主代理已查看改造前 12 张录像抽帧与裁切修正后六张入口采样；边缘灰底、大教学卡、重复台面及贴顶通道舒适度仍保留为后续明确问题。机关审查通过后继续丰富主支路遭遇；功能和静态台面均不代表最终画面完成。

## 实施历史：真人试玩失败与连续关卡重构（2026-09-13 续接）

以下保留各阶段当时的状态；“尚未接入新几何”等描述属于对应历史时点，当前工作区状态以上文为准。

扩建接入进行中：新版存档隔离已提交 `0700014`，旧样板地形隔离与冻结文件保护已提交 `2d5b142`、`1ed0c56`。实现方报告旧槽保护、样板生命周期与边界专项通过，待本任务最终独立复核。正在扩建实体地图并适配墙面抓取路段校验，尚未交付新的完整试玩包。

最新执行方式：用户要求“设立目标进行就好，把自动化取消掉”。已通过正式工具删除每 10 分钟运行的 `automation-9`，并创建新的完整序章活动目标；不再依靠定时任务唤醒。下方所有 ACTIVE/每 10 分钟描述仅是历史记录，不得据此恢复自动化。

连续新地图的数据层（`48a9b55`、`5eff042`）与跨区机械爪目标保留（`b9f6dee`、`c19dd55`、`63b7678`）已完成本阶段验收。抓住邻区地形后跨区，目标及当前/落地方向区域会继续保留；松爪回调中立即转场也不会提前卸载旧目标。连续镜头（`ef02c54`、`f58a45c`）也已完成本阶段验收：跨区平滑、三档下落速度的实际相机投影、逐帧制动墙可见性、最终聚焦位置均有专项；独立复核通过，主代理重新构建运行通过，仅有已记录的故意抛错观察者警告。Boss 出口/越界恢复/继续及双门专项通过，最终镜头修订只改测试。现有主场景仍使用原几何，下一步先隔离新版存档与旧样板几何，再接入实体地图扩建。检查发现旧样板复用主场景定义和邻区，已将保留旧坐标/镜头/几何快照列为接入前置项。以上不是新关卡试玩验收；真实新地图通路、机关、遭遇、完整路线、失败退出清理和正常窗口视听验证仍未完成。

当前路线迁移及相机修正已保存为**未完成的开发检查点 `fd15cc5`**（前一检查点 `2e28c62`）。相机与证据归属两项审查问题已关闭；主代理在最终源码上重新构建并复跑机械爪、释放、说明、反馈、Boss 出口/恢复等七组专项，全部通过。仍有旧房间 BrokenTooth 落台失败和当前连续地图 CatchOne 在助推前被实体阻挡，两项没有删测、放宽标准或算作通过。新大地图进入数据/路线图实施，尚未新增可玩几何，不提供伪称完成的新包。

旧盆地/上升区横向镜头接缝已通过专项和正常渲染验证：原身体裁切约 7.57px，修复后在 NVIDIA 硬件 Vulkan 的 404 帧真实 D/A 往返中没有裁切、死亡或复活，三处停留画面已检查；用户原测试/录制脚本未改。上下穿过邻区覆盖边界时，镜头现先检查当前与下一跟随位置的覆盖范围，再收缩边界。真实角色向上运动的失败→通过回归及独立复核已完成；主代理新构建的 post 也输出动态相机通过标记，随后仍在 CatchOne 失败，并在退出清理时出现 C# 绑定致命错误（真实退出码 -1073741795）。这不是完整 post 通过，也不代表完整关卡、美术或手感验收。

跨系统修复已纳入上述检查点：先修正旧护盾测试缺少真实输入采样的前提，再复现并修复“同帧挡住两次重击、仍有气力却意外收盾”。非致死受伤现在保留持续输入、消费旧单次指令；死亡/恢复/目标丢失仍完整清理。天花板脱钩测试核验真实法线安全裁剪和横向惯性，不要求穿向实体天花板；独立侧抓上升惯性、落地边距及无普通二段跳标准保留。专项通过不等于完整 pre/post 通过；余下路线、音效旧 W 消费者、退出清理及正常入口录像验收继续列为未完成事项。

2026-09-13 续接核对：目标工具当时返回无目标，已按批准范围重新建立完整序章目标，未设置虚构预算或恢复自动化。机械爪目标表面/胶囊安全停靠基础 `06223f4` 已通过独立审查，失败/通过测试的真实进程退出记录及恢复哈希已补齐。点按自动牵引的模型与临时控制桥已本地提交 `f7495ef`：鼠标松开不取消、无 W/S 自动收绳、接近安全停靠点时减速；30/60/120Hz 专项及主代理新构建复跑通过，真实 W/S 输入边界的错误注入也被测试捕获。独立审查判定符合本阶段规格、无重要缺陷；两处非阻断注释/命名问题已列入后续对应任务。

空格助推后脱离与 Q/右键/冲刺同帧优先级已通过本阶段独立审查：初版 `ce0a2d9`，目标失效/卸载与点击同帧的修正 `f45784d`。两个失效竞态分别有真实失败→通过证据；主代理在最终提交上新构建复跑通过（Godot 进程 112844 退出 0，无警告/错误）。覆盖无能量仍按右键时的取消优先级、助推只应用一次、回调重置不重施旧速度、失效后的旧点击不重发及下一次新点击恢复。表现/UI 迁移仍待完成；现有聚合测试的旧“助推后继续附着”断言明确未迁移，未删测或称为全回归成功。新地图与正常窗口手感验收尚未完成。旧包和旧存档保留，当前没有新的可交付版本。

机械爪实体阻挡专项已完成本阶段审查：实现 `ddfb3cb`，两轮测试补强 `b9068ac`、`979727f`。长下落抓高处墙面时，实际速度改善的正常制动不再误判为停滞；未放宽 0.25 秒真正停滞时限。墙、天花板和凸边使用真实碰撞命中点，分别实测超过 144/158/144px 的身体接近距离；安全停靠不要求撞墙。清晰视线下的胶囊侧翼阻挡、到达后新增遮挡、薄障碍和普通障碍的单次脱离/正常移动恢复均有专项覆盖。独立复核关闭三项测试证据缺口，主代理在最终提交上新构建通过（0 警告/错误），实际 Godot 进程 72128 退出 0。当前仍不是新地图、正常窗口手感验收或已导出的玩法版本。

目标驻留源接口已完成本阶段审查：初版 `6880906`、修正 `d5ad9e6`。实际控制器覆盖首次牵引登记、各种脱离/重置只清理一次、旧目标在清理回调内仍可读取、观察者抛错隔离，以及强制卸载回调重置后不再发布过期失败事件。最后一项有失败→通过证据；主代理最终新构建通过（0 警告/错误），实际 Godot 进程 107244 退出 0，专项包含两条故意触发的观察者警告。跨区域地图的保留/延迟卸载消费者现已接入并通过上述 `63b7678` 阶段验收。

新操作说明与反馈已完成代码层审查：初版 `528c234`、音效修正 `01c0821`。中英文控制栏、暂停页、阶段引导和样板提示已统一为 E 切爪、左键点按自动牵引、A/D 可选摆荡、空格跃出松爪、Q/右键取消；保留多分辨率布局检查。真实控制器与正式角色显示层的测试验证了助推→收势顺序，但目前仅是两个显示采样，是否自然可读仍须录像验收。音效/特效避免重复触发，且修复了再次抓冰面等无效目标时可能不再响的回归。三组专项在初版上由主代理新构建复跑通过，音效修正后又单独复跑通过；引导测试退出的音频残留已清理，故意触发的负例警告仍明确记录。下一步迁移旧路线与录制脚本，保留原落地边距、跳跃/冲刺约束，再做正常窗口实测；新地图、完整观感、正式打包仍未完成。

[连续峡谷实施计划](docs/superpowers/plans/2026-09-09-continuous-canyon-world.md) 已补入新版存档与旧样板几何先隔离、双向捷径分别实测、阶段式深渊可见性、六类独立机关、明确的主/支路遭遇位置和读取真实攻击前摇的临时萤火虫指引约束。休息营地不放敌人。跨任务检查已记录 11 项任务与 55 对共享接口；数据/相邻关系、目标驻留及连续镜头已完成本阶段验收，其余条目不是已完成成绩。

最新内容门槛：至少 10 种操作或决策真正不同的玩法场景，明确包含左右换壁的小峡谷和可抓壁制动的长下落深渊；以 12 类挑战、至少 6 类功能不同的机关、主路 8 处及支路 4 处普通遭遇作为初版编排目标。数量不是已完成成绩，也不能靠换背景或房间命名凑数。20–30/30–45 分钟是待真人测量的时长起点而非上限；丰富内容优先于拖慢移动、强制等待或堆血。既有旧包、存档和无关修改继续保留。

用户在 b98 原生包中实际走到神殿 Boss 后，报告继续向右人物消失、按 A 镜头移动但人物不回归；同时否定当前探索密度、路线提示和机械爪持按/W 收绳手感，要求点按自动牵引、地形配合与连续多层大场景。**这份新反馈优先于下方“暂不扩大全关”的旧样板阶段约定；当前不具备完成/品质验收条件。**

已保留原生故障画面，并通过受控机制测试复现：Boss 开门后 D 使玩家落至约 `(6600,2670)`，A 后仍在地图下，越界恢复次数为 0；神殿检查点重启后 Continue 被拒。源码已加入出口安全接管、越界一次恢复和合法 Continue；独立审查追加的“继续后入口门阻断”也有 RED→GREEN，已败状态双门开放，未败开战仍锁门。主代理正常渲染专项及硬件 Vulkan 录像已验证从真实神殿检查点持续 D 走到结束界面，四种已败恢复状态可 D/A 穿门往返，无重复击败事件或伪造真人成绩。独立复查关闭该缺陷；详见下方反馈记录。此为受控机制修复，不是完整正常战斗、新地图或整体品质验收，尚未重新导出用户包。

用户随后回复“批准”和“全部批准，丰富场景”，并补充流程时间太短：先处理通路/越界恢复/存档闭环，再推进自动牵引与连续多层探索。[结构改版详细设计](docs/superpowers/specs/2026-09-09-continuous-canyon-auto-pull-design.md) 已获全项方向批准，进入分段实施；首次主线 20–30 分钟、含支路 30–45 分钟是新的待真人验证目标，不能靠怪物增血、重复劳动或暂停凑时长。P0 专项修复和审查已完成并本地提交 `71c26b2`；[机械爪实施计划](docs/superpowers/plans/2026-09-09-auto-pull-claw.md) 的当前状态见本页顶部，新操作全流程与新地图仍未完成。不继续把 W 收绳说明当作最终方案，不通过更多 UI 微调或后台测试替代真人反馈，也不覆盖旧 b98 包及 ZIP。详见 [本次真人反馈与排查](docs/playtests/2026-09-09-boss-exit-and-continuous-world-feedback.md)。

## 上一阶段约定：5–8 分钟品质样板

用户于 2026-09-08 确认先制作一段包含代表性场景、可读战斗、机械爪探索与奖励的品质样板，
以实际运行的视觉、动作、上手与探索体验校准整部序章的制作方式；暂不扩大全关内容。
2–4 周是资源就绪后的初步验证窗口，不是已保证的交付日期；5–8 分钟需要首次真人试玩测量，不能由自动路线时长代替。
详见 [品质样板阶段约定](docs/production/2026-09-08-quality-sample-brief.md)。

用户“设立目标并进行”已批准盆地右半部“营地—战斗—机械爪探索—补给返回”路线，并已建立活动目标；视觉样板尚未验收。
`automation-9` 已通过正式工具恢复为 **ACTIVE**，每 10 分钟触发，完整字段读回一致；此前 PAUSED 是历史状态。
当前先制作独立正常入口与可靠的新局／复活／继续流程，再完成整圈及统一视听。不代表批准付费、外部招募或公开发行，也未取得正式美术／真人／十万付费的验收。
已实现第一项入口基础：新增独立的 [Play-Basin-Sample.cmd](Play-Basin-Sample.cmd)，带开发中标识、独立存档／设置与暂停返回标题；正常 Start 同步建立盆地复活点。
专项和普通窗口入口验证通过，短录像已抽帧审看。独立样板盆地已移除左侧 Rust，且复制 Laborer 调参将感知距离设为 200px，营地不主动接敌；走近后触发，追击脱离范围也相应缩短。攻击、护甲、伤害与破甲时序不变，默认整关保留原双敌／感知配置。
单场遭遇与营地等待／接近已有真实 RED→GREEN、生命周期回归及独立审查。正常窗口与录像已连续完成挡盾→机械爪开甲→反击击败→抓台探索→满血保留补给→归营；实操发现并修复 Q 后攻击误触机械爪重发失败，E 重发守卫保留。
已接入样板专用逐步引导：真实蓄力/开甲提示、可抓底面、收绳助推落台、满血补给保留及归营反馈；仅说明本段完成，不写整关结算。死亡/新局/继续重置，暂停隐藏且保留阶段。
专项及中文/英文 1.25 倍 UI 普通窗口整圈验证通过，并根据实图修正键位截断；详见 [逐步引导检查](docs/playtests/2026-09-08-basin-sample-guidance.md)。
样板劳动者现有独立分节造型：收锤蓄力、前倾横扫、低锤恢复，破甲才打开胸甲并露出核心；受伤只亮局部边缘。横扫保持真实危险轮廓，降低色面遮挡。原整关造型、碰撞和数值不变。
实际状态绑定、标题后启动、死亡重置与重新开局专项及普通窗口完整回路通过；这是程序化视觉候选，不是正式美术验收。详见 [劳动者动作检查](docs/playtests/2026-09-08-basin-laborer-motion.md)。
主角已加入真实护盾驱动的举臂/躯干支撑/披风承力姿态，以及举盾、收盾和接攻击过渡；地面挥击反向蓄力后前挥，不拉长武器。命中改为真实目标表面的短定向火花，音频位置、伤害、能量和过热规则不变。
修正向左举盾后原地攻击时“表现向左、真实攻击框向右”的分歧，动作读取本次实际攻击朝向；并让可替换的命中特效在入树初始化前就处于接触位置。这些属于共享人物表现层，默认整关也受益，不是新攻击瞄准玩法。专项和普通窗口样板闭环通过，详见 [主角战斗表现检查](docs/playtests/2026-09-08-player-combat-readability.md)。
盆地样板已接入分层岩壁、破损石拱、悬链及右侧砌石台面；两个背景槽独立可替换，真实顶边、碰撞和抓取规则不变。普通窗口完整回路与结构专项通过，仍是程序化候选而非正式美术；详见 [破拱与悬吊遗迹检查](docs/playtests/2026-09-08-basin-environment.md)。
样板战斗提示现区分蓄力、横扫中、收招仍有甲与实际开甲，并在敌人背侧显示真实生命细条，避开正面的攻击轮廓。紧急阶段缩短教学卡；局部提示被屏幕裁切时保留完整说明，暂停/死亡/新局不残留。专项和中英文普通窗口完整回路通过，玩法、时序、声音及默认整关 UI 不变；详见 [局部战斗提示检查](docs/playtests/2026-09-08-basin-combat-readout.md)。
战斗教学现读取实际气力、过热、按住右键和武器插槽：不能举盾时提示避让，不能攻击时说明松盾/选1/等待恢复，按盾阻挡机械爪时明确要求松右键。松键后不保留旧指令；已补耗尽气力后仍按盾的恢复陷阱。双语8情景、相关回归和中英文普通窗口“误用过热—恢复—完整回路”通过，数值/规则不变；详见 [可用操作教学检查](docs/playtests/2026-09-08-basin-combat-readiness.md)。
这些仍是正常引擎输入下的自动路线验证；新手理解、统一视听、用户品质评审和独立包交付未完成。匹配模板、样板专用导出入口、自包含运行时及白名单 staging 已实际跑通；运行时文件/资源包、完整 notices 也已核对。原生试玩又发现跨区死亡时恢复碰撞的时机错误，当前先修复失败恢复，再继续 [独立交付准备](docs/playtests/2026-09-08-basin-delivery-readiness.md) 和声音反馈。详见 [样板回路检查](docs/playtests/2026-09-08-basin-sample-loop.md)；入口基础见 [样板入口检查](docs/playtests/2026-09-08-basin-sample-entry.md)。
本次样板入口基于生产代码 `3b1406bf6a532c4f7ec5a0b5c74cb54b9ca2a357` 继续实现，默认整关与旧归档不变；盆地接缝相机测试草稿与 RED 证据保留，修复尚未完成。

2026-09-09 独立包开发进展：`6671d3b` 已修复 `7e13912` 启动时音频/本地化读取松散文件的问题，实际 Windows x64 包进入标题和营地，并从包内加载 .NET 运行时。PCK 的 14 JSON、76 音频 remap/sample 和 496 条目完整性已独立核对。用户操作期间未抢占键鼠；结束日志发现 22 条跨区死亡碰撞 flush 错误，因此该包保留为诊断候选，不发成品 ZIP。
现将复活整体延后到伤害碰撞回调结束，仍同步记录恢复请求/死亡前输入；真实跨区 AreaEntered 致死已有 RED→GREEN，连续两次恢复至营地、满血和实体地面通过。完成态提前拒绝无效恢复，新会话清理死亡表现，旧切房不抢占已接受的恢复；对应失败测试、恢复/记录器/人物/样板生命周期回归与独立审查通过。11.27 秒最终受控复现录像已抽帧审看，但不是正常探索/原生修复包/真人品质验收。玩法数值、美术和旧版本未改。最终源码盆地普通窗口闭环通过；整关有一次 Ascent 第三抓点后纯横移超时，随后不改代码、加现有输入/碰撞追踪的一次整关通过，不能据此称超时已修复。先定位这个未稳定复现的问题，再重导出、正常窗口复核和 ZIP 解压检查；见 [死亡恢复检查](docs/playtests/2026-09-09-basin-death-contact.md)。

## 持续迭代与归档

2026-09-09 控制参考源码修正：主菜单/暂停补清机械爪持按、收放绳、连接助推、松键脱离，去掉 A/D“瞄准”的误导标签；暂停说明重排为七行，修正实际测得的 231px 文本超出 206px 底板。中英设备往返与实际布局、原主菜单导航回归通过；持按/抓附限定各有变异失败验证。未改玩法或当前用户运行的 b98 包，也没有覆盖 ZIP；新的说明仍待图形审看与打包。详见 [操作参考检查](docs/playtests/2026-09-09-controls-reference.md)。

2026-09-09 02:02 起，用户回复“方便”后从新 b98 ZIP 解压目录打开独立游戏：标题、控制页、正常 Start 入营地和鼠标攻击能量反馈已实际观察，使用新隔离存档。窗口工具的 E/Esc 注入未生效，但用户随后亲手确认可以，主代理也实际见到机械爪选中+暂停菜单；不因此改输入。鼠标完成“返回标题→继续→营地”，隔离存档确认盆地坐标 `(2380,2014)`，未误记完成。已交还键鼠给用户试玩；通用收绳/助推操作参考正在补充，情境教学已有。原生死亡/完整回路、试听与品质评审仍待验证，目标保持 active，见 [本次原生窗口检查](docs/playtests/2026-09-09-basin-b98-package.md)。下方等待许可/键盘对照为历史状态。

2026-09-09 新私人候选 `2026.09.09-dev.b98cc47` 已另行打包，包含助推声音修复；旧 39b2eb8 ZIP 哈希未变。新包 199 文件在全新目录解压后逐项校验一致，候选与解压目录的隔离无窗口启动均实际退出 0。首次冷导入曾 0xC0000005 异常退出，后续独立导出实际退出 0，异常原因仍未确定并明确留档。新二进制尚未进行图形窗口、完整操作或听感检查，不能继承旧包的标题/营地证据。已连续三轮保留窗口空闲请求，现需用户允许操作与品质评审才能推进验收；不能再用后台打包代替该步骤。详见 [b98 新候选与待评审项](docs/playtests/2026-09-09-basin-b98-package.md)。

2026-09-09 声音反馈接续：已修复“先 W 收绳，再 Space 助推”时起音被同音色去重吞掉的问题；只恢复已接受助推的独立起音，连续 Reel/Swing、重复帧和被拒绝的第二次空中助推不重复响。真实 RED→GREEN、现有音频/VFX 与实际输入发布链回归通过，未更换音色、资源或玩法数值。39b2eb8 私人 ZIP 保持原样，尚不含这项源码修复；当前键鼠暂停，普通窗口新声音时机与听感仍待核验。详见 [助推声音检查](docs/playtests/2026-09-09-basin-boost-audio.md)。

2026-09-09 私人候选已推进到 `2026.09.09-dev.39b2eb8`：从冻结源码重新导出，原生标题/正常 Start 和包内 .NET 模块已实际核对；完整 ZIP 199 文件在全新目录解压后逐项哈希一致，含版本清单、操作说明、许可与未验收项。前台切换后暂停键鼠，修复包的原生整圈/死亡/继续与解压目录运行仍未完成，不称正式试玩版。两种上升段间歇失败保留；仅增加跨缝跳跃观测，未修改地图掩盖问题。详见 [私人候选检查](docs/playtests/2026-09-09-basin-private-candidate.md)。

按用户 2026-09-08 的要求持续优化序章，默认每 10 分钟触发推进一次，不保证每轮在 10 分钟内完成；
重大体验提升后固定版本、编号并核验 GitHub 归档，不按运行次数虚增版本。
十万愿意付费订阅玩家是待真实数据验证的商业目标，不是当前成果。
执行边界、发布门槛和机械爪/镜头开发修复接续点见
[持续优化与归档规则](docs/automation/prologue-continuous-improvement.md)。
2026-09-08 已打通 GitHub 连接：r1/r2 以独立筛选源码提交、带说明标签及 prerelease 完成历史归档，
8 个附件已实际下载并逐项核对 SHA256。仓库保持私有，`main` 未改；原本地包与未冻结 r3 工作树保留。
归档入口：[r1](https://github.com/FSBD-FX/OurGame/releases/tag/prologue-2026.09.08-r1-archive1)、
[r2](https://github.com/FSBD-FX/OurGame/releases/tag/prologue-2026.09.08-r2-archive1)。
这些是历史开发候选的源码包，不是新体验提升、免安装 EXE 或正式验收。

## 当前进度 · 2026-09-08

当前版本为 **2026.09.08-r2 开发试玩候选**：五个连续扇区、15 个微场景、11 条路段。
普通启动现在保留完整地形机械爪地图，入口会检查微场景与抓取配置，防止又启动陈旧的稀疏场景。
玩法包括墙面/天花板/底面/边缘抓取、Ice 与 Forbidden 材质拒绝、620px 射程、
每次成功抓附一次 Space 助推、两个检查点及 Boss 死亡/重试/完成流程。

本轮已调整 Boss 血条位置，修复开门后仍显示封闭门板、结算准星遮字和连续场景教学卡漏避让；
能量/热量条使用青绿、琥珀与过热橙红区分。第三上升锚点已下移，普通跳跃时可进入屏幕再瞄准。
五区增加低对比峡谷轮廓、断拱、齿轮、管道和破钟背景，并隐藏调试起落区域。
这些是可替换的程序化美术改进，**不代表正式美术或整体手感已验收**。

r2 进一步把圆片抓点换成五种材质轮廓：粗裂石、铆钉金属板、黄铜扣环、滑冰晶面、禁抓封板；
52 块地形增加低频接缝、板件与内侧阴影。10 个点目标与所有地形仍对应原有命中范围，
五区序列化玩法配置哈希与 r1 相同。皮肤不持有碰撞、抓取逻辑或逐帧重绘循环。

### 试玩入口

本机双击 [Play-Knight.cmd](Play-Knight.cmd)，或从工作区运行
[Play-Knight-2026-09-08.cmd](F:/Documents/游戏/Play-Knight-2026-09-08.cmd)。
这是源码试玩包，不是免安装 EXE。默认使用本机 G 盘 Godot 4.7.1 Mono 与 .NET SDK 9；
换电脑可运行 `tools/play-prologue.ps1 -GodotPath <Mono编辑器路径> -DotnetRoot <SDK目录>`。
Q/E 仅切换模式；左键执行，抓附时按住左键，W/S 收放绳，Space 助推，松开左键脱离。
右键防御会消耗能量并过热，不能无限举盾。完整按键见下方 Controls。

### 当前验证与剩余工作

机械爪后续开发检查：已修正松爪时上升惯性被普通短跳规则截断的问题，并补齐
“先冲刺、再松爪”顺序的边界，避免额外上升尾段或返还空中冲刺。最新专项测试通过；
pre/post 回归分别保留 2/3 条来自刻意异常用例的警告，没有新增测试错误。
正常窗口、非固定帧的自动全路线已连续两次通过；固定帧录像也跑完五区并已抽帧审看。
普通主场景窗口启动退出 0。详细命令、证据边界和剩余检查见
[机械爪惯性开发记录](docs/playtests/2026-09-08-grapple-release-motion.md)。
这些属于尚未冻结的 r3 开发工作，不改写 r2 归档，也不代表真人手感或稳定 60 FPS 验收。
第二次惯性检查实时采样仍有一次 57.208ms 回调间隔，历史峰值保留，不与后续样本混算。

上升区新增平滑的 120px 上方视野：第三抓点在站立发射点从屏外 `y=-35.99` 进入 `y=84.01`，
不必先跳起来找目标。没有改地图、跳跃、机械爪参数、缩放或美术资产。
跨区往返、检查点、新局、world 单独退出后相机还原均有回归；退出还原还经过删除修复行的失败验证。
正常速度自动全路线、完整 pre/post 和普通主场景启动通过；新全路线录像及上升区 12 秒片段已抽帧复核。
本轮仍是未冻结开发检查点；详见 [上升区视野记录](docs/playtests/2026-09-08-ascent-camera-headroom.md)。
后续已检查第三抓点主动松爪回落：固定上移曾让台面距屏底仅约 14px。
现在松爪上升减速时平滑回中，首次下降台面从 `y=706.33` 到 `643.73`；
18 个早期下降采样由 11 个超出 48px 安全边距变为 0 个，实际落回原台且可继续通关。
不改变轨迹，站立和重新瞄准仍恢复上方视野；新的 Aim 回归经过删除状态白名单的失败验证。
完整 pre/post、普通启动和带 `--check-ascent-return` 的正常窗口自动路线通过，录像已抽帧审看。
这仍是未冻结的小改，见 [松爪回落视野记录](docs/playtests/2026-09-08-ascent-return-camera.md)。
后续把 1–4 道具栏移至右下安全边缘，保持原尺寸、字号与 Q/E 位置。
默认 1280×720 回落专项中，18 个早期下降采样的道具栏交叠由 5 次降为 0 次；
四分辨率×三档 UI 缩放、完整 pre/post、正常窗口路线及普通启动通过，录像已独立抽帧复核。
这不是全关无遮挡：右下其他平台边缘仍有局部遮挡，大字号模式也未解决世界遮挡。
记录见 [道具栏避让回落平台](docs/playtests/2026-09-08-loadout-dock.md)。
机械爪教学已分阶段：入口卡只讲 E、瞄准与按住左键，真实附着后显示摆荡、收绳、一次助推及松手。
默认构图入口卡从7行缩至3行；实际松手且目标引用尚存时也恢复路线提示，Boss/机关教学不被替换。
双语/键位/状态边界、四分辨率排版、pre/post、正常窗口全路线及普通启动通过，录像已独立抽帧复核。
记录见 [机械爪分阶段提示](docs/playtests/2026-09-08-contextual-claw-guidance.md)。
审图另复现原有“仍在收绳但连接线消失”：第三锚点96个物理帧中约束继续、绳线被清空，并非新提示滞留。
后续已补遮挡反馈：近侧绳线在首个遮挡面前渐隐，角色侧保留收绳环；进入单向平台内部时也不恢复整绳。
发射准入和牵引物理不变，恢复视线/松手/卸载会正确还原或清理。真实插墙与内部边界有 RED/GREEN，
pre/post、正常窗口五区全路线通过；第三段117个视觉遮挡采样含92个截断绳、25个局部提示。
普通窗口启动及硬件录像通过，主/独立抽帧确认遮挡、恢复和松手；当时局部青环偏小、易混入手部装饰。
这不是物理绕绳或正式美术完成；记录见 [机械爪遮挡反馈](docs/playtests/2026-09-08-claw-occlusion-feedback.md)。
后续已把遮挡连接环独立提层并绑定真实手腕，适度放大、增加窄描边；不改变整绳层级或物理起点。
四姿态实际像素验证、换皮/重新挂载 RED/GREEN、最终 pre/post、正常速度全路线和普通启动通过。
主/独立审看同时间原图，手环较旧版清楚，样本未见新增遮脸或挡平台；松手和恢复视线正确消失。
手环与身体出绳点仍分开，不宣称完美衔接、全部动作或真人辨识验收。详见
[手腕连接提示记录](docs/playtests/2026-09-08-claw-wrist-cue.md)。
已复现并修复助推后的抓附姿态中断：Boost 继续沿用抓附动作与线轴张力，不再把手臂清空后重新抬起。
真实控制器/物理/呈现调度专项、pre/post、正常速度五区路线及普通启动通过；前后短录像已独立逐帧复核。
专项注入的是已采样输入帧，不是实体键盘端到端测试；未改助推力度、跳跃或冲刺，也未新增独立助推动画。
详见 [助推姿态连续性](docs/playtests/2026-09-08-claw-boost-continuity.md)。
首场交战实测发现并修复锈壳侍从的视线自遮挡：自己的攻击框不再被当成墙，外部墙和区域仍可遮挡。
恢复追击后又复现针剑的视觉过伸；只调整刺击姿态及插值，保留26×18伤害框与原前摇/伤害/恢复时长。
前后实景两次攻击的18个Active采样中，针尖中心距真实伤害矩形的最大超界由52.27px降至2.56px。
双向自动物理、针尖包络与阶段连续性回归、正常速度交战/五区通路、普通启动及独立审图通过。
这是本地开发小修，不是首战上手/审美验收；近身身体和武器仍交叠，见
[首敌视线与刺击范围记录](docs/playtests/2026-09-08-first-enemy-readability.md)。
首战输入实测另复现“同帧松右键并新点左键”丢失反击：攻击读取了尚未刷新的一帧旧盾状态。
现已按最新松盾输入解除该阻挡，持续举盾仍禁攻、旧左键不会自动重放、自然过热后仍可攻击。
四组自动InputMap回归、正常窗口格挡反击、pre/post、五区路线及普通启动通过；前后短录像已独立抽帧复核。
没有改护盾/能量数值、攻击缓冲、地图或美术，见 [松盾反击衔接记录](docs/playtests/2026-09-08-shield-attack-handoff.md)。
首战情境提示已接入：攻击模式近敌时，显示鼠标瞄准、RMB防守、刺后松盾再点LMB反击；
左上临时标题不修改真实任务，切模式、离开、遮挡、敌死或关闭指导后恢复当前任务。重试两次也保留短卡。
双语显示/恢复、距离滞回、8组排版、真实格挡反击、pre/post、正常五区路线及普通启动通过，前后短录像已独立抽帧复核。
新回归暴露了原有入口ReachBasin目标无法解析DescentIce的警告与英文ENERGY尾字遮挡，已由下述接续小修处理。
没有强制击杀、重排主线或更改数值，见 [首战情境提示记录](docs/playtests/2026-09-08-first-combat-teaching.md)。
首战后按 E 的入口卡现指向脚下连续平台右端，实际进入下降区才切换下行教学；不再索要尚未进入分区的冰面。
英文资源标签按真实字宽留位，保持字号、资源区边界和条右端，仅必要时略缩短条；中文可恢复原布局。
有效RED/GREEN、12组真实标签测量、中英正常窗口防反接跨区、pre/post、普通启动与正常五区路线通过。
前后约6秒硬件录像已独立抽帧复核；当时发现的下降卡遮住第一块小平台一角，已由下述接续小修处理。
本轮仍为本地检查点，非整包交付/真人验收，详见 [入口出口指引与资源标签](docs/playtests/2026-09-08-entrance-route-guidance.md)。
下降提示现避让已标记安全落脚台的碰撞顶边，不再把这些平台与普通墙面一起排除。
固定帧相同D下行的136采样中，前两台顶边遮挡由1次降为0次，文字全部可见；两次实际落台无需跳跃。
完整pre/post、中英正常速度下行、正常五区通路与普通启动通过；短片及全路线的上升区已独立抽帧审看。
保护范围仅为既有17个标记平台，并非全地形：其他平台/金门内部遮挡、HUD与锚点重叠和卡片位置切换仍未全解决。
没有改地图、跳跃、机械爪或战斗规则，也没有新资产；详见 [下降教学卡与落脚台](docs/playtests/2026-09-08-descent-card-clearance.md)。
下降第一小平台现有可选首爪提示：标记指向真实可抓的粗糙天花板命中点，说明 E/按住左键，同时保留 A/D 直接下行。
只在真实站在该台、目标可见/可抓/射程内且未持盾时显示；抓附后标记跟实际锁点，松手恢复下行提示。
已用真实输入从首台抓顶、W/D 牵引、松手落到右台，中英文各通过；完全不用爪的双平台下行仍通过。
pre/post、普通启动及正常五区通路通过；该次通路有一次60.930ms回调间隔，不称稳定60fps。
无地图/移动/抓取规则或新美术变化，仅本地检查点，见 [下降首爪指引](docs/playtests/2026-09-08-descent-first-claw.md)。
机械爪附着时现在持续显示现有手腕线轴环：按真实绳长收放旋转，停绳/到最短限制时不空转，松爪清除。
修复前该环只在遮挡时出现；没有改环大小、层级、输入、牵引力或路线。模型像素RED/GREEN与正常首爪、五区回归通过，
前后短片已独立抽帧审看；详见 [收绳反馈连续性](docs/playtests/2026-09-08-claw-reel-feedback.md)。
盆地回收台已从空奖励标记接入一次性生命补给：受伤接触恢复至多2格，满血保留，领取后开箱、显示实际恢复量并保存。
沿现有入口高台自然下落可领取，再从回收台左侧返回主线；不增必经任务、地形障碍、跳跃或机械爪能力。
死亡/继续游戏保持已领取，新局重置；回血回调切换新局不会污染新存档，返回标题仍保存原领取，换皮标签也可正确刷新。
详细证据与自动路线边界见 [盆地可选补给闭环](docs/playtests/2026-09-08-basin-salvage-supply.md)。这不是正式美术或真人探索验收。
盆地无效货台交互已退为低对比背景残骸：旧货台抓点距石锚仅20.88px，近旁瞄准会选中货台并被遮挡拒绝。
现在同一瞄准位置选中石锚，真实持左键/W收绳和松手返回通过；没有新增升降台、捷径或改动牵引参数。
详见 [盆地货台误导交互清理](docs/playtests/2026-09-08-basin-cargo-target.md)。当前场景只保留三个旧类机关，
旧Task6“四类机关”发行认证仍保留且不满足；活跃结构测试修订不等于旧认证通过，r3不得据此冻结。
最终完整pre/post、正常窗口抓取/五区路线与普通启动已复跑；旧四类审计也实际重跑并明确失败，未隐藏或放宽门槛。
盆地主地面重返补给台已实走：抓住既有上方平台底面，收绳、一次附着Space助推后落台，领取补给再回主地面。
没有普通跳跃、冲刺或恢复传送，也没有改地图/物理参数。盆地LearnShield期间真实抓附会临时显示爪操作与实际锁点，
松爪后恢复原护盾教学，不推进任务；中英文正常窗口及前后短片均验证。详见
[盆地补给返程与抓附提示](docs/playtests/2026-09-08-basin-supply-return-guidance.md)。
这是从作者检查点出发的自动输入专项，不代表真人能自行发现路线。左上任务标题、两次失败后的盆地护盾Hint保持原样。
完整pre/post、原高台补给路线、正常五区主线和隔离存档普通启动也通过；没有改变旧四类发行门槛或冻结r3。
盆地重型敌人的穿台伤害已复现并修正：旧路线站台时HP5→2，新版相同停留HP5、无隔台新起手或伤害。
实体地形同时限制该敌人的发现准入与接触伤害；合法起手仍完成原前摇/攻击/恢复，同层主动走近后仍有两次真实命中。
只有该敌人启用接触过滤，不改变其他Hitbox默认规则，也没有修改地图、伤害数值或美术。
最终完整pre/post、正常五区路线、独立存档普通启动及前后视觉审查通过，无新增严重阻塞；仍仅本地开发检查点。
详见 [盆地重型敌人地形遮挡](docs/playtests/2026-09-08-basin-laborer-terrain-occlusion.md)。
右侧补给台下84px敌人被78px净空卡住的问题已单独修正：台面、补给与上层标记整体抬56px，净空134px。
敌人保持原尺寸/速度/伤害，真实自行前进约128px并进入攻击；上层仍安全，两种补给往返路线均无新增跳跃或恢复。
恢复追击后，整关测试在正常受伤无敌期内注入致命伤的问题已单列处理：只等待无敌自然结束，不改生产保护规则。
正常窗口五区、四次恢复、门、Boss重试与结算重新通过；这仍是自动输入证据，不能代签真人体验。
最终串行pre/post及独立存档普通启动通过，既有中段缺目标/负向测试警告仍如实保留。
详见 [盆地敌人通行净空](docs/playtests/2026-09-08-basin-laborer-clearance.md)。左侧SupplyCave低台仍待整理；
盆地最右缘角色部分出画、贴身攻击重叠仍未解决，下一轮先核查跨区接缝视野，不把局部修复当成真人上手验收。
双重锁点符号与卡片换位仍有视觉噪声。
落地回摆舒适度及初次上手仍待真人体验。

本轮已运行界面/门板/教学避让集成回归、post 阶段关卡回归和新完整流程录像。
post 回归覆盖 11 路段各三个采样起点及扇区往返压力；不是两次完整独立认证。
最新证据、命令与已知边界见 [r2 材质润色记录](docs/playtests/2026-09-08-r2-material-polish.md)，
前一轮修复见 [r1 交付记录](docs/playtests/2026-09-08-r1-delivery.md)。
固定帧录像是自动输入主路线加后置动作特写，不是人类通关或正常速度帧率证明。
正式美术、初次游玩难度、持续实测手感与两次完整独立认证仍未完成，目标保持进行中。

`bac0358` 候选录像保留为失败的视觉审查证据：Boss HUD 遮挡了横扫和地波。
外部[录像清单](E:/OurGame/Knight/.evidence/task6-certified-bac0358-20260902T014812128Z/capture/capture-manifest.txt)
记录了该候选；最近一次实际独立验证 #1 的 [Pester 日志](E:/OurGame/Knight/.evidence/task6-certified-bac0358-20260902T014812128Z/verify-1/pester-base-external-evidence.console.log)
为 5 项通过、3 项失败，失败点是测试基础设施无法识别 `Get-FileHash`；#2 尚未运行。
旧录像哈希、旧性能数字及“两名独立验证者通过”的记录不再作为当前候选版的认证依据。

本轮另发现普通启动使用的五个场景陈旧，缺少 `MicroScenes` 与 `GrappleSurfaceProfile`；
录像工具结束时会还原这些旧场景。因此，自动录像不能证明普通启动交付的是相同地图。
本轮已重新生成并保留正确场景、添加启动前检查，并用直接录像命令绕过旧工具的还原步骤。
这些修复的当前验证见上方交付记录；旧版认证不自动续用。自动化不会创建
`HandsOnLauncher` 真人报告，也不能代填操作感受或证明首次通关时间。

| Task 6 asset area | Current source | Status |
| --- | --- | --- |
| Canyon terrain, surfaces, checkpoints and claws | Project-authored generated sector scenes | 5区/15微场景保留于普通启动；开发候选 |
| Canyon background | `CanyonBackdropVisual` static layered drawing | 新增5区低对比地标；可替换，无新付费资产 |
| Basin cargo remnant | `CanyonContentBuilder` / `VisualRoot/Midground/CargoRemnant` | 原高亮货台退为背景Node2D+2个Polygon；无抓点/碰撞/动作，不再误导为升降台；可替换程序化占位，非正式美术 |
| Canyon platform / point-target skins | `CanyonPlatformVisual` / `CanyonAnchorVisual` | r2：52平台、10抓点；按材质绘制，可替换，无新增碰撞 |
| HUD / reticle / guidance | Existing typed UI with revised placement and state rules | 分阶段抓爪/首战短卡、入口出口、资源标签和安全台顶边避让；盆地LearnShield抓附时复用现有爪卡与真实锁点，不写任务/数值，无新美术。重试Hint/任务标题竞争、金青叠标、未标记地形遮挡及真人首读仍待改 |
| Ascent traversal camera | Existing Camera2D local-position blend | 松爪减速回中、站立/新Aim恢复上方视野；本轮未改相机，无新增美术 |
| Attached claw connection | `ThreadLineVisual` / `ThreadShuttleVisual` procedural VFX | 近侧截断渐隐；附着期间持续显示已有手腕环，按真实收放绳旋转，换皮路径保留；无新资源/付费，未移动实体绳起点 |
| Attached boost action | Existing `PlayerPresentationCoordinator` / formal rig | Boost 保持抓附姿态与线轴张力；修复可替换特效的 cue 路由，无新美术或助推物理变化；尚无独立助推动画 |
| Shield-to-attack handoff | Existing `PlayerActionModeController` / formal rig | 松盾同帧的新左键可反击，现有首战短卡提示释放顺序；原动作、能量和过热规则不变，无新增或付费美术；真人首读仍未验证 |
| Basin optional supply | `CanyonContentBuilder` / `PrologueSupplyCache` | 一次性至多2格生命；右台和箱体同步抬高56px，原上层下落/地面抓爪上返均验证；可替换闭开箱及状态文字，无付费，非正式美术 |
| Basin heavy-enemy encounter | Existing `BellBackLaborer` / formal rig / convex sweep | 实体地形阻止跨层攻击；右补给台下净空134px恢复原AI追击，未缩身体或改素材/数值。左低台、右缘出画、贴身白色范围及真人首读仍待修 |
| Capture/keyframes | `.evidence/september08-r2-material-proof` | r2全流程+材质像素检查；r1和失败证据保留；不混入源码包 |
| Paid or officially commissioned art | None | No paid generation authorized or used for this revision; project-owned local/procedural art only |

See [the Task 6 validation record](docs/playtests/2026-08-29-terrain-grapple-dense-canyon-validation.md) for the evidence history. Its older pass claims do not override the current open work above.

## Historical six-room milestone (superseded)

以下六房间、离散单次机械丝牵引及旧 J/K 输入的段落保留为历史实现记录，已被当前地形机械爪候选方案取代。
本节的“now”“current”和自动验收数字均指当时的基线，不代表 2026-09-08 候选版已通过；当前操作见 Controls。

The complete production prologue first level is playable from awakening through the Fallen Bellkeeper and closing echo, but its sensory-quality milestone is active rather than complete. The latest hands-on run did not finish: it recorded 30 deaths (5 ordinary and 25 Boss), 25 Boss attempts, and stopped in `FallenBellkeeperArena / DefeatBellkeeper`. The current goal is to make enemy attacks predictable, mechanical thread smooth and explicit, player/enemy actions continuous, and all six rooms, HUD, VFX, and audio clearer before asking for a fresh 10–15 minute completion.

The approved direction is defined in the [combat readability and presentation design](docs/superpowers/specs/2026-08-21-combat-readability-presentation-design.md). Execution is split into five independently playable milestones: [attack readability and first-level difficulty](docs/superpowers/plans/2026-08-21-attack-readability-and-difficulty.md), [mechanical-thread casting](docs/superpowers/plans/2026-08-21-mechanical-thread-casting.md), [player and enemy action motion](docs/superpowers/plans/2026-08-21-action-motion-system.md), [environment/UI/audio polish](docs/superpowers/plans/2026-08-21-environment-ui-audio-polish.md), and [complete first-level experience validation](docs/superpowers/plans/2026-08-21-first-level-experience-validation.md). Each milestone must keep the game runnable, pass its focused tests, produce reviewed visible evidence, and feed real playtest findings back into centralized tuning.

The attack-readability implementation now has automated acceptance for source-aware damage deduplication, the Grave-Lamp Moth's World-clamped fixed-direction dive, and the Bell-Back Laborer's authoritative 120-degree convex sweep in both facings. Moth warning and Active geometry share the real collider: the locked capsule stops at the same first contact where the no-slide dive enters Recovery, including diagonal World contact. Fresh 2869-frame journey and 600-frame retry captures pass their exact 1280×720/60 FPS contracts and merged console/leak scans; their final three frames retain the intended title/Boss view instead of recording teardown gray. Whether these tells read comfortably at normal speed for a first-time player remains a deliberately open hands-on question rather than an automated claim.

Mechanical-thread casting now has a stable real shuttle at `Player/VisualRoot/ThreadShuttle`, live compact Chinese HUD feedback for Ready, Preview, Windup, Travel, Latched, Apply, Recovery, Failed and Cooldown, and real-input smoke coverage for neutral tap, directional hold, named wall obstruction, room/death cleanup and Boss-chain exposure. The deterministic 60 FPS journey records the successful preparation-to-recovery sequence as one cast and separately records no-target and wall-obstructed failures. This is automated readability evidence; normal-speed feel and first-time comprehension remain open for the next hands-on run.

The first-level traversal contract is now explicit and automatically proven. The player has one ordinary jump only—there is no double jump—and exactly one directional air dash can be accepted during each airborne cycle; a later Shift press in that same airtime is rejected without a second burst. Dash is an optional advanced correction and is not required by any of the ten mandatory route contracts. Mechanical thread is a discrete one-cast pull toward an authored anchor, not an elastic swing or a persistent tether. Every assisted route uses one real Apply, normal thread cleanup, and a later supported landing.

Player, Rust-Shell Squire, Grave-Lamp Moth, Bell-Back Laborer and Fallen Bellkeeper action motion now passes the automated 60 FPS continuity gate across all eleven named locomotion, attack, thread, hurt and Boss transitions. The three ordinary enemies use live generated replacement rigs: every stable `Body`, `Weapon`, `Shell`, `Wings`, `Lamp`, `Bell`, `Feet` and `Corruption` node is the real typed renderer and phase-driven transform owner, so future art inserted beneath a slot inherits that slot's motion. Corruption is kept behind the readable silhouette, weapon and face layers. The current automated evidence includes a zero-warning/error build, exactly one `PROLOGUE_TESTS_OK`, an 18.82 ms production-audit peak under the 50 ms budget, all eleven motion markers, seven independently judged target-marker lifecycles, 364 danger-geometry frames, and fresh 2869/600-frame AVI plus PNG captures. This completes automated action-motion acceptance only: normal-speed human taste acceptance and the overall prologue/traversal milestone remain pending.

The six prologue rooms now use the approved “Dusk-Bell Industrial Ruin” layered direction through a typed room descriptor catalog. Their large-form landmarks are respectively the broken courtyard clock, severed corridor gear train, upper pendulum chains, lower copper pipes with fungus, vertical Bell-Chain, and the Boss arena's backlit broken bell. Every room retains the seven replaceable art leaves and five depth roles, collision-derived platform skins, deterministic atmosphere, and independently fading foreground leaves. A reviewed 120-frame 1280×720 proof shows all six silhouettes together; focused structure/runtime tests and the full prologue runner pass, including an opaque-foreground mutation that fails the named combat-protection assertion. UI and audio polish remain open in the same milestone.

Development began prototype-first: P0 core slice, P1 loop, P2 Boss closure, then P3 polish and evidence. P4 now provides the complete production UI, formal art, animation, VFX, original audio, settings, atomic save/continue flow, localization and accessibility contracts defined by the [full production specification](docs/superpowers/specs/2026-08-17-prologue-full-production-design.md) and [implementation plan](docs/superpowers/plans/2026-08-17-prologue-full-production.md). Gameplay tuning, encounters, rooms, Boss timing and presentation remain exposed through bounded configuration contracts so presentation work does not rewrite gameplay.

Formal production is proceeding under a zero-pay-per-use boundary. The current running build provides the project-owned ten-layer Thread-Spool Bearer rig, three enemy silhouettes, the five-part Fallen Bellkeeper rig, seven stable art leaves for all six rooms, formal mechanism visuals, typed VFX/camera feedback, production UI, and a locally synthesized 48 kHz score/SFX library. These are working replaceable foundations and regression baselines, not final sensory acceptance: the active milestone will refine their timing, silhouettes, layering, density, and feedback without changing stable gameplay roots. The rejected A2 dressed character and rejected idle sheet remain evidence only and cannot become runtime art or animation masters. No paid generation is authorized.

The production UI foundation is now live in the generated build: the bilingual title/settings/controls shell, five-piece porcelain health display, continuous mechanical-thread spool, objective ribbon, first-level guidance panel and world markers, contextual prompt, Boss phase/armor bar, formal pause screen, recovery veil, and exact session completion card share one safe-area layout. The HUD is validated at 720p, 1080p, 1440p, and 4K and is driven by the existing health, thread, recovery, Boss, progress, and completion events.

The player gray-box has been removed. `Player.tscn` now mounts `PlayerFormalVisual.tscn`, a compact independent-layer rig with body, near/far limbs, needle weapon, cloth mantle, shoulder core, hovering lantern and tether. Idle, move, rise, fall, land, dash, ground/air/down attacks, thread cast, hurt, dead and recovery are explicitly mapped; facing mirrors only the assembled root on X, while the mantle, lantern and tether animate independently without stretching the whole character.

What is playable now: all six connected formal-art areas plus the earlier one-room technical prototype. Awakening Courtyard and Broken-Tooth Corridor teach the base controls, first combat, checkpoint and anchor traversal. Broken-Tooth splits into the vertical Upper Pendulum route (three anchors, one Moth and the persistent hidden alcove discovery) and the tighter Lower Buried Machinery route (one Rust-Shell plus one Moth); both reach Bell-Chain Mechanism and can be backtracked through the unchosen branch. The convergence room contains the Bell-Back Laborer, a generic mechanical-thread chain interaction, and a permanent shortcut to the corridor checkpoint. It leads into the two-phase Fallen Bellkeeper arena: direct phase-one attacks, a non-damaging 60% transition, armored phase two, thrown-hammer thread exposure, defeat, and fast full-state retry are playable. The ordered objective HUD closes on the restrained “last gravekeeper” clue; Escape/Start opens a five-command pause menu with controls, session-only guidance toggle, resume, checkpoint return and quit. Mixed encounters rebuild completely on checkpoint reset while the opened shortcut and hidden discovery persist. The complete prologue is the project main scene. Dynamic guidance is mounted only in this complete prologue first level; the earlier technical prototype and future levels do not inherit it through an Autoload.

The first-level Bell-Chain target alone opts into a `260px` near-field fallback for mechanical thread. Neutral `K` is verified at that mechanism; ordinary anchors, enemies and the Boss hammer chain remain directional. This is a target-local accessibility allowance, not a global widening of thread aim. The first-level guidance diamond resolves to the mechanism's actual `ThreadTarget`, so its marker and the visible thread endpoint coincide.

### Historical six-room traversal route

`OptionalAdvanced` currently describes the air dash only; none of the mandatory contracts below require it.

| Room | Contract ID | Kind | Support / anchor intent |
| --- | --- | --- | --- |
| Awakening Courtyard | `spawn_to_jump_step` | `SingleJump` | Spawn floor to the supported jump step; Space only |
| Awakening Courtyard | `jump_step_to_attack_ledge` | `SingleJump` | Jump step to the supported combat ledge; Space only |
| Broken-Tooth Corridor | `entry_to_anchor_landing` | `Assisted` | One jump plus one discrete pull to `Geometry/ThreadAnchor`, then supported landing |
| Upper Pendulum | `entry_to_lower_swing` | `Assisted` | Entry floor to LowerSwing through `Geometry/AnchorLow` |
| Upper Pendulum | `lower_to_mid_swing` | `Assisted` | LowerSwing to MidSwing through `Geometry/AnchorHigh` |
| Upper Pendulum | `mid_to_high_swing` | `Assisted` | MidSwing to HighSwing through `Geometry/ReboundTarget` |
| Upper Pendulum | `high_to_mechanism_landing` | `SingleJump` | HighSwing to the supported mechanism landing; no Dash/thread |
| Lower Buried Machinery | `entry_to_arena` | `SingleJump` | Route entry to the supported encounter floor |
| Bell-Chain Mechanism | `arrival_to_chain_floor` | `SingleJump` | Convergence arrival to the chain interaction floor |
| Fallen Bellkeeper Arena | `retry_to_arena_floor` | `SingleJump` | Retry entrance to the supported Boss arena floor |

## Run and verify

Commands below remain useful entry points. Older P0/P1/six-room capture counts and audit numbers
are historical baselines, not the current Task 6 result. The current verifier is under repair;
no command or old success marker here constitutes a fresh pass.

Build, import, unit tests, and the deterministic runtime smoke:

`powershell -ExecutionPolicy Bypass -File tools/verify.ps1`

Verify the complete prologue gate (20 leaf-to-root builders, semantic drift checks,
unit/integration tests, four-resolution UI/accessibility/visible-fallback/performance audits,
and the real-input critical path through Boss completion):

`powershell -ExecutionPolicy Bypass -File tools/verify-prologue.ps1`

Run only the production-quality audit (1280×720 through 4K UI, strict formal-asset mode,
six-room visual traversal and deterministic performance budgets):

`powershell -ExecutionPolicy Bypass -File tools/audit-prologue-production.ps1`

The historical action-motion production audit peaked at 18.82 ms, below the 50 ms budget,
at 369 nodes, with no visible Placeholder/Debug/Graybox class, unresolved texture/string/font,
engine error or exit leak. That historical full runner printed exactly one `PROLOGUE_TESTS_OK`.

Verify the current P0 prologue slice (Player → Rust-Shell → two rooms → persistent level → prologue tests → real-input smoke):

`powershell -ExecutionPolicy Bypass -File tools/verify-p0.ps1`

Both gates regenerate their owned scenes in leaf-to-parent order, reject semantic scene drift, restore the checked-in scene bytes after generation, then run import, unit/integration tests, and smoke without leaving generated-ID noise in the worktree.

Run the room:

`& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path .`

Run the complete prologue directly (this is also the project main scene):

`& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path . --scene res://scenes/prologue/PrologueLevel.tscn`

Capture the historical deterministic 10-second P0 proof route:

`powershell -ExecutionPolicy Bypass -File tools/capture-p0.ps1`

Use `tools/capture-p0.ps1 -FramesOnly` when numbered PNG evidence is preferable to AVI.

Capture the historical deterministic 15-second P1 branch-loop proof (upper route, hidden alcove,
lower encounter, Bell-Back Laborer, Bell-Chain opening, and shortcut return):

`powershell -ExecutionPolicy Bypass -File tools/capture-p1.ps1`

Use `tools/capture-p1.ps1 -FramesOnly` to produce 900 numbered PNG frames for visual review.

Capture and validate the complete prologue plus the uncut Boss death/retry proof:

`powershell -ExecutionPolicy Bypass -File tools/capture-prologue.ps1`

Use `tools/capture-prologue.ps1 -FramesOnly` for exact 2869-frame and 600-frame PNG sequences. The fresh reviewed proof is 1280×720 at 60 FPS: 47.8167 seconds for the complete journey and 10 seconds for the uncut Boss death/retry. The AVI files are `screenshots/prologue/prologue.avi` and `screenshots/prologue/boss-retry.avi`; numbered PNGs are under `screenshots/prologue/prologue-frames` and `screenshots/prologue/boss-retry-frames`. The compact traversal review set is under `screenshots/prologue/traversal-review` and includes the ordinary jump, same-airtime accepted/rejected Dash, failed-cast feedback/cleanup, two consecutive exact-anchor frames per assisted segment, each supported landing, and the real mechanism arrival. Automated traversal evidence is complete; normal-speed human review remains pending and must still confirm that no mandatory route suggests an unknown combo or double jump, the pull feels sufficient, and prompts/anchors read comfortably. Its captured mix peaks at -2.94 dBFS with no sample above -0.9 dBFS. See the [full-production certification](docs/playtests/2026-08-18-prologue-full-production.md), [guidance audit](docs/playtests/2026-08-14-prologue-guidance-audit.md) and [Bell-Chain aim-assist verification](docs/playtests/2026-08-15-bell-chain-aim-assist.md). The [playtest and completion audit](docs/playtests/2026-08-14-prologue-first-level.md) defines the evidence boundary, and the fresh first-time human 10–15 minute completion pass remains open.

Regenerate generated scenes in leaf-to-parent order:

`& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/art/BuildPlayerFormalVisual.cs`

`& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/BuildPlayer.cs`

`& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/art/BuildEnemyFormalVisuals.cs`

`& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/BuildRustShellSquire.cs`

`& 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --script res://tools/build/BuildPrototypeRoom.cs`

Capture the deterministic 12-second, 60 FPS proof route:

`powershell -ExecutionPolicy Bypass -File tools/capture-prototype.ps1`

If AVI is unavailable, use numbered PNG frames:

`powershell -ExecutionPolicy Bypass -File tools/capture-prototype.ps1 -FramesOnly`

`-SkipSmoke` remains available only for focused build/import/unit debugging.

## Controls

当前采用后续批准的纯键盘规则；旧b98试玩包及下方历史鼠标表不能作为现版操作指南。完整真人手感与新短喷仍未验收。

| 操作 | 当前键盘规则 |
| --- | --- |
| 水平移动／摆动 | A/D或左右方向键 |
| 八向方向配合 | W/S或上下方向键，结合A/D；不是必须按W才牵引 |
| 普通跳跃／附着借力跃出 | Space；不提供未附爪的普通二段跳 |
| 攻击／机械爪／能量盾 | J／K／L；K点按自动牵引，再次K取消；L按住，受能量／过热限制 |
| 环境交互 | Q；不是背包道具使用 |
| 槽位／暂停 | 1–4／Esc；目前仅槽1可攻击，2–4空槽 |
| Shift | 短喷推进器；八向配合，喷完保留惯性，A/D反向制动。实际Player专项通过，完整路线／最终视听／真人手感仍未验收 |

### 历史键鼠方案（已被纯键盘替代，不作为当前指南）

以下保留历史决策，不恢复Q/E切模式、鼠标攻防或“J/K失效”的旧要求。

| Action | Keyboard / mouse |
| --- | --- |
| 移动 / 附着时摆动 | A/D 或左右方向键 |
| 瞄准机械爪 / 盾方向 | 鼠标指针 |
| 方向输入 | W/S 或上下方向键；冲刺方向可结合 A/D，不控制机械爪收放绳 |
| 普通跳跃 / 附着跃出 | Space；未挂接时普通跳跃，成功挂接后一次助推并松爪 |
| 冲刺 | Shift；方向由键盘方向输入决定 |
| 切换攻击模式 | Q |
| 切换机械爪模式 | E |
| 选择当前槽位 | 1–4；攻击模式目前只有 1 号槽可用，2–4 为空槽 |
| 执行当前模式动作 | 鼠标左键；机械爪点按发射并自动牵引，松开鼠标不取消，活动时再次点按取消 |
| 能量盾 | 按住鼠标右键；先中断机械爪，能否举盾仍受能量与防御规则限制 |
| 暂停 | Escape |

手柄仍保留 InputMap 映射，但本轮尚未完成手柄端到端验证，因此不把旧手柄按键表作为当前完整操作指南。
空中换抓点并成功抓附后可以再次 Space 助推；这不提供脱离机械爪的普通二段跳。

## Hands-on playtest

Launch the current complete prologue with the pinned Godot Mono and .NET runtimes:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1
```

First-level guidance starts enabled for every new level session. Use `Pause > GUIDANCE: ON/OFF`
to hide or restore its instruction text, arrow and target diamonds; this toggle is session-only and
is not saved across a fresh level instance.

Only this launcher grants a `HandsOnLauncher` evidence source; normal editor runs and deterministic
capture drivers do not write human evidence. Every granted run creates non-gameplay telemetry under
`%APPDATA%\Godot\app_userdata\Knight\playtests\prologue`. It records unpaused elapsed time,
room order, first branch, checkpoints, ordinary/Boss deaths, Boss attempts, retry delay, shortcut,
hidden alcove, recovery time-to-control and completion. The launcher verifies .NET 9, builds current
source and refreshes Godot's C# import before opening the game. It never changes gameplay state.
After closing the game, open the
latest Markdown report with:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\play-prologue.ps1 -OpenReportOnly
```

The report intentionally leaves qualitative prompt/feel fields for the human player. A 10–15 minute
first-clear certificate still requires a completed hands-on run; deterministic presentation captures
are not substituted for human timing.

In the historical Task 3 Phase A baseline, the centralized Boss guidance fallback failed closed: an
all-blocked layout suppresses and clears the card rather than accepting unsafe geometry. Fresh
real-route captures clear all four Boss attack reads and the bottom Boss HUD, and process-driven
live-Boss guidance was checked at five UI resolutions. That baseline's verifier result does not
certify the current Task 6 HUD: its latest reviewed capture failed for Boss attack occlusion.
Current capture, verifier and hands-on work remain open. See the
[combat readability presentation audit](docs/playtests/2026-08-21-combat-readability-presentation-audit.md).

The future actions Ember (`L` / left shoulder), echo record (`U` / right shoulder), and echo replay (`I` / left trigger) remain mapped but are excluded from this milestone.

## Historical six-room scope (superseded)

The first level contains six independently verifiable areas: Awakening Courtyard, Broken-Tooth Corridor, the upper/lower loop split, Bell-Chain Mechanism, Fallen Bellkeeper, and the closing Echo. It introduces the Rust-Shell Squire, Grave-Lamp Moth and Bell-Back Laborer. Ember and Echo abilities remain excluded so the level can fully teach and test the mechanical thread.

See [Complete prologue first-level design](docs/superpowers/specs/2026-08-14-prologue-level-one-design.md), [implementation plan](docs/superpowers/plans/2026-08-14-prologue-level-one.md) and [playtest/completion audit](docs/playtests/2026-08-14-prologue-first-level.md). The written specification is approved and the complete functional loop is implemented, including all three mechanical-thread uses. Human first-pass timing remains a hands-on pacing check; automated transition and presentation drivers are explicitly not reported as player timing.

## Known exclusions

Ember, echo, a world map and Steamworks remain outside the first-level production milestone. Resolution, accessibility, fallback, and deterministic performance checks remain regression gates. Attack readability, mechanical-thread flow, action continuity, room/enemy presentation, compact HUD/audio feedback, final proof, and a fresh 10–15 minute hands-on completion are still active; automated drivers are not substituted for the human playtest.

## Asset status

### 当前连续峡谷的素材与表现状态（2026-10-03；各行注明阶段，旧行保留历史边界）

下表按实际图片内容和正常入口的绑定区分“已有接口”“可用素材”和“完成画面”，不以清单中的 `final: true` 判断美术质量。

| 范围 | 现有来源与实际内容 | 当前可用范围 | 尚未完成 |
| --- | --- | --- | --- |
| G盘存储迁移（2026-10-20；v033，无新美术/音频） | 当前主目录G:/game/Knight，完整开发快照/Git/工具及第一批归档SHA校验 | G fresh restore/build、module/keyboard GREEN；两退出行为通过但资源strictFAIL | 大历史媒体/证据/旧树未整体迁；E未删除；后续新媒体在G:/game/过程图稿与视频；全1–11仍OPEN |
| 恢复退出边界（2026-10-20；v032，无新美术/音频） | 公共回调两退出窗口真实RED后加两处有效性guard；P1恢复时序与自有清理适配 | 两退出专项GREEN，同源六phase四GREEN/两FAIL | 普通P1资源/完整post结构条件/历史故障及全1–11仍OPEN；转入用户指定G盘迁移 |
| 旧教学版本隔离（2026-10-19；v031，无新美术/音频） | 原十八V1几何合同改读冻结sample/legacy；正式V2继续独立验证，不改地图/玩家/96 | 窄原生十八兼容＋V2结构GREEN，实际资源/库存读回匹配 | 完整post仍P1死亡时序/Handle FAIL；不是正式V2实际通路、正常双路/视觉/最终内容/真人/FPS验收 |
| 死亡恢复回归（2026-10-19；v030，无新美术/音频） | 复用原保存Main/Player/Health与stable检查点；只改测试预期、失败清理及严格入口依赖 | 原出生/held输入/致死/时限不变；窄原生与完整post中的死亡恢复通过，同冻五门4GREEN/1FAIL | 连续U接缝新首错未定；不代表自然死亡/保存全矩阵、资源全局修复、正式内容/正常双路/真人/视觉/FPS验收 |
| 常驻场景包迁移（2026-10-17；占位候选） | 复用原actor／单一旧visual；24嵌入配置叶＋24作者tres，无新美术／音频／付费资产 | 48文件原字节迁移、24实际factory＋1额外原生Ready；原24初始化／7negative保持 | 154／31RID及完整回归失败；配置修改须检查重建，未发布／50入图／族系视觉／整关／性能／真人验收 |
| 炮台贴墙预警/配置（2026-10-16；组件局部） | opt-in Sentry改完整圆World裁剪；复用五actor，无新栅格/音频/付费素材或正式出生 | 新增11原生检查，实际预警不再穿墙，配置/动态遮挡控制符合 | 最终13专项资源FAIL、完整pre/post仍FAIL；未配置旧预警样式及其他方位/武器/全人口/视觉门不扩大验收 |
| 炮台环境/目标（2026-10-16；组件局部） | 复用五保存actor；opt-in真实void/pending/Reset接入，无新栅格/音频/付费素材或正式出生 | 15目标控制+15环境检查；最终10专项66行为/清理符合，旧相关7门严格GREEN | 10专项资源严格FAIL，完整pre/post含句柄/路线/恢复错误；不是50入图/族系/视觉/整路/性能/真人验收 |
| 炮台形状身份诊断（2026-10-16；非美术资产） | 只增加原15基础夹具的标量原生RID记录，无生产/地图/栅格/音频/付费素材变更 | 同场景三挂载复用保存三形状，保存15与自建25无交集，原15行为及清理符合 | stdout匿名15RID仍严格FAIL，不能按数量定位根因；私有查询Shape未覆盖，不是新怪物/视觉/整路验收 |
| 炮台回调/生命周期（2026-10-16；组件局部） | 复用既有五保存actor与表现，无新栅格/音频/付费素材；只修旧死亡回调写入时序，新增真实原生验证 | 五actor自然双生命/完整新预警、激光/导弹同栈撤权行为符合，旧相关综合/键盘/动作严格GREEN | 六常驻专项资源退出严格FAIL，完整pre/post仍FAIL；不是新怪物数量、视觉精修、全50入图/整路/性能/真人验收 |
| 常驻炮台适配（2026-10-16；组件局部，未新增入图） | 复用五个保存actor/现有程序武器与预警；只新增显式常驻权限/完整领地及动态Shape生产所有权，无新栅格/音频/付费素材 | 15基础/5休眠/7完整攻击边界行为符合；旧Sentry与四远程原综合、键盘/动作严格GREEN | 三常驻专项均15RID严格FAIL；同栈撤权/配置/外力/死亡、正式族系/50入图/整路/性能/真人未验收；不是视觉精修或新怪物数量 |
| V2下降区键盘机会引导（2026-10-15） | 复用现有无边框关键词／标记／短电流；仅修当前支撑／W目标／上拉空间查询，无新美术、音频或付费资产 | 真实J/K/Space、自然落台和中英局部边界专项通过；7.4秒实际GPU受控录像、原片和九帧分版保存并有限审阅 | 非自然走到第二层／整关／正常FPS／真人或连续视听证明；场景和动作仍原型、整体NEEDS_WORK；完整引导组合／旧聚合及资源问题未闭合 |
| 四种实体远程（2026-10-14；局部入图） | 共享C#时序/设置/伤害账本、原生圆扫/厚beam、四叶actor/保存tscn；程序武器轮廓/短cosmetic爆炸环，复用现有声音，无付费栅格或新音频 | 完整actor/保存场景14组、Fog/GPU及原键盘回归通过；正式四角色八候选/0.60s警告/实际发射与击败/配重两live通过，26配置名额；声音真实1/3/1/1与同步取消专项通过 | 实际Space/K逃脱、机关Area2D配合、正常新整关/录像/最终美术/性能未验收；26不是密度上限或全部体验完成。V1迁移/失焦/旧资源仍OPEN |
| 回声AI／安定无边框引导（2026-10-14实装，10-15复核） | 中英文关键词＋固定320阅读列、无外框软底；自建0.19秒双短电流WAV，独立UI通道/8秒限频；幽光4柔光点，无新栅格/付费资产，尚无人声配音/TTS | 原布局/双语/PCM/暂停/限频专项通过，5图分版审阅；10-15声音＋ECHO普通native9236ms严格通过，保留即时停止/解绑断言，测试清理另观测真实混音 | AI来源/身体/修复/专属关卡仅候选；部分WASD/W方向改键、全部提示组合、完整新路线/连续听感/真人未验收。失败录像及旧186/189Shape保留，幽光自然触发观感待审，整体美术NEEDS_WORK |
| 墓灯保存失败提示（2026-10-14） | 现有HUD文本层，中英文明确失败与重新接触，无生成/付费素材 | 真实GL文件锁失败/解锁重入六图，字形完整、不会被上下文覆盖；成功才点灯，原图和失败分版归档 | 不是新的墓灯美术；其他保存失败UI、整套英文/视听/真人验收未完成 |
| 电磁线圈发射器（2026-10-14；局部精修方向已批准） | 原260×120原生危险范围内程序化外壳；2局部单位暗铜连续轮廓、12单位以内角标，32世界单位宽侧面/破损绕组与双股左行电弧；无付费/新栅格资产 | 实际GPU范围/流向/镜像/遮挡/角色避让/极端缩放及精修像素通过；正式中英保存场景、实际死亡恢复原片分版归档。资源夹具严格FAIL及后续主线程释放GREEN分别保留；新版正常Explore完成，Main模拟持键撤回FAIL保留 | 仍偏示意机关，矩形提示感及场景/敌人/小地图待精修；正常时间仍有慢帧/失焦，真人整关未验收，不算新增机关种类/数量；固定60录像不证明实时FPS |
| 小地图柔和椭圆（2026-10-13；批准后局部修订） | 原程序化View／shader；柔和椭圆外沿与完整标记输出像素约束，无新付费图片 | 三类GPU反例→修复，最终HUD／正式接入／生命周期13项／原HUD引导及七帧GL采集通过；v008实景已归档审看，独立增量无阻断，保持探索与存档权限 | 黑暗占幅和整体视觉仍NEEDS_WORK；纯父变换动态重绘边界待验证，整关美术／真人／性能不以局部通过替代 |
| 盆地区配重升降转移（2026-10-12；已批方向，失败候选撤回） | 复用既有升降台／控制／抓环／材质，没有新图片、视频或付费素材 | 三个候选的真实横廊转移失败，候选与测试保留；仅本轮三product文件精确还原、构建通过，当前关卡未接入此新路线 | 待机械爪稳定牵引设计确认及新一轮真实路线／安全／视觉验证；不把历史局部通过或还原构建称成品 |
| 小地图可读性修订（2026-10-12；Task9 fix2限定通过） | 原View参数／标记程序化绘制，无新图片或付费素材；v006／v007原件、展示副本和失败历史均保留 | 最小UI亮芯26；仅尺寸变化更新渐隐参数；逐次反例768→576B（非零总分配）；五检查点负控／正控和九同版安全阶段通过，独立限定C0/I0/M0；v007全量归档和有限抽帧审阅完成 | 未归因分配尖峰、黑暗占幅、整体美术／丰富度／正常速度／真人仍OPEN，不是最终UI交付 |
| 探索地图＋引导当前候选（2026-10-12；Task8后） | 原地图显示／130结构目录不变；萤火虫支撑搜索单一生产优化，无新图片／付费素材 | 原11＋新16断言、两个邻项、独立限定C0/I0/M0；新同版静止地图额外开销门与正常Main／Explore完成 | 正常整关callback仍有长帧；新录像归档／画面审阅进行中；可选地图拒绝诊断、整体美术／丰富度／真人／连续视听未完成 |
| 无边框探索小地图（2026-10-12；正式接入候选） | 本地只读Control／固定局部R8遮罩及整输出像素shader，同源130结构／10488段目录；无生成图片／付费素材 | v00324文件冻结；目录／独立冷读／生命周期／GPU显示／局部模型预算／正常双路线和原六邻项通过；完整源码独立C0/I0/M1，过程画面v004已归档抽帧审阅 | 正常三臂恢复基线漂移，比较INVALID／FAIL，性能验收OPEN；可选地图拒绝诊断Minor延后；连续视听／真人与整体美术未验收，不是完整交付 |
| 活塞/摆锤提前预警（2026-10-11；本轮） | 原固定sibling Polygon2D的颜色/变换及原机关自然Frame，独立layer2逐小块原生遮挡显示，无新图片或付费素材 | 自然预警GPU原色/降层/墙体正负控、同帧撤销及保存controller、六同版专项/fresh Main与Explore通过 | 正式雾已条件显示；正常速度连续路线预警观感、视听/实时性能和历史GCHandle仍开放；旧归档非本轮视觉证明 |
| 压板门关闭提示（2026-10-11） | 原CanyonPressureGateVisual共享本地程序化关闭线/五tick；独立layer2消费原paint，无新图片或付费素材 | 两原saved门GPU、保存controller钟链遮挡/开启对照及生命周期、原门模型/14原生runtime族通过；只读显示不改门碰撞/时序 | 正式雾已条件显示；正常路线观感及完整视听待验收。键盘综合释放FATAL仍FAIL，不以此行表示整关可交付 |
| 移动机关中性实体边缘（2026-10-11） | 原saved lift/piston/pendulum的实际native矩形薄边；独立layer2程序化可替换显示，无新图像或付费素材 | CPU精确face、三原来源自然运动GPU、保存controller及同版专项通过；fresh Main/Explore自动键盘到Boss及有支撑出口 | 非伤害预警/安全落地承诺；不改运动或碰撞，正式雾已条件显示；不是正常战斗全路线/真人/实时性能或归档媒体验收，键盘综合FATAL仍FAIL |
| Boss提示避让v001（2026-10-04） | 实际装甲绘制范围接入现有提示卡避让，无美术生成/战斗或布局重写 | 专项RED→GREEN及源码复核通过；自动Main完成Boss/实体出口；原片/20秒预览/新旧各4帧保留，抽帧提示可见且不再覆盖暴露装甲 | 全量套件隔离、所有姿态/连续动作、声音、真人/实时与最终美术仍未验收 |
| 角色材质v001（2026-10-04） | PlayerFormalLayer实际身体/肢体/披风内部静态绘制，无新生成素材或旧图集替换 | 源码复核及六项姿态契约通过；自动Main完成Boss/实体出口；原片/12秒预览/新旧各12帧保留，局部布面与骨瓷同色改善 | 角色与武器仍偏原型，胸甲改动多被遮挡；整体材质、布料动作、连续视听、真人/实时未验收 |
| 入口承托v007（2026-10-04） | 正式入口已有七处斜撑的静态绘制接触/侧面修订，无新生成素材 | 源码/限定结构审查及自动Main完成Boss/实体出口；10494帧原片、720帧短片、新旧各8帧保留；局部连接改善 | 重复阶梯/平面背景/原型角色UI仍需完善；连续视听、真人、实时和完整交付未验收 |
| 入口→裂谷石材分节v006（2026-10-04） | 单绘制方法改为平面端面/稀疏接缝与磨损；无生成或购买；97项过程材料逐项校验 | 限定结构/源码通过；实际抽帧局部改善；2001帧失败原片/720帧入口预览完整解码，22张原尺寸帧保存 | 正常Main护盾通道失败，原因待观测；整体美术、连续视听/真人/实时/完整交付未验收 |
| 入口→裂谷光色材质v005（2026-10-04） | 选定正式场景的静态顶点渐变/石材面绘制，无新图片或付费生成；保留原片、12秒预览、新旧各8帧和审阅 | 限定结构/源码检查及正常自动Main完成Boss/有支撑出口；原片10494帧/预览720帧完整解码；实际对照确认局部纵深改善 | 材质NEEDS_WORK：柔滑重复长条、接缝/承托不足；角色/敌人/UI仍原型化，连续视听/真人/实时/完整交付未验收 |
| 开场教学UI v001（2026-10-04） | 显示层去重；原片、10秒短片与12张新旧原尺寸帧，无新生成美术 | 正常自动Main完成Boss/支撑出口；原片10494帧/短片600帧完整解码；已查看同帧去重及战斗提示保留 | 专项空间夹具失败/旧布局假设待诊断；持续运动、全尺度上手/听感/真人/实时与整体美术未验收 |
| 捷径落台观测 v001（2026-10-04） | 原片、3秒诊断片、8张原尺寸抽帧；没有新增游戏美术 | 只读观测版自动探索完成Boss/实体出口；下行10条及上行12条观测无读取异常，原片15571帧/短片180帧完整解码 | 旧停滞原因未证实；视觉仍简化重复；不是修复证明、最终美术、真人或实时验收 |
| 入口→裂谷色罩修订 v004（2026-10-03） | 移除五块整区装饰色罩，不删除语义引导节点；无新生成/采购图片 | 专项结构回归通过；原尺寸同帧抽查与独立审阅确认竖向色差线消失，预览和失败原片已留存 | 该次正常探索在捷径2996平台检查失败，原因仍待复现；广泛平涂、重复平台、角色/敌人/UI仍待精修；真人/实时/整体交付未验收 |
| 入口→裂谷构图 v003（2026-10-03，历史对照） | 两区移除各七根泛用装饰柱；本地静态岩壁/遗迹轮廓调整；未生成或采购新素材 | 此历史源码和两片生成场景通过结构检查；正常开局探索通关录制；原尺寸对照确认柱体重复减少 | 此版竖向色罩接缝未解决；平台、角色/敌人、UI仍简化；不是当前版本、最终美术、真人/实时或交付验收 |
| 入口构图 v002（2026-10-03，历史对照） | 已挂载表现脚本的本地静态多边形绘制；单个断钟、岩壁、遗迹承托和淡光；无新生成/采购图片 | 新正常开局探索录制通过；构建器与打包场景的材质/可读层分离回归通过；原尺寸同机位对照显示局部构图改善 | 此历史版本粗大折面、七根旧柱、阶梯路线及邻区分界明显；不是最终美术，动作/听感/真人/实时验收未完成 |
| 连续峡谷场景 | 五个正式扇区的独立背景/中景/可读层/前景/效果接口；新增运输门架、档案陈设、冰管、维修机座、配重与破拱等本地静态绘制 | 九个实际窗口同机位前后对照及正常输入接缝录像已审看，局部布景与截图安全修订通过独立审查；玩法碰撞和抓取材质不随装饰改变 | 简化几何仍明显；全路线正常镜头、区域光影材质、最终美术与实时运动验收 |
| 神殿上层补给支路（2026-10-02） | 复用既有地形可读外观、压力门程序绘制、可用/耗尽补给箱及状态文字；没有新增付费或下载资源 | 新梁、平台、支撑与围合、门和补给已由构建器生成；配置/存档/场景结构检查及本阶段独立复核通过 | 真实抓取落点、通道净空、防夹、补给收益和持久化生命周期；正常镜头可读性、材质及最终美术未验收，不计入已完成安装数量 |
| 环境与机关图集 | `environment-atlas.png` 为条纹色板；`mechanisms.png` 为简单原型符号 | 配色或原型符号参考，不能拉伸成正式石材或建筑插画 | 完整环境插画资源不足；新生成或采购仍须单独授权 |
| 护盾通道双语引导（2026-10-14，本轮） | 本地现有 HUD／本地化／迷雾事件接入；朝右举盾与耗能积热、离场松键散热，实时按键字形 | 无新栅格／音频／付费素材；字形像素RED→GREEN、正式场景专项、真实死亡恢复录像及无窗口回归通过 | 仅局部可读性改善；原件与预览存护盾通道引导v001–v004，最终画面仍NEEDS_WORK，非真人／正常性能验收 |
| 持续电磁通道方向层（2026-10-14，上一轮时点） | 本地 `FogShieldLaneReadabilityOverlay`：右侧来源轨、两排左箭头、左移光点，受真实范围/遮挡约束 | 无新栅格/音频/付费素材；局部GPU方向断言、保存控制器和实际受伤死亡恢复录像有运行证据；当轮最后GPU退出资源告警严格FAIL保留 | 仍为功能提示，不是最终机械装置美术；后续护盾提示见上行，整体层次及旧资源释放告警诊断仍待完善 |
| 新增双向蒸汽分流器 | 本地程序绘制的阀体、流向箭头、管线、无伤换向浅汽、预警与危险喷汽柱；独立可移除表现节点 | 组件独立审查、两处地图安装和区域真实输入往返已通过局部检查，蒸汽区域含实际战斗及阶梯回升；无新增图片、音频或付费素材 | 流向箭头歧义、矩形喷汽和预警形态仍待视觉修订；最终邻接回归及地图接入整体独立审查未完成，不是最终美术 |
| 主角、普通敌人与 Boss | 玩家图集是占位方块；敌人/Boss 图集为简单平涂部件；正常入口主要显示程序绘制的分部件形象 | 现有角色状态、部件和动画替换接口保留 | 正常入口的最终角色画面、布料感、动作力度与技能姿态审阅；参考立绘不是动画帧 |
| 新增钟矢哨兵/峡谷疾行虫 | 本地程序绘制的简化分部件形象，配合实际蓄力/冲刺/退让/打断状态 | 两个独立组件已通过修订审查；五个有限出场位已接入工作区并有局部运行证据，源码为5种/22位，但对象释放崩溃尚未解决，正式验收仍停在3种/17位 | 集成稳定性、整关平衡；最终轮廓、动作和材质精修 |
| 悬浮灯与特效源图 | `assets/player/thread-spool-bearer/lantern.png` 为 96×96 的较细致灯图；`prologue-vfx.png` 为四个简单圆环 | 灯图仅适合相应小尺寸用途；圆环可作原型效果，不代表完整技能美术 | 放大质量、真实显示尺寸、效果绑定、动作中的遮挡和风格统一 |
| 首关教学与路线提示 | 正常入口已有短暂萤火引导和教学卡安全边缘布局，开发检查点 `8e98120` | 暂停、局部危险避让、中英完整文字、实际隐藏后恢复和默认回归已通过本阶段验收 | 全关首次上手理解速度、UI 整体审美与正常速度体验 |
| 标准短喷模块配置（2026-10-18） | 实际BuildPlayer生成并保存于Player/Motor/InitialMobilityModule；八项显式native Resource值，旧场景已备份 | 项目自有配置，候选搬移/实际运动与正式读取通过；无新贴图、声音或付费 | 配置、观察／双语教学、typed姿态、世界喷焰与独立声音已接；音频退出严格FAIL，非最终美术或整关验收 |
| 短喷键盘教学（2026-10-18） | 自有localization／Controls／Pause／Guidance，真实字形与正常初始LearnAttackMode | 中英布局与实际初始可见卡专项通过，无新图像或音频 | 代码／内容验收，不替代完整上手体验、最终UI与视听验收 |
| 短喷姿态与斗篷边界（2026-10-18） | 现有分层Formal共同父刚体倾身／回稳／反刹，肩核喷口；Draw跨肩带边分割 | 真实左右键盘姿态、暂停／高优先动作及原两mantle几何合同通过，无整图拉伸／新栅格／付费；v014结构化拒绝／compatibility严格通过 | 仍是程序承载；历史教学资源问题OPEN，最终角色素材与像素观感待验 |
| 世界喷焰与独立短喷声音（2026-10-18） | 现有World单typed方向轮廓；自有dash.wav副本循环、release／preview有限尾声，独立SFX voice及唯一journal水位 | 实际保存Player喷口／世界轴、长燃烧／取消、暂停／重绑／重入／低闪／拒绝通过；未生成或付费新资产 | v013同冻再次退出2 ObjectDB严格FAIL；程序化／旧bank明确占位，正常速度AV／真人／最终音色未验收 |
| 旧教学兼容与拒绝诊断（2026-10-18） | 自有PresentationProfile只值诊断API；明确legacy场景、owned admission及原键盘／pointer适配测试，无新美术或音频 | v014两次pre严格通过；墙／顶／自救、GripStone正负控、9视线和4回返真实通过，原判定不放宽 | 不是V2 Main通关；legacy .2配重阻挡及全11消费者／最终资源继续OPEN，完整目标未完成 |
| 旧教学助跑与支撑预测（2026-10-18） | 自有测试观察／原InputMap助跑、实际不可变Motion／Module及保存矩形／胶囊计算，无新资产 | v015下行原出生区内助跑通过；同冻9视线、4回返、前7段21样本通过，12/15%及原目标支撑保留 | 最后高层中途碰撞仍严格FAIL；不是完整路线／最终美术／真人验收，全目标ACTIVE |

以上仅记录现有本地资源及运行状态，没有新增生成、下载或付费素材。场景、角色、UI 的最终视觉验收仍是完整目标的未完成项。

### 历史来源与替换里程碑

以下资产表保留来源、许可和历史替换里程碑。表内既有“自动验收完成”仅适用于所链接的旧基线，
不能作为当前连续峡谷的质量或回归通过结论；本轮状态以上表及 README 顶部为准。

| Asset | Current source | License/status | Replacement milestone |
| --- | --- | --- | --- |
| 候选父场景与小地图同源交接（2026-10-18） | [五区自有快照／搬移原字节／真实Ready／存活唯一角色](docs/production/2026-10-18-candidate-parent-embedded-minimap-v001.md) | 既有项目资产，ExplicitPlaceholder；无新素材／付费，不是最终族系美术或独立游戏包 | 候选接线与局部抓爪返程通过；正式仍127／新增0，最终十一门6GREEN5FAIL，完整可玩／视听未验收 |
| 冰区停靠净空与玩家局部往返（2026-10-18，隔离候选） | [80px停靠通道／同角色抓墙抓顶落台返程／20889行候选诊断](docs/production/2026-10-18-frozen-player-roundtrip-and-dock-clearance-v001.md) | generate2dmap结构／碰撞分层，复用现有灰盒skin；无新素材／付费，非最终美术 | 玩家局部实际运行及两幽灵通过；机关不活动，父Minimap未接线，正式仍127／新增0，最终十门6GREEN4FAIL，完整交付未验收 |
| 冰封围合侧龛与两Ghost（2026-10-17，历史隔离候选） | [三段石壁／两支撑台／两幽灵360帧真实巡逻与20901行原因](docs/production/2026-10-17-frozen-resident-alcoves-native-v001.md) | 复用引擎灰盒skin与旧visual，ExplicitPlaceholder；无新贴图／音频／付费，非最终族系美术 | 当时候选132地形／43候选7未解；当时Player仅胶囊查询，当前80px实际局部返程见上行，不回写历史报告 |
| 常驻点位实际叶与拒绝证据（2026-10-17，前轮测试准备） | [真实24候选叶／原生身体与factory走廊／20594行原因](docs/production/2026-10-17-resident-actual-leaf-geometry-v001.md) | 复用旧visual，ExplicitPlaceholder；无新贴图／音频／付费，不是正式美术 | 首案几何GREEN但同版复测FATAL；当时41候选／9未解／正式新增0，全部实际运动／完整关卡未验收 |
| 骨骸游荡弱点（2026-10-16，开发承载） | [复用保存Rust与Player的实际姿态／键盘链](docs/production/2026-10-16-resident-bone-wanderer-native-v001.md)；独立RecoveryOrThreadStunned策略与typed家族权限 | 无新贴图／音频／付费素材，不是正式骨骸身份 | 七项行为含保存Player K→J符合，但退出7 Shape RID严格FAIL；50正式布置／24族系场景／整关视觉及真人未验收 |
| 骨骸冲锋弱点（2026-10-17，开发承载） | [保存Skitter实际Recover／K来源空中窗口与键盘反击](docs/production/2026-10-17-resident-bone-charger-native-v001.md)；独占RecoveryOrThreadExposed及最终bool机械爪事务 | 无新贴图／音频／付费素材，不是正式骨骸身份 | 九行为含保存Player K→J与空中到期符合；退出7 Shape RID严格FAIL，正式入图／最终视觉／真人未验收 |
| 骨骸俯冲弱点（2026-10-17，开发承载） | [保存Moth实际Recover／K来源晕眩及保存Player向上K→J](docs/production/2026-10-17-resident-bone-moth-native-v001.md)；独占BoneDiver2及最终bool事务 | 无新贴图／音频／付费素材，不是正式骨骸身份 | 八行为符合但退出7 Shape RID严格FAIL；50正式点位／24场景／最终视觉／真人未验收 |
| 骨骸／幽灵机制（2026-10-16，开发承载） | 复用保存BellBackLaborer与RustShellSquire；新增不可变combat profile／骨骸方向姿态门／幽灵显隐和J-K事务 | 无新贴图／付费素材；Ghost只复用原VisualRoot alpha，不是正式族系身份 | 4＋3机制案例行为符合，Shape RID严格FAIL；50正式布置／24族系场景／正常速度视觉与真人未验收；完整pre再次FATAL，详见族系机制v001 |
| 常驻空中职能（2026-10-16，开发承载） | 复用保存GraveLampMoth与现有分层／预警；显式Flyer短侧袭和Diver下冲 | 无新图片／付费素材；不是新增正式族系美术 | 7新角色＋原20行为符合，但Shape RID严格FAIL；50正式布置／Bone-Ghost身份／正常速度视觉与真人仍待验收，详见空中职能v001 |
| Boss准入门（2026-10-11） | 复用原机械门panel；layout2独立Forbidden有限三片准入罩，与原地板正重叠 | 无新图片/付费素材；实体AND/同帧第六件/生命周期、native各片和四角胶囊、顶部及东侧冲刺专项通过，独立自然5→6与fresh Main/Explore自动回归通过 | 三shape雾上当前边界GPU/保存接线已通过，正式雾仍隐藏；更广缺额跨区回返、完整指引、正常速度视觉/性能与真人验收未完成 |
| Production manifest | `assets/production/prologue-assets.json`; all eight required categories and every runtime path are verified | Project-owned; strict id/path/source/license/file validation | 已完成 as a file/license/path gate; individual visual families below remain under sensory review |
| Formal UI system | `scenes/ui/PrologueUi.tscn` plus `Hud/FormalHud`; bilingual title/settings/controls, HUD, guidance, prompts, Boss bar, pause, recovery and completion card; thread state/failure text is live at the stable `Hud/FormalHud/ThreadSpool/State` path | Project-owned vector UI; no paid generation | 机械丝阶段反馈自动验收完成；compact health/thread cluster、one-line objective 与正常速度首读待真人复核 |
| Formal player rig | `scenes/presentation/PlayerFormalVisual.tscn`, ten-layer pose rig and validated clip metadata including Shield; mounted at `Player/VisualRoot/Formal` | Project-owned local vector/raster assets and procedural poses; no paid generation | 2026-09-08 新增真实防御上身/披风姿态、收盾接攻击过渡与实际攻击方向同步；专项和样板运行已验证，不把clip元数据视为新增绘制帧；正常速度节奏、力度与真人审美仍待复核 |
| Formal Rust-Shell | Live generated `Corruption`/`Body`/`Shell`/`Weapon` typed render slots with idle/chase/telegraph/attack/recover/stunned/dead states at `VisualRoot/Formal` | Project-owned local vector/raster assets; no paid generation | 保留旧分层接口；2026-09-08 实景修正刺击过伸，未改素材/伤害框。近身重叠、beginner timing、AC corruption 与正常速度首读待复核 |
| Formal prologue environments | Six catalog-driven room compositions use stable `Art/Far`, `Back`, `Mid`, `Platforms`, `Props`, `Front` and `Atmosphere` leaves; landmarks are broken clock, severed gear train, pendulum chains, copper pipes/fungus, vertical chain and backlit broken bell | Project-owned local procedural/vector layers; no paid generation | 自动验收完成: unique landmarks, five depth roles, collision-derived skins, deterministic atmosphere/parallax and combat-aware per-leaf foreground fading; normal-speed route taste review remains open |
| Formal Grave-Lamp Moth | Live generated `Corruption`/`Wings`/`Body`/`Lamp` typed render slots with hover/fly/telegraph/dive/recover/thread-pull/dead states at `VisualRoot/Formal` | Project-owned local vector/raster assets; no paid generation | 动作连续性、真实 replacement-slot 继承契约、World 碰撞与 warning/Active/Recovery 几何自动验收完成；folded-wing feel、AC corruption 与正常速度首读待真人复核 |
| Formal Bell-Back Laborer | Live generated `Corruption`/`Bell`/`Body`/`Feet`/`Weapon` typed render slots with approach/guard/telegraph/attack/exposed/recover/dead states at `VisualRoot/Formal` | Project-owned local vector/raster assets; no paid generation | 动作连续性、真实 replacement-slot 继承契约、authoritative 120-degree convex sweep 与阶段清场自动验收完成；counter-twist、AC corruption 与正常速度首读待真人复核 |
| Basin sample Laborer | `BasinLaborerVisual.cs` mounted through sample-only `BasinLaborer.tscn`; 10 actual drawn parts including separate armor, core, arm joints and hammer | Project-authored procedural drawing; no image generation or paid assets | 真实状态/左右镜像/原碰撞不变与样板完整回路已验证；正常入口仍用原造型，用户审美与新手首读未验收 |
| Basin sample environment | `BasinEnvironmentVisual` at independent AtmosphericRift/BuriedSanctum slots; `BasinPlatformVisual` on three terrain skins | Project-authored static drawing; no image generation or paid assets | 破拱/岩层/悬链及右侧台面已进入运行样板；精确碰撞/顶边/抓取规则与普通场景隔离通过，用户审美与背景不可抓取的首读待验收 |
| Basin sample combat readout | Runtime `SampleGuide/View/CombatReadout` and adaptive teaching panel | Project-authored UI; no generated images or paid assets | 活状态/生命条、背侧避让及屏幕裁切回退已验证；颜色以外保留文字，真人辨认速度与整体审美待评审 |
| First-level mechanisms | Formal Grave Lamp checkpoint, Thread Anchor, Bell-Chain and Shortcut Door leaves with idle/highlight/activate/active/disabled/restored states; Bell-Chain marker and thread endpoint are coincident; the programmatic thread shuttle is serialized at `Player/VisualRoot/ThreadShuttle` | Owned; no paid generation | 自动验收完成: Preview→Windup→Travel→Latched→Apply→Recovery、tap/hold、阻断失败和清场均有真实输入证据；正常速度手感待真人复核 |
| Formal VFX and camera feedback | `PrologueVfx.tscn` typed effects; `PlayerAttackContact` resolves confirmed hits to the actual target surface | Project-owned procedural atlas/code; no paid generation | AttackHit采用14px定向短火花而非人物中心光圈；原音频位置/震屏/时长保留，自定义CueScene初始化位置和四方向接触已测试；整体视听与特效风格仍待审看 |
| Original adaptive audio | 76 deterministic WAV assets plus `audio-manifest.json`; five buses, six ambiences and current typed cues | Project-owned local procedural synthesis with fixed seed and reproducible renderer; no paid generation | 已补收绳后的真实助推起音，沿用现有音色；音频/VFX和输入发布链通过，统一听感仍待评审，资源数量不代表品质 |
| Formal Fallen Bellkeeper | Five-part asymmetric bell-shell rig at `VisualRoot/Formal`, with independent armor, hammer, chain and exposed core | Project-owned local vector/raster assets; no paid generation | 四类 Boss 动作的进入/退出连续性、同帧 gate 与 60 FPS bounds 自动验收完成；health/timing balance、正常速度首读与真人审美验收仍待复核 |
| First-level guidance HUD | Project-owned procedural instruction text, off-screen arrow and target diamonds at `Hud/Guidance/VisualRoot`; centralized protected fallback fails closed and clears/hides cards with no safe Boss/danger/HUD geometry | Owned; no paid generation | All-blocked and four-attack real-route mutation regressions plus live five-resolution Boss guidance checks completed; first-time normal-speed comprehension/feel remains for the user-owned hands-on run |

The first six-frame body-only `idle` candidate passed geometric QC but failed
visual review because of abrupt body deformation and unconvincing mantle
motion. It is retained externally as rejection evidence only; no animation
atlas or runtime clip has been published.

## References

- [Nameless Belfry demo design specification](docs/superpowers/specs/2026-08-12-nameless-belfry-demo-design.md)
- [Complete prologue first-level design](docs/superpowers/specs/2026-08-14-prologue-level-one-design.md)
- [First-level guidance verification and visual audit](docs/playtests/2026-08-14-prologue-guidance-audit.md)
- [Bell-Chain aim-assist verification](docs/playtests/2026-08-15-bell-chain-aim-assist.md)
- [Technical prototype implementation plan](docs/superpowers/plans/2026-08-12-technical-prototype.md)
- [Player character art design](docs/superpowers/specs/2026-08-13-player-character-art-design.md)
- [A2 shoulder-rig redesign](docs/superpowers/specs/2026-08-13-player-a2-shoulder-rig-redesign.md)
- [Player character art implementation plan](docs/superpowers/plans/2026-08-13-player-character-art-pipeline.md)
- [Player art production status](docs/art/player/player-art-production.md)
- [Full prologue production specification](docs/superpowers/specs/2026-08-17-prologue-full-production-design.md)
- [Full prologue production implementation plan](docs/superpowers/plans/2026-08-17-prologue-full-production.md)
- [Full prologue production certification](docs/playtests/2026-08-18-prologue-full-production.md)
- [Combat readability and presentation design](docs/superpowers/specs/2026-08-21-combat-readability-presentation-design.md)
- [Attack readability and first-level difficulty plan](docs/superpowers/plans/2026-08-21-attack-readability-and-difficulty.md)
- [Mechanical-thread casting plan](docs/superpowers/plans/2026-08-21-mechanical-thread-casting.md)
- [Player and enemy action motion plan](docs/superpowers/plans/2026-08-21-action-motion-system.md)
- [Environment, UI, and audio polish plan](docs/superpowers/plans/2026-08-21-environment-ui-audio-polish.md)
- [Complete first-level experience validation plan](docs/superpowers/plans/2026-08-21-first-level-experience-validation.md)
最新推进（2026-10-10 20:38；原生错误时序21:09纠正）：完整Explore侧面跃出已明确不是多跳/重复K：真实角色在到达发布状态前后下落速度增加，出爪后碰平台侧面，再掉入危险区。修订仅测试侧使用与生产相同的实际8px到达几何和native接触否决，不改运动/地图/数值；局部3796与3636已实际单跳/单抓完整落台，但第二次Basin载入出现四个SwapGCHandle错误，3636成功后另有SetGodotObjectPtr错误，不能归为第三次GD.Load；整个局部suite仍FAIL，不能作整关通过。关闭winch的独立装配问题不解释这些原生错误。准备从标题重新验证完整Explore。目标保持ACTIVE；实体收集门禁/完整补收集回返、正式雾可读性与设置、实时性能/新录像仍未完成。本段之后的旧时间戳为历史状态，详见键盘阶段记录。
