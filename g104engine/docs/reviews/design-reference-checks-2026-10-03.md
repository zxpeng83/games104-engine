# 设计前参考源码核查：2026-10-03

本页保存本次设计对话中的源码核查、数学推导及证据边界。它不是 G104Engine 功能完成报告，也不代表用户已批准某项候选架构或实现方式。

## 核查范围与证据等级

- Piccolo 固定参考提交：`f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`；下列链接均绑定该提交，不引用浮动 `main` 作为实现依据。
- OpenTK 固定版本：`4.9.4`；核对官方源码及本机该版本 NuGet XML 文档。GL 上传、上下文和 .NET 清理规则另参考官方文档。
- “源码事实”表示所列路径可证明的行为；“推导”表示从具体公式或调用顺序推出的结论；“建议”仅表示后续设计可采用的做法。
- 本次没有构建、运行窗口或 GPU 探针，也没有安装依赖、修改源码/项目/包锁或执行 Git 写操作。下面的数值计算不是 OpenTK 库测试或 GPU 测试。
- 既往 D0–D4 的开发基础验证仍以 [阶段复查](foundation-review-2026-10-02.md) 和 [当前状态](../status.md) 中记录的证据为准；D5 的克隆、CI、第二台设备测试继续暂缓。

## Piccolo：源码能够支持的结论

### 渲染能力与范围

