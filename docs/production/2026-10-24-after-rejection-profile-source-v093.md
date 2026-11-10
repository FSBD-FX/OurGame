# v093 · 原拒绝后的同场景配置取证

2026-10-24，KNIGHT，唯一工程 `G:/knight/Knight`。原十一项目标ACTIVE。本轮有新的故障边界证据，不是游戏修复或整项完成。用户“没看到”继续有效；没有重新开窗、系统输入或画面验收。

## 事实、推理与风险

实际REF完整post又在低顶桥 `.03` 第7次观察收到一次null赋值。仅在原字段求值为空、原赋值记录成功Write＋Flush之后，对同一个已加载PackedScene做有界读取；没有再加载、重试或回填。可见的拒绝后JSON为 `object_wrapper_null_or_invalid`，SceneId=`9223374373853858855`，同一场景路径；ProfileId/serial记录0只是不可用身份的占位，不证明原生ObjectId为0。

该状态来自保存值为Object但二次桥接结果null或invalid的分支，不是“已证原Variant为Nil”。它也合并了null与失效wrapper，尚不能区分哪一个，更不能倒推最初setter转换时刻。再次AsGodotObject可能重建包装，取证会扰动后续清理。不能据此认定Dispose、最后一个case或FATAL的因果。

本案FATAL在stderr3356行，拒绝后JSON在3363行且与原生`Fatal error. `前缀串接：正常锚定来源记录0、可见JSON1；END3371报告7操作/7字段、after1/1，但不构成完整协议或稳定性通过。原180秒和原raw/scope/75门槛保持，实际164496ms原生FATAL，非超时。

另有独立真实失败：FormalHud/Prompt/Glyph与Prompt/Text在1920×1080、UI0.8下重叠47px，primary detail位于3370行，不是旧HUD Title失败。glyph矩形(1394.4,69.6)/191×59，copy(1538.4,64.8)/295.2×60。只读定位到生产先设glyph尺寸后改字号、copy固定偏移；此前组合是1280×720/UI1.5，候选为旧minimum-clamped宽未缩回。尚缺当次字号/minimum/font测量，不能断言唯一原因。本轮未修改UI。后来的普通post未复现该断言，但原180秒到期，仅35 enter/34 return，不能反证UI已修。

## 实现与测试

恰3个C#增量：ResidentSceneBinding、Task14NativeResourceIdentity、RunResidentCensusIdentityContract；另2个既有PS帮助器。没有新源文件、玩家物理/AI/HP/能量、地图、正式图声或发行变化。

- 普通编译的Profile自动属性和原OR/ConfigureActor/异常清理不变。新增接点只在REF拒绝分支。
- ROW成功状态包括Flush。未确认写入、未求值或原字段非空时不追加读取；sink异常不能替换原ArgumentException。
- 提取既有有界reader，保留节点512/属性128等边界；SceneState/Profile等借入资源不Dispose，只释放原局部值包装，不跨await保存资源。
- 新阶段明确`after_original_profile_null_gate`、NotAtConversion、SecondBridge；BEGIN不再谎称整个phase零GetState，END记录原空字段数与来源数。旧SOURCE协议不开启。
- 真实控制先RED后GREEN：同源对象/实例故意置空保持不变、借入对象仍有效；实际工厂的故意缺失配置仍拒绝，日志Flush失败不追加读。控制的私有saved-load只发生于隔离夹具，不是游戏加载器修复。
- canonical验证操作/slot/path/时序、完整字段及UInt64身份、未知状态/额外前缀/缺失来源/重复/错关联等。原75/raw/scope/180000ms、argv与guard不放宽。

## 实际运行

证据根：`G:/knight/验证证据/october13-stable-claw/`，下表时间短名唯一对应本轮案目录。

| 时间／phase | native exit／ms | 结果 |
|---|---:|---|
| 134812 REF contract | 1 / 638 | after-source真实RED |
| 135050 REF contract | 0 / 1019 | 同PackedScene取证GREEN，仅诊断 |
| 135226 REF contract | 未启动 | 编译CS0103，缺Catalog命名空间 |
| 135353 REF contract | 未启动 | 同编译错误，非行为RED |
| 135439 REF contract | 1 / 748 | 夹具ResourcePath为空的setup失败，非工厂目标RED |
| 135742 REF contract | 1 / 737 | 实际原工厂缺少拒绝后记录的目标RED |
| 135838 REF contract | 0 / 822 | 实际工厂/原拒绝/Flush否决GREEN，仅诊断 |
| 140030 REF post | -1073741795 / 164496 | 上述FATAL、UI重叠、混合来源行，FAIL |
| 140610 ordinary pre | 0 / 166728 | phase OK但7条Handle ERROR，FAIL |
| 140938 ordinary post | -1 / 180132 | 原cap TIMEOUT；35 enter/34 return、无phase OK，FAIL |
| 141313 ordinary models | 0 / 650 | 最终普通构建PASS |

