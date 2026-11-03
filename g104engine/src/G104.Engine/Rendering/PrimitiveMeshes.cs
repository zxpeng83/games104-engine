using OpenTK.Mathematics;

namespace G104.Engine.Rendering;

/// <summary>生成与模型导入共用20-float布局的基础几何；返回的GPU网格由渲染器释放。</summary>
internal static class PrimitiveMeshes
{
    /// <summary>生成以原点为中心、边长一米的立方体，每个面独立保存法线与UV。</summary>
    public static GpuMesh Cube()
    {
        var vertices = new List<float>(); var indices = new List<uint>();
        // 为一面写入四个顶点及两个三角形，切线方向和UV保持一致。
        void Face(Vector3 n, Vector3 tangent, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            uint start = (uint)(vertices.Count / 20);
            Add(vertices, a, n, new(0, 0), tangent); Add(vertices, b, n, new(1, 0), tangent);
            Add(vertices, c, n, new(1, 1), tangent); Add(vertices, d, n, new(0, 1), tangent);
            indices.AddRange([start, start + 1, start + 2, start, start + 2, start + 3]);
        }
        const float h = .5f;
        Face(Vector3.UnitZ, Vector3.UnitX, new(-h,-h,h), new(h,-h,h), new(h,h,h), new(-h,h,h));
        Face(-Vector3.UnitZ, -Vector3.UnitX, new(h,-h,-h), new(-h,-h,-h), new(-h,h,-h), new(h,h,-h));
        Face(Vector3.UnitX, -Vector3.UnitZ, new(h,-h,h), new(h,-h,-h), new(h,h,-h), new(h,h,h));
        Face(-Vector3.UnitX, Vector3.UnitZ, new(-h,-h,-h), new(-h,-h,h), new(-h,h,h), new(-h,h,-h));
        Face(Vector3.UnitY, Vector3.UnitX, new(-h,h,h), new(h,h,h), new(h,h,-h), new(-h,h,-h));
        Face(-Vector3.UnitY, Vector3.UnitX, new(-h,-h,-h), new(h,-h,-h), new(h,-h,h), new(-h,-h,h));
        return new(vertices.ToArray(), indices.ToArray());
    }
    /// <summary>生成位于XZ平面、法线朝+Y的一米方形网格。</summary>
    public static GpuMesh Plane()
    {
        var v = new List<float>();
        Add(v, new(-.5f,0,.5f), Vector3.UnitY, new(0,0), Vector3.UnitX);
        Add(v, new(.5f,0,.5f), Vector3.UnitY, new(1,0), Vector3.UnitX);
        Add(v, new(.5f,0,-.5f), Vector3.UnitY, new(1,1), Vector3.UnitX);
        Add(v, new(-.5f,0,-.5f), Vector3.UnitY, new(0,1), Vector3.UnitX);
        return new(v.ToArray(), [0,1,2,0,2,3]);
    }
    /// <summary>按经纬环生成半径0.5米的球，接缝处重复顶点以保留连续UV。</summary>
    public static GpuMesh Sphere()
    {
        const int rings = 16, segments = 24;
        var vertices = new List<float>(); var indices = new List<uint>();
        for (int y = 0; y <= rings; y++) for (int x = 0; x <= segments; x++)
        {
            float phi = y * MathF.PI / rings, theta = x * MathF.Tau / segments;
            var normal = new Vector3(MathF.Sin(phi) * MathF.Cos(theta), MathF.Cos(phi), MathF.Sin(phi) * MathF.Sin(theta));
            Add(vertices, normal * .5f, normal, new((float)x / segments, (float)y / rings), new(-MathF.Sin(theta), 0, MathF.Cos(theta)));
        }
        for (int y = 0; y < rings; y++) for (int x = 0; x < segments; x++)
        {
            uint a = (uint)(y * (segments + 1) + x), b = a + (uint)segments + 1;
            indices.AddRange([a, a + 1, b, a + 1, b + 1, b]);
        }
        return new(vertices.ToArray(), indices.ToArray());
    }
    /// <summary>追加统一顶点布局；基础几何使用刚性默认关节/权重及正切线手性。</summary>
    private static void Add(List<float> vertices, Vector3 p, Vector3 n, Vector2 uv, Vector3 t) => vertices.AddRange([
        p.X,p.Y,p.Z,n.X,n.Y,n.Z,uv.X,uv.Y,t.X,t.Y,t.Z,1,0,0,0,0,1,0,0,0]);
}
