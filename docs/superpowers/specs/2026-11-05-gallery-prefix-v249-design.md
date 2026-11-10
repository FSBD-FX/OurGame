# v249 · 保留首个HP1周期原始记录

v248尾部738–865未见合格owned漏J，720–737已被128尾部覆盖。目标只补原HP1记录的有界prefix，不重查末窗口，不改24/899/HP/储备/选敌/恢复规则。原十一项范围保持。

选择：保留首32个**已完成的原pending字符串**，原128尾部不变。相较扩大/无界尾部或再加每帧observer，prefix成本有界且能保留所缺首18条；这是开发者诊断选择，不是用户新玩法。用户已要求直接持续制作，权限不重复询问。

只改MainRoute文件：Complete处只构造一次完整行；前32原行入prefix并照原规则入tail；不增加native getter、查询、predicate、输入、等待、断言、所有权或flags。prefix独立打印标记`TASK10_GALLERY_COUNTER_PREFIX`，含原完整行和ordinal1..32；tail仍只128、原total/overflow含义不变。Flush时prefix只发一次，tail原语义不变；异常/earlycontinue/原终点仍同一完成机制。prefix不足32就如实记录count，不补行/推定不存在。

验：一次原normal Main/1888/无实验flag，原strict结果保留；核对首周期实际门及未知值。未证消费者缺口不改行为。旧Minor逐row fault归属本轮不顺手修；调度开销、匿名FATAL、正式美术/动作和完整体验继续OPEN。仅headless，无外发、媒体、额外dump、GC/KeepAlive/Git/删除/真实存档。
