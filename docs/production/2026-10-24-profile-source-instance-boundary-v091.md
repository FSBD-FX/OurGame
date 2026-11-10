# v091 · 配置来源与实例字段边界

2026-10-24，KNIGHT / `G:/knight/Knight`。这是开发诊断续记，不是发行版本，也不关闭原十一项中的任何整项。目标仍 ACTIVE；76只普通敌人、Boss/NPC、机关、正式视听、正常主线和探索、相机、真人硬件与交付要求全部保留。用户纠正“没看到窗口”继续有效，本轮没有前台游戏窗口或系统输入。

## 事实

前一轮仅核对“没看到”的回执，没有制作进展；本轮重新核实脏工作区、分支、HEAD及index，继续同一工程，无Git写入。

四个C#文件变化：`ResidentSceneBinding.cs`、`RunPrologueTests.cs`及两个既有diagnostics文件。新增观察仅在REF编译生效：同一已加载scrap-scatter场景的Profile值、原Instantiate中原准入表达式已经读取的Profile；上限512，只保留标量，不增加游戏序列Load/Instantiate/GC/drain、不回填配置、不Dispose借入的场景/SceneState/Profile。普通Profile表达式和准入保持。没有玩家物理、AI、HP/能量、地图、正式美术或声音变化。

来源读取、实例对照和日志失败隔离已经原生反例验证。完整REF后半段却在原180秒上限超时：11组来源/实例均同native身份，END缺失，另有1条Handle错误，严格FAIL。不是完整75案例通过，也不能把最后正在运行的case当成超时原因。

恢复普通构建后，后半段再次出现同一个具体失败：`canyon.v2.resident.low-ceiling-bridge.03`，root是身体、绑定存在且数量1，但Profile为空；原状态字段因短路未求值。HUD停留Title，原Playing断言失败；原始详情已在清理前写出。随后发生`gchandle.is_released()`原生FATAL，二者因果尚未证明。

## 推理与风险

保存文件含Profile不等于当次实例字段已赋值。观察运行中的11次正常对照，也不等于普通运行的空值已修复。GetState/Variant包装访问、REF日志和分配记录会扰动时序，不能宣称零观察效应。限定前缀只读审查未找到对目标Profile或叶场景的可达显式误Dispose，但这不是整个资源图安全证明。

下一步采用更低干扰的导出赋值观察：本地PDB生成代码显示先`VariantUtils.ConvertTo<ResidentSubtypeProfile>(value)`再赋给Profile属性。可在独立诊断中记录setter次数、是否null、CLR类型和managed hash，然后与原字段读值配对，不先访问来源Variant。这尚未实施；未调用setter仍不能区分缺失导出、分派失败或转换提前失败，null托管值也不直接证明原Variant是Nil。不能用默认配置、资源缓存、强制GC或放宽准入掩盖失败。

## 原生结果与证据

所有目录位于 `G:/knight/验证证据/october13-stable-claw/`；下表短名唯一对应原目录中的时间段。每案各自冻结，不把TDD中间版本拼成统一验收。

| 案件时间／phase | native exit／ms | 严格结果 |
|---|---:|---|
| 120041 REF identity-contract | 1 / 534 | 来源观察断言RED |
| 120422 REF identity-contract | 0 / 707 | 独立真实PackedScene控制GREEN，仅诊断 |
| 121614 REF identity-contract | 1 / 621 | 生命周期日志失败隔离RED |
| 121958 REF identity-contract | 未启动 | 编译CS1061，误用slot.Id；终态后改为StableId，不是行为RED |
| 122055 REF identity-contract | 1 / 640 | DeclareMainManagedThread仍在保护外，实际RED |
| 122208 REF identity-contract | 0 / 845 | 同控制及生命周期隔离GREEN，仅诊断 |
| 122442 REF prologue-post | -1 / 180103 | TIMEOUT、Handle1、22 enter/21 return、END0，FAIL |
| 123222 ordinary resident-scene-binding | 0 / 8423 | 24挂载/7负例/3诊断返回，但stdout130 ShapeRID，FAIL |
| 123530 REF identity-contract | 1 / 752 | 完整END写出后Flush失败的实际成功路径RED |
| 123732 REF identity-contract | 0 / 936 | END成功状态必须包含Flush结果，GREEN，仅诊断 |
| 123952 ordinary prologue-pre | 0 / 166301 | phase OK但Handle3，FAIL |
| 124317 ordinary prologue-post | -1073741795 / 162379 | 上述ProfileMissing/HUD Title、registry0/0、FATAL，非超时，FAIL |
| 124635 ordinary models | 0 / 532 | PASS |
| 124936 ordinary models | 0 / 538 | 最终普通构建PASS，不覆盖其他失败 |

