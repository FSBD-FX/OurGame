# v197：修复几何测试自身的资源寿命；不替代主线与序章验收

## 事实

只在 `G:/knight/Knight` 继续KNIGHT。原十一项目标仍ACTIVE，范围没有缩减。此次只修改 `tests/integration/RangedAttackGeometryRuntimeTests.cs`；生产Motor、Main试玩、canonical、地图、敌人数值、正式素材均未改。获准范围仍仅隔离无窗口构建/测试；无窗口显示、系统键鼠、真实玩家存档写入、付费生成、额外崩溃采集、自动化、Git写入/发布或删除。用户作废的三段视频没有用于美术分析。

最终同一源码三阶段严格通过：原生几何、几何数学、远程武器模型。原生几何保留原11个行为检查及两变体各8组真实单跳采样，每组分别查询新占据位置与已离开的旧位置；不是删减为原11项，也不是16个独立关卡案例。全部16组 current_hit=True、vacated_hit=False、HP5、实际起跳增量1，两变体mismatch均0，并自然落地。build零警告零错误，stdout/stderr/external无ERROR/WARNING/FATAL，原180s构建/55s原生上限不变，未超时、guards为空。

## 定位与修正

1. 只跑原11项时退出安静；仅增加保存Player加载/实例化/Free、尚未入树且不读取Shape，就出现一次3ShapeRID汇总ERROR。因此Ready、输入、跳跃和碰撞查询不是该控制复现所必需的条件。
2. 同一无树控制只显式结束本次PackedScene加载句柄，退出转为安静。但恢复完整11+16后仍剩一次1ShapeRID ERROR，不能把这个局部控制当成完整修复。
3. 把Shape getter放进同步NoInlining标量helper仍剩1个，故“缩短局部变量/回调捕获”本身未被证明有效。
4. 完全避免取得借用Shape2D包装：从仍挂载的Hurtbox RID读形状类型及数据，使用本次自有Variant的作用域释放，仅返回半径/高度两个float。完整11+16严格退出转为安静。最后把仅此fixture的场景加载改为私有 `CacheMode.Ignore`，避免主动释放共享缓存PackedScene包装；同样完整复验通过。

最终没有Dispose借用的Shape、外部依赖、Space，没有FreeRid，也没有新增GC、等待轮数、错误过滤、ExpectedFailure、跳过或加预算。临时original-only/no-tree条件及helper全部移除。原fixture先前的cleanup时序没有在本轮更改。

本机安装版主依据：`G:/knight/Tools/Godot_v4.7.1-stable_mono_win64/GodotSharp/Api/Debug/GodotSharp.xml`，7089–7092说明Ignore对主资源/内嵌资源私有而外部依赖仍Reuse；181532/181545给出capsule数据height、radius顺序。运行实测验证当前Vector2分支，不靠猜测交换维度。

## 全部案例（失败和诊断控制均保留）

证据根 `G:/knight/验证证据/october13-stable-claw/`。日期均20261101，表内为完整phase后的时间/唯一尾号。所有原生进程exit0；严格结果还检查原始日志，所以exit0不代表通过。下列“3 RID/1 RID”各是一条stdout汇总ERROR，external没有镜像，不重复计数。

| phase / 案例 | 构建ms | 原生ms | 严格结论及范围 |
|---|---:|---:|---|
| ranged-native-geometry-105408-742-ba216f65bc4440278f16c6b08890548c | 27862 | 630 | original11-only控制PASS，full_suite=false |
| ranged-native-geometry-105930-272-182ba32b5d38450c861fe1af9d6ec767 | 27917 | 709 | 无树加载控制FAIL，3 RID汇总ERROR |
| ranged-native-geometry-110514-023-53810214aa0646af9f6b90964fa7baae | 28299 | 613 | 有作用域加载句柄控制PASS，非完整套件 |
| ranged-native-geometry-110632-057-38f9833ddac4494c8e5ab6826e48b8cb | 28153 | 2976 | 完整11+16行为通过，1 RID ERROR严格FAIL |
| ranged-native-geometry-110836-460-eaba0e9a50e54ad4b2e1a52409654538 | 29471 | 3007 | NoInlining getter仍1 RID ERROR，严格FAIL |
| ranged-native-geometry-111001-664-a042b1f723264cae937e20e91123835c | 30797 | 3012 | native标量版完整11+16严格PASS，尚为cached加载 |
| ranged-native-geometry-111125-420-9da27df2dc49492db023927bdb54cc89 | 29157 | 3099 | 最终私有加载版完整11+16严格PASS |
| ranged-geometry-111322-320-2cc5b1e2a75a404f95351982be1f505a | 27382 | 502 | 最终版数学/查询回归严格PASS |
| ranged-models-111419-855-1ea2c68030f741a4bb8d4d4f21a6b6ad | 28128 | 498 | 最终版远程武器模型严格PASS，不是爪模型子集 |

共9个终结案例：6个限定范围PASS、3个严格FAIL；6个PASS里有2个只是诊断控制。最终同源三阶段均PASS，不能把全部9案写成全绿。根已读取最终三案完整stdout/stderr/external、构建和终态记录；没有以观察工具返回超时重启原生进程。

