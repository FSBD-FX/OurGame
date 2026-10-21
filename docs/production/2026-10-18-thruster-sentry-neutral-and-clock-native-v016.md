# 哨兵路线中性制动与原时限 · 原生 v016

2026-10-18，Asia/Shanghai。完整用户1–11工具goal ACTIVE，未缩减、未完成。Task1–5不重做；Task6音频退出／视听与Task7其余消费者及正常双路线继续OPEN。本报告只证明实际保存Player的受控测试路线，不是正式关卡通关、美术或真人验收。

## 事实、推理、风险

事实：实际标准模块的地面松键摩擦为1800，旧哨兵主／备用路线仍以2800连续减速估计停距。在真实bolt退休后的gap83.499756、vx−260处，旧含两tick余量的估计20.738095要求继续持A；独立逐tick求和含相同余量25.333333要求松A。原生RED记录最终物理A仍按住、fresh RawFrame／Current轴都是−1、真实Motor下一步仍−260，而不是应有的−230。

推理：这是测试策略同实际运动失配，不能据此调整敌人HP、护盾消耗、眩晕时长或游戏推进力。预测应读取已经Ready的不可变Motion／Module；neutral摩擦也不能替换为反向键制动。

风险：本轮未改生产Player或敌人，不代表玩家感受到新增技能或敌人变弱。受控fixture保HP5而非正常路线HP1状态；GeneratedMain只接了相同helper及原900循环剩余数，当前版本尚未正常游玩证明。旧legacy高层碰撞、历史资源错误及音频2 ObjectDB没有被本轮绿色关闭。

## 保留的改动

- `SteamSentryRecoveryDriver.TryDrive`接收调用者原时钟剩余帧。ApproachAndBrake用实际Motion／Module、实际Hz和剩余帧调用`FreeMotionForecast.Neutral`，保原60目标／64范围／8最小间隔、≤2实际停稳和两tick余量；不能预测停稳或存在燃烧时明确失败，不制造运动或新等待。
- 既有fallback同样迁移；专门增加同出生、同敌人、同真实退休的fallback接近case，随后仍由helper完成K／J和退休尾段。不是把未执行的fallback混记为已测。
- 新priority100观察器复用既有类型，仅缓存完成后的physics／input sequence／position／velocity／实际完整支撑／原始与采样轴。原循环中检查前一完成样本→发键→下一个完成Motor步；没有新增ProcessFrame等待、OS键、补键或回调侧断言／移动。
- 两条路线均必须实际出现至少一个“旧2800还持A，但真实1800应松A”的区分样本，并检查松键、真实减速与本步位移，不仅以最后击杀为验收。
- 全局clock保原入树前anchor；同步AddChild完成Ready后、首次await前冻结实际Motion／Module／configuration epoch。global与首次真实退休后first-K的推导使用实际九tick停时。真实原始60Hz cap来自10月16日案：global1215／first-K73。当前新推导分别1219／76，实际取更严格min＝1215／73；日志分别输出derived／legacy_cap／effective／clipped，不能把裁剪结果假称新derived。此cap仅资格化60Hz，不冒称30／120Hz。
- 每个Tick检查同一Motion对象、同一Module值及configuration epoch；fixture独立固定标准模块260／1800的九tick、1000/60停距和，保原7次J自然花费84能量、原敌人／能量／K／J窗口。
- owner补齐实际调用依赖的逐案源码副本，不反写旧receipt；全source库存仍1695。

独立算术更正：230、200、170、140、110、80、50、20、0的合计是1000，停距为16.6666667px，不是前期只读建议误算的17.5。本轮oracle使用独立逐项求和，RED／GREEN都记录25.3333333含余量阈值，没有采用错误字面值。

## 真实RED与首次GREEN

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。以下均fresh Rebuild0warning／0error、source1695、guards=[]；所有五log已完整读取，四类inventory前后相等。没有新增GC／cache／drain／时限或日志豁免；原runner已有退出drain保持原样，不能把该历史策略称本轮修复。

| case后缀（同phase ranged-steam-sentry-recovery） | engine／ms | 证据 |
| --- | --- | --- |
| 20261018-231615-910-6c29e7b68f724d76a32d4d33b1d60041 | 1／6305 | helper真实RED；physics330，rawA=true、两轴−1、Motor −260而独立应−230 |
| 20261018-231904-081-a6a7d491e8824ad394bc08787bfd044a | 0／9774 | 最小helper修正GREEN；两个区分样本及完整反击／退休尾段通过 |
| 20261018-232143-825-1b49b89cbd4a43f0a752268b7ba7c1cd | 1／14288 | helper已过，独立fallback真实RED；physics808同gap／speed仍持A、Motor −260 |
| 20261018-232331-786-d4a912874d824f01b58c319394fa5327 | 0／17867 | fallback最小修正GREEN；prearm＋两条完整受控路线均过，尚用旧clock公式 |

