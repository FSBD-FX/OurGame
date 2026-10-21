# 盆地样板：破拱与悬吊遗迹环境

日期：2026-09-08。基线：`fd536b72a4f8a4e769918cd40e084099af9b30db`。本地开发增量，不是正式美术或发行验收。

## 改动及边界

- 仅独立样板的 Background / Midground 换为 `BasinEnvironmentVisual`：峡谷大块面、远处遗迹、破损石拱、砌石柱、青铜连接与悬链。替换旧背景而非叠加齿轮、线条和纹理噪声；现有取景范围内的背景整体更新，不扩建左侧或整关玩法。
- 保留 `VisualRoot/Background/AtmosphericRift` 与 `VisualRoot/Midground/BuriedSanctum` 两个独立替换槽。静态 `_Draw`，无逐帧重绘、粒子或物理对象；由构建器打包，不在运行时拆改场景。
- EntryCatch、SalvageFloor 与右半部 BasinFloor 增加内侧砌石面、稀疏铜箍、下缘阴影。新 `BasinPlatformVisual` 继承原材质皮肤，保留相同尺寸、位置、材质及原 ReadableEdge 子节点。地面左半部仍使用原皮肤。
- 台下净空134px、上返抓取位置、单向碰撞/容差、抓取材质/半径/偏移/碰撞点选取全部不改。背景石拱和链条不能抓取；真实可站立顶边仍用原亮边。新玩家能否理解该区分，仍需真人观察。
- 默认完整序章的场景、共享环境绘制类与数值不变。另外两个样板场景只有构建器重打包产生的节点 ID 变动。
- 没有图像生成、素材采购或新付费。asset-gen 用于守住资产与付费边界，本轮是本地代码绘制候选，不称手绘成品。

## 证据

证据目录：`E:/OurGame/Knight/.evidence/september08-basin-environment`。

- `build-red.log` 构建成功后，`environment-red.log` 明确因缺少两个环境替换槽失败，退出1；并非编译问题。
- 接入后 `build-first.log` 零警告/错误，`pack-first.log` 样板125节点打包/重载125节点，入口验证通过。
- `environment-first.log` 退出0：独立槽和层级、普通场景隔离、全部实体矩形与皮肤对齐、精确顶边多边形、单向规则、抓取规则、视觉树无物理对象通过。结构测试不判断审美或玩家理解。
- `normal-first.log` 普通1280×720窗口、OpenGL compatibility、非固定帧、正常 Start 与引擎输入闭环通过：1次防御、2次开甲、6次命中、1次击败、1次地形附着/助推，0伤害/恢复/爪失败，满血保留补给并归营。自动路线不是首次真人时长样本。
- 主代理审看 `normal-first/02-camp.png` 与 `08-salvage-platform.png`：破拱形成整体地标，战斗区中部留暗，悬链接到实际高台，真实台顶边连续；人物、敌人、补给和教学卡可辨认。仍有大色块和横条原型感，不是参考品质认证。

## 最终验证与审查

- 独立审查归一化资源引用/随机节点ID后，确认生成场景79个 Gameplay 节点与基线相同。发现替换皮肤追加到末尾会改变覆盖顺序；`order-red.log` 有效失败 BasinFloor expected=1、actual=8。构建器改为保存原 GetIndex 并 MoveChild 复位，不改图形或玩法。补审确认该问题和类型/导出值测试缺口均已解决，无新 P1/P2。
- `build-final.log` 零警告/错误；`pack-final.log` 125→125；`environment-final.log` GREEN，新增锁定原绘制顺序、两个背景的类型/角色/Bounds、三处新皮肤类型和地板 DetailStartX=800。
- `sample-final.log` 样板生命周期/引导/存档隔离通过；`combat-final.log` 主角真实盾/动作/命中专项通过，只有两个原刻意非法Profile警告；`post-final.log` 完整post通过，75个旧分类用例，只有原抛异常观察器、错误VisualRoot、拒绝Completed→Recovering三条刻意警告，无ERROR。这三组在最终两行绘制顺序修复之前运行；最终重新打包后已重跑环境专项、普通完整回路及启动/录像，不宣称再跑完整post。旧pre相机RED未运行、删除或宣称修复。
- `normal-final.log` 最终普通窗口完整回路再次通过，计数与首轮相同；`ordinary-sample.log` / `ordinary-main.log` 两个普通入口直接启动均退出0，无ERROR/WARNING。
- `movie.log` RTX3060 Laptop硬件Vulkan、1280×720、30fps固定录像，423帧/14.1秒，完整闭环再次通过；不是性能测试或真人时长。
- `environment-proof.mp4` H.264/AAC、未变速、保留原音轨，SHA256 `80447524740E813AEC521A055D0FD90465E82E43FA99433BCF96341C6D3841CD`。主代理已审看全程每秒2帧表，以及最终普通窗口横扫/举盾、抓附全尺寸画面：真实威胁线与抓爪绳可见，前景UI未遮交战/落点，没有新增环境遮盖双方。尾部灰帧是测试卸载。并未逐帧认证动画或完成声音审美。

## 尚未完成

统一声音与正常速度感官、近战双方轮廓分离、新手首读、首次真人时长、用户品质认可和免SDK交付包。旧盆地接缝相机 RED 原样保留，未把不经过接缝的样板成功当作修复。十万付费仍无验证数据；本轮不新建发行版本、ZIP或GitHub发布。
