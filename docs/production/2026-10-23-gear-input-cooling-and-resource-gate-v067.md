# 齿轮输入、冷却与原生资源门 · v067

2026-10-23，Asia/Shanghai。仅KNIGHT：`G:/knight/Knight`，完整目标ACTIVE，未交付正式试玩。

## 目标与实际改动

用户再次要求设立目标并完整、高质量完成原十一项。本轮get_goal为null后已create_goal，无预算、无自动化。沿用[总待办](2026-10-18-project-remaining-backlog-v001.md)已有映射：移动→地图→敌群→机关→流程→引导→雾/小地图→美术→UI声音演出→失败关闭→验收交付。该文第11节FTR是原答复第12项未来预留，不变为当前必做；取消项不恢复。最终资源、同版Main+Explore、真人/性能/发行仍未完成，不勾完总项。

唯一编译源变动为`tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`，没有生产血量、AI、人口、伤害、力学、地图、存档或视听变动。

- 原跳跃循环三处标量观测：离开lane、首次目标接触、稳定落地，循环后输出，无额外等待。
- 实际离开lane后松L，保留所有重叠样本实际盾断言。用真实immutable motion/module的Neutral预测提前松方向，落在平台左四分之一。12稳定样本要求脚底、完整内侧、水平速度≤2、采样轴0；原180帧不变。
- 初始冷却原210帧内用正常A/D面向、J处理近身保存常驻远程敌人，危险时短L。Rust→Bell次序、同玩家、脚底、HP、终态热量/能量要求不变。最新版本不在此窗口主动J飞虫，这个最后收敛尚未执行到。
- 水平J用真实AttackCollider/HurtboxCollider的Shape.Collide、完整变换和拟议±42偏移核范围，不改实际碰撞。持2帧并分离释放采样，finally松直接J/A/D/L。
- 首个新增居民真实J连接触发旧观察器误判。现wave要求receiver===该actor的Health；居民要求exact saved resident且receiver===actor自身转发器。双方还需该Health接受队列末项等于真实玩家HitboxID/本次AttackStartedID；不接受任意代理或别人的Health。独立只读审查未见身份放宽，不等整关通过。

## 六次因果实验：全部严格失败且原件保留

统一根`G:/knight/验证证据/october13-stable-claw/`。各案普通Rebuild、1772项/888C#、无超时、guards0；不同源码不能拼绿。

| 完整案例目录 | exit/ms | 实际边界 |
| --- | --- | --- |
| thruster-route-main-diagnostic-20261023-121647-923-df43a2e7d81647c397ebd398bc563608 | 1/56454 | 只加观测，原冷却HP4→2 |
| thruster-route-main-diagnostic-20261023-121957-595-eb3fb398182145b4bf43c98434f36afe | 1/53303 | 松L后未过热，运动中下一样本目标脚底接触缺失；未证明滑出平台边界 |
| thruster-route-main-diagnostic-20261023-122419-163-36004b98fcbd4a1eb0a411fec9db3abe | 1/57982 | 零速落地后首J使Scatter HP2→1，旧观察器误判接收器 |
| thruster-route-main-diagnostic-20261023-122729-561-d3f8d5dd096443c584663a45f2919e07 | 1/61506 | 两J击败Scatter/HP4→4；82发送边缘仅2实际出手，末E=.489/heat=.286 |
| thruster-route-main-diagnostic-20261023-123054-486-01b50d1f174d479597a9aec815e8981e | 1/61714 | 修J采样/几何/finally后3出手；Scatter p2459/2478两连接死亡，HP4→4/heat0，但E=.843<.95 |
| thruster-route-main-diagnostic-20261023-123315-595-2365daf593034f90a81102d66c288842 | -1073741795/52715 | 最新只攻击近身远程居民版本在Gear前原生FATAL，新策略未被评价 |

首案离开lane p2417 E=.540/heat=.805，首次平台接触p2424 heat=.854，稳定落地p2445 heat1/过热。次案松L后稳定落地heat=.793/速度260。第五案零速落地p2453 E=.554/heat=.7175/盾关/HP4。这些不同冻结观测不是最新整关通过。

