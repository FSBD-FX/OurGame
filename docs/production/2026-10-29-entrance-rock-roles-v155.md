# v155 · 入口地面、岩顶、侧壁分工

项目仍是 `G:/knight/Knight` 的 KNIGHT Godot 工程。原十一项完整序章目标继续 ACTIVE，本轮不是正式交付，也没有正常速度画面或用户审美验收。

## 实际接入

原入口的五块厚岩不再共用“横层＋顶土”画法。现由 builder 明确赋予三种角色，保存到实际入口叶场景：

| 实际消费者 | 原矩形尺寸 | 新绘制用途 |
| --- | --- | --- |
| EntranceOuterWall | 48×720 | 纵向岩裂、不等距截层，保持实心侧壁 |
| StartFloor / BridgeFarBank | 320×84 / 880×84 | 压实土层、大块内嵌石、内部沉积阴影 |
| EntranceSkyOverhang / RiftEntryCeiling | 552×80 / 680×400 | 大尺度岩断面、内部下缘阴影，不再加地面土帽 |

全部新命令与点集只读，实际 `_Draw` 直接消费同一命令计划；没有把角色标签当成绘制完成。大形与有限裂线替代均匀横条，不使用随机碎噪点。它仍是项目原生矢量绘制样段，不是新付费位图、最终手绘场景或 Ori 品质证明。

薄 LowBridgeCeiling、其他区与旧样板继续 Legacy。143 地形实体、9 残片、76 普通敌人目标，以及碰撞、抓取材质、落脚高度、六像素 ReadableEdge、机关数值和存档规则未改变。本轮没有重做早已移除的旧阶梯，也不把该历史修改计作新增进展。

## 先生成候选，再接入保存叶

原 Build / SaveAndValidate 在独立 case storage 生成候选，并做保存前后节点数及原场景合同校验。比较旧叶与候选的完整序列化文本，仅忽略重新生成的资源/编辑器 ID 和五消费者的正确角色字段，其余内容必须一致。实际候选确有五条正确角色字段，审查又独立确认归一化内容一致。

核对当前旧叶、保留原件和候选 SHA 后，复制该**已生成、已核对的确切文件**到 `scenes/prologue/sectors/CanyonEntrance.tscn`，没有手写 TSCN 或重新盲生成。父场景仍引用同一路径。旧叶保留于第二案 `before/scenes/prologue/sectors/CanyonEntrance.tscn`。

- 旧叶 SHA256：`537C60397A65D5EE71934D4049A22B3C0AFEAC8FF8D8CA3FFF547E9AB59D9CFC`
- 接入叶 SHA256：`68B1DBBBAD3C34FFE2A0477427A4052C10213A7A971BC612D01279831A5AE578`

## 原生验证：断言通过不等于严格通过

统一证据根：`G:/knight/验证证据/october13-stable-claw`。九次构建全部 exit0，均保留合同、原生身份、wait、stdout/stderr/external、前后源/工具/二进制/存档目录清单；未放宽上限、清理失败或按随机成功补跑。

前两次预期 RED：

1. `entrance-rock-roles-20261029-170358-607-01b7db82398b4063b535ec76763af3a2`：真实 builder 侧壁 expected Wall3 / actual Legacy0；native1/768ms。另有36 ShapeRID，仍是严格失败，不隐去。
2. `entrance-rock-roles-20261029-170806-689-8afef8f417b041eb8474eb8589d63250`：builder 五角色及绘制边界通过，旧保存侧壁仍 Legacy0；native1/746ms。候选语义通过，另有48 ShapeRID。

接入后最终同版七阶段 **4 PASS / 3 FAIL**：

| 阶段 / case 后缀 | 原生结果 | 严格结果 |
| --- | --- | --- |
| entrance-rock-roles / 171138-253-c394c6489d7c4cf9a05fa887ce9739b1 | exit0 / 1284ms；10消费者、三角色、边界/只读/隔离及原143材质 builder＋packed 断言全部通过；183 ShapeRID＋3 Handle ERROR | FAIL |
| models / 171402-782-df5cdc467c674f07ae1c6aa5f7e359d0 | exit0 / 539ms，三日志无诊断 | PASS |
| canyon-structure-v2 / 171447-586-28ac5269465b48daa6352f827bd4247d | exit0 / 1385ms，三日志无诊断 | PASS |
| keyboard / 171532-048-386f218b2e684dcbbd0cae76211a1728 | exit0 / 11938ms，三日志无诊断 | PASS |
| recycling-traps / 171629-162-b6781c70f9634bb5ba9f2e4fec0fc61e | −1073741795 / 36431ms，gchandle.is_released FATAL | FAIL |
| prologue-pre / 171744-820-a4a81d6084c84959b7115b06881298db | 原180000ms上限超时；wait180324ms / exit−1，没有完整阶段收据 | FAIL |
| prologue-post / 172125-602-1048ba1d092c44caa23634cbba0146b8 | exit0 / 292441ms，原75 executable＋owner8，四条确切负控 WARNING | PASS |

