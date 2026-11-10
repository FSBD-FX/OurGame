# v193：先完成真实近战K再收尾，主线仍未通过

## 事实

仅改两个既有测试CS：MainRouteTests和RangedWeaponPlacementRuntimeTests。没有修改生产敌人、伤害、输入、物理、地图、美术或爪角度排序。原十一项完整序章目标仍ACTIVE/OPEN；这次是自动试玩的应战顺序修正，不是新增生产技能。

v192实际Jet完成时，Rust的原警告已不足8ticks；随后可发的致命J抢先，机枪手死亡使原外来K分支再也不能取得它作为selected。未完成Rust K，就不会绑定/解锁后续Rust守盾策略。

本次新增一个额外否决：仅在实际post-Shot/setup路径、确切捕获目标HP1、真实Jet完成、尚无已验证的绑定Rust K回执时，暂不发致命J。原`else`仍独占K的全部资格和操作：精力≥24、无盾/无攻击、Idle、完整支撑/停稳、全源有限安全余量、Rust真实Telegraph>8ticks、原解析器实际选择及LOS、原一次K与守恒账本。没有新等待池、强制选敌或放宽条件。

外层J与实际松盾后边界使用同一否决；只有原完整ledger校验通过、receipt匹配原绑定Rust才解锁。不是拿命令数或任意别人的receipt当成功。守盾策略仍要求机枪手fatal/Defeated；若反过来等守盾receipt才发fatal，会形成循环，本次没有这样改。

## 测试结果

入口为既有canonical `verify-claw-knight-v001.ps1 -Phase <phase> -SourceRows 1872`。build原180000ms、policy/actions/models55000ms、Main600000ms、pre180000ms、post300000ms及原900循环均未改。root串行启动并在运行期间冻结源码/helpers。

| 案尾/阶段 | native ms / exit | 严格结果 |
|---|---|---|
| 6c41c3 / policy RED | 980 / 1 | 真实断言RED，旧调度返回false而预期true；不是编译失败 |
| 75327c / policy GREEN | 3846 / 0 | 9个字面条件＋原保存pair/ledger/完整policy trial通过；不声称实际K |
| d548d6 / Main诊断 | 131623 / 1 | 新局部顺序真实通过，后续原Hop退休检查仍FAIL |
| 820b4e / actions | 9111 / 0 | 当前输入专项PASS |
| 412e90 / models | 469 / 0 | 当前机械爪模型子集PASS，不等于全部模型 |
| dbe4aa / pre | 172734 / 0 | 原180s内严格PASS，不认证历史资源/性能根因修复 |
| 60e2e7 / post | 208665 / 0 | 原300s内严格PASS；原75案例、owner8和退出清理齐，不是旧180s或性能认证 |

本轮七次native共5PASS/2FAIL（含有意RED），最终actions/Main/models/pre/post同源4PASS/1FAIL；policy GREEN也属于最终冻结源码。每次都按实际日志和退出判定，没有以预期RED或局部K成功替换完整Main失败。后段0ERROR/FATAL，仅原4限定WARNING（observer1、非Node2D fallback1、状态拒绝2），不关闭历史资源根因。

## Main的实际因果链

本波机枪手=`16034555762166`，绑定Rust=`16034119551563`，K target=`16034220214968`。原setup/neutral/Jet仍先发生。

| physics | 确证事件 |
|---|---|
| 6559 | 新否决取得义务，保留HP1机枪手，等待原K机会 |
| 6622 | 原解析器实际选中Rust target、LOS成立；原K发出，cast13 |
| 6634 | 同Rust守恒receipt：edges13→14、sample1、app1、Latched/EffectPublished/Idle真、HP2→2 |
| 6635 | 完整ledger校验后解除否决，恢复原攻击intent |
| 6640 | 同机枪手真实致命J及Defeated；不是把K当伤害 |
| 6684 | 同绑定Rust真实Hitbox/attack5/epoch1/transition57零伤格挡，右L已采样，玩家HP2 |
| 6700 | 常驻远程源实际锁定cycle5 |
| 6727 | 迟到的左盾return marker，但contact_sequence=0；不能当有效同epoch配对 |
| 6743 | tracked projectile13真实Retired端点；其owner仍Active/attack5，accepted_pass=false |

末态Hop stage1、issued=false、Space边沿0、fault1。原断言要求未穿过玩家的退休弹体有真实Idle owner/publication，当前没有，因此严格拒绝。没有删除断言或把未发跳计成功。

本段玩家HP保持2、恢复0/0；本案accepted damage共3条，最后在physics6121。不能称“整段之后永远无背击”或“所有Hop/主线问题已修复”。同帧6743有ShieldResolved58/Guarded0，但request_source未知，Retired事件本身无cause参数；缺少独立关联，不能断言该弹一定被盾消耗。

## 推理、风险与下一步

