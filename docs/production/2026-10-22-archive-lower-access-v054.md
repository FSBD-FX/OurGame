# 档案下层真实进出与兼容复验 · v054

2026-10-22，实际工程 `G:/knight/Knight`。完整1–11目标保持ACTIVE；这是局部制作进展，不是新试玩、最终美术或整关验收。

## 事实：同一Player连续访问上下层

已有上层路线之后，继续经原ArchiveShelf／ArchiveFloor，从下层左侧实际抓附、借力、落地、走入内部，最后返回原FrozenLeftCatch。本轮仅修改测试驱动、严格helper合同和状态文件，没有生产地形／力量／重力／资源代码改动，不缩身体或危险域。

先保留两个原生失败，再调整真实按键驱动：

1. `075816-55bf8e41`：native1／15902ms。下层Left面确实选中并进入Latched／Reel；D瞄准残留的正向速度使完整身体撞到FrozenCeiling侧面。独立同帧TestMove命中Left、LOS=true；没有下层到达／借力。
2. `080045-86577039`：native1／15996ms。真实A反向制动清除了正向漂移；下落过深后的Obstructed帧却是LOS=false／TestMove=false，绳路被冰角遮挡。不能把它等同首案身体碰撞，也不是控制器内部唯一分支的证明。
3. 仅将真实接近站位引导1376改1352、允许中心1348..1356，在更高位置提前SE抓取；同dock、侧面、力量、完整碰撞及原生上限不变。`080236-dfe84798`首次严格通过，612观察／619physics。

最终同冻案 `resident-archive-alcoves-20261022-080454-919-6fa2976f89274fc0b2479f6971522075` 再次严格通过，native0／19547ms：

- 同一实际Player从原Entry进入上层和下层内部，再回原冰区完整接落点；最终(1336.0747,2143.9849)。本案为 **612观察／626physics**，不是前案619。
- 下层真实Left抓附、同cast实际Arrived，再一次已有Space+A借力和fresh D入台；进入内部并完整支撑。
- 总计2casts／2Arrivals／1Cancel／1普通跳／1附着Boost；短喷0、HP5→5、治疗0、入树后位置／速度／血量注入0。
- 完整40×72身体、全部潜在危险域、命名平台接触、完整横向支撑及三脚底射线保留。原共享300native从离开旧Floor前开始，覆盖接近、准入、到达、借力和完整落地。
- 下层内部另有预先声明的120走动＋12制动预算；旧上层80＋12、转移300未扩大。准入、反刹、借力、取消及离台均检查真实fresh输入。

候选dock完整身体距Ice仅4px。**物理可行不等于真人容易使用；入口容错／可读性仍OPEN。**这是独立夹具，机关回调停止、居民不占用，不能当作正常开场到Boss或占用战斗的整条路线。

## 事实：最终17项同冻复验

证据统一在 `G:/knight/验证证据/october13-stable-claw/`，目录名为phase＋20261022＋下表时间＋GUID。owned会话47750终态exit1：**17=11PASS／6严格FAIL**。

| phase | 时间／GUID前缀 | native退出／ms | 严格结果 |
|---|---|---:|---|
| resident-archive-alcoves | 080454／6fa2976f | 0／19547 | PASS |
| resident-archive-metadata | 080554／0ba1905a | 0／5561 | FAIL：stdout266 ShapeRID |
| resident-steam-metadata | 080637／5f71299a | 0／4940 | FAIL：stdout266 ShapeRID |
| resident-gear-left-access | 080711／b9648d72 | 0／19296 | PASS |
| resident-gear-alcoves | 080758／d8792042 | 0／9869 | PASS |
| resident-parent | 080837／bb55a0e9 | 0／2172 | PASS |
| resident-steam-alcove | 080907／1eacb8a3 | 0／10442 | PASS |
| resident-pendulum-alcove | 080944／8d3cfe00 | -1073741795／11670 | FAIL：GCHandle Finalize FATAL |
| resident-abyss-alcove | 081026／55b0b6af | 0／8756 | PASS |
| resident-frozen-player | 081102／b20c8e26 | 0／5396 | PASS |
| resident-map-census | 081134／e4137f65 | 0／25052 | PASS：仅空间候选 |
| resident-void-stream | 081225／fe96fdbf | 0／1436 | FAIL：stdout187 ShapeRID |
| keyboard | 081253／85ad836b | 0／9673 | PASS |
| actions | 081330／73ad440b | 0／7079 | PASS |
| canyon-structure-v2 | 081404／d199da67 | 1／948 | FAIL：Archive640／704 |
| prologue-pre | 081434／b0292c09 | 0／100122 | PASS：完整原scope |
| prologue-post | 081641／c6155f71 | 1／1105 | FAIL：同Archive断言先于75scope |

