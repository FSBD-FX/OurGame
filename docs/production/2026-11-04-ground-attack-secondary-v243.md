# v243 · 半钟刃地面挥刃与悬灯的连续受力来源

状态：两个生产文件已接入，真实时间线和保存Player的J输入均已RED→GREEN；一次范围审查C0/I0/M2，最终同版十五门**12严格PASS/3FAIL**。**这是正式表现的一段增量，不是完整角色重做、真实力学升级或正常速度美感认可；原十一项及R-I/R-C/R-P仍OPEN。** 唯一G:/knight/Knight，仅无窗口内部输入，原失败/预算保留；无付费/GUI/系统输入/额外dump/新媒体/自动化/外部发布/删除/真实存档操作。

## 实际生产变化

- Timeline在得到既有权威GroundAttack后，使用其Phase/NormalizedTime/锁定Aim，映射连续四段目标0→-.35→1→.25→0；各段smoothstep、边界同值/零斜率，转到当前朝向父空间。没有新攻击状态、输入读取或独立计时器。
- SecondaryMotionInput末尾增加optional GroundAttackDrive=default；有限化/分量限幅后叠加到原Mantle/Lantern targets，Tether继续由灯/线轴差驱动。只使用原临界阻尼弹簧/原bounds，ShoulderSpool仍由爪/丝线独占，真实命中脉冲额外保留。
- 起势轻反向、发力释放、经过维持、收势归零；失去动作权威只移除目标，现有动量自然回稳，不瞬间清零。沿锁定Aim可表达横/上方向；既有起手上下文锁定不变，起手为地面攻击后离地仍是该动作，真正AirAttack/DownAttack/爪/受伤等接管不借地面源。
- 暂定目标强度是开发选择：mantle1.5/.75px与.04rad，lantern3.6/2.4px与.12rad，最终仍服从旧限幅；不是新增材质/位图、整图缩放或已获美感批准。FormalVisual层序、腿/二级槽ownership不改。

仅两个生产CS、四个测试CS变动，无新CS/UID，原生输入数量仍1888。实际CombatTuning、Hitbox Active/伤害、输入、Motor/碰撞/步态、落伤/单跳、爪/推进/取消、地图与76普通敌人未改。这里的‘受力’是无单位表现驱动，不冒充刚体质量/冲量模拟。

## 实际反例与首批GREEN

原证据保留在`G:/knight/验证证据/october13-stable-claw/`：

| 完整案例目录名 | 引擎ms/exit | 结果与边界 |
| --- | --- | --- |
| thruster-rig-compatibility-20261104-171152-372-4f6e5856e6ea42c19e48c972d39c95eb | 658/1 | 实际Timeline零速挥空Anticipation没有灯/系带来源，行为RED |
| thruster-presentation-rig-20261104-171233-517-a8107d22e2784b7a8df3e1d1bededd64 | 5741/1 | 真实保存Player/J同一缺口RED，非编译或输入前提失败 |
| thruster-rig-compatibility-20261104-172046-549-367e0241ae704f0587e9d32c01051a33 | 1386/0 | strict PASS/stderr空；四段/双朝向局部一致、命中额外、退出回稳、原11formal叶/mantle与新增model控制 |
| thruster-presentation-rig-20261104-172131-603-5aab97c9bb6a4915ad932cdc534b52a8 | 18799/0 | strict PASS/stderr空；两朝向各44真实发布样本、四段、同Formal、单次J/nohit/实体支撑/root不漂及回稳，旧8方向/rig原套保留 |

直接模型另验Up反应、线轴独立、非法/极值有限化、驱动移除仍带余速再回稳、原常值目标分帧一致、零delta与反射。原命中和挥空的差别继续验证。真实J观测从正常FramePublished读取并收集事实，末端验收，不手造表现帧或写速度/身体位置；这些是headless几何/时间证据，**不是可见幅度、绘制细腻度或用户手感认可**。

## 相关恢复反馈夹具：保持四帧功能门

v242顶部RecoveryFeedback夹具真实RED expectedPlaying/actualIntro；本次只把StartNew之后的单帧换为既有WaitForPlaying。`thruster-recovery-feedback-20261104-171321-219-649fe31af70e4a8790017a7f32a1acc7`功能全部通过，completions1/观察2帧/原cap4，但exit0仍6 ObjectDB/2 resources退出，owner严格FAIL，未借marker抹掉。

