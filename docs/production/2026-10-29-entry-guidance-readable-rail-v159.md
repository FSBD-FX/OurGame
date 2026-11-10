# KNIGHT v159 · 真实入口教学避让与固定阅读区

本轮回归及保管核验已结束：最终普通7阶段5PASS/2FAIL，全部12尝试6PASS/6FAIL。完整原十一项目标保持 ACTIVE/OPEN；不是正式交付，不保证十万付费玩家。工程仍为 `G:/knight/Knight` Godot.NET，未触碰独立 UE 工程。

## 事实

- 用户确认上一轮自动关闭窗口**没有看到游戏画面**。该次展示授权已使用，本轮不再开窗口、不抢前台、不发系统键鼠；NOTSEEN 不等于认可画面。本轮没有新 GL 像素或真人观感证据。
- v158 实际截图的大片教学层由 `WorldTargetMarkerVisual` 渲染，不是上方 ECHO 单行栏。旧布局只避让 HUD，完整组合机关文案撑高后覆盖主角身体、头和悬浮灯。
- 当前真实入口的弹板/齿轮组合提示合法地优先于 J/K/短喷普通教学，尚未完成的 LearnAttackMode 目标仍保留。不能为旧测试隐藏机关、移走真实入口玩家或改变优先级。

## 实际改动及边界

1. 普通教学保留原 720px 固定阅读列。已有 expanded + ContextTitle 的器件教学使用独立固定下阅读区：左缘48、宽至840，按底部永久仪表和 UI 缩放保留空间；世界目标与角色移动不决定提示位置。
2. 完整文字仍由实际 Label 测量高度，不截断、不省略、不缩字号；当前器件软底透明度 .42，普通 .62，装饰边线隐藏。长文超过144预留仍按实际高度处理；没有空间时沿原公共路径隐藏，不能据此声称所有窗口都有提示。
3. 四正文键×中英仅把硬换行改为 ` · `，共8值；第五个 recycling 标题键及其他 JSON 叶值不变。独立前后对照确认词、警告、占位符完整保留。
4. 真实入口测试保留原地玩家、实际保存场景、原相机、启用的组合弹板/齿轮、原HP、未弹射及未完成目标。两分辨率×三UI档×双语共12状态，实际 Backdrop 与头/胶囊/灯矩形同帧比较。默认1280中文提示由(48,250.28)/720×152改为(48,488)/840×88，英文高98；当前真实入口无交叠。
5. 独立 renderer fixture 明确用生产模型生成普通无上下文教学，不冒充真实出生优先级；另覆盖4器件文案×12组合、192移动目标样本、底部仪表隐藏和完整字形。旧 KeyboardTeaching 初始 SHIFT 前提经实际失败后改为“真实 Combo 前后保持＋显式普通模型挂载渲染探针”，不改玩法或实际目标。

仅4既有C#（真实入口测试、两教学测试、运行时布局）＋本地化JSON及canonical证据/门发生本轮代码变化；没有场景、物理、数值、输入规则或资源生成变化。1857源/897C#库存不增减，143地形/9残片/76普通敌人目标不变。

## 严格原生证据

所有case位于 `G:/knight/验证证据/october13-stable-claw/`，完整目录后缀见下表。命令统一为：

```powershell
pwsh -NoProfile -File .superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1 -Phase <phase> -SourceRows 1857
```

| case后缀 | phase | build exit/ms | native exit/ms | 严格结论 |
|---|---|---:|---:|---|
|20261029-213344-708-0324c53b3eae4bdb8e7d3bb4a83b38bc|thruster-approved-lantern|0/53170|1/6581|实际遮挡RED，10/12失败；非旧J/K设置错误|
|20261029-214035-328-2d81d1d055154ee19705c564efe6b3ee|thruster-approved-lantern|0/35838|0/6758|PASS，12真实入口状态|
|20261029-214233-224-4cf25711014c4742abab9147e6d822f6|thruster-approved-lantern-layout|0/48559|1/5021|FAIL，旧初始J/K前提|
|20261029-214749-496-3a1a4f8e338a48ec9d4d40bf06d85616|thruster-approved-lantern-layout|0/35457|1/4282|FAIL，新增测试字典漏move_up；原件保留|
|20261029-214947-029-e8cc5df6a150428f8adf2c7d0afe8fc6|thruster-approved-lantern-layout|0/39218|1/8124|FAIL，原普通12/器件48子项通过，但旧初始SHIFT前提失败|
|20261029-215526-062-11a80ba02c2e487e95e5b4ff8d44ef53|thruster-approved-lantern-layout|0/42910|0/7624|严格FAIL，功能回执齐但2 ObjectDB退出泄漏警告；不豁免|
|20261029-215844-179-c2bd1e0df41e4064bd52adac9518565a|thruster-approved-lantern|0/36949|0/5018|最终同源PASS，仍非像素/真人|
|20261029-215936-252-18b91ad4b73f44a9bb30de73abe7e4c2|recycling-traps|0/40728|0/33614|PASS，原组合接触/返程/源/重绑/恢复及7fixture合同|
|20261029-220101-589-4cfc55da74c64449925f90984702ebb4|formal-hud-layout|0/38715|0/7524|PASS，原12组合与原重叠断言|
|20261029-220157-143-61b2b55642cf474797a53f9e79633ccd|models|0/67082|0/949|PASS，严格普通模型阶段|
|20261029-220314-797-7fd9c4f6f0ff4a25990ac7a2ef5c10df|prologue-pre|0/40394|1/163974|FAIL，第二J检查时已到攻击结束；原cap180秒内失败|
|20261029-220652-511-b664643d30bb4c8aa37d6a2b9c9af6a0|prologue-post|0/45894|0/223496|PASS，原75案例＋8次owner；原300秒内完成|

