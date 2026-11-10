# v161 · 相机场景操作边界（非内容或崩溃修复版本）

完整原十一项目标继续 ACTIVE。当前工程 `G:/knight/Knight`；与其他聊天的 UE 工程无关。本次用户明确“没有看到游戏画面”，记录为 NOT SEEN，不自动重开、不算视觉认可。没有新玩法、美术、动画、UI、音频或媒体制作。

## 事实、推理与风险

1. **事实**：此前普通 pre 在低 Ascent 的样本和音频 drain 之后发生 Finalizer FATAL；缺少完整清理返回及下一次加载的普通边界，不能只凭最后一行认定故障发生在音频清理内部。
2. **本次检验**：怀疑相机 `using GetSlideCollision` 让另一读取者失效。真实三 fixture 共36次同步检查全部 `same_wrapper=False valid_after=True`，原角色/地面状态保持，故本次不支持该猜测。临时探针已从当前源码移除，封存日志和该次源文件仍保留。没有删除释放逻辑，也没有沿同一未经支持的假设改生产指导系统。
3. **下一步观测**：仅新增普通相机场景的30条标量操作收据，区分三轮的 cycle/sector/high、成功进入/返回和同一根 ID。保存后的根 ID 只当标量使用，不重新解引用已释放对象。
4. **风险与边界**：日志本身会改变时序；没有承诺日志抛错时透明、没有新通用 sink/异常包装/清理框架。缺失返回只能说明“未观测到返回”，不能证明某个内部资源或历史 FATAL 根因。没有更改引擎、GC、缓存策略、物理、帧预算或真实存档。

## 实现与测试

仅现有 `BasinAscentCameraRuntimeTests.cs` 和 canonical 验证工具。真实 `Load/Instantiate/AddChild`、原8帧、原 Playing 准入、每轮12样本及原清理操作保持；PackedScene 观测是同步传递，不新增异步资源持有。成功返回日志不放在无条件 finally 中。此前缓存对比诊断不重新运行或重新认定通过。

严格解析器按实际 stdout 检查30条、1→30、三轮各10阶段、正确 low/high、实例化前 root=0、随后同一正 root 且三轮根不同。最初1阳性/6阴性；唯一审查后增加数字ID别名、无空格坏前缀与30行内语法损坏，最终1阳性/9阴性。这是文本合同测试，不是游戏行为证明；控制报告来自 root 工具转录，不冒称 engine 日志另有独立控制报告文件。

| case（均在 `G:/knight/验证证据/october13-stable-claw`） | build exit/ms | native exit/ms | 严格结果 |
|---|---:|---:|---|
| `ranged-camera-ground-20261030-000144-754-3d1dbd70dcb74fec8e7d42028ea52219` | 0/32814 | 0/27903 | PASS；临时 peer 假说未获支持 |
| `ranged-camera-ground-20261030-000835-341-fdfaa45f78b34f19bc3d2bc6954d167f` | 0/35109 | 0/42575 | FAIL；原生原3轮通过，但新 gate 缺30条收据，是真实可观测性 RED，不是玩法 RED |
| `ranged-camera-ground-20261030-001057-459-c5a6f2f27f7541739e856183e007838e` | 0/39979 | 0/40713 | PASS；实际30边界、原36向上接触/recovery0、解析控制通过 |
| `prologue-pre-20261030-001351-528-dd01f049f12f4070b0a653c752d1e457` | 0/35751 | -1/180619，超时 | FAIL；原180000ms cap；还有7条 Handle ERROR，不是安静超时 |
| `ranged-camera-ground-20261030-001954-713-eff5716ffb9f4c4e9abd0b4701b3cc10` | 0/54281 | 0/40846 | FAIL；原相机通过，新审查反例揭示2项误准入 |
| `ranged-camera-ground-20261030-002227-985-7a1b49401f0f491bbffc9520358313e9` | 0/52195 | 0/35553 | PASS；最终1阳性/9阴性＋实际30边界，C#与001057相同 |
| `prologue-post-20261030-002526-033-c350d97627714a9c84af46ea24e26066` | 0/43715 | -1073741795/85326 | FAIL；真实 Finalizer FATAL，非仅 scope parser 拒绝 |
| `models-20261030-002855-537-f08a0d804f0043d79ea3fafdb7b51373` | 0/37352 | 0/563 | PASS；仅原机械爪两组模型，不覆盖短喷专项/全游戏 |