11次尝试含2构建FAIL、9次实际native；native3严格PASS/6FAIL，总尝试3PASS/8FAIL。普通post ERROR/Handle/FATAL/ShapeRID全0却超时，不能称FATAL或通过。REF post只有1个FATAL ERROR；3次native反例/setup断言和pre7条Handle合计本轮11个native ERROR头，external镜像不双计。buildFAIL各有2处CS0103及1个未生成binary guard，其余guards0。只有普通post超时；两post各两条原qualified WARNING保留。

135439的setup失败后撤回提前接点，修正隔离夹具为显式加载已保存文件，再取得135742真正RED后接回；没有把setup/编译错误当行为证明。根因尚未修复，未改策略的Main/Explore不重复跑取绿，旧死亡、完整双路资格仍OPEN。

## 审查、裁决与未完成

唯一fresh审查：Critical0 / Important0 / Minor1，没有修正轮或复审。Minor：`after-source-before-row`负例用非空gate行，先在计数而非时序条件被拒；精度缺口延期。reviewer用正确null-gate移位输入独立验证实际时序guard会拒绝，但尚缺持久化的精准反例。107纯合同由root完整运行通过；reviewer受限未执行10个owner顶层拒绝调用，其余97例独立通过。其他纯回归96/26/33通过。旧未知assignment前缀漏检已随新协议完整命名空间计数纳入负例；原四族Minor继续保留，资源故障不是Minor。

全部Declined的root裁决与成本：

1. W/preview未改，不认证排名；成本Main仍OPEN。
2. first-hit/receiver未改，不豁免LOS；成本完整选择验收OPEN。
3. readiness未改，仍要真实Playing；成本启动/路线资格OPEN。
4. callback/deferred未改，保actor/parent/pose/sequence；成本整体验收OPEN。
5. LoadLevel双异常/旧清理不混修；成本旧清理风险保留。
6. 二次桥接不证明原转换/FATAL；成本继续取证，不回填交付。
7. 普通回归按真实FAIL，Main/Explore不借诊断通过；成本没有完整75或正常双路资格。
8. 正式视听、相机、真人硬件/性能/发行未验证；成本全部原十一项ACTIVE。
9. 旧77→82 helper无完整基线副本，不伪造归属；成本该历史字节边界保留。
10. reviewer不跑10顶层调用，root实际107含这些拒绝检查；成本不是reviewer独立全107。
11. 借入wrapper引擎寿命未知，保现有所有权；成本不能宣称资源图安全。

沿用用户dirty checkout，不Git写入/建worktree/task-done/删除工作区；成本没有新提交式检查点。原十一项、76普通＋Boss/NPC、机关、正常双路/死亡恢复继续/Boss出口、相机/正常速度正式视听/硬件真人与交付不缩减。

下一步：先给上述真实HUD转换顺序补字号、Size/CustomMinimumSize/GetCombinedMinimumSize/字体宽标量及同条件新建对照，不增加等待或放宽矩形断言，再做最小布局修正。资源线继续区分二次桥接null/invalid及具体身份，不能把最后日志、hash或安静重复当因果。上述后续尚未实施。

## 保管与资源

53对已生成清单全匹配、54个各案OWN sealed bins全匹配；两buildFAIL未生成binary pair另列，不虚构。当前1776源/892C#、9帮助器、6普通二进制live漂移/缺失0；相对v092恰上述3个C#变化。最终普通pre/post/models源码相同，pre原额外帮助器10项、其他9项，不称清单完全相同。

- Source manifest：`E5E10EB802021FB46A3D04D9B851E999515B7C416B5C6730CF005226E057AA12`
- Binary manifest：`0A146EB60449894DB96048587EFF8FD295382F648A498B8C7ABEC9737030A8D6`
- Knight.dll：`6C930740AF3DB5686B64A4032415A5486D903E49158F4C9D80FB29AB0F478A4A`
- index：`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`，branch/HEAD未变。

最终普通模型案：`models-20261024-141313-790-3cf8c9018f544fa2826dd6846c2c4462`。全部root owner终态，其他godot-ai attach不碰。无新正式资产，资产表无新增正式项；无过程媒体、付费、自动化、打包、删除或真实存档语义读写。媒体一次手动检查UTC06:15:07.7006418、860284字节/基线3508/增长856776，Notify=false；20GiB只随正常汇报提及、不停工。目标工具仍ACTIVE，无状态变更。
