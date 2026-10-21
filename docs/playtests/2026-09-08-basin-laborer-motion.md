# 盆地样板：劳动者轮廓、动作与横扫遮挡

日期：2026-09-08。基线：`dfe7a6c8c4dd2258d921f05a70c63f55db8439ce`。本地开发增量，不是新发行版本、正式美术或商业验收。

## 事实与实现

旧实景中劳动者以少量刚性块面构成，待机/防守轮廓相近，开甲缺少实际装甲分离。横扫的白色填充会盖住近身交战双方。

- builder 生成独立 `BasinLaborer.tscn` 并替换样板的劳动者实例；普通整关原场景和原造型保留。
- `BasinLaborerVisual` 建立十个真实绘制部件：背钟、脚、躯干、核心、两片胸甲、头、上臂、前臂、锤。部件只创建一次，不把动画名或空槽当成已制作动作。
- 通过 `IBellBackLaborerVisual` 接收真实 typed frame，避免把 Guard/Recover 归成 Idle。蓄力、挥击和恢复由实际阶段剩余时间驱动；身体局部姿势、相连臂节和锤一起变化，不移动玩法根节点。
- Guard 收臂，Telegraph 后收，Attack 前挥，Recover 低锤并闭甲；只有 Exposed 打开两片胸甲、露出核心。Dead 隐藏核心并倒伏。Hurt 仅短暂提亮局部装甲/锤边缘。
- 左右朝向对本地骨架整体镜像一次，保留权威世界空间攻击多边形，真实危险范围仍由现有 `AttackTelegraphVisual` 绘制。
- 样板 Impact 色面透明度从默认 `.34` 降至 `.06`，边界改为暖色 `2.5px`；整条危险轮廓仍覆盖整个 Active 阶段。默认场景三个样式参数仍保持原值。
- 感知距离仍沿用前一轮样板的 200px；健康、伤害、攻击时序、护甲接收器、玩家碰撞、机械爪和地图未改。
- 没有付费生成或外部素材。asset-gen 流程约束了付费边界；本轮为本地程序绘制，不是生成后分层贴图。

## 检查与纠正

证据根：`E:/OurGame/Knight/.evidence/september08-basin-laborer-motion`，不打进游戏发行资产。

1. `armor-red.log`：构建成功后，实际样板敌人缺少 ChestLeft/ChestRight/Core 的断言失败。随后实现分节造型和独立场景。
2. `normal-first` 普通窗口实图确认造型变化，也确认白色横扫面仍遮挡；`occlusion-red.log` 在保留原填充默认值时实际失败，随后仅配置样板低遮挡样式。
3. 独立审查发现新测试在延迟标题初始化前就 Start，双方默认 Idle 可能误通过。已改为先等待 Title、Start 后确认 Playing，再经真实敌人 receiver 触发开甲/超时/死亡/重置，返回标题重新开始后再验证一次。这些明确是受控集成夹具，不声称为玩家正常攻击。
4. 独立 pose 夹具检查 Guard、Exposed、Recover、锤位移、左右镜像、世界攻击点不变、死亡与重置。普通输入 LoopProbe 每张游戏截图另检查实际 actor/rig 状态和核心可见性一致。
5. `build-final.log`：零警告、零错误。`armor-final.log`：`BASIN_LABORER_VISUAL_TESTS_OK`，上述加强后的专项通过。
6. `sample-final.log`：`BASIN_QUALITY_SAMPLE_TESTS_OK`，Start/Continue/新局、恢复、引导、存档/设置隔离及默认入口保持通过。
7. `normal-final.log`：1280×720、RTX 3060 Laptop、OpenGL compatibility、非固定帧窗口，真实引擎输入完成营地—战斗—机械爪—补给—返回。1 次防御、2 次开甲、6 次有效攻击、1 次击败、1 次地形抓附与助推；0 玩家受伤、0 恢复，满血保留补给。自动路线不是人测时长或“新手无伤”证明。
8. 主代理审看 `normal-first` 与 `normal-final/04-blocked-sweep.png` 全尺寸截图：攻击面不再白色盖住角色，边界仍在；开甲核心与锤/背钟轮廓可辨。这是有界的遮挡/姿态改善，不是统一美术已经精致的结论。
9. 独立只读补审：原 P2 测试问题已解决；未发现新 P1/P2，静态通过不替代上述运行证据。

## 最终回归与录像

- `post-final.log`：完整 post 阶段实际退出 0，`PROLOGUE_TESTS_OK`。保留 3 条刻意异常用例警告：抛异常的爪观察器、错误 VisualRoot 类型、Completed→Recovering 拒绝；无错误。原相机 pre RED 未运行/覆盖，也未删除其注册。
- `ordinary-sample.log` / `ordinary-main.log`：独立样板和默认主入口直接正常窗口启动，均退出 0，无警告/错误。
- `movie.log`：同一正常输入闭环和状态断言通过；RTX 3060 Laptop 硬件 Vulkan、1280×720、30fps，423 帧/14.10 秒。此固定帧录像不用于帧率性能或真人时长认证。
- `laborer-proof.mp4`：H.264/AAC，未变速；主代理复核 `laborer-proof-review.png` 每秒两帧全流程及开甲、倒伏全尺寸截图。能够看到收锤、开甲、击败倒伏、抓台和返回；结束灰帧是测试卸载，不是掉落。
- 视频 SHA256：`DC529812BEFE61D08E18409122737A50E0502F8897F976E5E2AC8754B44CB1B1`。保留音轨，但未据此声称新增声音内容或音效感官验收。

## 未完成与下一步

主角近身攻击轮廓仍偏小，防御状态尚未进入人物本体姿势管线；命中特效落在人物中心，容易和敌人重叠。下一项优先处理真实攻击/防守姿态与接触点反馈，而不是扩大地图或继续堆文案。

整体场景仍是程序化视觉候选，声音与角色/环境一致性、用户审美评审、首次真人 5–8 分钟实测、免 SDK 独立包均未完成。旧盆地→上升 camera RED 保留；本增量不声称修复接缝。无新付费、外测数据、十万付费结果或新 GitHub 发行版本。
