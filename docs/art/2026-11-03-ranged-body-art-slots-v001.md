# 远程身体换图接口 · v001（程序样段，非正式美术验收）

四原保存场景路径和`VisualRoot/WeaponVisual`默认类保留；不改SceneId/存档。默认程序身体：散弹宽肩锅炉、连发窄身弹链、导弹背筒低重心、激光棱镜细身。轮廓/比例/动画节奏为开发样段，不是用户批准的身份稿。

所有`Texture2D`为借用资源，表现器不Dispose，不修改原图。设置下面导出属性即可替换对应部位；不设置则继续使用程序轮廓。图像在画布内等比适配，不全图拉伸；关节只旋转和平移。坐标单位为Godot世界像素，不是素材源分辨率；空透明留边需绘者控制，几何接口不检测alpha。

| 槽 | 局部画布(x,y,w,h) | 关节/用途 |
| --- | --- | --- |
| TorsoTexture | -24,-25,48,43 | 躯干承力坐标 |
| HeadTexture | -13,-10,26,20 | 相对头中心，中心在躯干(0,-25) |
| UpperArmTexture | -4,0,8,18 | 肩在(0,0)，肘在(0,18)，远/近臂共槽 |
| ForearmTexture | -4,0,8,18 | 肘在(0,0)，手在(0,18) |
| ThighTexture | -5,0,10,13 | 髋至膝13 |
| ShinTexture | -5,0,10,13 | 膝至踝13 |
| FootTexture | -8,-4,16,4 | 脚底localY34，近远脚共槽 |
| WeaponTexture | -24,-26,70,52 | 原枪整体画布，握点(-7,9)/(7,7) |

需要非对称远近肢体或不同遮挡关系时，替换整个`WeaponVisual`节点并实现`IRangedWeaponVisual2D`：帧携带真实Role，Clear撤攻击瞬态，Reset明确重开检查点generation；不必继承默认renderer。当前不承诺运行中热换节点。未来Spine/Skeletal renderer可实现此接口，不意味着已采购/接入Spine。

`CurrentRig`和`GetPartGeometry(part,far)`与实际Draw共用，提供姿态/画布/适配矩形与程序轮廓，不是像素轮廓或危险范围。part为torso/head/upper-arm/forearm/thigh/shin/foot/weapon。权威预警、Muzzle及伤害仍由现有攻击系统决定，不能用装饰枪口替换碰撞。

本轮无新PNG、生成费或正式族系素材。正常速度轮廓、动作美感、枪口对齐及硬件性能仍须有授权后实看；不能由headless几何或资源manifest的final标签代签。

当前接口边界：骨架脚底为Y34，但极宽脚图仍按共同居中规则适配，实际矩形可能在脚底上方留约1px空隙（v214审查M1，尚未修底对齐）；正式脚部接图必须复核。极小非零父变换aim回退及重复姿态/轮廓计算的性能优化也未关闭，不能把默认样段通过当成任意替换资产都合格。
