# 完整路线后段与惯性站位适配 · 原生 v018

2026-10-19，Asia/Shanghai。完整用户1–11目标 ACTIVE，未缩减、未完成。本轮主要修改自动试玩控制与诊断，不是新增玩家技能、扩充正式敌群或完成最终美术。Task1–5的专项状态保留；Task6音频退出／最终视听、Task7全量迁移与正常双路线仍OPEN。

## 事实、推理、风险

事实：从真实标题、原出生和同一保存Player进行完整headless Main／Explore诊断，实际AI、机关、碰撞、伤害、复活监测均开启。最终同冻结版本Main已通过九段上行、三类主路远程实例和后续Split遭遇；Explore已无伤通过Frozen下组、真实摔伤返回Camp并走完盆地升降／横廊。两路仍严格FAIL，均未到Boss或完整成功artifact。

推理：新惯性改变了旧控制脚本的停靠、余速与目标预检。应迁移控制消费者，不能据这些失败反改获批摩擦／逆刹、扩大侦测范围、削弱伤害、移平台或强制指定机械爪目标。本轮发现的Archive离台死锁是v017新预测引入的控制回归，已明确归因，不称旧游戏地图bug。

风险：headless普通时序不证明正常窗口绘制、真人手感或60FPS；各局部通过也不等于整关完成。新增正式常驻仍0/50、24canonical、正式127／候选132与全部最终资源／UI／旧档／Windows ZIP／GitHub交付仍待完成。E盘最终检查约7.6GiB可用；已询问是否只把未来新增验证证据放G盘，尚未迁移、移动或删除任何旧文件。

## 本轮控制迁移与实际边界

1. Main深井前缀首次直接GREEN，推翻“也必须加入反刹”的假设。该前缀早于新增数字收据，只证明原4–6秒断言执行通过；4.633秒数字来自后续完整Main，不回填旧案。
2. ArchiveFloor→FrozenLeft：原120离台预算内，远离source边缘时允许正常接近，靠近离台后恢复预测上界；release后仍全neutral、真实完整落台／停稳。原下界、HP、跳／喷／恢复与落地预算不变。
3. Gear左返：外层终点2102、子循环旧终点2110造成零physics反复举盾过热。统一原2102终点，只在已实际离开lane并得到原安全台支撑后选择neutral尾部，不再无效重举盾。实际136帧、8脉冲8格挡、HP保持、安全台2097.32；原480及9／20／30／210预算保留。
4. Frozen Rust：原L被“已停稳可出J”资格禁止，首4tick无盾；解耦L与J后首六真实A＋Left-L已防住。随后真实站位摇摆仍FAIL，再迁移为实际Motion／module的离散neutral终点控制。原±1.5站位、55..64打击带、速度≤2、sampled neutral／向内朝向与900循环不变。最终Rust真实击败且HP2保持。
5. Frozen Moth：旧180循环同时符合Space与机会K，Dive中松L进入helper，外层防御暂不重算而受击。新控制在Space决策前检查安全cast窗，Dive／晚Telegraph继续正常防御。增加安全K的原prefix2无Space、原45内同adapter+1／同actor ThreadStunned／HP保持断言，但最终Explore实际thread_casts=0，不能称该条件分支已实测。实际5started／5connected、HP2→2；Camp随后68/160、716.03955高度、真实1伤HP2→1、0恢复通过。
6. Basin控制站：旧“距目标<12直接返回”仍带余速，却四帧后要求误差<8。仅原Walk(2492,80)启用实际neutral终点停靠，保支撑／HP／同Player及原终检，boarding／return两处Walk不改。Main12/80停2490.5842/v0；Explore13/80停2492.0173/约−0.333，后续原升降与横廊通过。Basin测试文件补入四个完整route phase的显式内容副本，不反写旧案。
7. Lift诱敌：旧逆刹停4767.6157，距Moth483.44775，实际超出原480。仅当实际neutral终点同时满足原侦测圈和保守完整支撑margin，才沿原12+8松键停靠；最终4778.011/v0、margin1.9888，真实Moth进入Align。原4763/50、480与180响应预算不变。
8. 镜像上行：原2800连续停距低估当前1800摩擦，Grip4站位误差5.544>4。使用实际快照离散neutral预测后，Grip4–9的原40准备／12停稳／52总量、±4／corner／完整支撑／HP／0恢复与精确目标均实际通过。收据预测与实际误差约0.0014px是观察值，不冒称新增独立精度验收门。

