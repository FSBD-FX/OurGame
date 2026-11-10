# v137续制 · 弹丸实体退场后的防御交接

KNIGHT，G:/knight/Knight；原十一项目标ACTIVE。这是v137同一次C/I修复pass的制作检查点，不是新成品版本。v138生产小地图兼容保留。本轮只有四测试CS+canonical变化；生产玩法数值、地图、美术及用户指定的敌人角度优先未改。

## 事实、推理、风险

此前trace只保留开头/结尾，不能据此解释中段。本轮记录实际命令边界的阶段/门控变化，首轮121条、末轮126条均无遗漏/观察故障；缓存生产EnergyFrameChanged真实能量、回复延迟和physics，不调用Step/请求/重置，不增加输入、等待或危险扫描。K不扣能或刷新回复延迟；持续盾和成功扣能才刷新默认.55s。

生产模型的FireRequested仅在Telegraph→Fire发一次；actor先创建并Launch实体，之后发布FrameChanged。同Fire后段不会再生第二颗。旧自动试玩却把Fire枚举视为必须守盾，即使这一发已真实Guarded/Retired/原生释放。默认Fire只有.12s，不是假定的.5s。

因此修正实体生命周期交接，不提高能量、不降低怪物伤害、不延长时限。仅Retired不够：Monitoring关闭是deferred，必须等确切原生对象消失。此源码风险不等于所有普通弹丸的同步伤害撤销已修复，后续要独立重现。

## 实现及防线

- 真实FireRequested后只观察该actor直属BellBolt{attackId}，绑定owner/attack/instance及该对象Retired事件。
- 仅同owner、同attack、已退场、后续physics、实体原生消失，才释放自身空Fire尾段。缺出生、活弹丸、错误owner/attack或旧physics不豁免。
- 外国危险源仍优先；空尾段中性等待，不提前K/J；70/58预留、900/1215时限不变。
- 主路线ACK与命令端共享同一实体证据，LiveBolt仍独立要求防御，不能一端正确松盾、另一端按Fire枚举误报。
- 原两个240px混敌几何、两个完整2J恢复、prearm及斜盾8控制保留。新增60px对照只验证空尾段至自然Recover：两次fresh ACK中一帧Fire、一帧Recover，明确记录；不强行等待外敌来攻击，也不冒称两帧都在短Fire内。

## 全部实际尝试

案例均在G:/knight/验证证据/october13-stable-claw，日期20261027；短ID对应完整原目录。

| 案ID | strict | native ms / 解释 |
| --- | --- | --- |
| focus042343-962aa77f | PASS | 28329，被动资源观察不退化旧专项 |
| Explore042454-612adc6e | FAIL | 148289，121变化完整、仍4K/0J |
| focus043224-5f438ddf | FAIL | 5896，远距离无空Fire窗口，覆盖不足 |
| focus043539-c9be803d | FAIL | 2732，真实Guarded/Retired/nativegone但旧rawL真，有效RED |
| focus043830-2d94973a | FAIL | 3749，近距反击早于外敌需求，新增组合夹具覆盖不足 |
| focus044104-eaf1b79c | FAIL | 2731，复合ACK失败、信息不足 |
| focus044357-af41a8e2 | FAIL | 2759，诊断明确commands2/fresh1，其他HP5/无KJ/无外敌/原生消失/松盾均满足 |
| focus044536-551ae8e2 | PASS | 29501，按真实短Fire修正ACK观察边界，不加时钟 |
| Explore044653-260f9e7e | FAIL | 133720，旧主路线ACK拒绝合法松盾，有效ACK RED |
| focus045108-29c200c8 | PASS | 29616，共享谓词及活弹丸/3身份时钟负控通过 |
| Explore045322-06a95193 | FAIL | 142467，已过旧ACK误报，原900仍SentryHP2/玩家HP2 |
| models045858-83729ba6 | PASS | 402，当前普通模型回归 |

本组12native严格4PASS/8FAIL，12build全0且quiet，无timeout/guards/ExpectedFailure。累计v137历史19+本组12=31native/10严格PASS/21FAIL，0buildFAIL；本组审计不补称此前19案已最终审计。没有筛绿、重试静默错误或豁免失败。

关键原始证据：[近距有效RED](../../../验证证据/october13-stable-claw/ranged-steam-sentry-recovery-20261027-043539-211-c9be803dad81485aa429e21530647269/engine.stderr.log)、[最终专项GREEN](../../../验证证据/october13-stable-claw/ranged-steam-sentry-recovery-20261027-045108-375-29c200c8256546b48a6f1dd321dc5b4a/engine.stdout.log)、[旧ACK有效RED](../../../验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261027-044653-977-260f9e7ee7e14bdc89a1e740fcdb0b7f/engine.stderr.log)、[最终探索仍FAIL](../../../验证证据/october13-stable-claw/thruster-route-exploration-diagnostic-20261027-045322-614-06a9519362b54467960c4e4dc03814c0/engine.stderr.log)。

最终专项近距commands2/ACK2/Fire样本1/Recover ACK、HP5、只一发真实Guarded/Retired/nativegone、无foreign或K/J；活弹丸负例及wrongowner/wrongattack/旧physics三负例通过。旧两个混敌J1/伤害1/六margin/HP5保持，两个完整恢复仍2J/3K并有退休后21实际样本。

最终探索126/160个变化、total126/omitted0/faults0。terminal K4/4为命令/应用，J0/0/0为命令/接受/完成。能量约45.7/100，不是耗尽，也不是击败敌人。Shell提取K须匹配带空格的` K=`，裸不区分大小写的`K=`会误取shot_attack，不能混淆。

## 审查、保管与后续

沿用唯一v137审查，本次ROOT-I4实际RED→GREEN属于同一C/I pass，无重审；旧48行trace Minor未修，原20项延期仍OPEN。当前完整pre/post/Main资格尚未重收齐，不能借v138旧源码批次认证；历史pre Handle错误/Main死亡复位未关闭。

[12案CHECKPOINT保管](../../.superpowers/sdd/2026-10-23-current-main-and-recovery-consumers/v137-resource-checkpoint.md)已ENDED，UTC21:02:05.6522455才解冻写文档。60/60清单对、6234实体SHA、72/72sealed binaries一致，新增缺失/额外/未映射/hash错0。当前1852/CS892、Debug6与finalmodels一致，HEAD/branch/index旧pin不变。旧14helper/LeftPNG缺口未重审。

没有新美术/动作/图稿/录像。一次手动媒体Check UTC2026-10-26T21:03:18.2709037Z：current1431609/base3508/growth1428101字节、16文件、Notifyfalse，未重置/建自动化。无付费、前台/OS输入、Git写入、删除、真实存档语义读取或UE修改。已非阻塞询问下轮约30秒画面显示；未获明确回复不视为授权。

下一步先独立重现普通非resident弹丸Retire→deferred完成间的同步伤害撤销风险，再处理混战资源与威胁交接，不猜另一套操作或加时筛绿。原完整十一项、正式美术/UI/动作/音频/硬件真人与发行继续ACTIVE/NOTSEEN，不标完成。
