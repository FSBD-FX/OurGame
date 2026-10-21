# 恢复退出边界与回归现状 · v032

记录日期2026-10-20，运行发生于2026-10-19；Asia/Shanghai。完整1–11目标ACTIVE，Task6/7未完成。用户仅确认标题可见，没有新动作/长测授权。当前按用户新要求转入存储盘点与G盘迁移，不再启动游戏或新增玩法修复。

## 事实、推理、风险

事实：原P1隐藏发现测试误要求公开RequestRespawn同步完成；真实记录为返回1请求/0完成，玩家处理已停，完成延后。原失败跳过自有fixture清理；cleanup-only保留断言RED。随后测试采用既有completion-only最多4 ProcessFrame观察，保留原两Physics初始化及QueueFree后1 Process尾，并验证reason/顺序/hidden/同level-player/完成时仍停处理。无实际Health致死、不写HP/输入/位置。

推理：完成信号不是重新开启玩家处理的同义词。独立公共完成回调退出测试分别同步移出关卡、延后一拍移出关卡，真实触发生产Resume第一/第二GetTree的data.tree null和ToSignal p_source null。生产只在首GetTree前及两await之间加原有效性检查；原等待、最终检查、恢复处理与_busy顺序不变。

风险：两窗口专项通过不证明cancel、新开局、重挂或所有资源生命周期都已正确；此前匿名Handle/FATAL/音频ObjectDB不据此关闭。普通P1窄叶仍有匿名资源退出FAIL；同源verbose安静只是诊断，不是因果修复。不加GC、等待、禁AI/碰撞、增加血量/预算或日志豁免。

## 原始证据

根目录：`E:/OurGame/Knight/.evidence/october13-stable-claw/`。旧074855编译失败、074949原同步断言RED、075147清理对照RED、075427行为收据通过但2 ObjectDB FAIL、080008同源verbose安静原件均保留。新本地verbose准入仅为已有P1资源调查增加单一phase，实际owner只读prefix合同先RED后14项GREEN，保留所有原诊断拒绝。

公共退出原始生产RED：

- `thruster-p1-recovery-exit-20261019-080434-991-481438c53882444ca076542a8a2641f0`：build0/27122ms、native0/1695ms，仍strictFAIL；deferred窗口两条生产ERROR，完整external。
- `thruster-p1-recovery-exit-20261019-080840-517-99433d943c6e4567a0c7ab7696c07edb`：build0/24615ms、native0/1676ms，仍strictFAIL；两窗口共四条生产ERROR，另4 ObjectDB/2 resources退出诊断。external漏末尾4行，完整stderr为准。原生产controller两案相同SHA `F611C55C70A25D4523F94FB044CF67F259FC234952F4881424F2384DCC2BFC43`。

最小guard后七案同源码/同二进制，含独立exit与六phase回归批。所有build0警告错误、无timeout、guards=[]；严格5GREEN/2FAIL，不是全suite通过。

| 案例目录后缀 | build ms | native exit/ms | 五日志行数 | strict |
| --- | ---: | --- | --- | --- |
| thruster-p1-recovery-exit-20261019-081321-665-582e574d858144d48a2fbe011b25e6cc | 28980 | 0/1780 | 7/0/12/0/12 | GREEN |
| thruster-p1-hidden-recovery-20261019-081544-690-e2a918add2594c7e88fe9732074ef2b7 | 33942 | 0/1776 | 7/0/12/4/12 | FAIL |
| thruster-generated-main-death-20261019-081632-753-78e777a17d944270b920040405d63c47 | 29866 | 0/1561 | 7/0/14/0/14 | GREEN |
| keyboard-20261019-081711-276-7c89f45b65c54dc79dca0ad16ad6e2d5 | 27652 | 0/9870 | 7/0/7/0/7 | GREEN |
| actions-20261019-081755-679-d6aa766bd9d642828769b3001433a433 | 28363 | 0/7299 | 7/0/10/0/10 | GREEN |
| prologue-pre-20261019-081840-095-925723e37fcc4c4ba57d200dee3edbee | 30800 | 0/100528 | 7/0/335/0/335 | GREEN |
| prologue-post-20261019-082059-223-1560393c2ee043cb8de1f521402a2b78 | 28185 | 1/131459 | 7/0/363/119/482 | FAIL |

五日志为build stdout/stderr、engine stdout/stderr、external。hidden实际4 ObjectDB及1 resource占用，四行退出诊断不在external，仍严格失败。post已过P1发现恢复，首错误推进到 `PrologueGuidanceRuntimeTests.UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff:976` 的rift-to-abyss复合结构条件；分项尚未打印，不能说实际驾驶落台失败。75case计划及GameFlow最终matcher未到达。post2WARNING/1ERROR，无Handle/FATAL/RID/ObjectDB；历史错误不因本案静默关闭。

独立只读审计：七案各四库存对、continuation、actual复制及6binary全匹配。source1695（含296 imported）SHA `E5AFE9940F0BC12509F1F7419EB27B316007FF1D9F21EB6BEB72507C84DFD749`；binary6 SHA `A9B1B1FE6437C2DA3D31C440742A93EBA19707D6D3E85087B53ECBAB4F993185`。hidden与pre各多一实际helper，不能把helper库存说成全部相同；共同owner字节一致。所有消费者现已终态，无新窗口。

## 保留边界

没有新增地图/美术/敌人，正式新50仍0/50、24canonical未实现；全部最终内容/正常双路/存档矩阵/真人/AV/FPS/唯一ZIP/获授权核验GitHub仍OPEN，无付费、发布、Git提交或自动化。E盘剩余约0.50GiB，用户新要求盘点并保存后迁移未来必需内容到G:/game；迁移清单独立记录，旧证据不改写/删除。
