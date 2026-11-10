# Sector assignment identity v258 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为原Supply区域配置替换点提供有界同进程身份关联，保留真实故障。
**Architecture:** REF-only observer复用已有标量registry；独立精确准入的原生小合同先验证，原pre只读现有字段。
**Tech Stack:** Godot/.NET C#、现有PS7 verifier，无新依赖。
**Spec:** docs/superpowers/specs/2026-11-05-sector-assignment-identity-v258-design.md

## Global Constraints

- 唯一G:/knight/Knight，root唯一作者/native，原地保before，不动Git/其他工程。
- 当前1897源；原pre180秒、普通post300秒/双线600秒和模型55秒保持。
- 不新增原pre资源/Load/GetState/Instantiate/GC/cache/KeepAlive/跨await借用；最多16字段/64快照。
- 仅headless/隔离存储；无GUI/OS/付费/媒体/dump/真实存档/自动化/删除发布。

## Review Focus

- 未启用scope、普通assembly不得记录或登记；普通排除门实际运行。
- before/after必须读取当前真实数组，重复观察不能覆盖首次registry；自有原生对象与独立literal字段测试。
- null/invalid/超16/超64和日志失败不可伪作成功；真实负控必须拒绝。
- 新phase必须精确REF、无混flag/waiver，raw三流故障/缺失重复收据拒绝，诊断绝不行为验收。
- 原fixture只观察已取得引用，观察异常不能泄漏自有新root；不额外读Scene/遍历/保留借用资源或改原GC/时限。

### Task1: 有界关联与真实资格

**Files:** 修改tests/diagnostics/Task14NativeResourceIdentity.cs（observer）、tests/diagnostics/RunResidentCensusIdentityContract.cs（提前窄分流/真实控制）、tests/integration/BasinSupplyRuntimeTests.cs（原替换点）；修改canonical verifier精确准入/runner/收据/冻结与普通排除。新增workspace纯PS准入控制。before全部保留。
**Interfaces:** 新ObserveAssignedSectorDefinitions(ulong rootId,SeamlessPrologueWorld world,string stage)->int，Inactive返回0；记录不可当owner证明。新Assert-SectorAssignmentAdmission(string Name,bool Identity,bool Mixed)与Assert-SectorAssignmentContractReceipt(int ExitCode,string OutText,string ErrorText,string ExternalText)只做准入/收据。

- [x] before/分支/HEAD已保存。实际AST缺失准入RED→最终51 GREEN；原生531ms反射缺失observer真实RED。另一次typed-array编译失败单独保存，不当行为RED。
- [x] observer与原替换点接线完成；最终独立原生合同615ms GREEN，真实null/root0/空stage/17字段/65快照/日志失败负控已执行；318纯控制通过，原始失败保留。
- [x] 单次pre REF146995ms/44案例及drain返回，但7个AddChild Handle错误严格FAIL；10快照/50字段/35对象完整配对，owner未定，journal上限完整性未知。普通models661ms恢复，无重复身份案。
- [x] 当前同版普通全16＋loaded排除共17全部严格PASS；20案100保护对/当前1897源完整集合/6普通binary/18helper无差异，2REF版本单列；未冲销REF7错误或历史FATAL。post4原负控警告单列，不称全rawquiet。
- [x] 唯一fresh审查C0/I0/M2，根复核评级、两Minor延期，无C/I修补或二审；16裁决/代价和最终绑定已存，README/backlog/反馈/报告回填，正常报告容量Check无新20GiB，完整目标ACTIVE，无Git/删除。

## Task1 terminal result

仅本有界诊断任务收尾，不是资源根因修复、正式美术动作或完整游戏完成。当前普通17PASS，REF一次仍严格FAIL；318纯控制、100保护对/当前1897完整源集/6普通binary/18helper/120封存文件无差异。保存原始所有失败。下一为Descent原挂载边界与journal完整性的可证伪现场读取，尚未实现；角色候选展示单次询问待答复，未扩权。原十一项ACTIVE。
