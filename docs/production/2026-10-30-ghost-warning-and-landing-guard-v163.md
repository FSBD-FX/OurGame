# v163 · 敌人起手朝向与机械爪撤爪落地守盾

状态：原完整十一项目标继续ACTIVE；这不是序章正式交付。最终普通六阶段3 PASS／3 FAIL；完整Main／Explore均未通过。项目仍为 `G:/knight/Knight`，窗口确认仍NOTSEEN，本轮未开游戏窗口。

## 实际游戏改动

修正Rust载体的起手朝向：居民显形或收招后，若已经进入攻击距离，不经过Chase移动也必须在**新预警开始时**朝向主角。预警开始后和Active期间不追踪转身，原攻击时长、碰撞形状、伤害、速度、血量和能量规则保持。

真实保存lost-echo对照曾观察到：actorX263.199、主角X280，幽灵仍Facing−1、攻击区X229.199，持续朝背离玩家的一侧刺击。修正后同条件Facing+1、攻击区X297.199，无盾实际接触掉血；不是只改图像或测试期望。

正常探索的自动键盘消费者补了Grip8落地守盾：沿用Grip1原有输入作用域，只在**原cast、原目标、原K撤爪Cancel**都匹配后，按当前保存Ghost同attack／phase的真实发布和AttackHitbox位置推荐方向。实际输入仍经方向键＋L、输入采样、能量和生产护盾解析。活爪阶段不提前L；失效发布、失去资格、完整落台或故障后释放。没有增加K／Space／Shift，不放宽原300tick转移或全线预算。

本轮生产改动仅 `scripts/enemies/RustShellSquire.cs` 的起手朝向；其余为测试、路线消费者与验证脚本。未增加新敌人数量、地形、正式美术或音频资产，素材表既有状态不变。

## 真实验证与边界

### 保存Ghost和保存Player，不以“没接触”代替格挡

独立World2D，仅挂树前平移原split-level定义，使用原1.2秒隐藏／4秒显形profile；这不是正常地图或完整空中组合。

- 两侧无盾对照均实际Connected，HP5→4。
- 方向键＋L对照实际同攻击Connected、ShieldResolved Guarded、fresh L和ShieldActive，HP5→5。
- 真实A键从预警前方绕到后方，预警及Active仍保持原朝向；没有写位置来造绕背。
- 初始隐藏、错误发布编号、非Normal伤害类别、休眠、唤醒后旧发布和离树均拒绝授权。
- 审查补充的自然转换：从有效攻击开始，不改时钟／重置／HP，自然显形→隐藏时旧权限清除；下一显形需要新攻击编号。组件driver实际松L已验，**不把它说成完整落地作用域在自然隐藏下的输入寿命已验**。

### 完整探索实际越过Grip8，随后Grip9仍失败

带原显式碰撞日志的完整Explore案 `025859-934-72ed4fff238a44a4aa7baf01f7b3521b`：

- cast27原撤爪后，physics9416实际Guarded，原HP1保持；此前同处死亡记录保留。
- physics9420落到原AscentGrip8完整支撑，原Task4返回；141／300、NextCast27→28，未加跳／喷／爪或恢复。
- 后续cast28仍受阻。实际locked LOS在9465–9470命中存活幽灵的 `@CharacterBody2D@62/Hurtbox`；9470原门为los=false、saved_contact=false、stalled=false。**不是已证明的地板阻挡**，不能据此忽略地板、敌人或放宽碰撞。
- native1／201608ms，严格FAIL，未到Boss／出口。不是完整Explore通过。

下一抓取前需要验证合法清敌／抓取衔接。现有通用战斗helper并非Grip8即插即用；Ghost若仍HP3通常需三次独立J命中，不能凭4秒显形就假定三J一定安全，不能偷偷放大任一原转移预算。

## 本轮原生记录

所有案位于 `G:/knight/验证证据/october13-stable-claw/`，表内时间前缀均20261030。16次构建全部exit0；16次原生严格6 PASS／10 FAIL，包含夹具问题和明确RED，不是游戏6／16“完成率”。没有ExpectedFailure豁免或增加上限。

| 阶段与时间 | build exit／ms | native exit／ms | 严格结果／含义 |
| --- | --- | --- | --- |
| stationary-guard 024229 b392 | 0／28888 | 1／1661 | FAIL：夹具漏保存profile绑定，非行为RED |
| stationary-guard 024414 72d9 | 0／26776 | 1／7760 | FAIL：无实际接触，待取证 |
| stationary-guard 024725 8af0 | 0／42814 | 1／7911 | FAIL：实际背身刺击、零接触 |
| stationary-guard 025037 ba23 | 0／31404 | 1／5362 | FAIL：朝向已正确；缺守盾策略的实际掉血RED |
| stationary-guard 025244 39b4 | 0／33655 | 0／10229 | PASS：四个真实组件控制 |
| stationary-guard 025428 ac65 | 0／27067 | 1／538 | FAIL：Grip8准入策略RED |
| stationary-guard 025629 c72a | 0／51360 | 0／9164 | PASS：新准入与原fault／组件 |
| Explore trace 025859 72ed | 0／48006 | 1／201608 | FAIL：Grip8通过，Grip9活Ghost遮挡 |
| stationary-guard 030554 05d6 | 0／51259 | 0／9175 | PASS：原／新控制，尚无自然隐藏控制 |
| stationary-guard 031038 c2ee | 0／28628 | 1／9914 | FAIL：自然隐藏通过后，负控误造了当前有效编号 |
| final stationary-guard 031240 2ef9 | 0／31708 | 0／13628 | PASS：自然隐藏／新编号与全部控制 |
| final models 031336 5d83 | 0／28316 | 0／550 | PASS：仅机械爪模型／到达几何，不是短喷全部单测 |
| final pre 031415 5bb0 | 0／27942 | 1／112150 | FAIL：首个同sector恢复完成事件expected1／actual0 |
| final post 031652 a570 | 0／32451 | 0／204004 | PASS：原75＋owner8，原4限定WARNING |
| final Main 032101 32fb | 0／30394 | 1／136069 | FAIL：配重混战能量0，被机枪手弹体12击杀后恢复 |
| final Explore 032356 d331 | 0／39864 | 1／182761 | FAIL：Grip8原141／300通过，Grip9 cast28 Obstructed |

