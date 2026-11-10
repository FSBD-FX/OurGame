# v330 · 悬吊圆弧齿轮独立内核与同版验证

## 事实、判断、风险

本轮推进原T03“空中来回摇摆齿轮”，不是增加旧摆锤计数或把直轨画弯。新增独立固定支点、电机周期驱动机构，保留旧直轨、固定齿轮、地图默认值和原动力；不冒称自由重力钟摆/正式美术。用户持续制作授权下的开发方案：先独立原生内核，随后才正式点位与通路验证；原完整十一项/T03均未因此完成。

新生产：`ArcGearMotionModel`有界时钟、解析位置/速度、完整周期/折返visited区间；`CircularArcSweep`双精度有限候选精确弧到点/有限线段，包含法向、支持线交点和投影分区根，胶囊/矩形包含与四边窄相；`CanyonArcGear`继承原Fixed单一GuardedDamage/cadence/ID/撤权链、原生形状/owner/local/尺寸核对，宽相矩形不能授权伤害，未扩大真实r24半径。未来占用先识别arc而非静态Fixed；雾只保护当前native圆，不揭全弧。

只有五新CS和三个既有文件变化，旧CanyonFixedGear/CanyonMovingGear/MovingGearMotionModel及保存地图未改；纯程序辅助悬吊线、支点与原gear外观，无新图片/音视频/付费/正式资产批准。

## 原生反例及修补

案例均位于`G:/knight/验证证据/october13-stable-claw/`。失败原件保留，不以成功覆盖。

| case后缀（phase均thruster-arc-gear） | 实际证据 |
|---|---|
| `20261108-204540-743-ff1f35a88364430385f2b9b67d8750f8` | 1917输入，缺独立runtime原生RED，engine1/531ms。非几何反例。 |
| `20261108-204815-164-97e77231c46047aea116d595f066f32e` | 最小静态stub、实际保存重载及原Player两案均执行；端点原生不碰，弧顶HP5/seq0而应4/1，弦内HP5/seq0，engine1/823ms，功能ARC_GEAR_APEX_RED。原生胶囊数据实测是(72,20)=height/radius，非反序猜测。 |
| `20261108-205231-667-a02a8204491d4d00b00776db23c161ce` | 字面量模型/精确几何测试先写，缺模型RED1/401ms。 |
| `20261108-205912-880-d6d6f53841b84621af0033d2eec35a7d` | 第一对实际几何及literal模型GREEN0/767ms；非完整内核验收。 |
| `20261108-210520-756-d662d9daada4470eb7b80fb11a7e16bf`、`20261108-210819-869-b8bb999b88f44fe9a112890baf4cfed5` | 扩展回调RED+标量诊断：前一Damage回调把另一Player移至2000，native缓存仍200；原revalidation抛错。已授权目标姿态改变应撤旧batch，不强写服务器/继续伤害。 |
| `20261108-211044-080-540292506bff4117935282b299daa1e8` | 目标姿态grant撤权修补后11双Player回调/owner/寿命/雾等GREEN0/4192ms。 |
| `20261108-211254-049-e6abf58f020e4c17b5e1445c76760d48` | 额外不支持/非均匀shape拒绝控制GREEN0/4565ms。 |
| `20261108-211651-236-9ebc878925f64bce8c4970edc841ea6f` | 取消第二目标Player层仍造成两伤/两contact/seq2，实际target-layer RED1/4090ms；之后窄相每次重验logical Player层，旧grant不能越权。 |
| `20261108-211853-047-70cd5f4ade264c3999c9f1c4e02b7c28` | 12回调/拒绝控制GREEN；随后补真正disabled-before-Ready和自然键盘移动覆盖，不以站定样本替代正常用法。 |
| `20261108-212426-079-ccd43b4394214895b29a0e03317157cd` | 原键盘左行Node179.38889而BodyGetState仍180，直接推翻root pose缓存应相等的前提。不是改物理/削弱动作取绿。 |
| `20261108-212919-975-ea46a83d10e347e483fe53c5d6bc5125` | 补充独立current-pose RED：旧native body在x>600、当前Scene在180，手算原生宽相0命中；齿轮HP5/seq0漏掉已进入的当前角色，不能只删一致断言。 |
| `20261108-213239-098-4eab18216b7044d89357fd22b73b6674` | 修补后最终arc严格GREEN，0/6256ms、stderr0、无超时/ERROR/WARNING/guard，原55秒cap。全部唯一markers通过，作为最终Arc资格，不重复筛绿。 |

最终修补沿用项目Incinerator的本拍CharacterBody姿态+真实原生shape/local/RID/尺寸校验，并把有界同World注册player组与原生候选联合；保留查询饱和拒绝。BodyGetState和宽相可晚于本拍MoveAndSlide，不再拿其缓存root姿态强行要求相等。已授予目标若被回调移走仍撤旧授权，logical Player层被撤也不能补伤；没有BodySetState强写或虚构伤害。

