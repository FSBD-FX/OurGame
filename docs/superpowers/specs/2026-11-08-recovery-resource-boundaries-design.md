# 恢复兼容的窄资源边界

## 已知与目标

继续原十一项Q05，不另建缩小目标。v321恢复21f895...在第二synthetic fixture清理返回后，下一Audio正式PrologueLevel Load/Instantiate出现两次SwapGCHandleForType，随后终结器SetGodotObjectPtr，共3条ERROR；engine0但strict FAIL。具体失效对象未知。当前Main/Explore及其余10阶段通过不能覆盖此失败。

本增量只给原恢复序列增加标量阶段和原引用身份收据，不做资源修复。不改原四delegate、测试顺序、输入、清理等待、GC/cache/引擎/预算。不把下一次安静运行当根因修复。

## 观测面

- 首Audio正式根load-enter、原Load返回、instantiate-enter/return及原QueueFree前/原await后。
- 两原synthetic fixture的scope（queue-fixture/visibility-fixture）、原Player Load返回/实例、原foreach已获得definition及Scene、level/world/player以及原cleanup前后。
- 既有PackSector/PackCustomSector的原packed与sector：Pack前后、临时sector Free前后。只在恢复trace已启用时输出，不影响其他phase。
- 次Audio正式根的原Load/Instantiate边界，进入业务前已有根ID与旧日志可关联。

只调用原对象已得引用的即时Observe；先IsInstanceValid，再读ID/nativeClass/Node.SceneFilePath或Resource.ResourcePath；GetType为托管类型。无额外Load、GetState/GetScript、InstanceFromId、WeakRef、集合重扫或匿名子资源发现。Loaded透传原表达式已有返回，不保留PackedScene局部跨Instantiate/await。清理后invalid仍带原已知标量ID，不伪造成另一ID0对象。

## 验证及原始失败

新test-only helper仅静态标量状态，最多512行，fault/omitted不能静默通过；disabled路径不读原生属性。纯known-ID选择核先stub，字面量valid42/invalid保留42/未知0须实际CONTROL_RED，再最小实现。此RED只证明诊断器未实现，不冒充新游戏bug。随后一次原恢复phase，严格原门与所有raw保留，失败不重复筛绿。至少models及原pre/post按当前产物验证；若出新失败按名称记录，不称整版完成。

日志必须声明timing_perturbed=true/cause_proved=false。它不能看到引擎旧GCHandle所属对象、内部脚本/子Resource或最终化时刻；阶段关联不是对象归因。任何观测fault只计数，不能掩盖原异常；正常结束明确AssertComplete，异常路径summary complete=false。

## 权限与质量边界

只G:/knight/Knight，无窗口/OS输入/paid/真档/dump/Git/删除/自动化。唯一脏树保留，root独占原文件/canonical/build/native，worker只新增helper。一次独立增量review，C/I一轮TDD修补、Minor留账。新编译产物不能沿用v321正常路线为整版资格。正式动作美术/声画/硬件真人和完整十一项保持OPEN。
