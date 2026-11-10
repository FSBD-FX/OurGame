# v149 · 屏幕边缘预警保护，整套资源验收仍失败

2026-10-28，工程 G:/knight/Knight。原十一项目标继续 ACTIVE/NOTSEEN；本轮不是正式美术交付或通关版本。遵守用户“暂不方便，继续无窗口工作”：没有可见游戏窗口、系统键鼠、付费生成、自动化、Git写入、删除或真实存档修改。

## 事实、推理、风险

- **事实：** 原 FogReadabilityOverlay 按攻击叶节点原点是否在屏内筛选。当原点为(144,64)、实际矩形覆盖 x124..164、逻辑视口128宽时，旧实现实际提交0，独立原生断言要求1，明确RED。
- **实施：** 仅将上述视口谓词换为当前权威攻击形状的保守包围盒与逻辑视口相交。复用 AttackShapeBounds.WorldBounds，四角经 GetCanvasTransform 投影；凸多边形仍是世界点，短胶囊仍按真实半径，有限值/溢出失败关闭。真正绘制仍是原形状，未放大伤害。源可见/活性、同viewport/world、620范围、阶段及原点LOS保持。
- **推理边界：** 原点屏外不等于攻击形状屏外。这是独立已复现的筛选缺陷，不是 v148 Grip8 失控残响死亡的已证原因；也不表示原先幽灵预警没接线。
- **风险：** 形状AABB及再投影保守多选会多提交少量被裁切命令。仅headless命令已验，没有GPU像素、轮廓描边薄片、正常速度、声音、硬件性能或真人观感证明。完整路线、正式图声动作/UI与历史资源问题仍OPEN。

## 已改范围

仅三个现有C#变化：

1. scripts/presentation/fog/FogReadabilityOverlay.cs：唯一生产修改，约束如上。
2. tests/integration/AttackTelegraphRuntimeTests.cs：独立 RunFogEdgeCommands，真实SubViewport/叶/overlay/native遮挡快照；不混入原trap委托链。
3. tests/RunStableClawPullTests.cs：仅新独立phase，原trap委托和退出drain位置/次数不变。

canonical verifier 增加精确 fog-edge-commands、拒绝9类大小写/混合参数负例、在一般fog可见分支前显式--headless、严格3sink及原始文件副本。没有改AI、伤害、能量、地图、存档schema、敌人之间角度优先；1857运行文件/897C#、143固体/9残片/76普通敌人不变。

夹具覆盖四边、矩形/地面带/椭圆/胶囊/世界多边形、短胶囊Y<X、无关源局部变换、逻辑缩放/平移/剪切、全屏外、None/Recovery、距离、隐藏、暂停、不完整快照、不同视口、真实墙、世界范围溢出和queued源/消费者。它不等于自然幽灵攻击与伤害时刻的实际像素证据。

## 本轮全部尝试

案例均在 G:/knight/验证证据/october13-stable-claw/，表中短ID可唯一定位对应目录；没有删失败案。

| 案例/phase | 结果 | 依据 |
| --- | --- | --- |
| 5d20c72ab33d47278d176f2df9be6ac5 edge | build FAIL | CS0117 SubViewport无OwnWorld2D；未启动native，不算行为RED |
| e26696c55b154dd6bfad2f30b1230365 edge | native RED | FOG_EDGE_ORIGIN_CULL expected1/actual0，exit1，原失败drain完成 |
| de1343b7049c4b35b8244ae71851da87 edge | basic PASS | 最小生产修复，3sink干净 |
| 6b901862eb844814ba468b7f659b0bac edge | strict FAIL | 行为全部过，退出ObjectDB1、Area2D1/Space2D1泄漏 |
| 1dd39e47a16f4a9480c30ec43e22cca1 edge | strict FAIL | 改负例挂载位置仍同样泄漏，否定较窄猜测 |
| de1be19fd52c4de7a2fe6224f403c22a edge | PASS | 显式自有测试World2D在两个测试viewport后释放；3sink干净，506ms |
| 28ee53ca83f6459ba47e409d900319ea models | PASS | 原普通模型 |
| 73d2423662d0466bbfdef9afd85bfecf keyboard | PASS | 原键盘门禁 |
| 5b64504280dd44c4b64c02c4efafee6f traps | PASS | 不变原机关委托链，27460ms；不能关闭v148 FATAL |
| 049a7fb516b44ffe8e3f1f24d4566d76 pre | strict FAIL | exit0/151751ms且有结束marker，但stderr和external各13次Handle is not initialized ERROR |
| d86eb6fac0dd42dfa17b68a17e900871 post | PASS | 199326ms，原75执行/0retired/75classified与owner8；原4精确限定warning |
| b219d172828f4339ba2d5f9c3118c3e9 normal-absence | PASS | 普通程序集无身份诊断类型 |
| a3adbbf9cc9b4201b2b69e688e8b973e final edge | PASS | 最终helper补原始副本范围后一次526ms，无新玩法编辑；3sink均0诊断 |

