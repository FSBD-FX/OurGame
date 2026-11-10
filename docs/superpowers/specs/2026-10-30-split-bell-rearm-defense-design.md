# Split Bell 闭甲回位的危险消费者覆盖

原十一项完整Explore推进，不是新玩家机制。v171实际Grip8清场/Grip9后，split Bell在HP1、Armor时攻击2杀死HP1玩家。源代码nonExposed回位分支在新危险采样之前continue并强制松L；这证明覆盖缺口，不证明70帧单次阻塞或刹车振荡。

闭甲分支也读取当前原enemy/foreign危险，真实L防守优先于回位/新K。不得在J或K飞行中用L取消它们；安全后松L，并重新达到原站位error<=1.5、速度<=2、无输入/活盾、原完整gallery支撑后，才按原K/LOS/实际application+1开甲。最后J仍必须当前Exposed/Health且实际掉血，不能用旧applications或曝光缓存。

不改原900实际physics、全路线600000ms、敌HP6/Armor/时钟/伤害、energy、碰撞或位置。保留零恢复/同玩家/同支撑/无额外跳喷。仅test-route代码和少量有界标量；不付费、不新开窗/系统输入/采集、不Git/删除/真实存档/自动化。

以真实原完整地图失败084014…94bfeae2为行为RED（不是构建错误）。直接同版完整Explore验证防御→第三K→真实Health最后J，不另造假HP1/假Player/关闭AI的夹具。若回位仍失败，保存error/vx/direction/idle/neutral/phase/guard证据后再判断，不放宽阈值。