最终源码SHA256：`A359ECA4E37E1E81DF0F597B2C52EB1F5CA2566B8CEFBD2AC910CA8B6BFC238E`。精确基线为v196冻结副本 `37E0B5C602BFED16BE042E02E632B9752E6573ACC3035B44FF50C9F2BB0137A2`，不是拿整个dirty tree与HEAD比较。

最终清单：source1872行、SHA `65214E4FAA7155638FA83040617AF49D4A133B785D1BA4F14DFB21A99FA41713`；helpers清单 `202E6A603FCB59497E6E14BBF2364FA685D90A435918E7E01EBB159B1B9CE861`；六binary清单 `DF7A11E5B70549011A373F8E24BBA1BF3D9EB1F8C008172076DCDB0357661785`；Knight.dll `F57C5BABFFAC76607704CC6FD0DAC44A09EED4AF445999381ACBD5BFCC7F5282`。Motor仍CB65、Main539、canonical7A79；index21610、HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6及原分支未改。

独立保管审计累计45对manifest、4390个选中source副本、108个helper副本、54个bin逐SHA一致；没有重哈前轮v196。11:18:21+08当前全部1872source/10helpers/6bin与最终三案一致；历史a042仅Geometry与当前不同，案内链完整，不强求旧版等于当前。11:18:29+08全部18个本轮owned身份按creation/path核终结，对应18个PID均不存在，无残留测试进程或误杀其他进程。

## 推理、风险与本轮裁定

系统排错/TDD流程使本次先保留失败，再分离加载与Shape包装获取两个边界；最终只修测试资源寿命，而非修改正式物理、伤害或试玩合同。这里的控制是因果边界证据，不是历史三个RID的逐对象身份证明。原11项和16组运行查询是行为回归，canonical严格退出检查是资源回归。

执行裁定（依次保留，含代价）：

1. 使用已授权的连续无窗口执行和冻结副本，不建立worktree/提交/删除辅助资料；代价是保留更多诊断案例，不获得Git发布资格。
2. 原11项控制通过后先做无树加载控制、不扰动Shape包装；代价是多一次有界诊断，绝不把控制PASS算完整套件。
3. 采用项目既有严格退出标准，未采纳godot.md通用的“RID可忽略”说明；代价是资源错误仍会否决相应验收，不会停止其他安全工作。
4. 只作用域释放测试取得的私有场景句柄，不处置借用形状，也不推广为生产共享缓存策略；代价是其他消费者/所有权情况需另证，失败原样保留。
5. 历史三个RID精确身份和完整链未证；只声明本fixture加载/getter边界已实测消除退出错误，代价是不关闭全部历史资源问题。
6. 所有历史Handle/FATAL/Shape错误继续OPEN；代价是完整资源稳定性仍需各自核验。
7. 未运行引擎停止、Dispose抛异常或实例化中断的任意故障注入；新增using的静态异常路径和正常完整运行已核，代价是既有teardown极端故障未获认证。
8. 生产并行共享缓存压力未测；Ignore仅测试私有源图、外部依赖仍Reuse，代价是不宣称生产加载政策普遍安全。
9. 弹体执行顺序、相对扫掠、下降/横移及Main路线不由本次上升采样证明；代价是完整主线仍未通过。
10. 审查者把相关模型/query最终回归留给根核验；根已完整读取三终态并全部PASS，代价是不扩大到其他阶段或声称这两项有独立运行复核。
11. 正式图声/UI/动作、目标硬件、真人、真实存档兼容及发布不在本次headless证据内；代价是原十一项和最终交付仍OPEN，不追加授权。

一次全改动独立审查 `native_geometry_ownership_v197_review`：C0/I0/新增M0；没有C/I修补或二审。v196已延期的GuardedDamage setup独立断言M1继续延期，不能称已修复。所有既有延期和全量稳定ID保留。

## 接下来与交付边界

主线最近的v196失败仍是6749的shot13使HP2→1，不能借本次测试修正续签Main。v196 Explore标题→Boss→实体支撑出口/D60、恢复0/0的历史通过保留，但不是v197当前路线重认证。本轮未复跑Main/pre/post/Explore；只有测试fixture改变，游戏生产代码未变，不重复用旧整线结果拼成新版本验收。起跳时机更改问题尚未收到答复，现行Main合同保持，不自行当作批准。

下一安全步骤：在原合同内继续核对混战跳跃/弹体边界和其余历史资源错误；若要改变起跳合同，必须等待已发问题的实际答复。正式美术、动作、UI/音频及硬件/真人体验仍需后续独立工作和权限，不能用安静构建替代。

本轮无新过程图稿/实录。手动容量检查2026-11-01 03:15:45UTC：7427750bytes、86files、基线3508bytes、Notify=false；未达到新增20GiB档位，不新建自动化，不暂停工作。计划与本次测试资源任务收尾已记入同计划ledger，原十一项目标仍ACTIVE；保留所有失败与控制原始证据，不提交、不删除计划工作区。