pre 三轮全部清理返回、随后真实第二J窗口17 physics/2 process成立，最后已到 `held active L`；仍因7条 `Handle is not initialized`（BasinSupply OpenSample/AddChild 堆栈）及原180秒超时严格失败。本次没有 FATAL，不能关闭历史 FATAL，也不能把越过旧位置说成修复。

## 唯一审查与单轮修正

审查 C0/I1/M2：数字ID用字符串去重可被前导0绕过；坏前缀无空格时被忽略；旧坏行控制只触发条数门。先增加三个独立反例并执行原解析器，工具实际记录 control7/8 MISSED、两项误准入，第三项原本已会拒绝；再收紧标准十进制写法与所有保留前缀候选，最终9阴性通过。没有第二轮审查或额外审查者；原 C#观察顺序/生命周期在该窄范围无阻断意见。

审查列出的边界均保留：日志异常不是透明性保证，日志会扰动 GC 时序；cleanup-return 不是全部原生资源已析构/无泄漏的证明；缓存诊断及 identity 构建的原生等价性没测；完整主线/探索、渲染及真人门不在此窄证据内。旧延期不因本审查清零。

后段 stderr 实际有 `gchandle.is_released()` → `mono_object_disposed_baseref:1788` → `GodotObject.Dispose/Finalize` → `GC.RunFinalizers` → `0xC000001D`。wrapper 最后报 scope receipts differ，不能用这个后续报错掩盖原生崩溃。最后 stdout 到 TASK8 `basin_checkpoint`；原75项 enter/return 均0，owner8完成标记0，不能称原75或owner8已完成。

本轮8尝试4 PASS/4 FAIL，全部原生均启动；没有构建失败或通过重跑抹掉旧失败。最后 helper 版本下 camera/models PASS、post FAIL；pre 使用审查前 helper，本轮严格失败保留，未在最终 helper 下重跑，不拼成最终四阶段全套通过。

## 当前保管与下一步

最终只读保管核验：40/40 清单对相同、48/48 封存二进制匹配；3834份现存选定 before/after 实体的SHA和对应路径通过，不冒称全源逐案实体备份。当前1857源/897 C#逐SHA与最后清单一致，无缺失/漂移/增删；与000835原始源相比只有相机测试 C#变化，与001057源清单相同。当前9项helper及Debug6bin与最后清单一致。八个实际 engine PID均已结束，无另行 kill。原生运行期间源/工具冻结，原始失败和副本不删除。

最终清单 SHA256（root 另行复核清单/hash与HEAD/index）：

```text
source-after 381B509484B8D0DF27DFE664B998DCBF5D51A82409FA278BAD1351AB09DFC3DB
helpers-after 858BF55ABBACEF597A7305258B548904A81D4CE0DA6BFF1E52303E10ACB7FA96
fresh-binaries E77D454E853CF07D91BA3A9179E3E88FD998ADCB05285325C870BBCA5A20BD1E
canonical D8250409CE7AA307DDDA1FD719356565CF12441A4FF6AAACC47A4A9A11F4B780
HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6
index 21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C
```

媒体一次性手动检查 `2026-10-29T16:28:21Z`：2,012,371 bytes/21文件、原基线3508、Notify=false，未增加过程媒体，没有定时器/自动化。当前目标工具实查仍 ACTIVE；本轮证据不是完整序章完成证明。

显示仍 NOT SEEN，已有正式资源表未新增项目。仅现有1个测试 C# 与 canonical 工具变化，不是游戏内容更新。下一实际工作仍需解决资源错误及完整路线通关阻塞；探索 Grip8→Grip9 的原居民/原接近首阻挡仍缺同次真实 collider/gate 证据，不用受控组件通过替代，也不改变用户保留的敌人角度优先规则或删除合法地形来凑通过。

## 不关闭的门

历史 Handle/ObjectDB/Finalizer 问题、同版完整 Main/Explore 从标题到 Boss 与有支撑出口、正式资源/动作/视听/UI、正常速度与目标硬件/真人试玩，均继续 OPEN。全十一项不勾完成，不保证十万付费玩家。没有付费生成、新窗口或 OS 输入、额外原生采集、自动化、Git 写入/发布、删除或真实存档语义读取。
