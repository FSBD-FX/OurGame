# 半钟刃地面挥刃—悬灯受力接续

目标：推进原V03/V04与R-C/R-I，补齐真实原地挥空时的附件受力来源，不把一次灯摆动视为完整角色动作重做或正常速度美感验收。上一轮v242为实际进展：提示测试修正且原同版宽门7/10，资源FATAL/另一个恢复启动夹具/Main仍红。

当前事实：Timeline先得到权威PlayerActionFrame，二级运动输入却没有普通攻击来源；武器有四段姿态，但零速挥空的灯/系带只能等命中才有接触脉冲。已批准身份是半钟刃、独立悬浮灯与能量系带；已有支撑步态/落地/制动/转身专项不重做。

选用既有权威GroundAttack的Phase、NormalizedTime和锁定Aim，转到当前朝向父空间形成无单位受力驱动；不另读输入、不新增技能计时、不反推命中。开发暂定平滑端值0→-.35→1→.25→0（各段smoothstep，边界同值/零斜率），沿Aim映射，起势反向、发力释放、经过保持、收势归零。仅驱动现有Mantle/Lantern/Tether有界弹簧；ShoulderSpool仍由爪/丝线独占，真实命中ContactImpulse额外叠加。

建议暂定强度：mantle反向位移1.5px横/0.75px竖、角度.04rad横；lantern反向位移3.6px横/2.4px竖、角度.12rad横；tether继续从灯/线轴差推导。所有项先合成再服从原bounds/frequency，旧输入默认驱动为0，尺度均1，不整图拉伸。这是开发选择，非用户已批准幅度；正常速度可见效果和优雅程度仍未证。

架构：两个生产文件：PlayerPresentationCoordinator.cs的纯Timeline派生驱动；SecondaryMotionModel.cs新增optional Vector2 GroundAttackDrive并有限化/叠加到原targets。FormalVisual既有动作后追加顺序及腿/二级槽ownership不改。空中、下劈、爪、盾、受伤、死亡/检查点接管时不新增地面驱动，已有余振自然回稳，不强制清零或补假命中。

不改实际CombatTuning、输入、Hitbox Active时间/伤害、Motor速度/惯性/碰撞、支撑脚、落伤、爪/短喷/取消规则、76普通敌人或地图。仅无窗口内部键盘，G:/knight/Knight唯一根，无付费/GUI/系统输入/额外dump/新媒体/自动化/提交发布/删除/真实存档操作；原时限/HP/失败不放宽，CS文件数量仍1888。

验收：先真实Timeline零速挥空和保存Player的J输入反例RED，再验证四阶段/镜像局部坐标/附加命中/不驱动线轴/退出回稳与实际Formal传递；原弹簧分帧/极值/非法/翻面合同不变。完整原pre/post、动作/爪/恢复/存档/资源及普通Main/Explore按同版继续；严格错误拒收，不借专项关闭历史FATAL。正常速度和用户认可仍需后续授权。

并行相关修正：v242顶部RecoveryFeedbackSurvivesOwnBoundary已有当前expectedPlaying/actualIntro真实RED，仅将单帧等待迁到已有WaitForPlaying。原4个ProcessFrame恢复完成观察预算、音频/VFX/重复请求门不改；若它仍红保留，不顺手扩大预算。
