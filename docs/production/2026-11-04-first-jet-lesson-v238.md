# v238 · 首次短喷教学的真实成功接线

工程仍为 `G:/knight/Knight`。本轮推进原十一项 G04/G05 的一个生产缺口，不改变动力、伤害、人数、地图、单跳、机械爪优先级或存档协议。**原十一项、G04整项、R-I/R-C/R-P、正式视听/动作和正常速度真人体验仍未完成。**

## 事实与实现

- 入口接地、非攻/盾/爪时可出现独立 ECHO 短喷/反向制动提示；固定既有阅读区、双语及实时键位。初始卡不增加世界目标。
- 成功来自当前 Motor 有界 journal 的真实 Started，核对 Motor/Player 身份、lifecycle/configuration 和游标；不是按下键、IsDashing 或重置计数差。
- 当前正式会话第一次真实成功后不再重复；指导关闭时仍观察成功。普通重置/暂停保留已使用状态；新会话/新owner建立最新基线，旧记录不补学。没有新增运动回调或第二套历史。
- 必要首次 K 教学和即时危险仍优先。爪教学结束后的短喷补充保留原 EntranceExit 标记及右行下降入口说明；不清空必要路线、不增加秘密目标。
- 没有新图片/声音生成、付费、窗口/系统键鼠、自动化、外部发布、真实存档操作或资料删除。新文案复用现有 ECHO 声音链。

## RED、缺陷与修补

1. [113805真实entry RED](../../../验证证据/october13-stable-claw/thruster-teaching-entry-20261104-113805-698-f6da4efb1ba04372b88595c3788f94a8/engine.stderr.log)：原支撑布置及自然落地成功，但自动卡没有短喷标题，仍是攻击长说明；退出1/6140ms。
2. [114334模型 RED](../../../验证证据/october13-stable-claw/thruster-teaching-20261104-114334-228-041583b0bfcc482a967dcb41a9a9b5d1/engine.stderr.log)：第一机会仍未选择独立卡，退出1/742ms。
3. 实施中另保留114622编译失败（缺namespace，已补）及114800夹具错误：V改键后却从静态默认glyph表读取SHIFT作期望；改为独立断言已设置的V前缀和旧SHIFT不残留，没有改变生产键位读取。
4. 初版实际通过首次教学，但旧generic夹具及首战消费者失败。generic场景现在先以真实左向Shift、原地面/墙自然停稳完成前提，再执行原目标/glyph/双语/恢复断言；首战仍严查普通HUD模型不变，不把任意上下文当成功。
5. 更完整首战暴露了真实生产冲突：可选短喷会盖掉首次K教学，并清空爪结束后的前进目标。[121607新增模型RED](../../../验证证据/october13-stable-claw/thruster-teaching-20261104-121607-296-3a792c240e9b42eeb19634abb236b6e3/engine.stderr.log)退出1/553ms。一次修补后，必要K优先，后续短喷保留原路线；原真实首战目标渲染断言保留并通过。

## 当前冻结轮实际结果

以下为本轮最终修补后的案例；旧轮通过不与此表拼接。所有正常/诊断路线都清空 `KNIGHT_*_PROBE`，不借实验开关代签默认路线。

| 门 | 案例 | 结果 |
| --- | --- | --- |
| 原首战完整专项 | first-combat-20261104-121856-900-631a8389add34e79a13dbafda4bb042f | 严格PASS，22373ms |
| 首次短喷模型/UI/真实按键生命周期 | thruster-teaching-20261104-122021-874-85106f444c9041ec9c54cb86ec0f5bb2 | 严格PASS，10946ms |
| 原mounted引导/实际恢复/改键双语 | thruster-teaching-ui-20261104-122140-377-c48cbd2aef794120a02f615758c0ec70 | 严格PASS，8016ms |
| 原短喷边界套件 | thruster-player-20261104-122249-015-34d1755fac2240a69c284c211d379e6a | 严格PASS，25115ms |
| 完整前段 | prologue-pre-20261104-122418-516-faf5a3faaf8543cc830181fbd21d8752 | 原180000ms门超时，wait 180299ms，严格FAIL |
| 完整后段 | prologue-post-20261104-122815-327-db6c7771f4094acfbb628627c1963ae4 | 严格PASS，245997ms；4条预期负测试警告，不是stderr空 |
| 原models门 | models-20261104-123321-034-ca01e0819fd941c6b095755476ee24ce | 严格PASS，641ms |
| 默认Main诊断路线 | thruster-route-main-diagnostic-20261104-123403-105-2055d3596a87402e8a5e54fb139c21ed | 严格FAIL，165227ms，未超时；配重战899预算余敌HP1 |
| 默认Explore诊断路线 | thruster-route-exploration-diagnostic-20261104-123733-349-6270d82bad114c0faee7840ce709cf7a | 严格PASS，362808ms；Boss及实体支撑出口，恢复0/0 |

