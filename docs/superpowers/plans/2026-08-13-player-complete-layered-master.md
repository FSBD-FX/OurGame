# 线轴承载者完整分层母版实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用完全免费的本地流程制作一套可逐件验收的右侧角色双母版，证明身体、装备、连接点和遮挡关系完整，再停止于静态动画准备关。

**Architecture:** 仓库内只保存合同、校验器、审查工具、测试和制作记录；所有可编辑 SVG、分层 PNG 和审查图保留在外部美术工作区。流程先建立不可变部件清单与失败闭合的质量检查，再依次制作结构稿、身体、胸背系统、斗篷、武器与手、悬浮灯与丝线，最后由同一组完整部件组装无遮盖母版和穿着母版。每组都在用户视觉审批处硬停止；工具绝不自动写入“accepted”。

**Tech Stack:** Python 3.14.6、Pillow 12.3.0、NumPy 2.5.2、标准库 XML/JSON、Microsoft Edge 无头 SVG 渲染、PowerShell、`unittest`

## Global Constraints

- 在 `E:\OurGame\Knight\.worktrees\player-art-pipeline`、分支 `codex/player-art-pipeline` 工作。
- 使用 `E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe`；不安装新 Python 包。
- 外部工作区固定为 `E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001`。
- 正式部件画布全部为 `1402x1122 RGBA`，公共脚底基线 `y=1080`；部件保持源尺寸坐标，不在图层文件内单独缩放。
- 角色主朝向固定为右侧；镜像属于未来运行时工作，不在本计划内制作。
- 身份参考为 `docs/art/player/reference/thread-spool-bearer-body.png`，SHA-256 `0358BFEA48D6FF79271A5B14AE03B0396361748E60EB2D9D36EF6A696D7B1B44`。
- 完整身份参考为 `docs/art/player/reference/thread-spool-bearer-full.png`，SHA-256 `726E13EA5AD2BF4BAF6B161CE51CE29BA2CD8ABFC259616471961CBC91F25909`。
- 悬浮灯参考为 `assets/player/thread-spool-bearer/lantern.png`，SHA-256 `C313BFAAF1A1B8C9994AA6FEBC96D5A9E5C82F740B58305FF3C96696DCC73AB0`。
- 不调用宿主图像生成、外部图像 API、付费服务或任何会消耗图像额度的功能。
- 本地 SVG/Pillow 结果达不到当前身份图质量时必须停止并展示差距；不得将结构稿或低质量本地结果标记为正式美术。
- 每个主体部件都必须完整；遮挡片只能叠加，不能从主体文件中剪走像素。
- 所有视觉审批必须来自用户明确回复；自动检查只产生 `ready-for-review`，不能产生 `accepted`。
- 停止边界是静态动画准备关。不得创建动画帧、图集、`.tres`、Godot 场景或替换任何运行时素材。
- 被拒绝的 B 路线工具、遮罩和图像只作为失败证据；不得作为新母版像素来源。

## File map

Repository files:

- `tools/art/complete_master_contract.py`：部件清单、哈希、画布、连接点和审批状态合同。
- `tools/art/complete_master_workspace.py`：建立外部目录、复制参考、写入不可变请求与来源清单。
- `tools/art/complete_master_svg.py`：校验外部 SVG 的图层 ID、画布与禁用特性，并通过本地 Edge 渲染透明 PNG。
- `tools/art/complete_master_review.py`：生成单件接触表、双母版、背部展开图、96 像素预览和边界标注图。
- `tools/art/complete_master_qc.py`：验证文件、哈希、透明度、画布、唯一主体、连接点、层级与新鲜度。
- `tools/verify-player-master.ps1`：运行全部母版测试，并按指定审查阶段准备一次新鲜证据包。
- `tests/art/test_complete_master_contract.py`：合同和部件清单测试。
- `tests/art/test_complete_master_svg.py`：SVG 失败闭合和渲染命令测试。
- `tests/art/test_complete_master_review.py`：合成、缩放、遮挡片和接触表测试。
- `tests/art/test_complete_master_qc.py`：缺件、混画布、错误哈希、重复主体、旧证据和越界连接点的 mutation 测试。
- `docs/art/player/player-art-production.md`：逐关状态与明确审批记录。
- `docs/art/player/player-art-generation-log.json`：免费本地母版尝试和最终证据哈希。

