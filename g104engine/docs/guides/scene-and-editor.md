# 场景、时间输入与设计编辑

本页定义场景、输入与有限编辑的实际接口、约束及失败语义。必要概念在本页说明；当前任务/验收查 [status.md](../status.md)，操作政策查 [agent-workflow.md](../agent-workflow.md)，设计来源可选查 [v1-baseline.md](../plans/v1-baseline.md)。

<a id="scene-contracts"></a>
## 数据与空间

代码入口位于 `src/G104.Engine/Scene`。`SceneDocument` 是设计数据，`SceneGraph` 为其派生逻辑世界矩阵，不保存 GPU、Jolt、声音句柄或运行进度。GUID 是持久对象身份，`ParentId` 描述场景层级；模型节点与骨骼树由导入/动画模块独立维护。角色 `Transform.Position` 表示 feet，Y-up、右手、米秒、默认前方 -Z。

CPU 使用 OpenTK 原生行向量，`TransformMath.Compose` 为 S×R×T，场景 `world=local×parentWorld`。重挂接默认保持世界姿态：`newLocal=oldWorld×inverse(newParentWorld)`。分解后必须重新组合并在容差内还原原矩阵；矩阵不可逆、剪切、反射或非有限数据会拒绝。四元数在求值边界归一化。

`SetWorldPosition/SetWorldRotation`是单字段操作：克隆已存TRS，仅改位置或旋转，不能从自身world矩阵反复分解并重估Scale。根位置直接保存；父级位置乘父world逆矩阵；行矩阵`local×parent`对应四元数`parent×local`，所以局部旋转为`inverse(parentRotation)×desiredWorldRotation`。Position/Rotation更新保留其余字段，Rebuild失败恢复旧TRS。Player/Npc已强制为根，Gameplay转向直接读存储的Quaternion。

通用`SetWorldMatrix`和保持世界重挂接仍需分解。接近180°时四元数w接近0，若矩阵反求选择除以小w的路径，会放大浮点误差，合法TRS也可能在重建时被判为剪切。本实现去行缩放后用double中间值及trace/最大对角元素四分支选择稳定分母；保留原`Tolerance=1e-4`、正缩放、有限值及重建校验，并拒绝行长度提取溢出。它不通过调大容差或吞异常隐藏非法矩阵。用户球/Ramp退出的无物理复现、修复与双配置后验见 [v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md)。

加载与每次结构编辑检查非空且唯一 GUID、有效 ParentId/TargetId、环、128 层上限、枚举、有限 TRS/组件值。所有接收场景子对象的节点要求正统一缩放。玩家/NPC 保持根节点和单位缩放；尺寸由角色参数控制。V1 可编辑 DTO 尚无活动相机或动态刚体组件；动态刚体验证对象由物理模块独立创建，不加入任意可挂接设计对象。

`SceneGraph(SceneDocument)` 保留传入文档，公开 `Document`、`OrderedIds`、`Object(Guid)`、`WorldMatrix(Guid)`、`WorldPosition(Guid)`、`SetWorldPosition`、`SetWorldRotation`、`SetWorldMatrix`、`Rebuild`、`CapturePrevious`、`ResetInterpolation`、`InterpolatedWorldMatrix(Guid,float)` 和 `Subtree(Guid)`。运行时改局部字段之后须 `Rebuild`；结构编辑走命令接口。每模拟步开始 `CapturePrevious`，显示时相同 alpha 插值全部节点的局部 TRS，再递归组合父显示世界。重挂接、Undo/Redo、瞬移/切场景应重置历史，碰撞读取逻辑世界。

<a id="time-and-input"></a>
## 时钟与输入

`Core/FixedStepClock` 默认 60 Hz、每显示帧最多五补步、输入时间上限 0.25 秒；超过上限的时间与五步后完整积压步记为丢弃，不足一步余量用于显示 alpha。`Advance(double,Action<float>)` 返回 `FixedStepResult(Steps,Alpha,DroppedSteps,DroppedSeconds)`，另提供累计丢步/丢时统计。暂停、失焦、加载和 Play/Stop 后由主循环调用 `Reset`；统计保留，累计器清零。

`InputBuffer.Push(InputState)` 接收 Move、LookDelta、JumpPressed、InteractPressed、Focused、CaptureKeyboard、CaptureMouse、Sprint。按住态由每步 `Consume` 读取；边沿在零步时保留，在下一有效步只消费一次。`ConsumeLook` 每显示帧调用一次，避免多个补步重复鼠标位移。UI 捕获相应设备时清理该设备旧输入；失焦/上下文切换调用 `Clear`。

