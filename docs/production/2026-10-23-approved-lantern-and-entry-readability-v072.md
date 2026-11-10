# v072 · 获批独立灯真实接入与入口教学避遮挡

2026-10-23；唯一工程 `G:/knight/Knight`，Godot4.7.1 mono/.NET9。原十一项完整目标 **ACTIVE、未完成**。本轮有真实运行/可见改进，不是最终角色、整关或发行交付；没有更换引擎、恢复自动化、付费生成或写Git。

## 事实、判断与边界

- 已批准独立96×96透明灯图此前未被正常Player加载；实际仍画程序圆环。本轮只替换Lantern的Draw，导出Texture2D接入口，Ready默认载入原PNG，在24×24世界画布绘制。原(-14,-42)轴点、Z3、动作/secondary所有权、单次根镜像与真实fog源均保留，没有复活被拒A2总装。
- PNG源SHA256仍为 `C313BFAAF1A1B8C9994AA6FEBC96D5A9E5C82F740B58305FF3C96696DCC73AB0`，未改像素、无新生成图/音频。其他身体、披风、场景、敌人多为原程序形象，不因此成为最终美术。
- 用户授权两次约30秒显示，无OS键鼠输入；第一次26042ms正常退出并获用户确认看到窗口。实际图暴露教学栏盖住头灯。第二次另获授权，25949ms正常退出；两张原始全视口图已审阅，入口头灯无遮挡。没有把静态脚本朝向样段称自然运动或真人审美接受。
- 固定教学栏320→720，不改字号/左锚，不追角色/目标；只把中英初始attack文案合并两行，武器1/J攻击/K直接发爪无需模式/Shift短喷/A-D反向制动/2-4空槽/耗能与恢复全部保留。真正Label测得两行CN需491px、EN需712px（含28padding）；720右缘768，原minimap左972。原520/三行设想被真实灯Y324保护框否定，不靠缩字、删信息或移动角色过关。
- 入口正常落地，真实待完成LearnAttackMode，两语actual prompt=(48,250.28)/(720,61)，底部311.28；灯保护框=(132,324)/(28,28)，头部约(143,344)/(34,34)。默认入口几何通过不代表扩大教学、全动态路线或所有提示永远无遮挡。

## 实现与复核

`PlayerFormalLayer.cs`只改灯加载/绘制；`WorldTargetMarkerVisual.cs`固定宽度；`prologue-ui.json`仅两语attack。新增`ApprovedLanternRuntimeTests.cs`保存Player Ready/源图/尺寸/轴点/镜像、真实灯光world pose、初始落地双语文字与头灯几何、GPU隐藏灯负控及测试自有失败清理。

独立审查发现显示fixture的audio排空失败可能盖掉原始断言并遗留level。先复现RED210610，再只修该fixture的原始异常+清理异常聚合与owned root补偿释放；恰好两个原异常对象及无关owner存活检查通过。未改生产audio/GC/共享资源释放。

原挂载教学在Intro时就等8次输入，211610实际失败；211857实际边界Intro/pendingTrue/sequence0。只在其一StartNew后加既有actualPlaying barrier，原8次采样、初始支撑、目标/失败计数及完整双语文字断言均不减。

## 原生结果（原件都在G:/knight/验证证据/october13-stable-claw）

各目录全名含下列时间、唯一UUID及provenance/source/helper/user/binary/raw记录；没有覆盖或将失败命名为通过。