External files:

- `request/master-request.json`
- `request/source-manifest.json`
- `authoring/structure.svg`
- `authoring/body.svg`
- `authoring/spool-core.svg`
- `authoring/mantle.svg`
- `authoring/weapon-hands.svg`
- `authoring/lantern-tether.svg`
- `layers/<part-id>.png`
- `review/structure-uncovered.png`
- `review/structure-dressed.png`
- `review/structure-back-detail.png`
- `review/body-parts.png`
- `review/spool-core-parts.png`
- `review/mantle-parts.png`
- `review/weapon-hands-parts.png`
- `review/lantern-tether-parts.png`
- `review/master-uncovered.png`
- `review/master-dressed.png`
- `review/master-back-detail.png`
- `review/master-all-parts.png`
- `review/master-dressed-96px.png`
- `evidence/qc-report.json`
- `evidence/review-state.json`

The canonical ordered part IDs are:

```text
inner-body,
head-shell, torso-shell, pelvis-shell,
left-upper-arm, left-forearm, left-hand,
right-upper-arm, right-forearm, right-hand,
left-thigh, left-shin, left-boot,
right-thigh, right-shin, right-boot,
front-spool-housing, front-spool-disc, front-spool-glow,
back-core-housing, back-energy-cell, back-core-glow, hidden-axle,
shoulder-collar, collar-fastener, front-collar-flap, mantle,
half-bell-cleaver, left-grip-overlay, right-grip-overlay,
lantern-housing, lantern-glow,
tether-back-connector, tether-energy-middle, tether-lantern-connector
```

---

### Task 1: 建立不可变母版合同与干净工作区

**Files:**
- Create: `tools/art/complete_master_contract.py`
- Create: `tools/art/complete_master_workspace.py`
- Create: `tests/art/test_complete_master_contract.py`
- Modify: `.gitignore`

**Interfaces:**
- Produces: `PART_IDS: tuple[str, ...]`
- Produces: `MasterRequest.load(path: Path) -> MasterRequest`
- Produces: `MasterRequest.validate_sources(project_root: Path) -> None`
- Produces: `bootstrap_workspace(project_root: Path, external_root: Path) -> dict[str, Any]`
- Produces CLI: `python tools/art/complete_master_workspace.py --project-root <path> --external-root <path>`

- [ ] **Step 1: 写合同 RED 测试**

测试必须断言 35 个部件 ID 精确有序、画布为 `1402x1122`、基线为
`1080`、三个参考哈希精确匹配，以及审批状态只能是
`not-started|ready-for-review|rejected|accepted`。同时断言主体部件与遮挡片分属
不同集合：

```python
def test_every_subject_part_is_unique_and_overlays_are_not_subjects(self):
    self.assertEqual(35, len(PART_IDS))
    self.assertEqual(len(PART_IDS), len(set(PART_IDS)))
    self.assertNotIn("left-grip-overlay", SUBJECT_PART_IDS)
    self.assertIn("left-grip-overlay", OVERLAY_PART_IDS)

def test_acceptance_cannot_be_created_without_explicit_review(self):
    request = valid_request()
    request["review"]["structure"] = {"status": "accepted"}
    with self.assertRaisesRegex(ValueError, "explicitUserApproval"):
        MasterRequest.from_dict(request)
```

- [ ] **Step 2: 运行 RED**

```powershell
$python='E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe'
& $python -m unittest discover -s tests\art -p 'test_complete_master_contract.py' -v
```

Expected: non-zero，报告 `ModuleNotFoundError: complete_master_contract`。

- [ ] **Step 3: 实现合同和工作区初始化**

