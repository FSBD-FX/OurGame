# Task10首次提示的真实就绪边界

目的：解释并修复当前恢复兼容门首次EnergyEmpty缺失，保留首次无假音、真实拒绝、成功后两次重入、16个生产publisher、生命周期清理与真实隔离存档原合同。不是新增美术/物理或缩减原十一项。

已知证据：v241同版thruster-recovery-compatibility151343严格失败，首次期望EnergyEmpty/vfx1，实际空/vfx0；未带首次输入/flow同步读数。夹具只等3个PhysicsFrame，真实启动跨居民准备后才Playing，后者仍是待证假设。

方案：先仅在测试局部观察flow/pending/Input/Combat状态、输入序号/边沿、能量/slot/rejection及四条同步事件；不改原等待/断言，原生重现后再决定修复层。若证明在Intro提前输入，复用既有WaitForPlaying与真实输入采样等待，之后才固定Motor；若已Playing且拒绝已发布，向消费者绑定/基线继续定位，不直接补播。后续生产publisher夹具按同一实际契约核对，保留所有16cue及原键鼠兼容覆盖，不改为直接调用表现方法。

边界：G:/knight/Knight唯一根，原1888输入、原native预算、敌群/数值/按键/真实存档不变。只无窗口及原内部输入；无付费/GUI/系统输入/额外dump/新媒体/自动化/外部Git写入/删除。记录全部RED，当前未解释native FATAL/Main失败不借此关闭。动力、动作语言和正常速度手感仍OPEN。

验收：原兼容入口同时跑完两个原方法；至少先看到有因果状态的RED，再最小修正取得当前专项证据，保留首次1cue/1vfx、两次重入2cue/2vfx及生产16cue和清理。完整原pre/post、相关动作/恢复/资源/存档与双路按当前同版再验证；任何失败照常报告，不加sleep或扩大时限筛绿。不凭此签完整序章。
