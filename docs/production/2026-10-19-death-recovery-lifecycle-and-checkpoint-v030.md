# 死亡恢复生命周期与检查点 · v030

2026-10-19，Asia/Shanghai。完整1–11目标ACTIVE；Task6/7与整关交付未完成。[v029](2026-10-19-user-confirmed-title-visibility-v029.md)的用户确认仅为标题可见，本轮没有重开窗口、自动Start或长路线显示授权。

## 事实、推理、风险

事实：保留原保存Main、原预树出生(120,574)、原三键held输入、公共Health致死调用、原await及四帧上限，先被动记录死亡时序。原测试要求RecoveryRequested回调仍看到伤前held输入，实际致死Damaged订阅已先安全清空Current/Raw；InputMap源仍held、sequence未变，不能归因失焦或没有采样。仅修测试预期与失败路径清理，不改生产移动、死亡、存档或地图。

推理：获批死亡规则要求撤销旧输入，RecoveryRequested仅传reason，并未承诺携带伤前输入快照。保留伤前held前置，改验公共致死调用返回后Current/Raw已清空、sequence不变且InputMap仍held，再继续原同步停处理、延后恢复、actor一次和不重放旧动作。没有把“必须在Damaged回调清空”的当前订阅时序钉成产品合同。

风险：这些是受控原生专项，不是自然游玩死亡、完整存档迁移或真人手感验收。新的五阶段完整前后段回归为4GREEN/1FAIL；post已通过死亡恢复，但在连续U形区域接缝出现新的首错，不能用专项GREEN冒称全suite或整关完成。

## 两个旧前提与清理对照

### 输入生命周期

同一physics4、sequence3的五条有界被动记录：伤前Current/Raw为move=1、primary/shield=true；既有Damaged订阅后Current/Raw归零、authority2→3，InputMap三键仍true；RecoveryRequested时authority4、Recovering、requested1/completed0/HP0；Died后及ApplyDamage返回一致。观察者在既有Ready订阅后安装，不冒称signal入口快照。记录5/8行，overflow=false，无新增等待或自动补键。

原断言失败时，成功路径末尾的原三键release及自有Main teardown被跳过。先单独将**原清理尾**移入finally，原断言仍保持RED：原时序不变、清理实际执行，本案退出2 ObjectDB未再出现。这个对照支持本fixture的失败路径清理，不识别匿名对象，也不关闭旧音频、FATAL、RID或其他资源故障。无GC、缓存清除、额外drain或借用资源Dispose。

### Layout2检查点身份

070810原错误直接打印实际恢复(160,600)，下一窄案071154才新增完整四点收据：captured ID=`canyon.v2.start`、captured position=(120,574)、activated=false，解析点/独立保存起点/实际恢复均为(160,600)。Layout2按stable ID恢复；CanyonWorldLayout解析及SaveData归一均为Entrance/(160,600)。fixture的预树覆盖只改变捕获坐标，不改变start ID，不能将其当作已激活的未来节点。

保留原出生、原1.1距离容差和时限；新增先验要求start ID/Entrance、未激活且ID解析点与**独立保存场景起点**一致，再验恢复位置、ID及未激活状态不变。没有写Progress/存档来配合断言。自定义出生坐标与默认ID可不一致的生产边界仍需关注；若正式保存出生未来偏离ID目录，应作为真实问题处理，本案不宣称所有自定义出生都正确。

## 原生案例链

