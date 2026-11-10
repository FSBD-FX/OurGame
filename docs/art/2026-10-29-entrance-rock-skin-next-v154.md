# 下一视觉切片：入口岩土角色（候选，未实现）

2026-10-29只读核查，非新图稿/资产或画面验收。正常父直接引用CanyonEntrance.tscn；旧七级阶梯已在入口重构移除，不能再做一次并计进展。

实际缺口：`CanyonPlatformVisual._Draw → DrawCanyonRockMass`对五块厚实体使用同一横层/顶端泥土配方：EntranceOuterWall48×720、StartFloor320×84、BridgeFarBank880×84、EntranceSkyOverhang552×80、RiftEntryCeiling680×400。背景实际走DrawEntranceNaturalApproach；旧Cavern/Foundations死方法不当作当前画面或重新启用。

建议下一制作片：由CanyonContentBuilder的BuildEntrance/Solid/Platform显式赋地基、侧壁、顶板三种地质绘制角色；地基压实土与内嵌碎岩、顶板大尺度断面/内下缘阴影而非地面土帽、窄壁竖裂面与截断岩层。沿用低频/少噪点的既有色光，不按节点名或尺寸猜角色，不新付费生成。使用同一实际Draw消费的只读命令数据验证，不拿metadata/颜色数量当美观。

五个真实world矩形依次为(0,0,48,720)、(0,636,320,84)、(400,636,880,84)、(48,0,552,80)、(600,0,680,400)。全部填色/含线宽笔画严格在实体内部，保留六像素ReadableEdge、抓面、碰撞、CatchShelf、薄LowBridgeCeiling、其他区和旧样板默认分支。不能画假洞、假可走面或冒称解决整图结构。

主要文件：scripts/presentation/CanyonPlatformVisual.cs；tools/build/prologue/CanyonContentBuilder.cs；生成叶scenes/prologue/sectors/CanyonEntrance.tscn。使用已有PrologueCanyonBuildParts.Build/SaveAndValidate和BuildCanyonEntrance，只生成/验证对应叶，不手改生成场景；先证明玩法指纹/实际矩形/材料/数值不变。

保留CanyonMaterialSkinTests与ContinuousCanyonReadabilityTests；新增真实builder/保存五消费者角色/实际命令、有限与含笔画边界、非入口/关闭staging/非GripStone/旧样板控制。原CanyonMaterialSkinCapture未开UseCanyonDepthStaging，不能给新路径像素续签。正常尺度正式雾/HUD/预警下的出生→断口→岩口→顶爪→接缝像素、正常速度和用户审美另验；当前仍NOTSEEN。

这些是开发建议/下一任务输入，不是用户已批准具体新视觉，也不是入口正式美术已经完成；原全十一项范围保留。
