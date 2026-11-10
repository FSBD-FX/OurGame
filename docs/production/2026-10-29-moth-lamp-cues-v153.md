# v153 — 墓灯蛾起手眼灯接线，完整序章仍在制作

2026-10-29。内部制作编号，不是公开Release或完整序章交付。完整原十一项工具目标已按用户要求设立并实查ACTIVE；没有恢复自动化。

## 玩家可感知的改动

墓灯蛾真实Telegraph现在将金色起手信号接到眼灯绘制；Active最初短闪和真实受伤反馈使用既有白色强度，Recovery、抓爪打断、三physics-frame受伤预算和检查点重置正确清除。既有折翼、锁定俯冲线/落点、伤害和碰撞均保留。没有改变攻击时长、难度、物理、目标选择、其他部件或中性底色，也没有生成新付费美术。

两既有生产C#的变化仅为Timeline→Lamp反馈→实际DrawCircle颜色；另两既有测试C#及canonical负责验证和精确专项准入。仍1857运行源/897 C#；143地形/9残片/76普通敌人目标不变。不是新增敌人数量、正式族系素材或全图美术完成。

依据：[获批战斗可读性2.2/4.2](../superpowers/specs/2026-08-21-combat-readability-presentation-design.md)、[本次窄修正](../superpowers/specs/2026-10-29-moth-lamp-cues-design.md)、[执行计划](../superpowers/plans/2026-10-29-moth-lamp-cues.md)。旧规格的房间/难度/时长已被后续决定替代，本轮只执行仍有效的起手表现要求。

## 实际验证，保留失败

全部证据在`G:/knight/验证证据/october13-stable-claw/`，原件未改。

| 案例/时间前缀 | 原生结果 | 范围 |
| --- | --- | --- |
| moth-lamp-cues 153212 eef2da53 | 1/635ms，实际RED | 真实锁定Telegraph下眼灯仍青色；尚未走到后续路径几何断言 |
| moth-lamp-cues 153354 db3534b8 | 0/740ms PASS | 金色、短白闪、双朝向、真实伤害、三帧清除、抓爪取消、typed帧/非Moth控制、reset及原路径/碰撞 |
| models 153529 7c5e2f36 | 0/710ms PASS | 原模型回归 |
| moth-lamp-cues 153619 2f04db04 | 0/759ms PASS | 同源专项 |
| keyboard 153702 6b59b6ca | 0/12056ms PASS | 原键盘专项 |
| actions 153758 b16d58b8 | 0/7258ms PASS | 原动作/能量专项 |
| recycling-traps 153902 ed1e4cc1 | 0/30528ms PASS | 原齿轮/弹板/组合/教学专项 |
| prologue-pre 154021 8d26a9e6 | 1/134306ms FAIL | 7条Handle ERROR＋首战隐藏恢复教学断言，0FATAL；external镜像同8事件 |
| prologue-post 154325 a0604a7b | 0/221954ms PASS | 原75逐一enter/return＋同owner8次；4条精确故意负测试WARNING，不关闭历史FATAL |
| first-combat 155512 9ee0adc8 | 0/7509ms PASS，仅独立诊断 | visible enemy restored本次提示正确；不替代组合pre，不证明前序污染 |

首个RED之后才修改生产；原保存敌人、实际时间线/真实伤害和Draw消费色被验证，没有mock金色。原Draw常量Glow的兼容观察只用于真实RED，新property实际供DrawCircle使用。十案8PASS/2FAIL（其中1为预期RED），build均0、无超时。最终普通七阶段为6PASS/1FAIL，不是整套绿色。

九类非法专项准入均在构建前被拒，包括大小写、录像、豁免、verbose、identity、三个trace和扩时标志。首次临时观察器误以为扩时应由新guard报错，实际原全局guard先拒绝；纠正观察器后核实，无生产变化/新原生启动。

## 原生问题和下一步

前段初始中英战斗文案、实际J和330/390距离提示已通过；失败发生在隐藏敌人后恢复显示，输出普通机械爪教学。该边界未记录同刻全部资格，隐藏的非resident Rust仍会移动，原摆位靠近地面边缘。因此不能先改文案、冻结敌人或换坐标来掩盖。独立first-combat的一次PASS仅说明未复现；下一补原失败点有界位置/速度/HP/模式/爪/群组/屏内/LOS证据，区分夹具前提和实际刷新问题。

资源调查只推进边界：v152 low-ceiling-bridge.03是scrap-scatter，准入在Instantiate后/ConfigureActor前拒绝，不与v150 precision-missile身份混同。现有托管日志缺原始export Variant→native/GCHandle→setter转换时刻信息；没有找到不改变原路径的安全源码实验。匹配符号原生调试/隔离引擎插桩需另行具体方案和授权，不重复ProcDump，不猜改GC、缓存、默认Profile或强保活。本轮post安静不能关闭历史句柄/FATAL。

## 唯一审查与保管

一次独立代码审查C0/I0/M1；无阻断代码问题，无C/I修复或第二次审查。M1是ledger把RED前置条件写成已过后续几何，现已在授权文档收尾纠正，原日志不动；没有夹带代码Minor打磨。取舍和成本：

- 延续指定脏工作区/已有获批起手设计，不重问引擎或新建工作树；不做Git或删除。最终审美仍需实机确认。
- 只读真实色数据和阶段，不称像素、正常速度、真人批准；头部形体/整套美术仍欠验收。
- 原55000ms专项、严格三sink与既有完整门保留；未重跑Main/Explore，其历史失败仍OPEN。
- v152和其他脏改动不由本次窄审查续签，整体资源/性能/存档/正式交付不获证明。
- 额外一次first-combat仅用于区分实际失败，未回填pre，也未当成根因修复。

分7＋2＋1三次限定审计：50清单对；before选定源2284份/helper52份，after相同重复份数亦逐件核过；60封存bin匹配。零哈希不符/现存未列副本/额外bin。不是每案完整复制全部源码。当前source清单`89D97CCF5D2E8B47F1F083B37C6EE56D994A9FE4873BFA0B803E83143F31AB66`，普通6bin清单`79FB3E61C1C1A822A7ECA9C8395BF06FFC5AF97B086EA7A8282D2D26C781D8D7`，canonical`CD810E4523BA4015AE40087DB03DC9C1ABA864756B9061D6E4D47800F0252C1B`。HEAD/index未变；当前记录的新rg等helper存在且匹配，不恢复旧路径。精确历史进程身份均结束；prePID已被不同出生时间进程复用，未触碰，不声称全系统空闲。

## 交付边界

全原十一项继续ACTIVE：正常主线/探索全程、原生根因及严格完整回归、正式角色/场景/敌族美术、连续动作、UI/声音/演出、目标硬件和用户试玩、版本交付仍未全部完成。不保证Ori同等级或十万付费玩家，不把本版称正式精美完成。

没有付费、窗口/系统输入、再次崩溃采集、引擎/依赖更换、新过程媒体、自动化、Git写入/上传、ZIP或删除，真实存档未作语义读取或改动。单次媒体Check为1431609字节/16文件、Notify=false，未达到新增20GiB；检查不产生定时任务或停工。