最终九案7严格PASS/2FAIL，父进程如实退出1，全部终态后才更新本表。各案原始文件都在 `G:/knight/验证证据/october13-stable-claw/` 对应案例目录。九案自身前后源码清单完全一致，跨案也同一清单SHA256 `4B1C69B814E5613633E8EBE63CB34D6043DFCE86FD6E6A596A063D8E2A791934`；6项fresh二进制清单的SHA256均为 `AC12B4AD9C992E1AF94A31B8B7F5A11A2D80E12959635EC9008998B2FC4FD264`。这是清单文件的哈希，不冒充单一程序集或游戏发行包的哈希。源资源仍1888行，未通过变更原验收器求绿。

后段4警告均来自原明确负测试：throwing ClawFrameChanged观察器、非法VisualScene根、两次Completed→Recovering拒绝；无未解释ERROR。默认Main实际7次J命中/3爪、玩家HP2、恢复0/0，原899结束Rust仍HP1；它证明默认自动驾驶未完成，不等于人手游戏绝对不可通关，也不通过降低HP/储备或加时关闭。

Explore原始 `storage/task10-route-result.json` 记录同一个玩家从标题开始、23次路线节点访问/11种挑战，Boss击败/安全出口均true、恢复请求/完成0/0。出口时HP3→3、完整实体地板支撑、Completed计数和事件均1；保持向右60帧后位置/完成计数不变，完成存档字节SHA一致。`operationCoverageComplete=false`、`humanValidated=false` 和 `fullGoalAccepted=false` 继续保留：不是所有敌群、所有操作、正常显示、性能或用户体验已通过。

前段超时停在 DescentClawGuidance。相较本轮较早115449，前两盆地案例从37258/73049ms变为44613/104252ms，物理帧增量也由406/1770变为501/2768，而process观察数接近；这是耗时位置证据，不足以推断新提示是原因，也不能称资源或性能已经修复。原预算没有提高，历史Handle/终结器故障保持开放。

只读独立核对进一步确认：Camera和CurrentLaborer helper均先等PhysicsFrame再等ProcessFrame，不是严格一步物理；blocked两案均完成原300次组合等待，不能把normal最长连续支撑36→29错当总循环数。两案及当前的Camera/CurrentLaborer/Descent/KeyboardAction/RunPrologue/Teardown/生产GuidanceController源码相同，但全清单有本地化、GuidanceModel和三个教学测试路径差异，不是完整同源码开/关A/B。第二案161817ms才进入Descent，旧案该项16807ms，两案均过第二J窗口，故不能把超时称作Descent卡死。下一最窄入口为原 `-Phase ranged-camera-ground`，不增加await、改物理节奏、删人口或提高超时；先补被动三时钟定位加载/准备/hold/清理。

当前相关局部入口：

```powershell
& 'G:/knight/Knight/.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1' -Phase thruster-teaching -SourceRows 1888
```

完整命令及所有中间失败见[本次执行账](../../.superpowers/sdd/2026-11-04-first-jet-lesson/progress.md)与各案原始文件；不是删除旧日志后的“全绿”。

## 实际覆盖与审查边界

真实保存场景、原碰撞及内部按键验证初始两语卡、V改键、L持盾时raw V被拒、真实新Started后结束卡、普通reset/暂停保留、新局同Motor旧journal不重放，以及关闭指导期间成功后不重复。x1100原BridgeFarBank支撑布置只是隔离夹具，**不是自然走通路线、正常像素或玩家认可的证据**。

一次独立只读审查C0/I0/M0，未执行原生门、未认证后续测试迁移或自然路线/视听；随后根通过更宽运行发现上述引路冲突并按Important修补，不用审查零问题掩盖运行失败。测试迁移和该轮修补为根自审/实际RED→GREEN，未做第二轮独立审读。

尚缺新增runtime覆盖：未读Started后death/reset、替换Player/Motor、64条journal断档、V改键后旧SHIFT、实际换区保留；生产拒绝与重基逻辑已静态核对，但不声称这些反例已跑。初始两行卡有布局合同，后续三行补充保留真实路线的证明也不等于大字号/正常速度/像素阅读已验收。

## 执行取舍及代价

- 保持用户指定唯一G工程及现有codex分支，不新建worktree/副本；依靠前后清单分离本轮变更，隔离性弱于独立工作区。
- 复用有界Started journal，不增加Motor同步事件；“已使用”是成功启动，不代表已掌握反刹或成功完成位移。
- 暂停前的真实Accepted仍有效，暂停中未被接受的raw按键不算；不会为了UI尚未刷新而抹去真实成功。
- Space+Shift原本允许组合，拒绝反例采用原L持盾输入；并非新增覆盖全部拒绝种类，原player套件保留。
- 必要K和原路线高于可选提示；后续卡保留既有目标、正文三行，阅读与美感仍须实际审阅。
- 原generic和首战消费者迁移不改生产难度或删原断言；根自审的独立性不足已明确保留，原生失败不得凭人工解释改签PASS。

## 下一步与完整目标

当前九案已全部终态，后续从默认Main最后一击的真实资源策略、原盆地/前段耗时证据及原十一项其他生产内容继续，而不是反复只筛局部绿票。继续原十一项，不把成功标准缩为本卡或仅构建通过。用户反馈的惯性、角色专属动作、连续优雅和物理手感仍是正式返工欠账；本轮没有生产力学/动作美术增量，正常速度视听、目标硬件及用户实际试玩未验收。
