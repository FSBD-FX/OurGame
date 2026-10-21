# 着陆边界补齐 v007

2026-10-18。完整1–11 ACTIVE；仅短喷整合计划Task4在brief专项范围完成，整计划／整关未完成。v004–006及全部RED原件保留。

v006的source1688冻结16门14GREEN/2FAIL保留。随后只补测试/runner/owner/记录，不改产品；以两个case源清单精确读回，Motor、Sampler、FallDamageModel、Tuning、KillZone、ShieldLane文件均1对1且字节相同，不冒称整个测试源清单未变。

新增严格case `thruster-impact-dangers-20261018-165113-389-3ea5dc9c17a345d58f2eb985600a93be`，engine0/6941ms，fresh Rebuild0警告0错误，source1688/guards=[]，source/helper/user/binary四清单均相同。stdout/stderr/external与终态已读回，无原生诊断；未加GC/cache/drain/时限/豁免。

同保存Player先真实W+Shift将800落差撞速降至111.16656，高度799.9961保留、伤害0/HP5；站地后真实D走进Area，速度(260,0)。ShieldLane实际1次native overlap/1pulse/1次伤害，HP5→4；KillZone实际1次native overlap/1次FALL RESET请求，HP5不改、无高度伤。恢复端是已有不会传送/治疗/复活的测试计数器，只证明生产KillZone请求委托独立于减伤，不冒认完整生产respawn或关闭广域Recovery失败。

Task4新自动physics场景21个（18着陆＋同实例重挂1＋独立危险2）；旧13摔伤及真实爪/风/预算/键盘/动作专项通过。两已定位问题有实际RED→GREEN：HUD旧sample完成边界、Motor树驻留探针释放复用。Task4既定保守采样/高度公式/配置冻结/一次消费及独立危险合同按各自证据覆盖。

仍OPEN：五历史严格失败、七Handle观察、pre Upper/profile、post Bell/Recovery；当前广域不绿。下一Task5资源保存与装配资格，随后Task6typed视听UI/Task7迁移及正常双路；50常驻/地图/最终美术/真人/性能/ZIP/GitHub等完整目标不缩减，无新展示媒体/付费/Git/自动化。