已解决的局部问题是原Rust K被过早fatal饿死。下一断点是时序交集：右格挡后原扫描转给尚未锁向的远程警告，genericDefend=false使L松开；直到警告剩8ticks才重开左L。Rust的旧Active/receipt此前已结束，严格Hop正确拒绝了历史transition与零sequence的return。

单独立即转左，仍早于本次远程6700锁向；单独稍晚K，若继续等6727才转左，也不足形成交集。下一先做少量状态变化标量，核对真实K应用、Stunned/Chase/Telegraph/Active、receipt清空、远程锁向、左L与ready各bit，再在原预算内验证合法相位与同epoch交接。不能靠补精力、放宽epoch或接受任意Retired取绿；Space触发没有24精力条件，24只在后续落地验收。

只读算术候选不是成功证明：原6622剩`.56666666`，维持`>8/60`最多严格<26ticks，离散保守≤25；延后16–25ticks在其他时钟/几何不变时可能把下一Rust Active移向实际锁向时段。Rust `.25s`是生产stun值，不是新增等待池；Chase受距离/LOS影响，可能没有有限退出界。公开ranged Settings是作者资源，不能冒充Ready后的私有运行快照。当前版本没有实施这些后续候选。

## 审查与范围裁决

唯一fresh最终审查C0/I0/M0，没有修补轮/二审，无新增Minor；v192的两项日志Minor仍为历史延期。审查拒判项逐条保留：

- 原Hop退休判据/修法不属本次delta：保留FAIL，下一调查；代价是完整Main继续OPEN。
- 退休原因未独立证明：不把同帧格挡当因果；代价是还需源身份证据。
- 全十一项及美术/UI/音频/渲染/真人/硬件：headless局部不能认证，正式交付OPEN。
- 全仓库旧驾驶逻辑、平衡和真实存档：本次不改、不授予资格，保留必要广域回归。
- 审查者不独立运行actions/models/pre/post/Explore：root负责当前必要回归；当前完整Explore未重跑，不借旧v186续签。
- 审查只核限定源码和相关原始日志，不等于完整保管或资源稳定性：另做只读新案保管；安静的一轮不关闭历史Handle/Finalizer根因。

## 证据与授权

新案根：`G:/knight/验证证据/october13-stable-claw/`，完整目录：

- `ranged-mixed-policy-20261101-081857-057-6c41c3f5955248cf9b74b7ec25a3fe23`
- `ranged-mixed-policy-20261101-082016-494-75327c1b1a654f67b420ab30a39945e2`
- `thruster-route-main-diagnostic-20261101-082132-265-d548d613dfb54a9faa730b9a77770ffb`
- `actions-20261101-082604-670-820b4e5832a94e439f0e384870721046`
- `models-20261101-082730-570-412e9083a3594c56a63d4de821441602`
- `prologue-pre-20261101-082818-618-dbe4aa10788d4b85a7ccb1a452c9f337`
- `prologue-post-20261101-083309-904-60e2e722c59a44988d779d1e31284248`

GREEN之后冻结源清单SHA=`96464F2B2CF39152155B7F45ACEB1023BAD5A863E39B17D34E1B92B87A662C4C`；DLL=`8CF2F41442E55391C6F44FC3BA78ECDAB024698AFAE8C02635E100EF485D01DB`。Main=`2F6B69C1364928F6654BA5B177ACC62258F34E7112B989FD6964FC406B0822A7`、Placement=`78061969687F2E13DD229C0A446D0609ADC1D1F897F25CD2E25970B09A677B95`。RED不混为同源。

08:37:14 +08手动媒体检查7427750bytes/86files、基线3508、Notify=false，无新媒体。HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6、分支codex/task6-certified-final-20260902T0555、index SHA仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。

保管核账闭合：35对manifest、3434份选定source副本、78份helper副本、42个归档bin逐SHA匹配，只逐次核新案，不重哈前案。08:38:14 +08当前1872 source/6 bin/post10 helpers匹配最终post；08:38:52 +08本轮14个owned身份按创建时间/路径核查全部终结，14个不同PID均不存在。最终binary清单SHA=`9BB2103764EB0B907F65262B13936CFD47670D9E67A936F9C78158C5418F626D`；post10-helper清单SHA仍`48D515B7A1AB55E140806BE7E146CC24EF185CE77B91BB440974849706C9B28B`。七案build依次27232、29584、24680、28182、22573、24200、27351ms，均原180000cap内exit0/0warning/0error。

仅隔离headless构建与Godot内部键盘事件，无图形窗口、系统键鼠、真实存档写入、付费生成、误发视频分析、额外崩溃采集、自动化、Git写入/发布或删除。全部root运行句柄已终结。原完整目标实际仍ACTIVE；本轮记录和局部顺序证据不等于完整Main、显示或成品验收。