原生最终覆盖：保存/CacheMode.Ignore重载、原Player胶囊20/72、全周期弧顶一次伤害/弦内安全；disabled-before-Ready真实阶段、暂停/hidden/disabled/queue、全未来弧占用、实际自动运动、快照导出编辑隔离、reset/reentry/ID不回退；manual native owner（无Collider子节点）及Capsule/Circle/Rectangle、多形状空者先行/一次伤害、不靠无敌帧；12双Player回调（含Always暂停/目标移位/目标层撤权）、2非法形状；native宽相为空但本拍移入仍伤害；左右真实引擎键盘输入各30物理帧、角色移动/受伤/存活且机关授权不失效；真实墙/距离/staged/暂停下的当前圆雾命令。不是硬件键盘、正常像素/音效、正式地图或相对Player CCD证据。

## 冻结版本与审查终态

1921全输入SHA`1B8A1BA782F26C5E64980E1D506718E4E913F2980F66C10346B16B73F36F29AF`；六产物清单`51F4317588FB44AF10B3C3085244CD01478BB108F6643A22DE788641A21F0AB4`。八文件精确当前/基线pins及全reviewfocus见`.superpowers/sdd/2026-11-08-arc-gear-kernel/review-package.md`。一次fresh `arc_kernel_fresh_review_v330`已完成C0/I0/M1，八当前/三基线pins及最终after副本核准，五个reviewfocus有界通过；不二审、不用该结论预签路线或全关。

owned旧三专项53158已终结exit0：moving-gear `213511-433-651ab81214974815a20a9265504cf2cb`、recycling `213548-083-30ad5266e9a544d392f9befa371c0cee`、savedline-layout `213647-743-ae69560f0134410da9eb006273d175e4`。原始独立审计三案均strict PASS：5667/26891/28669ms，原cap55000；快照245/232/246对，零差异，18份产物复制匹配。加Arc共四案970对/24份复制，source1921/六产物/109真用户文件前后一致。

冻结原11阶段串行批次66477已真正终结exit0；加Arc与旧三专项，共15个指定phase全部独立strict PASS，另保存独立cold PASS，不借旧版拼通过。全部engine0/noTimeout/failure空/guards0，ERROR总计0；只有后段原合同四条负例WARNING，后段stderr23311字节，不能写成全案stderr0。

以下case均在证据根，目录名为`phase-20261108-后缀`；Arc/旧三已在上文具名。

| phase | case后缀 | 原生ms / 原cap ms | 快照对 / helper行数 |
|---|---|---:|---:|
| thruster-rig-compatibility | `214427-442-63b7dc645e714ea4bfdeda06955f1a03` | 38593 / 55000 | 260 / 9 |
| thruster-presentation-rig | `214546-988-887416e6faa14edea2eb48921f5b31ff` | 18561 / 55000 | 248 / 10 |
| thruster-ground-gait | `214641-096-01ad8b0c83314e52aab3fec310dc9459` | 29955 / 55000 | 255 / 13 |
| thruster-recovery-compatibility | `214746-251-1a7452f7d8234ae498c81f5733c20d85` | 17005 / 55000 | 243 / 9 |
| actions | `214837-837-1a7365f35131477cbec6aa6439d8464d` | 10990 / 55000 | 233 / 9 |
| models | `214924-596-325a6946e5c44d88831cb036b4bb0fff` | 504 / 55000 | 231 / 9 |
| prologue-pre | `214959-105-e6e5ae70186c48fabc312028ff86d2f0` | 155978 / 180000 | 271 / 10 |
| prologue-post | `215313-192-526796bee7bf466a9892b7709a3a257c` | 196473 / 300000 | 274 / 10 |
| thruster-route-main-diagnostic | `215706-911-681c49f31f224e63a4c18ef20957e73c` | 257448 / 600000 | 300 / 9 |
| thruster-route-exploration-diagnostic | `220202-769-9127c01b4e9e4f96b101cad97d696697` | 325154 / 600000 | 300 / 9 |
| resident-save-compatibility | `220807-065-802477fe064841acad9c7bcec110163f` | 12831 / 55000 | 250 / 9 |

普通前段enter44/return44/executed44齐全，未见Handle/FATAL；后段10suites、executed/classified75、retired0/fail_closed=true。后段四WARNING精确为seamless故意抛错observer1、room-profile非法VisualScene根1、flow-registry拒绝Completed→Recovering2，均有scope回执；196473ms不签旧180秒或目标硬件性能。

