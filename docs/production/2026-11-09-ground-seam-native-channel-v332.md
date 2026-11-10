# v332 · 原生落脚与实际视觉发布双通道

状态：新增被动测试通道已在同版正常Main和Explore执行，最终原11指定回归10PASS/1FAIL；原完整十一项ACTIVE，整版尚未验收。只在`G:/knight/Knight`无窗口工作，无新游戏美术、生产动作/物理/地图/AI参数变化。

## 事实、推理、风险

v331的Main在450→454之间只有两次真实视觉发布。旧门要求相邻物理tick，因而严格失败；不能将缺帧认作已证滑脚，也不能用ProcessFrame的新root乘旧pose补造缺失视觉。

本轮保留原发布来源、tick+1、同脚stance、.15px、固定骨长及身份等全部门；仅新增挂在真实Motor之后的测试观察器。256条标量环记录完成step的motor/revision/tick、原生upward contact/body/shape/index/point/normal及无效原因。资源只局部借用，不Dispose，不写输入/位置/速度/HP或主动Present。物理完整性与实际视觉分别记账，缺发布仍是缺发布。

## 已运行的TDD与正常Main

证据根`G:/knight/验证证据/october13-stable-claw/`，不同源码不拼绿：

- `models-20261109-001316-558-326dec14ac084bf9a15fe264751834c7`：真实功能RED、535ms/exit1，五个字面量物理step尚不能输出451→452交接；不是编译失败。
- `models-20261109-001512-534-872a27be1bb4452b8cc86dffd478c074`：读器实现后505ms通过，一正控/16坏数据拒绝；是中间源码，未计当前最终套件。
- `models-20261109-002033-742-1c8f4fb08b0840eb884f19d4995cb030`：当前源码518ms通过；revision=0合法，缺tick/重复/失效/错owner/revision/shape/位移/缺handoff/容量等均拒绝。控制不是native场景。
- `thruster-route-main-diagnostic-20261109-002209-233-ace538e97cdc4437a40277e4e31e330a`：当前正常Main严格PASS250724/600000ms，stderr0/ERROR0/WARNING0；同Player标题→Boss→有支撑Completed出口、普通跳21/21接地、恢复0/0、一次完成、held60位置/body/save保持。实际save/terminal/held SHA`0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517`。

这次视觉453→454/publication727→728恰好相邻；新native通道亦为实际453→454两条记录，motor48268051986/revision2、两个真实body/shape、upward1/native index0，回调及权限故障false。PAIR/STEP/OK四行在stdout/external一致。它证明新接线实际工作，**不证明v331稀疏gap本轮重现、补全或根治**。

## 当前冻结回归

当前1924源清单SHA`96425469A73380E3658D9BD4BAE3F0C31F26F16E384CD695E6413B3FC575682B`，六产物清单SHA`C9D9185F72AF73BF2E113B525F6E7906130B5593387061CFBD066AF00B42F3FB`。四个修改文件和封存v331基线见[审查包](../../.superpowers/sdd/2026-11-09-ground-seam-native-channel/review-package.md)。没有在运行批次中改源，也没有同版重试筛绿。

最终11指定阶段如下，独立审计2873快照对差异0、66份实际binary复制匹配；不计入早期RED/中间源码。保存独立cold另1589ms、两个不同native进程及6只读witnesses成立，两款gear各四项坏donor控制/精确恢复通过。旧127→143实际Continue仍false，不能扩大兼容结论。

| 阶段 | 原生耗时ms | 严格结果 |
| --- | ---: | --- |
| models | 518 | PASS |
| Main | 250724 | PASS |
| rig | 38507 | PASS |
| presentation | 18449 | PASS |
| gait | 29774 | PASS |
| recovery | 16060 | PASS |
| actions | 10776 | PASS |
| save | 11823 | PASS，另有独立cold |
| pre | 160264 | FAIL：3条Handle错误，非FATAL |
| post | 190223 | PASS：75项/原4条限定负例WARNING |
| Explore | 321430 | PASS |