14次启动：1次构建失败、13次实际原生；原生5严格PASS、8严格FAIL。4条反例断言、4条Handle、1条FATAL及stdout1条ShapeRID，共10个ERROR头；external镜像不重复计原因。REF post和普通post各两条原qualified WARNING不当作新增故障。只有REF post超时。构建失败保留1个binary guard，其他guards0。

最终模型目录：`models-20261024-124936-901-7e41c53955824258abbacdb03bc46d07`。最终普通pre/post与模型的4个C#内容相同；最后只收紧REF回执consumer并补纯测试，再用最终models绑定当前helper哈希，不冒称所有历史case为同一helper冻结。

## 审查与一次修正

唯一fresh review：Critical0 / Important2 / Minor0，root同意两项完整性问题，未复审。

1. 回执曾误收缺身份/错误目标/关系/截断前缀。实际canonical函数反例RED→49；数组路径反例RED→53；同native但不同managed wrapper的合法记录曾被误拒，独立RED后改正。最终54例通过，严格校验字段类型、非零身份、目标路径、slot、关联与全部前缀，不把native身份和managed包装身份混为一谈。
2. END可能写出`faults=0`之后Flush失败。原生RED123530→GREEN123732：End返回真实成功状态，runner成功路径拒绝false；原失败路径忽略该返回值以保留原异常，不重发干净END。

纯回归最终54/96/26/33通过。另用保留的11组真实行做内存schema回放：原不完整日志被拒绝；仅在内存补合成END后验证schema兼容。合成END从未写回证据，不是原生完成证明。全套仍FAIL，因此不运行task-done/合并/工作区清理，不称重要问题修正后整套已绿。

审查Declined逐项裁决（完整理由/成本亦记ledger）：

1. W/preview排名未改，不作新认证；成本：Main仍失败。
2. LOS/receiver未改，保原物理准入；成本：目标选择验收仍未闭合。
3. Intro/Playing拒绝保留，不推断启动已修；成本：准备拒绝可能持续。
4. callback/deferred未改，保身份/姿态/序列要求；成本：完整生命周期仍未验收。
5. 旧LoadLevel/root.Free异常语义不混入本次observer；成本：旧清理故障仍可能存在。
6. 旧SceneWalk using SceneState不在本次前缀，不混修；成本：该独立问题待审。
7. 11对即时身份不证明图寿命/FATAL原因；成本：继续归因，不能交付。
8. REF post超时严格失败，不延长cap或归罪最后case；成本：完整身份追踪未证。
9. review不替代普通双路/全十一项/视听/硬件真人/发行；成本：目前没有完整试玩交付。
10. 旧admission helper无独立源副本，仅审当前；两diagnostics逆向移除新增块后哈希匹配v090；成本：不声称旧helper字节归属已证明。

沿用用户选择的脏checkout，不Git写入或删除工作区；成本是尚无新提交式检查点，不能标成发行存档。无新增Minor；此前四族Minor（贡献/观察完整性、旧Title/Supply/replay清理、Frozen日志刷新保护、等比缩放独立用例）继续保留，不把资源失败降为Minor。

## 保管、资源与完整目标

69对已生成manifest均一致，78个各案OWN sealed binaries均匹配。唯一未生成binary pair属于上述编译失败，无native。当前1776源/892C#、9帮助器、6普通二进制，live缺失/漂移/额外二进制均0；相对v090恰4个C#变化。

- Source manifest SHA256：`6C3C457CF17220DA650797A74FD8A12EF750BB5FECEDAD1AA2FD59EB7C5A07D0`
- Binary manifest SHA256：`9726AFB6A0BD189977EABBD5EF7F4198D16E5FFF76B9D0885B4C8C9232492F83`
- Knight.dll：`4A44B9D5178813C7A08B68BB356571213B59D234B3B557B25B5D29B3E25F148D`
- index：`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`；branch/HEAD未变。

git diff --check退出0，169条既有LF/CRLF提示，无其他输出。所有root构建/原生owner已终态。真实存档只核哈希，不读语义或改写。

无新正式美术/音频/地图/过程媒体，资源表无新增正式项。媒体只手动检查一次：UTC04:52:12.1557104，860284字节，基线3508，增长856776，Notify=false；未建立自动化，也没有打包/删除/付费/开窗/OS输入。完整原十一项、正常Main/Explore、齿轮战斗/冷却、相机、正式资源及正常速度视听、硬件真人和发行继续OPEN；当前目标工具仍ACTIVE。
