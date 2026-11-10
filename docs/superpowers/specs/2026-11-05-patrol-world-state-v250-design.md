# v250 · 巡逻查询三个原world状态的实证

## 事实、假设与选择

v249当前geometry022434完成50body/activity后，原RunPatrolEndpointReuse在两个跨world状态SequenceEqual失败，真实issues数组未留；同态fresh/batch差分没列失败。生产CheckPatrol每调用新建局部表，Query/Distinct保留命中顺序。**仅顺序差异**为待证假设，不能直接改断言或声称不是生产bug。

选调用现有完整RunPatrolEndpointReuse的独立普通headless入口，不复制第三套checker；保留原全部条件/计数/几何/三Sync。相较每次先执行50身份搜索，它可隔离当前断言；若隔离不复现，再跑一次原geometry的同一新记录，不把PASS冒充原故障修复。这是开发诊断选择，用户已要求直接持续制作，不重复项目/引擎/常规权限问答。

## 证据与边界

仅测试CS/现runner/host。`RunPatrolWorldWindow(SceneTree)`用原NativeFixture：Initialize→MountSavedSectors→Sync→StopCallbacks→AssertNativePreconditions→PopulateControlledCounterexamples→Sync→AssertControlledPreconditions→原RunPatrolEndpointReuse。借用原DisposeAsync；主异常与清理异常同时存在时保留两者，不能覆盖首因。不生成24 profile包/50census、不改保存场景、正式游戏或碰撞/原生生命周期设置。

原Compare只针对三个名字输出至多3个`RESIDENT_PATROL_WORLD_STATE` JSON：current_player_only / same_inputs_after_foreign_world_wall_added / same_inputs_after_foreign_world_wall_removed。复制已计算fresh和batch的body transform/bounds/有序issues/clear/proof、各有序corridor的bounds/issues/flags及已有count，标physics/process。无重复CheckBody/CheckCorridor/CheckPatrol、不增输入/等待/状态改写、不持native引用。记录异常计数明确，缺行不补猜。

只新增两个独立壁面前提query：使用现成fixture.HasNativeHit和actor已恢复64高的私有body shape，在(11064,1000)查询真实foreign collider ID；Add＋原Sync后必须命中，Free＋原Sync后不得命中旧ID。先输出标量收据再严格断言。初始wall尚未创建，不虚构有ID的前置命中证明。原两个断言及同态fresh/batch有序差分完全保留；新观察fault或缺3行也不能当PASS。

新phase `thruster-patrol-world-window`，普通55s/SourceRows1888；通过时唯一`RESIDENT_PATROL_WORLD_WINDOW_OK original_controls=true states=3 wall_registration=true full_census=false human=false whole_goal=false`及generic CORE，原strict错误/超时/泄漏门不变。原断言失败就是有效RED，不预设应绿。fixture-only player绝不当真实键盘玩家证明。

## 条件修补准入

只有三个真实状态证明wall注册/移除正确、每态batch=fresh且跨态bounds/flags与理由成员一致仅排列变化，才讨论两处跨态比较应按理由集合、同时保留每段顺序及所有中段危险/移除控制。必须另有能拒绝丢理由/多理由/丢段/错bounds/错flags的独立正负控制，不把生产结果重新计算成期望。若wall前提失败或fresh/batch不同，转定位fixture或生产，不改断言凑绿。

当前不改变生产Issues排序/缓存、24/899/HP/能量/单跳/55s/180s/300s/原位置。仅headless，无GC/KeepAlive/cache/引擎/dump/GUI/媒体/付费/自动化/Git/真实存档/删除。正式美术动作、历史匿名FATAL、Main与原十一项继续OPEN，单观察或一次PASS不完成它们。
