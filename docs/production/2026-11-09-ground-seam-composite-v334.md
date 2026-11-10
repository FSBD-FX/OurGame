# v334 · 完整物理接触与真实姿态发布联合验收

状态：用户批准的取证规则已接线，最终同版原11回归阶段全部严格PASS，含当前完整Main/Explore、前44/后75和保存独立cold。原完整十一项制作目标ACTIVE；这是测试证据校准，不是生产动画/物理/美术提升，也不是十一项制作全部完成。

## 事实、推理、风险

旧v333Explore真实P632/tick453→P633/tick455，而同次native记录453左面、454右面、455右面；两个P本身相邻、far站脚两端近乎不变。旧门在计算鞋底前先要求physics相邻而失败。原正式协调器只在_Process/少量零时间刷新中应用rig后发布，没有逐Motor通知队列；gait按tick差处理。因此“没有独立P454”不是已证生产通知丢失，更不是已证滑脚。

用户明确选择：完整native逐步真实接触 AND 相邻实际发布姿态同站脚≤原.15。只改测试门，保留固定骨长/真实Draw目标、角色/世界/shape身份、原输入/物理/地形/敌群/资源与原严格日志/cap。没有生成中间姿态，没有更改生产Present时机，没有将v333旧FAIL翻绿。

.15是原检查的Godot世界坐标阈值，不是这次测得的屏幕像素。联合证据只证明真实发布端点与完整接触，不证明未发布中间姿态、任意完整stance episode、GPU画面、60FPS或真人观感。

## 实现与反例

新Handoff值型读器要求：连续publication序号、有效非倒退时钟、两端Walking/原Motor与revision、两端与native root一致，原物理读器核每步完整/有效/单次原shape交接/水平运动；最后用原MeasurePair核同一站脚/.15。没有共同站脚或任何证据缺失都失败，不能用默认0当安全。

现在保留每个实际FramePublished作为前驱，包括同physics tick重复P；旧普通步行/左右面最小样本计数仍按不同physics tick，重复P不充数。原NativeTraceComplete、回调/权限失效与逐项清理继续执行。所有新policy回执明确是`adjacent-publications-complete-native-v1`，没有冒称旧规则未变。

原件根`G:/knight/验证证据/october13-stable-claw/`：

- models023042-ee459877：构建0、原生628ms/exit1，旧16物理负控先执行，新联合正控命中缺实现功能RED；不是编译失败。
- models023306-268162db：最小实现后严格PASS504ms，2正控/17负控/旧16物理负控；缺中间步、漏P、错owner/revision/root、倒退/缺失时间、无共同stance及真实4px突跳均拒绝，stderr0。当前源1926/9909、六产物2755；RED属不同源码2C5C/B042，不混绿。
- Explore023409-d1c14f51：严格PASS330638/600000ms，无超时、guards0/stderr0/ERROR0/WARNING0；本轮实际换面454→455、P575→576/native2STEP、同Motor/revision2、commonstance1、鞋底差.00013647876，回执两sink一致且源地面仍在。**本次恰物理相邻，不称新的稀疏gap重现**；跨两tick目前由字面控制验证，旧v333真实稀疏记录保留为不同版本证据。

Explore最终JSON核实标题开始、进程内同Player至Boss/支撑Completed，遭遇激活11/击败10、恢复0/0、33普通跳全接地、unsupported8、一次完成事件/次数；held60位置/body保持、实际save与terminal/held SHA一致`24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24`。自然Arc一次HP1→1/命中0、原阀门已关闭，无战斗隔离或输入改动。仅原路线镜头预算/局部回执，不是正常显示验收。

保存独立cold1598ms、writer7660/reader44500两原生进程、6只读witnesses；straight/Arc各4坏输入及精确恢复回执齐全。原55秒小阶段cap、原Main/Explore600秒、pre180秒/post300秒及严格日志门均保留。

最终原11回归阶段（不是制作待办完成数）：

| 阶段 | native耗时ms | 严格结果 |
| --- | ---: | --- |
| models | 504 | PASS |
| Explore | 330638 | PASS |
| rig | 38384 | PASS |
| presentation | 18487 | PASS |
| gait | 29829 | PASS |
| recovery | 16710 | PASS |
| actions | 10867 | PASS |
| save | 12654 | PASS，另cold1598 |
| Main | 254101 | PASS |
| pre | 149888 | PASS，44项 |
| post | 193542 | PASS，75项/原四限定负例WARNING |

当前Main完整JSON亦确认标题→同Player→Boss/支撑Completed、8/8遭遇、恢复0/0、21普通跳全grounded/unsupported2、一次完成事件与次数、held60位置/body/实际save保持；SHA`0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517`与terminal/held一致。joint实际453→454/P251→252/native2STEP/同motor/revision2/commonstance1/sole0，无权限或回调故障；时钟trail本次82条，不称128满环，亦非稀疏gap复现。

pre44 enter/return/executed完整、stderr0；post10suites/75executed/classified/retired0，ERROR0，stderr23551字节仅原四限定WARNING（观察者故障1、根类型1、拒绝Completed→Recovering2）。旧新巡逻6ShapeRID专项本轮未重跑，仍OPEN，不能用这11项代签资源根因。

独立原件审计最终2878快照对0差异/66实际binary复制匹配，RED不同版本不混入。一次最终当前身份核对19:01:23.8308561Z通过：实际重新枚举1926源/6产物与11案after一致、16唯一helper/5review pins、109保护行内104实际文件/BDDA、父D4AE/BasinA038保持，KNIGHT native0，G空闲76,993,736,704字节。[完整身份收据](../../.superpowers/sdd/2026-11-09-ground-seam-composite/current-identity-audit-v001.json)分开记录清单文件SHA与当前规范化文本SHA。与v333全部源行比较仅3个测试CS改变＋1个测试partial新增，canonical只加必需输入行；没有生产或资产变化。

本轮无新增过程媒体。near-final唯一20GiB流程检查19:01:25.3577949Z：18,882,711字节/182文件，增长18,879,203、Notify=false/档位0。没有重建定时自动化、打包或删除。Task1仅本联合取证组件已完成；完整原十一项/正式美术动作、正常AV/硬件真人、原生资源原因及旧Minor仍OPEN/ACTIVE。下一实际战斗入口是E08：首次完整攻击逐帧几何与真实键盘退避负控，先查近战67..70不可触及带的反复空挥，不扩伤害形或让预警必中。

## 一次审查与边界

一次fresh五文件审查C0/I0/M0，pin全匹配、canonical只增一个必需输入；无C/I修补或第二审。保持：旧失败不重判；6ShapeRID根因独立OPEN/未重跑；当前主线/余阶段靠终态执行；像素/声画/硬件真人未验；未发布pose/完整episode不推论；其他美术玩法/dirtytree不签收；review不代替原生/真实保护数据审计。全部裁决及代价见[ledger](../../.superpowers/sdd/2026-11-09-ground-seam-composite/progress.md)。

当前冻结源清单SHA`990993160960C65D1D277184C44B12A99D0BD7053EADA0DAED133DE3521BF874`、六产物清单SHA`2755E0CAA186BF1C6CA84DA63FD3C538A961AFA04C680D2CBA55A53961C7B98F`。无窗口/系统输入/付费或新美术/定时自动化/Git写入/删除/真实档实验/GCcache引擎或新采集。原正式美术动作/正常AV、硬件真人、全部十一项与历史资源/Minor继续OPEN，未声明完整交付。
