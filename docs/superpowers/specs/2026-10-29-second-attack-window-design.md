# KNIGHT v160 · 同次真实攻击窗口证据

完整原十一项目标继续ACTIVE，不重新定义成功为测试通过。当前G:/knight/Knight，1857源/897C#，143地形/9残片/76普通敌人目标不变；无正式交付/新版像素或真人认可。

## 事实与判定

v159普通pre的第二J已在physics4091启动，断言读取发生在4114，模型Elapsed=.30000004/Duration=.3/Attacking=false。Frames等待真实输入采样，但Settle另外等2physics及3process，不保证还在攻击窗口。当前测试在这个迟到的continuation断言“正在攻击”，不能可靠证明原本要检验的“攻击期间K权威及指导仍可用”。生产输入/攻击并无本次被拒证据，不能延长攻击或补按J来迁就夹具。

v159 layout退出2ObjectDB泄漏仍未定位。本轮唯一普通详细日志运行222124 native0/8804ms没有复现；38内建StringName不等于那两个对象。源审查音频已有Shutdown及自有流释放，混音drain差异只是候选；不改释放/GC/cache/引擎，不把本次安静诊断算修复。

## 需要保持的行为

- 保存场景原角色、真实InputMap J/W/K/Space、原HP、机关、碰撞、攻击时长、恢复与完整原断言语义；不执行系统输入/显示窗口。
- 第二J只一次，AttackStarted计数恰为baseline+1，非零同攻击ID；没有第三次攻击替换证据。
- 优先级100的测试观察器在Combat之后采集真实physics状态，在Guidance之后采集process状态；只保存值，回调不发命令、不Refresh指导、不抛断言或持有借用碰撞/资源。
- 资格必须来自同次第二攻击的live窗口：模型及公开IsAttacking、Elapsed<真实Duration、CanBeginGrapple；process样本必须发生在启动后的指导刷新，并包含同帧的完整实际卡片/目标/渲染文字。不得拿攻击前残留卡片或另一次攻击补证。
- 如果没有在攻击期间实际执行的指导process样本，缺证据就失败，不能强制Refresh/冻结物理/延长攻击或只凭physics旧文字通过。
- 测试continuation即使已自然结束，也用先前不可变窗口证据判断。之后仍验证自然结束、同ID/原计数、机会卡恢复及原30采样完成等待。

## 真实迟到continuation反例

既有descent-j-observation-control保留原四状态读取纯度控制，另调用同一个真实Run夹具的lateContinuation模式。仅把原本窗口断言之后的30输入采样完成等待搬到断言之前；总等待不增、不加J。先验证模型已自然结束且同ID/计数，再让未修复旧live断言产生严格RED。修复后早先physics/process窗口证据应通过，同次已结束快照经同一个窗口资格函数明确拒绝。它证明迟到continuation不能抹掉/伪造早先证据，不宣称固定复现原3process等待的机器调度。

## 范围和边界

只改测试/证据消费者，不新增生产API或玩法。保留原普通descent路径及全部后续爪/盾/气流/下落/取消控制，presentation入口不能混用late模式。完整Main/Explore、资源历史、正式视听/全尺寸UI/硬件真人及发行仍必须独立验收。M1器件生命周期和M2非identitycanvas延期不借本次关闭。

无新增collector/付费/媒体/前台/OS键鼠/Git写入/删除/真实存档语义操作；新日志继续隔离保管。仅手动容量检查，无自动化。
