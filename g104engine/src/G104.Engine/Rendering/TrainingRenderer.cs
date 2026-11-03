using G104.Engine.Animation;
using G104.Engine.Assets;
using G104.Engine.Scene;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace G104.Engine.Rendering;

/// <summary>本帧多Pass提交统计；不包含独立GPU计时，也不等于唯一场景几何数量。</summary>
/// <param name="DrawCalls">网格、全屏和粒子绘制调用总数。</param>
/// <param name="Triangles">各Pass累计提交的三角形数。</param>
/// <param name="AnimatedInstances">当前缓存中标记为角色的动画实例数。</param>
/// <param name="Width">本帧渲染目标像素宽度。</param>
/// <param name="Height">本帧渲染目标像素高度。</param>
public readonly record struct RendererStatistics(int DrawCalls, int Triangles, int AnimatedInstances, int Width, int Height);

// 固定Pass顺序用于学习和对照；GL资源始终由创建它们的当前上下文线程释放。
/// <summary>持有固定渲染Pass及共享GPU资源；所有GL操作由创建线程在有效当前上下文执行。</summary>
public sealed class TrainingRenderer : IDisposable
{
    /// <summary>用于检查GL调用归属的创建线程标识，不能证明上下文本身仍然有效。</summary>
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    /// <summary>解析shader、配置、模型与纹理的资产根。</summary>
    private readonly AssetRoot assets;
    /// <summary>解析CPU侧模型数据；其结果随后上传为共享GPU网格。</summary>
    private readonly GltfModelLoader loader;
    /// <summary>本渲染器拥有的共享纹理缓存，按活动绘制引用保留。</summary>
    private readonly TextureCache textures;
    /// <summary>角色Clip映射及动画参数配置，用于验证和创建实例控制器。</summary>
    private readonly AnimationSettings animationSettings;
    /// <summary>窗口寿命内共享的基础网格，渲染器最终统一释放。</summary>
    private readonly Dictionary<PrimitiveKind, GpuMesh> primitives = [];
    /// <summary>按模型路径复用的CPU数据与GPU网格；离场时释放网格。</summary>
    private readonly Dictionary<string, GpuModel> models = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>按对象GUID保存独立动画控制器，同时借用共享模型。</summary>
    private readonly Dictionary<Guid, (GpuModel Model, AnimationController Controller, bool Character)> instances = [];
    /// <summary>当前帧实际请求的纹理键，用于帧末清除不再引用的纹理。</summary>
    private readonly HashSet<string> usedTextures = new(StringComparer.Ordinal);
    /// <summary>forward为前向着色；gbuffer为几何写入；shadow为阴影深度；deferred为延迟光照。</summary>
    /// <remarks>sky为背景；tonemap为调试/曝光映射；fxaa为最终抗锯齿；particle为透明粒子，程序均由渲染器拥有。</remarks>
    private ShaderProgram? forward, gbuffer, shadow, deferred, sky, tonemap, fxaa, particle;
    /// <summary>当前尺寸的G-buffer/HDR/LDR附件；Resize成功后原子替换并释放旧组。</summary>
    private RenderTargets? targets;
    /// <summary>fullscreenVao为全屏三角VAO；boneBuffer为骨骼UBO；shadowBuffer/shadowTexture为阴影FBO及深度贴图。</summary>
    /// <remarks>skyTexture为天空立方贴图；particleVao/particleVbo为粒子布局和动态顶点缓冲，均在Dispose释放。</remarks>
    private int fullscreenVao, boneBuffer, shadowBuffer, shadowTexture, skyTexture, particleVao, particleVbo;
    /// <summary>drawCalls与triangles分别累计当前帧跨Pass绘制调用和提交三角形数。</summary>
    private int drawCalls, triangles;
    /// <summary>zeroSize表示窗口无可绘制尺寸；disposed表示资源释放已开始，后续调用被拒绝。</summary>
    private bool zeroSize, disposed;
    /// <summary>世界到方向光裁剪空间的CPU行向量矩阵，供阴影写入和采样共用。</summary>
    private Matrix4 lightViewProjection;
    /// <summary>方向光方形阴影贴图的边长像素数。</summary>
    public const int ShadowResolution = 2048;
    // 每灯RGB辐射最多1e12：反射基础色[0,1]、最多5灯与Exposure<=20的BRDF中间乘积仍在FP32内。
    // 这是明确拒绝极端输入的数值子集，不截断高光、光照输出或粗糙度。
    /// <summary>允许上传到shader的单灯每色道辐射上限；CPU先用double预乘检查。</summary>
    public const double MaximumShaderRadiance = 1e12;
    /// <summary>最近完成帧的提交统计，未绘制帧不会刷新。</summary>
    public RendererStatistics Statistics { get; private set; }

