# v317 · 最新产物完整合版验证

## 事实、判断与风险

继续唯一Godot工程`G:/knight/Knight`的原完整十一项。v316最后修正的是测试中的精确资源审计，虽未再改生产玩法，程序集仍已改变；因此不能直接把较早768751／B14A版本的完整路线与最新7BD49／FAC8专项拼成同一版验收。

本轮只验证最新冻结产物，不新增玩法、改测试输入、延长预算或修改资源策略。Root逐项读取1907输入、6二进制、9公共helper均与v316最新专项零差异，未发现遗留owned Godot／dotnet／MSBuild；`godot-ai attach`属于既有连接客户端，不终止它们。

风险与边界：完整路线是引擎内键盘驱动的无窗口诊断，不证明OS键盘来源、正常窗口声画、目标硬件帧率或用户认可；历史Handle错误即便本轮未复现也不算根因修复。正式家族美术路线仍等待已发问题的实际回答，不将预选推荐项当批准。

## 冻结身份

- 当前输入数：1907。
- source manifest SHA256：`7BD49E8EC23C81D3019EF7FBD86AD2888E650D81C2A18033F977B368CFAB1AB1`。
- six-binary manifest SHA256：`FAC8F585F365EE6161BDD5BCEA4F1B60F0E291221F9C7D6BECE0EB4AC8F90CC6`。
- 9公共helper manifest SHA256：`9270219644DF596BFB9B101360F78D7845C56FE899728FDE10C8F1D2B677C700`。
- 受保护真实用户存档清单基准：`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`。
- 规范入口：`.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1`，只传`-Phase <phase> -SourceRows 1907`。

完整主线案`thruster-route-main-diagnostic-20261108-111033-086-9bbef60dc8634080b81e8dfb35028f26`已真实终结：runner0／engine0／250229ms、failure=null、guards=[]，三sink无诊断、stderr空；上述source／six-bin／helper／真实用户保护清单均前后同一。

## 当前回归记录

本轮6个phase均已真实终结并严格通过，四阶段batch外层亦真实exit0。保留原主线／探索600000ms、pre180000ms、post300000ms预算，不用旧180秒后段性能承诺替代现有门。

| phase | 本轮case | engine ms | 严格结果 |
|---|---|---:|---|
| Main | `thruster-route-main-diagnostic-20261108-111033-086-9bbef60dc8634080b81e8dfb35028f26` | 250229 | PASS |
| Explore | `thruster-route-exploration-diagnostic-20261108-111533-299-2c54ab1b3f0546fcb6e69e0a4f7f1cae` | 321774 | PASS |
| pre | `prologue-pre-20261108-112152-274-f1c54fab7b2848da9cb09648c40e8cf1` | 145783 | PASS |
| post | `prologue-post-20261108-112453-348-5d5f30a53d7a446e869672ddd89f7277` | 190677 | PASS |
| actions | `actions-20261108-112838-850-ec30c7a6fa6f4a3cb633db8da26d1d7d` | 10804 | PASS |
| typed recovery | `thruster-recovery-compatibility-20261108-112921-862-0bafe60aeb1d4bd99167d95685b53775` | 15715 | PASS |

Explore同样runner0／engine0、failure=null／guards=[]，当前source／six-bin／helper／真实用户保护清单均前后同一，三sink无诊断／stderr空。详细路线与磁盘save的独立18项核查已完成，零差异。

### Main原始结果与独立核查

独立只读审计16项一致性检查零差异，包含案内六产物、10份相关源码副本和真实磁盘save，并非只相信最后marker。PID44248内从Title实际Start，同一Player48049948164直到Boss14997153388087及出口；fatal attack86，exposure14064→attack14089→damage／Defeated14093→completion14272→heldEnd14333。BossDied／arenaDefeated／completion／count各1，恢复0／0。

40×72实体body完整超过出口右面2.7603px，slide support为原ArenaFloor；出口166／188wait。完成后60次held D（physics编号差61不误记为61次）中pose(7340.7603,1079.9943)、velocity(260,0)、body、save不变；Completed状态停止玩家物理且显示结算，不把保持velocity字段误说为速度归零。HP1／1是当前／入战HP，不是MaxHealth。

实际save89899字节、SHA `0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517`，与completion／held声明一致；Completed false→true，Boss=true，原checkpoint不变，6唯一片，无数量豁免。8遭遇激活／8击败不等于全76配置位清场。

14242对连续Motor断言支持21普通跳均grounded、coyote／buffered／assisted接受均0；8531门样本、mount gap0，三门各穿越1。5片10212→6片未鸣铃10250→鸣铃授权10898→三碰撞面禁用10899→左侧通过11034。部分连续检查只有同版源码与终端断言而无逐帧raw，不声称离线逐帧回放、全方向穿墙认证或all-walls。

### Explore原始结果与独立核查

本案PID13912，实际Title→Start，同一Player48049948164到Boss28389347956225／实体出口；两进程Player ID数字相同不代表跨进程同一个live实例。fatal attack99：exposure17931→attack17956→damage／Defeated17960→completion18139→heldEnd18200，四项事件／计数均1，恢复0／0。HP3／3是当前／入战HP，不是改MaxHealth。

