# 短喷着陆同版回归 v005（仍未结项）

2026-10-18；完整1–11目标 ACTIVE。保留v004及全部旧证据，本记录不构成发布、真人体验或整关通过。

## 冻结批次结果

source1688的15项已全部终态，12GREEN／3FAIL。每项fresh Rebuild均0警告／0错误；source/helper/user/binary四类前后库存一致，guards=[]。未增加GC、cache、drain、时限或日志豁免。

| 专项 | engine exit／ms | 独立case |
|---|---|---|
| impact-formula | 0／563 | thruster-impact-formula-20261018-160833-890-4ee28f64f910481ea70eb74c19d12019 |
| impact-config | 0／542 | thruster-impact-config-20261018-160911-427-6e6d2aeb57384c40a08b0a437221a16d |
| impact-player | 0／4167 | thruster-impact-player-20261018-160949-179-4400d30bbc8e40d8b8293621b2f2f237 |
| impact-negative | 0／14719 | thruster-impact-negative-20261018-161025-006-81d105edf6554216985d708788f9cbc5 |
| impact-support | 0／14826 | thruster-impact-support-20261018-161111-410-d712c899c59c4a4c89a1a5371744da06 |
| impact-legacy | 0／36944 | thruster-impact-legacy-20261018-161158-893-de7ad1992d36424187ccae1b6e9e2388 |
| thruster-player | 0／22777 | thruster-player-20261018-161307-904-a3f5860db4124819a0ec21a9402801cc |
| thruster-traversal | 0／7519 | thruster-traversal-20261018-161359-207-113c8df254594620adb013af93d81b09 |
| updraft | 0／9615 | updraft-20261018-161436-919-40b1495832de4652bed79e1851d717d4 |
| keyboard | **1／931** | keyboard-20261018-161521-218-c8f93b3b06ad495593e6681ad5dabda3 |
| actions | 0／7216 | actions-20261018-161554-703-2af09b41e67e402a9bc36d2178ece0f6 |
| fall | 0／11588 | fall-20261018-161634-174-d6d2e0698eba47c6802103fdcc7a5cdd |
| fall-claw | 0／9201 | fall-claw-20261018-161716-740-7c16ead5ed3d4f60bec09be886469bb5 |
| prologue-pre | **1／88814** | prologue-pre-20261018-161800-264-f010c68b9c5f4a219b1962672e79a9f1 |
| prologue-post | **1／8251** | prologue-post-20261018-162004-359-ed1f37e644eb4aff9595274758318c0e |

原始证据均在 `E:/OurGame/Knight/.evidence/october13-stable-claw/<case>/`。

## 未通过项与下一步

- keyboard：`KeyboardClawRuntimeTests:64` 的 `reticle.Visible && State==Grippable` 合取断言失败；前一行真实preview.Surface已选中目标。原日志没有可见性／状态分项值，不归因输入、窗口失焦或拉力。先观察实际HUD回调完成序号及其消费的输入／目标，再判断是UI完成边界还是产品功能，不盲目增帧重跑。
- pre：旧UpperPendulum lower_to_mid_swing停在(264.99634,160.6107)，149/150步、115稳定落地帧、无预期LandingZoneCrossing；两项旧profile warning继续OPEN。本次没有Handle错误，但不能关闭上批七次Handle及此前资源生命周期失败。
- post：旧KillZone找不到六级父路径ILevelRecovery；旧Bell实测落地(540.0704,1196.0139)失败，继续OPEN。
- 静态检查另发现Motor仅Ready创建LandingImpactSampler、ExitTree释放；同实例移出再挂接可能复用已释放探针。正常生产路线暂未找到此操作，但已有小地图生命周期测试实际重挂Player。必须先用同一保存Player、自动physics、真实Space跳落验证，再决定最小修复，不把推断冒称已复现。

Task4仍in progress。Task5保存装配、Task6typed视听UI、Task7旧消费者及正常Main/Explore未开始。正式新常驻0/50、地图/资源/机关/真人/FPS/ZIP/GitHub等依完整总目标保留。未生成或展示新媒体、未付费、未Git写入、未恢复自动化。
