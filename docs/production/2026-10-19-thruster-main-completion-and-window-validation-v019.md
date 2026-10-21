# 短喷主线诊断通关与窗口双路验证 · v019

2026-10-19，Asia/Shanghai。完整用户1–11目标 ACTIVE，未缩减、未完成。Task1–5专项状态保留；Task6音频退出／最终视听、Task7全量迁移／正常双路线／完整七任务复审仍OPEN。本轮主要迁移自动路线控制，不是新增游戏美术、正式敌群或玩家技能。

## 事实、推理、风险

事实：本轮五个原生案例均已终态，最终同冻结headless Main从真实标题／原出生／同Player，完成Boss实际击败、完整支撑出口、Completed保存转换和继续按住D60帧仅一次结算；engine0/197908ms。最终Explore仍严格FAIL，在蒸汽通路下降时落回GearPistonOne，尚未到阀门。此前同冻结Windows Main／Explore均实际运行，但分别失败在神殿链条站位、蒸汽阀站位，未取得正常整关资格。

推理：获批惯性改变了旧自动操作的停距和跨await预检。应按实际不可变Motion／module迁移这些消费者，而不是降低敌人／伤害、移动平台、强制指定抓爪目标、放宽误差或延长原循环。最终Main成功证明当前主线路径在这些真实输入下可达，不证明所有入场状态或探索路线都安全。

风险：headless不能证明窗口绘制、真人手感或60FPS。两次Windows测试的Godot focus_loss均0，但Windows前台sidecar的两个边界均不是own PID；二者并列保留，不推断全程、不归责用户、不把本次算作持续OS前台验证。正式新常驻仍0/50、24canonical及最终地图／美术／UI／音频／存档组合／Windows ZIP／GitHub交付仍未完成；历史音频2 ObjectDB和五项严格失败不因本轮安静退出而关闭。

## 本轮迁移：原合同保留

修改集中在`tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`，只向原InputMap发送物理键事件；没有写运行中身体位置／速度、补HP、改目标resolver顺序／layer、关闭AI／危险／碰撞或增加退出drain。

1. **Grip9末端三站位**：原共享walk90／settle12／camera30不重置，改为actual ground-neutral停止终点。012951实际4201.385／4435.282／4780.7085均v0，累计84/90、3/12、0/30；原一次Space、真实FinalCatch与linkage随后通过。该案仍在Courtyard失败，不回写成整路成功。
2. **Gear Bell**：仅keyboard Gear generic Bell采用原55..64带内60站位、实际neutral预测、两fresh sampled neutral和完整同支撑；运动lift不要求InactiveLower或worldY静止。危险时原L／真实GuardSource优先，J仅真实Exposed；K保原75节奏、900循环和2／8／45时序。prefix2后失格只能在K尚未发出时返回issued=false，检查rawK／NextCastId／application不变，不计threadCasts、不进入45，恢复AttackMode及当前危险L后走原1帧yield。K发出后的错误仍严格抛出。
3. **Gear新增方向保护**：只读审阅发现局部override在原arena护栏之后；最终冻结中在override后重用原arena／cooldown向内或neutral规则。没有本案越界复现，也不把K的full-floor资格冒称任意移动边缘安全证明。
4. **Courtyard Bell**：保原一次Space和12 held样本。首完整上层着陆X只作为候选；neutral惯性已滑入Bell身体时，在原900内用实际停止终点回站、停稳／朝内，再过原native exact-target预检。只有真实准入后才存原CastStanceX供rearm。原012951的静止错选Floor和零K／零application仍保留。
5. **Temple链条站位**：Windows真实RED是5720±3停5723.6006/v0，40用尽而shared仅41/150，K尚未发。只迁移方向预测，保原40准备、12纯neutral、shared150、末端±3／完整支撑／fresh W／exact target／一K和HP。最终Main实际5720.81/v0、23/40+1/12=24/150，原链条application0→1和Boss流程通过。
6. **Steam阀站位**：Windows真实RED是3160±3停3156.3743/vx23.655941，100+20全耗；resolver已选原control/LOS=True，不是目标错误。只迁移同局部actual-neutral预测，原100+20及其既有correction／fresh-neutral语义不变。**最终Explore在更早通路失败，因此该新阀分支尚未实测，不称已修好。**

Gear首次NO_K的当前证据不能借旧011518解释：Windows Explore014133 caller和prefix均仍选同Bell；raw3→3、NextCast3→3、application0→0确实未发K。复合admission没有分别记录子布尔，紧随后Bell已Telegraph／举盾，与威胁变化一致但不是唯一拒因的直接证明。之后两个真实K/application0→1→2、HP1保持、6started／6connected通过。

