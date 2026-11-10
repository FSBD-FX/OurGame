# v212 · 支撑脚与髋膝踝步态基础

状态：**生产表现已接入；一次独立审查的四个Important均经真实RED→GREEN修补。最终同版9门7PASS/2FAIL：Main未清，pre原生崩溃；整套不绿、Task1及完整序章仍未完成。不是正式动作/美术或手感验收。**

## 事实、推理、风险

**事实：** 本轮新增PlayerGroundGait纯值模型，接入现有Coordinator→Timeline→Frame→FormalVisual→腿部Draw路径。没有改Motor力、速度、重力、碰撞、战斗、输入或能量；十个外层槽、旧构造和15点兼容观察布局保留。没有购买Spine、生成图片或替换获批位图；当前仍为程序结构样段。

**推理：** 用户反馈的整腿平移不能靠多插帧解决。先建立实际位移、地形支撑、固定长度膝踝和脚掌的对应关系，使后续正式分件素材有可替换的运动基础；宽低重心、陶瓷/布关节身份沿用旧规格，具体步幅、腿长和动作风格仍是开发侧待审阅选择。

**风险：** 原生几何通过不证明正常速度画面优美、布料精致或已达到Ori/Spine品质。暂时未知支撑不冒认脚已踩实；新的地面取样和全部旧流程还要跑完整回归，历史Main/Handle及所有原十一项未完成要求不关闭。

## 实现变化

| 原路径 | 当前基础 | 未完成边界 |
|---|---|---|
| 真实Run仍固定0.40秒 | 有实际地形样本时按相对支撑体的位移推进相位 | 旧合成帧保持兼容，不能伪造测量 |
| 整段腿绕槽位旋转 | 原槽内部绘制固定18＋18髋膝踝及鞋 | 程序尺寸不是获批解剖/正式素材 |
| 脚随角色平移 | 支撑脚保留地形局部点；抬脚沿轨迹落脚 | 不可达/未知几何不能标为精确支撑 |
| 停止仍沿时间循环 | 零位移不推进相位，摆脚短时收势 | 具体停顿/优雅与角色性格待正常速度审阅 |
| 每次渲染都可能改变步态 | 物理帧编号去重、实际物理步长 | 事件零时间发表和完整生命周期由独立审查/回归核对 |

模型只存Vector/Transform/Rect/ID等值，不持有碰撞/Node/Resource；真实已存在的slide contact提供单一矩形支撑的normal/point/变换/有限边界。平台搬运与相对步行距离分离；反向从当前脚点继续，腾空/短喷释放约束，传送/支撑切换重建。最终Draw在当前镜像、倾角和动作姿态后换算世界脚点，显示固定关节并暴露实际绘制点/截限状态；不是只检查理想目标。

## 测试先行与保留失败

- a6e3371b：真实Draw固定段长18对0的RED；其模型标记因测试末尾未抛错而无效，已明确保留，不能当模型通过证明。
- 38cf9b04：先修正测试末尾断言，再真实看到六项模型行为RED，随后实施模型。
- 50f4c935：模型12项通过，Draw仍18对0 RED；之后实施实际关节/脚掌绘制与生产接线。
- 703dea9f：重复渲染观察误触收势的反例RED，之后按真实physics stamp/step处理。
- d66f8dc2：首次专项GREEN，13模型项、120绘制样本、实际Motor/Coordinator60有效观察。
- b2fb7980：首次左朝向中立脚点未镜像的RED；修补仅初始化朝向，不以朝向键倒转运动惯性。
- 39902a98：模型19项通过，保留地面数据的死亡兼容帧仍锁脚的RED；消费端明确释放Dead/Recovery数据。
- 80979695：审查前最终专项GREEN，native2088ms/exit0、无ERROR/timeout/guard；model19/planted190/lifted156，Draw120/support134含攻防/爪/倾角/朝向/死亡，真实Motor/Coordinator60/support65。

Native接线夹具使用内部action输入，不冒称完整纯键盘路线、像素、真人或性能证明。所有原始案例保留在G:/knight/验证证据/october13-stable-claw/thruster-ground-gait-*。SourceRows1873，新增专项55秒，原build180s/路线600s/pre180s/post300s不变，不使用ExpectedFailure。

## 一次审查与一次修补

唯一fresh whole-change审查为C0/I4/M1，无二审。四个Important均已修补并由d7b22f4e专项确认：

