# v261 非零空中载荷交接

## 事实、推理、风险

v260已修locomotion模板，当前同版19/19普通回归通过；非零AirJet刚体偏转/骨盆偏移仍在期满时被raw Ordinary值立即取代。普通首帧EntryWeight通常已大于0，足端也会立即离开旧姿态。真实模型还有零时间lease退出、两个空中通道均inactive的一拍；单修共同倾角不足以保存这拍脚端。

只在喷射载荷交给普通空中的边界保留**最后实际使用的纯共同angle/vertical shift**及足端entry，不给所有动作再套低通；否则破坏已在同模式时同时间戳直接绘制的合同。不能缓存最终pose/action/secondary，不能冒充喷射lease仍有效。

## 要求

1. 有效Jet→有效Ordinary，第一帧从已显示共同load与足端起步，随后用原presentation elapsed和2/15秒smooth过渡到普通目标。无新增物理/技能时钟。跨边界脚端权重从0开始并受原Ordinary EntryWeight限制；完成后恢复原直接绘制。
2. 固定根的零时间交接保留10槽姿态、真实世界ankle/sole，腿18+18不拉伸/不clamp，主手和该动作应保留的副手握持不破坏。非零action/secondary不可重复烘入。
3. 重复/回退不推进，包括中途；无时间兼容按原等价1/60步完成；30/60/120Hz同一持续时间达到同一目标。只衔接这条边界，不改变有效Jet/稳定Ordinary直通语义。
4. 合格自由空中、两个raw通道都inactive的一拍，可在elapsed0冻结纯视觉载荷和足端；渲染正时间但同物理样本的普通目标仍可缺失，须沿原2/15秒有限卸载到中性/自由脚，而非直接清零或无限挂起。Ordinary到达时从实际已显示的恢复姿态接续。Raw Active却非法、地面/Dead/Recovery/ThreadLatch/Apply/显式cue reset仍立即清旧历史。公开Frame/Mobility/AirJet/资格保持原样，无火焰续命/退款/新跳。
5. 缓存共同角以world-facing符号存储，翻面时重表达；新有效Jet立即优先并取消旧桥接。不是全身转向或全局时间恢复语义的完成认证。
6. 既有自然双朝向Player内部键盘样段继续验证非零Coast自然期满、同Fall/正时间/dash1/jump0与真实世界几何，并实测桥接结束。撤掉v260“Body必须立即等于raw新Ordinary”的现状刻画，不拿旧测试阻止正确修复。

## 边界

G:/knight/Knight原地脏树、原十一项ACTIVE，1898源集，HEAD e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6。仅无窗口源码/内部输入隔离测试；不改物理/难度/素材/GC/cache/资源策略，不动真实存档，不开窗/OS输入/付费/采集/自动化/Git写入/删除。正式美术动作/物理手感R-I/R-C/R-P、历史原生故障、全76/正常硬件真人未验收，不能以本专项关闭。V1暂缺，后续重审现稿，不转授批准。