前段失败位于 `FirstCombatGuidanceRuntimeTests.CheckRepeatedFailureCard` 原Settle后，原因尚未定位，不直接归因朝向修正、性能或用户操作。后段本轮安静不关闭历史Finalizer／句柄／RID故障。

普通Main的最后伤害为physics6619，`canyon.v2.resident.counterweight-lift.04` 的precision-burst攻击12，HP1→0、能量0、未过热；随后正常恢复到盆地检查点，触发通关证明的零恢复／位置连续性断言。不能把它重新命名为已证实的非法传送。普通Explore也实际保住Grip8 HP1、发生一次Guarded、完整支撑返回，之后cast28原遮挡失败；普通编译没有身份日志，不把trace碰撞ID冒称它自己的ID。两条完整路线都未到Boss／出口。

## 单次审查与裁决

一次fresh审查C0／I1／M0；I1是“已有有效发布后自然隐藏”的覆盖缺口，不是已发现新的生产损害。先用实际旧native日志执行新增严格门，正确拒绝缺失转换收据（覆盖RED，不算游戏失败）；再补自然转换。首轮转换已通过，但后续负控用回调更新后的contactAttack+1恰好等于新有效编号，正确未被拒绝；修正负控为当前发布编号+1，并固定初次接触编号。最终普通组件实际GREEN。无第二次审查。

执行取舍及代价：

1. 沿用用户指定dirty G根、root原地执行／手动ledger、只读代理及一次最终审查；不建worktree、不提交或清理。代价：只审当前切片，不给整个脏分支／发布背书，工作区资料继续保留。
2. 新取证确认起手朝向缺陷后修生产载体，而不是移动测试主角规避。代价：全部Rust载体可能更有威胁，必须重跑完整路线，不能只凭Ghost组件宣布兼容。
3. 当前runtime原始证据由root读取，独立保管审计检查哈希；代码审查未独立重跑游戏。代价：源审查不等独立通关认证。
4. Grip9是真实活Ghost受击区遮挡，暂不修改物理／优先级或忽略敌人。代价：完整Explore仍FAIL，合法清敌阶段尚待制作验证。
5. 完整十一项／标题→Boss→实体出口继续OPEN，当前安静回归不关闭历史资源原因。代价：不称正式稳定交付。
6. 没有新显示许可，保持NOTSEEN；正式视听／动作／UI／目标硬件和用户试玩继续OPEN。代价：没有新的画面或手感验收，不把日志当精美度。
7. GhostNormal局部策略不保证多源、其他族系或Heavy全覆盖。代价：混合威胁仍需实际策略；原盾耗能／过热／方向规则不变。
8. 隔离存储和实际玩家目录字节清单保护不等于所有存档迁移都验收。代价：原P06／Q02仍开放，不读取真实存档语义来补证。

无新增Minor；历史延期条目保留。全部单次审查未判断的边界由上述裁决承接，没有从总目标删除。

## 保管与后续

全16个terminal案独立核对：80对before／after清单、96个封存bin、7894份实际选定实体（7730源／164helper）均匹配，每案1860项源／900C#；当前源／9helper及canonical／普通Debug6bin匹配最终清单，零漂移。原build＋engine身份已结束，不触碰旧不可归属PID8760，不声称全系统无Godot。外层六阶段loop exit0只是完成排队，不能覆盖其中三个phase exit1。

实体边界：完整trace Explore、最终Main／Explore均缺少DescentClawGuidanceRuntimeTests的before／after实体，清单哈希存在不等该实体已保存；不事后回填或声称逐案全源实体备份。只有最终pre案保存了该选定实体，不把它混成其他案自己的备份。

最终清单SHA256：

```text
source-after C1602C21E4A841928E2035513FF3A882A7313DEF61F73B02671432E8C59ED58B
helpers-after 423BF8FA9AF90930EA5416A1D7E60AF0770FA28DDFC5DB1A1015A2A7D4A72BDB
fresh-binaries E08FB7F1098A344B7A0406F57793DCB4941C2ED73430CB9847F3EBA3CA01B754
canonical A66871CD6752249E348DED5603FEE852FA1EC387D3D3DE8F55551BEE135CE1D5
```

HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变。root另核对上述清单SHA／index及普通Explore落台收据；代码diff-check无空白错误，仅现有Git换行转换提示。

无新付费生成、自动化、游戏窗口、OS键鼠、原生崩溃采集、Git写入／发布、资料删除或真实存档语义读写。无新过程图稿／视频；汇报前手动容量Check于2026-10-29T19:30:34.5186340Z：7,427,750 bytes／86文件，Notify=false，未达新增20GiB档位。下一步优先定位前段恢复事件边界与Grip9前合法战斗衔接，继续完整双路线和正式视听制作，不把本轮局部结果当十一项完成。
