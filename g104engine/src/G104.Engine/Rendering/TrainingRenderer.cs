using G104.Engine.Animation;
using G104.Engine.Assets;
using G104.Engine.Scene;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace G104.Engine.Rendering;

public readonly record struct RendererStatistics(int DrawCalls, int Triangles, int AnimatedInstances, int Width, int Height);

// 固定Pass顺序用于学习和对照；GL资源始终由创建它们的当前上下文线程释放。
public sealed class TrainingRenderer : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly AssetRoot assets;
    private readonly GltfModelLoader loader;
    private readonly TextureCache textures;
    private readonly AnimationSettings animationSettings;
    private readonly Dictionary<PrimitiveKind, GpuMesh> primitives = [];
    private readonly Dictionary<string, GpuModel> models = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, (GpuModel Model, AnimationController Controller, bool Character)> instances = [];
    private readonly HashSet<string> usedTextures = new(StringComparer.Ordinal);
    private ShaderProgram? forward, gbuffer, shadow, deferred, sky, tonemap, fxaa, particle;
    private RenderTargets? targets;
    private int fullscreenVao, boneBuffer, shadowBuffer, shadowTexture, skyTexture, particleVao, particleVbo;
    private int drawCalls, triangles;
    private bool zeroSize, disposed;
    private Matrix4 lightViewProjection;
    public const int ShadowResolution = 2048;
    public RendererStatistics Statistics { get; private set; }

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

    public void Resize(int width, int height)
    {
        EnsureOwner();
        zeroSize = width <= 0 || height <= 0;
        if (zeroSize || targets is not null && targets.Width == width && targets.Height == height) return;
        // 新附件完整成功后才替换；失败只释放临时附件并保留旧目标。
        var replacement = new RenderTargets(width, height);
        var previous = targets; targets = replacement; previous?.Dispose();
    }

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

    public void Prepare(IReadOnlyList<RenderObject> objects)
    {
        EnsureOwner();
        var previousModels = models.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var previousTextures = textures.Snapshot();
        try
        {
            foreach (var item in objects)
            {
                if (item.Design.Primitive == PrimitiveKind.Model)
                {
                    string path = item.Design.ModelPath ?? "models/UAL1_Standard.glb";
                    if (!models.TryGetValue(path, out var model)) { model = LoadModel(path); models.Add(path, model); }
                    if (item.Design.Kind is ObjectKind.Player or ObjectKind.Npc) animationSettings.ValidateCharacterModel(model.Data);
                    for (int i = 0; i < model.Data.Primitives.Length; i++)
                        LoadTextures(model.Data.Primitives[i].Material, path + "/" + i, item.Design.Material);
                }
                else LoadTextures(new(Vector4.One, 0, .6f, false, null, null, null, 1, -1), "", item.Design.Material);
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

    public void ResetScene()
    {
        EnsureOwner(); instances.Clear();
        foreach (var model in models.Values) foreach (var mesh in model.Meshes) mesh.Dispose();
        models.Clear(); textures.Retain(new HashSet<string>()); usedTextures.Clear();
    }

    public void ResetAnimations()
    {
        // 成功Play/Stop/Load提交后重建实例状态，保留刚Prepare验证的GPU资源。
        EnsureOwner(); instances.Clear();
    }

    public void ResetDisplayHistory()
    {
        EnsureOwner();
        foreach (var instance in instances.Values) instance.Controller.ResetInterpolation();
    }

    public string AnimationDebug(Guid id) => instances.TryGetValue(id, out var entry) ? entry.Controller.DebugText : "No animated model";
    public AnimationEvent[] DrainAnimationEvents(Guid id) => instances.TryGetValue(id, out var entry) ? entry.Controller.DrainEvents() : [];

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

    public void Render(CameraState camera, IReadOnlyList<RenderObject> objects, RenderSettings settings, IReadOnlyList<ParticleVisual> particles, RenderDebugView debugView)
    {
        EnsureOwner(); if (zeroSize || targets is null) return;
        drawCalls = triangles = 0;
        usedTextures.Clear();
        Matrix4 viewProjection = camera.View * camera.Projection;
        Matrix4 inverseViewProjection = viewProjection.Inverted();
        Vector3 sunDirection = settings.SunDirection.ToVector();
        if (sunDirection.LengthSquared < 1e-6f) sunDirection = new(-.5f, -1, -.4f);
        sunDirection.Normalize();
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
            GL.BlitFramebuffer(0, 0, targets.Width, targets.Height, 0, 0, targets.Width, targets.Height, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, targets.HdrBuffer);
        }
        else
        {
            sky!.Use(); sky.Set("uCamera", camera.Position); sky.Set("uInverseViewProjection", inverseViewProjection); BindSky(sky, 6); Fullscreen();
            GL.Enable(EnableCap.DepthTest); GL.DepthMask(true); GL.Enable(EnableCap.CullFace);
            forward!.Use(); SetLighting(forward, camera, objects, settings, sunDirection); DrawObjects(forward, viewProjection, objects, false);
        }
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

    private void DrawObjects(ShaderProgram program, Matrix4 viewProjection, IReadOnlyList<RenderObject> objects, bool shadowPass)
    {
        program.Use(); program.Set("uViewProjection", viewProjection); GL.Enable(EnableCap.DepthTest); GL.DepthMask(true);
        foreach (var item in objects)
        {
            if (!item.Design.Visible || item.Design.Kind == ObjectKind.Group) continue;
            if (item.Design.Primitive == PrimitiveKind.Model)
            {
                var entry = Instance(item);
                bool actor = item.Design.Kind is ObjectKind.Player or ObjectKind.Npc;
                Matrix4[] displayWorld = entry.Controller.GetDisplayWorld(item.InterpolationAlpha);
                Matrix4 correction = actor ? entry.Model.ActorCorrection : Matrix4.Identity;
                for (int i = 0; i < entry.Model.Data.Primitives.Length; i++)
                {
                    var primitive = entry.Model.Data.Primitives[i];
                    Matrix4 meshWorld = displayWorld[primitive.Node] * correction * item.ModelMatrix;
                    program.Set("uModel", meshWorld);
                    program.Set("uSkinned", primitive.Skin >= 0 ? 1 : 0);
                    if (primitive.Skin >= 0)
                    {
                        Matrix4[] palette = entry.Model.Data.CreatePalette(primitive.Skin, primitive.Node, displayWorld);
                        GL.BindBuffer(BufferTarget.UniformBuffer, boneBuffer); GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, palette.Length * 64, palette);
                    }
                    BindMaterial(program, primitive.Material, entry.Model.Path + "/" + i, item.Design.Material, true);
                    SetCulling(primitive.Material.DoubleSided, meshWorld);
                    Draw(entry.Model.Meshes[i]);
                }
            }
            else if (primitives.TryGetValue(item.Design.Primitive, out var mesh))
            {
                program.Set("uModel", item.ModelMatrix); program.Set("uSkinned", 0);
                var material = item.Design.Material;
                BindMaterial(program, new(new(material.BaseColor.ToVector(), 1), material.Metallic, material.Roughness, false, null, null, null, 1, -1), "", material, false);
                SetCulling(item.Design.Primitive == PrimitiveKind.Plane, item.ModelMatrix); Draw(mesh);
            }
        }
        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
    }

    private void BindMaterial(ShaderProgram program, ModelMaterial imported, string key, MaterialData design, bool useImported)
    {
        // 模型实例颜色作tint，保留同一GLB内不同子材质的颜色差异。
        Vector4 baseColor = useImported ? imported.BaseColor * new Vector4(design.BaseColor.ToVector(), 1) : imported.BaseColor;
        program.Set("uBaseColor", baseColor); program.Set("uMetallic", imported.Metallic); program.Set("uRoughness", imported.Roughness);
        program.Set("uNormalScale", imported.NormalScale); program.Set("uAlphaCutoff", imported.AlphaCutoff);
        var (baseTexture, normalTexture, mrTexture) = LoadTextures(imported, key, design);
        program.Set("uHasBaseTexture", baseTexture != 0 ? 1 : 0); program.Set("uHasNormalTexture", normalTexture != 0 ? 1 : 0); program.Set("uHasMrTexture", mrTexture != 0 ? 1 : 0);
        BindTexture(program, "uBaseTexture", baseTexture, 0); BindTexture(program, "uNormalTexture", normalTexture, 1); BindTexture(program, "uMrTexture", mrTexture, 2);
    }

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

    private void SetLighting(ShaderProgram program, CameraState camera, IReadOnlyList<RenderObject> objects, RenderSettings settings, Vector3 direction)
    {
        program.Set("uCamera", camera.Position); program.Set("uSunDirection", direction); program.Set("uSunRadiance", settings.SunColor.ToVector() * settings.SunIntensity);
        program.Set("uLightViewProjection", lightViewProjection); program.Set("uShadows", settings.Shadows ? 1 : 0); BindTexture(program, "uShadow", shadowTexture, 5);
        var lights = objects.Where(o => o.Design.Kind == ObjectKind.PointLight && o.Design.Visible).Take(4).ToArray();
        program.Set("uPointCount", lights.Length);
        for (int i = 0; i < lights.Length; i++)
        {
            var light = lights[i]; Vector3 position = light.ModelMatrix.ExtractTranslation();
            float radius = light.Design.Parameters.GetValueOrDefault("radius", 10), intensity = light.Design.Parameters.GetValueOrDefault("intensity", 25);
            program.Set($"uPointPositions[{i}]", new Vector4(position, radius)); program.Set($"uPointColors[{i}]", new Vector4(light.Design.Material.BaseColor.ToVector(), intensity));
        }
    }

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

    private void Draw(GpuMesh mesh) { mesh.Draw(); drawCalls++; triangles += mesh.IndexCount / 3; }
    private static void SetCulling(bool doubleSided, Matrix4 world)
    {
        GL.FrontFace(world.Determinant < 0 ? FrontFaceDirection.Cw : FrontFaceDirection.Ccw);
        if (doubleSided) GL.Disable(EnableCap.CullFace); else GL.Enable(EnableCap.CullFace);
    }
    private void Fullscreen()
    {
        GL.Disable(EnableCap.DepthTest); GL.DepthMask(false); GL.Disable(EnableCap.CullFace); GL.Disable(EnableCap.Blend);
        GL.BindVertexArray(fullscreenVao); GL.DrawArrays(PrimitiveType.Triangles, 0, 3); drawCalls++; triangles++;
    }
    private static void BindTexture(ShaderProgram program, string uniform, int texture, int unit)
    { GL.ActiveTexture(TextureUnit.Texture0 + unit); GL.BindTexture(TextureTarget.Texture2D, texture); program.Set(uniform, unit); }
    private void BindSky(ShaderProgram program, int unit)
    { GL.ActiveTexture(TextureUnit.Texture0 + unit); GL.BindTexture(TextureTarget.TextureCubeMap, skyTexture); program.Set("uSky", unit); }

    private void CreateShadow()
    {
        shadowBuffer = GL.GenFramebuffer(); GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadowBuffer);
        shadowTexture = RenderTargets.Texture(ShadowResolution, ShadowResolution, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent, PixelType.UnsignedInt);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowTexture, 0);
        GL.DrawBuffer(DrawBufferMode.None); GL.ReadBuffer(ReadBufferMode.None); RenderTargets.Check("Shadow");
    }
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
    private void CreateParticles()
    {
        particleVao = GL.GenVertexArray(); particleVbo = GL.GenBuffer(); GL.BindVertexArray(particleVao); GL.BindBuffer(BufferTarget.ArrayBuffer, particleVbo);
        int[] sizes = [3, 4, 2], offsets = [0, 3, 7];
        for (int i = 0; i < 3; i++) { GL.EnableVertexAttribArray(i); GL.VertexAttribPointer(i, sizes[i], VertexAttribPointerType.Float, false, 9 * sizeof(float), offsets[i] * sizeof(float)); }
    }
    private void DrawParticles(CameraState camera, Matrix4 viewProjection, IReadOnlyList<ParticleVisual> visuals)
    {
        if (visuals.Count == 0) return;
        var sorted = visuals.OrderByDescending(p => (p.Position - camera.Position).LengthSquared).Take(8192).ToArray();
        Matrix4 inverseView = camera.View.Inverted(); Vector3 right = inverseView.Row0.Xyz, up = inverseView.Row1.Xyz;
        var vertices = new float[sorted.Length * 6 * 9]; int cursor = 0;
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
    private void EnsureOwner()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("GL调用必须位于创建渲染器的当前上下文线程");
    }
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
    private sealed record GpuModel(string Path, GltfModel Data, GpuMesh[] Meshes, Matrix4 ActorCorrection);
}