pre003627-b92f2ddd虽engine0、44/44用例返回、160264/180000ms，仍因3条`Handle is not initialized`/GCHandle/SwapGCHandleForType ERROR严格FAIL；stderr16861字节，非FATAL终止。不能因计数和退出码签收稳定性。post003947-a03826c0完成10suites/75项、190223/300000ms，ERROR0；stderr23551字节来自原四条有限作用域的故意负例WARNING，已逐项核准。两线cap600000ms，其余原cap55000ms；均无超时或guard失败，不覆盖pre失败。

Explore004330-65b26146完整JSON严格通过、stderr0/ERROR0/WARNING0；同Player从标题至Boss死亡/实体出口，激活11/击败10、恢复0/0、普通跳33/33接地、一次完成/held60位置body稳定。实际save/terminal/held SHA`24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24`一致。原视觉452→453、publication736→738；新增native恰2STEP，两个真实shape/body及同motor/revision2、无权限/回调错误、complete1。**仍非实际稀疏physics缺口补全证明**。自然Arc另有一次原阀门关闭后的正常通行：约.541666→.583333/physics5933→5941、HP1→1/Arc伤害0、未隔离战斗或改输入。

所有拥有的进程已终结，未重启筛绿；同一9642/C9D9的11案after与当前重新枚举源集、六实际产物、109行保护清单（104个实际文件）一致，16个唯一工具/4review pins及父D4AE/BasinA038未变。最终审计2026-11-08T16:52:32Z通过，当前KNIGHT Godot进程0；[完整身份结果](../../.superpowers/sdd/2026-11-09-ground-seam-native-channel/current-identity-audit-v001.json)分开记录规范化字符串SHA和封存清单文件SHA。v331候选FATAL/metadata RID等专项未在本轮重跑，也不因此关闭。

## 一次审查及延期

唯一fresh全增量审查C0/I0/M1；四pin及基线、原门未松、借用所有权、原异常优先、关闭/逐项清理通过静态审查。本轮不追加第二审或Minor修补。

M332-1延期：字面量控制直接验证读器，尚未覆盖真实环提取wrap/覆写及capture从default/暂停/多owner转Valid=false的全控制矩阵。当前静态路径和原生happy path已查，不将其扩大为全异常native覆盖。所有旧Minor保留。

v331两FATAL、两RID原因、旧稀疏发布、原十一项/正式动作美术/正常声画/硬件真人/发布、无关dirtytree及引擎分配失败矩阵均不由此审查签收。完整裁决与代价在[ledger](../../.superpowers/sdd/2026-11-09-ground-seam-native-channel/progress.md)。

## 下一项真正玩法入口

只读检查发现ground wanderer协调候选：同区域、LOS=true但水平距离>360时，战斗模型Idle，而巡逻层仍Engaged/方向0，可能停止自主巡逻。未在原生保存布局复现，未修改AI、数量或360参数。下一先用真实Player和原作者敌人位置验证，再选协调修补；保留已起手攻击在仅退远时完成的旧合同，以及离域/遮挡的取消规则。

另校准E02：普通脱战可直接恢复巡逻；仅实际越界才进入Returning。两条都需对应真实输入证据，不强切AI状态，也不把Patrolling误报为已回home。24子型仍是开发占位绑定，正式五族外观/骨骸10/幽灵10及全76组合不缩减。

本轮无新增过程媒体；near-final仅一次20GiB流程检查在2026-11-08T16:52:33Z执行：18,882,711字节/182文件，增长18,879,203字节、未到20GiB，Notify=false。不建自动化/打包/删除。完整严格门仍有pre失败，原目标继续ACTIVE，不能认定完整序章或本小计划完成；下一优先真实Player/作者原位的远距巡逻候选原生RED，而不是重跑这11案或把诊断冒称新美术。