`bootstrap_workspace` 必须先验证仓库参考哈希，再创建 `request`、`authoring`、
`layers`、`review`、`evidence` 和 `rejected-b-route`。将两个身份参考与灯具参考
逐字节复制到 `request/references/`，记录源路径和 SHA-256。不得覆盖已经存在的
`master-request.json`；存在时只校验。

默认审查对象必须写为：

```json
{
  "structure": {"status": "not-started", "explicitUserApproval": null},
  "body": {"status": "not-started", "explicitUserApproval": null},
  "spoolCore": {"status": "not-started", "explicitUserApproval": null},
  "mantle": {"status": "not-started", "explicitUserApproval": null},
  "weaponHands": {"status": "not-started", "explicitUserApproval": null},
  "lanternTether": {"status": "not-started", "explicitUserApproval": null},
  "dressedMaster": {"status": "not-started", "explicitUserApproval": null},
  "smallScale": {"status": "not-started", "explicitUserApproval": null},
  "animationReady": {"status": "not-started", "explicitUserApproval": null}
}
```

将 `__pycache__/` 与 `*.py[cod]` 纳入 `.gitignore`。执行时把未提交的
`idle_layered_prepare.py` 和对应测试逐字节复制到外部
`rejected-b-route/repo-experiment/` 并写哈希清单，然后从工作树移除；不得删除
已提交的 B 路线规格、合同或任何外部失败图像。

- [ ] **Step 4: 运行 GREEN 与真实 bootstrap**

```powershell
& $python -m unittest discover -s tests\art -p 'test_complete_master_contract.py' -v
& $python tools\art\complete_master_workspace.py `
  --project-root 'E:\OurGame\Knight\.worktrees\player-art-pipeline' `
  --external-root 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001'
```

Expected: 测试全绿，CLI 精确输出
`COMPLETE_MASTER_WORKSPACE_OK parts=35`；三个复制参考的 SHA-256 与仓库一致。

- [ ] **Step 5: 提交 Task 1**

```powershell
git add .gitignore tools/art/complete_master_contract.py tools/art/complete_master_workspace.py tests/art/test_complete_master_contract.py
git commit -m "test: define complete player master contract"
```

---

### Task 2: 建立 SVG 渲染与严格证据检查

**Files:**
- Create: `tools/art/complete_master_svg.py`
- Create: `tools/art/complete_master_qc.py`
- Create: `tests/art/test_complete_master_svg.py`
- Create: `tests/art/test_complete_master_qc.py`
- Create: `tools/verify-player-master.ps1`

**Interfaces:**
- Consumes: `MasterRequest`, `PART_IDS`
- Produces: `validate_authoring_svg(path: Path, expected_ids: tuple[str, ...]) -> dict`
- Produces: `render_svg(edge_path: Path, source_svg: Path, output_png: Path) -> None`
- Produces: `verify_stage(root: Path, stage: str, verification_started_ns: int) -> dict`
- Produces CLI marker: `COMPLETE_MASTER_QC_OK stage=<stage> files=<count>`

- [ ] **Step 1: 写 SVG 与 QC mutation RED 测试**

合成 SVG fixture 必须覆盖：正确 `1402x1122 viewBox`、缺少部件 ID、重复 ID、
外链图像、`script`、远程 URL、画布不匹配。QC fixture 必须分别突变：缺 PNG、
错误尺寸、非 RGBA、空 alpha、alpha 接触画布边缘、哈希不符、主体 ID 重复、
连接点落在主体 alpha 外、证据时间早于本次验证、工具擅自写入 accepted。

```python
def test_rejects_subject_connection_outside_alpha(self):
    evidence = valid_fixture()
    evidence.manifest["connections"]["left-elbow"] = [4, 4]
    with self.assertRaisesRegex(ValueError, "left-elbow.*outside"):
        verify_fixture(evidence)

def test_rejects_tool_authored_acceptance(self):
    evidence = valid_fixture()
    evidence.review["body"] = {
        "status": "accepted", "explicitUserApproval": None
    }
    with self.assertRaisesRegex(ValueError, "explicitUserApproval"):
        verify_fixture(evidence)
```