摆锤案canonical原两K路线已成功，但candidate143世界建立附近出现 `gchandle.is_released()`、`GodotObject.Finalize()`、0xC000001D，external和stderr都有真实FATAL。候选路线未完成，不能继承v053 PASS或以旧路线marker覆盖。此匿名栈不足以归因具体对象或Archive改动，不重复抽取安静票。

两metadata仍有语义往返／原子拒绝／127碰撞与完整小地图保存证据，但成功标记后的266RID否决接纳；Archive窄3字段未发布。正式范围640与源码704仍未同步，结构和post都在原断言失败。post原75scope／8owner／4negative未执行到，不放宽helper、不继承历史通过。

## 事实：空间人数及保管链

当前143实体普通census实际完成50selected／0unresolved、24子型、1695corridor／71joint节点，search_capped=true。21420条JSONL独立逐条解析通过，SHA256为 `D8C5CEC0D5BDC761613B73EEEC65878B55862AE0B6F4646A46709709E06573CC`。

保存叶与实际factory已使用，但actual_ready=false／motion_certified=false／placements_authored=0。候选143／Descent135／正式127，正式新增0/50、全50 Placement=null、24正式子型Unimplemented；未将候选整体覆盖正式世界。

运行期间全部源／helper／文档冻结。终态后root重枚举1709源／879C#、11去重helper、109行真实用户清单（104文件只核哈希）、6binary均零差，无owned native consumer。独立审计17案：85对清单、6060选中source＋174helper前后副本、102binary、34组PID／argv／contract／wait均吻合；17次构建0warning/error、无timeout、guards0。选中副本不等于全部1709项实体备份或Git整库认证。

| 清单／文件 | SHA256 |
|---|---|
| source-after.txt | 56CBBAC24C242D1C818926170E36449A5F4476BADC2C984F7DA4AF2D2E3470D6 |
| helpers普通15案 | F575E1F4EAF7E5BD63D8025F21EDF7D4BE491803B2BB7E7A74F836F107B44A6A |
| helpers census | DB9234271C7B8802643D0C6DB669018F6D7CADB067F34260BD020DDC339A92CA |
| helpers pre | DC5A8119480838FF6CA04DA514525F828583ACF71B97F6D8916D065C219BAE10 |
| binaries六项 | B9E256D50D25D924545EE09726F673FE4B1F64A89BF0377CA6B88C5670D0FDB5 |
| Knight.dll | EEFEFD2B5CE87E50810A85A7D6D15B6A89B3D811734EA9F8A8A7DC4DDFAE8C69 |
| user109行 | BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB |
| continuation空 | 7EB70257593DA06F682A3DDDA54A9D260D4FC514F645237F5CA74B08F8DA61A6 |

HEAD仍e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6，index仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。没有Git写入或真实存档语义读取。

## 推理、风险与下一步

既有能力足以完成本次下层访问，不需据此修改全关拉力或增加二段跳；像素级入口不算完成手感，50空间候选不算同场密度验收。

独立只读资源调查未证明新的拥有权失衡。下一有区分力的诊断：观察已有SaveAndValidate默认加载、私有Ignore回读、父目录装载的根及子形状标量身份；仍存活只能定位家族，不证明错误拥有者。不得用新增GC／缓存／drain／KeepAlive或Dispose借入资源掩盖问题。

50同场Ready／巡逻测试须冻结本次joint最终选择，不能另从accepted桶任选坐标；一次挂全50、保持真实callbacks、完整身体并逐点回环，炮台固定home保持支撑。无目标巡逻经过home不冒认战斗Returning；32×32夹具viewport不能证明炮台可见攻击。旧36仅是26身份的主备保留记录，不是36只真实旧敌；普通活动危害、旧26共存及玩家接近仍待验。

完整1–11／Tasks1–4继续ACTIVE：资源错误关闭、严格区域同步／完整post、50实际运动／攻击／返回／生命周期／正式接线、统一原创视听／场景／UI、正常Main／Explore、存档死亡继续、720p至4K／目标机60FPS／真人试玩、唯一版本Windows ZIP及获授权GitHub归档全部保留。历史identity合同cleanup／call-regex和bottom180＋8共享预算仍OPEN。

本轮无付费、美术生成、音频、展示媒体、OS前台操作、Git写入、删除或自动化。一次性媒体-Check为4838字节／基线3508／增长1330／Notify=false；未重置基线、恢复旧归档或完成／暂停／阻塞目标。
