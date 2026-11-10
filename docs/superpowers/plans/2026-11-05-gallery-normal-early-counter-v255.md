# Gallery normal early counter v255 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让正常键盘gallery驾驶评估已有严格Skitter反击候选，并以真实输入验证。
**Architecture:** 一个纯scope选择helper，独立正常/显式分支；原live安全链和两次危险检查不变。只修改测试驾驶，生产游戏/数值不变。
**Tech Stack:** 现有C#、Godot/.NET、headless隔离verifier，无依赖。
**Spec:** docs/superpowers/specs/2026-11-05-gallery-normal-early-counter-v255-design.md

## Global Constraints

- 唯一G:/knight/Knight，原地脏树；无Git写/删除，保存before/旧失败。
- 不改生产游戏/美术/物理/输入定义/HP/能量/899/预算，不放宽原live安全门。
- 仅headless隔离存储；无GUI/OS/付费/新媒体/dump/GC/cache/KeepAlive/自动化/真实存档实验。
- root唯一CS作者/build/native；其他agent只读，运行前源码/宿主冻结。

## Review Focus

- 非键盘/非gallery不得进入新候选：literal外域矩阵，native两路线。
- 非owned/非Skitter/非Recovery正常路径不得获选：三个独立scope参数反控，native按当前frame提取。
- 旧显式probe不能丢失Laser等选择语义：literal显式表，live仍原门而非直接授权。
- 原释放后fresh检查必须保留：实际RECHECK sameowner/attack/waits0/freshtrue后才发J。
- 候选时长/能量不等于安全：原floor/slide0/30边界、未知/来弹、最终预算全部保留，真实J/命中/后续防守/完整主线验证。

### Task 1: Route selection and actual safety execution

**Files:** tests/integration/PrologueCanyonGeneratedMainRouteTests.cs；tests/unit/RangedWeaponModelTests.cs。root owns两文件，无新CS/UID，SourceRows1897。
**Interfaces:** `Task10GalleryEarlyCounterRoute(bool,bool,bool,bool,bool,bool,bool)->bool`，只选入口，旧live函数保持实际授权。

- [x] 保存before/branch/HEAD，主代理读spec/plan；写21个literal scope测试并注册Run；新增helper初始只映射旧probe-only行为，不接正常路径。
- [x] `verify-claw-knight-v001.ps1 -Phase ranged-models -SourceRows 1897`实际行为RED，编译错误不计。
- [x] helper最小GREEN并把正常ownedSkitterRecovery选择接入原GalleryEarlyCounter；仅gallery/key scope读native，原live与fresh两调用不改。
- [ ] 同命令GREEN；正常Main一次，核真实RECHECK/J/命中/防守和Boss支撑出口，不扩899。失败保留，未通过不宣称修复。
- [x] 其余当前同源14阶段各一次：Explore、prologue-pre/post、resident-save-compatibility/recycling-traps/canyon-structure-v2/resident-geometry/resident-patrol/resident-allocation/resident-state、models/keyboard/actions、thruster-incinerator-traversal。保strict ERROR/泄漏，禁止筛绿。实际12PASS/2FAIL，连GREEN/Main当前16=13PASS/3FAIL。
- [x] 核所有保护前后/current源/二进制/宿主；一次fresh增量审查，C/I一次TDD修补、Minor延期、所有裁决入账；不Git发布/清理。实际80对/1897全源/6binary/15helper0diff，一审C0/I0/M0，无新增修补。
- [x] README/总待办/反馈/报告更新、单次媒体Check，原十一项/全目标继续ACTIVE。媒体7427750B/86files/Notify=false，未建自动化。

执行结论：第4步已执行一次Main并验证真实J/命中/后续防守、gallery无伤通过；完整Main在Grip9失败，故该验收步骤仍未勾选，Task1不记complete。不得用Explore或局部成功替代MainBoss/出口。
