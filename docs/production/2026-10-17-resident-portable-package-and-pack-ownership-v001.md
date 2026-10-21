# 常驻场景包迁移与保存工具所有权 v001

2026-10-17，Asia/Shanghai。权威checkout：`E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555`。实际后台实现／原生验证进展，不是正式美术、全50入图或整关交付；完整目标ACTIVE。

## 事实、判断、风险

事实：旧候选叶引用某次测试目录的绝对Profile路径，不能直接搬进项目。本轮builder保留24作者tres原字节，把独立配置快照嵌入24叶。48文件原字节搬移后，24实际factory绑定迁移包内的Profile；一套幽灵额外经过真实Ready／时钟受击，原24初始化及7错误装载回归保持。另以原生Object instance-ID反例修正保存工具私有PackedScene的持有关系。

判断：这是可迁移候选准备，不是发布。项目外保存的正常RelativePaths在本次仍写绝对配置引用，因此最终用build自有pathless Duplicate快照，不手改tscn、TakeOverPath或调整缓存。通用保存接口／选项恢复原样，最终仅其私有PackedScene补using。新包仍依赖项目res://脚本／美术，不是独立游戏包。

风险：作者tres变化必须在新空目录检查重建，不能声称自动热更新。外观仍ExplicitPlaceholder，canonical24仍Unimplemented/null、50 Placement仍null、地图新增0。资源告警和整关失败未解，不能借局部通过发布或完成。

## 实际制作与反例

`BuildResidentSubtypeScenes.BuildInto`沿用隔离／空输出／非reparse／保护祖先检查。逐份验证显式配置身份、无覆盖复制作者文件，嵌入build自有配置Duplicate；实际actor／嵌套visual／单一ResidentBinding保留，无新增renderer、图像、平衡或自动mount。

迁移测试实际复制48文件、不改字节，24次Configure-before-Ready，Profile.ResourcePath必须在迁移包内（实际为对应tscn subresource），不接受旧input／staging路径；一套幽灵额外实际初始化并经过PhysicsFrame打开受击。`native_area=false/full_counter=false/full_motion=false`保留，不是AreaEntered／OS真人／全动作证明。

保存工具反例用纯StaticBody＋测试自己创建的Circle，分别走保存成功及真实保存嵌套实例的未owned子节点拒绝。调用者只Free自己的root、Dispose自己创建的circle，再立即查询scalar native Object instance-ID；原两路径仍有效。唯一生产修订为 `var packed` → `using var packed` 后，两路径均失效。没有生产测试钩子／Resource引用传出、GC、等待、cache改策或借用Shape Dispose。该局部合同不等于所有helper异常路径、Validated的另一份加载Resource或全部泄漏修复；大场景154／独立profile31告警均保持。

所有案例根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。以下均保留strict FAIL，无named RED／告警豁免。

| 完整案例目录 | Build ms | Engine exit／ms | 实际边界 |
| --- | ---: | --- | --- |
| resident-scene-binding-20261017-032921-141-fde900a0043c47a0addf3371e9113626 | 23945 | 1／992 | 包内24配置actual0；stdout31 Shape RID |
| resident-scene-binding-20261017-033119-753-41171fee76744b78ace350b23806614e | 23905 | 1／975 | 复制24配置＋RelativePaths后，实际仍原resident-leaves/rock-wanderer.tres；stdout32 RID |
| resident-scene-binding-20261017-033437-129-4d617d6fe2e94cc19024de85045ee26c | 23782 | 1／1028 | 同生产代码不满足“迁移包内sibling或嵌入配置”；stdout32 RID。raw未打印实际路径，不补造 |
| resident-scene-binding-20261017-033617-386-5569456d6b7a40369ad85fd0ffd361e8 | 23875 | 0／7267 | 嵌入快照后迁移24＋1native／原24native／7negative符合；stdout154 RID，仍FAIL |
| resident-scene-binding-20261017-034751-642-d56a0492b9504c599e8f8dac181f1bf1 | 26019 | 1／549 | caller清理后shape仍有效：success=False,rejected=False；stdout2 RID，不按数量做对象对应 |

033437→033617迁移测试未改；034751→034949整个测试SHA同 `6FF90EEB604A051C70B7D582326595C3354E4B2E1B2B717DD20BBEE7D522681D`，1675条完整源库存唯一变化为保存工具的一行using。Object instance-ID不是RenderingServer RID；证明仅限该拥有者／调用者释放合同，不解释154根因。

### 中间失败不覆盖

可迁移时点033617后另六案profile033824／allocation033854／keyboard033923／actions034002／pre034041／post034245为3GREEN／3FAIL。完整pre目录 `prologue-pre-20261017-034041-831-2dcc0c2d23e949ea95716d42617b53f3`，89159ms：三条Handle ERROR＋旧Upper路线ERROR＋两条profile警告。

前两Handle经SwapGCHandleForType，分别ResourceLoader.Load／PackedScene.Instantiate，项目边界均BasinSupply.CreateSampleFixture488→OpenSample455→NewGameDuringHealingCannotSpendTheNewRun295。第三经SetGodotObjectPtr→Dispose→Finalize，无项目对象身份。stderr起始行1／57／112，external61／117／172重复同三条，不算另三次已证明事件；未归因私有PackedScene。

## 最终同源码七门：3 GREEN／4 FAIL

