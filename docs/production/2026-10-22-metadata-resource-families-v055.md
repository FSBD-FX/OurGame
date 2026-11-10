# 区域同步资源身份观测 · v055

2026-10-22，实际工程 `G:/knight/Knight`，完整1–11目标保持ACTIVE。上一轮v054为实际进展；本轮完成一个有区分力的资源诊断，不是资源修复、人口正式接入、美术更新或新试玩交付。

## 事实：先验证诊断合同

独立 `METADATA_RESOURCE_IDENTITY` 编译门只允许Archive／Steam metadata两个phase使用 `-TraceMetadataResources`。没有复用REFCOUNTED最终释放探针：它会观察其他资源的Dispose；现有CensusScope在metadata也未开启，而mounted／SceneWalk路径会新增GetState，不适合本题。

- 实际argv合同先失败：普通7参数、要求诊断8参数；最小准入后24项通过。错误phase和混用flags在启动构建／引擎前拒绝。
- 实际日志判定函数先以缺失观测失败，再通过21项合同：完整证据、UInt64大ID、缺根／形状／liveness、矛盾或重复relation、损坏JSON、三个raw sink错误及普通／诊断混用均有控制。
- 初版PowerShell数组优先级让多个负例合为一例，实际仅6项；已修为独立用例并固定数量，最终21，不把6冒称20。旧census准入24项仍通过。

观察只读取已经存在的默认加载packed／验证copy、原始和donor图、私有Ignore回读、父定义已加载Scene。静态表仅存4096条以内的不可变字符串／ulong；清理后只按ID查询liveness。没有额外Load／Instantiate／GetState、GC／缓存／drain／KeepAlive或借入Dispose。生产SaveAndValidate仅有编译门内观测调用；普通行为不改。测试观察／退出marker和receipt始终behaviorAccepted=false／canonicalPublished=false。

## 事实：两个完整原生诊断

证据在 `G:/knight/验证证据/october13-stable-claw/`：

| 诊断案 | native退出／ms | 严格结果 |
|---|---:|---|
| resident-archive-metadata-metadata-resources-20261022-083209-881-07013de8f2844e62acf62b07a79c9595 | 0／5081 | FAIL：stdout255 ShapeRID |
| resident-steam-metadata-metadata-resources-20261022-083257-140-6557ba6d314e4603aa6b93adaa6efa84 | 0／5814 | FAIL：stdout266 ShapeRID |

不是266／266；255这个差异本身不能证明改善或因果。两个原完整语义同步测试都保留，raw错误仍否决接纳。

| 已观测shape族 | Archive条数 | Steam条数 | 既有外层清理后 |
|---|---:|---:|---|
| canonical | 40 | 40 | 全存活 |
| target | 40 | 40 | 与canonical同40个ID |
| direct-new donor | 48 | 45 | 全失效 |
| save-default验证copy | 40 | 40 | 根及40形状全失效 |
| private-ignore回读 | 40 | 40 | 根失效，40形状仍存活 |

canonical／donor／save-default／private-ignore四个shape族两两ID交集0；target只复用canonical。default、private、parent-candidate的Scene ID也三者不同。具体Scene ID：

| 案例 | default | private | parent-candidate |
|---|---|---|---|
| Archive | 9223372089652677177 | 9223372095776361059 | 9223372269722537984 |
| Steam | 9223372089669454359 | 9223372095726029377 | 9223372270980829153 |

**“父定义复用先前SaveAndValidate默认根”的本轮假说被否定。**最终parent-candidate Scene在Archive已失效、Steam仍存活；不能只凭同一路径推断同一个对象。Steam未记录canonical／target的Scene ID，不额外推断它们的根关系。

独立只读审计：Archive215 identity／172唯一ID、601 liveness、1 relation，共817 JSON；Steam210／169、586、1，共797。按原始十进制UInt64比较，无浮点舍入；40个liveness分组无缺失／多余／同点矛盾或false→true反转，8字段relation均吻合。10对清单、756选中source＋28helper副本、12binary及4组PID／argv／contract／wait一致；两次构建0warning/error、无timeout、guards0。

这些只是**退出drain之前、已命名采样点**的事实。private40仍有效不等于它们就是匿名泄漏，不证明非法拥有者或给予Dispose许可；不能把这40与255／266相减解释全部错误。没有据此变更释放策略或发布Archive三字段。

## 事实：普通构建恢复后的完整17项

全部源／helper／文档在owned运行期间冻结，会话30364终态exit1。没有重复抽取安静票。普通构建argv不带诊断symbol；首案保留DLL的PE元数据实际检查 `MetadataResourceTrace` 类型数量0。

| phase | 20261022时间／GUID前缀 | native退出／ms | 严格结果 |
|---|---|---:|---|
| resident-archive-alcoves | 083613／08331c3e | 0／18572 | PASS |
| resident-archive-metadata | 083704／288b817a | 0／4399 | FAIL：266 ShapeRID |
| resident-steam-metadata | 083736／95dfc816 | 0／4958 | FAIL：266 ShapeRID |
| resident-gear-left-access | 083809／bc0bc246 | 0／19503 | PASS |
| resident-gear-alcoves | 083855／9438ac29 | 0／9855 | PASS |
| resident-parent | 083934／81f946c9 | 0／2268 | PASS |
| resident-steam-alcove | 084005／4e7f883b | 0／10333 | PASS |
| resident-pendulum-alcove | 084043／ac42e993 | 0／19226 | PASS，本次安静不修复旧FATAL |
| resident-abyss-alcove | 084136／ebc01f82 | 0／8939 | PASS |
| resident-frozen-player | 084216／4e84b5b2 | 0／5417 | PASS |
| resident-map-census | 084248／1af24144 | 0／24060 | FAIL：Handle未初始化 |
| resident-void-stream | 084347／3d88d75e | 0／1456 | FAIL：187 ShapeRID |
| keyboard | 084415／61639cae | 0／9593 | PASS |
| actions | 084453／38ecae28 | 0／7081 | PASS |
| canyon-structure-v2 | 084527／5d07ac9f | 1／983 | FAIL：Archive640／704 |
| prologue-pre | 084556／19c15b51 | 0／99872 | PASS：完整原scope |
| prologue-post | 084807／44a2134e | 1／969 | FAIL：同Archive断言 |

