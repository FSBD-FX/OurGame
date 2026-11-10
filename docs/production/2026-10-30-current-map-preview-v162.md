# v162 · 当前关卡结构与配置分布图

工程仍是 `G:/knight/Knight` 的 KNIGHT Godot.NET，完整原十一项目标 ACTIVE。本轮推进明确待办 R09，不改变游戏玩法/地形/敌人数量/键位/数值，也没有制作新游戏美术。游戏窗口显示仍 NOT SEEN，本轮没有打开游戏窗口或发送系统键鼠。

## 当前图与完整清单

最终修订归档：`G:/knight/过程图稿与视频/2026-10-30_当前地图_v162r003/`。

- `presentation/current-map.png`：3200×1800侧视总图。
- `presentation/current-map.svg`：可缩放图；E/M/F编号与HTML清单的mapLabel对应。
- `presentation/current-map.html`：离线完整目标/机关/残片/检查点/抓取面材质/功能域/拓扑门禁/来源SHA清单。
- `presentation/current-map.json`：与同次原生测试SHA绑定的数据；替代出生位置留在原slot，不重复算敌人。
- `source-version-status.json`：原件/展示副本、源/二进制清单、renderer和单次死亡观察的SHA及状态。

原件在original、展示副本在presentation。5对输出逐字节一致，索引11个文件逐个核对。最初v162与中间r002及明确标名的控制夹具保留，不覆盖或删除；当前以r003为修订版。

### 事实

当前正常保存父场景/五叶与内嵌小地图目录重建一致：143结构固体；76普通配置目标＝26原遭遇slot＋50固定JSON常驻；9残片，最低6件且仍需原机关条件；3处恢复配置位置。14功能域属于5个流送区，不是新增加14个小关卡。

五引擎区目标配置：入口4／下降31／盆地18／上行20／神殿3。新增50的族系为原生8／废弃12／精工10／骨骸10／幽灵10。原26保留真实场景类名，未把它们猜成新的五族枚举。10个替代出生位置属于原slot，不能加为86只。

51条相关保存节点记录＝29个逻辑机关/门/补给锚点＋18通行段＋4外观组件；后三类不能混称51个独立陷阱。165抓取材质锚点不代表165个当时可抓目标。Boss是配置召唤点(6720,1080)，不是已出生统计；ECHO是已有名册中的1个附随指导身份，无固定刷新点，独立友方身体/商人为0。控制器原点不是实际执行器接触位置或动态危险扫掠。

### 推理与局限

root查看了已留档PNG：U形空间和人口分布可读；上行部分仍明显重复短平台，不能用14域元数据冒称内容已足够丰富或已达到目标美术。总览密集处编号会靠近，完整表格/可缩放SVG用于查明身份；没有把未验证边画成已通关跳跃轨迹。HTML为离线静态清单，未做浏览器像素验收；本图不是游戏实录或游戏内小地图。

### 风险

只保证此冻结数据的静态投影与记录，不给所有AI/机关/新局继续/正常路线/帧率/美术/真人体验续签。源文件未来若变化，当前图不能继续冒称实时版本。并发读取单字节快照加固仍延期，当前没有观察到并发源写入。

## 同次探索仍未通关

未修改原居民、接近和预算的完整探索身份诊断，在Grip8 cast27到Swing→撤爪→下落后，physics9416被 `canyon.v2.resident.split-level.02 / lost-echo.tscn` 的真实Hitbox击中，HP1→0，能量1、盾未按。恢复请求1/完成0，**未到Grip9**。所以没有依据修改未取得碰撞身份的Grip9旧post或忽略合法碰撞。红点仅为本次实际击杀观察，不是新增固定陷阱。

源码核对：Grip8尾段有不变式观察，但没有Grip1专用落地盾；现有专用盾只向下并明确仅覆盖Rust，不可直接当成Ghost防御。下一需真实同组合的起手/方向/盾资格见证。另将净空左移提前到K前会与现有45°目标平台角净空模型冲突，尚未实施任何猜测修复。

## 反例、审查与修正

1. 新原生导出phase先真实失败“current map snapshot absent”，其前置实际五叶/143目录已通过；再实现同步off-tree标量投影。实例化根各自finally释放，保存资源不Dispose，无mount/AI/输入/真实存档。
2. 一次实现构建失败CS1674：泛型Godot Dictionary不是IDisposable。保留失败、没有engine；只去除不支持的using，不改GC/缓存/引擎。
3. renderer校验初始7坏输入实际误准入RED→schema/数量/身份/有限坐标/边界/族系/来源校验GREEN。缺绘制实现及缺native payload SHA均实际拒绝后才实现。新增同次文件SHA和canonical精确门，防止仅凭exit0引用后来改过的JSON。
4. 首次绘制因H被PowerShell解析为Get-History别名而失败，未发布文件；改为Encode-MapText后真实SVG/PNG组合及字面投影检查通过。
5. 唯一新审查 C0/I1/M3。路径越界I1、编号查表和非致命误标按用户影响纳入一次修正；承诺的输出/转义/摘要控制补齐。真实7控制RED中3项失败（祖先junction、查表、非致命命名）→GREEN七项全通过。junction只指向本计划自有工作区目标，未越出授权工作区，未删除资料。控制用复制文本fixture明确不是额外native运行或游戏证明。

最终使用：源/receipt/绑定文件/观察日志祖先reparse拒绝、receipt独占CreateNew；E/M/F清单列直接对应图；非致命记录标受伤而非击杀。旧文件/旧输出保持，变动payload被拒，真实表格消费的HTML元字符安全编码。少量控制PNG在明确命名的夹具目录，不作为当前地图展示。

