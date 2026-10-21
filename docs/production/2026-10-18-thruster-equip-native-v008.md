# 推进模块装配原生接入 v008（Task5未完成）

2026-10-18；完整1–11 ACTIVE。只完成Task5的值事务与部分世界资格；尚未保存模块Resource或覆盖正式Player，未开放拾取/背包/UI换装。

## 实际运行

缺失实际Motor装配入口RED165551-a40e8dd6834841708ea9aab8e6b80ecf/549ms后接入只读ThrusterState/原子值提交；实际两模块首步72.01667与93.333336，作者Resource随后热改不改变运行快照，epoch递增一次，位置/速度/HP/预算不变。空中误接受真实RED165837-8b6b2b035f304b30a2d84929415cf95f/991ms后加入完成步支撑资格，第一tick重复请求不等于两tick，第二独立完成步才可装；实际横向乘台world/support均119.99999、相对切向站稳可装。

真实floor查询使用自有驻留期probe，完成步GetRealVelocity相对可信接触点速度严格<1；仅用于装配资格，不能用于撞速。实际多floor接触亦须相对稳定，queued floor/ancestor不授权。Motor控制/存活/燃烧/爪/原始输入与native pending边缘检查，成功仍只提交同pulse配置epoch，不另存速度或次数。

queued支撑误用RED170752-67f4e43caea642e0b854a7a4b36d38d3/938ms、控制禁用后缓存复用RED170906-1b94c3d1c88e4d928b2ba98cf9bf4083/1284ms均保留，分别按退休源排除与失效资格清理修正。通知long常量pattern编译失败170020不是行为RED，已按既有相等比较修复。

pending fixture原先在正在执行的physics回调内临时ParseInputEvent；171206实测physicalSpace/Held=true而本步JustPressed=false、Raw旧，前置错误而非装配产品误接受。仅将同一物理事件移到非physics两步之间，-350观测采样前、-1观测采样后，不加输入执行通道。六窗口真实Space/Shift/二者均存在native边缘，Raw保留组合，拒绝换装但原动作自然执行一次。

## 同版六门

source1689；每门fresh Rebuild0警告0错误，guards=[]，source/helper/user/binary四清单相同，三sink与终态已读回无原生诊断。旧keyboard runner既有wrapper finalizer/drain观察原样保留；新thruster严格阶段未加GC/cache/drain/时限/豁免。13721 batch已全部终态、无owned遗留后记录。

| phase | exit/ms | case |
|---|---|---|
| thruster-equip-guards | 0/3429 | thruster-equip-guards-20261018-171352-922-25b19f7a15fc494b8e67be142ca356a0 |
| thruster-equip-support | 0/1482 | thruster-equip-support-20261018-171431-951-bcf7050006ee4647804914a17d0bc41c |
| thruster-equip | 0/1121 | thruster-equip-20261018-171506-408-cff947dbd7364c3192d49c7a9c76b918 |
| thruster-impact-reentry | 0/1401 | thruster-impact-reentry-20261018-171537-587-c3909c177271485b8f3b314a48fbf6c7 |
| thruster-impact-player | 0/4036 | thruster-impact-player-20261018-171609-831-54d00344e5524385a8b1c42f3c8ea54e |
| keyboard | 0/9842 | keyboard-20261018-171643-533-901d1f7ba6c04ba6ad18f3201b585e0b |

证据位于E:/OurGame/Knight/.evidence/october13-stable-claw/<case>/。完整guard矩阵过：退役支撑、燃烧、暂停/控制禁用后资格、完整速度替换、死亡/queued owner、null/七字段NaN、真实跑动及自然停止两步、六pending输入窗口。仍不是整计划/全资格/正常全关通过；本轮未重跑广域pre/post，不以六门绿关闭旧FAIL。

## 接下来

Task5仍in progress：初始模块Export与实际消费、legacy作者参数诊断、实际builder外部候选安全输出、嵌入Resource与搬移default-cache读回、完整爪阶段/其余负控及最终回归待做。旧BuildPlayer忽略新参数并会覆盖正式Player，候选输出合同建立前绝不执行它。Task6typed视听UI、Task7旧消费者/正常Main与Explore随后。

五历史严格FAIL、七Handle观察、Upper/profile/Bell/Recovery继续OPEN；完整地图/50常驻/最终资源/真人/性能/ZIP/GitHub依总目标保留。不恢复自动化，不承诺十万付费人数，无新展示媒体/付费/Git/发布。
