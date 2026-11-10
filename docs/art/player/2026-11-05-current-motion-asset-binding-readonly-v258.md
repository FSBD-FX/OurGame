# 当前角色美术／动作接线核对 · v258（只读）

## 事实

保存[Player](../../../scenes/player/Player.tscn)的VisualRoot/Formal实例是十槽Node2D程序rig，不是已经接入的完整身体／披风PNG分层动画；没有保存的Spine/Skeleton2D/Bone2D/AnimationPlayer资源。真实链为PlayerPresentationCoordinator.PresentCurrent→PlayerPresentationTimeline.Step→PlayerPresentationFrame→PlayerFormalVisual.ApplyPlayerFrame。player-animation-map.json只是状态/clip元数据，不是绘制动作帧。

| 组件 | 当前实际运行资源 | 驱动／可编辑接入口 | 缺口 |
|---|---|---|---|
| 身体／四肢 | PlayerFormalLayer.DrawBody/DrawArm/DrawLeg程序几何，没有正式身体PNG绑定 | PlayerFormalVisual.ActionTarget、共同重心/脚底求解、GroundAttackWristOffset | 本地候选细分层还没有到十槽rig的正式绑定与轴点映射 |
| 披风 | DrawMantle多边形、褶面与描边；没有mantle.png绑定 | SecondaryMotionModel→PlayerMantleShape.Map | 候选纹理／网格变形接线未完成，不是更换atlas就能解决 |
| 双握武器 | assets/player/thread-spool-bearer/half-bell-cleaver.png及right/left-grip-overlay.png | PlayerFormalLayer默认加载与DrawTextureRegion；Formal近腕→主握点→副握点 | 三PNG与保管层同字节，但不代表完整身体素材已接入 |
| 悬灯／系带 | 独立lantern.png，24×24绘制框；系带为程序折线 | Timeline→SecondaryMotionModel→Formal二级槽 | 保管层的灯壳/光晕/三段系带PNG未绑定，独立运行灯不是同套灯 |

精确源码入口：PlayerPresentationCoordinator.cs的PresentCurrent822/Present773，PlayerPresentationTimeline.cs108，PlayerFormalVisual.cs107/ActionTarget579/重心353/握持838/腕轨迹899，PlayerFormalLayer.cs220/228/264/284/299/313/326/352/357，PlayerMantleShape.cs8；生成源tools/build/art/BuildPlayerFormalVisual.cs14。后续应同步维护生成源，避免生成覆盖手改场景。

## 已知候选，不是找回旧批准V1

只核对已知保管目录G:/knight/美术源文件/迁移留存/2026-10-24_complete-layered-001_未发布，没有扩大旧V1搜索。6SVG/35层PNG均存在；相对旧final-manifest为19相符/16不符/0缺失，相对迁移custody记载的当前字节0漂移。详见该目录KNIGHT-留存说明.md与layer-version-audit-20261024.csv。

可编辑源包括authoring/body.svg、mantle.svg、weapon-hands.svg、lantern-tether.svg等；layers中有头/躯干/骨盆/左右细分肢体、披风和衣领等。torso/多段左肢/mantle等16项与旧manifest不同，不能沿用旧批准。总装仍ready-for-review，smallScale/animationReady仍not-started。

方向参考thread-spool-bearer-direction-v2.png与历史full.png/body.png只是身份/早期参考；旧a2、rejected-b-route与idle grid不恢复成正式动作。assets/player/formal/player-layers.png是生成器十色块原型色板，当前身体绘制不采样它，换该PNG不会替换当前身体。用户U-20261105-01“后续重新审阅现有稿”不是当前身份、PNG、分层、总装或动作获批。

## 推理与风险

只增加插值或图片缩放无法补上未设计的起势、发力、经过、收势与布料形变。现有v243攻击二级来源、v244主动腕轨迹、v246共同重心已经接过，不能重新记为未开工；下一样段应沿同一权威phase/elapsed继续，勿新增另一个动作/输入时钟。近腕→武器→副握点及共同承重→落地/空中→二级/脚底的既有顺序不可随意重排，否则脚滑、握点延迟、附件漂移会回归。

正式素材接入前需先冻结待审候选版本/hash并审阅，不使用旧E路径脚本、不恢复旧批准、不自动付费重绘。R-I可见惯性、R-C角色动作语言、R-P物理/操控仍分别OPEN；数学连续与无窗口几何控制不能代替正常速度视听、实际操控及用户审阅。依据docs/production/2026-11-03-character-motion-rework-requirements-v001.md。

本核对由只读explorer根据当前保存场景、脚本、本地清单与逐文件hash完成；没有native、GUI、图像生成/编辑、媒体采集、资产或代码写入。本页不是新美术、动作完工或批准记录。
