# v080 · 配重返程与尸体挡爪修正

唯一工程 `G:/knight/Knight`。原批准十一项及全部正式交付条件保持 ACTIVE；这不是正式试玩版。用户最后更正为“没看到”，本轮没有重新开窗、发送系统输入或追认画面/遮挡验收。

## 实际改变

1. **配重横廊返程**：034808 原失败现场证实，A键/raw及sample轴均为-1，但身体被 `GearResidentRightShelf` 右缘挡住。该实体顶比横廊高36px，间隙32px小于身体直径40px。不是输入丢失。仅 shelfLanded 分支改为现有的一次地面起跳，越过棚面后从左侧落下、向右回到原 PistonTwo。真实完整身体支撑、中立停稳、同Player/HP/残片保持，96/109帧完成。原2975阈值仍在60帧内通过；109是原60+4+45的合计，不另加时间。首次机械爪转移150、原15保持及首次转移零普通跳仍保留。
2. **下层站位**：旧驾驶器用2800减速度估算，但实际推进器模块地面摩擦是1800。改用当前Motor/module的离散预测，最终仍以真实56–64距离、V0、raw/sample中立和支撑验收。18/57帧站稳，不改生产移动参数或攻击距离。后续调用方仅接受原3跳加显式验证的分支0/1，不接受任意实测跳数。
3. **实际游戏故障——死亡游荡者仍挡机械爪**：041310在ready且scheduled均成立的450/600/675帧仍选不到Bell；唯一同mask、完整排除caster子树的原生射线实际命中保存的 `scrap-wanderer/Hurtbox`，HP0。不是仅仅75帧发招周期错过。Rust居民现在死亡时撤销body层、hurt/target层与目标组，保留尸体、保存mask和身份；活体睡眠/幽灵隐身仍保留原物理body。所有居民在死亡、pending void、重置后重算权限，Monitorable延迟提交读取当前生命，普通wave分支不扩改。resolver、敌人HP/AI、人数、伤害及地图没有改。

独立测试先在原保存Rust、再在实际Scrap保存叶复现RED；042318 GREEN保留原15案例及默认wave/视线聚合。实际resolver证明活体可选、尸体不再挡后方Surface；死亡60帧不移动/不愈合，sleep/wake不复活权限；检查点恢复原层/mask/受击监测/抓取，真实原生PlayerAttack能连接扣血，同一次Died信号内重置也不会被旧延迟提交禁用。不是通过Free尸体或改LOS蒙混。临时Main门位与单射线取证在原件保存后移除，避免重复查询。

## 全部本轮原生结果

原件根：`G:/knight/验证证据/october13-stable-claw/`。下表时间是目录名中 `20261024-` 后的唯一时间段，完整目录及冻结清单都保留。

| Phase / 时间 | native / ms | 严格结果与边界 |
| --- | --- | --- |
| Main034808 | 1 / 82307 | FAIL，原Walk60精确棚缘碰撞 |
| Main035515 | 1 / 87220 | 返程96/109通过；下一站位67.238超出原范围，FAIL |
| Main040132 | 1 / 105814 | 站位18/57通过；原900战斗未完成/K0，FAIL |
| Main041310 | 1 / 104326 | 死Scrap受击框挡Bell射线，原战斗FAIL |
| patrol041832 | 1 / 43630 | 原保存Rust尸体挡爪RED |
| patrol042057 | 1 / 43634 | 实际保存Scrap叶同一RED |
| patrol042318 | 0 / 45496 | PASS，原15+默认wave/视线及上述新死亡/重置控制 |
| Bone042550 | 0 / 7852 | 原7行为返回，但退出7 ShapeRID，严格FAIL |
| Ghost042631 | 0 / 6463 | 原3行为返回，但退出4 ShapeRID，严格FAIL |
| void042709 | 0 / 13508 | PASS，原坠落/延后死亡控制 |
| Main042755 | 1 / 96481 | 原Bell真实K三次→Exposed、原战斗返回；调用方HP4→3约束FAIL |
| Explore043149 | 1 / 48277 | 深层探索HP0，physics2460请求恢复，零恢复路线条件FAIL |
| pre043313 | 0 / 167384 | 完整前半组严格PASS，本案无Handle/ERROR/FATAL |
| post043634 | -1 / 180071 | 原180秒TIMEOUT；45 enter/44 return，index44未返回 |
| models044011 | 0 / 550 | 最后普通模型PASS |

总计15原生，4严格PASS/11严格FAIL，无构建失败。纯入口96例、实际旧producer日志消费33例也重新通过；它们不证明当前正常双路线或最终视听。

最后post registry在engine157766ms才开始，最后index44 `RecoveryMovementAndJumpCancelOnlyPresentation` 于179471ms进入；三日志Handle/ERROR/FATAL均0，存在原案例故意抛observer的WARNING。超时仍是真实FAIL，不加180秒，不把全部耗时归因于最后案例或VFX，不把安静一次说成历史资源崩溃修好。

### 下一处因果边界

Main042755尸体修复后确实选中原Bell，实际三次K应用及Exposed成功、原战斗结束，但玩家掉1HP。原伤害行415：Bell Attack4，selected/danger同实例，受击时不在J、朝向正确、未过热，能量.34999886、L已松开。居民J在37/89/113，120接手Bell，139提前整段Telegraph举盾，173到35%撤盾，183受击。因而下一次只检验Gear驾驶器的35%保留门槛与实际 `CanBeginGuard`，不同时改晚起盾时间、K75、敌人或HP断言；尚未实施，不预称免伤已解决。