合计13个有界案例：1构建失败、12native；严格8PASS/5FAIL。另有一次canonical首行引号失误在PowerShell解析阶段被拒绝，未创建案例/启动native，已改正。原六项普通回归是5PASS/1FAIL，不把循环最终退出0冒称六项全绿。

pre错误的实际入口之一为 GCHandle.FromIntPtr→ScriptManagerBridge.SwapGCHandleForType→Node.AddChild→BasinSupplyRuntimeTests.OpenSample:463（NewGameDuringHealing）。没有对照证明其因果，不将此生产改动排除为不可能贡献，也不猜资源损坏、统一根因或加保活/GC。v148 Main死亡、ExploreGrip8死亡、本轮未重新完整跑；继续明确未通过，不能拼接旧片段通关。

## 唯一独立审查与取舍

fog_edge_final_review_v149，fresh gpt-6-astra/high，C0/I0/M1，已结束；没有C/I修复或二审。

- **V149-M1延期：** 当前正确拒绝投影后坐标/尺寸溢出，但夹具只明确测世界bounds溢出；extent1e38、canvas scale2会有有限角点±2e38而尺寸溢出的额外原生负控尚缺。后续测试加固，不冒称本轮已测。
- 保留指定脏KNIGHT分支、无新worktree/commit/delete：避免改用户工作，代价是依赖精确清单而非隔离合并边界。
- 直接执行已批准可读性修复、不再次反复询问：无新增玩法/审美定稿，代价是没有新口味确认。
- 陷阱身份诊断另立后续小任务，不在本轮同时改释放观察链；代价是v148原生fatal原因未定。
- 复用现有世界AABB再投影而不重复数学；代价是保守额外绘制，不改判定。
- GPU/描边/真人验收不判；原整程失败不判原因；本轮pre严格FAIL且不归因。这些是保留缺口，不是关闭问题。
- 早期case仅完整1857 hash manifest，selected文件副本没包含本次overlay和AttackTelegraphRuntimeTests；不补造旧原稿。最终phase扩选7个相关文件，final case同时保留两侧实际源码。精确历史全内容diff仍缺旧文件副本，代价为审查/保管证据有此边界。

## 固定身份与保管

最终代码三文件hash：

    FogReadabilityOverlay.cs BEF93B4133222C93F850BD9AF0E365D9011C7B25DF12A4385F66F2EEAE3DEC2B
    AttackTelegraphRuntimeTests.cs 5F03690287FA59A305AF34B9508E35D0993D4BA0DADE9395FE2304910275CFB5
    RunStableClawPullTests.cs 55D2CE3214A742B8D58712D0451C6DED8A592E9A00BA2CB05B0BF333CCF541D8
    source1857 A1F04FDC0E316E599ACF48419E396797C431233B263F6058FEEE4A0F88C9F9D8
    normal-binaries C717075226151A11AE08F1A35F9E173A636179CD4BE1234776B8E3CB75E5B6C1
    canonical 6A7ED7DD79C7A358489F747635CFC0DDAF9D518D8E2A4BE6B2641F439F150D0C

root最终a3ad独立核验：5清单对一致、448选定源码副本+10helper副本+6二进制符合各自清单、全部当前1857运行路径/897C#相符；build与engine两个PID+CreationTime均终结。

只读 fog_edge_custody_v149 已结束：此前12案59清单对逐字节相等（48源/用户/helper/continuation与11二进制对），5396源码副本+124helper副本+66二进制符合各自清单，0不符/未列名；23合同/native/wait PID匹配，23确切PID查询均不存在、0复用/0同身份存活/0超时。早期两个指定源码缺48个side/file副本，只有manifest hash，未补造；1CS0117构建失败、1真实RED、2严格泄漏FAIL及pre13错误均保留。

合计13案64清单对/5844源码副本+134helper副本/72二进制有界一致，25个自有进程身份终结；不是全机空闲或全部资源已释放。当前独立枚举1857/897与最终manifest完全一致，HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6/index21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C均未动。文档更新后root再次核验全部1857运行路径、6二进制及canonical不漂移，最终9参数负控再次拒绝/无native。

媒体仍1431609 bytes/16文件，新基线3508、增长1428101，2026-10-27T16:18:26Z手动-Check成功/Notify=false；没有新图稿/视频，没有重置基线或建立自动化。

## 下一步

优先在不变原测试/帧限内补陷阱fatal身份与pre实际AddChild/配置生命周期因果；不刷绿、不保活、不加GC/时限。之后续做真实Main/Explore失败段。F01仅局部修复，原十一项、正式美术/动作/UI音频/完整路线/硬件真人与交付都不勾整项；用户方便时再明确申请实际画面核对。