| 核查项 | 源码事实与限制 |
| --- | --- |
| Forward / Deferred | `render_system.cpp` 108–124 行有两条渲染路径的选择，240 行附近有 setter；本次所查 runtime/editor 范围未找到调用该 setter 的完整运行时 UI 路径。不能把“存在分支和 setter”写成“已经提供用户可操作的热切换”。[源码](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_system.cpp#L108) |
| FXAA | `render_system.cpp` 77–84 行包含初始化配置；`main_camera_pass.cpp` 342–373 行涉及随配置建立的附件。现有证据不足以承诺 FXAA 可任意运行时热切换，切换还可能牵涉渲染资源重建。[初始化](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_system.cpp#L77)、[附件配置](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/main_camera_pass.cpp#L342) |
| 可见性与绘制提交 | `render_scene.cpp` 186–223 行可见 CPU 视锥判定；`main_camera_pass.cpp` 2112–2129 行组织可见 mesh/material，2318 行附近提交 `DrawIndexed`。这些证据支持对应路径中的 CPU 可见性处理和分组提交，不等于完整 GPU-driven 主场景管线。[可见性](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_scene.cpp#L186)、[分组与绘制](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/main_camera_pass.cpp#L2112) |
| GPU 计算 | `particle_pass.cpp` 1567 行附近存在间接计算调度。因此不能从主场景 CPU 剔除路径推成“Piccolo 没有 GPU 计算或间接调度”。[粒子计算](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/particle_pass.cpp#L1567) |
| IBL | `render_resource.cpp` 27–46 行读取 HDR/BRDF 相关资源，`mesh_lighting.inl` 76–89 行参与环境光照计算。这是对应 IBL 资源与着色路径的证据，不能据此称为 Lumen。[资源读取](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_resource.cpp#L27)、[着色](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/include/mesh_lighting.inl#L76) |
| 高阶系统结论 | 在本次所查范围内，未建立完整的网络、Lumen 或 Nanite 功能链。应写成“本次未验证”，不能把目录名、课程主题、局部符号或缺少搜索命中替代完整实现证据。 |

### 对象、角色、动画与时步

- **对象与组件。** `object.h` 16–75 行、`object.cpp` 41–49 行和 `component.h` 12–24 行展示对象持有组件及逐项 tick 的关系。可用作对象/组件职责的学习入口；这本身不是数据导向 ECS 的证据。[对象声明](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/object/object.h#L16)、[对象 tick](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/object/object.cpp#L41)、[组件基类](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/component.h#L12)
- **更新顺序与时间来源。** `level.cpp` 138–162 行顺序为对象、角色、物理；`engine.cpp` 41–101 行使用实测 delta time，所查主循环没有固定步累加器。[Level](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/level/level.cpp#L138)、[主循环](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/engine.cpp#L41)
- **物理步进。** `physics_scene.cpp` 166–174 行未采用传入 delta time，而是把 `1 / update_frequency` 传给物理更新；配置默认频率为 60。它不能证明渲染循环之外存在完整固定步累加器。所查刚体创建点 119–121 行使用 Static，不能直接写成已核实完整动态刚体玩法。[物理场景](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp#L119)、[频率配置](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_config.h#L25)
- **角色移动。** `motor_component.cpp` 53–82、116–196 行使用变化的 delta time 计算位移；`character_controller.cpp` 27–42 行检查目标位置 overlap，发生重叠时不采用该次移动。这个具体路径不是完整的连续碰撞、贴墙滑动或台阶处理证据。[Motor](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/motor/motor_component.cpp#L53)、[Controller](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/controller/character_controller.cpp#L27)
- **动画。** `animation_component.cpp` 17–24 行使用 delta time；本次未建立动画 root motion 到角色位移的完整链，也未验证具体动画素材。动画更新存在不等于 root motion 已核实。[动画组件](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/animation/animation_component.cpp#L17)

### 坐标、矩阵与资源生命周期

- **坐标约定。** 物理重力配置体现 Z-up；所查角色 Motor 约定为 −Y 前、+X 左。`Math::makeLookAtMatrix` 对应右手观察空间、相机朝 −Z。世界坐标的上轴、角色模型的前轴和观察空间轴应分开描述。[重力](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_config.h#L23)、[角色方向](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/motor/motor_component.cpp#L154)、[LookAt](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/core/math/math.cpp#L115)
- **矩阵约定。** `matrix4.h` 的存储声明与乘法实现、shader 的 `row_major` 布局及 `mesh.vert` 119–121 行共同支持“行序存储、列向量计算”。不能把行序存储直接等同于行向量乘法。[Matrix4](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/core/math/matrix4.h#L67)、[Shader 布局](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/include/constants.h#L5)、[顶点变换](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/mesh.vert#L119)
- **投影差异。** `math.cpp` 140–151 行对应深度 0..1；`render_camera.cpp` 95–100 行另有 Y 翻转。不能把这套 Vulkan 投影直接搬到当前 OpenGL 4.3 默认裁剪约定。[投影](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/core/math/math.cpp#L140)、[相机 Y 翻转](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_camera.cpp#L95)
- **缓存与移交。** `render_resource.cpp` 271–345 行的 get-or-create 路径按 asset id 缓存，`render_system.cpp` 275–357 行处理交换数据；这可以支持资源复用与数据移交的学习，不直接证明引用计数、淘汰或完整释放方案。[资源缓存](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_resource.cpp#L271)、[交换数据](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_system.cpp#L275)
- **清理证据限制。** 所查 `render_resource.cpp` 18–20 行和 `render_scene.cpp` 8–10 行的 `clear` 为空；不能从函数名推断其已完成资源释放，也不能据此断言整个引擎所有位置都没有释放代码。[资源 clear](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_resource.cpp#L18)、[场景 clear](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_scene.cpp#L8)

## 补充核查：渲染模块范围（2026-10-03）

为讨论渲染实现深度，继续只读核对同一固定 Piccolo 提交的管线初始化、主绘制调用、相关 GLSL/include 和 README。以下是实际调用与着色代码证据，不是本次 GPU 验证；文件名、资源字段或 Pass 已接入不能单独证明效果完整。

| 范围 | 本次补充结论与源码 |
| --- | --- |
| PBR 与法线 | Forward 的 `mesh.frag` 采样基础颜色、金属度/粗糙度及法线，经 TBN 后调用公共光照；BRDF 有 GGX、几何项及 Schlick Fresnel。Deferred 解码 G-buffer 后调用相同光照。可称基础链路存在，不宣称全部材质能力或物理准确性已验证。[材质采样](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/mesh.frag#L71)、[BRDF](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/include/mesh_lighting.h#L6)、[Deferred 消费](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/deferred_lighting.frag#L60) |
| 阴影 | 方向光和点光 Shadow Pass 在主相机前调用，公共光照代码采样并比较深度。所查方向光为单张投影阴影；点光使用两层半球参数化纹理数组，不是六面 cubemap shadow，并受功能开关控制。[管线调用](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_pipeline.cpp#L184)、[阴影采样](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/include/mesh_lighting.inl#L28)、[点光条件](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/point_light_pass.cpp#L514) |
| IBL 与天空 | IBL 采样预计算辐照度、镜面预滤波 mip 与 BRDF LUT。Forward 有 drawSkybox 调用，天空 Shader 采样 cubemap；Deferred 的 unlit 分支也处理天空。本链不是大气散射或体积云。[IBL](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/include/mesh_lighting.inl#L76)、[天空绘制](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/main_camera_pass.cpp#L2051)、[天空 Shader](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/skybox.frag#L14) |
| Tone Mapping / FXAA | 主链实际调用 ToneMapping 并按配置调用 FXAA；前者有 Uncharted2 曲线、白点及 gamma，后者有亮度边缘检测与采样调整。这不等于自动曝光或 TAA。[后处理调用](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/main_camera_pass.cpp#L1970)、[Tone Mapping](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/tone_mapping.frag#L13)、[FXAA](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/fxaa.frag#L71) |
| Color Grading 与未完成通道 | ColorGradingPass 已初始化并调用，但 Shader 只获取 LUT 尺寸、读取输入后原样输出，尚未应用 LUT 调色。也不能从 AO/emissive sampler 声明推断功能完成；所查 G-buffer emissive 输出被注释。[调色 main](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/color_grading.frag#L13)、[G-buffer emissive](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/mesh_gbuffer.frag#L56) |
| 限定检查未发现 | 在上述范围未发现 Bloom、地形、体积云、SSAO、TAA 的完整主链接入；未发现 Bloom 的明确 TODO。这不代表所有仓库路径或历史版本绝无实现。[初始化列表](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_pipeline.cpp#L20)、[主绘制顺序](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/main_camera_pass.cpp#L1948) |

本地渲染笔记按第 1–16 章标题及相关段落定位，未做全文事实审计。它提供课程思路，但其中性能、硬件和算法能力的简化表述仍应结合源码/资料核查，不直接写成工程保证。模块范围讨论见 [渲染路线](../plans/rendering-roadmap.md)。

## 补充核查：资产、场景与模板（2026-10-03）

继续只读核对同一 Piccolo 固定提交的资源类型、对象加载/保存、Level 保存、运行期加载器和反射生成入口。未读取大素材、运行转换器或加载模型；以下结论不代表本工程已实现对应能力。

| 范围 | 实际行为与限制 |
| --- | --- |
| 定义与实例覆盖 | GObject 先装实例组件，再读取定义组件；同类型已存在则跳过定义组件，是整组件覆盖而非逐字段合并。保存会把当前合并后的全部组件写回实例列表，不只保存 override 差异，不能视为完整 Prefab 系统。[load/save](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/object/object.cpp#L63) |
| 对象身份与层级 | ObjectInstanceRes 保存名称、定义路径、组件列表，没有持久对象 ID 或 parent 字段；创建时重新分配递增运行期 ID。组件的 parent_object 表示宿主对象，不是场景对象父子层级。[对象格式](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/resource/res_type/common/object.h#L31)、[创建](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/level/level.cpp#L27)、[ID 分配](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/object/object_id_allocator.cpp#L7) |
| 场景保存 | 所查 Level::save 创建 LevelRes 并填对象数组，没有把格式中的 gravity、character_name 填回，因此不能称完整无损场景往返。删除方法也不自动证明有引用修复或 Undo。[保存](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/level/level.cpp#L104)、[格式字段](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/resource/res_type/common/level.h#L15)、[删除](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/level/level.cpp#L176) |
| 输入格式 | 网格有 OBJ/tinyobjloader 和自有 JSON/MeshData 分支；图片使用 stbi_load/stbi_loadf；骨架、动画、映射和 mask 走 AssetManager 的 JSON 读取链。限定范围未核到运行期 FBX/Assimp 链或可调用的完整离线 FBX 转换器，不能用课程讨论补成已实现导入流程。[网格/图片入口](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_resource_base.cpp#L23)、[动画加载](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/animation_loader.cpp#L74) |
| 资产路径与 GuidAllocator | AssetManager 拼接资源根与相对路径，动画缓存以文件路径作 key；渲染 GuidAllocator 是来源描述到运行期整数的映射，没有证明存在跨移动/重命名的持久 UUID。[路径解析](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/resource/asset_manager/asset_manager.cpp#L11)、[动画缓存](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/animation_system.cpp#L10)、[运行期映射](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_guid_allocator.h#L15) |
| 反射生成 | 预编译 CMake 调用 PiccoloParser，Parser 建立序列化与反射生成器。这是元数据/JSON 读写基础设施，不自动包含资产增量导入、依赖构建、Prefab 变体或 Undo/Redo。[生成器](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/meta_parser/parser/parser/parser.cpp#L42)、[构建调用](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/precompile/precompile.cmake#L35) |

glTF 的网格/材质/蒙皮/动画与坐标语义另查 Khronos 2.0 规范；JSON 读写能力查 System.Text.Json 官方资料。它们用于本项目的格式/数据边界选择，不是新解析库的兼容性测试或安装记录。具体方向与待办见 [资产/场景路线](../plans/assets-scene-roadmap.md)。

## 补充核查：物理查询、过滤与所有权（2026-10-03）

核查范围为同一固定 Piccolo 提交的 PhysicsScene/Manager、Jolt 适配、RigidBodyComponent 及资源类型。没有广扫第三方示例，也未运行物理测试；Jolt 自身能力不等于 Piccolo 已完成对应集成。

| 范围 | 实际行为与设计意义 |
| --- | --- |
| Raycast/Sweep/Overlap | 分别实际调用 Jolt CastRay、CastShape、CollideShape；前两者收集全部命中后排序，返回位置/法线/距离/BodyID，Overlap 返回布尔值。这不证明控制器已使用 Sweep 实现滑墙。所查包装没有逐次查询 mask/忽略自身参数；raycast/sweep 无命中时在清空 out_hits 之前返回，调用者不能假定旧列表已清空。[查询实现](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp#L186) |
| 碰撞层与 Trigger | 初始化注册了 BroadPhase/对象层过滤，规则含 NON_MOVING、MOVING、DEBRIS、SENSOR；创建体仍为 Static/NON_MOVING，所查适配层未见完整 sensor 标记和触发事件桥接。层名不等于 Trigger 已实现。[注册](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp#L52)、[过滤](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/jolt/utils.cpp#L47) |
| 形状与重力 | toShape 构造 Box/Sphere/Capsule，多形状组合为 StaticCompoundShape；不能据此称任意导入网格碰撞已接入。所查创建链没有用资源的 inverse_mass/actor_type 决定完整动力学行为。重力在场景初始化传给后端，包装公开读取接口。[形状](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/jolt/utils.cpp#L115)、[创建](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp#L93)、[公开接口](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.h#L47) |
| 同步方向 | 场景姿态写入调用 SetPositionAndRotation；缩放变化请求删除后重建，RigidBodyComponent::tick 为空。只改 Static 枚举不会自动补齐质量/惯量、动力学结果回写、事件和显示同步。[姿态写入](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp#L156)、[缩放处理](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/rigidbody/rigidbody_component.cpp#L60)、[空 tick](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/rigidbody/rigidbody_component.h#L19) |
| 延迟删除与场景寿命 | removeRigidBody 入队，tick 先模拟再 RemoveBody/DestroyBody；组件析构向当前活动物理场景请求删除。Manager 删除其 shared_ptr 不等于所有引用立刻消失，最后强引用结束后才析构。[排队/执行](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp#L154)、[组件清理](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/rigidbody/rigidbody_component.cpp#L33)、[Manager](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_manager.cpp#L40) |

PhysicsScene 构造对全局 `JPH::Factory::sInstance` 赋值，析构删除该全局槽位；本次没有验证多个物理场景同时存在。这种所有权安排不能当作本项目“新场景准备成功后替换旧场景”的现成实现依据。我们需要分别管理库级初始化与场景世界，并让对象清理明确指向所属场景，而非依赖任意时刻的活动场景。[构造与析构](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp#L34)

所查适配链没有建立完整可配置物理材质、动力学、Trigger、Joint、Ragdoll、Cloth、Vehicle 功能证明。对应本工程的范围、分工选择与待办见 [物理/角色路线](../plans/physics-character-roadmap.md)。

## 补充核查：动画求值、蒙皮与跨 Pass 一致性（2026-10-03）

同一固定 Piccolo 提交下只读核对 AnimationComponent/Manager、Skeleton、Bone/Node、MeshComponent、绑定加载以及主绘制/阴影/Pick 相关 Shader。未读取大动画素材或运行 GPU，以下是消费路径事实，不能扩展为所有资源内容已验证。

| 范围 | 源码事实与限制 |
| --- | --- |
| 初始化与层级 | Bone 从 binding_pose 初始化局部 TRS 并保存 initial pose；资源 tpose_matrix 直接赋给 inverse_Tpose 字段，不是在这里现场求逆。Skeleton 要求 flat 且拓扑有序；父子派生变换按数组顺序更新，缩放组合注明未处理 shear。[初始化](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/node.cpp#L220)、[层级传播](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/node.cpp#L25)、[Skeleton 前提](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/skeleton.cpp#L17) |
| 关键帧消费 | 每次先 reset 到 initial pose，再施加采样的 rotate/scale/translate；默认旋转局部右乘、缩放相乘、平移在父空间相加。这不是 glTF 绝对局部 TRS 的直接替换求值器；本次也未验证所有自有 JSON 键帧均符合该消费约定。[采样与应用](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/skeleton.cpp#L39)、[默认空间](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/node.h#L78)、[旋转/平移](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/node.cpp#L107)、[缩放/重置](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/node.cpp#L182) |
| 混合结构与实际行为 | Manager 读取多个 Clip、骨骼映射与 Mask 并计算每骨骼权重，但 Skeleton 只处理 clip_index<1，实际权重固定为 1，未有效消费混合权重。AnimSkelMap 的索引映射也不能称通用 Retarget。[权重准备](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/animation_system.cpp#L83)、[实际消费者](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/skeleton.cpp#L45) |
| Palette 与索引 | Skeleton 输出元素标记 boneID+1；MeshComponent 先放 identity，再按结果数组顺序放矩阵，没有按元素 index 定位。JSON 绑定索引被原样复制，主 Shader 仅累计至多四个 index>0 且 weight>0 的项。顺序与索引必须相容，identity 槽也不自动成为 fallback；不能把该约定照搬为 glTF Joint 0 无效。[输出](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/skeleton.cpp#L119)、[Palette](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/mesh/mesh_component.cpp#L70)、[绑定加载](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_resource_base.cpp#L133)、[主 Shader](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/mesh.vert#L72) |
| 阴影与 Pick | 方向光/点光阴影消费蒙皮后的 model_position；Pick Shader 虽计算该变量，最终 gl_Position 却使用原始 in_position，且 PickPass 实际使用此 Shader。这是所查路径不一致的源码证据，不是本次已运行观察到的画面结果。[方向光](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/mesh_directional_light_shadow.vert#L30)、[点光](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/mesh_point_light_shadow.vert#L30)、[Pick Shader](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/mesh_inefficient_pick.vert#L42)、[PickPass](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/pick_pass.cpp#L208) |

在所查动画与相关资源路径未建立状态机、BlendSpace、IK、Root Motion 提取/应用、事件及通用 Retarget 完整链路的证据。可借鉴基础数据流，但不把占位结构当作本工程的已完成方案。glTF 采样语义与本项目候选实验另见 [动画路线](../plans/animation-roadmap.md)；采样/蒙皮、输入格式子集、状态/事件和跨 Pass 一致性均需实际实现后验收。

## 补充核查：Gameplay、Lua 与 AI 边界（2026-10-03）

固定 Piccolo 的自有 runtime 中只读检查 Lua/Input/Character/Motor/Camera 与相关反射桥；没有扫描第三方示例或大型素材，没有执行脚本或玩法。以下不等于本项目已经具备对应功能。

| 范围 | 已核行为与限制 |
| --- | --- |
| Lua 执行 | 每个 LuaComponent 有 sol::state 和组件内脚本文本，加载时开放 base 库并绑定函数/宿主 GameObject，每次 tick 执行该字符串。所查链没有脚本文件监视、重载事务或状态迁移；字符串执行不能称完整 hot reload。[字段](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/lua/lua_component.h#L26)、[绑定/执行](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/lua/lua_component.cpp#L148) |
| 反射调用 | 导出 set_float、get_bool、invoke；按组件/字段点分路径查找。所查生成方法桥为无参数/无返回值调用，不是任意签名调用或已完成的 Gameplay API，复杂路径和错误情况未运行验证。[字段访问](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/lua/lua_component.cpp#L7)、[invoke](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/lua/lua_component.cpp#L85)、[生成桥](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/template/commonReflectionFile.mustache#L44) |
| 输入映射 | WindowSystem 注册键鼠回调，InputSystem 通过固定 GLFW_KEY 分支更新命令位。未由此证明可重绑定 Action、设备配置或输入上下文优先级；窗口回调广播也不是通用 Gameplay EventBus。[固定映射](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/input/input_system.cpp#L25)、[注册/tick](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/input/input_system.cpp#L140)、[窗口回调](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/window_system.h#L149) |
| 当前玩家 3C | Motor/Camera 校验宿主 ID 是否为活动玩家，不相同则返回；Motor 消费玩家命令，Camera 分派第一/第三人称与自由模式，Character 协调目标位置与朝向。不能直接视作多 NPC 的通用意图执行架构。[Motor](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/motor/motor_component.cpp#L53)、[Camera](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/camera/camera_component.cpp#L48)、[Character](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/character/character.cpp#L41) |
| 通用 AI 结论 | 限定搜索自有 runtime 的 FSM/StateMachine、BT、A*/Pathfinding、NavMesh/Recast/Detour、Perception、EventBus，未建立完整通用系统链路证据。但 Motor 有 idle/rising/falling 局部 JumpState，不能说“完全没有任何状态逻辑”。[JumpState](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/motor/motor_component.h#L16)、[局部转换](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/motor/motor_component.cpp#L120) |

本工程采用哪些规则、决策与导航能力见 [Gameplay/AI 路线](../plans/gameplay-ai-roadmap.md)。原笔记中的射击/战斗案例不扩张已确认的无战斗训练场范围。

## 补充核查：粒子链路与音频接入范围（2026-10-03）

同一固定 Piccolo 提交下只读检查 ParticleComponent/Manager、交换数据消费、主相机/粒子 Pass、相关 Shader，以及自有 runtime 的音频命名、构建清单和系统初始化。未运行 GPU、播放音频或读取第三方库目录。

| 范围 | 实际链路与限制 |
| --- | --- |
| 组件到渲染 | 组件加载创建 emitter、tick 提交更新/变换；Manager 写交换请求，RenderSystem 消费后创建/更新粒子 Pass。主绘制之后提交绘制、复制深度/法线，再 simulate，不是只有未接入的 Compute 文件。[组件](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/particle/particle_component.cpp#L14)、[请求消费](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_system.cpp#L413)、[管线时序](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_pipeline.cpp#L208) |
| GPU 池与模拟 | Kickoff 根据空闲数生成发射/模拟的 dispatch 参数并翻转 Alive 标记；Emit 原子取得空闲槽位，Simulate 回收死亡粒子、写另一个 Alive 列表及紧凑绘制数据。CPU 确实调用 kickoff dispatch 和后续 dispatchIndirect。[Kickoff](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/particle_kickoff.comp#L49)、[Emit](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/particle_emit.comp#L174)、[回收/输出](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/particle_simulate.comp#L144)、[模拟调用](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/particle_pass.cpp#L1685) |
| 回读与绘制 | simulate 逐 emitter 等待 fence，复制 Counter 到 host buffer，再等待/queueWaitIdle 并 map 读取存活数更新 CPU m_num_particle；draw 使用 cmdDraw(4, count)，不是 drawIndirect。只能称 GPU Compute 模拟＋间接 dispatch，不能称全程无回读或已证明高效。[复制准备](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/particle_pass.cpp#L1711)、[等待/读取](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/particle_pass.cpp#L1796)、[直接绘制](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/particle_pass.cpp#L309)、[Billboard](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/particlebillboard.vert#L31) |
| 深度碰撞与排序 | 输入为主深度和 GBuffer A 法线。Shader 只在投影 XY 屏幕范围内采样，以源码中的厚度 0.5 判断、速度朝表面时反射并乘 0.4；这些不是本工程选定参数。屏外/隐藏表面不完整，Forward 也不能仅凭调用粒子就宣称法线输入正确。限定 Manager/Pass/Shader 未见排序阶段，原子压紧不等于透明排序；实际 alpha 混合且不写深度。[输入](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/main_camera_pass.cpp#L2717)、[碰撞](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/glsl/particle_simulate.comp#L83)、[混合状态](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/particle_pass.cpp#L1158) |
| 音频范围 | 所查自有 runtime、Runtime/Engine CMake 和 global context 未发现完整 source/listener/device/playback 接入；NOSOUND 宏命中与声音系统无关。不据此断言全部历史或第三方代码没有音频能力。[Runtime 清单](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/CMakeLists.txt#L43)、[系统初始化](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/global/global_context.cpp#L26) |

据此可参考粒子生命周期和数据流，再单独设计时间、排序、同步与测量；音频需另选后端与明确参考。当前方向、范围和待办见 [粒子/声音路线](../plans/particles-audio-roadmap.md)，没有本工程粒子或声音运行验证。

## 补充核查：工具编辑、模式切换与观察边界（2026-10-03）

同一固定 Piccolo 提交下只读检查 editor UI/input/scene、相关组件实际消费者、WorldManager、LevelDebugger、FPS 与 Vulkan 调试标签路径。没有运行编辑器，也没有审计全部历史分支；下列缺失结论仅限所查路径。

| 议题 | 源码事实与边界 |
| --- | --- |
| 对象列表与属性 | 对象列表遍历 map 并以 Selectable 展示，为平面列表；反射字段取得指针后交给对应控件，所查路径未见通用校验/通知/Undo 事务，不能称为完整层级编辑器。[列表](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_ui.cpp#L408-L432)、[字段遍历](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_ui.cpp#L453-L523)、[float 控件](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_ui.cpp#L158-L173) |
| 属性实际生效 | motor 的 move_speed 在每次位移计算直接读取，不能用作“修改只影响序列化、运行永不使用”的例子。胶囊在加载时创建控制器并复制，Mesh 也在加载时生成派生描述；这些字段的编辑需另查重建路径，不能统一推断。[速度读取](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/motor/motor_component.cpp#L190-L196)、[控制器创建](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/motor/motor_component.cpp#L19-L28)、[胶囊复制](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/controller/character_controller.cpp#L12-L24)、[网格派生数据](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/mesh/mesh_component.cpp#L16-L39) |
| 创建、保存与重载 | 文件树仅在对应类型为 object 时创建对象；Save/Reload 是独立菜单。WorldManager 的 Reload 先卸载旧关卡，再按路径加载磁盘内容，不是保留未保存设计的运行停止语义，也不是本项目的成功后替换策略。[创建](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_ui.cpp#L827-L847)、[菜单](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_ui.cpp#L330-L338)、[重载](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/world/world_manager.cpp#L121-L160) |
| Editor/Game Mode | 切换模式标志、轴/输入焦点与相机视图；该路径不重载磁盘，也不恢复进入运行前的内存设计快照，不能直接当作我们的 Play/Stop 实现。[模式切换](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_ui.cpp#L686-L707) |
| 拾取与变换编辑 | 点击拾取的 Mesh ID 映射对象选择；轴操作分解矩阵后调用 Transform setter，编辑 tick 标记选中对象 dirty；Transform 编辑分支复制反射值到后继缓冲。存在编辑链路不等于所有物理形状缩放和动画拾取都正确，蒙皮 Pick 差异见动画核查。[拾取](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_input_manager.cpp#L269-L289)、[轴编辑](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_scene_manager.cpp#L407-L423)、[dirty](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_scene_manager.cpp#L22-L31)、[Transform](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/component/transform/transform_component.cpp#L38-L50) |
| 调试与性能 | LevelDebugger 有骨骼/名称/包围盒/相机信息显示；引擎平滑帧间隔换算 FPS 并写窗口标题。Vulkan DebugUtils push/pop 是调试标签，不是 GPU 计时；所查路径不能证明已有完整 Profiler、Undo 或暂停单步系统。[模块显示](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/level/level_debugger.cpp#L17-L39)、[FPS 计算](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/engine.cpp#L105-L119)、[标题](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/engine.cpp#L86-L87)、[DebugUtils](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/interface/vulkan/vulkan_rhi.cpp#L3524-L3543) |

工具链笔记的编辑事务、Command、Schema、PIE 与反射作为设计学习入口；旧案例中属性生效的说明需按具体版本和实际消费者复核。原笔记保持不改，本页记录固定 main 的差异，不将课程示意当作源码保证。

我们已确认的有限 Undo、运行隔离与轻量观察分期见 [工具/调试路线](../plans/tools-debug-roadmap.md)，这是本项目设计，不是声称 Piccolo 已具备这些完整能力。[Dear ImGui 官方项目](https://github.com/ocornut/imgui)说明界面工具包的定位，[Khronos glQueryCounter](https://github.com/KhronosGroup/OpenGL-Refpages/blob/main/gl4/glQueryCounter.xml)用于后续 GPU 时间查询机制参考；UI 库尚未选定，初版仅 FPS/帧耗时，没有本工程性能测量结果。

## 补充核查：主流程、同步加载与内部线程（2026-10-03）

同一固定 Piccolo 提交下只读检查自有 runtime、Editor 主循环及定向 thread/async/future、Job/Task/Scheduler、ECS/archetype 等入口。未构建或进行并发/性能实验；缺失结论不覆盖全部第三方能力、GPU 执行与历史版本。

| 议题 | 源码事实与边界 |
| --- | --- |
| 主帧执行 | tickOneFrame 顺序调用 logicalTick、swapLogicRenderData 和 rendererTick，注释明确 single thread；Editor 主循环顺序调用编辑器更新与同一主帧函数。不能从模块名推断已有独立渲染线程。[运行时主帧](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/engine.cpp#L68-L102)、[Editor](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor.cpp#L51-L62) |
| 数据交换 | RenderSwapContext 保存两份 RenderSwapData 和普通索引；检查渲染侧 optional 请求清空后交换索引。所查结构不自行创建逻辑/渲染线程或建立跨线程发布/复用协议；双缓冲不自动等于并行。[存储](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_swap_context.h#L123-L129)、[就绪](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_swap_context.cpp#L33-L54)、[交换](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_swap_context.cpp#L88-L97) |
| 对象组件组织 | Level 遍历对象 map 调用 GObject.tick，对象遍历多态组件 vector；随后更新活动 Character 与 PhysicsScene。所查链路不是按组件连续存储/查询/批处理的完整数据导向 ECS。[Level](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/level/level.cpp#L138-L162)、[tick](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/object/object.cpp#L41-L49)、[组件存储](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/object/object.h#L68-L75) |
| 场景与资产加载 | WorldManager 直接调用 Level.load；后者加载描述、创建物理场景并逐个创建对象。AssetManager 在当前调用读文件、解析 JSON、反序列化后返回。渲染首次遇到网格/材质也在 processSwapData 调用链加载并上传，不能称为后台资产流系统。[World](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/world/world_manager.cpp#L74-L118)、[Level](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/level/level.cpp#L55-L75)、[JSON读取](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/resource/asset_manager/asset_manager.h#L19-L45)、[渲染资源](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_system.cpp#L287-L351) |
| 物理内部线程 | PhysicsScene 创建 JPH::JobSystemThreadPool，并传给 PhysicsSystem::Update；这是物理库内的任务执行，不证明引擎已调度输入/Gameplay/动画/渲染的通用任务图。[创建](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp#L41-L47)、[使用](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp#L166-L174) |
| 日志内部线程 | LogSystem 初始化 spdlog 线程池并创建 async_logger。准确说法是主要逻辑/渲染顺序执行，不能说整个进程只有一条线程。[日志初始化](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/core/log/log_system.cpp#L10-L24) |

本次所查自有主流程未发现接入的通用引擎 Job 调度器或完整数据导向 ECS。Vulkan 队列/Compute 的 GPU 工作、库内 Worker 和异步日志均需单独区分。由此可借鉴数据交接与组件职责，再自行定义本项目的布局与调度实验，不能把课程第20节所有架构机制标为 Piccolo 已完成。

本轮另局部复习第20节的 3.11、4.7–4.8、5.8–5.9、6.3–6.7，区分数据组织、任务执行、执行上下文与测量；有栈机制对应 3.3.6，普通 C# 状态机示例不能冒充有栈 Fiber。外部原理核对采用 [Microsoft TPL 概览](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl) 与 [并行编程注意事项](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/potential-pitfalls-in-data-and-task-parallelism)：并行仍需处理分块、共享状态与等待，调度开销可能抵消收益。选定路线及尚未定案的具体机制见 [核心架构路线](../plans/core-architecture-roadmap.md)，没有本工程调度或性能结果。

## OpenTK 4.9.4：矩阵与 GPU 上传

### 已核实的源码事实

- `Matrix4` 使用顺序布局，字段依次为 `Row0`–`Row3`；`Vector4` 的分量按 X/Y/Z/W 顺序存储。
- `CreateTranslation` 把平移写入 `Row3.xyz`；`CreateFromQuaternion` 的具体系数对应行向量旋转，例如绕 +Z 旋转 90° 时，行向量 +X 变为 +Y。[Matrix4 源码](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Mathematics/Matrix/Matrix4.cs#L849)
- `Vector4.TransformRow(v, A)` 计算 `v * A`；`TransformColumn(A, v)` 计算 `A * v`。这里的结论来自分量公式，不把文档中的 “right-handed notation” 当作坐标系左右手性的判据。[Vector4 源码](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Mathematics/Vector/Vector4.cs#L1207)
- `LookAt` 构造世界到观察空间的行矩阵；前方为观察空间 −Z。`CreatePerspectiveFieldOfView` 调用 `CreatePerspectiveOffCenter`，后者有 `Row2.W = -1`，可推得 `w_clip = -z_view`；默认近/远面对应 NDC Z 的 −1/+1。[LookAt](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Mathematics/Matrix/Matrix4.cs#L1453)、[透视投影](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Mathematics/Matrix/Matrix4.cs#L1281)
- `GL.UniformMatrix4(location, transpose, ref matrix)` 直接把 `&matrix.Row0.X` 传给底层调用，原样传递 `transpose`，没有额外转置。[OpenGL4 封装](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Graphics/OpenGL4/Helper.cs#L685)
- Khronos 规定 `transpose = false` 按列序解释传入数据，`true` 按行序解释。因而，CPU 行序存储的数学矩阵 `A` 用 `false` 上传后，GLSL 中的数学矩阵是 `Aᵀ`；用 `true` 上传则是 `A`。[Khronos glUniform 文档源码](https://github.com/KhronosGroup/OpenGL-Refpages/blob/main/gl4/glUniform.xml#L427)

### 从上述事实推出的两套一致用法

以下限定为使用上述 OpenTK 工厂，并在 CPU 采用 `TransformRow` 的情形；不是声称所有存入 `Matrix4` 的值都只能表示行向量变换。

| CPU 计算 | 逐个上传 M/V/P | GLSL 计算 |
| --- | --- | --- |
| `M = S * R * T`；`childWorld = local * parentWorld` | `GL.UniformMatrix4(..., false, ref cpuRow)` | `P * V * M * vec4(position, 1)` |
| 同上 | `GL.UniformMatrix4(..., true, ref cpuRow)` | `vec4(position, 1) * M * V * P` |

两者等价的理由是：`(p_row * M_row * V_row * P_row)ᵀ = P_rowᵀ * V_rowᵀ * M_rowᵀ * p_column`。CPU 若预合成 MVP，行向量合成顺序仍为 `M * V * P`。

用户已选定“CPU 原生行向量、GLSL 列向量”策略，对应表中第一行；第二行仅为解释另一套一致用法。具体上传参数来自源码与公式推导，并非曾运行本工程验证；未来 UBO/SSBO 布局仍需单独确定。

需要分清存储顺序、向量乘法方向和上传解释三件事。不要在第一套用法中再额外转置 CPU 矩阵；也不要把原生 `CreateTranslation` 结果直接交给 `TransformColumn` 并期待同样的 XYZ 平移。

### 本对话的独立数值核验

下列四项独立算术检查均符合预期；未调用 OpenTK，也未创建 GL 上下文，不是现有或未来代码的自动化测试通过记录。

| 检查 | 输入与预期结果 |
| --- | --- |
| 缩放、旋转、平移 | `S=(2,3,4)`、绕 Z 轴 +90°、`T=(5,6,-7)`；点 `(1,0,0,1)` 按 `S*R*T` 变为 `(5,8,-7,1)`。 |
| 父子变换顺序 | 子节点沿 X 平移 1、父节点绕 Z 轴 +90°；子节点原点按 `local*parentWorld` 变为 `(0,1,0,1)`。 |
| CPU/GPU 代数等价 | 上述模型点再经观察变换 Z 平移 −3；投影 X/Y 对角系数为 1、near=0.1、far=100。CPU 行表达式与 GPU 等价列表达式均得到 clip `(5,8,9.81981981981982,10)`。 |
| 近远面深度 | 同一透视投影把观察空间 Z=−near / −far 映射到 NDC Z=−1 / +1。 |

## OpenTK 4.9.4：循环、尺寸与清理

### 更新和显示

- `GameWindow.Run()` 在一次迭代内依次调用 `OnUpdateFrame(elapsed)`、`OnRenderFrame(elapsed)`，二者使用相同的实际 elapsed，并受同一个频率门限控制。旧 `RenderFrequency` 已被标为不可使用；不能描述为两个独立频率的内置循环。
- `UpdateFrequency = 0` 可取消 OpenTK 的频率上限；启用 VSync 时，换帧仍可能限制整体循环速度。
- **可行性推导：** 可以在 `OnUpdateFrame` 将实际 elapsed 交给自研累加器，执行零次或多次固定 dt 的模拟，再在 `OnRenderFrame` 绘制一次。固定的是模拟步长，不是回调到达间隔；输入边沿不能在同一显示帧的多个模拟步中重复消费。用户已选定 60 Hz（dt 为 1/60 秒）；补步上限和超限时间处理仍属待确认设计。[GameWindow 固定版本源码](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Windowing.Desktop/GameWindow.cs)
- `FramebufferSize` 调用 `GLFW.GetFramebufferSize`，且可覆盖 `OnFramebufferResize`。`Run()` 主动发出的初始尺寸事件是 `OnResize(ClientSize)`，不能依赖它自动完成 framebuffer 像素尺寸初始化。后续实现宜在加载时主动读取 framebuffer 尺寸，并在其变化时更新 viewport 和投影宽高比；宽或高为零时跳过绘制和除法。[NativeWindow 固定版本源码](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Windowing.Desktop/NativeWindow.cs)

### GPU 生命周期与异常路径

源码的正常顺序为：`MakeCurrent → OnLoad → 首次 OnResize → 更新/渲染循环 → OnUnload → Run 返回`。调用方随后执行窗口 `Dispose()`；`GameWindow.Dispose()` 先调用基类，最终由 `NativeWindow.Dispose(bool)` 调用 `GLFW.DestroyWindow()`。

`Run()` 没有围绕上述回调的 `try/finally`，`Dispose()` 也不补调 `OnUnload()`。所以初始化、更新或渲染中途抛出异常，会跳过正常的 `OnUnload()`。不能把 OnUnload 描述为覆盖所有异常路径的自动清理保证。[GameWindow](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Windowing.Desktop/GameWindow.cs)、[NativeWindow](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Windowing.Desktop/NativeWindow.cs)

由此得到的基础版清理建议如下；这是生命周期约束，不是已经实现的代码：

1. 在同一主线程创建窗口、运行循环、释放 GL 资源并销毁窗口；回调中请求 `Close()`，不提前 `Dispose()` 窗口。
2. 外层 `using` 持有窗口，内层 `try/finally` 包住 `Run()`；内层 finally 先执行幂等 GPU 清理，退出外层 using 后才销毁窗口。正常 OnUnload 可调用同一清理入口。
3. 清理入口允许部分初始化；有句柄待释放时先确保所属上下文 current，再逐项删除。单项清理失败不能阻止其他资源尝试清理，也不能悄悄覆盖原始初始化错误。
4. GPU 创建放在明确初始化阶段。资源工厂负责回滚失败途中尚未移交所有权的句柄，不能只依赖成功赋给字段后的对象清理。
5. 该路径覆盖窗口已创建、上下文仍有效时的正常退出及托管异常展开；不承诺强制终止进程或上下文丢失后的正常 GL 清理。

GLFW 的窗口销毁会同时销毁上下文；GL 调用需要正确上下文在当前线程上。GC 不负责替应用执行 GL 删除，也不能把 GL 删除放到无上下文保证的终结器线程。`using` 则会在正常离开及托管异常展开时调用 Dispose。[GLFW 当前上下文](https://www.glfw.org/docs/3.4/context_guide.html#context_current)、[GLFW 窗口销毁](https://www.glfw.org/docs/3.4/group__window.html#gacdf43e51376051d2c091662e9fe3d7b2)、[.NET Dispose](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose)、[C# using](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using)

## 后续验证建议及未验证事项

- 当前 Engine + Sandbox 容器足以承载小规模验证，不必先新增测试项目或测试框架包。可在 Sandbox 规划独立 CPU 自检入口，使用显式断言和失败退出码；现有仅打印环境信息的 `--smoke` 不替代这些检查，也不能只用 Release 中可能不执行的 `Debug.Assert`。
- 固定步调度用合成时间序列验证小于一步、跨步、多步、长暂停和剩余时间策略；矩阵用手算预期值、非交换变换和近远面测试验证。应调用实际待验证逻辑，而非仅复制其公式。
- 用轻量假资源检查部分初始化、重复清理、清理顺序与单项失败；真实 GL 清理仍需独立图形场景验证，例如 shader 编译失败或初始化中途异常，核对上下文有效期与已创建句柄释放。
- M1 的可见三维结果、真实 shader/顶点数据、GPU 矩阵上传、缩放/高 DPI、最小化恢复和初始化失败清理均没有因本次源码核查而自动通过。
- 如后续需要标准测试项目或新增测试包，应另行讨论，并继续由用户按既有分工操作 VS 工程创建和 NuGet 配置/还原；本页不授权新增工程、依赖或恢复 D5。
