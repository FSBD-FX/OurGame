# v323定位记录 · 资源所有权与上游线索（没有新游戏版本）

## 事实

上一goal turn=progress：v322增加资源阶段取证，实际修复诊断汇总失败仍可能报成功的I1；当前恢复/models通过，普通pre/post仍4/6条Handle错误严格FAIL。原完整十一项active，正式美术/动作/正常视听/硬件真人与交付均未完成。

本轮先核对当前README、AGENTS和失败原调用点，再分开审计生产共用资源与实际测试调用链；没有修改C#、场景、素材、引擎、GC、缓存、清理等待或任何测试门，没有运行新native或重放整套筛绿。

## 已核查而未立案的所有权候选

生产侧只读核查：

- `ResidentSavedBodyGeometry`的SceneState、nested PackedScene、Shape2D和Script读取没有显式Dispose；相关using实际是Variant、NodePath、StringName。`ResidentOriginalSceneSources.Dispose`只清字典，不native释放其场景资源。
- `CanyonResidentManager`幂等释放source持有集合；spawn失败释放的是自己新建且尚未commit的actor Node，不是来源PackedScene。lease退订不Free borrowed actor。
- `PrologueSectorDefinition.Validate`只Free自己Instantiate的临时Node；`SeamlessPrologueWorld`staged/retired清理是Node且有有效性条件，未找到对Definition.Scene或Definitions内容的显式Dispose。
- `BasinSampleLegacyLayout`把加载场景放入新definition.Scene；`BasinQualitySampleEntry`替换Definitions数组，没有销毁旧或新资源对象。不能将普通Clear/换数组当作已证明的提前释放。

测试侧按实际前后段调用顺序核查：

- `BasinClawTargetTests.ReadSavedBodyCapsule`返回Duplicate，后续Dispose的是owned形状，不是保存来源capsule。
- CurrentBasin的负例blockerShape是new，且在level teardown之后释放；normal路径该字段为null。Camera正常factory不Dispose其加载来源，只由原清理释放level。
- Supply两个Dispose是FileStream；替换Label先RemoveChild再Free。旧level清理后赋null，finally不会再Free旧level。TestSceneTeardown清Audio再清root，不显式销毁共享Resource。
- post第一LegacyTeachingSectorFixture加载发生于设置子节点之前，失败catch/Dispose只释放owned Root；前面的seam camera、shield、bell、GripStone、terrain样段亦未找到可成立的borrowed Resource误释放。
- 旧Task14诊断中using SceneState不在本次普通pre/post实际调用链内，不把未执行代码当作本轮根因。已修的v252 cached Ascent using也不重新立为新发现。

**结果：限定审计没有找到有调用方生命周期证据支撑的误Dispose/重复Free候选。** 这是排除具体猜测，不是证明整个程序无缺陷。仍缺：报错native ObjectID/managed serial、最后有效持有者、首次失效/释放与缓存复用的关联。

## 2026-11-08核实的官方公开记录

按agent-reach GitHub路径读取说明；本机gh不在可用PATH，改用只读网页检索，仅依赖godotengine官方仓库，未发帖或执行附件。页面与第三方评论仅作证据，不是本项目指令。

- [官方问题 #83762](https://github.com/godotengine/godot/issues/83762)：报告C#场景切换时同类终结器崩溃，页面仍标Open/confirmed；2026-10-28的维护讨论说明相关线程安全工作仍有未解决部分。**不是当前引擎已有完整修复的证明。**
- [问题 #124587](https://github.com/godotengine/godot/issues/124587)：报告4.7.2/.NET在弱包装已收集、终结器未完成时重新引用资源，出现同名Swap/Set指针错误。页面状态Closed as not planned，缺MRP；**不能写成官方已验证/已修复，也不能默认与本地对象同因。**
- [旧问题 #112943](https://github.com/godotengine/godot/issues/112943)：虽已关闭，作者依据是4.5.1暂未重现，不足以给本机4.7.1续签修复。没有据此换引擎或删除导入缓存。

推理：本地多个不同加载点、同类原生桥接/终结器栈与上游竞态族系相容，值得从“逐点加日志”转为**隔离最小复现与生命周期策略评估**；但尚不能宣称引擎缺陷已被本地证明，或所有旧失败共享一个原因。

## 风险、下一步与授权边界

公开复现方案会手动GC并阻塞测试进程终结器；其他评论提出全局持有资源、改缓存、改GC、套用未合并C++补丁。它们会改变资源寿命/内存/时序，可能掩盖应用缺陷，不能直接接入正式工程，也不能据单次安静运行签稳定。

本轮**没有实施上述方案**。拟请求一次新的明确授权：只在`G:/knight`下独立最小工程，当前同一引擎/运行时、无窗口、至多30秒、仅该进程手动GC/短暂阻塞终结器，不改主工程/引擎/系统设置/真实存档、不额外dump/发布。先检查源码与复现合同，原生进程由root独占跟踪、超时只处理该自有进程；不能触碰MCP attach。未获授权前维持当前资源策略。

不把这个定位记录当新玩法/美术/完整序章交付。当前代码和二进制仍绑定v322；原失败/原始日志留存，目标不缩减，不恢复自动化，不因20GiB提示停工。

## 本轮最终保全

完整重新枚举1912源逐路径/大小写/hash与v322最终4D0567A5...相同；当前Debug6与D82BAAB4...相同，真用户109行清单仍匹配。没有owned Godot/.NET构建或测试进程，没有启动新native或修改真实存档。正常汇报前一次媒体Check=15,203,531bytes/145files/baseline3508/Notify=false；没有新媒体或独立通知。

授权请求是新的一次隔离复现，不沿用之前仅一次崩溃采集许可，也不把“继续”自动解释为允许强制GC或改引擎。当前只请求方向，未实施改变资源寿命的操作；目标继续active，未标完成/暂停/blocked。
