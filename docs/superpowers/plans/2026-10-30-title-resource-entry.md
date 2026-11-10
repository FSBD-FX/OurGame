# Standalone Title Resource Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Root executes; agents read-only.

**Goal:** 判别原keyboard前缀是否是标题两挂载资源错误的必要条件，不改变验收标准。
**Architecture:** canonical新增title-resource-entry，仅运行已有独立脚本的--contract-only；不创建新C#诊断类型或生产保活策略。
**Tech Stack:** Godot4.7.1.NET、现有pwsh canonical owner。
**Spec:** docs/superpowers/specs/2026-10-30-title-resource-entry-design.md

## Global Constraints

- G:/knight/Knight、完整十一项ACTIVE；root唯一写入/构建/native owner，运行期间源/helper冻结。
- 原55秒native、180秒build、全部raw错误及source/bin/user/helper守卫保留；不改默认缓存或GC。
- 诊断案不准冒充普通keyboard/完整验收；仅用户随后批准的显式隔离预览可单独显示，无OS输入/采集/付费/自动化/Git/删除。

## Review Focus

1. 误走RunStableClaw的unknown-phase或带残留--phase：覆盖实际argv为直接RunIsolatedTitleHold、storage＋--contract-only。
2. 相同合同被缩成一次mount：严格要求原mounts2/hold180/starts0/closes2与ENTRY/END三个唯一收据。
3. raw有错但native0就通过：三sink ERROR/WARNING/FATAL/assertion均否决。
4. 诊断结果流入live：新phase/case名不满足Get-TitleQualification，provenance/终结标记明确not-acceptance。
5. 首次无错被当根治：只记单次合同观察，不关闭原失败、不重复筛绿。

### Task 1: 原合同独立启动及严格保管

Files: 仅.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1；其他为MD记录。
Interfaces: Phase=title-resource-entry；原Run-Owned/Freezing/budget；res://tests/RunIsolatedTitleHold.cs -- --case-storage-root=<owned> --contract-only。

- [x] 现有入口请求新phase，观察不支持且无native的RED。
- [x] 增加phase/独立argv/严格三sink收据/diagnostic provenance，原普通分支不变。
- [x] 一次fresh构建＋独立native，对原5错身份未知结论作有限更新，不将偶发无错称稳定。
- [ ] 唯一fresh审查、保管、README/backlog及一次媒体容量检查；保留全部失败，不提交或清理。

指定dirty root继续；本任务不改生产故不以重复跑相同完整序章替代需要的单变量诊断。后续生产变更仍需真实RED→GREEN与完整普通回归。

### Task 2: 用户批准的只标题预览

Files: tools/show-isolated-title-v001.ps1；.superpowers/sdd/2026-10-30-persistent-title/test-launch-plan.ps1；canonical仅追加预览工具/helper实体冻结。
Interfaces: 显式-IsolatedPreview沿prepare→Get-TitleQualification→Start-TitleWindow传递；默认普通门不变。

- [x] Preview validator缺功能RED→新诊断字段/原始状态拒绝、默认普通拒绝诊断案GREEN。
- [x] 预览显式传播/默认不启动、两次资格失败/无当次显示许可均拒绝，原普通PS套件仍PASS。
- [x] 新helper冻结后fresh当前独立入口案、真实当前source/sixbin/helper资格与默认普通拒绝验证通过。
- [x] 唯一fresh审查C0/I0/M0后，仅当前许可开一次现有live脚本；不重编译未变代码、不自动关闭/抢前台/发送按键。用户已明确看到标题页，只认证显示。
- [x] 保存launch/READY、用户标题可见确认及关闭请求后AudioDrain/END0/进程终止；未捕获OS exitcode不补造。未宣称完整版、资源修复或美术认可；原失败继续保留。