- [ ] **Step 2: 运行 RED**

```powershell
& $python -m unittest discover -s tests\art -p 'test_complete_master_svg.py' -v
& $python -m unittest discover -s tests\art -p 'test_complete_master_qc.py' -v
```

Expected: non-zero，两个模块均不存在。

- [ ] **Step 3: 实现安全 SVG 合同和 QC**

SVG 只允许本地矢量节点 `svg,g,defs,path,rect,circle,ellipse,polygon,polyline,line,
linearGradient,radialGradient,stop,filter,feGaussianBlur,feColorMatrix,clipPath,mask`
及 `data-*` 元数据；拒绝 `script`、`foreignObject`、任何 `href` 外链和 `url()`
远程值。每个主体用唯一 `id="part-<part-id>"` 的顶层组表示。

Edge 固定发现顺序：显式 `--edge` 参数、
`C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe`、
`C:\Program Files\Microsoft\Edge\Application\msedge.exe`。渲染命令使用
`--headless --disable-gpu --hide-scrollbars --default-background-color=00000000
--window-size=1402,1122 --screenshot=<png> <svg>`；找不到 Edge 必须停止。

QC 写 JSON，但只能把当前阶段写成 `ready-for-review`。`accepted` 必须已存在于
`review-state.json` 且包含非空 `explicitUserApproval`、`approvedAt` 和审查文件哈希。

- [ ] **Step 4: 运行全部 mutation GREEN 与 focused wrapper**

```powershell
& $python -m unittest discover -s tests\art -p 'test_complete_master_*.py' -v
powershell -ExecutionPolicy Bypass -File tools\verify-player-master.ps1
```

Expected: 测试全绿；无 `-ExternalRoot` 时 wrapper 只运行测试并输出
`COMPLETE_MASTER_TESTS_OK`，不触碰外部作品。

- [ ] **Step 5: 提交 Task 2**

```powershell
git add tools/art/complete_master_svg.py tools/art/complete_master_qc.py tools/verify-player-master.ps1 tests/art/test_complete_master_svg.py tests/art/test_complete_master_qc.py
git commit -m "test: verify complete player master evidence"
```

---

### Task 3: 制作双母版低细节结构稿并停在结构关

**Files:**
- Create: `tools/art/complete_master_structure.py`
- Create: `tests/art/test_complete_master_structure.py`
- External create: `authoring/structure.svg`
- External render: `review/structure-*.png`

**Interfaces:**
- Produces: `build_structure_svg(request: MasterRequest) -> str`
- Produces: `render_structure_pack(root: Path) -> dict`

- [ ] **Step 1: 写结构 RED 测试**

测试必须证明无遮盖身体包含全部 16 个身体主体、左右肢体 ID 不同、穿着稿复用
同一批身体组而非另画一人、背部展开包含 5 个明确接口（前线轴中轴、背部核心、
肩领、斗篷根、丝线背端），并且所有关节落在相邻主体重叠区。

```python
def test_dressed_structure_reuses_uncovered_subject_ids(self):
    document = parse_svg(build_structure_svg(valid_request()))
    uncovered = ids_under(document, "structure-uncovered")
    dressed = ids_under(document, "structure-dressed")
    self.assertEqual(set(BODY_SUBJECT_IDS), uncovered & dressed)
```

- [ ] **Step 2: 运行 RED**

```powershell
& $python -m unittest discover -s tests\art -p 'test_complete_master_structure.py' -v
```

Expected: non-zero，模块不存在。

- [ ] **Step 3: 实现结构稿**

结构稿使用清晰的平面色和轮廓，不仿冒正式材质：内体深灰、陶瓷骨白、黄铜金、
斗篷青绿、武器橙、能量青、连接点紫。输出四张图：无遮盖、穿着、背部展开、
35 件部件地图。每个关节和接口必须有英文 ID 标签，避免本机字体差异影响渲染。