1. 零时间机械爪Idle事件不能提前占用新物理帧的旧root缓存。前两轮夹具因Reset清输入而没有真实移动，明确不算此项RED；修正为先用真实Motor加速，再让清输入后的实际余速前进，020531ac取得缓存X21.44442与实际25.277754的真实RED后才修生产发布。
2. 接触支撑按实际注册shape owner/index/transform核对，而非只数直接CollisionShape子节点；真实Polygon＋远处Rectangle夹具先RED，再修为歧义支撑不冒认。
3. 同SupportId原地改变有限bounds后必须重新投影/约束脚点；缩窄边界反例先RED，再修不可变bounds比较及重建。
4. 旧15点观察器的脚槽读取实际Draw脚掌，不再用过时常量；只改Draw脚点且节点矩阵不变的负控先RED，保留原点数及其他索引。完整膝/鞋墨迹范围尚未由这15点覆盖。

4261d489/c206bdf3保留前三项中的I2/I3/I4有效RED与I1无效witness诊断；020531ac仅I1有效RED；最后d7b22f4e build25059ms/native3287ms、exit0、无timeout/failure/guard，review4/model19/Draw540样本与569支撑/真实Motor60观察与65支撑均通过。绘制检查包含30/60/120采样率下260px/s中间帧的固定长度、实际支撑点及无截限，不是只比末态；不是像素或硬件FPS证明。

Minor延期：有限输入的派生矩阵/端点算术在极端范围仍可能溢出为非有限值；实际绘制有finite拦截，尚未证实普通关卡会触发，但纯值/诊断合同的鲁棒性不完整。本轮不顺手修Minor，也不宣称全范围物理数值安全。

初始8案与增量4案已独立只读核账：focused累计12案3PASS/9保留FAIL，60清单对、5680份选定源实体、120份helper、72份bin及24个owned身份核对相符，均已终结。首次模型标记缺陷和两次I1无效witness没有被追认为通过或有效失败。

## 最终同版回归

CS/helper冻结后，以下9门均为当前SourceRows1873，没有借旧v211结果；build均exit0、0错误/警告。单位ms，case位于G:/knight/验证证据/october13-stable-claw/，后缀为唯一案号：

| 阶段 | build | native | 严格结果 | case后缀 |
|---|---:|---:|---|---|
| ground-gait | 25059 | 3287 | PASS，review4/model19/Draw540 | d7b22f4e |
| actions | 30480 | 9508 | PASS | f0fcb06f |
| keyboard | 29191 | 20051 | PASS，额外隔离headless标题7807 | b487a2fc |
| models | 29321 | 513 | PASS，仅原机械爪子集 | 630daac0 |
| Main diagnostic | 28129 | 127868 | FAIL，原配重混战899 | 9aced3bd |
| Exploration diagnostic | 26455 | 316735 | PASS，Boss与实体支撑出口 | 7d77a4f8 |
| prologue-pre | 30289 | 57750 | FAIL，原生终结器崩溃 | 7c7580c3 |
| prologue-post | 29725 | 205827 | PASS，75有序对/owner8/nodes1→1 | 8f716897 |
| ranged-mixed-policy | 27363 | 3788 | PASS，仅策略/隔离接线 | ce399406 |

**最终7PASS/2FAIL，不是完成。** 原build180s、专项55s、双路600s、pre180s及既定post300s均未改；无timeout、guardFailures均空。post有4条原负控scope WARNING（seamless1/room-profile1/flow-registry2），无ERROR/FATAL；不是新增忽略规则或静音。models不是全部力学单测；策略PASS不是Main真实混战成功；采样率不是目标硬件FPS，headless标题不是用户看见画面。

Main9aced3bd仍为原900决策内未清配重混战：playerHP2/RustHP1/J7、末帧899/gap41.217，原平台及16.133251精力保留，不放宽24储备/时钟/HP/单跳门。没有把此失败归咎于新绘制，也没有关闭它。Explore当前真实完成Boss与实体出口、恢复0，原生音频排空5frames/68ms，109份保护条目/默认应用数据未改变；不因此把双路或正式流程判合格。

