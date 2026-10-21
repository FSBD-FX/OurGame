# 着陆、准星及探针生命周期 v006

2026-10-18；完整1–11 ACTIVE。v004/v005及失败原始证据不覆盖；本版不是整关／真人／发布结论。

## 已定位与修正

- 键盘诊断RED162857-6eca4d28b0154c239ee02f8c5ac780b3，Input12已有合法上目标，HUD只消费8/无目标；Grippable但Visible=false。原ProcessFrame信号在节点Process之前，旧fixture提前读UI。仅测试侧观测真实继承HUD回调完成序号，再按已消费sample屏障断言；不手动Present、不改产品优先级、能力或固定加帧。firstGREEN163337-b22ee0ff62334653b8a100433f8ed00f为Input/HUD同12、同目标、Visible=true，全部旧键盘合同通过。
- 同保存Player空中重挂真实RED163140-15ff979a886a432e9d6db6be68a953f3：SpaceHeld和采样仍正常，首错误为已释放KinematicCollision2D在Sampler.Prepare/TestMove被复用，位置冻结；退出又重复Dispose array。只将自有探针改为EnterTree创建、ExitTree先清引用再释放，不重置运动/落差/模块/Ready。firstGREEN163304-5a9253685aee4915af0be41d90e2d0a5：同实例上升续进、落地一次、无原生诊断；不冒称旧Claw全套重入已合格。

## 冻结16项终态

source1688，14GREEN/2FAIL；全部fresh Rebuild 0警告0错误、guards=[]，source/helper/user/binary四清单前后一致。owned batch已终态后才补测试/记录。实际命令为固定pwsh调用verify-claw.ps1各phase/SourceRows1688；严格无ExpectedFailure、无新GC/cache/drain/时限/日志豁免。

| phase | exit/ms | case |
|---|---|---|
| actions | 0/7224 | actions-20261018-164257-200-c525ee716cb848f082615e3b2480fd18 |
| fall | 0/11459 | fall-20261018-164334-189-926868108251447cb0e2df80278941f5 |
| fall-claw | 0/9091 | fall-claw-20261018-164413-769-724fee5304894f6c94ba1844b06d91bf |
| keyboard | 0/9729 | keyboard-20261018-164216-306-d64fae2345dd48d590ccdb833a598fe3 |
| prologue-post | 1/8570 | prologue-post-20261018-164703-891-632327f2c139482d801309a51fbf0f73 |
| prologue-pre | 1/89559 | prologue-pre-20261018-164457-272-e2a2a8389e0141b38da94d309ce2004b |
| thruster-impact-config | 0/532 | thruster-impact-config-20261018-163537-441-f44fbca2fffb418a8921ffc5e9bef103 |
| thruster-impact-formula | 0/538 | thruster-impact-formula-20261018-163505-042-fbb2697a02224f1794f86d0daf7c6613 |
| thruster-impact-legacy | 0/36984 | thruster-impact-legacy-20261018-163853-269-c932ab586d0e471ab7f184eed19924ce |
| thruster-impact-negative | 0/14961 | thruster-impact-negative-20261018-163643-711-5617233f22564601ad138c023657ec92 |
| thruster-impact-player | 0/4283 | thruster-impact-player-20261018-163607-098-e393139d2c314c699e2c7128ae74338f |
| thruster-impact-reentry | 0/1384 | thruster-impact-reentry-20261018-163820-763-9a2b3e07372d4455bbc0d173024d3e04 |
| thruster-impact-support | 0/14835 | thruster-impact-support-20261018-163733-873-21b01ff565194289917a1159573ee290 |
| thruster-player | 0/22695 | thruster-player-20261018-164001-729-2c6c2131f9554c389a0c2bca29e18758 |
| thruster-traversal | 0/7326 | thruster-traversal-20261018-164056-407-9167d65fe8c84b038f10a90675971c75 |
| updraft | 0/9572 | updraft-20261018-164135-322-0655e7042b90494c9037e64760901ee8 |

原始日志与库存：E:/OurGame/Knight/.evidence/october13-stable-claw/<case>/。前14专项stderr无诊断；旧runner原有托管wrapper-finalizer/drain观察保留，不搬至新strict thruster。broad巨大route向量原件保留，未冒称已逐向量复核。

pre仍旧Upper lower_to_mid_swing final(264.99634,160.6107)、149/150、stable115/noCrossing及2profile warning；post仍Bell(540.0704,1196.0139)/六级Recovery路径。当前无Handle再现不关闭历史七Handle或五FAIL。post在失败之前实际保存ShieldLane已记录HP5→4/1pulse/真实D，盾通过7pulse/HP5/真实L+Space；不能拿此大suite的局部绿遮盖后续失败。

## 仍需补齐的Task4边界

已有直接Health战斗负控及实际危险伤害，但“先可靠低冲击免伤、再真实接触危险/KillZone”的组合缺少专门证据。下一只补两自动键盘/真实Area场景，不改产品：保留同800落差、低冲击落地后走入默认ShieldLane或KillZone，观察真实伤害／恢复请求。恢复端仅测试只读计数，不能冒认完整生产复活／存档；广域Recovery失败仍OPEN。

Task4因此暂不结项，后续Task5装配/保存、Task6视听UI、Task7旧消费者及正常双路继续。正式新常驻0/50、地图/最终资源/真人/FPS/ZIP/GitHub等全目标不缩减。无本版新增展示媒体、付费、Git或自动化。