穿着图必须保持当前五大轮廓：圆头、胸线轴、大斗篷、半钟刃、悬浮灯；背部
展开必须让中轴、核心、肩领和斗篷根互不覆盖。

- [ ] **Step 4: 运行 GREEN、生成证据并检查原尺寸**

```powershell
& $python -m unittest discover -s tests\art -p 'test_complete_master_structure.py' -v
powershell -ExecutionPolicy Bypass -File tools\verify-player-master.ps1 `
  -ExternalRoot 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001' `
  -Stage structure
```

Expected: `COMPLETE_MASTER_QC_OK stage=structure files=4`；四张证据都是本次运行
生成，无遮盖身体没有装备遮挡，穿着稿和无遮盖稿复用同一身体结构。

- [ ] **Step 5: 强制视觉结构关**

向用户展示四张结构图并停止。明确说明这只是结构，不是正式画风。用户拒绝时
仅修改 `structure.svg` 和结构工具；用户明确批准后，手工把
`review-state.json.structure` 写为：

```json
{
  "status": "accepted",
  "explicitUserApproval": "用户回复原文",
  "approvedAt": "2026-08-13T00:00:00+08:00",
  "evidenceSha256": ["四张结构审查图的实际 SHA-256"]
}
```

日期时间写实际审批时间，不复制示例值。未批准不得执行 Task 4。

- [ ] **Step 6: 提交 Task 3 工具与结构审批记录**

```powershell
git add tools/art/complete_master_structure.py tests/art/test_complete_master_structure.py docs/art/player/player-art-production.md
git commit -m "art: prepare complete player structure review"
```

---

### Task 4: 绘制完整软内体与陶瓷身体部件

**Files:**
- External create: `authoring/body.svg`
- External render: `layers/inner-body.png` through `layers/right-boot.png`
- External render: `review/body-parts.png`, `review/body-uncovered.png`, `review/body-uncovered-96px.png`
- Modify: `docs/art/player/player-art-production.md`

**Interfaces:**
- Consumes: 已批准结构连接点和 `complete_master_svg.py`
- Produces: 16 个身体主体 PNG；每个文件 `1402x1122 RGBA`

- [ ] **Step 1: 在 `body.svg` 只画已批准身体组**

软内体必须从颈部连续到脚踝，四肢卸甲后仍然连贯。陶瓷件逐一绘制：头壳、
胸壳、骨盆壳、左右上臂、前臂、手、大腿、小腿、鞋。安静平面占主要面积；
裂纹和黄铜细节只放在轮廓、接缝和功能扣点，不能铺满表面。

- [ ] **Step 2: 分别渲染主体并运行 body QC**

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-player-master.ps1 `
  -ExternalRoot 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001' `
  -Stage body
```

Expected: 16 个主体全都非空、哈希唯一、连接点在 alpha 内、脚底都在 `y=1080±1`，
输出 `COMPLETE_MASTER_QC_OK stage=body files=19`。

- [ ] **Step 3: 原尺寸和 96 像素视觉检查**

检查左右肢体可区分、卸甲软内体无空洞、关节重叠自然、陶瓷头和小体型比例保留、
96 像素下没有碎裂噪点。任何一项不合格只返工 body 组。

- [ ] **Step 4: 停在身体部件关并记录明确审批**

向用户展示三张 review 图。批准后才更新 `review-state.json.body` 和制作文档；
未批准不得进入胸背系统。

- [ ] **Step 5: 提交身体关制作记录**

```powershell
git add docs/art/player/player-art-production.md
git commit -m "art: record complete player body review"
```

---

### Task 5: 绘制前线轴、背部核心与隐藏中轴

**Files:**
- External create: `authoring/spool-core.svg`
- External render: `layers/front-spool-*.png`, `layers/back-*.png`, `layers/hidden-axle.png`
- External render: `review/spool-core-parts.png`, `review/spool-core-mounted.png`, `review/spool-core-back-detail.png`
- Modify: `docs/art/player/player-art-production.md`

**Interfaces:**
- Consumes: 已批准完整躯干和背部连接点
- Produces: 7 个胸背主体/发光层 PNG

- [ ] **Step 1: 绘制完整、可拆卸的胸背系统**

前线轴外壳不能填补胸壳缺洞；线轴盘独立并保留旋转余量；前发光层只覆盖能量
槽。背部核心外壳和能量仓分别完整；后发光层独立。隐藏中轴在展开图可见，
穿着图中被躯干遮住。

- [ ] **Step 2: 运行 spoolCore QC**

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-player-master.ps1 `
  -ExternalRoot 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001' `
  -Stage spoolCore