所有预测只选择真实InputMap键；不写角色位置／速度、控制器目标，不关AI／碰撞，不补HP或重置机关时钟。生产Player保存字节SHA256仍为`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。

## 最终同冻结双路线：两路仍FAIL

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。final batch68994已终态exit1，之后才维护文档。

| 案例 | fresh build | engine | 首失败／通过边界 |
| --- | --- | --- | --- |
| thruster-route-exploration-diagnostic-20261019-011518-839-4d39317a9eee487bafc49778837bc2bd | 0／35332ms | 1／75128ms | Gear Bell实际K选地形，未完成Bell；前缀Frozen／Camp／Gear Rust／Basin lift-gallery通过 |
| thruster-route-main-diagnostic-20261019-011718-382-1e2f567b0fc54cf6aa7e2002d97e2391 | 0／27805ms | 1／131445ms | Grip9末端控制站共享walk90耗尽；已过九段上行、Scatter／Burst／Laser与Split，未到Boss |

Main三role均有真实telegraph／发射／玩家攻击连接／敌方HP0记录，observer为3实例、0恢复、fault none。完整route的AssertComplete尚未返回，不能只凭三role记录宣称完整Main成功。Explore observer实例0，远程后段尚未到达。

最终Main六个镜像站位观察：

| 目标 | 实际X | 预测X | 准备／停稳帧 |
| --- | ---: | ---: | ---: |
| Grip4 | 4386.797 | 4386.7983 | 18／9 |
| Grip5 | 4773.6904 | 4773.689 | 18／9 |
| Grip6 | 4390.758 | 4390.759 | 7／8 |
| Grip7 | 4773.6104 | 4773.609 | 18／9 |
| Grip8 | 4386.317 | 4386.3184 | 18／9 |
| Grip9 | 4773.6797 | 4773.678 | 18／9 |

## 两个当前首失败：下一步，不提前宣称修好

### Explore：Gear Bell预检跨await失效

原2／8／45时序中新增三个只读边界，未追加await或控制：caller physics4408/sample4392选Bell；prefix完成4410/4394已选Piston；K采样4412/4396实际Aim Piston，恰好一K、方向正确、HP1、恢复0。

caller到Bell anchor距离30.694、相对dx30.1022；prefix距离27.475、dx26.812；两者相对Y均6。**不能只归因进入32目标圆**，caller已在该圆内却仍选Bell。源码＋原生边界共同支持：Bell的R28/H84 Enemy胶囊还参与额外LOS，进入其中央直段后HitFromInside=false可跳过该遮挡，让零角Piston候选胜过偏轴Bell。原件没有ray collider/shape索引，不宣称直接观察了BodyCollider或HurtboxCollider中的哪一个。

下一只迁移该keyboard Gear generic Bell入站：原900内按实际neutral停止点准备外侧55..64、真实停稳／neutral／向内朝向，再进入原K时序；prefix后重查仅作为不发错误K的最后保护，不代替站位。保真实一K、实际目标／application／Exposed、HP与0恢复；不改resolver排序或忽略Piston。尚未实现。

### Main：Grip9共享站位预算在Gallery已耗尽

Gallery已停4197.0386/v0，满足4200±3，但walk90/90已耗尽。随后唯一真实Space13→14、完整落Grip9；第二次目标4436时没有剩余walk，仅neutral至settle7/12后失败。FinalCatch callback位于失败断言之后，**尚未调用**；不能说FinalCatch拉力不足。4320.098是着陆日志，不是失败瞬间最终X。

该局部WalkAndSettle仍用AirAcceleration1500的连续`v²/(2a)+4`估地面制动，与当前逆刹4000／neutral1800不符；固定+4也大于±3终止带。日志只保留前8帧，不把其余82帧称为已实测周期振荡。下一迁移该局部到实际neutral终点控制，保三段共用90／12／30及4200／4436／4780，不重置计数或增加跳／喷。尚未实现。

## 全部本轮原生案保留

以下后缀与phase拼接即完整目录。除单独标明的构建失败外，各fresh build均0警告0错误，无timeout；所有行为FAIL保留，不用后案通过前缀抵消原失败。

| phase | case后缀 | engine／ms | 当案结论 |
| --- | --- | --- | --- |
| thruster-main-abyss | 20261019-002506-667-4348adc51d17414db5636bd0a6c896d2 | 0／24517 | 首次前缀GREEN，不是整路 |
| thruster-route-main-diagnostic | 20261019-003724-543-51206392bb334ac8b06824973e735a29 | 1／44993 | Gear零步举盾过热 |
| thruster-route-exploration-diagnostic | 20261019-003844-748-f3e78dddeefc4b76aefb150c489357d4 | 1／27904 | ArchiveFloor离台死锁 |
| thruster-route-exploration-diagnostic | 20261019-004054-510-86677d6fa3c34cf28a92d717826f6591 | 1／34807 | 离台已过，Frozen Rust首4tick受击 |
| thruster-route-main-diagnostic | 20261019-004518-611-0356f4e827bf49dea041efadbae9f355 | 未启动 | build1／23725ms，两CS0103：缺motor局部引用；不是行为RED |
| thruster-route-main-diagnostic | 20261019-004922-524-d905f6de535a4e42ab67be11bb04ff8d | 1／53294 | Gear已过，Basin控制站余速越界 |
| thruster-route-exploration-diagnostic | 20261019-005324-684-265eab7a3d1f4b6aa68bc3e01a7c77eb | 1／38967 | 首击挡住，后段站位／气力门受击 |
| thruster-route-exploration-diagnostic | 20261019-005555-410-4de4a42af4a340d1b22c490d7fac2c6e | 1／42732 | Rust已过；下Moth受击后Camp发生1恢复与seam异常 |
| thruster-route-main-diagnostic | 20261019-005918-429-acf88bd35a23429189bbb10883632f60 | 1／83317 | Basin已过，Moth lure距离483.44775>480 |
| thruster-route-exploration-diagnostic | 20261019-010622-802-a5eed8ef462b496fb9fc96863a0d1692 | 1／75150 | Frozen／Camp已过，Gear Bell错误目标 |
| thruster-route-main-diagnostic | 20261019-010857-606-7af48f593b3045e1832606dd4e60d8b7 | 1／100507 | lure已过，Grip4旧停距误差超±4 |
| thruster-route-exploration-diagnostic | 20261019-011518-839-4d39317a9eee487bafc49778837bc2bd | 1／75128 | 同错误目标，三边界精确定位；不是修复 |
| thruster-route-main-diagnostic | 20261019-011718-382-1e2f567b0fc54cf6aa7e2002d97e2391 | 1／131445 | Grip4–9已过，末端共享90预算首错 |

005555还保留ranged observer的ZERO_RECOVERY_RED，不能遗漏或把seam异常自行解释为已证明的摔死。后案Camp零恢复不回写该案原因。两次多文件apply_patch上下文不符均原子失败；重读后准确应用，不记行为RED。诊断中一处把plain resolver误写成GetNode已在构建前改正，无原生失败案。

## 资格、保管链与存储

新完整诊断用每调用显式headlessNormalPlan参数复用原ranged observer和四个正常策略，默认false；入口拒绝非headless／非keyboard／局部scope混用。原Realtime runner仍拒绝headless。新完整phase进程cap取原normal600000ms，内部原路线／战斗预算不扩。正常窗口、human、performanceCertification、wholeGoal各标志均false。

最终两案五日志分别7／0／362／39／401行及7／0／1839／38／1877行，external与stdout＋stderr完整序列一致；各唯一engine ERROR，无额外WARNING／FATAL／Handle／ObjectDB。guards=[]，保留原audio drain，不新增退出等待、GC、静音或日志豁免；本轮无警告不关闭历史音频／资源失败。

四对inventory及continuation逐行／SHA一致；两案各212对实际selected（207 source＋5 helper）和各6份binary均与manifest匹配。source1695包含296 imported，不是1695个C#文件。仅核109项真实用户库存，不读存档语义。

最终共同文件SHA256：

```text
source-before.txt 7AE8C23A9EEF0E9AA398EB71C225040EECC3C8158C716035B8F019D0D2E0CD99
helpers-before.txt 1BF1160E745C19494AEFB2125C67A50FD59ECA5E1490FFAE8B50B00ABEDF98BB
user-before.txt BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB
binary inventory AC317965741E203FA4D33F5667FCE038D6988C3E57E0421BC3441D12B2052143
Knight.dll 3CD46D2DD8E7650DAA601369BD3AE4A1FD2AC1F12D019BEAB18DE24102D833EC
Knight.pdb 3D1D056BFC623874F028FDAB8AF7752C83E5BF8C73557C07EB2599B88C6DFB10
```

最终真实owner AST版本准入8例通过：两normal phase接纳1695、拒绝1675／1694／1696，18依赖及实际SDK／原pins保持；不启动engine。manifest在`normal-binding-v018-final-600a55cca49b4f72aabea7c85674b10b/normal-current-source.txt`，UTF8-LF字节SHA为D2626774BB346277FCDAC4EA73DEEDCE3805F02B2EBD558D2C9C47E9D6E3B7D5，不与上方不同文件编码的inventory SHA混报。原15例normal资格纯合同亦通过，Launcher参数明确，route／performanceCertification=false；此前本轮8例文件另保留，不覆盖。

全部受控native消费者终态后，专项进程复查无该checkout的Godot／dotnet消费者，才写本报告。无新展示媒体／美术／音频生成、付费生成、Git写／发布／ZIP或自动化。原媒体留存要求不变。下一先修上述两个消费者，再完整同版双路；最终正常窗口／Boss出口／旧档／真人／视听／性能及完整1–11仍未验收。
