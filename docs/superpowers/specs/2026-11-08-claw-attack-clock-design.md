# v327 · MechanicalClaw遮盖攻击时钟

原V03/V04/R-C动作连续性，沿v326真实Hurt时钟修复继续，不新造战斗规则。事实：CanBeginGrapple不限制已经开始的Combat，仲裁只压同拍J+K；先J后下一拍K源码可达，Claw优先早返回而Combat继续，ClawIdle只零时间刷新旧身份。因此存在过期攻击补播候选，尚需native反例。

保存Player、实体floor和可真实命中的静态Surface，两朝向×无K/真实K匹配控制，内部键盘事件而非OS输入。J先单独接受，松J下一拍K，真实抓取/附着保持至原攻击结束，再K取消，经正常收回到Idle；不写Claw/Combat状态或姿态，不改变角度优先/射程/禁抓规则。

priority100在Combat之后记Hitbox.Monitoring单有效窗与AttackStarted原ID/方向、Claw目标和附着/取消/Idle；自然FramePublished采集动作/累计时间。沿v326准入：原60Hz、累计Process/Physics偏差≤2/60+1e-6、正时间发布间隔≤2ticks；有效窗关闭后超过2ticks的Burst/Contact为过期发力。控制须四期/零过期；K须真实抓取且与Combat重叠，维持到真实结束，实际取消回Idle及最终回握/HP不变/普通跳0。准入失败不冒充功能RED。

仅在实际RED后让已有未取消攻击的隐藏年龄在MechanicalClaw优先返回前按原delta/.025停顿前进，Claw姿态优先/真实Combat/移动/抓取模型均不改，不重启/取消攻击、不改原Hurt规则。无Claw路径与Hurt结束步仍恰好推进一次。旧Thread生产输入固定default，没有同等正常入口证据，本轮不顺带改Thread早返回政策。

纯Timeline用10种非IdleClaw态×30/60/120×有无hitpause，遮盖.20s后零时间撤罩须追平无罩对照（默认原年龄.233333s及原停顿量化）；另验短攻击过期/freshID、取消及非有限delta，不替代真实抓取反例。完整11项目标及正式声画、美术、硬件真人仍OPEN。

当前只有无窗口权限。Root独占编辑/构建/native；保留dirtytree/所有失败/真用户存档。禁GUI/OSinput/付费/GC/cache实验/新dump/Git写入/worktree/删除/自动化。v326前段FATAL未定因，本轮不重复同版筛绿或擅调GC；新代码确有行为变化后才做当前同源完整资格单次运行，失败继续如实保留。
