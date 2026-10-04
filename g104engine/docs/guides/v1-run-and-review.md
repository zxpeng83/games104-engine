# 基础综合训练场 V1：运行、验收与学习

更新：2026-10-04。V1首轮实现、独立评审及反馈修复已落地；用户明确确认下方第一轮验收第1–5项初步体验无问题，**V1首轮人工验收通过（初步、非穷尽）**。后续Bug继续反馈修复，当前进入源码学习与调试练习；既有自动证据见 [独立评审](../reviews/v1-independent-review-2026-10-03.md) 和 [UI修复记录](../reviews/v1-ui-mouse-fix-2026-10-04.md)，学习掌握仍另行自测。

## 在VS中运行

打开 [g104engine.slnx](../../g104engine.slnx)，使用现有Debug或Release/x64配置，启动G104.Sandbox。默认入口现在是完整训练场，运行不需要FFmpeg或Python；已部署素材与OpenAL由项目复制到输出目录。保持已锁定SDK/NuGet，不需要新增包。

启动为Edit。按Play或F5创建独立运行世界；Stop回到当前内存设计，未保存编辑及Undo历史保留。UI用英文标签，详细原理和操作在本项目中文文档中解释。

| 操作 | 行为 |
| --- | --- |
| WASD | 镜头相对移动 |
| 左Shift | 跑步 |
| Space | 接地时跳跃；按键边沿只消费一次 |
| E | 靠近橙色按钮时切换它明确引用的门；门洞有角色时拒绝关闭 |
| 右键拖动 / 滚轮 | 相机环绕 / 距离；工具捕获的输入不驱动角色 |
| F5 / Play / Stop | 编辑与运行切换 |
| Escape | 运行时停止；编辑活动拖动时取消，否则可退出 |
| Ctrl+Z / Ctrl+Y / Ctrl+S | 编辑态Undo / Redo / 保存；文本/UI捕获时不抢快捷键 |

如果键盘焦点在工具控件，点击画面空白处再操作。玩家初始在场地南侧，朝前走到橙色按钮附近按E，门升起后穿过门洞进入绿色目标区。侧面设有墙滑区、有限坡和四级台阶；NPC使用平面A*巡逻/感知/跟随/搜索/返回，路径可在右侧开启叠加显示。

## 编辑和数据保护

左侧树选择对象，可创建Cube/Group/Light、修改局部位置/旋转/缩放、材质和已声明参数、重挂接及删除。连续拖动合并为一条Undo，Escape恢复原值。删除组覆盖整棵子树；若外部按钮仍引用门，先解除引用才能删除。玩家/NPC为根节点、单位缩放；有子对象的父节点用正统一缩放，角色/移动门下只允许纯显示后代。

文本草稿在换选中对象、折叠或隐藏字段前提交，Escape取消文本草稿。非法拖动帧被拒绝后仍保留原事务，接着拖动仍一条Undo；Escape恢复整段起点。启动没有有效磁盘存档时显示Unsaved；Reset seed与实际存档不同也显示Unsaved，只有Save更新磁盘基准。

模型Base color是导入材质的实例tint，最终有效反射色限定[0,1]，灯光HDR颜色另行校验；模型M/R保留导入值并在面板禁用。模型贴图覆盖需要网格有UV0。模型/贴图改变先准备资源，坏路径、坏内容或超出支持合同被拒绝，原设计与历史保持。按钮Target只提供Door，`<none>`明确解除控制，不自动控制第一扇门。

Save design保存设计配置，不保存运行位置、动画游标或机关进度。默认路径为 `%LOCALAPPDATA%/G104Engine/TrainingGroundV1/Scenes/training-ground.json`，实际路径显示在右侧；重新构建输出不覆盖用户设计。

Load saved/Reset seed在未保存状态下提供Save and load、Discard and load、Cancel。Reset seed恢复随应用的初始设计，只有再次Save才写入用户保存文件。设计JSON或引用的资源损坏时，启动报告问题并用种子设计恢复可用预览；首次覆盖保存前把被拒绝原件复制为`.rejected-日期-GUID.json`，界面显示备份位置。加载/准备失败不会替换当前有效设计。

### Load saved / Reset seed的含义与手动验收

这两个操作用于编辑设计，先Stop回到EDIT；Play期间它们禁用。若只是重新玩一轮，用Stop→Play即可。