原ArenaFloor支持40×72 body完整越出口3.191px，166／188wait完成；60次held D后pose(7341.191,1079.9327)、velocity(260,0)、body不变，physics关闭／结算面板可见。当前save90050字节，实际SHA `24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24` 与completion／held相同。终态7片、pre-Boss实际6片，差值为神殿上路temple-upper，不能写7片准入；checkpoint不变，无quota豁免。

18111对连续Motor断言支持33普通跳全grounded，coyote／buffered／assisted接受均0。门10036样本，bell／entry／exit穿越2／1／1；近侧epoch重挂1按原严格条件保留，不伪记零缺口或任意缺帧豁免。5片13767→6片未鸣铃13805→鸣铃授权14453→三面禁用14454→左侧通过14901。11激活／10击败，未击败仍为counterweight-lift，双向捷径均有，不等于全清或76槽full-census。

相对v316中间产物旧案，elapsed减少2471ms、绝对physics整体少1、PID及Boss实例改变；存档SHA／几何／跳跃／遭遇／重挂计数相同。不同进程单次耗时不构成性能提升结论；连续断言也不冒称逐帧raw回放。

v316最后已有的4个严格专项均属于上述同一冻结版本，详见[v316原始记录](2026-11-08-gear-callback-revocation-v316.md)：save及独立minimap cold reader、models、新移动齿轮、原recycling。它们是已保存的前一轮证据，不冒写成本轮重新运行。

普通pre原44项全部执行及退出清理通过，runner0／engine0、failure=null、guards=[]，三sink零诊断／stderr空；原180s预算不变。source／six-bin／user前后一致且同上述冻结版。pre helper清单为10行、SHA `48974C04588A48B79648908C0F503968EF1D51098F5CED3C4D52F24FE0B555E0`，其before／after同一：公共9行完全一致，仅按既有phase增加`test-clearance-native-red-admission.ps1`，不是helper漂移或把不同清单误称同一个文件hash。历史Handle不因本轮未复现而关闭。

普通post保留10个旧suite／75个案例、retired0／classified75及退出清理；failure=null／guards=[]。external与stderr各记录同一组4条原负例warning（不是8个独立问题）：ClawFrameChanged故意observer1、VisualScene根类型拒绝1、Completed→Recovering拒绝2；stdout无warning，三sink均无ERROR／FATAL／Handle。未引入命名豁免。使用原300000ms普通预算、190677ms，不声称满足旧180000ms性能门。post helper10行SHA `44025F9B622745A6847EA94E3F1C8A26E9C9459B77EF3A677E48A1D745D7A940`前后相同，公共9行相同，额外为既有`test-post-budget-consumer-v099.ps1`。

actions保留原完整键盘动作检查和6个真实保存Player／J能量边界，每个16轮只读poll无副作用：11.999947<12拒绝且发InsufficientEnergy，12与12.000053正确付费出招，自定义64上限及合法0成本均按实际Tuning。未改默认能量数值。typed recovery保留原2 delegates和typed cleanup，明确`legacy_component_fixture=true native_player_movement=false headless_only=true`，不是新增自然复活移动证明。

根最终逐案读回以上6案及v316最后4专项，共10个指定phase：各案build／engine均真实0且未超时、failure=null／guards=[]，1907 source与six-bin全部同一；每案helper／continuation保护前后同一，公共9helper无差异，user保护均BDDA0F...。各原生PID和contract相符、均headless。除post原4条负例warning外三sink无诊断。此为指定合并检查通过，不外推所有测试入口、全部组合或长期无资源故障。

## 执行边界

本轮没有GUI／OS输入／付费生成／额外native dump／真实存档实验／引擎、缓存、GC或预算改动／自动化／Git／删除。所有原始失败保留，未为资源故障重跑筛绿。媒体只在正常汇报时执行既有单次容量检查，不建立后台监控或到量停工。

moving-gear Task3的最新程序合版资格现已补齐；Task1／2／3程序计划完成，不等于T03整项或完整序章正式完成。Task3已有独立增量代码审查与本轮双路线只读审计，不新增重复代码审查、Git分支或清理脚本。既有延后Minor仍保留：场景fingerprint并非所有存储属性／匿名资源的完整证书，离线validation-only夹具未独立挡住任意调用路径的祖先junction；本次固定普通路径不等于已解决它。直接Free／同回调reparent／短暂撤权立即恢复、任意相对高速body连续碰撞也不外推为新增实测。

原十一项、正式家族美术／主角完整连续动作、圆弧悬吊款、历史资源根因、正常声画、硬件和真人验收继续开放。下一阶段可继续既有V05分件接入的脚图底锚适配（有明确字面量反例，须先RED）；家族造型路线问题保持非阻塞待答，不自动把推荐项算批准、不付费生成或接入未批准稿。不因每次文档更新重跑同一冻结游戏；下一次编译／资源改变再按影响做验证。

媒体本轮未新增图片／视频，只运行一次既有`-Check`：15,203,531字节、145文件、Notify=false，未达新增20GiB；不触发自动化、独立提醒或停工。
