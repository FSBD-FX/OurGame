# 短喷世界喷焰与独立声音 · 原生证据 v012

2026-10-18，20:35后记录。完整1–11目标ACTIVE，短喷整合Task6仍进行中、Task7待实施。本报告是开发证据版本，不是正式试玩版号或整关完成声明；继承[v011](2026-10-18-thruster-rig-and-mantle-native-v011.md)，不回写历史原件。

## 事实：实际接入

- 既有Coordinator成为Motor只读journal的唯一展示读取者，按Motor实例及单调序号推进水位；初绑／重入／缺口／换Director／暂停或换房清理世代只静默对齐，不补播旧Started。先提交水位再调用外部展示；同批Started→Ended且最终非Burning不创造旧喷焰或起音。BindVisualRoot不重置水位。
- 真实Formal姿态应用后读取肩核喷口世界坐标；现有World下单一typed effect持续跟随，轴为本次锁定方向的反方向，不重复乘朝向。燃烧结束或owner退出即隐藏并清理；coast不是持续喷火。程序化轮廓无整图拉伸、碰撞或第二速度；低闪设置立即作用于现有effect，使用稳定轮廓，未加全屏闪。
- 独立SFX ThrusterVoice不占普通四槽、UI或引导AI通道。现有自有dash.wav仅作起喷／持续占位：缓存WAV只读，Duplicate一次的自有副本循环，Shutdown清空Stream后仅Dispose自有副本。较长0.65秒测试模块持续超过原0.2秒WAV且只Play一次；实际取消用同一身份播放一次有限preview尾声，正常结束可用release占位，死亡／重置／退场不补尾声。未生成新音频，不称音色已验收。
- 已移除实际Player旧IsDashing上升沿→Dash cue，避免双播。Coordinator Ready／Enter共用幂等订阅接缝，Exit解绑并停止自身展示；同节点重入后的实际Health死亡仍进入Dead显示，无RequestReady。
- 实际保存Player左右键盘喷射、同发布喷口／世界轴、单effect／单voice、重绑、实时低闪、暂停清理而不改燃烧预算、恢复不重播、换房式清理、新燃烧、同owner重入／死亡通过。守盾拒绝Shift和普通反刹不产生成功喷射展示；停渲染期间完成的真实Started／Ended不被补播。
- 正式Player.tscn仍为v009真实builder标准模块原字节，SHA256 `2A77782B3AF69EF9FC72B124AD25E17CB3D92D2F90EEF8B54BF3B23B17A0BBD5`。本轮不改地图、移动／预算、存档、资源价格或已有资产文件。

## 测试先行与修正归因

- 200618-4ee22c2d是测试误用不存在的PlayerActionState.Dead造成构建失败，无engine，不冒称行为RED。改为已有Base.Dead后，200726-2854a864 engine1／561ms真实检出“无持续effect／identity voice消费者”，随后才接产品。
- 200958-e51d9089在实际喷口／起音通过后，失败于测试给BindVisualRoot传了Formal叶节点而非原VisualRoot合同；只修调用层级，不改Profile解析或扩大容差。
- 201053-223d4f7d的早期左右／生命周期行为完成，但退出2 ObjectDB，严格FAIL。后续补较长模块、取消尾声和拒绝负控，不能将早期marker当完整Task6验收。
- 201633-8da352e8／201807-19e646fb取消检查先混合了新physics结束事实与尚未更新的显示帧；诊断明确published仍Burning、endCount0。测试改为原6帧界内等待真实非燃烧显示回执，非额外退出drain。
- 201944-77c46349 engine1／2402ms在显示已结束、endCount1时tail仍False，真实检出nullable/default比较把刚播放尾声再次停掉。最小产品修正为显式audio非空后比较struct默认身份；未删断言或改物理时限。202215-35c406e4诊断版尾声True且自然完成，但资源退出仍严格FAIL。

## 推理：退出资源身份已收窄，尚未修复

