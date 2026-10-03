using System.Text;
using System.Text.Json;
using OpenTK.Mathematics;
using SharpGLTF.Schema2;
using NMatrix = System.Numerics.Matrix4x4;
using NQuaternion = System.Numerics.Quaternion;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;

namespace G104.Engine.Assets;

public sealed class GltfModelLoader(AssetRoot assets)
{
    public GltfModel Load(string relativePath)
    {
        string path = assets.Resolve(relativePath);
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".glb" or ".gltf")) throw new InvalidDataException($"V1模型只接受glTF 2.0/GLB：{relativePath}");
        using JsonDocument json = ReadJson(path, extension);
        JsonElement document = json.RootElement;
        if (document.GetProperty("asset").GetProperty("version").GetString() != "2.0")
            throw new InvalidDataException($"模型版本不是glTF 2.0：{relativePath}");
        if (document.TryGetProperty("extensionsUsed", out var extensions) && extensions.GetArrayLength() > 0)
            throw new InvalidDataException($"V1尚不支持glTF扩展：{string.Join(", ", extensions.EnumerateArray().Select(x => x.GetString()))}");
        foreach (string listName in new[] { "buffers", "images" })
        {
            if (!document.TryGetProperty(listName, out var list)) continue;
            foreach (var entry in list.EnumerateArray())
            {
                if (!entry.TryGetProperty("uri", out var uriValue)) continue;
                string uri = uriValue.GetString()!;
                if (uri.StartsWith("data:", StringComparison.Ordinal)) continue;
                if (uri.Contains('#') || uri.Contains('?') || Uri.TryCreate(uri, UriKind.Absolute, out _))
                    throw new InvalidDataException($"模型外部URI必须为本地相对路径：{uri}");
                string satellite = Path.Combine(Path.GetDirectoryName(relativePath) ?? "", Uri.UnescapeDataString(uri));
                _ = assets.Resolve(satellite);
            }
        }
        ModelRoot root = ModelRoot.Load(path);
        var nodes = new ModelNode[root.LogicalNodes.Count];
        for (int i = 0; i < nodes.Length; i++)
        {
            var node = root.LogicalNodes[i];
            if (!NMatrix.Decompose(node.LocalMatrix, out var scale, out var rotation, out var translation))
                throw new InvalidDataException($"节点局部矩阵不能分解为TRS：{node.Name}");
            nodes[i] = new(node.Name ?? $"Node{i}", node.VisualParent?.LogicalIndex ?? -1,
                new(Convert(translation), Convert(rotation), Convert(scale)));
        }
        int[] order = BuildOrder(nodes);
        var skinnedNodes = new HashSet<int>();
        void AddSkinAncestors(int index)
        { for (int current = index; current >= 0; current = nodes[current].Parent) skinnedNodes.Add(current); }
        foreach (var skin in root.LogicalSkins)
            for (int joint = 0; joint < skin.JointsCount; joint++) AddSkinAncestors(skin.GetJoint(joint).Item1.LogicalIndex);
        foreach (var node in root.LogicalNodes) if (node.Skin is not null) AddSkinAncestors(node.LogicalIndex);
        foreach (int index in skinnedNodes)
            if (nodes[index].DefaultPose.Scale.X <= 0 || nodes[index].DefaultPose.Scale.Y <= 0 || nodes[index].DefaultPose.Scale.Z <= 0)
                throw new InvalidDataException($"V1蒙皮节点不支持零/负缩放：{nodes[index].Name}");
        var skins = root.LogicalSkins.Select(skin =>
        {
            if (skin.JointsCount > GltfModel.MaximumJoints) throw new InvalidDataException($"骨骼数量{skin.JointsCount}超过V1容量{GltfModel.MaximumJoints}");
            return new ModelSkin(Enumerable.Range(0, skin.JointsCount).Select(i => skin.GetJoint(i).Item1.LogicalIndex).ToArray(),
                Enumerable.Range(0, skin.JointsCount).Select(i => Convert(skin.GetJoint(i).Item2)).ToArray());
        }).ToArray();
        var primitives = new List<ModelPrimitive>();
        // 仅默认场景的可见节点进入绘制；逻辑节点仍完整保留用于骨架求值。
        var scene = root.DefaultScene ?? root.LogicalScenes.FirstOrDefault() ?? throw new InvalidDataException("模型没有场景");
        foreach (var node in Node.Flatten(scene))
        {
            if (node.Mesh is null) continue;
            foreach (var primitive in node.Mesh.Primitives)
            {
                if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES || primitive.MorphTargetsCount != 0)
                    throw new InvalidDataException($"节点{node.Name}只支持三角形且不支持Morph");
                if (primitive.VertexAccessors.Keys.Any(k => k.StartsWith("JOINTS_", StringComparison.Ordinal) && k != "JOINTS_0" || k.StartsWith("WEIGHTS_", StringComparison.Ordinal) && k != "WEIGHTS_0"))
                    throw new InvalidDataException($"节点{node.Name}超过V1每顶点四个骨骼影响");
                string[] supported = ["POSITION", "NORMAL", "TEXCOORD_0", "TANGENT", "JOINTS_0", "WEIGHTS_0"];
                // 未被材质引用的额外UV集不影响UV0绘制；真正引用UV1的贴图会在材质导入时报错。
                bool Unsupported(string key) => !supported.Contains(key) && !key.StartsWith("TEXCOORD_", StringComparison.Ordinal);
                if (primitive.VertexAccessors.Keys.Any(Unsupported))
                    throw new InvalidDataException($"节点{node.Name}包含V1不支持的顶点属性：{string.Join(",", primitive.VertexAccessors.Keys.Where(Unsupported))}");
                var positions = primitive.GetVertexAccessor("POSITION")?.AsVector3Array() ?? throw new InvalidDataException("网格缺少POSITION");
                var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array() ?? throw new InvalidDataException("V1网格需要NORMAL");
                var uv = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                var tangent = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
                var joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
                var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();
                if (node.Skin is not null && (joints is null || weights is null)) throw new InvalidDataException("蒙皮网格缺少JOINTS_0/WEIGHTS_0");
                uint[] indices = primitive.IndexAccessor is null ? Enumerable.Range(0, positions.Count).Select(i => (uint)i).ToArray() : primitive.GetIndices().ToArray();
                if (indices.Length % 3 != 0 || indices.Any(i => i >= positions.Count)) throw new InvalidDataException("三角形索引无效");
                ModelMaterial material = ImportMaterial(primitive.Material);
                if ((material.BaseColorImage is not null || material.NormalImage is not null || material.MetallicRoughnessImage is not null) && uv is null)
                    throw new InvalidDataException("贴图材质需要TEXCOORD_0");
                var vertices = new float[positions.Count * 20];
                for (int i = 0; i < positions.Count; i++)
                {
                    int offset = i * 20;
                    Put(vertices, offset, new NVector4(positions[i], 0), 3);
                    Put(vertices, offset + 3, new NVector4(normals[i], 0), 3);
                    if (uv is not null) { vertices[offset + 6] = uv[i].X; vertices[offset + 7] = uv[i].Y; }
                    Put(vertices, offset + 8, tangent?[i] ?? new NVector4(1, 0, 0, 1), 4);
                    if (joints is not null && weights is not null)
                    {
                        NVector4 weight = weights[i];
                        float sum = weight.X + weight.Y + weight.Z + weight.W;
                        if (!float.IsFinite(sum) || sum <= 0 || weight.X < 0 || weight.Y < 0 || weight.Z < 0 || weight.W < 0) throw new InvalidDataException("骨骼权重无效");
                        NVector4 joint = joints[i];
                        for (int c = 0; c < 4; c++)
                        {
                            if (weight[c] <= 0) joint[c] = 0; // shader仍会读取零权重槽，索引也必须有界。
                            else if (joint[c] < 0 || joint[c] >= (node.Skin?.JointsCount ?? 0) || joint[c] != MathF.Floor(joint[c])) throw new InvalidDataException("骨骼索引无效");
                        }
                        Put(vertices, offset + 12, joint, 4);
                        Put(vertices, offset + 16, weight / sum, 4);
                    }
                    else vertices[offset + 16] = 1;
                }
                if (tangent is null && uv is not null) GenerateTangents(vertices, indices);
                primitives.Add(new(node.LogicalIndex, node.Skin?.LogicalIndex ?? -1, vertices, indices, material));
            }
        }
        var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
        foreach (var animation in root.LogicalAnimations)
        {
            string name = animation.Name ?? $"Clip{animation.LogicalIndex}";
            var tracks = new List<AnimationTrack>();
            foreach (var channel in animation.Channels)
            {
                int target = channel.TargetNode?.LogicalIndex ?? throw new InvalidDataException($"动作{name}含非TRS动画");
                switch (channel.TargetNodePath)
                {
                    case PropertyPath.translation: tracks.Add(ReadVectorTrack(target, TrackProperty.Translation, channel.GetTranslationSampler())); break;
                    case PropertyPath.scale:
                        var scaleTrack = ReadVectorTrack(target, TrackProperty.Scale, channel.GetScaleSampler());
                        if (skinnedNodes.Contains(target) && scaleTrack.Values.Any(v => v.X <= 0 || v.Y <= 0 || v.Z <= 0))
                            throw new InvalidDataException($"V1蒙皮动画不支持零/负缩放：{name}/{nodes[target].Name}");
                        tracks.Add(scaleTrack);
                        break;
                    case PropertyPath.rotation:
                        var sampler = channel.GetRotationSampler();
                        CheckInterpolation(sampler.InterpolationMode);
                        var keys = sampler.GetLinearKeys().ToArray();
                        tracks.Add(new(target, TrackProperty.Rotation, keys.Select(x => x.Item1).ToArray(), keys.Select(x => new Vector4(x.Item2.X, x.Item2.Y, x.Item2.Z, x.Item2.W)).ToArray(), sampler.InterpolationMode == AnimationInterpolationMode.STEP));
                        break;
                    default: throw new InvalidDataException($"动作{name}属性{channel.TargetNodePath}尚未支持");
                }
            }
            if (!clips.TryAdd(name, new(name, animation.Duration, tracks.ToArray()))) throw new InvalidDataException($"动作名重复：{name}");
        }
        if (primitives.Count == 0) throw new InvalidDataException($"模型默认场景没有可绘制三角形：{relativePath}");
        return new() { SourcePath = relativePath, Nodes = nodes, EvaluationOrder = order, Primitives = primitives.ToArray(), Skins = skins, Clips = clips };
    }

    private static JsonDocument ReadJson(string path, string extension)
    {
        if (extension == ".gltf") return JsonDocument.Parse(File.ReadAllBytes(path));
        using var reader = new BinaryReader(File.OpenRead(path));
        if (reader.ReadUInt32() != 0x46546C67 || reader.ReadUInt32() != 2) throw new InvalidDataException("无效GLB头");
        uint length = reader.ReadUInt32();
        if (length != reader.BaseStream.Length) throw new InvalidDataException("GLB长度不一致");
        int jsonLength = checked((int)reader.ReadUInt32());
        if (reader.ReadUInt32() != 0x4E4F534A || jsonLength > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("GLB缺少JSON块");
        return JsonDocument.Parse(reader.ReadBytes(jsonLength));
    }

    private static ModelMaterial ImportMaterial(Material? material)
    {
        if (material is null) return new(Vector4.One, 1, 1, false, null, null, null, 1, -1);
        if (material.Alpha == AlphaMode.BLEND) throw new InvalidDataException($"V1模型材质不支持Alpha Blend：{material.Name}");
        var color = material.FindChannel("BaseColor");
        var mr = material.FindChannel("MetallicRoughness");
        var normal = material.FindChannel("Normal");
        var emissive = material.FindChannel("Emissive");
        if (material.FindChannel("Occlusion")?.Texture is not null || emissive?.Texture is not null || emissive is { } e && (e.Color.X != 0 || e.Color.Y != 0 || e.Color.Z != 0))
            throw new InvalidDataException($"V1尚不支持模型材质的Occlusion/Emissive输入：{material.Name}");
        foreach (var channel in material.Channels)
            if (channel.Texture is not null && (channel.TextureCoordinate != 0 || channel.TextureTransform is not null)) throw new InvalidDataException("V1只支持UV0且不支持纹理变换");
        NVector4 baseColor = color?.Color ?? NVector4.One;
        return new(new(baseColor.X, baseColor.Y, baseColor.Z, baseColor.W), mr?.GetFactor("MetallicFactor") ?? 1, mr?.GetFactor("RoughnessFactor") ?? 1,
            material.DoubleSided, Image(color), Image(normal), Image(mr), normal?.GetFactor("NormalScale") ?? 1, material.Alpha == AlphaMode.MASK ? material.AlphaCutoff : -1);
    }

    private static ModelTexture? Image(MaterialChannel? channel)
    {
        var image = channel?.Texture?.PrimaryImage;
        if (image is null) return null;
        if (!image.Content.IsPng && !image.Content.IsJpg) throw new InvalidDataException("V1贴图只支持PNG/JPEG");
        var sampler = channel?.TextureSampler;
        return new(image.Content.Content.ToArray(), (int)(sampler?.WrapS ?? TextureWrapMode.REPEAT), (int)(sampler?.WrapT ?? TextureWrapMode.REPEAT),
            sampler is null || sampler.MinFilter == TextureMipMapFilter.DEFAULT ? 9987 : (int)sampler.MinFilter,
            sampler is null || sampler.MagFilter == TextureInterpolationFilter.DEFAULT ? 9729 : (int)sampler.MagFilter);
    }

    private static AnimationTrack ReadVectorTrack(int node, TrackProperty property, IAnimationSampler<NVector3> sampler)
    {
        CheckInterpolation(sampler.InterpolationMode);
        var keys = sampler.GetLinearKeys().ToArray();
        return new(node, property, keys.Select(x => x.Item1).ToArray(), keys.Select(x => new Vector4(x.Item2.X, x.Item2.Y, x.Item2.Z, 0)).ToArray(), sampler.InterpolationMode == AnimationInterpolationMode.STEP);
    }

    private static void CheckInterpolation(AnimationInterpolationMode mode)
    {
        if (mode is not (AnimationInterpolationMode.LINEAR or AnimationInterpolationMode.STEP)) throw new InvalidDataException($"V1动作插值尚不支持：{mode}");
    }

    private static int[] BuildOrder(ModelNode[] nodes)
    {
        var order = new List<int>();
        var visiting = new byte[nodes.Length];
        void Visit(int i)
        {
            if (visiting[i] == 2) return;
            if (visiting[i] == 1) throw new InvalidDataException("模型节点层级存在环");
            visiting[i] = 1;
            if (nodes[i].Parent >= 0) Visit(nodes[i].Parent);
            visiting[i] = 2;
            order.Add(i);
        }
        for (int i = 0; i < nodes.Length; i++) Visit(i);
        return order.ToArray();
    }

    private static void Put(float[] values, int offset, NVector4 value, int count)
    { for (int i = 0; i < count; i++) values[offset + i] = value[i]; }

    internal static void GenerateTangents(float[] vertices, uint[] indices)
    {
        var tangent = new Vector3[vertices.Length / 20];
        var bitangent = new Vector3[tangent.Length];
        Vector3 V(int i) => new(vertices[i * 20], vertices[i * 20 + 1], vertices[i * 20 + 2]);
        Vector2 U(int i) => new(vertices[i * 20 + 6], vertices[i * 20 + 7]);
        for (int i = 0; i < indices.Length; i += 3)
        {
            int a = (int)indices[i], b = (int)indices[i + 1], c = (int)indices[i + 2];
            Vector3 e1 = V(b) - V(a), e2 = V(c) - V(a);
            Vector2 d1 = U(b) - U(a), d2 = U(c) - U(a);
            float determinant = d1.X * d2.Y - d1.Y * d2.X;
            if (MathF.Abs(determinant) < 1e-8f) continue;
            Vector3 t = (e1 * d2.Y - e2 * d1.Y) / determinant;
            Vector3 bt = (e2 * d1.X - e1 * d2.X) / determinant;
            tangent[a] += t; tangent[b] += t; tangent[c] += t;
            bitangent[a] += bt; bitangent[b] += bt; bitangent[c] += bt;
        }
        for (int i = 0; i < tangent.Length; i++)
        {
            int o = i * 20;
            Vector3 n = new(vertices[o + 3], vertices[o + 4], vertices[o + 5]);
            Vector3 t = tangent[i] - n * Vector3.Dot(n, tangent[i]);
            if (t.LengthSquared < 1e-10f) t = Vector3.Cross(MathF.Abs(n.Y) < .9f ? Vector3.UnitY : Vector3.UnitX, n);
            t.Normalize();
            vertices[o + 8] = t.X; vertices[o + 9] = t.Y; vertices[o + 10] = t.Z;
            vertices[o + 11] = Vector3.Dot(Vector3.Cross(n, t), bitangent[i]) < 0 ? -1 : 1;
        }
    }

    private static Vector3 Convert(NVector3 v) => new(v.X, v.Y, v.Z);
    private static Quaternion Convert(NQuaternion q) => new(q.X, q.Y, q.Z, q.W);
    private static Matrix4 Convert(NMatrix m) => new(m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44);
}