| 操作 | 加载/保存的内容 |
| --- | --- |
| Save design | 把当前设计写到用户保存文件，覆盖上一次保存；不记录游玩位置或机关进度 |
| Load saved | 重新读取你最后一次Save design的设计 |
| Reset seed | 读取随程序附带的初始训练场设计；本身不覆盖用户保存文件 |

有未保存改动时，两个加载操作都会询问：`Cancel`取消加载并保留当前编辑；`Discard and load`放弃未保存改动后加载所选目标；`Save and load`先保存当前编辑，再加载所选目标。因此对Load saved选择Save and load，会重新载入刚保存的新版本；对Reset seed选择该项，会先保护当前编辑到存档，再显示初始场景。

以下用临时Cube区分保存版和未保存版。Save会更新用户文件，先确认当前需要保留的编辑已包含其中；若还需保留之前磁盘上的另一版本，先复制右侧Save路径显示的文件。成功Load/Reset会清空当前Undo/Redo历史，测试后通过Load saved恢复保存设计。

1. Stop后创建临时Cube，名称改为`Test_A`并按Enter提交，点击Save design；预期显示Saved。
2. 将它改名`Test_B`并提交，暂不保存；预期显示Unsaved。
3. 点击Load saved→Cancel；预期仍有Test_B，仍为Unsaved。
4. 再点Load saved→Discard and load；预期恢复Test_A，显示Saved。
5. 改名`Test_C`并提交、不保存，点击Reset seed→Cancel；预期仍有Test_C。再点Reset seed→Discard and load；预期显示初始训练场，临时Cube消失，因与保存版Test_A不同而显示Unsaved。此时不要点Save design。
6. 点击Load saved→Discard and load；预期Test_A恢复，显示Saved，证明Reset没有覆盖用户存档。
7. 补测Save and load：把Test_A改为Test_D并提交、不保存，点Reset seed→Save and load；预期显示初始训练场。不要保存初始场景，再点Load saved→Discard and load；预期恢复Test_D，证明重置前的编辑已保存。
8. 测试结束，在恢复的自定义设计中删除临时Cube并Save，确认其余原有对象和参数保留。测试结果由用户反馈后再记为通过。

Forward/Deferred在编辑态选择，下一次Play生效；共用场景、材质、骨骼与光源。右侧View可观察Base color、Normals、Roughness、Depth、Shadow，另外有选中骨架/路径叠加、动画状态与事件、声音测试、FPS/帧耗时。Loop test是低音测试信号，用于循环/停止观察，不是成品背景音乐。

## 建议的第一轮用户验收

**本轮结论：通过（初步、非穷尽）。** 2026-10-04用户明确表示本节第1–5项初步验收没问题，可暂记为通过，未穷尽所有分支，后续发现Bug再反馈。不把穷尽分支作为本轮通过的前置条件，也不推定跨设备/所有极端组合或源码学习已完成；下面步骤保留为操作与问题复试依据，不要求重复整轮验收。

用户反馈的球/Ramp附近SceneValidationException已专项修复，转身、球区移动、上下坡也已有单独人工确认。六个PBR球是纯材质展示、没有Collider，经过它们是当前设计；Ramp有真实Box碰撞，见 [修复记录](../reviews/v1-contact-exit-fix-2026-10-03.md)。

**UI鼠标修复已人工通过：** 2026-10-04用户明确反馈“验证鼠标点击事件已修复”。[专项记录](../reviews/v1-ui-mouse-fix-2026-10-04.md)保留误清控件激活状态/短点击丢失的修复及双配置回归证据；随后整体第1–5项也获初步验收通过。

**基础操控与玩法主线操作参考：** 左Shift跑、Space跳、贴墙斜走与四级台阶；右键环绕/滚轮调距，观察镜头遮挡及动作衔接。靠近橙色按钮按E，穿过打开的门进入绿色目标区中心，查看右上角`Runtime / Render debug`面板：`NPC: ... | Path: ...`下一行应显示`Training complete! Stop and Play to repeat.`，上方状态变为`Door: open | Goal: True`。提示是面板普通文字，后续按E交互可能替换该文字；当前Play内`Goal: True`仍保留完成状态，Stop后该运行状态不显示，下次Play重新开始。若仍为`Goal: False`，检查门已打开且角色走入绿色目标中心。开启路径叠加观察NPC的Patrol；从正面无遮挡处靠近触发Follow，再跑远或躲到实体墙后持续保持失视，观察Search→Return→Patrol；再次被看见会恢复Follow，NPC感知与开门/目标无触发依赖。按“操作—实际现象—预期”反馈问题，之后再按下列完整清单分组继续。