最小清理候选是在原功能断言之后，复用原TestSceneTeardown.DrainAudioOwner：撤销本夹具居民续体，关闭Audio/释放自有资源，实际观察新mix（原60帧/2000ms界限），再释放Level；嵌套finally保证drain失败也不跳过Level释放。不是GC/保活/任意延迟、不改正式退出或恢复4帧预算。匿名6/2没有资源身份，不能直接认定音频就是全部根因。

`thruster-recovery-feedback-20261104-172233-611-9af4bfc5ec694e25b7c38dbe6c28bd0b`已strict PASS/4454ms/stderr空，功能仍2/4帧，实际mix1帧/67ms。只证明此路径的释放闭环，不关闭既有geometry匿名FATAL。

## 同版宽门与未完成范围

当前`thruster-ground-gait-20261104-172802-142-b57526deec974e3b975a5a4efbaa3655` PASS/13695ms；`actions-20261104-172902-467-8a562b94870545f78bb7137dc767a347` PASS/9528ms；`thruster-formal-claw-phases-20261104-172951-520-07127e4801e34799b7039b294965b6e5` PASS/4797ms；`thruster-recovery-compatibility-20261104-173038-287-bd805b36ebcb41baa6bd82df40608c97` PASS/12210ms。

`prologue-pre-20261104-173124-545-ae2e64c1abb24dc98ab2b97348c9896e`严格FAIL：engine-1073741795/17690ms、非timeout。gchandle.is_released/mono_object_disposed_baseref→GodotObject.Finalize→GC.RunFinalizers，最后相机cycle1 mount-return，原44项未跑完且尚未到新模型控制。不能由最后日志定位资源或断言与新构建无关；未重跑筛绿，原始栈保留。

### 最终十五门12PASS/3FAIL

所有案例均在`G:/knight/验证证据/october13-stable-claw/`，以下是完整目录名，全部终态且未超时：

| 完整案例目录 | 原生ms | 严格结果 |
| --- | ---: | --- |
| thruster-rig-compatibility-20261104-172046-549-367e0241ae704f0587e9d32c01051a33 | 1386 | PASS，新增source/model＋原formal叶 |
| thruster-presentation-rig-20261104-172131-603-5aab97c9bb6a4915ad932cdc534b52a8 | 18799 | PASS，真实双向J＋原rig |
| thruster-recovery-feedback-20261104-172233-611-9af4bfc5ec694e25b7c38dbe6c28bd0b | 4454 | PASS，原4帧功能与真实mix清理 |
| thruster-ground-gait-20261104-172802-142-b57526deec974e3b975a5a4efbaa3655 | 13695 | PASS |
| actions-20261104-172902-467-8a562b94870545f78bb7137dc767a347 | 9528 | PASS |
| thruster-formal-claw-phases-20261104-172951-520-07127e4801e34799b7039b294965b6e5 | 4797 | PASS |
| thruster-recovery-compatibility-20261104-173038-287-bd805b36ebcb41baa6bd82df40608c97 | 12210 | PASS，原两个Task10方法 |
| prologue-pre-20261104-173124-545-ae2e64c1abb24dc98ab2b97348c9896e | 17690 | FAIL，原生finalizer FATAL |
| prologue-post-20261104-173217-684-09b811aab186463eb171d5f63a021f99 | 193829 | PASS，原300秒门 |
| resident-geometry-20261104-173712-707-2419b645b5254bde8f2c4679f10732eb | 8589 | FAIL，原生finalizer FATAL |
| resident-save-compatibility-20261104-173757-617-4c575f6325ee4e108f78c3c1cdaeb689 | 13289 | PASS |
| models-20261104-173848-305-fb0542dee4254e178cde341222d93fa2 | 521 | PASS，仅爪模型/到达几何子集 |
| keyboard-20261104-173925-265-a94384d1a81b4647ae577533b47a2ebd | 20582 | PASS，独立headless标题另8301ms/0 |
| thruster-route-main-diagnostic-20261104-174035-342-5d0412e8b5e54d919cbda5189735885f | 147650 | FAIL，原899配重战斗未清 |
| thruster-route-exploration-diagnostic-20261104-174345-434-26a9af4b48af44d2b541e2f8db9642ac | 353971 | PASS，Boss/实体出口 |