```

Expected: `COMPLETE_MASTER_QC_OK stage=spoolCore files=10`；移除设备后的躯干审查
合成仍非空完整，发光层 alpha 全部位于对应设备外壳包围盒内。

- [ ] **Step 3: 停在胸背系统视觉关**

展示单件、装配和背部展开三图。检查前后设备不粘斗篷、隐藏中轴逻辑成立、
线轴仍为胸前视觉中心。明确批准后更新 `review-state.json.spoolCore`。

- [ ] **Step 4: 提交胸背关制作记录**

```powershell
git add docs/art/player/player-art-production.md
git commit -m "art: record player spool core review"
```

---

### Task 6: 绘制连续斗篷、硬肩领和前领片

**Files:**
- External create: `authoring/mantle.svg`
- External render: `layers/shoulder-collar.png`, `layers/collar-fastener.png`, `layers/front-collar-flap.png`, `layers/mantle.png`
- External render: `review/mantle-parts.png`, `review/mantle-front-back.png`, `review/mantle-mounted.png`
- Modify: `docs/art/player/player-art-production.md`

**Interfaces:**
- Consumes: 已批准左肩、背部核心和躯干
- Produces: 4 个斗篷系统 PNG

- [ ] **Step 1: 绘制完整连续布面**

斗篷本体必须是一件连通主体，完整包含根部、外轮廓、破洞、金边和内侧布面。
肩领、扣件和前领片分别绘制；斗篷 PNG 内不得出现手、腿甲、武器、线轴或核心。
磨损集中在边缘和破洞，主体保留大块安静青绿平面。

- [ ] **Step 2: 运行 mantle QC**

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-player-master.ps1 `
  -ExternalRoot 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001' `
  -Stage mantle
```

Expected: `COMPLETE_MASTER_QC_OK stage=mantle files=7`；斗篷主体只有一个 alpha
连通组件，声明的破洞保持透明，根连接点落在肩领重叠区。

- [ ] **Step 3: 停在斗篷视觉关**

同时展示拆件、正反布面说明和装配图。检查硬件无混入、布面连续、破洞和金边
完整、穿着后保持原身份剪影。明确批准后更新 `review-state.json.mantle`。

- [ ] **Step 4: 提交斗篷关制作记录**

```powershell
git add docs/art/player/player-art-production.md
git commit -m "art: record complete mantle review"
```

---

### Task 7: 绘制完整半钟刃、双手与握持遮挡片

**Files:**
- External create: `authoring/weapon-hands.svg`
- External render: `layers/half-bell-cleaver.png`, `layers/left-grip-overlay.png`, `layers/right-grip-overlay.png`
- External render: `review/weapon-hands-parts.png`, `review/weapon-grip-open.png`, `review/weapon-grip-mounted.png`
- Modify: `docs/art/player/player-art-production.md`

**Interfaces:**
- Consumes: 已批准左右手和前臂连接点
- Produces: 1 个完整武器主体和 2 个只用于前景的握持遮挡片

- [ ] **Step 1: 绘制不缺柄的半钟刃和最小遮挡片**

刀柄、护手、半钟刃身必须全部存在于 `half-bell-cleaver.png`；遮挡片只包含握住
刀柄时位于前景的手指/掌缘像素，不能复制整只手。卸下遮挡片时完整手和完整
武器仍然可见。

- [ ] **Step 2: 运行 weaponHands QC**

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-player-master.ps1 `
  -ExternalRoot 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001' `
  -Stage weaponHands