Explore有实际落伤和机关/敌人损失，最终伤害来自 `frozen-detour.04/bound-ghost`，原始恢复观察为DEATH RESET。没有把它说成尸体修复回归，也没有清除该居民或豁免落伤；下一次逐段检查实际轨迹、防守和伤害来源。

## 独立审查与裁决

一次新上下文gpt-6-astra/high只读审查四文件和指定快照，未发现Critical/Important；没有复审或新代码修补轮。Basin/Main归属基线为034808/before，Rust/Patrol为models033906/before，不把整个dirty树冒认为已审。

审查建议的Minor“双目标组恢复未覆盖”经根执行者重分级为非缺陷：重置断言实际包含 `target.IsAvailable`；ThreadTarget2D:27是 `base.IsAvailable && IsInGroup(thread_targets)`，base又检查grapple组，所以只恢复任意一个组都会失败。保留行为断言，不为重复比较属性而新增代码。代价：若将来同时改可用性契约，需要重新审查该消费者。继承的Title/replay失败清理包裹较弱这一Minor仍延期，未证明它是资源崩溃原因。

执行裁决与代价：

- 原位dirty制作、禁止Git写入/新worktree/删除优先于通用提交清理助手；代价是按快照归因，不能声称clean commit通过。
- 原A-only/零普通跳返程是旧地图驾驶器假设，不是用户禁止普通跳；现有一跳替换只限该分支。代价是人类容错和完整往返仍需正常路线证明。
- 原阻挡观测不改输入/碰撞权限，借用slide不Dispose；代价是有少量失败取证开销，不能当正常路线通过。
- 站位使用实际模块但结果仍验原范围；代价是只修驾驶器预测，不代表移动手感已获真人认可。
- 下游明确0/1分支和原3跳相加，不宽放全局计数；代价是新分支仍须完整本地跳数控制。
- 死尸撤权限而不删敌人/改resolver；代价是所有家族、卸载重载和完整路线还须继续验证。
- 暂不把75门或伤害数值当根因；保留实际掉血FAIL。代价是本轮不交付完整Main。

审查所有Declined项均承接如下：

1. 完整Main：根后来实跑到战斗结束仍HPFAIL，继续OPEN；代价是不能交付通关版。
2. Explore/full pre/post/models：根后来全跑，分别FAIL/PASS/TIMEOUT/PASS，不冒称审阅者独立运行；代价是完整双路/回归仍未闭合。
3. Patrol042318：代码审阅与根原生证据分开，根结果严格PASS；代价是不假称第二次独立重跑。
4. Bone7RID：严格FAIL，不猜所有者或Dispose许可；代价是该家族退出资源仍未合格。
5. Ghost/void：根终态Ghost4RID严格FAIL、void PASS；代价是不从代码语义推断全家族资格。
6. 资源根因：历史Handle/FATAL/RID和post超时OPEN；代价是仍有崩溃/资源风险。
7. geometry身份诊断：方案未实现；代价是实际finalizer所有者仍未识别，不能以当前Load定归属。
8. geometry owner旧raw_terrain127/实际143：独立缺口未修、不豁免；代价是后续须独立事实及负控校正，geometry仍不合格。
9. reader/controller与first-hit：未改排序，取证和实际施放分开；代价是不外推至所有遮挡/输入情况。
10. Intro→Playing/回调退出/LoadLevel清理：本轮未改这些历史合同，保留实际回归边界；代价是不重签所有故障分支。
11. 全家族/50点/完整存档卸载生命周期：Scrap专项不等于全人口原生资格；代价是76普通/Boss/NPC还需完整实战。
12. 十一项/最终视听/硬件真人/发行：全部OPEN；代价是不宣称完成、不打正式包。
13. 真人展示：最后“没看到”有效，无窗口和视觉认可；代价是教学/灯避让与正常速度体验仍待确认。
14. 全dirty树与clean commit：仅四文件指定快照及本轮保管核验；代价是不能借此认证其他未提交修改。

## 保管、容量与继续顺序

15案75对source/helper/user/continuation/binary清单逐字节一致，无缺失或不符；90份封存二进制匹配。最后普通1776源/892C#、9辅助、6二进制实时漂移0；没有未结束的本轮native/build。

- Source清单：`D7D43121EF2F95F0E6880D95B24FD5461B067B1E3DF46B589F8481E12E2B903E`
- Binary清单：`2859F9F55CA4A6EBF65FBFDE5509AE31E57D94B366CFEE79EFF1035BC27C1617`
- Knight.dll：`3BD1C4C738F4D9B90EE9B2C8681C9E45E793BDBC251F8630BD531A64A7EBEDC7`
- HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、原branch及index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C` 未变。diff --check按CR-at-EOL退出0，169条原LF→CRLF提示不改配置求消音。

一次正常汇报内媒体检查UTC2026-10-23 20:42:23：current860284/base3508/growth856776 bytes，Notify=false；新媒体/美术/声音0。没有自动化、付费、Git写入、真实存档语义读取或用户资料删除；20GiB仅随正常汇报提醒，不停工。

继续：单变量Gear防守门槛→实际Explore伤害/轨迹→资源所有者与geometry消费合同→移动姿态下教学/头灯交集，之后仍须原全部正式美术动作、UI音频、硬件真人和版本发行。无窗口测试不能取代真人看到与认可。
