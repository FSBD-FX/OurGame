# v251 · 大型销毁池与盆地东附室

## 意图与权限

落实原T01，不以FALL RESET/80px缝/受控深井替代。用户已要求直接持续完整制作、由开发者合理决策；本设计是开发选择，不伪称用户逐坐标批准。仅headless/隔离存储，dirty原地；旧Main899/匿名finalizer与原十一项保持未完成。不生成付费素材，不开窗/OS输入，不动真实存档/Git/引擎/GC/自动化/删除。

## 方案比较与选择

原深井/Steam地板改成死亡坑会破坏安全下降或回环；80px入口缝不足大型；选择在原DesignBounds内向东扩Basin，不移动旧offset或旧节点/敌人/奖励。Basin world(1280,3600,5120,720)，原offset(1280,3600)不变，新增房间(5120,3600,1280,720)。图节点enum尾部追加IncineratorAnnex，optional/null challenge；原12 challenge、76敌人/9残片/1Boss及旧stable IDs不改。

## 完整空间（world Rect2）

上路A(5100,3676,160,24)、B(5240,3756,140,24)接原SteamRejoinTop(4940,3600,180,40)。安全上桥最终(5360,3836,800,24)，较初案下调80px，确保屋顶(5360,3600,1040,40)下有196px净空支持完整单跳；东侧留服务竖井。下路/西岸(4680,3996,760,24)从旧Step4(4530,3916,150,40)下落80px接入。大型池口(5440,3996,720,300)，实际致死Area(5440,4020,720,276)，实体底板(5440,4296,720,8)阻止iframe等待时先入y4304的FALL RESET。东岸(6160,3996,216,24)，东墙(6376,3600,24,720)。

开发补足agent提案的右岸返回：三一向平台均x6240/宽136/高16，y3916、3836、3756，每级80px；桥与三层之间80px水平开口，允许完整40px身体安全下落至东岸，也允许单跳跨回同高度桥。one-way防止低净高顶头；三平台都在池外。西岸不能在旧实心楼梯底部直接起跳80px：另加新房内(5140,3916/3836/3756,100,16)三one-way，入口A也one-way，从服务通道返回原Top。必须native单跳/真实键盘实测，不能仅凭Jump590/Gravity1850理论94px就宣布可达。上桥不是改成强制落坑，玩家可正常观测并原路返回；下路警告在池沿前，不强迫死亡。

节点主矩形只覆盖新房5120..6400，避免把旧SteamRejoinSteps重新归类。新接驳仅以完整玩家支撑走廊作fallback，旧steam fallback优先，绝不矩形吞并旧通道。实际物理返程校正：下入口SteamValves→附室单向；原SteamTop先命中Ascent的CounterweightLift主node，因此上路CounterweightLift↔附室用具名正反TraversalSegment，不假称返到下Steam。只为这对确实完成单跳返程的精确边放行，Boss/出口门禁不改。附室相机focus只覆盖新房；世界DesignBounds与存档layoutVersion2不变，增量discovery ID进注册表。scene叶/parent Definitions/MinimapTerrainCatalog必须一起按生成器保存、校验；不能沿用锁143solid且要求Bounds不变的旧同步器。

## 真实伤害与生命周期

新CanyonIncinerator为Area2D/ICheckpointResettable/ICanyonTelegraphSource，单一匹配矩形，Size范围有限正数。每玩家每.1s独立尝试，source为本节点instance ID、local sequence单调非零且reset不归零。先按receiver ID登记去重/冷却，再调用PlayerGuardedDamageReceiver；amount为当时有效MaxHealth，明确Unblockable/无击退（高温地形不是正面飞弹，开发分类选择）。仍完整遵守Health无敌帧：进入时拒绝不等于免疫永久，持续接触才按后续新identity再试；离开后无欠账伤害，不直接写HP/清iframe/请求Respawn。

仅当前native重叠且当前实际碰撞体仍交叠、玩家/receiver/health有效可处理、同world、无staged/queued祖先、HP>0时交付。暂停/标题/卸载/恢复禁止。外部回调后复核epoch/owner；reset清冷却但不复用identity，不从过期overlap杀安全复活玩家。新节点不创建query资源、不Dispose借用shape；实际形状匹配失效则停止危险授权。几何失效测试是控制，不把此机制设计成隐形伤害。

绘制只读同一危险矩形：池面、暗炉壁、余烬/炉条、明确池沿和危险标识，不能用额外光晕扩大碰撞。Fog保护层只在已见、正确viewport/world/原遮挡可见线段上显示真实边界，不穿墙透视。居民environment inventory必须登记新Area为Hazard。源代码表现不是正式手绘资产验收。

## 验证与未完成边界

先实际RED缺机制，再原生fixture真Player/GuardedDamage：被接受的单次致死、真实iframe拒绝后新identity致死、iframe内离开无延迟伤害、复活后旧overlap不二杀、同源再次进入、盾/暂停/staged/隐藏/禁碰撞/异world/非玩家/形状异常/queued祖先；不得以mock伤害或手工Died代替。地图独立literal bounds/旧布局不变/三返回台阶/真实区域保留，小地图与saved parent匹配。随后验证完整空间键盘绕行/回返与主动入坑的一次真实Died→DEATH RESET链；未实际完成的路线或正式迷雾像素/视觉/人工验收保持欠账，不能勾完整T01。原Main899/资源FATAL不因此豁免。
