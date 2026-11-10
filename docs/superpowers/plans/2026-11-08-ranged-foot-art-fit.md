# Ranged foot-art fitting implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修正原V05可替换脚部分件的底锚，使真实纹理矩形不因居中适配而悬离现有脚踝基线；不改rig或玩法，不冒称正式像素素材完成。

**Architecture:** 保留`GetPartGeometry(string,bool)`作为真实Draw的唯一几何入口，等比适配后只调整foot矩形Y，其余part原居中。测试创建并拥有真实ImageTexture，visual只借用。

**Tech Stack:** 当前Godot4.7.1 .NET／C#及规范PowerShell runner，不换工具链。

**Spec:** `docs/art/2026-11-08-enemy-family-current-state-v001.md`的V05脚图候选及原稳定V05／V02替换接口；不包含尚待选择的家族造型方向。

## Global Constraints

- 唯一`G:/knight/Knight`；原十一项ACTIVE，不切UE、不缩范围。
- 仅无窗口代码／构建／测试；无GUI／OS输入／真实存档／付费生成／新dump／Git／删除／新自动化／引擎缓存GC或预算变更。
- Root独占生产／canonical／native。单个worker仅拥有现有测试CS；不并行编译，不回退脏树。没有新增C#文件，预期1907，按实际清单验证。
- 不修改rig参数、骨长、碰撞、HP、能量、AI、地图、保存场景或素材资格；既有原生错误门、source/bin/helper/user保护保持。
- 只保证纹理矩形底部，不推断透明留边的实际鞋底像素；无任意旋转父级worldY不变声明。

## Review Focus

- 宽脚图悬空：80×10必须`(-8,-2,16,2)`，高脚图10×80必须`(-.25,-4,.5,4)`，不拉伸。
- 非foot误改：torso80×20保持`(-24,-9.5,48,12)`，20×80保持`(-5.375,-25,10.75,43)`。
- 错脚／翻面：4角色、near/far与两侧瞄准／支持水平镜像的真实脚踝变换，几何查询不推进rig。
- 无图／槽位切换：null保持原canvas与UsesTexture=false；替换后几何实际随真实纹理变化。
- 生命周期：Free visual不得Dispose借用纹理，test-only资源在节点释放／解除借用后由测试owner释放；不添加生产测试cleanup入口。

---

### Task 1: 实际矩形缺陷RED→GREEN及回归

**Files:** modify `tests/integration/RangedWeaponPresentationRuntimeTests.cs`、`scripts/enemies/ranged/RangedWeaponVisual.cs`；记录`README.md`／稳定待办／production报告与本plan ledger。不改canonical。

**Interfaces:** 现`RangedPartGeometry GetPartGeometry(string part,bool far=false)`、`RangedWeaponVisual.FootTexture`／`TorsoTexture`与真实Draw消费者；测试PartGeometry helper只加可选far参数。

- [x] Test-only真实ImageTexture控制先完成；四角色foot近远／瞄准／镜像／只读／所有权及非foot／null独立字面量断言，保留原rig测试。
- [x] root运行规范`-Phase ranged-presentation -SourceRows 1907`，先观察实际foot矩形不符的RED，不把build／fixture错误伪称RED。实际d902175...engine1／629ms，宽脚Y=-3对期望-2失败。
- [x] 仅在生产真实等比适配后给foot底部对齐，再同phase严格GREEN；不以test重定位骨骼凑绿。实际ff2e8c...engine0／46620ms，原完整phase及新marker精确一次通过。
- [x] 按影响串行`ranged-models`／`ranged-actor-saved`／完整普通`prologue-pre`／`prologue-post`／`models`，当前source/bin一致、原始错误门通过后才称本修复程序完成；保留每个失败，不筛绿或扩预算。五阶段batch真实exit0、加GREEN专项共6phase同D17960／B4BB0C严格PASS。
- [x] 一次限定独立增量review；C/I一轮TDD修补，Minor保留，不另开重复审查。C0／I0／M0，无需fix／二审；未代签native／后续batch／像素与正常视听。
- [x] 更新真实版本证据与边界，不把旧v317双路线无条件续签新程序集；正式全关／声画／人体验收仍待最终同版，原十一项目标不关闭。见production v318、README／稳定V05／Q01及本ledger。

## Execution rulings

按用户持续自主推进／仅关键缺信息才问，已批准分件接口的定位缺陷无需再次询问美术路线；该路线问答仍待，绝不当作默认批准。Root在唯一现有脏工作树执行测试先行，worker仅写单个测试文件；用原始规范case＋ledger代替Git／worktree／提交／清理脚本。此为bugfix规格，不新增美术设计或工具采购；代价是只改善后续真实纹理适配，不宣称当前全族系外形已变化。