post 已接近原300000ms上限，不是目标硬件性能合格。Main/Explore 本轮未跑，历史完整路线、资源错误及失败不关闭；不能用 post 的一次通过覆盖 pre 超时、机关 FATAL 或绘制专项的资源错误。候选生成和新测试路径的所有权尚无因果实验，不能将历史故障归因于这次画法，也不能声称这次未引入任何生命周期风险。

## 唯一审查与延期项

一次新上下文审查（entrance_rock_final_review_v155）：本轮生产差异 C0/I0；两项 Minor 延期，没有修复或再次审查：

- **M1 资源所有权边界**：新测试立即 Instantiate 加载的 PackedScene；候选调用现有 SaveAndValidate。释放节点不等于证明每个资源 wrapper 确定退场。现有源码观察不足以证明泄漏/FATAL 原因，不能随意 Dispose 共享缓存资源或增加 GC/强引用兜底。
- **M2 候选角色计数门**：归一化会验证出现的角色字段，却未独立强制候选必须五条；实际 builder 与接入叶的断言会事后拦截遗漏。当前候选和实际叶确有五条；未来可做候选自身的精确计数负控，本轮不把该加强项冒称完成。

审查未判断的边界继续保留：实际像素/美观/雾与 HUD 交互；正常速度视听/硬件/真人；完整双路和十一项及保存物理机关在序列化之外的正确性；历史资源根因及此次引入与否；无关 dirty 变更、旧绘制与全项目就绪；未来任意尺寸及各渲染器光栅覆盖。以上没有被“范围外”一词从总目标移除。

## 取舍、保管与后续

- 延续已批准的完整简报，由开发判断三角色与低噪绘制细节，不重复询问引擎或新立美术目标；代价是本轮实际画面仍未见、未获审美认可。
- 使用原生 Draw 而非生成栅格资产，没有付费或 asset-gen/imagegen 调用；代价是不能替代未来完整角色/场景正式美术。
- 保持严格原预算与三日志门，保管核对聚焦本轮变化与九案，不重审无关历史；代价是旧故障原因与全项目资格继续开放。
- 精确生成候选核验后接入、原件保留；文本与合同保证不等于像素保证，M2 的候选计数加强仍延期。
- 新审查使用真实旧快照＋新方法范围，而非没有本轮提交的空 HEAD..HEAD；代价是不对无关历史 dirty 内容给出全项目审查结论。

45组 case 内前后源/工具/二进制/用户目录/继续状态清单一致；当前五个变更源/叶与最终清单一致，1857源/897 C#。九个原生进程实例均已结束：第一案 PID2696 已被稍后新启动的 godot-ai.exe 重用，路径和出生时间不同，未将新进程当旧进程，更未终止它。无关旧 Godot 进程也未触碰。

最终 source manifest SHA `92175D493922791949902FA21424812A01AE436257D981C9EF33E7742AD47648`，六 binary manifest SHA `B717CD00B87FC7C8332F837BC4A5F72E603F890772E7631A489FA5680EE48A97`，canonical SHA `CC57DA29571B68671025A3E55F12DDBB6F206B3C5D5CE1539DF60673E0D8B2ED`。HEAD/index 保持原值，未提交/发布。

本轮无前台窗口、系统键鼠、过程图/视频、新付费资源、再次崩溃采集、自动化或删除。手动媒体 Check：1,431,609 bytes / 16文件 / Notify=false；不足新20GiB，不建立通知任务。正常雾/HUD下画面核对仍等用户方便，不把可选问题未回复视为批准或停工理由。

下一步先处理影响正式资格的原生资源故障与前段超时，继续区分测试环境、资源所有权与玩家问题；同时补入口视觉实测，之后再扩到整关场景。全十一项、正常 Main/Explore、正式音画动作/UI、硬件和用户游玩验收继续开放。
