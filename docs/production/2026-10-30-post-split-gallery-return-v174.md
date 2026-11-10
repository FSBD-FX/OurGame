# v174 · 战后单跳返横廊与原Grip9通过

## 实际推进

v173完整Explore清完split组后落在绞盘Barrier，原Grip9要求完整Gallery支撑而正确拒绝。本轮在原真实地图补上一次普通地面右跳；未改玩家物理、地图、危险、瞄准或数值，也未触碰原StageGrip9方法与其90/12/30预算。

当前真实案例：`G:/knight/验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261030-155925-142-62240ca7e9fe42478c007fd53e4ee60e`。

| 事件 | physics | 原生事实 |
|---|---:|---|
| 实际Barrier起点 | 10506 | (4045.772,1483.9337)、完整Barrier向上接触，HP1/jump16 |
| 唯一D+Space按下 | 10507 | 旧Space已fresh释放，非重建driver |
| 真正接受普通跳 | 10509 | jump16→17，vy−590，仍HP1 |
| 首次完整Gallery落地 | 10543 | (4184.8315,1443.9338)，完整胶囊与向上接触 |
| 松键与自然刹停完成 | 10552 | (4201.4995,1443.9338)，速度0，fresh neutral；总46/90实际帧 |
| 原Grip9落地 | 10586 | Gallery再起跳17→18后完整Grip9；是另一次地面跳，不是空中二段跳 |
| 原Grip9最终站位 | 10595 | (4338.1694,1363.9335)，速度0、HP1、recovery0/0 |

返Gallery全程dash/J/K和NextCastId均未新增，原Player/driver、生命与恢复计数保持。最小脚底Y1420.8976记录于实际飞行。新增阶段最多90实际physics，包含neutral/起跳/落台/刹停，所有await前核剩余；未挪用原Grip9预算、未延长全路600秒。若已完整Gallery支撑则不额外起跳。其他起点不作为任意救援。

## 完整探索仍失败

后来已打开绞盘并下行至底部返程；physics11539在空中被常驻`precision-missile`、stable ID `canyon.v2.resident.steam-valves.05`、actor817989947903、attack2击中，玩家HP1→0、**energy1/盾off**，recovery1/0。其后回camp触发位置连续性守卫，而不是没有原因的瞬移。

当前底部Leg只驱动A/D+Space，无主动危险/防御消费者，且既有KeyboardSource要求!ShieldHeld。不能直接塞L而绕过原合同：L守向由WASD决定，与空中Steer可能冲突；标准Missile策略还不接受本resident场景与空中状态。现有日志只证实发射来源/命中，未包含此次弹体逐帧路径和预警起点，因此尚未证明有及时合法的格挡窗口。下一先观察原120/12预算内的全来源危险、授权弹体与守向/Steer兼容性；不改敌人强度/能量/机关切换顺序或凭空增加等待池。

v175更正上述前提的调用范围：KeyboardSource的!ShieldHeld仅在Neutral/NeutralRight/control调用，flight Safety并非全程禁L。因此无需删除旧合同便可在已接受起跳后申请纵向L，再在原settle前释放；新真实轨迹和联调结果见[下层导弹格挡v175](2026-10-30-bottom-missile-guard-v175.md)。保留原v174记录，不以此更正冒称其当次路线通过。

## 验证与仍开放的资源问题

| 案例 | build/native ms | 结论 |
|---|---:|---|
| Explore155544…53c6dcc1 | 10755 / 未启动 | 新partial缺Seamless命名空间CS0246；已修，非行为RED |
| Explore155925…62240ca7 | 39700 / 235109 | 返程与原Grip9局部通过，后导弹死亡，完整路线FAIL |
| pre-identities161055…956f7a51 | 40791 / 180477 | 原180000cap超时FAIL；不是诊断通过或正常退出 |
| 普通pre161911…b2864d8f | 46876 / 180322 | 原180000cap超时FAIL，另有4条Handle ERROR |
| models162458…d2016ee4 | 39077 / 581 | 当前机械爪models入口严格PASS，非全部游戏模型 |

原v173真实wrong-support失败为行为RED；新实际地图46/90和原Grip9通过为对应局部GREEN，不把整路失败改判。两次pre原cap不放宽、不续跑，也未再次筛绿；本轮没有同版post/Main资格通过声明，v173已有通过或失败不能借给1866源版本。

身份诊断的descent packed source-pre/source-post与instance-immediate/after-instantiate/before-add-child五项均非null、valid且同ID；basin读取点也valid，未保持跨await强引用或改缓存。诊断与普通对照的6bin完全同字节。前者日志无ERROR但超时，后者4条同组Handle且超时；不能以一对样本把0vs4认作getter因果，或认为历史14Handle/FATAL已根治。getter可能扰动wrapper，且两次没有完整phase结束，所以整个资源稳定性继续OPEN。

普通4条首业务栈仍是Supply同一AddChild×2、Descent同一AddChild×2；本次无Load/Instantiate首业务栈，不能把数量当损坏资源数。两案共同覆盖只截止`GRAPPLE_DAMAGE_INPUT_INVALIDATION_OK`；诊断的Descent第二J时钟126697ms、普通96575ms，实际覆盖速率不同。只能排除**诊断该次已读点出现前三项null或实例替换ID**，不能借诊断的健康源证明普通案资源健康；暂不增加getter/强引用或重跑。

## 审查与取舍

唯一fresh review C0/I0/M1，不二审。Minor延期：finally与失败消息先读取Player位置，若Player已失效可能覆盖原错并妨碍此helper松键；后续应使用captured driver释放和安全格式化。当前实际Player持续有效、父级仍有清键，不能称失效路径已被覆盖。

选择90帧小返程而不直接复用旧Task4ReachSupport，是因为后者可在300循环后再等3帧，不能冒称硬300实际期限。代价是新阶段可能保守拒绝其他起点；不得用额外跳/瞬移放宽救援。使用原真实地图RED而非挪动HP/关闭AI的夹具，成本是一次约4分钟完整运行，但确实覆盖原消费者。审查未代替实际轨迹、清理或源/bin保管；以上原生事实各自成立，不泛化为全部硬件、生命周期或旧dirty历史已通过。

## 状态

工程1866源；本轮仅新增测试partial、原callsite和canonical实体依赖。没有生产素材/画质增量、窗口/OS输入、付费生成、新原生崩溃采集、自动化、Git/删除或真实存档写入。全部原十一项、完整Main/Explore、pre资源/时间、正式美术/UI/连续动作/音频及硬件真人仍ACTIVE/OPEN，非交付。

保管：本轮5尝试（1buildFAIL无native/binpair，4native仅models PASS、Explore后续死亡FAIL和两pre超时FAIL）。24组清单对/2560选定源实体/54helper实体/24bin全部逐件SHA匹配，案内前后零差/未列0，9个实际owned build/native PID结束。当前1866源/6bin/models实际9项helper与最后案一致；不将helper实体副本数误当manifest项数。HEAD/index仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6 / 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。最终单次媒体检查CurrentBytes7427750/files86/原基线3508/Notify=false，无自动化或新媒体。