## 原生执行记录

根：`G:/knight/验证证据/october13-stable-claw/`。下表不把renderer控制文本fixture计为native。

| case | build exit/ms | native exit/ms | 严格结果 |
|---|---:|---:|---|
| thruster-route-exploration-diagnostic-20261030-004044-432-1753fb1976a74550907c26aa91663ee5 | 0/35261 | 1/229040 | FAIL；Grip8幽灵击杀，Grip9未运行 |
| resident-map-preview-20261030-010853-596-011126a8707f411aa9f6ba5edd569c54 | 0/60150 | 1/1851 | 缺导出产物RED/FAIL |
| resident-map-preview-20261030-011245-375-7aef4474c7a74072bdbaa259dddcfa5a | 1/27136 | 未启动 | 构建FAIL，无fresh bin pair |
| resident-map-preview-20261030-011711-271-a6f1ec6d6c8c4f90a6fa21b71581eab4 | 0/43624 | 0/2076 | PASS；初始投影合同 |
| resident-map-preview-20261030-013753-945-9dee787a233a4ef2accd418181780f26 | 0/34677 | 0/1302 | PASS；最终payload绑定 |
| resident-parent-20261030-014636-522-5d268b30511a41af87ea832745733d74 | 0/32054 | 0/14099 | PASS；原同步/父/目录合同，不是全路线 |
| models-20261030-015255-892-f549369b977440a4a164f7b20daf7de8 | 0/30257 | 0/555 | PASS；仅原机械爪两组模型 |
| prologue-pre-20261030-020138-046-33e43cbadb804c01afafc6421a4356fe | 0/25918 | 0/160372 | PASS；本次三日志零ERROR/WARNING/FATAL，不关闭历史资源问题 |
| prologue-post-20261030-020716-611-3b756270666947a8afd9d066d4f874a3 | 0/40000 | 0/209536 | PASS；原75/75＋owner8，原限定WARNING 1＋1＋2 |

前段实际第二J窗口17physics/19process，原末段回归与退出drain完成。后段原75/75＋owner8及退出drain完成，原限定WARNING 1＋1＋2保留，不冒称零warning。本轮9尝试6PASS/3FAIL（含一构建FAIL未启动engine）；最终同源导出/parent/models/pre/post五专项通过，不能覆盖原完整Explore死亡或历史间歇性FATAL。

## 最终保管与媒体检查

44对现存清单相同，另1对bin清单因构建失败缺失；48/48实际封存二进制匹配。4358份现存选定实体的SHA及对应路径通过，不声称逐案全源实体备份。当前1859源/899 C#逐SHA与最后清单一致；相对v161只有2个新工具/测试C#＋RunStable变更，没有生产游戏/scene改动。最后post的10项helper（基础9＋post专用1）及Debug6bin匹配；2个离线PS另有独立SHA，不能冒称由runtime清单覆盖。8个实际新engine PID均结束，未触碰旧PID8760。

```text
source-after BD291866B7D13B801BAD51E9504FED8C0D9B37EDF12C1A9C6BB5977348DCAE3F
helpers-after EABD16BAC58C734FEC4E990A9655E7DB65FCE6D010E51248A0ECD895E3DF05AF
fresh-binaries 4BF8018C8C41E5F07EFF9B5CFCCE373570543D10CE127AFE08EE132A30BC6E00
canonical 9AB3D283ABA3C28811D3C37251E3988AC9CE61D140CEF56114B71242C10BB296
renderer BAEC2988F4654F64C041A752CBF3DA7D40B7CD1F4191EDB18C2BFD2EE3DB8799
renderer-harness 71C8BEC4006A6D79E90C7B05C1E69FD4FC3826EA971A77215AC7F6A4FC3D3950
```

HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`未变。r003 PNG与已审阅初稿同SHA `4E292D1B449B2251EA1A701C1EFCA92C09C6D8BE77E3797C7173BC0056CD3DB6`，r003的HTML编号对应和路径/标签修正已由实际消费者控制验证。

一次性媒体检查 `2026-10-29T18:11:08Z`：7,427,750 bytes/86文件（含关键图、源/索引与明确控制夹具），原基线3508，Notify=false。无新增20GiB档位，不建立自动化、不停工；没有大量游戏逐帧采集。

## 裁决与未完成门

沿用用户指定的dirty G根，手动apply_patch/ledger，不建新worktree、不commit或清理工作区；代价是本轮不是Git发布。R09是已明确的数据/注释交付，不擅自做视差新设计或付费美术。地图技能按调试预览分支使用已有原生数据，不把脚本画的结构图当最终视觉资产。创建少量关键节点图并原件/副本/索引留档，未建立自动化。

审查set-aside逐项保留：authored/alternate/affine/lifecycle正确不增修；锚点不代表执行器/动态危险，29与51含义分离；NPC名册/Boss配置不等活体；拓扑不等通行；现有全局混合标志准入仍适用；来源并发读取加固延期；概览重叠保留缩放/清单边界；独立审查不签署普通回归、游戏画面、历史资源、全路线或真人门。

完整1–11仍 ACTIVE/OPEN；没有改游戏地形/物理/战斗/数量/正式素材，没有UI/gameplay审美验收，没有付费/新采集/游戏窗口/系统输入/自动化/Git写入/删除/真实存档语义读写。旧PID8760仅知创建于10月26日，路径/命令/所有权不可读取，未触碰；不声称全机无Godot进程。