UI使用独立的 `ImGuiController(GameWindow window)`；生产窗口以 `new ImGuiController(this)` 绑定。MouseMove/Down/Up/Wheel、FocusedChanged和TextInput按事件顺序送入ImGui，BeginFrame同步键盘按住态并开帧，Dispose解除订阅。失焦先使鼠标位置无效再释放按钮，不能让焦点同批恢复完成旧点击。场景左点击清焦依据 `WantCaptureMouse`，不会误清按下后已激活的控件；角色InputBuffer依然在窗口层按设备捕获标志过滤，两条输入职责不能混写。

## 文件与模板

`SceneSerializer.Load(path,assetRoot)` 使用 camelCase 字段和字符串枚举，要求 schemaVersion、objects 数组与每个场景对象显式 id；不为漏写的身份自动生成新 GUID。拒绝未知版本、未知/重复 JSON 字段、非法对象/引用和缺失资产，文件上限 16 MiB。`AssetPath.Normalize` 将路径分隔符统一为 `/`，拒绝绝对路径、冒号/数据流、`.`/`..`、空段和非法字符；`Resolve` 校验规范化路径和已有链接的实际目标位于配置资源根内。初始资源与用户可写设计位置由 Sandbox 选择，不在 Engine 中硬编码用户位置。

`Save(document,path,assetRoot)` 同时检查结构与实际引用资产存在，是 Sandbox 保存入口。两参 `Save(document,path)` 保留给已确认资产的调用者与 CPU 检查，只验证结构和路径。验证后在同目录写随机临时文件、刷新落盘并原子替换，失败清理临时文件且保留已有文件。`Load` 从不修复或覆盖损坏原件；Sandbox 负责展示错误及后续加载入口。`Clone(document)` 深复制设计数据，用于运行实例与 Undo 快照。

`TemplateCatalog.Instantiate(templateId)` 深复制 Defaults，生成新 GUID，并保留 TemplateId。实例存完整有效字段，`TemplateOverrides` 明确记录白名单覆盖：name、transform、material、collider、modelPath、targetId、visible、parameters。父关系独立于模板深度；kind/primitive 不允许覆盖。模板不能引用另一模板、场景父节点或外部目标。V1 不自动传播模板默认值变化，不实现嵌套、变体、Apply/Revert。

<a id="editor-transactions"></a>
## 命令与历史

`Editor/SceneEditor` 提供 `Create(templateId,parentId?)`、`Create(SceneObjectData)`、`Delete`、`SetTransform`、`Reparent`、`SetParameter`、`SetMaterial`、`SetCollider`、`SetAsset`、`SetTarget`、`SetName`、`SetVisible`、`SetRenderPipeline`。`SetParameter` 只修改当前设计或模板已经声明的键；运行时字段不可临时加入。`SceneParameterRules` 校验 V1 消费字段的正值/非负约束、角色胶囊高大于直径、坡度/FOV、导航边界和最多 20,000 格等关联条件；未知扩展键仍须有限，不假称已有所有未来组件模式。UI 在模拟循环外的主线程安全点调用；编辑时校验设计并准备渲染资源，下一次 Play 从当前设计重建真实物理世界。编辑预览没有独立碰撞世界。

删除组形成一次整棵子树事务；外部对象 TargetId 指向任一后代时明确拒绝，必须先解除引用。必需玩家受删除保护。撤销恢复同一 GUID、局部姿态、父引用和所有设计字段，对仍在历史范围内的对象也复用同一 DTO 实例。

`EditorHistory` 默认最多 100 条，采用设计快照，运行状态不入栈。`Execute(label,Action)` 验证和失败回滚；拖动中无效帧只恢复到前一有效值，原事务仍可继续，最终一次 Undo 或取消恢复开始状态。`BeginTransaction(label)`、`CommitTransaction()`、`CancelTransaction()` 合并拖动/多操作。Undo 后有效新编辑清空旧 Redo；取消或无变化事务不清空 Redo。`CanUndo`、`CanRedo`、`UndoCount`、`RedoCount`、标签和 `IsDirty` 可供 UI 展示。成功保存之后才调用 `MarkSaved`；Undo 回到已保存内容时清除 dirty，保存后 Undo 离开保存内容时重新 dirty。`SetSavedBaseline(SceneDocument?)` 将实际保存目标的内容独立于当前设计与历史；Reset seed 建立新历史仍以用户保存文件为基准，内容不同则显示 Unsaved。Load saved 成功后以该文件内容为基准；保存文件缺失或被拒绝时传 null，不把回落种子误标为已保存。Play/Stop 保留该基准和原历史。