geometry当前仍同报错族的匿名gchandle finalizer栈，最后输出Archive与首BODY_CANDIDATE；没有资源身份，不能据此指定哪一个Resource或归因新纯值驱动。post四WARNING均原明确负控：throwing ClawFrameChanged observer、非法VisualScene root、两次Completed→Recovering拒绝。其余严格PASS无额外ERROR，三个失败均照原门拒收，不借process退出0/成功标记/父loop最后状态忽略。

Main仍原899门：Rust余HP1、playerHP2，7次J started/connected、3次爪，signed gap18.279，能量约.143/.157；失败未生成完成result JSON。没有改储备/HP/伤害/时限或注入回充probe，不据此断言真人不可通关。

Explore当前实际JSON为automated/syntheticFixtureFalse，从标题同Player，23访问/11挑战、11遭遇激活/10击败，未清配重遭遇；Boss/SafeExit/Completed为真，恢复0/0。真实第99击击败Boss，出口floor6080,1116,1600,40支撑40×72完整胶囊；死亡/场地/完成事件各1，向右保持60输入帧后位置7341.191,1079.9327和存档SHA `D68DEC7993D42D14D26D4F98E88C00B907350568CA6F6B52E552E87F08A56779`保持。operationCoverageComplete/humanValidated/fullGoalAccepted皆false，不能签全敌群/全操作或正常速度视听。

### 当前源码/二进制/真实存档保护

十五案source1888清单SHA均`B482BC060BA43EE503199F45082ACDF2B27252383F6BF80EC5FEF0D2010986F6`，六binary清单SHA均`F386A072C239E67FC0C305CABE260B8238D1670155704C946A6841A4B1B4E665`。这是清单文件Hash，不冒充单DLL或发布包。source/helper/user/continuation前后及fresh/after binary共75文本对一致；所有终态后再读1888当前文件零差异，无自有引擎/构建进程，真实玩家文件未动。

媒体一次性Check7427750字节/86文件，NotifyFalse，无新截图/录屏/自动化或独立提醒。局部资源清理通过不关闭其余匿名FATAL；原代码/数值/目标范围不因后台门而缩减。

## 一次独立范围审查与延期

C0/I0/M2，审查者独立读取三个专项wait/stdout/空stderr，无阻塞修补、无二审。M1延期：三秒退出回稳测试不能证明接管当帧；CheckpointRecovering标志不等于真实协调器ClearAttackState，当前实际生产清理仍在，后续需公开恢复路径/当帧与下一帧。M2延期：同一攻击中途翻面/零delta及Timeline向上GroundAttack组合尚未独立验，已有固定双向与纯模型Up/反射不冒充该组合覆盖。

审查限六文件指定新增块，不是Base=Head dirty全树、旧弹簧/优先级、生产物理/脚掌算法或完整保护harness的重新认证。新support指标只直接证明真实接地与root不漂，不单独证明脚底绘制接触质量。原GroundAttack起手后离地仍保留身份是明确旧合同。无GUI/录屏/听感，幅度和美感仍暂定；当前资源/Main及原十一项/正常速度/硬件/用户认可继续原门，全部裁定在过程账。

原global资源finalizer、普通Main899、旧HP4未归因等不由本专项关闭。用户要求的完整动作设计、正式分层美术、真实惯性/物理手感与全十一项不缩减；正常速度视听/UI/相机、目标硬件与用户试玩认可仍必需。

Task1生产接入与专项已证，Task2指定恢复夹具已闭环；整体仍三FAIL，不签计划/正式序章complete、不冻结为发行版或外部发布。下一继续身体重心/握持与收势的完整动作链，并按现有证据分开处理资源稳定与普通Main，不以本次附件响应代替‘精美完整’。

[设计](../superpowers/specs/2026-11-04-ground-attack-secondary-design.md) · [执行计划](../superpowers/plans/2026-11-04-ground-attack-secondary.md) · [过程账](../../.superpowers/sdd/2026-11-04-ground-attack-secondary/progress.md) · [完整原待办](2026-10-18-project-remaining-backlog-v001.md)
