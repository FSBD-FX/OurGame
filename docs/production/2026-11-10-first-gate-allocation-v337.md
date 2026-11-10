# v337 · 首关地图分配输入（不是新地图交付）

## 已完成的有界增量

按用户新图与Boss确认，加入可供后续builder使用的类型化目录：24主段、A–J十分路、旧76敌位逐身份迁移引用、24自然候选、10箱＋44资源点及分类机关配置。Boss23→24，J回23战前侧；第一关结束于门前，门后第二关排除。

本轮没有替换运行地图、生成正式美术或实现材料领取。100普通位、54奖励点、82个功能/控制/边界/材质配置单位都是初始规划，不是100只成品敌人/82种陷阱/已证明密度。目录不含碰撞坐标和世界尺寸。正式地形/实际领取与持久化/新敌行为/正常视觉及原十一项继续ACTIVE。

源码：`tools/build/prologue/FirstGateCampaignPlan.cs`，校验与反例：`tests/unit/FirstGateCampaignPlanTests.cs`，原models入口接入一行。旧九残片/最低六枚、场景/物理/AI/存档/键位未改。

## 实际红绿

- RED `models-20261110-123531-699-b8aac707fade4423a560cf2f73984310`：构建成功28572ms；原生516ms，实际断言期望24、现0失败；未超时、guards0，原日志保留。
- GREEN `models-20261110-123905-262-35bb743741374457b439cc748cbe574b`：构建0警告0错误；原生531ms、exit0、stderr0、无ERROR/WARNING、guards0。新目录正案/拓扑、旧身份遗漏重复、跨支路宝箱、类别/数量下限、空计划/错误资源/密度非有限与下降反例通过，原机械爪及地面接触模型仍执行。
- 密度反例用76/1000对100/2000，拒绝“敌人更多所以一定更满”；测试数字不是实际地图长度，实测密度仍未完成。

## 同版保管与旧范围

GREEN1929源清单SHA `D74A73EFED36DD63DB86153032B2518B66ECAE102EE0F23B605A4D0D7232DE8F`，六构建产物清单SHA `E8940FA3DC9A90B2A36E60985D1F51625D0242B3A525B3076E8BDBAA7AE6599F`。source/binaries/user/helpers/continuation前后清单各相同，真实档保护清单SHA仍BDDA。相对v336models封存源，仅新增目录/测试两CS及修改models入口；全部既有场景/运行源码逐行清单匹配。

该保管不等于完整回归。v336原十一测试9PASS2FAIL仍是上一源版本历史：Explore捷径HP2与旧HP1前提冲突、Main原899帧Bell余4未清；没有放宽前提/时限/补能量或重判旧失败。GCHandle/FATAL/ShapeRID根因、正式动作美术/正常声画/硬件/用户认可仍OPEN。

单次fresh只读审查C0/I0/M1，三pins与GREENsource匹配；延期Minor仅六类null行反例覆盖，生产已防护。本次没有Important/Critical修复轮或第二审查。[全部审查边界与判断成本](../../.superpowers/sdd/2026-11-10-first-gate-allocation/review-result.md)。

当前普通前段回归严格FAIL：`prologue-pre-20261110-124654-652-266fdd00f76347998e39deb6eb254d46`，native14612ms、exit -1073741795、未超时、guards0；实际`gchandle.is_released()`/`mono_object_disposed_baseref`、GodotObject.Finalize/GC.RunFinalizers原生FATAL。日志最后是相机fixture攀升区mount-return，不据此认定崩溃对象或新目录因果。没有同源重跑、调GC/cache/引擎/新dump。

普通后段 `prologue-post-20261110-124739-111-6e0ff10860c146df9c10f91e0fe1a46e` 严格PASS：185146ms、exit0、75案例/10suites/retired0/ERROR0及4条原有分类预期warning；源/六产物与models/pre相同。项目整体未通过。当前枚举1929源/六产物匹配，15对案内清单相同、18份实际binary复制匹配；[核验细节与CRLF比较修正](../../.superpowers/sdd/2026-11-10-first-gate-allocation/verification-v001.md)。没有当前完整主线/探索重跑或全十一项资格，目标保持ACTIVE。

本次媒体Check：21,736,453 bytes/185文件，Notify=false，未到20GiB提醒档；没有录像/付费生成/自动化。只保存本轮用户参考及索引。

## 规划/来源/下一步

[设计](../superpowers/specs/2026-11-10-first-gate-campaign-design.md)；[逐区分配表](2026-11-10-first-gate-distribution-v001.md)；[本增量计划](../superpowers/plans/2026-11-10-first-gate-allocation.md)；玩家意见U-20261110-01/02已记录。原参考保存于 `G:/knight/过程图稿与视频/2026-11-10_首关24主路10分路_用户参考_v001`，逐字节原件与展示副本、SHA/状态索引齐全。

下一实际制作是独立奖励事务/Q适配和01–04实体岩壁/顶壁/洞口/落脚样段，然后扩24/10；所有分路回返、旧机关实例保留、自然生态、Boss门禁/实体终点须实测。无新付费/GUI/OS输入/录屏/自动化/Git/删除/真实档/额外采集/引擎或GC缓存试验。