```

Expected: `COMPLETE_MASTER_QC_OK stage=weaponHands files=6`；两个遮挡片面积各小于
对应完整手 alpha 面积的 35%，武器主体非空且不接触画布边缘。

- [ ] **Step 3: 停在武器与手视觉关**

展示拆件、未握持、握持三图。检查手没有烘焙进武器、刀柄未被裁断、遮挡关系
自然、半钟刃仍宽阔易读。批准后更新 `review-state.json.weaponHands`。

- [ ] **Step 4: 提交武器关制作记录**

```powershell
git add docs/art/player/player-art-production.md
git commit -m "art: record complete weapon grip review"
```

---

### Task 8: 绘制悬浮灯、发光层和三段能量丝线

**Files:**
- External create: `authoring/lantern-tether.svg`
- External render: `layers/lantern-*.png`, `layers/tether-*.png`
- External render: `review/lantern-tether-parts.png`, `review/lantern-tether-mounted.png`, `review/lantern-tether-dimmed.png`
- Modify: `docs/art/player/player-art-production.md`

**Interfaces:**
- Consumes: 已批准背部丝线接口和灯具参考
- Produces: 5 个灯具/丝线 PNG

- [ ] **Step 1: 绘制完整灯壳和三段连接**

灯壳不含发光，发光层不改变灯壳轮廓。背端连接件、柔性发光中段、灯端连接件
分别完整；三段在装配图中连续，但文件之间不互借像素。灯具位于头顶偏后且与
头壳至少保持 12 个源像素间隙。

- [ ] **Step 2: 运行 lanternTether QC**

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-player-master.ps1 `
  -ExternalRoot 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001' `
  -Stage lanternTether
```

Expected: `COMPLETE_MASTER_QC_OK stage=lanternTether files=8`；灯光在灯壳包围盒内，
三段端点在 `2px` 容差内连接，灯壳 alpha 与头壳 alpha 不相交。

- [ ] **Step 3: 停在灯具视觉关**

展示单件、完整连接、能量减弱三图。检查灯与身体不粘连、三段接口明确、减弱
能量不会让实体端子消失。批准后更新 `review-state.json.lanternTether`。

- [ ] **Step 4: 提交灯具关制作记录**

```powershell
git add docs/art/player/player-art-production.md
git commit -m "art: record floating lantern system review"
```

---

### Task 9: 组装最终双母版并完成三道静态终审

**Files:**
- Create: `tools/art/complete_master_review.py`
- Create: `tests/art/test_complete_master_review.py`
- External render: final `review/master-*.png`
- Modify: `docs/art/player/player-art-production.md`
- Modify: `docs/art/player/player-art-generation-log.json`

**Interfaces:**
- Produces: `compose_master(root: Path, mode: str) -> Image.Image`
- Produces: `build_contact_sheet(root: Path, part_ids: tuple[str, ...]) -> Image.Image`
- Produces: `build_small_scale_review(image: Image.Image, body_height: int = 96) -> Image.Image`

- [ ] **Step 1: 写合成 RED 测试**

测试必须证明无遮盖母版只使用身体主体；穿着母版使用相同身体哈希并增加设备；
遮挡片只能在完整主体之后合成；接触表恰好出现 35 个标签；96 像素审查图采用
整张穿着母版一次性等比缩放，不能逐件重新缩放。

```python
def test_dressed_master_reuses_exact_body_layer_hashes(self):
    evidence = compose_fixture()
    self.assertEqual(
        evidence.uncovered_meta["bodyLayerSha256"],
        evidence.dressed_meta["bodyLayerSha256"],
    )

def test_overlay_never_replaces_subject(self):
    stack = build_stack("dressed")
    self.assertLess(stack.index("right-hand"), stack.index("right-grip-overlay"))
    self.assertIn("right-hand", stack)
