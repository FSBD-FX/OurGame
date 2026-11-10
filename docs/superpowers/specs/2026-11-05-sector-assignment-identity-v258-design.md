# v258 · 区域配置替换点身份关联

目的：v257只定位到区域配置serial1076终结调用，尚无native ID/root/sector。新增REF-only同步字段观察，获得同进程真实赋值关联；不是释放修复，不断言所有旧FATAL同因。

只观察Supply已经取得的world.Definitions：before/after原ConfigureLegacyWorld，root/world/index/sector/已有serial/nativeID/stage。复用ObserveResource标量注册，首次记录不覆盖，逐次关联单独打印。每次最多16字段、每个原probe scope最多64快照；超限/无效/日志失败严格失败，不把缺行当成功。不查询Scene依赖、不Load/GetState/Instantiate/GC/cache/KeepAlive、不保Resource跨await/静态保存。非REF普通assembly无诊断类型与事件。

单独sector-assignment-identity-contract只用自有root/world/少量自有definitions测试inactive无记录、真实旧/新数组、重复阶段、首次注册保留、未登记unknown、释放后标量查询；不执行旧完整census合同或把新对象放到原pre前缀。原pre顺序/帧/180秒上限保持；诊断后普通Rebuild恢复。头less隔离存储，原十一项ACTIVE，GUI/OS/付费/新媒体/dump/真实存档/自动化/Git变更/删除发布均无授权。

原地脏树root唯一作者/native执行器；只读子任务核对API。保before/raw全部失败。当前1897源/场景/美术不新增CS文件；诊断代码仅改现有测试CS。当前普通全16必须重验，不用v256资格代签新CS；一次原pre REF及普通恢复，不筛绿重试。原十一项/正式动作美术/正常速度硬件真人保持OPEN。