## 五个原生案例

证据根：`E:/OurGame/Knight/.evidence/october13-stable-claw`。下列完整目录均保留；fresh build全部0／0警告／0错误，无timeout，guards=[]。不以新案通过抵消旧FAIL。

| 案例 | engine／ms | 真实结果 |
| --- | --- | --- |
| thruster-route-main-diagnostic-20261019-012951-883-cf94a5baa66e4f5c858c29876f4d706e | 1／140240 | 三站位／FinalCatch过；Courtyard首Bell预检选Floor，K未发 |
| ranged-route-main-20261019-013758-537-79222d19deb549168ff85dc8618842ac | 1／142232 | 正常Windows；Gear2K／Courtyard3K与三主路远程role过，Temple站位超3px，Boss仍Dormant |
| ranged-route-exploration-20261019-014133-555-e35e7d70fdad4105ab0ddb25902838c7 | 1／86167 | 正常Windows；Gear安全拒发后2K／HP1保持，实际159/170进Steam walkway；阀站位未停稳 |
| thruster-route-main-diagnostic-20261019-014644-815-ad157f2c64b94cc88bbe9d29ac3560d1 | 0／197908 | headless完整路径诊断通过：Boss／有支撑出口／保存转换／held60一次结算，非normal-display资格 |
| thruster-route-exploration-diagnostic-20261019-015036-508-7f57d4d244f944169aea429e80ea0771 | 1／82831 | headless；Gear2K／6连接无伤过，更早Steam下降170/170落回PistonOne，未到阀／远程后段／Boss |

### 最终Main成功边界

同Player `46858765828`从Title到Boss `472345741803`，真实Boss攻击／暴露／击败与门禁执行。最终走到`(7340.7603,1079.9331)`，HP3、恢复0/0、完整Boss floor支撑且越过实体出口右边；Completed计数／信号均1。持续D60后原完成存档字节不变，未声称测得OS写入次数。

Scatter／Burst／Laser三个实际实例均有预告、实际发射、玩家攻击连接和HP0／败亡证据；inner原角色门与outer原terminal门均执行。原生实际键盘见证mouse_events=0／legacy_action_events=0，属于自动Godot键事件，不是人类OS按键证明。

`storage/task10-route-result.json`及`THRUSTER_ROUTE_DIAGNOSTIC_OK`存在。正常显示、human、performanceCertification、wholeGoal仍false；诊断日志中的fullRoute／whole_route_GREEN=false不能被改写为正常资格成功。

### 当前Explore首失败

Steam入口原80+90共享170，不重试。PistonTwo入场`(2931.8345,3799.9333)`／vx170；62帧时`(2689.7148,3808.8982)`／vx−260已离地；170耗尽时`(2640.061,3879.9758)`／v0完整落回GearPistonOne，HP1→1、伤0／疗0／恢复0。没有Valve receipt、结果artifact或success marker。

这比Windows014133的实际walkway成功更早，不能简单归为“阀修复回归”，更不能借后者证明本案通路。下一先核两案入场差异和现有gap-descent策略，保170／HP／不retry，不预先改地形、爪力或时间。

只读续查：成功案PistonTwo入场x2927.1897／vx170，61帧在2689.3997／3808.998／vx−260发松A；本案入场x2931.8345，62帧在2689.7148／3808.8982发松A。两条DESCENT仍move=-1，是命令边界，不是neutral已生效的完成步。原策略仅到x≤2690即松A、盲等walkway，未检查余速或首次中间支撑；Standard实际air-neutral为rate1.6指数阻力，不能使用旧2800匀减速解释。成功案后来横移仅约9.45px且停稳，缺少中间contacts／lift变换／活塞phase，不能断言哪一物体挡速或归于唯一随机phase。

保存静态PistonOne右边2652、lift左边2700、胶囊R20，保守整胶囊中心窄列仅2672..2680；动态活塞active右缘可到2666，使左边界变成2686，因此不能硬编码2676为全phase安全点。下一在原170内使用实际reverse／neutral轨迹及中间顶面／侧边和启用窗口，真实A/D提前制动，补fresh sample／首接触／lift bounds／活塞phase局部收据。落walkway后的旧ground-stop公式尚未执行，应作为后一独立边界迁移，不伪称本案已复现。

## 正常窗口、焦点与性能边界

用户本轮给出前台时段，正常两路结束后已明确释放；之后全部headless，无自动抢焦点／补键／重跑窗口。可选的“是否实际看到两次自动操作”问题尚未收到回复，不把未回复当阻塞或自动认可。