教学标量诊断200331-18b469f2（engine0／1847ms）在原teardown前只保存ulong ID、既有Free／await后查valid。离树Main、UI、实际入树Main及玩家／引导节点均释放；实际Motor六个自建ObjectDB查询亦失效。仍有2 ObjectDB警告，故仅排除这些具体身份，不能据数量认定根因。无额外Load／Instantiate／帧／GC／借用Dispose。

短喷202215-35c406e4的opt-in标量＋verbose精确对应最后自有WAV副本：
`9223372095474370190`，所有voice Stream=null后refs=2，自有Dispose／Root.Free后valid=True；退出WAV同ID ref1，另有AudioStreamPlaybackWAV `9223372095491147855` ref1。自然完成的较长模块副本当时refs=1、Free后失效。不是VFX Node泄漏，也不是已发现的漏写Dispose；仍未直接观测哪个server list节点持有最后引用。

本地gh不可用，按agent-reach读取GitHub官方源码，固定准确引擎ref `a13da4feb`，无安装／升级／仓库写入：
- [AudioStreamPlayerInternal](https://github.com/godotengine/godot/blob/a13da4feb/scene/audio/audio_stream_player_internal.cpp)，blob `133d16c78b620f80bed88767c9e6150e2e94d6f2`：Stop通知AudioServer后清自身playback列表；设置Stream先Stop。
- [AudioServer](https://github.com/godotengine/godot/blob/a13da4feb/servers/audio/audio_server.cpp#L1289)，blob `98bb2cebd94299cd2d87bc1ca5fd4d5c73ab1807`：非sample停止转入待淡出删除；混音路径删除list节点，注册主线程析构回调，后续update清理；不是Stop同步销毁保证。
- [Dummy driver](https://github.com/godotengine/godot/blob/a13da4feb/servers/audio/audio_driver_dummy.cpp)，blob `3d1070a6cb67da9972996870c5f610713dd14c56`：线程周期混音；finish停止线程。

上述路径与即时Quit时残留吻合，**不是已证明引擎bug已修复／正式版无泄漏**。不加清理帧、混音drain、GC、缓存或日志豁免，不Dispose借用playback，不以改用静音测试取绿。后续仍需合法释放／退出接缝的确定性方案与相同严格反例。

## 最终同冻结版本17门

batch66928全部终态exit4后才写文档。source1692，共同source inventory SHA256 `619764F39F40F8A06A509AC0001A73590C45604D2ABA0C93CBC31DD000ACE9D7`；fresh Rebuild全部0警告／0错误，source/helper/user/binary四对库存全等、guards=[]。三sink全量扫描并核对关键失败片段；**13 GREEN／4严格FAIL**，不是整关验收比例。

原件根目录：`E:/OurGame/Knight/.evidence/october13-stable-claw`。

| Phase | Case | Engine exit / ms | 严格结果 |
| --- | --- | --- | --- |
| thruster-world-jet | thruster-world-jet-20261018-202354-193-16ef32d38d5840279c9ace8c564a9012 | 0 / 3610 | **FAIL** |
| thruster-mantle | thruster-mantle-20261018-202426-654-f692250fc8cf49b28a01193110d93f94 | 0 / 633 | GREEN |
| thruster-rig-compatibility | thruster-rig-compatibility-20261018-202501-491-2228492ea4cb4417a8c6e32277f69b52 | 0 / 818 | **FAIL** |
| thruster-presentation-rig | thruster-presentation-rig-20261018-202532-196-3953940507f64ab982ede69247a3633a | 0 / 2319 | GREEN |
| thruster-presentation-model | thruster-presentation-model-20261018-202602-940-983e297f114b4ce7b5c01b56cddd388c | 0 / 541 | GREEN |
| thruster-brake-crossing | thruster-brake-crossing-20261018-202632-237-d11ecc56eda04c20b3c370f48c8dbc85 | 0 / 550 | GREEN |
| thruster-presentation-player | thruster-presentation-player-20261018-202702-952-c0a798e1162e4da48ad98ce8c78c9915 | 0 / 3686 | GREEN |
| thruster-teaching | thruster-teaching-20261018-202735-017-819fecb72869476c91a5b176f5f21e9b | 0 / 1743 | GREEN |
| thruster-audio-owner | thruster-audio-owner-20261018-202805-329-a5573c4c0f1d493c9a6a6b7c88841d7b | 0 / 1378 | GREEN |
| thruster-player | thruster-player-20261018-202835-000-cc8332ddcaba432088aa363959379dbf | 0 / 22796 | GREEN |
| thruster-pulse | thruster-pulse-20261018-202931-314-3c5123bf3c534309b0a84897cb6edc5e | 0 / 539 | GREEN |
| keyboard | keyboard-20261018-203000-940-e03182af42f249edaf47574371134f6b | 0 / 9569 | GREEN |
| actions | actions-20261018-203037-191-f912e22f9e804bd583e02a0b3277d83e | 0 / 7102 | GREEN |
| thruster-impact-reentry | thruster-impact-reentry-20261018-203111-025-767bb68a6e674206b807e7abe252daf9 | 0 / 1275 | GREEN |
| thruster-formal-module | thruster-formal-module-20261018-203140-745-e1bde0c4b9114adb8710973566889181 | 0 / 817 | GREEN |
| prologue-pre | prologue-pre-20261018-203210-141-8f74af3088d544c08a6b7290dec294c9 | 1 / 87918 | **FAIL** |
| prologue-post | prologue-post-20261018-203405-015-0e66777760874d17abc6669b4c042629 | 1 / 8139 | **FAIL** |

四FAIL：
1. world-jet全部行为marker过，stderr退出2 ObjectDB，外部日志无该警告；strict FAIL。
2. 原显示兼容子集11formal＋2mantle行为过，但原两Profile负例警告保留；strict FAIL。
3. pre旧Upper lower_to_mid_swing sample.5仍(264.99634,160.6107)，149／150；两Profile warning、四离树GetPath error。未出现FATAL/null不能关闭历史FATAL/null、七Handle或五历史FAIL。
4. post仍BellChain实落(540.0704,1196.0139)，KillZone ../../../../../..找不到ILevelRecovery。夹具和正式恢复接线继续分开判断。

教学本版1743ms严格GREEN，不回写之前两ObjectDB失败、不将未复现当已定位修复。原keyboard／actions既有退出清理保留；新短喷phase没有移入drain。

## 风险与下一步

- Task6的代码消费者已实际接入，但世界声音退出FAIL、历史教学资源原因、正常速度像素／连续听感／真人尚未闭合；仍in progress。程序化喷焰及旧音bank是明确占位，不是最终美术／音色。
- Task7只读范围核对：旧PlayerMotionModel.Step共70调用，69在旧unit兼容，唯一路线参考在PrologueCanyonTraversalTests:1914（PassiveAbyss默认模型）。正式Motor仅StepFree。未来最小只读MotionSettings应转发_model已生效_settings，不从可编辑Tuning重建；模块已有ThrusterState.Module。旧重力门包含旧Step合同，不能单独称新短喷重力验收。
- lower_to_mid旧预测及实际键盘迁移仍待；保留150tick、原落区、危险／碰撞，不通过移地形／改HP／运行中坐标凑通关。完整7任务fresh review尚未执行。
- 完整1–11仍含正式0／50新常驻、24canonical、正式127／候选132地图、机关、最终美术／UI／声音／演出、正常Main与Explore、存档／旧档／真人／显示60FPS及Windows ZIP／GitHub归档；范围不减，未发布／未完成。
- 本轮无新图像／音频生成、展示媒体、付费、Git写入、自动化或OS键鼠。真实用户数据仅保护库存哈希；原改动、失败原件和旧版本保留。

持续账：[总待办v001](2026-10-18-project-remaining-backlog-v001.md)、`.superpowers/sdd/2026-10-18-thruster-player-integration/progress.md`。

