# v209 · 抓到真实转盾冲突，主线仍未通过

## 事实、推理、风险

**事实：** 只在 KNIGHT 两份既有测试 CS 增加有限留存与原扫描上下文。生产玩法、数值、地图、输入序列、原900战斗/生命值/单跳/三弹/无恢复门均未改。v209是内部工作编号，不是发布版本。原十一项目标继续 ACTIVE。

**推理：** 此前最后96条日志覆盖了关键转盾瞬间。现在固定保留首次符合条件的近战接管前3条、当条、后4条原决策，抓到了 Burst17 尚未重叠却即将抵达时，驾驶转向右侧近战、被左弹击中的真实链。不是以一次局部通过代替完整主线。

**风险：** 记录有有限分配/输出成本，不保证零时序扰动；窗口单位是决策，不保证逐物理帧。当前主线仍失败，没有新增正式美术/动作/UI/声音或正式视听认可。历史前段、音频/资源、完整双路与真人/硬件质量门仍未关闭。

## 实现与独立审查

- BCL字符串窗口：滚动3条，第一次trigger后追加4条，重复trigger和后续日志不能覆盖；八组独立字面量控制先stub真实RED，再GREEN，原actions继续通过。
- 完整capsule保存原调用参数的值，赋值仍在原 `??` 惰性右侧/原fresh-J实参内部，不新增getter或空间扫描。原candidate身份/shape、当前physics/input/dt、角色速度与实际盾向一并记录。
- 原有末96条尾记录保持；新增窗口仅在已完成postHop第二K、Hop5后第一次NearContactGuard时触发，不参与决策。
- 唯一fresh whole-change review **C0/I0/M0**。特别核对了outer scan→capture之间无可达await后落入capture：有await的分支continue外循环；fresh-J先完成松盾等待，再扫描并立即记录。没有修补轮或二审。

## 实际主线结果

Main6acddb build29784ms/native135859ms，exit1，无timeout，guard=[]。原counterweight-lift900仍未清：RustHP2、playerHP1。原抛出栈经EDI保留；音频只在本案1frame/249ms通过，持久化109保护项不变，恢复0/0；不作音频修复或完整通关证明。

窗口8行pass459–466，physics6918–6925，首trigger在pass462 / **physics6921 / input6892**，不是旧v207的6922。

| 当前关键证据 | 数值/含义 |
|---|---|
| Burst17身份 | owner4899165178359 / projectile17132423220877 / leaf17132439997059 |
| 相邻真实中心 | (4601.3984375,3432.5546875) → (4608.23828125,3429.91015625) |
| 同扫描完整受击胶囊 | 中心(4635.4018555,3423.9685059)，r20，总高72，单位基；中轴Y3407.9685–3439.9685 |
| 当前clearance | 2.16357421875px；双方半径和25，不是半高36 |
| 原近战接管 | Rust8 Telegraph剩余0.13333315s，实际盾仍Left，NearContactGuard=true |
| 实际接受伤害 | **同physics6921 / 新input6893**，HP2→1，source4899165178359 / localAttack17，盾Held/Active但Right |
| 随后子弹 | Burst18当时刚出现，clearance79.15536；不是此次伤害来源 |

独立只读数核：相邻Δ=(6.83984375,−2.64453125)，dt1/60。若下一步恒速、角色静止，会入侵约4.67627px；若用真实右移0.611328125做同步线性插值，约入侵4.06494px。候选交点在胶囊轴段直边内。

上述**只是局部恒速外推，不是native TOI或弹体实际最终位置**。插值模型的5.27/5.79ms不能当Godot真实碰撞时间；生产处理顺序、碰撞截断及玩家加速度可改变分数。真实接受伤害与错误侧向的身份/时序是直接证据，不依赖这个预测。

## 下一修正需要证明什么

下一驾驶策略应区分“当前未重叠”和“下一步会抵达”，不能仅凭正clearance把左向防守让给更晚的右侧预警。需在原扫描中保留同弹相邻样本、同胶囊、dt/角色运动；首次/缺帧/身份变化明确unknown。它只能影响驾驶防守选择，不删除威胁、不放宽J时间/精力门，不把新第18弹或Rust漏掉。

修复验收必须实际证明：Left对17正常零伤处理 → 随后真实Right在Rust Active前生效 → 后续18仍记账 → 原HP/输入/K/单跳/三弹/原900与完整主线通过。当前只完成定位，尚未实施这一策略，不借约8ticks预警推定它必定成功。

## 本轮最终验证

全程SourceRows1872、原cap、source/helpers冻结，隔离headless/内部Godot键盘事件；无ExpectedFailure/fixedFPS/movie。