两正常命令真实Windows/windowed1280×720、timeScale1、physics60，无headless／fixed-fps／movie；start/end可见且Godot focused=True／focus_loss0。OS sidecar只采ready（collection前）与finally（异常后、Quit前），均foreground PID22460≠own PID30144／4000。原件无进程名／创建时间，后查当前同PID名称不作为历史责任或全程焦点证明。

正常Main的8109个callback间隔p50／p95／p99／max为15.100／29.755／47.070／293.062ms；Explore4386个为16.758／27.256／41.348／73.234ms。均为带遥测开销的失败路线诊断，不是GPU/present测量或稳定60FPS验收。两路未进入normal资格成功门，没有Boss成功artifact。

## 资格与保管链

全部五日志已完整核对。012951为7／0／1932／37／1969行；正常Main为7／0／2021／37／2058，正常Explore为7／0／464／37／501；最终Main为7／0／2305／0／2305，最终Explore为7／0／437／39／476。FAIL各唯一上述ERROR及同栈，无额外WARNING／FATAL／Handle／ObjectDB；GREEN无独立诊断。external可能在中间插入stderr，逐行顺序已核，不一概称stdout＋stderr简单拼接；Main最终stdout/external内容同但换行字节不同，不混报SHA。

各案source1695（含296 imported，非1695个C#文件）、user109、helper、6 binaries及continuation前后保持。headless各212 actual selected对（207源码＋5helper），normal各226对（213源码＋13helper）与六份实际binary副本均匹配清单。正常两案source／binary／user／normal-protected／continuation同字节；helper清单仅两行排序不同，排序后17行及13份实际helper内容完全相同，不称helper内容被改。最终headless两案全部库存与binary亦同字节。

重新运行真实owner AST版本准入8例和原资格纯合同15例均GREEN；不是engine／route／performance证明。最新pure manifest位于`normal-binding-v019-004cbedee69243c3b10922d13a0e317f/normal-current-source.txt`，UTF8-LF字节SHA256 `32F3E895405ABEB1EDB01D692197F233CE9F91F67AC4B4F087D8186E791310DB`，不与另一文件编码的source-before清单混报。

最终headless同冻清单文件SHA256（逐项实际副本已核；完整六binary在各案fresh-binaries.txt）：

```text
source-before.txt B9ED2BC0428D959C2B89D9538654325D0DAEA55B7FE00A3EE88CEB4A678D4A2D
helpers-before.txt 1BF1160E745C19494AEFB2125C67A50FD59ECA5E1490FFAE8B50B00ABEDF98BB
user-before.txt BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB
binary inventory 31A5514FF4EDB3E2CC4D6D97D17ED95CF1E3334112C84D7B6C67CC671EEFF527
continuation 7EB70257593DA06F682A3DDDA54A9D260D4FC514F645237F5CA74B08F8DA61A6
Knight.dll C44D72D7440783C293AFA794A238BF91CEDD2184D36ECD9CAD9405DB98C7E8E7
Knight.pdb 4786A70AFEFCD92DAC35D82B0B8615CB3F0DB1853A3A16E156CB4FC0D11BECEF
Main storage/task10-route-result.json 4C817301B077B1FE68A55DAF235A102DA64FC9B73EE64BD7E9266345D67B5662
```

生产Player保存字节SHA256仍`2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。所有owned native消费者终态、复查无Godot／dotnet消费者后才维护本报告、README／总账；没有读取真实用户存档语义。

## 其他完整范围：下一接缝调查，不冒认实现

只读核对新50的正式Placement仍全null、24Subtype仍Unimplemented／ScenePath=null；43联合候选是测试临时dictionary，24保存叶仍ExplicitPlaceholder，不能直接算正式入图。

E05下一最小接缝是稳定ID world-lifetime owner与sector退休边界：sector子节点会随卸载销毁；直接搬到world根又失去ResidentVoidContact的精确mounted-sector权限；Dormant不自动停止Rust的重力／MoveAndSlide；checkpoint ResetDescendants会调用旧IEncounterActor reset并可能ResetFull复活。没有生产resident manager／registry、退休交接或状态Capture／Restore调用者。后续应先以test-owned一至两身份验证真实stream往返、受伤／死亡保留、单一StableId、旧sector拒绝与无地形停算，再整合正式人口；目前只完成调查，没有manager代码或新正式出生。

最终检查E盘约6.44GiB可用；未来新增证据改存G盘的请求仍未获回复。未自行迁移／移动／删除旧证据、过程图稿或版本。没有新图像／视频／音频文件展示或生成、付费生成、Git写／发布、ZIP或自动化。完整1–11与全部剩余总账继续ACTIVE，不把本轮主线诊断成功当整关交付。
