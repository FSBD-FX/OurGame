# v318 · 远程敌人脚部分件底锚

## 事实、判断、风险

上一轮v317完成同产物完整Main／Explore和原前后段，属于progress；本轮继续原十一项的V05分层接入基础，不用单个矩形修复取代家族造型或全动作返工。唯一Godot工程仍为`G:/knight/Knight`，只无窗口。

当前`RangedWeaponVisual.GetPartGeometry`是真实DrawPart／DrawTextureRect的矩形入口。原foot画布`(-8,-4,16,4)`以同躯干的居中规则适配80×10纹理为`(-8,-3,16,2)`，底部离现rig脚踝基线1单位。原因是foot的锚点语义与其他part不同，并非骨长、物理引擎或碰撞缺陷。

本次只改变替换纹理的矩形底锚，没有新素材接入。矩形底部正确不保证透明留边的鞋底像素贴地，也不保证任意旋转祖先／全动作／真人观感；未把家族造型问题的推荐预选当成用户回答或付费授权。

## 实际RED→GREEN

测试先用真实test-owned Images／ImageTextures、四角色、near／far、左右瞄准、水平Scale±1、宽／高／null脚图及非foot躯干字面量，检查只读几何不推进rig、Free visual不释放借用纹理。所有资源在visual释放后由测试owner退出using释放，不增加生产测试清理API。原完整rig／生命周期／表现phase未删减。

- RED：`ranged-presentation-20261108-114020-052-d902175148a94c1a929e5df4460d76a2`，build通过、engine1／629ms、guards=[]；第一实际宽脚断言精确为期望`(-8,-2,16,2)`／实际`(-8,-3,16,2)`，不是编译或夹具错误。原始失败保留。
- 生产只改`GetPartGeometry`等比缩放后的origin：foot令Y=`canvas.End.Y-size.Y`，其余part原居中，X居中和比例不变。不改rig、AI、HP／能量／碰撞／地图／场景／素材或canonical。
- GREEN：`ranged-presentation-20261108-114139-260-ff2e8c1172914f878db79494eb0b991e`，engine0／46620ms、failure=null／guards=[]，原完整phase严格通过；`RANGED_FOOT_FIT_OK roles=4 near_far=true aims=2 mirror=true wide_tall_null=true torso_centered=true query_pure=true borrowed=true`在stdout／external各精确一次，三sink诊断0。

宽脚80×10为`(-8,-2,16,2)`，高脚10×80为`(-.25,-4,.5,4)`，null仍原canvas且UsesTexture=false；torso80×20为`(-24,-9.5,48,12)`、20×80为`(-5.375,-25,10.75,43)`，未被脚底锚污染。四角色literal脚踝X±20／15／22／12、Y34保持；检查parent space水平镜像，非任意世界旋转保证。

## 当前冻结身份与验证状态

- 当前1907输入source manifest：`D17960C982C8F3430F02C8713D3B858D548424B9AEC2E4DF7CBECF0539659196`。
- 六binary manifest：`B4BB0C2396B5C39F59BDF41546518982079D16F278E9A1CF0938CF960BB644A0`。
- 生产文件SHA：`25F260E4F7469CC57EA2639A7407D23CD199241C91B63EAFDF1D78DCC131A3D3`；测试文件SHA：`EC1CFB196372FCA28C864C4E93A0917D54416435BF761D2AD212FF7069329158`。
- 专项before／after source／binary／helper均同一；原normal whole merge的v317版本7BD49／FAC8与本版不同，不据此给新程序集续签完整双路线。

根比对v317与本轮完整1907清单：恰好上述生产／测试两文件变化，新增／删除输入均0。本轮已终结的追加阶段：