1. Debug/x64运行：移动/跑跳/墙滑/坡台，观察相机遮挡；用E开门并进入目标，观察NPC路径和反馈。
2. Stop后切换Deferred再Play，对比相同设计；检查阴影、天空、PBR样例球、纹理探针和调试视图。
3. Stop后创建/移动Cube，Undo/Redo；重挂Group观察子变换，Play再Stop确认未保存编辑还在。
4. Save，关闭并重新运行，确认设计恢复；试Load/Reset取消与保留原件流程，查看界面实际保存路径。
5. 听2D/3D、脚步/跳跃/机关反馈与Loop停止；缩放、最小化/恢复、失焦回切和正常关闭，检查有没有异常。

动画定义在 [character-animation.json](../../assets/config/character-animation.json)：可改Idle/Walk/Run/Jump动作映射、参考速度、过渡/状态秒数和事件标记，重建/重启后生效；错误配置明确拒绝，不制作通用节点编辑器。Clip采样、跨Clip混合和上一/当前局部Pose的显示插值分别实现，身体与骨架共用alpha；向下离地是Fall，实际接地才Land。

不用一次读完所有代码。先按 [实际架构与学习入口](v1-architecture-and-learning.md) 看Engine/Sandbox及帧数据流，再读 [场景/编辑](scene-and-editor.md)、[物理/Gameplay](physics-and-gameplay.md)、[渲染/动画](rendering-and-animation.md)，结合 [learning-map](../learning-map.md) 返回对应笔记大章。最后尝试调参、画出查询/蒙皮/存档流程，定位一次真实问题。

## 命令行验证入口

在g104engine目录使用现有已还原依赖，命令示例：

```powershell
dotnet build g104engine.slnx --no-restore --disable-build-servers -m:1 -c Debug -p:Platform=x64
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --smoke
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify --user-data-root .cache/execution/manual-verify
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-ui --user-data-root .cache/execution/manual-ui
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-ui-input --user-data-root .cache/execution/manual-ui-input
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-render --user-data-root .cache/execution/manual-render
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-contacts --user-data-root .cache/execution/manual-contacts
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --exercise-contacts --user-data-root .cache/execution/manual-contact-window
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-audio --user-data-root .cache/execution/manual-audio
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --exercise --frames 240 --user-data-root .cache/execution/manual-graphics --capture-root .cache/execution/manual-captures
```

`--smoke`只保留托管环境信息用途；`--verify`包括原生Jolt但不创建GL窗口；`--verify-audio`真的创建音频device/context；`--exercise`用隐藏GL窗口和脚本输入验证真实蒙皮/双管线/工具生命周期，必须显式提供隔离数据根，不代替人工键鼠、试听和手感。Windows受限沙箱可能拒绝File.Replace等原子文件操作；本次在获准环境重跑通过，未修改正确存档语义来绕过。

`--verify-ui`调用真实原生cimgui输入检查草稿、拖动事务及Saved基准，不创建GL窗口；`--verify-render`创建隐藏GL4.3窗口，读回HDR/G-buffer并与独立公式比较，也检查覆盖拒绝及资产解释。都使用显式隔离根；Release把路径中的Debug替换为Release即可。`--review-baseline`为本轮七个具名导航/玩法入口，完整`--verify`也包含这些行为。

`--verify-ui-input`创建隐藏Windows/GL窗口，定向Win32消息经GLFW/OpenTK、生产ImGui后端和共享清焦策略驱动真实控件，13项覆盖点击/下拉/勾选/拖动/文字/焦点/滚轮及F5输入可用性，不读取保存设计，也不移动桌面鼠标；人工体验独立记录，用户已确认鼠标点击修复，其他体验不从这些自动结果推定通过。

`--verify-contacts`独立检查38种变换/真实种子运动路线，失败汇总后退出1；`--exercise-contacts`走真实隐藏窗口的6球及Ramp上/下/侧九路，默认800帧，显式frames不能少于800，并强制隔离数据根。后者不输出旧240帧截图序列；实际绘制由每段计数和GL错误检查验证，不把capture-root当作截图已产生。两个配置本批专项和旧240帧均通过；不会自动覆盖你的保存设计。

素材来源及SHA见 [准备清单](../plans/v1-dependencies-and-assets.md) 和 `assets/licenses/asset-manifest.json`；`tools/prepare_assets.py`只负责已核对输入的提取/程序测试资产和离线转换，FFmpeg缓存不进入应用。D5、跨设备/CI、IBL与其他后移专题仍未恢复。
