using G104.Engine.Assets;
using G104.Engine.Scene;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;

namespace G104.Engine.Rendering;

// 入口要求当前线程持有GL上下文。CPU参考独立使用double公式，不以两管线互相吻合作为正确性证明。
public static class RenderingVerification
{
    private const int Size = 129;

    public static IReadOnlyList<string> Run(string assetRoot, string? verificationRoot = null)
    {
        string root = CreateFixtures(assetRoot, verificationRoot);
        var results = new List<string>(); int failures = 0;
        void Check(string name, Action<List<string>> action)
        {
            try { action(results); results.Add($"PASS {name}"); }
            catch (Exception error) { failures++; results.Add($"FAIL {name}: {error.Message}"); }
        }
        using var renderer = new TrainingRenderer(root, Size, Size);
        Check("GGX正对/斜光实际平面HDR与独立double参考", r => VerifyBrdf(renderer, r));
        Check("低粗糙度高强度HDR与色调映射保持有限", r => VerifyHdrRange(renderer, r));
        Check("FP32极端参数明确拒绝且double预乘允许有限radiance", r => VerifyInputRange(renderer, r));
        Check("基础反射色超出RGBA8合同明确拒绝且光源HDR色仍允许", r => VerifyReflectanceRange(renderer, root, r));
        Check("Deferred经过G-buffer量化仍保留0.045粗糙度下限", r => VerifyRoughnessFloor(renderer, r));
        Check("Billboard视深度顺序与独立两色source-over像素", r => VerifyParticles(renderer, r));
        Check("无UV模型后加Normal覆盖被Prepare拒绝并保留旧显示/动画", r => VerifyOverride(renderer, root, true, r));
        Check("无UV模型后加BaseColor覆盖被Prepare拒绝并保留旧显示/动画", r => VerifyOverride(renderer, root, false, r));
        Check("共享mesh的无skin刚性节点忽略皮肤属性", r => VerifyRigidSkin(root, r));
        Check("近相反grazing V/L生产BRDF与独立单位half-vector参考", r => VerifyAntipodalBrdf(root, r));
        ErrorCode glError = GL.GetError();
        if (glError != ErrorCode.NoError) { failures++; results.Add($"FAIL GL error {glError}"); }
        results.Add($"Fixtures: {root}");
        if (failures > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, results));
        return results;
    }

    private static CameraState FrontCamera(Vector3 position) => new(position, Vector3.Zero,
        Matrix4.LookAt(position, Vector3.Zero, Vector3.UnitY), Matrix4.CreateOrthographic(4, 4, .1f, 20));
    private static RenderObject Plane(float roughness) => new(Guid.NewGuid(),
        Matrix4.CreateScale(10) * Matrix4.CreateRotationX(MathF.PI / 2),
        new() { Name = "GGXPlane", Primitive = PrimitiveKind.Plane,
            Material = new() { BaseColor = Float3.One, Metallic = 1, Roughness = roughness } }, new());
    private static RenderSettings Light(RenderPipelineMode pipeline, Vector3 direction, float intensity = 1) => new()
    { Pipeline = pipeline, SunDirection = Float3.From(direction), SunColor = Float3.One, SunIntensity = intensity, Shadows = false, Fxaa = false };

    private static void VerifyBrdf(TrainingRenderer renderer, List<string> results)
    {
        var camera = FrontCamera(new(0, 0, 5));
        foreach (var pipeline in Enum.GetValues<RenderPipelineMode>())
        foreach (float roughness in new[] { .045f, .15f, .6f })
        foreach (float lightAngle in new[] { 0f, .08f, .6f })
        {
            Vector3 light = new(MathF.Sin(lightAngle), 0, MathF.Cos(lightAngle));
            renderer.Render(camera, [Plane(roughness)], Light(pipeline, -light), [], RenderDebugView.Final);
            Vector4 pixel = renderer.ReadHdrPixel(Size / 2, Size / 2);
            // 直接读存储roughness：GPU的Half舍入策略可能与System.Half转换不同，参考不借用lighting输出。
            double sampledRoughness = pipeline == RenderPipelineMode.Deferred ? Math.Max(renderer.ReadNormalRoughPixel(Size / 2, Size / 2).W, .045f) : roughness;
            double expected = ReferenceBrdf(sampledRoughness, lightAngle);
            Require(Finite(pixel) && Math.Abs(pixel.X - expected) <= Math.Max(.002, expected * .001),
                $"{pipeline} rough={roughness} angle={lightAngle}: HDR={pixel.X:G9}, reference={expected:G12}");
            if (lightAngle == 0) results.Add($"{pipeline} rough={roughness}: HDR={pixel.X:G9}, independent reference={expected:G12}, D={4 * expected:G12}");
        }
    }

    // 通用GGX定义与Schlick-GGX geometry，N=V=(0,0,1)，白色金属F=1，NL在最后乘入。
    private static double ReferenceBrdf(double perceptualRoughness, double lightAngle)
    {
        double alpha = perceptualRoughness * perceptualRoughness, alphaSquared = alpha * alpha;
        double nl = Math.Cos(lightAngle), nh = Math.Cos(lightAngle / 2);
        double denominator = nh * nh * (alphaSquared - 1) + 1;
        double distribution = alphaSquared / (Math.PI * denominator * denominator);
        double k = Math.Pow(perceptualRoughness + 1, 2) / 8;
        double geometry = nl / (nl * (1 - k) + k);
        return distribution * geometry / 4;
    }

    private static void VerifyHdrRange(TrainingRenderer renderer, List<string> results)
    {
        foreach (var pipeline in Enum.GetValues<RenderPipelineMode>())
        {
            renderer.Render(FrontCamera(new(0, 0, 5)), [Plane(.045f)], Light(pipeline, -Vector3.UnitZ, 10), [], RenderDebugView.Final);
            Vector4 hdr = renderer.ReadHdrPixel(Size / 2, Size / 2);
            double roughness = pipeline == RenderPipelineMode.Deferred ? Math.Max(renderer.ReadNormalRoughPixel(Size / 2, Size / 2).W, .045f) : .045f;
            double expected = 10 * ReferenceBrdf(roughness, 0);
            Require(Finite(hdr) && Math.Abs(hdr.X - expected) < expected * .008 && hdr.X > 65504,
                $"{pipeline} HDR={hdr.X:G9}, reference={expected:G12}; requires finite radiance above half-float range");
            var ldr = new float[4]; GL.ReadPixels(Size / 2, Size / 2, 1, 1, PixelFormat.Rgba, PixelType.Float, ldr);
            Require(ldr.All(float.IsFinite) && ldr[0] > .99f && ldr[0] <= 1, $"{pipeline} tonemap={ldr[0]}");
            results.Add($"{pipeline} high-light HDR={hdr.X:G9}, LDR={ldr[0]:G9}");
        }
    }

    private static void VerifyParticles(TrainingRenderer renderer, List<string> results)
    {
        var camera = new CameraState(Vector3.Zero, -Vector3.UnitZ, Matrix4.Identity, Matrix4.CreateOrthographic(4, 4, .1f, 20));
        ParticleVisual near = new(new(1, 0, -5), new(1, 0, 0, .5f), .15f);
        ParticleVisual far = new(new(.99f, 0, -5.001f), new(0, 0, 1, .5f), .15f);
        const int x = 96, y = Size / 2;
        float worldX = ((x + .5f) / Size * 2 - 1) * 2;
        float Alpha(ParticleVisual p) { float radius = MathF.Abs(worldX - p.Position.X) * 2 / p.Size; return p.Color.W * (1 - radius) * (1 - radius); }
        float a = Alpha(near), b = Alpha(far);
        foreach (var pipeline in Enum.GetValues<RenderPipelineMode>())
        {
            var settings = Light(pipeline, -Vector3.UnitZ);
            renderer.Render(camera, [], settings, [], RenderDebugView.Final);
            Vector3 background = renderer.ReadHdrPixel(x, y).Xyz;
            Vector3 expected = near.Color.Xyz * a + (far.Color.Xyz * b + background * (1 - b)) * (1 - a);
            // 枚举顺序也不能改变排序结果。
            foreach (var input in new[] { new[] { near, far }, new[] { far, near } })
            {
                renderer.Render(camera, [], settings, input, RenderDebugView.Final);
                Vector3 actual = renderer.ReadHdrPixel(x, y).Xyz;
                Require((actual - expected).Length < .003f, $"{pipeline} pixel={actual}, source-over reference={expected}; near r²={near.Position.LengthSquared}, far r²={far.Position.LengthSquared}");
                results.Add($"{pipeline} particles HDR={actual}, independent source-over={expected}");
            }
        }
    }

    private static void VerifyInputRange(TrainingRenderer renderer, List<string> results)
    {
        var camera = FrontCamera(new(0, 0, 5)); var plane = Plane(.045f);
        renderer.Render(camera, [plane], Light(RenderPipelineMode.Forward, -Vector3.UnitZ), [], RenderDebugView.Final);
        Vector4 previous = renderer.ReadHdrPixel(Size / 2, Size / 2);
        var unsupported = Light(RenderPipelineMode.Forward, -Vector3.UnitZ, float.MaxValue);
        unsupported.SunColor = new(float.MaxValue, float.MaxValue, float.MaxValue);
        bool rejected = false;
        try { renderer.Render(camera, [plane], unsupported, [], RenderDebugView.Final); }
        catch (InvalidDataException error) { rejected = error.Message.Contains("FP32", StringComparison.Ordinal); results.Add(error.Message); }
        Require(rejected, "有限DTO的光色*强度溢出必须在double预乘检查时明确拒绝");
        Require(renderer.ReadHdrPixel(Size / 2, Size / 2) == previous, "拒绝设置在GL Pass之前，保留原HDR显示");

        var unsupportedMaterial = Plane(.15f); unsupportedMaterial.Design.Material.BaseColor = new(float.MaxValue, 1, 1);
        rejected = false;
        try { renderer.Prepare([unsupportedMaterial]); }
        catch (InvalidDataException error) { rejected = error.Message.Contains("[0,1]", StringComparison.Ordinal); results.Add(error.Message); }
        Require(rejected, "超范围反射材质在Prepare明确拒绝");

        // 极大方向分量和光色本身仍是有限DTO；颜色*极小强度在double中求得的radiance可正常使用。
        var supported = Light(RenderPipelineMode.Forward, new(0, 0, -float.MaxValue), 1e-20f);
        supported.SunColor = new(1e30f, 1e30f, 1e30f); supported.Exposure = 20;
        renderer.Render(camera, [plane], supported, [], RenderDebugView.Final);
        Vector4 hdr = renderer.ReadHdrPixel(Size / 2, Size / 2);
        double expected = ReferenceBrdf(.045f, 0) * (double)supported.SunColor.X * supported.SunIntensity;
        Require(Finite(hdr) && Math.Abs(hdr.X - expected) <= expected * .008, $"double预乘/方向归一化 HDR={hdr.X:G9}, reference={expected:G12}");
        var ldr = new float[4]; GL.ReadPixels(Size / 2, Size / 2, 1, 1, PixelFormat.Rgba, PixelType.Float, ldr);
        Require(ldr.All(float.IsFinite) && ldr[0] > .99f, "支持范围内高曝光色调映射有限");

        var boundary = Plane(.045f);
        renderer.Prepare([boundary]);
        renderer.Render(camera, [boundary], Light(RenderPipelineMode.Forward, -Vector3.UnitZ, 1e12f), [], RenderDebugView.Final);
        hdr = renderer.ReadHdrPixel(Size / 2, Size / 2);
        Require(Finite(hdr) && hdr.X > 1e16f, "支持上限辐射不截断峰值且保持FP32有限");
        results.Add($"FP32支持上限HDR={hdr.X:G9}; 明确拒绝值不钳制D/粗糙度");
    }

    private static void VerifyReflectanceRange(TrainingRenderer renderer, string root, List<string> results)
    {
        var camera = FrontCamera(new(0, 0, 5));
        var invalid = Plane(.15f); invalid.Design.Material.BaseColor = new(2, 2, 2);
        bool prepareRejected = false;
        try { renderer.Prepare([invalid]); }
        catch (InvalidDataException error) { prepareRejected = error.Message.Contains("[0,1]", StringComparison.Ordinal); results.Add(error.Message); }
        int renderRejects = 0;
        foreach (var pipeline in Enum.GetValues<RenderPipelineMode>())
        {
            try
            {
                renderer.Render(camera, [invalid], Light(pipeline, -Vector3.UnitZ), [], RenderDebugView.Final);
                double roughness = pipeline == RenderPipelineMode.Deferred ? renderer.ReadNormalRoughPixel(Size / 2, Size / 2).W : .15f;
                float actual = renderer.ReadHdrPixel(Size / 2, Size / 2).X;
                results.Add($"BaseColor2 {pipeline} HDR={actual:G9}, independent unclipped input reference={2 * ReferenceBrdf(roughness, 0):G12}");
            }
            catch (InvalidDataException error) { if (error.Message.Contains("[0,1]", StringComparison.Ordinal)) renderRejects++; else throw; }
        }
        Require(prepareRejected && renderRejects == 2, "final effective反射色2必须由Prepare和两管线明确拒绝；不能在Deferred静默夹到1");
        var invalidModel = new RenderObject(Guid.NewGuid(), Matrix4.Identity,
            new() { Primitive = PrimitiveKind.Model, ModelPath = "no-uv.gltf", Material = new() { BaseColor = new(2, 2, 2) } }, new());
        bool modelRejected = false;
        try { renderer.Prepare([invalidModel]); }
        catch (InvalidDataException error) { modelRejected = error.Message.Contains("[0,1]", StringComparison.Ordinal); }
        Require(modelRejected, "模型factor1*tint2必须明确拒绝");
        var validTint = new RenderObject(Guid.NewGuid(), Matrix4.Identity,
            new() { Primitive = PrimitiveKind.Model, ModelPath = "half-base.gltf", Material = new() { BaseColor = new(2, 2, 2) } }, new());
        renderer.Prepare([validTint]);
        foreach (var pipeline in Enum.GetValues<RenderPipelineMode>())
        {
            renderer.Render(FrontCamera(new(5, 0, 0)), [validTint], Light(pipeline, -Vector3.UnitX), [], RenderDebugView.Final);
            double roughness = pipeline == RenderPipelineMode.Deferred ? renderer.ReadNormalRoughPixel(Size / 2, Size / 2).W : .15f;
            Require(Math.Abs(renderer.ReadHdrPixel(Size / 2, Size / 2).X - ReferenceBrdf(roughness, 0)) < ReferenceBrdf(roughness, 0) * .001, "模型factor.5*tint2最终反射色1合法，不能提前拒绝raw tint");
        }
        var brightLight = Light(RenderPipelineMode.Forward, -Vector3.UnitZ); brightLight.SunColor = new(2, 2, 2);
        renderer.Render(camera, [Plane(.15f)], brightLight, [], RenderDebugView.Final);
        double expected = 2 * ReferenceBrdf(.15f, 0);
        Require(Math.Abs(renderer.ReadHdrPixel(Size / 2, Size / 2).X - expected) < expected * .001, "HDR光源颜色2仍允许且不截断radiance");
        var lampDesign = new SceneObjectData { Name = "HdrColorLamp", Kind = ObjectKind.PointLight, Primitive = PrimitiveKind.Cube,
            Material = new() { BaseColor = new(2, 1, .5f), Metallic = 1, Roughness = .15f }, Parameters = new() { ["intensity"] = 2, ["radius"] = 10 } };
        var lamp = new RenderObject(Guid.NewGuid(), Matrix4.CreateScale(.1f) * Matrix4.CreateTranslation(1, 0, 3), lampDesign, new());
        renderer.Prepare([lamp]);
        foreach (var pipeline in Enum.GetValues<RenderPipelineMode>())
        {
            renderer.Render(camera, [Plane(.6f), lamp], Light(pipeline, -Vector3.UnitZ), [], RenderDebugView.Final);
            Require(Finite(renderer.ReadHdrPixel(Size / 2, Size / 2)), "HDR灯色与marker反射材质分域后两管线有限");
        }
        Require(lampDesign.Material.BaseColor == new Float3(2, 1, .5f), "marker归一化不能回写原始灯色/设计历史");
        results.Add("模型factor.5*tint2合法，factor1*tint2拒绝；SunColor2及PointLight颜色(2,1,.5)合法，原设计未改");
    }

    private static void VerifyRoughnessFloor(TrainingRenderer renderer, List<string> results)
    {
        renderer.Render(FrontCamera(new(0, 0, 5)), [Plane(.045f)], Light(RenderPipelineMode.Deferred, -Vector3.UnitZ), [], RenderDebugView.Final);
        double expected = ReferenceBrdf(.045f, 0);
        float actual = renderer.ReadHdrPixel(Size / 2, Size / 2).X;
        float stored = renderer.ReadNormalRoughPixel(Size / 2, Size / 2).W;
        results.Add($"Deferred floor: stored roughness={stored:G9}, HDR={actual:G9}, .045 reference={expected:G12}");
        Require(Math.Abs(actual - expected) < expected * .0003, "G-buffer写入将.045略向下舍入时，BRDF仍必须保留声明的.045下限");
    }

    private static void VerifyOverride(TrainingRenderer renderer, string root, bool normal, List<string> results)
    {
        renderer.ResetScene();
        var importedNoUv = new GltfModelLoader(new(root)).Load("no-uv.gltf");
        Require(importedNoUv.Primitives.All(p => !p.HasUv0), "保留源mesh无UV0能力信息");
        foreach (var p in importedNoUv.Primitives)
        for (int v = 0; v < p.Vertices.Length; v += 20)
        {
            Vector3 normalVector = new(p.Vertices[v + 3], p.Vertices[v + 4], p.Vertices[v + 5]);
            Vector3 tangent = new(p.Vertices[v + 8], p.Vertices[v + 9], p.Vertices[v + 10]);
            Require(Math.Abs(tangent.Length - 1) < .0001f && Math.Abs(Vector3.Dot(normalVector, tangent)) < .0001f, "+X法线无TANGENT模型有正交有限回退");
        }
        var triangle = new RenderObject(Guid.NewGuid(), Matrix4.Identity,
            new() { Name = "NoUvTriangle", Primitive = PrimitiveKind.Model, ModelPath = "no-uv.gltf", Material = new() { BaseColor = Float3.One } }, new());
        var actor = new RenderObject(Guid.NewGuid(), Matrix4.Identity,
            new() { Name = "HistoricalActor", Kind = ObjectKind.Player, Primitive = PrimitiveKind.Model, ModelPath = "models/UAL1_Standard.glb", Visible = false }, new(0, false, 0));
        RenderObject[] previous = [triangle, actor];
        renderer.Prepare(previous); renderer.UpdateAnimations(1f / 60, previous);
        var camera = FrontCamera(new(5, 0, 0)); var settings = Light(RenderPipelineMode.Forward, -Vector3.UnitX);
        renderer.Render(camera, previous, settings, [], RenderDebugView.Final);
        Vector4 before = renderer.ReadHdrPixel(Size / 2, Size / 2); string animationBefore = renderer.AnimationDebug(actor.Id);
        Require(Finite(before) && before.X > 0, "合法无UV/无TANGENT模型能绘制");
        var badDesign = new SceneObjectData { Name = "InvalidUvOverride", Primitive = PrimitiveKind.Model, ModelPath = "no-uv.gltf", Material = new() { BaseColor = Float3.One } };
        if (normal) badDesign.Material.NormalTexture = "plus-x-normal.png"; else badDesign.Material.BaseColorTexture = "plus-x-normal.png";
        var validNew = new RenderObject(Guid.NewGuid(), Matrix4.Identity,
            new() { Primitive = PrimitiveKind.Model, ModelPath = "uv.gltf", Material = new() { NormalTexture = "plus-x-normal.png" } }, new());
        string? rejection = null;
        try { renderer.Prepare([actor, validNew, new(triangle.Id, Matrix4.Identity, badDesign, new())]); }
        catch (InvalidDataException error) { rejection = error.Message; }
        Require(rejection is not null && rejection.Contains("TEXCOORD_0", StringComparison.Ordinal) && rejection.Contains("NoUvNode", StringComparison.Ordinal) && rejection.Contains("primitive", StringComparison.OrdinalIgnoreCase),
            $"需要明确模型/节点/primitive/UV错误；实际={rejection ?? "Prepare accepted invalid override"}");
        Require(renderer.AnimationDebug(actor.Id) == animationBefore, "失败Prepare不重置原动画游标/状态");
        renderer.Render(camera, previous, settings, [], RenderDebugView.Final);
        Require((renderer.ReadHdrPixel(Size / 2, Size / 2) - before).Length < .0001f, "失败Prepare后原无UV显示仍一致");
        Require(renderer.DrainAnimationEvents(actor.Id).Any(e => e.Name == "Fall"), "失败Prepare保留未消费历史Fall事件，不依赖可配置起跳阈值");
        results.Add(rejection!);
    }

    private static void VerifyRigidSkin(string root, List<string> results)
    {
        var model = new GltfModelLoader(new(root)).Load("shared-skin.gltf");
        Require(model.Primitives.Length == 2 && model.Primitives.Count(p => p.Skin >= 0) == 1, "同mesh有独立skinned和rigid引用");
        var rigid = model.Primitives.Single(p => p.Skin < 0);
        var world = new Matrix4[model.Nodes.Length]; model.EvaluateWorld(model.CreateDefaultPose(), world);
        Vector3 local = new(rigid.Vertices[0], rigid.Vertices[1], rigid.Vertices[2]);
        Vector3 transformed = Vector3.TransformPosition(local, world[rigid.Node]);
        Require(Math.Abs(transformed.X - 2) < .0001f && rigid.Vertices[12] == 0 && rigid.Vertices[16] == 1, "刚性引用按node变换且不读无skin的骨骼容量");
        results.Add("shared-skin.gltf: skin node + rigid node PASS；Khronos NODE_SKINNED_MESH_WITHOUT_SKIN为Warning");
    }

    private static void VerifyAntipodalBrdf(string root, List<string> results)
    {
        using var target = new RenderTargets(1, 1);
        using var program = new ShaderProgram(new(root), "fullscreen.vert", "brdf-probe.frag");
        int vao = GL.GenVertexArray();
        try
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, target.HdrBuffer); GL.Viewport(0, 0, 1, 1);
            GL.Disable(EnableCap.DepthTest); GL.Disable(EnableCap.Blend); GL.Disable(EnableCap.CullFace); GL.Disable(EnableCap.FramebufferSrgb);
            GL.BindVertexArray(vao); program.Use(); program.Set("uProbeRoughness", .045f);
            foreach (float epsilon in new[] { 1e-8f, 1e-12f, 1e-20f, 0 })
            {
                program.Set("uProbeEpsilon", epsilon); GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
                var pixel = new float[4]; GL.ReadPixels(0, 0, 1, 1, PixelFormat.Rgba, PixelType.Float, pixel);
                // 对称非零V/L的真正单位half始终+Z。这里独立使用原GGX分布和现有geometry/nv/nl防零约定。
                double roughness = .045f, alphaSquared = Math.Pow(roughness, 4), d = 1 / (Math.PI * alphaSquared);
                double nl = epsilon / Math.Sqrt(1 + (double)epsilon * epsilon), nv = Math.Max(nl, .0001);
                double k = Math.Pow(roughness + 1, 2) / 8;
                double geometry = nv / (nv * (1 - k) + k) * nl / (nl * (1 - k) + k);
                double expected = epsilon == 0 ? 0 : d * geometry / (4 * nv * Math.Max(nl, .0001)) * nl;
                results.Add($"Grazing epsilon={epsilon:G9}: GPU={pixel[0]:G9}, unit-half independent reference={expected:G12}");
                Require(pixel.All(float.IsFinite) && Math.Abs(pixel[0] - expected) <= Math.Max(1e-12, expected * .002), "近相反V/L不能用epsilon缩短half-vector，再套依赖单位half的cross GGX公式");
            }
        }
        finally { GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); GL.BindVertexArray(0); GL.UseProgram(0); GL.DeleteVertexArray(vao); }
    }

    private static string CreateFixtures(string assetRoot, string? verificationRoot)
    {
        string root = Path.Combine(verificationRoot ?? Path.Combine(Path.GetTempPath(), "G104.RenderingVerification"), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); Directory.CreateDirectory(Path.Combine(root, "shaders")); Directory.CreateDirectory(Path.Combine(root, "config")); Directory.CreateDirectory(Path.Combine(root, "models"));
        var assets = new AssetRoot(assetRoot);
        foreach (string file in Directory.GetFiles(Path.Combine(assetRoot, "shaders"))) File.Copy(file, Path.Combine(root, "shaders", Path.GetFileName(file)));
        File.Copy(assets.Resolve("config/character-animation.json"), Path.Combine(root, "config", "character-animation.json"));
        File.Copy(assets.Resolve("models/UAL1_Standard.glb"), Path.Combine(root, "models", "UAL1_Standard.glb"));
        WriteTriangle(Path.Combine(root, "no-uv.gltf"), false, false);
        WriteTriangle(Path.Combine(root, "uv.gltf"), true, false);
        WriteTriangle(Path.Combine(root, "shared-skin.gltf"), false, true);
        JsonNode halfBase = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "no-uv.gltf")))!;
        halfBase["materials"] = new JsonArray(new JsonObject { ["pbrMetallicRoughness"] = new JsonObject
            { ["baseColorFactor"] = new JsonArray(.5f, .5f, .5f, 1), ["metallicFactor"] = 1, ["roughnessFactor"] = .15f } });
        halfBase["meshes"]![0]!["primitives"]![0]!["material"] = 0;
        File.WriteAllText(Path.Combine(root, "half-base.gltf"), halfBase.ToJsonString());
        WriteNormalPng(Path.Combine(root, "plus-x-normal.png"));
        File.WriteAllText(Path.Combine(root, "shaders", "brdf-probe.frag"), """
            #version 430 core
            #include "lighting.glsl"
            uniform float uProbeEpsilon;
            uniform float uProbeRoughness;
            out vec4 color;
            void main() {
                vec3 view=normalize(vec3(1.0,0.0,uProbeEpsilon));
                vec3 light=normalize(vec3(-1.0,0.0,uProbeEpsilon));
                color=vec4(brdf(vec3(1.0),vec3(0.0,0.0,1.0),view,light,1.0,uProbeRoughness),1.0);
            }
            """);
        return root;
    }

    private static void WriteTriangle(string path, bool hasUv, bool sharedSkin)
    {
        var views = new JsonArray(); var accessors = new JsonArray();
        using var buffer = new MemoryStream(); using var writer = new BinaryWriter(buffer, Encoding.UTF8, true);
        int Attribute(float[] data, string type, int count, float[]? min = null, float[]? max = null)
        {
            int offset = (int)buffer.Position; foreach (float value in data) writer.Write(value);
            int view = views.Count; views.Add(new JsonObject { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = data.Length * 4, ["target"] = 34962 });
            var accessor = new JsonObject { ["bufferView"] = view, ["componentType"] = 5126, ["count"] = count, ["type"] = type };
            if (min is not null) accessor["min"] = new JsonArray(min.Select(v => JsonValue.Create(v)).ToArray());
            if (max is not null) accessor["max"] = new JsonArray(max.Select(v => JsonValue.Create(v)).ToArray());
            accessors.Add(accessor); return accessors.Count - 1;
        }
        var attributes = new JsonObject
        {
            ["POSITION"] = Attribute([0,-1,-1, 0,1,-1, 0,0,1], "VEC3", 3, [0,-1,-1], [0,1,1]),
            ["NORMAL"] = Attribute([1,0,0, 1,0,0, 1,0,0], "VEC3", 3)
        };
        if (hasUv) attributes["TEXCOORD_0"] = Attribute([0,0, 1,0, .5f,1], "VEC2", 3);
        if (sharedSkin)
        {
            int offset = (int)buffer.Position; for (int i = 0; i < 12; i++) writer.Write((ushort)0);
            views.Add(new JsonObject { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = 24, ["target"] = 34962 });
            accessors.Add(new JsonObject { ["bufferView"] = views.Count - 1, ["componentType"] = 5123, ["count"] = 3, ["type"] = "VEC4" });
            attributes["JOINTS_0"] = accessors.Count - 1;
            attributes["WEIGHTS_0"] = Attribute([1,0,0,0, 1,0,0,0, 1,0,0,0], "VEC4", 3);
        }
        var nodes = sharedSkin
            ? new JsonArray(new JsonObject { ["name"] = "SkinnedNode", ["mesh"] = 0, ["skin"] = 0 }, new JsonObject { ["name"] = "RigidNode", ["mesh"] = 0, ["translation"] = new JsonArray(2,0,0) }, new JsonObject { ["name"] = "Joint" })
            : new JsonArray(new JsonObject { ["name"] = hasUv ? "UvNode" : "NoUvNode", ["mesh"] = 0 });
        var document = new JsonObject
        {
            ["asset"] = new JsonObject { ["version"] = "2.0", ["generator"] = "G104 regression fixture" }, ["scene"] = 0,
            ["scenes"] = new JsonArray(new JsonObject { ["nodes"] = sharedSkin ? new JsonArray(0,1,2) : new JsonArray(0) }), ["nodes"] = nodes,
            ["buffers"] = new JsonArray(new JsonObject { ["byteLength"] = buffer.Length, ["uri"] = "data:application/octet-stream;base64," + Convert.ToBase64String(buffer.ToArray()) }),
            ["bufferViews"] = views, ["accessors"] = accessors,
            ["meshes"] = new JsonArray(new JsonObject { ["primitives"] = new JsonArray(new JsonObject { ["attributes"] = attributes, ["mode"] = 4 }) })
        };
        if (sharedSkin) document["skins"] = new JsonArray(new JsonObject { ["joints"] = new JsonArray(2), ["skeleton"] = 2 });
        File.WriteAllText(path, document.ToJsonString());
    }

    private static void WriteNormalPng(string path)
    {
        using var file = File.Create(path); file.Write([137,80,78,71,13,10,26,10]);
        void Chunk(string name, byte[] data)
        {
            byte[] type = Encoding.ASCII.GetBytes(name);
            void Big(uint value) => file.Write([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
            Big((uint)data.Length); file.Write(type); file.Write(data);
            uint crc = uint.MaxValue;
            foreach (byte value in type.Concat(data)) { crc ^= value; for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0 : 0xedb88320); }
            Big(~crc);
        }
        Chunk("IHDR", [0,0,0,1, 0,0,0,1, 8,6,0,0,0]);
        using var compressed = new MemoryStream();
        using (var zip = new ZLibStream(compressed, CompressionLevel.SmallestSize, true)) zip.Write([0,255,128,128,255]);
        Chunk("IDAT", compressed.ToArray()); Chunk("IEND", []);
    }

    private static bool Finite(Vector4 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z) && float.IsFinite(v.W);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
