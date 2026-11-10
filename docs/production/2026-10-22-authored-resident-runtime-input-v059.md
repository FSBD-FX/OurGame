# v059 · 50常驻布点进入实际关卡数据链

内部开发进展，不是新试玩发行。完整序章1–11及人口Tasks1–4继续ACTIVE。唯一总根`G:/knight`，当前Godot工程`G:/knight/Knight`；引擎比较未授权迁移UE，也不恢复已删除E根或另一个归档项目。

## 实际改动与边界

- 新增工程内版本化运行输入`assets/data/residents/canyon-resident-layout-v001.json`：50稳定身份、201个有序路点、完整home/territory/occupancy，来自已保留A案实际联合选择。原9803B/SHA`2F6E9E2A3ACE85E04D37470E81EB379E5A8F13FA85BC72AC476147574516D410`不改；工程输入实际9804B，SHA`AEACB6915FD16F9BDEE4ECC93FC59AD55D028EAE40E05EFCE83926831762DF18`。独立逐字节核验唯一差别是末尾一个LF，全部JSON值和点序相同，无绝对路径或receipt依赖。
- 新增`ResidentAuthoredLayout`生产消费者。运行读取固定res路径和独立固定payload/context SHA，不回退证据目录。严格验证1MiB上限、完整/唯一JSON属性、固定schema、精确50身份顺序、原route territory、有限点和矩形终点、最多16点、闭环运动/单点炮台。输出自有不可变数值副本，不加载角色场景、不授予物理/战斗/美术资格。
- 实际`PrologueLevelController._Ready`读取`ResidentLayout`，将50个带Placement的定义交给它唯一的`CanyonResidentManager`。`CanyonResidentCatalog.All`继续作为未布点的身份/分配基线，不能再将它的全null误报成实际Level没有收到数据。管理器尚无自动出生调用，24正式子型仍Unimplemented、正常地形仍127，因此正式新增敌人仍**0/50**。
- 出生拒绝细分为`placement_not_authored`和`scene_not_ready`，保留原`site_or_scene_not_ready`聚合码。实际Level逐ID证明确有位置但因场景未完成被拒；显式缺位置/有位置但Unimplemented/默认placeholder三个控制仍拒绝。没有放宽原子提交、可见范围、玩家、旧36主备出生记录、身体/路径/危险、HP/epoch/owner等任何权限。
- 旧联合codec改用同一生产解析器。143候选世界的完整准备与原生巡逻现在使用**实际res版本输入字节**，而非只验证新搜索结果；并逐字段要求其50行/201点与本轮独立新选择完全相同。仍是隔离、无目标/无攻击的候选，不是正常世界50投放。

本轮新增最终美术/音频0，没有改变地图/输入/力量/AI数值/存档格式，也没有制作新的图稿或视频。现有资产表与替换接口保留。已有导出preset包含`*.json`，但本轮没有导出Windows包，不能据规则声称解压包已验。

## 先失败再实现

所有case位于`G:/knight/验证证据/october13-stable-claw/`，原始失败不覆盖。

1. `resident-allocation-20261022-124609-414-b7fd4b633c2249b4a8eff54f569c4b95`：native1/540ms/guards0，编译通过，实际断言expected50/actual0；空解析器编译脚手架不能伪装布点。实现后`124757-132-a3876fc41c144f4a8fd4c5c388daa1ec` native0/612ms，31个同消费者损坏输入、原17分配及18联合codec反例通过。
2. `resident-level-lifecycle-20261022-124900-194-60ea165b148d4412b3ad380c1752bc03`：实际保存Level的空ResidentLayout触发预期失败；接入Load→manager后`125006-159-117559467e464f959ef10d2969f3c08f`通过。随后`125150-561-9c80576096df403986a27506d73e80b5` native1/1818ms检出就绪原因无法区分，添加上述细分码后`125256-680-e34c5c3d052a4d62817dc93c5048888c`通过；这不是声称此前实际发生了丢布点bug，而是可区分错误回接All模板的消费合同。
3. `resident-map-census-20261022-125658-935-b550f62165074072ac987afd29c343f4` native0/68803ms，50Ready/720运行完成但**严格FAIL**：FROZEN marker错误使用历史运行输入SHA，而当次保存的新候选/receipt持有新context SHA。没有放宽校验。修正为两条独立完整链：新候选payload/context/receipt仍与原marker相配；生产输入由实际SHA绑定到patrol记录，并要求两者全部数值/点序一致。最终同源新案130002严格通过。这个保管修复不叫资源寿命修复。

## 最终同源16项：12 PASS / 4严格FAIL

包括一次最终Census与随后串行15项，全部原生exit0、无timeout、guards0，16次quiet build。四项退出Shape2D RID仍严格FAIL，不因native0或成功marker放行。

