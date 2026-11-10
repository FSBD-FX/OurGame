# Resident geometry stable issue snapshots — v253 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 稳定几何诊断原因顺序，不降低任何真实阻挡或巡逻回归要求。

**Architecture:** 在现有两个不可变结果构造器中以 Ordinal 规范原因排列。保留 body 重复次数、path 去重、逐 corridor 顺序、支撑、查询和拒绝语义。直接值测试先 RED，旧 native 阶段原样回归。

**Tech Stack:** 现有 Godot/.NET C#、隔离 headless verifier；不新增依赖。

**Spec:** docs/superpowers/specs/2026-11-05-resident-issue-order-v253-design.md

## Global Constraints

- 唯一工程 G:/knight/Knight；原十一项及所有未解决失败仍 OPEN。
- 仅 headless 和隔离存储；真实存档/GUI/OS 输入/付费/新采集/自动化/Git 写/删除均不做。
- 保留原 208/20、两个跨态 SequenceEqual、真实 wall add/remove 和原预算/错误严判。
- 原地脏工作树，保存 before，禁止回退他人修改；根代理唯一 build/native 执行者。

## Review Focus

- 文化改变不得改变原因排列：tr-TR 和 invariant 的字面量 ordinal 案例。
- 稳定性不得通过丢原因获得：body 重复次数、path 原去重和不同原因均保留。
- 原列表被清空/改写不得改结果：构造后变更输入并尝试写只读返回集合。
- 不得把真实墙变化当排列噪声：运行原三态和 original 208/20。
- 不得排序路径段/支撑或改几何：直接快照字段/支撑序列和原 native fresh/batch 检查。

### Task 1: Stable diagnostic value boundary

**Files:** Create tests/unit/ResidentGeometryIssueOrderTests.cs; Modify tests/integration/CanyonResidentPlacementTests.cs (Run 入口调用); Modify scripts/prologue/residents/ResidentPlacementGeometry.cs (两个构造器)。

**Interfaces:** Consumes 现有 public result 构造器和 `TestAssert`；Produces `ResidentGeometryIssueOrderTests.Run()`，不新增生产接口。

- [x] 保存两现有源码 before 和分支/HEAD 事实；新增文件原先不存在。
- [x] 用真实构造器写六种排列的字面量期望及五类 Review Focus 测试，在 `Run` 开头调用。
- [x] `verify-claw-knight-v001.ps1 -Phase resident-geometry -SourceRows 1897`，旧实现须实际因原因排列失败，不能以编译/宿主错误充 RED。
- [x] 仅在两个构造器快照 ToArray 前加入 Ordinal 排序，原 Distinct 策略不改。
- [x] 同一命令验证新值测试及完整原几何回归；若原生故障/泄漏仍报 FAIL，记录而不重跑筛绿。
- [x] 若实际 native 完成仅被过期143固定清单拒绝，先更新现有纯宿主测试为实测157/63/110和旧143拒绝、缺项/混合/顺序测试须 RED；再更新唯一 fixture 及新值/208行要求，纯合同 GREEN 后同源严格重验，不回写先前 FAIL。
- [x] 同源回归 resident-save-compatibility、recycling-traps、canyon-structure-v2、resident-allocation、resident-state、resident-patrol、models、keyboard、actions、prologue-pre、prologue-post、thruster-incinerator-traversal、两条普通 route diagnostic，各一次；不扩预算。
- [x] 当前源/二进制/宿主冻结、真实保存保护清单核对。一次 fresh whole-increment review，C/I 仅一轮 TDD 修补；Minor 延期，裁决逐项入账。
- [x] 更新 README/原待办/反馈/生产报告，执行单次媒体容量检查；不提交/发布/清理工作目录，以局部证据报告，不认定完整目标完成。

执行步骤已做，但Task1广域验收未完成：最终15阶段13PASS/2FAIL（pre7Handle/Main899）。不能把上面“已执行”勾选解释为全绿/序章完成；本轮局部修复可保留，原十一项及全局目标保持ACTIVE。
