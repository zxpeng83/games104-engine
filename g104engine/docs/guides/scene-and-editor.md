# 场景、时间输入与设计编辑

2026-10-03，基础综合训练场 V1 获准实施后的实际接口说明。正式编译与 `--verify` 证据由执行台账汇总；本页不把编写自检视为已执行通过。

## 数据与空间

代码入口位于 `src/G104.Engine/Scene`。`SceneDocument` 是设计数据，`SceneGraph` 为其派生逻辑世界矩阵，不保存 GPU、Jolt、声音句柄或运行进度。GUID 是持久对象身份，`ParentId` 描述场景层级；模型节点与骨骼树由导入/动画模块独立维护。角色 `Transform.Position` 表示 feet，Y-up、右手、米秒、默认前方 -Z。

CPU 使用 OpenTK 原生行向量，`TransformMath.Compose` 为 S×R×T，场景 `world=local×parentWorld`。重挂接默认保持世界姿态：`newLocal=oldWorld×inverse(newParentWorld)`。分解后必须重新组合并在容差内还原原矩阵；矩阵不可逆、剪切、反射或非有限数据会拒绝。四元数在求值边界归一化。

加载与每次结构编辑检查非空且唯一 GUID、有效 ParentId/TargetId、环、128 层上限、枚举、有限 TRS/组件值。所有接收场景子对象的节点要求正统一缩放。玩家/NPC 保持根节点和单位缩放；尺寸由角色参数控制。V1 可编辑 DTO 尚无活动相机或动态刚体组件；动态刚体验证对象由物理模块独立创建，不加入任意可挂接设计对象。

`SceneGraph(SceneDocument)` 保留传入文档，公开 `Document`、`OrderedIds`、`Object(Guid)`、`WorldMatrix(Guid)`、`WorldPosition(Guid)`、`SetWorldPosition`、`SetWorldRotation`、`SetWorldMatrix`、`Rebuild`、`CapturePrevious`、`ResetInterpolation`、`InterpolatedWorldMatrix(Guid,float)` 和 `Subtree(Guid)`。运行时改局部字段之后须 `Rebuild`；结构编辑走命令接口。每模拟步开始 `CapturePrevious`，显示时相同 alpha 插值全部节点的局部 TRS，再递归组合父显示世界。重挂接、Undo/Redo、瞬移/切场景应重置历史，碰撞读取逻辑世界。

## 时钟与输入

`Core/FixedStepClock` 默认 60 Hz、每显示帧最多五补步、输入时间上限 0.25 秒；超过上限的时间与五步后完整积压步记为丢弃，不足一步余量用于显示 alpha。`Advance(double,Action<float>)` 返回 `FixedStepResult(Steps,Alpha,DroppedSteps,DroppedSeconds)`，另提供累计丢步/丢时统计。暂停、失焦、加载和 Play/Stop 后由主循环调用 `Reset`；统计保留，累计器清零。

`InputBuffer.Push(InputState)` 接收 Move、LookDelta、JumpPressed、InteractPressed、Focused、CaptureKeyboard、CaptureMouse、Sprint。按住态由每步 `Consume` 读取；边沿在零步时保留，在下一有效步只消费一次。`ConsumeLook` 每显示帧调用一次，避免多个补步重复鼠标位移。UI 捕获相应设备时清理该设备旧输入；失焦/上下文切换调用 `Clear`。

## 文件与模板

`SceneSerializer.Load(path,assetRoot)` 使用 camelCase 字段和字符串枚举，要求 schemaVersion、objects 数组与每个场景对象显式 id；不为漏写的身份自动生成新 GUID。拒绝未知版本、未知/重复 JSON 字段、非法对象/引用和缺失资产，文件上限 16 MiB。`AssetPath.Normalize` 将路径分隔符统一为 `/`，拒绝绝对路径、冒号/数据流、`.`/`..`、空段和非法字符；`Resolve` 校验规范化路径和已有链接的实际目标位于配置资源根内。初始资源与用户可写设计位置由 Sandbox 选择，不在 Engine 中硬编码用户位置。

`Save(document,path,assetRoot)` 同时检查结构与实际引用资产存在，是 Sandbox 保存入口。两参 `Save(document,path)` 保留给已确认资产的调用者与 CPU 检查，只验证结构和路径。验证后在同目录写随机临时文件、刷新落盘并原子替换，失败清理临时文件且保留已有文件。`Load` 从不修复或覆盖损坏原件；Sandbox 负责展示错误及后续加载入口。`Clone(document)` 深复制设计数据，用于运行实例与 Undo 快照。

