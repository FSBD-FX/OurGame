# v339 · 新首关入口实体样段（不是完整关卡交付）

Boss按用户确认放在23→24门前场；第一关止于真正大门前，门后第二关。完整24主段/A–J十支路、旧76普通敌人/各族系/远程职能/全部机关与原十一项仍是同一个ACTIVE目标，没有缩成这个样段。

## 本轮实际制作

新增独立3072×2048的01–04/A物理authoring leaf，不套用旧五区边框。1280×720参考覆盖3×3视窗，不是九个切换房间。外部岩棚→裂谷下行→向左封闭侧洞→下层向右岩梁；A从上层向东入资源洞，再下落回接03。15组真实矩形岩体、顶壁和边界/下方接落路线；134个节点保存重载无丢失。场景、实体碰撞、抓取材质、资源、未来机关/居民及五层视觉节点分开，旧Platform岩土皮肤复用，明确是原型，不是新生成/最终美术。

放置7个v338正式资源组件：主01/02/03/04各1堆，A箱＋2堆；ID和数量由原runtime目录决定。**独立叶尚未接campaign Flow/profile，这7处只证明保存位置，不能领取、没有新存档成功或完成关卡的声称。**54点完整部署/背包消费经济未完成；此前v338实际Q/原子存盘/冷读证据仍只代表其组件。

源入口：`G:/knight/Knight/tools/build/prologue/FirstGateEntryTerrain.cs`，本地固体共享接缝在相邻`CanyonContentBuilder.EntryTerrain.cs`。未改旧运行地形、正式Player物理、敌人、Boss、现存档或运行入口。

## 实际连续通行

最终普通headless case：`G:/knight/验证证据/october13-stable-claw/entry-terrain-20261110-144651-697-5f258eb9ba914a40bb86315c9bc1d8c3/`。

|独立连续路线|真实输入采样|普通跳/短喷|结果|
|---|---:|---:|---|
|01→02→03→04|1039|1/0|同一角色，HP5、受伤0|
|01→A→03→04|1484|1/0|同一角色，HP5、受伤0|
|01→04失足落底→接落台→回到岩梁|1089|2/0|两次分别落地起跳，不是二段跳，HP5、受伤0|

每条路线仅挂树前设置一次出生位，之后不写位置、速度、HP、物理调参或floor状态；实际键盘事件经原输入读取/MoveAndSlide。原生射线验证入口地面、03洞顶、转折岩壁及04封端。小地图从实际保存地形得到98304个8px格、15个结构形状，独立位置反例证明岩梁真实128px缺口及远岸。不是全图/反向/K任意路径证明、正常GPU画面或真人手感。

保存叶：上述case `storage/FirstGateEntry01To04-v339.tscn`，SHA256 `830C768F3B3ABE63C814AE9BFD12F0ED0D2899B6C99126F7A54E413136881C68`。生成器是可编辑来源；不依赖未来删除的媒体副本。

## RED、真实失败与回归

同根所有原件保留，未重跑筛绿、没有GC/引擎/验收豁免：

|entry-terrain case尾标|实际结果|
|---|---|
|142800-bb899e0d|空builder缺真实岩体，行为RED|
|143229-5ca00549|物理/保存通过但16ShapeRID退出泄漏，严格FAIL|
|143507-b5658ed4|test保管新建authoring shapes，在Free后确定性Dispose；139节点16体旧样段严格PASS。未改GC，不证明历史资源根因|
|143900-b1c23855|fixture Damaged委托签名CS0123，构建失败，未运行native，不计行为RED|
|144056-6b31058c|远岸literal为free，证明接落切口误扩256px，且失败退出仍16ShapeRID；严格FAIL，不能算稳定已修|
|144202-a886f7d5|修128缺口后主路1055步通过，A被992高岩台卡在1259/956；严格FAIL|
|144439-f624f9ff|岩台改864并随支撑移动资源后主路/A通过；接落driver未考虑惯性，实际先落返回台而不是底部，原底部前提FAIL|
|144651-5f258eb9|只提前释放水平键，不改底部/两次接地起跳标准；完整三路线60929ms严格PASS，stderr/未解释诊断/guards0|

最终当前1939源SHA `32D2B8D8099E7EFFD9997252AD23F2EC881CB8D5CFB050466554DDF61DFF83C8`，六产物清单SHA `517F4FD0DCDF0C0E0A8AC2DCAF44A449E4177F359BF6F290EFD8D73F6E90F50E`。同版四阶段结果：

|阶段与case尾标|严格结果|
|---|---|
|entry-terrain144651-5f258eb9|PASS60929ms|
|models144903-d8711c10|PASS628ms|
|prologue-pre145045-91980da4|**FAIL**140364ms；44用例虽全返回、native exit0，仍有3条Handle is not initialized/SwapGCHandleForType错误，含BasinSupply.OpenSample781|
|prologue-post145423-9b33bfcd|PASS186185ms；原75案例/10suites，4个原精确预期observer警告分类通过，retired0/ERROR0，采用原有300s门，不称原180s性能通过|

这不是整版通过。当前pre错误不能仅凭与历史相似认定根因，也没有消除历史finalizer FATAL。v338Main/Explore两条原完整路线失败仍OPEN；本轮未重跑它们、不能把v338的9PASS2FAIL写成本轮覆盖。全部原十一项/正式美术动作/正常AV/用户和目标硬件验收仍未完成。

## 审查、判断成本与下一步

终态独立保管核验：2044份实际前后快照、24份实际二进制复制、11个当前helper及当前1939源/六产物/109保护行与四案收据一致，原生消费者0。[终态核验](../../.superpowers/sdd/2026-11-10-first-gate-entry-terrain/verification-v001.md)。媒体单次手动检查21,736,453字节/185文件，未到20GiB档；无新增截图视频、自动化或删除。

一次fresh readonly审查C0/I0/M1，六pins/两原件逆向差异核对准确；不是无条件批准合并/发布。延期Minor仅owner:805“两路线”注释实际三条，真实180s门未变，避免为文字修饰改冻结身份，后续必要helper更新时修正。

本轮判断：先证明独立真实叶，再做世界/存档profile，避免同时大改生命周期；成本是尚不能以campaign游玩。沿用原型岩土、保留旧图及全部失败，不Git/删除；成本是仍未提交、未发布、最终美术待做。全24/10及居民机关密度未用这个样段代替；不签任意机械爪/回返/OOB或正常画面。资源错误保持阻断整版验收，不能由样段PASS豁免。[全部审查裁定与成本](../../.superpowers/sdd/2026-11-10-first-gate-entry-terrain/review-result.md)。

下一步：在旧流程保持不变的前提下，接入独立版本化World/Save/checkpoint/fog/minimap profile，将本叶变成实际campaign样段，验证7点Q/保存/死亡/冷读与反向K边界；继续完成其余20主段/9支路、自然/机械/骨骸/幽灵敌群和分类机关，并独立追查资源错误。全地图、正式美术、动作、UI和视听验收均不得勾完。
