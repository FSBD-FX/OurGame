# 受伤期间的攻击表现时钟 · v326

原V03/R-C/E08动作可读性修复，不是新战斗规则。沿v325只读候选：真实Combat在非致死Hurt中继续计时，表现ResolvePrimaryAction却提前返回，可能在Hurt结束后补播已过期Burst/Contact。必须先在保存Player上取得真实RED；旧same-state resume不等于批准冻结旧攻击年龄。

## 合同与方案

- 保存Player、实体地面、真实内部W/J、无敌人或Outgoing连接；成对无Hurt／真实零击退1伤Hurt，两朝向。AttackStarted原ID／方向、Combat后priority100观察真实Hitbox.Monitoring开闭，FramePublished自然回调观察phase和呈现时间；不手动调用_Process、不改帧率／数值。
- 控制fixture先成立：原60Hz、真实有效窗、四期、自然结束，Process对Physics累计时间偏差≤2/60+1e-6且正时间发布间隔≤2physics tick。零时间事件可重复但不能掩盖累计时差。正常对照同样零过期Burst/Contact；不满足时钟条件是测试准入失败，不是Hurt功能RED。
- 有效窗首次关闭后，超过2个physics tick仍发布Burst/Contact为过期发力。Hurt必须确实接受、实际出现Hurt、保持单AttackStarted及同ID接续；结束后无第二次有效窗、无额外输入、终态None／回稳。只测Monitoring这个生产激活开关，不冒称碰撞服务器瞬时接触矩阵。
- 若RED确认，仅让已有攻击年龄在Hurt遮盖期间继续按同一delta／原命中停顿前进；Hurt本身仍优先，同ID可按实际时间恢复为Recovery。不得全局!IsAttacking立即None、重启／取消真实Combat，或扩大到爪／盾所有接管。`.025` outgoing hitpause、零时间、不合法delta、过期短攻击、后续freshID及原恢复／骨架合同保留。

替代方案：只改!IsAttacking清理会丢仍在收招的同ID；改真实Combat暂停／重启会改变伤害窗口。采用表现隐藏年龄同步，数值／视觉资产不变，作用域最贴近已批准方向。

当前仅无窗口权限；不做GUI／OSinput／付费／GC或cache实验／新dump／真实存档写入／Git写入／删除／自动化。完整11项目标、正常声画与真人、正式族系／主角资产及历史资源根因仍OPEN。