| 阶段 | case后缀（完整名为阶段-20261022-后缀） | native PID/ms | 严格结果 |
| --- | --- | --- | --- |
| resident-map-census | 130002-283-5d3f2163d4444891aecbafce63ea85d6 | 8324/70787 | PASS |
| resident-allocation | 130229-129-eadadffeeb7849a28bbe1c0c196e0a6f | 46360/629 | PASS |
| resident-level-lifecycle | 130259-168-3d8e02843107486f886175b7d70c84e5 | 12012/4475 | PASS |
| resident-state | 130331-955-5db15b382f904d178f30227d68ae6b7a | 27980/529 | PASS |
| resident-lifecycle | 130358-608-33dcb622f23148c6928c827cf416a438 | 51428/1462 | PASS |
| resident-save-compatibility | 130426-911-ecd31ed2be134eafb4f7edaba553b6a5 | 32024/2658 | PASS |
| canyon-structure-v2 | 130502-602-e888e8ba3489458fbecc7344f822582a | 48072/1358 | PASS |
| resident-parent | 130531-442-9c07b8e31e70416aa0ca3d6251584714 | 44736/2292 | PASS |
| resident-scene-binding | 130603-675-c6fe0c60df9d464db918cb44de78c916 | 52796/7861 | **FAIL 137 RID** |
| resident-subtype-profile | 130637-672-2b6923743a9049e792c392f94fd6a386 | 39316/801 | **FAIL 31 RID** |
| resident-archive-metadata | 130704-871-209484dc90f54c22998b1c2ecf4f27f1 | 30768/4311 | **FAIL 255 RID** |
| resident-steam-metadata | 130736-036-c822dc6ff1034bc484ab68aba22770e0 | 52964/5050 | **FAIL 266 RID** |
| keyboard | 130807-423-f01781212ff447ef944c1c72a9f7081d | 49120/9802 | PASS |
| actions | 130848-855-76b486439275405d96e35b1c86c375a9 | 39776/7093 | PASS |
| prologue-pre | 130922-105-1f1c605e66e5442b95242485031cd828 | 54244/100134 | PASS |
| prologue-post | 131129-723-3eed6589667e46c497c239dad3df89e3 | 53760/147320 | PASS，限定4条故意警告 |

最终Census：输入生产SHA如上；当次独立payload`8EBCE387CD661260A4251038FB3A85B0E296422B6EAA4AE18A87172AA3968E01`，context`6B184127FAA6C462A19E9039A764C57DF323621618711383DAABEDD8B7809B80`。79个context实件/两个SHA与receipt核对一致。50真实Ready/51身体（含测试拥有的Boss）、41闭环+9炮台、720唯一physics帧14→733、489次真实落稳高度完整路径复核、Grow24间距/HP保持/healed0。`target_present=false/attackTested=false/formalSpawned=0/normal_world=false`保持；不是显示稳定60FPS或真人体验证明。

post原75案例/10 suites/0退役、8次owner生命周期、节点1→1。仅有故意观察者异常1、非法room-profile1、终态恢复拒绝2，stderr/external完全一致且scope逐项匹配；无其他ERROR/FATAL。不能把负测试警告当正常游戏零警告验收，也不关闭历史Handle/FATAL/音频故障。

## 冻结与保管

- 当前1712源/资源/导入项，881 C#。Source清单SHA`91E25307D86655F15FC168D3F1FBAB33AA8DDD26153BC7C6991263EDC7D46F5F`。
- 六binary清单`8B20F73BF57393C2FAA61501658E004C5A58DB71B493AB15E19D1F8A9E4CCA36`；DLL`CDA2ED6F048F0E1F61E553944D060AF852A885C002633CFD61405DCD7F16DBF4`。
- 真实用户109项仅原hash/存在性守卫，清单SHA`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`；没有读真实存档语义。
- 独立审计80对清单、5370份选定源副本（双侧）、164份helper副本、96份binary、32个build/native PID/argv/contract/wait均一致。不是说每案复制全部1712文件。普通build无诊断DefineConstants，6527类型中MetadataResourceTrace0/五已知诊断标识无。
- 所有自有原生消费者已结束才编辑本报告；HEAD仍`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`，index SHA`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。没有Git写入、删除、付费、依赖/引擎改变、前台按键、自动化、截图/视频或ZIP/标签发行。
- 媒体一次Check：4838B/基线3508B/增长1330B/Notify=false；不重建旧归档或自动化。检查时G余量201.03GiB。

## 决定、代价与下一步

1. 保留All为身份基线，新增生产布点数据链，不给数据发native/美术证书。若弄错，可能把错误数据带入所有区域，故固定外部SHA、严格31输入控制、真实Level消费者和当前完整50原生验证同时保留。
2. 本轮不推广143地图、不把24开发叶改成Implemented。代价是正常入口仍0新出生；这些明确仍须完成，不能用此顺序缩减目标。
3. 新候选与版本输入分别保管，完整数值相等才能把实际生产输入送入本轮原生空间检验。若弄错会给不同布点续签，因此两条SHA链与patrol绑定均不可省。

下一步实作匹配的正常地图/父小地图，以及24显式开发叶、真实隐藏准备/提交后stream-reset协调、实战Returning和旧26共存。地图差量不能粗暴全文件覆盖：新增16块之外，还需明确纳入已批准AbyssExit2局部中心740→748、宽200→216及匹配visual/ReadableEdge±100→±108；GripStone/权限不变，其他原127实体/旧波次/9残片/3checkpoint/门与出口保持。可编辑24profile源需版本化导入，A案参数是开发输入而非最终平衡；不能用测试Cases不同参数默默替换。

全部原26+新50=76普通+1Boss、骨骸10/幽灵10、地图/机关/移动/键盘/引导/雾/UI/存档、最终原创统一视听、正常Main+Explore/用户键盘/显示60FPS/Windows包/GitHub读回仍按完整1–11验收，未完成，不勾全Task或goal完成。

文档更新后主代理实际逐项读回：当前1712源与末案9个helper、6个Debug binary均与最终冻结清单一致，差异0；diff-check0、HEAD/index不变。无自有Godot/dotnet消费者，目标工具读回ACTIVE/unbudgeted。只增加本条文档记录，没有再次修改已验代码。