    /// <summary>同步创建程序、基础网格、阴影/天空/粒子与尺寸附件；失败清理已创建GPU资源。</summary>
    public TrainingRenderer(string assetRoot, int width, int height)
    {
        assets = new(assetRoot); loader = new(assets); textures = new(assets);
        animationSettings = AnimationSettings.Load(assets);
        try
        {
            forward = new(assets, "mesh.vert", "forward.frag"); gbuffer = new(assets, "mesh.vert", "gbuffer.frag");
            shadow = new(assets, "mesh.vert", "shadow.frag"); deferred = new(assets, "fullscreen.vert", "deferred.frag");
            sky = new(assets, "fullscreen.vert", "sky.frag"); tonemap = new(assets, "fullscreen.vert", "tonemap.frag");
            fxaa = new(assets, "fullscreen.vert", "fxaa.frag"); particle = new(assets, "particle.vert", "particle.frag");
            primitives.Add(PrimitiveKind.Cube, PrimitiveMeshes.Cube()); primitives.Add(PrimitiveKind.Plane, PrimitiveMeshes.Plane()); primitives.Add(PrimitiveKind.Sphere, PrimitiveMeshes.Sphere());
            fullscreenVao = GL.GenVertexArray();
            boneBuffer = GL.GenBuffer(); GL.BindBuffer(BufferTarget.UniformBuffer, boneBuffer);
            GL.BufferData(BufferTarget.UniformBuffer, GltfModel.MaximumJoints * 64, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, boneBuffer);
            CreateShadow(); skyTexture = CreateSky(); CreateParticles(); Resize(width, height);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); GL.BindVertexArray(0);
        }
        catch { Dispose(); throw; }
    }

    /// <summary>忽略零尺寸并暂停绘制；非零尺寸先创建完整新附件，再释放旧附件。</summary>
    public void Resize(int width, int height)
    {
        EnsureOwner();
        zeroSize = width <= 0 || height <= 0;
        if (zeroSize || targets is not null && targets.Width == width && targets.Height == height) return;
        // 新附件完整成功后才替换；失败只释放临时附件并保留旧目标。
        var replacement = new RenderTargets(width, height);
        var previous = targets; targets = replacement; previous?.Dispose();
    }

    /// <summary>按模拟步长秒数推进模型实例，移除消失GUID并释放离场共享模型网格。</summary>
    public void UpdateAnimations(float dt, IReadOnlyList<RenderObject> objects)
    {
        EnsureOwner();
        var active = new HashSet<Guid>();
        foreach (var item in objects)
        {
            if (item.Design.Primitive != PrimitiveKind.Model) continue;
            active.Add(item.Id);
            var instance = Instance(item);
            instance.Controller.Update(dt, item.Animation);
        }
        foreach (Guid id in instances.Keys.Where(id => !active.Contains(id)).ToArray()) instances.Remove(id);
        var activeModels = objects.Where(o => o.Design.Primitive == PrimitiveKind.Model).Select(o => o.Design.ModelPath ?? "models/UAL1_Standard.glb").ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string path in models.Keys.Where(p => !activeModels.Contains(p)).ToArray())
        { foreach (var mesh in models[path].Meshes) mesh.Dispose(); models.Remove(path); }
    }

    /// <summary>提交场景前验证材质与角色合同并预载GPU资源；失败只回滚本次新增模型/纹理。</summary>
    public void Prepare(IReadOnlyList<RenderObject> objects)
    {
        EnsureOwner();
        var previousModels = models.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var previousTextures = textures.Snapshot();
        try
        {
            foreach (var item in objects)
            {
                if (item.Design.Kind == ObjectKind.PointLight)
                    _ = CheckedRadiance(item.Design.Material.BaseColor.ToVector(), item.Design.Parameters.GetValueOrDefault("intensity", 25), $"点光{item.Design.Name}");
                MaterialData drawingMaterial = DrawingMaterial(item.Design);
                if (item.Design.Primitive == PrimitiveKind.Model)
                {
                    string path = item.Design.ModelPath ?? "models/UAL1_Standard.glb";
                    if (!models.TryGetValue(path, out var model)) { model = LoadModel(path); models.Add(path, model); }
                    if (item.Design.Kind is ObjectKind.Player or ObjectKind.Npc) animationSettings.ValidateCharacterModel(model.Data);
                    for (int i = 0; i < model.Data.Primitives.Length; i++)
                    {
                        ValidateModelMaterial(model.Data, model.Data.Primitives[i], drawingMaterial);
                        LoadTextures(model.Data.Primitives[i].Material, path + "/" + i, drawingMaterial);
                    }
                }
                else
                {
                    _ = CheckedReflectance(drawingMaterial.BaseColor.ToVector(), $"对象{item.Design.Name}反射基础色");
                    LoadTextures(new(Vector4.One, 0, .6f, false, null, null, null, 1, -1), "", drawingMaterial);
                }
            }
        }
        catch
        {
            foreach (string path in models.Keys.Where(p => !previousModels.Contains(p)).ToArray())
            { foreach (var mesh in models[path].Meshes) mesh.Dispose(); models.Remove(path); }
            textures.Retain(previousTextures);
            throw;
        }
    }

    /// <summary>清动画实例和场景模型/纹理，保留通用程序、基础网格与渲染目标。</summary>
    public void ResetScene()
    {
        EnsureOwner(); instances.Clear();
        foreach (var model in models.Values) foreach (var mesh in model.Meshes) mesh.Dispose();
        models.Clear(); textures.Retain(new HashSet<string>()); usedTextures.Clear();
    }

    /// <summary>成功提交Play/Stop/Load后重建动画状态，保留已经准备的共享GPU资产。</summary>
    public void ResetAnimations()
    {
        // 成功Play/Stop/Load提交后重建实例状态，保留刚Prepare验证的GPU资源。
        EnsureOwner(); instances.Clear();
    }

    /// <summary>将动画显示插值历史收束到当前逻辑姿态，不重置游标或未消费事件。</summary>
    public void ResetDisplayHistory()
    {
        EnsureOwner();
        foreach (var instance in instances.Values) instance.Controller.ResetInterpolation();
    }

    /// <summary>读取指定实例的动画调试文字，无模型实例时返回提示。</summary>
    public string AnimationDebug(Guid id) => instances.TryGetValue(id, out var entry) ? entry.Controller.DebugText : "No animated model";
    /// <summary>一次取走指定实例累计动画事件，避免绘制频率导致重复消费。</summary>
    public AnimationEvent[] DrainAnimationEvents(Guid id) => instances.TryGetValue(id, out var entry) ? entry.Controller.DrainEvents() : [];

    /// <summary>验证入口：读取左下原点像素的线性HDR RGBA，不经过色调映射。</summary>
    internal Vector4 ReadHdrPixel(int x, int y) => ReadTargetPixel(x, y, false);
    /// <summary>验证入口：读取G-buffer世界法线与存储后的粗糙度。</summary>
    internal Vector4 ReadNormalRoughPixel(int x, int y) => ReadTargetPixel(x, y, true);
    /// <summary>检查像素范围、读回指定浮点附件，并恢复先前读FBO绑定。</summary>
    private Vector4 ReadTargetPixel(int x, int y, bool normalRough)
    {
        EnsureOwner();
        if (targets is null || (uint)x >= targets.Width || (uint)y >= targets.Height) throw new ArgumentOutOfRangeException(nameof(x));
        GL.GetInteger(GetPName.ReadFramebufferBinding, out int previous);
        try
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, normalRough ? targets.GBuffer : targets.HdrBuffer);
            GL.ReadBuffer(normalRough ? ReadBufferMode.ColorAttachment1 : ReadBufferMode.ColorAttachment0);
            var pixel = new float[4]; GL.ReadPixels(x, y, 1, 1, PixelFormat.Rgba, PixelType.Float, pixel);
            return new(pixel[0], pixel[1], pixel[2], pixel[3]);
        }
        finally { GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, previous); }
    }

    /// <summary>生成插值后骨架父子连线的世界端点，与网格共用角色修正和实例变换。</summary>
    public IReadOnlyList<(Vector3 Start, Vector3 End)> SkeletonDebug(Guid id, Matrix4 instanceWorld, float alpha = 1)
    {
        if (!instances.TryGetValue(id, out var entry)) return [];
        var lines = new List<(Vector3, Vector3)>();
        Matrix4[] displayWorld = entry.Controller.GetDisplayWorld(alpha);
        Matrix4 correction = (entry.Character ? entry.Model.ActorCorrection : Matrix4.Identity) * instanceWorld;
        var joints = entry.Model.Data.Skins.SelectMany(s => s.Joints).ToHashSet();
        foreach (int joint in joints)
        {
            int parent = entry.Model.Data.Nodes[joint].Parent;
            if (parent >= 0 && joints.Contains(parent))
                lines.Add((Vector3.TransformPosition(Vector3.Zero, displayWorld[parent] * correction), Vector3.TransformPosition(Vector3.Zero, displayWorld[joint] * correction)));
        }
        return lines;
    }

    /// <summary>绘制阴影、主路径、透明粒子和后处理；读取显示姿态，不推进动画或粒子模拟。</summary>
    public void Render(CameraState camera, IReadOnlyList<RenderObject> objects, RenderSettings settings, IReadOnlyList<ParticleVisual> particles, RenderDebugView debugView)
    {
        EnsureOwner(); if (zeroSize || targets is null) return;
        _ = CheckedRadiance(settings.SunColor.ToVector(), settings.SunIntensity, "方向光");
        if (!float.IsFinite(settings.Exposure) || settings.Exposure <= 0) throw new InvalidDataException("渲染曝光必须为有限正数");
        drawCalls = triangles = 0;
        usedTextures.Clear();
        // CPU采用行向量：先View后Projection；其逆用于Deferred从深度重建世界位置。
        Matrix4 viewProjection = camera.View * camera.Projection;
        Matrix4 inverseViewProjection = viewProjection.Inverted();
        Vector3 sunDirection = settings.SunDirection.ToVector();
        // DTO的有限float分量仍可能在float LengthSquared中溢出；用double求方向长度。
        if (!float.IsFinite(sunDirection.X) || !float.IsFinite(sunDirection.Y) || !float.IsFinite(sunDirection.Z)) throw new InvalidDataException("方向光方向必须有限");
        double sunLengthSquared = (double)sunDirection.X * sunDirection.X + (double)sunDirection.Y * sunDirection.Y + (double)sunDirection.Z * sunDirection.Z;
        if (sunLengthSquared < 1e-6) { sunDirection = new(-.5f, -1, -.4f); sunLengthSquared = .25 + 1 + .16; }
        double sunLength = Math.Sqrt(sunLengthSquared);
        sunDirection = new((float)(sunDirection.X / sunLength), (float)(sunDirection.Y / sunLength), (float)(sunDirection.Z / sunLength));
        Vector3 center = new(camera.Target.X, 0, camera.Target.Z);
        Vector3 up = MathF.Abs(Vector3.Dot(sunDirection, Vector3.UnitY)) > .98f ? Vector3.UnitZ : Vector3.UnitY;
        lightViewProjection = Matrix4.LookAt(center - sunDirection * 65, center, up) * Matrix4.CreateOrthographic(80, 80, 1, 140);
        GL.Disable(EnableCap.FramebufferSrgb); GL.Disable(EnableCap.Blend); GL.Disable(EnableCap.ScissorTest); GL.Disable(EnableCap.StencilTest); GL.DepthMask(true); GL.ColorMask(true, true, true, true); GL.ClearDepth(1);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, boneBuffer);
        GL.Enable(EnableCap.DepthTest); GL.DepthFunc(DepthFunction.Less); GL.Enable(EnableCap.CullFace); GL.CullFace(TriangleFace.Back);
        if (settings.Shadows || debugView == RenderDebugView.Shadow)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadowBuffer); GL.Viewport(0, 0, ShadowResolution, ShadowResolution);
            GL.Clear(ClearBufferMask.DepthBufferBit); GL.Enable(EnableCap.PolygonOffsetFill);
            // 3×3 PCF在双轴取±1 texel，Nearest再含最多半texel残差，
            // 合计深度差可达3*maxSlope；只偏移1.5会在斜面出现重复三角自阴影。
            GL.PolygonOffset(3, 2);
            DrawObjects(shadow!, lightViewProjection, objects, true); GL.Disable(EnableCap.PolygonOffsetFill);
        }
        // 即使主路径为Forward，材质/法线/深度调试仍需要先写G-buffer。
        bool needGbuffer = settings.Pipeline == RenderPipelineMode.Deferred || debugView is RenderDebugView.BaseColor or RenderDebugView.Normals or RenderDebugView.Roughness or RenderDebugView.Depth;
        if (needGbuffer)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, targets.GBuffer); GL.Viewport(0, 0, targets.Width, targets.Height);
            GL.ClearColor(0, 0, 0, 0); GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            DrawObjects(gbuffer!, viewProjection, objects, false);
        }
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, targets.HdrBuffer); GL.Viewport(0, 0, targets.Width, targets.Height);
        GL.ClearColor(0, 0, 0, 1); GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        if (settings.Pipeline == RenderPipelineMode.Deferred)
        {
            deferred!.Use(); SetLighting(deferred, camera, objects, settings, sunDirection);
            deferred.Set("uInverseViewProjection", inverseViewProjection);
            BindTexture(deferred, "uBaseMetal", targets.BaseMetal, 0); BindTexture(deferred, "uNormalRough", targets.NormalRough, 1);
            BindTexture(deferred, "uDepth", targets.Depth, 2); BindSky(deferred, 6); Fullscreen();
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, targets.GBuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, targets.HdrBuffer);
            // 将几何深度复制到HDR独立深度附件，让后续粒子正确受不透明表面遮挡。
            GL.BlitFramebuffer(0, 0, targets.Width, targets.Height, 0, 0, targets.Width, targets.Height, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, targets.HdrBuffer);
        }
        else
        {
            sky!.Use(); sky.Set("uCamera", camera.Position); sky.Set("uInverseViewProjection", inverseViewProjection); BindSky(sky, 6); Fullscreen();
            GL.Enable(EnableCap.DepthTest); GL.DepthMask(true); GL.Enable(EnableCap.CullFace);
            forward!.Use(); SetLighting(forward, camera, objects, settings, sunDirection); DrawObjects(forward, viewProjection, objects, false);
        }
        // 粒子在线性HDR空间透明合成，随后统一曝光、Gamma和FXAA。
        DrawParticles(camera, viewProjection, particles);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, targets.LdrBuffer); tonemap!.Use();
        tonemap.Set("uDebug", (int)debugView); tonemap.Set("uExposure", Math.Clamp(settings.Exposure, .01f, 20));
        BindTexture(tonemap, "uHdr", targets.HdrColor, 0); BindTexture(tonemap, "uBaseMetal", targets.BaseMetal, 1);
        BindTexture(tonemap, "uNormalRough", targets.NormalRough, 2); BindTexture(tonemap, "uDepth", targets.Depth, 3); BindTexture(tonemap, "uShadow", shadowTexture, 4);
        Fullscreen();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); fxaa!.Use();
        fxaa.Set("uFxaa", settings.Fxaa && debugView == RenderDebugView.Final ? 1 : 0); fxaa.Set("uInvResolution", new Vector2(1f / targets.Width, 1f / targets.Height));
        BindTexture(fxaa, "uLdr", targets.LdrColor, 0); Fullscreen();
        GL.DepthMask(true); GL.BindVertexArray(0); GL.UseProgram(0); GL.ActiveTexture(TextureUnit.Texture0);
        Statistics = new(drawCalls, triangles, instances.Values.Count(i => i.Character), targets.Width, targets.Height);
        textures.Retain(usedTextures);
    }

    /// <summary>按可见对象绘制共享基础网格或模型子网格，复用同一材质/蒙皮路径。</summary>
    private void DrawObjects(ShaderProgram program, Matrix4 viewProjection, IReadOnlyList<RenderObject> objects, bool shadowPass)
    {
        program.Use(); program.Set("uViewProjection", viewProjection); GL.Enable(EnableCap.DepthTest); GL.DepthMask(true);
        foreach (var item in objects)
        {
            if (!item.Design.Visible || item.Design.Kind == ObjectKind.Group) continue;
            MaterialData drawingMaterial = DrawingMaterial(item.Design);
            if (item.Design.Primitive == PrimitiveKind.Model)
            {
                var entry = Instance(item);
                bool actor = item.Design.Kind is ObjectKind.Player or ObjectKind.Npc;
                Matrix4[] displayWorld = entry.Controller.GetDisplayWorld(item.InterpolationAlpha);
                Matrix4 correction = actor ? entry.Model.ActorCorrection : Matrix4.Identity;
                for (int i = 0; i < entry.Model.Data.Primitives.Length; i++)
                {
                    var primitive = entry.Model.Data.Primitives[i];
                    ValidateModelMaterial(entry.Model.Data, primitive, drawingMaterial);
                    // 行向量依次经过模型节点、角色外观修正、场景实例；蒙皮palette先回到该mesh局部空间。
                    Matrix4 meshWorld = displayWorld[primitive.Node] * correction * item.ModelMatrix;
                    program.Set("uModel", meshWorld);
                    program.Set("uSkinned", primitive.Skin >= 0 ? 1 : 0);
                    if (primitive.Skin >= 0)
                    {
                        Matrix4[] palette = entry.Model.Data.CreatePalette(primitive.Skin, primitive.Node, displayWorld);
                        GL.BindBuffer(BufferTarget.UniformBuffer, boneBuffer); GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, palette.Length * 64, palette);
                    }
                    BindMaterial(program, primitive.Material, entry.Model.Path + "/" + i, drawingMaterial, true);
                    SetCulling(primitive.Material.DoubleSided, meshWorld);
                    Draw(entry.Model.Meshes[i]);
                }
            }
            else if (primitives.TryGetValue(item.Design.Primitive, out var mesh))
            {
                program.Set("uModel", item.ModelMatrix); program.Set("uSkinned", 0);
                var material = drawingMaterial;
                BindMaterial(program, new(new(material.BaseColor.ToVector(), 1), material.Metallic, material.Roughness, false, null, null, null, 1, -1), "", material, false);
                SetCulling(item.Design.Primitive == PrimitiveKind.Plane, item.ModelMatrix); Draw(mesh);
            }
        }
        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
    }

    /// <summary>组合导入材质与实例tint，检查最终反射色并绑定颜色/法线/金属粗糙度纹理。</summary>
    private void BindMaterial(ShaderProgram program, ModelMaterial imported, string key, MaterialData design, bool useImported)
    {
        // 模型实例颜色作tint，保留同一GLB内不同子材质的颜色差异。
        Vector3 tint = design.BaseColor.ToVector();
        Vector4 baseColor = useImported ? new(
            CheckedProduct(imported.BaseColor.X, tint.X, "模型有效基础色R"),
            CheckedProduct(imported.BaseColor.Y, tint.Y, "模型有效基础色G"),
            CheckedProduct(imported.BaseColor.Z, tint.Z, "模型有效基础色B"), imported.BaseColor.W) : imported.BaseColor;
        _ = CheckedReflectance(baseColor.Xyz, "有效反射基础色");
        program.Set("uBaseColor", baseColor); program.Set("uMetallic", imported.Metallic); program.Set("uRoughness", imported.Roughness);
        program.Set("uNormalScale", imported.NormalScale); program.Set("uAlphaCutoff", imported.AlphaCutoff);
        var (baseTexture, normalTexture, mrTexture) = LoadTextures(imported, key, design);
        program.Set("uHasBaseTexture", baseTexture != 0 ? 1 : 0); program.Set("uHasNormalTexture", normalTexture != 0 ? 1 : 0); program.Set("uHasMrTexture", mrTexture != 0 ? 1 : 0);
        BindTexture(program, "uBaseTexture", baseTexture, 0); BindTexture(program, "uNormalTexture", normalTexture, 1); BindTexture(program, "uMrTexture", mrTexture, 2);
    }

    /// <summary>优先采用场景覆盖贴图，否则借用导入贴图；记录引用键并区分sRGB颜色和线性数据。</summary>
    private (int Base, int Normal, int Mr) LoadTextures(ModelMaterial imported, string key, MaterialData design)
    {
        int baseTexture = 0, normalTexture = 0, mrTexture = 0;
        if (design.BaseColorTexture is not null) { usedTextures.Add(design.BaseColorTexture + ":s"); baseTexture = textures.GetFile(design.BaseColorTexture, true); }
        else if (imported.BaseColorImage is not null) { usedTextures.Add(key + ":base"); baseTexture = textures.Get(key + ":base", imported.BaseColorImage, true); }
        if (design.NormalTexture is not null) { usedTextures.Add(design.NormalTexture + ":l"); normalTexture = textures.GetFile(design.NormalTexture, false); }
        else if (imported.NormalImage is not null) { usedTextures.Add(key + ":normal"); normalTexture = textures.Get(key + ":normal", imported.NormalImage, false); }
        if (imported.MetallicRoughnessImage is not null) { usedTextures.Add(key + ":mr"); mrTexture = textures.Get(key + ":mr", imported.MetallicRoughnessImage, false); }
        return (baseTexture, normalTexture, mrTexture);
    }

    /// <summary>检查模型最终有效反射色，以及任何有效贴图所必需的源TEXCOORD_0。</summary>
    private static void ValidateModelMaterial(GltfModel model, ModelPrimitive primitive, MaterialData design)
    {
        ModelMaterial imported = primitive.Material;
        _ = CheckedProduct(imported.BaseColor.X, design.BaseColor.X, "模型有效基础色R");
        _ = CheckedProduct(imported.BaseColor.Y, design.BaseColor.Y, "模型有效基础色G");
        _ = CheckedProduct(imported.BaseColor.Z, design.BaseColor.Z, "模型有效基础色B");
        if (!primitive.HasUv0 && (design.BaseColorTexture is not null || imported.BaseColorImage is not null
            || design.NormalTexture is not null || imported.NormalImage is not null || imported.MetallicRoughnessImage is not null))
            throw new InvalidDataException($"模型{model.SourcePath}节点{model.Nodes[primitive.Node].Name}[{primitive.Node}] primitive{primitive.PrimitiveIndex}最终贴图材质需要TEXCOORD_0；源网格没有UV0");
    }

    /// <summary>为HDR点光marker生成有限反射材质，保留原灯色和设计历史供照明使用。</summary>
    private static MaterialData DrawingMaterial(SceneObjectData design)
    {
        MaterialData material = design.Material;
        if (design.Kind != ObjectKind.PointLight) return material;
        Vector3 color = material.BaseColor.ToVector();
        float largest = Math.Max(1, Math.Max(color.X, Math.Max(color.Y, color.Z)));
        if (largest == 1) return material;
        // 灯的可见marker使用有限反射色；原设计RGB仍供SetLighting求真正radiance，不能回写设计/历史。
        return new() { BaseColor = Float3.From(color / largest), Metallic = material.Metallic, Roughness = material.Roughness,
            BaseColorTexture = material.BaseColorTexture, NormalTexture = material.NormalTexture };
    }

    /// <summary>以double检查有限非负乘积及用途上限，再转换float，拒绝值不作静默钳制。</summary>
    private static float CheckedProduct(float left, float right, string label, double maximum = 1)
    {
        double product = (double)left * right;
        if (!float.IsFinite(left) || !float.IsFinite(right) || left < 0 || right < 0 || product > maximum)
        {
            string range = maximum == 1 ? "反射基础色必须在[0,1]（RGBA8 G-buffer合同）" : $"GPU FP32 radiance必须有限、非负且不超过{maximum:G}";
            throw new InvalidDataException($"{label}超出V1数值范围：{range}；实际乘积{product:G}");
        }
        return (float)product;
    }
    /// <summary>逐通道检查RGBA8 G-buffer支持的[0,1]有效反射色。</summary>
    private static Vector3 CheckedReflectance(Vector3 color, string label) => new(
        CheckedProduct(color.X, 1, label + " R"), CheckedProduct(color.Y, 1, label + " G"), CheckedProduct(color.Z, 1, label + " B"));
    /// <summary>逐通道double预乘光色与强度，检查FP32支持子集后返回shader辐射。</summary>
    private static Vector3 CheckedRadiance(Vector3 color, float intensity, string label) => new(
        CheckedProduct(color.X, intensity, label + " radiance R", MaximumShaderRadiance), CheckedProduct(color.Y, intensity, label + " radiance G", MaximumShaderRadiance), CheckedProduct(color.Z, intensity, label + " radiance B", MaximumShaderRadiance));

    /// <summary>上传世界空间相机、方向光、阴影和最多四盏可见点光；点光位置取场景世界矩阵。</summary>
    private void SetLighting(ShaderProgram program, CameraState camera, IReadOnlyList<RenderObject> objects, RenderSettings settings, Vector3 direction)
    {
        program.Set("uCamera", camera.Position); program.Set("uSunDirection", direction); program.Set("uSunRadiance", CheckedRadiance(settings.SunColor.ToVector(), settings.SunIntensity, "方向光"));
        program.Set("uLightViewProjection", lightViewProjection); program.Set("uShadows", settings.Shadows ? 1 : 0); BindTexture(program, "uShadow", shadowTexture, 5);
        var lights = objects.Where(o => o.Design.Kind == ObjectKind.PointLight && o.Design.Visible).Take(4).ToArray();
        program.Set("uPointCount", lights.Length);
        for (int i = 0; i < lights.Length; i++)
        {
            var light = lights[i]; Vector3 position = light.ModelMatrix.ExtractTranslation();
            float radius = light.Design.Parameters.GetValueOrDefault("radius", 10), intensity = light.Design.Parameters.GetValueOrDefault("intensity", 25);
            // 先在double中相乘/检查，再一次转换到float；避免shader中先乘巨大色分量再乘微小强度而溢出。
            Vector3 radiance = CheckedRadiance(light.Design.Material.BaseColor.ToVector(), intensity, $"点光{light.Design.Name}");
            program.Set($"uPointPositions[{i}]", new Vector4(position, radius)); program.Set($"uPointColors[{i}]", new Vector4(radiance, 1));
        }
    }

    /// <summary>按GUID取得或重建独立动画实例，角色校验通过后借用路径共享模型。</summary>
    private (GpuModel Model, AnimationController Controller, bool Character) Instance(RenderObject item)
    {
        string path = item.Design.ModelPath ?? "models/UAL1_Standard.glb";
        if (!models.TryGetValue(path, out var model)) { model = LoadModel(path); models.Add(path, model); }
        bool character = item.Design.Kind is ObjectKind.Player or ObjectKind.Npc;
        if (character) animationSettings.ValidateCharacterModel(model.Data);
        if (!instances.TryGetValue(item.Id, out var entry) || entry.Model != model || entry.Character != character)
            instances[item.Id] = entry = (model, new(model.Data, animationSettings, character), character);
        return entry;
    }
    /// <summary>解析模型并上传全部子网格，计算脚底/身高/前向修正；失败释放已上传网格。</summary>
    private GpuModel LoadModel(string path)
    {
        var data = loader.Load(path); var meshes = new List<GpuMesh>();
        try
        {
            foreach (var primitive in data.Primitives) meshes.Add(new(primitive.Vertices, primitive.Indices));
            // 角色Position是脚底；导入边界统一适配+Z前向与1.9m高度，骨架根-90X仍完整保留。
            var bounds = data.ComputeBounds(); float minY = bounds.Min.Y, maxY = bounds.Max.Y;
            Matrix4 actorCorrection = maxY - minY > .001f ? Matrix4.CreateTranslation(0, -minY, 0) * Matrix4.CreateScale(1.9f / (maxY - minY)) * Matrix4.CreateRotationY(MathF.PI) : Matrix4.Identity;
            return new(path, data, meshes.ToArray(), actorCorrection);
        }
        catch { foreach (var mesh in meshes) mesh.Dispose(); throw; }
    }

    /// <summary>提交一个网格并累计本Pass的调用数和三角形数。</summary>
    private void Draw(GpuMesh mesh) { mesh.Draw(); drawCalls++; triangles += mesh.IndexCount / 3; }
    /// <summary>镜像世界矩阵反转正面绕序，双面材质关闭背面剔除。</summary>
    private static void SetCulling(bool doubleSided, Matrix4 world)
    {
        GL.FrontFace(world.Determinant < 0 ? FrontFaceDirection.Cw : FrontFaceDirection.Ccw);
        if (doubleSided) GL.Disable(EnableCap.CullFace); else GL.Enable(EnableCap.CullFace);
    }
    /// <summary>关闭深度写入、剔除和混合后绘制无顶点缓冲全屏三角形，并累计统计。</summary>
    private void Fullscreen()
    {
        GL.Disable(EnableCap.DepthTest); GL.DepthMask(false); GL.Disable(EnableCap.CullFace); GL.Disable(EnableCap.Blend);
        GL.BindVertexArray(fullscreenVao); GL.DrawArrays(PrimitiveType.Triangles, 0, 3); drawCalls++; triangles++;
    }
    /// <summary>把2D贴图绑定到指定纹理单元，并同步对应采样器uniform。</summary>
    private static void BindTexture(ShaderProgram program, string uniform, int texture, int unit)
    { GL.ActiveTexture(TextureUnit.Texture0 + unit); GL.BindTexture(TextureTarget.Texture2D, texture); program.Set(uniform, unit); }
    /// <summary>将本渲染器的天空立方贴图绑定到指定单元供背景采样。</summary>
    private void BindSky(ShaderProgram program, int unit)
    { GL.ActiveTexture(TextureUnit.Texture0 + unit); GL.BindTexture(TextureTarget.TextureCubeMap, skyTexture); program.Set("uSky", unit); }

    /// <summary>创建只含D24深度附件的方向光阴影FBO，所有句柄由渲染器最终释放。</summary>
    private void CreateShadow()
    {
        shadowBuffer = GL.GenFramebuffer(); GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadowBuffer);
        shadowTexture = RenderTargets.Texture(ShadowResolution, ShadowResolution, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent, PixelType.UnsignedInt);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowTexture, 0);
        GL.DrawBuffer(DrawBufferMode.None); GL.ReadBuffer(ReadBufferMode.None); RenderTargets.Check("Shadow");
    }
    /// <summary>生成六面渐变背景立方贴图；成功转交句柄，失败删除临时纹理。</summary>
    private static int CreateSky()
    {
        int texture = GL.GenTexture();
        try
        {
            GL.BindTexture(TextureTarget.TextureCubeMap, texture);
            // 自有六面天空渐变，只作为背景，不从它生成环境光照。
            const int size = 32;
            for (int face = 0; face < 6; face++)
            {
                var pixels = new byte[size * size * 3];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float elevation = face == 2 ? 1 : face == 3 ? 0 : 1 - (float)y / (size - 1);
                    Vector3 c = Vector3.Lerp(new(.32f, .42f, .56f), new(.07f, .2f, .4f), elevation);
                    int offset = (y * size + x) * 3; pixels[offset] = (byte)(c.X * 255); pixels[offset + 1] = (byte)(c.Y * 255); pixels[offset + 2] = (byte)(c.Z * 255);
                }
                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + face, 0, PixelInternalFormat.Rgb8, size, size, 0, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
            }
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge); GL.Enable(EnableCap.TextureCubeMapSeamless);
            return texture;
        }
        catch { GL.DeleteTexture(texture); throw; }
    }
    /// <summary>创建每顶点9个float的位置/颜色/UV布局及动态粒子缓冲。</summary>
    private void CreateParticles()
    {
        particleVao = GL.GenVertexArray(); particleVbo = GL.GenBuffer(); GL.BindVertexArray(particleVao); GL.BindBuffer(BufferTarget.ArrayBuffer, particleVbo);
        int[] sizes = [3, 4, 2], offsets = [0, 3, 7];
        for (int i = 0; i < 3; i++) { GL.EnableVertexAttribArray(i); GL.VertexAttribPointer(i, sizes[i], VertexAttribPointerType.Float, false, 9 * sizeof(float), offsets[i] * sizeof(float)); }
    }
    /// <summary>按相机深度从远到近构造Billboard，在HDR中深度测试但不写深度地透明合成。</summary>
    private void DrawParticles(CameraState camera, Matrix4 viewProjection, IReadOnlyList<ParticleVisual> visuals)
    {
        if (visuals.Count == 0) return;
        // 相机平面Billboard按view-space Z排序；径向距离会被侧向偏移误导。右手view下更负的Z更远。
        var sorted = visuals.OrderBy(p => Vector3.TransformPosition(p.Position, camera.View).Z).Take(8192).ToArray();
        Matrix4 inverseView = camera.View.Inverted(); Vector3 right = inverseView.Row0.Xyz, up = inverseView.Row1.Xyz;
        var vertices = new float[sorted.Length * 6 * 9]; int cursor = 0;
        // 用相机世界右/上轴展开一个角点，并写入位置、颜色与局部UV。
        void Vertex(ParticleVisual visual, float x, float y)
        {
            Vector3 position = visual.Position + (right * (x - .5f) + up * (y - .5f)) * visual.Size;
            vertices[cursor++] = position.X; vertices[cursor++] = position.Y; vertices[cursor++] = position.Z;
            vertices[cursor++] = visual.Color.X; vertices[cursor++] = visual.Color.Y; vertices[cursor++] = visual.Color.Z; vertices[cursor++] = visual.Color.W;
            vertices[cursor++] = x; vertices[cursor++] = y;
        }
        foreach (var visual in sorted) { Vertex(visual, 0, 0); Vertex(visual, 1, 0); Vertex(visual, 1, 1); Vertex(visual, 0, 0); Vertex(visual, 1, 1); Vertex(visual, 0, 1); }
        GL.BindVertexArray(particleVao); GL.BindBuffer(BufferTarget.ArrayBuffer, particleVbo); GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);
        particle!.Use(); particle.Set("uViewProjection", viewProjection); GL.Enable(EnableCap.DepthTest); GL.DepthFunc(DepthFunction.Lequal); GL.DepthMask(false);
        GL.Disable(EnableCap.CullFace); GL.Enable(EnableCap.Blend); GL.BlendEquation(BlendEquationMode.FuncAdd); GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DrawArrays(PrimitiveType.Triangles, 0, sorted.Length * 6); drawCalls++; triangles += sorted.Length * 2;
        GL.Disable(EnableCap.Blend); GL.DepthMask(true); GL.DepthFunc(DepthFunction.Less);
    }
    /// <summary>拒绝已释放对象和跨线程GL调用；实际当前上下文由窗口生命周期保证。</summary>
    private void EnsureOwner()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("GL调用必须位于创建渲染器的当前上下文线程");
    }
    /// <summary>在创建线程释放目标、共享资产、程序和专用句柄；同时覆盖部分初始化失败。</summary>
    public void Dispose()
    {
        if (disposed) return;
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("GPU资源必须在其当前上下文线程释放");
        disposed = true;
        targets?.Dispose(); targets = null; textures.Dispose(); foreach (var mesh in primitives.Values) mesh.Dispose(); primitives.Clear();
        foreach (var model in models.Values) foreach (var mesh in model.Meshes) mesh.Dispose(); models.Clear(); instances.Clear();
        foreach (var program in new[] { forward, gbuffer, shadow, deferred, sky, tonemap, fxaa, particle }) program?.Dispose();
        foreach (int buffer in new[] { boneBuffer, particleVbo }) if (buffer != 0) GL.DeleteBuffer(buffer);
        foreach (int vao in new[] { fullscreenVao, particleVao }) if (vao != 0) GL.DeleteVertexArray(vao);
        if (shadowBuffer != 0) GL.DeleteFramebuffer(shadowBuffer); if (shadowTexture != 0) GL.DeleteTexture(shadowTexture); if (skyTexture != 0) GL.DeleteTexture(skyTexture);
    }
    /// <summary>路径共享模型的CPU/GPU组合；Meshes由外层渲染器释放。</summary>
    /// <param name="Path">缓存使用的模型资产相对路径。</param>
    /// <param name="Data">导入节点、材质、蒙皮和动画CPU数据。</param>
    /// <param name="Meshes">与primitive一一对应的GPU网格。</param>
    /// <param name="ActorCorrection">角色脚底、1.9米高度与素材前向的外观适配矩阵。</param>
    private sealed record GpuModel(string Path, GltfModel Data, GpuMesh[] Meshes, Matrix4 ActorCorrection);
}
