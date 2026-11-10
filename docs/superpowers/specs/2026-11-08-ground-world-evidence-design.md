# 正式世界的共面落脚证据

## 判断与范围

继续v320原plan Step4，非重新设计支撑或新游戏功能。1910当前输入87EB3740...本轮开始重读零漂移；追加同sector盆地保存几何对照已实际原ground phase通过（cfb3d64...／29887ms），但这不等同正式world运行。

正式Main/Explore原策略在首战后已有Task4WalkTo(1360)，自然经过BridgeFarBank→RiftUpperFloor，无需插入移动或改危险。增加test-only被动FramePublished观察，在原Start成功后绑定，在原全路线完成门后断言，并纳入原异常保护finally。复用原headless diagnostic Main/Explore的600000ms，不伪装OS输入/正常窗口，所有旧Boss／实体出口／普通跳／命名门／存档／恢复门保留。

## 证据合同

- 每新physics的发布帧与正式Visual当前帧相同；只在入口seam窗口（x1100..1420、root y600附近）读取mounted两sector、真实Gameplay/Collision节点，不要求后半程永久保留入口。
- 同一level/player/world ID，当前level sector/world sector/root一致；新旧静态body/shape仍入树、未queued/disabled、可碰层、保存矩形世界bounds为(400,636,880,84)/(1280,636,620,24)。不能用MountedSectorIds列表替代实际碰撞有效。
- 实际upward slide collision shape从旧到新，窗口内前后均有非空接地步行；记录CurrentSector（不得要求新shape首次接触当拍world必已commit），随后确实到Descent且旧入口仍挂树/碰撞有效。
- 只有相邻physics、双拍均stance的同一脚才能作连续性比较；至少一例、实际Draw脚与Gait目标一致、原18+18骨长、最差world step<=.15px。缺样严格失败/不认定，不调整路线／预算强凑。
- callback缓存首错，AssertComplete主动抛；不得被Coordinator的observer故障隔离吞成假绿。只观察，不写位置/velocity/input/HP/地图。

## 验证器本身

纯测量pair核有字面量正例、4px站脚突跳负例、无stance负例。先让未实现pair核返回default，真实原生运行在control fail证明验证器缺功能；明确这是test-only验证器RED，不冒充新的游戏故障。实现最小计算后跑真实完整路线。原phase source/日志/预算/规则不放宽。

## 不变边界

只G:/knight/Knight无窗口；无paid／GUI／OS键鼠／真档／dump／Git／删除／自动化／引擎cache/GC/预算变化。唯一脏树保留。正式美术/像素声画/用户认可/目标硬件与原十一项不因观测通过关闭。v320 M1相位回绕/摆脚专门门继续延期，不顺手修。