`TemplateCatalog.Instantiate(templateId)` 深复制 Defaults，生成新 GUID，并保留 TemplateId。实例存完整有效字段，`TemplateOverrides` 明确记录白名单覆盖：name、transform、material、collider、modelPath、targetId、visible、parameters。父关系独立于模板深度；kind/primitive 不允许覆盖。模板不能引用另一模板、场景父节点或外部目标。V1 不自动传播模板默认值变化，不实现嵌套、变体、Apply/Revert。

## 命令与历史

`Editor/SceneEditor` 提供 `Create(templateId,parentId?)`、`Create(SceneObjectData)`、`Delete`、`SetTransform`、`Reparent`、`SetParameter`、`SetMaterial`、`SetCollider`、`SetAsset`、`SetTarget`、`SetName`、`SetVisible`、`SetRenderPipeline`。`SetParameter` 只修改当前设计或模板已经声明的键；运行时字段不可临时加入。`SceneParameterRules` 校验 V1 消费字段的正值/非负约束、角色胶囊高大于直径、坡度/FOV、导航边界和最多 20,000 格等关联条件；未知扩展键仍须有限，不假称已有所有未来组件模式。UI 在模拟循环外的主线程安全点调用，编辑预览的碰撞准备/替换由 Sandbox 管理。

删除组形成一次整棵子树事务；外部对象 TargetId 指向任一后代时明确拒绝，必须先解除引用。必需玩家受删除保护。撤销恢复同一 GUID、局部姿态、父引用和所有设计字段，对仍在历史范围内的对象也复用同一 DTO 实例。

`EditorHistory` 默认最多 100 条，采用设计快照，运行状态不入栈。`Execute(label,Action)` 验证和失败回滚；`BeginTransaction(label)`、`CommitTransaction()`、`CancelTransaction()` 合并拖动/多操作，取消恢复开始状态。Undo 后有效新编辑清空旧 Redo；取消或无变化事务不清空 Redo。`CanUndo`、`CanRedo`、`UndoCount`、`RedoCount`、标签和 `IsDirty` 可供 UI 展示。成功保存之后才调用 `MarkSaved`；Undo 回到已保存内容时清除 dirty，保存后 Undo 离开保存内容时重新 dirty。切换设计场景建立新编辑器或清历史。

Sandbox `Tools/SceneEditorPanel.cs` 的 `Draw(SceneEditor,bool,Guid?,Action<Action>)` 返回当前选择，通过传入队列提交全部设计命令；层级点击只改变选择。提供 cube/group/light 创建、子树删除、局部 TRS、父挂接保持世界、名字/显示、材质/纹理/模型、TargetId 与已声明参数、Undo/Redo。数值和颜色拖动使用上述事务，Escape 取消；文本在 Enter 或离开字段时一次提交。字符编辑保护、父候选约束与 Play 只读有明确提示。Play/Stop/Save/Load/Reset 和运行调试面板归主窗口，本面板不直接重建图形或碰撞。

## CPU 检查与验收边界

`CoreSelfChecks.Run()` 无新增测试框架：检查 30/60/144 Hz 时间步、五补步及丢时、输入边沿和 UI 捕获、父平移/旋转/统一缩放、保持世界重挂接、循环/剪切/角色父限制、插值重置、子树外部引用、稳定身份、拖动事务、Redo 分支、100 条上限、dirty、模板隔离与覆盖、JSON 保存重载/原子替换/损坏保留/字段和路径拒绝。文件检查只使用明确创建的随机临时目录，并在验证边界内清理，不访问真实用户 Scenes。

主任务 `--verify` 调用该入口并记录实际结果。GL/音频、静态碰撞随预览编辑同步、Play/Stop 保留未保存设计、真实 UI 手感和保存后重建/重启均需各自行为验证，不由纯 CPU 检查代替。

固定参考沿项目 Piccolo f5053707fed4d3f94d270a436fb0d3a8ae54e3e5；此模块的 GUID 设计格式、受控场景图、输入边沿和快照命令为本项目自研，概念与采用边界见 [完整实施方案](../plans/v1-implementation-draft.md) 第 3、4、7、9 节、[核心路线](../plans/core-architecture-roadmap.md) 与 [资产/场景路线](../plans/assets-scene-roadmap.md)。本页不声称逐项照搬 Piccolo。

## 资源与交付后的错误保护

EditorHistory.PrepareChange由TrainingWindow接入，在设计/历史提交前校验引用并准备GPU资源，坏路径/坏GLB/图片失败会恢复原设计；Undo/Redo先准备候选，失败不移动历史游标。Load/Reset在未保存状态提供明确保存/放弃/取消选择，成功加载停止旧voice与粒子；坏用户JSON/资源启动回落种子，首次保存前备份原件。工程数据和GPU/原生准备的两个验证层分别负责结构与实际内容。
