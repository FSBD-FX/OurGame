# 敌人家族外形现状 · v001

后续状态：v318已单独修复下文V05脚图矩形底锚候选并通过原生RED→GREEN／回归，见[修复记录](../production/2026-11-08-ranged-foot-art-fit-v318.md)。下文是v316未实施时的盘点原件；家族外形路线、正式素材和像素／动作审美仍未批准或因此完成。

## 已核实，不当成新批准

本轮只读盘点，未生成图片、未修改renderer／场景／素材，未把开发叶改标正式。

- `scripts/prologue/residents/ResidentSceneRegistry.cs`仍将24个开发叶列为`ExplicitPlaceholder`。`FinalOnly`不是这24份外形已经完成的证明。
- 岩／废件／骨骸游荡者与`lost-echo`同用Rust actor及`RustShellFormalVisual`；Ghost另由`RustShellGhostResidence`将VisualRoot alpha乘1／.3，实现相位，不是独立外形。
- 同职能冲锋者共用`CanyonSkitterVisual`，空中族系共用`GraveLampMothFormalVisual`，重装共用`BellBackLaborerFormalVisual`，Ghost炮塔仍用`BellBoltSentryVisual`。
- 四远程`RangedWeaponVisual`按Scatter／Burst／Missile／Laser职能区分完整程序形体，未按Family区分；Foot等八槽已存在，但不是已经装好族系正式素材。
- `EnemyFormalVisualPart`当前实际程序绘制；`enemy-atlas.png`仅metadata引用不能当运行像素证明。复用资产边界参见`2026-10-26-existing-art-reuse-v134.md`。

v306已记录“制作路线待选择”，文件没有保存此前两个选项的具体措辞，不从缺失内容推断用户批准。本次非阻塞问题明确询问“先看不付费程序绘制样段（推荐）／先审阅分层美术稿”；未收到回答前不把默认勾选当提交，不将候选自动标正式，也不授权付费。骨骸／幽灵背景设定已纳入既有方向，但具体造型和像素资产仍待制作／审阅。

## 可接样段的工程边界

第一组接点可选`bone-wanderer`与`lost-echo`的`VisualRoot/Formal`；保留Rust战斗时间线、Ghost相位／交互、HP、碰撞、稳定ID和placeholder资格。不能声称Rust现有Body／Shell／Weapon／Corruption分件已经等于完整四肢rig；需根据选定制作路线安排真实动作承载。

## 另一个已定位但本轮未实施的V05适配候选

`scripts/enemies/ranged/RangedWeaponVisual.cs:GetPartGeometry`目前所有纹理居中等比适配。脚画布为`Rect2(-8,-4,16,4)`，宽80×10脚图被适配为`Rect2(-8,-3,16,2)`，矩形底边为-1，较脚踝基线悬空1单位。应仅对foot底锚：保持等比／X居中，令Y=`canvas.End.Y-size.Y`；非foot继续居中。当前尚无该反例的原生RED，不能写已修。

后续独立字面量控制：

- 80×10脚图：`Rect2(-8,-2,16,2)`；10×80：`Rect2(-.25,-4,.5,4)`；无图仍`Rect2(-8,-4,16,4)`且UsesTexture=false。
- 原80×20躯干仍`Rect2(-24,-9.5,48,12)`；20×80仍`Rect2(-5.375,-25,10.75,43)`，拦截误把所有槽都改底锚。
- Scatter／Burst／Missile／Laser脚踝X分别±20／±15／±22／±12，Y34。近／远脚及左右瞄准／支持的Scale(-1,1)镜像都应保持原rig与纹理比例；读取几何不推进时间。无任意旋转父级世界Y34声明。
- 现有`tests/integration/RangedWeaponPresentationRuntimeTests.cs:CompleteBodyRigHasRealArtAndMotionContracts`的纹理几何段可追加控制，`PartGeometry` helper要允许far参数；场景不应拥有或Dispose调用方纹理。

这只涉及纹理矩形，不检测透明留边，不是鞋底像素贴地、正式新素材、全动作或正常速度审美验收。勿将此候选与本轮齿轮修复混为已经完成的视觉提升。