| 完整案例目录 | Build ms | Engine exit／ms | 严格结果 |
| --- | ---: | --- | --- |
| resident-scene-binding-20261017-034949-247-521269bbe2b24d6dac86dfa9f2fbea91 | 30988 | 0／7376 | FAIL：2所有权／24迁移factory＋1native／原24native／7negative符合；stdout154 Shape RID |
| resident-subtype-profile-20261017-035200-643-728a24ed386148e1931dc77d1ff4e3b5 | 28603 | 0／910 | FAIL：原Resource／14negative符合；stdout31 Shape RID |
| resident-allocation-20261017-035235-782-08cd12bd25204af986e29d5334e94cc9 | 21536 | 0／410 | GREEN：50身份／24子型／17negative；仍50未置／正式新增0 |
| keyboard-20261017-035302-799-3cfe4a1687184b73bda5ce9f21456e36 | 28106 | 0／9596 | GREEN；引擎合成输入，不是OS真人路线 |
| actions-20261017-035345-138-1a48e7a1a9de48c7bd7a3f438ec99855 | 22287 | 0／7229 | GREEN |
| prologue-pre-20261017-035419-582-ca8514f746694521a037b3924305f18a | 27662 | 1／88757 | FAIL：UpperPendulum/lower_to_mid_swing sample=.5，final=(264.99634,160.6107)，149/150；两条profile WARNING |
| prologue-post-20261017-035621-888-18fd7bf846a440cdbb8e95ce08e026b6 | 26506 | 1／9209 | FAIL：BellChainWaitForFloor final=(540.0704,1196.0139)；一条六祖先ILevelRecovery ERROR，external重复 |

最终pre三sink中Handle／SwapGCHandle／SetGodotObjectPtr均0，仅本次未复现，不关闭中间三条或历史FATAL。全部Fresh Rebuild exit0、0W0E；build cap180000ms，focused engine55000ms、完整pre/post180000ms；无timeout／guard failure。串行外层exit0只表示各owner终态，不把失败改绿。全部消费者终态后才编辑本报告和项目状态。

共同manifest SHA256：source `11CF2E4D8BD89897EF5C047C6B9F04593879F850DD497A156750CF07BAFFFC7D`；binary6 `90FE2D82AA28D7DE71AF105DB7939668AA92A268D5F273502CC1D81BA74DA19F`；helper9 `909C791DEC03D3DCE0B618A2248D2A6BF8D11DFDCFCD7D272BCED9B89E794F09`；pre helper10 `84580F8993F92C5A4A05F2458B2C5A07558F6D329E278F76A97CF87B6C925A69`。每案1675 source／109 protected／6 binary，before-after一致；真实userdata只hash，不读写语义。

独立只读核对：前四案20pairs／1192源码副本／24二进制，中间六案30pairs／1872源码副本／36二进制，所有权两案10pairs／596源码副本／12二进制，最终另六案30pairs／1872源码副本／36二进制全部匹配。合计90pairs／5532源码副本／108二进制；是证据保管，不是whole-plan代码评审／验收。

源码SHA：保存工具 `3EAACFA0C12C1AC99A3BA5691D29ABE9E38C9EF75BF2D98A9A6CDE6749CC0D44`；builder `56846B44A9C772F6A78416912CF723D892A663947FF0AE8BC620C7CE45DDD2F7`；tests `6FF90EEB604A051C70B7D582326595C3354E4B2E1B2B717DD20BBEE7D522681D`；owner `01864B1A3853B86328AFC37DB2044DD9D224FC29C0DBE7085A2D68A9F65712E2`。

## 产物与下一制作入口

最终scene-binding案例storage共124文件：`leaf-source-profiles/`24原始输入；`resident-leaves/`24positive tscn＋24作者tres＋2negative；`relocated-resident-package/`48迁移文件；root两份所有权控制文件。48迁移文件与原包同名SHA全一致，24作者tres与输入一致；24迁移叶均正确单一嵌入Profile，无外部tres／旧case或绝对Windows配置引用。原件未覆盖／删除。

没有新增C#文件，完整source/resource仍1675。owner新增保存工具选中快照及实际迁移／所有权收据要求，三sink严格否决／隔离／保护／正常路线精确guard不减。无新图片、音频或视频展示，未生成媒体／付费／Git／上传／自动化／前台抢键。

下一回到ALL50真实身体／活动／攻击／返航／玩家接近／保护区、必要围合侧龛及world-lifetime manager；候选包进入下一调查须保实际引用／字节保管，不能从failed案偷偷发布。旧41候选／9未解仍只是旧Probe保守包络：Abyss.01及Frozen.01/.02无采样模板；Archive.03/.04、Gear.06/.07、Steam.02、Pendulum.04联合冲突，Gear耗尽20000节点。当前查询丢弃每模板Issues，不能逐项倒推墙／支撑／交互区，更不能把已有3个Archive冲锋候选称完全放不下。先留实际失败原因、核验真实leaf／行为包络，再决定扩建，不简单提高预算或把null变原点。

全Task1最终50、全Task2、Task3/4均OPEN，不task-done／勾全完成／whole-plan review。旧26＋新50＝76普通＋1Boss、Bone10/Ghost10、ECHO一指引人格不减；键盘/Q/J/K/L、真实墙顶爪、9残片／6门槛、检查点／旧档／回返／支撑出口、正常Main/Explore、真实显示60帧、最终美术／音效／归档短录像审阅／真人反馈，以及资源／旧路线失败全部保留。目标ACTIVE，不承诺十万付费人数。