最后Main[stderr](G:/knight/验证证据/october13-stable-claw/thruster-route-main-diagnostic-20261023-123315-595-2365daf593034f90a81102d66c288842/engine.stderr.log)首因`gchandle.is_released()`，GodotObject.Dispose/Finalize→GC.RunFinalizers。无Handle-is-not-initialized/WARNING；stderr与external是同一FATAL两份记录。最远旧Abyss完成一次、6攻击/6连接、HP4→4、零恢复；末p2150向Basin下落，未到camp/Gear。未重复相同Main求安静结果。

## 最终同冻六案：5PASS/1FATAL

最后Main失败加有限5回归，不含较早不同源5案。

| 案例目录 | exit/ms | 结论 |
| --- | --- | --- |
| thruster-route-main-diagnostic-20261023-123315-595-2365daf593034f90a81102d66c288842 | -1073741795/52715 | 严格FATAL |
| thruster-free-forecast-20261023-123557-014-277765c76aa941d7a8025cf6e01bde66 | 0/574 | PASS |
| ranged-sentry-danger-20261023-123634-271-fc28ff265d894b87bc0b1aa9113f2c61 | 0/2324 | PASS |
| ranged-threat-policy-20261023-123709-001-ebdcb70bd7c14540b62b929bd46b482a | 0/4180 | PASS |
| keyboard-20261023-123740-910-175c4764b0d34684844f2a1a61eec463 | 0/9721 | PASS |
| actions-20261023-123821-290-85a4f032468d44daacf1f612772b9eb6 | 0/7245 | PASS |

五PASS三原始日志均无ERROR/WARNING/FATAL。六案source SHA256 `C2BE18CF2599595D6AAA15E635ABB802C5EDCDBB21565E4029879987C8182A29`；6bin清单SHA256 `C7BEBD6B7A7BB6E478320B3DA8A87571F93B5D1E05CDD917738FAC50B8F65C12`；Knight.dll `64323D042C6DFFC7B4A1AB9771D1FC66F6903850828D1E06D6A1E63DAB4AFDF1`。普通Rebuild0警告0错误，无额外DefineConstants。30对source/helper/user/continuation/bin清单前后一致；独立代理核36封存产物匹配。所有消费者终态后根核live1772源/最后helper/6bin零漂移，index仍21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C。仅核保护文件hash，不读真实玩家保存语义。

本轮未重跑完整pre/post、四类RID、Explore或显示性能；v066 pre Handle/FATAL、post Motor、四类RID历史继续OPEN，不冒认已修。全50实战/正式视听未验收。

## 资源问题：事实、推理与风险

只读定位v066 pre为TrySpawnCore挂载/激活后的第二次CheckAdmission，普通CaptureOriginalSpawns加载原敌PackedScene。9种唯一原敌未挂父探针在finally Free；PackedScene/序列化子资源不是探针私有，不可任意Dispose。日志缺scenePath/资源ID，不能指认某Tuning为此次坏对象；当前Main也无应用层资源身份。

agent-reach后端元数据不可用，未安装/改配置，改用只读网页检索Godot官方仓库。[#83762](https://github.com/godotengine/godot/issues/83762)有相同随机换场景报错；[#107541](https://github.com/godotengine/godot/issues/107541)涉及tool Resource默认值/重载，与本案普通运行不同；[#112943](https://github.com/godotengine/godot/issues/112943)有类似终结器栈。只是线索，不是4.7.1根因或修复承诺；没有套用强缓存、KeepAlive、GC、禁优化、换引擎或借入Resource Dispose。

下一先对原首个相机fixture及加载/实例化/Free做有界纯标量生命周期定位，保普通人口/AI/资源策略与原清理顺序；已有最小探针对照安静不能再当修复。随后验证最新Gear两J+短L/回能，继续Main+Explore和正式内容。Sentry早期无父节点、相机factory中途异常、audio drain抛错的清理缺口另记待修，未证其导致本次FATAL。

无新媒体/付费/安装/自动化/Git写入/删除/发行。一次本地容量检查Current4838B/base3508B/growth1330B/Notify=false（UTC04:40:39）；只在正常汇报内检查。全部十一项、最终视听/双路/恢复旧档/分辨率与性能/真人体验/ZIP与GitHub读回交付保留，目标ACTIVE。