```

- [ ] **Step 2: 运行 RED、实现合成器、运行 GREEN**

```powershell
& $python -m unittest discover -s tests\art -p 'test_complete_master_review.py' -v
```

Expected RED: 模块不存在。实现后重复同一命令，Expected GREEN: 全部通过。

- [ ] **Step 3: 生成最终新鲜证据包**

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-player-master.ps1 `
  -ExternalRoot 'E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001' `
  -Stage final
```

Expected: 测试、35 个图层、无遮盖母版、穿着母版、背部展开、全件接触表和
96 像素预览全部通过；精确输出
`COMPLETE_MASTER_QC_OK stage=final files=40`。此时状态只能是
`ready-for-review`。

- [ ] **Step 4: 穿着复原关**

展示 `master-uncovered.png`、`master-dressed.png`、`master-back-detail.png` 和
`master-all-parts.png`。用户必须确认没有缺件、混层、归属不明、身份漂移或连接
矛盾。批准后记录 `dressedMaster`；拒绝时只退回失败部件组。

- [ ] **Step 5: 小尺寸可读性关**

单独展示 `master-dressed-96px.png` 与原尺寸穿着母版。检查五大形状、面部目缝、
胸线轴、半钟刃、斗篷和悬浮灯仍清晰，噪点没有重新占据画面。批准后记录
`smallScale`。

- [ ] **Step 6: 静态动画准备关**

展示带连接点、主体边界、重叠区和遮挡顺序的标注图。只检查关节重叠、图层顺序、
连续斗篷和握持遮挡片；不播放动画。批准后记录 `animationReady` 并立即停止。

- [ ] **Step 7: 记录免费本地尝试**

在 generation log 追加：

```json
{
  "id": "complete-layered-master-001",
  "operation": "local-digital-layer-authoring",
  "hostMode": "none",
  "reportedModel": null,
  "quotaBearing": false,
  "externalApiCostCents": 0,
  "status": "accepted-static-master",
  "parent": "body-001"
}
```

只有三道终审都明确批准时状态才可写 `accepted-static-master`；否则写
`review-ready` 或 `rejected`。同时记录外部根目录、35 个图层清单、五张最终
审查图与 `qc-report.json` 的真实 SHA-256，并注明没有发布运行时素材。

- [ ] **Step 8: 验证 JSON、全套测试和文档差异**

```powershell
Get-Content -Raw -Encoding UTF8 docs\art\player\player-art-generation-log.json | ConvertFrom-Json | Out-Null
& $python -m unittest discover -s tests\art -p 'test_complete_master_*.py' -v
git diff --check
```

Expected: JSON 有效、全部母版测试通过、`git diff --check` 无输出。

- [ ] **Step 9: 提交静态母版记录并停止**

```powershell
git add tools/art/complete_master_review.py tests/art/test_complete_master_review.py docs/art/player/player-art-production.md docs/art/player/player-art-generation-log.json
git commit -m "art: record complete layered player master"
```

不得在此提交后创建待机、奔跑、攻击、丝线动作、图集、Godot 资源或场景。
动画需要新的设计规格和实施计划。

## Verification matrix

| Spec requirement | Enforced by |
| --- | --- |
| 双母版逐件验收 | Tasks 3, 9 |
| 完整软内体与独立陶瓷甲 | Tasks 1, 4 |
| 前线轴、背核心、隐藏中轴独立 | Task 5 |
| 连续斗篷、肩领和前领片 | Task 6 |
| 完整武器、完整手、最小握持遮挡 | Task 7 |
| 独立灯具和三段能量丝线 | Task 8 |
| 唯一归属与连接重叠 | Tasks 2, 3, 9 |
| 原尺寸与 96 像素审查 | Tasks 4, 9 |
| 每关用户明确批准 | Tasks 1–9 review-state contract |
| 免费、不生成、不计费 | Global Constraints, Task 9 log |
| 不发布动画和运行时素材 | Global Constraints, Task 9 stop boundary |

## Completion boundary

计划完成时只存在一套用户批准的静态完整分层母版、对应的 35 个独立部件、
全部审查证据和免费制作记录。任何动画、图集或 Godot 集成都属于后续独立计划。