全部12尝试6PASS/6FAIL；最终普通7阶段5PASS/2FAIL（layout退出ObjectDB警告及pre）。外层最后六阶段命令exit1只因pre，不包括另行执行的layoutFAIL。post原75案例/零退役/8次owner完整执行，stdout零ERROR/WARNING、stderr及external各为同4条既有限定WARNING（seamless观察者1、room-profile负控1、拒绝Completed→Recovering2），不是“全日志零警告”；没有扩时、添加豁免或绕过raw门。

pre的同次只读原始观察：command physics4090，实际第二次AttackStarted在4091/攻击ID2；断言观察在4114/输入序列302，模型Elapsed=0.30000004、Duration=.3、Attacking=false，HP5/Playing/ClawIdle且无拒绝。源码是在3个输入采样后再await Settle，然后才读IsAttacking。**本次**证明指令确实被接受但检查时已结束，不能称按键未响应、不能把历史所有第二J失败一并关闭；后续应让该“攻击中仍允许K”测试在实际物理攻击窗口观察，不改玩法时长/补按/延长预算。本轮保持源冻结，未修改此测试。

## 推理与风险

- RED→GREEN支持的是**当前真实入口12状态**的局部几何修正，不是全关UI、对比度、美术或任意移动位置永不遮挡。固定屏幕阅读区与“所有世界位置都避让角色”不能同时从该矩阵推出。
- 唯一新鲜最终审查：C0/I1/M2。I1是继承的旧初始SHIFT测试前提，接受并做一次修正；实际功能回执通过，但退出对象泄漏继续严格FAIL。未追加二审、未把资源警告降级为成功。
- M1延期：器件Clear/无可用空间/空间恢复的额外生命周期测试；目前只审过Clear重置，受限窗口提示可用性未验收。
- M2延期：非identity final canvas、拉伸/黑边窗口；当前窗口与内容等尺寸，不能推广为任意缩放映射证明。
- 所有历史资源Handle/FATAL/ShapeRID、完整Main/Explore失败继续OPEN。本轮纯UI修正不重复完整路线筛绿；最终全十一项验收仍必须是当前同版的完整双路、严格资源、正常速度视听、硬件性能与用户试玩认可。

审查逐条set-aside的root裁决及代价保存在 `.superpowers/sdd/2026-10-29-entry-guidance-readable-rail/progress.md`，没有把未测边界静默丢弃。

## 保管与交付

全部12案审计：60/60五类清单对、72/72封存bin、5700现存选定实体逐SHA及对应路径一致，wait/provenance/contract身份一致、guards空；不是1857源的逐案全量实体备份。两个早期layout案未包含ApprovedLantern测试实体副本，范围缺口保留；KeyboardTeaching实体首七案均已存在，新增显式copy条目不等于此前无副本。存档与续传库存仅比较清单，没有读取真实玩家存档语义。

最终当前1857源/897C#逐SHA匹配最后source-after，0新增/删除/漂移；相比v158仅上述4CS和JSON五路径变化。当前Debug6bin匹配最后fresh-binaries，canonical匹配helper。12个原engine身份都已结束；models旧PID被新conhost复用，依据创建时间区分，未误杀。

```text
source-after.txt E2943D8D3C0012B15196C9A237CE7FE4E05F341C5FB83F983EE0C3AE530E8BE4
helpers-after.txt 87BE721097E360F2A56C5A2C0212A71ACF8677CE686453EFCC9A36EAFB46CEF7
fresh-binaries.txt 5EC155DCA99599F1B99C09AA38B50C356C8413DA1E7EE1039451B9B4D878C7E1
canonical A0A93663BA3687FF3CDC5E56E1E9BC061EF4BE6C59349CF0176F9BBF33612A3A
HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6
index 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C
```

手动容量检查一次成功（2026-10-29T14:12:48Z）：媒体2,012,371bytes/21文件、Notify=false，未达新增20GiB档；无后台通知/自动化，本轮无新媒体，原件及副本不删改。

不恢复自动化，不付费生成，不再次采集原生dump，不修改真实玩家存档，不提交/推送Git，不删除资料。本轮没有新过程媒体；旧图仍为v158，不能当作本次修正后的画面。

## 下一步（不是已完成）

1. 以本轮已接受但过期的实际第二J为反例，修正“攻击中仍能K”的物理窗口观测；不延长攻击/补按/加预算凑绿。
2. 为layout退出的两个ObjectDB对象取得现有工具范围内的类型/所有权证据，再修释放；不把警告豁免、强制GC或换引擎当修复。若确需额外dump/工具，另需授权。
3. 完整Main/Explore目前仍保留v158失败，需解决能量/交叉火力与Grip9首次阻挡等实际障碍；组件或UI通过不能替代通关。
4. 继续正式美术、动作、场景、特效音频与正常速度画面验收；新版GL和用户实际看到/审美确认尚未发生，不重复使用旧显示授权。
