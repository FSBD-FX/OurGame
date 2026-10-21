# 短喷观察、反向制动与首段教学 v010

2026-10-18。完整1–11目标ACTIVE；短喷计划Task6进行中，Task7未完成。本报告不是整关、最终视听或真人验收。

## 事实：实际接入及边界

- 保存Player/Motor公开只读短喷帧与有序转换记录；真实接受才产生Owner/Lifecycle/ConfigurationEpoch/BurnId，真实结束原因在清除身份前记录。迟到渲染仍能消费开始和结束，不根据IsDashing猜原因。
- Burning、Coasting与Braking分开。余速观察必须已有实际喷力，最长0.45秒；新普通跳、实际附爪、完整速度替换、落地、可信静态轴阻挡、制动过零、reset/death/退树等提前退出。观察不改速度、次数或有效上升保护。
- 反向制动过零事实由既有同一动力学积分返回，未增加第二套运动积分。接触判断对斜面、混合面、移动支撑等仍保守Unknown。
- 同Player退树产生OwnerRetired而非伪造死亡；重新入树恢复Health监听，真实死亡与reset保留不同结束原因。
- 中英controls、暂停页和正常出生的首条LearnAttackMode教学增加“Shift短喷／A-D反向制动”，使用真实字形，保留战斗危险提示优先级。没有燃料条、新槽或背包使用规则。
- 音乐导演仅清理自己创建的Tween/Tweener包装对象；不Dispose借用Stream、PackedScene或缓存Resource。自然淡入、淡出callback、播放中／已完成／重复Shutdown已测。这不是短喷专属音效消费者或最终音色。

