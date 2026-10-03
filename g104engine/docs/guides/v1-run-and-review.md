# 基础综合训练场 V1：运行、验收与学习

更新：2026-10-03。V1首轮完整实现已落地；本机Debug/Release构建、CPU/原生行为、实际音频上下文和隐藏图形回归通过。用户视觉、试听、操控和学习验收仍待进行，细项见 [验收记录](../reviews/v1-implementation-review-2026-10-03.md)。

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

模型Base color是导入材质的实例tint，模型M/R保留导入值并在面板禁用；普通几何可编辑M/R。模型/贴图改变先准备资源，坏路径或坏内容被拒绝，原设计与历史保持。按钮Target只提供Door，`<none>`明确解除控制，不自动控制第一扇门。

Save design保存设计配置，不保存运行位置、动画游标或机关进度。默认路径为 `%LOCALAPPDATA%/G104Engine/TrainingGroundV1/Scenes/training-ground.json`，实际路径显示在右侧；重新构建输出不覆盖用户设计。

Load saved/Reset seed在未保存状态下提供Save and load、Discard and load、Cancel。Reset seed恢复随应用的初始设计，只有再次Save才写入用户保存文件。设计JSON或引用的资源损坏时，启动报告问题并用种子设计恢复可用预览；首次覆盖保存前把被拒绝原件复制为`.rejected-日期-GUID.json`，界面显示备份位置。加载/准备失败不会替换当前有效设计。

Forward/Deferred在编辑态选择，下一次Play生效；共用场景、材质、骨骼与光源。右侧View可观察Base color、Normals、Roughness、Depth、Shadow，另外有选中骨架/路径叠加、动画状态与事件、声音测试、FPS/帧耗时。Loop test是低音测试信号，用于循环/停止观察，不是成品背景音乐。

## 建议的第一轮用户验收

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
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-audio --user-data-root .cache/execution/manual-audio
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --exercise --frames 240 --user-data-root .cache/execution/manual-graphics --capture-root .cache/execution/manual-captures
```

`--smoke`只保留托管环境信息用途；`--verify`包括原生Jolt但不创建GL窗口；`--verify-audio`真的创建音频device/context；`--exercise`用隐藏GL窗口和脚本输入验证真实蒙皮/双管线/工具生命周期，必须显式提供隔离数据根，不代替人工键鼠、试听和手感。Windows受限沙箱可能拒绝File.Replace等原子文件操作；本次在获准环境重跑通过，未修改正确存档语义来绕过。

素材来源及SHA见 [准备清单](../plans/v1-dependencies-and-assets.md) 和 `assets/licenses/asset-manifest.json`；`tools/prepare_assets.py`只负责已核对输入的提取/程序测试资产和离线转换，FFmpeg缓存不进入应用。D5、跨设备/CI、IBL与其他后移专题仍未恢复。