**17=11PASS／6严格FAIL**。此次census并未崩溃或未完成：实际搜索结束50selected／0unresolved、1695corridor／71joint、21420 JSONL；但期间stderr／external出现同一条 `Handle is not initialized`，栈为 `SwapGCHandleForType → ResourceLoader.Load → GD.Load → CensusLeafSource.Configure:948 → RunActivityCandidateCensus:1352`，仍严格FAIL。存在可用数据不等于获得当前资格。JSONL逐条解析21420成功，SHA仍为D8C5CEC0D5BDC761613B73EEEC65878B55862AE0B6F4646A46709709E06573CC。

Archive同Player上下层进出本案613观察／620physics、HP5／完整40×72、2casts／2Arrivals／1Cancel／1普通跳／1已有Boost／短喷0／零后置姿态写入；不是v054的612／626。下层4px准入的真人容错仍OPEN。摆锤本次通过不关闭v054 Finalize FATAL；人口调查本次是可恢复ERROR，不冒称0xC000001D。

两个metadata、void退出及正式Archive640／704仍未解决；post未到原75scope／8owner／4negative，不放宽scope receipt，不继承历史通过。候选143／Descent135／正式127，正式新增0/50、全50 Placement=null、24正式子型Unimplemented保持。

### 当前文件封存

终态后root重新枚举1709源／879C#、11去重helper、109行真实用户清单（104文件仅哈希）、6binary均零差，owned native0。HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。

独立只读普通矩阵复核：85对清单、6060选中source＋174helper副本、102binary、34组PID／argv／contract／wait全部吻合，17次构建0warning/error、无timeout／guard差异。51份运行日志均无metadata-resource／refcounted诊断标记，17案DLL哈希一致；首DLL静态读取6486个TypeDefinition，诊断类型0。不是所有1709项的实体备份或整库Git认证。

| 当前普通清单／文件 | SHA256 |
|---|---|
| source-after.txt | E0CD149BF3ADEBE46E5447E98020779754236D3CD381C7095EBAAF4594F7F613 |
| helpers普通15案 | 3CE90F38C6D5DB73F117CC8918484F3A6B3366FD07C7231642BC7F545406955C |
| helpers census | DDBF064143083A596BC648E4A94CD9EB763063E0AE2B5823FA9F28B933732429 |
| helpers pre | FB5898670F3B73AD0B6E06197F6F5C7CADFCD097E79B009A209B4D8F882A913F |
| binaries六项 | 3959BE20517E7F317F2DAC3EED78B16538810FBFA1EF7932EE9FB64E8D4D78CA |
| Knight.dll | 7FD7E0FE0B14EA70829CB86970697C5B77CA4876371C2DC2871382002A5254FF |
| user109行 | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| continuation空 | 7EB70257593DA06F682A3DDDA54A9D260D4FC514F645237F5CA74B08F8DA61A6 |

## 推理、风险与下一步

资源定位的下一有区分力边界是已有MinimapTerrainCatalogBuilder临时实例，以及本次真实失败的CensusLeafSource加载；不得因同路径就假定同ID，也不得按存活／数量猜拥有权，或靠GC／缓存／drain／KeepAlive掩盖。并行推进内容，不把无限资源重跑当作替代。

下一人口步骤：把**最终joint的实际50个选择**导成固定顺序数值数据，外部可信receipt固定payload/context SHA；stableID查原catalog，不允许文件改写族系、角色或区域。保留home、原territory、全部有序points和occupancy，不能从accepted候选桶另选。挂载前整体拒绝缺／重身份、变点／活动域／包络／行序，甚至几何同样合法的飞行点顺序变更；随后一次挂50，观察真实完整身体、逐点回环和炮台固定支撑。无目标巡逻不等于战斗Returning，旧36是26身份的主备保留记录，普通活动危害／旧26共存／玩家接近仍需实测。

完整1–11／Tasks1–4及原26+新50+Boss、骨骸10／幽灵10、最终统一视听／场景／UI／演出、正常Main／Explore、存档死亡继续、720p至4K／目标机60FPS／真人试玩、唯一版本Windows ZIP及获授权GitHub归档全部保留。历史合同cleanup／call-regex和bottom180＋8共享预算仍OPEN，不标记任何完整Task或目标完成。

本轮无付费、生产玩法／几何／力量改动、新美术／音频／展示媒体、前台操作、Git写入、删除或自动化。媒体一次性-Check为4838字节／基线3508／增长1330／Notify=false，不重置基线，不恢复旧归档。

状态文档保存后再次枚举1709源及11去重helper均零差，受改代码diff检查exit0；目标工具读回仍ACTIVE。
