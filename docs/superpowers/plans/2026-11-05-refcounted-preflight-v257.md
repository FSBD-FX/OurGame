# RefCounted preflight v257 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 恢复资源身份测试的严格纯依赖准入并获得一次实际身份证据。
**Architecture:** 旧契约仅替换导入前缀，完整验证两个明确纯函数再导入；真实前缀/受控AST反例不启动owner。宿主和C#源不改，诊断后恢复原普通二进制。
**Tech Stack:** PowerShell7 AST、现有Godot/.NET verifier，无新依赖。
**Spec:** docs/superpowers/specs/2026-11-05-refcounted-preflight-v257-design.md

## Global Constraints

- 唯一G:/knight/Knight，原地脏树/保原件，root唯一代码和native，运行前冻结。
- 不改canonical宿主/CS/原269行控制，不删安全门；SourceRows1897/无UID。
- 仅headless隔离存储；无GUI/OS/付费/新媒体/dump/额外GC/cache/KeepAlive/引擎/真实存档实验/自动化/Git/删除。
- REF是时序受扰动诊断，不代普通资格或修复；普通恢复Rebuild与源/6binary绑定必须核验。

## Review Focus

- 合法已有纯依赖被误拒绝：实际导入前缀与canonical pre/post/pair的literalargv分别测试。
- 名称白名单掩盖有副作用依赖：依赖命令/成员/重定向/scoped变量/属性/默认参数/嵌套定义在导入前拒绝。
- 重名或间接调用改变解析对象：缺失/大小写重名/未知命令/调用操作符/签名改变的受控AST拒绝。
- 原argv与身份回执控制被意外弱化：原完整契约及v256的84控制原样运行，不把前缀通过当整套通过。
- 诊断被当成正常修复或遗留符号：一次身份案保留失败/符号falseBehavior，恢复普通models和完整源/6binary哈希；不复用新FATAL为旧原因。

### Task1: 严格导入、完整控制及一次诊断

**Files:** 修改`.superpowers/sdd/2026-10-16-resident-population/test-pre-finalizer-identity-admission-20261021.ps1`导入前缀；新增本计划workspace `test-preflight-pure-dependency-v257.ps1`；canonical宿主和CS只读。
**Interfaces:** 原Get-ClawBuildArguments(string,bool,bool,bool)->string[]；原Assert-ProfilePairAdmission(string,bool,bool)->void。两定义验证后才导入，不新造游戏API。

- [x] 保存before/branch/HEAD；实际旧前缀6项合法正例RED，非语法错误；污染体仅ImportOnly不执行。
- [x] 最小完整双定义/原签名/唯一名字验证→GREEN24；审查I1再2项File负例RED→最小拒绝→GREEN26。
- [x] 完整原契约157与v256相关84均通过，最终新鲜纯控制267/267；保原断言，无native。
- [x] 原canonical pre REF一次严格FAIL77783ms/无超时/guard0；call810区域配置serial1076未返回，不宣称root cause或修复。
- [x] 普通models Rebuild523ms通过，无符号；当前1897源/6binary同v256，运行内10保护对不变。I1后仅旧REF契约1项已知helper差异须最终绑定，不重写case或重跑身份案。
- [x] 一次fresh审查C0/I1/M0，I1一次实际TDD修补；267纯控制、11裁决/代价、历史Minor延期、文档及最终绑定已存。单次媒体Check 7,427,750B/86files/Notify=false；完整目标ACTIVE，无Git/清理、无第二审查或原生重试。

## Task1 terminal result

有界契约任务完成，非资源故障修复、非完整游戏完成。身份案仍严格FAIL；普通恢复PASS。最终绑定final-binding-v257.json：10保护对无差异、1897源完整集合/6普通binary同v256；REF案13helper中仅审后旧契约1项已知差异、恢复案9项全同、v256普通15helper全同；原契约尾段26853原始字节相等。所有执行进程终态，无后台任务遗留；下一仅现有before/after替换点资源标量关联。
