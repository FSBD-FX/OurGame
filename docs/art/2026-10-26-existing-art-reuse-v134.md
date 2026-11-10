# 现有美术复用盘点 · v134

2026-10-26，只读本地文件/脚本/批准索引，没有生成、下载、构建或显示新画面。根枚举确认`assets/`共13张PNG。`final:true`只是现有文件/来源/许可门，不是最终视觉批准。

## 已批准且已接入的四个独立原图

| 文件 | 当前所有者 | 实际边界 |
| --- | --- | --- |
| [lantern.png](../../assets/player/thread-spool-bearer/lantern.png) | Player Formal/Lantern，PlayerFormalLayer加载并DrawLantern | 96×96，独立灯获批；不能放大当整套场景灯具 |
| [half-bell-cleaver.png](../../assets/player/thread-spool-bearer/half-bell-cleaver.png) | Formal/Weapon实际区域绘制 | 1402×1122母版；v100接入，不是完整角色 |
| [right-grip-overlay.png](../../assets/player/thread-spool-bearer/right-grip-overlay.png) | Weapon刀后主握点同步 | v127接入，不是新手臂动作图集 |
| [left-grip-overlay.png](../../assets/player/thread-spool-bearer/left-grip-overlay.png) | Weapon真实副手接触时绘制 | v130接入，不代表身体/披风总装批准 |

来源与批准边界：[玩家生产Gate](player/player-art-production.md)、[v096逐件来源核验](../production/2026-10-24-profile-use-serial-and-asset-provenance-v096.md)、[v100接入](../production/2026-10-24-approved-weapon-runtime-and-bitmap-observer-v100.md)、[v127](../production/2026-10-26-one-hop-regeneration-and-approved-grip-v127.md)、[v130](../production/2026-10-26-secondary-grip-and-connected-arms-v130.md)。只读盘点核到四个当前SHA及导入目标存在；本次不是新的运行/像素或正常速度验收。

## 其余现有图集不是成熟插画库存

| 文件 | 当前事实 |
| --- | --- |
| [environment-atlas.png](../../assets/environment/nameless-belfry/environment-atlas.png) | 512×512条纹色板；实际环境主要程序/Polygon绘制，不是岩壁/遗迹绘画包 |
| [mechanisms.png](../../assets/environment/nameless-belfry/mechanisms.png) | 512×128四原型符号；机关外观自己Draw |
| [enemy-atlas.png](../../assets/enemies/formal/enemy-atlas.png) | 768×160平涂部件；场景metadata引用不等于PNG被显示，EnemyFormalVisualPart实际程序绘制 |
| [fallen-bellkeeper.png](../../assets/boss/fallen-bellkeeper/fallen-bellkeeper.png) | 512×256简化部件；Boss实际程序绘制，非最终Boss绘画批准 |
| [player-layers.png](../../assets/player/formal/player-layers.png) | 512×256矩形占位，十slot不是已完成的身体/披风素材 |
| [prologue-vfx.png](../../assets/vfx/prologue-vfx.png) | 256×64四圆环原型，实际VFX主要程序绘制 |

另三张Rust/Moth/Laborer分图同属简化部件，不增加独立正式族系。UI没有PNG套件，当前是自有矢量/文字；矢量UI不要求伪造一套位图作为完成证据。

## 下一制作边界

已有CanyonBackdropVisual/CanyonPlatformVisual和远中近/可走/前景层可复用为承载接口；没有证据支持“还藏着一套现成精美峡谷神殿包”。角色body静态参考含其他部件，不能整图塞入单Body层或恢复被拒idle。正式场景绘画、怪物族系与全动作、角色主体及整体画风仍需专门制作和实际审阅。

继续原十一项，不把占位图集或清单final字段当完成。新增付费制作需另行确认，未擅自改变身份、复制Ori/空洞骑士资产、下载外部包或新增成本。[本轮地图进展](../production/2026-10-26-gallery-throat-and-gravity-return-v134.md)。