| phase | case | engine ms | 结果 |
|---|---|---:|---|
| ranged-models | `ranged-models-20261108-114409-210-4b75aefe8f2b44b099e6948e5b4cc105` | 517 | strict PASS |
| ranged-actor-saved | `ranged-actor-saved-20261108-114445-290-bcf7d533d5684938b7b1f6dfcc35a2c9` | 58491 | strict PASS |
| prologue-pre | `prologue-pre-20261108-114617-188-804246130ebc4b0f9c2146f1b3a3cb31` | 147723 | strict PASS |
| prologue-post | `prologue-post-20261108-114920-922-61bbc06643214d62910c4543de71d3c7` | 189379 | strict PASS |
| models | `models-20261108-115306-631-c33356d756e142deb14bf53a3d5ea759` | 502 | strict PASS |

限定独立增量review C0／I0／M0：原件及当前SHA准确，生产只改适配原点，测试只新增调用／方法／far参数，其他内容完全未变。实际Draw直接消费同一几何，任意纹理比例／槽位切换／其他姿态不见该条件的新风险，借用纹理释放顺序正确；无新增审查修补或延期Minor，不另开二审。

审查未代签运行中的batch或native，未认证透明留边鞋底像素／最终家族美术／正常速度／完整关卡／声画，也未审旧全项目问题；这些由root保留为相应OPEN边界，不因为审查C0就关闭。

普通pre保留原44项和退出清理，runner0／engine0、failure=null／guards=[]，未改180s预算；post保留原75／retired0及退出清理，189379ms在原300000ms普通门内，不签旧180s性能。external／stderr各4条同一组原负例warning（observer1／VisualScene错误根类型1／Completed→Recovering拒绝2，不是8个独立故障），stdout无warning；三sink无ERROR／FATAL／Handle。models亦严格通过，五阶段batch外层真实exit0，未重试筛绿。本轮未复现历史句柄错误，不算根因修复。

连同首个GREEN专项，本轮6指定phase全部属于D17960／B4BB0C同一source／six-bin，各案build／engine真实0／无超时、failure=null／guards=[]；helper与continuation案内前后不变，公共9helper与v317完全相同，各phase额外原helper不被误当漂移。ranged三个阶段helper10行（额外原ranged准入），pre10、post10、models9；root最终1907输入／6binary／12唯一helper逐项live读回零差异，无owned Godot／dotnet／MSBuild遗留。真实用户保护109项清单前后均`BDDA0FBEA506AFB5A69E11CE0EED85C1E384C9020F32E1802AB7563B5B1BB2DB`，没有真实存档实验。

## 完成范围与继续项

本次脚图底锚bugfix的程序实现／原生RED→GREEN／六阶段回归／限定审查已完成。新程序集完整Main／Explore及保存兼容未在本轮重新运行，最近整版双路线资格仍是明确版本绑定的v317；最终交付前须对最后冻结版合并复验，不把旧版测试拼成新全关验收。此实现不改善默认仍未装正式脚图的家族素材，也不代替原V01/V03/V04主角动作和全部V05造型。

下一步仍应推进实际可见的家族造型与主角专属动作，按既有批准范围／待答路线区分候选与接入；当前没有新付费或正常窗口权限，不扩大为自动生成／接入未批准素材。若继续另一个代码增量，应先明确它服务的具体表现问题与实际运行消费者，不把数量、接口或测试数当精美度。

原资源根因、正式美术连续动作、正常声画／硬件／真人与整个十一项ACTIVE，不宣称成品交付。既有fingerprint覆盖／离线夹具路径两个Minor不被本次C0/I0/M0冲销；本次无新延期Minor。

无GUI／OS输入／付费生成／额外native采集／真实存档实验／引擎、缓存、GC或预算变更／自动化／Git／删除。未新建过程图片或视频。

正常汇报仅执行一次既有媒体`-Check`：15,203,531字节／145files、Notify=false，未达新增20GiB；无单独通知、自动化或停工。唯一实施ruling是沿用用户自主推进、同一脏树与无窗口限制修已记录接口缺陷，成本／边界为“矩形底锚不等于透明鞋底像素或正式美感验收”，本轮已保持这一边界。