正式Player仍为v009真实builder标准原字节，SHA256 `2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。未重写正式地形、重力、普通跳或玩家存档。

## 原生证据：同冻结版本13门

根目录：`E:/OurGame/Knight/.evidence/october13-stable-claw`。每行case均保留fresh build、三运行日志、native result和source/helper/user/binary前后库存；source1692，四对库存全等、guards=[]。构建0错误／0警告。共同source inventory SHA256 `6B0494CB896A7ADCD3D22A733C720E30C312B71065B684DC7EC88CD273A3F714`。

| Phase | Case | Engine exit / ms |
| --- | --- | --- |
| typed纯观察 | thruster-presentation-model-20261018-184825-506-b5d4e722321446b896b39f730a04eaf3 | 0 / 534 |
| 制动跨零 | thruster-brake-crossing-20261018-184901-878-f163fef9ddd24fd697b53859d3e6549e | 0 / 544 |
| 保存Player观察11场景 | thruster-presentation-player-20261018-184933-521-9c2320382ad349c0b3197c41d2a032ee | 0 / 3796 |
| 中英UI／实际初始教学 | thruster-teaching-20261018-185006-611-db3ab8c1a98e426c869cedac5eb1ec44 | 0 / 1876 |
| 音乐自有包装对象 | thruster-audio-owner-20261018-185040-416-63ad5c08673c4644aa772c004c696ea3 | 0 / 1393 |
| 原Player33＋scheduled | thruster-player-20261018-185113-638-afce35e228564431b6da8cb7729e0f51 | 0 / 22888 |
| pulse | thruster-pulse-20261018-185210-910-dc7606b235034100a7082c54430a77ed | 0 / 565 |
| keyboard | keyboard-20261018-185242-103-2be5a984a041446a98c9c824be542c6b | 0 / 9710 |
| actions | actions-20261018-185322-980-c2c195cd973d463f81008e529eba3630 | 0 / 7123 |
| 着陆重挂 | thruster-impact-reentry-20261018-185359-995-06a2ae65771248c8b37db6a8436574e5 | 0 / 1396 |
| 正式模块读取 | thruster-formal-module-20261018-185431-557-8b5b4b4b0962441fb07eb4a083c7716f | 0 / 1048 |
| **广域pre FAIL** | prologue-pre-20261018-185504-678-c8c1e4f1fd4c4cdb8420a717151cfa18 | **−1073741795 / 31156** |
| **广域post FAIL** | prologue-post-20261018-185606-223-0ced4fe6831b4f14995d235c86fb616d | **1 / 8249** |

结论严格为11专项GREEN／2广域FAIL。pre出现析构链`gchandle.is_released()` FATAL，并在Descent入树时五个sector definition均null；不能将本次仅写成旧Upper/profile失败。post仍为BellChain真实落点(540.0704,1196.0139)及KillZone未解析ILevelRecovery。

早期真实RED包括缺typed模型181233-de874883、缺跨零事实181653-fde43660、缺Motor观察182720-e3cdc、首段教学183605-3fa6c698和音频自建wrapper未释放184537-66818866。编译／Ready fixture前提失败没有冒充行为RED；183731-a3eb00a6虽内容断言过但退出2 ObjectDB，严格FAIL原件保留。安静的verbose诊断不替代普通验收。

## 推理：资源对照不等于修复

仅Audio自有清理开／关的精确字节：ON `FDD6486D51EDD0715326D3C57C7463EA4C3FFAEA7C386C949ED4134ABFA0FA33`；OFF `AF7DC1B0FC56B85479421D11E76F6DE4ECF63F72664FCEA710A0362435D67C2B`。

1. OFF普通pre185759-321fe09b，1／89852ms：未捕获FATAL/null，继续到旧Upper lower_to_mid_swing，profile两警告及离树GetPath四错误保留。
2. OFF scalar诊断190328-ab45138e，1／88593ms：12同步标量快照均有效，Descent两个快照与原Basin五资源ID相同，仍旧Upper失败。
3. ON恢复精确候选后scalar诊断190706-c0785d86，1／89777ms：12快照均有效，Descent两个快照一致，但与早期Basin原资源为不同五ID；仍旧Upper/profile/离树GetPath失败。

诊断仅启用`--trace-sector-definitions`，不额外Load、不换cache模式、不GC、不Dispose借用资源、不跨await持有新增资源、不增加帧或时限。单独case label与`sectorIdentityDiagnostic=true`标明非正常验收；三sink仍严格。资源ID变化说明实例化／资源生命周期值得继续观察，不能推出Audio造成或排除了FATAL，更不能推出故障已修复。10/14–17已有相同析构FATAL历史，本次五null组合尚未找到旧原件。

当前ON和scalar追踪代码共同库存SHA256 `D6F2CEB7AA28C6250504CEE2922BE09D50794642145157546686BA57A005CE64`；普通复验三门GREEN，四对库存全等、guards=[]、三sink无诊断：

| Case | Engine exit / ms |
| --- | --- |
| thruster-presentation-player-20261018-190914-600-ccb8ef0129034e5aa914f3fd582c69c3 | 0 / 4145 |
| thruster-teaching-20261018-190959-928-e612e2dde47943579a88238118780906 | 0 / 2382 |
| thruster-audio-owner-20261018-191046-716-8d1c0a10e72c4b4e89ad5b1b4955ea0d | 0 / 1388 |

不同冻结版本不混报为当前全源14门通过。以上所有消费者均终态后才记录。

## 风险与下一步

- Task6尚缺真正接入单一renderer的倾身／回稳／制动、持续世界喷焰、精确身份专属声音和低闪／暂停／退场清理；当前不能称已看见或听见完整短喷。
- Task7旧预测器、正常Main／Explore同版实体出口、旧档／保存死亡恢复仍待；五历史严格FAIL、七Handle观察和这次FATAL/null均OPEN，不做日志、GC、cache、时限或drain豁免。
- 全新常驻正式0／50、24canonical、正式127／候选132地图、机关／正式美术／最终UI／视听、真人／实际显示60FPS、ZIP与GitHub版本归档仍未完成。专项通过不提升为整关交付。
- 本轮无新展示媒体、图片／声音生成、付费、Git写／发布、自动化或OS键鼠；真实userdata仅库存哈希。后续展示仍先归档原件／展示副本／索引。

执行计划及持续状态：`docs/superpowers/plans/2026-10-18-thruster-player-integration.md`、`.superpowers/sdd/2026-10-18-thruster-player-integration/progress.md`。
