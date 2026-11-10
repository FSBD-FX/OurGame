# v151 — 受伤打断攻击时的肘点连续性；原生资源故障仍开放

2026-10-28；原完整十一项 ACTIVE / NOTSEEN。本轮不是全关完成、原生资源修复或真人视觉验收。

## 局部生产修复

仅修改 scripts/presentation/PlayerFormalLayer.cs 与既有 tests/integration/PlayerFormalVisualTests.cs，无新增运行文件。原24+24骨架、腕点、肩部/节点变换、主副握点、十slot、图片、Timeline、战斗及物理不改。腕点更新时从两种肘解选离前次较近的一支；近零腕长保持前方向，Draw/getter不推进历史。近零非零阈值内长度是小于.001的近似，不声称数学上始终精确24+24；原骨长容差.002保持。

主反例由保存的PlayerTuning与真实Timeline、60Hz生成：Apex稳定16帧，同一次DownAttack第3帧Hurt后恢复攻击，不靠手动跳腕点。原 PlayerArmMotionProbe 的32px相邻发布预算未放宽。

证据根：G:/knight/验证证据/october13-stable-claw/。

| 案例 | 结果 |
| --- | --- |
| thruster-formal-claw-phases-20261028-010554-790-63c2a83c39154ba58ac376c36d7f0d42 | 实际RED，native1/699ms；FarArm/Elbow46.88803超过原32；旧378握点等先通过，修复前Layer实体保留 |
| thruster-formal-claw-phases-20261028-010904-317-7fab4bc6f11b4cb382a312970d8dd022 | 同一反例基本GREEN，native0，严格日志干净 |
| thruster-formal-claw-phases-20261028-011148-542-3beb3f45efca4ceabacff42502151040 | 扩展GREEN，native0/4682ms；12真实方向/朝向/打断组合、2688关节对，最大28.994555；零点/纯读取控制通过；旧378握点最大误差8.996945E-06、472普通帧/1884对保留 |

## 普通七阶段：6通过，后段实际FATAL

根拥有的PTY79323已终止，exit1，V151_BATCH_FAILURES=prologue-post；未重复到绿。全部七次构建0；models/formal-recovery/presentation-rig/keyboard/pre/normal-absence六引擎退出0且严格诊断干净。

后段 prologue-post-20261028-012019-160-8b8a6631829742b69e9c4b0ce287420a：原生退出 -1073741795/164740ms、未超时，gchandle.is_released() 与0xC000001D FATAL。验证器先发现Stdout只有2/3 scope收据，缺flow-registry及PROLOGUE_TESTS_OK；这不是纯验证脚本误报。未改该检查顺序或忽略错误。前段本次通过不关闭v150前段FATAL，也不证明本次动作修复造成或解决资源错误。

## 审查、保管和边界

唯一新鲜最终代码审查 C0/I0/M1；无C/I修补轮、无二审。M1延期：补0<腕长<.001及阈值边界测试，明确上述长度近似，不为文字精度贸然破坏连续性。历史Minor保留。

七案只读审计：35/35清单对一致，3280选定源副本+74helper副本+42二进制各自匹配，0不符/未列文件；不是全源实体备份。14个构建/引擎contract-native-wait身份三联一致，精确PID当前均不存在，0超时；不声称全系统无进程。当前1857运行源/897C#与七案清单相同，SHA256 9380CA28BBBE1B19D43F7251AE38B3E07E23605BC7096949FEFB249593046288；普通6二进制清单 DA5DC34EFF8C7C4A5AEB1F35D77F2828E10935CCE9DE92D1F15ACB685A25CDA5。canonical、HEAD/index未改。

未新跑完整Main/Explore；v148真实死亡失败仍OPEN。正常速度肘形、遮挡、硬件像素和用户显示未验证，不能用无窗口几何GREEN代替精美验收。143实体/9残片/76普通敌人、角度优先规则不变。无付费生成、新媒体、自动化、系统输入、Git写入或删除。

用户新回复仅要求先给微软官方安装与一次采集方案；[方案v001](2026-10-28-native-crash-capture-proposal-v001.md)已保存，未下载/安装/接受许可/附加/采集/上传。下一先等该方案确认；可继续其他不依赖原生崩溃归因的无窗口制作，不扩大GC/缓存/时限。

媒体一次Check：1431609字节/16文件，较本地基线增长1428101字节，Notify=false；不重建自动化、不重置基线。