当前两路线都真实从标题Start、同进程持续Player经Boss死亡到有实体支撑出口Completed，恢复0/0、完成事件/次数各1；Main encounter激活/击败8/8，Explore11/10，不冒称全76清场。普通跳分别21/33次均grounded，coyote/buffered/assisted接受0；离地Space边沿2/8未变成普通二段跳。完成后held60的位置/body/save保持，physics禁用而velocity仍(260,0)，不是测试强写静止。Main实际save/terminal/held SHA`0A266C6A410B5EA436ECB1BDBD28BA3CF141792D2A90C0F10423E32FC9335517`，Explore三者SHA`24E346D9DBB459770261278AEF8A7726E0E2584078FE1BCA8ECE1484F5D43C24`。

Main本次ground seam452→453相邻physics、process611→613/publication605→607；账本110samples=55I+55P，含pair111行，不误称固定128条采样。walk46/handoff1/stance1、max sole step.00024414062；Explore walk45/handoff1/stance1、max.00013647876。旧v328缺帧失败保留，这不是其根因修复或全采样通道无遗漏的证明。

save暖writer44772 / reader43064为真实独立原生进程，cold0/1495ms，主/cold stderr0。六witnesses、旧schema及只读链控制通过；actual map-roundtrip与old-core-only SHA和cold回执匹配，不写真实用户存档实验。

独立审计全部15案3835快照对、差异0，90份实际封存binary复制90/90匹配。最后仅一次current rg+imported重新枚举1921、rg0，路径集合/每文件hash与lastcase零差异；当前6产物、109真用户文件、最后case的helper9/9一致，canonical仍`8AB8E9B7BF929EC50370EC32D787F4A49DB1A08928830EC400FC6F3A80DA27BB`。helper随phase实际9/10/13，不统称所有案9；各案内部稳定。上述只是本次冻结产物技术资格，不冲销任何历史失败、资源根因或正常AV/人体验收。

M330-1（延期）：manual-native正例中Circle与Rectangle同时命中，一条窄相分支始终miss可能被另一条遮蔽；未来补各自独立native正例，保留empty-first/dedup组合。当前未证生产分支错误，不在本轮顺手精修；旧所有Minor保留。

下一正式接入只读候选：实体`UpperGearGalleryTwo`下表面world y3716作锚，Basin local起点(2040,356)、PivotOffset(80,-240)、angle -0.6435011、period3.2、r24；全包络world x3296..3504/y3932..3992.9822。上方没有顶棚的gallery顶面已排除；下方原walkway y4116供步行备选。静态筛查没有占用实心地形/50居民包络，但仅余约51px站立头顶空间、最近居民包络35px；不是通路/跳跃/战斗/正常观感批准。须显式设Damage2（默认5会一击杀死满血Player），再原生证明安全走与有代价的错误高跳，不把纯路过装饰算挑战；当前未改地图。

后续[布局设计](../superpowers/specs/2026-11-08-under-gallery-arc-placement-design.md)与[执行计划](../superpowers/plans/2026-11-08-under-gallery-arc-placement.md)已保存，仅规划未执行；整个冻结批次未修改产品输入。需从当前叶增加唯一节点而非全量重建旧背景，父已有外部依赖则字节保持；新增精确Arc donor路径/配置图语义，不把straight1改成2。另核对当前保存的实际熔炉池与v251报告，纠正总待办T01的过时“尚未实现”；只修状态描述，不用受控旧专项签自然附室/正式视听完成。

## 开发取舍和保留边界

- 自主原T03范围内采用独立电机arc，不问例行审批；代价是该周期驱动不是自由物理钟摆，也不是已批准最终美术。
- 先内核后点位，不缩减T03/原十一项；代价是正式地图此刻仍没有新增Arc，源/builder/白名单/配置图/自然通路后续必须真实接入。
- root保留长期脏树，无Git/worktree/delete，手动ledger配全输入/产物副本；代价是没有隔离checkout或GitHub新版本声明，需逐文件身份审计。
- 用有限候选真弧窄相，不AABB/chord伤害/增大半径；宽相仅float ULP向外保守，窄相无伤害epsilon。代价是数值切点有双精度分类极限，未声称任意实数切点完美。
- 目标被回调移动后等下一正常物理步，不补旧batch伤；代价是即使移动后仍在危险内，这次旧grant也撤回。
- 真实普通移动推翻缓存root一致性前提，改为原生形状+本拍姿态、完整组联合候选；代价是额外有界组枚举，不是相对运动CCD。
- 只无窗口代码/构建/测试，无新媒体/paid/GUI/OS输入/GC/cache/引擎/新dump/真档/Git/自动化/发布；旧v328缺帧失败、历史资源FATAL和所有旧Minor、正式声画/资产/真人/原完整十一项仍OPEN。

本轮near-final仅一次mediaCheck，CheckedUtc2026-11-08T14:09:06.7200412Z：15,203,531字节/145文件、Notify=false，新增未达20GiB；没有新媒体/录屏/批量帧、自动化、打包或删除。G可用80,857,174,016字节约75.30GiB。内核Task1完成，原T03正式点位/正常观感与原完整十一项仍OPEN/ACTIVE，下一按已保存布局计划接真实工坊，不重复刷同版内核绿色。