pre7c7580c3 native57750ms，退出-1073741795（0xC000001D），不是超时。原始stderr为FATAL gchandle.is_released，mono_object_disposed_baseref1788→GodotObject.Dispose/Finalize→GC.RunFinalizers；stdout最后在盆地blocked-lane清理返回后。历史有同签名，但尚不能归因于最后夹具、新步态或引擎；不立即重复筛绿、不加GC/扩时限/升级运行时、不做额外dump。post通过不消除该崩溃。

批次首个策略phase误写为thruster-ranged-mixed-policy，被ValidateSet在启动前拒绝，没有生成游戏case；这是执行命令错误，不计游戏失败/通过。其余合法门继续串行，批次结束后已以既有正确ranged-mixed-policy补跑通过，保留原命令输出，不改验证器来接纳错名。focused12案＋最终其余8案共20个游戏case：9PASS/11保留FAIL；这是含TDD过程的累计数，不当作完成率。

## 最终保管与执行边界

独立只读核账最后8案40清单对/4012份选定源实体/90份helper/48份bin/17个owned身份，逐SHA相符；不重哈前12案，累计20案100对/9692份源/210份helper/120份bin/41个owned身份均核对。最终current1873源/最后policy的10 helpers/6 bin与保存副本一致。04:02:47 +08按保存creation/path复核41个身份均已终结；旧PID38196已复用为Codex node_repl，不误判成遗留build，KNIGHT Godot/dotnet为0。root另于04:03:41确认KNIGHT tools0。

最终sourceManifest为93C532B4ED9C09BEDB09E52120E691E79DC76A9D65662C198B4B91394D851AEF；policy helpersManifest为646F5CE099CD958C2CA0F255EF41CF0C9791303EA99B9C8E61FDCD5C8ED3C771（focused只有9 helpers，其不同摘要不是漂移）；binManifest为BAEFAAD7A4A289E818E9D29E7C8EBD4C75AA23911BD03645168D42F2BC1DE65D；Knight.dll为43EECA5E0B6E1A3D6FA6766EA30D96C67C7688D60887B0CAE3D95A87EA8CD8A4。

HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6、branch codex/task6-certified-final-20260902T0555、index21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C未变。原始失败/基线/素材保留；无GUI/OS输入、真实存档、付费/新图、额外dump、运行时变更、自动化、Git变更/发布或删除。没有新增过程媒体，04:00前后的本轮容量检查7,427,750bytes/86files、Notifyfalse/NewThresholds0；不是暂停条件或新提醒任务。

## 本轮裁定及代价

1. 沿用已有身份、低重心与分件方向，直接选择程序步态样段，不为已有边界重复询问；步幅/18＋18腿长是开发侧参数，可能仍被审美否决，不当正式动作稿批准。
2. root独写、保留脏工作区和精确基线，不建新worktree、不提交/发布/删除；一位独立审查补充检查，但不具备干净发布分支资格。
3. 旧构造/缺测帧只保持兼容，不捏造支撑；这些调用没有新脚底约束保证，不能借新模型名称宣称全部消费者已测。
4. 审查未判断正常速度观感；当前仅无窗口授权，不扩大显示/付费/采集范围。代价是美感、角色特色与实际手感仍未证实。
5. 审查未判断Main/Handle等历史故障；保留当前严格失败，不猜根因。代价是完整路线/稳定性不合格，不能正式交付。
6. 审查未独立执行全部native门，由root在修补后跑当前冻结全套，不拼旧通过。代价是已出现的两项失败继续阻止Task1/完整目标完成。
7. I4只修原15点内真实脚掌，不擅增膝/鞋墨迹观察合同。代价是脚点正确不等于完整腿轮廓或整帧相机可见性合格。

## 尚待完成

最终保管已核，Task1仍不记录complete，完整原十一项目标仍ACTIVE；原Main和当前原生资源崩溃保留。接下来先做资源故障的窄范围隔离定位，不以重跑筛绿代替修复；动作方向继续为起跑/急停/转身/落地关键姿态及短喷余速、牵引和附件响应联动。正式独立质量子条R-I可感知惯性、R-C角色动作语言、R-P物理对应与操控手感均OPEN，需要后续正常速度样段、实际操控和用户认可。

原十一项、正式完整动作链、惯性/美术/UI声音/相机、资源稳定、正常显示/硬件/真人验收继续ACTIVE。当前授权仍只允许无窗口工作，无GUI/OS输入、真实存档、付费、额外dump、自动化、Git发布或删除。后续只有获得当次显示许可后才能做正常速度样段审阅。