| phase / time | native exit / ms | 严格结果与含义 |
| --- | --- | --- |
| approved-lantern205351 | 1 /628 | RED，正常灯缺少Texture2D hook |
| approved-lantern205522 | 0 /845 | 初始binding GREEN，尚无像素证明 |
| approved-lantern-visible205836 | 0 /26042 | GPU专项PASS；人工发现教学挡灯，不是最终视觉通过 |
| approved-lantern210610 | 1 /1460 | RED，注入cleanup盖掉primary且owned root未释 |
| approved-lantern210809 | 1 /4156 | cleanup所有权通过，真实初始五行提示挡灯RED |
| approved-lantern211314 | 1 /5572 | 紧凑copy仍窄栏；实测双语字体宽，遮挡继续RED |
| approved-lantern211532 | 0 /4148 | 新720栏/默认两行/头灯几何GREEN |
| approved-lantern-layout211610 | 1 /5921 | 原12布局/48target+自定义glyph完成；旧挂载输入准备前提失败 |
| approved-lantern211813 | 0 /5419 | 最后生产冻结binding/cleanup/双语入口严格PASS |
| approved-lantern-layout211857 | 0 /7224 | 行为全完成，但退出2ObjectDB，**严格FAIL，未关闭** |
| approved-lantern-visible212027 | 0 /25949 | 实际RTX3060/OpenGL正常序章双朝向像素专项严格PASS |
| approved-lantern-layout212406 NativeVerbose | 0 /7412 | 诊断严格日志PASS，但未复现2ObjectDB，未取得类型/ID；不能代替普通失败 |
| formal-claw-phases212445 | 0 /4497 | 原registry三个真实抓爪表现/取消/占位兼容专项PASS |
| models212521 | 0 /521 | 核心模型PASS，普通产物恢复 |

总14案8严格PASS/6严格FAIL（含5个行为RED和1个退出资源FAIL），并非同冻结全量14绿。最终普通source1776项/892C#；后续仅owner增加exact-layout verbose准入，生产源码相同。该诊断无owner trace/新Dispose/GC/audio改动或预算放宽，原WARNING严格否决保留；raw还有38条unclaimed StringName诊断，不当作资源根治。

第二次GPU隐藏灯负控：右191/左190 ROI像素变化，同姿态正常帧噪声均0。不是整纹理逐像素相等，不是自然动作、正式角色/照明美感或性能证明。

## 留档与保全

修正前：`G:/knight/过程图稿与视频/2026-10-23_悬浮灯正式资源接入/v072`；修正后：同主题`v072b`。各2张1280×720全视口PNG，未裁切/放大/重绘；证据原件和展示副本字节/SHA一致，README含来源/长度/哈希/状态，旧图不覆盖。没有大批原帧或长片。

14案source/helper/user/continuation及binary共70/70前后manifest一致，无guard失败。最后source manifest SHA256 `949513C95DAF0EC114DFE89E9F8562F2FA99776E5246D4445E028F9A57B01723`；binary manifest `84CF5790C26A39C50E7ED5745272B794DDF4E38FAAF0B0041289ED0668EC8F65`。Git index仍`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。root owned进程均结束；godot-ai桥不动，无真实玩家存档语义读取、删除或Git写入。

仅一次工作内媒体检查UTC13:26:52：当前575415B/基线3508B，增长571907B，Notify=false；这不是游戏总大小。没有自动化、后台监控或20GiB停工机制。

## 后续完整目标，不缩减

1. 普通布局退出2ObjectDB、此前PresentationProfile finalizer FATAL/Handle/RID等仍OPEN；本轮verbose没复现不允许说修好。此前serial497的同serial创建/绑定来源仍未知。
2. Main棚侧壁转移三内部候选失败及ExploreHP下降仍未解决；本轮不重跑完整pre/post/正常Main/Explore，保留上轮失败，不能拼局部专项成整关通过。下一须完整Reel轨迹/真实力学与碰撞分析，不猜第四个距离阈值。
3. 现有获批灯已从“仅文件”推进到“正常角色Draw与实际起点显示”；其动作/线缆连接/亮度全场审阅、完整正式身体/敌族/场景/UI/声音、全50自然实战/76人口总账、真人键盘、目标硬件性能、ZIP与GitHub发行等原十一项继续。

完整稳定ID依据仍为`2026-10-18-project-remaining-backlog-v001.md`，不包含取消FTR或另一个UE工程。目标工具当前为active，不重建重复目标、不设自动化、不标完成。