两RED只有目标断言，无额外原生error／warning／RID／ObjectDB诊断；不是编译失败或被命名豁免的通过。各原件完整保留。

## 最终同冻结两门

batch36872终态exit0；全部owned消费者终态后才维护本报告、README与总待办。

共同source1695／SHA256 `D6F20AEF6ECB0D51D5D9D637B4B5A44F8D5BF198AB28FECF8D009C61C9D42B0E`。两个fresh Rebuild均0warning／0error；逐案source／helpers／真实用户文件inventory／binary前后哈希全等，guards=[]，五log已完整读取。两个phase的helper清单不同，不能要求其跨phase哈希相同；各自before／after相同。

| phase／case后缀 | engine／ms | 结果 |
| --- | --- | --- |
| ranged-steam-sentry-recovery／20261018-232720-052-23a289ee1a2e44e99f75bf62cbc57301 | 0／17728 | GREEN：未武装负控＋主／备用两条完整真实反击；新clock推导与原cap分别记录 |
| thruster-free-forecast／20261018-232816-694-697796b4ff1e432b9824aa394e6b6cde | 0／672 | GREEN：独立几何和／地面离散和／反刹过零、peak与net、零阻力和旧2800负控继续保留 |

最终binary inventory SHA256 `CE3FB1155AACFAB7C4DBEE842B44F9EF3BA64DFC843C6A4B3E0CF4DADC3A9C5A`；真实用户inventory `BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`。正式Player保存字节仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。

## 最终实际回执

- prearm：真实两次J击杀→立即QueueFree，helper始终未武装、0次夺权，20个gone-enemy观察完成；不是虚构bolt退休。
- 主路线：entry60／deadline1275；退休physics291；首次预算derived76、cap73。两个区分样本physics330／331，原生vx−260→−230→−200、raw／sampled轴均0，命令A／D均松开。
- 备用路线：entry538／deadline1753；退休physics769；相同推导／cap。区分样本physics808／809，实际减速同上；45个真实fallback接近tick，非未执行分支。
- 两条路线分别于physics535／1013完成。均只1颗真实bolt，typed warning→发射→正向Guarded、damage0／HP5→实际退休后才开始接近；3次新物理K／3次精确adapter application、2次新物理J／2次精确Hurtbox连接，敌人HP2→1→0／Defeated1。
- 每次J保完整动画时长及6个fresh安全样本；最后一次J期间真实敌人QueueFree，helper仍完成21个普通tick的退休尾段，21个gone-enemy样本，最终不再持J／K／L。玩家HP5、无受伤／死亡／回血／跳跃／短喷／恢复，未延长敌人眩晕或能量。

## 当前未完成与下一步

Sentry两数值接缝的专项迁移现在有真实证据；Task7全量尚未完成。v015 legacy ascent_grapple_three `.2`释放后撞回返天花板仍OPEN，本轮没有再跑pre／post，不给其历史GREEN或FAIL伪造新时间。continuous／SteamDiverter／missile／GeneratedMain其他站位、Boss和各回返接缝还需逐个验证；正常launcher1675旧绑定须按实际1695迁移后才能合法运行。

只读进一步调查已定位SteamDiverter的staging／同一harmless-window旧公式及原5400总physics、120walk／20stop等上限，尚未改源码或原生复现。Task6音频最后自有WAV／Playback的额外native引用已有历史ID证据，最新案仅计数，下一最小独立叶诊断仍待实施；不能把两案身份混报同版确证。

完整十区地图、正式新增50（当前0/50；24canonical未实现；正式127／候选132未发布）、机关、最终原创美术／动作／UI／声音／范围明确演出、历史五失败关闭、正常双路／存档／真人／显示60FPS、全七任务末review与Windows ZIP／授权GitHub交付都继续保留。十万付费人数不作为可保证结果。

本轮仅测试路线／源码保管与报告，没有新视觉或音频文件、展示媒体、付费生成、下载、OS输入／前台抢占、Git写入、发布或自动化恢复。展示媒体以后仍先按`E:/OurGame/Knight/过程图稿与视频`规范归档，旧稿旧包不覆盖；真实用户存档仅原保护inventory，不读取语义。