共同证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw/`。五日志顺序为build.stdout/build.stderr/engine.stdout/engine.stderr/godot.external，数字为行数。全部保留原件，不回填旧案资格。

| 案例目录 | build exit/ms | native exit/ms | 实际结论 | 五日志 |
| --- | --- | --- | --- | --- |
| thruster-generated-main-death-20261019-070219-925-ea5daf26263d46e99a87f4fda5284ff2 | 1/23741 | 未运行 | CS0029专用Godot delegate类型错误；仅编译失败，不计行为RED | 不作为运行证据 |
| thruster-generated-main-death-20261019-070335-662-f8e0ccc2e3e94706aa17e052213dbe16 | 0/29735 | 1/2383 | 原held回调预期RED，退出2 ObjectDB；external漏最后warning/location两行，stderr完整 | 7/0/8/36/42 |
| thruster-generated-main-death-20261019-070559-561-b3bdaf269bf54b28a86a4e70ff401d70 | 0/32862 | 1/2293 | cleanup-only仍原断言RED，原尾实际执行；本案无退出资源诊断 | 7/0/10/35/45 |
| thruster-generated-main-death-20261019-070810-305-0c5f31f63c344dad9f1991ae5c13e264 | 0/36399 | 1/1790 | 输入清理合同通过；下一旧坐标距离预期RED | 7/0/10/35/45 |
| thruster-generated-main-death-20261019-071154-523-513f7fb8f2d1490d80478aeff972245b | 0/30421 | 0/1694 | strict GREEN；原死亡/输入/检查点/延后恢复及原清理尾通过 | 7/0/14/0/14 |

四个实际运行案均无timeout、guards=[]；build均0警告错误。cleanup-only与输入修正案external完整，除预期实际断言ERROR外无ObjectDB/RID/Handle/FATAL。窄GREEN一次request/completed、completion_wait=1、resume_wait=2、actor恢复一次，原audio尾4帧/27ms。success marker明确`human=false full_route=false`，不代替完整恢复矩阵。

各案完整1695 source、9 helpers、109用户保护、continuation及6binary前后守恒。实际selected前3运行案167、窄GREEN168，两侧副本与清单逐hash匹配。完整1695含296 imported，不是1695份C#或全部资产字节副本。

```text
070335 source D5DF87B0D4C4559CF599F28123AC0EDF9D76FF3A53BC920B98AE9125FED3657C
070335 binary FA9F9741ECB7861E45BAB23F1DA920A30FE25E9D99ABF817F704FA2F6CEE1B9F
070559 source 7057727EEABDCB05EC45CABDCDBF34ED815A0597AF00E0BB3D2C09174981DB5F
070559 binary B3CDA64E73C851B8DB9339884D596820D1852913F0580C7D37E18ECA7C88499B
070810 source 14865EC1609FBBB0EF46F56F44413F662ABE863E0D022C085242436B4DF94750
070810 binary DAF1F030ED6598D566FDB6DC0319591BB081246FD71314F10F279993523D106D
071154 source 91C50A9030528211A3D3DA6E2FCD76565914C5B52010D18C142C4E58CD8B7470
071154 binary A244CAF904A840F5A431099CAD8E62D421C07A8355CDA9775C8FCCD08FA7ED8E
```

## 同一冻结版本五阶段回归

session57180终态exit1，明确`DEATH_REGRESSION_FAILED_PHASES=prologue-post`。整个批次及阶段间隙source/helper/docs冻结；没有并行原生消费者或新窗口。五案source/binary与071154相同，不拼接不同冻结为同版结果。

| 案例目录 | build exit/ms | native exit/ms | strict | 五日志 | actual selected |
| --- | --- | --- | --- | --- | --- |
| boundary-20261019-071342-686-a568a0283f91445796802fc65be0f8d3 | 0/30283 | 0/6145 | GREEN | 7/0/31/0/31 | 155 |
| keyboard-20261019-071426-628-2e130585ec2b4a9e9a48f4ad86b2e3e2 | 0/30430 | 0/9885 | GREEN | 7/0/7/0/7 | 155 |
| actions-20261019-071514-432-22137542214b43a5a668de7596a948a0 | 0/31555 | 0/7353 | GREEN | 7/0/10/0/10 | 155 |
| prologue-pre-20261019-071600-577-00fe1d93b39b4c8e8515a2558698a305 | 0/30846 | 0/100749 | GREEN | 7/0/335/0/335 | 182 |
| prologue-post-20261019-071820-270-d866b00698244d02809fcecdeaa64901 | 0/30175 | 1/136863 | FAIL | 7/0/355/117/472 | 176 |

各案build0警告错误、无timeout、guards=[]，1695source/109保护/continuation/6binary全部匹配；helpers为pre10、其余四案9，各自前后匹配，actual selected两侧逐hash匹配。external均完整覆盖stdout/stderr。前三门及pre三raw无诊断；pre实际执行原nonfatal/fatal输入失效案例（其Current与一次性边沿检查不冒称另验Raw或OS输入）。boundary仅受控通知，不是OS前台事实。

post stdout325实际通过generated Main死亡恢复与cleanup，随后通过资源HUD中英布局检查；这些headless布局不是720p–4K图像、正常FPS或视觉验收。首ERROR为：

```text
PrologueCanyonLegacyReplacementTests.FiveSectorSeamsFormOneContinuousU:62
continuous U seam 2 remains inside the authored streaming corridor
next=(1900,3598) previous=(1900,1442)
```

原96距离约束失败。**尚未确定是真实地图断接、布局目录/测试前提过时或深井重构未同步；不得仅放大容差或删断言。** 下一核对保存地形、流送接缝、实际运行路径与获批入口/深井方向。

post另有精确抛异常负例及RoomPresentationProfile错误root负例的原warning，无ObjectDB/RID/Handle/FATAL。不是纯warning veto。GameFlow logger进入ExecuteLegacyPlan，但接缝异常使调用方AssertExact尚未执行；本案没有GameFlow拒绝warning，不伪称已经观察到GameFlow计数失败。

首错fixture与Task7CaseRegistry本案未保留selected副本；只读审计先核当前文件SHA与本案1695清单一致再读取，未回填旧目录：

```text
fixture B3B5E883C31D18801F288E984704DBE5752ACB7F197B9ED37474D1953448ADE6
registry 22F2D641DD5EF0F64A4782EF0D17473B8D6BD253AC50AE6A3D7E95C0060772F3
```

## 状态、成本与下一步

本轮有进展：从不透明死亡复合失败收敛到两个旧测试前提，窄原生GREEN且完整post实际走过；新首错改变下一行动。未改生产Player（保存SHA仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`）、力学、存档、地图或正式资源。严格owner只增窄phase、精确成功标记及真实依赖保留；无此phase警告豁免。

完整1–11、Task6/7、连续U衔接、continuous Upper及其余消费者、Room精确负控、历史音频/匿名资源/FATAL、正式新50=0/50与24canonical、最终地图/原创视听/UI/演出、同版正常Main/Explore、保存迁移/死亡/继续、真人/连续AV/目标硬件60FPS、唯一ZIP与获授权核验GitHub全部继续OPEN。标题可见不扩成长测授权，后续真实前台核对另安排。

所有消费者已终态，source解冻，继续后台只读根因定位。E盘约2454007808字节（2.29GiB），新增证据成本需持续关注；不删移旧证据、不擅自迁移证据盘。无新展示媒体/付费生成、Git写入、ZIP、发布或自动化。
