# v172 · 闭甲回位不再绕过危险判断

## 本轮推进

在原完整地图与真实键盘输入中，split Bell重新闭甲后的分支现在也先读取原敌人/foreign危险，再决定L防御、回位或K。未改游戏生产机制、敌人数值、地图、原900物理帧/全程600秒或1.5位置/2速度阈值。

修复前的真实基线：v171 `084014…94bfeae2`，Bell剩1HP，physics10077攻击2杀死玩家；旧分支每次松L并提前continue，跳过后面的危险消费者。70帧观测年龄本身不是“控制器卡住”证明。

修复后 `090427…46f26bf0` 的事实：

- 10069发出真实L，10070起护盾active；10077攻击期间raw/sample L及shield均为true，玩家HP1保留。
- L按键可能同时引起合法方向移动，不冒称完全静止；未写位置/速度。
- 10109重新满足原neutral，10119第三次真实K令application2→3、Exposed、receiver=`../Health`，Bell仍1HP；**K本身不等于击杀**。
- 10121发出最后J，10127已选择后继敌人，随后确有同encounter的batch1波次Moth。这支持原Bell阶段已经越过；但Bell40行诊断已封顶，**没有独立最终Health1→0/命中事件行**，不冒充有该回执。
- 没有同击`ShieldResolved.Guarded`收据，不能仅由活盾和HP不变断言发生了格挡接触。
- 进入Moth阶段时仍是同Player、HP1、gallery完整支撑、recovery0/0；原无jump/dash/恢复及900预算守卫未失败。

## 完整路线仍失败

physics10270，真正致死来源是新波次 `GraveLampMoth` actor24364191844956 / hitbox24364275731229 / attack1；玩家1→0，energy0、盾off，触发原零恢复断言。不是被追逐的常驻bound-ghost同一实体。当前Explore不合格，Boss/实体出口未完成。

下一前沿的已核事实：resident选择门允许`|dy|≤74`，现行Moth K策略需要`dy<-74`；该次138迭代dy约−70.55…−19.94，K门未进入，玩家追出gallery落到winch barrier。`claw_target=unavailable`只是当前机械爪没有锁定对象，**不是**目标ThreadTarget不可用/无害证明。后续先核真实arena/实例资格，复用合法pursuit范围或增加driver当前J/K可达性选择，不删除resident、不改玩家机械爪夹角优先或遗漏其危险。

## 审查与验证边界

唯一fresh review：C0/I0/M1，无二审。M1为每episode前12次之后方向反转可能被诊断去重隐藏，属于观测完整性建议，延期，未改变任何控制。

| 案例 | build/native ms | 结果 |
|---|---:|---|
| Explore090204…6e804a9b | 29079 / 未启动 | CS0841诊断计数声明顺序错误；构建FAIL，非行为RED |
| Explore090427…46f26bf0 | 32375 / 206305 | Bell原失败点已越过，后续Moth击杀玩家，完整路线严格FAIL |

后案三sink仅原零恢复断言，无WARNING/FATAL；本轮未见FATAL不关闭v170资源问题。完整普通pre/post和硬件/真人/视听未以此认证。

保管：两案9对清单、1124选定源实体、20helper、6bin逐件SHA一致，案内零差；3个实际build/native PID均结束。当前1865源/6bin/9helper等于最后**失败**案，只证明字节绑定。首案没有成功binpair，不虚构native。

## 独立判断与边界

- 复用原地图行为RED，不造假HP1或关闭AI的替代夹具；代价是一次验证约4分钟，但确实覆盖原早continue。
- 仅修复既有批准输入策略内的消费者，不重复请求设计批准；不新增玩家能力或放宽站稳/伤害/预算。
- 实际L合法应对、第三K与后继阶段分开陈述；缺独立接触/最后伤害行就不编造。
- M1延期；全部十一项、Main/Explore、历史资源、正式美术/UI/动作/音频、硬件/真人与交付仍ACTIVE/OPEN。没有Git、付费、窗口/系统输入、媒体采集、自动化、删除或真实存档写入。
