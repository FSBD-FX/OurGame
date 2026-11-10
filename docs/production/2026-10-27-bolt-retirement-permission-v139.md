# v139 · 弹体退役后的同步命中撤销

工程仍为 `G:/knight/Knight` 的 KNIGHT / Godot.NET。原十一项完整目标 ACTIVE/NOTSEEN；本轮不是整关交付、画面升级或用户试玩认可。用户要求的敌人之间角度优先，以及既有近敌相对后方地形优先，均未修改。

## 实际生产修复

此前普通 BellBolt 的 `Retire()` 会立即隐身，却需延迟关闭原生碰撞。真实 Hitbox 的陈旧投递仍可能伤害尚未命中过的玩家。原生051633有效RED：退役回调实际命中、Connected1、HP5→4；不是编译失败，也不是自然飞行或用户遇到隐形伤害的证明。

初版 projectile TargetFilter 包装通过首次Launch前的控制，但唯一审查发现发射后合法组合规则仍有缺口。053744有效RED：组合规则先允许、再Retire并返回true，真实HP5→4。单次C/I修复将权限移到独立的内部可选 `Hitbox.SourcePermission`：在自定义TargetFilter前后检查，再消耗目标身份。只有BellBolt在Ready安装`!IsRetired`；其他Hitbox未配置时规则不变。无碰撞flush时序、伤害、数值、AI、地图或瞄准改动。

最终053957及恢复后060022均严格PASS，三个完整收据与原SentryDanger通过：回调/同帧/延迟退役拒绝；正常一次伤害；自定义与发射后deny→allow保留身份；回调退役拒绝；居民可用性/整圆领地；无效Launch拒绝；后续有效逻辑Launch可再次造成一次伤害。后者**不是物理池/延迟碰撞复活/旧代回调隔离认证**。测试使用真实保存Player/Hurtbox/GuardedDamageReceiver/Health，但控制夹具关闭Motor和无敌期；明确为queued-delivery simulation，非真人键鼠或自然重叠证明。

五个代码/验证文件：`scripts/combat/Hitbox.cs`、`scripts/enemies/sentry/BellBoltProjectile.cs`、`tests/integration/BellBoltSentryRuntimeTests.cs`、`tests/RunRangedWeaponTests.cs`、canonical `verify-claw-knight-v001.ps1`。Source1852/C#892，无新增CS/UID/美术资源。

## 严格回归与资源对照

| 最终候选检查 | 结果 | 原生耗时 |
|---|---|---:|
| ranged-sentry-danger053957 / 恢复后060022 | PASS，三个权限收据＋原危险测试 | 2459 / 2456ms |
| resident-sentry-default060052 | PASS | 10677ms |
| resident-turret-boundaries054137 | 7行为通过，但RID15，严格FAIL | 11791ms |
| ranged-projectile054218 | PASS | 1150ms |
| ranged-steam-sentry-recovery054249 | PASS | 29474ms |
| models054345 | PASS | 390ms |
| prologue-pre054413 | PASS | 154053ms |
| prologue-post054716 | PASS | 195565ms |
| Main055101 | FAIL，配重区死亡复位，位置跳约2987px | 106994ms |
| Explore055317 | FAIL，原900轮仍SentryHP2、玩家HP2，终态energy0.457 | 142231ms |

不要沿用旧v137案45.7能量解释本次Explore；本次没有证明单一原因，也不增预算、降难度或绕开敌人筛绿。前后段当前PASS不关闭历史Handle/FATAL或资源根因。

初版候选resident-turret053020的15行为全通过/native0，退出却报15个`P12GodotShape2D` RID，严格FAIL。计划先停止，后对尚未运行的八个不同phase做一次显式覆盖盘点，保留全部失败；只改变调度，不放宽检查器或验收。

为避免凭数量归因，做了一次明确的无新增gate对照055856：仅将Hitbox/BellBolt两个生产文件恢复为051633前的逐字节源码，其他当前输入及原15case不变。原始SHA分别为`964832796D2F58E591EC3FBFD76A7FCC1FAD0FDD75683060D95245C14FB151C0`、`442ED508877C9A0C4B274FDEA3C1234EF61D44DBC3DB695BAB1FF265E5F8FEED`。该案仍15行为通过/native0/24874ms、同RID15严格FAIL。因此该退出错误**可以在没有本次生产权限逻辑时出现**；不等于已定位15个RID、排除所有新资源影响或证明玩家内存增长。未新增GC/缓存策略，未释放借用场景Shape。

两次SHA/换行预检在构建及native前拒绝：apply_patch造成旧混合换行中的一个CRLF变LF。确认归一化文本完全相同后，仅机械对齐原换行模式，通过byteSHA才执行唯一一次native对照，不冒称预检是游戏RED。

对照后恢复五个候选代码/脚本，逐字节核对053957留存实体，再重建执行060022/060052均PASS；没有把无gate临时版留作开发结果。原RID严格失败与历史根因继续OPEN，不以对照代替资源验收。

## 版本、审查与未完成项

本轮19个native案例：12严格PASS/7严格FAIL，另两次无构建/native的预检拒绝。没有buildFAIL；初版与最终版区分保留，不宣称19案全部同一候选。所有案在 `G:/knight/验证证据/october13-stable-claw/`；准确案号及物理保管范围见本轮workspace。

唯一审查C0/I2/M0；I1有真实RED→GREEN单次修复，未复审。I2的资源严格资格仍OPEN，现补有来源的无gate对照，不能改判旧FAIL为PASS。七项Declined均在 `.superpowers/sdd/2026-10-27-bolt-retirement-revocation/final-review.md` 有root裁决；v137同次C/I、原20项Minor及全十一项稳定ID不缩减、不关闭。

独立物理保管核对已结束：19案95对清单、9428份实际源/帮助器副本、114份sealed binary逐SHA一致，新增缺失/额外/错配0；当前1852/892及Debug6与最终案一致。11个最终候选案全source同版，初版/RED/无gate组严格分开；无gate对照完整source只差两个指定生产文件。详情 `.superpowers/sdd/2026-10-27-bolt-retirement-revocation/custody.md`。这不修复历史14份helper/LeftPNG等证据缺口，也不算第二次代码审查。

HEAD/index未变，不创建提交/标签/推送，不删除、安装或读取真实存档内容。两条整程仍FAIL；正式美术/连续动作/UI/音频、正常速度视听、目标硬件真人与授权交付仍未完成。没有新过程媒体、付费生成、前台窗口/系统输入或自动化。

2026-10-26T22:01:47Z手动媒体检查：1,431,609bytes/16files，增长1,428,101bytes，未达新20GiB档；不重设基线、不停工、不建立通知任务。