| 案尾码 | 阶段 | build ms | native ms | 严格结果 |
|---|---|---:|---:|---|
|3c7d119e|stub actions|28037|532|具名真实RED，7失败控制|
|a8038641|最终actions|27349|9599|8窗口+8下降+11finalizer+原actions PASS|
|6acddb47|Main|29784|135859|原900 FAIL；8窗口完整，非timeout|
|3ddaaf9d|keyboard|27610|20358|PASS；独立headless title8203亦PASS|
|0dce8d41|远程混合策略|26928|3767|PASS|
|22a77c58|爪模型子集|27886|506|PASS，不称全游戏模型|
|fe1e9b83|完整pre|27865|169502|原180000内PASS；下降mask0/HP5/0恢复|
|15083e37|完整post|24336|210892|原300000内PASS|

唯一审查只签两文件静态改动，不签实际Main、原生前后段或正式质量。原始失败/基线全部保留。

当前pre余10498ms，不能由此认证性能稳定，或清除旧Handle/终态/超时失败。当前Explore未重跑，不续贴旧版探索资格。

8案共6限定PASS/1真实RED/1MainFAIL；最终7案同源同bin为6PASS/1MainFAIL，**不是全套通过**。原actions/keyboard/策略/爪模型子集/pre/post均通过；Main未清。所有build0警告/错误，guard=[]，原caps不变。post75有序同名/index enter→return、owner8/nodes1→1、finalizer/drain/phase齐；仅原4限定负控WARNING，无ERROR/FATAL。不将它用作历史资源/音频故障修复证据。

## 裁定与审查未验范围

1. 保留dirty checkout和旧失败，不Git/worktree/commit/删除，以manual ledger记录；代价是无clean-release资格。
2. 在既有持续批准范围内继续inline/headless，不新增前台、付费、自动化、真实存档或发布权限；代价是本轮不构成正常显示/人工认可。
3. 用原序列化行做有限留存，不新增扫描/原生观察器/预测策略；代价是非零日志开销、窗口外证据仍有限。
4. 静态审查不替代native/保管；分别实跑和核账，代价是C0/I0不等于全绿。
5. 决策数不当物理帧数；当前恰有连续physics仍逐身份检查，代价是缺样本时必须unknown，不能填造速度。
6. 仲裁/TOI/防守改动不混入诊断；代价是当前Main仍FAIL，必须另经测试和实际路线验证。
7. 旧footer `fresh_capsule_unchanged_null=true` 早于两基线，仍属既有误导文字；本轮以明确capsule JSON为证，代价是单独读取旧footer仍有误解风险。
8. 无关dirty、原pre/资源/Audio、正式美术动作UI声音、普通显示硬件真人、完整双路与原十一项未认证；代价是不能交付或保证付费人数。新增延期Minor0，旧延期不清除。

## 最终保管与容量

独立只读审计累计8案40清单对、3868份选中源副本、90份helper副本、48份bin，逐SHA一致。当前1872源、post专属10helpers、6bin与最后post零差异；最终7案同源同bin。2026-11-03 00:15:15 +08按保存creation/path复核17owned（8build＋8engine＋1title）全部结束，旧PID当时均不存在，已知KNIGHT工具进程0，无进程控制。

sourceManifest `3E363AF60C4EEC9EC4F957D839378902CC027EFA5D1476B008A658DD1FE5B1B2`；binaryManifest `78D571850457686155E8BB066445126936A27E5BDD4D74380D46C0B5EAF5FAC4`；DLL `04EDD9D7FBD2F47DABF3C632754463EE7831CBC3018EFF9ECECD5BCD3D3A66BC`；postHelpers `C5EF45802948AA866E2659414BDBE4BD8D22B0D980D459CAA292638C213429E7`。

Main `56B09932FE2AFB3BF111B9F286C2BAAC864BB64F438999D1DC32E18ED497295B`，Actions `3D72845F5EB2BD706B60D3491343BA9EB596124BF843DCF089B792D47063C512`。Descent08D917BD、Teardown26EF0C40、canonical7A79C29A保持原值。HEAD `e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、index `21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`和原branch不变；只读diff-check无空白错误，只有LF→CRLF规范化提示。

媒体工作内手动检查UTC2026-11-02T16:13:16为7,427,750bytes/86files，Notify=false/NewThresholds0；不新增自动化、不停工。本轮未保存普通调试图/视频，原始日志和失败均在 `G:/knight/验证证据/october13-stable-claw` 各具名案保留。无新付费、GUI/前台/OS输入、额外崩溃采集、运行时更换、真实存档写入、Git发布或资料删除。