Sandbox `Tools/SceneEditorPanel.cs` 的 `Draw(SceneEditor,bool,Guid?,Action<Action>)` 返回当前选择，通过传入队列提交全部设计命令；层级点击改变选择并提交旧对象尚未提交的文本。提供 cube/group/light 创建、子树删除、局部 TRS、父挂接保持世界、名字/显示、材质/纹理/模型、TargetId 与已声明参数、Undo/Redo。数值和颜色拖动使用上述事务，Escape 取消并忽略当前按住手势的剩余移动；下一次按下可重新编辑。文本在 Enter、离开字段或字段因切换选择/收起而停止绘制时一次提交，草稿持有原对象回调；Escape 丢弃草稿，后续失焦不会再次提交被取消内容。字符编辑保护、父候选约束与 Play 只读有明确提示。Play/Stop/Save/Load/Reset 和运行调试面板归主窗口，本面板不直接重建图形或碰撞。

## CPU 检查与验收边界

`CoreSelfChecks.Run()` 无新增测试框架：检查 30/60/144 Hz 时间步、五补步及丢时、输入边沿和 UI 捕获、父平移/旋转/统一缩放、保持世界重挂接、循环/剪切/角色父限制、插值重置、子树外部引用、稳定身份、拖动事务、Redo 分支、100 条上限、dirty、模板隔离与覆盖、JSON 保存重载/原子替换/损坏保留/字段和路径拒绝。文件检查只使用明确创建的随机临时目录，并在验证边界内清理，不访问真实用户 Scenes。

主任务 `--verify` 调用该入口并记录实际结果。GL/音频、编辑后下一次 Play 的物理重建、Play/Stop 保留未保存设计和真实UI/重启行为采用各自证据；这些不由纯CPU检查代替。CPU、生产窗口输入、运行准备及人工体验分别形成证据；覆盖范围不能相互代替。

Sandbox `--verify-ui` 使用现有 cimgui 的独立上下文，通过ImGui输入API注入鼠标、键盘和字符，驱动 `SceneEditorPanel.Draw` 后处理相同命令边界；它不经过生产OpenTK窗口事件适配器、不创建GL窗口、不写imgui.ini、不访问真实用户保存目录。面板可选控件区域观察只用于定位点击，正常窗口不订阅。11项覆盖四种文本在树切换时提交、非法胶囊拖动继续/一次Undo/Escape、文本Enter/Escape、收起材质/窗口与工具栏按下释放顺序，以及显式工作区验证目录中的自定义保存→Reset seed→保存重载基准。2026-10-04 UI修复批双配置各11PASS，历史日志由执行台账汇总。

`--verify-ui-input` 则在隐藏Windows/GL窗口中经Win32→GLFW/OpenTK→生产ImGuiController与共享清焦方法检查13项输入行为，覆盖按钮、Combo、勾选、拖动、文字、焦点、滚轮和F5输入可用性。它不直接执行完整Play准备，也不读取真实保存设计；命令与覆盖边界见 [v1-run-and-review.md](v1-run-and-review.md#run-and-verify)，历史验回可选查 [v1-ui-mouse-fix-2026-10-04.md](../reviews/v1-ui-mouse-fix-2026-10-04.md)。

固定参考沿项目 Piccolo f5053707fed4d3f94d270a436fb0d3a8ae54e3e5；此模块的 GUID 设计格式、受控场景图、输入边沿和快照命令为本项目自研，概念与采用边界见 [v1-baseline.md](../plans/v1-baseline.md) 的场景/编辑契约、[core-architecture-roadmap.md](../plans/core-architecture-roadmap.md) 与 [assets-scene-roadmap.md](../plans/assets-scene-roadmap.md)。本页不声称逐项照搬 Piccolo。

## 资源与交付后的错误保护

EditorHistory.PrepareChange由TrainingWindow接入，在设计/历史提交前校验引用并准备GPU资源，坏路径/坏GLB/图片失败会恢复原设计；Undo/Redo先准备候选，失败不移动历史游标。Load/Reset在未保存状态提供明确保存/放弃/取消选择，成功加载停止旧voice与粒子；坏用户JSON/资源启动回落种子，首次保存前备份原件。工程数据和GPU/原生准备的两个验证层分别负责结构与实际内容。
